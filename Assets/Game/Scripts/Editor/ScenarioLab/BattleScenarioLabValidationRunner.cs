#if UNITY_EDITOR
using System;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Unity.Transforms;
using UnityEngine.UI;

public static class BattleScenarioLabValidationRunner
{
    public const string Ad001ReportPath = "/private/tmp/warline-scenario-lab-ad001-air-defense.json";
    public const string Ad001DefinitionPath = "Assets/Game/Configs/ScenarioLab/AD001_AirMissileLauncher_InterceptIncomingGroundMissile_RadarComparison.asset";
    public const string Ad002DefinitionPath = "Assets/Game/Configs/ScenarioLab/AD002_AirMissileLauncher_InterceptEnemyJet_RadarComparison.asset";
    public const string Ad003DefinitionPath = "Assets/Game/Configs/ScenarioLab/AD003_AirMissileLauncher_TrackAndInterceptDroneScout_RadarComparison.asset";
    public const string Ad004DefinitionPath = "Assets/Game/Configs/ScenarioLab/AD004_AirMissileLauncher_InterceptTwoIncomingGroundMissiles_RadarComparison.asset";
    public const string Ad005DefinitionPath = "Assets/Game/Configs/ScenarioLab/AD005_TwoAirMissileLaunchers_InterceptTwoIncomingGroundMissiles_RadarComparison.asset";
    public const string Ad006DefinitionPath = "Assets/Game/Configs/ScenarioLab/AD006_AirMissileLauncher_RadarDisabledMidScenario_RadarComparison.asset";
    public const string Ad007DefinitionPath = "Assets/Game/Configs/ScenarioLab/AD007_AirMissileLauncher_ThreatStartsInsideRadarExtendedRange_RadarComparison.asset";
    public const string Ad008DefinitionPath = "Assets/Game/Configs/ScenarioLab/AD008_AirMissileLauncher_SaturatedMixedDroneAndGroundMissile_RadarComparison.asset";
    public const string Ad009DefinitionPath = "Assets/Game/Configs/ScenarioLab/AD009_AirMissileLauncher_SupportModeComparison_RadarSatelliteCombined.asset";
    public const string Ad010DefinitionPath = "Assets/Game/Configs/ScenarioLab/AD010_AirMissileLauncher_InterceptionGeometrySweep.asset";
    public const string Ad011DefinitionPath = "Assets/Game/Configs/ScenarioLab/AD011_AirMissileLauncher_TracksAndHitsAirTargetClasses.asset";
    public const string Ad011HelicopterPostImpactCapturePath = "/private/tmp/warline-scenario-lab-ad011-helicopter-post-impact.png";
    public const string Gm001DefinitionPath = "Assets/Game/Configs/ScenarioLab/GM001_GroundMissileLauncher_FiresVisibleRocketAndDamagesTarget.asset";
    public const string Dr001DefinitionPath = "Assets/Game/Configs/ScenarioLab/DR001_DroneReconDetectionAndThreatWarning.asset";
    private const string LiveEcsPlaybackActiveKey = "BattleScenarioLab.LiveEcsPlayback.Active";
    private const string LiveEcsPlaybackStartedAtKey = "BattleScenarioLab.LiveEcsPlayback.StartedAt";
    private const string LiveEcsPlaybackSeenRegistryKey = "BattleScenarioLab.LiveEcsPlayback.SeenRegistry";
    private const string LiveEcsPlaybackSeenAirLauncherKey = "BattleScenarioLab.LiveEcsPlayback.SeenAirLauncher";
    private const string LiveEcsPlaybackSeenGroundLauncherKey = "BattleScenarioLab.LiveEcsPlayback.SeenGroundLauncher";
    private const string LiveEcsPlaybackSeenProjectileKey = "BattleScenarioLab.LiveEcsPlayback.SeenProjectile";
    private const string LiveEcsPlaybackSeenGroundProjectileKey = "BattleScenarioLab.LiveEcsPlayback.SeenGroundProjectile";
    private const string LiveEcsPlaybackSeenAirProjectileKey = "BattleScenarioLab.LiveEcsPlayback.SeenAirProjectile";
    private const string LiveEcsPlaybackSeenGroundRocketVisualKey = "BattleScenarioLab.LiveEcsPlayback.SeenGroundRocketVisual";
    private const string LiveEcsPlaybackSeenInterceptEventKey = "BattleScenarioLab.LiveEcsPlayback.SeenInterceptEvent";
    private const string LiveEcsPlaybackGroundRocketClearedAfterInterceptKey = "BattleScenarioLab.LiveEcsPlayback.GroundRocketClearedAfterIntercept";
    private const string LiveEcsPlaybackClosestMissileDistanceKey = "BattleScenarioLab.LiveEcsPlayback.ClosestMissileDistance";
    private const string LiveEcsPlaybackClosestGroundVisualDistanceKey = "BattleScenarioLab.LiveEcsPlayback.ClosestGroundVisualDistance";
    private const string LiveEcsPlaybackClosestVisualInterceptDistanceKey = "BattleScenarioLab.LiveEcsPlayback.ClosestVisualInterceptDistance";
    private const string LiveEcsPlaybackMaxGroundMissileAltitudeKey = "BattleScenarioLab.LiveEcsPlayback.MaxGroundMissileAltitude";
    private const string LiveEcsPlaybackFailureKey = "BattleScenarioLab.LiveEcsPlayback.Failure";
    private const string LiveEcsPlaybackPendingExitKey = "BattleScenarioLab.LiveEcsPlayback.PendingExit";
    private const string LiveEcsPlaybackPendingPassedKey = "BattleScenarioLab.LiveEcsPlayback.PendingPassed";
    private const string LiveEcsPlaybackPendingMessageKey = "BattleScenarioLab.LiveEcsPlayback.PendingMessage";
    private const string LiveEcsPlaybackValidateAllVariantsKey = "BattleScenarioLab.LiveEcsPlayback.ValidateAllVariants";
    private const string LiveEcsPlaybackVariantDropdownValueKey = "BattleScenarioLab.LiveEcsPlayback.VariantDropdownValue";
    private const string LiveEcsPlaybackVariantRunStartedKey = "BattleScenarioLab.LiveEcsPlayback.VariantRunStarted";
    private const string LiveEcsPlaybackPassedVariantsKey = "BattleScenarioLab.LiveEcsPlayback.PassedVariants";
    private const string VisualSwitchActiveKey = "BattleScenarioLab.VisualSwitch.Active";
    private const string VisualSwitchStartedAtKey = "BattleScenarioLab.VisualSwitch.StartedAt";
    private const string VisualSwitchPhaseKey = "BattleScenarioLab.VisualSwitch.Phase";
    private const string VisualSwitchPhaseStartedAtKey = "BattleScenarioLab.VisualSwitch.PhaseStartedAt";
    private const string VisualSwitchFailureKey = "BattleScenarioLab.VisualSwitch.Failure";
    private const string VisualSwitchSeenAd001GroundProjectileKey = "BattleScenarioLab.VisualSwitch.SeenAd001GroundProjectile";
    private const string VisualSwitchSeenAd002TargetKey = "BattleScenarioLab.VisualSwitch.SeenAd002Target";
    private const string VisualSwitchSeenAd002AirProjectileKey = "BattleScenarioLab.VisualSwitch.SeenAd002AirProjectile";
    private const string VisualSwitchSeenAd002ImpactKey = "BattleScenarioLab.VisualSwitch.SeenAd002Impact";
    private const string VisualSwitchSeenAd011TargetKey = "BattleScenarioLab.VisualSwitch.SeenAd011Target";
    private const string VisualSwitchSeenAd011AirProjectileKey = "BattleScenarioLab.VisualSwitch.SeenAd011AirProjectile";
    private const string VisualSwitchSeenAd011ImpactKey = "BattleScenarioLab.VisualSwitch.SeenAd011Impact";
    private const string VisualSwitchSeenAd011HelicopterTargetKey = "BattleScenarioLab.VisualSwitch.SeenAd011HelicopterTarget";
    private const string VisualSwitchSeenAd011HelicopterAirProjectileKey = "BattleScenarioLab.VisualSwitch.SeenAd011HelicopterAirProjectile";
    private const string VisualSwitchSeenAd011HelicopterImpactKey = "BattleScenarioLab.VisualSwitch.SeenAd011HelicopterImpact";
    private const string VisualSwitchSeenAd011DroneTargetKey = "BattleScenarioLab.VisualSwitch.SeenAd011DroneTarget";
    private const string VisualSwitchSeenAd011DroneAirProjectileKey = "BattleScenarioLab.VisualSwitch.SeenAd011DroneAirProjectile";
    private const string VisualSwitchSeenAd011DroneImpactKey = "BattleScenarioLab.VisualSwitch.SeenAd011DroneImpact";
    private const string VisualSwitchSeenAd011AttackingJetTargetKey = "BattleScenarioLab.VisualSwitch.SeenAd011AttackingJetTarget";
    private const string VisualSwitchSeenAd011AttackingJetAirProjectileKey = "BattleScenarioLab.VisualSwitch.SeenAd011AttackingJetAirProjectile";
    private const string VisualSwitchSeenAd011AttackingJetImpactKey = "BattleScenarioLab.VisualSwitch.SeenAd011AttackingJetImpact";
    private const double LiveEcsPlaybackTimeoutSeconds = 40.0;
    private const double VisualSwitchTimeoutSeconds = 75.0;
    private const float LiveEcsPlaybackRequiredClosestMissileDistance = 2.5f;
    private const float LiveEcsPlaybackRequiredClosestGroundVisualDistance = 1.5f;
    private const float LiveEcsPlaybackRequiredClosestVisualInterceptDistance = 0.75f;
    private const float LiveEcsPlaybackMaxAllowedGroundMissileAltitude = 24f;
    private const string GroundLauncherSourceKey = "Unit_Veh_Missle_Launcher_Ground";
    private const string AirLauncherSourceKey = "Unit_Veh_Missle_Launcher_Air";
    private const string JetTargetSourceKey = "Unit_Veh_Jet_01";
    private const string HelicopterTargetSourceKey = "Unit_Veh_Helicopter_Attack";
    private const string DroneTargetSourceKey = "Unit_Veh_Drone";

    static BattleScenarioLabValidationRunner()
    {
        if (SessionState.GetBool(LiveEcsPlaybackActiveKey, false))
            HookLiveEcsPlaybackValidation();
        if (SessionState.GetBool(VisualSwitchActiveKey, false))
            HookVisualSwitchValidation();
        if (SessionState.GetBool(LiveEcsPlaybackPendingExitKey, false))
            HookLiveEcsPlaybackPendingExit();
    }

    [MenuItem("Warline Capture/Scenario Lab/Run AD-001 Air Defense")]
    public static void RunAirDefenseAd001()
    {
        try
        {
            BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Ad001DefinitionPath);
            BattleScenarioResult result = definition != null
                ? BattleScenarioAd001Runner.RunDefinition(definition)
                : BattleScenarioAd001Runner.RunDefault();
            string json = BattleScenarioReportJson.ToJson(result);
            File.WriteAllText(Ad001ReportPath, json);

            if (!result.Passed)
            {
                Debug.LogError($"[BattleScenarioLab] AD-001 failed. Report: {Ad001ReportPath}");
                Exit(1);
                return;
            }

            Debug.Log($"[BattleScenarioLab] AD-001 passed. Report: {Ad001ReportPath}");
            Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleScenarioLab] AD-001 exception: {ex}");
            Exit(1);
        }
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update AD-001 Definition")]
    public static void CreateOrUpdateAd001DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ad001DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Ad001DefinitionPath);
        if (definition == null && File.Exists(Ad001DefinitionPath))
        {
            AssetDatabase.DeleteAsset(Ad001DefinitionPath);
        }

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Ad001DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioAd001Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "AD-001 Air Missile Launcher Intercepts Incoming Ground Missile";
        serialized.FindProperty("description").stringValue =
            "Compares an isolated friendly air missile launcher against an incoming enemy ground missile with no support and with nearby radar support.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 12f;
        serialized.FindProperty("randomSeed").intValue = 12345;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseSideView;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(280f, 90f, 180f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioAd001Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] AD-001 definition saved: {Ad001DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update AD-002 Definition")]
    public static void CreateOrUpdateAd002DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ad002DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Ad002DefinitionPath);
        if (definition == null && File.Exists(Ad002DefinitionPath))
            AssetDatabase.DeleteAsset(Ad002DefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Ad002DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioAd002Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "AD-002 Air Missile Launcher Intercepts Enemy Jet";
        serialized.FindProperty("description").stringValue =
            "Compares an isolated friendly air missile launcher against an enemy jet entering air-defense range with no support and with nearby radar support.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 12f;
        serialized.FindProperty("randomSeed").intValue = 22345;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseSideView;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(280f, 90f, 180f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioAd002Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] AD-002 definition saved: {Ad002DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update AD-003 Definition")]
    public static void CreateOrUpdateAd003DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ad003DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Ad003DefinitionPath);
        if (definition == null && File.Exists(Ad003DefinitionPath))
            AssetDatabase.DeleteAsset(Ad003DefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Ad003DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioAd003Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "AD-003 Air Missile Launcher Tracks And Intercepts Drone Scout";
        serialized.FindProperty("description").stringValue =
            "Compares an isolated friendly air missile launcher against a drone scout entering air-defense range with no support and with nearby radar support.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 14f;
        serialized.FindProperty("randomSeed").intValue = 32345;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseSideView;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(300f, 90f, 180f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioAd003Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] AD-003 definition saved: {Ad003DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update AD-004 Definition")]
    public static void CreateOrUpdateAd004DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ad004DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Ad004DefinitionPath);
        if (definition == null && File.Exists(Ad004DefinitionPath))
            AssetDatabase.DeleteAsset(Ad004DefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Ad004DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioAd004Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "AD-004 Air Missile Launcher Intercepts Two Incoming Ground Missiles";
        serialized.FindProperty("description").stringValue =
            "Compares one isolated friendly air missile launcher against two staggered incoming enemy ground missiles with no support and with nearby radar support.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 16f;
        serialized.FindProperty("randomSeed").intValue = 42345;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseSideView;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(330f, 90f, 200f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioAd004Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] AD-004 definition saved: {Ad004DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update AD-005 Definition")]
    public static void CreateOrUpdateAd005DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ad005DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Ad005DefinitionPath);
        if (definition == null && File.Exists(Ad005DefinitionPath))
            AssetDatabase.DeleteAsset(Ad005DefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Ad005DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioAd005Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "AD-005 Two Air Missile Launchers Intercept Two Incoming Ground Missiles";
        serialized.FindProperty("description").stringValue =
            "Compares two isolated friendly air missile launchers against two staggered incoming enemy ground missiles with no support and with nearby radar support.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 16f;
        serialized.FindProperty("randomSeed").intValue = 52345;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseSideView;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(340f, 90f, 220f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioAd005Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] AD-005 definition saved: {Ad005DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update AD-006 Definition")]
    public static void CreateOrUpdateAd006DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ad006DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Ad006DefinitionPath);
        if (definition == null && File.Exists(Ad006DefinitionPath))
            AssetDatabase.DeleteAsset(Ad006DefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Ad006DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioAd006Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "AD-006 Air Missile Launcher Radar Disabled Mid-Scenario";
        serialized.FindProperty("description").stringValue =
            "Compares no support, persistent nearby radar, and nearby radar disabled mid-scenario for an isolated friendly air missile launcher against an incoming enemy ground missile.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 16f;
        serialized.FindProperty("randomSeed").intValue = 62345;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseSideView;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(300f, 90f, 180f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioAd006Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] AD-006 definition saved: {Ad006DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update AD-007 Definition")]
    public static void CreateOrUpdateAd007DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ad007DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Ad007DefinitionPath);
        if (definition == null && File.Exists(Ad007DefinitionPath))
            AssetDatabase.DeleteAsset(Ad007DefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Ad007DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioAd007Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "AD-007 Threat Starts Inside Radar Extended Range";
        serialized.FindProperty("description").stringValue =
            "Compares no support and nearby radar support when an incoming ground missile starts outside base launcher detection range but inside radar-extended range.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 16f;
        serialized.FindProperty("randomSeed").intValue = 72345;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseSideView;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(360f, 100f, 180f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioAd007Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] AD-007 definition saved: {Ad007DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update AD-008 Definition")]
    public static void CreateOrUpdateAd008DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ad008DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Ad008DefinitionPath);
        if (definition == null && File.Exists(Ad008DefinitionPath))
            AssetDatabase.DeleteAsset(Ad008DefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Ad008DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioAd008Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "AD-008 Saturated Mixed Drone And Ground Missile Attack";
        serialized.FindProperty("description").stringValue =
            "Compares no support and nearby radar support for one isolated friendly air missile launcher defending against a simultaneous enemy drone scout and incoming ground missile.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 18f;
        serialized.FindProperty("randomSeed").intValue = 82345;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseSideView;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(320f, 110f, 220f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioAd008Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] AD-008 definition saved: {Ad008DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update AD-009 Definition")]
    public static void CreateOrUpdateAd009DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ad009DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Ad009DefinitionPath);
        if (definition == null && File.Exists(Ad009DefinitionPath))
            AssetDatabase.DeleteAsset(Ad009DefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Ad009DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioAd009Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "AD-009 Support Mode Comparison";
        serialized.FindProperty("description").stringValue =
            "Compares no support, radar, satellite, and combined radar plus satellite support for an isolated friendly air missile launcher against an incoming ground missile.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 16f;
        serialized.FindProperty("randomSeed").intValue = 92345;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseSideView;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(360f, 100f, 180f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioAd009Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] AD-009 definition saved: {Ad009DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update AD-010 Definition")]
    public static void CreateOrUpdateAd010DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ad010DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Ad010DefinitionPath);
        if (definition == null && File.Exists(Ad010DefinitionPath))
            AssetDatabase.DeleteAsset(Ad010DefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Ad010DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioAd010Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "AD-010 Interception Geometry Sweep";
        serialized.FindProperty("description").stringValue =
            "Runs head-on, side-shot, tail-chase, and crossing-shot interception geometry against an isolated friendly air missile launcher with nearby radar support.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 18f;
        serialized.FindProperty("randomSeed").intValue = 102345;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseTopDown;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(460f, 120f, 380f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioAd010Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] AD-010 definition saved: {Ad010DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update AD-011 Definition")]
    public static void CreateOrUpdateAd011DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ad011DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Ad011DefinitionPath);
        if (definition == null && File.Exists(Ad011DefinitionPath))
            AssetDatabase.DeleteAsset(Ad011DefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Ad011DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioAd011Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "AD-011 Air Missile Launcher Hits Air Target Classes";
        serialized.FindProperty("description").stringValue =
            "Runs jet, helicopter, drone, and attacking-jet air targets against an isolated friendly air missile launcher with nearby radar support; every variant must be detected, tracked, launched on, and killed.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 14f;
        serialized.FindProperty("randomSeed").intValue = 112345;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseSideView;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(360f, 120f, 240f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioAd011Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] AD-011 definition saved: {Ad011DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update GM-001 Definition")]
    public static void CreateOrUpdateGm001DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Gm001DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Gm001DefinitionPath);
        if (definition == null && File.Exists(Gm001DefinitionPath))
            AssetDatabase.DeleteAsset(Gm001DefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Gm001DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioGm001Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "GM-001 Ground Missile Launcher Fires Visible Rocket And Damages Target";
        serialized.FindProperty("description").stringValue =
            "Runs an isolated enemy ground missile launcher through its real fire, flying rocket visual, projectile flight, and impact damage systems against a friendly ground target.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 8f;
        serialized.FindProperty("randomSeed").intValue = 201001;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseSideView;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(180f, 80f, 80f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioGm001Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] GM-001 definition saved: {Gm001DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Create or Update DR-001 Definition")]
    public static void CreateOrUpdateDr001DefinitionAsset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Dr001DefinitionPath) ?? "Assets/Game/Configs/ScenarioLab");

        BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(Dr001DefinitionPath);
        if (definition == null && File.Exists(Dr001DefinitionPath))
            AssetDatabase.DeleteAsset(Dr001DefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, Dr001DefinitionPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = BattleScenarioDr001Runner.ScenarioId;
        serialized.FindProperty("displayName").stringValue = "DR-001 Drone Recon Detection And Threat Warning";
        serialized.FindProperty("description").stringValue =
            "Runs the real threat detection warning system with a player air-threat detector and an enemy drone moving into detector radius.";
        serialized.FindProperty("fixedDeltaTime").floatValue = 0.05f;
        serialized.FindProperty("maxDurationSeconds").floatValue = 2f;
        serialized.FindProperty("randomSeed").intValue = 301001;
        serialized.FindProperty("cameraPreset").enumValueIndex = (int)BattleScenarioCameraPreset.AirDefenseTopDown;
        serialized.FindProperty("worldBounds").boundsValue = new Bounds(Vector3.zero, new Vector3(120f, 60f, 120f));
        serialized.FindProperty("spawnEntries").arraySize = 0;
        WriteVariants(serialized.FindProperty("scenarioVariants"), BattleScenarioDr001Runner.CreateDefaultVariants());
        WriteSuccessCriteria(serialized.FindProperty("successCriteria"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] DR-001 definition saved: {Dr001DefinitionPath}");
    }

    [MenuItem("Warline Capture/Scenario Lab/Validate Manual Scene Smoke")]
    public static void ValidateManualSceneSmoke()
    {
        try
        {
            Scene scene = EditorSceneManager.OpenScene(BattleScenarioLabSceneBuilder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException($"Scene could not be loaded: {BattleScenarioLabSceneBuilder.ScenePath}");

            GameObject root = RequireObject("BattleScenarioLabRoot");
            BattleScenarioLabSceneReferences references = RequireComponent<BattleScenarioLabSceneReferences>(root);
            BattleScenarioLabPlayBootstrap bootstrap = RequireComponent<BattleScenarioLabPlayBootstrap>(root);
            BattleScenarioLabVisualPlayback visualPlayback = RequireComponent<BattleScenarioLabVisualPlayback>(root);
            BattleScenarioLabOverlayView overlay = RequireSceneComponent<BattleScenarioLabOverlayView>();

            RequireReference(references.ScenarioDefinition, "scene scenario definition");
            RequireReference(references.ScenarioCamera, "scene camera");
            RequireReference(references.LauncherMarker, "launcher marker");
            RequireReference(references.RadarMarker, "radar marker");
            RequireReference(references.IncomingThreatStartMarker, "incoming threat marker");
            RequireReference(references.DefendedTargetMarker, "defended target marker");
            RequireObject("NeutralGroundPlane");
            RequireObject("AD001ScenarioMarkers");
            RequireObject("GroundMissileLauncherSpawnMarker");
            RequireObject("AirMissileLauncherSpawnMarker");
            RequireObject("RadarSupportSpawnMarker");
            RequireObject("DefendedTargetVisual");
            RequireScenarioLabSubSceneReference();
            RequireScenarioLabPrefabRegistryConfig();
            RequireObject("ScenarioLabOverlay");
            RequireObject("MetricsPanel");
            GameObject eventSystemObject = RequireObject("EventSystem");
            RequireComponent<EventSystem>(eventSystemObject);
            RequireComponent<BaseInputModule>(eventSystemObject);
            Dropdown scenarioDropdown = RequireComponent<Dropdown>(RequireObject("ScenarioSelector"));
            if (scenarioDropdown.options.Count < 4)
                throw new InvalidOperationException("ScenarioSelector has too few scenario options.");
            RequireDropdownTemplate(scenarioDropdown, "ScenarioSelector");
            Dropdown variantDropdown = RequireComponent<Dropdown>(RequireObject("VariantSelector"));
            if (variantDropdown.options.Count < 2)
                throw new InvalidOperationException("VariantSelector has no scenario variant options.");
            RequireDropdownTemplate(variantDropdown, "VariantSelector");
            Button previousButton = RequireComponent<Button>(RequireObject("PreviousScenarioButton"));
            if (previousButton.onClick.GetPersistentEventCount() == 0)
                throw new InvalidOperationException("PreviousScenarioButton has no persistent click listener.");
            Button nextButton = RequireComponent<Button>(RequireObject("NextScenarioButton"));
            if (nextButton.onClick.GetPersistentEventCount() == 0)
                throw new InvalidOperationException("NextScenarioButton has no persistent click listener.");
            Button restartButton = RequireComponent<Button>(RequireObject("RestartScenarioButton"));
            if (restartButton.onClick.GetPersistentEventCount() == 0)
                throw new InvalidOperationException("RestartScenarioButton has no persistent click listener.");

            SerializedObject bootstrapSerialized = new(bootstrap);
            RequireReference(
                bootstrapSerialized.FindProperty("scenarioDefinition").objectReferenceValue,
                "bootstrap scenario definition");
            SerializedProperty scenarioDefinitions = bootstrapSerialized.FindProperty("scenarioDefinitions");
            if (scenarioDefinitions.arraySize < 13)
                throw new InvalidOperationException("Bootstrap scenario definition list has too few entries.");
            bool hasAd011 = false;
            for (int i = 0; i < scenarioDefinitions.arraySize; i++)
            {
                if (scenarioDefinitions.GetArrayElementAtIndex(i).objectReferenceValue is BattleScenarioDefinition scenario &&
                    string.Equals(scenario.ScenarioId, BattleScenarioAd011Runner.ScenarioId, StringComparison.Ordinal))
                {
                    hasAd011 = true;
                    break;
                }
            }

            if (!hasAd011)
                throw new InvalidOperationException("Bootstrap scenario definition list is missing AD-011.");
            RequireReference(
                bootstrapSerialized.FindProperty("overlayView").objectReferenceValue,
                "bootstrap overlay view");
            RequireReference(
                bootstrapSerialized.FindProperty("visualPlayback").objectReferenceValue,
                "bootstrap visual playback");
            RequireReference(
                bootstrapSerialized.FindProperty("scenarioDropdown").objectReferenceValue,
                "bootstrap scenario dropdown");
            RequireReference(
                bootstrapSerialized.FindProperty("variantDropdown").objectReferenceValue,
                "bootstrap variant dropdown");

            SerializedObject visualSerialized = new(visualPlayback);
            RequireReference(visualSerialized.FindProperty("scenarioCamera").objectReferenceValue, "visual playback camera");
            RequireReference(visualSerialized.FindProperty("groundLauncherRoot").objectReferenceValue, "visual playback ground launcher");
            RequireReference(visualSerialized.FindProperty("airLauncherRoot").objectReferenceValue, "visual playback air launcher");
            RequireReference(visualSerialized.FindProperty("radarRoot").objectReferenceValue, "visual playback radar");
            RequireReference(visualSerialized.FindProperty("defendedTargetVisual").objectReferenceValue, "visual playback defended target");

            SerializedObject overlaySerialized = new(overlay);
            RequireReference(overlaySerialized.FindProperty("titleText").objectReferenceValue, "overlay title text");
            RequireReference(overlaySerialized.FindProperty("statusText").objectReferenceValue, "overlay status text");
            RequireReference(overlaySerialized.FindProperty("variantsText").objectReferenceValue, "overlay variants text");
            RequireReference(overlaySerialized.FindProperty("comparisonsText").objectReferenceValue, "overlay comparisons text");

            bootstrap.SelectNextScenario();
            if (scenarioDropdown.value != 1)
                throw new InvalidOperationException("NextScenarioButton did not advance from AD-001 to the next scenario.");
            Text titleText = RequireComponent<Text>(RequireObject("Title"));
            if (!titleText.text.Contains(BattleScenarioAd002Runner.ScenarioId, StringComparison.Ordinal))
                throw new InvalidOperationException("NextScenarioButton did not run AD-002 after advancing from AD-001.");
            Text variantsText = RequireComponent<Text>(RequireObject("Variants"));
            if (!variantsText.text.Contains("AD-002-A-NoSupport-Jet", StringComparison.Ordinal))
                throw new InvalidOperationException("NextScenarioButton did not show AD-002 variant metrics.");

            for (int i = 0; i < 9; i++)
                bootstrap.SelectNextScenario();
            if (scenarioDropdown.value != 10)
                throw new InvalidOperationException("Repeated NextScenarioButton did not advance to AD-011.");
            if (!titleText.text.Contains(BattleScenarioAd011Runner.ScenarioId, StringComparison.Ordinal))
                throw new InvalidOperationException("Repeated NextScenarioButton did not run AD-011.");

            BattleScenarioResult result = BattleScenarioAd001Runner.RunDefinition(references.ScenarioDefinition);
            if (!result.Passed)
                throw new InvalidOperationException($"AD-001 did not pass during manual scene smoke validation: {result.FailureReason}");

            Debug.Log($"[BattleScenarioLab] Manual scene smoke validation passed: {BattleScenarioLabSceneBuilder.ScenePath}");
            Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleScenarioLab] Manual scene smoke validation failed: {ex}");
            Exit(1);
        }
    }

    [MenuItem("Warline Capture/Scenario Lab/Validate Manual Scene Live ECS Playback")]
    public static void ValidateManualSceneLiveEcsPlayback()
    {
        try
        {
            EditorSceneManager.OpenScene(BattleScenarioLabSceneBuilder.ScenePath, OpenSceneMode.Single);
            SessionState.SetBool(LiveEcsPlaybackActiveKey, true);
            SessionState.SetBool(LiveEcsPlaybackValidateAllVariantsKey, true);
            SessionState.SetInt(LiveEcsPlaybackVariantDropdownValueKey, 1);
            SessionState.SetInt(LiveEcsPlaybackPassedVariantsKey, 0);
            SessionState.SetBool(LiveEcsPlaybackVariantRunStartedKey, false);
            SessionState.SetFloat(LiveEcsPlaybackStartedAtKey, (float)EditorApplication.timeSinceStartup);
            SessionState.EraseString(LiveEcsPlaybackFailureKey);
            ResetLiveEcsPlaybackObservationState();
            HookLiveEcsPlaybackValidation();
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleScenarioLab] Live ECS playback validation failed before PlayMode: {ex}");
            Exit(1);
        }
    }

    [MenuItem("Warline Capture/Scenario Lab/Validate Manual Scene Next Switches Visual Playback")]
    public static void ValidateManualSceneNextSwitchesVisualPlayback()
    {
        try
        {
            EditorSceneManager.OpenScene(BattleScenarioLabSceneBuilder.ScenePath, OpenSceneMode.Single);
            SessionState.SetBool(VisualSwitchActiveKey, true);
            SessionState.SetFloat(VisualSwitchStartedAtKey, (float)EditorApplication.timeSinceStartup);
            SessionState.SetFloat(VisualSwitchPhaseStartedAtKey, (float)EditorApplication.timeSinceStartup);
            SessionState.SetInt(VisualSwitchPhaseKey, 0);
            SessionState.EraseString(VisualSwitchFailureKey);
            ResetVisualSwitchObservationState();
            HookVisualSwitchValidation();
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleScenarioLab] Next visual switch validation failed before PlayMode: {ex}");
            Exit(1);
        }
    }

    private static void HookLiveEcsPlaybackValidation()
    {
        EditorApplication.update -= OnLiveEcsPlaybackValidationUpdate;
        EditorApplication.update += OnLiveEcsPlaybackValidationUpdate;
        Application.logMessageReceived -= OnLiveEcsPlaybackValidationLog;
        Application.logMessageReceived += OnLiveEcsPlaybackValidationLog;
    }

    private static void HookVisualSwitchValidation()
    {
        EditorApplication.update -= OnVisualSwitchValidationUpdate;
        EditorApplication.update += OnVisualSwitchValidationUpdate;
        Application.logMessageReceived -= OnVisualSwitchValidationLog;
        Application.logMessageReceived += OnVisualSwitchValidationLog;
    }

    private static void OnLiveEcsPlaybackValidationLog(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(LiveEcsPlaybackActiveKey, false))
            return;

        if (type == LogType.Error &&
            condition.Contains("Live ECS visual run could not resolve baked production launcher prefab entities", StringComparison.Ordinal))
        {
            SessionState.SetString(LiveEcsPlaybackFailureKey, condition);
        }
    }

    private static void OnVisualSwitchValidationLog(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(VisualSwitchActiveKey, false))
            return;

        if (type == LogType.Error &&
            (condition.Contains("Live ECS visual run could not resolve baked production launcher prefab entities", StringComparison.Ordinal) ||
             condition.Contains("Live ECS visual run could not resolve baked production air launcher/air target prefab entities", StringComparison.Ordinal)))
        {
            SessionState.SetString(VisualSwitchFailureKey, condition);
        }
    }

    private static void OnLiveEcsPlaybackValidationUpdate()
    {
        if (!SessionState.GetBool(LiveEcsPlaybackActiveKey, false))
            return;

        string failure = SessionState.GetString(LiveEcsPlaybackFailureKey, string.Empty);
        if (!string.IsNullOrEmpty(failure))
        {
            CompleteLiveEcsPlaybackValidation(false, failure);
            return;
        }

        if (!EditorApplication.isPlaying)
            return;

        if (!SessionState.GetBool(LiveEcsPlaybackVariantRunStartedKey, false))
        {
            if (!TryStartLiveEcsPlaybackVariant())
                return;
        }

        World world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            EntityManager em = world.EntityManager;
            if (HasAnyEntityWith<UnitPrefabRegistryTag>(em))
                SessionState.SetBool(LiveEcsPlaybackSeenRegistryKey, true);
            if (HasInstantiatedUnit(em, AirLauncherSourceKey))
                SessionState.SetBool(LiveEcsPlaybackSeenAirLauncherKey, true);
            if (HasInstantiatedUnit(em, GroundLauncherSourceKey))
                SessionState.SetBool(LiveEcsPlaybackSeenGroundLauncherKey, true);
            if (HasAnyEntityWith<GroundMissileProjectileComponent>(em) ||
                HasAnyEntityWith<AirMissileProjectileComponent>(em))
            {
                SessionState.SetBool(LiveEcsPlaybackSeenProjectileKey, true);
            }
            TrackInterceptEventMetrics(em);
            TrackLiveProjectileMetrics(em);
        }

        float closestMissileDistance = SessionState.GetFloat(LiveEcsPlaybackClosestMissileDistanceKey, float.PositiveInfinity);
        float closestGroundVisualDistance = SessionState.GetFloat(LiveEcsPlaybackClosestGroundVisualDistanceKey, float.PositiveInfinity);
        float closestVisualInterceptDistance = SessionState.GetFloat(LiveEcsPlaybackClosestVisualInterceptDistanceKey, float.PositiveInfinity);
        float maxGroundAltitude = SessionState.GetFloat(LiveEcsPlaybackMaxGroundMissileAltitudeKey, 0f);
        bool passed = SessionState.GetBool(LiveEcsPlaybackSeenRegistryKey, false) &&
            SessionState.GetBool(LiveEcsPlaybackSeenAirLauncherKey, false) &&
            SessionState.GetBool(LiveEcsPlaybackSeenGroundLauncherKey, false) &&
            SessionState.GetBool(LiveEcsPlaybackSeenGroundProjectileKey, false) &&
            SessionState.GetBool(LiveEcsPlaybackSeenAirProjectileKey, false) &&
            SessionState.GetBool(LiveEcsPlaybackSeenGroundRocketVisualKey, false) &&
            SessionState.GetBool(LiveEcsPlaybackSeenInterceptEventKey, false) &&
            SessionState.GetBool(LiveEcsPlaybackGroundRocketClearedAfterInterceptKey, false) &&
            closestMissileDistance <= LiveEcsPlaybackRequiredClosestMissileDistance &&
            closestGroundVisualDistance <= LiveEcsPlaybackRequiredClosestGroundVisualDistance &&
            closestVisualInterceptDistance <= LiveEcsPlaybackRequiredClosestVisualInterceptDistance &&
            maxGroundAltitude <= LiveEcsPlaybackMaxAllowedGroundMissileAltitude;
        if (passed)
        {
            int currentVariantValue = SessionState.GetInt(LiveEcsPlaybackVariantDropdownValueKey, 1);
            int passedVariants = SessionState.GetInt(LiveEcsPlaybackPassedVariantsKey, 0) + 1;
            SessionState.SetInt(LiveEcsPlaybackPassedVariantsKey, passedVariants);
            int variantCount = BattleScenarioAd001Runner.CreateDefaultVariants().Length;
            if (SessionState.GetBool(LiveEcsPlaybackValidateAllVariantsKey, false) &&
                currentVariantValue < variantCount)
            {
                Debug.Log(
                    $"[BattleScenarioLab] Live ECS playback variant {ResolveAd001VariantLabel(currentVariantValue)} passed: " +
                    $"closest={closestMissileDistance:0.00}m, groundVisual={closestGroundVisualDistance:0.00}m, " +
                    $"visualIntercept={closestVisualInterceptDistance:0.00}m, maxGroundAltitude={maxGroundAltitude:0.00}m");
                SessionState.SetInt(LiveEcsPlaybackVariantDropdownValueKey, currentVariantValue + 1);
                SessionState.SetBool(LiveEcsPlaybackVariantRunStartedKey, false);
                ResetLiveEcsPlaybackObservationState();
                return;
            }

            CompleteLiveEcsPlaybackValidation(
                true,
                $"production registry, instantiated launcher entities, near-contact missile intercept, synced visible ground rocket, visual missile contact, and ground rocket clear-after-intercept observed across {passedVariants} AD-001 visual variants; latest={ResolveAd001VariantLabel(currentVariantValue)}, closest={closestMissileDistance:0.00}m, groundVisual={closestGroundVisualDistance:0.00}m, visualIntercept={closestVisualInterceptDistance:0.00}m, maxGroundAltitude={maxGroundAltitude:0.00}m");
            return;
        }

        double elapsed = EditorApplication.timeSinceStartup - SessionState.GetFloat(LiveEcsPlaybackStartedAtKey, 0f);
        if (elapsed >= LiveEcsPlaybackTimeoutSeconds)
        {
            string reason =
                $"timed out after {LiveEcsPlaybackTimeoutSeconds:0.#}s; " +
                $"registry={SessionState.GetBool(LiveEcsPlaybackSeenRegistryKey, false)}, " +
                $"airLauncher={SessionState.GetBool(LiveEcsPlaybackSeenAirLauncherKey, false)}, " +
                $"groundLauncher={SessionState.GetBool(LiveEcsPlaybackSeenGroundLauncherKey, false)}, " +
                $"groundProjectile={SessionState.GetBool(LiveEcsPlaybackSeenGroundProjectileKey, false)}, " +
                $"airProjectile={SessionState.GetBool(LiveEcsPlaybackSeenAirProjectileKey, false)}, " +
                $"groundRocketVisual={SessionState.GetBool(LiveEcsPlaybackSeenGroundRocketVisualKey, false)}, " +
                $"interceptEvent={SessionState.GetBool(LiveEcsPlaybackSeenInterceptEventKey, false)}, " +
                $"groundRocketCleared={SessionState.GetBool(LiveEcsPlaybackGroundRocketClearedAfterInterceptKey, false)}, " +
                $"variant={ResolveAd001VariantLabel(SessionState.GetInt(LiveEcsPlaybackVariantDropdownValueKey, 1))}, " +
                $"closest={closestMissileDistance:0.00}m, " +
                $"groundVisual={closestGroundVisualDistance:0.00}m, " +
                $"visualIntercept={closestVisualInterceptDistance:0.00}m, " +
                $"maxGroundAltitude={maxGroundAltitude:0.00}m";
            CompleteLiveEcsPlaybackValidation(false, reason);
        }
    }

    private static void OnVisualSwitchValidationUpdate()
    {
        if (!SessionState.GetBool(VisualSwitchActiveKey, false))
            return;

        string failure = SessionState.GetString(VisualSwitchFailureKey, string.Empty);
        if (!string.IsNullOrEmpty(failure))
        {
            CompleteVisualSwitchValidation(false, failure);
            return;
        }

        if (!EditorApplication.isPlaying)
            return;

        BattleScenarioLabPlayBootstrap bootstrap = UnityEngine.Object.FindAnyObjectByType<BattleScenarioLabPlayBootstrap>();
        Dropdown scenarioDropdown = GameObject.Find("ScenarioSelector")?.GetComponent<Dropdown>();
        Dropdown variantDropdown = GameObject.Find("VariantSelector")?.GetComponent<Dropdown>();
        Text titleText = GameObject.Find("Title")?.GetComponent<Text>();
        if (bootstrap == null || scenarioDropdown == null || variantDropdown == null || titleText == null)
            return;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        int phase = SessionState.GetInt(VisualSwitchPhaseKey, 0);
        switch (phase)
        {
            case 0:
                if (HasAnyEntityWith<GroundMissileProjectileComponent>(em))
                    SessionState.SetBool(VisualSwitchSeenAd001GroundProjectileKey, true);

                if (SessionState.GetBool(VisualSwitchSeenAd001GroundProjectileKey, false))
                {
                    bootstrap.SelectNextScenario();
                    SessionState.SetInt(VisualSwitchPhaseKey, 1);
                    SessionState.SetFloat(VisualSwitchPhaseStartedAtKey, (float)EditorApplication.timeSinceStartup);
                    Debug.Log("[BattleScenarioLab] Next visual switch validation advanced from AD-001 to AD-002.");
                }
                break;

            case 1:
                if (scenarioDropdown.value != 1 ||
                    !titleText.text.Contains(BattleScenarioAd002Runner.ScenarioId, StringComparison.Ordinal))
                {
                    break;
                }

                TrackAirTargetSwitchState(em, JetTargetSourceKey, VisualSwitchSeenAd002TargetKey, VisualSwitchSeenAd002AirProjectileKey, VisualSwitchSeenAd002ImpactKey);
                if (HasAnyEntityWith<GroundMissileProjectileComponent>(em))
                {
                    break;
                }

                if (SessionState.GetBool(VisualSwitchSeenAd002TargetKey, false) &&
                    SessionState.GetBool(VisualSwitchSeenAd002AirProjectileKey, false) &&
                    SessionState.GetBool(VisualSwitchSeenAd002ImpactKey, false))
                {
                    for (int i = 0; i < 9; i++)
                        bootstrap.SelectNextScenario();

                    SessionState.SetInt(VisualSwitchPhaseKey, 2);
                    SessionState.SetFloat(VisualSwitchPhaseStartedAtKey, (float)EditorApplication.timeSinceStartup);
                    Debug.Log("[BattleScenarioLab] Next visual switch validation advanced from AD-002 to AD-011.");
                }
                break;

            case 2:
                if (scenarioDropdown.value != 10 ||
                    !titleText.text.Contains(BattleScenarioAd011Runner.ScenarioId, StringComparison.Ordinal))
                {
                    break;
                }

                TrackAirTargetSwitchState(em, JetTargetSourceKey, VisualSwitchSeenAd011TargetKey, VisualSwitchSeenAd011AirProjectileKey, VisualSwitchSeenAd011ImpactKey);
                if (FailOnDuplicateVisibleTargetVisuals(em, JetTargetSourceKey))
                    return;

                if (SessionState.GetBool(VisualSwitchSeenAd011TargetKey, false) &&
                    SessionState.GetBool(VisualSwitchSeenAd011AirProjectileKey, false) &&
                    SessionState.GetBool(VisualSwitchSeenAd011ImpactKey, false))
                {
                    bootstrap.SelectNextScenario();
                    SessionState.SetInt(VisualSwitchPhaseKey, 3);
                    SessionState.SetFloat(VisualSwitchPhaseStartedAtKey, (float)EditorApplication.timeSinceStartup);
                    Debug.Log("[BattleScenarioLab] Next visual switch validation advanced from AD-011 jet to AD-011 helicopter.");
                }
                break;

            case 3:
                if (scenarioDropdown.value != 10 ||
                    variantDropdown.value != 2 ||
                    !titleText.text.Contains(BattleScenarioAd011Runner.ScenarioId, StringComparison.Ordinal))
                {
                    break;
                }

                TrackAirTargetSwitchState(em, HelicopterTargetSourceKey, VisualSwitchSeenAd011HelicopterTargetKey, VisualSwitchSeenAd011HelicopterAirProjectileKey, VisualSwitchSeenAd011HelicopterImpactKey);
                if (FailOnDuplicateVisibleTargetVisuals(em, HelicopterTargetSourceKey))
                    return;

                if (SessionState.GetBool(VisualSwitchSeenAd011HelicopterTargetKey, false) &&
                    SessionState.GetBool(VisualSwitchSeenAd011HelicopterAirProjectileKey, false) &&
                    SessionState.GetBool(VisualSwitchSeenAd011HelicopterImpactKey, false) &&
                    ScenarioTargetAliveVisualHidden(em, HelicopterTargetSourceKey) &&
                    !HasInstantiatedUnit(em, JetTargetSourceKey))
                {
                    FrameScenarioTargetForCloseCapture(em, HelicopterTargetSourceKey);
                    CapturePlayModeCamera(Ad011HelicopterPostImpactCapturePath);
                    bootstrap.SelectNextScenario();
                    SessionState.SetInt(VisualSwitchPhaseKey, 4);
                    SessionState.SetFloat(VisualSwitchPhaseStartedAtKey, (float)EditorApplication.timeSinceStartup);
                    Debug.Log($"[BattleScenarioLab] Next visual switch validation advanced from AD-011 helicopter to AD-011 drone. Capture: {Ad011HelicopterPostImpactCapturePath}");
                }
                break;

            case 4:
                if (scenarioDropdown.value != 10 ||
                    variantDropdown.value != 3 ||
                    !titleText.text.Contains(BattleScenarioAd011Runner.ScenarioId, StringComparison.Ordinal))
                {
                    break;
                }

                TrackAirTargetSwitchState(em, DroneTargetSourceKey, VisualSwitchSeenAd011DroneTargetKey, VisualSwitchSeenAd011DroneAirProjectileKey, VisualSwitchSeenAd011DroneImpactKey);
                if (FailOnDuplicateVisibleTargetVisuals(em, DroneTargetSourceKey))
                    return;

                if (SessionState.GetBool(VisualSwitchSeenAd011DroneTargetKey, false) &&
                    SessionState.GetBool(VisualSwitchSeenAd011DroneAirProjectileKey, false) &&
                    SessionState.GetBool(VisualSwitchSeenAd011DroneImpactKey, false) &&
                    !HasInstantiatedUnit(em, JetTargetSourceKey) &&
                    !HasInstantiatedUnit(em, HelicopterTargetSourceKey))
                {
                    bootstrap.SelectNextScenario();
                    SessionState.SetInt(VisualSwitchPhaseKey, 5);
                    SessionState.SetFloat(VisualSwitchPhaseStartedAtKey, (float)EditorApplication.timeSinceStartup);
                    Debug.Log("[BattleScenarioLab] Next visual switch validation advanced from AD-011 drone to AD-011 attacking jet.");
                }
                break;

            case 5:
                if (scenarioDropdown.value != 10 ||
                    variantDropdown.value != 4 ||
                    !titleText.text.Contains(BattleScenarioAd011Runner.ScenarioId, StringComparison.Ordinal))
                {
                    break;
                }

                TrackAirTargetSwitchState(em, JetTargetSourceKey, VisualSwitchSeenAd011AttackingJetTargetKey, VisualSwitchSeenAd011AttackingJetAirProjectileKey, VisualSwitchSeenAd011AttackingJetImpactKey);
                if (FailOnDuplicateVisibleTargetVisuals(em, JetTargetSourceKey))
                    return;

                if (SessionState.GetBool(VisualSwitchSeenAd011AttackingJetTargetKey, false) &&
                    SessionState.GetBool(VisualSwitchSeenAd011AttackingJetAirProjectileKey, false) &&
                    SessionState.GetBool(VisualSwitchSeenAd011AttackingJetImpactKey, false) &&
                    !HasInstantiatedUnit(em, HelicopterTargetSourceKey) &&
                    !HasInstantiatedUnit(em, DroneTargetSourceKey))
                {
                    CompleteVisualSwitchValidation(
                        true,
                        "runtime Next stopped prior visuals, switched AD-001 to AD-002, then advanced AD-011 through jet, helicopter, drone, and attacking jet variants with old target entities cleaned between runs");
                    return;
                }
                break;
        }

        double elapsed = EditorApplication.timeSinceStartup - SessionState.GetFloat(VisualSwitchStartedAtKey, 0f);
        if (elapsed >= VisualSwitchTimeoutSeconds)
        {
            string reason =
                $"timed out after {VisualSwitchTimeoutSeconds:0.#}s; " +
                $"phase={SessionState.GetInt(VisualSwitchPhaseKey, 0)}, " +
                $"selector={scenarioDropdown.value}, " +
                $"title='{titleText.text}', " +
                $"ad001Ground={SessionState.GetBool(VisualSwitchSeenAd001GroundProjectileKey, false)}, " +
                $"ad002Target={SessionState.GetBool(VisualSwitchSeenAd002TargetKey, false)}, " +
                $"ad002AirProjectile={SessionState.GetBool(VisualSwitchSeenAd002AirProjectileKey, false)}, " +
                $"ad002Impact={SessionState.GetBool(VisualSwitchSeenAd002ImpactKey, false)}, " +
                $"ad011Target={SessionState.GetBool(VisualSwitchSeenAd011TargetKey, false)}, " +
                $"ad011AirProjectile={SessionState.GetBool(VisualSwitchSeenAd011AirProjectileKey, false)}, " +
                $"ad011Impact={SessionState.GetBool(VisualSwitchSeenAd011ImpactKey, false)}, " +
                $"ad011Helicopter={SessionState.GetBool(VisualSwitchSeenAd011HelicopterTargetKey, false)}/{SessionState.GetBool(VisualSwitchSeenAd011HelicopterAirProjectileKey, false)}/{SessionState.GetBool(VisualSwitchSeenAd011HelicopterImpactKey, false)}, " +
                $"ad011Drone={SessionState.GetBool(VisualSwitchSeenAd011DroneTargetKey, false)}/{SessionState.GetBool(VisualSwitchSeenAd011DroneAirProjectileKey, false)}/{SessionState.GetBool(VisualSwitchSeenAd011DroneImpactKey, false)}, " +
                $"ad011AttackingJet={SessionState.GetBool(VisualSwitchSeenAd011AttackingJetTargetKey, false)}/{SessionState.GetBool(VisualSwitchSeenAd011AttackingJetAirProjectileKey, false)}/{SessionState.GetBool(VisualSwitchSeenAd011AttackingJetImpactKey, false)}, " +
                $"visibleHelVisualRoots={CountScenarioTargetVisibleVisualRoots(em, HelicopterTargetSourceKey)}, " +
                $"visibleDroneVisualRoots={CountScenarioTargetVisibleVisualRoots(em, DroneTargetSourceKey)}, " +
                $"helAliveHidden={ScenarioTargetAliveVisualHidden(em, HelicopterTargetSourceKey)}, " +
                $"groundProjectileNow={HasAnyEntityWith<GroundMissileProjectileComponent>(em)}, " +
                $"airProjectileNow={HasAnyEntityWith<AirMissileProjectileComponent>(em)}";
            CompleteVisualSwitchValidation(false, reason);
        }
    }

    private static bool TryStartLiveEcsPlaybackVariant()
    {
        BattleScenarioLabPlayBootstrap bootstrap = UnityEngine.Object.FindAnyObjectByType<BattleScenarioLabPlayBootstrap>();
        GameObject variantObject = GameObject.Find("VariantSelector");
        if (bootstrap == null ||
            variantObject == null ||
            !variantObject.TryGetComponent(out Dropdown variantDropdown))
        {
            return false;
        }

        int variantValue = SessionState.GetInt(LiveEcsPlaybackVariantDropdownValueKey, 1);
        if (variantDropdown.options == null || variantDropdown.options.Count <= variantValue)
            return false;

        ResetLiveEcsPlaybackObservationState();
        SessionState.SetBool(LiveEcsPlaybackVariantRunStartedKey, true);
        SessionState.SetFloat(LiveEcsPlaybackStartedAtKey, (float)EditorApplication.timeSinceStartup);
        variantDropdown.SetValueWithoutNotify(variantValue);
        variantDropdown.RefreshShownValue();
        bootstrap.RunScenario();
        Debug.Log($"[BattleScenarioLab] Live ECS playback validating {ResolveAd001VariantLabel(variantValue)}.");
        return true;
    }

    private static void ResetLiveEcsPlaybackObservationState()
    {
        SessionState.SetBool(LiveEcsPlaybackSeenRegistryKey, false);
        SessionState.SetBool(LiveEcsPlaybackSeenAirLauncherKey, false);
        SessionState.SetBool(LiveEcsPlaybackSeenGroundLauncherKey, false);
        SessionState.SetBool(LiveEcsPlaybackSeenProjectileKey, false);
        SessionState.SetBool(LiveEcsPlaybackSeenGroundProjectileKey, false);
        SessionState.SetBool(LiveEcsPlaybackSeenAirProjectileKey, false);
        SessionState.SetBool(LiveEcsPlaybackSeenGroundRocketVisualKey, false);
        SessionState.SetBool(LiveEcsPlaybackSeenInterceptEventKey, false);
        SessionState.SetBool(LiveEcsPlaybackGroundRocketClearedAfterInterceptKey, false);
        SessionState.SetFloat(LiveEcsPlaybackClosestMissileDistanceKey, float.PositiveInfinity);
        SessionState.SetFloat(LiveEcsPlaybackClosestGroundVisualDistanceKey, float.PositiveInfinity);
        SessionState.SetFloat(LiveEcsPlaybackClosestVisualInterceptDistanceKey, float.PositiveInfinity);
        SessionState.SetFloat(LiveEcsPlaybackMaxGroundMissileAltitudeKey, 0f);
    }

    private static void ResetVisualSwitchObservationState()
    {
        SessionState.SetBool(VisualSwitchSeenAd001GroundProjectileKey, false);
        SessionState.SetBool(VisualSwitchSeenAd002TargetKey, false);
        SessionState.SetBool(VisualSwitchSeenAd002AirProjectileKey, false);
        SessionState.SetBool(VisualSwitchSeenAd002ImpactKey, false);
        SessionState.SetBool(VisualSwitchSeenAd011TargetKey, false);
        SessionState.SetBool(VisualSwitchSeenAd011AirProjectileKey, false);
        SessionState.SetBool(VisualSwitchSeenAd011ImpactKey, false);
        SessionState.SetBool(VisualSwitchSeenAd011HelicopterTargetKey, false);
        SessionState.SetBool(VisualSwitchSeenAd011HelicopterAirProjectileKey, false);
        SessionState.SetBool(VisualSwitchSeenAd011HelicopterImpactKey, false);
        SessionState.SetBool(VisualSwitchSeenAd011DroneTargetKey, false);
        SessionState.SetBool(VisualSwitchSeenAd011DroneAirProjectileKey, false);
        SessionState.SetBool(VisualSwitchSeenAd011DroneImpactKey, false);
        SessionState.SetBool(VisualSwitchSeenAd011AttackingJetTargetKey, false);
        SessionState.SetBool(VisualSwitchSeenAd011AttackingJetAirProjectileKey, false);
        SessionState.SetBool(VisualSwitchSeenAd011AttackingJetImpactKey, false);
    }

    private static void TrackAirTargetSwitchState(
        EntityManager em,
        string targetSourceKey,
        string targetSeenKey,
        string airProjectileSeenKey,
        string impactSeenKey)
    {
        if (HasInstantiatedUnit(em, targetSourceKey))
            SessionState.SetBool(targetSeenKey, true);
        if (HasAnyEntityWith<AirMissileProjectileComponent>(em))
            SessionState.SetBool(airProjectileSeenKey, true);
        if (HasDestroyedInstantiatedUnit(em, targetSourceKey))
            SessionState.SetBool(impactSeenKey, true);
    }

    private static bool HasDestroyedInstantiatedUnit(EntityManager em, string sourceKey)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<UnitHealth>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || em.HasComponent<Prefab>(entity))
                continue;

            string candidate = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.Equals(candidate, sourceKey, StringComparison.OrdinalIgnoreCase))
                continue;

            if (em.GetComponentData<UnitHealth>(entity).Current <= 0)
                return true;
        }

        return false;
    }

    private static bool FailOnDuplicateVisibleTargetVisuals(EntityManager em, string sourceKey)
    {
        int visibleRoots = CountScenarioTargetVisibleVisualRoots(em, sourceKey);
        if (visibleRoots <= 1)
            return false;

        CompleteVisualSwitchValidation(
            false,
            $"{sourceKey} has {visibleRoots} visible visual roots; expected exactly one live or destroyed visual root");
        return true;
    }

    private static int CountScenarioTargetVisibleVisualRoots(EntityManager em, string sourceKey)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSourcePrefabKey>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        int visibleRoots = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || em.HasComponent<Prefab>(entity))
                continue;

            string candidate = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.Equals(candidate, sourceKey, StringComparison.OrdinalIgnoreCase))
                continue;

            if (em.HasComponent<UnitDetailedVisualReference>(entity) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<UnitDetailedVisualReference>(entity).Root, true))
                visibleRoots++;

            if (em.HasComponent<UnitModelInstanceReference>(entity) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<UnitModelInstanceReference>(entity).Instance, true))
                visibleRoots++;

            if (em.HasComponent<UnitMidLodInstanceReference>(entity) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<UnitMidLodInstanceReference>(entity).Instance, true))
                visibleRoots++;

            if (em.HasComponent<UnitLowLodInstanceReference>(entity) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<UnitLowLodInstanceReference>(entity).Instance, true))
                visibleRoots++;

            if (em.HasComponent<VehicleDestroyedVisualInstanceReference>(entity) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<VehicleDestroyedVisualInstanceReference>(entity).Instance, true))
                visibleRoots++;
        }

        return visibleRoots;
    }

    private static bool IsRenderableVisibleRecursive(EntityManager em, Entity entity, bool parentVisible)
    {
        using NativeHashSet<Entity> visited = new(16, Allocator.Temp);
        return IsRenderableVisibleRecursive(em, entity, parentVisible, visited);
    }

    private static bool IsRenderableVisibleRecursive(EntityManager em, Entity entity, bool parentVisible, NativeHashSet<Entity> visited)
    {
        if (entity == Entity.Null || !em.Exists(entity) || !parentVisible)
            return false;
        if (!visited.Add(entity))
            return false;

        bool transformVisible = true;
        if (em.HasComponent<LocalTransform>(entity))
            transformVisible = math.abs(em.GetComponentData<LocalTransform>(entity).Scale) > 0.001f;

        bool entityVisible =
            transformVisible &&
            !em.HasComponent<Disabled>(entity) &&
            !em.HasComponent<DisableRendering>(entity) &&
            !em.HasComponent<UnitRenderBudgetCulledTag>(entity);
        if (entityVisible &&
            (em.HasComponent<RenderFilterSettings>(entity) ||
             em.HasComponent<RenderBounds>(entity) ||
             em.HasComponent<MaterialMeshInfo>(entity)))
        {
            return true;
        }

        if (em.HasBuffer<LinkedEntityGroup>(entity))
        {
            DynamicBuffer<LinkedEntityGroup> linkedEntities = em.GetBuffer<LinkedEntityGroup>(entity);
            for (int i = 0; i < linkedEntities.Length; i++)
            {
                Entity linkedEntity = linkedEntities[i].Value;
                if (linkedEntity != entity && IsRenderableVisibleRecursive(em, linkedEntity, true, visited))
                    return true;
            }
        }

        if (!em.HasBuffer<Child>(entity))
            return false;

        DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
        for (int i = 0; i < children.Length; i++)
        {
            bool childTransformVisible = transformVisible && !em.HasComponent<Disabled>(entity);
            if (IsRenderableVisibleRecursive(em, children[i].Value, childTransformVisible, visited))
                return true;
        }

        return false;
    }

    private static bool ScenarioTargetAliveVisualHidden(EntityManager em, string sourceKey)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<UnitHealth>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || em.HasComponent<Prefab>(entity))
                continue;

            string candidate = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.Equals(candidate, sourceKey, StringComparison.OrdinalIgnoreCase))
                continue;

            if (em.GetComponentData<UnitHealth>(entity).Current > 0)
                continue;

            if (em.HasComponent<UnitDetailedVisualReference>(entity) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<UnitDetailedVisualReference>(entity).Root, true))
                return false;

            if (em.HasComponent<UnitModelInstanceReference>(entity) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<UnitModelInstanceReference>(entity).Instance, true))
                return false;

            if (em.HasComponent<UnitMidLodInstanceReference>(entity) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<UnitMidLodInstanceReference>(entity).Instance, true))
                return false;

            if (em.HasComponent<UnitLowLodInstanceReference>(entity) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<UnitLowLodInstanceReference>(entity).Instance, true))
                return false;

            if (HasVisibleOriginalLinkedVisuals(em, entity))
                return false;

            if (em.HasComponent<UnitDestroyedVisualReference>(entity))
            {
                UnitDestroyedVisualReference visualRef = em.GetComponentData<UnitDestroyedVisualReference>(entity);
                if (IsRenderableVisibleRecursive(em, visualRef.AliveVisual, true) ||
                    IsRenderableVisibleRecursive(em, visualRef.DestroyedVisual, true))
                    return false;
            }

            return true;
        }

        return false;
    }

    private static bool HasVisibleOriginalLinkedVisuals(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity) || !em.HasBuffer<LinkedEntityGroup>(entity))
            return false;

        DynamicBuffer<LinkedEntityGroup> linkedEntities = em.GetBuffer<LinkedEntityGroup>(entity);
        for (int i = 0; i < linkedEntities.Length; i++)
        {
            Entity linkedEntity = linkedEntities[i].Value;
            if (linkedEntity == entity)
                continue;

            if (IsRenderableVisibleRecursive(em, linkedEntity, true))
                return true;
        }

        return false;
    }

    private static void CapturePlayModeCamera(string path)
    {
        Camera camera = ResolveScenarioCamera();
        if (camera == null)
            throw new InvalidOperationException("Cannot capture Scenario Lab visual proof because no camera is available.");

        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        const int width = 1280;
        const int height = 720;
        RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4,
            name = "ScenarioLabAd011HelicopterPostImpactCapture"
        };
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        Texture2D texture = null;
        try
        {
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();

            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply(false, false);
            if (!HasVisiblePixels(texture))
                throw new InvalidOperationException($"Scenario Lab capture is blank: {path}");

            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (texture != null)
                UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static Camera ResolveScenarioCamera()
    {
        BattleScenarioLabSceneReferences references = UnityEngine.Object.FindAnyObjectByType<BattleScenarioLabSceneReferences>();
        if (references != null && references.ScenarioCamera != null)
            return references.ScenarioCamera;

        if (Camera.main != null)
            return Camera.main;

        return UnityEngine.Object.FindAnyObjectByType<Camera>();
    }

    private static bool HasVisiblePixels(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i += 97)
        {
            Color32 pixel = pixels[i];
            if (pixel.r > 24 || pixel.g > 24 || pixel.b > 24)
                return true;
        }

        return false;
    }

    private static void FrameScenarioTargetForCloseCapture(EntityManager em, string sourceKey)
    {
        if (!TryResolveScenarioTargetFocus(em, sourceKey, out Vector3 focus))
            return;

        Camera camera = ResolveScenarioCamera();
        if (camera == null)
            return;

        Vector3 cameraPosition = focus + new Vector3(18f, 11f, -28f);
        Vector3 direction = focus - cameraPosition;
        camera.transform.position = cameraPosition;
        if (direction.sqrMagnitude > 0.0001f)
            camera.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 260f;
        camera.fieldOfView = 38f;
    }

    private static bool TryResolveScenarioTargetFocus(EntityManager em, string sourceKey, out Vector3 focus)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<UnitHealth>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || em.HasComponent<Prefab>(entity))
                continue;

            string candidate = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.Equals(candidate, sourceKey, StringComparison.OrdinalIgnoreCase))
                continue;

            if (em.HasComponent<VehicleDestroyedVisualInstanceReference>(entity))
            {
                Entity destroyedVisual = em.GetComponentData<VehicleDestroyedVisualInstanceReference>(entity).Instance;
                if (TryGetWorldPosition(em, destroyedVisual, out focus))
                    return true;
            }

            if (TryGetWorldPosition(em, entity, out focus))
                return true;
        }

        focus = default;
        return false;
    }

    private static bool TryGetWorldPosition(EntityManager em, Entity entity, out Vector3 position)
    {
        if (entity != Entity.Null && em.Exists(entity))
        {
            if (em.HasComponent<LocalToWorld>(entity))
            {
                position = (Vector3)em.GetComponentData<LocalToWorld>(entity).Position;
                return true;
            }

            if (em.HasComponent<LocalTransform>(entity))
            {
                position = (Vector3)em.GetComponentData<LocalTransform>(entity).Position;
                return true;
            }
        }

        position = default;
        return false;
    }

    private static string ResolveAd001VariantLabel(int dropdownValue)
    {
        BattleScenarioVariant[] variants = BattleScenarioAd001Runner.CreateDefaultVariants();
        int index = math.clamp(dropdownValue - 1, 0, math.max(0, variants.Length - 1));
        if (variants.Length == 0)
            return $"variant dropdown {dropdownValue}";

        BattleScenarioVariant variant = variants[index];
        return string.IsNullOrWhiteSpace(variant.VariantId) ? variant.Label : variant.VariantId;
    }

    private static void TrackLiveProjectileMetrics(EntityManager em)
    {
        bool hasGround = TryFindFirstPosition<GroundMissileProjectileComponent>(em, out float3 groundPosition);
        bool hasAir = TryFindFirstPosition<AirMissileProjectileComponent>(em, out float3 airPosition);
        bool hasGroundVisual = TryFindFirstPosition<GroundMissileFlyingRocketVisualComponent>(em, out float3 groundVisualPosition);

        if (hasGround)
        {
            SessionState.SetBool(LiveEcsPlaybackSeenGroundProjectileKey, true);
            float maxAltitude = SessionState.GetFloat(LiveEcsPlaybackMaxGroundMissileAltitudeKey, 0f);
            SessionState.SetFloat(LiveEcsPlaybackMaxGroundMissileAltitudeKey, math.max(maxAltitude, groundPosition.y));
        }

        if (hasAir)
            SessionState.SetBool(LiveEcsPlaybackSeenAirProjectileKey, true);

        if (hasGroundVisual)
            SessionState.SetBool(LiveEcsPlaybackSeenGroundRocketVisualKey, true);

        if (!hasGround || !hasAir)
        {
            TrackVisualDistances(hasGround, hasAir, hasGroundVisual, groundPosition, airPosition, groundVisualPosition);
            return;
        }

        float distance = math.distance(groundPosition, airPosition);
        float closest = SessionState.GetFloat(LiveEcsPlaybackClosestMissileDistanceKey, float.PositiveInfinity);
        SessionState.SetFloat(LiveEcsPlaybackClosestMissileDistanceKey, math.min(closest, distance));
        TrackVisualDistances(hasGround, hasAir, hasGroundVisual, groundPosition, airPosition, groundVisualPosition);
    }

    private static void TrackInterceptEventMetrics(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MissileInterceptedComponent>());
        using NativeArray<MissileInterceptedComponent> intercepts = query.ToComponentDataArray<MissileInterceptedComponent>(Allocator.Temp);
        bool sawIntercept = intercepts.Length > 0;
        for (int i = 0; i < intercepts.Length; i++)
        {
            SessionState.SetBool(LiveEcsPlaybackSeenInterceptEventKey, true);
            float visualSeparation = intercepts[i].VisualSeparation;
            if (!math.isfinite(visualSeparation))
                continue;

            float closestVisualIntercept = SessionState.GetFloat(LiveEcsPlaybackClosestVisualInterceptDistanceKey, float.PositiveInfinity);
            SessionState.SetFloat(
                LiveEcsPlaybackClosestVisualInterceptDistanceKey,
                math.min(closestVisualIntercept, visualSeparation));
        }

        if (sawIntercept && !HasAnyEntityWith<GroundMissileFlyingRocketVisualComponent>(em))
            SessionState.SetBool(LiveEcsPlaybackGroundRocketClearedAfterInterceptKey, true);
    }

    private static void TrackVisualDistances(
        bool hasGround,
        bool hasAir,
        bool hasGroundVisual,
        float3 groundPosition,
        float3 airPosition,
        float3 groundVisualPosition)
    {
        if (hasGround && hasGroundVisual)
        {
            float visualDistance = math.distance(groundPosition, groundVisualPosition);
            float closestVisual = SessionState.GetFloat(LiveEcsPlaybackClosestGroundVisualDistanceKey, float.PositiveInfinity);
            SessionState.SetFloat(LiveEcsPlaybackClosestGroundVisualDistanceKey, math.min(closestVisual, visualDistance));
        }

        if (hasAir && hasGroundVisual)
        {
            float visualInterceptDistance = math.distance(airPosition, groundVisualPosition);
            float closestVisualIntercept = SessionState.GetFloat(LiveEcsPlaybackClosestVisualInterceptDistanceKey, float.PositiveInfinity);
            SessionState.SetFloat(
                LiveEcsPlaybackClosestVisualInterceptDistanceKey,
                math.min(closestVisualIntercept, visualInterceptDistance));
        }
    }

    private static bool HasInstantiatedUnit(EntityManager em, string sourceKey)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSourcePrefabKey>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || em.HasComponent<Prefab>(entity))
                continue;

            string candidate = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (string.Equals(candidate, sourceKey, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool HasAnyEntityWith<T>(EntityManager em)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return query.CalculateEntityCount() > 0;
    }

    private static bool TryFindFirstPosition<T>(EntityManager em, out float3 position)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<T>(),
            ComponentType.ReadOnly<LocalTransform>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<LocalTransform>(entity))
                continue;

            position = em.GetComponentData<LocalTransform>(entity).Position;
            return true;
        }

        position = float3.zero;
        return false;
    }

    private static void CompleteLiveEcsPlaybackValidation(bool passed, string message)
    {
        EditorApplication.update -= OnLiveEcsPlaybackValidationUpdate;
        Application.logMessageReceived -= OnLiveEcsPlaybackValidationLog;
        SessionState.EraseBool(LiveEcsPlaybackActiveKey);
        SessionState.EraseFloat(LiveEcsPlaybackStartedAtKey);
        SessionState.EraseBool(LiveEcsPlaybackSeenRegistryKey);
        SessionState.EraseBool(LiveEcsPlaybackSeenAirLauncherKey);
        SessionState.EraseBool(LiveEcsPlaybackSeenGroundLauncherKey);
        SessionState.EraseBool(LiveEcsPlaybackSeenProjectileKey);
        SessionState.EraseBool(LiveEcsPlaybackSeenGroundProjectileKey);
        SessionState.EraseBool(LiveEcsPlaybackSeenAirProjectileKey);
        SessionState.EraseBool(LiveEcsPlaybackSeenGroundRocketVisualKey);
        SessionState.EraseBool(LiveEcsPlaybackSeenInterceptEventKey);
        SessionState.EraseBool(LiveEcsPlaybackGroundRocketClearedAfterInterceptKey);
        SessionState.EraseFloat(LiveEcsPlaybackClosestMissileDistanceKey);
        SessionState.EraseFloat(LiveEcsPlaybackClosestGroundVisualDistanceKey);
        SessionState.EraseFloat(LiveEcsPlaybackClosestVisualInterceptDistanceKey);
        SessionState.EraseFloat(LiveEcsPlaybackMaxGroundMissileAltitudeKey);
        SessionState.EraseString(LiveEcsPlaybackFailureKey);
        SessionState.EraseBool(LiveEcsPlaybackValidateAllVariantsKey);
        SessionState.EraseInt(LiveEcsPlaybackVariantDropdownValueKey);
        SessionState.EraseBool(LiveEcsPlaybackVariantRunStartedKey);
        SessionState.EraseInt(LiveEcsPlaybackPassedVariantsKey);

        if (EditorApplication.isPlaying)
        {
            SessionState.SetBool(LiveEcsPlaybackPendingExitKey, true);
            SessionState.SetBool(LiveEcsPlaybackPendingPassedKey, passed);
            SessionState.SetString(LiveEcsPlaybackPendingMessageKey, message);
            HookLiveEcsPlaybackPendingExit();
            EditorApplication.ExitPlaymode();
            return;
        }

        FinishLiveEcsPlaybackValidationExit(passed, message);
    }

    private static void CompleteVisualSwitchValidation(bool passed, string message)
    {
        EditorApplication.update -= OnVisualSwitchValidationUpdate;
        Application.logMessageReceived -= OnVisualSwitchValidationLog;
        SessionState.EraseBool(VisualSwitchActiveKey);
        SessionState.EraseFloat(VisualSwitchStartedAtKey);
        SessionState.EraseInt(VisualSwitchPhaseKey);
        SessionState.EraseFloat(VisualSwitchPhaseStartedAtKey);
        SessionState.EraseString(VisualSwitchFailureKey);
        SessionState.EraseBool(VisualSwitchSeenAd001GroundProjectileKey);
        SessionState.EraseBool(VisualSwitchSeenAd002TargetKey);
        SessionState.EraseBool(VisualSwitchSeenAd002AirProjectileKey);
        SessionState.EraseBool(VisualSwitchSeenAd002ImpactKey);
        SessionState.EraseBool(VisualSwitchSeenAd011TargetKey);
        SessionState.EraseBool(VisualSwitchSeenAd011AirProjectileKey);
        SessionState.EraseBool(VisualSwitchSeenAd011ImpactKey);
        SessionState.EraseBool(VisualSwitchSeenAd011HelicopterTargetKey);
        SessionState.EraseBool(VisualSwitchSeenAd011HelicopterAirProjectileKey);
        SessionState.EraseBool(VisualSwitchSeenAd011HelicopterImpactKey);
        SessionState.EraseBool(VisualSwitchSeenAd011DroneTargetKey);
        SessionState.EraseBool(VisualSwitchSeenAd011DroneAirProjectileKey);
        SessionState.EraseBool(VisualSwitchSeenAd011DroneImpactKey);
        SessionState.EraseBool(VisualSwitchSeenAd011AttackingJetTargetKey);
        SessionState.EraseBool(VisualSwitchSeenAd011AttackingJetAirProjectileKey);
        SessionState.EraseBool(VisualSwitchSeenAd011AttackingJetImpactKey);

        if (EditorApplication.isPlaying)
        {
            SessionState.SetBool(LiveEcsPlaybackPendingExitKey, true);
            SessionState.SetBool(LiveEcsPlaybackPendingPassedKey, passed);
            SessionState.SetString(
                LiveEcsPlaybackPendingMessageKey,
                $"Next visual switch validation {(passed ? "passed" : "failed")}: {message}");
            HookLiveEcsPlaybackPendingExit();
            EditorApplication.ExitPlaymode();
            return;
        }

        FinishLiveEcsPlaybackValidationExit(
            passed,
            $"Next visual switch validation {(passed ? "passed" : "failed")}: {message}");
    }

    private static void HookLiveEcsPlaybackPendingExit()
    {
        EditorApplication.update -= OnLiveEcsPlaybackPendingExitUpdate;
        EditorApplication.update += OnLiveEcsPlaybackPendingExitUpdate;
    }

    private static void OnLiveEcsPlaybackPendingExitUpdate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.update -= OnLiveEcsPlaybackPendingExitUpdate;
        bool passed = SessionState.GetBool(LiveEcsPlaybackPendingPassedKey, false);
        string message = SessionState.GetString(LiveEcsPlaybackPendingMessageKey, string.Empty);
        SessionState.EraseBool(LiveEcsPlaybackPendingExitKey);
        SessionState.EraseBool(LiveEcsPlaybackPendingPassedKey);
        SessionState.EraseString(LiveEcsPlaybackPendingMessageKey);
        FinishLiveEcsPlaybackValidationExit(passed, message);
    }

    private static void FinishLiveEcsPlaybackValidationExit(bool passed, string message)
    {
        if (passed)
        {
            Debug.Log($"[BattleScenarioLab] Live ECS playback validation passed: {message}");
            Exit(0);
            return;
        }

        Debug.LogError($"[BattleScenarioLab] Live ECS playback validation failed: {message}");
        Exit(1);
    }

    private static void WriteVariants(SerializedProperty variantsProperty, BattleScenarioVariant[] variants)
    {
        variantsProperty.arraySize = variants.Length;
        for (int i = 0; i < variants.Length; i++)
        {
            BattleScenarioVariant variant = variants[i];
            SerializedProperty element = variantsProperty.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("VariantId").stringValue = variant.VariantId;
            element.FindPropertyRelative("Label").stringValue = variant.Label;
            element.FindPropertyRelative("SupportMode").enumValueIndex = (int)variant.SupportMode;
            element.FindPropertyRelative("IncomingThreatKind").enumValueIndex = (int)variant.IncomingThreatKind;
            element.FindPropertyRelative("IncomingThreatSpeedMultiplier").floatValue = variant.IncomingThreatSpeedMultiplier;
            element.FindPropertyRelative("IncomingThreatStartDistance").floatValue = variant.IncomingThreatStartDistance;
            element.FindPropertyRelative("IncomingThreatAltitude").floatValue = variant.IncomingThreatAltitude;
            element.FindPropertyRelative("LauncherCount").intValue = variant.LauncherCount;
            element.FindPropertyRelative("RadarDistanceFromLauncher").floatValue = variant.RadarDistanceFromLauncher;
            element.FindPropertyRelative("ExpectedOutcome").enumValueIndex = (int)variant.ExpectedOutcome;
        }
    }

    private static void WriteSuccessCriteria(SerializedProperty criteria)
    {
        criteria.FindPropertyRelative("RequireDetection").boolValue = true;
        criteria.FindPropertyRelative("RequireLaunch").boolValue = true;
        criteria.FindPropertyRelative("RequireInterceptForSupportedNormal").boolValue = true;
        criteria.FindPropertyRelative("RequireSupportedVariantImprovesOrMatchesBaseline").boolValue = true;
        criteria.FindPropertyRelative("MaxSupportedDetectionDelaySeconds").floatValue = 0f;
        criteria.FindPropertyRelative("MaxSupportedLockDelaySeconds").floatValue = 0f;
    }

    private static GameObject RequireObject(string name)
    {
        GameObject gameObject = GameObject.Find(name);
        if (gameObject == null)
            throw new InvalidOperationException($"Missing required scene object: {name}");
        return gameObject;
    }

    private static T RequireComponent<T>(GameObject gameObject)
        where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
            throw new InvalidOperationException($"Missing required component {typeof(T).Name} on {gameObject.name}");
        return component;
    }

    private static T RequireSceneComponent<T>()
        where T : UnityEngine.Object
    {
        T component = UnityEngine.Object.FindAnyObjectByType<T>();
        if (component == null)
            throw new InvalidOperationException($"Missing required scene component: {typeof(T).Name}");
        return component;
    }

    private static void RequireReference(UnityEngine.Object reference, string label)
    {
        if (reference == null)
            throw new InvalidOperationException($"Missing required reference: {label}");
    }

    private static void RequireScenarioLabSubSceneReference()
    {
        GameObject subSceneObject = RequireObject("BattleScenarioLabBakedPrefabsSubScene");
        Component[] components = subSceneObject.GetComponents<Component>();
        Component subScene = null;
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component != null && component.GetType().FullName == "Unity.Scenes.SubScene")
            {
                subScene = component;
                break;
            }
        }

        if (subScene == null)
            throw new InvalidOperationException("BattleScenarioLabBakedPrefabsSubScene is missing Unity.Scenes.SubScene.");

        SerializedObject serialized = new(subScene);
        RequireReference(
            serialized.FindProperty("_SceneAsset").objectReferenceValue,
            "Scenario Lab baked prefab subscene asset");
        SerializedProperty autoLoad = serialized.FindProperty("AutoLoadScene");
        if (autoLoad != null && !autoLoad.boolValue)
            throw new InvalidOperationException("Scenario Lab baked prefab subscene must AutoLoadScene.");
    }

    private static void RequireScenarioLabPrefabRegistryConfig()
    {
        UnitPrefabRegistryAuthoringConfig config =
            AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(BattleScenarioLabSceneBuilder.PrefabRegistryConfigPath);
        RequireReference(config, "Scenario Lab unit prefab registry config");

        SerializedObject serialized = new(config);
        SerializedProperty prefabs = serialized.FindProperty("unitSpawnPrefabs");
        if (prefabs == null || prefabs.arraySize < 6)
            throw new InvalidOperationException("Scenario Lab unit prefab registry must contain the Scenario Lab production prefabs.");

        RequirePrefabInRegistry(prefabs, "Unit_Veh_Missle_Launcher_Ground");
        RequirePrefabInRegistry(prefabs, "Unit_Veh_Missle_Launcher_Air");
        RequirePrefabInRegistry(prefabs, "Unit_Veh_Radar_Tank");
        RequirePrefabInRegistry(prefabs, JetTargetSourceKey);
        RequirePrefabInRegistry(prefabs, HelicopterTargetSourceKey);
        RequirePrefabInRegistry(prefabs, DroneTargetSourceKey);
    }

    private static void RequirePrefabInRegistry(SerializedProperty prefabs, string prefabName)
    {
        for (int i = 0; i < prefabs.arraySize; i++)
        {
            if (prefabs.GetArrayElementAtIndex(i).objectReferenceValue is GameObject prefab &&
                string.Equals(prefab.name, prefabName, StringComparison.Ordinal))
            {
                return;
            }
        }

        throw new InvalidOperationException($"Scenario Lab unit prefab registry is missing {prefabName}.");
    }

    private static void RequireDropdownTemplate(Dropdown dropdown, string name)
    {
        if (dropdown.template == null)
            throw new InvalidOperationException($"{name} dropdown template is not assigned.");
        if (dropdown.template.GetComponentInChildren<Toggle>(true) == null)
            throw new InvalidOperationException($"{name} dropdown template is missing an item Toggle.");
        if (dropdown.captionText == null)
            throw new InvalidOperationException($"{name} dropdown caption text is not assigned.");
        if (dropdown.itemText == null)
            throw new InvalidOperationException($"{name} dropdown item text is not assigned.");
    }

    private static void Exit(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}
#endif
