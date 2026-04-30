using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEditor;
using UstAldanQuiz.Data;

namespace UstAldanQuiz.Editor
{
    // ─────────────────────────────────────────────────────────────────────────
    // GoogleSheetsImporter
    //
    // Импортирует данные из Google Sheets:
    //   • Лист «Вопросы» → ScriptableObject QuestionData + QuestionDatabase
    //   • Лист «Локализация» → Assets/Resources/Locales/ru.txt и sah.txt
    //
    // Формат листа «Вопросы» (первая строка — заголовки, регистр не важен):
    //   id | category_id | category_name | question | answer1 | answer2 |
    //   answer3 | answer4 | correct_index | difficulty
    //
    // Формат листа «Локализация»:
    //   key | ru | sah
    //
    // Настройки: UstAldan Quiz → Google Sheets → ⚙ Настройки
    // ─────────────────────────────────────────────────────────────────────────

    public static class GoogleSheetsImporter
    {
        private const string PrefId           = "GS_SpreadsheetId";
        private const string PrefQuestionsGid = "GS_QuestionsGid";
        private const string PrefLocaleGid    = "GS_LocaleGid";

        private static string SheetId       => EditorPrefs.GetString(PrefId,           "");
        private static string QuestionsGid  => EditorPrefs.GetString(PrefQuestionsGid, "0");
        private static string LocaleGid     => EditorPrefs.GetString(PrefLocaleGid,    "");

        private const string QuestionsDir = "Assets/ScriptableObjects/Questions";
        private const string CategoriesDir = "Assets/ScriptableObjects/Categories";
        private const string DatabaseDir   = "Assets/ScriptableObjects/Database";
        private const string LocaleDir     = "Assets/Resources/Locales";

        // ── Menu ──────────────────────────────────────────────────────────────

        [MenuItem("UstAldan Quiz/Google Sheets/⚙ Настройки", priority = 100)]
        public static void OpenSettings() => GoogleSheetsSettingsWindow.Open();

        [MenuItem("UstAldan Quiz/Google Sheets/↓ Импортировать всё", priority = 200)]
        public static void ImportAll()
        {
            if (!EnsureConfig()) return;
            bool ok = true;
            ok &= RunImportQuestions();
            if (!string.IsNullOrEmpty(LocaleGid))
                ok &= RunImportLocale();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (ok) Debug.Log("[GoogleSheets] Импорт завершён успешно.");
        }

        [MenuItem("UstAldan Quiz/Google Sheets/↓ Вопросы", priority = 201)]
        public static void ImportQuestionsMenu()
        {
            if (!EnsureConfig()) return;
            RunImportQuestions();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("UstAldan Quiz/Google Sheets/↓ Локализация", priority = 202)]
        public static void ImportLocaleMenu()
        {
            if (!EnsureConfig()) return;
            if (string.IsNullOrEmpty(LocaleGid))
            {
                Debug.LogWarning("[GoogleSheets] GID листа «Локализация» не задан в настройках.");
                return;
            }
            RunImportLocale();
            AssetDatabase.Refresh();
        }

        // ── Core ──────────────────────────────────────────────────────────────

        static bool RunImportQuestions()
        {
            string csv = Download(CsvUrl(SheetId, QuestionsGid));
            if (csv == null) return false;
            ProcessQuestions(csv);
            return true;
        }

        static bool RunImportLocale()
        {
            string csv = Download(CsvUrl(SheetId, LocaleGid));
            if (csv == null) return false;
            ProcessLocale(csv);
            return true;
        }

        // ── Download ──────────────────────────────────────────────────────────

        static string CsvUrl(string id, string gid)
        {
            string g = string.IsNullOrEmpty(gid) ? "" : $"&gid={gid}";
            return $"https://docs.google.com/spreadsheets/d/{id}/export?format=csv{g}";
        }

        static string Download(string url)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using var client = new WebClient { Encoding = Encoding.UTF8 };
                string text = client.DownloadString(url);
                // Если Google вернул HTML (редирект на логин) — сообщаем об ошибке
                if (text.TrimStart().StartsWith("<"))
                {
                    Debug.LogError("[GoogleSheets] Получен HTML вместо CSV. Убедитесь, что таблица опубликована: " +
                                   "Файл → Поделиться → Опубликовать в интернете → Значения CSV.");
                    return null;
                }
                Debug.Log($"[GoogleSheets] Загружено {text.Length} символов.");
                return text;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GoogleSheets] Ошибка загрузки: {e.Message}");
                return null;
            }
        }

        // ── Questions ─────────────────────────────────────────────────────────

        static void ProcessQuestions(string csv)
        {
            var rows = ParseCsv(csv);
            if (rows.Count < 2) { Debug.LogWarning("[GoogleSheets] Лист вопросов пустой."); return; }

            var hdr = rows[0];
            int iId   = Col(hdr, "id");
            int iCatId= Col(hdr, "category_id");
            int iCatN = Col(hdr, "category_name");
            int iQ    = Col(hdr, "question");
            int iA1   = Col(hdr, "answer1");
            int iA2   = Col(hdr, "answer2");
            int iA3   = Col(hdr, "answer3");
            int iA4   = Col(hdr, "answer4");
            int iCorr = Col(hdr, "correct_index");
            int iDiff = Col(hdr, "difficulty");

            if (iQ < 0 || iA1 < 0)
            {
                Debug.LogError("[GoogleSheets] Не найдены столбцы question / answer1. Проверьте заголовки.");
                return;
            }

            Directory.CreateDirectory(QuestionsDir);
            Directory.CreateDirectory(CategoriesDir);
            Directory.CreateDirectory(DatabaseDir);

            var catCache  = new Dictionary<string, QuestionCategory>(StringComparer.OrdinalIgnoreCase);
            var catAssets = new Dictionary<string, List<QuestionData>>(StringComparer.OrdinalIgnoreCase);
            int created = 0, updated = 0, skipped = 0;

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                string qText = V(row, iQ);
                if (string.IsNullOrWhiteSpace(qText)) { skipped++; continue; }

                string rowId   = iId >= 0 ? V(row, iId) : r.ToString();
                string catId   = iCatId >= 0 ? V(row, iCatId).ToLower() : "general";
                string catName = iCatN >= 0 ? V(row, iCatN) : catId;
                if (string.IsNullOrWhiteSpace(catId)) catId = "general";

                string a1 = V(row, iA1), a2 = V(row, iA2), a3 = V(row, iA3), a4 = V(row, iA4);
                int correctIdx = iCorr >= 0 && int.TryParse(V(row, iCorr), out int ci) ? Mathf.Clamp(ci, 0, 3) : 0;
                int difficulty = iDiff >= 0 && int.TryParse(V(row, iDiff), out int d) ? Mathf.Clamp(d, 1, 3) : 1;

                // Перемещаем правильный ответ на индекс 0
                var answers = new[] { a1, a2, a3, a4 };
                if (correctIdx != 0)
                    (answers[0], answers[correctIdx]) = (answers[correctIdx], answers[0]);

                if (!catCache.TryGetValue(catId, out var cat))
                {
                    cat = GetOrCreateCategory(catId, catName);
                    catCache[catId] = cat;
                }

                string catFolder = $"{QuestionsDir}/{Capitalize(catId)}";
                Directory.CreateDirectory(catFolder);
                string assetName = $"Q{rowId.Trim().PadLeft(3, '0')}";
                string assetPath = $"{catFolder}/{assetName}.asset";

                var asset = AssetDatabase.LoadAssetAtPath<QuestionData>(assetPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<QuestionData>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                    created++;
                }
                else updated++;

                asset.category     = cat;
                asset.questionText = qText;
                asset.answers      = answers;
                asset.difficulty   = difficulty;
                EditorUtility.SetDirty(asset);

                if (!catAssets.ContainsKey(catId)) catAssets[catId] = new List<QuestionData>();
                catAssets[catId].Add(asset);
            }

            foreach (var kv in catAssets)
                UpdateDatabase(kv.Key, catCache[kv.Key], kv.Value);

            Debug.Log($"[GoogleSheets] Вопросы — создано: {created}, обновлено: {updated}, пропущено: {skipped}.");
        }

        static QuestionCategory GetOrCreateCategory(string catId, string displayName)
        {
            string path = $"{CategoriesDir}/{Capitalize(catId)}.asset";
            var cat = AssetDatabase.LoadAssetAtPath<QuestionCategory>(path);
            if (cat != null) return cat;

            cat = ScriptableObject.CreateInstance<QuestionCategory>();
            cat.categoryId  = catId;
            cat.displayName = string.IsNullOrEmpty(displayName) ? catId : displayName;
            AssetDatabase.CreateAsset(cat, path);
            EditorUtility.SetDirty(cat);
            Debug.Log($"[GoogleSheets] Создана категория: {catId}");
            return cat;
        }

        static void UpdateDatabase(string catId, QuestionCategory cat, List<QuestionData> questions)
        {
            string path = $"{DatabaseDir}/{Capitalize(catId)}Database.asset";
            var db = AssetDatabase.LoadAssetAtPath<QuestionDatabase>(path);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<QuestionDatabase>();
                AssetDatabase.CreateAsset(db, path);
            }

            // Добавляем категорию в список если её ещё нет
            if (cat != null && !db.categories.Contains(cat))
                db.categories.Add(cat);

            // Сливаем вопросы без дублей
            foreach (var q in questions)
                if (!db.allQuestions.Contains(q)) db.allQuestions.Add(q);

            EditorUtility.SetDirty(db);
        }

        // ── Locale ────────────────────────────────────────────────────────────

        static void ProcessLocale(string csv)
        {
            var rows = ParseCsv(csv);
            if (rows.Count < 2) { Debug.LogWarning("[GoogleSheets] Лист локализации пустой."); return; }

            var hdr = rows[0];
            int iKey = Col(hdr, "key");
            int iRu  = Col(hdr, "ru");
            int iSah = Col(hdr, "sah");

            if (iKey < 0) { Debug.LogError("[GoogleSheets] Столбец key не найден в листе Локализация."); return; }

            var ru  = new Dictionary<string, string>();
            var sah = new Dictionary<string, string>();

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                string key = V(row, iKey).Trim();
                if (string.IsNullOrEmpty(key) || key.StartsWith("#")) continue;
                if (iRu  >= 0) { string v = V(row, iRu);  if (!string.IsNullOrEmpty(v)) ru[key]  = v; }
                if (iSah >= 0) { string v = V(row, iSah); if (!string.IsNullOrEmpty(v)) sah[key] = v; }
            }

            Directory.CreateDirectory(LocaleDir);

            if (ru.Count  > 0) MergeLocale(Path.Combine(LocaleDir, "ru.txt"),  ru);
            if (sah.Count > 0) MergeLocale(Path.Combine(LocaleDir, "sah.txt"), sah);

            Debug.Log($"[GoogleSheets] Локализация — ru: {ru.Count} ключей, sah: {sah.Count} ключей.");
        }

        static void MergeLocale(string filePath, Dictionary<string, string> updates)
        {
            var lines   = File.Exists(filePath)
                ? new List<string>(File.ReadAllLines(filePath, Encoding.UTF8))
                : new List<string>();
            var touched = new HashSet<string>();

            // Обновляем существующие ключи на месте
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("#") || !line.Contains("=")) continue;
                int eq = line.IndexOf('=');
                string key = line.Substring(0, eq).Trim();
                if (updates.TryGetValue(key, out string val))
                {
                    lines[i] = $"{key}={val}";
                    touched.Add(key);
                }
            }

            // Дописываем новые ключи в конец
            var newKeys = new List<string>();
            foreach (var kv in updates)
                if (!touched.Contains(kv.Key)) newKeys.Add(kv.Key);

            if (newKeys.Count > 0)
            {
                lines.Add("");
                lines.Add("# ── Google Sheets ──────────────────────────────────────────────────────────");
                foreach (var k in newKeys)
                    lines.Add($"{k}={updates[k]}");
            }

            File.WriteAllLines(filePath, lines, Encoding.UTF8);
            Debug.Log($"[GoogleSheets] {Path.GetFileName(filePath)}: обновлено {touched.Count}, добавлено {newKeys.Count}.");
        }

        // ── CSV parser (RFC-4180) ─────────────────────────────────────────────

        static List<List<string>> ParseCsv(string text)
        {
            var result = new List<List<string>>();
            var row    = new List<string>();
            var cell   = new StringBuilder();
            bool inQ   = false;

            text = text.Replace("\r\n", "\n").Replace('\r', '\n');

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQ)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                        else inQ = false;
                    }
                    else cell.Append(c);
                }
                else
                {
                    switch (c)
                    {
                        case '"':  inQ = true; break;
                        case ',':  row.Add(cell.ToString()); cell.Clear(); break;
                        case '\n':
                            row.Add(cell.ToString()); cell.Clear();
                            result.Add(row); row = new List<string>();
                            break;
                        default: cell.Append(c); break;
                    }
                }
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); result.Add(row); }
            return result;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static int Col(List<string> hdr, string name)
        {
            for (int i = 0; i < hdr.Count; i++)
                if (string.Equals(hdr[i].Trim(), name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        static string V(List<string> row, int idx) =>
            idx >= 0 && idx < row.Count ? row[idx].Trim() : "";

        static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1).ToLower();

        static bool EnsureConfig()
        {
            if (!string.IsNullOrEmpty(SheetId)) return true;
            Debug.LogError("[GoogleSheets] ID таблицы не задан. " +
                           "Откройте: UstAldan Quiz → Google Sheets → ⚙ Настройки");
            GoogleSheetsSettingsWindow.Open();
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Settings window
    // ─────────────────────────────────────────────────────────────────────────

    public class GoogleSheetsSettingsWindow : EditorWindow
    {
        string _id, _qGid, _lGid;

        public static void Open()
        {
            var w = GetWindow<GoogleSheetsSettingsWindow>("Google Sheets — Настройки");
            w.minSize = new Vector2(540, 320);
            w._id   = EditorPrefs.GetString("GS_SpreadsheetId",  "");
            w._qGid = EditorPrefs.GetString("GS_QuestionsGid",   "0");
            w._lGid = EditorPrefs.GetString("GS_LocaleGid",      "");
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Google Sheets — Настройки импорта", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "1. Откройте таблицу в браузере.\n" +
                "2. Файл → Поделиться → Опубликовать в интернете → Значения CSV → Опубликовать.\n" +
                "3. Скопируйте ID из адресной строки:\n" +
                "   https://docs.google.com/spreadsheets/d/ [ВОТ ID] /edit",
                MessageType.Info);

            EditorGUILayout.Space(6);
            _id   = EditorGUILayout.TextField("ID таблицы",                   _id);
            _qGid = EditorGUILayout.TextField("GID листа «Вопросы»",          _qGid);
            _lGid = EditorGUILayout.TextField("GID листа «Локализация»",      _lGid);

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "GID листа виден в URL при переходе на вкладку:\n" +
                "...spreadsheets/d/ID/edit#gid=[ВОТ GID]\n" +
                "Первый лист обычно имеет gid=0.",
                MessageType.None);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Сохранить", GUILayout.Height(32)))
            {
                EditorPrefs.SetString("GS_SpreadsheetId", _id.Trim());
                EditorPrefs.SetString("GS_QuestionsGid",  _qGid.Trim());
                EditorPrefs.SetString("GS_LocaleGid",     _lGid.Trim());
                Debug.Log("[GoogleSheets] Настройки сохранены.");
                Close();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Формат листа «Вопросы» (первая строка — заголовки):", EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(
                "id | category_id | category_name | question | answer1 | answer2 | answer3 | answer4 | correct_index | difficulty",
                EditorStyles.helpBox, GUILayout.Height(32));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Формат листа «Локализация» (первая строка — заголовки):", EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel("key | ru | sah", EditorStyles.helpBox, GUILayout.Height(22));
        }
    }
}
