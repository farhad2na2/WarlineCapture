using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;
using Game.Runtime;

public sealed class InitialUnitsSpawnFocusedTests
{
    public static void RunResourceBuildingSourceKeyBatchValidation()
    {
        string[] methodNames =
        {
            nameof(InitialUnitsSpawnSystem_CreatesPlayerEconomyWithConfiguredDollars),
            nameof(InitialUnitsSpawnSystem_AppliesConfiguredInitialFuelToPlayerUsableStorage),
            nameof(InitialUnitsSpawnSystem_ClampsConfiguredInitialFuelToPlayerUsableStorageCapacity),
            nameof(InitialUnitsSpawnSystem_DoesNotMarkConfiguredInitialFuelAppliedBeforeStorageExists),
            nameof(InitialUnitsSpawnSystem_AllowsConfiguredInitialFuelBeforeInitialBuildingsComplete),
            nameof(InitialUnitsSpawnSystem_QueuesConfiguredRuntimeSpawnRequestFromReadModel),
            nameof(InitialUnitsSpawnSystem_QueuesConfiguredKeyOnlyRuntimeSpawnRequestFromLegacyConfig),
            nameof(InitialUnitsSpawnSystem_QueuesFactionBaseRuntimeSpawnRequests),
            nameof(InitialUnitsSpawnSystem_SkipsFactionBaseRequestsOutsideGrid),
            nameof(InitialUnitsSpawnSystem_SkipsUnresolvedSourceKeyWithoutFallback),
            nameof(InitialUnitSpawnApplySystem_InstantiatesConvertedPrefabBackedUnit)
        };

        try
        {
            var tests = new InitialUnitsSpawnFocusedTests();
            Type testType = typeof(InitialUnitsSpawnFocusedTests);
            for (int i = 0; i < methodNames.Length; i++)
            {
                System.Reflection.MethodInfo method = testType.GetMethod(methodNames[i]);
                Assert.NotNull(method, $"Missing initial units focused validation method {methodNames[i]}.");
                method.Invoke(tests, null);
            }

            UnityEngine.Debug.Log($"[InitialUnitsSpawnFocusedValidation] result=Passed group=ResourceBuildingSourceKey methods={methodNames.Length}");
            ValidationExit.Exit(0);
        }
        catch (Exception ex)
        {
            Exception failure = ex is System.Reflection.TargetInvocationException && ex.InnerException != null
                ? ex.InnerException
                : ex;
            UnityEngine.Debug.LogException(failure);
            UnityEngine.Debug.LogError("[InitialUnitsSpawnFocusedValidation] result=Failed group=ResourceBuildingSourceKey");
            ValidationExit.Exit(1);
        }
    }

    public static void RunSpawnProgressCompletionBatchValidation()
    {
        string[] methodNames =
        {
            nameof(InitialUnitsSpawnSystem_InitializesProgressAndUnitEntries),
            nameof(InitialUnitsSpawnSystem_ClampsEntryToInitialSpawnBatchSize),
            nameof(InitialUnitsSpawnSystem_RejectsReservedFootprint),
            nameof(InitialUnitsSpawnSystem_ResolvesConfiguredHelipadSlot),
            nameof(InitialUnitsSpawnSystem_PreservesBlockerProgressIncrement),
            nameof(InitialUnitsSpawnSystem_WaitsForInitialBuildingCompletionWithoutFailOpen),
            nameof(InitialUnitsSpawnSystem_QueuesPendingFuelSeedWhenStorageIsUnavailableAtCompletion),
            nameof(InitialUnitsSpawnSystem_AppliesPendingFuelSeedWhenStorageAppears)
        };

        try
        {
            var tests = new InitialUnitsSpawnFocusedTests();
            Type testType = typeof(InitialUnitsSpawnFocusedTests);
            for (int i = 0; i < methodNames.Length; i++)
            {
                System.Reflection.MethodInfo method = testType.GetMethod(methodNames[i]);
                Assert.NotNull(method, $"Missing initial units focused validation method {methodNames[i]}.");
                method.Invoke(tests, null);
            }

            UnityEngine.Debug.Log($"[InitialUnitsSpawnFocusedValidation] result=Passed group=SpawnProgressCompletion methods={methodNames.Length}");
            ValidationExit.Exit(0);
        }
        catch (Exception ex)
        {
            Exception failure = ex is System.Reflection.TargetInvocationException && ex.InnerException != null
                ? ex.InnerException
                : ex;
            UnityEngine.Debug.LogException(failure);
            UnityEngine.Debug.LogError("[InitialUnitsSpawnFocusedValidation] result=Failed group=SpawnProgressCompletion");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void InitialUnitsSpawnSystem_CreatesPlayerEconomyWithConfiguredDollars()
    {
        using var world = new World("InitialUnitsSpawnResourceTest");
        EntityManager em = world.EntityManager;

        InitialUnitsSpawnSystem.ApplyInitialResourceTotals(
            em,
            new InitialUnitsSpawnConfig { InitialDollars = 345 });

        using EntityQuery economyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
        using NativeArray<Entity> economies = economyQuery.ToEntityArray(Allocator.Temp);
        Assert.AreEqual(1, economies.Length);
        FactionEconomy economy = em.GetComponentData<FactionEconomy>(economies[0]);
        Assert.AreEqual(FactionIdentity.PlayerFactionId, economy.FactionId);
        Assert.AreEqual(345, economy.Money);

        using EntityQuery policyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomyPolicy>());
        Assert.IsFalse(policyQuery.IsEmptyIgnoreFilter);
        FactionEconomyPolicy policy = em.GetComponentData<FactionEconomyPolicy>(policyQuery.GetSingletonEntity());
        Assert.AreEqual(0, policy.Enabled);
    }

    [Test]
    public void InitialUnitsSpawnSystem_AppliesConfiguredInitialFuelToPlayerUsableStorage()
    {
        using var world = new World("InitialUnitsSpawnFuelStorageTest");
        EntityManager em = world.EntityManager;

        Entity playerStorage = CreateFuelStorage(em, FactionIdentity.PlayerFactionId, capacity: 1000, storedFuel: 12f);
        Entity enemyStorage = CreateFuelStorage(em, FactionIdentity.EnemyFactionId, capacity: 1000, storedFuel: 7f);
        Entity playerRefinery = CreateFuelStorage(em, FactionIdentity.PlayerFactionId, capacity: 1000, storedFuel: 9f, fuelBarrelsPerDay: 10f);

        float addedFuel = InitialUnitsSpawnSystem.ApplyInitialUsableFuelStorage(em, initialFuel: 500);

        Assert.AreEqual(488f, addedFuel, 0.001f);
        BuildingResourceStorageComponent playerStorageAfter =
            em.GetComponentData<BuildingResourceStorageComponent>(playerStorage);
        Assert.AreEqual(500f, playerStorageAfter.StoredFuelBarrels, 0.001f);
        Assert.AreEqual(1u, playerStorageAfter.Version);
        Assert.AreEqual(7f, em.GetComponentData<BuildingResourceStorageComponent>(enemyStorage).StoredFuelBarrels, 0.001f);
        Assert.AreEqual(9f, em.GetComponentData<BuildingResourceStorageComponent>(playerRefinery).StoredFuelBarrels, 0.001f);
    }

    [Test]
    public void InitialUnitsSpawnSystem_ClampsConfiguredInitialFuelToPlayerUsableStorageCapacity()
    {
        using var world = new World("InitialUnitsSpawnFuelStorageCapacityTest");
        EntityManager em = world.EntityManager;

        Entity smallStorage = CreateFuelStorage(em, FactionIdentity.PlayerFactionId, capacity: 40, storedFuel: 10f);
        Entity secondStorage = CreateFuelStorage(em, FactionIdentity.PlayerFactionId, capacity: 25, storedFuel: 5f);

        float addedFuel = InitialUnitsSpawnSystem.ApplyInitialUsableFuelStorage(em, initialFuel: 500);

        Assert.AreEqual(50f, addedFuel, 0.001f);
        Assert.AreEqual(40f, em.GetComponentData<BuildingResourceStorageComponent>(smallStorage).StoredFuelBarrels, 0.001f);
        Assert.AreEqual(25f, em.GetComponentData<BuildingResourceStorageComponent>(secondStorage).StoredFuelBarrels, 0.001f);
    }

    [Test]
    public void InitialUnitsSpawnSystem_DoesNotMarkConfiguredInitialFuelAppliedBeforeStorageExists()
    {
        using var world = new World("InitialUnitsSpawnFuelStorageWaitTest");
        EntityManager em = world.EntityManager;

        bool appliedWithoutStorage = InitialUnitsSpawnSystem.TryApplyInitialUsableFuelStorage(
            em,
            initialFuel: 10000,
            out float addedWithoutStorage);

        Assert.IsFalse(appliedWithoutStorage);
        Assert.AreEqual(0f, addedWithoutStorage, 0.001f);

        Entity storage = CreateFuelStorage(em, FactionIdentity.PlayerFactionId, capacity: 10000, storedFuel: 0f);
        bool appliedWithStorage = InitialUnitsSpawnSystem.TryApplyInitialUsableFuelStorage(
            em,
            initialFuel: 10000,
            out float addedWithStorage);

        Assert.IsTrue(appliedWithStorage);
        Assert.AreEqual(10000f, addedWithStorage, 0.001f);
        Assert.AreEqual(10000f, em.GetComponentData<BuildingResourceStorageComponent>(storage).StoredFuelBarrels, 0.001f);
    }

    [Test]
    public void InitialUnitsSpawnSystem_AllowsConfiguredInitialFuelBeforeInitialBuildingsComplete()
    {
        using var world = new World("InitialUnitsSpawnFuelStorageBeforeBuildingsTest");
        EntityManager em = world.EntityManager;
        CreateFuelStorage(em, FactionIdentity.PlayerFactionId, capacity: 10000, storedFuel: 0f);

        bool canApply = InitialUnitsSpawnSystem.CanApplyInitialFuelStorage(
            em,
            new InitialUnitsSpawnConfig
            {
                InitialFuel = 10000
            });

        Assert.IsTrue(canApply);
    }

    [Test]
    public void InitialUnitsSpawnSystem_QueuesConfiguredRuntimeSpawnRequestFromReadModel()
    {
        using var world = new World("InitialUnitsSpawnConfiguredBuildingRequestTest");
        EntityManager em = world.EntityManager;
        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
        em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> readModels =
            em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
        readModels.Add(new BuildingConfiguredSpawnableReadModel
        {
            BuildingId = new FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_Tent")),
            DisplayName = new FixedString128Bytes("Tent"),
            FootprintCells = new int2(2, 2),
            CanRequest = 1
        });

        Entity configEntity = em.CreateEntity();
        DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> buildingSpawns =
            em.AddBuffer<InitialUnitsFactionBuildingSpawnEntry>(configEntity);
        Entity prefab = em.CreateEntity();
        buildingSpawns.Add(new InitialUnitsFactionBuildingSpawnEntry
        {
            FactionId = 1,
            Prefab = prefab,
            PrefabLookupKey = new FixedString128Bytes("Building_Tent"),
            OriginOffset = new int2(3, 4)
        });

        var factionSpawns = new NativeArray<InitialUnitsFactionSpawnEntry>(1, Allocator.Temp);
        try
        {
            factionSpawns[0] = new InitialUnitsFactionSpawnEntry { FactionId = 1, SpawnCell = new int2(10, 20) };
            var diagnosticLogWriter = new InitialUnitsSpawnSystem.InitialSpawnDiagnosticLogWriter();
            diagnosticLogWriter.EnsureQueue(em);

            bool issued = InitialUnitsSpawnSystem.EnqueueConfiguredInitialBuildingRequests(
                em,
                boundary,
                configEntity,
                factionSpawns,
                ref diagnosticLogWriter,
                out int requestCount);

            Assert.IsTrue(issued);
            Assert.AreEqual(1, requestCount);
            DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
                em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            Assert.AreEqual(1, requests.Length);
            BuildingRuntimeSpawnRequest request = requests[0];
            Assert.AreEqual(BuildingRuntimeSpawnRequest.KindBuilding, request.RequestKind);
            Assert.AreEqual(1, request.FactionId);
            Assert.AreEqual(new int2(13, 24), request.PreferredOrigin);
            Assert.AreEqual(configEntity, request.PlanEntity);
            Assert.AreEqual(BuildingRuntimeSpawnRequest.Pending, request.Status);
        }
        finally
        {
            factionSpawns.Dispose();
        }
    }

    [Test]
    public void InitialUnitsSpawnSystem_QueuesConfiguredKeyOnlyRuntimeSpawnRequestFromLegacyConfig()
    {
        using var world = new World("InitialUnitsSpawnConfiguredBuildingRequestKeyOnlyTest");
        EntityManager em = world.EntityManager;
        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
        em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> readModels =
            em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
        readModels.Add(new BuildingConfiguredSpawnableReadModel
        {
            BuildingId = new FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Tent_Regular")),
            DisplayName = new FixedString128Bytes("Tent"),
            FootprintCells = new int2(4, 4),
            CanRequest = 1
        });

        Entity configEntity = em.CreateEntity();
        DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> buildingSpawns =
            em.AddBuffer<InitialUnitsFactionBuildingSpawnEntry>(configEntity);
        buildingSpawns.Add(new InitialUnitsFactionBuildingSpawnEntry
        {
            FactionId = 2,
            Prefab = Entity.Null,
            PrefabLookupKey = new FixedString128Bytes("Tent_Regular"),
            OriginOffset = new int2(250, 250)
        });

        var factionSpawns = new NativeArray<InitialUnitsFactionSpawnEntry>(1, Allocator.Temp);
        try
        {
            factionSpawns[0] = new InitialUnitsFactionSpawnEntry { FactionId = 2, SpawnCell = new int2(150, 250) };
            var diagnosticLogWriter = new InitialUnitsSpawnSystem.InitialSpawnDiagnosticLogWriter();
            diagnosticLogWriter.EnsureQueue(em);

            bool issued = InitialUnitsSpawnSystem.EnqueueConfiguredInitialBuildingRequests(
                em,
                boundary,
                configEntity,
                factionSpawns,
                ref diagnosticLogWriter,
                out int requestCount);

            Assert.IsTrue(issued);
            Assert.AreEqual(1, requestCount);
            DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
                em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            Assert.AreEqual(1, requests.Length);
            BuildingRuntimeSpawnRequest request = requests[0];
            Assert.AreEqual(BuildingRuntimeSpawnRequest.KindBuilding, request.RequestKind);
            Assert.AreEqual(2, request.FactionId);
            Assert.AreEqual(new int2(400, 500), request.PreferredOrigin);
            Assert.AreEqual(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Tent_Regular"), request.BuildingId.ToString());
            Assert.AreEqual(configEntity, request.PlanEntity);
            Assert.AreEqual(BuildingRuntimeSpawnRequest.Pending, request.Status);
        }
        finally
        {
            factionSpawns.Dispose();
        }
    }

    [Test]
    public void InitialUnitsSpawnSystem_QueuesFactionBaseRuntimeSpawnRequests()
    {
        using var world = new World("InitialUnitsSpawnFactionBaseRequestTest");
        EntityManager em = world.EntityManager;
        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
        em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> readModels =
            em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
        AddInitialBaseSpawnableReadModel(readModels, "Wall_Dirt_Straight", new int2(4, 2));
        AddInitialBaseSpawnableReadModel(readModels, "Building_Road_Barrier", new int2(10, 4));
        for (int i = 0; i < InitialFactionBaseLayoutPlanner.RequiredBuildingKeys.Length; i++)
            AddInitialBaseSpawnableReadModel(readModels, InitialFactionBaseLayoutPlanner.RequiredBuildingKeys[i], new int2(6, 6));
        for (int i = 0; i < InitialFactionBaseLayoutPlanner.TentKeys.Length; i++)
            AddInitialBaseSpawnableReadModel(readModels, InitialFactionBaseLayoutPlanner.TentKeys[i], new int2(3, 3));

        Entity configEntity = em.CreateEntity();
        var factionSpawns = new NativeArray<InitialUnitsFactionSpawnEntry>(1, Allocator.Temp);
        try
        {
            factionSpawns[0] = new InitialUnitsFactionSpawnEntry
            {
                FactionId = FactionIdentity.PlayerFactionId,
                SpawnCell = new int2(1000, 2000)
            };

            const int coreRequestEntryIndex = -77;
            bool issued = InitialUnitsSpawnSystem.EnqueueInitialFactionBaseRequests(
                em,
                boundary,
                configEntity,
                new InitialUnitsSpawnConfig
                {
                    CreateFactionBases = 1,
                    BaseCoreBuildingPrefabLookupKey = new FixedString128Bytes("Building_Ammunition_Depot"),
                    BaseHalfWidthCells = 120,
                    BaseHalfHeightCells = 80
                },
                factionSpawns,
                new GridConfig { Width = 2400, Height = 3200, CellSize = 1f },
                coreRequestEntryIndex,
                out int requestCount);

            Assert.IsTrue(issued);
            DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
                em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            Assert.AreEqual(requests.Length, requestCount);
            Assert.Greater(requests.Length, 0);

            bool sawWallRunSegment = false;
            bool sawGateFlankSegment = false;
            bool sawGateBuilding = false;
            bool sawPlayerCore = false;
            string wallId = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Wall_Dirt_Straight");
            string gateId = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_Road_Barrier");
            string coreId = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_Ammunition_Depot");
            for (int i = 0; i < requests.Length; i++)
            {
                BuildingRuntimeSpawnRequest request = requests[i];
                Assert.AreEqual(i + 1, request.RequestId);
                Assert.AreEqual(configEntity, request.PlanEntity);
                Assert.AreEqual(FactionIdentity.PlayerFactionId, request.FactionId);
                Assert.AreEqual(1, request.HasOwnerFaction);
                if (request.RequestKind == BuildingRuntimeSpawnRequest.KindWallSegment &&
                    request.BuildingId.ToString() == wallId &&
                    request.AllowExistingWallOverlap == 0)
                {
                    sawWallRunSegment = true;
                }

                if (request.RequestKind == BuildingRuntimeSpawnRequest.KindWallSegment &&
                    request.BuildingId.ToString() == wallId &&
                    request.AllowExistingWallOverlap == 1)
                {
                    sawGateFlankSegment = true;
                }

                if (request.RequestKind == BuildingRuntimeSpawnRequest.KindBuilding &&
                    request.BuildingId.ToString() == gateId)
                {
                    sawGateBuilding = true;
                }

                if (request.RequestKind == BuildingRuntimeSpawnRequest.KindBuilding &&
                    request.BuildingId.ToString() == coreId &&
                    request.EntryIndex == coreRequestEntryIndex)
                {
                    sawPlayerCore = true;
                }
            }

            Assert.IsTrue(sawWallRunSegment);
            Assert.IsTrue(sawGateFlankSegment);
            Assert.IsTrue(sawGateBuilding);
            Assert.IsTrue(sawPlayerCore);
        }
        finally
        {
            factionSpawns.Dispose();
        }
    }

    [Test]
    public void InitialUnitsSpawnSystem_SkipsFactionBaseRequestsOutsideGrid()
    {
        using var world = new World("InitialUnitsSpawnFactionBaseBoundsRequestTest");
        EntityManager em = world.EntityManager;
        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
        em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> readModels =
            em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
        AddInitialBaseSpawnableReadModel(readModels, "Wall_Dirt_Straight", new int2(4, 2));
        AddInitialBaseSpawnableReadModel(readModels, "Building_Road_Barrier", new int2(10, 4));
        for (int i = 0; i < InitialFactionBaseLayoutPlanner.RequiredBuildingKeys.Length; i++)
            AddInitialBaseSpawnableReadModel(readModels, InitialFactionBaseLayoutPlanner.RequiredBuildingKeys[i], new int2(6, 6));
        for (int i = 0; i < InitialFactionBaseLayoutPlanner.TentKeys.Length; i++)
            AddInitialBaseSpawnableReadModel(readModels, InitialFactionBaseLayoutPlanner.TentKeys[i], new int2(3, 3));

        Entity configEntity = em.CreateEntity();
        var factionSpawns = new NativeArray<InitialUnitsFactionSpawnEntry>(1, Allocator.Temp);
        try
        {
            factionSpawns[0] = new InitialUnitsFactionSpawnEntry
            {
                FactionId = FactionIdentity.EnemyFactionId,
                SpawnCell = new int2(150, 50)
            };

            var grid = new GridConfig { Width = 512, Height = 512, CellSize = 1f };
            bool issued = InitialUnitsSpawnSystem.EnqueueInitialFactionBaseRequests(
                em,
                boundary,
                configEntity,
                new InitialUnitsSpawnConfig
                {
                    CreateFactionBases = 1,
                    BaseCoreBuildingPrefabLookupKey = new FixedString128Bytes("Building_Ammunition_Depot"),
                    BaseHalfWidthCells = 120,
                    BaseHalfHeightCells = 80
                },
                factionSpawns,
                grid,
                -77,
                out int requestCount);

            Assert.IsTrue(issued);
            DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
                em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            Assert.AreEqual(requests.Length, requestCount);
            Assert.Greater(requests.Length, 0);

            for (int i = 0; i < requests.Length; i++)
            {
                BuildingRuntimeSpawnRequest request = requests[i];
                Vector2Int footprint = ResolveInitialBaseRequestFootprint(request);
                Vector2Int origin = new(request.PreferredOrigin.x, request.PreferredOrigin.y);
                Assert.IsTrue(
                    InitialFactionBaseLayoutPlanner.IsFootprintInsideGrid(origin, footprint, grid.Width, grid.Height),
                    $"Generated request {i} should stay inside grid. kind={request.RequestKind} id={request.BuildingId.ToString()} origin={origin} footprint={footprint}");
            }
        }
        finally
        {
            factionSpawns.Dispose();
        }
    }

    private static void AddInitialBaseSpawnableReadModel(
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> readModels,
        string buildingId,
        int2 footprint)
    {
        readModels.Add(new BuildingConfiguredSpawnableReadModel
        {
            BuildingId = new FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId)),
            DisplayName = new FixedString128Bytes(buildingId),
            FootprintCells = footprint,
            CanRequest = 1
        });
    }

    private static Vector2Int ResolveInitialBaseRequestFootprint(BuildingRuntimeSpawnRequest request)
    {
        string buildingId = request.BuildingId.ToString();
        Vector2Int footprint = buildingId == BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Wall_Dirt_Straight")
            ? new Vector2Int(4, 2)
            : buildingId == BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_Road_Barrier")
                ? new Vector2Int(10, 4)
                : new Vector2Int(6, 6);
        return request.RotateVertical != 0
            ? new Vector2Int(footprint.y, footprint.x)
            : footprint;
    }

    [Test]
    public void InitialUnitsSpawnSystem_SkipsUnresolvedSourceKeyWithoutFallback()
    {
        using var world = new World("InitialUnitsSpawnSourceKeyTest");
        EntityManager em = world.EntityManager;
        var diagnosticLogWriter = new InitialUnitsSpawnSystem.InitialSpawnDiagnosticLogWriter();
        diagnosticLogWriter.EnsureQueue(em);
        Entity sourceEntity = em.CreateEntity();
        DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> sourceSpawns =
            em.AddBuffer<CustomGameFactionUnitSourceSpawnEntry>(sourceEntity);
        sourceSpawns.Add(new CustomGameFactionUnitSourceSpawnEntry
        {
            FactionId = 1,
            SourceKey = new FixedString64Bytes("rifleman"),
            Count = 3,
            SpawnOffset = new int2(2, 5)
        });

        InitialUnitsFactionUnitSpawnEntry unitSpawn = new()
        {
            FactionId = 1,
            Prefab = Entity.Null,
            Count = 3,
            SpawnOffset = new int2(2, 5)
        };
        InitialUnitsFactionUnitSpawnProgress progress = default;
        Assert.IsTrue(InitialUnitsSpawnSystem.TryGetCustomGameUnitSourceKey(sourceSpawns, true, 0, unitSpawn, out FixedString64Bytes sourceKey));
        Assert.AreEqual("rifleman", sourceKey.ToString());
        Assert.IsTrue(InitialUnitsSpawnSystem.TrySkipMissingPrefabUnit(
            em,
            unitSpawn,
            hasPrefab: false,
            hasSourceKey: true,
            sourceKey,
            ref progress,
            ref diagnosticLogWriter));
        Assert.AreEqual(3, progress.Spawned);

        using EntityQuery logQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialSpawnDiagnosticLogComponent>());
        Entity logEntity = logQuery.GetSingletonEntity();
        DynamicBuffer<InitialSpawnDiagnosticLogComponent> logs =
            em.GetBuffer<InitialSpawnDiagnosticLogComponent>(logEntity);
        Assert.AreEqual(1, logs.Length);
        Assert.AreEqual(InitialSpawnDiagnosticLogComponent.WarningSeverity, logs[0].Severity);
        StringAssert.Contains("sourceKey=rifleman", logs[0].Message.ToString());
    }

    [Test]
    public void InitialUnitSpawnApplySystem_InstantiatesConvertedPrefabBackedUnit()
    {
        using var world = new World("InitialUnitSpawnApplySystemTest");
        EntityManager em = world.EntityManager;
        Entity prefab = em.CreateEntity(
            typeof(UnitGrid),
            typeof(LocalTransform),
            typeof(UnitPrevWorldPos),
            typeof(UnitMoveVisualComponent),
            typeof(Faction),
            typeof(UnitRespawnPrefab),
            typeof(UnitSourcePrefabKey),
            typeof(UnitAttackCooldownComponent));
        em.SetComponentData(prefab, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Plane_Transport") });
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        _ = new InitialUnitSpawnApplySystem().InstantiateAndConfigureSpawnedUnit(
            em,
            ecb,
            prefab,
            hasPrefab: true,
            faction: 1,
            cell: new int2(4, 7),
            pos: new float3(4.5f, 0f, 7.5f));
        ecb.Playback(em);
        ecb.Dispose();

        using EntityQuery factionQuery = em.CreateEntityQuery(ComponentType.ReadOnly<Faction>());
        using NativeArray<Entity> entities = factionQuery.ToEntityArray(Allocator.Temp);
        Entity spawned = Entity.Null;
        for (int i = 0; i < entities.Length; i++)
        {
            Faction faction = em.GetComponentData<Faction>(entities[i]);
            if (faction.Id == 1)
            {
                spawned = entities[i];
                break;
            }
        }

        Assert.AreNotEqual(Entity.Null, spawned);
        Assert.AreEqual(new int2(4, 7), em.GetComponentData<UnitGrid>(spawned).Cell);
        Assert.AreEqual(1, em.GetComponentData<Faction>(spawned).Id);
        Assert.AreEqual(new float3(4.5f, 0f, 7.5f), em.GetComponentData<UnitPrevWorldPos>(spawned).Value);
        Assert.AreEqual(0, em.GetComponentData<UnitMoveVisualComponent>(spawned).IsMoving);
        Assert.AreEqual(new FixedString64Bytes("Unit_Veh_Plane_Transport"), em.GetComponentData<UnitSourcePrefabKey>(spawned).Value);
    }

    [Test]
    public void InitialUnitsSpawnSystem_InitializesProgressAndUnitEntries()
    {
        using var world = new World("InitialUnitsSpawnProgressTest");
        EntityManager em = world.EntityManager;
        Entity configEntity = em.CreateEntity(typeof(InitialUnitsSpawnConfig));
        em.SetComponentData(configEntity, new InitialUnitsSpawnConfig { RandomSeed = 0 });
        DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns =
            em.AddBuffer<InitialUnitsFactionUnitSpawnEntry>(configEntity);
        unitSpawns.Add(new InitialUnitsFactionUnitSpawnEntry { FactionId = 0, Count = 2 });
        unitSpawns.Add(new InitialUnitsFactionUnitSpawnEntry { FactionId = 1, Count = 3 });

        using EntityQuery pendingInitQuery = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<InitialUnitsSpawnInitialized>(),
                ComponentType.ReadOnly<InitialUnitsSpawnProgress>()
            }
        });

        InitialUnitsSpawnSystem.InitializeInitialSpawnProgress(em, pendingInitQuery);

        Assert.IsTrue(em.HasComponent<InitialUnitsSpawnProgress>(configEntity));
        InitialUnitsSpawnProgress progress = em.GetComponentData<InitialUnitsSpawnProgress>(configEntity);
        Assert.AreEqual(1u, progress.RandomState);
        Assert.AreEqual(0, progress.BlockersSpawned);
        Assert.AreEqual(0, progress.InitialResourcesApplied);
        Assert.AreEqual(0, progress.InitialFuelStorageApplied);

        DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> progressBuffer =
            em.GetBuffer<InitialUnitsFactionUnitSpawnProgress>(configEntity);
        Assert.AreEqual(2, progressBuffer.Length);
        Assert.AreEqual(0, progressBuffer[0].Spawned);
        Assert.AreEqual(0, progressBuffer[1].Spawned);
    }

    [Test]
    public void InitialUnitsSpawnSystem_ClampsEntryToInitialSpawnBatchSize()
    {
        using var world = new World("InitialUnitsSpawnBatchTest");
        EntityManager em = world.EntityManager;
        Entity configEntity = em.CreateEntity();
        Entity prefab = em.CreateEntity();
        DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns =
            em.AddBuffer<InitialUnitsFactionUnitSpawnEntry>(configEntity);
        unitSpawns.Add(new InitialUnitsFactionUnitSpawnEntry
        {
            FactionId = 1,
            Prefab = prefab,
            Count = 50
        });
        DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> unitProgress =
            em.AddBuffer<InitialUnitsFactionUnitSpawnProgress>(configEntity);
        unitProgress.Add(new InitialUnitsFactionUnitSpawnProgress { Spawned = 3 });
        unitSpawns = em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(configEntity);

        bool created = InitialUnitsSpawnSystem.TryCreateInitialUnitSpawnEntryBatch(
            unitSpawns,
            unitProgress,
            unitIndex: 0,
            remainingBatch: 24,
            out InitialUnitsSpawnSystem.InitialUnitSpawnEntryBatch batch);

        Assert.IsTrue(created);
        Assert.AreEqual(0, batch.UnitIndex);
        Assert.AreEqual(24, batch.ToSpawn);
        Assert.IsTrue(batch.HasPrefab);
        Assert.AreEqual(3, batch.EntryProgress.Spawned);
    }

    [Test]
    public void InitialUnitsSpawnSystem_RejectsReservedFootprint()
    {
        GridConfig grid = new()
        {
            Width = 1,
            Height = 1,
            CellSize = 1f,
            Origin = float3.zero
        };
        var walkable = new NativeArray<GridWalkable>(1, Allocator.Temp);
        var dynamicBlocked = new NativeBitArray(1, Allocator.Temp);
        var occupied = new NativeBitArray(1, Allocator.Temp);
        var reserved = new NativeBitArray(1, Allocator.Temp);
        try
        {
            walkable[0] = new GridWalkable { Value = 1 };
            reserved.Set(0, true);
            Unity.Mathematics.Random rng = new(1);

            bool found = InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(
                ref rng,
                grid,
                walkable,
                dynamicBlocked,
                occupied,
                ref reserved,
                center: int2.zero,
                radiusCells: 0,
                footprintSize: new int2(1, 1),
                isAirUnit: false,
                out int2 cell);

            Assert.IsFalse(found);
            Assert.AreEqual(int2.zero, cell);
        }
        finally
        {
            reserved.Dispose();
            occupied.Dispose();
            dynamicBlocked.Dispose();
            walkable.Dispose();
        }
    }

    [Test]
    public void InitialUnitsSpawnSystem_ResolvesConfiguredHelipadSlot()
    {
        using var world = new World("InitialUnitsSpawnAirPlatformTest");
        EntityManager em = world.EntityManager;
        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
            em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
        spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
        {
            FactionId = 1,
            BuildingId = new FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_Helipad")),
            SlotIndex = 0,
            Cell = new int2(7, 8),
            WorldPosition = new float3(7.5f, 0f, 8.5f)
        });

        GridConfig grid = new()
        {
            Width = 16,
            Height = 16,
            CellSize = 1f,
            Origin = float3.zero
        };

        bool found = InitialUnitsSpawnSystem.TryGetInitialAirPlatformSpawn(
            em,
            boundary,
            factionId: 1,
            configuredSpawnOffset: new int2(40, -50),
            grid,
            out int2 cell,
            out float3 position);

        Assert.IsTrue(found);
        Assert.AreEqual(new int2(7, 8), cell);
        Assert.AreEqual(new float3(7.5f, 0f, 8.5f), position);
    }

    [Test]
    public void InitialUnitsSpawnSystem_PreservesBlockerProgressIncrement()
    {
        using var world = new World("InitialUnitsSpawnBlockerTest");
        EntityManager em = world.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var diagnosticLogWriter = new InitialUnitsSpawnSystem.InitialSpawnDiagnosticLogWriter();
        diagnosticLogWriter.EnsureQueue(em);
        GridConfig grid = new()
        {
            Width = 1,
            Height = 1,
            CellSize = 1f,
            Origin = float3.zero
        };
        var walkable = new NativeArray<GridWalkable>(1, Allocator.Temp);
        var dynamicBlocked = new NativeBitArray(1, Allocator.Temp);
        var occupied = new NativeBitArray(1, Allocator.Temp);
        var reserved = new NativeBitArray(1, Allocator.Temp);
        try
        {
            walkable[0] = new GridWalkable { Value = 1 };
            Unity.Mathematics.Random rng = new(1);

            InitialUnitsSpawnSystem.InitialBlockerSpawnResult result = InitialUnitsSpawnSystem.SpawnInitialBlockerBatch(
                ref rng,
                em,
                ecb,
                new InitialUnitsSpawnConfig
                {
                    BlockerPrefab = Entity.Null,
                    BlockerCount = 5,
                    SpawnRadiusCells = 0
                },
                initialBlockerBatchSize: 2,
                blockersSpawned: 0,
                grid,
                walkable,
                dynamicBlocked,
                occupied,
                ref reserved,
                enableDiagnostics: false,
                ref diagnosticLogWriter);

            Assert.AreEqual(5, result.TargetCount);
            Assert.AreEqual(2, result.ProgressIncrement);
            Assert.AreEqual(0, result.SpawnedForLog);
        }
        finally
        {
            reserved.Dispose();
            occupied.Dispose();
            dynamicBlocked.Dispose();
            walkable.Dispose();
            ecb.Dispose();
        }
    }

    [Test]
    public void InitialUnitsSpawnSystem_WaitsForInitialBuildingCompletionWithoutFailOpen()
    {
        using var world = new World("InitialUnitsSpawnCompletionTest");
        EntityManager em = world.EntityManager;
        Entity configEntity = em.CreateEntity(typeof(InitialUnitsSpawnConfig), typeof(InitialUnitsSpawnProgress));
        em.SetComponentData(configEntity, new InitialUnitsSpawnConfig { CreateFactionBases = 1 });
        em.SetComponentData(configEntity, new InitialUnitsSpawnProgress());
        em.AddBuffer<InitialUnitsFactionUnitSpawnProgress>(configEntity);
        var diagnosticLogWriter = new InitialUnitsSpawnSystem.InitialSpawnDiagnosticLogWriter();
        diagnosticLogWriter.EnsureQueue(em);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        InitialUnitsSpawnProgress progress = em.GetComponentData<InitialUnitsSpawnProgress>(configEntity);
        try
        {
            bool completed = InitialUnitsSpawnSystem.UpdateInitialSpawnCompletion(
                em,
                ecb,
                configEntity,
                new InitialUnitsSpawnConfig { CreateFactionBases = 1 },
                ref progress,
                allUnitsSpawned: true,
                allBlockersSpawned: true,
                maxInitialBuildingCompletionWaitFrames: 2,
                ref diagnosticLogWriter,
                out bool progressChanged);

            Assert.IsFalse(completed);
            Assert.IsTrue(progressChanged);
            Assert.AreEqual(1, progress.InitialBuildingCompletionWaitFrames);
            Assert.AreEqual(0, progress.InitialBuildingsSpawned);

            completed = InitialUnitsSpawnSystem.UpdateInitialSpawnCompletion(
                em,
                ecb,
                configEntity,
                new InitialUnitsSpawnConfig { CreateFactionBases = 1 },
                ref progress,
                allUnitsSpawned: true,
                allBlockersSpawned: true,
                maxInitialBuildingCompletionWaitFrames: 2,
                ref diagnosticLogWriter,
                out progressChanged);

            Assert.IsFalse(completed);
            Assert.IsTrue(progressChanged);
            Assert.AreEqual(2, progress.InitialBuildingCompletionWaitFrames);
            Assert.AreEqual(0, progress.InitialBuildingsSpawned);

            using EntityQuery logQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialSpawnDiagnosticLogComponent>());
            Entity logEntity = logQuery.GetSingletonEntity();
            DynamicBuffer<InitialSpawnDiagnosticLogComponent> logs =
                em.GetBuffer<InitialSpawnDiagnosticLogComponent>(logEntity);
            Assert.AreEqual(2, logs.Length);
            Assert.AreEqual(InitialSpawnDiagnosticLogComponent.WarningSeverity, logs[0].Severity);
            Assert.AreEqual(InitialSpawnDiagnosticLogComponent.WarningSeverity, logs[1].Severity);
            StringAssert.Contains("waiting initial building completion", logs[0].Message.ToString());
            StringAssert.Contains("startup loading gate remains blocked", logs[1].Message.ToString());

            ecb.Playback(em);
            Assert.IsFalse(em.HasComponent<InitialUnitsSpawnInitialized>(configEntity));
            Assert.IsTrue(em.HasComponent<InitialUnitsSpawnProgress>(configEntity));
            Assert.IsTrue(em.HasBuffer<InitialUnitsFactionUnitSpawnProgress>(configEntity));
        }
        finally
        {
            ecb.Dispose();
        }
    }

    [Test]
    public void InitialUnitsSpawnSystem_QueuesPendingFuelSeedWhenStorageIsUnavailableAtCompletion()
    {
        using var world = new World("InitialUnitsSpawnPendingFuelCompletionTest");
        EntityManager em = world.EntityManager;
        Entity configEntity = em.CreateEntity(typeof(InitialUnitsSpawnConfig), typeof(InitialUnitsSpawnProgress));
        em.SetComponentData(configEntity, new InitialUnitsSpawnConfig { InitialFuel = 10000 });
        em.SetComponentData(configEntity, new InitialUnitsSpawnProgress { InitialBuildingsSpawned = 1 });
        em.AddBuffer<InitialUnitsFactionUnitSpawnProgress>(configEntity);

        var diagnosticLogWriter = new InitialUnitsSpawnSystem.InitialSpawnDiagnosticLogWriter();
        diagnosticLogWriter.EnsureQueue(em);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        InitialUnitsSpawnProgress progress = em.GetComponentData<InitialUnitsSpawnProgress>(configEntity);
        try
        {
            bool completed = InitialUnitsSpawnSystem.UpdateInitialSpawnCompletion(
                em,
                ecb,
                configEntity,
                new InitialUnitsSpawnConfig { InitialFuel = 10000 },
                ref progress,
                allUnitsSpawned: true,
                allBlockersSpawned: true,
                maxInitialBuildingCompletionWaitFrames: 2,
                ref diagnosticLogWriter,
                out bool progressChanged);

            Assert.IsTrue(completed);
            Assert.IsFalse(progressChanged);

            ecb.Playback(em);
            Assert.IsTrue(em.HasComponent<InitialUnitsSpawnInitialized>(configEntity));
            Assert.IsFalse(em.HasComponent<InitialUnitsSpawnProgress>(configEntity));
            Assert.IsFalse(em.HasBuffer<InitialUnitsFactionUnitSpawnProgress>(configEntity));
            Assert.IsTrue(em.HasComponent<InitialUsableFuelStorageSeedPending>(configEntity));
            Assert.AreEqual(10000, em.GetComponentData<InitialUsableFuelStorageSeedPending>(configEntity).InitialFuel);
        }
        finally
        {
            ecb.Dispose();
        }
    }

    [Test]
    public void InitialUnitsSpawnSystem_AppliesPendingFuelSeedWhenStorageAppears()
    {
        using var world = new World("InitialUnitsSpawnPendingFuelApplyTest");
        EntityManager em = world.EntityManager;
        Entity configEntity = em.CreateEntity(typeof(InitialUsableFuelStorageSeedPending));
        em.SetComponentData(configEntity, new InitialUsableFuelStorageSeedPending { InitialFuel = 10000 });
        using EntityQuery pendingFuelQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUsableFuelStorageSeedPending>());

        InitialUnitsSpawnSystem.ProcessPendingInitialFuelStorageSeeds(em, pendingFuelQuery);
        Assert.IsTrue(em.HasComponent<InitialUsableFuelStorageSeedPending>(configEntity));

        Entity storage = CreateFuelStorage(em, FactionIdentity.PlayerFactionId, capacity: 10000, storedFuel: 0f);
        InitialUnitsSpawnSystem.ProcessPendingInitialFuelStorageSeeds(em, pendingFuelQuery);

        Assert.IsFalse(em.HasComponent<InitialUsableFuelStorageSeedPending>(configEntity));
        Assert.AreEqual(10000f, em.GetComponentData<BuildingResourceStorageComponent>(storage).StoredFuelBarrels, 0.001f);
    }

    private static Entity CreateFuelStorage(
        EntityManager em,
        byte factionId,
        int capacity,
        float storedFuel,
        float fuelBarrelsPerDay = 0f)
    {
        Entity entity = em.CreateEntity(typeof(BuildingResourceStorageComponent));
        em.SetComponentData(entity, new BuildingResourceStorageComponent
        {
            OwnerFactionId = factionId,
            FuelStorageCapacity = capacity,
            FuelBarrelsPerDay = fuelBarrelsPerDay,
            StoredFuelBarrels = storedFuel
        });

        return entity;
    }
}
