using UnityEngine;

namespace UstAldanQuiz.Managers
{
    /// <summary>
    /// Хранит пользовательские настройки в PlayerPrefs.
    /// Доступ через статические свойства.
    /// </summary>
    public static class SettingsManager
    {
        private const string K_MUSIC     = "s_music";
        private const string K_SOUND     = "s_sound";
        private const string K_VIBRO     = "s_vibro";
        private const string K_MUSIC_VOL = "s_music_vol";
        private const string K_SOUND_VOL = "s_sound_vol";

        public static bool MusicEnabled
        {
            get => PlayerPrefs.GetInt(K_MUSIC, 1) == 1;
            set { PlayerPrefs.SetInt(K_MUSIC, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool SoundEnabled
        {
            get => PlayerPrefs.GetInt(K_SOUND, 1) == 1;
            set { PlayerPrefs.SetInt(K_SOUND, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool VibrationEnabled
        {
            get => PlayerPrefs.GetInt(K_VIBRO, 1) == 1;
            set { PlayerPrefs.SetInt(K_VIBRO, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(K_MUSIC_VOL, 1f);
            set { PlayerPrefs.SetFloat(K_MUSIC_VOL, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public static float SoundVolume
        {
            get => PlayerPrefs.GetFloat(K_SOUND_VOL, 1f);
            set { PlayerPrefs.SetFloat(K_SOUND_VOL, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }
    }
}
