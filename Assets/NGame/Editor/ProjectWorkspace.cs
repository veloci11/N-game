using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NGame.EditorTools
{
    public static class ProjectWorkspace
    {
        public const string ScenePath = "Assets/NGame/Scenes/Storybook.unity";
        private const string ResourcesPath = "Assets/Resources/Werewolf";

        [MenuItem("NGame/Sync Approved Art")]
        public static void SyncArt()
        {
            var source = Path.GetFullPath(Path.Combine(Application.dataPath, "../ArtSource/Werewolf"));
            Directory.CreateDirectory(ResourcesPath);
            foreach (var path in Directory.GetFiles(source, "*.png"))
                File.Copy(path, Path.Combine(ResourcesPath, Path.GetFileName(path)), true);
            var boards = Path.GetFullPath(Path.Combine(Application.dataPath, "../ArtSource/ApprovedBoards"));
            if (Directory.Exists(boards))
                foreach (var path in Directory.GetFiles(boards, "roles-*.png"))
                    File.Copy(path, Path.Combine(ResourcesPath, Path.GetFileName(path)), true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var path in Directory.GetFiles(ResourcesPath, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.isReadable = Path.GetFileNameWithoutExtension(path) == "avatars";
                importer.SaveAndReimport();
            }
            Debug.Log("[NGame] Approved paper artwork imported.");
        }

        [MenuItem("NGame/Prepare Storybook Scene")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            SyncArt();
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
                TMP_PackageResourceImporter.ImportResources(true, false, false);
            PlayerSettings.companyName = "NGame";
            PlayerSettings.productName = "N’GAME";
            PlayerSettings.defaultScreenWidth = 430;
            PlayerSettings.defaultScreenHeight = 932;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.ngame.storybook");
            EditorSettings.serializationMode = SerializationMode.ForceText;
            var current = EditorSceneManager.GetActiveScene();
            if (current.isDirty) throw new InvalidOperationException("Save the current scene first.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -10);
            var camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(12, 23, 35, 255);
            new GameObject("NGame Application").AddComponent<NGame.Presentation.StorybookApp>();
            Directory.CreateDirectory("Assets/NGame/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            SetPortrait();
            AssetDatabase.SaveAssets();
            Debug.Log("[NGame] New application scene saved: " + ScenePath);
        }

        [MenuItem("NGame/Portrait Game View")]
        public static void SetPortrait()
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType)
                .GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[]
                { Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone") });
            var type = asm.GetType("UnityEditor.GameViewSize");
            var flag = Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
            var size = Activator.CreateInstance(type, new[] { flag, (object)430, 932, "NGame Portrait" });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            var count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            var viewType = asm.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(viewType);
            viewType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(view, count - 1);
            view.Show();
            view.Focus();
        }
    }
}
