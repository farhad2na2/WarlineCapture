using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class BuildingPlacementRuntimeTickCompositionSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new BuildingPlacementRuntimeTickCompositionSystemHelperTests();
            tests.StartupTickRunsBoundaryBeforeAndAfterMapPlacementQueues();
            tests.SimulationTickKeepsMapPlacementQueuesAliveBeforeBoundary();
            tests.SimulationTickUpdatesProductionTransportsEveryFrameAndThrottlesResourceVisuals();
            UnityEngine.Debug.Log("[BuildingPlacementRuntimeTickFocusedValidation] result=Passed tests=3");
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogError($"[BuildingPlacementRuntimeTickFocusedValidation] result=Failed error={exception}");
            throw;
        }
    }

    [Test]
    public void StartupTickRunsBoundaryBeforeAndAfterMapPlacementQueues()
    {
        BuildingPlacementRuntimeTickCompositionSystemHelper tickSystem = new();
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
        BuildingPlacementRuntimeTickCompositionSystemHelper tickSystem = new();
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

    [Test]
    public void SimulationTickUpdatesProductionTransportsEveryFrameAndThrottlesResourceVisuals()
    {
        BuildingPlacementRuntimeTickCompositionSystemHelper tickSystem = new();
        var calls = new List<string>();

        BuildingPlacementRuntimeTickCompositionSystemHelper.Context context = CreateContext(
            calls,
            enqueueMapBuildingPlacements: () => calls.Add("mapBuildings"),
            enqueueMapVehiclePlacements: () => calls.Add("mapVehicles"),
            updateBuildingRuntimeBoundary: () => calls.Add("boundary"),
            updateActiveProductionTransports: () => calls.Add("activeTransport"),
            updateBuildingResourceVisuals: () => calls.Add("visuals"));

        tickSystem.UpdateSimulation(context);
        tickSystem.UpdateSimulation(context);

        Assert.AreEqual(2, calls.Count(call => call == "activeTransport"));
        Assert.AreEqual(1, calls.Count(call => call == "visuals"));
    }

    private static BuildingPlacementRuntimeTickCompositionSystemHelper.Context CreateContext(
        List<string> calls,
        System.Action enqueueMapBuildingPlacements,
        System.Action enqueueMapVehiclePlacements,
        System.Action updateBuildingRuntimeBoundary,
        System.Action updateActiveProductionTransports = null,
        System.Action updateBuildingResourceVisuals = null)
    {
        return new BuildingPlacementRuntimeTickCompositionSystemHelper.Context(
            processPendingProductions: () => calls.Add("production"),
            updateActiveProductionTransports: updateActiveProductionTransports ?? (() => calls.Add("activeTransport")),
            updateResourceProduction: () => calls.Add("resources"),
            updateResourceHaulers: () => calls.Add("haulers"),
            updateBuildingResourceVisuals: updateBuildingResourceVisuals ?? (() => calls.Add("visuals")),
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
