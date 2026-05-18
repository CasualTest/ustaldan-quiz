using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.UI
{
    public class SettingsPageUI : MonoBehaviour
    {
        [Header("Переключатели")]
        [SerializeField] private Toggle toggleMusic;
        [SerializeField] private Toggle toggleSound;
        [SerializeField] private Toggle toggleVibration;

        [Header("Ползунки")]
        [SerializeField] private Slider sliderMusic;
        [SerializeField] private Slider sliderSound;

        [Header("Язык")]
        [SerializeField] private Toggle toggleLang;

        [Header("Вкладки")]
        [SerializeField] private Button     tabSettings;
        [SerializeField] private Button     tabGame;
        [SerializeField] private Button     tabSecurity;
        [SerializeField] private GameObject panelSettings;
        [SerializeField] private GameObject panelGame;
        [SerializeField] private GameObject panelSecurity;

        private static readonly Color ColorOn     = new Color(0.18f, 0.38f, 0.25f);
        private static readonly Color ColorOff    = new Color(0.62f, 0.62f, 0.62f);
        private static readonly Color ColorTabOn  = Color.white;
        private static readonly Color ColorTabOff = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Color ColorIndOn  = new Color(0.78f, 0.66f, 0.29f);

        private void Start()
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

            BindLang();

            tabSettings?.onClick.AddListener(() => ShowTab(0));
            tabGame?.onClick.AddListener(()     => ShowTab(1));
            tabSecurity?.onClick.AddListener(() => ShowTab(2));

            ShowTab(0);
            LocaleManager.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            toggleLang?.onValueChanged.RemoveAllListeners();
            tabSettings?.onClick.RemoveAllListeners();
            tabGame?.onClick.RemoveAllListeners();
            tabSecurity?.onClick.RemoveAllListeners();
            LocaleManager.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            RefreshLang();
            Refresh(toggleMusic,     toggleMusic     != null && toggleMusic.isOn);
            Refresh(toggleSound,     toggleSound     != null && toggleSound.isOn);
            Refresh(toggleVibration, toggleVibration != null && toggleVibration.isOn);
        }

        // ── Вкладки ───────────────────────────────────────────────────────

        private void ShowTab(int index)
        {
            if (panelSettings != null) panelSettings.SetActive(index == 0);
            if (panelGame     != null) panelGame.SetActive(index == 1);
            if (panelSecurity != null) panelSecurity.SetActive(index == 2);
            RefreshTab(tabSettings, index == 0);
            RefreshTab(tabGame,     index == 1);
            RefreshTab(tabSecurity, index == 2);
        }

        private void RefreshTab(Button btn, bool active)
        {
            if (btn == null) return;
            var lbl = btn.GetComponentInChildren<TMP_Text>();
            if (lbl != null)
            {
                lbl.color     = active ? ColorTabOn : ColorTabOff;
                lbl.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
            }
            var ind = btn.transform.Find("Indicator")?.GetComponent<Image>();
            if (ind != null) ind.color = active ? ColorIndOn : Color.clear;
        }

        // ── Язык ──────────────────────────────────────────────────────────

        private void BindLang()
        {
            if (toggleLang == null) return;
            RefreshLang();
            toggleLang.onValueChanged.AddListener(val =>
                LocaleManager.CurrentLanguage = val ? LocaleManager.LangRu : LocaleManager.LangSah);
        }

        private void RefreshLang()
        {
            if (toggleLang == null) return;
            bool isRu = LocaleManager.CurrentLanguage == LocaleManager.LangRu;
            toggleLang.SetIsOnWithoutNotify(isRu);
            Refresh(toggleLang, isRu);
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
            var sprOn  = toggle.transform.Find("SpriteOn");
            var sprOff = toggle.transform.Find("SpriteOff");
            if (sprOn != null && sprOff != null)
            {
                sprOn.gameObject.SetActive(isOn);
                sprOff.gameObject.SetActive(!isOn);
                return;
            }

            var checkmark = toggle.transform.Find("Checkmark");
            if (checkmark != null)
            {
                checkmark.gameObject.SetActive(isOn);
                return;
            }

            if (toggle.targetGraphic is Image img)
                img.color = isOn ? ColorOn : ColorOff;
            var lbl = toggle.GetComponentInChildren<TMP_Text>();
            if (lbl != null) lbl.text = LocaleManager.Get(isOn ? "settings_on" : "settings_off");
        }

        // ── Ползунки ──────────────────────────────────────────────────────

        private static void BindSlider(Slider slider, Func<float> getter, Action<float> setter)
        {
            if (slider == null) return;
            slider.value = getter();
            slider.onValueChanged.AddListener(setter.Invoke);
        }
    }
}
