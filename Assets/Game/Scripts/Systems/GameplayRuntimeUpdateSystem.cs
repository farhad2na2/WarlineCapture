using Game.Scripts.UI;
using System;
using Unity.Entities;
using UnityEngine;

public sealed class GameplayRuntimeUpdateSystem
{
    private const int LoadingGateDiagnosticIntervalFrames = 120;
    private int _nextLoadingGateDiagnosticFrame;

    public void Update(
        MenuView menuView,
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
        MissionStartupSystem missionStartupSystem,
        TacticalMapRuntimeLoader mapLoader,
        Action roadBuildRuntimeUpdate,
        BuildingRuntimeUpdateSystem buildingRuntimeUpdate,
        BuildingRuntimeUpdateSystem.Context buildingRuntimeUpdateContext,
        Action selectionRuntimeUpdate,
        Camera worldCamera,
        RuntimeCityCompositionSystem runtimeCity,
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
            roadBuildRuntimeUpdate?.Invoke();
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
            runtimeCity?.Update(Time.frameCount);
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("RuntimeCity", stepStart);

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
                runtimeCity,
                runtimeGridBlockers,
                runtimeDecorations))
        {
            gameplayStartPending = false;
            menuView?.NotifyGameplayReady();
            Debug.Log($"[LoadingGate] ready frame={Time.frameCount} gameplayInitialized={(gameplayInitialized ? 1 : 0)} playRequested={(runtimeGameplayStateSystem.PlayRequested ? 1 : 0)}");
        }
        else if (gameplayStartPending)
        {
            LogLoadingGateIfDue(
                gameplayInitialized,
                runtimeGameplayStateSystem,
                runtimeCity,
                runtimeGridBlockers,
                runtimeDecorations);
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
        Action roadBuildOnGui,
        SelectionRectangleView selectionRectangleView)
    {
        if (!(gameplayInitialized && runtimeGameplayStateSystem.PlayRequested))
            return;

        double start = performanceDiagnosticsSystem.BeginTimedSection();
        roadBuildOnGui?.Invoke();
        selectionRectangleView?.Draw();
        performanceDiagnosticsSystem.EndOnGui(start);
    }

    private bool IsGameplayStartComplete(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        RuntimeCityCompositionSystem runtimeCity,
        RuntimeGridBlockerSystem runtimeGridBlockers,
        RuntimeDecorationSpawnerSystem runtimeDecorations)
    {
        if (!gameplayInitialized || !runtimeGameplayStateSystem.PlayRequested)
            return false;
        if (runtimeCity != null && !runtimeCity.HasSpawned)
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

    private void LogLoadingGateIfDue(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        RuntimeCityCompositionSystem runtimeCity,
        RuntimeGridBlockerSystem runtimeGridBlockers,
        RuntimeDecorationSpawnerSystem runtimeDecorations)
    {
        if (Time.frameCount < _nextLoadingGateDiagnosticFrame)
            return;

        _nextLoadingGateDiagnosticFrame = Time.frameCount + LoadingGateDiagnosticIntervalFrames;

        bool playRequested = runtimeGameplayStateSystem.PlayRequested;
        string cityState = runtimeCity == null
            ? "null"
            : $"spawned={(runtimeCity.HasSpawned ? 1 : 0)} generating={(runtimeCity.IsGenerating ? 1 : 0)} spawnOnStart={(runtimeCity.SpawnOnStartEnabled ? 1 : 0)}";
        string blockerState = runtimeGridBlockers == null
            ? "null"
            : $"spawned={(runtimeGridBlockers.HasSpawned ? 1 : 0)}";
        string decorationState = runtimeDecorations == null
            ? "null"
            : $"spawned={(runtimeDecorations.HasSpawned ? 1 : 0)}";

        GetInitialSpawnCounts(out int spawnConfigs, out int spawnInitialized, out int spawnProgress);

        Debug.Log(
            $"[LoadingGate] waiting frame={Time.frameCount} gameplayInitialized={(gameplayInitialized ? 1 : 0)} " +
            $"playRequested={(playRequested ? 1 : 0)} city={cityState} blockers={blockerState} decorations={decorationState} " +
            $"initialSpawn=configs:{spawnConfigs},initialized:{spawnInitialized},progress:{spawnProgress}");
    }

    private static void GetInitialSpawnCounts(
        out int configCount,
        out int initializedCount,
        out int progressCount)
    {
        configCount = 0;
        initializedCount = 0;
        progressCount = 0;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        EntityQuery configQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        EntityQuery initializedQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());
        EntityQuery progressQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnProgress>());

        configCount = configQuery.CalculateEntityCount();
        initializedCount = initializedQuery.CalculateEntityCount();
        progressCount = progressQuery.CalculateEntityCount();

        configQuery.Dispose();
        initializedQuery.Dispose();
        progressQuery.Dispose();
    }
}
