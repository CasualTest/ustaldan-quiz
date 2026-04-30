using UnityEngine;
using System.Runtime.InteropServices;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.Utils
{
    /// <summary>
    /// Тактильный отклик. Проверяет SettingsManager.VibrationEnabled перед каждым вызовом.
    /// Android: VibrationEffect API (API 26+) / предустановленные эффекты (API 29+).
    /// iOS: UIImpactFeedbackGenerator через нативный плагин.
    /// </summary>
    public static class HapticManager
    {
        // ── Публичные методы ─────────────────────────────────────────────

        /// <summary>Лёгкий щелчок — нажатие кнопки.</summary>
        public static void LightTap()
        {
            if (!SettingsManager.VibrationEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(HapticType.Click);
#elif UNITY_IOS && !UNITY_EDITOR
            _HapticLight();
#endif
        }

        /// <summary>Средний отклик — правильный ответ.</summary>
        public static void Correct()
        {
            if (!SettingsManager.VibrationEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(HapticType.Tick);
#elif UNITY_IOS && !UNITY_EDITOR
            _HapticMedium();
#endif
        }

        /// <summary>Тяжёлый двойной импульс — неправильный ответ.</summary>
        public static void Wrong()
        {
            if (!SettingsManager.VibrationEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(HapticType.Heavy);
#elif UNITY_IOS && !UNITY_EDITOR
            _HapticHeavy();
#endif
        }

        // ── Android ──────────────────────────────────────────────────────

#if UNITY_ANDROID && !UNITY_EDITOR
        private enum HapticType { Click, Tick, Heavy }

        private const int PREDEFINED_CLICK       = 0;
        private const int PREDEFINED_TICK        = 2;
        private const int PREDEFINED_HEAVY_CLICK = 5;

        private static int? _api;
        private static int ApiLevel
        {
            get
            {
                if (_api == null)
                    using (var v = new AndroidJavaClass("android.os.Build$VERSION"))
                        _api = v.GetStatic<int>("SDK_INT");
                return _api.Value;
            }
        }

        private static void VibrateAndroid(HapticType type)
        {
            try
            {
                using var player   = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator == null) return;

                if (ApiLevel >= 29)
                {
                    // Предустановленные эффекты — наилучшее качество
                    int id;
                    switch (type)
                    {
                        case HapticType.Click: id = PREDEFINED_CLICK;       break;
                        case HapticType.Tick:  id = PREDEFINED_TICK;        break;
                        default:               id = PREDEFINED_HEAVY_CLICK; break;
                    }
                    using var vfClass = new AndroidJavaClass("android.os.VibrationEffect");
                    using var effect  = vfClass.CallStatic<AndroidJavaObject>("createPredefined", id);
                    vibrator.Call("vibrate", effect);
                }
                else if (ApiLevel >= 26)
                {
                    // VibrationEffect API
                    switch (type)
                    {
                        case HapticType.Click:
                            VibrateSingleShot(vibrator, 20L, 80);
                            break;
                        case HapticType.Tick:
                            VibrateSingleShot(vibrator, 35L, 110);
                            break;
                        default:
                            // Двойной импульс для "тяжёлого" ощущения
                            VibrateWaveform(vibrator,
                                new long[] { 0L, 60L, 40L, 70L },
                                new int[]  { 0,  200,  0,  235 });
                            break;
                    }
                }
                else
                {
                    // Fallback для старых устройств
                    long ms;
                    switch (type)
                    {
                        case HapticType.Click: ms = 20L;  break;
                        case HapticType.Tick:  ms = 35L;  break;
                        default:               ms = 120L; break;
                    }
                    vibrator.Call("vibrate", ms);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[HapticManager] " + e.Message);
            }
        }

        private static void VibrateSingleShot(AndroidJavaObject vibrator, long ms, int amplitude)
        {
            using var vfClass = new AndroidJavaClass("android.os.VibrationEffect");
            using var effect  = vfClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude);
            vibrator.Call("vibrate", effect);
        }

        private static void VibrateWaveform(AndroidJavaObject vibrator,
            long[] timings, int[] amplitudes)
        {
            using var vfClass = new AndroidJavaClass("android.os.VibrationEffect");
            using var effect  = vfClass.CallStatic<AndroidJavaObject>(
                "createWaveform", timings, amplitudes, -1);
            vibrator.Call("vibrate", effect);
        }
#endif

        // ── iOS ──────────────────────────────────────────────────────────

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void _HapticLight();
        [DllImport("__Internal")] static extern void _HapticMedium();
        [DllImport("__Internal")] static extern void _HapticHeavy();
#endif
    }
}
