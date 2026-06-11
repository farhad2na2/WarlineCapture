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

        long timeBaselineBytes = CountAllocatedBytes(() =>
        {
            for (int i = 0; i < 128; i++)
            {
                diagnosticsSystem.BeginStep();
                diagnosticsSystem.BeginStep();
            }
        });

        diagnosticsSystem.BeginUpdate(gameplayActive: false);
        long measuredBytes = CountAllocatedBytes(() =>
        {
            for (int i = 0; i < 128; i++)
                diagnosticsSystem.EndStep("UnitRenderBudgetSystem", diagnosticsSystem.BeginStep());
        });

        Assert.LessOrEqual(
            measuredBytes,
            timeBaselineBytes,
            $"EndStep allocated beyond the warmed Unity time baseline. baseline={timeBaselineBytes}B measured={measuredBytes}B");
    }

    private static long CountAllocatedBytes(System.Action action)
    {
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        action();
        return System.GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
#endif
