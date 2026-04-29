using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// UstAldan Quiz → Adapt Menu for Mobile
/// Адаптирует MenuScreen под портретный телефон (720×1280).
/// Запускать ПОСЛЕ того как создан объект MenuScreen.
/// </summary>
public static class MenuMobileAdapter
{
    [MenuItem("UstAldan Quiz/Adapt Menu for Mobile")]
    public static void Adapt()
    {
        FixCanvasScaler();

        var menuGO = GameObject.Find("MenuScreen");
        if (menuGO == null)
        {
            Debug.LogWarning("[MenuAdapter] Объект 'MenuScreen' не найден в сцене.");
            return;
        }

        FixMenuScreen(menuGO);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[MenuAdapter] ✓ MenuScreen адаптирован для портретного телефона. " +
                  "Сохрани сцену Ctrl+S.\n" +
                  "Если порядок панелей в иерархии не совпадает с нужным — " +
                  "переставь их вручную в окне Hierarchy (сверху вниз = отображается сверху вниз).");
    }

    static void FixCanvasScaler()
    {
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null) return;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler == null) return;
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.Expand;
    }

    static void FixMenuScreen(GameObject menuGO)
    {
        // Растянуть MenuScreen на весь Canvas
        var rt = menuGO.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // VLG на MenuScreen — дети складываются сверху вниз
        var vlg = menuGO.GetComponent<VerticalLayoutGroup>()
                  ?? menuGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.padding = new RectOffset(24, 24, 60, 40);
        vlg.spacing = 20;

        foreach (Transform child in menuGO.transform)
        {
            var childRT = child.GetComponent<RectTransform>();
            if (childRT == null) continue;

            if (child.name == "Background")
            {
                // Фон — вне layout, растянуть на весь экран
                var le = child.GetComponent<LayoutElement>()
                         ?? child.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout  = true;
                childRT.anchorMin = Vector2.zero;
                childRT.anchorMax = Vector2.one;
                childRT.offsetMin = Vector2.zero;
                childRT.offsetMax = Vector2.zero;
                continue;
            }

            // Остальные панели — LayoutElement
            var panelLE = child.GetComponent<LayoutElement>()
                          ?? child.gameObject.AddComponent<LayoutElement>();
            panelLE.flexibleWidth = 1;

            switch (child.name)
            {
                case "CategoryPanel":
                    panelLE.minHeight       = 180;
                    panelLE.preferredHeight = 340;
                    panelLE.flexibleHeight  = 1f;
                    break;
                case "MainPanel":
                    panelLE.minHeight       = 100;
                    panelLE.preferredHeight = 200;
                    panelLE.flexibleHeight  = 0.5f;
                    break;
                case "BasementPanel":
                    panelLE.minHeight       = 80;
                    panelLE.preferredHeight = 120;
                    panelLE.flexibleHeight  = 0f;
                    break;
                default:
                    panelLE.minHeight       = 80;
                    panelLE.preferredHeight = 180;
                    panelLE.flexibleHeight  = 0.5f;
                    break;
            }

            // Убрать абсолютную позицию — layout сам расставит
            childRT.anchorMin = Vector2.zero;
            childRT.anchorMax = new Vector2(1f, 0f);
            childRT.offsetMin = Vector2.zero;
            childRT.offsetMax = Vector2.zero;
        }
    }
}
