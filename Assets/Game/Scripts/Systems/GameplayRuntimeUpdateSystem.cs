using Game.Scripts.UI;
using System;
using Unity.Entities;
using UnityEngine;

public sealed class GameplayRuntimeUpdateSystem
{
    public void Update(
        MenuView menuView,
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
        MissionStartupSystem missionStartupSystem,
        TacticalMapRuntimeLoader mapLoader,
        RoadBuildSystem roadBuild,
        BuildingRuntimeUpdateSystem buildingRuntimeUpdate,
        BuildingRuntimeUpdateSystem.Context buildingRuntimeUpdateContext,
        Action selectionRuntimeUpdate,
        Camera worldCamera,
        RuntimeCitySpawnerSystem runtimeCitySpawner,
        RuntimeGridBlockerSystem runtimeGridBlockers,
        RuntimeDecorationSpawnerSystem runtimeDecorations,
        DayNightSystem dayNight,
        CitizenPopulationSystem citizenPopulation,
        MainMenuPlayUI mainMenu,
        UnitImpostorRenderSystem unitImpostors,
        ref bool gameplayStartPending)
    {
        bool gameplayActive = gameplayInitialized && runtimeGameplayStateSystem.PlayRequested;
        performanceDiagnosticsSystem.BeginUpdate(gameplayActive);
        bool hadSlowStep = false;

        double stepStart = performanceDiagnosticsSystem.BeginStep();
        menuView?.SyncInputState();
        hadSlowStep |= performanceDiagnosticsSystem.EndStep("MenuCanvasInput", stepStart);
        if (gameplayActive)
        {
            GameRuntimeStats.RecordMissionElapsed(Time.deltaTime);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            missionStartupSystem.UpdateActiveMission(World.DefaultGameObjectInjectionWorld, mapLoader);
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("MissionRuntime", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            roadBuild?.Update();
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("RoadBuild", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            buildingRuntimeUpdate?.Update(buildingRuntimeUpdateContext);
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("BuildingPlacement", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            selectionRuntimeUpdate?.Invoke();
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("Selection", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            missionStartupSystem.ApplyM01ProductionCameraPoseIfActive(worldCamera, mapLoader);
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("MissionCamera", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            runtimeCitySpawner?.Update();
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("RuntimeCitySpawner", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            runtimeGridBlockers?.Update();
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("RuntimeGridBlockers", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            runtimeDecorations?.Update();
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("RuntimeDecorations", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            dayNight?.Update();
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("DayNight", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            citizenPopulation?.Update();
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("CitizenPopulation", stepStart);
        }

        stepStart = performanceDiagnosticsSystem.BeginStep();
        menuView?.SyncRuntimeState();
        hadSlowStep |= performanceDiagnosticsSystem.EndStep("MenuCanvas", stepStart);

        stepStart = performanceDiagnosticsSystem.BeginStep();
        mainMenu?.Update();
        hadSlowStep |= performanceDiagnosticsSystem.EndStep("MainMenu", stepStart);

        if (gameplayStartPending && IsGameplayStartComplete(
                gameplayInitialized,
                runtimeGameplayStateSystem,
                runtimeCitySpawner,
                runtimeGridBlockers,
                runtimeDecorations))
        {
            gameplayStartPending = false;
            menuView?.NotifyGameplayReady();
        }

        if (gameplayActive)
            WarlineCaptureMatchResultFlow.TryCompleteActiveMissionFromLoadedScene();

        performanceDiagnosticsSystem.EndUpdate(
            gameplayActive,
            hadSlowStep,
            menuView,
            unitImpostors?.LastDrawnCount ?? 0,
            gameplayInitialized,
            runtimeGameplayStateSystem.PlayRequested);
    }

    public void LateUpdate(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
        UnitAttackTraceSystem unitAttackTraces,
        UnitImpostorRenderSystem unitImpostors)
    {
        if (!(gameplayInitialized && runtimeGameplayStateSystem.PlayRequested))
            return;

        double start = performanceDiagnosticsSystem.BeginTimedSection();
        unitAttackTraces?.LateUpdate();
        unitImpostors?.LateUpdate();
        performanceDiagnosticsSystem.EndLateUpdate(start, unitImpostors?.LastDrawnCount ?? 0);
    }

    public void OnGui(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
        RoadBuildSystem roadBuild,
        SelectionRectangleView selectionRectangleView)
    {
        if (!(gameplayInitialized && runtimeGameplayStateSystem.PlayRequested))
            return;

        double start = performanceDiagnosticsSystem.BeginTimedSection();
        roadBuild?.OnGui();
        selectionRectangleView?.Draw();
        performanceDiagnosticsSystem.EndOnGui(start);
    }

    private bool IsGameplayStartComplete(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        RuntimeCitySpawnerSystem runtimeCitySpawner,
        RuntimeGridBlockerSystem runtimeGridBlockers,
        RuntimeDecorationSpawnerSystem runtimeDecorations)
    {
        if (!gameplayInitialized || !runtimeGameplayStateSystem.PlayRequested)
            return false;
        if (runtimeCitySpawner != null && !runtimeCitySpawner.HasSpawned)
            return false;
        if (runtimeGridBlockers != null && !runtimeGridBlockers.HasSpawned)
            return false;
        if (runtimeDecorations != null && !runtimeDecorations.HasSpawned)
            return false;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        EntityQuery allSpawnConfigs = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        EntityQuery initializedSpawnConfigs = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());

        int totalConfigCount = allSpawnConfigs.CalculateEntityCount();
        int initializedConfigCount = initializedSpawnConfigs.CalculateEntityCount();
        allSpawnConfigs.Dispose();
        initializedSpawnConfigs.Dispose();

        return totalConfigCount == 0 || initializedConfigCount >= totalConfigCount;
    }
}
