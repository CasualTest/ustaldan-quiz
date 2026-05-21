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

        AddStatusBarCover(canvasGO.transform);

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

        var catNameTMP = MakeTMP("CategoryName", header.transform, "История", 42, Color.white, font);
        SetLE(catNameTMP.gameObject, flexW: 1);
        catNameTMP.alignment = TextAlignmentOptions.Center;

        var scoreTMP = MakeTMP("ScoreText", header.transform, "Правильных: 0/15", 34, Color.white, font);
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

        // BtnFinish
        var btnFinishGO = MakePrimaryButton("BtnFinish", safeArea.transform, "Завершить", font);
        AddLocKey(btnFinishGO, "btn_finish");
        SetLE(btnFinishGO, minH: 100, prefH: 100);
        btnFinishGO.GetComponent<Image>().color = C_SECONDARY;
        btnFinishGO.gameObject.SetActive(false);

        // --- QuestionTile Prefab ---
        var tilePrefab = CreateTilePrefab(font);

        // --- QuestionWindow + FactPopup из Prefab ---
        var qWinGO = InstantiateUIPrefab(QuestionWindowPath, canvasGO.transform);
        if (qWinGO == null) return;
        var qWin = qWinGO.GetComponent<QuestionWindow>();

        var factGO = InstantiateUIPrefab(FactPopupPath, canvasGO.transform);
        if (factGO == null) return;
        var factPopupComp = factGO.GetComponent<FactPopup>();

        var soQW = new UnityEditor.SerializedObject(qWin);
        Prop(soQW, "factPopup", factPopupComp);
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
