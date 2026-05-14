using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.UI;
using UstAldanQuiz.Utils;

public static partial class GameSceneBuilder
{
    // =====================================================================
    // 5. СЦЕНА КАРТЫ-РОАДМАПА
    // =====================================================================

    static void DoBuildRoadmap(bool skipIfExists = false)
    {
        if (skipIfExists && File.Exists("Assets/Scenes/Roadmap.unity"))
        { Debug.Log("[GameSceneBuilder] Roadmap.unity уже существует — пропускаем."); return; }

        OpenOrCreateScene("Assets/Scenes/Roadmap.unity");
        var font = FindFont();

        var canvasGO = SetupCanvas("Roadmap");
        SetupCamera();
        SetupEventSystem();

        var bg = MakeGO("Background", canvasGO.transform);
        Stretch(bg); bg.AddComponent<Image>().color = C_BG;

        var safeArea = MakeGO("SafeArea", canvasGO.transform);
        Stretch(safeArea); safeArea.AddComponent<SafeArea>();
        var saVLG = safeArea.AddComponent<VerticalLayoutGroup>();
        saVLG.childAlignment        = TextAnchor.UpperCenter;
        saVLG.childForceExpandWidth = true;
        saVLG.childForceExpandHeight = false;
        saVLG.childControlWidth = saVLG.childControlHeight = true;
        saVLG.padding  = new RectOffset(0, 0, 68, 0); // top padding = header height
        saVLG.spacing  = 0;

        // ── Header (compact, ignoreLayout + absolute so VLG doesn't expand it) ──
        var header = MakeGO("Header", safeArea.transform);
        var headerLE = header.AddComponent<LayoutElement>();
        headerLE.ignoreLayout = true;
        var headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot     = new Vector2(0.5f, 1f);
        headerRT.offsetMin = new Vector2(0, -68);
        headerRT.offsetMax = Vector2.zero;
        header.AddComponent<Image>().color = C_PRIMARY;
        var hHLG = header.AddComponent<HorizontalLayoutGroup>();
        hHLG.childAlignment        = TextAnchor.MiddleCenter;
        hHLG.childForceExpandWidth = false; hHLG.childForceExpandHeight = true;
        hHLG.childControlWidth = hHLG.childControlHeight = true;
        hHLG.padding = new RectOffset(20, 20, 0, 0); hHLG.spacing = 12;

        var btnBackGO = MakeSecondaryButton("BtnBack", header.transform, "← Назад", font, minH: 52, minW: 140);
        btnBackGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.2f);
        SetLE(btnBackGO, minH: 52, minW: 140);
        AddLocKey(btnBackGO, "btn_back");

        var headerSpacer = MakeGO("Spacer", header.transform);
        SetLE(headerSpacer, flexW: 1f);
        headerSpacer.AddComponent<Image>().color = Color.clear;

        var btnResetGO = MakeSecondaryButton("BtnReset", header.transform, "↺ Сброс", font, minH: 52, minW: 140);
        btnResetGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.2f);
        SetLE(btnResetGO, minH: 52, minW: 140);
        AddLocKey(btnResetGO, "btn_reset");

        // ── ProgressBar ──────────────────────────────────────────────────────
        var progressRow = MakeGO("ProgressRow", safeArea.transform);
        SetLE(progressRow, minH: 52, prefH: 52);
        progressRow.AddComponent<Image>().color = Hex("1E4A2E"); // тёмно-зелёный под шапкой
        var pHLG = progressRow.AddComponent<HorizontalLayoutGroup>();
        pHLG.childAlignment        = TextAnchor.MiddleCenter;
        pHLG.childForceExpandWidth = false; pHLG.childForceExpandHeight = false;
        pHLG.childControlWidth = pHLG.childControlHeight = true;
        pHLG.padding = new RectOffset(24, 24, 10, 10); pHLG.spacing = 12;

        // BarContainer (background + fill)
        var barContainerGO = MakeGO("ProgressBarContainer", progressRow.transform);
        SetLE(barContainerGO, flexW: 1f, minH: 20);
        barContainerGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.22f);

        var progressFillGO = MakeGO("ProgressFill", barContainerGO.transform);
        var progressFillRT = progressFillGO.GetComponent<RectTransform>();
        progressFillRT.anchorMin = new Vector2(0, 0);
        progressFillRT.anchorMax = new Vector2(0, 1);
        progressFillRT.pivot     = new Vector2(0, 0.5f);
        progressFillRT.anchoredPosition = Vector2.zero;
        progressFillRT.sizeDelta = new Vector2(0, 0);
        progressFillGO.AddComponent<Image>().color = C_SECONDARY;

        // ProgressText (right of bar)
        var progressTextTMP = MakeTMP("ProgressText", progressRow.transform, "0/0", 26, Color.white, font, minH: 32);
        SetLE(progressTextTMP.gameObject, minW: 90);
        progressTextTMP.alignment = TextAlignmentOptions.MidlineRight;

        // ── MapScrollView ────────────────────────────────────────────────────
        var scrollGO = MakeGO("MapScrollView", safeArea.transform);
        SetLE(scrollGO, flexH: 1f, minH: 400);
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true;
        scroll.scrollSensitivity = 60f;
        scroll.movementType      = ScrollRect.MovementType.Elastic;

        var viewport = MakeGO("Viewport", scrollGO.transform);
        Stretch(viewport);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var mapContent = MakeGO("MapContent", viewport.transform);
        var mapContentRT = mapContent.GetComponent<RectTransform>();
        mapContentRT.anchorMin        = new Vector2(0, 1);
        mapContentRT.anchorMax        = new Vector2(0, 1);
        mapContentRT.pivot            = new Vector2(0, 1);
        mapContentRT.anchoredPosition = Vector2.zero;
        mapContentRT.sizeDelta        = new Vector2(1080, 2400); // sized at runtime by RoadmapUI
        mapContent.AddComponent<Image>().color = Color.clear;
        scroll.content = mapContentRT;

        // LinesContainer — first child of mapContent so lines render beneath tiles
        var linesContainerGO = MakeGO("LinesContainer", mapContent.transform);
        var linesRT = linesContainerGO.GetComponent<RectTransform>();
        linesRT.anchorMin = Vector2.zero;
        linesRT.anchorMax = Vector2.one;
        linesRT.offsetMin = linesRT.offsetMax = Vector2.zero;
        linesContainerGO.AddComponent<Image>().color = Color.clear;

        // ── BtnFinish ───────────────────────────────────────────────────────
        var btnFinishGO = MakeGO("BtnFinish", canvasGO.transform);
        var bfRT = btnFinishGO.GetComponent<RectTransform>();
        bfRT.anchorMin = new Vector2(0.5f, 0); bfRT.anchorMax = new Vector2(0.5f, 0);
        bfRT.pivot     = new Vector2(0.5f, 0);
        bfRT.anchoredPosition = new Vector2(0, 48);
        bfRT.sizeDelta = new Vector2(800, 110);
        var bfImg = btnFinishGO.AddComponent<Image>();
        bfImg.color  = C_SECONDARY;
        bfImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bfImg.type   = Image.Type.Sliced;
        var bfBtn = btnFinishGO.AddComponent<Button>(); bfBtn.targetGraphic = bfImg;
        btnFinishGO.AddComponent<ButtonSFX>();
        var bfLbl = MakeGO("Text", btnFinishGO.transform);
        var bfLblRT = bfLbl.GetComponent<RectTransform>();
        bfLblRT.anchorMin = Vector2.zero; bfLblRT.anchorMax = Vector2.one;
        bfLblRT.offsetMin = bfLblRT.offsetMax = Vector2.zero;
        var bfTMP = bfLbl.AddComponent<TextMeshProUGUI>();
        bfTMP.text = "← Главное меню"; bfTMP.fontSize = 36; bfTMP.color = Color.white;
        bfTMP.alignment = TextAlignmentOptions.Center; bfTMP.fontStyle = FontStyles.Bold;
        if (font != null) bfTMP.font = font;
        AddLocKey(btnFinishGO, "btn_main_menu");
        btnFinishGO.SetActive(false);

        // ── QuestionPanel ────────────────────────────────────────────────────
        var qPanel = MakeGO("QuestionPanel", canvasGO.transform);
        Stretch(qPanel);
        qPanel.AddComponent<Image>().color = C_OVERLAY;
        qPanel.SetActive(false);

        var qCard = MakeGO("QuestionCard", qPanel.transform);
        var qCardRT = qCard.GetComponent<RectTransform>();
        qCardRT.anchorMin = new Vector2(0.03f, 0.05f);
        qCardRT.anchorMax = new Vector2(0.97f, 0.95f);
        qCardRT.offsetMin = qCardRT.offsetMax = Vector2.zero;
        qCard.AddComponent<Image>().color = C_CARD;
        var qCardCG = qCard.AddComponent<CanvasGroup>();

        var qCardVLG = qCard.AddComponent<VerticalLayoutGroup>();
        qCardVLG.childAlignment        = TextAnchor.UpperCenter;
        qCardVLG.childForceExpandWidth = true;
        qCardVLG.childForceExpandHeight = false;
        qCardVLG.childControlWidth = qCardVLG.childControlHeight = true;
        qCardVLG.padding = new RectOffset(40, 40, 40, 40);
        qCardVLG.spacing = 24;

        var qTextTMP = MakeTMP("QuestionText", qCard.transform, "Текст вопроса...", 34, C_TEXT, font);
        qTextTMP.enableWordWrapping = true;
        SetLE(qTextTMP.gameObject, minH: 120, prefH: 200, flexH: 1f);

        var qImgContainer = MakeGO("QuestionImageContainer", qCard.transform);
        SetLE(qImgContainer, minH: 200, prefH: 300);
        qImgContainer.SetActive(false);
        var qImgGO = MakeGO("QuestionImage", qImgContainer.transform);
        var qImgRT = qImgGO.GetComponent<RectTransform>();
        qImgRT.anchorMin = Vector2.zero; qImgRT.anchorMax = Vector2.one;
        qImgRT.offsetMin = qImgRT.offsetMax = Vector2.zero;
        var qImg = qImgGO.AddComponent<Image>();
        qImg.preserveAspect = true;

        var answersGrid = MakeGO("AnswersGrid", qCard.transform);
        SetLE(answersGrid, minH: 340, prefH: 380);
        var aVLG = answersGrid.AddComponent<VerticalLayoutGroup>();
        aVLG.childAlignment        = TextAnchor.UpperCenter;
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

        var feedbackTMP = MakeTMP("ResultFeedback", qCard.transform, "Правильно!", 36, C_CORRECT, font);
        SetLE(feedbackTMP.gameObject, minH: 60);
        feedbackTMP.alignment = TextAlignmentOptions.Center;
        feedbackTMP.gameObject.SetActive(false);

        var btnContinueGO = MakePrimaryButton("BtnContinue", qPanel.transform, "Продолжить", font);
        AddLocKey(btnContinueGO, "btn_continue");
        var bcRT = btnContinueGO.GetComponent<RectTransform>();
        bcRT.anchorMin = new Vector2(0.5f, 0); bcRT.anchorMax = new Vector2(0.5f, 0);
        bcRT.pivot     = new Vector2(0.5f, 0);
        bcRT.anchoredPosition = new Vector2(0, 40);
        bcRT.sizeDelta = new Vector2(400, 100);
        btnContinueGO.SetActive(false);

        // ── FactPopup ───────────────────────────────────────────────────────
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
        var factCardCG = factCard.AddComponent<CanvasGroup>();
        factCard.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var factCardVLG = factCard.AddComponent<VerticalLayoutGroup>();
        factCardVLG.childAlignment        = TextAnchor.UpperCenter;
        factCardVLG.childForceExpandWidth = true;
        factCardVLG.childControlWidth = factCardVLG.childControlHeight = true;
        factCardVLG.padding = new RectOffset(48, 48, 48, 48);
        factCardVLG.spacing = 32;

        var factTextTMP = MakeTMP("FactText", factCard.transform, "", 34, C_TEXT, font);
        factTextTMP.alignment         = TextAlignmentOptions.Center;
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

        // ── RoadmapTile Prefab ───────────────────────────────────────────────
        var tilePrefab = CreateRoadmapTilePrefab(font);

        // ── RoadmapUI ────────────────────────────────────────────────────────
        var db = FindAsset<QuestionDatabase>("t:QuestionDatabase");

        var mgrGO    = MakeRootGO("RoadmapManager");
        var roadmapUI = mgrGO.AddComponent<RoadmapUI>();
        var soMap    = new UnityEditor.SerializedObject(roadmapUI);

        Prop(soMap, "questionDatabase",     db);
        Prop(soMap, "tilePrefab",           tilePrefab?.GetComponent<RoadmapTileUI>());
        Prop(soMap, "mapContent",           mapContentRT);
        Prop(soMap, "linesContainer",       linesRT);
        Prop(soMap, "progressBarFill",      progressFillRT);
        Prop(soMap, "progressText",         progressTextTMP);
        Prop(soMap, "btnBack",              btnBackGO.GetComponent<Button>());
        Prop(soMap, "btnReset",             btnResetGO.GetComponent<Button>());
        Prop(soMap, "btnFinish",            bfBtn);
        Prop(soMap, "questionPanel",        qPanel);
        Prop(soMap, "questionCard",         qCardRT);
        Prop(soMap, "questionCardGroup",    qCardCG);
        Prop(soMap, "questionText",         qTextTMP);
        Prop(soMap, "questionImage",        qImg);
        Prop(soMap, "questionImageContainer", qImgContainer);
        Prop(soMap, "resultFeedback",       feedbackTMP);
        Prop(soMap, "btnContinue",          btnContinueGO.GetComponent<Button>());
        Prop(soMap, "factPopup",            factPopupComp);
        SetArr(soMap, "answerButtons", answerBtns);
        SetArr(soMap, "answerLabels",  answerLabels);
        soMap.ApplyModifiedProperties();

        SaveScene("Assets/Scenes/Roadmap.unity");
        Debug.Log("[GameSceneBuilder] ✓ Roadmap сцена построена.");
    }

    // =====================================================================
    // ПРЕФАБ ТАЙЛА РОАДМАПА
    // =====================================================================

    static GameObject CreateRoadmapTilePrefab(TMP_FontAsset font)
    {
        const string prefabPath = "Assets/Prefabs/RoadmapTile.prefab";
        Directory.CreateDirectory("Assets/Prefabs");
        if (File.Exists(prefabPath)) AssetDatabase.DeleteAsset(prefabPath);

        var root    = new GameObject("RoadmapTile", typeof(RectTransform));
        var rootImg = root.AddComponent<Image>();
        rootImg.color  = Hex("E8E0D0");
        rootImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        rootImg.type   = Image.Type.Sliced;

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = rootImg;
        var cb = btn.colors;
        cb.highlightedColor = new Color(0.85f, 0.82f, 0.76f);
        cb.pressedColor     = new Color(0.75f, 0.72f, 0.66f);
        btn.colors = cb;

        // CategoryIcon (centred, shows category sprite)
        var iconGO = new GameObject("CategoryIcon", typeof(RectTransform));
        iconGO.transform.SetParent(root.transform, false);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.15f, 0.25f);
        iconRT.anchorMax = new Vector2(0.85f, 0.85f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;

        // Checkmark dot (top-right corner, shown on correct/wrong)
        var chkGO = new GameObject("Checkmark", typeof(RectTransform));
        chkGO.transform.SetParent(root.transform, false);
        var chkRT = chkGO.GetComponent<RectTransform>();
        chkRT.anchorMin = new Vector2(0.60f, 0.60f);
        chkRT.anchorMax = new Vector2(0.95f, 0.95f);
        chkRT.offsetMin = chkRT.offsetMax = Vector2.zero;
        var chkImg = chkGO.AddComponent<Image>();
        chkImg.color  = Color.white;
        chkImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        chkImg.type   = Image.Type.Sliced;
        chkGO.SetActive(false);

        // Index label (bottom-center, small number)
        var idxGO = new GameObject("IndexLabel", typeof(RectTransform));
        idxGO.transform.SetParent(root.transform, false);
        var idxRT = idxGO.GetComponent<RectTransform>();
        idxRT.anchorMin = new Vector2(0f, 0f);
        idxRT.anchorMax = new Vector2(1f, 0.30f);
        idxRT.offsetMin = idxRT.offsetMax = Vector2.zero;
        var idxTMP = idxGO.AddComponent<TextMeshProUGUI>();
        idxTMP.text      = "1";
        idxTMP.fontSize  = 28;
        idxTMP.color     = new Color(1f, 1f, 1f, 0.85f);
        idxTMP.alignment = TextAlignmentOptions.Center;
        idxTMP.fontStyle = FontStyles.Bold;
        if (font != null) idxTMP.font = font;

        // RoadmapTileUI component
        var tileUI = root.AddComponent<RoadmapTileUI>();
        var soTile = new UnityEditor.SerializedObject(tileUI);
        Prop(soTile, "button",       btn);
        Prop(soTile, "background",   rootImg);
        Prop(soTile, "categoryIcon", iconImg);
        Prop(soTile, "checkmark",    chkImg);
        Prop(soTile, "indexLabel",   idxTMP);
        soTile.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log($"[GameSceneBuilder] RoadmapTile prefab сохранён: {prefabPath}");
        return prefab;
    }
}
