using System;
using System.Collections.Generic;
using UnityEngine;
using UstAldanQuiz.Data;

namespace UstAldanQuiz.Managers
{
    public enum QuizEndReason { Completed, WrongAnswer, WalkedAway }

    public class QuizManager : MonoBehaviour
    {
        [Header("Менеджеры")]
        [SerializeField] private MenuManager _menuManager;

        [Header("База вопросов")]
        [SerializeField] private QuestionDatabase database;

        [Header("Денежная лесенка")]
        [SerializeField] private MoneyLadder moneyLadder;

        [Header("Вопросов в раунде (если нет MoneyLadder)")]
        [SerializeField] private int questionsPerRound = 15;

        // --- Состояние раунда ---
        private List<QuestionData> _roundQuestions = new List<QuestionData>();
        private int[] _shuffledAnswerIndices;
        private int _currentIndex = -1;
        private int _correctCount = 0;
        private int _lastCorrectIndex = -1;
        private QuestionCategory _currentCategory;
        private bool _playerLost;
        private bool _gameEnded;

        // --- Подсказки ---
        private bool _fiftyFiftyUsed;
        private bool _audienceHelpUsed;
        private bool _phoneFriendUsed;
        private int[] _fiftyFiftyHidden = new int[0];

        // --- События ---
        public event Action<QuestionData, string[]> OnQuestionShown;
        public event Action<bool, int> OnAnswerChecked;                             // (верно?, индекс правильного)
        public event Action<int, int, QuestionCategory, int, QuizEndReason> OnQuizFinished; // (верных, всего, кат, приз, причина)
        public event Action<int[]> OnFiftyFiftyResult;                              // индексы скрытых вариантов
        public event Action<int[]> OnAudienceHelpResult;                            // % для 4 вариантов
        public event Action<string> OnPhoneFriendResult;                            // текст подсказки

        // --- Свойства ---
        public int CurrentQuestionNumber => _currentIndex + 1;
        public int TotalQuestionsInRound => _roundQuestions.Count;
        public int CorrectCount => _correctCount;
        public bool FiftyFiftyAvailable => !_fiftyFiftyUsed;
        public bool AudienceHelpAvailable => !_audienceHelpUsed;
        public bool PhoneFriendAvailable => !_phoneFriendUsed;

        public string CurrentPrizeLabel =>
            moneyLadder != null && _currentIndex >= 0 ? moneyLadder.GetLabel(_currentIndex) : "—";

        public int GuaranteedPrize =>
            moneyLadder != null && _currentIndex >= 0 ? moneyLadder.GetGuaranteedPrize(_currentIndex) : 0;

        public int EarnedPrize =>
            moneyLadder != null && _lastCorrectIndex >= 0 ? moneyLadder.GetPrize(_lastCorrectIndex) : 0;

        // --- Управление ---

        public void Awake()
        {
            bool test = true;
        }

        public void StartQuiz(QuestionCategory category)
        {
            _currentCategory = category;
            _correctCount = 0;
            _currentIndex = -1;
            _lastCorrectIndex = -1;
            _playerLost = false;
            _gameEnded = false;
            _fiftyFiftyUsed = false;
            _audienceHelpUsed = false;
            _phoneFriendUsed = false;
            _fiftyFiftyHidden = new int[0];

            var pool = database.GetQuestionsByCategory(category);
            Shuffle(pool);

            int take = moneyLadder != null
                ? Mathf.Min(moneyLadder.QuestionCount, pool.Count)
                : Mathf.Min(questionsPerRound, pool.Count);

            _roundQuestions = pool.GetRange(0, take);

            if (_roundQuestions.Count == 0)
            {
                Debug.LogWarning($"Нет вопросов в категории {category?.displayName}");
                OnQuizFinished?.Invoke(0, 0, category, 0, QuizEndReason.Completed);
                return;
            }

            SaveManager.LastCategory = category.categoryId;
            ShowNextQuestion();
        }

        public void ShowNextQuestion()
        {
            if (_gameEnded) return;

            if (_playerLost)
            {
                FinishQuiz(GuaranteedPrize, QuizEndReason.WrongAnswer);
                return;
            }

            _currentIndex++;
            if (_currentIndex >= _roundQuestions.Count)
            {
                int fullPrize = moneyLadder != null
                    ? moneyLadder.GetPrize(_roundQuestions.Count - 1)
                    : _correctCount;
                FinishQuiz(fullPrize, QuizEndReason.Completed);
                return;
            }

            _fiftyFiftyHidden = new int[0];
            _shuffledAnswerIndices = new[] { 0, 1, 2, 3 };
            Shuffle(_shuffledAnswerIndices);

            var q = _roundQuestions[_currentIndex];
            var displayed = new string[4];
            for (int i = 0; i < 4; i++)
                displayed[i] = q.answers[_shuffledAnswerIndices[i]];

            OnQuestionShown?.Invoke(q, displayed);
        }

        public void SubmitAnswer(int displayedIndex)
        {
            if (_gameEnded || _currentIndex < 0 || _currentIndex >= _roundQuestions.Count) return;
            if (_shuffledAnswerIndices == null) return;

            bool isCorrect = _shuffledAnswerIndices[displayedIndex] == 0;

            if (isCorrect)
            {
                _correctCount++;
                _lastCorrectIndex = _currentIndex;
            }
            else
            {
                _playerLost = true;
            }

            int correctDisplayedIndex = Array.IndexOf(_shuffledAnswerIndices, 0);
            OnAnswerChecked?.Invoke(isCorrect, correctDisplayedIndex);
        }

        public void WalkAway()
        {
            _menuManager.BackToMenu();
            if (_gameEnded) return;
            FinishQuiz(EarnedPrize, QuizEndReason.WalkedAway);
        }

        // --- Подсказки ---

        public void UseFiftyFifty()
        {
            if (_fiftyFiftyUsed || _shuffledAnswerIndices == null || _gameEnded) return;
            _fiftyFiftyUsed = true;

            var wrong = new List<int>();
            for (int i = 0; i < 4; i++)
                if (_shuffledAnswerIndices[i] != 0) wrong.Add(i);

            // Убрать 2 из 3 неправильных, оставить 1
            int keepIdx = UnityEngine.Random.Range(0, wrong.Count);
            wrong.RemoveAt(keepIdx);
            _fiftyFiftyHidden = wrong.ToArray();

            OnFiftyFiftyResult?.Invoke(_fiftyFiftyHidden);
        }

        public void UseAudienceHelp()
        {
            if (_audienceHelpUsed || _shuffledAnswerIndices == null || _gameEnded) return;
            _audienceHelpUsed = true;

            int correctIdx = Array.IndexOf(_shuffledAnswerIndices, 0);
            OnAudienceHelpResult?.Invoke(GenerateAudienceVotes(correctIdx));
        }

        public void UsePhoneFriend()
        {
            if (_phoneFriendUsed || _currentIndex < 0 || _gameEnded) return;
            _phoneFriendUsed = true;

            int correctIdx = Array.IndexOf(_shuffledAnswerIndices, 0);
            string[] labels = { "A", "B", "C", "D" };
            string correctText = _roundQuestions[_currentIndex].answers[0];

            string hint;
            if (UnityEngine.Random.value < 0.8f)
            {
                hint = $"Я уверен — это вариант {labels[correctIdx]}: «{correctText}».";
            }
            else
            {
                var wrongOptions = new List<int>();
                for (int i = 0; i < 4; i++)
                    if (_shuffledAnswerIndices[i] != 0) wrongOptions.Add(i);
                int wrongPick = wrongOptions[UnityEngine.Random.Range(0, wrongOptions.Count)];
                hint = $"Думаю, это вариант {labels[wrongPick]}... но я не уверен.";
            }

            OnPhoneFriendResult?.Invoke(hint);
        }

        // --- Внутренние ---

        private void FinishQuiz(int prize, QuizEndReason reason)
        {
            if (_gameEnded) return;
            _gameEnded = true;
            SaveManager.SetBestScore(_currentCategory.categoryId, _correctCount);
            SaveManager.AddGameResult(_correctCount, _roundQuestions.Count);
            OnQuizFinished?.Invoke(_correctCount, _roundQuestions.Count, _currentCategory, prize, reason);
        }

        private int[] GenerateAudienceVotes(int correctIndex)
        {
            int[] result = new int[4];

            // Составить список активных (не скрытых 50:50) вариантов
            var active = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                bool hidden = false;
                foreach (int h in _fiftyFiftyHidden)
                    if (h == i) { hidden = true; break; }
                if (!hidden) active.Add(i);
            }

            int correctShare = UnityEngine.Random.Range(55, 85);
            result[correctIndex] = correctShare;
            int remaining = 100 - correctShare;

            var wrong = new List<int>(active);
            wrong.Remove(correctIndex);

            for (int i = 0; i < wrong.Count - 1; i++)
            {
                int share = UnityEngine.Random.Range(0, remaining + 1);
                result[wrong[i]] = share;
                remaining -= share;
            }
            if (wrong.Count > 0)
                result[wrong[wrong.Count - 1]] = remaining;

            return result;
        }

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
