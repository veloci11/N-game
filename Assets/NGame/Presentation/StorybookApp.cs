using NGame.Core;
using NGame.Core.Multiplayer;
using NGame.Presentation.Game;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace NGame.Presentation
{
    public enum AppRoute { Home, Preparation, Village, RoleLibrary, Play, GameHome }

    /// <summary>Application navigation and presentation only. All local preview state belongs to VillageSession.</summary>
    public sealed class StorybookApp : MonoBehaviour
    {
        public AppRoute Route { get; private set; } = AppRoute.Home;
        public VillageSession Session { get; private set; }
        public AppPreferences Preferences { get; private set; }
        public SecretCardView SecretCard { get; private set; }
        public RoleLibraryView RoleLibrary { get; private set; }
        public UnityRoomClient NetworkClient { get; private set; }
        public RoomFlowView Game { get; private set; }
        public GameHubView Hub { get; private set; }
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
        private TextMeshProUGUI gatheringCaption;
        private UnityEngine.UI.Button navigation;
        private UnityEngine.UI.Button contextualHelp;
        private readonly GameObject[] burgerBars = new GameObject[3];
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
        private TextMeshProUGUI narrationSetting;
        private UnityEngine.UI.Image chosenPortrait;
        private UnityEngine.UI.Button confirmationButton;
        private readonly UnityEngine.UI.Image[] playerTiles = new UnityEngine.UI.Image[6];
        private VillageStage stage;
        private RoomView lastSceneSnapshot;
        public HubBackdrop HubBackground { get; private set; }
        private GameObject readingVeil;
        private GameObject hubBrandMark;
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
            var connection = new GameObject("Room connection");
            connection.transform.SetParent(transform, false);
            NetworkClient = connection.AddComponent<UnityRoomClient>();
            var playRoot = UITheme.Box("Connected or practice game", viewport, true);
            Game = playRoot.gameObject.AddComponent<RoomFlowView>();
            Game.Initialize(Preferences, NetworkClient, stage.TransitionToDaylight, () => stage.DaylightAmount);
            gameObject.AddComponent<PublicNarrator>().Initialize(Preferences, () => Game.Client, () => Route == AppRoute.Play);
            Game.RequestedHome += OpenHome;
            var nightTouchRoot = UITheme.Box("Shared night touch ambience", viewport, true);
            nightTouchRoot.gameObject.AddComponent<NightCommonTapFeedback>().Initialize(Preferences,
                () => Route == AppRoute.Play && Game.Client?.View?.phase == "night" && Game.Client.View.mode == "in-person" && Game.Client.View.settings?.nightProtocol != "screen",
                () => IsModalOpen || Game.IsMenuOpen);
            var hubRoot = UITheme.Box("N’GAME game catalogue", viewport, true);
            Hub = hubRoot.gameObject.AddComponent<GameHubView>();
            Hub.Initialize(Preferences, id =>
            {
                if (id != GameHubView.WerewolfGameId) return;
                if (Hub.WantsToCreate) OpenPlay(); else OpenGameHome();
            }, OpenPlay, ShowSettings, OpenPlay);
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
            var hubAmbience = UITheme.Box("N’GAME universal background", root, true);
            HubBackground = hubAmbience.gameObject.AddComponent<HubBackdrop>();
            HubBackground.Initialize(Preferences);
            AddReadingVeil(root);
            viewport = UITheme.Box("Safe portrait content", root, true);
            viewport.gameObject.AddComponent<SafeViewport>();
            HubBackground.SetContentFrame(viewport);
        }

        private void AddReadingVeil(Transform parent)
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
            readingVeil = UITheme.Surface("Gentle ink wash", parent, Color.white, sprite, true).gameObject;
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
            navigation = Button("Safe application navigation", viewport, "‹", () =>
            {
                if (Route == AppRoute.Play && Game?.Client?.HasSession == true) Game.OpenMenu();
                else if (Route == AppRoute.GameHome) OpenHome();
                else OpenGameHome();
            }, new Vector2(0, 1), 40, -40, 42, 42, false);
            for (var i = 0; i < burgerBars.Length; i++)
            {
                var bar = UITheme.Surface("Safe burger line " + i, navigation.transform, UITheme.Parchment);
                UITheme.Position(bar.rectTransform, UITheme.Center, 0, (i - 1) * 6, 18, 1.6f);
                burgerBars[i] = bar.gameObject; bar.gameObject.SetActive(false);
            }
            header = Label("Current chapter heading", viewport, "N’GAME", 27, UITheme.Parchment,
                new Vector2(0, 1), 162, -40, 183, 43);
            header.alignment = TextAlignmentOptions.MidlineLeft;
            var brandMark = UITheme.Box("N’GAME folded paper mark", viewport);
            UITheme.Position(brandMark, new Vector2(0, 1), 40, -40, 40, 30);
            brandMark.gameObject.AddComponent<PaperCraneMark>().Initialize(Preferences);
            hubBrandMark = brandMark.gameObject;
            contextualHelp = Button("Open contextual help", viewport, "?", () =>
            {
                if (Route == AppRoute.Play) Game.OpenRoomHelp(); else ShowGameHelp();
            }, new Vector2(1, 1), -40, -40, 44, 44, false);
            var helpSurface = contextualHelp.GetComponent<UnityEngine.UI.Image>();
            helpSurface.sprite = UITheme.Disc;
            helpSurface.color = UITheme.Opacity(UITheme.Midnight, .91f);
            UITheme.Edge(helpSurface, UITheme.Opacity(UITheme.Brass, .8f));
            contextualHelp.GetComponentInChildren<TMP_Text>().fontSize = 27;
        }

        private void BuildHome()
        {
            homeView = UITheme.Box("Storybook home", viewport, true).gameObject;
            Label("Home title", homeView.transform, "Loup-Garou", 59, UITheme.Parchment,
                UITheme.Top, 0, -232, 382, 172);
            Label("Home invitation", homeView.transform, "Crée une soirée ou rejoins tes amis.", 22,
                UITheme.Parchment, UITheme.Top, 0, -349, 350, 74);
            UITheme.Separator(homeView.transform, UITheme.Bottom, 294);
            var gathering = Button("Open real gatherings", homeView.transform, "Créer ou rejoindre", OpenPlay,
                UITheme.Bottom, 0, 124, 382, 56);
            gatheringCaption = gathering.GetComponentInChildren<TextMeshProUGUI>();
            Button("Start individual practice", homeView.transform, "S’entraîner seul", StartTraining,
                UITheme.Bottom, 0, 62, 382, 48, false);
            Label("Training scope", homeView.transform, "Avec des villageois simulés", 10, UITheme.Quiet,
                UITheme.Bottom, 0, 21, 350, 22, false);
            // The earlier illustration/composition previews stay available to Editor tools and tests,
            // but must not masquerade as additional playable paths in the player's home screen.

        }

        private void BuildPreparation()
        {
            preparationView = UITheme.Box("Gathering composition helper", viewport, true).gameObject;
            Label("Preparation title", preparationView.transform, "Préparer une partie", 38, UITheme.Parchment,
                UITheme.Top, 0, -138, 390, 65);
            Label("Preparation introduction", preparationView.transform, "Choisis le nombre de joueurs.", 19,
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
            Button("Explore local illustration", preparationView.transform, "Ouvrir l’aperçu local", EnterVillage,
                UITheme.Bottom, 0, 72, 370, 55);
            UpdateComposition();
        }

        private void BuildVillage()
        {
            villageView = UITheme.Box("Village chapter", viewport, true).gameObject;
            chapter = Label("Chapter overline", villageView.transform, "APERÇU LOCAL", 10, UITheme.Brass,
                UITheme.Top, 0, -125, 360, 25, false);
            chapter.characterSpacing = 3.5f;
            villageTitle = Label("Village narration", villageView.transform, "Découvrir son rôle", 38, UITheme.Parchment,
                UITheme.Top, 0, -176, 400, 75);
            UITheme.Separator(villageView.transform, UITheme.Top, -222, 90);
            phaseCaption = Label("Village time", villageView.transform, "Nuit 1", 21, UITheme.Parchment,
                UITheme.Top, 0, -250, 320, 35);
            nightView = UITheme.Box("Secret discovery", villageView.transform, true).gameObject;
            Label("Privacy invitation", nightView.transform, "Cache ton écran aux autres joueurs.", 17, UITheme.Parchment,
                UITheme.Bottom, 0, 156, 350, 29);
            var hold = Button("Hold role reveal", nightView.transform, "", null,
                UITheme.Bottom, 0, 109, 370, 58, false);
            SecretCard = hold.gameObject.AddComponent<SecretCardView>();
            SecretCard.Initialize(Session, nightView.transform, Preferences);
            var dawnButton = Button("Discover sunrise", nightView.transform, "Voir le lever du jour  →", AdvanceToDawn,
                UITheme.Bottom, 0, 55, 350, 42, false);
            dawnButton.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            dawnCaption = Label("Sunrise whisper", villageView.transform, "Passage au jour", 25,
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
            Label("Vote scope", panel.transform, "Sélectionne un personnage, puis confirme.", 10,
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
            var night = Button("Return to midnight", voteView.transform, "←  Revenir à la nuit",
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
            Hub.ShowCatalogue();
            Hub.SetHasSavedRoom(NetworkClient.HasSession);
            RenderRoute();
        }

        public void OpenGameHome()
        {
            if (!initialized) return;
            CloseDialog();
            SecretCard.MaskImmediately();
            Route = AppRoute.GameHome;
            RenderRoute();
        }

        public void OpenPlay() => OpenClient(NetworkClient);

        public void OpenClient(IRoomClient client)
        {
            if (!initialized || client == null) return;
            CloseDialog();
            SecretCard.MaskImmediately();
            if (!ReferenceEquals(Game.Client, client)) Game.BindClient(client);
            Route = AppRoute.Play;
            RenderRoute();
        }

        public void StartTraining()
        {
            if (!initialized) return;
            if (NetworkClient.HasSession || NetworkClient.IsBusy)
            {
                var pendingConnection = NetworkClient.IsBusy && !NetworkClient.HasSession;
                var panel = Dialog(pendingConnection ? "CONNEXION" : "PARTIE ENREGISTRÉE",
                    pendingConnection ? "Connexion en cours" : "Une partie est en cours", 360);
                Label("Saved party training explanation", panel,
                    pendingConnection ? "Attends la fin de la connexion avant de lancer un entraînement."
                        : "Reprends ta soirée ou quitte-la depuis son menu avant de lancer un entraînement.",
                    18, UITheme.Parchment, UITheme.Top, 0, -170, 316, 88);
                Button("Resume party before practice", panel, pendingConnection ? "Voir la connexion" : "Reprendre ma partie", OpenPlay,
                    UITheme.Bottom, 0, 106, 322, 53);
                Button("Return without replacing party", panel, "Retour", CloseDialog,
                    UITheme.Bottom, 0, 45, 322, 48, false);
                return;
            }
            var practice = GetComponentInChildren<PracticeRoomClient>(true);
            if (practice == null)
            {
                var practiceObject = new GameObject("Individual practice engine");
                practiceObject.transform.SetParent(transform, false);
                practice = practiceObject.AddComponent<PracticeRoomClient>();
            }
            var name = Game.DisplayNameInput != null ? Game.DisplayNameInput.text : "Moi";
            Game.BindClient(practice);
            practice.CreateRoom(name, 0, new RoomSettings());
            OpenClient(practice);
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
            if (navigation != null) navigation.gameObject.SetActive(Route != AppRoute.Home);
            HubBackground.SetVisible(Route == AppRoute.Home);
            readingVeil.SetActive(Route != AppRoute.Home);
            header.color = Route == AppRoute.Home ? UITheme.Midnight : UITheme.Parchment;
            header.fontSize = Route == AppRoute.Home ? 37 : 27;
            header.enableAutoSizing = Route == AppRoute.Home;
            header.fontSizeMin = 27; header.fontSizeMax = 37;
            header.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.Position(header.rectTransform, new Vector2(0, 1), Route == AppRoute.Home ? 185 : 162, -40,
                Route == AppRoute.Home ? 230 : 183, Route == AppRoute.Home ? 58 : 43);
            hubBrandMark.SetActive(Route == AppRoute.Home);
            homeView.SetActive(Route == AppRoute.GameHome);
            Hub.gameObject.SetActive(Route == AppRoute.Home);
            contextualHelp.gameObject.SetActive(Route != AppRoute.Home);
            contextualHelp.GetComponentInChildren<TMP_Text>().text = "?";
            preparationView.SetActive(Route == AppRoute.Preparation);
            villageView.SetActive(Route == AppRoute.Village);
            RoleLibrary.gameObject.SetActive(Route == AppRoute.RoleLibrary);
            Game.gameObject.SetActive(Route == AppRoute.Play);
            Game.SetRouteActive(Route == AppRoute.Play);
            header.text = Route == AppRoute.Home ? "N’GAME" : Route == AppRoute.Preparation ? "Préparation"
                : Route == AppRoute.RoleLibrary ? "Les rôles" : "Loup-Garou";
            stage.SetHomeComposition(Route != AppRoute.Village && Route != AppRoute.Play);
            if (Route != AppRoute.Village && Route != AppRoute.Play) stage.SetDaylight(0);
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
            chapter.text = "APERÇU LOCAL";
            villageTitle.text = phase == VillagePhase.Night ? "Découvrir son rôle" : phase == VillagePhase.Dawn ? "Passage au jour"
                : phase == VillagePhase.Vote ? "Choisir une cible" : "Choix confirmé";
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
            if (initialized && stage != null)
            {
                var snapshot = Route == AppRoute.Play ? Game?.Client?.View : null;
                if (!ReferenceEquals(lastSceneSnapshot, snapshot)) { lastSceneSnapshot = snapshot; stage.BindPublic(snapshot); }
            }
            if (initialized && gatheringCaption != null)
                gatheringCaption.text = NetworkClient != null && NetworkClient.HasSession ? "Reprendre ma soirée" : "Créer ou rejoindre";
            if (initialized && Hub != null && Route == AppRoute.Home)
            {
                Hub.SetHasSavedRoom(NetworkClient != null && NetworkClient.HasSession);
                HubBackground.SetCatalogueState(NetworkClient != null && NetworkClient.HasSession, Hub.IsChoosingForCreation);
            }
            if (initialized && navigation != null)
            {
                var room = Route == AppRoute.Play && Game?.Client?.HasSession == true;
                navigation.GetComponentInChildren<TMP_Text>().enabled = !room;
                foreach (var bar in burgerBars) if (bar != null) bar.SetActive(room);
            }
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
            Game?.SetExternalBlock(true);
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
            var panel = Dialog("PRÉFÉRENCES", "Réglages", 480);
            var audio = Button("Ambient sound setting", panel, "", () => Preferences.SetAmbientSound(!Preferences.AmbientSound),
                UITheme.Top, 0, -159, 322, 55, false);
            audioSetting = audio.GetComponentInChildren<TextMeshProUGUI>();
            audioSetting.fontSize = 19;
            var motion = Button("Motion comfort setting", panel, "", () => Preferences.SetReducedMotion(!Preferences.ReducedMotion),
                UITheme.Top, 0, -226, 322, 55, false);
            motionSetting = motion.GetComponentInChildren<TextMeshProUGUI>();
            motionSetting.fontSize = 19;
            var narration = Button("Public phase narration setting", panel, "", () => Preferences.SetPublicNarration(!Preferences.PublicNarration),
                UITheme.Top, 0, -293, 322, 55, false);
            narrationSetting = narration.GetComponentInChildren<TextMeshProUGUI>();
            narrationSetting.fontSize = 19;
            Label("Reduced motion explanation", panel, "Animations réduites : moins de mouvements\net des transitions plus courtes.", 16,
                UITheme.Quiet, UITheme.Top, 0, -354, 322, 57);
            Button("Close preferences", panel, "Fermer", CloseDialog,
                UITheme.Bottom, 0, 45, 322, 53);
            ApplyPreferences();
        }

        public void ShowGameHelp()
        {
            if (!initialized) return;
            var panel = Dialog("AIDE", "Loup-Garou", 370);
            Button("Help read rules", panel, "Comment jouer", ShowRules, UITheme.Top, 0, -158, 314, 52);
            Button("Help browse roles", panel, "Voir les rôles", () => { CloseDialog(); OpenRoleLibrary(); }, UITheme.Top, 0, -220, 314, 52, false);
            Button("Help close", panel, "Fermer", CloseDialog, UITheme.Bottom, 0, 48, 314, 46, false);
        }

        public void ShowRules()
        {
            if (!initialized) return;
            var panel = Dialog("LOUP-GAROU", "Comment jouer", 487);
            Label("Game concept", panel,
                "6 à 24 joueurs, un appareil chacun.\nLoups contre village.\n\nLa nuit, suis ton rôle.\nLe jour, discute puis vote.\n\nMaintiens ta carte pour voir ton rôle.\nRelâche pour le cacher.", 21,
                UITheme.Parchment, UITheme.Top, 0, -241, 323, 250);
            Label("Rules prototype scope", panel, "Entre amis : chaque personne garde son appareil.\nEn solo : les autres habitants sont simulés.", 11,
                UITheme.Quiet, UITheme.Bottom, 0, 113, 322, 49, false);
            Button("Return from rules", panel, "J’ai compris", CloseDialog, UITheme.Bottom, 0, 48, 322, 54);
        }

        public void CloseDialog()
        {
            if (dialog != null) { dialog.SetActive(false); Destroy(dialog); dialog = null; }
            audioSetting = motionSetting = narrationSetting = null;
            Game?.SetExternalBlock(false);
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
            if (narrationSetting != null) narrationSetting.text = Preferences.PublicNarration ? "Appels publics  ·  Oui" : "Appels publics  ·  Non";
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

        private void OnApplicationPause(bool paused) { if (paused) { SecretCard?.MaskImmediately(); Game?.ForceMaskSecrets(); } }
        private void OnApplicationFocus(bool focused) { if (!focused) { SecretCard?.MaskImmediately(); Game?.ForceMaskSecrets(); } }
        private void OnDisable() { if (SecretCard != null) { SecretCard.MaskImmediately(); SecretCard.enabled = false; } Game?.SetExternalBlock(true); }
        private void OnEnable() { if (SecretCard != null) SecretCard.enabled = true; Game?.SetExternalBlock(false); }
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
