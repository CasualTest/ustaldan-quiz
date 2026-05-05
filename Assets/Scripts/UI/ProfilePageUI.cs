using System.Text;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;
using UnityEngine;

namespace UstAldanQuiz.UI
{
    public class ProfilePageUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private QuestionDatabase questionDatabase;

        private void Start()
        {
            Refresh();
            LocaleManager.OnLanguageChanged += Refresh;
        }

        private void OnDestroy()
        {
            LocaleManager.OnLanguageChanged -= Refresh;
        }

        private void Refresh()
        {
            if (bodyText == null) return;
            var sb = new StringBuilder();

            int played = SaveManager.TotalPlayed;
            sb.AppendLine(LocaleManager.Get("stats_format", played, 0, 0)
                          .Split(new[] { '\n' }, 2)[0].Trim());

            if (questionDatabase != null)
            {
                sb.AppendLine();
                foreach (var cat in questionDatabase.categories)
                {
                    if (cat == null) continue;
                    int total = questionDatabase.GetQuestionsByCategory(cat).Count;
                    int best  = SaveManager.GetBestScore(cat.categoryId);
                    sb.AppendLine($"{cat.displayName}:  {best} / {total}");
                }
            }

            bodyText.text = sb.ToString().TrimEnd();
        }
    }
}
