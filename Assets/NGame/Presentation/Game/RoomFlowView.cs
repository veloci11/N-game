using System;
using System.Collections.Generic;
using System.Text;
using NGame.Core;
using NGame.Core.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NGame.Presentation.Game
{
    /// <summary>One stable player frame for real rooms and the same-engine practice adapter.</summary>
    public sealed partial class RoomFlowView : MonoBehaviour
    {
        public IRoomClient Client { get; private set; }
        public RoomSecretCard SecretCard { get; private set; }
        public bool SecretsVisible => SecretCard != null && SecretCard.IsRevealed;
        public bool IsPrivateActionVisible => privateContent != null && privateContent.gameObject.activeInHierarchy;
        public bool IsMenuOpen => menu != null && menu.activeSelf;
        public bool IsQuitConfirmationOpen => quitConfirmation;
        public bool IsResumingAccess => Client?.HasSession == true && Client.View == null;
        public TMP_InputField DisplayNameInput { get; private set; }
        public TMP_InputField JoinCodeInput { get; private set; }
        public TMP_InputField EndpointInput { get; private set; }
        public UnityEngine.UI.Button PrimaryAction { get; private set; }
        public UnityEngine.UI.Button SecondaryAction { get; private set; }
        public event Action RequestedHome;
        private AppPreferences preferences;
        private Action<float> setDaylight;
        private Func<float> readingLight;
        private GameObject entry, lobby, game, composer, menu;
        private RectTransform entryContent, lobbyContent, body, publicContent, privateContent, roleHeader;
        private TextMeshProUGUI title, subtitle, error, clock, roleCaption, roleSummary;
        private UnityEngine.UI.Button retry, hold, info;
        private readonly UnityEngine.UI.Image[] avatarFrames = new UnityEngine.UI.Image[6];
        private readonly List<string> targets = new List<string>();
        private int avatar, lastTurn = -1, actionTurn, rosterPage, targetPage;
        private string choice, lastKind, lastMatch, lastBodyFingerprint;
        private TextMeshProUGUI voteProgress, acknowledgementProgress;
        private long lastRevision = -1;
        private bool lastBusy, lastConnected, routeActive, externalBlock, keyboardHold, keyboardRearm, built, rendering;
        private bool privateOpened, quitConfirmation, viewedRole, advancedConnection;
        private RoomSettings creationSettings = new RoomSettings();
        private Action primaryHandler, secondaryHandler;

        public void Initialize(AppPreferences settings, IRoomClient client, Action<float> daylight, Func<float> lightAmount = null)
        {
            if (built) return;
            preferences = settings; setDaylight = daylight; readingLight = lightAmount;
            Build(); built = true; BindClient(client);
        }

        public void BindClient(IRoomClient client)
        {
            ForceMaskSecrets(); CloseMenu();
            if (Client != null)
            {
                Client.Changed -= Render; Client.SecretsMustHide -= ForceMaskSecrets; UnbindCommunications(); Client.SetActive(false);
            }
            Client = client; lastTurn = -1; lastRevision = -1; lastMatch = null;
            targets.Clear(); choice = null; viewedRole = false;
            if (Client != null)
            {
                Client.Changed += Render; Client.SecretsMustHide += ForceMaskSecrets; BindCommunications(); Client.SetActive(routeActive);
            }
            Render();
        }

        private void Build()
        {
            title = UITheme.Copy("Real room heading", transform, "Créer ou rejoindre", 34, UITheme.Parchment);
            UITheme.Position(title.rectTransform, UITheme.Top, 0, -116, 382, 64);
            title.overflowMode = TextOverflowModes.Overflow;
            subtitle = UITheme.Copy("Real room public status", transform, "", 15, UITheme.Parchment, false);
            UITheme.Position(subtitle.rectTransform, UITheme.Top, 0, -174, 370, 50);
            subtitle.overflowMode = TextOverflowModes.Overflow;
            clock = UITheme.Copy("Server deadline", transform, "", 12, UITheme.Brass, false);
            UITheme.Position(clock.rectTransform, UITheme.Top, 0, -213, 370, 26);
            entry = UITheme.Box("Create or join a real room", transform, true).gameObject;
            entryContent = GameUI.Scroll(entry.transform, "Gathering entry", 236, 175);
            GameUI.Text(entryContent, "Nickname label", "Ton pseudo", 23, 42);
            DisplayNameInput = GameUI.Input(entryContent, "Player nickname", "Ton pseudo", 24);
            GameUI.Text(entryContent, "Avatar label", "Choisis un avatar", 16, 30);
            var portraits = GameUI.Row(entryContent, "Avatar choices", 54, 4);
            for (var i = 0; i < 6; i++)
            {
                var index = i;
                var option = GameUI.Button(portraits, "Avatar " + i, "", () => ChooseAvatar(index), false, 48);
                avatarFrames[i] = option.GetComponent<UnityEngine.UI.Image>();
                var picture = UITheme.Surface("Paper avatar", option.transform, Color.white, UITheme.Portrait(i), true);
                picture.preserveAspect = true; picture.rectTransform.offsetMin = new Vector2(4, 4); picture.rectTransform.offsetMax = new Vector2(-4, -4);
            }
            ChooseAvatar(0);
            GameUI.Text(entryContent, "Room invitation", "Tu as reçu un code ?", 22, 37);
            JoinCodeInput = GameUI.Input(entryContent, "Village invitation code", "Code du village", 12);
            JoinCodeInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
            GameUI.Text(entryContent, "Invitation instruction", "Renseigne ton pseudo, puis crée une soirée ou rejoins celle de tes amis.", 14, 65, false, UITheme.Quiet);
            var modes = GameUI.Row(entryContent, "Creation mode choices", 44);
            GameUI.Button(modes, "Create in person", "En présence", () => { creationSettings.mode = "in-person"; Render(); }, false, 44);
            GameUI.Button(modes, "Create remote", "À distance", () => { creationSettings.mode = "remote"; Render(); }, false, 44);
            var modeHint = GameUI.Text(entryContent, "Creation mode description", "", 13, 55, false, UITheme.Brass);
            modeHint.gameObject.AddComponent<ModeDescription>().Read = () => creationSettings.mode;
            GameUI.Button(entryContent, "Advanced room connectivity", "Paramètres de connexion", () =>
            {
                advancedConnection = !advancedConnection;
                EndpointInput.transform.parent.gameObject.SetActive(advancedConnection);
            }, false, 42);
            var network = GameUI.Column(entryContent, "Advanced server panel");
            GameUI.Text(network, "Connectivity explanation", "Tous les téléphones doivent utiliser le même serveur de soirée.", 13, 56, false, UITheme.Quiet);
            EndpointInput = GameUI.Input(network, "Reachable room server", "https://serveur-de-la-soiree", 180);
            EndpointInput.keyboardType = TouchScreenKeyboardType.URL;
            GameUI.Button(network, "Apply server address", "Enregistrer l’adresse", ApplyEndpoint, false, 43);
            network.gameObject.SetActive(false);
            lobby = UITheme.Box("Real gathering lobby", transform, true).gameObject;
            lobbyContent = GameUI.Scroll(lobby.transform, "Waiting for the village", 236, 175);
            game = UITheme.Box("Authoritative game projection", transform, true).gameObject;
            var cardPaper = UITheme.Surface("Personal story card paper", game.transform, UITheme.Opacity(UITheme.Midnight, .69f), UITheme.Panel);
            UITheme.Position(cardPaper.rectTransform, UITheme.Top, 0, -330, 382, 202);
            UITheme.Edge(cardPaper, UITheme.Opacity(UITheme.Brass, .42f));
            roleHeader = cardPaper.rectTransform;
            roleCaption = UITheme.Copy("Personal role caption", roleHeader, "Ton rôle reste secret.", 25, UITheme.Parchment);
            UITheme.Position(roleCaption.rectTransform, UITheme.Center, 67, 38, 200, 60);
            roleCaption.overflowMode = TextOverflowModes.Overflow;
            roleSummary = UITheme.Copy("Personal role instruction", roleHeader, "Maintiens le bouton du bas pour regarder ta carte.", 13, UITheme.Quiet, false);
            UITheme.Position(roleSummary.rectTransform, UITheme.Center, 67, -17, 192, 53);
            roleSummary.overflowMode = TextOverflowModes.Overflow;
            info = UITheme.Action("Open personal information", game.transform, "Mes informations", OpenPrivateInformation, false);
            UITheme.Position((RectTransform)info.transform, UITheme.Bottom, 0, 194, 200, 34);
            info.GetComponentInChildren<TMP_Text>().fontSize = 17;
            body = GameUI.Scroll(game.transform, "Current player view", 445, 225);
            privateContent = GameUI.Column(body, "Private information and actions", 0, 8);
            publicContent = GameUI.Column(body, "Public village information", 0, 8);
            PrimaryAction = UITheme.Action("Player primary action", transform, "Créer un village", () => primaryHandler?.Invoke());
            UITheme.Position((RectTransform)PrimaryAction.transform, UITheme.Bottom, 0, 124, 382, 56);
            SecondaryAction = UITheme.Action("Player secondary action", transform, "Rejoindre le village", () => secondaryHandler?.Invoke(), false);
            UITheme.Position((RectTransform)SecondaryAction.transform, UITheme.Bottom, 0, 62, 382, 48);
            hold = SecondaryAction;
            SecretCard = hold.gameObject.AddComponent<RoomSecretCard>();
            SecretCard.Initialize(roleHeader, preferences);
            SecretCard.VisibilityChanged += RevealChanged;
            error = UITheme.Copy("Room connection feedback", transform, "", 11, UITheme.Parchment, false);
            UITheme.Position(error.rectTransform, UITheme.Bottom, 0, 18, 382, 32);
            error.overflowMode = TextOverflowModes.Overflow;
            error.enableAutoSizing = true; error.fontSizeMin = 10; error.fontSizeMax = 11;
            retry = UITheme.Action("Retry last room operation", transform, "Réessayer", () => Client?.RetryPending(), false);
            UITheme.Position((RectTransform)retry.transform, UITheme.Top, 0, -215, 240, 31);
            retry.GetComponentInChildren<TMP_Text>().fontSize = 14;
            BuildChatAccess();
        }

        public void CreateRoom()
        {
            if (Client?.HasSession == true) { error.text = "Reprends ta place ou confirme d’abord ton départ depuis le menu."; return; }
            if (Client == null || string.IsNullOrWhiteSpace(DisplayNameInput.text)) { error.text = "Choisis un pseudo pour entrer dans le village."; return; }
            Client.CreateRoom(DisplayNameInput.text.Trim(), avatar, creationSettings);
        }
        public void JoinRoom()
        {
            if (Client?.HasSession == true) { error.text = "Reprends ta place ou confirme d’abord ton départ depuis le menu."; return; }
            if (Client == null || string.IsNullOrWhiteSpace(DisplayNameInput.text)) { error.text = "Choisis un pseudo avant de rejoindre tes amis."; return; }
            if (string.IsNullOrWhiteSpace(JoinCodeInput.text)) { error.text = "Entre le code reçu de tes amis."; return; }
            Client.JoinRoom(JoinCodeInput.text, DisplayNameInput.text.Trim(), avatar);
        }
        private void ChooseAvatar(int index)
        {
            avatar = index;
            for (var i = 0; i < avatarFrames.Length; i++) if (avatarFrames[i] != null)
                avatarFrames[i].color = i == avatar ? UITheme.Opacity(UITheme.Brass, .8f) : UITheme.Opacity(UITheme.Midnight, .82f);
        }
        private void ApplyEndpoint()
        {
            if (!(Client is UnityRoomClient http)) return;
            if (!http.ConfigureServer(EndpointInput.text)) error.text = "Entre une adresse http:// ou https:// valide.";
            else { EndpointInput.text = http.ServerUrl; error.text = "Adresse enregistrée. Retrouve maintenant tes amis."; }
        }
        public void SetRouteActive(bool active)
        {
            routeActive = active; Client?.SetActive(active);
            if (!active) { ForceMaskSecrets(); CloseMenu(); CloseChatPanel(); }
            else { Client?.Refresh(); Render(); }
        }
        public void SetExternalBlock(bool blocked)
        {
            externalBlock = blocked; SecretCard?.SetBlocked(blocked);
            if (blocked) { ForceMaskSecrets(); CloseChatPanel(); }
        }
        public void ForceMaskSecrets()
        {
            keyboardHold = false; keyboardRearm = true; privateOpened = false;
            SecretCard?.MaskImmediately(); ErasePrivate(); MaskChatForInterruption();
            if (built && !rendering && Client?.View != null)
            {
                if (game != null && game.activeInHierarchy) RenderBody(Client.View);
                ConfigureDock(Client.View);
            }
        }
        private void RevealChanged(bool visible)
        {
            if (visible) viewedRole = true;
            RenderCardCaption();
            if (built && Client?.View != null) { RenderCardLayout(Client.View); ConfigureDock(Client.View); }
        }
        private void RenderCardCaption()
        {
            if (roleCaption == null) return;
            var view = Client?.View;
            roleCaption.text = SecretsVisible ? view?.selfRoleName ?? "Ton rôle" : "Ton rôle reste secret.";
            roleSummary.text = SecretsVisible ? RoleCatalog.Find(view?.selfRoleId)?.Summary ?? "Garde cette carte pour toi."
                : "Maintiens le bouton du bas pour regarder ta carte.";
        }

        private void Render()
        {
            if (!built || rendering || Client == null) return;
            rendering = true;
            try
            {
                var view = Client.View;
                var inRoom = Client.HasSession && view != null;
                var pending = Client is UnityRoomClient httpPending && httpPending.HasPendingCommand;
                entry.SetActive(!inRoom && !IsResumingAccess); lobby.SetActive(inRoom && view.phase == "lobby"); game.SetActive(inRoom && view.phase != "lobby");
                retry.gameObject.SetActive(!string.IsNullOrEmpty(Client.StatusError) || pending);
                retry.interactable = !Client.IsBusy;
                error.text = Client.StatusError ?? (pending ? "Vérification de ta dernière action…" : Client.IsTraining ? "Entraînement · les autres habitants sont simulés" : "");
                EndpointInput.transform.parent.gameObject.SetActive(advancedConnection && Client is UnityRoomClient && !inRoom);
                if (Client is UnityRoomClient http && !EndpointInput.isFocused) EndpointInput.SetTextWithoutNotify(http.ServerUrl);
                if (view != null && view.serverTimeUnixMs > 0) serverOffsetMs = view.serverTimeUnixMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var changedTurn = view != null && (view.turn != lastTurn || view.action?.kind != lastKind);
                var changedMatch = view != null && view.matchId != lastMatch;
                if (changedTurn)
                {
                    privateOpened = false; ErasePrivate(); targets.Clear(); choice = null; targetPage = 0;
                    if (view.action?.submittedTargetIds != null) targets.AddRange(view.action.submittedTargetIds);
                }
                if (changedMatch) { viewedRole = false; rosterPage = 0; ResetChatDrafts(); }
                SecretCard.BindSnapshot(view, Client.IsConnected && inRoom && view.phase != "lobby");
                SecretCard.SetBlocked(externalBlock || IsMenuOpen || IsChatOpen);
                if (!inRoom)
                {
                    title.text = IsResumingAccess ? "Reprendre la partie" : "Créer ou rejoindre";
                    subtitle.text = IsResumingAccess ? "Ta place est sauvegardée. Reconnecte-toi pour retrouver ton rôle et ta partie."
                        : Client.IsBusy ? "Connexion en cours…" : "Crée une soirée ou saisis un code.";
                    clock.text = ""; if (routeActive) setDaylight?.Invoke(0);
                    if (IsResumingAccess) SetDock("Reprendre ma place", () => Client.RetryPending(), !Client.IsBusy, "Options de reprise", OpenMenu, true);
                    else SetDock("Créer un village", CreateRoom, !Client.IsBusy, "Rejoindre le village", JoinRoom, !Client.IsBusy);
                    lastRevision = -1; lastMatch = null; RefreshChatAccess(); return;
                }
                title.text = !string.IsNullOrEmpty(view.phaseTitle) ? view.phaseTitle : PhaseTitle(view.phase, view.winner);
                subtitle.text = Client is PracticeRoomClient practice ? practice.PracticeHint : view.phase == "lobby" && view.mode == "in-person"
                    ? "Chacun son téléphone · l’hôte garde les appels publics audibles. Les autres confirment leur préparation."
                    : view.statusMessage;
                if (routeActive) setDaylight?.Invoke(view.phase == "night" || view.phase == "reaction" || view.phase == "lobby" || view.phase == "distribution" ? 0 : 1);
                var changed = changedTurn || view.revision != lastRevision || Client.IsBusy != lastBusy || Client.IsConnected != lastConnected;
                if (changed)
                {
                    if (view.phase == "lobby") RenderLobby(view);
                    else if (BodyFingerprint(view) != lastBodyFingerprint || Client.IsBusy != lastBusy || Client.IsConnected != lastConnected) RenderBody(view);
                }
                info.interactable = Client.IsConnected && !string.IsNullOrEmpty(view.selfRoleId) && !externalBlock;
                RenderCardLayout(view); RenderCardCaption(); ConfigureDock(view); RefreshChatAccess();
                if (view.phase == "vote" && voteProgress != null) voteProgress.text = view.votesReceived + " / " + view.votesExpected + " votes reçus · choix cachés";
                if (view.phase == "distribution" && acknowledgementProgress != null) acknowledgementProgress.text = view.acknowledgedCount + " / " + (view.players?.Length ?? 0) + " rôles confirmés";
                if (IsChatOpen) RefreshChatPanel();
                lastTurn = view.turn; lastKind = view.action?.kind; lastMatch = view.matchId;
                lastRevision = view.revision; lastBusy = Client.IsBusy; lastConnected = Client.IsConnected;
            }
            finally { rendering = false; }
        }

        private void RenderCardLayout(RoomView view)
        {
            var publicVote = view.phase == "vote" && !privateOpened;
            var expanded = publicVote || privateOpened;
            var immersive = view.phase == "distribution" || view.phase == "night" && !privateOpened || expanded && SecretsVisible;
            roleHeader.gameObject.SetActive(!expanded || SecretsVisible);
            if (expanded && SecretsVisible) roleHeader.SetAsLastSibling();
            var nightWaiting = immersive && view.phase == "night" && !privateOpened;
            UITheme.Position(roleHeader, UITheme.Top, 0, immersive ? (nightWaiting ? -545 : -423) : -330, 382, immersive ? 350 : 202);
            var paper = roleHeader.GetComponent<UnityEngine.UI.Image>();
            paper.color = immersive ? Color.clear : UITheme.Opacity(UITheme.Midnight, .78f);
            var edge = roleHeader.GetComponent<UnityEngine.UI.Outline>();
            if (edge != null) edge.enabled = !immersive;
            roleCaption.gameObject.SetActive(!immersive);
            roleSummary.gameObject.SetActive(!immersive);
            info.gameObject.SetActive(view.phase != "distribution" && !SecretsVisible);
            SecretCard.SetImmersivePresentation(immersive);
            var shell = (RectTransform)body.parent.parent;
            // While typing, give the conversation the available area and restore the same vote draft afterwards.
            shell.gameObject.SetActive(!(publicVote && ChatKeyboardInset > 0));
            shell.offsetMin = new Vector2(21, publicVote ? (IsChatOpen ? ChatDockTop + 10 : 280) : nightWaiting ? 570 : 225);
            shell.offsetMax = new Vector2(-21, expanded ? -236 : immersive ? (nightWaiting ? -236 : -618) : -445);
            var bodyLayout = body.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            bodyLayout.padding = expanded || nightWaiting ? new RectOffset(10, 10, 8, 8) : new RectOffset(17, 17, 17, 17);
            publicContent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = publicVote ? 4 : 8;
            privateContent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 4;
            var viewportPaper = body.parent.GetComponent<UnityEngine.UI.Image>();
            if (viewportPaper != null) viewportPaper.color = immersive ? Color.clear : UITheme.Opacity(UITheme.Midnight, .93f);
        }

        private void RefreshChatLayout()
        {
            var view = Client?.View;
            if (!built || view == null) return;
            if (view.phase == "vote" && !privateOpened) RenderBody(view);
            RenderCardLayout(view);
            ConfigureDock(view);
        }

        private void SetDock(string primary, Action onPrimary, bool enabledPrimary, string secondary, Action onSecondary, bool enabledSecondary)
        {
            primaryHandler = onPrimary; secondaryHandler = onSecondary;
            PrimaryAction.GetComponentInChildren<TMP_Text>().text = primary;
            PrimaryAction.interactable = enabledPrimary && !externalBlock && !IsMenuOpen && !SecretsVisible
                && (!IsChatOpen || Client?.View?.phase == "vote" && ChatKeyboardInset <= 0);
            SecondaryAction.GetComponentInChildren<TMP_Text>().text = secondary;
            SecondaryAction.interactable = enabledSecondary && !externalBlock && !IsMenuOpen && !IsChatOpen;
        }
        private void ConfigureDock(RoomView view)
        {
            var live = Client.IsConnected && !Client.IsBusy;
            if (view.phase == "lobby")
            {
                var isReady = Self(view)?.ready ?? false;
                if (view.isHost) SetDock("Commencer la partie", () => Send("start"), live && view.canStart,
                    isReady ? "Je ne suis plus prêt" : "Je suis prêt", ToggleReady, live);
                else SetDock(isReady ? "Je ne suis plus prêt" : "Je suis prêt", ToggleReady, live, "Les règles de la soirée", OpenRoomHelp, true);
                return;
            }
            var secondary = SecretsVisible ? "Relâcher pour masquer" : SecretCard.IsHolding ? "Garde le doigt appuyé…" : "Maintenir · mon rôle privé";
            Action primary = null; var enabled = false; var caption = "En attente…";
            if (view.phase == "distribution")
            {
                caption = view.roleAcknowledged ? "Ton rôle est confirmé" : "J’ai découvert mon rôle";
                primary = () => Send("acknowledge-role"); enabled = !view.roleAcknowledged && viewedRole && live;
            }
            else if (view.phase == "finished")
            { caption = view.isHost ? "Préparer une revanche" : "L’hôte prépare la revanche"; primary = () => Send("reset"); enabled = live && view.isHost; }
            else if (view.phase == "vote" && view.isHost && view.canAdvance && (view.action == null || view.action.submitted))
            { caption = "Clore le scrutin…"; primary = ShowVoteClosureConfirmation; enabled = live; }
            else if (view.action != null && view.action.kind == "vote")
            {
                if (view.action.submitted && view.isHost && view.canAdvance)
                { caption = "Clore le scrutin…"; primary = ShowVoteClosureConfirmation; enabled = live; }
                else
                {
                    caption = view.action.submitted ? "Ton vote est enregistré" : "Confirmer mon vote";
                    primary = SubmitAction; enabled = CanUseAction(view.action) && HasChosenAction(view.action) && targets.Count == NeededTargets(view.action, choice);
                }
            }
            else if (view.action != null && !string.IsNullOrEmpty(view.action.kind) && view.action.kind != "observe"
                && (!view.action.submitted || view.action.kind == "wolves"))
            {
                caption = privateOpened ? (view.action.submitted ? "Modifier mon choix" : "Confirmer mon choix") : "Ouvrir mon action privée";
                primary = privateOpened ? (Action)SubmitAction : OpenPrivateInformation;
                enabled = live && (!privateOpened || CanUseAction(view.action) && HasChosenAction(view.action) && targets.Count == NeededTargets(view.action, choice));
            }
            else if (view.action?.kind == "observe") { caption = "Lire ma consigne privée"; primary = OpenPrivateInformation; enabled = live; }
            else if (view.isHost && view.canAdvance) { caption = view.phase == "discussion" ? "Ouvrir le vote" : "Continuer"; primary = () => Send("advance"); enabled = live; }
            else if (view.action?.submitted == true) caption = "Ton choix est enregistré";
            else if (Self(view)?.alive == false) caption = "Tu observes le village";
            SetDock(caption, primary, enabled, secondary, null, live && !string.IsNullOrEmpty(view.selfRoleId));
            // RoomSecretCard owns the secondary pointer hold. The Button never submits or changes a phase.
        }

        private void RenderLobby(RoomView view)
        {
            GameUI.Clear(lobbyContent);
            GameUI.Text(lobbyContent, "Actual room invitation code", Client.IsTraining ? "Une partie d’essai" : view.code, Client.IsTraining ? 31 : 43, 62, true, UITheme.Brass);
            if (!Client.IsTraining) GameUI.Button(lobbyContent, "Copy invitation code", "Copier le code du village", () => GUIUtility.systemCopyBuffer = view.code, false, 40);
            GameUI.Text(lobbyContent, "Gathering player count", Client.IsTraining ? "Toi et sept habitants simulés" : (view.players?.Length ?? 0) + ((view.players?.Length ?? 0) == 1 ? " personne · une carte chacune" : " personnes · une carte chacune"), 16, 35);
            if (view.isHost && !view.canStart && !string.IsNullOrEmpty(view.startBlockedReason))
                GameUI.Text(lobbyContent, "Reason the game cannot start", view.startBlockedReason, 15, 66, false, UITheme.Brass);
            RenderRoster(lobbyContent, view, true);
            var deck = new StringBuilder();
            foreach (var count in view.composition ?? Array.Empty<NGame.Core.Multiplayer.RoleCount>())
                deck.Append(count.count).Append(' ').Append(RoleCatalog.Find(count.roleId)?.DisplayName ?? count.roleId).Append(" · ");
            GameUI.Text(lobbyContent, "Public selected roles", deck.Length > 0 ? deck.ToString().Trim(' ', '·') : "Composition automatique selon le nombre de joueurs.", 14, 84);
            GameUI.Text(lobbyContent, "Room mode", view.mode == "remote" ? "À distance · messages et micro uniquement pendant le vote de jour."
                : "En présence · chacun son téléphone. Les appels publics sont lus sur le téléphone de l’hôte : gardez-le ouvert et audible. Si la narration est coupée, une personne doit lire ces appels à la table.", 13, 110, false, UITheme.Quiet);
            if (view.isHost)
            {
                GameUI.Button(lobbyContent, "Edit actual room rules", "Rôles et durées", OpenComposer, false, 44);
                if (!Client.IsTraining) GameUI.Button(lobbyContent, "Manage gathering participants", "Gérer les participants", OpenParticipantManagement, false, 44);
            }
            GameUI.Button(lobbyContent, "Read actual room rules", "Comment se déroule la partie ?", OpenRoomHelp, false, 42);
        }

        private void RenderBody(RoomView view)
        {
            try
            {
            GameUI.Clear(publicContent); ErasePrivate();
            publicContent.gameObject.SetActive(!privateOpened);
            if (privateOpened) { RenderPrivate(); return; }
            if (view.phase == "distribution")
            {
                acknowledgementProgress = GameUI.Text(publicContent, "Role acknowledgements", view.acknowledgedCount + " / " + (view.players?.Length ?? 0) + " rôles confirmés", 18, 44, true, UITheme.Brass);
                acknowledgementProgress.color = UITheme.Parchment;
                acknowledgementProgress.outlineColor = new Color32(7, 14, 23, 255);
                acknowledgementProgress.outlineWidth = .22f;
                return;
            }
            if (view.phase == "vote")
            {
                voteProgress = GameUI.Text(publicContent, "Vote progress", view.votesReceived + " / " + view.votesExpected + " votes reçus · choix cachés", 13, 20, false, UITheme.Brass);
                if (Self(view)?.alive == false && view.action == null)
                    GameUI.Text(publicContent, "Eliminated vote status", "Tu es éliminé : lis le Village et échange avec les autres éliminés dans Observateurs. Tu ne votes plus.", 15, 70, false, UITheme.Parchment);
                else RenderAction(publicContent, view.action, false);
                if (view.mode == "remote" && !IsChatOpen) RenderVoiceControl(publicContent);
                return;
            }
            if (view.phase == "verdict") RenderVerdict(publicContent, view);
            else if (view.phase == "finished")
            {
                GameUI.Text(publicContent, "Actual winning side", PhaseTitle(view.phase, view.winner), 29, 67, true, UITheme.Brass);
                var names = new List<string>();
                foreach (var id in view.winnerIds ?? Array.Empty<string>()) names.Add(Name(view, id));
                if (names.Count > 0) GameUI.Text(publicContent, "Exact winning players", "Victoire : " + string.Join(", ", names), 17, 73, false, UITheme.Brass);
                GameUI.Text(publicContent, "Final reveal notice", "Rôles de la partie", 15, 64, false);
            }
            else if (view.phase == "discussion")
                GameUI.Text(publicContent, "Discussion instruction", view.mode == "remote" ? "Lisez le bilan. L’hôte ouvre ensuite le vote : messages et micro y seront disponibles."
                    : "Parlez autour de la table. Les messages écrits seront disponibles pendant le vote.", 17, 102, false);
            else if (view.phase == "reaction")
                GameUI.Text(publicContent, "Reaction instruction", view.action != null ? "Une dernière action t’attend. Ouvre ton panneau privé pour choisir."
                    : "Une dernière action doit être résolue avant de continuer.", 17, 86, false);
            else if (view.phase == "night")
            {
                var eliminated = Self(view)?.alive == false && view.action == null;
                GameUI.Text(publicContent, "Night personal availability", eliminated ? "Tu observes la partie" : view.action != null && !view.action.submitted ? "C’est à toi de jouer." : view.action?.submitted == true ? "Choix confirmé" : "Les autres rôles jouent", 24, 42);
                GameUI.Text(publicContent, "Night waiting explanation", eliminated ? "Aucune action ne t’est demandée. Les messages des Observateurs seront disponibles au vote."
                    : view.action != null && !view.action.submitted ? "Ouvre ton action privée, choisis puis confirme."
                    : view.action?.submitted == true ? "Ton choix est enregistré. Attends la fin de cet appel."
                    : Client.IsTraining ? "Les habitants simulés jouent. Ton action apparaîtra à ton tour."
                    : view.mode == "in-person" ? "Garde les yeux fermés jusqu’à l’appel de ton rôle." : "Tu recevras ton action quand ce sera ton tour.", 15, 48, false, UITheme.Quiet);
                return;
            }
            if (Self(view)?.alive == false && view.phase != "finished")
                GameUI.Text(publicContent, "Eliminated player notice", "Tu es éliminé : tu peux observer, sans intervenir auprès des joueurs encore en jeu.", 16, 65, false, UITheme.Brass);
            RenderRecentEvents(publicContent, view);
            if (view.phase != "night" && view.phase != "reaction") RenderRoster(publicContent, view, false);
            }
            finally { lastBodyFingerprint = BodyFingerprint(view); }
        }
        private string BodyFingerprint(RoomView view)
        {
            var key = new StringBuilder().Append(view.matchId).Append('|').Append(view.turn).Append('|').Append(view.phase).Append('|').Append(privateOpened)
                .Append('|').Append(view.roleAcknowledged).Append('|').Append(view.winner).Append('|').Append(view.voteRound)
                .Append('|').Append(view.selfVoteTargetId).Append('|').Append(view.selfVoteAbstained).Append('|').Append(JsonUtility.ToJson(view.action))
                .Append('|').Append(JsonUtility.ToJson(view.verdict)).Append('|').Append(rosterPage).Append('|').Append(targetPage);
            foreach (var player in view.players ?? Array.Empty<PlayerView>())
                key.Append('|').Append(player.id).Append(':').Append(player.displayName).Append(':').Append(player.alive).Append(':').Append(player.revealedRoleId);
            foreach (var fact in view.privateFacts ?? Array.Empty<PrivateFact>()) key.Append('|').Append(JsonUtility.ToJson(fact));
            foreach (var item in view.events ?? Array.Empty<PublicEvent>()) key.Append('|').Append(item.sequence);
            return key.ToString();
        }
        private void RenderRecentEvents(Transform parent, RoomView view)
        {
            var events = view.events ?? Array.Empty<PublicEvent>();
            for (var i = Math.Max(0, events.Length - 2); i < events.Length; i++)
                GameUI.Text(parent, "Public resolved event", events[i].text, 14, 65, false, UITheme.Quiet);
        }
        private void RenderVerdict(Transform parent, RoomView view)
        {
            var verdict = view.verdict;
            GameUI.Text(parent, "Vote verdict title", "Le résultat du vote", 26, 43);
            if (verdict == null) { GameUI.Text(parent, "Vote verdict pending", view.statusMessage, 17, 83); return; }
            var result = !string.IsNullOrEmpty(verdict.eliminatedPlayerId) ? Name(view, verdict.eliminatedPlayerId) + " quitte le village."
                : verdict.result == "runoff" ? "Égalité : un second tour va départager les suspects." : "Personne n’est éliminé à ce scrutin.";
            GameUI.Text(parent, "Vote verdict result", result, 21, 90, true, UITheme.Brass);
            foreach (var vote in verdict.votes ?? Array.Empty<VoteRecordView>())
                GameUI.Text(parent, "Published ballot", Name(view, vote.playerId) + " → " + (vote.abstained ? "abstention" : Name(view, vote.targetId)), 14, 36, false);
        }
        private void RenderRoster(Transform parent, RoomView view, bool gathering)
        {
            var players = view.players ?? Array.Empty<PlayerView>();
            const int pageSize = 6;
            var pages = Math.Max(1, (players.Length + pageSize - 1) / pageSize);
            rosterPage = Mathf.Clamp(rosterPage, 0, pages - 1);
            GameUI.Text(parent, "Public roster title", "Les habitants · " + (rosterPage + 1) + " / " + pages, 20, 36);
            for (var n = rosterPage * pageSize; n < Math.Min(players.Length, (rosterPage + 1) * pageSize); n += 2)
            {
                var row = GameUI.Row(parent, "Stable public player row", 61, 8);
                for (var j = n; j < Math.Min(players.Length, n + 2); j++)
                {
                    var p = players[j];
                    var plate = UITheme.Surface("Public player " + p.id, row, UITheme.Opacity(UITheme.Parchment, p.alive ? .07f : .025f), UITheme.Panel);
                    plate.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 61;
                    var portrait = UITheme.Surface("Public identity portrait", plate.transform, p.alive ? Color.white : UITheme.Opacity(Color.white, .42f), UITheme.Portrait(p.avatarIndex));
                    UITheme.Position(portrait.rectTransform, new Vector2(0, .5f), 23, 0, 37, 44); portrait.preserveAspect = true;
                    var label = UITheme.Copy("Public identity name", plate.transform, p.displayName + (p.id == view.selfPlayerId ? " (toi)" : ""), 14, p.alive ? UITheme.Parchment : UITheme.Quiet, false, TextAlignmentOptions.MidlineLeft);
                    label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                    label.rectTransform.offsetMin = new Vector2(47, 22); label.rectTransform.offsetMax = new Vector2(-5, -3);
                    var state = gathering ? (!p.connected ? "hors connexion" : p.ready ? "prêt" : "en attente") : !string.IsNullOrEmpty(p.revealedRoleId) ? RoleCatalog.Find(p.revealedRoleId)?.DisplayName ?? p.revealedRoleId : p.alive ? "vivant" : "éliminé";
                    var status = UITheme.Copy("Public identity state", plate.transform, state, 11, UITheme.Brass, false, TextAlignmentOptions.MidlineLeft);
                    status.rectTransform.anchorMin = Vector2.zero; status.rectTransform.anchorMax = Vector2.one;
                    status.rectTransform.offsetMin = new Vector2(47, 2); status.rectTransform.offsetMax = new Vector2(-4, -31);
                }
            }
            if (pages > 1)
            {
                var nav = GameUI.Row(parent, "Roster pagination", 40);
                var previous = GameUI.Button(nav, "Previous public players", "Précédents", () => { rosterPage--; RefreshCurrentBody(); }, false, 40); previous.interactable = rosterPage > 0;
                var next = GameUI.Button(nav, "Next public players", "Suivants", () => { rosterPage++; RefreshCurrentBody(); }, false, 40); next.interactable = rosterPage < pages - 1;
            }
        }

        public void OpenPrivateInformation()
        {
            if (Client?.View == null || !Client.IsConnected || externalBlock || IsMenuOpen) return;
            if (IsChatOpen) CloseChatPanel();
            privateOpened = true; SecretCard.MaskImmediately(); RenderCardLayout(Client.View); RenderBody(Client.View); ConfigureDock(Client.View);
        }
        public void ClosePrivateInformation()
        { privateOpened = false; ErasePrivate(); if (Client?.View != null) { RenderCardLayout(Client.View); RenderBody(Client.View); ConfigureDock(Client.View); } }
        private void RenderPrivate()
        {
            var view = Client?.View;
            if (view == null || !privateOpened || !Client.IsConnected || externalBlock) return;
            privateContent.gameObject.SetActive(true); publicContent.gameObject.SetActive(false);
            var row = GameUI.Row(privateContent, "Private pane heading", 41);
            GameUI.Text(row, "Private pane indicator", view.selfRoleName ?? "Informations privées", 18, 40, true, UITheme.Brass);
            var close = GameUI.Button(row, "Close private action", "Masquer", ClosePrivateInformation, false, 38);
            close.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 97;

            var facts = new StringBuilder();
            if (view.wolfAllyIds?.Length > 0)
            {
                var allies = new List<string>(); foreach (var id in view.wolfAllyIds) allies.Add(Name(view, id));
                GameUI.Text(privateContent, "Private wolf allies", "Ta meute : " + string.Join(" · ", allies), 13, 32, false, UITheme.Brass);
            }
            foreach (var fact in view.privateFacts ?? Array.Empty<PrivateFact>())
                if (fact != null) facts.AppendLine(!string.IsNullOrEmpty(fact.description) ? fact.description : Name(view, fact.playerId) + " : " + RoleCatalog.Find(fact.roleId)?.DisplayName);
            if (view.action != null && view.action.kind != "vote") RenderAction(privateContent, view.action, true);
            else GameUI.Text(privateContent, "Private waiting message", "Aucune action de rôle ne t’est demandée maintenant.", 16, 42);
            if (view.action?.kind == "wolves" && !string.IsNullOrEmpty(view.action.privateInfo))
                GameUI.Text(privateContent, "Private current action information", view.action.privateInfo, 13, 32, false, UITheme.Brass);
            if (!string.IsNullOrEmpty(view.action?.description))
                GameUI.Text(privateContent, "Available action instructions", view.action.description, 14, 42, false);
            var role = RoleCatalog.Find(view.selfRoleId);
            if (role != null) GameUI.Text(privateContent, "Private role reminder", role.Summary, 14, 42, false, UITheme.Quiet);
            if (facts.Length > 0) GameUI.Text(privateContent, "Private authorized facts", facts.ToString(), 14, 40, false, UITheme.Brass);
        }
        private void ErasePrivate()
        {
            if (privateContent == null) return;
            foreach (var label in privateContent.GetComponentsInChildren<TMP_Text>(true)) label.text = "";
            GameUI.Clear(privateContent); privateContent.gameObject.SetActive(false);
        }
        private void RenderAction(Transform parent, ActionView action, bool isPrivate)
        {
            if (action == null || string.IsNullOrEmpty(action.kind)) return;
            if (isPrivate && !privateOpened) return;
            actionTurn = Client.View.turn;
            if (action.kind == "vote") { RenderCompactVote(parent, action); return; }
            // The role heading already identifies the power; keep the decisions above the fold.
            if (!isPrivate) GameUI.Text(parent, "Available action title", action.title ?? "À toi de choisir", 21, 34);
            if (action.kind != "wolves" && !string.IsNullOrEmpty(action.privateInfo))
                GameUI.Text(parent, "Private current action information", action.privateInfo, 13, 32, false, UITheme.Brass);
            if (action.kind == "witch") GameUI.Text(parent, "Available potion reserves", "Soin : " + (action.healingAvailable ? "disponible" : "utilisé") + " · Poison : " + (action.poisonAvailable ? "disponible" : "utilisé"), 13, 28, false, UITheme.Brass);
            var mutableWolf = action.kind == "wolves";
            if (action.submitted && !mutableWolf)
            {
                var receipt = "Ton choix est enregistré.";
                if (action.kind == "vote") receipt = Client.View.selfVoteAbstained ? "Ton abstention est enregistrée." : "Ton vote pour " + Name(Client.View, Client.View.selfVoteTargetId) + " est enregistré.";
                GameUI.Text(parent, "Submitted action state", receipt, 18, 68, true, UITheme.Brass); return;
            }
            var options = action.choices ?? Array.Empty<string>();
            if (options.Length == 0) return;
            if (choice == null || Array.IndexOf(options, choice) < 0)
                choice = !string.IsNullOrEmpty(action.submittedChoice) && Array.IndexOf(options, action.submittedChoice) >= 0
                    ? action.submittedChoice : action.kind == "witch" ? null : options[0];

            var optionColumns = action.kind == "witch" ? 4 : 2;
            for (var i = 0; i < options.Length; i += optionColumns)
            {
                var row = GameUI.Row(parent, "Action options row", 44);
                for (var j = i; j < Math.Min(options.Length, i + optionColumns); j++)
                {
                    var option = options[j];
                    var button = GameUI.Button(row, "Action choice " + option, (choice == option ? "• " : "") + (action.kind == "witch" && option == "both" ? "Les deux" : ChoiceName(option)), () => ChooseActionChoice(option), choice == option, 44);
                    button.GetComponentInChildren<TMP_Text>().fontSize = 16; button.interactable = !Client.IsBusy;
                }
            }
            if (choice == null)
            {
                GameUI.Text(parent, "Explicit potion choice required", "Choisis une option avant de confirmer.", 14, 32, false, UITheme.Brass);
                return;
            }
            var required = NeededTargets(action, choice);
            if (required <= 0) return;
            GameUI.Text(parent, "Required target count", targets.Count + " / " + required + (required == 1 ? " personne choisie" : " personnes choisies"), 14, 26, false, UITheme.Brass);
            var ids = action.allowedTargetIds ?? Array.Empty<string>(); const int pageSize = 4;
            var pages = Math.Max(1, (ids.Length + pageSize - 1) / pageSize); targetPage = Mathf.Clamp(targetPage, 0, pages - 1);
            var pageEnd = Math.Min(ids.Length, (targetPage + 1) * pageSize);
            for (var i = targetPage * pageSize; i < pageEnd; i += 2)
            {
                var row = GameUI.Row(parent, "Target selection row", 44);
                for (var j = i; j < Math.Min(pageEnd, i + 2); j++)
                {
                    var id = ids[j];
                    var button = GameUI.Button(row, "Action target " + id, (targets.Contains(id) ? "• " : "") + Name(Client.View, id), () => ToggleTarget(id), targets.Contains(id), 44);
                    button.GetComponentInChildren<TMP_Text>().fontSize = 17; button.interactable = !Client.IsBusy;
                }
            }
            if (pages > 1)
            {
                var nav = GameUI.Row(parent, "Target pagination", 40);
                var back = GameUI.Button(nav, "Previous targets", "← " + (targetPage + 1) + " / " + pages, () => { targetPage--; RefreshActionForm(); }, false, 40); back.interactable = targetPage > 0;
                var next = GameUI.Button(nav, "Next targets", "Autres personnes →", () => { targetPage++; RefreshActionForm(); }, false, 40); next.interactable = targetPage < pages - 1;
            }
        }
        public void ChooseActionChoice(string value)
        {
            var action = Client?.View?.action;
            if (!CanUseAction(action) || Array.IndexOf(action.choices ?? Array.Empty<string>(), value) < 0) return;
            choice = value; targets.Clear(); targetPage = 0; RefreshActionForm();
        }
        public void ToggleTarget(string id)
        {
            var action = Client?.View?.action;
            if (!CanUseAction(action) || Array.IndexOf(action.allowedTargetIds ?? Array.Empty<string>(), id) < 0) return;
            var count = NeededTargets(action, choice); if (count == 0) return;
            if (targets.Contains(id)) targets.Remove(id); else { if (targets.Count >= count) targets.RemoveAt(0); targets.Add(id); }
            RefreshActionForm();
        }
        public void SubmitAction()
        {
            var action = Client?.View?.action;
            if (!CanUseAction(action) || !HasChosenAction(action) || targets.Count != NeededTargets(action, choice)) return;
            Client.Send(new RoomCommand { kind = action.kind == "vote" ? "vote" : "act", choice = choice, targetIds = targets.ToArray(), turn = actionTurn, commandId = Guid.NewGuid().ToString("N") });
        }
        private bool CanUseAction(ActionView action) => action != null && Client.IsConnected && !Client.IsBusy && !externalBlock && !IsMenuOpen && (!IsChatOpen || action.kind == "vote" && ChatKeyboardInset <= 0)
            && (!action.submitted || action.kind == "wolves") && (action.kind == "vote" || privateOpened);
        private bool HasChosenAction(ActionView action) => action != null && !string.IsNullOrEmpty(choice)
            && Array.IndexOf(action.choices ?? Array.Empty<string>(), choice) >= 0;
        private static int NeededTargets(ActionView action, string selected) => selected == "pass" || selected == "save" ? 0 : Math.Max(0, action.targetCount);
        private void RefreshActionForm() { if (Client.View == null) return; RenderBody(Client.View); ConfigureDock(Client.View); }
        private void RefreshCurrentBody() { if (Client?.View == null) return; if (Client.View.phase == "lobby") RenderLobby(Client.View); else RenderBody(Client.View); }
        private void ToggleReady()
        {
            var self = Self(Client?.View);
            if (self != null) Client.Send(new RoomCommand { kind = "ready", ready = !self.ready, turn = Client.View.turn, commandId = Guid.NewGuid().ToString("N") });
        }
        private void Send(string kind)
        {
            if (Client?.View == null || Client.IsBusy || !Client.IsConnected) return;
            ForceMaskSecrets(); Client.Send(new RoomCommand { kind = kind, turn = Client.View.turn, commandId = Guid.NewGuid().ToString("N") });
        }
        private void OpenComposer()
        {
            if (Client?.View == null || !Client.View.isHost) return;
            ForceMaskSecrets(); if (composer != null) { GameUI.ClearSelection(composer.transform); Destroy(composer); }
            composer = UITheme.Box("Host configuration dialog", transform, true).gameObject;
            var panel = composer.AddComponent<RoomCompositionPanel>(); var turn = Client.View.turn;
            panel.Initialize(Client.View, Client.View.settings, settings => { creationSettings = settings; Client.Send(new RoomCommand { kind = "configure", settings = settings, turn = turn, commandId = Guid.NewGuid().ToString("N") }); },
                () => { if (composer != null) { GameUI.ClearSelection(composer.transform); composer.SetActive(false); Destroy(composer); composer = null; } });
        }

        public void OpenMenu()
        {
            if (Client?.HasSession != true) { RequestedHome?.Invoke(); return; }
            ForceMaskSecrets(); CloseChatPanel(); CloseMenu();
            menu = UITheme.Box("Safe room menu", transform, true).gameObject;
            var paper = UITheme.Surface("Room menu backdrop", menu.transform, new Color(.025f, .05f, .075f, .98f), null, true); paper.raycastTarget = true;
            var content = GameUI.Scroll(menu.transform, "Safe room menu content", 103, 175);
            GameUI.Text(content, "Room menu title", "Ta soirée", 34, 65);
            GameUI.Text(content, "Room menu code", "Village " + Client.RoomCode, 17, 40, false, UITheme.Brass);
            GameUI.Button(content, "Menu game instructions", "Règles et aide", OpenRoomHelp, false);
            GameUI.Button(content, "Read public chronicle", "Historique de la partie", OpenPublicChronicle, false);
            GameUI.Button(content, "Menu motion preference", preferences.ReducedMotion ? "Animations réduites : oui" : "Animations réduites : non", () => { preferences.SetReducedMotion(!preferences.ReducedMotion); OpenMenu(); }, false);
            GameUI.Button(content, "Menu sound preference", preferences.AmbientSound ? "Ambiance sonore : oui" : "Ambiance sonore : non", () => { preferences.SetAmbientSound(!preferences.AmbientSound); OpenMenu(); }, false);
            GameUI.Button(content, "Menu public narration preference", preferences.PublicNarration ? "Appels publics : activés" : "Appels publics : coupés", () => { preferences.SetPublicNarration(!preferences.PublicNarration); OpenMenu(); }, false);
            if (Client.View?.mode == "in-person") GameUI.Text(content, "Public narration responsibility", "Seul le téléphone de l’hôte lit les appels publics. Sans narration audible, prévoyez une personne qui les lit. Ce réglage n’active aucun microphone.", 14, 100, false, UITheme.Quiet);
            GameUI.Text(content, "Menu departure separator", "Quitter demande une confirmation.", 13, 60, false, UITheme.Quiet);
            GameUI.Button(content, "Request departure", IsResumingAccess ? "Abandonner cet accès sauvegardé…" : Client.IsTraining ? "Quitter l’entraînement…" : "Quitter la partie…", ShowQuitConfirmation, false, 44);
            MenuDock("Revenir à la partie", CloseMenu);
            SecretCard.SetBlocked(true);
            if (Client.View != null) ConfigureDock(Client.View);
            else { PrimaryAction.interactable = false; SecondaryAction.interactable = false; }
        }
        public void ShowQuitConfirmation()
        {
            if (menu == null) OpenMenu();
            if (menu == null) return;
            GameUI.Clear(menu.transform); quitConfirmation = true;
            var shade = UITheme.Surface("Departure confirmation backdrop", menu.transform, new Color(.025f, .05f, .075f, .99f), null, true); shade.raycastTarget = true;
            var panel = GameUI.Scroll(menu.transform, "Departure confirmation content", 170, 220);
            GameUI.Text(panel, "Departure confirmation title", IsResumingAccess ? "Abandonner cet accès ?" : "Quitter cette soirée ?", 31, 83);
            GameUI.Text(panel, "Departure consequence", IsResumingAccess ? "Cet accès sauvegardé sera supprimé sur cet appareil. Tu ne pourras plus récupérer cette place avec un simple pseudo. Essaie d’abord la reconnexion si ta partie existe encore." : Client.View?.phase == "lobby" ? "Tu quitteras le salon. Les autres personnes pourront continuer à se préparer."
                : "Ce départ supprime ton accès sur cet appareil. Ton rôle ne sera pas redistribué pendant la manche et les délais continueront. Pour revenir plus tard, ferme simplement l’application au lieu de confirmer ce départ.", 18, 174, false);
            var stay = UITheme.Action("Stay in current game", menu.transform, "Rester dans la partie", CloseMenu);
            UITheme.Position((RectTransform)stay.transform, UITheme.Bottom, 0, 124, 382, 56);
            var leave = UITheme.Action("Confirm room departure", menu.transform, IsResumingAccess ? "Supprimer cet accès" : "Confirmer mon départ", LeaveRoom, false);
            UITheme.Position((RectTransform)leave.transform, UITheme.Bottom, 0, 62, 382, 48);
        }
        private void LeaveRoom()
        {
            if (Client == null || Client.IsBusy) return;
            ForceMaskSecrets(); CloseMenu();
            if (IsResumingAccess) { Client.ForgetRoom(); return; }
            if (Client.IsConnected && Client.View != null) Client.Send(new RoomCommand { kind = "leave", turn = Client.View.turn, commandId = Guid.NewGuid().ToString("N") });
            else error.text = "Connexion nécessaire pour confirmer ton départ. Tu peux fermer l’application puis reprendre ta place.";
        }
        public void CloseMenu()
        {
            if (menu != null) { GameUI.ClearSelection(menu.transform); menu.SetActive(false); Destroy(menu); menu = null; }
            quitConfirmation = false; SecretCard?.SetBlocked(externalBlock || IsChatOpen);
            if (built && Client?.View != null) ConfigureDock(Client.View);
            else if (built && Client != null && !rendering) Render();
        }
        private void MenuDock(string label, Action callback)
        {
            var button = UITheme.Action("Close safe room menu", menu.transform, label, () => callback?.Invoke());
            UITheme.Position((RectTransform)button.transform, UITheme.Bottom, 0, 124, 382, 56);
        }
        public void OpenRoomHelp()
        {
            ForceMaskSecrets(); CloseChatPanel(); CloseMenu();
            menu = UITheme.Box("Room help", transform, true).gameObject;
            var shade = UITheme.Surface("Room help backdrop", menu.transform, new Color(.025f, .05f, .075f, .99f), null, true); shade.raycastTarget = true;
            var content = GameUI.Scroll(menu.transform, "Room instructions", 100, 175);
            GameUI.Text(content, "Rules title", "Comment jouer", 31, 64);
            GameUI.Text(content, "Rules basic flow", "1. Découvre ton rôle et confirme.\n\n2. La nuit, seuls les rôles appelés agissent : ouvre ton action privée, choisis puis confirme. Les autres attendent.\n\n3. Au jour, échangez vos soupçons, puis votez.\n\n4. Les réactions sont résolues avant le résultat. Une partie se termine par une victoire ou une égalité.", 18, 365, false);
            GameUI.Text(content, "Rules private gestures", "Le bouton du bas révèle ta carte tant que tu le maintiens. Pour agir d’une main, ouvre le panneau privé ; masque-le ensuite. Un menu, un changement de tour ou une interruption referme les secrets.", 15, 142, false, UITheme.Brass);
            GameUI.Text(content, "Rules remote voice", "Messages écrits et vocal : uniquement pendant le vote de jour. Le micro ne s’active jamais tout seul.", 15, 117, false);
            GameUI.Text(content, "Rules presence protocol", "En présence : gardez les yeux fermés quand la narration le demande. La Petite Fille observe la table ; elle ne reçoit jamais une liste numérique de loups.", 15, 117, false);
            if (Client.View?.mode == "in-person") GameUI.Text(content, "Rules common night gestures", "Pendant la nuit, tous peuvent tapoter doucement leur téléphone, même les yeux fermés. Hors de l’appel de ton rôle, garde les yeux fermés ; la Petite Fille conserve sa consigne physique. Ce geste est facultatif et ne garantit pas de cacher les gestes des autres.", 15, 164, false);
            MenuDock("Revenir au village", CloseMenu); SecretCard.SetBlocked(true);
            if (Client?.View != null) ConfigureDock(Client.View);
        }

        private void Update()
        {
            if (!built || !routeActive || Client == null) return;
            var light = readingLight?.Invoke() ?? 0;
            title.color = subtitle.color = Color.Lerp(UITheme.Parchment, UITheme.Midnight, light);
            clock.color = Color.Lerp(UITheme.Brass, UITheme.Midnight, light);
            var keyboard = Keyboard.current;
            var eventSystem = EventSystem.current;
            var selectedObject = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            var focused = selectedObject != null ? selectedObject.GetComponent<TMP_InputField>() : null;
            if (keyboard != null)
            {
                if (!keyboard.spaceKey.isPressed) keyboardRearm = false;
                var allowed = !keyboardRearm && !externalBlock && composer == null && !IsMenuOpen && !IsChatOpen && (focused == null || !focused.isFocused);
                if (allowed && keyboard.spaceKey.wasPressedThisFrame && game.activeInHierarchy) { keyboardHold = true; SecretCard.BeginHold(); }
                if (keyboardHold && keyboard.spaceKey.wasReleasedThisFrame) { keyboardHold = false; SecretCard.MaskImmediately(); }
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    if (IsChatOpen) CloseChatPanel(); else if (IsMenuOpen) CloseMenu(); else if (privateOpened) ClosePrivateInformation(); else if (Client.HasSession) OpenMenu();
                }
            }
            var view = Client.View;
            if (Client is PracticeRoomClient guided && guided.PracticeClockPaused)
                clock.text = "Entraînement · prends ton temps";
            else if (view != null && view.deadlineUnixMs > 0)
            {
                var seconds = Math.Max(0, (int)Math.Ceiling((view.deadlineUnixMs - ServerNow(view)) / 1000d));
                var period = view.phase == "night" ? "Nuit " + view.day : view.phase == "distribution" ? "Découverte" : "Jour " + view.day;
                clock.text = period + " · " + seconds + " s";
            }
            else clock.text = "";
            var previousKeyboardInset = ChatKeyboardInset;
            UpdateChatKeyboard();
            if (ChatKeyboardInset > 0) PrimaryAction.interactable = false;
            else if (previousKeyboardInset > 0 && view != null) ConfigureDock(view);
        }
        private long serverOffsetMs;
        private long ServerNow(RoomView view) => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + serverOffsetMs;
        private void OnApplicationFocus(bool focus) { if (!focus) ForceMaskSecrets(); }
        private void OnApplicationPause(bool pause) { if (pause) ForceMaskSecrets(); }
        private void OnEnable() { if (!built || !routeActive) return; ForceMaskSecrets(); Client?.SetActive(true); Client?.Refresh(); }
        private void OnDisable() { ForceMaskSecrets(); Client?.SetActive(false); }
        private void OnDestroy() { if (Client != null) { Client.Changed -= Render; Client.SecretsMustHide -= ForceMaskSecrets; UnbindCommunications(); } }
        private static PlayerView Self(RoomView view) { foreach (var p in view?.players ?? Array.Empty<PlayerView>()) if (p.id == view.selfPlayerId) return p; return null; }
        private static string Name(RoomView view, string id) { foreach (var p in view?.players ?? Array.Empty<PlayerView>()) if (p.id == id) return p.displayName; return "Une personne du village"; }
        private static string ChoiceName(string value)
        {
            switch (value)
            {
                case "pass": return "Passer"; case "save": return "Soin"; case "poison": return "Poison"; case "both": return "Soin + poison";
                case "link": return "Lier les amoureux"; case "protect": return "Protéger"; case "inspect": return "Consulter le rôle";
                case "kill": return "Cible de la meute"; case "shoot": return "Dernier tir"; case "vote": return "Un suspect"; default: return "Choisir";
            }
        }
        private static string PhaseTitle(string phase, string winner)
        {
            if (phase == "finished") return winner == "wolves" ? "La meute l’emporte." : winner == "village" || winner == "villagers" ? "Le village l’emporte."
                : winner == "lovers" ? "Les amoureux l’emportent." : "Partie terminée";
            switch (phase)
            {
                case "lobby": return "Le village se rassemble."; case "distribution": return "Découverte des rôles"; case "night": return "Le village s’endort.";
                case "dawn": return "Le jour se lève."; case "discussion": return "Les soupçons se croisent."; case "vote": return "Le village choisit.";
                case "verdict": return "Les voix se révèlent."; case "reaction": return "Un dernier souffle…"; default: return "Créer ou rejoindre";
            }
        }
    }

    internal sealed class ModeDescription : MonoBehaviour
    {
        public Func<string> Read;
        private void Update() { var text = GetComponent<TMP_Text>(); if (text != null && Read != null) text.text = Read() == "remote" ? "À distance · chacun son téléphone" : "En présence · autour de la même table"; }
    }
}
