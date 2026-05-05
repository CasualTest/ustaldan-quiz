using System.Text;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UstAldanQuiz.UI
{
    public class AboutPageUI : MonoBehaviour
    {
        [Header("Контент")]
        [SerializeField] private TMP_Text bodyText;

        [Header("Предложить вопрос")]
        [SerializeField] private Button            btnSuggest;
        [SerializeField] private SuggestQuestionUI suggestUI;

        private AboutData _data;

        private void Start()
        {
            _data = AboutData.Load();
            Refresh();
            LocaleManager.OnLanguageChanged += Refresh;
            btnSuggest?.onClick.AddListener(() => suggestUI?.Open());
        }

        private void OnDestroy()
        {
            LocaleManager.OnLanguageChanged -= Refresh;
            btnSuggest?.onClick.RemoveAllListeners();
        }

        private void Refresh()
        {
            if (_data == null || bodyText == null) return;
            var sb = new StringBuilder();

            Append(sb, L(_data.description));
            Append(sb, L(_data.developer));

            if (!string.IsNullOrEmpty(_data.version))
                sb.AppendLine($"v{_data.version}  •  {_data.year}");

            if (_data.contacts?.Count > 0)
            {
                sb.AppendLine();
                foreach (var c in _data.contacts)
                    if (!string.IsNullOrEmpty(c.value))
                        sb.AppendLine($"{L(c.label)}: {c.value}");
            }

            if (_data.partners?.Count > 0)
            {
                sb.AppendLine();
                foreach (var key in _data.partners)
                    Append(sb, L(key));
            }

            bodyText.text = sb.ToString().TrimEnd();
        }

        static string L(string key) => string.IsNullOrEmpty(key) ? "" : LocaleManager.Get(key);
        static void Append(StringBuilder sb, string text) { if (!string.IsNullOrEmpty(text)) sb.AppendLine(text); }
    }
}
