using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UstAldanQuiz.Utils
{
    /// <summary>
    /// Пружинная анимация масштаба кнопки.
    /// PointerDown: быстро сжать до 0.90.
    /// PointerUp: отскок по формуле затухающего осциллятора
    ///   s(t) = 1 + A·e^(−d·t)·cos(ω·t), даёт ~3% перелёт за 1.0.
    /// </summary>
    public class ButtonSpringAnim : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Coroutine _anim;

        public void OnPointerDown(PointerEventData _) => Run(SquishDown());
        public void OnPointerUp(PointerEventData _)   => Run(SpringBack());

        private void OnDisable()
        {
            if (_anim != null) StopCoroutine(_anim);
            transform.localScale = Vector3.one;
        }

        private void Run(IEnumerator routine)
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(routine);
        }

        private IEnumerator SquishDown()
        {
            float start = transform.localScale.x;
            const float target   = 0.90f;
            const float duration = 0.08f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = Mathf.Clamp01(t / duration);
                float e = 1f - Mathf.Pow(1f - p, 2f); // ease-out quad
                transform.localScale = Vector3.one * Mathf.Lerp(start, target, e);
                yield return null;
            }
            transform.localScale = Vector3.one * target;
        }

        private IEnumerator SpringBack()
        {
            // A = startScale - 1 (обычно −0.10 после сжатия до 0.90)
            float amp = transform.localScale.x - 1f;
            const float damping  = 8f;  // затухание
            const float omega    = 22f; // угловая частота (рад/с)
            const float duration = 0.6f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float s = 1f + amp * Mathf.Exp(-damping * t) * Mathf.Cos(omega * t);
                transform.localScale = Vector3.one * s;
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}
