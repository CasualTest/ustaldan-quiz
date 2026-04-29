using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Data;

namespace UstAldanQuiz.UI
{
    public enum TileState { Closed, Correct, Wrong, Active }

    /// <summary>
    /// Компонент плитки на карте вопросов.
    /// Повесить на корневой объект QuestionTile prefab.
    /// </summary>
    public class QuestionTileUI : MonoBehaviour
    {
        [Header("Компоненты плитки")]
        [SerializeField] private Button button;
        [SerializeField] private Image tileBackground;
        [SerializeField] private TMP_Text tileNumber;
        [SerializeField] private Image tileCategoryIcon;
        [SerializeField] private Image tileCheckmark;

        [Header("Спрайты (необязательно)")]
        [SerializeField] private Sprite checkmarkSprite;
        [SerializeField] private Sprite crossSprite;

        [Header("Цвета состояний")]
        [SerializeField] private Color colorClosed  = new Color(0.91f, 0.88f, 0.82f);
        [SerializeField] private Color colorCorrect = new Color(0.78f, 0.90f, 0.79f);
        [SerializeField] private Color colorWrong   = new Color(1.00f, 0.80f, 0.82f);
        [SerializeField] private Color colorActive  = new Color(0.68f, 0.85f, 1.00f);

        public QuestionData Question { get; private set; }
        public TileState State       { get; private set; }

        /// <summary>Вызывается когда игрок тапает на закрытую плитку.</summary>
        public event Action<QuestionTileUI> OnTileClicked;

        private void Awake()
        {
            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveAllListeners();
        }

        /// <summary>Инициализировать плитку вопросом и порядковым номером.</summary>
        public void Setup(QuestionData question, int number)
        {
            Question = question;

            if (tileNumber != null)
                tileNumber.text = number.ToString();

            // Иконка категории (если задана)
            bool hasIcon = question.category != null && question.category.icon != null;
            if (tileCategoryIcon != null)
            {
                tileCategoryIcon.gameObject.SetActive(hasIcon);
                if (hasIcon) tileCategoryIcon.sprite = question.category.icon;
            }

            SetState(TileState.Closed);
        }

        public void SetState(TileState state)
        {
            State = state;

            switch (state)
            {
                case TileState.Closed:
                    tileBackground.color  = colorClosed;
                    button.interactable   = true;
                    if (tileCheckmark != null) tileCheckmark.gameObject.SetActive(false);
                    break;

                case TileState.Correct:
                    tileBackground.color = colorCorrect;
                    button.interactable  = false;
                    ShowCheckmark(correct: true);
                    StartCoroutine(BounceScale());
                    break;

                case TileState.Wrong:
                    tileBackground.color = colorWrong;
                    button.interactable  = false;
                    ShowCheckmark(correct: false);
                    StartCoroutine(BounceScale());
                    break;

                case TileState.Active:
                    tileBackground.color = colorActive;
                    button.interactable  = false;
                    break;
            }
        }

        private void ShowCheckmark(bool correct)
        {
            if (tileCheckmark == null) return;
            tileCheckmark.gameObject.SetActive(true);
            if (correct  && checkmarkSprite != null) tileCheckmark.sprite = checkmarkSprite;
            if (!correct && crossSprite     != null) tileCheckmark.sprite = crossSprite;
        }

        private void HandleClick()
        {
            if (State != TileState.Closed) return;
            OnTileClicked?.Invoke(this);
        }

        // Лёгкое сжатие и возврат при смене цвета
        private IEnumerator BounceScale()
        {
            const float half = 0.075f;
            Vector3 normal = Vector3.one;
            Vector3 small  = Vector3.one * 0.9f;

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
