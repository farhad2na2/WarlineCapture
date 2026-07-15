#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using Game.Components;
using Game.Editor;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

public sealed class MatchPerformanceFixtureSeedTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        RunValidationStep(
            nameof(EmptyWorldSeedsHistoricalFloorAndIsIdempotent),
            tests => tests.EmptyWorldSeedsHistoricalFloorAndIsIdempotent(),
            ref passed);
        RunValidationStep(
            nameof(ExistingWorkloadOnlyFillsDeficits),
            tests => tests.ExistingWorkloadOnlyFillsDeficits(),
            ref passed);
        RunValidationStep(
            nameof(LargerWorkloadIsPreserved),
            tests => tests.LargerWorkloadIsPreserved(),
            ref passed);
        UnityEngine.Debug.Log($"[MatchPerformanceFixtureSeedValidation] result=Passed tests={passed}");
    }

    [Test]
    public void EmptyWorldSeedsHistoricalFloorAndIsIdempotent()
    {
        using World world = new(nameof(EmptyWorldSeedsHistoricalFloorAndIsIdempotent));
        EntityManager entityManager = world.EntityManager;

        MatchPerformanceFixtureSeed.Result first = MatchPerformanceFixtureSeed.Ensure(entityManager);

        Assert.AreEqual(MatchPerformanceFixtureSeed.TargetBuildingCount, first.AddedBuildingCount);
        Assert.AreEqual(MatchPerformanceFixtureSeed.TargetRenderVisualStateCount, first.AddedUnitCount);
        AssertHistoricalFloor(entityManager);

        MatchPerformanceFixtureSeed.Result second = MatchPerformanceFixtureSeed.Ensure(entityManager);

        Assert.Zero(second.AddedBuildingCount);
        Assert.Zero(second.AddedUnitCount);
        AssertHistoricalFloor(entityManager);
    }

    [Test]
    public void ExistingWorkloadOnlyFillsDeficits()
    {
        using World world = new(nameof(ExistingWorkloadOnlyFillsDeficits));
        EntityManager entityManager = world.EntityManager;
        CreateExistingBuildings(entityManager, 600);
        Entity preserved = CreateExistingUnits(entityManager, count: 80, culledCount: 40);

        MatchPerformanceFixtureSeed.Result result = MatchPerformanceFixtureSeed.Ensure(entityManager);

        Assert.AreEqual(28, result.AddedBuildingCount);
        Assert.AreEqual(25, result.AddedUnitCount);
        Assert.IsTrue(entityManager.Exists(preserved));
        Assert.AreEqual(
            new FixedString64Bytes("Unit_Chr_Soldier_Male_02_Alt_04"),
            entityManager.GetComponentData<UnitSourcePrefabKey>(preserved).Value);
        AssertHistoricalFloor(entityManager);
    }

    [Test]
    public void LargerWorkloadIsPreserved()
    {
        using World world = new(nameof(LargerWorkloadIsPreserved));
        EntityManager entityManager = world.EntityManager;
        CreateExistingBuildings(entityManager, MatchPerformanceFixtureSeed.TargetBuildingCount + 5);
        CreateExistingUnits(
            entityManager,
            MatchPerformanceFixtureSeed.TargetRenderVisualStateCount + 7,
            MatchPerformanceFixtureSeed.TargetCulledUnitCount + 3);
        int sourceCount = Count<UnitSourcePrefabKey>(entityManager);
        int buildingCount = Count<RuntimeBuildingCombatTag>(entityManager);
        int visualCount = Count<UnitRenderVisualComponent>(entityManager);
        int culledCount = Count<UnitRenderBudgetCulledUnitTag>(entityManager);

        MatchPerformanceFixtureSeed.Result result = MatchPerformanceFixtureSeed.Ensure(entityManager);

        Assert.Zero(result.AddedBuildingCount);
        Assert.Zero(result.AddedUnitCount);
        Assert.AreEqual(sourceCount, Count<UnitSourcePrefabKey>(entityManager));
        Assert.AreEqual(buildingCount, Count<RuntimeBuildingCombatTag>(entityManager));
        Assert.AreEqual(visualCount, Count<UnitRenderVisualComponent>(entityManager));
        Assert.AreEqual(culledCount, Count<UnitRenderBudgetCulledUnitTag>(entityManager));
    }

    private static void AssertHistoricalFloor(EntityManager entityManager)
    {
        Assert.AreEqual(
            MatchPerformanceFixtureSeed.TargetSourceEntityCount,
            Count<UnitSourcePrefabKey>(entityManager));
        Assert.AreEqual(
            MatchPerformanceFixtureSeed.TargetBuildingCount,
            Count<RuntimeBuildingCombatTag>(entityManager));
        Assert.AreEqual(
            MatchPerformanceFixtureSeed.TargetRenderVisualStateCount,
            Count<UnitRenderVisualComponent>(entityManager));
        Assert.AreEqual(
            MatchPerformanceFixtureSeed.TargetCulledUnitCount,
            Count<UnitRenderBudgetCulledUnitTag>(entityManager));
    }

    private static void CreateExistingBuildings(EntityManager entityManager, int count)
    {
        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(UnitSourcePrefabKey),
            typeof(RuntimeBuildingCombatTag));
        for (int i = 0; i < count; i++)
        {
            Entity entity = entityManager.CreateEntity(archetype);
            entityManager.SetComponentData(entity, new UnitSourcePrefabKey
            {
                Value = new FixedString64Bytes("Existing_Building")
            });
        }
    }

    private static Entity CreateExistingUnits(EntityManager entityManager, int count, int culledCount)
    {
        Entity first = Entity.Null;
        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(UnitSourcePrefabKey),
            typeof(UnitRenderVisualComponent));
        for (int i = 0; i < count; i++)
        {
            Entity entity = entityManager.CreateEntity(archetype);
            if (first == Entity.Null)
                first = entity;
            entityManager.SetComponentData(entity, new UnitSourcePrefabKey
            {
                Value = new FixedString64Bytes("Unit_Chr_Soldier_Male_02_Alt_04")
            });
            entityManager.SetComponentData(entity, new UnitRenderVisualComponent
            {
                Current = (byte)UnitRenderVisualKind.Detail,
                Desired = (byte)UnitRenderVisualKind.Detail
            });
            if (i < culledCount)
                entityManager.AddComponent<UnitRenderBudgetCulledUnitTag>(entity);
        }

        return first;
    }

    private static int Count<T>(EntityManager entityManager)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return query.CalculateEntityCount();
    }

    private static void RunValidationStep(
        string name,
        Action<MatchPerformanceFixtureSeedTests> action,
        ref int passed)
    {
        var tests = new MatchPerformanceFixtureSeedTests();
        try
        {
            action(tests);
            passed++;
        }
        catch (Exception exception)
        {
            throw new AssertionException($"{name} failed: {exception}");
        }
    }
}
#endif
