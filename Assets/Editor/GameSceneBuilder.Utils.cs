using System.IO;
using System.Linq;
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
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ — ОБЪЕКТЫ
    // =====================================================================

    static UnityEngine.SceneManagement.Scene OpenOrCreateScene(string path)
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        Selection.objects = new Object[0];

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(newScene, path);
        return EditorSceneManager.OpenScene(path);
    }

    static void SaveScene(string path)
    {
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
    }

    static GameObject SetupCanvas(string sceneName)
    {
        var go     = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static void SetupCamera()
    {
        var camGO  = new GameObject("Main Camera");
        camGO.tag  = "MainCamera";
        var cam    = camGO.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = Hex("F5F0E8");
        cam.cullingMask     = 0;
        cam.depth           = -1;
        camGO.AddComponent<AudioListener>();
    }

    static void SetupEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject MakeRootGO(string name)
    {
        return new GameObject(name);
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetLE(GameObject go, float minH = 0, float minW = 0, float prefH = 0, float flexH = -1f, float flexW = -1f)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        if (minH  > 0)  le.minHeight       = minH;
        if (minW  > 0)  le.minWidth        = minW;
        if (prefH > 0)  le.preferredHeight = prefH;
        if (flexH >= 0) le.flexibleHeight  = flexH;
        if (flexW >= 0) le.flexibleWidth   = flexW;
    }

    static TMP_Text MakeTMP(string name, Transform parent, string text, float size,
                            Color color, TMP_FontAsset font, float minH = 0, bool bold = false)
    {
        var go  = MakeGO(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.fontSize           = size;
        tmp.color              = color;
        tmp.enableWordWrapping = false;
        tmp.overflowMode       = TextOverflowModes.Overflow;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        if (font != null) tmp.font = font;
        if (minH > 0) SetLE(go, minH: minH);
        return tmp;
    }

    // Кнопка главного стиля (зелёный фон, белый текст)
    static GameObject MakePrimaryButton(string name, Transform parent, string text,
                                        TMP_FontAsset font, float minH = 110, float minW = 0)
    {
        var go  = MakeGO(name, parent);
        SetLE(go, minH: minH, minW: minW);
        var img   = go.AddComponent<Image>();
        img.color  = C_BTN_PRI;
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type   = Image.Type.Sliced;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors; cb.highlightedColor = new Color(0.22f, 0.50f, 0.32f); btn.colors = cb;
        go.AddComponent<ButtonSFX>();
        var lbl = MakeGO("Text", go.transform);
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(16, 0); lRT.offsetMax = new Vector2(-16, 0);
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 36; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        if (font != null) tmp.font = font;
        return go;
    }

    // Кнопка вторичного стиля (белый фон, зелёный текст)
    static GameObject MakeSecondaryButton(string name, Transform parent, string text,
                                          TMP_FontAsset font, float minH = 100, float minW = 0)
    {
        var go  = MakeGO(name, parent);
        SetLE(go, minH: minH, minW: minW);
        var img   = go.AddComponent<Image>();
        img.color  = C_BTN_SEC;
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type   = Image.Type.Sliced;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        go.AddComponent<ButtonSFX>();
        var lbl = MakeGO("Text", go.transform);
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(16, 0); lRT.offsetMax = new Vector2(-16, 0);
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 34; tmp.color = C_PRIMARY;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
        return go;
    }

    // Кнопка ответа (белый фон, тёмный текст)
    static (GameObject go, TMP_Text lbl) MakeAnswerButton(string name, Transform parent,
                                                           string text, TMP_FontAsset font)
    {
        var go  = MakeGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color  = Color.white;
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type   = Image.Type.Sliced;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        var cb = btn.colors;
        cb.highlightedColor = new Color(0.90f, 0.95f, 0.90f);
        cb.pressedColor     = new Color(0.80f, 0.90f, 0.80f);
        btn.colors = cb;

        var txtGO = MakeGO("Text", go.transform);
        var tRT   = txtGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(20, 0); tRT.offsetMax = new Vector2(-20, 0);
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 30; tmp.color = C_TEXT;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        if (font != null) tmp.font = font;
        return (go, tmp);
    }

    // Строка настройки: Label слева + Toggle-пилюля справа
    static Toggle MakeSettingRow(string label, Transform parent, TMP_FontAsset font, string locKey = null)
    {
        var row = MakeGO(label.Replace(" ", "") + "Row", parent);
        SetLE(row, minH: 96, prefH: 96);
        row.AddComponent<Image>().color = Color.clear;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = hlg.childControlHeight = true;
        hlg.padding = new RectOffset(48, 48, 0, 0);
        hlg.spacing = 24;

        var nameTMP = MakeTMP("Label", row.transform, label, 40, C_TEXT, font);
        SetLE(nameTMP.gameObject, flexW: 1f);
        if (locKey != null) AddLocKey(nameTMP.gameObject, locKey);

        // Пилюля-переключатель
        var pill = MakeGO("Toggle", row.transform);
        SetLE(pill, minW: 160, minH: 60);
        var pillImg = pill.AddComponent<Image>();
        pillImg.color  = C_PRIMARY;
        pillImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        pillImg.type   = Image.Type.Sliced;

        var toggle = pill.AddComponent<Toggle>();
        toggle.targetGraphic = pillImg;
        toggle.graphic       = null;
        toggle.isOn          = true;

        var pillLbl = MakeTMP("Label", pill.transform, "Вкл", 32, Color.white, font, bold: true);
        var pillRT  = pillLbl.GetComponent<RectTransform>();
        pillRT.anchorMin = Vector2.zero; pillRT.anchorMax = Vector2.one;
        pillRT.offsetMin = pillRT.offsetMax = Vector2.zero;
        pillLbl.alignment = TextAlignmentOptions.Center;

        return toggle;
    }

    // Строка с иконкой-переключателем + ползунком на одном уровне
    static (Toggle toggle, Slider slider) MakeIconSliderRow(
        string rowName, string spriteOnName, string spriteOffName,
        Transform parent, TMP_FontAsset font)
    {
        const string atlas = "Assets/Images/Sprites/buttons.png";
        EnsureReadable(atlas);
        var sOn  = LoadSprite(atlas, spriteOnName);
        var sOff = LoadSprite(atlas, spriteOffName);

        var row = MakeGO(rowName + "Row", parent);
        SetLE(row, minH: 96, prefH: 96);
        row.AddComponent<Image>().color = Color.clear;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth  = false;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.padding = new RectOffset(48, 48, 0, 0);
        hlg.spacing = 24;

        // Иконка-переключатель
        var iconGO  = MakeGO(rowName + "Toggle", row.transform);
        SetLE(iconGO, minW: 160, minH: 160);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color = Color.clear;
        var toggle = iconGO.AddComponent<Toggle>();
        toggle.targetGraphic = iconImg;
        toggle.transition    = Selectable.Transition.None;
        toggle.isOn          = true;

        Image MakeSpriteChild(string goName, Sprite sprite, bool active)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(iconGO.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = sprite; img.type = Image.Type.Simple;
            img.preserveAspect = true; img.color = Color.white;
            go.SetActive(active);
            return img;
        }
        MakeSpriteChild("SpriteOn",  sOn,  true);
        MakeSpriteChild("SpriteOff", sOff, false);

        // Ползунок (без надписи)
        const string acAtlas = "Assets/Images/Sprites/additional controls.png";
        EnsureReadable(acAtlas);
        var sBgSlider   = LoadSprite(acAtlas, "additional controls_3");
        var sFillSlider = LoadSprite(acAtlas, "additional controls_9");
        var sHandle     = LoadSprite(acAtlas, "additional controls_11");

        float handleH   = sHandle   != null ? (sHandle.rect.height / 2f)   : 44f;
        float handleW   = sHandle   != null ? (sHandle.rect.width / 2f)   : 44f;
        float sliderH   = sBgSlider != null ? (sBgSlider.rect.height / 2f) : 44f;

        var sliderGO = MakeGO(rowName + "Slider", row.transform);
        SetLE(sliderGO, minH: sliderH, prefH: sliderH, flexW: 1f);

        var bgGO  = MakeGO("Background", sliderGO.transform);
        var bgRT  = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = sBgSlider; bgImg.type = Image.Type.Simple;
        bgImg.preserveAspect = false; bgImg.color = Color.white;

        var fillAreaGO = MakeGO("Fill Area", sliderGO.transform);
        var faRT = fillAreaGO.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = faRT.offsetMax = Vector2.zero;
        var fillGO = MakeGO("Fill", fillAreaGO.transform);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(1f, 1f);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.sprite = sFillSlider; fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f; fillImg.color = Color.white;

        var handleAreaGO = MakeGO("Handle Slide Area", sliderGO.transform);
        var haRT = handleAreaGO.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = haRT.offsetMax = Vector2.zero;
        var handleGO = MakeGO("Handle", handleAreaGO.transform);
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = handleRT.anchorMax = new Vector2(0f, 0.5f);
        handleRT.pivot     = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(handleW, handleH);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.sprite = sHandle; handleImg.type = Image.Type.Simple;
        handleImg.preserveAspect = true; handleImg.color = Color.white;

        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect      = fillRT;
        slider.handleRect    = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;

        return (toggle, slider);
    }

    // Строка «Подпись + горизонтальный ползунок»
    static Slider MakeHSliderRow(string name, string labelText, Transform parent, TMP_FontAsset font)
    {
        var row = MakeGO(name, parent);
        SetLE(row, minH: 88, prefH: 88);
        var rowLE = row.GetComponent<LayoutElement>();
        rowLE.flexibleHeight = 0;
        rowLE.layoutPriority = 2;
        row.AddComponent<Image>().color = Color.clear;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.padding = new RectOffset(16, 16, 0, 0);
        hlg.spacing = 20;

        var lbl = MakeTMP("Label", row.transform, labelText, 36, C_TEXT, font, minH: 36);
        SetLE(lbl.gameObject, minW: 160);
        lbl.alignment = TextAlignmentOptions.MidlineLeft;

        const string acAtlas = "Assets/Images/Sprites/additional controls.png";
        EnsureReadable(acAtlas);
        var sBg     = LoadSprite(acAtlas, "additional controls_3");
        var sFill   = LoadSprite(acAtlas, "additional controls_9");
        var sHandle = LoadSprite(acAtlas, "additional controls_11");

        float handleH = sHandle != null ? sHandle.rect.height / 2f : 44f;
        float handleW = sHandle != null ? sHandle.rect.width  / 2f : 44f;
        float sliderH = sBg     != null ? sBg.rect.height     / 2f : 44f;

        var sliderGO = MakeGO(name + "Slider", row.transform);
        SetLE(sliderGO, minH: sliderH, prefH: sliderH, flexW: 1f);

        var bgGO = MakeGO("Background", sliderGO.transform);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = sBg; bgImg.type = Image.Type.Simple;
        bgImg.preserveAspect = false; bgImg.color = Color.white;

        var fillAreaGO = MakeGO("Fill Area", sliderGO.transform);
        var faRT = fillAreaGO.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = faRT.offsetMax = Vector2.zero;
        var fillGO  = MakeGO("Fill", fillAreaGO.transform);
        var fillRT  = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.sprite     = sFill;
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f; fillImg.color = Color.white;

        var handleAreaGO = MakeGO("Handle Slide Area", sliderGO.transform);
        var haRT = handleAreaGO.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = haRT.offsetMax = Vector2.zero;
        var handleGO = MakeGO("Handle", handleAreaGO.transform);
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = handleRT.anchorMax = new Vector2(0f, 0.5f);
        handleRT.pivot     = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(handleW, handleH);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.sprite = sHandle; handleImg.type = Image.Type.Simple;
        handleImg.preserveAspect = true; handleImg.color = Color.white;

        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect      = fillRT;
        slider.handleRect    = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;

        return slider;
    }

    // Строка с иконкой-переключателем без ползунка
    static Toggle MakeIconToggleRow(
        string rowName, string spriteOnPath, string spriteOffPath,
        Transform parent)
    {
        var sOn  = AssetDatabase.LoadAssetAtPath<Sprite>(spriteOnPath);
        var sOff = AssetDatabase.LoadAssetAtPath<Sprite>(spriteOffPath);

        var row = MakeGO(rowName + "Row", parent);
        SetLE(row, minH: 96, prefH: 96);
        row.AddComponent<Image>().color = Color.clear;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth  = false;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.padding = new RectOffset(48, 48, 0, 0);

        var iconGO  = MakeGO(rowName + "Toggle", row.transform);
        SetLE(iconGO, minW: 160, minH: 160);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color = Color.clear;
        var toggle = iconGO.AddComponent<Toggle>();
        toggle.targetGraphic = iconImg;
        toggle.transition    = Selectable.Transition.None;
        toggle.isOn          = true;

        Image MakeSpriteChild(string goName, Sprite sprite, bool active)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(iconGO.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = sprite; img.type = Image.Type.Simple;
            img.preserveAspect = true; img.color = Color.white;
            go.SetActive(active);
            return img;
        }
        MakeSpriteChild("SpriteOn",  sOn,  true);
        MakeSpriteChild("SpriteOff", sOff, false);

        return toggle;
    }

    // Карточка-переключатель: цветной фон, SpriteOn/SpriteOff иконки, подпись
    static Toggle MakeSettingsCard(string name, Color cardColor,
                                   Sprite spriteOn, Sprite spriteOff,
                                   string labelText, Transform parent, TMP_FontAsset font)
    {
        var cardGO  = MakeGO(name, parent);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color                  = cardColor;
        cardImg.sprite                 = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        cardImg.type                   = Image.Type.Sliced;
        cardImg.pixelsPerUnitMultiplier = 0.5f;

        var toggle = cardGO.AddComponent<Toggle>();
        toggle.targetGraphic = cardImg;
        toggle.transition    = Selectable.Transition.None;
        toggle.isOn          = true;

        // Иконка (центр, занимает верхние ~55% карточки)
        Image MakeSpriteChild(string goName, Sprite sprite, bool active)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(cardGO.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.30f);
            rt.anchorMax = new Vector2(0.85f, 0.88f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color          = Color.white;
            img.type           = Image.Type.Simple;
            img.preserveAspect = true;
            if (sprite != null) img.sprite = sprite;
            go.SetActive(active);
            return img;
        }
        MakeSpriteChild("SpriteOn",  spriteOn,  true);
        MakeSpriteChild("SpriteOff", spriteOff, false);

        // Подпись (нижняя треть)
        var lblGO = MakeGO("Label", cardGO.transform);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0);
        lblRT.anchorMax = new Vector2(1, 0.30f);
        lblRT.offsetMin = new Vector2(8, 4);
        lblRT.offsetMax = new Vector2(-8, 0);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text               = labelText;
        lbl.fontSize           = 30;
        lbl.color              = Color.white;
        lbl.alignment          = TextAlignmentOptions.Center;
        lbl.fontStyle          = FontStyles.Bold;
        lbl.enableWordWrapping = false;
        if (font != null) lbl.font = font;

        return toggle;
    }

    // Карточка переключения языка: один клик меняет флаг RU↔SAH (SpriteOn=RU, SpriteOff=SAH)
    static Toggle MakeLangToggleCard(string name, Sprite spriteRu, Sprite spriteSah,
                                     string labelText, Transform parent, TMP_FontAsset font)
    {
        var cardGO  = MakeGO(name, parent);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color  = new Color(0.13f, 0.55f, 0.47f);
        cardImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        cardImg.type   = Image.Type.Sliced;

        var toggle = cardGO.AddComponent<Toggle>();
        toggle.targetGraphic = cardImg;
        toggle.transition    = Selectable.Transition.None;
        toggle.isOn          = true;

        Image MakeFlagChild(string goName, Sprite sprite, bool active)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(cardGO.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.28f);
            rt.anchorMax = new Vector2(0.9f, 0.86f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color          = Color.white;
            img.type           = Image.Type.Simple;
            img.preserveAspect = true;
            if (sprite != null) img.sprite = sprite;
            go.SetActive(active);
            return img;
        }
        MakeFlagChild("SpriteOn",  spriteRu,  true);
        MakeFlagChild("SpriteOff", spriteSah, false);

        var lblGO = MakeGO("Label", cardGO.transform);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0);
        lblRT.anchorMax = new Vector2(1, 0.28f);
        lblRT.offsetMin = new Vector2(12, 4);
        lblRT.offsetMax = new Vector2(-12, 0);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text               = labelText;
        lbl.fontSize           = 30;
        lbl.color              = Color.white;
        lbl.alignment          = TextAlignmentOptions.Center;
        lbl.fontStyle          = FontStyles.Bold;
        lbl.enableWordWrapping = false;
        if (font != null) lbl.font = font;

        return toggle;
    }

    // Карточка-кнопка: цветной фон, текстовый символ иконки, подпись
    static Button MakeActionCard(string name, Color cardColor, string iconText,
                                 string labelText, Transform parent, TMP_FontAsset font)
    {
        var cardGO  = MakeGO(name, parent);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color                   = cardColor;
        cardImg.sprite                  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        cardImg.type                    = Image.Type.Sliced;
        cardImg.pixelsPerUnitMultiplier = 0.5f;

        var btn = cardGO.AddComponent<Button>();
        btn.targetGraphic = cardImg;
        btn.transition    = Selectable.Transition.None;

        var iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(cardGO.transform, false);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.28f);
        iconRT.anchorMax = new Vector2(1, 0.92f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        var iconTMP = iconGO.AddComponent<TextMeshProUGUI>();
        iconTMP.text      = iconText;
        iconTMP.fontSize  = 72;
        iconTMP.color     = Color.white;
        iconTMP.alignment = TextAlignmentOptions.Center;
        if (font != null) iconTMP.font = font;

        var lblGO = MakeGO("Label", cardGO.transform);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0);
        lblRT.anchorMax = new Vector2(1, 0.30f);
        lblRT.offsetMin = new Vector2(8, 4);
        lblRT.offsetMax = new Vector2(-8, 0);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text               = labelText;
        lbl.fontSize           = 30;
        lbl.color              = Color.white;
        lbl.alignment          = TextAlignmentOptions.Center;
        lbl.fontStyle          = FontStyles.Bold;
        lbl.enableWordWrapping = false;
        if (font != null) lbl.font = font;

        return btn;
    }

    // Карточка с вертикальным слайдером громкости внутри
    static (Toggle toggle, Slider slider) MakeToggleCardWithSlider(
        string name, Color cardColor,
        Sprite spriteOn, Sprite spriteOff,
        string labelText, Transform parent, TMP_FontAsset font)
    {
        var cardGO  = MakeGO(name, parent);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color                   = cardColor;
        cardImg.sprite                  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        cardImg.type                    = Image.Type.Sliced;
        cardImg.pixelsPerUnitMultiplier = 0.5f;

        var toggle = cardGO.AddComponent<Toggle>();
        toggle.targetGraphic = cardImg;
        toggle.transition    = Selectable.Transition.None;
        toggle.isOn          = true;

        // Иконка — левая половина, выше подписи
        Image MakeSpriteChild(string goName, Sprite sprite, bool active)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(cardGO.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.26f);
            rt.anchorMax = new Vector2(0.55f, 0.90f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color          = Color.white;
            img.type           = Image.Type.Simple;
            img.preserveAspect = true;
            if (sprite != null) img.sprite = sprite;
            go.SetActive(active);
            return img;
        }
        MakeSpriteChild("SpriteOn",  spriteOn,  true);
        MakeSpriteChild("SpriteOff", spriteOff, false);

        // Вертикальный слайдер — правая треть карточки
        const string acAtlas = "Assets/Images/Sprites/additional controls.png";
        EnsureReadable(acAtlas);
        var sBg     = LoadSprite(acAtlas, "additional controls_3");
        var sFill   = LoadSprite(acAtlas, "additional controls_9");
        var sHandle = LoadSprite(acAtlas, "additional controls_11");

        var sliderGO = MakeGO(name + "Slider", cardGO.transform);
        var sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.63f, 0.10f);
        sliderRT.anchorMax = new Vector2(0.88f, 0.90f);
        sliderRT.offsetMin = sliderRT.offsetMax = Vector2.zero;

        var bgGO  = MakeGO("Background", sliderGO.transform);
        var bgRT  = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = sBg; bgImg.type = Image.Type.Simple;
        bgImg.preserveAspect = false; bgImg.color = Color.white;

        var fillAreaGO = MakeGO("Fill Area", sliderGO.transform);
        var faRT = fillAreaGO.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = faRT.offsetMax = Vector2.zero;
        var fillGO  = MakeGO("Fill", fillAreaGO.transform);
        var fillRT  = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.sprite = sFill; fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Vertical;
        fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImg.fillAmount = 1f; fillImg.color = Color.white;

        var handleAreaGO = MakeGO("Handle Slide Area", sliderGO.transform);
        var haRT = handleAreaGO.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = haRT.offsetMax = Vector2.zero;
        var handleGO = MakeGO("Handle", handleAreaGO.transform);
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = handleRT.anchorMax = new Vector2(0.5f, 0f);
        handleRT.pivot     = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(60f, 60f);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.sprite = sHandle; handleImg.type = Image.Type.Simple;
        handleImg.preserveAspect = true; handleImg.color = Color.white;

        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect      = fillRT;
        slider.handleRect    = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction     = Slider.Direction.BottomToTop;
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;

        // Подпись
        var lblGO = MakeGO("Label", cardGO.transform);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0);
        lblRT.anchorMax = new Vector2(1, 0.26f);
        lblRT.offsetMin = new Vector2(8, 4);
        lblRT.offsetMax = new Vector2(-8, 0);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text               = labelText;
        lbl.fontSize           = 30;
        lbl.color              = Color.white;
        lbl.alignment          = TextAlignmentOptions.Center;
        lbl.fontStyle          = FontStyles.Bold;
        lbl.enableWordWrapping = false;
        if (font != null) lbl.font = font;

        return (toggle, slider);
    }

    // Карточка выбора языка (Button, цвет задаётся через SetLangBtn)
    static Button MakeLangCard(string name, Sprite icon, string labelText, Transform parent, TMP_FontAsset font)
    {
        var cardGO  = MakeGO(name, parent);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color  = new Color(0.85f, 0.85f, 0.85f);
        cardImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        cardImg.type   = Image.Type.Sliced;

        var btn = cardGO.AddComponent<Button>();
        btn.targetGraphic = cardImg;
        btn.transition    = Selectable.Transition.None;

        var iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(cardGO.transform, false);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.28f);
        iconRT.anchorMax = new Vector2(0.9f, 0.86f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color = Color.white;
        if (icon != null)
        {
            iconImg.sprite         = icon;
            iconImg.type           = Image.Type.Simple;
            iconImg.preserveAspect = true;
        }

        var lblGO = MakeGO("Label", cardGO.transform);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0);
        lblRT.anchorMax = new Vector2(1, 0.28f);
        lblRT.offsetMin = new Vector2(12, 4);
        lblRT.offsetMax = new Vector2(-12, 0);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text               = labelText;
        lbl.fontSize           = 30;
        lbl.color              = new Color(0.10f, 0.16f, 0.10f);
        lbl.alignment          = TextAlignmentOptions.Center;
        lbl.fontStyle          = FontStyles.Bold;
        lbl.enableWordWrapping = false;
        if (font != null) lbl.font = font;

        return btn;
    }

    // Таб-кнопка: подпись + индикатор-подчёркивание
    static Button MakeTabButton(string name, string labelText, Transform parent, TMP_FontAsset font)
    {
        var go  = MakeGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color = Color.clear;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition    = Selectable.Transition.None;

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.padding = new RectOffset(0, 0, 8, 0);
        vlg.spacing = 4;

        var lbl = MakeTMP("Label", go.transform, labelText, 34, Color.white, font, minH: 36, bold: true);
        lbl.alignment = TextAlignmentOptions.Center;
        SetLE(lbl.gameObject, flexH: 1f);

        var indGO = MakeGO("Indicator", go.transform);
        SetLE(indGO, minH: 4, prefH: 4);
        indGO.AddComponent<Image>().color = Color.clear;

        return btn;
    }

    // Разделитель строк настроек
    static void MakeRowSeparator(Transform parent)
    {
        var sep = MakeGO("Separator", parent);
        SetLE(sep, minH: 1, prefH: 1, flexH: 0);
        sep.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.08f);
    }

    // Строка выбора языка: Label слева + две кнопки справа
    static (Button btnRu, Button btnSah) MakeLangRow(Transform parent, TMP_FontAsset font)
    {
        var row = MakeGO("LangRow", parent);
        SetLE(row, minH: 96, prefH: 96);
        row.AddComponent<Image>().color = Color.clear;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = hlg.childControlHeight = true;
        hlg.padding = new RectOffset(48, 48, 0, 0);
        hlg.spacing = 24;

        var labelTMP = MakeTMP("Label", row.transform, "Язык", 40, C_TEXT, font);
        SetLE(labelTMP.gameObject, flexW: 1f);
        AddLocKey(labelTMP.gameObject, "settings_language");

        // Группа кнопок языков
        var btnGroup = MakeGO("LangButtons", row.transform);
        btnGroup.AddComponent<Image>().color = Color.clear;
        var bHLG = btnGroup.AddComponent<HorizontalLayoutGroup>();
        bHLG.childAlignment        = TextAnchor.MiddleCenter;
        bHLG.childForceExpandWidth = false;
        bHLG.childControlWidth     = bHLG.childControlHeight = true;
        bHLG.spacing = 12;

        var sRu  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Icons/language/rus.png");
        var sSah = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Icons/language/sakha.png");

        var btnRuGO  = MakeLangButton("BtnLangRu",  btnGroup.transform, sRu,  active: true);
        var btnSahGO = MakeLangButton("BtnLangSah", btnGroup.transform, sSah, active: false);

        return (btnRuGO.GetComponent<Button>(), btnSahGO.GetComponent<Button>());
    }

    static GameObject MakeLangButton(string name, Transform parent, Sprite icon, bool active)
    {
        var go  = MakeGO(name, parent);
        SetLE(go, minW: 60, minH: 40);
        var img = go.AddComponent<Image>();
        if (icon != null)
        {
            img.sprite         = icon;
            img.type           = Image.Type.Simple;
            img.preserveAspect = false;
            img.color          = active ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }
        else
        {
            img.color  = active ? C_PRIMARY : new Color(0.85f, 0.85f, 0.85f);
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type   = Image.Type.Sliced;
        }
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition    = Selectable.Transition.None;
        return go;
    }

    // Строка с ползунком громкости (label слева + Slider справа)
    static Slider MakeVolumeSliderRow(string rowName, Transform parent, TMP_FontAsset font)
    {
        var row = MakeGO(rowName + "Row", parent);
        SetLE(row, minH: 72, prefH: 72);
        row.AddComponent<Image>().color = Color.clear;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth      = hlg.childControlHeight = true;
        hlg.padding = new RectOffset(48, 48, 0, 0);
        hlg.spacing = 20;

        var lbl = MakeTMP("Label", row.transform, "Громкость", 32, C_TEXT2, font);
        SetLE(lbl.gameObject, minW: 170);

        // Контейнер слайдера
        var sliderGO = MakeGO("Slider", row.transform);
        SetLE(sliderGO, minH: 44, prefH: 44, flexW: 1f);

        // Background
        var bgGO = MakeGO("Background", sliderGO.transform);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.25f);
        bgRT.anchorMax = new Vector2(1f, 0.75f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<Image>().color = new Color(0.80f, 0.80f, 0.80f);

        // Fill Area
        var fillAreaGO = MakeGO("Fill Area", sliderGO.transform);
        var faRT = fillAreaGO.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.25f);
        faRT.anchorMax = new Vector2(1f, 0.75f);
        faRT.offsetMin = new Vector2(5f, 0f);
        faRT.offsetMax = new Vector2(-15f, 0f);

        var fillGO = MakeGO("Fill", fillAreaGO.transform);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(1f, 1f);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = C_PRIMARY;

        // Handle Slide Area
        var handleAreaGO = MakeGO("Handle Slide Area", sliderGO.transform);
        var haRT = handleAreaGO.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero;
        haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10f, 0f);
        haRT.offsetMax = new Vector2(-10f, 0f);

        var handleGO = MakeGO("Handle", handleAreaGO.transform);
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = handleRT.anchorMax = new Vector2(0f, 0.5f);
        handleRT.pivot     = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(44f, 44f);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color  = C_PRIMARY;
        handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        handleImg.type   = Image.Type.Sliced;

        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect      = fillRT;
        slider.handleRect    = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue      = 0f;
        slider.maxValue      = 1f;
        slider.value         = 1f;

        return slider;
    }

    // Вкладка нижнего навбара: кнопка с иконкой-кружком и подписью
    static (Button btn, Image icon, TMP_Text label) MakeNavTab(
        string name, string labelKey, Transform parent, TMP_FontAsset font)
    {
        var go  = MakeGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color = Color.clear;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        go.AddComponent<ButtonSFX>();

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth  = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.padding = new RectOffset(0, 0, 10, 10);
        vlg.spacing = 4;

        var iconGO  = MakeGO("Icon", go.transform);
        SetLE(iconGO, minW: 36, minH: 36, prefH: 36);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color  = new Color(0.60f, 0.60f, 0.60f);
        iconImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        iconImg.type   = Image.Type.Sliced;

        var lbl = MakeTMP("Label", go.transform, labelKey, 28,
                          new Color(0.60f, 0.60f, 0.60f), font, minH: 28);
        AddLocKey(lbl.gameObject, labelKey);
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.gameObject.SetActive(false);

        return (btn, iconImg, lbl);
    }

    // =====================================================================
    // ПАНЕЛЬ "ПРЕДЛОЖИТЬ ВОПРОС"
    // =====================================================================

    static SuggestQuestionUI BuildSuggestPanel(Transform canvasTransform, TMP_FontAsset font)
    {
        // Overlay (полный экран)
        var overlay = MakeGO("SuggestPanel", canvasTransform);
        Stretch(overlay);
        overlay.AddComponent<Image>().color = C_OVERLAY;
        var overlayCG = overlay.AddComponent<CanvasGroup>();
        overlay.SetActive(false);

        // Sheet — якорное позиционирование: заполняет экран с отступами
        var sheet   = MakeGO("SuggestSheet", overlay.transform);
        var sheetRT = sheet.GetComponent<RectTransform>();
        sheetRT.anchorMin = Vector2.zero;
        sheetRT.anchorMax = Vector2.one;
        sheetRT.offsetMin = new Vector2(40, 80);   // отступ слева/снизу
        sheetRT.offsetMax = new Vector2(-40, -80); // отступ справа/сверху
        sheet.AddComponent<Image>().color = C_CARD;
        var sheetCG = sheet.AddComponent<CanvasGroup>();

        // Header — прикреплён к верху sheet
        var header   = MakeGO("SuggestHeader", sheet.transform);
        var headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot     = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0, 100);
        header.AddComponent<Image>().color = C_PRIMARY;
        var hHLG = header.AddComponent<HorizontalLayoutGroup>();
        hHLG.childAlignment        = TextAnchor.MiddleLeft;
        hHLG.childForceExpandHeight = true;
        hHLG.childControlWidth = hHLG.childControlHeight = true;
        hHLG.padding = new RectOffset(40, 112, 0, 0); // правый отступ = ширина кнопки закрытия
        var titleTMP = MakeTMP("SuggestTitle", header.transform, "Предложить вопрос", 40, Color.white, font, bold: true);
        SetLE(titleTMP.gameObject, flexW: 1f);
        AddLocKey(titleTMP.gameObject, "suggest_title");

        // Кнопка закрытия — абсолютно позиционированная поверх правого верхнего угла sheet
        var closeBtnGO = MakeGO("BtnClose", sheet.transform);
        var closeBtnRT = closeBtnGO.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1f, 1f);
        closeBtnRT.anchorMax = new Vector2(1f, 1f);
        closeBtnRT.pivot     = new Vector2(1f, 1f);
        closeBtnRT.anchoredPosition = Vector2.zero;
        closeBtnRT.sizeDelta        = new Vector2(100, 100);
        var closeImg   = closeBtnGO.AddComponent<Image>();
        closeImg.color = new Color(0f, 0f, 0f, 0.30f);
        var closeBtn   = closeBtnGO.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        var closeTMP   = MakeTMP("X", closeBtnGO.transform, "×", 48, Color.white, font, bold: true);
        var closeRT    = closeTMP.GetComponent<RectTransform>();
        closeRT.anchorMin = Vector2.zero; closeRT.anchorMax = Vector2.one;
        closeRT.offsetMin = closeRT.offsetMax = Vector2.zero;
        closeTMP.alignment = TextAlignmentOptions.Center;

        // Кнопка «Отправить» — прикреплена к низу sheet
        var sendRow   = MakeGO("SendBtnRow", sheet.transform);
        var sendRowRT = sendRow.GetComponent<RectTransform>();
        sendRowRT.anchorMin = new Vector2(0, 0);
        sendRowRT.anchorMax = new Vector2(1, 0);
        sendRowRT.pivot     = new Vector2(0.5f, 0f);
        sendRowRT.anchoredPosition = Vector2.zero;
        sendRowRT.sizeDelta = new Vector2(0, 88);
        sendRow.AddComponent<Image>().color = C_CARD;
        var srHLG = sendRow.AddComponent<HorizontalLayoutGroup>();
        srHLG.padding = new RectOffset(48, 48, 8, 16);
        srHLG.childForceExpandWidth = true;
        srHLG.childControlWidth = srHLG.childControlHeight = true;
        var btnSendGO = MakePrimaryButton("BtnSend", sendRow.transform, "Отправить", font);
        AddLocKey(btnSendGO, "suggest_send");

        // ScrollView — заполняет пространство между header и send row
        var scrollGO   = MakeGO("ScrollView", sheet.transform);
        var scrollRT   = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(0, 88);   // над send row
        scrollRT.offsetMax = new Vector2(0, -100); // под header
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal        = false;
        scroll.vertical          = true;
        scroll.scrollSensitivity = 50f;
        scroll.movementType      = ScrollRect.MovementType.Clamped;

        // Viewport
        var viewport = MakeGO("Viewport", scrollGO.transform);
        var vpRT     = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = new Vector2(1f, 1f);
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-24, 0); // место для скроллбара
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = vpRT;

        // Content
        var content   = MakeGO("Content", viewport.transform);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var contentVLG = content.AddComponent<VerticalLayoutGroup>();
        contentVLG.childAlignment        = TextAnchor.UpperCenter;
        contentVLG.childForceExpandWidth = true;
        contentVLG.childControlWidth = contentVLG.childControlHeight = true;
        contentVLG.padding = new RectOffset(0, 0, 8, 16);
        contentVLG.spacing = 4;
        scroll.content = contentRT;

        // Вертикальный скроллбар
        var sbGO  = MakeGO("Scrollbar", scrollGO.transform);
        var sbRT  = sbGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot     = new Vector2(1, 0.5f);
        sbRT.anchoredPosition = Vector2.zero;
        sbRT.sizeDelta = new Vector2(20, 0);
        sbGO.AddComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f);
        var sb = sbGO.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        var sbHandleArea   = MakeGO("SlidingArea", sbGO.transform);
        var sbHandleAreaRT = sbHandleArea.GetComponent<RectTransform>();
        sbHandleAreaRT.anchorMin = Vector2.zero; sbHandleAreaRT.anchorMax = Vector2.one;
        sbHandleAreaRT.offsetMin = sbHandleAreaRT.offsetMax = Vector2.zero;

        var sbHandle   = MakeGO("Handle", sbHandleArea.transform);
        var sbHandleRT = sbHandle.GetComponent<RectTransform>();
        sbHandleRT.anchorMin = Vector2.zero; sbHandleRT.anchorMax = Vector2.one;
        sbHandleRT.offsetMin = sbHandleRT.offsetMax = Vector2.zero;
        var sbHandleImg = sbHandle.AddComponent<Image>();
        sbHandleImg.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        sb.handleRect          = sbHandleRT;
        sb.targetGraphic       = sbHandleImg;
        scroll.verticalScrollbar = sb;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        // Поля ввода
        var qRuField     = MakeFieldRow("QuestionRU",  "suggest_question_ru",    content.transform, font, multiline: true);
        var qSahField    = MakeFieldRow("QuestionSAH", "suggest_question_sah",   content.transform, font, multiline: true);
        MakeFormSpacer(content.transform);
        var ans1Field    = MakeFieldRow("Answer1",     "suggest_answer_correct", content.transform, font);
        var ans2Field    = MakeFieldRow("Answer2",     "suggest_answer_2",       content.transform, font);
        var ans3Field    = MakeFieldRow("Answer3",     "suggest_answer_3",       content.transform, font);
        var ans4Field    = MakeFieldRow("Answer4",     "suggest_answer_4",       content.transform, font);
        MakeFormSpacer(content.transform);
        var factRuField  = MakeFieldRow("FactRU",      "suggest_fact_ru",        content.transform, font, multiline: true);
        var factSahField = MakeFieldRow("FactSAH",     "suggest_fact_sah",       content.transform, font, multiline: true);

        // Сообщение об ошибке валидации (скрыто по умолчанию)
        var errGO  = MakeGO("ValidationError", content.transform);
        SetLE(errGO, minH: 56, prefH: 56);
        var errTMP = errGO.AddComponent<TextMeshProUGUI>();
        errTMP.text      = "Необходимо заполнить все поля";
        errTMP.fontSize  = 26;
        errTMP.color     = new Color(0.85f, 0.15f, 0.15f);
        errTMP.alignment = TextAlignmentOptions.Center;
        errTMP.fontStyle = FontStyles.Bold;
        if (font != null) errTMP.font = font;
        errGO.SetActive(false);

        // SuggestQuestionUI компонент
        var mgrGO = new GameObject("SuggestManager");
        var comp  = mgrGO.AddComponent<SuggestQuestionUI>();
        var so    = new UnityEditor.SerializedObject(comp);
        Prop(so, "panel",            overlay);
        Prop(so, "btnClose",         closeBtn);
        Prop(so, "sheetRect",        sheetRT);
        Prop(so, "sheetGroup",       sheetCG);
        Prop(so, "overlayGroup",     overlayCG);
        Prop(so, "questionRuField",  qRuField);
        Prop(so, "questionSahField", qSahField);
        Prop(so, "answer1Field",     ans1Field);
        Prop(so, "answer2Field",     ans2Field);
        Prop(so, "answer3Field",     ans3Field);
        Prop(so, "answer4Field",     ans4Field);
        Prop(so, "factRuField",      factRuField);
        Prop(so, "factSahField",     factSahField);
        Prop(so, "btnSend",          btnSendGO.GetComponent<Button>());
        Prop(so, "scrollContent",    contentRT);
        Prop(so, "errorText",        errTMP);
        so.ApplyModifiedProperties();

        return comp;
    }

    // Строка «метка + поле ввода» для формы предложения
    static TMP_InputField MakeFieldRow(string rowName, string locKey, Transform parent,
                                       TMP_FontAsset font, bool multiline = false)
    {
        var row = MakeGO(rowName + "Row", parent);
        SetLE(row, minH: multiline ? 200 : 128, prefH: multiline ? 200 : 128);
        var vlg = row.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.padding = new RectOffset(40, 40, 8, 8);
        vlg.spacing = 6;

        var lbl = MakeTMP("Label", row.transform, locKey, 32, C_TEXT2, font, minH: 36);
        AddLocKey(lbl.gameObject, locKey);

        return MakeInputField(rowName + "Field", row.transform, font, multiline);
    }

    // TMP_InputField с правильной иерархией (viewport / placeholder / text)
    static TMP_InputField MakeInputField(string name, Transform parent,
                                         TMP_FontAsset font, bool multiline = false)
    {
        var go  = MakeGO(name, parent);
        SetLE(go, minH: multiline ? 144 : 76, prefH: multiline ? 144 : 76);
        var img = go.AddComponent<Image>();
        img.color  = new Color(0.93f, 0.93f, 0.93f);
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type   = Image.Type.Sliced;

        // Text Area (viewport)
        var taGO = MakeGO("Text Area", go.transform);
        var taRT = taGO.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(12, 6); taRT.offsetMax = new Vector2(-12, -6);
        taGO.AddComponent<RectMask2D>();

        // Placeholder
        var phGO  = MakeGO("Placeholder", taGO.transform);
        var phRT  = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = phRT.offsetMax = Vector2.zero;
        var phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text      = "—";
        phTMP.fontSize  = 28;
        phTMP.color     = new Color(0.65f, 0.65f, 0.65f);
        phTMP.fontStyle = FontStyles.Italic;
        if (font != null) phTMP.font = font;
        if (multiline) phTMP.enableWordWrapping = true;

        // Text
        var txtGO  = MakeGO("Text", taGO.transform);
        var txtRT  = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        var txtTMP = txtGO.AddComponent<TextMeshProUGUI>();
        txtTMP.fontSize = 28;
        txtTMP.color    = C_TEXT;
        if (font != null) txtTMP.font = font;
        if (multiline) txtTMP.enableWordWrapping = true;

        var field = go.AddComponent<TMP_InputField>();
        field.textViewport    = taRT;
        field.textComponent   = txtTMP;
        field.placeholder     = phTMP;
        field.characterLimit  = multiline ? 500 : 200;
        if (multiline) field.lineType = TMP_InputField.LineType.MultiLineNewline;

        return field;
    }

    // Прозрачный разделитель внутри формы
    static void MakeFormSpacer(Transform parent)
    {
        var sp = MakeGO("FormSpacer", parent);
        SetLE(sp, minH: 20, prefH: 20);
        sp.AddComponent<Image>().color = Color.clear;
    }

    static void AddStatusBarCover(Transform canvasTransform)
    {
        var go = MakeGO("StatusBarCover", canvasTransform);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 0f);
        go.AddComponent<Image>().color = Color.black;
        go.AddComponent<UstAldanQuiz.UI.StatusBarCover>();
    }

    // =====================================================================
    // ЛОКАЛИЗАЦИЯ — добавить LocaleText на TMP_Text внутри GO
    // =====================================================================

    static void AddLocKey(GameObject go, string key)
    {
        if (go == null) return;
        var tmp = go.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null)
        {
            Debug.LogWarning($"[GameSceneBuilder] TMP_Text не найден для ключа '{key}' в {go.name}");
            return;
        }
        var lt = tmp.gameObject.AddComponent<LocaleText>();
        var so = new UnityEditor.SerializedObject(lt);
        var kp = so.FindProperty("key");
        if (kp != null) { kp.stringValue = key; so.ApplyModifiedProperties(); }
    }

    // =====================================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ — АССЕТЫ
    // =====================================================================

    static TMP_FontAsset FindFont()
    {
        TMP_FontAsset best = null;
        foreach (var g in AssetDatabase.FindAssets("PTSans t:TMP_FontAsset", new[] { "Assets" }))
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g));
            if (f == null || !f.HasCharacter(' ')) continue;
            if (best == null || f.glyphTable.Count > best.glyphTable.Count)
                best = f;
        }
        if (best != null) return best;

        const string libPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        var lib = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(libPath);
        return lib != null ? lib : TMP_Settings.defaultFontAsset;
    }

    static QuestionCategory FindCategory(string id)
    {
        var guids = AssetDatabase.FindAssets("t:QuestionCategory");
        foreach (var g in guids)
        {
            var cat = AssetDatabase.LoadAssetAtPath<QuestionCategory>(AssetDatabase.GUIDToAssetPath(g));
            if (cat != null && cat.categoryId == id) return cat;
        }
        return null;
    }

    static AudioClip FindAudioClip(string folder, string nameContains)
    {
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (nameContains == null || file.Contains(nameContains.ToLowerInvariant()))
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }

    static T FindAsset<T>(string filter) where T : Object
    {
        var guids = AssetDatabase.FindAssets(filter);
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static Sprite LoadSprite(string atlasPath, string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(atlasPath)
            .OfType<Sprite>()
            .FirstOrDefault(s => s.name == spriteName);
    }

    static void EnsureReadable(string atlasPath)
    {
        if (AssetImporter.GetAtPath(atlasPath) is not TextureImporter ti || ti.isReadable) return;
        ti.isReadable = true;
        AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceUpdate);
    }

    static void ApplyHyperCasualButton(GameObject btnGO, string atlasPath,
                                        string normalName, string pressedName)
    {
        var normalSprite  = LoadSprite(atlasPath, normalName);
        var pressedSprite = LoadSprite(atlasPath, pressedName);
        if (normalSprite == null)
        {
            Debug.LogWarning($"[GameSceneBuilder] Спрайт '{normalName}' не найден в {atlasPath}");
            return;
        }

        var img = btnGO.GetComponent<Image>();
        img.sprite = normalSprite;
        img.type   = Image.Type.Sliced;
        img.color  = Color.white;

        var btn = btnGO.GetComponent<Button>();
        btn.transition = Selectable.Transition.SpriteSwap;
        if (pressedSprite != null)
        {
            var ss = btn.spriteState;
            ss.pressedSprite     = pressedSprite;
            ss.highlightedSprite = pressedSprite;
            // selectedSprite не задаём: Selected-состояние использует null → выглядит как Normal
            btn.spriteState = ss;
        }

        if (btnGO.GetComponent<ButtonDragReset>() == null)
            btnGO.AddComponent<ButtonDragReset>();
        if (btnGO.GetComponent<ButtonSpringAnim>() == null)
            btnGO.AddComponent<ButtonSpringAnim>();
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    // SerializedObject helpers
    static void Prop(UnityEditor.SerializedObject so, string name, Object value)
    {
        var p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"[GameSceneBuilder] Поле '{name}' не найдено"); return; }
        p.objectReferenceValue = value;
    }

    static void SetArr<T>(UnityEditor.SerializedObject so, string name, T[] values) where T : Object
    {
        var p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"[GameSceneBuilder] Массив '{name}' не найден"); return; }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
