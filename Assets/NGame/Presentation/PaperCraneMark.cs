using NGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NGame.Presentation
{
    /// <summary>The terracotta folded-paper bird used by the approved N’GAME reference.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PaperCraneMark : MaskableGraphic
    {
        private AppPreferences preferences;
        private float clock;
        private bool wasReduced;
        public float AnimationClock => clock;

        public void Initialize(AppPreferences settings)
        {
            preferences = settings;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
        }

        private void Update()
        {
            var reduced = preferences != null && preferences.ReducedMotion;
            if (!reduced) { clock += Time.unscaledDeltaTime; SetVerticesDirty(); }
            else if (!wasReduced) SetVerticesDirty();
            wasReduced = reduced;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            var width = Mathf.Min(rect.width, rect.height * 1.35f);
            var reduced = preferences != null && preferences.ReducedMotion;
            var fold = reduced ? 0 : Mathf.Sin(clock * .9f) * .018f;
            var tail = new Vector2(-.5f, .25f);
            var shoulder = new Vector2(-.17f, .1f);
            var breast = new Vector2(-.08f, -.27f);
            var wing = new Vector2(.1f, .47f + fold);
            var ridge = new Vector2(.28f, .28f);
            var throat = new Vector2(.055f, -.19f);
            var head = new Vector2(.36f, .46f);
            var beak = new Vector2(.5f, .39f);
            Triangle(mesh, rect, width, tail, shoulder, breast, new Color32(173, 73, 47, 255));
            Triangle(mesh, rect, width, shoulder, wing, breast, new Color32(197, 100, 66, 255));
            Triangle(mesh, rect, width, breast, wing, ridge, new Color32(148, 58, 39, 255));
            Triangle(mesh, rect, width, breast, ridge, throat, new Color32(188, 87, 56, 255));
            Triangle(mesh, rect, width, throat, ridge, head, new Color32(165, 67, 43, 255));
            Triangle(mesh, rect, width, ridge, head, beak, new Color32(208, 120, 80, 255));
        }

        private void Triangle(VertexHelper mesh, Rect rect, float width,
            Vector2 a, Vector2 b, Vector2 c, Color shade)
        {
            var start = mesh.currentVertCount;
            var tint = shade * color;
            mesh.AddVert(Point(rect, width, a), tint, Vector2.zero);
            mesh.AddVert(Point(rect, width, b), tint, Vector2.zero);
            mesh.AddVert(Point(rect, width, c), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }

        private static Vector3 Point(Rect rect, float width, Vector2 point) =>
            new Vector3(rect.center.x + point.x * width, rect.center.y + (point.y - .1f) * width, 0);
    }
}
