using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Data;

namespace UstAldanQuiz.UI
{
    public class CategoryButtonUI : MonoBehaviour
    {
        [SerializeField] Button    button;
        [SerializeField] Image     background;
        [SerializeField] TMP_Text  label;
        [SerializeField] GameObject highlight;

        public QuestionCategory Category { get; private set; }
        public event Action<CategoryButtonUI> OnClicked;

        public void Setup(QuestionCategory category)
        {
            Category = category;
            if (label != null) label.text = category.displayName;
            button?.onClick.AddListener(() => OnClicked?.Invoke(this));
        }

        public void SetSelected(bool selected)
        {
            if (highlight != null) highlight.SetActive(selected);
        }

        private void OnDestroy()
        {
            button?.onClick.RemoveAllListeners();
        }
    }
}
