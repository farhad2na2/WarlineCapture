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
        Assert.AreEqual(new Vector2Int(40, 20), runtime.FootprintCells);
        Assert.AreEqual(runtime.FootprintCells, catalog.FootprintCells);
        Assert.LessOrEqual(runtime.LocalBounds.size.x, runtime.FootprintCells.x);
        Assert.LessOrEqual(runtime.LocalBounds.size.z, runtime.FootprintCells.y);
        Assert.AreEqual(new Vector3(2, 2, 2), prefab.transform.Find("Model").localScale,
            "The enlarged barracks must fit without shrinking the requested visual.");
    }

    [TestCase(10, 8, 2, 3, 10, 8)]
    [TestCase(2, 3, 10, 8, 10, 8)]
    [TestCase(10, 3, 2, 8, 10, 8)]
    public void FootprintContainsBothAuthoredReservationAndRenderedGeometry(int authoredX, int authoredY,
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
        }
        finally { Object.DestroyImmediate(root); }
    }
}
