using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class RuntimeBuildingSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new RuntimeBuildingSystemTests();
            tests.AllocateId_IsSequentialAndResetsOnClear();
            tests.SelectionTracksOnlyExistingBuildings();
            tests.RemovingOtherBuilding_PreservesCurrentSelection();
            tests.BuildingUiSelectionCommandRequest_DeletesSelectedBuildingAndWritesResult();
            tests.BuildingUiSelectionCommandRequest_ClearsSelectionAndWritesResult();
            Debug.Log("[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5");
            ValidationExit.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[RuntimeBuildingSystemFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void AllocateId_IsSequentialAndResetsOnClear()
    {
        var system = new RuntimeBuildingCollection<string>();

        Assert.AreEqual(1, system.AllocateId());
        Assert.AreEqual(2, system.AllocateId());

        system.AddBuilding(1, "Barracks");
        system.Clear();

        Assert.AreEqual(0, system.Count);
        Assert.AreEqual(1, system.AllocateId());
    }

    [Test]
    public void SelectionTracksOnlyExistingBuildings()
    {
        var system = new RuntimeBuildingCollection<string>();
        system.AddBuilding(10, "HQ");
        system.SelectBuilding(10);

        Assert.IsTrue(system.HasSelectedBuilding());
        Assert.AreEqual(10, system.CurrentActiveBuildingId);

        system.RemoveBuilding(10);

        Assert.IsFalse(system.HasSelectedBuilding());
        Assert.IsFalse(system.CurrentActiveBuildingId.HasValue);
    }

    [Test]
    public void RemovingOtherBuilding_PreservesCurrentSelection()
    {
        var system = new RuntimeBuildingCollection<string>();
        system.AddBuilding(1, "Barracks");
        system.AddBuilding(2, "HQ");
        system.SelectBuilding(2);

        system.RemoveBuilding(1);

        Assert.IsTrue(system.HasSelectedBuilding());
        Assert.AreEqual(2, system.CurrentActiveBuildingId);
    }

    [Test]
    public void BuildingUiSelectionCommandRequest_DeletesSelectedBuildingAndWritesResult()
    {
        using World world = new("BuildingUiSelectionCommandDeleteTest");
        var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
        RuntimeBuildingEntity building = new() { Id = 7 };
        runtimeBuildings.AddBuilding(building.Id, building);
        runtimeBuildings.SelectBuilding(building.Id);
        var selectionSystem = new BuildingSelectionRuntimeCompositionSystemHelper();
        BuildingSelectionRuntimeCompositionSystemHelper.Context context = CreateSelectionContext(runtimeBuildings);

        int requestId = selectionSystem.EnqueueDeleteSelectedBuilding(world.EntityManager);
        selectionSystem.ProcessPendingUiSelectionCommands(
            world.EntityManager,
            context,
            buildingId => runtimeBuildings.RemoveBuilding(buildingId));

        Assert.IsTrue(selectionSystem.TryGetUiSelectionCommandResult(
            world.EntityManager,
            requestId,
            out BuildingUiSelectionCommandResultElement result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(BuildingUiSelectionCommandRequestElement.KindDeleteSelectedBuilding, result.RequestKind);
        Assert.AreEqual(BuildingUiSelectionCommandResultElement.Completed, result.ResultCode);
        Assert.AreEqual(building.Id, result.BuildingId);
        Assert.IsFalse(runtimeBuildings.ContainsBuilding(building.Id));
        Assert.IsFalse(runtimeBuildings.CurrentActiveBuildingId.HasValue);

        using EntityQuery queueQuery = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<BuildingUiSelectionCommandQueueComponent>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        Assert.AreEqual(0, world.EntityManager.GetBuffer<BuildingUiSelectionCommandRequestElement>(queueEntity).Length);
    }

    [Test]
    public void BuildingUiSelectionCommandRequest_ClearsSelectionAndWritesResult()
    {
        using World world = new("BuildingUiSelectionCommandClearTest");
        var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
        RuntimeBuildingEntity building = new() { Id = 9 };
        runtimeBuildings.AddBuilding(building.Id, building);
        runtimeBuildings.SelectBuilding(building.Id);
        int refreshCount = 0;
        var selectionSystem = new BuildingSelectionRuntimeCompositionSystemHelper();
        BuildingSelectionRuntimeCompositionSystemHelper.Context context = CreateSelectionContext(runtimeBuildings, () => refreshCount++);

        int requestId = selectionSystem.EnqueueClearSelectedBuilding(world.EntityManager);
        selectionSystem.ProcessPendingUiSelectionCommands(world.EntityManager, context, null);

        Assert.IsTrue(selectionSystem.TryGetUiSelectionCommandResult(
            world.EntityManager,
            requestId,
            out BuildingUiSelectionCommandResultElement result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(BuildingUiSelectionCommandRequestElement.KindClearSelectedBuilding, result.RequestKind);
        Assert.AreEqual(BuildingUiSelectionCommandResultElement.Completed, result.ResultCode);
        Assert.AreEqual(0, result.BuildingId);
        Assert.IsFalse(runtimeBuildings.CurrentActiveBuildingId.HasValue);
        Assert.AreEqual(1, refreshCount);
    }

    private static BuildingSelectionRuntimeCompositionSystemHelper.Context CreateSelectionContext(
        RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildings,
        BuildingSelectionRuntimeCompositionSystemHelper.RuntimeAction refreshMarkers = null)
    {
        var selectionSystem = new BuildingSelectionRuntimeCompositionSystemHelper();
        return selectionSystem.CreateContext(
            runtimeBuildings,
            runtimeBuildings.Buildings,
            null,
            null,
            null,
            null,
            refreshMarkers,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }
}
#endif
