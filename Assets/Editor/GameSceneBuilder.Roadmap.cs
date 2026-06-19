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

        AddStatusBarCover(canvasGO.transform);

        var safeArea = MakeGO("SafeArea", canvasGO.transform);
        Stretch(safeArea); safeArea.AddComponent<SafeArea>();
        var saVLG = safeArea.AddComponent<VerticalLayoutGroup>();
        saVLG.childAlignment        = TextAnchor.UpperCenter;
        saVLG.childForceExpandWidth = true;
        saVLG.childForceExpandHeight = false;
        saVLG.childControlWidth = saVLG.childControlHeight = true;
        saVLG.padding  = new RectOffset(0, 0, 140, 0); // top padding = header height
        saVLG.spacing  = 0;

        // ── Header (compact, ignoreLayout + absolute so VLG doesn't expand it) ──
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
        hHLG.childAlignment        = TextAnchor.MiddleCenter;
        hHLG.childForceExpandWidth = false; hHLG.childForceExpandHeight = true;
        hHLG.childControlWidth = hHLG.childControlHeight = true;
        hHLG.padding = new RectOffset(20, 20, 0, 0); hHLG.spacing = 12;

        const string backAtlas = "Assets/Images/Sprites/additional controls.png";
        EnsureReadable(backAtlas);
        var sBack  = LoadSprite(backAtlas, "additional controls_4");
        const int backH = 120;
        int backW = sBack != null ? Mathf.RoundToInt(backH * sBack.rect.width / sBack.rect.height) : backH;
        var btnBackGO  = MakeSecondaryButton("BtnBack", header.transform, "", font, minH: backH, minW: backW);
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

        var titleTMP = MakeTMP("Title", header.transform, "Все вопросы", 42, Color.white, font);
        SetLE(titleTMP.gameObject, flexW: 1);
        titleTMP.alignment = TextAlignmentOptions.Center;
        AddLocKey(titleTMP.gameObject, "btn_arcade");

        // Правый слот для симметрии (равен ширине BtnBack), чтобы заголовок был по центру
        var rightSlot = MakeGO("RightSlot", header.transform);
        SetLE(rightSlot, minW: backW, flexW: 0);
        rightSlot.AddComponent<Image>().color = Color.clear;

        // ── ProgressBar ──────────────────────────────────────────────────────
        var progressRow = MakeGO("ProgressRow", safeArea.transform);
        SetLE(progressRow, minH: 52, prefH: 52);
        progressRow.AddComponent<Image>().color = Hex("1E4A2E"); // тёмно-зелёный под шапкой
        var pHLG = progressRow.AddComponent<HorizontalLayoutGroup>();
        pHLG.childAlignment        = TextAnchor.MiddleCenter;
        pHLG.childForceExpandWidth = false; pHLG.childForceExpandHeight = false;
        pHLG.childControlWidth = pHLG.childControlHeight = true;
        pHLG.padding = new RectOffset(24, 24, 10, 10); pHLG.spacing = 12;

        // ProgressBar из Prefab — высота по нативному размеру спрайта
        const string acAtlas = "Assets/Images/Sprites/additional controls.png";
        EnsureReadable(acAtlas);
        var sBgBar    = LoadSprite(acAtlas, "additional controls_2");
        float nativeH = sBgBar != null ? sBgBar.rect.height : 64f;
        float barH    = nativeH / 2f;
        float rowH    = nativeH + pHLG.padding.top + pHLG.padding.bottom;

        var rowLE = progressRow.GetComponent<LayoutElement>() ?? progressRow.AddComponent<LayoutElement>();
        rowLE.minHeight = rowLE.preferredHeight = rowH;

        var pbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProgressBarPath);
        if (pbPrefab == null) { Debug.LogError("[GameSceneBuilder] ProgressBar.prefab не найден. Запустите '0 — Build UI Prefabs'."); return; }

        // Бар фиксированной ширины — не растягивается на весь экран
        const float pbW = 600f;

        var leftSpacer = MakeGO("LeftSpacer", progressRow.transform);
        SetLE(leftSpacer, flexW: 1f); leftSpacer.AddComponent<Image>().color = Color.clear;

        var pbGO = (GameObject)PrefabUtility.InstantiatePrefab(pbPrefab, progressRow.transform);
        SetLE(pbGO, minW: pbW, flexW: 0, minH: barH, prefH: barH);
        var progressBarComp = pbGO.GetComponent<ProgressBarUI>();

        var rightSpacer = MakeGO("RightSpacer", progressRow.transform);
        SetLE(rightSpacer, flexW: 1f); rightSpacer.AddComponent<Image>().color = Color.clear;

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

        // ── Scrollbar (вертикальный, справа) ─────────────────────────────────
        var scrollbarGO = MakeGO("Scrollbar", scrollGO.transform);
        var sbRT = scrollbarGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot     = new Vector2(1, 0.5f);
        sbRT.anchoredPosition = Vector2.zero;
        sbRT.sizeDelta = new Vector2(18, 0);
        var sbBg = scrollbarGO.AddComponent<Image>();
        sbBg.color  = new Color(0f, 0f, 0f, 0.10f);
        sbBg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        sbBg.type   = Image.Type.Sliced;

        var sbSlideArea = MakeGO("SlidingArea", scrollbarGO.transform);
        var sbSlideRT = sbSlideArea.GetComponent<RectTransform>();
        sbSlideRT.anchorMin = Vector2.zero; sbSlideRT.anchorMax = Vector2.one;
        sbSlideRT.offsetMin = new Vector2(2, 2);
        sbSlideRT.offsetMax = new Vector2(-2, -2);

        var sbHandle = MakeGO("Handle", sbSlideArea.transform);
        var sbHandleRT = sbHandle.GetComponent<RectTransform>();
        sbHandleRT.anchorMin = Vector2.zero; sbHandleRT.anchorMax = Vector2.one;
        sbHandleRT.offsetMin = sbHandleRT.offsetMax = Vector2.zero;
        var sbHandleImg = sbHandle.AddComponent<Image>();
        sbHandleImg.color  = C_PRIMARY;
        sbHandleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sbHandleImg.type   = Image.Type.Sliced;

        var sbComp = scrollbarGO.AddComponent<Scrollbar>();
        sbComp.targetGraphic = sbHandleImg;
        sbComp.handleRect    = sbHandleRT;
        sbComp.direction     = Scrollbar.Direction.BottomToTop;

        scroll.verticalScrollbar = sbComp;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scroll.verticalScrollbarSpacing    = 4;

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

        // ── QuestionWindow + FactPopup из Prefab ────────────────────────
        var qWinGO = InstantiateUIPrefab(QuestionWindowPath, canvasGO.transform);
        if (qWinGO == null) return;
        var qWin = qWinGO.GetComponent<QuestionWindow>();

        var factGO = InstantiateUIPrefab(FactPopupPath, canvasGO.transform);
        if (factGO == null) return;
        var factPopupComp = factGO.GetComponent<FactPopup>();

        var soQW = new UnityEditor.SerializedObject(qWin);
        Prop(soQW, "factPopup", factPopupComp);
        soQW.ApplyModifiedProperties();

        // ── WordBuilderWindow из Prefab ──────────────────────────────────────
        var wbWinGO = InstantiateUIPrefab(WordBuilderWindowPath, canvasGO.transform);
        if (wbWinGO == null) return;
        var wbWin = wbWinGO.GetComponent<WordBuilderWindow>();

        var soWBW = new UnityEditor.SerializedObject(wbWin);
        Prop(soWBW, "factPopup", factPopupComp);
        soWBW.ApplyModifiedProperties();

        // ── RoadmapTile Prefab ───────────────────────────────────────────────
        var tilePrefab = CreateRoadmapTilePrefab(font);

        // ── RoadmapUI ────────────────────────────────────────────────────────
        var db = FindAsset<QuestionDatabase>("t:QuestionDatabase");

        var mgrGO    = MakeRootGO("RoadmapManager");
        var roadmapUI = mgrGO.AddComponent<RoadmapUI>();
        var soMap    = new UnityEditor.SerializedObject(roadmapUI);

        Prop(soMap, "questionDatabase",  db);
        Prop(soMap, "tilePrefab",        tilePrefab?.GetComponent<RoadmapTileUI>());
        Prop(soMap, "mapContent",        mapContentRT);
        Prop(soMap, "linesContainer",    linesRT);
        Prop(soMap, "progressBar",       progressBarComp);
        Prop(soMap, "btnBack",           btnBackGO.GetComponent<Button>());
        Prop(soMap, "btnFinish",         bfBtn);
        Prop(soMap, "questionWindowFull", qWin);
        Prop(soMap, "wordBuilderWindow", wbWin);
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
        const string atlas      = "Assets/Images/Sprites/buttons.png";
        Directory.CreateDirectory("Assets/Prefabs");
        if (File.Exists(prefabPath)) AssetDatabase.DeleteAsset(prefabPath);

        var sDefault = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Icons/bg_yellow.png");
        var sCorrect = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Icons/bg_green_success.png");
        var sWrong   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Icons/bg_red.png");

        var root    = new GameObject("RoadmapTile", typeof(RectTransform));
        var rootImg = root.AddComponent<Image>();
        rootImg.sprite = sDefault != null ? sDefault
                       : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        rootImg.type  = Image.Type.Simple;
        rootImg.color = Color.white;

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = rootImg;
        btn.transition    = Selectable.Transition.None;
        root.AddComponent<ButtonSpringAnim>();

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
        Prop(soTile, "button",        btn);
        Prop(soTile, "background",    rootImg);
        Prop(soTile, "categoryIcon",  iconImg);
        Prop(soTile, "checkmark",     chkImg);
        Prop(soTile, "indexLabel",    idxTMP);
        Prop(soTile, "spriteDefault", sDefault);
        Prop(soTile, "spriteCorrect", sCorrect);
        Prop(soTile, "spriteWrong",   sWrong);
        soTile.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Selection.activeGameObject = null;
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log($"[GameSceneBuilder] RoadmapTile prefab сохранён: {prefabPath}");
        return prefab;
    }
}
