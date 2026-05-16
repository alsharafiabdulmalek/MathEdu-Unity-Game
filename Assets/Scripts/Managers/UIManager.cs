// -----------------------------------------------------------------------------
// UIManager.cs
// -----------------------------------------------------------------------------
// Lightweight scene-flow coordinator. Centralizes scene names so the rest of
// the code never hard-codes them, and adds a tiny fade-to-black helper.
// Scenes are loaded by name; if a scene is not registered in Build Settings
// the loader logs a clear warning so the developer knows what to fix.
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
        public const string SceneBootstrap   = "Bootstrap";
        public const string SceneMainMenu    = "MainMenu";
        public const string SceneLevelSelect = "LevelSelect";
        public const string SceneModeSelect  = "ModeSelect";
        public const string SceneLearn       = "LearnMode";
        public const string ScenePractice    = "PracticeMode";
        public const string SceneQuiz        = "QuizMode";
        public const string SceneStory       = "StoryMode";
        public const string SceneSpeed       = "SpeedRound";
        public const string SceneResults     = "Results";

        public bool transitionInFlight { get; private set; }

        public void Go(string sceneName, float fade = 0.25f)
        {
            if (transitionInFlight) return;
            StartCoroutine(GoRoutine(sceneName, fade));
        }

        private IEnumerator GoRoutine(string sceneName, float fade)
        {
            transitionInFlight = true;
            var fader = FadeOverlay.Acquire();
            yield return fader.FadeTo(1f, fade);

            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogWarning($"[UIManager] Scene '{sceneName}' not in Build Settings.");
                transitionInFlight = false;
                yield break;
            }
            while (!op.isDone) yield return null;

            yield return fader.FadeTo(0f, fade);
            transitionInFlight = false;
        }
    }
}
