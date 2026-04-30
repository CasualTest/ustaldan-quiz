using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;
using UstAldanQuiz.UI;
using UstAldanQuiz.Utils;

/// <summary>
/// UstAldan Quiz → Game Setup → *
/// Создаёт ассеты вопросов, сцены MainMenu / QuestionMap / Results,
/// и добавляет сцены в Build Settings.
/// </summary>
public static class GameSceneBuilder
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

    [MenuItem("UstAldan Quiz/Game Setup/0 — Build Intro Scene")]
    public static void BuildIntroScene() => DoBuildIntro();

    [MenuItem("UstAldan Quiz/Game Setup/1 — Create Question Assets")]
    public static void CreateQuestionAssets() => DoCreateQuestions();

    [MenuItem("UstAldan Quiz/Game Setup/2 — Build Main Menu Scene")]
    public static void BuildMainMenuScene() => DoBuildMainMenu();

    [MenuItem("UstAldan Quiz/Game Setup/3 — Build Question Map Scene")]
    public static void BuildQuestionMapScene() => DoBuildQuestionMap();

    [MenuItem("UstAldan Quiz/Game Setup/4 — Build Results Scene")]
    public static void BuildResultsScene() => DoBuildResults();

    [MenuItem("UstAldan Quiz/Game Setup/5 — Add Scenes to Build Settings")]
    public static void AddScenesToBuildSettings() => DoAddScenes();

    [MenuItem("UstAldan Quiz/Game Setup/RUN ALL (full setup)")]
    public static void RunAll()
    {
        DoBuildIntro();
        DoCreateQuestions();
        DoBuildMainMenu();
        DoBuildQuestionMap();
        DoBuildResults();
        DoAddScenes();
        Debug.Log("[GameSceneBuilder] ✓ Полная настройка завершена!");
    }

    // =====================================================================
    // 0. ИНТРО-СЦЕНА
    // =====================================================================

    static void DoBuildIntro()
    {
        OpenOrCreateScene("Assets/Scenes/Intro.unity");

        // Камера — чёрный фон
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.cullingMask     = 0;
        cam.depth           = -1;
        camGO.AddComponent<AudioListener>();

        SetupEventSystem();

        // Canvas
        var canvasGO = SetupCanvas("Intro");

        // Чёрный фон (пока RenderTexture не создана)
        var bg = MakeGO("Background", canvasGO.transform);
        Stretch(bg);
        bg.AddComponent<Image>().color = Color.black;

        // RawImage — сюда VideoPlayer рендерит кадры
        var videoDisplayGO = MakeGO("VideoDisplay", canvasGO.transform);
        Stretch(videoDisplayGO);
        var rawImage = videoDisplayGO.AddComponent<RawImage>();
        rawImage.color = Color.white;

        // Подсказка «нажмите чтобы пропустить»
        var font    = FindFont();
        var skipTMP = MakeTMP("SkipHint", canvasGO.transform,
                              "Нажмите, чтобы пропустить", 28,
                              new Color(1f, 1f, 1f, 0.55f), font);
        var skipRT = skipTMP.GetComponent<RectTransform>();
        skipRT.anchorMin = new Vector2(0f, 0f);
        skipRT.anchorMax = new Vector2(1f, 0f);
        skipRT.pivot     = new Vector2(0.5f, 0f);
        skipRT.anchoredPosition = new Vector2(0f, 60f);
        skipRT.sizeDelta        = new Vector2(0f, 50f);
        skipTMP.alignment = TextAlignmentOptions.Center;

        // VideoPlayer
        var vpGO        = new GameObject("VideoPlayer");
        var videoPlayer = vpGO.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake   = false;
        videoPlayer.renderMode    = VideoRenderMode.RenderTexture; // текстура задаётся в IntroUI.Start
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.waitForFirstFrame = true;

        // Ищем видеоклип в Assets/Videos/
        VideoClip clip = null;
        var clipGuids = AssetDatabase.FindAssets("t:VideoClip", new[] { "Assets/Videos" });
        if (clipGuids.Length > 0)
            clip = AssetDatabase.LoadAssetAtPath<VideoClip>(AssetDatabase.GUIDToAssetPath(clipGuids[0]));
        else
            Debug.LogWarning("[GameSceneBuilder] Видеофайл не найден в Assets/Videos/. Добавь его вручную.");

        // IntroUI
        var introGO = new GameObject("IntroManager");
        var introUI = introGO.AddComponent<IntroUI>();
        var soIntro = new SerializedObject(introUI);
        Prop(soIntro, "videoPlayer",  videoPlayer);
        Prop(soIntro, "videoDisplay", rawImage);
        if (clip != null) Prop(soIntro, "introClip", clip);
        soIntro.ApplyModifiedProperties();

        SaveScene("Assets/Scenes/Intro.unity");
        Debug.Log("[GameSceneBuilder] ✓ Intro сцена построена.");
    }

    // =====================================================================
    // 1. ВОПРОСЫ
    // =====================================================================

    static void DoCreateQuestions()
    {
        const string dir = "Assets/ScriptableObjects/Questions/History";
        Directory.CreateDirectory(dir);

        var historyCategory = FindCategory("history");
        if (historyCategory == null)
        {
            Debug.LogWarning("[GameSceneBuilder] Категория 'history' не найдена. " +
                             "Убедись что History.asset существует и имеет categoryId = history.");
        }

        var questions = new (string q, string[] a)[]
        {
            ("В каком году был образован Усть-Алданский улус?",
             new[]{"1925","1930","1920","1935"}),
            ("Как называется река, на берегах которой расположен Усть-Алданский улус?",
             new[]{"Алдан","Лена","Вилюй","Амга"}),
            ("Административный центр Усть-Алданского улуса?",
             new[]{"Борогонцы","Якутск","Намцы","Покровск"}),
            ("В каком регионе России находится Усть-Алданский улус?",
             new[]{"Республика Саха (Якутия)","Иркутская область","Магаданская область","Хабаровский край"}),
            ("Как называется якутский героический эпос?",
             new[]{"Олонхо","Манас","Калевала","Джангар"}),
            ("В каком году Олонхо было включено в список UNESCO?",
             new[]{"2005","2010","2000","2015"}),
            ("Как называется традиционный якутский праздник встречи лета?",
             new[]{"Ысыах","Сабантуй","Навруз","Масленица"}),
            ("Что означает слово «Саха» в названии республики?",
             new[]{"Название народа якутов","Название реки","Слово «земля»","Слово «север»"}),
            ("Какое традиционное жилище у якутов?",
             new[]{"Балаган (юрта)","Чум","Иглу","Яранга"}),
            ("Как называется традиционный якутский напиток из кобыльего молока?",
             new[]{"Кумыс","Чай","Квас","Айран"}),
            ("Какое дерево является символом Якутии?",
             new[]{"Лиственница","Берёза","Кедр","Сосна"}),
            ("Как называется традиционный якутский нож?",
             new[]{"Бытык","Тесак","Засапожник","Кинжал"}),
            ("Чем занимаются жители Усть-Алданского улуса?",
             new[]{"Скотоводством","Добычей нефти","Добычей золота","Добычей угля"}),
            ("Как называется якутская порода лошадей?",
             new[]{"Якутская порода","Монгольская","Степная","Тундровая"}),
            ("Что означает «Усть-» в названии улуса?",
             new[]{"Устье реки","Северный","Большой","Старый"}),
        };

        int created = 0;
        for (int i = 0; i < questions.Length; i++)
        {
            string path = $"{dir}/Q{i + 1:D2}.asset";
            if (File.Exists(path)) continue;

            var asset = ScriptableObject.CreateInstance<QuestionData>();
            asset.category     = historyCategory;
            asset.questionText = questions[i].q;
            asset.answers      = questions[i].a;
            asset.difficulty   = i < 5 ? 1 : i < 10 ? 2 : 3;
            AssetDatabase.CreateAsset(asset, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Добавить вопросы в базу данных (если есть)
        AddQuestionsToDatabase(dir, historyCategory);

        Debug.Log($"[GameSceneBuilder] Создано вопросов: {created} (из 15).");
    }

    static void AddQuestionsToDatabase(string questionDir, QuestionCategory category)
    {
        var dbGuids = AssetDatabase.FindAssets("t:QuestionDatabase");
        if (dbGuids.Length == 0) return;

        var db = AssetDatabase.LoadAssetAtPath<QuestionDatabase>(
            AssetDatabase.GUIDToAssetPath(dbGuids[0]));
        if (db == null) return;

        var so = new SerializedObject(db);
        var listProp = so.FindProperty("allQuestions");

        var questionGuids = AssetDatabase.FindAssets("t:QuestionData", new[] { questionDir });
        foreach (var guid in questionGuids)
        {
            var q = AssetDatabase.LoadAssetAtPath<QuestionData>(AssetDatabase.GUIDToAssetPath(guid));
            if (q == null) continue;

            bool alreadyIn = false;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == q)
                { alreadyIn = true; break; }
            }
            if (alreadyIn) continue;

            listProp.arraySize++;
            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = q;
        }
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    // =====================================================================
    // 2. СЦЕНА ГЛАВНОГО МЕНЮ
    // =====================================================================

    static void DoBuildMainMenu()
    {
        var scene = OpenOrCreateScene("Assets/Scenes/MainMenu.unity");

        var font = FindFont();
        var canvasGO = SetupCanvas(scene.name);
        SetupCamera();
        SetupEventSystem();

        // Фон (вне SafeArea)
        var bg = MakeGO("Background", canvasGO.transform);
        Stretch(bg);
        bg.AddComponent<Image>().color = C_BG;

        // SafeArea
        var safeArea = MakeGO("SafeArea", canvasGO.transform);
        Stretch(safeArea);
        safeArea.AddComponent<SafeArea>();

        var vlg = safeArea.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.padding = new RectOffset(40, 40, 50, 40);
        vlg.spacing = 28;

        // TopOrnament
        var ornament = MakeGO("TopOrnament", safeArea.transform);
        SetLE(ornament, minH: 80, prefH: 80);
        ornament.AddComponent<Image>().color = C_PRIMARY;

        // LogoBlock
        var logo = MakeGO("LogoBlock", safeArea.transform);
        SetLE(logo, minH: 220, prefH: 300, flexH: 0.5f);
        var logoVLG = logo.AddComponent<VerticalLayoutGroup>();
        logoVLG.childAlignment = TextAnchor.MiddleCenter;
        logoVLG.childForceExpandWidth = true;
        logoVLG.childControlWidth = logoVLG.childControlHeight = true;
        logoVLG.spacing = 8;

        MakeTMP("BadgeText",  logo.transform, "Усть-Алданский улус",  28, C_TEXT2,  font, minH: 40);
        MakeTMP("TitleMain",  logo.transform, "Викторина",            72, C_TEXT,   font, minH: 100, bold: true);
        MakeTMP("TitleYear",  logo.transform, "100 лет",              64, C_SECONDARY, font, minH: 80, bold: true);
        MakeTMP("SubTitle",   logo.transform, "1925 — 2025",          28, C_TEXT2,  font, minH: 40);

        // CategoryGrid — кнопки создаются динамически в MainMenuUI.Start()
        var gridGO = MakeGO("CategoryGrid", safeArea.transform);
        SetLE(gridGO, minH: 400, prefH: 420);
        var grid = gridGO.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(460, 180);
        grid.spacing         = new Vector2(20, 16);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment  = TextAnchor.UpperCenter;

        // CategoryButton префаб
        var catBtnPrefab = CreateCategoryButtonPrefab(font);

        // ButtonsBlock
        var btnsBlock = MakeGO("ButtonsBlock", safeArea.transform);
        SetLE(btnsBlock, minH: 280, prefH: 360, flexH: 1f);
        var btnsVLG = btnsBlock.AddComponent<VerticalLayoutGroup>();
        btnsVLG.childAlignment = TextAnchor.MiddleCenter;
        btnsVLG.childForceExpandWidth = true;
        btnsVLG.childControlWidth = btnsVLG.childControlHeight = true;
        btnsVLG.spacing = 20;

        var btnPlayGO     = MakePrimaryButton("BtnPlay",      btnsBlock.transform, "Начать игру",   font);
        var btnSettingsGO = MakeSecondaryButton("BtnSettings", btnsBlock.transform, "Настройки",     font);
        var btnRecordsGO  = MakeSecondaryButton("BtnRecords",  btnsBlock.transform, "Рекорды",       font);
        var btnAboutGO    = MakeSecondaryButton("BtnAbout",    btnsBlock.transform, "О приложении",  font);

        // StatsBar
        var statsGO  = MakeGO("StatsBar", safeArea.transform);
        SetLE(statsGO, minH: 60, prefH: 70);
        var statsTMP = MakeTMP("StatsText", statsGO.transform, "Сыграно игр: 0    Лучший результат: 0/15",
                               26, C_TEXT2, font);
        var statsRT = statsTMP.GetComponent<RectTransform>();
        statsRT.anchorMin = Vector2.zero; statsRT.anchorMax = Vector2.one;
        statsRT.offsetMin = statsRT.offsetMax = Vector2.zero;
        statsTMP.alignment = TextAlignmentOptions.Center;

        // --- Попап «нет вопросов» (поверх всего на Canvas) ---
        var popup = MakeGO("NoQuestionsPopup", canvasGO.transform);
        Stretch(popup);
        popup.AddComponent<Image>().color = C_OVERLAY;
        popup.SetActive(false);

        var card = MakeGO("PopupCard", popup.transform);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot     = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(900, 640);
        card.AddComponent<Image>().color = C_CARD;

        var cardVLG = card.AddComponent<VerticalLayoutGroup>();
        cardVLG.childAlignment        = TextAnchor.MiddleCenter;
        cardVLG.childForceExpandWidth = true;
        cardVLG.childControlWidth     = cardVLG.childControlHeight = true;
        cardVLG.padding = new RectOffset(60, 60, 60, 60);
        cardVLG.spacing = 32;

        var popupIcon = MakeTMP("PopupIcon", card.transform, "!", 96, C_SECONDARY, font, minH: 110, bold: true);
        popupIcon.alignment = TextAlignmentOptions.Center;

        var popupTitle = MakeTMP("PopupTitle", card.transform, "Нет вопросов", 44, C_TEXT, font, minH: 60, bold: true);
        popupTitle.alignment = TextAlignmentOptions.Center;

        var popupMsg = MakeTMP("PopupMessage", card.transform,
                               "В этой категории пока нет вопросов", 34, C_TEXT2, font, minH: 80);
        popupMsg.alignment         = TextAlignmentOptions.Center;
        popupMsg.enableWordWrapping = true;

        var btnCloseGO = MakePrimaryButton("BtnClosePopup", card.transform, "Понятно", font, minH: 110);

        // --- Панель настроек ---
        var settingsPanel = MakeGO("SettingsPanel", canvasGO.transform);
        Stretch(settingsPanel);
        settingsPanel.AddComponent<Image>().color = C_OVERLAY;
        settingsPanel.SetActive(false);

        var sheet = MakeGO("SettingsSheet", settingsPanel.transform);
        var sheetRT = sheet.GetComponent<RectTransform>();
        sheetRT.anchorMin = new Vector2(0.5f, 0.5f);
        sheetRT.anchorMax = new Vector2(0.5f, 0.5f);
        sheetRT.pivot     = new Vector2(0.5f, 0.5f);
        sheetRT.sizeDelta = new Vector2(960, 0);
        sheet.AddComponent<Image>().color = C_CARD;
        sheet.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var sheetVLG = sheet.AddComponent<VerticalLayoutGroup>();
        sheetVLG.childAlignment        = TextAnchor.UpperCenter;
        sheetVLG.childForceExpandWidth = true;
        sheetVLG.childControlWidth     = sheetVLG.childControlHeight = true;

        // Header панели
        var sHeader = MakeGO("SettingsHeader", sheet.transform);
        SetLE(sHeader, minH: 100, prefH: 100);
        sHeader.AddComponent<Image>().color = C_PRIMARY;
        var sHLG = sHeader.AddComponent<HorizontalLayoutGroup>();
        sHLG.childAlignment        = TextAnchor.MiddleLeft;
        sHLG.childForceExpandHeight = true;
        sHLG.childControlWidth      = sHLG.childControlHeight = true;
        sHLG.padding  = new RectOffset(40, 16, 0, 0);
        sHLG.spacing  = 0;

        var sTitleTMP = MakeTMP("SettingsTitle", sHeader.transform, "Настройки", 40, Color.white, font, bold: true);
        SetLE(sTitleTMP.gameObject, flexW: 1f);

        var sBtnCloseGO = MakeGO("BtnSettingsClose", sHeader.transform);
        SetLE(sBtnCloseGO, minW: 100, minH: 100);
        var sBtnCloseImg = sBtnCloseGO.AddComponent<Image>();
        sBtnCloseImg.color = Color.clear;
        var sBtnClose = sBtnCloseGO.AddComponent<Button>();
        sBtnClose.targetGraphic = sBtnCloseImg;
        var sCloseTMP = MakeTMP("CloseLabel", sBtnCloseGO.transform, "✕", 40, Color.white, font);
        var sCloseRT  = sCloseTMP.GetComponent<RectTransform>();
        sCloseRT.anchorMin = Vector2.zero; sCloseRT.anchorMax = Vector2.one;
        sCloseRT.offsetMin = sCloseRT.offsetMax = Vector2.zero;
        sCloseTMP.alignment = TextAlignmentOptions.Center;

        // Строки настроек
        var rowsGO = MakeGO("SettingsRows", sheet.transform);
        var rowsVLG = rowsGO.AddComponent<VerticalLayoutGroup>();
        rowsVLG.childAlignment        = TextAnchor.UpperCenter;
        rowsVLG.childForceExpandWidth = true;
        rowsVLG.childControlWidth     = rowsVLG.childControlHeight = true;
        rowsVLG.padding = new RectOffset(0, 0, 16, 32);

        var toggleMusic  = MakeSettingRow("Музыка",       rowsGO.transform, font);
        var sliderMusic  = MakeVolumeSliderRow("MusicVol",  rowsGO.transform, font);
        MakeRowSeparator(rowsGO.transform);
        var toggleSound  = MakeSettingRow("Звуки",        rowsGO.transform, font);
        var sliderSound  = MakeVolumeSliderRow("SoundVol",  rowsGO.transform, font);
        MakeRowSeparator(rowsGO.transform);
        var toggleVibro  = MakeSettingRow("Виброотклик",  rowsGO.transform, font);

        // SettingsUI компонент
        var settingsMgrGO = new GameObject("SettingsManager");
        var settingsUI    = settingsMgrGO.AddComponent<SettingsUI>();
        var soSet         = new SerializedObject(settingsUI);
        Prop(soSet, "settingsPanel",    settingsPanel);
        Prop(soSet, "btnClose",         sBtnClose);
        Prop(soSet, "toggleMusic",      toggleMusic);
        Prop(soSet, "toggleSound",      toggleSound);
        Prop(soSet, "toggleVibration",  toggleVibro);
        Prop(soSet, "sliderMusic",      sliderMusic);
        Prop(soSet, "sliderSound",      sliderSound);
        soSet.ApplyModifiedProperties();

        // AudioManager
        var audioGO  = new GameObject("AudioManager");
        var audioMgr = audioGO.AddComponent<AudioManager>();
        var audioSrc1 = audioGO.AddComponent<AudioSource>(); // музыка
        audioSrc1.loop        = true;
        audioSrc1.playOnAwake = false;
        audioSrc1.volume      = 0.6f;
        var audioSrc2 = audioGO.AddComponent<AudioSource>(); // SFX
        audioSrc2.playOnAwake = false;
        audioSrc2.volume      = 1f;

        var soAudio = new SerializedObject(audioMgr);
        Prop(soAudio, "musicSource", audioSrc1);
        Prop(soAudio, "sfxSource",   audioSrc2);
        var musicClip   = FindAudioClip("Assets/Audio/Music",  null);
        var clickClip   = FindAudioClip("Assets/Audio/SFX",    "click");
        var correctClip = FindAudioClip("Assets/Audio/SFX",    "correct");
        var wrongClip   = FindAudioClip("Assets/Audio/SFX",    "wrong");
        if (musicClip   != null) Prop(soAudio, "backgroundMusic",   musicClip);
        if (clickClip   != null) Prop(soAudio, "buttonClickClip",   clickClip);
        if (correctClip != null) Prop(soAudio, "correctAnswerClip", correctClip);
        if (wrongClip   != null) Prop(soAudio, "wrongAnswerClip",   wrongClip);
        soAudio.ApplyModifiedProperties();

        // GameManager
        var gmGO = MakeRootGO("GameManager");
        var gm   = gmGO.AddComponent<GameManager>();

        // Обновляем список категорий в базе — добавляем все найденные QuestionCategory
        var db = FindAsset<QuestionDatabase>("t:QuestionDatabase");
        if (db != null)
        {
            var catGuids = AssetDatabase.FindAssets("t:QuestionCategory");
            var allCats  = new List<QuestionCategory>();
            foreach (var g in catGuids)
            {
                var cat = AssetDatabase.LoadAssetAtPath<QuestionCategory>(
                              AssetDatabase.GUIDToAssetPath(g));
                if (cat != null) allCats.Add(cat);
            }
            allCats.Sort((a, b) => string.Compare(a.categoryId, b.categoryId,
                                                   System.StringComparison.Ordinal));
            db.categories = allCats;

            // Мастер-база содержит ВСЕ вопросы всех категорий
            var qGuids = AssetDatabase.FindAssets("t:QuestionData");
            var allQ   = new List<QuestionData>();
            foreach (var qg in qGuids)
            {
                var q = AssetDatabase.LoadAssetAtPath<QuestionData>(
                            AssetDatabase.GUIDToAssetPath(qg));
                if (q != null) allQ.Add(q);
            }
            db.allQuestions = allQ;

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }

        // MainMenuUI
        var managerGO = MakeRootGO("MenuManager");
        var menuUI = managerGO.AddComponent<MainMenuUI>();
        var soUI   = new SerializedObject(menuUI);

        Prop(soUI, "questionDatabase",     db);
        Prop(soUI, "categoryGrid",         gridGO.transform);
        Prop(soUI, "categoryButtonPrefab", catBtnPrefab?.GetComponent<CategoryButtonUI>());
        Prop(soUI, "btnPlay",              btnPlayGO.GetComponent<Button>());
        Prop(soUI, "btnSettings",          btnSettingsGO.GetComponent<Button>());
        Prop(soUI, "btnRecords",           btnRecordsGO.GetComponent<Button>());
        Prop(soUI, "btnAbout",             btnAboutGO.GetComponent<Button>());
        Prop(soUI, "statsText",            statsTMP);
        Prop(soUI, "noQuestionsPopup",     popup);
        Prop(soUI, "noQuestionsPopupText", popupMsg);
        Prop(soUI, "btnClosePopup",        btnCloseGO.GetComponent<Button>());
        Prop(soUI, "settingsUI",           settingsUI);
        soUI.ApplyModifiedProperties();

        SaveScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("[GameSceneBuilder] ✓ MainMenu сцена построена.");
    }

    // =====================================================================
    // 3. СЦЕНА КАРТЫ ВОПРОСОВ
    // =====================================================================

    static void DoBuildQuestionMap()
    {
        OpenOrCreateScene("Assets/Scenes/QuestionMap.unity");
        var font = FindFont();

        var canvasGO = SetupCanvas("QuestionMap");
        SetupCamera();
        SetupEventSystem();

        var bg = MakeGO("Background", canvasGO.transform);
        Stretch(bg); bg.AddComponent<Image>().color = C_BG;

        var safeArea = MakeGO("SafeArea", canvasGO.transform);
        Stretch(safeArea); safeArea.AddComponent<SafeArea>();

        var saVLG = safeArea.AddComponent<VerticalLayoutGroup>();
        saVLG.childAlignment = TextAnchor.UpperCenter;
        saVLG.childForceExpandWidth = true;
        saVLG.childForceExpandHeight = false;
        saVLG.childControlWidth = saVLG.childControlHeight = true;
        saVLG.padding  = new RectOffset(0, 0, 0, 0);
        saVLG.spacing  = 0;

        // --- Header ---
        var header = MakeGO("Header", safeArea.transform);
        SetLE(header, minH: 100, prefH: 100);
        header.AddComponent<Image>().color = C_PRIMARY;
        var hHLG = header.AddComponent<HorizontalLayoutGroup>();
        hHLG.childAlignment = TextAnchor.MiddleCenter;
        hHLG.childForceExpandWidth = false; hHLG.childForceExpandHeight = true;
        hHLG.childControlWidth = hHLG.childControlHeight = true;
        hHLG.padding = new RectOffset(24, 24, 0, 0); hHLG.spacing = 16;

        var btnBackGO = MakeSecondaryButton("BtnBack", header.transform, "← Назад", font, minH: 70, minW: 160);
        btnBackGO.GetComponent<Image>().color = new Color(1,1,1,0.2f);
        SetLE(btnBackGO, minH: 70, minW: 160);

        var catNameTMP = MakeTMP("CategoryName", header.transform, "История", 36, Color.white, font);
        SetLE(catNameTMP.gameObject, flexW: 1);
        catNameTMP.alignment = TextAlignmentOptions.Center;

        var scoreTMP = MakeTMP("ScoreText", header.transform, "Правильных: 0/15", 28, Color.white, font);
        SetLE(scoreTMP.gameObject, minW: 200);
        scoreTMP.alignment = TextAlignmentOptions.Right;

        // --- MapScrollView ---
        var scrollGO = MakeGO("MapScrollView", safeArea.transform);
        SetLE(scrollGO, flexH: 1f, minH: 400);
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true;

        var viewport = MakeGO("Viewport", scrollGO.transform);
        Stretch(viewport);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = MakeGO("MapContent", viewport.transform);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
        content.AddComponent<Image>().color = Color.clear;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var gridLG = content.AddComponent<GridLayoutGroup>();
        gridLG.cellSize        = new Vector2(300, 200);
        gridLG.spacing         = new Vector2(20, 20);
        gridLG.padding         = new RectOffset(30, 30, 30, 30);
        gridLG.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLG.constraintCount = 3;
        gridLG.childAlignment  = TextAnchor.UpperCenter;
        scroll.content = contentRT;

        // --- QuestionPanel (оверлей поверх Canvas, не в VLG) ---
        var qPanel = MakeGO("QuestionPanel", canvasGO.transform);
        Stretch(qPanel);
        qPanel.AddComponent<Image>().color = C_OVERLAY;
        qPanel.SetActive(false);

        var qCard = MakeGO("QuestionCard", qPanel.transform);
        var qCardRT = qCard.GetComponent<RectTransform>();
        qCardRT.anchorMin = new Vector2(0.03f, 0.05f);
        qCardRT.anchorMax = new Vector2(0.97f, 0.95f);
        qCardRT.offsetMin = qCardRT.offsetMax = Vector2.zero;
        var qCardImg = qCard.AddComponent<Image>(); qCardImg.color = C_CARD;
        var qCardCG  = qCard.AddComponent<CanvasGroup>();

        var qCardVLG = qCard.AddComponent<VerticalLayoutGroup>();
        qCardVLG.childAlignment = TextAnchor.UpperCenter;
        qCardVLG.childForceExpandWidth = true;
        qCardVLG.childForceExpandHeight = false;
        qCardVLG.childControlWidth = qCardVLG.childControlHeight = true;
        qCardVLG.padding = new RectOffset(40, 40, 40, 40);
        qCardVLG.spacing = 24;

        // Текст вопроса
        var qTextTMP = MakeTMP("QuestionText", qCard.transform, "Текст вопроса...", 34, C_TEXT, font);
        qTextTMP.enableWordWrapping = true;
        SetLE(qTextTMP.gameObject, minH: 120, prefH: 200, flexH: 1f);

        // Контейнер изображения
        var qImgContainer = MakeGO("QuestionImageContainer", qCard.transform);
        SetLE(qImgContainer, minH: 200, prefH: 300);
        qImgContainer.SetActive(false);
        var qImgGO = MakeGO("QuestionImage", qImgContainer.transform);
        var qImgRT = qImgGO.GetComponent<RectTransform>();
        qImgRT.anchorMin = Vector2.zero; qImgRT.anchorMax = Vector2.one;
        qImgRT.offsetMin = qImgRT.offsetMax = Vector2.zero;
        var qImg = qImgGO.AddComponent<Image>();
        qImg.preserveAspect = true;

        // Кнопки ответов
        var answersGrid = MakeGO("AnswersGrid", qCard.transform);
        SetLE(answersGrid, minH: 340, prefH: 380);
        var aVLG = answersGrid.AddComponent<VerticalLayoutGroup>();
        aVLG.childAlignment = TextAnchor.UpperCenter;
        aVLG.childForceExpandWidth = true;
        aVLG.childControlWidth = aVLG.childControlHeight = true;
        aVLG.spacing = 16;

        string[] abcd = { "A", "B", "C", "D" };
        var answerBtns   = new Button[4];
        var answerLabels = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            var (aBtnGO, aLbl) = MakeAnswerButton($"AnswerBtn_{abcd[i]}", answersGrid.transform,
                                                  $"{abcd[i]}: Вариант ответа", font);
            SetLE(aBtnGO, minH: 72, prefH: 76);
            answerBtns[i]   = aBtnGO.GetComponent<Button>();
            answerLabels[i] = aLbl;
        }

        // ResultFeedback
        var feedbackTMP = MakeTMP("ResultFeedback", qCard.transform, "Правильно! ✓", 36, C_CORRECT, font);
        SetLE(feedbackTMP.gameObject, minH: 60);
        feedbackTMP.alignment = TextAlignmentOptions.Center;
        feedbackTMP.gameObject.SetActive(false);

        // BtnContinue (внизу оверлея, вне карточки)
        var btnContinueGO = MakePrimaryButton("BtnContinue", qPanel.transform, "Продолжить →", font);
        var bcRT = btnContinueGO.GetComponent<RectTransform>();
        bcRT.anchorMin = new Vector2(0.5f, 0); bcRT.anchorMax = new Vector2(0.5f, 0);
        bcRT.pivot     = new Vector2(0.5f, 0);
        bcRT.anchoredPosition = new Vector2(0, 40);
        bcRT.sizeDelta = new Vector2(400, 100);
        btnContinueGO.gameObject.SetActive(false);

        // BtnFinish
        var btnFinishGO = MakePrimaryButton("BtnFinish", safeArea.transform, "Завершить →", font);
        SetLE(btnFinishGO, minH: 100, prefH: 100);
        btnFinishGO.GetComponent<Image>().color = C_SECONDARY;
        btnFinishGO.gameObject.SetActive(false);

        // --- QuestionTile Prefab ---
        var tilePrefab = CreateTilePrefab(font);

        // --- QuestionMapUI ---
        var mapManagerGO = MakeRootGO("QuestionMapManager");
        var mapUI        = mapManagerGO.AddComponent<QuestionMapUI>();
        var soMap        = new SerializedObject(mapUI);

        Prop(soMap, "tilePrefab",             tilePrefab?.GetComponent<QuestionTileUI>());
        Prop(soMap, "mapContent",             content.transform);
        Prop(soMap, "categoryNameText",       catNameTMP);
        Prop(soMap, "scoreText",              scoreTMP);
        Prop(soMap, "btnBack",                btnBackGO.GetComponent<Button>());
        Prop(soMap, "questionPanel",          qPanel);
        Prop(soMap, "questionCard",           qCardRT);
        Prop(soMap, "questionCardGroup",      qCardCG);
        Prop(soMap, "questionText",           qTextTMP);
        Prop(soMap, "questionImage",          qImg);
        Prop(soMap, "questionImageContainer", qImgContainer);
        Prop(soMap, "resultFeedback",         feedbackTMP);
        Prop(soMap, "btnContinue",            btnContinueGO.GetComponent<Button>());
        Prop(soMap, "btnFinish",              btnFinishGO.GetComponent<Button>());
        SetArr(soMap, "answerButtons", answerBtns);
        SetArr(soMap, "answerLabels",  answerLabels);
        soMap.ApplyModifiedProperties();

        SaveScene("Assets/Scenes/QuestionMap.unity");
        Debug.Log("[GameSceneBuilder] ✓ QuestionMap сцена построена.");
    }

    // =====================================================================
    // 4. СЦЕНА РЕЗУЛЬТАТОВ
    // =====================================================================

    static void DoBuildResults()
    {
        OpenOrCreateScene("Assets/Scenes/Results.unity");
        var font = FindFont();

        var canvasGO = SetupCanvas("Results");
        SetupCamera();
        SetupEventSystem();

        var bg = MakeGO("Background", canvasGO.transform);
        Stretch(bg); bg.AddComponent<Image>().color = C_BG;

        var safeArea = MakeGO("SafeArea", canvasGO.transform);
        Stretch(safeArea); safeArea.AddComponent<SafeArea>();

        var vlg = safeArea.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.padding = new RectOffset(60, 60, 80, 60);
        vlg.spacing = 32;

        // Заголовок
        var titleTMP = MakeTMP("ResultTitle", safeArea.transform, "Отлично!", 64, C_PRIMARY, font, minH: 90, bold: true);
        titleTMP.alignment = TextAlignmentOptions.Center;

        // ScoreCircle
        var circleGO = MakeGO("ScoreCircle", safeArea.transform);
        SetLE(circleGO, minH: 220, prefH: 220);
        var circleImg = circleGO.AddComponent<Image>();
        circleImg.color = C_PRIMARY;
        var circleRT = circleGO.GetComponent<RectTransform>();
        circleRT.anchorMin = circleRT.anchorMax = new Vector2(0.5f, 0);
        var circleTMP = MakeTMP("ScoreCircleText", circleGO.transform, "0/15", 60, Color.white, font, bold: true);
        var cRT = circleTMP.GetComponent<RectTransform>();
        cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        circleTMP.alignment = TextAlignmentOptions.Center;

        // StarsRow
        var starsRow = MakeGO("StarsRow", safeArea.transform);
        SetLE(starsRow, minH: 100, prefH: 100);
        var starsHLG = starsRow.AddComponent<HorizontalLayoutGroup>();
        starsHLG.childAlignment = TextAnchor.MiddleCenter;
        starsHLG.spacing = 24;
        starsHLG.childForceExpandWidth = false;
        starsHLG.childControlWidth = starsHLG.childControlHeight = true;

        var starImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var starGO  = MakeGO($"Star_{i}", starsRow.transform);
            SetLE(starGO, minH: 80, minW: 80);
            starImages[i] = starGO.AddComponent<Image>();
            starImages[i].color = new Color(0.78f, 0.66f, 0.29f);
        }

        // ScoreText
        var scoreTMP = MakeTMP("ScoreText", safeArea.transform,
            "Вы ответили правильно на 0 из 15 вопросов", 32, C_TEXT, font, minH: 60);
        scoreTMP.alignment = TextAlignmentOptions.Center;
        scoreTMP.enableWordWrapping = true;

        // BestScore
        var bestTMP = MakeTMP("BestScoreText", safeArea.transform,
            "Лучший результат: 0/15", 30, C_TEXT2, font, minH: 50);
        bestTMP.alignment = TextAlignmentOptions.Center;

        // NewBestBadge
        var badgeGO = MakeGO("NewBestBadge", safeArea.transform);
        SetLE(badgeGO, minH: 80, prefH: 80);
        badgeGO.AddComponent<Image>().color = C_SECONDARY;
        var badgeTMP = MakeTMP("BadgeText", badgeGO.transform, "Новый рекорд! 🏆", 34, Color.white, font);
        var badgeTMPRT = badgeTMP.GetComponent<RectTransform>();
        badgeTMPRT.anchorMin = Vector2.zero; badgeTMPRT.anchorMax = Vector2.one;
        badgeTMPRT.offsetMin = badgeTMPRT.offsetMax = Vector2.zero;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeGO.SetActive(false);

        // Кнопки
        var btnPlayAgain = MakePrimaryButton("BtnPlayAgain", safeArea.transform, "Играть снова",   font);
        var btnMainMenu  = MakeSecondaryButton("BtnMainMenu", safeArea.transform, "Главное меню",  font);
        var btnShare     = MakeSecondaryButton("BtnShare",    safeArea.transform, "Поделиться",    font);
        SetLE(btnPlayAgain, minH: 110, prefH: 110);
        SetLE(btnMainMenu,  minH: 100, prefH: 100);
        SetLE(btnShare,     minH: 90,  prefH: 90);

        // ResultsUI
        var resManagerGO = MakeRootGO("ResultsManager");
        var resUI        = resManagerGO.AddComponent<ResultsUI>();
        var soRes        = new SerializedObject(resUI);

        Prop(soRes, "resultTitle",     titleTMP);
        Prop(soRes, "scoreCircleText", circleTMP);
        Prop(soRes, "scoreText",       scoreTMP);
        Prop(soRes, "bestScoreText",   bestTMP);
        Prop(soRes, "newBestBadge",    badgeGO);
        Prop(soRes, "btnPlayAgain",    btnPlayAgain.GetComponent<Button>());
        Prop(soRes, "btnMainMenu",     btnMainMenu.GetComponent<Button>());
        Prop(soRes, "btnShare",        btnShare.GetComponent<Button>());
        SetArr(soRes, "stars", starImages);
        soRes.ApplyModifiedProperties();

        SaveScene("Assets/Scenes/Results.unity");
        Debug.Log("[GameSceneBuilder] ✓ Results сцена построена.");
    }

    // =====================================================================
    // 5. BUILD SETTINGS
    // =====================================================================

    static void DoAddScenes()
    {
        // Порядок важен: Intro = 0, MainMenu = 1, QuestionMap = 2, Results = 3
        string[] ordered = {
            "Assets/Scenes/Intro.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/QuestionMap.unity",
            "Assets/Scenes/Results.unity",
        };

        var entries = new List<EditorBuildSettingsScene>();
        foreach (var path in ordered)
        {
            if (!File.Exists(path)) { Debug.LogWarning($"[GameSceneBuilder] Файл не найден: {path}"); continue; }
            entries.Add(new EditorBuildSettingsScene(path, true));
        }

        // Сохраняем остальные сцены (не из нашего списка) в конце
        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (!ordered.Contains(existing.path))
                entries.Add(existing);
        }

        EditorBuildSettings.scenes = entries.ToArray();
        Debug.Log("[GameSceneBuilder] ✓ Сцены добавлены в Build Settings (Intro=0, MainMenu=1, QuestionMap=2, Results=3).");
    }

    // =====================================================================
    // ПРЕФАБ КНОПКИ КАТЕГОРИИ
    // =====================================================================

    static GameObject CreateCategoryButtonPrefab(TMP_FontAsset font)
    {
        const string prefabPath = "Assets/Prefabs/CategoryButton.prefab";
        Directory.CreateDirectory("Assets/Prefabs");
        if (File.Exists(prefabPath)) AssetDatabase.DeleteAsset(prefabPath);

        var root    = new GameObject("CategoryButton", typeof(RectTransform));
        var rootImg = root.AddComponent<Image>();
        rootImg.color  = C_PRIMARY;
        rootImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        rootImg.type   = Image.Type.Sliced;

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = rootImg;
        var cb = btn.colors;
        cb.highlightedColor = new Color(0.22f, 0.50f, 0.32f);
        cb.pressedColor     = new Color(0.18f, 0.42f, 0.27f);
        btn.colors = cb;

        // Highlight (выделение выбранной категории)
        var hlGO = new GameObject("Highlight", typeof(RectTransform));
        hlGO.transform.SetParent(root.transform, false);
        var hlRT = hlGO.GetComponent<RectTransform>();
        hlRT.anchorMin = Vector2.zero; hlRT.anchorMax = Vector2.one;
        hlRT.offsetMin = hlRT.offsetMax = Vector2.zero;
        hlGO.AddComponent<Image>().color = new Color(1f, 0.84f, 0f, 0.35f);
        hlGO.SetActive(false);

        // Label
        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(root.transform, false);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0f, 0f);
        lblRT.anchorMax = new Vector2(1f, 0.55f);
        lblRT.offsetMin = new Vector2(12f, 8f);
        lblRT.offsetMax = new Vector2(-12f, 0f);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text      = "Категория";
        lbl.fontSize  = 34;
        lbl.color     = Color.white;
        lbl.alignment = TextAlignmentOptions.Bottom;
        lbl.fontStyle = FontStyles.Bold;
        lbl.enableWordWrapping = true;
        if (font != null) lbl.font = font;

        // CategoryButtonUI
        var catUI = root.AddComponent<CategoryButtonUI>();
        var soCat = new SerializedObject(catUI);
        var pBtn  = soCat.FindProperty("button");    if (pBtn  != null) pBtn.objectReferenceValue  = btn;
        var pBg   = soCat.FindProperty("background"); if (pBg   != null) pBg.objectReferenceValue   = rootImg;
        var pLbl  = soCat.FindProperty("label");     if (pLbl  != null) pLbl.objectReferenceValue  = lbl;
        var pHl   = soCat.FindProperty("highlight"); if (pHl   != null) pHl.objectReferenceValue   = hlGO;
        soCat.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log($"[GameSceneBuilder] CategoryButton prefab сохранён: {prefabPath}");
        return prefab;
    }

    // =====================================================================
    // ПРЕФАБ ПЛИТКИ
    // =====================================================================

    static GameObject CreateTilePrefab(TMP_FontAsset font)
    {
        const string prefabPath = "Assets/Prefabs/QuestionTile.prefab";
        Directory.CreateDirectory("Assets/Prefabs");

        // Удалить старый prefab если есть
        if (File.Exists(prefabPath)) AssetDatabase.DeleteAsset(prefabPath);

        var root = new GameObject("QuestionTile", typeof(RectTransform));

        var rootImg = root.AddComponent<Image>();
        rootImg.color  = C_TILE_DEF;
        rootImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        rootImg.type   = Image.Type.Sliced;

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = rootImg;
        var cb = btn.colors;
        cb.highlightedColor = new Color(0.85f, 0.82f, 0.76f);
        cb.pressedColor     = new Color(0.75f, 0.72f, 0.66f);
        btn.colors = cb;

        // Number
        var numGO  = new GameObject("TileNumber", typeof(RectTransform));
        numGO.transform.SetParent(root.transform, false);
        var numRT  = numGO.GetComponent<RectTransform>();
        numRT.anchorMin = new Vector2(0, 0.5f); numRT.anchorMax = Vector2.one;
        numRT.offsetMin = new Vector2(12, 0); numRT.offsetMax = new Vector2(-12, -8);
        var numTMP = numGO.AddComponent<TextMeshProUGUI>();
        numTMP.text = "1"; numTMP.fontSize = 44; numTMP.color = C_TEXT;
        numTMP.alignment = TextAlignmentOptions.BottomRight;
        if (font != null) numTMP.font = font;

        // Checkmark (hidden)
        var chkGO  = new GameObject("TileCheckmark", typeof(RectTransform));
        chkGO.transform.SetParent(root.transform, false);
        var chkRT  = chkGO.GetComponent<RectTransform>();
        chkRT.anchorMin = new Vector2(0, 0.5f); chkRT.anchorMax = new Vector2(0.5f, 1f);
        chkRT.offsetMin = new Vector2(12, -8); chkRT.offsetMax = new Vector2(-4, -8);
        chkGO.AddComponent<Image>().color = C_CORRECT;
        chkGO.SetActive(false);

        // QuestionTileUI component
        var tileUI = root.AddComponent<QuestionTileUI>();
        var soTile = new SerializedObject(tileUI);
        Prop(soTile, "button",          btn);
        Prop(soTile, "tileBackground",  rootImg);
        Prop(soTile, "tileNumber",      numTMP);
        Prop(soTile, "tileCheckmark",   chkGO.GetComponent<Image>());
        soTile.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log($"[GameSceneBuilder] QuestionTile prefab сохранён: {prefabPath}");
        return prefab;
    }

    // =====================================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ — ОБЪЕКТЫ
    // =====================================================================

    static UnityEngine.SceneManagement.Scene OpenOrCreateScene(string path)
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(newScene, path);
        return EditorSceneManager.OpenScene(path);
    }

    static void SaveScene(string path)
    {
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
    }

    static GameObject SetupCanvas(string sceneName)
    {
        var go     = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 1f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static void SetupCamera()
    {
        var camGO  = new GameObject("Main Camera");
        camGO.tag  = "MainCamera";
        var cam    = camGO.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = Hex("F5F0E8");
        cam.cullingMask     = 0;
        cam.depth           = -1;
        camGO.AddComponent<AudioListener>();
    }

    static void SetupEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject MakeRootGO(string name)
    {
        return new GameObject(name);
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetLE(GameObject go, float minH = 0, float minW = 0, float prefH = 0, float flexH = -1f, float flexW = -1f)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        if (minH  > 0)  le.minHeight       = minH;
        if (minW  > 0)  le.minWidth        = minW;
        if (prefH > 0)  le.preferredHeight = prefH;
        if (flexH >= 0) le.flexibleHeight  = flexH;
        if (flexW >= 0) le.flexibleWidth   = flexW;
    }

    static TMP_Text MakeTMP(string name, Transform parent, string text, float size,
                            Color color, TMP_FontAsset font, float minH = 0, bool bold = false)
    {
        var go  = MakeGO(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.fontSize           = size;
        tmp.color              = color;
        tmp.enableWordWrapping = false;
        tmp.overflowMode       = TextOverflowModes.Overflow;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        if (font != null) tmp.font = font;
        if (minH > 0) SetLE(go, minH: minH);
        return tmp;
    }

    // Кнопка главного стиля (зелёный фон, белый текст)
    static GameObject MakePrimaryButton(string name, Transform parent, string text,
                                        TMP_FontAsset font, float minH = 110, float minW = 0)
    {
        var go  = MakeGO(name, parent);
        SetLE(go, minH: minH, minW: minW);
        var img   = go.AddComponent<Image>();
        img.color  = C_BTN_PRI;
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type   = Image.Type.Sliced;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors; cb.highlightedColor = new Color(0.22f, 0.50f, 0.32f); btn.colors = cb;
        go.AddComponent<ButtonSFX>();
        var lbl = MakeGO("Text", go.transform);
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(16, 0); lRT.offsetMax = new Vector2(-16, 0);
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 36; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        if (font != null) tmp.font = font;
        return go;
    }

    // Строка настройки: Label слева + Toggle-пилюля справа
    static Toggle MakeSettingRow(string label, Transform parent, TMP_FontAsset font)
    {
        var row = MakeGO(label.Replace(" ", "") + "Row", parent);
        SetLE(row, minH: 96, prefH: 96);
        row.AddComponent<Image>().color = Color.clear;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = hlg.childControlHeight = true;
        hlg.padding = new RectOffset(48, 48, 0, 0);
        hlg.spacing = 24;

        var nameTMP = MakeTMP("Label", row.transform, label, 34, C_TEXT, font);
        SetLE(nameTMP.gameObject, flexW: 1f);

        // Пилюля-переключатель
        var pill = MakeGO("Toggle", row.transform);
        SetLE(pill, minW: 160, minH: 60);
        var pillImg = pill.AddComponent<Image>();
        pillImg.color  = C_PRIMARY;
        pillImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        pillImg.type   = Image.Type.Sliced;

        var toggle = pill.AddComponent<Toggle>();
        toggle.targetGraphic = pillImg;
        toggle.graphic       = null;
        toggle.isOn          = true;

        var pillLbl = MakeTMP("Label", pill.transform, "Вкл", 26, Color.white, font, bold: true);
        var pillRT  = pillLbl.GetComponent<RectTransform>();
        pillRT.anchorMin = Vector2.zero; pillRT.anchorMax = Vector2.one;
        pillRT.offsetMin = pillRT.offsetMax = Vector2.zero;
        pillLbl.alignment = TextAlignmentOptions.Center;

        return toggle;
    }

    // Разделитель строк настроек
    static void MakeRowSeparator(Transform parent)
    {
        var sep = MakeGO("Separator", parent);
        SetLE(sep, minH: 1, prefH: 1);
        sep.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.08f);
    }

    // Строка с ползунком громкости (label слева + Slider справа)
    static Slider MakeVolumeSliderRow(string rowName, Transform parent, TMP_FontAsset font)
    {
        var row = MakeGO(rowName + "Row", parent);
        SetLE(row, minH: 72, prefH: 72);
        row.AddComponent<Image>().color = Color.clear;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth      = hlg.childControlHeight = true;
        hlg.padding = new RectOffset(48, 48, 0, 0);
        hlg.spacing = 20;

        var lbl = MakeTMP("Label", row.transform, "Громкость", 26, C_TEXT2, font);
        SetLE(lbl.gameObject, minW: 170);

        // Контейнер слайдера
        var sliderGO = MakeGO("Slider", row.transform);
        SetLE(sliderGO, minH: 44, prefH: 44, flexW: 1f);

        // Background
        var bgGO = MakeGO("Background", sliderGO.transform);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.25f);
        bgRT.anchorMax = new Vector2(1f, 0.75f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<Image>().color = new Color(0.80f, 0.80f, 0.80f);

        // Fill Area
        var fillAreaGO = MakeGO("Fill Area", sliderGO.transform);
        var faRT = fillAreaGO.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.25f);
        faRT.anchorMax = new Vector2(1f, 0.75f);
        faRT.offsetMin = new Vector2(5f, 0f);
        faRT.offsetMax = new Vector2(-15f, 0f);

        var fillGO = MakeGO("Fill", fillAreaGO.transform);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(1f, 1f);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = C_PRIMARY;

        // Handle Slide Area
        var handleAreaGO = MakeGO("Handle Slide Area", sliderGO.transform);
        var haRT = handleAreaGO.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero;
        haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10f, 0f);
        haRT.offsetMax = new Vector2(-10f, 0f);

        var handleGO = MakeGO("Handle", handleAreaGO.transform);
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = handleRT.anchorMax = new Vector2(0f, 0.5f);
        handleRT.pivot     = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(44f, 44f);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color  = C_PRIMARY;
        handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        handleImg.type   = Image.Type.Sliced;

        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect      = fillRT;
        slider.handleRect    = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue      = 0f;
        slider.maxValue      = 1f;
        slider.value         = 1f;

        return slider;
    }

    // Кнопка вторичного стиля (белый фон, зелёный текст)
    static GameObject MakeSecondaryButton(string name, Transform parent, string text,
                                          TMP_FontAsset font, float minH = 100, float minW = 0)
    {
        var go  = MakeGO(name, parent);
        SetLE(go, minH: minH, minW: minW);
        var img   = go.AddComponent<Image>();
        img.color  = C_BTN_SEC;
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type   = Image.Type.Sliced;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        go.AddComponent<ButtonSFX>();
        var lbl = MakeGO("Text", go.transform);
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(16, 0); lRT.offsetMax = new Vector2(-16, 0);
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 34; tmp.color = C_PRIMARY;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
        return go;
    }

    // Кнопка ответа (белый фон, тёмный текст)
    static (GameObject go, TMP_Text lbl) MakeAnswerButton(string name, Transform parent,
                                                           string text, TMP_FontAsset font)
    {
        var go  = MakeGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color  = Color.white;
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type   = Image.Type.Sliced;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        var cb = btn.colors;
        cb.highlightedColor = new Color(0.90f, 0.95f, 0.90f);
        cb.pressedColor     = new Color(0.80f, 0.90f, 0.80f);
        btn.colors = cb;

        var txtGO = MakeGO("Text", go.transform);
        var tRT   = txtGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(20, 0); tRT.offsetMax = new Vector2(-20, 0);
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 30; tmp.color = C_TEXT;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        if (font != null) tmp.font = font;
        return (go, tmp);
    }

    // =====================================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ — АССЕТЫ
    // =====================================================================

    static TMP_FontAsset FindFont()
    {
        // Сначала пробуем PTSans SDF (Assets/Fonts/)
        var guids = AssetDatabase.FindAssets("PTSans SDF t:TMP_FontAsset", new[] { "Assets/Fonts" });
        if (guids.Length > 0)
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (f != null && f.HasCharacter(' ')) return f;
        }
        // Fallback: LiberationSans SDF из TMP Resources
        const string libPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        var lib = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(libPath);
        return lib != null ? lib : TMP_Settings.defaultFontAsset;
    }

    static QuestionCategory FindCategory(string id)
    {
        var guids = AssetDatabase.FindAssets("t:QuestionCategory");
        foreach (var g in guids)
        {
            var cat = AssetDatabase.LoadAssetAtPath<QuestionCategory>(AssetDatabase.GUIDToAssetPath(g));
            if (cat != null && cat.categoryId == id) return cat;
        }
        return null;
    }

    static AudioClip FindAudioClip(string folder, string nameContains)
    {
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (nameContains == null || file.Contains(nameContains.ToLowerInvariant()))
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }

    static T FindAsset<T>(string filter) where T : Object
    {
        var guids = AssetDatabase.FindAssets(filter);
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    // SerializedObject helpers
    static void Prop(SerializedObject so, string name, Object value)
    {
        var p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"[GameSceneBuilder] Поле '{name}' не найдено"); return; }
        p.objectReferenceValue = value;
    }

    static void SetArr<T>(SerializedObject so, string name, T[] values) where T : Object
    {
        var p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"[GameSceneBuilder] Массив '{name}' не найден"); return; }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
