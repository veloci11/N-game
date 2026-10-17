using System;
using System.Collections.Generic;

namespace NGame.Core
{
    public sealed class RoleCount
    {
        public string Label { get; }
        public int Count { get; }

        internal RoleCount(string label, int count)
        {
            Label = label;
            Count = count;
        }
    }

    /// <summary>A proposed composition, not a role distribution or a running game.</summary>
    public sealed class PartyComposition
    {
        public int PlayerCount { get; }
        public int Wolves { get; }
        public int Seers { get; }
        public int Villagers { get; }
        public string Label { get; }
        public string Note { get; }
        public IReadOnlyList<RoleCount> Roles { get; }
        public IReadOnlyList<string> Warnings { get; }

        internal PartyComposition(int playerCount, int wolves)
        {
            PlayerCount = playerCount;
            Wolves = wolves;
            Seers = 1;
            Villagers = playerCount - wolves - Seers;
            Label = wolves + (wolves == 1 ? " Loup-Garou" : " Loups-Garous")
                + " · 1 Voyante · " + Villagers + " Villageois";
            Note = "Une composition proposée pour les essais. Son équilibrage reste à valider entre amis.";
            Roles = Array.AsReadOnly(new[]
            {
                new RoleCount("Loup-Garou", Wolves),
                new RoleCount("Voyante", Seers),
                new RoleCount("Villageois", Villagers)
            });
            var warnings = new List<string> { Note };
            if (playerCount == 6)
                warnings.Add("À six joueurs, le format d’initiation peut donner des parties très courtes.");
            Warnings = warnings.AsReadOnly();
        }
    }

    public static class PartyComposer
    {
        public const int MinimumPlayers = 6;
        public const int MaximumPlayers = 12;

        public static PartyComposition Recommend(int playerCount)
        {
            if (playerCount < MinimumPlayers || playerCount > MaximumPlayers)
                throw new ArgumentOutOfRangeException(nameof(playerCount), playerCount,
                    "Cette proposition couvre les groupes de six à douze joueurs.");

            var wolves = playerCount == 6 ? 1 : playerCount <= 10 ? 2 : 3;
            return new PartyComposition(playerCount, wolves);
        }
    }
}
