using System;
using Game.Simulation;

internal static class GameplayStartGatePolicySmoke
{
    private static int checks;

    private static bool CompleteGate(
        bool gameplayInitialized = true,
        bool playRequested = true,
        bool buildingStartupComplete = true,
        bool runtimeCityPresent = false,
        bool runtimeCityComplete = false,
        bool runtimeGridBlockersPresent = false,
        bool runtimeGridBlockersComplete = false,
        bool runtimeDecorationsPresent = false,
        bool runtimeDecorationsComplete = false,
        bool worldAvailable = true,
        int initialSpawnConfigCount = 1,
        int initialSpawnInitializedCount = 1) =>
        GameplayStartGatePolicy.IsGameplayStartComplete(
            gameplayInitialized,
            playRequested,
            buildingStartupComplete,
            runtimeCityPresent,
            runtimeCityComplete,
            runtimeGridBlockersPresent,
            runtimeGridBlockersComplete,
            runtimeDecorationsPresent,
            runtimeDecorationsComplete,
            worldAvailable,
            initialSpawnConfigCount,
            initialSpawnInitializedCount);

    private static void Expect(string name, bool actual, bool expected)
    {
        checks++;
        if (actual != expected)
            throw new InvalidOperationException(name + " expected=" + expected + " actual=" + actual);
    }

    private static int Main()
    {
        Expect("not_initialized", GameplayStartGatePolicy.HasLaunchRequest(false, true), false);
        Expect("not_requested", GameplayStartGatePolicy.HasLaunchRequest(true, false), false);
        Expect("launch", GameplayStartGatePolicy.HasLaunchRequest(true, true), true);

        Expect("building_pending", GameplayStartGatePolicy.IsBuildingStartupComplete(false), false);
        Expect("optional_owner_absent", GameplayStartGatePolicy.IsOptionalRuntimeOwnerComplete(false, false), true);
        Expect("city_pending", GameplayStartGatePolicy.IsOptionalRuntimeOwnerComplete(true, false), false);
        Expect("runtime_owner_complete", GameplayStartGatePolicy.IsOptionalRuntimeOwnerComplete(true, true), true);

        Expect("world_missing", GameplayStartGatePolicy.IsWorldAvailable(false), false);
        Expect("world_created", GameplayStartGatePolicy.IsWorldAvailable(true), true);
        Expect("no_initial_spawns", GameplayStartGatePolicy.AreInitialSpawnCountsComplete(0, 0), true);
        Expect("uninitialized_spawn", GameplayStartGatePolicy.AreInitialSpawnCountsComplete(2, 1), false);
        Expect("all_spawns_initialized", GameplayStartGatePolicy.AreInitialSpawnCountsComplete(2, 2), true);
        Expect("extra_initialized_rows", GameplayStartGatePolicy.AreInitialSpawnCountsComplete(2, 3), true);

        Expect("complete_gate", CompleteGate(), true);
        Expect("gate_not_initialized", CompleteGate(gameplayInitialized: false), false);
        Expect("gate_not_requested", CompleteGate(playRequested: false), false);
        Expect("gate_building_pending", CompleteGate(buildingStartupComplete: false), false);
        Expect("gate_present_city_pending", CompleteGate(runtimeCityPresent: true), false);
        Expect("gate_present_blocker_pending", CompleteGate(runtimeGridBlockersPresent: true), false);
        Expect("gate_present_decoration_pending", CompleteGate(runtimeDecorationsPresent: true), false);
        Expect("gate_missing_world", CompleteGate(worldAvailable: false), false);
        Expect("gate_initial_spawn_pending", CompleteGate(initialSpawnInitializedCount: 0), false);
        Expect("gate_all_present_owners_complete", CompleteGate(
            runtimeCityPresent: true, runtimeCityComplete: true,
            runtimeGridBlockersPresent: true, runtimeGridBlockersComplete: true,
            runtimeDecorationsPresent: true, runtimeDecorationsComplete: true), true);

        Expect("no_timer", GameplayStartGatePolicy.IsRuntimeCityFailOpenWindow(true, true, -1, 1800), false);
        Expect("not_initialized_timeout", GameplayStartGatePolicy.IsRuntimeCityFailOpenWindow(false, true, 0, 1800), false);
        Expect("not_requested_timeout", GameplayStartGatePolicy.IsRuntimeCityFailOpenWindow(true, false, 0, 1800), false);
        Expect("before_timeout", GameplayStartGatePolicy.IsRuntimeCityFailOpenWindow(true, true, 0, GameplayStartGatePolicy.RuntimeCityFailOpenFrames - 1), false);
        Expect("timeout_boundary", GameplayStartGatePolicy.IsRuntimeCityFailOpenWindow(true, true, 0, GameplayStartGatePolicy.RuntimeCityFailOpenFrames), true);
        Expect("city_absent", GameplayStartGatePolicy.HasRuntimeCity(false), false);
        Expect("city_spawned", GameplayStartGatePolicy.IsRuntimeCityNotSpawned(true), false);
        Expect("city_generating", GameplayStartGatePolicy.IsRuntimeCityNotGenerating(true), false);
        Expect("city_not_spawned", GameplayStartGatePolicy.IsRuntimeCityNotSpawned(false), true);
        Expect("city_not_generating", GameplayStartGatePolicy.IsRuntimeCityNotGenerating(false), true);

        Console.WriteLine("[GameplayStartGatePolicySmoke] result=Passed checks=" + checks);
        return 0;
    }
}
