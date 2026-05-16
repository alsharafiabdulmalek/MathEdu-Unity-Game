// -----------------------------------------------------------------------------
// ProgressBar.cs
// -----------------------------------------------------------------------------
// Slim horizontal bar built from two Images. Spawn(parent) builds it. SetValue
// (0..1) updates the fill width using anchorMax.x so it auto-scales with
// container size.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public class ProgressBar : MonoBehaviour
    {
        private RectTransform _fill;
        private Image _fillImg;

        public static ProgressBar Spawn(RectTransform parent, float height = 28f,
            Color? back = null, Color? fill = null)
        {
            var go = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(0, height);
            var bg = go.GetComponent<Image>();
            bg.color  = back ?? new Color(0, 0, 0, 0.25f);
            bg.sprite = DefaultSprite.RoundedRect(14);
            bg.type   = Image.Type.Sliced;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(go.transform, false);
            var frt = (RectTransform)fillGo.transform;
            frt.anchorMin = new Vector2(0, 0);
            frt.anchorMax = new Vector2(0, 1);
            frt.offsetMin = new Vector2(2, 2);
            frt.offsetMax = new Vector2(-2, -2);
            var fimg = fillGo.GetComponent<Image>();
            fimg.color  = fill ?? UIFactory.Success;
            fimg.sprite = DefaultSprite.RoundedRect(12);
            fimg.type   = Image.Type.Sliced;
            fimg.raycastTarget = false;

            var pb = go.AddComponent<ProgressBar>();
            pb._fill    = frt;
            pb._fillImg = fimg;
            return pb;
        }

        public void SetValue(float v)
        {
            v = Mathf.Clamp01(v);
            if (_fill == null) return;
            _fill.anchorMax = new Vector2(v, 1);
        }

        public void SetFillColor(Color c)
        {
            if (_fillImg != null) _fillImg.color = c;
        }
    }
}
