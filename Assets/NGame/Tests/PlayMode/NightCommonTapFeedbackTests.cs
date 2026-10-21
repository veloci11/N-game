using System.Collections;
using NGame.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NGame.Presentation.Tests
{
    public sealed class NightCommonTapFeedbackTests
    {
        private sealed class Store : IPreferenceStore
        {
            public bool ReadBool(string key, bool fallback) => fallback;
            public void WriteBool(string key, bool value) { }
            public void Save() { }
        }
        private GameObject owner;
        private NightCommonTapFeedback feedback;
        private AppPreferences preferences;
        private bool night, blocked;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            owner = new GameObject("Common night feedback fixture", typeof(RectTransform), typeof(Canvas));
            preferences = new AppPreferences(new Store()); preferences.SetReducedMotion(false);
            feedback = owner.AddComponent<NightCommonTapFeedback>();
            feedback.Initialize(preferences, () => night, () => blocked);
            night = true; blocked = false;
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown() { Object.Destroy(owner); yield return null; }

        [UnityTest]
        public IEnumerator EachAllowedContactUsesTheSameFixedNonBlockingLightThenFades()
        {
            feedback.RegisterTap(); Assert.That(feedback.IsShowing, Is.True);
            var light = owner.GetComponentInChildren<Image>();
            var position = light.rectTransform.anchoredPosition;
            var color = light.color;
            Assert.That(light.raycastTarget, Is.False);
            Assert.That(light.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            feedback.RegisterTap();
            Assert.That(feedback.FeedbackCount, Is.EqualTo(2));
            Assert.That(light.rectTransform.anchoredPosition, Is.EqualTo(position));
            Assert.That(light.color, Is.EqualTo(color), "No role, touch location or score changes the feedback.");
            yield return new WaitForSecondsRealtime(.55f);
            Assert.That(feedback.IsShowing, Is.False);
        }
        [UnityTest]
        public IEnumerator PhaseMenuAndReducedMotionStopFeedbackWithoutReplayingIt()
        {
            feedback.RegisterTap(); night = false; yield return null;
            Assert.That(feedback.IsShowing, Is.False);
            feedback.RegisterTap(); Assert.That(feedback.FeedbackCount, Is.EqualTo(1));
            night = true; feedback.RegisterTap(); blocked = true; yield return null;
            Assert.That(feedback.IsShowing, Is.False);
            blocked = false; preferences.SetReducedMotion(true); feedback.RegisterTap();
            Assert.That(feedback.IsShowing, Is.False);
            Assert.That(feedback.FeedbackCount, Is.EqualTo(2));
            preferences.SetReducedMotion(false); yield return null;
            Assert.That(feedback.IsShowing, Is.False, "Changing a preference does not replay an old touch.");
        }
        [UnityTest]
        public IEnumerator FocusAndPauseMaskImmediatelyAndDoNotReplayAfterReturning()
        {
            feedback.RegisterTap(); owner.SendMessage("OnApplicationFocus", false);
            Assert.That(feedback.IsShowing, Is.False);
            feedback.RegisterTap(); Assert.That(feedback.FeedbackCount, Is.EqualTo(1));
            owner.SendMessage("OnApplicationFocus", true); yield return null;
            Assert.That(feedback.IsShowing, Is.False);
            feedback.RegisterTap(); owner.SendMessage("OnApplicationPause", true);
            Assert.That(feedback.IsShowing, Is.False);
            owner.SendMessage("OnApplicationPause", false); yield return null;
            Assert.That(feedback.IsShowing, Is.False);
        }
    }
}
