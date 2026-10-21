using System;
using System.Collections.Generic;
using NGame.Core;
using NGame.Core.Multiplayer;
using UnityEngine;

namespace NGame.Presentation.Game
{
    public sealed class RoomCompositionPanel : MonoBehaviour
    {
        private static readonly string[] RoleIds = { "werewolf", "villager", "seer", "witch", "hunter", "cupid", "guard", "little-girl" };
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        private readonly Dictionary<string, TMPro.TextMeshProUGUI> labels = new Dictionary<string, TMPro.TextMeshProUGUI>();
        private readonly Dictionary<string, UnityEngine.UI.Button> additions = new Dictionary<string, UnityEngine.UI.Button>();
        private readonly Dictionary<string, UnityEngine.UI.Button> removals = new Dictionary<string, UnityEngine.UI.Button>();
        private RoomSettings working;
        private Action<RoomSettings> apply;
        private Action close;
        private TMPro.TextMeshProUGUI mode;
        private TMPro.TextMeshProUGUI total;
        private TMPro.TextMeshProUGUI durations, protocol;
        private int playerCount;
        private UnityEngine.UI.Button confirm;

        public void Initialize(RoomView view, RoomSettings initial, Action<RoomSettings> save, Action dismiss)
        {
            apply = save;
            close = dismiss;
            playerCount = view?.players?.Length ?? 0;
            working = initial != null ? JsonUtility.FromJson<RoomSettings>(JsonUtility.ToJson(initial)) : new RoomSettings();
            working.mode = view?.mode ?? working.mode;
            foreach (var id in RoleIds) counts[id] = 0;
            var deck = initial?.composition;
            if (deck == null || deck.Length == 0) deck = view?.composition;
            if (deck != null)
                foreach (var entry in deck) if (entry != null && counts.ContainsKey(entry.roleId)) counts[entry.roleId] = entry.count;
            if (CountCards() == 0) Suggest();
            var shade = UITheme.Surface("Composition privacy backdrop", transform, new Color(.01f, .02f, .035f, .95f), null, true);
            shade.raycastTarget = true;
            var heading = UITheme.Copy("Lobby role configuration title", transform, "Le casting du village", 33, UITheme.Parchment);
            UITheme.Position(heading.rectTransform, UITheme.Top, 0, -114, 390, 62);
            var content = GameUI.Scroll(transform, "Eight implemented roles", 160, 175);
            mode = GameUI.Text(content, "Configured mode", "", 22, 42);
            var modes = GameUI.Row(content, "Mode choices", 45);
            GameUI.Button(modes, "Physical gathering mode", "En présence", () => SetMode("in-person"), false, 43);
            GameUI.Button(modes, "Remote room mode", "À distance", () => SetMode("remote"), false, 43);
            var protocols = GameUI.Row(content, "Host night protocol choices", 44);
            GameUI.Button(protocols, "Host traditional night protocol", "Yeux fermés", () => SetProtocol("traditional"), false, 44);
            GameUI.Button(protocols, "Host screen night protocol", "Lucioles", () => SetProtocol("screen"), false, 44);
            protocol = GameUI.Text(content, "Configured night protocol", "", 13, 100, false, UITheme.Brass);
            total = GameUI.Text(content, "Composition count", "", 15, 48, false, UITheme.Brass);
            GameUI.Button(content, "Suggest a complete composition", "Proposer une composition", () => { Suggest(); Render(); }, false);
            foreach (var id in RoleIds)
            {
                var roleId = id;
                var row = GameUI.Row(content, "Composition " + id, 44);
                var minus = GameUI.Button(row, "Remove " + id, "−", () => Change(roleId, -1), false, 42);
                minus.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 40;
                removals[id] = minus;
                labels[id] = GameUI.Text(row, "Count " + id, "", 18, 42);
                labels[id].GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 205;
                var plus = GameUI.Button(row, "Add " + id, "+", () => Change(roleId, 1), false, 42);
                plus.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 40;
                additions[id] = plus;
            }
            GameUI.Text(content, "Physical observation protocol",
                "La Petite Fille demande une nuit traditionnelle à la même table : yeux fermés, narration et observation physique. Elle est indisponible à distance et avec les lucioles.",
                11, 74, false, UITheme.Quiet);
            durations = GameUI.Text(content, "Configured durations", "", 15, 45, false);
            var night = GameUI.Row(content, "Night duration adjustment", 42);
            GameUI.Button(night, "Shorter night", "Nuit −5 s", () => { working.nightSeconds = Mathf.Max(3, working.nightSeconds - 5); Render(); }, false, 40);
            GameUI.Button(night, "Longer night", "Nuit +5 s", () => { working.nightSeconds = Mathf.Min(120, working.nightSeconds + 5); Render(); }, false, 40);
            var day = GameUI.Row(content, "Discussion duration adjustment", 42);
            GameUI.Button(day, "Shorter discussion", "Débat −15 s", () => { working.discussionSeconds = Mathf.Max(5, working.discussionSeconds - 15); Render(); }, false, 40);
            GameUI.Button(day, "Longer discussion", "Débat +15 s", () => { working.discussionSeconds = Mathf.Min(600, working.discussionSeconds + 15); Render(); }, false, 40);
            GameUI.Text(content, "Day vote duration note", "Vote : 45 s en présence ; à distance, la fenêtre de parole reste ouverte au moins 30 s et jusqu’à 90 s par défaut. Le micro est coupé au départ.", 12, 85, false, UITheme.Quiet);
            GameUI.Text(content, "Implemented role scope", "Ces huit rôles possèdent un moteur de règles. Les autres cartes restent au catalogue.",
                11, 51, false, UITheme.Quiet);
            confirm = UITheme.Action("Apply room composition", transform, "Appliquer au village", Apply);
            UITheme.Position((RectTransform)confirm.transform, UITheme.Bottom, 0, 124, 382, 56);
            var back = UITheme.Action("Cancel composition edit", transform, "Retour au salon", () => close?.Invoke(), false);
            UITheme.Position((RectTransform)back.transform, UITheme.Bottom, 0, 62, 382, 48);
            Render();
        }

        private void SetMode(string value)
        {
            working.mode = value;
            // Keep the selected cards visible: changing a mode must never silently replace a role.
            Render();
        }
        private void SetProtocol(string value) { working.nightProtocol = value; Render(); }
        private bool LittleGirlAvailable => working.mode != "remote" && working.nightProtocol == "traditional";
        private void Change(string id, int delta)
        {
            if (id == "little-girl" && !LittleGirlAvailable && delta > 0) return;
            var limit = id == "werewolf" || id == "villager" ? 24 : 1;
            counts[id] = Mathf.Clamp(counts[id] + delta, 0, limit);
            Render();
        }
        private int CountCards() { var sum = 0; foreach (var pair in counts) sum += pair.Value; return sum; }
        private void Suggest()
        {
            foreach (var id in RoleIds) counts[id] = 0;
            var number = Mathf.Clamp(playerCount, 6, 24);
            counts["werewolf"] = number == 6 ? 1 : number <= 11 ? 2 : number <= 18 ? 3 : 4;
            counts["seer"] = 1;
            if (number >= 8) { counts["witch"] = 1; counts["hunter"] = 1; }
            if (number >= 10) counts["guard"] = 1;
            if (number >= 12) counts["cupid"] = 1;
            counts["villager"] = number - CountCards();
        }
        private void Render()
        {
            if (mode == null) return;
            mode.text = working.mode == "remote" ? "Une soirée à distance" : "Autour de la même table";
            var number = CountCards();
            protocol.text = working.nightProtocol == "screen"
                ? "Lucioles : écrans ouverts et activité facultative pour tous. Les appels nocturnes gardent leur durée complète. La Petite Fille est indisponible."
                : "Yeux fermés entre les appels. La Petite Fille est possible uniquement à la même table, avec observation physique.";
            if (!LittleGirlAvailable && counts["little-girl"] > 0)
                protocol.text += "\nRetire explicitement la Petite Fille ou choisis le protocole traditionnel en présence.";
            total.text = number + " cartes · " + playerCount + " personnes au salon\nUne carte par personne sera requise pour démarrer.";
            foreach (var id in RoleIds)
            {
                var available = id != "little-girl" || LittleGirlAvailable;
                labels[id].text = (RoleCatalog.Find(id)?.DisplayName ?? id) + " · " + counts[id] + (!available ? "\nTraditionnel en présence" : "");
                removals[id].interactable = counts[id] > 0;
                additions[id].interactable = available && counts[id] < (id == "werewolf" || id == "villager" ? 24 : 1);
            }
            durations.text = "Étape nocturne : " + working.nightSeconds + " s\nDiscussion : " + working.discussionSeconds + " s";
            if (confirm != null) confirm.interactable = number >= 6 && number <= 24 && counts["werewolf"] > 0 && counts["werewolf"] < number && (LittleGirlAvailable || counts["little-girl"] == 0);
        }
        private void Apply()
        {
            var deck = new List<NGame.Core.Multiplayer.RoleCount>();
            foreach (var id in RoleIds) if (counts[id] > 0) deck.Add(new NGame.Core.Multiplayer.RoleCount { roleId = id, count = counts[id] });
            working.composition = deck.ToArray();
            apply?.Invoke(working);
            close?.Invoke();
        }
    }
}
