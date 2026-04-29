using UnityEngine;

namespace UstAldanQuiz.Utils
{
    /// <summary>
    /// Адаптирует RectTransform под безопасную зону экрана (вырезы, чёлки, home bar).
    /// Повесить на SafeArea GameObject внутри Canvas. Anchors: stretch/stretch.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (_lastSafeArea != Screen.safeArea || _lastScreenSize != screenSize)
                Apply();
        }

        private void Apply()
        {
            _lastSafeArea   = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width == 0 || Screen.height == 0) return;

            var anchorMin = _lastSafeArea.position;
            var anchorMax = _lastSafeArea.position + _lastSafeArea.size;

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
