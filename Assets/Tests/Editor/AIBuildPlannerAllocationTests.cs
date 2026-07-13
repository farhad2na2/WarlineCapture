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

        for (int i = 0; i < WarmupCalls; i++)
        {
            decision = AIBuildPlannerSystem.SelectBuildDecision(
                entries,
                spawnables,
                ownedSummaries,
                spawnRequests,
                plan,
                economyMoney: 500);
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
                economyMoney: 500);
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(AIBuildPlannerSystem.BuildDecisionResult.Request, decision.Result);
        Assert.AreEqual(1, decision.EntryIndex);
        Assert.AreEqual(new FixedString128Bytes("barracks"), decision.BuildingId);
        Assert.AreEqual(250, decision.Cost);
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

        for (int i = 0; i < WarmupCalls; i++)
        {
            decision = AIBuildPlannerSystem.SelectBuildDecision(
                entries,
                spawnables,
                ownedSummaries,
                spawnRequests,
                plan,
                economyMoney: 0);
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
                economyMoney: 0);
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
