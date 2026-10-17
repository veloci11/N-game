using System;
using System.Collections.Generic;

namespace NGame.Core
{
    public enum VillagePhase
    {
        Night,
        Dawn,
        Vote,
        VoteConfirmed
    }

    /// <summary>A decorative participant, with no assigned or exposed secret role.</summary>
    public sealed class DecorativePlayer
    {
        public string Id { get; }
        public string Name { get; }

        public DecorativePlayer(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Un identifiant est requis.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Un nom est requis.", nameof(name));
            Id = id;
            Name = name;
        }
    }

    /// <summary>
    /// Local visual preview state. No network, distributed roles, collective tally or elimination.
    /// The presentation owns hold timing and must conceal on release and lifecycle cancellation.
    /// </summary>
    public sealed class VillageSession
    {
        private static readonly IReadOnlyList<DecorativePlayer> DemoPlayers = Array.AsReadOnly(new[]
        {
            new DecorativePlayer("lea", "Léa"),
            new DecorativePlayer("max", "Max"),
            new DecorativePlayer("chloe", "Chloé"),
            new DecorativePlayer("hugo", "Hugo"),
            new DecorativePlayer("emma", "Emma"),
            new DecorativePlayer("lucas", "Lucas")
        });

        public VillagePhase Phase { get; private set; } = VillagePhase.Night;
        public bool RoleVisible { get; private set; }
        public string SelectedPlayerId { get; private set; }
        public string ConfirmedPlayerId { get; private set; }
        public IReadOnlyList<DecorativePlayer> Players => DemoPlayers;

        // This fixed role belongs only to the local demonstration, never to a decorative player.
        public string LocalRoleName => RoleVisible ? "Loup-Garou" : null;

        /// <summary>Emitted synchronously once per effective state change.</summary>
        public event Action Changed;

        public bool RevealSecret()
        {
            if (Phase != VillagePhase.Night || RoleVisible) return false;
            RoleVisible = true;
            return Notify();
        }

        public bool ConcealSecret()
        {
            if (!RoleVisible) return false;
            RoleVisible = false;
            return Notify();
        }

        public bool MoveTo(VillagePhase phase)
        {
            if (!Enum.IsDefined(typeof(VillagePhase), phase))
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Phase inconnue.");
            if (phase == VillagePhase.VoteConfirmed && ConfirmedPlayerId == null)
                throw new InvalidOperationException("Confirme une sélection avant d’afficher sa confirmation.");

            var changed = Phase != phase || RoleVisible || SelectedPlayerId != null;
            Phase = phase;
            RoleVisible = false;
            SelectedPlayerId = null;
            // A presentation shortcut must never overwrite an already confirmed choice.
            return changed && Notify();
        }

        public bool SelectPlayer(string id)
        {
            if (Phase != VillagePhase.Vote || ConfirmedPlayerId != null || !ContainsPlayer(id)) return false;
            if (SelectedPlayerId == id) return false;
            SelectedPlayerId = id;
            return Notify();
        }

        public bool ConfirmSelection()
        {
            if (Phase != VillagePhase.Vote || ConfirmedPlayerId != null || SelectedPlayerId == null) return false;
            ConfirmedPlayerId = SelectedPlayerId;
            SelectedPlayerId = null;
            RoleVisible = false;
            Phase = VillagePhase.VoteConfirmed;
            return Notify();
        }

        public bool Reset()
        {
            var changed = Phase != VillagePhase.Night || RoleVisible || SelectedPlayerId != null || ConfirmedPlayerId != null;
            Phase = VillagePhase.Night;
            RoleVisible = false;
            SelectedPlayerId = null;
            ConfirmedPlayerId = null;
            return changed && Notify();
        }

        private static bool ContainsPlayer(string id)
        {
            if (id == null) return false;
            for (var index = 0; index < DemoPlayers.Count; index++)
                if (string.Equals(DemoPlayers[index].Id, id, StringComparison.Ordinal)) return true;
            return false;
        }

        private bool Notify()
        {
            Changed?.Invoke();
            return true;
        }
    }
}
