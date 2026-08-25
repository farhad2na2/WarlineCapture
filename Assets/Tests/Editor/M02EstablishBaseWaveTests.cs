#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseWaveTests
{
    private const string Marker = "[M02EstablishBaseWaveValidation] result=Passed tests=9";
    private const string MissionId = "saga.ch01.m02.establish_base";
    private const string ScenarioId = "scenario.ch01.m02.establish_base";
    private const string MapId = "opmap.ch01.forward_post_01";

    [MenuItem("Game/Validation/Run M02 Establish Base Wave Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseWaveTests tests = new();
            tests.CanonicalProjectionCarriesExactDelayedWaveContract();
            tests.DelayedWaveSpawnStartsSuppressedAndCannotAutoEngage();
            tests.WarningIsIssuedOnceAtTheAuthoredBoundary();
            tests.TimeJumpStillWarnsBeforeOneShotActivation();
            tests.MissingOrDuplicateWaveRosterFailsClosed();
            tests.StaleAttemptIdentityCannotWarnOrActivate();
            tests.PatrolRouteGatePreservesM01AndRequiresM02Activation();
            tests.SuppressedActorsAreExcludedFromAutomaticTargetAcquisitionQueries();
            tests.SuppressedTargetsCannotTakeUnitAttackDamage();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseWaveValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [MenuItem("Game/Validation/Run M02 Establish Base Wave Regressions")]
    public static void RunRegressionValidation()
    {
        try
        {
            RunValidation(RunFocusedValidation);
            RunValidation(ThreatWarningValidationTests.RunBatchValidation);
            RunValidation(MatchHudMinimapMarkerSystemTests.RunFocusedValidation);
            RunValidation(M01FirstContactContractValidation.RunFocusedValidation);
            RunValidation(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            Debug.Log("[M02EstablishBaseWaveRegressionValidation] result=Passed suites=5");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseWaveRegressionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void CanonicalProjectionCarriesExactDelayedWaveContract()
    {
        using WaveFixture fixture = new(spawnWave: false);
        ref CampaignMissionDefinitionBlob definition = ref fixture.Definition;
        Assert.AreEqual("group.ch01.m02.hostile_patrol", definition.DelayedWaveUnitGroupId.ToString());
        Assert.AreEqual("route.ch01.m02.hostile_patrol", definition.DelayedWaveRouteId.ToString());
        Assert.AreEqual("role.friendly.forward_post", definition.DelayedWaveTargetMissionRoleId.ToString());
        Assert.AreEqual(90000, definition.DelayedWaveWarningAtMilliseconds);
        Assert.AreEqual(120000, definition.DelayedWaveActivationAtMilliseconds);
        Assert.IsTrue(fixture.EntityManager.HasComponent<CampaignMissionDelayedWaveStateComponent>(fixture.Root));
        Assert.IsTrue(CampaignMissionDelayedWaveUtility.TryResolveDefinition(
            ref definition, out int expectedCount, out byte factionId));
        Assert.AreEqual(3, expectedCount);
        Assert.AreEqual(FactionIdentity.EnemyFactionId, factionId);
    }

    [Test]
    public void DelayedWaveSpawnStartsSuppressedAndCannotAutoEngage()
    {
        using WaveFixture fixture = new(spawnWave: false);
        ref CampaignMissionDefinitionBlob definition = ref fixture.Definition;
        Entity entity = fixture.EntityManager.CreateEntity(typeof(UnitCombat));
        fixture.EntityManager.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1 });

        Assert.IsTrue(CampaignMissionDelayedWaveUtility.ShouldSuppressAtSpawn(
            ref definition, new FixedString64Bytes("group.ch01.m02.hostile_patrol")));
        Assert.IsFalse(CampaignMissionDelayedWaveUtility.ShouldSuppressAtSpawn(
            ref definition, new FixedString64Bytes("group.ch01.m02.command_squad")));
        CampaignMissionDelayedWaveUtility.ApplyCombatHoldAtSpawn(
            fixture.EntityManager,
            entity,
            ref definition,
            new FixedString64Bytes("group.ch01.m02.hostile_patrol"));
        Assert.AreEqual(0, fixture.EntityManager.GetComponentData<UnitCombat>(entity).AutoEngage);
    }

    [Test]
    public void WarningIsIssuedOnceAtTheAuthoredBoundary()
    {
        using WaveFixture fixture = new();
        fixture.SetElapsed(89999);
        fixture.Run();
        Assert.AreEqual(0, fixture.WaveState.WarningIssued);

        fixture.SetElapsed(90000);
        fixture.Run();
        ThreatWarningRuntimeStateComponent warning = fixture.Warning;
        Assert.AreEqual(1, warning.HasPendingWarning);
        Assert.AreEqual(ThreatWarningType.Ground, warning.PendingType);
        Assert.AreEqual(30f, warning.PendingEtaSeconds);
        Assert.AreEqual(3, warning.PendingThreatCount);
        Assert.AreEqual(1, fixture.WaveState.WarningIssued);
        Assert.AreEqual(1, fixture.Facts.DefenseWaveWarningIssued);

        Assert.IsTrue(ThreatWarningRuntimeState.ClearPendingWarning(fixture.EntityManager));
        uint clearedVersion = fixture.Warning.Version;
        fixture.Run();
        Assert.AreEqual(clearedVersion, fixture.Warning.Version);
        Assert.AreEqual(0, fixture.Warning.HasPendingWarning);
    }

    [Test]
    public void TimeJumpStillWarnsBeforeOneShotActivation()
    {
        using WaveFixture fixture = new();
        fixture.SetElapsed(120000);
        fixture.Run();
        Assert.AreEqual(1, fixture.WaveState.WarningIssued);
        Assert.AreEqual(0, fixture.WaveState.Activated);
        fixture.AssertWaveSuppressed(expected: true, expectedAutoEngage: 0);

        fixture.Run();
        Assert.AreEqual(1, fixture.WaveState.Activated);
        Assert.AreEqual(1, fixture.Facts.DefenseWaveActivated);
        fixture.AssertWaveSuppressed(expected: false, expectedAutoEngage: 1);

        fixture.Run();
        Assert.AreEqual(1, fixture.WaveState.Activated);
        fixture.AssertWaveSuppressed(expected: false, expectedAutoEngage: 1);
    }

    [Test]
    public void MissingOrDuplicateWaveRosterFailsClosed()
    {
        using WaveFixture fixture = new();
        fixture.EntityManager.DestroyEntity(fixture.WaveEntities[0]);
        fixture.SetElapsed(90000);
        fixture.Run();
        Assert.AreEqual(0, fixture.WaveState.WarningIssued);
        Assert.AreEqual(0, fixture.Facts.DefenseWaveWarningIssued);

        fixture.CreateWaveEntity();
        fixture.CreateWaveEntity();
        fixture.Run();
        Assert.AreEqual(0, fixture.WaveState.WarningIssued);
        Assert.AreEqual(0, fixture.Facts.DefenseWaveWarningIssued);
    }

    [Test]
    public void StaleAttemptIdentityCannotWarnOrActivate()
    {
        using WaveFixture fixture = new();
        CampaignMissionDelayedWaveStateComponent state = fixture.WaveState;
        state.SessionToken = new FixedString64Bytes("stale-session");
        state.WarningIssued = 0;
        fixture.EntityManager.SetComponentData(fixture.Root, state);
        fixture.SetElapsed(120000);
        fixture.Run();
        Assert.AreEqual(0, fixture.WaveState.WarningIssued);
        fixture.AssertWaveSuppressed(expected: true, expectedAutoEngage: 0);

        state.SessionToken = fixture.Runtime.SessionToken;
        state.AttemptOrdinal = fixture.Runtime.AttemptOrdinal;
        state.SourceVersion = fixture.Catalog.SourceVersion;
        fixture.EntityManager.SetComponentData(fixture.Root, state);
        fixture.Run();
        Assert.AreEqual(1, fixture.WaveState.WarningIssued);
        Assert.AreEqual(0, fixture.WaveState.Activated);
    }

    [Test]
    public void PatrolRouteGatePreservesM01AndRequiresM02Activation()
    {
        FixedString64Bytes m02 = new(MissionId);
        FixedString64Bytes m01 = new("saga.ch01.m01.first_contact");
        Assert.IsFalse(CampaignMissionDelayedWaveUtility.ShouldIssuePatrolRoute(
            m02, MissionPhaseKind.Engage, missionRuntimeEnabled: 1, delayedWaveActivated: 0));
        Assert.IsTrue(CampaignMissionDelayedWaveUtility.ShouldIssuePatrolRoute(
            m02, MissionPhaseKind.FindSquad, missionRuntimeEnabled: 1, delayedWaveActivated: 1));
        Assert.IsFalse(CampaignMissionDelayedWaveUtility.ShouldIssuePatrolRoute(
            m01, MissionPhaseKind.FindSquad, missionRuntimeEnabled: 0, delayedWaveActivated: 1));
        Assert.IsFalse(CampaignMissionDelayedWaveUtility.ShouldIssuePatrolRoute(
            m01, MissionPhaseKind.Engage, missionRuntimeEnabled: 0, delayedWaveActivated: 0));
    }

    [Test]
    public void SuppressedActorsAreExcludedFromAutomaticTargetAcquisitionQueries()
    {
        using World world = new(nameof(SuppressedActorsAreExcludedFromAutomaticTargetAcquisitionQueries));
        EntityManager entityManager = world.EntityManager;
        CreateGrid(entityManager);
        Entity attacker = CreateCombatUnit(
            entityManager, FactionIdentity.PlayerFactionId, new int2(10, 10), suppressed: false);
        Entity target = CreateCombatUnit(
            entityManager, FactionIdentity.EnemyFactionId, new int2(11, 10), suppressed: true);
        using EntityQuery acquisitionPopulation = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitCombat>(),
                ComponentType.ReadOnly<UnitAttack>(),
                ComponentType.ReadOnly<LocalTransform>()
            },
            None = new[] { ComponentType.ReadOnly<CampaignMissionCombatSuppressedTag>() }
        });

        using (NativeArray<Entity> heldPopulation = acquisitionPopulation.ToEntityArray(Allocator.Temp))
        {
            Assert.AreEqual(1, heldPopulation.Length);
            Assert.AreEqual(attacker, heldPopulation[0]);
        }

        entityManager.RemoveComponent<CampaignMissionCombatSuppressedTag>(target);
        using NativeArray<Entity> releasedPopulation = acquisitionPopulation.ToEntityArray(Allocator.Temp);
        CollectionAssert.AreEquivalent(new[] { attacker, target }, releasedPopulation.ToArray());
    }

    [Test]
    public void SuppressedTargetsCannotTakeUnitAttackDamage()
    {
        using World world = new(nameof(SuppressedTargetsCannotTakeUnitAttackDamage));
        EntityManager entityManager = world.EntityManager;
        CreateGrid(entityManager);
        Entity attacker = CreateCombatUnit(
            entityManager, FactionIdentity.PlayerFactionId, new int2(10, 10), suppressed: false);
        Entity target = CreateCombatUnit(
            entityManager, FactionIdentity.EnemyFactionId, new int2(11, 10), suppressed: true);
        AddAttackState(entityManager, attacker, target);
        SystemHandle attack = world.CreateSystem<UnitAttackSystem>();

        world.SetTime(new TimeData(1d, 0.2f));
        attack.Update(world.Unmanaged);
        entityManager.CompleteAllTrackedJobs();
        Assert.AreEqual(100, entityManager.GetComponentData<UnitHealth>(target).Current);

        entityManager.RemoveComponent<CampaignMissionCombatSuppressedTag>(target);
        entityManager.SetComponentData(attacker, Engage(target));
        world.SetTime(new TimeData(1.2d, 0.2f));
        attack.Update(world.Unmanaged);
        entityManager.CompleteAllTrackedJobs();
        Assert.AreEqual(90, entityManager.GetComponentData<UnitHealth>(target).Current);
    }

    private static void RunValidation(Action validation)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
            validation();
        if (ValidationExit.LastExitCode is int exitCode && exitCode != 0)
            throw new InvalidOperationException(
                $"{validation.Method.DeclaringType?.Name}.{validation.Method.Name} failed validation.");
    }

    private static void CreateGrid(EntityManager entityManager)
    {
        Entity grid = entityManager.CreateEntity(typeof(GridConfig));
        entityManager.SetComponentData(grid, new GridConfig
        {
            Width = 64,
            Height = 64,
            CellSize = 1f,
            Origin = float3.zero
        });
    }

    private static Entity CreateCombatUnit(
        EntityManager entityManager,
        byte factionId,
        int2 cell,
        bool suppressed)
    {
        Entity entity = entityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(LocalTransform));
        entityManager.SetComponentData(entity, new Faction { Id = factionId });
        entityManager.SetComponentData(entity, new UnitGrid { Cell = cell });
        entityManager.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        entityManager.SetComponentData(entity, new UnitCombat
        {
            CanAttack = 1,
            AutoEngage = 1,
            AggroRangeCells = 8,
            ChaseBreakDistance = 16f
        });
        entityManager.SetComponentData(entity, new UnitAttack
        {
            Range = 8f,
            CooldownSeconds = 1f,
            Damage = 10,
            TraceVisibleSeconds = 0.1f,
            TracerEveryNthShot = 1
        });
        entityManager.SetComponentData(
            entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        if (suppressed)
            entityManager.AddComponent<CampaignMissionCombatSuppressedTag>(entity);
        return entity;
    }

    private static void AddAttackState(EntityManager entityManager, Entity attacker, Entity target)
    {
        entityManager.AddComponentData(attacker, Engage(target));
        entityManager.AddComponentData(attacker, new UnitAttackCooldownComponent());
        entityManager.AddComponentData(attacker, new UnitAttackTraceComponent());
        entityManager.AddComponentData(attacker, new UnitAttackAnimationComponent());
    }

    private static EngageTarget Engage(Entity target) => new()
    {
        Target = target,
        Cell = new int2(11, 10),
        Position = new float3(11.5f, 0f, 10.5f),
        IsCommanded = 1
    };

    private sealed class WaveFixture : IDisposable
    {
        private readonly World _world;
        private readonly SystemHandle _system;
        private CampaignMissionCatalogComponent _catalog;

        internal WaveFixture(bool spawnWave = true)
        {
            _world = new World("m02-wave-fixture");
            EntityManager entityManager = _world.EntityManager;
            MissionDefinitionCatalogConfig missions = AssetDatabase.LoadAssetAtPath<MissionDefinitionCatalogConfig>(
                M02EstablishBaseConfigBuilder.MissionCatalogPath);
            OperationMapCatalogConfig maps = AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(
                M02EstablishBaseConfigBuilder.OperationMapCatalogPath);
            Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(
                entityManager, missions, maps, 18, out Entity root, out string error), error);
            Root = root;
            _catalog = entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            Runtime = new CampaignMissionRuntimeComponent
            {
                MissionId = new FixedString64Bytes(MissionId),
                ScenarioId = new FixedString64Bytes(ScenarioId),
                OperationMapId = new FixedString64Bytes(MapId),
                SessionToken = new FixedString64Bytes("m02-wave-session"),
                Phase = MissionPhaseKind.FindSquad,
                Outcome = MissionOutcomeKind.None,
                LaunchOrigin = MissionLaunchOriginKind.CampaignOperations,
                RunKind = MissionRunKind.FirstClear,
                SourceVersion = _catalog.SourceVersion,
                AttemptOrdinal = 2,
                DeterministicSeed = 2002001,
                Version = 1
            };
            entityManager.SetComponentData(root, Runtime);
            entityManager.SetComponentData(root, new CampaignMissionAttemptFactsComponent
            {
                CommandSquadSpawned = 1,
                CommandSquadAlive = 1,
                HostileTotalCount = 3
            });
            entityManager.SetComponentData(root, new CampaignMissionDelayedWaveStateComponent
            {
                SessionToken = Runtime.SessionToken,
                AttemptOrdinal = Runtime.AttemptOrdinal,
                SourceVersion = _catalog.SourceVersion,
                Initialized = 1
            });
            Entity warning = entityManager.CreateEntity(typeof(ThreatWarningRuntimeStateComponent));
            entityManager.SetComponentData(warning, new ThreatWarningRuntimeStateComponent
            {
                PendingType = ThreatWarningType.Ground
            });
            if (spawnWave)
            {
                CreateWaveEntity();
                CreateWaveEntity();
                CreateWaveEntity();
            }
            _system = _world.GetOrCreateSystem<CampaignMissionDelayedWaveSystem>();
        }

        internal EntityManager EntityManager => _world.EntityManager;
        internal Entity Root { get; }
        internal CampaignMissionCatalogComponent Catalog => _catalog;
        internal CampaignMissionRuntimeComponent Runtime { get; }
        internal List<Entity> WaveEntities { get; } = new();
        internal CampaignMissionAttemptFactsComponent Facts =>
            EntityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(Root);
        internal CampaignMissionDelayedWaveStateComponent WaveState =>
            EntityManager.GetComponentData<CampaignMissionDelayedWaveStateComponent>(Root);
        internal ThreatWarningRuntimeStateComponent Warning
        {
            get
            {
                Assert.IsTrue(ThreatWarningRuntimeState.TryRead(
                    EntityManager, out ThreatWarningRuntimeStateComponent warning));
                return warning;
            }
        }

        internal ref CampaignMissionDefinitionBlob Definition
        {
            get
            {
                for (int i = 0; i < _catalog.Blob.Value.Missions.Length; i++)
                {
                    if (_catalog.Blob.Value.Missions[i].MissionId.Equals(Runtime.MissionId))
                        return ref _catalog.Blob.Value.Missions[i];
                }
                throw new InvalidOperationException("Canonical M02 definition is missing.");
            }
        }

        internal Entity CreateWaveEntity()
        {
            ref CampaignMissionDefinitionBlob definition = ref Definition;
            Entity entity = EntityManager.CreateEntity(
                typeof(CampaignMissionUnitRoleComponent),
                typeof(Faction),
                typeof(UnitCombat),
                typeof(UnitHealth),
                typeof(CampaignMissionCombatSuppressedTag),
                typeof(CampaignMissionStationaryUnitTag));
            EntityManager.SetComponentData(entity, new CampaignMissionUnitRoleComponent
            {
                MissionRoleId = new FixedString64Bytes("role.hostile.patrol"),
                UnitGroupId = definition.DelayedWaveUnitGroupId,
                RouteId = definition.DelayedWaveRouteId,
                SessionToken = Runtime.SessionToken
            });
            EntityManager.SetComponentData(entity, new Faction { Id = FactionIdentity.EnemyFactionId });
            EntityManager.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 0 });
            EntityManager.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
            WaveEntities.Add(entity);
            return entity;
        }

        internal void SetElapsed(int elapsedMilliseconds)
        {
            CampaignMissionAttemptFactsComponent facts = Facts;
            facts.ElapsedMilliseconds = elapsedMilliseconds;
            EntityManager.SetComponentData(Root, facts);
        }

        internal void Run()
        {
            _world.Unmanaged.GetUnsafeSystemRef<CampaignMissionDelayedWaveSystem>(_system)
                .OnUpdate(ref _world.Unmanaged.ResolveSystemStateRef(_system));
        }

        internal void AssertWaveSuppressed(bool expected, byte expectedAutoEngage)
        {
            for (int i = 0; i < WaveEntities.Count; i++)
            {
                Entity entity = WaveEntities[i];
                if (!EntityManager.Exists(entity))
                    continue;
                Assert.AreEqual(expected,
                    EntityManager.HasComponent<CampaignMissionCombatSuppressedTag>(entity));
                Assert.AreEqual(expected,
                    EntityManager.HasComponent<CampaignMissionStationaryUnitTag>(entity));
                Assert.AreEqual(expectedAutoEngage, EntityManager.GetComponentData<UnitCombat>(entity).AutoEngage);
            }
        }

        public void Dispose()
        {
            CampaignMissionCatalogComponent catalog =
                EntityManager.GetComponentData<CampaignMissionCatalogComponent>(Root);
            CampaignMissionCatalogDisposalSystem.DisposeOwned(ref catalog);
            EntityManager.SetComponentData(Root, catalog);
            _world.Dispose();
        }
    }
}
#endif
