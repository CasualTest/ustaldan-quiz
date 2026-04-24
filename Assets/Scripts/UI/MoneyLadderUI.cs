using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.UI
{
    /// <summary>
    /// Боковая панель с денежной лесенкой (15 уровней).
    /// levelLabels[0] и levelBackgrounds[0] — первый вопрос (снизу).
    /// </summary>
    public class MoneyLadderUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private QuizManager quizManager;
        [SerializeField] private MoneyLadder moneyLadder;

        [Header("Строки лесенки (15 штук, индекс 0 = вопрос 1)")]
        [SerializeField] private TMP_Text[] levelLabels = new TMP_Text[15];
        [SerializeField] private Image[] levelBackgrounds = new Image[15];

        [Header("Цвета строк")]
        [SerializeField] private Color defaultColor    = new Color(0.12f, 0.12f, 0.35f);
        [SerializeField] private Color currentColor    = new Color(0.90f, 0.70f, 0.10f);
        [SerializeField] private Color safeZoneColor   = new Color(0.10f, 0.55f, 0.10f);
        [SerializeField] private Color passedColor     = new Color(0.15f, 0.35f, 0.15f);

        private void Awake()
        {
            BuildLabels();
        }

        private void OnEnable()
        {
            if (quizManager != null)
                quizManager.OnQuestionShown += OnQuestionShown;
        }

        private void OnDisable()
        {
            if (quizManager != null)
                quizManager.OnQuestionShown -= OnQuestionShown;
        }

        private void OnQuestionShown(QuestionData q, string[] answers)
        {
            RefreshHighlight();
        }

        private void BuildLabels()
        {
            if (moneyLadder == null) return;

            int count = Mathf.Min(levelLabels.Length, moneyLadder.QuestionCount);
            for (int i = 0; i < count; i++)
            {
                if (levelLabels[i] != null)
                    levelLabels[i].text = moneyLadder.GetLabel(i);

                // Пометить несгораемые уровни знаком
                if (levelBackgrounds[i] != null && moneyLadder.levels[i].isSafeZone)
                    levelBackgrounds[i].color = safeZoneColor;
            }
        }

        public void RefreshHighlight()
        {
            if (moneyLadder == null) return;

            int current = quizManager.CurrentQuestionNumber - 1; // 0-based
            int count = Mathf.Min(levelLabels.Length, moneyLadder.QuestionCount);

            for (int i = 0; i < count; i++)
            {
                if (levelBackgrounds[i] == null) continue;

                if (i == current)
                    levelBackgrounds[i].color = currentColor;
                else if (i < current)
                    levelBackgrounds[i].color = passedColor;
                else if (moneyLadder.levels[i].isSafeZone)
                    levelBackgrounds[i].color = safeZoneColor;
                else
                    levelBackgrounds[i].color = defaultColor;
            }
        }
    }
}
