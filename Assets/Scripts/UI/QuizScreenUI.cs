using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.UI
{
    /// <summary>
    /// UI экрана квиза. Подписывается на события QuizManager и отображает состояние.
    /// </summary>
    public class QuizScreenUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private QuizManager quizManager;

        [Header("Вопрос")]
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Image questionImage;
        [SerializeField] private GameObject questionImageContainer;
        [SerializeField] private TMP_Text progressText; // «3 / 10»

        [Header("Кнопки ответов")]
        [SerializeField] private Button[] answerButtons = new Button[4];
        [SerializeField] private TMP_Text[] answerLabels = new TMP_Text[4];

        [Header("Цвета подсветки")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color correctColor = new Color(0.4f, 0.85f, 0.4f);
        [SerializeField] private Color wrongColor = new Color(0.9f, 0.35f, 0.35f);

        [Header("Задержка перед следующим вопросом (сек)")]
        [SerializeField] private float nextDelay = 1.5f;

        private int _lastClickedIndex = -1;

        private void OnEnable()
        {
            quizManager.OnQuestionShown += HandleQuestionShown;
            quizManager.OnAnswerChecked += HandleAnswerChecked;

            for (int i = 0; i < answerButtons.Length; i++)
            {
                int captured = i; // замыкание
                answerButtons[i].onClick.AddListener(() => OnAnswerClicked(captured));
            }
        }

        private void OnDisable()
        {
            quizManager.OnQuestionShown -= HandleQuestionShown;
            quizManager.OnAnswerChecked -= HandleAnswerChecked;

            foreach (var btn in answerButtons)
                btn.onClick.RemoveAllListeners();
        }

        private void HandleQuestionShown(QuestionData q, string[] displayedAnswers)
        {
            questionText.text = q.questionText;

            // Картинка к вопросу — опционально
            if (q.questionImage != null)
            {
                questionImageContainer.SetActive(true);
                questionImage.sprite = q.questionImage;
            }
            else
            {
                questionImageContainer.SetActive(false);
            }

            // Прогресс
            progressText.text = $"{quizManager.CurrentQuestionNumber} / {quizManager.TotalQuestionsInRound}";

            // Варианты
            for (int i = 0; i < answerButtons.Length; i++)
            {
                answerLabels[i].text = displayedAnswers[i];
                answerButtons[i].interactable = true;
                answerButtons[i].image.color = defaultColor;
            }
        }

        private void OnAnswerClicked(int index)
        {
            _lastClickedIndex = index;
            // Блокируем все кнопки, чтобы нельзя было тапнуть дважды
            foreach (var btn in answerButtons) btn.interactable = false;
            quizManager.SubmitAnswer(index);
        }

        private void HandleAnswerChecked(bool isCorrect, int correctDisplayedIndex)
        {
            // Подсвечиваем правильный зелёным
            answerButtons[correctDisplayedIndex].image.color = correctColor;

            // Если игрок ошибся — подсвечиваем его выбор красным
            if (!isCorrect && _lastClickedIndex >= 0)
                answerButtons[_lastClickedIndex].image.color = wrongColor;

            Invoke(nameof(GoToNext), nextDelay);
        }

        private void GoToNext()
        {
            quizManager.ShowNextQuestion();
        }
    }
}
