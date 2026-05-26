using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;
using UstAldanQuiz.Utils;

namespace UstAldanQuiz.UI
{
    public class QuestionMapUI : MonoBehaviour
    {
        [Header("Карта вопросов")]
        [SerializeField] private QuestionTileUI tilePrefab;
        [SerializeField] private Transform      mapContent;

        [Header("Header")]
        [SerializeField] private TMP_Text categoryNameText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Button   btnBack;

        [Header("Панель вопроса")]
        [SerializeField] private QuestionWindow     questionWindow;
        [SerializeField] private QuestionWindowFull questionWindowFull;

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
        private int _lockedCount;
        private int[] _shuffledIndices;

        private int NewQuestionsTotal => _tiles.Count - _lockedCount;

        private static readonly string[] Prefixes = { "A", "B", "C", "D" };

        private bool        UseFullWindow  => questionWindowFull != null;
        private TMP_Text    WQuestionText  => UseFullWindow ? questionWindowFull.questionText  : questionWindow?.questionText;
        private Button[]    WAnswerBtns    => UseFullWindow ? questionWindowFull.answerButtons : questionWindow?.answerButtons;
        private TMP_Text[]  WAnswerLabels  => UseFullWindow ? questionWindowFull.answerLabels  : questionWindow?.answerLabels;
        private TMP_Text    WFeedback      => UseFullWindow ? questionWindowFull.resultFeedback : questionWindow?.resultFeedback;
        private Button      WBtnContinue   => UseFullWindow ? questionWindowFull.btnContinue   : questionWindow?.btnContinue;
        private FactPopup   WFactPopup     => UseFullWindow ? questionWindowFull.factPopup      : questionWindow?.factPopup;

        private void WOpen()  { if (UseFullWindow) questionWindowFull.Open();  else questionWindow?.Open(); }
        private void WClose() { if (UseFullWindow) questionWindowFull.Close(); else questionWindow?.Close(); }
        private void WShowImage(string url) { if (UseFullWindow) questionWindowFull.ShowImage(url); else questionWindow?.ShowImage(url); }

        private void Start()
        {
            btnBack?.onClick.AddListener(GoToMainMenu);
            btnFinish?.onClick.AddListener(GoToResults);

            if (questionWindowFull != null) questionWindowFull.OnClosed += HandleWindowClosed;
            else if (questionWindow != null) questionWindow.OnClosed   += HandleWindowClosed;

            if (btnFinish != null) btnFinish.gameObject.SetActive(false);

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
            btnFinish?.onClick.RemoveAllListeners();
            if (questionWindowFull != null) questionWindowFull.OnClosed -= HandleWindowClosed;
            else if (questionWindow != null) questionWindow.OnClosed   -= HandleWindowClosed;
        }

        // ── Плитки ────────────────────────────────────────────────────────

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
                    tile.SetState(prev.Value ? TileState.Correct : TileState.Wrong, animate: false);
                    _lockedCount++;
                }
            }
        }

        // ── Вопрос ────────────────────────────────────────────────────────

        private void HandleTileClick(QuestionTileUI tile)
        {
            _activeTile = tile;
            tile.SetState(TileState.Active);
            ShowQuestion(tile.Question);
        }

        private void ShowQuestion(QuestionData q)
        {
            if (!UseFullWindow && questionWindow == null) return;

            _shuffledIndices = new[] { 0, 1, 2, 3 };
            ShuffleArray(_shuffledIndices);

            if (WQuestionText != null)
                WQuestionText.text = GetLocalizedQuestion(q);

            WShowImage(q.imageUrl);

            if (UseFullWindow)
                questionWindowFull.SetHeader(
                    GameManager.Instance?.SelectedCategory?.displayName ?? "",
                    scoreText != null ? scoreText.text : "");

            var btns   = WAnswerBtns;
            var labels = WAnswerLabels;
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

            WFeedback?.gameObject.SetActive(false);
            WBtnContinue?.gameObject.SetActive(false);

            WOpen();
        }

        // ── Ответ ─────────────────────────────────────────────────────────

        private void HandleAnswer(int displayedIndex)
        {
            if (!UseFullWindow && questionWindow == null) return;

            bool isCorrect      = _shuffledIndices[displayedIndex] == 0;
            int  correctDisplay = Array.IndexOf(_shuffledIndices, 0);

            var btns = WAnswerBtns;
            foreach (var btn in btns) if (btn != null) btn.interactable = false;

            if (btns[correctDisplay] != null)
                btns[correctDisplay].image.color = colorCorrect;
            if (!isCorrect && btns[displayedIndex] != null)
                btns[displayedIndex].image.color = colorWrong;

            var feedback = WFeedback;
            if (feedback != null)
            {
                feedback.gameObject.SetActive(true);
                feedback.text  = LocaleManager.Get(isCorrect ? "question_correct" : "question_wrong");
                feedback.color = isCorrect ? colorCorrect : colorWrong;
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
                ? qd.factAfterSah : qd?.factAfterRu;

            if (!isCorrect && !string.IsNullOrWhiteSpace(fact) && WFactPopup != null)
                StartCoroutine(ShowFactAfterDelay(fact, 0.8f));
            else
                StartCoroutine(ShowContinueAfterDelay(1.5f));
        }

        private IEnumerator ShowContinueAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            WBtnContinue?.gameObject.SetActive(true);
        }

        private IEnumerator ShowFactAfterDelay(string fact, float delay)
        {
            yield return new WaitForSeconds(delay);
            WFactPopup?.Show(fact, onClosed: () => WBtnContinue?.gameObject.SetActive(true));
        }

        private void HandleWindowClosed()
        {
            if (_activeTile?.State == TileState.Active)
                _activeTile.SetState(TileState.Closed);
            _activeTile = null;
            if (_answeredCount >= NewQuestionsTotal)
                StartCoroutine(FinishAfterDelay(0.8f));
        }

        // ── Завершение ────────────────────────────────────────────────────

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
