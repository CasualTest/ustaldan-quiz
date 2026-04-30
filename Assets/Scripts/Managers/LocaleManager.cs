using System;
using System.Collections.Generic;
using UnityEngine;

namespace UstAldanQuiz.Managers
{
    /// <summary>
    /// Загружает строки из Assets/Resources/Locales/{lang}.txt.
    /// Формат файла: key=value  (#-строки — комментарии, \n в значении — перенос строки).
    /// </summary>
    public static class LocaleManager
    {
        public const string LangRu  = "ru";
        public const string LangSah = "sah";

        private static readonly Dictionary<string, string> _strings =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private static string _currentLang;

        /// <summary>Текущий язык. При смене автоматически перезагружает строки.</summary>
        public static string CurrentLanguage
        {
            get => _currentLang ?? PlayerPrefs.GetString("language", LangRu);
            set
            {
                PlayerPrefs.SetString("language", value);
                PlayerPrefs.Save();
                Load(value);
            }
        }

        /// <summary>Вызывается при смене языка — все LocaleText перечитывают свои ключи.</summary>
        public static event Action OnLanguageChanged;

        // ── Публичные методы ─────────────────────────────────────────────

        /// <summary>Получить строку по ключу, опционально с подстановкой {0},{1}…</summary>
        public static string Get(string key, params object[] args)
        {
            EnsureLoaded();
            if (!_strings.TryGetValue(key, out var val))
            {
                Debug.LogWarning($"[Locale] Ключ не найден: '{key}'");
                return key;
            }
            return args.Length > 0 ? string.Format(val, args) : val;
        }

        /// <summary>Явная загрузка языка. Обычно не нужна — вызывается автоматически.</summary>
        public static void Load(string lang)
        {
            _currentLang = lang;
            _strings.Clear();

            var asset = Resources.Load<TextAsset>($"Locales/{lang}");
            if (asset == null)
            {
                Debug.LogError($"[Locale] Файл не найден: Resources/Locales/{lang}.txt");
                return;
            }

            foreach (var rawLine in asset.text.Split('\n'))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                var eq = line.IndexOf('=');
                if (eq < 1) continue;

                var k = line.Substring(0, eq).Trim();
                var v = line.Substring(eq + 1).Trim().Replace("\\n", "\n");
                _strings[k] = v;
            }

            OnLanguageChanged?.Invoke();
            Debug.Log($"[Locale] Загружен язык '{lang}': {_strings.Count} строк.");
        }

        // ── Приватное ────────────────────────────────────────────────────

        private static void EnsureLoaded()
        {
            if (_strings.Count == 0)
                Load(CurrentLanguage);
        }
    }
}
