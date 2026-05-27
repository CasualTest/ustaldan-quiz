using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UstAldanQuiz.UI
{
    public class LetterTileUI : MonoBehaviour
    {
        [SerializeField] private Button   button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image    background;

        public char Letter { get; private set; }
        public bool IsUsed { get; private set; }

        private CanvasGroup _group;

        public event Action OnTapped;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            button?.onClick.AddListener(() => OnTapped?.Invoke());
        }

        private void OnDestroy()
        {
            button?.onClick.RemoveAllListeners();
        }

        public void Setup(char letter)
        {
            Letter = letter;
            IsUsed = false;
            if (label != null) label.text = letter.ToString();
            RefreshVisual();
        }

        public void SetUsed(bool used)
        {
            IsUsed = used;
            RefreshVisual();
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null) button.interactable = interactable && !IsUsed;
        }

        private void RefreshVisual()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            _group.alpha = IsUsed ? 0.28f : 1f;
            if (button != null) button.interactable = !IsUsed;
        }
    }
}
