using UnityEngine;

namespace UstAldanQuiz.Managers
{
    /// <summary>
    /// Локальное сохранение прогресса через PlayerPrefs.
    /// PlayerPrefs работает одинаково на Android и iOS — ничего доп. настраивать не нужно.
    /// </summary>
    public static class SaveManager
    {
        private const string KEY_BEST_SCORE = "best_score_";      // + categoryId
        private const string KEY_TOTAL_PLAYED = "total_played";
        private const string KEY_TOTAL_CORRECT = "total_correct";
        private const string KEY_LAST_CATEGORY = "last_category";

        // ---- Лучший счёт по категории ----
        public static int GetBestScore(string categoryId)
        {
            return PlayerPrefs.GetInt(KEY_BEST_SCORE + categoryId, 0);
        }

        public static void SetBestScore(string categoryId, int score)
        {
            int current = GetBestScore(categoryId);
            if (score > current)
            {
                PlayerPrefs.SetInt(KEY_BEST_SCORE + categoryId, score);
                PlayerPrefs.Save();
            }
        }

        // ---- Общая статистика ----
        public static int TotalPlayed => PlayerPrefs.GetInt(KEY_TOTAL_PLAYED, 0);
        public static int TotalCorrect => PlayerPrefs.GetInt(KEY_TOTAL_CORRECT, 0);

        public static void AddGameResult(int correctAnswers, int totalQuestions)
        {
            PlayerPrefs.SetInt(KEY_TOTAL_PLAYED, TotalPlayed + totalQuestions);
            PlayerPrefs.SetInt(KEY_TOTAL_CORRECT, TotalCorrect + correctAnswers);
            PlayerPrefs.Save();
        }

        // ---- Последняя выбранная категория (удобство UX) ----
        public static string LastCategory
        {
            get => PlayerPrefs.GetString(KEY_LAST_CATEGORY, string.Empty);
            set { PlayerPrefs.SetString(KEY_LAST_CATEGORY, value); PlayerPrefs.Save(); }
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
