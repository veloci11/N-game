using System;
using NGame.Core;
using TMPro;
using UnityEngine;

namespace NGame.Presentation
{
    /// <summary>The application catalogue. Opening a game never creates a network room.</summary>
    public sealed class GameHubView : MonoBehaviour
    {
        public const string WerewolfGameId = "werewolf";
        public bool IsChoosingForCreation { get; private set; }
        public bool WantsToCreate => IsChoosingForCreation;
        public UnityEngine.UI.Button PrimaryAction { get; private set; }
        public UnityEngine.UI.Button SecondaryAction { get; private set; }
        public UnityEngine.UI.Button ResumeAction { get; private set; }
        public UnityEngine.UI.Button SettingsAction { get; private set; }

        private static readonly Color HubInk = new Color32(16, 30, 44, 255);
        private static readonly Color HubMuted = new Color32(82, 75, 65, 255);
        private static readonly Color HubPaper = new Color32(250, 241, 222, 255);
        private AppPreferences preferences;
        private Action<string> openGame;
        private Action join;
        private Action settings;
        private Action resume;
        private TextMeshProUGUI heading;
        private TextMeshProUGUI instruction;
        private TextMeshProUGUI werewolfAction;
        private UnityEngine.UI.Button back;
        private UnityEngine.UI.Button werewolf;
        private RoleCardMotion portraitMotion;
        private CanvasGroup visibility;
        private RectTransform catalogueCards;
        private float entrance;
        private bool initialized;
        private bool hasSavedRoom;

        public void Initialize(AppPreferences appPreferences, Action<string> onOpenGame,
            Action onJoin, Action onSettings, Action onResume)
        {
            if (initialized) return;
            preferences = appPreferences;
            openGame = onOpenGame;
            join = onJoin;
            settings = onSettings;
            resume = onResume;
            visibility = gameObject.AddComponent<CanvasGroup>();
            Build();
            initialized = true;
            ShowCatalogue();
        }

        public void SetHasSavedRoom(bool value)
        {
            hasSavedRoom = value;
            if (ResumeAction != null) ResumeAction.gameObject.SetActive(value && !IsChoosingForCreation);
        }

        public void ShowCatalogue()
        {
            IsChoosingForCreation = false;
            if (!initialized) return;
            heading.text = "Choisis un jeu";
            instruction.text = "Pour jouer ensemble, chacun prend son téléphone.";
            werewolfAction.text = "Ouvrir le jeu";
            SetCaption(PrimaryAction, "Créer une soirée");
            SetCaption(SecondaryAction, "Rejoindre une soirée");
            PrimaryAction.interactable = openGame != null;
            SecondaryAction.interactable = join != null;
            back.gameObject.SetActive(false);
            ResumeAction.gameObject.SetActive(hasSavedRoom);
        }

        public void ShowGameSelection()
        {
            if (!initialized) return;
            IsChoosingForCreation = true;
            heading.text = "Créer une soirée";
            instruction.text = "Choisis le jeu. Le salon sera créé à l’étape suivante.";
            werewolfAction.text = "Choisir";
            SetCaption(PrimaryAction, "Continuer avec Loup-Garou");
            SetCaption(SecondaryAction, "Annuler");
            PrimaryAction.interactable = openGame != null;
            SecondaryAction.interactable = true;
            back.gameObject.SetActive(true);
            ResumeAction.gameObject.SetActive(false);
        }

        private void Build()
        {
            // The parent owns the N’GAME brand at the top; this view shares the same header positions.
            back = UITheme.Action("Hub selection back", transform, "‹", ShowCatalogue, false);
            UITheme.Position((RectTransform)back.transform, new Vector2(0, 1), 40, -40, 42, 42);
            SettingsAction = UITheme.Action("Hub settings", transform, "…", () => settings?.Invoke(), false);
            UITheme.Position((RectTransform)SettingsAction.transform, new Vector2(1, 1), -40, -40, 42, 42);
            SettingsAction.interactable = settings != null;
            PaperControl(SettingsAction); PaperControl(back);

            heading = Label("Hub heading", transform, "", 39, HubInk, 0, -126, 382, 60);
            instruction = Label("Hub instruction", transform, "", 14, HubMuted, 0, -179, 364, 42, false);
            catalogueCards = UITheme.Box("Hub responsive game cards", transform);
            UITheme.Position(catalogueCards, UITheme.Top, 0, -225, 430, 432);
            catalogueCards.pivot = new Vector2(.5f, 1);
            BuildWerewolfCard();
            BuildUpcomingCard("president", "Président", "Cartes", -99, true);
            BuildUpcomingCard("mimes", "Mimes", "Gestes", 99, false);

            ResumeAction = UITheme.Action("Hub resume saved party", transform, "Reprendre ma partie", () => resume?.Invoke(), false);
            UITheme.Position((RectTransform)ResumeAction.transform, UITheme.Bottom, 0, 203, 382, 48);
            ResumeAction.interactable = resume != null;
            PaperControl(ResumeAction);
            var resumeCaption = ResumeAction.GetComponentInChildren<TMP_Text>();
            resumeCaption.fontSize = 19;
            resumeCaption.rectTransform.offsetMin = new Vector2(12, 18);
            resumeCaption.rectTransform.offsetMax = new Vector2(-12, 0);
            Label("Hub saved access notice", ResumeAction.transform, "Accès enregistré sur cet appareil", 10, HubMuted,
                0, -13, 350, 20, false, UITheme.Center);
            PrimaryAction = UITheme.Action("Hub create party", transform, "Créer une soirée", () =>
            {
                if (IsChoosingForCreation) OpenWerewolf(); else ShowGameSelection();
            });
            UITheme.Position((RectTransform)PrimaryAction.transform, UITheme.Bottom, 0, 124, 382, 56);
            SecondaryAction = UITheme.Action("Hub join party", transform, "Rejoindre une soirée", () =>
            {
                if (IsChoosingForCreation) ShowCatalogue(); else join?.Invoke();
            }, false);
            UITheme.Position((RectTransform)SecondaryAction.transform, UITheme.Bottom, 0, 62, 382, 48);
            PaperControl(SecondaryAction);
            Label("Hub availability", transform, "1 jeu disponible · 2 en préparation", 11, HubMuted,
                0, 21, 382, 23, false, UITheme.Bottom);
        }

        private void BuildWerewolfCard()
        {
            werewolf = UITheme.Action("Hub game werewolf", catalogueCards, "", OpenWerewolf, false);
            UITheme.Position((RectTransform)werewolf.transform, UITheme.Top, 0, -119, 382, 238);
            werewolf.GetComponent<UnityEngine.UI.Image>().color = HubPaper;
            werewolf.interactable = openGame != null;
            var art = UITheme.Surface("Hub living werewolf illustration", werewolf.transform,
                Color.white, UITheme.RoleCard(0));
            UITheme.Position(art.rectTransform, UITheme.Center, -109, 0, 144, 216);
            art.preserveAspect = true;
            portraitMotion = art.gameObject.AddComponent<RoleCardMotion>();
            portraitMotion.Initialize(preferences, 0, art);
            portraitMotion.SetRole("werewolf");
            Label("Hub werewolf name", werewolf.transform, "Loup-Garou", 31, HubInk, 71, 61, 211, 52, true, UITheme.Center);
            Label("Hub werewolf player count", werewolf.transform, "6 à 24 joueurs", 14, UITheme.Terracotta, 71, 13, 211, 27, false, UITheme.Center);
            Label("Hub werewolf supported modes", werewolf.transform, "En présence\nou à distance", 14, HubMuted, 71, -24, 211, 49, false, UITheme.Center);
            var seal = UITheme.Surface("Hub werewolf action seal", werewolf.transform, UITheme.Terracotta, UITheme.Panel);
            UITheme.Position(seal.rectTransform, UITheme.Center, 71, -84, 180, 38);
            werewolfAction = Label("Hub werewolf action", seal.transform, "Ouvrir le jeu", 18, UITheme.Parchment, 0, 0, 174, 36, true, UITheme.Center);
        }

        private void BuildUpcomingCard(string id, string name, string category, float x, bool playingCards)
        {
            var button = UITheme.Action("Hub game " + id + " unavailable", catalogueCards, "", null, false);
            UITheme.Position((RectTransform)button.transform, UITheme.Top, x, -345, 184, 174);
            button.GetComponent<UnityEngine.UI.Image>().color = HubPaper;
            var availabilityColors = button.colors;
            availabilityColors.disabledColor = Color.white;
            button.colors = availabilityColors;
            button.interactable = false;
            var icon = UITheme.Box("Hub animated " + id + " icon", button.transform, true);
            icon.gameObject.AddComponent<HubGameIcon>().Initialize(preferences, playingCards);
            Label("Hub upcoming " + id + " name", button.transform, name, 29, HubInk, 0, 0, 172, 44, true, UITheme.Center);
            Label("Hub upcoming " + id + " category", button.transform, category, 12, HubMuted, 0, -31, 172, 24, false, UITheme.Center);
            Label("Hub upcoming " + id + " availability", button.transform, "En préparation", 13, UITheme.Terracotta, 0, -61, 172, 26, false, UITheme.Center);
        }

        private static TextMeshProUGUI Label(string name, Transform parent, string value, float size,
            Color color, float x, float y, float width, float height, bool display = true, Vector2? anchor = null)
        {
            var text = UITheme.Copy(name, parent, value, size, color, display);
            UITheme.Position(text.rectTransform, anchor ?? UITheme.Top, x, y, width, height);
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(10, size - 3);
            text.fontSizeMax = size;
            return text;
        }

        private static void PaperControl(UnityEngine.UI.Button button)
        {
            button.GetComponent<UnityEngine.UI.Image>().color = HubPaper;
            button.GetComponentInChildren<TMP_Text>().color = HubInk;
        }
        private void OpenWerewolf() => openGame?.Invoke(WerewolfGameId);
        private static void SetCaption(UnityEngine.UI.Button button, string value)
        { if (button != null) button.GetComponentInChildren<TMP_Text>().text = value; }
        private void OnEnable()
        {
            entrance = 0;
            if (portraitMotion != null) portraitMotion.RestartEntrance(0);
        }
        private void LateUpdate()
        {
            if (!initialized || visibility == null) return;
            if (catalogueCards != null)
            {
                var height = ((RectTransform)transform).rect.height;
                var footerTop = hasSavedRoom && !IsChoosingForCreation ? 243f : 168f;
                var scale = Mathf.Clamp((height - footerTop - 225f) / 432f, .3f, 1f);
                catalogueCards.localScale = Vector3.one * scale;
            }
            entrance += Time.unscaledDeltaTime;
            visibility.alpha = preferences == null || preferences.ReducedMotion ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01(entrance / .3f));
        }
    }
}
