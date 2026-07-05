using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Game.Runtime;

public sealed class BuildingPlacementRuntimeTickCompositionSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new BuildingPlacementRuntimeTickCompositionSystemHelperTests();
                tests.StartupTickRunsBoundaryBeforeAndAfterMapPlacementQueues();
                tests.SimulationTickRunsBoundaryBeforeProductionWork();
                tests.SimulationTickThrottlesProductionTransportsAndResourceVisuals();
                tests.SimulationTickSkipsImmediateActiveTransportProbeWhenPendingTickIsIdle();
                tests.SimulationTickPassesAccumulatedDeltaToResourceProduction();
                UnityEngine.Debug.Log("[BuildingPlacementRuntimeTickFocusedValidation] result=Passed tests=5");
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
            updateBuildingRuntimeState: () => calls.Add("boundary")));

        CollectionAssert.AreEqual(
            new[] { "boundary", "mapBuildings", "mapVehicles", "boundary" },
            calls);
    }

    [Test]
    public void SimulationTickRunsBoundaryBeforeProductionWork()
    {
        BuildingPlacementRuntimeTickCompositionSystemHelper tickSystem = new();
        var calls = new List<string>();

        tickSystem.UpdateSimulation(CreateContext(
            calls,
            enqueueMapBuildingPlacements: () => calls.Add("mapBuildings"),
            enqueueMapVehiclePlacements: () => calls.Add("mapVehicles"),
            updateBuildingRuntimeState: () => calls.Add("boundary")));

        Assert.GreaterOrEqual(calls.Count, 3);
        CollectionAssert.AreEqual(
            new[] { "boundary", "production", "activeTransport" },
            calls.GetRange(0, 3));
    }

    [Test]
    public void SimulationTickThrottlesProductionTransportsAndResourceVisuals()
    {
        BuildingPlacementRuntimeTickCompositionSystemHelper tickSystem = new();
        var calls = new List<string>();

        BuildingPlacementRuntimeTickCompositionSystemHelper.Context context = CreateContext(
            calls,
            enqueueMapBuildingPlacements: () => calls.Add("mapBuildings"),
            enqueueMapVehiclePlacements: () => calls.Add("mapVehicles"),
            updateBuildingRuntimeState: () => calls.Add("boundary"),
            updateActiveProductionTransports: () =>
            {
                calls.Add("activeTransport");
                return true;
            },
            updateBuildingResourceVisuals: () => calls.Add("visuals"));

        tickSystem.UpdateSimulation(context);
        tickSystem.UpdateSimulation(context);

        Assert.AreEqual(1, calls.Count(call => call == "activeTransport"));
        Assert.AreEqual(1, calls.Count(call => call == "visuals"));
    }

    [Test]
    public void SimulationTickSkipsImmediateActiveTransportProbeWhenPendingTickIsIdle()
    {
        BuildingPlacementRuntimeTickCompositionSystemHelper tickSystem = new();
        var calls = new List<string>();

        BuildingPlacementRuntimeTickCompositionSystemHelper.Context context = CreateContext(
            calls,
            enqueueMapBuildingPlacements: () => calls.Add("mapBuildings"),
            enqueueMapVehiclePlacements: () => calls.Add("mapVehicles"),
            updateBuildingRuntimeState: () => calls.Add("boundary"),
            processPendingProductions: () =>
            {
                calls.Add("production");
                return false;
            },
            updateActiveProductionTransports: () =>
            {
                calls.Add("activeTransport");
                return false;
            });

        tickSystem.UpdateSimulation(context);
        tickSystem.UpdateSimulation(context);

        Assert.AreEqual(1, calls.Count(call => call == "activeTransport"));
    }

    [Test]
    public void SimulationTickPassesAccumulatedDeltaToResourceProduction()
    {
        BuildingPlacementRuntimeTickCompositionSystemHelper tickSystem = new();
        var calls = new List<string>();
        float resourceDeltaTime = -1f;

        BuildingPlacementRuntimeTickCompositionSystemHelper.Context context = CreateContext(
            calls,
            enqueueMapBuildingPlacements: () => calls.Add("mapBuildings"),
            enqueueMapVehiclePlacements: () => calls.Add("mapVehicles"),
            updateBuildingRuntimeState: () => calls.Add("boundary"),
            updateResourceProduction: deltaTime =>
            {
                calls.Add("resources");
                resourceDeltaTime = deltaTime;
            },
            getDeltaTime: () => 1f);

        tickSystem.UpdateSimulation(context);

        Assert.AreEqual(1, calls.Count(call => call == "resources"));
        Assert.AreEqual(1f, resourceDeltaTime, 0.0001f);
    }

    private static BuildingPlacementRuntimeTickCompositionSystemHelper.Context CreateContext(
        List<string> calls,
        System.Action enqueueMapBuildingPlacements,
        System.Action enqueueMapVehiclePlacements,
        System.Action updateBuildingRuntimeState,
        System.Func<bool> processPendingProductions = null,
        System.Func<bool> updateActiveProductionTransports = null,
        System.Action<float> updateResourceProduction = null,
        System.Action updateBuildingResourceVisuals = null,
        System.Func<float> getDeltaTime = null)
    {
        return new BuildingPlacementRuntimeTickCompositionSystemHelper.Context(
            processPendingProductions: processPendingProductions ?? (() =>
            {
                calls.Add("production");
                return true;
            }),
            updateActiveProductionTransports: updateActiveProductionTransports ?? (() =>
            {
                calls.Add("activeTransport");
                return true;
            }),
            updateResourceProduction: updateResourceProduction ?? (_ => calls.Add("resources")),
            updateResourceHaulers: () => calls.Add("haulers"),
            updateBuildingResourceVisuals: updateBuildingResourceVisuals ?? (() => calls.Add("visuals")),
            cleanupRecentSpawnReservations: () => calls.Add("reservations"),
            syncDestroyedRuntimeBuildingCombatEntities: () => calls.Add("destroyedSync"),
            updateDestroyedBuildings: () => calls.Add("destroyed"),
            updateRoadBarrierDoors: () => calls.Add("doors"),
            flushPendingMarkerRefresh: () => calls.Add("markers"),
            enqueueMapBuildingPlacements: enqueueMapBuildingPlacements,
            enqueueMapVehiclePlacements: enqueueMapVehiclePlacements,
            updateBuildingRuntimeState: updateBuildingRuntimeState,
            updateInput: () => default,
            diagnosticsSystem: null,
            diagnosticsContext: default,
            getDeltaTime: getDeltaTime);
    }
}
