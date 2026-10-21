using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NGame.Core;
using NGame.Core.Multiplayer;
using NGame.Presentation.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NGame.Presentation.Tests
{
    /// <summary>UI callback/lifecycle regressions; does not claim novice or real-device validation.</summary>
    public sealed class PlayerCommunicationTests
    {
        private sealed class MemoryPreferences : IPreferenceStore
        {
            public bool ReadBool(string key, bool fallback) => fallback;
            public void WriteBool(string key, bool value) { }
            public void Save() { }
        }
        private sealed class FixtureClient : IRoomClient, IRoomCommunicationClient
        {
            public RoomView View { get; set; }
            public string RoomCode => View?.code;
            public bool HasSession => View != null;
            public bool IsBusy => false;
            public bool IsConnected => true;
            public bool IsTraining => false;
            public string StatusError => null;
            public CommunicationView Communications { get; set; }
            public ChatMessage[] Messages => Array.Empty<ChatMessage>();
            public bool IsChatBusy => false;
            public string ChatError => null;
            public VoiceView Voice { get; set; }
            public readonly List<RoomCommand> Commands = new List<RoomCommand>();
            public readonly List<string> SentChats = new List<string>();
            public event Action Changed;
            public event Action SecretsMustHide;
            public event Action CommunicationChanged;
            public void CreateRoom(string name, int avatar, RoomSettings settings) { }
            public void JoinRoom(string code, string name, int avatar) { }
            public void Send(RoomCommand command) => Commands.Add(command);
            public void Refresh() => Changed?.Invoke();
            public void SetActive(bool active) { if (!active) SecretsMustHide?.Invoke(); }
            public void ForgetRoom() { View = null; Changed?.Invoke(); }
            public void RetryPending() { }
            public void OpenChannel(string id) => CommunicationChanged?.Invoke();
            public void CloseChannel() { }
            public void SendChat(string channel, string text) => SentChats.Add(channel + ":" + text);
            public void SetMicrophone(bool enabled) { Voice.microphoneActive = enabled; CommunicationChanged?.Invoke(); }
            public void CommunicationsTick() => CommunicationChanged?.Invoke();
        }
        private GameObject owner, ownedEventSystem;
        private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();
        private RoomFlowView flow;
        private FixtureClient client;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            owner = new GameObject("Player communication UI fixture", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            owner.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; owner.GetComponent<Canvas>().sortingOrder = 1000;
            var scaler = owner.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(430, 932); scaler.matchWidthOrHeight = 1;
            if (EventSystem.current == null) ownedEventSystem = new GameObject("Communication fixture EventSystem", typeof(EventSystem));
            owner.GetComponent<RectTransform>().sizeDelta = new Vector2(430, 932);
            client = new FixtureClient
            {
                View = new RoomView
                {
                    code = "TESTUI", matchId = "first-match", selfPlayerId = "one", selfRoleId = "villager", selfRoleName = "Villageois",
                    turn = 8, phase = "vote", mode = "remote", voteRound = 1, votesExpected = 2,
                    players = new[] { new PlayerView { id = "one", displayName = "Léa", alive = true, connected = true }, new PlayerView { id = "two", displayName = "Max", alive = true, connected = true } },
                    action = new ActionView { kind = "vote", title = "Un suspect", targetCount = 1, choices = new[] { "vote", "pass" }, allowedTargetIds = new[] { "two" } }
                },
                Communications = new CommunicationView
                {
                    matchId = "first-match", turn = 8,
                    channels = new[] { new ChannelView { id = "village", name = "Village", canRead = true, canSend = true } }
                },
                Voice = new VoiceView { available = true, canListen = true, canTransmit = true }
            };
            var frame = new GameObject("Player safe frame", typeof(RectTransform));
            frame.transform.SetParent(owner.transform, false);
            var frameRect = (RectTransform)frame.transform; frameRect.anchorMin = Vector2.zero; frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
            flow = frame.AddComponent<RoomFlowView>();
            flow.Initialize(new AppPreferences(new MemoryPreferences()), client, _ => { });
            flow.SetRouteActive(true);
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator CleanUp() { UnityEngine.Object.Destroy(owner); if (ownedEventSystem != null) UnityEngine.Object.Destroy(ownedEventSystem); ownedEventSystem = null; yield return null; }
        private UnityEngine.UI.Button Button(string name)
            => owner.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(button => button.name == name && button.gameObject.activeInHierarchy);

        private bool CanHit(UnityEngine.UI.Button button, out PointerEventData pointer)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            pointer = new PointerEventData(EventSystem.current) { pointerId = 41, button = PointerEventData.InputButton.Left,
                position = position, pressPosition = position, pointerPress = button.gameObject, rawPointerPress = button.gameObject };
            pointerHits.Clear(); EventSystem.current.RaycastAll(pointer, pointerHits);
            if (pointerHits.Count == 0) return false;
            pointer.pointerCurrentRaycast = pointerHits[0]; pointer.pointerPressRaycast = pointerHits[0];
            return ExecuteEvents.GetEventHandler<IPointerClickHandler>(pointerHits[0].gameObject) == button.gameObject;
        }
        private IEnumerator TapProduction(string name)
        {
            var button = Button(name);
            Assert.That(button.interactable, Is.True, "Disabled control: " + name);
            for (var attempt = 0; attempt < 24; attempt++)
            {
                if (CanHit(button, out var pointer))
                {
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                    yield return null; yield break;
                }
                var scroll = button.GetComponentInParent<ScrollRect>();
                Assert.That(scroll, Is.Not.Null, "Control covered outside scrolling content: " + name);
                ExecuteEvents.Execute(scroll.gameObject, new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -4) }, ExecuteEvents.scrollHandler);
                yield return null;
            }
            Assert.Fail("Control unreachable through actual raycasts: " + name);
        }

        [UnityTest]
        public IEnumerator ScreenNightUsesTheSamePublicViewAndMaVueForActorsAndWaitingPlayers()
        {
            client.View.settings = new RoomSettings { nightProtocol = "screen" };
            client.View.phase = "night"; client.View.nightStep = "seer"; client.View.serverTimeUnixMs = 120000; client.View.phaseStartedUnixMs = 100000;
            foreach (var role in new[] { "villager", "werewolf", "seer" })
            {
                client.View.selfRoleId = role; client.View.turn++; client.View.revision++;
                client.View.action = role == "villager" ? null : new ActionView { kind = role == "werewolf" ? "wolves" : "seer",
                    choices = new[] { "inspect" }, targetCount = 1, allowedTargetIds = new[] { "two" } };
                client.Refresh(); yield return null;
                Assert.That(flow.PrimaryAction.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Ma vue"));
                Assert.That(owner.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "Real room heading").text, Is.EqualTo("Tour de la Voyante"), "The public call follows nightStep, never the private action or role.");
                Assert.That(flow.NightActivity.IsAvailable, Is.True);
                Assert.That(flow.PrivateActionStage.gameObject.activeInHierarchy, Is.False);
                Assert.That(owner.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "Common screen night activity").text, Is.EqualTo("Veille des lucioles"));
                Assert.That(owner.GetComponentsInChildren<TMP_Text>().Any(t => t.name == "Night personal availability"), Is.False);
                yield return TapProduction("Player primary action");
                Assert.That(flow.IsPrivateActionVisible, Is.True);
                Assert.That(flow.PrivateActionStage.gameObject.activeInHierarchy, Is.True);
                Assert.That(flow.NightActivity.gameObject.activeInHierarchy, Is.False);
                flow.ClosePrivateInformation(); yield return null;
                Assert.That(flow.PrivateActionStage.gameObject.activeInHierarchy, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator PrivateActionSceneryWaitsForTheServerReceiptAndMasksOnFocusLoss()
        {
            client.View.settings = new RoomSettings { nightProtocol = "screen" };
            client.View.phase = "night"; client.View.selfRoleId = "seer"; client.View.selfRoleName = "Voyante";
            client.View.turn++; client.View.revision++;
            client.View.action = new ActionView { kind = "seer", choices = new[] { "inspect", "pass" }, targetCount = 1, allowedTargetIds = new[] { "two" } };
            client.Refresh(); yield return null;
            yield return TapProduction("Player primary action");
            var stage = flow.PrivateActionStage;
            Assert.That(stage.gameObject.activeInHierarchy, Is.True);
            Assert.That(owner.GetComponentsInChildren<Image>().Single(i => i.name == "Small private role portrait").preserveAspect, Is.True);
            yield return TapProduction("Action target two");
            Assert.That(owner.GetComponentsInChildren<Image>().Any(i => i.name == "Private selected target avatar 0" && i.sprite != null), Is.True);
            yield return TapProduction("Player primary action");
            Assert.That(client.Commands.Count, Is.EqualTo(1));
            Assert.That(stage.ConfirmationVersion, Is.Zero, "A tap sends a request; it is not an accepted power yet.");
            client.View.action.submitted = true; client.View.revision++; client.Refresh(); yield return null;
            Assert.That(stage.ConfirmationVersion, Is.EqualTo(1));
            client.View.revision++; client.Refresh(); yield return null;
            Assert.That(stage.ConfirmationVersion, Is.EqualTo(1), "A later projection must not replay the receipt.");
            flow.SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);
            Assert.That(flow.IsPrivateActionVisible, Is.False);
            Assert.That(stage.gameObject.activeInHierarchy, Is.False);
            Assert.That(owner.GetComponentsInChildren<Image>(true).Single(i => i.name == "Small private role portrait").gameObject.activeInHierarchy, Is.False);
            Assert.That(owner.GetComponentsInChildren<Image>(true).Where(i => i.name.StartsWith("Private selected target avatar ")).All(i => !i.gameObject.activeInHierarchy), Is.True);
            Assert.That(flow.PrimaryAction.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Ma vue"));
        }

        [UnityTest]
        public IEnumerator ScreenNightActivityAndPrivateTargetsFitThe850PointSafeFrameWithoutOverlap()
        {
            var frame = (RectTransform)flow.transform; frame.offsetMin = new Vector2(0, 40); frame.offsetMax = new Vector2(0, -42);
            client.View.settings = new RoomSettings { nightProtocol = "screen" }; client.View.phase = "night";
            client.View.turn++; client.View.revision++; client.View.selfRoleId = "seer";
            client.View.action = new ActionView { kind = "seer", choices = new[] { "inspect", "pass" }, targetCount = 1, allowedTargetIds = new[] { "two" } };
            client.Refresh(); yield return null; yield return null;
            var body = (RectTransform)typeof(RoomFlowView).GetField("body", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(flow);
            var shell = (RectTransform)body.parent.parent;
            Assert.That(shell.rect.height, Is.EqualTo(94).Within(1));
            var activity = (RectTransform)flow.NightActivity.transform;
            Assert.That(activity.anchoredPosition.y + activity.rect.height * .5f, Is.LessThan(shell.offsetMin.y));
            Assert.That(activity.anchoredPosition.y - activity.rect.height * .5f, Is.GreaterThan(225));
            Assert.That(CanHit(Button("Firefly touch target 0"), out _), Is.True);
            yield return TapProduction("Firefly touch target 0"); Assert.That(flow.NightActivity.LocalCatchCount, Is.EqualTo(1));
            yield return TapProduction("Player primary action"); yield return null;
            Assert.That(shell.rect.height, Is.EqualTo(389).Within(1));
            Assert.That(CanHit(Button("Action target two"), out _), Is.True);
            flow.ClosePrivateInformation(); client.View.settings.nightProtocol = "traditional"; client.View.revision++; client.Refresh(); yield return null;
            Assert.That(flow.NightActivity.gameObject.activeInHierarchy, Is.False);
            Assert.That(owner.GetComponentsInChildren<TMP_Text>().Any(t => t.name == "Night personal availability"), Is.True);
        }

        [UnityTest]
        public IEnumerator ChangingNightProtocolNeverSilentlyReplacesTheLittleGirl()
        {
            var dialog = new GameObject("Protocol composition UI fixture", typeof(RectTransform)); dialog.transform.SetParent(owner.transform, false);
            var rect = (RectTransform)dialog.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var initial = new RoomSettings { mode = "in-person", nightProtocol = "traditional", composition = new[]
                { new NGame.Core.Multiplayer.RoleCount { roleId = "werewolf", count = 1 }, new NGame.Core.Multiplayer.RoleCount { roleId = "villager", count = 4 }, new NGame.Core.Multiplayer.RoleCount { roleId = "little-girl", count = 1 } } };
            RoomSettings applied = null;
            dialog.AddComponent<RoomCompositionPanel>().Initialize(new RoomView { mode = "in-person", players = new PlayerView[6] }, initial, value => applied = value, () => { });
            yield return null;
            yield return TapProduction("Host screen night protocol");
            Assert.That(Button("Apply room composition").interactable, Is.False);
            Assert.That(owner.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "Count little-girl").text, Does.Contain("1"));
            Assert.That(Button("Remove little-girl").interactable, Is.True, "The host must explicitly remove an incompatible selected card.");
            yield return TapProduction("Remove little-girl"); yield return TapProduction("Add villager");
            yield return TapProduction("Apply room composition");
            Assert.That(applied, Is.Not.Null); Assert.That(applied.nightProtocol, Is.EqualTo("screen"));
            Assert.That(applied.composition.Any(c => c.roleId == "little-girl"), Is.False);
            Assert.That(initial.composition.Single(c => c.roleId == "little-girl").count, Is.EqualTo(1), "The original room settings are copied, never mutated while editing.");
        }

        [UnityTest]
        public IEnumerator RoleGuideStaysInsideTheRoomAndShowsOnlyPublicCatalogueRules()
        {
            client.View.privateFacts = new[] { new PrivateFact { description = "CONFIDENTIAL ASSIGNMENT" } };
            flow.OpenRoomHelp(); yield return null;
            yield return TapProduction("Open implemented role guide");
            var guide = owner.GetComponentsInChildren<RectTransform>().Single(t => t.name == "Implemented role guide");
            Assert.That(guide.GetComponentsInChildren<TMP_Text>().Count(t => t.name.StartsWith("Role guide name ")), Is.EqualTo(8));
            Assert.That(guide.GetComponentsInChildren<TMP_Text>().Any(t => !string.IsNullOrEmpty(t.text) && t.text.Contains("CONFIDENTIAL ASSIGNMENT")), Is.False);
            Assert.That(flow.IsPrivateActionVisible, Is.False);
            Assert.That(client.Commands, Is.Empty);
            yield return TapProduction("Close safe room menu");
            Assert.That(owner.GetComponentsInChildren<TMP_Text>().Any(t => t.name == "Rules title"), Is.True);
        }

        [UnityTest]
        public IEnumerator ChatCannotOpenOutsideVotingAndItsToggleIsAbsent()
        {
            foreach (var phase in new[] { "lobby", "distribution", "night", "dawn", "discussion", "verdict", "reaction", "finished" })
            {
                client.View.phase = phase; client.View.turn++; client.View.revision++; client.View.action = null;
                // Stale readable ACL must not bypass the local phase guard.
                client.Communications.channels = new[] { new ChannelView { id = "village", name = "Village", canRead = true, canSend = true } }; client.Refresh();
                flow.OpenChatPanel(); yield return null;
                Assert.That(flow.IsChatOpen, Is.False, "Chat must stay closed in " + phase);
                var toggle = owner.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(item => item.name == "Open room chat");
                Assert.That(toggle.gameObject.activeInHierarchy, Is.False, "No message entry outside vote: " + phase);
                flow.SubmitChatMessage(); Assert.That(client.SentChats, Is.Empty);
            }
        }

        [UnityTest]
        public IEnumerator VotingMessagesUseABoundedTransparentBottomDock()
        {
            var primary = ((RectTransform)flow.PrimaryAction.transform).anchoredPosition;
            var secondary = ((RectTransform)flow.SecondaryAction.transform).anchoredPosition;
            yield return TapProduction("Open room chat");
            Assert.That(flow.IsChatOpen, Is.True);
            var panel = owner.GetComponentsInChildren<RectTransform>().Single(item => item.name == "Room conversation panel");
            Assert.That(panel.rect.height, Is.LessThanOrEqualTo(230.1f));
            Assert.That(panel.anchoredPosition.y - panel.rect.height * panel.pivot.y, Is.GreaterThanOrEqualTo(225));
            Assert.That(panel.rect.width, Is.LessThanOrEqualTo(382.1f));
            var backdrop = owner.GetComponentsInChildren<Image>().Single(item => item.name == "Conversation dock background");
            Assert.That(backdrop.color.a, Is.InRange(.4f, .9f));
            Assert.That(owner.GetComponentsInChildren<Transform>().Any(item => item.name == "Conversation private backdrop"), Is.False);
            Assert.That(((RectTransform)flow.PrimaryAction.transform).anchoredPosition, Is.EqualTo(primary));
            Assert.That(((RectTransform)flow.SecondaryAction.transform).anchoredPosition, Is.EqualTo(secondary));
            yield return TapProduction("Open room chat");
            Assert.That(flow.IsChatOpen, Is.False, "The same fixed bar collapses the history.");
        }

        [UnityTest]
        public IEnumerator AnExpandedMessageDockStillAllowsARealVoteThenClosesAtPhaseChange()
        {
            yield return TapProduction("Open room chat");
            Assert.That(flow.IsChatOpen, Is.True);
            yield return TapProduction("Action target two");
            yield return TapProduction("Player primary action");
            Assert.That(client.Commands.Single().kind, Is.EqualTo("vote"));
            Assert.That(client.Commands.Single().targetIds, Is.EqualTo(new[] { "two" }));
            Assert.That(flow.IsChatOpen, Is.True, "A public vote does not dismiss the message dock.");
            client.View.phase = "verdict"; client.View.turn++; client.View.revision++; client.View.action = null;
            client.Communications.channels = Array.Empty<ChannelView>(); client.Refresh(); yield return null;
            Assert.That(flow.IsChatOpen, Is.False);
            Assert.That(flow.SecretsVisible, Is.False, "Leaving vote must not reveal or restore a private card.");
            Assert.That(flow.ChatInput, Is.Null);
        }

        [UnityTest]
        public IEnumerator ThreeTargetsAndTheirPageControlsFitAboveExpandedMessagesWithoutScrolling()
        {
            var extra = Enumerable.Range(3, 7).Select(i => new PlayerView { id = "person-" + i, displayName = "Joueur " + i, alive = true, connected = true });
            client.View.players = client.View.players.Concat(extra).ToArray();
            client.View.action.allowedTargetIds = client.View.players.Where(item => item.id != "one").Select(item => item.id).ToArray();
            client.View.revision++; client.Refresh(); yield return null;
            yield return TapProduction("Open room chat"); yield return null;
            var visibleTargets = owner.GetComponentsInChildren<UnityEngine.UI.Button>().Where(item => item.name.StartsWith("Action target ")).ToArray();
            Assert.That(visibleTargets.Length, Is.EqualTo(3), "One row avoids the clipped second row while messages are open.");
            foreach (var target in visibleTargets)
            {
                Assert.That(((RectTransform)target.transform).rect.height, Is.GreaterThanOrEqualTo(44));
                Assert.That(CanHit(target, out _), Is.True, "Every target is visible without scrolling: " + target.name);
            }
            Assert.That(CanHit(Button("Next targets"), out _), Is.True, "Page navigation must be visible above messages without scrolling.");
            Assert.That(owner.GetComponentsInChildren<TMP_Text>().Single(item => item.name == "Target page indicator").text, Is.EqualTo("1 / 3"));
            yield return TapProduction("Next targets");
            Assert.That(owner.GetComponentsInChildren<TMP_Text>().Single(item => item.name == "Target page indicator").text, Is.EqualTo("2 / 3"));
            const string targetName = "Action target person-5";
            yield return TapProduction(targetName);
            var selectedTargetControl = Button(targetName);
            client.View.votesReceived++; client.View.revision++; client.Refresh(); yield return null;
            Assert.That(Button(targetName), Is.SameAs(selectedTargetControl), "Another vote must not replace my paged selection control.");
            yield return TapProduction("Player primary action");
            Assert.That(client.Commands.Single().targetIds, Is.EqualTo(new[] { "person-5" }));
        }

        [UnityTest]
        public IEnumerator EliminatedPlayersReceiveObserverInstructionsInsteadOfAPowerOrAVote()
        {
            client.View.players[0].alive = false; client.View.action = null; client.View.revision++; client.Refresh(); yield return null;
            var voteNotice = owner.GetComponentsInChildren<TMP_Text>().Single(item => item.name == "Eliminated vote status");
            Assert.That(voteNotice.text, Does.Contain("Tu ne votes plus").And.Contain("Observateurs"));
            Assert.That(flow.PrimaryAction.interactable, Is.False);
            client.View.phase = "night"; client.View.turn++; client.View.revision++; client.Refresh(); yield return null;
            var nightNotice = owner.GetComponentsInChildren<TMP_Text>().Single(item => item.name == "Night personal availability");
            Assert.That(nightNotice.text, Is.EqualTo("Tu observes la partie"));
            Assert.That(flow.PrimaryAction.interactable, Is.False);
            Assert.That(flow.IsPrivateActionVisible, Is.False);
            Assert.That(client.Commands, Is.Empty);
        }

        [UnityTest]
        public IEnumerator CommunicationPollingDoesNotReplaceAVoteTargetBetweenPointerEvents()
        {
            var target = Button("Action target two");
            var microphone = Button("Remote day vote microphone");
            for (var i = 0; i < 4; i++) { client.CommunicationsTick(); yield return null; }
            Assert.That(Button("Action target two"), Is.SameAs(target), "A network poll must not destroy an in-progress target interaction.");
            client.View.revision++; client.View.votesReceived = 1; client.Refresh(); yield return null;
            Assert.That(Button("Action target two"), Is.SameAs(target), "Someone else voting updates the counter without replacing my target buttons.");
            Assert.That(Button("Remote day vote microphone"), Is.SameAs(microphone));
            var microphonePosition = ((RectTransform)microphone.transform).anchoredPosition;
            microphone.onClick.Invoke();
            Assert.That(client.Voice.microphoneActive, Is.True);
            microphone.onClick.Invoke();
            Assert.That(client.Voice.microphoneActive, Is.False, "The click reads current microphone state rather than a captured old state.");
            target.onClick.Invoke();
            flow.PrimaryAction.onClick.Invoke();
            Assert.That(client.Commands.Count, Is.EqualTo(1));
            Assert.That(client.Commands[0].kind, Is.EqualTo("vote"));
            Assert.That(client.Commands[0].targetIds, Is.EqualTo(new[] { "two" }));
            client.View.action.submitted = true; client.View.revision++; client.Refresh(); yield return null;
            Assert.That(Button("Remote day vote microphone"), Is.SameAs(microphone));
            Assert.That(((RectTransform)microphone.transform).anchoredPosition, Is.EqualTo(microphonePosition),
                "Submitting a vote must not move or replace the microphone control.");
        }

        [UnityTest]
        public IEnumerator ARevokedPrivateChatDraftDoesNotBecomeAPublicMessage()
        {
            client.View.players[0].alive = false; client.View.action = null; client.View.revision++; client.Refresh();
            client.Communications.channels = new[]
            {
                new ChannelView { id = "village", name = "Village", canRead = true, canSend = false },
                new ChannelView { id = "observateurs", name = "Observateurs", canRead = true, canSend = true, isPrivate = true }
            };
            Button("Open room chat").onClick.Invoke();
            Button("Conversation channel observateurs").onClick.Invoke();
            flow.ChatInput.text = "Nous sommes dans le canal des éliminés.";
            client.Communications.channels = new[] { new ChannelView { id = "village", name = "Village", canRead = true, canSend = false } };
            client.CommunicationsTick();
            yield return null;
            Assert.That(flow.IsChatOpen, Is.False, "Losing the private audience closes its conversation immediately.");
            Button("Open room chat").onClick.Invoke();
            Assert.That(flow.ChatInput.text, Is.Empty, "A private draft must never migrate into Village.");
            Assert.That(client.SentChats, Is.Empty);
        }

        [UnityTest]
        public IEnumerator FocusLossRequiresReopeningThePrivateActionBeforeItCanSubmit()
        {
            client.View.phase = "night"; client.View.turn++; client.View.selfRoleId = "seer"; client.View.selfRoleName = "Voyante";
            client.View.action = new ActionView { kind = "seer", title = "Ton enquête", targetCount = 1, choices = new[] { "inspect" }, allowedTargetIds = new[] { "two" } };
            client.Refresh();
            flow.PrimaryAction.onClick.Invoke();
            Assert.That(flow.IsPrivateActionVisible, Is.True, "The action can be opened with one tap; holding a second finger is unnecessary.");
            Button("Action target two").onClick.Invoke();
            flow.SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);
            Assert.That(flow.IsPrivateActionVisible, Is.False);
            Assert.That(flow.PrimaryAction.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Ouvrir mon action privée"));
            flow.PrimaryAction.onClick.Invoke();
            Assert.That(flow.IsPrivateActionVisible, Is.True);
            Assert.That(client.Commands, Is.Empty, "The first tap after masking reopens; it does not silently submit the old private choice.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TypingTemporarilyHidesTheVoteBodyAndRestoresItsSelectedPage()
        {
            client.View.players = client.View.players.Concat(Enumerable.Range(3, 7).Select(i => new PlayerView
                { id = "person-" + i, displayName = "Joueur " + i, alive = true, connected = true })).ToArray();
            client.View.action.allowedTargetIds = client.View.players.Where(p => p.id != "one").Select(p => p.id).ToArray();
            client.View.revision++; client.Refresh(); yield return null;
            yield return TapProduction("Open room chat");
            yield return TapProduction("Next targets");
            yield return TapProduction("Action target person-5");
            var selected = Button("Action target person-5");
            var scrollShell = selected.GetComponentInParent<ScrollRect>().gameObject;
            var insetSetter = typeof(RoomFlowView).GetProperty("ChatKeyboardInset").GetSetMethod(true);
            var refresh = typeof(RoomFlowView).GetMethod("RefreshChatLayout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            // Inject the operating-system inset synchronously: no device keyboard is claimed by this regression.
            insetSetter.Invoke(flow, new object[] { 350f }); refresh.Invoke(flow, null);
            Assert.That(scrollShell.activeSelf, Is.False, "No negative-height or obscured vote controls while typing.");
            Assert.That(flow.PrimaryAction.interactable, Is.False);
            insetSetter.Invoke(flow, new object[] { 0f }); refresh.Invoke(flow, null);
            Assert.That(scrollShell.activeSelf, Is.True);
            yield return null; yield return null; // Allow destroyed controls and the restored layout to settle.
            Assert.That(Button("Action target person-5").GetComponentInChildren<TMP_Text>().text, Does.Contain("• "), "Keyboard must preserve the selected target on its page.");
            Assert.That(owner.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "Target page indicator").text, Is.EqualTo("2 / 3"));
            Assert.That(CanHit(Button("Next targets"), out _), Is.True);
            yield return TapProduction("Player primary action");
            Assert.That(client.Commands.Single().targetIds, Is.EqualTo(new[] { "person-5" }));
        }

        [UnityTest]
        public IEnumerator PrivateWitchTargetsAndPagerFitInsideAn850PointSafeFrame()
        {
            var frame = (RectTransform)flow.transform;
            frame.offsetMin = new Vector2(0, 40); frame.offsetMax = new Vector2(0, -42);
            client.View.players = client.View.players.Concat(Enumerable.Range(3, 7).Select(i => new PlayerView
                { id = "person-" + i, displayName = "Joueur " + i, alive = true, connected = true })).ToArray();
            client.View.phase = "night"; client.View.turn++; client.View.revision++;
            client.View.selfRoleId = "witch"; client.View.selfRoleName = "Sorcière";
            client.View.privateFacts = new[] { new PrivateFact { description = "Une ancienne information doit rester consultable." } };
            client.View.action = new ActionView { kind = "witch", title = "Utiliser les potions",
                description = "Soin et poison sont chacun utilisables une fois. Avec les deux, sélectionne uniquement la cible du poison.",
                privateInfo = "La victime de la meute est Max.", healingAvailable = true, poisonAvailable = true,
                targetCount = 1, choices = new[] { "pass", "save", "poison", "both" },
                allowedTargetIds = client.View.players.Where(p => p.id != "one").Select(p => p.id).ToArray() };
            client.Refresh(); flow.PrimaryAction.onClick.Invoke(); yield return null; yield return null;
            Assert.That(frame.rect.height, Is.EqualTo(850).Within(1));
            var roleHeader = (RectTransform)typeof(RoomFlowView).GetField("roleHeader", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(flow);
            Assert.That(roleHeader.gameObject.activeInHierarchy, Is.False);
            Assert.That(CanHit(Button("Action choice both"), out _), Is.True);
            yield return TapProduction("Action choice both"); yield return null; yield return null;
            var visible = owner.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.name.StartsWith("Action target ")).ToArray();
            Assert.That(visible.Length, Is.EqualTo(4));
            foreach (var target in visible)
            {
                Assert.That(((RectTransform)target.transform).rect.height, Is.GreaterThanOrEqualTo(44));
                Assert.That(CanHit(target, out _), Is.True, "Target must be reachable without scrolling: " + target.name);
            }
            Assert.That(CanHit(Button("Next targets"), out _), Is.True, "Private page controls fit without scrolling at safe height850.");
            yield return TapProduction("Next targets"); yield return null;
            yield return TapProduction("Action target person-6");
            yield return TapProduction("Player primary action");
            Assert.That(client.Commands.Single().choice, Is.EqualTo("both"));
            Assert.That(client.Commands.Single().targetIds, Is.EqualTo(new[] { "person-6" }));
            Assert.That(owner.GetComponentsInChildren<TMP_Text>().Any(t => !string.IsNullOrEmpty(t.text) && t.text.Contains("ancienne information")), Is.True);
        }

        [UnityTest]
        public IEnumerator WitchMustExplicitlyChooseBeforeConfirming()
        {
            client.View.phase = "night"; client.View.turn++; client.View.selfRoleId = "witch";
            client.View.action = new ActionView { kind = "witch", choices = new[] { "pass", "heal", "poison" }, allowedTargetIds = new[] { "two" }, healingAvailable = true, poisonAvailable = true };
            client.Refresh();
            flow.PrimaryAction.onClick.Invoke();
            Assert.That(flow.IsPrivateActionVisible, Is.True);
            Assert.That(flow.PrimaryAction.interactable, Is.False, "Opening a private power must not choose Pass automatically.");
            flow.PrimaryAction.onClick.Invoke();
            Assert.That(client.Commands, Is.Empty);
            Button("Action choice pass").onClick.Invoke();
            Assert.That(flow.PrimaryAction.interactable, Is.True);
            flow.PrimaryAction.onClick.Invoke();
            Assert.That(client.Commands.Single().choice, Is.EqualTo("pass"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator HostVoteClosureRequiresConfirmationAndCannotCloseAnotherTurn()
        {
            client.View.isHost = true; client.View.canAdvance = true; client.View.action.submitted = true;
            client.Refresh();
            flow.PrimaryAction.onClick.Invoke();
            Assert.That(client.Commands, Is.Empty, "The primary button opens a safe confirmation.");
            var confirm = Button("Confirm vote closure");
            client.View.turn++; client.View.phase = "night";
            confirm.onClick.Invoke();
            Assert.That(client.Commands, Is.Empty, "A stale confirmation cannot advance the next phase.");
            client.View.turn++; client.View.phase = "vote"; client.Refresh();
            flow.ShowVoteClosureConfirmation();
            Button("Confirm vote closure").onClick.Invoke();
            Assert.That(client.Commands.Single().kind, Is.EqualTo("advance"));
            Assert.That(client.Commands.Single().turn, Is.EqualTo(client.View.turn));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PublicChronicleContainsEveryPublicEventAndNoPrivateFact()
        {
            client.View.events = Enumerable.Range(1, 5).Select(i => new PublicEvent { sequence = i, text = "Public annonce " + i }).ToArray();
            client.View.privateFacts = new[] { new PrivateFact { description = "CONFIDENTIAL VOYANTE" } };
            flow.OpenPublicChronicle();
            var text = string.Join("\n", owner.GetComponentsInChildren<TMP_Text>().Select(item => item.text));
            Assert.That(text, Does.Contain("Public annonce 1").And.Contain("Public annonce 5"));
            Assert.That(text, Does.Not.Contain("CONFIDENTIAL VOYANTE"));
            Assert.That(client.Commands, Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemovingASeatRequiresExplicitConfirmationAndKeepsHost()
        {
            client.View.phase = "lobby"; client.View.turn++; client.View.revision++; client.View.isHost = true; client.View.hostPlayerId = "one";
            client.View.startBlockedReason = "Il faut au moins six joueurs.";
            client.Refresh();
            Assert.That(string.Join(" ", owner.GetComponentsInChildren<TMP_Text>().Select(item => item.text)), Does.Contain(client.View.startBlockedReason));
            flow.OpenParticipantManagement();
            Assert.That(owner.GetComponentsInChildren<UnityEngine.UI.Button>().Any(item => item.name == "Remove gathering player one"), Is.False);
            Button("Remove gathering player two").onClick.Invoke();
            Assert.That(client.Commands, Is.Empty);
            Button("Confirm participant removal").onClick.Invoke();
            Assert.That(client.Commands.Single().kind, Is.EqualTo("remove-player"));
            Assert.That(client.Commands.Single().targetIds, Is.EqualTo(new[] { "two" }));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnOpenPrivateDraftIsPurgedBeforeAnotherMatchCanReuseTheChannel()
        {
            client.View.phase = "vote"; client.View.turn++; client.View.revision++;
            client.View.players[0].alive = false; client.View.action = null;
            client.Communications.channels = new[] {
                new ChannelView { id = "village", name = "Village", canRead = true, canSend = false },
                new ChannelView { id = "observateurs", name = "Observateurs", canRead = true, canSend = true, isPrivate = true }
            };
            client.Refresh();
            Button("Open room chat").onClick.Invoke();
            Button("Conversation channel observateurs").onClick.Invoke();
            flow.ChatInput.text = "Brouillon des anciens observateurs";
            client.View.matchId = "second-match"; client.View.turn++; client.View.revision++;
            client.Communications.matchId = "second-match";
            client.Refresh();
            Assert.That(flow.IsChatOpen, Is.False);
            Button("Open room chat").onClick.Invoke();
            Button("Conversation channel observateurs").onClick.Invoke();
            Assert.That(flow.ChatInput.text, Is.Empty, "Closing an old field must not save it under the new match id.");
            Assert.That(client.SentChats, Is.Empty);
            yield return null;
        }
    }
}
