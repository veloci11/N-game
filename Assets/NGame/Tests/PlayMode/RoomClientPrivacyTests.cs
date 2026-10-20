using System.Collections;
using System.Linq;
using NGame.Core;
using NGame.Core.Multiplayer;
using NGame.Presentation.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NGame.Presentation.Tests
{
    public sealed class RoomClientPrivacyTests
    {
        private sealed class MemoryPreferences : IPreferenceStore
        {
            public bool ReadBool(string key, bool fallback) => fallback;
            public void WriteBool(string key, bool value) { }
            public void Save() { }
        }
        private GameObject owner;
        [TearDown] public void CleanUp() { if (owner != null) Object.DestroyImmediate(owner); }

        [Test]
        public void TrainingStartsWithEightReadyPlayersAndOnlyTheLocalPrivateRole()
        {
            owner = new GameObject("Offline training contract test");
            var practice = owner.AddComponent<PracticeRoomClient>();
            practice.CreateRoom("LÉA", 3, new RoomSettings());
            Assert.That(practice.IsTraining, Is.True);
            Assert.That(practice.HasSession, Is.True);
            Assert.That(practice.View.players.Length, Is.EqualTo(8));
            Assert.That(practice.View.players.All(p => p.ready), Is.True);
            Assert.That(practice.View.canStart, Is.True);
            practice.Send(new RoomCommand { kind = "start", turn = practice.View.turn, commandId = "practice-test-start" });
            Assert.That(practice.StatusError, Is.Null);
            Assert.That(practice.View.phase, Is.EqualTo("distribution"),
                "Players must be able to discover their card before timed night powers begin.");
            Assert.That(RoleCatalog.Find(practice.View.selfRoleId), Is.Not.Null);
            Assert.That(practice.View.players.All(p => p.revealedRoleId == null), Is.True,
                "The simulated room must still use recipient-specific private projections.");
            if (practice.View.selfRoleId != "werewolf") Assert.That(practice.View.wolfAllyIds, Is.Empty);
            practice.Send(new RoomCommand { kind = "act", turn = practice.View.turn - 1, commandId = "late-practice-choice" });
            Assert.That(practice.StatusError, Is.Not.Null);
            practice.RetryPending();
            Assert.That(practice.StatusError, Is.Null, "Retry must clear a resolved local training error.");
            var hideEvents = 0;
            practice.SecretsMustHide += () => hideEvents++;
            practice.SetActive(false);
            owner.SendMessage("OnApplicationFocus", false);
            Assert.That(hideEvents, Is.GreaterThanOrEqualTo(2));
            practice.ForgetRoom();
            Assert.That(practice.View, Is.Null);
            Assert.That(practice.HasSession, Is.False);
        }

        [Test]
        public void OrdinaryNetworkCommandsOmitPhantomUnitySettingsButConfigurationKeepsThem()
        {
            var command = new RoomCommand { kind = "ready", ready = true, turn = 17, commandId = "wire-ready" };
            var body = UnityRoomClient.SerializeCommand(command);
            Assert.That(body, Does.Not.Contain("\"settings\""));
            var decoded = JsonUtility.FromJson<RoomCommand>(body);
            Assert.That(decoded.turn, Is.EqualTo(17));
            Assert.That(decoded.commandId, Is.EqualTo("wire-ready"));
            command.kind = "configure";
            command.settings = new RoomSettings { nightSeconds = 51 };
            Assert.That(UnityRoomClient.SerializeCommand(command), Does.Contain("\"nightSeconds\":51"));
        }

        [UnityTest]
        public IEnumerator RealRoomCardMasksDuringFlipAndDoesNotResumeAfterPhaseOrFocusChange()
        {
            owner = new GameObject("Connected privacy card test", typeof(RectTransform), typeof(Canvas));
            var hold = new GameObject("Privacy hold", typeof(RectTransform));
            hold.transform.SetParent(owner.transform, false);
            var card = hold.AddComponent<RoomSecretCard>();
            card.Initialize(owner.transform, new AppPreferences(new MemoryPreferences()));
            var snapshot = new RoomView { code = "ABCDEF", turn = 4, phase = "night", selfRoleId = "seer" };
            card.BindSnapshot(snapshot, true);
            card.BeginHold();
            yield return new WaitForSecondsRealtime(RoomSecretCard.HoldSeconds + .03f);
            Assert.That(card.IsRevealed, Is.True);
            card.MaskImmediately();
            Assert.That(card.IsFaceDisplayed, Is.False);
            Assert.That(card.IsRevealed, Is.False);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(card.IsFaceDisplayed, Is.False, "A canceled flip must never restore the face.");
            card.BeginHold();
            yield return new WaitForSecondsRealtime(.2f);
            card.BindSnapshot(new RoomView { code = "ABCDEF", turn = 5, phase = "dawn", selfRoleId = "seer" }, true);
            yield return new WaitForSecondsRealtime(RoomSecretCard.HoldSeconds);
            Assert.That(card.IsHolding, Is.False);
            Assert.That(card.IsFaceDisplayed, Is.False);
            card.BeginHold();
            hold.SendMessage("OnApplicationFocus", false);
            yield return new WaitForSecondsRealtime(RoomSecretCard.HoldSeconds + .2f);
            Assert.That(card.IsFaceDisplayed, Is.False);
            Assert.That(card.IsHolding, Is.False);
            card.SetBlocked(true);
            card.BeginHold();
            Assert.That(card.IsHolding, Is.False);
        }
    }
}
