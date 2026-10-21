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

            heading = Label("Hub heading", transform, "", 36, HubInk, 0, -133, 382, 55);
            instruction = Label("Hub instruction", transform, "", 13, HubMuted, 0, -180, 364, 36, false);
            catalogueCards = UITheme.Box("Hub responsive game cards", transform);
            UITheme.Position(catalogueCards, UITheme.Bottom, 0, 260, 382, 172);
            BuildWerewolfCard();
            BuildUpcomingCard("president", "Président", "Cartes", 0, true);
            BuildUpcomingCard("mimes", "Mimes", "Gestes", 128, false);

            ResumeAction = UITheme.Action("Hub resume saved party", transform, "Reprendre ma partie", () => resume?.Invoke(), false);
            UITheme.Position((RectTransform)ResumeAction.transform, UITheme.Bottom, 0, 184, 382, 32);
            ResumeAction.interactable = resume != null;
            PaperControl(ResumeAction);
            ResumeAction.GetComponentInChildren<TMP_Text>().fontSize = 17;
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
            UITheme.Position((RectTransform)werewolf.transform, UITheme.Center, -128, 0, 118, 172);
            werewolf.GetComponent<UnityEngine.UI.Image>().color = HubPaper;
            werewolf.interactable = openGame != null;
            var frame = UITheme.Box("Hub werewolf thumbnail frame", werewolf.transform);
            UITheme.Position(frame, UITheme.Center, 0, 39, 108, 84);
            frame.gameObject.AddComponent<HubCatalogueThumbnail>().Initialize(preferences, 0);
            Label("Hub werewolf name", werewolf.transform, "Loup-Garou", 21, HubInk, 0, -19, 112, 29, true, UITheme.Center);
            Label("Hub werewolf player count", werewolf.transform, "6 à 24 joueurs", 11, HubMuted, 0, -45, 110, 22, false, UITheme.Center);
            werewolfAction = Label("Hub werewolf action", werewolf.transform, "Ouvrir le jeu", 12, UITheme.Terracotta,
                0, -66, 110, 22, false, UITheme.Center);
        }

        private void BuildUpcomingCard(string id, string name, string category, float x, bool playingCards)
        {
            var button = UITheme.Action("Hub game " + id + " unavailable", catalogueCards, "", null, false);
            UITheme.Position((RectTransform)button.transform, UITheme.Center, x, 0, 118, 172);
            button.GetComponent<UnityEngine.UI.Image>().color = HubPaper;
            var availabilityColors = button.colors;
            availabilityColors.disabledColor = Color.white;
            button.colors = availabilityColors;
            button.interactable = false;
            var icon = UITheme.Box("Hub animated " + id + " icon", button.transform);
            UITheme.Position(icon, UITheme.Center, 0, 39, 108, 84);
            icon.gameObject.AddComponent<HubCatalogueThumbnail>().Initialize(preferences, playingCards ? 1 : 2);
            Label("Hub upcoming " + id + " name", button.transform, name, 23, HubInk, 0, -19, 112, 29, true, UITheme.Center);
            Label("Hub upcoming " + id + " category", button.transform, category, 11, HubMuted, 0, -45, 110, 22, false, UITheme.Center);
            Label("Hub upcoming " + id + " availability", button.transform, "En préparation", 11, UITheme.Terracotta,
                0, -66, 110, 22, false, UITheme.Center);
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
        }
        private void LateUpdate()
        {
            if (!initialized || visibility == null) return;
            if (catalogueCards != null)
            {
                // Only the row moves to make room for saved access; the action dock never moves.
                catalogueCards.anchoredPosition = new Vector2(0, hasSavedRoom && !IsChoosingForCreation ? 300 : 260);
                catalogueCards.localScale = Vector3.one;
            }
            entrance += Time.unscaledDeltaTime;
            visibility.alpha = preferences == null || preferences.ReducedMotion ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01(entrance / .3f));
        }
    }
}
