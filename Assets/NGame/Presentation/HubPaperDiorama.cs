using System.Collections.Generic;
using NGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NGame.Presentation
{
    /// <summary>A paper stage made from independent transparent cutouts. No image deformation.</summary>
    public sealed class HubPaperDiorama : MonoBehaviour
    {
        private enum Motion { Still, Cloud, Head, Foliage }
        private sealed class Layer
        {
            public string Id, Asset;
            public Image Image;
            public Sprite OwnedSprite;
            public Vector2 Position, Pivot;
            public float Extent, Angle, Phase;
            public Motion Motion;
        }

        private readonly List<Layer> layers = new List<Layer>();
        private AppPreferences preferences;
        private RectTransform frame;
        private bool initialized;
        private float clock, nextLoadAttempt;
        public float AnimationClock => clock;
        public int LoadedLayerCount
        {
            get { var count = 0; foreach (var layer in layers) if (layer.Image.sprite != null) count++; return count; }
        }
        public bool ReducedMotion => preferences != null && preferences.ReducedMotion;

        public void Initialize(AppPreferences settings)
        {
            preferences = settings;
            if (initialized) return;
            initialized = true;
            frame = (RectTransform)transform;
            var input = gameObject.GetComponent<CanvasGroup>();
            if (input == null) input = gameObject.AddComponent<CanvasGroup>();
            input.interactable = false; input.blocksRaycasts = false;
            // Front-to-back order is deliberate: clouds disappear behind the castle silhouette.
            Add("cloud-left", "cloud", new Vector2(.22f, .74f), .48f, Vector2.one * .5f, Motion.Cloud, .4f);
            Add("cloud-right", "cloud", new Vector2(.74f, .83f), .38f, Vector2.one * .5f, Motion.Cloud, 2.6f);
            Add("wolf-body", "wolf-body", new Vector2(.73f, .25f), .52f, Vector2.one * .5f, Motion.Still);
            Add("wolf-head", "wolf-head", new Vector2(.74f, .46f), .67f, new Vector2(.6f, .16f), Motion.Head, .6f);
            Add("book-castle", "book-castle", new Vector2(.50f, .46f), 1.18f, Vector2.one * .5f, Motion.Still);
            Add("foliage-left", "foliage", new Vector2(.08f, .08f), .32f, new Vector2(.5f, 0), Motion.Foliage, .2f);
            Add("foliage-right", "foliage", new Vector2(.91f, .08f), .30f, new Vector2(.5f, 0), Motion.Foliage, 2.1f);
            Add("crown", "crown", new Vector2(.77f, .14f), .40f, Vector2.one * .5f, Motion.Still);
            TryLoadAssets(); Render();
        }

        /// <summary>Positions are normalized within the available stage. Extent uses its shortest side.</summary>
        public bool SetLayerLayout(string id, Vector2 position, float extent, Vector2 pivot, float angle = 0)
        {
            foreach (var layer in layers)
            {
                if (layer.Id != id) continue;
                layer.Position = position; layer.Extent = Mathf.Max(.01f, extent);
                layer.Pivot = pivot; layer.Angle = angle; Render(); return true;
            }
            return false;
        }

        public Image FindLayer(string id)
        { foreach (var layer in layers) if (layer.Id == id) return layer.Image; return null; }

        private void Add(string id, string asset, Vector2 position, float extent, Vector2 pivot, Motion motion, float phase = 0)
        {
            var image = UITheme.Surface("Paper cutout " + id, transform, Color.white);
            image.preserveAspect = true; image.raycastTarget = false; image.enabled = false;
            layers.Add(new Layer { Id = id, Asset = asset, Image = image, Position = position, Extent = extent,
                Pivot = pivot, Motion = motion, Phase = phase });
        }

        private void TryLoadAssets()
        {
            foreach (var layer in layers)
            {
                if (layer.Image == null || layer.Image.sprite != null) continue;
                var path = "Hub/Layers/" + layer.Asset;
                var sprite = Resources.Load<Sprite>(path);
                if (sprite == null)
                {
                    var texture = Resources.Load<Texture2D>(path);
                    if (texture == null) continue;
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
                    sprite.name = "Independent paper cutout " + layer.Id;
                    layer.OwnedSprite = sprite;
                }
                layer.Image.sprite = sprite; layer.Image.enabled = true;
            }
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            if (!ReducedMotion) clock += Time.unscaledDeltaTime;
            if (Time.unscaledTime >= nextLoadAttempt)
            { nextLoadAttempt = Time.unscaledTime + 1; TryLoadAssets(); }
            Render();
        }

        private void Render()
        {
            if (frame == null) return;
            var dimensions = frame.rect.size;
            var unit = Mathf.Min(dimensions.x, dimensions.y);
            foreach (var layer in layers)
            {
                var rect = layer.Image.rectTransform;
                rect.anchorMin = rect.anchorMax = layer.Position;
                rect.pivot = layer.Pivot;
                // Uniform square boxes plus preserveAspect retain the actual cutout proportions.
                rect.sizeDelta = Vector2.one * (unit * layer.Extent);
                var offset = Vector2.zero; var angle = layer.Angle;
                if (!ReducedMotion)
                {
                    switch (layer.Motion)
                    {
                        case Motion.Cloud:
                            offset.x = Mathf.Sin(clock * .065f + layer.Phase) * unit * .065f;
                            break;
                        case Motion.Head:
                            angle += Mathf.Sin(clock * .30f + layer.Phase) * .9f;
                            break;
                        case Motion.Foliage:
                            angle += Mathf.Sin(clock * .42f + layer.Phase) * .8f;
                            break;
                    }
                }
                rect.anchoredPosition = offset;
                rect.localRotation = Quaternion.Euler(0, 0, angle);
                rect.localScale = Vector3.one;
            }
        }

        private void OnDestroy()
        { foreach (var layer in layers) if (layer.OwnedSprite != null) Destroy(layer.OwnedSprite); }

    }
}
