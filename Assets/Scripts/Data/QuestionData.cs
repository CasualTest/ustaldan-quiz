using UnityEngine;

namespace UstAldanQuiz.Data
{
    /// <summary>
    /// Один вопрос квиза с 4 вариантами ответа (как в «Кто хочет стать миллионером»).
    /// Создание: ПКМ в Project → Create → UstAldan Quiz → Question
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewQuestion",
        menuName = "UstAldan Quiz/Question",
        order = 1)]
    public class QuestionData : ScriptableObject
    {
        [Header("Основное")]
        [Tooltip("Категория, к которой относится вопрос")]
        public QuestionCategory category;

        [Tooltip("Текст вопроса")]
        [TextArea(2, 5)]
        public string questionText;

        [Tooltip("Изображение к вопросу (опционально)")]
        public Sprite questionImage;

        [Header("Ответы (индекс 0 — всегда правильный)")]
        [Tooltip("Ровно 4 варианта ответа. ПЕРВЫЙ — правильный, остальные будут перемешаны при показе.")]
        public string[] answers = new string[4];

        [Header("Сложность")]
        [Range(1, 3)]
        [Tooltip("1 = лёгкий, 2 = средний, 3 = сложный")]
        public int difficulty = 1;

        /// <summary>
        /// Текст правильного ответа (первый в массиве).
        /// </summary>
        public string CorrectAnswer => answers != null && answers.Length > 0 ? answers[0] : string.Empty;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Гарантируем, что массив ответов всегда длиной 4
            if (answers == null || answers.Length != 4)
            {
                var fixedArray = new string[4];
                if (answers != null)
                {
                    for (int i = 0; i < Mathf.Min(answers.Length, 4); i++)
                        fixedArray[i] = answers[i];
                }
                answers = fixedArray;
            }
        }
#endif
    }
}
