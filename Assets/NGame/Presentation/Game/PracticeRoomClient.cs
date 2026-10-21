using System;
using System.Collections.Generic;
using System.Linq;
using NGame.Core.Multiplayer;
using UnityEngine;

namespace NGame.Presentation.Game
{
    /// <summary>Offline practice, explicitly simulated. Bots decide only from their own authorized view.</summary>
    public sealed partial class PracticeRoomClient : MonoBehaviour, IRoomClient, IRoomCommunicationClient
    {
        private WerewolfRoom room;
        private const string HumanId = "practice-human";
        private readonly List<string> bots = new List<string>();
        private readonly System.Random random = new System.Random();
        private float nextThink;
        private float turnBegan;
        private float practiceElapsed;
        private int observedTurn = -1;
        private bool active;
        private bool wasFocused = true;
        private long revision = -1;
        private int serial;
        public RoomView View { get; private set; }
        public string RoomCode => room == null ? string.Empty : room.Code;
        public bool HasSession => room != null;
        public bool IsBusy => false;
        public bool IsConnected => room != null;
        public bool IsTraining => true;
        public string StatusError { get; private set; }
        public event Action Changed;
        public event Action SecretsMustHide;
        private double practiceTimeMs;
        private long Now => (long)practiceTimeMs;

        // These controls describe the simulated clock, never another player's secret state.
        public bool PracticeClockPaused => View != null &&
            (View.phase == "discussion" || View.phase == "distribution" && !View.roleAcknowledged
             || View.action != null && !View.action.submitted && View.action.kind != "observe");
        public string PracticeHint
        {
            get
            {
                if (View == null) return "Entraînement individuel : sept bots, aucun autre joueur connecté.";
                switch (View.phase)
                {
                    case "lobby": return "Sept bots sont prêts. Lance la partie pour découvrir ton rôle.";
                    case "distribution": return "Maintiens ta carte, lis ton rôle, puis confirme. Le temps est suspendu.";
                    case "discussion": return "Lis le bilan, puis « Ouvrir le vote » quand tu es prêt. Le temps est suspendu.";
                    case "vote": return View.action != null && !View.action.submitted
                        ? "Lis le Chat des bots, choisis un suspect ou abstiens-toi, puis confirme. Temps suspendu."
                        : View.players.Any(p => p.id == View.selfPlayerId && p.alive)
                            ? "Ton vote est enregistré. Tu peux lire le Chat pendant les derniers votes des bots."
                            : "Tu es éliminé : lis le Village et écris dans Observateurs pendant le vote.";
                    case "dawn": return "Lis le bilan de la nuit. Une absence d’élimination ne révèle pas qui a protégé ou soigné.";
                    case "verdict": return "Lis le verdict. Tu peux relire les votes publics dans la chronique.";
                    case "finished": return "Consulte les rôles révélés et le résultat, puis relance une partie si tu veux.";
                    case "reaction": return View.action != null && !View.action.submitted
                        ? "Choisis ta cible ou renonce au dernier tir, puis confirme. Le temps est suspendu."
                        : "Un dernier pouvoir se résout avant la suite.";
                    case "night":
                        if (View.action != null && View.action.kind == "observe") return "Lis ton information privée. Elle n’apparaît pas dans le Chat du Village.";
                        if (View.action != null && !View.action.submitted) return "Lis ton pouvoir, choisis les cibles autorisées et confirme. Le temps est suspendu.";
                        return "Pas de choix pour toi à cet appel : les bots jouent. L’attente est accélérée.";
                    default: return "Tes pouvoirs restent privés. Appuie-toi sur les événements publics pour voter.";
                }
            }
        }

        public void CreateRoom(string displayName, int avatarIndex, RoomSettings settings)
        {
            SecretsMustHide?.Invoke();
            practiceTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            nextThink = 0;
            practiceElapsed = 0;
            ResetPracticeConversation();
            // Eight is enough to exercise potions, the guard, the seer, Cupid and death reactions.
            // The physical Little Girl is deliberately absent from a solo simulation.
            var practice = new RoomSettings
            {
                mode = "in-person", nightProtocol = "screen", discussionSeconds = 60, nightSeconds = 12, voteSeconds = 45,
                composition = new[]
                {
                    new RoleCount { roleId = "werewolf", count = 2 },
                    new RoleCount { roleId = "seer", count = 1 },
                    new RoleCount { roleId = "witch", count = 1 },
                    new RoleCount { roleId = "hunter", count = 1 },
                    new RoleCount { roleId = "guard", count = 1 },
                    new RoleCount { roleId = "cupid", count = 1 },
                    new RoleCount { roleId = "villager", count = 1 }
                }
            };
            room = new WerewolfRoom("ESSAI", practice, random.Next());
            practiceChat = new RoomCommunications();
            var name = string.IsNullOrWhiteSpace(displayName) ? "Moi" : displayName.Trim();
            var added = room.AddPlayer(HumanId, name, Mathf.Clamp(avatarIndex, 0, 5));
            if (!added.ok) { StatusError = added.message; room = null; Changed?.Invoke(); return; }
            bots.Clear();
            var names = new[] { "Léa", "Max", "Chloé", "Hugo", "Emma", "Lucas", "Jade" };
            for (var i = 0; i < names.Length; i++)
            {
                var id = "practice-" + i;
                var botName = names[i] + " · bot";
                room.AddPlayer(id, botName, i % 6);
                bots.Add(id);
                Apply(id, new RoomCommand { kind = "ready", ready = true });
            }
            Apply(HumanId, new RoomCommand { kind = "ready", ready = true });
            active = true;
            observedTurn = -1;
            revision = -1;
            StatusError = null;
            Refresh();
        }

        public void JoinRoom(string code, string displayName, int avatarIndex)
        {
            StatusError = "Cet entraînement est individuel. Rejoins une vraie soirée depuis le menu multijoueur.";
            Changed?.Invoke();
        }

        public void Send(RoomCommand command)
        {
            if (room == null || command == null) return;
            if (string.IsNullOrEmpty(command.commandId)) command.commandId = Guid.NewGuid().ToString("N");
            var result = Apply(HumanId, command);
            StatusError = result.ok ? null : result.message;
            if (command.kind == "leave" && result.ok) { ForgetRoom(); return; }
            Refresh();
        }

        private RuleResult Apply(string id, RoomCommand command)
        {
            var state = room.StateFor(id, Now);
            // Only internal bot commands acquire the current turn. UI commands retain their expected turn.
            if (id != HumanId || command.commandId == null) command.turn = state.turn;
            if (string.IsNullOrEmpty(command.commandId)) command.commandId = "practice-" + (++serial);
            return room.Apply(id, command, Now);
        }

        public void Refresh()
        {
            if (room == null) return;
            var state = room.StateFor(HumanId, Now);
            if (state.turn != observedTurn)
            {
                SecretsMustHide?.Invoke();
                observedTurn = state.turn;
                turnBegan = practiceElapsed;
            }
            View = state;
            UpdatePracticeCommunications(state);
            revision = state.revision;
            Changed?.Invoke();
        }

        public void SetActive(bool value)
        {
            active = value;
            if (!value) SecretsMustHide?.Invoke();
            else if (room != null) Refresh();
        }
        public void RetryPending()
        {
            StatusError = null;
            Refresh();
        }
        public void ForgetRoom()
        {
            SecretsMustHide?.Invoke();
            room = null; View = null; active = false; bots.Clear(); revision = -1;
            StatusError = null;
            ClearPracticeCommunications();
            Changed?.Invoke();
        }

        private void Update() => AdvancePractice(Time.unscaledDeltaTime);

        private void AdvancePractice(float elapsedSeconds)
        {
            if (!active || room == null || !wasFocused) return;
            // A local lesson is pausable: leaving the app cannot silently expire the learner's choice.
            // Only passive night calls run faster. Dawn, verdict and private observations retain reading time.
            var speed = View != null && View.phase == "night" && View.action == null ? 2d : 1d;
            var elapsed = Math.Max(0, Math.Min(elapsedSeconds, .25f));
            practiceElapsed += elapsed;
            if (!PracticeClockPaused) practiceTimeMs += elapsed * 1000d * speed;
            if (practiceElapsed < nextThink) return;
            nextThink = practiceElapsed + .2f;
            room.Tick(Now);
            var self = room.StateFor(HumanId, Now);
            if (self.turn != observedTurn) { Refresh(); self = View; }
            for (var i = 0; i < bots.Count; i++)
            {
                // Visible public vote progress is staggered; no private actor indicator is exposed.
                if (practiceElapsed - turnBegan < .6f + i * .22f) continue;
                var id = bots[i];
                var bot = room.StateFor(id, Now);
                if (bot.turn != observedTurn) break;
                if (bot.phase == "distribution") Apply(id, new RoomCommand { kind = "acknowledge-role" });
                else if (bot.phase == "lobby")
                {
                    var player = bot.players.FirstOrDefault(p => p.id == id);
                    if (player != null && !player.ready) Apply(id, new RoomCommand { kind = "ready", ready = true });
                }
                else if (bot.action != null && !bot.action.submitted && bot.action.kind != "observe")
                {
                    var command = Choose(bot);
                    if (command != null) Apply(id, command);
                }
            }
            self = room.StateFor(HumanId, Now);
            if (self.turn != observedTurn) { Refresh(); self = View; }
            // A discussion ends only when the learner chooses to continue, never after ten silent seconds.
            UpdatePracticeConversation(self);
            var latest = room.StateFor(HumanId, Now);
            // Refresh the clock offset as well, including frozen reading windows.
            if (latest.revision != revision || latest.turn != observedTurn || PracticeClockPaused) Refresh();
        }

        private RoomCommand Choose(RoomView bot)
        {
            var action = bot.action;
            var targets = action.allowedTargetIds ?? Array.Empty<string>();
            var choices = action.choices ?? Array.Empty<string>();
            var command = new RoomCommand { kind = action.kind == "vote" ? "vote" : "act" };
            if (choices.Length == 0) return null;
            if (action.kind == "witch")
            {
                // The witch knows the attacked person's name, not their hidden role.
                if (choices.Contains("save")) { command.choice = "save"; return command; }
                command.choice = "pass";
                var knownWolf = KnownWolf(bot, targets);
                if (knownWolf != null && choices.Contains("poison"))
                { command.choice = "poison"; command.targetIds = new[] { knownWolf }; }
                return command;
            }
            if (targets.Length < action.targetCount)
            { command.choice = "pass"; return command; }
            command.choice = choices.FirstOrDefault(c => c != "pass") ?? "pass";
            if (command.choice == "pass") return command;
            if (action.targetCount == 2)
            {
                command.targetIds = targets.OrderBy(_ => random.Next()).Take(2).ToArray();
                return command;
            }
            if (action.targetCount == 1)
            {
                string target = null;
                if (action.kind == "wolves")
                {
                    // Pack members can see their colleagues' choices. Stable agreement avoids accidental ties.
                    foreach (var player in bot.players)
                        if (targets.Contains(player.id) && !string.IsNullOrEmpty(action.privateInfo)
                            && action.privateInfo.Contains(" choisit " + player.displayName + "."))
                        { target = player.id; break; }
                    target = target ?? targets[(bot.day + 1) % targets.Length];
                }
                else if (action.kind == "vote" || action.kind == "hunter")
                {
                    target = KnownWolf(bot, targets);
                    var pool = bot.selfRoleId == "werewolf"
                        ? targets.Where(id => !bot.wolfAllyIds.Contains(id)).ToArray() : targets;
                    // Publicly suspicious decisions are used by every bot; hidden roles are never queried.
                    if (target == null && pool.Length > 0)
                    {
                        var previous = bot.events.Where(e => e.kind == "vote" && !string.IsNullOrEmpty(e.playerId))
                            .GroupBy(e => e.playerId).OrderByDescending(g => g.Count()).FirstOrDefault();
                        target = previous != null && pool.Contains(previous.Key) ? previous.Key
                            : pool[(bot.day + 2) % pool.Length];
                    }
                }
                target = target ?? targets[random.Next(targets.Length)];
                command.targetIds = new[] { target };
            }
            return command;
        }

        private static string KnownWolf(RoomView bot, string[] allowed)
        {
            return bot.privateFacts.FirstOrDefault(f => f.roleId == "werewolf" && allowed.Contains(f.playerId))?.playerId;
        }
        private void OnApplicationFocus(bool focused)
        {
            wasFocused = focused;
            if (!focused) SecretsMustHide?.Invoke();
        }
        private void OnApplicationPause(bool paused)
        {
            wasFocused = !paused;
            if (paused) SecretsMustHide?.Invoke();
        }
        private void OnDisable() => SecretsMustHide?.Invoke();
    }
}
