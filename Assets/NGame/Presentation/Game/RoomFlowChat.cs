using System;
using System.Collections.Generic;
using NGame.Core.Multiplayer;
using TMPro;
using UnityEngine;

namespace NGame.Presentation.Game
{
    public sealed partial class RoomFlowView
    {
        // Bounded message dock; voting resumes with its choices intact after keyboard entry.
        public bool IsChatOpen => chatPanel != null && chatPanel.gameObject.activeSelf;
        public TMP_InputField ChatInput { get; private set; }
        public float ChatKeyboardInset { get; private set; }
        public float ChatDockHeight => chatPanel != null ? chatPanel.rect.height : 0;
        public float ChatDockTop => chatPanel != null ? chatPanel.anchoredPosition.y + chatPanel.rect.height * (1 - chatPanel.pivot.y) : 270;
        private float AvailableChatHeight => Mathf.Clamp(((RectTransform)transform).rect.height - 692, 140, 230);
        private bool ChatAllowed => Client?.View?.phase == "vote" && Client.HasSession && communications != null;
        private IRoomCommunicationClient communications;
        private UnityEngine.UI.Button chatAccess, sendChat, microphoneButton;
        private RectTransform chatPanel, chatHistory, chatChannels, chatComposer, chatHistoryShell;
        private TextMeshProUGUI chatStatus, voiceStatus;
        private string activeChannel, draftMatch, pendingChatText, pendingChatChannel;
        private long pendingAfterSequence, drawnLastSequence = -1;
        private string drawnChatChannel, drawnChannelOptions;
        private readonly Dictionary<string, string> drafts = new Dictionary<string, string>();

        private void BuildChatAccess()
        {
            chatAccess = UITheme.Action("Open room chat", transform, "Messages", () => { if (IsChatOpen) CloseChatPanel(); else OpenChatPanel(); }, false);
            UITheme.Position((RectTransform)chatAccess.transform, UITheme.Bottom, 0, 247, 382, 36);
            chatAccess.GetComponent<UnityEngine.UI.Image>().color = new Color(.025f, .05f, .075f, .72f);
            chatAccess.GetComponentInChildren<TMP_Text>().fontSize = 14;
            chatAccess.gameObject.SetActive(false);
            microphoneButton = UITheme.Action("Remote day vote microphone", transform, "Activer micro",
                () => communications?.SetMicrophone(communications.Voice?.microphoneActive != true), false);
            UITheme.Position((RectTransform)microphoneButton.transform, UITheme.Top, 114, -213, 144, 30);
            var microphoneTouch = UITheme.Surface("Microphone generous touch target", microphoneButton.transform, Color.clear, null, true);
            microphoneTouch.raycastTarget = true;
            microphoneTouch.rectTransform.offsetMin = new Vector2(0, -7);
            microphoneTouch.rectTransform.offsetMax = new Vector2(0, 7);
            microphoneTouch.transform.SetAsFirstSibling();
            microphoneButton.GetComponentInChildren<TMP_Text>().fontSize = 14;
            microphoneButton.gameObject.SetActive(false);
        }
        private void BindCommunications()
        {
            communications = Client as IRoomCommunicationClient;
            if (communications != null) communications.CommunicationChanged += CommunicationChanged;
        }
        private void UnbindCommunications()
        {
            if (communications != null) communications.CommunicationChanged -= CommunicationChanged;
            communications = null;
        }
        private void CommunicationChanged()
        {
            RefreshChatAccess();
            if (IsChatOpen) RefreshChatPanel();
            UpdateVoiceControl();
        }
        private void RefreshChatAccess()
        {
            if (chatAccess == null) return;
            if (!ChatAllowed && IsChatOpen) CloseChatPanel();
            chatAccess.gameObject.SetActive(ChatAllowed && !externalBlock && !IsMenuOpen);
            chatAccess.GetComponentInChildren<TMP_Text>().text = IsChatOpen ? "Messages · Réduire" : "Messages";
            chatAccess.interactable = Client?.IsConnected == true && !externalBlock && !IsMenuOpen;
            UpdateVoiceControl();
        }
        private string DraftKey(string channel) => (Client?.View?.matchId ?? "lobby") + ":" + channel;
        private void SaveDraft()
        { if (ChatInput != null && !string.IsNullOrEmpty(activeChannel)) drafts[DraftKey(activeChannel)] = ChatInput.text; }
        private void ResetChatDrafts()
        {
            // Closing saves the old field; purge only afterwards so no draft survives under the next match id.
            if (IsChatOpen) CloseChatPanel();
            drafts.Clear(); pendingChatText = pendingChatChannel = null; draftMatch = Client?.View?.matchId;
        }
        private void MaskChatForInterruption()
        { if (IsChatOpen) CloseChatPanel(); }
        public void OpenChatPanel()
        {
            if (!ChatAllowed || !Client.IsConnected || externalBlock || IsMenuOpen || IsChatOpen) return;
            ForceMaskSecrets(); CloseMenu();
            var channels = communications.Communications?.channels ?? Array.Empty<ChannelView>();
            ChannelView first = null;
            foreach (var channel in channels) if (channel.canRead && (first == null || channel.id == "village")) first = channel;
            if (first == null) { error.text = "Messages indisponibles pendant ce vote."; return; }
            chatPanel = UITheme.Box("Room conversation panel", transform);
            var dockHeight = AvailableChatHeight;
            UITheme.Position(chatPanel, UITheme.Bottom, 0, 270 + dockHeight * .5f, 382, dockHeight);
            var backdrop = UITheme.Surface("Conversation dock background", chatPanel, new Color(.025f, .05f, .075f, .76f), UITheme.Panel, true);
            backdrop.raycastTarget = true;
            chatChannels = UITheme.Box("Conversation channel choices", chatPanel);
            UITheme.Position(chatChannels, UITheme.Top, 0, -15, 354, 24);
            var row = chatChannels.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            row.spacing = 8; row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = true; row.childForceExpandHeight = false;
            chatHistory = GameUI.Scroll(chatPanel, "Conversation history", 32, 68);
            chatHistoryShell = (RectTransform)chatHistory.parent.parent;
            chatHistoryShell.offsetMin = new Vector2(10, 68);
            chatHistoryShell.offsetMax = new Vector2(-10, -32);
            var historyPaper = chatHistoryShell.GetComponent<UnityEngine.UI.ScrollRect>().viewport.GetComponent<UnityEngine.UI.Image>();
            if (historyPaper != null) historyPaper.color = Color.clear;
            var historyLayout = chatHistory.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            historyLayout.padding = new RectOffset(8, 8, 4, 4); historyLayout.spacing = 3;
            chatStatus = UITheme.Copy("Conversation status", chatPanel, "", 12, UITheme.Brass, false, TextAlignmentOptions.MidlineLeft);
            UITheme.Position(chatStatus.rectTransform, UITheme.Bottom, 0, 57, 354, 14);
            chatStatus.enableAutoSizing = true; chatStatus.fontSizeMin = 10; chatStatus.fontSizeMax = 12;
            chatStatus.overflowMode = TextOverflowModes.Truncate;
            chatComposer = UITheme.Box("Conversation keyboard composer", chatPanel);
            UITheme.Position(chatComposer, UITheme.Bottom, 0, 26, 354, 44);
            ChatInput = GameUI.Input(chatComposer, "Conversation message", "Ton message…", 500);
            UITheme.Position((RectTransform)ChatInput.transform, UITheme.Center, -42, 0, 270, 44);
            ChatInput.lineType = TMP_InputField.LineType.SingleLine;
            ChatInput.shouldHideMobileInput = true;
            ChatInput.textComponent.fontSize = 16;
            ChatInput.onValueChanged.AddListener(value => { if (!string.IsNullOrEmpty(activeChannel)) drafts[DraftKey(activeChannel)] = value; });
            ChatInput.onSubmit.AddListener(_ => SubmitChatMessage());
            sendChat = UITheme.Action("Send current channel message", chatComposer, "Envoyer", SubmitChatMessage);
            UITheme.Position((RectTransform)sendChat.transform, UITheme.Center, 139, 0, 76, 44);
            sendChat.GetComponentInChildren<TMP_Text>().fontSize = 14;
            draftMatch = Client.View.matchId; SecretCard.SetBlocked(true);
            RefreshChatAccess();
            SelectChatChannel(first.id);
            if (Client.View != null) RefreshChatLayout();
        }
        private void SelectChatChannel(string id)
        {
            if (!IsChatOpen || communications == null) return;
            if (!ChatAllowed) { CloseChatPanel(); return; }
            SaveDraft();
            ChannelView target = null;
            foreach (var channel in communications.Communications?.channels ?? Array.Empty<ChannelView>()) if (channel.id == id && channel.canRead) target = channel;
            if (target == null) return;
            activeChannel = id; drawnLastSequence = -1; drawnChatChannel = null;
            ChatInput.SetTextWithoutNotify(drafts.TryGetValue(DraftKey(id), out var draft) ? draft : "");
            communications.OpenChannel(id); RefreshChatPanel();
        }
        private void RefreshChatPanel()
        {
            if (!IsChatOpen || communications == null) return;
            if (!ChatAllowed) { CloseChatPanel(); return; }
            var view = Client?.View;
            if (view == null || view.matchId != draftMatch) { ResetChatDrafts(); return; }
            ChannelView selected = null;
            foreach (var channel in communications.Communications?.channels ?? Array.Empty<ChannelView>()) if (channel.id == activeChannel && channel.canRead) selected = channel;
            if (selected == null) { CloseChatPanel(); return; }
            var options = activeChannel;
            foreach (var channel in communications.Communications?.channels ?? Array.Empty<ChannelView>())
                if (channel.canRead) options += "|" + channel.id + ":" + channel.name + ":" + channel.isPrivate;
            if (options != drawnChannelOptions)
            {
                GameUI.Clear(chatChannels);
                foreach (var channel in communications.Communications?.channels ?? Array.Empty<ChannelView>())
                {
                    if (!channel.canRead) continue;
                    var id = channel.id;
                    var button = GameUI.Button(chatChannels, "Conversation channel " + id, (channel.isPrivate ? "Privé · " : "") + channel.name, () => SelectChatChannel(id), id == activeChannel, 24);
                    button.GetComponentInChildren<TMP_Text>().fontSize = 13;
                }
                drawnChannelOptions = options;
            }
            var messages = communications.Messages ?? Array.Empty<ChatMessage>();
            var lastSequence = messages.Length > 0 ? messages[messages.Length - 1].sequence : 0;
            if (lastSequence != drawnLastSequence || drawnChatChannel != activeChannel)
            {
                var scroll = chatHistoryShell.GetComponent<UnityEngine.UI.ScrollRect>();
                var position = scroll.verticalNormalizedPosition;
                var follow = drawnChatChannel != activeChannel || position <= .04f;
                GameUI.Clear(chatHistory);
                if (messages.Length == 0)
                { var empty = GameUI.Text(chatHistory, "Conversation empty state", "Aucun message pour ce vote.", 15, 40, false, UITheme.Quiet); empty.alignment = TextAlignmentOptions.TopLeft; }
                foreach (var message in messages)
                {
                    if (message.channelId != activeChannel || message.matchId != view.matchId) continue;
                    var author = GameUI.Text(chatHistory, "Conversation author", message.authorName + (message.authorPlayerId == view.selfPlayerId ? " (toi)" : ""), 13, 20, false, UITheme.Brass);
                    author.alignment = TextAlignmentOptions.TopLeft; author.fontStyle = FontStyles.Bold;
                    var bodyText = GameUI.Text(chatHistory, "Conversation plain message", message.text, 16, 28, false);
                    bodyText.alignment = TextAlignmentOptions.TopLeft;
                    if (pendingChatText != null && pendingChatChannel == activeChannel && message.sequence > pendingAfterSequence
                        && message.authorPlayerId == view.selfPlayerId && message.text == pendingChatText)
                    {
                        if (ChatInput.text.Trim() == pendingChatText) { ChatInput.SetTextWithoutNotify(""); drafts[DraftKey(activeChannel)] = ""; }
                        pendingChatText = pendingChatChannel = null;
                    }
                }
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = follow ? 0 : position;
                drawnLastSequence = lastSequence; drawnChatChannel = activeChannel;
            }
            ChatInput.interactable = selected.canSend && !communications.IsChatBusy && Client.IsConnected;
            sendChat.interactable = selected.canSend && !communications.IsChatBusy && Client.IsConnected;
            chatStatus.text = !string.IsNullOrEmpty(communications.ChatError) ? communications.ChatError : communications.IsChatBusy ? "Envoi en cours…"
                : !selected.canSend ? "Lecture seule" : selected.isPrivate ? "Canal privé" : "";
        }
        public void SubmitChatMessage()
        {
            if (!ChatAllowed || !IsChatOpen || communications == null || !Client.IsConnected || communications.IsChatBusy || string.IsNullOrWhiteSpace(ChatInput.text)) return;
            var permitted = false;
            foreach (var channel in communications.Communications?.channels ?? Array.Empty<ChannelView>()) if (channel.id == activeChannel && channel.canSend) permitted = true;
            if (!permitted) return;
            pendingChatText = ChatInput.text.Trim(); pendingChatChannel = activeChannel; pendingAfterSequence = 0;
            foreach (var message in communications.Messages ?? Array.Empty<ChatMessage>()) pendingAfterSequence = Math.Max(pendingAfterSequence, message.sequence);
            communications.SendChat(activeChannel, pendingChatText);
            if (ChatInput != null) ChatInput.DeactivateInputField();
            if (chatComposer != null) GameUI.ClearSelection(chatComposer);
        }
        public void CloseChatPanel()
        {
            SaveDraft();
            if (chatPanel != null) GameUI.ClearSelection(chatPanel);
            if (ChatInput != null) ChatInput.DeactivateInputField();
            if (chatPanel != null) { chatPanel.gameObject.SetActive(false); Destroy(chatPanel.gameObject); chatPanel = null; }
            ChatKeyboardInset = 0;
            ChatInput = null; activeChannel = null; drawnChatChannel = null; drawnLastSequence = -1; drawnChannelOptions = null;
            communications?.CloseChannel(); SecretCard?.SetBlocked(externalBlock || IsMenuOpen);
            if (chatAccess != null) chatAccess.GetComponentInChildren<TMP_Text>().text = "Messages";
            if (built && Client?.View != null) RefreshChatLayout();
        }
        private void UpdateChatKeyboard()
        {
            if (!ChatAllowed) { if (IsChatOpen) CloseChatPanel(); if (chatAccess != null) chatAccess.gameObject.SetActive(false); return; }
            if (!IsChatOpen || chatComposer == null || chatHistoryShell == null) return;
            var root = (RectTransform)transform;
            var previousKeyboardInset = ChatKeyboardInset;
            ChatKeyboardInset = TouchScreenKeyboard.visible && Screen.height > 0
                ? TouchScreenKeyboard.area.height / Screen.height * root.rect.height : 0;
            // Reserve the actual dock top when the keyboard raises it; keep vote selections intact.
            var bottom = Mathf.Max(270, ChatKeyboardInset + 8);
            var height = Mathf.Clamp(root.rect.height - bottom - 235, 140, AvailableChatHeight);
            var layoutChanged = Mathf.Abs(previousKeyboardInset - ChatKeyboardInset) > .5f
                || Mathf.Abs(chatPanel.rect.height - height) > .5f
                || Mathf.Abs(chatPanel.anchoredPosition.y - bottom - height * .5f) > .5f;
            UITheme.Position(chatPanel, UITheme.Bottom, 0, bottom + height * .5f, 382, height);
            if (layoutChanged) RefreshChatLayout();
            if (sendChat != null && communications != null)
            {
                var canSend = false;
                foreach (var channel in communications.Communications?.channels ?? Array.Empty<ChannelView>()) if (channel.id == activeChannel) canSend = channel.canSend;
                sendChat.interactable = canSend && !communications.IsChatBusy && Client.IsConnected && !string.IsNullOrWhiteSpace(ChatInput.text);
            }
        }
        private void RenderVoiceControl(Transform parent)
        {
            voiceStatus = GameUI.Text(parent, "Remote voice availability", "", 12, 65, false, UITheme.Quiet);
            UpdateVoiceControl();
        }
        private void UpdateVoiceControl()
        {
            if (microphoneButton == null) return;
            var room = Client?.View;
            var show = room != null && room.mode == "remote" && room.phase == "vote" && Client.IsConnected
                && !externalBlock && !IsMenuOpen && string.IsNullOrEmpty(Client.StatusError);
            microphoneButton.gameObject.SetActive(show);
            if (clock != null) UITheme.Position(clock.rectTransform, UITheme.Top, show ? -78 : 0, -213, show ? 220 : 370, 26);
            var voice = communications?.Voice;
            microphoneButton.GetComponentInChildren<TMP_Text>().text = voice?.microphoneActive == true ? "Couper micro"
                : voice?.available != true ? "Micro indispo." : !voice.canTransmit ? "Écoute seule" : "Activer micro";
            microphoneButton.interactable = show && voice != null && voice.available && voice.canTransmit;
            if (voiceStatus != null) voiceStatus.text = voice?.reason ?? "Le vocal n’est pas disponible sur cette plateforme. Le chat écrit reste utilisable.";
        }
    }
}
