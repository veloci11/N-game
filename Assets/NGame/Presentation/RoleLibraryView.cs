using NGame.Core;
using TMPro;
using UnityEngine;

namespace NGame.Presentation
{
    /// <summary>A public visual catalogue. It never reads or assigns a player's secret role.</summary>
    public sealed class RoleLibraryView : MonoBehaviour
    {
        private const int CardsPerPage = 4;
        public int PageIndex { get; private set; }
        public int PageCount => (RoleCatalog.All.Count + CardsPerPage - 1) / CardsPerPage;
        public string SelectedRoleId { get; private set; }
        private RectTransform root;
        private RectTransform grid;
        private GameObject detail;
        private GameObject overview;
        private readonly RectTransform[] cards = new RectTransform[CardsPerPage];
        private readonly UnityEngine.UI.Image[] art = new UnityEngine.UI.Image[CardsPerPage];
        private readonly TextMeshProUGUI[] names = new TextMeshProUGUI[CardsPerPage];
        private readonly RoleCardMotion[] motions = new RoleCardMotion[CardsPerPage];
        private TextMeshProUGUI pageLabel;
        private UnityEngine.UI.Button previous;
        private UnityEngine.UI.Button next;
        private TextMeshProUGUI detailTitle;
        private TextMeshProUGUI summary;
        private TextMeshProUGUI presence;
        private TextMeshProUGUI distance;
        private TextMeshProUGUI presenceLabel;
        private TextMeshProUGUI distanceLabel;
        private TextMeshProUGUI ruleStatus;
        private RectTransform detailRule;
        private UnityEngine.UI.Image detailArt;
        private RoleCardMotion detailMotion;
        private RectTransform detailCard;
        private AppPreferences preferences;
        private float lastHeight;

        public void Initialize(AppPreferences settings)
        {
            preferences = settings;
            root = (RectTransform)transform;
            overview = UITheme.Box("Catalogue introduction", transform, true).gameObject;
            var paper = UITheme.Surface("Library ink backdrop", overview.transform, UITheme.Opacity(UITheme.Midnight, .6f), UITheme.Panel, true);
            paper.rectTransform.offsetMin = new Vector2(16, 92);
            paper.rectTransform.offsetMax = new Vector2(-16, -207);
            var caption = Text("Catalogue overline", overview.transform, "LE LIVRE DES RÔLES", 9, UITheme.Brass,
                UITheme.Top, 0, -109, 370, 23, false);
            caption.characterSpacing = 3;
            Text("Catalogue title", overview.transform, "Les cartes du village", 36, UITheme.Parchment,
                UITheme.Top, 0, -150, 395, 58);
            Text("Catalogue invitation", overview.transform, "Chaque visage cache une histoire.", 18, UITheme.Parchment,
                UITheme.Top, 0, -190, 370, 32);
            grid = UITheme.Box("Four illustrated roles", transform, true);
            for (var i = 0; i < CardsPerPage; i++) BuildCardSlot(i);
            previous = UITheme.Action("Previous role page", transform, "‹", () => OpenPage(PageIndex - 1), false);
            UITheme.Position((RectTransform)previous.transform, UITheme.Bottom, -119, 61, 48, 44);
            next = UITheme.Action("Next role page", transform, "›", () => OpenPage(PageIndex + 1), false);
            UITheme.Position((RectTransform)next.transform, UITheme.Bottom, 119, 61, 48, 44);
            pageLabel = Text("Role page number", transform, "", 20, UITheme.Parchment,
                UITheme.Bottom, 0, 61, 150, 39);
            Text("Catalogue scope", transform, "8 RÔLES JOUABLES · 16 CARTES ILLUSTRÉES", 8,
                UITheme.Opacity(UITheme.Parchment, .65f), UITheme.Bottom, 0, 23, 390, 24, false).characterSpacing = 1.1f;
            BuildDetail();
            OpenPage(0);
            ArrangeCards();
            ArrangeDetail();
        }

        private TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color,
            Vector2 anchor, float x, float y, float width, float height, bool display = true)
        {
            var label = UITheme.Copy(name, parent, value, size, color, display);
            UITheme.Position(label.rectTransform, anchor, x, y, width, height);
            return label;
        }

        private void BuildCardSlot(int slot)
        {
            var index = slot;
            var button = UITheme.Action("Open illustrated role " + slot, grid, "", () =>
            {
                var roleIndex = PageIndex * CardsPerPage + index;
                if (roleIndex < RoleCatalog.All.Count) ShowRole(RoleCatalog.All[roleIndex].Id);
            }, false);
            cards[slot] = (RectTransform)button.transform;
            var image = UITheme.Surface("Approved card illustration", button.transform, Color.white, null, true);
            image.preserveAspect = true;
            art[slot] = image;
            names[slot] = Text("Role name", grid, "", 22, UITheme.Parchment,
                UITheme.Top, 0, 0, 176, 39);
            motions[slot] = button.gameObject.AddComponent<RoleCardMotion>();
            motions[slot].Initialize(preferences, slot, image);
        }

        private void BuildDetail()
        {
            detail = UITheme.Box("Illustrated role detail", transform, true).gameObject;
            var background = UITheme.Surface("Detail quiet paper", detail.transform,
                UITheme.Opacity(UITheme.Midnight, .98f), UITheme.Panel, true);
            background.rectTransform.offsetMin = new Vector2(13, 94);
            background.rectTransform.offsetMax = new Vector2(-13, -94);
            detailTitle = Text("Detailed role name", detail.transform, "", 36, UITheme.Parchment,
                UITheme.Top, 0, -129, 390, 65);
            var frame = UITheme.Surface("Large approved role card", detail.transform, UITheme.Midnight, UITheme.Panel);
            detailCard = frame.rectTransform;
            UITheme.Position(detailCard, UITheme.Top, 0, -330, 210, 310);
            detailArt = UITheme.Surface("Large role illustration", frame.transform, Color.white, null, true);
            detailArt.preserveAspect = true;
            detailArt.raycastTarget = true;
            detailMotion = frame.gameObject.AddComponent<RoleCardMotion>();
            detailMotion.Initialize(preferences, 9, detailArt);
            summary = Text("Role story", detail.transform, "", 21, UITheme.Parchment,
                UITheme.Top, 0, -547, 340, 99);
            summary.lineSpacing = 2;
            detailRule = UITheme.Box("Detail mode separator", detail.transform);
            UITheme.Position(detailRule, UITheme.Top, 0, -611, 304, 3);
            UITheme.Separator(detailRule, UITheme.Center, 0, 304);
            presenceLabel = Text("Presence mode label", detail.transform, "EN PRÉSENCE", 9, UITheme.Brass,
                UITheme.Top, -93, -645, 145, 26, false);
            presenceLabel.characterSpacing = 1.2f;
            presence = Text("Presence mode design", detail.transform, "", 17, UITheme.Parchment,
                UITheme.Top, 72, -645, 190, 43);
            distanceLabel = Text("Distance mode label", detail.transform, "À DISTANCE", 9, UITheme.Brass,
                UITheme.Top, -93, -692, 145, 26, false);
            distanceLabel.characterSpacing = 1.2f;
            distance = Text("Distance mode design", detail.transform, "", 17, UITheme.Parchment,
                UITheme.Top, 72, -692, 190, 43);
            ruleStatus = Text("Role rules status", detail.transform, "",
                10, UITheme.Quiet, UITheme.Bottom, 0, 133, 340, 52, false);
            var back = UITheme.Action("Close role detail", detail.transform, "Retour aux cartes", BackToGrid, false);
            UITheme.Position((RectTransform)back.transform, UITheme.Bottom, 0, 62, 340, 47);
        }

        public bool OpenPage(int number)
        {
            if (number < 0 || number >= PageCount) return false;
            PageIndex = number;
            BackToGrid();
            for (var i = 0; i < CardsPerPage; i++)
            {
                var index = number * CardsPerPage + i;
                cards[i].gameObject.SetActive(index < RoleCatalog.All.Count);
                names[i].gameObject.SetActive(index < RoleCatalog.All.Count);
                if (index >= RoleCatalog.All.Count) continue;
                var role = RoleCatalog.All[index];
                art[i].sprite = UITheme.RoleCard(role.RoleArtIndex);
                motions[i].SetRole(role.Id);
                names[i].text = role.DisplayName;
                motions[i].RestartEntrance(i);
            }
            previous.interactable = number > 0;
            next.interactable = number < PageCount - 1;
            pageLabel.text = (number + 1) + " / " + PageCount;
            return true;
        }

        public bool ShowRole(string id)
        {
            var role = RoleCatalog.Find(id);
            if (role == null) return false;
            SelectedRoleId = role.Id;
            detailTitle.text = role.DisplayName;
            detailArt.sprite = UITheme.RoleCard(role.RoleArtIndex);
            detailMotion.SetRole(role.Id);
            summary.text = role.Summary;
            presence.text = ModeDescription(role.PresenceSupport);
            distance.text = ModeDescription(role.DistanceSupport);
            ruleStatus.text = role.RulesImplemented
                ? "Rôle disponible dans les compositions compatibles.\nLes règles choisies sont affichées avant la partie."
                : "Illustration du catalogue.\nCe rôle n’est pas encore disponible en partie.";
            grid.gameObject.SetActive(false);
            overview.SetActive(false);
            previous.gameObject.SetActive(false);
            next.gameObject.SetActive(false);
            pageLabel.gameObject.SetActive(false);
            detail.SetActive(true);
            detailMotion.RestartEntrance(0);
            return true;
        }

        public void BackToGrid()
        {
            SelectedRoleId = null;
            if (overview != null) overview.SetActive(true);
            if (detail != null) detail.SetActive(false);
            if (grid != null) grid.gameObject.SetActive(true);
            if (previous != null) previous.gameObject.SetActive(true);
            if (next != null) next.gameObject.SetActive(true);
            if (pageLabel != null) pageLabel.gameObject.SetActive(true);
        }

        private static string ModeDescription(RoleModeSupport support)
        {
            switch (support)
            {
                case RoleModeSupport.TableProtocolRequired: return "Protocole à table";
                case RoleModeSupport.AdaptationRequired: return "Variante nécessaire";
                default: return "Principe transposable";
            }
        }

        private void ArrangeCards()
        {
            if (root == null || grid == null) return;
            var available = Mathf.Max(330, root.rect.height - 340);
            var row = available * .5f;
            var height = Mathf.Min(225, row - 47);
            var width = height * .71f;
            for (var i = 0; i < CardsPerPage; i++)
            {
                var x = i % 2 == 0 ? -91 : 91;
                var y = -232 - height * .5f - i / 2 * row;
                UITheme.Position(cards[i], UITheme.Top, x, y, width, height);
                motions[i].SetRestPose(new Vector2(x, y));
                UITheme.Position(names[i].rectTransform, UITheme.Top, x, y - height * .5f - 23, 176, 39);
            }
            lastHeight = root.rect.height;
        }
        private void ArrangeDetail()
        {
            if (root == null || detailCard == null || root.rect.height <= 0) return;
            const float cardTop = 175;
            var cardHeight = Mathf.Clamp(root.rect.height - 560, 190, 310);
            var cardCenter = cardTop + cardHeight * .5f;
            UITheme.Position(detailCard, UITheme.Top, 0, -cardCenter, cardHeight * 210 / 310, cardHeight);
            detailMotion.SetRestPose(new Vector2(0, -cardCenter));

            var cardBottom = cardTop + cardHeight;
            const float summaryHeight = 88;
            var summaryCenter = cardBottom + 10 + summaryHeight * .5f;
            UITheme.Position(summary.rectTransform, UITheme.Top, 0, -summaryCenter, 340, summaryHeight);
            var ruleY = summaryCenter + summaryHeight * .5f + 10;
            UITheme.Position(detailRule, UITheme.Top, 0, -ruleY, 304, 3);
            var presenceY = ruleY + 30;
            var distanceY = presenceY + 44;
            UITheme.Position(presenceLabel.rectTransform, UITheme.Top, -93, -presenceY, 145, 26);
            UITheme.Position(presence.rectTransform, UITheme.Top, 72, -presenceY, 190, 40);
            UITheme.Position(distanceLabel.rectTransform, UITheme.Top, -93, -distanceY, 145, 26);
            UITheme.Position(distance.rectTransform, UITheme.Top, 72, -distanceY, 190, 40);
        }

        private void LateUpdate()
        {
            if (root == null || Mathf.Approximately(lastHeight, root.rect.height)) return;
            ArrangeCards();
            ArrangeDetail();
        }
    }
}
