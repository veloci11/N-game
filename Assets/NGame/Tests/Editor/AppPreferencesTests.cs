using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NGame.Core.Tests
{
    public sealed class AppPreferencesTests
    {
        private sealed class MemoryStore : IPreferenceStore
        {
            public readonly Dictionary<string, bool> Values = new Dictionary<string, bool>();
            public int Saves;
            public bool ReadBool(string key, bool fallback) => Values.TryGetValue(key, out var value) ? value : fallback;
            public void WriteBool(string key, bool value) => Values[key] = value;
            public void Save() => Saves++;
        }

        [Test]
        public void FreshPreferencesLeaveAudioAndReducedMotionDisabledWithoutWriting()
        {
            var store = new MemoryStore();
            var preferences = new AppPreferences(store);
            Assert.That(preferences.AmbientSound, Is.False);
            Assert.That(preferences.ReducedMotion, Is.False);
            Assert.That(preferences.PublicNarration, Is.True, "Table calls are enabled independently from optional ambience.");
            Assert.That(store.Saves, Is.Zero);
            Assert.That(store.Values, Is.Empty);
        }

        [Test]
        public void SavedChoicesAreReadAndEachChangeIsPersistedBeforeNotifying()
        {
            var store = new MemoryStore();
            store.Values[AppPreferences.ReducedMotionKey] = true;
            var preferences = new AppPreferences(store);
            Assert.That(preferences.ReducedMotion, Is.True);
            var events = 0;
            preferences.Changed += () =>
            {
                events++;
                Assert.That(store.Values[AppPreferences.AmbientSoundKey], Is.EqualTo(preferences.AmbientSound));
                Assert.That(store.Saves, Is.GreaterThan(0));
            };
            preferences.SetAmbientSound(true);
            Assert.That(events, Is.EqualTo(1));
            Assert.That(store.Saves, Is.EqualTo(1));
            preferences.SetReducedMotion(false);
            Assert.That(store.Values[AppPreferences.ReducedMotionKey], Is.False);
            Assert.That(events, Is.EqualTo(2));
        }

        [Test]
        public void SettingTheCurrentChoiceDoesNotWriteOrNotify()
        {
            var store = new MemoryStore();
            var preferences = new AppPreferences(store);
            var events = 0;
            preferences.Changed += () => events++;
            preferences.SetAmbientSound(false);
            preferences.SetReducedMotion(false);
            Assert.That(events, Is.Zero);
            Assert.That(store.Saves, Is.Zero);
        }

        [Test]
        public void MissingPreferenceStoreIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new AppPreferences(null));
        }

        [Test]
        public void TableNarrationCanBeDisabledAndRestoredWithoutEnablingAmbientSound()
        {
            var store = new MemoryStore();
            var preferences = new AppPreferences(store);
            preferences.SetPublicNarration(false);
            Assert.That(store.Values[AppPreferences.PublicNarrationKey], Is.False);
            Assert.That(store.Saves, Is.EqualTo(1));
            var restored = new AppPreferences(store);
            Assert.That(restored.PublicNarration, Is.False);
            Assert.That(restored.AmbientSound, Is.False);
        }
    }
}
