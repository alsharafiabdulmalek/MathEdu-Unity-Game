// -----------------------------------------------------------------------------
// MascotHost.cs
// -----------------------------------------------------------------------------
// A friendly cartoon mascot that hosts learning content. Sits at a configurable
// corner of the screen, holds a small speech bubble, and lip-syncs/bobs while
// text is "talking". Communicates the same emotion the ReactionFace does but
// with a bigger body and a speech panel — used on Learn Mode and the Main Menu.
//
// Body anatomy (all procedural, no external sprites required):
//   • Body  — a coloured rounded rect, slightly rounded "shoulders".
//   • Head  — a circle on top of the body, ~70 % body width.
//   • Face  — either an emoji glyph (default) or an IconLibrary sprite.
//   • Cheek blush — two small pink dots on the head (added in Awake).
//   • Speech bubble — a rounded rect with a tail, holds the host's message.
//
// Behaviours:
//   • Idle: gentle 0.97 → 1.03 body breathing + tiny head bob (1 cycle ≈ 1.6 s).
//   • Speak(text, duration): bobs the head a bit faster + animates the bubble
//     scale-in from 0.6 → 1.05 → 1.0 and fades out after `duration`.
//   • React(mood): switches the face emoji and pulses the body for ~0.5 s.
// -----------------------------------------------------------------------------

using System.Collections;
using MathEdu.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public class MascotHost : MonoBehaviour
    {
        public enum Mood { Happy, Idle, Cheer, Sad, Surprised, Thinking }

        private RectTransform _body;
        private RectTransform _head;
        private Image _bodyImg;
        private Image _headImg;
        private Image _faceSprite;
        private TextMeshProUGUI _faceGlyph;
        private RectTransform _bubble;
        private TextMeshProUGUI _bubbleText;
        private CanvasGroup _bubbleCg;
        private Coroutine _bubbleRoutine;
        private Coroutine _bobRoutine;
        private float _bobBase;

        // ---------------------------------------------------------------------
        // Build
        // ---------------------------------------------------------------------

        /// <summary>Spawn a mascot anchored to <paramref name="parent"/>. Returns
        /// the controller so callers can wire <see cref="Speak"/> / <see cref="React"/>.</summary>
        public static MascotHost Spawn(RectTransform parent, Vector2? anchorMin = null,
            Vector2? anchorMax = null, Color? bodyTint = null,
            string name = "MascotHost")
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = anchorMin ?? new Vector2(0, 0);
            rt.anchorMax = anchorMax ?? new Vector2(0.55f, 0.45f);
            rt.offsetMin = new Vector2(20, 20);
            rt.offsetMax = new Vector2(-20, -20);

            var mascot = root.AddComponent<MascotHost>();

            // -------- Body --------
            var body = new GameObject("Body", typeof(Image));
            body.transform.SetParent(rt, false);
            var bRt = (RectTransform)body.transform;
            bRt.anchorMin = new Vector2(0.20f, 0.05f);
            bRt.anchorMax = new Vector2(0.80f, 0.55f);
            bRt.offsetMin = Vector2.zero; bRt.offsetMax = Vector2.zero;
            var bImg = body.GetComponent<Image>();
            bImg.sprite = DefaultSprite.RoundedRect(60);
            bImg.color  = bodyTint ?? new Color(0.40f, 0.55f, 0.90f, 1f);
            bImg.type   = Image.Type.Sliced;
            bImg.raycastTarget = false;

            // -------- Head --------
            var head = new GameObject("Head", typeof(Image));
            head.transform.SetParent(rt, false);
            var hRt = (RectTransform)head.transform;
            hRt.anchorMin = new Vector2(0.20f, 0.45f);
            hRt.anchorMax = new Vector2(0.80f, 0.95f);
            hRt.offsetMin = Vector2.zero; hRt.offsetMax = Vector2.zero;
            var hImg = head.GetComponent<Image>();
            hImg.sprite = DefaultSprite.Circle();
            hImg.color  = bodyTint.HasValue
                ? Color.Lerp(bodyTint.Value, Color.white, 0.30f)
                : new Color(0.95f, 0.85f, 0.60f, 1f);
            hImg.raycastTarget = false;

            // Cheek blush
            for (int i = 0; i < 2; i++)
            {
                var cheek = new GameObject($"Cheek_{i}", typeof(Image));
                cheek.transform.SetParent(hRt, false);
                var crt = (RectTransform)cheek.transform;
                crt.anchorMin = new Vector2(i == 0 ? 0.12f : 0.66f, 0.18f);
                crt.anchorMax = new Vector2(i == 0 ? 0.34f : 0.88f, 0.34f);
                crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
                var cImg = cheek.GetComponent<Image>();
                cImg.sprite = DefaultSprite.Circle();
                cImg.color  = new Color(0.95f, 0.55f, 0.65f, 0.6f);
                cImg.raycastTarget = false;
            }

            // Face: sprite first, glyph fallback (both children, only one shown)
            var spriteGo = new GameObject("FaceSprite", typeof(Image));
            spriteGo.transform.SetParent(hRt, false);
            var srt = (RectTransform)spriteGo.transform;
            srt.anchorMin = new Vector2(0.18f, 0.30f);
            srt.anchorMax = new Vector2(0.82f, 0.88f);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            var faceImg = spriteGo.GetComponent<Image>();
            faceImg.preserveAspect = true;
            faceImg.raycastTarget = false;
            faceImg.enabled = false;

            var glyph = UIFactory.CreateText(hRt, "😊", 100, Color.white,
                TextAlignmentOptions.Center, "FaceGlyph");
            glyph.fontStyle = FontStyles.Bold;
            glyph.color = new Color(0.10f, 0.15f, 0.25f);
            var gRt = glyph.rectTransform;
            gRt.anchorMin = new Vector2(0.10f, 0.18f);
            gRt.anchorMax = new Vector2(0.90f, 0.90f);
            gRt.offsetMin = Vector2.zero; gRt.offsetMax = Vector2.zero;

            // Speech bubble (off to the right of the head)
            var bub = new GameObject("Bubble", typeof(Image), typeof(CanvasGroup));
            bub.transform.SetParent(rt, false);
            var brt = (RectTransform)bub.transform;
            brt.anchorMin = new Vector2(0.55f, 0.55f);
            brt.anchorMax = new Vector2(1.35f, 1.05f);
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            var bubImg = bub.GetComponent<Image>();
            bubImg.sprite = DefaultSprite.RoundedRect(28);
            bubImg.color  = new Color(1f, 1f, 1f, 0.95f);
            bubImg.type   = Image.Type.Sliced;
            bubImg.raycastTarget = false;
            var bubCg = bub.GetComponent<CanvasGroup>();
            bubCg.alpha = 0;

            var bubTxt = UIFactory.CreateText(brt, "", 38, new Color(0.10f, 0.15f, 0.25f),
                TextAlignmentOptions.MidlineLeft, "BubbleText");
            bubTxt.enableWordWrapping = true;
            var btRt = bubTxt.rectTransform;
            btRt.offsetMin = new Vector2(28, 18); btRt.offsetMax = new Vector2(-28, -18);

            // Wire up the controller
            mascot._body = bRt; mascot._bodyImg = bImg;
            mascot._head = hRt; mascot._headImg = hImg;
            mascot._faceSprite = faceImg;
            mascot._faceGlyph  = glyph;
            mascot._bubble = brt;
            mascot._bubbleText = bubTxt;
            mascot._bubbleCg = bubCg;
            mascot._bobBase = hRt.anchoredPosition.y;
            mascot._bobRoutine = mascot.StartCoroutine(mascot.BobLoop());
            mascot.SetMood(Mood.Idle);
            return mascot;
        }

        // ---------------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------------
        public void Speak(string text, float duration = 3.0f)
        {
            if (_bubbleRoutine != null) StopCoroutine(_bubbleRoutine);
            // Shape Arabic so the mascot's speech bubble shows connected
            // cursive words. Pass-through for English / numeric strings.
            Localization.SetText(_bubbleText, text);
            _bubbleRoutine = StartCoroutine(BubbleSequence(duration));
        }

        public void React(Mood mood)
        {
            SetMood(mood);
            StartCoroutine(BodyPulse(mood == Mood.Sad ? 0.92f : 1.18f, 0.30f));
        }

        // ---------------------------------------------------------------------
        // Animation
        // ---------------------------------------------------------------------
        private IEnumerator BubbleSequence(float showFor)
        {
            // Pop in
            float t = 0; const float popDur = 0.18f;
            while (t < popDur)
            {
                t += Time.unscaledDeltaTime;
                float k = t / popDur;
                _bubble.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.06f, k);
                _bubbleCg.alpha    = k;
                yield return null;
            }
            t = 0; const float settleDur = 0.08f;
            while (t < settleDur)
            {
                t += Time.unscaledDeltaTime;
                _bubble.localScale = Vector3.one * Mathf.Lerp(1.06f, 1.0f, t / settleDur);
                yield return null;
            }
            _bubble.localScale = Vector3.one;
            _bubbleCg.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(Mathf.Max(0.5f, showFor - 0.5f));

            // Fade
            t = 0; const float fadeDur = 0.30f;
            while (t < fadeDur)
            {
                t += Time.unscaledDeltaTime;
                _bubbleCg.alpha = 1f - t / fadeDur;
                yield return null;
            }
            _bubbleCg.alpha = 0;
            _bubbleRoutine = null;
        }

        private IEnumerator BodyPulse(float peak, float dur)
        {
            float t = 0;
            Vector3 start = Vector3.one;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float s = k < 0.5f
                    ? Mathf.Lerp(1f, peak, k / 0.5f)
                    : Mathf.Lerp(peak, 1f, (k - 0.5f) / 0.5f);
                _body.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _body.localScale = start;
        }

        private IEnumerator BobLoop()
        {
            float t = 0;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Sin(t * 2.4f);
                _head.anchoredPosition = new Vector2(0, _bobBase + k * 6f);
                _body.localRotation = Quaternion.Euler(0, 0, k * 1.5f);
                yield return null;
            }
        }

        // ---------------------------------------------------------------------
        // State
        // ---------------------------------------------------------------------
        private void SetMood(Mood mood)
        {
            string spriteKey;
            string glyph;
            switch (mood)
            {
                case Mood.Happy:     spriteKey = "smile"; glyph = "😄"; break;
                case Mood.Cheer:     spriteKey = "smile"; glyph = "🤩"; break;
                case Mood.Sad:       spriteKey = "sad";   glyph = "😟"; break;
                case Mood.Surprised: spriteKey = "wow";   glyph = "😮"; break;
                case Mood.Thinking:  spriteKey = "cool";  glyph = "🤔"; break;
                default:             spriteKey = "smile"; glyph = "🙂"; break;
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
                _faceGlyph.enabled = true;
                _faceGlyph.text = glyph;
            }
        }
    }
}
