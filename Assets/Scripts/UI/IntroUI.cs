using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

namespace UstAldanQuiz.UI
{
    public class IntroUI : MonoBehaviour
    {
        [SerializeField] VideoPlayer videoPlayer;
        [SerializeField] RawImage    videoDisplay;
        [SerializeField] VideoClip   introClip;
        [SerializeField] string      nextSceneName = "MainMenu";

        RenderTexture _rt;
        bool _loading;

        void Start()
        {
            _rt = new RenderTexture(Screen.width, Screen.height, 0);
            videoPlayer.targetTexture = _rt;
            videoDisplay.texture = _rt;

            if (introClip != null)
                videoPlayer.clip = introClip;

            videoPlayer.loopPointReached += _ => Skip();
            videoPlayer.Play();
        }

        void Update()
        {
            if (_loading) return;

            bool tapped = Input.GetMouseButtonDown(0) ||
                          (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            if (tapped) Skip();
        }

        void Skip()
        {
            if (_loading) return;
            _loading = true;
            videoPlayer.Stop();
            SceneManager.LoadScene(nextSceneName);
        }

        void OnDestroy()
        {
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
            }
        }
    }
}
