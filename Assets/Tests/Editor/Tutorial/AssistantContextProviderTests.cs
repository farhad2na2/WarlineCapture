using System.IO;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class AssistantContextProviderTests
{
    private bool _previousPlayRequested;
    private World _previousWorld;
    private World _world;
    private GameObject _viewObject;
    private GameObject _objectivePanelObject;
    private BattleHudRuntimeFeedbackView _feedbackView;
    private Chapter01M01PlayableRuntime.RuntimeState _runtimeState;

    [SetUp]
    public void SetUp()
    {
        GameRuntimeStats.Reset();
        _previousPlayRequested = InitialUnitsRuntimeState.PlayRequested;
        InitialUnitsRuntimeState.PlayRequested = true;
        WarlineCaptureMissionSession.BeginMission(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureRoute.SagaMap);
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("AssistantContextProviderTests");
        World.DefaultGameObjectInjectionWorld = _world;
        Assert.IsTrue(Chapter01M01PlayableRuntime.TryInitializeActiveMission(_world, out _runtimeState));
        _viewObject = new GameObject("AssistantContextProviderBattleHudView");
        _feedbackView = _viewObject.AddComponent<BattleHudRuntimeFeedbackView>();
        _objectivePanelObject = new GameObject("AssistantContextProviderObjectivePanel");
        _objectivePanelObject.AddComponent<MatchObjectivePanelSystem>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_objectivePanelObject != null)
            Object.DestroyImmediate(_objectivePanelObject);
        if (_viewObject != null)
            Object.DestroyImmediate(_viewObject);
        if (_world != null && _world.IsCreated)
            _world.Dispose();
        World.DefaultGameObjectInjectionWorld = _previousWorld;
        WarlineCaptureMissionSession.Clear();
        InitialUnitsRuntimeState.PlayRequested = _previousPlayRequested;
        GameRuntimeStats.Reset();
    }

    [Test]
    public void BuildContext_SourcesMissionIdsRuntimeReadinessAndObjectiveVisibility()
    {
        var session = new TutorialSessionState();
        AssistantContext context = CreateProvider().BuildContext(session);

        Assert.AreEqual(M01AssistantIds.MissionId, context.MissionId);
        Assert.AreEqual(M01AssistantIds.ScenarioSetupId, context.ScenarioSetupId);
        Assert.AreEqual(M01AssistantIds.LevelId, context.LevelId);
        Assert.AreEqual(M01AssistantIds.IsoMapId, context.IsoMapId);
        Assert.AreEqual(WarlineCaptureRoute.Match.ToString(), context.ActiveRoute);
        Assert.IsTrue(context.IsMatchOverlayActive);
        Assert.IsTrue(context.ObjectivePanelVisible);
        Assert.IsTrue(context.CommandSquadSpawned);
        Assert.IsTrue(context.CommandSquadAlive);
        Assert.IsFalse(context.CommandSquadSelected);
        Assert.IsTrue(context.MoveTargetAvailable);
        Assert.IsTrue(context.EnemyPatrolSpawned);
        Assert.IsTrue(context.EnemyPatrolVisible);
        Assert.IsFalse(context.EnemyPatrolDestroyed);
        Assert.IsTrue(context.TypedCommandHooksAvailable);
    }

    [Test]
    public void BuildContext_SourcesSelectionAndMoveAcceptanceFromRuntimeState()
    {
        var session = new TutorialSessionState();
        Assert.IsTrue(M01AssistantCommandRuntime.TrySelectRuntimeEntity(
            _world,
            M01AssistantIds.PlayerSquadEntityId).Accepted);
        Assert.IsTrue(M01AssistantCommandRuntime.TryIssueMoveToAnchor(
            _world,
            M01AssistantIds.MoveTargetAnchorId).Accepted);

        AssistantContext context = CreateProvider().BuildContext(session);

        Assert.IsTrue(context.CommandSquadSelected);
        Assert.IsTrue(context.MoveCommandAccepted);
        Assert.IsTrue(session.M01MoveCommandAccepted);
        Assert.IsTrue(session.IsStepCompleted(M01AssistantIds.MoveStepId));
    }

    [Test]
    public void BuildContext_SourcesAttackAcceptanceAndEnemyDestroyedFromRuntimeState()
    {
        var session = new TutorialSessionState();
        Assert.IsTrue(M01AssistantCommandRuntime.TrySelectRuntimeEntity(
            _world,
            M01AssistantIds.PlayerSquadEntityId).Accepted);
        Assert.IsTrue(M01AssistantCommandRuntime.TryIssueAttackTarget(
            _world,
            M01AssistantIds.EnemyPatrolEntityId).Accepted);

        AssistantContext attackContext = CreateProvider().BuildContext(session);
        Assert.IsTrue(attackContext.AttackCommandAccepted);
        Assert.IsTrue(session.M01AttackCommandAccepted);

        _world.EntityManager.SetComponentData(_runtimeState.EnemyPatrol, new UnitHealth { Current = 0, Max = 100 });

        AssistantContext destroyedContext = CreateProvider().BuildContext(session);
        Assert.IsTrue(destroyedContext.EnemyPatrolDestroyed);
        Assert.IsFalse(destroyedContext.EnemyPatrolVisible);
    }

    [Test]
    public void BuildContext_SourcesLatestRejectedCommandResultFromRuntimeFeedbackSystem()
    {
        var session = new TutorialSessionState();
        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(
            _feedbackView,
            TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));

        AssistantContext context = CreateProvider().BuildContext(session);

        Assert.IsFalse(context.LastCommandResultAccepted);
        Assert.AreEqual(TacticalCommandReasonCode.NoSelection, context.LastCommandReasonCode);
        Assert.AreEqual(TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.NoSelection), context.LastCommandReasonText);
        Assert.AreEqual(TacticalCommandReasonCode.NoSelection, session.LastRejectedReasonCode);
        Assert.AreEqual(M01AssistantIds.SelectSquadStepId, session.LastRejectedAtStepId);
    }

    [Test]
    public void BuildContext_TypedCommandReadinessFollowsDeadCommandSquad()
    {
        _world.EntityManager.SetComponentData(_runtimeState.PlayerSquad, new UnitHealth { Current = 0, Max = 100 });

        AssistantContext context = CreateProvider().BuildContext(new TutorialSessionState());

        Assert.IsTrue(context.CommandSquadSpawned);
        Assert.IsFalse(context.CommandSquadAlive);
        Assert.IsFalse(context.TypedCommandHooksAvailable);
    }

    [Test]
    public void BuildContext_FeedsLiveContextIntoRecommendationProvider()
    {
        var session = new TutorialSessionState();
        session.CompleteStep(M01AssistantIds.ObjectivesStepId);
        var service = new WarlineCaptureAssistantService(new M01AssistantRecommendationProvider(), session);

        AssistantRecommendation select = service.Evaluate(CreateProvider().BuildContext(session));

        Assert.AreEqual(M01AssistantIds.SelectSquadRecommendationId, select.RecommendationId);
        Assert.IsTrue(select.CanExecute);

        Assert.IsTrue(M01AssistantCommandRuntime.TrySelectRuntimeEntity(
            _world,
            M01AssistantIds.PlayerSquadEntityId).Accepted);
        AssistantRecommendation move = service.Evaluate(CreateProvider().BuildContext(session));

        Assert.AreEqual(M01AssistantIds.MoveToCoverRecommendationId, move.RecommendationId);
        Assert.IsTrue(move.CanExecute);
    }

    [Test]
    public void AssistantContextProvider_DoesNotUseUiHierarchyScreenCoordinatesOrHudText()
    {
        string source = File.ReadAllText(ResolveRepoFilePath("Assets/Game/Scripts/Tutorial/Assistant/AssistantContextProvider.cs"));

        StringAssert.DoesNotContain(".Find(", source);
        StringAssert.DoesNotContain("GetComponentInChildren", source);
        StringAssert.DoesNotContain("Screen.", source);
        StringAssert.DoesNotContain("mousePosition", source);
        StringAssert.DoesNotContain("anchoredPosition", source);
        StringAssert.DoesNotContain("NameText", source);
        StringAssert.DoesNotContain("SelectedEntityPanel", source);
        StringAssert.DoesNotContain("Button", source);
    }

    private AssistantContextProvider CreateProvider()
    {
        return new AssistantContextProvider(
            _world,
            _feedbackView,
            router: null,
            resultFlow: null,
            objectivePanel: _objectivePanelObject.GetComponent<MatchObjectivePanelSystem>());
    }

    private static string ResolveRepoFilePath(string relativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        return string.IsNullOrEmpty(projectRoot)
            ? Path.GetFullPath(relativePath)
            : Path.Combine(projectRoot, relativePath);
    }
}
