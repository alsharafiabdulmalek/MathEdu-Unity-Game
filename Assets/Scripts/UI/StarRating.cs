// -----------------------------------------------------------------------------
// StarRating.cs
// -----------------------------------------------------------------------------
// Tiny widget that draws 0-3 stars using TMP text glyphs. Avoids needing star
// sprite assets up front; swap in a Sprite-based version when artwork lands.
// -----------------------------------------------------------------------------

using System.Collections;
using TMPro;
using UnityEngine;

namespace MathEdu.UI
{
    public class StarRating : MonoBehaviour
    {
        private TextMeshProUGUI _label;

        public static StarRating Spawn(RectTransform parent, int stars = 0, int size = 64)
        {
            var go = new GameObject("Stars", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<StarRating>();
            sr._label = UIFactory.CreateText(
                (RectTransform)go.transform,
                Render(stars),
                size,
                UIFactory.Accent,
                TextAlignmentOptions.Center,
                "StarsText");
            return sr;
        }

        public void SetStars(int n)
        {
            if (_label != null) _label.text = Render(n);
        }

        public IEnumerator AnimateTo(int stars, float perStep = 0.35f)
        {
            for (int i = 0; i <= stars; i++)
            {
                SetStars(i);
                yield return new WaitForSeconds(perStep);
            }
        }

        private static string Render(int stars)
        {
            stars = Mathf.Clamp(stars, 0, 3);
            string s = "";
            for (int i = 0; i < 3; i++)
                s += i < stars ? "★" : "☆";
            return s;
        }
    }
}
