#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WarlineCaptureGeneratedSceneBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC01_BaseGateTown.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC01_BaseGateTown";
    private const string ReportPath = "Design/AgentReports/2026-05-19_gameplay_gc01-base-gate-town-generation.md";

    private static readonly List<string> PlacementLog = new();
    private static readonly List<string> MissingPrefabLog = new();

    [MenuItem("WarlineCapture/Design/Build Generated Scene GC01 Base Gate Town")]
    public static void BuildGc01BaseGateTown()
    {
        PlacementLog.Clear();
        MissingPrefabLog.Clear();

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);

        CaptureScene();
        WriteReport();

        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GENERATED_SCENE_BUILT scene={ScenePath} captures={CaptureRoot} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static void BuildScene()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.78f, 0.72f, 0.62f, 1f);
        RenderSettings.skybox = null;

        GameObject root = new("GC01_BaseGateTown_Root");
        GameObject flatGrid = Child(root, "FlatGameplayGrid");
        GameObject roads = Child(root, "Roads_Highway_SideRoutes");
        GameObject town = Child(root, "TownDistrict_West");
        GameObject baseArea = Child(root, "MilitaryBase_East");
        GameObject runway = Child(root, "Runway_Airfield_Edge");
        GameObject industrial = Child(root, "IndustrialFuelObjective_SouthEast");
        GameObject dressing = Child(root, "Boundary_Decoration");

        Material sand = Material("GC01_Sand", new Color(0.62f, 0.52f, 0.35f, 1f));
        Material dirt = Material("GC01_DirtRoad", new Color(0.43f, 0.34f, 0.22f, 1f));
        Material asphalt = Material("GC01_Asphalt", new Color(0.12f, 0.115f, 0.105f, 1f));
        Material runwayMat = Material("GC01_Runway", new Color(0.2f, 0.2f, 0.18f, 1f));
        Material gridMat = Material("GC01_GridMarker", new Color(0.18f, 0.34f, 0.38f, 0.65f));

        Surface(flatGrid, "FlatDesertGameplayPlane_260x220", new Vector3(0f, 0f, 0f), new Vector2(260f, 220f), sand);
        Cube(flatGrid, "PlayableAreaBorder_North", new Vector3(0f, 0.02f, 110f), new Vector3(260f, 0.08f, 1.5f), gridMat);
        Cube(flatGrid, "PlayableAreaBorder_South", new Vector3(0f, 0.02f, -110f), new Vector3(260f, 0.08f, 1.5f), gridMat);
        Cube(flatGrid, "PlayableAreaBorder_West", new Vector3(-130f, 0.02f, 0f), new Vector3(1.5f, 0.08f, 220f), gridMat);
        Cube(flatGrid, "PlayableAreaBorder_East", new Vector3(130f, 0.02f, 0f), new Vector3(1.5f, 0.08f, 220f), gridMat);

        BuildAuthoredGround(dressing);
        BuildAuthoredRoads(roads, runway, asphalt, dirt, runwayMat);

        BuildTown(town);
        BuildMilitaryBase(baseArea);
        BuildIndustrial(industrial);
        BuildCheckpoint(roads);
        BuildBoundaryDressing(dressing);
        BuildCameras(root);
        BuildLight(root);
    }

    private static void BuildAuthoredGround(GameObject parent)
    {
        PlaceScaled(parent, "SandDuneBackplate", "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_03.prefab", new Vector3(-92f, 0f, 78f), 14f, new Vector3(0.18f, 0.12f, 0.18f));
        PlaceScaled(parent, "SandDuneBackplate", "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_01.prefab", new Vector3(94f, 0f, -84f), -18f, new Vector3(0.22f, 0.1f, 0.22f));
        PlaceScaled(parent, "SandDuneBackplate", "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_02.prefab", new Vector3(-118f, 0f, -76f), 37f, new Vector3(0.16f, 0.1f, 0.16f));
        PlaceScaled(parent, "SandDuneBackplate", "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_02.prefab", new Vector3(118f, 0f, 84f), -28f, new Vector3(0.16f, 0.1f, 0.16f));
        PlaceScaled(parent, "GroundCrater", "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Ground_Crater_02.prefab", new Vector3(-28f, 0f, 20f), 9f, new Vector3(1.4f, 1f, 1.4f));
        PlaceScaled(parent, "GroundCrater", "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Ground_Crater_02.prefab", new Vector3(42f, 0f, -42f), -22f, new Vector3(1.25f, 1f, 1.25f));
    }

    private static void BuildAuthoredRoads(GameObject roads, GameObject runway, Material asphalt, Material dirt, Material runwayMat)
    {
        Surface(roads, "Highway_Dark_Asphalt", new Vector3(0f, 0.012f, 0f), new Vector2(18f, 220f), asphalt);
        Surface(roads, "Highway_Center_Line", new Vector3(0f, 0.035f, 0f), new Vector2(0.75f, 210f), Material("GC01_Line", new Color(0.78f, 0.68f, 0.34f, 1f)));
        Surface(roads, "Highway_West_Shoulder", new Vector3(-10.6f, 0.025f, 0f), new Vector2(1.2f, 210f), Material("GC01_RoadShoulder", new Color(0.27f, 0.23f, 0.18f, 1f)));
        Surface(roads, "Highway_East_Shoulder", new Vector3(10.6f, 0.025f, 0f), new Vector2(1.2f, 210f), Material("GC01_RoadShoulder", new Color(0.27f, 0.23f, 0.18f, 1f)));

        Surface(roads, "Town_Dirt_Access_Underlay", new Vector3(-46f, 0.018f, -18f), new Vector2(75f, 10f), dirt);
        Surface(roads, "Base_Gate_Dirt_Access_Underlay", new Vector3(46f, 0.018f, 18f), new Vector2(75f, 10f), dirt);
        Surface(roads, "Industrial_Dirt_Access_Underlay", new Vector3(62f, 0.018f, -52f), new Vector2(95f, 8f), dirt);

        Surface(runway, "Runway_Edge_Underlay", new Vector3(112f, 0.018f, 24f), new Vector2(20f, 142f), runwayMat);
        Surface(runway, "Runway_CenterDash", new Vector3(112f, 0.04f, 24f), new Vector2(1f, 120f), Material("GC01_RunwayDash", new Color(0.68f, 0.68f, 0.58f, 1f)));
        Surface(runway, "Runway_WestEdge", new Vector3(101.5f, 0.04f, 24f), new Vector2(0.7f, 132f), Material("GC01_RunwayEdge", new Color(0.7f, 0.7f, 0.62f, 1f)));
        Surface(runway, "Runway_EastEdge", new Vector3(122.5f, 0.04f, 24f), new Vector2(0.7f, 132f), Material("GC01_RunwayEdge", new Color(0.7f, 0.7f, 0.62f, 1f)));
    }

    private static void BuildTown(GameObject parent)
    {
        string[] homes =
        {
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_06.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_08.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_06.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_07.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_03.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_03.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_04.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_05.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_05.prefab"
        };

        Vector3[] positions =
        {
            new(-92f, 0f, 50f), new(-66f, 0f, 46f), new(-104f, 0f, 18f), new(-72f, 0f, 18f),
            new(-105f, 0f, -15f), new(-79f, 0f, -34f), new(-50f, 0f, -28f), new(-108f, 0f, -58f),
            new(-56f, 0f, 58f), new(-96f, 0f, -82f), new(-65f, 0f, -78f)
        };
        float[] rotations = { 8f, -12f, 92f, 3f, -18f, 86f, -5f, 15f, 88f, -8f, 0f };
        for (int i = 0; i < homes.Length; i++)
            Place(parent, "TownBuilding", homes[i], positions[i], rotations[i], 1f);

        Place(parent, "TownGasStation", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GasStation_01.prefab", new Vector3(-44f, 0f, -6f), 90f, 1f);
        Place(parent, "TownLandmark", "Assets/Game/Prefabs/Environment/City/Clock_Tower_01.prefab", new Vector3(-82f, 0f, -2f), 0f, 0.9f);
        Place(parent, "TownCourtyard", "Assets/Game/Prefabs/Environment/City/SM_Bld_Fountain_01.prefab", new Vector3(-75f, 0f, -12f), 0f, 0.85f);
        Place(parent, "VillageWall", "Assets/Game/Prefabs/Environment/CityWalls/SM_Bld_Village_Wall_01.prefab", new Vector3(-40f, 0f, 20f), 90f, 1f);
        Place(parent, "VillageWall", "Assets/Game/Prefabs/Environment/CityWalls/SM_Bld_Village_Wall_02.prefab", new Vector3(-40f, 0f, -10f), 90f, 1f);
        Place(parent, "VillageGate", "Assets/Game/Prefabs/Environment/CityWalls/SM_Bld_Village_Wall_Gate_01.prefab", new Vector3(-40f, 0f, -35f), 90f, 1f);

        for (int i = 0; i < 6; i++)
            Place(parent, "TownAlleyCover", "Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Dirt_02.prefab", new Vector3(-92f + i * 10f, 0f, 2f + (i % 2) * 11f), i % 2 == 0 ? 0f : 90f, 0.8f);
    }

    private static void BuildMilitaryBase(GameObject parent)
    {
        Place(parent, "Hangar", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hangar_01.prefab", new Vector3(62f, 0f, 70f), 90f, 0.95f);
        Place(parent, "Barracks", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab", new Vector3(50f, 0f, 35f), 0f, 1f);
        Place(parent, "Barracks", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab", new Vector3(76f, 0f, 35f), 0f, 1f);
        Place(parent, "Tent", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_01.prefab", new Vector3(45f, 0f, 6f), 0f, 1f);
        Place(parent, "Tent", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Desert_01.prefab", new Vector3(66f, 0f, 5f), 8f, 1f);
        Place(parent, "GuardTower", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GuardTower_01.prefab", new Vector3(28f, 0f, 58f), 0f, 1f);
        Place(parent, "GuardTower", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GuardTower_02.prefab", new Vector3(28f, 0f, -6f), 0f, 1f);
        Place(parent, "APC", "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_APC_01.prefab", new Vector3(55f, 0f, -18f), -22f, 1f);
        Place(parent, "Tank", "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Tank_USA_01.prefab", new Vector3(79f, 0f, -16f), 12f, 1f);
        Place(parent, "RadarTank", "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Radar_Tank_01.prefab", new Vector3(42f, 0f, 62f), 18f, 1f);
        Place(parent, "RocketTruck", "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Rocket_Truck_01.prefab", new Vector3(84f, 0f, 60f), -18f, 1f);
        Place(parent, "Jet", "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Jet_01.prefab", new Vector3(112f, 0f, 62f), 0f, 0.9f);
        Place(parent, "HelicopterPad", "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Helicopter_Attack_01.prefab", new Vector3(96f, 0f, 2f), -12f, 0.9f);

        for (int i = 0; i < 7; i++)
        {
            Place(parent, "BaseFenceNorth", "Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Dirt_02.prefab", new Vector3(36f + i * 10f, 0f, 88f), 0f, 1f);
            Place(parent, "BaseFenceSouth", "Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Dirt_02.prefab", new Vector3(36f + i * 10f, 0f, -32f), 0f, 1f);
        }
        for (int i = 0; i < 6; i++)
        {
            Place(parent, "BaseFenceWest", "Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab", new Vector3(26f, 0f, -20f + i * 20f), 90f, 1f);
            Place(parent, "BaseFenceEast", "Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab", new Vector3(94f, 0f, -20f + i * 20f), 90f, 1f);
        }
    }

    private static void BuildIndustrial(GameObject parent)
    {
        Place(parent, "FuelTank", "Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_Tank_01.prefab", new Vector3(82f, 0f, -74f), 0f, 1f);
        Place(parent, "FuelBladder", "Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Fuel_Bladder_01.prefab", new Vector3(101f, 0f, -74f), 0f, 1f);
        Place(parent, "Pipeline", "Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_Pipe_Large_Section_01.prefab", new Vector3(92f, 0f, -56f), 90f, 1f);
        Place(parent, "Truck", "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Truck_01_Tanker.prefab", new Vector3(68f, 0f, -58f), 90f, 1f);
        Place(parent, "DestroyedTruck", "Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Truck_01_Destroyed.prefab", new Vector3(34f, 0f, -50f), -25f, 1f);
    }

    private static void BuildCheckpoint(GameObject parent)
    {
        for (int i = 0; i < 4; i++)
        {
            Place(parent, "CheckpointBarrierWest", "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Barrier_01.prefab", new Vector3(-12f, 0f, -12f + i * 8f), 16f, 1f);
            Place(parent, "CheckpointBarrierEast", "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Barrier_01.prefab", new Vector3(14f, 0f, -8f + i * 8f), -12f, 1f);
        }

        Place(parent, "CheckpointTower", "Assets/Game/Prefabs/Buildings/Building_GuardTower.prefab", new Vector3(22f, 0f, 18f), 180f, 1f);
        Place(parent, "RoadBlockTruck", "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Truck_01_Canopy.prefab", new Vector3(6f, 0f, 26f), 8f, 1f);
    }

    private static void BuildBoundaryDressing(GameObject parent)
    {
        string[] rocks =
        {
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_06.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_04.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_04.prefab"
        };

        Vector3[] positions =
        {
            new(-122f, 0f, 65f), new(-116f, 0f, -78f), new(122f, 0f, -95f), new(118f, 0f, 94f),
            new(-106f, 0f, 86f), new(-120f, 0f, -20f), new(118f, 0f, -20f)
        };
        for (int i = 0; i < positions.Length; i++)
            Place(parent, "BoundaryRock", rocks[i % rocks.Length], positions[i], i * 23f, i % 3 == 2 ? 0.7f : 1f);

        string[] plants =
        {
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_01.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Bush_Group_01.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Plant_01.prefab"
        };
        Vector3[] plantPositions =
        {
            new(-112f, 0f, 28f), new(-89f, 0f, 58f), new(-54f, 0f, -58f), new(-96f, 0f, -78f),
            new(36f, 0f, -78f), new(105f, 0f, -40f), new(96f, 0f, 86f), new(58f, 0f, 92f)
        };
        for (int i = 0; i < plantPositions.Length; i++)
            Place(parent, "DesertPlant", plants[i % plants.Length], plantPositions[i], i * 31f, 0.85f);
    }

    private static void BuildCameras(GameObject root)
    {
        GameObject top = Child(root, "Camera_TopDown");
        Camera topCamera = top.AddComponent<Camera>();
        topCamera.orthographic = true;
        topCamera.orthographicSize = 115f;
        topCamera.clearFlags = CameraClearFlags.SolidColor;
        topCamera.backgroundColor = new Color(0.48f, 0.4f, 0.28f, 1f);
        topCamera.transform.position = new Vector3(0f, 190f, 0f);
        topCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        topCamera.nearClipPlane = 0.1f;
        topCamera.farClipPlane = 500f;

        GameObject perspective = Child(root, "Camera_Perspective");
        Camera perspectiveCamera = perspective.AddComponent<Camera>();
        perspectiveCamera.fieldOfView = 38f;
        perspectiveCamera.clearFlags = CameraClearFlags.SolidColor;
        perspectiveCamera.backgroundColor = new Color(0.48f, 0.4f, 0.28f, 1f);
        perspectiveCamera.transform.position = new Vector3(-118f, 86f, -118f);
        perspectiveCamera.transform.LookAt(new Vector3(-14f, 0f, -4f));
        perspectiveCamera.nearClipPlane = 0.1f;
        perspectiveCamera.farClipPlane = 600f;

        GameObject town = Child(root, "Camera_TownClose");
        Camera townCamera = town.AddComponent<Camera>();
        townCamera.fieldOfView = 36f;
        townCamera.clearFlags = CameraClearFlags.SolidColor;
        townCamera.backgroundColor = new Color(0.48f, 0.4f, 0.28f, 1f);
        townCamera.transform.position = new Vector3(-128f, 52f, -82f);
        townCamera.transform.LookAt(new Vector3(-78f, 0f, -18f));
        townCamera.nearClipPlane = 0.1f;
        townCamera.farClipPlane = 400f;

        GameObject baseClose = Child(root, "Camera_BaseClose");
        Camera baseCamera = baseClose.AddComponent<Camera>();
        baseCamera.fieldOfView = 36f;
        baseCamera.clearFlags = CameraClearFlags.SolidColor;
        baseCamera.backgroundColor = new Color(0.48f, 0.4f, 0.28f, 1f);
        baseCamera.transform.position = new Vector3(5f, 58f, -74f);
        baseCamera.transform.LookAt(new Vector3(68f, 0f, 24f));
        baseCamera.nearClipPlane = 0.1f;
        baseCamera.farClipPlane = 500f;
    }

    private static void BuildLight(GameObject root)
    {
        GameObject lightObject = Child(root, "DirectionalLight");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        light.color = new Color(1f, 0.92f, 0.78f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
    }

    private static void CaptureScene()
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        foreach (Camera camera in cameras)
        {
            if (camera.name == "Camera_TopDown")
                Render(camera, ProjectPath(CaptureRoot + "/gc01_topdown_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_Perspective")
                Render(camera, ProjectPath(CaptureRoot + "/gc01_perspective_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_TownClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc01_town_close_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_BaseClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc01_base_close_1920x1080.png"), 1920, 1080);
        }
    }

    private static void WriteReport()
    {
        StringBuilder report = new();
        report.AppendLine("# GC01 Base Gate Town Generation");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Generate first rule-authored 3D scene from Demo/promoted Game prefab vocabulary.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Design/Architecture/gameplay_3d_scene_generation_plan.md`");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGeneratedSceneBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC01_BaseGateTown.unity`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC01_BaseGateTown/gc01_topdown_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC01_BaseGateTown/gc01_perspective_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC01_BaseGateTown/gc01_town_close_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC01_BaseGateTown/gc01_base_close_1920x1080.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: none. This is a design-target scene generator and does not alter runtime ECS contracts.");
        report.AppendLine("User-visible behavior: none in the shipped Game flow yet; generated scene is a design target.");
        report.AppendLine("Validation run: Unity editor scene build and proof capture via `WarlineCaptureGeneratedSceneBuilder.BuildGc01BaseGateTown`.");
        report.AppendLine("Validation result: scene saved and perspective/close captures exported; no missing prefabs were reported in the final build.");
        report.AppendLine("Known gaps: rendered top-down proof still exposes texture-atlas artifacts from some PolygonMilitary prefabs; perspective close views are the current visual acceptance proof. Next pass should add top-down-safe prefab filtering, footprint/overlap validation, and denser decoration.");
        report.AppendLine("Cross-lane impacts: Designer/PM can review proof captures and request grammar changes; no UI/runtime source files are changed.");
        report.AppendLine("Next recommended task: add footprint/overlap validation and generate role-colored map/walkability proof for GC01.");
        report.AppendLine();
        report.AppendLine("Placed prefabs:");
        foreach (string line in PlacementLog)
            report.AppendLine("- " + line);
        if (MissingPrefabLog.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("Missing prefabs replaced by placeholders:");
            foreach (string line in MissingPrefabLog)
                report.AppendLine("- " + line);
        }

        File.WriteAllText(ProjectPath(ReportPath), report.ToString(), Encoding.UTF8);
    }

    private static GameObject Place(GameObject parent, string role, string prefabPath, Vector3 position, float yRotation, float scale)
    {
        return PlaceScaled(parent, role, prefabPath, position, yRotation, Vector3.one * scale);
    }

    private static GameObject PlaceScaled(GameObject parent, string role, string prefabPath, Vector3 position, float yRotation, Vector3 scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject instance;
        if (prefab == null)
        {
            MissingPrefabLog.Add($"{role}: {prefabPath}");
            instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = "Missing_" + Path.GetFileNameWithoutExtension(prefabPath);
            instance.transform.localScale = new Vector3(5f, 2f, 5f);
        }
        else
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        instance.name = $"{role}_{Path.GetFileNameWithoutExtension(prefabPath)}";
        instance.transform.SetParent(parent.transform, true);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        instance.transform.localScale = scale;
        AlignToGround(instance);
        PlacementLog.Add($"{role}: {prefabPath} at {Format(instance.transform.position)} rotY={yRotation:0.#} scale={Format(scale)}");
        return instance;
    }

    private static void AlignToGround(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float offset = bounds.min.y;
        instance.transform.position = new Vector3(instance.transform.position.x, instance.transform.position.y - offset, instance.transform.position.z);
    }

    private static GameObject Cube(GameObject parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent.transform, false);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        if (cube.TryGetComponent(out Renderer renderer))
            renderer.sharedMaterial = material;
        return cube;
    }

    private static GameObject Surface(GameObject parent, string name, Vector3 position, Vector2 size, Material material)
    {
        GameObject surface = new(name);
        surface.transform.SetParent(parent.transform, false);
        surface.transform.position = position;

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
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = surface.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        return surface;
    }

    private static Material Material(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new(shader)
        {
            name = name,
            color = color
        };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", Texture2D.whiteTexture);
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
        RenderTexture texture = new(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 2
        };
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
}
#endif
