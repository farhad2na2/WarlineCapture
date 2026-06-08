using System.Reflection;
using Game.Scripts.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SettingsPanelSceneValidationTests
{
    private const string ScenePath = "Assets/Game/Scenes/Match.unity";

    [Test]
    public void MatchScene_DoesNotContainLegacyMenuViewSettingsPanel()
    {
        SceneYamlTestUtility scene = SceneYamlTestUtility.Load(ScenePath);
        Assert.Throws<AssertionException>(() => scene.FindRequiredBlockContaining("m_EditorClassIdentifier: Assembly-CSharp::Game.Scripts.UI.MenuView"));
        Assert.Throws<AssertionException>(() => scene.FindRequiredBlockContaining("m_Name: UI_Canvas"));
    }

    [Test]
    public void MenuView_FpsLabelFormatsValue()
    {
        var menuViewObject = new GameObject("MenuViewTest");
        var labelObject = new GameObject("FpsLabelTest");

        try
        {
            MenuView menuView = menuViewObject.AddComponent<MenuView>();
            TMP_Text fpsText = labelObject.AddComponent<TextMeshProUGUI>();

            menuView.fpsText = fpsText;
            menuView.SetFpsLabel(61);

            Assert.AreEqual("61", fpsText.text);
        }
        finally
        {
            Object.DestroyImmediate(labelObject);
            Object.DestroyImmediate(menuViewObject);
        }
    }

    [Test]
    public void MenuView_FpsPanelTogglesRuntimeLogPanelAndFormatsWarningsAndErrors()
    {
        var root = new GameObject("UI_Canvas", typeof(RectTransform));
        var panelMain = new GameObject("Panel_Main", typeof(RectTransform));
        var panelFps = new GameObject("Panel_FPS", typeof(RectTransform));
        var panelLog = new GameObject("Panel_Log", typeof(RectTransform));
        var scrollView = new GameObject("Scroll View", typeof(RectTransform), typeof(ScrollRect));
        var viewport = new GameObject("Viewport", typeof(RectTransform));
        var content = new GameObject("Content", typeof(RectTransform));
        var label = new GameObject("Label_Log", typeof(RectTransform));

        try
        {
            panelMain.transform.SetParent(root.transform);
            panelFps.transform.SetParent(panelMain.transform);
            panelLog.transform.SetParent(panelMain.transform);
            scrollView.transform.SetParent(panelLog.transform);
            viewport.transform.SetParent(scrollView.transform);
            content.transform.SetParent(viewport.transform);
            label.transform.SetParent(content.transform);
            ((RectTransform)viewport.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 320f);
            ((RectTransform)viewport.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 120f);

            ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = (RectTransform)content.transform;
            TMP_Text logText = label.AddComponent<TextMeshProUGUI>();
            MenuView menuView = root.AddComponent<MenuView>();
            InvokePrivate(menuView, "ResolveRuntimeLogPanel");

            Assert.IsFalse(panelLog.activeSelf);
            Assert.NotNull(panelFps.GetComponent<EventTrigger>());

            InvokePrivate(menuView, "HandleRuntimeLogMessage", "boot warning", string.Empty, LogType.Warning);
            InvokePrivate(menuView, "HandleRuntimeLogMessage", "boot error", string.Empty, LogType.Error);
            InvokePrivate(menuView, "ToggleRuntimeLogPanel");

            Assert.IsTrue(panelLog.activeSelf);
            StringAssert.Contains("<color=#FFA500>[Warning] boot warning</color>", logText.text);
            StringAssert.Contains("<color=#FF4040>[Error] boot error</color>", logText.text);
            StringAssert.Contains("\n\n", logText.text);
            Assert.AreEqual(0f, scrollRect.verticalNormalizedPosition, 0.001f);

            InvokePrivate(menuView, "ToggleRuntimeLogPanel");

            Assert.IsFalse(panelLog.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method, methodName);
        method.Invoke(target, args);
    }

}
