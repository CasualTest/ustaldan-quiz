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
    // Лист «Вопросы» (первая строка — заголовки):
    //   id | category_id | category_name | question | question_sah |
    //   answer1 | answer2 | answer3 | answer4 | correct_index |
    //   difficulty | fact_after_ru | fact_after_sah | image_url
    //
    // Лист «Словосоставление» (первая строка — заголовки):
    //   id | category_id | category_name | question | question_sah |
    //   word_answer | word_answer_sah | extra_letters |
    //   image_url | image_url2 | image_url3 | image_url4 |
    //   difficulty | fact_after_ru | fact_after_sah
    //   (question_type = "word_builder" проставляется автоматически)
    //
    // Лист «Локализация»:
    //   key | ru | sah
    //
    // Настройки: UstAldan Quiz → Google Sheets → ⚙ Настройки
    // ─────────────────────────────────────────────────────────────────────────

    public static class GoogleSheetsImporter
    {
        private const string PrefQUrl  = "GS_QuestionsUrl";
        private const string PrefWBUrl = "GS_WordBuilderUrl";
        private const string PrefLUrl  = "GS_LocaleUrl";

        private static string QuestionsUrl   => EditorPrefs.GetString(PrefQUrl,  "");
        private static string WordBuilderUrl => EditorPrefs.GetString(PrefWBUrl, "");
        private static string LocaleUrl      => EditorPrefs.GetString(PrefLUrl,  "");

        private const string CategoriesDir = "Assets/ScriptableObjects/Categories";
        private const string DatabaseDir   = "Assets/ScriptableObjects/Database";
        private const string LocaleDir     = "Assets/Resources/Locales";
        private const string JsonPath      = "Assets/Resources/questions.json";

        // ── Menu ──────────────────────────────────────────────────────────────

        [MenuItem("UstAldan Quiz/Google Sheets/⚙ Настройки", priority = 100)]
        public static void OpenSettings() => GoogleSheetsSettingsWindow.Open();

        [MenuItem("UstAldan Quiz/Google Sheets/↓ Импортировать всё", priority = 200)]
        public static void ImportAll()
        {
            if (!EnsureConfig()) return;
            AssetDatabase.Refresh();

            var (stdEntries, stdCats) = DownloadAndParseStandard(QuestionsUrl) ?? default;
            var (wbEntries,  wbCats)  = !string.IsNullOrEmpty(WordBuilderUrl)
                ? DownloadAndParseWordBuilder(WordBuilderUrl) ?? default
                : default;

            var allEntries = Merge(stdEntries, wbEntries);
            var allCats    = MergeCats(stdCats, wbCats);

            if (allEntries != null)
            {
                SaveJson(allEntries, allCats ?? new Dictionary<string, QuestionCategory>());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[GoogleSheets] Импорт завершён.");
            }

            if (!string.IsNullOrEmpty(LocaleUrl))
            {
                var csv = Download(LocaleUrl);
                if (csv != null) ProcessLocale(csv);
            }
        }

        [MenuItem("UstAldan Quiz/Google Sheets/↓ Вопросы", priority = 201)]
        public static void ImportQuestionsMenu()
        {
            if (!EnsureConfig()) return;
            AssetDatabase.Refresh();

            var result = DownloadAndParseStandard(QuestionsUrl);
            if (result == null) return;
            var (newEntries, newCats) = result.Value;

            // Сохраняем WB-вопросы из существующего JSON
            var existing = LoadExistingEntries();
            var wbExisting = FilterByType(existing, "word_builder");

            SaveJson(Merge(newEntries, wbExisting), MergeCatsFromJson(newCats, wbExisting));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("UstAldan Quiz/Google Sheets/↓ Словосоставление", priority = 202)]
        public static void ImportWordBuilderMenu()
        {
            if (string.IsNullOrEmpty(WordBuilderUrl))
            {
                Debug.LogWarning("[GoogleSheets] URL листа «Словосоставление» не задан. Откройте: UstAldan Quiz → Google Sheets → ⚙ Настройки");
                GoogleSheetsSettingsWindow.Open();
                return;
            }
            AssetDatabase.Refresh();

            var result = DownloadAndParseWordBuilder(WordBuilderUrl);
            if (result == null) return;
            var (newEntries, newCats) = result.Value;

            // Сохраняем обычные вопросы из существующего JSON
            var existing    = LoadExistingEntries();
            var stdExisting = FilterByType(existing, "standard_or_empty");

            SaveJson(Merge(stdExisting, newEntries), MergeCatsFromJson(newCats, stdExisting));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("UstAldan Quiz/Google Sheets/↓ Вопросы из локального CSV", priority = 210)]
        public static void ImportQuestionsFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Выберите CSV с вопросами", "", "csv");
            if (string.IsNullOrEmpty(path)) return;
            AssetDatabase.Refresh();
            string csv = File.ReadAllText(path, Encoding.UTF8);
            var result = ParseStandard(csv);
            if (result == null) return;
            var (newEntries, newCats) = result.Value;
            var existing   = LoadExistingEntries();
            var wbExisting = FilterByType(existing, "word_builder");
            SaveJson(Merge(newEntries, wbExisting), MergeCatsFromJson(newCats, wbExisting));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("UstAldan Quiz/Google Sheets/↓ Словосоставление из локального CSV", priority = 211)]
        public static void ImportWordBuilderFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Выберите CSV со словосоставлением", "", "csv");
            if (string.IsNullOrEmpty(path)) return;
            AssetDatabase.Refresh();
            string csv = File.ReadAllText(path, Encoding.UTF8);
            var result = ParseWordBuilder(csv);
            if (result == null) return;
            var (newEntries, newCats) = result.Value;
            var existing    = LoadExistingEntries();
            var stdExisting = FilterByType(existing, "standard_or_empty");
            SaveJson(Merge(stdExisting, newEntries), MergeCatsFromJson(newCats, stdExisting));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("UstAldan Quiz/Google Sheets/↓ Локализация", priority = 220)]
        public static void ImportLocaleMenu()
        {
            if (string.IsNullOrEmpty(LocaleUrl))
            {
                Debug.LogWarning("[GoogleSheets] URL листа «Локализация» не задан. Откройте: UstAldan Quiz → Google Sheets → ⚙ Настройки");
                GoogleSheetsSettingsWindow.Open();
                return;
            }
            var csv = Download(LocaleUrl);
            if (csv != null) ProcessLocale(csv);
            AssetDatabase.Refresh();
        }

        // ── Download & parse wrappers ─────────────────────────────────────────

        static (List<QuestionDatabase.QuestionJsonEntry>, Dictionary<string, QuestionCategory>)? DownloadAndParseStandard(string url)
        {
            var csv = Download(url);
            return csv != null ? ParseStandard(csv) : null;
        }

        static (List<QuestionDatabase.QuestionJsonEntry>, Dictionary<string, QuestionCategory>)? DownloadAndParseWordBuilder(string url)
        {
            var csv = Download(url);
            return csv != null ? ParseWordBuilder(csv) : null;
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
                Debug.LogError($"[GoogleSheets] HTTP {(int)r.StatusCode} {r.StatusDescription}\nURL: {url}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GoogleSheets] Ошибка загрузки: {e.Message}");
                return null;
            }
        }

        // ── Standard questions parser ─────────────────────────────────────────

        static (List<QuestionDatabase.QuestionJsonEntry>, Dictionary<string, QuestionCategory>)? ParseStandard(string csv)
        {
            var rows = ParseCsv(csv);
            if (rows.Count < 2) { Debug.LogWarning("[GoogleSheets] Лист вопросов пустой."); return null; }

            var hdr = rows[0];
            int iId      = ColAny(hdr, "id", "ID");
            int iCatId   = ColAny(hdr, "category_id", "category", "Category");
            int iCatN    = ColAny(hdr, "category_name", "category", "Category");
            int iQ       = ColAny(hdr, "question", "question_ru", "Question_RU");
            int iQSah    = ColAny(hdr, "question_sah", "Question_SAH");
            int iA1      = ColAny(hdr, "answer1", "answer_1", "Answer_1");
            int iA2      = ColAny(hdr, "answer2", "answer_2", "Answer_2");
            int iA3      = ColAny(hdr, "answer3", "answer_3", "Answer_3");
            int iA4      = ColAny(hdr, "answer4", "answer_4", "Answer_4");
            int iCorr    = ColAny(hdr, "correct_index", "Correct_Index");
            int iDiff    = ColAny(hdr, "difficulty", "Difficulty");
            int iFactRu  = ColAny(hdr, "fact_after_ru", "Fact_After_RU", "fact_after", "Fact_After");
            int iFactSah = ColAny(hdr, "fact_after_sah", "Fact_After_SAH");
            int iImgUrl  = ColAny(hdr, "imageurl", "image_url", "ImageURL", "imageURL");

            if (iQ < 0 || iA1 < 0)
            {
                Debug.LogError("[GoogleSheets] Не найдены столбцы question / answer1.\nЗаголовки: " + string.Join(", ", hdr));
                return null;
            }

            EnsureAssetFolder(CategoriesDir);
            EnsureAssetFolder(DatabaseDir);
            EnsureAssetFolder("Assets/Resources");

            var catCache = new Dictionary<string, QuestionCategory>(StringComparer.OrdinalIgnoreCase);
            var entries  = new List<QuestionDatabase.QuestionJsonEntry>();
            int skipped  = 0;

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                string q = V(row, iQ);
                if (string.IsNullOrWhiteSpace(q)) { skipped++; continue; }

                string rowId   = iId >= 0 ? V(row, iId) : "";
                if (string.IsNullOrWhiteSpace(rowId)) rowId = r.ToString();
                string catId   = (iCatId >= 0 ? V(row, iCatId) : "general").ToLower();
                string catName = iCatN  >= 0 ? V(row, iCatN) : catId;
                if (string.IsNullOrWhiteSpace(catId)) catId = "general";

                var answers = new[] { V(row, iA1), V(row, iA2), V(row, iA3), V(row, iA4) };
                int corr = iCorr >= 0 && int.TryParse(V(row, iCorr), out int ci) ? Mathf.Clamp(ci, 0, 3) : 0;
                int diff = iDiff >= 0 && int.TryParse(V(row, iDiff), out int d)  ? Mathf.Clamp(d,  1, 3) : 1;
                if (corr != 0) (answers[0], answers[corr]) = (answers[corr], answers[0]);

                if (!catCache.ContainsKey(catId))
                    catCache[catId] = GetOrCreateCategory(catId, catName);

                entries.Add(new QuestionDatabase.QuestionJsonEntry
                {
                    id          = $"Q{rowId.Trim().PadLeft(3, '0')}",
                    categoryId  = catId,
                    questionRu  = q,
                    questionSah = iQSah    >= 0 ? V(row, iQSah)    : "",
                    answers     = answers,
                    difficulty  = diff,
                    factRu      = iFactRu  >= 0 ? V(row, iFactRu)  : "",
                    factSah     = iFactSah >= 0 ? V(row, iFactSah) : "",
                    imageUrl    = iImgUrl  >= 0 ? V(row, iImgUrl)  : "",
                });
            }

            Debug.Log($"[GoogleSheets] Вопросы — распознано: {entries.Count}, пропущено: {skipped}.");
            return (entries, catCache);
        }

        // ── Word-builder sheet parser ─────────────────────────────────────────

        static (List<QuestionDatabase.QuestionJsonEntry>, Dictionary<string, QuestionCategory>)? ParseWordBuilder(string csv)
        {
            var rows = ParseCsv(csv);
            if (rows.Count < 2) { Debug.LogWarning("[GoogleSheets] Лист «Словосоставление» пустой."); return null; }

            var hdr = rows[0];
            int iId         = ColAny(hdr, "id", "ID");
            int iCatId      = ColAny(hdr, "category_id", "category", "Category");
            int iCatN       = ColAny(hdr, "category_name", "category", "Category");
            int iQ          = ColAny(hdr, "question", "question_ru", "Question_RU");
            int iQSah       = ColAny(hdr, "question_sah", "Question_SAH");
            int iWordAns    = ColAny(hdr, "word_answer",     "WordAnswer");
            int iWordAnsSah = ColAny(hdr, "word_answer_sah", "WordAnswerSah");
            int iExtraLet   = ColAny(hdr, "extra_letters",   "ExtraLetters");
            int iImgUrl     = ColAny(hdr, "image_url",  "imageurl",  "ImageURL",  "imageURL");
            int iImgUrl2    = ColAny(hdr, "image_url2", "imageurl2", "ImageURL2");
            int iImgUrl3    = ColAny(hdr, "image_url3", "imageurl3", "ImageURL3");
            int iImgUrl4    = ColAny(hdr, "image_url4", "imageurl4", "ImageURL4");
            int iDiff       = ColAny(hdr, "difficulty", "Difficulty");
            int iFactRu     = ColAny(hdr, "fact_after_ru",  "Fact_After_RU",  "fact_after", "Fact_After");
            int iFactSah    = ColAny(hdr, "fact_after_sah", "Fact_After_SAH");

            if (iWordAns < 0)
            {
                Debug.LogError("[GoogleSheets] Лист «Словосоставление»: не найден столбец word_answer.\nЗаголовки: " + string.Join(", ", hdr));
                return null;
            }

            EnsureAssetFolder(CategoriesDir);
            EnsureAssetFolder(DatabaseDir);
            EnsureAssetFolder("Assets/Resources");

            var catCache = new Dictionary<string, QuestionCategory>(StringComparer.OrdinalIgnoreCase);
            var entries  = new List<QuestionDatabase.QuestionJsonEntry>();
            int skipped  = 0;

            for (int r = 1; r < rows.Count; r++)
            {
                var row      = rows[r];
                string word  = V(row, iWordAns);
                if (string.IsNullOrWhiteSpace(word)) { skipped++; continue; }

                string rowId   = iId >= 0 ? V(row, iId) : "";
                if (string.IsNullOrWhiteSpace(rowId)) rowId = r.ToString();
                string catId   = (iCatId >= 0 ? V(row, iCatId) : "general").ToLower();
                string catName = iCatN >= 0 ? V(row, iCatN) : catId;
                if (string.IsNullOrWhiteSpace(catId)) catId = "general";
                int diff = iDiff >= 0 && int.TryParse(V(row, iDiff), out int d) ? Mathf.Clamp(d, 1, 3) : 1;

                if (!catCache.ContainsKey(catId))
                    catCache[catId] = GetOrCreateCategory(catId, catName);

                entries.Add(new QuestionDatabase.QuestionJsonEntry
                {
                    id            = $"W{rowId.Trim().PadLeft(3, '0')}",
                    categoryId    = catId,
                    questionRu    = iQ      >= 0 ? V(row, iQ)      : "",
                    questionSah   = iQSah   >= 0 ? V(row, iQSah)   : "",
                    answers       = new string[4],
                    difficulty    = diff,
                    factRu        = iFactRu    >= 0 ? V(row, iFactRu)    : "",
                    factSah       = iFactSah   >= 0 ? V(row, iFactSah)   : "",
                    imageUrl      = iImgUrl    >= 0 ? V(row, iImgUrl)    : "",
                    questionType  = "word_builder",
                    wordAnswer    = word,
                    wordAnswerSah = iWordAnsSah >= 0 ? V(row, iWordAnsSah) : "",
                    extraLetters  = iExtraLet   >= 0 ? V(row, iExtraLet)   : "",
                    imageUrl2     = iImgUrl2    >= 0 ? V(row, iImgUrl2)    : "",
                    imageUrl3     = iImgUrl3    >= 0 ? V(row, iImgUrl3)    : "",
                    imageUrl4     = iImgUrl4    >= 0 ? V(row, iImgUrl4)    : "",
                });
            }

            Debug.Log($"[GoogleSheets] Словосоставление — распознано: {entries.Count}, пропущено: {skipped}.");
            return (entries, catCache);
        }

        // ── Save & update DB ──────────────────────────────────────────────────

        static void SaveJson(List<QuestionDatabase.QuestionJsonEntry> entries, Dictionary<string, QuestionCategory> catCache)
        {
            string json = JsonUtility.ToJson(
                new QuestionDatabase.QuestionsJson { questions = entries.ToArray() },
                prettyPrint: true);
            File.WriteAllText(JsonPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            AssetDatabase.ImportAsset(JsonPath, ImportAssetOptions.ForceUpdate);

            var allCats = new List<QuestionCategory>(catCache.Values);
            allCats.Sort((a, b) => string.Compare(a.categoryId, b.categoryId, StringComparison.Ordinal));
            UpdateAllDatabases(allCats);

            Debug.Log($"[GoogleSheets] Сохранено {entries.Count} вопросов → {JsonPath}");
        }

        // ── Existing JSON helpers ─────────────────────────────────────────────

        static List<QuestionDatabase.QuestionJsonEntry> LoadExistingEntries()
        {
            if (!File.Exists(JsonPath)) return new List<QuestionDatabase.QuestionJsonEntry>();
            string json = File.ReadAllText(JsonPath, Encoding.UTF8);
            if (json.Length > 0 && json[0] == '﻿') json = json.Substring(1);
            var data = JsonUtility.FromJson<QuestionDatabase.QuestionsJson>(json);
            return data?.questions != null
                ? new List<QuestionDatabase.QuestionJsonEntry>(data.questions)
                : new List<QuestionDatabase.QuestionJsonEntry>();
        }

        // mode: "word_builder" → WB only; "standard_or_empty" → non-WB
        static List<QuestionDatabase.QuestionJsonEntry> FilterByType(
            List<QuestionDatabase.QuestionJsonEntry> entries, string mode)
        {
            var result = new List<QuestionDatabase.QuestionJsonEntry>();
            foreach (var e in entries)
            {
                bool isWB = e.questionType == "word_builder";
                if (mode == "word_builder"      && isWB)  result.Add(e);
                if (mode == "standard_or_empty" && !isWB) result.Add(e);
            }
            return result;
        }

        static List<QuestionDatabase.QuestionJsonEntry> Merge(
            List<QuestionDatabase.QuestionJsonEntry> a,
            List<QuestionDatabase.QuestionJsonEntry> b)
        {
            var result = new List<QuestionDatabase.QuestionJsonEntry>();
            if (a != null) result.AddRange(a);
            if (b != null) result.AddRange(b);
            return result;
        }

        static Dictionary<string, QuestionCategory> MergeCats(
            Dictionary<string, QuestionCategory> a,
            Dictionary<string, QuestionCategory> b)
        {
            var result = new Dictionary<string, QuestionCategory>(StringComparer.OrdinalIgnoreCase);
            if (a != null) foreach (var kv in a) result[kv.Key] = kv.Value;
            if (b != null) foreach (var kv in b) result[kv.Key] = kv.Value;
            return result;
        }

        static Dictionary<string, QuestionCategory> MergeCatsFromJson(
            Dictionary<string, QuestionCategory> existing,
            List<QuestionDatabase.QuestionJsonEntry> jsonEntries)
        {
            // Для JSON-записей категории уже существуют в проекте — подгружаем из Assets
            var result = new Dictionary<string, QuestionCategory>(StringComparer.OrdinalIgnoreCase);
            if (existing != null) foreach (var kv in existing) result[kv.Key] = kv.Value;
            foreach (var e in jsonEntries)
            {
                if (string.IsNullOrEmpty(e.categoryId) || result.ContainsKey(e.categoryId)) continue;
                var cat = GetOrCreateCategory(e.categoryId, e.categoryId);
                result[e.categoryId] = cat;
            }
            return result;
        }

        // ── DB update ─────────────────────────────────────────────────────────

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

        static void UpdateAllDatabases(List<QuestionCategory> allCats)
        {
            var guids = AssetDatabase.FindAssets("t:QuestionDatabase");

            if (guids.Length == 0)
            {
                var master = ScriptableObject.CreateInstance<QuestionDatabase>();
                string mp  = $"{DatabaseDir}/QuestionDatabase.asset";
                AssetDatabase.CreateAsset(master, mp);
                guids = new[] { AssetDatabase.AssetPathToGUID(mp) };
                Debug.Log("[GoogleSheets] Создана мастер-база QuestionDatabase.asset");
            }

            foreach (var guid in guids)
            {
                var db = AssetDatabase.LoadAssetAtPath<QuestionDatabase>(AssetDatabase.GUIDToAssetPath(guid));
                if (db == null) continue;
                db.categories   = new List<QuestionCategory>(allCats);
                db.allQuestions = new List<QuestionData>(); // загружается из JSON в рантайме
                EditorUtility.SetDirty(db);
            }

            Debug.Log($"[GoogleSheets] Обновлено баз данных: {guids.Length} — категорий: {allCats.Count}.");
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
        static string V(List<string> row, int idx) => idx >= 0 && idx < row.Count ? row[idx].Trim() : "";
        static string Cap(string s)                => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1).ToLower();

        static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
            string name   = Path.GetFileName(path);
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

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
        string _qUrl, _wbUrl, _lUrl;

        public static void Open()
        {
            var w = GetWindow<GoogleSheetsSettingsWindow>("Google Sheets — Настройки");
            w.minSize = new Vector2(620, 430);
            w._qUrl  = EditorPrefs.GetString("GS_QuestionsUrl",   "");
            w._wbUrl = EditorPrefs.GetString("GS_WordBuilderUrl",  "");
            w._lUrl  = EditorPrefs.GetString("GS_LocaleUrl",       "");
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
            EditorGUILayout.LabelField("URL листа «Словосоставление» (необязательно):", EditorStyles.boldLabel);
            _wbUrl = EditorGUILayout.TextField(_wbUrl);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("URL листа «Локализация» (необязательно):", EditorStyles.boldLabel);
            _lUrl = EditorGUILayout.TextField(_lUrl);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Сохранить", GUILayout.Height(32)))
            {
                EditorPrefs.SetString("GS_QuestionsUrl",  _qUrl.Trim());
                EditorPrefs.SetString("GS_WordBuilderUrl", _wbUrl.Trim());
                EditorPrefs.SetString("GS_LocaleUrl",      _lUrl.Trim());
                Debug.Log("[GoogleSheets] Настройки сохранены.");
                Close();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Формат листа «Вопросы»:", EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(
                "id | category_id | category_name | question | question_sah | answer1 | answer2 | answer3 | answer4 | correct_index | difficulty | fact_after_ru | fact_after_sah | image_url",
                EditorStyles.helpBox, GUILayout.Height(32));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Формат листа «Словосоставление»:", EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(
                "id | category_id | category_name | question | question_sah | word_answer | word_answer_sah | extra_letters | image_url | image_url2 | image_url3 | image_url4 | difficulty | fact_after_ru | fact_after_sah",
                EditorStyles.helpBox, GUILayout.Height(32));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Формат листа «Локализация»:", EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel("key | ru | sah", EditorStyles.helpBox, GUILayout.Height(22));
        }
    }
}
