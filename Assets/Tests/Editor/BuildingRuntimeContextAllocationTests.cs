using Game.Components;
using Game.Runtime;

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingRuntimeContextAllocationTests
{
    private const int WarmupCalls = 16;
    private const int MeasuredCalls = 512;

    [Test]
    public void BuildingSource_WarmedNullRegistrationDoesNotRebuildContextGraph()
    {
        BuildingRuntimeContextFactoryCompositionSystemHelper.Source context = CreateBuildingSource();
        AssertZeroAllocation(
            () => context.RegisterRuntimeBuilding(null, null, default, false),
            "runtime building registration");
    }

    [Test]
    public void BuildingSource_WarmedNullOwnershipDoesNotRebuildContextGraph()
    {
        BuildingRuntimeContextFactoryCompositionSystemHelper.Source context = CreateBuildingSource();
        AssertZeroAllocation(
            () => context.SetRuntimeBuildingOwnerFaction(null, null),
            "runtime building ownership");
    }

    [Test]
    public void RuntimeSource_WarmedNullBreachDoesNotRebuildContextGraph()
    {
        BuildingGameplaySourceCompositionSystemHelper source = new BuildingGameplayChildSystem().Create();
        BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource context =
            source.BuildingRuntimeContextCompositionSystemHelper.CreateRuntimeContextSource(
                source,
                TryGetEntityManager,
                TryGetGridData,
                IsHouseBuilding,
                TryResolveBuildingFocusWorldPosition,
                TryGetRuntimeBuilding,
                GetEffectivePlacementRect);

        AssertZeroAllocation(
            () => context.RememberOpenBaseBreach(null),
            "runtime breach callback");
    }

    [Test]
    public void RuntimeQuery_WarmedEffectivePlacementRectDoesNotAllocate()
    {
        BuildingGameplaySourceCompositionSystemHelper source = new BuildingGameplayChildSystem().Create();
        var definition = new BuildingDefinition { FootprintCells = new Vector2Int(2, 3) };
        var originCell = new Vector2Int(7, 11);

        RectInt rect = source.BuildingRuntimeQueryCompositionSystemHelper.GetEffectivePlacementRect(
            source,
            definition,
            originCell,
            default,
            false);

        Assert.AreEqual(new RectInt(originCell, definition.FootprintCells), rect);
        AssertZeroAllocation(
            () => source.BuildingRuntimeQueryCompositionSystemHelper.GetEffectivePlacementRect(
                source,
                definition,
                originCell,
                default,
                false),
            "effective placement rectangle query");
    }

    [Test]
    public void CreationContext_WarmedRedirectDoesNotRebuildRedirectContext()
    {
        BuildingRuntimeCreationCompositionSystemHelper.Context context =
            new BuildingRuntimeContextFactoryCompositionSystemHelper().CreateCreationContext(CreateBuildingSource());
        AssertZeroAllocation(
            () => context.RedirectUnits(default),
            "runtime placement redirect");
    }

    [Test]
    public void CreationContext_ObservesDeferredStateChanges()
    {
        bool deferred = false;
        int redirectCount = 0;
        int deferredFootprintCount = 0;
        int pendingMarkerRefreshCount = 0;
        var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
        BuildingRuntimeCreationCompositionSystemHelper.Context context = new(
            runtimeBuildings,
            null,
            default,
            null,
            () => deferred,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            footprint => redirectCount++,
            footprint => deferredFootprintCount++,
            () => pendingMarkerRefreshCount++,
            null,
            null);
        var creationSystem = new BuildingRuntimeCreationCompositionSystemHelper();
        var definition = new BuildingDefinition { FootprintCells = Vector2Int.one };
        var immediateInstance = new GameObject("ImmediateBuilding");
        var deferredInstance = new GameObject("DeferredBuilding");

        try
        {
            RuntimeBuildingEntity immediate = creationSystem.RegisterRuntimeBuilding(
                context,
                definition,
                immediateInstance,
                Vector2Int.zero,
                false);

            deferred = true;
            RuntimeBuildingEntity deferredBuilding = creationSystem.RegisterRuntimeBuilding(
                context,
                definition,
                deferredInstance,
                Vector2Int.one,
                false);

            Assert.NotNull(immediate);
            Assert.NotNull(deferredBuilding);
            Assert.AreEqual(1, redirectCount);
            Assert.AreEqual(1, deferredFootprintCount);
            Assert.AreEqual(1, pendingMarkerRefreshCount);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(immediateInstance);
            UnityEngine.Object.DestroyImmediate(deferredInstance);
        }
    }

    private static BuildingRuntimeContextFactoryCompositionSystemHelper.Source CreateBuildingSource()
    {
        BuildingGameplaySourceCompositionSystemHelper source = new BuildingGameplayChildSystem().Create();
        return source.BuildingRuntimeContextCompositionSystemHelper.CreateBuildingRuntimeContextSource(
            source,
            default,
            null,
            TryGetEntityManager,
            TryGetGridData,
            GetEffectivePlacementRect,
            OverlapsAnyRuntimeBuilding,
            IsHouseBuilding,
            TryResolveBuildingFocusWorldPosition,
            TryGetRuntimeBuilding,
            BeginDeferredSideEffects,
            EndDeferredSideEffects,
            5f);
    }

    private static void AssertZeroAllocation(Action action, string operation)
    {
        for (int i = 0; i < WarmupCalls; i++)
            action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < MeasuredCalls; i++)
            action();
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(
            0L,
            allocatedBytes,
            $"Warmed {operation} allocated {allocatedBytes} managed bytes over {MeasuredCalls} calls.");
    }

    private static bool TryGetEntityManager(out EntityManager entityManager)
    {
        entityManager = default;
        return false;
    }

    private static bool TryGetGridData(
        BuildingGameplaySourceCompositionSystemHelper source,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData)
    {
        gridEntity = Entity.Null;
        grid = default;
        roads = default;
        blockerData = default;
        return false;
    }

    private static RectInt GetEffectivePlacementRect(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingDefinition definition,
        Vector2Int originCell,
        GridConfig grid,
        bool rotateVertical)
    {
        return default;
    }

    private static bool OverlapsAnyRuntimeBuilding(
        BuildingGameplaySourceCompositionSystemHelper source,
        RectInt candidateRect)
    {
        return false;
    }

    private static bool IsHouseBuilding(
        BuildingGameplaySourceCompositionSystemHelper source,
        RuntimeBuildingEntity building)
    {
        return false;
    }

    private static bool TryResolveBuildingFocusWorldPosition(
        BuildingGameplaySourceCompositionSystemHelper source,
        RuntimeBuildingEntity building,
        out Vector3 worldPosition)
    {
        worldPosition = default;
        return false;
    }

    private static bool TryGetRuntimeBuilding(
        BuildingGameplaySourceCompositionSystemHelper source,
        int id,
        out RuntimeBuildingEntity building)
    {
        building = null;
        return false;
    }

    private static void BeginDeferredSideEffects(BuildingGameplaySourceCompositionSystemHelper source)
    {
    }

    private static void EndDeferredSideEffects(BuildingGameplaySourceCompositionSystemHelper source)
    {
    }
}
#endif
