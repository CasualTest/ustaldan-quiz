using System;
using System.Collections.Generic;
using UnityEngine;

namespace UstAldanQuiz.Managers
{
    public static class SaveManager
    {
        private const string KEY_BEST_SCORE    = "best_score_";
        private const string KEY_TOTAL_PLAYED  = "total_played";
        private const string KEY_TOTAL_CORRECT = "total_correct";
        private const string KEY_LAST_CATEGORY = "last_category";
        private const string KEY_Q_CORRECT     = "q_ok_";
        private const string KEY_Q_WRONG       = "q_ng_";

        // In-memory cache: prefKey → set of question names
        private static readonly Dictionary<string, HashSet<string>> _cache =
            new Dictionary<string, HashSet<string>>();

        // ── Лучший счёт ───────────────────────────────────────────────────

        public static int GetBestScore(string categoryId) =>
            PlayerPrefs.GetInt(KEY_BEST_SCORE + categoryId, 0);

        public static void SetBestScore(string categoryId, int score)
        {
            if (score > GetBestScore(categoryId))
            {
                PlayerPrefs.SetInt(KEY_BEST_SCORE + categoryId, score);
                PlayerPrefs.Save();
            }
        }

        // ── Общая статистика ──────────────────────────────────────────────

        public static int TotalPlayed  => PlayerPrefs.GetInt(KEY_TOTAL_PLAYED,  0);
        public static int TotalCorrect => PlayerPrefs.GetInt(KEY_TOTAL_CORRECT, 0);

        public static void AddGameResult(int correctAnswers, int totalQuestions)
        {
            PlayerPrefs.SetInt(KEY_TOTAL_PLAYED,  TotalPlayed  + totalQuestions);
            PlayerPrefs.SetInt(KEY_TOTAL_CORRECT, TotalCorrect + correctAnswers);
            PlayerPrefs.Save();
        }

        // ── Последняя категория ───────────────────────────────────────────

        public static string LastCategory
        {
            get => PlayerPrefs.GetString(KEY_LAST_CATEGORY, string.Empty);
            set { PlayerPrefs.SetString(KEY_LAST_CATEGORY, value); PlayerPrefs.Save(); }
        }

        // ── Прогресс вопросов ─────────────────────────────────────────────

        /// <summary>null = не отвечали, true = правильно, false = неправильно</summary>
        public static bool? GetQuestionResult(string categoryId, string questionName)
        {
            if (GetSet(KEY_Q_CORRECT + categoryId).Contains(questionName)) return true;
            if (GetSet(KEY_Q_WRONG   + categoryId).Contains(questionName)) return false;
            return null;
        }

        public static void MarkQuestionAnswered(string categoryId, string questionName, bool correct)
        {
            string key = (correct ? KEY_Q_CORRECT : KEY_Q_WRONG) + categoryId;
            var set = GetSet(key);
            if (set.Add(questionName))
                SaveSet(key, set);
        }

        public static void ClearQuestionProgress(string categoryId)
        {
            string okKey = KEY_Q_CORRECT + categoryId;
            string ngKey = KEY_Q_WRONG   + categoryId;
            _cache.Remove(okKey);
            _cache.Remove(ngKey);
            PlayerPrefs.DeleteKey(okKey);
            PlayerPrefs.DeleteKey(ngKey);
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            _cache.Clear();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static HashSet<string> GetSet(string key)
        {
            if (_cache.TryGetValue(key, out var cached)) return cached;
            string raw = PlayerPrefs.GetString(key, null);
            var set = new HashSet<string>();
            if (!string.IsNullOrEmpty(raw))
            {
                if (raw.StartsWith("{"))
                {
                    var wrapper = JsonUtility.FromJson<StringList>(raw);
                    if (wrapper?.items != null)
                        foreach (var item in wrapper.items) set.Add(item);
                }
                else
                {
                    // старый формат: |name1|name2|
                    foreach (var part in raw.Split('|'))
                        if (!string.IsNullOrEmpty(part)) set.Add(part);
                    // сразу перезаписываем в новый формат
                    SaveSet(key, set);
                }
            }
            _cache[key] = set;
            return set;
        }

        private static void SaveSet(string key, HashSet<string> set)
        {
            var wrapper = new StringList();
            foreach (var item in set) wrapper.items.Add(item);
            PlayerPrefs.SetString(key, JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();
        }

        [Serializable]
        private class StringList
        {
            public List<string> items = new List<string>();
        }
    }
}
