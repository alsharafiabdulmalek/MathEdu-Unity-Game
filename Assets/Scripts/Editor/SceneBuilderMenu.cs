// -----------------------------------------------------------------------------
// SceneBuilderMenu.cs
// -----------------------------------------------------------------------------
// One-click scene generator. Creates minimal .unity scenes containing a single
// "[SceneRoot]" GameObject with the matching MonoBehaviour attached. The
// MonoBehaviour builds the entire Canvas + EventSystem + UI procedurally at
// runtime (see UIFactory + each Mode manager).
//
// Run this once after cloning:
//   MathEdu / Build All Scenes
//
// It also registers the new scenes in EditorBuildSettings in the correct
// order, so File > Build Settings is already configured.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using MathEdu.Gameplay;
using MathEdu.Managers;
using MathEdu.Modes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathEdu.EditorTools
{
    public static class SceneBuilderMenu
    {
        private const string SceneDir = "Assets/Scenes";

        // Scene name -> Manager type that builds the screen at runtime.
        // Order matters for File > Build Settings (Bootstrap must be scene 0).
        private static readonly (string name, System.Type type)[] Scenes =
        {
            (UIManager.SceneBootstrap,          typeof(BootstrapManager)),
            (UIManager.ScenePlayerSetup,        typeof(PlayerSetupManager)),
            (UIManager.SceneMainMenu,           typeof(MainMenuManager)),
            (UIManager.SceneLevelSelect,        typeof(LevelSelectManager)),
            (UIManager.SceneModeSelect,         typeof(ModeSelectManager)),
            (UIManager.SceneLearn,              typeof(LearnModeManager)),
            (UIManager.ScenePractice,           typeof(PracticeModeManager)),
            (UIManager.SceneQuiz,               typeof(QuizModeManager)),
            (UIManager.SceneStory,              typeof(StoryModeManager)),
            (UIManager.SceneSpeed,              typeof(SpeedRoundManager)),
            (UIManager.SceneResults,            typeof(ResultsManager)),
            (UIManager.SceneSettings,           typeof(SettingsManager)),
            (UIManager.SceneParentalDashboard,  typeof(ParentalDashboardManager)),
        };

        [MenuItem("MathEdu/Build All Scenes", priority = 5)]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(SceneDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            var paths = new List<EditorBuildSettingsScene>();
            foreach (var (name, type) in Scenes)
            {
                string path = $"{SceneDir}/{name}.unity";
                CreateOrReplaceScene(path, type);
                paths.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = paths.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("MathEdu",
                $"Created {Scenes.Length} scenes and registered them in Build Settings.\n\nFirst scene is '{Scenes[0].name}'.",
                "OK");
        }

        private static void CreateOrReplaceScene(string path, System.Type managerType)
        {
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var go = new GameObject("[SceneRoot]");
            go.AddComponent(managerType);

            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
#endif
