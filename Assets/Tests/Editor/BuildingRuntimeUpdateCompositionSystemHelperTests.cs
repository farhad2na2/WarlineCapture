using NUnit.Framework;
using Game.Runtime;

public sealed class BuildingRuntimeUpdateCompositionSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        var tests = new BuildingRuntimeUpdateCompositionSystemHelperTests();
        tests.StartupCompleteDefaultsToTrueWhenNoGateIsProvided();
        tests.StartupCompleteUsesProvidedGate();
        UnityEngine.Debug.Log("[BuildingRuntimeUpdateFocusedValidation] result=Passed tests=2");
    }

    [Test]
    public void StartupCompleteDefaultsToTrueWhenNoGateIsProvided()
    {
        var context = new BuildingRuntimeUpdateCompositionSystemHelper.Context(
            updateBuildingStartupTick: null,
            updateBuildingSimulationTick: null,
            runtimeBuildingEntityLinks: null);

        Assert.IsTrue(context.StartupComplete);
    }

    [Test]
    public void StartupCompleteUsesProvidedGate()
    {
        bool complete = false;
        var context = new BuildingRuntimeUpdateCompositionSystemHelper.Context(
            updateBuildingStartupTick: null,
            updateBuildingSimulationTick: null,
            runtimeBuildingEntityLinks: null,
            isStartupComplete: () => complete);

        Assert.IsFalse(context.StartupComplete);
        complete = true;
        Assert.IsTrue(context.StartupComplete);
    }
}
