using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NGame.Core.Multiplayer;
using UnityEngine;

namespace NGame.Presentation.Game
{
    public sealed partial class UnityRoomClient
    {
        private bool communicationPolling;
        private bool appFocused = true;
        private bool appPaused;
        private int channelGeneration;
        private string openChannel;
        private long chatCursor;
        private float nextCommunicationPoll;
        private readonly Dictionary<string, ChatSendRequest> pendingMessages = new Dictionary<string, ChatSendRequest>();
        private NativeVoiceClient voiceClient;
        private float lastRequestDuration;
        private float communicationReceivedAt;
        private float communicationTransitEstimate;
        public CommunicationView Communications { get; private set; }
        public ChatMessage[] Messages { get; private set; } = Array.Empty<ChatMessage>();
        public bool IsChatBusy { get; private set; }
        public string ChatError { get; private set; }
        public VoiceView Voice => voiceClient != null ? voiceClient.View : Communications?.voice;
        public event Action CommunicationChanged;

        public void OpenChannel(string channelId)
        {
            if (openChannel != channelId)
            {
                openChannel = channelId; channelGeneration++; chatCursor = 0;
                Messages = Array.Empty<ChatMessage>(); ChatError = null;
            }
            nextCommunicationPoll = 0;
            EnsureCommunicationLoop();
            CommunicationChanged?.Invoke();
        }
        public void CloseChannel()
        {
            openChannel = null; channelGeneration++; Messages = Array.Empty<ChatMessage>();
            chatCursor = 0; ChatError = null;
            CommunicationChanged?.Invoke();
        }
        public void SendChat(string channelId, string text)
        {
            if (!HasSession || IsChatBusy) return;
            var channel = Communications?.channels.FirstOrDefault(c => c.id == channelId && c.canSend);
            if (channel == null) { ChatError = "Cette conversation est fermée pour le moment."; CommunicationChanged?.Invoke(); return; }
            var trimmed = (text ?? string.Empty).Trim();
            if (trimmed.Length == 0 || trimmed.Length > 500) { ChatError = "Écris un message de 1 à 500 caractères."; CommunicationChanged?.Invoke(); return; }
            pendingMessages.TryGetValue(channelId, out var pendingMessage);
            if (pendingMessage != null && pendingMessage.matchId != Communications.matchId)
            { pendingMessages.Remove(channelId); pendingMessage = null; }
            if (pendingMessage != null && (pendingMessage.channelId != channelId || pendingMessage.text != trimmed))
            { ChatError = "Le dernier envoi est incertain. Réessaie le même message avant d’en écrire un autre."; CommunicationChanged?.Invoke(); return; }
            var request = pendingMessage ?? new ChatSendRequest { channelId = channelId, text = trimmed,
                matchId = Communications.matchId, turn = Communications.turn, policyEpoch = Communications.policyEpoch, clientMessageId = Guid.NewGuid().ToString("N") };
            pendingMessages[channelId] = request; IsChatBusy = true; ChatError = null;
            CommunicationChanged?.Invoke();
            var requestGeneration = generation;
            StartCoroutine(Request("POST", RoomPath + "/messages", JsonUtility.ToJson(request), true, requestGeneration, payload =>
            {
                IsChatBusy = false;
                var result = Parse<ChatSendResult>(payload);
                if (result == null || !result.ok) { ChatError = result?.message ?? "Le message n’a pas été accepté."; }
                else
                {
                    pendingMessages.Remove(request.channelId);
                    if (result.communications != null) ApplyCommunications(result.communications);
                    if (openChannel == request.channelId && result.sent != null) AddMessages(new[] { result.sent });
                    nextCommunicationPoll = 0;
                }
                CommunicationChanged?.Invoke();
            }, (message, status) =>
            {
                IsChatBusy = false;
                if (status > 0) pendingMessages.Remove(request.channelId);
                ChatError = status == 0 ? "Envoi incertain. Réessaie le même message pour éviter un doublon." : message;
                nextCommunicationPoll = 0;
                CommunicationChanged?.Invoke();
            }));
        }
        public void SetMicrophone(bool enabled)
        {
            EnsureVoiceClient();
            UpdateVoice();
            voiceClient.SetMicrophone(enabled);
        }
        private void EnsureVoiceClient()
        {
            if (voiceClient != null) return;
            voiceClient = gameObject.AddComponent<NativeVoiceClient>();
            voiceClient.Initialize(this);
            voiceClient.Changed += () => CommunicationChanged?.Invoke();
        }
        private void UpdateVoice()
        {
            if (View == null || Communications == null) { StopVoice(); return; }
            EnsureVoiceClient();
            voiceClient.Configure(View, Communications, active && appFocused && !appPaused && IsConnected,
                Mathf.Max(0, Time.realtimeSinceStartup - communicationReceivedAt) + communicationTransitEstimate);
        }
        private void StopVoice() { if (voiceClient != null) voiceClient.Close(); }
        private void EnsureCommunicationLoop()
        {
            if (communicationPolling || !active || !HasSession) return;
            communicationPolling = true;
            StartCoroutine(CommunicationLoop());
        }
        private IEnumerator CommunicationLoop()
        {
            while (active && HasSession)
            {
                if (appFocused && !appPaused && Time.unscaledTime >= nextCommunicationPoll)
                {
                    var current = generation;
                    yield return Request("GET", RoomPath + "/communications", null, true, current, payload =>
                    {
                        var value = Parse<CommunicationView>(payload);
                        if (value != null) ApplyCommunications(value);
                    }, (message, status) => { StopVoice(); });
                    var selected = openChannel;
                    var selectionGeneration = channelGeneration;
                    if (HasSession && selected != null && Communications?.channels.Any(c => c.id == selected && c.canRead) == true)
                    {
                        yield return Request("GET", RoomPath + "/messages/" + Uri.EscapeDataString(selected) + "?after=" + chatCursor,
                            null, true, current, payload =>
                            {
                                if (selectionGeneration != channelGeneration || selected != openChannel) return;
                                var page = Parse<ChatPage>(payload);
                                if (page == null || page.matchId != Communications?.matchId) return;
                                AddMessages(page.messages); chatCursor = page.cursor;
                                CommunicationChanged?.Invoke();
                            }, (message, status) =>
                            {
                                if (selectionGeneration == channelGeneration) { ChatError = message; CommunicationChanged?.Invoke(); }
                            });
                    }
                    nextCommunicationPoll = Time.unscaledTime + .8f;
                }
                yield return null;
            }
            communicationPolling = false;
        }
        private void ApplyCommunications(CommunicationView value)
        {
            if (View != null && value.turn < View.turn) return;
            if (Communications != null && Communications.matchId != value.matchId)
            {
                Messages = Array.Empty<ChatMessage>(); chatCursor = 0; pendingMessages.Clear(); IsChatBusy = false;
                channelGeneration++;
            }
            if (View != null && value.turn != View.turn) { StopVoice(); SecretsMustHide?.Invoke(); Refresh(); }
            Communications = value;
            communicationReceivedAt = Time.realtimeSinceStartup;
            // A full round trip is deliberately conservative: local capture closes early rather than late.
            communicationTransitEstimate = lastRequestDuration;
            if (openChannel != null && !value.channels.Any(c => c.id == openChannel && c.canRead))
            { openChannel = null; Messages = Array.Empty<ChatMessage>(); chatCursor = 0; channelGeneration++; SecretsMustHide?.Invoke(); }
            UpdateVoice();
            CommunicationChanged?.Invoke();
        }
        private void AddMessages(ChatMessage[] values)
        {
            foreach (var message in values ?? Array.Empty<ChatMessage>())
                if (message.authorPlayerId == playerId && pendingMessages.TryGetValue(message.channelId, out var pending)
                    && message.clientMessageId == pending.clientMessageId)
                    pendingMessages.Remove(message.channelId);
            Messages = Messages.Concat(values ?? Array.Empty<ChatMessage>()).GroupBy(m => m.id).Select(g => g.Last())
                .OrderBy(m => m.sequence).Reverse().Take(200).Reverse().ToArray();
        }
        private void StopCommunications(bool forget = false)
        {
            StopVoice(); channelGeneration++;
            if (!forget) return;
            Communications = null; Messages = Array.Empty<ChatMessage>(); pendingMessages.Clear();
            openChannel = null; chatCursor = 0; ChatError = null; IsChatBusy = false;
            CommunicationChanged?.Invoke();
        }
        internal string VoicePath => RoomPath + "/voice";
        internal IEnumerator VoiceTransport(string method, string path, string body, Action<string> success, Action<string, long> failure)
            => Request(method, path, body, true, generation, success, failure);
    }
}
