using UnityEngine;
using UnityEngine.Rendering;

namespace NGame.Presentation
{
    /// <summary>Two separately illustrated paper pieces: a fixed body and a rigid head turning at its neck.</summary>
    public sealed class IndependentWolfPuppet : MonoBehaviour
    {
        private SpriteRenderer source, body, head;
        private Sprite ownedBody, ownedHead;
        private Transform assembly;
        private bool reducedMotion, sourceWasEnabled;
        private float clock;
        public bool IsReady => body != null && head != null && body.sprite != null && head.sprite != null;
        public float HeadAngleDegrees { get; private set; }

        public bool Initialize(SpriteRenderer illustration)
        {
            if (IsReady) return true;
            if (illustration == null || illustration.sprite == null) return false;
            var bodyArt = LoadCutout("wolf-body", Vector2.one * .5f);
            var headArt = LoadCutout("wolf-head", new Vector2(.6f, .16f));
            if (bodyArt == null || headArt == null)
            {
                if (bodyArt != null) Destroy(bodyArt);
                if (headArt != null) Destroy(headArt);
                return false;
            }
            source = illustration; sourceWasEnabled = source.enabled;
            ownedBody = bodyArt; ownedHead = headArt;
            var stage = new GameObject("Independent wolf paper parts");
            assembly = stage.transform; assembly.SetParent(source.transform, false);
            // The generated wolf looks left. Mirror each piece and its joint position to face the village.
            body = MakePart("Fixed wolf body", bodyArt, new Vector2(0, -.30f), 1f, 0);
            head = MakePart("Rigid wolf head at neck", headArt, new Vector2(.16f, .05f), .90f, 1);
            body.flipX = head.flipX = true;
            var bounds = PartBounds(body); bounds.Encapsulate(PartBounds(head));
            var target = source.sprite.bounds;
            var fit = Mathf.Min(target.size.x / bounds.size.x, target.size.y / bounds.size.y);
            assembly.localScale = Vector3.one * fit;
            assembly.localPosition = target.center - bounds.center * fit;
            source.enabled = false;
            SetTint(source.color); Render();
            return true;
        }

        private static Sprite LoadCutout(string filename, Vector2 pivot)
        {
            var path = "Hub/Layers/" + filename;
            var imported = Resources.Load<Sprite>(path);
            var texture = imported != null ? imported.texture : Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            var rect = imported != null ? imported.rect : new Rect(0, 0, texture.width, texture.height);
            var sprite = Sprite.Create(texture, rect, pivot, 100, 0, SpriteMeshType.FullRect);
            sprite.name = "Independent village " + filename;
            return sprite;
        }

        private SpriteRenderer MakePart(string name, Sprite sprite, Vector2 position, float width, int order)
        {
            var piece = new GameObject(name, typeof(SpriteRenderer));
            piece.transform.SetParent(assembly, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = Vector3.one * (width / sprite.bounds.size.x);
            var renderer = piece.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            // The source's existing unlit sprite material is borrowed, never mutated or destroyed.
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder + order;
            renderer.maskInteraction = source.maskInteraction;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Bounds PartBounds(SpriteRenderer part)
        {
            var bounds = part.sprite.bounds;
            var scale = part.transform.localScale;
            var center = bounds.center;
            if (part.flipX) center.x = -center.x;
            return new Bounds(part.transform.localPosition + Vector3.Scale(center, scale), Vector3.Scale(bounds.size, scale));
        }

        public void SetReducedMotion(bool enabled) { reducedMotion = enabled; Render(); }
        public void SetTint(Color color)
        { if (body != null) body.color = color; if (head != null) head.color = color; }

        private void LateUpdate()
        {
            if (!IsReady) return;
            if (!reducedMotion) clock += Time.unscaledDeltaTime;
            Render();
        }
        private void Render()
        {
            HeadAngleDegrees = reducedMotion ? 0 : Mathf.Sin(clock * .30f) * .9f;
            if (head != null) head.transform.localRotation = Quaternion.Euler(0, 0, HeadAngleDegrees);
        }
        private void OnDestroy()
        {
            if (source != null) source.enabled = sourceWasEnabled;
            if (assembly != null) Destroy(assembly.gameObject);
            if (ownedBody != null) Destroy(ownedBody);
            if (ownedHead != null) Destroy(ownedHead);
        }
    }
}
