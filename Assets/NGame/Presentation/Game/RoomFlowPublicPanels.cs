using System;
using NGame.Core.Multiplayer;
using UnityEngine;

namespace NGame.Presentation.Game
{
    public sealed partial class RoomFlowView
    {
        private RectTransform PublicPanel(string name, string heading)
        {
            ForceMaskSecrets(); CloseChatPanel(); CloseMenu();
            menu = UITheme.Box(name, transform, true).gameObject;
            var backdrop = UITheme.Surface(name + " paper", menu.transform,
                new Color(.025f, .05f, .075f, .99f), null, true);
            backdrop.raycastTarget = true;
            var content = GameUI.Scroll(menu.transform, name + " content", 103, 175);
            GameUI.Text(content, name + " heading", heading, 31, 68);
            SecretCard.SetBlocked(true);
            PrimaryAction.interactable = SecondaryAction.interactable = false;
            return content;
        }

        public void OpenPublicChronicle()
        {
            var view = Client?.View;
            if (view == null) return;
            var content = PublicPanel("Public village chronicle", "Historique de la partie");
            GameUI.Text(content, "Chronicle explanation", "Les annonces publiques reçues jusqu’à l’ouverture de cette page. La partie continue pendant ta lecture.", 15, 80, false, UITheme.Brass);
            var events = view.events ?? Array.Empty<PublicEvent>();
            if (events.Length == 0) GameUI.Text(content, "Empty public chronicle", "Aucune annonce pour cette manche.", 18, 86);
            foreach (var item in events)
                GameUI.Text(content, "Chronicle event " + item.sequence, item.text, 17, 56, false);
            MenuDock("Revenir à la partie", CloseMenu);
        }

        public void ShowVoteClosureConfirmation()
        {
            var view = Client?.View;
            if (view == null || view.phase != "vote" || !view.isHost || !view.canAdvance) return;
            var expectedTurn = view.turn;
            var expectedMatch = view.matchId;
            var content = PublicPanel("Vote closure confirmation", "Terminer ce vote ?");
            GameUI.Text(content, "Vote closure consequence", "Tous les votes sont reçus et le temps minimal est écoulé. Clore le scrutin coupe les micros et affiche le verdict. Vous pouvez aussi poursuivre les échanges jusqu’à la fin du chronomètre.", 18, 185, false);
            MenuDock("Continuer les échanges", CloseMenu);
            var close = UITheme.Action("Confirm vote closure", menu.transform, "Clore le scrutin", () =>
            {
                CloseMenu();
                var current = Client?.View;
                if (current == null || current.matchId != expectedMatch || current.turn != expectedTurn
                    || current.phase != "vote" || !current.isHost || !current.canAdvance)
                { error.text = "Ce scrutin est déjà terminé ou ne peut plus être clos."; return; }
                Client.Send(new RoomCommand { kind = "advance", turn = expectedTurn, commandId = Guid.NewGuid().ToString("N") });
            }, false);
            UITheme.Position((RectTransform)close.transform, UITheme.Bottom, 0, 62, 382, 48);
        }

        public void OpenParticipantManagement()
        {
            var view = Client?.View;
            if (view == null || view.phase != "lobby" || !view.isHost) return;
            var content = PublicPanel("Gathering participants management", "Gérer les participants");
            GameUI.Text(content, "Participant management explanation", "Retirer une place demande une confirmation. Le groupe devra ensuite confirmer à nouveau sa préparation et vérifier la composition.", 15, 99, false, UITheme.Brass);
            foreach (var player in view.players ?? Array.Empty<PlayerView>())
            {
                var id = player.id;
                if (id == view.selfPlayerId)
                { GameUI.Text(content, "Host reserved seat", player.displayName + " · hôte", 18, 50); continue; }
                var button = GameUI.Button(content, "Remove gathering player " + id,
                    "Retirer " + player.displayName + (player.connected ? "" : " · absent"), () => ShowParticipantRemoval(id), false, 53);
                button.interactable = Client.IsConnected && !Client.IsBusy;
            }
            MenuDock("Revenir au salon", CloseMenu);
        }

        private void ShowParticipantRemoval(string playerId)
        {
            var view = Client?.View;
            if (view == null || view.phase != "lobby" || !view.isHost || playerId == view.selfPlayerId) return;
            var expectedTurn = view.turn;
            var expectedCode = view.code;
            var content = PublicPanel("Participant removal confirmation", "Retirer cette place ?");
            GameUI.Text(content, "Participant removal consequence", Name(view, playerId) + " quittera ce salon et son ancien accès ne fonctionnera plus. Les autres devront confirmer leur préparation à nouveau. Vérifie aussi le nombre de cartes.", 18, 174, false);
            MenuDock("Conserver cette place", OpenParticipantManagement);
            var remove = UITheme.Action("Confirm participant removal", menu.transform, "Retirer la place", () =>
            {
                CloseMenu();
                var current = Client?.View;
                if (current == null || current.code != expectedCode || current.phase != "lobby" || !current.isHost)
                { error.text = "Le salon a changé. Cette place n’a pas été retirée."; return; }
                Client.Send(new RoomCommand { kind = "remove-player", targetIds = new[] { playerId }, turn = expectedTurn,
                    commandId = Guid.NewGuid().ToString("N") });
            }, false);
            UITheme.Position((RectTransform)remove.transform, UITheme.Bottom, 0, 62, 382, 48);
        }
    }
}
