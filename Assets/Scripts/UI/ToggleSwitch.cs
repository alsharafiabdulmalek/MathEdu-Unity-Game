// -----------------------------------------------------------------------------
// ToggleSwitch.cs
// -----------------------------------------------------------------------------
// Mobile-friendly, sprite-aware toggle. Renders as the UITheme's on/off
// sprites when available (e.g. the bundled "on Toggle.png"/"off Toggle.png"
// from the Layer Lab UI Assets pack); falls back to a tinted pill when not.
//
// Used by the Settings screen for Music / SFX / Haptics toggles. Emits the
// new value through an onValueChanged event.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
    {
        public event Action<bool> onValueChanged;

        private bool _value;
        private Image _bgImage;
        private RectTransform _knob;
        private Image _knobImage;
        private bool _useSpriteMode;

        public bool Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                Render();
                onValueChanged?.Invoke(_value);
            }
        }

        public static ToggleSwitch Spawn(RectTransform parent, bool initial, string name = "Toggle")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ToggleSwitch));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(180, 90);

            var bg = go.GetComponent<Image>();
            var ts = go.GetComponent<ToggleSwitch>();

            var onSp  = UIThemeService.ToggleOn();
            var offSp = UIThemeService.ToggleOff();
            ts._useSpriteMode = (onSp != null && offSp != null);

            if (ts._useSpriteMode)
            {
                bg.sprite = initial ? onSp : offSp;
                bg.type   = Image.Type.Simple;
                bg.preserveAspect = true;
                bg.color  = Color.white;
            }
            else
            {
                bg.sprite = DefaultSprite.RoundedRect(40);
                bg.type   = Image.Type.Sliced;
                bg.color  = initial ? UIFactory.Success : new Color(0.45f, 0.45f, 0.5f);

                // Knob
                var knob = new GameObject("Knob", typeof(Image));
                knob.transform.SetParent(rt, false);
                var krt = (RectTransform)knob.transform;
                krt.sizeDelta = new Vector2(70, 70);
                krt.anchorMin = krt.anchorMax = new Vector2(initial ? 1 : 0, 0.5f);
                krt.pivot = new Vector2(initial ? 1 : 0, 0.5f);
                krt.anchoredPosition = new Vector2(initial ? -8 : 8, 0);
                var ki = knob.GetComponent<Image>();
                ki.sprite = DefaultSprite.Circle();
                ki.color  = Color.white;

                ts._knob      = krt;
                ts._knobImage = ki;
            }

            ts._bgImage = bg;
            ts._value = initial;
            return ts;
        }

        public void OnPointerClick(PointerEventData eventData) => Value = !_value;

        private void Render()
        {
            if (_useSpriteMode)
            {
                _bgImage.sprite = _value
                    ? UIThemeService.ToggleOn()
                    : UIThemeService.ToggleOff();
                return;
            }

            _bgImage.color = _value ? UIFactory.Success : new Color(0.45f, 0.45f, 0.5f);
            if (_knob != null)
            {
                _knob.anchorMin = _knob.anchorMax = new Vector2(_value ? 1 : 0, 0.5f);
                _knob.pivot = new Vector2(_value ? 1 : 0, 0.5f);
                _knob.anchoredPosition = new Vector2(_value ? -8 : 8, 0);
            }
        }
    }
}
