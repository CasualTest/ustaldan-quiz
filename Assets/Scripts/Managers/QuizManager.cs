using System;
using System.Collections.Generic;
using UnityEngine;
using UstAldanQuiz.Data;

namespace UstAldanQuiz.Managers
{
    /// <summary>
    /// Ядро квиза: подготавливает вопросы, проверяет ответы, считает счёт.
    /// UI слой подписывается на события и отрисовывает состояние.
    /// </summary>
    public class QuizManager : MonoBehaviour
    {
        [Header("База вопросов")]
        [SerializeField] private QuestionDatabase database;

        [Header("Настройки раунда")]
        [Tooltip("Сколько вопросов в одной игре")]
        [SerializeField] private int questionsPerRound = 10;

        // --- Состояние ---
        private List<QuestionData> _roundQuestions = new List<QuestionData>();
        private int[] _shuffledAnswerIndices; // порядок вариантов для текущего вопроса
        private int _currentIndex = -1;
        private int _correctCount = 0;
        private QuestionCategory _currentCategory;

        // --- События для UI ---
        public event Action<QuestionData, string[]> OnQuestionShown;     // (вопрос, перемешанные ответы)
        public event Action<bool, int> OnAnswerChecked;                  // (правильно?, индексПравильногоОтвета в перемешанном массиве)
        public event Action<int, int, QuestionCategory> OnQuizFinished;  // (правильных, всего, категория)

        // --- Свойства ---
        public int CurrentQuestionNumber => _currentIndex + 1;
        public int TotalQuestionsInRound => _roundQuestions.Count;
        public int CorrectCount => _correctCount;

        /// <summary>
        /// Начать квиз по указанной категории.
        /// </summary>
        public void StartQuiz(QuestionCategory category)
        {
            _currentCategory = category;
            _correctCount = 0;
            _currentIndex = -1;

            var pool = database.GetQuestionsByCategory(category);
            Shuffle(pool);

            int take = Mathf.Min(questionsPerRound, pool.Count);
            _roundQuestions = pool.GetRange(0, take);

            if (_roundQuestions.Count == 0)
            {
                Debug.LogWarning($"Нет вопросов в категории {category?.displayName}");
                OnQuizFinished?.Invoke(0, 0, category);
                return;
            }

            SaveManager.LastCategory = category.categoryId;
            ShowNextQuestion();
        }

        /// <summary>
        /// Показать следующий вопрос или завершить квиз.
        /// </summary>
        public void ShowNextQuestion()
        {
            _currentIndex++;
            if (_currentIndex >= _roundQuestions.Count)
            {
                FinishQuiz();
                return;
            }

            var q = _roundQuestions[_currentIndex];

            // Перемешиваем 4 варианта ответа. Индекс 0 — правильный в QuestionData.
            _shuffledAnswerIndices = new[] { 0, 1, 2, 3 };
            Shuffle(_shuffledAnswerIndices);

            var displayedAnswers = new string[4];
            for (int i = 0; i < 4; i++)
                displayedAnswers[i] = q.answers[_shuffledAnswerIndices[i]];

            OnQuestionShown?.Invoke(q, displayedAnswers);
        }

        /// <summary>
        /// Проверить ответ пользователя по индексу в ПОКАЗАННОМ (перемешанном) массиве.
        /// </summary>
        public void SubmitAnswer(int displayedIndex)
        {
            if (_currentIndex < 0 || _currentIndex >= _roundQuestions.Count) return;
            if (_shuffledAnswerIndices == null) return;

            // Правильный — тот, у которого оригинальный индекс 0
            int originalIndex = _shuffledAnswerIndices[displayedIndex];
            bool isCorrect = originalIndex == 0;

            if (isCorrect) _correctCount++;

            // Находим позицию правильного в показанном массиве — чтобы UI мог подсветить
            int correctDisplayedIndex = Array.IndexOf(_shuffledAnswerIndices, 0);

            OnAnswerChecked?.Invoke(isCorrect, correctDisplayedIndex);
        }

        private void FinishQuiz()
        {
            SaveManager.SetBestScore(_currentCategory.categoryId, _correctCount);
            SaveManager.AddGameResult(_correctCount, _roundQuestions.Count);
            OnQuizFinished?.Invoke(_correctCount, _roundQuestions.Count, _currentCategory);
        }

        // --- Утилиты ---
        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
