using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;
using UstAldanQuiz.UI;
using UstAldanQuiz.Utils;

public static partial class GameSceneBuilder
{
    // =====================================================================
    // 2. СЦЕНА ГЛАВНОГО МЕНЮ
    // =====================================================================

    static void DoBuildMainMenu(bool skipIfExists = false)
    {
        if (skipIfExists && File.Exists("Assets/Scenes/MainMenu.unity"))
        { Debug.Log("[GameSceneBuilder] MainMenu.unity уже существует — пропускаем."); return; }

        // Автоназначение иконок категориям по categoryId → Assets/Images/Icons/{id}.png
        foreach (var guid in AssetDatabase.FindAssets("t:QuestionCategory"))
        {
            var catPath = AssetDatabase.GUIDToAssetPath(guid);
            var cat     = AssetDatabase.LoadAssetAtPath<QuestionCategory>(catPath);
            if (cat == null) continue;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Images/Icons/{cat.categoryId}.png");
            if (sprite != null && cat.icon != sprite)
            {
                cat.icon = sprite;
                EditorUtility.SetDirty(cat);
            }
        }
        AssetDatabase.SaveAssets();

        var scene    = OpenOrCreateScene("Assets/Scenes/MainMenu.unity");
        var font     = FindFont();
        var canvasGO = SetupCanvas(scene.name);
        SetupCamera();
        SetupEventSystem();

        // ── Фон ──────────────────────────────────────────────────────────────
        var bg = MakeGO("Background", canvasGO.transform);
        Stretch(bg);
        bg.AddComponent<Image>().color = C_BG;

        // ── SafeArea (VLG: ContentArea + BottomNavBar) ────────────────────
        var safeArea = MakeGO("SafeArea", canvasGO.transform);
        Stretch(safeArea);
        safeArea.AddComponent<SafeArea>();
        var saVLG = safeArea.AddComponent<VerticalLayoutGroup>();
        saVLG.childAlignment         = TextAnchor.UpperCenter;
        saVLG.childForceExpandWidth  = true;
        saVLG.childForceExpandHeight = false;
        saVLG.childControlWidth = saVLG.childControlHeight = true;
        saVLG.padding = new RectOffset(0, 0, 0, 80); // нижний отступ = высота navBar
        saVLG.spacing = 0;

        // ── ContentArea (содержит все 4 страницы, показывается только одна)
        var contentArea = MakeGO("ContentArea", safeArea.transform);
        SetLE(contentArea, flexH: 1f, minH: 300);
        contentArea.AddComponent<RectMask2D>(); // клипинг для анимации слайда

        // ── Страница 0: Главная ───────────────────────────────────────────
        var catBtnPrefab = CreateCategoryButtonPrefab(font);

        var homePage = MakeGO("HomePage", contentArea.transform);
        Stretch(homePage);
        homePage.AddComponent<Image>().color = C_BG;
        var homeVLG = homePage.AddComponent<VerticalLayoutGroup>();
        homeVLG.childAlignment         = TextAnchor.UpperCenter;
        homeVLG.childForceExpandWidth  = true;
        homeVLG.childForceExpandHeight = false;
        homeVLG.childControlWidth = homeVLG.childControlHeight = true;
        homeVLG.padding = new RectOffset(40, 40, 40, 100);
        homeVLG.spacing = 16;

        // LogoBlock
        var logo = MakeGO("LogoBlock", homePage.transform);
        SetLE(logo, minH: 180, prefH: 200);
        var logoVLG = logo.AddComponent<VerticalLayoutGroup>();
        logoVLG.childAlignment = TextAnchor.MiddleCenter;
        logoVLG.childForceExpandWidth = true;
        logoVLG.childControlWidth = logoVLG.childControlHeight = true;
        logoVLG.spacing = 4;
        AddLocKey(MakeTMP("BadgeText", logo.transform, "Усть-Алданский улус", 24, C_TEXT2,    font, minH: 36).gameObject, "app_badge");
        AddLocKey(MakeTMP("TitleMain", logo.transform, "Викторина",           64, C_TEXT,     font, minH: 80, bold: true).gameObject, "app_title");
        AddLocKey(MakeTMP("TitleYear", logo.transform, "100 лет",             48, C_SECONDARY, font, minH: 60, bold: true).gameObject, "app_year");

        // CategoryGrid
        var gridGO = MakeGO("CategoryGrid", homePage.transform);
        SetLE(gridGO, minH: 160, prefH: 260, flexH: 1f);
        var grid = gridGO.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(440, 150);
        grid.spacing         = new Vector2(16, 12);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment  = TextAnchor.UpperCenter;

        // StatsBar
        var statsGO  = MakeGO("StatsBar", homePage.transform);
        SetLE(statsGO, minH: 44, prefH: 44);
        var statsTMP = MakeTMP("StatsText", statsGO.transform, "", 22, C_TEXT2, font);
        var statsRT  = statsTMP.GetComponent<RectTransform>();
        statsRT.anchorMin = Vector2.zero; statsRT.anchorMax = Vector2.one;
        statsRT.offsetMin = statsRT.offsetMax = Vector2.zero;
        statsTMP.alignment = TextAlignmentOptions.Center;

        // Обёртка центрирует BtnPlay и ограничивает его ширину
        var btnPlayWrapper = MakeGO("BtnPlayWrapper", homePage.transform);
        SetLE(btnPlayWrapper, minH: 166);
        var wrapHLG = btnPlayWrapper.AddComponent<HorizontalLayoutGroup>();
        wrapHLG.childAlignment         = TextAnchor.MiddleCenter;
        wrapHLG.childForceExpandWidth  = false;
        wrapHLG.childForceExpandHeight = false;
        wrapHLG.childControlWidth = wrapHLG.childControlHeight = true;

        var btnPlayGO = MakePrimaryButton("BtnPlay", btnPlayWrapper.transform, "Начать игру", font, minH: 166);
        SetLE(btnPlayGO, minW: 520, flexW: 0);
        AddLocKey(btnPlayGO, "btn_play");
        ApplyHyperCasualButton(btnPlayGO,
            "Assets/Images/Sprites/buttons.png",
            normalName: "buttons_12", pressedName: "buttons_13");

        // Кнопка «Аркада» — переход на Roadmap
        var btnArcadeWrapper = MakeGO("BtnArcadeWrapper", homePage.transform);
        SetLE(btnArcadeWrapper, minH: 166);
        var arcadeWrapHLG = btnArcadeWrapper.AddComponent<HorizontalLayoutGroup>();
        arcadeWrapHLG.childAlignment         = TextAnchor.MiddleCenter;
        arcadeWrapHLG.childForceExpandWidth  = false;
        arcadeWrapHLG.childForceExpandHeight = false;
        arcadeWrapHLG.childControlWidth = arcadeWrapHLG.childControlHeight = true;

        var btnArcadeGO = MakePrimaryButton("BtnArcade", btnArcadeWrapper.transform, "Аркада", font, minH: 166);
        SetLE(btnArcadeGO, minW: 520, flexW: 0);
        AddLocKey(btnArcadeGO, "btn_arcade");
        ApplyHyperCasualButton(btnArcadeGO,
            "Assets/Images/Sprites/buttons.png",
            normalName: "buttons_12", pressedName: "buttons_13");

        // ── Страница 1: Рекорды ───────────────────────────────────────────
        var recordsPage = MakeGO("RecordsPage", contentArea.transform);
        Stretch(recordsPage);
        recordsPage.AddComponent<Image>().color = C_BG;
        var recVLG = recordsPage.AddComponent<VerticalLayoutGroup>();
        recVLG.childAlignment = TextAnchor.UpperCenter;
        recVLG.childForceExpandWidth = true;
        recVLG.childForceExpandHeight = false;
        recVLG.childControlWidth = recVLG.childControlHeight = true;

        var recHeader = MakeGO("RecordsHeader", recordsPage.transform);
        SetLE(recHeader, minH: 100, prefH: 100);
        recHeader.AddComponent<Image>().color = C_PRIMARY;
        var recTitleTMP = MakeTMP("RecordsTitle", recHeader.transform, "Рекорды", 40, Color.white, font, bold: true);
        var recTitleRT  = recTitleTMP.GetComponent<RectTransform>();
        recTitleRT.anchorMin = Vector2.zero; recTitleRT.anchorMax = Vector2.one;
        recTitleRT.offsetMin = new Vector2(40, 0); recTitleRT.offsetMax = Vector2.zero;
        recTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        AddLocKey(recTitleTMP.gameObject, "btn_records");

        var recScrollGO = MakeGO("RecordsScroll", recordsPage.transform);
        SetLE(recScrollGO, flexH: 1f);
        var recScroll = recScrollGO.AddComponent<ScrollRect>();
        recScroll.horizontal = false; recScroll.vertical = true;
        var recViewport = MakeGO("Viewport", recScrollGO.transform);
        Stretch(recViewport);
        recViewport.AddComponent<RectMask2D>();
        recScroll.viewport = recViewport.GetComponent<RectTransform>();
        var recContent = MakeGO("RecordsContent", recViewport.transform);
        var recContentRT = recContent.GetComponent<RectTransform>();
        recContentRT.anchorMin = new Vector2(0, 1); recContentRT.anchorMax = new Vector2(1, 1);
        recContentRT.pivot     = new Vector2(0.5f, 1);
        recContentRT.offsetMin = recContentRT.offsetMax = Vector2.zero;
        recContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var recContentVLG = recContent.AddComponent<VerticalLayoutGroup>();
        recContentVLG.childAlignment = TextAnchor.UpperCenter;
        recContentVLG.childForceExpandWidth = true;
        recContentVLG.childControlWidth = recContentVLG.childControlHeight = true;
        recContentVLG.spacing = 1;
        recScroll.content = recContentRT;
        recordsPage.SetActive(false);

        // ── Страница 2: Настройки ─────────────────────────────────────────
        var settingsPage = MakeGO("SettingsPage", contentArea.transform);
        Stretch(settingsPage);
        settingsPage.AddComponent<Image>().color = C_BG;
        var setVLG = settingsPage.AddComponent<VerticalLayoutGroup>();
        setVLG.childAlignment = TextAnchor.UpperCenter;
        setVLG.childForceExpandWidth = true;
        setVLG.childForceExpandHeight = false;
        setVLG.childControlWidth = setVLG.childControlHeight = true;

        var setHeader = MakeGO("SettingsHeader", settingsPage.transform);
        SetLE(setHeader, minH: 100, prefH: 100);
        setHeader.AddComponent<Image>().color = C_PRIMARY;
        var setTitleTMP = MakeTMP("SettingsTitle", setHeader.transform, "Настройки", 40, Color.white, font, bold: true);
        var setTitleRT  = setTitleTMP.GetComponent<RectTransform>();
        setTitleRT.anchorMin = Vector2.zero; setTitleRT.anchorMax = Vector2.one;
        setTitleRT.offsetMin = new Vector2(40, 0); setTitleRT.offsetMax = Vector2.zero;
        setTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        AddLocKey(setTitleTMP.gameObject, "settings_title");

        var setRowsGO = MakeGO("SettingsRows", settingsPage.transform);
        SetLE(setRowsGO, flexH: 1f);
        var setRowsVLG = setRowsGO.AddComponent<VerticalLayoutGroup>();
        setRowsVLG.childAlignment = TextAnchor.UpperCenter;
        setRowsVLG.childForceExpandWidth = true;
        setRowsVLG.childControlWidth = setRowsVLG.childControlHeight = true;
        setRowsVLG.padding = new RectOffset(0, 0, 16, 32);

        var toggleMusic = MakeSettingRow("Музыка",      setRowsGO.transform, font, "settings_music");
        var sliderMusic = MakeVolumeSliderRow("MusicVol", setRowsGO.transform, font);
        MakeRowSeparator(setRowsGO.transform);
        var toggleSound = MakeSettingRow("Звуки",       setRowsGO.transform, font, "settings_sound");
        var sliderSound = MakeVolumeSliderRow("SoundVol", setRowsGO.transform, font);
        MakeRowSeparator(setRowsGO.transform);
        var toggleVibro = MakeSettingRow("Виброотклик", setRowsGO.transform, font, "settings_vibration");
        MakeRowSeparator(setRowsGO.transform);
        var (btnLangRu, btnLangSah) = MakeLangRow(setRowsGO.transform, font);

        var settingsPageUI = settingsPage.AddComponent<SettingsPageUI>();
        var soSetPage      = new UnityEditor.SerializedObject(settingsPageUI);
        Prop(soSetPage, "toggleMusic",     toggleMusic);
        Prop(soSetPage, "toggleSound",     toggleSound);
        Prop(soSetPage, "toggleVibration", toggleVibro);
        Prop(soSetPage, "sliderMusic",     sliderMusic);
        Prop(soSetPage, "sliderSound",     sliderSound);
        Prop(soSetPage, "btnLangRu",       btnLangRu);
        Prop(soSetPage, "btnLangSah",      btnLangSah);
        soSetPage.ApplyModifiedProperties();
        settingsPage.SetActive(false);

        // ── Страница 3: О приложении ──────────────────────────────────────
        var aboutPage = MakeGO("AboutPage", contentArea.transform);
        Stretch(aboutPage);
        aboutPage.AddComponent<Image>().color = C_BG;
        var aboutPageVLG = aboutPage.AddComponent<VerticalLayoutGroup>();
        aboutPageVLG.childAlignment = TextAnchor.UpperCenter;
        aboutPageVLG.childForceExpandWidth = true;
        aboutPageVLG.childForceExpandHeight = false;
        aboutPageVLG.childControlWidth = aboutPageVLG.childControlHeight = true;

        var aboutHeader = MakeGO("AboutHeader", aboutPage.transform);
        SetLE(aboutHeader, minH: 100, prefH: 100);
        aboutHeader.AddComponent<Image>().color = C_PRIMARY;
        var aboutTitleTMP = MakeTMP("AboutTitle", aboutHeader.transform, "О приложении", 40, Color.white, font, bold: true);
        var aboutTitleRT  = aboutTitleTMP.GetComponent<RectTransform>();
        aboutTitleRT.anchorMin = Vector2.zero; aboutTitleRT.anchorMax = Vector2.one;
        aboutTitleRT.offsetMin = new Vector2(40, 0); aboutTitleRT.offsetMax = Vector2.zero;
        aboutTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        AddLocKey(aboutTitleTMP.gameObject, "btn_about");

        var aboutScrollGO = MakeGO("AboutScroll", aboutPage.transform);
        SetLE(aboutScrollGO, flexH: 1f);
        var aboutScroll = aboutScrollGO.AddComponent<ScrollRect>();
        aboutScroll.horizontal = false; aboutScroll.vertical = true;
        var aboutViewport = MakeGO("Viewport", aboutScrollGO.transform);
        Stretch(aboutViewport);
        aboutViewport.AddComponent<RectMask2D>();
        aboutScroll.viewport = aboutViewport.GetComponent<RectTransform>();
        var aboutContent = MakeGO("AboutContent", aboutViewport.transform);
        var aboutContentRT = aboutContent.GetComponent<RectTransform>();
        aboutContentRT.anchorMin = new Vector2(0, 1); aboutContentRT.anchorMax = new Vector2(1, 1);
        aboutContentRT.pivot     = new Vector2(0.5f, 1);
        aboutContentRT.offsetMin = aboutContentRT.offsetMax = Vector2.zero;
        aboutContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var aboutContentVLG = aboutContent.AddComponent<VerticalLayoutGroup>();
        aboutContentVLG.childAlignment = TextAnchor.UpperLeft;
        aboutContentVLG.childForceExpandWidth = true;
        aboutContentVLG.childControlWidth = aboutContentVLG.childControlHeight = true;
        aboutContentVLG.padding = new RectOffset(48, 48, 32, 48);
        aboutContentVLG.spacing = 24;
        aboutScroll.content = aboutContentRT;

        var aboutBodyTMP = MakeTMP("AboutBodyText", aboutContent.transform, "", 32, C_TEXT, font);
        aboutBodyTMP.alignment         = TextAlignmentOptions.TopLeft;
        aboutBodyTMP.enableWordWrapping = true;

        var aboutSuggestBtnGO = MakePrimaryButton("BtnSuggest", aboutContent.transform, "Предложить вопрос", font, minH: 104);
        AddLocKey(aboutSuggestBtnGO, "btn_suggest");

        var suggestUIComp = BuildSuggestPanel(canvasGO.transform, font);

        var aboutPageUI = aboutPage.AddComponent<AboutPageUI>();
        var soAboutPage = new UnityEditor.SerializedObject(aboutPageUI);
        Prop(soAboutPage, "bodyText",  aboutBodyTMP);
        Prop(soAboutPage, "btnSuggest", aboutSuggestBtnGO.GetComponent<Button>());
        Prop(soAboutPage, "suggestUI",  suggestUIComp);
        soAboutPage.ApplyModifiedProperties();
        aboutPage.SetActive(false);

        // ── Страница 4: Профиль ───────────────────────────────────────────
        var profilePage = MakeGO("ProfilePage", contentArea.transform);
        Stretch(profilePage);
        profilePage.AddComponent<Image>().color = C_BG;
        var profVLG = profilePage.AddComponent<VerticalLayoutGroup>();
        profVLG.childAlignment = TextAnchor.UpperCenter;
        profVLG.childForceExpandWidth = true;
        profVLG.childForceExpandHeight = false;
        profVLG.childControlWidth = profVLG.childControlHeight = true;

        var profHeader = MakeGO("ProfileHeader", profilePage.transform);
        SetLE(profHeader, minH: 100, prefH: 100);
        profHeader.AddComponent<Image>().color = C_PRIMARY;
        var profTitleTMP = MakeTMP("ProfileTitle", profHeader.transform, "Профиль", 40, Color.white, font, bold: true);
        var profTitleRT  = profTitleTMP.GetComponent<RectTransform>();
        profTitleRT.anchorMin = Vector2.zero; profTitleRT.anchorMax = Vector2.one;
        profTitleRT.offsetMin = new Vector2(40, 0); profTitleRT.offsetMax = Vector2.zero;
        profTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        AddLocKey(profTitleTMP.gameObject, "btn_profile");

        var profScrollGO = MakeGO("ProfileScroll", profilePage.transform);
        SetLE(profScrollGO, flexH: 1f);
        var profScroll = profScrollGO.AddComponent<ScrollRect>();
        profScroll.horizontal = false; profScroll.vertical = true;
        var profViewport = MakeGO("Viewport", profScrollGO.transform);
        Stretch(profViewport);
        profViewport.AddComponent<RectMask2D>();
        profScroll.viewport = profViewport.GetComponent<RectTransform>();
        var profContent = MakeGO("ProfileContent", profViewport.transform);
        var profContentRT = profContent.GetComponent<RectTransform>();
        profContentRT.anchorMin = new Vector2(0, 1); profContentRT.anchorMax = new Vector2(1, 1);
        profContentRT.pivot     = new Vector2(0.5f, 1);
        profContentRT.offsetMin = profContentRT.offsetMax = Vector2.zero;
        profContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var profContentVLG = profContent.AddComponent<VerticalLayoutGroup>();
        profContentVLG.childAlignment = TextAnchor.UpperLeft;
        profContentVLG.childForceExpandWidth = true;
        profContentVLG.childControlWidth = profContentVLG.childControlHeight = true;
        profContentVLG.padding = new RectOffset(48, 48, 32, 48);
        profContentVLG.spacing = 24;
        profScroll.content = profContentRT;

        var profBodyTMP = MakeTMP("ProfileBodyText", profContent.transform, "", 32, C_TEXT, font);
        profBodyTMP.alignment         = TextAlignmentOptions.TopLeft;
        profBodyTMP.enableWordWrapping = true;

        var profPageUI = profilePage.AddComponent<ProfilePageUI>();
        var soProfPage = new UnityEditor.SerializedObject(profPageUI);
        Prop(soProfPage, "bodyText", profBodyTMP);
        soProfPage.ApplyModifiedProperties();
        profilePage.SetActive(false);

        // ── BottomNavBar — прямой дочерний объект Canvas, полная ширина ───
        var navBar   = MakeGO("BottomNavBar", canvasGO.transform);
        var navBarRT = navBar.GetComponent<RectTransform>();
        navBarRT.anchorMin        = new Vector2(0, 0);
        navBarRT.anchorMax        = new Vector2(1, 0);
        navBarRT.pivot            = new Vector2(0.5f, 0);
        navBarRT.anchoredPosition = Vector2.zero;
        navBarRT.sizeDelta        = new Vector2(0, 80);
        navBar.AddComponent<Image>().color = C_BG;

        var navHLG = navBar.AddComponent<HorizontalLayoutGroup>();
        navHLG.childAlignment         = TextAnchor.MiddleCenter;
        navHLG.childForceExpandWidth  = true;
        navHLG.childForceExpandHeight = true;
        navHLG.childControlWidth = navHLG.childControlHeight = true;

        var (tabRecsBtn,  iconRecords,  labelRecords)  = MakeNavTab("TabRecords",  "btn_records",  navBar.transform, font);
        var (tabSetBtn,   iconSettings, labelSettings) = MakeNavTab("TabSettings", "btn_settings", navBar.transform, font);
        var (tabHomeBtn,  iconHome,     labelHome)     = MakeNavTab("TabHome",     "btn_play",     navBar.transform, font);
        var (tabAboutBtn, iconAbout,    labelAbout)    = MakeNavTab("TabAbout",    "btn_about",    navBar.transform, font);
        var (tabProfBtn,  iconProfile,  labelProfile)  = MakeNavTab("TabProfile",  "btn_profile",  navBar.transform, font);

        // Иконка Home — спрайт buttons_41 + пружинная анимация
        var homeSprite = LoadSprite("Assets/Images/Sprites/buttons.png", "buttons_41");
        if (homeSprite != null)
        {
            iconHome.sprite         = homeSprite;
            iconHome.type           = Image.Type.Simple;
            iconHome.preserveAspect = true;
            iconHome.color          = Color.white;
            var iconLE = iconHome.GetComponent<LayoutElement>();
            iconLE.minWidth      = homeSprite.rect.width;
            iconLE.minHeight     = homeSprite.rect.height;
            iconLE.preferredWidth  = homeSprite.rect.width;
            iconLE.preferredHeight = homeSprite.rect.height;
        }
        tabHomeBtn.gameObject.AddComponent<ButtonSpringAnim>();

        // Иконка Settings — спрайт buttons_33
        var settingsSprite = LoadSprite("Assets/Images/Sprites/buttons.png", "buttons_33");
        if (settingsSprite != null)
        {
            iconSettings.sprite         = settingsSprite;
            iconSettings.type           = Image.Type.Simple;
            iconSettings.preserveAspect = true;
            iconSettings.color          = Color.white;
            var iconLE = iconSettings.GetComponent<LayoutElement>();
            iconLE.minWidth      = settingsSprite.rect.width;
            iconLE.minHeight     = settingsSprite.rect.height;
            iconLE.preferredWidth  = settingsSprite.rect.width;
            iconLE.preferredHeight = settingsSprite.rect.height;
        }
        tabSetBtn.gameObject.AddComponent<ButtonSpringAnim>();

        // Иконка About — спрайт buttons_34
        var aboutSprite = LoadSprite("Assets/Images/Sprites/buttons.png", "buttons_34");
        if (aboutSprite != null)
        {
            iconAbout.sprite         = aboutSprite;
            iconAbout.type           = Image.Type.Simple;
            iconAbout.preserveAspect = true;
            iconAbout.color          = Color.white;
            var iconLE = iconAbout.GetComponent<LayoutElement>();
            iconLE.minWidth      = aboutSprite.rect.width;
            iconLE.minHeight     = aboutSprite.rect.height;
            iconLE.preferredWidth  = aboutSprite.rect.width;
            iconLE.preferredHeight = aboutSprite.rect.height;
        }
        tabAboutBtn.gameObject.AddComponent<ButtonSpringAnim>();

        // Иконка Profile — спрайт buttons_34
        var profileSprite = LoadSprite("Assets/Images/Sprites/buttons.png", "buttons_36");
        if (profileSprite != null)
        {
            iconProfile.sprite         = profileSprite;
            iconProfile.type           = Image.Type.Simple;
            iconProfile.preserveAspect = true;
            iconProfile.color          = Color.white;
            var iconLE = iconProfile.GetComponent<LayoutElement>();
            iconLE.minWidth      = profileSprite.rect.width;
            iconLE.minHeight     = profileSprite.rect.height;
            iconLE.preferredWidth  = profileSprite.rect.width;
            iconLE.preferredHeight = profileSprite.rect.height;
        }
        tabProfBtn.gameObject.AddComponent<ButtonSpringAnim>();

        // Иконка Records — спрайт buttons_42
        var recordsSprite = LoadSprite("Assets/Images/Sprites/buttons.png", "buttons_42");
        if (recordsSprite != null)
        {
            iconRecords.sprite         = recordsSprite;
            iconRecords.type           = Image.Type.Simple;
            iconRecords.preserveAspect = true;
            iconRecords.color          = Color.white;
            var iconLE = iconRecords.GetComponent<LayoutElement>();
            iconLE.minWidth      = recordsSprite.rect.width;
            iconLE.minHeight     = recordsSprite.rect.height;
            iconLE.preferredWidth  = recordsSprite.rect.width;
            iconLE.preferredHeight = recordsSprite.rect.height;
        }
        tabRecsBtn.gameObject.AddComponent<ButtonSpringAnim>();

        // ── Попап «нет вопросов» ─────────────────────────────────────────
        var popup = MakeGO("NoQuestionsPopup", canvasGO.transform);
        Stretch(popup);
        popup.AddComponent<Image>().color = C_OVERLAY;
        var popupOverlayCG = popup.AddComponent<CanvasGroup>();
        popup.SetActive(false);

        var card    = MakeGO("PopupCard", popup.transform);
        var cardRT  = card.GetComponent<RectTransform>();
        cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot     = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(900, 640);
        card.AddComponent<Image>().color = C_CARD;
        var popupCardCG = card.AddComponent<CanvasGroup>();
        var cardVLG = card.AddComponent<VerticalLayoutGroup>();
        cardVLG.childAlignment = TextAnchor.MiddleCenter;
        cardVLG.childForceExpandWidth = true;
        cardVLG.childControlWidth = cardVLG.childControlHeight = true;
        cardVLG.padding = new RectOffset(60, 60, 60, 60);
        cardVLG.spacing = 32;

        var popupIcon = MakeTMP("PopupIcon", card.transform, "!", 96, C_SECONDARY, font, minH: 110, bold: true);
        popupIcon.alignment = TextAlignmentOptions.Center;
        AddLocKey(popupIcon.gameObject, "no_questions_icon");
        var popupTitle = MakeTMP("PopupTitle", card.transform, "Нет вопросов", 44, C_TEXT, font, minH: 60, bold: true);
        popupTitle.alignment = TextAlignmentOptions.Center;
        AddLocKey(popupTitle.gameObject, "no_questions_title");
        var popupMsg = MakeTMP("PopupMessage", card.transform, "", 34, C_TEXT2, font, minH: 80);
        popupMsg.alignment = TextAlignmentOptions.Center;
        popupMsg.enableWordWrapping = true;
        var btnCloseGO = MakePrimaryButton("BtnClosePopup", card.transform, "Понятно", font, minH: 110);
        AddLocKey(btnCloseGO, "btn_close");

        // ── AudioManager ──────────────────────────────────────────────────
        var audioGO   = new GameObject("AudioManager");
        var audioMgr  = audioGO.AddComponent<AudioManager>();
        var audioSrc1 = audioGO.AddComponent<AudioSource>();
        audioSrc1.loop = true; audioSrc1.playOnAwake = false; audioSrc1.volume = 0.6f;
        var audioSrc2 = audioGO.AddComponent<AudioSource>();
        audioSrc2.playOnAwake = false; audioSrc2.volume = 1f;

        var soAudio = new UnityEditor.SerializedObject(audioMgr);
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
        gmGO.AddComponent<GameManager>();

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

            var qGuids = AssetDatabase.FindAssets("t:QuestionData");
            var allQ   = new List<QuestionData>();
            foreach (var qg in qGuids)
            {
                var q = AssetDatabase.LoadAssetAtPath<QuestionData>(
                            AssetDatabase.GUIDToAssetPath(qg));
                if (q != null) allQ.Add(q);
            }
            db.allQuestions = allQ;

            UnityEditor.EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }

        // Завершаем wiring ProfilePageUI.questionDatabase теперь, когда db объявлен
        Prop(soProfPage, "questionDatabase", db);
        soProfPage.ApplyModifiedProperties();

        // MainMenuUI
        var managerGO = MakeRootGO("MenuManager");
        var menuUI = managerGO.AddComponent<MainMenuUI>();
        var soUI   = new UnityEditor.SerializedObject(menuUI);

        Prop(soUI, "questionDatabase",     db);
        Prop(soUI, "homePage",             homePage);
        Prop(soUI, "recordsPage",          recordsPage);
        Prop(soUI, "settingsPage",         settingsPage);
        Prop(soUI, "aboutPage",            aboutPage);
        Prop(soUI, "profilePage",          profilePage);
        Prop(soUI, "tabHome",              tabHomeBtn);
        Prop(soUI, "tabRecords",           tabRecsBtn);
        Prop(soUI, "tabSettings",          tabSetBtn);
        Prop(soUI, "tabAbout",             tabAboutBtn);
        Prop(soUI, "tabProfile",           tabProfBtn);
        Prop(soUI, "iconHome",             iconHome);
        Prop(soUI, "iconRecords",          iconRecords);
        Prop(soUI, "iconSettings",         iconSettings);
        Prop(soUI, "iconAbout",            iconAbout);
        Prop(soUI, "iconProfile",          iconProfile);
        Prop(soUI, "labelHome",            labelHome);
        Prop(soUI, "labelRecords",         labelRecords);
        Prop(soUI, "labelSettings",        labelSettings);
        Prop(soUI, "labelAbout",           labelAbout);
        Prop(soUI, "labelProfile",         labelProfile);
        Prop(soUI, "categoryGrid",         gridGO.transform);
        Prop(soUI, "categoryButtonPrefab", catBtnPrefab?.GetComponent<CategoryButtonUI>());
        Prop(soUI, "btnPlay",              btnPlayGO.GetComponent<Button>());
        Prop(soUI, "btnArcade",           btnArcadeGO.GetComponent<Button>());
        Prop(soUI, "statsText",            statsTMP);
        Prop(soUI, "recordsContent",       recContent.transform);

        var noQPopupComp = popup.AddComponent<NoQuestionsPopup>();
        var soNQ = new UnityEditor.SerializedObject(noQPopupComp);
        Prop(soNQ, "panel",        popup);
        Prop(soNQ, "btnClose",     btnCloseGO.GetComponent<Button>());
        Prop(soNQ, "sheetRect",    cardRT);
        Prop(soNQ, "sheetGroup",   popupCardCG);
        Prop(soNQ, "overlayGroup", popupOverlayCG);
        Prop(soNQ, "messageText",  popupMsg);
        soNQ.ApplyModifiedProperties();

        Prop(soUI, "noQuestionsPopup", noQPopupComp);
        soUI.ApplyModifiedProperties();

        SaveScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("[GameSceneBuilder] ✓ MainMenu сцена построена.");
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

        // IconImage — верхние 60% кнопки
        var iconGO = new GameObject("IconImage", typeof(RectTransform));
        iconGO.transform.SetParent(root.transform, false);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.38f);
        iconRT.anchorMax = new Vector2(0.9f, 0.96f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color           = Color.white;
        iconImg.preserveAspect  = true;
        iconGO.SetActive(false); // включается в Setup() если icon != null

        // Label
        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(root.transform, false);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0f, 0f);
        lblRT.anchorMax = new Vector2(1f, 0.42f);
        lblRT.offsetMin = new Vector2(12f, 6f);
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
        var soCat = new UnityEditor.SerializedObject(catUI);
        var pBtn  = soCat.FindProperty("button");     if (pBtn  != null) pBtn.objectReferenceValue  = btn;
        var pBg   = soCat.FindProperty("background"); if (pBg  != null) pBg.objectReferenceValue   = rootImg;
        var pIcon = soCat.FindProperty("iconImage");  if (pIcon != null) pIcon.objectReferenceValue = iconImg;
        var pLbl  = soCat.FindProperty("label");      if (pLbl  != null) pLbl.objectReferenceValue  = lbl;
        var pHl   = soCat.FindProperty("highlight");  if (pHl   != null) pHl.objectReferenceValue   = hlGO;
        soCat.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log($"[GameSceneBuilder] CategoryButton prefab сохранён: {prefabPath}");
        return prefab;
    }
}
