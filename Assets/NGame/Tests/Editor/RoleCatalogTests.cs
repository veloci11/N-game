using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NGame.Core.Tests
{
    public sealed class RoleCatalogTests
    {
        [Test]
        public void ApprovedCatalogueHasUniqueIdsAndACompleteIllustrationMapping()
        {
            var approvedIds = new[]
            {
                "werewolf", "villager", "seer", "witch", "hunter", "cupid", "guard", "little-girl",
                "white-wolf", "thief", "elder", "raven", "wild-child", "piper", "fox", "scapegoat"
            };
            Assert.That(RoleCatalog.All.Count, Is.EqualTo(approvedIds.Length));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var artIndices = new HashSet<int>();
            for (var index = 0; index < approvedIds.Length; index++)
            {
                var role = RoleCatalog.All[index];
                Assert.That(role.Id, Is.EqualTo(approvedIds[index]), "Catalogue order must match the approved art sheets.");
                Assert.That(ids.Add(role.Id), Is.True, "A duplicate ID would misidentify a card.");
                Assert.That(artIndices.Add(role.RoleArtIndex), Is.True, "Each card requires a distinct illustration.");
                Assert.That(role.RoleArtIndex, Is.EqualTo(index));
            }
        }

        [Test]
        public void EveryCardHasPublicCopyAndVersionedEngineSupportIsLimitedToEightRoles()
        {
            var implemented = new HashSet<string>(StringComparer.Ordinal)
                { "werewolf", "villager", "seer", "witch", "hunter", "cupid", "guard", "little-girl" };
            foreach (var role in RoleCatalog.All)
            {
                Assert.That(string.IsNullOrWhiteSpace(role.DisplayName), Is.False);
                Assert.That(string.IsNullOrWhiteSpace(role.Summary), Is.False);
                Assert.That(role.RulesImplemented, Is.EqualTo(implemented.Contains(role.Id)));
                Assert.That(role.ImplementedRuleset, Is.EqualTo(implemented.Contains(role.Id) ? "ngame-classic-v1" : null));
                Assert.That(role.SupportsRuleset("ngame-classic-v1", "in-person"), Is.EqualTo(implemented.Contains(role.Id)));
                Assert.That(role.SupportsRuleset("ngame-classic-v1", "remote"), Is.EqualTo(implemented.Contains(role.Id) && role.Id != "little-girl"));
                Assert.That(role.SupportsRuleset("unversioned-classic", "in-person"), Is.False);
                Assert.That(role.SupportsRuleset("ngame-classic-v1", "unknown-mode"), Is.False);
                Assert.That(Enum.IsDefined(typeof(RoleModeSupport), role.PresenceSupport), Is.True);
                Assert.That(Enum.IsDefined(typeof(RoleModeSupport), role.DistanceSupport), Is.True);
            }
        }

        [Test]
        public void PetiteFillePreservesThePhysicalProtocolAndRequiresARemoteAdaptation()
        {
            var role = RoleCatalog.Find("little-girl");
            Assert.That(role.PresenceSupport, Is.EqualTo(RoleModeSupport.TableProtocolRequired));
            Assert.That(role.DistanceSupport, Is.EqualTo(RoleModeSupport.AdaptationRequired));
            Assert.That(role.Summary, Does.Contain("risque"));
        }

        [Test]
        public void SpatialAndBuildingDependenciesAreNotPresentedAsAutomaticRemoteRules()
        {
            Assert.That(RoleCatalog.Find("fox").DistanceSupport, Is.EqualTo(RoleModeSupport.AdaptationRequired));
            var raven = RoleCatalog.Find("raven");
            Assert.That(raven.PresenceSupport, Is.EqualTo(RoleModeSupport.AdaptationRequired));
            Assert.That(raven.DistanceSupport, Is.EqualTo(RoleModeSupport.AdaptationRequired));
        }

        [Test]
        public void KnownLookupReturnsTheSameImmutableCatalogueEntry()
        {
            foreach (var role in RoleCatalog.All) Assert.That(RoleCatalog.Find(role.Id), Is.SameAs(role));
            var list = (IList<RoleDefinition>)RoleCatalog.All;
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("missing")]
        [TestCase("WEREWOLF")]
        [TestCase(" werewolf ")]
        public void UnknownIdsDoNotFallBackToAnotherRole(string id)
        {
            Assert.That(RoleCatalog.Find(id), Is.Null);
        }
    }
}
