// -----------------------------------------------------------------------------
// BootstrapManager.cs
// -----------------------------------------------------------------------------
// Tiny scene that ensures all root managers exist, shows a quick splash, then
// jumps to either:
//   - PlayerSetup on first launch, or whenever the profile has been wiped.
//   - MainMenu otherwise.
//
// Polish: the splash now includes an animated bouncing icon (book/star/sparkle)
// and a soft scale-in for the wordmark + tagline so the first frame the user
// sees feels alive.
// -----------------------------------------------------------------------------

using System.Collections;
using MathEdu.Managers;
using MathEdu.UI;
using MathEdu.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.Modes
{
    public class BootstrapManager : MonoBehaviour
    {
        [Tooltip("Seconds the splash logo stays visible before the next scene loads.")]
        public float splashSeconds = 1.5f;

        private void Start()
        {
            _ = GameManager.Instance;

            var (canvas, safe) = UIFactory.CreateCanvas("[BootstrapCanvas]");
            UIFactory.CreateThemedBackground(safe, "setup");

            // Animated mark — a circle background with a bouncing emoji on top.
            var markGo = new GameObject("Mark", typeof(RectTransform), typeof(Image));
            markGo.transform.SetParent(safe, false);
            var markRt = (RectTransform)markGo.transform;
            markRt.anchorMin = new Vector2(0.30f, 0.65f);
            markRt.anchorMax = new Vector2(0.70f, 0.85f);
            markRt.offsetMin = Vector2.zero; markRt.offsetMax = Vector2.zero;
            var markImg = markGo.GetComponent<Image>();
            markImg.sprite = DefaultSprite.Circle();
            markImg.color  = new Color(1f, 1f, 1f, 0.18f);
            markImg.raycastTarget = false;

            var markIco = UIFactory.CreateText(markRt, "📚", 200, Color.white,
                TextAlignmentOptions.Center, "MarkIco");
            markIco.fontStyle = FontStyles.Bold;

            var logo = UIFactory.CreateText(safe, Localization.T("boot.app_name"), 160,
                Color.white, TMPro.TextAlignmentOptions.Center, "Logo");
            logo.fontStyle = FontStyles.Bold;
            var lrt = logo.rectTransform;
            lrt.anchorMin = new Vector2(0, 0.40f); lrt.anchorMax = new Vector2(1, 0.55f);

            var tag = UIFactory.CreateText(safe, Localization.T("boot.tagline"), 56,
                new Color(1, 1, 1, 0.85f), TMPro.TextAlignmentOptions.Center, "Tag");
            tag.fontStyle = FontStyles.Italic;
            var trt = tag.rectTransform;
            trt.anchorMin = new Vector2(0, 0.30f); trt.anchorMax = new Vector2(1, 0.40f);

            StartCoroutine(SplashAnimation(markRt, logo.rectTransform, tag.rectTransform));
            Invoke(nameof(GoToNextScene), splashSeconds);
        }

        private IEnumerator SplashAnimation(RectTransform mark, RectTransform logo, RectTransform tag)
        {
            // Scale-in
            mark.localScale = Vector3.zero;
            logo.localScale = Vector3.zero;
            tag.localScale  = Vector3.zero;

            float t = 0f;
            const float dur = 0.45f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                mark.localScale = Vector3.one * EaseOutBack(k);
                yield return null;
            }
            mark.localScale = Vector3.one;

            t = 0;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                logo.localScale = Vector3.one * EaseOutBack(k);
                tag.localScale  = Vector3.one * EaseOutBack(k * 0.85f);
                yield return null;
            }
            logo.localScale = Vector3.one;
            tag.localScale  = Vector3.one;

            // Soft idle bob on the mark
            float u = 0;
            while (true)
            {
                u += Time.unscaledDeltaTime;
                float bob = Mathf.Sin(u * 3.5f) * 0.05f;
                mark.localScale = Vector3.one * (1f + bob);
                yield return null;
            }
        }

        private static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float kx = k - 1f;
            return 1f + c3 * kx * kx * kx + c1 * kx * kx;
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
