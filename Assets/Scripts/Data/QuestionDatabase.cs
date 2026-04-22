using System.Collections.Generic;
using UnityEngine;

namespace UstAldanQuiz.Data
{
    /// <summary>
    /// Общая база вопросов квиза. Один экземпляр на весь проект.
    /// Создание: ПКМ → Create → UstAldan Quiz → Question Database
    /// </summary>
    [CreateAssetMenu(
        fileName = "QuestionDatabase",
        menuName = "UstAldan Quiz/Question Database",
        order = 2)]
    public class QuestionDatabase : ScriptableObject
    {
        [Tooltip("Все категории квиза")]
        public List<QuestionCategory> categories = new List<QuestionCategory>();

        [Tooltip("Все вопросы. Можно перетащить папкой целиком.")]
        public List<QuestionData> allQuestions = new List<QuestionData>();

        /// <summary>
        /// Получить все вопросы в указанной категории.
        /// </summary>
        public List<QuestionData> GetQuestionsByCategory(QuestionCategory category)
        {
            var result = new List<QuestionData>();
            if (category == null) return result;

            foreach (var q in allQuestions)
            {
                if (q != null && q.category == category)
                    result.Add(q);
            }
            return result;
        }
    }
}
