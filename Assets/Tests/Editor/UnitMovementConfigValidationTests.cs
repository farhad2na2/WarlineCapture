using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using Game.Configs;

public sealed class UnitMovementConfigValidationTests
{
    private const string ConfigRoot = "Assets/Game/Configs/Prefabs/";

    private static readonly Dictionary<string, ExpectedMovement> ExpectedConfigs = new()
    {
        { "Prefab_UnitGrid_Chr_Bombsuit_Male_01_Config.asset", new ExpectedMovement(2.8f, 1.1f, 1.1f) },
        { "Prefab_UnitGrid_Chr_Civilian_Female_01_Config.asset", new ExpectedMovement(3.2f, 1.35f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Civilian_Female_02_Config.asset", new ExpectedMovement(3.2f, 1.35f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Civilian_Male_01_Config.asset", new ExpectedMovement(3.2f, 1.35f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Civilian_Male_02_Config.asset", new ExpectedMovement(3.2f, 1.35f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Contractor_Female_01_Config.asset", new ExpectedMovement(4.2f, 1.55f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Contractor_Male_01_Config.asset", new ExpectedMovement(4.2f, 1.55f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Contractor_Male_02_Config.asset", new ExpectedMovement(4.2f, 1.55f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Ghillie_Male_01_Config.asset", new ExpectedMovement(4f, 1.45f, 1.12f) },
        { "Prefab_UnitGrid_Chr_Insurgent_Female_01_Config.asset", new ExpectedMovement(4.4f, 1.65f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Insurgent_Female_02_Config.asset", new ExpectedMovement(4.4f, 1.65f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Insurgent_Male_01_Config.asset", new ExpectedMovement(4.4f, 1.65f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Insurgent_Male_02_Config.asset", new ExpectedMovement(4.4f, 1.65f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Insurgent_Male_03_Config.asset", new ExpectedMovement(4.4f, 1.65f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Insurgent_Male_04_Config.asset", new ExpectedMovement(4.4f, 1.65f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Insurgent_Male_05_Config.asset", new ExpectedMovement(4.4f, 1.65f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Leader_Male_01_Config.asset", new ExpectedMovement(4.6f, 1.6f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Pilot_Female_01_Config.asset", new ExpectedMovement(3.6f, 1.45f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Pilot_Male_01_Config.asset", new ExpectedMovement(3.6f, 1.45f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Female_01_Alt_01_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Female_01_Alt_02_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Female_01_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_01_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_02_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Female_02_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Male_01_Alt_01_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Male_01_Alt_02_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Male_01_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_01_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_02_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_03_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Chr_Soldier_Male_02_Config.asset", new ExpectedMovement(4.8f, 1.7f, 1.15f) },
        { "Prefab_UnitGrid_Veh_APC_Fast_Config.asset", new ExpectedMovement(11f, 11f, 1.35f, soldierCapacity: 10) },
        { "Prefab_UnitGrid_Veh_APC_Heavy_Config.asset", new ExpectedMovement(8f, 8f, 1.35f, soldierCapacity: 10) },
        { "Prefab_UnitGrid_Veh_APC_Slow_Config.asset", new ExpectedMovement(7f, 7f, 1.35f, soldierCapacity: 10) },
        { "Prefab_UnitGrid_Veh_Drone_Config.asset", new ExpectedMovement(28f, 28f, 1f, 12f, true) },
        { "Prefab_UnitGrid_Veh_Helicopter_Attack_Config.asset", new ExpectedMovement(22f, 22f, 1f, 5f, true) },
        { "Prefab_UnitGrid_Veh_Helicopter_Attack_Small_Config.asset", new ExpectedMovement(24f, 24f, 1f, 5f, true) },
        { "Prefab_UnitGrid_Veh_Helicopter_Transport_Config.asset", new ExpectedMovement(20f, 20f, 1f, 5f, true, 10) },
        { "Prefab_UnitGrid_Veh_Jet_01_Config.asset", new ExpectedMovement(36f, 36f, 1f, 15f, true) },
        { "Prefab_UnitGrid_Veh_Jet_02_Config.asset", new ExpectedMovement(36f, 36f, 1f, 15f, true) },
        { "Prefab_UnitGrid_Veh_Light_Armored_Car_Config.asset", new ExpectedMovement(13f, 13f, 1.35f) },
        { "Prefab_UnitGrid_Veh_Missle_Launcher_Air_Config.asset", new ExpectedMovement(7.5f, 7.5f, 1.3f) },
        { "Prefab_UnitGrid_Veh_Missle_Launcher_Ground_Config.asset", new ExpectedMovement(7.5f, 7.5f, 1.3f) },
        { "Prefab_UnitGrid_Veh_Plane_Transport_Config.asset", new ExpectedMovement(30f, 30f, 1f, 12f, true) },
        { "Prefab_UnitGrid_Veh_Radar_Tank.asset", new ExpectedMovement(7f, 7f, 1.3f) },
        { "Prefab_UnitGrid_Veh_Tank_USA_Config.asset", new ExpectedMovement(8.5f, 8.5f, 1.3f) },
        { "Prefab_UnitGrid_Veh_Truck_Canopy.asset", new ExpectedMovement(8f, 8f, 1.35f, soldierCapacity: 10) },
        { "Prefab_UnitGrid_Veh_Truck_Tanker.asset", new ExpectedMovement(8f, 8f, 1.35f) },
        { "Prefab_UnitGrid_Veh_Truck_Tray.asset", new ExpectedMovement(8f, 8f, 1.35f) },
    };

    [Test]
    public void AllUnitMovementConfigsUseExpectedMovementSpeeds()
    {
        foreach (KeyValuePair<string, ExpectedMovement> pair in ExpectedConfigs)
        {
            string path = ConfigRoot + pair.Key;
            ExpectedMovement expected = pair.Value;
            UnitGridAuthoringConfig config = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(path);
            Assert.NotNull(config, path);

            Assert.That(config.Speed, Is.EqualTo(expected.Speed).Within(0.001f), path);
            Assert.That(config.WalkSpeed, Is.EqualTo(expected.WalkSpeed).Within(0.001f), path);
            Assert.That(config.RoadSpeedMultiplier, Is.EqualTo(expected.RoadSpeedMultiplier).Within(0.001f), path);
            Assert.That(config.WalkSpeed, Is.Positive, path);
            Assert.That(config.WalkSpeed, Is.LessThanOrEqualTo(config.Speed), path);
            Assert.That(config.RunwayTaxiSpeed, Is.EqualTo(expected.RunwayTaxiSpeed).Within(0.001f), path);
            Assert.That(config.IsAirUnit, Is.EqualTo(expected.IsAirUnit), path);
            Assert.That(config.SoldierTransportCapacity, Is.EqualTo(expected.SoldierCapacity), path);
        }
    }

    [Test]
    public void AllCharacterAndVehicleMovementConfigsAreCoveredByValidation()
    {
        string[] guids = AssetDatabase.FindAssets("t:UnitGridAuthoringPrefabConfigAsset", new[] { ConfigRoot.TrimEnd('/') });
        HashSet<string> discovered = new();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileName(path);
            if (fileName.StartsWith("Prefab_UnitGrid_Chr_") || fileName.StartsWith("Prefab_UnitGrid_Veh_"))
                discovered.Add(fileName);
        }

        CollectionAssert.AreEquivalent(ExpectedConfigs.Keys, discovered);
    }

    [Test]
    public void AllUnitDisplayTextFitsRuntimeFixedStrings()
    {
        string[] guids = AssetDatabase.FindAssets("t:UnitGridAuthoringPrefabConfigAsset", new[] { ConfigRoot.TrimEnd('/') });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnitGridAuthoringConfig config = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(path);
            Assert.NotNull(config, path);

            Assert.DoesNotThrow(() => _ = new FixedString64Bytes(config.DisplayName), path);
            Assert.DoesNotThrow(() => _ = new FixedString128Bytes(config.Description ?? string.Empty), path);
        }
    }

    private readonly struct ExpectedMovement
    {
        public ExpectedMovement(float speed, float walkSpeed, float roadSpeedMultiplier, float runwayTaxiSpeed = 5f, bool isAirUnit = false, int soldierCapacity = 0)
        {
            Speed = speed;
            WalkSpeed = walkSpeed;
            RoadSpeedMultiplier = roadSpeedMultiplier;
            RunwayTaxiSpeed = runwayTaxiSpeed;
            IsAirUnit = isAirUnit;
            SoldierCapacity = soldierCapacity;
        }

        public float Speed { get; }
        public float WalkSpeed { get; }
        public float RoadSpeedMultiplier { get; }
        public float RunwayTaxiSpeed { get; }
        public bool IsAirUnit { get; }
        public int SoldierCapacity { get; }
    }
}
