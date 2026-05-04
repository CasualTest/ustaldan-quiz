using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("База вопросов")]
        [SerializeField] private QuestionDatabase questionDatabase;

        [Header("Грид категорий")]
        [SerializeField] private Transform          categoryGrid;
        [SerializeField] private CategoryButtonUI   categoryButtonPrefab;

        [Header("Кнопки навигации")]
        [SerializeField] private Button btnPlay;
        [SerializeField] private Button btnSettings;
        [SerializeField] private Button btnRecords;
        [SerializeField] private Button btnAbout;

        [Header("Настройки")]
        [SerializeField] private SettingsUI settingsUI;

        [Header("О приложении")]
        [SerializeField] private AboutUI aboutUI;

        [Header("Статистика")]
        [SerializeField] private TMP_Text statsText;

        [Header("Попап — нет вопросов")]
        [SerializeField] private GameObject noQuestionsPopup;
        [SerializeField] private TMP_Text   noQuestionsPopupText;
        [SerializeField] private Button     btnClosePopup;

        private readonly List<CategoryButtonUI> _spawnedButtons = new List<CategoryButtonUI>();
        private CategoryButtonUI  _selectedButton;
        private QuestionCategory  _selectedCategory;

        private void Start()
        {
            btnPlay?.onClick.AddListener(HandlePlay);
            btnSettings?.onClick.AddListener(() => settingsUI?.Open());
            btnAbout?.onClick.AddListener(() => aboutUI?.Open());
            btnClosePopup?.onClick.AddListener(CloseNoQuestionsPopup);
            if (noQuestionsPopup != null) noQuestionsPopup.SetActive(false);

            SpawnCategoryButtons();

            // Восстановить последнюю выбранную категорию
            string lastId = SaveManager.LastCategory;
            CategoryButtonUI toSelect = _spawnedButtons.Count > 0 ? _spawnedButtons[0] : null;
            foreach (var btn in _spawnedButtons)
                if (btn.Category?.categoryId == lastId) { toSelect = btn; break; }
            if (toSelect != null) HandleCategoryButtonClick(toSelect);
        }

        private void OnDestroy()
        {
            btnPlay?.onClick.RemoveAllListeners();
            btnSettings?.onClick.RemoveAllListeners();
            btnAbout?.onClick.RemoveAllListeners();
            btnClosePopup?.onClick.RemoveAllListeners();
            foreach (var btn in _spawnedButtons)
                if (btn != null) btn.OnClicked -= HandleCategoryButtonClick;
        }

        private void SpawnCategoryButtons()
        {
            if (categoryGrid == null || categoryButtonPrefab == null || questionDatabase == null) return;

            foreach (Transform child in categoryGrid) Destroy(child.gameObject);
            _spawnedButtons.Clear();

            foreach (var category in questionDatabase.categories)
            {
                if (category == null) continue;
                var btn = Instantiate(categoryButtonPrefab, categoryGrid);
                btn.Setup(category);
                btn.OnClicked += HandleCategoryButtonClick;
                _spawnedButtons.Add(btn);
            }
        }

        private void HandleCategoryButtonClick(CategoryButtonUI clickedBtn)
        {
            _selectedButton?.SetSelected(false);
            _selectedButton   = clickedBtn;
            _selectedCategory = clickedBtn.Category;
            _selectedButton.SetSelected(true);
            RefreshStats();
        }

        private void HandlePlay()
        {
            if (_selectedCategory == null)
            {
                Debug.LogWarning("[MainMenuUI] Категория не выбрана.");
                return;
            }
            if (questionDatabase == null)
            {
                Debug.LogWarning("[MainMenuUI] QuestionDatabase не назначена.");
                return;
            }
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[MainMenuUI] GameManager не найден в сцене.");
                return;
            }

            var questions = questionDatabase.GetQuestionsByCategory(_selectedCategory);
            if (questions.Count == 0)
            {
                ShowNoQuestionsPopup(_selectedCategory.displayName);
                return;
            }

            GameManager.Instance.PrepareSession(_selectedCategory, questionDatabase);
            GameManager.Instance.LoadScene("QuestionMap");
        }

        private void ShowNoQuestionsPopup(string categoryName)
        {
            if (noQuestionsPopupText != null)
                noQuestionsPopupText.text = LocaleManager.Get("no_questions_message", categoryName);
            if (noQuestionsPopup != null)
                noQuestionsPopup.SetActive(true);
        }

        private void CloseNoQuestionsPopup()
        {
            if (noQuestionsPopup != null)
                noQuestionsPopup.SetActive(false);
        }

        private void RefreshStats()
        {
            if (statsText == null) return;
            string catId = _selectedCategory?.categoryId ?? "";
            int best   = SaveManager.GetBestScore(catId);
            int played = SaveManager.TotalPlayed;
            int total  = questionDatabase != null && _selectedCategory != null
                ? questionDatabase.GetQuestionsByCategory(_selectedCategory).Count
                : 0;
            statsText.text = LocaleManager.Get("stats_format", played, best, total);
        }
    }
}
