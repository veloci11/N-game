using System.Collections.Generic;
using NGame.Core;
using UnityEngine;

namespace NGame.Presentation
{
    /// <summary>Universal paper-storybook ambience. This surface never receives player input.</summary>
    public sealed class HubBackdrop : MonoBehaviour
    {
        private sealed class Decoration
        {
            public RectTransform Rect;
            public UnityEngine.UI.Image Image;
            public Vector2 Rest;
            public float Angle, Phase, Amplitude;
            public Color Color;
            public bool Spark;
        }

        public float AmbientClock => ambientClock;
        public int MovingDecorationCount => decorations.Count;
        private readonly List<Decoration> decorations = new List<Decoration>();
        private AppPreferences preferences;
        private UnityEngine.UI.Image illustration, light;
        private RectTransform content;
        private Sprite ownedSprite;
        private RectTransform safeFrame;
        private bool hasSavedRoom, choosingGame;
        private RectTransform hero;
        public HubPaperDiorama Diorama { get; private set; }
        public bool PaperMotionEnabled => Diorama != null && !ReducedMotion;
        private float ambientClock, nextAssetAttempt;
        private bool initialized;
        private bool ReducedMotion => preferences != null && preferences.ReducedMotion;

        public void Initialize(AppPreferences settings)
        {
            preferences = settings;
            if (initialized && content != null) return;
            initialized = true;
            decorations.Clear();
            var previous = transform.Find("Universal storybook backdrop layers");
            if (previous != null) { previous.gameObject.SetActive(false); Destroy(previous.gameObject); }
            content = UITheme.Box("Universal storybook backdrop layers", transform, true);
            var input = content.gameObject.AddComponent<CanvasGroup>();
            input.interactable = false; input.blocksRaycasts = false;
            content.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            UITheme.Surface("Warm paper base", content, UITheme.Parchment, null, true);
            illustration = UITheme.Surface("Universal storybook illustration", content, Color.white);
            UITheme.Position(illustration.rectTransform, UITheme.Center, 0, 0, 430, 932);
            illustration.enabled = false;
            hero = UITheme.Box("Independent paper diorama", content);
            Diorama = hero.gameObject.AddComponent<HubPaperDiorama>();
            Diorama.Initialize(preferences);
            light = UITheme.Surface("Warm breathing paper light", content, UITheme.Opacity(Color.white, .035f), UITheme.Mist);
            UITheme.Position(light.rectTransform, new Vector2(.5f, 1), 0, -85, 700, 340);
            for (var i = 0; i < 8; i++) AddDecoration(i, false);
            for (var i = 0; i < 10; i++) AddDecoration(i, true);
            TryLoadIllustration();
            Render();
        }

        public void SetContentFrame(RectTransform frame) => safeFrame = frame;
        public void SetCatalogueState(bool savedRoom, bool choosing)
        { hasSavedRoom = savedRoom; choosingGame = choosing; }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        private void AddDecoration(int index, bool spark)
        {
            var left = index % 2 == 0;
            var image = UITheme.Surface((spark ? "Gold paper glint " : "Folded paper leaf ") + index, content,
                UITheme.Opacity(spark ? UITheme.Brass : index % 3 == 0 ? UITheme.Terracotta : UITheme.Brass,
                    spark ? .45f : .3f), spark ? UITheme.Disc : UITheme.Panel);
            var rest = new Vector2(left ? 12 + index % 3 * 5 : -12 - index % 3 * 5, 0);
            UITheme.Position(image.rectTransform, new Vector2(left ? 0 : 1, .16f + index * (spark ? .075f : .095f)),
                rest.x, rest.y, spark ? 2.5f : 9, spark ? 2.5f : 21);
            var angle = left ? -35f + index * 7 : 35f - index * 7;
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            decorations.Add(new Decoration { Rect = image.rectTransform, Image = image, Rest = rest,
                Angle = angle, Phase = index * 1.73f, Amplitude = spark ? 5 : 3,
                Color = image.color, Spark = spark });
            if (!spark)
            {
                var crease = UITheme.Surface("Paper leaf fold", image.transform, UITheme.Opacity(UITheme.Parchment, .6f));
                UITheme.Position(crease.rectTransform, UITheme.Center, 0, 0, .7f, 16);
            }
        }

        private void TryLoadIllustration()
        {
            if (illustration == null || illustration.sprite != null) return;
            var source = Resources.Load<Sprite>("Hub/storybook-hub");
            var texture = source != null ? source.texture : Resources.Load<Texture2D>("Hub/storybook-hub");
            if (texture == null) return;
            // Use the clear top of the approved-style artwork as paper, and its middle as a separate pop-up layer.
            var paperBottom = Mathf.FloorToInt(texture.height * .80f);
            ownedSprite = Sprite.Create(texture, new Rect(0, paperBottom, texture.width, texture.height - paperBottom),
                Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            illustration.sprite = ownedSprite;
            illustration.enabled = true;
        }

        private void LateUpdate()
        {
            if (!initialized || content == null) return;
            if (!ReducedMotion) ambientClock += Time.unscaledDeltaTime;
            if (illustration != null && illustration.sprite == null && Time.unscaledTime >= nextAssetAttempt)
            { nextAssetAttempt = Time.unscaledTime + 1; TryLoadIllustration(); }
            Render();
        }

        private void Render()
        {
            var clock = ReducedMotion ? 0 : ambientClock;
            if (illustration != null)
            {
                var bounds = content.rect.size;
                var sprite = illustration.sprite;
                if (sprite != null)
                {
                    // Cover the entire viewport without stretching; the masked overscan allows gentle parallax.
                    var source = sprite.rect.size;
                    var cover = Mathf.Max((bounds.x + 20) / source.x, (bounds.y + 20) / source.y);
                    illustration.rectTransform.sizeDelta = source * cover;
                }
                illustration.rectTransform.anchoredPosition = Vector2.zero;
            }
            if (hero != null)
            {
                var frame = safeFrame != null ? safeFrame : (RectTransform)transform;
                var rowBottom = hasSavedRoom && !choosingGame ? 300f : 260f;
                var available = Mathf.Max(80, frame.rect.height - rowBottom - 298f);
                var topCenter = content.InverseTransformPoint(frame.TransformPoint(new Vector3(frame.rect.center.x, frame.rect.yMax, 0)));
                UITheme.Position(hero, UITheme.Center, topCenter.x,
                    topCenter.y - 200 - available * .5f, frame.rect.width, available);
            }
            if (light != null) light.color = UITheme.Opacity(Color.white,
                ReducedMotion ? .035f : .035f + Mathf.Sin(clock * .47f) * .015f);
            foreach (var item in decorations)
            {
                if (item.Rect == null || item.Image == null) continue;
                item.Rect.anchoredPosition = item.Rest + (ReducedMotion ? Vector2.zero :
                    new Vector2(Mathf.Sin(clock * .38f + item.Phase) * 2,
                        Mathf.Sin(clock * .55f + item.Phase) * item.Amplitude));
                item.Rect.localRotation = Quaternion.Euler(0, 0, item.Angle +
                    (ReducedMotion ? 0 : Mathf.Sin(clock * .65f + item.Phase) * 9));
                item.Image.color = UITheme.Opacity(item.Color, item.Color.a * (ReducedMotion ? 1 :
                    .8f + Mathf.Sin(clock * (item.Spark ? 1.1f : .4f) + item.Phase) * .2f));
            }
        }

        private void OnDestroy()
        {
            if (ownedSprite != null) Destroy(ownedSprite);
        }
    }
}
