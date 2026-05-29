#if UNITY_EDITOR
using System;
using Game.Scripts.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WarlineCaptureMenuDiagnosticsInstaller
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string DiagnosticsRootName = "MenuDiagnosticsPanel";
    private const string FpsPanelName = "Panel_FPS";
    private const string LogPanelName = "Panel_Log";

    public static void InstallMenuDiagnosticsPanel()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        Canvas canvas = FindSceneComponent<Canvas>(scene);
        if (canvas == null)
            throw new InvalidOperationException($"{MenuScenePath} must contain a Canvas before installing menu diagnostics.");

        RemoveExisting(canvas.transform, DiagnosticsRootName);
        RemoveExisting(canvas.transform, LogPanelName);

        GameObject diagnosticsRoot = CreateUiObject(DiagnosticsRootName, canvas.transform);
        RectTransform diagnosticsRect = diagnosticsRoot.GetComponent<RectTransform>();
        Stretch(diagnosticsRect);
        diagnosticsRoot.transform.SetAsLastSibling();

        GameObject logPanel = CreateLogPanel(canvas.transform);
        GameObject fpsPanel = CreateFpsPanel(diagnosticsRoot.transform, out Button fpsButton, out TMP_Text fpsText);
        Button closeButton = CreateCloseButton(logPanel.transform);
        ScrollRect scrollRect = CreateLogScroll(logPanel.transform, out TMP_Text logText);

        MenuDiagnosticsView diagnosticsView = diagnosticsRoot.AddComponent<MenuDiagnosticsView>();
        diagnosticsView.Configure(fpsButton, fpsText, logPanel, logText, scrollRect, closeButton);
        logPanel.SetActive(false);
        fpsPanel.transform.SetAsLastSibling();
        logPanel.transform.SetAsLastSibling();

        if (!EditorSceneManager.SaveScene(scene, MenuScenePath))
            throw new InvalidOperationException($"Failed to save {MenuScenePath} after installing menu diagnostics.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_MENU_DIAGNOSTICS_INSTALLED scene={MenuScenePath}");
    }

    public static void ValidateMenuDiagnosticsPanel()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        MenuDiagnosticsView diagnosticsView = FindSceneComponent<MenuDiagnosticsView>(scene);
        if (diagnosticsView == null)
            throw new InvalidOperationException($"{MenuScenePath} must contain MenuDiagnosticsView.");

        if (diagnosticsView.FpsButton == null ||
            diagnosticsView.FpsText == null ||
            diagnosticsView.LogPanel == null ||
            diagnosticsView.LogText == null ||
            diagnosticsView.LogScrollRect == null ||
            diagnosticsView.CloseButton == null)
        {
            throw new InvalidOperationException("MenuDiagnosticsView must have all serialized references assigned.");
        }

        RectTransform fpsRect = diagnosticsView.FpsButton.GetComponent<RectTransform>();
        if (fpsRect == null ||
            fpsRect.anchorMin != new Vector2(0.5f, 0f) ||
            fpsRect.anchorMax != new Vector2(0.5f, 0f) ||
            fpsRect.pivot != new Vector2(0.5f, 0f))
        {
            throw new InvalidOperationException("Panel_FPS must stay anchored bottom-center like the legacy Match canvas FPS panel.");
        }

        Debug.Log($"WARLINECAPTURE_MENU_DIAGNOSTICS_VALIDATED scene={MenuScenePath}");
    }

    private static GameObject CreateFpsPanel(Transform parent, out Button button, out TMP_Text label)
    {
        GameObject panel = CreateUiObject(FpsPanelName, parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(220f, 80f);

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.02f, 0.025f, 0.03f, 0.82f);

        button = panel.AddComponent<Button>();
        button.targetGraphic = background;

        GameObject labelObject = CreateUiObject("Label_FPS", panel.transform);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        Stretch(labelRect);
        label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "0";
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 40f;
        label.color = new Color(0.9f, 0.96f, 1f, 1f);

        return panel;
    }

    private static GameObject CreateLogPanel(Transform parent)
    {
        GameObject panel = CreateUiObject(LogPanelName, parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        Stretch(rect);

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.86f);
        return panel;
    }

    private static Button CreateCloseButton(Transform parent)
    {
        GameObject close = CreateUiObject("CloseButton", parent);
        RectTransform rect = close.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-28f, -28f);
        rect.sizeDelta = new Vector2(84f, 64f);

        Image background = close.AddComponent<Image>();
        background.color = new Color(0.12f, 0.16f, 0.2f, 0.95f);
        Button button = close.AddComponent<Button>();
        button.targetGraphic = background;

        GameObject labelObject = CreateUiObject("Label_Close", close.transform);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        Stretch(labelRect);
        TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "X";
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 30f;
        label.color = Color.white;
        return button;
    }

    private static ScrollRect CreateLogScroll(Transform parent, out TMP_Text logText)
    {
        GameObject scrollObject = CreateUiObject("LogScroll", parent);
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(64f, 64f);
        scrollRectTransform.offsetMax = new Vector2(-64f, -112f);

        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateUiObject("Viewport", scrollObject.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        scrollRect.viewport = viewportRect;

        GameObject content = CreateUiObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 720f);
        scrollRect.content = contentRect;

        GameObject labelObject = CreateUiObject("Label_Log", content.transform);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0f, 720f);

        logText = labelObject.AddComponent<TextMeshProUGUI>();
        logText.text = string.Empty;
        logText.richText = true;
        logText.alignment = TextAlignmentOptions.TopLeft;
        logText.textWrappingMode = TextWrappingModes.Normal;
        logText.overflowMode = TextOverflowModes.Overflow;
        logText.fontSize = 28f;
        logText.color = Color.white;
        return scrollRect;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static void RemoveExisting(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
#endif
