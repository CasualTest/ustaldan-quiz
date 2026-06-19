using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UstAldanQuiz.Data;

namespace UstAldanQuiz.Managers
{
    /// <summary>
    /// Синглтон, живущий между сценами. Хранит состояние текущей сессии.
    /// Должен быть на GameObject в сцене MainMenu.
    /// </summary>
    public enum GameMode
    {
        Category,
        Roadmap,
        Millionaire
    }

    public class AnswerLog
    {
        public string questionId;
        public string categoryId;
        public string categoryName;
        public bool   isCorrect;
        public float  timeSec;
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const int MillionaireQuestionCount = 15;

        // --- Сессия ---
        public QuestionCategory SelectedCategory { get; set; }
        public List<QuestionData> SessionQuestions { get; private set; } = new List<QuestionData>();
        public int CorrectAnswers { get; set; }
        public int TotalQuestions => SessionQuestions?.Count ?? 0;
        public GameMode CurrentMode { get; private set; } = GameMode.Category;
        public List<AnswerLog> AnswerLogs { get; private set; } = new List<AnswerLog>();

        private QuestionDatabase _database;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Подготовить новую сессию: выбрать категорию и перемешать 15 вопросов.
        /// </summary>
        public void LogAnswer(QuestionData q, bool isCorrect, float timeSec)
        {
            if (q == null) return;
            AnswerLogs.Add(new AnswerLog
            {
                questionId   = q.name,
                categoryId   = q.category != null ? q.category.categoryId : "",
                categoryName = q.category != null ? q.category.displayName : "",
                isCorrect    = isCorrect,
                timeSec      = timeSec
            });
        }

        public void PrepareSession(QuestionCategory category, QuestionDatabase database)
        {
            SelectedCategory = category;
            _database        = database;
            CorrectAnswers   = 0;
            CurrentMode      = GameMode.Category;
            AnswerLogs.Clear();

            database.EnsureRuntimeQuestionsLoaded();
            SessionQuestions = database.GetQuestionsByCategory(category);

            SaveManager.LastCategory = category.categoryId;
        }

        /// <summary>
        /// Подготовить сессию «Миллионер»: 15 случайных вопросов, отсортированных по сложности.
        /// </summary>
        public void PrepareMillionaireSession(QuestionDatabase database)
        {
            SelectedCategory = null;
            _database        = database;
            CorrectAnswers   = 0;
            CurrentMode      = GameMode.Millionaire;
            AnswerLogs.Clear();

            database.EnsureRuntimeQuestionsLoaded();

            var pool = new List<QuestionData>();
            foreach (var q in database.allQuestions)
                if (q != null) pool.Add(q);
            Shuffle(pool);

            int take = Mathf.Min(MillionaireQuestionCount, pool.Count);
            var selected = pool.GetRange(0, take);
            selected.Sort((a, b) => a.difficulty.CompareTo(b.difficulty));
            SessionQuestions = selected;
        }

        /// <summary>
        /// Повторить сессию с той же категорией и базой (для кнопки «Играть снова»).
        /// </summary>
        public void PrepareNewSession()
        {
            if (_database == null) return;
            if (CurrentMode == GameMode.Millionaire)
            {
                PrepareMillionaireSession(_database);
                return;
            }
            if (SelectedCategory != null)
            {
                SaveManager.ClearQuestionProgress(SelectedCategory.categoryId);
                PrepareSession(SelectedCategory, _database);
            }
        }

        /// <summary>
        /// Загрузить сцену по имени.
        /// </summary>
        public void LoadScene(string sceneName, bool instant = false)
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadScene(sceneName, instant);
            else
                SceneManager.LoadScene(sceneName);
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
