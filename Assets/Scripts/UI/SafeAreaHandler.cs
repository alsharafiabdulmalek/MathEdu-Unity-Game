// -----------------------------------------------------------------------------
// SafeAreaHandler.cs
// -----------------------------------------------------------------------------
// Resizes the attached RectTransform every frame to match Screen.safeArea so
// notched / dynamic-island devices never clip critical UI. Cheap enough to run
// in Update on the root SafeArea node only.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace MathEdu.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _lastSafe;
        private Vector2Int _lastScreen;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        private void OnEnable()  => Apply();
        private void Update()
        {
            if (_lastSafe != Screen.safeArea ||
                _lastScreen.x != Screen.width ||
                _lastScreen.y != Screen.height)
                Apply();
        }

        private void Apply()
        {
            _lastSafe   = Screen.safeArea;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            var safe = Screen.safeArea;
            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rt.anchorMin = anchorMin;
            _rt.anchorMax = anchorMax;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
