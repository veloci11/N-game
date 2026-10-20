using System;
using System.Collections.Generic;
using System.Linq;
using NGame.Core.Multiplayer;
using NetworkRoleCount = NGame.Core.Multiplayer.RoleCount;
using NUnit.Framework;

namespace NGame.Core.Tests
{
    public sealed class WerewolfRoomTests
    {
        private sealed class Game
        {
            public readonly WerewolfRoom Room;
            public readonly string[] Ids;
            public long Now = 100000;
            private int commandSequence;
            public string Host => Ids[0];
            public RoomView View => For(Host);

            public Game(params string[] deck) : this(true, deck) { }
            public Game(bool revealDeaths, params string[] deck)
            {
                Ids = Enumerable.Range(0, deck.Length).Select(i => "p" + i).ToArray();
                Room = new WerewolfRoom("TEST42", new RoomSettings
                {
                    nightSeconds = 10, discussionSeconds = 5, revealEliminatedRoles = revealDeaths,
                    composition = deck.GroupBy(role => role).Select(g => new NetworkRoleCount { roleId = g.Key, count = g.Count() }).ToArray()
                }, 1729);
                for (var i = 0; i < Ids.Length; i++) Ok(Room.AddPlayer(Ids[i], "Joueur " + i, i % 6));
            }

            public RoomView For(string id) => Room.StateFor(id, Now);
            public string One(string role) => Ids.Single(id => For(id).selfRoleId == role);
            public string[] WithRole(string role) => Ids.Where(id => For(id).selfRoleId == role).ToArray();
            public PlayerView Player(string id) => View.players.Single(p => p.id == id);
            public RoomCommand Command(string id, string kind, string choice = null, params string[] targets)
                => new RoomCommand { kind = kind, choice = choice, targetIds = targets, turn = For(id).turn, commandId = "cmd-" + ++commandSequence };
            public RuleResult Send(string id, string kind, string choice = null, params string[] targets)
                => Room.Apply(id, Command(id, kind, choice, targets), Now);
            public void Start(bool acknowledgeRoles = true)
            {
                foreach (var id in Ids)
                {
                    var command = Command(id, "ready"); command.ready = true;
                    Ok(Room.Apply(id, command, Now));
                }
                Assert.That(View.canStart, Is.True);
                Ok(Send(Host, "start"));
                Assert.That(View.phase, Is.EqualTo("distribution"));
                if (acknowledgeRoles) foreach (var id in Ids) Ok(Send(id, "acknowledge-role"));
            }
            public void CloseStep()
            {
                var oldTurn = View.turn;
                Now += 3000; Room.Tick(Now);
                // The physical Petite Fille keeps the automatic window open, but a host may close it.
                if (View.turn == oldTurn) Ok(Send(Host, "advance"));
                Assert.That(View.turn, Is.GreaterThan(oldTurn));
            }
            public void PassCurrentNightStep()
            {
                Assert.That(View.phase, Is.EqualTo("night"));
                foreach (var id in Ids)
                {
                    var action = For(id).action;
                    if (action != null && action.kind != "observe" && !action.submitted) Ok(Send(id, "act", "pass"));
                }
                CloseStep();
            }
            public void PassNight()
            {
                for (var guard = 0; View.phase == "night" && guard < 12; guard++) PassCurrentNightStep();
                Assert.That(View.phase, Is.Not.EqualTo("night"), "A night must not stall after all roles pass.");
            }
            public string ReachAction(string kind)
            {
                for (var guard = 0; guard < 12 && View.phase == "night"; guard++)
                {
                    var actor = Ids.FirstOrDefault(id => For(id).action?.kind == kind);
                    if (actor != null) return actor;
                    PassCurrentNightStep();
                }
                Assert.Fail("Action not reached: " + kind);
                return null;
            }
            public void OpenVote()
            {
                if (View.phase == "dawn") { Now += 4000; Room.Tick(Now); }
                Assert.That(View.phase, Is.EqualTo("discussion"));
                Ok(Send(Host, "advance"));
                Assert.That(View.phase, Is.EqualTo("vote"));
            }
            public void Votes(Func<string, string> targetFor)
            {
                var living = View.players.Where(p => p.alive).Select(p => p.id).ToArray();
                foreach (var id in living)
                {
                    var target = targetFor(id);
                    Ok(target == null ? Send(id, "vote", "pass") : Send(id, "vote", "vote", target));
                }
                FinishVerdict();
            }
            public void FinishVerdict()
            {
                if (View.phase != "verdict") return;
                Now += 5000; Room.Tick(Now);
            }
            public void VoteOut(string target) => Votes(id => id == target ? null : target);
            public void NoEliminationDay() { OpenVote(); Votes(_ => null); }
            public void WolvesChoose(string target)
            {
                ReachAction("wolves");
                foreach (var id in WithRole("werewolf").Where(id => Player(id).alive))
                    Ok(target == null ? Send(id, "act", "pass") : Send(id, "act", "kill", target));
                CloseStep();
            }
        }

        private static void Ok(RuleResult result) => Assert.That(result.ok, Is.True, result.code + ": " + result.message);
        private static void Fails(RuleResult result, string code)
        { Assert.That(result.ok, Is.False); Assert.That(result.code, Is.EqualTo(code)); }
        private static string[] BasicDeck => new[] { "werewolf", "seer", "villager", "villager", "villager", "villager" };

        [Test]
        public void DistributionWaitsForEveryPersonalAcknowledgementWithoutStartingPowerClocks()
        {
            var game = new Game(BasicDeck); game.Start(false);
            var first = game.View;
            Assert.That(first.phase, Is.EqualTo("distribution"));
            Assert.That(first.deadlineUnixMs - game.Now, Is.EqualTo(120000));
            Assert.That(first.roleAcknowledged, Is.False); Assert.That(first.action, Is.Null);
            Assert.That(first.canAdvance, Is.False);
            Fails(game.Send(game.Host, "advance"), "STEP_PENDING");
            foreach (var id in game.Ids.Take(game.Ids.Length - 1)) Ok(game.Send(id, "acknowledge-role"));
            game.Now += 60000; game.Room.Tick(game.Now);
            Assert.That(game.View.phase, Is.EqualTo("distribution"));
            Assert.That(game.View.acknowledgedCount, Is.EqualTo(5));
            Assert.That(game.For(game.Ids[0]).roleAcknowledged, Is.True);
            Ok(game.Send(game.Ids.Last(), "acknowledge-role"));
            Assert.That(game.View.phase, Is.EqualTo("night"));
            Assert.That(game.View.deadlineUnixMs - game.Now, Is.EqualTo(10000));
            Fails(game.Send(game.Host, "acknowledge-role"), "WRONG_PHASE");
        }

        [Test]
        public void DistributionTimeoutExplainsAbsenceAndStartsOnlyTheFirstNightWindow()
        {
            var game = new Game(BasicDeck); game.Start(false);
            var oldMatch = game.View.matchId;
            game.Now += 3600000; game.Room.Tick(game.Now);
            Assert.That(game.View.phase, Is.EqualTo("night"));
            Assert.That(game.View.nightStep, Is.EqualTo("seer"));
            Assert.That(game.View.deadlineUnixMs - game.Now, Is.EqualTo(10000));
            Assert.That(game.View.matchId, Is.EqualTo(oldMatch));
            Assert.That(game.View.events.Any(e => e.kind == "distribution-timeout"), Is.True);
            Assert.That(game.View.players.All(p => p.alive), Is.True);
        }

        [Test]
        public void RemoteVoteKeepsItsSpeechWindowAndRequiresHostOrDeadlineToClose()
        {
            var game = new Game(BasicDeck);
            var configure = game.Command(game.Host, "configure");
            configure.settings = new RoomSettings { mode = "remote", nightSeconds = 10, discussionSeconds = 5 };
            Ok(game.Room.Apply(game.Host, configure, game.Now));
            game.Start(); game.PassNight(); game.OpenVote();
            var voteTurn = game.View.turn;
            Assert.That(game.View.deadlineUnixMs - game.Now, Is.EqualTo(90000));
            game.Votes(_ => null);
            Assert.That(game.View.phase, Is.EqualTo("vote"));
            Assert.That(game.View.votesReceived, Is.EqualTo(6)); Assert.That(game.View.votesExpected, Is.EqualTo(6));
            Assert.That(game.View.selfVoteAbstained, Is.True);
            Fails(game.Send(game.Host, "advance"), "STEP_PENDING");
            game.Now += 30000; game.Room.Tick(game.Now);
            Assert.That(game.View.turn, Is.EqualTo(voteTurn), "The minimum does not auto-close the conversation.");
            Assert.That(game.View.canAdvance, Is.True);
            Fails(game.Send(game.Ids[1], "advance"), "HOST_ONLY");
            Ok(game.Send(game.Host, "advance"));
            Assert.That(game.View.phase, Is.EqualTo("verdict"));
            Assert.That(game.View.verdict.result, Is.EqualTo("no-elimination"));
        }

        [Test]
        public void VoteChoicesArePersonalUntilVerdictAndTheVerdictDoesNotStartAHunterClockEarly()
        {
            var game = new Game("werewolf", "hunter", "villager", "villager", "villager", "villager"); game.Start();
            game.WolvesChoose(null); game.OpenVote();
            var hunter = game.One("hunter"); var wolf = game.One("werewolf");
            Ok(game.Send(wolf, "vote", "vote", hunter));
            Assert.That(game.For(wolf).selfVoteTargetId, Is.EqualTo(hunter));
            var other = game.Ids.First(id => id != wolf);
            Assert.That(game.For(other).selfVoteTargetId, Is.Null);
            Assert.That(game.For(other).action.submittedTargetIds, Is.Empty);
            foreach (var id in game.Ids.Where(id => id != wolf))
                Ok(id == hunter ? game.Send(id, "vote", "pass") : game.Send(id, "vote", "vote", hunter));
            Assert.That(game.View.phase, Is.EqualTo("verdict"));
            Assert.That(game.View.verdict.eliminatedPlayerId, Is.EqualTo(hunter));
            Assert.That(game.View.verdict.votes.Length, Is.EqualTo(6));
            Assert.That(game.For(hunter).action, Is.Null);
            game.Now += 2999; game.Room.Tick(game.Now);
            Assert.That(game.View.canAdvance, Is.False);
            game.Now += 2001; game.Room.Tick(game.Now);
            Assert.That(game.View.phase, Is.EqualTo("reaction"));
            Assert.That(game.For(hunter).action.kind, Is.EqualTo("hunter"));
            Assert.That(game.View.deadlineUnixMs - game.Now, Is.EqualTo(20000));
            Assert.That(game.View.winner, Is.Null);
        }

        [Test]
        public void ConfirmedActionAndPotionReservesAreStructuredAndDetached()
        {
            var game = new Game("werewolf", "witch", "villager", "villager", "villager", "villager"); game.Start();
            var victim = game.WithRole("villager")[0]; var wolf = game.One("werewolf");
            Ok(game.Send(wolf, "act", "kill", victim));
            var action = game.For(wolf).action;
            Assert.That(action.submittedChoice, Is.EqualTo("kill"));
            Assert.That(action.submittedTargetIds, Is.EqualTo(new[] { victim }));
            action.submittedTargetIds[0] = "tampered";
            Assert.That(game.For(wolf).action.submittedTargetIds, Is.EqualTo(new[] { victim }));
            game.CloseStep();
            var witch = game.One("witch"); var potions = game.For(witch).action;
            Assert.That(potions.healingAvailable, Is.True); Assert.That(potions.poisonAvailable, Is.True);
            Assert.That(potions.wolfVictimId, Is.EqualTo(victim));
            Assert.That(game.For(victim).action, Is.Null);
        }

        [Test]
        public void LobbyDepartureRemovesThePlaceResetsConsentAndTransfersTheHost()
        {
            var game = new Game(BasicDeck);
            var ready = game.Command(game.Ids[1], "ready"); ready.ready = true;
            Ok(game.Room.Apply(game.Ids[1], ready, game.Now));
            Ok(game.Send(game.Host, "leave"));
            Assert.That(game.Room.ContainsPlayer(game.Host), Is.False);
            var remaining = game.For(game.Ids[1]);
            Assert.That(remaining.isHost, Is.True); Assert.That(remaining.players.Length, Is.EqualTo(5));
            Assert.That(remaining.players.All(p => !p.ready), Is.True);
            Assert.Throws<ArgumentException>(() => game.For(game.Host));
        }

        [Test]
        public void HostCanRemoveAnAbsentLobbyPlaceAndItsReceiptsWithoutKeepingFalseReadiness()
        {
            var game = new Game();
            // Use an automatic deck so removing one place adapts card count without inventing a manual composition.
            for (var i = 0; i < 7; i++) Ok(game.Room.AddPlayer("extra-" + i, "Invité " + i, i % 6));
            var host = "extra-0"; var removed = "extra-6";
            foreach (var id in Enumerable.Range(0, 7).Select(i => "extra-" + i))
                Ok(game.Room.Apply(id, new RoomCommand { kind = "ready", ready = true, commandId = "ready", turn = 0 }, game.Now));
            game.Room.SetConnected(removed, false);
            Assert.That(game.Room.StateFor(host, game.Now).startBlockedReason, Does.Contain("déconnectés"));
            var command = new RoomCommand { kind = "remove-player", targetIds = new[] { removed }, commandId = "remove", turn = 0 };
            Ok(game.Room.Apply(host, command, game.Now));
            Ok(game.Room.Apply(host, command, game.Now));
            Assert.That(game.Room.ContainsPlayer(removed), Is.False);
            var view = game.Room.StateFor(host, game.Now);
            Assert.That(view.players.Length, Is.EqualTo(6)); Assert.That(view.players.All(p => !p.ready), Is.True);
            Assert.That(view.canStart, Is.False); Assert.That(view.startBlockedReason, Does.Contain("déclarer prêt"));
            Assert.That(view.composition.Sum(c => c.count), Is.EqualTo(6));
            Assert.That(game.Room.CaptureSnapshot().receipts.Any(r => r.key.StartsWith(removed + "\n", StringComparison.Ordinal)), Is.False);
            Fails(game.Room.Apply(removed, new RoomCommand { kind = "ready", ready = true, commandId = "late", turn = 0 }, game.Now), "NOT_MEMBER");
        }

        [Test]
        public void RemovingAPlaceRequiresTheHostAndOneOtherCurrentMember()
        {
            var game = new Game(BasicDeck);
            Fails(game.Send(game.Ids[1], "remove-player", null, game.Ids[2]), "HOST_ONLY");
            Fails(game.Send(game.Host, "remove-player", null, game.Host), "INVALID_TARGET");
            Fails(game.Send(game.Host, "remove-player"), "INVALID_TARGET");
            Fails(game.Send(game.Host, "remove-player", null, game.Ids[1], game.Ids[2]), "INVALID_TARGET");
            Fails(game.Send(game.Host, "remove-player", null, "missing-member"), "INVALID_TARGET");
            Assert.That(game.View.players.Length, Is.EqualTo(6));
            game.Start(false);
            var secrets = game.Ids.Select(id => game.For(id).selfRoleId).ToArray();
            Fails(game.Send(game.Host, "remove-player", null, game.Ids[1]), "WRONG_PHASE");
            Assert.That(game.Ids.Select(id => game.For(id).selfRoleId), Is.EqualTo(secrets));
            foreach (var id in game.Ids) Ok(game.Send(id, "acknowledge-role"));
            Fails(game.Send(game.Host, "remove-player", null, game.Ids[1]), "WRONG_PHASE");
            Assert.That(game.View.players.Length, Is.EqualTo(6));
        }

        [Test]
        public void StartBlockedReasonMatchesTheActualStartRefusalAndARejectedConfigurationNeverReplacesIt()
        {
            var game = new Game(BasicDeck);
            Assert.That(game.View.startBlockedReason, Is.EqualTo(game.Send(game.Host, "start").message));
            foreach (var id in game.Ids)
            {
                var ready = game.Command(id, "ready"); ready.ready = true;
                Ok(game.Room.Apply(id, ready, game.Now));
            }
            Assert.That(game.View.canStart, Is.True); Assert.That(game.View.startBlockedReason, Is.Null);
            game.Room.SetConnected(game.Ids[1], false);
            Assert.That(game.View.startBlockedReason, Is.EqualTo(game.Send(game.Host, "start").message));
            game.Room.SetConnected(game.Ids[1], true);
            var configure = game.Command(game.Host, "configure");
            configure.settings = new RoomSettings { composition = new[] { new NetworkRoleCount { roleId = "werewolf", count = 1 }, new NetworkRoleCount { roleId = "villager", count = 6 } } };
            Ok(game.Room.Apply(game.Host, configure, game.Now));
            Assert.That(game.View.startBlockedReason, Is.EqualTo(game.Send(game.Host, "start").message));
            Assert.That(game.View.startBlockedReason, Does.Contain("exactement une carte"));
            configure = game.Command(game.Host, "configure"); configure.settings = new RoomSettings { mode = "remote", composition = new[]
            { new NetworkRoleCount { roleId = "werewolf", count = 1 }, new NetworkRoleCount { roleId = "little-girl", count = 1 }, new NetworkRoleCount { roleId = "villager", count = 4 } } };
            Fails(game.Room.Apply(game.Host, configure, game.Now), "PHYSICAL_ROLE");
            Assert.That(game.View.mode, Is.EqualTo("in-person"));
            Assert.That(game.View.startBlockedReason, Does.Contain("exactement une carte"), "A rejected proposal cannot become the current room's refusal reason.");
        }

        [Test]
        public void RemovingAPlaceKeepsAnExplicitDeckAndExplainsItsMismatchOrTheMinimumPlayerCount()
        {
            var seven = new Game("werewolf", "seer", "villager", "villager", "villager", "villager", "villager");
            Ok(seven.Send(seven.Host, "remove-player", null, seven.Ids.Last()));
            Assert.That(seven.View.composition.Sum(c => c.count), Is.EqualTo(7));
            Assert.That(seven.View.startBlockedReason, Does.Contain("exactement une carte"));
            Ok(seven.Send(seven.Host, "remove-player", null, seven.Ids[5]));
            Assert.That(seven.View.players.Length, Is.EqualTo(5));
            Assert.That(seven.View.startBlockedReason, Does.Contain("au moins six joueurs"));
            Fails(seven.Send(seven.Host, "start"), "NOT_ENOUGH_PLAYERS");
        }

        [Test]
        public void InGameDeparturePreservesItsSecretAndResumeDoesNotStealBackHosting()
        {
            var game = new Game(BasicDeck); game.Start();
            var role = game.View.selfRoleId; var match = game.View.matchId;
            Ok(game.Send(game.Host, "leave"));
            Assert.That(game.View.players.Single(p => p.id == game.Host).connected, Is.False);
            Assert.That(game.For(game.Ids[1]).isHost, Is.True);
            Assert.That(game.View.selfRoleId, Is.EqualTo(role));
            game.Room.SetConnected(game.Host, true);
            Assert.That(game.View.players.Single(p => p.id == game.Host).connected, Is.True);
            Assert.That(game.View.selfRoleId, Is.EqualTo(role)); Assert.That(game.View.matchId, Is.EqualTo(match));
            Assert.That(game.View.isHost, Is.False);
        }

        [Test]
        public void SnapshotPreservesSecretsReceiptsAndClockWithoutSharingMutableArrays()
        {
            var game = new Game(BasicDeck); game.Start();
            var seer = game.One("seer"); var wolf = game.One("werewolf");
            var inspect = game.Command(seer, "act", "inspect", wolf);
            Ok(game.Room.Apply(seer, inspect, game.Now)); game.CloseStep();
            var snapshot = game.Room.CaptureSnapshot();
            var restored = WerewolfRoom.RestoreSnapshot(snapshot);
            var view = restored.StateFor(seer, game.Now);
            Assert.That(view.matchId, Is.EqualTo(game.For(seer).matchId));
            Assert.That(view.turn, Is.EqualTo(game.View.turn)); Assert.That(view.deadlineUnixMs, Is.EqualTo(game.View.deadlineUnixMs));
            Assert.That(view.privateFacts.Single().roleId, Is.EqualTo("werewolf"));
            Ok(restored.Apply(seer, inspect, game.Now));
            Assert.That(restored.StateFor(seer, game.Now).privateFacts.Length, Is.EqualTo(1));
            Assert.That(restored.StateFor(wolf, game.Now).privateFacts, Is.Empty);
            snapshot.roles[0].value = "thief"; snapshot.facts.First(f => f.playerId == seer).values[0].roleId = "villager";
            snapshot.players[0].name = "changed";
            Assert.That(restored.StateFor(seer, game.Now).privateFacts.Single().roleId, Is.EqualTo("werewolf"));
            Assert.That(game.For(seer).privateFacts.Single().roleId, Is.EqualTo("werewolf"));
            Assert.That(restored.StateFor(seer, game.Now).players[0].displayName, Is.EqualTo("Joueur 0"));
        }

        [Test]
        public void RestoredFinishedGameShufflesTheSameFutureDeckAndResetChangesTheMatch()
        {
            var game = new Game(BasicDeck); game.Start(); game.PassNight(); game.OpenVote(); game.VoteOut(game.One("werewolf"));
            Assert.That(game.View.phase, Is.EqualTo("finished"));
            var oldMatch = game.View.matchId;
            var restored = WerewolfRoom.RestoreSnapshot(game.Room.CaptureSnapshot());
            foreach (var room in new[] { game.Room, restored })
            {
                Ok(room.Apply(game.Host, new RoomCommand { kind = "reset", turn = room.StateFor(game.Host, game.Now).turn, commandId = "resume-reset" }, game.Now));
                Assert.That(room.StateFor(game.Host, game.Now).matchId, Is.Not.EqualTo(oldMatch));
                foreach (var id in game.Ids)
                    Ok(room.Apply(id, new RoomCommand { kind = "ready", ready = true, turn = room.StateFor(id, game.Now).turn, commandId = "rematch-ready" }, game.Now));
                Ok(room.Apply(game.Host, new RoomCommand { kind = "start", turn = room.StateFor(game.Host, game.Now).turn, commandId = "rematch-start" }, game.Now));
            }
            foreach (var id in game.Ids)
                Assert.That(restored.StateFor(id, game.Now).selfRoleId, Is.EqualTo(game.Room.StateFor(id, game.Now).selfRoleId));
        }

        [Test]
        public void LeavingTheFinishedRosterDoesNotReplaceTheVictoryExplanation()
        {
            var game = new Game(BasicDeck); game.Start(); game.PassNight(); game.OpenVote(); game.VoteOut(game.One("werewolf"));
            var explanation = game.View.statusMessage;
            Assert.That(explanation, Does.Contain("Le village a éliminé"));
            Ok(game.Send(game.Host, "leave"));
            Assert.That(game.For(game.Ids[1]).statusMessage, Is.EqualTo(explanation));
            Assert.That(game.For(game.Ids[1]).winner, Is.EqualTo("village"));
        }

        [TestCase("lobby")]
        [TestCase("distribution")]
        [TestCase("night")]
        [TestCase("dawn")]
        [TestCase("discussion")]
        [TestCase("vote")]
        [TestCase("verdict")]
        [TestCase("reaction")]
        [TestCase("finished")]
        public void SnapshotRestoresEveryCriticalPhaseWithTheSameRecipientRights(string requestedPhase)
        {
            var game = requestedPhase == "reaction"
                ? new Game("werewolf", "hunter", "villager", "villager", "villager", "villager") : new Game(BasicDeck);
            if (requestedPhase == "distribution")
            {
                game.Start(false); Ok(game.Send(game.Ids[0], "acknowledge-role"));
            }
            else if (requestedPhase != "lobby")
            {
                game.Start();
                if (requestedPhase == "night")
                    Ok(game.Send(game.One("seer"), "act", "inspect", game.One("werewolf")));
                else if (requestedPhase == "reaction") game.WolvesChoose(game.One("hunter"));
                else
                {
                    game.PassNight();
                    if (requestedPhase == "discussion") { game.Now += 4000; game.Room.Tick(game.Now); }
                    if (requestedPhase == "vote" || requestedPhase == "verdict" || requestedPhase == "finished")
                    {
                        game.OpenVote();
                        if (requestedPhase == "vote") Ok(game.Send(game.Ids[0], "vote", "pass"));
                        else if (requestedPhase == "finished") game.VoteOut(game.One("werewolf"));
                        else foreach (var id in game.Ids) Ok(game.Send(id, "vote", "pass"));
                    }
                }
            }
            Assert.That(game.View.phase, Is.EqualTo(requestedPhase));
            var restored = WerewolfRoom.RestoreSnapshot(game.Room.CaptureSnapshot());
            foreach (var id in game.Ids)
            {
                var before = game.For(id); var after = restored.StateFor(id, game.Now);
                Assert.That(after.phase, Is.EqualTo(before.phase));
                Assert.That(after.matchId, Is.EqualTo(before.matchId));
                Assert.That(after.revision, Is.EqualTo(before.revision));
                Assert.That(after.turn, Is.EqualTo(before.turn));
                Assert.That(after.day, Is.EqualTo(before.day));
                Assert.That(after.deadlineUnixMs, Is.EqualTo(before.deadlineUnixMs));
                Assert.That(after.phaseStartedUnixMs, Is.EqualTo(before.phaseStartedUnixMs));
                Assert.That(after.selfRoleId, Is.EqualTo(before.selfRoleId));
                Assert.That(after.roleAcknowledged, Is.EqualTo(before.roleAcknowledged));
                Assert.That(after.acknowledgedCount, Is.EqualTo(before.acknowledgedCount));
                Assert.That(after.isHost, Is.EqualTo(before.isHost));
                Assert.That(after.canAdvance, Is.EqualTo(before.canAdvance));
                Assert.That(after.canStart, Is.EqualTo(before.canStart));
                Assert.That(after.voteRound, Is.EqualTo(before.voteRound));
                Assert.That(after.votesReceived, Is.EqualTo(before.votesReceived));
                Assert.That(after.selfVoteTargetId, Is.EqualTo(before.selfVoteTargetId));
                Assert.That(after.selfVoteAbstained, Is.EqualTo(before.selfVoteAbstained));
                Assert.That(after.wolfAllyIds, Is.EqualTo(before.wolfAllyIds));
                Assert.That(after.winner, Is.EqualTo(before.winner));
                Assert.That(after.winnerIds, Is.EqualTo(before.winnerIds));
                Assert.That(after.events.Select(e => e.sequence + ":" + e.kind + ":" + e.text),
                    Is.EqualTo(before.events.Select(e => e.sequence + ":" + e.kind + ":" + e.text)));
                Assert.That(after.privateFacts.Select(f => f.key + ":" + f.playerId + ":" + f.roleId),
                    Is.EqualTo(before.privateFacts.Select(f => f.key + ":" + f.playerId + ":" + f.roleId)));
                Assert.That(after.action?.kind, Is.EqualTo(before.action?.kind));
                Assert.That(after.action?.submitted, Is.EqualTo(before.action?.submitted));
                Assert.That(after.action?.submittedChoice, Is.EqualTo(before.action?.submittedChoice));
                Assert.That(after.action?.submittedTargetIds, Is.EqualTo(before.action?.submittedTargetIds));
                Assert.That(after.action?.allowedTargetIds, Is.EqualTo(before.action?.allowedTargetIds));
                Assert.That(after.verdict?.result, Is.EqualTo(before.verdict?.result));
                Assert.That(after.verdict?.eliminatedPlayerId, Is.EqualTo(before.verdict?.eliminatedPlayerId));
            }
            if (requestedPhase != "lobby" && requestedPhase != "finished")
            {
                var deadline = game.View.deadlineUnixMs;
                game.Now = deadline; game.Room.Tick(deadline); restored.Tick(deadline);
                foreach (var id in game.Ids)
                {
                    Assert.That(restored.StateFor(id, deadline).phase, Is.EqualTo(game.For(id).phase));
                    Assert.That(restored.StateFor(id, deadline).turn, Is.EqualTo(game.For(id).turn));
                    Assert.That(restored.StateFor(id, deadline).players.Select(p => p.alive), Is.EqualTo(game.For(id).players.Select(p => p.alive)));
                }
            }
        }

        [TestCase(6)]
        [TestCase(7)]
        [TestCase(12)]
        [TestCase(24)]
        public void DefaultCompositionProducesExactlyOneSupportedCardPerReadyPlayer(int count)
        {
            var room = new WerewolfRoom("DEFAULT", new RoomSettings(), 42);
            for (var i = 0; i < count; i++)
            {
                Ok(room.AddPlayer("p" + i, "Personne " + i, i % 6));
                Ok(room.Apply("p" + i, new RoomCommand { kind = "ready", ready = true, commandId = "ready" + i }, 0));
            }
            var view = room.StateFor("p0", 0);
            Assert.That(view.composition.Sum(c => c.count), Is.EqualTo(count));
            Assert.That(view.canStart, Is.True);
            Ok(room.Apply("p0", new RoomCommand { kind = "start", commandId = "start" }, 0));
            var roles = Enumerable.Range(0, count).Select(i => room.StateFor("p" + i, 0).selfRoleId).ToArray();
            foreach (var entry in view.composition) Assert.That(roles.Count(role => role == entry.roleId), Is.EqualTo(entry.count));
            Assert.That(room.StateFor("p0", 0).phase, Is.EqualTo("distribution"));
            if (count == 24) Fails(room.AddPlayer("p24", "Trop tard", 0), "JOIN_CLOSED");
        }

        [Test]
        public void HostAndReadinessAreEnforcedAndNewConfigurationNeedsFreshConsent()
        {
            var game = new Game(BasicDeck);
            Fails(game.Send(game.Ids[1], "start"), "HOST_ONLY");
            Fails(game.Send(game.Host, "start"), "NOT_READY");
            foreach (var id in game.Ids)
            {
                var command = game.Command(id, "ready"); command.ready = true;
                Ok(game.Room.Apply(id, command, game.Now));
            }
            var configure = game.Command(game.Host, "configure"); configure.settings = new RoomSettings { discussionSeconds = 30 };
            Ok(game.Room.Apply(game.Host, configure, game.Now));
            Assert.That(game.View.players.All(p => !p.ready), Is.True);
            Assert.That(game.View.canStart, Is.False);
            configure.settings.discussionSeconds = 999;
            game.Start(); // The caller cannot mutate settings retained by the server.
            Fails(game.Room.AddPlayer("late", "En retard", 0), "JOIN_CLOSED");
        }

        [Test]
        public void InvalidMembershipAndNamesNeverBecomePlayers()
        {
            var room = new WerewolfRoom("MEMBERS", new RoomSettings(), 42);
            Ok(room.AddPlayer("first", "Léa", 0));
            Fails(room.AddPlayer("first", "Max", 0), "PLAYER_EXISTS");
            Fails(room.AddPlayer("second", " léa ", 1), "NAME_TAKEN");
            Fails(room.AddPlayer("bad\n", "Max", 0), "INVALID_PLAYER");
            Fails(room.AddPlayer("second", " ", 1), "INVALID_NAME");
            Fails(room.AddPlayer("second", "Max", 6), "INVALID_AVATAR");
            Fails(room.Apply("outsider", new RoomCommand { kind = "start", commandId = "x" }, 0), "NOT_MEMBER");
            Assert.Throws<ArgumentException>(() => room.StateFor("outsider", 0));
            Assert.That(room.StateFor("first", 0).players.Length, Is.EqualTo(1));
        }

        [TestCase("white-wolf")]
        [TestCase("thief")]
        [TestCase("elder")]
        [TestCase("raven")]
        [TestCase("wild-child")]
        [TestCase("piper")]
        [TestCase("fox")]
        [TestCase("scapegoat")]
        public void IllustratedRolesWithoutAnEngineAreRejectedRatherThanSilentlyActivated(string role)
        {
            var game = new Game(BasicDeck);
            var command = game.Command(game.Host, "configure");
            command.settings = new RoomSettings { composition = new[]
            { new NetworkRoleCount { roleId = "werewolf", count = 1 }, new NetworkRoleCount { roleId = role, count = 1 }, new NetworkRoleCount { roleId = "villager", count = 4 } } };
            Fails(game.Room.Apply(game.Host, command, game.Now), "UNSUPPORTED_ROLE");
            Assert.That(game.View.composition.All(entry => entry.roleId != role), Is.True);
        }

        [Test]
        public void RemotePetiteFilleAndIncorrectDeckSizesCannotStart()
        {
            var game = new Game(BasicDeck);
            var command = game.Command(game.Host, "configure");
            command.settings = new RoomSettings { mode = "remote", composition = new[]
            { new NetworkRoleCount { roleId = "werewolf", count = 1 }, new NetworkRoleCount { roleId = "little-girl", count = 1 }, new NetworkRoleCount { roleId = "villager", count = 4 } } };
            Fails(game.Room.Apply(game.Host, command, game.Now), "PHYSICAL_ROLE");
            command = game.Command(game.Host, "configure");
            command.settings = new RoomSettings { composition = new[]
            { new NetworkRoleCount { roleId = "werewolf", count = 1 }, new NetworkRoleCount { roleId = "villager", count = 6 } } };
            Ok(game.Room.Apply(game.Host, command, game.Now));
            Fails(game.Send(game.Host, "start"), "DECK_SIZE");
        }

        [Test]
        public void CommandsAreIdempotentButPayloadChangesAndStaleTurnsAreRejected()
        {
            var game = new Game(BasicDeck); game.Start();
            var seer = game.ReachAction("seer");
            var command = game.Command(seer, "act", "pass");
            Ok(game.Room.Apply(seer, command, game.Now));
            Ok(game.Room.Apply(seer, command, game.Now));
            command.choice = "inspect"; command.targetIds = new[] { game.One("werewolf") };
            Fails(game.Room.Apply(seer, command, game.Now), "COMMAND_CONFLICT");
            command.choice = "pass"; command.targetIds = Array.Empty<string>();
            game.CloseStep();
            var currentTurn = game.View.turn;
            Ok(game.Room.Apply(seer, command, game.Now));
            Assert.That(game.View.turn, Is.EqualTo(currentTurn));
            command.commandId = "new-but-old-turn";
            Fails(game.Room.Apply(seer, command, game.Now), "STALE_TURN");
            Assert.That(game.For(game.One("werewolf")).action.submitted, Is.False);
        }

        [Test]
        public void NightKeepsItsMinimumWindowAndAnOutageDoesNotInventAWholeGame()
        {
            var game = new Game(BasicDeck); game.Start();
            var seer = game.ReachAction("seer");
            var turn = game.View.turn;
            Fails(game.Send(game.Host, "advance"), "STEP_PENDING");
            Ok(game.Send(seer, "act", "pass"));
            game.Now += 2999; game.Room.Tick(game.Now);
            Assert.That(game.View.turn, Is.EqualTo(turn));
            Assert.That(game.View.canAdvance, Is.False);
            game.Now++; game.Room.Tick(game.Now);
            Assert.That(game.View.turn, Is.GreaterThan(turn));
            var nextTurn = game.View.turn;
            game.Now += 3600000; game.Room.Tick(game.Now);
            Assert.That(game.View.turn, Is.EqualTo(nextTurn + 1));
            Assert.That(game.View.phase, Is.EqualTo("dawn"));
            Assert.That(game.View.players.All(p => p.alive), Is.True, "An absent wolf passes instead of getting an invented target.");
        }

        [Test]
        public void SeerLearnsTheExactCardAndOnlyTheRecipientGetsThatFact()
        {
            var game = new Game(BasicDeck); game.Start();
            var seer = game.ReachAction("seer"); var wolf = game.One("werewolf");
            Fails(game.Send(seer, "act", "inspect", seer), "INVALID_TARGET");
            Ok(game.Send(seer, "act", "inspect", wolf));
            Fails(game.Send(seer, "act", "pass"), "ALREADY_SUBMITTED");
            game.CloseStep();
            var fact = game.For(seer).privateFacts.Single();
            Assert.That(fact.playerId, Is.EqualTo(wolf)); Assert.That(fact.roleId, Is.EqualTo("werewolf"));
            foreach (var id in game.Ids.Where(id => id != seer)) Assert.That(game.For(id).privateFacts, Is.Empty);
            Assert.That(game.For(seer).players.All(p => p.revealedRoleId == null), Is.True);
            fact.roleId = "villager";
            Assert.That(game.For(seer).privateFacts.Single().roleId, Is.EqualTo("werewolf"));
        }

        [Test]
        public void RoleSecretsWolfAlliesAndDetachedSnapshotsHaveSeparateRecipients()
        {
            var game = new Game("werewolf", "werewolf", "villager", "villager", "villager", "villager"); game.Start();
            var wolves = game.WithRole("werewolf"); var villager = game.WithRole("villager")[0];
            var publicView = game.For(villager); var wolfView = game.For(wolves[0]);
            Assert.That(publicView.wolfAllyIds, Is.Empty); Assert.That(publicView.action, Is.Null);
            Assert.That(publicView.players.All(p => p.revealedRoleId == null), Is.True);
            Assert.That(wolfView.wolfAllyIds, Is.EqualTo(new[] { wolves[1] }));
            Ok(game.Send(wolves[1], "act", "kill", villager));
            Assert.That(game.For(wolves[0]).action.privateInfo, Does.Contain(game.Player(villager).displayName));
            Assert.That(game.For(villager).privateFacts, Is.Empty);
            publicView.players[0].alive = false; publicView.players[0].revealedRoleId = "werewolf";
            publicView.composition[0].count = 999; wolfView.wolfAllyIds[0] = "impostor";
            publicView.settings.nightSeconds = 120;
            publicView.settings.composition[0].count = 999;
            Assert.That(game.View.players.All(p => p.alive && p.revealedRoleId == null), Is.True);
            Assert.That(game.View.composition.Sum(c => c.count), Is.EqualTo(6));
            Assert.That(game.View.settings.nightSeconds, Is.EqualTo(10));
            Assert.That(game.View.settings.composition.Sum(c => c.count), Is.EqualTo(6));
            Assert.That(game.For(wolves[0]).wolfAllyIds, Is.EqualTo(new[] { wolves[1] }));
        }

        [Test]
        public void ACompleteVillageVictoryRevealsCardsAndResetRemovesAllOldSecrets()
        {
            var game = new Game(false, BasicDeck); game.Start();
            var seer = game.ReachAction("seer"); var wolf = game.One("werewolf"); var victim = game.WithRole("villager")[0];
            Ok(game.Send(seer, "act", "inspect", wolf)); game.CloseStep();
            game.WolvesChoose(victim);
            Assert.That(game.Player(victim).alive, Is.False);
            Assert.That(game.Player(victim).revealedRoleId, Is.Null);
            Assert.That(game.View.events.Single(e => e.kind == "elimination").text, Does.Not.Contain("Son rôle"));
            game.OpenVote();
            Fails(game.Send(victim, "vote", "vote", wolf), "NO_VOTE");
            game.VoteOut(wolf);
            Assert.That(game.View.phase, Is.EqualTo("finished")); Assert.That(game.View.winner, Is.EqualTo("village"));
            Assert.That(game.View.winnerIds, Does.Contain(victim), "A dead villager shares the team victory.");
            Assert.That(game.View.players.All(p => p.revealedRoleId != null), Is.True);
            var previousTurn = game.View.turn;
            Fails(game.Send(game.Ids[1], "reset"), "HOST_ONLY");
            Ok(game.Send(game.Host, "reset"));
            Assert.That(game.View.phase, Is.EqualTo("lobby")); Assert.That(game.View.turn, Is.GreaterThan(previousTurn));
            foreach (var id in game.Ids)
            {
                var view = game.For(id);
                Assert.That(view.selfRoleId, Is.Null); Assert.That(view.privateFacts, Is.Empty); Assert.That(view.wolfAllyIds, Is.Empty);
                Assert.That(view.players.All(p => p.alive && !p.ready && p.revealedRoleId == null), Is.True);
            }
            Assert.That(game.View.events.Select(e => e.kind), Is.EqualTo(new[] { "reset" }));
        }

        [Test]
        public void WolvesDoNotWinAtParityAndMustEliminateTheFinalNonWolf()
        {
            var game = new Game("werewolf", "werewolf", "villager", "villager", "villager", "villager"); game.Start();
            var villagers = game.WithRole("villager");
            game.WolvesChoose(villagers[0]); game.OpenVote(); game.VoteOut(villagers[1]);
            Assert.That(game.View.phase, Is.EqualTo("night")); Assert.That(game.View.winner, Is.Null);
            Assert.That(game.View.players.Count(p => p.alive), Is.EqualTo(4), "Two wolves and two villagers is not an automatic victory.");
            game.WolvesChoose(villagers[2]);
            Assert.That(game.View.phase, Is.EqualTo("dawn")); Assert.That(game.View.winner, Is.Null);
            game.OpenVote(); game.VoteOut(villagers[3]);
            Assert.That(game.View.winner, Is.EqualTo("wolves"));
            Assert.That(game.View.winnerIds, Is.EquivalentTo(game.WithRole("werewolf")));
        }

        [Test]
        public void GuardProtectionBlocksWolvesButTheSameTargetCannotBeProtectedNextNight()
        {
            var game = new Game("werewolf", "guard", "villager", "villager", "villager", "villager"); game.Start();
            var guard = game.ReachAction("guard"); var target = game.WithRole("villager")[0];
            Assert.That(game.For(guard).action.allowedTargetIds, Does.Contain(guard));
            Ok(game.Send(guard, "act", "protect", target)); game.CloseStep(); game.WolvesChoose(target);
            Assert.That(game.View.players.All(p => p.alive), Is.True);
            game.NoEliminationDay();
            Assert.That(game.For(guard).action.allowedTargetIds, Does.Not.Contain(target));
            Assert.That(game.For(guard).action.privateInfo, Does.Contain(game.Player(target).displayName));
            foreach (var id in game.Ids.Where(id => id != guard))
                Assert.That(game.For(id).action?.privateInfo, Is.Null, "The guard's previous choice is private.");
            Fails(game.Send(guard, "act", "protect", target), "INVALID_TARGET");
            Ok(game.Send(guard, "act", "pass")); game.CloseStep(); game.WolvesChoose(null);
            game.NoEliminationDay();
            Assert.That(game.For(guard).action.allowedTargetIds, Does.Contain(target), "Passing a night breaks the consecutive-target restriction.");
        }

        [Test]
        public void GuardDoesNotBlockPoisonAndTheWitchCannotSpendHealingOnABlockedAttack()
        {
            var game = new Game("werewolf", "guard", "witch", "villager", "villager", "villager"); game.Start();
            var guard = game.ReachAction("guard"); var victim = game.WithRole("villager")[0];
            Ok(game.Send(guard, "act", "protect", victim)); game.CloseStep(); game.WolvesChoose(victim);
            var witch = game.One("witch");
            Assert.That(game.For(witch).action.choices, Does.Not.Contain("save"));
            Assert.That(game.For(witch).action.choices, Does.Not.Contain("both"));
            Fails(game.Send(witch, "act", "save"), "INVALID_CHOICE");
            Ok(game.Send(witch, "act", "poison", victim)); game.CloseStep();
            Assert.That(game.Player(victim).alive, Is.False);
        }

        [Test]
        public void WitchCanUseBothPotionsInOneNightButNeverAgainAfterTheyAreSpent()
        {
            var game = new Game("werewolf", "witch", "villager", "villager", "villager", "villager"); game.Start();
            var wolfVictim = game.WithRole("villager")[0]; var poisonVictim = game.WithRole("villager")[1]; var witch = game.One("witch");
            game.WolvesChoose(wolfVictim);
            Assert.That(game.For(witch).action.choices, Does.Contain("both"));
            Assert.That(game.For(witch).action.privateInfo, Does.Contain(game.Player(wolfVictim).displayName));
            Fails(game.Send(witch, "act", "both", wolfVictim, poisonVictim), "INVALID_TARGET");
            Ok(game.Send(witch, "act", "both", poisonVictim)); game.CloseStep();
            Assert.That(game.Player(wolfVictim).alive, Is.True); Assert.That(game.Player(poisonVictim).alive, Is.False);
            game.NoEliminationDay(); game.WolvesChoose(null);
            Assert.That(game.View.nightStep, Is.EqualTo("witch"), "A missing public call must not disclose spent private potions.");
            Assert.That(game.For(witch).action, Is.Null);
            game.PassNight(); Assert.That(game.View.phase, Is.EqualTo("dawn"));
        }

        [Test]
        public void WitchKeepsTheUnusedPoisonAndCannotHealASecondTime()
        {
            var game = new Game("werewolf", "witch", "villager", "villager", "villager", "villager"); game.Start();
            var witch = game.One("witch"); var victims = game.WithRole("villager"); var wolf = game.One("werewolf");
            game.WolvesChoose(victims[0]); Ok(game.Send(witch, "act", "save")); game.CloseStep();
            Assert.That(game.Player(victims[0]).alive, Is.True);
            game.NoEliminationDay(); game.WolvesChoose(victims[1]);
            Assert.That(game.For(witch).action.choices, Is.EquivalentTo(new[] { "pass", "poison" }));
            Fails(game.Send(witch, "act", "save"), "INVALID_CHOICE");
            Ok(game.Send(witch, "act", "poison", wolf)); game.CloseStep();
            Assert.That(game.View.winner, Is.EqualTo("village"));
            Assert.That(game.Player(victims[1]).alive, Is.False);
        }

        [Test]
        public void WitchMayHealHerselfWithoutSelectingAPoisonTarget()
        {
            var game = new Game("werewolf", "witch", "villager", "villager", "villager", "villager"); game.Start();
            var witch = game.One("witch"); game.WolvesChoose(witch);
            Assert.That(game.For(witch).action.choices, Does.Contain("save"));
            Fails(game.Send(witch, "act", "save", witch), "INVALID_TARGET");
            Ok(game.Send(witch, "act", "save")); game.CloseStep();
            Assert.That(game.Player(witch).alive, Is.True);
            Assert.That(game.View.players.All(p => p.alive), Is.True);
        }

        [Test]
        public void PoisonStillKillsAPlayerWhoseWolfAttackWasHealedTheSameNight()
        {
            var game = new Game("werewolf", "witch", "villager", "villager", "villager", "villager"); game.Start();
            var witch = game.One("witch"); var victim = game.WithRole("villager")[0]; game.WolvesChoose(victim);
            Ok(game.Send(witch, "act", "both", victim)); game.CloseStep();
            Assert.That(game.Player(victim).alive, Is.False);
            Assert.That(game.View.events.Count(e => e.kind == "elimination"), Is.EqualTo(1));
        }

        [Test]
        public void ADisagreementInTheMeuteDoesNotInventATieBreakerVictim()
        {
            var game = new Game("werewolf", "werewolf", "villager", "villager", "villager", "villager"); game.Start();
            var wolves = game.WithRole("werewolf"); var villagers = game.WithRole("villager");
            Fails(game.Send(wolves[0], "act", "kill", wolves[1]), "INVALID_TARGET");
            Ok(game.Send(wolves[0], "act", "kill", villagers[0]));
            Ok(game.Send(wolves[1], "act", "kill", villagers[1])); game.CloseStep();
            Assert.That(game.View.phase, Is.EqualTo("dawn"));
            Assert.That(game.View.players.All(p => p.alive), Is.True);
        }

        [Test]
        public void PetiteFilleHasNoDigitalIdentitiesAndWolvesCanChangeTheirChoiceDuringHerWindow()
        {
            var game = new Game("werewolf", "werewolf", "little-girl", "villager", "villager", "villager"); game.Start();
            var wolves = game.WithRole("werewolf"); var girl = game.One("little-girl"); var victims = game.WithRole("villager");
            foreach (var wolf in wolves) Ok(game.Send(wolf, "act", "kill", victims[0]));
            var oldTurn = game.View.turn;
            game.Now += 3000; game.Room.Tick(game.Now);
            Assert.That(game.View.turn, Is.EqualTo(oldTurn), "Automatic progression must preserve her physical observation window.");
            Assert.That(game.View.canAdvance, Is.True);
            var observation = game.For(girl);
            Assert.That(observation.action.kind, Is.EqualTo("observe")); Assert.That(observation.action.choices, Is.Empty);
            Assert.That(observation.wolfAllyIds, Is.Empty); Assert.That(observation.privateFacts, Is.Empty);
            Assert.That(observation.players.All(p => p.revealedRoleId == null), Is.True);
            Fails(game.Send(girl, "act", "pass"), "NO_ACTION");
            foreach (var wolf in wolves) Ok(game.Send(wolf, "act", "kill", victims[1]));
            game.CloseStep();
            Assert.That(game.Player(victims[0]).alive, Is.True); Assert.That(game.Player(victims[1]).alive, Is.False);
        }

        [Test]
        public void PhysicalPetiteFilleRemainsVulnerableEvenWhenTheGuardChoosesHer()
        {
            var game = new Game("werewolf", "guard", "little-girl", "villager", "villager", "villager"); game.Start();
            var guard = game.ReachAction("guard"); var girl = game.One("little-girl");
            Ok(game.Send(guard, "act", "protect", girl)); game.CloseStep(); game.WolvesChoose(girl);
            Assert.That(game.Player(girl).alive, Is.False, "The published classic Salvateur exception applies to the physical Petite Fille.");
        }

        [Test]
        public void MissingPetiteFilleAndWolfActionsExpireWithoutInventingAnAttack()
        {
            var game = new Game("werewolf", "little-girl", "villager", "villager", "villager", "villager"); game.Start();
            game.Now = game.View.deadlineUnixMs; game.Room.Tick(game.Now);
            Assert.That(game.View.phase, Is.EqualTo("dawn"));
            Assert.That(game.View.players.All(p => p.alive), Is.True);
        }

        [Test]
        public void NightNarrationCallsTheRoleWithoutIdentifyingItsPlayer()
        {
            var game = new Game(BasicDeck); game.Start();
            Assert.That(game.View.nightStep, Is.EqualTo("seer"));
            Assert.That(game.View.statusMessage, Does.Contain("La Voyante se réveille"));
            foreach (var id in game.Ids)
            {
                Assert.That(game.For(id).statusMessage, Is.EqualTo(game.View.statusMessage));
                Assert.That(game.View.statusMessage, Does.Not.Contain(game.Player(id).displayName));
            }
            game.PassNight(); Assert.That(game.View.nightStep, Is.Null);
        }

        [Test]
        public void HiddenDeadRolesRetainTheirPublicCallWithoutOfferingThemAnAction()
        {
            var game = new Game(false, BasicDeck); game.Start();
            var seer = game.One("seer"); game.PassCurrentNightStep(); game.WolvesChoose(seer); game.NoEliminationDay();
            Assert.That(game.Player(seer).revealedRoleId, Is.Null);
            Assert.That(game.View.nightStep, Is.EqualTo("seer"));
            Assert.That(game.For(seer).action, Is.Null);
            Assert.That(game.Ids.All(id => game.For(id).action == null), Is.True);
            game.CloseStep(); Assert.That(game.View.nightStep, Is.EqualTo("werewolf"));
        }

        [Test]
        public void ADeadHunterCanReverseTheOutcomeBeforeVictoryIsEvaluated()
        {
            var game = new Game("werewolf", "hunter", "villager", "villager", "villager", "villager"); game.Start();
            var hunter = game.One("hunter"); var wolf = game.One("werewolf");
            game.WolvesChoose(hunter);
            Assert.That(game.View.phase, Is.EqualTo("reaction")); Assert.That(game.View.winner, Is.Null);
            Assert.That(game.Player(hunter).alive, Is.False); Assert.That(game.For(hunter).action.kind, Is.EqualTo("hunter"));
            Fails(game.Send(wolf, "act", "shoot", hunter), "NO_ACTION");
            Ok(game.Send(hunter, "act", "shoot", wolf));
            Assert.That(game.View.phase, Is.EqualTo("finished")); Assert.That(game.View.winner, Is.EqualTo("village"));
            Assert.That(game.View.winnerIds, Does.Contain(hunter));
        }

        [Test]
        public void HunterTimeoutPassesAndDoesNotFabricateAShot()
        {
            var game = new Game("werewolf", "hunter", "villager", "villager", "villager", "villager"); game.Start();
            var hunter = game.One("hunter"); game.WolvesChoose(hunter);
            game.Now = game.View.deadlineUnixMs; game.Room.Tick(game.Now);
            Assert.That(game.View.phase, Is.EqualTo("dawn"));
            Assert.That(game.View.players.Count(p => !p.alive), Is.EqualTo(1));
            Assert.That(game.View.events.Count(e => e.kind == "reaction-pass"), Is.EqualTo(1));
        }

        [Test]
        public void TheLastHunterAndWolfCanBothDieBeforeTheDrawIsDecided()
        {
            var game = new Game("werewolf", "hunter", "villager", "villager", "villager", "villager"); game.Start();
            var villagers = game.WithRole("villager"); var hunter = game.One("hunter"); var wolf = game.One("werewolf");
            game.WolvesChoose(villagers[0]); game.OpenVote(); game.VoteOut(villagers[1]);
            game.WolvesChoose(villagers[2]); game.OpenVote(); game.VoteOut(villagers[3]);
            Assert.That(game.View.players.Count(p => p.alive), Is.EqualTo(2));
            Assert.That(game.View.phase, Is.EqualTo("night"));
            game.WolvesChoose(hunter);
            Assert.That(game.View.phase, Is.EqualTo("reaction")); Assert.That(game.View.winner, Is.Null);
            Ok(game.Send(hunter, "act", "shoot", wolf));
            Assert.That(game.View.winner, Is.EqualTo("draw")); Assert.That(game.View.winnerIds, Is.Empty);
            Assert.That(game.View.players.All(p => !p.alive), Is.True);
        }

        [Test]
        public void MixedLoversDyingTogetherCannotWinTheirOriginalTeamsAndHunterStillActs()
        {
            var game = new Game("werewolf", "hunter", "cupid", "villager", "villager", "villager"); game.Start();
            var cupid = game.ReachAction("cupid"); var hunter = game.One("hunter"); var wolf = game.One("werewolf");
            Ok(game.Send(cupid, "act", "link", wolf, hunter)); game.CloseStep(); game.WolvesChoose(hunter);
            Assert.That(game.View.phase, Is.EqualTo("reaction")); Assert.That(game.View.winner, Is.Null);
            Assert.That(game.Player(wolf).alive, Is.False); Assert.That(game.Player(hunter).alive, Is.False);
            Ok(game.Send(hunter, "act", "shoot", game.WithRole("villager")[0]));
            Assert.That(game.View.winner, Is.EqualTo("village"));
            Assert.That(game.View.winnerIds, Does.Not.Contain(hunter)); Assert.That(game.View.winnerIds, Does.Not.Contain(wolf));
        }

        [Test]
        public void LoverDeathTriggersTheHuntersReactionAndOnlyLoversLearnTheirLink()
        {
            var game = new Game("werewolf", "hunter", "cupid", "villager", "villager", "villager"); game.Start();
            var cupid = game.ReachAction("cupid"); var hunter = game.One("hunter"); var partner = game.WithRole("villager")[0];
            Fails(game.Send(cupid, "act", "link", hunter, hunter), "INVALID_TARGET");
            Ok(game.Send(cupid, "act", "link", hunter, partner)); game.CloseStep();
            Assert.That(game.For(hunter).privateFacts.Single().playerId, Is.EqualTo(partner));
            Assert.That(game.For(partner).privateFacts.Single().playerId, Is.EqualTo(hunter));
            Assert.That(game.For(game.One("werewolf")).privateFacts, Is.Empty);
            Assert.That(game.For(hunter).privateFacts.Single().roleId, Is.Null, "Cupid does not disclose the partner's card.");
            game.WolvesChoose(partner);
            Assert.That(game.Player(partner).alive, Is.False); Assert.That(game.Player(hunter).alive, Is.False);
            Assert.That(game.View.phase, Is.EqualTo("reaction"));
            Ok(game.Send(hunter, "act", "shoot", game.One("werewolf")));
            Assert.That(game.View.winner, Is.EqualTo("village"));
            Assert.That(game.View.events.Count(e => e.kind == "elimination"), Is.EqualTo(3));
        }

        [Test]
        public void LoversReceiveAnEightSecondRecognitionCallWithoutPubliclyIdentifyingTheirPair()
        {
            var game = new Game("werewolf", "cupid", "villager", "villager", "villager", "villager"); game.Start();
            var cupid = game.ReachAction("cupid"); var pair = game.WithRole("villager").Take(2).ToArray();
            Ok(game.Send(cupid, "act", "link", pair)); game.CloseStep();
            var view = game.View; var turn = view.turn;
            Assert.That(view.nightStep, Is.EqualTo("lovers"));
            Assert.That(view.deadlineUnixMs - game.Now, Is.EqualTo(8000));
            Assert.That(view.statusMessage, Does.Contain("Les amoureux se reconnaissent"));
            foreach (var id in game.Ids)
            {
                var personal = game.For(id);
                Assert.That(personal.statusMessage, Is.EqualTo(view.statusMessage));
                Assert.That(personal.statusMessage, Does.Not.Contain(game.Player(id).displayName));
                Assert.That(personal.events.All(e => e.kind != "lover" && e.kind != "cupid-choice"), Is.True);
                if (pair.Contains(id))
                {
                    Assert.That(personal.action.kind, Is.EqualTo("observe"));
                    Assert.That(personal.action.privateInfo, Does.Contain(game.Player(pair.Single(other => other != id)).displayName));
                    Assert.That(personal.action.allowedTargetIds, Is.Empty);
                    Assert.That(personal.action.choices, Is.Empty);
                }
                else Assert.That(personal.action, Is.Null);
            }
            game.Now += 3000; game.Room.Tick(game.Now);
            Assert.That(game.View.turn, Is.EqualTo(turn)); Assert.That(game.View.canAdvance, Is.True);
            game.Now += 4999; game.Room.Tick(game.Now); Assert.That(game.View.turn, Is.EqualTo(turn));
            game.Now++; game.Room.Tick(game.Now);
            Assert.That(game.View.turn, Is.GreaterThan(turn)); Assert.That(game.View.nightStep, Is.EqualTo("werewolf"));
        }

        [Test]
        public void CupidPassingStillProducesTheSamePublicRecognitionWindowAndHostMayCloseAfterThreeSeconds()
        {
            var game = new Game("werewolf", "cupid", "villager", "villager", "villager", "villager"); game.Start();
            var cupid = game.ReachAction("cupid"); Ok(game.Send(cupid, "act", "pass")); game.CloseStep();
            Assert.That(game.View.nightStep, Is.EqualTo("lovers"));
            Assert.That(game.View.deadlineUnixMs - game.Now, Is.EqualTo(8000));
            Assert.That(game.Ids.All(id => game.For(id).action == null && game.For(id).privateFacts.Length == 0), Is.True);
            Fails(game.Send(game.Host, "advance"), "STEP_PENDING");
            var oldTurn = game.View.turn;
            game.Now += 3000; game.Room.Tick(game.Now);
            Assert.That(game.View.turn, Is.EqualTo(oldTurn));
            Ok(game.Send(game.Host, "advance"));
            Assert.That(game.View.nightStep, Is.EqualTo("werewolf"));
        }

        [Test]
        public void MixedLoversWinOnlyWhenTheyAreTheFinalTwoSurvivors()
        {
            var game = new Game("werewolf", "cupid", "villager", "villager", "villager", "villager"); game.Start();
            var cupid = game.ReachAction("cupid"); var wolf = game.One("werewolf"); var villagers = game.WithRole("villager");
            Ok(game.Send(cupid, "act", "link", wolf, villagers[0])); game.CloseStep();
            game.WolvesChoose(villagers[1]); game.OpenVote(); game.VoteOut(villagers[2]);
            Assert.That(game.View.phase, Is.EqualTo("night")); Assert.That(game.View.winner, Is.Null);
            game.WolvesChoose(villagers[3]);
            Assert.That(game.View.winner, Is.Null, "The remaining Cupid must still be eliminated.");
            game.OpenVote(); game.VoteOut(cupid);
            Assert.That(game.View.winner, Is.EqualTo("lovers"));
            Assert.That(game.View.winnerIds, Is.EquivalentTo(new[] { wolf, villagers[0] }));
        }

        [Test]
        public void TiedVotesHaveOneRestrictedRunoffThenNoEliminationAndVotesStaySecretUntilClosure()
        {
            var game = new Game("werewolf", "villager", "villager", "villager", "villager", "villager"); game.Start();
            game.WolvesChoose(null); game.OpenVote();
            var a = game.Ids[0]; var b = game.Ids[1];
            Func<string, string> split = id => id == a || id == game.Ids[4] || id == game.Ids[5] ? b : a;
            Ok(game.Send(a, "vote", "vote", b));
            Assert.That(game.View.events.All(e => e.kind != "vote"), Is.True);
            Fails(game.Send(a, "vote", "pass"), "ALREADY_SUBMITTED");
            foreach (var id in game.Ids.Skip(1)) Ok(game.Send(id, "vote", "vote", split(id)));
            game.FinishVerdict();
            Assert.That(game.View.phase, Is.EqualTo("vote"));
            var outsider = game.Ids[2];
            Assert.That(game.For(outsider).action.allowedTargetIds, Is.EquivalentTo(new[] { a, b }));
            Fails(game.Send(outsider, "vote", "vote", game.Ids[3]), "INVALID_TARGET");
            game.Votes(split);
            Assert.That(game.View.phase, Is.EqualTo("night")); Assert.That(game.View.day, Is.EqualTo(2));
            Assert.That(game.View.players.All(p => p.alive), Is.True);
            Assert.That(game.View.events.Count(e => e.kind == "no-elimination"), Is.EqualTo(1));
            Assert.That(game.View.events.Count(e => e.kind == "vote"), Is.EqualTo(12));
        }

        [Test]
        public void ARunoffCanResolveTheTieAndCannotBeReplayedWithTheOldTurn()
        {
            var game = new Game("werewolf", "villager", "villager", "villager", "villager", "villager"); game.Start();
            game.WolvesChoose(null); game.OpenVote();
            var wolf = game.One("werewolf"); var candidate = game.WithRole("villager")[0];
            var others = game.Ids.Where(id => id != wolf && id != candidate).ToArray();
            var oldTurn = game.View.turn;
            game.Votes(id => id == wolf || id == others[2] || id == others[3] ? candidate : wolf);
            Assert.That(game.View.phase, Is.EqualTo("vote")); Assert.That(game.View.turn, Is.GreaterThan(oldTurn));
            var stale = game.Command(others[0], "vote", "vote", candidate); stale.turn = oldTurn;
            Fails(game.Room.Apply(others[0], stale, game.Now), "STALE_TURN");
            game.VoteOut(wolf);
            Assert.That(game.View.winner, Is.EqualTo("village"));
        }

        [Test]
        public void VoteDeadlineRecordsOnlyAbstentionsWhenNobodyResponds()
        {
            var game = new Game("werewolf", "villager", "villager", "villager", "villager", "villager"); game.Start();
            game.WolvesChoose(null); game.OpenVote();
            game.Now = game.View.deadlineUnixMs; game.Room.Tick(game.Now);
            game.FinishVerdict();
            Assert.That(game.View.phase, Is.EqualTo("night"));
            Assert.That(game.View.players.All(p => p.alive), Is.True);
            var votes = game.View.events.Where(e => e.kind == "vote").ToArray();
            Assert.That(votes.Length, Is.EqualTo(6)); Assert.That(votes.All(e => e.text.EndsWith(" s’abstient.", StringComparison.Ordinal)), Is.True);
        }
    }
}
