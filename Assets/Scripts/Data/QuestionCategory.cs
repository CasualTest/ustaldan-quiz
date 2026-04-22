using UnityEngine;

namespace UstAldanQuiz.Data
{
    /// <summary>
    /// Категория вопросов: История, Культура, Люди и т.д.
    /// Создание: ПКМ в Project → Create → UstAldan Quiz → Category
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewCategory",
        menuName = "UstAldan Quiz/Category",
        order = 0)]
    public class QuestionCategory : ScriptableObject
    {
        [Tooltip("Уникальный ID категории, например: history, culture, people")]
        public string categoryId;

        [Tooltip("Отображаемое имя, например: История")]
        public string displayName;

        [Tooltip("Короткое описание категории")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Иконка категории (опционально)")]
        public Sprite icon;

        [Tooltip("Цвет категории для UI (опционально)")]
        public Color themeColor = Color.white;
    }
}
