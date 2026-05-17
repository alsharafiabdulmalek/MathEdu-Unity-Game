// -----------------------------------------------------------------------------
// AccuracyBarChart.cs
// -----------------------------------------------------------------------------
// Renders a small, horizontal "accuracy" bar chart inside the Parental
// Dashboard. Each row is a subject; the filled portion of the bar represents
// the percentage of questions answered correctly. Designed to be readable on
// a phone in a glance.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using MathEdu.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public class AccuracyBarChart : MonoBehaviour
    {
        public static AccuracyBarChart Spawn(RectTransform parent,
            IList<(string label, float pct, Color color)> rows, string title = "")
        {
            var go = new GameObject("AccuracyChart", typeof(RectTransform), typeof(AccuracyBarChart));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var col = UIFactory.CreateVerticalLayout(rt, 12,
                new RectOffset(20, 20, 20, 20), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            if (!string.IsNullOrEmpty(title))
            {
                var t = UIFactory.CreateText((RectTransform)col.transform, title, 40,
                    Color.white, TextAlignmentOptions.Left, "Title");
                t.fontStyle = FontStyles.Bold;
                var le = t.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 56;
            }

            foreach (var row in rows)
                BuildRow((RectTransform)col.transform, row.label, row.pct, row.color);

            return go.GetComponent<AccuracyBarChart>();
        }

        private static void BuildRow(RectTransform parent, string label, float pct, Color color)
        {
            var row = new GameObject($"Row_{label}", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var rrt = (RectTransform)row.transform;
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 70; le.minHeight = 70;

            // Label (left, 30%)
            var lbl = UIFactory.CreateText(rrt, label, 30,
                Color.white, TextAlignmentOptions.MidlineLeft, "Lbl");
            lbl.fontStyle = FontStyles.Bold;
            var lrt = lbl.rectTransform;
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(0.32f, 1);
            lrt.offsetMin = new Vector2(8, 4); lrt.offsetMax = new Vector2(-8, -4);

            // Bar background
            var bg = new GameObject("Bg", typeof(Image));
            bg.transform.SetParent(rrt, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = new Vector2(0.32f, 0.20f);
            bgRt.anchorMax = new Vector2(0.86f, 0.80f);
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.color  = new Color(1, 1, 1, 0.15f);
            bgImg.sprite = DefaultSprite.RoundedRect(16);
            bgImg.type   = Image.Type.Sliced;

            // Bar fill
            var fill = new GameObject("Fill", typeof(Image));
            fill.transform.SetParent(bgRt, false);
            var frt = (RectTransform)fill.transform;
            frt.anchorMin = new Vector2(0, 0);
            frt.anchorMax = new Vector2(Mathf.Clamp01(pct / 100f), 1);
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            var fImg = fill.GetComponent<Image>();
            fImg.color  = color;
            fImg.sprite = DefaultSprite.RoundedRect(16);
            fImg.type   = Image.Type.Sliced;

            // Percent text
            var pctTxt = UIFactory.CreateText(rrt, $"{Mathf.RoundToInt(pct)}%", 30,
                Color.white, TextAlignmentOptions.MidlineRight, "Pct");
            var prt = pctTxt.rectTransform;
            prt.anchorMin = new Vector2(0.86f, 0); prt.anchorMax = new Vector2(1, 1);
            prt.offsetMin = new Vector2(8, 4); prt.offsetMax = new Vector2(-8, -4);
        }
    }
}
