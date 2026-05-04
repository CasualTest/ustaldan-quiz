using System;
using System.Text;
using TMPro;
using UstAldanQuiz.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UstAldanQuiz.UI
{
    public class SuggestQuestionUI : BaseWindow
    {
        [Header("Поля ввода")]
        [SerializeField] private TMP_InputField questionRuField;
        [SerializeField] private TMP_InputField questionSahField;
        [SerializeField] private TMP_InputField answer1Field;
        [SerializeField] private TMP_InputField answer2Field;
        [SerializeField] private TMP_InputField answer3Field;
        [SerializeField] private TMP_InputField answer4Field;
        [SerializeField] private TMP_InputField factRuField;
        [SerializeField] private TMP_InputField factSahField;

        [Header("Кнопки")]
        [SerializeField] private Button btnSend;

        [Header("Скролл")]
        [SerializeField] private RectTransform scrollContent;

        [Header("Валидация")]
        [SerializeField] private TMP_Text errorText;

        private const string Email = "uacbsborsupp1@gmail.com";

        protected override void OnWindowStart()
        {
            btnSend?.onClick.AddListener(Send);
        }

        protected override void OnWindowDestroy()
        {
            btnSend?.onClick.RemoveAllListeners();
        }

        public override void Open()
        {
            ClearFields();
            if (scrollContent != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
            base.Open();
        }

        private void Send()
        {
            if (!ValidateFields()) return;
            string subject = Uri.EscapeDataString(LocaleManager.Get("suggest_email_subject"));
            string body    = Uri.EscapeDataString(BuildBody());
            Application.OpenURL($"mailto:{Email}?subject={subject}&body={body}");
        }

        private bool ValidateFields()
        {
            TMP_InputField[] required = { questionRuField, answer1Field, answer2Field, answer3Field, answer4Field };
            bool ok = System.Array.TrueForAll(required, f => f != null && !string.IsNullOrWhiteSpace(f.text));
            if (errorText != null)
            {
                errorText.text = LocaleManager.Get("suggest_error");
                errorText.gameObject.SetActive(!ok);
                if (scrollContent != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
            }
            return ok;
        }

        private string BuildBody()
        {
            var sb = new StringBuilder();
            AppendField(sb, "Вопрос (RU)",  questionRuField);
            AppendField(sb, "Вопрос (SAH)", questionSahField);
            sb.AppendLine();
            AppendField(sb, "Правильный ответ", answer1Field);
            AppendField(sb, "Вариант 2",        answer2Field);
            AppendField(sb, "Вариант 3",        answer3Field);
            AppendField(sb, "Вариант 4",        answer4Field);
            sb.AppendLine();
            AppendField(sb, "Факт (RU)",  factRuField);
            AppendField(sb, "Факт (SAH)", factSahField);
            return sb.ToString().TrimEnd();
        }

        private static void AppendField(StringBuilder sb, string label, TMP_InputField field)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.text)) return;
            sb.AppendLine($"{label}: {field.text}");
        }

        private void ClearFields()
        {
            TMP_InputField[] fields = { questionRuField, questionSahField,
                                        answer1Field, answer2Field, answer3Field, answer4Field,
                                        factRuField, factSahField };
            foreach (var f in fields)
                if (f != null) f.text = "";
            if (errorText != null) errorText.gameObject.SetActive(false);
        }
    }
}
