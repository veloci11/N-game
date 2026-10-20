using System.Collections.Generic;
using UnityEngine;

namespace NGame.Presentation
{
    /// <summary>Small effects stay anchored to the illustrated chimney and stars, including after a resize.</summary>
    public sealed class VillageDetails : MonoBehaviour
    {
        private SpriteRenderer village;
        private readonly List<SpriteRenderer> smoke = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> stars = new List<SpriteRenderer>();
        private readonly Vector2[] starUv =
        {
            new Vector2(.454f, .963f), new Vector2(.647f, .96f), new Vector2(.95f, .916f),
            new Vector2(.666f, .798f), new Vector2(.257f, .771f)
        };
        public int ChimneyPuffCount => smoke.Count;
        public int TwinklingStarCount => stars.Count;
        public Vector3 FirstSmokePosition => smoke.Count == 0 ? Vector3.zero : smoke[0].transform.position;
        public float FirstStarIntensity => stars.Count == 0 ? 0 : stars[0].color.a;

        public void Initialize(SpriteRenderer anchor, Material paper, Sprite glow, Material amber)
        {
            village = anchor;
            for (var i = 0; i < 7; i++) smoke.Add(Add("Chimney breath " + i, UITheme.Mist, paper, 3));
            for (var i = 0; i < starUv.Length; i++) stars.Add(Add("Starlight on paper " + i, glow, amber, 3));
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
            return village.transform.TransformPoint(new Vector3(bounds.min.x + bounds.size.x * u,
                bounds.min.y + bounds.size.y * v, 0));
        }

        public void Evaluate(float seconds, bool reducedMotion, float daylight)
        {
            if (village == null || village.sprite == null) return;
            var clock = reducedMotion ? 0 : seconds;
            for (var i = 0; i < smoke.Count; i++)
            {
                var progress = Mathf.Repeat(clock * .045f + i / (float)smoke.Count, 1);
                var puff = smoke[i];
                var u = .244f + progress * .035f + Mathf.Sin(clock * .18f + i) * .002f * progress;
                var v = .663f + progress * .063f;
                puff.transform.position = Point(u, v);
                var size = village.bounds.size.y * Mathf.Lerp(.008f, .038f, progress);
                puff.transform.localScale = new Vector3(size * 1.45f, size, 1);
                puff.transform.localEulerAngles = new Vector3(0, 0, -progress * 24);
                var alpha = Mathf.Sin(progress * Mathf.PI) * .09f;
                puff.color = Color.Lerp(new Color(.57f, .65f, .74f, alpha),
                    new Color(.77f, .72f, .63f, alpha * .7f), daylight);
                // Reduced motion keeps the scenery calm without freezing visible particles in mid-air.
                puff.enabled = !reducedMotion;
            }
            for (var i = 0; i < stars.Count; i++)
            {
                var star = stars[i];
                star.transform.position = Point(starUv[i].x, starUv[i].y);
                var pulse = reducedMotion ? .12f : .11f + Mathf.Sin(clock * (.43f + i * .073f) + i * 2.1f) * .055f;
                star.color = new Color(1, .72f, .35f, pulse * (1 - daylight));
                star.transform.localScale = Vector3.one * village.bounds.size.y * .022f;
            }
        }
    }
}
