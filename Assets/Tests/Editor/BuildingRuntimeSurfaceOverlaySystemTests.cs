using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class BuildingRuntimeSurfaceOverlaySystemTests
{
    public static void RunFocusedValidation()
    {
        var tests = new BuildingRuntimeSurfaceOverlaySystemTests();
        tests.RunwayOverlayHeightUsesAuthoredSurfaceCenterInsteadOfRendererTop();
        Debug.Log("[BuildingRuntimeSurfaceOverlayValidation] result=Passed tests=1");
    }

    [Test]
    public void RunwayOverlayHeightUsesAuthoredSurfaceCenterInsteadOfRendererTop()
    {
        Vector3 runwaySurfaceCenter = new(12f, 0.005f, 34f);

        Assert.AreEqual(
            0.005f,
            BuildingRuntimeSurfaceOverlaySystem.ResolveRunwayOverlayHeight(runwaySurfaceCenter),
            0.0001f);
    }
}
#endif
