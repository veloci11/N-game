using System;
using System.Linq;
using System.Reflection;
using NGame.Core.Multiplayer;
using NGame.Presentation.Game;
using NUnit.Framework;
using UnityEngine;

namespace NGame.Presentation.Tests
{
    public sealed class PracticeGuidanceTests
    {
        private GameObject owner;
        private PracticeRoomClient client;
        private static readonly MethodInfo StepMethod = typeof(PracticeRoomClient).GetMethod("AdvancePractice", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ClockField = typeof(PracticeRoomClient).GetField("practiceTimeMs", BindingFlags.NonPublic | BindingFlags.Instance);

        [SetUp] public void SetUp()
        {
            owner = new GameObject("Deterministic real-engine practice lesson");
            client = owner.AddComponent<PracticeRoomClient>();
            client.CreateRoom("Apprenti", 0, new RoomSettings());
        }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(owner); }
        private void Step(int frames) { for (var i = 0; i < frames; i++) StepMethod.Invoke(client, new object[] { .2f }); }
        private double Clock => (double)ClockField.GetValue(client);
        private void Send(string kind, string choice = null, string[] targets = null)
        {
            client.Send(new RoomCommand { kind = kind, turn = client.View.turn, commandId = Guid.NewGuid().ToString("N"), choice = choice, targetIds = targets ?? Array.Empty<string>() });
            Assert.That(client.StatusError, Is.Null);
        }
        private void ReachDiscussion()
        {
            Send("start");
            Send("acknowledge-role");
            for (var frame = 0; frame < 1200 && client.View.phase != "discussion"; frame++)
            {
                var action = client.View.action;
                if (action != null && !action.submitted && action.kind != "observe")
                {
                    var choice = action.kind == "witch" ? action.choices.Contains("save") ? "save" : "pass"
                        : action.choices.FirstOrDefault(c => c != "pass") ?? "pass";
                    Send(action.kind == "vote" ? "vote" : "act", choice,
                        choice == "pass" || action.kind == "witch" ? null : action.allowedTargetIds.Take(action.targetCount).ToArray());
                }
                Step(1);
            }
            Assert.That(client.View.phase, Is.EqualTo("discussion"));
        }

        [Test] public void DistributionWaitsForLearnerInsteadOfExpiringTheirSecretCard()
        {
            Send("start");
            var clock = Clock;
            Step(600);
            Assert.That(client.View.phase, Is.EqualTo("distribution"));
            Assert.That(client.View.roleAcknowledged, Is.False);
            Assert.That(client.View.acknowledgedCount, Is.EqualTo(7));
            Assert.That(Clock, Is.EqualTo(clock));
            Assert.That(client.PracticeHint, Does.Contain("confirme"));
            Assert.That(client.View.players.Where(p => p.id != client.View.selfPlayerId).All(p => p.displayName.EndsWith(" · bot")), Is.True);
        }

        [Test] public void DiscussionHasNoChatAndWaitsForExplicitAdvance()
        {
            ReachDiscussion();
            client.OpenChannel("village");
            var clock = Clock;
            Step(400);
            Assert.That(client.View.phase, Is.EqualTo("discussion"));
            Assert.That(Clock, Is.EqualTo(clock));
            Assert.That(client.Communications.channels, Is.Empty);
            Assert.That(client.Messages, Is.Empty);
            Assert.That(client.PracticeHint, Does.Contain("Ouvrir le vote"));
            client.SendChat("village", "Pas de discussion avant le vote");
            Assert.That(client.ChatError, Is.Not.Null);
            Send("advance");
            Assert.That(client.View.phase, Is.EqualTo("vote"));
            client.OpenChannel("village");
            Assert.That(client.Messages, Is.Empty, "No bot text should have been generated during discussion.");
        }

        [Test] public void VoteHasThreeLabelledBotExamplesAndAGenericReplyOnlyDuringVoting()
        {
            ReachDiscussion();
            Send("advance");
            client.OpenChannel("village");
            Step(50);
            Assert.That(client.View.phase, Is.EqualTo("vote"));
            Assert.That(client.PracticeHint, Does.Contain("Chat"));
            Assert.That(client.Messages.Length, Is.EqualTo(3));
            Assert.That(client.Messages.All(m => m.channelId == "village" && m.text.StartsWith("[Bot · entraînement] ")), Is.True);
            Assert.That(client.PracticeHint.Length, Is.LessThanOrEqualTo(110));
            client.SendChat("village", "Pourquoi ce vote ?");
            Step(12);
            Assert.That(client.Messages.Last().text, Does.Contain("réponse d’entraînement générique"));
            Send("vote", "pass");
            Step(10);
            Assert.That(client.View.phase, Is.EqualTo("verdict"));
            Assert.That(client.Communications.channels, Is.Empty);
            Assert.That(client.Messages, Is.Empty);
            client.SendChat("village", "Pas après la clôture");
            Assert.That(client.ChatError, Is.Not.Null);
        }

        [Test] public void VoteCannotBePlayedForLearnerAndSubmittedAbstentionIsRespected()
        {
            ReachDiscussion();
            Send("advance");
            var clock = Clock;
            Step(400);
            Assert.That(client.View.phase, Is.EqualTo("vote"));
            Assert.That(client.View.action.submitted, Is.False);
            Assert.That(Clock, Is.EqualTo(clock));
            Send("vote", "pass");
            Step(10);
            Assert.That(client.View.verdict, Is.Not.Null);
            var record = client.View.verdict.votes.Single(v => v.playerId == client.View.selfPlayerId);
            Assert.That(record.abstained, Is.True);
        }

        [Test] public void FocusAndInactiveViewsPauseTheLocalClockAndHideSecrets()
        {
            var hidden = 0;
            client.SecretsMustHide += () => hidden++;
            var clock = Clock;
            owner.SendMessage("OnApplicationFocus", false);
            Step(100);
            Assert.That(Clock, Is.EqualTo(clock));
            owner.SendMessage("OnApplicationFocus", true);
            client.SetActive(false);
            Step(100);
            Assert.That(Clock, Is.EqualTo(clock));
            client.SetActive(true);
            Step(2);
            Assert.That(Clock, Is.GreaterThan(clock));
            Assert.That(hidden, Is.GreaterThanOrEqualTo(2));
        }
    }
}
