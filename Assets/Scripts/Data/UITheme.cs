// -----------------------------------------------------------------------------
// UITheme.cs
// -----------------------------------------------------------------------------
// Single source of truth for swap-in artwork. Drop your sprites into the
// Inspector slots on this asset and the entire UI picks them up — buttons,
// panels, toggles, backgrounds, etc. — without code changes.
//
// Storage:
//   Place the configured UITheme.asset in `Assets/Resources/UITheme.asset`.
//   `UIThemeService` will pick it up automatically; otherwise the procedural
//   defaults from `DefaultSprite` are used.
//
// Suggested mapping when using the bundled Layer Lab / UI asset packs:
//   buttonSprite           → ui_button.png  (9-sliced)
//   panelSprite            → ui_panel.png   (9-sliced)
//   cardSprite             → ui_card.png    (9-sliced)
//   toggleOnSprite         → "on Toggle.png"  (Layer Lab UI Assets)
//   toggleOffSprite        → "off Toggle.png" (Layer Lab UI Assets)
//   menuBackground         → backgrounds/bg_menu.png
//   gameplayBackground     → backgrounds/bg_play.png
// -----------------------------------------------------------------------------

using UnityEngine;

namespace MathEdu.Data
{
    [CreateAssetMenu(
        fileName = "UITheme",
        menuName = "MathEdu/UI Theme",
        order    = 50)]
    public class UITheme : ScriptableObject
    {
        [Header("Backgrounds")]
        public Sprite menuBackground;
        public Sprite gameplayBackground;
        public Sprite resultsBackground;
        public Sprite settingsBackground;
        public Sprite parentalBackground;
        public Sprite setupBackground;

        [Header("Panels & Buttons (9-sliced if possible)")]
        public Sprite buttonSprite;
        public Sprite panelSprite;
        public Sprite cardSprite;
        public Sprite pillSprite;        // rounded pill shape (feedback toasts)
        public Sprite headerSprite;      // header bar background

        [Header("Icons")]
        public Sprite starFilled;
        public Sprite starEmpty;
        public Sprite lockIcon;
        public Sprite settingsIcon;
        public Sprite backArrow;
        public Sprite chartIcon;
        public Sprite coinIcon;

        [Header("Toggle Sprites (on/off)")]
        public Sprite toggleOnSprite;
        public Sprite toggleOffSprite;

        [Header("Slider")]
        public Sprite sliderBackground;
        public Sprite sliderFill;
        public Sprite sliderHandle;

        [Header("Colours (optional overrides)")]
        public bool   overrideColours;
        public Color  bgTop      = new Color(0.20f, 0.30f, 0.55f);
        public Color  bgBottom   = new Color(0.40f, 0.55f, 0.90f);
        public Color  primary    = new Color(0.30f, 0.65f, 0.95f);
        public Color  success    = new Color(0.30f, 0.80f, 0.40f);
        public Color  danger     = new Color(0.95f, 0.40f, 0.40f);
        public Color  accent     = new Color(0.95f, 0.55f, 0.20f);
    }
}
