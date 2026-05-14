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
    // 4. СЦЕНА РЕЗУЛЬТАТОВ
    // =====================================================================

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
        Stretch(bg); bg.AddComponent<Image>().color = C_BG;

        var safeArea = MakeGO("SafeArea", canvasGO.transform);
        Stretch(safeArea); safeArea.AddComponent<SafeArea>();

        var vlg = safeArea.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.padding = new RectOffset(60, 60, 80, 60);
        vlg.spacing = 32;

        // Заголовок
        var titleTMP = MakeTMP("ResultTitle", safeArea.transform, "Отлично!", 64, C_PRIMARY, font, minH: 90, bold: true);
        titleTMP.alignment = TextAlignmentOptions.Center;

        // ScoreCircle
        var circleGO = MakeGO("ScoreCircle", safeArea.transform);
        SetLE(circleGO, minH: 220, prefH: 220);
        var circleImg = circleGO.AddComponent<Image>();
        circleImg.color = C_PRIMARY;
        var circleRT = circleGO.GetComponent<RectTransform>();
        circleRT.anchorMin = circleRT.anchorMax = new Vector2(0.5f, 0);
        var circleTMP = MakeTMP("ScoreCircleText", circleGO.transform, "0/15", 60, Color.white, font, bold: true);
        var cRT = circleTMP.GetComponent<RectTransform>();
        cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        circleTMP.alignment = TextAlignmentOptions.Center;

        // StarsRow
        var starsRow = MakeGO("StarsRow", safeArea.transform);
        SetLE(starsRow, minH: 100, prefH: 100);
        var starsHLG = starsRow.AddComponent<HorizontalLayoutGroup>();
        starsHLG.childAlignment = TextAnchor.MiddleCenter;
        starsHLG.spacing = 24;
        starsHLG.childForceExpandWidth = false;
        starsHLG.childControlWidth = starsHLG.childControlHeight = true;

        var starImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var starGO  = MakeGO($"Star_{i}", starsRow.transform);
            SetLE(starGO, minH: 80, minW: 80);
            starImages[i] = starGO.AddComponent<Image>();
            starImages[i].color = new Color(0.78f, 0.66f, 0.29f);
        }

        // ScoreText
        var scoreTMP = MakeTMP("ScoreText", safeArea.transform,
            "Вы ответили правильно на 0 из 15 вопросов", 32, C_TEXT, font, minH: 60);
        scoreTMP.alignment = TextAlignmentOptions.Center;
        scoreTMP.enableWordWrapping = true;

        // BestScore
        var bestTMP = MakeTMP("BestScoreText", safeArea.transform,
            "Лучший результат: 0/15", 30, C_TEXT2, font, minH: 50);
        bestTMP.alignment = TextAlignmentOptions.Center;

        // NewBestBadge
        var badgeGO = MakeGO("NewBestBadge", safeArea.transform);
        SetLE(badgeGO, minH: 80, prefH: 80);
        badgeGO.AddComponent<Image>().color = C_SECONDARY;
        var badgeTMP = MakeTMP("BadgeText", badgeGO.transform, "Новый рекорд!", 34, Color.white, font);
        var badgeTMPRT = badgeTMP.GetComponent<RectTransform>();
        badgeTMPRT.anchorMin = Vector2.zero; badgeTMPRT.anchorMax = Vector2.one;
        badgeTMPRT.offsetMin = badgeTMPRT.offsetMax = Vector2.zero;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeGO.SetActive(false);

        // Кнопки
        var btnPlayAgain = MakePrimaryButton("BtnPlayAgain",   safeArea.transform, "Играть снова",  font);
        var btnMainMenu  = MakeSecondaryButton("BtnMainMenu",  safeArea.transform, "Главное меню",  font);
        var btnShare     = MakeSecondaryButton("BtnShare",     safeArea.transform, "Поделиться",    font);
        SetLE(btnPlayAgain, minH: 110, prefH: 110);
        SetLE(btnMainMenu,  minH: 100, prefH: 100);
        SetLE(btnShare,     minH: 90,  prefH: 90);
        AddLocKey(btnPlayAgain, "btn_play_again");
        AddLocKey(btnMainMenu,  "btn_main_menu");
        AddLocKey(btnShare,     "btn_share");

        // ResultsUI
        var resManagerGO = MakeRootGO("ResultsManager");
        var resUI        = resManagerGO.AddComponent<ResultsUI>();
        var soRes        = new UnityEditor.SerializedObject(resUI);

        Prop(soRes, "resultTitle",     titleTMP);
        Prop(soRes, "scoreCircleText", circleTMP);
        Prop(soRes, "scoreText",       scoreTMP);
        Prop(soRes, "bestScoreText",   bestTMP);
        Prop(soRes, "newBestBadge",    badgeGO);
        Prop(soRes, "btnPlayAgain",    btnPlayAgain.GetComponent<Button>());
        Prop(soRes, "btnMainMenu",     btnMainMenu.GetComponent<Button>());
        Prop(soRes, "btnShare",        btnShare.GetComponent<Button>());
        SetArr(soRes, "stars", starImages);
        soRes.ApplyModifiedProperties();

        SaveScene("Assets/Scenes/Results.unity");
        Debug.Log("[GameSceneBuilder] ✓ Results сцена построена.");
    }
}
