using System;
using System.Collections.Generic;

namespace NGame.Core
{
    /// <summary>Design feasibility for a mode; this does not mean a rule is implemented.</summary>
    public enum RoleModeSupport
    {
        FaithfulPossible,
        TableProtocolRequired,
        AdaptationRequired
    }

    /// <summary>Immutable public catalogue entry, never an assignment of a secret role to a player.</summary>
    public sealed class RoleDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Summary { get; }
        public int RoleArtIndex { get; }
        public RoleModeSupport PresenceSupport { get; }
        public RoleModeSupport DistanceSupport { get; }

        /// <summary>Versioned N'GAME engine support; this does not claim every published variant is implemented.</summary>
        public string ImplementedRuleset { get; }
        public bool RulesImplemented => ImplementedRuleset != null;

        public bool SupportsRuleset(string ruleset, string mode)
            => RulesImplemented && string.Equals(ruleset, ImplementedRuleset, StringComparison.Ordinal)
                && (mode == "in-person" || (mode == "remote" && DistanceSupport == RoleModeSupport.FaithfulPossible));

        internal RoleDefinition(string id, string displayName, string summary, int roleArtIndex,
            RoleModeSupport presenceSupport, RoleModeSupport distanceSupport, bool rulesImplemented)
        {
            Id = id;
            DisplayName = displayName;
            Summary = summary;
            RoleArtIndex = roleArtIndex;
            PresenceSupport = presenceSupport;
            DistanceSupport = distanceSupport;
            ImplementedRuleset = rulesImplemented ? "ngame-classic-v1" : null;
        }
    }

    public static class RoleCatalog
    {
        // The order is the approved illustration order, independent from night action order.
        public static IReadOnlyList<RoleDefinition> All { get; } = Array.AsReadOnly(new[]
        {
            Define("werewolf", "Loup-Garou", "Se réunit avec la meute pour choisir sa victime nocturne.", 0, implemented: true),
            Define("villager", "Villageois", "Observe, débat et vote pour démasquer les loups.", 1, implemented: true),
            Define("seer", "Voyante", "Découvre secrètement le rôle d’un joueur pendant la nuit.", 2, implemented: true),
            Define("witch", "Sorcière", "Dispose d’une potion de soin et d’une potion de poison.", 3, implemented: true),
            Define("hunter", "Chasseur", "Désigne une dernière cible lorsqu’il est éliminé.", 4, implemented: true),
            Define("cupid", "Cupidon", "Lie deux amoureux dont les destins deviennent inséparables.", 5, implemented: true),
            Define("guard", "Garde", "Protège un joueur contre les loups, sous les restrictions du Salvateur.", 6, implemented: true),
            Define("little-girl", "Petite Fille", "Observe discrètement les loups éveillés, au risque d’être surprise.", 7,
                RoleModeSupport.TableProtocolRequired, RoleModeSupport.AdaptationRequired, implemented: true),
            Define("white-wolf", "Loup Blanc", "Se cache dans la meute et cherche à survivre seul.", 8),
            Define("thief", "Voleur", "Peut échanger son rôle au début avec une carte de réserve.", 9),
            Define("elder", "Ancien", "Résiste à une première attaque des loups ; sa mort a des conséquences.", 10),
            Define("raven", "Corbeau", "Marque un suspect qui reçoit deux voix supplémentaires au vote.", 11,
                RoleModeSupport.AdaptationRequired, RoleModeSupport.AdaptationRequired),
            Define("wild-child", "Enfant Sauvage", "Choisit un modèle et rejoint les loups si celui-ci est éliminé.", 12),
            Define("piper", "Joueur de Flûte", "Charme des joueurs et poursuit sa propre condition de victoire.", 13),
            Define("fox", "Renard", "Cherche la présence d’un loup parmi trois voisins du cercle.", 14,
                RoleModeSupport.FaithfulPossible, RoleModeSupport.AdaptationRequired),
            Define("scapegoat", "Bouc Émissaire", "Subit une égalité du vote et désigne ensuite les votants autorisés.", 15)
        });

        /// <summary>Exact, case-sensitive stable ID lookup. Unknown and null IDs return null.</summary>
        public static RoleDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (var index = 0; index < All.Count; index++)
                if (string.Equals(All[index].Id, id, StringComparison.Ordinal)) return All[index];
            return null;
        }

        private static RoleDefinition Define(string id, string name, string summary, int artIndex,
            RoleModeSupport presence = RoleModeSupport.FaithfulPossible,
            RoleModeSupport distance = RoleModeSupport.FaithfulPossible, bool implemented = false)
        {
            return new RoleDefinition(id, name, summary, artIndex, presence, distance, implemented);
        }
    }
}
