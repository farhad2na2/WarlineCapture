using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class ThreatWarningValidationTests
{
    private const string ScenePath = "Assets/Game/Scenes/Match.unity";
    private const string RadarTankConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Radar_Tank.asset";
    private const string SatelliteDishConfigPath = "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Satelite_Dish_Config.asset";
    private const string GameStringsConfigPath = "Assets/Game/Configs/Scene/Game_GameStrings_Config.asset";

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new ThreatWarningValidationTests();
            tests.ThreatWarningConfigs_AssignDetectorRolesDescriptionsAndStrings();
            tests.MatchScene_DoesNotContainLegacyMenuViewWarningPanel();
            tests.ThreatDetectionWarningSystem_GroundRadarWarnsOnlyWhenNewGroundThreatEntersRadius();
            tests.ThreatDetectionWarningSystem_GroundRadarIgnoresEnemySoldiersMovingTowardSensor();
            tests.ThreatDetectionWarningSystem_IgnoresVehiclesMovingAwayFromSensor();
            tests.ThreatDetectionWarningSystem_SatelliteWarnsOnlyForAirThreats();
            tests.ThreatDetectionWarningSystem_CompletesPendingUnitGridWriterBeforeChunkRead();
            Debug.Log("[ThreatWarningValidation] result=Passed");
            ValidationExit.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[ThreatWarningValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ThreatWarningConfigs_AssignDetectorRolesDescriptionsAndStrings()
    {
        UnitGridAuthoringConfig radarConfig = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(RadarTankConfigPath);
        BuildingDefinitionAuthoringConfig satelliteConfig = AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(SatelliteDishConfigPath);
        GameStringsConfig stringsConfig = AssetDatabase.LoadAssetAtPath<GameStringsConfig>(GameStringsConfigPath);

        Assert.NotNull(radarConfig);
        Assert.NotNull(satelliteConfig);
        Assert.NotNull(stringsConfig);

        Assert.AreEqual(ThreatDetectionKind.Ground, radarConfig.ThreatDetectionKind);
        Assert.AreEqual(240, radarConfig.ThreatDetectionRadiusCells);
        Assert.IsFalse(radarConfig.CanAttack);
        Assert.That(radarConfig.Description, Does.Contain("ground"));
        Assert.That(radarConfig.Description, Does.Contain("cannot attack"));
        Assert.DoesNotThrow(() => _ = new FixedString128Bytes(radarConfig.Description ?? string.Empty));

        Assert.AreEqual(ThreatDetectionKind.Air, satelliteConfig.ThreatDetectionKind);
        Assert.AreEqual(240, satelliteConfig.ThreatDetectionRadiusCells);
        Assert.That(satelliteConfig.Description, Does.Contain("Air"));
        Assert.That(satelliteConfig.Description, Does.Contain("cannot attack"));
        Assert.DoesNotThrow(() => _ = new FixedString128Bytes(satelliteConfig.Description ?? string.Empty));

        GameStrings.Init(stringsConfig);
        Assert.AreEqual("Ground vehicle attack detected", GameStrings.Get("warning_ground_attack_type"));
        Assert.AreEqual("Air attack detected", GameStrings.Get("warning_air_attack_type"));
        Assert.AreEqual("Estimated time to base: 12 seconds", GameStrings.Format("warning_attack_eta_seconds", 12));
    }

    [Test]
    public void MatchScene_DoesNotContainLegacyMenuViewWarningPanel()
    {
        SceneYamlTestUtility scene = SceneYamlTestUtility.Load(ScenePath);
        Assert.Throws<AssertionException>(() => scene.FindRequiredBlockContaining("::Game.Scripts.UI.MenuView"));
        Assert.Throws<AssertionException>(() => scene.FindRequiredBlockContaining("m_Name: UI_Canvas"));
    }

    [Test]
    public void ThreatDetectionWarningSystem_GroundRadarWarnsOnlyWhenNewGroundThreatEntersRadius()
    {
        using var world = new World("ThreatDetectionWarningSystem_Ground");
        EntityManager em = world.EntityManager;
        Entity sensor = CreateUnit(em, FactionIdentity.PlayerFactionId, new int2(20, 20), false, 100, 0f);
        em.AddComponentData(sensor, new ThreatDetector
        {
            Kind = (byte)ThreatDetectionKind.Ground,
            RadiusCells = 20
        });
        CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(30, 20), false, 100, 5f, true, new int2(20, 20));
        CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(24, 20), true, 100, 15f, false, new int2(20, 20));

        SystemHandle system = world.CreateSystem<ThreatDetectionWarningSystem>();
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            ThreatWarningRuntimeState.Reset();

            system.Update(world.Unmanaged);
            Assert.IsTrue(ThreatWarningRuntimeState.HasPendingWarning);
            Assert.AreEqual(ThreatWarningType.Ground, ThreatWarningRuntimeState.PendingType);
            Assert.That(ThreatWarningRuntimeState.PendingEtaSeconds, Is.EqualTo(2f).Within(0.01f));

            ThreatWarningRuntimeState.ClearPendingWarning();
            system.Update(world.Unmanaged);
            Assert.IsFalse(ThreatWarningRuntimeState.HasPendingWarning);
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            ThreatWarningRuntimeState.Reset();
        }
    }

    [Test]
    public void ThreatDetectionWarningSystem_GroundRadarIgnoresEnemySoldiersMovingTowardSensor()
    {
        using var world = new World("ThreatDetectionWarningSystem_GroundIgnoresSoldiers");
        EntityManager em = world.EntityManager;
        Entity sensor = CreateUnit(em, FactionIdentity.PlayerFactionId, new int2(20, 20), false, 100, 0f);
        em.AddComponentData(sensor, new ThreatDetector
        {
            Kind = (byte)ThreatDetectionKind.Ground,
            RadiusCells = 20
        });
        CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(30, 20), false, 100, 5f, false, new int2(20, 20));

        SystemHandle system = world.CreateSystem<ThreatDetectionWarningSystem>();
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            ThreatWarningRuntimeState.Reset();

            system.Update(world.Unmanaged);
            Assert.IsFalse(ThreatWarningRuntimeState.HasPendingWarning);
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            ThreatWarningRuntimeState.Reset();
        }
    }

    [Test]
    public void ThreatDetectionWarningSystem_IgnoresVehiclesMovingAwayFromSensor()
    {
        using var world = new World("ThreatDetectionWarningSystem_IgnoresMovingAway");
        EntityManager em = world.EntityManager;
        Entity groundSensor = CreateUnit(em, FactionIdentity.PlayerFactionId, new int2(20, 20), false, 100, 0f);
        em.AddComponentData(groundSensor, new ThreatDetector
        {
            Kind = (byte)ThreatDetectionKind.Ground,
            RadiusCells = 20
        });
        Entity airSensor = CreateUnit(em, FactionIdentity.PlayerFactionId, new int2(50, 50), false, 100, 0f);
        em.AddComponentData(airSensor, new ThreatDetector
        {
            Kind = (byte)ThreatDetectionKind.Air,
            RadiusCells = 30
        });

        CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(30, 20), false, 100, 5f, true, new int2(45, 20));
        CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(65, 50), true, 100, 15f, false, new int2(90, 50));

        SystemHandle system = world.CreateSystem<ThreatDetectionWarningSystem>();
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            ThreatWarningRuntimeState.Reset();

            system.Update(world.Unmanaged);
            Assert.IsFalse(ThreatWarningRuntimeState.HasPendingWarning);
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            ThreatWarningRuntimeState.Reset();
        }
    }

    [Test]
    public void ThreatDetectionWarningSystem_SatelliteWarnsOnlyForAirThreats()
    {
        using var world = new World("ThreatDetectionWarningSystem_Air");
        EntityManager em = world.EntityManager;
        Entity sensor = CreateUnit(em, FactionIdentity.PlayerFactionId, new int2(50, 50), false, 100, 0f);
        em.AddComponentData(sensor, new ThreatDetector
        {
            Kind = (byte)ThreatDetectionKind.Air,
            RadiusCells = 30
        });
        CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(55, 50), false, 100, 5f, true, new int2(50, 50));
        CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(65, 50), true, 100, 15f, false, new int2(50, 50));

        SystemHandle system = world.CreateSystem<ThreatDetectionWarningSystem>();
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            ThreatWarningRuntimeState.Reset();

            system.Update(world.Unmanaged);
            Assert.IsTrue(ThreatWarningRuntimeState.HasPendingWarning);
            Assert.AreEqual(ThreatWarningType.Air, ThreatWarningRuntimeState.PendingType);
            Assert.That(ThreatWarningRuntimeState.PendingEtaSeconds, Is.EqualTo(1f).Within(0.01f));
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            ThreatWarningRuntimeState.Reset();
        }
    }

    [Test]
    public void ThreatDetectionWarningSystem_CompletesPendingUnitGridWriterBeforeChunkRead()
    {
        using var world = new World("ThreatDetectionWarningSystem_PendingUnitGridWriter");
        EntityManager em = world.EntityManager;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeBitArray occupied = default;
        NativeList<int2> pathPool = default;

        try
        {
            const int width = 48;
            const int height = 48;
            int gridSize = width * height;
            blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
            blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
            for (int i = 0; i < friendlyPassFactionIds.Length; i++)
                friendlyPassFactionIds[i] = byte.MaxValue;
            occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            pathPool = new NativeList<int2>(Allocator.Persistent);
            pathPool.Add(new int2(2, 1));
            pathPool.Add(new int2(3, 1));

            var grid = new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
            Entity gridEntity = em.CreateEntity(
                typeof(GridConfig),
                typeof(DynamicBlockerComponent),
                typeof(DynamicOccupancyComponent),
                typeof(PathPoolComponent));
            em.SetComponentData(gridEntity, grid);
            em.SetComponentData(gridEntity, new DynamicBlockerComponent
            {
                GridSize = gridSize,
                Counts = blockerCounts,
                Blocked = blocked,
                FriendlyPassFactionIds = friendlyPassFactionIds
            });
            em.SetComponentData(gridEntity, new DynamicOccupancyComponent
            {
                GridSize = gridSize,
                Occupied = occupied
            });
            em.SetComponentData(gridEntity, new PathPoolComponent { Cells = pathPool });

            em.AddBuffer<GridWalkable>(gridEntity);
            em.AddBuffer<GridRoad>(gridEntity);
            em.AddBuffer<GridRoadSidewalk>(gridEntity);
            em.AddBuffer<GridRoadDirt>(gridEntity);
            DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
            DynamicBuffer<GridRoad> roads = em.GetBuffer<GridRoad>(gridEntity);
            DynamicBuffer<GridRoadSidewalk> sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity);
            DynamicBuffer<GridRoadDirt> dirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity);
            walkable.ResizeUninitialized(gridSize);
            roads.ResizeUninitialized(gridSize);
            sidewalks.ResizeUninitialized(gridSize);
            dirtRoads.ResizeUninitialized(gridSize);
            for (int i = 0; i < gridSize; i++)
            {
                walkable[i] = new GridWalkable { Value = 1 };
                roads[i] = new GridRoad { Value = 0 };
                sidewalks[i] = new GridRoadSidewalk { Value = 0 };
                dirtRoads[i] = new GridRoadDirt { Value = 0 };
            }

            Entity movingUnit = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitMove),
                typeof(UnitMovementBehavior),
                typeof(UnitVehicleMovement),
                typeof(UnitVehicleKinematics),
                typeof(UnitPathFollow),
                typeof(UnitPathRange),
                typeof(LocalTransform));
            em.SetComponentData(movingUnit, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(movingUnit, new UnitGrid { Cell = new int2(1, 1) });
            em.SetComponentData(movingUnit, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(movingUnit, new UnitMove { Speed = 2f, WalkSpeed = 2f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.01f });
            em.SetComponentData(movingUnit, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
            em.SetComponentData(movingUnit, new UnitVehicleMovement());
            em.SetComponentData(movingUnit, new UnitVehicleKinematics());
            em.SetComponentData(movingUnit, new UnitPathFollow { PathIndex = 0 });
            em.SetComponentData(movingUnit, new UnitPathRange { Start = 0, Length = pathPool.Length });
            em.SetComponentData(movingUnit, LocalTransform.FromPosition(new float3(1.5f, 0f, 1.5f)));

            Entity sensor = CreateUnit(em, FactionIdentity.PlayerFactionId, new int2(20, 20), false, 100, 0f);
            em.AddComponentData(sensor, new ThreatDetector
            {
                Kind = (byte)ThreatDetectionKind.Ground,
                RadiusCells = 20
            });
            CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(30, 20), false, 100, 5f, true, new int2(20, 20));

            world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            SystemHandle movementSystem = world.CreateSystem<UnitGridMovementSystem>();
            SystemHandle threatSystem = world.CreateSystem<ThreatDetectionWarningSystem>();

            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            ThreatWarningRuntimeState.Reset();

            world.SetTime(new TimeData(0.1d, 0.1f));
            movementSystem.Update(world.Unmanaged);
            Assert.DoesNotThrow(() => threatSystem.Update(world.Unmanaged));
            Assert.IsTrue(ThreatWarningRuntimeState.HasPendingWarning);
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            ThreatWarningRuntimeState.Reset();
            if (pathPool.IsCreated)
                pathPool.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    private static Entity CreateUnit(
        EntityManager em,
        byte factionId,
        int2 cell,
        bool air,
        int health,
        float speed,
        bool groundVehicle = false,
        int2? targetCell = null)
    {
        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new Faction { Id = factionId });
        em.AddComponentData(entity, new UnitGrid { Cell = cell });
        em.AddComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.AddComponentData(entity, new UnitMovementBehavior
        {
            AllowIdleWander = 0,
            UsesVehicleMotion = (byte)(groundVehicle ? 1 : 0)
        });
        if (speed > 0f)
        {
            em.AddComponentData(entity, new UnitMove
            {
                Speed = speed,
                WalkSpeed = speed,
                RoadSpeedMultiplier = 1f,
                ArriveDistance = 0.05f
            });
        }
        if (targetCell.HasValue)
            em.AddComponentData(entity, new UnitTarget { Cell = targetCell.Value });
        if (air)
        {
            em.AddComponentData(entity, new UnitAirMovement
            {
                CruiseHeight = 6f,
                RunwayTaxiSpeed = 5f
            });
        }

        return entity;
    }
}
