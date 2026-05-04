using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.UI
{
    public class AboutUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button     btnClose;
        [SerializeField] private TMP_Text   titleText;
        [SerializeField] private TMP_Text   bodyText;

        private AboutData _data;

        private void Start()
        {
            _data = AboutData.Load();
            if (panel != null) panel.SetActive(false);
            btnClose?.onClick.AddListener(Close);
            LocaleManager.OnLanguageChanged += Refresh;
        }

        private void OnDestroy()
        {
            btnClose?.onClick.RemoveAllListeners();
            LocaleManager.OnLanguageChanged -= Refresh;
        }

        public void Open()
        {
            Refresh();
            panel?.SetActive(true);
        }

        public void Close() => panel?.SetActive(false);

        private void Refresh()
        {
            if (_data == null) return;

            if (titleText != null)
                titleText.text = L(_data.title);

            if (bodyText != null)
                bodyText.text = BuildBody();
        }

        private string BuildBody()
        {
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

            return sb.ToString().TrimEnd();
        }

        static string L(string key) =>
            string.IsNullOrEmpty(key) ? "" : LocaleManager.Get(key);

        static void Append(StringBuilder sb, string text)
        {
            if (!string.IsNullOrEmpty(text)) sb.AppendLine(text);
        }
    }
}
