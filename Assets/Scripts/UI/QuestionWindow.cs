using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Utils;

namespace UstAldanQuiz.UI
{
    public class QuestionWindow : MonoBehaviour
    {
        [Header("Панель")]
        [SerializeField] private GameObject  panel;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Button      btnClose;

        [Header("Заголовок (необязательно)")]
        [SerializeField] private TMP_Text headerTitle;
        [SerializeField] private TMP_Text headerScore;

        [Header("Вопрос")]
        public TMP_Text questionText;

        [Header("Медиа")]
        public GameObject        mediaZone;
        public RawImage          questionImage;
        public AspectRatioFitter imageAspectFitter;
        public Image             spinnerImage;

        [Header("Ответы")]
        public Button[]   answerButtons = new Button[4];
        public TMP_Text[] answerLabels  = new TMP_Text[4];

        [Header("Результат")]
        public Button    btnContinue;
        public FactPopup factPopup;

        public event Action OnClosed;
        public event Action OnCloseRequested;

        private Coroutine _anim;
        private Coroutine _spinRoutine;

        private const float DurationOpen  = 0.20f;
        private const float DurationClose = 0.15f;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            btnClose?.onClick.AddListener(HandleCloseClick);
            btnContinue?.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
        }

        private void HandleCloseClick()
        {
            OnCloseRequested?.Invoke();
            Close();
        }

        private void OnDestroy()
        {
            btnClose?.onClick.RemoveAllListeners();
            btnContinue?.onClick.RemoveAllListeners();
        }

        // ── Публичный API ──────────────────────────────────────────────────────

        public void Open()
        {
            panel?.SetActive(true);
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            if (panel != null && !panel.activeSelf) return;
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateClose());
        }

        public void SetHeader(string title, string score)
        {
            if (headerTitle != null) headerTitle.text = title;
            if (headerScore != null) headerScore.text = score;
        }

        public void ShowImage(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                if (mediaZone != null) mediaZone.SetActive(false);
                return;
            }
            if (mediaZone != null) mediaZone.SetActive(true);
            if (questionImage != null) questionImage.gameObject.SetActive(false);
            ShowSpinner(true);

            StartCoroutine(ImageLoader.Load(url, tex =>
            {
                ShowSpinner(false);
                if (tex == null) { if (mediaZone != null) mediaZone.SetActive(false); return; }
                if (questionImage != null)
                {
                    questionImage.gameObject.SetActive(true);
                    questionImage.texture = tex;
                    if (imageAspectFitter != null)
                        imageAspectFitter.aspectRatio = (float)tex.width / tex.height;
                }
            }));
        }

        // ── Анимации ───────────────────────────────────────────────────────────

        private IEnumerator AnimateOpen()
        {
            if (panelGroup != null) panelGroup.alpha = 0f;
            for (float t = 0f; t < DurationOpen; t += Time.unscaledDeltaTime)
            {
                if (panelGroup != null)
                    panelGroup.alpha = EaseOutCubic(t / DurationOpen);
                yield return null;
            }
            if (panelGroup != null) panelGroup.alpha = 1f;
        }

        private IEnumerator AnimateClose()
        {
            float a0 = panelGroup != null ? panelGroup.alpha : 1f;
            for (float t = 0f; t < DurationClose; t += Time.unscaledDeltaTime)
            {
                if (panelGroup != null)
                    panelGroup.alpha = Mathf.Lerp(a0, 0f, t / DurationClose);
                yield return null;
            }
            if (panelGroup != null) panelGroup.alpha = 0f;
            panel?.SetActive(false);
            OnClosed?.Invoke();
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void ShowSpinner(bool show)
        {
            if (spinnerImage == null) return;
            spinnerImage.gameObject.SetActive(show);
            if (show)
            {
                spinnerImage.transform.localRotation = Quaternion.identity;
                _spinRoutine = StartCoroutine(SpinLoop());
            }
            else if (_spinRoutine != null)
            {
                StopCoroutine(_spinRoutine);
                _spinRoutine = null;
            }
        }

        private IEnumerator SpinLoop()
        {
            while (true)
            {
                spinnerImage.transform.Rotate(0f, 0f, -360f * Time.deltaTime);
                yield return null;
            }
        }

        private static float EaseOutCubic(float x) =>
            1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);
    }
}
