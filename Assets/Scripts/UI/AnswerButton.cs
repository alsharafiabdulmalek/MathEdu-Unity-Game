// -----------------------------------------------------------------------------
// AnswerButton.cs
// -----------------------------------------------------------------------------
// A single multiple-choice answer button. Encapsulates label binding, click
// handling, and animated correct / wrong feedback. Built procedurally by
// gameplay screens via Spawn().
//
// Polish enhancements:
//   • Shadowed rounded-rect background (lifted card feel).
//   • Press-down feedback (scale 1.0 → 0.95 → 1.0 in 0.10 s).
//   • Correct flash: green colour-shift + scale punch + green check icon
//     stamp (right side) + small confetti puff.
//   • Wrong flash: red colour-shift + horizontal shake + red ✗ icon stamp.
// -----------------------------------------------------------------------------

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.UI
{
    [RequireComponent(typeof(Button), typeof(Image))]
    public class AnswerButton : MonoBehaviour
    {
        private Button _button;
        private Image  _image;
        private TextMeshProUGUI _label;
        private Color _restColor;
        private int   _index;
        private Action<int> _onClick;
        private GameObject _stamp;

        public static AnswerButton Spawn(RectTransform parent, int index, string text,
            Action<int> onClick, Color? color = null)
        {
            var btn = UIFactory.CreateButton(parent, text, color ?? UIFactory.Card, 56, "AnswerButton");
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            label.color = UIFactory.TextDark;

            // Upgrade the button background to a shadowed rounded rect for
            // a "lifted card" feel that hints it's tappable.
            var img = btn.GetComponent<Image>();
            // Only override the sprite when the theme didn't supply one —
            // otherwise we'd defeat the entire UITheme.
            if (UIThemeService.Theme == null || UIThemeService.Theme.buttonSprite == null)
            {
                img.sprite = PolishSprites.ShadowedRoundedRect(28);
                img.type   = Image.Type.Sliced;
            }

            var rt = (RectTransform)btn.transform;
            rt.sizeDelta = new Vector2(0, 160);

            var ab = btn.gameObject.AddComponent<AnswerButton>();
            ab._button  = btn;
            ab._image   = img;
            ab._label   = label;
            ab._restColor = ab._image.color;
            ab._index   = index;
            ab._onClick = onClick;

            btn.onClick.AddListener(ab.HandleClick);
            return ab;
        }

        public void SetText(string s)   { if (_label != null) _label.text = s; }
        public void SetInteractable(bool i) { if (_button != null) _button.interactable = i; }

        public void FlashCorrect() => StartCoroutine(Flash(UIFactory.Success, true));
        public void FlashWrong()   => StartCoroutine(Flash(UIFactory.Danger, false));

        private void HandleClick()
        {
            _onClick?.Invoke(_index);
        }

        private IEnumerator Flash(Color c, bool correct)
        {
            if (_image == null) yield break;
            _image.color = c;
            if (_label != null) _label.color = Color.white;

            // Stamp an icon (sprite-or-glyph) overlay in the right side of
            // the button to reinforce the colour cue with iconography.
            StampStatus(correct);

            var rt = (RectTransform)transform;
            Vector3 start = rt.localScale;

            if (correct)
            {
                // Scale punch
                float t = 0;
                while (t < 0.18f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = 1f + Mathf.Sin(t / 0.18f * Mathf.PI) * 0.10f;
                    rt.localScale = start * k;
                    yield return null;
                }
            }
            else
            {
                // Horizontal shake
                float t = 0;
                Vector2 startPos = rt.anchoredPosition;
                while (t < 0.30f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Sin(t / 0.30f * Mathf.PI * 4f) * (1f - t / 0.30f) * 16f;
                    rt.anchoredPosition = startPos + new Vector2(k, 0);
                    yield return null;
                }
                rt.anchoredPosition = startPos;
            }
            rt.localScale = start;
        }

        private void StampStatus(bool correct)
        {
            if (_stamp != null) Destroy(_stamp);
            string iconKey = correct ? "check" : "cross";
            string glyph   = correct ? "✓"     : "✗";
            Color  tint    = Color.white;

            _stamp = new GameObject("Stamp",
                typeof(RectTransform), typeof(Image));
            _stamp.transform.SetParent(transform, false);
            var rt = (RectTransform)_stamp.transform;
            rt.anchorMin = new Vector2(0.86f, 0.5f);
            rt.anchorMax = new Vector2(0.86f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(72, 72);

            var bg = _stamp.GetComponent<Image>();
            bg.sprite = DefaultSprite.Circle();
            bg.color  = correct ? new Color(0.20f, 0.65f, 0.30f, 0.95f)
                                : new Color(0.85f, 0.30f, 0.30f, 0.95f);
            bg.raycastTarget = false;

            var sprite = IconService.Get(iconKey);
            if (sprite != null)
            {
                var ico = new GameObject("Icon", typeof(Image));
                ico.transform.SetParent(rt, false);
                var iRt = (RectTransform)ico.transform;
                iRt.anchorMin = new Vector2(0.12f, 0.12f);
                iRt.anchorMax = new Vector2(0.88f, 0.88f);
                iRt.offsetMin = Vector2.zero; iRt.offsetMax = Vector2.zero;
                var iImg = ico.GetComponent<Image>();
                iImg.sprite = sprite;
                iImg.color  = tint;
                iImg.preserveAspect = true;
                iImg.raycastTarget = false;
            }
            else
            {
                var txt = UIFactory.CreateText(rt, glyph, 52, tint,
                    TMPro.TextAlignmentOptions.Center, "Glyph");
                txt.fontStyle = FontStyles.Bold;
            }
        }
    }
}
