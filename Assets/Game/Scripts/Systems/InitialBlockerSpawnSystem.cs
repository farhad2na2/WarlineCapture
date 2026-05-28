using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public readonly struct InitialBlockerSpawnSystem
{
    public readonly struct Result
    {
        public readonly int TargetCount;
        public readonly int ProgressIncrement;
        public readonly int SpawnedForLog;

        public Result(int targetCount, int progressIncrement, int spawnedForLog)
        {
            TargetCount = targetCount;
            ProgressIncrement = progressIncrement;
            SpawnedForLog = spawnedForLog;
        }
    }

    public Result SpawnBatch(
        ref Unity.Mathematics.Random rng,
        EntityManager em,
        EntityCommandBuffer ecb,
        InitialUnitsSpawnConfig config,
        bool useM01CompactRuntime,
        int initialBlockerBatchSize,
        int blockersSpawned,
        GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeBitArray occupied,
        ref NativeBitArray reserved,
        bool enableDiagnostics,
        ref InitialSpawnDiagnosticLogSystem diagnosticLogSystem)
    {
        int blockerTargetCount = useM01CompactRuntime ? 0 : config.BlockerCount;
        int blockersToSpawn = math.min(initialBlockerBatchSize, math.max(0, blockerTargetCount - blockersSpawned));
        int spawnedForLog = 0;

        for (int i = 0; i < blockersToSpawn; i++)
        {
            if (config.BlockerPrefab == Entity.Null)
                break;

            int2 center = new int2(grid.Width / 2, grid.Height / 2);
            int radius = math.max(0, config.SpawnRadiusCells) + 20;
            if (!SpawnCellUtility.TryFindSpawnCellNear(ref rng, grid, walkable, dynamicBlocked, occupied, ref reserved, center, radius, new int2(1, 1), out int2 cell))
            {
                if (enableDiagnostics)
                    diagnosticLogSystem.EnqueueWarning(em, $"[InitialSpawn] no-free-blocker-cell center={center} radius={radius}");
                break;
            }

            Entity instance = ecb.Instantiate(config.BlockerPrefab);
            ecb.SetComponent(instance, new UnitGrid { Cell = cell });
            ecb.SetComponent(instance, LocalTransform.FromPosition(GridUtils.CellToWorldCenter(grid, cell)));
            spawnedForLog++;
        }

        return new Result(blockerTargetCount, blockersToSpawn, spawnedForLog);
    }
}
