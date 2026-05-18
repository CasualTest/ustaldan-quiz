using System;
using UnityEngine;
using UnityEngine.UI;

namespace UstAldanQuiz.UI
{
    public class ConfirmPopup : BaseWindow
    {
        [SerializeField] private Button btnYes;
        [SerializeField] private Button btnNo;

        private Action _onConfirm;

        protected override void OnWindowStart()
        {
            btnNo?.onClick.AddListener(Close);
        }

        protected override void OnWindowDestroy()
        {
            btnYes?.onClick.RemoveAllListeners();
            btnNo?.onClick.RemoveAllListeners();
        }

        public void Show(Action onConfirm)
        {
            _onConfirm = onConfirm;
            btnYes.onClick.RemoveAllListeners();
            btnYes.onClick.AddListener(() => { Close(); _onConfirm?.Invoke(); });
            Open();
        }
    }
}
