using System;
using NGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NGame.Presentation
{
    /// <summary>Local, role-independent feedback. Observes input without consuming UI actions.</summary>
    public sealed class NightCommonTapFeedback : MonoBehaviour
    {
        private const float Duration = .45f;
        private const int CounterLimit = 9999;
        private AppPreferences preferences;
        private Func<bool> nightEnabled, blocked;
        private Image glow;
        private float remaining;
        private bool focused = true, paused;
        public int FeedbackCount { get; private set; }
        public bool IsShowing => glow != null && glow.gameObject.activeSelf && glow.color.a > 0;

        // Attach beneath the existing UI Canvas; this component creates no camera or input module.
        public void Initialize(AppPreferences settings, Func<bool> isNightEnabled, Func<bool> isBlocked)
        {
            if (preferences != null) preferences.Changed -= PreferencesChanged;
            preferences = settings; nightEnabled = isNightEnabled; blocked = isBlocked;
            if (preferences != null) preferences.Changed += PreferencesChanged;
            if (glow == null)
            {
                glow = UITheme.Surface("Common fixed night touch light", transform, Color.clear, UITheme.Mist);
                UITheme.Position(glow.rectTransform, new Vector2(0, .5f), 8, 0, 16, 150);
                glow.raycastTarget = false;
                var group = glow.gameObject.AddComponent<CanvasGroup>();
                group.interactable = false; group.blocksRaycasts = false;
            }
            Hide();
        }

        private bool Allowed => isActiveAndEnabled && focused && !paused && preferences != null
            && !preferences.ReducedMotion && nightEnabled != null && nightEnabled()
            && blocked != null && !blocked();

        public void RegisterTap()
        {
            if (!Allowed || glow == null) { Hide(); return; }
            FeedbackCount = Mathf.Min(CounterLimit, FeedbackCount + 1);
            remaining = Duration;
            glow.gameObject.SetActive(true);
            glow.color = UITheme.Opacity(UITheme.Brass, .38f);
        }

        private void Update()
        {
            if (!Allowed) { Hide(); return; }
            // Reading the device edge does not send an event, select a target or swallow a pointer.
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) RegisterTap();
            if (remaining <= 0) return;
            remaining = Mathf.Max(0, remaining - Time.unscaledDeltaTime);
            if (remaining <= 0) { Hide(); return; }
            glow.color = UITheme.Opacity(UITheme.Brass, .38f * Mathf.SmoothStep(0, 1, remaining / Duration));
        }

        private void Hide()
        {
            remaining = 0;
            if (glow != null) { glow.color = Color.clear; glow.gameObject.SetActive(false); }
        }
        private void PreferencesChanged() { if (!Allowed) Hide(); }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) Hide(); }
        private void OnApplicationPause(bool value) { paused = value; if (value) Hide(); }
        private void OnDisable() => Hide();
        private void OnDestroy() { if (preferences != null) preferences.Changed -= PreferencesChanged; }
    }
}
