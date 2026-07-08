using System;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchHudAssistantUiSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.BindMatchHudAssistant_CreatesButtonPanelAndBlocksWorldClicks());
            passed++;

            Debug.Log($"[MatchHudAssistantUiValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[MatchHudAssistantUiValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<MatchHudAssistantUiSystemHelperTests> testCase)
    {
        var tests = new MatchHudAssistantUiSystemHelperTests();
        try
        {
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
        }
    }

    [TearDown]
    public void TearDown()
    {
        GameObject[] roots = GameObject.FindGameObjectsWithTag("Untagged");
        for (int i = roots.Length - 1; i >= 0; i--)
        {
            if (roots[i] != null && roots[i].name.StartsWith("AssistantUiTest", StringComparison.Ordinal))
                UnityEngine.Object.DestroyImmediate(roots[i]);
        }
    }

    [Test]
    public void BindMatchHudAssistant_CreatesButtonPanelAndBlocksWorldClicks()
    {
        RectTransform overlay = CreateRectRoot("AssistantUiTestOverlay", new Vector2(1920f, 1080f));
        RectTransform header = CreateRect("HeaderContent", overlay);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(0f, 1f);
        header.pivot = new Vector2(0f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(1920f, 160f);

        var runtimeState = new FakeMatchRuntimeState();
        var ui = new MainMenuPlayUI();
        ui.Init(null, runtimeState);
        ui.BindMatchHudAssistant(header.gameObject, overlay);

        RectTransform button = header.Find("AriaAssistantButton") as RectTransform;
        RectTransform panel = overlay.Find("AriaAssistantPanel") as RectTransform;
        Assert.NotNull(button, "Binding the match HUD assistant must add the ARIA header button.");
        Assert.NotNull(panel, "Binding the match HUD assistant must add the ARIA panel shell.");
        Assert.IsFalse(panel.gameObject.activeSelf, "The ARIA panel should start closed.");

        Button buttonComponent = button.GetComponent<Button>();
        Assert.NotNull(buttonComponent, "The ARIA header button must be a Unity Button.");
        buttonComponent.onClick.Invoke();

        Assert.IsTrue(panel.gameObject.activeSelf, "Clicking ARIA should open the panel shell.");
        Assert.IsTrue(runtimeState.SuppressNextWorldClick, "ARIA UI clicks must suppress the next world click.");

        Vector2 buttonPoint = CenterScreenPoint(button);
        Assert.IsTrue(ui.IsPointerOverAnyGameplayUi(buttonPoint, out string source));
        Assert.AreEqual("MatchHudAssistant", source);

        ui.Dispose();
        UnityEngine.Object.DestroyImmediate(overlay.gameObject);
    }

    private static RectTransform CreateRectRoot(string name, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        return rect;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        return root.GetComponent<RectTransform>();
    }

    private static Vector2 CenterScreenPoint(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        return RectTransformUtility.WorldToScreenPoint(null, center);
    }

    private sealed class FakeMatchRuntimeState : IMatchRuntimeState
    {
        public bool PlayRequested { get; set; }
        public bool SimulationActive { get; set; }
        public bool SelectionModeActive { get; set; }
        public bool BuildModeActive { get; set; }
        public bool ZoomInHeld { get; set; }
        public bool ZoomOutHeld { get; set; }
        public bool SuppressNextWorldClick { get; set; }
    }
}
