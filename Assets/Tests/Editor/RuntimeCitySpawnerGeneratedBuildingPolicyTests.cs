using System;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;

public sealed class RuntimeCitySpawnerGeneratedBuildingPolicyTests
{
    private const string ConfigPath = "Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset";

    [Test]
    public void ProductionConfig_ProvidesDestroyedVisualForEveryGeneratedBuildingRole()
    {
        RuntimeCitySpawnerSystemConfig config =
            AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(ConfigPath);
        Assert.That(config, Is.Not.Null);

        AssertRole(config, GeneratedCityBuildingRole.House,
            "Assets/Game/Prefabs/Buildings/Destroyed/Building_House_Destroyed.prefab");
        AssertRole(config, GeneratedCityBuildingRole.Shop,
            "Assets/Game/Prefabs/Buildings/Destroyed/Building_Shop_Destroyed.prefab");
        AssertRole(config, GeneratedCityBuildingRole.Civic,
            "Assets/Game/Prefabs/Buildings/Destroyed/Building_Hall_Destroyed.prefab");
        AssertRole(config, GeneratedCityBuildingRole.Other,
            "Assets/Game/Prefabs/Buildings/Destroyed/Building_Barrack_Destroyed.prefab");
        Assert.That(
            () => config.GetGeneratedDestroyedVisualPrefab(GeneratedCityBuildingRole.None),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    private static void AssertRole(
        RuntimeCitySpawnerSystemConfig config,
        GeneratedCityBuildingRole role,
        string expectedPath)
    {
        Assert.That(config.GetGeneratedDestroyedVisualPrefab(role), Is.Not.Null, role.ToString());
        Assert.That(
            AssetDatabase.GetAssetPath(config.GetGeneratedDestroyedVisualPrefab(role)),
            Is.EqualTo(expectedPath),
            role.ToString());
    }
}
