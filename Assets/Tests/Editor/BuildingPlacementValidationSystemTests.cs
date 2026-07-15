using Game.Components;
using Game.Runtime;
using Game.Tactical.Contracts;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingPlacementValidationUtilitySystemHelperTests
{
    private World _world;

    public static void RunPlacementCommandRequestValidation()
    {
        try
        {
            var tests = new BuildingPlacementValidationUtilitySystemHelperTests();
            tests.BuildingUiPlacementCommandRequest_RejectsMissingSession();
            tests.BuildingUiPlacementCommandRequest_ConfirmRejectsMissingActivePlacement();
            tests.BuildingUiPlacementCommandRequest_ConfirmRejectsBlockedPlacement();
            tests.BuildingUiPlacementCommandRequest_ConfirmRejectsInvalidPlacement();
            tests.BuildingUiPlacementCommandRequest_ConfirmRejectsNotEnoughMoney();
            tests.BuildingUiPlacementCommandRequest_ConfirmWritesAcceptedResult();
            tests.BuildingUiPlacementCommandRequest_CancelWritesAcceptedResult();
            tests.BuildingUiPlacementCommandRequest_RotateWritesAcceptedResult();
            tests.BuildingUiPlacementCommandRequest_ExitBuildModeHonorsClearSelectionFlag();
            tests.BuildingUiPlacementCommandRequest_BeginConfiguredPlacementWritesAcceptedResult();
            tests.BuildingUiPlacementCommandRequest_BeginConfiguredPlacementRejectsMissingConfig();
            tests.BuildingPlacementCommandResultMapper_MapsConfirmFailureResultCodes();
            tests.BuildingPlacementCommandResultMapper_MapsTacticalReasonCodes();
            tests.BuildingUiPlacementCommandEntityCache_ReusesWarmPositiveAndNegativeLookupsWithoutManagedAllocation();
            tests.BuildingUiPlacementCommandEntityCache_RebindsWhenWorldChanges();
            tests.BuildingUiPlacementCommandEntityCache_RecoversDestroyedEntityAndRepairsBuffers();
            tests.BuildingUiPlacementEconomyTransactionId_SurvivesQueueEntityRecreation();
            tests.BuildingPlacementInputRuntimeTick_ProcessesQueuedPlacementCommandBeforeCameraGate();
            tests.BuildingPlacementInputScratchLists_ReuseImmediatePreviewStorageWithoutSharingOwnedResults();
            Debug.Log("[BuildingPlacementCommandRequestValidation] result=Passed tests=19");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingPlacementCommandRequestValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void BuildingPlacementCommandResultMapper_MapsConfirmFailureResultCodes()
    {
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.Rejected,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.None));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.MissingActivePlacement,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.MissingActivePlacement));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.BlockedPlacement,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.BlockedPlacement));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.InvalidPlacement,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InvalidPlacement));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.NotEnoughMoney,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InsufficientCredits));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.InsufficientMaterials,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InsufficientMaterials));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.InsufficientCreditsAndMaterials,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InsufficientCreditsAndMaterials));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.DuplicateTransaction,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.DuplicateTransaction));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.TransactionRejected,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.TransactionRejected));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.RegistrationFailed,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.RegistrationFailed));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.Rejected,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                (BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason)int.MaxValue));
    }

    [Test]
    public void BuildingPlacementCommandResultMapper_MapsTacticalReasonCodes()
    {
        Assert.AreEqual(
            TacticalCommandReasonCode.None,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.Completed));
        Assert.AreEqual(
            TacticalCommandReasonCode.TargetBlocked,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.BlockedPlacement));
        Assert.AreEqual(
            TacticalCommandReasonCode.TargetUnreachable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.InvalidPlacement));
        Assert.AreEqual(
            TacticalCommandReasonCode.InsufficientResources,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.NotEnoughMoney));
        Assert.AreEqual(
            TacticalCommandReasonCode.InsufficientResources,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.InsufficientMaterials));
        Assert.AreEqual(
            TacticalCommandReasonCode.InsufficientResources,
            BuildingPlacementCommandResultMapper.ToReasonCode(
                BuildingUiPlacementCommandResultElement.InsufficientCreditsAndMaterials));
        Assert.AreEqual(
            TacticalCommandReasonCode.BuildUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.MissingActivePlacement));
        Assert.AreEqual(
            TacticalCommandReasonCode.BuildUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.MissingConfig));
        Assert.AreEqual(
            TacticalCommandReasonCode.CommandUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.MissingSession));
        Assert.AreEqual(
            TacticalCommandReasonCode.CommandUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.Rejected));
        Assert.AreEqual(
            TacticalCommandReasonCode.CommandUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.DuplicateTransaction));
        Assert.AreEqual(
            TacticalCommandReasonCode.CommandUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.RegistrationFailed));
        Assert.AreEqual(
            TacticalCommandReasonCode.CommandUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.TransactionRejected));
        Assert.AreEqual(
            TacticalCommandReasonCode.CommandUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(byte.MaxValue));
    }

    [Test]
    public void BuildingUiPlacementCommandEntityCache_ReusesWarmPositiveAndNegativeLookupsWithoutManagedAllocation()
    {
        using World world = new("BuildingUiPlacementCommandEntityCacheAllocationTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();

        commandSystem.ProcessPendingUiPlacementCommandsIfPresent(world.EntityManager, default);
        long idleAllocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 300; i++)
        {
            commandSystem.ProcessPendingUiPlacementCommandsIfPresent(world.EntityManager, default);
            commandSystem.HasPendingUiPlacementCommands(world.EntityManager);
        }
        long idleAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - idleAllocationStart;

        commandSystem.TryGetUiPlacementCommandResult(world.EntityManager, -1, out _);
        long queueAllocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 300; i++)
            commandSystem.TryGetUiPlacementCommandResult(world.EntityManager, -1, out _);
        long queueAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - queueAllocationStart;

        Assert.AreEqual(0L, idleAllocatedBytes, "Warm idle placement command polling must reuse the negative queue lookup.");
        Assert.AreEqual(0L, queueAllocatedBytes, "Warm placement command reads must reuse the queue entity.");
    }

    [Test]
    public void BuildingUiPlacementCommandEntityCache_RebindsWhenWorldChanges()
    {
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        using (World firstWorld = new("BuildingUiPlacementCommandEntityCacheRebind.First"))
            commandSystem.TryGetUiPlacementCommandResult(firstWorld.EntityManager, -1, out _);

        using (World secondWorld = new("BuildingUiPlacementCommandEntityCacheRebind.Second"))
        {
            commandSystem.TryGetUiPlacementCommandResult(secondWorld.EntityManager, -1, out _);
            Assert.AreEqual(1, CountEntitiesWith<BuildingUiPlacementCommandQueueComponent>(secondWorld.EntityManager));
        }
    }

    [Test]
    public void BuildingUiPlacementCommandEntityCache_RecoversDestroyedEntityAndRepairsBuffers()
    {
        using World world = new("BuildingUiPlacementCommandEntityCacheRecoveryTest");
        EntityManager em = world.EntityManager;
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();

        commandSystem.TryGetUiPlacementCommandResult(em, -1, out _);
        Entity cachedEntity = GetSingletonEntity<BuildingUiPlacementCommandQueueComponent>(em);
        em.DestroyEntity(cachedEntity);
        Entity adoptedEntity = em.CreateEntity(typeof(BuildingUiPlacementCommandQueueComponent));

        commandSystem.TryGetUiPlacementCommandResult(em, -1, out _);

        Assert.IsTrue(em.HasBuffer<BuildingUiPlacementCommandRequestElement>(adoptedEntity));
        Assert.IsTrue(em.HasBuffer<BuildingUiPlacementCommandResultElement>(adoptedEntity));
    }

    [Test]
    public void BuildingUiPlacementEconomyTransactionId_SurvivesQueueEntityRecreation()
    {
        using World world = new(nameof(BuildingUiPlacementEconomyTransactionId_SurvivesQueueEntityRecreation));
        EntityManager em = world.EntityManager;
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();

        commandSystem.EnqueueConfirmBuildingPlacement(em);
        Entity firstQueue = GetSingletonEntity<BuildingUiPlacementCommandQueueComponent>(em);
        int firstTransactionId = em.GetBuffer<BuildingUiPlacementCommandRequestElement>(firstQueue)[0]
            .EconomyTransactionId;
        em.DestroyEntity(firstQueue);

        commandSystem.EnqueueConfirmBuildingPlacement(em);
        Entity replacementQueue = GetSingletonEntity<BuildingUiPlacementCommandQueueComponent>(em);
        int replacementTransactionId = em.GetBuffer<BuildingUiPlacementCommandRequestElement>(replacementQueue)[0]
            .EconomyTransactionId;

        Assert.Greater(firstTransactionId, 0);
        Assert.Greater(replacementTransactionId, firstTransactionId);
    }

    private static int CountEntitiesWith<T>(EntityManager entityManager)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return query.CalculateEntityCount();
    }

    private static Entity GetSingletonEntity<T>(EntityManager entityManager)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return query.GetSingletonEntity();
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
        _world = null;
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_RejectsMissingSession()
    {
        using World world = new("BuildingUiPlacementCommandMissingSessionTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = default;

        int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            world.EntityManager,
            requestId,
            out BuildingUiPlacementCommandResultElement result));
        Assert.AreEqual(0, result.Accepted);
        Assert.AreEqual(BuildingUiPlacementCommandRequestElement.KindConfirmPlacement, result.RequestKind);
        Assert.AreEqual(BuildingUiPlacementCommandResultElement.MissingSession, result.ResultCode);
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ConfirmRejectsMissingActivePlacement()
    {
        using World world = new("BuildingUiPlacementCommandMissingActivePlacementTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreatePlacementCommandContext(
            new BuildingPlacementSessionCompositionSystemHelper());

        int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        AssertPlacementResult(
            world.EntityManager,
            commandSystem,
            requestId,
            BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
            accepted: false,
            BuildingUiPlacementCommandResultElement.MissingActivePlacement);
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ConfirmRejectsBlockedPlacement()
    {
        using World world = new("BuildingUiPlacementCommandBlockedPlacementTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreateActivePlacementCommandContext(
            out _,
            out GameObject prefab,
            out GameObject root,
            placementIsValid: false);

        try
        {
            context.SessionSystem.BeginPlacement(
                context.SessionContext,
                new BuildingDefinition
                {
                    Prefab = prefab,
                    FootprintCells = new Vector2Int(1, 1)
                });

            int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
            commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

            AssertPlacementResult(
                world.EntityManager,
                commandSystem,
                requestId,
                BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
                accepted: false,
                BuildingUiPlacementCommandResultElement.BlockedPlacement);
            Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiPlacementCommandResultElement typedResult));
            Assert.AreEqual((int)TacticalCommandReasonCode.TargetBlocked, typedResult.ReasonCode);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ConfirmRejectsInvalidPlacement()
    {
        using World world = new("BuildingUiPlacementCommandInvalidPlacementTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreateActivePlacementCommandContext(
            out _,
            out GameObject prefab,
            out GameObject root,
            validateConfirm: _ => false);

        try
        {
            context.SessionSystem.BeginPlacement(
                context.SessionContext,
                new BuildingDefinition
                {
                    Prefab = prefab,
                    FootprintCells = new Vector2Int(1, 1)
                });

            int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
            commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

            AssertPlacementResult(
                world.EntityManager,
                commandSystem,
                requestId,
                BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
                accepted: false,
                BuildingUiPlacementCommandResultElement.InvalidPlacement);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ConfirmRejectsNotEnoughMoney()
    {
        using World world = new("BuildingUiPlacementCommandNotEnoughMoneyTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreateActivePlacementCommandContext(
            out _,
            out GameObject prefab,
            out GameObject root,
            trySpendCost: _ => false);

        try
        {
            context.SessionSystem.BeginPlacement(
                context.SessionContext,
                new BuildingDefinition
                {
                    Prefab = prefab,
                    FootprintCells = new Vector2Int(1, 1),
                    CreditsCost = 50
                });

            int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
            commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

            AssertPlacementResult(
                world.EntityManager,
                commandSystem,
                requestId,
                BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
                accepted: false,
                BuildingUiPlacementCommandResultElement.NotEnoughMoney);
            Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiPlacementCommandResultElement typedResult));
            Assert.AreEqual((int)TacticalCommandReasonCode.InsufficientResources, typedResult.ReasonCode);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ConfirmWritesAcceptedResult()
    {
        using World world = new("BuildingUiPlacementCommandConfirmTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        int commitCount = 0;
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreateActivePlacementCommandContext(
            out BuildingPlacementLifecycleCompositionSystemHelper lifecycleSystem,
            out GameObject prefab,
            out GameObject root,
            commitPlacement: _ => commitCount++);

        try
        {
            context.SessionSystem.BeginPlacement(
                context.SessionContext,
                new BuildingDefinition
                {
                    Prefab = prefab,
                    FootprintCells = new Vector2Int(1, 1)
                });

            int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
            commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

            AssertPlacementResult(
                world.EntityManager,
                commandSystem,
                requestId,
                BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
                accepted: true,
                BuildingUiPlacementCommandResultElement.Completed);
            Assert.AreEqual(1, commitCount);
            Assert.IsFalse(lifecycleSystem.HasPendingBuildingPlacement);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_CancelWritesAcceptedResult()
    {
        using World world = new("BuildingUiPlacementCommandCancelTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        bool commandModeCleared = false;
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreatePlacementCommandContext(
            new BuildingPlacementSessionCompositionSystemHelper(),
            clearCommandMode: () => commandModeCleared = true);

        int requestId = commandSystem.EnqueueCancelBuildingPlacement(world.EntityManager);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            world.EntityManager,
            requestId,
            out BuildingUiPlacementCommandResultElement result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(BuildingUiPlacementCommandRequestElement.KindCancelPlacement, result.RequestKind);
        Assert.AreEqual(BuildingUiPlacementCommandResultElement.Completed, result.ResultCode);
        Assert.IsTrue(commandModeCleared);

        using EntityQuery queueQuery = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<BuildingUiPlacementCommandQueueComponent>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        Assert.AreEqual(0, world.EntityManager.GetBuffer<BuildingUiPlacementCommandRequestElement>(queueEntity).Length);
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_RotateWritesAcceptedResult()
    {
        using World world = new("BuildingUiPlacementCommandRotateTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        int updateCount = 0;
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreateActivePlacementCommandContext(
            out BuildingPlacementLifecycleCompositionSystemHelper lifecycleSystem,
            out GameObject prefab,
            out GameObject root,
            updatePlacement: _ => updateCount++);

        try
        {
            context.SessionSystem.BeginPlacement(
                context.SessionContext,
                new BuildingDefinition
                {
                    Prefab = prefab,
                    FootprintCells = new Vector2Int(1, 1)
                });

            Assert.IsNotNull(lifecycleSystem.ActivePlacement);
            Assert.IsFalse(lifecycleSystem.ActivePlacement.AutoRotateVertical);

            int requestId = commandSystem.EnqueueRotateBuildingPlacement(world.EntityManager);
            commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

            Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiPlacementCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiPlacementCommandRequestElement.KindRotatePlacement, result.RequestKind);
            Assert.AreEqual(BuildingUiPlacementCommandResultElement.Completed, result.ResultCode);
            Assert.IsNotNull(lifecycleSystem.ActivePlacement);
            Assert.IsTrue(lifecycleSystem.ActivePlacement.AutoRotateVertical);
            Assert.AreEqual(2, updateCount);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ExitBuildModeHonorsClearSelectionFlag()
    {
        using World world = new("BuildingUiPlacementCommandExitBuildModeTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        int clearSelectionCount = 0;
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreatePlacementCommandContext(
            new BuildingPlacementSessionCompositionSystemHelper(),
            clearSelectedBuilding: _ => clearSelectionCount++);

        int preservedSelectionRequestId = commandSystem.EnqueueExitBuildMode(
            world.EntityManager,
            clearBuildingSelection: false);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            world.EntityManager,
            preservedSelectionRequestId,
            out BuildingUiPlacementCommandResultElement preservedSelectionResult));
        Assert.AreEqual(1, preservedSelectionResult.Accepted);
        Assert.AreEqual(BuildingUiPlacementCommandResultElement.Completed, preservedSelectionResult.ResultCode);
        Assert.AreEqual(0, clearSelectionCount);

        int clearSelectionRequestId = commandSystem.EnqueueExitBuildMode(
            world.EntityManager,
            clearBuildingSelection: true);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            world.EntityManager,
            clearSelectionRequestId,
            out BuildingUiPlacementCommandResultElement clearSelectionResult));
        Assert.AreEqual(1, clearSelectionResult.Accepted);
        Assert.AreEqual(BuildingUiPlacementCommandResultElement.Completed, clearSelectionResult.ResultCode);
        Assert.AreEqual(1, clearSelectionCount);
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_BeginConfiguredPlacementWritesAcceptedResult()
    {
        using World world = new("BuildingUiPlacementCommandBeginConfiguredTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        var definitionSystem = new BuildingDefinitionPrefabSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreateActivePlacementCommandContext(
            out BuildingPlacementLifecycleCompositionSystemHelper lifecycleSystem,
            out GameObject prefab,
            out GameObject root,
            definitionSystem: definitionSystem);

        try
        {
            definitionSystem.RebuildSpawnablesLookup(new List<GameObject> { prefab }, null);
            definitionSystem.RebuildConfiguredSpawnableDefinitions(null, UnityEngine.Object.DestroyImmediate);

            int requestId = commandSystem.EnqueueBeginConfiguredPlacement(world.EntityManager, prefab.name);
            commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

            AssertPlacementResult(
                world.EntityManager,
                commandSystem,
                requestId,
                BuildingUiPlacementCommandRequestElement.KindBeginConfiguredPlacement,
                accepted: true,
                BuildingUiPlacementCommandResultElement.Completed);
            Assert.IsTrue(lifecycleSystem.HasPendingBuildingPlacement);
            Assert.AreEqual(prefab, lifecycleSystem.ActivePlacement.Definition.Prefab);
        }
        finally
        {
            definitionSystem.ClearConfiguredSpawnableDefinitions(UnityEngine.Object.DestroyImmediate);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_BeginConfiguredPlacementRejectsMissingConfig()
    {
        using World world = new("BuildingUiPlacementCommandBeginConfiguredMissingConfigTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreatePlacementCommandContext(
            new BuildingPlacementSessionCompositionSystemHelper());

        int requestId = commandSystem.EnqueueBeginConfiguredPlacement(world.EntityManager, "missing-building");
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        AssertPlacementResult(
            world.EntityManager,
            commandSystem,
            requestId,
            BuildingUiPlacementCommandRequestElement.KindBeginConfiguredPlacement,
            accepted: false,
            BuildingUiPlacementCommandResultElement.MissingConfig);

        using EntityQuery queueQuery = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<BuildingUiPlacementCommandQueueComponent>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        Assert.AreEqual(0, world.EntityManager.GetBuffer<BuildingUiPlacementCommandRequestElement>(queueEntity).Length);
    }

    [Test]
    public void BuildingPlacementInputRuntimeTick_ProcessesQueuedPlacementCommandBeforeCameraGate()
    {
        using World world = new("BuildingPlacementInputTickQueuedPlacementCommandTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context commandContext = CreatePlacementCommandContext(
            new BuildingPlacementSessionCompositionSystemHelper());
        int requestId = commandSystem.EnqueueCancelBuildingPlacement(world.EntityManager);

        var tickSystem = new BuildingPlacementInputRuntimeTickUiSystemHelper();
        BuildingPlacementInputRuntimeTickUiSystemHelper.Context tickContext = new(
            getWorldCamera: () => null,
            getActivePlacement: () => null,
            placementInputSystem: null,
            activePlacementPointerContext: default,
            isPlayRequested: () => false,
            isBuildModeActive: () => false,
            placementPreviewSystem: null,
            hasActiveBuilding: () => false,
            runtimeGameplayStateSystem: new RuntimeGameplayStateSystem(),
            getMainMenu: () => null,
            selectionClickSystem: null,
            selectionClickContext: default,
            shouldBlockBuildingSelectionClick: () => false,
            clickDragThresholdPixels: 8f,
            processPendingPlacementCommands: () => commandSystem.ProcessPendingUiPlacementCommandsIfPresent(
                world.EntityManager,
                commandContext));

        tickSystem.Update(tickContext);

        AssertPlacementResult(
            world.EntityManager,
            commandSystem,
            requestId,
            BuildingUiPlacementCommandRequestElement.KindCancelPlacement,
            accepted: true,
            BuildingUiPlacementCommandResultElement.Completed);
    }

    [Test]
    public void PlacementRectValidation_RejectsRoadCellsAndRuntimeBuildingOverlap()
    {
        CreateRoadBuffer(4, 4, out GridConfig grid, out DynamicBuffer<GridRoad> roads);
        roads[GridUtils.CellToIndex(new Unity.Mathematics.int2(1, 1), grid.Width)] = new GridRoad { Value = 1 };
        DynamicBlockerComponent blockerData = default;

        Assert.IsFalse(BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(
            new RectInt(1, 1, 1, 1),
            grid,
            roads,
            blockerData,
            false,
            null,
            0,
            0,
            null,
            null,
            null));

        Assert.IsFalse(BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(
            new RectInt(2, 2, 1, 1),
            grid,
            roads,
            blockerData,
            false,
            null,
            0,
            0,
            null,
            null,
            rect => rect.position == new Vector2Int(2, 2)));
    }

    [Test]
    public void PlacementRectValidation_AllowsRuntimeBlockerCellsButRejectsStaticBlockers()
    {
        CreateRoadBuffer(4, 4, out GridConfig grid, out DynamicBuffer<GridRoad> roads);
        int blockedIndex = GridUtils.CellToIndex(new Unity.Mathematics.int2(1, 2), grid.Width);
        var blocked = new NativeBitArray(16, Allocator.Persistent);
        blocked.Set(blockedIndex, true);
        DynamicBlockerComponent blockerData = new() { GridSize = 16, Blocked = blocked };

        try
        {
            Assert.IsFalse(BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(
                new RectInt(1, 2, 1, 1),
                grid,
                roads,
                blockerData,
                false,
                null,
                0,
                0,
                null,
                null,
                null));

            Assert.IsTrue(BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(
                new RectInt(1, 2, 1, 1),
                grid,
                roads,
                blockerData,
                false,
                null,
                0,
                0,
                (x, y, _, _) => x == 1 && y == 2,
                null,
                null));
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
        }
    }

    [Test]
    public void InvalidPrefix_DetectsRoadFootprintMaskAndOutOfBounds()
    {
        CreateRoadBuffer(4, 4, out GridConfig grid, out DynamicBuffer<GridRoad> roads);
        bool[] roadFootprintMask = new bool[16];
        roadFootprintMask[GridUtils.CellToIndex(new Unity.Mathematics.int2(2, 1), grid.Width)] = true;
        int[] prefix = null;

        BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(
            grid,
            roads,
            default,
            roadFootprintMask,
            null,
            ref prefix,
            out int prefixWidth,
            out int prefixHeight,
            out bool hasPrefix);

        Assert.IsTrue(hasPrefix);
        Assert.IsFalse(BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(prefix, prefixWidth, prefixHeight, new Vector2Int(0, 0), new Vector2Int(1, 1)));
        Assert.IsTrue(BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(prefix, prefixWidth, prefixHeight, new Vector2Int(2, 1), new Vector2Int(1, 1)));
        Assert.IsTrue(BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(prefix, prefixWidth, prefixHeight, new Vector2Int(4, 4), new Vector2Int(1, 1)));
    }

    [Test]
    public void WallSegmentConflict_OnlyRejectsOverlappingSameAxisSegments()
    {
        Assert.IsTrue(BuildingPlacementValidationUtilitySystemHelper.DoWallSegmentsConflict(
            new Vector2Int(1, 1),
            new Vector2Int(1, 4),
            true,
            new Vector2Int(1, 3),
            new Vector2Int(1, 4),
            true));

        Assert.IsFalse(BuildingPlacementValidationUtilitySystemHelper.DoWallSegmentsConflict(
            new Vector2Int(1, 1),
            new Vector2Int(1, 4),
            true,
            new Vector2Int(1, 3),
            new Vector2Int(4, 1),
            false));

        Assert.IsFalse(BuildingPlacementValidationUtilitySystemHelper.DoWallSegmentsConflict(
            new Vector2Int(1, 1),
            new Vector2Int(1, 4),
            true,
            new Vector2Int(3, 1),
            new Vector2Int(1, 4),
            true));
    }

    [Test]
    public void BuildingPlacementInputScratchLists_ReuseImmediatePreviewStorageWithoutSharingOwnedResults()
    {
        var input = new BuildingPlacementInputUiSystemHelper();
        var placement = new ScratchPlacementState
        {
            Definition = new BuildingDefinition { FootprintCells = new Vector2Int(1, 1) },
            DragStartOriginCell = new Vector2Int(1, 1),
            DragCurrentOriginCell = new Vector2Int(4, 1)
        };

        IReadOnlyList<Vector2Int> firstScratch = input.BuildWallPlacementOriginsScratch(placement, UnitFootprint);
        Assert.AreEqual(4, firstScratch.Count);
        Assert.AreEqual(new Vector2Int(4, 1), firstScratch[3]);

        List<Vector2Int> owned = input.BuildWallPlacementOrigins(placement, UnitFootprint);
        Assert.AreEqual(firstScratch.Count, owned.Count);
        Assert.AreNotSame(firstScratch, owned);

        placement.DragCurrentOriginCell = new Vector2Int(2, 1);
        IReadOnlyList<Vector2Int> secondScratch = input.BuildWallPlacementOriginsScratch(placement, UnitFootprint);
        Assert.AreSame(firstScratch, secondScratch);
        Assert.AreEqual(2, secondScratch.Count);
        Assert.AreEqual(4, owned.Count);
        Assert.AreEqual(new Vector2Int(4, 1), owned[3]);

        IReadOnlyList<BuildingPlacementInputUiSystemHelper.WallRun> firstRuns = input.BuildFinalWallRunsScratch(placement, UnitFootprint);
        IReadOnlyList<BuildingPlacementInputUiSystemHelper.WallRun> secondRuns = input.BuildFinalWallRunsScratch(placement, UnitFootprint);
        Assert.AreSame(firstRuns, secondRuns);
        Assert.AreEqual(1, secondRuns.Count);
        Assert.AreEqual(secondScratch.Count, secondRuns[0].Origins.Count);
        Assert.AreEqual(secondScratch[1], secondRuns[0].Origins[1]);
    }

    private static Vector2Int UnitFootprint(BuildingDefinition _, bool __)
    {
        return new Vector2Int(1, 1);
    }

    private static BuildingPlacementCommandRequestCompositionSystemHelper.Context CreateActivePlacementCommandContext(
        out BuildingPlacementLifecycleCompositionSystemHelper lifecycleSystem,
        out GameObject prefab,
        out GameObject root,
        bool placementIsValid = true,
        Action<BuildingPlacementLifecycleCompositionSystemHelper.PlacementState> updatePlacement = null,
        Action<BuildingPlacementLifecycleCompositionSystemHelper.PlacementState> commitPlacement = null,
        BuildingPlacementLifecycleCompositionSystemHelper.ValidateConfirmDelegate validateConfirm = null,
        Func<int, bool> trySpendCost = null,
        BuildingDefinitionPrefabSystemHelper definitionSystem = null)
    {
        var runtimeStateSystem = new RuntimeGameplayStateSystem();
        lifecycleSystem = new BuildingPlacementLifecycleCompositionSystemHelper();
        var sessionSystem = new BuildingPlacementSessionCompositionSystemHelper();
        prefab = new GameObject("PlacementCommandTestPrefab");
        root = new GameObject("PlacementCommandTestRoot");
        Transform rootTransform = root.transform;
        BuildingPlacementLifecycleCompositionSystemHelper activeLifecycleSystem = lifecycleSystem;

        BuildingPlacementLifecycleCompositionSystemHelper.UpdatePlacementVisualDelegate updatePlacementVisual =
            (placement, _, _) =>
            {
                placement.IsValid = placementIsValid;
                updatePlacement?.Invoke(placement);
            };

        BuildingPlacementSessionCompositionSystemHelper.Context sessionContext = new(
            runtimeStateSystem,
            activeLifecycleSystem,
            null,
            null,
            () => new BuildingPlacementLifecycleCompositionSystemHelper.CancelContext(null, null, UnityEngine.Object.DestroyImmediate),
            () => new BuildingPlacementLifecycleCompositionSystemHelper.BeginContext(
                runtimeStateSystem,
                null,
                null,
                rootTransform,
                null,
                UnityEngine.Object.DestroyImmediate,
                _ => Vector2Int.zero,
                null,
                updatePlacementVisual,
                null,
                null,
                null),
            () => new BuildingPlacementLifecycleCompositionSystemHelper.ConfirmContext(
                validateConfirm ?? (placement => placement.IsValid),
                (_, creditsCost, _) => trySpendCost?.Invoke(creditsCost) == false
                    ? FactionConstructionResourceMutationResult.InsufficientCredits
                    : FactionConstructionResourceMutationResult.Applied,
                _ => FactionConstructionResourceMutationResult.Applied,
                _ => FactionConstructionResourceMutationResult.Applied,
                placement =>
                {
                    commitPlacement?.Invoke(placement);
                    return new BuildingPlacementCommitCompositionSystemHelper.CommitOutcome(null, 1);
                }),
            () => new BuildingPlacementLifecycleCompositionSystemHelper.RotateContext(updatePlacementVisual),
            null,
            null,
            null,
            null);

        return new BuildingPlacementCommandRequestCompositionSystemHelper.Context(
            null,
            definitionSystem,
            sessionSystem,
            sessionContext,
            null);
    }

    private static void AssertPlacementResult(
        EntityManager em,
        BuildingPlacementCommandRequestCompositionSystemHelper commandSystem,
        int requestId,
        byte requestKind,
        bool accepted,
        byte resultCode)
    {
        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            em,
            requestId,
            out BuildingUiPlacementCommandResultElement result));
        Assert.AreEqual(accepted ? 1 : 0, result.Accepted);
        Assert.AreEqual(requestKind, result.RequestKind);
        Assert.AreEqual(resultCode, result.ResultCode);
        if (requestKind == BuildingUiPlacementCommandRequestElement.KindConfirmPlacement)
            Assert.Greater(result.EconomyTransactionId, 0);
        else
            Assert.AreEqual(0, result.EconomyTransactionId);
    }

    private static BuildingPlacementCommandRequestCompositionSystemHelper.Context CreatePlacementCommandContext(
        BuildingPlacementSessionCompositionSystemHelper sessionSystem,
        Action<string> clearSelectedBuilding = null,
        Action clearCommandMode = null)
    {
        var runtimeStateSystem = new RuntimeGameplayStateSystem();
        var lifecycleSystem = new BuildingPlacementLifecycleCompositionSystemHelper();
        BuildingPlacementSessionCompositionSystemHelper.Context sessionContext = new(
            runtimeStateSystem,
            lifecycleSystem,
            null,
            null,
            () => new BuildingPlacementLifecycleCompositionSystemHelper.CancelContext(null, null, null),
            () => default,
            () => default,
            () => default,
            null,
            null,
            clearSelectedBuilding,
            clearCommandMode);

        return new BuildingPlacementCommandRequestCompositionSystemHelper.Context(
            null,
            null,
            sessionSystem,
            sessionContext,
            null);
    }

    private void CreateRoadBuffer(int width, int height, out GridConfig grid, out DynamicBuffer<GridRoad> roads)
    {
        _world ??= new World("BuildingPlacementValidationUtilitySystemHelperTests");
        Entity entity = _world.EntityManager.CreateEntity();
        roads = _world.EntityManager.AddBuffer<GridRoad>(entity);
        roads.ResizeUninitialized(width * height);
        for (int i = 0; i < roads.Length; i++)
            roads[i] = default;

        grid = new GridConfig
        {
            Width = width,
            Height = height,
            CellSize = 1f,
            Origin = default
        };
    }

    private sealed class ScratchPlacementState : BuildingPlacementInputUiSystemHelper.IPlacementState
    {
        public BuildingDefinition Definition { get; set; }
        public Vector2Int OriginCell { get; set; }
        public Vector2Int CommittedOriginCell { get; set; }
        public Vector2Int DragStartOriginCell { get; set; }
        public Vector2Int DragCurrentOriginCell { get; set; }
        public BuildingPlacementInputUiSystemHelper.DragFirstAxis DragFirstAxis { get; set; }
        public bool HideCurrentWallPreview { get; set; }
        public bool IsValid { get; set; } = true;
        public float LastPointerMovedAt { get; set; }
        public Vector2 LastPointerScreenPosition { get; set; }
        public List<BuildingPlacementInputUiSystemHelper.WallRun> CommittedWallRuns { get; set; }
    }
}
#endif
