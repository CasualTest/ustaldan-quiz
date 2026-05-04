using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.UI
{
    public class SettingsUI : BaseWindow
    {
        [Header("Переключатели")]
        [SerializeField] private Toggle toggleMusic;
        [SerializeField] private Toggle toggleSound;
        [SerializeField] private Toggle toggleVibration;

        [Header("Ползунки громкости")]
        [SerializeField] private Slider sliderMusic;
        [SerializeField] private Slider sliderSound;

        [Header("Язык")]
        [SerializeField] private Button btnLangRu;
        [SerializeField] private Button btnLangSah;

        private static readonly Color ColorOn       = new Color(0.18f, 0.38f, 0.25f);
        private static readonly Color ColorOff      = new Color(0.62f, 0.62f, 0.62f);
        private static readonly Color ColorLangOn   = new Color(0.18f, 0.38f, 0.25f);
        private static readonly Color ColorLangOff  = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color ColorLangText = new Color(0.10f, 0.16f, 0.10f);

        protected override void OnWindowStart()
        {
            Bind(toggleMusic,     () => SettingsManager.MusicEnabled,
                                  v  => { SettingsManager.MusicEnabled = v; AudioManager.Instance?.ApplyMusicSettings(); });
            Bind(toggleSound,     () => SettingsManager.SoundEnabled,
                                  v  => { SettingsManager.SoundEnabled = v; AudioManager.Instance?.ApplySoundSettings(); });
            Bind(toggleVibration, () => SettingsManager.VibrationEnabled,
                                  v  => SettingsManager.VibrationEnabled = v);

            BindSlider(sliderMusic, () => SettingsManager.MusicVolume,
                                    v  => { SettingsManager.MusicVolume = v; AudioManager.Instance?.ApplyMusicSettings(); });
            BindSlider(sliderSound, () => SettingsManager.SoundVolume,
                                    v  => { SettingsManager.SoundVolume = v; AudioManager.Instance?.ApplySoundSettings(); });

            btnLangRu?.onClick.AddListener(() => SetLanguage(LocaleManager.LangRu));
            btnLangSah?.onClick.AddListener(() => SetLanguage(LocaleManager.LangSah));
            RefreshLangButtons();

            LocaleManager.OnLanguageChanged += OnLanguageChanged;
        }

        protected override void OnWindowDestroy()
        {
            btnLangRu?.onClick.RemoveAllListeners();
            btnLangSah?.onClick.RemoveAllListeners();
            LocaleManager.OnLanguageChanged -= OnLanguageChanged;
        }

        public override void Open()
        {
            RefreshLangButtons();
            base.Open();
        }

        // ── Язык ──────────────────────────────────────────────────────────

        private void SetLanguage(string lang) => LocaleManager.CurrentLanguage = lang;

        private void OnLanguageChanged()
        {
            RefreshLangButtons();
            Refresh(toggleMusic,     toggleMusic     != null && toggleMusic.isOn);
            Refresh(toggleSound,     toggleSound     != null && toggleSound.isOn);
            Refresh(toggleVibration, toggleVibration != null && toggleVibration.isOn);
        }

        private void RefreshLangButtons()
        {
            string cur = LocaleManager.CurrentLanguage;
            SetLangBtnState(btnLangRu,  cur == LocaleManager.LangRu);
            SetLangBtnState(btnLangSah, cur == LocaleManager.LangSah);
        }

        private void SetLangBtnState(Button btn, bool active)
        {
            if (btn == null) return;
            btn.image.color = active ? ColorLangOn : ColorLangOff;
            var tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.color = active ? Color.white : ColorLangText;
        }

        // ── Переключатели ─────────────────────────────────────────────────

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
            if (lbl != null) lbl.text = LocaleManager.Get(isOn ? "settings_on" : "settings_off");
        }

        private static void BindSlider(Slider slider, Func<float> getter, Action<float> setter)
        {
            if (slider == null) return;
            slider.value = getter();
            slider.onValueChanged.AddListener(setter.Invoke);
        }
    }
}
