#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;

public sealed class PerformanceDiagnosticsSystemAllocationTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new PerformanceDiagnosticsSystemAllocationTests();
            tests.EndStepDoesNotAllocateAfterWarmup();
            UnityEngine.Debug.Log("[PerformanceDiagnosticsAllocationValidation] result=Passed tests=1");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[PerformanceDiagnosticsAllocationValidation] result=Failed");
            throw;
        }
    }

    [Test]
    public void EndStepDoesNotAllocateAfterWarmup()
    {
        var diagnosticsSystem = new PerformanceDiagnosticsSystem();
        double start = diagnosticsSystem.BeginStep();
        diagnosticsSystem.EndStep("UnitRenderBudgetSystem", start);
        for (int i = 0; i < 256; i++)
            diagnosticsSystem.EndStep("UnitRenderBudgetSystem", diagnosticsSystem.BeginStep());

        int timeBaselineBlocks = CountGcAllocationBlocks(() =>
        {
            for (int i = 0; i < 128; i++)
            {
                diagnosticsSystem.BeginStep();
                diagnosticsSystem.BeginStep();
            }
        });

        diagnosticsSystem.BeginUpdate(gameplayActive: false);
        int measuredBlocks = CountGcAllocationBlocks(() =>
        {
            for (int i = 0; i < 128; i++)
                diagnosticsSystem.EndStep("UnitRenderBudgetSystem", diagnosticsSystem.BeginStep());
        });

        Assert.LessOrEqual(
            measuredBlocks,
            timeBaselineBlocks,
            $"EndStep allocated beyond the warmed Unity time baseline. baseline={timeBaselineBlocks} measured={measuredBlocks}");
    }

    private static int CountGcAllocationBlocks(System.Action action)
    {
        UnityEngine.Profiling.Recorder recorder = UnityEngine.Profiling.Recorder.Get("GC.Alloc");
        recorder.enabled = false;
#if !UNITY_WEBGL
        recorder.FilterToCurrentThread();
#endif
        recorder.enabled = true;
        try
        {
            action();
        }
        finally
        {
            recorder.enabled = false;
#if !UNITY_WEBGL
            recorder.CollectFromAllThreads();
#endif
        }

        return recorder.sampleBlockCount;
    }
}
#endif
