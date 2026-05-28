using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UstAldanQuiz.UI;
using UstAldanQuiz.Utils;

public static partial class GameSceneBuilder
{
    const string PrefabsPath              = "Assets/Prefabs/UI";
    const string QuestionWindowPath       = "Assets/Prefabs/UI/QuestionWindow.prefab";
    const string FactPopupPath            = "Assets/Prefabs/UI/FactPopup.prefab";
    const string ProgressBarPath          = "Assets/Prefabs/UI/ProgressBar.prefab";
    const string AnswerButtonPath         = "Assets/Prefabs/UI/AnswerButton.prefab";
    const string LetterTilePath           = "Assets/Prefabs/UI/LetterTile.prefab";
    const string WordBuilderWindowPath    = "Assets/Prefabs/UI/WordBuilderWindow.prefab";

    // =====================================================================
    // МЕНЮ
    // =====================================================================

    [MenuItem("UstAldan Quiz/Game Setup/0 — Build UI Prefabs")]
    public static void BuildUIPrefabs()
    {
        EnsureAssetFolder("Assets/Prefabs");
        EnsureAssetFolder(PrefabsPath);
        BuildAnswerButtonPrefab();
        BuildLetterTilePrefab();
        BuildQuestionWindowPrefab();
        BuildWordBuilderWindowPrefab();
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
    // AnswerButton.prefab
    // =====================================================================

    static void BuildAnswerButtonPrefab()
    {
        var font = FindFont();
        const string popupsAtlas = "Assets/Images/Sprites/popups.png";
        EnsureReadable(popupsAtlas);
        var sPopup = LoadSprite(popupsAtlas, "popups_3");

        var root   = new GameObject("AnswerButton", typeof(RectTransform));
        var btnImg = root.AddComponent<Image>();
        btnImg.sprite = sPopup; btnImg.type = Image.Type.Sliced; btnImg.color = Color.white;
        var btn = root.AddComponent<Button>();
        btn.targetGraphic = btnImg; btn.transition = Selectable.Transition.None;
        root.AddComponent<ButtonSpringAnim>();

        var lbl = MakeTMP("Label", root.transform, "Вариант ответа", 38, C_TEXT, font);
        lbl.enableWordWrapping = true;
        lbl.alignment          = TextAlignmentOptions.MidlineLeft;
        var lblRT = lbl.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = new Vector2(40, 8); lblRT.offsetMax = new Vector2(-40, -8);

        PrefabUtility.SaveAsPrefabAsset(root, AnswerButtonPath);
        Selection.activeGameObject = null;
        Object.DestroyImmediate(root);
        Debug.Log("[GameSceneBuilder] ✓ AnswerButton.prefab");
    }

    static (Button btn, Image img, TMP_Text lbl) SpawnAnswerButton(Transform parent, string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnswerButtonPath);
        var go     = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name    = name;
        return (go.GetComponent<Button>(),
                go.GetComponent<Image>(),
                go.GetComponentInChildren<TMP_Text>());
    }

    // =====================================================================
    // QuestionWindow.prefab  (стиль Quizzland: фото сверху, контент снизу)
    // =====================================================================

    static void BuildQuestionWindowPrefab()
    {
        var font = FindFont();
        const string popupsAtlas = "Assets/Images/Sprites/popups.png";
        EnsureReadable(popupsAtlas);
        var sPopup = LoadSprite(popupsAtlas, "popups_3");
        const string acAtlas = "Assets/Images/Sprites/additional controls.png";
        EnsureReadable(acAtlas);
        var sSpinner = LoadSprite(acAtlas, "additional controls_12");
        EnsureReadable("Assets/Images/Sprites/buttons.png");
        var sClose   = LoadSprite("Assets/Images/Sprites/buttons.png", "buttons_10");
        var sBgGray  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Icons/bg_gray.png");

        // Root (no visual — MonoBehaviour всегда активен для корутин)
        var root = new GameObject("QuestionWindow", typeof(RectTransform));

        // ── Panel (full screen, белый фон, CanvasGroup для fade) ──────────
        var panel   = MakeGO("Panel", root.transform);
        Stretch(panel);
        panel.AddComponent<Image>().color = C_BG;
        var panelCG = panel.AddComponent<CanvasGroup>();
        panel.SetActive(false);

        // Container (VLG, заполняет Panel)
        var container = MakeGO("Container", panel.transform);
        Stretch(container);
        var cVLG = container.AddComponent<VerticalLayoutGroup>();
        cVLG.childAlignment         = TextAnchor.UpperCenter;
        cVLG.childForceExpandWidth  = true;
        cVLG.childForceExpandHeight = false;
        cVLG.childControlWidth = cVLG.childControlHeight = true;
        cVLG.spacing = 0;

        // StatusBarCover
        var sbGO = MakeGO("StatusBarCover", container.transform);
        SetLE(sbGO, minH: 0, prefH: 0);
        sbGO.AddComponent<Image>().color = Color.black;
        sbGO.AddComponent<UstAldanQuiz.UI.StatusBarCover>();

        // ── ImageZone (фото сверху, фиксированная высота) ─────────────────
        var imageZone = MakeGO("ImageZone", container.transform);
        SetLE(imageZone, minH: 300, prefH: 660, flexH: 0f);
        imageZone.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);

        var qImgGO = MakeGO("QuestionImage", imageZone.transform);
        Stretch(qImgGO);
        var qImg    = qImgGO.AddComponent<RawImage>();
        var qImgARF = qImgGO.AddComponent<AspectRatioFitter>();
        qImgARF.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        var spinnerGO = MakeGO("Spinner", imageZone.transform);
        var spinRT    = spinnerGO.GetComponent<RectTransform>();
        spinRT.anchorMin = spinRT.anchorMax = new Vector2(0.5f, 0.5f);
        spinRT.pivot     = new Vector2(0.5f, 0.5f);
        spinRT.anchoredPosition = Vector2.zero;
        spinRT.sizeDelta        = new Vector2(80f, 80f);
        var spinImg = spinnerGO.AddComponent<Image>();
        spinImg.sprite = sSpinner; spinImg.type = Image.Type.Simple; spinImg.preserveAspect = true;
        spinnerGO.SetActive(false);

        // ── ContentSheet (белый, скруглённый верх, заполняет остаток) ─────
        var contentSheet = MakeGO("ContentSheet", container.transform);
        SetLE(contentSheet, minH: 600, flexH: 1f);
        var csImg = contentSheet.AddComponent<Image>();
        csImg.color = C_BG;
        var csVLG = contentSheet.AddComponent<VerticalLayoutGroup>();
        csVLG.childAlignment         = TextAnchor.UpperCenter;
        csVLG.childForceExpandWidth  = true;
        csVLG.childForceExpandHeight = false;
        csVLG.childControlWidth = csVLG.childControlHeight = true;
        csVLG.padding = new RectOffset(36, 36, 36, 24);
        csVLG.spacing = 20;

        // QuestionText
        var qTMP = MakeTMP("QuestionText", contentSheet.transform, "Текст вопроса...", 44, C_TEXT, font);
        qTMP.enableWordWrapping = true;
        qTMP.alignment          = TextAlignmentOptions.Center;
        SetLE(qTMP.gameObject, minH: 100, flexH: 1f);

        // AnswersZone
        var answersZone = MakeGO("AnswersZone", contentSheet.transform);
        SetLE(answersZone, minH: 848);
        var aVLG = answersZone.AddComponent<VerticalLayoutGroup>();
        aVLG.childAlignment         = TextAnchor.UpperCenter;
        aVLG.childForceExpandWidth  = true;
        aVLG.childForceExpandHeight = false;
        aVLG.childControlWidth = aVLG.childControlHeight = true;
        aVLG.spacing = 16;

        string[] abcd = { "A", "B", "C", "D" };
        var answerBtns   = new Button[4];
        var answerLabels = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            var (aBtn, aBtnImg, aLbl) = SpawnAnswerButton(answersZone.transform, $"AnswerBtn_{abcd[i]}");
            SetLE(aBtn.gameObject, minH: 192, prefH: 200);
            aBtnImg.sprite = sBgGray;
            aBtnImg.type   = Image.Type.Simple;
            aBtnImg.color  = Color.white;
            aLbl.text      = $"{abcd[i]}: Вариант ответа";
            aLbl.fontSize  = 40;
            aLbl.alignment = TextAlignmentOptions.MidlineLeft;
            var aLblRT = aLbl.GetComponent<RectTransform>();
            aLblRT.offsetMin = new Vector2(32, 8); aLblRT.offsetMax = new Vector2(-32, -8);
            answerBtns[i]   = aBtn;
            answerLabels[i] = aLbl;
        }

        // ContinueSpacer (резервирует место для абсолютной кнопки)
        var spacerGO = MakeGO("ContinueSpacer", contentSheet.transform);
        SetLE(spacerGO, minH: 170, prefH: 170);

        // ── BtnClose (абсолютный, поверх фото, сверху-справа) ─────────────
        var btnCloseGO = MakeGO("BtnClose", panel.transform);
        var btnCloseRT = btnCloseGO.GetComponent<RectTransform>();
        btnCloseRT.anchorMin        = new Vector2(1f, 1f);
        btnCloseRT.anchorMax        = new Vector2(1f, 1f);
        btnCloseRT.pivot            = new Vector2(1f, 1f);
        btnCloseRT.anchoredPosition = new Vector2(-20f, -50f);
        btnCloseRT.sizeDelta        = new Vector2(110f, 110f);
        var btnCloseImg = btnCloseGO.AddComponent<Image>();
        if (sClose != null)
        {
            btnCloseImg.sprite = sClose; btnCloseImg.type = Image.Type.Simple;
            btnCloseImg.preserveAspect = true; btnCloseImg.color = Color.white;
            btnCloseImg.alphaHitTestMinimumThreshold = 0.1f;
        }
        else btnCloseImg.color = new Color(1f, 1f, 1f, 0.4f);
        var btnCloseBtn = btnCloseGO.AddComponent<Button>();
        btnCloseBtn.targetGraphic = btnCloseImg;
        btnCloseBtn.transition    = Selectable.Transition.None;
        btnCloseGO.AddComponent<ButtonSFX>();
        btnCloseGO.AddComponent<ButtonSpringAnim>();

        // ── BtnContinue (абсолютный, снизу по центру, выезжает снизу) ─────
        var btnContGO = MakePrimaryButton("BtnContinue", panel.transform, "Продолжить", font);
        AddLocKey(btnContGO, "btn_continue");
        var bcRT = btnContGO.GetComponent<RectTransform>();
        bcRT.anchorMin        = new Vector2(0.5f, 0f);
        bcRT.anchorMax        = new Vector2(0.5f, 0f);
        bcRT.pivot            = new Vector2(0.5f, 0f);
        bcRT.anchoredPosition = new Vector2(0f, 40f);
        bcRT.sizeDelta        = new Vector2(960f, 130f);
        ApplyHyperCasualButton(btnContGO, "Assets/Images/Sprites/buttons.png", "buttons_12", "buttons_13");
        btnContGO.AddComponent<UstAldanQuiz.UI.SlideInOnEnable>();
        btnContGO.SetActive(false);

        // ── Компонент ─────────────────────────────────────────────────────
        var qWinFull = root.AddComponent<UstAldanQuiz.UI.QuestionWindow>();
        var soQWF    = new UnityEditor.SerializedObject(qWinFull);
        Prop(soQWF, "panel",             panel);
        Prop(soQWF, "panelGroup",        panelCG);
        Prop(soQWF, "btnClose",          btnCloseBtn);
        Prop(soQWF, "questionText",      qTMP);
        Prop(soQWF, "mediaZone",         imageZone);
        Prop(soQWF, "questionImage",     qImg);
        Prop(soQWF, "imageAspectFitter", qImgARF);
        Prop(soQWF, "spinnerImage",      spinImg);
        Prop(soQWF, "btnContinue",       btnContGO.GetComponent<Button>());
        SetArr(soQWF, "answerButtons", answerBtns);
        SetArr(soQWF, "answerLabels",  answerLabels);
        soQWF.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, QuestionWindowPath);
        Selection.activeGameObject = null;
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
        var labelTMP = MakeTMP("Label", root.transform, "0 / 0", 30, Color.white, font, bold: true);
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
        Selection.activeGameObject = null;
        Object.DestroyImmediate(root);
        Debug.Log("[GameSceneBuilder] ✓ ProgressBar.prefab");
    }

    // =====================================================================
    // LetterTile.prefab  (буква в банке для режима «Составь слово»)
    // =====================================================================

    static void BuildLetterTilePrefab()
    {
        var font = FindFont();

        var root = new GameObject("LetterTile", typeof(RectTransform));
        root.AddComponent<CanvasGroup>();

        var bg  = root.AddComponent<Image>();
        bg.color = new Color(0.94f, 0.90f, 0.80f);

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.transition    = Selectable.Transition.None;
        root.AddComponent<ButtonSpringAnim>();

        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(root.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "А";
        tmp.fontSize  = 52;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Hex("1A2A1A");
        tmp.fontStyle = FontStyles.Bold;
        if (font != null) tmp.font = font;

        var tileUI = root.AddComponent<UstAldanQuiz.UI.LetterTileUI>();
        var soTile = new UnityEditor.SerializedObject(tileUI);
        Prop(soTile, "button",     btn);
        Prop(soTile, "label",      tmp);
        Prop(soTile, "background", bg);
        soTile.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, LetterTilePath);
        Selection.activeGameObject = null;
        Object.DestroyImmediate(root);
        Debug.Log("[GameSceneBuilder] ✓ LetterTile.prefab");
    }

    // =====================================================================
    // WordBuilderWindow.prefab  (полноэкранный, стиль Quizzland)
    // =====================================================================

    static void BuildWordBuilderWindowPrefab()
    {
        var font = FindFont();
        const string acAtlas = "Assets/Images/Sprites/additional controls.png";
        EnsureReadable(acAtlas);
        var sSpinner = LoadSprite(acAtlas, "additional controls_12");
        EnsureReadable("Assets/Images/Sprites/buttons.png");
        var sClose = LoadSprite("Assets/Images/Sprites/buttons.png", "buttons_10");

        var letterTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LetterTilePath);

        // Root (no visual — MonoBehaviour всегда активен для корутин)
        var root = new GameObject("WordBuilderWindow", typeof(RectTransform));

        // ── Panel (full screen, fade) ──────────────────────────────────────
        var panel   = MakeGO("Panel", root.transform);
        Stretch(panel);
        panel.AddComponent<Image>().color = C_BG;
        var panelCG = panel.AddComponent<CanvasGroup>();
        panel.SetActive(false);

        // Container (VLG)
        var container = MakeGO("Container", panel.transform);
        Stretch(container);
        var cVLG = container.AddComponent<VerticalLayoutGroup>();
        cVLG.childAlignment         = TextAnchor.UpperCenter;
        cVLG.childForceExpandWidth  = true;
        cVLG.childForceExpandHeight = false;
        cVLG.childControlWidth = cVLG.childControlHeight = true;
        cVLG.spacing = 0;

        // StatusBarCover
        var sbGO = MakeGO("StatusBarCover", container.transform);
        SetLE(sbGO, minH: 0, prefH: 0);
        sbGO.AddComponent<Image>().color = Color.black;
        sbGO.AddComponent<UstAldanQuiz.UI.StatusBarCover>();

        // ── ImageZone (1 фото, сверху) ─────────────────────────────────────
        var imageZone = MakeGO("ImageZone", container.transform);
        SetLE(imageZone, minH: 200, prefH: 460, flexH: 0f);
        imageZone.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);
        imageZone.SetActive(false);

        var qImgGO = MakeGO("QuestionImage", imageZone.transform);
        Stretch(qImgGO);
        var qImg    = qImgGO.AddComponent<RawImage>();
        qImg.raycastTarget = false;
        var qImgARF = qImgGO.AddComponent<AspectRatioFitter>();
        qImgARF.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        var spinnerGO = MakeGO("Spinner", imageZone.transform);
        var spinRT    = spinnerGO.GetComponent<RectTransform>();
        spinRT.anchorMin = spinRT.anchorMax = new Vector2(0.5f, 0.5f);
        spinRT.pivot     = new Vector2(0.5f, 0.5f);
        spinRT.anchoredPosition = Vector2.zero;
        spinRT.sizeDelta        = new Vector2(80f, 80f);
        var spinImg = spinnerGO.AddComponent<Image>();
        spinImg.sprite = sSpinner; spinImg.type = Image.Type.Simple; spinImg.preserveAspect = true;
        spinImg.raycastTarget = false;
        spinnerGO.SetActive(false);

        // Кнопка зума на самой imageZone (дети не перехватывают — raycastTarget=false)
        var imageZoneImg = imageZone.GetComponent<Image>();
        var imgClickBtn  = imageZone.AddComponent<Button>();
        imgClickBtn.targetGraphic = imageZoneImg;
        imgClickBtn.transition    = Selectable.Transition.None;

        // ── Zone_4Photo (2×2 сетка, сверху) ───────────────────────────────
        var zone4 = MakeGO("Zone_4Photo", container.transform);
        SetLE(zone4, minH: 200, prefH: 460, flexH: 0f);
        var zone4Grid = zone4.AddComponent<GridLayoutGroup>();
        zone4Grid.cellSize        = new Vector2(522, 220);
        zone4Grid.spacing         = new Vector2(8, 8);
        zone4Grid.padding         = new RectOffset(14, 14, 6, 6);
        zone4Grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        zone4Grid.constraintCount = 2;
        zone4Grid.childAlignment  = TextAnchor.MiddleCenter;
        zone4.SetActive(false);

        var photoImages = new RawImage[4];
        for (int i = 0; i < 4; i++)
        {
            var phGO  = MakeGO($"Photo_{i}", zone4.transform);
            var phImg = phGO.AddComponent<Image>();
            phImg.color = new Color(0.14f, 0.14f, 0.14f);
            var phBtn = phGO.AddComponent<Button>();
            phBtn.targetGraphic = phImg;
            phBtn.transition    = Selectable.Transition.None;

            var rawImgGO = MakeGO("RawImage", phGO.transform);
            Stretch(rawImgGO);
            photoImages[i] = rawImgGO.AddComponent<RawImage>();
            var arf = rawImgGO.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        }

        // ── ContentSheet (VLG, кремовый, заполняет остаток) ───────────────
        var contentSheet = MakeGO("ContentSheet", container.transform);
        SetLE(contentSheet, minH: 400, flexH: 1f);
        contentSheet.AddComponent<Image>().color = C_BG;
        var csVLG = contentSheet.AddComponent<VerticalLayoutGroup>();
        csVLG.childAlignment         = TextAnchor.UpperCenter;
        csVLG.childForceExpandWidth  = true;
        csVLG.childForceExpandHeight = false;
        csVLG.childControlWidth = csVLG.childControlHeight = true;
        csVLG.padding = new RectOffset(28, 28, 24, 16);
        csVLG.spacing = 16;

        // QuestionText
        var qTMP = MakeTMP("QuestionText", contentSheet.transform, "Составь слово...", 46, C_TEXT, font);
        qTMP.enableWordWrapping = true;
        qTMP.alignment          = TextAlignmentOptions.Center;
        SetLE(qTMP.gameObject, minH: 60, flexH: 0.3f);

        // Zone_Slots
        var zoneSlots = MakeGO("Zone_Slots", contentSheet.transform);
        SetLE(zoneSlots, minH: 100, prefH: 110);
        var slotsCSF = zoneSlots.AddComponent<ContentSizeFitter>();
        slotsCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var slotsGrid = zoneSlots.AddComponent<GridLayoutGroup>();
        slotsGrid.cellSize       = new Vector2(88, 88);
        slotsGrid.spacing        = new Vector2(10, 10);
        slotsGrid.childAlignment = TextAnchor.MiddleCenter;
        slotsGrid.constraint     = GridLayoutGroup.Constraint.Flexible;

        // Zone_Letters
        var zoneLetters = MakeGO("Zone_Letters", contentSheet.transform);
        SetLE(zoneLetters, minH: 100, prefH: 210, flexH: 0.5f);
        var lettersCSF = zoneLetters.AddComponent<ContentSizeFitter>();
        lettersCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var lettersGrid = zoneLetters.AddComponent<GridLayoutGroup>();
        lettersGrid.cellSize       = new Vector2(96, 96);
        lettersGrid.spacing        = new Vector2(12, 12);
        lettersGrid.childAlignment = TextAnchor.MiddleCenter;
        lettersGrid.constraint     = GridLayoutGroup.Constraint.Flexible;

        // ContinueSpacer
        var spacerGO = MakeGO("ContinueSpacer", contentSheet.transform);
        SetLE(spacerGO, minH: 170, prefH: 170);

        // ── BtnClose (абсолютный, top-right) ──────────────────────────────
        var btnCloseGO = MakeGO("BtnClose", panel.transform);
        var btnCloseRT = btnCloseGO.GetComponent<RectTransform>();
        btnCloseRT.anchorMin        = new Vector2(1f, 1f);
        btnCloseRT.anchorMax        = new Vector2(1f, 1f);
        btnCloseRT.pivot            = new Vector2(1f, 1f);
        btnCloseRT.anchoredPosition = new Vector2(-20f, -50f);
        btnCloseRT.sizeDelta        = new Vector2(110f, 110f);
        var btnCloseImg = btnCloseGO.AddComponent<Image>();
        if (sClose != null)
        {
            btnCloseImg.sprite = sClose; btnCloseImg.type = Image.Type.Simple;
            btnCloseImg.preserveAspect = true; btnCloseImg.color = Color.white;
            btnCloseImg.alphaHitTestMinimumThreshold = 0.1f;
        }
        else btnCloseImg.color = new Color(1f, 1f, 1f, 0.4f);
        var btnCloseBtn = btnCloseGO.AddComponent<Button>();
        btnCloseBtn.targetGraphic = btnCloseImg;
        btnCloseBtn.transition    = Selectable.Transition.None;
        btnCloseGO.AddComponent<ButtonSFX>();
        btnCloseGO.AddComponent<ButtonSpringAnim>();

        // ── BtnContinue (абсолютный, снизу, выезжает) ─────────────────────
        var btnContGO = MakePrimaryButton("BtnContinue", panel.transform, "Продолжить", font);
        AddLocKey(btnContGO, "btn_continue");
        var bcRT = btnContGO.GetComponent<RectTransform>();
        bcRT.anchorMin        = new Vector2(0.5f, 0f);
        bcRT.anchorMax        = new Vector2(0.5f, 0f);
        bcRT.pivot            = new Vector2(0.5f, 0f);
        bcRT.anchoredPosition = new Vector2(0f, 40f);
        bcRT.sizeDelta        = new Vector2(960f, 130f);
        ApplyHyperCasualButton(btnContGO, "Assets/Images/Sprites/buttons.png", "buttons_12", "buttons_13");
        btnContGO.AddComponent<UstAldanQuiz.UI.SlideInOnEnable>();
        btnContGO.SetActive(false);

        // ── ImageZoomOverlay ───────────────────────────────────────────────
        var zoomOverlayGO  = MakeGO("ImageZoomOverlay", panel.transform);
        Stretch(zoomOverlayGO);
        var zoomImg = zoomOverlayGO.AddComponent<Image>();
        zoomImg.color = new Color(0f, 0f, 0f, 0.92f);
        var zoomCG  = zoomOverlayGO.AddComponent<CanvasGroup>();
        var zoomBtn = zoomOverlayGO.AddComponent<Button>();
        zoomBtn.targetGraphic = zoomImg;
        zoomBtn.transition    = Selectable.Transition.None;
        zoomOverlayGO.SetActive(false);

        var zoomedImgGO = MakeGO("ZoomedImage", zoomOverlayGO.transform);
        var zoomedRT    = zoomedImgGO.GetComponent<RectTransform>();
        zoomedRT.anchorMin = new Vector2(0.02f, 0.05f);
        zoomedRT.anchorMax = new Vector2(0.98f, 0.95f);
        zoomedRT.offsetMin = zoomedRT.offsetMax = Vector2.zero;
        var zoomedRawImg = zoomedImgGO.AddComponent<RawImage>();
        var zoomedARF    = zoomedImgGO.AddComponent<AspectRatioFitter>();
        zoomedARF.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        // ── Компонент ──────────────────────────────────────────────────────
        var wbw  = root.AddComponent<UstAldanQuiz.UI.WordBuilderWindow>();
        var soWB = new UnityEditor.SerializedObject(wbw);
        Prop(soWB, "panel",             panel);
        Prop(soWB, "panelGroup",        panelCG);
        Prop(soWB, "btnClose",          btnCloseBtn);
        Prop(soWB, "imageZoomOverlay",   zoomOverlayGO);
        Prop(soWB, "zoomOverlayGroup",   zoomCG);
        Prop(soWB, "zoomedImage",        zoomedRawImg);
        Prop(soWB, "zoomedImageFitter",  zoomedARF);
        Prop(soWB, "questionText",      qTMP);
        Prop(soWB, "zone4Photo",        zone4);
        Prop(soWB, "mediaZone",         imageZone);
        Prop(soWB, "questionImage",       qImg);
        Prop(soWB, "imageAspectFitter",  qImgARF);
        Prop(soWB, "spinnerImage",       spinImg);
        Prop(soWB, "questionImageButton", imgClickBtn);
        Prop(soWB, "slotsContainer",    zoneSlots.GetComponent<RectTransform>());
        Prop(soWB, "lettersContainer",  zoneLetters.GetComponent<RectTransform>());
        if (letterTilePrefab != null)
            Prop(soWB, "letterTilePrefab", letterTilePrefab.GetComponent<UstAldanQuiz.UI.LetterTileUI>());
        Prop(soWB, "btnContinue",       btnContGO.GetComponent<Button>());
        SetArr(soWB, "photoImages", photoImages);
        soWB.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, WordBuilderWindowPath);
        Selection.activeGameObject = null;
        Object.DestroyImmediate(root);
        Debug.Log("[GameSceneBuilder] ✓ WordBuilderWindow.prefab");
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

        var factTextTMP = MakeTMP("FactText", card.transform, "", 40, C_TEXT, font);
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
        Selection.activeGameObject = null;
        Object.DestroyImmediate(root);
        Debug.Log("[GameSceneBuilder] ✓ FactPopup.prefab");
    }
}
