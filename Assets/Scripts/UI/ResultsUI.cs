using System.Collections;
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

        [Header("Счёт")]
        [SerializeField] private TMP_Text scoreCircleText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestScoreText;

        [Header("Звёзды (3 Image, слева направо)")]
        [SerializeField] private Image[] stars = new Image[3];
        [SerializeField] private Color starActive   = new Color(0.78f, 0.66f, 0.29f);
        [SerializeField] private Color starInactive = new Color(0.75f, 0.75f, 0.75f);

        [Header("Новый рекорд")]
        [SerializeField] private GameObject newBestBadge;

        [Header("Кнопки")]
        [SerializeField] private Button btnPlayAgain;
        [SerializeField] private Button btnMainMenu;
        [SerializeField] private Button btnShare;

        private void Start()
        {
            var gm = GameManager.Instance;
            int correct  = gm != null ? gm.CorrectAnswers  : 0;
            int total    = gm != null ? gm.TotalQuestions   : 0;
            string catId = gm?.SelectedCategory?.categoryId ?? "";

            if (resultTitle != null)
                resultTitle.text = GetTitle(correct, total);

            if (scoreCircleText != null)
                scoreCircleText.text = $"{correct}/{total}";
            if (scoreText != null)
                scoreText.text = LocaleManager.Get("result_score_detail", correct, total);

            int prevBest  = SaveManager.GetBestScore(catId);
            bool isNewBest = correct > prevBest;
            if (isNewBest) SaveManager.SetBestScore(catId, correct);
            int showBest = isNewBest ? correct : prevBest;
            if (bestScoreText != null)
                bestScoreText.text = LocaleManager.Get("result_best_score", showBest, total);

            if (newBestBadge != null)
            {
                newBestBadge.SetActive(isNewBest);
                if (isNewBest) StartCoroutine(AnimateBadge());
            }

            ShowStars(correct, total);

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

        // ─────────────────────────────────────────────────────────────────

        private static string GetTitle(int correct, int total)
        {
            if (total == 0) return "—";
            float pct = (float)correct / total;
            if (pct < 0.40f) return LocaleManager.Get("result_poor");
            if (pct < 0.70f) return LocaleManager.Get("result_ok");
            if (pct < 0.90f) return LocaleManager.Get("result_good");
            return LocaleManager.Get("result_great");
        }

        private void ShowStars(int correct, int total)
        {
            if (stars == null || stars.Length < 3 || total == 0) return;
            float pct      = (float)correct / total;
            int   starCount = pct < 0.50f ? 1 : pct < 0.80f ? 2 : 3;
            for (int i = 0; i < stars.Length; i++)
                if (stars[i] != null)
                    stars[i].color = i < starCount ? starActive : starInactive;
        }

        private void HandlePlayAgain()
        {
            GameManager.Instance?.PrepareNewSession();
            GameManager.Instance?.LoadScene("QuestionMap");
        }

        private void HandleShare()
        {
            var gm = GameManager.Instance;
            int correct    = gm != null ? gm.CorrectAnswers  : 0;
            int shareTotal = gm != null ? gm.TotalQuestions  : 0;
            string text = LocaleManager.Get("result_share_text", correct, shareTotal);

#if UNITY_ANDROID
            ShareAndroid(text);
#elif UNITY_IOS
            ShareIOS(text);
#else
            GUIUtility.systemCopyBuffer = text;
            Debug.Log("[ResultsUI] Текст скопирован: " + text);
#endif
        }

#if UNITY_ANDROID
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

#if UNITY_IOS
        private static void ShareIOS(string text)
        {
            GUIUtility.systemCopyBuffer = text;
        }
#endif

        private IEnumerator AnimateBadge()
        {
            if (newBestBadge == null) yield break;
            var rt = newBestBadge.GetComponent<RectTransform>();
            if (rt == null) yield break;
            rt.localScale = Vector3.zero;
            const float up = 0.15f, down = 0.08f;
            for (float t = 0; t < up;   t += Time.deltaTime)
            { rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.25f, t / up); yield return null; }
            for (float t = 0; t < down; t += Time.deltaTime)
            { rt.localScale = Vector3.Lerp(Vector3.one * 1.25f, Vector3.one, t / down); yield return null; }
            rt.localScale = Vector3.one;
        }
    }
}
