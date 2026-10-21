using System;
using System.Collections;
using NGame.Core;
using NGame.Core.Multiplayer;
using NGame.Presentation.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NGame.Presentation.Tests
{
    /// <summary>Checks public-call selection and actual AudioSource lifecycle, muted. No microphone or acoustic/device claim.</summary>
    public sealed class PublicNarratorTests
    {
        private sealed class MemoryPreferences : IPreferenceStore
        {
            public bool ReadBool(string key, bool fallback) => fallback;
            public void WriteBool(string key, bool value) { }
            public void Save() { }
        }

        private sealed class FixtureClient : IRoomClient
        {
            public RoomView View { get; set; }
            public string RoomCode => View?.code;
            public bool HasSession => View != null;
            public bool IsBusy => false;
            public bool IsConnected { get; set; } = true;
            public bool IsTraining { get; set; }
            public string StatusError => null;
            public event Action Changed;
            public event Action SecretsMustHide;
            public void CreateRoom(string name, int avatar, RoomSettings settings) { }
            public void JoinRoom(string code, string name, int avatar) { }
            public void Send(RoomCommand command) { }
            public void Refresh() => Changed?.Invoke();
            public void SetActive(bool active) { if (!active) SecretsMustHide?.Invoke(); }
            public void ForgetRoom() { View = null; Changed?.Invoke(); }
            public void RetryPending() { }
        }

        private GameObject owner;
        private PublicNarrator narrator;
        private AudioSource speaker;
        private FixtureClient client;
        private AppPreferences preferences;
        private bool visible;
        private int nextTurn;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            owner = new GameObject("Muted public narration fixture");
            client = new FixtureClient();
            preferences = new AppPreferences(new MemoryPreferences());
            preferences.SetPublicNarration(true);
            visible = true;
            narrator = owner.AddComponent<PublicNarrator>();
            narrator.Initialize(preferences, () => client, () => visible);
            speaker = owner.GetComponent<AudioSource>();
            speaker.mute = true;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (owner != null) UnityEngine.Object.Destroy(owner);
            yield return null;
        }

        private RoomView PublicView(string phase = "night", string step = "seer")
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return new RoomView
            {
                code = "NARRAT", matchId = "public-narration-fixture", turn = ++nextTurn,
                phase = phase, nightStep = step, mode = "in-person", isHost = true,
                selfPlayerId = "host", hostPlayerId = "host", selfRoleId = "villager", selfRoleName = "Villageois",
                serverTimeUnixMs = now, deadlineUnixMs = now + 120000,
                players = new[] { new PlayerView { id = "host", displayName = "Public host", alive = true } }
            };
        }

        private void AssertCall(string call)
        {
            Assert.That(narrator.CurrentPublicCall, Is.EqualTo(call));
            var expected = Resources.Load<AudioClip>("Werewolf/Narration/" + call);
            Assert.That(expected, Is.Not.Null, "A real shipped public-call resource is required for this check.");
            Assert.That(speaker.clip, Is.SameAs(expected));
        }

        private void AssertSilent(string reason)
        {
            Assert.That(narrator.CurrentPublicCall, Is.Null, reason);
            Assert.That(speaker.clip, Is.Null, reason + " The previous clip must be detached, not only visually muted.");
            Assert.That(narrator.IsSpeaking, Is.False, reason);
        }

        [UnityTest]
        public IEnumerator ConnectedVisibleHostInPresenceUsesTheShippedPublicCall()
        {
            client.View = PublicView("night", "werewolf");
            yield return null;
            AssertCall("wolves");
            Assert.That(speaker.loop, Is.False, "A phase announcement must never loop over the action window.");
            Assert.That(speaker.mute, Is.True, "The test remains silent and does not require an acoustic device.");
        }

        [UnityTest]
        public IEnumerator ScreenNightNeverPlaysEyesClosedCallsButKeepsDaytimeHostAnnouncements()
        {
            client.View = PublicView("night", "seer");
            yield return null;
            AssertCall("seer");
            foreach (var step in new[] { "cupid", "lovers", "guard", "seer", "werewolf", "witch" })
            {
                var screen = PublicView("night", step);
                screen.settings = new RoomSettings { nightProtocol = "screen" };
                client.View = screen;
                yield return null;
                AssertSilent("The traditional eyes-closed clip must not contradict screen night: " + step);
            }
            client.View = PublicView("dawn", null);
            client.View.settings = new RoomSettings { nightProtocol = "screen" };
            yield return null;
            AssertCall("dawn");
            client.View = PublicView("vote", null);
            client.View.settings = new RoomSettings { nightProtocol = "screen" };
            yield return null;
            AssertCall("vote");
            client.View = PublicView("vote", null);
            client.View.settings = new RoomSettings { nightProtocol = "screen" };
            client.View.isHost = false;
            yield return null;
            AssertSilent("Screen protocol must not expand narration permissions to a guest.");
            client = null;
            yield return null;
            AssertSilent("A missing client or host view detaches public narration safely.");
        }

        [UnityTest]
        public IEnumerator SwitchingProtocolDuringOneObservedCallStopsAndNeverReplaysThatCall()
        {
            client.View = PublicView("night", "werewolf");
            yield return null;
            AssertCall("wolves");
            client.View.settings = new RoomSettings { nightProtocol = "screen" };
            yield return null;
            AssertSilent("Even an unchanged turn must stop if its protocol no longer permits this clip.");
            client.View.settings.nightProtocol = "traditional";
            yield return null;
            AssertSilent("Restoring eligibility alone cannot restart a call in the middle of its window.");
            client.View = PublicView("night", "guard");
            yield return null;
            AssertCall("guard");
        }

        [UnityTest]
        public IEnumerator GuestRemoteTrainingDisconnectedAndHiddenViewsNeverStartAnAnnouncement()
        {
            client.View = PublicView(); client.View.isHost = false;
            yield return null;
            AssertSilent("An invited player's phone must not duplicate the host's public voice.");

            client.View = PublicView(); client.View.mode = "remote";
            yield return null;
            AssertSilent("Remote player voice and public table narration are different features.");

            client.IsTraining = true; client.View = PublicView();
            yield return null;
            AssertSilent("The solo practice adapter must not announce real table calls.");

            client.IsTraining = false; client.IsConnected = false; client.View = PublicView();
            yield return null;
            AssertSilent("An offline projection cannot start a public announcement.");

            client.IsConnected = true; visible = false; client.View = PublicView();
            yield return null;
            AssertSilent("Leaving the game route keeps its narrator silent.");
        }

        [UnityTest]
        public IEnumerator PublicAnnouncementIgnoresAllEightSecretRolesNamesAndPrivateFacts()
        {
            var roles = new[] { "villager", "werewolf", "seer", "witch", "hunter", "cupid", "guard", "little-girl" };
            foreach (var role in roles)
            {
                var view = PublicView("night", "seer");
                view.selfRoleId = role;
                view.selfRoleName = "PRIVATE ROLE " + role;
                view.players[0].displayName = "PRIVATE NAME " + role;
                view.action = new ActionView { kind = "witch", privateInfo = "PRIVATE TARGET " + role };
                view.privateFacts = new[] { new PrivateFact { key = "private-" + role, roleId = role,
                    playerId = "hidden-player", description = "PRIVATE FACT " + role } };
                client.View = view;
                yield return null;
                AssertCall("seer");
            }
        }

        [UnityTest]
        public IEnumerator DisablingThePreferenceStopsImmediatelyAndDoesNotReplayTheSameTurn()
        {
            client.View = PublicView();
            yield return null;
            AssertCall("seer");
            preferences.SetPublicNarration(false);
            AssertSilent("Disabling public narration stops the current AudioSource immediately.");
            preferences.SetPublicNarration(true);
            yield return null;
            AssertSilent("Re-enabling during the same call must not replay or start it midway.");
            client.View = PublicView("night", "guard");
            yield return null;
            AssertCall("guard");
        }

        [UnityTest]
        public IEnumerator FocusPausePhaseAndHostChangesStopTheCurrentClip()
        {
            client.View = PublicView();
            yield return null;
            AssertCall("seer");
            owner.SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);
            AssertSilent("Focus loss must stop public audio immediately.");
            owner.SendMessage("OnApplicationFocus", true, SendMessageOptions.RequireReceiver);
            yield return null;
            AssertSilent("Focus recovery alone does not replay the old phase.");

            client.View = PublicView("night", "witch");
            yield return null;
            AssertCall("witch");
            owner.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);
            AssertSilent("An OS pause must stop the current clip.");
            owner.SendMessage("OnApplicationPause", false, SendMessageOptions.RequireReceiver);

            client.View = PublicView("vote", null);
            yield return null;
            AssertCall("vote");
            client.View = PublicView("lobby", null);
            yield return null;
            AssertSilent("Returning to the lobby detaches the previous announcement.");

            client.View = PublicView("night", "cupid");
            yield return null;
            AssertCall("cupid");
            client.View = PublicView("night", "cupid"); client.View.isHost = false; client.View.hostPlayerId = "new-host";
            yield return null;
            AssertSilent("Losing the host seat immediately ends this device's public call.");
        }

        [UnityTest]
        public IEnumerator NearlyExpiredOrUnknownPublicPhasesStaySilent()
        {
            var late = PublicView();
            late.deadlineUnixMs = late.serverTimeUnixMs + 50;
            client.View = late;
            yield return null;
            AssertSilent("Do not play a long phase call when its window is already ending.");
            client.View = PublicView("night", "werewolf");
            yield return null;
            AssertCall("wolves");
            client.View = PublicView("unknown-public-phase", null);
            yield return null;
            AssertSilent("An unknown phase must not reuse the preceding phase's clip.");
        }
    }
}
