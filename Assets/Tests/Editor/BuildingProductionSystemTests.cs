#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BuildingProductionSystemTests
{
    public static void RunBuildingGameplayCompositionRuntimeSmokeValidation()
    {
        try
        {
            var tests = new BuildingProductionSystemTests();
            tests.BuildingGameplayComposition_InitializesRuntimeDollarsFromInitialUnitsConfig();
            tests.BuildingGameplayComposition_CampBuildingRequestStartsConfiguredPlacement();
            Debug.Log("[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed");
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingGameplayCompositionRuntimeSmokeValidation] result=Failed");
            UnityEditor.EditorApplication.Exit(1);
        }
    }

    public static void RunProductionCameraFocusValidation()
    {
        try
        {
            var tests = new BuildingProductionSystemTests();
            tests.FocusNewestPlayerProducedUnit_RequestsCameraMoveToNewestSpawn();
            tests.FocusNewestPlayerProducedUnit_IgnoresWhenBuildDrawerClosed();
            tests.FocusNewestPlayerProducedUnit_AllowsNeutralOrUnownedProducerOutput();
            tests.FocusNewestPlayerProducedUnit_IgnoresNonPlayerProduction();
            tests.ResolveProducedUnitFaction_DefaultsNeutralOrUnownedProductionToPlayer();
            tests.TryFindFirstFriendlyProducerBuilding_PrefersPlayerProducerOverNeutralFallback();
            tests.TryFindFirstFriendlyProducerBuilding_AllowsNeutralFallbackWhenNoPlayerProducerExists();
            tests.RebuildPendingProductionTimeline_ChainsQueuedItemsAfterActiveProduction();
            tests.RebuildPendingProductionTimeline_AfterActiveRemovalResetsNextActiveProgress();
            Debug.Log("[BuildingProductionCameraFocusValidation] result=Passed tests=9");
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingProductionCameraFocusValidation] result=Failed");
            UnityEditor.EditorApplication.Exit(1);
        }
    }

    public static void RunProductionMetadataValidation()
    {
        try
        {
            var tests = new BuildingProductionSystemTests();
            tests.ResolveProductionDurationSeconds_UsesUnitAuthoringDuration();
            tests.ResolveProductionTransportSettings_UsesConfiguredTransportAuthoring();
            tests.ResolveProductionTransportSettings_DefaultsLargeVehicleToPlaneTransport();
            Debug.Log("[BuildingProductionMetadataValidation] result=Passed tests=3");
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingProductionMetadataValidation] result=Failed");
            UnityEditor.EditorApplication.Exit(1);
        }
    }

    public static void RunProductionRequestValidation()
    {
        try
        {
            var tests = new BuildingProductionSystemTests();
            tests.BuildingUiProductionCommandRequest_QueuesSelectedBuildingUnitAndWritesResult();
            tests.BuildingUiProductionCommandRequest_RejectsMissingActiveBuilding();
            tests.BuildingUiProductionCommandRequest_RejectsStaleFrame();
            tests.BuildingUiProductionCommandRequest_RejectsUnavailablePrefab();
            tests.BuildingUiProductionCommandRequest_RejectsQueueFull();
            tests.BuildingUiProductionCommandRequest_CancelsPendingProductionAndWritesResult();
            tests.BuildingUiCampItemCommandRequest_StartsConfiguredPlacementAndWritesResult();
            tests.BuildingUiCampItemCommandRequest_QueuesUnitProductionAndWritesResult();
            tests.BuildingRuntimeBoundary_ProcessesQueuedUiProductionCommand();
            tests.BuildingRuntimeBoundary_ProcessesQueuedCampItemCommand();
            Debug.Log("[BuildingProductionRequestValidation] result=Passed tests=10");
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingProductionRequestValidation] result=Failed");
            UnityEditor.EditorApplication.Exit(1);
        }
    }

    [Test]
    public void InitializePendingProduction_SetsReadyTimeAndTransportFields()
    {
        var pending = new TestPendingProduction();
        var system = new BuildingProductionSystem();

        system.InitializePendingProduction(
            pending,
            productionIndex: 2,
            spawnUnitPrefab: null,
            now: 10f,
            productionDurationSeconds: 4f,
            reservedProductionSlotIndex: 1,
            transportPrefab: null,
            transportArrivalSeconds: 3f,
            transportHoldForNextReadySeconds: 5f,
            transportMaxConcurrent: 2,
            transportMode: BuildingProductionSystem.ProductionTransportMode.Plane,
            transportRequiresAirportRunway: true);

        Assert.AreEqual(2, pending.ProductionIndex);
        Assert.AreEqual(10f, pending.StartedAt);
        Assert.AreEqual(14f, pending.ReadyAt);
        Assert.AreEqual(1, pending.ReservedProductionSlotIndex);
        Assert.AreEqual(3f, pending.TransportArrivalSeconds);
        Assert.AreEqual(5f, pending.TransportHoldForNextReadySeconds);
        Assert.AreEqual(2, pending.TransportMaxConcurrent);
        Assert.AreEqual(BuildingProductionSystem.ProductionTransportMode.Plane, pending.TransportMode);
        Assert.IsTrue(pending.TransportRequiresAirportRunway);
    }

    [Test]
    public void FocusNewestPlayerProducedUnit_RequestsCameraMoveToNewestSpawn()
    {
        using World world = new("FocusNewestPlayerProducedUnit_RequestsCameraMoveToNewestSpawn");
        EntityManager em = world.EntityManager;
        Entity older = em.CreateEntity(typeof(LocalTransform));
        Entity newest = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(older, LocalTransform.FromPosition(new float3(1f, 0f, 2f)));
        em.SetComponentData(newest, LocalTransform.FromPosition(new float3(7f, 0f, 9f)));

        RuntimeBuildingEntity building = new()
        {
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            ProducedUnits = new List<Entity> { older, newest }
        };

        Vector3? requestedFocus = null;
        BuildingProductionTransportBridgeSystem.Context context = new(
            null,
            null,
            null,
            null,
            default,
            () => true,
            worldPosition => requestedFocus = worldPosition);

        Assert.IsTrue(BuildingProductionTransportBridgeSystem.FocusNewestPlayerProducedUnit(context, building, em));
        Assert.IsTrue(requestedFocus.HasValue);
        Assert.AreEqual(new Vector3(7f, 0f, 9f), requestedFocus.Value);
    }

    [Test]
    public void FocusNewestPlayerProducedUnit_IgnoresWhenBuildDrawerClosed()
    {
        using World world = new("FocusNewestPlayerProducedUnit_IgnoresWhenBuildDrawerClosed");
        EntityManager em = world.EntityManager;
        Entity newest = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(newest, LocalTransform.FromPosition(new float3(7f, 0f, 9f)));

        RuntimeBuildingEntity building = new()
        {
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            ProducedUnits = new List<Entity> { newest }
        };

        bool requestedFocus = false;
        BuildingProductionTransportBridgeSystem.Context context = new(
            null,
            null,
            null,
            null,
            default,
            () => false,
            _ => requestedFocus = true);

        Assert.IsFalse(BuildingProductionTransportBridgeSystem.FocusNewestPlayerProducedUnit(context, building, em));
        Assert.IsFalse(requestedFocus);
    }

    [Test]
    public void FocusNewestPlayerProducedUnit_IgnoresNonPlayerProduction()
    {
        using World world = new("FocusNewestPlayerProducedUnit_IgnoresNonPlayerProduction");
        EntityManager em = world.EntityManager;
        Entity newest = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(newest, LocalTransform.FromPosition(new float3(7f, 0f, 9f)));

        RuntimeBuildingEntity building = new()
        {
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.EnemyFactionId,
            ProducedUnits = new List<Entity> { newest }
        };

        bool requestedFocus = false;
        BuildingProductionTransportBridgeSystem.Context context = new(
            null,
            null,
            null,
            null,
            default,
            () => true,
            _ => requestedFocus = true);

        Assert.IsFalse(BuildingProductionTransportBridgeSystem.FocusNewestPlayerProducedUnit(context, building, em));
        Assert.IsFalse(requestedFocus);
    }

    [Test]
    public void FocusNewestPlayerProducedUnit_AllowsNeutralOrUnownedProducerOutput()
    {
        using World world = new("FocusNewestPlayerProducedUnit_AllowsNeutralOrUnownedProducerOutput");
        EntityManager em = world.EntityManager;
        Entity newest = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(newest, LocalTransform.FromPosition(new float3(3f, 0f, 4f)));

        RuntimeBuildingEntity unownedBuilding = new()
        {
            HasOwnerFaction = false,
            ProducedUnits = new List<Entity> { newest }
        };
        RuntimeBuildingEntity neutralBuilding = new()
        {
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.NeutralFactionId,
            ProducedUnits = new List<Entity> { newest }
        };

        int focusCount = 0;
        BuildingProductionTransportBridgeSystem.Context context = new(
            null,
            null,
            null,
            null,
            default,
            () => true,
            _ => focusCount++);

        Assert.IsTrue(BuildingProductionTransportBridgeSystem.FocusNewestPlayerProducedUnit(context, unownedBuilding, em));
        Assert.IsTrue(BuildingProductionTransportBridgeSystem.FocusNewestPlayerProducedUnit(context, neutralBuilding, em));
        Assert.AreEqual(2, focusCount);
    }

    [Test]
    public void ResolveProducedUnitFaction_DefaultsNeutralOrUnownedProductionToPlayer()
    {
        Assert.AreEqual(
            FactionIdentity.PlayerFactionId,
            BuildingSpawnSystem.ResolveProducedUnitFaction(new RuntimeBuildingEntity { HasOwnerFaction = false }));
        Assert.AreEqual(
            FactionIdentity.PlayerFactionId,
            BuildingSpawnSystem.ResolveProducedUnitFaction(new RuntimeBuildingEntity
            {
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.NeutralFactionId
            }));
        Assert.AreEqual(
            FactionIdentity.PlayerFactionId,
            BuildingSpawnSystem.ResolveProducedUnitFaction(new RuntimeBuildingEntity
            {
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId
            }));
        Assert.AreEqual(
            FactionIdentity.EnemyFactionId,
            BuildingSpawnSystem.ResolveProducedUnitFaction(new RuntimeBuildingEntity
            {
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.EnemyFactionId
            }));
    }

    [Test]
    public void TryFindFirstFriendlyProducerBuilding_PrefersPlayerProducerOverNeutralFallback()
    {
        var requestSystem = new BuildingProductionRequestBoundary();
        var productionSystem = new BuildingProductionSystem();
        GameObject unitPrefab = new("Attack Helicopter");
        try
        {
            RuntimeBuildingEntity neutralProducer = CreateProducerBuilding(
                id: 10,
                displayName: "Neutral Helipad",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.NeutralFactionId);
            RuntimeBuildingEntity playerProducer = CreateProducerBuilding(
                id: 20,
                displayName: "Player Helipad",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [neutralProducer.Id] = neutralProducer,
                [playerProducer.Id] = playerProducer
            };
            BuildingProductionRequestBoundary.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab);

            Assert.IsTrue(requestSystem.TryFindFirstFriendlyProducerBuilding(
                context,
                unitPrefab,
                out int buildingId,
                out int productionIndex,
                out string displayName));
            Assert.AreEqual(playerProducer.Id, buildingId);
            Assert.AreEqual(0, productionIndex);
            Assert.AreEqual("Player Helipad", displayName);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void TryFindFirstFriendlyProducerBuilding_AllowsNeutralFallbackWhenNoPlayerProducerExists()
    {
        var requestSystem = new BuildingProductionRequestBoundary();
        var productionSystem = new BuildingProductionSystem();
        GameObject unitPrefab = new("Attack Helicopter");
        try
        {
            RuntimeBuildingEntity neutralProducer = CreateProducerBuilding(
                id: 10,
                displayName: "Neutral Helipad",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.NeutralFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [neutralProducer.Id] = neutralProducer
            };
            BuildingProductionRequestBoundary.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab);

            Assert.IsTrue(requestSystem.TryFindFirstFriendlyProducerBuilding(
                context,
                unitPrefab,
                out int buildingId,
                out int productionIndex,
                out string displayName));
            Assert.AreEqual(neutralProducer.Id, buildingId);
            Assert.AreEqual(0, productionIndex);
            Assert.AreEqual("Neutral Helipad", displayName);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiProductionCommandRequest_QueuesSelectedBuildingUnitAndWritesResult()
    {
        using World world = new("BuildingUiProductionCommandRequestTest");
        var requestSystem = new BuildingProductionRequestBoundary();
        var productionSystem = new BuildingProductionSystem();
        GameObject unitPrefab = new("Requestable Unit");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 20,
                displayName: "Player Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            BuildingProductionRequestBoundary.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager);

            int requestId = requestSystem.EnqueueCreateUnitFromSelectedBuilding(
                world.EntityManager,
                producer.Id,
                productionIndex: 0,
                frameCount: 42);
            requestSystem.ProcessPendingUiProductionCommands(world.EntityManager, context, frameCount: 42);

            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.Queued, result.ResultCode);
            Assert.AreEqual(producer.Id, result.BuildingId);
            Assert.AreEqual(0, result.ProductionIndex);
            Assert.AreEqual(1, producer.PendingProductions.Count);
            Assert.AreSame(unitPrefab, producer.PendingProductions[0].Prefab);

            using EntityQuery queueQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingUiProductionCommandQueueComponent>());
            Entity queueEntity = queueQuery.GetSingletonEntity();
            Assert.AreEqual(0, world.EntityManager.GetBuffer<BuildingUiProductionCommandRequestElement>(queueEntity).Length);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiProductionCommandRequest_RejectsMissingActiveBuilding()
    {
        using World world = new("BuildingUiProductionCommandMissingActiveBuildingTest");
        var requestSystem = new BuildingProductionRequestBoundary();
        var productionSystem = new BuildingProductionSystem();
        GameObject unitPrefab = new("Requestable Unit");
        try
        {
            BuildingProductionRequestBoundary.Context context = CreateProducerSelectionContext(
                new Dictionary<int, RuntimeBuildingEntity>(),
                productionSystem,
                unitPrefab,
                world.EntityManager);

            int requestId = requestSystem.EnqueueCreateUnitFromSelectedBuilding(
                world.EntityManager,
                null,
                productionIndex: 0,
                frameCount: 42);
            requestSystem.ProcessPendingUiProductionCommands(world.EntityManager, context, frameCount: 42);

            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(0, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.MissingActiveBuilding, result.ResultCode);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiProductionCommandRequest_RejectsStaleFrame()
    {
        using World world = new("BuildingUiProductionCommandStaleFrameTest");
        var requestSystem = new BuildingProductionRequestBoundary();
        var productionSystem = new BuildingProductionSystem();
        GameObject unitPrefab = new("Requestable Unit");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 25,
                displayName: "Player Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            BuildingProductionRequestBoundary.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager);

            int requestId = requestSystem.EnqueueCreateUnitFromSelectedBuilding(
                world.EntityManager,
                producer.Id,
                productionIndex: 0,
                frameCount: 41);
            requestSystem.ProcessPendingUiProductionCommands(world.EntityManager, context, frameCount: 42);

            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(0, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.NotArmed, result.ResultCode);
            Assert.AreEqual(0, producer.PendingProductions.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiProductionCommandRequest_RejectsUnavailablePrefab()
    {
        using World world = new("BuildingUiProductionCommandUnavailablePrefabTest");
        var requestSystem = new BuildingProductionRequestBoundary();
        var productionSystem = new BuildingProductionSystem();
        GameObject contextUnitPrefab = new("Context Unit");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 30,
                displayName: "Player Factory",
                unitPrefab: null,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            BuildingProductionRequestBoundary.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                contextUnitPrefab,
                world.EntityManager);

            int requestId = requestSystem.EnqueueCreateUnitFromSelectedBuilding(
                world.EntityManager,
                producer.Id,
                productionIndex: 0,
                frameCount: 42);
            requestSystem.ProcessPendingUiProductionCommands(world.EntityManager, context, frameCount: 42);

            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(0, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.UnavailablePrefab, result.ResultCode);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(contextUnitPrefab);
        }
    }

    [Test]
    public void BuildingUiProductionCommandRequest_RejectsQueueFull()
    {
        using World world = new("BuildingUiProductionCommandQueueFullTest");
        var requestSystem = new BuildingProductionRequestBoundary();
        var productionSystem = new BuildingProductionSystem();
        GameObject unitPrefab = new("Requestable Unit");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 31,
                displayName: "Player Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            BuildingProductionRequestBoundary.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager,
                tryQueuePlayerUnit: (_, _, _) => false);

            int requestId = requestSystem.EnqueueCreateUnitFromSelectedBuilding(
                world.EntityManager,
                producer.Id,
                productionIndex: 0,
                frameCount: 42);
            requestSystem.ProcessPendingUiProductionCommands(world.EntityManager, context, frameCount: 42);

            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(0, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.QueueFull, result.ResultCode);
            Assert.AreEqual(0, producer.PendingProductions.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiProductionCommandRequest_CancelsPendingProductionAndWritesResult()
    {
        using World world = new("BuildingUiProductionCommandCancelProductionTest");
        var requestSystem = new BuildingProductionRequestBoundary();
        var productionSystem = new BuildingProductionSystem();
        GameObject unitPrefab = new("Requestable Unit");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 20,
                displayName: "Player Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            BuildingProductionRequestBoundary.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager);

            Assert.IsTrue(requestSystem.EnqueueAndProcessCreateUnitFromSelectedBuilding(
                world.EntityManager,
                context,
                producer.Id,
                productionIndex: 0,
                frameCount: 42));
            Assert.AreEqual(1, producer.PendingProductions.Count);

            int requestId = requestSystem.EnqueueCancelProduction(world.EntityManager, producer.Id, pendingProductionIndex: 0);
            requestSystem.ProcessPendingUiProductionCommands(world.EntityManager, context, frameCount: 0, now: 20f);

            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.Cancelled, result.ResultCode);
            Assert.AreEqual(0, producer.PendingProductions.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiCampItemCommandRequest_StartsConfiguredPlacementAndWritesResult()
    {
        using World world = new("BuildingUiCampItemCommandPlacementTest");
        var requestSystem = new BuildingProductionRequestBoundary();
        GameObject buildingPrefab = new("Requestable Airport");
        try
        {
            BuildingDefinition definition = new()
            {
                DisplayName = "Requestable Airport",
                Prefab = buildingPrefab
            };
            bool beganPlacement = false;
            int activePlacementCost = -1;
            BuildingProductionRequestBoundary.Context context = CreateCampItemRequestContext(
                new Dictionary<int, RuntimeBuildingEntity>(),
                new List<BuildingDefinition> { definition },
                new Dictionary<GameObject, BuildingDefinition> { { buildingPrefab, definition } },
                Array.Empty<GameObject>(),
                new Dictionary<string, GameObject>(),
                requestPrefab =>
                {
                    beganPlacement = requestPrefab == buildingPrefab;
                    return true;
                },
                _ => true,
                _ => { },
                amount => activePlacementCost = amount);

            int requestId = requestSystem.EnqueueCampItemRequest(
                world.EntityManager,
                buildingPrefab,
                price: 1234,
                focusProducerOnSuccess: true);
            requestSystem.ProcessPendingUiCampItemCommands(world.EntityManager, context, frameCount: 77);

            Assert.IsTrue(requestSystem.TryGetUiCampItemCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiCampItemCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiCampItemCommandResultElement.PlacementStarted, result.ResultCode);
            Assert.AreEqual(1234, result.Price);
            Assert.AreEqual(BuildingDefinitionSystem.NormalizeSpawnableKey(buildingPrefab.name), result.ItemId.ToString());
            Assert.IsTrue(beganPlacement);
            Assert.AreEqual(1234, activePlacementCost);

            using EntityQuery queueQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingUiCampItemCommandQueueComponent>());
            Entity queueEntity = queueQuery.GetSingletonEntity();
            Assert.AreEqual(0, world.EntityManager.GetBuffer<BuildingUiCampItemCommandRequestElement>(queueEntity).Length);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(buildingPrefab);
        }
    }

    [Test]
    public void BuildingUiCampItemCommandRequest_QueuesUnitProductionAndWritesResult()
    {
        using World world = new("BuildingUiCampItemCommandUnitProductionTest");
        var requestSystem = new BuildingProductionRequestBoundary();
        var productionSystem = new BuildingProductionSystem();
        GameObject unitPrefab = new("Requestable Vehicle");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 21,
                displayName: "Vehicle Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            BuildingProductionRequestBoundary.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager);

            int requestId = requestSystem.EnqueueCampItemRequest(
                world.EntityManager,
                unitPrefab,
                price: 5678,
                focusProducerOnSuccess: false);
            requestSystem.ProcessPendingUiCampItemCommands(world.EntityManager, context, frameCount: 88);

            Assert.IsTrue(requestSystem.TryGetUiCampItemCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiCampItemCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiCampItemCommandResultElement.ProductionQueued, result.ResultCode);
            Assert.AreEqual(5678, result.Price);
            Assert.AreEqual(BuildingDefinitionSystem.NormalizeSpawnableKey(unitPrefab.name), result.ItemId.ToString());
            Assert.AreEqual(1, producer.PendingProductions.Count);
            Assert.AreSame(unitPrefab, producer.PendingProductions[0].Prefab);
            Assert.AreEqual(0, producer.PendingProductions[0].ProductionIndex);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingRuntimeBoundary_ProcessesQueuedUiProductionCommand()
    {
        using World world = new("BuildingRuntimeBoundaryQueuedUiProductionTest");
        var requestSystem = new BuildingProductionRequestBoundary();
        var productionSystem = new BuildingProductionSystem();
        var boundarySystem = new BuildingRuntimeBoundarySystem();
        var runtimeQuerySystem = new BuildingRuntimeQuerySystem();
        GameObject unitPrefab = new("Runtime Boundary Unit");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 42,
                displayName: "Boundary Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            Entity boundaryEntity = world.EntityManager.CreateEntity(typeof(BuildingRuntimeBoundaryTag));
            using EntityQuery boundaryQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>());
            BuildingProductionRequestBoundary.Context productionContext = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager);
            BuildingRuntimeQuerySystem.Context runtimeQueryContext = CreateRuntimeQueryContext(
                runtimeBuildings,
                world.EntityManager,
                productionSystem);

            int requestId = requestSystem.EnqueueCreateUnitFromSelectedBuilding(
                world.EntityManager,
                producer.Id,
                productionIndex: 0,
                frameCount: 42);

            boundarySystem.Update(
                new BuildingDefinitionSystem(),
                new BuildingRuntimeSpawnSystem(),
                default,
                requestSystem,
                productionContext,
                runtimeQuerySystem,
                runtimeQueryContext,
                new FactionResourceSystem(),
                world.EntityManager,
                boundaryQuery,
                runtimeBuildings,
                now: 20f,
                frameCount: 42);

            Assert.IsTrue(world.EntityManager.Exists(boundaryEntity));
            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.Queued, result.ResultCode);
            Assert.AreEqual(1, producer.PendingProductions.Count);
            Assert.AreSame(unitPrefab, producer.PendingProductions[0].Prefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingRuntimeBoundary_ProcessesQueuedCampItemCommand()
    {
        using World world = new("BuildingRuntimeBoundaryQueuedCampItemTest");
        var requestSystem = new BuildingProductionRequestBoundary();
        var productionSystem = new BuildingProductionSystem();
        var boundarySystem = new BuildingRuntimeBoundarySystem();
        var runtimeQuerySystem = new BuildingRuntimeQuerySystem();
        GameObject unitPrefab = new("Runtime Boundary Vehicle");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 43,
                displayName: "Boundary Vehicle Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            world.EntityManager.CreateEntity(typeof(BuildingRuntimeBoundaryTag));
            using EntityQuery boundaryQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>());
            BuildingProductionRequestBoundary.Context productionContext = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager);
            BuildingRuntimeQuerySystem.Context runtimeQueryContext = CreateRuntimeQueryContext(
                runtimeBuildings,
                world.EntityManager,
                productionSystem);

            int requestId = requestSystem.EnqueueCampItemRequest(
                world.EntityManager,
                unitPrefab,
                price: 250,
                focusProducerOnSuccess: false);

            boundarySystem.Update(
                new BuildingDefinitionSystem(),
                new BuildingRuntimeSpawnSystem(),
                default,
                requestSystem,
                productionContext,
                runtimeQuerySystem,
                runtimeQueryContext,
                new FactionResourceSystem(),
                world.EntityManager,
                boundaryQuery,
                runtimeBuildings,
                now: 20f,
                frameCount: 42);

            Assert.IsTrue(requestSystem.TryGetUiCampItemCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiCampItemCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiCampItemCommandResultElement.ProductionQueued, result.ResultCode);
            Assert.AreEqual(1, producer.PendingProductions.Count);
            Assert.AreSame(unitPrefab, producer.PendingProductions[0].Prefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void ResolveProductionDurationSeconds_UsesUnitAuthoringDuration()
    {
        GameObject prefab = new("Unit_Infantry_Test");
        try
        {
            UnitGridAuthoring authoring = prefab.AddComponent<UnitGridAuthoring>();
            SetAuthoringField(authoring, "productionDurationSeconds", 12.5f);

            BuildingProductionSystem system = CreateProductionSystem();

            Assert.AreEqual(12.5f, system.ResolveProductionDurationSeconds(prefab), 0.0001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void ResolveProductionTransportSettings_UsesConfiguredTransportAuthoring()
    {
        GameObject producedPrefab = new("Unit_Infantry_Test");
        GameObject transportPrefab = new("Unit_Veh_Helicopter_Transport");
        try
        {
            UnitGridAuthoring producedAuthoring = producedPrefab.AddComponent<UnitGridAuthoring>();
            UnitGridAuthoring transportAuthoring = transportPrefab.AddComponent<UnitGridAuthoring>();
            SetAuthoringField(producedAuthoring, "productionTransportPrefab", transportPrefab);
            SetAuthoringField(transportAuthoring, "productionTransportArrivalSeconds", 8f);
            SetAuthoringField(transportAuthoring, "productionTransportHoldForNextReadySeconds", 3f);
            SetAuthoringField(transportAuthoring, "productionTransportMaxConcurrent", 4);

            BuildingProductionSystem system = CreateProductionSystem();
            BuildingProductionSystem.ProductionTransportSettings settings = system.ResolveProductionTransportSettings(
                producedPrefab,
                new[] { transportPrefab },
                new Dictionary<string, GameObject> { ["unit_veh_helicopter_transport"] = transportPrefab },
                null);

            Assert.AreSame(transportPrefab, settings.TransportPrefab);
            Assert.AreEqual(8f, settings.ArrivalSeconds, 0.0001f);
            Assert.AreEqual(3f, settings.HoldForNextReadySeconds, 0.0001f);
            Assert.AreEqual(4, settings.MaxConcurrent);
            Assert.AreEqual(BuildingProductionSystem.ProductionTransportMode.Helicopter, settings.Mode);
            Assert.IsFalse(settings.RequiresAirportRunway);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(producedPrefab);
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void ResolveProductionTransportSettings_DefaultsLargeVehicleToPlaneTransport()
    {
        GameObject producedPrefab = new("Unit_Veh_TankHeavy_Test");
        GameObject helicopterPrefab = new("Unit_Veh_Helicopter_Transport");
        GameObject planePrefab = new("Unit_Veh_Plane_Transport");
        try
        {
            producedPrefab.AddComponent<UnitGridAuthoring>();
            helicopterPrefab.AddComponent<UnitGridAuthoring>();
            planePrefab.AddComponent<UnitGridAuthoring>();

            var prefabsByKey = new Dictionary<string, GameObject>
            {
                ["unit_veh_helicopter_transport"] = helicopterPrefab,
                ["unit_veh_plane_transport"] = planePrefab
            };
            BuildingProductionSystem system = CreateProductionSystem();
            BuildingProductionSystem.ProductionTransportSettings settings = system.ResolveProductionTransportSettings(
                producedPrefab,
                new[] { helicopterPrefab, planePrefab },
                prefabsByKey,
                (GameObject _, out Bounds bounds) =>
                {
                    bounds = new Bounds(Vector3.zero, new Vector3(3f, 1f, 2f));
                    return true;
                });

            Assert.AreSame(planePrefab, settings.TransportPrefab);
            Assert.AreEqual(BuildingProductionSystem.ProductionTransportMode.Plane, settings.Mode);
            Assert.IsTrue(settings.RequiresAirportRunway);
            Assert.AreEqual(1, settings.MaxConcurrent);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(producedPrefab);
            UnityEngine.Object.DestroyImmediate(helicopterPrefab);
            UnityEngine.Object.DestroyImmediate(planePrefab);
        }
    }

    [Test]
    public void PrewarmConfiguredProductionTransportPools_ParentsSelfArrivingAirUnitsUnderRuntimeRoot()
    {
        GameObject runtimeRoot = new("RuntimeTransports_Test");
        GameObject airPrefab = new("Unit_Veh_Jet_02");
        using var world = new World(nameof(PrewarmConfiguredProductionTransportPools_ParentsSelfArrivingAirUnitsUnderRuntimeRoot));
        try
        {
            UnitGridAuthoring airAuthoring = airPrefab.AddComponent<UnitGridAuthoring>();
            SetAuthoringField(airAuthoring, "isAirUnit", true);
            SetAuthoringField(airAuthoring, "productionTransportArrivalSeconds", 2f);
            SetAuthoringField(airAuthoring, "productionTransportHoldForNextReadySeconds", 1f);
            SetAuthoringField(airAuthoring, "productionTransportMaxConcurrent", 1);
            SetAuthoringField(airAuthoring, "productionTransportRequiresAirportRunway", true);

            BuildingProductionSystem productionSystem = CreateProductionSystem();
            BuildingProductionTransportSystem transportSystem = new();
            transportSystem.SetRuntimeRoot(runtimeRoot.transform);

            transportSystem.PrewarmConfiguredProductionTransportPools(
                productionSystem,
                new[] { airPrefab },
                new Dictionary<string, GameObject> { ["unit_veh_jet_02"] = airPrefab },
                null,
                world.GetOrCreateSystemManaged<BuildingVisualSystem>());

            Assert.AreEqual(2, runtimeRoot.transform.childCount, "Self-arriving air transport prewarm should still keep the default warm pool size.");
            for (int i = 0; i < runtimeRoot.transform.childCount; i++)
            {
                Transform child = runtimeRoot.transform.GetChild(i);
                Assert.AreSame(runtimeRoot.transform, child.parent);
                Assert.IsFalse(child.gameObject.activeSelf);
                StringAssert.StartsWith("Unit_Veh_Jet_02", child.name);
            }

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Assert.AreNotEqual(
                    "Unit_Veh_Jet_02(Clone)",
                    root.name,
                    "Prewarmed transport pool instances must be owned by RuntimeTransports, not leaked as scene-root unit clones.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(runtimeRoot);
            UnityEngine.Object.DestroyImmediate(airPrefab);
        }
    }

    [Test]
    public void GetProgress_ComputesRemainingAndCanCapTransportProgress()
    {
        var transportPrefab = new GameObject("Transport");
        try
        {
            var pending = new TestPendingProduction
            {
                StartedAt = 0f,
                ReadyAt = 10f,
                TransportPrefab = transportPrefab
            };

            var system = new BuildingProductionSystem();
            BuildingProductionSystem.PendingProductionProgress progress = system.GetProgress(pending, 9.9f, true);

            Assert.AreEqual(10f, progress.DurationSeconds);
            Assert.AreEqual(0.1f, progress.RemainingSeconds, 0.0001f);
            Assert.AreEqual(0.97f, progress.Progress01, 0.0001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void TransportLaunchAndDelay_UsesArrivalWindow()
    {
        var transportPrefab = new GameObject("Transport");
        try
        {
            var pending = new TestPendingProduction
            {
                StartedAt = 0f,
                ReadyAt = 20f,
                TransportPrefab = transportPrefab,
                TransportArrivalSeconds = 5f
            };

            var system = new BuildingProductionSystem();

            Assert.AreEqual(15f, system.GetTransportLaunchAt(pending));
            Assert.IsFalse(system.ShouldLaunchTransport(pending, 14.9f));
            Assert.IsTrue(system.ShouldLaunchTransport(pending, 15f));

            system.DelayPendingProduction(pending, 2f);

            Assert.AreEqual(2f, pending.StartedAt);
            Assert.AreEqual(22f, pending.ReadyAt);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void RebuildPendingProductionTimeline_ChainsQueuedItemsAfterActiveProduction()
    {
        var first = new TestPendingProduction { StartedAt = 10f, ReadyAt = 20f };
        var second = new TestPendingProduction { StartedAt = 12f, ReadyAt = 22f };
        var third = new TestPendingProduction { StartedAt = 13f, ReadyAt = 18f };
        var pending = new List<TestPendingProduction> { first, second, third };

        var system = new BuildingProductionSystem();
        system.RebuildPendingProductionTimeline(pending, now: 15f, preserveActiveProgress: true);

        Assert.AreEqual(10f, first.StartedAt, 0.0001f);
        Assert.AreEqual(20f, first.ReadyAt, 0.0001f);
        Assert.AreEqual(20f, second.StartedAt, 0.0001f);
        Assert.AreEqual(30f, second.ReadyAt, 0.0001f);
        Assert.AreEqual(30f, third.StartedAt, 0.0001f);
        Assert.AreEqual(35f, third.ReadyAt, 0.0001f);
    }

    [Test]
    public void RebuildPendingProductionTimeline_AfterActiveRemovalResetsNextActiveProgress()
    {
        var next = new TestPendingProduction { StartedAt = 0f, ReadyAt = 10f };
        var later = new TestPendingProduction { StartedAt = 0f, ReadyAt = 5f };
        var pending = new List<TestPendingProduction> { next, later };

        var system = new BuildingProductionSystem();
        system.RebuildPendingProductionTimeline(pending, now: 50f, preserveActiveProgress: false);
        BuildingProductionSystem.PendingProductionProgress progress = system.GetProgress(next, 50f, capTransportProgress: false);

        Assert.AreEqual(50f, next.StartedAt, 0.0001f);
        Assert.AreEqual(60f, next.ReadyAt, 0.0001f);
        Assert.AreEqual(60f, later.StartedAt, 0.0001f);
        Assert.AreEqual(65f, later.ReadyAt, 0.0001f);
        Assert.AreEqual(0f, progress.Progress01, 0.0001f);
    }

    [Test]
    public void ReadinessHelpers_ReportReadyAndSoonStates()
    {
        var pending = new TestPendingProduction
        {
            ReadyAt = 20f
        };

        var system = new BuildingProductionSystem();

        Assert.IsFalse(system.IsReady(pending, 19.9f));
        Assert.IsTrue(system.IsReady(pending, 20f));
        Assert.IsTrue(system.IsReadyWithin(pending, 18f, 2.5f));
        Assert.IsFalse(system.IsReadyWithin(pending, 16f, 2.5f));
    }

    [Test]
    public void PruneProducedUnits_RemovesDeadUnitsAndClearsDeadSlots()
    {
        using World world = new("BuildingProductionSystemTests");
        EntityManager entityManager = world.EntityManager;
        Entity alive = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(alive, new UnitHealth { Current = 5, Max = 10 });
        Entity dead = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(dead, new UnitHealth { Current = 0, Max = 10 });
        var producedUnits = new List<Entity> { alive, dead, Entity.Null };
        GameObject deadPrefab = new("DeadPrefab");
        var producedUnitPrefabs = new Dictionary<Entity, GameObject>
        {
            [dead] = deadPrefab
        };
        Entity[] slots = { dead, alive };

        try
        {
            var system = new BuildingProductionSystem();
            system.PruneProducedUnits(producedUnits, slots, producedUnitPrefabs, entityManager);

            Assert.AreEqual(1, producedUnits.Count);
            Assert.AreEqual(alive, producedUnits[0]);
            Assert.IsFalse(producedUnitPrefabs.ContainsKey(dead));
            Assert.AreEqual(Entity.Null, slots[0]);
            Assert.AreEqual(alive, slots[1]);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(deadPrefab);
        }
    }

    [Test]
    public void TransportPendingQueries_FindReadyAndSoonEntries()
    {
        var transportPrefab = new GameObject("Transport");
        try
        {
            var ready = new TestPendingProduction
            {
                TransportPrefab = transportPrefab,
                ReadyAt = 10f
            };
            var soon = new TestPendingProduction
            {
                TransportPrefab = transportPrefab,
                ReadyAt = 13f
            };
            var later = new TestPendingProduction
            {
                TransportPrefab = transportPrefab,
                ReadyAt = 20f
            };
            var pending = new List<TestPendingProduction> { soon, ready, later };

            var system = new BuildingProductionSystem();

            Assert.AreSame(ready, system.FindNextReadyTransportPending(pending, transportPrefab, 10f));
            Assert.AreSame(soon, system.FindNextSoonTransportPending(pending, transportPrefab, 10f, 4f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void PendingRemovalHelpers_RemoveByReferenceAndIndex()
    {
        var first = new TestPendingProduction();
        var second = new TestPendingProduction();
        var third = new TestPendingProduction();
        var pending = new List<TestPendingProduction> { first, second, third };

        var system = new BuildingProductionSystem();

        Assert.IsTrue(system.RemovePendingProduction(pending, second));
        Assert.AreEqual(2, pending.Count);
        Assert.AreSame(first, pending[0]);
        Assert.AreSame(third, pending[1]);

        Assert.IsTrue(system.RemovePendingAt(pending, 0));
        Assert.AreEqual(1, pending.Count);
        Assert.AreSame(third, pending[0]);
    }

    [Test]
    public void BuildingGameplayComposition_InitializesRuntimeDollarsFromInitialUnitsConfig()
    {
        var placementConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        var initialUnitsConfig = ScriptableObject.CreateInstance<InitialUnitsSpawnerAuthoringSceneConfigAsset>();
        BuildingGameplayCompositionResultSystem.Result result = default;
        try
        {
            SetPrivateField(initialUnitsConfig, "initialDollars", 12345);
            SetPrivateField(placementConfig, "initialUnitsConfig", initialUnitsConfig);

            var composition = new BuildingGameplayCompositionSystem();
            result = composition.Initialize(
                placementConfig,
                worldCamera: null,
                runtimeTransportsRoot: null,
                runtimeUiRoot: null,
                roadFootprintState: default,
                factionVisuals: null,
                dayNight: null,
                resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeySystem.ResolveSpawnableLookupKey,
                tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataSystem.TryGetBuildingDefinitionMetadata,
                tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataSystem.TryGetUnitDefinitionMetadata);

            Assert.AreEqual(12345, result.UiCommand.CurrentDollars(result.UiCommandContext));
        }
        finally
        {
            result.Dispose?.Invoke();
            UnityEngine.Object.DestroyImmediate(initialUnitsConfig);
            UnityEngine.Object.DestroyImmediate(placementConfig);
        }
    }

    [Test]
    public void BuildingGameplayComposition_CampBuildingRequestStartsConfiguredPlacement()
    {
        var placementConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        var initialUnitsConfig = ScriptableObject.CreateInstance<InitialUnitsSpawnerAuthoringSceneConfigAsset>();
        var buildingPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        BuildingGameplayCompositionResultSystem.Result result = default;
        try
        {
            buildingPrefab.name = "Soldier Base";
            SetPrivateField(initialUnitsConfig, "initialDollars", 10000);
            SetPrivateField(placementConfig, "initialUnitsConfig", initialUnitsConfig);
            SetPrivateField(placementConfig, "spawnables", new List<GameObject> { buildingPrefab });

            var composition = new BuildingGameplayCompositionSystem();
            result = composition.Initialize(
                placementConfig,
                worldCamera: null,
                runtimeTransportsRoot: null,
                runtimeUiRoot: null,
                roadFootprintState: default,
                factionVisuals: null,
                dayNight: null,
                resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeySystem.ResolveSpawnableLookupKey,
                tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataSystem.TryGetBuildingDefinitionMetadata,
                tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataSystem.TryGetUnitDefinitionMetadata);

            BuildingUiCommandBoundary.CampRequestFailure failure = result.UiCommand.TryRequestCampItem(
                result.UiCommandContext,
                buildingPrefab,
                price: 500,
                out _,
                focusProducerOnSuccess: true);

            Assert.AreEqual(BuildingUiCommandBoundary.CampRequestFailure.None, failure);
        }
        finally
        {
            result.Dispose?.Invoke();
            UnityEngine.Object.DestroyImmediate(buildingPrefab);
            UnityEngine.Object.DestroyImmediate(initialUnitsConfig);
            UnityEngine.Object.DestroyImmediate(placementConfig);
        }
    }

    private sealed class TestPendingProduction : BuildingProductionSystem.IPendingProduction
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
        public BuildingProductionSystem.ProductionTransportMode TransportMode { get; set; }
        public bool TransportRequiresAirportRunway { get; set; }
    }

    private static void SetAuthoringField<T>(UnitGridAuthoring authoring, string fieldName, T value)
    {
        FieldInfo field = typeof(UnitGridAuthoring).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"{nameof(UnitGridAuthoring)} must expose serialized field '{fieldName}' for this test.");
        field.SetValue(authoring, value);
    }

    private static BuildingProductionSystem CreateProductionSystem()
    {
        var system = new BuildingProductionSystem();
        system.ConfigureUnitProductionMetadataResolver(BuildingProductionUnitMetadataSystem.TryGetMetadata);
        return system;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = FindPrivateField(target.GetType(), fieldName);
        Assert.IsNotNull(field, $"{target.GetType().Name} must expose serialized field '{fieldName}' for this test.");
        field.SetValue(target, value);
    }

    private static RuntimeBuildingEntity CreateProducerBuilding(
        int id,
        string displayName,
        GameObject unitPrefab,
        bool hasOwnerFaction,
        byte ownerFactionId)
    {
        return new RuntimeBuildingEntity
        {
            Id = id,
            HasOwnerFaction = hasOwnerFaction,
            OwnerFactionId = ownerFactionId,
            Definition = new BuildingDefinition
            {
                DisplayName = displayName,
                ProductionSlots = new List<BuildingDefinition.ProductionSlotDefinition>
                {
                    new() { SpawnUnitPrefab = unitPrefab }
                }
            },
            ProducedUnits = new List<Entity>(),
            ProducedUnitPrefabs = new Dictionary<Entity, GameObject>(),
            PendingProductions = new List<RuntimeBuildingEntity.PendingProduction>()
        };
    }

    private static BuildingProductionRequestBoundary.Context CreateCampItemRequestContext(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        IReadOnlyList<BuildingDefinition> configuredSpawnableDefinitions,
        IReadOnlyDictionary<GameObject, BuildingDefinition> configuredDefinitionsByPrefab,
        IReadOnlyList<GameObject> unitPrefabs,
        IReadOnlyDictionary<string, GameObject> unitPrefabsByKey,
        BuildingProductionRequestBoundary.BeginPlacementForConfiguredSpawnableDelegate beginPlacement,
        BuildingProductionRequestBoundary.TrySpendDollarsDelegate trySpendDollars,
        BuildingProductionRequestBoundary.RefundDollarsDelegate refundDollars,
        BuildingProductionRequestBoundary.SetActivePlacementCostDelegate setActivePlacementCost)
    {
        var productionSystem = new BuildingProductionSystem();
        BuildingProductionSystem.QueueContext queueContext = new(
            unitPrefabs,
            unitPrefabsByKey,
            new BuildingProductionSlotSystem(),
            null,
            null);

        return new BuildingProductionRequestBoundary.Context(
            runtimeBuildings,
            configuredSpawnableDefinitions,
            configuredDefinitionsByPrefab,
            unitPrefabs,
            unitPrefabsByKey,
            100000,
            productionSystem,
            queueContext,
            null,
            BuildingDefinitionSystem.GetProductionPrefab,
            null,
            beginPlacement,
            trySpendDollars,
            refundDollars,
            setActivePlacementCost,
            null,
            _ => { },
            () => { },
            () => { },
            () => { },
            _ => { },
            _ => Vector3.zero,
            _ => { },
            Debug.LogWarning,
            (_, _) => 0,
            (_, _) => 0);
    }

    private static BuildingProductionRequestBoundary.Context CreateProducerSelectionContext(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        BuildingProductionSystem productionSystem,
        GameObject unitPrefab,
        EntityManager entityManager = default,
        BuildingProductionRequestBoundary.TryQueuePlayerUnitDelegate tryQueuePlayerUnit = null)
    {
        var unitPrefabs = new List<GameObject> { unitPrefab };
        BuildingProductionSystem.QueueContext queueContext = new(
            unitPrefabs,
            new Dictionary<string, GameObject>(),
            new BuildingProductionSlotSystem(),
            null,
            null);

        return new BuildingProductionRequestBoundary.Context(
            runtimeBuildings,
            null,
            null,
            unitPrefabs,
            new Dictionary<string, GameObject>(),
            100000,
            productionSystem,
            queueContext,
            null,
            BuildingDefinitionSystem.GetProductionPrefab,
            null,
            null,
            _ => true,
            _ => { },
            _ => { },
            tryQueuePlayerUnit ?? ((building, productionIndex, spawnUnitPrefab) => productionSystem.TryQueuePlayerUnitFromBuilding(
                queueContext,
                building,
                productionIndex,
                spawnUnitPrefab,
                entityManager,
                10f)),
            _ => { },
            () => { },
            () => { },
            () => { },
            _ => { },
            _ => Vector3.zero,
            _ => { },
            Debug.LogWarning,
            (_, _) => 0,
            (_, _) => 0);
    }

    private static BuildingRuntimeQuerySystem.Context CreateRuntimeQueryContext(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        EntityManager entityManager,
        BuildingProductionSystem productionSystem)
    {
        bool TryGetEntityManager(out EntityManager em)
        {
            em = entityManager;
            return entityManager.World != null && entityManager.World.IsCreated;
        }

        return new BuildingRuntimeQuerySystem.Context(
            runtimeBuildings,
            TryGetEntityManager,
            productionSystem,
            BuildingDefinitionSystem.NormalizeSpawnableKey,
            _ => false,
            (building, normalizedId) => BuildingDefinitionSystem.RuntimeDefinitionMatchesId(building?.Definition, normalizedId),
            (prefab, normalizedId) => BuildingDefinitionSystem.NormalizeSpawnableKey(prefab != null ? prefab.name : string.Empty) == normalizedId,
            (RuntimeBuildingEntity building, out Vector3 worldPosition) =>
            {
                worldPosition = Vector3.zero;
                return false;
            },
            (RuntimeBuildingEntity building, int2 unitFootprint, int2 referenceCell, out int2 goal) =>
            {
                goal = default;
                return false;
            },
            (RuntimeBuildingEntity building, int2 currentCell, int2 unitFootprint) => false,
            definition => false,
            (
                byte attackerFactionId,
                Entity finalTarget,
                int2 finalTargetCell,
                int2 attackerCell,
                out Entity breachTarget,
                out int2 breachCell,
                out float3 breachPosition,
                out string reason) =>
            {
                breachTarget = Entity.Null;
                breachCell = default;
                breachPosition = default;
                reason = string.Empty;
                return false;
            });
    }

    private static FieldInfo FindPrivateField(System.Type type, string fieldName)
    {
        for (System.Type current = type; current != null; current = current.BaseType)
        {
            FieldInfo field = current.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return field;
        }

        return null;
    }
}
#endif
