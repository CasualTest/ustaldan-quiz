using System.Collections;
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

        [Header("Страницы")]
        [SerializeField] private GameObject homePage;
        [SerializeField] private GameObject recordsPage;
        [SerializeField] private GameObject settingsPage;
        [SerializeField] private GameObject aboutPage;
        [SerializeField] private GameObject profilePage;

        [Header("Таббар — кнопки")]
        [SerializeField] private Button tabHome;
        [SerializeField] private Button tabRecords;
        [SerializeField] private Button tabSettings;
        [SerializeField] private Button tabAbout;
        [SerializeField] private Button tabProfile;

        [Header("Таббар — иконки")]
        [SerializeField] private Image iconHome;
        [SerializeField] private Image iconRecords;
        [SerializeField] private Image iconSettings;
        [SerializeField] private Image iconAbout;
        [SerializeField] private Image iconProfile;

        [Header("Таббар — подписи")]
        [SerializeField] private TMP_Text labelHome;
        [SerializeField] private TMP_Text labelRecords;
        [SerializeField] private TMP_Text labelSettings;
        [SerializeField] private TMP_Text labelAbout;
        [SerializeField] private TMP_Text labelProfile;

        [Header("Главная — категории и кнопки")]
        [SerializeField] private Transform        categoryGrid;
        [SerializeField] private CategoryButtonUI categoryButtonPrefab;
        [SerializeField] private Button           btnPlay;
        [SerializeField] private Button           btnArcade;
        [SerializeField] private TMP_Text         statsText;

        [Header("Рекорды — контейнер строк")]
        [SerializeField] private Transform recordsContent;

        [Header("Попапы")]
        [SerializeField] private NoQuestionsPopup noQuestionsPopup;

        private readonly List<CategoryButtonUI> _spawnedButtons = new List<CategoryButtonUI>();
        private CategoryButtonUI _selectedButton;
        private QuestionCategory _selectedCategory;
        private int  _currentTab = 0;
        private bool _animating  = false;

        private static readonly Color ColorActive   = new Color(0.18f, 0.38f, 0.25f);
        private static readonly Color ColorInactive = new Color(0.60f, 0.60f, 0.60f);
        private static readonly Color RowText       = new Color(0.10f, 0.16f, 0.10f);
        private static readonly Color RowScore      = new Color(0.18f, 0.38f, 0.25f);
        private static readonly Color RowAlt        = new Color(0.97f, 0.97f, 0.97f);

        private void Start()
        {
            tabHome?.onClick.AddListener(() => SwitchTab(0));
            tabRecords?.onClick.AddListener(() => SwitchTab(1));
            tabSettings?.onClick.AddListener(() => SwitchTab(2));
            tabAbout?.onClick.AddListener(() => SwitchTab(3));
            tabProfile?.onClick.AddListener(() => SwitchTab(4));
            btnPlay?.onClick.AddListener(HandlePlay);
            btnArcade?.onClick.AddListener(HandleArcade);

            SpawnCategoryButtons();
            RefreshRecords();

            string lastId = SaveManager.LastCategory;
            CategoryButtonUI toSelect = _spawnedButtons.Count > 0 ? _spawnedButtons[0] : null;
            foreach (var btn in _spawnedButtons)
                if (btn.Category?.categoryId == lastId) { toSelect = btn; break; }
            if (toSelect != null) HandleCategoryButtonClick(toSelect);

            SwitchTabImmediate(0);

            LocaleManager.OnLanguageChanged += RefreshStats;
            LocaleManager.OnLanguageChanged += RefreshRecords;
        }

        private void OnDestroy()
        {
            tabHome?.onClick.RemoveAllListeners();
            tabRecords?.onClick.RemoveAllListeners();
            tabSettings?.onClick.RemoveAllListeners();
            tabAbout?.onClick.RemoveAllListeners();
            tabProfile?.onClick.RemoveAllListeners();
            btnPlay?.onClick.RemoveAllListeners();
            btnArcade?.onClick.RemoveAllListeners();
            foreach (var btn in _spawnedButtons)
                if (btn != null) btn.OnClicked -= HandleCategoryButtonClick;
            LocaleManager.OnLanguageChanged -= RefreshStats;
            LocaleManager.OnLanguageChanged -= RefreshRecords;
        }

        // ── Переключение вкладок ──────────────────────────────────────────

        private void SwitchTab(int index)
        {
            if (_animating || index == _currentTab) return;

            SetTabColor(iconHome,     labelHome,     index == 0);
            SetTabColor(iconRecords,  labelRecords,  index == 1);
            SetTabColor(iconSettings, labelSettings, index == 2);
            SetTabColor(iconAbout,    labelAbout,    index == 3);
            SetTabColor(iconProfile,  labelProfile,  index == 4);

            int from = _currentTab;
            _currentTab = index;
            StartCoroutine(AnimateTab(from, index));
        }

        private void SwitchTabImmediate(int index)
        {
            _currentTab = index;
            homePage?.SetActive(index == 0);
            recordsPage?.SetActive(index == 1);
            settingsPage?.SetActive(index == 2);
            aboutPage?.SetActive(index == 3);
            profilePage?.SetActive(index == 4);

            SetTabColor(iconHome,     labelHome,     index == 0);
            SetTabColor(iconRecords,  labelRecords,  index == 1);
            SetTabColor(iconSettings, labelSettings, index == 2);
            SetTabColor(iconAbout,    labelAbout,    index == 3);
            SetTabColor(iconProfile,  labelProfile,  index == 4);
        }

        private IEnumerator AnimateTab(int from, int to)
        {
            _animating = true;

            var pages = new[] { homePage, recordsPage, settingsPage, aboutPage, profilePage };
            var outPage = pages[from];
            var inPage  = pages[to];

            if (outPage == null || inPage == null) { _animating = false; yield break; }

            var outRT = outPage.GetComponent<RectTransform>();
            var inRT  = inPage.GetComponent<RectTransform>();

            // direction > 0 → new tab is to the right → slide right-to-left
            float width = ((RectTransform)inRT.parent).rect.width;
            if (width <= 0f) width = 1080f;
            float startX = (to > from ? 1f : -1f) * width;

            inPage.SetActive(true);
            TabSetOffset(outRT, 0f);
            TabSetOffset(inRT,  startX);

            const float duration = 0.22f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                TabSetOffset(outRT, Mathf.LerpUnclamped(0f,      -startX, t));
                TabSetOffset(inRT,  Mathf.LerpUnclamped(startX,  0f,      t));
                yield return null;
            }

            TabSetOffset(outRT, 0f);
            TabSetOffset(inRT,  0f);
            outPage.SetActive(false);

            _animating = false;
        }

        private static void TabSetOffset(RectTransform rt, float x)
        {
            rt.offsetMin = new Vector2(x, rt.offsetMin.y);
            rt.offsetMax = new Vector2(x, rt.offsetMax.y);
        }

        private static void SetTabColor(Image icon, TMP_Text label, bool active)
        {
            Color c = active ? ColorActive : ColorInactive;
            if (icon  != null) icon.color  = c;
            if (label != null) label.color = c;
        }

        // ── Главная ───────────────────────────────────────────────────────

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

        private void HandleArcade()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.LoadScene("Roadmap");
        }

        private void HandlePlay()
        {
            if (_selectedCategory == null || questionDatabase == null || GameManager.Instance == null) return;
            var questions = questionDatabase.GetQuestionsByCategory(_selectedCategory);
            if (questions.Count == 0) { noQuestionsPopup?.Show(_selectedCategory.displayName); return; }
            GameManager.Instance.PrepareSession(_selectedCategory, questionDatabase);
            GameManager.Instance.LoadScene("QuestionMap");
        }

        private void RefreshStats()
        {
            if (statsText == null) return;
            string catId = _selectedCategory?.categoryId ?? "";
            int best   = SaveManager.GetBestScore(catId);
            int played = SaveManager.TotalPlayed;
            int total  = questionDatabase != null && _selectedCategory != null
                ? questionDatabase.GetQuestionsByCategory(_selectedCategory).Count : 0;
            statsText.text = LocaleManager.Get("stats_format", played, best, total);
        }

        // ── Рекорды ───────────────────────────────────────────────────────

        private void RefreshRecords()
        {
            if (recordsContent == null || questionDatabase == null) return;
            foreach (Transform child in recordsContent) Destroy(child.gameObject);

            bool alt = false;
            foreach (var cat in questionDatabase.categories)
            {
                if (cat == null) continue;
                int total = questionDatabase.GetQuestionsByCategory(cat).Count;
                int best  = SaveManager.GetBestScore(cat.categoryId);

                var rowGO = new GameObject("Row_" + cat.categoryId);
                rowGO.transform.SetParent(recordsContent, false);
                var rowImg = rowGO.AddComponent<Image>();
                rowImg.color = alt ? RowAlt : Color.white;
                alt = !alt;

                var rowLE = rowGO.AddComponent<LayoutElement>();
                rowLE.minHeight = rowLE.preferredHeight = 80;

                var rowHLG = rowGO.AddComponent<HorizontalLayoutGroup>();
                rowHLG.childAlignment       = TextAnchor.MiddleLeft;
                rowHLG.childForceExpandWidth = false;
                rowHLG.childForceExpandHeight = true;
                rowHLG.childControlWidth = rowHLG.childControlHeight = true;
                rowHLG.padding = new RectOffset(40, 40, 0, 0);
                rowHLG.spacing = 16;

                var nameGO = new GameObject("Name");
                nameGO.transform.SetParent(rowGO.transform, false);
                var nameLE = nameGO.AddComponent<LayoutElement>();
                nameLE.flexibleWidth = 1;
                var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
                nameTMP.text      = cat.displayName;
                nameTMP.fontSize  = 30;
                nameTMP.color     = RowText;
                nameTMP.alignment = TextAlignmentOptions.MidlineLeft;

                var scoreGO = new GameObject("Score");
                scoreGO.transform.SetParent(rowGO.transform, false);
                var scoreLE = scoreGO.AddComponent<LayoutElement>();
                scoreLE.minWidth = 120;
                var scoreTMP = scoreGO.AddComponent<TextMeshProUGUI>();
                scoreTMP.text      = $"{best} / {total}";
                scoreTMP.fontSize  = 30;
                scoreTMP.color     = RowScore;
                scoreTMP.alignment = TextAlignmentOptions.MidlineRight;
                scoreTMP.fontStyle = FontStyles.Bold;
            }
        }
    }
}
