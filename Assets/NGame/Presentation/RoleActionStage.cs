using System.Collections.Generic;
using NGame.Core;
using NGame.Core.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace NGame.Presentation
{
    /// <summary>Decorative private action feedback. Never reads targets, private facts or another player's state.</summary>
    public sealed class RoleActionStage : MonoBehaviour
    {
        private enum Movement { Still, Float, Tilt, Orbit, Bubble, Glint, Aim, Lid, Flame, Bolt }
        private sealed class Piece
        {
            public RectTransform Rect; public Image Image; public Vector2 Rest;
            public Color Color; public Movement Movement; public float Phase, Amplitude, Angle;
        }
        private sealed class PotionBottle
        {
            public Image[] Images; public Color[] Colors; public bool Available = true;
        }
        private readonly List<PotionBottle> bottles = new List<PotionBottle>();
        private static readonly Color HealingColor = new Color(.35f, .74f, .49f, 1);
        private static readonly Color PoisonColor = new Color(.68f, .38f, .79f, 1);
        private readonly List<Piece> pieces = new List<Piece>();
        private readonly List<Sprite> ownedSprites = new List<Sprite>();
        private readonly List<Image> confirmation = new List<Image>();
        private AppPreferences preferences;
        private RectTransform art;
        private bool initialized, hasBound, confirmed;
        private string actionIdentity, acceptedChoice;
        private float clock, burst;
        public string VisualKind { get; private set; }
        public float AnimationClock => clock;
        public int ConfirmationVersion { get; private set; }
        public bool IsBound => hasBound;
        private bool Quiet => preferences != null && preferences.ReducedMotion;

        public void Initialize(AppPreferences settings)
        {
            preferences = settings;
            if (initialized) return;
            initialized = true;
            var input = GetComponent<CanvasGroup>();
            if (input == null) input = gameObject.AddComponent<CanvasGroup>();
            input.interactable = false; input.blocksRaycasts = false;
        }

        /// <summary>Pass the authoritative submitted flag and a stable match/turn identity, never optimistic button state.</summary>
        public void Bind(ActionView action, string roleId, bool isConfirmed, string identity = null)
        {
            if (!initialized) Initialize(null);
            if (actionIdentity != identity)
            { actionIdentity = identity; hasBound = false; confirmed = false; clock = burst = 0; }
            var kind = Kind(action?.kind, roleId);
            var changed = VisualKind != kind;
            if (changed) { VisualKind = kind; clock = burst = 0; Rebuild(); }
            acceptedChoice = isConfirmed ? action?.submittedChoice : null;
            if (kind == "witch" && action?.kind == "witch" && bottles.Count == 2)
            { bottles[0].Available = action.healingAvailable; bottles[1].Available = action.poisonAvailable; }
            if (hasBound && !changed && !confirmed && isConfirmed) NotifyConfirmed();
            confirmed = isConfirmed; hasBound = true; Render();
        }
        /// <summary>For an explicit authoritative acknowledgement; Bind handles the normal first submitted transition.</summary>
        public void NotifyConfirmed()
        {
            ConfirmationVersion++;
            var potionUsed = acceptedChoice == "save" || acceptedChoice == "heal" || acceptedChoice == "poison" || acceptedChoice == "both";
            burst = Quiet || VisualKind == "witch" && !potionUsed ? 0 : 1; Render();
        }

        private static string Kind(string action, string role)
        {
            if (action == "wolves") return "werewolf";
            if (action == "seer" || action == "witch" || action == "guard" || action == "hunter" || action == "cupid") return action;
            switch (role)
            {
                case "werewolf": case "seer": case "witch": case "guard": case "hunter": case "cupid": case "little-girl": return role;
                default: return "villager";
            }
        }
        private void Rebuild()
        {
            if (art != null) { art.gameObject.SetActive(false); Destroy(art.gameObject); }
            foreach (var sprite in ownedSprites) Destroy(sprite);
            ownedSprites.Clear(); pieces.Clear(); confirmation.Clear(); bottles.Clear();
            art = UITheme.Box("Private " + VisualKind + " action scenery", transform, true);
            switch (VisualKind)
            {
                case "seer": BuildSeer(); break;
                case "witch": BuildWitch(); break;
                case "werewolf": BuildWolf(); break;
                case "guard": BuildGuard(); break;
                case "cupid": BuildCupid(); break;
                case "hunter": BuildHunter(); break;
                case "little-girl": BuildEye(); break;
                default: BuildLantern(); break;
            }
            for (var i = 0; i < 8; i++)
            {
                var dot = Image("Action receipt light " + i, art, Vector2.zero, new Vector2(2.5f, 2.5f), UITheme.Brass, UITheme.Disc);
                confirmation.Add(dot);
            }
        }
        private void BuildSeer()
        {
            if (Art("seer-orb", Vector2.zero, 57) == null)
            {
                Add("Crystal sphere", Vector2.zero, new Vector2(43, 43), new Color(.38f, .62f, .72f, .72f), UITheme.Disc);
                Add("Crystal stand", new Vector2(0, -25), new Vector2(35, 5), UITheme.Brass);
            }
            for (var i = 0; i < 6; i++) Add("Separate orbit star " + i, Vector2.zero, Vector2.one * 2.5f, UITheme.Brass, UITheme.Disc, Movement.Orbit, i * Mathf.PI / 3, 30);
            Add("Orb reflected light", new Vector2(-8, 9), new Vector2(7, 7), UITheme.Opacity(Color.white, .65f), UITheme.Disc, Movement.Float, 1, 1.4f);
        }
        private void BuildWitch()
        {
            Bottle(-34, new Color(.35f, .65f, .52f, .85f), 0);
            Bottle(34, new Color(.69f, .36f, .46f, .85f), 2.4f);
        }
        private void Bottle(float x, Color liquid, float phase)
        {
            var holder = UITheme.Box("Separate potion bottle " + x, art);
            UITheme.Position(holder, UITheme.Center, x, -24, 34, 0); holder.pivot = new Vector2(.5f, 0);
            Track(holder, null, Movement.Tilt, phase, 1.3f);
            if (Art("witch-bottle", new Vector2(0, 24), 48, holder) == null)
            {
                Image("Bottle outline", holder, new Vector2(0, 20), new Vector2(28, 34), UITheme.Opacity(UITheme.Brass, .75f), UITheme.Panel);
                Image("Bottle neck", holder, new Vector2(0, 40), new Vector2(12, 14), UITheme.Brass, UITheme.Panel);
                Image("Bottle cork", holder, new Vector2(0, 48), new Vector2(16, 5), UITheme.Terracotta, UITheme.Panel);
            }
            var window = UITheme.Box("Clipped potion contents", holder);
            UITheme.Position(window, UITheme.Center, 0, 20, 22, 27); window.gameObject.AddComponent<RectMask2D>();
            Image("Fixed potion liquid", window, new Vector2(0, -7), new Vector2(22, 20), liquid, UITheme.Panel);
            for (var i = 0; i < 3; i++)
            {
                var bubble = Image("Potion bubble " + i, window, new Vector2(i * 6 - 6, -10), Vector2.one * 2, UITheme.Parchment, UITheme.Disc);
                Track(bubble.rectTransform, bubble, Movement.Bubble, phase + i * 1.9f, 20);
            }
            var images = holder.GetComponentsInChildren<Image>(true);
            var colors = new Color[images.Length]; for (var i = 0; i < images.Length; i++) colors[i] = images[i].color;
            bottles.Add(new PotionBottle { Images = images, Colors = colors });
        }
        private void BuildWolf()
        {
            var body = Resource("Hub/Layers/wolf-body"); var head = Resource("Hub/Layers/wolf-head");
            if (body != null) Add("Independent wolf body", new Vector2(7, -14), new Vector2(42, 42), Color.white, body);
            if (head != null)
            {
                var piece = Add("Independent wolf head", new Vector2(0, 5), new Vector2(42, 42), Color.white, head, Movement.Tilt, .6f, .9f);
                piece.Rect.pivot = new Vector2(.6f, .16f);
            }
        }
        private void BuildGuard()
        {
            if (Art("guard-shield", Vector2.zero, 59) == null)
            {
                var diamond = Add("Shield pointed lower piece", new Vector2(0, -3), new Vector2(31, 31), UITheme.Brass);
                diamond.Angle = 45;
                Add("Shield face", new Vector2(0, 7), new Vector2(42, 26), UITheme.Brass);
                Add("Shield center fold", new Vector2(0, 0), new Vector2(2, 38), UITheme.Parchment);
            }
            Add("Separate shield reflection", new Vector2(-18, 0), new Vector2(2, 38), UITheme.Opacity(Color.white, .65f), UITheme.Panel, Movement.Glint, 0, 18);
        }
        private void BuildCupid()
        {
            if (Art("cupid-bow", new Vector2(-49, 0), 48) == null)
            {
                for (var i = 0; i < 7; i++)
                {
                    var a = -.9f + i * .3f;
                    var p = Add("Bow arc " + i, new Vector2(-46 + Mathf.Cos(a) * 17, Mathf.Sin(a) * 24), new Vector2(3, 9), UITheme.Brass);
                    p.Angle = -a * Mathf.Rad2Deg;
                }
                Add("Bow string", new Vector2(-35, 0), new Vector2(1, 39), UITheme.Quiet);
            }
            Heart("First linked heart", new Vector2(8, 1), 0);
            Heart("Second linked heart", new Vector2(43, 1), Mathf.PI);
            Add("Lovers connecting thread", new Vector2(25, 0), new Vector2(16, 1), UITheme.Brass);
        }
        private void Heart(string name, Vector2 position, float phase)
        {
            var root = UITheme.Box(name, art); UITheme.Position(root, UITheme.Center, position.x, position.y, 24, 24);
            Track(root, null, Movement.Float, phase, 2);
            var diamond = Image("Heart folded tip", root, new Vector2(0, -3), new Vector2(15, 15), UITheme.Terracotta, UITheme.Panel);
            diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            Image("Heart left lobe", root, new Vector2(-5, 3), new Vector2(14, 14), UITheme.Terracotta, UITheme.Disc);
            Image("Heart right lobe", root, new Vector2(5, 3), new Vector2(14, 14), UITheme.Terracotta, UITheme.Disc);
        }
        private void BuildHunter()
        {
            Art("hunter-crossbow", new Vector2(-46, -2), 46);
            var aim = UITheme.Box("Independent aiming reticle", art); UITheme.Position(aim, UITheme.Center, 8, 0, 54, 54);
            Track(aim, null, Movement.Aim, 0, 2);
            foreach (var sign in new[] { -1, 1 })
            {
                Image("Reticle horizontal " + sign, aim, new Vector2(sign * 19, 0), new Vector2(12, 2), UITheme.Brass, UITheme.Panel);
                Image("Reticle vertical " + sign, aim, new Vector2(0, sign * 19), new Vector2(2, 12), UITheme.Brass, UITheme.Panel);
            }
            Add("Separate hunter bolt", new Vector2(-20, 0), new Vector2(17, 2), UITheme.Parchment, UITheme.Panel, Movement.Bolt, 0, 28);
        }
        private void BuildEye()
        {
            Art("little-girl-eye", Vector2.zero, 62);
            Add("Observing eye", Vector2.zero, new Vector2(49, 22), UITheme.Parchment, UITheme.Disc);
            Add("Eye pupil without player information", Vector2.zero, new Vector2(14, 14), UITheme.Midnight, UITheme.Disc, Movement.Aim, 1, 2);
            Add("Upper paper eyelid", new Vector2(0, 19), new Vector2(52, 16), UITheme.Midnight, UITheme.Panel, Movement.Lid, 1, -12);
            Add("Lower paper eyelid", new Vector2(0, -19), new Vector2(52, 16), UITheme.Midnight, UITheme.Panel, Movement.Lid, 1, 12);
        }
        private void BuildLantern()
        {
            if (Art("villager-lantern", Vector2.zero, 62) == null)
            {
                Add("Anchored lantern frame", Vector2.zero, new Vector2(28, 37), UITheme.Brass);
                Add("Lantern glass", Vector2.zero, new Vector2(21, 28), UITheme.Midnight);
                Add("Lantern handle", new Vector2(0, 25), new Vector2(15, 8), UITheme.Brass, UITheme.Panel);
            }
            Add("Anchored lantern flame", new Vector2(0, -3), new Vector2(8, 15), new Color(1, .69f, .27f, .9f), UITheme.Disc, Movement.Flame, .5f, .12f);
        }

        private Piece Add(string name, Vector2 position, Vector2 size, Color color, Sprite sprite = null,
            Movement motion = Movement.Still, float phase = 0, float amplitude = 0)
        {
            var image = Image(name, art, position, size, color, sprite ?? UITheme.Panel);
            return Track(image.rectTransform, image, motion, phase, amplitude);
        }
        private Piece Track(RectTransform rect, Image image, Movement motion, float phase, float amplitude)
        {
            var piece = new Piece { Rect = rect, Image = image, Rest = rect.anchoredPosition,
                Color = image != null ? image.color : Color.white, Movement = motion, Phase = phase, Amplitude = amplitude };
            pieces.Add(piece); return piece;
        }
        private static Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color, Sprite sprite)
        {
            var image = UITheme.Surface(name, parent, color, sprite); image.preserveAspect = true; image.raycastTarget = false;
            UITheme.Position(image.rectTransform, UITheme.Center, position.x, position.y, size.x, size.y); return image;
        }
        private Image Art(string name, Vector2 position, float extent, Transform parent = null)
        {
            var sprite = Resource("Werewolf/Actions/" + name);
            return sprite != null ? Image("Separate illustrated " + name, parent != null ? parent : art, position,
                Vector2.one * extent, Color.white, sprite) : null;
        }
        private Sprite Resource(string path)
        {
            var sprite = Resources.Load<Sprite>(path); if (sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>(path); if (texture == null) return null;
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            ownedSprites.Add(sprite); return sprite;
        }
        private void LateUpdate()
        {
            if (!hasBound) return;
            if (!Quiet) { clock += Time.unscaledDeltaTime; burst = Mathf.Max(0, burst - Time.unscaledDeltaTime / .65f); }
            else burst = 0;
            Render();
        }
        private void Render()
        {
            if (art != null)
            {
                var height = ((RectTransform)transform).rect.height;
                art.localScale = Vector3.one * Mathf.Clamp(height / 80f, .25f, 1f);
            }
            var t = Quiet ? 0 : clock;
            foreach (var piece in pieces)
            {
                if (piece.Rect == null) continue;
                var position = piece.Rest; var angle = piece.Angle; var color = piece.Color;
                if (Quiet && piece.Movement == Movement.Orbit)
                    position += new Vector2(Mathf.Cos(piece.Phase), Mathf.Sin(piece.Phase)) * piece.Amplitude;
                if (!Quiet)
                {
                    var wave = Mathf.Sin(t * .7f + piece.Phase);
                    switch (piece.Movement)
                    {
                        case Movement.Float: position.y += wave * piece.Amplitude; break;
                        case Movement.Tilt: angle += Mathf.Sin(t * .5f + piece.Phase) * piece.Amplitude; break;
                        case Movement.Orbit: position += new Vector2(Mathf.Cos(t * .5f + piece.Phase), Mathf.Sin(t * .5f + piece.Phase)) * piece.Amplitude; break;
                        case Movement.Bubble: position.y += Mathf.Repeat(t * 6 + piece.Phase * 4, piece.Amplitude); break;
                        case Movement.Glint: position.x += (wave + 1) * piece.Amplitude; color.a *= .35f + .45f * (wave + 1) / 2; break;
                        case Movement.Aim: position += new Vector2(wave, Mathf.Cos(t * .53f + piece.Phase)) * piece.Amplitude; break;
                        case Movement.Lid: position.y += Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * .45f + piece.Phase)), 20) * piece.Amplitude; break;
                        case Movement.Flame: color.a *= .87f + wave * piece.Amplitude; break;
                        case Movement.Bolt: position.x += burst > 0 ? (1 - burst) * piece.Amplitude : 0; break;
                    }
                }
                piece.Rect.anchoredPosition = position; piece.Rect.localRotation = Quaternion.Euler(0, 0, angle);
                if (piece.Image != null) piece.Image.color = color;
            }
            foreach (var bottle in bottles)
                for (var i = 0; i < bottle.Images.Length; i++)
                {
                    var image = bottle.Images[i]; if (image == null) continue;
                    image.color = bottle.Available ? bottle.Colors[i] : new Color(.45f, .45f, .45f, bottle.Colors[i].a * .35f);
                    if (image.name.StartsWith("Potion bubble ")) image.enabled = bottle.Available;
                }
            for (var i = 0; i < confirmation.Count; i++)
            {
                var dot = confirmation[i]; var angle = i * Mathf.PI / 4;
                dot.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (18 + (1 - burst) * 27);
                var receiptColor = UITheme.Brass;
                if (VisualKind == "witch") receiptColor = acceptedChoice == "poison" || acceptedChoice == "both" && i % 2 == 1 ? PoisonColor : HealingColor;
                dot.color = UITheme.Opacity(receiptColor, Quiet ? 0 : burst * .8f);
            }
        }
        private void OnDisable() { burst = 0; Render(); }
        private void OnDestroy() { foreach (var sprite in ownedSprites) if (sprite != null) Destroy(sprite); }
    }
}
