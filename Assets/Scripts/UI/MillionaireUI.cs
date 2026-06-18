using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;
using UstAldanQuiz.Utils;

namespace UstAldanQuiz.UI
{
    public class MillionaireUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Button   btnBack;

        [Header("Окно вопроса")]
        [SerializeField] private QuestionWindow questionWindowFull;

        [Header("Wow-эффект")]
        [SerializeField] private GameObject  wowPopup;
        [SerializeField] private CanvasGroup wowGroup;
        [SerializeField] private RectTransform wowSheet;
        [SerializeField] private TMP_Text     wowText;

        [Header("Цвета ответов")]
        [SerializeField] private Color colorDefault = Color.white;
        [SerializeField] private Color colorCorrect = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color colorWrong   = new Color(0.96f, 0.26f, 0.21f);

        private int   _currentIndex;
        private int   _correctCount;
        private int[] _shuffledIndices;
        private bool  _finished;
        private bool  _answeredCurrent;
        private bool  _exiting;
        private float _questionStartTime;

        private static readonly string[] Prefixes = { "A", "B", "C", "D" };

        private void Start()
        {
            btnBack?.onClick.AddListener(GoToMainMenu);
            if (questionWindowFull != null) questionWindowFull.OnClosed += HandleWindowClosed;

            var gm = GameManager.Instance;
            if (gm == null || gm.SessionQuestions == null || gm.SessionQuestions.Count == 0)
            {
                Debug.LogWarning("[MillionaireUI] Нет данных сессии в GameManager.");
                return;
            }

            if (titleText != null)
                titleText.text = LocaleManager.Get("mode_millionaire");

            if (wowPopup != null) wowPopup.SetActive(false);

            _currentIndex = 0;
            _correctCount = 0;
            UpdateProgress();
            ShowCurrentQuestion();
        }

        private void OnDestroy()
        {
            btnBack?.onClick.RemoveAllListeners();
            if (questionWindowFull != null) questionWindowFull.OnClosed -= HandleWindowClosed;
        }

        // ── Вопрос ────────────────────────────────────────────────────────

        private void ShowCurrentQuestion()
        {
            var gm = GameManager.Instance;
            if (gm == null || _currentIndex >= gm.SessionQuestions.Count) { FinishGame(); return; }
            if (questionWindowFull == null) return;

            _answeredCurrent   = false;
            _questionStartTime = Time.unscaledTime;

            var q = gm.SessionQuestions[_currentIndex];

            _shuffledIndices = new[] { 0, 1, 2, 3 };
            ShuffleArray(_shuffledIndices);

            if (questionWindowFull.questionText != null)
                questionWindowFull.questionText.text = GetLocalizedQuestion(q);

            questionWindowFull.ShowImage(q.imageUrl);

            questionWindowFull.SetHeader(
                LocaleManager.Get("mode_millionaire"),
                progressText != null ? progressText.text : "");

            var btns   = questionWindowFull.answerButtons;
            var labels = questionWindowFull.answerLabels;
            for (int i = 0; i < btns.Length; i++)
            {
                if (btns[i] == null) continue;
                int captured = i;
                labels[i].text         = $"{Prefixes[i]}: {q.answers[_shuffledIndices[i]]}";
                btns[i].image.color    = colorDefault;
                btns[i].interactable   = true;
                btns[i].onClick.RemoveAllListeners();
                btns[i].onClick.AddListener(() => HandleAnswer(captured));
            }

            questionWindowFull.btnContinue?.gameObject.SetActive(false);
            if (!questionWindowFull.gameObject.activeInHierarchy || !IsWindowOpen())
                questionWindowFull.Open();
        }

        private bool IsWindowOpen()
        {
            var panel = questionWindowFull != null ? questionWindowFull.transform.Find("Panel") : null;
            return panel != null && panel.gameObject.activeSelf;
        }

        // ── Ответ ─────────────────────────────────────────────────────────

        private void HandleAnswer(int displayedIndex)
        {
            if (questionWindowFull == null) return;

            bool isCorrect      = _shuffledIndices[displayedIndex] == 0;
            int  correctDisplay = Array.IndexOf(_shuffledIndices, 0);

            _answeredCurrent = true;

            var gmLog = GameManager.Instance;
            var qLog  = gmLog != null && _currentIndex < gmLog.SessionQuestions.Count ? gmLog.SessionQuestions[_currentIndex] : null;
            float elapsed = Mathf.Max(0f, Time.unscaledTime - _questionStartTime);
            gmLog?.LogAnswer(qLog, isCorrect, elapsed);

            var btns = questionWindowFull.answerButtons;
            foreach (var btn in btns) if (btn != null) btn.interactable = false;

            if (btns[correctDisplay] != null)
                btns[correctDisplay].image.color = colorCorrect;
            if (!isCorrect && btns[displayedIndex] != null)
                btns[displayedIndex].image.color = colorWrong;

            if (isCorrect)
            {
                _correctCount++;
                if (GameManager.Instance != null) GameManager.Instance.CorrectAnswers = _correctCount;
                AudioManager.Instance?.PlayCorrect();
                HapticManager.Correct();
                UpdateProgress();
                StartCoroutine(WowThenNext());
            }
            else
            {
                AudioManager.Instance?.PlayWrong();
                HapticManager.Wrong();
                _finished = true;
                StartCoroutine(FinishAfterDelay(0.8f));
            }
        }

        private IEnumerator WowThenNext()
        {
            yield return new WaitForSeconds(0.6f);
            yield return ShowWow();
            _currentIndex++;
            var gm = GameManager.Instance;
            if (gm == null || _currentIndex >= gm.SessionQuestions.Count) { FinishGame(); yield break; }
            ShowCurrentQuestion();
        }

        private IEnumerator ShowWow()
        {
            if (wowPopup == null) yield break;

            wowPopup.SetActive(true);
            if (wowText != null) wowText.text = LocaleManager.Get("wow_correct");

            const float fadeIn = 0.18f, hold = 0.55f, fadeOut = 0.18f;

            if (wowSheet != null) wowSheet.localScale = Vector3.zero;
            if (wowGroup != null) wowGroup.alpha = 0f;

            for (float t = 0f; t < fadeIn; t += Time.unscaledDeltaTime)
            {
                float k = t / fadeIn;
                if (wowGroup != null) wowGroup.alpha = k;
                if (wowSheet != null) wowSheet.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.15f, EaseOut(k));
                yield return null;
            }
            if (wowGroup != null) wowGroup.alpha = 1f;
            if (wowSheet != null) wowSheet.localScale = Vector3.one * 1.15f;

            for (float t = 0f; t < 0.12f; t += Time.unscaledDeltaTime)
            {
                if (wowSheet != null) wowSheet.localScale = Vector3.one * Mathf.Lerp(1.15f, 1.0f, t / 0.12f);
                yield return null;
            }
            if (wowSheet != null) wowSheet.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(hold);

            for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime)
            {
                if (wowGroup != null) wowGroup.alpha = 1f - (t / fadeOut);
                yield return null;
            }
            if (wowGroup != null) wowGroup.alpha = 0f;
            wowPopup.SetActive(false);
        }

        private IEnumerator FinishAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            FinishGame();
        }

        // ── Закрытие окна ─────────────────────────────────────────────────

        private void HandleWindowClosed()
        {
            if (_exiting || _finished) return;
            GoToMainMenu();
        }

        private static float EaseOut(float x) => 1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);

        // ── Завершение ────────────────────────────────────────────────────

        private void FinishGame()
        {
            if (_exiting) return;
            _exiting = true;
            var gm = GameManager.Instance;
            int total = gm != null ? gm.SessionQuestions.Count : GameManager.MillionaireQuestionCount;
            SaveManager.AddGameResult(_correctCount, total);
            if (gm != null) gm.LoadScene("Results");
            else SceneTransition.Instance?.LoadScene("MainMenu");
        }

        private void GoToMainMenu()
        {
            if (_exiting) return;
            _exiting = true;
            var gm = GameManager.Instance;
            if (gm != null) gm.LoadScene("MainMenu");
            else SceneTransition.Instance?.LoadScene("MainMenu");
        }

        private void UpdateProgress()
        {
            if (progressText != null)
            {
                int total = GameManager.Instance?.SessionQuestions?.Count ?? GameManager.MillionaireQuestionCount;
                progressText.text = LocaleManager.Get("score_format", _correctCount, total);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static string GetLocalizedQuestion(QuestionData q)
        {
            bool useSah = LocaleManager.CurrentLanguage == LocaleManager.LangSah;
            if (useSah && !string.IsNullOrWhiteSpace(q.questionTextSah)) return q.questionTextSah;
            return q.questionText;
        }

        private static void ShuffleArray(int[] arr)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}
