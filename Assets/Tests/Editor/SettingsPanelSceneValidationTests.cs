using System.Collections.Generic;
using System.Reflection;
using Game.Scripts.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SettingsPanelSceneValidationTests
{
    private const string ScenePath = "Assets/Game/Scenes/Game.unity";

    [Test]
    public void GameScene_SettingsButtonPanelAndGameplaySpeedDropdownAreWired()
    {
        SceneYamlTestUtility scene = SceneYamlTestUtility.Load(ScenePath);
        string menuViewBlock = scene.FindRequiredBlockContaining("m_EditorClassIdentifier: Assembly-CSharp::Game.Scripts.UI.MenuView");

        string settingsButtonId = scene.GetRequiredFieldFileId(menuViewBlock, "buttonSettings");
        string settingsPanelId = scene.GetRequiredFieldFileId(menuViewBlock, "panelSettings");
        string gameplaySpeedDropdownId = scene.GetRequiredFieldFileId(menuViewBlock, "gameplaySpeedDropdown");

        Assert.AreEqual("Button_Settings", scene.GetRequiredGameObjectNameForReference(settingsButtonId));
        Assert.AreEqual("Panel_Settings", scene.GetRequiredGameObjectNameForReference(settingsPanelId));
        Assert.AreEqual("Dropdown_GameplaySpeed", scene.GetRequiredGameObjectNameForReference(gameplaySpeedDropdownId));
        Assert.IsFalse(scene.GetRequiredActiveStateForReference(settingsPanelId));
        Assert.DoesNotThrow(() => scene.FindRequiredBlockContaining("m_Name: Panel_Example_Settings"));
        scene.AssertPersistentCallsAreEmpty(settingsButtonId, "Button_Settings");

        IReadOnlyList<string> gameplaySpeedOptions = scene.GetDropdownOptionTexts(gameplaySpeedDropdownId);
        Assert.AreEqual(12, gameplaySpeedOptions.Count);
        Assert.AreEqual("1x", gameplaySpeedOptions[0]);
        Assert.AreEqual("1.25x", gameplaySpeedOptions[1]);
        Assert.AreEqual("1.5x", gameplaySpeedOptions[2]);
        Assert.AreEqual("10x", gameplaySpeedOptions[11]);

        AssertDropdown(scene, menuViewBlock, "aiDifficultyDropdown", "Dropdown_AIDifficulty", 4, "Easy", "Brutal");
        AssertDropdown(scene, menuViewBlock, "aiStartingMoneyDropdown", "Dropdown_AIStartingMoney", 3, "Low", "High");
        AssertDropdown(scene, menuViewBlock, "aiIncomeMultiplierDropdown", "Dropdown_AIIncomeMultiplier", 5, "0.75x", "2x");
        AssertDropdown(scene, menuViewBlock, "aiBuildSpeedDropdown", "Dropdown_AIBuildSpeed", 3, "Slow", "Fast");
        AssertDropdown(scene, menuViewBlock, "aiUnitProductionSpeedDropdown", "Dropdown_AIUnitProductionSpeed", 3, "Slow", "Fast");
        AssertDropdown(scene, menuViewBlock, "aiAttackGroupSizeDropdown", "Dropdown_AIAttackGroupSize", 3, "Small", "Large");
        AssertDropdown(scene, menuViewBlock, "aiAttackFrequencyDropdown", "Dropdown_AIAttackFrequency", 3, "Rare", "Frequent");
        AssertDropdown(scene, menuViewBlock, "aiAggressionDropdown", "Dropdown_AIAggression", 3, "Defensive", "Aggressive");
        AssertDropdown(scene, menuViewBlock, "aiExpansionDropdown", "Dropdown_AIExpansion", 4, "Off", "Fast");
        AssertDropdown(scene, menuViewBlock, "aiTargetPriorityDropdown", "Dropdown_AITargetPriority", 4, "Balanced", "Production");
        AssertDropdown(scene, menuViewBlock, "aiPlayerAutoDropdown", "Dropdown_AIPlayerAuto", 2, "Off", "On");
        AssertDropdown(scene, menuViewBlock, "aiEnemyCountDropdown", "Dropdown_AIEnemyCount", 3, "1", "3");
    }

    [Test]
    public void GameScene_FpsLabelIsWiredAndUpdatedByMenuView()
    {
        SceneYamlTestUtility scene = SceneYamlTestUtility.Load(ScenePath);
        string menuViewBlock = scene.FindRequiredBlockContaining("m_EditorClassIdentifier: Assembly-CSharp::Game.Scripts.UI.MenuView");
        string fpsTextId = scene.GetRequiredFieldFileId(menuViewBlock, "fpsText");

        Assert.AreEqual("Label_FPS", scene.GetRequiredGameObjectNameForReference(fpsTextId));

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

    private static void AssertDropdown(
        SceneYamlTestUtility scene,
        string menuViewBlock,
        string fieldName,
        string expectedName,
        int optionCount,
        string firstOption,
        string lastOption)
    {
        string dropdownId = scene.GetRequiredFieldFileId(menuViewBlock, fieldName);
        IReadOnlyList<string> options = scene.GetDropdownOptionTexts(dropdownId);

        Assert.AreEqual(expectedName, scene.GetRequiredGameObjectNameForReference(dropdownId));
        Assert.AreEqual(optionCount, options.Count);
        Assert.AreEqual(firstOption, options[0]);
        Assert.AreEqual(lastOption, options[optionCount - 1]);
    }
}
