using System.Collections.Generic;
using UnityEngine;

namespace NGame.Presentation
{
    /// <summary>Independent paper moon and clouds, positioned in the village illustration's coordinate space.</summary>
    public sealed class LivingSky : MonoBehaviour
    {
        private SpriteRenderer village;
        private SpriteRenderer moon;
        private SpriteRenderer halo;
        private readonly List<SpriteRenderer> clouds = new List<SpriteRenderer>();
        private readonly List<Sprite> slices = new List<Sprite>();
        public bool IsReady => moon != null;
        public Vector3 MoonPosition => moon != null ? moon.transform.position : Vector3.zero;
        public float CloudDisplacement { get; private set; }

        public void Initialize(SpriteRenderer anchor, Material paper, Sprite softGlow, Material glow)
        {
            village = anchor;
            var illustration = UITheme.Art("moon");
            if (illustration == null) return;
            moon = Add("Independent paper moon", illustration, paper, 2);
            halo = Add("Moon amber atmosphere", softGlow, glow, 1);
            var cloudStrip = UITheme.Art("clouds");
            if (cloudStrip != null)
            {
                var regions = new[]
                {
                    new Rect(16f / 2172, 233f / 724, 742f / 2172, 250f / 724),
                    new Rect(772f / 2172, 217f / 724, 645f / 2172, 271f / 724),
                    new Rect(1438f / 2172, 207f / 724, 722f / 2172, 331f / 724)
                };
                for (var i = 0; i < 3; i++)
                {
                    var cell = cloudStrip.rect;
                    var region = regions[i];
                    var piece = Sprite.Create(cloudStrip.texture,
                        new Rect(cell.x + cell.width * region.x, cell.y + cell.height * region.y,
                            cell.width * region.width, cell.height * region.height),
                        new Vector2(.5f, .5f), cloudStrip.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                    slices.Add(piece);
                    clouds.Add(Add("Drifting paper cloud " + i, piece, paper, 3));
                }
            }
            Evaluate(0, false, 0);
        }

        private SpriteRenderer Add(string label, Sprite sprite, Material material, int order)
        {
            var node = new GameObject(label);
            node.transform.SetParent(transform, false);
            var renderer = node.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            return renderer;
        }

        private Vector3 Point(float u, float v)
        {
            var bounds = village.sprite.bounds;
            return village.transform.TransformPoint(new Vector3(
                bounds.min.x + bounds.size.x * u, bounds.min.y + bounds.size.y * v, 0));
        }

        public void Evaluate(float seconds, bool reducedMotion, float daylight)
        {
            if (!IsReady || village == null) return;
            var clock = reducedMotion ? 0 : seconds;
            var visibility = 1 - Mathf.Clamp01(daylight);
            var diameter = village.bounds.size.y * .134f;
            moon.transform.position = Point(.735f + Mathf.Sin(clock * .13f) * .018f,
                .875f + Mathf.Sin(clock * .17f) * .009f);
            moon.transform.localScale = Vector3.one * diameter / moon.sprite.bounds.size.y;
            moon.transform.localEulerAngles = new Vector3(0, 0, Mathf.Sin(clock * .11f) * 4);
            moon.color = new Color(1, 1, 1, visibility);
            halo.transform.position = moon.transform.position;
            halo.transform.localScale = Vector3.one * diameter * 1.8f;
            halo.color = new Color(1, .66f, .25f,
                visibility * (reducedMotion ? .12f : .12f + Mathf.Sin(clock * .36f) * .025f));
            CloudDisplacement = reducedMotion ? 0 : Mathf.Sin(clock * .095f) * .065f;
            for (var i = 0; i < clouds.Count; i++)
            {
                var cloud = clouds[i];
                var u = .55f + i * .16f + CloudDisplacement * (i % 2 == 0 ? 1 : -.65f);
                var v = .84f + i * .045f + Mathf.Sin(clock * .14f + i) * (reducedMotion ? 0 : .004f);
                cloud.transform.position = Point(u, v);
                cloud.transform.localScale = Vector3.one * (diameter * (1.25f + i * .12f)) / cloud.sprite.bounds.size.x;
                cloud.color = new Color(1, 1, 1, visibility);
            }
        }

        private void OnDestroy()
        {
            foreach (var slice in slices) if (slice != null) Destroy(slice);
        }
    }
}
