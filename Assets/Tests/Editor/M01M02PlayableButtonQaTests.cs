#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RuntimeSettingsService = Game.UI.Runtime.SettingsService;

public sealed class M01M02PlayableButtonQaTests
{
    private const string Marker =
        "[M01M02PlayableButtonQaValidation] result=Passed suites=28 buttonPrefabs=10 directInteractions=3";

    private static readonly string[] ButtonPrefabPaths =
    {
        "Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchLanguageChoice.prefab",
        "Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchNarrativeSequence.prefab",
        "Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab",
        "Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab",
        "Assets/Game/Prefabs/UI/Shell/Content/SCN07_LoadoutSquadPrepContent.prefab",
        "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab",
        "Assets/Game/Prefabs/UI/Shell/Popups/SCN08_FullMapPopup.prefab",
        "Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab",
        "Assets/Game/Prefabs/UI/Shell/Popups/SCN_SettingsPopup.prefab",
        "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab"
    };

    private int auditedButtonCount;

    public static void RunButtonInventoryFocusedValidation()
    {
        try
        {
            M01M02PlayableButtonQaTests tests = new();
            tests.AllM01M02ButtonPrefabsExposePointerTargets();
            Debug.Log(
                $"[M01M02ButtonInventoryValidation] result=Passed prefabs={ButtonPrefabPaths.Length} " +
                $"buttons={tests.auditedButtonCount} pointerTargets=Passed");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M01M02ButtonInventoryValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunFocusedValidation()
    {
        try
        {
            M01M02PlayableButtonQaTests tests = new();
            tests.AllM01M02ButtonPrefabsExposePointerTargets();
            tests.SettingsButtonsSwitchPagesAndInvokeActions();
            tests.PauseButtonsOpenAndCloseOwnedPanels();
            tests.MissionResultButtonsInvokeExactlyOneBoundAction();

            var suites = new (string Name, Action Run)[]
            {
                (nameof(FirstLaunchNarrativeMenuIntegrationTests), FirstLaunchNarrativeMenuIntegrationTests.RunFocusedValidation),
                (nameof(NarrativeInteractiveViewsTests), NarrativeInteractiveViewsTests.RunFocusedValidation),
                (nameof(M01FirstContactCampaignUiTests), M01FirstContactCampaignUiTests.RunFocusedValidation),
                (nameof(M01FirstContactMissionBriefingTests), M01FirstContactMissionBriefingTests.RunFocusedValidation),
                (nameof(LoadoutSquadPrepV3PrefabTests), LoadoutSquadPrepV3PrefabTests.RunFocusedValidation),
                (nameof(M01FirstContactLaunchBootstrapTests), M01FirstContactLaunchBootstrapTests.RunFocusedValidation),
                (nameof(M01GuidedMoveRouteTests), M01GuidedMoveRouteTests.RunTutorialFinaleFocusedValidation),
                (nameof(M01FirstContactDeterministicGameplayTests), M01FirstContactDeterministicGameplayTests.RunFocusedValidation),
                (nameof(M01FirstContactRuntimeOwnershipTests), M01FirstContactRuntimeOwnershipTests.RunFocusedValidation),
                (nameof(M01FirstContactSettlementTests), M01FirstContactSettlementTests.RunFocusedValidation),
                (nameof(MatchHudCommandControlsCurrentPrefabTests), MatchHudCommandControlsCurrentPrefabTests.RunFocusedValidation),
                (nameof(MatchHudAssistantUiSystemHelperTests), MatchHudAssistantUiSystemHelperTests.RunFocusedValidation),
                (nameof(PauseOptionsV3PrefabTests), PauseOptionsV3PrefabTests.RunFocusedValidation),
                (nameof(SettingsPopupValidationTests), SettingsPopupValidationTests.RunFocusedValidation),
                (nameof(M01FirstContactHudResultTests), M01FirstContactHudResultTests.RunFocusedValidation),
                (nameof(M02EstablishBaseCampaignUiTests), M02EstablishBaseCampaignUiTests.RunFocusedValidation),
                (nameof(M02EstablishBaseLaunchTests), M02EstablishBaseLaunchTests.RunFocusedValidation),
                (nameof(M02EstablishBaseObjectiveTests), M02EstablishBaseObjectiveTests.RunFocusedValidation),
                (nameof(M02EstablishBasePlacementTests), M02EstablishBasePlacementTests.RunFocusedValidation),
                (nameof(M02EstablishBaseProductionTests), M02EstablishBaseProductionTests.RunFocusedValidation),
                (nameof(M02EstablishBaseGuidanceTests), M02EstablishBaseGuidanceTests.RunFocusedValidation),
                (nameof(M02EstablishBaseDoItTests), M02EstablishBaseDoItTests.RunFocusedValidation),
                (nameof(M02EstablishBaseLifecycleTests), M02EstablishBaseLifecycleTests.RunFocusedValidation),
                (nameof(M02EstablishBaseResourceTests), M02EstablishBaseResourceTests.RunFocusedValidation),
                (nameof(M02EstablishBaseBarracksProductionTests), M02EstablishBaseBarracksProductionTests.RunFocusedValidation),
                (nameof(M02EstablishBaseWaveTests), M02EstablishBaseWaveTests.RunFocusedValidation),
                (nameof(M02EstablishBaseResultSettlementTests), M02EstablishBaseResultSettlementTests.RunFocusedValidation)
            };

            for (int index = 0; index < suites.Length; index++)
                RunSuite(suites[index].Name, suites[index].Run);

            RunSuite(nameof(M02EstablishBaseHudResultTests), M02EstablishBaseHudResultTests.RunFocusedValidation);
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M01M02PlayableButtonQaValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void AllM01M02ButtonPrefabsExposePointerTargets()
    {
        auditedButtonCount = 0;
        var failures = new List<string>();
        for (int prefabIndex = 0; prefabIndex < ButtonPrefabPaths.Length; prefabIndex++)
        {
            string path = ButtonPrefabPaths[prefabIndex];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.NotNull(prefab, path);
            Button[] buttons = prefab.GetComponentsInChildren<Button>(true);
            Assert.Greater(buttons.Length, 0, $"{path} must expose at least one button.");
            auditedButtonCount += buttons.Length;
            for (int buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
            {
                Button button = buttons[buttonIndex];
                if (button.targetGraphic == null)
                {
                    failures.Add($"{path}:{HierarchyPath(button.transform)} targetGraphic=null");
                    continue;
                }

                if (!button.targetGraphic.raycastTarget)
                    failures.Add($"{path}:{HierarchyPath(button.transform)} raycastTarget=false");
                if (!button.targetGraphic.transform.IsChildOf(button.transform) &&
                    button.targetGraphic.transform != button.transform)
                    failures.Add($"{path}:{HierarchyPath(button.transform)} targetGraphic outside button hierarchy");
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [Test]
    public void SettingsButtonsSwitchPagesAndInvokeActions()
    {
        const string path = "Assets/Game/Prefabs/UI/Shell/Popups/SCN_SettingsPopup.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        UISettingsModel previous = RuntimeSettingsService.Load();
        try
        {
            SettingsPopupView popup = instance.GetComponent<SettingsPopupView>();
            V3SettingsTabView tabs = instance.GetComponentInChildren<V3SettingsTabView>(true);
            Assert.NotNull(popup);
            Assert.NotNull(tabs);
            InvokeLifecycle(tabs, "Awake");
            InvokeLifecycle(popup, "Awake");
            Assert.AreEqual(4, tabs.TabButtons.Length);

            for (int index = 0; index < tabs.TabButtons.Length; index++)
            {
                tabs.TabButtons[index].onClick.Invoke();
                Assert.AreEqual(index, tabs.SelectedIndex, tabs.TabButtons[index].name);
                for (int pageIndex = 0; pageIndex < tabs.Pages.Length; pageIndex++)
                    Assert.AreEqual(index == pageIndex, tabs.Pages[pageIndex].activeSelf,
                        $"tab={index} page={pageIndex}");
            }

            int closes = 0;
            popup.BindClose(() => closes++);
            popup.ResetButton.onClick.Invoke();
            popup.ApplyButton.onClick.Invoke();
            popup.CloseButton.onClick.Invoke();
            Assert.AreEqual(1, closes);
        }
        finally
        {
            SettingsPopupView popup = instance != null ? instance.GetComponent<SettingsPopupView>() : null;
            V3SettingsTabView tabs = instance != null
                ? instance.GetComponentInChildren<V3SettingsTabView>(true)
                : null;
            InvokeLifecycle(popup, "OnDestroy");
            InvokeLifecycle(tabs, "OnDestroy");
            RuntimeSettingsService.Save(previous);
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void PauseButtonsOpenAndCloseOwnedPanels()
    {
        const string path = "Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            PauseOptionsV3PopupView view = instance.GetComponent<PauseOptionsV3PopupView>();
            Assert.NotNull(view);
            InvokeLifecycle(view, "OnEnable");
            Assert.IsFalse(view.RestartConfirmation.activeSelf);
            Assert.IsFalse(view.HelpPanel.activeSelf);

            view.RestartButton.onClick.Invoke();
            Assert.IsTrue(view.RestartConfirmation.activeSelf);
            FindButton(view.RestartConfirmation.transform, "RestartCancelButton").onClick.Invoke();
            Assert.IsFalse(view.RestartConfirmation.activeSelf);

            view.HelpButton.onClick.Invoke();
            Assert.IsTrue(view.HelpPanel.activeSelf);
            FindButton(view.HelpPanel.transform, "HelpCloseButton").onClick.Invoke();
            Assert.IsFalse(view.HelpPanel.activeSelf);
        }
        finally
        {
            PauseOptionsV3PopupView view = instance != null
                ? instance.GetComponent<PauseOptionsV3PopupView>()
                : null;
            InvokeLifecycle(view, "OnDisable");
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MissionResultButtonsInvokeExactlyOneBoundAction()
    {
        const string path = "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            MissionResultPopupView view = instance.GetComponent<MissionResultPopupView>();
            Assert.NotNull(view);
            InvokeLifecycle(view, "OnEnable");
            int primary = 0;
            int retry = 0;
            view.Bind(() => primary++, () => retry++);

            UiMissionResultPopupModel victory = new(
                1, "saga.ch01.m01.first_contact", UiMissionResultOutcome.Victory,
                "VICTORY", "FIRST CONTACT • OLD MARKET", "Sector secure.", 3,
                "03:14", "0", "3 / 3", "1,200 CREDITS", "CONTINUE", true, false);
            view.Apply(in victory);
            FindButton(instance.transform, "ContinueButton").onClick.Invoke();
            Assert.AreEqual(1, primary);
            Assert.AreEqual(0, retry);

            UiMissionResultPopupModel loss = new(
                2, "saga.ch01.m01.first_contact", UiMissionResultOutcome.Loss,
                "MISSION FAILED", "FIRST CONTACT • OLD MARKET", "Command squad lost.", 0,
                "01:05", "1", "1 / 3", "NO REWARD", "RETRY", true, true);
            view.Apply(in loss);
            FindButton(instance.transform, "ReplayButton").onClick.Invoke();
            Assert.AreEqual(1, primary);
            Assert.AreEqual(1, retry);
        }
        finally
        {
            MissionResultPopupView view = instance != null
                ? instance.GetComponent<MissionResultPopupView>()
                : null;
            InvokeLifecycle(view, "OnDisable");
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void RunSuite(string name, Action suite)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
            suite();
        if (ValidationExit.LastExitCode is int exitCode && exitCode != 0)
            throw new InvalidOperationException($"{name} failed with exit code {exitCode}.");
        Debug.Log($"[M01M02PlayableButtonQaValidation] suite={name} result=Passed");
    }

    private static Button FindButton(Transform root, string name)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int index = 0; index < buttons.Length; index++)
            if (buttons[index].name == name)
                return buttons[index];
        throw new MissingReferenceException($"Missing button {name} under {root.name}.");
    }

    private static string HierarchyPath(Transform target)
    {
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }
        return path;
    }

    private static void InvokeLifecycle(object target, string methodName)
    {
        if (target == null)
            return;
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method, $"{target.GetType().Name}.{methodName}");
        method.Invoke(target, null);
    }
}
#endif
