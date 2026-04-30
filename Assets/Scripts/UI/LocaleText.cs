using UnityEngine;
using TMPro;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.UI
{
    /// <summary>
    /// Добавить на объект с TMP_Text. Автоматически применяет локализованную строку
    /// и обновляется при смене языка.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocaleText : MonoBehaviour
    {
        [SerializeField] private string key;

        private TMP_Text _tmp;

        private void Awake()  => _tmp = GetComponent<TMP_Text>();

        private void Start()
        {
            Apply();
            LocaleManager.OnLanguageChanged += Apply;
        }

        private void OnDestroy() => LocaleManager.OnLanguageChanged -= Apply;

        public void Apply()
        {
            if (_tmp == null) _tmp = GetComponent<TMP_Text>();
            if (!string.IsNullOrEmpty(key))
                _tmp.text = LocaleManager.Get(key);
        }

        /// <summary>Установить ключ программно (из GameSceneBuilder).</summary>
        public void SetKey(string locKey)
        {
            key = locKey;
            if (_tmp != null) Apply();
        }
    }
}
