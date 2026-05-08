using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class M01AssistantRuntimeTests
{
    [Test]
    public void M01AssistantRecommendationProvider_ObjectivesIntroStartsWhenMatchObjectiveVisible()
    {
        var provider = new M01AssistantRecommendationProvider();
        var session = new TutorialSessionState();

        AssistantRecommendation recommendation = provider.Evaluate(CreateM01Context(objectivePanelVisible: true), session);

        Assert.AreEqual(M01AssistantIds.ObjectivesRecommendationId, recommendation.RecommendationId);
        Assert.AreEqual(M01AssistantIds.ObjectivesStepId, recommendation.StepId);
        Assert.AreEqual(AssistantIntentKind.FocusUiElement, recommendation.ShowMeIntent.IntentKind);
        Assert.AreEqual(M01AssistantIds.ObjectivePanelId, recommendation.ShowMeIntent.TargetId);
        Assert.IsTrue(recommendation.CanShow);
        Assert.IsFalse(recommendation.CanExecute);
    }

    [Test]
    public void M01AssistantRecommendationProvider_SelectSquadWhenNoSelection()
    {
        var provider = new M01AssistantRecommendationProvider();
        var session = new TutorialSessionState();

        AssistantRecommendation recommendation = provider.Evaluate(CreateM01Context(commandSquadSpawned: true), session);

        Assert.AreEqual(M01AssistantIds.SelectSquadRecommendationId, recommendation.RecommendationId);
        Assert.AreEqual(M01AssistantIds.SelectSquadStepId, recommendation.StepId);
        Assert.AreEqual(AssistantIntentKind.SelectRuntimeEntity, recommendation.DoItIntent.IntentKind);
        Assert.AreEqual(M01AssistantIds.PlayerSquadEntityId, recommendation.DoItIntent.TargetId);
        Assert.IsFalse(recommendation.CanExecute, "Gameplay typed hooks are not assumed ready by default.");
    }

    [Test]
    public void M01AssistantRecommendationProvider_MoveAfterSelection()
    {
        var provider = new M01AssistantRecommendationProvider();
        var session = new TutorialSessionState();

        AssistantRecommendation recommendation = provider.Evaluate(CreateM01Context(
            commandSquadSpawned: true,
            commandSquadSelected: true,
            moveTargetAvailable: true,
            typedHooksAvailable: true), session);

        Assert.AreEqual(M01AssistantIds.MoveToCoverRecommendationId, recommendation.RecommendationId);
        Assert.AreEqual(M01AssistantIds.MoveStepId, recommendation.StepId);
        Assert.AreEqual(AssistantIntentKind.PreviewPathToAnchor, recommendation.ShowMeIntent.IntentKind);
        Assert.AreEqual(AssistantIntentKind.MoveSelectedUnits, recommendation.DoItIntent.IntentKind);
        Assert.AreEqual(M01AssistantIds.MoveTargetAnchorId, recommendation.DoItIntent.TargetId);
        Assert.IsTrue(recommendation.CanExecute);
        Assert.IsTrue(session.IsStepCompleted(M01AssistantIds.SelectSquadStepId));
    }

    [Test]
    public void M01AssistantRecommendationProvider_AttackWhenPatrolVisible()
    {
        var provider = new M01AssistantRecommendationProvider();
        var session = new TutorialSessionState();

        AssistantRecommendation recommendation = provider.Evaluate(CreateM01Context(
            commandSquadSpawned: true,
            commandSquadSelected: true,
            moveCommandAccepted: true,
            enemyPatrolSpawned: true,
            enemyPatrolVisible: true,
            typedHooksAvailable: true), session);

        Assert.AreEqual(M01AssistantIds.AttackPatrolRecommendationId, recommendation.RecommendationId);
        Assert.AreEqual(M01AssistantIds.AttackStepId, recommendation.StepId);
        Assert.AreEqual(AssistantIntentKind.AttackTarget, recommendation.DoItIntent.IntentKind);
        Assert.AreEqual(M01AssistantIds.EnemyPatrolEntityId, recommendation.DoItIntent.TargetId);
        Assert.IsTrue(recommendation.CanExecute);
        Assert.IsTrue(session.IsStepCompleted(M01AssistantIds.MoveStepId));
    }

    [Test]
    public void M01AssistantRecommendationProvider_InvalidNoSelectionRecoversToSelect()
    {
        var provider = new M01AssistantRecommendationProvider();
        var session = new TutorialSessionState();

        AssistantContext context = CreateM01Context(commandSquadSpawned: true);
        context.LastCommandResultAccepted = false;
        context.LastCommandReasonCode = TacticalCommandReasonCode.NoSelection;

        AssistantRecommendation recommendation = provider.Evaluate(context, session);

        Assert.AreEqual(M01AssistantIds.InvalidCommandRecommendationId, recommendation.RecommendationId);
        Assert.AreEqual(M01AssistantIds.SelectSquadStepId, recommendation.StepId);
        Assert.AreEqual(TacticalCommandReasonCode.NoSelection, recommendation.BlockingReasonCode);
        Assert.AreEqual(AssistantIntentKind.SelectRuntimeEntity, recommendation.DoItIntent.IntentKind);
        Assert.IsFalse(session.IsStepCompleted(M01AssistantIds.SelectSquadStepId));
    }

    [Test]
    public void M01AssistantRecommendationProvider_BuildRejectedExplainsMissionLock()
    {
        var provider = new M01AssistantRecommendationProvider();
        var session = new TutorialSessionState();

        AssistantContext context = CreateM01Context(commandSquadSpawned: true, commandSquadSelected: true);
        context.LastCommandResultAccepted = false;
        context.LastCommandReasonCode = TacticalCommandReasonCode.MissionDoesNotAllowBuild;

        AssistantRecommendation recommendation = provider.Evaluate(context, session);

        Assert.AreEqual(M01AssistantIds.InvalidCommandRecommendationId, recommendation.RecommendationId);
        Assert.AreEqual(TacticalCommandReasonCode.MissionDoesNotAllowBuild, recommendation.BlockingReasonCode);
        Assert.AreEqual(AssistantIntentKind.FocusObjectiveAnchor, recommendation.ShowMeIntent.IntentKind);
        Assert.AreEqual(M01AssistantIds.ObjectiveAnchorId, recommendation.ShowMeIntent.TargetId);
        Assert.IsFalse(recommendation.CanExecute);
        StringAssert.Contains("Building unlocks", recommendation.Body);
    }

    [Test]
    public void M01AssistantRuntime_DoesNotReplayCompletedSteps()
    {
        var service = new WarlineCaptureAssistantService();

        service.CompleteStep(M01AssistantIds.SelectSquadStepId);
        AssistantRecommendation recommendation = service.Evaluate(CreateM01Context(commandSquadSpawned: true));

        Assert.IsFalse(recommendation.HasRecommendation);
    }

    [Test]
    public void WarlineCaptureAssistantService_CreatesPanelPresentationWithoutExecutingGameplay()
    {
        var service = new WarlineCaptureAssistantService();

        AssistantRecommendation recommendation = service.Evaluate(CreateM01Context(
            commandSquadSpawned: true,
            commandSquadSelected: true,
            moveTargetAvailable: true));
        AssistantPanelPresentationData presentation = service.CreatePresentationData();

        Assert.AreEqual(recommendation.RecommendationId, presentation.RecommendationId);
        Assert.AreEqual(recommendation.Title, presentation.Title);
        Assert.IsTrue(presentation.CanShow);
        Assert.IsFalse(presentation.CanExecute, "The service exposes a Do It intent but does not execute gameplay or assume hooks are ready.");
        Assert.AreEqual(AssistantIntentKind.MoveSelectedUnits, recommendation.DoItIntent.IntentKind);
    }

    [Test]
    public void CommandIntentExecutor_RejectsCoordinateTargetsByModel()
    {
        string source = File.ReadAllText(ResolveRepoFilePath("Assets/Game/Scripts/Tutorial/Recommendations/AssistantRuntimeModels.cs"));

        StringAssert.DoesNotContain("Screen.", source);
        StringAssert.DoesNotContain("mousePosition", source);
        StringAssert.DoesNotContain("Vector2", source);
        StringAssert.DoesNotContain("Vector3", source);

        AssistantRecommendation recommendation = new M01AssistantRecommendationProvider().Evaluate(
            CreateM01Context(commandSquadSpawned: true, commandSquadSelected: true, moveTargetAvailable: true),
            new TutorialSessionState());

        Assert.AreEqual(AssistantTargetType.TacticalAnchor, recommendation.DoItIntent.TargetType);
        Assert.AreEqual(M01AssistantIds.MoveTargetAnchorId, recommendation.DoItIntent.TargetId);
    }

    private static AssistantContext CreateM01Context(
        bool objectivePanelVisible = false,
        bool commandSquadSpawned = false,
        bool commandSquadSelected = false,
        bool moveTargetAvailable = false,
        bool moveCommandAccepted = false,
        bool enemyPatrolSpawned = false,
        bool enemyPatrolVisible = false,
        bool enemyPatrolDestroyed = false,
        bool resultPopupVisible = false,
        bool typedHooksAvailable = false)
    {
        return new AssistantContext
        {
            MissionId = M01AssistantIds.MissionId,
            ScenarioSetupId = M01AssistantIds.ScenarioSetupId,
            LevelId = M01AssistantIds.LevelId,
            IsoMapId = M01AssistantIds.IsoMapId,
            IsMatchOverlayActive = true,
            ObjectivePanelVisible = objectivePanelVisible,
            CommandSquadSpawned = commandSquadSpawned,
            CommandSquadSelected = commandSquadSelected,
            MoveTargetAvailable = moveTargetAvailable,
            MoveCommandAccepted = moveCommandAccepted,
            EnemyPatrolSpawned = enemyPatrolSpawned,
            EnemyPatrolVisible = enemyPatrolVisible,
            EnemyPatrolDestroyed = enemyPatrolDestroyed,
            ResultPopupVisible = resultPopupVisible,
            TypedCommandHooksAvailable = typedHooksAvailable
        };
    }

    private static string ResolveRepoFilePath(string relativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        return string.IsNullOrEmpty(projectRoot)
            ? Path.GetFullPath(relativePath)
            : Path.Combine(projectRoot, relativePath);
    }
}
