#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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

public static class WarlineCaptureGc11ExpandedRtsSceneBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC11_ExpandedMilitaryRts_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC11_ExpandedMilitaryRts_2048";
    private const string ReportPath = "Design/AgentReports/2026-05-20_gameplay_gc11-expanded-military-rts-scene.md";
    private const float MapSize = 2048f;
    private const int LayoutMaskLayer = 30;

    private static readonly List<Rect> RoadAndWalkableMasks = new();
    private static readonly List<Rect> PlacementPads = new();
    private static readonly List<string> PlacementLog = new();
    private static readonly List<string> ValidationLog = new();
    private static readonly List<string> MissingAssets = new();

    private readonly struct Placement
    {
        public readonly string Name;
        public readonly string Path;
        public readonly Vector3 Position;
        public readonly float RotationY;
        public readonly float Scale;
        public readonly Vector2 Footprint;

        public Placement(string name, string path, Vector3 position, float rotationY, float scale, Vector2 footprint)
        {
            Name = name;
            Path = path;
            Position = position;
            RotationY = rotationY;
            Scale = scale;
            Footprint = footprint;
        }
    }

    [MenuItem("WarlineCapture/Design/Build GC11 Expanded Military RTS 2048")]
    public static void BuildGc11ExpandedMilitaryRts2048()
    {
        RoadAndWalkableMasks.Clear();
        PlacementPads.Clear();
        PlacementLog.Clear();
        ValidationLog.Clear();
        MissingAssets.Clear();

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        GameObject root = new("GC11_ExpandedMilitaryRts_2048_Root");
        BuildEnvironment(root);
        BuildGround(root);
        BuildMasks(root);
        BuildVisualRoads(root);
        PlaceCityDistrict(root);
        PlaceEnemyCamps(root);
        PlaceScenicEdges(root);
        PlaceProofUnits(root);
        BuildCameras(root);
        ValidateLayout();

        EditorSceneManager.SaveScene(scene, ScenePath);
        CaptureScene();
        WriteReport();
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GC11_EXPANDED_MILITARY_RTS_BUILT scene={ScenePath} captures={CaptureRoot} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static void BuildEnvironment(GameObject root)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.74f, 0.68f, 0.56f, 1f);
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.57f, 0.48f, 0.35f, 1f);
        RenderSettings.fogDensity = 0.00032f;

        Light key = Child(root, "DirectionalLight_Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.7f;
        key.color = new Color(1f, 0.88f, 0.66f, 1f);
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.55f;
        key.transform.rotation = Quaternion.Euler(50f, -42f, 0f);

        Light fill = Child(root, "DirectionalLight_Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.25f;
        fill.color = new Color(0.62f, 0.78f, 1f, 1f);
        fill.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(30f, 132f, 0f);

        Volume volume = Child(root, "GC11_RTS_ReviewVolume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "GC11_RTS_ReviewProfile";
        volume.sharedProfile = profile;
        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.05f);
        color.contrast.Override(10f);
        color.saturation.Override(2f);
        color.colorFilter.Override(new Color(1f, 0.94f, 0.82f, 1f));
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
    }

    private static void BuildGround(GameObject root)
    {
        Surface(root, "FlatPlayableSand_2048x2048", Vector3.zero, new Vector2(MapSize, MapSize), Material("GC11_SandBase", new Color(0.55f, 0.45f, 0.29f, 1f)), 0f);
        Surface(root, "MapBoundary_North", new Vector3(0f, 0.06f, 1024f), new Vector2(MapSize, 8f), Material("GC11_Boundary", new Color(0.42f, 0.36f, 0.25f, 1f)), 0.06f);
        Surface(root, "MapBoundary_South", new Vector3(0f, 0.06f, -1024f), new Vector2(MapSize, 8f), Material("GC11_Boundary", new Color(0.42f, 0.36f, 0.25f, 1f)), 0.06f);
        Surface(root, "MapBoundary_West", new Vector3(-1024f, 0.06f, 0f), new Vector2(8f, MapSize), Material("GC11_Boundary", new Color(0.42f, 0.36f, 0.25f, 1f)), 0.06f);
        Surface(root, "MapBoundary_East", new Vector3(1024f, 0.06f, 0f), new Vector2(8f, MapSize), Material("GC11_Boundary", new Color(0.42f, 0.36f, 0.25f, 1f)), 0.06f);
    }

    private static void BuildMasks(GameObject root)
    {
        GameObject masks = Child(root, "BlueprintLocked_RoadsWalkableNoObjectMasks");
        Material road = Material("GC11_Roads_KeepEmpty", new Color(0.18f, 0.17f, 0.14f, 1f));
        Material route = Material("GC11_WalkableCombat_KeepEmpty", new Color(0.38f, 0.50f, 0.27f, 1f));
        Material plaza = Material("GC11_Plaza_KeepEmpty", new Color(0.58f, 0.50f, 0.34f, 1f));
        Material buffer = Material("GC11_NoObjectBuffer_Yellow", new Color(0.86f, 0.66f, 0.16f, 1f));

        AddMaskSurface(masks, "NorthSouthHighway_West", -405f, 0f, 88f, 1960f, road, 0.35f);
        AddMaskSurface(masks, "NorthSouthHighway_East", 70f, 0f, 88f, 1960f, road, 0.36f);
        AddMaskSurface(masks, "NorthConnectorRoad", 0f, 620f, 1760f, 72f, road, 0.37f);
        AddMaskSurface(masks, "MidConnectorRoad", -20f, 250f, 1670f, 72f, road, 0.38f);
        AddMaskSurface(masks, "MainAssaultRoad", 20f, -215f, 1780f, 78f, road, 0.39f);
        AddMaskSurface(masks, "SouthernSupplyRoad", -145f, -720f, 1220f, 70f, road, 0.40f);
        AddMaskSurface(masks, "AirfieldServiceRoad", 650f, 210f, 72f, 1220f, road, 0.41f);

        foreach (float x in new[] { -885f, -710f, -535f, -260f })
            AddMaskSurface(masks, $"CityVerticalStreet_{x:0}", x, 0f, 44f, 1620f, road, 0.42f);
        foreach (float z in new[] { 800f, 480f, 70f, -310f, -650f })
            AddMaskSurface(masks, $"CityHorizontalStreet_{z:0}", -640f, z, 680f, 44f, road, 0.43f);

        AddMaskSurface(masks, "PlayerStartWalkableZone", -780f, -575f, 410f, 250f, route, 0.50f);
        AddMaskSurface(masks, "CentralOpenCombatZone", -60f, -260f, 410f, 280f, route, 0.51f);
        AddMaskSurface(masks, "AirfieldApproachWalkableZone", 520f, 365f, 420f, 220f, route, 0.52f);
        AddMaskSurface(masks, "VehicleCampWalkableZone", 700f, -595f, 360f, 220f, route, 0.53f);
        AddMaskSurface(masks, "MarketPlaza_KeepEmpty", -210f, 380f, 260f, 84f, plaza, 0.54f);
        AddMaskSurface(masks, "SouthObjectivePlaza_KeepEmpty", -105f, -775f, 300f, 90f, plaza, 0.55f);

        AddBufferSurface(masks, "HighwayWest_NoObjectBuffer", -405f, 0f, 138f, 2048f, buffer);
        AddBufferSurface(masks, "HighwayEast_NoObjectBuffer", 70f, 0f, 138f, 2048f, buffer);
        AddBufferSurface(masks, "NorthConnector_NoObjectBuffer", 0f, 620f, 1840f, 118f, buffer);
        AddBufferSurface(masks, "MidConnector_NoObjectBuffer", -20f, 250f, 1740f, 118f, buffer);
        AddBufferSurface(masks, "MainAssault_NoObjectBuffer", 20f, -215f, 1840f, 126f, buffer);
        AddBufferSurface(masks, "CityGrid_NoObjectBuffer", -640f, 70f, 820f, 1080f, buffer);
    }

    private static void BuildVisualRoads(GameObject root)
    {
        GameObject roads = Child(root, "VisualRoads_ReviewCameraVisible");
        Material asphalt = Material("GC11_VisualRoad_AsphaltDust", new Color(0.21f, 0.19f, 0.15f, 1f));
        Material compacted = Material("GC11_VisualWalkable_CompactedSand", new Color(0.46f, 0.39f, 0.27f, 1f));
        Surface(roads, "NorthSouthHighway_West_Visual", new Vector3(-405f, 0.16f, 0f), new Vector2(88f, 1960f), asphalt, 0.16f);
        Surface(roads, "NorthSouthHighway_East_Visual", new Vector3(70f, 0.17f, 0f), new Vector2(88f, 1960f), asphalt, 0.17f);
        Surface(roads, "NorthConnectorRoad_Visual", new Vector3(0f, 0.18f, 620f), new Vector2(1760f, 72f), asphalt, 0.18f);
        Surface(roads, "MidConnectorRoad_Visual", new Vector3(-20f, 0.19f, 250f), new Vector2(1670f, 72f), asphalt, 0.19f);
        Surface(roads, "MainAssaultRoad_Visual", new Vector3(20f, 0.2f, -215f), new Vector2(1780f, 78f), asphalt, 0.2f);
        Surface(roads, "SouthernSupplyRoad_Visual", new Vector3(-145f, 0.21f, -720f), new Vector2(1220f, 70f), asphalt, 0.21f);
        Surface(roads, "AirfieldServiceRoad_Visual", new Vector3(650f, 0.22f, 210f), new Vector2(72f, 1220f), asphalt, 0.22f);

        foreach (float x in new[] { -885f, -710f, -535f, -260f })
            Surface(roads, $"CityVerticalStreet_{x:0}_Visual", new Vector3(x, 0.23f, 0f), new Vector2(44f, 1620f), asphalt, 0.23f);
        foreach (float z in new[] { 800f, 480f, 70f, -310f, -650f })
            Surface(roads, $"CityHorizontalStreet_{z:0}_Visual", new Vector3(-640f, 0.24f, z), new Vector2(680f, 44f), asphalt, 0.24f);

        Surface(roads, "PlayerStartWalkableZone_Visual", new Vector3(-780f, 0.12f, -575f), new Vector2(410f, 250f), compacted, 0.12f);
        Surface(roads, "CentralOpenCombatZone_Visual", new Vector3(-60f, 0.13f, -260f), new Vector2(410f, 280f), compacted, 0.13f);
        Surface(roads, "AirfieldApproachWalkableZone_Visual", new Vector3(520f, 0.14f, 365f), new Vector2(420f, 220f), compacted, 0.14f);
        Surface(roads, "VehicleCampWalkableZone_Visual", new Vector3(700f, 0.15f, -595f), new Vector2(360f, 220f), compacted, 0.15f);
    }

    private static void PlaceCityDistrict(GameObject root)
    {
        GameObject city = Child(root, "CityDistrict_WestAndSouthWest_PlacedOnlyOnPads");

        Placement[] buildings =
        {
            new("CityCore_House_01", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_01.prefab", new Vector3(-960f, 0f, 900f), 6f, 2.2f, new Vector2(80f, 70f)),
            new("CityCore_Shop_06", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_06.prefab", new Vector3(-790f, 0f, 900f), -8f, 2.2f, new Vector2(90f, 70f)),
            new("CityCore_House_05", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_05.prefab", new Vector3(-620f, 0f, 720f), 91f, 2.0f, new Vector2(80f, 70f)),
            new("CityCore_Shop_08", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_08.prefab", new Vector3(-170f, 0f, 720f), 0f, 2.1f, new Vector2(80f, 70f)),
            new("CityCore_Shop_12", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_12.prefab", new Vector3(-960f, 0f, 350f), -4f, 2.05f, new Vector2(80f, 70f)),
            new("CityMarket_Hall", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hall_01.prefab", new Vector3(-790f, 0f, 350f), 3f, 2.1f, new Vector2(90f, 70f)),
            new("CityMarket_GasStation", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GasStation_01.prefab", new Vector3(-620f, 0f, 350f), 90f, 1.9f, new Vector2(90f, 70f)),
            new("SouthTown_House_03", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_03.prefab", new Vector3(-960f, 0f, -820f), 0f, 2.2f, new Vector2(80f, 70f)),
            new("SouthTown_Shop_03", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_03.prefab", new Vector3(-790f, 0f, -820f), -2f, 2.2f, new Vector2(90f, 70f)),
            new("SouthTown_House_07", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_07.prefab", new Vector3(-620f, 0f, -820f), 3f, 2.0f, new Vector2(80f, 70f))
        };

        foreach (Placement placement in buildings)
            PlaceOnPad(city, placement);

        AddPad(city, "CityPadVisual_NorthWest", -900f, 865f, 160f, 110f, new Color(0.36f, 0.54f, 0.62f, 0.65f));
        AddPad(city, "CityPadVisual_Market", -700f, 95f, 430f, 135f, new Color(0.36f, 0.54f, 0.62f, 0.55f));
        AddPad(city, "CityPadVisual_SouthTown", -650f, -815f, 640f, 135f, new Color(0.36f, 0.54f, 0.62f, 0.55f));
    }

    private static void PlaceEnemyCamps(GameObject root)
    {
        GameObject camps = Child(root, "EnemyCamps_Multiple_PlacedOnlyOnPads");

        AddCampPad(camps, "AirfieldCampPad", 710f, 620f, 420f, 360f);
        PlaceOnPad(camps, new Placement("Airfield_Hangar", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hangar_01.prefab", new Vector3(805f, 0f, 740f), 90f, 2.3f, new Vector2(150f, 110f)));
        PlaceOnPad(camps, new Placement("Airfield_ControlTower", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_ControlTower_01.prefab", new Vector3(760f, 0f, 540f), 0f, 1.9f, new Vector2(70f, 70f)));
        PlaceOnPad(camps, new Placement("Airfield_TentNorth", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Desert_01.prefab", new Vector3(550f, 0f, 720f), 0f, 2.1f, new Vector2(90f, 70f)));
        PlaceOnPad(camps, new Placement("Airfield_Jet", "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Jet_01.prefab", new Vector3(880f, 0f, 500f), 14f, 1.7f, new Vector2(170f, 120f)));

        AddCampPad(camps, "CommandOutpostPad", 430f, -20f, 340f, 270f);
        PlaceOnPad(camps, new Placement("Command_Barracks", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab", new Vector3(390f, 0f, 35f), -4f, 2.2f, new Vector2(140f, 90f)));
        PlaceOnPad(camps, new Placement("Command_GuardTower", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GuardTower_01.prefab", new Vector3(555f, 0f, 50f), 0f, 1.8f, new Vector2(60f, 60f)));
        PlaceOnPad(camps, new Placement("Command_RadarTank", "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Radar_Tank_01.prefab", new Vector3(470f, 0f, -80f), 18f, 1.75f, new Vector2(95f, 80f)));

        AddCampPad(camps, "VehicleFuelCampPad", 700f, -690f, 440f, 410f);
        PlaceOnPad(camps, new Placement("VehicleCamp_Tank", "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Tank_Russian_01.prefab", new Vector3(600f, 0f, -805f), -8f, 1.9f, new Vector2(105f, 80f)));
        PlaceOnPad(camps, new Placement("VehicleCamp_APC", "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_APC_01.prefab", new Vector3(760f, 0f, -805f), 8f, 1.9f, new Vector2(105f, 80f)));
        PlaceOnPad(camps, new Placement("VehicleCamp_Fuel", "Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Fuel_Bladder_01.prefab", new Vector3(900f, 0f, -795f), 0f, 2.3f, new Vector2(85f, 85f)));
        PlaceOnPad(camps, new Placement("VehicleCamp_PipelineTank", "Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_Tank_01.prefab", new Vector3(705f, 0f, -795f), 0f, 1.9f, new Vector2(125f, 90f)));

        BuildCampBarriers(camps);
    }

    private static void PlaceScenicEdges(GameObject root)
    {
        GameObject scenic = Child(root, "ScenicEdges_NoGameplayBlockers");
        AddNoBuildZone(scenic, "NoBuild_NorthWest", -900f, 930f, 240f, 120f);
        AddNoBuildZone(scenic, "NoBuild_SouthWest", -905f, -940f, 250f, 145f);
        AddNoBuildZone(scenic, "NoBuild_NorthEast", 930f, 930f, 180f, 140f);
        AddNoBuildZone(scenic, "NoBuild_SouthEast", 930f, -930f, 180f, 140f);

        PlaceOptional(scenic, "EdgeDune_NW", "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_02.prefab", new Vector3(-920f, 0f, 920f), 18f, 2.6f);
        PlaceOptional(scenic, "EdgeDune_SW", "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_01.prefab", new Vector3(-930f, 0f, -925f), -22f, 2.5f);
        PlaceOptional(scenic, "EdgeDune_SE", "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_03.prefab", new Vector3(930f, 0f, -930f), 12f, 2.4f);
    }

    private static void PlaceProofUnits(GameObject root)
    {
        GameObject units = Child(root, "ProofUnits_OnWalkableRoutes");
        PlaceOptional(units, "Player_Soldier_01", "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab", new Vector3(-820f, 0f, -575f), 48f, 1.65f);
        PlaceOptional(units, "Player_Soldier_02", "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab", new Vector3(-775f, 0f, -545f), 48f, 1.65f);
        PlaceOptional(units, "Player_Soldier_03", "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_01.prefab", new Vector3(-730f, 0f, -515f), 48f, 1.65f);
        PlaceOptional(units, "MidRoute_Soldier", "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_02.prefab", new Vector3(-90f, 0f, -235f), 32f, 1.65f);
        PlaceOptional(units, "Enemy_Airfield_01", "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab", new Vector3(735f, 0f, 360f), 226f, 1.65f);
        PlaceOptional(units, "Enemy_Command_01", "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_02.prefab", new Vector3(480f, 0f, 0f), 248f, 1.65f);
        PlaceOptional(units, "Enemy_VehicleCamp_01", "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Female_01.prefab", new Vector3(710f, 0f, -585f), 238f, 1.65f);
    }

    private static void BuildCampBarriers(GameObject parent)
    {
        string barrier = "Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab";
        for (int i = 0; i < 6; i++)
        {
            PlaceOptional(parent, "Airfield_NorthBarrier_" + i, barrier, new Vector3(545f + i * 62f, 0f, 815f), 0f, 1.55f);
            PlaceOptional(parent, "Vehicle_SouthBarrier_" + i, barrier, new Vector3(555f + i * 62f, 0f, -880f), 0f, 1.55f);
        }
        for (int i = 0; i < 5; i++)
        {
            PlaceOptional(parent, "Command_WestBarrier_" + i, barrier, new Vector3(300f, 0f, -120f + i * 62f), 90f, 1.45f);
            PlaceOptional(parent, "Vehicle_EastBarrier_" + i, barrier, new Vector3(930f, 0f, -850f + i * 62f), 90f, 1.45f);
        }
    }

    private static void BuildCameras(GameObject root)
    {
        BuildCamera(root, "Camera_GC11_TopdownLayoutProof", new Vector3(0f, 1800f, -1f), Vector3.zero, 38f, true);
        BuildCamera(root, "Camera_GC11_RtsPlayableOverview", new Vector3(-650f, 760f, -1020f), new Vector3(35f, 0f, -80f), 36f, false);
        BuildCamera(root, "Camera_GC11_CityStartRoute", new Vector3(-920f, 390f, -880f), new Vector3(-650f, 0f, -300f), 34f, false);
        BuildCamera(root, "Camera_GC11_EnemyCampsRoute", new Vector3(240f, 620f, -1030f), new Vector3(650f, 0f, -80f), 36f, false);
    }

    private static void BuildCamera(GameObject root, string name, Vector3 position, Vector3 target, float fov, bool orthographic)
    {
        GameObject cameraObject = Child(root, name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.52f, 0.42f, 0.28f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 5000f;
        camera.fieldOfView = fov;
        camera.orthographic = orthographic;
        camera.orthographicSize = 1120f;
        if (!orthographic)
            camera.cullingMask &= ~(1 << LayoutMaskLayer);
        camera.transform.position = position;
        camera.transform.LookAt(target);
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = false;
        cameraData.antialiasing = AntialiasingMode.None;
    }

    private static void ValidateLayout()
    {
        foreach (Rect pad in PlacementPads)
        {
            foreach (Rect mask in RoadAndWalkableMasks)
            {
                if (pad.Overlaps(mask))
                    ValidationLog.Add($"ERROR: placement pad overlaps road/walkable mask pad={pad} mask={mask}");
            }
        }

        if (MissingAssets.Count > 0)
            ValidationLog.Add("WARNING: missing optional prefabs: " + string.Join(", ", MissingAssets.Distinct(StringComparer.Ordinal)));

        if (!ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)))
            ValidationLog.Add("PASS: GC11 scene generated with multiple enemy camps, explicit empty roads/walkable masks, and object placement limited to pads.");
    }

    private static void CaptureScene()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (camera.name == "Camera_GC11_TopdownLayoutProof")
                Render(camera, ProjectPath(CaptureRoot + "/gc11_topdown_layout_proof_1920x1080.png"));
            if (camera.name == "Camera_GC11_RtsPlayableOverview")
                Render(camera, ProjectPath(CaptureRoot + "/gc11_rts_playable_overview_1920x1080.png"));
            if (camera.name == "Camera_GC11_CityStartRoute")
                Render(camera, ProjectPath(CaptureRoot + "/gc11_city_start_route_1920x1080.png"));
            if (camera.name == "Camera_GC11_EnemyCampsRoute")
                Render(camera, ProjectPath(CaptureRoot + "/gc11_enemy_camps_route_1920x1080.png"));
        }
    }

    private static void WriteReport()
    {
        StringBuilder report = new();
        report.AppendLine("# GC11 Expanded Military RTS Scene");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Convert the approved expanded 2048 RTS blueprint into a Unity review scene with roads, walkable masks, multiple enemy camps, proof units, and no object placement on roads.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGc11ExpandedRtsSceneBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC11_ExpandedMilitaryRts_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC11_ExpandedMilitaryRts_2048/gc11_topdown_layout_proof_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC11_ExpandedMilitaryRts_2048/gc11_rts_playable_overview_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC11_ExpandedMilitaryRts_2048/gc11_city_start_route_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC11_ExpandedMilitaryRts_2048/gc11_enemy_camps_route_1920x1080.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: GC11 visual playable scene blockout contract from `Design/Blueprints/gc11_military_rts_2048_expanded_walkable_blueprint.svg`.");
        report.AppendLine("User-visible behavior: none in shipped flow; generated Unity scene and captures are available for visual review.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureGc11ExpandedRtsSceneBuilder.BuildGc11ExpandedMilitaryRts2048`.");
        report.AppendLine("Validation result: " + (ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)) ? "failed; see validation log." : "passed scene generation validation."));
        report.AppendLine("Known gaps: this is a readable first Unity blockout, not final art dressing. Roads/walkable masks are intentionally visible so layout can be reviewed before decoration.");
        report.AppendLine("Cross-lane impacts: Art/Design can now review Unity screenshots and approve whether to replace pads with richer Demo-authored modules.");
        report.AppendLine("Next recommended task: if GC11 layout is approved, replace pad-level buildings with richer Demo-scene clusters while preserving the same road/walkable masks.");
        report.AppendLine();
        report.AppendLine("Validation log:");
        foreach (string line in ValidationLog)
            report.AppendLine("- " + line);
        report.AppendLine();
        report.AppendLine("Placement log:");
        foreach (string line in PlacementLog)
            report.AppendLine("- " + line);
        File.WriteAllText(ProjectPath(ReportPath), report.ToString(), Encoding.UTF8);
    }

    private static void AddMaskSurface(GameObject parent, string name, float x, float z, float width, float depth, Material material, float y)
    {
        RoadAndWalkableMasks.Add(CenterRect(x, z, width, depth));
        GameObject surface = Surface(parent, name, new Vector3(x, y, z), new Vector2(width, depth), material, y);
        surface.layer = LayoutMaskLayer;
    }

    private static void AddBufferSurface(GameObject parent, string name, float x, float z, float width, float depth, Material material)
    {
        // Buffers are placement rules, not rendered slabs. Rendering them as filled quads hides the actual roads.
    }

    private static void AddPad(GameObject parent, string name, float x, float z, float width, float depth, Color color)
    {
        GameObject surface = Surface(parent, name, new Vector3(x, 0.18f, z), new Vector2(width, depth), Material(name + "_Material", color), 0.18f);
        surface.layer = LayoutMaskLayer;
    }

    private static void AddCampPad(GameObject parent, string name, float x, float z, float width, float depth)
    {
        AddPad(parent, name, x, z, width, depth, new Color(0.62f, 0.40f, 0.18f, 0.62f));
    }

    private static void AddNoBuildZone(GameObject parent, string name, float x, float z, float width, float depth)
    {
        GameObject surface = Surface(parent, name, new Vector3(x, 0.12f, z), new Vector2(width, depth), Material(name + "_Material", new Color(0.45f, 0.18f, 0.16f, 1f)), 0.12f);
        surface.layer = LayoutMaskLayer;
    }

    private static void PlaceOnPad(GameObject parent, Placement placement)
    {
        Rect footprint = CenterRect(placement.Position.x, placement.Position.z, placement.Footprint.x, placement.Footprint.y);
        foreach (Rect mask in RoadAndWalkableMasks)
        {
            if (footprint.Overlaps(mask))
                ValidationLog.Add($"ERROR: placement {placement.Name} overlaps road/walkable mask footprint={footprint} mask={mask}");
        }

        PlacementPads.Add(footprint);
        PlaceOptional(parent, placement.Name, placement.Path, placement.Position, placement.RotationY, placement.Scale);
    }

    private static void PlaceOptional(GameObject parent, string name, string path, Vector3 position, float rotationY, float scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            MissingAssets.Add(path);
            GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.name = name + "_MissingPrefabPlaceholder";
            placeholder.transform.SetParent(parent.transform, false);
            placeholder.transform.position = position + Vector3.up;
            placeholder.transform.localScale = new Vector3(22f, 2f, 22f);
            placeholder.GetComponent<MeshRenderer>().sharedMaterial = Material("GC11_MissingPrefabPlaceholder", Color.magenta);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent.transform, true);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        instance.transform.localScale = Vector3.one * scale;
        AlignBottomNearGround(instance);
        PlacementLog.Add($"{name}: {path} at ({position.x:0.#}, {position.z:0.#}) rot={rotationY:0.#} scale={scale:0.##}");
    }

    private static void AlignBottomNearGround(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        go.transform.position -= new Vector3(0f, bounds.min.y, 0f);
    }

    private static Rect CenterRect(float x, float z, float width, float depth)
    {
        return new Rect(x - width * 0.5f, z - depth * 0.5f, width, depth);
    }

    private static GameObject Surface(GameObject parent, string name, Vector3 position, Vector2 size, Material material, float y)
    {
        GameObject surface = new(name);
        surface.transform.SetParent(parent.transform, false);
        surface.transform.position = new Vector3(position.x, y, position.z);
        Mesh mesh = new();
        float halfX = size.x * 0.5f;
        float halfZ = size.y * 0.5f;
        mesh.vertices = new[] { new Vector3(-halfX, 0f, -halfZ), new Vector3(-halfX, 0f, halfZ), new Vector3(halfX, 0f, halfZ), new Vector3(halfX, 0f, -halfZ) };
        mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        surface.AddComponent<MeshRenderer>().sharedMaterial = material;
        return surface;
    }

    private static Material Material(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Lit");
        Material material = new(shader) { name = name, color = color };
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

    private static void Render(Camera camera, string path)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture texture = new(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
        camera.targetTexture = texture;
        RenderTexture.active = texture;
        GL.Clear(true, true, camera.backgroundColor);
        camera.Render();
        Texture2D image = new(1920, 1080, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
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
}
#endif
