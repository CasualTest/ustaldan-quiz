using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UstAldanQuiz.UI;
using UstAldanQuiz.Utils;

public static partial class GameSceneBuilder
{
    // =====================================================================
    // 6. СЦЕНА «МИЛЛИОНЕР»
    // =====================================================================

    static void DoBuildMillionaire(bool skipIfExists = false)
    {
        if (skipIfExists && File.Exists("Assets/Scenes/Millionaire.unity"))
        { Debug.Log("[GameSceneBuilder] Millionaire.unity уже существует — пропускаем."); return; }

        OpenOrCreateScene("Assets/Scenes/Millionaire.unity");
        var font = FindFont();

        var canvasGO = SetupCanvas("Millionaire");
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
        saVLG.padding  = new RectOffset(0, 0, 140, 0);
        saVLG.spacing  = 0;

        // ── Header ────────────────────────────────────────────────────────
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
        const int sideSlotW = 200;
        int backW = sBack != null ? Mathf.RoundToInt(backH * sBack.rect.width / sBack.rect.height) : backH;

        var leftSlot = MakeGO("LeftSlot", header.transform);
        SetLE(leftSlot, minW: sideSlotW, flexW: 0);
        var leftSlotHLG = leftSlot.AddComponent<HorizontalLayoutGroup>();
        leftSlotHLG.childAlignment        = TextAnchor.MiddleLeft;
        leftSlotHLG.childForceExpandWidth = false;
        leftSlotHLG.childForceExpandHeight = true;
        leftSlotHLG.childControlWidth = leftSlotHLG.childControlHeight = true;
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

        var titleTMP = MakeTMP("Title", header.transform, "Миллионер", 42, Color.white, font);
        SetLE(titleTMP.gameObject, flexW: 1);
        titleTMP.alignment = TextAlignmentOptions.Center;
        AddLocKey(titleTMP.gameObject, "mode_millionaire");

        var progressTMP = MakeTMP("ProgressText", header.transform, "0/15", 34, Color.white, font);
        SetLE(progressTMP.gameObject, minW: sideSlotW, flexW: 0);
        progressTMP.alignment = TextAlignmentOptions.Right;

        // ── Заглушка для VLG (вопрос показывается через QuestionWindow) ──
        var stub = MakeGO("CenterStub", safeArea.transform);
        SetLE(stub, flexH: 1f, minH: 400);
        stub.AddComponent<Image>().color = Color.clear;

        // ── QuestionWindow + FactPopup ───────────────────────────────────
        var qWinGO = InstantiateUIPrefab(QuestionWindowPath, canvasGO.transform);
        if (qWinGO == null) return;
        var qWin = qWinGO.GetComponent<QuestionWindow>();

        var factGO = InstantiateUIPrefab(FactPopupPath, canvasGO.transform);
        if (factGO == null) return;
        var factPopupComp = factGO.GetComponent<FactPopup>();

        var soQW = new UnityEditor.SerializedObject(qWin);
        Prop(soQW, "factPopup", factPopupComp);
        soQW.ApplyModifiedProperties();

        // ── MillionaireUI ────────────────────────────────────────────────
        var managerGO = MakeRootGO("MillionaireManager");
        var ui        = managerGO.AddComponent<MillionaireUI>();
        var soUI      = new UnityEditor.SerializedObject(ui);

        Prop(soUI, "titleText",          titleTMP);
        Prop(soUI, "progressText",       progressTMP);
        Prop(soUI, "btnBack",            btnBackGO.GetComponent<Button>());
        Prop(soUI, "questionWindowFull", qWin);

        var colorProp = soUI.FindProperty("colorDefault");
        if (colorProp != null) colorProp.colorValue = Color.white;

        soUI.ApplyModifiedProperties();

        SaveScene("Assets/Scenes/Millionaire.unity");
        Debug.Log("[GameSceneBuilder] ✓ Millionaire сцена построена.");
    }
}
