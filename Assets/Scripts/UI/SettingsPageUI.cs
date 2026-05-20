using System;
using System.Collections;
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

        [Header("Вкладка Игра")]
        [SerializeField] private Button        btnReset;
        [SerializeField] private ConfirmPopup  confirmResetPopup;
        [SerializeField] private Button        btnSuggest;
        [SerializeField] private SuggestQuestionUI suggestUI;

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

        private const float TabAnimDuration = 0.18f;
        private const float TabSlideOffset  = 60f;

        private int            _activeTab = -1;
        private Coroutine      _tabAnim;
        private GameObject[]   _panels;
        private CanvasGroup[]  _panelGroups;
        private RectTransform[] _panelRects;

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

            btnReset?.onClick.AddListener(() => confirmResetPopup?.Show(SaveManager.ResetAll));
            btnSuggest?.onClick.AddListener(() => suggestUI?.Open());

            _panels = new[] { panelSettings, panelGame, panelSecurity };
            _panelGroups = new CanvasGroup[_panels.Length];
            _panelRects  = new RectTransform[_panels.Length];
            for (int i = 0; i < _panels.Length; i++)
            {
                if (_panels[i] == null) continue;
                _panels[i].SetActive(false);
                var cg = _panels[i].GetComponent<CanvasGroup>();
                _panelGroups[i] = cg != null ? cg : _panels[i].AddComponent<CanvasGroup>();
                _panelRects[i]  = _panels[i].GetComponent<RectTransform>();
            }

            tabSettings?.onClick.AddListener(() => ShowTab(0));
            tabGame?.onClick.AddListener(()     => ShowTab(1));
            tabSecurity?.onClick.AddListener(() => ShowTab(2));

            ShowTab(0);
            LocaleManager.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            toggleLang?.onValueChanged.RemoveAllListeners();
            btnReset?.onClick.RemoveAllListeners();
            btnSuggest?.onClick.RemoveAllListeners();
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
            if (index == _activeTab) return;

            RefreshTab(tabSettings, index == 0);
            RefreshTab(tabGame,     index == 1);
            RefreshTab(tabSecurity, index == 2);

            int prev = _activeTab;
            int dir  = (prev < 0 || index > prev) ? 1 : -1;
            _activeTab = index;

            if (_tabAnim != null) StopCoroutine(_tabAnim);

            if (prev < 0)
            {
                if (_panels[index] != null) _panels[index].SetActive(true);
                return;
            }

            _tabAnim = StartCoroutine(AnimateTabs(prev, index, dir));
        }

        private IEnumerator AnimateTabs(int from, int to, int dir)
        {
            if (_panels[to] != null)
            {
                _panels[to].SetActive(true);
                _panelGroups[to].alpha = 0f;
                if (_panelRects[to] != null)
                    _panelRects[to].anchoredPosition = new Vector2(TabSlideOffset * dir, 0f);
            }

            for (float t = 0f; t < TabAnimDuration; t += Time.unscaledDeltaTime)
            {
                float e = Mathf.SmoothStep(0f, 1f, t / TabAnimDuration);

                if (_panels[from] != null)
                {
                    _panelGroups[from].alpha = 1f - e;
                    if (_panelRects[from] != null)
                        _panelRects[from].anchoredPosition = new Vector2(-TabSlideOffset * dir * e, 0f);
                }
                if (_panels[to] != null)
                {
                    _panelGroups[to].alpha = e;
                    if (_panelRects[to] != null)
                        _panelRects[to].anchoredPosition = new Vector2(TabSlideOffset * dir * (1f - e), 0f);
                }

                yield return null;
            }

            if (_panels[from] != null)
            {
                _panels[from].SetActive(false);
                _panelGroups[from].alpha = 1f;
                if (_panelRects[from] != null)
                    _panelRects[from].anchoredPosition = Vector2.zero;
            }
            if (_panels[to] != null)
            {
                _panelGroups[to].alpha = 1f;
                if (_panelRects[to] != null)
                    _panelRects[to].anchoredPosition = Vector2.zero;
            }
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
