using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NGame.Presentation.Game
{
    /// <summary>Only a resume capability, never roles or private projections. Native storage lives in the app-private directory.</summary>
    internal static class RoomResumeStore
    {
        [Serializable] internal sealed class SavedAccess
        {
            public string server;
            public string roomCode;
            public string playerId;
            public string token;
        }

        private static string PathName => Path.Combine(Application.persistentDataPath, "room-access.json");
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void NGameSaveAccess(string json);
        [DllImport("__Internal")] private static extern IntPtr NGameReadAccess();
        [DllImport("__Internal")] private static extern void NGameFreeAccess(IntPtr pointer);
#elif UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal", SetLastError = true)] private static extern int chmod(string path, uint mode);
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_ANDROID || UNITY_STANDALONE_LINUX
        [DllImport("libc", SetLastError = true)] private static extern int chmod(string path, uint mode);
#endif

        public static SavedAccess Read(string server)
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                var pointer = NGameReadAccess();
                var json = pointer == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(pointer);
                if (pointer != IntPtr.Zero) NGameFreeAccess(pointer);
#else
                var json = File.Exists(PathName) ? File.ReadAllText(PathName) : null;
#endif
                var value = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SavedAccess>(json);
                return value != null && value.server == server && !string.IsNullOrEmpty(value.token) ? value : null;
            }
            catch (Exception) { return null; }
        }

        public static void Save(string server, string code, string playerId, string token)
        {
            var json = JsonUtility.ToJson(new SavedAccess { server = server, roomCode = code, playerId = playerId, token = token });
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                NGameSaveAccess(json);
#else
                Directory.CreateDirectory(Application.persistentDataPath);
                var temporary = PathName + ".tmp";
                File.WriteAllText(temporary, json);
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE_LINUX
                chmod(temporary, 384); // 0600: this OS user only.
#endif
                if (File.Exists(PathName)) File.Replace(temporary, PathName, null);
                else File.Move(temporary, PathName);
#endif
            }
            catch (Exception) { /* An unwritable profile never prevents the live game. No secret is logged. */ }
        }

        public static void Clear()
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                NGameSaveAccess("");
#else
                if (File.Exists(PathName)) File.Delete(PathName);
                if (File.Exists(PathName + ".tmp")) File.Delete(PathName + ".tmp");
#endif
            }
            catch (Exception) { }
        }
    }
}
