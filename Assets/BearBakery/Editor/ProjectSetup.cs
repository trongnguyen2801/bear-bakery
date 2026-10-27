// Batch-mode entry points: project settings, scene creation, builds.
using System.IO;
using System.Linq;
using BB.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BB.EditorTools
{
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("BearBakery/Setup Project")]
        public static void Setup()
        {
            PlayerSettings.companyName = "Prototype";
            PlayerSettings.productName = "Tiệm Bánh Gấu";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.prototype.tiembanhgau");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.prototype.tiembanhgau");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.prototype.tiembanhgau");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 390;
            PlayerSettings.defaultScreenHeight = 844;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            // legacy Input Manager (Input.touches / mouse); no Input System package in this project
            var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            if (ps != null)
            {
                var so = new SerializedObject(ps);
                var p = so.FindProperty("activeInputHandler");
                if (p != null) { p.intValue = 0; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
            // icon from the provided set
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BearBakery/Resources/Art/ui/Illust_Bear01__Atlas_CanvasUI_p0.png");
            if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            CreateScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] done");
        }

        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var boot = new GameObject("GameBoot");
            boot.AddComponent<GameBoot>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        [MenuItem("BearBakery/Build macOS")]
        public static void BuildMac()
        {
            Setup();
            var outPath = "Builds/macOS/TiemBanhGau.app";
            var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = outPath, target = BuildTarget.StandaloneOSX, options = BuildOptions.None
            });
            Debug.Log($"[Build] macOS result={r.summary.result} size={r.summary.totalSize} errors={r.summary.totalErrors} -> {outPath}");
            if (Application.isBatchMode) EditorApplication.Exit(r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }

        [MenuItem("BearBakery/Build Android APK")]
        public static void BuildAndroid()
        {
            Setup();
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            EditorUserBuildSettings.buildAppBundle = false;
            var outPath = "Builds/Android/TiemBanhGau.apk";
            var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = outPath, target = BuildTarget.Android, options = BuildOptions.None
            });
            Debug.Log($"[Build] Android result={r.summary.result} size={r.summary.totalSize} errors={r.summary.totalErrors} -> {outPath}");
            if (Application.isBatchMode) EditorApplication.Exit(r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>Loads every manifest sprite through Resources to prove they are integrated (asset audit).</summary>
        public static void VerifyArt()
        {
            Art.Init();
            int ok = 0; var missing = new System.Collections.Generic.List<string>();
            foreach (var id in Art.Ids.ToList())
            {
                var path = Art.PathOf(id);
                var sp = Resources.Load<Sprite>(path);
                if (sp != null) ok++; else missing.Add(id + " -> " + path);
            }
            var report = $"Sprites in manifest: {Art.Count}\nLoaded via Resources.Load<Sprite>: {ok}\nMissing: {missing.Count}\n" + string.Join("\n", missing);
            File.WriteAllText("Docs/art_load_check.txt", report);
            Debug.Log("[VerifyArt] " + report);
            if (Application.isBatchMode) EditorApplication.Exit(missing.Count == 0 ? 0 : 1);
        }
    }
}
