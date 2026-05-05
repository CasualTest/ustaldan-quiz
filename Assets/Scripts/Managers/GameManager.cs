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
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // --- Сессия ---
        public QuestionCategory SelectedCategory { get; set; }
        public List<QuestionData> SessionQuestions { get; private set; } = new List<QuestionData>();
        public int CorrectAnswers { get; set; }
        public int TotalQuestions => SessionQuestions?.Count ?? 0;

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
        public void PrepareSession(QuestionCategory category, QuestionDatabase database)
        {
            SelectedCategory = category;
            _database        = database;
            CorrectAnswers   = 0;

            var pool = database.GetQuestionsByCategory(category);
            Shuffle(pool);
            SessionQuestions = pool;

            SaveManager.LastCategory = category.categoryId;
        }

        /// <summary>
        /// Повторить сессию с той же категорией и базой (для кнопки «Играть снова»).
        /// </summary>
        public void PrepareNewSession()
        {
            if (_database != null && SelectedCategory != null)
            {
                SaveManager.ClearQuestionProgress(SelectedCategory.categoryId);
                PrepareSession(SelectedCategory, _database);
            }
        }

        /// <summary>
        /// Загрузить сцену по имени.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadScene(sceneName);
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
