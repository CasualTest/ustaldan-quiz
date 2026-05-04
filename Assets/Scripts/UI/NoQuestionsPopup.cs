using TMPro;
using UstAldanQuiz.Managers;
using UnityEngine;

namespace UstAldanQuiz.UI
{
    public class NoQuestionsPopup : BaseWindow
    {
        [Header("Контент")]
        [SerializeField] private TMP_Text messageText;

        public void Show(string categoryName)
        {
            if (messageText != null)
                messageText.text = LocaleManager.Get("no_questions_message", categoryName);
            Open();
        }
    }
}
