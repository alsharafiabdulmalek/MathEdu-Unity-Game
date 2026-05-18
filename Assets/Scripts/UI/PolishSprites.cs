// -----------------------------------------------------------------------------
// PolishSprites.cs
// -----------------------------------------------------------------------------
// Additional procedural sprite generators that complement DefaultSprite. These
// are the "polish-pass" art primitives — star, glow ring, soft drop-shadow
// rounded rect, dotted grid pattern — used by the engagement-visuals layer.
//
// Each generator caches its result in a small static dictionary so the same
// sprite isn't re-generated on every menu rebuild.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace MathEdu.UI
{
    public static class PolishSprites
    {
        private static readonly Dictionary<string, Sprite> _cache = new();

        /// <summary>5-point star (white) – plays nicely with Image.color tinting.</summary>
        public static Sprite Star()
        {
            const string key = "star";
            if (_cache.TryGetValue(key, out var hit)) return hit;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float rOuter = size * 0.48f;
            float rInner = size * 0.20f;

            // Build the 10-vertex star polygon and test each pixel for inside-ness.
            var verts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float r = (i % 2 == 0) ? rOuter : rInner;
                float ang = (Mathf.PI / 2f) - (i * Mathf.PI / 5f); // start at top
                verts[i] = new Vector2(cx + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r);
            }

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside = PointInPolygon(new Vector2(x + 0.5f, y + 0.5f), verts);
                // Soft anti-alias by sampling 4 sub-points
                int hits = inside ? 1 : 0;
                hits += PointInPolygon(new Vector2(x + 0.2f, y + 0.2f), verts) ? 1 : 0;
                hits += PointInPolygon(new Vector2(x + 0.8f, y + 0.2f), verts) ? 1 : 0;
                hits += PointInPolygon(new Vector2(x + 0.8f, y + 0.8f), verts) ? 1 : 0;
                float a = hits / 4f;
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels(px); tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
            _cache[key] = s;
            return s;
        }

        /// <summary>Soft circular glow — radial falloff from centre.</summary>
        public static Sprite Glow()
        {
            const string key = "glow";
            if (_cache.TryGetValue(key, out var hit)) return hit;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            float c = size * 0.5f, r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                // Quadratic falloff
                float a = Mathf.Clamp01(1f - d * d);
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels(px); tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
            _cache[key] = s;
            return s;
        }

        /// <summary>Hollow ring with a 16 % thickness (for emphasis halos).</summary>
        public static Sprite Ring(int thicknessPct = 14)
        {
            string key = $"ring_{thicknessPct}";
            if (_cache.TryGetValue(key, out var hit)) return hit;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            float c = size * 0.5f;
            float rOuter = size * 0.49f;
            float rInner = rOuter * (1f - thicknessPct / 100f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(Mathf.Min(rOuter - d, d - rInner));
                px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(a));
            }
            tex.SetPixels(px); tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
            _cache[key] = s;
            return s;
        }

        /// <summary>
        /// Rounded rectangle with a small drop shadow baked into the texture.
        /// Useful for the "lifted" card look without needing a separate shadow
        /// image. The shadow takes up ~12 px on all sides.
        /// </summary>
        public static Sprite ShadowedRoundedRect(int radius = 24)
        {
            string key = $"shadowrr_{radius}";
            if (_cache.TryGetValue(key, out var hit)) return hit;

            const int pad = 14;
            int size = Mathf.Max(radius * 2 + 4, 32) + pad * 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];

            // 1) Draw a soft drop shadow underneath: offset by +6 px down, then
            //    blur via two passes of a 3x3 box average.
            var shadow = new float[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int sx = x;
                int sy = y + 6;
                if (sy < 0 || sy >= size) continue;
                if (sx < pad || sx >= size - pad) continue;
                if (sy < pad || sy >= size - pad) continue;
                bool inside = IsInsideRR(sx - pad, sy - pad, size - pad * 2, size - pad * 2, radius);
                if (inside) shadow[y * size + x] = 1f;
            }
            for (int pass = 0; pass < 3; pass++) BoxBlur(shadow, size);

            // 2) Stamp the rounded rect on top.
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int ix = x - pad, iy = y - pad;
                bool inside = false;
                if (ix >= 0 && iy >= 0 && ix < size - pad * 2 && iy < size - pad * 2)
                    inside = IsInsideRR(ix, iy, size - pad * 2, size - pad * 2, radius);

                float shA = shadow[y * size + x] * 0.45f;
                if (inside)
                    px[y * size + x] = Color.white;
                else if (shA > 0.005f)
                    px[y * size + x] = new Color(0, 0, 0, shA);
                else
                    px[y * size + x] = new Color(0, 0, 0, 0);
            }
            tex.SetPixels(px); tex.Apply();
            tex.filterMode = FilterMode.Bilinear;

            var s = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius + pad, radius + pad, radius + pad, radius + pad));

            _cache[key] = s;
            return s;
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------
        private static bool PointInPolygon(Vector2 p, Vector2[] verts)
        {
            bool inside = false;
            int n = verts.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((verts[i].y > p.y) != (verts[j].y > p.y)) &&
                    (p.x < (verts[j].x - verts[i].x) * (p.y - verts[i].y) /
                        (verts[j].y - verts[i].y + 0.0001f) + verts[i].x))
                    inside = !inside;
            }
            return inside;
        }

        private static bool IsInsideRR(int x, int y, int w, int h, int radius)
        {
            int rx = -1, ry = -1;
            if (x < radius && y < radius)                     { rx = radius - x; ry = radius - y; }
            else if (x >= w - radius && y < radius)           { rx = x - (w - radius - 1); ry = radius - y; }
            else if (x < radius && y >= h - radius)           { rx = radius - x; ry = y - (h - radius - 1); }
            else if (x >= w - radius && y >= h - radius)      { rx = x - (w - radius - 1); ry = y - (h - radius - 1); }
            if (rx < 0 || ry < 0) return true;
            return rx * rx + ry * ry <= radius * radius;
        }

        private static void BoxBlur(float[] buf, int size)
        {
            var copy = new float[buf.Length];
            System.Array.Copy(buf, copy, buf.Length);
            for (int y = 1; y < size - 1; y++)
            for (int x = 1; x < size - 1; x++)
            {
                float sum = 0;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    sum += copy[(y + dy) * size + (x + dx)];
                buf[y * size + x] = sum / 9f;
            }
        }
    }
}
