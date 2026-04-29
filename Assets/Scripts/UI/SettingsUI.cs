using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Панель")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button     btnClose;

        [Header("Переключатели")]
        [SerializeField] private Toggle toggleMusic;
        [SerializeField] private Toggle toggleSound;
        [SerializeField] private Toggle toggleVibration;

        private static readonly Color ColorOn  = new Color(0.18f, 0.38f, 0.25f);
        private static readonly Color ColorOff = new Color(0.62f, 0.62f, 0.62f);

        private void Start()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            btnClose?.onClick.AddListener(Close);

            Bind(toggleMusic,     () => SettingsManager.MusicEnabled,
                                  v  => { SettingsManager.MusicEnabled     = v; AudioManager.Instance?.ApplyMusicSettings(); });
            Bind(toggleSound,     () => SettingsManager.SoundEnabled,
                                  v  => SettingsManager.SoundEnabled     = v);
            Bind(toggleVibration, () => SettingsManager.VibrationEnabled,
                                  v  => SettingsManager.VibrationEnabled = v);
        }

        private void OnDestroy()
        {
            btnClose?.onClick.RemoveAllListeners();
        }

        public void Open()  => settingsPanel?.SetActive(true);
        public void Close() => settingsPanel?.SetActive(false);

        // ── Приватные хелперы ──────────────────────────────────────────────

        private void Bind(Toggle toggle, Func<bool> getter, Action<bool> setter)
        {
            if (toggle == null) return;
            toggle.isOn = getter();
            Refresh(toggle, toggle.isOn);
            toggle.onValueChanged.AddListener(val => { Refresh(toggle, val); setter(val); });
        }

        private static void Refresh(Toggle toggle, bool isOn)
        {
            if (toggle.targetGraphic is Image img)
                img.color = isOn ? ColorOn : ColorOff;

            var lbl = toggle.GetComponentInChildren<TMP_Text>();
            if (lbl != null) lbl.text = isOn ? "Вкл" : "Выкл";
        }
    }
}
