using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DenseCityBuildingMaterialLibraryTests
{
    [Test]
    public void LoadExisting_ResolvesExactPersistentFacadeAndShopAssets()
    {
        DenseCityBuildingMaterialLibrary library = DenseCityBuildingMaterialLibrary.LoadExisting();
        Material facadeA = LoadMaterial(
            "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_01_A.mat");
        Material shopA = LoadMaterial(
            "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_03_A.mat");

        DenseCityBuildingMaterialSelection facadeSelection =
            DenseCityBuildingMaterialVariantSelector.Select(
                new Vector3(10f, 0f, -5f),
                24681357u,
                GeneratedCityBuildingRole.House,
                true,
                false);
        DenseCityBuildingMaterialSelection shopSelection =
            DenseCityBuildingMaterialVariantSelector.Select(
                new Vector3(10f, 0f, -5f),
                24681357u,
                GeneratedCityBuildingRole.Shop,
                true,
                true);

        Material facadeVariant = library.Resolve(facadeA, facadeSelection);
        Material shopVariant = library.Resolve(shopA, shopSelection);
        Material shopFacadeVariant = library.Resolve(facadeA, shopSelection);

        Assert.That(library.IsFacadeFamily(facadeA), Is.True);
        Assert.That(library.IsShopFamily(shopA), Is.True);
        Assert.That(AssetDatabase.GetAssetPath(facadeVariant),
            Is.EqualTo("Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/dense_city_building_materials/DenseCity_Facade_A_04.mat"));
        Assert.That(AssetDatabase.GetAssetPath(shopVariant),
            Is.EqualTo("Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/dense_city_building_materials/DenseCity_Shop05_A_04.mat"));
        Assert.That(AssetDatabase.GetAssetPath(shopFacadeVariant),
            Is.EqualTo("Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/dense_city_building_materials/DenseCity_Facade_A_05.mat"));
    }

    [Test]
    public void Resolve_PreservesUnsupportedAndOriginalShopMaterials()
    {
        DenseCityBuildingMaterialLibrary library = DenseCityBuildingMaterialLibrary.LoadExisting();
        Material unsupported = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
        Material shopA = LoadMaterial(
            "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_03_A.mat");
        var noVariant = new DenseCityBuildingMaterialSelection(false, false, -1, -1, -1);
        var originalShop = new DenseCityBuildingMaterialSelection(true, true, -1, -1, 0);

        Assert.That(library.Resolve(unsupported, noVariant), Is.SameAs(unsupported));
        Assert.That(library.Resolve(unsupported, originalShop), Is.SameAs(unsupported));
        Assert.That(library.Resolve(shopA, originalShop), Is.SameAs(shopA));
    }

    private static Material LoadMaterial(string path) =>
        AssetDatabase.LoadAssetAtPath<Material>(path) ??
        throw new AssertionException($"Missing test material {path}.");
}
