using System.Linq;
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
}
