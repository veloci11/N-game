using UnityEngine;

namespace NGame.Presentation
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeViewport : MonoBehaviour
    {
        private Rect previousArea;
        private Vector2Int previousSize;
        private RectTransform target;
        private void Awake() { target = GetComponent<RectTransform>(); Refresh(); }
        private void LateUpdate()
        {
            if (previousArea != Screen.safeArea || previousSize != new Vector2Int(Screen.width, Screen.height)) Refresh();
        }
        private void Refresh()
        {
            if (Screen.width == 0 || Screen.height == 0) return;
            var safe = Screen.safeArea;
            if (safe.width == 0 || safe.height == 0) safe = new Rect(0, 0, Screen.width, Screen.height);
            target.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            target.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            target.offsetMin = target.offsetMax = Vector2.zero;
            previousArea = Screen.safeArea;
            previousSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
