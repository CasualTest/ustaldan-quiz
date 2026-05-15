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
    /// <summary>
    /// Сцена Roadmap — змейка из вопросов (как Quizzland).
    /// Карта генерируется один раз и хранится в PlayerPrefs до прохождения всех вопросов.
    /// </summary>
    public class RoadmapUI : MonoBehaviour
    {
        [Header("Данные")]
        [SerializeField] private QuestionDatabase questionDatabase;

        [Header("Карта")]
        [SerializeField] private RoadmapTileUI tilePrefab;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private RectTransform linesContainer;

        [Header("Прогресс")]
        [SerializeField] private RectTransform progressBarFill;
        [SerializeField] private TMP_Text      progressText;

        [Header("Хедер")]
        [SerializeField] private Button   btnBack;
        [SerializeField] private Button   btnReset;
        [SerializeField] private Button   btnFinish;

        [Header("Панель вопроса")]
        [SerializeField] private GameObject    questionPanel;
        [SerializeField] private RectTransform questionCard;
        [SerializeField] private CanvasGroup   questionCardGroup;
        [SerializeField] private TMP_Text      questionText;
        [SerializeField] private Image         questionImage;
        [SerializeField] private GameObject    questionImageContainer;
        [SerializeField] private Button[]      answerButtons = new Button[4];
        [SerializeField] private TMP_Text[]    answerLabels  = new TMP_Text[4];
        [SerializeField] private TMP_Text      resultFeedback;
        [SerializeField] private Button        btnContinue;
        [SerializeField] private FactPopup     factPopup;

        [Header("Цвета")]
        [SerializeField] private Color colorDefault = Color.white;
        [SerializeField] private Color colorCorrect = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color colorWrong   = new Color(0.96f, 0.26f, 0.21f);
        [SerializeField] private Color lineColor    = new Color(0.55f, 0.50f, 0.42f, 0.70f);

        // Must match RoadmapManager.TileSize
        private const float TileSize      = RoadmapManager.TileSize;
        private const float LineThickness = 10f;

        private readonly List<RoadmapTileUI>  _tiles     = new List<RoadmapTileUI>();
        private          List<QuestionData>   _questions = new List<QuestionData>();
        private          RoadmapSaveData      _layout;
        private          RoadmapTileUI        _activeTile;
        private          int                  _correctCount;
        private          int                  _answeredCount;
        private          int                  _lockedCount;
        private          int[]                _shuffledIndices;

        private int NewQuestionsTotal => _tiles.Count - _lockedCount;

        private static readonly string[] Prefixes = { "A", "B", "C", "D" };

        // ── Unity lifecycle ────────────────────────────────────────────────

        private void Start()
        {
            btnBack?.onClick.AddListener(GoToMainMenu);
            btnReset?.onClick.AddListener(ResetProgress);
            btnContinue?.onClick.AddListener(CloseQuestionPanel);
            btnFinish?.onClick.AddListener(GoToMainMenu);

            if (questionPanel != null) questionPanel.SetActive(false);
            if (btnFinish     != null) btnFinish.gameObject.SetActive(false);

            if (questionDatabase == null)
            {
                Debug.LogWarning("[RoadmapUI] questionDatabase не назначена.");
                return;
            }

            _questions = new List<QuestionData>(questionDatabase.allQuestions);
            if (_questions.Count == 0)
            {
                Debug.LogWarning("[RoadmapUI] Нет вопросов в базе данных.");
                return;
            }

            if (AllQuestionsAnswered())
            {
                foreach (var cat in questionDatabase.categories)
                    SaveManager.ClearQuestionProgress(cat.categoryId);
                RoadmapManager.Clear();
            }

            _layout = RoadmapManager.Load();
            if (_layout == null || _layout.nodes.Count != _questions.Count)
            {
                _layout = RoadmapManager.Generate(_questions);
                RoadmapManager.Save(_layout);
            }

            SpawnMap();
            UpdateProgress();

            if (NewQuestionsTotal == 0 && btnFinish != null)
                btnFinish.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            btnBack?.onClick.RemoveAllListeners();
            btnReset?.onClick.RemoveAllListeners();
            btnContinue?.onClick.RemoveAllListeners();
            btnFinish?.onClick.RemoveAllListeners();
            foreach (var btn in answerButtons) btn?.onClick.RemoveAllListeners();
        }

        // ── Map building ───────────────────────────────────────────────────

        private void SpawnMap()
        {
            foreach (Transform t in mapContent) Destroy(t.gameObject);
            _tiles.Clear();
            _lockedCount = 0;

            // Content height based on number of rows
            int rows = Mathf.CeilToInt((float)_layout.nodes.Count / RoadmapManager.Cols);
            float contentHeight = RoadmapManager.TopMargin
                                + rows * (TileSize + RoadmapManager.VGap)
                                + 80f;
            mapContent.sizeDelta = new Vector2(1080f, contentHeight);

            // Lines container: stretch over content
            if (linesContainer != null)
            {
                linesContainer.anchorMin = Vector2.zero;
                linesContainer.anchorMax = Vector2.one;
                linesContainer.offsetMin = linesContainer.offsetMax = Vector2.zero;
                linesContainer.SetAsFirstSibling();
            }

            // Spawn tiles — match by index (nodes are generated in the same order as _questions)
            for (int i = 0; i < _layout.nodes.Count; i++)
            {
                var node = _layout.nodes[i];
                var q    = i < _questions.Count ? _questions[i] : null;
                if (q == null) continue;

                var tile   = Instantiate(tilePrefab, mapContent);
                var tileRT = tile.GetComponent<RectTransform>();
                tileRT.anchorMin        = new Vector2(0, 1);
                tileRT.anchorMax        = new Vector2(0, 1);
                tileRT.pivot            = new Vector2(0.5f, 0.5f);
                tileRT.anchoredPosition = new Vector2(node.x, node.y);
                tileRT.sizeDelta        = new Vector2(TileSize, TileSize);

                tile.Setup(q, i);
                tile.OnTileClicked += HandleTileClick;
                _tiles.Add(tile);

                string catId = q.category?.categoryId ?? "";
                var prev = SaveManager.GetQuestionResult(catId, q.name);
                if (prev.HasValue)
                {
                    tile.SetState(prev.Value ? TileState.Correct : TileState.Wrong);
                    _lockedCount++;
                }
            }

            DrawLines();
        }

        private void DrawLines()
        {
            var parent  = linesContainer != null ? linesContainer : mapContent;
            var drawn   = new HashSet<int>();

            for (int i = 0; i < _layout.nodes.Count; i++)
            {
                var nodeA = _layout.nodes[i];
                foreach (int j in nodeA.edges)
                {
                    if (j < 0 || j >= _layout.nodes.Count) continue;
                    int key = Mathf.Min(i, j) * 10000 + Mathf.Max(i, j);
                    if (!drawn.Add(key)) continue;

                    var nodeB = _layout.nodes[j];
                    var line  = new GameObject("Line", typeof(RectTransform));
                    line.transform.SetParent(parent, false);

                    line.AddComponent<Image>().color = lineColor;

                    var rt   = line.GetComponent<RectTransform>();
                    var posA = new Vector2(nodeA.x, nodeA.y);
                    var posB = new Vector2(nodeB.x, nodeB.y);
                    var mid  = (posA + posB) * 0.5f;
                    float dist  = Vector2.Distance(posA, posB);
                    float angle = Mathf.Atan2(posB.y - posA.y, posB.x - posA.x) * Mathf.Rad2Deg;

                    rt.anchorMin        = new Vector2(0, 1);
                    rt.anchorMax        = new Vector2(0, 1);
                    rt.pivot            = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = mid;
                    rt.sizeDelta        = new Vector2(dist, LineThickness);
                    rt.localRotation    = Quaternion.Euler(0, 0, angle);
                }
            }
        }

        // ── Progress ───────────────────────────────────────────────────────

        private void UpdateProgress()
        {
            int total    = _tiles.Count;
            int answered = _lockedCount + _answeredCount;

            if (progressText != null)
                progressText.text = $"{answered} / {total}";

            if (progressBarFill != null)
            {
                var parentRT = (RectTransform)progressBarFill.parent;
                float w = parentRT != null ? parentRT.rect.width : 800f;
                if (w <= 0f) w = 800f;
                float ratio = total > 0 ? Mathf.Clamp01((float)answered / total) : 0f;
                progressBarFill.sizeDelta = new Vector2(w * ratio, progressBarFill.sizeDelta.y);
            }
        }

        // ── Question popup ─────────────────────────────────────────────────

        private void HandleTileClick(RoadmapTileUI tile)
        {
            _activeTile = tile;
            tile.SetState(TileState.Active);
            ShowQuestion(tile.Question);
        }

        private void ShowQuestion(QuestionData q)
        {
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
                answerLabels[i].text          = $"{Prefixes[i]}: {q.answers[_shuffledIndices[i]]}";
                answerButtons[i].image.color  = colorDefault;
                answerButtons[i].interactable = true;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => HandleAnswer(captured));
            }

            if (resultFeedback != null) resultFeedback.gameObject.SetActive(false);
            if (btnContinue    != null) btnContinue.gameObject.SetActive(false);

            if (questionPanel != null) questionPanel.SetActive(true);
            StartCoroutine(AnimateCardIn());
        }

        private void HandleAnswer(int displayedIndex)
        {
            bool isCorrect      = _shuffledIndices[displayedIndex] == 0;
            int  correctDisplay = Array.IndexOf(_shuffledIndices, 0);

            foreach (var btn in answerButtons) if (btn != null) btn.interactable = false;

            if (answerButtons[correctDisplay] != null)
                answerButtons[correctDisplay].image.color = colorCorrect;
            if (!isCorrect && answerButtons[displayedIndex] != null)
                answerButtons[displayedIndex].image.color = colorWrong;

            if (resultFeedback != null)
            {
                resultFeedback.gameObject.SetActive(true);
                resultFeedback.text  = LocaleManager.Get(isCorrect ? "question_correct" : "question_wrong");
                resultFeedback.color = isCorrect ? colorCorrect : colorWrong;
            }

            if (isCorrect) { _correctCount++; AudioManager.Instance?.PlayCorrect(); HapticManager.Correct(); }
            else           { AudioManager.Instance?.PlayWrong(); HapticManager.Wrong(); }

            _answeredCount++;
            _activeTile?.SetState(isCorrect ? TileState.Correct : TileState.Wrong);

            string catId = _activeTile?.Question?.category?.categoryId ?? "";
            if (_activeTile != null)
                SaveManager.MarkQuestionAnswered(catId, _activeTile.Question.name, isCorrect);

            UpdateProgress();

            var    qd   = _activeTile?.Question;
            bool   sah  = LocaleManager.CurrentLanguage == LocaleManager.LangSah;
            string fact = sah && !string.IsNullOrWhiteSpace(qd?.factAfterSah)
                ? qd.factAfterSah : qd?.factAfterRu;

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
            factPopup?.Show(fact, onClosed: () =>
            {
                if (btnContinue != null) btnContinue.gameObject.SetActive(true);
            });
        }

        private void CloseQuestionPanel()
        {
            if (questionPanel != null) questionPanel.SetActive(false);
            _activeTile = null;

            if (_answeredCount >= NewQuestionsTotal && btnFinish != null)
                btnFinish.gameObject.SetActive(true);
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private bool AllQuestionsAnswered()
        {
            foreach (var q in _questions)
            {
                var r = SaveManager.GetQuestionResult(q.category?.categoryId ?? "", q.name);
                if (!r.HasValue) return false;
            }
            return true;
        }

        private void GoToMainMenu()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.LoadScene("MainMenu");
            else SceneTransition.Instance?.LoadScene("MainMenu");
        }

        private void ResetProgress()
        {
            foreach (var cat in questionDatabase.categories)
                SaveManager.ClearQuestionProgress(cat.categoryId);
            RoadmapManager.Clear();

            _lockedCount   = 0;
            _answeredCount = 0;
            _correctCount  = 0;

            _layout = RoadmapManager.Generate(_questions);
            RoadmapManager.Save(_layout);

            if (questionPanel != null) questionPanel.SetActive(false);
            if (btnFinish     != null) btnFinish.gameObject.SetActive(false);

            SpawnMap();
            UpdateProgress();
        }

        private static string GetLocalizedQuestion(QuestionData q)
        {
            bool useSah = LocaleManager.CurrentLanguage == LocaleManager.LangSah;
            if (useSah && !string.IsNullOrWhiteSpace(q.questionTextSah)) return q.questionTextSah;
            return q.questionText;
        }

        private IEnumerator AnimateCardIn()
        {
            if (questionCard == null) yield break;
            const float duration = 0.2f;
            questionCard.localScale = Vector3.one * 0.8f;
            if (questionCardGroup != null) questionCardGroup.alpha = 0f;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float p = Mathf.Clamp01(t / duration);
                float e = 1f - Mathf.Pow(1f - p, 3f);
                questionCard.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, e);
                if (questionCardGroup != null) questionCardGroup.alpha = e;
                yield return null;
            }
            questionCard.localScale = Vector3.one;
            if (questionCardGroup != null) questionCardGroup.alpha = 1f;
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
