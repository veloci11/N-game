using System.Collections.Generic;
using NGame.Core;
using UnityEngine;

namespace NGame.Presentation
{
    /// <summary>Small, separately moving paper objects. Captions and catalogue hit areas stay stationary.</summary>
    public sealed class HubGameIcon : MonoBehaviour
    {
        private sealed class Part { public RectTransform Rect; public Vector2 Rest; public float Angle, Phase; }
        private readonly List<Part> parts = new List<Part>();
        private readonly List<Sprite> sprites = new List<Sprite>();
        private AppPreferences preferences;
        private bool cards, initialized;
        private float clock;
        public float AnimationClock => clock;
        public int MovingPartCount => parts.Count;
        private static readonly Color Ink = new Color(.2f, .19f, .16f);

        public void Initialize(AppPreferences settings, bool playingCards)
        {
            if (initialized) return;
            initialized = true; preferences = settings; cards = playingCards;
            if (cards) BuildCards(); else BuildMasks();
        }

        private RectTransform Moving(string name, float x, float y, float angle, int index)
        {
            var rect = UITheme.Box(name, transform);
            UITheme.Position(rect, UITheme.Center, x, y, 48, 56);
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            parts.Add(new Part { Rect = rect, Rest = rect.anchoredPosition, Angle = angle, Phase = index * 1.8f });
            return rect;
        }

        private UnityEngine.UI.Image Shape(string name, Transform parent, Sprite sprite, Color color,
            float x, float y, float width, float height)
        {
            var image = UITheme.Surface(name, parent, color, sprite);
            UITheme.Position(image.rectTransform, UITheme.Center, x, y, width, height);
            return image;
        }

        private void BuildCards()
        {
            var heart = Raster("Paper heart pip", (x, y) =>
            {
                x *= 1.1f; y = y * 1.15f + .12f;
                var a = x * x + y * y - .72f;
                return a * a * a - x * x * y * y * y <= 0;
            });
            var diamond = Polygon("Paper diamond pip", new[] { new Vector2(0, 1), Vector2.right,
                new Vector2(0, -1), Vector2.left });
            for (var i = 0; i < 3; i++)
            {
                var part = Moving("Animated president card " + i, (i - 1) * 19, 49 + (i == 1 ? 4 : 0), (1 - i) * 18, i);
                Shape("Ink paper card edge", part, UITheme.Panel, UITheme.Brass, 0, 0, 33, 46);
                Shape("Ivory card face", part, UITheme.Panel, UITheme.Parchment, 0, 0, 30, 43);
                if (i == 0)
                {
                    Shape("Terracotta patterned card back", part, UITheme.Panel, UITheme.Terracotta, 0, 0, 25, 37);
                    for (var n = 0; n < 3; n++) Shape("Gold back ornament " + n, part, diamond,
                        UITheme.Brass, 0, (n - 1) * 10, 8, 8);
                }
                else
                {
                    var pip = i == 1 ? diamond : heart;
                    var color = i == 1 ? Ink : UITheme.Terracotta;
                    Shape("Large playing card pip", part, pip, color, 0, 0, 15, 17);
                    Shape("Top corner card pip", part, pip, color, -9, 14, 5, 6);
                    Shape("Bottom corner card pip", part, pip, color, 9, -14, 5, 6);
                }
            }
            var crown = Polygon("Folded paper crown", new[] { new Vector2(-1, .8f), new Vector2(-.52f, -.1f),
                new Vector2(0, 1), new Vector2(.52f, -.1f), new Vector2(1, .8f), new Vector2(.74f, -1), new Vector2(-.74f, -1) });
            var crownRoot = Moving("Animated president paper crown", 0, 77, -3, 3);
            Shape("Gold folded crown", crownRoot, crown, UITheme.Brass, 0, 0, 26, 15);
            Shape("Crown paper fold", crownRoot, diamond, UITheme.Opacity(UITheme.Parchment, .65f), -3, -2, 6, 9);
        }

        private void BuildMasks()
        {
            var outline = Polygon("Theatre paper mask silhouette", new[] {
                new Vector2(-1, .76f), new Vector2(-.65f, 1), new Vector2(.65f, 1), new Vector2(1, .76f),
                new Vector2(.88f, -.23f), new Vector2(.53f, -.77f), new Vector2(0, -1),
                new Vector2(-.53f, -.77f), new Vector2(-.88f, -.23f) });
            for (var i = 0; i < 2; i++)
            {
                var part = Moving("Animated theatre mask " + i, i == 0 ? -16 : 17, i == 0 ? 55 : 49, i == 0 ? 14 : -13, i);
                var happy = i == 0;
                Shape("Mask paper edge", part, outline, Ink, 0, 0, 35, 44);
                Shape("Mask folded face", part, outline, happy ? UITheme.Parchment : UITheme.Terracotta, 0, 0, 32, 41);
                var facialInk = happy ? Ink : UITheme.Parchment;
                var leftEye = Shape("Mask left eye", part, UITheme.Disc, facialInk, -7, 6, 8, 4);
                var rightEye = Shape("Mask right eye", part, UITheme.Disc, facialInk, 7, 6, 8, 4);
                leftEye.rectTransform.localRotation = Quaternion.Euler(0, 0, happy ? -12 : 18);
                rightEye.rectTransform.localRotation = Quaternion.Euler(0, 0, happy ? 12 : -18);
                var mouth = Raster(happy ? "Comedy smiling mouth" : "Tragedy sad mouth", (x, y) =>
                {
                    var curve = (happy ? 1 : -1) * (.95f * x * x - .4f);
                    return Mathf.Abs(x) < .9f && Mathf.Abs(y - curve) < .16f;
                });
                Shape("Expressive theatre mouth", part, mouth, facialInk, 0, -8, 19, 11);
                Shape("Folded mask nose", part, UITheme.Panel, UITheme.Opacity(UITheme.Brass, .65f), 0, 1, 2, 8);
                if (!happy) Shape("Tragedy paper tear", part, UITheme.Disc, UITheme.Brass, 9, 0, 2, 4);
            }
        }

        private Sprite Polygon(string name, Vector2[] vertices) => Raster(name, (x, y) =>
        {
            var inside = false;
            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
            {
                var a = vertices[i]; var b = vertices[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        });

        private Sprite Raster(string name, System.Func<float, float, bool> contains)
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var colors = new Color[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var coverage = 0f;
                    for (var sy = 0; sy < 2; sy++) for (var sx = 0; sx < 2; sx++)
                        if (contains((x + .25f + sx * .5f) / size * 2 - 1,
                            (y + .25f + sy * .5f) / size * 2 - 1)) coverage += .25f;
                    colors[y * size + x] = new Color(1, 1, 1, coverage);
                }
            texture.SetPixels(colors); texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            sprite.name = name; sprites.Add(sprite); return sprite;
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            var reduced = preferences != null && preferences.ReducedMotion;
            if (!reduced) clock += Time.unscaledDeltaTime;
            foreach (var part in parts)
            {
                if (part.Rect == null) continue;
                var wave = reduced ? 0 : Mathf.Sin(clock * (cards ? .95f : 1.4f) + part.Phase);
                var tilt = reduced ? 0 : Mathf.Sin(clock * (cards ? .8f : 1.4f) + part.Phase) * (cards ? 5 : 9);
                part.Rect.anchoredPosition = part.Rest + new Vector2(cards ? wave * 1.5f : 0, wave * (cards ? 1.6f : 2.2f));
                part.Rect.localRotation = Quaternion.Euler(0, 0, part.Angle + tilt);
            }
        }
        private void OnDestroy()
        {
            foreach (var sprite in sprites)
                if (sprite != null) { var texture = sprite.texture; Destroy(sprite); if (texture != null) Destroy(texture); }
        }
    }
}
