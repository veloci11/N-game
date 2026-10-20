using System;
using System.Collections;
using System.Collections.Generic;
using NGame.Core.Multiplayer;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace NGame.Presentation.Game
{
    /// <summary>Real native microphone/audio transport. PCM is ephemeral and accepted only in the current remote daytime vote.</summary>
    public sealed class NativeVoiceClient : MonoBehaviour
    {
        private UnityRoomClient owner;
        private string match;
        private int turn = -1;
        private int epoch;
        private long cursor;
        private float nextPoll;
        private float deadline;
        private bool foreground;
        private bool pollInFlight;
        private bool sendInFlight;
        private bool permissionPending;
        private AudioClip microphone;
        private int readPosition;
        private readonly float[] samples = new float[RoomVoiceRelay.SamplesPerChunk];
        private readonly Dictionary<string, Speaker> speakers = new Dictionary<string, Speaker>();
        public VoiceView View { get; private set; } = new VoiceView { reason = "Le vocal s’ouvre uniquement pendant le vote de jour." };
        public event Action Changed;

        private sealed class Speaker
        {
            public AudioSource Source;
            public AudioClip Clip;
            private readonly float[] ring = new float[RoomVoiceRelay.SampleRate];
            private readonly object gate = new object();
            private int read;
            private int count;
            public void Push(byte[] pcm)
            {
                lock (gate)
                {
                    var incoming = pcm.Length / 2;
                    if (count + incoming > ring.Length)
                    { var discard = count + incoming - ring.Length; read = (read + discard) % ring.Length; count -= discard; }
                    for (var i = 0; i < incoming; i++)
                    { var sample = (short)(pcm[i * 2] | (pcm[i * 2 + 1] << 8)); ring[(read + count++) % ring.Length] = sample / 32768f; }
                }
            }
            public void Read(float[] output)
            {
                lock (gate)
                {
                    for (var i = 0; i < output.Length; i++)
                    {
                        output[i] = count == 0 ? 0 : ring[read];
                        if (count > 0) { read = (read + 1) % ring.Length; count--; }
                    }
                }
            }
            public void Clear() { lock (gate) { read = count = 0; Array.Clear(ring, 0, ring.Length); } }
        }

        internal void Initialize(UnityRoomClient value) { owner = value; }
        internal void Configure(RoomView room, CommunicationView communications, bool isForeground, float communicationAgeSeconds = 0)
        {
            var allowed = communications.voice;
            var same = room != null && allowed != null && room.matchId == allowed.matchId && room.turn == allowed.turn;
            foreground = isForeground;
#if UNITY_WEBGL && !UNITY_EDITOR
            Close();
            View = new VoiceView { matchId = room?.matchId, turn = room == null ? 0 : room.turn,
                reason = "Le vocal natif n’est pas disponible dans ce navigateur. Le chat écrit reste utilisable." };
            return;
#else
            if (!same || !isForeground || !allowed.canListen)
            {
                Close();
                if (allowed != null) View = new VoiceView { provider = allowed.provider, matchId = allowed.matchId,
                    turn = allowed.turn, policyEpoch = allowed.policyEpoch, reason = !isForeground ? "Micro coupé pendant l’interruption." : allowed.reason };
                return;
            }
            var estimatedDeadline = Time.realtimeSinceStartup + Mathf.Max(0,
                (allowed.closesAtUnixMs - communications.serverTimeUnixMs) / 1000f - communicationAgeSeconds);
            if (match != allowed.matchId || turn != allowed.turn)
            {
                Close(); match = allowed.matchId; turn = allowed.turn; cursor = 0;
                View = new VoiceView { provider = allowed.provider, matchId = match, turn = turn, policyEpoch = allowed.policyEpoch,
                    available = true, canListen = true, canTransmit = allowed.canTransmit, reason = allowed.reason, closesAtUnixMs = allowed.closesAtUnixMs };
                nextPoll = 0;
                deadline = estimatedDeadline;
            }
            else deadline = Mathf.Min(deadline, estimatedDeadline);
            View.canTransmit = allowed.canTransmit;
            if (!allowed.canTransmit && View.microphoneActive) StopMicrophone();
#endif
        }

        public void SetMicrophone(bool enabled)
        {
            if (!enabled)
            {
                if (permissionPending) { epoch++; StopAllCoroutines(); permissionPending = false; pollInFlight = sendInFlight = false; }
                StopMicrophone(); Changed?.Invoke(); return;
            }
            if (!View.available || !View.canTransmit || !foreground || permissionPending || View.microphoneActive) return;
            StartCoroutine(StartMicrophone(epoch));
        }
        private IEnumerator StartMicrophone(int expectedEpoch)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            yield break;
#else
            permissionPending = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                var completed = false;
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => completed = true;
                callbacks.PermissionDenied += _ => completed = true;
                callbacks.PermissionDeniedAndDontAskAgain += _ => completed = true;
                Permission.RequestUserPermission(Permission.Microphone, callbacks);
                while (!completed && expectedEpoch == epoch) yield return null;
            }
            var permitted = Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
                yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            var permitted = Application.HasUserAuthorization(UserAuthorization.Microphone);
#endif
            permissionPending = false;
            if (expectedEpoch != epoch || !foreground || !View.canTransmit || !View.available || Time.realtimeSinceStartup >= deadline) yield break;
            if (!permitted) { View.reason = "Permission micro refusée. Tu peux continuer en texte."; Changed?.Invoke(); yield break; }
            try
            {
                if (Microphone.devices.Length == 0) { View.reason = "Aucun microphone détecté. Le texte reste disponible."; Changed?.Invoke(); yield break; }
                microphone = Microphone.Start(null, true, 1, RoomVoiceRelay.SampleRate);
                if (microphone == null || microphone.frequency != RoomVoiceRelay.SampleRate || microphone.channels != 1)
                { StopMicrophone(); View.reason = "Ce format de microphone n’est pas pris en charge. Continue en texte."; Changed?.Invoke(); yield break; }
                readPosition = 0;
                View.microphoneActive = true; View.reason = "Ton micro est ouvert pendant ce vote.";
                Changed?.Invoke();
            }
            catch (Exception) { StopMicrophone(); View.reason = "Le microphone n’a pas pu démarrer. Continue en texte."; Changed?.Invoke(); }
#endif
        }

        private void Update()
        {
            if (owner == null || !View.available || !foreground) return;
            if (Time.realtimeSinceStartup >= deadline) { Close(); Changed?.Invoke(); return; }
            if (!pollInFlight && Time.unscaledTime >= nextPoll)
            {
                pollInFlight = true; nextPoll = Time.unscaledTime + .18f;
                StartCoroutine(Poll(epoch));
            }
#if !UNITY_WEBGL || UNITY_EDITOR
            if (View.microphoneActive && microphone != null)
            {
                var position = Microphone.GetPosition(null);
                var available = (position - readPosition + microphone.samples) % microphone.samples;
                if (available >= RoomVoiceRelay.SamplesPerChunk)
                {
                    // Drop old chunks instead of queuing seconds of late speech on a slow connection.
                    if (available >= RoomVoiceRelay.SamplesPerChunk * 2)
                        readPosition = (position - RoomVoiceRelay.SamplesPerChunk + microphone.samples) % microphone.samples;
                    microphone.GetData(samples, readPosition);
                    readPosition = (readPosition + RoomVoiceRelay.SamplesPerChunk) % microphone.samples;
                    if (!sendInFlight)
                    {
                        var bytes = new byte[samples.Length * 2];
                        for (var i = 0; i < samples.Length; i++)
                        { var value = (short)Mathf.RoundToInt(Mathf.Clamp(samples[i], -1, 1) * 32767); bytes[i * 2] = (byte)value; bytes[i * 2 + 1] = (byte)(value >> 8); }
                        var request = new VoiceChunkRequest { matchId = match, turn = turn, policyEpoch = View.policyEpoch,
                            chunkId = Guid.NewGuid().ToString("N"), pcmBase64 = Convert.ToBase64String(bytes) };
                        sendInFlight = true;
                        StartCoroutine(Send(request, epoch));
                    }
                }
            }
#endif
        }
        private IEnumerator Poll(int expectedEpoch)
        {
            var path = owner.VoicePath + "?matchId=" + Uri.EscapeDataString(match) + "&turn=" + turn + "&after=" + cursor;
            yield return owner.VoiceTransport("GET", path, null, payload =>
            {
                if (expectedEpoch != epoch || !foreground) return;
                var result = JsonUtility.FromJson<VoicePollResult>(payload);
                if (result?.voice == null || result.voice.matchId != match || result.voice.turn != turn || !result.voice.canListen)
                { Close(); return; }
                cursor = result.cursor;
                foreach (var packet in result.packets ?? Array.Empty<VoicePacket>())
                {
                    if (result.serverTimeUnixMs - packet.sentAtUnixMs > 1200) continue;
                    try { Receive(packet); } catch (FormatException) { }
                }
            }, (message, status) => { if (expectedEpoch == epoch) TransportFailed(message); });
            if (expectedEpoch == epoch) pollInFlight = false;
        }
        private IEnumerator Send(VoiceChunkRequest request, int expectedEpoch)
        {
            yield return owner.VoiceTransport("POST", owner.VoicePath, JsonUtility.ToJson(request), _ => { },
                (message, status) => { if (expectedEpoch == epoch) TransportFailed(message); });
            if (expectedEpoch == epoch) sendInFlight = false;
        }
        private void Receive(VoicePacket packet)
        {
            var pcm = Convert.FromBase64String(packet.pcmBase64);
            if (pcm.Length != RoomVoiceRelay.SamplesPerChunk * 2) return;
            if (!speakers.TryGetValue(packet.authorPlayerId, out var speaker))
            {
                if (speakers.Count >= 24) return;
                speaker = new Speaker();
                speaker.Clip = AudioClip.Create("Vote voice", RoomVoiceRelay.SampleRate, 1, RoomVoiceRelay.SampleRate, true, speaker.Read);
                speaker.Source = gameObject.AddComponent<AudioSource>();
                speaker.Source.clip = speaker.Clip; speaker.Source.loop = true; speaker.Source.spatialBlend = 0; speaker.Source.playOnAwake = false;
                speaker.Source.volume = .85f; speaker.Source.ignoreListenerPause = true;
                speakers.Add(packet.authorPlayerId, speaker);
                speaker.Push(pcm); speaker.Source.Play();
            }
            else speaker.Push(pcm);
        }
        private void TransportFailed(string message)
        { Close(); View.reason = "Vocal interrompu. Le chat écrit et le vote restent disponibles."; Changed?.Invoke(); }
        private void StopMicrophone()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (microphone != null) { Microphone.End(null); Destroy(microphone); microphone = null; }
#endif
            if (View != null) { View.microphoneActive = false; View.reason = "Micro coupé."; }
        }
        public void Close()
        {
            epoch++; permissionPending = false;
            StopAllCoroutines();
            StopMicrophone();
            foreach (var speaker in speakers.Values)
            { speaker.Clear(); if (speaker.Source != null) { speaker.Source.Stop(); Destroy(speaker.Source); } if (speaker.Clip != null) Destroy(speaker.Clip); }
            speakers.Clear(); pollInFlight = sendInFlight = false; cursor = 0; match = null; turn = -1;
            if (View != null) { View.available = false; View.canListen = false; View.canTransmit = false; }
        }
        private void OnApplicationFocus(bool focused) { if (!focused) { foreground = false; Close(); Changed?.Invoke(); } }
        private void OnApplicationPause(bool paused) { if (paused) { foreground = false; Close(); Changed?.Invoke(); } }
        private void OnDisable() { foreground = false; Close(); }
        private void OnDestroy() { Close(); }
    }
}
