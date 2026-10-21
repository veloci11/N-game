using System;
using System.Collections.Generic;
using NGame.Core.Multiplayer;
using UnityEngine;
using UnityEngine.Rendering;

namespace NGame.Presentation
{
    /// <summary>A village assembled from separate objects. No complete-village texture or UV displacement.</summary>
    public sealed class LayeredVillageScene : MonoBehaviour
    {
        private sealed class Part
        {
            public string Id, Asset;
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public float Height;
            public Color Night = new Color(.48f, .56f, .72f), Day = new Color(1.08f, 1.06f, .98f);
        }
        private sealed class Lamp
        {
            public Part Building;
            public Vector2 Uv;
            public SpriteRenderer Halo, Flame;
            public SpriteMask Mask;
            public Vector2 Opening = new Vector2(.10f, .11f);
            public float Radius, Phase, Intensity;
            public Color Color;
        }
        private readonly List<Part> parts = new List<Part>();
        private readonly List<Lamp> lamps = new List<Lamp>();
        private readonly List<SpriteRenderer> reflections = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> stars = new List<SpriteRenderer>();
        private readonly List<Sprite> ownedSprites = new List<Sprite>();
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private readonly Dictionary<Part, Material> windowMaterials = new Dictionary<Part, Material>();
        private readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();
        private readonly List<Part> clouds = new List<Part>();
        private readonly Dictionary<string, Part> publicSymbols = new Dictionary<string, Part>();
        private readonly List<SpriteRenderer> publicConstellation = new List<SpriteRenderer>();
        private Camera stageCamera;
        private Material paperMaterial, geometryMaterial;
        private Mesh skyMesh, riverMesh;
        private MeshRenderer riverRenderer;
        private Part moon, walkingWolf;
        private Sprite[] wolfFrames, hearthFrames;
        public int HearthFrameCount => hearthFrames?.Length ?? 0;
        public int GetHearthFrameIndex(int window) => reduced ? 0 : Mathf.FloorToInt(clock * (6.2f + window * .09f) + window * 1.37f) % 4;
        private IndependentWolfPuppet restingWolf;
        private Part witchSecondBottle;
        private readonly Color[] skyColors = new Color[10];
        private readonly Color[] riverColors = new Color[36];
        private float clock, daylight, nextLoad, lastAspect = -1, lastGeometryDaylight = -1;
        private bool initialized, reduced, home;
        private string publicPhase, publicStep, publicKey;
        private double publicElapsedAtBind, localTimeAtBind;
        private long lastPublicServerTime;

        public float Clock => clock;
        public int LampCount => lamps.Count;
        public bool WaterAnimationEnabled => !reduced && reflections.Count > 0;
        public bool LampFlickerEnabled => !reduced && daylight < .99f;
        public float FirstLampIntensity { get; private set; }
        public float GetLampIntensity(int index) => index >= 0 && index < lamps.Count ? lamps[index].Intensity : 0;
        public Color GetLampColor(int index) => index >= 0 && index < lamps.Count ? lamps[index].Color : Color.clear;
        public Vector3 MoonPosition => moon?.Renderer != null ? moon.Renderer.transform.position : Vector3.zero;
        public float CloudDisplacement { get; private set; }
        public bool HasIndependentMoon => moon?.Renderer != null && moon.Renderer.sprite != null;
        public int IndependentRendererCount => GetComponentsInChildren<Renderer>(true).Length;
        public bool WolfWalking => walkingWolf?.Renderer != null && walkingWolf.Renderer.enabled;
        public int WolfFrameCount => wolfFrames?.Length ?? 0;
        public int WolfFrameIndex { get; private set; }
        public double PublicCallElapsedSeconds => PublicElapsed;
        public string ActivePublicVignette { get; private set; }
        public Vector3 WolfPosition => walkingWolf?.Renderer != null ? walkingWolf.Renderer.transform.position : Vector3.zero;
        public int WolfIndependentPartCount => restingWolf != null && restingWolf.IsReady ? 2 : 0;
        public float WolfHeadAngleDegrees => restingWolf != null ? restingWolf.HeadAngleDegrees : 0;
        private float WidthFactor => stageCamera != null ? stageCamera.orthographicSize * 2 * stageCamera.aspect / 4.3f : 1;
        private float HeightFactor => stageCamera != null ? stageCamera.orthographicSize / 4.66f : 1;

        public void Initialize(Camera camera, Material material)
        {
            if (initialized) return;
            initialized = true; stageCamera = camera; paperMaterial = material;
            if (stageCamera != null) { stageCamera.clearFlags = CameraClearFlags.SolidColor; stageCamera.backgroundColor = new Color(.025f, .055f, .10f); }
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            if (shader != null) geometryMaterial = new Material(shader) { name = "Independent village solid paper geometry", mainTexture = Texture2D.whiteTexture };
            BuildSky(); BuildRiver(); BuildTown(); BuildSkyObjects(); BuildReflections(); BuildPublicVignettes();
            walkingWolf = Add("Public walking wolf", "wolf-walk", new Vector2(-.6f, -2.3f), 1.1f, 45);
            // A separately illustrated two-piece wolf is available for the quiet village composition.
            var restNode = new GameObject("Independent resting wolf", typeof(SpriteRenderer));
            restNode.transform.SetParent(transform, false);
            var rest = restNode.GetComponent<SpriteRenderer>(); rest.sprite = UITheme.Art("wolf");
            rest.sharedMaterial = paperMaterial; rest.sortingOrder = 39;
            if (rest.sprite != null)
            {
                rest.transform.localPosition = new Vector3(-1.58f * WidthFactor, -1.55f * HeightFactor, 0);
                rest.transform.localScale = Vector3.one * (1.75f * HeightFactor / rest.sprite.bounds.size.y);
                restingWolf = restNode.AddComponent<IndependentWolfPuppet>();
                if (!restingWolf.Initialize(rest)) { rest.enabled = false; restingWolf = null; }
            }
            TryLoad(); Layout(); Render();
        }

        private void BuildTown()
        {
            // Heights refer to separate sprite boxes, never regions in a larger village image.
            Add("Far left pine", "pine", new Vector2(-1.7f, .5f), 2.6f, -18, true);
            Add("Far right pine", "pine", new Vector2(1.75f, .85f), 2.8f, -18);
            Add("Far pine two", "pine", new Vector2(-.95f, .85f), 2.25f, -17);
            Add("Far pine three", "pine", new Vector2(.95f, 1.15f), 2.6f, -17, true);
            Add("Village steps", "path", new Vector2(.05f,-1.15f), 2.6f, -13);
            Add("Church", "church", new Vector2(.5f, .3f), 2.7f, -12);
            var a = Add("Upper cottage", "cottage", new Vector2(-.9f, -.35f), 1.55f, -8);
            AddLamp(a, new Vector2(.49f, .48f), .15f); AddLamp(a, new Vector2(.67f, .48f), .10f);
            var b = Add("Village inn", "inn", new Vector2(1.17f, -.85f), 2.05f, -5);
            AddLamp(b, new Vector2(.32f, .49f), .15f); AddLamp(b, new Vector2(.65f, .49f), .15f);
            var c = Add("Lower cottage left", "cottage", new Vector2(-1.26f, -1.55f), 1.8f, 4, true);
            AddLamp(c, new Vector2(.48f, .49f), .17f);
            var d = Add("Lower cottage right", "cottage", new Vector2(1.57f, -2.2f), 1.6f, 7);
            AddLamp(d, new Vector2(.49f, .49f), .14f); AddLamp(d, new Vector2(.67f, .49f), .10f);
            var e = Add("Rear inn", "inn", new Vector2(-1.68f, -.12f), 1.25f, -10, true);
            AddLamp(e, new Vector2(.34f, .50f), .12f);
            var church = Find("Church"); AddLamp(church, new Vector2(.5f, .65f), .15f); AddLamp(church, new Vector2(.5f, .40f), .12f);
            Add("Left bank upper", "rock-bank", new Vector2(-1.2f, -2.3f), .65f, 12);
            Add("Right bank upper", "rock-bank", new Vector2(1.35f, -2.85f), .72f, 13, true);
            Add("Bridge", "bridge", new Vector2(-.1f, -2.05f), 1.25f, 24);
            Add("Left bank foreground", "rock-bank", new Vector2(-1.5f, -3.9f), .8f, 31);
            Add("Right bank foreground", "rock-bank", new Vector2(1.65f, -4.15f), .85f, 32, true);
            Add("Front pine left", "pine", new Vector2(-2.15f, -2.4f), 3.25f, 36);
            Add("Front pine right", "pine", new Vector2(2.17f, -3.25f), 2.6f, 37, true);
            // Openings measured on the independently generated sprites; UV origin is bottom-left.
            var locations = new[] { new Vector2(.474f,.547f), new Vector2(.376f,.248f), new Vector2(.311f,.491f), new Vector2(.734f,.253f),
                new Vector2(.474f,.547f), new Vector2(.474f,.547f), new Vector2(.376f,.248f), new Vector2(.303f,.686f), new Vector2(.75f,.535f), new Vector2(.375f,.37f) };
            for (var i = 0; i < locations.Length; i++)
            {
                lamps[i].Uv = locations[i]; lamps[i].Radius = i == 8 ? .09f : .07f;
                lamps[i].Opening = i == 8 ? new Vector2(.055f,.10f) : new Vector2(.075f,.085f);
            }
            var distantPositions = new[] { new Vector2(-1.46f,.36f), new Vector2(-.52f,.45f), new Vector2(.22f,.70f), new Vector2(1.42f,.38f) };
            for (var i = 0; i < distantPositions.Length; i++)
            {
                var house = Add("Distant cottage " + i, "cottage", distantPositions[i], .75f + i * .07f, -15 + i % 2, i % 2 == 0);
                AddLamp(house, new Vector2(.474f,.547f), .035f);
                lamps[lamps.Count-1].Opening = new Vector2(.075f,.085f);
            }
        }

        private Part Add(string id, string asset, Vector2 position, float height, int order, bool flip = false)
        {
            var renderer = SpriteObject(id, null, order);
            renderer.flipX = flip;
            var part = new Part { Id = id, Asset = asset, Position = position, Height = height, Renderer = renderer };
            parts.Add(part); return part;
        }
        private Part Find(string id) => parts.Find(p => p.Id == id);
        public SpriteRenderer GetPartRenderer(string id) => Find(id)?.Renderer;
        public bool SetPartLayout(string id, Vector2 worldPosition, float height, bool flipX = false)
        {
            var part = Find(id); if (part == null) return false;
            part.Position = worldPosition; part.Height = Mathf.Max(.01f, height); part.Renderer.flipX = flipX;
            LayoutPart(part); PositionLamps(); return true;
        }
        public bool SetWindowLayout(int index, Vector2 imageUv, float radius)
        {
            if (index < 0 || index >= lamps.Count) return false;
            lamps[index].Uv = new Vector2(Mathf.Clamp01(imageUv.x), Mathf.Clamp01(imageUv.y));
            lamps[index].Radius = Mathf.Max(.01f, radius); PositionLamps(); return true;
        }
        public bool SetWindowOpening(int index, Vector2 imageUv, Vector2 openingSizeUv)
        {
            if (!SetWindowLayout(index, imageUv, index >= 0 && index < lamps.Count ? lamps[index].Radius : .1f)) return false;
            lamps[index].Opening = new Vector2(Mathf.Clamp(openingSizeUv.x, .005f, 1), Mathf.Clamp(openingSizeUv.y, .005f, 1));
            PositionLamps(); return true;
        }
        private SpriteRenderer SpriteObject(string label, Sprite sprite, int order)
        {
            var node = new GameObject(label, typeof(SpriteRenderer)); node.transform.SetParent(transform, false);
            var renderer = node.GetComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sortingOrder = order; if (paperMaterial != null) renderer.sharedMaterial = paperMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; return renderer;
        }
        private Sprite Load(string asset)
        {
            if (art.TryGetValue(asset, out var existing) && existing != null) return existing;
            var path = asset.StartsWith("Actions/", StringComparison.Ordinal) ? "Werewolf/" + asset : "Werewolf/Layered/" + asset;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(path);
                if (texture == null) return null;
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
                ownedSprites.Add(sprite);
            }
            art[asset] = sprite; return sprite;
        }
        private void TryLoad()
        {
            if (hearthFrames == null)
            {
                var atlas = Load("hearth-flame");
                if (atlas != null)
                {
                    hearthFrames = new Sprite[4];
                    for (var i = 0; i < 4; i++)
                    {
                        var left = Mathf.FloorToInt(atlas.rect.width * i / 4f);
                        var right = Mathf.FloorToInt(atlas.rect.width * (i + 1) / 4f);
                        hearthFrames[i] = Sprite.Create(atlas.texture, new Rect(atlas.rect.x + left, atlas.rect.y, right-left, atlas.rect.height), Vector2.one*.5f, 100, 0, SpriteMeshType.FullRect);
                        hearthFrames[i].name = "Independent hearth flame pose " + i; ownedSprites.Add(hearthFrames[i]);
                    }
                }
            }
            foreach (var part in parts)
            {
                if (part.Asset == null || part.Renderer.sprite != null) continue;
                if (part == walkingWolf) { LoadWolfFrames(); continue; }
                var sprite = Load(part.Asset); if (sprite == null) continue;
                part.Renderer.sprite = sprite; LayoutPart(part);
            }
        }
        private void LoadWolfFrames()
        {
            if (wolfFrames != null) return;
            var atlas = Load("wolf-walk"); if (atlas == null) return;
            wolfFrames = new Sprite[8]; var cell = atlas.rect; var width = Mathf.Floor(cell.width / 4); var height = Mathf.Floor(cell.height / 2);
            for (var i = 0; i < 8; i++)
            {
                wolfFrames[i] = Sprite.Create(atlas.texture, new Rect(cell.x + i % 4 * width, cell.y + (1 - i / 4) * height, width, height),
                    Vector2.one * .5f, atlas.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                wolfFrames[i].name = "Wolf walk frame " + i; ownedSprites.Add(wolfFrames[i]);
            }
            walkingWolf.Renderer.sprite = wolfFrames[0]; LayoutPart(walkingWolf);
        }

        private void BuildSkyObjects()
        {
            moon = Add("Independent moon", null, new Vector2(1.25f, 3.0f), 1.05f, -45);
            moon.Renderer.sprite = UITheme.Art("moon");
            var strip = UITheme.Art("clouds");
            if (strip != null)
            {
                var regions = new[] { new Rect(16f / 2172, 233f / 724, 742f / 2172, 250f / 724), new Rect(772f / 2172, 217f / 724, 645f / 2172, 271f / 724), new Rect(1438f / 2172, 207f / 724, 722f / 2172, 331f / 724) };
                for (var i = 0; i < 3; i++)
                {
                    var r = regions[i]; var baseRect = strip.rect;
                    var slice = Sprite.Create(strip.texture, new Rect(baseRect.x + r.x * baseRect.width, baseRect.y + r.y * baseRect.height, r.width * baseRect.width, r.height * baseRect.height), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
                    ownedSprites.Add(slice);
                    var cloud = Add("Independent cloud " + i, null, new Vector2(-1.2f + i * 1.2f, 2.4f + i * .47f), .45f + i * .06f, -40 + i);
                    cloud.Renderer.sprite = slice; clouds.Add(cloud);
                }
            }
            for (var i = 0; i < 21; i++)
            {
                var star = SpriteObject("Independent star " + i, UITheme.Disc, -44);
                star.transform.localPosition = new Vector3((Mathf.Repeat(i * 1.618f, 1) - .5f) * 4.1f, .8f + Mathf.Repeat(i * .417f, 1) * 3.5f, 0);
                star.transform.localScale = Vector3.one * (.018f + i % 3 * .007f); stars.Add(star);
            }
        }
        private void AddLamp(Part building, Vector2 uv, float radius)
        {
            if (building == null) return;
            var order = building.Renderer.sortingOrder;
            if (!windowMaterials.ContainsKey(building))
            {
                var shader = Resources.Load<Shader>("Werewolf/Shader/HearthWindows");
                if (shader != null)
                {
                    var material = new Material(shader) { name = building.Id + " warm interior glass" };
                    windowMaterials[building] = material; building.Renderer.sharedMaterial = material;
                }
            }
            var maskNode = new GameObject(building.Id + " independent window opening " + lamps.Count, typeof(SpriteMask));
            maskNode.transform.SetParent(transform, false);
            var mask = maskNode.GetComponent<SpriteMask>(); mask.sprite = UITheme.Panel;
            mask.isCustomRangeActive = true; mask.frontSortingOrder = order + 2; mask.backSortingOrder = order + 1;
            var lamp = new Lamp { Building = building, Uv = uv, Radius = radius, Phase = lamps.Count * 1.731f, Mask = mask,
                Halo = SpriteObject(building.Id + " anchored window halo " + lamps.Count, UITheme.Mist, order + 1),
                Flame = SpriteObject(building.Id + " clipped hearth flame " + lamps.Count, UITheme.Disc, order + 2) };
            lamp.Flame.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            lamps.Add(lamp);
        }

        private void PositionLamps()
        {
            foreach (var lamp in lamps)
            {
                var renderer = lamp.Building.Renderer;
                if (renderer.sprite == null) { lamp.Halo.enabled = lamp.Flame.enabled = false; continue; }
                // The fire lives behind the illustrated glass. No flame sprite is pasted over the facade.
                lamp.Halo.enabled = lamp.Flame.enabled = false;
                var bounds = renderer.sprite.bounds;
                var x = bounds.min.x + bounds.size.x * lamp.Uv.x;
                if (renderer.flipX) x = -x;
                var point = renderer.transform.TransformPoint(new Vector3(x, bounds.min.y + bounds.size.y * lamp.Uv.y, 0));
                lamp.Halo.transform.position = lamp.Flame.transform.position = lamp.Mask.transform.position = point;
                var windowSize = Vector2.Scale(new Vector2(bounds.size.x, bounds.size.y), lamp.Opening) * renderer.transform.lossyScale.y;
                var maskBounds = lamp.Mask.sprite.bounds.size;
                lamp.Mask.transform.localScale = new Vector3(windowSize.x / maskBounds.x, windowSize.y / maskBounds.y, 1);
                var haloBounds = lamp.Halo.sprite.bounds.size;
                lamp.Halo.transform.localScale = new Vector3(lamp.Radius * 2 / haloBounds.x, lamp.Radius * 2.8f / haloBounds.y, 1) * HeightFactor;
                var flameBounds = lamp.Flame.sprite.bounds.size;
                lamp.Flame.transform.localScale = new Vector3(windowSize.x * .9f / flameBounds.x, windowSize.y * 1.2f / flameBounds.y, 1);
            }
        }

        private void BuildSky()
        {
            skyMesh = new Mesh { name = "Independent vertex-color sky gradient" }; ownedMeshes.Add(skyMesh);
            var node = new GameObject("Geometric sky gradient", typeof(MeshFilter), typeof(MeshRenderer)); node.transform.SetParent(transform, false);
            node.GetComponent<MeshFilter>().sharedMesh = skyMesh;
            var renderer = node.GetComponent<MeshRenderer>(); renderer.sharedMaterial = geometryMaterial; renderer.sortingOrder = -100;
            var vertices = new Vector3[10]; var colors = new Color[10]; var uv = new Vector2[10]; var triangles = new int[24];
            for (var row = 0; row < 5; row++)
            {
                var y = Mathf.Lerp(-4.8f, 4.8f, row / 4f); vertices[row * 2] = new Vector3(-8, y, 0); vertices[row * 2 + 1] = new Vector3(8, y, 0);
                if (row >= 4) continue;
                var t = row * 6; var v = row * 2; triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1; triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            skyMesh.vertices = vertices; skyMesh.uv = uv; skyMesh.colors = colors; skyMesh.triangles = triangles; skyMesh.RecalculateBounds();
            // Independent silhouettes add depth without reintroducing a complete landscape illustration.
            BuildPolygon("Far paper hillside", new[] { new Vector2(-4,-4.8f), new Vector2(4,-4.8f), new Vector2(4,.1f), new Vector2(2,1.15f), new Vector2(.8f,.4f), new Vector2(-.6f,1.05f), new Vector2(-2,1.55f), new Vector2(-4,.5f) }, -29, new Color(.007f,.014f,.026f));
        }
        private void BuildRiver()
        {
            riverMesh = new Mesh { name = "Independent river ribbon geometry" }; ownedMeshes.Add(riverMesh);
            var node = new GameObject("Independent dark paper river", typeof(MeshFilter), typeof(MeshRenderer)); node.transform.SetParent(transform, false);
            node.GetComponent<MeshFilter>().sharedMesh = riverMesh; riverRenderer = node.GetComponent<MeshRenderer>(); riverRenderer.sharedMaterial = geometryMaterial; riverRenderer.sortingOrder = -20;
            const int count = 18; var vertices = new Vector3[count * 2]; var colors = new Color[count * 2]; var uv = new Vector2[count * 2]; var triangles = new int[(count - 1) * 6];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)(count - 1); var y = Mathf.Lerp(-4.8f, .55f, t); var center = Mathf.Sin(t * 5) * .35f - .3f; var half = Mathf.Lerp(1.05f, .13f, t);
                vertices[i * 2] = new Vector3(center - half, y, 0); vertices[i * 2 + 1] = new Vector3(center + half, y, 0);
                if (i == count - 1) continue;
                var k = i * 6; var v = i * 2; triangles[k] = v; triangles[k + 1] = v + 2; triangles[k + 2] = v + 1; triangles[k + 3] = v + 1; triangles[k + 4] = v + 2; triangles[k + 5] = v + 3;
            }
            riverMesh.vertices = vertices; riverMesh.uv = uv; riverMesh.colors = colors; riverMesh.triangles = triangles; riverMesh.RecalculateBounds();
        }
        private void BuildPolygon(string label, Vector2[] points, int order, Color color)
        {
            var mesh = new Mesh { name = label }; ownedMeshes.Add(mesh); var vertices = new Vector3[points.Length]; var colors = new Color[points.Length]; var uv = new Vector2[points.Length]; var tris = new int[(points.Length - 2) * 3];
            for (var i = 0; i < points.Length; i++) { vertices[i] = points[i]; colors[i] = color; }
            for (var i = 0; i < points.Length - 2; i++) { tris[i * 3] = 0; tris[i * 3 + 1] = i + 1; tris[i * 3 + 2] = i + 2; }
            mesh.vertices = vertices; mesh.colors = colors; mesh.uv = uv; mesh.triangles = tris; mesh.RecalculateBounds();
            var node = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer)); node.transform.SetParent(transform, false); node.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.GetComponent<MeshRenderer>(); renderer.sharedMaterial = geometryMaterial; renderer.sortingOrder = order;
        }
        private void BuildReflections()
        {
            for (var i = 0; i < 34; i++)
            {
                var reflection = SpriteObject("Independent warm water stroke " + i, UITheme.Mist, -19);
                var t = i / 33f; var y = Mathf.Lerp(-4.6f, -.65f, t); var center = Mathf.Sin(((y + 4.8f) / 5.35f) * 5) * .35f - .3f;
                reflection.transform.localPosition = new Vector3(center + Mathf.Sin(i * 2.37f) * .23f * (1 - t), y, 0);
                var bounds = reflection.sprite.bounds.size;
                reflection.transform.localScale = new Vector3(Mathf.Lerp(.32f, .045f, t) * (.5f + Mathf.Repeat(i * .618f, .5f)) / bounds.x, (.009f + i % 3 * .003f) / bounds.y, 1);
                reflections.Add(reflection);
            }
        }

        /// <summary>Copies only public timing fields; never inspects actions, targets, players or private facts.</summary>
        public void BindPublic(RoomView view)
        {
            if (view == null) { publicPhase = publicStep = publicKey = null; lastPublicServerTime = 0; Render(); return; }
            var phase = view.phase; var step = view.nightStep; var match = view.matchId; var day = view.day; var started = view.phaseStartedUnixMs; var serverNow = view.serverTimeUnixMs;
            var key = match + ":" + day + ":" + phase + ":" + step + ":" + started;
            var elapsed = Math.Max(0, (serverNow - started) / 1000.0);
            // Repeated snapshots of one public call resync its clock without restarting the walk.
            var changedCall = publicKey != key;
            if (changedCall || serverNow > lastPublicServerTime && Math.Abs(PublicElapsed - elapsed) > 1.5)
            { publicElapsedAtBind = elapsed; localTimeAtBind = Time.unscaledTimeAsDouble; }
            publicKey = key; publicPhase = phase; publicStep = step; lastPublicServerTime = changedCall ? serverNow : Math.Max(lastPublicServerTime, serverNow); Render();
        }
        private double PublicElapsed => publicElapsedAtBind + (Time.unscaledTimeAsDouble - localTimeAtBind);
        public void SetDaylight(float amount) { daylight = Mathf.Clamp01(amount); Render(); }
        public void SetReducedMotion(bool enabled) { reduced = enabled; if (restingWolf != null) restingWolf.SetReducedMotion(enabled); Render(); }
        public void SetHomeComposition(bool enabled) { home = enabled; Render(); }
        private void LayoutPart(Part part)
        {
            var renderer = part.Renderer; renderer.transform.localPosition = new Vector3(part.Position.x * WidthFactor, part.Position.y * HeightFactor, 0);
            if (renderer.sprite != null) renderer.transform.localScale = Vector3.one * (part.Height * HeightFactor / Mathf.Max(.001f, renderer.sprite.bounds.size.y));
        }
        private void Layout()
        {
            foreach (var part in parts) LayoutPart(part);
            if (skyMesh != null) { var owner = transform.Find("Geometric sky gradient"); if (owner != null) owner.localScale = new Vector3(1, HeightFactor, 1); }
            var river = transform.Find("Independent dark paper river"); if (river != null) river.localScale = new Vector3(WidthFactor, HeightFactor, 1);
            PositionLamps(); lastAspect = stageCamera != null ? stageCamera.aspect : 1;
        }
        private void LateUpdate()
        {
            if (!initialized || stageCamera == null) return;
            if (!reduced) clock += Time.unscaledDeltaTime;
            if (Time.unscaledTime >= nextLoad) { nextLoad = Time.unscaledTime + 1; TryLoad(); }
            if (!Mathf.Approximately(lastAspect, stageCamera.aspect)) Layout();
            Render();
        }
        private void Render()
        {
            if (!initialized) return;
            var motion = reduced ? 0 : clock;
            if (stageCamera != null) stageCamera.backgroundColor = Color.Lerp(new Color(.025f,.055f,.10f), new Color(.68f,.77f,.82f), daylight);
            if (skyMesh != null && !Mathf.Approximately(lastGeometryDaylight, daylight))
            {
                var colors = skyColors;
                for (var i = 0; i < 5; i++)
                {
                    var night = Color.Lerp(new Color(.018f,.034f,.057f), new Color(.003f,.007f,.021f), i / 4f);
                    var day = Color.Lerp(new Color(.91f,.77f,.57f), new Color(.45f,.64f,.77f), i / 4f); colors[i * 2] = colors[i * 2 + 1] = Color.Lerp(night, day, daylight);
                }
                skyMesh.colors = colors;
            }
            if (riverMesh != null && !Mathf.Approximately(lastGeometryDaylight, daylight))
            {
                var colors = riverColors; var water = Color.Lerp(new Color(.004f,.013f,.027f), new Color(.24f,.43f,.48f), daylight);
                for (var i = 0; i < colors.Length; i++) colors[i] = water; riverMesh.colors = colors;
            }
            lastGeometryDaylight = daylight;
            foreach (var part in parts) if (part.Renderer != null) part.Renderer.color = Color.Lerp(part.Night, part.Day, daylight);
            if (moon?.Renderer != null)
            {
                moon.Renderer.color = new Color(1, 1, 1, 1 - daylight);
                moon.Renderer.transform.localPosition = new Vector3((moon.Position.x + Mathf.Sin(motion * .10f) * .06f) * WidthFactor, (moon.Position.y + Mathf.Sin(motion * .13f) * .04f) * HeightFactor, 0);
                moon.Renderer.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(motion * .08f) * 2);
            }
            CloudDisplacement = reduced ? 0 : Mathf.Sin(motion * .13f) * .20f;
            for (var i = 0; i < clouds.Count; i++)
            {
                var cloud = clouds[i]; cloud.Renderer.transform.localPosition = new Vector3((cloud.Position.x + CloudDisplacement * (i % 2 == 0 ? 1 : -.7f)) * WidthFactor, cloud.Position.y * HeightFactor, 0);
                cloud.Renderer.color = Color.Lerp(new Color(.7f,.78f,.85f,.85f), new Color(1,1,.95f,.8f), daylight);
            }
            PositionLamps();
            foreach (var pair in windowMaterials)
            {
                var slot = 0;
                foreach (var lamp in lamps)
                {
                    if (lamp.Building != pair.Key || slot >= 6) continue;
                    var half = lamp.Opening * .5f;
                    pair.Value.SetVector("_Window" + slot, new Vector4(lamp.Uv.x-half.x, lamp.Uv.y-half.y, lamp.Uv.x+half.x, lamp.Uv.y+half.y));
                    slot++;
                }
                pair.Value.SetFloat("_WindowCount", slot);
                pair.Value.SetFloat("_Night", 1-daylight);
                pair.Value.SetFloat("_Clock", motion);
                pair.Value.SetFloat("_Reduced", reduced ? 1 : 0);
                pair.Value.SetFloat("_Phase", parts.IndexOf(pair.Key) * 1.731f);
            }
            for (var i = 0; i < lamps.Count; i++)
            {
                var lamp = lamps[i];
                if (hearthFrames != null) lamp.Flame.sprite = hearthFrames[GetHearthFrameIndex(i)];
                var rate = 1 + i * .037f;
                var flicker = reduced ? .84f : .84f + Mathf.Sin(motion * 1.31f * rate + lamp.Phase) * .045f
                    + Mathf.Sin(motion * 4.73f * rate + lamp.Phase * 1.7f) * .025f
                    + Mathf.Sin(motion * 8.37f + lamp.Phase * .43f) * .012f;
                var strength = (1 - daylight) * Mathf.Clamp01(flicker);
                var warmth = reduced ? .45f : .5f + Mathf.Sin(motion * 1.87f * rate + lamp.Phase) * .35f;
                var color = Color.Lerp(new Color(1,.69f,.32f), new Color(1,.79f,.43f), warmth);
                lamp.Color = color; lamp.Intensity = strength;
                lamp.Halo.color = new Color(color.r,color.g,color.b,strength * .23f);
                lamp.Flame.color = new Color(color.r,color.g,color.b,strength * .88f);
                // The flame changes between generated poses. Neither facade nor flame texture is stretched.
                if (i == 0) FirstLampIntensity = strength;
            }

            for (var i = 0; i < reflections.Count; i++) reflections[i].color = Color.Lerp(new Color(.64f,.72f,.78f,(reduced ? .12f : .10f + Mathf.Sin(motion * 1.2f + i) * .035f)), new Color(.82f,.9f,.86f,.13f), daylight);
            for (var i = 0; i < stars.Count; i++) stars[i].color = new Color(1,.87f,.61f,(1 - daylight) * (reduced ? .5f : .42f + Mathf.Sin(motion * .7f + i) * .19f));
            RenderWolf(); RenderPublicVignettes();
        }
        private void BuildPublicVignettes()
        {
            // Public call emblems are not villagers and never point to a selected player's building.
            publicSymbols["guard"] = Add("Public guard gate emblem", "Actions/guard-shield", new Vector2(-.12f, -1.45f), .38f, 26);
            publicSymbols["seer"] = Add("Public seer constellation emblem", "Actions/seer-orb", new Vector2(.1f, 1.1f), .40f, -3);
            publicSymbols["witch"] = Add("Public witch potion emblem", "Actions/witch-bottle", new Vector2(.25f, -1.55f), .35f, 26);
            publicSymbols["cupid"] = Add("Public cupid constellation emblem", "Actions/cupid-bow", new Vector2(0, 1.05f), .42f, -3);
            publicSymbols["lovers"] = publicSymbols["cupid"];
            witchSecondBottle = Add("Public witch second potion emblem", "Actions/witch-bottle", new Vector2(.55f, -1.55f), .30f, 26);
            witchSecondBottle.Renderer.enabled = false;
            for (var i = 0; i < 7; i++)
            {
                var star = SpriteObject("Public call constellation point " + i, UITheme.Disc, -2);
                star.transform.localScale = Vector3.one * (.018f + i % 2 * .007f); publicConstellation.Add(star);
            }
            foreach (var symbol in publicSymbols.Values) symbol.Renderer.enabled = false;
        }
        private void RenderPublicVignettes()
        {
            ActivePublicVignette = !home && publicPhase == "night" ? publicStep : null;
            foreach (var symbol in publicSymbols.Values) symbol.Renderer.enabled = false;
            if (witchSecondBottle != null) witchSecondBottle.Renderer.enabled = false;
            var elapsed = reduced ? 0 : (float)PublicElapsed;
            var hasSymbol = ActivePublicVignette != null && publicSymbols.ContainsKey(ActivePublicVignette);
            if (hasSymbol)
            {
                var symbol = publicSymbols[ActivePublicVignette];
                symbol.Renderer.enabled = symbol.Renderer.sprite != null;
                symbol.Renderer.color = new Color(1, 1, 1, (1 - daylight) * .9f);
                // Shield and bottles stay resting at the gate. Only celestial call emblems rotate.
                var angle = ActivePublicVignette == "seer" ? Mathf.Sin(elapsed * .45f) * 7
                    : ActivePublicVignette == "cupid" || ActivePublicVignette == "lovers" ? Mathf.Sin(elapsed * .30f) * 3 : 0;
                symbol.Renderer.transform.localRotation = Quaternion.Euler(0, 0, angle);
                if (ActivePublicVignette == "witch" && witchSecondBottle != null)
                {
                    symbol.Renderer.color = new Color(.86f,.65f,1,(1 - daylight) * .9f);
                    witchSecondBottle.Renderer.enabled = witchSecondBottle.Renderer.sprite != null;
                    witchSecondBottle.Renderer.color = new Color(.63f,1,.72f,(1 - daylight) * .9f);
                }
            }
            for (var i = 0; i < publicConstellation.Count; i++)
            {
                var star = publicConstellation[i]; star.enabled = hasSymbol;
                if (!hasSymbol) continue;
                var isCupid = ActivePublicVignette == "cupid" || ActivePublicVignette == "lovers";
                var selectedPart = publicSymbols[ActivePublicVignette];
                var center = selectedPart.Renderer.transform.localPosition;
                star.sortingOrder = selectedPart.Renderer.sortingOrder + 1;
                var angle = i / (float)publicConstellation.Count * Mathf.PI * 2;
                var radius = ActivePublicVignette == "guard" || ActivePublicVignette == "witch" ? .23f : .31f;
                // A shared constellation on the sign itself, independent of actor and target identities.
                star.transform.localPosition = center + new Vector3(Mathf.Cos(angle) * radius * WidthFactor, Mathf.Sin(angle) * radius * HeightFactor, 0);
                var intensity = reduced ? .4f : .28f + .21f * (.5f + .5f * Mathf.Sin(elapsed * .8f - i * .7f));
                star.color = isCupid ? new Color(.94f,.45f,.4f,intensity * (1 - daylight)) : new Color(1,.73f,.32f,intensity * (1 - daylight));
            }
        }
        private void RenderWolf()
        {
            var walk = !home && publicPhase == "night" && publicStep == "werewolf" && wolfFrames != null;
            if (walkingWolf?.Renderer != null)
            {
                walkingWolf.Renderer.enabled = walk;
                if (walk)
                {
                    var elapsed = reduced ? 0 : PublicElapsed;
                    // Same street and timing for every viewer; it never approaches a selected victim's home.
                    var progress = Mathf.PingPong((float)elapsed / 7f, 1);
                    walkingWolf.Renderer.flipX = (int)(elapsed / 7) % 2 == 1;
                    var x = Mathf.Lerp(-1.12f, .82f, progress); var y = Mathf.Lerp(-1.75f, -1.35f, progress) + Mathf.Sin(progress * Mathf.PI) * .10f;
                    walkingWolf.Renderer.transform.localPosition = new Vector3(x * WidthFactor, y * HeightFactor, 0);
                    WolfFrameIndex = reduced ? 0 : (int)(elapsed * 8) % wolfFrames.Length;
                    walkingWolf.Renderer.sprite = wolfFrames[WolfFrameIndex];
                    walkingWolf.Renderer.color = new Color(1,1,1,1 - daylight);
                }
            }
            if (restingWolf != null)
            {
                restingWolf.gameObject.SetActive(!home && !walk);
                restingWolf.SetTint(new Color(1,1,1,1 - daylight));
            }
        }
        private void OnDestroy()
        {
            foreach (var sprite in ownedSprites) if (sprite != null) Destroy(sprite);
            foreach (var mesh in ownedMeshes) if (mesh != null) Destroy(mesh);
            foreach (var material in windowMaterials.Values) if (material != null) Destroy(material);
            if (geometryMaterial != null) Destroy(geometryMaterial);
        }
    }
}
