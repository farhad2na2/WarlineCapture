using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class M01AssistantCommandRuntimeTests
{
    private const string DefinitionPath = "Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset";

    private World _previousWorld;
    private World _world;
    private GameObject _loaderRoot;
    private GameObject _cameraObject;
    private TacticalMapRuntimeLoader _loader;
    private Chapter01M01PlayableRuntime.RuntimeState _runtimeState;

    [SetUp]
    public void SetUp()
    {
        GameRuntimeStats.Reset();
        WarlineCaptureMissionSession.BeginMission(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureRoute.SagaMap);
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("M01AssistantCommandRuntimeTests");
        World.DefaultGameObjectInjectionWorld = _world;
        _loader = CreateLoadedRuntimeLoader();
        Assert.IsTrue(Chapter01M01PlayableRuntime.TryInitializeActiveMission(_world, _loader, out _runtimeState));
    }

    [TearDown]
    public void TearDown()
    {
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
    public void TrySelectRuntimeEntity_SelectsM01CommandSquad()
    {
        TacticalCommandResult result = M01AssistantCommandRuntime.TrySelectRuntimeEntity(
            _world,
            Chapter01M01PlayableRuntime.PlayerSquadEntityId);

        Assert.IsTrue(result.Accepted);
        Assert.IsTrue(_world.EntityManager.HasComponent<SelectedUnitTag>(_runtimeState.PlayerSquad));
    }

    [Test]
    public void TrySelectRuntimeEntity_RejectsInvalidId()
    {
        TacticalCommandResult result = M01AssistantCommandRuntime.TrySelectRuntimeEntity(
            _world,
            "unit.player.unknown");

        AssertRejected(result, TacticalCommandReasonCode.TargetNotAttackable);
    }

    [Test]
    public void TryIssueMoveToAnchor_RejectsMissingSelection()
    {
        TacticalCommandResult result = M01AssistantCommandRuntime.TryIssueMoveToAnchor(
            _world,
            _loader,
            M01AssistantCommandRuntime.MoveToCoverAnchorId);

        AssertRejected(result, TacticalCommandReasonCode.NoSelection);
    }

    [Test]
    public void TryIssueMoveToAnchor_IssuesNormalMoveOrderToCoverAnchor()
    {
        Assert.IsTrue(M01AssistantCommandRuntime.TrySelectRuntimeEntity(
            _world,
            Chapter01M01PlayableRuntime.PlayerSquadEntityId).Accepted);
        Assert.IsTrue(_loader.TryGetAnchorCell(M01AssistantCommandRuntime.MoveToCoverAnchorId, out Vector2Int coverCell));

        TacticalCommandResult result = M01AssistantCommandRuntime.TryIssueMoveToAnchor(
            _world,
            _loader,
            M01AssistantCommandRuntime.MoveToCoverAnchorId);

        EntityManager em = _world.EntityManager;
        Assert.IsTrue(result.Accepted);
        Assert.AreEqual(new int2(coverCell.x, coverCell.y), em.GetComponentData<UnitTarget>(_runtimeState.PlayerSquad).Cell);
        Assert.AreEqual(new int2(coverCell.x, coverCell.y), em.GetComponentData<UnitPathRequest>(_runtimeState.PlayerSquad).Goal);
    }

    [Test]
    public void TryIssueMoveToAnchor_RejectsMissingAnchor()
    {
        Assert.IsTrue(M01AssistantCommandRuntime.TrySelectRuntimeEntity(
            _world,
            Chapter01M01PlayableRuntime.PlayerSquadEntityId).Accepted);

        TacticalCommandResult result = M01AssistantCommandRuntime.TryIssueMoveToAnchor(
            _world,
            _loader,
            "tutorial.move_target.missing");

        AssertRejected(result, TacticalCommandReasonCode.TargetNotAttackable);
    }

    [Test]
    public void TryIssueMoveToAnchor_RejectsDeadSquad()
    {
        EntityManager em = _world.EntityManager;
        em.AddComponent<SelectedUnitTag>(_runtimeState.PlayerSquad);
        em.SetComponentData(_runtimeState.PlayerSquad, new UnitHealth { Current = 0, Max = 100 });

        TacticalCommandResult result = M01AssistantCommandRuntime.TryIssueMoveToAnchor(
            _world,
            _loader,
            M01AssistantCommandRuntime.MoveToCoverAnchorId);

        AssertRejected(result, TacticalCommandReasonCode.TargetNotAttackable);
    }

    [Test]
    public void TryIssueAttackTarget_IssuesNormalEngageTargetOrder()
    {
        Assert.IsTrue(M01AssistantCommandRuntime.TrySelectRuntimeEntity(
            _world,
            Chapter01M01PlayableRuntime.PlayerSquadEntityId).Accepted);

        TacticalCommandResult result = M01AssistantCommandRuntime.TryIssueAttackTarget(
            _world,
            Chapter01M01PlayableRuntime.EnemyPatrolEntityId);

        EntityManager em = _world.EntityManager;
        Assert.IsTrue(result.Accepted);
        EngageTarget engageTarget = em.GetComponentData<EngageTarget>(_runtimeState.PlayerSquad);
        Assert.AreEqual(_runtimeState.EnemyPatrol, engageTarget.Target);
        Assert.AreEqual(1, engageTarget.IsCommanded);
    }

    [Test]
    public void TryIssueAttackTarget_RejectsInvalidId()
    {
        Assert.IsTrue(M01AssistantCommandRuntime.TrySelectRuntimeEntity(
            _world,
            Chapter01M01PlayableRuntime.PlayerSquadEntityId).Accepted);

        TacticalCommandResult result = M01AssistantCommandRuntime.TryIssueAttackTarget(
            _world,
            "unit.enemy.unknown");

        AssertRejected(result, TacticalCommandReasonCode.TargetNotAttackable);
    }

    [Test]
    public void TryIssueAttackTarget_RejectsDeadPatrol()
    {
        Assert.IsTrue(M01AssistantCommandRuntime.TrySelectRuntimeEntity(
            _world,
            Chapter01M01PlayableRuntime.PlayerSquadEntityId).Accepted);
        _world.EntityManager.SetComponentData(_runtimeState.EnemyPatrol, new UnitHealth { Current = 0, Max = 100 });

        TacticalCommandResult result = M01AssistantCommandRuntime.TryIssueAttackTarget(
            _world,
            Chapter01M01PlayableRuntime.EnemyPatrolEntityId);

        AssertRejected(result, TacticalCommandReasonCode.TargetNotAttackable);
    }

    [Test]
    public void GetBuildCommandResult_RejectsM01BuildWithMissionReason()
    {
        TacticalCommandResult result = M01AssistantCommandRuntime.GetBuildCommandResult();

        AssertRejected(result, TacticalCommandReasonCode.MissionDoesNotAllowBuild);
        Assert.AreEqual(WarlineCaptureMissionRules.M01BuildDisabledMessage, result.Message);
    }

    private TacticalMapRuntimeLoader CreateLoadedRuntimeLoader()
    {
        TacticalMapDefinition definition = AssetDatabase.LoadAssetAtPath<TacticalMapDefinition>(DefinitionPath);
        Assert.NotNull(definition);

        _loaderRoot = new GameObject("M01AssistantRuntimeLoaderTestRoot");
        _cameraObject = new GameObject("M01AssistantRuntimeLoaderTestCamera");
        Camera camera = _cameraObject.AddComponent<Camera>();
        TacticalMapRuntimeLoader loader = _loaderRoot.AddComponent<TacticalMapRuntimeLoader>();
        loader.Configure(definition, null, camera, TacticalMapRuntimePlane.GameplayXZ);
        loader.Load();
        return loader;
    }

    private static void AssertRejected(TacticalCommandResult result, TacticalCommandReasonCode reasonCode)
    {
        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(reasonCode, result.ReasonCode);
        Assert.IsFalse(string.IsNullOrWhiteSpace(TacticalCommandFeedbackText.ToDisplayText(result.ReasonCode)) && string.IsNullOrWhiteSpace(result.Message));
    }
}
