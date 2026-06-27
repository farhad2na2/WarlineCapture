#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
    public const string Gm001DefinitionPath = "Assets/Game/Configs/ScenarioLab/GM001_GroundMissileLauncher_FiresVisibleRocketAndDamagesTarget.asset";
    public const string Dr001DefinitionPath = "Assets/Game/Configs/ScenarioLab/DR001_DroneReconDetectionAndThreatWarning.asset";

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
            RequireObject("AD001VisualPlayback");
            RequireObject("GroundMissileLauncherVisual");
            RequireObject("AirMissileLauncherVisual");
            RequireObject("RadarSupportVisual");
            RequireObject("DefendedTargetVisual");
            RequireObject("IncomingGroundMissileVisual");
            RequireObject("AirDefenseInterceptorVisual");
            RequireObject("IncomingGroundMissileTrail");
            RequireObject("AirDefenseInterceptorTrail");
            RequireObject("GroundLaunchFlash");
            RequireObject("AirLaunchFlash");
            RequireObject("InterceptExplosion");
            RequireObject("ScenarioLabOverlay");
            RequireObject("MetricsPanel");
            GameObject eventSystemObject = RequireObject("EventSystem");
            RequireComponent<EventSystem>(eventSystemObject);
            RequireComponent<BaseInputModule>(eventSystemObject);
            Dropdown variantDropdown = RequireComponent<Dropdown>(RequireObject("VariantSelector"));
            if (variantDropdown.options.Count < 2)
                throw new InvalidOperationException("VariantSelector has no scenario variant options.");
            Button restartButton = RequireComponent<Button>(RequireObject("RestartScenarioButton"));
            if (restartButton.onClick.GetPersistentEventCount() == 0)
                throw new InvalidOperationException("RestartScenarioButton has no persistent click listener.");

            SerializedObject bootstrapSerialized = new(bootstrap);
            RequireReference(
                bootstrapSerialized.FindProperty("scenarioDefinition").objectReferenceValue,
                "bootstrap scenario definition");
            RequireReference(
                bootstrapSerialized.FindProperty("overlayView").objectReferenceValue,
                "bootstrap overlay view");
            RequireReference(
                bootstrapSerialized.FindProperty("visualPlayback").objectReferenceValue,
                "bootstrap visual playback");
            RequireReference(
                bootstrapSerialized.FindProperty("variantDropdown").objectReferenceValue,
                "bootstrap variant dropdown");

            SerializedObject visualSerialized = new(visualPlayback);
            RequireReference(visualSerialized.FindProperty("scenarioCamera").objectReferenceValue, "visual playback camera");
            RequireReference(visualSerialized.FindProperty("groundLauncherVisual").objectReferenceValue, "visual playback ground launcher");
            RequireReference(visualSerialized.FindProperty("airLauncherVisual").objectReferenceValue, "visual playback air launcher");
            RequireReference(visualSerialized.FindProperty("radarVisual").objectReferenceValue, "visual playback radar");
            RequireReference(visualSerialized.FindProperty("defendedTargetVisual").objectReferenceValue, "visual playback defended target");
            RequireReference(visualSerialized.FindProperty("incomingMissileVisual").objectReferenceValue, "visual playback incoming missile");
            RequireReference(visualSerialized.FindProperty("interceptorVisual").objectReferenceValue, "visual playback interceptor");
            RequireReference(visualSerialized.FindProperty("incomingTrail").objectReferenceValue, "visual playback incoming trail");
            RequireReference(visualSerialized.FindProperty("interceptorTrail").objectReferenceValue, "visual playback interceptor trail");
            RequireReference(visualSerialized.FindProperty("groundLaunchFlash").objectReferenceValue, "visual playback ground launch flash");
            RequireReference(visualSerialized.FindProperty("airLaunchFlash").objectReferenceValue, "visual playback air launch flash");
            RequireReference(visualSerialized.FindProperty("interceptExplosion").objectReferenceValue, "visual playback intercept explosion");

            SerializedObject overlaySerialized = new(overlay);
            RequireReference(overlaySerialized.FindProperty("titleText").objectReferenceValue, "overlay title text");
            RequireReference(overlaySerialized.FindProperty("statusText").objectReferenceValue, "overlay status text");
            RequireReference(overlaySerialized.FindProperty("variantsText").objectReferenceValue, "overlay variants text");
            RequireReference(overlaySerialized.FindProperty("comparisonsText").objectReferenceValue, "overlay comparisons text");

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

    private static void Exit(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}
#endif
