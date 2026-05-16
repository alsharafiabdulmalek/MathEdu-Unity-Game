// -----------------------------------------------------------------------------
// Timer.cs
// -----------------------------------------------------------------------------
// Countdown timer widget. Combines a TMP label and a horizontal fill bar.
// Used by Quiz Mode and Speed Round.
// -----------------------------------------------------------------------------

using System;
using TMPro;
using UnityEngine;

namespace MathEdu.UI
{
    public class Timer : MonoBehaviour
    {
        private TextMeshProUGUI _label;
        private ProgressBar _bar;
        private float _duration;
        private float _remaining;
        private bool  _running;

        public event Action OnExpired;

        public static Timer Spawn(RectTransform parent)
        {
            var go = new GameObject("Timer", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(0, 80);

            var label = UIFactory.CreateText(rt, "00:00", 48,
                UIFactory.TextLight, TMPro.TextAlignmentOptions.Center, "TimeLabel");
            label.fontStyle = FontStyles.Bold;

            var barHolder = new GameObject("BarHolder", typeof(RectTransform));
            barHolder.transform.SetParent(rt, false);
            var brt = (RectTransform)barHolder.transform;
            brt.anchorMin = new Vector2(0.1f, 0);
            brt.anchorMax = new Vector2(0.9f, 0.3f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var bar = ProgressBar.Spawn(brt, 16);

            var t = go.AddComponent<Timer>();
            t._label = label;
            t._bar   = bar;
            return t;
        }

        public void Begin(float seconds)
        {
            _duration  = Mathf.Max(0.01f, seconds);
            _remaining = _duration;
            _running   = true;
            UpdateView();
        }

        public void Pause()  => _running = false;
        public void Resume() => _running = true;
        public void Stop()   { _running = false; _remaining = 0; UpdateView(); }

        public float Remaining => _remaining;

        private void Update()
        {
            if (!_running) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0)
            {
                _remaining = 0;
                _running = false;
                UpdateView();
                OnExpired?.Invoke();
                return;
            }
            UpdateView();
        }

        private void UpdateView()
        {
            int s = Mathf.CeilToInt(_remaining);
            int m = s / 60;
            int sec = s % 60;
            if (_label != null) _label.text = $"{m:00}:{sec:00}";
            if (_bar   != null) _bar.SetValue(_remaining / _duration);
            if (_bar != null)
            {
                if      (_remaining / _duration < 0.2f) _bar.SetFillColor(UIFactory.Danger);
                else if (_remaining / _duration < 0.5f) _bar.SetFillColor(UIFactory.Accent);
                else                                    _bar.SetFillColor(UIFactory.Success);
            }
        }
    }
}
