#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ViceBayEmpire.EditorTools
{
    /// <summary>
    /// Builds the Vice Bay Empire standalone player. Because the game builds itself at
    /// runtime, the only scene needed is ViceBay_MainScene (created by
    /// ViceBaySceneCreator). Use the menu items, or run headless from CI/command line:
    ///
    ///   Unity -quit -batchmode -projectPath &lt;proj&gt; -executeMethod ViceBayEmpire.EditorTools.BuildScript.BuildWindows
    ///
    /// Requires the matching platform Build Support module installed in the Unity Hub
    /// (Windows / Mac / Linux Build Support). You must build ON or FOR a platform whose
    /// module is installed — the Editor cannot make a Windows .exe without Windows
    /// Build Support present.
    /// </summary>
    public static class BuildScript
    {
        const string ScenePath = "Assets/Scenes/ViceBay_MainScene.unity";

        static string[] Scenes()
        {
            if (!File.Exists(ScenePath))
                ViceBaySceneCreator.CreateScene();   // generate the scene if missing
            return new[] { ScenePath };
        }

        [MenuItem("ViceBay/Build/Windows (.exe)")]
        public static void BuildWindows() =>
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/ViceBayEmpire.exe");

        [MenuItem("ViceBay/Build/macOS (.app)")]
        public static void BuildMac() =>
            Build(BuildTarget.StandaloneOSX, "Builds/macOS/ViceBayEmpire.app");

        [MenuItem("ViceBay/Build/Linux")]
        public static void BuildLinux() =>
            Build(BuildTarget.StandaloneLinux64, "Builds/Linux/ViceBayEmpire.x86_64");

        static void Build(BuildTarget target, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var options = new BuildPlayerOptions
            {
                scenes = Scenes(),
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[ViceBay] Build succeeded: {outputPath} ({summary.totalSize / (1024 * 1024)} MB)");
            else
                Debug.LogError($"[ViceBay] Build FAILED: {summary.result} ({summary.totalErrors} errors)");
        }
    }
}
#endif
