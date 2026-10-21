using System;
using NGame.Core;
using NGame.Core.Multiplayer;
using NGame.Presentation.Game;
using UnityEngine;

namespace NGame.Presentation
{
    /// <summary>Public phase calls from the host's phone only; never reads assignments, names or private facts.</summary>
    public sealed class PublicNarrator : MonoBehaviour
    {
        private AppPreferences preferences;
        private Func<IRoomClient> clientSource;
        private Func<bool> routeVisible;
        private AudioSource speaker;
        private string observedKey;
        private RoomView observedView;
        private float observedAt;
        private bool focused = true;
        private bool paused;
        public string CurrentPublicCall { get; private set; }
        public bool IsSpeaking => speaker != null && speaker.isPlaying;

        public void Initialize(AppPreferences settings, Func<IRoomClient> client, Func<bool> visible = null)
        {
            preferences = settings;
            clientSource = client;
            routeVisible = visible;
            speaker = gameObject.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.loop = false;
            speaker.spatialBlend = 0;
            speaker.volume = .7f;
            preferences.Changed += PreferencesChanged;
        }

        private void Update()
        {
            if (speaker == null || preferences == null || clientSource == null) { StopSpeech(); return; }
            var client = clientSource?.Invoke();
            var view = client?.View;
            if (!ReferenceEquals(view, observedView)) { observedView = view; observedAt = Time.realtimeSinceStartup; }
            var key = view == null ? null : view.matchId + ":" + view.turn + ":" + view.phase + ":" + view.nightStep + ":" + view.hostPlayerId;
            var changed = key != observedKey;
            if (changed) { observedKey = key; StopSpeech(); }
            var eligible = focused && !paused && preferences.PublicNarration && (routeVisible?.Invoke() ?? true)
                && client != null && !client.IsTraining && client.IsConnected && view != null && view.isHost && view.mode == "in-person"
                // Shipped night clips explicitly say "Yeux fermés" and belong only to the traditional protocol.
                // Daytime announcements remain available for both protocols.
                && !(view.phase == "night" && view.settings?.nightProtocol == "screen");
            if (!eligible) { StopSpeech(); return; }
            if (!changed) return;
            var call = view.phase == "night" ? view.nightStep : view.phase == "reaction" ? "hunter" : view.phase;
            if (call == "werewolf") call = "wolves";
            if (call == "lobby" || string.IsNullOrEmpty(call)) return;
            var clip = Resources.Load<AudioClip>("Werewolf/Narration/" + call);
            if (clip == null) return;
            var remaining = view.deadlineUnixMs - view.serverTimeUnixMs - (Time.realtimeSinceStartup - observedAt) * 1000;
            if (view.deadlineUnixMs > 0 && view.serverTimeUnixMs > 0 && remaining < clip.length * 1000 + 100) return;
            CurrentPublicCall = call;
            speaker.clip = clip;
            speaker.Play();
        }

        private void PreferencesChanged() { if (!preferences.PublicNarration) StopSpeech(); }
        private void StopSpeech()
        {
            if (speaker != null && (speaker.clip != null || speaker.isPlaying)) { speaker.Stop(); speaker.clip = null; }
            CurrentPublicCall = null;
        }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) StopSpeech(); }
        private void OnApplicationPause(bool value) { paused = value; if (value) StopSpeech(); }
        private void OnDisable() => StopSpeech();
        private void OnDestroy() { if (preferences != null) preferences.Changed -= PreferencesChanged; }
    }
}
