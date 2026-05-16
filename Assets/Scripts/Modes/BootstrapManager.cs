// -----------------------------------------------------------------------------
// BootstrapManager.cs
// -----------------------------------------------------------------------------
// Tiny scene that ensures all root managers exist, then jumps to MainMenu.
// Place this script on the only GameObject in the Bootstrap scene.
// -----------------------------------------------------------------------------

using MathEdu.Managers;
using MathEdu.UI;
using TMPro;
using UnityEngine;

namespace MathEdu.Modes
{
    public class BootstrapManager : MonoBehaviour
    {
        private void Start()
        {
            _ = GameManager.Instance;

            var (canvas, safe) = UIFactory.CreateCanvas("[BootstrapCanvas]");
            UIFactory.CreateGradientBackground(safe, UIFactory.BgTop, UIFactory.BgBottom);

            var logo = UIFactory.CreateText(safe, "MathEdu", 160,
                Color.white, TMPro.TextAlignmentOptions.Center, "Logo");
            logo.fontStyle = FontStyles.Bold;
            var lrt = logo.rectTransform;
            lrt.anchorMin = new Vector2(0, 0.5f); lrt.anchorMax = new Vector2(1, 0.65f);

            var tag = UIFactory.CreateText(safe, "Learn. Play. Win.", 56,
                new Color(1, 1, 1, 0.85f), TMPro.TextAlignmentOptions.Center, "Tag");
            tag.fontStyle = FontStyles.Italic;
            var trt = tag.rectTransform;
            trt.anchorMin = new Vector2(0, 0.4f); trt.anchorMax = new Vector2(1, 0.5f);

            Invoke(nameof(GoToMainMenu), 1.2f);
        }

        private void GoToMainMenu()
        {
            GameManager.Instance.UI.Go(UIManager.SceneMainMenu, 0.4f);
        }
    }
}
