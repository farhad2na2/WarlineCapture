using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using Game.Configs;
using Game.Runtime;

public sealed class MapVehiclePlacementStartupCompletionTests
{
    public static void RunFocusedValidation()
    {
        var tests = new MapVehiclePlacementStartupCompletionTests();
        tests.EmptyPlacementConfigCompletesAfterAuthoringRootIsHidden();
        UnityEngine.Debug.Log("[MapVehiclePlacementStartupCompletionValidation] result=Passed tests=1");
    }

    [Test]
    public void EmptyPlacementConfigCompletesAfterAuthoringRootIsHidden()
    {
        MapVehiclePlacementConfig config = ScriptableObject.CreateInstance<MapVehiclePlacementConfig>();
        GameObject root = new("Vehicles");
        World world = new("MapVehiclePlacementStartupCompletionTests");
        try
        {
            var unitPrefabContext = new RuntimeUnitPrefabSystem.Context(
                spawnPrefabSystem: default,
                tryGetEntityManager: TryGetEntityManager,
                ensureEntityQueries: null,
                createSpawnPrefabContext: null);
            MapVehiclePlacementSpawnPrefabSystemHelper system = new();
            var context = new MapVehiclePlacementSpawnPrefabSystemHelper.Context(
                config,
                root.transform,
                unitPrefabSystem: default,
                unitPrefabContext,
                tryGetGridData: null,
                logWarning: null);

            Assert.IsFalse(system.IsCompleteFor(config, root.transform));

            system.Update(context);
            Assert.IsTrue(system.IsCompleteFor(config, root.transform));
            Assert.IsFalse(root.activeSelf);
        }
        finally
        {
            world.Dispose();
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(config);
        }

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = world.EntityManager;
            return true;
        }
    }
}
