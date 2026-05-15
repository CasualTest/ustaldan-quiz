using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UstAldanQuiz.UI
{
    public class QuestionWindow : BaseWindow
    {
        [Header("Вопрос")]
        public TMP_Text questionText;

        [Header("Медиа")]
        public GameObject mediaZone;
        public Image      questionImage;

        [Header("Ответы")]
        public Button[]   answerButtons = new Button[4];
        public TMP_Text[] answerLabels  = new TMP_Text[4];

        [Header("Результат")]
        public TMP_Text  resultFeedback;
        public Button    btnContinue;
        public FactPopup factPopup;

        public event Action OnClosed;

        protected override void OnWindowStart()
        {
            btnContinue?.onClick.AddListener(Close);
        }

        protected override void OnWindowDestroy()
        {
            btnContinue?.onClick.RemoveAllListeners();
        }

        public override void Close()
        {
            base.Close();
            OnClosed?.Invoke();
        }
    }
}
