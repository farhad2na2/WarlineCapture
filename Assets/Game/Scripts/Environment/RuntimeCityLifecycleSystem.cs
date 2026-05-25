using System.Collections;

internal sealed class RuntimeCityLifecycleSystem
{
    private IEnumerator _generationRoutine;
    private int _generationStartedFrame = -1;
    private int _generationMoveNextCount;
    private int _nextGenerationDiagnosticFrame;
    private bool _spawned;

    public bool IsSpawned => _spawned;
    public bool IsGenerating => _generationRoutine != null;

    public bool HasSpawned(int cityCount)
    {
        return _spawned || cityCount <= 0;
    }

    public bool ShouldYield(int completedWorkItems, int generationYieldInterval)
    {
        return generationYieldInterval > 0 &&
            completedWorkItems > 0 &&
            (completedWorkItems % generationYieldInterval) == 0;
    }

    public void MarkSpawned()
    {
        _spawned = true;
        _generationRoutine = null;
    }

    public void CancelGeneration()
    {
        _generationRoutine = null;
    }

    public bool TryBeginGeneration(IEnumerator generationRoutine, Context context)
    {
        if (_spawned || _generationRoutine != null || generationRoutine == null)
            return false;
        if (context.CityCount <= 0)
            return false;

        _generationStartedFrame = context.FrameCount;
        _generationMoveNextCount = 0;
        _nextGenerationDiagnosticFrame = context.FrameCount;
        _generationRoutine = generationRoutine;

        context.Diagnostics?.LogLifecycleStart(
            context.FrameCount,
            context.CityCount,
            context.GenerateBuildings,
            context.GenerationYieldInterval);

        return true;
    }

    public void Tick(Context context)
    {
        if (_generationRoutine == null)
            return;

        IEnumerator currentRoutine = _generationRoutine;
        _generationMoveNextCount++;
        if (context.FrameCount >= _nextGenerationDiagnosticFrame)
        {
            _nextGenerationDiagnosticFrame = context.FrameCount + 120;
            context.Diagnostics?.LogLifecycleGenerating(
                context.FrameCount,
                _generationStartedFrame,
                _generationMoveNextCount,
                context.CityCount,
                context.GenerateBuildings,
                context.GenerationYieldInterval);
        }

        if (currentRoutine.MoveNext())
            return;

        if (_generationRoutine == currentRoutine)
            _generationRoutine = null;

        context.Diagnostics?.LogLifecycleEnded(
            context.FrameCount,
            _generationStartedFrame,
            _generationMoveNextCount,
            _spawned);
    }

    public void CompleteGeneration(int generatedCityCount, Context context)
    {
        _spawned = true;
        _generationRoutine = null;

        context.Diagnostics?.LogLifecycleCompleted(
            context.FrameCount,
            _generationStartedFrame,
            _generationMoveNextCount,
            generatedCityCount);
    }

    public readonly struct Context
    {
        public readonly int FrameCount;
        public readonly int CityCount;
        public readonly bool GenerateBuildings;
        public readonly int GenerationYieldInterval;
        public readonly RuntimeCityDiagnosticSystem Diagnostics;

        public Context(
            int frameCount,
            int cityCount,
            bool generateBuildings,
            int generationYieldInterval,
            RuntimeCityDiagnosticSystem diagnostics)
        {
            FrameCount = frameCount;
            CityCount = cityCount;
            GenerateBuildings = generateBuildings;
            GenerationYieldInterval = generationYieldInterval;
            Diagnostics = diagnostics;
        }
    }
}
