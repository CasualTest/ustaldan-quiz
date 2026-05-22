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

        var headerSpacer = MakeGO("Spacer", header.transform);
        SetLE(headerSpacer, flexW: 1f);
        headerSpacer.AddComponent<Image>().color = Color.clear;

        const string buttonsAtlas = "Assets/Images/Sprites/buttons.png";
        EnsureReadable(buttonsAtlas);
        var sReset    = LoadSprite(buttonsAtlas, "buttons_26");
        var btnResetGO = MakeSecondaryButton("BtnReset", header.transform, "", font, minH: backH, minW: backH);
        var btnResetImg = btnResetGO.GetComponent<Image>();
        if (sReset != null)
        {
            btnResetImg.sprite                       = sReset;
            btnResetImg.type                         = Image.Type.Simple;
            btnResetImg.preserveAspect               = true;
            btnResetImg.color                        = Color.white;
            btnResetImg.alphaHitTestMinimumThreshold = 0.1f;
        }
        else
        {
            btnResetImg.color = new Color(1, 1, 1, 0.2f);
        }
        var resetLE = btnResetGO.GetComponent<LayoutElement>() ?? btnResetGO.AddComponent<LayoutElement>();
        resetLE.minWidth       = backH;
        resetLE.minHeight      = backH;
        resetLE.preferredWidth = backH;
        resetLE.flexibleWidth  = 0;
        var resetText = btnResetGO.transform.Find("Text");
        if (resetText != null) resetText.gameObject.SetActive(false);
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

        // Спейсеры по бокам → бар занимает 75% ширины (flex 6:1:1)
        var leftSpacer = MakeGO("LeftSpacer", progressRow.transform);
        SetLE(leftSpacer, flexW: 1f); leftSpacer.AddComponent<Image>().color = Color.clear;

        var pbGO = (GameObject)PrefabUtility.InstantiatePrefab(pbPrefab, progressRow.transform);
        SetLE(pbGO, flexW: 6f, minH: barH, prefH: barH);
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

        // ── QuestionWindow + FactPopup из Prefab ────────────────────────────
        var qWinGO = InstantiateUIPrefab(QuestionWindowPath, canvasGO.transform);
        if (qWinGO == null) return;
        var qWin = qWinGO.GetComponent<QuestionWindow>();

        var factGO = InstantiateUIPrefab(FactPopupPath, canvasGO.transform);
        if (factGO == null) return;
        var factPopupComp = factGO.GetComponent<FactPopup>();

        var soQW = new UnityEditor.SerializedObject(qWin);
        Prop(soQW, "factPopup", factPopupComp);
        soQW.ApplyModifiedProperties();

        // ── RoadmapTile Prefab ───────────────────────────────────────────────
        var tilePrefab = CreateRoadmapTilePrefab(font);

        // ── RoadmapUI ────────────────────────────────────────────────────────
        var db = FindAsset<QuestionDatabase>("t:QuestionDatabase");

        var mgrGO    = MakeRootGO("RoadmapManager");
        var roadmapUI = mgrGO.AddComponent<RoadmapUI>();
        var soMap    = new UnityEditor.SerializedObject(roadmapUI);

        Prop(soMap, "questionDatabase", db);
        Prop(soMap, "tilePrefab",       tilePrefab?.GetComponent<RoadmapTileUI>());
        Prop(soMap, "mapContent",       mapContentRT);
        Prop(soMap, "linesContainer",   linesRT);
        Prop(soMap, "progressBar",      progressBarComp);
        Prop(soMap, "btnBack",          btnBackGO.GetComponent<Button>());
        Prop(soMap, "btnReset",         btnResetGO.GetComponent<Button>());
        Prop(soMap, "btnFinish",        bfBtn);
        Prop(soMap, "questionWindow",   qWin);
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

        var sDefault = LoadSprite(atlas, "buttons_3");
        var sCorrect = LoadSprite(atlas, "buttons_4");
        var sWrong   = LoadSprite(atlas, "buttons_5");

        var root    = new GameObject("RoadmapTile", typeof(RectTransform));
        var rootImg = root.AddComponent<Image>();
        if (sDefault != null)
        {
            rootImg.sprite = sDefault;
            rootImg.type   = Image.Type.Sliced;
            rootImg.color  = Color.white;
        }
        else
        {
            rootImg.color  = Hex("E8E0D0");
            rootImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            rootImg.type   = Image.Type.Sliced;
        }

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
