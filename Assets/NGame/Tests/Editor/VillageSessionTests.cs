using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NGame.Core.Tests
{
    public sealed class VillageSessionTests
    {
        [Test]
        public void NewSessionStartsAtNightWithoutExposingTheLocalRole()
        {
            var session = new VillageSession();
            Assert.That(session.Phase, Is.EqualTo(VillagePhase.Night));
            Assert.That(session.RoleVisible, Is.False);
            Assert.That(session.LocalRoleName, Is.Null);
            Assert.That(session.SelectedPlayerId, Is.Null);
            Assert.That(session.ConfirmedPlayerId, Is.Null);
        }

        [Test]
        public void RevealExposesOnlyTheLocalDemoRoleAndConcealMasksSynchronously()
        {
            var session = new VillageSession();
            Assert.That(session.RevealSecret(), Is.True);
            Assert.That(session.RoleVisible, Is.True);
            Assert.That(session.LocalRoleName, Is.EqualTo("Loup-Garou"));
            Assert.That(session.ConcealSecret(), Is.True);
            Assert.That(session.RoleVisible, Is.False);
            Assert.That(session.LocalRoleName, Is.Null);
        }

        [TestCase(VillagePhase.Dawn)]
        [TestCase(VillagePhase.Vote)]
        public void NonNightPhasesCannotRevealTheLocalRole(VillagePhase phase)
        {
            var session = new VillageSession();
            session.MoveTo(phase);
            Assert.That(session.RevealSecret(), Is.False);
            Assert.That(session.RoleVisible, Is.False);
            Assert.That(session.LocalRoleName, Is.Null);
        }

        [Test]
        public void MovingPhaseMasksAndConcealingAgainNeverRestoresTheRole()
        {
            var session = new VillageSession();
            session.RevealSecret();
            session.MoveTo(VillagePhase.Dawn);
            session.ConcealSecret();
            session.MoveTo(VillagePhase.Night);
            Assert.That(session.RoleVisible, Is.False);
            Assert.That(session.LocalRoleName, Is.Null);
        }

        [Test]
        public void ReenteringNightStillConcealsAnExposedRole()
        {
            var session = new VillageSession();
            session.RevealSecret();
            Assert.That(session.MoveTo(VillagePhase.Night), Is.True);
            Assert.That(session.RoleVisible, Is.False);
        }

        [Test]
        public void PlayerChoicesAreTheSixImmutableDecorativeParticipants()
        {
            var session = new VillageSession();
            var ids = new List<string>();
            var names = new List<string>();
            foreach (var player in session.Players)
            {
                ids.Add(player.Id);
                names.Add(player.Name);
            }
            Assert.That(ids, Is.EqualTo(new[] { "lea", "max", "chloe", "hugo", "emma", "lucas" }));
            Assert.That(names, Is.EqualTo(new[] { "Léa", "Max", "Chloé", "Hugo", "Emma", "Lucas" }));
            var list = (IList<DecorativePlayer>)session.Players;
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
        }

        [Test]
        public void SelectionAndConfirmationAreUnavailableOutsideVoting()
        {
            var session = new VillageSession();
            Assert.That(session.SelectPlayer("lea"), Is.False);
            Assert.That(session.ConfirmSelection(), Is.False);
            session.MoveTo(VillagePhase.Dawn);
            Assert.That(session.SelectPlayer("lea"), Is.False);
            Assert.That(session.SelectedPlayerId, Is.Null);
            Assert.That(session.ConfirmedPlayerId, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("missing")]
        [TestCase("LEA")]
        public void UnknownPlayerCannotReplaceAValidSelection(string invalidId)
        {
            var session = new VillageSession();
            session.MoveTo(VillagePhase.Vote);
            session.SelectPlayer("lea");
            Assert.That(session.SelectPlayer(invalidId), Is.False);
            Assert.That(session.SelectedPlayerId, Is.EqualTo("lea"));
        }

        [Test]
        public void PhaseNavigationDiscardsAPendingSelection()
        {
            var session = new VillageSession();
            session.MoveTo(VillagePhase.Vote);
            session.SelectPlayer("chloe");
            session.MoveTo(VillagePhase.Night);
            session.MoveTo(VillagePhase.Vote);
            Assert.That(session.SelectedPlayerId, Is.Null);
            Assert.That(session.ConfirmSelection(), Is.False);
        }

        [Test]
        public void ReenteringVoteAlsoDiscardsAPendingSelection()
        {
            var session = new VillageSession();
            session.MoveTo(VillagePhase.Vote);
            session.SelectPlayer("max");
            session.MoveTo(VillagePhase.Vote);
            Assert.That(session.SelectedPlayerId, Is.Null);
            Assert.That(session.ConfirmedPlayerId, Is.Null);
        }

        [Test]
        public void ConfirmationRequiresASelectionAndConsumesIt()
        {
            var session = new VillageSession();
            session.MoveTo(VillagePhase.Vote);
            Assert.That(session.ConfirmSelection(), Is.False);
            Assert.That(session.Phase, Is.EqualTo(VillagePhase.Vote));
            session.SelectPlayer("emma");
            Assert.That(session.ConfirmSelection(), Is.True);
            Assert.That(session.Phase, Is.EqualTo(VillagePhase.VoteConfirmed));
            Assert.That(session.ConfirmedPlayerId, Is.EqualTo("emma"));
            Assert.That(session.SelectedPlayerId, Is.Null);
            Assert.That(session.RoleVisible, Is.False);
        }

        [Test]
        public void AConfirmedChoiceCannotBeChangedByNavigatingPhases()
        {
            var session = new VillageSession();
            session.MoveTo(VillagePhase.Vote);
            session.SelectPlayer("hugo");
            session.ConfirmSelection();
            session.MoveTo(VillagePhase.Night);
            session.MoveTo(VillagePhase.Dawn);
            session.MoveTo(VillagePhase.Vote);
            Assert.That(session.SelectPlayer("lucas"), Is.False);
            Assert.That(session.ConfirmSelection(), Is.False);
            Assert.That(session.ConfirmedPlayerId, Is.EqualTo("hugo"));
            Assert.That(session.SelectedPlayerId, Is.Null);
        }

        [Test]
        public void ResetMasksTheRoleAndStartsAFreshSelectableRound()
        {
            var session = new VillageSession();
            session.MoveTo(VillagePhase.Vote);
            session.SelectPlayer("hugo");
            session.ConfirmSelection();
            session.MoveTo(VillagePhase.Night);
            session.RevealSecret();
            session.Reset();
            Assert.That(session.Phase, Is.EqualTo(VillagePhase.Night));
            Assert.That(session.RoleVisible, Is.False);
            Assert.That(session.LocalRoleName, Is.Null);
            Assert.That(session.ConfirmedPlayerId, Is.Null);
            Assert.That(session.SelectedPlayerId, Is.Null);
            session.MoveTo(VillagePhase.Vote);
            Assert.That(session.SelectPlayer("lucas"), Is.True);
            Assert.That(session.ConfirmSelection(), Is.True);
            Assert.That(session.ConfirmedPlayerId, Is.EqualTo("lucas"));
        }

        [Test]
        public void ConfirmationCannotBeFabricatedByDirectPhaseNavigation()
        {
            var session = new VillageSession();
            Assert.Throws<InvalidOperationException>(() => session.MoveTo(VillagePhase.VoteConfirmed));
            Assert.That(session.Phase, Is.EqualTo(VillagePhase.Night));
            Assert.That(session.ConfirmedPlayerId, Is.Null);
        }

        [Test]
        public void UnknownPhaseIsRejectedWithoutCorruptingTheSession()
        {
            var session = new VillageSession();
            Assert.Throws<ArgumentOutOfRangeException>(() => session.MoveTo((VillagePhase)99));
            Assert.That(session.Phase, Is.EqualTo(VillagePhase.Night));
        }

        [Test]
        public void EventsObserveCompleteChangesAndNoOpsDoNotNotify()
        {
            var session = new VillageSession();
            var events = 0;
            session.Changed += () =>
            {
                events++;
                if (session.Phase != VillagePhase.Night) Assert.That(session.RoleVisible, Is.False);
            };
            session.Reset();
            session.ConcealSecret();
            session.SelectPlayer("lea");
            Assert.That(events, Is.Zero);
            session.RevealSecret();
            session.RevealSecret();
            session.MoveTo(VillagePhase.Vote);
            session.SelectPlayer("lea");
            session.SelectPlayer("lea");
            session.ConfirmSelection();
            session.ConfirmSelection();
            Assert.That(events, Is.EqualTo(4));
        }

        [Test]
        public void IndependentSessionsCannotShareARevealOrAChoice()
        {
            var first = new VillageSession();
            var second = new VillageSession();
            first.RevealSecret();
            first.MoveTo(VillagePhase.Vote);
            first.SelectPlayer("max");
            first.ConfirmSelection();
            Assert.That(second.Phase, Is.EqualTo(VillagePhase.Night));
            Assert.That(second.RoleVisible, Is.False);
            Assert.That(second.ConfirmedPlayerId, Is.Null);
        }
    }
}
