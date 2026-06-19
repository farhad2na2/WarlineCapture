using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;

public sealed class BuildingPlacementRuntimeTickSystemTests
{
    [Test]
    public void StartupTickRunsBoundaryBeforeAndAfterMapPlacementQueues()
    {
        using World world = new("BuildingPlacementRuntimeTickStartupTests");
        BuildingPlacementRuntimeTickSystem tickSystem = world.CreateSystemManaged<BuildingPlacementRuntimeTickSystem>();
        var calls = new List<string>();

        tickSystem.UpdateStartup(CreateContext(
            calls,
            enqueueMapBuildingPlacements: () => calls.Add("mapBuildings"),
            enqueueMapVehiclePlacements: () => calls.Add("mapVehicles"),
            updateBuildingRuntimeBoundary: () => calls.Add("boundary")));

        CollectionAssert.AreEqual(
            new[] { "boundary", "mapBuildings", "mapVehicles", "boundary" },
            calls);
    }

    [Test]
    public void SimulationTickKeepsMapPlacementQueuesAliveBeforeBoundary()
    {
        using World world = new("BuildingPlacementRuntimeTickSimulationTests");
        BuildingPlacementRuntimeTickSystem tickSystem = world.CreateSystemManaged<BuildingPlacementRuntimeTickSystem>();
        var calls = new List<string>();

        tickSystem.UpdateSimulation(CreateContext(
            calls,
            enqueueMapBuildingPlacements: () => calls.Add("mapBuildings"),
            enqueueMapVehiclePlacements: () => calls.Add("mapVehicles"),
            updateBuildingRuntimeBoundary: () => calls.Add("boundary")));

        Assert.GreaterOrEqual(calls.Count, 3);
        CollectionAssert.AreEqual(
            new[] { "mapBuildings", "mapVehicles", "boundary" },
            calls.GetRange(0, 3));
    }

    private static BuildingPlacementRuntimeTickSystem.Context CreateContext(
        List<string> calls,
        System.Action enqueueMapBuildingPlacements,
        System.Action enqueueMapVehiclePlacements,
        System.Action updateBuildingRuntimeBoundary)
    {
        return new BuildingPlacementRuntimeTickSystem.Context(
            processPendingProductions: () => calls.Add("production"),
            updateResourceProduction: () => calls.Add("resources"),
            updateResourceHaulers: () => calls.Add("haulers"),
            updateBuildingResourceVisuals: () => calls.Add("visuals"),
            cleanupRecentSpawnReservations: () => calls.Add("reservations"),
            syncDestroyedRuntimeBuildingCombatEntities: () => calls.Add("destroyedSync"),
            updateDestroyedBuildings: () => calls.Add("destroyed"),
            updateRoadBarrierDoors: () => calls.Add("doors"),
            flushPendingMarkerRefresh: () => calls.Add("markers"),
            enqueueMapBuildingPlacements: enqueueMapBuildingPlacements,
            enqueueMapVehiclePlacements: enqueueMapVehiclePlacements,
            updateBuildingRuntimeBoundary: updateBuildingRuntimeBoundary,
            updateInput: () => default,
            diagnosticsSystem: null,
            diagnosticsContext: default);
    }
}
