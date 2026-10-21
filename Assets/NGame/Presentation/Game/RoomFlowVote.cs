using System;
using NGame.Core.Multiplayer;
using TMPro;
using UnityEngine;

namespace NGame.Presentation.Game
{
    public sealed partial class RoomFlowView
    {
        private void RenderCompactVote(Transform parent, ActionView action)
        {
            if (action.submitted)
            {
                GameUI.Text(parent, "Submitted action state", Client.View.selfVoteAbstained ? "Abstention enregistrée"
                    : "Vote pour " + Name(Client.View, Client.View.selfVoteTargetId) + " enregistré", 18, 48, true, UITheme.Brass);
                return;
            }
            var options = action.choices ?? Array.Empty<string>();
            if (options.Length == 0) return;
            if (string.IsNullOrEmpty(choice) || Array.IndexOf(options, choice) < 0) choice = options[0];
            var optionRow = GameUI.Row(parent, "Action options row", 44);
            foreach (var value in options)
            {
                var selected = value;
                var button = GameUI.Button(optionRow, "Action choice " + value,
                    (choice == value ? "• " : "") + ChoiceName(value), () => ChooseActionChoice(selected), choice == value, 44);
                button.GetComponentInChildren<TMP_Text>().fontSize = 15;
                button.interactable = !Client.IsBusy;
            }
            if (NeededTargets(action, choice) == 0) return;
            var ids = action.allowedTargetIds ?? Array.Empty<string>();
            const int pageSize = 3;
            var pages = Math.Max(1, (ids.Length + pageSize - 1) / pageSize);
            targetPage = Mathf.Clamp(targetPage, 0, pages - 1);
            var end = Math.Min(ids.Length, (targetPage + 1) * pageSize);
            for (var i = targetPage * pageSize; i < end; i += 3)
            {
                var row = GameUI.Row(parent, "Target selection row", 44, 5);
                for (var j = i; j < Math.Min(end, i + 3); j++)
                {
                    var id = ids[j];
                    var button = GameUI.Button(row, "Action target " + id,
                        (targets.Contains(id) ? "• " : "") + Name(Client.View, id), () => ToggleTarget(id), targets.Contains(id), 44);
                    button.GetComponentInChildren<TMP_Text>().fontSize = 15;
                    button.interactable = !Client.IsBusy;
                }
            }
            if (pages <= 1) return;
            var navigation = GameUI.Row(parent, "Target pagination", 40);
            navigation.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
            var previous = GameUI.Button(navigation, "Previous targets", "← Précédents",
                () => { targetPage--; RefreshActionForm(); }, false, 40);
            previous.interactable = targetPage > 0;
            previous.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;
            var page = GameUI.Text(navigation, "Target page indicator", (targetPage + 1) + " / " + pages, 14, 40, false, UITheme.Brass);
            var pageLayout = page.GetComponent<UnityEngine.UI.LayoutElement>(); pageLayout.minWidth = pageLayout.preferredWidth = 44; pageLayout.flexibleWidth = 0;
            page.alignment = TextAlignmentOptions.Midline;
            var next = GameUI.Button(navigation, "Next targets", "Suivants →", () => { targetPage++; RefreshActionForm(); }, false, 40);
            next.interactable = targetPage < pages - 1;
            next.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;
        }
    }
}
