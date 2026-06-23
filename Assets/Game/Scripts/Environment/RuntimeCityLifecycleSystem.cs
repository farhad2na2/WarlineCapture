using System.Collections;

internal sealed class RuntimeCityLifecycleSystem
{
    private readonly RuntimeCityLifecycleState _state = new();

    public RuntimeCityLifecycleState State => _state;

    public bool IsSpawned => _state.IsSpawned;
    public bool IsGenerating => _state.IsGenerating;

    public bool HasSpawned(int cityCount)
    {
        return _state.HasSpawned(cityCount);
    }

    public bool ShouldYield(int completedWorkItems, int generationYieldInterval)
    {
        return _state.ShouldYield(completedWorkItems, generationYieldInterval);
    }

    public void MarkSpawned()
    {
        _state.MarkSpawned();
    }

    public void CancelGeneration()
    {
        _state.CancelGeneration();
    }

    public bool TryBeginGeneration(IEnumerator generationRoutine, Context context)
    {
        return _state.TryBeginGeneration(generationRoutine, context);
    }

    public void Tick(Context context)
    {
        _state.Tick(context);
    }

    public void CompleteGeneration(int generatedCityCount, Context context)
    {
        _state.CompleteGeneration(generatedCityCount, context);
    }

    public readonly struct Context
    {
        public readonly int FrameCount;
        public readonly int CityCount;
        public readonly bool GenerateBuildings;
        public readonly int GenerationYieldInterval;
        public readonly RuntimeCityDiagnosticsSystemHelper Diagnostics;

        public Context(
            int frameCount,
            int cityCount,
            bool generateBuildings,
            int generationYieldInterval,
            RuntimeCityDiagnosticsSystemHelper diagnostics)
        {
            FrameCount = frameCount;
            CityCount = cityCount;
            GenerateBuildings = generateBuildings;
            GenerationYieldInterval = generationYieldInterval;
            Diagnostics = diagnostics;
        }
    }
}

internal sealed class RuntimeCityLifecycleState
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

    public bool TryBeginGeneration(IEnumerator generationRoutine, RuntimeCityLifecycleSystem.Context context)
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

    public void Tick(RuntimeCityLifecycleSystem.Context context)
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

    public void CompleteGeneration(int generatedCityCount, RuntimeCityLifecycleSystem.Context context)
    {
        _spawned = true;
        _generationRoutine = null;

        context.Diagnostics?.LogLifecycleCompleted(
            context.FrameCount,
            _generationStartedFrame,
            _generationMoveNextCount,
            generatedCityCount);
    }

}
