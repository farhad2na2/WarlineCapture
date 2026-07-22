using System.Linq;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DenseCityVisualAssetMetadataExtractorTests
{
    private const string ShopPrefabPath =
        "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_04.prefab";

    [Test]
    public void Extract_ReturnsStablePrefabAndSortedUniqueMaterialIdentity()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPrefabPath);
        Assert.That(prefab, Is.Not.Null);

        DenseCityVisualAssetMetadata metadata = DenseCityVisualAssetMetadataExtractor.Extract(prefab);
        string expectedGuid = AssetDatabase.AssetPathToGUID(ShopPrefabPath);

        Assert.That(metadata.PrefabAssetGuid, Is.EqualTo(expectedGuid));
        Assert.That(metadata.PrefabLocalId, Is.GreaterThan(0));
        Assert.That(metadata.MaterialAssetGuids, Is.Not.Empty);
        Assert.That(
            metadata.MaterialAssetGuids,
            Is.EqualTo(metadata.MaterialAssetGuids.OrderBy(value => value).Distinct().ToArray()));
    }

    [Test]
    public void Extract_RejectsNonPersistentSceneObject()
    {
        var instance = new GameObject("NonPersistentDenseCityVisual");
        try
        {
            Assert.That(
                () => DenseCityVisualAssetMetadataExtractor.Extract(instance),
                Throws.InvalidOperationException.With.Message.Contains("persistent asset"));
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void Extract_RecordsResolvedPersistentMaterialIdentityBeforeRealization()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPrefabPath);
        DenseCityBuildingMaterialLibrary library = DenseCityBuildingMaterialLibrary.LoadExisting();
        DenseCityBuildingMaterialSelection selection =
            DenseCityBuildingMaterialVariantSelector.Select(
                new Vector3(10f, 0f, -5f),
                24681357u,
                GeneratedCityBuildingRole.Shop,
                true,
                false);

        DenseCityVisualAssetMetadata metadata = DenseCityVisualAssetMetadataExtractor.Extract(
            prefab,
            material => library.Resolve(material, selection));
        string selectedMaterialGuid = AssetDatabase.AssetPathToGUID(
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/dense_city_building_materials/DenseCity_Facade_A_04.mat");

        Assert.That(selectedMaterialGuid, Is.Not.Empty);
        Assert.That(metadata.MaterialAssetGuids, Does.Contain(selectedMaterialGuid));
    }

    [Test]
    public void Extract_RejectsNullMaterialResolution()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPrefabPath);

        Assert.That(
            () => DenseCityVisualAssetMetadataExtractor.Extract(prefab, _ => null),
            Throws.InvalidOperationException.With.Message.Contains("resolver returned null"));
    }
}
