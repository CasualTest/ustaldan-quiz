using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UstAldanQuiz.UI
{
    public class ProgressBarUI : MonoBehaviour
    {
        [SerializeField] private Image    fillImage;
        [SerializeField] private TMP_Text label;

        public void SetProgress(int answered, int total)
        {
            float ratio = total > 0 ? Mathf.Clamp01((float)answered / total) : 0f;
            if (label     != null) label.text        = $"{answered} / {total}";
            if (fillImage != null) fillImage.fillAmount = ratio;
        }
    }
}
