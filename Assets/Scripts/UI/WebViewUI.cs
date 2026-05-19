using UnityEngine;

namespace UstAldanQuiz.UI
{
    // Всегда активен (нужен для UnitySendMessage от нативного слоя).
    public class WebViewUI : MonoBehaviour
    {
        [SerializeField] private string url       = "https://100.uacbs.ru";
        [SerializeField] private int    marginTop = 100; // canvas units (высота шапки)

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _plugin;
#endif

        private void OnDestroy()
        {
            DestroyPlugin();
        }

        public void Open() => Open(url);

        public void Open(string targetUrl)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            EnsurePlugin();
            _plugin.Call("Show", targetUrl, CalcHeaderPx());
#else
            Application.OpenURL(targetUrl);
#endif
        }

        // Вызывается Java через UnitySendMessage
        public void OnNativeClose(string _)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _plugin?.Call("Hide");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void EnsurePlugin()
        {
            if (_plugin != null) return;
            AndroidJavaClass  player   = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            _plugin = new AndroidJavaObject("com.ustaldanquiz.plugin.WebViewPlugin", activity, gameObject.name);
        }

        private int CalcHeaderPx()
        {
            Canvas canvas    = GetComponentInParent<Canvas>();
            float  scale     = canvas != null ? canvas.scaleFactor : 1f;
            int    statusBar = Screen.height - (int)Screen.safeArea.yMax;
            return statusBar + Mathf.RoundToInt(marginTop * scale);
        }

        private void DestroyPlugin()
        {
            if (_plugin == null) return;
            _plugin.Call("Destroy");
            _plugin.Dispose();
            _plugin = null;
        }
#else
        private void DestroyPlugin() { }
#endif
    }
}
