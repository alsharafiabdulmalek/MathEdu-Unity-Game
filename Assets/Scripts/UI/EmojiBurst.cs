// -----------------------------------------------------------------------------
// EmojiBurst.cs
// -----------------------------------------------------------------------------
// Spawns a short burst of floating "particles" (emoji glyphs or sprites) that
// fly outward from a screen position, peak, and fade. Used to add a "juicy"
// micro-reward to:
//   • Correct answer (small ⭐ + ✨ puff)
//   • Streak milestones (🎉 + 🔥 confetti at 3/5/10 in a row)
//   • Level complete (🎊 🥳 ⭐ shower)
//   • Wrong answer (subtle dim 💥 + sad 😟 — sparingly!)
//
// Particles are TMP text or Image children of a temporary CanvasGroup parent
// that lives directly under the supplied parent transform (typically the
// gameplay safe-area). Each particle interpolates position, scale, and alpha
// over the burst lifetime, then the parent destroys itself.
//
// All numeric tuning is in the static config arrays at the top of the class
// so you can adjust intensity per event without touching the math.
// -----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public class EmojiBurst : MonoBehaviour
    {
        // Each preset lists the candidate glyphs and the corresponding
        // IconLibrary keys (sprite-first, glyph fallback). Equally weighted.
        private static readonly (string glyph, string iconKey, Color color)[] CorrectPool =
        {
            ("⭐", "star",   new Color(1.00f, 0.85f, 0.20f)),
            ("✨", "sparkle",new Color(1.00f, 0.95f, 0.55f)),
            ("👍", null,     new Color(0.40f, 0.85f, 0.55f)),
        };

        private static readonly (string glyph, string iconKey, Color color)[] CheerPool =
        {
            ("🎉", null,     new Color(1.00f, 0.55f, 0.20f)),
            ("🎊", null,     new Color(1.00f, 0.30f, 0.55f)),
            ("⭐", "star",   new Color(1.00f, 0.85f, 0.20f)),
            ("🔥", "fire",   new Color(1.00f, 0.45f, 0.20f)),
            ("✨", "sparkle",new Color(1.00f, 0.95f, 0.55f)),
        };

        private static readonly (string glyph, string iconKey, Color color)[] WinPool =
        {
            ("🎉", null,     new Color(1.00f, 0.55f, 0.20f)),
            ("🥳", null,     new Color(1.00f, 0.70f, 0.30f)),
            ("⭐", "star",   new Color(1.00f, 0.85f, 0.20f)),
            ("🏆", "trophy", new Color(1.00f, 0.78f, 0.25f)),
            ("✨", "sparkle",new Color(1.00f, 0.95f, 0.55f)),
            ("🎊", null,     new Color(0.95f, 0.40f, 0.65f)),
            ("👑", "crown",  new Color(1.00f, 0.80f, 0.10f)),
        };

        private static readonly (string glyph, string iconKey, Color color)[] WrongPool =
        {
            ("💧", null,     new Color(0.40f, 0.65f, 0.95f, 0.85f)),
            ("😟", "sad",    new Color(0.85f, 0.50f, 0.50f, 0.85f)),
        };

        private static readonly (string glyph, string iconKey, Color color)[] BadgePool =
        {
            ("🏅", "medal",  new Color(1.00f, 0.78f, 0.25f)),
            ("🎖", null,     new Color(0.95f, 0.55f, 0.20f)),
            ("✨", "sparkle",new Color(1.00f, 0.95f, 0.55f)),
            ("⭐", "star",   new Color(1.00f, 0.85f, 0.20f)),
        };

        // ---------------------------------------------------------------------
        // Public entry points
        // ---------------------------------------------------------------------

        /// <summary>Small "correct answer" puff. Count = 6.</summary>
        public static void Correct(RectTransform parent, Vector2 anchoredPos)
            => Burst(parent, anchoredPos, CorrectPool, count: 6, life: 0.9f, spread: 220);

        /// <summary>Bigger "streak/cheer" celebration. Count = 14.</summary>
        public static void Cheer(RectTransform parent, Vector2 anchoredPos)
            => Burst(parent, anchoredPos, CheerPool, count: 14, life: 1.2f, spread: 360);

        /// <summary>Full-screen "level complete" shower. Count = 32.</summary>
        public static void Win(RectTransform parent)
        {
            // Drop particles from the top of the screen — feels like confetti rain.
            var inst = Make(parent, "EmojiBurstWin");
            var rt = (RectTransform)inst.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var burst = inst.AddComponent<EmojiBurst>();
            burst.StartCoroutine(burst.RainCoroutine(WinPool, 32, 2.4f));
        }

        /// <summary>Quiet "wrong answer" hint. Count = 3.</summary>
        public static void Wrong(RectTransform parent, Vector2 anchoredPos)
            => Burst(parent, anchoredPos, WrongPool, count: 3, life: 0.7f, spread: 160, gravity: -260f);

        /// <summary>"Badge unlocked" sprinkle. Count = 18.</summary>
        public static void Badge(RectTransform parent, Vector2 anchoredPos)
            => Burst(parent, anchoredPos, BadgePool, count: 18, life: 1.5f, spread: 340);

        // ---------------------------------------------------------------------
        // Core builder
        // ---------------------------------------------------------------------
        private static void Burst(RectTransform parent, Vector2 anchored,
            (string glyph, string iconKey, Color color)[] pool, int count, float life,
            float spread, float gravity = -420f)
        {
            var inst = Make(parent, "EmojiBurst");
            var rt = (RectTransform)inst.transform;
            // Anchor to centre-bottom-left of parent so anchoredPos works the
            // intuitive way (pixels from bottom-left of parent rect).
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(spread * 2, spread * 2);

            var burst = inst.AddComponent<EmojiBurst>();
            burst.StartCoroutine(burst.BurstCoroutine(pool, count, life, spread, gravity));
        }

        private static GameObject Make(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            go.GetComponent<CanvasGroup>().blocksRaycasts = false;
            return go;
        }

        // ---------------------------------------------------------------------
        // Coroutines
        // ---------------------------------------------------------------------
        private IEnumerator BurstCoroutine(
            (string glyph, string iconKey, Color color)[] pool, int count, float life,
            float spread, float gravity)
        {
            var parts = new List<Particle>();
            for (int i = 0; i < count; i++)
            {
                var p = pool[Random.Range(0, pool.Length)];
                parts.Add(SpawnParticle(p.glyph, p.iconKey, p.color,
                    AngleVelocity(spread), Random.Range(0.6f, 1.1f)));
            }

            float t = 0;
            while (t < life)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / life);
                foreach (var part in parts)
                {
                    part.velocity += new Vector2(0, gravity * Time.unscaledDeltaTime);
                    part.rt.anchoredPosition += part.velocity * Time.unscaledDeltaTime;
                    part.rt.localScale = Vector3.one * Mathf.Lerp(part.startScale, part.startScale * 0.55f, k);
                    part.rt.localRotation = Quaternion.Euler(0, 0, part.spin * Mathf.Lerp(0, 360f, k));
                    if (part.cg != null) part.cg.alpha = 1f - k * k; // ease-in fade
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        private IEnumerator RainCoroutine(
            (string glyph, string iconKey, Color color)[] pool, int count, float life)
        {
            // Spawn over the first 60% of `life`, drift to the bottom, fade.
            float spawnUntil = life * 0.6f;
            int spawned = 0;
            float t = 0;
            var parts = new List<Particle>();
            var rt = (RectTransform)transform;
            float w = rt.rect.width;
            float h = rt.rect.height;

            while (t < life)
            {
                t += Time.unscaledDeltaTime;
                // Throttle spawn
                int target = Mathf.RoundToInt(Mathf.Min(t / spawnUntil, 1f) * count);
                while (spawned < target)
                {
                    var p = pool[Random.Range(0, pool.Length)];
                    var go = MakeParticle(p.glyph, p.iconKey, p.color, Random.Range(0.9f, 1.4f));
                    var prt = (RectTransform)go.transform;
                    prt.anchorMin = new Vector2(0, 1);
                    prt.anchorMax = new Vector2(0, 1);
                    prt.pivot     = new Vector2(0.5f, 0.5f);
                    prt.anchoredPosition = new Vector2(Random.Range(0, w), Random.Range(0, h * 0.20f));
                    var part = new Particle
                    {
                        rt = prt,
                        cg = go.GetComponent<CanvasGroup>(),
                        velocity = new Vector2(Random.Range(-50f, 50f), Random.Range(-360f, -240f)),
                        spin = Random.Range(-1.5f, 1.5f),
                        startScale = Random.Range(0.9f, 1.4f)
                    };
                    parts.Add(part);
                    spawned++;
                }

                foreach (var part in parts)
                {
                    part.velocity += new Vector2(0, -300f * Time.unscaledDeltaTime);
                    part.rt.anchoredPosition += part.velocity * Time.unscaledDeltaTime;
                    part.rt.localRotation =
                        Quaternion.Euler(0, 0, part.spin * (Time.unscaledTime * 60f));
                    if (part.cg != null && t > spawnUntil)
                    {
                        float fade = 1f - Mathf.Clamp01((t - spawnUntil) / (life - spawnUntil));
                        part.cg.alpha = fade;
                    }
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        // ---------------------------------------------------------------------
        // Particle helpers
        // ---------------------------------------------------------------------
        private class Particle
        {
            public RectTransform rt;
            public CanvasGroup   cg;
            public Vector2       velocity;
            public float         spin;
            public float         startScale;
        }

        private static Vector2 AngleVelocity(float spread)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            // Pull slightly toward "up" so the burst feels celebratory.
            float vy = Mathf.Sin(angle) * Random.Range(spread * 0.7f, spread) + 220f;
            float vx = Mathf.Cos(angle) * Random.Range(spread * 0.6f, spread);
            return new Vector2(vx, vy);
        }

        private Particle SpawnParticle(string glyph, string iconKey, Color color,
            Vector2 velocity, float startScale)
        {
            // MakeParticle already parents to `transform`. We just re-anchor
            // the particle to the centre of the burst origin.
            var go = MakeParticle(glyph, iconKey, color, startScale);
            var prt = (RectTransform)go.transform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot     = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            return new Particle
            {
                rt = prt,
                cg = go.GetComponent<CanvasGroup>(),
                velocity = velocity,
                spin = Random.Range(-1.5f, 1.5f),
                startScale = startScale
            };
        }

        private GameObject MakeParticle(string glyph, string iconKey, Color color, float scale)
        {
            var go = new GameObject("p", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(96, 96);

            var sprite = !string.IsNullOrEmpty(iconKey) ? IconService.Get(iconKey) : null;
            if (sprite != null)
            {
                var imgGo = new GameObject("img", typeof(Image));
                imgGo.transform.SetParent(rt, false);
                var imgRt = (RectTransform)imgGo.transform;
                imgRt.anchorMin = Vector2.zero; imgRt.anchorMax = Vector2.one;
                imgRt.offsetMin = Vector2.zero; imgRt.offsetMax = Vector2.zero;
                var img = imgGo.GetComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = color;
                img.raycastTarget = false;
            }
            else
            {
                var txt = UIFactory.CreateText(rt, glyph, 76, color,
                    TextAlignmentOptions.Center, "txt");
                txt.fontStyle = FontStyles.Bold;
            }
            rt.localScale = Vector3.one * scale;
            return go;
        }
    }
}
