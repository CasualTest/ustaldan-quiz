using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.UI
{
    public class ResultsUI : MonoBehaviour
    {
        [Header("Заголовок")]
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private TMP_Text subtitleText;

        [Header("Главный счёт")]
        [SerializeField] private TMP_Text scoreBigText;

        [Header("Статистика — значения")]
        [SerializeField] private TMP_Text totalCountText;
        [SerializeField] private TMP_Text correctCountText;
        [SerializeField] private TMP_Text wrongCountText;
        [SerializeField] private TMP_Text avgTimeText;

        [Header("Темы — контейнер строк")]
        [SerializeField] private Transform categoriesContent;
        [SerializeField] private GameObject categoryRowPrefab;

        [Header("Лучший результат")]
        [SerializeField] private TMP_Text bestScoreText;

        [Header("Кнопки")]
        [SerializeField] private Button btnPlayAgain;
        [SerializeField] private Button btnMainMenu;
        [SerializeField] private Button btnShare;

        private static readonly Color ProgressFillColor = new Color(0.78f, 0.66f, 0.29f);
        private static readonly Color ProgressBgColor   = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color RowTextColor      = new Color(0.10f, 0.16f, 0.10f);

        private void Start()
        {
            var gm = GameManager.Instance;
            int correct  = gm != null ? gm.CorrectAnswers : 0;
            int total    = gm != null ? gm.TotalQuestions : 0;
            int answered = gm != null && gm.AnswerLogs != null ? gm.AnswerLogs.Count : total;
            int wrong    = Mathf.Max(0, answered - correct);

            if (resultTitle != null)
                resultTitle.text = LocaleManager.Get("results_title");
            if (subtitleText != null)
                subtitleText.text = GetSubtitle(correct, total);

            if (scoreBigText != null)
                scoreBigText.text = $"{correct}/{total}";

            if (totalCountText   != null) totalCountText.text   = total.ToString();
            if (correctCountText != null) correctCountText.text = correct.ToString();
            if (wrongCountText   != null) wrongCountText.text   = wrong.ToString();

            float avgTime = ComputeAvgTime();
            if (avgTimeText != null)
                avgTimeText.text = LocaleManager.Get("avg_time_format", avgTime.ToString("F1"));

            FillCategories();

            string bestKey = GetBestKey();
            int prevBest   = SaveManager.GetBestScore(bestKey);
            bool isNewBest = correct > prevBest;
            if (isNewBest) SaveManager.SetBestScore(bestKey, correct);
            int showBest = isNewBest ? correct : prevBest;
            if (bestScoreText != null)
                bestScoreText.text = LocaleManager.Get("result_best_score", showBest, total);

            btnPlayAgain?.onClick.AddListener(HandlePlayAgain);
            btnMainMenu?.onClick.AddListener(() => GameManager.Instance?.LoadScene("MainMenu"));
            btnShare?.onClick.AddListener(HandleShare);
        }

        private void OnDestroy()
        {
            btnPlayAgain?.onClick.RemoveAllListeners();
            btnMainMenu?.onClick.RemoveAllListeners();
            btnShare?.onClick.RemoveAllListeners();
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static string GetSubtitle(int correct, int total)
        {
            if (total == 0) return "—";
            float pct = (float)correct / total;
            if (pct < 0.40f) return LocaleManager.Get("result_poor");
            if (pct < 0.70f) return LocaleManager.Get("result_ok");
            if (pct < 0.90f) return LocaleManager.Get("result_good");
            return LocaleManager.Get("result_great");
        }

        private static string GetBestKey()
        {
            var gm = GameManager.Instance;
            if (gm == null) return "";
            if (gm.CurrentMode == GameMode.Millionaire) return "_millionaire";
            if (gm.CurrentMode == GameMode.Roadmap)     return "_roadmap";
            return gm.SelectedCategory?.categoryId ?? "";
        }

        private static float ComputeAvgTime()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.AnswerLogs == null || gm.AnswerLogs.Count == 0) return 0f;
            float sum = 0f;
            foreach (var log in gm.AnswerLogs) sum += log.timeSec;
            return sum / gm.AnswerLogs.Count;
        }

        private void FillCategories()
        {
            if (categoriesContent == null) return;
            foreach (Transform t in categoriesContent) Destroy(t.gameObject);

            var gm = GameManager.Instance;
            if (gm == null || gm.AnswerLogs == null || gm.AnswerLogs.Count == 0) return;

            var perCat = new Dictionary<string, (string name, int correct, int total)>();
            foreach (var log in gm.AnswerLogs)
            {
                string key = string.IsNullOrEmpty(log.categoryId) ? "_other" : log.categoryId;
                string name = string.IsNullOrEmpty(log.categoryName) ? LocaleManager.Get("category_other") : log.categoryName;
                perCat.TryGetValue(key, out var prev);
                perCat[key] = (name, prev.correct + (log.isCorrect ? 1 : 0), prev.total + 1);
            }

            foreach (var kv in perCat)
                SpawnCategoryRow(kv.Value.name, kv.Value.correct, kv.Value.total);
        }

        private void SpawnCategoryRow(string name, int correct, int total)
        {
            GameObject row;
            if (categoryRowPrefab != null)
            {
                row = Instantiate(categoryRowPrefab, categoriesContent);
                row.SetActive(true);
            }
            else
            {
                row = BuildCategoryRow();
                row.transform.SetParent(categoriesContent, false);
            }

            var nameTMP  = row.transform.Find("Name")?.GetComponent<TMP_Text>();
            var scoreTMP = row.transform.Find("Score")?.GetComponent<TMP_Text>();
            var fill     = row.transform.Find("Progress/Fill") as RectTransform;

            if (nameTMP  != null) nameTMP.text  = name;
            if (scoreTMP != null) scoreTMP.text = $"{correct}/{total}";
            if (fill     != null)
            {
                float k = total > 0 ? Mathf.Clamp01((float)correct / total) : 0f;
                fill.anchorMin = new Vector2(0, 0);
                fill.anchorMax = new Vector2(k, 1);
                fill.offsetMin = fill.offsetMax = Vector2.zero;
            }
        }

        // Fallback — программное создание строки если нет префаба
        private GameObject BuildCategoryRow()
        {
            var row = new GameObject("CategoryRow", typeof(RectTransform));
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.minHeight = rowLE.preferredHeight = 50;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.spacing = 16;

            var nameGO = new GameObject("Name", typeof(RectTransform));
            nameGO.transform.SetParent(row.transform, false);
            var nameLE = nameGO.AddComponent<LayoutElement>();
            nameLE.minWidth = 160; nameLE.preferredWidth = 160;
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.fontSize = 28; nameTMP.color = RowTextColor;
            nameTMP.alignment = TextAlignmentOptions.MidlineLeft;

            var progressGO = new GameObject("Progress", typeof(RectTransform));
            progressGO.transform.SetParent(row.transform, false);
            var progressLE = progressGO.AddComponent<LayoutElement>();
            progressLE.flexibleWidth = 1; progressLE.minHeight = 14; progressLE.preferredHeight = 14;
            var bgImg = progressGO.AddComponent<Image>();
            bgImg.color = ProgressBgColor;

            var fillGO = new GameObject("Fill", typeof(RectTransform));
            fillGO.transform.SetParent(progressGO.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            fillGO.AddComponent<Image>().color = ProgressFillColor;

            var scoreGO = new GameObject("Score", typeof(RectTransform));
            scoreGO.transform.SetParent(row.transform, false);
            var scoreLE = scoreGO.AddComponent<LayoutElement>();
            scoreLE.minWidth = 60;
            var scoreTMP = scoreGO.AddComponent<TextMeshProUGUI>();
            scoreTMP.fontSize = 28; scoreTMP.color = RowTextColor;
            scoreTMP.alignment = TextAlignmentOptions.MidlineRight;
            scoreTMP.fontStyle = FontStyles.Bold;

            return row;
        }

        private void HandlePlayAgain()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.PrepareNewSession();
            string targetScene = gm.CurrentMode switch
            {
                GameMode.Millionaire => "Millionaire",
                GameMode.Roadmap     => "Roadmap",
                _                    => "QuestionMap"
            };
            gm.LoadScene(targetScene);
        }

        private void HandleShare()
        {
            var gm = GameManager.Instance;
            int correct    = gm != null ? gm.CorrectAnswers  : 0;
            int shareTotal = gm != null ? gm.TotalQuestions  : 0;
            string text = LocaleManager.Get("result_share_text", correct, shareTotal);

#if UNITY_ANDROID && !UNITY_EDITOR
            ShareAndroid(text);
#elif UNITY_IOS && !UNITY_EDITOR
            GUIUtility.systemCopyBuffer = text;
#else
            GUIUtility.systemCopyBuffer = text;
            Debug.Log("[ResultsUI] Текст скопирован: " + text);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void ShareAndroid(string text)
        {
            var intentClass  = new AndroidJavaClass("android.content.Intent");
            var intentObject = new AndroidJavaObject("android.content.Intent");
            intentObject.Call<AndroidJavaObject>("setAction",
                intentClass.GetStatic<string>("ACTION_SEND"));
            intentObject.Call<AndroidJavaObject>("setType", "text/plain");
            intentObject.Call<AndroidJavaObject>("putExtra",
                intentClass.GetStatic<string>("EXTRA_TEXT"), text);
            var unity    = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
            var chooser  = intentClass.CallStatic<AndroidJavaObject>(
                "createChooser", intentObject, LocaleManager.Get("btn_share"));
            activity.Call("startActivity", chooser);
        }
#endif
    }
}
