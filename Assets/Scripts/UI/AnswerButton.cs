// -----------------------------------------------------------------------------
// AnswerButton.cs
// -----------------------------------------------------------------------------
// A single multiple-choice answer button. Encapsulates label binding, click
// handling, and animated correct / wrong feedback. Built procedurally by
// gameplay screens via Spawn().
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

        public static AnswerButton Spawn(RectTransform parent, int index, string text,
            Action<int> onClick, Color? color = null)
        {
            var btn = UIFactory.CreateButton(parent, text, color ?? UIFactory.Card, 56, "AnswerButton");
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            label.color = UIFactory.TextDark;

            var rt = (RectTransform)btn.transform;
            rt.sizeDelta = new Vector2(0, 160);

            var ab = btn.gameObject.AddComponent<AnswerButton>();
            ab._button  = btn;
            ab._image   = btn.GetComponent<Image>();
            ab._label   = label;
            ab._restColor = ab._image.color;
            ab._index   = index;
            ab._onClick = onClick;

            btn.onClick.AddListener(ab.HandleClick);
            return ab;
        }

        public void SetText(string s)   { if (_label != null) _label.text = s; }
        public void SetInteractable(bool i) { if (_button != null) _button.interactable = i; }

        public void FlashCorrect() => StartCoroutine(Flash(UIFactory.Success));
        public void FlashWrong()   => StartCoroutine(Flash(UIFactory.Danger));

        private void HandleClick()
        {
            _onClick?.Invoke(_index);
        }

        private IEnumerator Flash(Color c)
        {
            if (_image == null) yield break;
            _image.color = c;
            if (_label != null) _label.color = Color.white;

            var rt = (RectTransform)transform;
            Vector3 start = rt.localScale;
            float t = 0;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f + Mathf.Sin(t / 0.18f * Mathf.PI) * 0.07f;
                rt.localScale = start * k;
                yield return null;
            }
            rt.localScale = start;
        }
    }
}
