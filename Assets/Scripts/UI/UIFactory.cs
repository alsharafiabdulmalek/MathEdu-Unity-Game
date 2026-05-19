// -----------------------------------------------------------------------------
// UIFactory.cs
// -----------------------------------------------------------------------------
// Utility helpers for building Canvas / TMP UI at runtime. Every screen in the
// game constructs its widgets through these helpers so we get:
//   - One consistent visual style.
//   - Zero hand-edited .unity YAML for layout.
//   - Safe-area-aware containers out of the box.
//   - Automatic i18n: every TMP text created here flows through
//     Localization.Apply() so the Arabic font + RTL flag are applied in
//     one place without each screen having to repeat itself.
// -----------------------------------------------------------------------------

using MathEdu.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public static class UIFactory
    {
        public static Color BgTop     => Themed(c => c.bgTop,    new Color(0.20f, 0.30f, 0.55f));
        public static Color BgBottom  => Themed(c => c.bgBottom, new Color(0.40f, 0.55f, 0.90f));
        public static readonly Color Panel     = new Color(1.00f, 1.00f, 1.00f, 0.95f);
        public static readonly Color Card      = new Color(1.00f, 1.00f, 1.00f, 1.00f);
        public static Color Accent    => Themed(c => c.accent,   new Color(0.95f, 0.55f, 0.20f));
        public static Color Primary   => Themed(c => c.primary,  new Color(0.30f, 0.65f, 0.95f));
        public static Color Success   => Themed(c => c.success,  new Color(0.30f, 0.80f, 0.40f));
        public static Color Danger    => Themed(c => c.danger,   new Color(0.95f, 0.40f, 0.40f));
        public static readonly Color TextDark  = new Color(0.10f, 0.15f, 0.25f);
        public static readonly Color TextLight = Color.white;

        private static Color Themed(System.Func<MathEdu.Data.UITheme, Color> get, Color def)
        {
            var t = UIThemeService.Theme;
            return (t != null && t.overrideColours) ? get(t) : def;
        }

        public static (Canvas canvas, RectTransform safeArea) CreateCanvas(string name = "[UIRoot]")
        {
            // -------- Camera --------
            if (Object.FindAnyObjectByType<Camera>() == null)
            {
                var camGo = new GameObject("[MainCamera]", typeof(Camera));
                camGo.tag = "MainCamera";
                var cam = camGo.GetComponent<Camera>();
                cam.clearFlags     = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.10f, 0.13f, 0.20f);
                cam.cullingMask    = 0;
                cam.orthographic   = true;
                cam.nearClipPlane  = 0.1f;
                cam.farClipPlane   = 100f;
                cam.depth          = -100;
                if (Object.FindAnyObjectByType<AudioListener>() == null)
                    camGo.AddComponent<AudioListener>();
            }

            // -------- Canvas --------
            var canvasGo = new GameObject(name,
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;
            scaler.referencePixelsPerUnit = 100;

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("[EventSystem]",
                    typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var safeAreaGo = new GameObject("SafeArea", typeof(RectTransform));
            var sa = safeAreaGo.GetComponent<RectTransform>();
            sa.SetParent(canvas.transform, false);
            sa.anchorMin = Vector2.zero;
            sa.anchorMax = Vector2.one;
            sa.offsetMin = Vector2.zero;
            sa.offsetMax = Vector2.zero;
            safeAreaGo.AddComponent<SafeAreaHandler>();

            return (canvas, sa);
        }

        public static Image CreateGradientBackground(RectTransform parent, Color top, Color bottom)
        {
            var go = new GameObject("Background", typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Stretch(rt);
            var img = go.GetComponent<Image>();
            img.color  = Color.white;
            img.sprite = DefaultSprite.Gradient(top, bottom);
            img.raycastTarget = false;

            // Polish: layer a soft radial glow on top to mimic post-processing
            // bloom / vignette. Cheap (one extra Image) and totally optional —
            // a UITheme background overrides this whole path anyway.
            var glow = new GameObject("BgGlow", typeof(Image));
            glow.transform.SetParent(rt, false);
            var grt = glow.GetComponent<RectTransform>();
            Stretch(grt);
            var glowImg = glow.GetComponent<Image>();
            glowImg.sprite = PolishSprites.Glow();
            glowImg.color  = new Color(1f, 1f, 1f, 0.18f);
            glowImg.raycastTarget = false;
            return img;
        }

        public static Image CreateSolidBackground(RectTransform parent, Color c)
        {
            var go = new GameObject("Background", typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Stretch(rt);
            var img = go.GetComponent<Image>();
            img.color = c;
            img.sprite = DefaultSprite.Solid();
            img.raycastTarget = false;
            return img;
        }

        public static Image CreateThemedBackground(RectTransform parent, string key)
        {
            var sprite = UIThemeService.BackgroundFor(key);
            if (sprite != null)
            {
                var go = new GameObject("Background", typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                Stretch(rt);
                var img = go.GetComponent<Image>();
                img.color  = Color.white;
                img.sprite = sprite;
                img.type   = Image.Type.Simple;
                img.raycastTarget  = false;
                return img;
            }
            return CreateGradientBackground(parent, BgTop, BgBottom);
        }

        public static RectTransform CreatePanel(RectTransform parent, Vector2 anchorMin,
            Vector2 anchorMax, Color color, float cornerRadius = 16f, string name = "Panel")
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color  = color;
            img.sprite = UIThemeService.PanelSprite() != null
                ? UIThemeService.PanelSprite()
                : DefaultSprite.RoundedRect((int)cornerRadius);
            img.type   = Image.Type.Sliced;
            return rt;
        }

        public static RectTransform CreateCard(RectTransform parent, Color color, string name = "Card")
        {
            return CreatePanel(parent, new Vector2(0, 0), new Vector2(1, 1), color, 24, name);
        }

        public static TextMeshProUGUI CreateText(RectTransform parent, string text,
            int fontSize = 42, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Center,
            string name = "Text")
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            // Arabic-shape the input on the way in so callers that pass raw
            // logical-order Arabic (e.g. question prompts from QuestionStrings,
            // dynamic answer options, hardcoded fallback labels) end up with
            // connected cursive glyphs instead of disconnected letter boxes.
            tmp.text          = Localization.Shape(text);
            tmp.fontSize      = fontSize;
            tmp.color         = color ?? TextDark;
            tmp.alignment     = align;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;

            var rt = tmp.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16, 8);
            rt.offsetMax = new Vector2(-16, -8);

            // i18n: swap font + RTL flag based on the current language.
            // No-op when language is English.
            Localization.Apply(tmp);
            return tmp;
        }

        public static Button CreateButton(RectTransform parent, string label,
            Color? bg = null, int fontSize = 48, string name = "Button")
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(480, 140);

            var img = go.GetComponent<Image>();
            img.color  = bg ?? Primary;
            img.sprite = UIThemeService.ButtonSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
            colors.pressedColor     = new Color(0.80f, 0.80f, 0.80f);
            colors.selectedColor    = new Color(0.94f, 0.94f, 0.94f);
            btn.colors = colors;

            var txt = CreateText(rt, label, fontSize, TextLight, TextAlignmentOptions.Center, "Label");
            txt.fontStyle = FontStyles.Bold;
            txt.raycastTarget = false;

            // Universal tap-sound hook: every button created via UIFactory
            // plays the "tap" SFX on click. This removes ~50 redundant
            // `PlaySFX("tap")` calls scattered through the manager scripts.
            // AudioManager throttles repeated taps so this is safe in lists.
            btn.onClick.AddListener(PlayButtonTap);
            return btn;
        }

        // Hooked into every UIFactory-built Button. Stored once on the static
        // type so listener removal/add works as expected.
        private static void PlayButtonTap()
        {
            var gm = MathEdu.Managers.GameManager.Instance;
            if (gm != null && gm.Audio != null) gm.Audio.PlaySFX("tap");
        }

        public static Button CreateIconButton(RectTransform parent, string icon,
            Color? bg = null, string name = "IconButton")
        {
            var btn = CreateButton(parent, icon, bg, 60, name);
            ((RectTransform)btn.transform).sizeDelta = new Vector2(140, 140);
            return btn;
        }

        public static TMP_InputField CreateInputField(RectTransform parent, string placeholder,
            int fontSize = 44, string name = "Input")
        {
            var go = new GameObject(name, typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(640, 140);

            var img = go.GetComponent<Image>();
            img.color  = Color.white;
            img.sprite = UIThemeService.PanelSprite() != null
                ? UIThemeService.PanelSprite()
                : DefaultSprite.RoundedRect(20);
            img.type = Image.Type.Sliced;

            var input = go.GetComponent<TMP_InputField>();

            var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(rt, false);
            var tar = (RectTransform)textArea.transform;
            tar.anchorMin = Vector2.zero; tar.anchorMax = Vector2.one;
            tar.offsetMin = new Vector2(24, 12);
            tar.offsetMax = new Vector2(-24, -12);

            var placeholderTxt = CreateText(tar, placeholder, fontSize,
                new Color(0.4f, 0.4f, 0.5f),
                Localization.IsRTL ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft,
                "Placeholder");
            placeholderTxt.fontStyle = FontStyles.Italic;

            var contentTxt = CreateText(tar, "", fontSize,
                TextDark,
                Localization.IsRTL ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft,
                "Content");

            input.textViewport = tar;
            input.textComponent = contentTxt;
            input.placeholder   = placeholderTxt;
            input.fontAsset     = contentTxt.font;
            input.characterLimit = 24;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        public static Slider CreateSlider(RectTransform parent, float initialValue = 0.5f,
            string name = "Slider")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(560, 60);

            var slider = go.GetComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue  = 0f;
            slider.maxValue  = 1f;
            slider.wholeNumbers = false;

            var bg = new GameObject("Background", typeof(Image));
            bg.transform.SetParent(rt, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = new Vector2(0, 0.25f);
            bgRt.anchorMax = new Vector2(1, 0.75f);
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.2f, 0.25f, 0.35f, 0.6f);
            bgImg.sprite = UIThemeService.SliderBg();
            bgImg.type   = Image.Type.Sliced;

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(rt, false);
            var faRt = (RectTransform)fillArea.transform;
            faRt.anchorMin = new Vector2(0, 0.25f);
            faRt.anchorMax = new Vector2(1, 0.75f);
            faRt.offsetMin = new Vector2(10, 0);
            faRt.offsetMax = new Vector2(-10, 0);

            var fill = new GameObject("Fill", typeof(Image));
            fill.transform.SetParent(faRt, false);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            var fillImg = fill.GetComponent<Image>();
            fillImg.color  = Primary;
            fillImg.sprite = UIThemeService.SliderFill();
            fillImg.type   = Image.Type.Sliced;

            var handleArea = new GameObject("HandleSlideArea", typeof(RectTransform));
            handleArea.transform.SetParent(rt, false);
            var haRt = (RectTransform)handleArea.transform;
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(20, 0); haRt.offsetMax = new Vector2(-20, 0);

            var handle = new GameObject("Handle", typeof(Image));
            handle.transform.SetParent(haRt, false);
            var hRt = (RectTransform)handle.transform;
            hRt.sizeDelta = new Vector2(56, 56);
            var hImg = handle.GetComponent<Image>();
            hImg.color  = Color.white;
            hImg.sprite = UIThemeService.SliderHandle();

            slider.fillRect       = fillRt;
            slider.handleRect     = hRt;
            slider.targetGraphic  = hImg;
            slider.value          = Mathf.Clamp01(initialValue);
            return slider;
        }

        public static GameObject CreateVerticalLayout(RectTransform parent, float spacing = 24f,
            RectOffset padding = null, string name = "VLayout")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            var v = go.GetComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset(24, 24, 24, 24);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth  = true;
            v.childForceExpandHeight = false;
            return go;
        }

        public static GameObject CreateHorizontalLayout(RectTransform parent, float spacing = 24f,
            RectOffset padding = null, string name = "HLayout")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = padding ?? new RectOffset(16, 16, 16, 16);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = false;
            return go;
        }

        public static GameObject CreateGridLayout(RectTransform parent, Vector2 cellSize,
            Vector2 spacing, int constraintCount = 2, string name = "Grid")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(parent, false);
            var g = go.GetComponent<GridLayoutGroup>();
            g.cellSize  = cellSize;
            g.spacing   = spacing;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = constraintCount;
            g.padding = new RectOffset(24, 24, 24, 24);
            g.childAlignment = TextAnchor.UpperCenter;
            return go;
        }

        public static ScrollRect CreateScrollView(RectTransform parent, string name = "Scroll")
        {
            var scrollGo = new GameObject(name, typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            var rt = (RectTransform)scrollGo.transform;
            Stretch(rt);

            var bgImg = scrollGo.GetComponent<Image>();
            bgImg.color = new Color(1, 1, 1, 0);

            var sr = scrollGo.GetComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical   = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform),
                typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            var vrt = (RectTransform)viewport.transform;
            Stretch(vrt);
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot     = new Vector2(0.5f, 1);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(0, 0);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 24;
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vrt;
            sr.content  = crt;
            return sr;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void AnchorTop(RectTransform rt, float height = 200, float topPad = 0)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -topPad);
            rt.sizeDelta = new Vector2(0, height);
        }

        public static void AnchorBottom(RectTransform rt, float height = 200, float bottomPad = 0)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot     = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, bottomPad);
            rt.sizeDelta = new Vector2(0, height);
        }
    }
}
