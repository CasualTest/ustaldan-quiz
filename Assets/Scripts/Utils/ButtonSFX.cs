using UnityEngine;
using UnityEngine.UI;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.Utils
{
    /// <summary>
    /// Повесить на любую кнопку — при нажатии играет звук клика.
    /// customClip переопределяет стандартный звук из AudioManager.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonSFX : MonoBehaviour
    {
        [SerializeField] AudioClip customClip;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Play);
        }

        private void OnDestroy()
        {
            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveListener(Play);
        }

        private void Play()
        {
            HapticManager.LightTap();
            if (customClip != null)
                AudioManager.Instance?.PlaySFX(customClip);
            else
                AudioManager.Instance?.PlayButtonClick();
        }
    }
}
