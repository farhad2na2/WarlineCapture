using System;
using System.Diagnostics;
using Unity.Mathematics;

public sealed class MapSurfacePerformanceValidationSystem
{
    public const double BaselineFrameBudgetMilliseconds = 16.67d;
    public const long MaxSamplingAllocationBytes = 128;

    private readonly MapSurfaceQuerySystem _querySystem = new();
    private readonly MapSurfacePathingValidationSystem _pathingValidationSystem = new();

    public readonly struct Result
    {
        public readonly int SampleIterations;
        public readonly int HeightSamples;
        public readonly int NormalSamples;
        public readonly int PathingChecks;
        public readonly long AllocatedBytes;
        public readonly long ElapsedTicks;
        public readonly int EstimatedSurfaceBytes;
        public readonly bool StayedWithinFrameBudget;
        public readonly bool StayedWithinAllocationBudget;

        public Result(
            int sampleIterations,
            int heightSamples,
            int normalSamples,
            int pathingChecks,
            long allocatedBytes,
            long elapsedTicks,
            int estimatedSurfaceBytes,
            bool stayedWithinFrameBudget,
            bool stayedWithinAllocationBudget)
        {
            SampleIterations = sampleIterations;
            HeightSamples = heightSamples;
            NormalSamples = normalSamples;
            PathingChecks = pathingChecks;
            AllocatedBytes = allocatedBytes;
            ElapsedTicks = elapsedTicks;
            EstimatedSurfaceBytes = estimatedSurfaceBytes;
            StayedWithinFrameBudget = stayedWithinFrameBudget;
            StayedWithinAllocationBudget = stayedWithinAllocationBudget;
        }
    }

    public Result RunSamplingProbe(MapSurfaceComponent surface, int sampleIterations)
    {
        int iterations = math.max(1, sampleIterations);
        MapSurfaceQuerySystem.Context context = new(surface);
        RunWarmup(surface, context);

        var stopwatch = new Stopwatch();
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        stopwatch.Start();

        int heightSamples = 0;
        int normalSamples = 0;
        int pathingChecks = 0;
        int2 dimensions = math.max(surface.Dimensions, new int2(1, 1));
        for (int i = 0; i < iterations; i++)
        {
            int2 cell = new(i % dimensions.x, (i / dimensions.x) % dimensions.y);
            if (_querySystem.TrySampleHeight(context, cell, out _))
                heightSamples++;
            if (_querySystem.TrySampleNormal(context, cell, out _))
                normalSamples++;
            if (_pathingValidationSystem.CanTraverse(surface, surface.HasSurfaceData, cell, MapSurfaceMovementMask.Infantry))
                pathingChecks++;
        }

        stopwatch.Stop();
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        double elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        bool stayedWithinFrameBudget = elapsedMilliseconds <= BaselineFrameBudgetMilliseconds;
        bool stayedWithinAllocationBudget = allocatedBytes <= MaxSamplingAllocationBytes;
        return new Result(
            iterations,
            heightSamples,
            normalSamples,
            pathingChecks,
            allocatedBytes,
            stopwatch.ElapsedTicks,
            EstimateSurfaceMemoryBytes(surface),
            stayedWithinFrameBudget,
            stayedWithinAllocationBudget);
    }

    public int EstimateSurfaceMemoryBytes(MapSurfaceComponent surface)
    {
        if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
            return 0;

        ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
        const int estimatedCellBytes = 8;
        const int estimatedSampleBytes = 64;
        const int estimatedConnectionBytes = 24;
        return blob.Cells.Length * estimatedCellBytes +
               blob.Samples.Length * estimatedSampleBytes +
               blob.Connections.Length * estimatedConnectionBytes;
    }

    private void RunWarmup(MapSurfaceComponent surface, MapSurfaceQuerySystem.Context context)
    {
        if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
            return;

        int2 cell = int2.zero;
        _querySystem.TrySampleHeight(context, cell, out _);
        _querySystem.TrySampleNormal(context, cell, out _);
        _pathingValidationSystem.CanTraverse(surface, surface.HasSurfaceData, cell, MapSurfaceMovementMask.Infantry);
    }
}
