using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Data;

namespace UstAldanQuiz.UI
{
    public class CategoryButtonUI : MonoBehaviour
    {
        [SerializeField] Button     button;
        [SerializeField] Image      background;
        [SerializeField] Image      iconImage;
        [SerializeField] TMP_Text   label;
        [SerializeField] GameObject highlight;

        public QuestionCategory Category { get; private set; }
        public event Action<CategoryButtonUI> OnClicked;

        public void Setup(QuestionCategory category)
        {
            Category = category;
            if (label != null) label.text = category.displayName;
            if (iconImage != null)
            {
                iconImage.sprite = category.icon;
                iconImage.gameObject.SetActive(category.icon != null);
            }
            if (background != null && category.backgroundSprite != null)
            {
                background.sprite = category.backgroundSprite;
                background.color  = Color.white;
                background.type   = Image.Type.Simple;
            }
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
