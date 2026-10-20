using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NGame.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NGame.Presentation.Tests
{
    /// <summary>Real raycast controls and route contracts; does not establish device or novice UX.</summary>
    public sealed class GameHubTests
    {
        private sealed class MemoryStore : IPreferenceStore
        {
            public bool ReadBool(string key, bool fallback) => key == AppPreferences.ReducedMotionKey;
            public void WriteBool(string key, bool value) { }
            public void Save() { }
        }

        private GameObject owner, ownedEvents;
        private GameHubView hub;
        private readonly List<string> opened = new List<string>();
        private readonly List<bool> intentions = new List<bool>();
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private int joined, settings, resumed;
        private bool integrationPreferencesChanged, hadServer, hadAudio, hadMotion;
        private string originalServer;
        private int originalAudio, originalMotion, originalFrameRate;
        private ScreenOrientation originalOrientation;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            opened.Clear(); intentions.Clear(); joined = settings = resumed = 0;
            owner = new GameObject("Hub controls fixture", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            owner.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            owner.GetComponent<Canvas>().sortingOrder = 10000;
            var scaler = owner.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(430, 932);
            scaler.matchWidthOrHeight = 1;
            if (EventSystem.current == null) ownedEvents = new GameObject("Hub fixture events", typeof(EventSystem));
            hub = owner.AddComponent<GameHubView>();
            hub.Initialize(new AppPreferences(new MemoryStore()), id =>
            { opened.Add(id); intentions.Add(hub.WantsToCreate); }, () => joined++, () => settings++, () => resumed++);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (owner != null) Object.Destroy(owner);
            if (ownedEvents != null) Object.Destroy(ownedEvents);
            ownedEvents = null;
            yield return null;
            if (integrationPreferencesChanged)
            {
                if (hadServer) PlayerPrefs.SetString("ngame.network.server", originalServer);
                else PlayerPrefs.DeleteKey("ngame.network.server");
                RestoreBool(AppPreferences.AmbientSoundKey, hadAudio, originalAudio);
                RestoreBool(AppPreferences.ReducedMotionKey, hadMotion, originalMotion);
                PlayerPrefs.Save();
                Application.targetFrameRate = originalFrameRate;
                Screen.orientation = originalOrientation;
                integrationPreferencesChanged = false;
            }
        }

        [UnityTest]
        public IEnumerator CatalogueOpensOnlyWerewolfWithoutCreationIntent()
        {
            yield return Tap(Find("Hub game werewolf"));
            CollectionAssert.AreEqual(new[] { GameHubView.WerewolfGameId }, opened);
            CollectionAssert.AreEqual(new[] { false }, intentions);
            Assert.AreEqual(0, joined + settings + resumed);
        }

        [UnityTest]
        public IEnumerator CreationSelectionCanCancelAndContinueWithoutPrematureCallback()
        {
            yield return Tap(hub.PrimaryAction);
            Assert.IsTrue(hub.WantsToCreate);
            Assert.IsEmpty(opened);
            yield return Tap(hub.SecondaryAction);
            Assert.IsFalse(hub.WantsToCreate);
            Assert.IsEmpty(opened);
            yield return Tap(hub.PrimaryAction);
            yield return Tap(Find("Hub selection back"));
            Assert.IsFalse(hub.WantsToCreate);
            Assert.IsEmpty(opened);
            yield return Tap(hub.PrimaryAction);
            yield return Tap(hub.PrimaryAction);
            CollectionAssert.AreEqual(new[] { "werewolf" }, opened);
            CollectionAssert.AreEqual(new[] { true }, intentions);
            hub.ShowCatalogue();
            yield return Tap(hub.PrimaryAction);
            yield return Tap(Find("Hub game werewolf"));
            CollectionAssert.AreEqual(new[] { true, true }, intentions);
        }

        [UnityTest]
        public IEnumerator FutureGamesAreClearlyUnavailableAndIgnorePointerClicks()
        {
            foreach (var id in new[] { "president", "mimes" })
            {
                var card = Find("Hub game " + id + " unavailable");
                Assert.IsFalse(card.interactable);
                Assert.IsTrue(card.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text == "En préparation"));
                yield return Tap(card);
            }
            Assert.IsEmpty(opened);
            Assert.AreEqual(0, joined + settings + resumed);
        }

        [UnityTest]
        public IEnumerator SavedAccessShowsResumeOnlyInCatalogue()
        {
            Assert.IsFalse(hub.ResumeAction.gameObject.activeSelf);
            hub.SetHasSavedRoom(true);
            Assert.IsTrue(hub.ResumeAction.gameObject.activeInHierarchy);
            // Let the newly enabled Graphic join the rendered canvas before the first possible tap.
            yield return null;
            yield return Tap(hub.ResumeAction);
            Assert.AreEqual(1, resumed);
            hub.ShowGameSelection();
            Assert.IsFalse(hub.ResumeAction.gameObject.activeSelf);
            hub.SetHasSavedRoom(true);
            Assert.IsFalse(hub.ResumeAction.gameObject.activeSelf);
            hub.ShowCatalogue();
            Assert.IsTrue(hub.ResumeAction.gameObject.activeSelf);
            hub.SetHasSavedRoom(false);
            Assert.IsFalse(hub.ResumeAction.gameObject.activeSelf);
            Assert.IsEmpty(opened);
        }

        [UnityTest]
        public IEnumerator JoinAndSettingsAreIndependentAndFooterNeverMoves()
        {
            AssertFooter();
            yield return Tap(hub.SecondaryAction);
            yield return Tap(hub.SettingsAction);
            Assert.AreEqual(1, joined);
            Assert.AreEqual(1, settings);
            hub.SetHasSavedRoom(true); AssertFooter();
            hub.ShowGameSelection(); AssertFooter();
            hub.ShowCatalogue(); AssertFooter();
            Assert.IsEmpty(opened);
        }

        [UnityTest]
        public IEnumerator CompactSafeAreaKeepsFutureCardsAboveSavedResumeAndFooterReachable()
        {
            // Keep the Canvas at its real screen size and constrain only the safe-area child.
            Object.Destroy(hub);
            foreach (Transform child in owner.transform) Object.Destroy(child.gameObject);
            yield return null;
            var safeObject = new GameObject("Compact safe area 430 by 850", typeof(RectTransform));
            var safeArea = (RectTransform)safeObject.transform;
            safeArea.SetParent(owner.transform, false);
            safeArea.anchorMin = safeArea.anchorMax = safeArea.pivot = new Vector2(.5f, .5f);
            safeArea.sizeDelta = new Vector2(430, 850);
            hub = safeObject.AddComponent<GameHubView>();
            hub.Initialize(new AppPreferences(new MemoryStore()), id => opened.Add(id),
                () => joined++, () => settings++, () => resumed++);
            hub.SetHasSavedRoom(true);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual(new Vector2(430, 850), safeArea.rect.size);
            AssertFooter();
            var resumeCorners = new Vector3[4];
            ((RectTransform)hub.ResumeAction.transform).GetWorldCorners(resumeCorners);
            var resumeTop = resumeCorners.Max(corner => corner.y);
            foreach (var id in new[] { "president", "mimes" })
            {
                var corners = new Vector3[4];
                ((RectTransform)Find("Hub game " + id + " unavailable").transform).GetWorldCorners(corners);
                Assert.GreaterOrEqual(corners.Min(corner => corner.y) - resumeTop,
                    10f * safeArea.lossyScale.y, id + " must stay above the saved-resume button with a visible gap");
            }
            yield return Tap(hub.ResumeAction);
            Assert.AreEqual(1, resumed);
            Assert.IsEmpty(opened);
            AssertFooter();
        }

        [UnityTest]
        public IEnumerator AppCatalogueVisitsWerewolfLandingAndReturnsWithoutCreatingRoom()
        {
            Object.Destroy(owner);
            foreach (var existing in Object.FindObjectsByType<StorybookApp>()) Object.Destroy(existing.gameObject);
            yield return null;
            integrationPreferencesChanged = true;
            originalFrameRate = Application.targetFrameRate; originalOrientation = Screen.orientation;
            hadServer = PlayerPrefs.HasKey("ngame.network.server");
            originalServer = PlayerPrefs.GetString("ngame.network.server");
            hadAudio = PlayerPrefs.HasKey(AppPreferences.AmbientSoundKey);
            hadMotion = PlayerPrefs.HasKey(AppPreferences.ReducedMotionKey);
            originalAudio = PlayerPrefs.GetInt(AppPreferences.AmbientSoundKey);
            originalMotion = PlayerPrefs.GetInt(AppPreferences.ReducedMotionKey);
            PlayerPrefs.SetString("ngame.network.server", "http://127.0.0.1:18991/hub-route-fixture");
            PlayerPrefs.SetInt(AppPreferences.AmbientSoundKey, 0);
            PlayerPrefs.SetInt(AppPreferences.ReducedMotionKey, 1);
            owner = new GameObject("Hub application route fixture");
            var app = owner.AddComponent<StorybookApp>();
            yield return null;
            yield return null;
            Assert.AreEqual(AppRoute.Home, app.Route);
            Assert.IsTrue(app.HubBackground.gameObject.activeInHierarchy);
            var hadSession = app.NetworkClient.HasSession;
            var roomCode = app.NetworkClient.RoomCode;
            Assert.IsFalse(app.NetworkClient.IsBusy);
            Assert.IsFalse(app.NetworkClient.HasPendingCommand);
            yield return Tap(Find("Hub game werewolf"));
            Assert.AreEqual(AppRoute.GameHome, app.Route);
            Assert.IsFalse(app.HubBackground.gameObject.activeInHierarchy);
            Assert.IsTrue(Find("Open real gatherings").gameObject.activeInHierarchy);
            yield return Tap(Find("Safe application navigation"));
            Assert.AreEqual(AppRoute.Home, app.Route);
            Assert.IsTrue(app.HubBackground.gameObject.activeInHierarchy);
            Assert.AreEqual(hadSession, app.NetworkClient.HasSession);
            Assert.AreEqual(roomCode, app.NetworkClient.RoomCode);
            Assert.IsFalse(app.NetworkClient.IsBusy);
            Assert.IsFalse(app.NetworkClient.HasPendingCommand);
        }

        private void AssertFooter()
        {
            var primary = (RectTransform)hub.PrimaryAction.transform;
            var secondary = (RectTransform)hub.SecondaryAction.transform;
            Assert.AreEqual(new Vector2(0, 124), primary.anchoredPosition);
            Assert.AreEqual(new Vector2(382, 56), primary.sizeDelta);
            Assert.AreEqual(new Vector2(0, 62), secondary.anchoredPosition);
            Assert.AreEqual(new Vector2(382, 48), secondary.sizeDelta);
        }

        private Button Find(string name) => owner.GetComponentsInChildren<Button>(true)
            .Single(b => b.name == name && b.gameObject.activeInHierarchy);

        private IEnumerator Tap(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current)
            {
                pointerId = 83, button = PointerEventData.InputButton.Left,
                position = position, pressPosition = position,
                pointerPress = button.gameObject, rawPointerPress = button.gameObject
            };
            hits.Clear(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, button.name + " must be reachable by raycast");
            pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
            Assert.AreEqual(button.gameObject, ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),
                button.name + " must not be covered by another control");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
        }

        private static void RestoreBool(string key, bool existed, int value)
        { if (existed) PlayerPrefs.SetInt(key, value); else PlayerPrefs.DeleteKey(key); }
    }
}
