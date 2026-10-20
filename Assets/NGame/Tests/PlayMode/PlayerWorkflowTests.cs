using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NGame.Core;
using NGame.Core.Multiplayer;
using NGame.Presentation.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NGame.Presentation.Tests
{
    /// <summary>
    /// Exercises production controls through the EventSystem, including actual raycast reachability.
    /// Fixture cases validate ergonomics/state binding, not server integration or novice human comprehension.
    /// The separate practice case uses the real engine and bots, without submitting RoomCommands from this test.
    /// </summary>
    public sealed class PlayerWorkflowTests
    {
        private sealed class FixtureClient : IRoomClient
        {
            public RoomView View { get; private set; }
            public string RoomCode => View?.code ?? "";
            public bool HasSession => View != null;
            public bool IsBusy => false;
            public bool IsConnected => HasSession;
            public bool IsTraining => false;
            public string StatusError => null;
            public readonly List<RoomCommand> Sent = new List<RoomCommand>();
            public event Action Changed;
            public event Action SecretsMustHide;

            public FixtureClient(RoomView view) { View = view; }
            public void CreateRoom(string name, int avatar, RoomSettings settings) { }
            public void JoinRoom(string code, string name, int avatar) { }
            public void SetActive(bool active) { if (!active) SecretsMustHide?.Invoke(); }
            public void Refresh() => Changed?.Invoke();
            public void RetryPending() => Changed?.Invoke();
            public void ForgetRoom() { View = null; SecretsMustHide?.Invoke(); Changed?.Invoke(); }
            public void Send(RoomCommand command)
            {
                Sent.Add(command);
                SecretsMustHide?.Invoke();
                if (command.kind == "acknowledge-role") { View.roleAcknowledged = true; View.acknowledgedCount++; }
                else if (command.kind == "act" || command.kind == "vote")
                {
                    View.action.submitted = true; View.action.submittedChoice = command.choice;
                    View.action.submittedTargetIds = (string[])command.targetIds.Clone();
                    if (command.kind == "vote")
                    {
                        View.selfVoteTargetId = command.targetIds.FirstOrDefault();
                        View.selfVoteAbstained = command.choice == "pass"; View.votesReceived++;
                    }
                }
                else if (command.kind == "reset")
                {
                    View.turn++; View.phase = "lobby"; View.matchId = "fixture-rematch";
                    View.selfRoleId = View.selfRoleName = null; View.action = null;
                    View.privateFacts = Array.Empty<PrivateFact>(); View.winner = null;
                    foreach (var player in View.players) { player.alive = true; player.ready = false; player.revealedRoleId = null; }
                }
                else if (command.kind == "leave") View = null;
                if (View != null) View.revision++;
                Changed?.Invoke();
            }
        }

        private GameObject owner;
        private StorybookApp app;
        private int originalFrameRate, originalAudio, originalMotion;
        private bool hadAudio, hadMotion, hadServer;
        private string originalServer;
        private ScreenOrientation originalOrientation;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            originalFrameRate = Application.targetFrameRate; originalOrientation = Screen.orientation;
            hadServer = PlayerPrefs.HasKey("ngame.network.server");
            originalServer = PlayerPrefs.GetString("ngame.network.server");
            PlayerPrefs.SetString("ngame.network.server", "http://127.0.0.1:18991/workflow-fixture");
            hadAudio = PlayerPrefs.HasKey(AppPreferences.AmbientSoundKey);
            hadMotion = PlayerPrefs.HasKey(AppPreferences.ReducedMotionKey);
            originalAudio = PlayerPrefs.GetInt(AppPreferences.AmbientSoundKey);
            originalMotion = PlayerPrefs.GetInt(AppPreferences.ReducedMotionKey);
            foreach (var existing in UnityEngine.Object.FindObjectsByType<StorybookApp>()) UnityEngine.Object.Destroy(existing.gameObject);
            yield return null;
            PlayerPrefs.SetInt(AppPreferences.AmbientSoundKey, 0);
            PlayerPrefs.SetInt(AppPreferences.ReducedMotionKey, 1);
            owner = new GameObject("Player workflow through actual controls");
            app = owner.AddComponent<StorybookApp>();
            yield return null;
            app.OpenGameHome();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (owner != null) UnityEngine.Object.Destroy(owner);
            yield return null;
            RestorePreference(AppPreferences.AmbientSoundKey, hadAudio, originalAudio);
            RestorePreference(AppPreferences.ReducedMotionKey, hadMotion, originalMotion);
            if (hadServer) PlayerPrefs.SetString("ngame.network.server", originalServer);
            else PlayerPrefs.DeleteKey("ngame.network.server");
            PlayerPrefs.Save();
            Application.targetFrameRate = originalFrameRate; Screen.orientation = originalOrientation;
        }

        private static void RestorePreference(string key, bool existed, int value)
        { if (existed) PlayerPrefs.SetInt(key, value); else PlayerPrefs.DeleteKey(key); }

        private static RoomView View(string phase, string role = "seer")
        {
            var players = Enumerable.Range(0, 6).Select(i => new PlayerView
            { id = "player-" + i, displayName = "Personne " + i, avatarIndex = i, ready = true, alive = true, connected = true }).ToArray();
            return new RoomView
            {
                code = "TESTUI", matchId = "fixture-match", mode = "in-person", phase = phase,
                selfPlayerId = players[0].id, hostPlayerId = players[1].id, selfRoleId = role,
                selfRoleName = RoleCatalog.Find(role)?.DisplayName, day = 1, turn = 7, revision = 1,
                serverTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                deadlineUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 120000,
                settings = new RoomSettings(), players = players,
                composition = new[] { new NGame.Core.Multiplayer.RoleCount { roleId = "werewolf", count = 1 },
                    new NGame.Core.Multiplayer.RoleCount { roleId = "seer", count = 1 },
                    new NGame.Core.Multiplayer.RoleCount { roleId = "villager", count = 4 } }
            };
        }

        private Button FindButton(string name)
        {
            var button = owner.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(candidate => candidate.name == name && candidate.gameObject.activeInHierarchy);
            Assert.That(button, Is.Not.Null, "Missing active production button: " + name);
            return button;
        }

        private bool CanHit(Button button, out PointerEventData pointer)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            pointer = new PointerEventData(EventSystem.current)
            {
                pointerId = 31, button = PointerEventData.InputButton.Left, position = position,
                pressPosition = position, pointerPress = button.gameObject, rawPointerPress = button.gameObject
            };
            hits.Clear(); EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0) return false;
            pointer.pointerCurrentRaycast = hits[0]; pointer.pointerPressRaycast = hits[0];
            return ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == button.gameObject;
        }

        private IEnumerator Reach(Button button)
        {
            Assert.That(button.gameObject.activeInHierarchy, Is.True);
            for (var attempt = 0; attempt < 24; attempt++)
            {
                if (CanHit(button, out _)) yield break;
                var scroll = button.GetComponentInParent<ScrollRect>();
                Assert.That(scroll, Is.Not.Null, "Button is obscured outside a scroll view: " + button.name);
                var wheel = new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -4) };
                ExecuteEvents.Execute(scroll.gameObject, wheel, ExecuteEvents.scrollHandler);
                yield return null;
            }
            Assert.Fail("The player cannot reach the actual control by scrolling: " + button.name);
        }

        private IEnumerator Tap(string name)
        {
            var button = FindButton(name);
            Assert.That(button.interactable, Is.True, "Disabled production button: " + name);
            yield return Reach(button);
            Assert.That(CanHit(button, out var pointer), Is.True, "A different UI element covers " + name);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
        }

        private IEnumerator HoldPersonalCard()
        {
            var button = app.Game.SecondaryAction;
            Assert.That(button.interactable, Is.True);
            yield return Reach(button);
            Assert.That(CanHit(button, out var pointer), Is.True);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(RoomSecretCard.HoldSeconds + .12f);
            Assert.That(app.Game.SecretCard.IsRevealed, Is.True, "The actual lower control must reveal after a real hold.");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            Assert.That(app.Game.SecretCard.IsFaceDisplayed, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BurgerAndStayNeverLeaveOrSendAGameActionAndTheDockDoesNotMove()
        {
            var fixture = new FixtureClient(View("night")); app.OpenClient(fixture);
            yield return null;
            var primary = (RectTransform)app.Game.PrimaryAction.transform;
            var secondary = (RectTransform)app.Game.SecondaryAction.transform;
            var upperPosition = primary.anchoredPosition; var lowerPosition = secondary.anchoredPosition;
            yield return Tap("Safe application navigation");
            Assert.That(app.Game.IsMenuOpen, Is.True); Assert.That(fixture.HasSession, Is.True);
            Assert.That(fixture.Sent, Is.Empty);
            yield return Tap("Request departure");
            Assert.That(app.Game.IsQuitConfirmationOpen, Is.True);
            Assert.That(app.Game.SecretCard.IsFaceDisplayed, Is.False);
            Assert.That(app.Game.PrimaryAction.interactable, Is.False);
            yield return Tap("Stay in current game");
            Assert.That(app.Game.IsMenuOpen, Is.False); Assert.That(app.Route, Is.EqualTo(AppRoute.Play));
            Assert.That(fixture.HasSession, Is.True); Assert.That(fixture.Sent, Is.Empty);
            Assert.That(primary.anchoredPosition, Is.EqualTo(upperPosition));
            Assert.That(secondary.anchoredPosition, Is.EqualTo(lowerPosition));
        }

        [UnityTest]
        public IEnumerator DistributionRequiresTheVisibleHoldThenThePrimaryAcknowledgement()
        {
            var fixture = new FixtureClient(View("distribution")); app.OpenClient(fixture);
            yield return null;
            Assert.That(app.Game.PrimaryAction.interactable, Is.False);
            yield return HoldPersonalCard();
            Assert.That(app.Game.PrimaryAction.interactable, Is.True);
            yield return Tap("Player primary action");
            Assert.That(fixture.Sent.Count, Is.EqualTo(1));
            Assert.That(fixture.Sent[0].kind, Is.EqualTo("acknowledge-role"));
            Assert.That(fixture.Sent[0].turn, Is.EqualTo(7));
            Assert.That(app.Game.PrimaryAction.interactable, Is.False);
            Assert.That(app.Game.SecretCard.IsFaceDisplayed, Is.False);
        }

        [UnityTest]
        public IEnumerator APrivatePowerCanBeOpenedSelectedAndConfirmedWithSuccessiveSinglePointerTaps()
        {
            var state = View("night");
            state.action = new ActionView
            {
                kind = "seer", title = "Une carte dans la nuit", description = "Choisis une personne.",
                targetCount = 1, choices = new[] { "inspect", "pass" }, allowedTargetIds = new[] { "player-1", "player-2" }
            };
            var fixture = new FixtureClient(state); app.OpenClient(fixture);
            yield return null;
            Assert.That(app.Game.IsPrivateActionVisible, Is.False);
            yield return Tap("Player primary action");
            Assert.That(app.Game.IsPrivateActionVisible, Is.True);
            Assert.That(app.Game.SecretCard.IsHolding, Is.False, "Selecting a target must not require maintaining another pointer.");
            yield return Tap("Action choice inspect");
            yield return Tap("Action target player-2");
            Assert.That(fixture.Sent, Is.Empty, "Selecting a target must not submit it.");
            yield return Tap("Player primary action");
            Assert.That(fixture.Sent.Count, Is.EqualTo(1));
            Assert.That(fixture.Sent[0].kind, Is.EqualTo("act")); Assert.That(fixture.Sent[0].choice, Is.EqualTo("inspect"));
            Assert.That(fixture.Sent[0].targetIds, Is.EqualTo(new[] { "player-2" }));
            Assert.That(app.Game.IsPrivateActionVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator InterruptionMasksTheOneHandPrivatePaneWithoutReopeningIt()
        {
            var state = View("night");
            state.privateFacts = new[] { new PrivateFact { key = "seer-1", playerId = "player-1", roleId = "werewolf", description = "Découverte autorisée." } };
            app.OpenClient(new FixtureClient(state));
            yield return null;
            yield return Tap("Open personal information");
            Assert.That(app.Game.IsPrivateActionVisible, Is.True);
            app.Game.gameObject.SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);
            Assert.That(app.Game.IsPrivateActionVisible, Is.False);
            var publicView = owner.GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == "Public village information").gameObject;
            Assert.That(publicView.activeInHierarchy, Is.True, "Masking a private pane must restore the public waiting view immediately.");
            app.Game.gameObject.SendMessage("OnApplicationFocus", true, SendMessageOptions.RequireReceiver);
            yield return null;
            Assert.That(app.Game.IsPrivateActionVisible, Is.False);
            Assert.That(publicView.activeInHierarchy, Is.True);
            Assert.That(app.Game.SecretCard.IsFaceDisplayed, Is.False);
        }

        [UnityTest]
        public IEnumerator FinishedPrimaryPreparesARematchAndClearsTheOldPrivateView()
        {
            var state = View("finished"); state.isHost = true; state.hostPlayerId = state.selfPlayerId; state.winner = "village";
            var fixture = new FixtureClient(state); app.OpenClient(fixture);
            yield return null;
            yield return Tap("Player primary action");
            Assert.That(fixture.Sent.Single().kind, Is.EqualTo("reset"));
            Assert.That(fixture.View.phase, Is.EqualTo("lobby"));
            Assert.That(fixture.View.matchId, Is.EqualTo("fixture-rematch"));
            Assert.That(fixture.View.selfRoleId, Is.Null);
            Assert.That(app.Game.IsPrivateActionVisible, Is.False); Assert.That(app.Game.SecretCard.IsFaceDisplayed, Is.False);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator RealPracticeCanFinishAndPrepareARematchUsingOnlyProductionControls()
        {
            // Route selection is setup; every game action below is a real, raycast-checked control.
            app.StartTraining();
            yield return null;
            Assert.That(app.Game.Client, Is.TypeOf<PracticeRoomClient>());
            Assert.That(app.Game.Client.View.phase, Is.EqualTo("lobby"));
            yield return Tap("Player primary action");
            Assert.That(app.Game.Client.View.phase, Is.EqualTo("distribution"));
            yield return HoldPersonalCard();
            yield return Tap("Player primary action");
            var started = Time.realtimeSinceStartup;
            var sawNight = false; var sawVote = false;
            var deadline = started + 260;
            while (app.Game.Client.HasSession && app.Game.Client.View.phase != "finished" && Time.realtimeSinceStartup < deadline)
            {
                var state = app.Game.Client.View;
                sawNight |= state.phase == "night"; sawVote |= state.phase == "vote";
                if (state.phase == "distribution")
                {
                    Assert.That(state.roleAcknowledged, Is.True, "The local acknowledgement must remain recorded while bots read.");
                }
                else if (state.action != null && !state.action.submitted && state.action.kind != "observe")
                {
                    if (state.action.kind != "vote" && !app.Game.IsPrivateActionVisible)
                        yield return Tap("Player primary action");
                    state = app.Game.Client.View;
                    if (state.action != null && !state.action.submitted && state.action.kind != "observe")
                    {
                        // A valid pass keeps this end-to-end scenario independent from any hidden assignment.
                        if (state.action.choices.Contains("pass")) yield return Tap("Action choice pass");
                        if (app.Game.PrimaryAction.interactable) yield return Tap("Player primary action");
                    }
                }
                else if (state.isHost && state.canAdvance && app.Game.PrimaryAction.interactable)
                {
                    yield return Tap("Player primary action");
                    if (state.phase == "vote") yield return Tap("Confirm vote closure");
                }
                yield return new WaitForSecondsRealtime(.1f);
            }
            Assert.That(app.Game.Client.HasSession, Is.True);
            Assert.That(app.Game.Client.View.phase, Is.EqualTo("finished"), "A real practice match must finish within the test budget.");
            Assert.That(sawNight, Is.True); Assert.That(sawVote, Is.True);
            Assert.That(app.Game.Client.View.winner, Is.Not.Null.And.Not.Empty);
            yield return Tap("Player primary action");
            Assert.That(app.Game.Client.View.phase, Is.EqualTo("lobby"));
            Assert.That(app.Game.Client.View.selfRoleId, Is.Null);
        }
    }
}
