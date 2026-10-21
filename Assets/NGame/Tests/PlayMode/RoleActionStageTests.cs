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
    public sealed class RoleActionStageTests
    {
        private sealed class Store : IPreferenceStore
        {
            public bool ReadBool(string key, bool fallback) => fallback;
            public void WriteBool(string key, bool value) { }
            public void Save() { }
        }
        private GameObject owner;
        private RoleActionStage stage;
        private AppPreferences preferences;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            owner = new GameObject("Private role action scenery fixture", typeof(RectTransform));
            ((RectTransform)owner.transform).sizeDelta = new Vector2(320, 80);
            preferences = new AppPreferences(new Store()); preferences.SetReducedMotion(false);
            stage = owner.AddComponent<RoleActionStage>(); stage.Initialize(preferences); yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown() { Object.Destroy(owner); yield return null; }

        [UnityTest]
        public IEnumerator EveryBaseRoleHasItsOwnNonInteractiveSceneryWithoutCopyingSecretData()
        {
            var roles = new[] { "werewolf", "seer", "witch", "guard", "cupid", "hunter", "little-girl", "villager" };
            foreach (var role in roles)
            {
                stage.Bind(new ActionView { kind = role == "werewolf" ? "wolves" : "observe", privateInfo = "SECRET_PLAYER_IDENTITY" }, role, false);
                yield return null;
                Assert.That(stage.VisualKind, Is.EqualTo(role));
                Assert.That(stage.IsBound, Is.True);
                Assert.That(owner.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                var images = owner.GetComponentsInChildren<Image>(); Assert.That(images.Length, Is.GreaterThan(8));
                foreach (var image in images)
                {
                    Assert.That(image.raycastTarget, Is.False);
                    Assert.That(image.material, Is.SameAs(Graphic.defaultGraphicMaterial), "Action scenery must not deform an image through a shader.");
                }
                Assert.That(owner.GetComponentsInChildren<TMPro.TMP_Text>(), Is.Empty, "No role facts, target identity or result text is copied into decoration.");
            }
        }

        [UnityTest]
        public IEnumerator OrbStarsOrbitButTheSeparatelyDrawnSphereStaysStill()
        {
            stage.Bind(new ActionView { kind = "seer" }, "seer", false); yield return null;
            var star = owner.GetComponentsInChildren<RectTransform>().Single(t => t.name == "Separate orbit star 0");
            var original = star.anchoredPosition;
            var stationary = owner.GetComponentsInChildren<Image>().First(i => i.name == "Crystal sphere" || i.name == "Separate illustrated seer-orb").rectTransform;
            var stationaryPosition = stationary.anchoredPosition; var stationaryScale = stationary.localScale;
            yield return new WaitForSecondsRealtime(.22f);
            Assert.That(Vector2.Distance(original, star.anchoredPosition), Is.GreaterThan(.1f));
            Assert.That(stationary.anchoredPosition, Is.EqualTo(stationaryPosition));
            Assert.That(stationary.localScale, Is.EqualTo(stationaryScale));
        }

        [UnityTest]
        public IEnumerator AReceiptTriggersOnlyOnANewConfirmationNotEveryProjectionRefresh()
        {
            var action = new ActionView { kind = "witch" };
            stage.Bind(action, "witch", true);
            Assert.That(stage.ConfirmationVersion, Is.Zero, "Opening a previously submitted power must not pretend it was just submitted.");
            stage.Bind(action, "witch", false); stage.Bind(action, "witch", true);
            Assert.That(stage.ConfirmationVersion, Is.EqualTo(1));
            for (var i = 0; i < 5; i++) stage.Bind(action, "witch", true);
            Assert.That(stage.ConfirmationVersion, Is.EqualTo(1));
            stage.Bind(action, "witch", true, "another-match/turn-3");
            Assert.That(stage.ConfirmationVersion, Is.EqualTo(1), "A different match/turn starts a fresh receipt latch without replaying an old submission.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator WitchReceiptUsesOnlyTheAcceptedPotionAndStopsWhenTheStageCloses()
        {
            var action = new ActionView { kind = "witch", submittedChoice = "save", healingAvailable = false, poisonAvailable = true };
            stage.Bind(action, "witch", false, "save-turn");
            var light = owner.GetComponentsInChildren<Image>().Single(i => i.name == "Action receipt light 0");
            Assert.That(light.color.a, Is.Zero, "An unaccepted selection must not show a potion receipt.");
            stage.Bind(action, "witch", true, "save-turn");
            Assert.That(light.color.g, Is.GreaterThan(light.color.r)); Assert.That(light.color.a, Is.GreaterThan(0));
            var healingBottle = owner.GetComponentsInChildren<RectTransform>().Single(t => t.name == "Separate potion bottle -34");
            var grey = healingBottle.GetComponentInChildren<Image>().color;
            Assert.That(grey.r, Is.EqualTo(grey.g).Within(.001f)); Assert.That(grey.a, Is.LessThan(.5f));
            owner.SetActive(false); owner.SetActive(true); yield return null;
            Assert.That(light.color.a, Is.Zero, "Returning to the private stage must not replay an old receipt.");
            action.submittedChoice = "both"; stage.Bind(action, "witch", false, "both-turn"); stage.Bind(action, "witch", true, "both-turn");
            var otherLight = owner.GetComponentsInChildren<Image>().Single(i => i.name == "Action receipt light 1");
            Assert.That(light.color.g, Is.GreaterThan(light.color.r)); Assert.That(otherLight.color.r, Is.GreaterThan(otherLight.color.g));
            action.submittedChoice = "pass"; stage.Bind(action, "witch", false, "pass-turn"); stage.Bind(action, "witch", true, "pass-turn");
            Assert.That(light.color.a, Is.Zero, "Passing must not pretend that a potion was used.");
        }

        [UnityTest]
        public IEnumerator SelectionSettlesTheHunterAimButAPassNeverLaunchesTheBolt()
        {
            var action = new ActionView { kind = "hunter", submittedChoice = "pass" };
            stage.Bind(action, "hunter", false, "hunter-turn"); stage.SetSelection("shoot", 1, 1); yield return null;
            var aim = owner.GetComponentsInChildren<RectTransform>().Single(t => t.name == "Independent aiming reticle");
            var bolt = owner.GetComponentsInChildren<RectTransform>().Single(t => t.name == "Separate hunter bolt");
            var aimPosition = aim.anchoredPosition; var boltPosition = bolt.anchoredPosition;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(aim.anchoredPosition, Is.EqualTo(aimPosition), "Selecting a target settles the reticle instead of keeping it drifting.");
            stage.SetSelection("pass", 0, 0); stage.Bind(action, "hunter", true, "hunter-turn");
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(bolt.anchoredPosition, Is.EqualTo(boltPosition), "An accepted Pass is not a shot.");
            Assert.That(owner.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Action receipt light")).All(i => i.color.a == 0), Is.True);
        }

        [UnityTest]
        public IEnumerator CupidPreviewUsesOnlyTheNumberOfChosenTargetsAndStaysNonInteractive()
        {
            stage.Bind(new ActionView { kind = "cupid" }, "cupid", false); stage.SetSelection("link", 1, 2);
            var first = owner.GetComponentsInChildren<RectTransform>().Single(t => t.name == "First linked heart");
            var second = owner.GetComponentsInChildren<RectTransform>().Single(t => t.name == "Second linked heart");
            var thread = owner.GetComponentsInChildren<Image>().Single(i => i.name == "Lovers connecting thread");
            Assert.That(first.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
            Assert.That(second.GetComponent<CanvasGroup>().alpha, Is.LessThan(.5f)); var pendingAlpha = thread.color.a;
            stage.SetSelection("link", 2, 2);
            Assert.That(second.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1)); Assert.That(thread.color.a, Is.GreaterThan(pendingAlpha));
            Assert.That(owner.GetComponentsInChildren<Button>(), Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FocusAndPauseHideTheEntirePrivateSceneryUntilAnExplicitRebind()
        {
            var action = new ActionView { kind = "seer", submittedChoice = "inspect" };
            stage.Bind(action, "seer", false); stage.SetSelection("inspect", 1, 1); stage.Bind(action, "seer", true);
            var group = owner.GetComponent<CanvasGroup>(); Assert.That(group.alpha, Is.EqualTo(1));
            owner.SendMessage("OnApplicationFocus", false); Assert.That(group.alpha, Is.Zero);
            var clock = stage.AnimationClock;
            owner.SendMessage("OnApplicationFocus", true); yield return null;
            Assert.That(group.alpha, Is.Zero, "Focus return is not permission to reveal the private pane.");
            Assert.That(stage.AnimationClock, Is.EqualTo(clock));
            stage.Bind(action, "seer", true); Assert.That(group.alpha, Is.EqualTo(1));
            Assert.That(stage.ConfirmationVersion, Is.EqualTo(1), "Rebinding a received action must not replay its receipt.");
            owner.SendMessage("OnApplicationPause", true); owner.SendMessage("OnApplicationPause", false); yield return null;
            Assert.That(group.alpha, Is.Zero);
        }

        [UnityTest]
        public IEnumerator VillagerUsesTheExistingLanternAndTheLittleGirlHasNoDigitalPowerControls()
        {
            stage.Bind(null, "villager", false); yield return null;
            var lantern = owner.GetComponentsInChildren<Image>().Single(i => i.name == "Existing illustrated villager lantern" || i.name == "Separate illustrated villager-lantern");
            Assert.That(lantern.sprite, Is.Not.Null); Assert.That(lantern.preserveAspect, Is.True);
            stage.Bind(new ActionView { kind = "observe", submitted = true, privateInfo = "HIDDEN WOLF IDENTITY" }, "little-girl", true);
            stage.SetSelection(null, 0, 0); yield return null;
            Assert.That(owner.GetComponentsInChildren<Button>(), Is.Empty);
            Assert.That(owner.GetComponentsInChildren<TMPro.TMP_Text>(), Is.Empty);
            Assert.That(owner.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Action receipt light")).All(i => i.color.a == 0), Is.True);
        }

        [UnityTest]
        public IEnumerator ReducedMotionStopsRoleMovementAndTheConfirmationBurst()
        {
            stage.Bind(new ActionView { kind = "seer" }, "seer", false);
            yield return new WaitForSecondsRealtime(.1f);
            preferences.SetReducedMotion(true); stage.NotifyConfirmed(); yield return null;
            var clock = stage.AnimationClock;
            var stars = owner.GetComponentsInChildren<RectTransform>().Where(t => t.name.StartsWith("Separate orbit star")).ToArray();
            var positions = stars.Select(t => t.anchoredPosition).ToArray();
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(stage.AnimationClock, Is.EqualTo(clock));
            for (var i = 0; i < stars.Length; i++) Assert.That(stars[i].anchoredPosition, Is.EqualTo(positions[i]));
            foreach (var light in owner.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Action receipt light"))) Assert.That(light.color.a, Is.Zero);
        }
    }
}
