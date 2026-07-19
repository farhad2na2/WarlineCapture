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
        tests.RendererFreeMapUsesBakedPlacementPrefab();
        UnityEngine.Debug.Log("[MapBuildingPlacementStartupCompletionValidation] result=Passed tests=2");
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

    [Test]
    public void RendererFreeMapUsesBakedPlacementPrefab()
    {
        MapBuildingPlacementConfig config = ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
        GameObject authoringRoot = new("Buildings");
        GameObject runtimeRoot = new("RuntimeBuildings");
        GameObject prefab = new("Building_Test");
        try
        {
            var placement = new MapBuildingPlacementConfigEntry(
                "Map/Buildings/Building_Test/Visual",
                "Building_Test",
                prefab,
                factionId: 1,
                worldCenter: new Vector3(10f, 0f, 20f),
                worldPosition: new Vector3(10f, 0f, 20f),
                worldEulerAngles: new Vector3(0f, 45f, 0f),
                worldScale: Vector3.one,
                yawDegrees: 45f,
                rotateVertical: false);
            var context = new MapBuildingPlacementSpawnPrefabSystemHelper.Context(
                config,
                authoringRoot.transform,
                runtimeSpawnSystem: null,
                runtimeSpawnContext: default,
                tryGetGridData: null,
                logWarning: Assert.Fail);

            GameObject wrapper = MapBuildingPlacementSpawnPrefabSystemHelper.CreateAuthoredMapVisualInstance(
                context,
                placement,
                runtimeRoot.transform);

            Assert.That(wrapper, Is.Not.Null);
            Assert.That(wrapper.transform.childCount, Is.EqualTo(1));
            Assert.That(wrapper.transform.GetChild(0).name, Is.EqualTo(prefab.name));
            Assert.That(wrapper.transform.position, Is.EqualTo(placement.WorldPosition));
        }
        finally
        {
            Object.DestroyImmediate(runtimeRoot);
            Object.DestroyImmediate(authoringRoot);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(config);
        }
    }
}
