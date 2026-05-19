using UnityEngine;
using UnityEngine.UI;

namespace UstAldanQuiz.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class StatusBarCover : MonoBehaviour
    {
        private void Start()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            float topGapPx = Screen.height - (Screen.safeArea.y + Screen.safeArea.height);
            float h = topGapPx / canvas.scaleFactor;

            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, Mathf.Max(0f, h));
        }
    }
}
