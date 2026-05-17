// -----------------------------------------------------------------------------
// UIThemeService.cs
// -----------------------------------------------------------------------------
// Lazy, static accessor for the optional UITheme ScriptableObject. Every
// UIFactory helper consults this service first; if a Sprite slot on the theme
// is non-null it's used, otherwise the procedural DefaultSprite is returned.
//
// To plug your own sprite art into the entire UI:
//   1. Create → MathEdu → UI Theme  (or drop your assets into the inspector
//      of an existing one).
//   2. Move/copy that asset to `Assets/Resources/UITheme.asset`.
//   3. Press Play. Every screen now uses your sprites.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using UnityEngine;

namespace MathEdu.UI
{
    public static class UIThemeService
    {
        private static UITheme _theme;
        private static bool    _loaded;

        public static UITheme Theme
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    _theme  = Resources.Load<UITheme>("UITheme");
                    if (_theme == null)
                        _theme = Resources.Load<UITheme>("UI/UITheme");
                }
                return _theme;
            }
        }

        public static void Override(UITheme theme)
        {
            _theme  = theme;
            _loaded = true;
        }

        // ---- helpers ------------------------------------------------------

        public static Sprite ButtonSprite()
            => Theme != null && Theme.buttonSprite != null
                ? Theme.buttonSprite
                : DefaultSprite.RoundedRect(24);

        public static Sprite PanelSprite()
            => Theme != null && Theme.panelSprite != null
                ? Theme.panelSprite
                : DefaultSprite.RoundedRect(16);

        public static Sprite CardSprite()
            => Theme != null && Theme.cardSprite != null
                ? Theme.cardSprite
                : DefaultSprite.RoundedRect(24);

        public static Sprite PillSprite()
            => Theme != null && Theme.pillSprite != null
                ? Theme.pillSprite
                : DefaultSprite.RoundedRect(36);

        public static Sprite HeaderSprite()
            => Theme != null && Theme.headerSprite != null
                ? Theme.headerSprite
                : DefaultSprite.Solid();

        public static Sprite BackgroundFor(string key)
        {
            if (Theme == null) return null;
            return key switch
            {
                "menu"     => Theme.menuBackground,
                "play"     => Theme.gameplayBackground,
                "results"  => Theme.resultsBackground,
                "settings" => Theme.settingsBackground,
                "parental" => Theme.parentalBackground,
                "setup"    => Theme.setupBackground,
                _          => Theme.menuBackground
            };
        }

        public static Sprite SliderBg()
            => Theme != null && Theme.sliderBackground != null
                ? Theme.sliderBackground
                : DefaultSprite.RoundedRect(20);

        public static Sprite SliderFill()
            => Theme != null && Theme.sliderFill != null
                ? Theme.sliderFill
                : DefaultSprite.RoundedRect(20);

        public static Sprite SliderHandle()
            => Theme != null && Theme.sliderHandle != null
                ? Theme.sliderHandle
                : DefaultSprite.Circle();

        public static Sprite ToggleOn()
            => Theme != null ? Theme.toggleOnSprite : null;

        public static Sprite ToggleOff()
            => Theme != null ? Theme.toggleOffSprite : null;
    }
}
