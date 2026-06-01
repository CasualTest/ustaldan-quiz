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

        [Header("Цвета ответов")]
        [SerializeField] private Color colorDefault = Color.white;
        [SerializeField] private Color colorCorrect = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color colorWrong   = new Color(0.96f, 0.26f, 0.21f);

        private int   _currentIndex;
        private int   _correctCount;
        private int[] _shuffledIndices;
        private bool  _finished;

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
            questionWindowFull.Open();
        }

        // ── Ответ ─────────────────────────────────────────────────────────

        private void HandleAnswer(int displayedIndex)
        {
            if (questionWindowFull == null) return;

            bool isCorrect      = _shuffledIndices[displayedIndex] == 0;
            int  correctDisplay = Array.IndexOf(_shuffledIndices, 0);

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
            }
            else
            {
                AudioManager.Instance?.PlayWrong();
                HapticManager.Wrong();
            }

            UpdateProgress();

            var gm = GameManager.Instance;
            var q  = gm != null && _currentIndex < gm.SessionQuestions.Count ? gm.SessionQuestions[_currentIndex] : null;
            bool   sah  = LocaleManager.CurrentLanguage == LocaleManager.LangSah;
            string fact = sah && !string.IsNullOrWhiteSpace(q?.factAfterSah) ? q.factAfterSah : q?.factAfterRu;

            if (!isCorrect && !string.IsNullOrWhiteSpace(fact) && questionWindowFull.factPopup != null)
                StartCoroutine(ShowFactAfterDelay(fact, 0.8f));
            else
                StartCoroutine(ShowContinueAfterDelay(1.5f));

            if (!isCorrect) _finished = true;
        }

        private IEnumerator ShowContinueAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            questionWindowFull?.btnContinue?.gameObject.SetActive(true);
        }

        private IEnumerator ShowFactAfterDelay(string fact, float delay)
        {
            yield return new WaitForSeconds(delay);
            questionWindowFull?.factPopup?.Show(fact,
                onClosed: () => questionWindowFull?.btnContinue?.gameObject.SetActive(true));
        }

        // ── Закрытие окна ─────────────────────────────────────────────────

        private void HandleWindowClosed()
        {
            if (_finished) { FinishGame(); return; }

            _currentIndex++;
            var gm = GameManager.Instance;
            if (gm == null || _currentIndex >= gm.SessionQuestions.Count) { FinishGame(); return; }

            ShowCurrentQuestion();
        }

        // ── Завершение ────────────────────────────────────────────────────

        private void FinishGame()
        {
            var gm = GameManager.Instance;
            int total = gm != null ? gm.SessionQuestions.Count : GameManager.MillionaireQuestionCount;
            SaveManager.AddGameResult(_correctCount, total);
            if (gm != null) gm.LoadScene("Results");
            else SceneTransition.Instance?.LoadScene("MainMenu");
        }

        private void GoToMainMenu()
        {
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
