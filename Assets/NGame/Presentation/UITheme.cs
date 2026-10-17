using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NGame.Presentation
{
    /// <summary>The shared paper, type and layout vocabulary. No game state lives in this class.</summary>
    internal static class UITheme
    {
        public static readonly Color Parchment = Parse("F5E9CF");
        public static readonly Color Brass = Parse("C9A875");
        public static readonly Color Midnight = Parse("101E2C");
        public static readonly Color Quiet = Parse("ABB5B8");
        public static readonly Color Terracotta = Parse("A45B3D");
        public static readonly Vector2 Center = new Vector2(.5f, .5f);
        public static readonly Vector2 Top = new Vector2(.5f, 1);
        public static readonly Vector2 Bottom = new Vector2(.5f, 0);
        private static readonly Dictionary<string, Sprite> Illustrations = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, TMP_FontAsset> Fonts = new Dictionary<string, TMP_FontAsset>();
        private static Sprite panelSprite;
        private static Sprite discSprite;
        private static Sprite mistSprite;
        private static Sprite[] portraits;
        private static readonly Sprite[] roleCards = new Sprite[16];

        private static Color Parse(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
        public static Color Opacity(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
        public static Sprite Panel => panelSprite != null ? panelSprite : panelSprite = Procedural("Paper corners", 48, 48,
            (x, y) => 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(10, 12,
                new Vector2(Mathf.Max(11 - x, 0, x - 36), Mathf.Max(11 - y, 0, y - 36)).magnitude)),
            new Vector4(13, 13, 13, 13));
        public static Sprite Disc => discSprite != null ? discSprite : discSprite = Procedural("Circular seal", 64, 64,
            (x, y) => 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(30, 32,
                Vector2.Distance(new Vector2(x, y), Vector2.one * 31.5f))));
        public static Sprite Mist => mistSprite != null ? mistSprite : mistSprite = Procedural("River breath", 256, 64,
            (x, y) => Mathf.Pow(Mathf.Clamp01(1 - new Vector2((x - 128) / 128, (y - 32) / 32).sqrMagnitude), 2));

        public static Sprite Art(string name)
        {
            if (Illustrations.TryGetValue(name, out var result)) return result;
            result = Resources.Load<Sprite>("Werewolf/" + name);
            if (result == null)
            {
                var texture = Resources.Load<Texture2D>("Werewolf/" + name);
                if (texture != null) result = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Center, 100);
            }
            if (result != null) Illustrations[name] = result;
            return result;
        }

        public static Sprite Portrait(int index)
        {
            if (index < 0 || index > 5) return null;
            if (portraits == null)
            {
                var image = Art("avatars");
                if (image == null) return null;
                var texture = image.texture;
                var w = texture.width / 3;
                var h = texture.height / 2;
                portraits = new Sprite[6];
                for (var i = 0; i < 6; i++) portraits[i] = Sprite.Create(texture,
                    new Rect(i % 3 * w, (1 - i / 3) * h, w, h), Center, 100);
            }
            return portraits[index];
        }

        public static Sprite RoleCard(int index)
        {
            if (index < 0 || index >= roleCards.Length) return null;
            if (roleCards[index] != null) return roleCards[index];
            var sheet = Art(index < 8 ? "roles-core" : "roles-extended");
            if (sheet == null) return null;
            var slot = index % 8;
            var column = slot % 4;
            Rect crop;
            if (index < 8)
            {
                var x = new[] { 56f, 433f, 778f, 1127f };
                var width = new[] { 354f, 326f, 327f, 353f };
                crop = new Rect(x[column], slot < 4 ? 526 : 31, width[column], slot < 4 ? 490 : 485);
            }
            else
            {
                var x = new[] { 52f, 413f, 776f, 1138f };
                crop = new Rect(x[column], slot < 4 ? 522 : 34, 348, 484);
            }
            var texture = sheet.texture;
            crop = new Rect(crop.x * texture.width / 1536f, crop.y * texture.height / 1024f,
                crop.width * texture.width / 1536f, crop.height * texture.height / 1024f);
            roleCards[index] = Sprite.Create(texture, crop, Center, 100, 0, SpriteMeshType.FullRect);
            roleCards[index].name = "Approved role card " + index;
            return roleCards[index];
        }

        private static TMP_FontAsset Typeface(bool display)
        {
            var name = display ? "CormorantGaramond" : "Manrope";
            if (Fonts.TryGetValue(name, out var font) && font != null) return font;
            font = Resources.Load<TMP_FontAsset>("Werewolf/Fonts/" + name);
            if (font == null)
            {
                var ttf = Resources.Load<Font>("Werewolf/Fonts/" + name);
                if (ttf != null) font = TMP_FontAsset.CreateFontAsset(ttf, 64, 8,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            }
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (font != null) Fonts[name] = font;
            return font;
        }

        private static Sprite Procedural(string name, int width, int height, Func<float, float, float> alpha,
            Vector4 border = default)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var data = new Color[width * height];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++) data[y * width + x] = new Color(1, 1, 1, Mathf.Clamp01(alpha(x, y)));
            texture.SetPixels(data);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, width, height), Center, 100, 0, SpriteMeshType.FullRect, border);
        }

        public static RectTransform Box(string name, Transform parent, bool fill = false)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            if (fill)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            return rect;
        }

        public static void Position(RectTransform rect, Vector2 anchor, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = Center;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static UnityEngine.UI.Image Surface(string name, Transform parent, Color color,
            Sprite texture = null, bool fill = false)
        {
            var image = Box(name, parent, fill).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = texture;
            image.color = color;
            image.raycastTarget = false;
            if (texture == Panel) image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }

        public static TextMeshProUGUI Copy(string name, Transform parent, string value, float size,
            Color color, bool display = true, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var text = Box(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = Typeface(display);
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.richText = false;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        public static UnityEngine.UI.Button Action(string name, Transform parent, string label,
            UnityEngine.Events.UnityAction action, bool primary = true)
        {
            var background = Surface(name, parent, primary ? Terracotta : Opacity(Midnight, .82f), Panel);
            background.raycastTarget = true;
            var button = background.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = background;
            var states = button.colors;
            states.highlightedColor = new Color(1.08f, 1.08f, 1.04f, 1);
            states.pressedColor = new Color(.76f, .8f, .82f, 1);
            states.selectedColor = Color.white;
            states.disabledColor = new Color(1, 1, 1, .32f);
            states.fadeDuration = .1f;
            button.colors = states;
            if (action != null) button.onClick.AddListener(action);
            Edge(background, Opacity(Brass, primary ? .32f : .52f));
            var caption = Copy("Caption", background.transform, label, 21, Parchment);
            caption.rectTransform.anchorMin = Vector2.zero;
            caption.rectTransform.anchorMax = Vector2.one;
            caption.rectTransform.offsetMin = new Vector2(12, 3);
            caption.rectTransform.offsetMax = new Vector2(-12, -3);
            return button;
        }

        public static void Edge(UnityEngine.UI.Image image, Color color)
        {
            var edge = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
            edge.effectColor = color;
            edge.effectDistance = new Vector2(.7f, -.7f);
        }

        public static void Separator(Transform parent, Vector2 anchor, float y, float width = 110)
        {
            var line = Surface("Hairline brass", parent, Opacity(Brass, .6f));
            Position(line.rectTransform, anchor, 0, y, width, .6f);
            var dot = Surface("Letterpress diamond", parent, Brass);
            Position(dot.rectTransform, anchor, 0, y, 3, 3);
            dot.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
        }
    }
}
