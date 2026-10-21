using NGame.Core;
using UnityEngine;

namespace NGame.Presentation
{
    /// <summary>One illustrated catalogue vignette, cropped from the shared three-column paper atlas.</summary>
    public sealed class HubCatalogueThumbnail : MonoBehaviour
    {
        private AppPreferences preferences;
        private UnityEngine.UI.Image illustration;
        private Sprite ownedSprite;
        private int index;
        private float clock, nextAttempt;
        private bool initialized;
        public float AnimationClock => clock;

        public void Initialize(AppPreferences settings, int atlasIndex)
        {
            if (initialized) return;
            initialized = true;
            preferences = settings; index = Mathf.Clamp(atlasIndex, 0, 2);
            gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            illustration = UITheme.Surface("Hub illustrated catalogue vignette " + index, transform, Color.white);
            UITheme.Position(illustration.rectTransform, UITheme.Center, 0, 0, 112, 112);
            illustration.enabled = false;
            TryLoad();
            Render();
        }

        private void TryLoad()
        {
            if (illustration == null || ownedSprite != null) return;
            var atlas = Resources.Load<Texture2D>("Hub/catalogue-storybook");
            if (atlas == null) return;
            var width = atlas.width / 3f;
            ownedSprite = Sprite.Create(atlas, new Rect(width * index, 0, width, atlas.height),
                Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            ownedSprite.name = "Storybook catalogue illustration " + index;
            illustration.sprite = ownedSprite;
            illustration.enabled = true;

        }

        private void LateUpdate()
        {
            if (!initialized || illustration == null) return;
            if (preferences == null || !preferences.ReducedMotion) clock += Time.unscaledDeltaTime;
            if (ownedSprite == null && Time.unscaledTime >= nextAttempt)
            { nextAttempt = Time.unscaledTime + 1; TryLoad(); }
            Render();
        }

        private void Render()
        {
            if (illustration == null) return;
            var reduced = preferences != null && preferences.ReducedMotion;
            var rect = ((RectTransform)transform).rect;
            // Square illustration covers the thumbnail window; no stretching or atlas bleed.
            var extent = Mathf.Max(rect.width, rect.height) + 5;
            illustration.rectTransform.sizeDelta = Vector2.one * extent;
            illustration.rectTransform.anchoredPosition = reduced ? Vector2.zero :
                new Vector2(Mathf.Sin(clock * .6f + index) * .6f, Mathf.Sin(clock * .8f + index) * 1.2f);
            illustration.rectTransform.localRotation = Quaternion.Euler(0, 0,
                reduced ? 0 : Mathf.Sin(clock * .5f + index) * .7f);
        }

        private void OnDestroy()
        {
            if (ownedSprite != null) Destroy(ownedSprite);
        }
    }
}
