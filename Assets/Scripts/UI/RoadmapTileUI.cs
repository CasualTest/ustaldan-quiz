using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UstAldanQuiz.Data;

namespace UstAldanQuiz.UI
{
    public class RoadmapTileUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image  background;
        [SerializeField] private Image  categoryIcon;
        [SerializeField] private Image  checkmark;

        [SerializeField] private Color colorUnanswered = new Color(0.91f, 0.88f, 0.82f);
        [SerializeField] private Color colorCorrect    = new Color(0.78f, 0.90f, 0.79f);
        [SerializeField] private Color colorWrong      = new Color(1.00f, 0.80f, 0.82f);
        [SerializeField] private Color colorActive     = new Color(0.68f, 0.85f, 1.00f);

        public QuestionData Question { get; private set; }
        public TileState    State    { get; private set; }

        public event Action<RoadmapTileUI> OnTileClicked;

        private void Awake()   { button.onClick.AddListener(HandleClick); }
        private void OnDestroy() { button.onClick.RemoveAllListeners(); }

        public void Setup(QuestionData question)
        {
            Question = question;

            if (background != null)
            {
                Color baseColor = colorUnanswered;
                if (question.category != null)
                    baseColor = Color.Lerp(colorUnanswered, question.category.themeColor, 0.30f);
                background.color = baseColor;
            }

            if (categoryIcon != null)
            {
                bool hasIcon = question.category?.icon != null;
                categoryIcon.gameObject.SetActive(hasIcon);
                if (hasIcon) categoryIcon.sprite = question.category.icon;
            }

            if (checkmark != null) checkmark.gameObject.SetActive(false);
            State = TileState.Closed;
            if (button != null) button.interactable = true;
        }

        public void SetState(TileState state)
        {
            State = state;
            switch (state)
            {
                case TileState.Closed:
                    if (button != null) button.interactable = true;
                    if (checkmark != null) checkmark.gameObject.SetActive(false);
                    break;

                case TileState.Correct:
                    if (background != null) background.color = colorCorrect;
                    if (button != null) button.interactable = false;
                    if (checkmark != null) checkmark.gameObject.SetActive(true);
                    StartCoroutine(BounceScale());
                    break;

                case TileState.Wrong:
                    if (background != null) background.color = colorWrong;
                    if (button != null) button.interactable = false;
                    if (checkmark != null) checkmark.gameObject.SetActive(true);
                    StartCoroutine(BounceScale());
                    break;

                case TileState.Active:
                    if (background != null) background.color = colorActive;
                    if (button != null) button.interactable = false;
                    break;
            }
        }

        private void HandleClick()
        {
            if (State != TileState.Closed) return;
            OnTileClicked?.Invoke(this);
        }

        private IEnumerator BounceScale()
        {
            const float half = 0.075f;
            Vector3 normal = Vector3.one, small = Vector3.one * 0.88f;
            for (float t = 0; t < half; t += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(normal, small, t / half);
                yield return null;
            }
            for (float t = 0; t < half; t += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(small, normal, t / half);
                yield return null;
            }
            transform.localScale = normal;
        }
    }
}
