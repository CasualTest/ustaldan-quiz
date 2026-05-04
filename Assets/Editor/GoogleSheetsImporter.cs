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
    // Импортирует данные из Google Sheets (опубликованных листов).
    //
    // Как получить URL:
    //   Файл → Поделиться → Опубликовать в интернете →
    //   выберите нужный лист → "Значения CSV" → Опубликовать →
    //   скопируйте полученную ссылку.
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
        private const string PrefQUrl = "GS_QuestionsUrl";
        private const string PrefLUrl = "GS_LocaleUrl";

        private static string QuestionsUrl => EditorPrefs.GetString(PrefQUrl, "");
        private static string LocaleUrl    => EditorPrefs.GetString(PrefLUrl, "");

        private const string QuestionsDir  = "Assets/ScriptableObjects/Questions";
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
            RunImportQuestions();
            if (!string.IsNullOrEmpty(LocaleUrl))
                RunImportLocale();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GoogleSheets] Импорт завершён.");
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
            if (string.IsNullOrEmpty(LocaleUrl))
            {
                Debug.LogWarning("[GoogleSheets] URL листа «Локализация» не задан. Откройте: UstAldan Quiz → Google Sheets → ⚙ Настройки");
                GoogleSheetsSettingsWindow.Open();
                return;
            }
            RunImportLocale();
            AssetDatabase.Refresh();
        }

        // ── Core ──────────────────────────────────────────────────────────────

        static void RunImportQuestions()
        {
            string csv = Download(QuestionsUrl);
            if (csv != null) ProcessQuestions(csv);
        }

        static void RunImportLocale()
        {
            string csv = Download(LocaleUrl);
            if (csv != null) ProcessLocale(csv);
        }

        // ── Download ──────────────────────────────────────────────────────────

        static string Download(string url)
        {
            Debug.Log($"[GoogleSheets] Загружаю: {url}");
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using var client = new WebClient { Encoding = Encoding.UTF8 };
                string text = client.DownloadString(url);
                if (text.TrimStart().StartsWith("<"))
                {
                    Debug.LogError(
                        "[GoogleSheets] Получен HTML вместо CSV.\n" +
                        "Убедитесь что таблица опубликована:\n" +
                        "Файл → Поделиться → Опубликовать в интернете → выберите лист → CSV → Опубликовать.");
                    return null;
                }
                Debug.Log($"[GoogleSheets] Загружено {text.Length} символов.");
                return text;
            }
            catch (WebException we) when (we.Response is HttpWebResponse r)
            {
                Debug.LogError(
                    $"[GoogleSheets] HTTP {(int)r.StatusCode} {r.StatusDescription}\n" +
                    $"URL: {url}");
                return null;
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

            var hdr   = rows[0];
            int iId   = ColAny(hdr, "id", "ID");
            int iCatId= ColAny(hdr, "category_id", "category", "Category");
            int iCatN = ColAny(hdr, "category_name", "category", "Category");
            int iQ    = ColAny(hdr, "question", "question_ru", "Question_RU");
            int iQSah = ColAny(hdr, "question_sah", "Question_SAH");
            int iA1   = ColAny(hdr, "answer1", "answer_1", "Answer_1");
            int iA2   = ColAny(hdr, "answer2", "answer_2", "Answer_2");
            int iA3   = ColAny(hdr, "answer3", "answer_3", "Answer_3");
            int iA4   = ColAny(hdr, "answer4", "answer_4", "Answer_4");
            int iCorr    = ColAny(hdr, "correct_index", "Correct_Index");
            int iDiff    = ColAny(hdr, "difficulty", "Difficulty");
            int iFactRu  = ColAny(hdr, "fact_after_ru", "Fact_After_RU", "fact_after", "Fact_After");
            int iFactSah = ColAny(hdr, "fact_after_sah", "Fact_After_SAH");

            if (iQ < 0 || iA1 < 0)
            {
                Debug.LogError("[GoogleSheets] Не найдены столбцы question / answer1.\n" +
                               "Заголовки в таблице: " + string.Join(", ", hdr));
                return;
            }

            Directory.CreateDirectory(QuestionsDir);
            Directory.CreateDirectory(CategoriesDir);
            Directory.CreateDirectory(DatabaseDir);

            // Собираем все существующие QuestionData assets до импорта
            var existingGuids = AssetDatabase.FindAssets("t:QuestionData", new[] { QuestionsDir });
            var existingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in existingGuids)
                existingPaths.Add(AssetDatabase.GUIDToAssetPath(g));

            var catCache   = new Dictionary<string, QuestionCategory>(StringComparer.OrdinalIgnoreCase);
            var catAssets  = new Dictionary<string, List<QuestionData>>(StringComparer.OrdinalIgnoreCase);
            var touchedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int created = 0, updated = 0, skipped = 0;

            for (int r = 1; r < rows.Count; r++)
            {
                var row  = rows[r];
                string q = V(row, iQ);
                if (string.IsNullOrWhiteSpace(q)) { skipped++; continue; }

                string rowId   = iId >= 0 ? V(row, iId) : r.ToString();
                string catId   = (iCatId >= 0 ? V(row, iCatId) : "general").ToLower();
                string catName = iCatN >= 0 ? V(row, iCatN) : catId;
                if (string.IsNullOrWhiteSpace(catId)) catId = "general";

                var answers = new[] { V(row, iA1), V(row, iA2), V(row, iA3), V(row, iA4) };
                int corr = iCorr >= 0 && int.TryParse(V(row, iCorr), out int ci) ? Mathf.Clamp(ci, 0, 3) : 0;
                int diff = iDiff >= 0 && int.TryParse(V(row, iDiff), out int d)  ? Mathf.Clamp(d,  1, 3) : 1;

                // Правильный ответ всегда на индексе 0
                if (corr != 0) (answers[0], answers[corr]) = (answers[corr], answers[0]);

                if (!catCache.TryGetValue(catId, out var cat))
                {
                    cat = GetOrCreateCategory(catId, catName);
                    catCache[catId] = cat;
                }

                string folder    = $"{QuestionsDir}/{Cap(catId)}";
                Directory.CreateDirectory(folder);
                string assetPath = $"{folder}/Q{rowId.Trim().PadLeft(3, '0')}.asset";

                var asset = AssetDatabase.LoadAssetAtPath<QuestionData>(assetPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<QuestionData>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                    created++;
                }
                else updated++;

                asset.category        = cat;
                asset.questionText    = q;
                asset.questionTextSah = iQSah    >= 0 ? V(row, iQSah)    : "";
                asset.answers         = answers;
                asset.difficulty      = diff;
                asset.factAfterRu     = iFactRu  >= 0 ? V(row, iFactRu)  : "";
                asset.factAfterSah    = iFactSah >= 0 ? V(row, iFactSah) : "";
                EditorUtility.SetDirty(asset);
                touchedPaths.Add(assetPath);

                if (!catAssets.ContainsKey(catId)) catAssets[catId] = new List<QuestionData>();
                catAssets[catId].Add(asset);
            }

            // Удаляем ассеты которых нет в таблице
            int deleted = 0;
            foreach (var path in existingPaths)
            {
                if (!touchedPaths.Contains(path))
                {
                    AssetDatabase.DeleteAsset(path);
                    deleted++;
                }
            }

            // Собираем полный список всех импортированных категорий и вопросов
            var allCats      = new List<QuestionCategory>(catCache.Values);
            var allQuestions = new List<QuestionData>();
            foreach (var kv in catAssets) allQuestions.AddRange(kv.Value);

            // Обновляем все существующие QuestionDatabase — каждая получает полный список
            UpdateAllDatabases(allCats, allQuestions);

            Debug.Log($"[GoogleSheets] Вопросы — создано: {created}, обновлено: {updated}, удалено: {deleted}, пропущено: {skipped}.");
        }

        static QuestionCategory GetOrCreateCategory(string catId, string displayName)
        {
            string path = $"{CategoriesDir}/{Cap(catId)}.asset";
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

        static void UpdateAllDatabases(List<QuestionCategory> allCats, List<QuestionData> allQuestions)
        {
            var guids = AssetDatabase.FindAssets("t:QuestionDatabase");

            // Если баз нет вообще — создаём одну мастер-базу
            if (guids.Length == 0)
            {
                var master = ScriptableObject.CreateInstance<QuestionDatabase>();
                string mp  = $"{DatabaseDir}/QuestionDatabase.asset";
                AssetDatabase.CreateAsset(master, mp);
                guids = new[] { AssetDatabase.AssetPathToGUID(mp) };
                Debug.Log("[GoogleSheets] Создана мастер-база QuestionDatabase.asset");
            }

            // Каждую найденную базу наполняем полным списком категорий и вопросов
            foreach (var guid in guids)
            {
                var db = AssetDatabase.LoadAssetAtPath<QuestionDatabase>(AssetDatabase.GUIDToAssetPath(guid));
                if (db == null) continue;
                db.categories   = new List<QuestionCategory>(allCats);
                db.allQuestions = new List<QuestionData>(allQuestions);
                EditorUtility.SetDirty(db);
            }

            Debug.Log($"[GoogleSheets] Обновлено баз данных: {guids.Length} — категорий: {allCats.Count}, вопросов: {allQuestions.Count}.");
        }

        // ── Locale ────────────────────────────────────────────────────────────

        static void ProcessLocale(string csv)
        {
            var rows = ParseCsv(csv);
            if (rows.Count < 2) { Debug.LogWarning("[GoogleSheets] Лист локализации пустой."); return; }

            var hdr  = rows[0];
            int iKey = Col(hdr, "key");
            int iRu  = Col(hdr, "ru");
            int iSah = Col(hdr, "sah");

            if (iKey < 0) { Debug.LogError("[GoogleSheets] Столбец key не найден."); return; }

            var ru  = new Dictionary<string, string>();
            var sah = new Dictionary<string, string>();

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                string key = V(row, iKey);
                if (string.IsNullOrEmpty(key) || key.StartsWith("#")) continue;
                if (iRu  >= 0) { string v = V(row, iRu);  if (v.Length > 0) ru[key]  = v; }
                if (iSah >= 0) { string v = V(row, iSah); if (v.Length > 0) sah[key] = v; }
            }

            Directory.CreateDirectory(LocaleDir);
            if (ru.Count  > 0) MergeLocale(Path.Combine(LocaleDir, "ru.txt"),  ru);
            if (sah.Count > 0) MergeLocale(Path.Combine(LocaleDir, "sah.txt"), sah);

            Debug.Log($"[GoogleSheets] Локализация — ru: {ru.Count}, sah: {sah.Count} ключей.");
        }

        static void MergeLocale(string filePath, Dictionary<string, string> updates)
        {
            var lines   = File.Exists(filePath)
                ? new List<string>(File.ReadAllLines(filePath, Encoding.UTF8))
                : new List<string>();
            var touched = new HashSet<string>();

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("#") || !line.Contains("=")) continue;
                int eq     = line.IndexOf('=');
                string key = line.Substring(0, eq).Trim();
                if (updates.TryGetValue(key, out string val)) { lines[i] = $"{key}={val}"; touched.Add(key); }
            }

            var newKeys = new List<string>();
            foreach (var kv in updates) if (!touched.Contains(kv.Key)) newKeys.Add(kv.Key);
            if (newKeys.Count > 0)
            {
                lines.Add("");
                lines.Add("# ── Google Sheets ──────────────────────────────────────────────────────────");
                foreach (var k in newKeys) lines.Add($"{k}={updates[k]}");
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
                else switch (c)
                {
                    case '"':  inQ = true; break;
                    case ',':  row.Add(cell.ToString()); cell.Clear(); break;
                    case '\n': row.Add(cell.ToString()); cell.Clear(); result.Add(row); row = new List<string>(); break;
                    default:   cell.Append(c); break;
                }
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); result.Add(row); }
            return result;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static int    Col(List<string> h, string n) { for (int i = 0; i < h.Count; i++) if (string.Equals(h[i].Trim(), n, StringComparison.OrdinalIgnoreCase)) return i; return -1; }
        static int    ColAny(List<string> h, params string[] names) { foreach (var n in names) { int i = Col(h, n); if (i >= 0) return i; } return -1; }
        static string V(List<string> row, int idx)  => idx >= 0 && idx < row.Count ? row[idx].Trim() : "";
        static string Cap(string s)                 => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1).ToLower();

        static bool EnsureConfig()
        {
            if (!string.IsNullOrEmpty(QuestionsUrl)) return true;
            Debug.LogError("[GoogleSheets] URL не задан. Откройте: UstAldan Quiz → Google Sheets → ⚙ Настройки");
            GoogleSheetsSettingsWindow.Open();
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Settings window
    // ─────────────────────────────────────────────────────────────────────────

    public class GoogleSheetsSettingsWindow : EditorWindow
    {
        string _qUrl, _lUrl;

        public static void Open()
        {
            var w = GetWindow<GoogleSheetsSettingsWindow>("Google Sheets — Настройки");
            w.minSize = new Vector2(600, 340);
            w._qUrl = EditorPrefs.GetString("GS_QuestionsUrl", "");
            w._lUrl = EditorPrefs.GetString("GS_LocaleUrl",    "");
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Google Sheets — Настройки импорта", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Как получить URL для каждого листа:\n" +
                "1. Откройте таблицу → перейдите на нужный лист (вкладку)\n" +
                "2. Файл → Поделиться → Опубликовать в интернете\n" +
                "3. В первом выпадающем выберите название листа\n" +
                "4. Во втором выберите «Значения, разделённые запятыми (CSV)»\n" +
                "5. Нажмите «Опубликовать» → скопируйте ссылку",
                MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("URL листа «Вопросы» (обязательно):", EditorStyles.boldLabel);
            _qUrl = EditorGUILayout.TextField(_qUrl);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("URL листа «Локализация» (необязательно):", EditorStyles.boldLabel);
            _lUrl = EditorGUILayout.TextField(_lUrl);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Сохранить", GUILayout.Height(32)))
            {
                EditorPrefs.SetString("GS_QuestionsUrl", _qUrl.Trim());
                EditorPrefs.SetString("GS_LocaleUrl",    _lUrl.Trim());
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
