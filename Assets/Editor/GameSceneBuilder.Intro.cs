using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UstAldanQuiz.UI;

public static partial class GameSceneBuilder
{
    // =====================================================================
    // 0. ИНТРО-СЦЕНА
    // =====================================================================

    static void DoBuildIntro(bool skipIfExists = false)
    {
        if (skipIfExists && File.Exists("Assets/Scenes/Intro.unity"))
        { Debug.Log("[GameSceneBuilder] Intro.unity уже существует — пропускаем."); return; }

        OpenOrCreateScene("Assets/Scenes/Intro.unity");

        // Камера — чёрный фон
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.cullingMask     = 0;
        cam.depth           = -1;
        camGO.AddComponent<AudioListener>();

        SetupEventSystem();

        // Canvas
        var canvasGO = SetupCanvas("Intro");

        // Чёрный фон (пока RenderTexture не создана)
        var bg = MakeGO("Background", canvasGO.transform);
        Stretch(bg);
        bg.AddComponent<Image>().color = Color.black;

        // RawImage — сюда VideoPlayer рендерит кадры
        var videoDisplayGO = MakeGO("VideoDisplay", canvasGO.transform);
        Stretch(videoDisplayGO);
        var rawImage = videoDisplayGO.AddComponent<RawImage>();
        rawImage.color = Color.white;

        // Подсказка «нажмите чтобы пропустить»
        var font    = FindFont();
        var skipTMP = MakeTMP("SkipHint", canvasGO.transform,
                              "Нажмите, чтобы пропустить", 28,
                              new Color(1f, 1f, 1f, 0.55f), font);
        var skipRT = skipTMP.GetComponent<RectTransform>();
        skipRT.anchorMin = new Vector2(0f, 0f);
        skipRT.anchorMax = new Vector2(1f, 0f);
        skipRT.pivot     = new Vector2(0.5f, 0f);
        skipRT.anchoredPosition = new Vector2(0f, 60f);
        skipRT.sizeDelta        = new Vector2(0f, 50f);
        skipTMP.alignment = TextAlignmentOptions.Center;

        // VideoPlayer
        var vpGO        = new GameObject("VideoPlayer");
        var videoPlayer = vpGO.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake   = false;
        videoPlayer.renderMode    = VideoRenderMode.RenderTexture; // текстура задаётся в IntroUI.Start
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.waitForFirstFrame = true;

        // Ищем видеоклип в Assets/Videos/
        VideoClip clip = null;
        var clipGuids = AssetDatabase.FindAssets("t:VideoClip", new[] { "Assets/Videos" });
        if (clipGuids.Length > 0)
            clip = AssetDatabase.LoadAssetAtPath<VideoClip>(AssetDatabase.GUIDToAssetPath(clipGuids[0]));
        else
            Debug.LogWarning("[GameSceneBuilder] Видеофайл не найден в Assets/Videos/. Добавь его вручную.");

        // IntroUI
        var introGO = new GameObject("IntroManager");
        var introUI = introGO.AddComponent<IntroUI>();
        var soIntro = new UnityEditor.SerializedObject(introUI);
        Prop(soIntro, "videoPlayer",  videoPlayer);
        Prop(soIntro, "videoDisplay", rawImage);
        if (clip != null) Prop(soIntro, "introClip", clip);
        soIntro.ApplyModifiedProperties();

        SaveScene("Assets/Scenes/Intro.unity");
        Debug.Log("[GameSceneBuilder] ✓ Intro сцена построена.");
    }
}
