#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AirMissileLauncherAuthoringTests
{
    private const string AirLauncherPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Missle_Launcher_Air.prefab";
    private const string AirLauncherConfigPath = "Assets/Game/Configs/Weapons/AirMissileLauncher_Air_Config.asset";
    private const string AirLauncherUnitConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Missle_Launcher_Air_Config.asset";

    [Test]
    public void AirLauncherConfigHasRequiredRuntimeValuesAndVfxReferences()
    {
        AirMissileLauncherConfig config = AssetDatabase.LoadAssetAtPath<AirMissileLauncherConfig>(AirLauncherConfigPath);
        Assert.NotNull(config, $"Missing air missile launcher config at {AirLauncherConfigPath}.");

        Assert.Greater(config.MinRange, 0f);
        Assert.Greater(config.BaseDetectionRange, config.MinRange);
        Assert.Greater(config.MaxDetectionRange, config.BaseDetectionRange);
        Assert.Greater(config.TurretYawSpeedDegreesPerSecond, 0f);
        Assert.Greater(config.LockSeconds, 0f);
        Assert.Greater(config.ReloadSeconds, 0f);
        Assert.Greater(config.MissileSpeed, 0f);
        Assert.Greater(config.MissileTurnRateDegreesPerSecond, 0f);
        Assert.Greater(config.MissileLifetimeSeconds, 0f);
        Assert.Greater(config.ProximityFuseRadius, 0f);
        Assert.Greater(config.AirTargetDamage, 0);
        Assert.Greater(config.IncomingMissileDamage, config.AirTargetDamage);
        Assert.NotNull(config.LaunchFlashPrefab);
        Assert.NotNull(config.LaunchSmokePrefab);
        Assert.NotNull(config.MissileTrailPrefab);
        Assert.NotNull(config.AirburstExplosionPrefab);
        Assert.NotNull(config.AirTargetImpactPrefab);
        Assert.NotNull(config.InterceptExplosionPrefab);
    }

    [Test]
    public void AirLauncherUnitConfigAssignsAirMissileConfig()
    {
        UnitGridAuthoringConfig unitConfig = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(AirLauncherUnitConfigPath);
        AirMissileLauncherConfig airConfig = AssetDatabase.LoadAssetAtPath<AirMissileLauncherConfig>(AirLauncherConfigPath);

        Assert.NotNull(unitConfig, $"Missing air launcher unit config at {AirLauncherUnitConfigPath}.");
        Assert.NotNull(airConfig, $"Missing air missile launcher config at {AirLauncherConfigPath}.");
        Assert.AreEqual(airConfig, unitConfig.AirMissileLauncherConfig);
        Assert.IsTrue(unitConfig.CanAttack);
        Assert.IsTrue(unitConfig.AllowAutoEngage);
    }

    [Test]
    public void AirLauncherPrefabSerializesTurretAndMissileVisualReferences()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AirLauncherPrefabPath);
        Assert.NotNull(prefab, $"Missing air missile launcher prefab at {AirLauncherPrefabPath}.");

        UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
        Assert.NotNull(authoring);
        SerializedObject serialized = new(authoring);

        Transform turret = GetReference<Transform>(serialized, "airMissileLauncherTurret");
        SerializedProperty missiles = serialized.FindProperty("airMissileLauncherMissiles");

        Assert.NotNull(authoring.AirMissileLauncherConfig);
        Assert.NotNull(turret);
        Assert.AreEqual("Missle_Launcher_Air", turret.name);
        Assert.NotNull(missiles);
        Assert.AreEqual(12, missiles.arraySize);
        Assert.AreEqual("SM_Prop_Missle_Launcher_02_Missle_1", missiles.GetArrayElementAtIndex(0).objectReferenceValue.name);
        Assert.AreEqual("SM_Prop_Missle_Launcher_02_Missle_12", missiles.GetArrayElementAtIndex(11).objectReferenceValue.name);
    }

    [Test]
    public void AirLauncherBakerProjectsThreatDetectorsToAirDefenseSupportProviders()
    {
        string source = System.IO.File.ReadAllText("Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs");

        StringAssert.Contains("AddAirDefenseSupportProvider(entity, authoring.threatDetectionKind, authoring.threatDetectionRadiusCells)", source);
        StringAssert.Contains("AirDefenseSupportProviderKind.Satellite", source);
        StringAssert.Contains("AirDefenseSupportProviderKind.Radar", source);
    }

    private static T GetReference<T>(SerializedObject serialized, string propertyName) where T : Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        Assert.NotNull(property, $"Missing serialized property {propertyName}.");
        return property.objectReferenceValue as T;
    }
}
#endif
