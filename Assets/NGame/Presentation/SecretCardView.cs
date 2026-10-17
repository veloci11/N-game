using NGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NGame.Presentation
{
    /// <summary>Input privacy boundary: the model can reveal only during a deliberate, uninterrupted hold.</summary>
    public sealed class SecretCardView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ICancelHandler
    {
        public const float HoldDuration = .65f;
        public bool IsHolding { get; private set; }
        public float HoldFraction { get; private set; }
        private VillageSession session;
        private GameObject back;
        private GameObject face;
        private TextMeshProUGUI instruction;
        private UnityEngine.UI.Image fill;
        private int activePointer;
        private float heldSeconds;
        private RectTransform card;
        private bool reducedMotion;
        private RoleCardMotion cardMotion;
        private bool lastVisible;
        private bool flipping;
        private float flipElapsed;
        private float revealLight;
        public bool IsFlipping => flipping;
        public bool IsFaceDisplayed => face != null && face.activeInHierarchy;

        public void Initialize(VillageSession model, Transform viewParent, AppPreferences preferences = null)
        {
            session = model;
            var cardImage = UITheme.Surface("Secret paper card", viewParent, UITheme.Midnight, UITheme.Panel);
            card = cardImage.rectTransform;
            UITheme.Position(card, UITheme.Bottom, 77, 314, 184, 276);
            card.localEulerAngles = new Vector3(0, 0, -2.8f);
            UITheme.Edge(cardImage, UITheme.Opacity(UITheme.Brass, .85f));
            cardImage.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
            back = UITheme.Box("Role concealed", card, true).gameObject;
            var backArt = UITheme.Surface("Botanical back illustration", back.transform, Color.white, UITheme.Art("role-back"), true);
            var heading = UITheme.Copy("Secret label", back.transform, "Ton rôle", 23, UITheme.Parchment);
            UITheme.Position(heading.rectTransform, UITheme.Top, 0, -37, 145, 36);
            DrawSeal(back.transform);
            var whisper = UITheme.Copy("Secret whisper", back.transform, "Un secret\nà garder.", 19, UITheme.Parchment);
            UITheme.Position(whisper.rectTransform, UITheme.Bottom, 0, 62, 145, 57);
            face = UITheme.Box("Role revealed", card, true).gameObject;
            var frontArt = UITheme.Surface("Revealed wolf illustration", face.transform, Color.white, UITheme.Art("role-front"), true);
            var name = UITheme.Copy("Local preview role", face.transform, "Loup-Garou", 24, UITheme.Parchment);
            UITheme.Position(name.rectTransform, UITheme.Bottom, 0, 34, 166, 39);
            var camp = UITheme.Copy("Local preview faction", face.transform, "LE CAMP DES LOUPS", 7, UITheme.Brass, false);
            camp.characterSpacing = 1;
            UITheme.Position(camp.rectTransform, UITheme.Bottom, 0, 15, 163, 17);
            face.SetActive(false);
            cardMotion = card.gameObject.AddComponent<RoleCardMotion>();
            cardMotion.Initialize(preferences, 3, backArt, frontArt);
            fill = UITheme.Surface("Hold progress", transform, UITheme.Opacity(UITheme.Brass, .2f), UITheme.Panel, true);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillAmount = 0;
            instruction = UITheme.Copy("Hold instruction", transform, "Maintenir pour révéler", 21, UITheme.Parchment);
            UITheme.Position(instruction.rectTransform, UITheme.Center, 0, 0, 350, 45);
            session.Changed += Render;
            Render();
        }

        private static void DrawSeal(Transform parent)
        {
            var loop = UITheme.Surface("Lock shackle", parent, UITheme.Parchment, UITheme.Disc);
            UITheme.Position(loop.rectTransform, UITheme.Center, 0, 12, 32, 37);
            var hollow = UITheme.Surface("Lock opening", parent, UITheme.Midnight, UITheme.Disc);
            UITheme.Position(hollow.rectTransform, UITheme.Center, 0, 12, 21, 25);
            var body = UITheme.Surface("Lock body", parent, UITheme.Parchment, UITheme.Panel);
            UITheme.Position(body.rectTransform, UITheme.Center, 0, -7, 43, 34);
            var keyhole = UITheme.Surface("Lock keyhole", parent, UITheme.Midnight, UITheme.Disc);
            UITheme.Position(keyhole.rectTransform, UITheme.Center, 0, -6, 7, 9);
        }

        public void SetMotionReduced(bool value)
        {
            reducedMotion = value;
            if (cardMotion != null) cardMotion.SetMotionReduced(value);
            if (!value || card == null) return;
            flipping = false;
            flipElapsed = revealLight = 0;
            card.localEulerAngles = new Vector3(0, 0, -2.8f);
            card.localScale = Vector3.one;
            if (session != null && session.RoleVisible)
            {
                back.SetActive(false);
                face.SetActive(true);
            }
        }

        public void BeginHold()
        {
            if (!isActiveAndEnabled || session == null || session.Phase != VillagePhase.Night) return;
            MaskImmediately();
            IsHolding = true;
        }

        public void MaskImmediately()
        {
            IsHolding = false;
            heldSeconds = HoldFraction = 0;
            CancelFlip();
            if (fill != null) fill.fillAmount = 0;
            session?.ConcealSecret();
            if (back != null) back.SetActive(true);
            if (face != null) face.SetActive(false);
            if (instruction != null) instruction.text = "Maintenir pour révéler";
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHolding || eventData.button != PointerEventData.InputButton.Left) return;
            activePointer = eventData.pointerId;
            BeginHold();
        }
        public void OnPointerUp(PointerEventData eventData) { if (eventData.pointerId == activePointer) MaskImmediately(); }
        public void OnPointerExit(PointerEventData eventData) { if (eventData.pointerId == activePointer) MaskImmediately(); }
        public void OnCancel(BaseEventData eventData) => MaskImmediately();

        private void Update()
        {
            if (IsHolding)
            {
                if (session.Phase != VillagePhase.Night) { MaskImmediately(); return; }
                heldSeconds += Time.unscaledDeltaTime;
                HoldFraction = Mathf.Clamp01(heldSeconds / HoldDuration);
                fill.fillAmount = HoldFraction;
                if (HoldFraction >= 1 && !session.RoleVisible) session.RevealSecret();
            }
            if (cardMotion == null || reducedMotion) return;
            if (flipping)
            {
                if (!IsHolding || !session.RoleVisible) { MaskImmediately(); return; }
                flipElapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(flipElapsed / .18f);
                var showFront = progress >= .5f;
                back.SetActive(!showFront);
                face.SetActive(showFront);
                cardMotion.SetFlipAngle(showFront ? Mathf.Lerp(-90, 0, (progress - .5f) * 2)
                    : Mathf.Lerp(0, 90, progress * 2));
                revealLight = showFront ? 1 : 0;
                if (progress >= 1) { flipping = false; cardMotion.SetFlipAngle(0); }
            }
            else revealLight = Mathf.MoveTowards(revealLight, 0, Time.unscaledDeltaTime * 2);
            cardMotion.SetRevealGlow(revealLight);
        }

        private void Render()
        {
            if (session.Phase != VillagePhase.Night) IsHolding = false;
            if (!session.RoleVisible)
            {
                if (lastVisible)
                {
                    IsHolding = false;
                    heldSeconds = HoldFraction = 0;
                    if (fill != null) fill.fillAmount = 0;
                }
                CancelFlip();
                if (back != null) back.SetActive(true);
                if (face != null) face.SetActive(false);
            }
            else if (!lastVisible)
            {
                flipping = !reducedMotion;
                flipElapsed = 0;
                if (back != null) back.SetActive(flipping);
                if (face != null) face.SetActive(!flipping);
            }
            lastVisible = session.RoleVisible;
            if (instruction != null) instruction.text = session.RoleVisible ? "Relâcher pour cacher" : "Maintenir pour révéler";
        }

        private void CancelFlip()
        {
            flipping = false;
            flipElapsed = revealLight = 0;
            lastVisible = false;
            if (cardMotion != null) { cardMotion.SetFlipAngle(0); cardMotion.SetRevealGlow(0); }
            if (card != null) card.localRotation = Quaternion.Euler(0, 0, -2.8f);
        }
        private void OnDisable() => MaskImmediately();
        private void OnApplicationPause(bool pause) { if (pause) MaskImmediately(); }
        private void OnApplicationFocus(bool focus) { if (!focus) MaskImmediately(); }
        private void OnDestroy() { if (session != null) session.Changed -= Render; }
    }
}
