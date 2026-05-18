// -----------------------------------------------------------------------------
// AnimatedFeedback.cs
// -----------------------------------------------------------------------------
// Pops a big "✓ Correct!" or "✗ Try again" message in the centre of the
// screen, then fades it out. Used by all gameplay modes to celebrate answers.
//
// Polish enhancements:
//   • Backed by ShadowedRoundedRect for a lifted feel.
//   • Reaction face glyph or sprite on the left side of the pill.
//   • Emoji-burst behind the pill on correct answers (small puff) and a
//     larger streak burst when the optional `streakCount` is >= 3.
//   • Subtle scale → wobble → fade easing using EaseOutBack.
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
        private Image _faceSprite;
        private TextMeshProUGUI _faceGlyph;
        private RectTransform _safeArea;

        public static AnimatedFeedback Spawn(RectTransform parent)
        {
            var go = new GameObject("AnimatedFeedback", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0, 220);

            // Pill background (shadowed for a "popped" feel).
            var pillGo = new GameObject("Pill", typeof(Image));
            pillGo.transform.SetParent(rt, false);
            var pillRt = (RectTransform)pillGo.transform;
            pillRt.anchorMin = new Vector2(0.10f, 0);
            pillRt.anchorMax = new Vector2(0.90f, 1);
            pillRt.offsetMin = Vector2.zero; pillRt.offsetMax = Vector2.zero;
            var bg = pillGo.GetComponent<Image>();
            bg.color  = UIFactory.Success;
            bg.sprite = PolishSprites.ShadowedRoundedRect(40);
            bg.type   = Image.Type.Sliced;
            bg.raycastTarget = false;

            // Reaction face puck inside the pill, left-aligned.
            var faceHolder = new GameObject("FaceHolder", typeof(Image));
            faceHolder.transform.SetParent(pillRt, false);
            var fhRt = (RectTransform)faceHolder.transform;
            fhRt.anchorMin = new Vector2(0.03f, 0.15f);
            fhRt.anchorMax = new Vector2(0.18f, 0.85f);
            fhRt.offsetMin = Vector2.zero; fhRt.offsetMax = Vector2.zero;
            var fhImg = faceHolder.GetComponent<Image>();
            fhImg.sprite = DefaultSprite.Circle();
            fhImg.color  = new Color(1f, 1f, 1f, 0.85f);
            fhImg.raycastTarget = false;

            // Sprite face (if available)
            var spriteGo = new GameObject("FaceSprite", typeof(Image));
            spriteGo.transform.SetParent(fhRt, false);
            var sRt = (RectTransform)spriteGo.transform;
            sRt.anchorMin = new Vector2(0.10f, 0.10f);
            sRt.anchorMax = new Vector2(0.90f, 0.90f);
            sRt.offsetMin = Vector2.zero; sRt.offsetMax = Vector2.zero;
            var faceSprite = spriteGo.GetComponent<Image>();
            faceSprite.preserveAspect = true;
            faceSprite.raycastTarget = false;
            faceSprite.enabled = false;

            var faceGlyph = UIFactory.CreateText(fhRt, "😊", 110, Color.white,
                TMPro.TextAlignmentOptions.Center, "FaceGlyph");
            faceGlyph.fontStyle = FontStyles.Bold;

            // Main label
            var label = UIFactory.CreateText(pillRt, "", 96, Color.white,
                TMPro.TextAlignmentOptions.Center, "Label");
            label.fontStyle = FontStyles.Bold;
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0.20f, 0);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.offsetMin = new Vector2(0, 0); lrt.offsetMax = new Vector2(-24, 0);

            var fb = go.AddComponent<AnimatedFeedback>();
            fb._label = label;
            fb._bg    = bg;
            fb._faceSprite = faceSprite;
            fb._faceGlyph  = faceGlyph;
            fb._safeArea = parent;
            go.GetComponent<CanvasGroup>().alpha = 0;
            go.SetActive(false);
            return fb;
        }

        public void ShowCorrect(string msg = "Correct!", int streakCount = 0)
        {
            // Re-activate BEFORE StartCoroutine. Unity won't start coroutines
            // on inactive GameObjects, and PopFade's own SetActive(true) is
            // never reached because the coroutine never starts.
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            StopAllCoroutines();
            _bg.color = streakCount >= 3 ? new Color(0.20f, 0.85f, 0.50f) : UIFactory.Success;
            _label.text = (streakCount >= 3 ? $"x{streakCount} " : "✓ ") + msg;
            SetFace(streakCount >= 3 ? "cheer" : "happy");
            SpawnBurst(streakCount);
            StartCoroutine(PopFade(streakCount >= 3 ? 1.35f : 1.20f));
        }

        public void ShowWrong(string msg = "Try again", bool surprised = false)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            StopAllCoroutines();
            _bg.color = surprised ? new Color(0.95f, 0.55f, 0.20f) : UIFactory.Danger;
            _label.text = "✗ " + msg;
            SetFace(surprised ? "surprised" : "sad");
            SpawnWrongBurst();
            StartCoroutine(PopFade(1.10f));
        }

        private void SetFace(string moodKey)
        {
            // Try real sprite first, else emoji glyph.
            string spriteKey;
            string glyph;
            switch (moodKey)
            {
                case "cheer":     spriteKey = "smile"; glyph = "🤩"; break;
                case "happy":     spriteKey = "smile"; glyph = "😄"; break;
                case "sad":       spriteKey = "sad";   glyph = "😢"; break;
                case "surprised": spriteKey = "wow";   glyph = "😮"; break;
                default:          spriteKey = "smile"; glyph = "🙂"; break;
            }
            var sprite = IconService.Get(spriteKey);
            if (sprite != null)
            {
                _faceSprite.sprite = sprite;
                _faceSprite.enabled = true;
                _faceGlyph.enabled = false;
            }
            else
            {
                _faceSprite.enabled = false;
                _faceGlyph.text = glyph;
                _faceGlyph.enabled = true;
            }
        }

        private void SpawnBurst(int streakCount)
        {
            if (_safeArea == null) return;
            // Anchor the burst at the centre of the safe area (the pill is
            // centre-anchored too, so this lines up with the feedback's
            // visual position).
            float w = _safeArea.rect.width;
            float h = _safeArea.rect.height;
            Vector2 centre = new Vector2(w * 0.5f, h * 0.5f);
            if (streakCount >= 3) EmojiBurst.Cheer(_safeArea, centre);
            else                  EmojiBurst.Correct(_safeArea, centre);
        }

        private void SpawnWrongBurst()
        {
            if (_safeArea == null) return;
            float w = _safeArea.rect.width;
            float h = _safeArea.rect.height;
            EmojiBurst.Wrong(_safeArea, new Vector2(w * 0.5f, h * 0.5f));
        }

        private IEnumerator PopFade(float holdSeconds)
        {
            // Belt and braces — Spawn() deactivated us once, and ShowCorrect /
            // ShowWrong now re-activate before invoking us; this assignment is
            // defensive in case some other future caller forgets.
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            var cg = GetComponent<CanvasGroup>();
            var rt = (RectTransform)transform;
            cg.alpha = 1;
            rt.localScale = Vector3.one * 0.6f;
            rt.localRotation = Quaternion.identity;

            // Easing scale-in (ease-out-back)
            float t = 0;
            const float popDur = 0.28f;
            while (t < popDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / popDur);
                rt.localScale = Vector3.one * EaseOutBack(k);
                rt.localRotation = Quaternion.Euler(0, 0, (1f - k) * 4f);
                yield return null;
            }
            // Tiny wobble back to 1.0
            t = 0;
            const float settleDur = 0.10f;
            while (t < settleDur)
            {
                t += Time.unscaledDeltaTime;
                float k = t / settleDur;
                float s = Mathf.Lerp(1.08f, 1.0f, k);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            rt.localScale = Vector3.one;
            yield return new WaitForSeconds(holdSeconds);

            // Fade out
            t = 0;
            const float fadeDur = 0.30f;
            while (t < fadeDur)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = 1 - t / fadeDur;
                yield return null;
            }
            cg.alpha = 0;
            gameObject.SetActive(false);
        }

        private static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float kx = k - 1f;
            return 1f + c3 * kx * kx * kx + c1 * kx * kx;
        }
    }
}
