// -----------------------------------------------------------------------------
// Timer.cs
// -----------------------------------------------------------------------------
// Countdown timer widget. Combines a TMP label and a horizontal fill bar.
// Used by Quiz Mode and Speed Round.
//
// Visual treatment per spec:
//   • Green when >50% time remaining.
//   • Yellow/Accent between 20% and 50%.
//   • Red below 20% — also pulses the fill alpha (1 → 0.5 → 1 every 0.3s).
//   • "tick" SFX once per second when remaining time is below 5 seconds.
//   • Fires OnExpired when the remaining time reaches 0.
// -----------------------------------------------------------------------------

using System;
using System.Collections;
using MathEdu.Managers;
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
        private float _lastTickAt;
        private Coroutine _pulseRoutine;
        private float _pulseAlpha = 1f;

        public event Action OnExpired;

        public static Timer Spawn(RectTransform parent)
        {
            var go = new GameObject("Timer", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(0, 80);

            var label = UIFactory.CreateText(rt, "00:00", 44,
                UIFactory.TextLight, TMPro.TextAlignmentOptions.Center, "TimeLabel");
            label.fontStyle = FontStyles.Bold;
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0, 0.45f);
            lrt.anchorMax = new Vector2(1, 1);

            var barHolder = new GameObject("BarHolder", typeof(RectTransform));
            barHolder.transform.SetParent(rt, false);
            var brt = (RectTransform)barHolder.transform;
            brt.anchorMin = new Vector2(0.05f, 0);
            brt.anchorMax = new Vector2(0.95f, 0.40f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var bar = ProgressBar.Spawn(brt, 24);

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
            _lastTickAt = -1f;
            _pulseAlpha = 1f;
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
            _pulseRoutine = StartCoroutine(PulseLoop());
            UpdateView();
        }

        public void Pause()  { _running = false; }
        public void Resume() { _running = true; }
        public void Stop()   { _running = false; _remaining = 0; UpdateView(); }

        public float Remaining => _remaining;

        private void Update()
        {
            if (!_running) return;
            _remaining -= Time.unscaledDeltaTime;

            // "timerTick" SFX every full second when below 5s remaining.
            if (_remaining < 5f && _remaining > 0f)
            {
                int currentSecond = Mathf.CeilToInt(_remaining);
                if (Mathf.Abs(currentSecond - _lastTickAt) > 0.01f)
                {
                    _lastTickAt = currentSecond;
                    var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
                    if (audio != null) audio.PlaySFX("timerTick");
                }
            }

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
            if (_bar == null) return;

            float frac = _duration > 0 ? Mathf.Clamp01(_remaining / _duration) : 0;
            _bar.SetValue(frac);

            Color fill;
            if      (frac < 0.20f) fill = new Color(1.00f, 0.20f, 0.20f, _pulseAlpha);
            else if (frac < 0.50f) fill = new Color(1.00f, 0.85f, 0.20f, 1f);
            else                   fill = new Color(0.30f, 0.80f, 0.40f, 1f);
            _bar.SetFillColor(fill);
        }

        /// <summary>
        /// Below 0.2 fill the bar pulses between alpha 1 → 0.5 → 1 every 0.3s
        /// to add urgency. Above 0.2 the alpha is held at 1 and the coroutine
        /// idles cheaply.
        /// </summary>
        private IEnumerator PulseLoop()
        {
            while (_running)
            {
                float frac = _duration > 0 ? Mathf.Clamp01(_remaining / _duration) : 0;
                if (frac < 0.20f)
                {
                    // Smooth pulse 1 → 0.5 → 1 across 0.3 seconds.
                    float t = 0;
                    const float period = 0.3f;
                    while (t < period && _running)
                    {
                        t += Time.unscaledDeltaTime;
                        float k = Mathf.PingPong(t / period, 0.5f);
                        _pulseAlpha = 1f - k;
                        UpdateView();
                        yield return null;
                    }
                }
                else
                {
                    _pulseAlpha = 1f;
                    yield return null;
                }
            }
            _pulseAlpha = 1f;
        }
    }
}
