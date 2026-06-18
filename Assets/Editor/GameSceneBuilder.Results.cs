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
    // 4. СЦЕНА РЕЗУЛЬТАТОВ
    // =====================================================================

    static readonly Color C_RESULTS_BG       = Hex("F5F0E8");
    static readonly Color C_CARD_BG          = Hex("FFFFFF");
    static readonly Color C_CARD_BORDER      = Hex("E1DDD3");
    static readonly Color C_SCORE_GREEN      = Hex("2D6040");
    static readonly Color C_GOLD             = Hex("C8A84B");
    static readonly Color C_TROPHY           = Hex("C8A84B");
    static readonly Color C_ICON_FILL        = Hex("2D6040");
    static readonly Color C_TEXT_DARK        = Hex("1A2A1A");
    static readonly Color C_TEXT_MUTED       = Hex("4A6A4A");
    static readonly Color C_PROGRESS_BG      = Hex("E5E3DC");
    static readonly Color C_PROGRESS_FILL    = Hex("C8A84B");

    static void DoBuildResults(bool skipIfExists = false)
    {
        if (skipIfExists && File.Exists("Assets/Scenes/Results.unity"))
        { Debug.Log("[GameSceneBuilder] Results.unity уже существует — пропускаем."); return; }

        OpenOrCreateScene("Assets/Scenes/Results.unity");
        var font = FindFont();

        var canvasGO = SetupCanvas("Results");
        SetupCamera();
        SetupEventSystem();

        var bg = MakeGO("Background", canvasGO.transform);
        Stretch(bg); bg.AddComponent<Image>().color = C_RESULTS_BG;

        AddStatusBarCover(canvasGO.transform);

        var safeArea = MakeGO("SafeArea", canvasGO.transform);
        Stretch(safeArea); safeArea.AddComponent<SafeArea>();

        var saVLG = safeArea.AddComponent<VerticalLayoutGroup>();
        saVLG.childAlignment         = TextAnchor.UpperCenter;
        saVLG.childForceExpandWidth  = true;
        saVLG.childForceExpandHeight = false;
        saVLG.childControlWidth = saVLG.childControlHeight = true;
        saVLG.spacing = 0;

        // Scroll (растягивается, занимает всё свободное место)
        var scrollGO = MakeGO("Scroll", safeArea.transform);
        SetLE(scrollGO, flexH: 1f, minH: 200);
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true;

        var viewport = MakeGO("Viewport", scrollGO.transform);
        Stretch(viewport);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = MakeGO("Content", viewport.transform);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
        var contentVLG = content.AddComponent<VerticalLayoutGroup>();
        contentVLG.childAlignment        = TextAnchor.UpperCenter;
        contentVLG.childForceExpandWidth = true;
        contentVLG.childForceExpandHeight = false;
        contentVLG.childControlWidth = contentVLG.childControlHeight = true;
        contentVLG.padding = new RectOffset(40, 40, 60, 60);
        contentVLG.spacing = 24;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;

        // ── Заголовок ────────────────────────────────────────────────────
        var titleTMP = MakeTMP("ResultTitle", content.transform, "Результаты", 60, C_SCORE_GREEN, font, minH: 80, bold: true);
        titleTMP.alignment = TextAlignmentOptions.Center;
        AddLocKey(titleTMP.gameObject, "results_title");

        var subtitleTMP = MakeTMP("Subtitle", content.transform, "Ты отлично справился!", 36, C_TEXT_DARK, font, minH: 50);
        subtitleTMP.alignment = TextAlignmentOptions.Center;

        // ── Кубок ────────────────────────────────────────────────────────
        var trophyGO = MakeGO("Trophy", content.transform);
        SetLE(trophyGO, minH: 130, prefH: 130);
        var trophyImg = trophyGO.AddComponent<Image>();
        var trophySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Icons/trophy.png");
        if (trophySprite != null)
        {
            trophyImg.sprite = trophySprite;
            trophyImg.preserveAspect = true;
            trophyImg.color = Color.white;
        }
        else
        {
            trophyImg.color = Color.clear;
            var trophyText = MakeTMP("TrophyEmoji", trophyGO.transform, "🏆", 110, C_TROPHY, font, bold: true);
            var trtRT = trophyText.GetComponent<RectTransform>();
            trtRT.anchorMin = Vector2.zero; trtRT.anchorMax = Vector2.one;
            trtRT.offsetMin = trtRT.offsetMax = Vector2.zero;
            trophyText.alignment = TextAlignmentOptions.Center;
        }

        // ── Главный счёт (зелёный блок) ──────────────────────────────────
        var scoreCard = MakeGO("ScoreCard", content.transform);
        SetLE(scoreCard, minH: 280, prefH: 280);
        scoreCard.AddComponent<Image>().color = C_SCORE_GREEN;
        var scVLG = scoreCard.AddComponent<VerticalLayoutGroup>();
        scVLG.childAlignment = TextAnchor.MiddleCenter;
        scVLG.childForceExpandWidth = true;
        scVLG.childControlWidth = scVLG.childControlHeight = true;
        scVLG.spacing = 4;
        scVLG.padding = new RectOffset(20, 20, 20, 20);

        var scLabel = MakeTMP("Label", scoreCard.transform, "Твой результат", 36, Color.white, font, minH: 50);
        scLabel.alignment = TextAlignmentOptions.Center;
        AddLocKey(scLabel.gameObject, "results_your_score");

        var scoreBigTMP = MakeTMP("ScoreBig", scoreCard.transform, "0/15", 120, Color.white, font, minH: 140, bold: true);
        scoreBigTMP.alignment = TextAlignmentOptions.Center;

        var scCorrect = MakeTMP("CorrectLabel", scoreCard.transform, "Верных ответов", 34, Color.white, font, minH: 50);
        scCorrect.alignment = TextAlignmentOptions.Center;
        AddLocKey(scCorrect.gameObject, "results_correct_answers");

        // ── Статистика (карточка) ────────────────────────────────────────
        var statsCard = MakeCard("StatsCard", content.transform);
        var statsVLG = statsCard.AddComponent<VerticalLayoutGroup>();
        statsVLG.childAlignment = TextAnchor.UpperLeft;
        statsVLG.childForceExpandWidth = true;
        statsVLG.childControlWidth = statsVLG.childControlHeight = true;
        statsVLG.padding = new RectOffset(28, 28, 24, 24);
        statsVLG.spacing = 12;
        statsCard.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var totalRow   = MakeStatRow("RowTotal",   statsCard.transform, font, "Всего вопросов", "stats_total");
        var correctRow = MakeStatRow("RowCorrect", statsCard.transform, font, "Верных ответов", "stats_correct");
        var wrongRow   = MakeStatRow("RowWrong",   statsCard.transform, font, "Неверных ответов", "stats_wrong");
        var timeRow    = MakeStatRow("RowTime",    statsCard.transform, font, "Среднее время ответа", "stats_avg_time");

        // ── Результат по темам ───────────────────────────────────────────
        var catsCard = MakeCard("CategoriesCard", content.transform);
        var catsVLG = catsCard.AddComponent<VerticalLayoutGroup>();
        catsVLG.childAlignment = TextAnchor.UpperLeft;
        catsVLG.childForceExpandWidth = true;
        catsVLG.childControlWidth = catsVLG.childControlHeight = true;
        catsVLG.padding = new RectOffset(28, 28, 24, 24);
        catsVLG.spacing = 14;
        catsCard.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var catsTitleTMP = MakeTMP("Title", catsCard.transform, "Результат по темам", 34, C_SCORE_GREEN, font, minH: 50, bold: true);
        catsTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        AddLocKey(catsTitleTMP.gameObject, "results_by_categories");

        var catsContent = MakeGO("CategoriesContent", catsCard.transform);
        var catsContentVLG = catsContent.AddComponent<VerticalLayoutGroup>();
        catsContentVLG.childAlignment = TextAnchor.UpperLeft;
        catsContentVLG.childForceExpandWidth = true;
        catsContentVLG.childControlWidth = catsContentVLG.childControlHeight = true;
        catsContentVLG.spacing = 10;
        catsContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Лучший результат ─────────────────────────────────────────────
        var bestCard = MakeCard("BestScoreCard", content.transform);
        SetLE(bestCard, minH: 90, prefH: 90);
        var bestHLG = bestCard.AddComponent<HorizontalLayoutGroup>();
        bestHLG.childAlignment = TextAnchor.MiddleCenter;
        bestHLG.childForceExpandWidth = false;
        bestHLG.childForceExpandHeight = false;
        bestHLG.childControlWidth = bestHLG.childControlHeight = true;
        bestHLG.padding = new RectOffset(28, 28, 16, 16);
        bestHLG.spacing = 14;

        var starGO = MakeGO("Star", bestCard.transform);
        SetLE(starGO, minW: 44, minH: 44, prefH: 44);
        var starText = MakeTMP("StarText", starGO.transform, "★", 44, C_GOLD, font, bold: true);
        var starRT = starText.GetComponent<RectTransform>();
        starRT.anchorMin = Vector2.zero; starRT.anchorMax = Vector2.one;
        starRT.offsetMin = starRT.offsetMax = Vector2.zero;
        starText.alignment = TextAlignmentOptions.Center;

        var bestTMP = MakeTMP("BestText", bestCard.transform, "Лучший результат: 0/15", 32, C_TEXT_DARK, font, minH: 50);
        bestTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // ── Кнопки (фиксированный блок снизу) ────────────────────────────
        var bottomBlock = MakeGO("BottomButtons", safeArea.transform);
        var bottomVLG = bottomBlock.AddComponent<VerticalLayoutGroup>();
        bottomVLG.childAlignment         = TextAnchor.UpperCenter;
        bottomVLG.childForceExpandWidth  = true;
        bottomVLG.childForceExpandHeight = false;
        bottomVLG.childControlWidth = bottomVLG.childControlHeight = true;
        bottomVLG.padding = new RectOffset(40, 40, 20, 40);
        bottomVLG.spacing = 14;
        bottomBlock.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        bottomBlock.AddComponent<Image>().color = C_RESULTS_BG;

        var btnPlayAgain = MakePrimaryButton("BtnPlayAgain", bottomBlock.transform, "Играть снова", font);
        var btnMainMenu  = MakeSecondaryButton("BtnMainMenu", bottomBlock.transform, "Главное меню", font);
        var btnShare     = MakeSecondaryButton("BtnShare",    bottomBlock.transform, "Поделиться",   font);
        SetLE(btnPlayAgain, minH: 110, prefH: 110);
        SetLE(btnMainMenu,  minH: 100, prefH: 100);
        SetLE(btnShare,     minH: 100, prefH: 100);
        AddLocKey(btnPlayAgain, "btn_play_again");
        AddLocKey(btnMainMenu,  "btn_main_menu");
        AddLocKey(btnShare,     "btn_share");

        // ── ResultsUI ────────────────────────────────────────────────────
        var resManagerGO = MakeRootGO("ResultsManager");
        var resUI        = resManagerGO.AddComponent<ResultsUI>();
        var soRes        = new UnityEditor.SerializedObject(resUI);

        Prop(soRes, "resultTitle",       titleTMP);
        Prop(soRes, "subtitleText",      subtitleTMP);
        Prop(soRes, "scoreBigText",      scoreBigTMP);
        Prop(soRes, "totalCountText",    totalRow);
        Prop(soRes, "correctCountText",  correctRow);
        Prop(soRes, "wrongCountText",    wrongRow);
        Prop(soRes, "avgTimeText",       timeRow);
        Prop(soRes, "categoriesContent", catsContent.transform);
        Prop(soRes, "bestScoreText",     bestTMP);
        Prop(soRes, "btnPlayAgain",      btnPlayAgain.GetComponent<Button>());
        Prop(soRes, "btnMainMenu",       btnMainMenu.GetComponent<Button>());
        Prop(soRes, "btnShare",          btnShare.GetComponent<Button>());
        soRes.ApplyModifiedProperties();

        SaveScene("Assets/Scenes/Results.unity");
        Debug.Log("[GameSceneBuilder] ✓ Results сцена построена.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    static GameObject MakeCard(string name, Transform parent)
    {
        var card = MakeGO(name, parent);
        card.AddComponent<Image>().color = C_CARD_BG;
        var outline = card.AddComponent<Outline>();
        outline.effectColor    = C_CARD_BORDER;
        outline.effectDistance = new Vector2(1, -1);
        return card;
    }

    // Строка статистики: иконка | подпись | значение. Возвращает TMP_Text значения.
    static TMP_Text MakeStatRow(string name, Transform parent, TMP_FontAsset font, string defaultLabel, string locKey)
    {
        var row = MakeGO(name, parent);
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight = rowLE.preferredHeight = 56;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.spacing = 16;

        // Иконка-кружок зелёный
        var iconGO = MakeGO("Icon", row.transform);
        SetLE(iconGO, minW: 48, minH: 48, prefH: 48);
        iconGO.AddComponent<Image>().color = C_ICON_FILL;

        // Подпись
        var labelGO = MakeGO("Label", row.transform);
        var labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.flexibleWidth = 1;
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text     = defaultLabel;
        labelTMP.fontSize = 30;
        labelTMP.color    = C_TEXT_DARK;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) labelTMP.font = font;
        AddLocKey(labelGO, locKey);

        // Значение справа
        var valueGO = MakeGO("Value", row.transform);
        var valueLE = valueGO.AddComponent<LayoutElement>();
        valueLE.minWidth = 110;
        var valueTMP = valueGO.AddComponent<TextMeshProUGUI>();
        valueTMP.text     = "0";
        valueTMP.fontSize = 30;
        valueTMP.color    = C_TEXT_DARK;
        valueTMP.alignment = TextAlignmentOptions.MidlineRight;
        valueTMP.fontStyle = FontStyles.Bold;
        if (font != null) valueTMP.font = font;

        return valueTMP;
    }
}
