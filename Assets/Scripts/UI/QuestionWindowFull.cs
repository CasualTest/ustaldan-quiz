using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Utils;

namespace UstAldanQuiz.UI
{
    public class QuestionWindowFull : MonoBehaviour
    {
        [Header("Панель")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button     btnClose;

        [Header("Заголовок")]
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
        public TMP_Text resultFeedback;
        public Button   btnContinue;
        public FactPopup factPopup;

        public event Action OnClosed;

        private Coroutine _spinRoutine;

        // ── Unity lifecycle ────────────────────────────────────────────────
        private void Start()
        {
            btnClose?.onClick.AddListener(Close);
            btnContinue?.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            btnClose?.onClick.RemoveAllListeners();
            btnContinue?.onClick.RemoveAllListeners();
        }

        // ── Публичный API ─────────────────────────────────────────────────
        public void Open()
        {
            if (panel != null) panel.SetActive(true);
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            OnClosed?.Invoke();
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

        // ── Helpers ───────────────────────────────────────────────────────
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
    }
}
