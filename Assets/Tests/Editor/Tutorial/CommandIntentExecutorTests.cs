using System.IO;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class CommandIntentExecutorTests
{
    private const string DefinitionPath = "Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset";

    private World _previousWorld;
    private World _world;
    private GameObject _loaderRoot;
    private GameObject _cameraObject;
    private TacticalMapRuntimeLoader _loader;
    private RTSSelectionSystem _selection;
    private Chapter01M01PlayableRuntime.RuntimeState _runtimeState;

    [SetUp]
    public void SetUp()
    {
        GameRuntimeStats.Reset();
        WarlineCaptureMissionSession.BeginMission(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureRoute.SagaMap);
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("CommandIntentExecutorTests");
        World.DefaultGameObjectInjectionWorld = _world;
        _selection = new RTSSelectionSystem();
        _loader = CreateLoadedRuntimeLoader();
        Assert.IsTrue(Chapter01M01PlayableRuntime.TryInitializeActiveMission(_world, _loader, out _runtimeState));
    }

    [TearDown]
    public void TearDown()
    {
        _selection?.Dispose();
        if (_loaderRoot != null)
            Object.DestroyImmediate(_loaderRoot);
        if (_cameraObject != null)
            Object.DestroyImmediate(_cameraObject);
        if (_world != null && _world.IsCreated)
            _world.Dispose();
        World.DefaultGameObjectInjectionWorld = _previousWorld;
        WarlineCaptureMissionSession.Clear();
        GameRuntimeStats.Reset();
    }

    [Test]
    public void ExecuteDoIt_SelectRuntimeEntity_SelectsM01CommandSquad()
    {
        var session = new TutorialSessionState();
        var executor = new CommandIntentExecutor(session, _world, _loader, _selection);

        TacticalCommandResult result = executor.ExecuteDoItIntent(CreateSelectIntent());

        Assert.IsTrue(result.Accepted);
        Assert.IsTrue(_world.EntityManager.HasComponent<SelectedUnitTag>(_runtimeState.PlayerSquad));
        Assert.IsTrue(session.IsStepCompleted(M01AssistantIds.SelectSquadStepId));
    }

    [Test]
    public void ExecuteDoIt_MoveToCover_IssuesNormalMoveOrderAndCompletesSessionStep()
    {
        var session = new TutorialSessionState();
        var executor = new CommandIntentExecutor(session, _world, _loader, _selection);
        Assert.IsTrue(executor.ExecuteDoItIntent(CreateSelectIntent()).Accepted);
        Assert.IsTrue(_loader.TryGetAnchorCell(M01AssistantIds.MoveTargetAnchorId, out Vector2Int coverCell));

        TacticalCommandResult result = executor.ExecuteDoItIntent(CreateMoveIntent());

        EntityManager em = _world.EntityManager;
        Assert.IsTrue(result.Accepted);
        Assert.AreEqual(new int2(coverCell.x, coverCell.y), em.GetComponentData<UnitTarget>(_runtimeState.PlayerSquad).Cell);
        Assert.IsTrue(session.IsStepCompleted(M01AssistantIds.MoveStepId));
    }

    [Test]
    public void ExecuteDoIt_AttackPatrol_IssuesNormalAttackOrderAndCompletesSessionStep()
    {
        var session = new TutorialSessionState();
        var executor = new CommandIntentExecutor(session, _world, _loader, _selection);
        Assert.IsTrue(executor.ExecuteDoItIntent(CreateSelectIntent()).Accepted);

        TacticalCommandResult result = executor.ExecuteDoItIntent(CreateAttackIntent());

        EntityManager em = _world.EntityManager;
        Assert.IsTrue(result.Accepted);
        Assert.AreEqual(_runtimeState.EnemyPatrol, em.GetComponentData<EngageTarget>(_runtimeState.PlayerSquad).Target);
        Assert.IsTrue(session.IsStepCompleted(M01AssistantIds.AttackStepId));
    }

    [Test]
    public void ExecuteDoIt_RejectsMissingRuntimeEntity()
    {
        var session = new TutorialSessionState();
        var executor = new CommandIntentExecutor(session, _world, _loader, _selection);

        TacticalCommandResult result = executor.ExecuteDoItIntent(new AssistantIntent(
            "do.select_missing",
            AssistantIntentKind.SelectRuntimeEntity,
            M01AssistantIds.MissionId,
            M01AssistantIds.SelectSquadStepId,
            AssistantTargetType.RuntimeEntity,
            "unit.player.missing",
            TacticalCommandMode.None,
            canExecuteGameplay: true,
            requiresSelectedEntity: false,
            requiresVisibleTarget: false,
            "one selection intent"));

        AssertRejected(result, TacticalCommandReasonCode.TargetNotAttackable);
        Assert.IsFalse(session.IsStepCompleted(M01AssistantIds.SelectSquadStepId));
    }

    [Test]
    public void ExecuteDoIt_MoveToCover_RejectsNoSelection()
    {
        var session = new TutorialSessionState();
        var executor = new CommandIntentExecutor(session, _world, _loader, _selection);

        TacticalCommandResult result = executor.ExecuteDoItIntent(CreateMoveIntent());

        AssertRejected(result, TacticalCommandReasonCode.NoSelection);
        Assert.AreEqual(TacticalCommandReasonCode.NoSelection, session.LastRejectedReasonCode);
    }

    [Test]
    public void ExecuteDoIt_MoveToCover_RejectsMissingAnchor()
    {
        var executor = new CommandIntentExecutor(new TutorialSessionState(), _world, _loader, _selection);
        Assert.IsTrue(executor.ExecuteDoItIntent(CreateSelectIntent()).Accepted);

        TacticalCommandResult result = executor.ExecuteDoItIntent(new AssistantIntent(
            "do.move_missing",
            AssistantIntentKind.MoveSelectedUnits,
            M01AssistantIds.MissionId,
            M01AssistantIds.MoveStepId,
            AssistantTargetType.TacticalAnchor,
            "tutorial.move_target.missing",
            TacticalCommandMode.Move,
            canExecuteGameplay: true,
            requiresSelectedEntity: true,
            requiresVisibleTarget: false,
            "one move intent"));

        AssertRejected(result, TacticalCommandReasonCode.TargetNotAttackable);
    }

    [Test]
    public void ExecuteDoIt_MoveToCover_RejectsDeadSquad()
    {
        var executor = new CommandIntentExecutor(new TutorialSessionState(), _world, _loader, _selection);
        EntityManager em = _world.EntityManager;
        em.AddComponent<SelectedUnitTag>(_runtimeState.PlayerSquad);
        em.SetComponentData(_runtimeState.PlayerSquad, new UnitHealth { Current = 0, Max = 100 });

        TacticalCommandResult result = executor.ExecuteDoItIntent(CreateMoveIntent());

        AssertRejected(result, TacticalCommandReasonCode.TargetNotAttackable);
    }

    [Test]
    public void ExecuteDoIt_AttackPatrol_RejectsDeadPatrol()
    {
        var executor = new CommandIntentExecutor(new TutorialSessionState(), _world, _loader, _selection);
        Assert.IsTrue(executor.ExecuteDoItIntent(CreateSelectIntent()).Accepted);
        _world.EntityManager.SetComponentData(_runtimeState.EnemyPatrol, new UnitHealth { Current = 0, Max = 100 });

        TacticalCommandResult result = executor.ExecuteDoItIntent(CreateAttackIntent());

        AssertRejected(result, TacticalCommandReasonCode.TargetNotAttackable);
    }

    [Test]
    public void ExecuteDoIt_DoesNotExecuteShowMeFocusIntent()
    {
        var session = new TutorialSessionState();
        var executor = new CommandIntentExecutor(session, _world, _loader, _selection);

        TacticalCommandResult result = executor.ExecuteDoItIntent(new AssistantIntent(
            "show.objective_panel",
            AssistantIntentKind.FocusUiElement,
            M01AssistantIds.MissionId,
            M01AssistantIds.ObjectivesStepId,
            AssistantTargetType.UiElement,
            M01AssistantIds.ObjectivePanelId,
            TacticalCommandMode.None,
            canExecuteGameplay: false,
            requiresSelectedEntity: false,
            requiresVisibleTarget: false,
            "preview"));

        AssertRejected(result, TacticalCommandReasonCode.TargetNotAttackable);
        Assert.AreEqual(TacticalCommandReasonCode.TargetNotAttackable, session.LastRejectedReasonCode);
        Assert.IsFalse(_world.EntityManager.HasComponent<SelectedUnitTag>(_runtimeState.PlayerSquad));
    }

    [Test]
    public void ExecuteDoIt_RejectsNonM01MissionIntent()
    {
        var session = new TutorialSessionState();
        var executor = new CommandIntentExecutor(session, _world, _loader, _selection);

        TacticalCommandResult result = executor.ExecuteDoItIntent(new AssistantIntent(
            "do.select_other_mission",
            AssistantIntentKind.SelectRuntimeEntity,
            "saga.ch99.other",
            M01AssistantIds.SelectSquadStepId,
            AssistantTargetType.RuntimeEntity,
            M01AssistantIds.PlayerSquadEntityId,
            TacticalCommandMode.None,
            canExecuteGameplay: true,
            requiresSelectedEntity: false,
            requiresVisibleTarget: false,
            "one selection intent"));

        AssertRejected(result, TacticalCommandReasonCode.TargetNotAttackable);
        Assert.AreEqual(TacticalCommandReasonCode.TargetNotAttackable, session.LastRejectedReasonCode);
        Assert.IsFalse(_world.EntityManager.HasComponent<SelectedUnitTag>(_runtimeState.PlayerSquad));
    }

    [Test]
    public void StopAssistantControl_ClearsPreviewAndTakeoverWithoutGameplayCommand()
    {
        var session = new TutorialSessionState();
        session.SetActivePreview("show.move_to_cover");
        session.SetActiveTakeover("do.move_to_cover");
        var executor = new CommandIntentExecutor(session, _world, _loader, _selection);

        TacticalCommandResult result = executor.StopAssistantControl();

        Assert.IsTrue(result.Accepted);
        Assert.IsEmpty(session.ActivePreviewIntentId);
        Assert.IsEmpty(session.ActiveTakeoverIntentId);
        Assert.IsFalse(_world.EntityManager.HasComponent<SelectedUnitTag>(_runtimeState.PlayerSquad));
    }

    [Test]
    public void GetBuildCommandResult_RejectsM01BuildWithMissionReason()
    {
        var session = new TutorialSessionState();
        var executor = new CommandIntentExecutor(session, _world, _loader, _selection);

        TacticalCommandResult result = executor.GetBuildCommandResult();

        AssertRejected(result, TacticalCommandReasonCode.MissionDoesNotAllowBuild);
        Assert.AreEqual(WarlineCaptureMissionRules.M01BuildDisabledMessage, result.Message);
        Assert.AreEqual(TacticalCommandReasonCode.MissionDoesNotAllowBuild, session.LastRejectedReasonCode);
    }

    [Test]
    public void WarlineCaptureAssistantService_ExecutesCurrentDoItThroughExecutor()
    {
        var session = new TutorialSessionState();
        var service = new WarlineCaptureAssistantService(new M01AssistantRecommendationProvider(), session);
        service.Evaluate(CreateM01Context(commandSquadSpawned: true, typedHooksAvailable: true));

        TacticalCommandResult result = service.ExecuteCurrentDoIt(new CommandIntentExecutor(session, _world, _loader, _selection));

        Assert.IsTrue(result.Accepted);
        Assert.IsTrue(_world.EntityManager.HasComponent<SelectedUnitTag>(_runtimeState.PlayerSquad));
        Assert.IsTrue(session.IsStepCompleted(M01AssistantIds.SelectSquadStepId));
    }

    [Test]
    public void CommandIntentExecutor_DoesNotUseUiHierarchyScreenCoordinatesOrHudText()
    {
        string source = File.ReadAllText(ResolveRepoFilePath("Assets/Game/Scripts/Tutorial/Assistant/CommandIntentExecutor.cs"));

        StringAssert.DoesNotContain(".Find(", source);
        StringAssert.DoesNotContain("FindObject", source);
        StringAssert.DoesNotContain("GetComponentInChildren", source);
        StringAssert.DoesNotContain("Screen.", source);
        StringAssert.DoesNotContain("mousePosition", source);
        StringAssert.DoesNotContain("anchoredPosition", source);
        StringAssert.DoesNotContain("NameText", source);
        StringAssert.DoesNotContain("SelectedEntityPanel", source);
        StringAssert.DoesNotContain("Button", source);
    }

    private TacticalMapRuntimeLoader CreateLoadedRuntimeLoader()
    {
        TacticalMapDefinition definition = AssetDatabase.LoadAssetAtPath<TacticalMapDefinition>(DefinitionPath);
        Assert.NotNull(definition);

        _loaderRoot = new GameObject("CommandIntentExecutorRuntimeLoaderTestRoot");
        _cameraObject = new GameObject("CommandIntentExecutorRuntimeLoaderTestCamera");
        Camera camera = _cameraObject.AddComponent<Camera>();
        TacticalMapRuntimeLoader loader = _loaderRoot.AddComponent<TacticalMapRuntimeLoader>();
        loader.Configure(definition, null, camera, TacticalMapRuntimePlane.GameplayXZ);
        loader.Load();
        return loader;
    }

    private static AssistantIntent CreateSelectIntent()
    {
        return new AssistantIntent(
            "do.select_squad",
            AssistantIntentKind.SelectRuntimeEntity,
            M01AssistantIds.MissionId,
            M01AssistantIds.SelectSquadStepId,
            AssistantTargetType.RuntimeEntity,
            M01AssistantIds.PlayerSquadEntityId,
            TacticalCommandMode.None,
            canExecuteGameplay: true,
            requiresSelectedEntity: false,
            requiresVisibleTarget: false,
            "one selection intent");
    }

    private static AssistantIntent CreateMoveIntent()
    {
        return new AssistantIntent(
            "do.move_to_cover",
            AssistantIntentKind.MoveSelectedUnits,
            M01AssistantIds.MissionId,
            M01AssistantIds.MoveStepId,
            AssistantTargetType.TacticalAnchor,
            M01AssistantIds.MoveTargetAnchorId,
            TacticalCommandMode.Move,
            canExecuteGameplay: true,
            requiresSelectedEntity: true,
            requiresVisibleTarget: false,
            "one move intent");
    }

    private static AssistantIntent CreateAttackIntent()
    {
        return new AssistantIntent(
            "do.attack_patrol",
            AssistantIntentKind.AttackTarget,
            M01AssistantIds.MissionId,
            M01AssistantIds.AttackStepId,
            AssistantTargetType.RuntimeEntity,
            M01AssistantIds.EnemyPatrolEntityId,
            TacticalCommandMode.Attack,
            canExecuteGameplay: true,
            requiresSelectedEntity: true,
            requiresVisibleTarget: true,
            "one attack intent");
    }

    private static AssistantContext CreateM01Context(bool commandSquadSpawned, bool typedHooksAvailable)
    {
        return new AssistantContext
        {
            MissionId = M01AssistantIds.MissionId,
            ScenarioSetupId = M01AssistantIds.ScenarioSetupId,
            LevelId = M01AssistantIds.LevelId,
            IsoMapId = M01AssistantIds.IsoMapId,
            IsMatchOverlayActive = true,
            CommandSquadSpawned = commandSquadSpawned,
            TypedCommandHooksAvailable = typedHooksAvailable
        };
    }

    private static void AssertRejected(TacticalCommandResult result, TacticalCommandReasonCode reasonCode)
    {
        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(reasonCode, result.ReasonCode);
    }

    private static string ResolveRepoFilePath(string relativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        return string.IsNullOrEmpty(projectRoot)
            ? Path.GetFullPath(relativePath)
            : Path.Combine(projectRoot, relativePath);
    }
}
