using System.Collections;
using System.Reflection;
using NGame.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace NGame.Presentation.Tests
{
    /// <summary>Tests the new live presentation, with physical hold time and lifecycle messages.</summary>
    public sealed class StorybookPrivacyTests
    {
        private GameObject owner;
        private StorybookApp app;
        private bool savedAudioExists;
        private bool savedMotionExists;
        private int savedAudio;
        private int savedMotion;
        private int savedFrameRate;
        private float savedTimeScale;
        private ScreenOrientation savedOrientation;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            savedAudioExists = PlayerPrefs.HasKey(AppPreferences.AmbientSoundKey);
            savedMotionExists = PlayerPrefs.HasKey(AppPreferences.ReducedMotionKey);
            savedAudio = PlayerPrefs.GetInt(AppPreferences.AmbientSoundKey);
            savedMotion = PlayerPrefs.GetInt(AppPreferences.ReducedMotionKey);
            savedFrameRate = Application.targetFrameRate;
            savedTimeScale = Time.timeScale;
            savedOrientation = Screen.orientation;
            foreach (var existing in Object.FindObjectsByType<StorybookApp>())
                Object.Destroy(existing.gameObject);
            yield return null;
            PlayerPrefs.SetInt(AppPreferences.AmbientSoundKey, 0);
            PlayerPrefs.SetInt(AppPreferences.ReducedMotionKey, 0);
            owner = new GameObject("NGame presentation test");
            app = owner.AddComponent<StorybookApp>();
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(owner);
            yield return null;
            RestorePreference(AppPreferences.AmbientSoundKey, savedAudioExists, savedAudio);
            RestorePreference(AppPreferences.ReducedMotionKey, savedMotionExists, savedMotion);
            PlayerPrefs.Save();
            Application.targetFrameRate = savedFrameRate;
            Time.timeScale = savedTimeScale;
            Screen.orientation = savedOrientation;
        }

        private static void RestorePreference(string key, bool existed, int value)
        {
            if (existed) PlayerPrefs.SetInt(key, value);
            else PlayerPrefs.DeleteKey(key);
        }

        private GameObject SecretFace()
        {
            foreach (var child in owner.GetComponentsInChildren<Transform>(true))
                if (child.name == "Role revealed") return child.gameObject;
            Assert.Fail("The actual private card front is required for a useful privacy check.");
            return null;
        }

        [Test]
        public void HomeAndPreparationKeepTheCardHiddenAndBoundThePartySize()
        {
            Assert.That(app.Route, Is.EqualTo(AppRoute.Home));
            Assert.That(app.Session.RoleVisible, Is.False);
            app.SecretCard.BeginHold();
            Assert.That(app.SecretCard.IsHolding, Is.False);
            app.OpenPreparation();
            Assert.That(app.Route, Is.EqualTo(AppRoute.Preparation));
            app.SetPreparationCount(5);
            Assert.That(app.PreparationPlayerCount, Is.EqualTo(6));
            app.SetPreparationCount(13);
            Assert.That(app.PreparationPlayerCount, Is.EqualTo(12));
            app.SetPreparationCount(9);
            Assert.That(app.PreparationPlayerCount, Is.EqualTo(9));
            Assert.That(SecretFace().activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator ARealUnscaledHoldRevealsAndMaskingHidesTheActualGraphicsImmediately()
        {
            app.EnterVillage();
            Time.timeScale = 0;
            app.SecretCard.BeginHold();
            Assert.That(app.Session.RoleVisible, Is.False, "A press must not reveal immediately.");
            yield return new WaitForSecondsRealtime(SecretCardView.HoldDuration + .15f);
            Assert.That(app.Session.RoleVisible, Is.True);
            Assert.That(SecretFace().activeInHierarchy, Is.True);
            Assert.That(app.SecretCard.HoldFraction, Is.EqualTo(1));
            app.SecretCard.MaskImmediately();
            Assert.That(app.Session.RoleVisible, Is.False);
            Assert.That(SecretFace().activeInHierarchy, Is.False);
            Assert.That(app.SecretCard.HoldFraction, Is.Zero);
        }

        [UnityTest]
        public IEnumerator BackgroundingAnUnfinishedHoldCannotRevealAfterResuming()
        {
            app.EnterVillage();
            app.SecretCard.BeginHold();
            yield return null;
            app.SecretCard.gameObject.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);
            app.SecretCard.gameObject.SendMessage("OnApplicationPause", false, SendMessageOptions.RequireReceiver);
            yield return new WaitForSecondsRealtime(SecretCardView.HoldDuration + .15f);
            Assert.That(app.Session.RoleVisible, Is.False);
            Assert.That(SecretFace().activeInHierarchy, Is.False);
            Assert.That(app.SecretCard.IsHolding, Is.False);
            Assert.That(app.SecretCard.HoldFraction, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FocusLossMasksAVisibleRoleWithoutResumingTheOldHold()
        {
            app.EnterVillage();
            app.SecretCard.BeginHold();
            yield return new WaitForSecondsRealtime(SecretCardView.HoldDuration + .15f);
            Assert.That(app.Session.RoleVisible, Is.True);
            owner.SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);
            Assert.That(app.Session.RoleVisible, Is.False);
            Assert.That(SecretFace().activeInHierarchy, Is.False);
            owner.SendMessage("OnApplicationFocus", true, SendMessageOptions.RequireReceiver);
            yield return new WaitForSecondsRealtime(SecretCardView.HoldDuration + .15f);
            Assert.That(app.Session.RoleVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator SettingsCancelTheHoldAndClosingDoesNotRestartIt()
        {
            app.EnterVillage();
            app.SecretCard.BeginHold();
            yield return null;
            app.ShowSettings();
            Assert.That(app.IsModalOpen, Is.True);
            app.SecretCard.BeginHold();
            Assert.That(app.SecretCard.IsHolding, Is.False);
            app.CloseDialog();
            yield return new WaitForSecondsRealtime(SecretCardView.HoldDuration + .15f);
            Assert.That(app.Session.RoleVisible, Is.False);
            Assert.That(app.IsModalOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator PhaseAndRouteChangesCancelPendingRevealAndVillageEntryResetsOnlyTheRound()
        {
            app.EnterVillage();
            app.Preferences.SetReducedMotion(true);
            app.SecretCard.BeginHold();
            yield return null;
            app.AdvanceToDawn();
            Assert.That(app.Session.RoleVisible, Is.False);
            Assert.That(app.SecretCard.IsHolding, Is.False);
            yield return new WaitForSecondsRealtime(SecretCardView.HoldDuration + .15f);
            Assert.That(app.Session.Phase, Is.EqualTo(VillagePhase.Vote));
            app.Session.SelectPlayer("chloe");
            app.Session.ConfirmSelection();
            app.OpenHome();
            app.EnterVillage();
            Assert.That(app.Session.Phase, Is.EqualTo(VillagePhase.Night));
            Assert.That(app.Session.ConfirmedPlayerId, Is.Null);
            Assert.That(app.Session.RoleVisible, Is.False);
            Assert.That(app.Preferences.ReducedMotion, Is.True);
        }

        [UnityTest]
        public IEnumerator ASecondPointerCannotTakeOverThePrimaryHold()
        {
            app.EnterVillage();
            var first = new PointerEventData(null) { pointerId = 5, button = PointerEventData.InputButton.Left };
            var second = new PointerEventData(null) { pointerId = 9, button = PointerEventData.InputButton.Left };
            app.SecretCard.OnPointerDown(first);
            app.SecretCard.OnPointerDown(second);
            yield return new WaitForSecondsRealtime(SecretCardView.HoldDuration + .15f);
            Assert.That(app.Session.RoleVisible, Is.True);
            app.SecretCard.OnPointerUp(second);
            Assert.That(app.Session.RoleVisible, Is.True);
            app.SecretCard.OnPointerExit(first);
            Assert.That(app.Session.RoleVisible, Is.False);
            Assert.That(SecretFace().activeInHierarchy, Is.False);
        }

        [TestCase("Panel")]
        [TestCase("Disc")]
        public void ActualProceduralUiSpritesHaveAnOpaqueCenterAndTransparentCorners(string propertyName)
        {
            var theme = typeof(StorybookApp).Assembly.GetType("NGame.Presentation.UITheme", true);
            var property = theme.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);
            Assert.That(property, Is.Not.Null);
            var sprite = (Sprite)property.GetValue(null);
            Assert.That(sprite, Is.Not.Null);

            // The production textures are intentionally non-readable: inspect their real GPU pixels.
            var source = sprite.texture;
            var target = RenderTexture.GetTemporary(source.width, source.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var previousTarget = RenderTexture.active;
            Texture2D readable = null;
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                readable.Apply(false, false);
                var rect = sprite.rect;
                var center = readable.GetPixel(Mathf.FloorToInt(rect.x + rect.width * .5f),
                    Mathf.FloorToInt(rect.y + rect.height * .5f));
                var corner = readable.GetPixel(Mathf.FloorToInt(rect.x), Mathf.FloorToInt(rect.y));
                Assert.That(center.a, Is.GreaterThan(.9f), propertyName + " must render buttons and masks visibly.");
                Assert.That(corner.a, Is.LessThan(.1f), propertyName + " must retain its cut-out shape.");
            }
            finally
            {
                RenderTexture.active = previousTarget;
                RenderTexture.ReleaseTemporary(target);
                if (readable != null) Object.Destroy(readable);
            }
        }

        [UnityTest]
        public IEnumerator ReducedMotionFreezesAmbientClockWaterAndWindowFlickerThenCanResume()
        {
            app.EnterVillage();
            var stage = owner.GetComponentInChildren<VillageStage>();
            Assert.That(stage, Is.Not.Null);
            app.Preferences.SetReducedMotion(false);
            stage.SetDaylight(0);
            yield return null;
            var activeClock = stage.AmbientClock;
            var activeIntensity = stage.FirstLampIntensity;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(stage.AmbientClock, Is.GreaterThan(activeClock));
            Assert.That(stage.WaterAnimationEnabled, Is.True, "The actual river shader must be available and animated.");
            Assert.That(stage.LampFlickerEnabled, Is.True);
            Assert.That(Mathf.Abs(stage.FirstLampIntensity - activeIntensity), Is.GreaterThan(.0001f));
            Assert.That(stage.ActiveLampCount, Is.EqualTo(10), "Only lights anchored to the village remain.");
            Assert.That(GameObject.Find("Independent paper lantern"), Is.Null, "The unsupported floating lantern was removed.");

            app.Preferences.SetReducedMotion(true);
            var quietClock = stage.AmbientClock;
            var quietIntensity = stage.FirstLampIntensity;
            Assert.That(stage.WaterAnimationEnabled, Is.False);
            Assert.That(stage.LampFlickerEnabled, Is.False);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(stage.AmbientClock, Is.EqualTo(quietClock));
            Assert.That(stage.FirstLampIntensity, Is.EqualTo(quietIntensity));

            app.Preferences.SetReducedMotion(false);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(stage.AmbientClock, Is.GreaterThan(quietClock));
            Assert.That(stage.WaterAnimationEnabled, Is.True);
            Assert.That(stage.LampFlickerEnabled, Is.True);
        }

        [Test]
        public void FullDaylightExtinguishesTheVillageLamps()
        {
            app.EnterVillage();
            var stage = owner.GetComponentInChildren<VillageStage>();
            Assert.That(stage, Is.Not.Null);
            stage.SetDaylight(0);
            Assert.That(stage.ActiveLampCount, Is.GreaterThan(0));
            Assert.That(stage.FirstLampIntensity, Is.GreaterThan(0));
            stage.SetDaylight(1);
            Assert.That(stage.ActiveLampCount, Is.Zero);
            Assert.That(stage.FirstLampIntensity, Is.Zero);
            Assert.That(stage.LampFlickerEnabled, Is.False);
        }

        [UnityTest]
        public IEnumerator LivePhaseLightTransitionFinishesDespiteRepeatedRoomSnapshots()
        {
            var stage = owner.GetComponentInChildren<VillageStage>();
            stage.SetDaylight(0);
            stage.TransitionToDaylight(1);
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(stage.DaylightAmount, Is.GreaterThan(.02f).And.LessThan(.9f));
            stage.TransitionToDaylight(1); // Polling the same phase must not restart its animation.
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.That(stage.DaylightAmount, Is.EqualTo(1).Within(.001f));
            Assert.That(stage.ActiveLampCount, Is.Zero);
            app.Preferences.SetReducedMotion(true);
            stage.TransitionToDaylight(0);
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(stage.DaylightAmount, Is.Zero.Within(.001f));
            Assert.That(stage.ActiveLampCount, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator ArticulatedWolfAndIndependentSkyMoveThenRestWithReducedMotion()
        {
            app.EnterVillage();
            var stage = owner.GetComponentInChildren<VillageStage>();
            Assert.That(stage.WolfArticulated, Is.True, "The wolf must use its real deformation mesh.");
            Assert.That(stage.WolfMeshVertexCount, Is.GreaterThan(1000));
            Assert.That(stage.MoonIndependent, Is.True, "The moving moon requires the separate sky assets.");
            Assert.That(stage.Details, Is.Not.Null);
            Assert.That(stage.Details.ChimneyPuffCount, Is.EqualTo(7));
            Assert.That(stage.Details.TwinklingStarCount, Is.EqualTo(5));
            var head = stage.WolfHeadAngleDegrees;
            var moon = stage.MoonPosition;
            var clouds = stage.CloudDisplacement;
            var smoke = stage.Details.FirstSmokePosition;
            var stars = stage.Details.FirstStarIntensity;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(Mathf.Abs(stage.WolfHeadAngleDegrees - head), Is.GreaterThan(.001f));
            Assert.That(Vector3.Distance(stage.MoonPosition, moon), Is.GreaterThan(.0001f));
            Assert.That(Mathf.Abs(stage.CloudDisplacement - clouds), Is.GreaterThan(.0001f),
                "The separate paper clouds must travel across the sky.");
            Assert.That(Vector3.Distance(stage.Details.FirstSmokePosition, smoke), Is.GreaterThan(.0001f));
            Assert.That(Mathf.Abs(stage.Details.FirstStarIntensity - stars), Is.GreaterThan(.00001f));
            Assert.That(stage.WolfLeftEarAngleDegrees, Is.Not.EqualTo(stage.WolfRightEarAngleDegrees));
            app.Preferences.SetReducedMotion(true);
            var restingMoon = stage.MoonPosition;
            var quietStars = stage.Details.FirstStarIntensity;
            Assert.That(stage.WolfHeadAngleDegrees, Is.Zero);
            Assert.That(stage.WolfLeftEarAngleDegrees, Is.Zero);
            Assert.That(stage.WolfRightEarAngleDegrees, Is.Zero);
            Assert.That(stage.CloudDisplacement, Is.Zero);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(stage.MoonPosition, Is.EqualTo(restingMoon));
            Assert.That(stage.Details.FirstStarIntensity, Is.EqualTo(quietStars));
        }

        [UnityTest]
        public IEnumerator OpeningThePublicRoleLibraryCancelsThePrivateHoldAndSupportsAllFourPages()
        {
            app.EnterVillage();
            app.SecretCard.BeginHold();
            yield return null;
            app.OpenRoleLibrary();
            Assert.That(app.Route, Is.EqualTo(AppRoute.RoleLibrary));
            Assert.That(app.SecretCard.IsHolding, Is.False);
            Assert.That(SecretFace().activeInHierarchy, Is.False);
            Assert.That(app.RoleLibrary.PageCount, Is.EqualTo(4));
            var theme = typeof(StorybookApp).Assembly.GetType("NGame.Presentation.UITheme", true);
            var illustration = theme.GetMethod("RoleCard", BindingFlags.Static | BindingFlags.Public);
            foreach (var role in RoleCatalog.All)
            {
                Assert.That(illustration.Invoke(null, new object[] { role.RoleArtIndex }), Is.Not.Null, role.Id);
                Assert.That(app.RoleLibrary.OpenPage(role.RoleArtIndex / 4), Is.True);
                Assert.That(app.RoleLibrary.ShowRole(role.Id), Is.True);
                Assert.That(app.RoleLibrary.SelectedRoleId, Is.EqualTo(role.Id));
                Assert.That(app.Session.RoleVisible, Is.False);
            }
            Assert.That(app.RoleLibrary.OpenPage(-1), Is.False);
            Assert.That(app.RoleLibrary.OpenPage(4), Is.False);
            Assert.That(app.RoleLibrary.ShowRole("unknown"), Is.False);
            yield return new WaitForSecondsRealtime(SecretCardView.HoldDuration + .15f);
            Assert.That(app.Session.RoleVisible, Is.False, "No old private hold may finish inside the public library.");
        }

        [UnityTest]
        public IEnumerator CancellingDuringTheCardFlipHidesThePrivateFaceInTheSameFrame()
        {
            app.EnterVillage();
            app.SecretCard.BeginHold();
            yield return new WaitForSecondsRealtime(SecretCardView.HoldDuration + .025f);
            Assert.That(app.Session.RoleVisible, Is.True);
            Assert.That(app.SecretCard.IsFlipping, Is.True);
            app.SecretCard.MaskImmediately();
            Assert.That(app.Session.RoleVisible, Is.False);
            Assert.That(SecretFace().activeInHierarchy, Is.False);
            Assert.That(app.SecretCard.IsFaceDisplayed, Is.False);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(SecretFace().activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator LeavingReducedMotionDoesNotRestartTheEntranceOfAlreadyVisibleCards()
        {
            app.OpenRoleLibrary();
            app.Preferences.SetReducedMotion(true);
            app.RoleLibrary.OpenPage(1);
            yield return null;
            foreach (var card in app.RoleLibrary.GetComponentsInChildren<CanvasGroup>())
                Assert.That(card.alpha, Is.EqualTo(1));
            app.Preferences.SetReducedMotion(false);
            yield return null;
            foreach (var card in app.RoleLibrary.GetComponentsInChildren<CanvasGroup>())
                Assert.That(card.alpha, Is.EqualTo(1), "A visible card must stay visible when motion resumes.");
        }

#if UNITY_EDITOR
        [Test]
        public void VillageShadersDoNotReportCompilationErrors()
        {
            foreach (var resource in new[] { "PaperRiver", "AmberGlow", "WolfPaper", "UICard" })
            {
                var shader = Resources.Load<Shader>("Werewolf/Shader/" + resource);
                Assert.That(shader, Is.Not.Null, resource);
                foreach (var message in UnityEditor.ShaderUtil.GetShaderMessages(shader))
                    Assert.That(message.severity.ToString(), Is.Not.EqualTo("Error"), resource + ": " + message.message);
            }
        }
#endif

        [UnityTest]
        public IEnumerator PublicPortraitCacheRecoversAfterUnityDestroysRuntimeSprites()
        {
            var theme = typeof(StorybookApp).Assembly.GetType("NGame.Presentation.UITheme", true);
            var portrait = theme.GetMethod("Portrait", BindingFlags.Static | BindingFlags.Public);
            var first = (Sprite)portrait.Invoke(null, new object[] { 0 });
            Assert.That(first != null, Is.True);
            UnityEngine.Object.Destroy(first);
            yield return null;
            Assert.That(first == null, Is.True);
            var restored = (Sprite)portrait.Invoke(null, new object[] { 0 });
            Assert.That(restored != null, Is.True, "Re-entering Play must recreate an unloaded public portrait.");
            Assert.That(restored.texture != null, Is.True);
            for (var i = 0; i < 6; i++) Assert.That((Sprite)portrait.Invoke(null, new object[] { i }) != null, Is.True);
        }
    }
}
