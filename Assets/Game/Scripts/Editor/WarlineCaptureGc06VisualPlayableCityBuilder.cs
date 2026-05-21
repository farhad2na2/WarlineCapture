#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WarlineCaptureGc06VisualPlayableCityBuilder
{
    private const string DemoScenePath = "Assets/Game/Scenes/Demo.unity";
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC06_VisualPlayableCity_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC06_VisualPlayableCity_2048";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GC06_VisualPlayableCity_2048";
    private const string ModuleCatalogPath = DataRoot + "/gc06_demo_module_catalog.json";
    private const string ReportPath = "Design/AgentReports/2026-05-20_gameplay_gc06-visual-playable-city.md";
    private const float MapSize = 2048f;

    private static readonly List<Zone> Roads = new();
    private static readonly List<Zone> Spawns = new();
    private static readonly List<Zone> Objectives = new();
    private static readonly List<Zone> ModuleFootprints = new();
    private static readonly List<string> BuildLog = new();
    private static readonly List<string> ValidationLog = new();
    private static readonly List<string> RejectLog = new();
    private static readonly List<ModuleRecord> ModuleRecords = new();
    private static int rejectedInteriorTerrainRoots;

    private readonly struct Zone
    {
        public readonly string Name;
        public readonly Rect Rect;

        public Zone(string name, Rect rect)
        {
            Name = name;
            Rect = rect;
        }
    }

    private readonly struct ModuleSpec
    {
        public readonly string Name;
        public readonly string Role;
        public readonly Bounds SourceBounds;
        public readonly Vector3 TargetCenter;
        public readonly float RotationY;
        public readonly float Scale;

        public ModuleSpec(string name, string role, Bounds sourceBounds, Vector3 targetCenter, float rotationY, float scale)
        {
            Name = name;
            Role = role;
            SourceBounds = sourceBounds;
            TargetCenter = targetCenter;
            RotationY = rotationY;
            Scale = scale;
        }

        public Vector2 Footprint => new(SourceBounds.size.x * Scale + 18f, SourceBounds.size.z * Scale + 18f);
    }

    [Serializable]
    private sealed class ModuleRecord
    {
        public string name;
        public string role;
        public float sourceCenterX;
        public float sourceCenterZ;
        public float sourceWidth;
        public float sourceDepth;
        public float targetX;
        public float targetZ;
        public float rotationY;
        public float scale;
        public float footprintWidth;
        public float footprintDepth;
        public int clonedRoots;
    }

    [Serializable]
    private sealed class ModuleCatalogFile
    {
        public string generatedBy;
        public string sourceScene;
        public string generatedScene;
        public List<ModuleRecord> modules = new();
    }

    [MenuItem("WarlineCapture/Design/Build GC06 Visual Playable City 2048")]
    public static void BuildGc06VisualPlayableCity2048()
    {
        Roads.Clear();
        Spawns.Clear();
        Objectives.Clear();
        ModuleFootprints.Clear();
        BuildLog.Clear();
        ValidationLog.Clear();
        RejectLog.Clear();
        ModuleRecords.Clear();
        rejectedInteriorTerrainRoots = 0;

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        Directory.CreateDirectory(ProjectPath(DataRoot));

        Scene demoScene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        List<ModuleSpec> modules = BuildModuleSpecs();
        Dictionary<string, List<GameObject>> sourceByModule = new(StringComparer.Ordinal);
        foreach (ModuleSpec module in modules)
            sourceByModule[module.Name] = CollectSourceRoots(module);

        Scene generatedScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(generatedScene);

        GameObject root = new("GC06_VisualPlayableCity_2048_Root");
        BuildRenderEnvironment(root);
        BuildBasePlane(root);
        DefineGameplayMasks();
        BuildPlayableRoadSurfaces(root);
        CloneModules(root, sourceByModule, modules);
        BuildArtDirectedSetDressing(root);
        NormalizeSceneLightBudget(root);
        PlaceSoldierRouteProof(root);
        BuildCameras(root);
        ValidateLayout();
        ClearEditorSelectionOutlines(root);

        EditorSceneManager.CloseScene(demoScene, true);
        EditorSceneManager.SaveScene(generatedScene, ScenePath);
        CaptureScene();
        WriteModuleCatalog();
        WriteReport();
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GC06_VISUAL_PLAYABLE_CITY_BUILT scene={ScenePath} captureRoot={CaptureRoot} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static List<ModuleSpec> BuildModuleSpecs()
    {
        return new List<ModuleSpec>();
    }

    private static void BuildRenderEnvironment(GameObject root)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.49f, 1f);
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.57f, 0.49f, 0.37f, 1f);
        RenderSettings.fogDensity = 0.00036f;

        Light key = Child(root, "DirectionalLight_Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.95f;
        key.color = new Color(1f, 0.88f, 0.66f, 1f);
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.58f;
        key.shadowBias = 0.04f;
        key.shadowNormalBias = 0.42f;
        key.transform.rotation = Quaternion.Euler(50f, -42f, 0f);

        Light fill = Child(root, "DirectionalLight_Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.34f;
        fill.color = new Color(0.58f, 0.74f, 1f, 1f);
        fill.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(30f, 136f, 0f);

        Volume volume = Child(root, "GC06_RTS_PresentationVolume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "GC06_RTS_PresentationProfile";
        volume.sharedProfile = profile;
        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.02f);
        color.contrast.Override(22f);
        color.saturation.Override(8f);
        color.colorFilter.Override(new Color(1f, 0.93f, 0.82f, 1f));
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
        profile.Add<Bloom>(true).intensity.Override(0.035f);
    }

    private static void BuildBasePlane(GameObject root)
    {
        Surface(root, "FlatGameplayPlane_2048x2048", Vector3.zero, new Vector2(MapSize, MapSize), CreateMaterial("GC06_SandBase", new Color(0.48f, 0.39f, 0.24f, 1f)), 0f);
    }

    private static void DefineGameplayMasks()
    {
        AddRoad("MainHighway", -590f, 0f, 82f, 1560f);
        AddRoad("BaseEntryRoad", -330f, -420f, 520f, 42f);
        AddRoad("CentralParadeRoad", -70f, 0f, 52f, 1120f);
        AddRoad("TentCampLane_South", -225f, -260f, 620f, 36f);
        AddRoad("TentCampLane_North", -225f, 20f, 620f, 36f);
        AddRoad("HangarServiceRoad", 270f, 260f, 620f, 38f);
        AddRoad("VehicleDepotLane", 275f, -210f, 640f, 38f);
        AddRoad("RunwayAccessRoad", 640f, 100f, 44f, 780f);
        AddRoad("FuelServiceRoad", 500f, -520f, 420f, 36f);

        Spawns.Add(new Zone("PlayerSpawn_MainGate", CenterRect(-520f, -420f, 74f, 48f)));
        Spawns.Add(new Zone("EnemySpawn_RunwayApron", CenterRect(640f, 260f, 74f, 48f)));
        Objectives.Add(new Zone("Objective_CommandTent", CenterRect(-70f, 20f, 92f, 60f)));
        Objectives.Add(new Zone("Objective_VehicleDepot", CenterRect(290f, -210f, 96f, 60f)));
        Objectives.Add(new Zone("Objective_RunwayJets", CenterRect(640f, 260f, 96f, 60f)));
    }

    private static void AddRoad(string name, float centerX, float centerZ, float sizeX, float sizeZ)
    {
        Roads.Add(new Zone(name, CenterRect(centerX, centerZ, sizeX, sizeZ)));
    }

    private static void BuildPlayableRoadSurfaces(GameObject root)
    {
        GameObject masks = Child(root, "PlayableRoads_Visual");
        Material road = CreateMaterial("GC06_DustTrackRoad", new Color(0.50f, 0.43f, 0.31f, 1f));
        Material highway = CreateMaterial("GC06_MainHighway_DustTrack", new Color(0.42f, 0.36f, 0.27f, 1f));
        Material shoulder = CreateMaterial("GC06_RoadShoulder_SoftDust", new Color(0.60f, 0.50f, 0.32f, 1f));
        Material roadEdge = CreateMaterial("GC06_RoadEdge_SandHighlight", new Color(0.67f, 0.57f, 0.38f, 1f));

        foreach (Zone roadZone in Roads)
        {
            Surface(masks, roadZone.Name + "_Shoulder", Center(roadZone.Rect, 0.045f), new Vector2(roadZone.Rect.width + 18f, roadZone.Rect.height + 18f), shoulder, 0.045f);
            Surface(masks, roadZone.Name, Center(roadZone.Rect, 0.055f), new Vector2(roadZone.Rect.width, roadZone.Rect.height), roadZone.Name == "MainHighway" ? highway : road, 0.055f);
        }

        Surface(masks, "MainHighway_WestDustLine", new Vector3(-642f, 0.066f, 0f), new Vector2(3f, 1550f), roadEdge, 0.066f);
        Surface(masks, "MainHighway_EastDustLine", new Vector3(-538f, 0.066f, 0f), new Vector2(3f, 1550f), roadEdge, 0.066f);
    }

    private static void CloneModules(GameObject root, Dictionary<string, List<GameObject>> sourceByModule, List<ModuleSpec> modules)
    {
        GameObject moduleRoot = Child(root, "DemoAuthoredPlayableModules");
        foreach (ModuleSpec module in modules)
        {
            Rect footprint = FootprintRect(module.TargetCenter, module.Footprint);
            if (Roads.Any(road => road.Rect.Overlaps(footprint)))
            {
                ValidationLog.Add($"ERROR: module {module.Name} footprint overlaps explicit walkable road. footprint={Format(footprint)}");
                continue;
            }

            if (ModuleFootprints.Any(existing => existing.Rect.Overlaps(footprint)))
            {
                ValidationLog.Add($"ERROR: module {module.Name} footprint overlaps existing module footprint. footprint={Format(footprint)}");
                continue;
            }

            List<GameObject> roots = sourceByModule[module.Name];
            GameObject placedRoot = Child(moduleRoot, module.Name);
            Quaternion rotation = Quaternion.Euler(0f, module.RotationY, 0f);
            foreach (GameObject source in roots)
            {
                if (source == null)
                    continue;

                GameObject clone = Object.Instantiate(source);
                clone.name = module.Name + "_" + source.name;
                clone.transform.SetParent(placedRoot.transform, true);

                Vector3 relative = source.transform.position - module.SourceBounds.center;
                relative = rotation * (relative * module.Scale);
                clone.transform.position = module.TargetCenter + new Vector3(relative.x, source.transform.position.y * 0.12f, relative.z);
                clone.transform.rotation = rotation * source.transform.rotation;
                clone.transform.localScale = source.transform.lossyScale * module.Scale;
                AlignBottomNearGround(clone);
            }

            ModuleFootprints.Add(new Zone(module.Name, footprint));
            ModuleRecords.Add(new ModuleRecord
            {
                name = module.Name,
                role = module.Role,
                sourceCenterX = module.SourceBounds.center.x,
                sourceCenterZ = module.SourceBounds.center.z,
                sourceWidth = module.SourceBounds.size.x,
                sourceDepth = module.SourceBounds.size.z,
                targetX = module.TargetCenter.x,
                targetZ = module.TargetCenter.z,
                rotationY = module.RotationY,
                scale = module.Scale,
                footprintWidth = module.Footprint.x,
                footprintDepth = module.Footprint.y,
                clonedRoots = roots.Count
            });
            BuildLog.Add($"{module.Name}: cloned {roots.Count} Demo roots into legal module footprint={Format(footprint)} role={module.Role} scale={module.Scale.ToString("0.##", CultureInfo.InvariantCulture)}");
        }
    }

    private static void BuildArtDirectedSetDressing(GameObject root)
    {
        GameObject dressing = Child(root, "GC06_ArtDirectedPlayableDressing");

        BuildPerimeter(dressing);
        BuildDenseStrategicAnchors(dressing);
        BuildTentCamp(dressing);
        BuildCommandAndHangarZone(dressing);
        BuildVehicleDepot(dressing);
        BuildRunwayApron(dressing);
        BuildFuelAndUtilityZone(dressing);
        BuildCoverAndDetailPass(dressing);
    }

    private static void BuildDenseStrategicAnchors(GameObject parent)
    {
        PlacePrefab(parent, "Assets/Game/Prefabs/Generated/IsometricMaps/LargeMapStrategicAreas_A/StrategicTentCamp_A.prefab", new Vector3(-275f, 0f, -160f), 0f, 0.92f, "DenseAnchor_StrategicTentCamp");
        PlacePrefab(parent, "Assets/Game/Prefabs/Generated/IsometricMaps/LargeMapStrategicAreas_A/StrategicVehicleTankDepot_A.prefab", new Vector3(285f, 0f, -240f), 0f, 0.92f, "DenseAnchor_StrategicVehicleTankDepot");
        PlacePrefab(parent, "Assets/Game/Prefabs/Generated/IsometricMaps/LargeMapStrategicAreas_A/StrategicAirportHeliArea_A.prefab", new Vector3(530f, 0f, 255f), 0f, 0.82f, "DenseAnchor_StrategicAirportHeliArea");
        PlacePrefab(parent, "Assets/Game/Prefabs/Generated/IsometricMaps/LargeMapStrategicAreas_A/StrategicOilRefineryFuel_A.prefab", new Vector3(390f, 0f, -535f), 0f, 0.72f, "DenseAnchor_StrategicFuelArea");
    }

    private static void BuildPerimeter(GameObject parent)
    {
        for (int i = 0; i < 15; i++)
        {
            float x = -505f + i * 72f;
            PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Wall_Dirt_Straight.prefab", new Vector3(x, 0f, -650f), 90f, 1.18f, "PerimeterWall_South_" + i.ToString("00", CultureInfo.InvariantCulture));
            PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Wall_Dirt_Straight.prefab", new Vector3(x, 0f, 650f), 90f, 1.18f, "PerimeterWall_North_" + i.ToString("00", CultureInfo.InvariantCulture));
        }

        for (int i = 0; i < 18; i++)
        {
            float z = -590f + i * 70f;
            PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Wall_Dirt_Straight.prefab", new Vector3(-560f, 0f, z), 0f, 1.18f, "PerimeterWall_West_" + i.ToString("00", CultureInfo.InvariantCulture));
            PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Wall_Dirt_Straight.prefab", new Vector3(560f, 0f, z), 0f, 1.18f, "PerimeterWall_East_" + i.ToString("00", CultureInfo.InvariantCulture));
        }

        Vector3[] towers =
        {
            new(-555f, 0f, -645f), new(-555f, 0f, 645f), new(555f, 0f, -645f), new(555f, 0f, 645f),
            new(-555f, 0f, -410f), new(-555f, 0f, 110f), new(555f, 0f, -185f), new(555f, 0f, 250f)
        };

        for (int i = 0; i < towers.Length; i++)
            PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Building_GuardTower.prefab", towers[i], i * 45f, 0.78f, "GuardTower_" + i.ToString("00", CultureInfo.InvariantCulture));

        PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Building_Road_Barrier.prefab", new Vector3(-520f, 0f, -450f), 90f, 1.25f, "MainGate_Barrier_L");
        PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Building_Road_Barrier.prefab", new Vector3(-520f, 0f, -390f), 90f, 1.25f, "MainGate_Barrier_R");
    }

    private static void BuildTentCamp(GameObject parent)
    {
        string[] tents =
        {
            "Assets/Game/Prefabs/Buildings/Tent_Regular.prefab",
            "Assets/Game/Prefabs/Buildings/Tent_Contractor.prefab",
            "Assets/Game/Prefabs/Buildings/Tent_Expert.prefab"
        };

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                Vector3 position = new(-410f + col * 92f, 0f, -300f + row * 150f);
                PlacePrefab(parent, tents[(row + col) % tents.Length], position, 90f, 0.86f, $"TentCamp_Row{row}_Tent{col}");
            }
        }

        PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Building_WaterTank.prefab", new Vector3(-40f, 0f, -76f), 0f, 0.68f, "TentCamp_WaterTower");
        PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Tent_Portaloo.prefab", new Vector3(-512f, 0f, -60f), 0f, 0.8f, "TentCamp_ServiceToilets");
    }

    private static void BuildCommandAndHangarZone(GameObject parent)
    {
        PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab", new Vector3(-80f, 0f, 120f), 90f, 0.9f, "Command_Barrack_A");
        PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab", new Vector3(74f, 0f, 120f), 90f, 0.9f, "Command_Barrack_B");
        PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Building_Hall_01.prefab", new Vector3(50f, 0f, 410f), 90f, 0.82f, "North_Hall_Command");
        PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Building_Airport.prefab", new Vector3(322f, 0f, 410f), 90f, 0.76f, "Airfield_Hangar");
        PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Tent.prefab", new Vector3(-125f, 0f, 330f), 90f, 0.92f, "Command_LargeTent");
    }

    private static void BuildVehicleDepot(GameObject parent)
    {
        Vector3[] tankPositions =
        {
            new(150f, 0f, -290f), new(260f, 0f, -290f), new(370f, 0f, -290f),
            new(170f, 0f, -145f), new(300f, 0f, -145f)
        };

        for (int i = 0; i < tankPositions.Length; i++)
            PlacePrefab(parent, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab", tankPositions[i], 250f + i * 6f, 0.66f, "VehicleDepot_Tank_" + i.ToString("00", CultureInfo.InvariantCulture));

        PlacePrefab(parent, "Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Heavy.prefab", new Vector3(450f, 0f, -205f), 252f, 0.72f, "VehicleDepot_Apc_Heavy");
        PlacePrefab(parent, "Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Fast.prefab", new Vector3(475f, 0f, -305f), 248f, 0.7f, "VehicleDepot_Apc_Fast");
        PlacePrefab(parent, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Canopy.prefab", new Vector3(65f, 0f, -435f), 88f, 0.72f, "VehicleDepot_Truck_Canopy");
        PlacePrefab(parent, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Tray.prefab", new Vector3(260f, 0f, -435f), 88f, 0.72f, "VehicleDepot_Truck_Tray");
    }

    private static void BuildRunwayApron(GameObject parent)
    {
        Surface(parent, "RunwayApron_ConcretePad", new Vector3(704f, 0.067f, 322f), new Vector2(520f, 440f), CreateMaterial("GC06_RunwayConcrete", new Color(0.42f, 0.36f, 0.30f, 1f)), 0.067f);
        PlacePrefab(parent, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Jet_01.prefab", new Vector3(760f, 0f, 400f), 225f, 0.38f, "Runway_Jet_Large");
        PlacePrefab(parent, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Jet_02.prefab", new Vector3(625f, 0f, 190f), 225f, 0.34f, "Runway_Jet_Small");
        PlacePrefab(parent, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Attack_Small.prefab", new Vector3(430f, 0f, 305f), 125f, 0.54f, "Runway_AttackHeli");
    }

    private static void BuildFuelAndUtilityZone(GameObject parent)
    {
        PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Building_Fuel_Bladder.prefab", new Vector3(390f, 0f, -535f), 0f, 0.92f, "FuelZone_Bladder_A");
        PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Building_Fuel_Bladder.prefab", new Vector3(510f, 0f, -535f), 0f, 0.92f, "FuelZone_Bladder_B");
        PlacePrefab(parent, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Tanker.prefab", new Vector3(265f, 0f, -535f), 88f, 0.68f, "FuelZone_Tanker");
        PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Building_Satelite_Dish.prefab", new Vector3(475f, 0f, 30f), 218f, 0.62f, "Comms_SatelliteDish");
        PlacePrefab(parent, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Radar_Tank.prefab", new Vector3(370f, 0f, 30f), 245f, 0.62f, "Comms_RadarTank");
    }

    private static void BuildCoverAndDetailPass(GameObject parent)
    {
        for (int i = 0; i < 12; i++)
            PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Building_Road_Barrier.prefab", new Vector3(-420f + i * 78f, 0f, -500f), 90f, 0.82f, "SouthGate_BarrierRow_" + i.ToString("00", CultureInfo.InvariantCulture));

        for (int i = 0; i < 9; i++)
        {
            PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Wall_Fence_Straight.prefab", new Vector3(120f + i * 52f, 0f, -88f), 90f, 0.72f, "Depot_InternalFence_A_" + i.ToString("00", CultureInfo.InvariantCulture));
            PlacePrefab(parent, "Assets/Game/Prefabs/Buildings/Wall_Fence_Straight.prefab", new Vector3(120f + i * 52f, 0f, -355f), 90f, 0.72f, "Depot_InternalFence_B_" + i.ToString("00", CultureInfo.InvariantCulture));
        }

        PlaceEdgeRocks(parent);
        PlaceRoadsidePalms(parent);
    }

    private static void PlaceEdgeRocks(GameObject parent)
    {
        string[] rocks =
        {
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_02.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_04.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_06.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Ruin_02.prefab"
        };

        Vector3[] positions =
        {
            new(-1010f, 0f, -860f), new(-1000f, 0f, 780f), new(-1040f, 0f, -140f), new(-950f, 0f, 410f),
            new(980f, 0f, -900f), new(1010f, 0f, 810f), new(980f, 0f, -90f), new(930f, 0f, 250f),
            new(-250f, 0f, -930f), new(260f, 0f, 900f), new(880f, 0f, -560f), new(-860f, 0f, 900f)
        };

        for (int i = 0; i < positions.Length; i++)
            PlacePrefab(parent, rocks[i % rocks.Length], positions[i], (i * 37f) % 360f, 0.85f + (i % 3) * 0.16f, "EdgeRock_" + i.ToString("00", CultureInfo.InvariantCulture));
    }

    private static void PlaceRoadsidePalms(GameObject parent)
    {
        string[] plants =
        {
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_01.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_03.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_Big_02.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Bush_Group_02.prefab"
        };

        Vector3[] positions =
        {
            new(-742f, 0f, -424f), new(-658f, 0f, -96f), new(-548f, 0f, 350f), new(-358f, 0f, -246f),
            new(-230f, 0f, 450f), new(104f, 0f, 36f), new(104f, 0f, 214f), new(318f, 0f, -100f),
            new(318f, 0f, 318f), new(690f, 0f, -130f), new(690f, 0f, 620f), new(880f, 0f, 420f)
        };

        for (int i = 0; i < positions.Length; i++)
            PlacePrefab(parent, plants[i % plants.Length], positions[i], (i * 53f) % 360f, 0.72f + (i % 2) * 0.18f, "RoadsidePlant_" + i.ToString("00", CultureInfo.InvariantCulture));
    }

    private static void PlacePrefab(GameObject parent, string path, Vector3 position, float rotationY, float scale, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            RejectLog.Add("Missing dressing prefab: " + path);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent.transform, true);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        bool generatedStrategicModule = path.Contains("/Generated/IsometricMaps/", StringComparison.Ordinal);
        float visualScale = path.Contains("/Vehicles/Unit_Veh_Jet_", StringComparison.Ordinal) || generatedStrategicModule ? scale : scale * 3.2f;
        instance.transform.localScale = Vector3.one * visualScale;
        AlignBottomNearGround(instance);
        StripDebugRingRenderers(instance);
    }

    private static void StripDebugRingRenderers(GameObject instance)
    {
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            Bounds bounds = renderer.bounds;
            if (bounds.size.y > 1.2f)
                continue;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                Color color = Color.black;
                if (material.HasProperty("_BaseColor"))
                    color = material.GetColor("_BaseColor");
                else if (material.HasProperty("_Color"))
                    color = material.GetColor("_Color");

                bool brightDebugGreen = color.g > 0.45f && color.g > color.r * 1.35f && color.g > color.b * 1.35f;
                bool debugName = material.name.IndexOf("selection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    material.name.IndexOf("ring", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    material.name.IndexOf("circle", StringComparison.OrdinalIgnoreCase) >= 0;
                if (brightDebugGreen || debugName)
                {
                    renderer.enabled = false;
                    break;
                }
            }
        }
    }

    private static List<GameObject> CollectSourceRoots(ModuleSpec module)
    {
        Dictionary<int, GameObject> roots = new();
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (transform == null || transform.gameObject.scene.path != DemoScenePath)
                continue;
            if (transform.GetComponent<Camera>() != null || transform.GetComponent<Light>() != null)
                continue;

            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0)
                continue;

            Bounds bounds = CalculateBounds(renderers);
            if (!module.SourceBounds.Intersects(bounds))
                continue;
            if (IsSkyOrHugeBackground(transform.name, bounds))
                continue;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject) ?? TopSceneObject(transform);
            if (root == null || root.GetComponent<Camera>() != null || root.GetComponent<Light>() != null)
                continue;

            Renderer[] rootRenderers = root.GetComponentsInChildren<Renderer>(false);
            if (rootRenderers.Length == 0)
                continue;
            Bounds rootBounds = CalculateBounds(rootRenderers);
            if (!module.SourceBounds.Intersects(rootBounds))
                continue;
            if (IsSkyOrHugeBackground(root.name, rootBounds))
                continue;
            if (IsInteriorTerrainBlocker(root, rootBounds))
            {
                rejectedInteriorTerrainRoots++;
                continue;
            }

            roots[root.GetInstanceID()] = root;
        }

        List<GameObject> result = roots.Values
            .Where(go => !HasSelectedAncestor(go.transform, roots))
            .OrderBy(go => go.name, StringComparer.Ordinal)
            .ToList();

        if (result.Count == 0)
            RejectLog.Add($"{module.Name}: no source roots selected from Demo.");
        return result;
    }

    private static bool HasSelectedAncestor(Transform transform, Dictionary<int, GameObject> selected)
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (selected.ContainsKey(parent.gameObject.GetInstanceID()))
                return true;
            parent = parent.parent;
        }
        return false;
    }

    private static GameObject TopSceneObject(Transform transform)
    {
        Transform current = transform;
        while (current.parent != null)
            current = current.parent;
        return current.gameObject;
    }

    private static void PlaceSoldierRouteProof(GameObject root)
    {
        GameObject units = Child(root, "Soldiers_OnValidatedWalkableStreets_NoDebugRings");

        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab", new Vector3(-520f, 0f, -420f), 72f, "player soldier 1");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab", new Vector3(-478f, 0f, -397f), 72f, "player soldier 2");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_01.prefab", new Vector3(-475f, 0f, -443f), 72f, "player soldier 3");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_02.prefab", new Vector3(-370f, 0f, -420f), 72f, "player soldier 4");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02.prefab", new Vector3(-70f, 0f, -50f), 20f, "patrol soldier 1");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01_Alt_02.prefab", new Vector3(282f, 0f, -210f), 268f, "depot soldier 1");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab", new Vector3(640f, 0f, 260f), 252f, "enemy soldier 1");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_02.prefab", new Vector3(610f, 0f, 285f), 252f, "enemy soldier 2");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Female_01.prefab", new Vector3(615f, 0f, 238f), 252f, "enemy soldier 3");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_03.prefab", new Vector3(500f, 0f, 260f), 252f, "enemy soldier 4");
    }

    private static void BuildRoute(GameObject parent, string name, Vector3[] points, Material material)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 from = points[i];
            Vector3 to = points[i + 1];
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = $"{name}_{i:00}";
            segment.transform.SetParent(parent.transform, true);
            segment.transform.position = new Vector3((from.x + to.x) * 0.5f, 0.16f, (from.z + to.z) * 0.5f);
            segment.transform.localScale = new Vector3(8f, 0.05f, Vector3.Distance(from, to));
            segment.transform.rotation = Quaternion.LookRotation(to - from, Vector3.up);
            Object.DestroyImmediate(segment.GetComponent<Collider>());
            segment.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    private static void PlaceUnit(GameObject parent, string path, Vector3 position, float rotationY, string label)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            RejectLog.Add("Missing proof unit: " + path);
            return;
        }

        GameObject unit = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        unit.name = label;
        unit.transform.SetParent(parent.transform, true);
        unit.transform.position = position;
        unit.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        unit.transform.localScale = Vector3.one * 1.45f;
        AlignBottomNearGround(unit);

        if (!IsPointOnRoad(position))
            ValidationLog.Add($"ERROR: {label} is not on a walkable road at {Format(position)}");
    }

    private static void BuildSelectionRing(GameObject parent, string name, Vector3 position, float radius, Material material)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = name;
        ring.transform.SetParent(parent.transform, true);
        ring.transform.position = position;
        ring.transform.localScale = new Vector3(radius, 0.035f, radius);
        Object.DestroyImmediate(ring.GetComponent<Collider>());
        MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void BuildCameras(GameObject root)
    {
        Camera map = CameraObject(root, "Camera_GC06_TopDownMilitaryBase");
        map.orthographic = true;
        map.orthographicSize = 760f;
        map.transform.position = new Vector3(0f, 1400f, 0f);
        map.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        BuildRtsCamera(root, "Camera_GC06_TargetStyleMilitaryOverview", new Vector3(20f, 0f, -40f), new Vector3(-260f, 520f, -540f), 34f);
        BuildRtsCamera(root, "Camera_GC06_TentCampReview", new Vector3(-245f, 0f, -150f), new Vector3(-160f, 310f, -300f), 35f);
        BuildRtsCamera(root, "Camera_GC06_VehicleDepotReview", new Vector3(280f, 0f, -220f), new Vector3(-160f, 320f, -290f), 35f);
        BuildRtsCamera(root, "Camera_GC06_RunwayApronReview", new Vector3(615f, 0f, 285f), new Vector3(-190f, 360f, -330f), 36f);
    }

    private static void BuildRtsCamera(GameObject root, string name, Vector3 target, Vector3 offset, float fieldOfView)
    {
        Camera camera = CameraObject(root, name);
        camera.orthographic = false;
        camera.fieldOfView = fieldOfView;
        camera.transform.position = target + offset;
        camera.transform.LookAt(target);
    }

    private static Camera CameraObject(GameObject root, string name)
    {
        GameObject cameraObject = Child(root, name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.42f, 0.35f, 0.24f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 3000f;
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        return camera;
    }

    private static void NormalizeSceneLightBudget(GameObject root)
    {
        foreach (Light light in root.GetComponentsInChildren<Light>(true))
        {
            if (light.type == LightType.Directional)
                continue;

            light.shadows = LightShadows.None;
            light.intensity = Mathf.Min(light.intensity * 0.12f, 0.22f);
        }
    }

    private static void ClearEditorSelectionOutlines(GameObject root)
    {
        Selection.objects = Array.Empty<Object>();
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            EditorUtility.SetSelectedRenderState(renderer, EditorSelectedRenderState.Hidden);
    }

    private static void ValidateLayout()
    {
        foreach (Zone module in ModuleFootprints)
            foreach (Zone road in Roads)
                if (module.Rect.Overlaps(road.Rect))
                    ValidationLog.Add($"ERROR: module {module.Name} overlaps explicit walkable road {road.Name}");

        foreach (Zone spawn in Spawns)
            if (!Roads.Any(road => road.Rect.Overlaps(spawn.Rect)))
                ValidationLog.Add($"ERROR: spawn {spawn.Name} is not connected to a road.");

        foreach (Zone objective in Objectives)
            if (!Roads.Any(road => road.Rect.Overlaps(objective.Rect)))
                ValidationLog.Add($"ERROR: objective {objective.Name} is not connected to a road.");

        if (ValidationLog.Count == 0)
            ValidationLog.Add("PASS: GC06 built a visual military-base RTS scene with clear playable roads, spawns, objectives, and proof soldiers.");
    }

    private static void CaptureScene()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (camera.name == "Camera_GC06_TopDownMilitaryBase")
                Render(camera, ProjectPath(CaptureRoot + "/gc06_topdown_military_base_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC06_TargetStyleMilitaryOverview")
                Render(camera, ProjectPath(CaptureRoot + "/gc06_target_style_military_overview_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC06_TentCampReview")
                Render(camera, ProjectPath(CaptureRoot + "/gc06_tent_camp_review_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC06_VehicleDepotReview")
                Render(camera, ProjectPath(CaptureRoot + "/gc06_vehicle_depot_review_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC06_RunwayApronReview")
                Render(camera, ProjectPath(CaptureRoot + "/gc06_runway_apron_review_1920x1080.png"), 1920, 1080);
        }
    }

    private static void WriteModuleCatalog()
    {
        ModuleCatalogFile catalog = new()
        {
            generatedBy = nameof(WarlineCaptureGc06VisualPlayableCityBuilder),
            sourceScene = DemoScenePath,
            generatedScene = ScenePath,
            modules = ModuleRecords
        };
        File.WriteAllText(ProjectPath(ModuleCatalogPath), JsonUtility.ToJson(catalog, true), Encoding.UTF8);
    }

    private static void WriteReport()
    {
        StringBuilder report = new();
        report.AppendLine("# GC06 Visual Military Base Playable Scene");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Build a visually good top-down military RTS scene before ECS/runtime conversion.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGc06VisualPlayableCityBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC06_VisualPlayableCity_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Data/GeneratedScenes/GC06_VisualPlayableCity_2048/gc06_demo_module_catalog.json`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC06_VisualPlayableCity_2048/gc06_target_style_military_overview_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC06_VisualPlayableCity_2048/gc06_topdown_military_base_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC06_VisualPlayableCity_2048/gc06_tent_camp_review_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC06_VisualPlayableCity_2048/gc06_vehicle_depot_review_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC06_VisualPlayableCity_2048/gc06_runway_apron_review_1920x1080.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: Gameplay playable scene generation workflow contract.");
        report.AppendLine("User-visible behavior: none in shipped flow; generated GC06 scene is available for visual/design review.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureGc06VisualPlayableCityBuilder.BuildGc06VisualPlayableCity2048`.");
        report.AppendLine("Validation result: " + (ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)) ? "failed with scene placement errors; see validation log below." : "passed visual scene generation and walkability validation."));
        report.AppendLine("Known gaps: first visual match pass only; still needs user/PM visual review against the supplied Polygon Military Pack target image before deeper runtime/ECS conversion.");
        report.AppendLine("Cross-lane impacts: Art/Design can judge visual density, camera angle, compound layout, and military-base target match from captures.");
        report.AppendLine("Next recommended task: iterate GC06 composition from the review screenshots until accepted as a visually good playable military RTS scene.");
        report.AppendLine();
        report.AppendLine($"Modules placed: {ModuleRecords.Count}");
        report.AppendLine($"Interior terrain/blocker roots rejected from playable modules: {rejectedInteriorTerrainRoots}");
        report.AppendLine();
        report.AppendLine("Validation log:");
        foreach (string line in ValidationLog)
            report.AppendLine("- " + line);
        report.AppendLine();
        report.AppendLine("Module log:");
        foreach (string line in BuildLog)
            report.AppendLine("- " + line);
        if (RejectLog.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("Selection warnings:");
            foreach (string line in RejectLog)
                report.AppendLine("- " + line);
        }

        File.WriteAllText(ProjectPath(ReportPath), report.ToString(), Encoding.UTF8);
    }

    private static bool IsPointOnRoad(Vector3 position)
    {
        Vector2 point = new(position.x, position.z);
        return Roads.Any(road => road.Rect.Contains(point));
    }

    private static bool IsSkyOrHugeBackground(string name, Bounds bounds)
    {
        string lower = name.ToLowerInvariant();
        return lower.Contains("sky") ||
            lower.Contains("cloud") ||
            lower.Contains("skydome") ||
            bounds.center.y > 500f ||
            bounds.size.x > 1300f ||
            bounds.size.z > 1300f;
    }

    private static bool IsInteriorTerrainBlocker(GameObject root, Bounds bounds)
    {
        string lower = root.name.ToLowerInvariant();
        if (lower.Contains("terrain") || lower.Contains("mountain") || lower.Contains("sanddune") || lower.Contains("dune"))
            return true;
        return bounds.size.x > 170f && bounds.size.z > 170f && bounds.size.y < 30f;
    }

    private static void AlignBottomNearGround(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return;

        Bounds bounds = CalculateBounds(renderers);
        if (bounds.min.y < -2f || bounds.min.y > 8f)
            go.transform.position -= new Vector3(0f, bounds.min.y, 0f);
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static Rect CenterRect(float centerX, float centerZ, float width, float depth)
    {
        return new Rect(centerX - width * 0.5f, centerZ - depth * 0.5f, width, depth);
    }

    private static Rect FootprintRect(Vector3 position, Vector2 size)
    {
        return CenterRect(position.x, position.z, size.x, size.y);
    }

    private static Vector3 Center(Rect rect, float y)
    {
        return new Vector3(rect.center.x, y, rect.center.y);
    }

    private static void Surface(GameObject parent, string name, Vector3 position, Vector2 size, Material material, float y)
    {
        GameObject surface = new(name);
        surface.transform.SetParent(parent.transform, false);
        surface.transform.position = new Vector3(position.x, y, position.z);
        Mesh mesh = new();
        float halfX = size.x * 0.5f;
        float halfZ = size.y * 0.5f;
        mesh.vertices = new[]
        {
            new Vector3(-halfX, 0f, -halfZ),
            new Vector3(-halfX, 0f, halfZ),
            new Vector3(halfX, 0f, halfZ),
            new Vector3(halfX, 0f, -halfZ)
        };
        mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        surface.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Color");
        Material material = new(shader)
        {
            name = name,
            color = color
        };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static void Render(Camera camera, string path, int width, int height)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture texture = new(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
        camera.targetTexture = texture;
        RenderTexture.active = texture;
        GL.Clear(true, true, camera.backgroundColor);
        camera.Render();
        Texture2D image = new(width, height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        texture.Release();
        Object.DestroyImmediate(texture);
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);
    }

    private static string Format(Vector3 value)
    {
        return $"({value.x:0.##}, {value.y:0.##}, {value.z:0.##})";
    }

    private static string Format(Rect rect)
    {
        return $"center=({rect.center.x:0.##}, {rect.center.y:0.##}) size=({rect.width:0.##}, {rect.height:0.##})";
    }
}
#endif
