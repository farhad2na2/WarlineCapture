using Game.Scripts.UI;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class ThreatWarningValidationTests
{
    private const string ScenePath = "Assets/Game/Scenes/Game2D.unity";
    private const string RadarTankConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Radar_Tank.asset";
    private const string SatelliteDishConfigPath = "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Satelite_Dish_Config.asset";
    private const string GameStringsConfigPath = "Assets/Game/Configs/Scene/Game_GameStrings_Config.asset";

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new ThreatWarningValidationTests();
            tests.ThreatWarningConfigs_AssignDetectorRolesDescriptionsAndStrings();
            tests.GameScene_TacticalWarningPanelIsWiredOnMenuView();
            tests.ThreatDetectionWarningSystem_GroundRadarWarnsOnlyWhenNewGroundThreatEntersRadius();
            tests.ThreatDetectionWarningSystem_GroundRadarIgnoresEnemySoldiersMovingTowardSensor();
            tests.ThreatDetectionWarningSystem_IgnoresVehiclesMovingAwayFromSensor();
            tests.ThreatDetectionWarningSystem_SatelliteWarnsOnlyForAirThreats();
            Debug.Log("[ThreatWarningValidation] result=Passed");
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[ThreatWarningValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void ThreatWarningConfigs_AssignDetectorRolesDescriptionsAndStrings()
    {
        UnitGridAuthoringConfig radarConfig = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(RadarTankConfigPath);
        BuildingDefinitionAuthoringConfig satelliteConfig = AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(SatelliteDishConfigPath);
        GameStringsConfig stringsConfig = AssetDatabase.LoadAssetAtPath<GameStringsConfig>(GameStringsConfigPath);

        Assert.NotNull(radarConfig);
        Assert.NotNull(satelliteConfig);
        Assert.NotNull(stringsConfig);

        Assert.AreEqual(ThreatDetectionKind.Ground, radarConfig.ThreatDetectionKind);
        Assert.AreEqual(240, radarConfig.ThreatDetectionRadiusCells);
        Assert.IsFalse(radarConfig.CanAttack);
        Assert.That(radarConfig.Description, Does.Contain("ground"));
        Assert.That(radarConfig.Description, Does.Contain("cannot attack"));
        Assert.DoesNotThrow(() => _ = new FixedString128Bytes(radarConfig.Description ?? string.Empty));

        Assert.AreEqual(ThreatDetectionKind.Air, satelliteConfig.ThreatDetectionKind);
        Assert.AreEqual(240, satelliteConfig.ThreatDetectionRadiusCells);
        Assert.That(satelliteConfig.Description, Does.Contain("Air"));
        Assert.That(satelliteConfig.Description, Does.Contain("cannot attack"));
        Assert.DoesNotThrow(() => _ = new FixedString128Bytes(satelliteConfig.Description ?? string.Empty));

        GameStrings.Init(stringsConfig);
        Assert.AreEqual("Ground vehicle attack detected", GameStrings.Get("warning_ground_attack_type"));
        Assert.AreEqual("Air attack detected", GameStrings.Get("warning_air_attack_type"));
        Assert.AreEqual("Estimated time to base: 12 seconds", GameStrings.Format("warning_attack_eta_seconds", 12));
    }

    [Test]
    public void GameScene_TacticalWarningPanelIsWiredOnMenuView()
    {
        SceneYamlTestUtility scene = SceneYamlTestUtility.Load(ScenePath);
        string menuViewBlock = scene.FindRequiredBlockContaining("m_EditorClassIdentifier: Assembly-CSharp::Game.Scripts.UI.MenuView");

        string panelGameId = scene.GetRequiredFieldFileId(menuViewBlock, "panelGame");
        string warningLabelId = scene.GetRequiredFieldFileId(menuViewBlock, "warningLabel");
        string tacticalWarningPanelId = scene.GetRequiredFieldFileId(menuViewBlock, "tacticalWarningPanel");
        string tacticalWarningTypeLabelId = scene.GetRequiredFieldFileId(menuViewBlock, "tacticalWarningTypeLabel");
        string tacticalWarningDescriptionLabelId = scene.GetRequiredFieldFileId(menuViewBlock, "tacticalWarningDescriptionLabel");

        Assert.AreEqual("Panel_Warning", scene.GetRequiredGameObjectNameForReference(tacticalWarningPanelId));
        Assert.AreEqual("Label_Match_Type", scene.GetRequiredGameObjectNameForReference(tacticalWarningTypeLabelId));
        Assert.AreEqual("Label_Match_Description", scene.GetRequiredGameObjectNameForReference(tacticalWarningDescriptionLabelId));
        Assert.IsFalse(scene.GetRequiredActiveStateForReference(tacticalWarningPanelId));
        Assert.AreEqual(
            scene.GetRectTransformFileIdForReference(panelGameId),
            scene.GetRectTransformParentFileIdForReference(tacticalWarningPanelId));
        Assert.AreNotEqual(warningLabelId, tacticalWarningTypeLabelId);
        Assert.AreNotEqual(warningLabelId, tacticalWarningDescriptionLabelId);
    }

    [Test]
    public void ThreatDetectionWarningSystem_GroundRadarWarnsOnlyWhenNewGroundThreatEntersRadius()
    {
        using var world = new World("ThreatDetectionWarningSystem_Ground");
        EntityManager em = world.EntityManager;
        Entity sensor = CreateUnit(em, 0, new int2(20, 20), false, 100, 0f);
        em.AddComponentData(sensor, new ThreatDetector
        {
            Kind = (byte)ThreatDetectionKind.Ground,
            RadiusCells = 20
        });
        CreateUnit(em, 1, new int2(30, 20), false, 100, 5f, true, new int2(20, 20));
        CreateUnit(em, 1, new int2(24, 20), true, 100, 15f, false, new int2(20, 20));

        SystemHandle system = world.CreateSystem<ThreatDetectionWarningSystem>();
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            ThreatWarningRuntimeState.Reset();

            system.Update(world.Unmanaged);
            Assert.IsTrue(ThreatWarningRuntimeState.HasPendingWarning);
            Assert.AreEqual(ThreatWarningType.Ground, ThreatWarningRuntimeState.PendingType);
            Assert.That(ThreatWarningRuntimeState.PendingEtaSeconds, Is.EqualTo(2f).Within(0.01f));

            ThreatWarningRuntimeState.ClearPendingWarning();
            system.Update(world.Unmanaged);
            Assert.IsFalse(ThreatWarningRuntimeState.HasPendingWarning);
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            ThreatWarningRuntimeState.Reset();
        }
    }

    [Test]
    public void ThreatDetectionWarningSystem_GroundRadarIgnoresEnemySoldiersMovingTowardSensor()
    {
        using var world = new World("ThreatDetectionWarningSystem_GroundIgnoresSoldiers");
        EntityManager em = world.EntityManager;
        Entity sensor = CreateUnit(em, 0, new int2(20, 20), false, 100, 0f);
        em.AddComponentData(sensor, new ThreatDetector
        {
            Kind = (byte)ThreatDetectionKind.Ground,
            RadiusCells = 20
        });
        CreateUnit(em, 1, new int2(30, 20), false, 100, 5f, false, new int2(20, 20));

        SystemHandle system = world.CreateSystem<ThreatDetectionWarningSystem>();
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            ThreatWarningRuntimeState.Reset();

            system.Update(world.Unmanaged);
            Assert.IsFalse(ThreatWarningRuntimeState.HasPendingWarning);
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            ThreatWarningRuntimeState.Reset();
        }
    }

    [Test]
    public void ThreatDetectionWarningSystem_IgnoresVehiclesMovingAwayFromSensor()
    {
        using var world = new World("ThreatDetectionWarningSystem_IgnoresMovingAway");
        EntityManager em = world.EntityManager;
        Entity groundSensor = CreateUnit(em, 0, new int2(20, 20), false, 100, 0f);
        em.AddComponentData(groundSensor, new ThreatDetector
        {
            Kind = (byte)ThreatDetectionKind.Ground,
            RadiusCells = 20
        });
        Entity airSensor = CreateUnit(em, 0, new int2(50, 50), false, 100, 0f);
        em.AddComponentData(airSensor, new ThreatDetector
        {
            Kind = (byte)ThreatDetectionKind.Air,
            RadiusCells = 30
        });

        CreateUnit(em, 1, new int2(30, 20), false, 100, 5f, true, new int2(45, 20));
        CreateUnit(em, 1, new int2(65, 50), true, 100, 15f, false, new int2(90, 50));

        SystemHandle system = world.CreateSystem<ThreatDetectionWarningSystem>();
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            ThreatWarningRuntimeState.Reset();

            system.Update(world.Unmanaged);
            Assert.IsFalse(ThreatWarningRuntimeState.HasPendingWarning);
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            ThreatWarningRuntimeState.Reset();
        }
    }

    [Test]
    public void ThreatDetectionWarningSystem_SatelliteWarnsOnlyForAirThreats()
    {
        using var world = new World("ThreatDetectionWarningSystem_Air");
        EntityManager em = world.EntityManager;
        Entity sensor = CreateUnit(em, 0, new int2(50, 50), false, 100, 0f);
        em.AddComponentData(sensor, new ThreatDetector
        {
            Kind = (byte)ThreatDetectionKind.Air,
            RadiusCells = 30
        });
        CreateUnit(em, 1, new int2(55, 50), false, 100, 5f, true, new int2(50, 50));
        CreateUnit(em, 1, new int2(65, 50), true, 100, 15f, false, new int2(50, 50));

        SystemHandle system = world.CreateSystem<ThreatDetectionWarningSystem>();
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            ThreatWarningRuntimeState.Reset();

            system.Update(world.Unmanaged);
            Assert.IsTrue(ThreatWarningRuntimeState.HasPendingWarning);
            Assert.AreEqual(ThreatWarningType.Air, ThreatWarningRuntimeState.PendingType);
            Assert.That(ThreatWarningRuntimeState.PendingEtaSeconds, Is.EqualTo(1f).Within(0.01f));
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            ThreatWarningRuntimeState.Reset();
        }
    }

    private static Entity CreateUnit(
        EntityManager em,
        byte factionId,
        int2 cell,
        bool air,
        int health,
        float speed,
        bool groundVehicle = false,
        int2? targetCell = null)
    {
        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new Faction { Id = factionId });
        em.AddComponentData(entity, new UnitGrid { Cell = cell });
        em.AddComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.AddComponentData(entity, new UnitMovementBehavior
        {
            AllowIdleWander = 0,
            UsesVehicleMotion = (byte)(groundVehicle ? 1 : 0)
        });
        if (speed > 0f)
        {
            em.AddComponentData(entity, new UnitMove
            {
                Speed = speed,
                WalkSpeed = speed,
                RoadSpeedMultiplier = 1f,
                ArriveDistance = 0.05f
            });
        }
        if (targetCell.HasValue)
            em.AddComponentData(entity, new UnitTarget { Cell = targetCell.Value });
        if (air)
        {
            em.AddComponentData(entity, new UnitAirMovement
            {
                CruiseHeight = 6f,
                RunwayTaxiSpeed = 5f
            });
        }

        return entity;
    }
}
