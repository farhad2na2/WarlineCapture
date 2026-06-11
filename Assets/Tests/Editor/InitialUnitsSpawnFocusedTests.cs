using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class InitialUnitsSpawnFocusedTests
{
    public static void RunResourceBuildingSourceKeyBatchValidation()
    {
        string[] methodNames =
        {
            nameof(InitialSpawnResourceSystem_CreatesPlayerEconomyWithConfiguredDollars),
            nameof(InitialConfiguredBuildingRequestSystem_QueuesRuntimeSpawnRequestFromReadModel),
            nameof(InitialUnitSourceKeySystem_SkipsUnresolvedSourceKeyWithoutFallback),
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
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Exception failure = ex is System.Reflection.TargetInvocationException && ex.InnerException != null
                ? ex.InnerException
                : ex;
            UnityEngine.Debug.LogException(failure);
            UnityEngine.Debug.LogError("[InitialUnitsSpawnFocusedValidation] result=Failed group=ResourceBuildingSourceKey");
            UnityEditor.EditorApplication.Exit(1);
        }
    }

    public static void RunSpawnProgressCompletionBatchValidation()
    {
        string[] methodNames =
        {
            nameof(InitialUnitsSpawnProgressSystem_InitializesProgressAndUnitEntries),
            nameof(InitialUnitSpawnBatchSystem_ClampsEntryToInitialSpawnBatchSize),
            nameof(InitialUnitSpawnCellSystem_RejectsReservedFootprint),
            nameof(InitialAirPlatformSpawnSystem_ResolvesConfiguredHelipadSlot),
            nameof(InitialBlockerSpawnSystem_PreservesPlannedProgressIncrement),
            nameof(InitialSpawnCompletionSystem_WaitsThenFailOpens)
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
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Exception failure = ex is System.Reflection.TargetInvocationException && ex.InnerException != null
                ? ex.InnerException
                : ex;
            UnityEngine.Debug.LogException(failure);
            UnityEngine.Debug.LogError("[InitialUnitsSpawnFocusedValidation] result=Failed group=SpawnProgressCompletion");
            UnityEditor.EditorApplication.Exit(1);
        }
    }

    [Test]
    public void InitialSpawnResourceSystem_CreatesPlayerEconomyWithConfiguredDollars()
    {
        using var world = new World("InitialSpawnResourceSystemTest");
        EntityManager em = world.EntityManager;

        new InitialSpawnResourceSystem().ApplyInitialTotals(
            em,
            new InitialUnitsSpawnConfig { InitialDollars = 345 });

        using EntityQuery economyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
        using NativeArray<Entity> economies = economyQuery.ToEntityArray(Allocator.Temp);
        Assert.AreEqual(1, economies.Length);
        FactionEconomy economy = em.GetComponentData<FactionEconomy>(economies[0]);
        Assert.AreEqual(FactionIdentitySystem.PlayerFactionId, economy.FactionId);
        Assert.AreEqual(345, economy.Money);

        using EntityQuery policyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomyPolicy>());
        Assert.IsFalse(policyQuery.IsEmptyIgnoreFilter);
        FactionEconomyPolicy policy = em.GetComponentData<FactionEconomyPolicy>(policyQuery.GetSingletonEntity());
        Assert.AreEqual(0, policy.Enabled);
    }

    [Test]
    public void InitialConfiguredBuildingRequestSystem_QueuesRuntimeSpawnRequestFromReadModel()
    {
        using var world = new World("InitialConfiguredBuildingRequestSystemTest");
        EntityManager em = world.EntityManager;
        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeBoundaryTag));
        em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> readModels =
            em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
        readModels.Add(new BuildingConfiguredSpawnableReadModel
        {
            BuildingId = new FixedString128Bytes(BuildingDefinitionSystem.NormalizeSpawnableKey("Building_Tent")),
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
            var diagnosticLogSystem = new InitialSpawnDiagnosticLogSystem();
            diagnosticLogSystem.EnsureQueue(em);

            bool issued = new InitialConfiguredBuildingRequestSystem().Enqueue(
                em,
                boundary,
                configEntity,
                factionSpawns,
                ref diagnosticLogSystem,
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
    public void InitialUnitSourceKeySystem_SkipsUnresolvedSourceKeyWithoutFallback()
    {
        using var world = new World("InitialUnitSourceKeySystemTest");
        EntityManager em = world.EntityManager;
        var diagnosticLogSystem = new InitialSpawnDiagnosticLogSystem();
        diagnosticLogSystem.EnsureQueue(em);
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
        var sourceKeySystem = new InitialUnitSourceKeySystem();

        Assert.IsTrue(sourceKeySystem.TryGetCustomGameUnitSourceKey(sourceSpawns, true, 0, unitSpawn, out FixedString64Bytes sourceKey));
        Assert.AreEqual("rifleman", sourceKey.ToString());
        Assert.IsTrue(sourceKeySystem.TrySkipMissingPrefabUnit(
            em,
            unitSpawn,
            hasPrefab: false,
            hasSourceKey: true,
            sourceKey,
            ref progress,
            ref diagnosticLogSystem));
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
            typeof(UnitAttackCooldownComponent));
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
    }

    [Test]
    public void InitialUnitsSpawnProgressSystem_InitializesProgressAndUnitEntries()
    {
        using var world = new World("InitialUnitsSpawnProgressSystemTest");
        EntityManager em = world.EntityManager;
        Entity configEntity = em.CreateEntity(typeof(InitialUnitsSpawnConfig));
        em.SetComponentData(configEntity, new InitialUnitsSpawnConfig { RandomSeed = 0 });
        DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns =
            em.AddBuffer<InitialUnitsFactionUnitSpawnEntry>(configEntity);
        unitSpawns.Add(new InitialUnitsFactionUnitSpawnEntry { FactionId = 0, Count = 2 });
        unitSpawns.Add(new InitialUnitsFactionUnitSpawnEntry { FactionId = 1, Count = 3 });

        using EntityQuery dummyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
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
        var context = new InitialUnitsSpawnQuerySystem.Context(
            dummyQuery,
            dummyQuery,
            dummyQuery,
            pendingInitQuery,
            pendingInitQuery,
            dummyQuery);

        new InitialUnitsSpawnProgressSystem().InitializePending(em, context);

        Assert.IsTrue(em.HasComponent<InitialUnitsSpawnProgress>(configEntity));
        InitialUnitsSpawnProgress progress = em.GetComponentData<InitialUnitsSpawnProgress>(configEntity);
        Assert.AreEqual(1u, progress.RandomState);
        Assert.AreEqual(0, progress.BlockersSpawned);
        Assert.AreEqual(0, progress.InitialResourcesApplied);

        DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> progressBuffer =
            em.GetBuffer<InitialUnitsFactionUnitSpawnProgress>(configEntity);
        Assert.AreEqual(2, progressBuffer.Length);
        Assert.AreEqual(0, progressBuffer[0].Spawned);
        Assert.AreEqual(0, progressBuffer[1].Spawned);
    }

    [Test]
    public void InitialUnitSpawnBatchSystem_ClampsEntryToInitialSpawnBatchSize()
    {
        using var world = new World("InitialUnitSpawnBatchSystemTest");
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

        bool created = new InitialUnitSpawnBatchSystem().TryCreateEntryBatch(
            unitSpawns,
            unitProgress,
            unitIndex: 0,
            remainingBatch: 24,
            out InitialUnitSpawnBatchSystem.EntryBatch batch);

        Assert.IsTrue(created);
        Assert.AreEqual(0, batch.UnitIndex);
        Assert.AreEqual(24, batch.ToSpawn);
        Assert.IsTrue(batch.HasPrefab);
        Assert.AreEqual(3, batch.EntryProgress.Spawned);
    }

    [Test]
    public void InitialUnitSpawnCellSystem_RejectsReservedFootprint()
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

            bool found = new InitialUnitSpawnCellSystem().TryFindInitialUnitSpawnCell(
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
    public void InitialAirPlatformSpawnSystem_ResolvesConfiguredHelipadSlot()
    {
        using var world = new World("InitialAirPlatformSpawnSystemTest");
        EntityManager em = world.EntityManager;
        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeBoundaryTag));
        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
            em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
        spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
        {
            FactionId = 1,
            BuildingId = new FixedString128Bytes(BuildingDefinitionSystem.NormalizeSpawnableKey("Building_Helipad")),
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

        bool found = new InitialAirPlatformSpawnSystem().TryGetInitialAirPlatformSpawn(
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
    public void InitialBlockerSpawnSystem_PreservesPlannedProgressIncrement()
    {
        using var world = new World("InitialBlockerSpawnSystemTest");
        EntityManager em = world.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var diagnosticLogSystem = new InitialSpawnDiagnosticLogSystem();
        diagnosticLogSystem.EnsureQueue(em);
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

            InitialBlockerSpawnSystem.Result result = new InitialBlockerSpawnSystem().SpawnBatch(
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
                ref diagnosticLogSystem);

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
    public void InitialSpawnCompletionSystem_WaitsThenFailOpens()
    {
        using var world = new World("InitialSpawnCompletionSystemTest");
        EntityManager em = world.EntityManager;
        Entity configEntity = em.CreateEntity(typeof(InitialUnitsSpawnConfig), typeof(InitialUnitsSpawnProgress));
        em.SetComponentData(configEntity, new InitialUnitsSpawnConfig { CreateFactionBases = 1 });
        em.SetComponentData(configEntity, new InitialUnitsSpawnProgress());
        em.AddBuffer<InitialUnitsFactionUnitSpawnProgress>(configEntity);
        var diagnosticLogSystem = new InitialSpawnDiagnosticLogSystem();
        diagnosticLogSystem.EnsureQueue(em);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        InitialUnitsSpawnProgress progress = em.GetComponentData<InitialUnitsSpawnProgress>(configEntity);
        try
        {
            bool completed = new InitialSpawnCompletionSystem().Update(
                em,
                ecb,
                configEntity,
                new InitialUnitsSpawnConfig { CreateFactionBases = 1 },
                ref progress,
                allUnitsSpawned: true,
                allBlockersSpawned: true,
                maxInitialBuildingCompletionWaitFrames: 2,
                ref diagnosticLogSystem,
                out bool progressChanged);

            Assert.IsFalse(completed);
            Assert.IsTrue(progressChanged);
            Assert.AreEqual(1, progress.InitialBuildingCompletionWaitFrames);
            Assert.AreEqual(0, progress.InitialBuildingsSpawned);

            completed = new InitialSpawnCompletionSystem().Update(
                em,
                ecb,
                configEntity,
                new InitialUnitsSpawnConfig { CreateFactionBases = 1 },
                ref progress,
                allUnitsSpawned: true,
                allBlockersSpawned: true,
                maxInitialBuildingCompletionWaitFrames: 2,
                ref diagnosticLogSystem,
                out progressChanged);

            Assert.IsTrue(completed);
            Assert.IsTrue(progressChanged);
            Assert.AreEqual(2, progress.InitialBuildingCompletionWaitFrames);
            Assert.AreEqual(1, progress.InitialBuildingsSpawned);

            using EntityQuery logQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialSpawnDiagnosticLogComponent>());
            Entity logEntity = logQuery.GetSingletonEntity();
            DynamicBuffer<InitialSpawnDiagnosticLogComponent> logs =
                em.GetBuffer<InitialSpawnDiagnosticLogComponent>(logEntity);
            Assert.AreEqual(1, logs.Length);
            Assert.AreEqual(InitialSpawnDiagnosticLogComponent.WarningSeverity, logs[0].Severity);
            StringAssert.Contains("fail-open initial building completion", logs[0].Message.ToString());

            ecb.Playback(em);
            Assert.IsTrue(em.HasComponent<InitialUnitsSpawnInitialized>(configEntity));
            Assert.IsFalse(em.HasComponent<InitialUnitsSpawnProgress>(configEntity));
            Assert.IsFalse(em.HasBuffer<InitialUnitsFactionUnitSpawnProgress>(configEntity));
        }
        finally
        {
            ecb.Dispose();
        }
    }
}
