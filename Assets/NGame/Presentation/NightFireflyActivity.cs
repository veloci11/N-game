using System;
using NGame.Core;
using NGame.Core.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace NGame.Presentation
{
    /// <summary>Optional local night activity. Its public-time schedule never depends on a role or pending action.</summary>
    public sealed class NightFireflyActivity : MonoBehaviour
    {
        private const int FlyCount = 4;
        private static readonly Vector2[] Origins = { new Vector2(.14f, .61f), new Vector2(.36f, .81f), new Vector2(.65f, .62f), new Vector2(.85f, .81f) };
        private AppPreferences preferences;
        private Func<RoomView> readView;
        private Func<bool> blocked;
        private RectTransform frame, content, lantern, footing;
        private Image lampLight;
        private Sprite ownedLantern;
        private readonly Button[] buttons = new Button[FlyCount];
        private readonly Image[] flights = new Image[FlyCount];
        private readonly Image[] stationaryLights = new Image[FlyCount];
        private readonly Vector2[] departure = new Vector2[FlyCount];
        private readonly float[] flightAge = new float[FlyCount];
        private readonly float[] lastTap = new float[FlyCount];
        private bool initialized, focused = true, paused;
        private long observedServerTime, phaseStarted;
        private float serverObservedAt, catchLight;
        private string lastPhase;
        public bool IsAvailable { get; private set; }
        public int LocalCatchCount { get; private set; }
        public int TargetCount => FlyCount;
        private bool Quiet => preferences != null && preferences.ReducedMotion;
        public bool HasFlyingFireflies
        {
            get { for (var i = 0; i < FlyCount; i++) if (flightAge[i] >= 0 && !Quiet && IsAvailable) return true; return false; }
        }

        public void Initialize(AppPreferences settings, Func<RoomView> view, Func<bool> isBlocked)
        {
            preferences = settings; readView = view; blocked = isBlocked;
            if (initialized) return;
            initialized = true; frame = (RectTransform)transform;
            content = UITheme.Box("Optional common firefly activity", transform, true);
            // Only the four explicit 44-point targets accept input. The scene itself cannot cover another control.
            UITheme.Copy("Firefly activity instruction", content, "Touche une luciole pour l’attirer vers la lanterne.", 12, UITheme.Quiet, false);
            var text = content.GetComponentInChildren<TMPro.TMP_Text>();
            UITheme.Position(text.rectTransform, UITheme.Bottom, 0, 5, 342, 22);
            lampLight = UITheme.Surface("Anchored firefly lantern glow", content, UITheme.Opacity(UITheme.Brass, .18f), UITheme.Disc);
            UITheme.Position(lampLight.rectTransform, UITheme.Center, 0, 0, 56, 56);
            var stoneFooting = UITheme.Surface("Lantern fixed stone footing", content, Color.white, UITheme.Art("Layered/rock-bank"));
            UITheme.Position(stoneFooting.rectTransform, UITheme.Center, 0, -35, 92, 30); stoneFooting.preserveAspect = true; footing = stoneFooting.rectTransform;
            var image = UITheme.Surface("Anchored firefly lantern", content, Color.white, LanternSprite());
            image.preserveAspect = true; image.enabled = image.sprite != null; lantern = image.rectTransform;
            UITheme.Position(lantern, UITheme.Center, 0, 0, 42, 60);
            for (var i = 0; i < FlyCount; i++)
            {
                var index = i;
                var hit = UITheme.Surface("Firefly touch target " + i, content, Color.clear, UITheme.Disc);
                hit.raycastTarget = true;
                UITheme.Position(hit.rectTransform, UITheme.Center, 0, 0, 44, 44);
                var button = hit.gameObject.AddComponent<Button>(); button.targetGraphic = hit;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => RegisterTap(index)); buttons[i] = button;
                var dot = UITheme.Surface("Waiting firefly light " + i, hit.transform, UITheme.Brass, UITheme.Disc);
                UITheme.Position(dot.rectTransform, UITheme.Center, 0, 0, 7, 7); stationaryLights[i] = dot;
                foreach (var side in new[] { -1, 1 })
                {
                    var wing = UITheme.Surface("Paper firefly wing " + side, hit.transform, UITheme.Opacity(UITheme.Parchment, .68f), UITheme.Disc);
                    UITheme.Position(wing.rectTransform, UITheme.Center, side * 5, 1, 6, 3);
                    wing.rectTransform.localRotation = Quaternion.Euler(0, 0, side * 28);
                }
                flights[i] = UITheme.Surface("Locally guided firefly " + i, content, UITheme.Brass, UITheme.Disc);
                UITheme.Position(flights[i].rectTransform, UITheme.Center, 0, 0, 5, 5);
                flightAge[i] = -1; lastTap[i] = -100;
            }
            RefreshAvailability(); Render();
        }

        private Sprite LanternSprite()
        {
            var sprite = Resources.Load<Sprite>("Werewolf/lantern"); if (sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>("Werewolf/lantern"); if (texture == null) return null;
            ownedLantern = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            return ownedLantern;
        }

        private void RefreshAvailability()
        {
            var view = readView?.Invoke();
            // This is intentionally the full list of room fields read by this component.
            var phase = view?.phase;
            var start = view?.phaseStartedUnixMs ?? 0;
            var server = view?.serverTimeUnixMs ?? 0;
            var screenProtocol = view?.settings?.nightProtocol == "screen";
            if (phase != lastPhase || start != phaseStarted)
            {
                ResetActivity(); lastPhase = phase; phaseStarted = start;
                observedServerTime = 0;
            }
            if (server != observedServerTime)
            { observedServerTime = server; serverObservedAt = Time.unscaledTime; }
            var allowed = phase == "night" && screenProtocol && focused && !paused && !(blocked?.Invoke() ?? false);
            if (!allowed && IsAvailable) ResetActivity();
            IsAvailable = allowed;
            if (content != null) content.gameObject.SetActive(allowed);
        }

        private double PublicSeconds()
        {
            // A missing server clock gives a shared static schedule instead of inventing a per-device phase.
            if (observedServerTime <= 0 || phaseStarted <= 0) return 0;
            return Math.Max(0, (observedServerTime - phaseStarted) / 1000d + Time.unscaledTime - serverObservedAt);
        }
        public static Vector2 ScheduledPosition(int index, double publicSeconds, bool reducedMotion)
        {
            index = Mathf.Clamp(index, 0, FlyCount - 1); var position = Origins[index];
            if (!reducedMotion)
            {
                var phase = index * 1.7;
                position.x += (float)Math.Sin(publicSeconds * .37 + phase) * .035f;
                position.y += (float)Math.Cos(publicSeconds * .29 + phase) * .035f;
            }
            return position;
        }
        public Vector2 TargetPosition(int index)
        { return Local(ScheduledPosition(index, PublicSeconds(), Quiet)); }
        private Vector2 Local(Vector2 normalized)
        { return new Vector2((normalized.x - .5f) * frame.rect.width, (normalized.y - .5f) * frame.rect.height); }

        public bool RegisterTap(int index)
        {
            if (!initialized) return false;
            RefreshAvailability();
            if (!IsAvailable || index < 0 || index >= FlyCount || Time.unscaledTime - lastTap[index] < .8f) return false;
            lastTap[index] = Time.unscaledTime; departure[index] = TargetPosition(index);
            flightAge[index] = Quiet ? -1 : 0; catchLight = 1;
            LocalCatchCount = Math.Min(9999, LocalCatchCount + 1);
            Render(); return true;
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            RefreshAvailability(); if (!IsAvailable) return;
            catchLight = Mathf.Max(0, catchLight - Time.unscaledDeltaTime * 1.8f);
            for (var i = 0; i < FlyCount; i++)
            {
                if (Quiet) flightAge[i] = -1;
                else if (flightAge[i] >= 0)
                { flightAge[i] += Time.unscaledDeltaTime; if (flightAge[i] >= .7f) flightAge[i] = -1; }
            }
            Render();
        }
        private void Render()
        {
            if (content == null || !IsAvailable) return;
            var destination = Local(new Vector2(.86f, .25f));
            lantern.anchoredPosition = destination; lampLight.rectTransform.anchoredPosition = destination;
            if (footing != null) footing.anchoredPosition = destination + new Vector2(0,-32);
            lampLight.color = UITheme.Opacity(UITheme.Brass, .18f + catchLight * .33f);
            var seconds = PublicSeconds();
            for (var i = 0; i < FlyCount; i++)
            {
                buttons[i].GetComponent<RectTransform>().anchoredPosition = Local(ScheduledPosition(i, seconds, Quiet));
                stationaryLights[i].color = UITheme.Opacity(UITheme.Brass,
                    Quiet ? .9f : .75f + Mathf.Sin((float)seconds * .8f + i) * .2f);
                var flying = flightAge[i] >= 0 && !Quiet; flights[i].enabled = flying;
                if (!flying) continue;
                var u = Mathf.Clamp01(flightAge[i] / .7f);
                var control = (departure[i] + destination) * .5f + new Vector2(0, 35);
                flights[i].rectTransform.anchoredPosition = (1 - u) * (1 - u) * departure[i] + 2 * (1 - u) * u * control + u * u * destination;
                flights[i].color = UITheme.Opacity(UITheme.Brass, 1 - u * .55f);
            }
        }
        private void ResetActivity()
        {
            LocalCatchCount = 0; catchLight = 0;
            for (var i = 0; i < FlyCount; i++)
            { flightAge[i] = -1; lastTap[i] = -100; if (flights[i] != null) flights[i].enabled = false; }
        }
        private void Suspend()
        { ResetActivity(); IsAvailable = false; if (content != null) content.gameObject.SetActive(false); }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) Suspend(); }
        private void OnApplicationPause(bool value) { paused = value; if (value) Suspend(); }
        private void OnDisable() { Suspend(); }
        private void OnDestroy() { if (ownedLantern != null) Destroy(ownedLantern); }
    }
}
