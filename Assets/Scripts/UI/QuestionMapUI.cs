using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;
using UstAldanQuiz.Utils;

namespace UstAldanQuiz.UI
{
    /// <summary>
    /// Управляет картой вопросов и панелью вопроса поверх карты.
    /// Повесить на объект Manager в сцене QuestionMap.
    /// </summary>
    public class QuestionMapUI : MonoBehaviour
    {
        [Header("Карта вопросов")]
        [SerializeField] private QuestionTileUI tilePrefab;
        [SerializeField] private Transform mapContent;

        [Header("Header")]
        [SerializeField] private TMP_Text categoryNameText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Button btnBack;

        [Header("Панель вопроса")]
        [SerializeField] private GameObject questionPanel;
        [SerializeField] private RectTransform questionCard;
        [SerializeField] private CanvasGroup questionCardGroup;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Image questionImage;
        [SerializeField] private GameObject questionImageContainer;

        [Header("Кнопки ответов (4 штуки, A/B/C/D)")]
        [SerializeField] private Button[] answerButtons = new Button[4];
        [SerializeField] private TMP_Text[] answerLabels = new TMP_Text[4];

        [Header("Обратная связь")]
        [SerializeField] private TMP_Text resultFeedback;
        [SerializeField] private Button btnContinue;

        [Header("Завершение")]
        [SerializeField] private Button btnFinish;

        [Header("Попап факта")]
        [SerializeField] private FactPopup factPopup;

        [Header("Цвета ответов")]
        [SerializeField] private Color colorDefault = Color.white;
        [SerializeField] private Color colorCorrect = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color colorWrong   = new Color(0.96f, 0.26f, 0.21f);

        private readonly List<QuestionTileUI> _tiles = new List<QuestionTileUI>();
        private QuestionTileUI _activeTile;
        private int _answeredCount; // отвечено в этой сессии
        private int _correctCount;  // правильно в этой сессии
        private int _lockedCount;   // заблокировано из предыдущих сессий
        private int[] _shuffledIndices;

        private int NewQuestionsTotal => _tiles.Count - _lockedCount;

        private static readonly string[] Prefixes = { "A", "B", "C", "D" };

        private void Start()
        {
            // Кнопки регистрируются всегда, независимо от наличия сессии
            btnBack?.onClick.AddListener(GoToMainMenu);
            btnContinue?.onClick.AddListener(CloseQuestionPanel);
            btnFinish?.onClick.AddListener(GoToResults);

            if (questionPanel != null) questionPanel.SetActive(false);
            if (btnFinish     != null) btnFinish.gameObject.SetActive(false);

            var gm = GameManager.Instance;
            if (gm == null || gm.SessionQuestions == null || gm.SessionQuestions.Count == 0)
            {
                Debug.LogWarning("[QuestionMapUI] Нет данных сессии в GameManager.");
                return;
            }

            if (categoryNameText != null)
                categoryNameText.text = gm.SelectedCategory?.displayName ?? "";

            SpawnTiles(gm.SessionQuestions);
            UpdateScore();

            if (NewQuestionsTotal == 0)
                StartCoroutine(FinishAfterDelay(0.5f));
        }

        private void OnDestroy()
        {
            btnBack?.onClick.RemoveAllListeners();
            btnContinue?.onClick.RemoveAllListeners();
            btnFinish?.onClick.RemoveAllListeners();
            foreach (var btn in answerButtons) btn?.onClick.RemoveAllListeners();
        }

        // -------------------------------------------------------------------------
        // Создание плиток
        // -------------------------------------------------------------------------

        private void SpawnTiles(List<QuestionData> questions)
        {
            foreach (Transform child in mapContent) Destroy(child.gameObject);
            _tiles.Clear();
            _lockedCount = 0;

            string catId = GameManager.Instance?.SelectedCategory?.categoryId ?? "";

            for (int i = 0; i < questions.Count; i++)
            {
                var tile = Instantiate(tilePrefab, mapContent);
                tile.Setup(questions[i], i + 1);
                tile.OnTileClicked += HandleTileClick;
                _tiles.Add(tile);

                bool? prev = SaveManager.GetQuestionResult(catId, questions[i].name);
                if (prev.HasValue)
                {
                    tile.SetState(prev.Value ? TileState.Correct : TileState.Wrong);
                    _lockedCount++;
                }
            }
        }

        // -------------------------------------------------------------------------
        // Показ вопроса
        // -------------------------------------------------------------------------

        private void HandleTileClick(QuestionTileUI tile)
        {
            _activeTile = tile;
            tile.SetState(TileState.Active);
            ShowQuestion(tile.Question);
        }

        private static string GetLocalizedQuestion(QuestionData q)
        {
            bool useSah = LocaleManager.CurrentLanguage == LocaleManager.LangSah;
            if (useSah && !string.IsNullOrWhiteSpace(q.questionTextSah))
                return q.questionTextSah;
            return q.questionText;
        }

        private void ShowQuestion(QuestionData q)
        {
            // Перемешать индексы ответов
            _shuffledIndices = new[] { 0, 1, 2, 3 };
            ShuffleArray(_shuffledIndices);

            if (questionText != null) questionText.text = GetLocalizedQuestion(q);

            bool hasImage = q.questionImage != null;
            if (questionImageContainer != null) questionImageContainer.SetActive(hasImage);
            if (hasImage && questionImage != null) questionImage.sprite = q.questionImage;

            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null) continue;
                int captured = i;
                answerLabels[i].text        = $"{Prefixes[i]}: {q.answers[_shuffledIndices[i]]}";
                answerButtons[i].image.color = colorDefault;
                answerButtons[i].interactable = true;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => HandleAnswer(captured));
            }

            if (resultFeedback != null) resultFeedback.gameObject.SetActive(false);
            if (btnContinue    != null) btnContinue.gameObject.SetActive(false);

            if (questionPanel != null) questionPanel.SetActive(true);
            StartCoroutine(AnimateCardIn());
        }

        // -------------------------------------------------------------------------
        // Обработка ответа
        // -------------------------------------------------------------------------

        private void HandleAnswer(int displayedIndex)
        {
            bool isCorrect       = _shuffledIndices[displayedIndex] == 0;
            int  correctDisplay  = Array.IndexOf(_shuffledIndices, 0);

            // Заблокировать все кнопки
            foreach (var btn in answerButtons) if (btn != null) btn.interactable = false;

            // Подсветка результата
            if (answerButtons[correctDisplay] != null)
                answerButtons[correctDisplay].image.color = colorCorrect;
            if (!isCorrect && answerButtons[displayedIndex] != null)
                answerButtons[displayedIndex].image.color = colorWrong;

            // Текст обратной связи
            if (resultFeedback != null)
            {
                resultFeedback.gameObject.SetActive(true);
                resultFeedback.text  = LocaleManager.Get(isCorrect ? "question_correct" : "question_wrong");
                resultFeedback.color = isCorrect ? colorCorrect : colorWrong;
            }

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

            _answeredCount++;
            _activeTile?.SetState(isCorrect ? TileState.Correct : TileState.Wrong);

            string catId = GameManager.Instance?.SelectedCategory?.categoryId ?? "";
            if (_activeTile != null)
                SaveManager.MarkQuestionAnswered(catId, _activeTile.Question.name, isCorrect);

            UpdateScore();

            var    qd   = _activeTile?.Question;
            bool   sah  = LocaleManager.CurrentLanguage == LocaleManager.LangSah;
            string fact = sah && !string.IsNullOrWhiteSpace(qd?.factAfterSah)
                ? qd.factAfterSah
                : qd?.factAfterRu;
            if (!isCorrect && !string.IsNullOrWhiteSpace(fact) && factPopup != null)
                StartCoroutine(ShowFactAfterDelay(fact, 0.8f));
            else
                StartCoroutine(ShowContinueAfterDelay(1.5f));
        }

        private IEnumerator ShowContinueAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (btnContinue != null) btnContinue.gameObject.SetActive(true);
        }

        private IEnumerator ShowFactAfterDelay(string fact, float delay)
        {
            yield return new WaitForSeconds(delay);
            factPopup.Show(fact, onClosed: () =>
            {
                if (btnContinue != null) btnContinue.gameObject.SetActive(true);
            });
        }

        private void CloseQuestionPanel()
        {
            if (questionPanel != null) questionPanel.SetActive(false);
            _activeTile = null;

            if (_answeredCount >= NewQuestionsTotal)
                StartCoroutine(FinishAfterDelay(0.8f));
        }

        private IEnumerator FinishAfterDelay(float delay)
        {
            if (btnFinish != null) btnFinish.gameObject.SetActive(true);
            yield return new WaitForSeconds(delay);
            GoToResults();
        }

        private void GoToMainMenu()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.LoadScene("MainMenu");
            else SceneTransition.Instance?.LoadScene("MainMenu");
        }

        private void GoToResults()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                SaveManager.SetBestScore(gm.SelectedCategory?.categoryId ?? "", _correctCount);
                SaveManager.AddGameResult(_correctCount, _tiles.Count);
                gm.LoadScene("Results");
            }
            else SceneTransition.Instance?.LoadScene("MainMenu");
        }

        private void UpdateScore()
        {
            if (scoreText != null)
                scoreText.text = LocaleManager.Get("score_format", _correctCount, _tiles.Count);
        }

        // -------------------------------------------------------------------------
        // Анимации
        // -------------------------------------------------------------------------

        private IEnumerator AnimateCardIn()
        {
            if (questionCard == null) yield break;

            const float duration = 0.2f;
            questionCard.localScale = Vector3.one * 0.8f;
            if (questionCardGroup != null) questionCardGroup.alpha = 0f;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float p = Mathf.Clamp01(t / duration);
                float e = 1f - Mathf.Pow(1f - p, 3f); // ease-out cubic
                questionCard.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, e);
                if (questionCardGroup != null) questionCardGroup.alpha = e;
                yield return null;
            }

            questionCard.localScale = Vector3.one;
            if (questionCardGroup != null) questionCardGroup.alpha = 1f;
        }

        // -------------------------------------------------------------------------
        // Утилиты
        // -------------------------------------------------------------------------

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
