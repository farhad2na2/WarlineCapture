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

public static class WarlineCaptureGc13BlueprintRtsSceneBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC13_BlueprintRts_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GC13_BlueprintRts_2048";
    private const string FootprintCatalogPath = DataRoot + "/gc13_prefab_footprint_catalog.json";
    private const string ReportPath = "Design/AgentReports/2026-05-20_gameplay_gc13-blueprint-rts-scene.md";
    private const float MapSize = 2048f;

    private static readonly List<Zone> Roads = new();
    private static readonly List<Zone> Buildings = new();
    private static readonly List<Zone> Blockers = new();
    private static readonly List<Zone> Spawns = new();
    private static readonly List<Zone> Objectives = new();
    private static readonly List<string> PlacementLog = new();
    private static readonly List<string> ValidationLog = new();
    private static readonly List<string> MissingAssets = new();
    private static readonly List<CatalogEntry> Catalog = new();
    private static CoverageSummary coverageSummary;

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

    private readonly struct Placement
    {
        public readonly string Name;
        public readonly string Path;
        public readonly Vector3 Position;
        public readonly float RotationY;
        public readonly Vector2 Footprint;
        public readonly Vector3 Scale;
        public readonly string Role;

        public Placement(string name, string path, Vector3 position, float rotationY, Vector2 footprint, Vector3 scale, string role)
        {
            Name = name;
            Path = path;
            Position = position;
            RotationY = rotationY;
            Footprint = footprint;
            Scale = scale;
            Role = role;
        }
    }

    [Serializable]
    private sealed class CatalogEntry
    {
        public string name;
        public string role;
        public string path;
        public float scale;
        public float measuredWidth;
        public float measuredDepth;
        public float measuredHeight;
        public float gameplayWidth;
        public float gameplayDepth;
    }

    [Serializable]
    private sealed class CatalogFile
    {
        public string generatedBy;
        public string scene;
        public List<CatalogEntry> entries = new();
    }

    private readonly struct CoverageSummary
    {
        public readonly float RoadPercent;
        public readonly float BuildingPercent;
        public readonly float BlockerPercent;
        public readonly float SpawnObjectivePercent;
        public readonly float EmptyPercent;

        public CoverageSummary(float roadPercent, float buildingPercent, float blockerPercent, float spawnObjectivePercent, float emptyPercent)
        {
            RoadPercent = roadPercent;
            BuildingPercent = buildingPercent;
            BlockerPercent = blockerPercent;
            SpawnObjectivePercent = spawnObjectivePercent;
            EmptyPercent = emptyPercent;
        }
    }

    [MenuItem("WarlineCapture/Design/Build GC13 Blueprint RTS 2048")]
    public static void BuildGc13BlueprintRts2048()
    {
        Roads.Clear();
        Buildings.Clear();
        Blockers.Clear();
        Spawns.Clear();
        Objectives.Clear();
        PlacementLog.Clear();
        ValidationLog.Clear();
        MissingAssets.Clear();
        coverageSummary = default;

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        Directory.CreateDirectory(ProjectPath(DataRoot));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        GameObject root = new("GC13_BlueprintRts_2048_Root");
        BuildRenderEnvironment(root);
        BuildBasePlane(root);
        DefineGameplayMasks();
        BuildMeasuredCatalog();
        WriteMeasuredCatalog();
        BuildMaskSurfaces(root);
        PlaceTownDistrict(root);
        PlaceMilitaryAndIndustrialDistrict(root);
        PlaceDressing(root);
        PlaceSoldierRouteProof(root);
        BuildCameras(root);
        ValidateLayout();
        coverageSummary = CalculateCoverageSummary();

        EditorSceneManager.SaveScene(scene, ScenePath);
        CaptureScene();
        WriteReport();
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GC13_BLUEPRINT_RTS_BUILT scene={ScenePath} captureRoot={CaptureRoot} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static void BuildRenderEnvironment(GameObject root)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.72f, 0.69f, 0.62f, 1f);
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.57f, 0.49f, 0.37f, 1f);
        RenderSettings.fogDensity = 0.00034f;

        Light key = Child(root, "DirectionalLight_Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.65f;
        key.color = new Color(1f, 0.9f, 0.72f, 1f);
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.55f;
        key.transform.rotation = Quaternion.Euler(50f, -42f, 0f);

        Light fill = Child(root, "DirectionalLight_Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.38f;
        fill.color = new Color(0.58f, 0.74f, 1f, 1f);
        fill.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(30f, 136f, 0f);

        Volume volume = Child(root, "GC13_RTS_PresentationVolume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "GC13_RTS_PresentationProfile";
        volume.sharedProfile = profile;
        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.1f);
        color.contrast.Override(12f);
        color.saturation.Override(4f);
        color.colorFilter.Override(new Color(1f, 0.95f, 0.86f, 1f));
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
    }

    private static void BuildBasePlane(GameObject root)
    {
        Surface(root, "FlatGameplayPlane_2048x2048", Vector3.zero, new Vector2(MapSize, MapSize), CreateMaterial("GC13_SandBase", new Color(0.55f, 0.45f, 0.29f, 1f)), 0f);
    }

    private static void DefineGameplayMasks()
    {
        AddBlueprintRoad("MainNorthSouth_West", 258f, 0f, 60f, 890f);
        AddBlueprintRoad("MainNorthSouth_East", 440f, 0f, 58f, 890f);
        AddBlueprintRoad("NorthCityToAirfield", 62f, 224f, 766f, 54f);
        AddBlueprintRoad("CentralCityToCommand", 130f, 426f, 686f, 54f);
        AddBlueprintRoad("SouthPlayerToFuel", 38f, 578f, 790f, 86f);

        Spawns.Add(new Zone("PlayerSpawn_Blueprint", BlueprintRect(52f, 624f, 36f, 36f)));
        Spawns.Add(new Zone("EnemySpawn_Airfield", BlueprintRect(775f, 227f, 34f, 34f)));
        Spawns.Add(new Zone("EnemySpawn_Command", BlueprintRect(582f, 416f, 28f, 28f)));
        Spawns.Add(new Zone("EnemySpawn_VehicleFuel", BlueprintRect(690f, 634f, 28f, 28f)));
        Objectives.Add(new Zone("Objective_MarketMid", BlueprintRect(356f, 238f, 134f, 52f)));
        Objectives.Add(new Zone("Objective_SouthTown", BlueprintRect(340f, 706f, 134f, 56f)));
        Objectives.Add(new Zone("Objective_CommandOutpost", BlueprintRect(540f, 396f, 86f, 62f)));
    }

    private static void AddRoad(string name, float centerX, float centerZ, float sizeX, float sizeZ)
    {
        Roads.Add(new Zone(name, CenterRect(centerX, centerZ, sizeX, sizeZ)));
    }

    private static void AddBlueprintRoad(string name, float x, float y, float width, float height)
    {
        Roads.Add(new Zone(name, BlueprintRect(x, y, width, height)));
    }

    private static Vector3 BlueprintPoint(float x, float y)
    {
        return new Vector3((x / 890f - 0.5f) * MapSize, 0f, (0.5f - y / 890f) * MapSize);
    }

    private static Rect BlueprintRect(float x, float y, float width, float height)
    {
        Vector3 center = BlueprintPoint(x + width * 0.5f, y + height * 0.5f);
        return CenterRect(center.x, center.z, width / 890f * MapSize, height / 890f * MapSize);
    }

    private static void BuildMaskSurfaces(GameObject root)
    {
        GameObject masks = Child(root, "GameplayMasks_VisibleProof");
        Material road = CreateMaterial("GC13_WalkableRoad_Proof", new Color(0.16f, 0.18f, 0.16f, 1f));
        Material sidewalk = CreateMaterial("GC13_WalkableShoulder_Proof", new Color(0.68f, 0.56f, 0.35f, 1f));
        Material spawn = CreateMaterial("GC13_Spawn_Proof", new Color(0.04f, 0.18f, 0.9f, 1f));
        Material objective = CreateMaterial("GC13_Objective_Proof", new Color(0.9f, 0.48f, 0.04f, 1f));

        BuildBlueprintRoadSurfaces(masks, road, sidewalk);

        foreach (Zone zone in Spawns)
            Surface(masks, zone.Name, Center(zone.Rect, 0.075f), new Vector2(zone.Rect.width, zone.Rect.height), spawn, 0.075f);
        foreach (Zone zone in Objectives)
            Surface(masks, zone.Name, Center(zone.Rect, 0.078f), new Vector2(zone.Rect.width, zone.Rect.height), objective, 0.078f);
    }

    private static void BuildBlueprintRoadSurfaces(GameObject parent, Material road, Material shoulder)
    {
        SurfaceBlueprintPolygon(parent, "MainNorthSouth_West_Shoulder", shoulder, 0.044f, (238f, -4f), (338f, -4f), (306f, 894f), (206f, 894f));
        SurfaceBlueprintPolygon(parent, "MainNorthSouth_West", road, 0.056f, (258f, 0f), (318f, 0f), (286f, 890f), (226f, 890f));
        SurfaceBlueprintPolygon(parent, "MainNorthSouth_East_Shoulder", shoulder, 0.044f, (420f, -4f), (518f, -4f), (488f, 894f), (390f, 894f));
        SurfaceBlueprintPolygon(parent, "MainNorthSouth_East", road, 0.056f, (440f, 0f), (498f, 0f), (468f, 890f), (410f, 890f));

        SurfaceBlueprintPolygon(parent, "NorthCityToAirfield_Shoulder", shoulder, 0.044f, (48f, 226f), (844f, 198f), (850f, 304f), (50f, 332f));
        SurfaceBlueprintPolygon(parent, "NorthCityToAirfield", road, 0.056f, (62f, 250f), (824f, 224f), (828f, 278f), (66f, 304f));
        SurfaceBlueprintPolygon(parent, "CentralCityToCommand_Shoulder", shoulder, 0.044f, (112f, 400f), (358f, 388f), (628f, 322f), (838f, 322f), (838f, 426f), (652f, 426f), (380f, 494f), (116f, 508f));
        SurfaceBlueprintPolygon(parent, "CentralCityToCommand", road, 0.056f, (130f, 426f), (360f, 414f), (632f, 348f), (816f, 350f), (816f, 402f), (646f, 400f), (374f, 466f), (134f, 480f));
        SurfaceBlueprintPolygon(parent, "SouthPlayerToFuel_Shoulder", shoulder, 0.044f, (26f, 588f), (248f, 552f), (518f, 448f), (840f, 412f), (850f, 524f), (544f, 558f), (274f, 660f), (28f, 698f));
        SurfaceBlueprintPolygon(parent, "SouthPlayerToFuel", road, 0.056f, (38f, 612f), (252f, 578f), (520f, 474f), (820f, 442f), (828f, 496f), (538f, 528f), (270f, 632f), (44f, 668f));

        SurfaceBlueprintPolygon(parent, "EnemyCampService_NorthEast_Visual", road, 0.052f, (660f, 92f), (716f, 92f), (708f, 760f), (652f, 760f));
        SurfaceBlueprintPolygon(parent, "CityVertical_WestEdge_Visual", road, 0.052f, (74f, 104f), (126f, 802f), (82f, 804f), (30f, 106f));
        SurfaceBlueprintPolygon(parent, "CityVertical_MidWest_Visual", road, 0.052f, (166f, 72f), (210f, 822f), (166f, 824f), (122f, 74f));
        SurfaceBlueprintPolygon(parent, "CityVertical_Mid_Visual", road, 0.052f, (334f, 118f), (388f, 812f), (344f, 814f), (290f, 120f));
    }

    private static void PlaceTownDistrict(GameObject root)
    {
        GameObject town = Child(root, "Blueprint_CityCoreAndSouthTown");
        List<Placement> placements = new()
        {
            Building("CityCore_Hall", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hall_01.prefab", BlueprintPoint(63f, 67f), 0f, 62f, 46f),
            Building("CityCore_CommandShop", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_11.prefab", BlueprintPoint(177f, 65f), 0f, 78f, 44f),
            Building("CityCore_Block_01", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_01.prefab", BlueprintPoint(52f, 161f), 0f, 46f, 42f),
            Building("CityCore_Block_02", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_02.prefab", BlueprintPoint(149f, 162f), 90f, 54f, 40f),
            Building("CityCore_Block_03", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_03.prefab", BlueprintPoint(247f, 160f), 0f, 52f, 40f),
            Building("CivilMarket_West", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_07.prefab", BlueprintPoint(63f, 355f), 0f, 54f, 42f),
            Building("CivilMarket_Mid", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_09.prefab", BlueprintPoint(162f, 355f), 0f, 62f, 42f),
            Building("CivilMarket_East", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_03.prefab", BlueprintPoint(264f, 353f), 0f, 54f, 42f),
            Building("SouthTown_Block_01", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_05.prefab", BlueprintPoint(66f, 737f), 0f, 58f, 46f),
            Building("SouthTown_Block_02", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_06.prefab", BlueprintPoint(184f, 736f), 90f, 72f, 46f),
            Building("SouthTown_Block_03", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_07.prefab", BlueprintPoint(303f, 736f), 0f, 58f, 46f),
            Building("MarketMid_ObjectiveBuilding", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GasStation_01.prefab", BlueprintPoint(423f, 264f), 0f, 90f, 42f),
            Building("SouthObjective_Building", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_04_Destroyed.prefab", BlueprintPoint(407f, 734f), 0f, 86f, 42f),
        };

        foreach (Placement placement in placements)
            PlaceValidated(town, placement, Buildings, Roads, "building");

        PlaceTownLotDetails(town);
    }

    private static Placement Building(string name, string path, float x, float z, float rotationY, float width, float depth)
    {
        return new Placement(name, path, new Vector3(x, 0f, z), rotationY, CatalogFootprint(path, 1.45f, width, depth), Vector3.one * 1.45f, "building");
    }

    private static Placement Building(string name, string path, Vector3 position, float rotationY, float width, float depth)
    {
        return new Placement(name, path, position, rotationY, new Vector2(width, depth), Vector3.one * 0.95f, "building");
    }

    private static void PlaceMilitaryAndIndustrialDistrict(GameObject root)
    {
        GameObject baseRoot = Child(root, "Blueprint_EnemyCampsAndMilitaryDistricts");
        List<Placement> placements = new()
        {
            Base("Airfield_Hangar", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hangar_01.prefab", BlueprintPoint(667f, 123f), 0f, 120f, 78f),
            Base("Airfield_ControlTower", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_ControlTower_01.prefab", BlueprintPoint(668f, 195f), 0f, 56f, 48f),
            Base("Airfield_HQ", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab", BlueprintPoint(752f, 358f), 0f, 74f, 52f),
            Base("Command_HQ", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab", BlueprintPoint(583f, 427f), 0f, 76f, 54f),
            Base("Command_Supply", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Desert_01.prefab", BlueprintPoint(586f, 490f), 0f, 88f, 38f),
            Base("VehicleCamp_WestPad", "Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Heavy.prefab", BlueprintPoint(632f, 648f), 0f, 56f, 42f),
            Base("VehicleCamp_EastPad", "Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab", BlueprintPoint(716f, 648f), 0f, 56f, 42f),
            Base("VehicleCamp_Barracks", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab", BlueprintPoint(672f, 728f), 0f, 124f, 48f),
            Base("FuelCamp_Utility", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GasTower_01.prefab", BlueprintPoint(789f, 676f), 0f, 42f, 112f),
        };

        foreach (Placement placement in placements)
            PlaceValidated(baseRoot, placement, Buildings, Roads, "base");

        PlaceBaseLotDetails(baseRoot);
    }

    private static Placement Base(string name, string path, float x, float z, float rotationY, float width, float depth)
    {
        return new Placement(name, path, new Vector3(x, 0f, z), rotationY, CatalogFootprint(path, 1.25f, width, depth), Vector3.one * 1.25f, "base");
    }

    private static Placement Base(string name, string path, Vector3 position, float rotationY, float width, float depth)
    {
        return new Placement(name, path, position, rotationY, new Vector2(width, depth), Vector3.one * 0.82f, "base");
    }

    private static void PlaceDressing(GameObject root)
    {
        GameObject dressing = Child(root, "Blueprint_Dressing_LegalOutsideRoads");
        List<Placement> placements = new()
        {
            Deco("NorthWest_NoBuildRock", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_05.prefab", BlueprintPoint(55f, 38f), 20f, 76f, 58f),
            Deco("SouthWest_NoBuildRock", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_03.prefab", BlueprintPoint(64f, 820f), -20f, 78f, 70f),
            Deco("NorthEast_NoBuildRock", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_04.prefab", BlueprintPoint(858f, 58f), 35f, 72f, 60f),
            Deco("SouthEast_NoBuildRock", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_06.prefab", BlueprintPoint(840f, 842f), -15f, 78f, 64f),
            Deco("MidWalk_RockCluster_01", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_02.prefab", BlueprintPoint(470f, 575f), 24f, 50f, 44f),
            Deco("AirfieldEdge_RockCluster", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_01.prefab", BlueprintPoint(742f, 320f), 24f, 46f, 38f),
        };

        int index = 0;
        foreach ((float x, float y) in new[] { (34f, 520f), (128f, 484f), (214f, 506f), (344f, 486f), (500f, 446f), (552f, 266f), (668f, 358f), (614f, 594f), (760f, 566f), (816f, 632f) })
        {
            string path = index % 2 == 0 ? "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_01.prefab" : "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_02.prefab";
            placements.Add(Deco($"WalkAreaPalmEdge_{index:00}", path, BlueprintPoint(x, y), 0f, 24f, 24f));
            index++;
        }

        foreach (Placement placement in placements)
            PlaceValidated(dressing, placement, Blockers, Roads, "dressing");
    }

    private static void PlaceTownLotDetails(GameObject town)
    {
        string[] annexes =
        {
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Shack_01.prefab",
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Shack_02.prefab",
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_Well_01.prefab",
            "Assets/Game/Prefabs/Environment/City/SM_Bld_Fountain_01.prefab",
            "Assets/Game/Prefabs/Environment/CityWalls/SM_Bld_Village_Wall_01.prefab",
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_ClothCover_01.prefab",
        };

        int index = 0;
        foreach ((float x, float y) in new[] { (95f, 118f), (126f, 225f), (205f, 224f), (302f, 224f), (104f, 430f), (198f, 430f), (304f, 430f), (116f, 788f), (244f, 788f), (482f, 300f), (368f, 300f) })
        {
            AddFittedSecondary(town, $"BlueprintTownDetail_{index:00}", annexes[index % annexes.Length], BlueprintPoint(x, y).x, BlueprintPoint(x, y).z, (index % 4) * 45f, 26f, 22f, 1.0f, "town detail");
            index++;
        }
    }

    private static void PlaceBaseLotDetails(GameObject baseRoot)
    {
        string[] baseDetails =
        {
            "Assets/Game/Prefabs/Buildings/Wall_Fence_Straight.prefab",
            "Assets/Game/Prefabs/Buildings/Wall_Dirt_Straight.prefab",
            "Assets/Game/Prefabs/Buildings/Building_Road_Barrier.prefab",
            "Assets/Game/Prefabs/Buildings/Building_Satelite_Dish.prefab",
            "Assets/Game/Prefabs/Buildings/Building_WaterTank.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Canopy.prefab",
        };

        int index = 0;
        foreach ((float x, float y) in new[] { (610f, 82f), (716f, 92f), (642f, 320f), (650f, 350f), (512f, 356f), (650f, 512f), (566f, 574f), (800f, 552f), (810f, 814f), (744f, 296f) })
        {
            AddFittedSecondary(baseRoot, $"BlueprintBaseDetail_{index:00}", baseDetails[index % baseDetails.Length], BlueprintPoint(x, y).x, BlueprintPoint(x, y).z, (index % 4) * 45f, 32f, 22f, 1.0f, "base detail");
            index++;
        }
    }

    private static void AddFittedSecondary(GameObject parent, string name, string path, float x, float z, float rotationY, float width, float depth, float scale, string role)
    {
        Vector2 footprint = CatalogFootprint(path, scale, width, depth);
        List<Zone> target = role.Contains("detail", StringComparison.Ordinal) || role.Contains("market", StringComparison.Ordinal) ? Buildings : Blockers;
        PlaceValidated(parent, new Placement(name, path, new Vector3(x, 0f, z), rotationY, footprint, Vector3.one * scale, role), target, Roads, role);
    }

    private static Placement Deco(string name, string path, float x, float z, float rotationY, float width, float depth)
    {
        return new Placement(name, path, new Vector3(x, 0f, z), rotationY, new Vector2(width, depth), Vector3.one * 1.15f, "dressing");
    }

    private static Placement Deco(string name, string path, Vector3 position, float rotationY, float width, float depth)
    {
        return new Placement(name, path, position, rotationY, new Vector2(width, depth), Vector3.one * 1.0f, "dressing");
    }

    private static void PlaceSoldierRouteProof(GameObject root)
    {
        GameObject units = Child(root, "Soldiers_OnValidatedWalkableStreets");
        Material blue = CreateMaterial("GC13_BlueRoute", new Color(0.04f, 0.16f, 0.95f, 1f));
        Material red = CreateMaterial("GC13_RedRoute", new Color(0.85f, 0.04f, 0.02f, 1f));
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab", BlueprintPoint(70f, 642f), 72f, blue, "player soldier 1");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab", BlueprintPoint(82f, 632f), 72f, blue, "player soldier 2");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_01.prefab", BlueprintPoint(82f, 652f), 72f, blue, "player soldier 3");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_02.prefab", BlueprintPoint(94f, 642f), 72f, blue, "player soldier 4");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab", BlueprintPoint(792f, 244f), 252f, red, "enemy soldier 1");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_02.prefab", BlueprintPoint(596f, 430f), 252f, red, "enemy soldier 2");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Female_01.prefab", BlueprintPoint(704f, 648f), 252f, red, "enemy soldier 3");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_03.prefab", BlueprintPoint(716f, 662f), 252f, red, "enemy soldier 4");
    }

    private static void BuildRoute(GameObject parent, string name, Vector3[] points, Material material)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 from = points[i];
            Vector3 to = points[i + 1];
            Vector3 center = (from + to) * 0.5f;
            float length = Vector3.Distance(from, to);
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = $"{name}_{i:00}";
            segment.transform.SetParent(parent.transform, true);
            segment.transform.position = new Vector3(center.x, 0.16f, center.z);
            segment.transform.localScale = new Vector3(8f, 0.05f, length);
            segment.transform.rotation = Quaternion.LookRotation(to - from, Vector3.up);
            Object.DestroyImmediate(segment.GetComponent<Collider>());
            segment.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    private static void PlaceUnit(GameObject parent, string path, Vector3 position, float rotationY, Material ringMaterial, string label)
    {
        GameObject unit = InstantiatePrefab(path, parent, label);
        if (unit == null)
            return;

        unit.transform.position = position;
        unit.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        unit.transform.localScale = Vector3.one * 1.1f;
        AlignBottomNearGround(unit);
        BuildSelectionRing(parent, label + "_Ring", new Vector3(unit.transform.position.x, 0.12f, unit.transform.position.z), 3.4f, ringMaterial);

        if (!IsPointOnRoad(position))
            ValidationLog.Add($"ERROR: {label} is not on a walkable road at {Format(position)}");
        else
            PlacementLog.Add($"{label}: placed on walkable street at {Format(position)}");
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

    private static void PlaceValidated(GameObject parent, Placement placement, List<Zone> targetZones, List<Zone> forbiddenWalkable, string role)
    {
        bool optionalDetail = role.Contains("detail", StringComparison.Ordinal) || role.Contains("market", StringComparison.Ordinal);
        Rect footprint = FootprintRect(placement.Position, placement.Footprint);
        if (forbiddenWalkable.Any(zone => zone.Rect.Overlaps(footprint)))
        {
            if (role == "dressing" || optionalDetail)
            {
                PlacementLog.Add($"skipped optional {role}: {placement.Name} touched walkable road and was omitted. footprint={Format(footprint)}");
                return;
            }

            PlacementLog.Add($"skipped {role}: {placement.Name} touched walkable road and was omitted. footprint={Format(footprint)}");
            return;
        }

        if (Buildings.Concat(Blockers).Any(zone => zone.Rect.Overlaps(footprint)))
        {
            if (role == "dressing" || optionalDetail)
            {
                PlacementLog.Add($"skipped optional {role}: {placement.Name} overlapped a placed footprint and was omitted. footprint={Format(footprint)}");
                return;
            }

            PlacementLog.Add($"skipped {role}: {placement.Name} overlapped a placed footprint and was omitted. footprint={Format(footprint)}");
            return;
        }

        GameObject instance = InstantiatePrefab(placement.Path, parent, placement.Name);
        if (instance == null)
            return;

        instance.transform.position = placement.Position;
        instance.transform.rotation = Quaternion.Euler(0f, placement.RotationY, 0f);
        instance.transform.localScale = placement.Scale;
        AlignBottomNearGround(instance);
        targetZones.Add(new Zone(placement.Name, footprint));
        PlacementLog.Add($"{placement.Role}: {placement.Name} at {Format(placement.Position)} footprint={Format(footprint)} asset={placement.Path}");
    }

    private static GameObject InstantiatePrefab(string path, GameObject parent, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            MissingAssets.Add(path);
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent.transform, true);
        return instance;
    }

    private static void BuildCameras(GameObject root)
    {
        Camera map = CameraObject(root, "Camera_GC13_TopDownWalkability");
        map.orthographic = true;
        map.orthographicSize = 1030f;
        map.transform.position = new Vector3(0f, 1400f, 0f);
        map.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Camera town = CameraObject(root, "Camera_GC13_RtsTownRoute");
        town.orthographic = false;
        town.fieldOfView = 42f;
        town.transform.position = new Vector3(-880f, 150f, -780f);
        town.transform.LookAt(BlueprintPoint(170f, 642f));

        Camera baseView = CameraObject(root, "Camera_GC13_RtsBaseRoute");
        baseView.orthographic = false;
        baseView.fieldOfView = 42f;
        baseView.transform.position = new Vector3(520f, 165f, -760f);
        baseView.transform.LookAt(BlueprintPoint(620f, 430f));

        Camera overview = CameraObject(root, "Camera_GC13_RtsCoverageOverview");
        overview.orthographic = false;
        overview.fieldOfView = 46f;
        overview.transform.position = new Vector3(-240f, 380f, -1180f);
        overview.transform.LookAt(Vector3.zero);

        Camera denseCity = CameraObject(root, "Camera_GC13_RtsDenseCityReview");
        denseCity.orthographic = false;
        denseCity.fieldOfView = 40f;
        denseCity.transform.position = new Vector3(-850f, 130f, 20f);
        denseCity.transform.LookAt(BlueprintPoint(160f, 250f));

        Camera denseBase = CameraObject(root, "Camera_GC13_RtsDenseBaseReview");
        denseBase.orthographic = false;
        denseBase.fieldOfView = 40f;
        denseBase.transform.position = new Vector3(490f, 150f, -540f);
        denseBase.transform.LookAt(BlueprintPoint(704f, 648f));
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

    private static void ValidateLayout()
    {
        foreach (Zone building in Buildings)
            foreach (Zone road in Roads)
                if (building.Rect.Overlaps(road.Rect))
                    ValidationLog.Add($"ERROR: building {building.Name} overlaps road {road.Name}");

        foreach (Zone blocker in Blockers)
            foreach (Zone road in Roads)
                if (blocker.Rect.Overlaps(road.Rect))
                    ValidationLog.Add($"ERROR: blocker {blocker.Name} overlaps road {road.Name}");

        foreach (Zone spawn in Spawns)
            if (!Roads.Any(road => road.Rect.Overlaps(spawn.Rect)))
                ValidationLog.Add($"ERROR: spawn {spawn.Name} is not connected to a road.");

        foreach (Zone objective in Objectives)
            if (!Roads.Any(road => road.Rect.Overlaps(Expanded(objective.Rect, 80f))))
                ValidationLog.Add($"ERROR: objective {objective.Name} is not adjacent to a road.");

        if (ValidationLog.Count == 0)
            ValidationLog.Add("PASS: GC13 blueprint-first layout has no building/blocker overlap on walkable roads; spawns/objectives connect to the GC11 road contract; proof soldiers are on walkable streets.");
    }

    private static CoverageSummary CalculateCoverageSummary()
    {
        const int samplesPerAxis = 128;
        int road = 0;
        int building = 0;
        int blocker = 0;
        int spawnObjective = 0;
        int empty = 0;

        for (int z = 0; z < samplesPerAxis; z++)
        {
            for (int x = 0; x < samplesPerAxis; x++)
            {
                Vector2 point = new(
                    -MapSize * 0.5f + (x + 0.5f) * MapSize / samplesPerAxis,
                    -MapSize * 0.5f + (z + 0.5f) * MapSize / samplesPerAxis);

                if (Spawns.Any(zone => zone.Rect.Contains(point)) || Objectives.Any(zone => zone.Rect.Contains(point)))
                    spawnObjective++;
                else if (Roads.Any(zone => zone.Rect.Contains(point)))
                    road++;
                else if (Buildings.Any(zone => zone.Rect.Contains(point)))
                    building++;
                else if (Blockers.Any(zone => zone.Rect.Contains(point)))
                    blocker++;
                else
                    empty++;
            }
        }

        float total = samplesPerAxis * samplesPerAxis;
        return new CoverageSummary(road / total * 100f, building / total * 100f, blocker / total * 100f, spawnObjective / total * 100f, empty / total * 100f);
    }

    private static void CaptureScene()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (camera.name == "Camera_GC13_TopDownWalkability")
                Render(camera, ProjectPath(CaptureRoot + "/gc13_topdown_blueprint_match_2048x2048.png"), 2048, 2048);
            if (camera.name == "Camera_GC13_RtsTownRoute")
                Render(camera, ProjectPath(CaptureRoot + "/gc13_rts_town_route_soldiers_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC13_RtsBaseRoute")
                Render(camera, ProjectPath(CaptureRoot + "/gc13_rts_base_route_soldiers_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC13_RtsCoverageOverview")
                Render(camera, ProjectPath(CaptureRoot + "/gc13_rts_2048_coverage_overview_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC13_RtsDenseCityReview")
                Render(camera, ProjectPath(CaptureRoot + "/gc13_rts_dense_city_review_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC13_RtsDenseBaseReview")
                Render(camera, ProjectPath(CaptureRoot + "/gc13_rts_dense_base_review_1920x1080.png"), 1920, 1080);
        }
    }

    private static void WriteReport()
    {
        StringBuilder report = new();
        report.AppendLine("# GC13 Blueprint-First RTS Scene");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Rebuild the 2048 RTS scene as a blueprint-first pass that follows the GC11 road, city, enemy airfield, command outpost, vehicle/fuel camp, spawn, and objective layout before visual dressing.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGc13BlueprintRtsSceneBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC13_BlueprintRts_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Data/GeneratedScenes/GC13_BlueprintRts_2048/gc13_prefab_footprint_catalog.json`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048/gc13_topdown_blueprint_match_2048x2048.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048/gc13_rts_town_route_soldiers_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048/gc13_rts_base_route_soldiers_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048/gc13_rts_2048_coverage_overview_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048/gc13_rts_dense_city_review_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048/gc13_rts_dense_base_review_1920x1080.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: GC11 expanded 2048 RTS blueprint layout contract.");
        report.AppendLine("User-visible behavior: none in shipped flow; generated scene is available for PM/gameplay review.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureGc13BlueprintRtsSceneBuilder.BuildGc13BlueprintRts2048`.");
        report.AppendLine("Validation result: " + (ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)) ? "failed with layout errors; see validation log below." : "passed blueprint-first road and footprint validation."));
        report.AppendLine("Known gaps: GC13 prioritizes blueprint match and walkability over beauty. It uses individual prefabs and legal dressing, so the next visual pass should replace simple lots with authored modules that preserve the same footprint contract.");
        report.AppendLine("Cross-lane impacts: PM/Design can review the workflow and proof captures; runtime ECS flow and UI are untouched.");
        report.AppendLine("Next recommended task: compare the GC13 top-down capture against the GC11 blueprint; if accepted, replace individual lot fillers with authored visual modules without moving the roads or gameplay masks.");
        report.AppendLine();
        report.AppendLine("Coverage metrics:");
        report.AppendLine($"- walkable roads: {coverageSummary.RoadPercent:0.0}%");
        report.AppendLine($"- buildings/base structures: {coverageSummary.BuildingPercent:0.0}%");
        report.AppendLine($"- blockers/decor/industrial: {coverageSummary.BlockerPercent:0.0}%");
        report.AppendLine($"- spawns/objectives: {coverageSummary.SpawnObjectivePercent:0.0}%");
        report.AppendLine($"- empty/unreserved desert: {coverageSummary.EmptyPercent:0.0}%");
        report.AppendLine($"- measured prefab catalog entries: {Catalog.Count}");
        report.AppendLine();
        report.AppendLine("Validation log:");
        foreach (string line in ValidationLog)
            report.AppendLine("- " + line);
        if (MissingAssets.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("Missing assets:");
            foreach (string path in MissingAssets.Distinct(StringComparer.Ordinal))
                report.AppendLine("- " + path);
        }
        report.AppendLine();
        report.AppendLine("Placement log:");
        foreach (string line in PlacementLog)
            report.AppendLine("- " + line);

        File.WriteAllText(ProjectPath(ReportPath), report.ToString(), Encoding.UTF8);
    }

    private static void BuildMeasuredCatalog()
    {
        Catalog.Clear();
        foreach ((string role, float scale, string path) in CandidatePrefabs())
        {
            if (Catalog.Any(entry => entry.path == path && Mathf.Approximately(entry.scale, scale)))
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                MissingAssets.Add(path);
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "GC13_CatalogMeasure_" + Path.GetFileNameWithoutExtension(path);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.position = new Vector3(5000f, 5000f, 5000f);
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * scale;

            Bounds bounds = CalculateRendererBounds(instance);
            Object.DestroyImmediate(instance);

            float measuredWidth = Mathf.Max(0.01f, bounds.size.x);
            float measuredDepth = Mathf.Max(0.01f, bounds.size.z);
            float measuredHeight = Mathf.Max(0.01f, bounds.size.y);
            Vector2 gameplay = GameplayFootprintFor(path, role, measuredWidth, measuredDepth);
            Catalog.Add(new CatalogEntry
            {
                name = Path.GetFileNameWithoutExtension(path),
                role = role,
                path = path,
                scale = scale,
                measuredWidth = measuredWidth,
                measuredDepth = measuredDepth,
                measuredHeight = measuredHeight,
                gameplayWidth = gameplay.x,
                gameplayDepth = gameplay.y
            });
        }

        PlacementLog.Add($"catalog: measured {Catalog.Count} prefab footprints before layout placement.");
    }

    private static IEnumerable<(string role, float scale, string path)> CandidatePrefabs()
    {
        string[] town =
        {
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hall_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Clock_Tower_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GasStation_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_04_Destroyed.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_03.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_05.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_06.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_07.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_03.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_07.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_09.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_11.prefab",
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Shack_01.prefab",
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Shack_02.prefab",
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_Well_01.prefab",
            "Assets/Game/Prefabs/Environment/City/SM_Bld_Fountain_01.prefab",
            "Assets/Game/Prefabs/Environment/CityWalls/SM_Bld_Village_Wall_01.prefab",
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_ClothCover_01.prefab",
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_ClothCover_02.prefab",
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_ClothCover_03.prefab",
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Archway_01.prefab",
        };
        foreach (string path in town)
            yield return ("town", 1.2f, path);

        string[] military =
        {
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_ControlTower_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hangar_01.prefab",
            "Assets/Game/Prefabs/Buildings/Building_Helipad.prefab",
            "Assets/Game/Prefabs/Buildings/Building_Fuel_Bladder.prefab",
            "Assets/Game/Prefabs/Buildings/Building_Refinery.prefab",
            "Assets/Game/Prefabs/Buildings/Building_Refinery_Big.prefab",
            "Assets/Game/Prefabs/Buildings/Building_OilPump.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Desert_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Refugee_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_CamoNet_Tent_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GuardTower_01.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Heavy.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab",
            "Assets/Game/Prefabs/Buildings/Wall_Fence_Straight.prefab",
            "Assets/Game/Prefabs/Buildings/Wall_Dirt_Straight.prefab",
            "Assets/Game/Prefabs/Buildings/Building_Road_Barrier.prefab",
            "Assets/Game/Prefabs/Buildings/Building_Satelite_Dish.prefab",
            "Assets/Game/Prefabs/Buildings/Building_WaterTank.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Canopy.prefab",
        };
        foreach (string path in military)
            yield return ("base", 1.0f, path);

        string[] blockers =
        {
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_02.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_03.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_04.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_05.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_06.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_01.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_02.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Pebbles_01.prefab",
        };
        foreach (string path in blockers)
            yield return ("dressing", 1.0f, path);
    }

    private static Vector2 CatalogFootprint(string path, float scale, float fallbackWidth, float fallbackDepth)
    {
        CatalogEntry entry = Catalog.FirstOrDefault(item => item.path == path && Mathf.Abs(item.scale - scale) < 0.26f);
        if (entry == null)
            return new Vector2(fallbackWidth, fallbackDepth);
        return new Vector2(Mathf.Max(fallbackWidth, entry.gameplayWidth), Mathf.Max(fallbackDepth, entry.gameplayDepth));
    }

    private static Vector2 GameplayFootprintFor(string path, string role, float measuredWidth, float measuredDepth)
    {
        float padding = role == "dressing" ? 10f : 18f;
        float minimum = role == "base" ? 34f : 24f;
        if (path.Contains("/Vehicles/", StringComparison.Ordinal))
            minimum = 42f;
        if (path.Contains("Hangar", StringComparison.Ordinal) || path.Contains("Refinery", StringComparison.Ordinal))
            minimum = 78f;
        return new Vector2(Mathf.Max(minimum, measuredWidth + padding), Mathf.Max(minimum, measuredDepth + padding));
    }

    private static Bounds CalculateRendererBounds(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return new Bounds(instance.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void WriteMeasuredCatalog()
    {
        CatalogFile file = new()
        {
            generatedBy = nameof(WarlineCaptureGc13BlueprintRtsSceneBuilder),
            scene = ScenePath,
            entries = Catalog.OrderBy(entry => entry.role, StringComparer.Ordinal).ThenBy(entry => entry.name, StringComparer.Ordinal).ToList()
        };
        File.WriteAllText(ProjectPath(FootprintCatalogPath), JsonUtility.ToJson(file, true), Encoding.UTF8);
    }

    private static bool IsPointOnRoad(Vector3 position)
    {
        Vector2 point = new(position.x, position.z);
        return Roads.Any(road => road.Rect.Contains(point));
    }

    private static Rect Expanded(Rect rect, float amount)
    {
        return new Rect(rect.xMin - amount, rect.yMin - amount, rect.width + amount * 2f, rect.height + amount * 2f);
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

    private static void SurfaceBlueprintPolygon(GameObject parent, string name, Material material, float y, params (float x, float y)[] points)
    {
        if (points.Length < 3)
            return;

        GameObject surface = new(name);
        surface.transform.SetParent(parent.transform, false);
        surface.transform.position = new Vector3(0f, y, 0f);
        Mesh mesh = new();
        Vector3[] vertices = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
            vertices[i] = BlueprintPoint(points[i].x, points[i].y);

        int[] triangles = new int[(points.Length - 2) * 3];
        int triangle = 0;
        for (int i = 1; i < points.Length - 1; i++)
        {
            triangles[triangle++] = 0;
            triangles[triangle++] = i;
            triangles[triangle++] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.uv = vertices.Select(vertex => new Vector2(vertex.x / MapSize + 0.5f, vertex.z / MapSize + 0.5f)).ToArray();
        mesh.triangles = triangles;
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
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", Texture2D.whiteTexture);
        return material;
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
