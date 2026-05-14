using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UstAldanQuiz.Utils
{
    /// <summary>
    /// Сбрасывает overrideSprite кнопки если указатель отпущен за её пределами.
    /// Unity EventSystem шлёт PointerUp на оригинальную кнопку в любом случае,
    /// но Selectable переходит в Selected-состояние, которое может держать pressedSprite.
    /// Этот компонент явно обнуляет overrideSprite при отпускании вне кнопки.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonDragReset : MonoBehaviour,
        IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        private Button _btn;
        private bool _isOver;

        void Awake() => _btn = GetComponent<Button>();

        public void OnPointerEnter(PointerEventData _) => _isOver = true;
        public void OnPointerExit(PointerEventData _)  => _isOver = false;

        public void OnPointerUp(PointerEventData _)
        {
            if (!_isOver && _btn.targetGraphic is Image img)
                img.overrideSprite = null;
        }
    }
}
