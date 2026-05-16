// -----------------------------------------------------------------------------
// FadeOverlay.cs
// -----------------------------------------------------------------------------
// Persistent full-screen black image used by UIManager for scene transitions.
// Acquire() lazily creates the overlay once and survives scene loads.
// -----------------------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public class FadeOverlay : MonoBehaviour
    {
        private static FadeOverlay _instance;
        private Image _image;

        public static FadeOverlay Acquire()
        {
            if (_instance != null) return _instance;

            var go = new GameObject("[FadeOverlay]",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var imgGo = new GameObject("Fade", typeof(Image));
            imgGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var img = imgGo.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = false;

            _instance = go.AddComponent<FadeOverlay>();
            _instance._image = img;
            return _instance;
        }

        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (_image == null) yield break;
            float start = _image.color.a;
            float t = 0;
            duration = Mathf.Max(0.01f, duration);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(start, targetAlpha, t / duration);
                var c = _image.color; c.a = a; _image.color = c;
                yield return null;
            }
            var final = _image.color; final.a = targetAlpha; _image.color = final;
        }
    }
}
