using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MaterialFabricationConfigProjectionTests
{
    private const string ConfigPath =
        "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Ammunition_Depot_Config.asset";
    private const string PrefabPath =
        "Assets/Game/Prefabs/Buildings/Building_Ammunition_Depot.prefab";

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new MaterialFabricationConfigProjectionTests();
            tests.AmmunitionDepotConfig_UsesFieldFabricationIdentityAndAuthoredBalance();
            tests.AmmunitionDepotPrefab_PreservesCompatibilityIdAndProjectsMetadata();
            tests.ConfigValidation_ReturnsTypedFailuresForInvalidAuthoredValues();
            Debug.Log("[MaterialFabricationConfigProjectionValidation] result=Passed tests=3");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MaterialFabricationConfigProjectionValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void AmmunitionDepotConfig_UsesFieldFabricationIdentityAndAuthoredBalance()
    {
        BuildingDefinitionAuthoringConfig config =
            AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(ConfigPath);

        Assert.IsNotNull(config);
        Assert.AreEqual("Field Fabrication Depot", config.DisplayName);
        Assert.AreEqual(
            "Consumes Oil to operate fabrication lines that produce Materials for construction, repairs, and battlefield infrastructure.",
            config.Description);
        Assert.AreEqual(24, config.OilStorageCapacity);
        Assert.IsTrue(config.MaterialFabricationEnabled);
        Assert.AreEqual(4f, config.MaterialFabricationOilConsumedPerCycle);
        Assert.AreEqual(20, config.MaterialFabricationMaterialsOutputPerCycle);
        Assert.AreEqual(30f, config.MaterialFabricationCycleDurationSeconds);
        Assert.AreEqual(
            MaterialFabricationOutputCapacityPolicyCode.RequireFullCycleCapacity,
            config.MaterialFabricationOutputCapacityPolicy);
        Assert.AreEqual(
            MaterialFabricationConfigValidationCode.Valid,
            config.ValidateMaterialFabricationConfiguration());
    }

    [Test]
    public void AmmunitionDepotPrefab_PreservesCompatibilityIdAndProjectsMetadata()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        Assert.IsNotNull(prefab);
        Assert.AreEqual("Building_Ammunition_Depot", prefab.name);
        Assert.IsTrue(
            BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata(
                prefab,
                out BuildingDefinitionPrefabSystemHelper.BuildingDefinitionMetadata metadata));
        Assert.AreEqual("Field Fabrication Depot", metadata.DisplayName);
        Assert.AreEqual(24, metadata.OilStorageCapacity);
        Assert.IsTrue(metadata.MaterialFabricationEnabled);
        Assert.AreEqual(4f, metadata.MaterialFabricationOilConsumedPerCycle);
        Assert.AreEqual(20, metadata.MaterialFabricationMaterialsOutputPerCycle);
        Assert.AreEqual(30f, metadata.MaterialFabricationCycleDurationSeconds);
        Assert.AreEqual(
            MaterialFabricationOutputCapacityPolicyCode.RequireFullCycleCapacity,
            metadata.MaterialFabricationOutputCapacityPolicy);
    }

    [Test]
    public void ConfigValidation_ReturnsTypedFailuresForInvalidAuthoredValues()
    {
        BuildingDefinitionAuthoringConfig config = ScriptableObject.CreateInstance<BuildingDefinitionAuthoringConfig>();
        try
        {
            var serialized = new SerializedObject(config);
            Set(serialized, "materialFabricationEnabled", true);
            AssertValidation(serialized, config, MaterialFabricationConfigValidationCode.MissingOilInputCapacity);

            Set(serialized, "oilStorageCapacity", 24);
            Set(serialized, "materialFabricationOilConsumedPerCycle", -1f);
            AssertValidation(serialized, config, MaterialFabricationConfigValidationCode.InvalidOilConsumption);

            Set(serialized, "materialFabricationOilConsumedPerCycle", 4f);
            Set(serialized, "materialFabricationMaterialsOutputPerCycle", 0);
            AssertValidation(serialized, config, MaterialFabricationConfigValidationCode.InvalidMaterialsOutput);

            Set(serialized, "materialFabricationMaterialsOutputPerCycle", 20);
            Set(serialized, "materialFabricationCycleDurationSeconds", 0f);
            AssertValidation(serialized, config, MaterialFabricationConfigValidationCode.InvalidCycleDuration);

            Set(serialized, "materialFabricationCycleDurationSeconds", 30f);
            Set(serialized, "materialFabricationOutputCapacityPolicy", 255);
            AssertValidation(serialized, config, MaterialFabricationConfigValidationCode.UnsupportedOutputCapacityPolicy);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    private static void AssertValidation(
        SerializedObject serialized,
        BuildingDefinitionAuthoringConfig config,
        MaterialFabricationConfigValidationCode expected)
    {
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Assert.AreEqual(expected, config.ValidateMaterialFabricationConfiguration());
    }

    private static void Set(SerializedObject serialized, string propertyName, bool value)
    {
        serialized.FindProperty(propertyName).boolValue = value;
    }

    private static void Set(SerializedObject serialized, string propertyName, int value)
    {
        serialized.FindProperty(propertyName).intValue = value;
    }

    private static void Set(SerializedObject serialized, string propertyName, float value)
    {
        serialized.FindProperty(propertyName).floatValue = value;
    }
}
