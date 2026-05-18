// -----------------------------------------------------------------------------
// IconService.cs
// -----------------------------------------------------------------------------
// Static facade for resolving icons by name. Every UI manager goes through this
// service instead of touching the IconLibrary directly, so the lookup-with-
// fallback pattern is in one place.
//
// Resolution order for IconService.Get(key):
//   1. If a sprite is wired on the loaded IconLibrary in the matching slot, use it.
//   2. Otherwise return null and let the caller fall back to its emoji glyph.
//
// Helpers like IconButton(parent, key, fallbackGlyph, color, ...) handle the
// fallback automatically so the call site stays readable.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public static class IconService
    {
        private static IconLibrary _library;
        private static bool _loaded;

        public static IconLibrary Library
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    _library = Resources.Load<IconLibrary>("IconLibrary");
                }
                return _library;
            }
        }

        public static void Reset()
        {
            _library = null;
            _loaded  = false;
        }

        public static void Override(IconLibrary lib)
        {
            _library = lib;
            _loaded  = true;
        }

        public static Sprite Get(string key)
        {
            var lib = Library;
            if (lib == null) return null;
            return key switch
            {
                "gear"        => lib.gear,
                "settings"    => lib.gear,
                "parent"      => lib.parent,
                "account"     => lib.parent,
                "back"        => lib.back,
                "home"        => lib.home,
                "play"        => lib.play,
                "pause"       => lib.pause,
                "next"        => lib.next,
                "refresh"     => lib.refresh,

                "star"        => lib.star,
                "trophy"      => lib.trophy,
                "crown"       => lib.crown,
                "gem"         => lib.gem,
                "medal"       => lib.medal,
                "badge"       => lib.badge,

                "bulb"        => lib.lightbulb,
                "hint"        => lib.lightbulb,
                "heart"       => lib.heart,
                "life"        => lib.heart,
                "clock"       => lib.clock,
                "check"       => lib.check,
                "correct"     => lib.check,
                "cross"       => lib.cross,
                "wrong"       => lib.cross,
                "lock"        => lib.lockClosed,
                "unlock"      => lib.lockOpen,
                "profile"     => lib.profile,

                "musicOn"     => lib.musicOn,
                "musicOff"    => lib.musicOff,
                "soundOn"     => lib.soundOn,
                "sfxOn"       => lib.soundOn,
                "soundOff"    => lib.soundOff,
                "sfxOff"      => lib.soundOff,

                "smile"       => lib.emojiSmile,
                "happy"       => lib.emojiSmile,
                "cheer"       => lib.emojiSmile,
                "sad"         => lib.emojiSad,
                "angry"       => lib.emojiAngry,
                "wow"         => lib.emojiWow,
                "surprise"    => lib.emojiWow,
                "cool"        => lib.emojiCool != null ? lib.emojiCool : lib.emojiSmile,

                "fire"        => lib.fire,
                "sparkle"     => lib.sparkle,
                "sun"         => lib.sun,
                "flower"      => lib.flower,
                "leaf"        => lib.leaf,
                _             => null
            };
        }

        public static bool Has(string key) => Get(key) != null;

        public static Button IconButton(RectTransform parent, string key,
            string fallbackGlyph, Color? bg = null, string name = null)
        {
            var sprite = Get(key);
            string label = sprite != null ? "" : fallbackGlyph;

            var btn = UIFactory.CreateIconButton(parent, label, bg, name ?? $"Icon_{key}");
            var rt  = (RectTransform)btn.transform;
            rt.sizeDelta = new Vector2(140, 140);

            if (sprite != null)
            {
                AddIconOverlay(rt, sprite, 32f, Color.white);
            }
            return btn;
        }

        public static Image AddIconOverlay(RectTransform parent, Sprite sprite,
            float inset = 24f, Color? tint = null)
        {
            var go = new GameObject("IconOverlay", typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);

            var img = go.GetComponent<Image>();
            img.sprite        = sprite;
            img.color         = tint ?? Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static Image IconWidget(RectTransform parent, string key,
            float size = 64f, Color? tint = null, string name = null)
        {
            var sprite = Get(key);
            var go = new GameObject(name ?? $"IconWidget_{key}", typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(size, size);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color  = tint ?? Color.white;
            }
            else
            {
                img.color = new Color(0, 0, 0, 0);
            }
            return img;
        }

        public static RectTransform IconTextChip(RectTransform parent, string iconKey,
            string fallbackGlyph, string text, int fontSize = 32, Color? tint = null,
            string name = null)
        {
            var go = new GameObject(name ?? $"Chip_{iconKey}",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(140, 60);

            var hl = go.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 8;
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = true;
            hl.padding = new RectOffset(4, 4, 2, 2);

            var sprite = Get(iconKey);
            if (sprite != null)
            {
                var icoGo = new GameObject("Icon", typeof(Image), typeof(LayoutElement));
                icoGo.transform.SetParent(rt, false);
                var icoImg = icoGo.GetComponent<Image>();
                icoImg.sprite = sprite;
                icoImg.preserveAspect = true;
                icoImg.color = tint ?? Color.white;
                icoImg.raycastTarget = false;
                var le = icoGo.GetComponent<LayoutElement>();
                le.preferredWidth = fontSize + 14; le.preferredHeight = fontSize + 14;
            }
            else
            {
                var icoTxt = UIFactory.CreateText(rt, fallbackGlyph, fontSize + 6,
                    tint ?? Color.white, TextAlignmentOptions.Center, "IconGlyph");
                var le = icoTxt.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = fontSize + 14;
            }

            var lbl = UIFactory.CreateText(rt, text, fontSize, tint ?? Color.white,
                TextAlignmentOptions.MidlineLeft, "Label");
            lbl.fontStyle = FontStyles.Bold;
            return rt;
        }
    }
}
