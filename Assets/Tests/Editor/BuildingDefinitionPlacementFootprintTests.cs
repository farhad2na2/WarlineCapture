using Game.Composition;
using Game.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BuildingDefinitionPlacementFootprintTests
{
    [Test]
    public void CanonicalBarracksRuntimeAndCatalogFootprintsFitTheM2Lot()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Buildings/Building_Barrack.prefab");
        Assert.NotNull(prefab);
        var definitions = new BuildingDefinitionPrefabSystemHelper();
        definitions.ConfigureAuthoringMetadataResolvers(BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
        var runtime = definitions.CreateRuntimeBuildingDefinition(prefab, "Barracks", "", Vector2Int.one, 1, null);
        var catalog = definitions.CreateDefinition(prefab, "Barracks", "", 1, null, null, null, null);
        Assert.AreEqual(new Vector2Int(28, 15), runtime.FootprintCells);
        Assert.AreEqual(runtime.FootprintCells, catalog.FootprintCells);
        Assert.LessOrEqual(runtime.LocalBounds.size.x, runtime.FootprintCells.x);
        Assert.LessOrEqual(runtime.LocalBounds.size.z, runtime.FootprintCells.y);
        Assert.AreEqual(new Vector3(2, 2, 2), prefab.transform.Find("Model").localScale,
            "The enlarged barracks must fit without shrinking the requested visual.");
    }

    [TestCase(10, 8, 2, 3, 2, 3)]
    [TestCase(2, 3, 10, 8, 10, 8)]
    [TestCase(10, 3, 2, 8, 2, 8)]
    public void FootprintFitsRenderedGeometryInsteadOfStaleAuthoredReservation(int authoredX, int authoredY,
        int visualX, int visualY, int expectedX, int expectedY)
    {
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var model = prefab.transform;
        // Child geometry gives stable model bounds in the building root's local space.
        var root = new GameObject("Footprint test"); model.SetParent(root.transform); model.localScale = new Vector3(visualX, 1, visualY);
        try
        {
            bool Metadata(GameObject _, out BuildingDefinitionPrefabSystemHelper.BuildingDefinitionMetadata metadata)
            { metadata = new BuildingDefinitionPrefabSystemHelper.BuildingDefinitionMetadata { FootprintCells = new Vector2Int(authoredX, authoredY), MaxHealth = 1 }; return true; }
            var definitions = new BuildingDefinitionPrefabSystemHelper(); definitions.ConfigureAuthoringMetadataResolvers(Metadata, null);
            var runtime = definitions.CreateRuntimeBuildingDefinition(root, "", "", Vector2Int.one, 1, null);
            var catalog = definitions.CreateDefinition(root, "", "", 1, null, null, null, null);
            Assert.AreEqual(new Vector2Int(expectedX, expectedY), runtime.FootprintCells);
            Assert.AreEqual(runtime.FootprintCells, catalog.FootprintCells);
            root.transform.rotation = Quaternion.Euler(0, 37, 0);
            root.transform.position = new Vector3(103, 0, -27);
            Assert.IsTrue(BuildingModelBounds.TryMeasure(root, out var rotated));
            Assert.That(rotated.size.x, Is.EqualTo(visualX).Within(.001f));
            Assert.That(rotated.size.z, Is.EqualTo(visualY).Within(.001f));
        }
        finally { Object.DestroyImmediate(root); }
    }
    [Test]
    public void FootprintIgnoresInactiveGeometryAndParticleEffects()
    {
        var root = new GameObject("Footprint geometry");
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(4, 3, 6);
        var hidden = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hidden.transform.SetParent(root.transform, false);
        hidden.transform.localScale = Vector3.one * 100;
        hidden.SetActive(false);
        var line = new GameObject("Range overlay").AddComponent<LineRenderer>();
        line.transform.SetParent(root.transform, false);
        line.positionCount = 2; line.SetPosition(0, Vector3.one * -500); line.SetPosition(1, Vector3.one * 500);
        try
        {
            Assert.IsTrue(BuildingModelBounds.TryMeasure(root, out var bounds));
            Assert.AreEqual(new Vector2Int(4, 6), BuildingModelBounds.EnclosingCells(bounds));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void RadarFitsItsModelAndGateKeepsMovingArmClearance()
    {
        foreach (var entry in new[] { ("Building_Satelite_Dish", new Vector2Int(10, 10)), ("Building_Road_Barrier", new Vector2Int(8, 2)) })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Buildings/" + entry.Item1 + ".prefab");
            var definitions = new BuildingDefinitionPrefabSystemHelper();
            definitions.ConfigureAuthoringMetadataResolvers(BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            var runtime = definitions.CreateRuntimeBuildingDefinition(prefab, "", "", Vector2Int.one, 1, null);
            Assert.AreEqual(entry.Item2, runtime.FootprintCells);
        }
    }

}
