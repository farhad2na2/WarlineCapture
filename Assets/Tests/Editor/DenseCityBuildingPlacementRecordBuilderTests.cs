using Game.Components;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DenseCityBuildingPlacementRecordBuilderTests
{
    private const string ConfigPath =
        "Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset";
    private const string ShopPath =
        "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_04.prefab";

    [Test]
    public void Create_UsesProductionDestroyedPolicyAndSelectedPersistentMaterials()
    {
        RuntimeCitySpawnerSystemConfig config =
            AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(ConfigPath);
        GameObject intactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPath);
        GameObject destroyedPrefab = config.GetGeneratedDestroyedVisualPrefab(GeneratedCityBuildingRole.Shop);
        DenseCityBuildingMaterialLibrary materialLibrary = DenseCityBuildingMaterialLibrary.LoadExisting();
        DenseCityBuildingMaterialSelection selection =
            DenseCityBuildingMaterialVariantSelector.Select(
                new Vector3(10f, 0f, -5f),
                config.RandomSeed,
                GeneratedCityBuildingRole.Shop,
                true,
                false);
        Matrix4x4 matrix = Matrix4x4.TRS(
            new Vector3(10f, 2f, -5f),
            Quaternion.Euler(0f, 90f, 0f),
            Vector3.one);

        DenseCityBuildingRecordGroup group = DenseCityBuildingPlacementRecordBuilder.Create(
            new DenseCityBuildingPlacementRecordRequest(
                "dense-city-v1",
                unchecked((int)config.RandomSeed),
                3,
                25,
                intactPrefab,
                destroyedPrefab,
                selection,
                matrix,
                new Vector2Int(20, 30),
                new Vector2Int(8, 6),
                new Vector2(8f, 6f),
                2f,
                new Bounds(new Vector3(10f, 4f, -5f), new Vector3(6f, 4f, 8f)),
                Vector3.right,
                0,
                config.DefaultBuildingMaxHealth,
                (uint)(MapSurfaceMovementMask.AllGroundUnits |
                       MapSurfaceMovementMask.AirGrounded |
                       MapSurfaceMovementMask.BuildingPlacement),
                0,
                new Vector2Int(1, 2)),
            materialLibrary);

        string selectedMaterialGuid = AssetDatabase.AssetPathToGUID(
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/dense_city_building_materials/DenseCity_Facade_A_04.mat");
        Assert.That(group.Building.Identity.DeterministicSequence, Is.EqualTo(25));
        Assert.That(group.Building.OriginCell, Is.EqualTo(new Vector2Int(20, 30)));
        Assert.That(group.Building.FootprintCells, Is.EqualTo(new Vector2Int(8, 6)));
        Assert.That(group.Building.MaximumHealth, Is.EqualTo(config.DefaultBuildingMaxHealth));
        Assert.That(group.IntactPresentation.PrefabAssetGuid, Is.EqualTo(AssetDatabase.AssetPathToGUID(ShopPath)));
        Assert.That(group.IntactPresentation.MaterialAssetGuids.ToArray(), Does.Contain(selectedMaterialGuid));
        Assert.That(group.DestroyedPresentation.PrefabAssetGuid,
            Is.EqualTo(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(destroyedPrefab))));
    }
}
