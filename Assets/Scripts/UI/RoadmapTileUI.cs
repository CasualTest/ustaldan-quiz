using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Data;

namespace UstAldanQuiz.UI
{
    public class RoadmapTileUI : MonoBehaviour
    {
        [SerializeField] private Button    button;
        [SerializeField] private Image     background;
        [SerializeField] private Image     categoryIcon;
        [SerializeField] private Image     checkmark;
        [SerializeField] private TMP_Text  indexLabel;

        [SerializeField] private Sprite spriteDefault;
        [SerializeField] private Sprite spriteCorrect;
        [SerializeField] private Sprite spriteWrong;

        [SerializeField] private Color colorUnanswered = new Color(0.91f, 0.88f, 0.82f);
        [SerializeField] private Color colorCorrect    = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color colorWrong      = new Color(0.80f, 0.18f, 0.14f);
        [SerializeField] private Color colorActive     = new Color(0.68f, 0.85f, 1.00f);

        public QuestionData Question { get; private set; }
        public TileState    State    { get; private set; }

        public event Action<RoadmapTileUI> OnTileClicked;

        private void Awake()    { button.onClick.AddListener(HandleClick); }
        private void OnDestroy() { button.onClick.RemoveAllListeners(); }

        public void Setup(QuestionData question, int tileIndex)
        {
            Question = question;

            if (categoryIcon != null)
            {
                bool hasIcon = question.category?.icon != null;
                categoryIcon.gameObject.SetActive(hasIcon);
                if (hasIcon) categoryIcon.sprite = question.category.icon;
            }

            if (indexLabel != null) indexLabel.text = (tileIndex + 1).ToString();
            if (checkmark  != null) checkmark.gameObject.SetActive(false);

            State = TileState.Closed;
            ApplySprite(spriteDefault, colorUnanswered);
            if (button != null) button.interactable = true;
        }

        public void SetState(TileState state, bool animate = true)
        {
            State = state;
            switch (state)
            {
                case TileState.Closed:
                    ApplySprite(spriteDefault, colorUnanswered);
                    if (button    != null) button.interactable = true;
                    if (checkmark != null) checkmark.gameObject.SetActive(false);
                    break;

                case TileState.Correct:
                    ApplySprite(spriteCorrect, colorCorrect);
                    if (button    != null) button.interactable = false;
                    if (checkmark != null) checkmark.gameObject.SetActive(false);
                    if (animate) StartCoroutine(BounceScale());
                    break;

                case TileState.Wrong:
                    ApplySprite(spriteWrong, colorWrong);
                    if (button    != null) button.interactable = false;
                    if (checkmark != null) checkmark.gameObject.SetActive(false);
                    if (animate) StartCoroutine(BounceScale());
                    break;

                case TileState.Active:
                    ApplySprite(spriteDefault, colorActive);
                    if (button != null) button.interactable = false;
                    break;
            }
        }

        private void ApplySprite(Sprite sprite, Color fallback)
        {
            if (background == null) return;
            if (sprite != null)
            {
                background.sprite = sprite;
                background.type   = Image.Type.Sliced;
                background.color  = Color.white;
            }
            else
            {
                background.color = fallback;
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
