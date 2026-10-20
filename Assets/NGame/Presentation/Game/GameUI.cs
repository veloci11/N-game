using TMPro;
using UnityEngine;

namespace NGame.Presentation.Game
{
    internal sealed class ReadableTextLayout : MonoBehaviour
    {
        public float MinimumHeight;
        private float lastWidth = -1;
        private string lastText;
        private void LateUpdate()
        {
            var label = GetComponent<TMP_Text>();
            var width = ((RectTransform)transform).rect.width;
            if (label == null || width < 20 || lastWidth == width && lastText == label.text) return;
            lastWidth = width; lastText = label.text;
            var element = GetComponent<UnityEngine.UI.LayoutElement>();
            element.preferredHeight = Mathf.Max(MinimumHeight, label.GetPreferredValues(label.text, width, Mathf.Infinity).y + 6);
        }
    }

    internal static class GameUI
    {
        public static TextMeshProUGUI Text(Transform parent, string name, string value, float size = 18,
            float height = 36, bool display = true, Color? color = null)
        {
            var label = UITheme.Copy(name, parent, value, size, color ?? UITheme.Parchment, display);
            label.richText = false;
            label.overflowMode = TextOverflowModes.Overflow;
            var layout = label.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredHeight = Mathf.Max(height, label.GetPreferredValues(value, 340, Mathf.Infinity).y + 6);
            layout.minHeight = height;
            if (parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() == null)
                label.gameObject.AddComponent<ReadableTextLayout>().MinimumHeight = height;
            return label;
        }

        public static UnityEngine.UI.Button Button(Transform parent, string name, string label,
            UnityEngine.Events.UnityAction callback, bool primary = true, float height = 48)
        {
            var button = UITheme.Action(name, parent, label, callback, primary);
            button.GetComponentInChildren<TMP_Text>().richText = false;
            var caption = button.GetComponentInChildren<TMP_Text>();
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 14;
            caption.fontSizeMax = caption.fontSize;
            caption.overflowMode = TextOverflowModes.Overflow;
            var layout = button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredHeight = layout.minHeight = height;
            return button;
        }

        public static RectTransform Column(Transform parent, string name, int padding = 0, float spacing = 9)
        {
            var column = UITheme.Box(name, parent);
            var layout = column.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = column.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            return column;
        }

        public static RectTransform Row(Transform parent, string name, float height, float spacing = 8)
        {
            var row = UITheme.Box(name, parent);
            var element = row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            element.preferredHeight = element.minHeight = height;
            var group = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childControlWidth = group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.childAlignment = TextAnchor.MiddleCenter;
            return row;
        }

        public static RectTransform Scroll(Transform parent, string name, float top = 206, float bottom = 190)
        {
            var shell = UITheme.Box(name, parent, true);
            shell.offsetMin = new Vector2(21, bottom);
            shell.offsetMax = new Vector2(-21, -top);
            var viewport = UITheme.Surface("Readable scroll paper", shell, UITheme.Opacity(UITheme.Midnight, .93f), UITheme.Panel, true);
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var scroll = shell.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 26;
            var content = Column(viewport.transform, name + " content", 17, 10);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, 0);
            scroll.content = content;
            return content;
        }

        public static TMP_InputField Input(Transform parent, string name, string placeholder, int limit = 24)
        {
            var surface = UITheme.Surface(name, parent, UITheme.Opacity(UITheme.Parchment, .08f), UITheme.Panel);
            surface.raycastTarget = true;
            var layout = surface.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredHeight = layout.minHeight = 50;
            var input = surface.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = surface;
            input.characterLimit = limit;
            var viewport = UITheme.Box("Input viewport", surface.transform, true);
            viewport.offsetMin = new Vector2(14, 4);
            viewport.offsetMax = new Vector2(-14, -4);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            input.textViewport = viewport;
            var text = UITheme.Copy("Typed value", viewport, "", 20, UITheme.Parchment, false, TextAlignmentOptions.MidlineLeft);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            input.textComponent = text;
            text.richText = false;
            var hint = UITheme.Copy("Placeholder", viewport, placeholder, 18, UITheme.Quiet, false, TextAlignmentOptions.MidlineLeft);
            hint.rectTransform.anchorMin = Vector2.zero;
            hint.rectTransform.anchorMax = Vector2.one;
            hint.rectTransform.offsetMin = hint.rectTransform.offsetMax = Vector2.zero;
            input.placeholder = hint;
            input.selectionColor = UITheme.Opacity(UITheme.Brass, .4f);
            input.customCaretColor = true;
            input.caretColor = UITheme.Parchment;
            return input;
        }

        public static void ClearSelection(Transform root)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null || root == null) return;
            var selected = eventSystem.currentSelectedGameObject;
            if (selected != null && (selected.transform == root || selected.transform.IsChildOf(root)))
                eventSystem.SetSelectedGameObject(null);
        }

        public static void Clear(Transform content)
        {
            ClearSelection(content);
            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }
        }
        public static void Fill(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
