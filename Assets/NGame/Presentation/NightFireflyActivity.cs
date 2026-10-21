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
        private Camera villageCamera;
        private Transform villageAnchor;
        private Canvas canvas;
        private Vector3 lanternLocalPoint = new Vector3(.90f, -1.65f, 0);
        private readonly Vector3[] villageFlyOrigins = { new Vector3(-1.48f,-.30f,0), new Vector3(-.58f,.08f,0), new Vector3(.51f,-.28f,0), new Vector3(1.36f,.06f,0) };
        private Image lampLight;
        private TMPro.TMP_Text instruction;
        private Sprite ownedLantern;
        private readonly Button[] buttons = new Button[FlyCount];
        private readonly Image[] flights = new Image[FlyCount];
        private readonly Image[] stationaryLights = new Image[FlyCount];
        private readonly RectTransform[,] waitingWings = new RectTransform[FlyCount, 2];
        private readonly RectTransform[,] flyingWings = new RectTransform[FlyCount, 2];
        private readonly Image[,] trails = new Image[FlyCount, 5];
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
        public bool IsSceneAnchored => frame != null && villageCamera != null && villageAnchor != null && canvas != null;
        public Vector2 LanternPosition => DestinationPosition();
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
            UITheme.Copy("Firefly activity instruction", content, "Touche une luciole pour guider son vol.", 13, UITheme.Parchment, false);
            var text = content.GetComponentInChildren<TMPro.TMP_Text>(); instruction = text;
            UITheme.Position(text.rectTransform, UITheme.Center, 0, -107, 342, 24);
            text.outlineColor = new Color(.015f,.025f,.035f,.95f); text.outlineWidth = .20f; text.extraPadding = true;
            lampLight = UITheme.Surface("Anchored firefly lantern glow", content, UITheme.Opacity(UITheme.Brass, .18f), UITheme.Disc);
            UITheme.Position(lampLight.rectTransform, UITheme.Center, 0, 0, 38, 38);
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
                UITheme.Position(dot.rectTransform, UITheme.Center, 0, 0, 5, 8); stationaryLights[i] = dot;
                MakeInsect(dot, waitingWings, i);
                flights[i] = UITheme.Surface("Locally guided firefly " + i, content, UITheme.Brass, UITheme.Disc);
                UITheme.Position(flights[i].rectTransform, UITheme.Center, 0, 0, 5, 8);
                MakeInsect(flights[i], flyingWings, i);
                flights[i].enabled = false; flights[i].gameObject.SetActive(false);
                for (var trail = 0; trail < 5; trail++)
                {
                    trails[i,trail] = UITheme.Surface("Local firefly flight trace " + i + " " + trail, content, Color.clear, UITheme.Disc);
                    UITheme.Position(trails[i,trail].rectTransform, UITheme.Center, 0, 0, 2.3f, 2.3f);
                    trails[i,trail].enabled = false;
                }
                flightAge[i] = -1; lastTap[i] = -100;
            }
            RefreshAvailability(); Render();
        }

        private static void MakeInsect(Image body, RectTransform[,] wings, int index)
        {
            var glow = UITheme.Surface("Soft firefly abdomen light", body.transform, UITheme.Opacity(UITheme.Brass,.10f), UITheme.Mist);
            UITheme.Position(glow.rectTransform, UITheme.Center, 0,-2,24,24); glow.transform.SetAsFirstSibling();
            var head = UITheme.Surface("Separate dark firefly head", body.transform, new Color(.10f,.15f,.15f,1), UITheme.Disc);
            UITheme.Position(head.rectTransform, UITheme.Center, 0,5,3.5f,3.5f);
            for (var side = 0; side < 2; side++)
            {
                var direction = side == 0 ? -1 : 1;
                var wing = UITheme.Surface("Separate paper firefly wing " + side, body.transform, UITheme.Opacity(UITheme.Parchment,.85f), UITheme.Disc);
                UITheme.Position(wing.rectTransform, UITheme.Center, direction * 5,2,8,3.5f);
                wings[index,side] = wing.rectTransform;
            }
        }

        /// <summary>Anchors the local activity to the independent village. No role, target or action is supplied.</summary>
        public void BindVillage(Camera camera, Transform village)
        {
            villageCamera = camera; villageAnchor = village; canvas = GetComponentInParent<Canvas>(); Render();
        }
        public void SetVillageLandmarks(Vector3 lanternGroundPoint, Vector3[] flyOrigins)
        {
            lanternLocalPoint = lanternGroundPoint;
            if (flyOrigins != null && flyOrigins.Length == FlyCount) Array.Copy(flyOrigins, villageFlyOrigins, FlyCount);
            Render();
        }
        private bool ProjectVillagePoint(Vector3 localPoint, out Vector2 point)
        {
            point = Vector2.zero;
            if (!IsSceneAnchored) return false;
            var screen = villageCamera.WorldToScreenPoint(villageAnchor.TransformPoint(localPoint));
            if (screen.z < 0) return false;
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(frame, screen, uiCamera, out point);
        }
        private Vector2 DestinationPosition()
        { return ProjectVillagePoint(lanternLocalPoint, out var point) ? point : Local(new Vector2(.86f,.25f)); }

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
        {
            index = Mathf.Clamp(index,0,FlyCount-1);
            var scheduled = ScheduledPosition(index, PublicSeconds(), Quiet);
            var drift = scheduled - Origins[index];
            var worldPoint = villageFlyOrigins[index] + new Vector3(drift.x * 4.3f, drift.y * 2.2f,0);
            return ProjectVillagePoint(worldPoint, out var point) ? point : Local(scheduled);
        }
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
            var destination = DestinationPosition();
            if (instruction != null)
            {
                var width = Mathf.Min(342, Mathf.Max(0, frame.rect.width - 24));
                var horizontalLimit = Mathf.Max(0, (frame.rect.width - width) * .5f - 6);
                instruction.rectTransform.sizeDelta = new Vector2(width, 24);
                instruction.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(destination.x, -horizontalLimit, horizontalLimit), destination.y - 52);
            }
            lantern.anchoredPosition = destination; lampLight.rectTransform.anchoredPosition = destination;
            if (footing != null) footing.anchoredPosition = destination + new Vector2(0,-32);
            lampLight.color = UITheme.Opacity(UITheme.Brass, .09f + catchLight * .14f);
            var seconds = PublicSeconds();
            for (var i = 0; i < FlyCount; i++)
            {
                buttons[i].GetComponent<RectTransform>().anchoredPosition = TargetPosition(i);
                stationaryLights[i].color = UITheme.Opacity(UITheme.Brass,
                    Quiet ? .9f : .75f + Mathf.Sin((float)seconds * .8f + i) * .2f);
                var flying = flightAge[i] >= 0 && !Quiet;
                stationaryLights[i].gameObject.SetActive(!flying);
                flights[i].enabled = flying; flights[i].gameObject.SetActive(flying);
                AnimateWings(waitingWings, i, (float)seconds);
                AnimateWings(flyingWings, i, Quiet ? 0 : flightAge[i] * 1.4f);
                var u = Mathf.Clamp01(flightAge[i] / .7f);
                for (var trail = 0; trail < 5; trail++)
                {
                    var trailing = trails[i,trail]; var age = u - (trail + 1) * .055f;
                    trailing.enabled = flying && age > 0;
                    if (!trailing.enabled) continue;
                    trailing.rectTransform.anchoredPosition = FlightPoint(departure[i], destination, age);
                    trailing.color = UITheme.Opacity(UITheme.Brass, (1 - u) * (1 - trail / 5f) * .34f);
                }
                if (!flying) continue;
                flights[i].rectTransform.anchoredPosition = FlightPoint(departure[i], destination, u);
                flights[i].color = UITheme.Opacity(UITheme.Brass, 1 - u * .3f);
            }
        }
        private static Vector2 FlightPoint(Vector2 origin, Vector2 destination, float progress)
        {
            var control = (origin + destination) * .5f + new Vector2(0,35);
            return (1-progress)*(1-progress)*origin + 2*(1-progress)*progress*control + progress*progress*destination;
        }
        private void AnimateWings(RectTransform[,] wings, int index, float seconds)
        {
            for (var side = 0; side < 2; side++)
            {
                var direction = side == 0 ? -1 : 1;
                var angle = Quiet ? 28 : 28 + Mathf.Sin(seconds * 12 + index * 1.7f) * 12;
                wings[index,side].localRotation = Quaternion.Euler(0,0,direction*angle);
            }
        }
        private void ResetActivity()
        {
            LocalCatchCount = 0; catchLight = 0;
            for (var i = 0; i < FlyCount; i++)
            {
                flightAge[i] = -1; lastTap[i] = -100;
                if (flights[i] != null) { flights[i].enabled = false; flights[i].gameObject.SetActive(false); }
                if (stationaryLights[i] != null) stationaryLights[i].gameObject.SetActive(true);
                for (var trail=0; trail<5; trail++) if (trails[i,trail] != null) trails[i,trail].enabled=false;
            }
        }
        private void Suspend()
        { ResetActivity(); IsAvailable = false; if (content != null) content.gameObject.SetActive(false); }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) Suspend(); }
        private void OnApplicationPause(bool value) { paused = value; if (value) Suspend(); }
        private void OnDisable() { Suspend(); }
        private void OnDestroy() { if (ownedLantern != null) Destroy(ownedLantern); }
    }
}
