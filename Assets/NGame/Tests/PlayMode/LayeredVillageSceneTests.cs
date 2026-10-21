using System.Collections;
using System.IO;
using NGame.Core.Multiplayer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NGame.Presentation.Tests
{
    public sealed class LayeredVillageSceneTests
    {
        private GameObject owner;
        private Material material;
        private LayeredVillageScene scene;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            owner = new GameObject("Separate village fixture");
            var cameraNode = new GameObject("Independent fixture camera", typeof(Camera)); cameraNode.transform.SetParent(owner.transform);
            var camera = cameraNode.GetComponent<Camera>(); camera.enabled = false; camera.orthographic = true; camera.orthographicSize = 4.66f; camera.aspect = 430f / 932;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null); material = new Material(shader);
            scene = owner.AddComponent<LayeredVillageScene>(); scene.Initialize(camera, material); scene.SetDaylight(0); scene.SetHomeComposition(false);
            yield return null; yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        { Object.Destroy(owner); if (material != null) Object.Destroy(material); yield return null; }

        [Test]
        public void VillageUsesIndividualLoadedObjectsAndNoWholePictureMapperOrWarpMaterial()
        {
            Assert.That(scene.IndependentRendererCount, Is.GreaterThan(50));
            foreach (var id in new[] { "Church", "Village inn", "Upper cottage", "Bridge", "Far left pine", "Left bank foreground" })
            { var renderer = scene.GetPartRenderer(id); Assert.That(renderer, Is.Not.Null, id); Assert.That(renderer.sprite, Is.Not.Null, id); }
            Assert.That(scene.HasIndependentMoon, Is.True);
            Assert.That(owner.GetComponentInChildren<VillageDetails>(), Is.Null);
            Assert.That(owner.GetComponentInChildren<LivingSky>(), Is.Null, "The sky must not be anchored to a full village image.");
            foreach (var renderer in scene.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite != null)
                    Assert.That(renderer.sprite.texture.name, Does.Not.Contain("village-night").And.Not.Contain("village-dawn"), renderer.name);
                if (renderer.sharedMaterial != null)
                {
                    Assert.That(renderer.sharedMaterial.shader, Is.Not.EqualTo(Resources.Load<Shader>("Werewolf/Shader/PaperRiver")), renderer.name);
                    Assert.That(renderer.sharedMaterial.shader, Is.Not.EqualTo(Resources.Load<Shader>("Werewolf/Shader/WolfPaper")), renderer.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator WindowsHaveSeparateClippedFlamesWithDifferentFlickerWhileHousesRemainFixed()
        {
            Assert.That(scene.LampCount, Is.GreaterThanOrEqualTo(10));
            var masks = scene.GetComponentsInChildren<SpriteMask>(); Assert.That(masks.Length, Is.EqualTo(scene.LampCount));
            foreach (var mask in masks) { Assert.That(mask.sprite, Is.Not.Null); Assert.That(mask.isCustomRangeActive, Is.True); }
            var clipped = 0;
            foreach (var renderer in scene.GetComponentsInChildren<SpriteRenderer>())
                if (renderer.name.Contains("clipped hearth flame")) { Assert.That(renderer.maskInteraction, Is.EqualTo(SpriteMaskInteraction.VisibleInsideMask)); clipped++; }
            Assert.That(clipped, Is.EqualTo(scene.LampCount));
            var house = scene.GetPartRenderer("Village inn"); var position = house.transform.position; var scale = house.transform.localScale; var tint = house.color;
            var first = scene.GetLampIntensity(0); var second = scene.GetLampIntensity(1); var color = scene.GetLampColor(1);
            Assert.That(Mathf.Abs(first - second), Is.GreaterThan(.001f));
            yield return new WaitForSecondsRealtime(.31f);
            Assert.That(Mathf.Abs(scene.GetLampIntensity(0) - first), Is.GreaterThan(.0001f));
            Assert.That(scene.GetLampColor(1), Is.Not.EqualTo(color));
            Assert.That(house.transform.position, Is.EqualTo(position)); Assert.That(house.transform.localScale, Is.EqualTo(scale)); Assert.That(house.color, Is.EqualTo(tint));
            scene.SetReducedMotion(true); var fixedLight = scene.GetLampIntensity(0); var fixedColor = scene.GetLampColor(1);
            yield return new WaitForSecondsRealtime(.18f);
            Assert.That(scene.GetLampIntensity(0), Is.EqualTo(fixedLight)); Assert.That(scene.GetLampColor(1), Is.EqualTo(fixedColor));
            scene.SetDaylight(1); Assert.That(scene.FirstLampIntensity, Is.Zero);
        }

        private static RoomView Night(string step, long serverTime = 104000) => new RoomView
        {
            matchId = "public-scene-fixture", phase = "night", nightStep = step, day = 1,
            phaseStartedUnixMs = 100000, serverTimeUnixMs = serverTime
        };

        [UnityTest]
        public IEnumerator WolfPatrolDependsOnPublicCallNotPrivateActorTargetsOrFactsAndDoesNotRestartOnPoll()
        {
            Assert.That(scene.WolfFrameCount, Is.EqualTo(8), "The separately generated walking atlas must contain eight cells.");
            var view = Night("werewolf"); scene.BindPublic(view);
            Assert.That(scene.WolfWalking, Is.True);
            var position = scene.WolfPosition; var frame = scene.WolfFrameIndex;
            view.selfRoleId = "seer"; view.action = new ActionView { kind = "inspect", allowedTargetIds = new[] { "secret-a" }, submittedTargetIds = new[] { "secret-b" } };
            view.privateFacts = new[] { new PrivateFact { description = "must not influence public scenery", roleId = "werewolf" } };
            view.players = new[] { new PlayerView { id = "secret-a", displayName = "Changed identity" } };
            scene.BindPublic(view);
            Assert.That(scene.WolfPosition, Is.EqualTo(position)); Assert.That(scene.WolfFrameIndex, Is.EqualTo(frame));
            yield return new WaitForSecondsRealtime(.28f);
            var advancedPosition = scene.WolfPosition;
            Assert.That(Vector3.Distance(advancedPosition, position), Is.GreaterThan(.001f));
            scene.BindPublic(Night("werewolf", 104280));
            Assert.That(Vector3.Distance(scene.WolfPosition, advancedPosition), Is.LessThan(.03f), "Same call refresh must not restart the patrol at the beginning.");
            scene.BindPublic(Night("seer")); Assert.That(scene.WolfWalking, Is.False);
            scene.BindPublic(Night("werewolf")); scene.SetReducedMotion(true);
            var resting = scene.WolfPosition; var restingFrame = scene.WolfFrameIndex;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(scene.WolfPosition, Is.EqualTo(resting)); Assert.That(scene.WolfFrameIndex, Is.EqualTo(restingFrame));
            scene.BindPublic(null); Assert.That(scene.WolfWalking, Is.False);
        }

        [UnityTest]
        public IEnumerator RebindingOneCachedPublicSnapshotForTwoSecondsNeverRewindsTheCallClock()
        {
            var cached = Night("werewolf"); scene.BindPublic(cached); var beginning = scene.PublicCallElapsedSeconds;
            for (var i = 0; i < 6; i++)
            {
                yield return new WaitForSecondsRealtime(.31f);
                scene.BindPublic(cached);
            }
            Assert.That(scene.PublicCallElapsedSeconds, Is.GreaterThan(beginning + 1.6),
                "A cached view has an unchanged server timestamp and must not rewind a long-running public animation.");
            foreach (var step in new[] { "guard", "seer", "witch", "cupid", "lovers" })
            {
                scene.BindPublic(Night(step));
                Assert.That(scene.ActivePublicVignette, Is.EqualTo(step));
                Assert.That(scene.WolfWalking, Is.False);
            }
            scene.BindPublic(new RoomView { matchId = "public-scene-fixture", phase = "vote", day = 1, serverTimeUnixMs = 120000 });
            Assert.That(scene.ActivePublicVignette, Is.Null);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator GeneratedVillagePartsContainRealTransparentAndOpaquePixels()
        {
            foreach (var name in new[] { "cottage", "inn", "church", "bridge", "rock-bank", "pine", "wolf-walk" })
            {
                var path = Path.Combine(Application.dataPath, "Resources/Werewolf/Layered/" + name + ".png");
                Assert.That(File.Exists(path), Is.True, name + " generated source PNG");
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.That(texture.LoadImage(File.ReadAllBytes(path), false), Is.True, name);
                var pixels = texture.GetPixels32(); var transparent = false; var opaque = false;
                foreach (var pixel in pixels) { if (pixel.a == 0) transparent = true; if (pixel.a >= 250) opaque = true; if (transparent && opaque) break; }
                Object.Destroy(texture);
                Assert.That(transparent, Is.True, name + " must not have a flattened background"); Assert.That(opaque, Is.True, name + " visible artwork");
                yield return null;
            }
        }
#endif
    }
}
