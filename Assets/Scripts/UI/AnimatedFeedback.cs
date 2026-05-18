// -----------------------------------------------------------------------------
// AnimatedFeedback.cs
// -----------------------------------------------------------------------------
// Pops a big "✓ Correct!" or "✗ Try again" message in the centre of the
// screen, then fades it out. Used by all gameplay modes to celebrate answers.
//
// Lifecycle:
//   • Spawn() builds the widget and immediately deactivates it so it doesn't
//     occupy screen space until the first answer.
//   • ShowCorrect() / ShowWrong() re-activate the GameObject *before*
//     StartCoroutine — Unity refuses to start a coroutine on an inactive
//     host (logs "Coroutine couldn't be started because the game object
//     'AnimatedFeedback' is inactive!"), and the coroutine's first line
//     (gameObject.SetActive(true)) never runs because the coroutine never
//     starts.
//   • PopFade() ends by deactivating the GameObject again so the next
//     answer can re-activate cleanly.
// -----------------------------------------------------------------------------

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public class AnimatedFeedback : MonoBehaviour
    {
        private TextMeshProUGUI _label;
        private Image _bg;

        public static AnimatedFeedback Spawn(RectTransform parent)
        {
            var go = new GameObject("AnimatedFeedback", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0, 200);

            var pill = UIFactory.CreatePanel(rt, new Vector2(0.1f, 0), new Vector2(0.9f, 1),
                UIFactory.Success, 36, "Pill");
            var bg = pill.GetComponent<Image>();
            var label = UIFactory.CreateText(pill, "", 96, Color.white,
                TMPro.TextAlignmentOptions.Center, "Label");
            label.fontStyle = FontStyles.Bold;

            var fb = go.AddComponent<AnimatedFeedback>();
            fb._label = label;
            fb._bg    = bg;
            go.GetComponent<CanvasGroup>().alpha = 0;
            go.SetActive(false);
            return fb;
        }

        public void ShowCorrect(string msg = "Correct!")
        {
            // Re-activate BEFORE StartCoroutine. Unity won't start coroutines
            // on inactive GameObjects, and PopFade's own SetActive(true) is
            // never reached because the coroutine never starts.
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            StopAllCoroutines();
            _bg.color = UIFactory.Success;
            _label.text = "✓ " + msg;
            StartCoroutine(PopFade());
        }

        public void ShowWrong(string msg = "Try again")
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            StopAllCoroutines();
            _bg.color = UIFactory.Danger;
            _label.text = "✗ " + msg;
            StartCoroutine(PopFade());
        }

        private IEnumerator PopFade()
        {
            // Belt and braces — Spawn() deactivated us once, and ShowCorrect /
            // ShowWrong now re-activate before invoking us; this assignment is
            // defensive in case some other future caller forgets.
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            var cg = GetComponent<CanvasGroup>();
            var rt = (RectTransform)transform;
            cg.alpha = 1;
            rt.localScale = Vector3.one * 0.6f;

            float t = 0;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.05f, t / 0.25f);
                yield return null;
            }
            t = 0;
            while (t < 0.10f)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.one * Mathf.Lerp(1.05f, 1.0f, t / 0.10f);
                yield return null;
            }
            yield return new WaitForSeconds(0.6f);
            t = 0;
            while (t < 0.30f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = 1 - t / 0.30f;
                yield return null;
            }
            cg.alpha = 0;
            gameObject.SetActive(false);
        }
    }
}
