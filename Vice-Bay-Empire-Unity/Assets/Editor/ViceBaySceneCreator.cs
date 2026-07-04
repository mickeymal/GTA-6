#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViceBayEmpire.Bootstrap;

namespace ViceBayEmpire.EditorTools
{
    /// <summary>
    /// One-click scene setup. Run  ViceBay ▸ Create ViceBay_MainScene  from the menu bar
    /// and it creates (or overwrites) Assets/Scenes/ViceBay_MainScene.unity containing a
    /// single "GameBootstrap" object. Press Play in that scene and the whole city builds
    /// itself. This is the only Editor step required — everything else is runtime code.
    /// </summary>
    public static class ViceBaySceneCreator
    {
        const string ScenePath = "Assets/Scenes/ViceBay_MainScene.unity";

        [MenuItem("ViceBay/Create ViceBay_MainScene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // the single bootstrap object that builds the entire world at Play
            var boot = new GameObject("GameBootstrap");
            boot.AddComponent<GameBootstrap>();

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            // add it to Build Settings as scene 0 so builds start here
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == ScenePath))
                list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();

            EditorUtility.DisplayDialog("Vice Bay",
                "Created Assets/Scenes/ViceBay_MainScene.unity.\n\nPress Play to build and explore the city.",
                "Let's go");
        }

        [MenuItem("ViceBay/Open ViceBay_MainScene")]
        public static void OpenScene()
        {
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else CreateScene();
        }
    }
}
#endif
