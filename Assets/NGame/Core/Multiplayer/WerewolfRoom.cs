using System;
using System.Collections.Generic;
using System.Linq;

namespace NGame.Core.Multiplayer
{
    /// <summary>
    /// Authoritative in-memory game state. The HTTP boundary owns authentication and locking.
    /// Every view is a detached, recipient-specific projection; no Unity API is used here.
    /// </summary>
    public sealed class WerewolfRoom
    {
        private sealed class Player
        {
            public string Id, Name;
            public int Avatar;
            public bool Ready, Alive = true, Connected = true, Departed;
        }
        private sealed class Submission
        {
            public string Choice;
            public string[] Targets;
        }
        private sealed class Receipt
        {
            public string Fingerprint;
            public RuleResult Result;
        }
        private static readonly HashSet<string> SupportedRoles = new HashSet<string>(StringComparer.Ordinal)
        { "werewolf", "villager", "seer", "witch", "hunter", "cupid", "guard", "little-girl" };
        private readonly List<Player> players = new List<Player>();
        private readonly Dictionary<string, string> roles = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Submission> submissions = new Dictionary<string, Submission>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<PrivateFact>> facts = new Dictionary<string, List<PrivateFact>>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> lovers = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Receipt> receipts = new Dictionary<string, Receipt>(StringComparer.Ordinal);
        private readonly Queue<string> receiptOrder = new Queue<string>();
        private readonly List<PublicEvent> events = new List<PublicEvent>();
        private readonly Queue<string> hunterReactions = new Queue<string>();
        private readonly HashSet<string> reactedHunters = new HashSet<string>(StringComparer.Ordinal);
        private readonly Random random;
        private readonly int randomSeed;
        private int randomDrawCount;
        private readonly HashSet<string> acknowledged = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> nightSteps = new List<string>();
        private RoomSettings settings;
        private string hostId;
        private string phase = "lobby";
        private string matchId = Guid.NewGuid().ToString("N");
        private string winner;
        private string[] winnerIds = Array.Empty<string>();
        private string lastGuardTarget;
        private string protectedTarget;
        private string wolfVictim;
        private string poisonVictim;
        private string afterReaction;
        private string afterVerdict;
        private VoteVerdictView verdict;
        private string[] runoffTargets;
        private bool healingAvailable = true;
        private bool poisonAvailable = true;
        private bool healedTonight;
        private int nightIndex;
        private int voteRound;
        private int turn;
        private int day;
        private int eventSequence;
        private long revision;
        private long phaseStarted;
        private long deadline;
        private long lastNow;

        public string Code { get; }
        public long Revision => revision;

        public WerewolfRoom(string code, RoomSettings roomSettings, int randomSeed)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length > 32) throw new ArgumentException("Code de salon invalide.", nameof(code));
            settings = CopySettings(roomSettings ?? new RoomSettings());
            var validation = ValidateSettings(settings);
            if (!validation.ok) throw new ArgumentException(validation.message, nameof(roomSettings));
            Code = code;
            this.randomSeed = randomSeed;
            random = new Random(randomSeed);
        }

        public RuleResult AddPlayer(string id, string displayName, int avatarIndex)
        {
            if (phase != "lobby") return Fail("JOIN_CLOSED", "La partie a déjà commencé.");
            if (players.Count >= 24) return Fail("ROOM_FULL", "Le salon accueille au maximum 24 personnes.");
            if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || id.Any(char.IsControl)) return Fail("INVALID_PLAYER", "Identifiant invalide.");
            var name = (displayName ?? string.Empty).Trim();
            if (name.Length < 1 || name.Length > 24 || name.Any(char.IsControl)) return Fail("INVALID_NAME", "Choisis un pseudo de 1 à 24 caractères.");
            if (avatarIndex < 0 || avatarIndex > 5) return Fail("INVALID_AVATAR", "Portrait invalide.");
            if (players.Any(p => p.Id == id)) return Fail("PLAYER_EXISTS", "Cette personne est déjà dans le salon.");
            if (players.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))) return Fail("NAME_TAKEN", "Ce pseudo est déjà utilisé.");
            players.Add(new Player { Id = id, Name = name, Avatar = avatarIndex });
            facts[id] = new List<PrivateFact>();
            if (hostId == null) hostId = id;
            revision++;
            return RuleResult.Success();
        }

        public RoomView StateFor(string playerId, long nowUnixMs)
        {
            var self = FindPlayer(playerId);
            if (self == null) throw new ArgumentException("Cette personne n’appartient pas au salon.", nameof(playerId));
            Tick(nowUnixMs);
            roles.TryGetValue(playerId, out var ownRole);
            var view = new RoomView
            {
                code = Code, mode = settings.mode, ruleset = settings.ruleset, settings = CopySettings(settings), matchId = matchId,
                revision = revision, turn = turn, day = day, phase = phase,
                phaseTitle = PhaseTitle(), serverTimeUnixMs = lastNow, phaseStartedUnixMs = phaseStarted,
                nightStep = phase == "night" ? nightSteps[nightIndex] : null,
                deadlineUnixMs = deadline, hostPlayerId = hostId, selfPlayerId = playerId,
                roleAcknowledged = acknowledged.Contains(playerId), acknowledgedCount = acknowledged.Count,
                voteRound = voteRound, votesReceived = phase == "vote" ? submissions.Count : 0,
                votesExpected = phase == "vote" ? players.Count(p => p.Alive) : 0,
                voteTargetIds = phase == "vote" ? VoteTargets(self) : Array.Empty<string>(),
                selfVoteTargetId = phase == "vote" && submissions.TryGetValue(playerId, out var ownVote) && ownVote.Choice == "vote" ? ownVote.Targets[0] : null,
                selfVoteAbstained = phase == "vote" && submissions.TryGetValue(playerId, out var ownPass) && ownPass.Choice == "pass",
                verdict = CopyVerdict(verdict),
                selfRoleId = ownRole, selfRoleName = ownRole == null ? null : RoleCatalog.Find(ownRole)?.DisplayName,
                isHost = playerId == hostId, canStart = playerId == hostId && CanStart(),
                startBlockedReason = ValidateStart().message,
                canAdvance = playerId == hostId && CanAdvance(lastNow),
                statusMessage = Status(), winner = winner,
                winnerIds = phase == "finished" ? (string[])winnerIds.Clone() : Array.Empty<string>(),
                players = players.Select(p => new PlayerView
                {
                    id = p.Id, displayName = p.Name, avatarIndex = p.Avatar, ready = p.Ready,
                    alive = p.Alive, connected = p.Connected,
                    revealedRoleId = phase == "finished" || (!p.Alive && settings.revealEliminatedRoles)
                        ? RoleOf(p.Id) : null
                }).ToArray(),
                composition = Composition().Select(CopyCount).ToArray(),
                privateFacts = facts[playerId].Select(CopyFact).ToArray(),
                events = events.Select(e => new PublicEvent
                { sequence = e.sequence, kind = e.kind, text = e.text, playerId = e.playerId }).ToArray(),
                action = ActionFor(self)
            };
            if (ownRole == "werewolf")
                view.wolfAllyIds = players.Where(p => p.Id != playerId && RoleOf(p.Id) == "werewolf").Select(p => p.Id).ToArray();
            return view;
        }

        public RuleResult Apply(string actorId, RoomCommand command, long nowUnixMs)
        {
            Tick(nowUnixMs);
            var actor = FindPlayer(actorId);
            if (actor == null) return Fail("NOT_MEMBER", "Tu n’appartiens pas à ce salon.");
            if (command == null || string.IsNullOrWhiteSpace(command.commandId) || command.commandId.Length > 128)
                return Fail("INVALID_COMMAND", "Un identifiant de commande est requis.");
            var key = actorId + "\n" + command.commandId;
            var fingerprint = Fingerprint(command);
            if (receipts.TryGetValue(key, out var receipt))
                return receipt.Fingerprint == fingerprint ? CopyResult(receipt.Result)
                    : Fail("COMMAND_CONFLICT", "Cet identifiant a déjà été utilisé pour une autre action.");

            RuleResult result;
            if (command.turn != turn) result = Fail("STALE_TURN", "La partie a avancé. Actualise ton écran.");
            else
            {
                switch (command.kind)
                {
                    case "ready": result = SetReady(actor, command); break;
                    case "configure": result = Configure(actor, command); break;
                    case "start": result = Start(actor, lastNow); break;
                    case "acknowledge-role": result = AcknowledgeRole(actor); break;
                    case "leave": result = Leave(actor); break;
                    case "remove-player": result = RemovePlayer(actor, command); break;
                    case "act": result = SubmitAction(actor, command); break;
                    case "vote": result = SubmitVote(actor, command); break;
                    case "advance":
                        result = actor.Id != hostId ? Fail("HOST_ONLY", "Seul l’hôte peut avancer cette étape.")
                            : !CanAdvance(lastNow) ? Fail("STEP_PENDING", "Des actions ou le délai restent en cours.")
                            : Advance(lastNow);
                        break;
                    case "reset": result = Reset(actor, lastNow); break;
                    default: result = Fail("INVALID_COMMAND", "Commande inconnue."); break;
                }
            }
            receipts[key] = new Receipt { Fingerprint = fingerprint, Result = CopyResult(result) };
            receiptOrder.Enqueue(key);
            if (receiptOrder.Count > 2048) receipts.Remove(receiptOrder.Dequeue());
            Tick(lastNow);
            return result;
        }

        public void Tick(long nowUnixMs)
        {
            lastNow = Math.Max(lastNow, Math.Max(0, nowUnixMs));
            // New deadlines begin at the current server time: a long outage cannot silently play
            // through a whole game with fabricated votes or targets.
            for (var transitions = 0; transitions < 32 && ShouldAutoAdvance(lastNow); transitions++) Advance(lastNow);
        }

        private RuleResult SetReady(Player actor, RoomCommand command)
        {
            if (phase != "lobby") return Fail("WRONG_PHASE", "La préparation est terminée.");
            if (actor.Ready != command.ready) { actor.Ready = command.ready; revision++; }
            return RuleResult.Success();
        }

        private RuleResult Configure(Player actor, RoomCommand command)
        {
            if (actor.Id != hostId) return Fail("HOST_ONLY", "Seul l’hôte règle le salon.");
            if (phase != "lobby") return Fail("WRONG_PHASE", "Les règles sont figées pendant la partie.");
            if (command.settings == null) return Fail("INVALID_SETTINGS", "Réglages manquants.");
            var proposed = CopySettings(command.settings);
            var check = ValidateSettings(proposed);
            if (!check.ok) return check;
            settings = proposed;
            foreach (var player in players) player.Ready = false;
            revision++;
            return RuleResult.Success("Règles mises à jour. Chacun doit confirmer à nouveau.");
        }

        private bool CanStart() => ValidateStart().ok;

        private RuleResult ValidateStart()
        {
            if (phase != "lobby") return Fail("WRONG_PHASE", "La partie a déjà commencé.");
            if (players.Count < 6) return Fail("NOT_ENOUGH_PLAYERS", "Il faut au moins six joueurs ; six et sept sont des variantes N’GAME.");
            if (players.Count > 24) return Fail("ROOM_FULL", "Le salon accueille au maximum 24 personnes.");
            var settingsCheck = ValidateSettings(settings);
            if (!settingsCheck.ok) return settingsCheck;
            var deckCheck = ValidateDeck(Composition());
            if (!deckCheck.ok) return deckCheck;
            if (players.Any(p => !p.Connected)) return Fail("PLAYER_OFFLINE", "Attends le retour des joueurs déconnectés ou retire leurs places avant de lancer.");
            if (players.Any(p => !p.Ready)) return Fail("NOT_READY", "Chaque joueur doit se déclarer prêt.");
            return RuleResult.Success();
        }

        private RuleResult Start(Player actor, long now)
        {
            if (actor.Id != hostId) return Fail("HOST_ONLY", "Seul l’hôte lance la partie.");
            var check = ValidateStart();
            if (!check.ok) return check;
            var deckCounts = Composition();
            var deck = new List<string>();
            foreach (var count in deckCounts) for (var i = 0; i < count.count; i++) deck.Add(count.roleId);
            for (var i = deck.Count - 1; i > 0; i--)
            {
                var swap = random.Next(i + 1);
                randomDrawCount++;
                var value = deck[i]; deck[i] = deck[swap]; deck[swap] = value;
            }
            roles.Clear();
            acknowledged.Clear(); verdict = null; afterVerdict = null; voteRound = 0;
            matchId = Guid.NewGuid().ToString("N");
            for (var i = 0; i < players.Count; i++) roles[players[i].Id] = deck[i];
            Event("start", "Les rôles sont distribués. Chacun découvre sa carte et confirme qu’il est prêt.");
            ChangePhase("distribution", now, 120000);
            return RuleResult.Success();
        }

        private RuleResult AcknowledgeRole(Player actor)
        {
            if (phase != "distribution") return Fail("WRONG_PHASE", "La découverte des rôles est terminée.");
            if (acknowledged.Add(actor.Id)) revision++;
            return RuleResult.Success("Ta carte est comprise. Attends les autres joueurs.");
        }

        public bool ContainsPlayer(string playerId) => FindPlayer(playerId) != null;

        public void SetConnected(string playerId, bool connected)
        {
            var player = FindPlayer(playerId);
            if (player == null || (player.Connected == connected && (!connected || !player.Departed))) return;
            player.Connected = connected;
            if (connected) player.Departed = false;
            if (hostId == null || FindPlayer(hostId)?.Connected != true)
                hostId = players.FirstOrDefault(p => p.Connected)?.Id ?? hostId;
            revision++;
        }

        private RuleResult RemovePlayer(Player actor, RoomCommand command)
        {
            if (actor.Id != hostId) return Fail("HOST_ONLY", "Seul l’hôte peut retirer une place du salon.");
            if (phase != "lobby") return Fail("WRONG_PHASE", "Les places ne peuvent être retirées qu’avant la distribution des cartes.");
            if (command.targetIds == null || command.targetIds.Length != 1 || command.targetIds[0] == actor.Id)
                return Fail("INVALID_TARGET", "Choisis une autre personne du salon. Pour partir toi-même, utilise le menu de sortie.");
            var target = FindPlayer(command.targetIds[0]);
            if (target == null) return Fail("INVALID_TARGET", "Cette personne n’appartient plus au salon.");
            RemoveLobbyPlayer(target);
            Event("player-removed", actor.Name + " retire la place de " + target.Name + " du salon. Chacun doit confirmer à nouveau.");
            return RuleResult.Success("Place retirée. Vérifie le nombre de cartes et les confirmations avant de lancer.");
        }

        private void RemoveLobbyPlayer(Player player)
        {
            players.Remove(player); facts.Remove(player.Id); acknowledged.Remove(player.Id);
            foreach (var remaining in players) remaining.Ready = false;
            var prefix = player.Id + "\n";
            foreach (var key in receipts.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()) receipts.Remove(key);
            var retainedReceipts = receiptOrder.Where(key => !key.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            receiptOrder.Clear();
            foreach (var key in retainedReceipts) receiptOrder.Enqueue(key);
            if (hostId == player.Id) hostId = players.FirstOrDefault(p => p.Connected)?.Id ?? players.FirstOrDefault()?.Id;
            revision++;
        }

        private RuleResult Leave(Player actor)
        {
            if (phase == "lobby" || phase == "finished")
            {
                // A completed game keeps its final roster until the next lobby; do not erase its outcome.
                if (phase == "lobby")
                {
                    RemoveLobbyPlayer(actor);
                    Event("departure", actor.Name + " quitte le salon.");
                    return RuleResult.Success("Tu as quitté le salon.");
                }
            }
            actor.Departed = true;
            SetConnected(actor.Id, false);
            Event("departure", actor.Name + " quitte la partie. Sa place reste réservée pour cette manche.");
            return RuleResult.Success("Ta place reste réservée pour cette manche.");
        }

        private RuleResult Reset(Player actor, long now)
        {
            if (actor.Id != hostId) return Fail("HOST_ONLY", "Seul l’hôte prépare la revanche.");
            if (phase != "finished" && phase != "lobby") return Fail("GAME_RUNNING", "La partie doit être terminée avant une revanche.");
            roles.Clear(); lovers.Clear(); hunterReactions.Clear(); reactedHunters.Clear();
            foreach (var departed in players.Where(p => p.Departed).ToArray())
            { players.Remove(departed); facts.Remove(departed.Id); }
            if (FindPlayer(hostId) == null) hostId = players.FirstOrDefault(p => p.Connected)?.Id ?? players.FirstOrDefault()?.Id;
            acknowledged.Clear(); verdict = null; afterVerdict = null; voteRound = 0;
            matchId = Guid.NewGuid().ToString("N");
            foreach (var list in facts.Values) list.Clear();
            foreach (var player in players) { player.Alive = true; player.Ready = false; }
            events.Clear(); winner = null; winnerIds = Array.Empty<string>();
            lastGuardTarget = protectedTarget = wolfVictim = poisonVictim = null;
            healingAvailable = poisonAvailable = true;
            healedTonight = false; day = 0; nightSteps.Clear(); runoffTargets = null;
            ChangePhase("lobby", now, 0);
            Event("reset", "Une nouvelle partie peut être préparée.");
            return RuleResult.Success();
        }

        private RuleResult SubmitAction(Player actor, RoomCommand command)
        {
            var action = ActionFor(actor);
            if ((phase != "night" && phase != "reaction") || action == null || action.kind == "observe")
                return Fail("NO_ACTION", "Aucune action ne t’est proposée maintenant.");
            if (action.submitted && action.kind != "wolves") return Fail("ALREADY_SUBMITTED", "Ton choix est déjà confirmé pour cette étape.");
            var choice = command.choice ?? string.Empty;
            if (!action.choices.Contains(choice)) return Fail("INVALID_CHOICE", "Ce choix n’est pas autorisé.");
            var targets = command.targetIds ?? Array.Empty<string>();
            var needed = choice == "pass" || choice == "save" ? 0 : action.targetCount;
            if (targets.Length != needed || targets.Distinct(StringComparer.Ordinal).Count() != targets.Length
                || targets.Any(t => t == null || !action.allowedTargetIds.Contains(t)))
                return Fail("INVALID_TARGET", "Sélectionne les cibles autorisées, sans doublon.");
            submissions[actor.Id] = new Submission { Choice = choice, Targets = (string[])targets.Clone() };
            revision++;
            return RuleResult.Success();
        }

        private RuleResult SubmitVote(Player actor, RoomCommand command)
        {
            if (phase != "vote" || !actor.Alive) return Fail("NO_VOTE", "Tu ne peux pas voter maintenant.");
            if (submissions.ContainsKey(actor.Id)) return Fail("ALREADY_SUBMITTED", "Ton vote est déjà confirmé.");
            var choice = command.choice ?? "vote";
            var targets = command.targetIds ?? Array.Empty<string>();
            if (choice == "pass")
            {
                if (targets.Length != 0) return Fail("INVALID_TARGET", "Une abstention n’a pas de cible.");
            }
            else if (choice != "vote" || targets.Length != 1 || !VoteTargets(actor).Contains(targets[0]))
                return Fail("INVALID_TARGET", "Cette personne ne peut pas recevoir ton vote.");
            submissions[actor.Id] = new Submission { Choice = choice, Targets = (string[])targets.Clone() };
            revision++;
            return RuleResult.Success();
        }

        private ActionView ActionFor(Player actor)
        {
            if (phase == "vote" && actor.Alive)
                return MakeAction("vote", "Le vote du village", "Choisis une personne ou abstiens-toi. Les choix seront révélés à la clôture.",
                    1, VoteTargets(actor), new[] { "vote", "pass" }, actor.Id);
            if (phase == "reaction" && hunterReactions.Count > 0 && hunterReactions.Peek() == actor.Id)
                return MakeAction("hunter", "Ton dernier tir", "Choisis une dernière cible, ou passe. Une absence à l’échéance vaut un passage.",
                    1, LivingExcept(actor.Id), new[] { "shoot", "pass" }, actor.Id);
            if (phase != "night" || !actor.Alive || nightIndex >= nightSteps.Count) return null;
            var step = nightSteps[nightIndex];
            var role = RoleOf(actor.Id);
            if (step == "lovers")
            {
                if (!lovers.TryGetValue(actor.Id, out var partner)) return null;
                return new ActionView
                {
                    kind = "observe", title = "Votre lien secret",
                    description = "Découvrez discrètement votre partenaire, puis refermez les yeux. Sa carte reste secrète.",
                    privateInfo = "Ton destin est lié à " + NameOf(partner) + ".", submitted = true
                };
            }
            if (step == "werewolf" && role == "little-girl" && settings.mode == "in-person")
                return new ActionView { kind = "observe", title = "Un regard dans la nuit",
                    description = "Seulement pendant le réveil des loups : observe discrètement la table. Ils peuvent te remarquer et changer de cible.", submitted = true };
            if (role != step) return null;
            if (step == "witch" && !healingAvailable && !poisonAvailable) return null;
            switch (step)
            {
                case "cupid": return MakeAction("cupid", "Les liens du cœur", "Lie deux personnes. Tu peux faire partie du couple.",
                    2, players.Where(p => p.Alive).Select(p => p.Id).ToArray(), new[] { "link", "pass" }, actor.Id);
                case "guard":
                    var guardAction = MakeAction("guard", "Veiller sur le village", "Protège une personne. Tu peux te protéger, mais pas deux nuits consécutives sur la même cible. La Petite Fille reste vulnérable aux loups.",
                        1, players.Where(p => p.Alive && p.Id != lastGuardTarget).Select(p => p.Id).ToArray(), new[] { "protect", "pass" }, actor.Id);
                    guardAction.privateInfo = lastGuardTarget == null ? "Tu n’as protégé personne la nuit précédente."
                        : "La nuit précédente, tu protégeais " + NameOf(lastGuardTarget) + ". Tu dois choisir une autre personne cette nuit.";
                    return guardAction;
                case "seer": return MakeAction("seer", "Une carte dans la nuit", "Découvre le rôle exact d’une autre personne vivante.",
                    1, LivingExcept(actor.Id), new[] { "inspect", "pass" }, actor.Id);
                case "werewolf":
                    var wolfAction = MakeAction("wolves", "Le choix de la meute", "La cible unique la plus désignée sera attaquée. Une égalité ne donne aucune victime.",
                        1, players.Where(p => p.Alive && RoleOf(p.Id) != "werewolf").Select(p => p.Id).ToArray(), new[] { "kill", "pass" }, actor.Id);
                    wolfAction.privateInfo = string.Join("\n", players.Where(p => p.Id != actor.Id && RoleOf(p.Id) == "werewolf" && submissions.ContainsKey(p.Id))
                        .Select(p => p.Name + (submissions[p.Id].Choice == "pass" ? " passe." : " choisit " + NameOf(submissions[p.Id].Targets[0]) + ".")));
                    return wolfAction;
                case "witch":
                    var choices = new List<string> { "pass" };
                    if (healingAvailable && wolfVictim != null) choices.Add("save");
                    if (poisonAvailable) choices.Add("poison");
                    if (healingAvailable && poisonAvailable && wolfVictim != null) choices.Add("both");
                    var witchAction = MakeAction("witch", "Utiliser les potions", "Soin et poison sont chacun utilisables une fois. Avec les deux, sélectionne uniquement la cible du poison.",
                        1, LivingExcept(actor.Id), choices.ToArray(), actor.Id);
                    witchAction.healingAvailable = healingAvailable;
                    witchAction.poisonAvailable = poisonAvailable;
                    witchAction.wolfVictimId = wolfVictim;
                    witchAction.privateInfo = wolfVictim == null ? "Aucune victime de la meute à soigner cette nuit."
                        : "La victime de la meute est " + NameOf(wolfVictim) + ".";
                    return witchAction;
                default: return null;
            }
        }

        private ActionView MakeAction(string kind, string title, string description, int targetCount,
            string[] allowed, string[] choices, string actorId)
        {
            submissions.TryGetValue(actorId, out var submission);
            return new ActionView { kind = kind, title = title, description = description, targetCount = targetCount,
                allowedTargetIds = allowed, choices = choices, submitted = submission != null,
                submittedChoice = submission?.Choice,
                submittedTargetIds = submission == null ? Array.Empty<string>() : (string[])submission.Targets.Clone() };
        }

        private string[] LivingExcept(string id) => players.Where(p => p.Alive && p.Id != id).Select(p => p.Id).ToArray();
        private string[] VoteTargets(Player actor) => players.Where(p => p.Alive && p.Id != actor.Id
            && (runoffTargets == null || runoffTargets.Contains(p.Id))).Select(p => p.Id).ToArray();

        private bool AllActionsSubmitted()
        {
            if (phase == "vote") return players.Where(p => p.Alive).All(p => submissions.ContainsKey(p.Id));
            if (phase == "reaction") return hunterReactions.Count == 0 || submissions.ContainsKey(hunterReactions.Peek());
            if (phase == "night") return players.Where(p => p.Alive && ActionFor(p) != null && ActionFor(p).kind != "observe")
                .All(p => submissions.ContainsKey(p.Id));
            return false;
        }

        private bool CanAdvance(long now)
        {
            if (phase == "discussion") return true;
            if (phase == "dawn" || phase == "verdict") return now - phaseStarted >= 3000;
            if (phase == "night") return settings.nightProtocol == "screen"
                ? now >= deadline : now - phaseStarted >= 3000 && (AllActionsSubmitted() || now >= deadline);
            if (phase == "vote") return now >= deadline || (AllActionsSubmitted() && (settings.mode != "remote" || now - phaseStarted >= 30000));
            if (phase == "reaction") return AllActionsSubmitted() || now >= deadline;
            return false;
        }
        private bool ShouldAutoAdvance(long now)
        {
            if (phase == "lobby" || phase == "finished") return false;
            if (phase == "distribution") return acknowledged.Count == players.Count || now >= deadline;
            if (phase == "dawn" || phase == "discussion" || phase == "verdict") return now >= deadline;
            if (phase == "vote" && settings.mode == "remote") return now >= deadline;
            // The physical observer needs a real window even after the wolves first agree.
            // The host may still close it explicitly after the normal three-second minimum.
            if (phase == "night" && nightSteps[nightIndex] == "lovers") return now >= deadline;
            if (phase == "night" && nightSteps[nightIndex] == "werewolf" && CallsRole("little-girl"))
                return now >= deadline;
            return CanAdvance(now);
        }

        private RuleResult Advance(long now)
        {
            switch (phase)
            {
                case "distribution":
                    if (acknowledged.Count < players.Count)
                        Event("distribution-timeout", "La découverte des cartes est terminée après deux minutes. Les personnes absentes passent leurs actions à l’échéance.");
                    BeginNight(now, true);
                    break;
                case "night": FinishNightStep(now); break;
                case "dawn": ChangePhase("discussion", now, settings.discussionSeconds * 1000L); break;
                case "discussion": BeginVote(now, null, 1); break;
                case "vote": ResolveVote(now); break;
                case "verdict":
                    if (afterVerdict == "runoff") BeginVote(now, verdict.tiedPlayerIds, 2);
                    else FinishAfterReactions(now, "night");
                    break;
                case "reaction": ResolveHunter(now); break;
                default: return Fail("WRONG_PHASE", "Cette étape ne peut pas avancer.");
            }
            return RuleResult.Success();
        }

        private void BeginNight(long now, bool first = false)
        {
            verdict = null;
            day = first ? 1 : day + 1;
            protectedTarget = wolfVictim = poisonVictim = null;
            healedTonight = false; nightSteps.Clear(); nightIndex = 0;
            if (day == 1 && HasLivingRole("cupid")) { nightSteps.Add("cupid"); nightSteps.Add("lovers"); }
            if (CallsRole("guard")) nightSteps.Add("guard");
            if (CallsRole("seer")) nightSteps.Add("seer");
            if (CallsRole("werewolf")) nightSteps.Add("werewolf");
            // A public call must not disclose whether the witch still owns a potion.
            // If dead cards stay hidden, retain their public calls for the same reason.
            if (CallsRole("witch")) nightSteps.Add("witch");
            if (nightSteps.Count == 0) { FinishAfterReactions(now, "dawn"); return; }
            ChangePhase("night", now, settings.nightSeconds * 1000L);
        }

        private void FinishNightStep(long now)
        {
            var step = nightSteps[nightIndex];
            foreach (var actor in players.Where(p => p.Alive && RoleOf(p.Id) == step))
            {
                submissions.TryGetValue(actor.Id, out var action);
                if (step == "guard")
                {
                    lastGuardTarget = action != null && action.Choice == "protect" ? action.Targets[0] : null;
                    protectedTarget = lastGuardTarget;
                }
                if (action == null || action.Choice == "pass") continue;
                if (step == "cupid")
                {
                    var first = action.Targets[0]; var second = action.Targets[1];
                    lovers[first] = second; lovers[second] = first;
                    Fact(first, "lover", second, null, "Ton destin est lié à " + NameOf(second) + ".");
                    Fact(second, "lover", first, null, "Ton destin est lié à " + NameOf(first) + ".");
                    Fact(actor.Id, "cupid-choice", first, null, NameOf(first) + " et " + NameOf(second) + " sont liés.");
                }
                else if (step == "seer")
                {
                    var target = action.Targets[0];
                    Fact(actor.Id, "seer-" + day, target, RoleOf(target), NameOf(target) + " a pour rôle " + RoleCatalog.Find(RoleOf(target)).DisplayName + ".");
                }
                else if (step == "witch")
                {
                    if (action.Choice == "save" || action.Choice == "both") { healedTonight = true; healingAvailable = false; }
                    if (action.Choice == "poison" || action.Choice == "both") { poisonVictim = action.Targets[0]; poisonAvailable = false; }
                }
            }
            if (step == "werewolf")
            {
                var tally = submissions.Values.Where(s => s.Choice == "kill").GroupBy(s => s.Targets[0])
                    .Select(g => new { Id = g.Key, Count = g.Count() }).OrderByDescending(g => g.Count).ToArray();
                wolfVictim = tally.Length == 0 || (tally.Length > 1 && tally[0].Count == tally[1].Count) ? null : tally[0].Id;
                if (wolfVictim == protectedTarget && RoleOf(wolfVictim) != "little-girl") wolfVictim = null;
            }
            nightIndex++;
            if (nightIndex < nightSteps.Count) ChangePhase("night", now, nightSteps[nightIndex] == "lovers" ? 8000 : settings.nightSeconds * 1000L);
            else
            {
                var deaths = new List<string>();
                if (wolfVictim != null && !healedTonight) deaths.Add(wolfVictim);
                if (poisonVictim != null) deaths.Add(poisonVictim);
                KillBatch(deaths);
                FinishAfterReactions(now, "dawn");
            }
        }

        private void BeginVote(long now, string[] candidates, int round)
        {
            verdict = null;
            runoffTargets = candidates;
            voteRound = round;
            ChangePhase("vote", now, settings.mode == "remote" ? settings.voteSeconds * 1000L : 45000);
            Event("vote-open", round == 1 ? "Le village peut voter." : "Un second vote départage les personnes à égalité.");
        }

        private void ResolveVote(long now)
        {
            var records = players.Where(p => p.Alive).Select(p =>
            {
                submissions.TryGetValue(p.Id, out var submission);
                return new VoteRecordView { playerId = p.Id,
                    targetId = submission?.Choice == "vote" ? submission.Targets[0] : null,
                    abstained = submission == null || submission.Choice == "pass" };
            }).ToArray();
            foreach (var player in players.Where(p => p.Alive))
            {
                submissions.TryGetValue(player.Id, out var vote);
                Event("vote", vote == null || vote.Choice == "pass" ? player.Name + " s’abstient."
                    : player.Name + " vote pour " + NameOf(vote.Targets[0]) + ".", player.Id);
            }
            var tally = submissions.Values.Where(s => s.Choice == "vote").GroupBy(s => s.Targets[0])
                .Select(g => new { Id = g.Key, Count = g.Count() }).OrderByDescending(g => g.Count).ToArray();
            if (tally.Length > 0)
            {
                var tied = tally.Where(t => t.Count == tally[0].Count).Select(t => t.Id).ToArray();
                if (tied.Length > 1 && voteRound == 1)
                {
                    verdict = new VoteVerdictView { result = "runoff", tiedPlayerIds = tied, votes = records };
                    afterVerdict = "runoff";
                    Event("vote-tie", "Égalité : un second vote va départager les personnes désignées.");
                    ChangePhase("verdict", now, 5000); return;
                }
                if (tied.Length == 1)
                {
                    verdict = new VoteVerdictView { result = "elimination", eliminatedPlayerId = tied[0], votes = records };
                    afterVerdict = "night";
                    KillBatch(new[] { tied[0] });
                    ChangePhase("verdict", now, 5000); return;
                }
            }
            verdict = new VoteVerdictView { result = "no-elimination", votes = records };
            afterVerdict = "night";
            Event("no-elimination", "Personne n’est éliminé par ce vote.");
            ChangePhase("verdict", now, 5000);
        }

        private void KillBatch(IEnumerable<string> initial)
        {
            var pending = new Queue<string>(initial.Distinct(StringComparer.Ordinal));
            while (pending.Count > 0)
            {
                var id = pending.Dequeue();
                var player = FindPlayer(id);
                if (player == null || !player.Alive) continue;
                player.Alive = false;
                var reveal = settings.revealEliminatedRoles ? " Son rôle : " + RoleCatalog.Find(RoleOf(id)).DisplayName + "." : string.Empty;
                Event("elimination", player.Name + " est éliminé." + reveal, id);
                if (RoleOf(id) == "hunter" && reactedHunters.Add(id)) hunterReactions.Enqueue(id);
                if (lovers.TryGetValue(id, out var partner) && FindPlayer(partner).Alive) pending.Enqueue(partner);
            }
        }

        private void FinishAfterReactions(long now, string next)
        {
            afterReaction = next;
            if (hunterReactions.Count > 0) ChangePhase("reaction", now, 20000);
            else ContinueAfterReactions(now);
        }

        private void ResolveHunter(long now)
        {
            var hunter = hunterReactions.Dequeue();
            if (submissions.TryGetValue(hunter, out var action) && action.Choice == "shoot") KillBatch(action.Targets);
            else Event("reaction-pass", "Aucun dernier tir n’est effectué.");
            if (hunterReactions.Count > 0) ChangePhase("reaction", now, 20000);
            else ContinueAfterReactions(now);
        }

        private void ContinueAfterReactions(long now)
        {
            if (CheckVictory(now)) return;
            if (afterReaction == "dawn")
            {
                Event("dawn", "Le jour se lève sur le village.");
                ChangePhase("dawn", now, 4000);
            }
            else BeginNight(now);
        }

        private bool CheckVictory(long now)
        {
            var alive = players.Where(p => p.Alive).ToArray();
            if (alive.Length == 0) { Finish("draw", Array.Empty<string>(), "Personne ne survit : la partie se termine à égalité.", now); return true; }
            if (alive.Length == 2 && lovers.TryGetValue(alive[0].Id, out var other) && other == alive[1].Id && IsMixedLover(alive[0].Id))
            { Finish("lovers", alive.Select(p => p.Id).ToArray(), "Les amoureux de camps opposés remportent la partie.", now); return true; }
            if (alive.All(p => RoleOf(p.Id) != "werewolf"))
            { Finish("village", players.Where(p => RoleOf(p.Id) != "werewolf" && !IsMixedLover(p.Id)).Select(p => p.Id).ToArray(), "Le village a éliminé tous les loups.", now); return true; }
            if (alive.All(p => RoleOf(p.Id) == "werewolf"))
            { Finish("wolves", players.Where(p => RoleOf(p.Id) == "werewolf" && !IsMixedLover(p.Id)).Select(p => p.Id).ToArray(), "Les loups ont éliminé le dernier adversaire du village.", now); return true; }
            return false;
        }

        private bool IsMixedLover(string id) => lovers.TryGetValue(id, out var other)
            && (RoleOf(id) == "werewolf") != (RoleOf(other) == "werewolf");
        private void Finish(string winningTeam, string[] winners, string message, long now)
        {
            winner = winningTeam; winnerIds = winners;
            ChangePhase("finished", now, 0);
            Event("finished", message);
        }

        private void ChangePhase(string next, long now, long duration)
        {
            phase = next; turn++; revision++; phaseStarted = now;
            deadline = duration == 0 ? 0 : now > long.MaxValue - duration ? long.MaxValue : now + duration;
            submissions.Clear();
        }

        private RoleCount[] Composition()
        {
            if (settings.composition.Length > 0) return settings.composition.Select(CopyCount).ToArray();
            var count = players.Count;
            if (count < 6) return Array.Empty<RoleCount>();
            var wolves = count == 6 ? 1 : count <= 11 ? 2 : count <= 18 ? 3 : 4;
            var deck = new List<RoleCount> { new RoleCount { roleId = "werewolf", count = wolves }, new RoleCount { roleId = "seer", count = 1 } };
            if (count >= 8) { deck.Add(new RoleCount { roleId = "witch", count = 1 }); deck.Add(new RoleCount { roleId = "hunter", count = 1 }); }
            if (count >= 10) deck.Add(new RoleCount { roleId = "guard", count = 1 });
            if (count >= 12) deck.Add(new RoleCount { roleId = "cupid", count = 1 });
            deck.Add(new RoleCount { roleId = "villager", count = count - deck.Sum(r => r.count) });
            return deck.ToArray();
        }

        private RuleResult ValidateDeck(RoleCount[] deck)
        {
            if (deck.Sum(c => c.count) != players.Count) return Fail("DECK_SIZE", "La composition doit contenir exactement une carte par joueur.");
            var wolves = deck.Where(c => c.roleId == "werewolf").Sum(c => c.count);
            if (wolves == 0 || wolves >= players.Count) return Fail("INVALID_COMPOSITION", "Il faut des loups et des habitants qui ne sont pas loups.");
            return RuleResult.Success();
        }

        private static RuleResult ValidateSettings(RoomSettings value)
        {
            if (value.mode != "in-person" && value.mode != "remote") return Fail("INVALID_MODE", "Choisis le mode en présence ou à distance.");
            if (value.nightProtocol != "traditional" && value.nightProtocol != "screen")
                return Fail("INVALID_NIGHT_PROTOCOL", "Choisis la nuit traditionnelle ou la nuit sur écran.");
            if (value.ruleset != "ngame-classic-v1") return Fail("UNSUPPORTED_RULESET", "Ce jeu de règles n’est pas disponible.");
            if (value.discussionSeconds < 5 || value.discussionSeconds > 600 || value.nightSeconds < 3 || value.nightSeconds > 120
                || value.voteSeconds < 30 || value.voteSeconds > 300)
                return Fail("INVALID_DURATION", "Discussion : 5 à 600 secondes ; étape nocturne : 3 à 120 secondes ; vote distant : 30 à 300 secondes.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var total = 0L;
            foreach (var entry in value.composition)
            {
                if (entry == null || entry.roleId == null || !SupportedRoles.Contains(entry.roleId)) return Fail("UNSUPPORTED_ROLE", "Un rôle de la composition n’a pas encore de moteur de règles.");
                if (!ids.Add(entry.roleId)) return Fail("DUPLICATE_ROLE", "Regroupe chaque rôle dans une seule ligne de composition.");
                if (entry.count < 1 || entry.count > 24 || (entry.roleId != "werewolf" && entry.roleId != "villager" && entry.count > 1))
                    return Fail("INVALID_ROLE_COUNT", "Les rôles spéciaux sont uniques dans cette première version.");
                if (entry.roleId == "little-girl" && (value.mode != "in-person" || value.nightProtocol != "traditional"))
                    return Fail("PHYSICAL_ROLE", "La Petite Fille exige une partie en présence avec nuit traditionnelle, les yeux fermés. Elle n’est pas disponible en nuit sur écran.");
                total += entry.count;
            }
            if (value.composition.Length > 0 && (total < 6 || total > 24)) return Fail("DECK_SIZE", "Prévois entre six et vingt-quatre cartes.");
            return RuleResult.Success();
        }

        private string Status()
        {
            switch (phase)
            {
                case "lobby": return "Chacun rejoint le village et confirme qu’il est prêt.";
                case "distribution": return "Découvre discrètement ta carte puis confirme. La nuit commence quand chacun est prêt, ou après deux minutes d’attente.";
                case "night":
                    if (settings.nightProtocol == "screen") return "La nuit sur écran est en cours. Chacun garde son téléphone ; ouvre ta vue privée pour tes choix. Chaque appel garde la même durée, même après confirmation.";
                    if (settings.mode != "in-person") return "Le village dort. Consulte discrètement les actions qui te sont proposées.";
                    switch (nightSteps[nightIndex])
                    {
                        case "cupid": return "Tout le monde ferme les yeux. Cupidon se réveille et lie deux amoureux, puis se rendort.";
                        case "lovers": return "Les amoureux se reconnaissent, puis se rendorment. Les autres gardent les yeux fermés.";
                        case "guard": return "Le Garde se réveille et choisit sa protection. Les autres gardent les yeux fermés.";
                        case "seer": return "La Voyante se réveille et consulte une carte. Les autres gardent les yeux fermés.";
                        case "werewolf": return "Les loups se réveillent et choisissent leur victime. Les autres gardent les yeux fermés ; seule la Petite Fille peut regarder discrètement.";
                        case "witch": return "La Sorcière se réveille pour ses potions. Les autres gardent les yeux fermés.";
                        default: return "Tout le monde ferme les yeux. Le village s’endort.";
                    }
                case "dawn": return "Le jour se lève. Les événements de la nuit ont été résolus.";
                case "discussion": return "Échangez vos soupçons avant le vote.";
                case "vote": return voteRound == 2 ? "Second vote entre les personnes à égalité." : "Le village choisit ensemble.";
                case "verdict":
                    return verdict?.result == "runoff" ? "Égalité : les personnes désignées seront départagées au second tour."
                        : verdict?.eliminatedPlayerId != null ? NameOf(verdict.eliminatedPlayerId) + " est éliminé par le vote du village."
                        : "Personne n’est éliminé par ce vote.";
                case "reaction": return "Une dernière réaction doit être résolue.";
                case "finished": return events.LastOrDefault(e => e.kind == "finished")?.text ?? "La partie est terminée.";
                default: return string.Empty;
            }
        }

        private string PhaseTitle()
        {
            switch (phase)
            {
                case "lobby": return "Le salon du village";
                case "distribution": return "Découverte des rôles";
                case "night": return "Le village s’endort";
                case "dawn": return "Le jour se lève";
                case "discussion": return "Les soupçons du village";
                case "vote": return voteRound == 2 ? "Le second vote" : "Le vote du village";
                case "verdict": return "Le verdict du village";
                case "reaction": return "Un dernier geste";
                case "finished": return "Résultat de la partie";
                default: return "Le village";
            }
        }

        /// <summary>Detached server-only save; call under the same room lock as Apply and StateFor.</summary>
        public RoomSnapshot CaptureSnapshot()
        {
            return new RoomSnapshot
            {
                code = Code, settings = CopySettings(settings), hostId = hostId, phase = phase, matchId = matchId,
                winner = winner, winnerIds = (string[])winnerIds.Clone(), randomSeed = randomSeed, randomDrawCount = randomDrawCount,
                lastGuardTarget = lastGuardTarget, protectedTarget = protectedTarget, wolfVictim = wolfVictim,
                poisonVictim = poisonVictim, afterReaction = afterReaction, afterVerdict = afterVerdict,
                runoffTargets = runoffTargets == null ? null : (string[])runoffTargets.Clone(),
                healingAvailable = healingAvailable, poisonAvailable = poisonAvailable, healedTonight = healedTonight,
                nightIndex = nightIndex, voteRound = voteRound, turn = turn, day = day, eventSequence = eventSequence,
                revision = revision, phaseStarted = phaseStarted, deadline = deadline, lastNow = lastNow,
                nightSteps = nightSteps.ToArray(), hunterReactions = hunterReactions.ToArray(), reactedHunters = reactedHunters.ToArray(),
                acknowledgedIds = acknowledged.ToArray(),
                players = players.Select(p => new SnapshotPlayer { id = p.Id, name = p.Name, avatar = p.Avatar,
                    ready = p.Ready, alive = p.Alive, connected = p.Connected, departed = p.Departed }).ToArray(),
                roles = roles.Select(p => new SnapshotValue { key = p.Key, value = p.Value }).ToArray(),
                lovers = lovers.Select(p => new SnapshotValue { key = p.Key, value = p.Value }).ToArray(),
                submissions = submissions.Select(p => new SnapshotSubmission { playerId = p.Key,
                    choice = p.Value.Choice, targets = (string[])p.Value.Targets.Clone() }).ToArray(),
                facts = facts.Select(p => new SnapshotFacts { playerId = p.Key, values = p.Value.Select(CopyFact).ToArray() }).ToArray(),
                receipts = receiptOrder.Select(key => new SnapshotReceipt { key = key,
                    fingerprint = receipts[key].Fingerprint, result = CopyResult(receipts[key].Result) }).ToArray(),
                events = events.Select(CopyEvent).ToArray(), verdict = CopyVerdict(verdict)
            };
        }

        /// <summary>Restores trusted server storage only. Never accept snapshots from an HTTP client.</summary>
        public static WerewolfRoom RestoreSnapshot(RoomSnapshot snapshot)
        {
            if (snapshot == null || snapshot.version != 1 || snapshot.settings == null
                || snapshot.players == null || snapshot.players.Length > 24
                || snapshot.randomDrawCount < 0 || snapshot.randomDrawCount > 1000000)
                throw new ArgumentException("Sauvegarde de partie invalide ou non prise en charge.", nameof(snapshot));
            var room = new WerewolfRoom(snapshot.code, snapshot.settings, snapshot.randomSeed)
            {
                hostId = snapshot.hostId, phase = snapshot.phase, matchId = snapshot.matchId,
                winner = snapshot.winner, winnerIds = (string[])(snapshot.winnerIds ?? Array.Empty<string>()).Clone(),
                lastGuardTarget = snapshot.lastGuardTarget, protectedTarget = snapshot.protectedTarget,
                wolfVictim = snapshot.wolfVictim, poisonVictim = snapshot.poisonVictim,
                afterReaction = snapshot.afterReaction, afterVerdict = snapshot.afterVerdict,
                runoffTargets = snapshot.runoffTargets == null ? null : (string[])snapshot.runoffTargets.Clone(),
                healingAvailable = snapshot.healingAvailable, poisonAvailable = snapshot.poisonAvailable,
                healedTonight = snapshot.healedTonight, nightIndex = snapshot.nightIndex,
                voteRound = snapshot.voteRound, turn = snapshot.turn, day = snapshot.day,
                eventSequence = snapshot.eventSequence, revision = snapshot.revision,
                phaseStarted = snapshot.phaseStarted, deadline = snapshot.deadline, lastNow = snapshot.lastNow,
                verdict = CopyVerdict(snapshot.verdict), randomDrawCount = snapshot.randomDrawCount
            };
            // Random.Next(smallMaximum) consumes the same seeded PRNG step as Next().
            for (var i = 0; i < snapshot.randomDrawCount; i++) room.random.Next();
            foreach (var value in snapshot.players)
            {
                room.players.Add(new Player { Id = value.id, Name = value.name, Avatar = value.avatar,
                    Ready = value.ready, Alive = value.alive, Connected = value.connected, Departed = value.departed });
                room.facts[value.id] = new List<PrivateFact>();
            }
            foreach (var value in snapshot.roles ?? Array.Empty<SnapshotValue>()) room.roles.Add(value.key, value.value);
            foreach (var value in snapshot.lovers ?? Array.Empty<SnapshotValue>()) room.lovers.Add(value.key, value.value);
            foreach (var value in snapshot.submissions ?? Array.Empty<SnapshotSubmission>())
                room.submissions.Add(value.playerId, new Submission { Choice = value.choice,
                    Targets = (string[])(value.targets ?? Array.Empty<string>()).Clone() });
            foreach (var value in snapshot.facts ?? Array.Empty<SnapshotFacts>())
                room.facts[value.playerId] = (value.values ?? Array.Empty<PrivateFact>()).Select(CopyFact).ToList();
            foreach (var value in snapshot.receipts ?? Array.Empty<SnapshotReceipt>())
            {
                room.receipts.Add(value.key, new Receipt { Fingerprint = value.fingerprint, Result = CopyResult(value.result) });
                room.receiptOrder.Enqueue(value.key);
            }
            room.events.AddRange((snapshot.events ?? Array.Empty<PublicEvent>()).Select(CopyEvent));
            room.nightSteps.AddRange(snapshot.nightSteps ?? Array.Empty<string>());
            foreach (var value in snapshot.hunterReactions ?? Array.Empty<string>()) room.hunterReactions.Enqueue(value);
            foreach (var value in snapshot.reactedHunters ?? Array.Empty<string>()) room.reactedHunters.Add(value);
            foreach (var value in snapshot.acknowledgedIds ?? Array.Empty<string>()) room.acknowledged.Add(value);
            if (!new[] { "lobby", "distribution", "night", "dawn", "discussion", "vote", "verdict", "reaction", "finished" }.Contains(room.phase)
                || (room.phase == "night" && (room.nightIndex < 0 || room.nightIndex >= room.nightSteps.Count))
                || room.players.Any(p => !room.facts.ContainsKey(p.Id))
                || (room.phase != "lobby" && room.players.Any(p => !room.roles.ContainsKey(p.Id)))
                || (room.phase == "verdict" && room.verdict == null)
                || (room.phase == "reaction" && room.hunterReactions.Count == 0))
                throw new ArgumentException("Sauvegarde de partie incohérente.", nameof(snapshot));
            return room;
        }

        private Player FindPlayer(string id) => id == null ? null : players.FirstOrDefault(p => p.Id == id);
        private string RoleOf(string id) => id != null && roles.TryGetValue(id, out var role) ? role : null;
        private bool HasLivingRole(string role) => players.Any(p => p.Alive && RoleOf(p.Id) == role);
        private bool CallsRole(string role) => HasLivingRole(role) || (!settings.revealEliminatedRoles && roles.Values.Contains(role));
        private string NameOf(string id) => FindPlayer(id)?.Name ?? "une personne";
        private void Event(string kind, string text, string id = null)
        {
            events.Add(new PublicEvent { sequence = ++eventSequence, kind = kind, text = text, playerId = id });
            revision++;
        }
        private void Fact(string recipient, string key, string subject, string role, string description)
        {
            facts[recipient].Add(new PrivateFact { key = key, playerId = subject, roleId = role, description = description });
        }
        private static RoleCount CopyCount(RoleCount value) => value == null ? null : new RoleCount { roleId = value.roleId, count = value.count };
        private static PrivateFact CopyFact(PrivateFact value) => new PrivateFact { key = value.key, playerId = value.playerId, roleId = value.roleId, description = value.description };
        private static PublicEvent CopyEvent(PublicEvent value) => new PublicEvent { sequence = value.sequence,
            kind = value.kind, text = value.text, playerId = value.playerId };
        private static VoteVerdictView CopyVerdict(VoteVerdictView value) => value == null ? null : new VoteVerdictView
        {
            result = value.result, eliminatedPlayerId = value.eliminatedPlayerId,
            tiedPlayerIds = (string[])(value.tiedPlayerIds ?? Array.Empty<string>()).Clone(),
            votes = (value.votes ?? Array.Empty<VoteRecordView>()).Select(v => new VoteRecordView
            { playerId = v.playerId, targetId = v.targetId, abstained = v.abstained }).ToArray()
        };
        private static RoomSettings CopySettings(RoomSettings value) => new RoomSettings
        {
            mode = value.mode, nightProtocol = value.nightProtocol ?? "traditional", ruleset = value.ruleset, discussionSeconds = value.discussionSeconds,
            nightSeconds = value.nightSeconds, voteSeconds = value.voteSeconds, revealEliminatedRoles = value.revealEliminatedRoles,
            composition = (value.composition ?? Array.Empty<RoleCount>()).Select(CopyCount).ToArray()
        };
        private static RuleResult CopyResult(RuleResult value) => new RuleResult { ok = value.ok, code = value.code, message = value.message };
        private static RuleResult Fail(string code, string text) => RuleResult.Fail(code, text);
        private static string Fingerprint(RoomCommand value)
        {
            var settingsPart = value.settings == null ? string.Empty : value.settings.mode + ":" + value.settings.ruleset
                + ":" + value.settings.discussionSeconds + ":" + value.settings.nightSeconds + ":" + value.settings.voteSeconds + ":" + value.settings.revealEliminatedRoles
                + ":" + string.Join(",", (value.settings.composition ?? Array.Empty<RoleCount>()).Select(c => c == null ? "null" : c.roleId + "=" + c.count))
                // Preserve old traditional command receipts when restoring a pre-protocol save.
                + (value.settings.nightProtocol == null || value.settings.nightProtocol == "traditional" ? string.Empty : ":nightProtocol=" + value.settings.nightProtocol);
            return value.kind + "\u001f" + value.turn + "\u001f" + value.choice + "\u001f" + value.ready + "\u001f"
                + string.Join("\u001f", value.targetIds ?? Array.Empty<string>()) + "\u001f" + settingsPart;
        }
    }
}
