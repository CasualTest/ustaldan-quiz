using System;
using TMPro;
using UnityEngine;

namespace UstAldanQuiz.UI
{
    public class FactPopup : BaseWindow
    {
        [Header("Контент")]
        [SerializeField] private TMP_Text factText;

        private Action _onClosed;

        protected override void OnWindowStart()
        {
            // btnClose (кнопка ОК) унаследована — Close() вызывается автоматически
        }

        public void Show(string fact, Action onClosed = null)
        {
            _onClosed = onClosed;
            if (factText != null) factText.text = fact;
            Open();
        }

        public override void Close()
        {
            base.Close();
            _onClosed?.Invoke();
            _onClosed = null;
        }
    }
}
