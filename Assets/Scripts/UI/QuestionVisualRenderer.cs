// -----------------------------------------------------------------------------
// QuestionVisualRenderer.cs
// -----------------------------------------------------------------------------
// Renders the visual that accompanies each MathQuestion (clock face for time,
// dots for counting, a fraction pie, etc.). Built from primitive Images so it
// works before any custom sprite art is added.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public class QuestionVisualRenderer : MonoBehaviour
    {
        private RectTransform _root;

        public static QuestionVisualRenderer Spawn(RectTransform parent, float height = 360)
        {
            var go = new GameObject("QuestionVisual", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            var r = go.AddComponent<QuestionVisualRenderer>();
            r._root = rt;
            return r;
        }

        public void Show(MathQuestion q)
        {
            Clear();
            if (q == null) return;
            switch (q.visual)
            {
                case QuestionVisual.Dots:        DrawDots(q);       break;
                case QuestionVisual.ClockFace:   DrawClock(q);      break;
                case QuestionVisual.Fraction:    DrawFraction(q);   break;
                case QuestionVisual.NumberLine:  DrawNumberLine(q); break;
                case QuestionVisual.Pattern:     DrawPattern(q);    break;
                case QuestionVisual.Money:       DrawMoney(q);      break;
                case QuestionVisual.ShapePicker: DrawShapeHint(q);  break;
                case QuestionVisual.TextOnly:    /* no visual */    break;
            }
        }

        private void Clear()
        {
            for (int i = _root.childCount - 1; i >= 0; i--)
                Destroy(_root.GetChild(i).gameObject);
        }

        private RectTransform AddImage(Color color, Sprite sprite, Vector2 size, Vector2 pos)
        {
            var go = new GameObject("Img", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = false;
            return rt;
        }

        private TextMeshProUGUI AddLabel(string text, Vector2 pos, int size = 36, Color? color = null)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(_root, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color ?? UIFactory.TextLight;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var rt = tmp.rectTransform;
            rt.sizeDelta = new Vector2(800, 80);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            return tmp;
        }

        private void DrawDots(MathQuestion q)
        {
            if (q.visualPayload == null || q.visualPayload.Length < 1) return;
            int n = Mathf.Min(20, q.visualPayload[0] + (q.visualPayload.Length > 1 ? q.visualPayload[1] : 0));
            int cols = Mathf.CeilToInt(Mathf.Sqrt(n));
            float spacing = 80;
            float startX = -((cols - 1) * spacing) / 2f;

            int placed = 0;
            for (int y = 0; y < cols && placed < n; y++)
            for (int x = 0; x < cols && placed < n; x++)
            {
                Color c = (placed < q.visualPayload[0])
                    ? UIFactory.Accent
                    : (q.visualPayload.Length > 1 ? UIFactory.Primary : UIFactory.Accent);
                AddImage(c, DefaultSprite.Circle(), new Vector2(64, 64),
                    new Vector2(startX + x * spacing, ((cols - 1) / 2f - y) * spacing));
                placed++;
            }
        }

        private void DrawClock(MathQuestion q)
        {
            if (q.visualPayload == null || q.visualPayload.Length < 2) return;
            int hour   = q.visualPayload[0];
            int minute = q.visualPayload[1];

            AddImage(Color.white, DefaultSprite.Circle(), new Vector2(300, 300), Vector2.zero);
            AddImage(UIFactory.TextDark, DefaultSprite.Circle(), new Vector2(280, 280), Vector2.zero);
            AddImage(Color.white, DefaultSprite.Circle(), new Vector2(260, 260), Vector2.zero);

            for (int i = 0; i < 12; i++)
            {
                float ang = i * 30f * Mathf.Deg2Rad;
                Vector2 p = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang)) * 110;
                AddImage(UIFactory.TextDark, DefaultSprite.Circle(), new Vector2(10, 10), p);
            }
            AddLabel("12", new Vector2(0, 100), 36, UIFactory.TextDark);
            AddLabel("3",  new Vector2(100, 0), 36, UIFactory.TextDark);
            AddLabel("6",  new Vector2(0, -100),36, UIFactory.TextDark);
            AddLabel("9",  new Vector2(-100, 0),36, UIFactory.TextDark);

            float hourAng = ((hour % 12) + minute / 60f) * 30f;
            DrawHand(hourAng, 65, 12, UIFactory.TextDark);
            float minAng  = minute * 6f;
            DrawHand(minAng,  95,  8, UIFactory.Primary);
            AddImage(UIFactory.Accent, DefaultSprite.Circle(), new Vector2(20, 20), Vector2.zero);
        }

        private void DrawHand(float deg, float len, float thickness, Color c)
        {
            var go = new GameObject("Hand", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(thickness, len);
            rt.localRotation = Quaternion.Euler(0, 0, -deg);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = c;
            img.sprite = DefaultSprite.RoundedRect(6);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
        }

        private void DrawFraction(MathQuestion q)
        {
            if (q.visualPayload == null || q.visualPayload.Length < 2) return;
            int num = q.visualPayload[0];
            int den = q.visualPayload[1];

            float totalW = 600;
            float h = 120;
            float cellW = totalW / den;
            float startX = -totalW / 2f + cellW / 2f;
            for (int i = 0; i < den; i++)
            {
                bool filled = i < num;
                var rt = AddImage(
                    filled ? UIFactory.Accent : Color.white,
                    DefaultSprite.RoundedRect(16),
                    new Vector2(cellW - 8, h),
                    new Vector2(startX + i * cellW, 0));
                rt.GetComponent<Image>().type = Image.Type.Sliced;
            }
            AddLabel($"{num} of {den}", new Vector2(0, -110), 40, UIFactory.TextLight);
        }

        private void DrawNumberLine(MathQuestion q)
        {
            AddImage(Color.white, DefaultSprite.RoundedRect(8),
                new Vector2(700, 12), Vector2.zero);
            for (int i = 0; i < 10; i++)
            {
                AddImage(UIFactory.Accent, DefaultSprite.Circle(),
                    new Vector2(24, 24), new Vector2(-350 + i * 78, 0));
            }
        }

        private void DrawPattern(MathQuestion q)
        {
            for (int i = 0; i < 5; i++)
            {
                var rt = AddImage(UIFactory.Primary, DefaultSprite.RoundedRect(18),
                    new Vector2(120, 120), new Vector2(-360 + i * 150, 0));
                rt.GetComponent<Image>().type = Image.Type.Sliced;
            }
            var qrt = AddImage(UIFactory.Accent, DefaultSprite.RoundedRect(18),
                new Vector2(120, 120), new Vector2(390, 0));
            qrt.GetComponent<Image>().type = Image.Type.Sliced;
            AddLabel("?", new Vector2(390, 0), 80, UIFactory.TextDark);
        }

        private void DrawMoney(MathQuestion q)
        {
            string[] coinLabels = { "1c", "5c", "10c", "25c", "$1" };
            for (int i = 0; i < coinLabels.Length; i++)
            {
                AddImage(UIFactory.Success, DefaultSprite.Circle(),
                    new Vector2(110, 110), new Vector2(-260 + i * 130, 0));
                AddLabel(coinLabels[i], new Vector2(-260 + i * 130, 0), 32);
            }
        }

        private void DrawShapeHint(MathQuestion q)
        {
            var rt = AddImage(UIFactory.Primary, DefaultSprite.RoundedRect(28),
                new Vector2(240, 240), Vector2.zero);
            rt.GetComponent<Image>().type = Image.Type.Sliced;
            AddLabel("?", Vector2.zero, 120, Color.white);
        }
    }
}
