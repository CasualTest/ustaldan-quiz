using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UstAldanQuiz.Data;

/// <summary>
/// Читает Assets/Data/questions.csv и создаёт/обновляет ScriptableObject-ассеты вопросов.
/// Меню: UstAldan Quiz → Import Questions from CSV
/// </summary>
public static class CsvQuestionImporter
{
    private const string CsvPath       = "Assets/Data/questions.csv";
    private const string CategoriesDir = "Assets/ScriptableObjects/Categories";
    private const string QuestionsDir  = "Assets/ScriptableObjects/Questions";
    private const string DatabaseDir   = "Assets/ScriptableObjects/Database";

    // Соответствие: имя в CSV → (categoryId, displayName, имя папки/файла)
    private static readonly Dictionary<string, CategoryMeta> KnownCategories =
        new Dictionary<string, CategoryMeta>(StringComparer.OrdinalIgnoreCase)
        {
            { "History",   new CategoryMeta("history",   "История",   "History")   },
            { "Culture",   new CategoryMeta("culture",   "Культура",  "Culture")   },
            { "People",    new CategoryMeta("people",    "Люди",      "People")    },
            { "Geography", new CategoryMeta("geography", "География", "Geography") },
        };

    [MenuItem("UstAldan Quiz/Import Questions from CSV")]
    public static void ImportQuestions()
    {
        string fullPath = Path.GetFullPath(CsvPath);
        if (!File.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("Ошибка", $"Файл не найден:\n{CsvPath}", "OK");
            return;
        }

        string[] lines = File.ReadAllLines(fullPath, Encoding.UTF8);
        if (lines.Length < 2)
        {
            EditorUtility.DisplayDialog("Ошибка", "CSV пустой или содержит только заголовок.", "OK");
            return;
        }

        // --- Разбор заголовка ---
        string[] headers = ParseLine(lines[0]);
        int colId      = Find(headers, "ID");
        int colCat     = Find(headers, "Category");
        int colRu      = Find(headers, "Question_RU");
        int colA1      = Find(headers, "Answer_1");
        int colA2      = Find(headers, "Answer_2");
        int colA3      = Find(headers, "Answer_3");
        int colA4      = Find(headers, "Answer_4");
        int colCorrect = Find(headers, "Correct_Index");

        if (colId < 0 || colCat < 0 || colRu < 0 ||
            colA1 < 0 || colA2 < 0 || colA3 < 0 || colA4 < 0 || colCorrect < 0)
        {
            EditorUtility.DisplayDialog("Ошибка",
                "CSV не содержит всех обязательных колонок:\n" +
                "ID, Category, Question_RU, Answer_1–4, Correct_Index", "OK");
            return;
        }

        EnsureFolder(CategoriesDir);
        EnsureFolder(QuestionsDir);
        EnsureFolder(DatabaseDir);

        var categoryCache      = new Dictionary<string, QuestionCategory>(StringComparer.OrdinalIgnoreCase);
        var questionsByCategory = new Dictionary<string, List<QuestionData>>(StringComparer.OrdinalIgnoreCase);
        int created = 0, updated = 0, skipped = 0;

        // --- Обработка строк ---
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] f = ParseLine(line);

            string idStr      = Cell(f, colId);
            string catStr     = Cell(f, colCat);
            string questionRu = Cell(f, colRu);
            string a1 = Cell(f, colA1), a2 = Cell(f, colA2),
                   a3 = Cell(f, colA3), a4 = Cell(f, colA4);
            string correctStr = Cell(f, colCorrect);

            if (string.IsNullOrWhiteSpace(idStr) || string.IsNullOrWhiteSpace(catStr))
            {
                Debug.LogWarning($"[CsvImporter] Строка {i + 1}: пропущен ID или Category — пропускаю");
                skipped++;
                continue;
            }

            if (!int.TryParse(idStr, out int qId))
            {
                Debug.LogWarning($"[CsvImporter] Строка {i + 1}: некорректный ID '{idStr}' — пропускаю");
                skipped++;
                continue;
            }

            if (!int.TryParse(correctStr, out int correctIdx) || correctIdx < 0 || correctIdx > 3)
            {
                Debug.LogWarning($"[CsvImporter] Строка {i + 1}: некорректный Correct_Index '{correctStr}' (должен быть 0–3) — пропускаю");
                skipped++;
                continue;
            }

            QuestionCategory catAsset = GetOrCreateCategory(catStr, categoryCache);
            if (catAsset == null) { skipped++; continue; }

            // Папка для вопросов категории
            string folderName = ResolveFolderName(catStr);
            string questionFolder = $"{QuestionsDir}/{folderName}";
            EnsureFolder(questionFolder);

            // answers[0] = правильный ответ
            string[] raw = { a1, a2, a3, a4 };
            string[] answers = new string[4];
            answers[0] = raw[correctIdx];
            int slot = 1;
            for (int a = 0; a < 4; a++)
                if (a != correctIdx) answers[slot++] = raw[a];

            // Создать или обновить QuestionData asset
            string assetPath = $"{questionFolder}/Q{qId:D2}.asset";
            QuestionData qData = AssetDatabase.LoadAssetAtPath<QuestionData>(assetPath);
            bool isNew = qData == null;
            if (isNew) qData = ScriptableObject.CreateInstance<QuestionData>();

            qData.category     = catAsset;
            qData.questionText = questionRu;
            qData.answers      = answers;
            qData.difficulty   = 1;

            if (isNew)
            {
                AssetDatabase.CreateAsset(qData, assetPath);
                created++;
            }
            else
            {
                EditorUtility.SetDirty(qData);
                updated++;
            }

            if (!questionsByCategory.ContainsKey(catStr))
                questionsByCategory[catStr] = new List<QuestionData>();
            questionsByCategory[catStr].Add(qData);
        }

        // --- Обновление/создание баз данных ---
        foreach (var kvp in questionsByCategory)
        {
            string folder = ResolveFolderName(kvp.Key);
            string dbPath = $"{DatabaseDir}/{folder}Database.asset";

            QuestionDatabase db = AssetDatabase.LoadAssetAtPath<QuestionDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<QuestionDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
                Debug.Log($"[CsvImporter] Создана база данных: {dbPath}");
            }

            // Мерж: добавляем новые вопросы из CSV, не трогаем существующие
            var merged = new List<QuestionData>(db.allQuestions.Where(q => q != null));
            foreach (var csvQ in kvp.Value)
                if (!merged.Contains(csvQ))
                    merged.Add(csvQ);

            db.categories   = new List<QuestionCategory> { categoryCache[kvp.Key] };
            db.allQuestions = merged;
            EditorUtility.SetDirty(db);
        }

        // --- Обновляем мастер-базу (используется в MainMenuUI) ---
        // Ищем базу с наибольшим числом категорий — это и есть мастер
        QuestionDatabase masterDb = null;
        foreach (var g in AssetDatabase.FindAssets("t:QuestionDatabase"))
        {
            var d = AssetDatabase.LoadAssetAtPath<QuestionDatabase>(
                        AssetDatabase.GUIDToAssetPath(g));
            if (d == null) continue;
            if (masterDb == null || d.categories.Count > masterDb.categories.Count)
                masterDb = d;
        }

        if (masterDb != null)
        {
            var allImported = questionsByCategory.Values
                .SelectMany(list => list)
                .ToList();

            var masterMerged = new List<QuestionData>(
                masterDb.allQuestions.Where(q => q != null));
            foreach (var q in allImported)
                if (!masterMerged.Contains(q)) masterMerged.Add(q);

            masterDb.allQuestions = masterMerged;
            EditorUtility.SetDirty(masterDb);
            Debug.Log($"[CsvImporter] Мастер-база обновлена: {masterMerged.Count} вопросов.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string report = $"Импорт завершён!\n" +
                        $"Создано: {created}\n" +
                        $"Обновлено: {updated}\n" +
                        $"Пропущено: {skipped}";
        EditorUtility.DisplayDialog("CSV Импорт", report, "OK");
        Debug.Log($"[CsvImporter] {report.Replace('\n', ' ')}");
    }

    // ── Хелперы ──────────────────────────────────────────────────────────────

    private static QuestionCategory GetOrCreateCategory(string catStr,
        Dictionary<string, QuestionCategory> cache)
    {
        if (cache.TryGetValue(catStr, out var hit)) return hit;

        string folder  = ResolveFolderName(catStr);
        string path    = $"{CategoriesDir}/{folder}.asset";

        QuestionCategory cat = AssetDatabase.LoadAssetAtPath<QuestionCategory>(path);
        if (cat == null)
        {
            cat = ScriptableObject.CreateInstance<QuestionCategory>();
            if (KnownCategories.TryGetValue(catStr, out var meta))
            {
                cat.categoryId  = meta.Id;
                cat.displayName = meta.Display;
            }
            else
            {
                cat.categoryId  = catStr.ToLowerInvariant();
                cat.displayName = catStr;
            }
            AssetDatabase.CreateAsset(cat, path);
            Debug.Log($"[CsvImporter] Создана категория: {path}");
        }

        cache[catStr] = cat;
        return cat;
    }

    private static string ResolveFolderName(string catStr) =>
        KnownCategories.TryGetValue(catStr, out var m) ? m.Folder : catStr;

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        string name   = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static int Find(string[] headers, string name)
    {
        for (int i = 0; i < headers.Length; i++)
            if (string.Equals(headers[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private static string Cell(string[] fields, int index) =>
        index >= 0 && index < fields.Length ? fields[index].Trim() : string.Empty;

    // RFC-4180 CSV парсер с поддержкой кавычек
    private static string[] ParseLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                { sb.Append('"'); i++; }
                else
                    inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            { fields.Add(sb.ToString()); sb.Clear(); }
            else
                sb.Append(c);
        }
        fields.Add(sb.ToString());
        return fields.ToArray();
    }

    private readonly struct CategoryMeta
    {
        public readonly string Id, Display, Folder;
        public CategoryMeta(string id, string display, string folder)
        { Id = id; Display = display; Folder = folder; }
    }
}
