using NGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace NGame.Presentation
{
    public enum AppRoute { Home, Preparation, Village, RoleLibrary }

    /// <summary>Application navigation and presentation only. All local preview state belongs to VillageSession.</summary>
    public sealed class StorybookApp : MonoBehaviour
    {
        public AppRoute Route { get; private set; } = AppRoute.Home;
        public VillageSession Session { get; private set; }
        public AppPreferences Preferences { get; private set; }
        public SecretCardView SecretCard { get; private set; }
        public RoleLibraryView RoleLibrary { get; private set; }
        public int PreparationPlayerCount { get; private set; } = 8;
        public bool IsModalOpen => dialog != null;
        private RectTransform viewport;
        private GameObject homeView;
        private GameObject preparationView;
        private GameObject villageView;
        private GameObject nightView;
        private GameObject voteView;
        private GameObject confirmationView;
        private GameObject dialog;
        private TextMeshProUGUI header;
        private TextMeshProUGUI chapter;
        private TextMeshProUGUI villageTitle;
        private TextMeshProUGUI phaseCaption;
        private TextMeshProUGUI dawnCaption;
        private TextMeshProUGUI preparationCount;
        private TextMeshProUGUI composition;
        private TextMeshProUGUI compositionNote;
        private RectTransform preparationPaper;
        private TextMeshProUGUI chosenName;
        private TextMeshProUGUI audioSetting;
        private TextMeshProUGUI motionSetting;
        private UnityEngine.UI.Image chosenPortrait;
        private UnityEngine.UI.Button confirmationButton;
        private readonly UnityEngine.UI.Image[] playerTiles = new UnityEngine.UI.Image[6];
        private VillageStage stage;
        private VillagePhase previousPhase;
        private float dawnElapsed;
        private AudioSource sound;
        private AudioClip soundClip;
        private bool initialized;

        private void Awake() => Initialize();

        public void Initialize()
        {
            if (initialized) return;
            Session = new VillageSession();
            Preferences = new AppPreferences(new UnityPreferenceStore());
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            var scene = new GameObject("Paper diorama");
            scene.transform.SetParent(transform, false);
            stage = scene.AddComponent<VillageStage>();
            stage.Initialize();
            BuildCanvas();
            BuildHeader();
            BuildHome();
            BuildPreparation();
            BuildVillage();
            var libraryRoot = UITheme.Box("The public role catalogue", viewport, true);
            RoleLibrary = libraryRoot.gameObject.AddComponent<RoleLibraryView>();
            RoleLibrary.Initialize(Preferences);
            InitializeSound();
            Session.Changed += ModelChanged;
            Preferences.Changed += ApplyPreferences;
            initialized = true;
            ApplyPreferences();
            OpenHome();
        }

        private void BuildCanvas()
        {
            var root = UITheme.Box("Storybook interface", transform);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(430, 932);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var input = new GameObject("UI input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                input.transform.SetParent(transform, false);
                input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            AddReadingVeil(root);
            viewport = UITheme.Box("Safe portrait content", root, true);
            viewport.gameObject.AddComponent<SafeViewport>();
        }

        private static void AddReadingVeil(Transform parent)
        {
            var texture = new Texture2D(1, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[128];
            for (var i = 0; i < pixels.Length; i++)
            {
                var y = i / 127f;
                var bottom = .72f * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.03f, .38f, y)));
                var top = .45f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.64f, 1, y));
                pixels[i] = new Color(.02f, .035f, .055f, bottom + top);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 128), UITheme.Center);
            UITheme.Surface("Gentle ink wash", parent, Color.white, sprite, true);
        }

        private TextMeshProUGUI Label(string name, Transform parent, string value, float size, Color color,
            Vector2 anchor, float x, float y, float width, float height, bool display = true)
        {
            var text = UITheme.Copy(name, parent, value, size, color, display);
            UITheme.Position(text.rectTransform, anchor, x, y, width, height);
            return text;
        }

        private UnityEngine.UI.Button Button(string name, Transform parent, string value,
            UnityEngine.Events.UnityAction action, Vector2 anchor, float x, float y, float width, float height, bool primary = true)
        {
            var button = UITheme.Action(name, parent, value, action, primary);
            UITheme.Position((RectTransform)button.transform, anchor, x, y, width, height);
            return button;
        }

        private void BuildHeader()
        {
            Button("Return to storybook", viewport, "‹", OpenHome, new Vector2(0, 1), 40, -40, 42, 42, false);
            header = Label("Current chapter heading", viewport, "N’GAME", 27, UITheme.Parchment,
                new Vector2(0, 1), 162, -40, 183, 43);
            header.alignment = TextAlignmentOptions.MidlineLeft;
            Button("Open quiet settings", viewport, "···", ShowSettings, new Vector2(1, 1), -40, -40, 42, 42, false);
        }

        private void BuildHome()
        {
            homeView = UITheme.Box("Storybook home", viewport, true).gameObject;
            var eyebrow = Label("Home overline", homeView.transform, "LES CONTES DE N’GAME", 10, UITheme.Brass,
                UITheme.Top, 0, -136, 350, 27, false);
            eyebrow.characterSpacing = 3.4f;
            Label("Home title", homeView.transform, "Le village\nde papier.", 59, UITheme.Parchment,
                UITheme.Top, 0, -232, 382, 172);
            Label("Home invitation", homeView.transform, "Une nuit. Des secrets.\nEt toute une histoire à partager.", 22,
                UITheme.Parchment, UITheme.Top, 0, -349, 350, 74);
            UITheme.Separator(homeView.transform, UITheme.Bottom, 231);
            Label("Home preview notice", homeView.transform, "LE PREMIER CHAPITRE", 9, UITheme.Brass,
                UITheme.Bottom, 0, 205, 350, 24, false).characterSpacing = 2.5f;
            Button("Enter the village", homeView.transform, "Entrer dans le village", EnterVillage,
                UITheme.Bottom, 0, 155, 370, 58);
            Button("Prepare a gathering", homeView.transform, "Préparer une partie", OpenPreparation,
                UITheme.Bottom, 0, 87, 370, 54, false);
            var roles = Button("Discover the illustrated roles", homeView.transform, "Les rôles", OpenRoleLibrary,
                UITheme.Bottom, -94, 37, 174, 40, false);
            roles.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            var how = Button("Learn the game", homeView.transform, "Comment jouer", ShowRules,
                UITheme.Bottom, 94, 37, 174, 40, false);
            how.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
        }

        private void BuildPreparation()
        {
            preparationView = UITheme.Box("Gathering composition helper", viewport, true).gameObject;
            Label("Preparation title", preparationView.transform, "Préparer une partie", 38, UITheme.Parchment,
                UITheme.Top, 0, -138, 390, 65);
            Label("Preparation introduction", preparationView.transform, "Un petit guide pour réunir le village.", 19,
                UITheme.Parchment, UITheme.Top, 0, -190, 360, 40);
            var panel = UITheme.Surface("Composition paper", preparationView.transform,
                UITheme.Opacity(UITheme.Midnight, .94f), UITheme.Panel);
            UITheme.Position(panel.rectTransform, UITheme.Bottom, 0, 371, 370, 426);
            preparationPaper = panel.rectTransform;
            UITheme.Edge(panel, UITheme.Opacity(UITheme.Brass, .55f));
            Label("Player count caption", panel.transform, "COMBIEN ÊTES-VOUS ?", 10, UITheme.Brass,
                UITheme.Top, 0, -35, 330, 26, false).characterSpacing = 2;
            preparationCount = Label("Number of players", panel.transform, "8", 65, UITheme.Parchment,
                UITheme.Top, 0, -106, 130, 88);
            Button("Fewer players", panel.transform, "−", () => SetPreparationCount(PreparationPlayerCount - 1),
                UITheme.Top, -111, -106, 49, 49, false);
            Button("More players", panel.transform, "+", () => SetPreparationCount(PreparationPlayerCount + 1),
                UITheme.Top, 111, -106, 49, 49, false);
            UITheme.Separator(panel.transform, UITheme.Top, -172, 290);
            composition = Label("Suggested role composition", panel.transform, "", 25, UITheme.Parchment,
                UITheme.Top, 0, -252, 327, 139);
            composition.lineSpacing = 4;
            compositionNote = Label("Composition uncertainty", panel.transform, "", 11, UITheme.Quiet,
                UITheme.Bottom, 0, 55, 327, 71, false);
            Label("Preparation honesty", preparationView.transform,
                "Cette préparation ne crée pas de partie connectée.", 11, UITheme.Quiet,
                UITheme.Bottom, 0, 126, 370, 34, false);
            Button("Explore local illustration", preparationView.transform, "Explorer le village", EnterVillage,
                UITheme.Bottom, 0, 72, 370, 55);
            UpdateComposition();
        }

        private void BuildVillage()
        {
            villageView = UITheme.Box("Village chapter", viewport, true).gameObject;
            chapter = Label("Chapter overline", villageView.transform, "CHAPITRE I", 10, UITheme.Brass,
                UITheme.Top, 0, -125, 360, 25, false);
            chapter.characterSpacing = 3.5f;
            villageTitle = Label("Village narration", villageView.transform, "Le village s’endort.", 38, UITheme.Parchment,
                UITheme.Top, 0, -176, 400, 75);
            UITheme.Separator(villageView.transform, UITheme.Top, -222, 90);
            phaseCaption = Label("Village time", villageView.transform, "Nuit 1", 21, UITheme.Parchment,
                UITheme.Top, 0, -250, 320, 35);
            nightView = UITheme.Box("Secret discovery", villageView.transform, true).gameObject;
            Label("Privacy invitation", nightView.transform, "À l’abri des regards.", 17, UITheme.Parchment,
                UITheme.Bottom, 0, 156, 350, 29);
            var hold = Button("Hold role reveal", nightView.transform, "", null,
                UITheme.Bottom, 0, 109, 370, 58, false);
            SecretCard = hold.gameObject.AddComponent<SecretCardView>();
            SecretCard.Initialize(Session, nightView.transform, Preferences);
            var dawnButton = Button("Discover sunrise", nightView.transform, "Voir le lever du jour  →", AdvanceToDawn,
                UITheme.Bottom, 0, 55, 350, 42, false);
            dawnButton.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            dawnCaption = Label("Sunrise whisper", villageView.transform, "Les premières lueurs…", 25,
                UITheme.Parchment, UITheme.Bottom, 0, 124, 360, 70);
            BuildVoting();
            BuildConfirmation();
            Label("Local preview disclosure", villageView.transform, "APERÇU INTERACTIF · LOCAL", 8,
                UITheme.Opacity(UITheme.Parchment, .58f), UITheme.Bottom, 0, 18, 370, 18, false).characterSpacing = 1.7f;
        }

        private void BuildVoting()
        {
            voteView = UITheme.Box("Village vote", villageView.transform, true).gameObject;
            var panel = UITheme.Surface("Voting paper", voteView.transform,
                UITheme.Opacity(UITheme.Midnight, .94f), UITheme.Panel);
            UITheme.Position(panel.rectTransform, UITheme.Bottom, 0, 281, 376, 388);
            UITheme.Edge(panel, UITheme.Opacity(UITheme.Brass, .55f));
            Label("Vote question", panel.transform, "Qui vous semble suspect ?", 27, UITheme.Parchment,
                UITheme.Top, 0, -34, 348, 45);
            Label("Vote scope", panel.transform, "Six personnages pour découvrir le geste du vote.", 10,
                UITheme.Quiet, UITheme.Top, 0, -68, 348, 25, false);
            for (var i = 0; i < Session.Players.Count; i++)
            {
                var id = Session.Players[i].Id;
                var tile = Button("Choose " + Session.Players[i].Name, panel.transform, "", () => Session.SelectPlayer(id),
                    UITheme.Top, i % 2 == 0 ? -87 : 87, -119 - i / 2 * 68, 160, 57, false);
                playerTiles[i] = tile.GetComponent<UnityEngine.UI.Image>();
                playerTiles[i].color = UITheme.Opacity(UITheme.Parchment, .07f);
                Portrait(tile.transform, i, new Vector2(0, .5f), 29, 0, 43);
                var name = Label("Villager name", tile.transform, Session.Players[i].Name, 21,
                    UITheme.Parchment, new Vector2(0, .5f), 108, 0, 100, 38);
                name.alignment = TextAlignmentOptions.MidlineLeft;
            }
            confirmationButton = Button("Confirm local choice", panel.transform, "Confirmer mon choix",
                () => Session.ConfirmSelection(), UITheme.Bottom, 0, 44, 336, 53);
            var night = Button("Return to midnight", voteView.transform, "←  Retrouver la nuit",
                () => Session.MoveTo(VillagePhase.Night), UITheme.Bottom, 0, 55, 340, 42, false);
            night.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
        }

        private UnityEngine.UI.Image Portrait(Transform parent, int index, Vector2 anchor, float x, float y, float size)
        {
            var frame = UITheme.Surface("Portrait seal", parent, UITheme.Parchment, UITheme.Disc);
            UITheme.Position(frame.rectTransform, anchor, x, y, size, size);
            frame.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
            return UITheme.Surface("Origami villager", frame.transform, Color.white, UITheme.Portrait(index), true);
        }

        private void BuildConfirmation()
        {
            confirmationView = UITheme.Box("Local choice confirmation", villageView.transform, true).gameObject;
            var panel = UITheme.Surface("Confirmation paper", confirmationView.transform,
                UITheme.Opacity(UITheme.Midnight, .94f), UITheme.Panel);
            UITheme.Position(panel.rectTransform, UITheme.Bottom, 0, 301, 366, 410);
            UITheme.Edge(panel, UITheme.Opacity(UITheme.Brass, .55f));
            chosenPortrait = Portrait(panel.transform, 0, UITheme.Top, 0, -71, 88);
            chosenName = Label("Chosen villager", panel.transform, "", 43, UITheme.Parchment,
                UITheme.Top, 0, -150, 330, 68);
            Label("Confirmation scope", panel.transform, "a reçu ton choix.\n\nCe vote est une démonstration.\nAucun joueur n’est éliminé.", 19,
                UITheme.Quiet, UITheme.Top, 0, -245, 330, 125);
            Button("Replay local preview", panel.transform, "Rejouer l’aperçu", EnterVillage,
                UITheme.Bottom, 0, 48, 325, 54);
        }

        public void OpenHome()
        {
            if (!initialized) return;
            CloseDialog();
            SecretCard.MaskImmediately();
            Route = AppRoute.Home;
            RenderRoute();
        }

        public void OpenPreparation()
        {
            if (!initialized) return;
            CloseDialog();
            SecretCard.MaskImmediately();
            Route = AppRoute.Preparation;
            UpdateComposition();
            RenderRoute();
        }

        public void OpenRoleLibrary()
        {
            if (!initialized) return;
            CloseDialog();
            SecretCard.MaskImmediately();
            Route = AppRoute.RoleLibrary;
            RoleLibrary.OpenPage(0);
            RenderRoute();
        }

        public void EnterVillage()
        {
            if (!initialized) return;
            CloseDialog();
            Session.Reset();
            SecretCard.MaskImmediately();
            Route = AppRoute.Village;
            RenderRoute();
            ModelChanged();
        }

        public void SetPreparationCount(int number)
        {
            PreparationPlayerCount = Mathf.Clamp(number, 6, 12);
            UpdateComposition();
        }

        private void UpdateComposition()
        {
            if (preparationCount == null) return;
            var suggestion = PartyComposer.Recommend(PreparationPlayerCount);
            preparationCount.text = suggestion.PlayerCount.ToString();
            composition.text = suggestion.Wolves + (suggestion.Wolves == 1 ? " Loup-Garou\n" : " Loups-Garous\n")
                + suggestion.Seers + " Voyante\n"
                + suggestion.Villagers + " Villageois";
            compositionNote.text = suggestion.Note;
        }

        public void AdvanceToDawn()
        {
            if (Route != AppRoute.Village || IsModalOpen || Session.Phase != VillagePhase.Night) return;
            SecretCard.MaskImmediately();
            Session.MoveTo(VillagePhase.Dawn);
        }

        private void RenderRoute()
        {
            homeView.SetActive(Route == AppRoute.Home);
            preparationView.SetActive(Route == AppRoute.Preparation);
            villageView.SetActive(Route == AppRoute.Village);
            RoleLibrary.gameObject.SetActive(Route == AppRoute.RoleLibrary);
            header.text = Route == AppRoute.Home ? "N’GAME" : Route == AppRoute.Preparation ? "La veillée"
                : Route == AppRoute.RoleLibrary ? "Les rôles" : "Loup-Garou";
            stage.SetHomeComposition(Route != AppRoute.Village);
            if (Route != AppRoute.Village) stage.SetDaylight(0);
        }

        private void ModelChanged()
        {
            if (Route != AppRoute.Village) return;
            var phase = Session.Phase;
            if (phase != previousPhase && phase == VillagePhase.Dawn) dawnElapsed = 0;
            previousPhase = phase;
            nightView.SetActive(phase == VillagePhase.Night);
            voteView.SetActive(phase == VillagePhase.Vote);
            confirmationView.SetActive(phase == VillagePhase.VoteConfirmed);
            dawnCaption.gameObject.SetActive(phase == VillagePhase.Dawn);
            chapter.text = phase == VillagePhase.Night ? "CHAPITRE I" : phase == VillagePhase.Dawn ? "ENTRE DEUX MONDES"
                : phase == VillagePhase.Vote ? "LES PREMIERS SOUPÇONS" : "UN MURMURE AU VILLAGE";
            villageTitle.text = phase == VillagePhase.Night ? "Le village s’endort." : phase == VillagePhase.Dawn ? "La nuit s’efface."
                : phase == VillagePhase.Vote ? "Le village se réveille." : "Le choix est fait.";
            villageTitle.fontSize = phase == VillagePhase.Vote ? 36 : 38;
            phaseCaption.text = phase == VillagePhase.Night ? "Nuit 1" : phase == VillagePhase.Dawn ? "L’aube" : "Jour 1";
            if (phase != VillagePhase.Dawn) SetDaylight(phase == VillagePhase.Night ? 0 : 1);
            for (var i = 0; i < Session.Players.Count; i++)
            {
                playerTiles[i].color = Session.Players[i].Id == Session.SelectedPlayerId
                    ? UITheme.Opacity(UITheme.Brass, .34f) : UITheme.Opacity(UITheme.Parchment, .07f);
                if (Session.Players[i].Id != Session.ConfirmedPlayerId) continue;
                chosenName.text = Session.Players[i].Name;
                chosenPortrait.sprite = UITheme.Portrait(i);
            }
            confirmationButton.interactable = Session.SelectedPlayerId != null && Session.ConfirmedPlayerId == null;
        }

        private void Update()
        {
            if (!initialized || Route != AppRoute.Village || Session.Phase != VillagePhase.Dawn) return;
            dawnElapsed += Time.unscaledDeltaTime;
            var amount = Mathf.Clamp01(dawnElapsed / (Preferences.ReducedMotion ? .4f : 3.4f));
            SetDaylight(Mathf.SmoothStep(0, 1, amount));
            if (amount >= 1) Session.MoveTo(VillagePhase.Vote);
        }

        private void SetDaylight(float amount)
        {
            stage.SetDaylight(amount);
            // Ink follows the changing sky so the narration stays legible at sunrise.
            villageTitle.color = Color.Lerp(UITheme.Parchment, UITheme.Midnight, amount);
            phaseCaption.color = Color.Lerp(UITheme.Parchment, UITheme.Midnight, amount);
            chapter.color = Color.Lerp(UITheme.Brass, UITheme.Midnight, amount);
        }

        private void LateUpdate()
        {
            if (!initialized || preparationPaper == null || Route != AppRoute.Preparation) return;
            var scale = Mathf.Clamp((viewport.rect.height - 400) / 426, .73f, 1);
            preparationPaper.localScale = Vector3.one * scale;
            preparationPaper.anchoredPosition = new Vector2(0, 158 + 213 * scale);
        }

        private RectTransform Dialog(string overline, string title, float height)
        {
            CloseDialog();
            SecretCard.MaskImmediately();
            SecretCard.enabled = false;
            dialog = UITheme.Box("Storybook dialog", viewport, true).gameObject;
            var shade = UITheme.Surface("Quiet backdrop", dialog.transform, new Color(.01f, .025f, .045f, .86f), null, true);
            shade.raycastTarget = true;
            shade.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseDialog);
            var panel = UITheme.Surface("Dialog paper", dialog.transform, UITheme.Midnight, UITheme.Panel);
            panel.raycastTarget = true;
            UITheme.Position(panel.rectTransform, UITheme.Center, 0, 0, 366, height);
            UITheme.Edge(panel, UITheme.Opacity(UITheme.Brass, .65f));
            Label("Dialog overline", panel.transform, overline, 9, UITheme.Brass,
                UITheme.Top, 0, -35, 325, 25, false).characterSpacing = 2.5f;
            Label("Dialog title", panel.transform, title, 33, UITheme.Parchment, UITheme.Top, 0, -83, 330, 63);
            return panel.rectTransform;
        }

        public void ShowSettings()
        {
            if (!initialized) return;
            var panel = Dialog("LE CALME AVANT LA NUIT", "Les petits réglages", 402);
            var audio = Button("Ambient sound setting", panel, "", () => Preferences.SetAmbientSound(!Preferences.AmbientSound),
                UITheme.Top, 0, -159, 322, 55, false);
            audioSetting = audio.GetComponentInChildren<TextMeshProUGUI>();
            audioSetting.fontSize = 19;
            var motion = Button("Motion comfort setting", panel, "", () => Preferences.SetReducedMotion(!Preferences.ReducedMotion),
                UITheme.Top, 0, -226, 322, 55, false);
            motionSetting = motion.GetComponentInChildren<TextMeshProUGUI>();
            motionSetting.fontSize = 19;
            Label("Reduced motion explanation", panel, "Une scène plus calme,\navec des transitions plus courtes.", 16,
                UITheme.Quiet, UITheme.Top, 0, -288, 322, 57);
            Button("Close preferences", panel, "Retrouver le conte", CloseDialog,
                UITheme.Bottom, 0, 45, 322, 53);
            ApplyPreferences();
        }

        public void ShowRules()
        {
            if (!initialized) return;
            var panel = Dialog("LE JEU DES SOUPÇONS", "Quand vient la nuit…", 487);
            Label("Game concept", panel,
                "Les Loups-Garous se cachent\nparmi les habitants du village.\n\nLa nuit, les rôles secrets agissent.\nLe jour, on discute, on doute, on vote.\n\nL’aperçu te fait découvrir l’ambiance,\nla carte secrète et le geste du vote.", 21,
                UITheme.Parchment, UITheme.Top, 0, -241, 323, 250);
            Label("Rules preview disclosure", panel, "Une présentation locale.\nLa partie complète reste à construire.", 11,
                UITheme.Quiet, UITheme.Bottom, 0, 113, 322, 49, false);
            Button("Return from rules", panel, "J’ai compris", CloseDialog, UITheme.Bottom, 0, 48, 322, 54);
        }

        public void CloseDialog()
        {
            if (dialog != null) { dialog.SetActive(false); Destroy(dialog); dialog = null; }
            audioSetting = motionSetting = null;
            if (SecretCard == null) return;
            SecretCard.MaskImmediately();
            SecretCard.enabled = true;
        }

        private void ApplyPreferences()
        {
            stage.SetMotionReduced(Preferences.ReducedMotion);
            SecretCard.SetMotionReduced(Preferences.ReducedMotion);
            if (sound != null)
            {
                if (Preferences.AmbientSound && !sound.isPlaying) sound.Play();
                if (!Preferences.AmbientSound) sound.Stop();
            }
            if (audioSetting != null) audioSetting.text = Preferences.AmbientSound ? "Ambiance sonore  ·  Oui" : "Ambiance sonore  ·  Non";
            if (motionSetting != null) motionSetting.text = Preferences.ReducedMotion ? "Animations réduites  ·  Oui" : "Animations réduites  ·  Non";
        }

        private void InitializeSound()
        {
            var source = new GameObject("Soft original night wind");
            source.transform.SetParent(transform, false);
            sound = source.AddComponent<AudioSource>();
            sound.playOnAwake = false;
            sound.loop = true;
            sound.volume = .08f;
            const int rate = 24000;
            var samples = new float[rate * 9];
            var random = new System.Random(302);
            var breeze = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                breeze = breeze * .993f + ((float)random.NextDouble() * 2 - 1) * .007f;
                var phase = i / (float)(samples.Length - 1);
                samples[i] = breeze * Mathf.Pow(Mathf.Max(0, Mathf.Sin(phase * Mathf.PI)), .4f) * 3;
            }
            soundClip = AudioClip.Create("Original paper village air", samples.Length, 1, rate, false);
            soundClip.SetData(samples, 0);
            sound.clip = soundClip;
        }

        private void OnApplicationPause(bool paused) { if (paused) SecretCard?.MaskImmediately(); }
        private void OnApplicationFocus(bool focused) { if (!focused) SecretCard?.MaskImmediately(); }
        private void OnDisable() { if (SecretCard != null) { SecretCard.MaskImmediately(); SecretCard.enabled = false; } }
        private void OnEnable() { if (SecretCard != null) SecretCard.enabled = true; }
        private void OnDestroy()
        {
            if (Session != null) { Session.ConcealSecret(); Session.Changed -= ModelChanged; }
            if (Preferences != null) Preferences.Changed -= ApplyPreferences;
            if (soundClip != null) Destroy(soundClip);
        }
    }

    internal sealed class UnityPreferenceStore : IPreferenceStore
    {
        public bool ReadBool(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) != 0;
        public void WriteBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
        public void Save() => PlayerPrefs.Save();
    }
}
