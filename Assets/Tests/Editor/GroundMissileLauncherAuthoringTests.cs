#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GroundMissileLauncherAuthoringTests
{
    private const string GroundLauncherPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Missle_Launcher_Ground.prefab";
    private const string GroundLauncherConfigPath = "Assets/Game/Configs/Weapons/GroundMissileLauncher_Ground_Config.asset";
    private const string GroundLauncherUnitConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Missle_Launcher_Ground_Config.asset";

    [Test]
    public void GroundLauncherConfigHasRequiredVfxReferences()
    {
        GroundMissileLauncherConfig config = AssetDatabase.LoadAssetAtPath<GroundMissileLauncherConfig>(GroundLauncherConfigPath);
        Assert.NotNull(config, $"Missing ground missile launcher config at {GroundLauncherConfigPath}.");
        Assert.Greater(config.MinRange, 0f);
        Assert.Greater(config.MaxRange, config.MinRange);
        Assert.GreaterOrEqual(config.Damage, 600);
        Assert.Greater(config.DamageRadius, 0f);
        Assert.NotNull(config.LauncherBackfirePrefab);
        Assert.NotNull(config.RocketTrailPrefab);
        Assert.NotNull(config.ImpactExplosionPrefab);
        Assert.NotNull(config.ImpactSmokePrefab);
    }

    [Test]
    public void GroundLauncherPrefabSerializesMissileVisualReferences()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GroundLauncherPrefabPath);
        Assert.NotNull(prefab, $"Missing ground missile launcher prefab at {GroundLauncherPrefabPath}.");

        UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
        Assert.NotNull(authoring);
        SerializedObject serialized = new(authoring);

        Transform battery = GetReference<Transform>(serialized, "groundMissileLauncherBattery");
        Transform smokeSpawn = GetReference<Transform>(serialized, "groundMissileLauncherSmokeSpawn");
        SerializedProperty rockets = serialized.FindProperty("groundMissileLauncherRockets");

        Assert.NotNull(authoring.GroundMissileLauncherConfig);
        Assert.NotNull(battery);
        Assert.NotNull(smokeSpawn);
        Assert.AreEqual("SM_Veh_Rocket_Truck_01_Rocket_Battery", battery.name);
        Assert.NotNull(rockets);
        Assert.AreEqual(12, rockets.arraySize);
        Assert.AreEqual("SM_Veh_Rocket_Truck_01_Rocket_1", rockets.GetArrayElementAtIndex(0).objectReferenceValue.name);
    }

    [Test]
    public void GroundLauncherUnitAttackRangeOverridesMissileConfigMaxRange()
    {
        UnitGridAuthoringConfig unitConfig = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(GroundLauncherUnitConfigPath);
        GroundMissileLauncherConfig missileConfig = AssetDatabase.LoadAssetAtPath<GroundMissileLauncherConfig>(GroundLauncherConfigPath);

        Assert.NotNull(unitConfig, $"Missing ground launcher unit config at {GroundLauncherUnitConfigPath}.");
        Assert.NotNull(missileConfig, $"Missing ground missile launcher config at {GroundLauncherConfigPath}.");
        Assert.Greater(unitConfig.AttackRange, missileConfig.MaxRange);
        Assert.GreaterOrEqual(unitConfig.AttackRange, 5000f);
    }

    [Test]
    public void GroundLauncherBakerUsesUnitAttackRangeAsMissileMaxRangeFloor()
    {
        string source = File.ReadAllText("Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs");

        StringAssert.Contains(
            "MaxRange = math.max(missileConfig.MaxRange, authoring.ConfiguredAttackRange)",
            source);
    }

    private static T GetReference<T>(SerializedObject serialized, string propertyName) where T : Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        Assert.NotNull(property, $"Missing serialized property {propertyName}.");
        return property.objectReferenceValue as T;
    }
}
#endif
