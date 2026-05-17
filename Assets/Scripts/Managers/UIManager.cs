// -----------------------------------------------------------------------------
// UIManager.cs
// -----------------------------------------------------------------------------
// Lightweight scene-flow coordinator. Centralizes scene names so the rest of
// the code never hard-codes them, and wraps every scene change in a fade
// transition.
//
// Scene loading rules:
//   1. FadeOverlay fades from current alpha → 1 over `fade` seconds.
//   2. SceneManager.LoadSceneAsync(name)
//   3. New scene Start() runs (managers rebuild their UI).
//   4. FadeOverlay fades from 1 → 0 over `fade` seconds.
//
// The FadeOverlay Canvas is created lazily, marked DontDestroyOnLoad, and
// kept across all subsequent scene loads so the fade is seamless.
// -----------------------------------------------------------------------------

using System.Collections;
using MathEdu.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathEdu.Managers
{
    public class UIManager : MonoBehaviour
    {
        // Canonical scene names. Keep in sync with the .unity files in
        // Assets/Scenes/ and with EditorBuildSettings.scenes.
        public const string SceneBootstrap        = "Bootstrap";
        public const string ScenePlayerSetup      = "PlayerSetup";
        public const string SceneMainMenu         = "MainMenu";
        public const string SceneLevelSelect      = "LevelSelect";
        public const string SceneModeSelect       = "ModeSelect";
        public const string SceneLearn            = "LearnMode";
        public const string ScenePractice         = "PracticeMode";
        public const string SceneQuiz             = "QuizMode";
        public const string SceneStory            = "StoryMode";
        public const string SceneSpeed            = "SpeedRound";
        public const string SceneResults          = "Results";
        public const string SceneSettings         = "Settings";
        public const string SceneParentalDashboard = "ParentalDashboard";

        public bool transitionInFlight { get; private set; }

        /// <summary>
        /// Predefined parent-scene mapping used by Back buttons. Falling back
        /// to MainMenu for any scene without a registered parent keeps the
        /// player from getting stuck.
        /// </summary>
        public static string ParentSceneOf(string scene)
        {
            return scene switch
            {
                SceneLevelSelect      => SceneMainMenu,
                SceneModeSelect       => SceneLevelSelect,
                SceneLearn            => SceneModeSelect,
                ScenePractice         => SceneModeSelect,
                SceneQuiz             => SceneModeSelect,
                SceneStory            => SceneModeSelect,
                SceneSpeed            => SceneModeSelect,
                SceneResults          => SceneMainMenu,
                SceneSettings         => SceneMainMenu,
                SceneParentalDashboard=> SceneMainMenu,
                _                     => SceneMainMenu
            };
        }

        public void Go(string sceneName, float fade = 0.25f)
        {
            if (transitionInFlight) return;
            StartCoroutine(GoRoutine(sceneName, fade));
        }

        private IEnumerator GoRoutine(string sceneName, float fade)
        {
            transitionInFlight = true;

            // Soft whoosh on every transition.
            var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
            if (audio != null) audio.PlaySFX("pageTransition");

            var fader = FadeOverlay.Acquire();
            yield return fader.FadeTo(1f, fade);

            AsyncOperation op = null;
            try
            {
                op = SceneManager.LoadSceneAsync(sceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Failed to load scene '{sceneName}': {e.Message}");
            }

            if (op == null)
            {
                Debug.LogWarning($"[UIManager] Scene '{sceneName}' not in Build Settings.");
                yield return fader.FadeTo(0f, fade);
                transitionInFlight = false;
                yield break;
            }

            while (!op.isDone) yield return null;

            yield return fader.FadeTo(0f, fade);
            transitionInFlight = false;
        }
    }
}
