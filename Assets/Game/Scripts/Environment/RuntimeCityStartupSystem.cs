using System.Collections.Generic;
using UnityEngine;

internal sealed class RuntimeCityStartupSystem
{
    private readonly RuntimeCityStartupState _state = new();

    public Result Evaluate(Context context)
    {
        return _state.Evaluate(context);
    }

    public Result EvaluateManualGeneration(Context context)
    {
        return _state.EvaluateManualGeneration(context);
    }

    public static string DescribeStartupBlocker(Context context)
    {
        return RuntimeCityStartupState.DescribeStartupBlocker(context);
    }

    public delegate bool TryGetPendingInitialUnitsDelegate(out int totalConfigs, out int initializedConfigs);

    public delegate bool TryGetRoadCellSizeDelegate(out int roadCellSizeInGridCells);

    public delegate bool TryGetGridDataDelegate(out GridConfig grid);

    public readonly struct Context
    {
        public readonly int FrameCount;
        public readonly bool SpawnOnStart;
        public readonly bool IsSpawned;
        public readonly int CityCount;
        public readonly bool PlayRequested;
        public readonly bool IsMissionExcluded;
        public readonly bool GenerateBuildings;
        public readonly bool HasRoadRuntimeGenerationSystem;
        public readonly bool HasSpawnSystem;
        public readonly IReadOnlyCollection<GameObject> HallPrefabs;
        public readonly IReadOnlyCollection<GameObject> ShopPrefabs;
        public readonly IReadOnlyCollection<GameObject> HousePrefabs;
        public readonly TryGetPendingInitialUnitsDelegate TryGetPendingInitialUnits;
        public readonly TryGetRoadCellSizeDelegate TryGetRoadCellSize;
        public readonly TryGetGridDataDelegate TryGetGridData;
        public readonly RuntimeCityDiagnosticsSystemHelper Diagnostics;

        public Context(
            int frameCount,
            bool spawnOnStart,
            bool isSpawned,
            int cityCount,
            bool playRequested,
            bool isMissionExcluded,
            bool generateBuildings,
            bool hasRoadRuntimeGenerationSystem,
            bool hasSpawnSystem,
            IReadOnlyCollection<GameObject> hallPrefabs,
            IReadOnlyCollection<GameObject> shopPrefabs,
            IReadOnlyCollection<GameObject> housePrefabs,
            TryGetPendingInitialUnitsDelegate tryGetPendingInitialUnits,
            TryGetRoadCellSizeDelegate tryGetRoadCellSize,
            TryGetGridDataDelegate tryGetGridData,
            RuntimeCityDiagnosticsSystemHelper diagnostics)
        {
            FrameCount = frameCount;
            SpawnOnStart = spawnOnStart;
            IsSpawned = isSpawned;
            CityCount = cityCount;
            PlayRequested = playRequested;
            IsMissionExcluded = isMissionExcluded;
            GenerateBuildings = generateBuildings;
            HasRoadRuntimeGenerationSystem = hasRoadRuntimeGenerationSystem;
            HasSpawnSystem = hasSpawnSystem;
            HallPrefabs = hallPrefabs;
            ShopPrefabs = shopPrefabs;
            HousePrefabs = housePrefabs;
            TryGetPendingInitialUnits = tryGetPendingInitialUnits;
            TryGetRoadCellSize = tryGetRoadCellSize;
            TryGetGridData = tryGetGridData;
            Diagnostics = diagnostics;
        }
    }

    public readonly struct Result
    {
        public readonly ResultKind Kind;
        public readonly GridConfig Grid;
        public readonly int RoadCellSizeInGridCells;

        private Result(ResultKind kind, GridConfig grid, int roadCellSizeInGridCells)
        {
            Kind = kind;
            Grid = grid;
            RoadCellSizeInGridCells = roadCellSizeInGridCells;
        }

        public static Result None => new(ResultKind.None, default, 0);

        public static Result MarkSpawned => new(ResultKind.MarkSpawned, default, 0);

        public static Result Generate(GridConfig grid, int roadCellSizeInGridCells)
        {
            return new Result(ResultKind.Generate, grid, roadCellSizeInGridCells);
        }
    }

    public enum ResultKind
    {
        None,
        MarkSpawned,
        Generate
    }
}

internal sealed class RuntimeCityStartupState
{
    private int _nextInitialSpawnWaitDiagnosticFrame;

    public RuntimeCityStartupSystem.Result Evaluate(RuntimeCityStartupSystem.Context context)
    {
        if (!context.SpawnOnStart || context.IsSpawned)
            return RuntimeCityStartupSystem.Result.None;
        if (context.CityCount <= 0)
            return RuntimeCityStartupSystem.Result.None;
        if (!context.PlayRequested)
            return RuntimeCityStartupSystem.Result.None;
        if (context.IsMissionExcluded)
            return RuntimeCityStartupSystem.Result.MarkSpawned;
        if (context.TryGetPendingInitialUnits != null &&
            context.TryGetPendingInitialUnits(out int initialSpawnConfigs, out int initializedInitialSpawnConfigs))
        {
            LogInitialSpawnWait(context, initialSpawnConfigs, initializedInitialSpawnConfigs);
            return RuntimeCityStartupSystem.Result.None;
        }

        return TryCreateGenerateResult(context);
    }

    public RuntimeCityStartupSystem.Result EvaluateManualGeneration(RuntimeCityStartupSystem.Context context)
    {
        if (context.IsSpawned)
            return RuntimeCityStartupSystem.Result.None;
        if (context.CityCount <= 0)
            return RuntimeCityStartupSystem.Result.None;

        return TryCreateGenerateResult(context);
    }

    public static string DescribeStartupBlocker(RuntimeCityStartupSystem.Context context)
    {
        if (!context.SpawnOnStart)
            return "spawnOnStart=0";
        if (context.IsSpawned)
            return "alreadySpawned";
        if (context.CityCount <= 0)
            return $"cityCount={context.CityCount}";
        if (!context.PlayRequested)
            return "playRequested=0";
        if (context.IsMissionExcluded)
            return "missionExcluded";
        if (context.TryGetPendingInitialUnits != null &&
            context.TryGetPendingInitialUnits(out int initialSpawnConfigs, out int initializedInitialSpawnConfigs))
        {
            return $"pendingInitialUnits configs={initialSpawnConfigs} initialized={initializedInitialSpawnConfigs}";
        }
        if (!context.HasRoadRuntimeGenerationSystem)
            return "missingRoadRuntimeGenerationSystem";
        if (context.GenerateBuildings && !context.HasSpawnSystem)
            return "missingBuildingSpawnSystem";
        if (context.TryGetRoadCellSize == null)
            return "missingRoadCellSizeQuery";
        if (!context.TryGetRoadCellSize(out int roadCellSizeInGridCells))
            return "missingRoadCellSize";
        if (context.TryGetGridData == null)
            return "missingGridDataQuery";
        if (!context.TryGetGridData(out GridConfig grid))
            return "missingGridData";
        if (!HasRequiredPrefabs(context.HallPrefabs, context.ShopPrefabs, context.HousePrefabs))
        {
            int hallCount = context.HallPrefabs?.Count ?? 0;
            int shopCount = context.ShopPrefabs?.Count ?? 0;
            int houseCount = context.HousePrefabs?.Count ?? 0;
            return $"missingCityPrefabs hall={hallCount} shop={shopCount} house={houseCount}";
        }

        return $"readyToGenerate roadCellSize={roadCellSizeInGridCells} grid={grid.Width}x{grid.Height}";
    }

    private static RuntimeCityStartupSystem.Result TryCreateGenerateResult(RuntimeCityStartupSystem.Context context)
    {
        if (!context.HasRoadRuntimeGenerationSystem)
            return RuntimeCityStartupSystem.Result.None;
        if (context.GenerateBuildings && !context.HasSpawnSystem)
            return RuntimeCityStartupSystem.Result.None;
        if (context.TryGetRoadCellSize == null ||
            !context.TryGetRoadCellSize(out int roadCellSizeInGridCells))
        {
            return RuntimeCityStartupSystem.Result.None;
        }
        if (context.TryGetGridData == null ||
            !context.TryGetGridData(out GridConfig grid))
        {
            return RuntimeCityStartupSystem.Result.None;
        }
        if (!HasRequiredPrefabs(context.HallPrefabs, context.ShopPrefabs, context.HousePrefabs))
            return RuntimeCityStartupSystem.Result.None;

        return RuntimeCityStartupSystem.Result.Generate(grid, roadCellSizeInGridCells);
    }

    private void LogInitialSpawnWait(RuntimeCityStartupSystem.Context context, int initialSpawnConfigs, int initializedInitialSpawnConfigs)
    {
        if (context.FrameCount < _nextInitialSpawnWaitDiagnosticFrame)
            return;

        _nextInitialSpawnWaitDiagnosticFrame = context.FrameCount + 120;
        context.Diagnostics?.LogInitialSpawnWait(context.FrameCount, initialSpawnConfigs, initializedInitialSpawnConfigs);
    }

    private static bool HasRequiredPrefabs(
        IReadOnlyCollection<GameObject> hallPrefabs,
        IReadOnlyCollection<GameObject> shopPrefabs,
        IReadOnlyCollection<GameObject> housePrefabs)
    {
        return hallPrefabs != null &&
            hallPrefabs.Count > 0 &&
            shopPrefabs != null &&
            shopPrefabs.Count > 0 &&
            housePrefabs != null &&
            housePrefabs.Count > 0;
    }
}
