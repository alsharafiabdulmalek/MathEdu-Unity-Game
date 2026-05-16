// -----------------------------------------------------------------------------
// DefaultSprite.cs
// -----------------------------------------------------------------------------
// Generates plain procedural sprites (rounded rectangles, solid pixels, vertical
// gradients) so the project ships with usable default UI before any custom
// artwork is added. Each method caches its result so identical sprites are
// only generated once per session.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace MathEdu.UI
{
    public static class DefaultSprite
    {
        private static readonly Dictionary<string, Sprite> _cache = new();

        public static Sprite Solid()
        {
            if (_cache.TryGetValue("solid", out var s)) return s;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px  = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            s = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100,
                0, SpriteMeshType.FullRect);
            _cache["solid"] = s;
            return s;
        }

        public static Sprite RoundedRect(int radius = 24)
        {
            string key = $"rr_{radius}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            int size = Mathf.Max(radius * 2 + 4, 32);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px  = new Color[size * size];

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside = true;
                int rx = -1, ry = -1;

                if (x < radius && y < radius)                          { rx = radius - x; ry = radius - y; }
                else if (x >= size - radius && y < radius)             { rx = x - (size - radius - 1); ry = radius - y; }
                else if (x < radius && y >= size - radius)             { rx = radius - x; ry = y - (size - radius - 1); }
                else if (x >= size - radius && y >= size - radius)     { rx = x - (size - radius - 1); ry = y - (size - radius - 1); }

                if (rx >= 0 && ry >= 0)
                {
                    float dist = Mathf.Sqrt(rx * rx + ry * ry);
                    inside = dist <= radius;
                    if (inside && dist > radius - 1)
                    {
                        float a = 1f - (dist - (radius - 1));
                        px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(a));
                        continue;
                    }
                }
                px[y * size + x] = inside ? Color.white : new Color(1, 1, 1, 0);
            }

            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));

            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite Gradient(Color top, Color bottom)
        {
            string key = $"grad_{top}_{bottom}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            int w = 8, h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                var c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode   = TextureWrapMode.Clamp;
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
            _cache[key] = s;
            return s;
        }

        public static Sprite Circle()
        {
            if (_cache.TryGetValue("circle", out var c)) return c;
            int size = 64;
            float r = size * 0.5f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - r, dy = y - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - d);
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels(px); tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
            _cache["circle"] = sp;
            return sp;
        }
    }
}
