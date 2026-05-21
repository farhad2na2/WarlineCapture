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

public static class WarlineCaptureGc03PlayableCityBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC03_PlayableCity_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC03_PlayableCity_2048";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GC03_PlayableCity_2048";
    private const string FootprintCatalogPath = DataRoot + "/gc03_prefab_footprint_catalog.json";
    private const string ReportPath = "Design/AgentReports/2026-05-20_gameplay_gc03-playable-city-layout.md";
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

    [MenuItem("WarlineCapture/Design/Build GC03 Playable City 2048")]
    public static void BuildGc03PlayableCity2048()
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

        GameObject root = new("GC03_PlayableCity_2048_Root");
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
        Debug.Log($"WARLINECAPTURE_GC03_PLAYABLE_CITY_BUILT scene={ScenePath} captureRoot={CaptureRoot} report={ReportPath}");
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

        Volume volume = Child(root, "GC03_RTS_PresentationVolume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "GC03_RTS_PresentationProfile";
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
        Surface(root, "FlatGameplayPlane_2048x2048", Vector3.zero, new Vector2(MapSize, MapSize), CreateMaterial("GC03_SandBase", new Color(0.55f, 0.45f, 0.29f, 1f)), 0f);
    }

    private static void DefineGameplayMasks()
    {
        AddRoad("MainHighway", 0f, 0f, 92f, 1920f);

        foreach (float x in new[] { -900f, -700f, -500f, -300f, -110f })
            AddRoad($"TownVertical_{x:0}", x, 0f, 36f, 1560f);
        foreach (float z in new[] { -720f, -520f, -320f, -120f, 80f, 280f, 480f, 680f })
            AddRoad($"TownHorizontal_{z:0}", -505f, z, 850f, 38f);

        AddRoad("TownHighwayConnector_North", -145f, 480f, 260f, 38f);
        AddRoad("TownHighwayConnector_Market", -145f, 80f, 260f, 38f);
        AddRoad("TownHighwayConnector_South", -145f, -320f, 260f, 38f);

        foreach (float x in new[] { 230f, 450f, 670f, 850f })
            AddRoad($"BaseVertical_{x:0}", x, 40f, 36f, 1420f);
        foreach (float z in new[] { -620f, -380f, -140f, 120f, 360f, 600f })
            AddRoad($"BaseHorizontal_{z:0}", 535f, z, 690f, 38f);

        AddRoad("BaseHighwayConnector_North", 145f, 360f, 260f, 38f);
        AddRoad("BaseHighwayConnector_Gate", 145f, 120f, 260f, 38f);
        AddRoad("IndustrialHighwayConnector", 145f, -380f, 260f, 38f);

        Spawns.Add(new Zone("PlayerSpawn_WestTownStreet", CenterRect(-900f, -320f, 74f, 48f)));
        Spawns.Add(new Zone("EnemySpawn_BaseRoad", CenterRect(450f, 120f, 74f, 48f)));
        Objectives.Add(new Zone("Objective_TownMarket", CenterRect(-500f, 80f, 94f, 60f)));
        Objectives.Add(new Zone("Objective_BaseGate", CenterRect(230f, 120f, 90f, 60f)));
        Objectives.Add(new Zone("Objective_IndustrialFuel", CenterRect(450f, -380f, 96f, 60f)));
    }

    private static void AddRoad(string name, float centerX, float centerZ, float sizeX, float sizeZ)
    {
        Roads.Add(new Zone(name, CenterRect(centerX, centerZ, sizeX, sizeZ)));
    }

    private static void BuildMaskSurfaces(GameObject root)
    {
        GameObject masks = Child(root, "GameplayMasks_VisibleProof");
        Material road = CreateMaterial("GC03_WalkableRoad_Proof", new Color(0.16f, 0.18f, 0.16f, 1f));
        Material sidewalk = CreateMaterial("GC03_WalkableShoulder_Proof", new Color(0.68f, 0.56f, 0.35f, 1f));
        Material spawn = CreateMaterial("GC03_Spawn_Proof", new Color(0.04f, 0.18f, 0.9f, 1f));
        Material objective = CreateMaterial("GC03_Objective_Proof", new Color(0.9f, 0.48f, 0.04f, 1f));

        foreach (Zone roadZone in Roads)
        {
            Surface(masks, roadZone.Name + "_Shoulder", Center(roadZone.Rect, 0.045f), new Vector2(roadZone.Rect.width + 18f, roadZone.Rect.height + 18f), sidewalk, 0.045f);
            Surface(masks, roadZone.Name, Center(roadZone.Rect, 0.055f), new Vector2(roadZone.Rect.width, roadZone.Rect.height), road, 0.055f);
        }

        foreach (Zone zone in Spawns)
            Surface(masks, zone.Name, Center(zone.Rect, 0.075f), new Vector2(zone.Rect.width, zone.Rect.height), spawn, 0.075f);
        foreach (Zone zone in Objectives)
            Surface(masks, zone.Name, Center(zone.Rect, 0.078f), new Vector2(zone.Rect.width, zone.Rect.height), objective, 0.078f);
    }

    private static void PlaceTownDistrict(GameObject root)
    {
        GameObject town = Child(root, "TownBuildings_ExpandedLots");
        List<Placement> placements = new()
        {
            Building("TownMarketHall", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hall_01.prefab", -600f, 180f, 0f, 86f, 68f),
            Building("TownClockTower", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Clock_Tower_01.prefab", -400f, 180f, 0f, 52f, 52f),
            Building("TownGasStation", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GasStation_01.prefab", -600f, -420f, 90f, 86f, 64f),
            Building("TownDestroyedLandmark", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_04_Destroyed.prefab", -400f, -420f, 0f, 62f, 60f),
        };

        string[] assets =
        {
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
        };

        int index = 0;
        foreach (float z in new[] { -620f, -420f, -220f, -20f, 180f, 380f, 580f, 780f })
        {
            foreach (float x in new[] { -800f, -600f, -400f, -210f })
            {
                if (Mathf.Abs(x + 600f) < 1f && (Mathf.Abs(z - 180f) < 1f || Mathf.Abs(z + 420f) < 1f))
                    continue;
                if (Mathf.Abs(x + 400f) < 1f && (Mathf.Abs(z - 180f) < 1f || Mathf.Abs(z + 420f) < 1f))
                    continue;

                string path = assets[index % assets.Length];
                Vector2 fallback = index % 5 == 0 ? new Vector2(62f, 58f) : new Vector2(54f, 54f);
                Vector2 footprint = CatalogFootprint(path, 1.42f, fallback.x, fallback.y);
                placements.Add(new Placement($"TownLot_{index:00}", path, new Vector3(x, 0f, z), (index % 4) * 90f, footprint, Vector3.one * 1.42f, "building"));
                index++;
            }
        }

        foreach (Placement placement in placements)
            PlaceValidated(town, placement, Buildings, Roads, "building");

        PlaceTownLotDetails(town);
    }

    private static Placement Building(string name, string path, float x, float z, float rotationY, float width, float depth)
    {
        return new Placement(name, path, new Vector3(x, 0f, z), rotationY, CatalogFootprint(path, 1.45f, width, depth), Vector3.one * 1.45f, "building");
    }

    private static void PlaceMilitaryAndIndustrialDistrict(GameObject root)
    {
        GameObject baseRoot = Child(root, "MilitaryAndIndustrial_ExpandedLots");
        List<Placement> placements = new()
        {
            Base("BaseControlTower", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_ControlTower_01.prefab", 560f, 500f, 0f, 64f, 64f),
            Base("BaseHangar", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hangar_01.prefab", 760f, 500f, 0f, 100f, 82f),
            Base("BaseHelipad", "Assets/Game/Prefabs/Buildings/Building_Helipad.prefab", 760f, -20f, 0f, 86f, 86f),
            Base("BaseFuelBladder", "Assets/Game/Prefabs/Buildings/Building_Fuel_Bladder.prefab", 780f, -260f, 0f, 76f, 58f),
            Base("IndustrialRefinery", "Assets/Game/Prefabs/Buildings/Building_Refinery.prefab", 560f, -500f, 0f, 100f, 78f),
            Base("IndustrialRefineryBig", "Assets/Game/Prefabs/Buildings/Building_Refinery_Big.prefab", 760f, -500f, 0f, 100f, 82f),
            Base("IndustrialOilPump", "Assets/Game/Prefabs/Buildings/Building_OilPump.prefab", 340f, -740f, 0f, 58f, 48f),
        };

        string[] assets =
        {
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Desert_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Refugee_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_CamoNet_Tent_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GuardTower_01.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Heavy.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab",
        };

        int index = 0;
        foreach (float z in new[] { -740f, -500f, -260f, 0f, 240f, 500f, 740f })
        {
            foreach (float x in new[] { 340f, 560f, 780f })
            {
                if ((Mathf.Abs(x - 560f) < 1f && (Mathf.Abs(z - 500f) < 1f || Mathf.Abs(z + 500f) < 1f)) ||
                    (Mathf.Abs(x - 780f) < 1f && (Mathf.Abs(z - 500f) < 1f || Mathf.Abs(z) < 1f || Mathf.Abs(z + 260f) < 1f || Mathf.Abs(z + 500f) < 1f)) ||
                    (Mathf.Abs(x - 340f) < 1f && Mathf.Abs(z + 740f) < 1f))
                    continue;

                string path = assets[index % assets.Length];
                bool vehicle = path.Contains("/Vehicles/", StringComparison.Ordinal);
                Vector2 fallback = vehicle ? new Vector2(54f, 38f) : new Vector2(66f, 54f);
                Vector2 footprint = CatalogFootprint(path, 1.25f, fallback.x, fallback.y);
                placements.Add(new Placement($"BaseLot_{index:00}", path, new Vector3(x, 0f, z), (index % 4) * 90f, footprint, Vector3.one * 1.25f, "base"));
                index++;
            }
        }

        foreach (Placement placement in placements)
            PlaceValidated(baseRoot, placement, Buildings, Roads, "base");

        PlaceBaseLotDetails(baseRoot);
    }

    private static Placement Base(string name, string path, float x, float z, float rotationY, float width, float depth)
    {
        return new Placement(name, path, new Vector3(x, 0f, z), rotationY, CatalogFootprint(path, 1.25f, width, depth), Vector3.one * 1.25f, "base");
    }

    private static void PlaceDressing(GameObject root)
    {
        GameObject dressing = Child(root, "Dressing_LegalOutsideRoads");
        List<Placement> placements = new()
        {
            Deco("TownRock_WestBoundary_01", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_05.prefab", -1000f, 520f, 20f, 90f, 82f),
            Deco("TownRock_WestBoundary_02", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_03.prefab", -1000f, -40f, -20f, 78f, 70f),
            Deco("TownRock_WestBoundary_03", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_04.prefab", -1000f, -650f, 35f, 96f, 86f),
            Deco("BaseRock_EastBoundary_01", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_06.prefab", 1000f, 500f, -15f, 96f, 86f),
            Deco("BaseRock_EastBoundary_02", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_02.prefab", 1000f, -280f, 24f, 82f, 76f),
        };

        int index = 0;
        foreach (float z in new[] { -620f, -220f, 180f, 580f })
        {
            foreach (float x in new[] { -805f, -605f, -405f, -205f, 335f, 555f, 775f })
            {
                string path = index % 2 == 0 ? "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_01.prefab" : "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_02.prefab";
                placements.Add(Deco($"PalmStreetEdge_{index:00}", path, x, z + 66f, 0f, 28f, 28f));
                index++;
            }
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
        foreach (float z in new[] { -620f, -420f, -220f, -20f, 180f, 380f, 580f, 780f })
        {
            foreach (float x in new[] { -800f, -600f, -400f, -210f })
            {
                AddFittedSecondary(town, $"TownDetail_{index:00}_A", annexes[index % annexes.Length], x - 64f, z + 64f, 45f, 28f, 24f, 1.2f, "town detail");
                AddFittedSecondary(town, $"TownDetail_{index:00}_B", annexes[(index + 2) % annexes.Length], x + 64f, z - 64f, 135f, 26f, 24f, 1.1f, "town detail");
                if (index % 2 == 0)
                    AddFittedSecondary(town, $"TownDetail_{index:00}_C", "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Archway_01.prefab", x + 64f, z + 64f, 90f, 28f, 24f, 1.0f, "town detail");
                index++;
            }
        }

        foreach (float z in new[] { -720f, -520f, -320f, -120f, 80f, 280f, 480f, 680f })
        {
            AddFittedSecondary(town, $"TownStreetMarket_{z:0}_West", "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_ClothCover_02.prefab", -465f, z + 65f, 0f, 28f, 24f, 1.05f, "market dressing");
            AddFittedSecondary(town, $"TownStreetMarket_{z:0}_East", "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_ClothCover_03.prefab", -535f, z - 65f, 180f, 28f, 24f, 1.05f, "market dressing");
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
        foreach (float z in new[] { -740f, -500f, -260f, 0f, 240f, 500f, 740f })
        {
            foreach (float x in new[] { 340f, 560f, 780f })
            {
                AddFittedSecondary(baseRoot, $"BaseDetail_{index:00}_A", baseDetails[index % baseDetails.Length], x - 66f, z + 62f, 90f, 34f, 24f, 1.0f, "base detail");
                AddFittedSecondary(baseRoot, $"BaseDetail_{index:00}_B", baseDetails[(index + 3) % baseDetails.Length], x + 66f, z - 62f, 0f, 32f, 24f, 1.0f, "base detail");
                if (index % 3 == 0)
                    AddFittedSecondary(baseRoot, $"BaseDetail_{index:00}_C", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Pebbles_01.prefab", x + 66f, z + 62f, 20f, 24f, 20f, 1.0f, "base detail");
                index++;
            }
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

    private static void PlaceSoldierRouteProof(GameObject root)
    {
        GameObject units = Child(root, "Soldiers_OnValidatedWalkableStreets");
        Material blue = CreateMaterial("GC03_BlueRoute", new Color(0.04f, 0.16f, 0.95f, 1f));
        Material red = CreateMaterial("GC03_RedRoute", new Color(0.85f, 0.04f, 0.02f, 1f));
        Material yellow = CreateMaterial("GC03_RouteLine", new Color(0.95f, 0.75f, 0.05f, 1f));

        BuildRoute(units, "PlayerRoute_WalkableProof", new[]
        {
            new Vector3(-900f, 0f, -320f),
            new Vector3(-700f, 0f, -320f),
            new Vector3(-700f, 0f, 80f),
            new Vector3(-500f, 0f, 80f),
            new Vector3(-110f, 0f, 80f),
            new Vector3(-10f, 0f, 80f)
        }, yellow);

        BuildRoute(units, "EnemyRoute_WalkableProof", new[]
        {
            new Vector3(450f, 0f, 120f),
            new Vector3(230f, 0f, 120f),
            new Vector3(45f, 0f, 120f),
            new Vector3(0f, 0f, 80f)
        }, yellow);

        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab", new Vector3(-900f, 0f, -320f), 72f, blue, "player soldier 1");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab", new Vector3(-872f, 0f, -302f), 72f, blue, "player soldier 2");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_01.prefab", new Vector3(-872f, 0f, -338f), 72f, blue, "player soldier 3");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_02.prefab", new Vector3(-842f, 0f, -320f), 72f, blue, "player soldier 4");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab", new Vector3(450f, 0f, 120f), 252f, red, "enemy soldier 1");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_02.prefab", new Vector3(420f, 0f, 138f), 252f, red, "enemy soldier 2");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Female_01.prefab", new Vector3(420f, 0f, 102f), 252f, red, "enemy soldier 3");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_03.prefab", new Vector3(390f, 0f, 120f), 252f, red, "enemy soldier 4");
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

            ValidationLog.Add($"ERROR: skipped {placement.Name}; {role} footprint intersects walkable road. footprint={Format(footprint)}");
            return;
        }

        if (Buildings.Concat(Blockers).Any(zone => zone.Rect.Overlaps(footprint)))
        {
            if (role == "dressing" || optionalDetail)
            {
                PlacementLog.Add($"skipped optional {role}: {placement.Name} overlapped a placed footprint and was omitted. footprint={Format(footprint)}");
                return;
            }

            ValidationLog.Add($"ERROR: skipped {placement.Name}; {role} footprint overlaps placed footprint. footprint={Format(footprint)}");
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
        Camera map = CameraObject(root, "Camera_GC03_TopDownWalkability");
        map.orthographic = true;
        map.orthographicSize = 1030f;
        map.transform.position = new Vector3(0f, 1400f, 0f);
        map.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Camera town = CameraObject(root, "Camera_GC03_RtsTownRoute");
        town.orthographic = false;
        town.fieldOfView = 42f;
        town.transform.position = new Vector3(-690f, 112f, -520f);
        town.transform.LookAt(new Vector3(-615f, 0f, -135f));

        Camera baseView = CameraObject(root, "Camera_GC03_RtsBaseRoute");
        baseView.orthographic = false;
        baseView.fieldOfView = 42f;
        baseView.transform.position = new Vector3(480f, 112f, -230f);
        baseView.transform.LookAt(new Vector3(525f, 0f, 210f));

        Camera overview = CameraObject(root, "Camera_GC03_RtsCoverageOverview");
        overview.orthographic = false;
        overview.fieldOfView = 46f;
        overview.transform.position = new Vector3(-180f, 210f, -1040f);
        overview.transform.LookAt(new Vector3(-80f, 0f, 20f));

        Camera denseCity = CameraObject(root, "Camera_GC03_RtsDenseCityReview");
        denseCity.orthographic = false;
        denseCity.fieldOfView = 40f;
        denseCity.transform.position = new Vector3(-760f, 92f, -445f);
        denseCity.transform.LookAt(new Vector3(-560f, 0f, 90f));

        Camera denseBase = CameraObject(root, "Camera_GC03_RtsDenseBaseReview");
        denseBase.orthographic = false;
        denseBase.fieldOfView = 40f;
        denseBase.transform.position = new Vector3(310f, 92f, -530f);
        denseBase.transform.LookAt(new Vector3(590f, 0f, 120f));
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
            if (!Roads.Any(road => road.Rect.Overlaps(objective.Rect)))
                ValidationLog.Add($"ERROR: objective {objective.Name} is not connected to a road.");

        if (ValidationLog.Count == 0)
            ValidationLog.Add("PASS: GC03 expanded 2048 layout has no building/blocker overlap on walkable roads; spawns/objectives connect to road masks; proof soldiers are on walkable streets.");
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
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (camera.name == "Camera_GC03_TopDownWalkability")
                Render(camera, ProjectPath(CaptureRoot + "/gc03_topdown_walkability_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC03_RtsTownRoute")
                Render(camera, ProjectPath(CaptureRoot + "/gc03_rts_town_route_soldiers_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC03_RtsBaseRoute")
                Render(camera, ProjectPath(CaptureRoot + "/gc03_rts_base_route_soldiers_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC03_RtsCoverageOverview")
                Render(camera, ProjectPath(CaptureRoot + "/gc03_rts_2048_coverage_overview_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC03_RtsDenseCityReview")
                Render(camera, ProjectPath(CaptureRoot + "/gc03_rts_dense_city_review_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC03_RtsDenseBaseReview")
                Render(camera, ProjectPath(CaptureRoot + "/gc03_rts_dense_base_review_1920x1080.png"), 1920, 1080);
        }
    }

    private static void WriteReport()
    {
        StringBuilder report = new();
        report.AppendLine("# GC03 Playable City 2048 Layout");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Expand GC03 from a small playable skeleton into a 2048-scale road-first city/base layout with explicit walkable masks, legal footprints, soldier route proofs, coverage metrics, and validation.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Design/Architecture/gameplay_playable_scene_generation_workflow.md`");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGc03PlayableCityBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC03_PlayableCity_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Data/GeneratedScenes/GC03_PlayableCity_2048/gc03_prefab_footprint_catalog.json`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC03_PlayableCity_2048/gc03_topdown_walkability_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC03_PlayableCity_2048/gc03_rts_town_route_soldiers_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC03_PlayableCity_2048/gc03_rts_base_route_soldiers_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC03_PlayableCity_2048/gc03_rts_2048_coverage_overview_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC03_PlayableCity_2048/gc03_rts_dense_city_review_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC03_PlayableCity_2048/gc03_rts_dense_base_review_1920x1080.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: Gameplay playable scene generation workflow contract.");
        report.AppendLine("User-visible behavior: none in shipped flow; generated scene is available for PM/gameplay review.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureGc03PlayableCityBuilder.BuildGc03PlayableCity2048`.");
        report.AppendLine("Validation result: " + (ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)) ? "failed with layout errors; see validation log below." : "passed expanded 2048 road-first footprint validation."));
        report.AppendLine("Known gaps: GC03 now has measured prefab bounds and denser legal lot filling, but composition still needs a final art-direction pass using authored Demo-scene clusters for more deliberate landmarks and street dressing.");
        report.AppendLine("Cross-lane impacts: PM/Design can review the workflow and proof captures; runtime ECS flow and UI are untouched.");
        report.AppendLine("Next recommended task: convert the best Demo-scene building clusters into reusable block modules, then let GC03 place modules instead of mostly individual prefabs.");
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
            instance.name = "GC03_CatalogMeasure_" + Path.GetFileNameWithoutExtension(path);
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
            generatedBy = nameof(WarlineCaptureGc03PlayableCityBuilder),
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
