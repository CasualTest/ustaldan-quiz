using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UstAldanQuiz.UI;
using UstAldanQuiz.Utils;

public static partial class GameSceneBuilder
{
    // =====================================================================
    // 3. СЦЕНА КАРТЫ ВОПРОСОВ
    // =====================================================================

    static void DoBuildQuestionMap(bool skipIfExists = false)
    {
        if (skipIfExists && File.Exists("Assets/Scenes/QuestionMap.unity"))
        { Debug.Log("[GameSceneBuilder] QuestionMap.unity уже существует — пропускаем."); return; }

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
        saVLG.padding  = new RectOffset(0, 0, 140, 0); // top padding = header height
        saVLG.spacing  = 0;

        // --- Header (ignoreLayout + absolute so VLG doesn't expand it) ---
        var header = MakeGO("Header", safeArea.transform);
        var headerLE = header.AddComponent<LayoutElement>();
        headerLE.ignoreLayout = true;
        var headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot     = new Vector2(0.5f, 1f);
        headerRT.offsetMin = new Vector2(0, -140);
        headerRT.offsetMax = Vector2.zero;
        header.AddComponent<Image>().color = C_PRIMARY;
        var hHLG = header.AddComponent<HorizontalLayoutGroup>();
        hHLG.childAlignment = TextAnchor.MiddleCenter;
        hHLG.childForceExpandWidth = false; hHLG.childForceExpandHeight = true;
        hHLG.childControlWidth = hHLG.childControlHeight = true;
        hHLG.padding = new RectOffset(24, 24, 0, 0); hHLG.spacing = 16;

        const string backAtlas = "Assets/Images/Sprites/additional controls.png";
        EnsureReadable(backAtlas);
        var sBack = LoadSprite(backAtlas, "additional controls_4");
        const int backH    = 120;
        const int sideSlotW = 200; // одинаковая ширина слева и справа — текст по центру
        int backW = sBack != null ? Mathf.RoundToInt(backH * sBack.rect.width / sBack.rect.height) : backH;

        // Левый слот фиксированной ширины = правому, чтобы CategoryName был строго по центру
        var leftSlot = MakeGO("LeftSlot", header.transform);
        SetLE(leftSlot, minW: sideSlotW, flexW: 0);
        var leftSlotHLG = leftSlot.AddComponent<HorizontalLayoutGroup>();
        leftSlotHLG.childAlignment        = TextAnchor.MiddleLeft;
        leftSlotHLG.childForceExpandWidth = false;
        leftSlotHLG.childForceExpandHeight = true;
        leftSlotHLG.childControlWidth = leftSlotHLG.childControlHeight = true;
        leftSlotHLG.padding = new RectOffset(0, 0, 0, 0);
        leftSlot.AddComponent<Image>().color = Color.clear;

        var btnBackGO  = MakeSecondaryButton("BtnBack", leftSlot.transform, "", font, minH: backH, minW: backW);
        var btnBackImg = btnBackGO.GetComponent<Image>();
        if (sBack != null)
        {
            btnBackImg.sprite                       = sBack;
            btnBackImg.type                         = Image.Type.Simple;
            btnBackImg.preserveAspect               = true;
            btnBackImg.color                        = Color.white;
            btnBackImg.alphaHitTestMinimumThreshold = 0.1f;
        }
        else
        {
            btnBackImg.color = new Color(1, 1, 1, 0.2f);
        }
        var backLE = btnBackGO.GetComponent<LayoutElement>() ?? btnBackGO.AddComponent<LayoutElement>();
        backLE.minWidth       = backW;
        backLE.minHeight      = backH;
        backLE.preferredWidth = backW;
        backLE.flexibleWidth  = 0;
        var backText = btnBackGO.transform.Find("Text");
        if (backText != null) backText.gameObject.SetActive(false);
        AddLocKey(btnBackGO, "btn_back");

        var catNameTMP = MakeTMP("CategoryName", header.transform, "История", 36, Color.white, font);
        SetLE(catNameTMP.gameObject, flexW: 1);
        catNameTMP.alignment = TextAlignmentOptions.Center;

        var scoreTMP = MakeTMP("ScoreText", header.transform, "Правильных: 0/15", 28, Color.white, font);
        SetLE(scoreTMP.gameObject, minW: sideSlotW, flexW: 0);
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

        var tileSprite = LoadSprite("Assets/Images/Sprites/buttons.png", "buttons_3");
        var tileCell   = tileSprite != null
            ? new Vector2(tileSprite.rect.width, tileSprite.rect.height)
            : new Vector2(300, 200);

        var gridLG = content.AddComponent<GridLayoutGroup>();
        gridLG.cellSize        = tileCell;
        gridLG.spacing         = new Vector2(20, 20);
        gridLG.padding         = new RectOffset(30, 30, 30, 30);
        gridLG.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLG.constraintCount = 3;
        gridLG.childAlignment  = TextAnchor.UpperCenter;
        scroll.content = contentRT;

        // --- QuestionWindow ---
        const string popupsAtlas = "Assets/Images/Sprites/popups.png";
        EnsureReadable(popupsAtlas);
        var sPopup = LoadSprite(popupsAtlas, "popups_3");

        var qOverlay  = MakeGO("QuestionWindow", canvasGO.transform);
        Stretch(qOverlay);
        qOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        var qOverlayCG = qOverlay.AddComponent<CanvasGroup>();
        qOverlay.SetActive(false);

        var qSheet   = MakeGO("Sheet", qOverlay.transform);
        var qSheetRT = qSheet.GetComponent<RectTransform>();
        qSheetRT.anchorMin = new Vector2(0.04f, 0.05f);
        qSheetRT.anchorMax = new Vector2(0.96f, 0.88f);
        qSheetRT.offsetMin = qSheetRT.offsetMax = Vector2.zero;
        var qSheetCG  = qSheet.AddComponent<CanvasGroup>();
        var qSheetVLG = qSheet.AddComponent<VerticalLayoutGroup>();
        qSheetVLG.childAlignment         = TextAnchor.UpperCenter;
        qSheetVLG.childForceExpandWidth  = true;
        qSheetVLG.childForceExpandHeight = false;
        qSheetVLG.childControlWidth = qSheetVLG.childControlHeight = true;
        qSheetVLG.spacing = 20;

        // Зона 1 — вопрос
        var qZone1    = MakeGO("Zone_Question", qSheet.transform);
        SetLE(qZone1, minH: 180, prefH: 220, flexH: 1f);
        var qZone1Img = qZone1.AddComponent<Image>();
        qZone1Img.sprite = sPopup; qZone1Img.type = Image.Type.Sliced; qZone1Img.color = Color.white;
        var qZone1VLG = qZone1.AddComponent<VerticalLayoutGroup>();
        qZone1VLG.childAlignment         = TextAnchor.MiddleCenter;
        qZone1VLG.childForceExpandWidth  = true;
        qZone1VLG.childForceExpandHeight = false;
        qZone1VLG.childControlWidth = qZone1VLG.childControlHeight = true;
        qZone1VLG.padding = new RectOffset(48, 48, 36, 36);
        var qTextTMP = MakeTMP("QuestionText", qZone1.transform, "Текст вопроса...", 34, C_TEXT, font);
        qTextTMP.enableWordWrapping = true;
        qTextTMP.alignment          = TextAlignmentOptions.Center;
        SetLE(qTextTMP.gameObject, flexH: 1f);

        // Зона 2 — медиа
        var qMediaZone = MakeGO("Zone_Media", qSheet.transform);
        SetLE(qMediaZone, minH: 200, prefH: 260);
        qMediaZone.SetActive(false);
        var qImgGO = MakeGO("QuestionImage", qMediaZone.transform);
        var qImgRT = qImgGO.GetComponent<RectTransform>();
        qImgRT.anchorMin = Vector2.zero; qImgRT.anchorMax = Vector2.one;
        qImgRT.offsetMin = qImgRT.offsetMax = Vector2.zero;
        var qImg = qImgGO.AddComponent<Image>();
        qImg.preserveAspect = true;

        // Зона 3 — ответы
        var qAnswersZone = MakeGO("Zone_Answers", qSheet.transform);
        SetLE(qAnswersZone, minH: 340, prefH: 360);
        var aVLG = qAnswersZone.AddComponent<VerticalLayoutGroup>();
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
            var aBtnGO  = MakeGO($"AnswerBtn_{abcd[i]}", qAnswersZone.transform);
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

        var bottomZone = MakeGO("Zone_Bottom", qSheet.transform);
        SetLE(bottomZone, minH: 200, prefH: 200);

        var feedbackTMP = MakeTMP("ResultFeedback", bottomZone.transform, "Правильно!", 38, C_CORRECT, font);
        var feedbackRT  = feedbackTMP.GetComponent<RectTransform>();
        feedbackRT.anchorMin = new Vector2(0f, 0.44f); feedbackRT.anchorMax = Vector2.one;
        feedbackRT.offsetMin = feedbackRT.offsetMax = Vector2.zero;
        feedbackTMP.alignment = TextAlignmentOptions.Center;
        feedbackTMP.gameObject.SetActive(false);

        var btnContinueGO = MakePrimaryButton("BtnContinue", bottomZone.transform, "Продолжить", font, minH: 110);
        AddLocKey(btnContinueGO, "btn_continue");
        var bcRT = btnContinueGO.GetComponent<RectTransform>();
        bcRT.anchorMin = Vector2.zero; bcRT.anchorMax = new Vector2(1f, 0.44f);
        bcRT.offsetMin = new Vector2(0, 8); bcRT.offsetMax = Vector2.zero;
        btnContinueGO.SetActive(false);

        // BtnFinish
        var btnFinishGO = MakePrimaryButton("BtnFinish", safeArea.transform, "Завершить", font);
        AddLocKey(btnFinishGO, "btn_finish");
        SetLE(btnFinishGO, minH: 100, prefH: 100);
        btnFinishGO.GetComponent<Image>().color = C_SECONDARY;
        btnFinishGO.gameObject.SetActive(false);

        // --- QuestionTile Prefab ---
        var tilePrefab = CreateTilePrefab(font);

        // --- FactPopup ---
        var factOverlay = MakeGO("FactPopup", canvasGO.transform);
        Stretch(factOverlay);
        factOverlay.AddComponent<Image>().color = C_OVERLAY;
        var factOverlayCG = factOverlay.AddComponent<CanvasGroup>();
        factOverlay.SetActive(false);

        var factCard = MakeGO("FactCard", factOverlay.transform);
        var factCardRT = factCard.GetComponent<RectTransform>();
        factCardRT.anchorMin = factCardRT.anchorMax = new Vector2(0.5f, 0.5f);
        factCardRT.pivot     = new Vector2(0.5f, 0.5f);
        factCardRT.sizeDelta = new Vector2(900, 0);
        factCard.AddComponent<Image>().color = C_CARD;
        var factCardCG  = factCard.AddComponent<CanvasGroup>();
        factCard.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var factCardVLG = factCard.AddComponent<VerticalLayoutGroup>();
        factCardVLG.childAlignment        = TextAnchor.UpperCenter;
        factCardVLG.childForceExpandWidth = true;
        factCardVLG.childControlWidth     = factCardVLG.childControlHeight = true;
        factCardVLG.padding = new RectOffset(48, 48, 48, 48);
        factCardVLG.spacing = 32;

        var factTextTMP = MakeTMP("FactText", factCard.transform, "", 34, C_TEXT, font);
        factTextTMP.alignment       = TextAlignmentOptions.Center;
        factTextTMP.enableWordWrapping = true;
        SetLE(factTextTMP.gameObject);

        var factBtnGO = MakePrimaryButton("BtnOk", factCard.transform, "Понятно", font, minH: 110);
        AddLocKey(factBtnGO, "btn_close");

        var factPopupComp = factOverlay.AddComponent<FactPopup>();
        var soFact = new UnityEditor.SerializedObject(factPopupComp);
        Prop(soFact, "panel",        factOverlay);
        Prop(soFact, "btnClose",     factBtnGO.GetComponent<Button>());
        Prop(soFact, "sheetRect",    factCardRT);
        Prop(soFact, "sheetGroup",   factCardCG);
        Prop(soFact, "overlayGroup", factOverlayCG);
        Prop(soFact, "factText",     factTextTMP);
        soFact.ApplyModifiedProperties();

        // QuestionWindow компонент
        var qWin = qOverlay.AddComponent<QuestionWindow>();
        var soQW = new UnityEditor.SerializedObject(qWin);
        Prop(soQW, "panel",          qOverlay);
        Prop(soQW, "sheetRect",      qSheetRT);
        Prop(soQW, "sheetGroup",     qSheetCG);
        Prop(soQW, "overlayGroup",   qOverlayCG);
        Prop(soQW, "questionText",   qTextTMP);
        Prop(soQW, "mediaZone",      qMediaZone);
        Prop(soQW, "questionImage",  qImg);
        Prop(soQW, "resultFeedback", feedbackTMP);
        Prop(soQW, "btnContinue",    btnContinueGO.GetComponent<Button>());
        Prop(soQW, "factPopup",      factPopupComp);
        SetArr(soQW, "answerButtons", answerBtns);
        SetArr(soQW, "answerLabels",  answerLabels);
        soQW.ApplyModifiedProperties();

        // --- QuestionMapUI ---
        var mapManagerGO = MakeRootGO("QuestionMapManager");
        var mapUI        = mapManagerGO.AddComponent<QuestionMapUI>();
        var soMap        = new UnityEditor.SerializedObject(mapUI);

        Prop(soMap, "tilePrefab",       tilePrefab?.GetComponent<QuestionTileUI>());
        Prop(soMap, "mapContent",       content.transform);
        Prop(soMap, "categoryNameText", catNameTMP);
        Prop(soMap, "scoreText",        scoreTMP);
        Prop(soMap, "btnBack",          btnBackGO.GetComponent<Button>());
        Prop(soMap, "questionWindow",   qWin);
        Prop(soMap, "btnFinish",        btnFinishGO.GetComponent<Button>());
        soMap.ApplyModifiedProperties();

        SaveScene("Assets/Scenes/QuestionMap.unity");
        Debug.Log("[GameSceneBuilder] ✓ QuestionMap сцена построена.");
    }

    // =====================================================================
    // ПРЕФАБ ПЛИТКИ
    // =====================================================================

    static GameObject CreateTilePrefab(TMP_FontAsset font)
    {
        const string prefabPath = "Assets/Prefabs/QuestionTile.prefab";
        Directory.CreateDirectory("Assets/Prefabs");
        if (File.Exists(prefabPath)) AssetDatabase.DeleteAsset(prefabPath);

        var root = new GameObject("QuestionTile", typeof(RectTransform));

        const string atlas = "Assets/Images/Sprites/buttons.png";
        var sDefault = LoadSprite(atlas, "buttons_3");
        var sCorrect = LoadSprite(atlas, "buttons_4");
        var sWrong   = LoadSprite(atlas, "buttons_5");

        var rootImg = root.AddComponent<Image>();
        rootImg.sprite = sDefault != null ? sDefault
                       : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        rootImg.type  = Image.Type.Sliced;
        rootImg.color = sDefault != null ? Color.white : C_TILE_DEF;

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = rootImg;
        btn.transition    = Selectable.Transition.None;
        root.AddComponent<ButtonSpringAnim>();

        // Number
        var numGO  = new GameObject("TileNumber", typeof(RectTransform));
        numGO.transform.SetParent(root.transform, false);
        var numRT  = numGO.GetComponent<RectTransform>();
        numRT.anchorMin = Vector2.zero; numRT.anchorMax = Vector2.one;
        numRT.offsetMin = numRT.offsetMax = Vector2.zero;
        var numTMP = numGO.AddComponent<TextMeshProUGUI>();
        numTMP.text = "1"; numTMP.fontSize = 44; numTMP.color = C_TEXT;
        numTMP.alignment = TextAlignmentOptions.Center;
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
        var soTile = new UnityEditor.SerializedObject(tileUI);
        Prop(soTile, "button",          btn);
        Prop(soTile, "tileBackground",  rootImg);
        Prop(soTile, "tileNumber",      numTMP);
        Prop(soTile, "tileCheckmark",   chkGO.GetComponent<Image>());
        Prop(soTile, "spriteDefault",   sDefault);
        Prop(soTile, "spriteCorrect",   sCorrect);
        Prop(soTile, "spriteWrong",     sWrong);
        soTile.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log($"[GameSceneBuilder] QuestionTile prefab сохранён: {prefabPath}");
        return prefab;
    }
}
