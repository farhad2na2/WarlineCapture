using System;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

public sealed class GameplayRuntimeUpdateSystem
{
    private const int LoadingGateDiagnosticIntervalFrames = 120;
    private const int LoadingGateFailOpenFrames = 1800;
    private static readonly ProfilerMarker BeginUpdateMarker = new("GameplayRuntimeUpdate.BeginUpdate");
    private static readonly ProfilerMarker RoadBuildMarker = new("GameplayRuntimeUpdate.RoadBuild");
    private static readonly ProfilerMarker BuildingPlacementMarker = new("GameplayRuntimeUpdate.BuildingPlacement");
    private static readonly ProfilerMarker SelectionMarker = new("GameplayRuntimeUpdate.Selection");
    private static readonly ProfilerMarker RuntimeCityMarker = new("GameplayRuntimeUpdate.RuntimeCity");
    private static readonly ProfilerMarker RuntimeGridBlockersMarker = new("GameplayRuntimeUpdate.RuntimeGridBlockers");
    private static readonly ProfilerMarker RuntimeDecorationsMarker = new("GameplayRuntimeUpdate.RuntimeDecorations");
    private static readonly ProfilerMarker DayNightMarker = new("GameplayRuntimeUpdate.DayNight");
    private static readonly ProfilerMarker CitizenPopulationMarker = new("GameplayRuntimeUpdate.CitizenPopulation");
    private static readonly ProfilerMarker MainMenuMarker = new("GameplayRuntimeUpdate.MainMenu");
    private static readonly ProfilerMarker LoadingGateMarker = new("GameplayRuntimeUpdate.LoadingGate");
    private static readonly ProfilerMarker EndUpdateMarker = new("GameplayRuntimeUpdate.EndUpdate");
    private int _nextLoadingGateDiagnosticFrame;
    private int _loadingGateStartedFrame = -1;
    private World _initialSpawnQueryWorld;
    private EntityQuery _initialSpawnConfigQuery;
    private EntityQuery _initialSpawnInitializedQuery;
    private EntityQuery _initialSpawnProgressQuery;
    private bool _hasInitialSpawnQueries;

    public void Update(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
        Action roadBuildRuntimeUpdate,
        BuildingRuntimeUpdateSystem buildingRuntimeUpdate,
        BuildingRuntimeUpdateSystem.Context buildingRuntimeUpdateContext,
        Action selectionRuntimeUpdate,
        Camera worldCamera,
        RuntimeCityCompositionSystem runtimeCity,
        RuntimeGridBlockerSystem runtimeGridBlockers,
        RuntimeDecorationSpawnerSystem runtimeDecorations,
        DayNightSystem dayNight,
        Action citizenPopulationRuntimeUpdate,
        IMatchRuntimeUi mainMenu,
        IUnitImpostorRenderer unitImpostors,
        ref bool gameplayStartPending)
    {
        bool gameplayActive = gameplayInitialized && runtimeGameplayStateSystem.PlayRequested;
        using (BeginUpdateMarker.Auto())
        {
            performanceDiagnosticsSystem.BeginUpdate(gameplayActive);
        }

        bool hadSlowStep = false;

        double stepStart;
        if (gameplayActive)
        {
            stepStart = performanceDiagnosticsSystem.BeginStep();
            using (RoadBuildMarker.Auto())
            {
                roadBuildRuntimeUpdate?.Invoke();
            }
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("RoadBuild", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            using (BuildingPlacementMarker.Auto())
            {
                buildingRuntimeUpdate?.Update(buildingRuntimeUpdateContext);
            }
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("BuildingPlacement", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            using (SelectionMarker.Auto())
            {
                selectionRuntimeUpdate?.Invoke();
            }
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("Selection", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            using (RuntimeCityMarker.Auto())
            {
                runtimeCity?.Update(Time.frameCount);
            }
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("RuntimeCity", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            using (RuntimeGridBlockersMarker.Auto())
            {
                runtimeGridBlockers?.Update();
            }
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("RuntimeGridBlockers", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            using (RuntimeDecorationsMarker.Auto())
            {
                runtimeDecorations?.Update();
            }
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("RuntimeDecorations", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            using (DayNightMarker.Auto())
            {
                dayNight?.Update();
            }
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("DayNight", stepStart);

            stepStart = performanceDiagnosticsSystem.BeginStep();
            using (CitizenPopulationMarker.Auto())
            {
                citizenPopulationRuntimeUpdate?.Invoke();
            }
            hadSlowStep |= performanceDiagnosticsSystem.EndStep("CitizenPopulation", stepStart);
        }

        stepStart = performanceDiagnosticsSystem.BeginStep();
        using (MainMenuMarker.Auto())
        {
            mainMenu?.Update();
        }
        hadSlowStep |= performanceDiagnosticsSystem.EndStep("MainMenu", stepStart);

        using (LoadingGateMarker.Auto())
        {
            if (gameplayStartPending && _loadingGateStartedFrame < 0)
                _loadingGateStartedFrame = Time.frameCount;

            if (gameplayStartPending && IsGameplayStartComplete(
                    gameplayInitialized,
                    runtimeGameplayStateSystem,
                    runtimeCity,
                    runtimeGridBlockers,
                    runtimeDecorations))
            {
                gameplayStartPending = false;
                _loadingGateStartedFrame = -1;
                Debug.Log($"[LoadingGate] ready frame={Time.frameCount} gameplayInitialized={(gameplayInitialized ? 1 : 0)} playRequested={(runtimeGameplayStateSystem.PlayRequested ? 1 : 0)}");
            }
            else if (gameplayStartPending && ShouldFailOpenLoadingGate(
                         gameplayInitialized,
                         runtimeGameplayStateSystem,
                         runtimeCity,
                         out string failOpenReason))
            {
                gameplayStartPending = false;
                _loadingGateStartedFrame = -1;
                runtimeCity?.MarkSpawnedAfterLoadingGateTimeout();
                Debug.LogError($"[LoadingGate] failOpen frame={Time.frameCount} reason={failOpenReason}");
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
        }

        using (EndUpdateMarker.Auto())
        {
            performanceDiagnosticsSystem.EndUpdate(
                gameplayActive,
                hadSlowStep,
                unitImpostors?.LastDrawnCount ?? 0,
                gameplayInitialized,
                runtimeGameplayStateSystem.PlayRequested);
        }
    }

    public void Dispose()
    {
        if (!_hasInitialSpawnQueries)
            return;

        if (_initialSpawnQueryWorld != null && _initialSpawnQueryWorld.IsCreated)
        {
            _initialSpawnConfigQuery.Dispose();
            _initialSpawnInitializedQuery.Dispose();
            _initialSpawnProgressQuery.Dispose();
        }

        _initialSpawnQueryWorld = null;
        _hasInitialSpawnQueries = false;
    }

    public void LateUpdate(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
        IUnitAttackTraceRenderer unitAttackTraces,
        IUnitImpostorRenderer unitImpostors)
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
        ISelectionRectangleView selectionRectangleView)
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

        EnsureInitialSpawnQueries(world.EntityManager);

        int totalConfigCount = _initialSpawnConfigQuery.CalculateEntityCount();
        int initializedConfigCount = _initialSpawnInitializedQuery.CalculateEntityCount();

        return totalConfigCount == 0 || initializedConfigCount >= totalConfigCount;
    }

    private bool ShouldFailOpenLoadingGate(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        RuntimeCityCompositionSystem runtimeCity,
        out string reason)
    {
        reason = string.Empty;
        if (_loadingGateStartedFrame < 0)
            return false;
        if (!gameplayInitialized || !runtimeGameplayStateSystem.PlayRequested)
            return false;
        if (Time.frameCount - _loadingGateStartedFrame < LoadingGateFailOpenFrames)
            return false;
        if (runtimeCity == null || runtimeCity.HasSpawned || runtimeCity.IsGenerating)
            return false;

        reason = $"runtimeCityNotGenerating blocker={runtimeCity.DescribeStartupBlocker(Time.frameCount)}";
        return true;
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
            : $"spawned={(runtimeCity.HasSpawned ? 1 : 0)} generating={(runtimeCity.IsGenerating ? 1 : 0)} spawnOnStart={(runtimeCity.SpawnOnStartEnabled ? 1 : 0)} blocker={runtimeCity.DescribeStartupBlocker(Time.frameCount)}";
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

    private void GetInitialSpawnCounts(
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

        EnsureInitialSpawnQueries(world.EntityManager);

        configCount = _initialSpawnConfigQuery.CalculateEntityCount();
        initializedCount = _initialSpawnInitializedQuery.CalculateEntityCount();
        progressCount = _initialSpawnProgressQuery.CalculateEntityCount();
    }

    private void EnsureInitialSpawnQueries(EntityManager entityManager)
    {
        World world = entityManager.World;
        if (_hasInitialSpawnQueries && _initialSpawnQueryWorld == world)
            return;

        if (_hasInitialSpawnQueries)
            Dispose();

        _initialSpawnQueryWorld = world;
        _initialSpawnConfigQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        _initialSpawnInitializedQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());
        _initialSpawnProgressQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnProgress>());
        _hasInitialSpawnQueries = true;
    }
}
