using System;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RuntimeCitySpawnerGeneratedBuildingPolicyTests
{
    private const string ConfigPath = "Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset";
    private const string MapWideConfigPath =
        "Assets/Game/Configs/OperationMaps/Skirmish/SkirmishDesertBase_MapWideCity_Config.asset";

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new RuntimeCitySpawnerGeneratedBuildingPolicyTests();
            tests.ProductionConfig_ProvidesDestroyedVisualForEveryGeneratedBuildingRole(ConfigPath);
            tests.ProductionConfig_ProvidesDestroyedVisualForEveryGeneratedBuildingRole(MapWideConfigPath);
            Debug.Log("[RuntimeCitySpawnerGeneratedBuildingPolicyValidation] result=Passed configs=2");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[RuntimeCitySpawnerGeneratedBuildingPolicyValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [TestCase(ConfigPath)]
    [TestCase(MapWideConfigPath)]
    public void ProductionConfig_ProvidesDestroyedVisualForEveryGeneratedBuildingRole(string configPath)
    {
        RuntimeCitySpawnerSystemConfig config =
            AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(configPath);
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
