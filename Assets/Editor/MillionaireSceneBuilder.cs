using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;
using UstAldanQuiz.UI;

/// <summary>
/// Запуск: UstAldan Quiz → Build Millionaire UI
/// Создаёт полный UI в стиле «Кто хочет стать миллионером» в текущей сцене.
/// </summary>
public static class MillionaireSceneBuilder
{
    const float LADDER_W  = 220f;
    const float ROW_H     = 29f;
    const float ROW_GAP   = 2f;
    const float TITLE_H   = 32f;

    // -------------------------------------------------------------------------
    [MenuItem("UstAldan Quiz/Build Millionaire UI")]
    public static void Build()
    {
        var font = FindFont();

        // QuizManager — обычный root GameObject (не UI)
        var qmGO = FindRootOrCreate("QuizManager");
        var quizManager = GetOrAdd<QuizManager>(qmGO);

        // Canvas
        var canvasGO = SetupCanvas();
        HideOldUI(canvasGO);

        // Корневой экран (QuizScreenUI будет здесь)
        var screen = FindOrCreate("QuizScreen", canvasGO.transform);
        Stretch(screen);

        // Фон
        var bg = FindOrCreate("Background", screen.transform);
        Stretch(bg);
        GetOrAdd<Image>(bg).color = new Color(0.08f, 0.05f, 0.18f);

        // ===== ДЕНЕЖНАЯ ЛЕСЕНКА (левая панель) =====
        var ladderPanel = FindOrCreate("MoneyLadderPanel", screen.transform);
        var ladderRT    = GetOrAdd<RectTransform>(ladderPanel);
        ladderRT.anchorMin = new Vector2(0, 0);
        ladderRT.anchorMax = new Vector2(0, 1);
        ladderRT.pivot     = new Vector2(0, 0.5f);
        ladderRT.offsetMin = Vector2.zero;
        ladderRT.offsetMax = new Vector2(LADDER_W, 0);
        GetOrAdd<Image>(ladderPanel).color = new Color(0.05f, 0.04f, 0.22f);

        // Заголовок лесенки
        var ladderTitle = FindOrCreate("LadderTitle", ladderPanel.transform);
        PlaceRow(ladderTitle, 0f, TITLE_H);
        var ladderTitleTMP = GetOrAdd<TextMeshProUGUI>(ladderTitle);
        SetTMP(ladderTitleTMP, "Таблица призов", 13, font, new Color(1f, 0.85f, 0.3f),
               TextAlignmentOptions.Center);

        // 15 строк: Level_14 сверху (1 000 000₽) → Level_00 снизу (100₽)
        var levelBgs    = new Image[15];
        var levelLabels = new TextMeshProUGUI[15];

        for (int i = 14; i >= 0; i--)
        {
            float yFromTop = TITLE_H + (14 - i) * (ROW_H + ROW_GAP);
            var row = FindOrCreate($"Level_{i:D2}", ladderPanel.transform);
            PlaceRow(row, yFromTop, ROW_H);

            var rowImg  = GetOrAdd<Image>(row);
            rowImg.color = (i == 4 || i == 9)                    // несгораемые: Q5 и Q10
                ? new Color(0.10f, 0.45f, 0.10f)
                : new Color(0.12f, 0.12f, 0.35f);
            levelBgs[i] = rowImg;

            var labelGO = FindOrCreate("Label", row.transform);
            var labelRT = GetOrAdd<RectTransform>(labelGO);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(6,  1);
            labelRT.offsetMax = new Vector2(-6, -1);
            var labelTMP = GetOrAdd<TextMeshProUGUI>(labelGO);
            SetTMP(labelTMP, "—", 12, font, Color.white, TextAlignmentOptions.Right);
            labelTMP.overflowMode = TextOverflowModes.Overflow;
            levelLabels[i] = labelTMP;
        }

        // ===== ОСНОВНАЯ ОБЛАСТЬ (правее лесенки) =====
        var mainArea = FindOrCreate("MainArea", screen.transform);
        var mainRT = GetOrAdd<RectTransform>(mainArea);
        mainRT.anchorMin = Vector2.zero;
        mainRT.anchorMax = Vector2.one;
        mainRT.offsetMin = new Vector2(LADDER_W, 0);
        mainRT.offsetMax = Vector2.zero;

        var mainVLG = GetOrAdd<VerticalLayoutGroup>(mainArea);
        ConfigVLG(mainVLG, TextAnchor.UpperCenter, 8, new RectOffset(16, 16, 16, 16));

        // --- Верхняя строка (прогресс + призы) ---
        var topBar = FindOrCreate("TopBar", mainArea.transform);
        SetLE(topBar, minH: 52, prefH: 52);
        ConfigHLG(GetOrAdd<HorizontalLayoutGroup>(topBar), TextAnchor.MiddleLeft, 10);

        var progressText      = EnsureLabel("ProgressText",      topBar.transform, "Вопрос 1 из 15", 14, font, flexW: 1);
        progressText.alignment = TextAlignmentOptions.Left;

        var currentPrizeText  = EnsureLabel("CurrentPrizeText",  topBar.transform, "За этот вопрос: —", 14, font, flexW: 1);
        currentPrizeText.alignment = TextAlignmentOptions.Center;
        currentPrizeText.color = new Color(1f, 0.9f, 0.35f);

        var guaranteedPrizeText = EnsureLabel("GuaranteedPrizeText", topBar.transform, "Несгораемая: —", 14, font, flexW: 1);
        guaranteedPrizeText.alignment = TextAlignmentOptions.Right;
        guaranteedPrizeText.color = new Color(0.4f, 1f, 0.4f);

        // --- Панель вопроса ---
        var questionPanel = FindOrCreate("QuestionPanel", mainArea.transform);
        SetLE(questionPanel, minH: 175, prefH: 175);
        GetOrAdd<Image>(questionPanel).color = new Color(0.12f, 0.08f, 0.35f);

        var imgContainer = FindOrCreate("QuestionImageContainer", questionPanel.transform);
        var icRT = GetOrAdd<RectTransform>(imgContainer);
        icRT.anchorMin = new Vector2(0, 0); icRT.anchorMax = new Vector2(0, 1);
        icRT.pivot     = new Vector2(0, 0.5f);
        icRT.offsetMin = new Vector2(10, 10); icRT.offsetMax = new Vector2(150, -10);
        imgContainer.SetActive(false);

        var qImgGO = FindOrCreate("QuestionImage", imgContainer.transform);
        Stretch(qImgGO);
        var questionImage = GetOrAdd<Image>(qImgGO);

        var questionTextGO = FindOrCreate("QuestionText", questionPanel.transform);
        var qtRT = GetOrAdd<RectTransform>(questionTextGO);
        qtRT.anchorMin = Vector2.zero; qtRT.anchorMax = Vector2.one;
        qtRT.offsetMin = new Vector2(20, 12); qtRT.offsetMax = new Vector2(-20, -12);
        var questionTMP = GetOrAdd<TextMeshProUGUI>(questionTextGO);
        questionTMP.text = "Текст вопроса будет здесь...";
        questionTMP.fontSize = 19;
        questionTMP.color = Color.white;
        questionTMP.alignment = TextAlignmentOptions.Center;
        questionTMP.enableWordWrapping = true;
        if (font != null) questionTMP.font = font;

        // --- Кнопки ответов ---
        var answersPanel = FindOrCreate("AnswersPanel", mainArea.transform);
        SetLE(answersPanel, minH: 288, prefH: 288);
        ConfigVLG(GetOrAdd<VerticalLayoutGroup>(answersPanel), TextAnchor.UpperCenter, 6,
                  new RectOffset(0, 0, 0, 0));

        var answerButtons = new Button[4];
        var answerLabels  = new TextMeshProUGUI[4];
        string[] prefixes = { "A", "B", "C", "D" };

        for (int i = 0; i < 4; i++)
        {
            var (btn, lbl) = CreateAnswerButton($"AnswerButton{i}",
                answersPanel.transform, $"{prefixes[i]}: Вариант ответа", font);
            answerButtons[i] = btn;
            answerLabels[i]  = lbl;
        }

        // --- Нижняя строка (подсказки + забрать деньги) ---
        var bottomBar = FindOrCreate("BottomBar", mainArea.transform);
        SetLE(bottomBar, minH: 64, prefH: 64);
        ConfigHLG(GetOrAdd<HorizontalLayoutGroup>(bottomBar),
                  TextAnchor.MiddleCenter, 10, new RectOffset(8, 8, 6, 6));

        var fiftyBtn    = CreateLifelineBtn("FiftyFiftyButton",   bottomBar.transform,
                            "50:50",         new Color(0.90f, 0.60f, 0.10f), font);
        var audienceBtn = CreateLifelineBtn("AudienceHelpButton", bottomBar.transform,
                            "Помощь\nзала",  new Color(0.20f, 0.50f, 0.90f), font);
        var phoneBtn    = CreateLifelineBtn("PhoneFriendButton",  bottomBar.transform,
                            "Звонок\nдругу", new Color(0.20f, 0.75f, 0.40f), font);

        // Распорка
        GetOrAdd<LayoutElement>(FindOrCreate("Spacer", bottomBar.transform)).flexibleWidth = 1;

        var walkAwayBtn = CreateLifelineBtn("WalkAwayButton", bottomBar.transform,
                            "Забрать\nденьги", new Color(0.85f, 0.20f, 0.20f), font);

        // ===== ОВЕРЛЕИ =====
        var (audPanel, audBars, audPercents, closeAudBtn) = CreateAudiencePanel(screen.transform, font);
        var (phonePanel, phoneTxt, closePhoneBtn)          = CreatePhonePanel(screen.transform, font);
        var (resultPanel, resultTitle, resultPrize, playBtn) = CreateResultPanel(screen.transform, font);

        // ===== ПРИВЯЗКА КОМПОНЕНТОВ =====

        // MoneyLadderUI
        var mlUI = GetOrAdd<MoneyLadderUI>(ladderPanel);
        WireMoneyLadderUI(mlUI, quizManager, levelBgs, levelLabels);

        // QuizScreenUI
        var screenUI = GetOrAdd<QuizScreenUI>(screen);
        WireScreenUI(screenUI, quizManager,
            questionTMP, questionImage, imgContainer,
            progressText, answerButtons, answerLabels,
            currentPrizeText, guaranteedPrizeText,
            fiftyBtn, audienceBtn, phoneBtn, walkAwayBtn,
            audPanel, audBars, audPercents, closeAudBtn,
            phonePanel, phoneTxt, closePhoneBtn,
            resultPanel, resultTitle, resultPrize, playBtn);

        // QuizManager
        WireQuizManager(quizManager);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[MillionaireSceneBuilder] ✓ UI построен! Нажми Ctrl+S для сохранения.");
    }

    // =========================================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ — ОБЪЕКТЫ
    // =========================================================================

    static TMP_FontAsset FindFont()
    {
        // Используем шрифт из Resources/Fonts & Materials/ — полный атлас со всеми служебными глифами
        const string path = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font != null) return font;
        return TMP_Settings.defaultFontAsset;
    }

    static GameObject SetupCanvas()
    {
        var go = GameObject.Find("Canvas") ?? new GameObject("Canvas");
        var canvas  = GetOrAdd<Canvas>(go);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = GetOrAdd<CanvasScaler>(go);
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1280, 720);
        scaler.matchWidthOrHeight   = 0.5f;

        GetOrAdd<GraphicRaycaster>(go);
        return go;
    }

    static void HideOldUI(GameObject canvasGO)
    {
        string[] legacy = {
            "AnswerButton1","AnswerButton2","AnswerButton3","AnswerButton4",
            "QuestionText","QuestionImageContainer","ProgressText"
        };
        foreach (var n in legacy)
        {
            var t = canvasGO.transform.Find(n);
            if (t != null)
            {
                t.gameObject.SetActive(false);
                t.gameObject.name = "[OLD] " + t.gameObject.name;
            }
        }
    }

    static GameObject FindRootOrCreate(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go;
    }

    static GameObject FindOrCreate(string name, Transform parent)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component =>
        go.GetComponent<T>() ?? go.AddComponent<T>();

    static void Stretch(GameObject go)
    {
        var rt = GetOrAdd<RectTransform>(go);
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    // Прикрепить к верхнему краю родителя, отступ yFromTop сверху, высота height
    static void PlaceRow(GameObject go, float yFromTop, float height)
    {
        var rt = GetOrAdd<RectTransform>(go);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -yFromTop);
        rt.sizeDelta        = new Vector2(0, height);
    }

    // =========================================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ — КОМПОНЕНТЫ
    // =========================================================================

    static void ConfigVLG(VerticalLayoutGroup vlg, TextAnchor anchor, float spacing,
                          RectOffset padding, bool expandW = true, bool expandH = false)
    {
        vlg.childAlignment      = anchor;
        vlg.spacing             = spacing;
        vlg.padding             = padding;
        vlg.childForceExpandWidth  = expandW;
        vlg.childForceExpandHeight = expandH;
        vlg.childControlWidth   = true;
        vlg.childControlHeight  = true;
    }

    static void ConfigHLG(HorizontalLayoutGroup hlg, TextAnchor anchor, float spacing,
                          RectOffset padding = null)
    {
        hlg.childAlignment         = anchor;
        hlg.spacing                = spacing;
        hlg.padding                = padding ?? new RectOffset(0,0,0,0);
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
    }

    static void SetLE(GameObject go, float minH = 0, float prefH = 0, float flexW = 0)
    {
        var le = GetOrAdd<LayoutElement>(go);
        if (minH  > 0) le.minHeight       = minH;
        if (prefH > 0) le.preferredHeight = prefH;
        if (flexW > 0) le.flexibleWidth   = flexW;
    }

    static void SetTMP(TextMeshProUGUI tmp, string text, float size, TMP_FontAsset font,
                       Color color, TextAlignmentOptions align)
    {
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = false;
        if (font != null) tmp.font = font;
    }

    static TextMeshProUGUI EnsureLabel(string name, Transform parent, string text,
                                       float size, TMP_FontAsset font,
                                       float minW = 0, float flexW = 0)
    {
        var go  = FindOrCreate(name, parent);
        var le  = GetOrAdd<LayoutElement>(go);
        if (minW  > 0) le.minWidth      = minW;
        if (flexW > 0) le.flexibleWidth = flexW;
        var tmp = GetOrAdd<TextMeshProUGUI>(go);
        tmp.text               = text;
        tmp.fontSize           = size;
        tmp.color              = Color.white;
        tmp.enableWordWrapping = false;
        tmp.overflowMode       = TextOverflowModes.Overflow;
        if (font != null) tmp.font = font;
        return tmp;
    }

    static (Button btn, TextMeshProUGUI lbl) CreateAnswerButton(
        string name, Transform parent, string text, TMP_FontAsset font)
    {
        var go = FindOrCreate(name, parent);
        SetLE(go, minH: 62, prefH: 62);

        var img = GetOrAdd<Image>(go);
        img.color  = new Color(0.10f, 0.08f, 0.32f);
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type   = Image.Type.Sliced;

        var btn = GetOrAdd<Button>(go);
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.10f, 0.08f, 0.32f);
        cb.highlightedColor = new Color(0.20f, 0.16f, 0.50f);
        cb.pressedColor     = new Color(0.05f, 0.04f, 0.20f);
        btn.colors = cb;

        var textGO = FindOrCreate("Text", go.transform);
        var textRT = GetOrAdd<RectTransform>(textGO);
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(14, 0); textRT.offsetMax = new Vector2(-14, 0);

        var tmp = GetOrAdd<TextMeshProUGUI>(textGO);
        tmp.text     = text;
        tmp.fontSize = 16;
        tmp.color    = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        if (font != null) tmp.font = font;

        return (btn, tmp);
    }

    static Button CreateLifelineBtn(string name, Transform parent,
                                    string text, Color color, TMP_FontAsset font)
    {
        var go = FindOrCreate(name, parent);
        var le = GetOrAdd<LayoutElement>(go);
        le.minWidth = le.preferredWidth   = 120;
        le.minHeight = le.preferredHeight = 54;

        var img = GetOrAdd<Image>(go);
        img.color  = color;
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type   = Image.Type.Sliced;

        var btn = GetOrAdd<Button>(go);
        btn.targetGraphic = img;

        var textGO = FindOrCreate("Text", go.transform);
        var textRT = GetOrAdd<RectTransform>(textGO);
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        var tmp = GetOrAdd<TextMeshProUGUI>(textGO);
        tmp.text     = text;
        tmp.fontSize = 13;
        tmp.color    = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        if (font != null) tmp.font = font;

        return btn;
    }

    // =========================================================================
    // ОВЕРЛЕЙ — ПОМОЩЬ ЗАЛА
    // =========================================================================

    static (GameObject panel, Image[] bars, TextMeshProUGUI[] percents, Button closeBtn)
        CreateAudiencePanel(Transform parent, TMP_FontAsset font)
    {
        var overlay = FindOrCreate("AudienceHelpPanel", parent);
        Stretch(overlay);
        GetOrAdd<Image>(overlay).color = new Color(0, 0, 0, 0.72f);
        overlay.SetActive(false);

        var box   = FindOrCreate("Box", overlay.transform);
        var boxRT = GetOrAdd<RectTransform>(box);
        boxRT.anchorMin = boxRT.anchorMax = boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(600, 330);
        GetOrAdd<Image>(box).color = new Color(0.11f, 0.07f, 0.30f);

        var vlg = GetOrAdd<VerticalLayoutGroup>(box);
        ConfigVLG(vlg, TextAnchor.UpperCenter, 8, new RectOffset(22, 22, 18, 18));

        var titleTMP = EnsureLabel("Title", box.transform, "Помощь зала", 22, font);
        titleTMP.color = new Color(1f, 0.85f, 0.3f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        SetLE(titleTMP.gameObject, minH: 38);

        var bars     = new Image[4];
        var percents = new TextMeshProUGUI[4];
        string[] opts = { "A", "B", "C", "D" };
        Color[] barColors = {
            new Color(0.20f, 0.50f, 0.90f), new Color(0.20f, 0.70f, 0.40f),
            new Color(0.90f, 0.50f, 0.10f), new Color(0.80f, 0.20f, 0.30f)
        };

        for (int i = 0; i < 4; i++)
        {
            var row    = FindOrCreate($"BarRow_{i}", box.transform);
            SetLE(row, minH: 38);
            ConfigHLG(GetOrAdd<HorizontalLayoutGroup>(row), TextAnchor.MiddleLeft, 8);

            var optLbl = EnsureLabel("OptionLabel", row.transform, $"{opts[i]}:", 16, font, minW: 30);
            optLbl.alignment = TextAlignmentOptions.Right;

            var barBgGO = FindOrCreate("BarBackground", row.transform);
            SetLE(barBgGO, flexW: 1);
            GetOrAdd<Image>(barBgGO).color = new Color(0.18f, 0.18f, 0.18f);

            var fillGO = FindOrCreate("BarFill", barBgGO.transform);
            var fillRT = GetOrAdd<RectTransform>(fillGO);
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            var fillImg    = GetOrAdd<Image>(fillGO);
            fillImg.color  = barColors[i];
            fillImg.type   = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 0;
            bars[i] = fillImg;

            var pctTMP = EnsureLabel("PercentText", row.transform, "0%", 16, font, minW: 55);
            pctTMP.alignment = TextAlignmentOptions.Left;
            percents[i] = pctTMP;
        }

        var closeBtn = CreateLifelineBtn("CloseButton", box.transform, "Закрыть",
                           new Color(0.35f, 0.35f, 0.35f), font);
        var closeBtnLE = GetOrAdd<LayoutElement>(closeBtn.gameObject);
        closeBtnLE.minHeight = closeBtnLE.preferredHeight = 44;
        closeBtnLE.minWidth  = closeBtnLE.preferredWidth  = 130;

        return (overlay, bars, percents, closeBtn);
    }

    // =========================================================================
    // ОВЕРЛЕЙ — ЗВОНОК ДРУГУ
    // =========================================================================

    static (GameObject panel, TextMeshProUGUI hintText, Button closeBtn)
        CreatePhonePanel(Transform parent, TMP_FontAsset font)
    {
        var overlay = FindOrCreate("PhoneFriendPanel", parent);
        Stretch(overlay);
        GetOrAdd<Image>(overlay).color = new Color(0, 0, 0, 0.72f);
        overlay.SetActive(false);

        var box   = FindOrCreate("Box", overlay.transform);
        var boxRT = GetOrAdd<RectTransform>(box);
        boxRT.anchorMin = boxRT.anchorMax = boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(500, 240);
        GetOrAdd<Image>(box).color = new Color(0.11f, 0.07f, 0.30f);

        var vlg = GetOrAdd<VerticalLayoutGroup>(box);
        ConfigVLG(vlg, TextAnchor.UpperCenter, 12, new RectOffset(24, 24, 20, 20));

        var titleTMP = EnsureLabel("Title", box.transform, "Звонок другу", 22, font);
        titleTMP.color = new Color(1f, 0.85f, 0.3f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        SetLE(titleTMP.gameObject, minH: 38);

        var hintGO = FindOrCreate("HintText", box.transform);
        SetLE(hintGO, minH: 80);
        var hintTMP = GetOrAdd<TextMeshProUGUI>(hintGO);
        hintTMP.text  = "Подсказка друга появится здесь...";
        hintTMP.fontSize = 15;
        hintTMP.color = Color.white;
        hintTMP.alignment = TextAlignmentOptions.Center;
        hintTMP.enableWordWrapping = true;
        if (font != null) hintTMP.font = font;

        var closeBtn = CreateLifelineBtn("CloseButton", box.transform, "Закрыть",
                           new Color(0.35f, 0.35f, 0.35f), font);
        SetLE(closeBtn.gameObject, minH: 44);

        return (overlay, hintTMP, closeBtn);
    }

    // =========================================================================
    // ОВЕРЛЕЙ — ЭКРАН РЕЗУЛЬТАТА
    // =========================================================================

    static (GameObject panel, TextMeshProUGUI title, TextMeshProUGUI prize, Button playAgain)
        CreateResultPanel(Transform parent, TMP_FontAsset font)
    {
        var overlay = FindOrCreate("ResultPanel", parent);
        Stretch(overlay);
        GetOrAdd<Image>(overlay).color = new Color(0, 0, 0, 0.86f);
        overlay.SetActive(false);

        var box   = FindOrCreate("Box", overlay.transform);
        var boxRT = GetOrAdd<RectTransform>(box);
        boxRT.anchorMin = boxRT.anchorMax = boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(560, 280);
        GetOrAdd<Image>(box).color = new Color(0.08f, 0.05f, 0.24f);

        var vlg = GetOrAdd<VerticalLayoutGroup>(box);
        ConfigVLG(vlg, TextAnchor.UpperCenter, 18, new RectOffset(32, 32, 30, 30));

        var titleTMP = EnsureLabel("ResultTitle", box.transform, "Результат", 28, font);
        titleTMP.color = new Color(1f, 0.85f, 0.3f);
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        SetLE(titleTMP.gameObject, minH: 48);

        var prizeTMP = EnsureLabel("ResultPrize", box.transform, "Ваш выигрыш: 0 ₽", 22, font);
        prizeTMP.alignment = TextAlignmentOptions.Center;
        SetLE(prizeTMP.gameObject, minH: 48);

        var playBtn = CreateLifelineBtn("PlayAgainButton", box.transform, "Играть снова",
                          new Color(0.20f, 0.60f, 0.20f), font);
        var playLE = GetOrAdd<LayoutElement>(playBtn.gameObject);
        playLE.minHeight = playLE.preferredHeight = 54;
        playLE.minWidth  = playLE.preferredWidth  = 180;

        return (overlay, titleTMP, prizeTMP, playBtn);
    }

    // =========================================================================
    // ПРИВЯЗКА ССЫЛОК (SerializedObject API)
    // =========================================================================

    static void WireScreenUI(
        QuizScreenUI ui, QuizManager qm,
        TextMeshProUGUI questionText, Image questionImage, GameObject questionImageContainer,
        TextMeshProUGUI progressText, Button[] answerButtons, TextMeshProUGUI[] answerLabels,
        TextMeshProUGUI currentPrizeText, TextMeshProUGUI guaranteedPrizeText,
        Button fiftyFifty, Button audience, Button phone, Button walkAway,
        GameObject audPanel, Image[] audBars, TextMeshProUGUI[] audPercents, Button closeAud,
        GameObject phonePanel, TextMeshProUGUI phoneText, Button closePhone,
        GameObject resultPanel, TextMeshProUGUI resultTitle, TextMeshProUGUI resultPrize,
        Button playAgain)
    {
        var so = new SerializedObject(ui);

        Prop(so, "quizManager",              qm);
        Prop(so, "questionText",             questionText);
        Prop(so, "questionImage",            questionImage);
        Prop(so, "questionImageContainer",   questionImageContainer);
        Prop(so, "progressText",             progressText);
        Prop(so, "currentPrizeText",         currentPrizeText);
        Prop(so, "guaranteedPrizeText",      guaranteedPrizeText);
        Prop(so, "fiftyFiftyButton",         fiftyFifty);
        Prop(so, "audienceHelpButton",       audience);
        Prop(so, "phoneFriendButton",        phone);
        Prop(so, "walkAwayButton",           walkAway);
        Prop(so, "audienceHelpPanel",        audPanel);
        Prop(so, "closeAudienceButton",      closeAud);
        Prop(so, "phoneFriendPanel",         phonePanel);
        Prop(so, "phoneFriendText",          phoneText);
        Prop(so, "closePhoneButton",         closePhone);
        Prop(so, "resultPanel",              resultPanel);
        Prop(so, "resultTitleText",          resultTitle);
        Prop(so, "resultPrizeText",          resultPrize);
        Prop(so, "playAgainButton",          playAgain);

        SetArr(so, "answerButtons",        answerButtons);
        SetArr(so, "answerLabels",         answerLabels);
        SetArr(so, "audienceBars",         audBars);
        SetArr(so, "audiencePercentLabels", audPercents);

        so.ApplyModifiedProperties();
    }

    static void WireMoneyLadderUI(MoneyLadderUI ui, QuizManager qm,
                                  Image[] levelBgs, TextMeshProUGUI[] levelLabels)
    {
        var so = new SerializedObject(ui);
        Prop(so, "quizManager", qm);

        var ml = GetOrCreateMoneyLadder();
        Prop(so, "moneyLadder", ml);

        SetArr(so, "levelBackgrounds", levelBgs);
        SetArr(so, "levelLabels",      levelLabels);

        so.ApplyModifiedProperties();
    }

    static void WireQuizManager(QuizManager qm)
    {
        var so = new SerializedObject(qm);

        var dbGuids = AssetDatabase.FindAssets("t:QuestionDatabase");
        if (dbGuids.Length > 0)
            Prop(so, "database", AssetDatabase.LoadAssetAtPath<QuestionDatabase>(
                AssetDatabase.GUIDToAssetPath(dbGuids[0])));
        else
            Debug.LogWarning("[Builder] QuestionDatabase не найден — назначь вручную.");

        Prop(so, "moneyLadder", GetOrCreateMoneyLadder());
        so.ApplyModifiedProperties();
    }

    static MoneyLadder GetOrCreateMoneyLadder()
    {
        // Всегда пересоздаём, чтобы метки подхватывали актуальные дефолты из MoneyLadder.cs
        var guids = AssetDatabase.FindAssets("t:MoneyLadder");
        if (guids.Length > 0)
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guids[0]));

        System.IO.Directory.CreateDirectory("Assets/Data");
        var ml = ScriptableObject.CreateInstance<MoneyLadder>();
        AssetDatabase.CreateAsset(ml, "Assets/Data/MoneyLadder.asset");
        AssetDatabase.SaveAssets();
        return ml;
    }

    // Установить object reference через SerializedObject
    static void Prop(SerializedObject so, string name, UnityEngine.Object value)
    {
        var p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"[Builder] Поле '{name}' не найдено"); return; }
        p.objectReferenceValue = value;
    }

    // Установить массив object references
    static void SetArr<T>(SerializedObject so, string name, T[] values) where T : UnityEngine.Object
    {
        var p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"[Builder] Массив '{name}' не найдено"); return; }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
