#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class M02EstablishBaseGuidanceTests
{
    private const string FocusedMarker =
        "[M02EstablishBaseGuidanceValidation] result=Passed tests=11";

    [MenuItem("Game/Validation/Run M02 Establish Base Guidance Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseGuidanceTests tests = new();
            tests.FirstStepTargetsTheRealBuildControl();
            tests.BuildAcknowledgementAdvancesToBarracksSelection();
            tests.SecondStepTargetsTheRealBarracksControl();
            tests.AcknowledgedBarracksSelectionIsNotRepublished();
            tests.AuthoritativePlacementClearsSelectionGuidance();
            tests.M02UsesItsOwnNineStepTutorialSequence();
            tests.UiSurfacePreviewCompletesWithoutWorldResolution();
            tests.BuildDoItInvokesTheBoundBuildButton();
            tests.BarracksDoItInvokesSelectionWithoutPlacement();
            tests.M01TutorialProjectionRemainsUnchanged();
            tests.UiSurfaceGuidanceUsesTypedControlsWithoutScreenCoordinates();
            Debug.Log(FocusedMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseGuidanceValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [MenuItem("Game/Validation/Run M02 Establish Base Guidance Regressions")]
    public static void RunRegressionValidation()
    {
        try
        {
            RunValidation(RunFocusedValidation);
            RunValidation(M01FirstContactGuidanceTests.RunFocusedValidation);
            RunValidation(M02EstablishBaseBuildCatalogTests.RunFocusedValidation);
            RunValidation(MatchHudAssistantUiSystemHelperTests.RunFocusedValidation);
            RunValidation(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            Debug.Log("[M02EstablishBaseGuidanceRegressionValidation] result=Passed suites=5");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseGuidanceRegressionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void FirstStepTargetsTheRealBuildControl()
    {
        Assert.IsTrue(TryProject(default, default, out CampaignMissionGuidanceProjectionComponent guidance));
        Assert.AreEqual(CampaignMissionGuidancePromptKind.EstablishBaseOpenBuild, guidance.Prompt);
        Assert.AreEqual(AssistantRecommendationKind.Build, guidance.RecommendationKind);
        Assert.AreEqual(AssistantTargetKind.UiSurface, guidance.TargetKind);
        Assert.AreEqual("ui.match.build", guidance.TargetId.ToString());
        Assert.AreEqual("Open Build", guidance.Title.ToString());
        Assert.AreEqual("Open Build to restore the forward post.", guidance.Body.ToString());
        Assert.AreEqual(1, guidance.CanShow);
        Assert.AreEqual(1, guidance.CanExecute);
        Assert.AreEqual("DO IT", guidance.ActionLabel.ToString());
    }

    [Test]
    public void BuildAcknowledgementAdvancesToBarracksSelection()
    {
        Assert.IsTrue(TryProject(default, default, out CampaignMissionGuidanceProjectionComponent build));
        build.AcknowledgedGuidanceId = build.GuidanceId;

        Assert.IsTrue(TryProject(build, default, out CampaignMissionGuidanceProjectionComponent barracks));
        Assert.AreEqual(CampaignMissionGuidancePromptKind.EstablishBaseSelectBarracks, barracks.Prompt);
        Assert.AreNotEqual(build.GuidanceId, barracks.GuidanceId);
    }

    [Test]
    public void SecondStepTargetsTheRealBarracksControl()
    {
        CampaignMissionGuidanceProjectionComponent barracks = ProjectBarracksStep();
        Assert.AreEqual(AssistantRecommendationKind.Select, barracks.RecommendationKind);
        Assert.AreEqual(AssistantTargetKind.UiSurface, barracks.TargetKind);
        Assert.AreEqual("ui.build_drawer.barracks", barracks.TargetId.ToString());
        Assert.AreEqual("Select Barracks", barracks.Title.ToString());
        Assert.AreEqual("Select Barracks from the building catalog.", barracks.Body.ToString());
        Assert.AreEqual(1, barracks.CanShow);
        Assert.AreEqual(1, barracks.CanExecute);
    }

    [Test]
    public void AcknowledgedBarracksSelectionIsNotRepublished()
    {
        CampaignMissionGuidanceProjectionComponent barracks = ProjectBarracksStep();
        barracks.AcknowledgedGuidanceId = barracks.GuidanceId;
        Assert.IsFalse(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            barracks,
            out _));
        Assert.IsFalse(TryProject(barracks, default, out _));
    }

    [Test]
    public void AuthoritativePlacementClearsSelectionGuidance()
    {
        CampaignMissionGuidanceProjectionComponent barracks = ProjectBarracksStep();
        CampaignMissionAttemptFactsComponent facts = default;
        facts.RequiredBuildingPlacedCount = 1;

        Assert.IsTrue(TryProject(barracks, facts, out CampaignMissionGuidanceProjectionComponent cleared));
        Assert.AreEqual(0, cleared.Active);
        Assert.AreEqual(CampaignMissionGuidancePromptKind.None, cleared.Prompt);
    }

    [Test]
    public void M02UsesItsOwnNineStepTutorialSequence()
    {
        Assert.IsTrue(TryProject(default, default, out CampaignMissionGuidanceProjectionComponent build));
        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            build,
            out AssistantRecommendationElement buildRecommendation));
        Assert.AreEqual(2, buildRecommendation.TutorialStep);
        Assert.AreEqual(9, buildRecommendation.TutorialStepCount);

        CampaignMissionGuidanceProjectionComponent barracks = ProjectBarracksStep();
        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            barracks,
            out AssistantRecommendationElement barracksRecommendation));
        Assert.AreEqual(3, barracksRecommendation.TutorialStep);
        Assert.AreEqual(9, barracksRecommendation.TutorialStepCount);
    }

    [Test]
    public void UiSurfacePreviewCompletesWithoutWorldResolution()
    {
        AssistantCommandIntentRequestElement preview = new()
        {
            Kind = AssistantCommandIntentKind.ShowRecommendation,
            TargetKind = AssistantTargetKind.UiSurface
        };
        Assert.IsTrue(AssistantCommandIntentSystem.IsUiSurfacePreview(in preview));

        preview.Kind = AssistantCommandIntentKind.FocusCamera;
        Assert.IsTrue(AssistantCommandIntentSystem.IsUiSurfacePreview(in preview));

        preview.Kind = AssistantCommandIntentKind.SelectEntity;
        Assert.IsFalse(AssistantCommandIntentSystem.IsUiSurfacePreview(in preview));
        preview.Kind = AssistantCommandIntentKind.ShowRecommendation;
        preview.TargetKind = AssistantTargetKind.WorldPosition;
        Assert.IsFalse(AssistantCommandIntentSystem.IsUiSurfacePreview(in preview));
    }

    [Test]
    public void BuildDoItInvokesTheBoundBuildButton()
    {
        GameObject root = new("M02 Build Guidance Test", typeof(RectTransform), typeof(Image), typeof(Button));
        AssistantHighlightPresentationSystemHelper helper = new();
        try
        {
            Button button = root.GetComponent<Button>();
            int actualClicks = 0;
            byte acknowledgedKind = 0;
            button.onClick.AddListener(() => actualClicks++);
            helper.Bind(null, uiSurfaceAcknowledged: kind => acknowledgedKind = kind);
            helper.BindBuildButton(button);
            helper.BeginPendingShowMe(
                (byte)AssistantRecommendationKind.Build,
                (byte)AssistantTargetKind.UiSurface);

            Assert.IsTrue(helper.TryExecuteUiSurface(
                (byte)AssistantRecommendationKind.Build,
                (byte)AssistantTargetKind.UiSurface));
            Assert.AreEqual(1, actualClicks);
            Assert.AreEqual((byte)AssistantRecommendationKind.Build, acknowledgedKind);
        }
        finally
        {
            helper.Unbind();
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BarracksDoItInvokesSelectionWithoutPlacement()
    {
        GameObject drawerObject = new("M02 Build Drawer Guidance Test", typeof(RectTransform));
        GameObject itemObject = new("Barracks Item", typeof(RectTransform), typeof(Image), typeof(Button));
        itemObject.transform.SetParent(drawerObject.transform, false);
        BuildDrawerView drawer = drawerObject.AddComponent<BuildDrawerView>();
        BuildDrawerItemView item = itemObject.AddComponent<BuildDrawerItemView>();
        Button selectionButton = itemObject.GetComponent<Button>();
        SetPrivateField(item, "selectionButton", selectionButton);
        SetPrivateField(drawer, "itemTemplate", item);

        AssistantHighlightPresentationSystemHelper helper = new();
        try
        {
            int selectionClicks = 0;
            byte acknowledgedKind = 0;
            selectionButton.onClick.AddListener(() => selectionClicks++);
            helper.Bind(null, uiSurfaceAcknowledged: kind => acknowledgedKind = kind);
            helper.BindBuildDrawer(drawer);
            helper.BeginPendingShowMe(
                (byte)AssistantRecommendationKind.Select,
                (byte)AssistantTargetKind.UiSurface);

            Assert.IsTrue(helper.TryExecuteUiSurface(
                (byte)AssistantRecommendationKind.Select,
                (byte)AssistantTargetKind.UiSurface));
            Assert.AreEqual(1, selectionClicks);
            Assert.AreEqual((byte)AssistantRecommendationKind.Select, acknowledgedKind);
            Assert.IsNull(drawer.PrimaryActionButton,
                "Barracks guidance must stop at selection and must not invoke placement.");
        }
        finally
        {
            helper.Unbind();
            UnityEngine.Object.DestroyImmediate(drawerObject);
        }
    }

    [Test]
    public void M01TutorialProjectionRemainsUnchanged()
    {
        CampaignMissionRuntimeComponent runtime = Runtime("saga.ch01.m01.first_contact");
        runtime.Phase = MissionPhaseKind.MoveToCover;
        Entity friendly = new() { Index = 7, Version = 1 };
        Assert.IsTrue(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
            default,
            runtime,
            default,
            Settings(),
            friendly,
            Entity.Null,
            new float3(3f, 0f, 5f),
            default,
            out CampaignMissionGuidanceProjectionComponent guidance));
        Assert.AreEqual(CampaignMissionGuidancePromptKind.MoveToCover, guidance.Prompt);
        Assert.AreEqual(AssistantTargetKind.WorldPosition, guidance.TargetKind);
        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            guidance,
            out AssistantRecommendationElement recommendation));
        Assert.AreEqual(2, recommendation.TutorialStep);
        Assert.AreEqual(5, recommendation.TutorialStepCount);
    }

    [Test]
    public void UiSurfaceGuidanceUsesTypedControlsWithoutScreenCoordinates()
    {
        string guidance = File.ReadAllText(
            "Assets/Game/Scripts/Runtime/Missions/CampaignMissionGuidanceProjectionSystem.cs");
        string highlight = File.ReadAllText(
            "Assets/Game/Scripts/UI/Screens/AssistantHighlightPresentationSystemHelper.UiSurfaceGuidance.cs");
        string highlightLayout = File.ReadAllText(
            "Assets/Game/Scripts/UI/Screens/AssistantHighlightPresentationSystemHelper.Guidance.cs");
        string buildDrawerGuidance = File.ReadAllText(
            "Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.MissionGuidance.cs");
        string readModel = File.ReadAllText(
            "Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Assistant.cs");

        Assert.That(guidance, Does.Contain("AssistantTargetKind.UiSurface"));
        Assert.That(guidance, Does.Contain("ui.match.build"));
        Assert.That(guidance, Does.Contain("ui.build_drawer.barracks"));
        Assert.That(highlight, Does.Contain("BindBuildButton"));
        Assert.That(highlight, Does.Contain("BindBuildDrawer"));
        Assert.That(highlight, Does.Contain("target.onClick.Invoke()"));
        Assert.That(highlightLayout, Does.Contain("GetWorldCorners"));
        Assert.That(buildDrawerGuidance, Does.Contain("UiCampaignGuidanceTargetKind.BuildButton"));
        Assert.That(buildDrawerGuidance, Does.Contain("UiCampaignGuidanceTargetKind.BarracksCatalogItem"));
        Assert.That(readModel, Does.Contain("topRecommendation.TargetKind != AssistantTargetKind.UiSurface"));
        Assert.That(guidance, Does.Not.Contain("Screen.width"));
        Assert.That(guidance, Does.Not.Contain("Screen.height"));
    }

    private static CampaignMissionGuidanceProjectionComponent ProjectBarracksStep()
    {
        Assert.IsTrue(TryProject(default, default, out CampaignMissionGuidanceProjectionComponent build));
        build.AcknowledgedGuidanceId = build.GuidanceId;
        Assert.IsTrue(TryProject(build, default, out CampaignMissionGuidanceProjectionComponent barracks));
        return barracks;
    }

    private static bool TryProject(
        in CampaignMissionGuidanceProjectionComponent current,
        in CampaignMissionAttemptFactsComponent facts,
        out CampaignMissionGuidanceProjectionComponent guidance) =>
        CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
            current,
            Runtime("saga.ch01.m02.establish_base"),
            facts,
            Settings(),
            Entity.Null,
            Entity.Null,
            default,
            default,
            out guidance);

    private static CampaignMissionRuntimeComponent Runtime(string missionId) => new()
    {
        MissionId = new FixedString64Bytes(missionId),
        SessionToken = new FixedString64Bytes("m02-guidance-session"),
        Phase = MissionPhaseKind.FindSquad,
        Outcome = MissionOutcomeKind.None,
        Guidance = NarrativeGuidanceMode.Full,
        RunKind = MissionRunKind.FirstClear,
        Version = 1,
        SourceVersion = 1,
        AttemptOrdinal = 1,
        ReplayTutorialEnabled = 1
    };

    private static AssistantSettingsComponent Settings() => new()
    {
        GuidanceLevel = AssistantGuidanceLevel.FullGuidance,
        SubtitlesEnabled = 1
    };

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing serialized field {target.GetType().Name}.{fieldName}.");
        field.SetValue(target, value);
    }

    private static void RunValidation(Action validation)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
            validation();
        if (ValidationExit.LastExitCode is int exitCode && exitCode != 0)
        {
            throw new InvalidOperationException(
                $"{validation.Method.DeclaringType?.Name}.{validation.Method.Name} failed validation.");
        }
    }
}
#endif
