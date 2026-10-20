using System.Collections.Generic;
using NGame.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NGame.Presentation
{
    /// <summary>Card-only movement and masked foil/portrait effects. Labels and actions remain stationary.</summary>
    public sealed class RoleCardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private sealed class Surface
        {
            public UnityEngine.UI.Image Image;
            public Material Material;
        }
        private readonly List<Surface> surfaces = new List<Surface>();
        private RectTransform rect;
        private CanvasGroup opacity;
        private AppPreferences preferences;
        private Vector2 restPosition;
        private Vector3 restEuler;
        private float time;
        private float entrance;
        private float delay;
        private float phase;
        private float hovered;
        private bool pointerInside;
        private Vector2 pointer;
        private float flip;
        private float revealGlow;
        private bool initialized;
        private bool forcedReducedMotion;
        private bool wasReducedMotion;
        private int roleKind;
        private bool immersive;
        public float PortraitClock => time;
        public float FlipAngle => flip;

        public void Initialize(AppPreferences settings, int seed, params UnityEngine.UI.Image[] artwork)
        {
            if (initialized) return;
            initialized = true;
            preferences = settings;
            wasReducedMotion = ReducedMotion;
            phase = seed * 1.731f;
            rect = (RectTransform)transform;
            restPosition = rect.anchoredPosition;
            restEuler = rect.localEulerAngles;
            opacity = GetComponent<CanvasGroup>();
            if (opacity == null) opacity = gameObject.AddComponent<CanvasGroup>();
            var shader = Resources.Load<Shader>("Werewolf/Shader/UICard");
            if (shader != null)
                foreach (var image in artwork)
                {
                    if (image == null) continue;
                    var material = new Material(shader) { name = "Living paper card " + seed };
                    image.material = material;
                    surfaces.Add(new Surface { Image = image, Material = material });
                }
            if (preferences != null) preferences.Changed += SettingsChanged;
            RestartEntrance(seed % 4);
        }

        public void SetRestPose(Vector2 position)
        {
            restPosition = position;
            if (ReducedMotion && rect != null) rect.anchoredPosition = position;
        }
        public void RestartEntrance(int order) { entrance = 0; delay = Mathf.Max(0, order) * .08f; }
        public void SetImmersive(bool value)
        {
            if (immersive == value) return;
            immersive = value;
            RestartEntrance(0);
        }
        public void SetFlipAngle(float degrees) => flip = degrees;
        public void SetRevealGlow(float amount) => revealGlow = Mathf.Clamp01(amount);
        public void SetMotionReduced(bool value) { forcedReducedMotion = value; SettingsChanged(); }
        /// <summary>Only illustrated fronts receive role effects. The common back never hints at its secret.</summary>
        public void SetRole(string roleId)
        {
            var role = RoleCatalog.Find(roleId);
            roleKind = role == null ? 0 : role.RoleArtIndex + 1;
        }
        private bool ReducedMotion => forcedReducedMotion || (preferences != null && preferences.ReducedMotion);

        public void OnPointerEnter(PointerEventData eventData) { pointerInside = true; OnPointerMove(eventData); }
        public void OnPointerExit(PointerEventData eventData) { pointerInside = false; pointer = Vector2.zero; }
        public void OnPointerMove(PointerEventData eventData)
        {
            if (rect == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.enterEventCamera, out var local);
            pointer = new Vector2(Mathf.Clamp(local.x / Mathf.Max(1, rect.rect.width), -.5f, .5f),
                Mathf.Clamp(local.y / Mathf.Max(1, rect.rect.height), -.5f, .5f));
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            var reduced = ReducedMotion;
            if (!reduced) { time += Time.unscaledDeltaTime; entrance += Time.unscaledDeltaTime; }
            var appear = reduced ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01((entrance - delay) / (immersive ? .65f : .42f)));
            hovered = reduced ? 0 : Mathf.MoveTowards(hovered, pointerInside ? 1 : 0, Time.unscaledDeltaTime * 6);
            var floatY = reduced ? 0 : Mathf.Sin(time * 1.05f + phase) * 3;
            rect.anchoredPosition = restPosition + new Vector2(0, floatY + hovered * 8 - (1 - appear) * (immersive ? 64 : 13));
            var tilt = reduced ? Vector2.zero : pointer * hovered;
            rect.localRotation = Quaternion.Euler(restEuler.x - tilt.y * 12, restEuler.y + flip + tilt.x * 15,
                restEuler.z + (reduced ? 0 : Mathf.Sin(time * .69f + phase) * .65f));
            rect.localScale = Vector3.one * (reduced ? 1 : Mathf.Lerp(immersive ? .82f : .94f, 1, appear) + hovered * .018f);
            opacity.alpha = appear;
            foreach (var surface in surfaces) UpdateSurface(surface, reduced);
        }

        private void UpdateSurface(Surface surface, bool reduced)
        {
            if (surface.Image == null || surface.Material == null) return;
            var sprite = surface.Image.sprite;
            if (sprite == null) return;
            var source = sprite.rect;
            var uv = new Vector4(source.x / sprite.texture.width, source.y / sprite.texture.height,
                source.width / sprite.texture.width, source.height / sprite.texture.height);
            var front = sprite.name.StartsWith("Approved role card ", System.StringComparison.Ordinal);
            Apply(surface.Material, uv, reduced, front ? roleKind : 0);
            var rendered = surface.Image.materialForRendering;
            if (rendered != surface.Material) Apply(rendered, uv, reduced, front ? roleKind : 0);
        }

        private void Apply(Material material, Vector4 uv, bool reduced, int illustrationKind)
        {
            if (material == null) return;
            material.SetVector("_ArtRect", uv);
            material.SetFloat("_Clock", time);
            material.SetFloat("_Motion", reduced ? 0 : 1);
            material.SetFloat("_Seed", phase);
            material.SetFloat("_RevealGlow", reduced ? 0 : revealGlow);
            material.SetFloat("_RoleKind", illustrationKind);
        }

        private void SettingsChanged()
        {
            if (!ReducedMotion)
            {
                if (wasReducedMotion) entrance = delay + .42f;
                wasReducedMotion = false;
                return;
            }
            wasReducedMotion = true;
            pointerInside = false;
            pointer = Vector2.zero;
            hovered = 0;
            flip = 0;
            revealGlow = 0;
        }
        private void OnDisable() { pointerInside = false; pointer = Vector2.zero; flip = 0; revealGlow = 0; }
        private void OnDestroy()
        {
            if (preferences != null) preferences.Changed -= SettingsChanged;
            foreach (var surface in surfaces) if (surface.Material != null) Destroy(surface.Material);
        }
    }
}
