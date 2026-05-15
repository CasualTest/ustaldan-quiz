using System;
using System.Collections.Generic;
using UnityEngine;

namespace UstAldanQuiz.Data
{
    [CreateAssetMenu(
        fileName = "QuestionDatabase",
        menuName = "UstAldan Quiz/Question Database",
        order = 2)]
    public class QuestionDatabase : ScriptableObject
    {
        [Tooltip("Все категории квиза")]
        public List<QuestionCategory> categories = new List<QuestionCategory>();

        [Tooltip("Заполняется автоматически из Resources/questions.json при старте. В инспекторе можно оставить пустым.")]
        public List<QuestionData> allQuestions = new List<QuestionData>();

        public void EnsureRuntimeQuestionsLoaded()
        {
            if (allQuestions.Count > 0 && allQuestions[0] != null)
            {
                Debug.Log($"[QuestionDatabase] Уже загружено {allQuestions.Count} вопросов, пропускаем.");
                return;
            }

            var textAsset = Resources.Load<TextAsset>("questions");
            if (textAsset == null)
            {
                Debug.LogWarning("[QuestionDatabase] Resources/questions.json не найден.");
                return;
            }

            string json = textAsset.text;
            // убираем BOM если есть
            if (json.Length > 0 && json[0] == '﻿') json = json.Substring(1);

            var data = JsonUtility.FromJson<QuestionsJson>(json);
            if (data?.questions == null || data.questions.Length == 0)
            {
                Debug.LogWarning($"[QuestionDatabase] questions.json пустой или не распарсился. Длина текста: {json.Length}, первые символы: '{(json.Length > 20 ? json.Substring(0, 20) : json)}'");
                return;
            }

            var catLookup = new Dictionary<string, QuestionCategory>(StringComparer.OrdinalIgnoreCase);
            foreach (var cat in categories)
                if (cat != null) catLookup[cat.categoryId] = cat;

            Debug.Log($"[QuestionDatabase] Категорий в базе: {catLookup.Count} ({string.Join(", ", catLookup.Keys)})");

            allQuestions = new List<QuestionData>(data.questions.Length);
            foreach (var entry in data.questions)
            {
                var q = ScriptableObject.CreateInstance<QuestionData>();
                q.name            = entry.id;
                q.questionText    = entry.questionRu;
                q.questionTextSah = entry.questionSah;
                q.answers         = entry.answers ?? new string[4];
                q.difficulty      = entry.difficulty;
                q.factAfterRu     = entry.factRu;
                q.factAfterSah    = entry.factSah;
                catLookup.TryGetValue(entry.categoryId ?? "", out q.category);
                allQuestions.Add(q);
            }

            Debug.Log($"[QuestionDatabase] Загружено {allQuestions.Count} вопросов из JSON.");
        }

        public List<QuestionData> GetQuestionsByCategory(QuestionCategory category)
        {
            var result = new List<QuestionData>();
            if (category == null) return result;
            foreach (var q in allQuestions)
                if (q != null && q.category == category)
                    result.Add(q);
            return result;
        }

        // ── JSON types ────────────────────────────────────────────────────────

        [Serializable]
        public class QuestionJsonEntry
        {
            public string   id;
            public string   categoryId;
            public string   questionRu;
            public string   questionSah;
            public string[] answers;
            public int      difficulty;
            public string   factRu;
            public string   factSah;
        }

        [Serializable]
        public class QuestionsJson
        {
            public QuestionJsonEntry[] questions;
        }
    }
}
