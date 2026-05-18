using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UstAldanQuiz.UI;
using UstAldanQuiz.Utils;

public static partial class GameSceneBuilder
{
    const string PrefabsPath        = "Assets/Prefabs/UI";
    const string QuestionWindowPath = "Assets/Prefabs/UI/QuestionWindow.prefab";
    const string FactPopupPath      = "Assets/Prefabs/UI/FactPopup.prefab";
    const string ProgressBarPath    = "Assets/Prefabs/UI/ProgressBar.prefab";

    // =====================================================================
    // МЕНЮ
    // =====================================================================

    [MenuItem("UstAldan Quiz/Game Setup/0 — Build UI Prefabs")]
    public static void BuildUIPrefabs()
    {
        EnsureAssetFolder("Assets/Prefabs");
        EnsureAssetFolder(PrefabsPath);
        BuildQuestionWindowPrefab();
        BuildFactPopupPrefab();
        BuildProgressBarPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GameSceneBuilder] ✓ UI Prefabs собраны.");
    }

    // =====================================================================
    // ВСПОМОГАТЕЛЬНЫЕ
    // =====================================================================

    static void EnsureAssetFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
        var folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }

    // Загружает prefab и размещает его в parent, растягивая на весь Canvas
    internal static GameObject InstantiateUIPrefab(string prefabPath, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[GameSceneBuilder] Prefab не найден: {prefabPath}. Запустите '0 — Build UI Prefabs'.");
            return null;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Stretch(go);
        return go;
    }

    // =====================================================================
    // QuestionWindow.prefab
    // =====================================================================

    static void BuildQuestionWindowPrefab()
    {
        var font = FindFont();
        const string popupsAtlas = "Assets/Images/Sprites/popups.png";
        EnsureReadable(popupsAtlas);
        var sPopup = LoadSprite(popupsAtlas, "popups_3");

        // Root — контейнер без визуала, всегда активен (нужен для корутин MonoBehaviour)
        var root = new GameObject("QuestionWindow", typeof(RectTransform));

        // Overlay = panel для BaseWindow (скрывается при Close)
        var overlay = MakeGO("Overlay", root.transform);
        Stretch(overlay);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        var overlayCG = overlay.AddComponent<CanvasGroup>();
        overlay.SetActive(false);

        // Sheet
        var sheet   = MakeGO("Sheet", overlay.transform);
        var sheetRT = sheet.GetComponent<RectTransform>();
        sheetRT.anchorMin = new Vector2(0.04f, 0.05f);
        sheetRT.anchorMax = new Vector2(0.96f, 0.88f);
        sheetRT.offsetMin = sheetRT.offsetMax = Vector2.zero;
        var sheetCG  = sheet.AddComponent<CanvasGroup>();
        var sheetVLG = sheet.AddComponent<VerticalLayoutGroup>();
        sheetVLG.childAlignment         = TextAnchor.UpperCenter;
        sheetVLG.childForceExpandWidth  = true;
        sheetVLG.childForceExpandHeight = false;
        sheetVLG.childControlWidth = sheetVLG.childControlHeight = true;
        sheetVLG.spacing = 20;

        // Zone_Question
        var zoneQ    = MakeGO("Zone_Question", sheet.transform);
        SetLE(zoneQ, minH: 180, prefH: 220, flexH: 1f);
        var zoneQImg = zoneQ.AddComponent<Image>();
        zoneQImg.sprite = sPopup; zoneQImg.type = Image.Type.Sliced; zoneQImg.color = Color.white;
        var zoneQVLG = zoneQ.AddComponent<VerticalLayoutGroup>();
        zoneQVLG.childAlignment         = TextAnchor.MiddleCenter;
        zoneQVLG.childForceExpandWidth  = true;
        zoneQVLG.childForceExpandHeight = false;
        zoneQVLG.childControlWidth = zoneQVLG.childControlHeight = true;
        zoneQVLG.padding = new RectOffset(48, 48, 36, 36);
        var qTextTMP = MakeTMP("QuestionText", zoneQ.transform, "Текст вопроса...", 34, C_TEXT, font);
        qTextTMP.enableWordWrapping = true;
        qTextTMP.alignment          = TextAlignmentOptions.Center;
        SetLE(qTextTMP.gameObject, flexH: 1f);

        // Zone_Media
        var zoneMedia = MakeGO("Zone_Media", sheet.transform);
        SetLE(zoneMedia, minH: 200, prefH: 260);
        zoneMedia.SetActive(false);
        var qImgGO = MakeGO("QuestionImage", zoneMedia.transform);
        var qImgRT = qImgGO.GetComponent<RectTransform>();
        qImgRT.anchorMin = Vector2.zero; qImgRT.anchorMax = Vector2.one;
        qImgRT.offsetMin = qImgRT.offsetMax = Vector2.zero;
        var qImg = qImgGO.AddComponent<Image>();
        qImg.preserveAspect = true;

        // Zone_Answers
        var zoneAnswers = MakeGO("Zone_Answers", sheet.transform);
        SetLE(zoneAnswers, minH: 340, prefH: 360);
        var aVLG = zoneAnswers.AddComponent<VerticalLayoutGroup>();
        aVLG.childAlignment         = TextAnchor.UpperCenter;
        aVLG.childForceExpandWidth  = true;
        aVLG.childForceExpandHeight = false;
        aVLG.childControlWidth = aVLG.childControlHeight = true;
        aVLG.spacing = 14;

        string[] abcd = { "A", "B", "C", "D" };
        var answerBtns   = new Button[4];
        var answerLabels = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            var aBtnGO  = MakeGO($"AnswerBtn_{abcd[i]}", zoneAnswers.transform);
            SetLE(aBtnGO, minH: 92, prefH: 96);
            var aBtnImg = aBtnGO.AddComponent<Image>();
            aBtnImg.sprite = sPopup; aBtnImg.type = Image.Type.Sliced; aBtnImg.color = Color.white;
            var aBtn = aBtnGO.AddComponent<Button>();
            aBtn.targetGraphic = aBtnImg; aBtn.transition = Selectable.Transition.None;
            aBtnGO.AddComponent<ButtonSpringAnim>();
            var aLbl   = MakeTMP($"Label_{abcd[i]}", aBtnGO.transform, $"{abcd[i]}: Вариант ответа", 30, C_TEXT, font);
            aLbl.enableWordWrapping = true;
            aLbl.alignment          = TextAlignmentOptions.MidlineLeft;
            var aLblRT = aLbl.GetComponent<RectTransform>();
            aLblRT.anchorMin = Vector2.zero; aLblRT.anchorMax = Vector2.one;
            aLblRT.offsetMin = new Vector2(40, 8); aLblRT.offsetMax = new Vector2(-40, -8);
            answerBtns[i]   = aBtn;
            answerLabels[i] = aLbl;
        }

        // Zone_Bottom — резервирует место для фидбека и кнопки без смещения контента
        var zoneBottom = MakeGO("Zone_Bottom", sheet.transform);
        SetLE(zoneBottom, minH: 260, prefH: 260);

        var feedbackTMP = MakeTMP("ResultFeedback", zoneBottom.transform, "Правильно!", 38, C_CORRECT, font);
        var feedbackRT  = feedbackTMP.GetComponent<RectTransform>();
        feedbackRT.anchorMin = new Vector2(0f, 0.55f); feedbackRT.anchorMax = Vector2.one;
        feedbackRT.offsetMin = feedbackRT.offsetMax = Vector2.zero;
        feedbackTMP.alignment = TextAlignmentOptions.Center;
        feedbackTMP.gameObject.SetActive(false);

        var btnContinueGO = MakePrimaryButton("BtnContinue", zoneBottom.transform, "Продолжить", font);
        AddLocKey(btnContinueGO, "btn_continue");
        var bcRT = btnContinueGO.GetComponent<RectTransform>();
        bcRT.anchorMin        = new Vector2(0.5f, 0f);
        bcRT.anchorMax        = new Vector2(0.5f, 0f);
        bcRT.pivot            = new Vector2(0.5f, 0f);
        bcRT.anchoredPosition = new Vector2(0f, 8f);
        bcRT.sizeDelta        = new Vector2(520f, 150f);
        btnContinueGO.SetActive(false);
        ApplyHyperCasualButton(btnContinueGO, "Assets/Images/Sprites/buttons.png", "buttons_12", "buttons_13");

        // BtnClose — верхний правый угол Sheet (абсолютное позиционирование внутри Overlay)
        EnsureReadable("Assets/Images/Sprites/buttons.png");
        var sClose     = LoadSprite("Assets/Images/Sprites/buttons.png", "buttons_10");
        var btnCloseGO = MakeGO("BtnClose", overlay.transform);
        var btnCloseRT = btnCloseGO.GetComponent<RectTransform>();
        btnCloseRT.anchorMin        = new Vector2(0.96f, 0.88f);
        btnCloseRT.anchorMax        = new Vector2(0.96f, 0.88f);
        btnCloseRT.pivot            = new Vector2(0.5f, 0.5f);
        btnCloseRT.anchoredPosition = Vector2.zero;
        btnCloseRT.sizeDelta        = new Vector2(110f, 110f);
        var btnCloseImg = btnCloseGO.AddComponent<Image>();
        btnCloseImg.sprite          = sClose;
        btnCloseImg.type            = Image.Type.Simple;
        btnCloseImg.preserveAspect  = true;
        btnCloseImg.color           = Color.white;
        if (sClose != null) btnCloseImg.alphaHitTestMinimumThreshold = 0.1f;
        var btnCloseBtn = btnCloseGO.AddComponent<Button>();
        btnCloseBtn.targetGraphic = btnCloseImg;
        btnCloseBtn.transition    = Selectable.Transition.None;
        btnCloseGO.AddComponent<ButtonSFX>();
        btnCloseGO.AddComponent<ButtonSpringAnim>();

        // Компонент
        var qWin = root.AddComponent<QuestionWindow>();
        var soQW = new SerializedObject(qWin);
        Prop(soQW, "panel",          overlay);
        Prop(soQW, "btnClose",       btnCloseBtn);
        Prop(soQW, "sheetRect",      sheetRT);
        Prop(soQW, "sheetGroup",     sheetCG);
        Prop(soQW, "overlayGroup",   overlayCG);
        Prop(soQW, "questionText",   qTextTMP);
        Prop(soQW, "mediaZone",      zoneMedia);
        Prop(soQW, "questionImage",  qImg);
        Prop(soQW, "resultFeedback", feedbackTMP);
        Prop(soQW, "btnContinue",    btnContinueGO.GetComponent<Button>());
        SetArr(soQW, "answerButtons", answerBtns);
        SetArr(soQW, "answerLabels",  answerLabels);
        soQW.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, QuestionWindowPath);
        Object.DestroyImmediate(root);
        Debug.Log("[GameSceneBuilder] ✓ QuestionWindow.prefab");
    }

    // =====================================================================
    // ProgressBar.prefab
    // =====================================================================

    static void BuildProgressBarPrefab()
    {
        var font = FindFont();
        const string acAtlas = "Assets/Images/Sprites/additional controls.png";
        EnsureReadable(acAtlas);
        var sBg   = LoadSprite(acAtlas, "additional controls_2");
        var sFill = LoadSprite(acAtlas, "additional controls_8");

        var root = new GameObject("ProgressBar", typeof(RectTransform));

        // Background
        var bg = MakeGO("Background", root.transform);
        Stretch(bg);
        var bgImg = bg.AddComponent<Image>();
        bgImg.sprite = sBg; bgImg.type = Image.Type.Simple; bgImg.color = Color.white;

        // Fill — Image.Filled/Horizontal, fillAmount управляется из кода
        var fill = MakeGO("Fill", root.transform);
        Stretch(fill);
        var fillImg = fill.AddComponent<Image>();
        fillImg.sprite     = sFill;
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 0f;
        fillImg.color      = Color.white;

        // Label — поверх бара, по центру
        var labelTMP = MakeTMP("Label", root.transform, "0 / 0", 24, Color.white, font, bold: true);
        var labelRT  = labelTMP.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        labelTMP.alignment = TextAlignmentOptions.Center;

        // Компонент
        var pbComp = root.AddComponent<ProgressBarUI>();
        var soPB   = new SerializedObject(pbComp);
        Prop(soPB, "fillImage", fillImg);
        Prop(soPB, "label",     labelTMP);
        soPB.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, ProgressBarPath);
        Object.DestroyImmediate(root);
        Debug.Log("[GameSceneBuilder] ✓ ProgressBar.prefab");
    }

    // =====================================================================
    // FactPopup.prefab
    // =====================================================================

    static void BuildFactPopupPrefab()
    {
        var font = FindFont();
        const string popupsAtlas = "Assets/Images/Sprites/popups.png";
        EnsureReadable(popupsAtlas);
        var sPopup4 = LoadSprite(popupsAtlas, "popups_4");

        var root = new GameObject("FactPopup", typeof(RectTransform));

        // Overlay = panel
        var overlay = MakeGO("Overlay", root.transform);
        Stretch(overlay);
        overlay.AddComponent<Image>().color = C_OVERLAY;
        var overlayCG = overlay.AddComponent<CanvasGroup>();
        overlay.SetActive(false);

        // Card
        var card   = MakeGO("Card", overlay.transform);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot     = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(900, 0);
        var cardImg = card.AddComponent<Image>();
        cardImg.sprite = sPopup4; cardImg.type = Image.Type.Sliced; cardImg.color = Color.white;
        var cardCG = card.AddComponent<CanvasGroup>();
        card.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var cardVLG = card.AddComponent<VerticalLayoutGroup>();
        cardVLG.childAlignment        = TextAnchor.UpperCenter;
        cardVLG.childForceExpandWidth = true;
        cardVLG.childControlWidth = cardVLG.childControlHeight = true;
        cardVLG.padding = new RectOffset(48, 48, 48, 48);
        cardVLG.spacing = 32;

        var factTextTMP = MakeTMP("FactText", card.transform, "", 34, C_TEXT, font);
        factTextTMP.alignment          = TextAlignmentOptions.Center;
        factTextTMP.enableWordWrapping = true;
        SetLE(factTextTMP.gameObject);

        // Обёртка для кнопки — запрещает растяжение childForceExpandWidth
        var btnWrapper = MakeGO("BtnOkWrapper", card.transform);
        SetLE(btnWrapper, minH: 166, prefH: 166);
        var wrapHLG = btnWrapper.AddComponent<HorizontalLayoutGroup>();
        wrapHLG.childAlignment         = TextAnchor.MiddleCenter;
        wrapHLG.childForceExpandWidth  = false;
        wrapHLG.childForceExpandHeight = false;
        wrapHLG.childControlWidth = wrapHLG.childControlHeight = true;

        var btnOkGO = MakePrimaryButton("BtnOk", btnWrapper.transform, "Понятно", font, minH: 166, minW: 520);
        SetLE(btnOkGO, flexW: 0);
        AddLocKey(btnOkGO, "btn_close");
        ApplyHyperCasualButton(btnOkGO, "Assets/Images/Sprites/buttons.png", "buttons_12", "buttons_13");

        // Компонент
        var factComp = root.AddComponent<FactPopup>();
        var soFact   = new SerializedObject(factComp);
        Prop(soFact, "panel",        overlay);
        Prop(soFact, "btnClose",     btnOkGO.GetComponent<Button>());
        Prop(soFact, "sheetRect",    cardRT);
        Prop(soFact, "sheetGroup",   cardCG);
        Prop(soFact, "overlayGroup", overlayCG);
        Prop(soFact, "factText",     factTextTMP);
        soFact.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, FactPopupPath);
        Object.DestroyImmediate(root);
        Debug.Log("[GameSceneBuilder] ✓ FactPopup.prefab");
    }
}
