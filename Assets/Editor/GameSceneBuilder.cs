using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;

/// <summary>
/// UstAldan Quiz → Game Setup → *
/// Создаёт ассеты вопросов, сцены MainMenu / QuestionMap / Results / Roadmap,
/// и добавляет сцены в Build Settings.
///
/// Разбит на partial-файлы:
///   GameSceneBuilder.cs          — меню, константы, общие точки входа
///   GameSceneBuilder.Intro.cs    — DoBuildIntro
///   GameSceneBuilder.MainMenu.cs — DoBuildMainMenu + CreateCategoryButtonPrefab
///   GameSceneBuilder.QuestionMap.cs — DoBuildQuestionMap + CreateTilePrefab
///   GameSceneBuilder.Results.cs  — DoBuildResults
///   GameSceneBuilder.Roadmap.cs  — DoBuildRoadmap + CreateRoadmapTilePrefab
///   GameSceneBuilder.Utils.cs    — все вспомогательные методы
/// </summary>
public static partial class GameSceneBuilder
{
    // =====================================================================
    // Цвета (светлая тема)
    // =====================================================================
    static readonly Color C_BG         = Hex("F5F0E8");
    static readonly Color C_PRIMARY     = Hex("2D6040");
    static readonly Color C_SECONDARY   = Hex("C8A84B");
    static readonly Color C_TEXT        = Hex("1A2A1A");
    static readonly Color C_TEXT2       = Hex("4A6A4A");
    static readonly Color C_BTN_PRI     = Hex("2D6040");
    static readonly Color C_BTN_SEC     = Hex("FFFFFF");
    static readonly Color C_TILE_DEF    = Hex("E8E0D0");
    static readonly Color C_CORRECT     = Hex("4CAF50");
    static readonly Color C_WRONG       = Hex("F44336");
    static readonly Color C_OVERLAY     = new Color(0, 0, 0, 0.55f);
    static readonly Color C_CARD        = Hex("FFFFFF");

    // =====================================================================
    // МЕНЮ
    // =====================================================================

    [MenuItem("UstAldan Quiz/Game Setup/1 — Build Intro Scene")]
    public static void BuildIntroScene() => DoBuildIntro();

    [MenuItem("UstAldan Quiz/Game Setup/2 — Build Main Menu Scene")]
    public static void BuildMainMenuScene() => DoBuildMainMenu();

    [MenuItem("UstAldan Quiz/Game Setup/3 — Build Question Map Scene")]
    public static void BuildQuestionMapScene() => DoBuildQuestionMap();

    [MenuItem("UstAldan Quiz/Game Setup/4 — Build Results Scene")]
    public static void BuildResultsScene() => DoBuildResults();

    [MenuItem("UstAldan Quiz/Game Setup/5 — Build Roadmap Scene")]
    public static void BuildRoadmapScene() => DoBuildRoadmap();

    [MenuItem("UstAldan Quiz/Game Setup/6 — Add Scenes to Build Settings")]
    public static void AddScenesToBuildSettings() => DoAddScenes();

    // Принудительно пересоздаёт prefab'ы и все сцены
    [MenuItem("UstAldan Quiz/Game Setup/FORCE REBUILD — пересоздать всё")]
    public static void ForceRebuildAll()
    {
        SetPTSansAsDefault();
        BuildUIPrefabs();
        DoBuildIntro();
        DoBuildMainMenu();
        DoBuildQuestionMap();
        DoBuildResults();
        DoBuildRoadmap();
        DoAddScenes();
        Debug.Log("[GameSceneBuilder] ✓ Всё пересоздано.");
    }

    static void SetPTSansAsDefault()
    {
        var ptSans = FindFont();
        if (ptSans == null)
        {
            Debug.LogWarning("[FontFix] PTSans SDF не найден — TMP default не изменён.");
            return;
        }

        var settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings == null)
        {
            Debug.LogWarning("[FontFix] TMP Settings asset не найден.");
            return;
        }

        var so   = new UnityEditor.SerializedObject(settings);
        var prop = so.FindProperty("m_defaultFontAsset");
        if (prop != null)
        {
            prop.objectReferenceValue = ptSans;
            so.ApplyModifiedProperties();
            UnityEditor.EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FontFix] TMP default font → {ptSans.name}  (больше не будет LiberationSans)");
        }
        else
        {
            Debug.LogWarning("[FontFix] Свойство m_defaultFontAsset не найдено в TMP Settings.");
        }
    }

    // =====================================================================
    // 6. BUILD SETTINGS
    // =====================================================================

    static void DoAddScenes()
    {
        // Порядок важен: Intro = 0, MainMenu = 1, QuestionMap = 2, Results = 3, Roadmap = 4
        string[] ordered = {
            "Assets/Scenes/Intro.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/QuestionMap.unity",
            "Assets/Scenes/Results.unity",
            "Assets/Scenes/Roadmap.unity",
        };

        var entries = new List<UnityEditor.EditorBuildSettingsScene>();
        foreach (var path in ordered)
        {
            if (!File.Exists(path)) { Debug.LogWarning($"[GameSceneBuilder] Файл не найден: {path}"); continue; }
            entries.Add(new UnityEditor.EditorBuildSettingsScene(path, true));
        }

        // Сохраняем остальные сцены (не из нашего списка) в конце
        foreach (var existing in UnityEditor.EditorBuildSettings.scenes)
        {
            if (!ordered.Contains(existing.path))
                entries.Add(existing);
        }

        UnityEditor.EditorBuildSettings.scenes = entries.ToArray();
        Debug.Log("[GameSceneBuilder] ✓ Сцены добавлены в Build Settings (Intro=0, MainMenu=1, QuestionMap=2, Results=3).");
    }
}
