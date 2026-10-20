using System.Collections.Generic;
using UnityEngine;

namespace NGame.Presentation
{
    /// <summary>A real orthographic scene, with independently moving paper layers behind the interface.</summary>
    public sealed class VillageStage : MonoBehaviour
    {
        private sealed class Layer
        {
            public SpriteRenderer Renderer;
            public bool Cover;
            public float Scale;
            public Vector2 Anchor;
            public Vector2 Drift;
            public Vector3 RestScale;
        }

        private sealed class Lamp
        {
            public SpriteRenderer Anchor;
            public Vector2 ImageUv;
            public SpriteRenderer Halo;
            public SpriteRenderer Flame;
            public float Radius;
            public float Seed;
            public float Strength;
        }

        private readonly List<Layer> layers = new List<Layer>();
        private readonly List<SpriteRenderer> fireflies = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> fog = new List<SpriteRenderer>();
        private readonly List<Lamp> lamps = new List<Lamp>();
        private Camera stageCamera;
        private Material paperMaterial;
        private Material riverMaterial;
        private Material glowMaterial;
        private Sprite glowSprite;
        private Texture2D glowTexture;
        private WolfPaperPuppet wolfPuppet;
        private LivingSky sky;
        private VillageDetails details;
        private Layer night;
        private Layer dawn;
        private Layer wolf;
        private Layer foreground;
        private float daylight;
        private float time;
        private bool quiet;
        private bool home;
        private int lastWidth;
        private int lastHeight;
        private float firstLampIntensity;
        private float lightFrom;
        private float lightTarget;
        private float lightElapsed;
        private bool changingLight;

        public float AmbientClock => time;
        public float DaylightAmount => daylight;
        public int ActiveLampCount => daylight < .99f ? lamps.Count : 0;
        public bool WaterAnimationEnabled => riverMaterial != null && !quiet;
        public bool LampFlickerEnabled => !quiet && daylight < .99f;
        public float FirstLampIntensity => firstLampIntensity;
        public bool WolfArticulated => wolfPuppet != null && wolfPuppet.IsReady;
        public int WolfMeshVertexCount => wolfPuppet != null ? wolfPuppet.VertexCount : 0;
        public float WolfHeadAngleDegrees => wolfPuppet != null ? wolfPuppet.HeadAngleDegrees : 0;
        public float WolfLeftEarAngleDegrees => wolfPuppet != null ? wolfPuppet.LeftEarAngleDegrees : 0;
        public float WolfRightEarAngleDegrees => wolfPuppet != null ? wolfPuppet.RightEarAngleDegrees : 0;
        public float WolfEyeGlow => wolfPuppet != null ? wolfPuppet.EyeGlow : 0;
        public bool MoonIndependent => sky != null && sky.IsReady;
        public Vector3 MoonPosition => sky != null ? sky.MoonPosition : Vector3.zero;
        public float CloudDisplacement => sky != null ? sky.CloudDisplacement : 0;
        public VillageDetails Details => details;

        public void Initialize()
        {
            stageCamera = Camera.main;
            if (stageCamera == null)
            {
                var cameraObject = new GameObject("Storybook camera", typeof(Camera), typeof(AudioListener));
                cameraObject.transform.SetParent(transform, false);
                cameraObject.tag = "MainCamera";
                stageCamera = cameraObject.GetComponent<Camera>();
            }
            stageCamera.orthographic = true;
            stageCamera.orthographicSize = 4.66f;
            stageCamera.transform.position = new Vector3(0, 0, -10);
            stageCamera.clearFlags = CameraClearFlags.SolidColor;
            stageCamera.backgroundColor = UITheme.Midnight;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            if (shader != null) paperMaterial = new Material(shader) { name = "Unlit paper diorama" };
            var riverShader = Resources.Load<Shader>("Werewolf/Shader/PaperRiver");
            if (riverShader != null) riverMaterial = new Material(riverShader) { name = "Localized river surface" };
            var glowShader = Resources.Load<Shader>("Werewolf/Shader/AmberGlow");
            if (glowShader != null) glowMaterial = new Material(glowShader) { name = "Individual amber light halos" };
            var movingMoonReady = UITheme.Art("moon") != null && UITheme.Art("village-night-sky") != null;
            night = AddLayer("Village at midnight", UITheme.Art(movingMoonReady ? "village-night-sky" : "village-night"), 0, true, 1.035f, Vector2.zero, Vector2.zero);
            dawn = AddLayer("Village at sunrise", UITheme.Art("village-dawn"), 1, true, 1.035f, Vector2.zero, Vector2.zero);
            if (riverMaterial != null)
            {
                night.Renderer.sharedMaterial = riverMaterial;
                dawn.Renderer.sharedMaterial = riverMaterial;
            }
            CreateGlowTexture();
            if (movingMoonReady)
            {
                sky = gameObject.AddComponent<LivingSky>();
                sky.Initialize(night.Renderer, paperMaterial, glowSprite, glowMaterial);
            }
            BuildWindowLights();
            details = gameObject.AddComponent<VillageDetails>();
            details.Initialize(night.Renderer, paperMaterial, glowSprite, glowMaterial);
            wolf = AddLayer("Folded wolf", UITheme.Art("wolf"), 4, false, 2.9f, new Vector2(-.34f, -.02f), new Vector2(.012f, .008f));
            wolfPuppet = wolf.Renderer.gameObject.AddComponent<WolfPaperPuppet>();
            wolfPuppet.Initialize(wolf.Renderer);
            for (var i = 0; i < 3; i++)
            {
                var mist = AddLayer("River mist " + i, UITheme.Mist, 5 + i, false, .9f,
                    new Vector2(i % 2 == 0 ? -.2f : .2f, -.3f + i * .2f), new Vector2(.4f, .04f));
                mist.Renderer.color = new Color(.61f, .7f, .77f, .115f + i * .012f);
                fog.Add(mist.Renderer);
            }
            foreground = AddLayer("Near paper foliage", UITheme.Art("foreground"), 9, true, 1.045f,
                Vector2.zero, new Vector2(.1f, .04f));
            for (var i = 0; i < 12; i++)
            {
                var firefly = AddLayer("Floating golden fibre " + i, UITheme.Disc, 8, false, .025f + i % 3 * .005f,
                    new Vector2(-.42f + i * .189f % .84f, -.35f + i * .137f % .74f), new Vector2(.08f, .12f));
                firefly.Renderer.color = UITheme.Opacity(UITheme.Brass, .4f);
                fireflies.Add(firefly.Renderer);
            }
            Layout();
            SetDaylight(0);
            AnimateLights();
        }

        private Layer AddLayer(string label, Sprite sprite, int order, bool cover, float scale, Vector2 anchor, Vector2 drift)
        {
            var node = new GameObject(label);
            node.transform.SetParent(transform, false);
            var renderer = node.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            if (paperMaterial != null) renderer.sharedMaterial = paperMaterial;
            var layer = new Layer { Renderer = renderer, Cover = cover, Scale = scale, Anchor = anchor, Drift = drift };
            layers.Add(layer);
            return layer;
        }

        public void SetDaylight(float amount)
        {
            changingLight = false;
            ApplyDaylight(amount);
        }

        public void TransitionToDaylight(float amount)
        {
            var target = Mathf.Clamp01(amount);
            if ((changingLight && Mathf.Approximately(lightTarget, target))
                || (!changingLight && Mathf.Approximately(daylight, target))) return;
            lightFrom = daylight;
            lightTarget = target;
            lightElapsed = 0;
            changingLight = true;
        }

        private void ApplyDaylight(float amount)
        {
            daylight = Mathf.Clamp01(amount);
            if (dawn != null) dawn.Renderer.color = new Color(1, 1, 1, daylight);
            if (wolf != null) wolf.Renderer.color = new Color(1, 1, 1, (1 - daylight) * (home ? 0 : 1));
            if (foreground != null) foreground.Renderer.color = Color.Lerp(Color.white, new Color(1.14f, 1.1f, 1.02f), daylight);
            if (riverMaterial != null) riverMaterial.SetFloat("_Reflection", Mathf.Lerp(1, .18f, daylight));
            if (wolfPuppet != null) wolfPuppet.Evaluate(time, quiet, wolf.Renderer.color);
            if (sky != null) sky.Evaluate(time, quiet, daylight);
            if (details != null) details.Evaluate(time, quiet, daylight);
            AnimateLights();
        }

        public void SetMotionReduced(bool enabled)
        {
            quiet = enabled;
            foreach (var firefly in fireflies) firefly.gameObject.SetActive(!enabled);
            if (riverMaterial != null) riverMaterial.SetFloat("_Motion", enabled ? 0 : 1);
            if (enabled) Layout();
            if (wolfPuppet != null) wolfPuppet.Evaluate(time, enabled, wolf.Renderer.color);
            if (sky != null) sky.Evaluate(time, enabled, daylight);
            if (details != null) details.Evaluate(time, enabled, daylight);
            AnimateLights();
        }

        public void SetHomeComposition(bool enabled) { home = enabled; SetDaylight(daylight); }

        private void Layout()
        {
            if (stageCamera == null) return;
            var height = stageCamera.orthographicSize * 2;
            var width = height * stageCamera.aspect;
            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (layer.Renderer.sprite == null) continue;
                var source = layer.Renderer.sprite.bounds.size;
                var scale = layer.Cover ? Mathf.Max(width / source.x, height / source.y) * layer.Scale
                    : layer.Scale / source.y;
                layer.Renderer.transform.localScale = Vector3.one * scale;
                layer.RestScale = Vector3.one * scale;
                layer.Renderer.transform.localPosition = new Vector3(layer.Anchor.x * width, layer.Anchor.y * height, 0);
            }
            PositionLights();
            lastWidth = Screen.width;
            lastHeight = Screen.height;
        }

        private void LateUpdate()
        {
            // Script reload can restore Unity object references before the runtime-only layer graph.
            // A fresh Play session reconstructs the graph; never dereference a partially restored stage.
            if (stageCamera == null || night?.Renderer == null || wolf?.Renderer == null) return;
            if (lastWidth != Screen.width || lastHeight != Screen.height) Layout();
            if (changingLight)
            {
                lightElapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(lightElapsed / (quiet ? .28f : 3.4f));
                ApplyDaylight(Mathf.Lerp(lightFrom, lightTarget, Mathf.SmoothStep(0, 1, progress)));
                if (progress >= 1) changingLight = false;
            }
            if (quiet)
            {
                PositionLights();
                if (sky != null) sky.Evaluate(time, true, daylight);
                if (details != null) details.Evaluate(time, true, daylight);
                return;
            }
            time += Time.unscaledDeltaTime;
            if (riverMaterial != null) riverMaterial.SetFloat("_Clock", time);
            var height = stageCamera.orthographicSize * 2;
            var width = height * stageCamera.aspect;
            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                var speed = layer.Cover ? .095f : .1f + i * .008f;
                var x = layer.Anchor.x * width + Mathf.Sin(time * speed + (layer.Cover ? 0 : i)) * layer.Drift.x;
                var y = layer.Anchor.y * height + Mathf.Cos(time * speed * .7f + (layer.Cover ? 0 : i)) * layer.Drift.y;
                layer.Renderer.transform.localPosition = new Vector3(x, y, 0);
            }
            if (wolf != null && !WolfArticulated)
            {
                var breath = Mathf.Sin(time * 1.23f);
                wolf.Renderer.transform.localScale = Vector3.Scale(wolf.RestScale,
                    new Vector3(1 + breath * .0035f, 1 + breath * .009f, 1));
                wolf.Renderer.transform.localEulerAngles = new Vector3(0, 0, Mathf.Sin(time * .34f) * .48f);
            }
            if (WolfArticulated) wolfPuppet.Evaluate(time, false, wolf.Renderer.color);
            if (sky != null) sky.Evaluate(time, false, daylight);
            if (details != null) details.Evaluate(time, false, daylight);
            for (var i = 0; i < fireflies.Count; i++) fireflies[i].color = UITheme.Opacity(UITheme.Brass,
                (.24f + Mathf.Sin(time * .65f + i) * .13f) * (1 - daylight * .7f));
            AnimateLights();
            PositionLights();
        }

        private void CreateGlowTexture()
        {
            const int size = 128;
            glowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { name = "Soft amber point glow", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var radius = new Vector2((x - 63.5f) / 64, (y - 63.5f) / 64).sqrMagnitude;
                    var alpha = Mathf.Exp(-radius * 5.7f) * Mathf.Clamp01((1 - radius) * 4);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            glowTexture.SetPixels(pixels);
            glowTexture.Apply(false, true);
            glowSprite = Sprite.Create(glowTexture, new Rect(0, 0, size, size), UITheme.Center, size);
        }

        private void BuildWindowLights()
        {
            if (night.Renderer.sprite == null) return;
            // Coordinates refer to the original illustration, before cover cropping or device aspect changes.
            AddLamp("Left cottage high window", night.Renderer, new Vector2(.278f, .615f), .35f, .78f);
            AddLamp("Left cottage low window", night.Renderer, new Vector2(.29f, .567f), .32f, .72f);
            AddLamp("Left village doorway", night.Renderer, new Vector2(.342f, .518f), .41f, .9f);
            AddLamp("Middle cottage", night.Renderer, new Vector2(.568f, .506f), .37f, .84f);
            AddLamp("Middle cottage doorway", night.Renderer, new Vector2(.583f, .477f), .33f, .72f);
            AddLamp("Church tower window", night.Renderer, new Vector2(.861f, .656f), .43f, .92f);
            AddLamp("Church lower window", night.Renderer, new Vector2(.856f, .579f), .35f, .78f);
            AddLamp("Right village arch", night.Renderer, new Vector2(.812f, .463f), .44f, .82f);
            AddLamp("Riverbank lantern", night.Renderer, new Vector2(.154f, .328f), .66f, 1.05f);
            AddLamp("Bridge light", night.Renderer, new Vector2(.802f, .352f), .57f, .95f);
        }

        private void AddLamp(string name, SpriteRenderer anchor, Vector2 uv, float radius, float strength, int order = 3)
        {
            var light = new Lamp
            {
                Anchor = anchor, ImageUv = uv, Radius = radius, Strength = strength,
                Seed = lamps.Count * 2.719f + .67f,
                Halo = GlowRenderer(name + " - warm surrounding light", order),
                Flame = GlowRenderer(name + " - flame heart", order + 1)
            };
            light.Halo.transform.localScale = Vector3.one * radius;
            light.Flame.transform.localScale = new Vector3(radius * .14f, radius * .23f, 1);
            lamps.Add(light);
        }

        private SpriteRenderer GlowRenderer(string name, int order)
        {
            var node = new GameObject(name);
            node.transform.SetParent(transform, false);
            var renderer = node.AddComponent<SpriteRenderer>();
            renderer.sprite = glowSprite;
            renderer.sortingOrder = order;
            renderer.sharedMaterial = glowMaterial != null ? glowMaterial : paperMaterial;
            return renderer;
        }

        private void PositionLights()
        {
            foreach (var lamp in lamps)
            {
                if (lamp.Anchor == null || lamp.Anchor.sprite == null) continue;
                var bounds = lamp.Anchor.sprite.bounds;
                var local = new Vector3(bounds.min.x + bounds.size.x * lamp.ImageUv.x,
                    bounds.min.y + bounds.size.y * lamp.ImageUv.y, 0);
                var point = lamp.Anchor.transform.TransformPoint(local);
                lamp.Halo.transform.position = point;
                lamp.Flame.transform.position = point;
            }
        }

        private void AnimateLights()
        {
            var nightStrength = 1 - Mathf.SmoothStep(0, 1, daylight);
            for (var i = 0; i < lamps.Count; i++)
            {
                var lamp = lamps[i];
                var pulse = quiet ? 1 : .88f + Mathf.Sin(time * 5.3f + lamp.Seed) * .16f
                    + Mathf.Sin(time * 12.7f + lamp.Seed * 1.7f) * .07f
                    + Mathf.Sin(time * 23.1f + lamp.Seed) * .03f;
                var intensity = pulse * lamp.Strength * nightStrength;
                lamp.Halo.color = new Color(1, .49f, .13f, intensity * .3f);
                lamp.Flame.color = new Color(1, .78f, .34f, intensity * .78f);
                lamp.Halo.transform.localScale = Vector3.one * lamp.Radius * (quiet ? 1 : .97f + pulse * .05f);
                lamp.Flame.transform.localScale = new Vector3(lamp.Radius * .14f,
                    lamp.Radius * .23f * (quiet ? 1 : .72f + pulse * .34f), 1);
                if (i == 0) firstLampIntensity = intensity;
            }
        }

        private void OnDestroy()
        {
            if (paperMaterial != null) Destroy(paperMaterial);
            if (riverMaterial != null) Destroy(riverMaterial);
            if (glowMaterial != null) Destroy(glowMaterial);
            if (glowSprite != null) Destroy(glowSprite);
            if (glowTexture != null) Destroy(glowTexture);
        }
    }
}
