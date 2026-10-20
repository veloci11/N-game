using System.Collections;
using System.Linq;
using NGame.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NGame.Presentation.Tests
{
    public sealed class HubBackdropTests
    {
        private sealed class Store : IPreferenceStore
        {
            public bool ReadBool(string key, bool fallback) => false;
            public void WriteBool(string key, bool value) { }
            public void Save() { }
        }
        private GameObject owner;
        private HubBackdrop backdrop;
        private AppPreferences preferences;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            owner = new GameObject("Universal hub backdrop fixture", typeof(RectTransform), typeof(Canvas));
            owner.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var safe = new GameObject("Backdrop viewport", typeof(RectTransform));
            safe.transform.SetParent(owner.transform, false);
            var rect = (RectTransform)safe.transform;
            rect.sizeDelta = new Vector2(430, 932);
            preferences = new AppPreferences(new Store());
            backdrop = safe.AddComponent<HubBackdrop>();
            backdrop.Initialize(preferences);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        { if (owner != null) Object.Destroy(owner); yield return null; }

        [UnityTest]
        public IEnumerator IllustrationCoversViewportOverOpaquePaperAndNeverBlocksInput()
        {
            Canvas.ForceUpdateCanvases();
            var art = Image("Universal storybook illustration");
            Assert.IsNotNull(art.sprite, "The general app illustration must be installed as a resource");
            Assert.IsTrue(art.enabled && art.gameObject.activeInHierarchy);
            Assert.IsFalse(art.raycastTarget);
            var basePaper = Image("Warm paper base");
            Assert.AreEqual(1f, basePaper.color.a);
            Assert.Greater(basePaper.color.r, .8f);
            Assert.Greater(basePaper.color.g, .7f);
            Assert.Greater(basePaper.color.b, .6f);
            Assert.AreEqual(Vector2.zero, basePaper.rectTransform.anchorMin);
            Assert.AreEqual(Vector2.one, basePaper.rectTransform.anchorMax);
            Assert.AreEqual(Vector2.zero, basePaper.rectTransform.offsetMin);
            Assert.AreEqual(Vector2.zero, basePaper.rectTransform.offsetMax);
            var viewport = ((RectTransform)backdrop.transform).rect.size;
            Assert.GreaterOrEqual(art.rectTransform.rect.width, viewport.x);
            Assert.GreaterOrEqual(art.rectTransform.rect.height, viewport.y);
            Assert.AreEqual(art.sprite.rect.width / art.sprite.rect.height,
                art.rectTransform.rect.width / art.rectTransform.rect.height, .001f);
            Assert.IsTrue(owner.GetComponentsInChildren<Image>().All(image => !image.raycastTarget));
            var group = owner.GetComponentInChildren<CanvasGroup>();
            Assert.IsFalse(group.blocksRaycasts);
            Assert.IsFalse(group.interactable);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AmbientMotionChangesRealDecorationPosesAndStopsWithReducedMotion()
        {
            var paperShader = Resources.Load<Shader>("Hub/HubPaper");
            Assert.IsNotNull(paperShader);
            Assert.IsTrue(paperShader.isSupported, "Living paper shader must be supported by the active renderer");
            Assert.IsTrue(backdrop.PaperMotionEnabled);
            Assert.GreaterOrEqual(backdrop.MovingDecorationCount, 8);
            var leaf = Image("Folded paper leaf 0").rectTransform;
            var initial = leaf.anchoredPosition;
            var clock = backdrop.AmbientClock;
            yield return new WaitForSecondsRealtime(.22f);
            Assert.Greater(backdrop.AmbientClock, clock + .1f);
            Assert.Greater(Vector2.Distance(initial, leaf.anchoredPosition), .01f);
            preferences.SetReducedMotion(true);
            Assert.IsFalse(backdrop.PaperMotionEnabled);
            yield return null;
            yield return null;
            var stillClock = backdrop.AmbientClock;
            var stillPosition = leaf.anchoredPosition;
            var stillRotation = leaf.localRotation;
            yield return new WaitForSecondsRealtime(.22f);
            Assert.AreEqual(stillClock, backdrop.AmbientClock);
            Assert.AreEqual(stillPosition, leaf.anchoredPosition);
            Assert.AreEqual(stillRotation, leaf.localRotation);
        }

        [UnityTest]
        public IEnumerator BothGameIconsMoveTheirOwnPaperPartsAndRespectReducedMotion()
        {
            var cardObject = new GameObject("President icon fixture", typeof(RectTransform));
            var maskObject = new GameObject("Mimes icon fixture", typeof(RectTransform));
            cardObject.transform.SetParent(owner.transform, false);
            maskObject.transform.SetParent(owner.transform, false);
            var cards = cardObject.AddComponent<HubGameIcon>();
            var masks = maskObject.AddComponent<HubGameIcon>();
            cards.Initialize(preferences, true);
            masks.Initialize(preferences, false);
            Assert.AreEqual(4, cards.MovingPartCount);
            Assert.AreEqual(2, masks.MovingPartCount);
            var paperParts = cardObject.GetComponentsInChildren<RectTransform>()
                .Concat(maskObject.GetComponentsInChildren<RectTransform>())
                .Where(rect => rect.name.StartsWith("Animated ")).ToArray();
            Assert.AreEqual(6, paperParts.Length);
            var poses = paperParts.Select(rect => rect.anchoredPosition).ToArray();
            var turns = paperParts.Select(rect => rect.localRotation).ToArray();
            yield return new WaitForSecondsRealtime(.22f);
            Assert.Greater(cards.AnimationClock, .1f);
            Assert.Greater(masks.AnimationClock, .1f);
            for (var i = 0; i < paperParts.Length; i++)
                Assert.IsTrue(Vector2.Distance(poses[i], paperParts[i].anchoredPosition) > .01f ||
                    Quaternion.Angle(turns[i], paperParts[i].localRotation) > .1f,
                    paperParts[i].name + " must animate independently");
            Assert.IsTrue(cardObject.GetComponentsInChildren<Image>().Concat(maskObject.GetComponentsInChildren<Image>())
                .All(image => !image.raycastTarget));
            preferences.SetReducedMotion(true);
            yield return null;
            yield return null;
            var cardClock = cards.AnimationClock; var maskClock = masks.AnimationClock;
            poses = paperParts.Select(rect => rect.anchoredPosition).ToArray();
            turns = paperParts.Select(rect => rect.localRotation).ToArray();
            yield return new WaitForSecondsRealtime(.22f);
            Assert.AreEqual(cardClock, cards.AnimationClock);
            Assert.AreEqual(maskClock, masks.AnimationClock);
            for (var i = 0; i < paperParts.Length; i++)
            {
                Assert.AreEqual(poses[i], paperParts[i].anchoredPosition);
                Assert.AreEqual(turns[i], paperParts[i].localRotation);
            }
        }

        private Image Image(string name) => owner.GetComponentsInChildren<Image>(true).Single(image => image.name == name);
    }
}
