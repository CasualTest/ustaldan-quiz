using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UstAldanQuiz.Managers
{
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition Instance { get; private set; }

        private CanvasGroup _fade;
        private bool        _busy;

        private const float FadeDuration = 0.35f;

        // Создаётся автоматически до загрузки первой сцены
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoCreate()
        {
            var go = new GameObject("[SceneTransition]");
            go.AddComponent<SceneTransition>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            BuildFadeOverlay();
            // Первая сцена появляется из чёрного
            StartCoroutine(FadeIn());
        }

        // ── Публичный API ─────────────────────────────────────────────────────

        public void LoadScene(string sceneName)
        {
            if (!_busy) StartCoroutine(Transition(sceneName));
        }

        // ── Переход ───────────────────────────────────────────────────────────

        private IEnumerator Transition(string sceneName)
        {
            _busy = true;
            _fade.blocksRaycasts = true;

            yield return Fade(0f, 1f);

            var op = SceneManager.LoadSceneAsync(sceneName);
            yield return op;

            yield return Fade(1f, 0f);

            _fade.blocksRaycasts = false;
            _busy = false;
        }

        private IEnumerator FadeIn()
        {
            _fade.alpha = 1f;
            yield return Fade(1f, 0f);
        }

        private IEnumerator Fade(float from, float to)
        {
            _fade.alpha = from;
            for (float t = 0; t < FadeDuration; t += Time.unscaledDeltaTime)
            {
                _fade.alpha = Mathf.Lerp(from, to, t / FadeDuration);
                yield return null;
            }
            _fade.alpha = to;
        }

        // ── Создание оверлея ──────────────────────────────────────────────────

        private void BuildFadeOverlay()
        {
            // Canvas поверх всего
            var canvasGO = new GameObject("FadeCanvas");
            canvasGO.transform.SetParent(transform);
            DontDestroyOnLoad(canvasGO);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasGO.AddComponent<CanvasScaler>();

            // Чёрный экран
            var imgGO = new GameObject("FadeImage");
            imgGO.transform.SetParent(canvasGO.transform, false);
            imgGO.AddComponent<Image>().color = Color.black;
            var rt = imgGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            _fade = canvasGO.AddComponent<CanvasGroup>();
            _fade.alpha          = 1f;
            _fade.blocksRaycasts = false;
            _fade.interactable   = false;
        }
    }
}
