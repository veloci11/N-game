using System.Collections;
using System.Linq;
using NGame.Core;
using NGame.Core.Multiplayer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NGame.Presentation.Tests
{
    public sealed class NightFireflyActivityTests
    {
        private sealed class Store : IPreferenceStore
        {
            public bool ReadBool(string key, bool fallback) => fallback;
            public void WriteBool(string key, bool value) { }
            public void Save() { }
        }
        private GameObject owner;
        private AppPreferences preferences;
        private RoomView actor, observer;
        private NightFireflyActivity first, second;
        private bool blocked;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            owner = new GameObject("Common firefly activity fixture", typeof(RectTransform));
            preferences = new AppPreferences(new Store()); preferences.SetReducedMotion(false);
            actor = View(); actor.selfRoleId = "werewolf"; actor.action = new ActionView { kind = "wolves", submitted = true };
            observer = View(); observer.selfRoleId = "villager"; observer.action = null;
            first = Build("Actor's optional activity", () => actor);
            second = Build("Observer's optional activity", () => observer);
            yield return null;
        }
        private static RoomView View() => new RoomView { phase = "night", settings = new RoomSettings { nightProtocol = "screen" },
            serverTimeUnixMs = 120000, phaseStartedUnixMs = 100000 };
        private NightFireflyActivity Build(string name, System.Func<RoomView> view)
        {
            var child = new GameObject(name, typeof(RectTransform)); child.transform.SetParent(owner.transform, false);
            ((RectTransform)child.transform).sizeDelta = new Vector2(430, 240);
            var activity = child.AddComponent<NightFireflyActivity>(); activity.Initialize(preferences, view, () => blocked); return activity;
        }
        [UnityTearDown]
        public IEnumerator TearDown() { Object.Destroy(owner); yield return null; }

        [UnityTest]
        public IEnumerator ActorAndObserverHaveTheSamePublicScheduleEvenAfterConfirmationAndLocalTaps()
        {
            Assert.That(first.IsAvailable && second.IsAvailable, Is.True);
            for (var i = 0; i < 4; i++) Assert.That(Vector2.Distance(first.TargetPosition(i), second.TargetPosition(i)), Is.LessThan(.01f));
            var actorBefore = JsonUtility.ToJson(actor); var observerBefore = JsonUtility.ToJson(observer);
            Assert.That(first.RegisterTap(0), Is.True);
            Assert.That(first.LocalCatchCount, Is.EqualTo(1)); Assert.That(second.LocalCatchCount, Is.Zero);
            Assert.That(first.HasFlyingFireflies, Is.True);
            Assert.That(JsonUtility.ToJson(actor), Is.EqualTo(actorBefore), "An optional tap must never alter the accepted role action or room state.");
            Assert.That(JsonUtility.ToJson(observer), Is.EqualTo(observerBefore));
            yield return new WaitForSecondsRealtime(.2f);
            for (var i = 0; i < 4; i++) Assert.That(Vector2.Distance(first.TargetPosition(i), second.TargetPosition(i)), Is.LessThan(.01f));
            var targets = first.GetComponentsInChildren<Button>(); Assert.That(targets.Length, Is.EqualTo(4));
            foreach (var target in targets)
            {
                Assert.That(((RectTransform)target.transform).rect.size, Is.EqualTo(new Vector2(44, 44)));
                Assert.That(target.targetGraphic.raycastTarget, Is.True);
            }
            Assert.That(first.GetComponentsInChildren<Image>().Count(i => i.raycastTarget), Is.EqualTo(4), "Only explicit targets may receive a tap.");
            var lantern = first.GetComponentsInChildren<Image>().Single(i => i.name == "Anchored firefly lantern");
            Assert.That(lantern.sprite, Is.Not.Null); Assert.That(lantern.preserveAspect, Is.True);
        }

        [UnityTest]
        public IEnumerator AContactMovesOneVisibleInsectInsteadOfDuplicatingItAndLeavesOnlyNonInteractiveTraces()
        {
            Assert.That(first.RegisterTap(0), Is.True);
            var images = first.GetComponentsInChildren<Image>(true);
            var waiting = images.Single(i => i.name == "Waiting firefly light 0");
            var flying = images.Single(i => i.name == "Locally guided firefly 0");
            Assert.That(waiting.gameObject.activeSelf, Is.False, "The captured insect must leave its original perch.");
            Assert.That(flying.gameObject.activeInHierarchy, Is.True);
            Assert.That(flying.GetComponentsInChildren<Image>().Count(i => i.name.StartsWith("Separate paper firefly wing")), Is.EqualTo(2));
            yield return new WaitForSecondsRealtime(.19f);
            var traces = first.GetComponentsInChildren<Image>(true).Where(i => i.name.StartsWith("Local firefly flight trace")).ToArray();
            Assert.That(traces.Any(i => i.enabled), Is.True);
            Assert.That(traces.All(i => !i.raycastTarget), Is.True);
            Assert.That(first.GetComponentsInChildren<Image>(true).Count(i => i.raycastTarget), Is.EqualTo(4));
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(waiting.gameObject.activeSelf, Is.True);
            Assert.That(flying.gameObject.activeSelf, Is.False);
            Assert.That(traces.All(i => !i.enabled), Is.True);
        }

        [UnityTest]
        public IEnumerator ActivityFollowsVillageWorldPositionsAcrossCameraMovementWithoutRoomMutation()
        {
            var canvas = owner.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            yield return null;
            var world = new GameObject("Village anchor test world"); world.transform.SetParent(owner.transform, false); world.transform.position = Vector3.zero;
            var cameraObject = new GameObject("Village anchor test camera", typeof(Camera)); cameraObject.transform.SetParent(owner.transform, false);
            var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false; camera.orthographic = true; camera.orthographicSize = 4.66f; camera.aspect = 430f / 932;
            camera.transform.position = new Vector3(0,0,-10);
            first.BindVillage(camera, world.transform);
            Assert.That(first.IsSceneAnchored, Is.True);
            var lantern = first.LanternPosition; var fly = first.TargetPosition(0); var roomBefore = JsonUtility.ToJson(actor);
            camera.transform.position += new Vector3(.25f,0,0);
            yield return null;
            Assert.That(first.LanternPosition.x, Is.LessThan(lantern.x));
            Assert.That(first.TargetPosition(0).x, Is.LessThan(fly.x));
            Assert.That(first.LanternPosition.y, Is.EqualTo(lantern.y).Within(.01f));
            Assert.That(JsonUtility.ToJson(actor), Is.EqualTo(roomBefore));
            Assert.That(first.GetComponentsInChildren<Button>().All(b => ((RectTransform)b.transform).rect.size == new Vector2(44,44)), Is.True);
        }

        [UnityTest]
        public IEnumerator TraditionalProtocolMenusFocusAndPhaseChangesSuspendWithoutReplayingFlights()
        {
            actor.settings.nightProtocol = "traditional"; yield return null;
            Assert.That(first.IsAvailable, Is.False); Assert.That(first.RegisterTap(0), Is.False);
            actor.settings.nightProtocol = "screen"; yield return null; Assert.That(first.RegisterTap(0), Is.True);
            blocked = true; yield return null; Assert.That(first.IsAvailable, Is.False);
            blocked = false; yield return null; Assert.That(first.LocalCatchCount, Is.Zero); Assert.That(first.HasFlyingFireflies, Is.False);
            Assert.That(first.RegisterTap(1), Is.True); first.SendMessage("OnApplicationFocus", false);
            Assert.That(first.IsAvailable, Is.False); Assert.That(first.RegisterTap(2), Is.False);
            first.SendMessage("OnApplicationFocus", true); yield return null;
            Assert.That(first.LocalCatchCount, Is.Zero); Assert.That(first.HasFlyingFireflies, Is.False);
            first.SendMessage("OnApplicationPause", true); Assert.That(first.RegisterTap(0), Is.False);
            first.SendMessage("OnApplicationPause", false); yield return null;
            actor.phase = "vote"; yield return null; Assert.That(first.RegisterTap(0), Is.False);
            actor.phase = "night"; actor.phaseStartedUnixMs = 121000; actor.serverTimeUnixMs = 122000; yield return null;
            Assert.That(first.LocalCatchCount, Is.Zero); Assert.That(first.HasFlyingFireflies, Is.False);
        }

        [UnityTest]
        public IEnumerator ReducedMotionKeepsStaticTouchableTargetsAndUsesLightFeedbackWithoutFlights()
        {
            preferences.SetReducedMotion(true); yield return null;
            var position = first.TargetPosition(1);
            Assert.That(first.RegisterTap(1), Is.True); Assert.That(first.HasFlyingFireflies, Is.False);
            Assert.That(first.LocalCatchCount, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(first.TargetPosition(1), Is.EqualTo(position));
            Assert.That(first.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Locally guided firefly")).All(i => !i.enabled), Is.True);
        }
    }
}
