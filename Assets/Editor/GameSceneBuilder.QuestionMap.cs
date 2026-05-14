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
        saVLG.padding  = new RectOffset(0, 0, 64, 0); // top padding = header height
        saVLG.spacing  = 0;

        // --- Header (ignoreLayout + absolute so VLG doesn't expand it) ---
        var header = MakeGO("Header", safeArea.transform);
        var headerLE = header.AddComponent<LayoutElement>();
        headerLE.ignoreLayout = true;
        var headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot     = new Vector2(0.5f, 1f);
        headerRT.offsetMin = new Vector2(0, -64);
        headerRT.offsetMax = Vector2.zero;
        header.AddComponent<Image>().color = C_PRIMARY;
        var hHLG = header.AddComponent<HorizontalLayoutGroup>();
        hHLG.childAlignment = TextAnchor.MiddleCenter;
        hHLG.childForceExpandWidth = false; hHLG.childForceExpandHeight = true;
        hHLG.childControlWidth = hHLG.childControlHeight = true;
        hHLG.padding = new RectOffset(24, 24, 0, 0); hHLG.spacing = 16;

        var btnBackGO = MakeSecondaryButton("BtnBack", header.transform, "← Назад", font, minH: 70, minW: 160);
        btnBackGO.GetComponent<Image>().color = new Color(1,1,1,0.2f);
        SetLE(btnBackGO, minH: 70, minW: 160);
        AddLocKey(btnBackGO, "btn_back");

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
        var feedbackTMP = MakeTMP("ResultFeedback", qCard.transform, "Правильно!", 36, C_CORRECT, font);
        SetLE(feedbackTMP.gameObject, minH: 60);
        feedbackTMP.alignment = TextAlignmentOptions.Center;
        feedbackTMP.gameObject.SetActive(false);

        // BtnContinue (внизу оверлея, вне карточки)
        var btnContinueGO = MakePrimaryButton("BtnContinue", qPanel.transform, "Продолжить", font);
        AddLocKey(btnContinueGO, "btn_continue");
        var bcRT = btnContinueGO.GetComponent<RectTransform>();
        bcRT.anchorMin = new Vector2(0.5f, 0); bcRT.anchorMax = new Vector2(0.5f, 0);
        bcRT.pivot     = new Vector2(0.5f, 0);
        bcRT.anchoredPosition = new Vector2(0, 40);
        bcRT.sizeDelta = new Vector2(400, 100);
        btnContinueGO.gameObject.SetActive(false);

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

        // --- QuestionMapUI ---
        var mapManagerGO = MakeRootGO("QuestionMapManager");
        var mapUI        = mapManagerGO.AddComponent<QuestionMapUI>();
        var soMap        = new UnityEditor.SerializedObject(mapUI);

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
        Prop(soMap, "factPopup", factPopupComp);
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
        var soTile = new UnityEditor.SerializedObject(tileUI);
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
}
