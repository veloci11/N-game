using System;

namespace NGame.Core
{
    public interface IPreferenceStore
    {
        bool ReadBool(string key, bool fallback);
        void WriteBool(string key, bool value);
        void Save();
    }

    public sealed class AppPreferences
    {
        public const string ReducedMotionKey = "ngame.settings.reduced-motion";
        public const string AmbientSoundKey = "ngame.settings.ambient-sound";
        private readonly IPreferenceStore store;
        public bool ReducedMotion { get; private set; }
        public bool AmbientSound { get; private set; }
        public event Action Changed;

        public AppPreferences(IPreferenceStore preferenceStore)
        {
            store = preferenceStore ?? throw new ArgumentNullException(nameof(preferenceStore));
            ReducedMotion = store.ReadBool(ReducedMotionKey, false);
            AmbientSound = store.ReadBool(AmbientSoundKey, false);
        }

        public void SetReducedMotion(bool enabled)
        {
            if (ReducedMotion == enabled) return;
            ReducedMotion = enabled;
            Persist(ReducedMotionKey, enabled);
        }

        public void SetAmbientSound(bool enabled)
        {
            if (AmbientSound == enabled) return;
            AmbientSound = enabled;
            Persist(AmbientSoundKey, enabled);
        }

        private void Persist(string key, bool enabled)
        {
            store.WriteBool(key, enabled);
            store.Save();
            Changed?.Invoke();
        }
    }
}
