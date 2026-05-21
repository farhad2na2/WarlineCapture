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

public static class WarlineCaptureGc26SourceScaleCompoundLayoutBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC26_SourceScaleCompoundLayout_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC26_SourceScaleCompoundLayout_2048";
    private const string ReportPath = "Design/AgentReports/2026-05-21_gameplay_gc26-source-scale-compound-layout.md";
    private const string DataPath = "Design/AgentReports/Data/GeneratedScenes/GC26_SourceScaleCompoundLayout_2048/gc26_source_scale_compound_layout_contract.json";
    private const float MapSize = 2048f;

    private static readonly List<Zone> MacroRoads = new();
    private static readonly List<Zone> SoldierLocalZones = new();
    private static readonly List<Zone> VehicleZones = new();
    private static readonly List<Zone> BlockerZones = new();
    private static readonly List<District> Districts = new();
    private static readonly List<MaskedModule> Modules = new();
    private static readonly List<Placement> Placements = new();
    private static readonly List<AuthoredClusterPlacement> AuthoredClusterPlacements = new();
    private static readonly List<string> MissingAssets = new();
    private static readonly List<string> ValidationLog = new();
    private static readonly List<string> SkippedVisuals = new();
    private static readonly List<string> ScaleAuditLog = new();
    private static int mainBlockerPlacements;
    private static int detailBlockerPlacements;
    private static int authoredClusterPlacements;

    private readonly struct AuthoredClusterPlacement
    {
        public readonly string Name;
        public readonly string Path;
        public readonly string ModuleName;
        public readonly string MaskName;
        public readonly Rect Bounds;
        public readonly float Scale;

        public AuthoredClusterPlacement(string name, string path, string moduleName, string maskName, Rect bounds, float scale)
        {
            Name = name;
            Path = path;
            ModuleName = moduleName;
            MaskName = maskName;
            Bounds = bounds;
            Scale = scale;
        }
    }

    private readonly struct MaskedModule
    {
        public readonly string Name;
        public readonly string DistrictName;
        public readonly string Style;
        public readonly string ReplacementIntent;
        public readonly string[] BlockerPrefixes;

        public MaskedModule(string name, string districtName, string style, string replacementIntent, params string[] blockerPrefixes)
        {
            Name = name;
            DistrictName = districtName;
            Style = style;
            ReplacementIntent = replacementIntent;
            BlockerPrefixes = blockerPrefixes;
        }
    }

    private readonly struct District
    {
        public readonly string Name;
        public readonly Rect Rect;
        public readonly string Role;
        public readonly string Style;
        public readonly float Density;

        public District(string name, Rect rect, string role, string style, float density)
        {
            Name = name;
            Rect = rect;
            Role = role;
            Style = style;
            Density = density;
        }
    }

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
        public GeneratorSettingsExport generatorSettings;
        public List<DistrictExport> districts = new();
        public List<ModuleExport> modules = new();
        public List<ZoneExport> zones = new();
        public List<PlacementExport> placements = new();
        public List<AuthoredClusterExport> authoredClusters = new();
    }

    [Serializable]
    private sealed class GeneratorSettingsExport
    {
        public float mapSizeMeters;
        public string roadPattern;
        public string terrainProfile;
        public string artStyle;
        public string authoringRule;
    }

    [Serializable]
    private sealed class DistrictExport
    {
        public string name;
        public string role;
        public string style;
        public float density;
        public float centerX;
        public float centerZ;
        public float width;
        public float depth;
    }

    [Serializable]
    private sealed class ModuleExport
    {
        public string name;
        public string district;
        public string style;
        public string replacementIntent;
        public float centerX;
        public float centerZ;
        public float width;
        public float depth;
        public List<string> blockerMasks = new();
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

    [Serializable]
    private sealed class AuthoredClusterExport
    {
        public string name;
        public string prefab;
        public string module;
        public string mask;
        public float centerX;
        public float centerZ;
        public float width;
        public float depth;
        public float scale;
    }

    [MenuItem("WarlineCapture/Design/Build GC26 Source-Scale Compound Layout 2048")]
    public static void BuildGc26SourceScaleCompoundLayout2048()
    {
        MacroRoads.Clear();
        SoldierLocalZones.Clear();
        VehicleZones.Clear();
        BlockerZones.Clear();
        Districts.Clear();
        Modules.Clear();
        Placements.Clear();
        AuthoredClusterPlacements.Clear();
        MissingAssets.Clear();
        ValidationLog.Clear();
        SkippedVisuals.Clear();
        ScaleAuditLog.Clear();
        mainBlockerPlacements = 0;
        detailBlockerPlacements = 0;
        authoredClusterPlacements = 0;

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(DataPath)));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        GameObject root = new("GC26_SourceScaleCompoundLayout_2048_Root");
        BuildEnvironment(root);
        DefineContract();
        BuildGroundAndRoads(root);
        PlaceVisualsFromContract(root);
        PlaceAuthoredClusterModules(root);
        PlaceProofUnits(root);
        BuildDebugOverlay(root);
        BuildGeneratorAuthoringOverlay(root);
        BuildMaskedModuleOverlay(root);
        BuildCameras(root);
        Validate();

        EditorSceneManager.SaveScene(scene, ScenePath);
        CaptureScene();
        WriteContractJson();
        WriteReport();
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GC26_SOURCE_SCALE_COMPOUND_LAYOUT_BUILT scene={ScenePath} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static void DefineContract()
    {
        AddDistrict("NorthWestDenseTown", 42f, 46f, 390f, 190f, "Civilian city blocks", "MiddleEasternTown", 0.78f);
        AddDistrict("WestMarketResidential", 64f, 282f, 230f, 232f, "Market streets and low houses", "MarketVillage", 0.72f);
        AddDistrict("SouthPlayerBase", 44f, 650f, 236f, 206f, "Player staging base", "MilitaryBase", 0.58f);
        AddDistrict("CentralBarracksSpine", 300f, 484f, 190f, 358f, "Source-scale barracks and service yards", "MilitaryCamp", 0.66f);
        AddDistrict("NorthEastAirfield", 606f, 34f, 292f, 330f, "Source-scale runway, hangars, apron", "Airfield", 0.62f);
        AddDistrict("CentralCommandLogistics", 532f, 326f, 326f, 210f, "Source-scale command and vehicle logistics", "CommandDepot", 0.70f);
        AddDistrict("SouthEastArmorFuel", 560f, 574f, 292f, 292f, "Armor parking and fuel utility", "ArmorFuelDepot", 0.74f);

        AddMacroRoad("MacroRoad_MainNorthSouth_West", 258f, 0f, 60f, 890f);
        AddMacroRoad("MacroRoad_MainNorthSouth_East", 486f, 0f, 56f, 890f);
        AddMacroRoad("MacroRoad_NorthCityToAirfield", 62f, 224f, 536f, 54f);
        AddMacroRoad("MacroRoad_CentralCityToCommand", 130f, 428f, 356f, 54f);
        AddMacroRoad("MacroRoad_CommandToAirfield_East", 542f, 286f, 356f, 42f);
        AddMacroRoad("MacroRoad_SouthPlayerToFuel", 38f, 862f, 790f, 54f);

        AddCity("CityCore_NorthWest", 58f, 68f, 142f, 136f, true);
        AddCity("CityCentral_WestInner", 304f, 84f, 108f, 118f, false);
        AddCity("CityMarket_West", 102f, 320f, 150f, 136f, true);
        AddCity("CitySouth_West", 76f, 714f, 150f, 136f, true);
        AddCamp("SouthGate_PlayerCamp", 64f, 686f, 110f, 118f);
        AddCamp("CentralTentBarracks", 314f, 512f, 146f, 132f);
        AddCamp("WestTentBarracks", 314f, 682f, 146f, 132f);
        AddCamp("Airfield_Apron", 646f, 72f, 204f, 250f);
        AddCamp("CommandDepot_CentralEast", 574f, 350f, 204f, 166f);
        AddCamp("VehicleYard_NorthEast", 784f, 360f, 66f, 84f);
        AddCamp("VehicleYard_SouthEast", 626f, 642f, 166f, 166f);
        AddCamp("FuelUtility_East", 800f, 604f, 48f, 126f);

        DefineMaskedModules();
    }

    private static void DefineMaskedModules()
    {
        AddModule("TownBlockModule_NorthWest", "NorthWestDenseTown", "Demo-authored village houses/shops", "Replace with selected Demo town cluster while preserving the listed building blocker masks.", "CityCore_NorthWest", "CityCentral_WestInner");
        AddModule("MarketModule_West", "WestMarketResidential", "Market village with cloth covers, alleys, walls", "Replace with Demo market/residential module; streets stay soldier-walkable.", "CityMarket_West");
        AddModule("PlayerBaseModule_SouthGate", "SouthPlayerBase", "Military entry camp", "Replace with Demo base gate/tent cluster; player staging lanes stay walkable.", "SouthGate_PlayerCamp");
        AddModule("BarracksModule_CentralSpine", "CentralBarracksSpine", "Barracks and tent service strip", "Replace with Demo barracks/tent row cluster; central macro-road access stays clear.", "CentralTentBarracks", "WestTentBarracks");
        AddModule("AirfieldModule_NorthEast", "NorthEastAirfield", "Runway apron, hangar, control tower", "Replace with Demo airfield cluster; runway and apron vehicle lanes stay walkable.", "Airfield_Apron");
        AddModule("CommandDepotModule_CentralEast", "CentralCommandLogistics", "Command depot and vehicle yard", "Replace with Demo command/logistics module; command yard stays vehicle-walkable.", "CommandDepot_CentralEast", "VehicleYard_NorthEast");
        AddModule("ArmorFuelModule_SouthEast", "SouthEastArmorFuel", "Armor park and fuel utility", "Replace with Demo armor/fuel service module; vehicle loop stays walkable.", "VehicleYard_SouthEast", "FuelUtility_East");
    }

    private static void AddModule(string name, string districtName, string style, string replacementIntent, params string[] blockerPrefixes)
    {
        Modules.Add(new MaskedModule(name, districtName, style, replacementIntent, blockerPrefixes));
    }

    private static void AddDistrict(string name, float x, float y, float width, float height, string role, string style, float density)
    {
        Districts.Add(new District(name, BlueprintRect(x, y, width, height), role, style, density));
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

        Volume volume = Child(root, "GC26_RTS_PresentationVolume").AddComponent<Volume>();
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
        Material baseMat = CreateMaterial("GC26_SandBase", new Color(0.58f, 0.49f, 0.34f, 1f));
        Material sandLightMat = CreateMaterial("GC26_SandWindLight", new Color(0.66f, 0.57f, 0.40f, 1f));
        Material sandDarkMat = CreateMaterial("GC26_SandCompactedDark", new Color(0.46f, 0.38f, 0.25f, 1f));
        Material roadMat = CreateMaterial("GC26_AuthoredCompactedRoad", new Color(0.50f, 0.40f, 0.25f, 1f));
        Material roadEdgeMat = CreateMaterial("GC26_RoadRaisedEdge", new Color(0.34f, 0.27f, 0.17f, 1f));
        Material shoulderMat = CreateMaterial("GC26_AuthoredRoadShoulder", new Color(0.47f, 0.38f, 0.24f, 1f));
        Material trackMat = CreateMaterial("GC26_DirtTireTrack", new Color(0.26f, 0.20f, 0.12f, 1f));
        Material runwayMat = CreateMaterial("GC26_RunwayWeatheredAsphalt", new Color(0.24f, 0.24f, 0.22f, 1f));
        Material runwayMarkMat = CreateMaterial("GC26_RunwayFadedMarking", new Color(0.72f, 0.68f, 0.58f, 1f));
        Material plazaMat = CreateMaterial("GC26_LocalPackedPlaza", new Color(0.63f, 0.52f, 0.35f, 1f));
        Material vehicleMat = CreateMaterial("GC26_VehicleYardDust", new Color(0.50f, 0.42f, 0.31f, 1f));
        Material districtPadMat = CreateMaterial("GC26_DistrictPackedSandPad", new Color(0.61f, 0.51f, 0.34f, 1f));

        Surface(root, "FlatGameplayBase_2048", Vector3.zero, new Vector2(MapSize, MapSize), baseMat, -0.04f);
        AddGroundVariationTiles(root, sandLightMat, sandDarkMat);
        AddDistrictPads(root, districtPadMat);

        GameObject local = Child(root, "AuthoredLocalYards_FromWalkabilityContract");
        foreach (Zone zone in SoldierLocalZones)
            Surface(local, zone.Name, Center(zone.Rect, 0.015f), new Vector2(zone.Rect.width, zone.Rect.height), plazaMat, 0.015f);
        foreach (Zone zone in VehicleZones)
            Surface(local, zone.Name, Center(zone.Rect, 0.026f), new Vector2(zone.Rect.width, zone.Rect.height), vehicleMat, 0.026f);

        GameObject roads = Child(root, "AuthoredRoadMeshes_VisualOnly_PreserveGc17Masks");
        SurfaceBlueprintPolygon(roads, "MainNorthSouth_West_Shoulder", shoulderMat, 0.036f, (238f, -4f), (338f, -4f), (306f, 894f), (206f, 894f));
        SurfaceBlueprintPolygon(roads, "MainNorthSouth_West", roadMat, 0.052f, (258f, 0f), (318f, 0f), (286f, 890f), (226f, 890f));
        SurfaceBlueprintPolygon(roads, "MainNorthSouth_East_Shoulder", shoulderMat, 0.036f, (466f, -4f), (562f, -4f), (542f, 894f), (446f, 894f));
        SurfaceBlueprintPolygon(roads, "MainNorthSouth_East", roadMat, 0.052f, (486f, 0f), (542f, 0f), (522f, 890f), (466f, 890f));

        SurfaceBlueprintPolygon(roads, "NorthCityToAirfield_Shoulder", shoulderMat, 0.036f, (48f, 226f), (608f, 212f), (610f, 302f), (50f, 316f));
        SurfaceBlueprintPolygon(roads, "NorthCityToAirfield", roadMat, 0.052f, (62f, 248f), (598f, 236f), (598f, 278f), (66f, 292f));
        SurfaceBlueprintPolygon(roads, "CentralCityToCommand_Shoulder", shoulderMat, 0.036f, (112f, 404f), (492f, 396f), (492f, 502f), (116f, 510f));
        SurfaceBlueprintPolygon(roads, "CentralCityToCommand", roadMat, 0.052f, (130f, 428f), (486f, 420f), (486f, 472f), (134f, 480f));
        SurfaceBlueprintPolygon(roads, "CommandToAirfield_East_Shoulder", shoulderMat, 0.036f, (530f, 270f), (902f, 270f), (902f, 342f), (530f, 342f));
        SurfaceBlueprintPolygon(roads, "CommandToAirfield_East", roadMat, 0.052f, (542f, 286f), (898f, 286f), (898f, 328f), (542f, 328f));
        SurfaceBlueprintPolygon(roads, "SouthPlayerToFuel_Shoulder", shoulderMat, 0.036f, (26f, 842f), (840f, 842f), (840f, 930f), (26f, 930f));
        SurfaceBlueprintPolygon(roads, "SouthPlayerToFuel", roadMat, 0.052f, (38f, 862f), (828f, 862f), (828f, 916f), (38f, 916f));

        SurfaceBlueprintPolygon(roads, "Airfield_Runway_Primary", runwayMat, 0.066f, (724f, 70f), (830f, 70f), (830f, 326f), (724f, 326f));
        SurfaceBlueprintPolygon(roads, "Airfield_Runway_Diagonal", runwayMat, 0.065f, (790f, 34f), (850f, 48f), (778f, 314f), (718f, 300f));
        SurfaceBlueprintPolygon(roads, "Market_Objective_Plaza", plazaMat, 0.064f, (356f, 238f), (490f, 238f), (490f, 290f), (356f, 290f));
        SurfaceBlueprintPolygon(roads, "South_Objective_Plaza", plazaMat, 0.064f, (340f, 706f), (474f, 706f), (474f, 762f), (340f, 762f));
        SurfaceBlueprintPolygon(roads, "Command_Outpost_Yard", plazaMat, 0.063f, (560f, 342f), (790f, 342f), (808f, 522f), (570f, 526f));
        SurfaceBlueprintPolygon(roads, "VehicleFuel_Yard", vehicleMat, 0.063f, (600f, 610f), (842f, 590f), (850f, 818f), (626f, 842f));

        SurfaceBlueprintPolygon(roads, "Track_PlayerToCommand_A", trackMat, 0.082f, (48f, 862f), (828f, 862f), (828f, 874f), (48f, 874f));
        SurfaceBlueprintPolygon(roads, "Track_NorthRoad_A", trackMat, 0.082f, (70f, 262f), (598f, 248f), (598f, 260f), (70f, 274f));
        SurfaceBlueprintPolygon(roads, "Track_CommandRoad_A", trackMat, 0.082f, (138f, 440f), (486f, 432f), (486f, 444f), (140f, 452f));
        AddRoadEdgeBands(roads, roadEdgeMat, runwayMarkMat);
    }

    private static void AddGroundVariationTiles(GameObject root, Material lightMat, Material darkMat)
    {
        GameObject variation = Child(root, "GC26_VisualSandVariationTiles");
        SurfaceBlueprintRect(variation, "SandLight_NorthWest", 20f, 52f, 260f, 120f, lightMat, -0.02f);
        SurfaceBlueprintRect(variation, "SandDark_WestCityBacklot", 80f, 456f, 186f, 92f, darkMat, -0.018f);
        SurfaceBlueprintRect(variation, "SandLight_CommandApproach", 536f, 306f, 240f, 82f, lightMat, -0.017f);
        SurfaceBlueprintRect(variation, "SandDark_SouthFuelRidge", 594f, 704f, 240f, 108f, darkMat, -0.018f);
        SurfaceBlueprintRect(variation, "SandLight_AirfieldDust", 670f, 58f, 180f, 136f, lightMat, -0.017f);
        SurfaceBlueprintRect(variation, "SandDark_SouthBaseOuter", 120f, 706f, 230f, 120f, darkMat, -0.018f);
    }

    private static void AddDistrictPads(GameObject root, Material padMat)
    {
        GameObject pads = Child(root, "GC26_AuthoredDistrictPads_VisualOnly");
        foreach (District district in Districts)
        {
            Rect rect = Expanded(district.Rect, 10f);
            Surface(pads, district.Name + "_PackedDistrictPad", Center(rect, 0.004f), new Vector2(rect.width, rect.height), padMat, 0.004f);
        }
    }

    private static void AddRoadEdgeBands(GameObject roads, Material edgeMat, Material runwayMarkMat)
    {
        SurfaceBlueprintPolygon(roads, "Edge_MainNorthSouth_West_A", edgeMat, 0.091f, (222f, 0f), (232f, 0f), (200f, 890f), (190f, 890f));
        SurfaceBlueprintPolygon(roads, "Edge_MainNorthSouth_West_B", edgeMat, 0.091f, (314f, 0f), (324f, 0f), (292f, 890f), (282f, 890f));
        SurfaceBlueprintPolygon(roads, "Edge_MainNorthSouth_East_A", edgeMat, 0.091f, (466f, 0f), (476f, 0f), (456f, 890f), (446f, 890f));
        SurfaceBlueprintPolygon(roads, "Edge_MainNorthSouth_East_B", edgeMat, 0.091f, (542f, 0f), (552f, 0f), (532f, 890f), (522f, 890f));
        SurfaceBlueprintPolygon(roads, "Edge_NorthRoad_A", edgeMat, 0.091f, (58f, 242f), (600f, 230f), (600f, 240f), (60f, 252f));
        SurfaceBlueprintPolygon(roads, "Edge_NorthRoad_B", edgeMat, 0.091f, (66f, 292f), (600f, 278f), (600f, 288f), (68f, 302f));
        SurfaceBlueprintPolygon(roads, "Edge_CommandRoad_A", edgeMat, 0.091f, (128f, 418f), (488f, 410f), (488f, 420f), (130f, 428f));
        SurfaceBlueprintPolygon(roads, "Edge_CommandRoad_B", edgeMat, 0.091f, (132f, 480f), (488f, 472f), (488f, 482f), (134f, 490f));
        SurfaceBlueprintRect(roads, "Runway_Center_FadedStripe", 774f, 90f, 8f, 220f, runwayMarkMat, 0.096f);
        SurfaceBlueprintRect(roads, "Runway_Threshold_North", 748f, 104f, 58f, 6f, runwayMarkMat, 0.097f);
        SurfaceBlueprintRect(roads, "Runway_Threshold_South", 748f, 292f, 58f, 6f, runwayMarkMat, 0.097f);
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

    private static void PlaceAuthoredClusterModules(GameObject root)
    {
        GameObject parent = Child(root, "AuthoredDemoClusters_SourceScaleReference");
        foreach (MaskedModule module in Modules)
        {
            Zone? targetMask = UsesChildMaskedPlacement(module) ? BestMaskForAuthoredCluster(module) : ModuleEnvelope(module);
            if (!targetMask.HasValue)
            {
                SkippedVisuals.Add($"{module.Name}: skipped authored cluster because module owns no legal blocker mask or envelope.");
                continue;
            }

            string path = AuthoredClusterPrefabFor(module);
            if (UsesChildMaskedPlacement(module))
                PlaceChildMaskedAuthoredModule(parent, module, path);
            else
                PlaceAuthoredCluster(parent, module, targetMask.Value, path);
        }
    }

    private static bool UsesChildMaskedPlacement(MaskedModule module)
    {
        return module.Name.Contains("TownBlock", StringComparison.Ordinal) ||
            module.Name.Contains("Market", StringComparison.Ordinal);
    }

    private static Zone? BestMaskForAuthoredCluster(MaskedModule module)
    {
        return ModuleMasks(module)
            .Where(mask => !MacroRoads.Any(road => road.Rect.Overlaps(mask.Rect)))
            .OrderByDescending(mask => mask.Rect.width * mask.Rect.height)
            .Cast<Zone?>()
            .FirstOrDefault();
    }

    private static Zone? ModuleEnvelope(MaskedModule module)
    {
        List<Zone> masks = ModuleMasks(module)
            .Where(mask => !MacroRoads.Any(road => road.Rect.Overlaps(mask.Rect)))
            .ToList();
        if (masks.Count == 0)
            return null;

        Rect envelope = masks[0].Rect;
        for (int i = 1; i < masks.Count; i++)
            envelope = Union(envelope, masks[i].Rect);

        return new Zone(module.Name + "_SourceScaleEnvelope", envelope, "ScaleReferenceEnvelope");
    }

    private static string AuthoredClusterPrefabFor(MaskedModule module)
    {
        if (module.Name.Contains("TownBlock", StringComparison.Ordinal))
            return "Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab";
        if (module.Name.Contains("Market", StringComparison.Ordinal))
            return "Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab";
        if (module.Name.Contains("PlayerBase", StringComparison.Ordinal))
            return "Assets/Game/Prefabs/Generated/GC04Modules/BaseSouthDepot_DemoAuthored.prefab";
        if (module.Name.Contains("Barracks", StringComparison.Ordinal))
            return "Assets/Game/Prefabs/Generated/GC04Modules/BaseBarracks_DemoAuthored.prefab";
        if (module.Name.Contains("Airfield", StringComparison.Ordinal))
            return "Assets/Game/Prefabs/Generated/GC04Modules/RunwayApron_DemoAuthored.prefab";
        if (module.Name.Contains("CommandDepot", StringComparison.Ordinal))
            return "Assets/Game/Prefabs/Generated/GC04Modules/BaseCommand_DemoAuthored.prefab";
        return "Assets/Game/Prefabs/Generated/GC04Modules/BaseMotorPool_DemoAuthored.prefab";
    }

    private static void PlaceChildMaskedAuthoredModule(GameObject parent, MaskedModule module, string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            MissingAssets.Add(path);
            return;
        }

        List<Zone> masks = ModuleMasks(module)
            .Where(mask => !MacroRoads.Any(road => road.Rect.Overlaps(mask.Rect)))
            .OrderByDescending(mask => mask.Rect.width * mask.Rect.height)
            .ToList();
        if (masks.Count == 0)
        {
            SkippedVisuals.Add($"{module.Name}: skipped child-masked authored module because it has no legal blocker masks.");
            return;
        }

        GameObject source = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        source.name = "GC26_" + module.Name + "_ChildMaskSource";
        source.transform.position = new Vector3(5000f, 0f, 5000f);
        source.transform.rotation = Quaternion.identity;
        source.transform.localScale = Vector3.one;

        const int piecesPerMask = 4;
        List<GameObject> children = RenderableChildRoots(source).Take(masks.Count * piecesPerMask).ToList();
        if (children.Count == 0)
        {
            SkippedVisuals.Add($"{module.Name}: skipped child-masked authored module because prefab has no renderable child roots.");
            Object.DestroyImmediate(source);
            return;
        }

        GameObject moduleRoot = Child(parent, "GC26_" + module.Name + "_ChildMaskedPieces");
        int accepted = 0;
        int pieceIndex = 0;
        for (int maskIndex = 0; maskIndex < masks.Count; maskIndex++)
        {
            for (int slotIndex = 0; slotIndex < piecesPerMask && pieceIndex < children.Count; slotIndex++)
            {
                Zone subMask = ChildMaskSubZone(masks[maskIndex], slotIndex);
                if (TryPlaceChildMaskedPiece(moduleRoot, module, subMask, path, children[pieceIndex], pieceIndex))
                    accepted++;
                pieceIndex++;
            }
        }

        Object.DestroyImmediate(source);
        if (accepted == 0)
            SkippedVisuals.Add($"{module.Name}: no child-masked pieces from {path} fit inside owned blocker masks.");
    }

    private static IEnumerable<GameObject> RenderableChildRoots(GameObject source)
    {
        return source.GetComponentsInChildren<Renderer>(false)
            .Where(renderer => IsUsefulTownRenderer(renderer.gameObject.name))
            .OrderByDescending(renderer => renderer.bounds.size.x * renderer.bounds.size.z)
            .Select(renderer => renderer.gameObject)
            .Where(go => go != source)
            .Distinct();
    }

    private static Zone ChildMaskSubZone(Zone mask, int slotIndex)
    {
        float width = mask.Rect.width * 0.52f;
        float height = mask.Rect.height * 0.52f;
        float xOffset = ((slotIndex % 2) == 0 ? -0.18f : 0.18f) * mask.Rect.width;
        float yOffset = ((slotIndex / 2) == 0 ? -0.18f : 0.18f) * mask.Rect.height;
        Rect rect = CenterRect(mask.Rect.center.x + xOffset, mask.Rect.center.y + yOffset, width, height);
        return new Zone(mask.Name + "_ChildSlot_" + slotIndex, rect, mask.Kind);
    }

    private static bool IsUsefulTownRenderer(string name)
    {
        bool usefulTownPiece = name.Contains("Bld", StringComparison.Ordinal) ||
            name.Contains("Shop", StringComparison.Ordinal) ||
            name.Contains("House", StringComparison.Ordinal) ||
            name.Contains("Tent", StringComparison.Ordinal) ||
            name.Contains("Tower", StringComparison.Ordinal) ||
            name.Contains("Wall", StringComparison.Ordinal) ||
            name.Contains("Fence", StringComparison.Ordinal) ||
            name.Contains("Camo", StringComparison.Ordinal) ||
            name.Contains("Destroyed", StringComparison.Ordinal);

        return usefulTownPiece &&
            !name.Contains("FX_", StringComparison.Ordinal) &&
            !name.Contains("Dust", StringComparison.Ordinal) &&
            !name.Contains("SM_Env_Ground", StringComparison.Ordinal) &&
            !name.Contains("Ground", StringComparison.Ordinal) &&
            !name.Contains("Hill", StringComparison.Ordinal) &&
            !name.Contains("Rock", StringComparison.Ordinal) &&
            !name.Contains("Clock_Tower_Clock", StringComparison.Ordinal) &&
            !name.Contains("HourHand", StringComparison.Ordinal) &&
            !name.Contains("MinuteHand", StringComparison.Ordinal);
    }

    private static bool TryPlaceChildMaskedPiece(GameObject parent, MaskedModule module, Zone mask, string sourcePath, GameObject sourceChild, int index)
    {
        GameObject instance = Object.Instantiate(sourceChild);
        instance.name = "GC26_" + module.Name + "_ChildPiece_" + index + "_" + sourceChild.name;
        instance.transform.SetParent(parent.transform, true);
        instance.transform.position = Center(mask.Rect, 0f);
        instance.transform.rotation = Quaternion.Euler(0f, RotationFor(authoredClusterPlacements + index), 0f);
        instance.transform.localScale = Vector3.one;
        AlignBottomNearGround(instance);

        Bounds bounds = CalculateRendererBounds(instance);
        instance.transform.position += new Vector3(mask.Rect.center.x - bounds.center.x, 0f, mask.Rect.center.y - bounds.center.z);
        AlignBottomNearGround(instance);

        bounds = CalculateRendererBounds(instance);
        Rect rect = CenterRect(bounds.center.x, bounds.center.z, bounds.size.x, bounds.size.z);
        if (!Contains(Expanded(mask.Rect, 1f), rect))
        {
            SkippedVisuals.Add($"{module.Name}/{sourceChild.name}: rejected child-masked piece because renderer bounds leak outside blocker mask {mask.Name}.");
            Object.DestroyImmediate(instance);
            return false;
        }
        if (MacroRoads.Any(zone => zone.Rect.Overlaps(rect)))
        {
            SkippedVisuals.Add($"{module.Name}/{sourceChild.name}: rejected child-masked piece because renderer bounds overlap macro road space.");
            Object.DestroyImmediate(instance);
            return false;
        }

        foreach (LODGroup lod in instance.GetComponentsInChildren<LODGroup>(false))
        {
            lod.ForceLOD(0);
            lod.enabled = false;
        }

        string virtualPath = sourcePath + "::" + sourceChild.name;
        AuthoredClusterPlacements.Add(new AuthoredClusterPlacement(instance.name, virtualPath, module.Name, mask.Name, rect, 1f));
        ScaleAuditLog.Add($"{instance.name}: source scale 1.00; footprint {rect.width:0.0}m x {rect.height:0.0}m; mask {mask.Rect.width:0.0}m x {mask.Rect.height:0.0}m.");
        authoredClusterPlacements++;
        return true;
    }

    private static void PlaceAuthoredCluster(GameObject parent, MaskedModule module, Zone mask, string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            MissingAssets.Add(path);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "GC26_" + module.Name + "_AuthoredCluster";
        instance.transform.SetParent(parent.transform, true);
        instance.transform.position = Center(mask.Rect, 0f);
        instance.transform.rotation = Quaternion.Euler(0f, RotationFor(authoredClusterPlacements), 0f);
        instance.transform.localScale = Vector3.one;
        AlignBottomNearGround(instance);

        Bounds bounds = CalculateRendererBounds(instance);
        instance.transform.position += new Vector3(mask.Rect.center.x - bounds.center.x, 0f, mask.Rect.center.y - bounds.center.z);
        AlignBottomNearGround(instance);

        bounds = CalculateRendererBounds(instance);
        Rect rect = CenterRect(bounds.center.x, bounds.center.z, bounds.size.x, bounds.size.z);
        if (!Contains(Expanded(mask.Rect, 18f), rect))
        {
            SkippedVisuals.Add($"{module.Name}: rejected scale-1 authored cluster {path} because renderer bounds exceed envelope {mask.Name}.");
            Object.DestroyImmediate(instance);
            return;
        }

        if (MacroRoads.Any(zone => zone.Rect.Overlaps(rect)))
        {
            SkippedVisuals.Add($"{module.Name}: rejected authored cluster {path} because fitted renderer bounds overlap macro road space.");
            Object.DestroyImmediate(instance);
            return;
        }

        foreach (LODGroup lod in instance.GetComponentsInChildren<LODGroup>(false))
        {
            lod.ForceLOD(0);
            lod.enabled = false;
        }

        AuthoredClusterPlacements.Add(new AuthoredClusterPlacement(instance.name, path, module.Name, mask.Name, rect, 1f));
        ScaleAuditLog.Add($"{instance.name}: source scale 1.00; footprint {rect.width:0.0}m x {rect.height:0.0}m; envelope {mask.Rect.width:0.0}m x {mask.Rect.height:0.0}m.");
        authoredClusterPlacements++;
    }

    private static void ClampRendererBoundsInsideMask(GameObject instance, Rect mask)
    {
        Bounds bounds = CalculateRendererBounds(instance);
        float deltaX = 0f;
        float deltaZ = 0f;
        if (bounds.min.x < mask.xMin)
            deltaX = mask.xMin - bounds.min.x;
        else if (bounds.max.x > mask.xMax)
            deltaX = mask.xMax - bounds.max.x;

        if (bounds.min.z < mask.yMin)
            deltaZ = mask.yMin - bounds.min.z;
        else if (bounds.max.z > mask.yMax)
            deltaZ = mask.yMax - bounds.max.z;

        instance.transform.position += new Vector3(deltaX, 0f, deltaZ);
        AlignBottomNearGround(instance);
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
        return 1f;
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
        instance.name = "GC26_" + name;
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
        PlacePrefab(parent, name, path, position, new Vector2(12f, 12f), rotationY, 1f, "ProofUnit");
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
        Material green = CreateMaterial("GC26_DebugSoldierWalkable", new Color(0f, 1f, 0.1f, 0.6f));
        Material cyan = CreateMaterial("GC26_DebugVehicleWalkable", new Color(0f, 0.8f, 1f, 0.6f));
        Material red = CreateMaterial("GC26_DebugBlocker", new Color(1f, 0f, 0f, 0.7f));
        foreach (Zone zone in SoldierLocalZones)
            Surface(debug, zone.Name, Center(zone.Rect, 0.2f), new Vector2(zone.Rect.width, zone.Rect.height), green, 0.2f);
        foreach (Zone zone in VehicleZones)
            Surface(debug, zone.Name, Center(zone.Rect, 0.22f), new Vector2(zone.Rect.width, zone.Rect.height), cyan, 0.22f);
        foreach (Zone zone in BlockerZones)
            Surface(debug, zone.Name, Center(zone.Rect, 0.24f), new Vector2(zone.Rect.width, zone.Rect.height), red, 0.24f);
    }

    private static void BuildGeneratorAuthoringOverlay(GameObject root)
    {
        GameObject overlay = Child(root, "HiddenProceduralGeneratorAuthoringOverlay");
        overlay.SetActive(false);

        Material city = CreateMaterial("GC26_AuthoringDistrict_City", new Color(0.20f, 0.42f, 0.80f, 0.85f));
        Material baseMat = CreateMaterial("GC26_AuthoringDistrict_Base", new Color(0.30f, 0.62f, 0.28f, 0.85f));
        Material logistics = CreateMaterial("GC26_AuthoringDistrict_Logistics", new Color(0.84f, 0.58f, 0.18f, 0.85f));
        Material airfield = CreateMaterial("GC26_AuthoringDistrict_Airfield", new Color(0.68f, 0.68f, 0.74f, 0.85f));

        foreach (District district in Districts)
        {
            Material material = district.Style.Contains("Airfield", StringComparison.Ordinal) ? airfield :
                district.Style.Contains("Military", StringComparison.Ordinal) || district.Style.Contains("Camp", StringComparison.Ordinal) ? baseMat :
                district.Style.Contains("Depot", StringComparison.Ordinal) || district.Style.Contains("Fuel", StringComparison.Ordinal) ? logistics :
                city;

            Surface(overlay, district.Name, Center(district.Rect, 0.34f), new Vector2(district.Rect.width, district.Rect.height), material, 0.34f);
        }

        GameObject labels = Child(overlay, "DistrictLabels");
        foreach (District district in Districts)
        {
            GameObject label = Child(labels, district.Name + "_Label");
            label.transform.position = Center(district.Rect, 5f);
            label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = $"{district.Name}\n{district.Style}  {district.Density:0.00}";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 7f;
            text.fontSize = 28;
            text.color = Color.black;
        }
    }

    private static void BuildMaskedModuleOverlay(GameObject root)
    {
        GameObject overlay = Child(root, "HiddenMaskedDistrictModuleOverlay");
        overlay.SetActive(false);

        Material moduleMat = CreateMaterial("GC26_ModuleMask_DistrictEnvelope", new Color(0.88f, 0.18f, 0.92f, 0.82f));
        Material blockerMat = CreateMaterial("GC26_ModuleMask_BlockerOwned", new Color(1f, 0.08f, 0.08f, 0.9f));

        foreach (MaskedModule module in Modules)
        {
            List<Zone> masks = ModuleMasks(module).ToList();
            if (masks.Count == 0)
                continue;

            Rect envelope = masks[0].Rect;
            for (int i = 1; i < masks.Count; i++)
                envelope = Union(envelope, masks[i].Rect);

            GameObject moduleRoot = Child(overlay, module.Name);
            Surface(moduleRoot, module.Name + "_Envelope", Center(envelope, 0.38f), new Vector2(envelope.width, envelope.height), moduleMat, 0.38f);
            foreach (Zone mask in masks)
                Surface(moduleRoot, mask.Name + "_OwnedBlockerMask", Center(mask.Rect, 0.44f), new Vector2(mask.Rect.width, mask.Rect.height), blockerMat, 0.44f);

            GameObject label = Child(moduleRoot, module.Name + "_Label");
            label.transform.position = Center(envelope, 6f);
            label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = $"{module.Name}\n{masks.Count} blocker masks";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 7f;
            text.fontSize = 28;
            text.color = Color.black;
        }
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
            ValidationLog.Add($"ERROR: GC26 placed only {detailBlockerPlacements} legal detail props; densification target is at least 60.");

        if (Districts.Count < 7)
            ValidationLog.Add($"ERROR: GC26 exported only {Districts.Count} districts; procedural authoring target is at least 7 named districts.");

        foreach (Zone zone in SoldierLocalZones.Concat(VehicleZones).Concat(BlockerZones))
            if (!Districts.Any(district => district.Rect.Overlaps(zone.Rect)))
                ValidationLog.Add($"ERROR: zone {zone.Name} is not assigned to any procedural district footprint.");

        if (Modules.Count < 7)
            ValidationLog.Add($"ERROR: GC26 exported only {Modules.Count} reusable masked modules; target is at least 7.");

        if (authoredClusterPlacements < Modules.Count)
            ValidationLog.Add($"ERROR: GC26 placed only {authoredClusterPlacements} authored Demo clusters for {Modules.Count} reusable modules.");

        foreach (MaskedModule module in Modules)
        {
            if (!Districts.Any(district => district.Name == module.DistrictName))
                ValidationLog.Add($"ERROR: module {module.Name} references missing district {module.DistrictName}.");

            List<Zone> masks = ModuleMasks(module).ToList();
            if (masks.Count == 0)
                ValidationLog.Add($"ERROR: module {module.Name} owns no blocker masks.");

            foreach (Zone mask in masks)
            {
                if (MacroRoads.Any(road => road.Rect.Overlaps(mask.Rect)))
                    continue;
                if (!Districts.Any(district => district.Name == module.DistrictName && district.Rect.Overlaps(mask.Rect)))
                    ValidationLog.Add($"ERROR: module {module.Name} blocker mask {mask.Name} is outside its district envelope.");
            }
        }

        foreach (AuthoredClusterPlacement cluster in AuthoredClusterPlacements)
        {
            Zone mask = ResolveParentBlockerMask(cluster.MaskName);
            if (mask.Name == null)
                ValidationLog.Add($"ERROR: authored cluster {cluster.Name} references missing mask {cluster.MaskName}.");
            else if (!Contains(Expanded(mask.Rect, IsSourceScaleEnvelope(mask.Name) ? 18f : 1f), cluster.Bounds))
                ValidationLog.Add($"ERROR: authored cluster {cluster.Name} renderer bounds leak outside scale reference envelope/mask {cluster.MaskName}.");

            if (MacroRoads.Any(zone => zone.Rect.Overlaps(cluster.Bounds)))
                ValidationLog.Add($"ERROR: authored cluster {cluster.Name} renderer bounds overlap macro road space.");
        }

        if (ValidationLog.Count == 0)
            ValidationLog.Add($"PASS: GC26 instantiated {authoredClusterPlacements} source-scale Demo-authored clusters/modules, exported {Modules.Count} reusable masked district modules across {Districts.Count} named districts, preserved the GC17/GC18 walkability masks, and kept legal visuals off macro roads. Detail props: {detailBlockerPlacements}; primary blocker visuals: {mainBlockerPlacements}; total placements: {Placements.Count}; blocker zones: {BlockerZones.Count}; soldier zones: {SoldierLocalZones.Count}; vehicle zones: {VehicleZones.Count}; macro roads: {MacroRoads.Count}.");
    }

    private static Zone ResolveParentBlockerMask(string maskName)
    {
        if (IsSourceScaleEnvelope(maskName))
        {
            string moduleName = maskName.Substring(0, maskName.IndexOf("_SourceScaleEnvelope", StringComparison.Ordinal));
            MaskedModule module = Modules.FirstOrDefault(candidate => candidate.Name == moduleName);
            Zone? envelope = module.Name == null ? null : ModuleEnvelope(module);
            return envelope ?? default;
        }

        Zone exact = BlockerZones.FirstOrDefault(zone => zone.Name == maskName);
        if (exact.Name != null)
            return exact;

        int childSlotIndex = maskName.IndexOf("_ChildSlot_", StringComparison.Ordinal);
        if (childSlotIndex < 0)
            return default;

        string parentName = maskName.Substring(0, childSlotIndex);
        return BlockerZones.FirstOrDefault(zone => zone.Name == parentName);
    }

    private static bool IsSourceScaleEnvelope(string maskName)
    {
        return maskName.Contains("_SourceScaleEnvelope", StringComparison.Ordinal);
    }

    private static IEnumerable<Zone> ModuleMasks(MaskedModule module)
    {
        foreach (Zone blocker in BlockerZones)
            if (module.BlockerPrefixes.Any(prefix => blocker.Name.StartsWith(prefix, StringComparison.Ordinal)))
                yield return blocker;
    }

    private static bool TouchesWalkable(Vector3 position, Vector2 footprint)
    {
        Rect rect = CenterRect(position.x, position.z, footprint.x, footprint.y);
        return MacroRoads.Concat(SoldierLocalZones).Concat(VehicleZones).Any(zone => zone.Rect.Overlaps(rect));
    }

    private static void BuildCameras(GameObject root)
    {
        BuildRtsCamera(root, "Camera_GC26_RtsOverview", new Vector3(-620f, 520f, -820f), BlueprintPoint(388f, 430f), 38f);
        BuildRtsCamera(root, "Camera_GC26_RtsCityReadable", new Vector3(-930f, 330f, -430f), BlueprintPoint(172f, 366f), 34f);
        BuildRtsCamera(root, "Camera_GC26_RtsAirfieldCommand", new Vector3(260f, 430f, -620f), BlueprintPoint(660f, 320f), 36f);
        BuildRtsCamera(root, "Camera_GC26_RtsDenseCityClose", new Vector3(-700f, 210f, -250f), BlueprintPoint(176f, 392f), 30f);
        BuildRtsCamera(root, "Camera_GC26_ScaleAuditClose", new Vector3(-700f, 92f, -185f), BlueprintPoint(176f, 392f), 22f);

        Camera top = CameraObject(root, "Camera_GC26_TopDownContractVisualProof");
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
            if (camera.name == "Camera_GC26_RtsOverview")
                Render(camera, ProjectPath(CaptureRoot + "/gc26_rts_overview_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC26_RtsCityReadable")
                Render(camera, ProjectPath(CaptureRoot + "/gc26_rts_city_readable_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC26_RtsAirfieldCommand")
                Render(camera, ProjectPath(CaptureRoot + "/gc26_rts_airfield_command_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC26_RtsDenseCityClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc26_rts_dense_city_close_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC26_ScaleAuditClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc26_scale_audit_close_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC26_TopDownContractVisualProof")
            {
                RenderContractProof(camera, ProjectPath(CaptureRoot + "/gc26_topdown_contract_visual_proof_2048x2048.png"));
                RenderGeneratorBlueprint(camera, ProjectPath(CaptureRoot + "/gc26_topdown_generator_blueprint_2048x2048.png"));
                RenderModuleMaskProof(camera, ProjectPath(CaptureRoot + "/gc26_topdown_module_mask_proof_2048x2048.png"));
            }
        }
    }

    private static void RenderModuleMaskProof(Camera camera, string path)
    {
        GameObject modules = FindSceneObjectIncludingInactive("HiddenMaskedDistrictModuleOverlay");
        GameObject generator = FindSceneObjectIncludingInactive("HiddenProceduralGeneratorAuthoringOverlay");
        GameObject overlay = FindSceneObjectIncludingInactive("HiddenWalkabilityDebugOverlay");
        GameObject visuals = FindSceneObjectIncludingInactive("DenseVisuals_PlacedInsideBlockerZones_NotWalkableZones");
        GameObject dressing = FindSceneObjectIncludingInactive("LightDressing_OutsideWalkableLanes");
        GameObject units = FindSceneObjectIncludingInactive("ProofUnits_OnWalkableOnly");

        bool modulesWasActive = modules != null && modules.activeSelf;
        bool generatorWasActive = generator != null && generator.activeSelf;
        bool overlayWasActive = overlay != null && overlay.activeSelf;
        bool visualsWereActive = visuals != null && visuals.activeSelf;
        bool dressingWasActive = dressing != null && dressing.activeSelf;
        bool unitsWereActive = units != null && units.activeSelf;

        if (modules != null)
            modules.SetActive(true);
        if (generator != null)
            generator.SetActive(false);
        if (overlay != null)
            overlay.SetActive(true);
        if (visuals != null)
            visuals.SetActive(false);
        if (dressing != null)
            dressing.SetActive(false);
        if (units != null)
            units.SetActive(false);

        Render(camera, path, 2048, 2048);

        if (modules != null)
            modules.SetActive(modulesWasActive);
        if (generator != null)
            generator.SetActive(generatorWasActive);
        if (overlay != null)
            overlay.SetActive(overlayWasActive);
        if (visuals != null)
            visuals.SetActive(visualsWereActive);
        if (dressing != null)
            dressing.SetActive(dressingWasActive);
        if (units != null)
            units.SetActive(unitsWereActive);
    }

    private static void RenderGeneratorBlueprint(Camera camera, string path)
    {
        GameObject generator = FindSceneObjectIncludingInactive("HiddenProceduralGeneratorAuthoringOverlay");
        GameObject overlay = FindSceneObjectIncludingInactive("HiddenWalkabilityDebugOverlay");
        GameObject visuals = FindSceneObjectIncludingInactive("DenseVisuals_PlacedInsideBlockerZones_NotWalkableZones");
        GameObject dressing = FindSceneObjectIncludingInactive("LightDressing_OutsideWalkableLanes");
        GameObject units = FindSceneObjectIncludingInactive("ProofUnits_OnWalkableOnly");

        bool generatorWasActive = generator != null && generator.activeSelf;
        bool overlayWasActive = overlay != null && overlay.activeSelf;
        bool visualsWereActive = visuals != null && visuals.activeSelf;
        bool dressingWasActive = dressing != null && dressing.activeSelf;
        bool unitsWereActive = units != null && units.activeSelf;

        if (generator != null)
            generator.SetActive(true);
        if (overlay != null)
            overlay.SetActive(true);
        if (visuals != null)
            visuals.SetActive(false);
        if (dressing != null)
            dressing.SetActive(false);
        if (units != null)
            units.SetActive(false);

        Render(camera, path, 2048, 2048);

        if (generator != null)
            generator.SetActive(generatorWasActive);
        if (overlay != null)
            overlay.SetActive(overlayWasActive);
        if (visuals != null)
            visuals.SetActive(visualsWereActive);
        if (dressing != null)
            dressing.SetActive(dressingWasActive);
        if (units != null)
            units.SetActive(unitsWereActive);
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
            generatedBy = nameof(WarlineCaptureGc26SourceScaleCompoundLayoutBuilder),
            scene = ScenePath,
            generatorSettings = new GeneratorSettingsExport
            {
                mapSizeMeters = MapSize,
                roadPattern = "Two north-south arteries with three authored diagonal/east-west connectors",
                terrainProfile = "Flat RTS gameplay plane",
                artStyle = "PolygonMilitary desert military town",
                authoringRule = "Roads and local yards are walkable; all buildings, props, rocks, tents, and static vehicles must live inside blocker zones."
            }
        };
        foreach (District district in Districts)
            export.districts.Add(new DistrictExport { name = district.Name, role = district.Role, style = district.Style, density = district.Density, centerX = district.Rect.center.x, centerZ = district.Rect.center.y, width = district.Rect.width, depth = district.Rect.height });
        foreach (MaskedModule module in Modules)
        {
            List<Zone> masks = ModuleMasks(module).ToList();
            Rect envelope = masks.Count > 0 ? masks[0].Rect : new Rect();
            for (int i = 1; i < masks.Count; i++)
                envelope = Union(envelope, masks[i].Rect);

            ModuleExport moduleExport = new()
            {
                name = module.Name,
                district = module.DistrictName,
                style = module.Style,
                replacementIntent = module.ReplacementIntent,
                centerX = envelope.center.x,
                centerZ = envelope.center.y,
                width = envelope.width,
                depth = envelope.height
            };
            foreach (Zone mask in masks)
                moduleExport.blockerMasks.Add(mask.Name);
            export.modules.Add(moduleExport);
        }
        foreach (Zone zone in MacroRoads.Concat(SoldierLocalZones).Concat(VehicleZones).Concat(BlockerZones))
            export.zones.Add(new ZoneExport { name = zone.Name, kind = zone.Kind, centerX = zone.Rect.center.x, centerZ = zone.Rect.center.y, width = zone.Rect.width, depth = zone.Rect.height });
        foreach (Placement placement in Placements)
            export.placements.Add(new PlacementExport { name = placement.Name, prefab = placement.Path, zone = placement.ZoneName, x = placement.Position.x, z = placement.Position.z, width = placement.Footprint.x, depth = placement.Footprint.y });
        foreach (AuthoredClusterPlacement cluster in AuthoredClusterPlacements)
            export.authoredClusters.Add(new AuthoredClusterExport { name = cluster.Name, prefab = cluster.Path, module = cluster.ModuleName, mask = cluster.MaskName, centerX = cluster.Bounds.center.x, centerZ = cluster.Bounds.center.y, width = cluster.Bounds.width, depth = cluster.Bounds.height, scale = cluster.Scale });
        File.WriteAllText(ProjectPath(DataPath), JsonUtility.ToJson(export, true), Encoding.UTF8);
    }

    private static void WriteReport()
    {
        StringBuilder report = new();
        report.AppendLine("# GC26 Source-Scale Compound Layout Scene");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Continue from GC25 by correcting the blueprint layout so barracks, airfield, command, player base, and armor/fuel compounds can be placed at source scale 1 with roads routed around them.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGc26SourceScaleCompoundLayoutBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC26_SourceScaleCompoundLayout_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Data/GeneratedScenes/GC26_SourceScaleCompoundLayout_2048/gc26_source_scale_compound_layout_contract.json`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC26_SourceScaleCompoundLayout_2048/gc26_rts_overview_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC26_SourceScaleCompoundLayout_2048/gc26_rts_city_readable_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC26_SourceScaleCompoundLayout_2048/gc26_rts_airfield_command_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC26_SourceScaleCompoundLayout_2048/gc26_rts_dense_city_close_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC26_SourceScaleCompoundLayout_2048/gc26_scale_audit_close_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC26_SourceScaleCompoundLayout_2048/gc26_topdown_contract_visual_proof_2048x2048.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC26_SourceScaleCompoundLayout_2048/gc26_topdown_generator_blueprint_2048x2048.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC26_SourceScaleCompoundLayout_2048/gc26_topdown_module_mask_proof_2048x2048.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: GC17/GC18 walkability visual contract remains the source of truth; GC26 updates the generated road and compound envelopes around source-scale prefabs instead of resizing art.");
        report.AppendLine("User-visible behavior: no shipped runtime behavior changed; generated scene uses larger source-scale compound footprints and rerouted roads so large authored modules are no longer hidden by scale fitting.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureGc26SourceScaleCompoundLayoutBuilder.BuildGc26SourceScaleCompoundLayout2048`.");
        report.AppendLine("Validation result: " + (ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)) ? "failed; see validation log below." : "passed source-scale validation."));
        report.AppendLine("Known gaps: roads are still generated visual surfaces, not final Polygon/Demo road modules; city density is still low compared with the target mockup.");
        report.AppendLine("Cross-lane impacts: Design can now review a source-scale compound blueprint where road routing adapts to real art size.");
        report.AppendLine("Next recommended task: GC27 should replace generated surface strips with real Polygon/Demo road, wall, curb, terrain, and compound modules while preserving source scale 1.");
        report.AppendLine();
        report.AppendLine("Counts:");
        report.AppendLine($"- fitted Demo-authored clusters: {authoredClusterPlacements}");
        report.AppendLine($"- reusable masked modules: {Modules.Count}");
        report.AppendLine($"- procedural districts: {Districts.Count}");
        report.AppendLine($"- macro roads: {MacroRoads.Count}");
        report.AppendLine($"- soldier local zones: {SoldierLocalZones.Count}");
        report.AppendLine($"- vehicle zones: {VehicleZones.Count}");
        report.AppendLine($"- blocker zones: {BlockerZones.Count}");
        report.AppendLine($"- visual placements: {Placements.Count}");
        report.AppendLine($"- primary blocker visuals: {mainBlockerPlacements}");
        report.AppendLine($"- dense blocker detail props: {detailBlockerPlacements}");
        report.AppendLine($"- skipped road-conflict visuals: {SkippedVisuals.Count}");
        report.AppendLine();
        report.AppendLine("Procedural districts:");
        foreach (District district in Districts)
            report.AppendLine($"- {district.Name}: {district.Role}; style={district.Style}; density={district.Density:0.00}");
        report.AppendLine();
        report.AppendLine("Masked modules:");
        foreach (MaskedModule module in Modules)
            report.AppendLine($"- {module.Name}: district={module.DistrictName}; masks={ModuleMasks(module).Count()}; replacement={module.Style}");
        report.AppendLine();
        report.AppendLine("Authored clusters:");
        foreach (AuthoredClusterPlacement cluster in AuthoredClusterPlacements)
            report.AppendLine($"- {cluster.Name}: prefab={cluster.Path}; module={cluster.ModuleName}; mask={cluster.MaskName}; scale={cluster.Scale:0.00}");
        report.AppendLine();
        report.AppendLine("Scale audit:");
        foreach (string line in ScaleAuditLog)
            report.AppendLine("- " + line);
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

    private static Bounds CalculateRendererBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static Rect Expanded(Rect rect, float amount)
    {
        return new Rect(rect.xMin - amount, rect.yMin - amount, rect.width + amount * 2f, rect.height + amount * 2f);
    }

    private static Rect Union(Rect a, Rect b)
    {
        float xMin = Mathf.Min(a.xMin, b.xMin);
        float yMin = Mathf.Min(a.yMin, b.yMin);
        float xMax = Mathf.Max(a.xMax, b.xMax);
        float yMax = Mathf.Max(a.yMax, b.yMax);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
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

    private static void SurfaceBlueprintRect(GameObject parent, string name, float x, float z, float width, float depth, Material material, float y)
    {
        Rect rect = BlueprintRect(x, z, width, depth);
        Surface(parent, name, Center(rect, y), new Vector2(rect.width, rect.height), material, y);
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
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material material = new(shader) { name = name, color = color };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.12f);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
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
