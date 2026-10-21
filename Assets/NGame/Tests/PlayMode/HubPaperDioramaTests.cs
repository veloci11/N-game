using System.Collections;
using System.Linq;
using NGame.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NGame.Presentation.Tests
{
    public sealed class HubPaperDioramaTests
    {
        private sealed class Store : IPreferenceStore
        {
            public bool ReadBool(string key, bool fallback) => fallback;
            public void WriteBool(string key, bool value) { }
            public void Save() { }
        }
        private GameObject owner;
        private HubPaperDiorama stage;
        private AppPreferences preferences;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            owner = new GameObject("Independent paper stage fixture", typeof(RectTransform));
            ((RectTransform)owner.transform).sizeDelta = new Vector2(430, 334);
            preferences = new AppPreferences(new Store()); preferences.SetReducedMotion(false);
            stage = owner.AddComponent<HubPaperDiorama>(); stage.Initialize(preferences);
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown() { Object.Destroy(owner); yield return null; }

        [UnityTest]
        public IEnumerator EveryCutoutHasRealTransparencyAndCannotInterceptControls()
        {
            Assert.That(stage.LoadedLayerCount, Is.EqualTo(8), "All generated cutouts must be imported before shipping the paper stage.");
            var images = owner.GetComponentsInChildren<Image>();
            Assert.That(images.Length, Is.EqualTo(8));
            foreach (var image in images)
            {
                Assert.That(image.enabled, Is.True);
                Assert.That(image.raycastTarget, Is.False);
                Assert.That(image.preserveAspect, Is.True);
                Assert.That(image.material, Is.SameAs(Graphic.defaultGraphicMaterial), "No UV-deformation material may be used.");
            }
            Assert.That(owner.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            foreach (var texture in images.Select(i => i.sprite.texture).Distinct()) AssertRealAlpha(texture);
            Assert.That(stage.FindLayer("wolf-head").transform.GetSiblingIndex(), Is.LessThan(stage.FindLayer("book-castle").transform.GetSiblingIndex()), "The book hides the lower wolf cutout seam.");
            yield return null;
        }

        private static void AssertRealAlpha(Texture texture)
        {
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32);
            var sample = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(texture, target); RenderTexture.active = target;
                sample.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); sample.Apply();
                var pixels = sample.GetPixels32();
                Assert.That(pixels.Any(p => p.a < 8), Is.True, texture.name + " must have transparent outside pixels.");
                Assert.That(pixels.Any(p => p.a > 240), Is.True, texture.name + " must contain visible paper artwork.");
            }
            finally
            { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Object.Destroy(sample); }
        }

        [UnityTest]
        public IEnumerator HeadAndCloudMoveIndependentlyWhileBookAndBodyStayStill()
        {
            var book = stage.FindLayer("book-castle").rectTransform;
            var body = stage.FindLayer("wolf-body").rectTransform;
            var head = stage.FindLayer("wolf-head").rectTransform;
            var cloud = stage.FindLayer("cloud-left").rectTransform;
            var bookPosition = book.anchoredPosition; var bodyPosition = body.anchoredPosition;
            var bookAngle = book.localRotation; var bodyAngle = body.localRotation;
            var headAngle = head.localEulerAngles.z; var cloudPosition = cloud.anchoredPosition;
            yield return new WaitForSecondsRealtime(.45f);
            Assert.That(book.anchoredPosition, Is.EqualTo(bookPosition));
            Assert.That(body.anchoredPosition, Is.EqualTo(bodyPosition));
            Assert.That(book.localRotation, Is.EqualTo(bookAngle));
            Assert.That(body.localRotation, Is.EqualTo(bodyAngle));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(headAngle, head.localEulerAngles.z)), Is.GreaterThan(.001f));
            Assert.That(Vector2.Distance(cloudPosition, cloud.anchoredPosition), Is.GreaterThan(.001f));
            ((RectTransform)owner.transform).sizeDelta = new Vector2(430, 220); yield return null;
            foreach (var image in owner.GetComponentsInChildren<Image>())
            {
                Assert.That(image.rectTransform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(image.rectTransform.rect.width, Is.EqualTo(image.rectTransform.rect.height).Within(.001f), "Uniform image boxes retain proportions when the hero area changes.");
                Assert.That(image.preserveAspect, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator ReducedMotionFreezesTheClockAndReturnsTheCutoutsToRest()
        {
            yield return new WaitForSecondsRealtime(.15f);
            preferences.SetReducedMotion(true); yield return null;
            var clock = stage.AnimationClock;
            var head = stage.FindLayer("wolf-head").rectTransform;
            var cloud = stage.FindLayer("cloud-left").rectTransform;
            Assert.That(head.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(cloud.anchoredPosition, Is.EqualTo(Vector2.zero));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(stage.AnimationClock, Is.EqualTo(clock));
            Assert.That(head.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(cloud.anchoredPosition, Is.EqualTo(Vector2.zero));
        }
    }
}
