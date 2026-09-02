using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Tactical.Contracts;
using Game.Narrative.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MatchHudAssistantUiSystemHelperTests
{
    private const string PopupPrefabPath =
        "Assets/Game/Prefabs/UI/Shell/Popups/POP13_ARIACommandAssistantPopup.prefab";
    private const string MatchHudPrefabPath =
        "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string AriaPortraitPath =
        "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/portrait_aria_v3.png";
    private bool _openedScene;

    [UnityEditor.MenuItem("Game/Validation/Run ARIA Tutorial Briefing Focused")]
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.PopupPrefab_BindsLockedLandscapeHierarchyAndMenuReference());
            passed++;
            RunCase(test => test.TutorialBriefing_RemainsVisibleThroughEachInstructionAndResetsForReplay());
            passed++;
            RunCase(test => test.TutorialBriefing_AutoShowsVisibleIndicatorForEveryInstruction());
            passed++;
            RunCase(test => test.TutorialNarration_MapsEveryM01StepToEnglishAndPersianEvents());
            passed++;
            RunCase(test => test.TutorialLocalization_MapsEveryM01StepToEnglishAndPersianText());
            passed++;
            RunCase(test => test.TutorialBriefing_PersianUsesRtlFontAndLocalizedSubsteps());
            passed++;
            RunCase(test => test.TutorialDoIt_SelectsCommandModeBeforeExecutingWorldOrder());
            passed++;
            RunCase(test => test.MatchHudPrefab_ContainsEditableAssistantButton());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_UsesPrefabButtonAndRestoresObjectives());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_MissingPrefabButtonDoesNotCreateRuntimeFallback());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_AppliesStructuredRowsWithoutCreatingPopupObjects());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_RebindPreservesPendingM02Action());
            passed++;
            RunCase(test => test.AssistantPanelUi_UnchangedModelApplicationsAllocateZeroManagedBytes());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_EnforcesPopupExclusivity());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_CloseEscapeAndStopHaveSeparateSemantics());
            passed++;
            RunCase(test => test.GuidedCommandHighlight_PointsAtCommandBeforeWorldTarget());
            passed++;
            RunCase(test => test.FirstShowMe_SelectSquad_ClosesPanelAndShowsVisibleIndicator());
            passed++;
            RunCase(test => test.FirstShowMe_SelectSquad_ShowsImmediateUiCueBeforeEcsHighlight());
            passed++;
            RunCase(test => test.MissionReplay_ResetClearsCompletedGuidanceState());
            passed++;
            RunCase(test => test.PopupContentVersionChangePreservesPendingGuidanceState());
            passed++;
            RunCase(test => test.MissionReplay_ShellReplacementClearsPendingGuidanceState());
            passed++;
            RunCase(test => test.DelayedEcsHighlight_RemainsPendingUntilCanonicalTargetOrMissionReset());
            passed++;
            RunCase(test => test.DiagnosticSuppressionState_IsOwnedByEachHudInstance());
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

    public static void RunDiagnosticOwnershipValidation()
    {
        MatchHudAssistantUiSystemHelperTests tests = new();
        try
        {
            tests.DiagnosticSuppressionState_IsOwnedByEachHudInstance();
            Debug.Log("[MatchHudAssistantDiagnosticOwnershipValidation] result=Passed");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchHudAssistantDiagnosticOwnershipValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    public static void RunActionLabelValidation()
    {
        MatchHudAssistantUiSystemHelperTests tests = new();
        try
        {
            tests.BindMatchHudAssistant_AppliesStructuredRowsWithoutCreatingPopupObjects();
            Debug.Log("[MatchHudAssistantActionLabelValidation] result=Passed tests=1");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchHudAssistantActionLabelValidation] result=Failed passed=0");
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
        UiShellRuntimeGateway.Register(null);
        GameObject[] roots = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = roots.Length - 1; i >= 0; i--)
        {
            GameObject root = roots[i];
            if (root == null || EditorUtility.IsPersistent(root))
                continue;
            if (root.name.StartsWith("AssistantUiTest", StringComparison.Ordinal) ||
                root.name.StartsWith("AriaAssistantPreviewHighlightRuntime", StringComparison.Ordinal) ||
                root.name.StartsWith("AriaAssistantTargetIndicatorRuntime", StringComparison.Ordinal))
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        if (_openedScene)
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        _openedScene = false;
    }

    [Test]
    public void DiagnosticSuppressionState_IsOwnedByEachHudInstance()
    {
        Type helperType = typeof(MatchHudAssistantUiSystemHelper);
        FieldInfo missingButton = helperType.GetField(
            "_loggedMissingButton",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo invalidButton = helperType.GetField(
            "_loggedInvalidButton",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(missingButton);
        Assert.NotNull(invalidButton);
        Assert.IsFalse(missingButton.IsStatic);
        Assert.IsFalse(invalidButton.IsStatic);
    }

    [Test]
    public void PopupPrefab_BindsLockedLandscapeHierarchyAndMenuReference()
    {
        GameObject prefab = LoadPopupPrefab();
        AriaCommandAssistantPopupView view = prefab.GetComponent<AriaCommandAssistantPopupView>();
        Assert.NotNull(view, "POP-13 prefab must own the focused ARIA popup view.");

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.NotNull(instance);
        instance.name = "AssistantUiTestPopupPrefab";
        view = instance.GetComponent<AriaCommandAssistantPopupView>();
        Assert.IsTrue(view.TryBindHierarchy(), "The view must bind every required stable prefab child.");
        Assert.AreEqual(new Vector2(1672f, 941f), view.LandscapeLayout.sizeDelta);
        Assert.AreEqual(Vector2.zero, view.LandscapeLayout.anchoredPosition);
        Assert.NotNull(view.CommandAssistantPanel);
        Assert.GreaterOrEqual(view.CommandAssistantPanel.anchoredPosition.x, 1100f);
        Assert.NotNull(view.LandscapeLayout.GetComponent<MainMenuV3SectionLayoutView>());
        Assert.NotNull(FindNamed(view.transform, "GoalRow0"));
        Assert.NotNull(FindNamed(view.transform, "AlertRow2"));
        Assert.NotNull(FindNamed(view.transform, "ReportRow1"));
        Assert.NotNull(FindNamed(view.transform, "TargetMarker2"));

        Assert.IsNull(
            view.GetComponentInChildren<AriaTutorialBriefingView>(true),
            "POP-13 must not duplicate the tutorial surface owned by the permanent Match HUD ARIA panel.");

        GameObject matchHudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudPrefabPath);
        Assert.NotNull(matchHudPrefab, MatchHudPrefabPath);
        Transform assistantButton = FindNamed(matchHudPrefab.transform, "AriaAssistantButton");
        Assert.NotNull(assistantButton);
        AriaTutorialBriefingView tutorial = assistantButton.GetComponent<AriaTutorialBriefingView>();
        Assert.NotNull(tutorial, "The Match HUD ARIA panel must own the single tutorial surface.");
        Assert.IsTrue(tutorial.TryBindHierarchy());
        Assert.AreEqual(
            AriaPortraitPath,
            AssetDatabase.GetAssetPath(tutorial.PortraitImage.sprite));
        Assert.IsNull(FindNamed(assistantButton, "TutorialInputBlocker"),
            "The persistent tutorial briefing must not block battlefield or HUD input.");
        Assert.AreEqual(new Vector2(0f, 1f), tutorial.BriefingLayout.anchorMin);
        Assert.AreEqual(new Vector2(0f, 1f), tutorial.BriefingLayout.anchorMax);
        Assert.IsTrue(tutorial.ShowMeButton.transform.IsChildOf(assistantButton));
        Assert.IsTrue(tutorial.DoItButton.transform.IsChildOf(assistantButton));
        Assert.GreaterOrEqual((tutorial.ShowMeButton.transform as RectTransform).rect.height, 54f);
        Assert.GreaterOrEqual((tutorial.DoItButton.transform as RectTransform).rect.height, 54f);
        Assert.IsNull(tutorial.CloseButton, "The Match HUD tutorial surface must not add a SKIP action.");

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        _openedScene = true;
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        Assert.AreSame(prefab, content.AriaCommandAssistantPopupPrefab);
    }

    [Test]
    public void TutorialBriefing_RemainsVisibleThroughEachInstructionAndResetsForReplay()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out _);
        var gateway = new FakeAssistantPanelGateway(
            CreateStructuredModel(801u, recommendationKind: 1, recommendationTargetKind: 6,
                tutorialStep: 1, tutorialStepCount: 5),
            UiAssistantHighlightModel.Empty)
        {
            CinematicInteractionLocked = true
        };
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());

        ui.Update();
        AriaCommandAssistantPopupView popup =
            overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        AriaTutorialBriefingView tutorial =
            header.Find("AriaAssistantButton").GetComponent<AriaTutorialBriefingView>();
        Assert.IsFalse(popup.IsOpen, "The tutorial briefing must wait for the opening cinematic lock.");
        Assert.IsFalse(tutorial.IsPresentationVisible);

        gateway.CinematicInteractionLocked = false;
        SetPrivateField(ui, "_nextAssistantPanelRefreshTime", 0f);
        ui.Update();
        Assert.IsFalse(popup.IsOpen, "Tutorial steps must not open a second POP-13 surface.");
        Assert.AreEqual(1, gateway.TutorialNarrationSteps.Count);
        Assert.AreEqual(1, gateway.TutorialNarrationSteps[0]);
        Assert.AreEqual("Preview the verified hostile source before dispatch.", gateway.TutorialNarrationTexts[0]);
        Assert.IsTrue(tutorial.IsPresentationVisible);
        Assert.IsNull(tutorial.CloseButton, "The embedded tutorial has no SKIP action.");
        Assert.AreEqual("FOCUS HOSTILE ARMOR", tutorial.TitleText.text);
        Assert.AreEqual("STEP 1/5", tutorial.ProgressText.text);
        Assert.AreEqual(1, gateway.AssistantIntentRequestCount,
            "Opening the first tutorial instruction must automatically issue SHOW ME.");
        Assert.AreEqual(UiAssistantCommandIntentKind.ShowRecommendation, gateway.LastAssistantIntentKind);

        gateway.AssistantPanel = CreateStructuredModel(
            802u, recommendationKind: 1, recommendationTargetKind: 6,
            tutorialStep: 1, tutorialStepCount: 5);
        SetPrivateField(ui, "_nextAssistantPanelRefreshTime", 0f);
        ui.Update();
        Assert.IsTrue(tutorial.IsPresentationVisible, "The active tutorial instruction must remain visible.");

        gateway.AssistantPanel = CreateStructuredModel(
            803u, recommendationKind: 2, recommendationTargetKind: 1,
            tutorialStep: 2, tutorialStepCount: 5,
            recommendationBody: "Move the squad to the marked cover position.");
        SetPrivateField(ui, "_nextAssistantPanelRefreshTime", 0f);
        ui.Update();
        Assert.IsFalse(tutorial.IsPresentationVisible,
            "Completing one instruction must remove ARIA before the next instruction appears.");

        MatchHudAssistantUiSystemHelper helper =
            GetPrivateField<MatchHudAssistantUiSystemHelper>(ui, "_matchHudAssistantUiSystem");
        float stepTwoDeadline = GetPrivateField<float>(helper, "_tutorialShowAtUnscaledTime");
        helper.TickHighlight(stepTwoDeadline - 0.01f);
        Assert.IsFalse(tutorial.IsPresentationVisible, "The next tutorial instruction must wait for two seconds.");
        Assert.AreEqual(1, gateway.TutorialNarrationSteps.Count,
            "A hidden pending instruction must not start narration.");
        helper.TickHighlight(stepTwoDeadline);
        Assert.IsTrue(tutorial.IsPresentationVisible, "The next tutorial instruction must appear after two seconds.");
        Assert.AreEqual(2, gateway.TutorialNarrationSteps.Count);
        Assert.AreEqual(2, gateway.TutorialNarrationSteps[1]);
        Assert.AreEqual(UiTutorialNarrationPhase.PrimaryAction, gateway.TutorialNarrationPhases[1]);
        Assert.AreEqual("Tap MOVE to select the move command.", gateway.TutorialNarrationTexts[1]);
        Assert.AreEqual("STEP 2/5", tutorial.ProgressText.text);
        Assert.AreEqual("PRESS MOVE", tutorial.TitleText.text);
        Assert.AreEqual("Tap MOVE to select the move command.", tutorial.BodyText.text);
        Assert.AreEqual(2, gateway.AssistantIntentRequestCount,
            "Opening the MOVE instruction must automatically reveal its command button.");

        MatchOverlayCommandControlsView commandControls = CreateCommandControls(overlay);
        ui.BindMatchHudCommandControls(commandControls);
        commandControls.MoveButton.onClick.Invoke();
        Assert.IsFalse(tutorial.IsPresentationVisible,
            "Pressing MOVE must remove the completed command-button instruction.");
        float moveTargetDeadline = GetPrivateField<float>(helper, "_tutorialShowAtUnscaledTime");
        helper.TickHighlight(moveTargetDeadline - 0.01f);
        Assert.IsFalse(tutorial.IsPresentationVisible, "The destination instruction must honor the two-second delay.");
        gateway.TutorialNarrationFailuresRemaining = 1;
        helper.TickHighlight(moveTargetDeadline);
        Assert.IsTrue(tutorial.IsPresentationVisible, "ARIA must return to teach the destination substep.");
        Assert.AreEqual(3, gateway.AssistantIntentRequestCount,
            "Opening the destination substep must automatically reveal its world target once.");
        Assert.AreEqual(UiAssistantCommandIntentKind.ShowRecommendation, gateway.LastAssistantIntentKind);
        Assert.IsFalse(gateway.LastAssistantIntentFromTakeover);
        Assert.AreEqual(2, gateway.TutorialNarrationSteps.Count,
            "A rejected narration request must remain pending rather than being marked complete.");
        helper.TickHighlight(moveTargetDeadline + 0.01f);
        Assert.AreEqual(3, gateway.AssistantIntentRequestCount,
            "Narration retries must not repeat an accepted automatic target reveal.");
        Assert.AreEqual(3, gateway.TutorialNarrationSteps.Count,
            "The destination narration must retry after a transient gateway rejection.");
        Assert.AreEqual(2, gateway.TutorialNarrationSteps[2]);
        Assert.AreEqual(UiTutorialNarrationPhase.WorldTarget, gateway.TutorialNarrationPhases[2]);
        Assert.AreEqual(
            "Tap the highlighted destination to move your squad.",
            gateway.TutorialNarrationTexts[2]);
        Assert.AreEqual("STEP 3/5", tutorial.ProgressText.text);
        Assert.AreEqual("CHOOSE DESTINATION", tutorial.TitleText.text);
        Assert.AreEqual("Tap the highlighted destination to move your squad.", tutorial.BodyText.text);

        helper.CompleteWorldTarget(TacticalCommandMode.Move);
        helper.ApplyCommandMode(TacticalCommandMode.None);
        Assert.IsFalse(tutorial.IsPresentationVisible, "Accepting the destination must remove the completed instruction.");
        helper.ApplyReadModel(gateway.AssistantPanel);
        helper.TickHighlight(float.MaxValue);
        Assert.IsFalse(tutorial.IsPresentationVisible,
            "A stale projection must not reopen a completed tutorial instruction.");

        gateway.AssistantPanel = CreateStructuredModel(
            804u, recommendationKind: 3, recommendationTargetKind: 6,
            tutorialStep: 3, tutorialStepCount: 5,
            recommendationBody: "Inspect the armed patrol near the civilians.");
        helper.ApplyReadModel(gateway.AssistantPanel);
        helper.TickHighlight(100f);
        helper.TickHighlight(102f);
        Assert.IsTrue(tutorial.IsPresentationVisible);
        Assert.AreEqual(4, gateway.TutorialNarrationSteps.Count);
        Assert.AreEqual(3, gateway.TutorialNarrationSteps[3]);
        Assert.AreEqual(UiTutorialNarrationPhase.PrimaryAction, gateway.TutorialNarrationPhases[3]);
        Assert.AreEqual("Tap ATTACK to select the attack command.", gateway.TutorialNarrationTexts[3]);
        Assert.AreEqual("STEP 4/5", tutorial.ProgressText.text);
        Assert.AreEqual("Tap ATTACK to select the attack command.", tutorial.BodyText.text);
        Assert.AreEqual(4, gateway.AssistantIntentRequestCount,
            "Opening the ATTACK instruction must automatically reveal its command button.");
        commandControls.AttackButton.onClick.Invoke();
        Assert.IsFalse(tutorial.IsPresentationVisible,
            "Pressing ATTACK must remove the completed command-button instruction.");
        float attackTargetDeadline = GetPrivateField<float>(helper, "_tutorialShowAtUnscaledTime");
        helper.TickHighlight(attackTargetDeadline);
        Assert.IsTrue(tutorial.IsPresentationVisible, "ARIA must return to teach the enemy-target substep.");
        Assert.AreEqual(5, gateway.AssistantIntentRequestCount,
            "Opening the enemy-target substep must automatically reveal its world target once.");
        helper.TickHighlight(attackTargetDeadline + 0.01f);
        Assert.AreEqual(5, gateway.AssistantIntentRequestCount,
            "An open enemy-target substep must not enqueue duplicate automatic reveals.");
        Assert.AreEqual(5, gateway.TutorialNarrationSteps.Count);
        Assert.AreEqual(3, gateway.TutorialNarrationSteps[4]);
        Assert.AreEqual(UiTutorialNarrationPhase.WorldTarget, gateway.TutorialNarrationPhases[4]);
        Assert.AreEqual(
            "Tap the highlighted enemy to issue the attack.",
            gateway.TutorialNarrationTexts[4]);
        Assert.AreEqual("STEP 5/5", tutorial.ProgressText.text);
        Assert.AreEqual("CHOOSE ENEMY", tutorial.TitleText.text);
        Assert.AreEqual("Tap the highlighted enemy to issue the attack.", tutorial.BodyText.text);
        helper.CompleteWorldTarget(TacticalCommandMode.Attack);
        Assert.IsFalse(tutorial.IsPresentationVisible, "The final attack must hide ARIA immediately.");

        gateway.AssistantPanel = CreateStructuredModel(
            805u, recommendationKind: 3, recommendationTargetKind: 6,
            tutorialStep: 4, tutorialStepCount: 5);
        helper.ApplyReadModel(gateway.AssistantPanel);
        helper.TickHighlight(float.MaxValue);
        Assert.IsFalse(tutorial.IsPresentationVisible,
            "Final combat suppresses all later tutorial briefing projections.");

        helper.ResetForMissionAttempt();
        gateway.AssistantPanel = CreateStructuredModel(
            806u, recommendationKind: 1, recommendationTargetKind: 6,
            tutorialStep: 1, tutorialStepCount: 5);
        helper.ApplyReadModel(gateway.AssistantPanel);
        helper.TickHighlight(0f);
        Assert.IsTrue(tutorial.IsPresentationVisible, "A new mission attempt must present step one again.");
        Assert.AreEqual(6, gateway.TutorialNarrationSteps.Count,
            "A replay must narrate the first tutorial step again.");
        Assert.AreEqual(1, gateway.TutorialNarrationSteps[5]);
        ui.Dispose();
    }

    [Test]
    public void TutorialBriefing_AutoShowsVisibleIndicatorForEveryInstruction()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out _);
        var gateway = new FakeAssistantPanelGateway(
            CreateStructuredModel(821u, recommendationKind: 1, recommendationTargetKind: 6,
                tutorialStep: 1, tutorialStepCount: 5),
            UiAssistantHighlightModel.Empty);
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
        MatchHudSquadTrayView squadTray = CreateSquadTray(overlay);
        ui.BindMatchHudSquadTray(squadTray);
        MatchOverlayCommandControlsView commandControls = CreateCommandControls(overlay);
        ui.BindMatchHudCommandControls(commandControls);

        ui.Update();
        GameObject indicator = FindLoadedObject("AriaAssistantTargetIndicatorRuntime");
        AssertVisibleIndicator(indicator, "SELECT SQUAD");
        Assert.AreEqual(1, gateway.AssistantIntentRequestCount);

        gateway.AssistantPanel = CreateStructuredModel(
            822u, recommendationKind: 2, recommendationTargetKind: 1,
            tutorialStep: 2, tutorialStepCount: 5);
        MatchHudAssistantUiSystemHelper helper =
            GetPrivateField<MatchHudAssistantUiSystemHelper>(ui, "_matchHudAssistantUiSystem");
        helper.ApplyReadModel(gateway.AssistantPanel);
        helper.TickHighlight(10f);
        helper.TickHighlight(12f);
        AssertVisibleIndicator(indicator, "PRESS MOVE");
        Assert.AreEqual(2, gateway.AssistantIntentRequestCount);

        commandControls.MoveButton.onClick.Invoke();
        float moveTargetDeadline = GetPrivateField<float>(helper, "_tutorialShowAtUnscaledTime");
        helper.TickHighlight(moveTargetDeadline);
        Assert.IsFalse(indicator.activeInHierarchy,
            "An unresolved destination must stay hidden instead of pointing at a local placeholder.");
        gateway.AssistantHighlight = CreateHighlightModel(824u, recommendationKind: 2);
        helper.ApplyHighlightReadModel(gateway.AssistantHighlight);
        helper.TickHighlight(moveTargetDeadline + 0.01f);
        AssertVisibleIndicator(indicator, "CLICK DESTINATION");
        AssertWorldRingCenteredAt(new Vector3(12f, 3.28f, 9f));
        Assert.AreEqual(3, gateway.AssistantIntentRequestCount);

        helper.CompleteWorldTarget(TacticalCommandMode.Move);
        helper.ApplyCommandMode(TacticalCommandMode.None);
        gateway.AssistantPanel = CreateStructuredModel(
            825u, recommendationKind: 3, recommendationTargetKind: 6,
            tutorialStep: 3, tutorialStepCount: 5);
        helper.ApplyReadModel(gateway.AssistantPanel);
        helper.TickHighlight(20f);
        helper.TickHighlight(22f);
        AssertVisibleIndicator(indicator, "PRESS ATTACK");
        Assert.AreEqual(4, gateway.AssistantIntentRequestCount);

        commandControls.AttackButton.onClick.Invoke();
        float attackTargetDeadline = GetPrivateField<float>(helper, "_tutorialShowAtUnscaledTime");
        helper.TickHighlight(attackTargetDeadline);
        Assert.IsFalse(indicator.activeInHierarchy,
            "An unresolved enemy must stay hidden instead of reusing the previous destination.");
        gateway.AssistantHighlight = CreateHighlightModel(827u, recommendationKind: 3, targetKind: 6);
        helper.ApplyHighlightReadModel(gateway.AssistantHighlight);
        helper.TickHighlight(attackTargetDeadline + 0.01f);
        AssertVisibleIndicator(indicator, "CLICK ENEMY");
        AssertWorldRingCenteredAt(new Vector3(12f, 3.28f, 9f));
        Assert.AreEqual(5, gateway.AssistantIntentRequestCount);
        ui.Dispose();
    }

    [Test]
    public void TutorialNarration_MapsEveryM01StepToEnglishAndPersianEvents()
    {
        MethodInfo resolver = typeof(UiShellEcsGateway).GetMethod(
            "ResolveTutorialAudioEventId",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(resolver);
        string[] englishSuffixes =
        {
            "FindSquad.En", "MoveToCover.En", "ConfirmThreat.En", "Engage.En", "SecureCorridor.En"
        };
        string[] persianSuffixes =
        {
            "FindSquad.Fa", "MoveToCover.Fa", "ConfirmThreat.Fa", "Engage.Fa", "SecureCorridor.Fa"
        };
        for (byte step = 1; step <= 5; step++)
        {
            StringAssert.EndsWith(
                englishSuffixes[step - 1],
                resolver.Invoke(
                    null,
                    new object[]
                    {
                        step,
                        UiTutorialNarrationPhase.PrimaryAction,
                        FirstLaunchNarrativeLanguage.English
                    })?.ToString());
            StringAssert.EndsWith(
                persianSuffixes[step - 1],
                resolver.Invoke(
                    null,
                    new object[]
                    {
                        step,
                        UiTutorialNarrationPhase.PrimaryAction,
                        FirstLaunchNarrativeLanguage.Persian
                    })?.ToString());
        }

        StringAssert.EndsWith(
            "MoveDestination.En",
            resolver.Invoke(
                null,
                new object[]
                {
                    (byte)2,
                    UiTutorialNarrationPhase.WorldTarget,
                    FirstLaunchNarrativeLanguage.English
                })?.ToString());
        StringAssert.EndsWith(
            "MoveDestination.Fa",
            resolver.Invoke(
                null,
                new object[]
                {
                    (byte)2,
                    UiTutorialNarrationPhase.WorldTarget,
                    FirstLaunchNarrativeLanguage.Persian
                })?.ToString());
        StringAssert.EndsWith(
            "AttackTarget.En",
            resolver.Invoke(
                null,
                new object[]
                {
                    (byte)3,
                    UiTutorialNarrationPhase.WorldTarget,
                    FirstLaunchNarrativeLanguage.English
                })?.ToString());
        StringAssert.EndsWith(
            "AttackTarget.Fa",
            resolver.Invoke(
                null,
                new object[]
                {
                    (byte)3,
                    UiTutorialNarrationPhase.WorldTarget,
                    FirstLaunchNarrativeLanguage.Persian
                })?.ToString());
    }

    [Test]
    public void TutorialLocalization_MapsEveryM01StepToEnglishAndPersianText()
    {
        MethodInfo resolver = typeof(UiShellEcsGateway).GetMethod(
            "TryResolveTutorialPresentationText",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(resolver);
        string[] englishTitles =
        {
            "Find your squad", "Move to cover", "Confirm the threat", "Engage the patrol", "Secure the corridor"
        };
        string[] englishBodies =
        {
            "Select the command squad to begin.",
            "Move the squad to the marked cover position.",
            "Inspect the armed patrol near the civilians.",
            "Attack the confirmed hostile patrol.",
            "Check the objective and secure the civilian route."
        };
        string[] persianTitles =
        {
            "گروه خود را پیدا کنید", "به پوشش حرکت کنید", "تهدید را بررسی کنید",
            "با گشت دشمن درگیر شوید", "مسیر را امن کنید"
        };
        string[] persianBodies =
        {
            "برای شروع، گروه فرماندهی را انتخاب کنید.",
            "گروه را به موقعیت پوشش علامت‌گذاری‌شده منتقل کنید.",
            "گشت مسلح نزدیک غیرنظامیان را بررسی کنید.",
            "به گشت دشمن تأییدشده حمله کنید.",
            "هدف را بررسی کنید و مسیر غیرنظامیان را امن کنید."
        };

        for (byte step = 1; step <= 5; step++)
        {
            object[] english =
            {
                step, FirstLaunchNarrativeLanguage.English, null, null, false
            };
            Assert.IsTrue((bool)resolver.Invoke(null, english));
            Assert.AreEqual(englishTitles[step - 1], english[2]);
            Assert.AreEqual(englishBodies[step - 1], english[3]);
            Assert.IsFalse((bool)english[4]);

            object[] persian =
            {
                step, FirstLaunchNarrativeLanguage.Persian, null, null, false
            };
            Assert.IsTrue((bool)resolver.Invoke(null, persian));
            Assert.AreEqual(persianTitles[step - 1], persian[2]);
            Assert.AreEqual(persianBodies[step - 1], persian[3]);
            Assert.IsTrue((bool)persian[4]);
        }
    }

    [Test]
    public void TutorialBriefing_PersianUsesRtlFontAndLocalizedSubsteps()
    {
        GameObject matchHudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudPrefabPath);
        Assert.NotNull(matchHudPrefab, MatchHudPrefabPath);
        GameObject instance = UnityEngine.Object.Instantiate(matchHudPrefab);
        instance.name = "AssistantUiTestPersianTutorial";
        AriaTutorialBriefingView tutorial =
            instance.GetComponentInChildren<AriaTutorialBriefingView>(true);
        Assert.NotNull(tutorial);
        Assert.IsTrue(tutorial.TryBindHierarchy());

        tutorial.Apply(CreateStructuredModel(
            901u,
            recommendationKind: 2,
            recommendationTargetKind: 1,
            tutorialStep: 2,
            tutorialStepCount: 5,
            recommendationTitle: "به پوشش حرکت کنید",
            recommendationBody: "گروه را به موقعیت پوشش علامت‌گذاری‌شده منتقل کنید.",
            tutorialRightToLeft: true));

        Assert.IsTrue(tutorial.TitleText.isRightToLeftText);
        Assert.IsTrue(tutorial.BodyText.isRightToLeftText);
        Assert.IsTrue(tutorial.ProgressText.isRightToLeftText);
        Assert.AreEqual(
            "Assets/Game/Art/UI/Fonts/NotoSansArabic/NotoSansArabic-Narrative SDF.asset",
            AssetDatabase.GetAssetPath(tutorial.TitleText.font));
        Assert.AreNotEqual("PRESS MOVE", tutorial.TitleText.text);
        StringAssert.DoesNotContain("TRAINING", tutorial.ProgressText.text);
        StringAssert.DoesNotContain(
            "SHOW ME",
            tutorial.ShowMeButton.GetComponentInChildren<TMP_Text>(true).text);
        Assert.AreEqual(
            "برای انتخاب دستور حرکت، روی «حرکت» بزنید.",
            tutorial.CurrentInstructionBody);
        Assert.AreEqual(
            UiTutorialNarrationPhase.PrimaryAction,
            tutorial.CurrentNarrationPhase);

        string pressMoveTitle = tutorial.TitleText.text;
        string pressMoveBody = tutorial.BodyText.text;
        tutorial.ApplyInteractionState(TacticalCommandMode.Move, worldTargetCompleted: false);
        Assert.AreNotEqual(pressMoveTitle, tutorial.TitleText.text);
        Assert.AreNotEqual("CHOOSE DESTINATION", tutorial.TitleText.text);
        Assert.AreNotEqual(pressMoveBody, tutorial.BodyText.text);
        StringAssert.DoesNotContain("Tap", tutorial.BodyText.text);
        Assert.AreEqual(
            "برای حرکت گروه، روی مقصد علامت‌گذاری‌شده بزنید.",
            tutorial.CurrentInstructionBody);
        Assert.AreEqual(
            UiTutorialNarrationPhase.WorldTarget,
            tutorial.CurrentNarrationPhase);
        string destinationBody = tutorial.BodyText.text;
        tutorial.ApplyInteractionState(TacticalCommandMode.Move, worldTargetCompleted: true);
        Assert.AreNotEqual("MOVING TO COVER", tutorial.TitleText.text);
        Assert.AreNotEqual(destinationBody, tutorial.BodyText.text);
        StringAssert.DoesNotContain("Your squad", tutorial.BodyText.text);
    }

    [Test]
    public void TutorialDoIt_SelectsCommandModeBeforeExecutingWorldOrder()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out _);
        var gateway = new FakeAssistantPanelGateway(
            CreateStructuredModel(
                902u,
                recommendationKind: 3,
                recommendationTargetKind: 6,
                tutorialStep: 4,
                tutorialStepCount: 5,
                recommendationBody: "Attack the confirmed hostile patrol."),
            CreateHighlightModel(902u, recommendationKind: 3, targetKind: 6));
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());

        MatchOverlayCommandControlsView commandControls = CreateCommandControls(overlay);
        var commandInput = new MatchOverlayCommandInputUiSystemHelper();
        commandInput.Bind(
            commandControls,
            new AcceptedSelectionUiCommand(),
            commandModeQueued: ui.AcknowledgeMatchHudGuidedCommandMode);
        ui.BindMatchHudCommandControls(commandControls);
        ui.Update();

        MatchHudAssistantUiSystemHelper helper =
            GetPrivateField<MatchHudAssistantUiSystemHelper>(ui, "_matchHudAssistantUiSystem");
        helper.TickHighlight(float.MaxValue);
        AriaTutorialBriefingView tutorial = overlay
            .GetComponentInChildren<AriaTutorialBriefingView>(true);
        Assert.IsTrue(tutorial.IsPresentationVisible);
        Assert.AreEqual("PRESS ATTACK", tutorial.TitleText.text);
        Assert.AreEqual("Tap ATTACK to select the attack command.", tutorial.BodyText.text);
        Assert.AreEqual("STEP 4/5", tutorial.ProgressText.text);

        tutorial.DoItButton.onClick.Invoke();

        Assert.AreEqual(1, gateway.AssistantIntentRequestCount,
            "The first tutorial DO IT must only add the automatic ATTACK reveal, not issue the world order.");
        Assert.IsFalse(tutorial.IsPresentationVisible);
        float targetInstructionDeadline = GetPrivateField<float>(helper, "_tutorialShowAtUnscaledTime");
        helper.TickHighlight(targetInstructionDeadline);
        Assert.IsTrue(tutorial.IsPresentationVisible);
        Assert.AreEqual("CHOOSE ENEMY", tutorial.TitleText.text);
        Assert.AreEqual("Tap the highlighted enemy to issue the attack.", tutorial.BodyText.text);
        Assert.AreEqual("STEP 5/5", tutorial.ProgressText.text);
        Assert.AreEqual(2, gateway.AssistantIntentRequestCount,
            "Opening the target instruction must automatically issue SHOW ME.");
        Assert.AreEqual(UiAssistantCommandIntentKind.ShowRecommendation, gateway.LastAssistantIntentKind);
        Assert.IsFalse(gateway.LastAssistantIntentFromTakeover);

        tutorial.DoItButton.onClick.Invoke();

        Assert.AreEqual(3, gateway.AssistantIntentRequestCount);
        Assert.AreEqual(UiAssistantCommandIntentKind.ExecuteRecommendation, gateway.LastAssistantIntentKind);
        Assert.IsTrue(gateway.LastAssistantIntentFromTakeover);
        ui.Dispose();
    }

    [Test]
    public void MatchHudPrefab_ContainsEditableAssistantButton()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudPrefabPath);
        Assert.NotNull(prefab, MatchHudPrefabPath);

        Transform button = FindNamed(prefab.transform, "AriaAssistantButton");
        Assert.NotNull(button, "The Match HUD prefab must own the ARIA access button.");
        Assert.NotNull(button.GetComponent<Image>());
        Assert.NotNull(button.GetComponent<Button>());
        Assert.NotNull(button.GetComponent<Canvas>());
        Assert.NotNull(button.GetComponent<GraphicRaycaster>());
        Assert.NotNull(FindNamed(button, "Label")?.GetComponent<TMP_Text>());
        Assert.NotNull(FindNamed(button, "State")?.GetComponent<TMP_Text>());
        Assert.NotNull(FindNamed(button, "AlertCue")?.GetComponent<TMP_Text>());
    }

    [Test]
    public void BindMatchHudAssistant_UsesPrefabButtonAndRestoresObjectives()
    {
        CreateHudHarness(
            includeAssistantButton: true,
            out RectTransform overlay,
            out RectTransform header,
            out RectTransform objectives);
        RectTransform prefabButton = objectives.parent.Find("AriaAssistantButton") as RectTransform;
        var runtimeState = new FakeMatchRuntimeState();
        var ui = new MainMenuPlayUI();
        ui.Init(null, runtimeState);
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());

        RectTransform button = objectives.parent.Find("AriaAssistantButton") as RectTransform;
        AriaCommandAssistantPopupView popup = overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        Assert.NotNull(button);
        Assert.AreSame(prefabButton, button, "Binding must reuse the prefab-owned button instance.");
        Assert.NotNull(popup);
        Assert.IsTrue(objectives.gameObject.activeSelf, "ARIA must augment the HUD without hiding objectives.");
        Assert.AreEqual(new Vector2(400f, 683f), button.sizeDelta);
        Assert.IsFalse(popup.gameObject.activeSelf, "ARIA starts closed.");

        button.GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(popup.IsOpen);
        Assert.IsTrue(runtimeState.SuppressNextWorldClick);
        Assert.IsTrue(ui.IsPointerOverAnyGameplayUi(CenterScreenPoint(button), out string source));
        Assert.AreEqual("MatchHudAssistant", source);

        ui.Dispose();
        Assert.IsTrue(objectives.gameObject.activeSelf, "Unbind must restore the old objective root.");
        Assert.AreSame(
            prefabButton,
            objectives.parent.Find("AriaAssistantButton") as RectTransform,
            "Unbind must not destroy the prefab-owned button.");
    }

    [Test]
    public void BindMatchHudAssistant_MissingPrefabButtonDoesNotCreateRuntimeFallback()
    {
        CreateHudHarness(
            includeAssistantButton: false,
            out RectTransform overlay,
            out RectTransform header,
            out RectTransform objectives);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        const string expectedError =
            "[ARIA] Match HUD prefab is missing HeaderContent/AriaAssistantButton; runtime button creation is disabled.";
        try
        {
            LogAssert.Expect(LogType.Error, expectedError);
        }
        catch (InvalidOperationException exception) when (
            exception.Message.Contains("No log scope is available", StringComparison.Ordinal))
        {
            // Focused executeMethod validation invokes the same case without a Test Runner log scope.
        }
        string capturedError = null;
        Application.LogCallback captureError = (condition, _, logType) =>
        {
            if (logType == LogType.Error && string.Equals(condition, expectedError, StringComparison.Ordinal))
                capturedError = condition;
        };
        Application.logMessageReceived += captureError;
        try
        {
            ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
        }
        finally
        {
            Application.logMessageReceived -= captureError;
        }

        Assert.AreEqual(expectedError, capturedError);

        RectTransform button = header.Find("AriaAssistantButton") as RectTransform;
        Assert.IsNull(button, "Runtime binding must not recreate a missing prefab button.");
        Assert.IsTrue(objectives.gameObject.activeSelf, "A failed binding must keep objectives visible.");
        Assert.IsNull(overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true));

        ui.Dispose();
    }

    [Test]
    public void BindMatchHudAssistant_AppliesStructuredRowsWithoutCreatingPopupObjects()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out RectTransform objectives);
        var gateway = new FakeAssistantPanelGateway(CreateStructuredModel(42), CreateHighlightModel(88));
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());

        AriaCommandAssistantPopupView popup = overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        Assert.NotNull(popup);
        int childCountBefore = popup.GetComponentsInChildren<Transform>(true).Length;
        ui.Update();
        int childCountAfter = popup.GetComponentsInChildren<Transform>(true).Length;
        Assert.AreEqual(childCountBefore, childCountAfter, "Read-model binding must reuse the prefab hierarchy.");

        Assert.AreEqual("Secure the relay", Text(popup.transform, "Goal0Title"));
        Assert.AreEqual("PRIMARY / ACTIVE", Text(popup.transform, "Goal0StateText"));
        Assert.IsFalse(FindNamed(popup.transform, "GoalRow2").gameObject.activeSelf);
        Assert.AreEqual("HOSTILE ARMOR", Text(popup.transform, "Alert0Body"));
        Assert.AreEqual("CRITICAL / NEW", Text(popup.transform, "Alert0PriorityText"));
        Assert.AreEqual("Fuel convoy ready", Text(popup.transform, "Report0Body"));
        Assert.AreEqual("LOW / ACTIVE", Text(popup.transform, "Report0PriorityText"));
        Assert.AreEqual("Raven Tank", Text(popup.transform, "TargetNameText"));
        Assert.AreEqual("PRESENTED", Text(popup.transform, "NarrationStateText"));
        Assert.IsTrue(FindNamed(popup.transform, "NarrationWaveform").gameObject.activeSelf);
        Assert.AreEqual("ELAPSED: 02:07", Text(popup.transform, "ElapsedText"));
        Assert.IsTrue(FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().interactable);
        Assert.IsTrue(FindNamed(popup.transform, "DoItButton").GetComponent<Button>().interactable);
        Assert.IsTrue(FindNamed(popup.transform, "StopButton").GetComponent<Button>().interactable);
        Assert.AreEqual("SHOW ME", Text(popup.transform, "ShowMeButtonLabel"));
        Assert.AreEqual("DO IT", Text(popup.transform, "DoItButtonLabel"));

        FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen, "SHOW ME must close ARIA so the camera reveal is visible.");
        objectives.parent.Find("AriaAssistantButton").GetComponent<Button>().onClick.Invoke();
        FindNamed(popup.transform, "DoItButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen, "DO IT must close ARIA so selection or movement feedback is visible.");
        Assert.AreEqual(2, gateway.AssistantIntentRequestCount);
        Assert.AreEqual(UiAssistantCommandIntentKind.ExecuteRecommendation, gateway.LastAssistantIntentKind);
        Assert.IsTrue(gateway.LastAssistantIntentFromTakeover);

        GameObject worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.NotNull(worldRing);
        Assert.AreEqual(64, worldRing.GetComponent<LineRenderer>().positionCount);
        GameObject targetIndicator = FindLoadedObject("AriaAssistantTargetIndicatorRuntime");
        Assert.NotNull(targetIndicator, "World-target Show Me must create an explicit screen indicator.");
        Assert.AreEqual("ARIA TARGET", targetIndicator.GetComponentInChildren<TMP_Text>(true).text);
        Assert.IsFalse(targetIndicator.GetComponent<CanvasGroup>().blocksRaycasts);
        Assert.IsFalse(popup.PreviewHighlight.raycastTarget, "ARIA preview visuals must never block gameplay input.");
        ui.Dispose();
    }

    [Test]
    public void AssistantPanelUi_UnchangedModelApplicationsAllocateZeroManagedBytes()
    {
        GameObject instance = UnityEngine.Object.Instantiate(LoadPopupPrefab());
        instance.name = "AssistantUiTestAllocationPopup";
        AriaCommandAssistantPopupView popup = instance.GetComponent<AriaCommandAssistantPopupView>();
        Assert.NotNull(popup);
        Assert.IsTrue(popup.TryBindHierarchy());

        var panel = new AssistantPanelUiSystemHelper();
        panel.Bind(popup, null, null);
        UiAssistantPanelModel model = CreateStructuredModel(73);
        panel.ApplyReadModel(model);
        for (int i = 0; i < 16; i++)
            panel.ApplyReadModel(model);

        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            panel.ApplyReadModel(model);
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        Assert.AreEqual(0, allocatedBytes, "Repeated applications of one assistant model version must allocate zero managed bytes.");
        panel.Unbind();
    }

    [Test]
    public void BindMatchHudAssistant_EnforcesPopupExclusivity()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out RectTransform objectives);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());

        GameObject buildRoot = CreatePopupPeer<BuildDrawerView>("AssistantUiTestBuildDrawer");
        GameObject mapRoot = CreatePopupPeer<MatchHudFullMapPopupView>("AssistantUiTestFullMap");
        GameObject exchangeRoot = CreatePopupPeer<ResourceExchangePopupView>("AssistantUiTestResourceExchange");
        ui.BindBuildDrawer(buildRoot.GetComponent<BuildDrawerView>());
        ui.BindMatchHudFullMapPopup(mapRoot.GetComponent<MatchHudFullMapPopupView>());
        ui.BindResourceExchangePopup(exchangeRoot.GetComponent<ResourceExchangePopupView>());

        int buildClosed = 0;
        int mapClosed = 0;
        int exchangeClosed = 0;
        ui.ConfigureLargeTacticalPopupCloseActions(
            () => { buildClosed++; buildRoot.SetActive(false); },
            () => { mapClosed++; mapRoot.SetActive(false); },
            () => { exchangeClosed++; exchangeRoot.SetActive(false); });

        RectTransform button = objectives.parent.Find("AriaAssistantButton") as RectTransform;
        button.GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(1, buildClosed);
        Assert.AreEqual(1, mapClosed);
        Assert.AreEqual(1, exchangeClosed);
        Assert.IsTrue(overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true).IsOpen);
        ui.Dispose();
    }

    [Test]
    public void BindMatchHudAssistant_CloseEscapeAndStopHaveSeparateSemantics()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out RectTransform objectives);
        var gateway = new FakeAssistantPanelGateway(
            CreateStructuredModel(91, canExecute: false),
            UiAssistantHighlightModel.Empty);
        UiShellRuntimeGateway.Register(gateway);
        var panelStates = new List<bool>();
        var ui = new MainMenuPlayUI();
        ui.MatchHudAssistantPanelOpenChanged += panelStates.Add;
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
        ui.Update();

        Button access = objectives.parent.Find("AriaAssistantButton").GetComponent<Button>();
        AriaCommandAssistantPopupView popup = overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        Assert.IsFalse(FindNamed(popup.transform, "DoItButton").GetComponent<Button>().interactable);
        Assert.AreEqual("DO IT", Text(popup.transform, "DoItButtonLabel"));
        access.onClick.Invoke();
        FindNamed(popup.transform, "HeaderCloseButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen);
        Assert.AreEqual(0, gateway.AssistantIntentRequestCount, "Header CLOSE must not enqueue a command.");

        access.onClick.Invoke();
        FindNamed(popup.transform, "CloseButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen);
        Assert.AreEqual(0, gateway.AssistantIntentRequestCount, "Bottom CLOSE must not enqueue a command.");

        access.onClick.Invoke();
        FindNamed(popup.transform, "StopButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(popup.IsOpen, "STOP is a command and must not close the popup.");
        Assert.AreEqual(1, gateway.AssistantIntentRequestCount);
        Assert.AreEqual(UiAssistantCommandIntentKind.StopAssistantControl, gateway.LastAssistantIntentKind);

        Assert.IsTrue(ui.TryCloseMatchHudAssistantForBack());
        Assert.IsFalse(popup.IsOpen, "Escape/back closes ARIA first.");
        Assert.AreEqual(1, gateway.AssistantIntentRequestCount, "Escape/back must not enqueue STOP.");
        Assert.IsFalse(gateway.LastAssistantPanelOpen);
        CollectionAssert.Contains(gateway.AssistantPanelOpenStates, true);
        CollectionAssert.Contains(panelStates, true);
        Assert.IsFalse(panelStates[panelStates.Count - 1]);
        ui.Dispose();
    }

    [Test]
    public void GuidedCommandHighlight_PointsAtCommandBeforeWorldTarget()
    {
        AssertGuidedCommandHighlight(
            recommendationKind: 2,
            commandMode: TacticalCommandMode.Move,
            initialLabel: "PRESS MOVE",
            armedLabel: "CLICK DESTINATION");
        AssertGuidedCommandHighlight(
            recommendationKind: 3,
            commandMode: TacticalCommandMode.Attack,
            initialLabel: "PRESS ATTACK",
            armedLabel: "CLICK ENEMY");
    }

    [Test]
    public void FirstShowMe_SelectSquad_ClosesPanelAndShowsVisibleIndicator()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out RectTransform objectives);
        var cameraObject = new GameObject("AssistantUiTestCamera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(12f, 3f, 0f);
        cameraObject.transform.rotation = Quaternion.identity;

        UiAssistantHighlightModel selectSquadHighlight = CreateHighlightModel(
            version: 501u,
            recommendationKind: 1,
            targetKind: 6);
        var gateway = new FakeAssistantPanelGateway(
            CreateStructuredModel(500u),
            UiAssistantHighlightModel.Empty)
        {
            HighlightAfterShowMe = selectSquadHighlight
        };
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
        ui.Update();

        objectives.parent.Find("AriaAssistantButton").GetComponent<Button>().onClick.Invoke();
        AriaCommandAssistantPopupView popup = overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        Assert.IsTrue(popup.IsOpen);
        FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen, "The real first Show Me closes the ARIA popup.");

        SetPrivateField(ui, "_nextAssistantPanelRefreshTime", 0f);
        ui.Update();

        GameObject indicator = FindLoadedObject("AriaAssistantTargetIndicatorRuntime");
        GameObject worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.NotNull(indicator);
        Assert.IsTrue(indicator.activeInHierarchy,
            "The first Select-squad Show Me indicator must remain visible after the popup closes.");
        Assert.AreEqual("SELECT SQUAD", indicator.GetComponentInChildren<TMP_Text>(true).text);
        Assert.NotNull(worldRing);
        Assert.IsTrue(worldRing.activeInHierarchy,
            "The first Show Me must retain the world ring around the squad.");
        Assert.AreEqual(1, gateway.AssistantIntentRequestCount);
        Assert.AreEqual(UiAssistantCommandIntentKind.ShowRecommendation, gateway.LastAssistantIntentKind);
        ui.Dispose();
    }

    [Test]
    public void FirstShowMe_SelectSquad_ShowsImmediateUiCueBeforeEcsHighlight()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out RectTransform objectives);
        var gateway = new FakeAssistantPanelGateway(
            CreateStructuredModel(510u, recommendationKind: 1, recommendationTargetKind: 6),
            UiAssistantHighlightModel.Empty);
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.ConfigureMatchHudSquadTrayBinding(view =>
            view?.Bind(slot => view.SetSelectedSlot(slot)));
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
        MatchHudSquadTrayView squadTray = CreateSquadTray(overlay);
        ui.BindMatchHudSquadTray(squadTray);
        MatchOverlayCommandControlsView commandControls = CreateCommandControls(overlay);
        ui.BindMatchHudCommandControls(commandControls);
        ui.Update();

        objectives.parent.Find("AriaAssistantButton").GetComponent<Button>().onClick.Invoke();
        AriaCommandAssistantPopupView popup = overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().onClick.Invoke();

        GameObject cue = FindLoadedObject("AriaAssistantTargetIndicatorRuntime");
        Assert.NotNull(cue);
        Assert.IsTrue(cue.activeInHierarchy,
            "The first Show Me click must point at the Rifle Squad UI immediately, before ECS responds.");
        Assert.AreEqual("SELECT SQUAD", cue.GetComponentInChildren<TMP_Text>(true).text);
        Assert.IsFalse(cue.transform.IsChildOf(squadTray.transform),
            "The squad-button arrow must use the top-level HUD canvas so the tray cannot clip it.");
        Assert.IsFalse(popup.IsOpen);
        Assert.AreEqual(1, gateway.AssistantIntentRequestCount);

        SetPrivateField(ui, "_nextAssistantPanelRefreshTime", 0f);
        ui.Update();
        Assert.IsTrue(cue.activeInHierarchy,
            "The Select Squad cue must persist while the player has not clicked the squad button.");

        FindNamed(squadTray.transform, "SoldierCard").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(cue.activeSelf,
            "Selecting the highlighted squad button must complete and remove the Select Squad cue.");
        Assert.AreEqual(UiAssistantCommandIntentKind.StopAssistantControl, gateway.LastAssistantIntentKind);

        // Deliberately leave the panel projection on Select to cover the one-frame stale
        // read observed in the real mission. The next explicit Show Me must still teach Move.
        objectives.parent.Find("AriaAssistantButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(popup.IsOpen);
        FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen);
        Assert.IsTrue(cue.activeInHierarchy);
        Assert.AreEqual("PRESS MOVE", cue.GetComponentInChildren<TMP_Text>(true).text,
            "After the squad is selected, the next Show Me must point to the Move command button.");

        gateway.AssistantHighlight = CreateHighlightModel(511u, recommendationKind: 2);
        SetPrivateField(ui, "_nextAssistantPanelRefreshTime", 0f);
        ui.Update();
        var commandInput = new MatchOverlayCommandInputUiSystemHelper();
        commandInput.Bind(
            commandControls,
            new AcceptedSelectionUiCommand(),
            commandModeQueued: ui.AcknowledgeMatchHudGuidedCommandMode);
        commandControls.MoveButton.onClick.Invoke();
        Assert.IsFalse(cue.activeSelf,
            "Clicking Move must finish the command-button instruction before the destination instruction.");

        // Keep the structured panel deliberately stale on Select. The active highlight is
        // already Move, so the next explicit Show Me must reveal the ground destination.
        objectives.parent.Find("AriaAssistantButton").GetComponent<Button>().onClick.Invoke();
        FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().onClick.Invoke();
        GameObject worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.NotNull(worldRing);
        Assert.IsTrue(worldRing.activeSelf,
            "The explicit Show Me after clicking Move must reveal the authored ground target.");
        Assert.AreEqual("CLICK DESTINATION", cue.GetComponentInChildren<TMP_Text>(true).text);
        commandInput.Unbind(commandControls);
        ui.Dispose();
    }

    [Test]
    public void MissionReplay_ResetClearsCompletedGuidanceState()
    {
        var presentation = new AssistantHighlightPresentationSystemHelper();
        presentation.ApplyReadModel(CreateHighlightModel(601u, recommendationKind: 2));
        SetPrivateField(presentation, "_selectSquadCompleted", true);
        SetPrivateField(presentation, "_commandGuidanceArmed", true);
        SetPrivateField(presentation, "_awaitingNextShowMe", true);
        SetPrivateField(presentation, "_worldTargetShowRequested", true);

        presentation.ResetForMissionAttempt();

        Assert.IsFalse(GetPrivateField<bool>(presentation, "_selectSquadCompleted"));
        Assert.IsFalse(GetPrivateField<bool>(presentation, "_commandGuidanceArmed"));
        Assert.IsFalse(GetPrivateField<bool>(presentation, "_awaitingNextShowMe"));
        Assert.IsFalse(GetPrivateField<bool>(presentation, "_worldTargetShowRequested"));
        Assert.IsFalse(presentation.LastAppliedModel.Active,
            "A replay must not reuse the previous attempt's squad centroid or command step.");
    }

    [Test]
    public void PopupContentVersionChangePreservesPendingGuidanceState()
    {
        var shellObject = new GameObject("ActiveMissionShell");
        var shellContent = shellObject.AddComponent<UIShellContentView>();
        var ui = new MainMenuPlayUI();
        ui.BindGuidedHudRuntime(shellContent);

        MatchHudAssistantUiSystemHelper assistant =
            GetPrivateField<MatchHudAssistantUiSystemHelper>(ui, "_matchHudAssistantUiSystem");
        AssistantHighlightPresentationSystemHelper presentation =
            GetPrivateField<AssistantHighlightPresentationSystemHelper>(
                assistant,
                "_highlightPresentationSystem");
        presentation.ApplyReadModel(CreateHighlightModel(701u, recommendationKind: 2));
        SetPrivateField(presentation, "_commandGuidanceArmed", true);
        SetPrivateField(presentation, "_awaitingNextShowMe", true);
        SetPrivateField(presentation, "_worldTargetShowRequested", true);

        shellContent.MarkContentChanged();
        ui.BindGuidedHudRuntime(shellContent);

        Assert.IsTrue(GetPrivateField<bool>(presentation, "_commandGuidanceArmed"));
        Assert.IsTrue(GetPrivateField<bool>(presentation, "_awaitingNextShowMe"));
        Assert.IsTrue(GetPrivateField<bool>(presentation, "_worldTargetShowRequested"));
        Assert.IsTrue(presentation.LastAppliedModel.Active,
            "Installing or closing a popup must not reset the active mission tutorial.");
        ui.Dispose();
        UnityEngine.Object.DestroyImmediate(shellObject);
    }

    [Test]
    public void BindMatchHudAssistant_RebindPreservesPendingM02Action()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out _);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        GameObject popupPrefab = LoadPopupPrefab();
        ui.BindMatchHudAssistant(header.gameObject, overlay, popupPrefab);

        MatchHudAssistantUiSystemHelper assistant =
            GetPrivateField<MatchHudAssistantUiSystemHelper>(ui, "_matchHudAssistantUiSystem");
        SetPrivateField(assistant, "_pendingM02DoItStep", (byte)6);
        SetPrivateField(assistant, "_pendingM02DoItUntilUnscaledTime", 123f);
        SetPrivateField(assistant, "_narratedTutorialCues", (ushort)(1 << 5));

        ui.BindMatchHudAssistant(header.gameObject, overlay, popupPrefab);

        Assert.AreEqual(6, GetPrivateField<byte>(assistant, "_pendingM02DoItStep"));
        Assert.AreEqual(123f,
            GetPrivateField<float>(assistant, "_pendingM02DoItUntilUnscaledTime"));
        Assert.AreEqual((ushort)(1 << 5),
            GetPrivateField<ushort>(assistant, "_narratedTutorialCues"),
            "Rebinding controls after opening the Build drawer must not replay ARIA or lose DO IT.");
        ui.Dispose();
    }

    [Test]
    public void MissionReplay_ShellReplacementClearsPendingGuidanceState()
    {
        var firstShellObject = new GameObject("FirstMissionShell");
        var nextShellObject = new GameObject("NextMissionShell");
        var ui = new MainMenuPlayUI();
        ui.BindGuidedHudRuntime(firstShellObject.AddComponent<UIShellContentView>());

        MatchHudAssistantUiSystemHelper assistant =
            GetPrivateField<MatchHudAssistantUiSystemHelper>(ui, "_matchHudAssistantUiSystem");
        AssistantHighlightPresentationSystemHelper presentation =
            GetPrivateField<AssistantHighlightPresentationSystemHelper>(
                assistant,
                "_highlightPresentationSystem");
        presentation.ApplyReadModel(CreateHighlightModel(702u, recommendationKind: 2));
        SetPrivateField(presentation, "_commandGuidanceArmed", true);
        SetPrivateField(presentation, "_awaitingNextShowMe", true);
        SetPrivateField(presentation, "_worldTargetShowRequested", true);

        ui.BindGuidedHudRuntime(nextShellObject.AddComponent<UIShellContentView>());

        Assert.IsFalse(GetPrivateField<bool>(presentation, "_commandGuidanceArmed"));
        Assert.IsFalse(GetPrivateField<bool>(presentation, "_awaitingNextShowMe"));
        Assert.IsFalse(GetPrivateField<bool>(presentation, "_worldTargetShowRequested"));
        Assert.IsFalse(presentation.LastAppliedModel.Active,
            "A new Match HUD owner must reset tutorial state for the next attempt.");
        ui.Dispose();
        UnityEngine.Object.DestroyImmediate(nextShellObject);
        UnityEngine.Object.DestroyImmediate(firstShellObject);
    }

    [Test]
    public void DelayedEcsHighlight_RemainsPendingUntilCanonicalTargetOrMissionReset()
    {
        var presentation = new AssistantHighlightPresentationSystemHelper();
        presentation.ApplyReadModel(UiAssistantHighlightModel.Empty);

        presentation.BeginPendingShowMe(recommendationKind: 2, targetKind: 1);
        presentation.AcknowledgeCommandMode(TacticalCommandMode.Move);
        presentation.BeginPendingShowMe(recommendationKind: 2, targetKind: 1);
        Assert.IsTrue(presentation.LastAppliedModel.Active,
            "The second Show Me request must retain its pending recommendation identity.");

        presentation.ApplyReadModel(UiAssistantHighlightModel.Empty);

        Assert.IsTrue(presentation.LastAppliedModel.Active,
            "A stale empty read must not consume a pending request before ECS publishes its canonical target.");
        Assert.IsTrue(GetPrivateField<bool>(presentation, "_pendingFirstShowMe"));
        presentation.ResetForMissionAttempt();
        Assert.IsFalse(presentation.LastAppliedModel.Active,
            "Mission reset must clear a pending target from the previous attempt.");
        Assert.IsFalse(GetPrivateField<bool>(presentation, "_commandGuidanceArmed"));
        Assert.IsFalse(GetPrivateField<bool>(presentation, "_worldTargetShowRequested"));
    }

    private static void AssertGuidedCommandHighlight(
        byte recommendationKind,
        TacticalCommandMode commandMode,
        string initialLabel,
        string armedLabel)
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out RectTransform objectives);
        var gateway = new FakeAssistantPanelGateway(
            CreateStructuredModel(
                (uint)(100 + recommendationKind),
                recommendationKind: recommendationKind,
                recommendationTargetKind: 1),
            CreateHighlightModel((uint)(200 + recommendationKind), recommendationKind));
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
        MatchOverlayCommandControlsView commandControls = CreateCommandControls(overlay);
        ui.BindMatchHudCommandControls(commandControls);
        ui.ApplyMatchHudCommandMode(commandMode);
        var commandInput = new MatchOverlayCommandInputUiSystemHelper();
        commandInput.Bind(
            commandControls,
            new AcceptedSelectionUiCommand(),
            commandModeQueued: ui.AcknowledgeMatchHudGuidedCommandMode);
        ui.Update();
        // Passive ECS projection can arrive immediately after the Show Me read model.
        // It must not be mistaken for the player's command-button click.
        ui.ApplyMatchHudCommandMode(commandMode);

        GameObject indicator = FindLoadedObject("AriaAssistantTargetIndicatorRuntime");
        GameObject worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.NotNull(indicator);
        Assert.IsTrue(indicator.activeInHierarchy,
            "The guided command-button indicator must be visible in the active HUD hierarchy.");
        Assert.IsTrue(indicator.GetComponent<Canvas>().overrideSorting,
            "The animated ARIA indicator must have an isolated canvas to avoid rebuilding the full HUD.");
        Assert.AreEqual(Vector3.one, indicator.transform.localScale);
        Assert.AreEqual(initialLabel, indicator.GetComponentInChildren<TMP_Text>(true).text);
        Assert.IsTrue(worldRing == null || !worldRing.activeSelf,
            "Before the command is armed, ARIA must teach the command button instead of exposing the world target.");

        Button guidedButton = commandMode == TacticalCommandMode.Move
            ? commandControls.MoveButton
            : commandControls.AttackButton;
        guidedButton.onClick.Invoke();
        worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.IsFalse(indicator.activeSelf,
            "Accepting the command button must end this Show Me step immediately.");
        Assert.IsTrue(worldRing == null || !worldRing.activeSelf,
            "ARIA must wait for another Show Me request before exposing the world target.");

        // Opening the ARIA popup can clear the transient gameplay command mode. The
        // acknowledged guidance step must still remember that Move/Attack was pressed.
        ui.ApplyMatchHudCommandMode(TacticalCommandMode.None);

        gateway.AssistantHighlight = CreateHighlightModel(
            (uint)(300 + recommendationKind), recommendationKind);
        SetPrivateField(ui, "_nextAssistantPanelRefreshTime", 0f);
        ui.Update();
        Assert.IsFalse(indicator.activeSelf,
            "A refreshed ECS preview must not reveal the world target without another Show Me click.");

        objectives.parent.Find("AriaAssistantButton").GetComponent<Button>().onClick.Invoke();
        AriaCommandAssistantPopupView popup = overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        Assert.IsTrue(popup.IsOpen);
        FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen);
        worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.AreEqual(armedLabel, indicator.GetComponentInChildren<TMP_Text>(true).text);
        Assert.NotNull(worldRing);
        Assert.IsTrue(worldRing.activeSelf,
            "The next explicit Show Me after arming the command must reveal its world target.");
        ui.CompleteMatchHudGuidedWorldTarget(commandMode);
        Assert.IsFalse(indicator.activeSelf,
            "Completing the highlighted world action must remove its screen arrow immediately.");
        Assert.IsFalse(worldRing.activeSelf,
            "Completing the highlighted world action must remove its ground ring immediately.");
        commandInput.Unbind(commandControls);
        ui.Dispose();
        UiShellRuntimeGateway.Register(null);
    }

    private static UiAssistantPanelModel CreateStructuredModel(
        uint version,
        bool canExecute = true,
        byte recommendationKind = 0,
        byte recommendationTargetKind = 0,
        byte tutorialStep = 0,
        byte tutorialStepCount = 0,
        string recommendationTitle = "Focus hostile armor",
        string recommendationBody = "Preview the verified hostile source before dispatch.",
        bool tutorialRightToLeft = false)
    {
        return new UiAssistantPanelModel(
            version,
            true,
            127,
            new UiAssistantGoalRowModel(true, 10, "Secure the relay", "Hold the uplink perimeter.", 0, 2, true),
            new UiAssistantGoalRowModel(true, 11, "Protect civilians", "Keep the evacuation route open.", 2, 3, false),
            UiAssistantGoalRowModel.Empty,
            new UiAssistantMessageRowModel(true, 20, "HOSTILE ARMOR", "Raven Tank is firing on Echo Squad.", 3, 1, 1, true, false),
            UiAssistantMessageRowModel.Empty,
            UiAssistantMessageRowModel.Empty,
            new UiAssistantMessageRowModel(true, 30, "Fuel convoy ready", "Depot route is clear.", 0, 2, 2, false, false),
            UiAssistantMessageRowModel.Empty,
            new UiAssistantTargetLockModel(true, 2, 1, "Raven Tank", "Echo Squad", "140 M", "72 / 100", "HOSTILE", "PREVIEW", "Line of fire verified."),
            new UiAssistantNarrationModel((byte)UiAssistantNarrationStateKind.Presented, 3, "PRESENTED", "Hostile armor identified.", string.Empty, true),
            true,
            recommendationTitle,
            recommendationBody,
            "CRITICAL",
            "SHOW ME",
            true,
            canExecute,
            true,
            false,
            "PLAYER CONTROL",
            "You are issuing orders directly.",
            recommendationKind: recommendationKind,
            recommendationTargetKind: recommendationTargetKind,
            tutorialStep: tutorialStep,
            tutorialStepCount: tutorialStepCount,
            tutorialRightToLeft: tutorialRightToLeft);
    }

    private static UiAssistantHighlightModel CreateHighlightModel(
        uint version,
        byte recommendationKind = 0,
        byte targetKind = 1)
    {
        return new UiAssistantHighlightModel(
            version, true, 7, 3101, recommendationKind, targetKind, 12f, 3f, 9f, 1f);
    }

    private static MatchOverlayCommandControlsView CreateCommandControls(RectTransform parent)
    {
        var root = new GameObject(
            "AssistantUiTestCommandControls",
            typeof(RectTransform),
            typeof(MatchOverlayCommandControlsView));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(900f, 180f);
        Button move = CreateCommandButton("MoveCommandButton", rootRect, new Vector2(-180f, 0f));
        Button attack = CreateCommandButton("AttackCommandButton", rootRect, new Vector2(180f, 0f));
        SetPrivateField(root.GetComponent<MatchOverlayCommandControlsView>(), "moveButton", move);
        SetPrivateField(root.GetComponent<MatchOverlayCommandControlsView>(), "attackButton", attack);
        return root.GetComponent<MatchOverlayCommandControlsView>();
    }

    private static MatchHudSquadTrayView CreateSquadTray(RectTransform parent)
    {
        var root = new GameObject(
            "AssistantUiTestSquadTray",
            typeof(RectTransform),
            typeof(MatchHudSquadTrayView));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Button soldier = CreateCommandButton("SoldierCard", rootRect, new Vector2(-500f, -350f));
        var cards = new MatchHudSquadTrayView.Card[5];
        cards[0] = new MatchHudSquadTrayView.Card
        {
            Button = soldier,
            FrameImage = soldier.GetComponent<Image>(),
            PortraitImage = soldier.GetComponent<Image>()
        };
        SetPrivateField(root.GetComponent<MatchHudSquadTrayView>(), "cards", cards);
        return root.GetComponent<MatchHudSquadTrayView>();
    }

    private static Button CreateCommandButton(string name, RectTransform parent, Vector2 position)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(260f, 120f);
        Button button = root.GetComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        return button;
    }

    private static void SetPrivateField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, name);
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, name);
        return (T)field.GetValue(target);
    }

    private static void CreateHudHarness(
        bool includeAssistantButton,
        out RectTransform overlay,
        out RectTransform header,
        out RectTransform objectives)
    {
        overlay = CreateRectRoot("AssistantUiTestOverlay", new Vector2(4800f, 2160f));
        header = CreateRect("HeaderContent", overlay);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0f, 560f);

        objectives = CreateRect("ObjectivesPanel", header);
        objectives.anchorMin = new Vector2(0f, 1f);
        objectives.anchorMax = new Vector2(0f, 1f);
        objectives.pivot = new Vector2(0f, 1f);
        objectives.anchoredPosition = new Vector2(16f, -16f);
        objectives.sizeDelta = new Vector2(670f, 520f);
        RectTransform objectivesContent = CreateRect("ObjectivesContent", objectives);
        RectTransform elapsed = CreateRect("Elapsed", objectivesContent);
        elapsed.gameObject.AddComponent<MatchHudObjectivesElapsedView>();

        if (includeAssistantButton)
            CreateAssistantButton(header);
    }

    private static void CreateAssistantButton(RectTransform parent)
    {
        GameObject matchHudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudPrefabPath);
        Assert.NotNull(matchHudPrefab, MatchHudPrefabPath);
        Transform source = FindNamed(matchHudPrefab.transform, "AriaAssistantButton");
        Assert.NotNull(source, "The Match HUD prefab must provide the canonical ARIA hierarchy for tests.");
        GameObject root = UnityEngine.Object.Instantiate(source.gameObject, parent, false);
        root.name = "AriaAssistantButton";
        root.SetActive(true);
    }

    private static TMP_Text CreateAssistantButtonText(string name, RectTransform parent, string value)
    {
        RectTransform rect = CreateRect(name, parent);
        TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        return text;
    }

    private static RectTransform CreateRectRoot(string name, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        return rect;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        return root.GetComponent<RectTransform>();
    }

    private static GameObject CreatePopupPeer<T>(string name) where T : Component
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(T));
        root.SetActive(true);
        return root;
    }

    private static GameObject LoadPopupPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);
        Assert.NotNull(prefab, PopupPrefabPath);
        return prefab;
    }

    private static Transform FindNamed(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindNamed(root.GetChild(i), name);
            if (match != null)
                return match;
        }
        return null;
    }

    private static GameObject FindLoadedObject(string name)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int index = 0; index < objects.Length; index++)
        {
            if (objects[index] != null && objects[index].name == name)
                return objects[index];
        }
        return null;
    }

    private static void AssertVisibleIndicator(GameObject indicator, string expectedText)
    {
        Assert.NotNull(indicator, "The automatic tutorial indicator must exist.");
        Assert.IsTrue(indicator.activeInHierarchy,
            $"The automatic tutorial indicator '{expectedText}' must be visible.");
        Assert.AreEqual(expectedText, indicator.GetComponentInChildren<TMP_Text>(true).text);
    }

    private static void AssertWorldRingCenteredAt(Vector3 expectedCenter)
    {
        GameObject ringObject = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.NotNull(ringObject, "The canonical tutorial world ring must exist.");
        Assert.IsTrue(ringObject.activeInHierarchy, "The canonical tutorial world ring must be visible.");
        LineRenderer ring = ringObject.GetComponent<LineRenderer>();
        Assert.NotNull(ring);
        var positions = new Vector3[ring.positionCount];
        ring.GetPositions(positions);
        Vector3 center = Vector3.zero;
        for (int index = 0; index < positions.Length; index++)
            center += positions[index];
        center /= positions.Length;
        Assert.That(center.x, Is.EqualTo(expectedCenter.x).Within(0.01f));
        Assert.That(center.y, Is.EqualTo(expectedCenter.y).Within(0.01f));
        Assert.That(center.z, Is.EqualTo(expectedCenter.z).Within(0.01f));
    }

    private static string Text(Transform root, string name)
    {
        Transform match = FindNamed(root, name);
        Assert.NotNull(match, name);
        TMP_Text text = match.GetComponent<TMP_Text>();
        Assert.NotNull(text, name);
        return text.text;
    }

    private static Vector2 CenterScreenPoint(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) * 0.5f);
    }

    private static bool Contains(RectTransform parent, RectTransform child)
    {
        if (parent == null || child == null)
            return false;
        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners);
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 local = parent.InverseTransformPoint(corners[i]);
            if (!parent.rect.Contains(local))
                return false;
        }
        return true;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T match = root.GetComponentInChildren<T>(true);
            if (match != null)
                return match;
        }
        return null;
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

    private sealed class AcceptedSelectionUiCommand : ISelectionUiCommand
    {
        public void CaptureUiClickSequence() { }
        public bool RequestDeselectAll() => true;
        public bool RequestEnterSelectionMode() => true;
        public bool RequestExitSelectionMode() => true;
        public bool RequestMoveCommandMode() => true;
        public bool RequestAttackCommandMode() => true;
        public bool RequestScanCommandMode() => true;
        public bool RequestBoardTargetMode() => true;
        public bool RequestToggleTacticalFollowCameraMode() => true;
        public bool RequestHoldPosition() => true;
        public bool RequestStop() => true;
        public bool RequestBoardAllSelectedTransport() => true;
        public bool RequestCancelActiveCommandMode() => true;
    }

    private sealed class FakeAssistantPanelGateway : IUiShellRuntimeGateway, IUiAssistantPanelStateGateway,
        IUiTutorialNarrationGateway,
        IUiMissionHudRestrictionsGateway
    {
        public UiAssistantHighlightModel AssistantHighlight { get; set; }
        public UiAssistantHighlightModel HighlightAfterShowMe { get; set; }
        public int AssistantIntentRequestCount { get; private set; }
        public UiAssistantCommandIntentKind LastAssistantIntentKind { get; private set; }
        public bool LastAssistantIntentFromTakeover { get; private set; }
        public UiAssistantPanelModel AssistantPanel { get; set; }
        public List<bool> AssistantPanelOpenStates { get; } = new();
        public List<byte> TutorialNarrationSteps { get; } = new();
        public List<UiTutorialNarrationPhase> TutorialNarrationPhases { get; } = new();
        public List<string> TutorialNarrationTexts { get; } = new();
        public int TutorialNarrationFailuresRemaining { get; set; }
        public bool LastAssistantPanelOpen => AssistantPanelOpenStates.Count > 0 &&
                                              AssistantPanelOpenStates[AssistantPanelOpenStates.Count - 1];
        public bool CinematicInteractionLocked { get; set; }

        public FakeAssistantPanelGateway(UiAssistantPanelModel assistantPanel, UiAssistantHighlightModel assistantHighlight)
        {
            AssistantPanel = assistantPanel;
            AssistantHighlight = assistantHighlight;
        }

        public bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory) => false;
        public bool TryEnqueueUiAction(UiActionKind kind, int payloadId) => false;
        public bool TrySetAssistantPanelOpen(bool open)
        {
            AssistantPanelOpenStates.Add(open);
            return true;
        }

        public bool TryEnqueueTutorialNarration(
            byte tutorialStep,
            byte tutorialStepCount,
            UiTutorialNarrationPhase phase,
            string text)
        {
            if (TutorialNarrationFailuresRemaining > 0)
            {
                TutorialNarrationFailuresRemaining--;
                return false;
            }

            TutorialNarrationSteps.Add(tutorialStep);
            TutorialNarrationPhases.Add(phase);
            TutorialNarrationTexts.Add(text);
            return true;
        }

        public bool TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind kind, bool fromTakeover)
        {
            AssistantIntentRequestCount++;
            LastAssistantIntentKind = kind;
            LastAssistantIntentFromTakeover = fromTakeover;
            if (kind == UiAssistantCommandIntentKind.ShowRecommendation && HighlightAfterShowMe.Active)
                AssistantHighlight = HighlightAfterShowMe;
            return true;
        }

        public bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading) { loading = default; return false; }
        public bool TrySetLoadingProgress(float progress01, string status, bool complete) => false;
        public bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics) { diagnostics = default; return false; }
        public bool TryReadShellState(out UiShellStateModel state) { state = default; return false; }
        public bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile) { profile = default; return false; }
        public bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources) { resources = default; return false; }
        public bool TryReadMissionResult(out UiMissionResultPopupModel result) { result = default; return false; }
        public bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection) { selection = UiMatchHudSelectionPanelModel.Hidden; return false; }
        public bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState) { commandState = default; return false; }
        public bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header) { header = UiMatchHudHeaderModel.Default; return false; }
        public bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces) { statusSurfaces = UiMatchHudStatusSurfacesModel.Default; return false; }
        public bool TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel) { assistantPanel = AssistantPanel; return true; }
        public bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight) { assistantHighlight = AssistantHighlight; return AssistantHighlight.Active; }
        public bool TryReadMissionHudRestrictions(out UiMissionHudRestrictionsModel restrictions)
        {
            restrictions = new UiMissionHudRestrictionsModel(
                "campaign.chapter01.mission01", true, true, true, true, true,
                CinematicInteractionLocked);
            return true;
        }
        public bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap) { minimap = UiMatchHudMinimapModel.Default; return false; }
        public bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer) { passengerDrawer = UiMatchHudPassengerDrawerModel.Hidden; return false; }
        public bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray) { squadTray = UiMatchHudSquadTrayModel.Default; return false; }
        public bool TryReadBuildDrawer(out UiBuildDrawerModel drawer) { drawer = UiBuildDrawerModel.Empty; return false; }
        public bool TryReadResourceExchange(out UiResourceExchangeModel exchange) { exchange = UiResourceExchangeModel.Empty; return false; }
        public bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar) { placementBar = UiBuildPlacementConfirmationBarModel.Hidden; return false; }
        public bool TryReadArmoryCategory(out ArmoryCatalogCategory category) { category = ArmoryCatalogCategory.Characters; return false; }
        public bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category) => false;
        public bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands) => false;
        public bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion) => false;
    }
}
