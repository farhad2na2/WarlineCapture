#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using Game.Catalog.Contracts;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
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
        "[M02EstablishBaseGuidanceValidation] result=Passed tests=33";
    private static readonly float3 CanonicalBuildAnchor = new(1040.5f, 0.009179778f, 394.5f);

    [MenuItem("Game/Validation/Run M02 Establish Base Guidance Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseGuidanceTests tests = new();
            tests.FirstStepTargetsTheRealBuildControl();
            tests.BuildAcknowledgementAdvancesToBarracksSelection();
            tests.SecondStepTargetsTheRealBarracksControl();
            tests.AcknowledgedBarracksSelectionAdvancesToFootprintPlacement();
            tests.FootprintPlacementTargetsTheCanonicalBuildLot();
            tests.AuthoritativePlacementAdvancesToResourceSpendReview();
            tests.AcknowledgedResourceSpendWaitsForCompletionThenQueuesRifle();
            tests.RifleStepTargetsTheRealProductionControls();
            tests.AcknowledgedRifleQueueClearsGuidance();
            tests.CompletedRifleKeepsAriaHiddenUntilWaveWarning();
            tests.WaveWarningPreemptsIncompleteRifleProduction();
            tests.WaveActivationPreemptsIncompleteRifleProduction();
            tests.CriticalWarningSurvivesContextualAndMinimalGuidanceModes();
            tests.WaveWarningTargetsCanonicalDefenseLaneWithoutCombatControl();
            tests.WaveActivationTransitionsToDefenseWithoutIssuingACommand();
            tests.DefeatedWaveClearsDefenseGuidance();
            tests.WarningAndDefenseTextMatchesEnglishAndPersian();
            tests.BarracksAndRifleGuidanceHaveDistinctTypedTargets();
            tests.BarracksAndRifleGuidanceTextMatchesEnglishAndPersian();
            tests.M02UsesItsOwnNineStepTutorialSequence();
            tests.UiSurfacePreviewCompletesWithoutWorldResolution();
            tests.BuildDoItInvokesTheBoundBuildButton();
            tests.BarracksDoItInvokesSelectionWithoutPlacement();
            tests.PlacementDoItUsesTheRealPlaceAndConfirmButtons();
            tests.ResourceSpendContinueUsesTheTypedResourceStrip();
            tests.RifleDoItUsesTheRealRecruitButton();
            tests.PlacementBarDisplaysCreditsAndMaterialsCost();
            tests.M02GuidanceCannotBorrowM01NarrationEvents();
            tests.M01TutorialProjectionRemainsUnchanged();
            tests.UiSurfaceGuidanceUsesTypedControlsWithoutScreenCoordinates();
            tests.M02OpeningUsesAHorizontalBaseSweep();
            tests.M02OpeningCannotEmitM01CivilianPanicAudio();
            tests.M02OpeningCompletesAfterTheSingleSweep();
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

    [Test]
    public void M02OpeningUsesAHorizontalBaseSweep()
    {
        using BlobBuilder builder = new(Allocator.Temp);
        ref OperationMapBlob map = ref builder.ConstructRoot<OperationMapBlob>();
        BlobBuilderArray<OperationMapAnchorBlob> anchors = builder.Allocate(ref map.Anchors, 2);
        anchors[0] = new OperationMapAnchorBlob
        {
            Id = new FixedString64Bytes("anchor.ch01.m02.resource_focus"),
            Position = new float3(830.5f, 0f, 375.5f)
        };
        anchors[1] = new OperationMapAnchorBlob
        {
            Id = new FixedString64Bytes("anchor.ch01.m02.build_lot"),
            Position = CanonicalBuildAnchor
        };
        using BlobAssetReference<OperationMapBlob> blob =
            builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Temp);

        CampaignMissionSpawnSystem.ResolveOpeningPresentationFocus(
            new FixedString64Bytes("saga.ch01.m02.establish_base"),
            ref blob.Value,
            new float3(1f),
            new float3(2f),
            out float3 start,
            out float3 end,
            out float3 midpoint);

        Assert.AreEqual(anchors[0].Position, start);
        Assert.AreEqual(CanonicalBuildAnchor, end);
        Assert.AreEqual(math.lerp(start, end, 0.5f), midpoint);
        Assert.Greater(end.x - start.x, 150f);
        Assert.Less(math.abs(end.z - start.z), 40f);
    }

    [Test]
    public void M02OpeningCannotEmitM01CivilianPanicAudio()
    {
        FixedString64Bytes m01 = new("saga.ch01.m01.first_contact");
        FixedString64Bytes m02 = new("saga.ch01.m02.establish_base");
        Assert.IsTrue(CampaignMissionPatrolOrderSystem.ShouldEmitOpeningPanicAudio(m01));
        Assert.IsFalse(CampaignMissionPatrolOrderSystem.ShouldEmitOpeningPanicAudio(m02));
        Assert.IsFalse(CampaignMissionPatrolOrderSystem.ShouldUseEstablishBaseOpening(m01));
        Assert.IsTrue(CampaignMissionPatrolOrderSystem.ShouldUseEstablishBaseOpening(m02));
    }

    [Test]
    public void M02OpeningCompletesAfterTheSingleSweep()
    {
        Assert.AreEqual(1, CampaignMissionPatrolOrderSystem.EvaluateEstablishBaseOpeningStage(
            0,
            CampaignMissionPatrolOrderSystem.EstablishBaseOpeningHoldMilliseconds,
            0,
            out byte queueSweep));
        Assert.AreEqual(1, queueSweep);
        Assert.AreEqual(1, CampaignMissionPatrolOrderSystem.EvaluateEstablishBaseOpeningStage(
            1,
            CampaignMissionPatrolOrderSystem.EstablishBaseOpeningCompleteMilliseconds,
            1,
            out queueSweep));
        Assert.AreEqual(0, queueSweep);
        Assert.AreEqual(6, CampaignMissionPatrolOrderSystem.EvaluateEstablishBaseOpeningStage(
            1,
            CampaignMissionPatrolOrderSystem.EstablishBaseOpeningCompleteMilliseconds,
            0,
            out queueSweep));
        Assert.AreEqual(0, queueSweep);
    }

    [MenuItem("Game/Validation/Run M02 Establish Base Guidance Regressions")]
    public static void RunRegressionValidation()
    {
        try
        {
            RunValidation(RunFocusedValidation);
            RunValidation(M02EstablishBasePlacementTests.RunFocusedValidation);
            RunValidation(BuildingPlacementConstructionTransactionTests.RunFocusedValidation);
            RunValidation(BuildingConstructionResourceTransactionSystemHelperTests.RunFocusedValidation);
            RunValidation(M01FirstContactGuidanceTests.RunFocusedValidation);
            RunValidation(M02EstablishBaseBuildCatalogTests.RunFocusedValidation);
            RunValidation(M02EstablishBaseProductionTests.RunFocusedValidation);
            RunValidation(M02EstablishBaseWaveTests.RunFocusedValidation);
            RunValidation(ThreatWarningValidationTests.RunBatchValidation);
            RunValidation(M02EstablishBaseOperationMapTests.RunFocusedValidation);
            RunValidation(AssistantCommandIntentGatewayTests.RunFocusedValidation);
            RunValidation(AssistantCommandIntentSystemTests.RunFocusedValidation);
            RunValidation(MatchHudAssistantUiSystemHelperTests.RunFocusedValidation);
            RunValidation(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            Debug.Log("[M02EstablishBaseGuidanceRegressionValidation] result=Passed suites=14");
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
    public void AcknowledgedBarracksSelectionAdvancesToFootprintPlacement()
    {
        CampaignMissionGuidanceProjectionComponent barracks = ProjectBarracksStep();
        barracks.AcknowledgedGuidanceId = barracks.GuidanceId;
        Assert.IsFalse(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            barracks,
            out _));
        Assert.IsTrue(TryProject(barracks, default, out CampaignMissionGuidanceProjectionComponent placement));
        Assert.AreEqual(CampaignMissionGuidancePromptKind.EstablishBasePlaceBarracks, placement.Prompt);
        Assert.AreNotEqual(barracks.GuidanceId, placement.GuidanceId);
    }

    [Test]
    public void FootprintPlacementTargetsTheCanonicalBuildLot()
    {
        CampaignMissionGuidanceProjectionComponent placement = ProjectPlacementStep();
        Assert.AreEqual(AssistantRecommendationKind.Build, placement.RecommendationKind);
        Assert.AreEqual(AssistantTargetKind.WorldPosition, placement.TargetKind);
        Assert.AreEqual("anchor.ch01.m02.build_lot", placement.TargetId.ToString());
        Assert.AreEqual(CanonicalBuildAnchor, placement.WorldPosition);
        Assert.AreEqual(1, placement.HasWorldPosition);
        Assert.AreEqual("Place the Barracks", placement.Title.ToString());
        Assert.That(placement.Body.ToString(), Does.Contain("green footprint"));
        Assert.That(placement.Body.ToString(), Does.Contain("exact cost"));
        Assert.AreEqual("DO IT", placement.ActionLabel.ToString());
    }

    [Test]
    public void AuthoritativePlacementAdvancesToResourceSpendReview()
    {
        CampaignMissionGuidanceProjectionComponent placement = ProjectPlacementStep();
        CampaignMissionAttemptFactsComponent facts = default;
        facts.RequiredBuildingPlacedCount = 1;

        Assert.IsTrue(TryProject(placement, facts, out CampaignMissionGuidanceProjectionComponent resource));
        Assert.AreEqual(CampaignMissionGuidancePromptKind.EstablishBaseObserveResourceSpend, resource.Prompt);
        Assert.AreEqual(AssistantRecommendationKind.Explain, resource.RecommendationKind);
        Assert.AreEqual(AssistantTargetKind.UiSurface, resource.TargetKind);
        Assert.AreEqual("ui.match.resources", resource.TargetId.ToString());
        Assert.AreEqual("CONTINUE", resource.ActionLabel.ToString());
        Assert.That(resource.Body.ToString(), Does.Contain("Credits and Materials"));
    }

    [Test]
    public void AcknowledgedResourceSpendWaitsForCompletionThenQueuesRifle()
    {
        CampaignMissionAttemptFactsComponent facts = default;
        facts.RequiredBuildingPlacedCount = 1;
        Assert.IsTrue(TryProject(ProjectPlacementStep(), facts, out CampaignMissionGuidanceProjectionComponent resource));
        resource.AcknowledgedGuidanceId = resource.GuidanceId;

        Assert.IsFalse(TryProject(resource, facts, out _),
            "Acknowledging cost review must not expose production before the Barracks completes.");

        facts.RequiredBuildingCompletedCount = 1;
        Assert.IsTrue(TryProject(resource, facts, out CampaignMissionGuidanceProjectionComponent queue));
        Assert.AreEqual(CampaignMissionGuidancePromptKind.EstablishBaseQueueRifle, queue.Prompt);
    }

    [Test]
    public void RifleStepTargetsTheRealProductionControls()
    {
        CampaignMissionGuidanceProjectionComponent queue = ProjectRifleQueueStep();
        Assert.AreEqual(AssistantRecommendationKind.Produce, queue.RecommendationKind);
        Assert.AreEqual(AssistantTargetKind.UiSurface, queue.TargetKind);
        Assert.AreEqual("ui.build_drawer.rifle", queue.TargetId.ToString());
        Assert.AreEqual("Queue a rifle squad", queue.Title.ToString());
        Assert.AreEqual(
            "Open production, select Soldiers, and recruit the required rifle squad.",
            queue.Body.ToString());
        Assert.AreEqual("DO IT", queue.ActionLabel.ToString());
        Assert.AreEqual(1, queue.CanShow);
        Assert.AreEqual(1, queue.CanExecute);

        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            queue,
            out AssistantRecommendationElement recommendation));
        Assert.AreEqual(6, recommendation.TutorialStep);
        Assert.AreEqual(9, recommendation.TutorialStepCount);
    }

    [Test]
    public void AcknowledgedRifleQueueClearsGuidance()
    {
        CampaignMissionGuidanceProjectionComponent queue = ProjectRifleQueueStep();
        queue.AcknowledgedGuidanceId = queue.GuidanceId;
        CampaignMissionAttemptFactsComponent facts = new()
        {
            RequiredBuildingPlacedCount = 1,
            RequiredBuildingCompletedCount = 1
        };

        Assert.IsTrue(TryProject(queue, facts, out CampaignMissionGuidanceProjectionComponent cleared));
        Assert.AreEqual(0, cleared.Active);
        Assert.AreEqual(CampaignMissionGuidancePromptKind.None, cleared.Prompt);
    }

    [Test]
    public void CompletedRifleKeepsAriaHiddenUntilWaveWarning()
    {
        CampaignMissionGuidanceProjectionComponent queue = ProjectRifleQueueStep();
        queue.AcknowledgedGuidanceId = queue.GuidanceId;
        CampaignMissionAttemptFactsComponent facts = DefenseFacts();

        Assert.IsTrue(TryProject(queue, facts, out CampaignMissionGuidanceProjectionComponent hidden));
        Assert.AreEqual(0, hidden.Active);
        Assert.AreEqual(CampaignMissionGuidancePromptKind.None, hidden.Prompt);
        Assert.IsFalse(TryProject(hidden, facts, out _),
            "ARIA must remain hidden between rifle completion and the real delayed-wave warning.");
    }

    [Test]
    public void WaveWarningPreemptsIncompleteRifleProduction()
    {
        CampaignMissionAttemptFactsComponent facts = DefenseFacts();
        facts.RequiredUnitProducedCount = 0;
        facts.DefenseWaveWarningIssued = 1;

        Assert.IsTrue(TryProject(ProjectRifleQueueStep(), facts,
            out CampaignMissionGuidanceProjectionComponent warning));
        Assert.AreEqual(CampaignMissionGuidancePromptKind.EstablishBaseIncomingPatrol, warning.Prompt);
    }

    [Test]
    public void WaveActivationPreemptsIncompleteRifleProduction()
    {
        CampaignMissionAttemptFactsComponent facts = DefenseFacts();
        facts.RequiredUnitProducedCount = 0;
        facts.DefenseWaveWarningIssued = 1;
        facts.DefenseWaveActivated = 1;

        Assert.IsTrue(TryProject(ProjectRifleQueueStep(), facts,
            out CampaignMissionGuidanceProjectionComponent defense));
        Assert.AreEqual(CampaignMissionGuidancePromptKind.EstablishBaseDefendPost, defense.Prompt);
    }

    [Test]
    public void CriticalWarningSurvivesContextualAndMinimalGuidanceModes()
    {
        CampaignMissionAttemptFactsComponent facts = DefenseFacts();
        facts.DefenseWaveWarningIssued = 1;
        foreach (NarrativeGuidanceMode mode in new[]
                 {
                     NarrativeGuidanceMode.Contextual,
                     NarrativeGuidanceMode.Minimal
                 })
        {
            Assert.IsTrue(TryProjectWithGuidance(default, facts, mode,
                out CampaignMissionGuidanceProjectionComponent warning), mode.ToString());
            Assert.AreEqual(CampaignMissionGuidancePromptKind.EstablishBaseIncomingPatrol, warning.Prompt);
            Assert.AreEqual(AssistantMessagePriority.Critical, warning.Priority);
            Assert.AreEqual(0, warning.CanExecute);
        }
    }

    [Test]
    public void WaveWarningTargetsCanonicalDefenseLaneWithoutCombatControl()
    {
        CampaignMissionGuidanceProjectionComponent warning = ProjectWarningStep();
        Assert.AreEqual(CampaignMissionGuidancePromptKind.EstablishBaseIncomingPatrol, warning.Prompt);
        Assert.AreEqual(AssistantRecommendationKind.DefensiveAlert, warning.RecommendationKind);
        Assert.AreEqual(AssistantTargetKind.Objective, warning.TargetKind);
        Assert.AreEqual("anchor.ch01.m02.defense_boundary", warning.TargetId.ToString());
        Assert.AreEqual(float3.zero, warning.WorldPosition);
        Assert.AreEqual(0, warning.HasWorldPosition,
            "The objective resolver must fail closed instead of accepting a fallback world-origin target.");
        Assert.AreEqual(1, warning.CanShow);
        Assert.AreEqual(0, warning.CanExecute);
        Assert.AreEqual("SHOW ME", warning.ActionLabel.ToString());
        Assert.AreEqual(Entity.Null, warning.SourceEntity);
        Assert.AreEqual(Entity.Null, warning.TargetEntity);

        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            warning,
            out AssistantRecommendationElement recommendation));
        Assert.AreEqual(7, recommendation.TutorialStep);
        Assert.AreEqual(9, recommendation.TutorialStepCount);
    }

    [Test]
    public void WaveActivationTransitionsToDefenseWithoutIssuingACommand()
    {
        CampaignMissionGuidanceProjectionComponent warning = ProjectWarningStep();
        CampaignMissionAttemptFactsComponent facts = DefenseFacts();
        facts.DefenseWaveWarningIssued = 1;
        facts.DefenseWaveActivated = 1;

        Assert.IsTrue(TryProject(warning, facts, out CampaignMissionGuidanceProjectionComponent defense));
        Assert.AreEqual(CampaignMissionGuidancePromptKind.EstablishBaseDefendPost, defense.Prompt);
        Assert.AreNotEqual(warning.GuidanceId, defense.GuidanceId);
        Assert.AreEqual(AssistantRecommendationKind.DefensiveAlert, defense.RecommendationKind);
        Assert.AreEqual(AssistantTargetKind.Objective, defense.TargetKind);
        Assert.AreEqual("anchor.ch01.m02.defense_boundary", defense.TargetId.ToString());
        Assert.AreEqual(0, defense.HasWorldPosition);
        Assert.AreEqual(0, defense.CanExecute);
        Assert.AreEqual(Entity.Null, defense.SourceEntity);
        Assert.AreEqual(Entity.Null, defense.TargetEntity);

        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            defense,
            out AssistantRecommendationElement recommendation));
        Assert.AreEqual(8, recommendation.TutorialStep);
        Assert.AreEqual(9, recommendation.TutorialStepCount);
    }

    [Test]
    public void DefeatedWaveClearsDefenseGuidance()
    {
        CampaignMissionGuidanceProjectionComponent defense = ProjectDefenseStep();
        CampaignMissionAttemptFactsComponent facts = DefenseFacts();
        facts.DefenseWaveWarningIssued = 1;
        facts.DefenseWaveActivated = 1;
        facts.HostileDefeatedCount = facts.HostileTotalCount;

        Assert.IsTrue(TryProject(defense, facts, out CampaignMissionGuidanceProjectionComponent cleared));
        Assert.AreEqual(0, cleared.Active);
        Assert.AreEqual(CampaignMissionGuidancePromptKind.None, cleared.Prompt);
    }

    [Test]
    public void WarningAndDefenseTextMatchesEnglishAndPersian()
    {
        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            ProjectWarningStep(),
            out AssistantRecommendationElement warning));
        Assert.IsTrue(UiShellEcsGateway.TryResolveM02GuidancePresentationText(
            in warning,
            FirstLaunchNarrativeLanguage.English,
            out string warningEnglishTitle,
            out string warningEnglishBody,
            out bool warningEnglishRtl));
        Assert.AreEqual("Incoming patrol", warningEnglishTitle);
        Assert.That(warningEnglishBody, Does.Contain("marked defense lane"));
        Assert.IsFalse(warningEnglishRtl);
        Assert.IsTrue(UiShellEcsGateway.TryResolveM02GuidancePresentationText(
            in warning,
            FirstLaunchNarrativeLanguage.Persian,
            out string warningPersianTitle,
            out string warningPersianBody,
            out bool warningPersianRtl));
        Assert.AreEqual("گشت دشمن نزدیک می‌شود", warningPersianTitle);
        Assert.That(warningPersianBody, Does.Contain("مسیر دفاعی"));
        Assert.IsTrue(warningPersianRtl);

        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            ProjectDefenseStep(),
            out AssistantRecommendationElement defense));
        Assert.IsTrue(UiShellEcsGateway.TryResolveM02GuidancePresentationText(
            in defense,
            FirstLaunchNarrativeLanguage.Persian,
            out string defensePersianTitle,
            out string defensePersianBody,
            out bool defensePersianRtl));
        Assert.AreEqual("از پاسگاه پیشرو دفاع کنید", defensePersianTitle);
        Assert.That(defensePersianBody, Does.Contain("تصمیم‌های تاکتیکی"));
        Assert.IsTrue(defensePersianRtl);
    }

    [Test]
    public void BarracksAndRifleGuidanceHaveDistinctTypedTargets()
    {
        CampaignMissionGuidanceProjectionComponent barracks = ProjectBarracksStep();
        CampaignMissionGuidanceProjectionComponent rifle = ProjectRifleQueueStep();
        Assert.AreNotEqual(barracks.Prompt, rifle.Prompt);
        Assert.AreNotEqual(barracks.TargetId, rifle.TargetId);
        Assert.AreEqual(AssistantRecommendationKind.Select, barracks.RecommendationKind);
        Assert.AreEqual(AssistantRecommendationKind.Produce, rifle.RecommendationKind);
    }

    [Test]
    public void BarracksAndRifleGuidanceTextMatchesEnglishAndPersian()
    {
        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            ProjectBarracksStep(),
            out AssistantRecommendationElement barracks));
        Assert.IsTrue(UiShellEcsGateway.TryResolveM02GuidancePresentationText(
            in barracks,
            FirstLaunchNarrativeLanguage.English,
            out string barracksEnglishTitle,
            out string barracksEnglishBody,
            out bool barracksEnglishRtl));
        Assert.AreEqual("Select Barracks", barracksEnglishTitle);
        Assert.AreEqual("Select Barracks from the building catalog.", barracksEnglishBody);
        Assert.IsFalse(barracksEnglishRtl);
        Assert.IsTrue(UiShellEcsGateway.TryResolveM02GuidancePresentationText(
            in barracks,
            FirstLaunchNarrativeLanguage.Persian,
            out string barracksPersianTitle,
            out string barracksPersianBody,
            out bool barracksPersianRtl));
        Assert.AreEqual("پادگان را انتخاب کنید", barracksPersianTitle);
        Assert.AreEqual("پادگان را از فهرست ساختمان‌ها انتخاب کنید.", barracksPersianBody);
        Assert.IsTrue(barracksPersianRtl);

        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            ProjectRifleQueueStep(),
            out AssistantRecommendationElement rifle));
        Assert.IsTrue(UiShellEcsGateway.TryResolveM02GuidancePresentationText(
            in rifle,
            FirstLaunchNarrativeLanguage.Persian,
            out string riflePersianTitle,
            out string riflePersianBody,
            out bool riflePersianRtl));
        Assert.AreEqual("یک گروه تفنگدار در صف بگذارید", riflePersianTitle);
        Assert.That(riflePersianBody, Does.Contain("سربازان"));
        Assert.IsTrue(riflePersianRtl);
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

        CampaignMissionGuidanceProjectionComponent placement = ProjectPlacementStep();
        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            placement,
            out AssistantRecommendationElement placementRecommendation));
        Assert.AreEqual(4, placementRecommendation.TutorialStep);
        Assert.AreEqual(9, placementRecommendation.TutorialStepCount);

        CampaignMissionAttemptFactsComponent facts = default;
        facts.RequiredBuildingPlacedCount = 1;
        Assert.IsTrue(TryProject(placement, facts, out CampaignMissionGuidanceProjectionComponent resource));
        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            resource,
            out AssistantRecommendationElement resourceRecommendation));
        Assert.AreEqual(5, resourceRecommendation.TutorialStep);
        Assert.AreEqual(9, resourceRecommendation.TutorialStepCount);

        CampaignMissionGuidanceProjectionComponent rifle = ProjectRifleQueueStep();
        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            rifle,
            out AssistantRecommendationElement rifleRecommendation));
        Assert.AreEqual(6, rifleRecommendation.TutorialStep);
        Assert.AreEqual(9, rifleRecommendation.TutorialStepCount);

        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            ProjectWarningStep(),
            out AssistantRecommendationElement warningRecommendation));
        Assert.AreEqual(7, warningRecommendation.TutorialStep);
        Assert.AreEqual(9, warningRecommendation.TutorialStepCount);

        Assert.IsTrue(AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(
            ProjectDefenseStep(),
            out AssistantRecommendationElement defenseRecommendation));
        Assert.AreEqual(8, defenseRecommendation.TutorialStep);
        Assert.AreEqual(9, defenseRecommendation.TutorialStepCount);
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
    public void PlacementDoItUsesTheRealPlaceAndConfirmButtons()
    {
        GameObject drawerObject = new("M02 Placement Command Test", typeof(RectTransform));
        GameObject primaryObject = new("Place", typeof(RectTransform), typeof(Image), typeof(Button));
        primaryObject.transform.SetParent(drawerObject.transform, false);
        GameObject placementObject = new("M02 Confirmation Command Test", typeof(RectTransform));
        GameObject confirmObject = new("Confirm", typeof(RectTransform), typeof(Image), typeof(Button));
        confirmObject.transform.SetParent(placementObject.transform, false);
        GameObject prefab = new("Building_Barrack");
        TestBuildingUiCommand command = new();
        try
        {
            BuildDrawerView drawer = drawerObject.AddComponent<BuildDrawerView>();
            SetPrivateField(drawer, "buildButton", primaryObject.GetComponent<Button>());
            BuildDrawerCatalogRuntimeView catalog = drawerObject.AddComponent<BuildDrawerCatalogRuntimeView>();
            catalog.ConfigureForTests(drawer, null, null);
            catalog.BindRuntimeCommands(command, null);
            SetPrivateField(catalog, "_selectedItem", new BuildDrawerCatalogItem(
                BuildDrawerCategory.Buildings,
                prefab,
                "Barracks",
                "BUILDINGS",
                "Forward post barracks",
                90,
                0,
                30f,
                new Vector2Int(20, 10),
                null,
                null,
                null));
            SetPrivateField(catalog, "_hasSelectedItem", true);

            Assert.IsTrue(catalog.TryInvokePrimaryActionFromGuidance());
            Assert.AreEqual(1, command.PlaceRequests);
            Assert.IsTrue(command.HasPendingBuildingPlacement);

            BuildPlacementConfirmationBarView confirmation =
                placementObject.AddComponent<BuildPlacementConfirmationBarView>();
            SetPrivateField(confirmation, "root", placementObject.transform as RectTransform);
            SetPrivateField(confirmation, "confirmButton", confirmObject.GetComponent<Button>());
            confirmation.BindRuntimeCommands(command);

            Assert.IsTrue(confirmation.TryInvokeConfirmFromGuidance());
            Assert.AreEqual(1, command.ConfirmRequests);
            Assert.IsFalse(command.HasPendingBuildingPlacement);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(placementObject);
            UnityEngine.Object.DestroyImmediate(drawerObject);
        }
    }

    [Test]
    public void ResourceSpendContinueUsesTheTypedResourceStrip()
    {
        GameObject resourceObject = new("ResourceStrip", typeof(RectTransform));
        AssistantHighlightPresentationSystemHelper helper = new();
        try
        {
            byte acknowledgedKind = 0;
            helper.Bind(null, uiSurfaceAcknowledged: kind => acknowledgedKind = kind);
            helper.BindResourceStrip(resourceObject.transform as RectTransform);
            helper.BeginPendingShowMe(
                (byte)AssistantRecommendationKind.Explain,
                (byte)AssistantTargetKind.UiSurface);

            Assert.IsTrue(helper.TryExecuteUiSurface(
                (byte)AssistantRecommendationKind.Explain,
                (byte)AssistantTargetKind.UiSurface));
            Assert.AreEqual((byte)AssistantRecommendationKind.Explain, acknowledgedKind);
        }
        finally
        {
            helper.Unbind();
            UnityEngine.Object.DestroyImmediate(resourceObject);
        }
    }

    [Test]
    public void RifleDoItUsesTheRealRecruitButton()
    {
        GameObject drawerObject = new("M02 Rifle Production Guidance", typeof(RectTransform));
        GameObject primaryObject = new("Recruit", typeof(RectTransform), typeof(Image), typeof(Button));
        primaryObject.transform.SetParent(drawerObject.transform, false);
        GameObject rifle = new("Unit_Chr_Soldier_Male_02_Alt_04");
        AssistantHighlightPresentationSystemHelper helper = new();
        TestBuildingUiCommand command = new();
        try
        {
            BuildDrawerView drawer = drawerObject.AddComponent<BuildDrawerView>();
            SetPrivateField(drawer, "buildButton", primaryObject.GetComponent<Button>());
            BuildDrawerCatalogRuntimeView catalog = drawerObject.AddComponent<BuildDrawerCatalogRuntimeView>();
            catalog.ConfigureForTests(drawer, null, null);
            catalog.BindRuntimeCommands(command, null);
            SetPrivateField(catalog, "_activeCategory", BuildDrawerCategory.Soldiers);
            SetPrivateField(catalog, "_selectedItem", new BuildDrawerCatalogItem(
                BuildDrawerCategory.Soldiers,
                rifle,
                "Rifle Squad",
                "SOLDIERS",
                "Required rifle squad",
                20,
                0,
                5f,
                Vector2Int.one,
                null,
                null,
                null));
            SetPrivateField(catalog, "_hasSelectedItem", true);
            byte acknowledgedKind = 0;
            helper.Bind(null, uiSurfaceAcknowledged: kind => acknowledgedKind = kind);
            helper.BindBuildDrawer(drawer);
            helper.BeginPendingShowMe(
                (byte)AssistantRecommendationKind.Produce,
                (byte)AssistantTargetKind.UiSurface);

            Assert.IsTrue(helper.TryExecuteUiSurface(
                (byte)AssistantRecommendationKind.Produce,
                (byte)AssistantTargetKind.UiSurface));
            Assert.AreEqual(1, command.ProductionRequests);
            Assert.AreEqual(0, command.PlaceRequests);
            Assert.AreEqual((byte)AssistantRecommendationKind.Produce, acknowledgedKind);
        }
        finally
        {
            helper.Unbind();
            UnityEngine.Object.DestroyImmediate(rifle);
            UnityEngine.Object.DestroyImmediate(drawerObject);
        }
    }

    [Test]
    public void PlacementBarDisplaysCreditsAndMaterialsCost()
    {
        Assert.AreEqual("40,000 CR / 90 MAT",
            BuildPlacementConfirmationBarView.FormatCostForTests(40000, 90));
    }

    [Test]
    public void M02GuidanceCannotBorrowM01NarrationEvents()
    {
        Assert.IsFalse(MatchHudAssistantUiSystemHelper.CanUseLegacyTutorialNarration(9));
        Assert.IsTrue(MatchHudAssistantUiSystemHelper.CanUseLegacyTutorialNarration(5));
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
        Assert.That(guidance, Does.Contain("anchor.ch01.m02.build_lot"));
        Assert.That(guidance, Does.Contain("ui.match.resources"));
        Assert.That(guidance, Does.Contain("ui.build_drawer.rifle"));
        Assert.That(highlight, Does.Contain("BindBuildButton"));
        Assert.That(highlight, Does.Contain("BindBuildDrawer"));
        Assert.That(highlight, Does.Contain("BindResourceStrip"));
        Assert.That(highlight, Does.Contain("target.onClick.Invoke()"));
        Assert.That(highlightLayout, Does.Contain("GetWorldCorners"));
        Assert.That(buildDrawerGuidance, Does.Contain("UiCampaignGuidanceTargetKind.BuildButton"));
        Assert.That(buildDrawerGuidance, Does.Contain("UiCampaignGuidanceTargetKind.BarracksCatalogItem"));
        Assert.That(buildDrawerGuidance, Does.Contain("TryInvokeRifleProductionFromGuidance"));
        Assert.That(buildDrawerGuidance, Does.Contain("soldiersTab.onClick.Invoke()"));
        Assert.That(buildDrawerGuidance, Does.Contain("itemButton.onClick.Invoke()"));
        Assert.That(readModel, Does.Contain("topRecommendation.TargetKind != AssistantTargetKind.UiSurface"));
        Assert.That(readModel, Does.Contain("topRecommendation.TutorialStepCount != 9"));
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

    private static CampaignMissionGuidanceProjectionComponent ProjectPlacementStep()
    {
        CampaignMissionGuidanceProjectionComponent barracks = ProjectBarracksStep();
        barracks.AcknowledgedGuidanceId = barracks.GuidanceId;
        Assert.IsTrue(TryProject(barracks, default, out CampaignMissionGuidanceProjectionComponent placement));
        return placement;
    }

    private static CampaignMissionGuidanceProjectionComponent ProjectRifleQueueStep()
    {
        CampaignMissionGuidanceProjectionComponent placement = ProjectPlacementStep();
        CampaignMissionAttemptFactsComponent facts = new()
        {
            RequiredBuildingPlacedCount = 1,
            RequiredBuildingCompletedCount = 1
        };
        Assert.IsTrue(TryProject(placement, facts, out CampaignMissionGuidanceProjectionComponent resource));
        resource.AcknowledgedGuidanceId = resource.GuidanceId;
        Assert.IsTrue(TryProject(resource, facts, out CampaignMissionGuidanceProjectionComponent queue));
        return queue;
    }

    private static CampaignMissionGuidanceProjectionComponent ProjectWarningStep()
    {
        CampaignMissionGuidanceProjectionComponent queue = ProjectRifleQueueStep();
        queue.AcknowledgedGuidanceId = queue.GuidanceId;
        CampaignMissionAttemptFactsComponent facts = DefenseFacts();
        facts.DefenseWaveWarningIssued = 1;
        Assert.IsTrue(TryProject(queue, facts, out CampaignMissionGuidanceProjectionComponent warning));
        return warning;
    }

    private static CampaignMissionGuidanceProjectionComponent ProjectDefenseStep()
    {
        CampaignMissionGuidanceProjectionComponent warning = ProjectWarningStep();
        CampaignMissionAttemptFactsComponent facts = DefenseFacts();
        facts.DefenseWaveWarningIssued = 1;
        facts.DefenseWaveActivated = 1;
        Assert.IsTrue(TryProject(warning, facts, out CampaignMissionGuidanceProjectionComponent defense));
        return defense;
    }

    private static CampaignMissionAttemptFactsComponent DefenseFacts() => new()
    {
        RequiredBuildingPlacedCount = 1,
        RequiredBuildingCompletedCount = 1,
        RequiredUnitProducedCount = 1,
        HostileTotalCount = 3
    };

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
            CanonicalBuildAnchor,
            out guidance);

    private static bool TryProjectWithGuidance(
        in CampaignMissionGuidanceProjectionComponent current,
        in CampaignMissionAttemptFactsComponent facts,
        NarrativeGuidanceMode mode,
        out CampaignMissionGuidanceProjectionComponent guidance)
    {
        CampaignMissionRuntimeComponent runtime = Runtime("saga.ch01.m02.establish_base");
        runtime.Guidance = mode;
        return CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
            current,
            runtime,
            facts,
            Settings(),
            Entity.Null,
            Entity.Null,
            default,
            default,
            CanonicalBuildAnchor,
            out guidance);
    }

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

    private sealed class TestBuildingUiCommand : IBuildingUiCommand
    {
        public int CurrentDollars => 100000;
        public bool HasPendingBuildingPlacement { get; private set; }
        public bool CanConfirmBuildingPlacement => HasPendingBuildingPlacement;
        public string PlacementStatusText => "Barracks: Valid placement";
        public int ActivePlacementCost => 90;
        public int ActivePlacementCreditsCost => 40000;
        public float ActivePlacementDurationSeconds => 30f;
        public int MaxQueuedUnitProductions => 25;
        public int PlaceRequests { get; private set; }
        public int ConfirmRequests { get; private set; }
        public int ProductionRequests { get; private set; }

        public BuildingUiCommandFailure GetCampRequestFailure(
            GameObject prefab,
            int materialsCost,
            out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            return BuildingUiCommandFailure.None;
        }

        public BuildingUiCommandFailure TryRequestCampItem(
            GameObject prefab,
            int materialsCost,
            out string requiredBuildingDisplayName,
            bool focusProducerOnSuccess)
        {
            requiredBuildingDisplayName = string.Empty;
            if (prefab != null && prefab.name.StartsWith("Building_", StringComparison.Ordinal))
            {
                PlaceRequests++;
                HasPendingBuildingPlacement = true;
            }
            else
            {
                ProductionRequests++;
            }
            return BuildingUiCommandFailure.None;
        }

        public bool CancelProduction(int buildingId, int pendingProductionIndex) => false;

        public bool ConfirmBuildingPlacement()
        {
            ConfirmRequests++;
            HasPendingBuildingPlacement = false;
            return true;
        }

        public void CancelBuildingPlacement() => HasPendingBuildingPlacement = false;

        public bool RotateBuildingPlacement() => HasPendingBuildingPlacement;
    }
}
#endif
