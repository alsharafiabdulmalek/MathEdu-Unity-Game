// -----------------------------------------------------------------------------
// UIFactory.cs
// -----------------------------------------------------------------------------
// Utility helpers for building Canvas / TMP UI at runtime. Every screen in the
// game constructs its widgets through these helpers, so we get:
//   - One consistent visual style.
//   - Zero hand-edited .unity YAML for layout - scenes only need a single
//     [SceneBuilder] GameObject with the appropriate manager script.
//   - Safe-area-aware containers out of the box.
//
// When real sprite assets are dropped in, the static "DefaultSprite" methods
// are the single place to swap in the new artwork.
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public static class UIFactory
    {
        // -------------------------------------------------------------------
        // Color palette (designer-tunable)
        // -------------------------------------------------------------------
        public static readonly Color BgTop     = new Color(0.20f, 0.30f, 0.55f);
        public static readonly Color BgBottom  = new Color(0.40f, 0.55f, 0.90f);
        public static readonly Color Panel     = new Color(1.00f, 1.00f, 1.00f, 0.95f);
        public static readonly Color Card      = new Color(1.00f, 1.00f, 1.00f, 1.00f);
        public static readonly Color Accent    = new Color(0.95f, 0.55f, 0.20f);
        public static readonly Color Primary   = new Color(0.30f, 0.65f, 0.95f);
        public static readonly Color Success   = new Color(0.30f, 0.80f, 0.40f);
        public static readonly Color Danger    = new Color(0.95f, 0.40f, 0.40f);
        public static readonly Color TextDark  = new Color(0.10f, 0.15f, 0.25f);
        public static readonly Color TextLight = Color.white;

        // -------------------------------------------------------------------
        // Root canvas with EventSystem and safe-area handling
        // -------------------------------------------------------------------
        public static (Canvas canvas, RectTransform safeArea) CreateCanvas(string name = "[UIRoot]")
        {
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

        // -------------------------------------------------------------------
        // Backgrounds
        // -------------------------------------------------------------------
        public static Image CreateGradientBackground(RectTransform parent, Color top, Color bottom)
        {
            var go = new GameObject("Background", typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Stretch(rt);
            var img = go.GetComponent<Image>();
            img.color  = Color.white;
            img.sprite = DefaultSprite.Gradient(top, bottom);
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
            return img;
        }

        // -------------------------------------------------------------------
        // Panels and cards
        // -------------------------------------------------------------------
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
            img.sprite = DefaultSprite.RoundedRect((int)cornerRadius);
            img.type   = Image.Type.Sliced;
            return rt;
        }

        public static RectTransform CreateCard(RectTransform parent, Color color, string name = "Card")
        {
            var rt = CreatePanel(parent, new Vector2(0, 0), new Vector2(1, 1), color, 24, name);
            return rt;
        }

        // -------------------------------------------------------------------
        // Text (TextMeshProUGUI)
        // -------------------------------------------------------------------
        public static TextMeshProUGUI CreateText(RectTransform parent, string text,
            int fontSize = 42, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Center,
            string name = "Text")
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text          = text;
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
            return tmp;
        }

        // -------------------------------------------------------------------
        // Buttons
        // -------------------------------------------------------------------
        public static Button CreateButton(RectTransform parent, string label,
            Color? bg = null, int fontSize = 48, string name = "Button")
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(480, 140);

            var img = go.GetComponent<Image>();
            img.color  = bg ?? Primary;
            img.sprite = DefaultSprite.RoundedRect(24);
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
            return btn;
        }

        public static Button CreateIconButton(RectTransform parent, string icon,
            Color? bg = null, string name = "IconButton")
        {
            var btn = CreateButton(parent, icon, bg, 60, name);
            ((RectTransform)btn.transform).sizeDelta = new Vector2(140, 140);
            return btn;
        }

        // -------------------------------------------------------------------
        // Layout helpers
        // -------------------------------------------------------------------
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

        // -------------------------------------------------------------------
        // Misc utilities
        // -------------------------------------------------------------------
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
