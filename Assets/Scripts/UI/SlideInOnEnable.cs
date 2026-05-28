using System.Collections;
using UnityEngine;

namespace UstAldanQuiz.UI
{
    public class SlideInOnEnable : MonoBehaviour
    {
        [SerializeField] private float slideDistance = 220f;
        [SerializeField] private float duration      = 0.32f;

        private RectTransform _rt;

        private void Awake() => _rt = GetComponent<RectTransform>();

        private void OnEnable()
        {
            StopAllCoroutines();
            StartCoroutine(Slide());
        }

        private IEnumerator Slide()
        {
            Vector2 target = _rt.anchoredPosition;
            _rt.anchoredPosition = target - new Vector2(0f, slideDistance);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _rt.anchoredPosition = Vector2.LerpUnclamped(
                    target - new Vector2(0f, slideDistance),
                    target,
                    EaseOutCubic(t / duration));
                yield return null;
            }
            _rt.anchoredPosition = target;
        }

        private static float EaseOutCubic(float x) =>
            1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);
    }
}
