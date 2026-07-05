using NUnit.Framework;
using UnityEngine;
using Game.Configs;
using Game.Runtime;

public sealed class MapBuildingPlacementStartupCompletionTests
{
    public static void RunFocusedValidation()
    {
        var tests = new MapBuildingPlacementStartupCompletionTests();
        tests.EmptyPlacementConfigCompletesOnlyAfterAuthoringRootIsHidden();
        UnityEngine.Debug.Log("[MapBuildingPlacementStartupCompletionValidation] result=Passed tests=1");
    }

    [Test]
    public void EmptyPlacementConfigCompletesOnlyAfterAuthoringRootIsHidden()
    {
        MapBuildingPlacementConfig config = ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
        GameObject root = new("Buildings");
        try
        {
            MapBuildingPlacementSpawnPrefabSystemHelper system = new();
            var context = new MapBuildingPlacementSpawnPrefabSystemHelper.Context(
                config,
                root.transform,
                runtimeSpawnSystem: null,
                runtimeSpawnContext: default,
                tryGetGridData: null,
                logWarning: null);

            Assert.IsFalse(system.IsCompleteFor(config, root.transform));

            system.Update(context);
            Assert.IsFalse(system.IsCompleteFor(config, root.transform));
            Assert.IsTrue(root.activeSelf);

            system.Update(context);
            Assert.IsTrue(system.IsCompleteFor(config, root.transform));
            Assert.IsFalse(root.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(config);
        }
    }
}
