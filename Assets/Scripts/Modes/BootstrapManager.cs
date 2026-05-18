// -----------------------------------------------------------------------------
// BootstrapManager.cs
// -----------------------------------------------------------------------------
// Tiny scene that ensures all root managers exist, shows a quick splash, then
// jumps to either:
//   - PlayerSetup on first launch, or whenever the profile has been wiped.
//   - MainMenu otherwise.
// -----------------------------------------------------------------------------

using MathEdu.Managers;
using MathEdu.UI;
using MathEdu.Utility;
using TMPro;
using UnityEngine;

namespace MathEdu.Modes
{
    public class BootstrapManager : MonoBehaviour
    {
        [Tooltip("Seconds the splash logo stays visible before the next scene loads.")]
        public float splashSeconds = 1.2f;

        private void Start()
        {
            _ = GameManager.Instance;

            var (canvas, safe) = UIFactory.CreateCanvas("[BootstrapCanvas]");
            UIFactory.CreateThemedBackground(safe, "setup");

            var logo = UIFactory.CreateText(safe, Localization.T("boot.app_name"), 160,
                Color.white, TMPro.TextAlignmentOptions.Center, "Logo");
            logo.fontStyle = FontStyles.Bold;
            var lrt = logo.rectTransform;
            lrt.anchorMin = new Vector2(0, 0.5f); lrt.anchorMax = new Vector2(1, 0.65f);

            var tag = UIFactory.CreateText(safe, Localization.T("boot.tagline"), 56,
                new Color(1, 1, 1, 0.85f), TMPro.TextAlignmentOptions.Center, "Tag");
            tag.fontStyle = FontStyles.Italic;
            var trt = tag.rectTransform;
            trt.anchorMin = new Vector2(0, 0.4f); trt.anchorMax = new Vector2(1, 0.5f);

            Invoke(nameof(GoToNextScene), splashSeconds);
        }

        private void GoToNextScene()
        {
            var profile = GameManager.Instance.Profile;
            string scene = (profile != null && profile.setupComplete)
                ? UIManager.SceneMainMenu
                : UIManager.ScenePlayerSetup;
            GameManager.Instance.UI.Go(scene, 0.4f);
        }
    }
}
