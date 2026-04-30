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

        [Header("Цвета ответов")]
        [SerializeField] private Color colorDefault = Color.white;
        [SerializeField] private Color colorCorrect = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color colorWrong   = new Color(0.96f, 0.26f, 0.21f);

        private readonly List<QuestionTileUI> _tiles = new List<QuestionTileUI>();
        private QuestionTileUI _activeTile;
        private int _answeredCount;
        private int _correctCount;
        private int[] _shuffledIndices;

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

            for (int i = 0; i < questions.Count; i++)
            {
                var tile = Instantiate(tilePrefab, mapContent);
                tile.Setup(questions[i], i + 1);
                tile.OnTileClicked += HandleTileClick;
                _tiles.Add(tile);
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

        private void ShowQuestion(QuestionData q)
        {
            // Перемешать индексы ответов
            _shuffledIndices = new[] { 0, 1, 2, 3 };
            ShuffleArray(_shuffledIndices);

            if (questionText != null) questionText.text = q.questionText;

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
                resultFeedback.text  = isCorrect ? "Правильно! ✓" : "Неверно ✗";
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
            UpdateScore();

            StartCoroutine(ShowContinueAfterDelay(1.5f));
        }

        private IEnumerator ShowContinueAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (btnContinue != null) btnContinue.gameObject.SetActive(true);
        }

        private void CloseQuestionPanel()
        {
            if (questionPanel != null) questionPanel.SetActive(false);
            _activeTile = null;

            if (_answeredCount >= _tiles.Count)
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
            else SceneManager.LoadScene("MainMenu");
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
            else SceneManager.LoadScene("MainMenu");
        }

        private void UpdateScore()
        {
            if (scoreText != null)
                scoreText.text = $"Правильных: {_correctCount} / {_tiles.Count}";
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
