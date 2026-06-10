#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GroundMissileLauncherAuthoringTests
{
    private const string GroundLauncherPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Missle_Launcher_Ground.prefab";
    private const string GroundLauncherConfigPath = "Assets/Game/Configs/Weapons/GroundMissileLauncher_Ground_Config.asset";

    [Test]
    public void GroundLauncherConfigHasRequiredVfxReferences()
    {
        GroundMissileLauncherConfig config = AssetDatabase.LoadAssetAtPath<GroundMissileLauncherConfig>(GroundLauncherConfigPath);
        Assert.NotNull(config, $"Missing ground missile launcher config at {GroundLauncherConfigPath}.");
        Assert.Greater(config.MinRange, 0f);
        Assert.Greater(config.MaxRange, config.MinRange);
        Assert.Greater(config.Damage, 0);
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

    private static T GetReference<T>(SerializedObject serialized, string propertyName) where T : Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        Assert.NotNull(property, $"Missing serialized property {propertyName}.");
        return property.objectReferenceValue as T;
    }
}
#endif
