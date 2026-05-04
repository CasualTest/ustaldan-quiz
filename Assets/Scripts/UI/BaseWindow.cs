using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UstAldanQuiz.UI
{
    public abstract class BaseWindow : MonoBehaviour
    {
        [Header("Окно")]
        [SerializeField] protected GameObject panel;
        [SerializeField] protected Button     btnClose;

        [Header("Анимация")]
        [SerializeField] private RectTransform sheetRect;
        [SerializeField] private CanvasGroup   sheetGroup;
        [SerializeField] private CanvasGroup   overlayGroup;

        private const float DurationOpen  = 0.22f;
        private const float DurationClose = 0.15f;

        private Coroutine _anim;

        // Awake вызывается даже у неактивных объектов — безопасно для панелей
        // которые стартуют с SetActive(false)
        protected virtual void Awake()
        {
            btnClose?.onClick.AddListener(Close);
        }

        protected virtual void Start()
        {
            OnWindowStart();
        }

        protected virtual void OnDestroy()
        {
            btnClose?.onClick.RemoveAllListeners();
            OnWindowDestroy();
        }

        // Хуки для наследников
        protected virtual void OnWindowStart()   { }
        protected virtual void OnWindowDestroy() { }

        public virtual void Open()
        {
            panel?.SetActive(true);
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateOpen());
        }

        public virtual void Close()
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateClose());
        }

        // ── Анимации ─────────────────────────────────────────────────────────

        private IEnumerator AnimateOpen()
        {
            if (sheetRect    != null) sheetRect.localScale = Vector3.one * 0.85f;
            if (sheetGroup   != null) sheetGroup.alpha     = 0f;
            if (overlayGroup != null) overlayGroup.alpha   = 0f;

            for (float t = 0; t < DurationOpen; t += Time.unscaledDeltaTime)
            {
                float e = EaseOutCubic(t / DurationOpen);
                if (sheetRect    != null) sheetRect.localScale = Vector3.LerpUnclamped(Vector3.one * 0.85f, Vector3.one, e);
                if (sheetGroup   != null) sheetGroup.alpha     = e;
                if (overlayGroup != null) overlayGroup.alpha   = e;
                yield return null;
            }

            if (sheetRect    != null) sheetRect.localScale = Vector3.one;
            if (sheetGroup   != null) sheetGroup.alpha     = 1f;
            if (overlayGroup != null) overlayGroup.alpha   = 1f;
        }

        private IEnumerator AnimateClose()
        {
            float s0 = sheetRect    != null ? sheetRect.localScale.x : 1f;
            float a0 = sheetGroup   != null ? sheetGroup.alpha       : 1f;
            float o0 = overlayGroup != null ? overlayGroup.alpha     : 1f;

            for (float t = 0; t < DurationClose; t += Time.unscaledDeltaTime)
            {
                float e = EaseInQuad(t / DurationClose);
                if (sheetRect    != null) sheetRect.localScale = Vector3.one * Mathf.Lerp(s0, 0.85f, e);
                if (sheetGroup   != null) sheetGroup.alpha     = Mathf.Lerp(a0, 0f, e);
                if (overlayGroup != null) overlayGroup.alpha   = Mathf.Lerp(o0, 0f, e);
                yield return null;
            }

            panel?.SetActive(false);
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        private static float EaseInQuad(float t)   => Mathf.Clamp01(t) * Mathf.Clamp01(t);
    }
}
