using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class Chapter01M01PlayableRuntimeTests
{
    private static readonly int2 PlayerSpawnCell = new(980, 1000);
    private static readonly int2 EnemySpawnCell = new(1032, 1000);
    private static readonly int2 CameraStartCell = new(1006, 1000);
    private static readonly int2 CoverCell = new(992, 1004);
    private World _world;
    private World _previousWorld;
    private GameObject _cameraObject;

    [SetUp]
    public void SetUp()
    {
        GameRuntimeStats.Reset();
        new ActiveMissionSession().BeginMission(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureRoute.SagaMap);
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("Chapter01M01PlayableRuntimeTests");
        World.DefaultGameObjectInjectionWorld = _world;
    }

    [TearDown]
    public void TearDown()
    {
        if (_cameraObject != null)
            Object.DestroyImmediate(_cameraObject);
        if (_world != null && _world.IsCreated)
            _world.Dispose();
        World.DefaultGameObjectInjectionWorld = _previousWorld;
        InitialUnitsRuntimeState.PlayRequested = false;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.BuildModeActive = false;
        new ActiveMissionSession().Clear();
        GameRuntimeStats.Reset();
    }

    [Test]
    public void M01ActiveMissionSession_ResolvesProductionContractIds()
    {
        MissionConfig mission = new ActiveMissionSession().ActiveMission;

        Assert.AreEqual("saga.ch01.m01.first_contact", new ActiveMissionSession().ActiveMissionId);
        Assert.AreEqual("scenario.ch01.m01.first_contact", new ActiveMissionSession().ActiveScenarioSetupId);
        Assert.AreEqual("level.ch01.district_edge_01", new ActiveMissionSession().ActiveLevelId);
        Assert.AreEqual("iso.ch01.district_edge_01", new ActiveMissionSession().ActiveIsoMapId);
        Assert.AreEqual("preview.ch01.first_contact", new ActiveMissionSession().ActiveMapPreviewArtId);
        Assert.AreEqual("minimap.ch01.first_contact", new ActiveMissionSession().ActiveMinimapArtId);
        Assert.AreEqual(1, mission.Objectives[0].TargetAmount);
        Assert.AreEqual("command_squad_survives", mission.Objectives[1].Id);
    }

    [Test]
    public void M01FixedMissionCells_ResolveToGameplayXZRuntimePositions()
    {
        Assert.IsTrue(Chapter01M01PlayableRuntime.TryGetCameraStartWorld(_world, out Vector3 cameraStartWorld));

        Assert.AreEqual(new Vector3(CameraStartCell.x, 0f, CameraStartCell.y), cameraStartWorld);
        Assert.AreEqual(CoverCell, Chapter01M01PlayableRuntime.GetMoveToCoverCell());
    }

    [Test]
    public void Initialize_CreatesFriendlySquadAndHostilePatrolFromMetadataAnchors()
    {
        Assert.IsTrue(Chapter01M01PlayableRuntime.TryInitializeActiveMission(_world, out Chapter01M01PlayableRuntime.RuntimeState state));

        EntityManager em = _world.EntityManager;
        Assert.IsTrue(em.Exists(state.PlayerSquad));
        Assert.IsTrue(em.Exists(state.EnemyPatrol));
        Assert.IsTrue(em.Exists(state.CommandPoint));
        Assert.AreEqual(0, em.GetComponentData<Faction>(state.PlayerSquad).Id);
        Assert.AreEqual(1, em.GetComponentData<Faction>(state.EnemyPatrol).Id);
        Assert.AreEqual(Chapter01M01PlayableRuntime.PlayerSquadEntityId, em.GetComponentData<MissionRuntimeEntityId>(state.PlayerSquad).Value.ToString());
        Assert.AreEqual(Chapter01M01PlayableRuntime.EnemyPatrolEntityId, em.GetComponentData<MissionRuntimeEntityId>(state.EnemyPatrol).Value.ToString());
        Assert.AreEqual(Chapter01M01PlayableRuntime.DecorCommandPointEntityId, em.GetComponentData<MissionRuntimeEntityId>(state.CommandPoint).Value.ToString());
        Assert.IsTrue(em.HasComponent<MissionRuntimeCommandSquadTag>(state.PlayerSquad));
        Assert.IsTrue(em.HasComponent<MissionRuntimeEnemyPatrolTag>(state.EnemyPatrol));
        Assert.IsTrue(em.HasComponent<MissionRuntimePatrolRoute>(state.EnemyPatrol));
        Assert.IsTrue(em.HasComponent<UnitPathRequest>(state.EnemyPatrol));
        Assert.AreEqual("Unit_Chr_Soldier_Male_02_Alt_04", em.GetComponentData<UnitSourcePrefabKey>(state.PlayerSquad).Value.ToString());
        Assert.AreEqual("Unit_Chr_Soldier_Male_02_Alt_04", em.GetComponentData<UnitSourcePrefabKey>(state.EnemyPatrol).Value.ToString());
        Assert.IsFalse(em.HasComponent<UnitDestroyedVisualReference>(state.CommandPoint));

        AssertRuntimePosition(state.PlayerSpawnWorld, em.GetComponentData<LocalTransform>(state.PlayerSquad).Position);
        AssertRuntimePosition(state.EnemySpawnWorld, em.GetComponentData<LocalTransform>(state.EnemyPatrol).Position);
        AssertRuntimePosition(state.CommandPointWorld, em.GetComponentData<LocalTransform>(state.CommandPoint).Position);
    }

    [Test]
    public void Initialize_BindsExistingSpawnedUnitsBeforeCreatingFallbacks()
    {
        Entity playerCandidate = CreateCandidateUnit(0, PlayerSpawnCell);
        Entity enemyCandidate = CreateCandidateUnit(1, EnemySpawnCell);

        Assert.IsTrue(Chapter01M01PlayableRuntime.TryInitializeActiveMission(_world, out Chapter01M01PlayableRuntime.RuntimeState state));

        Assert.AreEqual(playerCandidate, state.PlayerSquad);
        Assert.AreEqual(enemyCandidate, state.EnemyPatrol);
        Assert.AreEqual(Chapter01M01PlayableRuntime.PlayerSquadEntityId, _world.EntityManager.GetComponentData<MissionRuntimeEntityId>(playerCandidate).Value.ToString());
        Assert.AreEqual(Chapter01M01PlayableRuntime.EnemyPatrolEntityId, _world.EntityManager.GetComponentData<MissionRuntimeEntityId>(enemyCandidate).Value.ToString());
        Assert.AreEqual("Unit_Chr_Soldier_Male_02_Alt_04", _world.EntityManager.GetComponentData<UnitSourcePrefabKey>(playerCandidate).Value.ToString());
        Assert.AreEqual("Unit_Chr_Soldier_Male_02_Alt_04", _world.EntityManager.GetComponentData<UnitSourcePrefabKey>(enemyCandidate).Value.ToString());
    }

    [Test]
    public void DestroyingHostilePatrol_CompletesObjectiveAndReadiesResultRoute()
    {
        Assert.IsTrue(Chapter01M01PlayableRuntime.TryInitializeActiveMission(_world, out Chapter01M01PlayableRuntime.RuntimeState state));

        EntityManager em = _world.EntityManager;
        em.SetComponentData(state.EnemyPatrol, new UnitHealth { Current = 0, Max = 100 });
        GameRuntimeStats.RecordMilitaryDeath(1);

        Assert.IsTrue(Chapter01M01PlayableRuntime.TryEvaluateActiveMission(_world, out Chapter01M01PlayableRuntime.Evaluation evaluation));
        Assert.IsTrue(evaluation.CommandSquadAlive);
        Assert.IsTrue(evaluation.PatrolDestroyed);
        Assert.IsTrue(evaluation.ObjectiveComplete);
        Assert.IsTrue(evaluation.ResultRouteReady);
        Assert.IsTrue(Chapter01M01PlayableRuntime.ShouldStartResultFlow(_world));
    }

    [Test]
    public void LosingCommandSquad_PreventsM01Completion()
    {
        Assert.IsTrue(Chapter01M01PlayableRuntime.TryInitializeActiveMission(_world, out Chapter01M01PlayableRuntime.RuntimeState state));

        EntityManager em = _world.EntityManager;
        em.SetComponentData(state.PlayerSquad, new UnitHealth { Current = 0, Max = 100 });
        em.SetComponentData(state.EnemyPatrol, new UnitHealth { Current = 0, Max = 100 });
        GameRuntimeStats.RecordMilitaryDeath(0);
        GameRuntimeStats.RecordMilitaryDeath(1);

        MissionResultData result = new ActiveMissionSession().BuildCurrentResult(GameRuntimeStats.GetSnapshot());
        Assert.IsFalse(result.Victory);
        Assert.IsTrue(Chapter01M01PlayableRuntime.TryEvaluateActiveMission(_world, out Chapter01M01PlayableRuntime.Evaluation evaluation));
        Assert.IsFalse(evaluation.CommandSquadAlive);
        Assert.IsFalse(evaluation.ObjectiveComplete);
        Assert.IsFalse(evaluation.ResultRouteReady);
    }

    [Test]
    public void Build_IsDisabledForM01WithSharedMissionReason()
    {
        Assert.IsFalse(new MissionCommandPolicySystem().IsBuildAllowedForActiveOperation());
        Assert.AreEqual("Building unlocks in the next mission.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.MissionDoesNotAllowBuild));

        new ActiveMissionSession().BeginMission("saga.ch01.m02.establish_base", WarlineCaptureRoute.SagaMap);
        Assert.IsTrue(new MissionCommandPolicySystem().IsBuildAllowedForActiveOperation());
    }

    [Test]
    public void RoadBuildCommandSetBuildMode_RejectsM01WithoutEnteringBuildMode()
    {
        InitialUnitsRuntimeState.PlayRequested = true;
        InitialUnitsRuntimeState.SelectionModeActive = true;
        InitialUnitsRuntimeState.BuildModeActive = false;

        var commandSystem = new RoadBuildCommandSystem();
        var runtimeGameplayStateSystem = new RuntimeGameplayStateSystem();
        var context = new RoadBuildCommandSystem.Context(
            runtimeGameplayStateSystem,
            new RoadBuildSessionSystem(),
            default,
            null);

        commandSystem.SetBuildMode(context, true);

        Assert.IsFalse(InitialUnitsRuntimeState.BuildModeActive, "M01 must not enter road/build mode through the road build command boundary.");
    }

    [Test]
    public void FixedTacticalMissionGuardrail_DisablesGenericAIPlansOnlyWhenActive()
    {
        EntityManager em = _world.EntityManager;
        Entity buildPlan = em.CreateEntity(typeof(AIBuildPlan));
        em.SetComponentData(buildPlan, new AIBuildPlan { FactionId = 1, Enabled = 1 });
        Entity productionPlan = em.CreateEntity(typeof(AIProductionPlan));
        em.SetComponentData(productionPlan, new AIProductionPlan { FactionId = 1, Enabled = 1 });
        Entity squadPlan = em.CreateEntity(typeof(AISquadPlan));
        em.SetComponentData(squadPlan, new AISquadPlan { FactionId = 1, Enabled = 1 });

        MissionStartupSystem missionStartupSystem = new();
        missionStartupSystem.DisableGenericAIPlansForFixedTacticalMission(_world, false);

        Assert.AreEqual(1, em.GetComponentData<AIBuildPlan>(buildPlan).Enabled, "Non-M01 AI build plans must remain available.");
        Assert.AreEqual(1, em.GetComponentData<AIProductionPlan>(productionPlan).Enabled, "Non-M01 AI production plans must remain available.");
        Assert.AreEqual(1, em.GetComponentData<AISquadPlan>(squadPlan).Enabled, "Non-M01 AI squad plans must remain available.");

        missionStartupSystem.DisableGenericAIPlansForFixedTacticalMission(_world, true);

        Assert.AreEqual(0, em.GetComponentData<AIBuildPlan>(buildPlan).Enabled, "M01 uses authored patrol scripting, not generic base-building AI.");
        Assert.AreEqual(0, em.GetComponentData<AIProductionPlan>(productionPlan).Enabled, "M01 should not emit generic producer-missing log noise.");
        Assert.AreEqual(0, em.GetComponentData<AISquadPlan>(squadPlan).Enabled, "M01 should not emit generic squad-waiting log noise.");
    }

    [Test]
    public void MissionStartupSystem_AppliesM01CameraFraming()
    {
        Camera camera = CreateCamera();
        camera.aspect = 16f / 9f;

        MissionStartupSystem missionStartupSystem = new();
        Assert.IsTrue(missionStartupSystem.ApplyM01ProductionCameraPoseForCurrentAspect(_world, camera));

        Assert.IsTrue(camera.orthographic);
        Assert.AreEqual(10f, camera.transform.position.y, 0.0001f);
        Assert.AreEqual(90f, camera.transform.rotation.eulerAngles.x, 0.0001f);
        Assert.GreaterOrEqual(camera.orthographicSize, 0.72f);
        Assert.LessOrEqual(camera.orthographicSize, 0.96f);
    }

    private Camera CreateCamera()
    {
        _cameraObject = new GameObject("M01RuntimeCameraTestRoot");
        return _cameraObject.AddComponent<Camera>();
    }

    private Entity CreateCandidateUnit(byte factionId, int2 cell)
    {
        Vector3 worldPosition = new(cell.x, 0f, cell.y);

        Entity entity = _world.EntityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(UnitHealth),
            typeof(LocalTransform));
        _world.EntityManager.SetComponentData(entity, new Faction { Id = factionId });
        _world.EntityManager.SetComponentData(entity, new UnitGrid { Cell = cell });
        _world.EntityManager.SetComponentData(entity, new UnitMove { Speed = 1f, WalkSpeed = 1f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        _world.EntityManager.SetComponentData(entity, new UnitCombat { AggroRangeCells = 5, ChaseBreakDistance = 1f, CanAttack = 1, AutoEngage = 1 });
        _world.EntityManager.SetComponentData(entity, new UnitAttack { Range = 1f, CooldownSeconds = 1f, Damage = 10 });
        _world.EntityManager.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        _world.EntityManager.SetComponentData(entity, LocalTransform.FromPosition(worldPosition));
        return entity;
    }

    private static void AssertRuntimePosition(Vector3 expected, float3 actual)
    {
        Assert.AreEqual(expected.x, actual.x, 0.0001f);
        Assert.AreEqual(expected.y, actual.y, 0.0001f);
        Assert.AreEqual(expected.z, actual.z, 0.0001f);
    }
}
