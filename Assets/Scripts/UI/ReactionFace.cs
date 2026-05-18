// -----------------------------------------------------------------------------
// ReactionFace.cs
// -----------------------------------------------------------------------------
// A small, animated face widget that sits in the gameplay HUD and reacts to
// answer events. Communicates win/loss state instantly without stealing
// real-estate from the question card.
//
// Behaviours:
//   • Idle: gentle 4% breathing scale (1.0 → 1.04 → 1.0 over ~2 s).
//   • Happy: scale-punch 1.0 → 1.25 → 1.0 + +12° wobble; tints background
//     green; switches to "happy" face. Stays expressed for ~0.9 s then
//     decays back to idle.
//   • Cheer: same as Happy plus a small confetti puff (calls EmojiBurst.Cheer).
//     Triggered when the player hits a 3+ correct streak.
//   • Sad: scale dip 1.0 → 0.85 → 1.0; tints background red; switches to
//     "sad" face. Slight downward bob.
//   • Surprised: scale punch + rotation wobble + "wow" face. Used for
//     timer expire / first wrong answer.
//
// Visual primitives:
//   • Background: rounded coloured disc (sized 220x220 by default).
//   • Foreground: either a sprite from IconLibrary (Pictoicon_Emoji_*) OR
//     a TMP emoji glyph fallback. The sprite is sized to 70 % of the disc.
//
// Spawn places the widget at the top-right of the supplied parent by default
// so it lives next to the question card. Customise with the returned
// RectTransform if needed.
// -----------------------------------------------------------------------------

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public class ReactionFace : MonoBehaviour
    {
        public enum Mood { Idle, Happy, Cheer, Sad, Surprised }

        private Image _bg;
        private Image _spriteIco;
        private TextMeshProUGUI _glyph;
        private RectTransform _rt;
        private Coroutine _breath;
        private Coroutine _expression;
        private Color _idleTint = new Color(0.95f, 0.82f, 0.30f, 1f);

        // ---------------------------------------------------------------------
        // Build
        // ---------------------------------------------------------------------
        public static ReactionFace Spawn(RectTransform parent, Vector2? anchorMin = null,
            Vector2? anchorMax = null, float size = 220f, string name = "ReactionFace")
        {
            var go = new GameObject(name,
                typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin ?? new Vector2(0.78f, 0.62f);
            rt.anchorMax = anchorMax ?? new Vector2(0.78f, 0.62f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);

            var bg = go.GetComponent<Image>();
            bg.sprite = DefaultSprite.Circle();
            bg.color  = new Color(0.95f, 0.82f, 0.30f, 1f);
            bg.raycastTarget = false;

            // A subtle outer ring for legibility against any background.
            var ring = new GameObject("Ring", typeof(Image));
            ring.transform.SetParent(rt, false);
            var ringRt = (RectTransform)ring.transform;
            ringRt.anchorMin = Vector2.zero; ringRt.anchorMax = Vector2.one;
            ringRt.offsetMin = new Vector2(-10, -10);
            ringRt.offsetMax = new Vector2(10, 10);
            var ringImg = ring.GetComponent<Image>();
            ringImg.sprite = DefaultSprite.Circle();
            ringImg.color  = new Color(1f, 1f, 1f, 0.18f);
            ringImg.raycastTarget = false;

            // Foreground: sprite OR glyph (one or the other shows).
            var icoGo = new GameObject("Sprite", typeof(Image));
            icoGo.transform.SetParent(rt, false);
            var icoRt = (RectTransform)icoGo.transform;
            icoRt.anchorMin = Vector2.zero; icoRt.anchorMax = Vector2.one;
            icoRt.offsetMin = new Vector2(size * 0.12f, size * 0.12f);
            icoRt.offsetMax = new Vector2(-size * 0.12f, -size * 0.12f);
            var ico = icoGo.GetComponent<Image>();
            ico.preserveAspect = true;
            ico.raycastTarget = false;
            ico.enabled = false;
            ico.sprite = IconService.Get("smile");

            var glyph = UIFactory.CreateText(rt, "😊", Mathf.RoundToInt(size * 0.55f),
                Color.white, TextAlignmentOptions.Center, "Glyph");
            glyph.fontStyle = FontStyles.Bold;
            glyph.raycastTarget = false;

            var rf = go.AddComponent<ReactionFace>();
            rf._bg = bg;
            rf._spriteIco = ico;
            rf._glyph = glyph;
            rf._rt = rt;
            rf.SetMood(Mood.Idle, immediate: true);
            rf._breath = rf.StartCoroutine(rf.BreathLoop());
            return rf;
        }

        // ---------------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------------
        public void Happy(string text = null)        => Trigger(Mood.Happy,     text);
        public void Cheer(string text = null)        => Trigger(Mood.Cheer,     text);
        public void Sad(string text = null)          => Trigger(Mood.Sad,       text);
        public void Surprised(string text = null)    => Trigger(Mood.Surprised, text);
        public void Reset()                          => SetMood(Mood.Idle, immediate: true);

        private void Trigger(Mood mood, string _)
        {
            if (_expression != null) StopCoroutine(_expression);
            _expression = StartCoroutine(PlayExpression(mood));
        }

        // ---------------------------------------------------------------------
        // Animation
        // ---------------------------------------------------------------------
        private IEnumerator PlayExpression(Mood mood)
        {
            SetMood(mood, immediate: false);

            float t = 0f;
            float dur = 0.22f;
            // Punch / dip scale curve
            Vector3 startScale = _rt.localScale;
            Vector3 peakScale =
                mood == Mood.Sad ? Vector3.one * 0.86f :
                mood == Mood.Cheer ? Vector3.one * 1.32f :
                mood == Mood.Surprised ? Vector3.one * 1.20f :
                Vector3.one * 1.22f;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                _rt.localScale = Vector3.Lerp(startScale, peakScale, EaseOutBack(k));
                _rt.localRotation = Quaternion.Euler(0, 0, WobbleAngle(mood, k));
                yield return null;
            }

            t = 0f; dur = 0.18f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                _rt.localScale = Vector3.Lerp(peakScale, Vector3.one, k);
                _rt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(WobbleAngle(mood, 1f), 0f, k));
                yield return null;
            }
            _rt.localScale = Vector3.one;
            _rt.localRotation = Quaternion.identity;

            // Linger in the expression for a moment before returning to idle.
            yield return new WaitForSeconds(mood == Mood.Cheer ? 1.0f : 0.7f);
            SetMood(Mood.Idle, immediate: false);
            _expression = null;
        }

        private static float WobbleAngle(Mood mood, float k)
        {
            return mood switch
            {
                Mood.Happy     => Mathf.Sin(k * Mathf.PI * 2f) * 8f,
                Mood.Cheer     => Mathf.Sin(k * Mathf.PI * 3f) * 12f,
                Mood.Surprised => Mathf.Sin(k * Mathf.PI * 2f) * 6f,
                Mood.Sad       => -Mathf.Sin(k * Mathf.PI) * 4f,
                _              => 0f
            };
        }

        private static float EaseOutBack(float k)
        {
            // c1 = 1.70158; c3 = c1 + 1
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float kx = k - 1f;
            return 1f + c3 * kx * kx * kx + c1 * kx * kx;
        }

        private IEnumerator BreathLoop()
        {
            float t = 0;
            while (true)
            {
                if (_expression == null)
                {
                    t += Time.unscaledDeltaTime;
                    float k = (Mathf.Sin(t * 2f) + 1f) * 0.5f;
                    float scale = Mathf.Lerp(0.98f, 1.04f, k);
                    _rt.localScale = new Vector3(scale, scale, 1f);
                }
                yield return null;
            }
        }

        // ---------------------------------------------------------------------
        // State
        // ---------------------------------------------------------------------
        private void SetMood(Mood mood, bool immediate)
        {
            // Tint
            Color target = mood switch
            {
                Mood.Happy     => new Color(0.30f, 0.80f, 0.45f, 1f),
                Mood.Cheer     => new Color(0.30f, 0.85f, 0.55f, 1f),
                Mood.Sad       => new Color(0.95f, 0.45f, 0.45f, 1f),
                Mood.Surprised => new Color(0.95f, 0.65f, 0.20f, 1f),
                _              => _idleTint
            };
            if (immediate) _bg.color = target;
            else StartCoroutine(LerpColor(_bg, target, 0.18f));

            // Sprite / glyph swap
            string spriteKey;
            string glyph;
            switch (mood)
            {
                case Mood.Happy:     spriteKey = "smile"; glyph = "😄"; break;
                case Mood.Cheer:     spriteKey = "smile"; glyph = "🤩"; break;
                case Mood.Sad:       spriteKey = "sad";   glyph = "😢"; break;
                case Mood.Surprised: spriteKey = "wow";   glyph = "😮"; break;
                default:             spriteKey = "smile"; glyph = "🙂"; break;
            }
            var sprite = IconService.Get(spriteKey);
            if (sprite != null)
            {
                _spriteIco.sprite  = sprite;
                _spriteIco.enabled = true;
                _glyph.enabled     = false;
            }
            else
            {
                _spriteIco.enabled = false;
                _glyph.text        = glyph;
                _glyph.enabled     = true;
            }
        }

        private static IEnumerator LerpColor(Image img, Color to, float dur)
        {
            Color from = img.color;
            float t = 0;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                img.color = Color.Lerp(from, to, t / dur);
                yield return null;
            }
            img.color = to;
        }
    }
}
