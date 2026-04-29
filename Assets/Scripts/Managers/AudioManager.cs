using UnityEngine;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.Managers
{
    /// <summary>
    /// Singleton. Живёт между сценами.
    /// Управляет фоновой музыкой и SFX.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Источники звука")]
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource sfxSource;

        [Header("Музыка")]
        [SerializeField] AudioClip backgroundMusic;

        [Header("SFX")]
        [SerializeField] AudioClip buttonClickClip;
        [SerializeField] AudioClip correctAnswerClip;
        [SerializeField] AudioClip wrongAnswerClip;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (backgroundMusic != null && musicSource != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.loop = true;
                musicSource.Play();
            }
            ApplyMusicSettings();
        }

        // ── Музыка ────────────────────────────────────────────────────────

        /// <summary>Применяет текущее состояние SettingsManager.MusicEnabled.</summary>
        public void ApplyMusicSettings()
        {
            if (musicSource == null) return;
            musicSource.mute = !SettingsManager.MusicEnabled;
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
            ApplyMusicSettings();
        }

        public void StopMusic()
        {
            musicSource?.Stop();
        }

        // ── SFX ───────────────────────────────────────────────────────────

        public void PlaySFX(AudioClip clip)
        {
            if (!SettingsManager.SoundEnabled || clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void PlayButtonClick()    => PlaySFX(buttonClickClip);
        public void PlayCorrect()        => PlaySFX(correctAnswerClip);
        public void PlayWrong()          => PlaySFX(wrongAnswerClip);
    }
}
