using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.UI
{
    public class QuizScreenUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private QuizManager quizManager;

        [Header("Вопрос")]
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Image questionImage;
        [SerializeField] private GameObject questionImageContainer;
        [SerializeField] private TMP_Text progressText;       // «Вопрос 7 из 15»

        [Header("Кнопки ответов")]
        [SerializeField] private Button[] answerButtons = new Button[4];
        [SerializeField] private TMP_Text[] answerLabels = new TMP_Text[4];

        [Header("Приз")]
        [SerializeField] private TMP_Text currentPrizeText;    // «За этот вопрос: 4 000 ₽»
        [SerializeField] private TMP_Text guaranteedPrizeText; // «Несгораемая: 1 000 ₽»

        [Header("Подсказки")]
        [SerializeField] private Button fiftyFiftyButton;
        [SerializeField] private Button audienceHelpButton;
        [SerializeField] private Button phoneFriendButton;

        [Header("Забрать деньги")]
        [SerializeField] private Button walkAwayButton;

        [Header("Панель помощи зала")]
        [SerializeField] private GameObject audienceHelpPanel;
        [SerializeField] private Image[] audienceBars = new Image[4];           // Image с fillAmount (Filled тип)
        [SerializeField] private TMP_Text[] audiencePercentLabels = new TMP_Text[4];
        [SerializeField] private Button closeAudienceButton;

        [Header("Панель звонка другу")]
        [SerializeField] private GameObject phoneFriendPanel;
        [SerializeField] private TMP_Text phoneFriendText;
        [SerializeField] private Button closePhoneButton;

        [Header("Экран результата")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitleText;     // «Поздравляем!» / «Неверно!»
        [SerializeField] private TMP_Text resultPrizeText;     // «Ваш выигрыш: 32 000 ₽»
        [SerializeField] private Button playAgainButton;

        [Header("Цвета подсветки")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color correctColor = new Color(0.4f, 0.85f, 0.4f);
        [SerializeField] private Color wrongColor = new Color(0.9f, 0.35f, 0.35f);
        [SerializeField] private Color hiddenColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [Header("Задержка перед следующим вопросом (сек)")]
        [SerializeField] private float nextDelay = 2f;

        private int _lastClickedIndex = -1;
        private static readonly string[] PrefixLabels = { "A", "B", "C", "D" };

        private void OnEnable()
        {
            quizManager.OnQuestionShown += HandleQuestionShown;
            quizManager.OnAnswerChecked += HandleAnswerChecked;
            quizManager.OnFiftyFiftyResult += HandleFiftyFifty;
            quizManager.OnAudienceHelpResult += HandleAudienceHelp;
            quizManager.OnPhoneFriendResult += HandlePhoneFriend;
            quizManager.OnQuizFinished += HandleQuizFinished;

            for (int i = 0; i < answerButtons.Length; i++)
            {
                int captured = i;
                answerButtons[i].onClick.AddListener(() => OnAnswerClicked(captured));
            }

            fiftyFiftyButton?.onClick.AddListener(() => quizManager.UseFiftyFifty());
            audienceHelpButton?.onClick.AddListener(() => quizManager.UseAudienceHelp());
            phoneFriendButton?.onClick.AddListener(() => quizManager.UsePhoneFriend());
            walkAwayButton?.onClick.AddListener(() => quizManager.WalkAway());
            closeAudienceButton?.onClick.AddListener(ClosePopups);
            closePhoneButton?.onClick.AddListener(ClosePopups);
        }

        private void OnDisable()
        {
            quizManager.OnQuestionShown -= HandleQuestionShown;
            quizManager.OnAnswerChecked -= HandleAnswerChecked;
            quizManager.OnFiftyFiftyResult -= HandleFiftyFifty;
            quizManager.OnAudienceHelpResult -= HandleAudienceHelp;
            quizManager.OnPhoneFriendResult -= HandlePhoneFriend;
            quizManager.OnQuizFinished -= HandleQuizFinished;

            foreach (var btn in answerButtons) btn.onClick.RemoveAllListeners();
            fiftyFiftyButton?.onClick.RemoveAllListeners();
            audienceHelpButton?.onClick.RemoveAllListeners();
            phoneFriendButton?.onClick.RemoveAllListeners();
            walkAwayButton?.onClick.RemoveAllListeners();
            closeAudienceButton?.onClick.RemoveAllListeners();
            closePhoneButton?.onClick.RemoveAllListeners();
            playAgainButton?.onClick.RemoveAllListeners();
        }

        private void HandleQuestionShown(QuestionData q, string[] displayedAnswers)
        {
            ClosePopups();
            resultPanel?.SetActive(false);
            _lastClickedIndex = -1;

            questionText.text = q.questionText;

            if (q.questionImage != null)
            {
                questionImageContainer?.SetActive(true);
                questionImage.sprite = q.questionImage;
            }
            else
            {
                questionImageContainer?.SetActive(false);
            }

            progressText.text = $"Вопрос {quizManager.CurrentQuestionNumber} из {quizManager.TotalQuestionsInRound}";

            if (currentPrizeText != null)
                currentPrizeText.text = $"За этот вопрос: {quizManager.CurrentPrizeLabel}";

            if (guaranteedPrizeText != null)
            {
                int guaranteed = quizManager.GuaranteedPrize;
                guaranteedPrizeText.text = guaranteed > 0
                    ? $"Несгораемая: {guaranteed:#,##0} руб.".Replace(",", " ")
                    : "Несгораемая: —";
            }

            for (int i = 0; i < answerButtons.Length; i++)
            {
                answerLabels[i].text = $"{PrefixLabels[i]}: {displayedAnswers[i]}";
                answerButtons[i].interactable = true;
                answerButtons[i].image.color = defaultColor;
            }

            RefreshLifelineButtons();
            walkAwayButton?.gameObject.SetActive(true);
        }

        private void OnAnswerClicked(int index)
        {
            _lastClickedIndex = index;
            foreach (var btn in answerButtons) btn.interactable = false;
            SetLifelinesInteractable(false);
            if (walkAwayButton != null) walkAwayButton.interactable = false;
            quizManager.SubmitAnswer(index);
        }

        private void HandleAnswerChecked(bool isCorrect, int correctDisplayedIndex)
        {
            answerButtons[correctDisplayedIndex].image.color = correctColor;

            if (!isCorrect && _lastClickedIndex >= 0 && _lastClickedIndex != correctDisplayedIndex)
                answerButtons[_lastClickedIndex].image.color = wrongColor;

            Invoke(nameof(GoToNext), nextDelay);
        }

        private void GoToNext()
        {
            quizManager.ShowNextQuestion();
        }

        private void HandleFiftyFifty(int[] hiddenIndices)
        {
            foreach (int idx in hiddenIndices)
            {
                answerButtons[idx].interactable = false;
                answerButtons[idx].image.color = hiddenColor;
                answerLabels[idx].text = PrefixLabels[idx] + ":";
            }
            fiftyFiftyButton?.gameObject.SetActive(false);
        }

        private void HandleAudienceHelp(int[] percentages)
        {
            if (audienceHelpPanel == null) return;
            audienceHelpPanel.SetActive(true);

            for (int i = 0; i < 4; i++)
            {
                if (audienceBars != null && i < audienceBars.Length && audienceBars[i] != null)
                    audienceBars[i].fillAmount = percentages[i] / 100f;
                if (audiencePercentLabels != null && i < audiencePercentLabels.Length && audiencePercentLabels[i] != null)
                    audiencePercentLabels[i].text = $"{percentages[i]}%";
            }
            audienceHelpButton?.gameObject.SetActive(false);
        }

        private void HandlePhoneFriend(string hint)
        {
            if (phoneFriendPanel == null) return;
            phoneFriendPanel.SetActive(true);
            if (phoneFriendText != null) phoneFriendText.text = hint;
            phoneFriendButton?.gameObject.SetActive(false);
        }

        private void HandleQuizFinished(int correct, int total, QuestionCategory cat, int prize, QuizEndReason reason)
        {
            CancelInvoke(nameof(GoToNext));
            ClosePopups();

            if (resultPanel == null) return;
            resultPanel.SetActive(true);

            string prizeStr = $"{prize:#,##0} руб.".Replace(",", " ");

            switch (reason)
            {
                case QuizEndReason.Completed when prize >= 1_000_000:
                    if (resultTitleText != null) resultTitleText.text = "Поздравляем!";
                    if (resultPrizeText != null) resultPrizeText.text = $"Вы выиграли {prizeStr}!";
                    break;
                case QuizEndReason.Completed:
                    if (resultTitleText != null) resultTitleText.text = "Отличная игра!";
                    if (resultPrizeText != null) resultPrizeText.text = $"Ваш выигрыш: {prizeStr}";
                    break;
                case QuizEndReason.WrongAnswer:
                    if (resultTitleText != null) resultTitleText.text = "Неверный ответ!";
                    if (resultPrizeText != null) resultPrizeText.text = prize > 0
                        ? $"Несгораемая сумма: {prizeStr}"
                        : "Вы уходите ни с чем.";
                    break;
                case QuizEndReason.WalkedAway:
                    if (resultTitleText != null) resultTitleText.text = "Вы забрали деньги!";
                    if (resultPrizeText != null) resultPrizeText.text = $"Ваш выигрыш: {prizeStr}";
                    break;
            }

            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveAllListeners();
                playAgainButton.onClick.AddListener(() => quizManager.StartQuiz(cat));
            }
        }

        private void ClosePopups()
        {
            audienceHelpPanel?.SetActive(false);
            phoneFriendPanel?.SetActive(false);
        }

        private void RefreshLifelineButtons()
        {
            if (fiftyFiftyButton != null)   fiftyFiftyButton.gameObject.SetActive(quizManager.FiftyFiftyAvailable);
            if (audienceHelpButton != null) audienceHelpButton.gameObject.SetActive(quizManager.AudienceHelpAvailable);
            if (phoneFriendButton != null)  phoneFriendButton.gameObject.SetActive(quizManager.PhoneFriendAvailable);
            SetLifelinesInteractable(true);
        }

        private void SetLifelinesInteractable(bool interactable)
        {
            if (fiftyFiftyButton != null)   fiftyFiftyButton.interactable = interactable;
            if (audienceHelpButton != null) audienceHelpButton.interactable = interactable;
            if (phoneFriendButton != null)  phoneFriendButton.interactable = interactable;
        }
    }
}
