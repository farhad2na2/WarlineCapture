using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingUiQuerySystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new BuildingUiQuerySystemTests();
            tests.AddPendingProducedUnitEntries_AddsProgressCappedPendingEntries();
            tests.GetProducedUnits_PrunesDeadProducedUnits();
            tests.AddProducedUnitEntries_ResolvesReadyPrefabFromPassivePreviewDelegate();
            tests.SelectedBuildingProducedUnits_ReadsProducedUnitReadModel();
            tests.GetFriendlyPendingProductionUiEntries_IncludesPlayerOwnedProducerQueues();
            tests.GetFriendlyPendingProductionUiEntries_IncludesOperationMapProducerQueues();
            tests.SelectedMaterialFabricationReadModel_JoinsAuthoritativeEcsStateAndShapesProgress();
            tests.SelectedMaterialFabricationReadModel_RejectsMissingDuplicateAndMismatchedOwners();
            tests.SelectedMaterialFabricationReadModel_RejectsNonDepotSelection();
            tests.SelectedMaterialFabricationReadModel_AdvancesVersionOnlyWhenSourceStateChanges();
            tests.SelectedMaterialFabricationReadModel_UnchangedReadAllocatesNoManagedMemory();
            Debug.Log("[BuildingUiQueryValidation] result=Passed tests=11");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[BuildingUiQueryValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void AddPendingProducedUnitEntries_AddsProgressCappedPendingEntries()
    {
        var prefab = new GameObject("UnitPrefab");
        try
        {
            var pending = new TestPendingProduction
            {
                Prefab = prefab,
                StartedAt = 0f,
                ReadyAt = 10f,
                TransportPrefab = new GameObject("Transport")
            };
            try
            {
                var entries = new List<BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry>();

                var uiQuery = new BuildingUiQueryUiSystemHelper();
                uiQuery.AddPendingProducedUnitEntries(
                    new[] { pending },
                    new BuildingProductionQueueCompositionSystemHelper(),
                    9.9f,
                    entries);

                Assert.AreEqual(1, entries.Count);
                Assert.AreEqual(Entity.Null, entries[0].Unit);
                Assert.AreSame(prefab, entries[0].Prefab);
                Assert.IsFalse(entries[0].IsReady);
                Assert.AreEqual(0.97f, entries[0].Progress01, 0.0001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(pending.TransportPrefab);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void AddPendingProductionUiEntries_AddsRemainingDurationAndProgress()
    {
        var prefab = new GameObject("UnitPrefab");
        try
        {
            var pending = new TestPendingProduction
            {
                Prefab = prefab,
                StartedAt = 5f,
                ReadyAt = 15f
            };
            var entries = new List<BuildingUiQueryUiSystemHelper.PendingProductionUiEntry>();

            var uiQuery = new BuildingUiQueryUiSystemHelper();
            uiQuery.AddPendingProductionUiEntries(
                42,
                new[] { pending },
                new BuildingProductionQueueCompositionSystemHelper(),
                10f,
                entries);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(42, entries[0].BuildingId);
            Assert.AreSame(prefab, entries[0].Prefab);
            Assert.AreEqual(5f, entries[0].RemainingSeconds);
            Assert.AreEqual(10f, entries[0].DurationSeconds);
            Assert.AreEqual(0.5f, entries[0].Progress01);
            Assert.AreEqual(5f, entries[0].StartedAt);
            Assert.AreEqual(15f, entries[0].ReadyAt);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void GetProducedUnits_PrunesDeadProducedUnits()
    {
        using World world = new("BuildingUiQuerySystemTests");
        EntityManager entityManager = world.EntityManager;
        Entity alive = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(alive, new UnitHealth { Current = 10, Max = 10 });
        Entity dead = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(dead, new UnitHealth { Current = 0, Max = 10 });

        var produced = new List<Entity> { alive, dead, Entity.Null };
        var results = new List<Entity>();

        var uiQuery = new BuildingUiQueryUiSystemHelper();
        uiQuery.GetProducedUnits(produced, entityManager, new BuildingProductionQueueCompositionSystemHelper(), results);

        Assert.AreEqual(1, produced.Count);
        Assert.AreEqual(alive, produced[0]);
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(alive, results[0]);
    }

    [Test]
    public void AddProducedUnitEntries_ResolvesReadyPrefabFromPassivePreviewDelegate()
    {
        using World world = new("BuildingUiQuerySystemTests_ProducedUnitPreview");
        EntityManager entityManager = world.EntityManager;
        Entity alive = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(alive, new UnitHealth { Current = 10, Max = 10 });

        GameObject prefab = new("Unit_Infantry_SourceKeyPreview");
        try
        {
            var produced = new List<Entity> { alive };
            var entries = new List<BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry>();

            var uiQuery = new BuildingUiQueryUiSystemHelper();
            uiQuery.AddProducedUnitEntries(
                produced,
                null,
                null,
                null,
                entityManager,
                new BuildingProductionQueueCompositionSystemHelper(),
                0f,
                entries,
                (Entity unit, out GameObject resolvedPrefab) =>
                {
                    resolvedPrefab = unit == alive ? prefab : null;
                    return resolvedPrefab != null;
                });

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(alive, entries[0].Unit);
            Assert.AreSame(prefab, entries[0].Prefab);
            Assert.IsTrue(entries[0].IsReady);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void SelectedBuildingProducedUnits_ReadsProducedUnitReadModel()
    {
        using World world = new("BuildingUiQuerySystemTests_ProducedUnitReadModel");
        EntityManager entityManager = world.EntityManager;
        Entity alive = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(alive, new UnitHealth { Current = 10, Max = 10 });
        Entity dead = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(dead, new UnitHealth { Current = 0, Max = 10 });
        Entity otherBuildingUnit = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(otherBuildingUnit, new UnitHealth { Current = 10, Max = 10 });

        Entity boundaryEntity = entityManager.CreateEntity(typeof(BuildingRuntimeStateTag));
        DynamicBuffer<BuildingProducedUnitReadModel> producedUnits =
            entityManager.AddBuffer<BuildingProducedUnitReadModel>(boundaryEntity);
        producedUnits.Add(new BuildingProducedUnitReadModel
        {
            BuildingRuntimeId = 7,
            Unit = alive,
            UnitSourceKey = new FixedString64Bytes("unit_inf_regular")
        });
        producedUnits.Add(new BuildingProducedUnitReadModel
        {
            BuildingRuntimeId = 7,
            Unit = dead,
            UnitSourceKey = new FixedString64Bytes("unit_inf_regular")
        });
        producedUnits.Add(new BuildingProducedUnitReadModel
        {
            BuildingRuntimeId = 8,
            Unit = otherBuildingUnit,
            UnitSourceKey = new FixedString64Bytes("unit_inf_regular")
        });

        RuntimeBuildingEntity selectedBuilding = new()
        {
            Id = 7
        };
        var runtimeBuildings = new Dictionary<int, RuntimeBuildingEntity>
        {
            [selectedBuilding.Id] = selectedBuilding
        };
        GameObject previewPrefab = new("UnitPreview");
        try
        {
            BuildingUiQueryUiSystemHelper.Context context = new(
                runtimeBuildings,
                () => selectedBuilding.Id,
                (out EntityManager em) =>
                {
                    em = entityManager;
                    return true;
                },
                new BuildingProductionQueueCompositionSystemHelper(),
                () => 10f,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                (Entity unit, out GameObject prefab) =>
                {
                    prefab = unit == alive ? previewPrefab : null;
                    return prefab != null;
                });
            var uiQuery = new BuildingUiQueryUiSystemHelper();
            var producedUnitResults = new List<Entity>();
            uiQuery.GetSelectedBuildingProducedUnits(context, producedUnitResults);

            Assert.AreEqual(1, producedUnitResults.Count);
            Assert.AreEqual(alive, producedUnitResults[0]);
            Assert.IsNull(selectedBuilding.ProducedUnits);

            var entries = new List<BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry>();
            uiQuery.GetSelectedBuildingProducedUnitEntries(context, entries);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(alive, entries[0].Unit);
            Assert.AreSame(previewPrefab, entries[0].Prefab);
            Assert.IsTrue(entries[0].IsReady);
            Assert.IsNull(selectedBuilding.ProducedUnits);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(previewPrefab);
        }
    }

    [Test]
    public void GetFriendlyPendingProductionUiEntries_IncludesPlayerOwnedProducerQueues()
    {
        GameObject prefab = new("Attack Helicopter");
        try
        {
            RuntimeBuildingEntity playerProducer = new()
            {
                Id = 7,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                Definition = new BuildingDefinition { DisplayName = "Player Helipad" },
                PendingProductions = new List<RuntimeBuildingEntity.PendingProduction>
                {
                    new()
                    {
                        Prefab = prefab,
                        StartedAt = 10f,
                        ReadyAt = 20f
                    }
                }
            };
            RuntimeBuildingEntity enemyProducer = new()
            {
                Id = 8,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.EnemyFactionId,
                Definition = new BuildingDefinition { DisplayName = "Enemy Helipad" },
                PendingProductions = new List<RuntimeBuildingEntity.PendingProduction>
                {
                    new()
                    {
                        Prefab = prefab,
                        StartedAt = 10f,
                        ReadyAt = 20f
                    }
                }
            };
            var runtimeBuildings = new Dictionary<int, RuntimeBuildingEntity>
            {
                [playerProducer.Id] = playerProducer,
                [enemyProducer.Id] = enemyProducer
            };
            BuildingUiQueryUiSystemHelper.Context context = new(
                runtimeBuildings,
                null,
                null,
                new BuildingProductionQueueCompositionSystemHelper(),
                () => 12.5f,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
            var entries = new List<BuildingUiQueryUiSystemHelper.PendingProductionUiEntry>();

            var uiQuery = new BuildingUiQueryUiSystemHelper();
            uiQuery.GetFriendlyPendingProductionUiEntries(context, entries);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(playerProducer.Id, entries[0].BuildingId);
            Assert.AreSame(prefab, entries[0].Prefab);
            Assert.AreEqual("Player Helipad", entries[0].ProducerDisplayName);
            Assert.AreEqual(0.25f, entries[0].Progress01, 0.0001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void GetFriendlyPendingProductionUiEntries_IncludesOperationMapProducerQueues()
    {
        using World world = new("BuildingUiQuerySystemTests_OperationMapProductionQueue");
        EntityManager entityManager = world.EntityManager;
        GameObject prefab = new("Rifle Infantry");
        try
        {
            Entity prefabEntity = entityManager.CreateEntity();
            Entity producer = entityManager.CreateEntity(
                typeof(OperationMapBuildingComponent),
                typeof(OperationMapBuildingProductionQueueComponent),
                typeof(OperationMapBuildingUnitProductionRequest),
                typeof(Faction),
                typeof(UnitHealth),
                typeof(UnitDisplayInfo));
            entityManager.SetComponentData(producer, new OperationMapBuildingComponent
            {
                StableId = new FixedString128Bytes("contractor-tent-5006"),
                PlacementIndex = 5006
            });
            entityManager.SetComponentData(producer, new Faction { Id = FactionIdentity.PlayerFactionId });
            entityManager.SetComponentData(producer, new UnitHealth { Current = 350, Max = 350 });
            entityManager.SetComponentData(producer, new UnitDisplayInfo
            {
                Name = new FixedString64Bytes("Contractor Tent")
            });
            entityManager.GetBuffer<OperationMapBuildingUnitProductionRequest>(producer).Add(
                new OperationMapBuildingUnitProductionRequest
                {
                    RequestId = 3,
                    ProductionIndex = 0,
                    UnitPrefab = prefabEntity,
                    UnitSourceKey = new FixedString64Bytes(prefab.name),
                    QueuedAt = 10f,
                    ReadyAt = 30f,
                    Status = OperationMapBuildingUnitProductionRequest.Pending
                });

            BuildingUiQueryUiSystemHelper.Context context = new(
                new Dictionary<int, RuntimeBuildingEntity>(),
                null,
                (out EntityManager em) =>
                {
                    em = entityManager;
                    return true;
                },
                null,
                () => 15f,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                (Entity unit, out GameObject resolvedPrefab) =>
                {
                    resolvedPrefab = unit == prefabEntity ? prefab : null;
                    return resolvedPrefab != null;
                });
            var entries = new List<BuildingUiQueryUiSystemHelper.PendingProductionUiEntry>();

            new BuildingUiQueryUiSystemHelper().GetFriendlyPendingProductionUiEntries(context, entries);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(5006, entries[0].BuildingId);
            Assert.AreEqual(-1, entries[0].PendingProductionIndex);
            Assert.AreSame(prefab, entries[0].Prefab);
            Assert.AreEqual("Contractor Tent", entries[0].ProducerDisplayName);
            Assert.AreEqual(0.25f, entries[0].Progress01, 0.0001f);
            Assert.AreEqual(15f, entries[0].RemainingSeconds, 0.0001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void SelectedMaterialFabricationReadModel_JoinsAuthoritativeEcsStateAndShapesProgress()
    {
        using World world = new("BuildingUiQuerySystemTests_MaterialFabricationReadModel");
        EntityManager entityManager = world.EntityManager;
        RuntimeBuildingEntity selectedBuilding = CreateMaterialFabricationBuilding(
            entityManager,
            runtimeBuildingId: 41,
            ownerFactionId: FactionIdentity.PlayerFactionId,
            storedOilBarrels: 18.5f,
            oilStorageCapacity: 60,
            cycleProgressSeconds: 4f,
            cycleDurationSeconds: 10f);
        selectedBuilding.StoredOilBarrels = 999f;

        Entity playerEconomy = CreateFactionMaterialsEntity(
            entityManager,
            FactionIdentity.PlayerFactionId,
            current: 23,
            capacity: 90,
            version: 7);
        Entity enemyEconomy = CreateFactionMaterialsEntity(
            entityManager,
            FactionIdentity.EnemyFactionId,
            current: 70,
            capacity: 80,
            version: 3);
        BuildingUiQueryUiSystemHelper.Context context = CreateMaterialFabricationContext(
            entityManager,
            selectedBuilding,
            new[] { enemyEconomy, playerEconomy });

        var query = new BuildingUiQueryUiSystemHelper();
        Assert.IsTrue(query.TryGetSelectedMaterialFabricationReadModel(context, out UiMaterialFabricationReadModel model));

        Assert.AreEqual(41, model.RuntimeBuildingId);
        Assert.AreEqual(FactionIdentity.PlayerFactionId, model.OwnerFactionId);
        Assert.AreEqual(18, model.OilInputCurrentBarrels);
        Assert.AreEqual(60, model.OilInputCapacityBarrels);
        Assert.AreEqual(5f, model.OilConsumedPerCycle);
        Assert.AreEqual(10f, model.CycleDurationSeconds);
        Assert.AreEqual(4f, model.CycleProgressSeconds);
        Assert.AreEqual(0.4f, model.Progress01, 0.0001f);
        Assert.AreEqual(3, model.MaterialsOutputPerCycle);
        Assert.AreEqual(23, model.FactionMaterialsCurrent);
        Assert.AreEqual(90, model.FactionMaterialsCapacity);
        Assert.IsTrue(model.ProductionEnabled);
        Assert.AreEqual(MaterialFabricationStatusCode.Producing, model.Status);
        Assert.AreEqual(MaterialFabricationBlockReasonCode.None, model.BlockReason);
        Assert.Greater(model.Version, 0u);
    }

    [Test]
    public void SelectedMaterialFabricationReadModel_RejectsMissingDuplicateAndMismatchedOwners()
    {
        using World world = new("BuildingUiQuerySystemTests_MaterialFabricationOwnerValidation");
        EntityManager entityManager = world.EntityManager;
        RuntimeBuildingEntity selectedBuilding = CreateMaterialFabricationBuilding(
            entityManager,
            runtimeBuildingId: 42,
            ownerFactionId: FactionIdentity.PlayerFactionId);
        var query = new BuildingUiQueryUiSystemHelper();

        BuildingUiQueryUiSystemHelper.Context missingContext = CreateMaterialFabricationContext(
            entityManager,
            selectedBuilding,
            System.Array.Empty<Entity>());
        Assert.IsFalse(query.TryGetSelectedMaterialFabricationReadModel(missingContext, out _));

        Entity mismatchedEconomy = entityManager.CreateEntity(
            typeof(FactionEconomy),
            typeof(FactionTacticalMaterialsComponent));
        entityManager.SetComponentData(mismatchedEconomy, new FactionEconomy
        {
            FactionId = FactionIdentity.PlayerFactionId
        });
        entityManager.SetComponentData(mismatchedEconomy, new FactionTacticalMaterialsComponent
        {
            FactionId = FactionIdentity.EnemyFactionId,
            Current = 10,
            Capacity = 50
        });
        BuildingUiQueryUiSystemHelper.Context mismatchContext = CreateMaterialFabricationContext(
            entityManager,
            selectedBuilding,
            new[] { mismatchedEconomy });
        Assert.IsFalse(query.TryGetSelectedMaterialFabricationReadModel(mismatchContext, out _));

        Entity first = CreateFactionMaterialsEntity(
            entityManager,
            FactionIdentity.PlayerFactionId,
            current: 10,
            capacity: 50);
        Entity second = CreateFactionMaterialsEntity(
            entityManager,
            FactionIdentity.PlayerFactionId,
            current: 20,
            capacity: 60);
        BuildingUiQueryUiSystemHelper.Context duplicateContext = CreateMaterialFabricationContext(
            entityManager,
            selectedBuilding,
            new[] { first, second });
        Assert.IsFalse(query.TryGetSelectedMaterialFabricationReadModel(duplicateContext, out _));
    }

    [Test]
    public void SelectedMaterialFabricationReadModel_RejectsNonDepotSelection()
    {
        using World world = new("BuildingUiQuerySystemTests_NonMaterialFabricationBuilding");
        EntityManager entityManager = world.EntityManager;
        Entity combatEntity = entityManager.CreateEntity(typeof(BuildingResourceStorageComponent));
        entityManager.SetComponentData(combatEntity, new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = 43,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            StoredOilBarrels = 12f,
            OilStorageCapacity = 40
        });
        RuntimeBuildingEntity selectedBuilding = new()
        {
            Id = 43,
            CombatEntity = combatEntity
        };
        Entity playerEconomy = CreateFactionMaterialsEntity(
            entityManager,
            FactionIdentity.PlayerFactionId,
            current: 10,
            capacity: 50);
        BuildingUiQueryUiSystemHelper.Context context = CreateMaterialFabricationContext(
            entityManager,
            selectedBuilding,
            new[] { playerEconomy });

        var query = new BuildingUiQueryUiSystemHelper();
        Assert.IsFalse(query.TryGetSelectedMaterialFabricationReadModel(context, out _));
    }

    [Test]
    public void SelectedMaterialFabricationReadModel_AdvancesVersionOnlyWhenSourceStateChanges()
    {
        using World world = new("BuildingUiQuerySystemTests_MaterialFabricationVersion");
        EntityManager entityManager = world.EntityManager;
        RuntimeBuildingEntity selectedBuilding = CreateMaterialFabricationBuilding(
            entityManager,
            runtimeBuildingId: 44,
            ownerFactionId: FactionIdentity.PlayerFactionId);
        Entity playerEconomy = CreateFactionMaterialsEntity(
            entityManager,
            FactionIdentity.PlayerFactionId,
            current: 10,
            capacity: 50,
            version: 2);
        BuildingUiQueryUiSystemHelper.Context context = CreateMaterialFabricationContext(
            entityManager,
            selectedBuilding,
            new[] { playerEconomy });
        var query = new BuildingUiQueryUiSystemHelper();

        Assert.IsTrue(query.TryGetSelectedMaterialFabricationReadModel(context, out UiMaterialFabricationReadModel first));
        Assert.IsTrue(query.TryGetSelectedMaterialFabricationReadModel(context, out UiMaterialFabricationReadModel unchanged));
        Assert.AreEqual(first.Version, unchanged.Version);

        FactionTacticalMaterialsComponent materials =
            entityManager.GetComponentData<FactionTacticalMaterialsComponent>(playerEconomy);
        materials.Current++;
        entityManager.SetComponentData(playerEconomy, materials);

        Assert.IsTrue(query.TryGetSelectedMaterialFabricationReadModel(context, out UiMaterialFabricationReadModel changed));
        Assert.Greater(changed.Version, unchanged.Version);
        Assert.AreEqual(materials.Current, changed.FactionMaterialsCurrent);
        Assert.IsTrue(query.TryGetSelectedMaterialFabricationReadModel(context, out UiMaterialFabricationReadModel stableAgain));
        Assert.AreEqual(changed.Version, stableAgain.Version);
    }

    [Test]
    public void SelectedMaterialFabricationReadModel_UnchangedReadAllocatesNoManagedMemory()
    {
        using World world = new("BuildingUiQuerySystemTests_MaterialFabricationNoGc");
        EntityManager entityManager = world.EntityManager;
        RuntimeBuildingEntity selectedBuilding = CreateMaterialFabricationBuilding(
            entityManager,
            runtimeBuildingId: 45,
            ownerFactionId: FactionIdentity.PlayerFactionId);
        Entity playerEconomy = CreateFactionMaterialsEntity(
            entityManager,
            FactionIdentity.PlayerFactionId,
            current: 10,
            capacity: 50);
        BuildingUiQueryUiSystemHelper.Context context = CreateMaterialFabricationContext(
            entityManager,
            selectedBuilding,
            new[] { playerEconomy });
        var query = new BuildingUiQueryUiSystemHelper();

        for (int i = 0; i < 8; i++)
            Assert.IsTrue(query.TryGetSelectedMaterialFabricationReadModel(context, out _));

        bool allReadsSucceeded = true;
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 128; i++)
            allReadsSucceeded &= query.TryGetSelectedMaterialFabricationReadModel(context, out _);
        long allocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsTrue(allReadsSucceeded);
        Assert.AreEqual(0L, allocatedBytes);
    }

    private static RuntimeBuildingEntity CreateMaterialFabricationBuilding(
        EntityManager entityManager,
        int runtimeBuildingId,
        byte ownerFactionId,
        float storedOilBarrels = 20f,
        int oilStorageCapacity = 80,
        float cycleProgressSeconds = 2f,
        float cycleDurationSeconds = 8f)
    {
        Entity combatEntity = entityManager.CreateEntity(
            typeof(BuildingResourceStorageComponent),
            typeof(MaterialFabricationComponent),
            typeof(MaterialFabricationInputTag));
        entityManager.SetComponentData(combatEntity, new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = runtimeBuildingId,
            OwnerFactionId = ownerFactionId,
            StoredOilBarrels = storedOilBarrels,
            OilStorageCapacity = oilStorageCapacity,
            Version = 4
        });
        entityManager.SetComponentData(combatEntity, new MaterialFabricationComponent
        {
            RuntimeBuildingId = runtimeBuildingId,
            OwnerFactionId = ownerFactionId,
            ProductionEnabled = 1,
            OilConsumedPerCycle = 5f,
            MaterialsOutputPerCycle = 3,
            CycleDurationSeconds = cycleDurationSeconds,
            CycleProgressSeconds = cycleProgressSeconds,
            Status = MaterialFabricationStatusCode.Producing,
            BlockReason = MaterialFabricationBlockReasonCode.None,
            Version = 5
        });
        return new RuntimeBuildingEntity
        {
            Id = runtimeBuildingId,
            CombatEntity = combatEntity,
            HasOwnerFaction = true,
            OwnerFactionId = ownerFactionId
        };
    }

    private static Entity CreateFactionMaterialsEntity(
        EntityManager entityManager,
        byte factionId,
        int current,
        int capacity,
        uint version = 1)
    {
        Entity entity = entityManager.CreateEntity(
            typeof(FactionEconomy),
            typeof(FactionTacticalMaterialsComponent));
        entityManager.SetComponentData(entity, new FactionEconomy
        {
            FactionId = factionId
        });
        entityManager.SetComponentData(entity, new FactionTacticalMaterialsComponent
        {
            FactionId = factionId,
            Current = current,
            Capacity = capacity,
            Version = version
        });
        return entity;
    }

    private static BuildingUiQueryUiSystemHelper.Context CreateMaterialFabricationContext(
        EntityManager entityManager,
        RuntimeBuildingEntity selectedBuilding,
        IReadOnlyList<Entity> factionResourceEntities)
    {
        var runtimeBuildings = new Dictionary<int, RuntimeBuildingEntity>
        {
            [selectedBuilding.Id] = selectedBuilding
        };
        return new BuildingUiQueryUiSystemHelper.Context(
            runtimeBuildings,
            () => selectedBuilding.Id,
            (out EntityManager em) =>
            {
                em = entityManager;
                return true;
            },
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            factionResourceEntities);
    }

    private sealed class TestPendingProduction : BuildingProductionQueueCompositionSystemHelper.IPendingProduction
    {
        public int ProductionIndex { get; set; }
        public GameObject Prefab { get; set; }
        public float StartedAt { get; set; }
        public float ReadyAt { get; set; }
        public int ReservedProductionSlotIndex { get; set; }
        public GameObject TransportPrefab { get; set; }
        public float TransportArrivalSeconds { get; set; }
        public float TransportHoldForNextReadySeconds { get; set; }
        public int TransportMaxConcurrent { get; set; }
        public BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode TransportMode { get; set; }
        public bool TransportRequiresAirportRunway { get; set; }
    }
}
#endif
