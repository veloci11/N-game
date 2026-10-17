using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NGame.Core.Tests
{
    public sealed class PartyComposerTests
    {
        [TestCase(6, 1, 1, 4)]
        [TestCase(7, 2, 1, 4)]
        [TestCase(8, 2, 1, 5)]
        [TestCase(9, 2, 1, 6)]
        [TestCase(10, 2, 1, 7)]
        [TestCase(11, 3, 1, 7)]
        [TestCase(12, 3, 1, 8)]
        public void ProposedCountsFitEverySupportedParty(int players, int wolves, int seers, int villagers)
        {
            var composition = PartyComposer.Recommend(players);
            Assert.That(composition.PlayerCount, Is.EqualTo(players));
            Assert.That(composition.Wolves, Is.EqualTo(wolves));
            Assert.That(composition.Seers, Is.EqualTo(seers));
            Assert.That(composition.Villagers, Is.EqualTo(villagers));
            Assert.That(composition.Wolves + composition.Seers + composition.Villagers, Is.EqualTo(players));
        }

        [TestCase(0)]
        [TestCase(5)]
        [TestCase(13)]
        [TestCase(int.MaxValue)]
        public void UnsupportedSizesAreRejectedRatherThanSilentlyClamped(int players)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PartyComposer.Recommend(players));
        }

        [Test]
        public void CompositionClearlyRemainsAnUntestedBalanceProposal()
        {
            for (var players = PartyComposer.MinimumPlayers; players <= PartyComposer.MaximumPlayers; players++)
            {
                var composition = PartyComposer.Recommend(players);
                Assert.That(composition.Note, Does.Contain("reste à valider"));
                Assert.That(composition.Warnings, Is.Not.Empty);
                Assert.That(composition.Label, Does.Contain("Voyante"));
            }
            Assert.That(PartyComposer.Recommend(6).Warnings.Count, Is.EqualTo(2));
        }

        [Test]
        public void RoleEntriesAreReadOnlyAndContainCountsWithoutPlayerAssignments()
        {
            var composition = PartyComposer.Recommend(8);
            Assert.That(composition.Roles.Count, Is.EqualTo(3));
            Assert.That(composition.Roles[0].Label, Is.EqualTo("Loup-Garou"));
            Assert.That(composition.Roles[0].Count, Is.EqualTo(2));
            Assert.That(composition.Roles[1].Label, Is.EqualTo("Voyante"));
            Assert.That(composition.Roles[2].Label, Is.EqualTo("Villageois"));
            var list = (IList<RoleCount>)composition.Roles;
            Assert.Throws<NotSupportedException>(() => list.Clear());
            var warnings = (IList<string>)composition.Warnings;
            Assert.Throws<NotSupportedException>(() => warnings.Clear());
        }
    }
}
