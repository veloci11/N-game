using System;
using NGame.Core;
using NGame.Core.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NGame.Presentation.Game
{
    public sealed class RoomSecretCard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ICancelHandler
    {
        public const float HoldSeconds = .65f;
        public bool IsHolding { get; private set; }
        public bool IsRevealed { get; private set; }
        public bool IsFaceDisplayed => face != null && face.activeInHierarchy;
        public float HoldProgress { get; private set; }
        public event Action<bool> VisibilityChanged;
        private RoomView snapshot;
        private bool connected;
        private bool blocked;
        private int pointerId;
        private float elapsed;
        private float flip;
        private bool flipping;
        private GameObject back;
        private GameObject face;
        private UnityEngine.UI.Image frontArt;
        private UnityEngine.UI.Image progress;
        private TMPro.TextMeshProUGUI caption;
        private RectTransform card;
        private RoleCardMotion motion;
        private AppPreferences preferences;

        public void Initialize(Transform cardContainer, AppPreferences settings)
        {
            preferences = settings;
            var plate = UITheme.Surface("Personal sealed card", cardContainer, UITheme.Midnight, UITheme.Panel);
            card = plate.rectTransform;
            UITheme.Position(card, UITheme.Center, -103, 0, 124, 186);
            plate.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
            back = UITheme.Box("Personal card concealed", card, true).gameObject;
            var backArt = UITheme.Surface("Personal botanical card back", back.transform, Color.white, UITheme.Art("role-back"), true);
            var lockText = UITheme.Copy("Sealed role mark", back.transform, "?", 46, UITheme.Parchment);
            UITheme.Position(lockText.rectTransform, UITheme.Center, 0, -2, 80, 75);
            face = UITheme.Box("Personal card revealed", card, true).gameObject;
            frontArt = UITheme.Surface("Personal role illustration", face.transform, Color.white, null, true);
            frontArt.preserveAspect = true;
            face.SetActive(false);
            motion = card.gameObject.AddComponent<RoleCardMotion>();
            motion.Initialize(settings, 11, backArt, frontArt);
            progress = UITheme.Surface("Personal privacy hold progress", transform,
                UITheme.Opacity(UITheme.Brass, .2f), UITheme.Panel, true);
            progress.type = UnityEngine.UI.Image.Type.Filled;
            progress.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            progress.fillAmount = 0;
            caption = transform.Find("Caption")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (caption == null) caption = UITheme.Copy("Personal privacy hold caption", transform, "Maintenir · privé", 21, UITheme.Parchment);
            caption.transform.SetAsLastSibling();
            GameUI.Fill(caption.rectTransform);
        }

        public void SetImmersivePresentation(bool immersive)
        {
            if (card == null) return;
            var position = immersive ? Vector2.zero : new Vector2(-103, 0);
            UITheme.Position(card, UITheme.Center, position.x, position.y, immersive ? 220 : 124, immersive ? 330 : 186);
            motion.SetRestPose(position);
            motion.SetImmersive(immersive);
        }

        public void BindSnapshot(RoomView value, bool isConnected)
        {
            if (snapshot == null || value == null || snapshot.code != value.code || snapshot.turn != value.turn
                || snapshot.phase != value.phase || snapshot.selfRoleId != value.selfRoleId) MaskImmediately();
            snapshot = value;
            connected = isConnected;
            if (!connected || snapshot == null || string.IsNullOrEmpty(snapshot.selfRoleId)) MaskImmediately();
        }

        public void SetBlocked(bool value) { blocked = value; if (value) MaskImmediately(); }

        public void BeginHold()
        {
            if (!isActiveAndEnabled || blocked || !connected || snapshot == null || string.IsNullOrEmpty(snapshot.selfRoleId)) return;
            MaskImmediately();
            IsHolding = true;
        }

        public void MaskImmediately()
        {
            IsHolding = IsRevealed = flipping = false;
            elapsed = HoldProgress = flip = 0;
            if (back != null) back.SetActive(true);
            if (face != null) face.SetActive(false);
            if (frontArt != null) frontArt.sprite = null;
            if (progress != null) progress.fillAmount = 0;
            if (caption != null) caption.text = "Maintenir · privé";
            if (motion != null) { motion.SetRole(null); motion.SetFlipAngle(0); motion.SetRevealGlow(0); }
            if (card != null) card.localRotation = Quaternion.identity;
            VisibilityChanged?.Invoke(false);
        }

        private void Update()
        {
            if (!IsHolding) return;
            if (blocked || !connected || snapshot == null) { MaskImmediately(); return; }
            elapsed += Time.unscaledDeltaTime;
            HoldProgress = Mathf.Clamp01(elapsed / HoldSeconds);
            progress.fillAmount = HoldProgress;
            if (!IsRevealed && HoldProgress >= 1)
            {
                motion.SetRole(snapshot.selfRoleId);
                var definition = RoleCatalog.Find(snapshot.selfRoleId);
                frontArt.sprite = definition != null ? UITheme.RoleCard(definition.RoleArtIndex) : null;
                IsRevealed = true;
                flipping = !preferences.ReducedMotion;
                flip = 0;
                caption.text = "Relâcher pour masquer";
                if (!flipping) { back.SetActive(false); face.SetActive(true); }
                VisibilityChanged?.Invoke(true);
            }
            if (!flipping) return;
            flip += Time.unscaledDeltaTime / .36f;
            var front = flip >= .5f;
            back.SetActive(!front);
            face.SetActive(front);
            motion.SetFlipAngle(front ? Mathf.Lerp(-90, 0, Mathf.Clamp01((flip - .5f) * 2)) : Mathf.Lerp(0, 90, flip * 2));
            motion.SetRevealGlow(front ? Mathf.Clamp01(2 - flip) : 0);
            if (flip >= 1) { flipping = false; motion.SetFlipAngle(0); }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHolding || eventData.button != PointerEventData.InputButton.Left) return;
            pointerId = eventData.pointerId;
            BeginHold();
        }
        public void OnPointerUp(PointerEventData eventData) { if (eventData.pointerId == pointerId) MaskImmediately(); }
        public void OnPointerExit(PointerEventData eventData) { if (eventData.pointerId == pointerId) MaskImmediately(); }
        public void OnCancel(BaseEventData eventData) => MaskImmediately();
        private void OnApplicationFocus(bool focus) { if (!focus) MaskImmediately(); }
        private void OnApplicationPause(bool pause) { if (pause) MaskImmediately(); }
        private void OnDisable() => MaskImmediately();
    }
}
