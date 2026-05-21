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

public static class WarlineCaptureGc19DenseBlockerVisualBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC19_DenseBlockerVisual_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC19_DenseBlockerVisual_2048";
    private const string ReportPath = "Design/AgentReports/2026-05-21_gameplay_gc19-dense-blocker-visual.md";
    private const string DataPath = "Design/AgentReports/Data/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_dense_blocker_visual_contract.json";
    private const float MapSize = 2048f;

    private static readonly List<Zone> MacroRoads = new();
    private static readonly List<Zone> SoldierLocalZones = new();
    private static readonly List<Zone> VehicleZones = new();
    private static readonly List<Zone> BlockerZones = new();
    private static readonly List<Placement> Placements = new();
    private static readonly List<string> MissingAssets = new();
    private static readonly List<string> ValidationLog = new();
    private static readonly List<string> SkippedVisuals = new();
    private static int mainBlockerPlacements;
    private static int detailBlockerPlacements;

    private readonly struct Zone
    {
        public readonly string Name;
        public readonly Rect Rect;
        public readonly string Kind;

        public Zone(string name, Rect rect, string kind)
        {
            Name = name;
            Rect = rect;
            Kind = kind;
        }
    }

    private readonly struct Placement
    {
        public readonly string Name;
        public readonly string Path;
        public readonly Vector3 Position;
        public readonly Vector2 Footprint;
        public readonly float RotationY;
        public readonly float Scale;
        public readonly string ZoneName;

        public Placement(string name, string path, Vector3 position, Vector2 footprint, float rotationY, float scale, string zoneName)
        {
            Name = name;
            Path = path;
            Position = position;
            Footprint = footprint;
            RotationY = rotationY;
            Scale = scale;
            ZoneName = zoneName;
        }
    }

    [Serializable]
    private sealed class ContractExport
    {
        public string generatedBy;
        public string scene;
        public List<ZoneExport> zones = new();
        public List<PlacementExport> placements = new();
    }

    [Serializable]
    private sealed class ZoneExport
    {
        public string name;
        public string kind;
        public float centerX;
        public float centerZ;
        public float width;
        public float depth;
    }

    [Serializable]
    private sealed class PlacementExport
    {
        public string name;
        public string prefab;
        public string zone;
        public float x;
        public float z;
        public float width;
        public float depth;
    }

    [MenuItem("WarlineCapture/Design/Build GC19 Dense Blocker Visual 2048")]
    public static void BuildGc19DenseBlockerVisual2048()
    {
        MacroRoads.Clear();
        SoldierLocalZones.Clear();
        VehicleZones.Clear();
        BlockerZones.Clear();
        Placements.Clear();
        MissingAssets.Clear();
        ValidationLog.Clear();
        SkippedVisuals.Clear();
        mainBlockerPlacements = 0;
        detailBlockerPlacements = 0;

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(DataPath)));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        GameObject root = new("GC19_DenseBlockerVisual_2048_Root");
        BuildEnvironment(root);
        DefineContract();
        BuildGroundAndRoads(root);
        PlaceVisualsFromContract(root);
        PlaceProofUnits(root);
        BuildDebugOverlay(root);
        BuildCameras(root);
        Validate();

        EditorSceneManager.SaveScene(scene, ScenePath);
        CaptureScene();
        WriteContractJson();
        WriteReport();
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GC19_DENSE_BLOCKER_VISUAL_BUILT scene={ScenePath} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static void DefineContract()
    {
        AddMacroRoad("MacroRoad_MainNorthSouth_West", 258f, 0f, 60f, 890f);
        AddMacroRoad("MacroRoad_MainNorthSouth_East", 440f, 0f, 58f, 890f);
        AddMacroRoad("MacroRoad_NorthCityToAirfield", 62f, 224f, 766f, 54f);
        AddMacroRoad("MacroRoad_CentralCityToCommand", 130f, 426f, 686f, 54f);
        AddMacroRoad("MacroRoad_SouthPlayerToFuel", 38f, 578f, 790f, 86f);

        AddCity("CityCore_NorthWest", 58f, 68f, 142f, 136f, true);
        AddCity("CityCentral_WestInner", 304f, 84f, 108f, 118f, false);
        AddCity("CityMarket_West", 102f, 320f, 150f, 136f, true);
        AddCity("CitySouth_West", 76f, 714f, 150f, 136f, true);
        AddCamp("SouthGate_PlayerCamp", 64f, 686f, 94f, 102f);
        AddCamp("CentralTentBarracks", 310f, 500f, 104f, 96f);
        AddCamp("WestTentBarracks", 350f, 724f, 102f, 102f);
        AddCamp("Airfield_Apron", 718f, 114f, 118f, 148f);
        AddCamp("CommandDepot_CentralEast", 548f, 478f, 114f, 94f);
        AddCamp("VehicleYard_NorthEast", 596f, 286f, 88f, 82f);
        AddCamp("VehicleYard_SouthEast", 656f, 666f, 112f, 112f);
        AddCamp("FuelUtility_East", 754f, 558f, 72f, 104f);
    }

    private static void AddMacroRoad(string name, float x, float y, float width, float height)
    {
        MacroRoads.Add(new Zone(name, BlueprintRect(x, y, width, height), "MacroRoadWalkable"));
    }

    private static void AddCity(string name, float x, float y, float width, float height, bool vehiclePocket)
    {
        SoldierLocalZones.Add(new Zone(name + "_SoldierLocal", BlueprintRect(x, y, width, height), "SoldierLocalWalkable"));
        if (vehiclePocket)
            VehicleZones.Add(new Zone(name + "_VehiclePocket", BlueprintRect(x + width * 0.35f, y + height * 0.36f, width * 0.30f, height * 0.28f), "VehicleWalkable"));

        AddBlocker(name + "_Building_NW", x + width * 0.08f, y + height * 0.08f, width * 0.28f, height * 0.30f);
        AddBlocker(name + "_Building_NE", x + width * 0.62f, y + height * 0.08f, width * 0.28f, height * 0.30f);
        AddBlocker(name + "_Building_SW", x + width * 0.08f, y + height * 0.62f, width * 0.28f, height * 0.28f);
        AddBlocker(name + "_Building_SE", x + width * 0.62f, y + height * 0.62f, width * 0.28f, height * 0.28f);
    }

    private static void AddCamp(string name, float x, float y, float width, float height)
    {
        SoldierLocalZones.Add(new Zone(name + "_SoldierYard", BlueprintRect(x, y, width, height), "SoldierLocalWalkable"));
        VehicleZones.Add(new Zone(name + "_VehicleYard", BlueprintRect(x + width * 0.16f, y + height * 0.20f, width * 0.68f, height * 0.58f), "VehicleWalkable"));
        AddBlocker(name + "_Static_NW", x + width * 0.08f, y + height * 0.10f, width * 0.30f, height * 0.26f);
        AddBlocker(name + "_Static_SE", x + width * 0.60f, y + height * 0.62f, width * 0.30f, height * 0.26f);
        AddBlocker(name + "_VehicleStaticProp", x + width * 0.38f, y + height * 0.34f, width * 0.24f, height * 0.22f);
    }

    private static void AddBlocker(string name, float x, float y, float width, float height)
    {
        BlockerZones.Add(new Zone(name, BlueprintRect(x, y, width, height), "Blocker"));
    }

    private static void BuildEnvironment(GameObject root)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.92f, 0.84f, 0.68f, 1f);
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.68f, 0.58f, 0.42f, 1f);
        RenderSettings.fogDensity = 0.00016f;

        Light key = Child(root, "DirectionalLight_Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 2.05f;
        key.color = new Color(1f, 0.9f, 0.72f, 1f);
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.35f;
        key.transform.rotation = Quaternion.Euler(50f, -42f, 0f);

        Light fill = Child(root, "DirectionalLight_Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.52f;
        fill.color = new Color(0.62f, 0.76f, 1f, 1f);
        fill.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(34f, 138f, 0f);

        Volume volume = Child(root, "GC19_RTS_PresentationVolume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = profile;
        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.32f);
        color.contrast.Override(5f);
        color.saturation.Override(4f);
        color.colorFilter.Override(new Color(1f, 0.97f, 0.88f, 1f));
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
    }

    private static void BuildGroundAndRoads(GameObject root)
    {
        Material baseMat = CreateMaterial("GC19_SandBase", new Color(0.61f, 0.50f, 0.31f, 1f));
        Material roadMat = CreateMaterial("GC19_AuthoredCompactedRoad", new Color(0.64f, 0.53f, 0.35f, 1f));
        Material shoulderMat = CreateMaterial("GC19_AuthoredRoadShoulder", new Color(0.53f, 0.43f, 0.27f, 1f));
        Material trackMat = CreateMaterial("GC19_DirtTireTrack", new Color(0.42f, 0.32f, 0.18f, 1f));
        Material runwayMat = CreateMaterial("GC19_RunwayWeatheredAsphalt", new Color(0.36f, 0.35f, 0.31f, 1f));
        Material plazaMat = CreateMaterial("GC19_LocalPackedPlaza", new Color(0.70f, 0.58f, 0.37f, 1f));
        Material vehicleMat = CreateMaterial("GC19_VehicleYardDust", new Color(0.55f, 0.46f, 0.33f, 1f));

        Surface(root, "FlatGameplayBase_2048", Vector3.zero, new Vector2(MapSize, MapSize), baseMat, -0.04f);

        GameObject local = Child(root, "AuthoredLocalYards_FromWalkabilityContract");
        foreach (Zone zone in SoldierLocalZones)
            Surface(local, zone.Name, Center(zone.Rect, 0.015f), new Vector2(zone.Rect.width, zone.Rect.height), plazaMat, 0.015f);
        foreach (Zone zone in VehicleZones)
            Surface(local, zone.Name, Center(zone.Rect, 0.026f), new Vector2(zone.Rect.width, zone.Rect.height), vehicleMat, 0.026f);

        GameObject roads = Child(root, "AuthoredRoadMeshes_VisualOnly_PreserveGc17Masks");
        SurfaceBlueprintPolygon(roads, "MainNorthSouth_West_Shoulder", shoulderMat, 0.036f, (238f, -4f), (338f, -4f), (306f, 894f), (206f, 894f));
        SurfaceBlueprintPolygon(roads, "MainNorthSouth_West", roadMat, 0.052f, (258f, 0f), (318f, 0f), (286f, 890f), (226f, 890f));
        SurfaceBlueprintPolygon(roads, "MainNorthSouth_East_Shoulder", shoulderMat, 0.036f, (420f, -4f), (518f, -4f), (488f, 894f), (390f, 894f));
        SurfaceBlueprintPolygon(roads, "MainNorthSouth_East", roadMat, 0.052f, (440f, 0f), (498f, 0f), (468f, 890f), (410f, 890f));

        SurfaceBlueprintPolygon(roads, "NorthCityToAirfield_Shoulder", shoulderMat, 0.036f, (48f, 226f), (844f, 198f), (850f, 304f), (50f, 332f));
        SurfaceBlueprintPolygon(roads, "NorthCityToAirfield", roadMat, 0.052f, (62f, 250f), (824f, 224f), (828f, 278f), (66f, 304f));
        SurfaceBlueprintPolygon(roads, "CentralCityToCommand_Shoulder", shoulderMat, 0.036f, (112f, 400f), (358f, 388f), (628f, 322f), (838f, 322f), (838f, 426f), (652f, 426f), (380f, 494f), (116f, 508f));
        SurfaceBlueprintPolygon(roads, "CentralCityToCommand", roadMat, 0.052f, (130f, 426f), (360f, 414f), (632f, 348f), (816f, 350f), (816f, 402f), (646f, 400f), (374f, 466f), (134f, 480f));
        SurfaceBlueprintPolygon(roads, "SouthPlayerToFuel_Shoulder", shoulderMat, 0.036f, (26f, 588f), (248f, 552f), (518f, 448f), (840f, 412f), (850f, 524f), (544f, 558f), (274f, 660f), (28f, 698f));
        SurfaceBlueprintPolygon(roads, "SouthPlayerToFuel", roadMat, 0.052f, (38f, 612f), (252f, 578f), (520f, 474f), (820f, 442f), (828f, 496f), (538f, 528f), (270f, 632f), (44f, 668f));

        SurfaceBlueprintPolygon(roads, "Airfield_Runway_Primary", runwayMat, 0.066f, (744f, 92f), (818f, 92f), (818f, 296f), (744f, 296f));
        SurfaceBlueprintPolygon(roads, "Airfield_Runway_Diagonal", runwayMat, 0.065f, (770f, 30f), (816f, 42f), (770f, 270f), (724f, 258f));
        SurfaceBlueprintPolygon(roads, "Market_Objective_Plaza", plazaMat, 0.064f, (356f, 238f), (490f, 238f), (490f, 290f), (356f, 290f));
        SurfaceBlueprintPolygon(roads, "South_Objective_Plaza", plazaMat, 0.064f, (340f, 706f), (474f, 706f), (474f, 762f), (340f, 762f));
        SurfaceBlueprintPolygon(roads, "Command_Outpost_Yard", plazaMat, 0.063f, (512f, 356f), (650f, 350f), (664f, 512f), (536f, 540f), (492f, 438f));
        SurfaceBlueprintPolygon(roads, "VehicleFuel_Yard", vehicleMat, 0.063f, (566f, 574f), (800f, 552f), (836f, 650f), (810f, 814f), (640f, 842f), (558f, 762f));

        SurfaceBlueprintPolygon(roads, "Track_PlayerToCommand_A", trackMat, 0.082f, (48f, 630f), (252f, 596f), (520f, 492f), (818f, 460f), (820f, 474f), (524f, 508f), (258f, 612f), (52f, 646f));
        SurfaceBlueprintPolygon(roads, "Track_NorthRoad_A", trackMat, 0.082f, (70f, 262f), (824f, 236f), (824f, 248f), (70f, 274f));
        SurfaceBlueprintPolygon(roads, "Track_CommandRoad_A", trackMat, 0.082f, (138f, 436f), (360f, 424f), (632f, 358f), (812f, 360f), (812f, 372f), (636f, 370f), (364f, 438f), (140f, 450f));
    }

    private static void PlaceVisualsFromContract(GameObject root)
    {
        GameObject visuals = Child(root, "DenseVisuals_PlacedInsideBlockerZones_NotWalkableZones");
        int index = 0;
        foreach (Zone blocker in BlockerZones)
        {
            if (MacroRoads.Any(road => road.Rect.Overlaps(blocker.Rect)))
            {
                SkippedVisuals.Add($"{blocker.Name}: skipped because blocker anchor overlaps macro road.");
                continue;
            }

            string path = PrefabFor(blocker, index);
            Vector2 footprint = new(blocker.Rect.width, blocker.Rect.height);
            PlacePrefab(visuals, blocker.Name + "_Primary", path, Center(blocker.Rect, 0f), footprint, RotationFor(index), ScaleFor(blocker, path), blocker.Name);
            mainBlockerPlacements++;
            PlaceDenseDetailsForBlocker(visuals, blocker, index);
            index++;
        }

        GameObject detail = Child(root, "LightDressing_OutsideWalkableLanes");
        PlaceDressing(detail, "Rock_NorthEdge_01", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_04.prefab", BlueprintPoint(508f, 102f), 18f, 1.2f, new Vector2(34f, 28f));
        PlaceDressing(detail, "Rock_CommandEdge_01", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_02.prefab", BlueprintPoint(690f, 386f), -18f, 1.0f, new Vector2(28f, 24f));
        PlaceDressing(detail, "Palm_MarketEdge_01", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_01.prefab", BlueprintPoint(270f, 350f), 0f, 1.0f, new Vector2(22f, 22f));
        PlaceDressing(detail, "Palm_SouthEdge_01", "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_02.prefab", BlueprintPoint(258f, 756f), 0f, 1.0f, new Vector2(22f, 22f));
    }

    private static void PlaceDenseDetailsForBlocker(GameObject parent, Zone blocker, int index)
    {
        List<DetailSpec> details = DetailSpecsFor(blocker, index);
        foreach (DetailSpec detail in details)
        {
            Vector3 position = DetailPosition(blocker.Rect, detail.NormalizedOffset);
            Rect footprint = CenterRect(position.x, position.z, detail.Footprint.x, detail.Footprint.y);
            if (!Contains(blocker.Rect, footprint))
            {
                SkippedVisuals.Add($"{blocker.Name}_{detail.Name}: skipped because detail footprint would leak outside blocker.");
                continue;
            }
            if (MacroRoads.Any(road => road.Rect.Overlaps(footprint)))
            {
                SkippedVisuals.Add($"{blocker.Name}_{detail.Name}: skipped because detail footprint would overlap macro road.");
                continue;
            }

            PlacePrefab(parent, blocker.Name + "_" + detail.Name, detail.Path, position, detail.Footprint, detail.RotationY, detail.Scale * 1.65f, blocker.Name);
            detailBlockerPlacements++;
        }
    }

    private readonly struct DetailSpec
    {
        public readonly string Name;
        public readonly string Path;
        public readonly Vector2 NormalizedOffset;
        public readonly Vector2 Footprint;
        public readonly float RotationY;
        public readonly float Scale;

        public DetailSpec(string name, string path, Vector2 normalizedOffset, Vector2 footprint, float rotationY, float scale)
        {
            Name = name;
            Path = path;
            NormalizedOffset = normalizedOffset;
            Footprint = footprint;
            RotationY = rotationY;
            Scale = scale;
        }
    }

    private static List<DetailSpec> DetailSpecsFor(Zone blocker, int index)
    {
        string name = blocker.Name;
        if (name.Contains("Airfield", StringComparison.Ordinal))
            return new List<DetailSpec>
            {
                Detail("RunwayLight_A", "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Runway_Light_01.prefab", -0.24f, -0.20f, 8f, 8f, 0f, 1.1f),
                Detail("RunwayBarrier_B", "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Runway_Barrier_01.prefab", 0.24f, 0.20f, 14f, 10f, 90f, 1.0f),
                Detail("PalletStack_C", "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Pallet_02.prefab", 0.0f, 0.30f, 12f, 10f, 14f, 1.0f),
            };
        if (name.Contains("Vehicle", StringComparison.Ordinal))
            return new List<DetailSpec>
            {
                Detail("Barrier_A", "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Barrier_01.prefab", -0.26f, 0.24f, 14f, 8f, 0f, 1.0f),
                Detail("Pallet_B", "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Pallet_02.prefab", 0.24f, -0.24f, 12f, 10f, 70f, 1.0f),
                Detail("Cone_C", "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Cone_01.prefab", 0.0f, 0.30f, 6f, 6f, 0f, 1.0f),
            };
        if (name.Contains("Fuel", StringComparison.Ordinal))
            return new List<DetailSpec>
            {
                Detail("GasPump_A", "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Gaspump_01.prefab", -0.18f, -0.18f, 10f, 10f, 0f, 1.0f),
                Detail("GasTank_B", "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Gastank_01.prefab", 0.22f, 0.20f, 12f, 10f, 28f, 1.0f),
                Detail("Pipe_C", "Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_Pipe_Small_Straight_01.prefab", 0.0f, 0.32f, 16f, 8f, 90f, 0.85f),
            };
        if (name.Contains("Tent", StringComparison.Ordinal) || name.Contains("Camp", StringComparison.Ordinal) || name.Contains("Depot", StringComparison.Ordinal))
            return new List<DetailSpec>
            {
                Detail("Sandbags_A", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Sandbags_01.prefab", -0.25f, -0.22f, 18f, 12f, 0f, 0.72f),
                Detail("Generator_B", "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Generator_Small_01.prefab", 0.25f, 0.20f, 12f, 10f, 35f, 0.9f),
                Detail("CamoNet_C", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_CamoNet_Tent_01.prefab", 0.0f, 0.30f, 18f, 14f, 90f, 0.75f),
            };

        return new List<DetailSpec>
        {
            Detail("Fence_A", index % 2 == 0 ? "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Fence_01.prefab" : "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Fence_03.prefab", -0.28f, -0.24f, 18f, 8f, 0f, 0.9f),
            Detail("Cover_B", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Cover_01.prefab", 0.25f, -0.18f, 16f, 12f, 90f, 0.8f),
            Detail("Planter_C", "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Planter_01.prefab", -0.18f, 0.26f, 10f, 10f, 25f, 0.9f),
            Detail("Cart_D", "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Cart_Wood_01.prefab", 0.25f, 0.24f, 12f, 10f, 65f, 0.9f),
        };
    }

    private static DetailSpec Detail(string name, string path, float offsetX, float offsetY, float width, float depth, float rotationY, float scale)
    {
        return new DetailSpec(name, path, new Vector2(offsetX, offsetY), new Vector2(width, depth), rotationY, scale);
    }

    private static Vector3 DetailPosition(Rect rect, Vector2 normalizedOffset)
    {
        return new Vector3(rect.center.x + rect.width * normalizedOffset.x, 0f, rect.center.y + rect.height * normalizedOffset.y);
    }

    private static string PrefabFor(Zone blocker, int index)
    {
        string name = blocker.Name;
        if (name.Contains("Airfield", StringComparison.Ordinal))
            return index % 2 == 0 ? "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hangar_01.prefab" : "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GuardTower_01.prefab";
        if (name.Contains("Vehicle", StringComparison.Ordinal) || name.Contains("Fuel", StringComparison.Ordinal))
            return index % 3 == 0 ? "Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab" : "Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Canopy.prefab";
        if (name.Contains("Tent", StringComparison.Ordinal) || name.Contains("Camp", StringComparison.Ordinal) || name.Contains("Depot", StringComparison.Ordinal))
            return index % 2 == 0 ? "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Desert_01.prefab" : "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab";
        return (index % 4) switch
        {
            0 => "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_01.prefab",
            1 => "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_03.prefab",
            2 => "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_09.prefab",
            _ => "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_11.prefab",
        };
    }

    private static float RotationFor(int index)
    {
        return (index % 4) * 90f + (index % 2 == 0 ? 0f : 8f);
    }

    private static float ScaleFor(Zone blocker, string path)
    {
        float min = Mathf.Min(blocker.Rect.width, blocker.Rect.height);
        if (path.Contains("/Vehicles/", StringComparison.Ordinal))
            return Mathf.Clamp(min / 30f, 1.05f, 1.65f);
        if (path.Contains("Hangar", StringComparison.Ordinal))
            return Mathf.Clamp(min / 44f, 0.95f, 1.7f);
        return Mathf.Clamp(min / 34f, 0.95f, 1.55f);
    }

    private static void PlacePrefab(GameObject parent, string name, string path, Vector3 position, Vector2 footprint, float rotationY, float scale, string zoneName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            MissingAssets.Add(path);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "GC19_" + name;
        instance.transform.SetParent(parent.transform, true);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        instance.transform.localScale = Vector3.one * scale;
        AlignBottomNearGround(instance);
        foreach (LODGroup lod in instance.GetComponentsInChildren<LODGroup>(false))
        {
            lod.ForceLOD(0);
            lod.enabled = false;
        }
        Placements.Add(new Placement(instance.name, path, position, footprint, rotationY, scale, zoneName));
    }

    private static void PlaceDressing(GameObject parent, string name, string path, Vector3 position, float rotationY, float scale, Vector2 footprint)
    {
        if (TouchesWalkable(position, footprint))
        {
            SkippedVisuals.Add($"{name}: skipped because dressing footprint would overlap walkable space.");
            return;
        }
        PlacePrefab(parent, name, path, position, footprint, rotationY, scale, "Dressing");
    }

    private static void PlaceProofUnits(GameObject root)
    {
        GameObject units = Child(root, "ProofUnits_OnWalkableOnly");
        PlaceUnit(units, "PlayerSoldier_MarketLane", "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab", BlueprintPoint(176f, 392f), 70f);
        PlaceUnit(units, "PlayerAPC_MacroRoad", "Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Heavy.prefab", BlueprintPoint(390f, 552f), 72f);
        PlaceUnit(units, "EnemySoldier_AirfieldYard", "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab", BlueprintPoint(810f, 150f), 246f);
        PlaceUnit(units, "EnemyTank_VehicleYard", "Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab", BlueprintPoint(680f, 720f), 246f);
    }

    private static void PlaceUnit(GameObject parent, string name, string path, Vector3 position, float rotationY)
    {
        PlacePrefab(parent, name, path, position, new Vector2(12f, 12f), rotationY, path.Contains("/Vehicles/", StringComparison.Ordinal) ? 1f : 1.35f, "ProofUnit");
        Vector2 point = new(position.x, position.z);
        if (!MacroRoads.Concat(SoldierLocalZones).Concat(VehicleZones).Any(zone => zone.Rect.Contains(point)))
            ValidationLog.Add($"ERROR: proof unit {name} is not on walkable space.");
        if (BlockerZones.Any(zone => zone.Rect.Contains(point)))
            ValidationLog.Add($"ERROR: proof unit {name} intersects blocker space.");
    }

    private static void BuildDebugOverlay(GameObject root)
    {
        GameObject debug = Child(root, "HiddenWalkabilityDebugOverlay");
        debug.SetActive(false);
        Material green = CreateMaterial("GC19_DebugSoldierWalkable", new Color(0f, 1f, 0.1f, 0.6f));
        Material cyan = CreateMaterial("GC19_DebugVehicleWalkable", new Color(0f, 0.8f, 1f, 0.6f));
        Material red = CreateMaterial("GC19_DebugBlocker", new Color(1f, 0f, 0f, 0.7f));
        foreach (Zone zone in SoldierLocalZones)
            Surface(debug, zone.Name, Center(zone.Rect, 0.2f), new Vector2(zone.Rect.width, zone.Rect.height), green, 0.2f);
        foreach (Zone zone in VehicleZones)
            Surface(debug, zone.Name, Center(zone.Rect, 0.22f), new Vector2(zone.Rect.width, zone.Rect.height), cyan, 0.22f);
        foreach (Zone zone in BlockerZones)
            Surface(debug, zone.Name, Center(zone.Rect, 0.24f), new Vector2(zone.Rect.width, zone.Rect.height), red, 0.24f);
    }

    private static void Validate()
    {
        foreach (Placement placement in Placements)
        {
            Rect footprint = CenterRect(placement.Position.x, placement.Position.z, placement.Footprint.x, placement.Footprint.y);
            if (placement.ZoneName != "ProofUnit" && placement.ZoneName != "Dressing")
            {
                Zone blocker = BlockerZones.FirstOrDefault(zone => zone.Name == placement.ZoneName);
                if (blocker.Name == null || !Expanded(blocker.Rect, 2f).Overlaps(footprint))
                    ValidationLog.Add($"ERROR: placement {placement.Name} is not anchored to blocker zone {placement.ZoneName}.");
                if (blocker.Name != null && !Contains(Expanded(blocker.Rect, 1f), footprint))
                    ValidationLog.Add($"ERROR: placement {placement.Name} leaks outside blocker zone {placement.ZoneName}.");
            }

            if (placement.ZoneName != "ProofUnit" && MacroRoads.Any(zone => zone.Rect.Overlaps(footprint)))
                ValidationLog.Add($"ERROR: placement {placement.Name} overlaps a macro road.");
        }

        foreach (Zone blocker in BlockerZones)
            if (!SoldierLocalZones.Any(zone => zone.Rect.Overlaps(blocker.Rect)) && !VehicleZones.Any(zone => zone.Rect.Overlaps(blocker.Rect)))
                ValidationLog.Add($"ERROR: blocker {blocker.Name} is not inside a local gameplay area.");

        if (MissingAssets.Count > 0)
            ValidationLog.Add($"ERROR: missing {MissingAssets.Distinct(StringComparer.Ordinal).Count()} prefab assets.");

        if (detailBlockerPlacements < 60)
            ValidationLog.Add($"ERROR: GC19 placed only {detailBlockerPlacements} legal detail props; densification target is at least 60.");

        if (ValidationLog.Count == 0)
            ValidationLog.Add($"PASS: GC19 preserved the GC17/GC18 walkability masks and densified legal blocker zones with {detailBlockerPlacements} additional blocker detail props plus {mainBlockerPlacements} primary blocker visuals. Total visual placements: {Placements.Count}; blocker zones: {BlockerZones.Count}; soldier zones: {SoldierLocalZones.Count}; vehicle zones: {VehicleZones.Count}; macro roads: {MacroRoads.Count}.");
    }

    private static bool TouchesWalkable(Vector3 position, Vector2 footprint)
    {
        Rect rect = CenterRect(position.x, position.z, footprint.x, footprint.y);
        return MacroRoads.Concat(SoldierLocalZones).Concat(VehicleZones).Any(zone => zone.Rect.Overlaps(rect));
    }

    private static void BuildCameras(GameObject root)
    {
        BuildRtsCamera(root, "Camera_GC19_RtsOverview", new Vector3(-620f, 520f, -820f), BlueprintPoint(388f, 430f), 38f);
        BuildRtsCamera(root, "Camera_GC19_RtsCityReadable", new Vector3(-930f, 330f, -430f), BlueprintPoint(172f, 366f), 34f);
        BuildRtsCamera(root, "Camera_GC19_RtsAirfieldCommand", new Vector3(260f, 430f, -620f), BlueprintPoint(660f, 320f), 36f);
        BuildRtsCamera(root, "Camera_GC19_RtsDenseCityClose", new Vector3(-700f, 210f, -250f), BlueprintPoint(176f, 392f), 30f);

        Camera top = CameraObject(root, "Camera_GC19_TopDownContractVisualProof");
        top.orthographic = true;
        top.orthographicSize = 1035f;
        top.transform.position = new Vector3(0f, 1600f, 0f);
        top.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static void BuildRtsCamera(GameObject root, string name, Vector3 position, Vector3 target, float fov)
    {
        Camera camera = CameraObject(root, name);
        camera.fieldOfView = fov;
        camera.transform.position = position;
        camera.transform.LookAt(target);
    }

    private static Camera CameraObject(GameObject root, string name)
    {
        GameObject cameraObject = Child(root, name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.62f, 0.50f, 0.33f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 4000f;
        UniversalAdditionalCameraData data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        return camera;
    }

    private static void CaptureScene()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (camera.name == "Camera_GC19_RtsOverview")
                Render(camera, ProjectPath(CaptureRoot + "/gc19_rts_overview_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC19_RtsCityReadable")
                Render(camera, ProjectPath(CaptureRoot + "/gc19_rts_city_readable_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC19_RtsAirfieldCommand")
                Render(camera, ProjectPath(CaptureRoot + "/gc19_rts_airfield_command_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC19_RtsDenseCityClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc19_rts_dense_city_close_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC19_TopDownContractVisualProof")
                RenderContractProof(camera, ProjectPath(CaptureRoot + "/gc19_topdown_contract_visual_proof_2048x2048.png"));
        }
    }

    private static void RenderContractProof(Camera camera, string path)
    {
        GameObject overlay = FindSceneObjectIncludingInactive("HiddenWalkabilityDebugOverlay");
        GameObject visuals = FindSceneObjectIncludingInactive("DenseVisuals_PlacedInsideBlockerZones_NotWalkableZones");
        GameObject dressing = FindSceneObjectIncludingInactive("LightDressing_OutsideWalkableLanes");
        GameObject units = FindSceneObjectIncludingInactive("ProofUnits_OnWalkableOnly");

        bool overlayWasActive = overlay != null && overlay.activeSelf;
        bool visualsWereActive = visuals != null && visuals.activeSelf;
        bool dressingWasActive = dressing != null && dressing.activeSelf;
        bool unitsWereActive = units != null && units.activeSelf;

        if (overlay != null)
            overlay.SetActive(true);
        if (visuals != null)
            visuals.SetActive(false);
        if (dressing != null)
            dressing.SetActive(false);
        if (units != null)
            units.SetActive(false);

        Render(camera, path, 2048, 2048);

        if (overlay != null)
            overlay.SetActive(overlayWasActive);
        if (visuals != null)
            visuals.SetActive(visualsWereActive);
        if (dressing != null)
            dressing.SetActive(dressingWasActive);
        if (units != null)
            units.SetActive(unitsWereActive);
    }

    private static GameObject FindSceneObjectIncludingInactive(string name)
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go => go.name == name && go.scene.IsValid());
    }

    private static void WriteContractJson()
    {
        ContractExport export = new()
        {
            generatedBy = nameof(WarlineCaptureGc19DenseBlockerVisualBuilder),
            scene = ScenePath
        };
        foreach (Zone zone in MacroRoads.Concat(SoldierLocalZones).Concat(VehicleZones).Concat(BlockerZones))
            export.zones.Add(new ZoneExport { name = zone.Name, kind = zone.Kind, centerX = zone.Rect.center.x, centerZ = zone.Rect.center.y, width = zone.Rect.width, depth = zone.Rect.height });
        foreach (Placement placement in Placements)
            export.placements.Add(new PlacementExport { name = placement.Name, prefab = placement.Path, zone = placement.ZoneName, x = placement.Position.x, z = placement.Position.z, width = placement.Footprint.x, depth = placement.Footprint.y });
        File.WriteAllText(ProjectPath(DataPath), JsonUtility.ToJson(export, true), Encoding.UTF8);
    }

    private static void WriteReport()
    {
        StringBuilder report = new();
        report.AppendLine("# GC19 Dense Blocker Visual Scene");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Continue from GC18 by densifying city and military-base blocker footprints with legal detail props while preserving the GC17/GC18 walkability masks and authored road frame.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGc19DenseBlockerVisualBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC19_DenseBlockerVisual_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Data/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_dense_blocker_visual_contract.json`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_rts_overview_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_rts_city_readable_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_rts_airfield_command_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_rts_dense_city_close_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_topdown_contract_visual_proof_2048x2048.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: GC17 walkability visual contract remains the source of truth; GC19 adds visuals only inside blocker zones.");
        report.AppendLine("User-visible behavior: no shipped runtime behavior changed; generated scene gives denser city/base blocker clusters for PM review.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureGc19DenseBlockerVisualBuilder.BuildGc19DenseBlockerVisual2048`.");
        report.AppendLine("Validation result: " + (ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)) ? "failed; see validation log below." : "passed dense-blocker visual validation."));
        report.AppendLine("Known gaps: This is still a generated visual pass, not final art quality. Whole authored Demo modules are still not promoted because their internal walkability is opaque.");
        report.AppendLine("Cross-lane impacts: Art/Design can review whether blocker footprints are large enough for the target visual density without sacrificing RTS walkability.");
        report.AppendLine("Next recommended task: GC20 should either enlarge/reshape accepted blocker footprints for better city massing or convert selected Demo-authored modules into explicit blocker masks.");
        report.AppendLine();
        report.AppendLine("Counts:");
        report.AppendLine($"- macro roads: {MacroRoads.Count}");
        report.AppendLine($"- soldier local zones: {SoldierLocalZones.Count}");
        report.AppendLine($"- vehicle zones: {VehicleZones.Count}");
        report.AppendLine($"- blocker zones: {BlockerZones.Count}");
        report.AppendLine($"- visual placements: {Placements.Count}");
        report.AppendLine($"- primary blocker visuals: {mainBlockerPlacements}");
        report.AppendLine($"- dense blocker detail props: {detailBlockerPlacements}");
        report.AppendLine($"- skipped road-conflict visuals: {SkippedVisuals.Count}");
        report.AppendLine();
        report.AppendLine("Validation log:");
        foreach (string line in ValidationLog)
            report.AppendLine("- " + line);
        if (SkippedVisuals.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("Skipped visuals:");
            foreach (string line in SkippedVisuals)
                report.AppendLine("- " + line);
        }
        if (MissingAssets.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("Missing assets:");
            foreach (string path in MissingAssets.Distinct(StringComparer.Ordinal))
                report.AppendLine("- " + path);
        }
        File.WriteAllText(ProjectPath(ReportPath), report.ToString(), Encoding.UTF8);
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

    private static Rect Expanded(Rect rect, float amount)
    {
        return new Rect(rect.xMin - amount, rect.yMin - amount, rect.width + amount * 2f, rect.height + amount * 2f);
    }

    private static Rect CenterRect(float centerX, float centerZ, float width, float depth)
    {
        return new Rect(centerX - width * 0.5f, centerZ - depth * 0.5f, width, depth);
    }

    private static bool Contains(Rect outer, Rect inner)
    {
        return inner.xMin >= outer.xMin &&
            inner.xMax <= outer.xMax &&
            inner.yMin >= outer.yMin &&
            inner.yMax <= outer.yMax;
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
        mesh.vertices = new[] { new Vector3(-halfX, 0f, -halfZ), new Vector3(-halfX, 0f, halfZ), new Vector3(halfX, 0f, halfZ), new Vector3(halfX, 0f, -halfZ) };
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
}
#endif
