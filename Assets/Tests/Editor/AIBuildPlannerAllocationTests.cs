using Game.Components;
using Game.Runtime;

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public sealed class AIBuildPlannerAllocationTests
{
    private const int WarmupCalls = 16;
    private const int MeasuredCalls = 512;

    [Test]
    public void SelectBuildDecision_WarmedNormalizedRequestPathDoesNotAllocateManagedMemory()
    {
        using World world = new(nameof(SelectBuildDecision_WarmedNormalizedRequestPathDoesNotAllocateManagedMemory));
        EntityManager entityManager = world.EntityManager;
        Entity planEntity = entityManager.CreateEntity(typeof(AIBuildPlanEntry));
        Entity boundaryEntity = CreateBoundaryEntity(entityManager);

        entityManager.GetBuffer<AIBuildPlanEntry>(planEntity).Add(new AIBuildPlanEntry
        {
            BuildingId = new FixedString64Bytes("   ")
        });
        entityManager.GetBuffer<AIBuildPlanEntry>(planEntity).Add(new AIBuildPlanEntry
        {
            BuildingId = new FixedString64Bytes("\u2003BARRACKS\u2003")
        });
        entityManager.GetBuffer<AIBuildPlanEntry>(planEntity).Add(new AIBuildPlanEntry
        {
            BuildingId = new FixedString64Bytes("TENT_REGULAR")
        });
        entityManager.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity).Add(
            new BuildingConfiguredSpawnableReadModel
            {
                BuildingId = new FixedString128Bytes("barracks"),
                DisplayName = new FixedString128Bytes("Barracks"),
                Price = 250,
                MaterialsCost = 75,
                CanRequest = 1
            });
        entityManager.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(boundaryEntity).Add(
            new BuildingRuntimeOwnedBuildingSummary
            {
                FactionId = 2,
                BuildingId = new FixedString128Bytes("tent_regular"),
                Count = 1
            });

        DynamicBuffer<AIBuildPlanEntry> entries = entityManager.GetBuffer<AIBuildPlanEntry>(planEntity, true);
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables =
            entityManager.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity, true);
        DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> ownedSummaries =
            entityManager.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(boundaryEntity, true);
        DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests =
            entityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity, true);
        AIBuildPlan plan = new()
        {
            FactionId = 2,
            NextBuildIndex = 2,
            BaseCenterCell = new int2(20, 30)
        };
        AIBuildPlannerSystem.BuildDecision decision = default;
        FactionEconomy economy = new() { FactionId = 2, Money = 500 };
        FactionTacticalMaterialsComponent materials = new()
        {
            FactionId = 2,
            Current = 100,
            Capacity = 100
        };

        for (int i = 0; i < WarmupCalls; i++)
        {
            decision = AIBuildPlannerSystem.SelectBuildDecision(
                entries,
                spawnables,
                ownedSummaries,
                spawnRequests,
                plan,
                economy,
                materials);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < MeasuredCalls; i++)
        {
            decision = AIBuildPlannerSystem.SelectBuildDecision(
                entries,
                spawnables,
                ownedSummaries,
                spawnRequests,
                plan,
                economy,
                materials);
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(AIBuildPlannerSystem.BuildDecisionResult.Request, decision.Result);
        Assert.AreEqual(1, decision.EntryIndex);
        Assert.AreEqual(new FixedString128Bytes("barracks"), decision.BuildingId);
        Assert.AreEqual(0, decision.Cost);
        Assert.AreEqual(75, decision.MaterialsCost);
        Assert.AreEqual(new int2(34, 30), decision.PreferredOrigin);
        Assert.AreEqual(
            0L,
            allocatedBytes,
            $"Warmed normalized build request decisions allocated {allocatedBytes} managed bytes over {MeasuredCalls} calls.");
    }

    [Test]
    public void SelectBuildDecision_WarmedOwnedSkipToPendingPathDoesNotAllocateManagedMemory()
    {
        using World world = new(nameof(SelectBuildDecision_WarmedOwnedSkipToPendingPathDoesNotAllocateManagedMemory));
        EntityManager entityManager = world.EntityManager;
        Entity planEntity = entityManager.CreateEntity(typeof(AIBuildPlanEntry));
        Entity boundaryEntity = CreateBoundaryEntity(entityManager);

        entityManager.GetBuffer<AIBuildPlanEntry>(planEntity).Add(new AIBuildPlanEntry
        {
            BuildingId = new FixedString64Bytes("OWNED")
        });
        entityManager.GetBuffer<AIBuildPlanEntry>(planEntity).Add(new AIBuildPlanEntry
        {
            BuildingId = new FixedString64Bytes(" WAITING ")
        });
        entityManager.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(boundaryEntity).Add(
            new BuildingRuntimeOwnedBuildingSummary
            {
                FactionId = 3,
                BuildingId = new FixedString128Bytes("owned"),
                Count = 1
            });
        entityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity).Add(
            new BuildingRuntimeSpawnRequest
            {
                FactionId = 3,
                BuildingId = new FixedString128Bytes("waiting"),
                Status = BuildingRuntimeSpawnRequest.Pending
            });

        DynamicBuffer<AIBuildPlanEntry> entries = entityManager.GetBuffer<AIBuildPlanEntry>(planEntity, true);
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables =
            entityManager.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity, true);
        DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> ownedSummaries =
            entityManager.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(boundaryEntity, true);
        DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests =
            entityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity, true);
        AIBuildPlan plan = new()
        {
            FactionId = 3,
            NextBuildIndex = 0,
            BaseCenterCell = new int2(10, 10)
        };
        AIBuildPlannerSystem.BuildDecision decision = default;
        FactionEconomy economy = new() { FactionId = 3, Money = 0 };
        FactionTacticalMaterialsComponent materials = new() { FactionId = 3, Capacity = 0 };

        for (int i = 0; i < WarmupCalls; i++)
        {
            decision = AIBuildPlannerSystem.SelectBuildDecision(
                entries,
                spawnables,
                ownedSummaries,
                spawnRequests,
                plan,
                economy,
                materials);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < MeasuredCalls; i++)
        {
            decision = AIBuildPlannerSystem.SelectBuildDecision(
                entries,
                spawnables,
                ownedSummaries,
                spawnRequests,
                plan,
                economy,
                materials);
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(1, decision.EntryIndex);
        Assert.AreEqual(new FixedString128Bytes("waiting"), decision.BuildingId);
        Assert.AreEqual(AIBuildPlannerSystem.BuildDecisionResult.Pending, decision.Result);
        Assert.AreEqual(
            0L,
            allocatedBytes,
            $"Warmed owned-skip and pending build decisions allocated {allocatedBytes} managed bytes over {MeasuredCalls} calls.");
    }

    [Test]
    public void SelectBuildDecision_UsesAuthoredMaterialsCostAndIgnoresLegacyPrice()
    {
        using World world = new(nameof(SelectBuildDecision_UsesAuthoredMaterialsCostAndIgnoresLegacyPrice));
        EntityManager entityManager = world.EntityManager;
        Entity planEntity = entityManager.CreateEntity(typeof(AIBuildPlanEntry));
        Entity boundaryEntity = CreateBoundaryEntity(entityManager);
        entityManager.GetBuffer<AIBuildPlanEntry>(planEntity).Add(new AIBuildPlanEntry
        {
            BuildingId = new FixedString64Bytes("DEPOT")
        });
        entityManager.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity).Add(
            new BuildingConfiguredSpawnableReadModel
            {
                BuildingId = new FixedString128Bytes("depot"),
                DisplayName = new FixedString128Bytes("Depot"),
                Price = 200,
                MaterialsCost = 80,
                CanRequest = 1
            });

        DynamicBuffer<AIBuildPlanEntry> entries = entityManager.GetBuffer<AIBuildPlanEntry>(planEntity, true);
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables =
            entityManager.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity, true);
        DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> ownedSummaries =
            entityManager.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(boundaryEntity, true);
        DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests =
            entityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity, true);
        AIBuildPlan plan = new() { FactionId = 4 };

        FactionEconomy economy = new() { FactionId = 4, Money = 500 };
        FactionTacticalMaterialsComponent materials = new()
        {
            FactionId = 4,
            Current = 79,
            Capacity = 100
        };
        AIBuildPlannerSystem.BuildDecision materialsDecision = AIBuildPlannerSystem.SelectBuildDecision(
            entries,
            spawnables,
            ownedSummaries,
            spawnRequests,
            plan,
            economy,
            materials);

        economy.Money = 199;
        materials.Current = 79;
        AIBuildPlannerSystem.BuildDecision combinedDecision = AIBuildPlannerSystem.SelectBuildDecision(
            entries,
            spawnables,
            ownedSummaries,
            spawnRequests,
            plan,
            economy,
            materials);

        Assert.AreEqual(AIBuildPlannerSystem.BuildDecisionResult.InsufficientMaterials, materialsDecision.Result);
        Assert.AreEqual(80, materialsDecision.MaterialsCost);
        Assert.AreEqual(AIBuildPlannerSystem.BuildDecisionResult.InsufficientMaterials, combinedDecision.Result);
    }

    [Test]
    public void MaterialsRecoveryNeed_PreservesFirstBlockedTimeAndRejectsImpossibleCapacity()
    {
        using World world = new(nameof(MaterialsRecoveryNeed_PreservesFirstBlockedTimeAndRejectsImpossibleCapacity));
        EntityManager entityManager = world.EntityManager;
        Entity planEntity = entityManager.CreateEntity(typeof(AIMaterialsRecoveryNeedComponent));
        FactionTacticalMaterialsComponent materials = new()
        {
            FactionId = 2,
            Current = 20,
            Capacity = 100
        };

        AIBuildPlannerSystem.PublishMaterialsRecoveryNeed(
            entityManager,
            planEntity,
            factionId: 2,
            requiredCredits: 1000,
            requiredMaterials: 60,
            materials: materials,
            now: 10f);
        AIBuildPlannerSystem.PublishMaterialsRecoveryNeed(
            entityManager,
            planEntity,
            factionId: 2,
            requiredCredits: 1000,
            requiredMaterials: 60,
            materials: materials,
            now: 20f);

        AIMaterialsRecoveryNeedComponent need =
            entityManager.GetComponentData<AIMaterialsRecoveryNeedComponent>(planEntity);
        Assert.AreEqual(1, need.Active);
        Assert.AreEqual(1000, need.RequiredCredits);
        Assert.AreEqual(60, need.RequiredMaterials);
        Assert.AreEqual(40, need.MissingMaterials);
        Assert.AreEqual(10f, need.FirstBlockedTimeSeconds);
        Assert.AreEqual(20f, need.LastEvaluatedTimeSeconds);

        materials.Capacity = 50;
        AIBuildPlannerSystem.PublishMaterialsRecoveryNeed(
            entityManager,
            planEntity,
            factionId: 2,
            requiredCredits: 1000,
            requiredMaterials: 60,
            materials: materials,
            now: 30f);
        Assert.AreEqual(
            0,
            entityManager.GetComponentData<AIMaterialsRecoveryNeedComponent>(planEntity).Active);
        AIBuildPlannerSystem.ClearMaterialsRecoveryNeed(
            entityManager,
            planEntity,
            factionId: 2,
            now: 31f);
        need = entityManager.GetComponentData<AIMaterialsRecoveryNeedComponent>(planEntity);
        Assert.AreEqual(0, need.RequiredCredits);
        Assert.AreEqual(0, need.RequiredMaterials);
        Assert.AreEqual(0, need.MissingMaterials);
    }

    [Test]
    public void MaterialsRecoveryNeed_ClearRemovesPublishedDemand()
    {
        using World world = new(nameof(MaterialsRecoveryNeed_ClearRemovesPublishedDemand));
        EntityManager entityManager = world.EntityManager;
        Entity planEntity = entityManager.CreateEntity(typeof(AIMaterialsRecoveryNeedComponent));
        FactionTacticalMaterialsComponent materials = new()
        {
            FactionId = 3,
            Current = 0,
            Capacity = 100
        };
        AIBuildPlannerSystem.PublishMaterialsRecoveryNeed(
            entityManager,
            planEntity,
            factionId: 3,
            requiredCredits: 500,
            requiredMaterials: 50,
            materials: materials,
            now: 5f);

        AIBuildPlannerSystem.ClearMaterialsRecoveryNeed(
            entityManager,
            planEntity,
            factionId: 3,
            now: 6f);

        AIMaterialsRecoveryNeedComponent need =
            entityManager.GetComponentData<AIMaterialsRecoveryNeedComponent>(planEntity);
        Assert.AreEqual(0, need.Active);
        Assert.AreEqual(0, need.RequiredCredits);
        Assert.AreEqual(0, need.RequiredMaterials);
        Assert.AreEqual(0, need.MissingMaterials);
        Assert.AreEqual(6f, need.LastEvaluatedTimeSeconds);
    }

    private static Entity CreateBoundaryEntity(EntityManager entityManager)
    {
        return entityManager.CreateEntity(
            typeof(BuildingRuntimeStateTag),
            typeof(BuildingConfiguredSpawnableReadModel),
            typeof(BuildingRuntimeOwnedBuildingSummary),
            typeof(BuildingRuntimeSpawnRequest));
    }
}
#endif
