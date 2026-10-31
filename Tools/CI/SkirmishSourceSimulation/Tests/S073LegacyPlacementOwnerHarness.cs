extern alias ActualCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Simulation;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;

internal static class S073LegacyPlacementOwnerHarness
{
    private sealed class ActualBlockerPlacementGrid : IRuntimeGridBlockerPlacementGrid
    {
        private readonly DynamicBuffer<GridWalkable> walkable;
        private readonly DynamicBuffer<GridRoad> roads;
        private readonly Unity.Collections.NativeBitArray blocked;
        public int Width { get; }
        public int Height { get; }
        public ActualBlockerPlacementGrid(GridConfig grid, DynamicBuffer<GridWalkable> walkable,
            DynamicBuffer<GridRoad> roads, DynamicBlockerComponent blockers)
        { Width = grid.Width; Height = grid.Height; this.walkable = walkable; this.roads = roads; blocked = blockers.Blocked; }
        public bool IsWalkable(int index) => walkable[index].Value != 0;
        public bool IsRoad(int index) => roads[index].Value != 0;
        public bool IsBlocked(int index) => blocked.IsCreated && blocked.IsSet(index);
    }

    static readonly string[] ProductionRoots =
    {
        "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_APC_Fast", "Unit_Veh_APC_Heavy",
        "Unit_Veh_APC_Slow", "Unit_Veh_Light_Armored_Car", "Unit_Veh_Missle_Launcher_Air",
        "Unit_Veh_Missle_Launcher_Ground", "Unit_Veh_Radar_Tank", "Unit_Veh_Tank_USA",
        "Unit_Veh_Truck_Canopy", "Unit_Veh_Truck_Tanker", "Unit_Veh_Truck_Tray"
    };
    static readonly string[] InitialRoots =
    {
        "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_Light_Armored_Car",
        "Unit_Veh_Truck_Tray", "Unit_Veh_Truck_Tanker"
    };
    static void Need(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    static string Sha(string path)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    static string YamlScalar(string text, string key)
    {
        MatchCollection rows = Regex.Matches(text, "(?m)^  " + Regex.Escape(key) + @":\s*(.*?)\s*$");
        Need(rows.Count == 1 && rows[0].Groups[1].Value.Length > 0, "Unique serialized S073 AI field required: " + key);
        return rows[0].Groups[1].Value;
    }
    static List<string> YamlStringList(string text, string key)
    {
        string body = Regex.Match(text, "(?ms)^  " + Regex.Escape(key) + @":[ \t]*(?<body>.*?)(?=^  [A-Za-z]|\z)").Groups["body"].Value;
        if (body.Length == 0) throw new InvalidDataException("Missing S073 AI list " + key);
        if (body.Trim() == "[]") return new List<string>();
        var rows = Regex.Matches(body, @"(?m)^  - ([^\r\n]+)$").Cast<Match>().Select(m => m.Groups[1].Value.Trim()).ToList();
        if (rows.Count == 0 || rows.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("Invalid S073 AI list " + key);
        return rows;
    }
    static int YamlInt(string text, string key) => int.Parse(YamlScalar(text, key), CultureInfo.InvariantCulture);
    static float YamlFloat(string text, string key) => float.Parse(YamlScalar(text, key), CultureInfo.InvariantCulture);
    static bool YamlBool(string text, string key) => YamlScalar(text, key) switch { "1" => true, "0" => false, _ => throw new InvalidDataException("Invalid S073 AI boolean " + key) };
    static AIControllerConfig ReadAIController(string path, string expectedName)
    {
        string text = File.ReadAllText(path);
        Need(YamlScalar(text, "m_Name") == expectedName && YamlScalar(text, "m_Script") == "{fileID: 11500000, guid: 85730297a3598ee4d92dcb41787af9ae, type: 3}",
            "Exact original S073 AIControllerConfig source required: " + expectedName);
        var config = ScriptableObject.CreateInstance<AIControllerConfig>();
        config.name = expectedName;
        SetField(config, "enabled", YamlBool(text, "enabled"));
        SetField(config, "role", Enum.ToObject(typeof(AIControllerRole), YamlInt(text, "role")));
        SetField(config, "difficulty", Enum.ToObject(typeof(AIControllerDifficulty), YamlInt(text, "difficulty")));
        SetField(config, "factionId", YamlInt(text, "factionId"));
        SetField(config, "autoControlsPlayerFaction", YamlBool(text, "autoControlsPlayerFaction"));
        SetField(config, "startingMoney", YamlInt(text, "startingMoney"));
        SetField(config, "incomeMultiplier", YamlFloat(text, "incomeMultiplier"));
        SetField(config, "oilSellPrice", YamlInt(text, "oilSellPrice"));
        SetField(config, "fuelSellPrice", YamlInt(text, "fuelSellPrice"));
        SetField(config, "buildIntervalSeconds", YamlFloat(text, "buildIntervalSeconds"));
        SetField(config, "unitProductionIntervalSeconds", YamlFloat(text, "unitProductionIntervalSeconds"));
        SetField(config, "attackIntervalSeconds", YamlFloat(text, "attackIntervalSeconds"));
        SetField(config, "maxActiveAttackGroups", YamlInt(text, "maxActiveAttackGroups"));
        SetField(config, "defenseRadiusCells", YamlInt(text, "defenseRadiusCells"));
        SetField(config, "aggression", YamlFloat(text, "aggression"));
        SetField(config, "preferredBuildingIds", YamlStringList(text, "preferredBuildingIds"));
        SetField(config, "preferredUnitIds", YamlStringList(text, "preferredUnitIds"));
        SetField(config, "preferredVehicleIds", YamlStringList(text, "preferredVehicleIds"));
        return config;
    }
    static AIPlanEntryStartupConfig ReadAIPlan(string path)
    {
        string text = File.ReadAllText(path);
        Need(YamlScalar(text, "m_Name") == "Plan" && YamlScalar(text, "m_Script") == "{fileID: 11500000, guid: 2842cdb0ea2049d2bc13487757390b68, type: 3}",
            "Exact original S073 AI plan config source required");
        var config = ScriptableObject.CreateInstance<AIPlanEntryStartupConfig>();
        config.name = "Plan";
        SetField(config, "fallbackBuildingIds", YamlStringList(text, "fallbackBuildingIds"));
        SetField(config, "fallbackProductionUnitIds", YamlStringList(text, "fallbackProductionUnitIds"));
        return config;
    }
    static OperationMapDefinition ReadSelectedMap(string path)
    {
        string text = File.ReadAllText(path);
        string Root(string key) => Regex.Match(text, "(?m)^  " + Regex.Escape(key) + @":\s*(.+)$").Groups[1].Value.Trim();
        string Section(string name) => Regex.Match(text, "(?ms)^  " + Regex.Escape(name) + @":\r?\n(?<body>.*?)(?=^  [A-Za-z]|\z)").Groups["body"].Value;
        string Nested(string section, string key) => Regex.Match(Section(section), "(?m)^    " + Regex.Escape(key) + @":\s*(.+)$").Groups[1].Value.Trim();
        Vector3 Origin(string value) { var m = Regex.Match(value, @"\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)\}"); return new Vector3(float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture)); }
        Vector2Int Dimensions(string value) { var m = Regex.Match(value, @"\{x:\s*(\d+),\s*y:\s*(\d+)\}"); return new Vector2Int(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)); }
        string surfaceGuid = Regex.Match(Section("mapSurfaceDataReference"), "(?m)^    m_AssetGUID:\\s*(.+)$").Groups[1].Value.Trim();
        var source = (OperationMapDefinition)FormatterServices.GetUninitializedObject(typeof(OperationMapDefinition));
        SetField(source, "operationMapId", Root("operationMapId"));
        SetField(source, "sourceIdentityHash", Root("sourceIdentityHash"));
        SetField(source, "sourceBinding", new OperationMapSourceBindingConfig(
            Nested("sourceBinding", "sourceOperationMapId"), Nested("sourceBinding", "sourceIdentityHash"), Nested("sourceBinding", "sourceContentHash")));
        SetField(source, "bounds", new OperationMapBoundsConfig(
            Origin(Nested("bounds", "worldMin")), Origin(Nested("bounds", "worldMax")),
            Origin(Nested("bounds", "playableMin")), Origin(Nested("bounds", "playableMax")),
            Origin(Nested("bounds", "cameraMin")), Origin(Nested("bounds", "cameraMax"))));
        SetField(source, "gridMetadata", new OperationMapGridMetadataConfig(
            Nested("gridMetadata", "assetGuid"), Nested("gridMetadata", "contentHash"), Origin(Nested("gridMetadata", "origin")),
            Dimensions(Nested("gridMetadata", "dimensions")), float.Parse(Nested("gridMetadata", "cellSize"), CultureInfo.InvariantCulture), 0));
        SetField(source, "surfaceMetadata", new OperationMapSurfaceMetadataConfig(
            Nested("surfaceMetadata", "assetGuid"), Nested("surfaceMetadata", "contentHash"), Nested("surfaceMetadata", "runtimeBlobHash"),
            int.Parse(Nested("surfaceMetadata", "surfaceCount")), int.Parse(Nested("surfaceMetadata", "payloadVersion")),
            byte.Parse(Nested("surfaceMetadata", "payloadEncoding")), float.Parse(Nested("surfaceMetadata", "minimumHeight"), CultureInfo.InvariantCulture),
            float.Parse(Nested("surfaceMetadata", "maximumHeight"), CultureInfo.InvariantCulture)));
        SetField(source, "navigationMetadata", new OperationMapNavigationMetadataConfig(
            Nested("navigationMetadata", "authoredSubSceneGuid"), long.Parse(Nested("navigationMetadata", "gridAuthoringLocalId")),
            int.Parse(Nested("navigationMetadata", "staticGridBlockerCount")), Nested("navigationMetadata", "usesSurfaceMovementMetadata") == "1",
            Nested("navigationMetadata", "supportsDynamicBlockers") == "1", Nested("navigationMetadata", "supportsDynamicOccupancy") == "1"));
        SetField(source, "mapSurfaceDataReference", new AssetReference(surfaceGuid));
        return source;
    }
    static string Required(JObject value, string path) => (string)value.SelectToken(path) ?? throw new InvalidDataException("Missing S073 input: " + path);
    static SystemState State(EntityManager em) => new SystemState { EntityManager = em, Enabled = true };

    static JObject Run(string[] args)
    {
        Need(args.Length == 37, "Expected source templates, S073 configs, Campaign inputs, placement candidate, preset readback, exact AI source pins, captured map-placement data, pinned blocker metadata, captured Campaign asset root, and hash-bound special-building runtime reference");
        string initialTemplatePath = Path.GetFullPath(args[0]), templatePath = Path.GetFullPath(args[2]), identityPath = Path.GetFullPath(args[4]);
        string legacyPath = Path.GetFullPath(args[6]), definitionsPath = Path.GetFullPath(args[8]);
        string sourceRoot = Path.GetFullPath(args[10]);
        string capturedAssetRoot = Path.GetFullPath(args[34]);
        string specialBuildingRuntimeReferencePath = Path.GetFullPath(args[35]);
        string specialBuildingRuntimeReferenceSha = args[36];
        string mapPath = Path.Combine(sourceRoot, "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset");
        string graphPath = Path.GetFullPath(args[11]), initialPath = Path.GetFullPath(args[13]);
        string physicalPath = Path.GetFullPath(args[15]), waterPath = Path.GetFullPath(args[17]), startupPath = Path.GetFullPath(args[19]);
        string presetReadbackPath = Path.GetFullPath(args[25]);
        string roadOutput = Path.GetFullPath(args[22]); Directory.CreateDirectory(roadOutput);
        JObject initialTemplate = JObject.Parse(File.ReadAllText(initialTemplatePath));
        JObject template = JObject.Parse(File.ReadAllText(templatePath));
        JObject legacy = JObject.Parse(File.ReadAllText(legacyPath));
        JObject graph = JObject.Parse(File.ReadAllText(graphPath));
        JObject initialization = JObject.Parse(File.ReadAllText(initialPath));
        Need(Sha(initialTemplatePath) == args[1] && Sha(templatePath) == args[3] && Sha(identityPath) == args[5] && Sha(legacyPath) == args[7] &&
             Sha(definitionsPath) == args[9] && Sha(graphPath) == args[12] && Sha(initialPath) == args[14] &&
             Sha(physicalPath) == args[16] && Sha(waterPath) == args[18] && Sha(startupPath) == args[20] &&
             Sha(presetReadbackPath) == args[26],
             "A pinned S073/Campaign owner input changed");
        Need((string)legacy["kind"] == "S073LegacyInitialSpawnExternalData" && (string)legacy["mission"] == "S073", "Actual S073 legacy config required");
        Need(args[26] == "b0b76f25ff16e67b5fb287fdd9b739754ce3f5baf1e6e93e085e0dbba480ee10" &&
             Required(legacy, "source_artifacts.config_readback_sha256") == args[26],
             "Full source-bound S073 config readback lineage required");
        JObject fullPresetReadback = JObject.Parse(File.ReadAllText(presetReadbackPath));
        JObject presetFields = (JObject)fullPresetReadback["Assets/Game/Resources/SkirmishIndustrialBasin.asset"]?["MonoBehaviour"];
        JObject constructionFields = (JObject)fullPresetReadback["Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset"]?["MonoBehaviour"];
        JObject initialForcesFields = (JObject)fullPresetReadback["Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset"]?["MonoBehaviour"];
        JArray controllerRefs = presetFields?["aiControllers"] as JArray;
        Need((string)presetFields?["m_Name"] == "SkirmishIndustrialBasin" &&
             (int?)presetFields?["reinforcementInfantryTarget"] > 0 && (int?)presetFields?["attackSquadSize"] > 0 &&
             (string)presetFields?["buildingPlacement"]?["guid"] == "79bf06dc9b9c142249fbdc78c6e9a8ef" &&
             controllerRefs?.Count == 2 && (string)controllerRefs[0]?["guid"] == "c3d7226b0272d44dfa219e9185a76087" &&
             (string)controllerRefs[1]?["guid"] == "4e7dd2768a00645fab3308d56ba9352b" &&
             (string)presetFields?["aiPlan"]?["guid"] == "eeead70461e414c80a3753a7987c3f58" &&
             (string)constructionFields?["initialUnitsConfig"]?["guid"] == "aa39266ae0d5e4db0a66069bf1d5a5f4" &&
             (string)initialForcesFields?["m_Name"] == "InitialForces",
             "Original S073 preset-to-construction-to-initial-forces references changed");
        Need((int?)initialForcesFields?["initialMaterials"] == (int?)legacy["initial_spawn"]?["InitialMaterials"] &&
             (int?)initialForcesFields?["materialsCapacity"] == (int?)legacy["initial_spawn"]?["MaterialsCapacity"] &&
             (int?)initialForcesFields?["initialAiMaterials"] == (int?)legacy["initial_spawn"]?["InitialAiMaterials"] &&
             (int?)initialForcesFields?["aiMaterialsCapacity"] == (int?)legacy["initial_spawn"]?["AiMaterialsCapacity"] &&
             (int?)initialForcesFields?["initialFuel"] == (int?)legacy["initial_spawn"]?["InitialFuel"],
             "Legacy initial-spawn values differ from the full original InitialForces readback");
        Need(args[5] == Required(legacy, "source_artifacts.name_readback_sha256"), "S073 source identity readback changed");
        Need((string)initialization["kind"] == "GeneratedMissionInitialization" && (string)initialization["mission_id"] == "S002", "Pinned shared Campaign asset graph source required");
        Need(Sha(mapPath) == args[21], "Current logical S073 OperationMapDefinition changed");
        var expectedInitialRoots = legacy["captured_unit_prefab_roots"] as JObject ?? throw new InvalidDataException("S073 initial prefab key set required");
        Need(JToken.DeepEquals(expectedInitialRoots, new JObject(expectedInitialRoots.Properties().Select(p => new JProperty(p.Name, initialTemplate["opening_prefabs"]?[p.Name])))) &&
             (string)initialTemplate["kind"] == "AuthoredSkirmishPrefabReferenceData" &&
             (string)initialTemplate["authority"] == "ActualSourceAssetsAndNativeBakers_NotMissionState" &&
             (string)initialTemplate["opening_prefabs"]?["Unit_Chr_Soldier_Male_02_Alt_04"] != null,
             "Pinned production source capture must contain the exact four S073 legacy opening roots");
        Need(JToken.DeepEquals(expectedInitialRoots, new JObject(expectedInitialRoots.Properties().Select(p => new JProperty(p.Name, template["opening_prefabs"]?[p.Name])))),
             "The production source capture changed an initial S073 prefab root");
        JObject sourceIdentity = JObject.Parse(File.ReadAllText(identityPath));
        Need(Sha(identityPath) == args[5] &&
             (string)sourceIdentity["kind"] == "S073LegacyPrefabSourceNameReadback" &&
             (string)sourceIdentity["mission"] == "S073" &&
             (string)sourceIdentity["prefab_capture_sha256"] == "ee7f8dbfacde7d775a32b9cd87d7a321a0ade0af131da9a9aa0417efcc5d41d4",
             "Pinned S073 source-name readback and its historical capture hash required");
        foreach (JProperty root in expectedInitialRoots.Properties())
            Need(sourceIdentity["objects"]?.Children<JObject>().Any(row => (string)row["unit_source_key"] == root.Name &&
                 (string)row["game_object_name"] == root.Name) == true,
                 "Independent S073 name readback lacks opening prefab root: " + root.Name);
        OperationMapDefinition selected = ReadSelectedMap(mapPath);
        SourceS073LegacySpawnRequestAdapter.RequireSharedPlacementSource(selected);

        // Reject source-bound placement candidates that fit the grid but cross the
        // selected logical map's authored playable rectangle. Do this before the
        // expensive Campaign surface/materialization path so failed candidates are cheap.
        string placementConfigPath = Path.GetFullPath(args[23]);
        var placementConfig = JObject.Parse(File.ReadAllText(placementConfigPath));
        bool prefixReplayConfig = (int?)placementConfig["schema_version"] == 2 &&
             (string)placementConfig["kind"] == "S073LegacyBuildingOriginsCandidateV2" &&
             (string)placementConfig["authority"] == "DerivedByOriginalPlacementOwnerPrefix_RingOrderReplay_ValidationCandidateOnly";
        bool originalSearchConfig = (int?)placementConfig["schema_version"] == 1 &&
             (string)placementConfig["kind"] == "S073LegacyBuildingOriginsCandidateV1" &&
             (string)placementConfig["authority"] == "DerivedByOriginalBuildingPlacementOwner_ValidationCandidateOnly";
        Need(Sha(placementConfigPath) == args[24] && (prefixReplayConfig || originalSearchConfig) &&
             (string)placementConfig["operation_map_id"] == selected.OperationMapId &&
             (string)placementConfig["campaign_source"] == selected.SourceBinding.SourceOperationMapId,
             "Exact external S073 placement candidate required before source-fit validation");
        var playableMin = selected.Bounds.PlayableMin;
        var playableMax = selected.Bounds.PlayableMax;
        var candidateRows = placementConfig["overrides"] as JArray;
        Need(candidateRows != null && candidateRows.Count == 10, "Complete S073 placement candidates required");
        foreach (var row in candidateRows)
        {
            var origin = row["candidate_origin"] as JArray;
            var footprint = row["footprint"] as JArray;
            Need(origin?.Count == 2 && footprint?.Count == 2, "Exact S073 candidate origin and footprint required");
            float minX = selected.GridMetadata.Origin.x + (int)origin[0] * selected.GridMetadata.CellSize;
            float minZ = selected.GridMetadata.Origin.z + (int)origin[1] * selected.GridMetadata.CellSize;
            float maxX = minX + (int)footprint[0] * selected.GridMetadata.CellSize;
            float maxZ = minZ + (int)footprint[1] * selected.GridMetadata.CellSize;
            Need(minX >= playableMin.x && minZ >= playableMin.z && maxX <= playableMax.x && maxZ <= playableMax.z,
                 "S073 initial building footprint crosses the authored playable bounds: " + (string)row["building_id"]);
        }

        var materializer = new SourceConfigMaterializer(graph);
        const string gridPath = "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset";
        const string surfacePath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";
        var gridData = materializer.ByPath<GridAuthoringSceneConfigAsset>(gridPath);
        var surfaceData = materializer.ByPath<MapSurfaceDataAsset>(surfacePath);
        var gridMeta = selected.GridMetadata;
        var surfaceMeta = selected.SurfaceMetadata;
        Need(gridData.Width == gridMeta.Dimensions.x && gridData.Height == gridMeta.Dimensions.y && gridData.CellSize == gridMeta.CellSize &&
             gridData.Origin.x == gridMeta.Origin.x && gridData.Origin.y == gridMeta.Origin.y && gridData.Origin.z == gridMeta.Origin.z,
             "Current baked S073 Campaign grid differs from selected logical binding");
        Need(surfaceMeta.AssetGuid == selected.MapSurfaceDataReference.AssetGUID && surfaceMeta.SurfaceCount == gridData.Width * gridData.Height,
             "Current baked S073 Campaign surface differs from selected logical binding");

        using var autonomousWorld = new AutonomousWorld();
        var em = autonomousWorld.Entities;
        em.LogicalAllocatorAdmitted = true;
        int next = 1; em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var surfaceBlob = surfaceData.TryCreateRuntimeBlobAsset(Allocator.Persistent, out var blob) ? blob : throw new InvalidDataException("Original Campaign surface decoder rejected payload");
        ref MapSurfaceBlob surface = ref surfaceBlob.Value;
        Need(surface.Dimensions.x == gridData.Width && surface.Dimensions.y == gridData.Height && surface.CellSize == gridData.CellSize &&
             surface.GridOrigin.x == gridData.Origin.x && surface.GridOrigin.y == gridData.Origin.y && surface.GridOrigin.z == gridData.Origin.z,
             "Decoded surface and Campaign grid frames differ");
        GeneratedMapSurfaceFlags.GetSurfaceFeatureFlags(ref surface, out byte layered, out byte roadSurfaces, out byte bridges);
        Entity surfaceEntity = em.CreateEntity();
        em.AddComponentData(surfaceEntity, new MapSurfaceComponent { SurfaceBlob = surfaceBlob, GridOrigin = surface.GridOrigin, CellSize = surface.CellSize,
            Dimensions = surface.Dimensions, HasSurfaceData = 1, HasLayeredCells = layered, HasRoadSurfaces = roadSurfaces, HasBridgeSurfaces = bridges });
        em.AddComponentData(surfaceEntity, new MapSurfacePathCostComponent { EnableSlopeCost = 0, GentleSlopeTraversalCost = 0, SteepSlopeTraversalCost = 0 });
        Entity gridEntity = em.CreateEntity(); GeneratedGridSourceBaking.Bake(em, gridEntity, gridData);
        GeneratedFreshGridStorage.Initialize(em, gridEntity);
        int authoredNavigation = SourceNavigationFacts.Import(em, (JObject)initialization["navigation_reference"]);

        string scenePath = Path.Combine(sourceRoot, "Assets/Game/Scenes/Match.unity");
        var roadRun = new JObject { ["schema_version"] = 1, ["kind"] = "SkirmishSourceSimulationRun", ["mission"] = "S073",
            ["repository_root"] = sourceRoot, ["source_repository_root"] = sourceRoot, ["output_directory"] = roadOutput,
            ["mission_scene"] = new JObject { ["path"] = scenePath, ["sha256"] = Sha(scenePath) } };
        autonomousWorld.PrepareFreshInitialRoads(roadRun);
        autonomousWorld.SyncS073LegacyDiagnosticRoads(gridEntity);

        string blockerMetadataPath = Path.GetFullPath(args[32]);
        Need(args[33] == "e73b06fb4913d442139071cc6739aa4d9672aa312b6543a31f377d139aab8776" &&
             Sha(blockerMetadataPath) == args[33], "Exact reviewed original S073 runtime blocker metadata required");
        JObject blockerCapture = JObject.Parse(File.ReadAllText(blockerMetadataPath));
        JObject blockerConfig = blockerCapture["config"] as JObject;
        JArray blockerPrefabRows = blockerCapture["prefabs"] as JArray;
        Need((string)blockerCapture["kind"] == "S073OriginalRuntimeBlockerPrefabMetadata" &&
             (string)blockerCapture["authority"] == "MatchSceneConfig_OriginalRuntimeGridBlockerPresentationSystemHelper.BuildMetadata" &&
             (bool?)blockerConfig?["spawn_on_start"] == true && (int?)blockerConfig?["blocker_count"] == 1 &&
             (int?)blockerConfig?["random_seed"] == 24680 && (int?)blockerConfig?["prefab_count"] == 63 &&
             blockerPrefabRows?.Count == 63 && (bool?)blockerCapture["random_placement_executed"] == false &&
             (bool?)blockerCapture["blocker_spawned"] == false,
             "Source blocker capture must be config/metadata only, with no claimed placement instance");
        var blockerPrefabs = new List<RuntimeGridBlockerPrefabData>(blockerPrefabRows.Count);
        foreach (JObject row in blockerPrefabRows)
        {
            string name = (string)row["name"];
            string kind = RuntimeGridBlockerPlacementKernel.ClassifyPrefabKind(name).ToString();
            JArray bounds = row["local_bounds_size"] as JArray;
            Need(!string.IsNullOrWhiteSpace(name) && kind == (string)row["kind"] &&
                 (bool?)row["has_bounds"] == true && bounds?.Count == 3,
                 "Exact original S073 blocker prefab metadata row required");
            blockerPrefabs.Add(new RuntimeGridBlockerPrefabData { Name = name,
                Kind = (BlockerPrefabKind)Enum.Parse(typeof(BlockerPrefabKind), kind),
                BlocksOnlyCenterCell = (bool?)row["blocks_only_center_cell"] == true,
                HasBounds = true, BoundsWidth = (float)bounds[0], BoundsDepth = (float)bounds[2] });
        }
        blockerPrefabs.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        autonomousWorld.InitializeS073LegacyDiagnosticBlockerOwners(gridEntity);
        DynamicBuffer<GridWalkable> blockerWalkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBuffer<GridRoad> blockerRoads = em.GetBuffer<GridRoad>(gridEntity);
        var sourceGrid = em.GetComponentData<GridConfig>(gridEntity);
        var sourceBlockerState = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        Need(blockerWalkable.Length == sourceGrid.Width * sourceGrid.Height && blockerRoads.Length == blockerWalkable.Length &&
             sourceBlockerState.Blocked.IsCreated && sourceBlockerState.Blocked.Length == blockerWalkable.Length,
             "Actual selected Campaign grid predicates and shared blocker storage required");
        List<RuntimeGridBlockerPlacementData> actualBlockerPlan = RuntimeGridBlockerPlacementPolicy.Plan(
            new ActualBlockerPlacementGrid(sourceGrid, blockerWalkable, blockerRoads, sourceBlockerState), blockerPrefabs,
            (int)blockerConfig["blocker_count"], (uint)(int)blockerConfig["random_seed"], sourceGrid.CellSize,
            (float)blockerConfig["tree_spawn_ratio"], (int)blockerConfig["tree_cluster_count"],
            (int)blockerConfig["tree_cluster_spacing_min_cells"], (int)blockerConfig["tree_cluster_spacing_max_cells"],
            (int)blockerConfig["tree_cluster_distance_min_cells"], (int)blockerConfig["tree_cluster_distance_max_cells"]);
        var runtimeBlockerOwnership = new RuntimeGridBlockerOwnershipKernel();
        runtimeBlockerOwnership.InitializeGrid(sourceGrid.Width, sourceGrid.Height);
        var actualBlockerRows = new JArray();
        foreach (RuntimeGridBlockerPlacementData placement in actualBlockerPlan)
        {
            int blockerId = runtimeBlockerOwnership.AllocateId();
            Entity blockerEntity = em.CreateEntity();
            em.AddComponentData(blockerEntity, new UnitGrid { Cell = placement.OriginCell });
            em.AddComponentData(blockerEntity, new GridBlockerSize { Size = placement.SizeCells });
            em.AddComponent<StaticGridBlocker>(blockerEntity);
            runtimeBlockerOwnership.Register(blockerId, blockerEntity, placement.OriginCell, placement.SizeCells);
            autonomousWorld.UpdateS073LegacyDiagnosticBlockerOwners();
            sourceBlockerState = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            bool appliedBlocked = true;
            for (int y = placement.OriginCell.y; y < placement.OriginCell.y + placement.SizeCells.y; y++)
                for (int x = placement.OriginCell.x; x < placement.OriginCell.x + placement.SizeCells.x; x++)
                    appliedBlocked &= sourceBlockerState.Blocked.IsSet(x + y * sourceGrid.Width);
            Need(appliedBlocked, "Original StaticGridBlockerUpdateSystem did not apply the placed blocker footprint");
            actualBlockerRows.Add(new JObject { ["prefab"] = blockerPrefabs[placement.PrefabIndex].Name,
                ["kind"] = placement.Kind.ToString(), ["origin_cell"] = new JArray(placement.OriginCell.x, placement.OriginCell.y),
                ["footprint"] = new JArray(placement.SizeCells.x, placement.SizeCells.y),
                ["original_static_blocker_owner_applied"] = appliedBlocked });
        }
        var actualBlockerPlacement = new JObject { ["source_metadata_sha256"] = args[33],
            ["actual_selected_campaign_grid_used"] = true, ["native_grid_predicates_used"] = true,
            ["seed"] = (int)blockerConfig["random_seed"], ["requested_count"] = (int)blockerConfig["blocker_count"],
            ["placed_count"] = actualBlockerRows.Count, ["placements"] = actualBlockerRows,
            ["shared_runtime_blocker_ownership_kernel_invoked"] = runtimeBlockerOwnership.Count == actualBlockerPlan.Count,
            ["original_grid_storage_and_static_blocker_owners_invoked"] = true,
            ["world_is_full_mission"] = false };

        var initialData = legacy["initial_spawn"].ToObject<LegacyInitialSpawnData>();
        SourceS073LegacyPresetProjection.Install(3, presetFields, initialData);
        string enemyAIPath = Path.Combine(args[10], "Assets/Game/Configs/Skirmish/Enemy.asset");
        string playerAIPath = Path.Combine(args[10], "Assets/Game/Configs/Skirmish/Player.asset");
        string aiPlanPath = Path.Combine(args[10], "Assets/Game/Configs/Skirmish/Plan.asset");
        Need(Sha(enemyAIPath) == args[27] && Sha(playerAIPath) == args[28] && Sha(aiPlanPath) == args[29],
            "Current exact S073 AI config inputs changed");
        // The production capture is one complete source namespace, including the four exact
        // initial roots checked above. Importing the focused capture separately would allocate
        // the same source Entity identities twice in this manager.
        var importedProduction = GeneratedReferenceTemplateImporter.Import(em, template, Path.GetDirectoryName(templatePath), ProductionRoots);
        var templates = importedProduction.Prefabs;
        Need(InitialRoots.All(templates.ContainsKey) && ProductionRoots.All(templates.ContainsKey), "Complete source startup/production prefab namespace required");
        var initialUnitTemplates = InitialRoots.ToDictionary(key => key, key => templates[key], StringComparer.Ordinal);
        var initialized = new CustomGameStartupSystemHelper(em).InitializeFromLegacyData(initialData, initialUnitTemplates);
        Need(initialized.Initialized && initialized.FactionCount == 2 && initialized.InitialBuildingEntryCount == 10, "Original S073 legacy startup owner failed");
        Entity plan;
        using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<CustomGameStartupStateComponent>(), ComponentType.ReadOnly<InitialUnitsSpawnConfig>()))
        { Need(query.CalculateEntityCount() == 1, "Unique actual legacy startup plan required"); plan = query.GetSingletonEntity(); }
        var session = em.CreateEntity(); em.AddComponentData(session, new SkirmishMatchState { SessionId = new FixedString64Bytes("S073-legacy-placement"),
            ScenarioIndex = 3, Phase = SkirmishPhase.Preparing, StartupFailure = SkirmishStartupFailureCode.None, Outcome = SkirmishOutcome.None, Reason = SkirmishEndReason.None });
        em.AddBuffer<SkirmishTrackedUnit>(session);
        Entity boundary = em.CreateEntity(); em.AddComponentData(boundary, new BuildingRuntimeStateTag());
        var requests = em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
        var definitions = GeneratedBuildingDefinitions.LoadS073Legacy(definitionsPath, args[9], templates, legacy, sourceRoot);
        Need(definitions.Definitions.Count == 6, "Exact S073 original building definitions required");
        var configured = em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
        var configuredSpawnableSourceRows = new JArray();
        foreach (var d in definitions.Definitions)
        {
            Need(definitions.AssetIdentities.TryGetValue(d.Prefab.name, out JObject sourcePrefabIdentity),
                "Exact captured source prefab identity required for configured building metadata: " + d.Prefab.name);
            bool canRequest = SourceS073LegacyPresetProjection.ReadBuildingCanRequest(
                sourceRoot, d.Prefab.name, sourcePrefabIdentity, out JObject requestabilityEvidence);
            string normalizedBuildingId = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(d.Prefab.name);
            configured.Add(new BuildingConfiguredSpawnableReadModel
            {
                BuildingId = new FixedString128Bytes(normalizedBuildingId),
                DisplayName = new FixedString128Bytes(d.DisplayName ?? string.Empty),
                Price = Math.Max(0, d.CreditsCost),
                MaterialsCost = Math.Max(0, d.MaterialsCost),
                FootprintCells = new int2(Math.Max(1, d.FootprintCells.x), Math.Max(1, d.FootprintCells.y)),
                CanRequest = canRequest ? (byte)1 : (byte)0
            });
            configuredSpawnableSourceRows.Add(new JObject
            {
                ["building_id"] = normalizedBuildingId,
                ["display_name"] = d.DisplayName,
                ["price"] = Math.Max(0, d.CreditsCost),
                ["materials_cost"] = Math.Max(0, d.MaterialsCost),
                ["footprint"] = new JArray(Math.Max(1, d.FootprintCells.x), Math.Max(1, d.FootprintCells.y)),
                ["can_request"] = canRequest,
                ["authoritative_requestability_source"] = requestabilityEvidence
            });
        }
        Need(configured.Length == 6 && configured.Any(row => row.BuildingId.ToString() == "building_guardtower" &&
             row.CanRequest == 1 && row.Price == 22000 && row.MaterialsCost == 50),
            "S073 GuardTower configured spawnable must use exact source identity and authored costs/requestability");

        var domain = SourceS073LegacySpawnRequestAdapter.LoadSharedPlacementSource(selected, physicalPath, args[16], waterPath, args[18], startupPath, args[20], sourceRoot, capturedAssetRoot);
        var masks = domain.Build(gridData.Width, gridData.Height, gridData.CellSize, gridData.Origin.x, gridData.Origin.y, gridData.Origin.z);
        var roads = new bool[checked(gridData.Width * gridData.Height)]; BuildingPlacementRoadSurfaceMask.Append(em, surfaceEntity, em.GetComponentData<GridConfig>(gridEntity), roads);
        if (domain.UsesOriginalOwnerMaskCapture) roads = domain.CopyOriginalOwnerRoadMask();
        var water = domain.BuildWaterMask(true, gridData.Width, gridData.Height, gridData.CellSize, gridData.Origin.x, gridData.Origin.y, gridData.Origin.z);
        var grid = em.GetComponentData<GridConfig>(gridEntity);
        string mapCapturePath = Path.GetFullPath(args[30]);
        JObject mapPlacementCapture = JObject.Parse(File.ReadAllText(mapCapturePath));
        JObject specialBuildingRuntimeReference = JObject.Parse(File.ReadAllText(specialBuildingRuntimeReferencePath));
        Need(Sha(specialBuildingRuntimeReferencePath) == specialBuildingRuntimeReferenceSha &&
             specialBuildingRuntimeReferenceSha == "0eb6f1fb3eacac1a6fde4986b3b8118923ce021f48fa245279949385e52c2b1e" &&
             (int?)specialBuildingRuntimeReference["schema_version"] == 1 &&
             (string)specialBuildingRuntimeReference["kind"] == "S073OriginalSpecialBuildingRuntimeReference" &&
             (string)specialBuildingRuntimeReference["catalog_id"] == "S073" &&
             (bool?)specialBuildingRuntimeReference["mission_world_allocated"] == false &&
             (bool?)specialBuildingRuntimeReference["full_mission_initialized"] == false &&
             (bool?)specialBuildingRuntimeReference["native_visual_readiness"] == false &&
             ((JArray)specialBuildingRuntimeReference["road_gates"]).Select(item => (int)item["row_index"]).SequenceEqual(new[] { 108, 109, 110 }),
             "Pinned non-world original source airport/runway and road-gate data required");
        Need(Sha(mapCapturePath) == args[31] && args[31] == "a0107fcdaaf7ace274345b4615292134ec5e8547b456dffdb3c6b857dc72ae73" &&
             (int?)mapPlacementCapture["schema_version"] == 1 && (string)mapPlacementCapture["kind"] == "S073OriginalMapBuildingReferences" &&
             (string)mapPlacementCapture["operation_map_id"] == selected.OperationMapId &&
             (string)mapPlacementCapture["source_identity_hash"] == selected.SourceIdentityHash &&
             (string)mapPlacementCapture["physical_source_binding"]?["operation_map_id"] == selected.SourceBinding.SourceOperationMapId &&
             (string)mapPlacementCapture["placement_config"]?["path"] == "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01_BuildingPlacements.asset" &&
             (string)mapPlacementCapture["placement_config"]?["guid"] == "a84d1564be130447197d5a5f2a7fe11d" &&
             (int?)mapPlacementCapture["placement_config"]?["placement_count"] == 432 &&
             (mapPlacementCapture["unsupported_values"] as JArray)?.Count == 0 &&
             (bool?)mapPlacementCapture["placements_processed"] == false &&
             (bool?)mapPlacementCapture["mission_world_allocated"] == false &&
             (bool?)mapPlacementCapture["full_mission_initialized"] == false &&
             (bool?)mapPlacementCapture["native_visual_readiness"] == false,
             "Exact non-world source capture for the S073-bound original map placement config required");
        var mapDefinitions = GeneratedBuildingDefinitions.LoadS073MapPlacements(mapCapturePath, args[31], sourceRoot, templates);
        var mapPlacementOriginReplay = ReplayOriginalMapPlacementOrigins(mapPlacementCapture, grid, mapDefinitions.Definitions);
        bool RuntimeBlocked(int x, int z, int width, int height)
        {
            var blocked = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
            for (int y = z; y < z + height; y++) for (int xx = x; xx < x + width; xx++)
                if (xx >= 0 && y >= 0 && xx < grid.Width && y < grid.Height && blocked.IsSet(y * grid.Width + xx)) return true;
            return false;
        }
        // Load the previously owner-derived candidate as external mission data.
        // This does not edit the authored legacy artifact, Campaign map, or shared spawn rules.
        var factionSpawns = em.GetBuffer<InitialUnitsFactionSpawnEntry>(plan);
        var factionSpawnById = new Dictionary<byte, int2>();
        for (int i = 0; i < factionSpawns.Length; i++) factionSpawnById.Add(factionSpawns[i].FactionId, factionSpawns[i].SpawnCell);
        var initialBuildings = em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(plan);
        var placementCandidates = new JArray();
        Need(Sha(placementConfigPath) == args[24] && (prefixReplayConfig || originalSearchConfig) &&
             (string)placementConfig["mission_id"] == "S073" &&
             (string)placementConfig["operation_map_id"] == selected.OperationMapId &&
             (string)placementConfig["campaign_source"] == selected.SourceBinding.SourceOperationMapId &&
             (string)placementConfig["source_identity_hash"] == selected.SourceIdentityHash &&
             (string)placementConfig["legacy_initial_spawn_sha256"] == args[7] &&
             (string)placementConfig["building_definitions_sha256"] == args[9] &&
             (string)placementConfig["grid_sha256"] == selected.GridMetadata.ContentHash &&
             (string)placementConfig["surface_sha256"] == selected.SurfaceMetadata.ContentHash &&
             (string)placementConfig["operation_map_sha256"] == args[21] &&
             (bool?)placementConfig["geometry_changed"] == false &&
             (bool?)placementConfig["budgets_changed"] == false &&
             (bool?)placementConfig["shared_spawn_rule_changed"] == false,
             "Exact external S073 candidate config source binding required");
        var overrides = placementConfig["overrides"] as JArray;
        Need(overrides != null && overrides.Count == initialBuildings.Length && initialBuildings.Length == 10,
             "Complete ordered S073 external origin override rows required");
        int originalOwnerSearchRows = 0, originalPrefixReplayRows = 0;
        for (int i = 0; i < initialBuildings.Length; i++)
        {
            var entry = initialBuildings[i];
            var factionSpawn = factionSpawnById[entry.FactionId];
            var originalOrigin = factionSpawn + entry.OriginOffset;
            string key = entry.PrefabLookupKey.ToString();
            var definition = definitions.Definitions.Single(value =>
                BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(value.Prefab.name) == key);
            var sourceOverride = overrides[i] as JObject;
            var original = sourceOverride?["original_origin"] as JArray;
            var candidate = sourceOverride?["candidate_origin"] as JArray;
            var footprint = sourceOverride?["footprint"] as JArray;
            string searchPolicy = (string)sourceOverride?["search_policy"];
            bool originalOwnerSearch = searchPolicy == "BuildingRuntimeSpawnCompositionSystemHelper.TryFindValidInitialBuildingOrigin";
            bool originalPrefixReplay = searchPolicy == "BuildingRuntimeSpawnCompositionSystemHelper.TryFindValidInitialBuildingOrigin_RingOrderReplay";
            bool searchEvidenceValid = originalOwnerSearch && (bool?)sourceOverride["original_owner_search_found"] == true ||
                originalPrefixReplay && prefixReplayConfig && i == 8 &&
                (bool?)sourceOverride["original_owner_search_found"] == false &&
                (bool?)sourceOverride["original_owner_prefix_replay_found"] == true &&
                (bool?)sourceOverride["runtime_owner_acceptance_pending"] == true;
            Need(sourceOverride != null && (int?)sourceOverride["request_index"] == i &&
                 (int?)sourceOverride["faction_id"] == entry.FactionId &&
                 (string)sourceOverride["building_id"] == key &&
                 searchEvidenceValid &&
                 original?.Count == 2 && (int?)original[0] == originalOrigin.x && (int?)original[1] == originalOrigin.y &&
                 candidate?.Count == 2 && footprint?.Count == 2 &&
                 (int?)footprint[0] == definition.FootprintCells.x && (int?)footprint[1] == definition.FootprintCells.y,
                 "External S073 origin row differs from current legacy plan or source definition");
            if (originalOwnerSearch) originalOwnerSearchRows++;
            if (originalPrefixReplay) originalPrefixReplayRows++;
            var candidateOrigin = new Vector2Int((int)candidate[0], (int)candidate[1]);
            placementCandidates.Add(new JObject { ["request_index"] = i, ["faction_id"] = entry.FactionId,
                ["building_id"] = key, ["original_origin"] = new JArray(originalOrigin.x, originalOrigin.y),
                ["footprint"] = new JArray(definition.FootprintCells.x, definition.FootprintCells.y),
                ["original_owner_search_found"] = originalOwnerSearch,
                ["original_owner_prefix_replay_found"] = originalPrefixReplay,
                ["candidate_origin"] = new JArray(candidateOrigin.x, candidateOrigin.y),
                ["source_config_sha256"] = args[24] });
            entry.OriginOffset = new int2(candidateOrigin.x - factionSpawn.x, candidateOrigin.y - factionSpawn.y);
            initialBuildings[i] = entry;
        }
        Need(originalOwnerSearchRows == (prefixReplayConfig ? 9 : 10) &&
             originalPrefixReplayRows == (prefixReplayConfig ? 1 : 0),
             "S073 candidate must preserve nine original owner results and at most one explicit refinery prefix replay");
        string constructionPath = Path.Combine(sourceRoot, "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset");
        string constructionText = File.ReadAllText(constructionPath);
        var planeMatch = Regex.Match(constructionText, @"(?m)^  buildPlaneY: ([^\r\n]+)$");
        if (!planeMatch.Success || !float.TryParse(planeMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float buildPlaneY))
            throw new InvalidDataException("Actual S073 build plane required");
        var registry = new Dictionary<int, RuntimeBuildingEntity>();
        using var startupRedirectQuery = em.CreateEntityQuery(typeof(UnitMove), typeof(UnitGrid), typeof(LocalTransform));
        var startupRedirectOwner = new BuildingPlacementRedirectCompositionSystemHelper();
        var startupRedirectContext = new BuildingPlacementRedirectCompositionSystemHelper.Context(
            (out EntityManager manager) => { manager = em; return true; },
            (out Entity sourceGridEntity, out GridConfig sourceGridConfig, out DynamicBuffer<GridRoad> sourceRoads,
                out DynamicBlockerComponent sourceBlockers) =>
            {
                sourceGridEntity = gridEntity; sourceGridConfig = em.GetComponentData<GridConfig>(gridEntity);
                sourceRoads = em.GetBuffer<GridRoad>(gridEntity); sourceBlockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity); return true;
            }, _ => { }, () => startupRedirectQuery);
        Action<RectInt> startupRedirectUnits = rect => startupRedirectOwner.RedirectUnitsAroundPlacedBuilding(startupRedirectContext, rect);
        var pipeline = new GeneratedStartingBuildingPipeline(em, gridEntity, boundary, definitions.Definitions, true, registry,
            masks.AuthoredBuildings, roads, masks.Sidewalks, water, buildPlaneY, RuntimeBlocked, startupRedirectUnits);
        var gameplayStartOwner = new RuntimeGameplayStateSystem(em);
        gameplayStartOwner.ResetForGameplayStart();
        var spawnSystem = new InitialUnitsSpawnSystem();
        var spawnState = new SystemState { EntityManager = em, Enabled = true };
        spawnSystem.OnCreate(ref spawnState);
        var beforeSpawn = new HashSet<Entity>();
        using (var unitsBefore = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitSourcePrefabKey>()))
        using (var entities = unitsBefore.ToEntityArray(Allocator.Temp))
            for (int i = 0; i < entities.Length; i++) beforeSpawn.Add(entities[i]);

        // Let the original owner issue the authored building requests, then run the
        // existing request processor before the next original spawn-system update.
        spawnState.Begin();
        try { spawnSystem.OnUpdate(ref spawnState); }
        finally { spawnState.End(); }
        requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
        int queued = requests.Length;
        Need(queued == 10, "Original S073 initial-spawn owner did not queue all source rows");
        pipeline.ProcessRequests(1f / 60f, forceScan: true);
        int frames = 1;
        requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
        var rows = new JArray(requests.Select(r => new JObject { ["request_id"] = r.RequestId, ["building_id"] = r.BuildingId.ToString(),
            ["faction_id"] = r.FactionId, ["preferred_origin"] = new JArray(r.PreferredOrigin.x, r.PreferredOrigin.y), ["status"] = r.Status.ToString(),
            ["result_code"] = r.ResultCode, ["building_runtime_id"] = r.BuildingRuntimeId, ["actual_origin"] = new JArray(r.ActualOrigin.x, r.ActualOrigin.y),
            ["actual_footprint"] = new JArray(r.ActualFootprint.x, r.ActualFootprint.y) }));
        var buildings = new JArray(registry.OrderBy(pair => pair.Key).Select(pair => new JObject { ["runtime_id"] = pair.Key,
            ["definition"] = pair.Value.Definition.Prefab.name, ["origin"] = new JArray(pair.Value.OriginCell.x, pair.Value.OriginCell.y),
            ["combat_entity"] = pair.Value.CombatEntity.Index + ":" + pair.Value.CombatEntity.Version,
            ["blocker_entity"] = pair.Value.BlockerEntity.Index + ":" + pair.Value.BlockerEntity.Version }));
        foreach (var pair in registry) autonomousWorld.Buildings.Add(pair.Key, pair.Value);
        Entity gameplayStateEntity;
        using (var gameplayStates = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
        {
            Need(gameplayStates.CalculateEntityCount() == 1, "Original gameplay-start owner must create one real runtime state");
            gameplayStateEntity = gameplayStates.GetSingletonEntity();
        }
        var gameplayStateAfterRequest = em.GetComponentData<RuntimeGameplayStateComponent>(gameplayStateEntity);
        Need(gameplayStateAfterRequest.PlayRequested == 1 && gameplayStateAfterRequest.SimulationActive == 0 &&
             gameplayStateAfterRequest.SuppressNextWorldClick == 1,
             "Original ResetForGameplayStart must request play without bypassing the loading gate");
        var rulesSystem = new SkirmishRulesSystem();
        var rulesState = State(em);
        rulesSystem.OnCreate(ref rulesState);
        Need(rulesState.RequiredQueriesPresent, "Original SkirmishRulesSystem match/gameplay required queries must be present");
        int spawnFrames = 0;
        // The source owner rate-limits authored requests at normal 1/60 steps.
        // Allow the full original startup window instead of an artificial 12-frame cutoff.
        for (; spawnFrames < 128 && !em.HasComponent<InitialUnitsSpawnInitialized>(plan); spawnFrames++)
        {
            spawnState.Begin();
            try { spawnSystem.OnUpdate(ref spawnState); }
            finally { spawnState.End(); }
            pipeline.ProcessRequests((spawnFrames + 2) / 60f);
            frames++;
        }
        var newlySpawned = new List<Entity>();
        var registeredBuildingEntities = new HashSet<Entity>();
        foreach (RuntimeBuildingEntity building in registry.Values)
        {
            if (building.CombatEntity != Entity.Null) registeredBuildingEntities.Add(building.CombatEntity);
            if (building.BlockerEntity != Entity.Null) registeredBuildingEntities.Add(building.BlockerEntity);
        }
        using (var unitsAfter = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitSourcePrefabKey>()))
        using (var entities = unitsAfter.ToEntityArray(Allocator.Temp))
            for (int i = 0; i < entities.Length; i++)
                if (!beforeSpawn.Contains(entities[i]) && !registeredBuildingEntities.Contains(entities[i])) newlySpawned.Add(entities[i]);
        if (!em.HasComponent<InitialUnitsSpawnInitialized>(plan) || newlySpawned.Count != 24)
        {
            InitialUnitsSpawnProgress progress = em.HasComponent<InitialUnitsSpawnProgress>(plan)
                ? em.GetComponentData<InitialUnitsSpawnProgress>(plan) : default;
            string unitProgress = em.HasBuffer<InitialUnitsFactionUnitSpawnProgress>(plan)
                ? string.Join(",", em.GetBuffer<InitialUnitsFactionUnitSpawnProgress>(plan).Select(row => row.Spawned)) : "missing";
            string requestStatus = em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary)
                ? string.Join(",", em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Select(row => row.Status + ":" + row.ResultCode)) : "missing";
            throw new InvalidOperationException($"Original S073 spawn completion absent: frames={spawnFrames} initialized={em.HasComponent<InitialUnitsSpawnInitialized>(plan)} spawned={newlySpawned.Count} buildingRequestsIssued={progress.InitialBuildingRequestsIssued} buildingsSpawned={progress.InitialBuildingsSpawned} blockersSpawned={progress.BlockersSpawned} unitProgress=[{unitProgress}] requestStatus=[{requestStatus}]");
        }
        int factionOneUnits = newlySpawned.Count(entity => em.GetComponentData<Faction>(entity).Id == 1);
        int factionTwoUnits = newlySpawned.Count(entity => em.GetComponentData<Faction>(entity).Id == 2);
        Need(factionOneUnits == 12 && factionTwoUnits == 12, "Original initial-unit spawner changed S073 faction grants");
        // Retain the completed original startup while its canonical registry still contains
        // exactly the ten legacy initial buildings. The separate captured map row below is
        // then admitted as a later source placement, not miscounted as startup output.
        autonomousWorld.RetainS073LegacyDiagnosticStartup(em, gridEntity, surfaceEntity, session, boundary, plan, newlySpawned);

        // The capture rows describe the S073 logical map, while authored Campaign
        // entities retain the map ID of the physical source declared by its binding.
        // Join on that explicit binding instead of reporting every source row missing.
        string sourceOperationMapId = selected.SourceBinding.SourceOperationMapId;
        var bakedMapEntitiesByPlacementIndex = new Dictionary<int, List<Entity>>();
        using (var bakedMapQuery = em.CreateEntityQuery(typeof(OperationMapBuildingComponent), typeof(RuntimeBuildingCombatInfo)))
        using (var bakedMapEntities = bakedMapQuery.ToEntityArray(Allocator.Temp))
        {
            for (int i = 0; i < bakedMapEntities.Length; i++)
            {
                Entity entity = bakedMapEntities[i];
                OperationMapBuildingComponent identity = em.GetComponentData<OperationMapBuildingComponent>(entity);
                if (identity.OperationMapId.ToString() != sourceOperationMapId) continue;
                if (!bakedMapEntitiesByPlacementIndex.TryGetValue(identity.PlacementIndex, out List<Entity> placementEntities))
                    bakedMapEntitiesByPlacementIndex.Add(identity.PlacementIndex, placementEntities = new List<Entity>());
                placementEntities.Add(entity);
            }
        }
        var bakedMapGeometryRows = new JArray();
        int bakedMapUniqueRows = 0, bakedMapExactOriginAndFootprintMatches = 0;
        int bakedMapMismatches = 0, bakedMapMissingRows = 0, bakedMapDuplicateIndices = 0;
        for (int i = 0; i < ((JArray)mapPlacementCapture["placement_rows"]).Count; i++)
        {
            JObject capturedRow = (JObject)mapPlacementCapture["placement_rows"][i];
            JObject resolvedRow = (JObject)mapPlacementOriginReplay["origins"][i];
            int placementIndex = (int)capturedRow["row_index"];
            var auditRow = new JObject
            {
                ["placement_index"] = placementIndex,
                ["prefab_key"] = (string)capturedRow["prefab"]["key"],
                ["runtime_owner_origin"] = resolvedRow["origin"].DeepClone(),
                ["runtime_owner_footprint"] = resolvedRow["footprint"].DeepClone()
            };
            if (!bakedMapEntitiesByPlacementIndex.TryGetValue(placementIndex, out List<Entity> matches) || matches.Count == 0)
            {
                bakedMapMissingRows++;
                auditRow["baked_owner_match_count"] = 0;
                auditRow["geometry_matches"] = false;
                auditRow["classification"] = "baked-owner-row-missing";
                bakedMapGeometryRows.Add(auditRow);
                continue;
            }
            if (matches.Count != 1)
            {
                bakedMapDuplicateIndices++;
                auditRow["baked_owner_match_count"] = matches.Count;
                auditRow["geometry_matches"] = false;
                auditRow["classification"] = "baked-owner-placement-index-ambiguous";
                bakedMapGeometryRows.Add(auditRow);
                continue;
            }

            bakedMapUniqueRows++;
            Entity bakedEntity = matches[0];
            OperationMapBuildingComponent identity = em.GetComponentData<OperationMapBuildingComponent>(bakedEntity);
            RuntimeBuildingCombatInfo combatInfo = em.GetComponentData<RuntimeBuildingCombatInfo>(bakedEntity);
            int2 runtimeOrigin = new int2((int)resolvedRow["origin"][0], (int)resolvedRow["origin"][1]);
            int2 runtimeFootprint = new int2((int)resolvedRow["footprint"][0], (int)resolvedRow["footprint"][1]);
            bool sameOrigin = combatInfo.OriginCell.x == runtimeOrigin.x && combatInfo.OriginCell.y == runtimeOrigin.y;
            bool sameFootprint = combatInfo.FootprintCells.x == runtimeFootprint.x && combatInfo.FootprintCells.y == runtimeFootprint.y;
            bool sameGeometry = sameOrigin && sameFootprint;
            if (sameGeometry) bakedMapExactOriginAndFootprintMatches++;
            else bakedMapMismatches++;
            auditRow["baked_entity"] = bakedEntity.Index + ":" + bakedEntity.Version;
            auditRow["baked_source_global_object_id"] = identity.SourceGlobalObjectId.ToString();
            auditRow["baked_runtime_building_id"] = combatInfo.RuntimeBuildingId;
            auditRow["baked_owner_origin"] = new JArray(combatInfo.OriginCell.x, combatInfo.OriginCell.y);
            auditRow["baked_owner_footprint"] = new JArray(combatInfo.FootprintCells.x, combatInfo.FootprintCells.y);
            auditRow["origin_matches"] = sameOrigin;
            auditRow["footprint_matches"] = sameFootprint;
            auditRow["geometry_matches"] = sameGeometry;
            auditRow["classification"] = sameGeometry ? "same-origin-and-footprint" : "cross-owner-geometry-difference";
            bakedMapGeometryRows.Add(auditRow);
        }
        JObject bakedMapGeometryAudit = new JObject
        {
            ["logical_operation_map_id"] = (string)mapPlacementCapture["operation_map_id"],
            ["baked_source_operation_map_id"] = sourceOperationMapId,
            ["captured_config_row_count"] = ((JArray)mapPlacementCapture["placement_rows"]).Count,
            ["baked_owner_unique_row_count"] = bakedMapUniqueRows,
            ["same_origin_and_footprint_count"] = bakedMapExactOriginAndFootprintMatches,
            ["cross_owner_geometry_difference_count"] = bakedMapMismatches,
            ["baked_owner_missing_row_count"] = bakedMapMissingRows,
            ["baked_owner_duplicate_index_count"] = bakedMapDuplicateIndices,
            ["runtime_owner"] = "MapBuildingPlacementOriginKernel from captured config and source runtime definition",
            ["baked_owner"] = "OperationMapBuildingAuthoring.Baker RuntimeBuildingCombatInfo in retained ECS",
            ["owner_equivalence_proven"] = false,
            ["rows"] = bakedMapGeometryRows
        };

        JObject rotatedCandidateRow = (JObject)mapPlacementCapture["placement_rows"][2];
        JObject rotatedCandidateOriginRow = (JObject)mapPlacementOriginReplay["origins"][2];
        int2 rotatedCandidateOrigin = new int2((int)rotatedCandidateOriginRow["origin"][0], (int)rotatedCandidateOriginRow["origin"][1]);
        int2 rotatedCandidateFootprint = new int2((int)rotatedCandidateOriginRow["footprint"][0], (int)rotatedCandidateOriginRow["footprint"][1]);
        DynamicBuffer<GridWalkable> rotatedCandidateWalkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBuffer<GridRoad> rotatedCandidateRoads = em.GetBuffer<GridRoad>(gridEntity);
        DynamicBlockerComponent rotatedCandidateBlockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        int rotatedCandidateNonWalkableCells = 0, rotatedCandidateGridBlockedCells = 0, rotatedCandidateRoadCells = 0;
        for (int y = rotatedCandidateOrigin.y; y < rotatedCandidateOrigin.y + rotatedCandidateFootprint.y; y++)
        for (int x = rotatedCandidateOrigin.x; x < rotatedCandidateOrigin.x + rotatedCandidateFootprint.x; x++)
        {
            int cellIndex = GridUtils.CellToIndex(new int2(x, y), grid.Width);
            if (rotatedCandidateWalkable[cellIndex].Value == 0) rotatedCandidateNonWalkableCells++;
            if (rotatedCandidateBlockers.Blocked.IsCreated && rotatedCandidateBlockers.Blocked.IsSet(cellIndex)) rotatedCandidateGridBlockedCells++;
            if (rotatedCandidateRoads[cellIndex].Value != 0) rotatedCandidateRoadCells++;
        }
        IReadOnlyList<int> rotatedCandidateRuntimeBlockerIds = runtimeBlockerOwnership.FindOverlappingIds(
            rotatedCandidateOrigin, rotatedCandidateFootprint);
        var rotatedCandidateStaticBlockerEntities = new JArray();
        using (var candidateBlockerQuery = em.CreateEntityQuery(typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize)))
        using (var candidateBlockers = candidateBlockerQuery.ToEntityArray(Allocator.Temp))
        {
            for (int i = 0; i < candidateBlockers.Length; i++)
            {
                Entity blockerEntity = candidateBlockers[i];
                int2 blockerOrigin = em.GetComponentData<UnitGrid>(blockerEntity).Cell;
                int2 blockerSize = em.GetComponentData<GridBlockerSize>(blockerEntity).Size;
                bool overlaps = rotatedCandidateOrigin.x < blockerOrigin.x + blockerSize.x && blockerOrigin.x < rotatedCandidateOrigin.x + rotatedCandidateFootprint.x &&
                    rotatedCandidateOrigin.y < blockerOrigin.y + blockerSize.y && blockerOrigin.y < rotatedCandidateOrigin.y + rotatedCandidateFootprint.y;
                if (overlaps)
                {
                    var overlapEvidence = new JObject
                    {
                        ["entity"] = blockerEntity.Index + ":" + blockerEntity.Version,
                        ["origin"] = new JArray(blockerOrigin.x, blockerOrigin.y),
                        ["footprint"] = new JArray(blockerSize.x, blockerSize.y),
                        ["has_authored_map_identity"] = em.HasComponent<OperationMapBuildingComponent>(blockerEntity)
                    };
                    if (em.HasComponent<OperationMapBuildingComponent>(blockerEntity))
                    {
                        OperationMapBuildingComponent identity = em.GetComponentData<OperationMapBuildingComponent>(blockerEntity);
                        overlapEvidence["operation_map_id"] = identity.OperationMapId.ToString();
                        overlapEvidence["placement_index"] = identity.PlacementIndex;
                        overlapEvidence["source_global_object_id"] = identity.SourceGlobalObjectId.ToString();
                    }
                    if (em.HasComponent<RuntimeBuildingCombatInfo>(blockerEntity))
                    {
                        RuntimeBuildingCombatInfo combat = em.GetComponentData<RuntimeBuildingCombatInfo>(blockerEntity);
                        overlapEvidence["runtime_building_id"] = combat.RuntimeBuildingId;
                        overlapEvidence["combat_origin"] = new JArray(combat.OriginCell.x, combat.OriginCell.y);
                        overlapEvidence["combat_footprint"] = new JArray(combat.FootprintCells.x, combat.FootprintCells.y);
                    }
                    rotatedCandidateStaticBlockerEntities.Add(overlapEvidence);
                }
            }
        }
        bool rotatedCandidateSurfaceLevel = new BuildingSurfacePlacementUtilitySystemHelper().TryEvaluateFootprint(
            em, rotatedCandidateOrigin, rotatedCandidateFootprint, .5f, 45f, out _);
        JObject rotatedCandidatePreflight = new JObject
        {
            ["source_row_index"] = (int)rotatedCandidateRow["row_index"],
            ["prefab"] = rotatedCandidateRow["prefab"].DeepClone(),
            ["faction_id"] = (int)rotatedCandidateRow["faction_id"],
            ["rotate_vertical"] = (bool)rotatedCandidateRow["rotate_vertical"],
            ["origin"] = new JArray(rotatedCandidateOrigin.x, rotatedCandidateOrigin.y),
            ["footprint"] = new JArray(rotatedCandidateFootprint.x, rotatedCandidateFootprint.y),
            ["shared_original_surface_level_predicate"] = rotatedCandidateSurfaceLevel,
            ["non_walkable_grid_cells"] = rotatedCandidateNonWalkableCells,
            ["dynamic_grid_blocked_cells"] = rotatedCandidateGridBlockedCells,
            ["road_cells"] = rotatedCandidateRoadCells,
            ["runtime_blocker_owner_overlap_ids"] = new JArray(rotatedCandidateRuntimeBlockerIds),
            ["runtime_blocker_owner_registry_scope"] = "shared kernel reconstructed by this diagnostic harness; not the production helper instance",
            ["static_ecs_blocker_overlaps"] = rotatedCandidateStaticBlockerEntities,
            ["registration_attempted"] = false,
            ["placement_owner_complete"] = false,
            ["mission_readiness_proven"] = false
        };

        RuntimeBuildingEntity authoredMapBuilding = null;
        var registeredSourceMapActors = new Dictionary<Entity, Tuple<RuntimeBuildingEntity, int, string>>();
        JObject authoredMapActorEvidence = null;
        JObject authoredMapRotatedActorEvidence = null;
        JObject authoredMapThirdBarrackEvidence = null;
        JObject authoredMapFourthBarrackEvidence = null;
        JObject authoredMapFifthFuelBladderEvidence = null;
        JObject authoredMapSixthFuelBladderEvidence = null;
        JObject authoredMapEnemyHallEvidence = null;
        JObject authoredMapSecondEnemyHallEvidence = null;
        JObject authoredMapHouseEvidence = null;
        var sourceDrivenMapRows = new JArray();
        var unsupportedSequentialRows = new JArray();
        JObject authoredMapWallEvidence = null;
        var mapPlacementCallbackRows = new List<int>();
        var mapPlacementCallbackSuccessRows = new List<int>();
        var mapPlacementPriorityPassRows = new List<int>();
        bool RegisterCapturedMapRow(int scheduledIndex)
        {
            mapPlacementCallbackRows.Add(scheduledIndex);
            if (scheduledIndex == 2)
            {
                authoredMapRotatedActorEvidence = RegisterCapturedRotatedBarrack(2, 868, 386,
                    new[] { 2, 3, 424, 342, 337, 338, 339, 340, 341, 288, 289, 298 });
                if (authoredMapRotatedActorEvidence == null)
                    return false;
                mapPlacementCallbackSuccessRows.Add(scheduledIndex);
                return true;
            }
            if (scheduledIndex == 3)
            {
                authoredMapThirdBarrackEvidence = RegisterCapturedRotatedBarrack(3, 868, 377, new[] { 2, 3 });
                if (authoredMapThirdBarrackEvidence == null)
                    return false;
                mapPlacementCallbackSuccessRows.Add(scheduledIndex);
                return true;
            }
            if (scheduledIndex == 4)
            {
                authoredMapFourthBarrackEvidence = RegisterCapturedRotatedBarrack(4, 909, 345,
                    new[] { 4, 227, 367, 368, 369, 382, 383, 384 });
                if (authoredMapFourthBarrackEvidence == null)
                    return false;
                mapPlacementCallbackSuccessRows.Add(scheduledIndex);
                return true;
            }
            if (scheduledIndex == 5)
            {
                authoredMapFifthFuelBladderEvidence = RegisterCapturedMapBuilding(5, "Building_Fuel_Bladder",
                    "Map/Buildings/Building_Fuel_Bladder/SM_Prop_Fuel_Bladder_01", 14, 10, 825, 368,
                    true, 1, new[] { 0, 1060439283, 0, 1060439283 }, new Vector3(0f, 90f, 0f), new[] { 5, 6, 223 });
                if (authoredMapFifthFuelBladderEvidence == null)
                    return false;
                mapPlacementCallbackSuccessRows.Add(scheduledIndex);
                return true;
            }
            if (scheduledIndex == 6)
            {
                authoredMapSixthFuelBladderEvidence = RegisterCapturedMapBuilding(6, "Building_Fuel_Bladder",
                    "Map/Buildings/Building_Fuel_Bladder/SM_Prop_Fuel_Bladder_01 (1)", 14, 10, 825, 357,
                    false, 1, new[] { 0, 901775359, 0, 1065353216 },
                    new Vector3(0f, 0.000163924531f, 0f), new[] { 6, 12, 223 });
                if (authoredMapSixthFuelBladderEvidence == null)
                    return false;
                mapPlacementCallbackSuccessRows.Add(scheduledIndex);
                return true;
            }
            if (scheduledIndex == 20)
            {
                authoredMapEnemyHallEvidence = RegisterCapturedMapBuilding(20, "Building_Hall",
                    "Map/Buildings/Building_Hall/SM_Bld_Hall_01", 24, 37, 1020, 528,
                    true, 0, new[] { 0, 1060439289, 0, 1060439277 }, new Vector3(0f, 90.00006f, 0f), new[] { 20 });
                if (authoredMapEnemyHallEvidence == null)
                    return false;
                mapPlacementCallbackSuccessRows.Add(scheduledIndex);
                return true;
            }
            if (scheduledIndex == 21)
            {
                authoredMapSecondEnemyHallEvidence = RegisterCapturedMapBuilding(21, "Building_Hall",
                    "Map/Buildings/Building_Hall/SM_Bld_Hall_01", 24, 37, 545, 379,
                    true, 0, new[] { 0, 1060439289, 0, 1060439277 }, new Vector3(0f, 90.00006f, 0f), Array.Empty<int>());
                if (authoredMapSecondEnemyHallEvidence == null)
                    return false;
                mapPlacementCallbackSuccessRows.Add(scheduledIndex);
                return true;
            }
            if (scheduledIndex != 1)
            {
                JObject rowEvidence = RegisterCapturedSourceMapRow(scheduledIndex);
                if (rowEvidence == null)
                {
                    JObject row = (JObject)mapPlacementCapture["placement_rows"][scheduledIndex];
                    RuntimeDefinition definition = mapDefinitions.Definitions[(int)row["prefab_index"]];
                    unsupportedSequentialRows.Add(new JObject { ["row_index"] = scheduledIndex,
                        ["category"] = (string)row["category"], ["source_path"] = (string)row["source_path"],
                        ["is_wall"] = definition.IsWall, ["has_runway"] = definition.HasRunway,
                        ["attack_impact_present"] = definition.AttackImpactPrefab != null,
                        ["muzzle_flash_present"] = definition.MuzzleFlashPrefab != null,
                        ["source_metadata_complete"] = definition.SourceMetadataComplete,
                        ["reason"] = "Source definition is outside the qualified shared map-row registration owner" });
                    return false;
                }
                if (scheduledIndex == 25)
                    authoredMapHouseEvidence = rowEvidence;
                if (scheduledIndex == 244)
                    authoredMapWallEvidence = rowEvidence;
                sourceDrivenMapRows.Add(rowEvidence);
                mapPlacementCallbackSuccessRows.Add(scheduledIndex);
                return true;
            }

        // Materialize the one currently supported sequential source row through the existing
        // shared building registration and actor owners. The callback still runs under the real
        // 432-row priority pass and first 32 sequential attempts; all other source rows remain
        // explicitly unhandled, and the separately selected wall row does not complete the map.
        JObject authoredMapRow = (JObject)mapPlacementCapture["placement_rows"][scheduledIndex];
        int authoredDefinitionIndex = (int)authoredMapRow["prefab_index"];
        RuntimeDefinition authoredDefinition = mapDefinitions.Definitions[authoredDefinitionIndex];
        JObject authoredEuler = (JObject)authoredMapRow["world_euler_angles"];
        JArray authoredRotationBits = authoredMapRow["world_rotation_f32bits"] as JArray;
        Need(authoredRotationBits != null && authoredRotationBits.Count == 4,
            "Original UnityEngine.Quaternion.Euler result is required for this authored row");
        var authoredSourceWorldRotation = new quaternion(
            BitConverter.ToSingle(BitConverter.GetBytes((int)authoredRotationBits[0]), 0),
            BitConverter.ToSingle(BitConverter.GetBytes((int)authoredRotationBits[1]), 0),
            BitConverter.ToSingle(BitConverter.GetBytes((int)authoredRotationBits[2]), 0),
            BitConverter.ToSingle(BitConverter.GetBytes((int)authoredRotationBits[3]), 0));
        JObject authoredScale = (JObject)authoredMapRow["world_scale"];
        JObject authoredPosition = (JObject)authoredMapRow["world_position"];
        JObject authoredOriginRow = (JObject)mapPlacementOriginReplay["origins"][scheduledIndex];
        int2 authoredOrigin = new int2((int)authoredOriginRow["origin"][0], (int)authoredOriginRow["origin"][1]);
        Vector3 authoredWorldEuler = new Vector3((float)authoredEuler["x"], (float)authoredEuler["y"], (float)authoredEuler["z"]);
        int preservedAuthoredOverlaps = 0, blockerRemovalCallbackCount = 0, redirectOwnerInvocationCount = 0;
        Entity preservedAuthoredBlocker = Entity.Null;
        JObject preservedAuthoredOverlapEvidence = null;
        quaternion capturedAuthoredRotation = default;
        int observedSourceMapOverlapCount = 0;
        bool expectsLegacyS002AuthoredOverlap = (string)mapPlacementCapture["operation_map_id"] == "opmap.skirmish.desert_base_01";
        void ReadPinnedAuthoredMapBlocker(Vector2Int origin, Vector2Int footprint,
            out Entity matchEntity, out int2 matchOrigin, out int2 matchSize,
            out OperationMapBuildingComponent matchIdentity, out quaternion matchRotation, out int2 selectionCenter)
        {
            matchEntity = Entity.Null;
            matchOrigin = default;
            matchSize = default;
            matchIdentity = default;
            matchRotation = default;
            selectionCenter = default;
            int overlapCount = 0;
            using var blockers = em.CreateEntityQuery(typeof(StaticGridBlocker),
                typeof(UnitGrid), typeof(GridBlockerSize));
            using var entities = blockers.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity blockerEntity = entities[i];
                int2 blockerOrigin;
                int2 blockerSize;
                bool registeredSourceMapActor = registeredSourceMapActors.TryGetValue(blockerEntity, out var sourceActor);
                bool authoredBuilding = em.HasComponent<OperationMapBuildingComponent>(blockerEntity) &&
                    em.HasComponent<RuntimeBuildingCombatInfo>(blockerEntity);
                if (authoredBuilding)
                {
                    RuntimeBuildingCombatInfo combat = em.GetComponentData<RuntimeBuildingCombatInfo>(blockerEntity);
                    blockerOrigin = combat.OriginCell;
                    blockerSize = combat.FootprintCells;
                }
                else if (registeredSourceMapActor)
                {
                    Need(sourceActor.Item2 >= 0 && sourceActor.Item2 < 432 &&
                         (string)mapPlacementCapture["placement_rows"][sourceActor.Item2]["source_path"] == sourceActor.Item3 &&
                         sourceActor.Item1.BlockerEntity == blockerEntity && sourceActor.Item1.CombatEntity != Entity.Null &&
                         em.Exists(sourceActor.Item1.CombatEntity) && em.HasComponent<RuntimeBuildingCombatInfo>(sourceActor.Item1.CombatEntity),
                        "Previously registered map blocker must retain its exact captured source-row owner");
                    RuntimeBuildingCombatInfo combat = em.GetComponentData<RuntimeBuildingCombatInfo>(sourceActor.Item1.CombatEntity);
                    blockerOrigin = combat.OriginCell;
                    blockerSize = combat.FootprintCells;
                    Need(combat.RuntimeBuildingId == sourceActor.Item1.Id && sourceActor.Item1.Definition != null &&
                         combat.FootprintCells.Equals(new int2(sourceActor.Item1.Definition.FootprintCells.x, sourceActor.Item1.Definition.FootprintCells.y)),
                        "Previously registered map blocker must match its canonical runtime owner footprint");
                }
                else
                {
                    blockerOrigin = em.GetComponentData<UnitGrid>(blockerEntity).Cell;
                    blockerSize = em.GetComponentData<GridBlockerSize>(blockerEntity).Size;
                    // Runtime blockers registered through the shared owner are
                    // removed by that owner below; they are not baked map actors.
                    if (runtimeBlockerOwnership.TryGetOwnedEntity(blockerEntity, out RuntimeGridBlockerOwnershipRecord owned))
                    {
                        Need(owned.OriginCell.Equals(blockerOrigin) && owned.SizeCells.Equals(blockerSize),
                            "Runtime blocker owner record must match the live ECS footprint");
                        continue;
                    }
                }
                bool overlaps = origin.x < blockerOrigin.x + blockerSize.x && blockerOrigin.x < origin.x + footprint.x &&
                    origin.y < blockerOrigin.y + blockerSize.y && blockerOrigin.y < origin.y + footprint.y;
                if (overlaps)
                {
                    if (!authoredBuilding && !registeredSourceMapActor)
                        throw new InvalidOperationException("Unexpected blocker overlap outside the pinned authored map actor; runtime blocker-owner membership is not available in this bounded harness: " +
                            blockerEntity.Index + ":" + blockerEntity.Version + "@" + blockerOrigin.x + "," + blockerOrigin.y + "+" + blockerSize.x + "x" + blockerSize.y);

                    OperationMapBuildingComponent identity = authoredBuilding ? em.GetComponentData<OperationMapBuildingComponent>(blockerEntity) : default;
                    if (authoredBuilding)
                    {
                        const string expectedSourceGlobalObjectId = "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1506274068195782-2359600729620732797";
                        Need(identity.OperationMapId.ToString() == "opmap.skirmish.desert_base_01" && identity.PlacementIndex == 1 &&
                             identity.SourceGlobalObjectId.ToString() == expectedSourceGlobalObjectId &&
                             blockerOrigin.x == 944 && blockerOrigin.y == 346 && blockerSize.x == 32 && blockerSize.y == 37,
                             "Overlapping authored blocker must match the pinned source identity and authoritative origin/footprint");
                    }
                    else
                    {
                        JObject sourceRow = (JObject)mapPlacementCapture["placement_rows"][sourceActor.Item2];
                        Need(sourceActor.Item1.Definition.Prefab.name == (string)sourceRow["category"] &&
                             sourceActor.Item1.OriginCell.x == blockerOrigin.x && sourceActor.Item1.OriginCell.y == blockerOrigin.y,
                            "Overlapping source-map actor must match its exact captured row and original shared runtime owner");
                    }
                    overlapCount++;
                    matchEntity = blockerEntity;
                    matchOrigin = blockerOrigin;
                    matchSize = blockerSize;
                    matchIdentity = identity;
                    Entity transformEntity = registeredSourceMapActor ? sourceActor.Item1.CombatEntity : blockerEntity;
                    matchRotation = em.GetComponentData<LocalTransform>(transformEntity).Rotation;
                    selectionCenter = em.GetComponentData<UnitGrid>(blockerEntity).Cell;
                }
            }
            observedSourceMapOverlapCount = overlapCount;
            Need(!expectsLegacyS002AuthoredOverlap || overlapCount == 1,
                "S002 must retain its exact source-authored overlap; every other overlap must match a tracked source row or runtime blocker owner");
            if (overlapCount == 0)
                return;
            if (!expectsLegacyS002AuthoredOverlap && registeredSourceMapActors.ContainsKey(matchEntity))
                return; // The exact source row's transform was already checked by RegisterCapturedMapBuilding.
            int[] capturedRotationBits = new[]
            {
                BitConverter.ToInt32(BitConverter.GetBytes(matchRotation.value.x), 0),
                BitConverter.ToInt32(BitConverter.GetBytes(matchRotation.value.y), 0),
                BitConverter.ToInt32(BitConverter.GetBytes(matchRotation.value.z), 0),
                BitConverter.ToInt32(BitConverter.GetBytes(matchRotation.value.w), 0)
            };
            Need(capturedRotationBits.SequenceEqual(new[] { unchecked((int)0x80000000u), 0x33fffffe, unchecked((int)0x80000000u), 0x3f800000 }),
                "Source actor quaternion must match exact native captured bits, including signed-zero anchors");
        }
        void ObserveOriginalBlockerRemovalBoundary(Vector2Int origin, Vector2Int footprint)
        {
            blockerRemovalCallbackCount++;
            ReadPinnedAuthoredMapBlocker(origin, footprint, out var boundaryEntity, out var boundaryOrigin,
                out var boundarySize, out var boundaryIdentity, out var boundaryRotation, out var selectionCenter);
            Need(boundaryEntity == preservedAuthoredBlocker &&
                 (boundaryEntity == Entity.Null || boundaryRotation.Equals(capturedAuthoredRotation)),
                "Original blocker-removal boundary must observe the exact prevalidated source actor, if one overlaps");
            preservedAuthoredOverlaps = observedSourceMapOverlapCount;
            preservedAuthoredOverlapEvidence = new JObject
            {
                ["observed_source_map_overlap_count"] = observedSourceMapOverlapCount,
                ["source_authored_overlap"] = boundaryEntity != Entity.Null,
                ["entity"] = boundaryEntity == Entity.Null ? JValue.CreateNull() : new JValue(boundaryEntity.Index + ":" + boundaryEntity.Version),
                ["operation_map_id"] = boundaryEntity == Entity.Null ? JValue.CreateNull() : new JValue(boundaryIdentity.OperationMapId.ToString()),
                ["placement_index"] = boundaryEntity == Entity.Null ? JValue.CreateNull() : new JValue(boundaryIdentity.PlacementIndex),
                ["source_global_object_id"] = boundaryEntity == Entity.Null ? JValue.CreateNull() : new JValue(boundaryIdentity.SourceGlobalObjectId.ToString()),
                ["authoritative_origin"] = boundaryEntity == Entity.Null ? JValue.CreateNull() : new JArray(boundaryOrigin.x, boundaryOrigin.y),
                ["authoritative_footprint"] = boundaryEntity == Entity.Null ? JValue.CreateNull() : new JArray(boundarySize.x, boundarySize.y),
                ["unit_grid_selection_center"] = boundaryEntity == Entity.Null ? JValue.CreateNull() : new JArray(selectionCenter.x, selectionCenter.y),
                ["preserved_by_runtime_blocker_removal_owner"] = true
            };
        }
        void RemoveRuntimeBlockers(Vector2Int origin, Vector2Int footprint)
        {
            IReadOnlyList<int> overlapping = runtimeBlockerOwnership.FindOverlappingIds(
                new int2(origin.x, origin.y), new int2(footprint.x, footprint.y));
            bool removed = false;
            for (int i = 0; i < overlapping.Count; i++)
            {
                if (!runtimeBlockerOwnership.TryRemove(overlapping[i], true, out RuntimeGridBlockerOwnershipRecord blocker))
                    continue;
                if (blocker.Entity != Entity.Null && em.Exists(blocker.Entity))
                    em.DestroyEntity(blocker.Entity);
                removed = true;
            }
            if (removed)
                autonomousWorld.UpdateS073LegacyDiagnosticBlockerOwners();
        }
        void RemoveRuntimeBlockersAtOriginalBoundary(Vector2Int origin, Vector2Int footprint)
        {
            ObserveOriginalBlockerRemovalBoundary(origin, footprint);
            RemoveRuntimeBlockers(origin, footprint);
        }
        JObject RegisterCapturedRotatedBarrack(int sourceRowIndex, int expectedOriginX, int expectedOriginY,
            int[] expectedPlacementIndices) => RegisterCapturedMapBuilding(sourceRowIndex, "Building_Barrack",
                "Map/Buildings/Building_Barrack/SM_Bld_Barracks_01" + (sourceRowIndex == 2 ? "" : " (" + (sourceRowIndex - 2) + ")"),
                28, 15, expectedOriginX, expectedOriginY, true,
                1, new[] { 0, 1060439283, 0, 1060439283 }, new Vector3(0f, 90f, 0f), expectedPlacementIndices);

        JObject RegisterCapturedMapBuilding(int sourceRowIndex, string expectedBuildingKey, string expectedSourcePath,
            int sourceFootprintX, int sourceFootprintY, int expectedOriginX, int expectedOriginY, bool expectedRotateVertical,
            int expectedFactionId,
            int[] expectedRotationBits, Vector3 expectedEuler,
            int[] expectedPlacementIndices, bool allowSourceAuthoredMapWall = false,
            SourceMapRuntimeFacts sourceMapRuntimeFacts = null)
        {
            JObject row = (JObject)mapPlacementCapture["placement_rows"][sourceRowIndex];
            Need((int?)row["row_index"] == sourceRowIndex && (string)row["category"] == expectedBuildingKey &&
                 (string)row["prefab"]?["key"] == expectedBuildingKey && (int?)row["faction_id"] == expectedFactionId &&
                 (bool?)row["rotate_vertical"] == expectedRotateVertical &&
                 (string)row["source_path"] == expectedSourcePath,
                "Exact captured S073 map " + expectedBuildingKey + " binding required for source row " + sourceRowIndex);
            RuntimeDefinition sourceDefinition = mapDefinitions.Definitions[(int)row["prefab_index"]];
            bool isRunway = sourceDefinition.HasRunway;
            bool isGate = BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(sourceDefinition);
            bool runwayFactsMatch = sourceMapRuntimeFacts != null && sourceMapRuntimeFacts.SourceRowIndex == sourceRowIndex &&
                sourceMapRuntimeFacts.SourceBuildingKey == expectedBuildingKey && sourceMapRuntimeFacts.HasRunway && !sourceMapRuntimeFacts.HasDoor;
            bool gateFactsMatch = sourceMapRuntimeFacts != null && sourceMapRuntimeFacts.SourceRowIndex == sourceRowIndex &&
                sourceMapRuntimeFacts.SourceBuildingKey == expectedBuildingKey && sourceMapRuntimeFacts.HasDoor && !sourceMapRuntimeFacts.HasRunway;
            Need(sourceDefinition.SourceMetadataComplete && sourceDefinition.Prefab.name == expectedBuildingKey &&
                 sourceDefinition.IsWall == allowSourceAuthoredMapWall && (!isRunway || runwayFactsMatch) && (!isGate || gateFactsMatch) &&
                 ((isRunway && runwayFactsMatch) || (isGate && gateFactsMatch) || (!isRunway && !isGate && sourceMapRuntimeFacts == null)) &&
                 sourceDefinition.FootprintCells.x == sourceFootprintX && sourceDefinition.FootprintCells.y == sourceFootprintY,
                "Complete source definition with the explicitly captured ordinary, runway, gate, or wall owner and footprint required for " + expectedBuildingKey);
            int2 footprint = MapBuildingPlacementOriginKernel.ResolvePlacementFootprint(
                sourceDefinition.FootprintCells.x, sourceDefinition.FootprintCells.y,
                isLinearWall: allowSourceAuthoredMapWall,
                localBoundsSizeX: allowSourceAuthoredMapWall ? sourceDefinition.LocalBounds.size.x : 0f,
                localBoundsSizeZ: allowSourceAuthoredMapWall ? sourceDefinition.LocalBounds.size.z : 0f,
                rotateVertical: expectedRotateVertical);
            var rotatedDefinition = (RuntimeDefinition)typeof(RuntimeDefinition)
                .GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(sourceDefinition, null);
            rotatedDefinition.FootprintCells = new Vector2Int(footprint.x, footprint.y);
            JObject center = (JObject)row["world_center"];
            int2 centerCell = GridUtils.WorldToCell(grid,
                new float3((float)center["x"], (float)center["y"], (float)center["z"]));
            int2 origin = MapBuildingPlacementOriginKernel.ResolveOriginCell(centerCell,
                new Vector2Int(footprint.x, footprint.y), grid, useGeometricFootprintCenter: false);
            JObject replayOrigin = (JObject)mapPlacementOriginReplay["origins"][sourceRowIndex];
            Need(origin.x == expectedOriginX && origin.y == expectedOriginY &&
                 origin.x == (int)replayOrigin["origin"][0] && origin.y == (int)replayOrigin["origin"][1] &&
                 (int)replayOrigin["footprint"][0] == footprint.x && (int)replayOrigin["footprint"][1] == footprint.y,
                "Source origin and original wall/ordinary footprint must match the current captured-input shared replay");
            if (!allowSourceAuthoredMapWall)
            {
                int expectedFootprintX = expectedRotateVertical ? sourceFootprintY : sourceFootprintX;
                int expectedFootprintY = expectedRotateVertical ? sourceFootprintX : sourceFootprintY;
                Need(footprint.x == expectedFootprintX && footprint.y == expectedFootprintY,
                    "Original shared placement kernel must resolve the captured ordinary footprint for " + expectedBuildingKey);
            }
            JArray rotationBits = row["world_rotation_f32bits"] as JArray;
            Need(rotationBits != null && rotationBits.Count == 4 &&
                 rotationBits.Select(value => (int)value).SequenceEqual(expectedRotationBits),
                "Captured source quaternion bits, including signed-zero anchors, required");
            var mathRotation = new quaternion(
                BitConverter.ToSingle(BitConverter.GetBytes((int)rotationBits[0]), 0),
                BitConverter.ToSingle(BitConverter.GetBytes((int)rotationBits[1]), 0),
                BitConverter.ToSingle(BitConverter.GetBytes((int)rotationBits[2]), 0),
                BitConverter.ToSingle(BitConverter.GetBytes((int)rotationBits[3]), 0));
            var sourceRotation = new ActualCore::UnityEngine.Quaternion(
                mathRotation.value.x, mathRotation.value.y, mathRotation.value.z, mathRotation.value.w);
            JObject euler = (JObject)row["world_euler_angles"];
            var worldEuler = new Vector3((float)euler["x"], (float)euler["y"], (float)euler["z"]);
            Need(worldEuler.Equals(expectedEuler), "Captured authored Euler rotation must remain exact for source row " + sourceRowIndex);

            var preexistingRuntimeBuildingOverlaps = new JArray();
            var preservedRuntimeBuildings = new List<RuntimeBuildingEntity>();
            foreach (RuntimeBuildingEntity existing in autonomousWorld.Buildings.Values)
            {
                if (existing?.Definition == null || !existing.Definition.SourceMetadataComplete)
                    continue;
                int2 existingOrigin = new int2(existing.OriginCell.x, existing.OriginCell.y);
                int2 existingFootprint = new int2(existing.Definition.FootprintCells.x, existing.Definition.FootprintCells.y);
                bool overlaps = origin.x < existingOrigin.x + existingFootprint.x && existingOrigin.x < origin.x + footprint.x &&
                    origin.y < existingOrigin.y + existingFootprint.y && existingOrigin.y < origin.y + footprint.y;
                if (!overlaps)
                    continue;
                int overlapWidth = Math.Min(origin.x + footprint.x, existingOrigin.x + existingFootprint.x) -
                    Math.Max(origin.x, existingOrigin.x);
                int overlapHeight = Math.Min(origin.y + footprint.y, existingOrigin.y + existingFootprint.y) -
                    Math.Max(origin.y, existingOrigin.y);
                preservedRuntimeBuildings.Add(existing);
                preexistingRuntimeBuildingOverlaps.Add(new JObject
                {
                    ["runtime_id"] = existing.Id,
                    ["definition"] = existing.Definition.Prefab.name,
                    ["origin"] = new JArray(existingOrigin.x, existingOrigin.y),
                    ["footprint"] = new JArray(existingFootprint.x, existingFootprint.y),
                    ["overlap_cells"] = new JArray(overlapWidth, overlapHeight),
                    ["combat_entity"] = existing.CombatEntity.Index + ":" + existing.CombatEntity.Version
                });
            }
            if (sourceRowIndex == 3)
                Need(authoredMapRotatedActorEvidence != null &&
                    preexistingRuntimeBuildingOverlaps.Any(item => (int?)item["runtime_id"] == (int?)authoredMapRotatedActorEvidence["runtime_id"] &&
                    (string)item["definition"] == "Building_Barrack" && (int?)item["overlap_cells"]?[0] == 15 &&
                    (int?)item["overlap_cells"]?[1] == 19),
                    "Row 3 must expose its real 15x19 runtime overlap with the registered preceding source Barrack");

            // UnitGrid is a selection center, not an origin. Use combat origin/footprint and
            // preserve all exact source owners because the original registration owner removes
            // tracked runtime blockers, not these baked map actors.
            var preservedSourceBlockers = new List<Tuple<Entity, int, int2, int2>>();
            int observedSourceOverlapCount = 0;
            void ObserveAndRemoveRuntimeBlockers(Vector2Int at, Vector2Int size)
            {
                using var query = em.CreateEntityQuery(typeof(StaticGridBlocker), typeof(UnitGrid),
                    typeof(GridBlockerSize), typeof(OperationMapBuildingComponent), typeof(RuntimeBuildingCombatInfo));
                using var blockers = query.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in blockers)
                {
                    var selectionCenter = em.GetComponentData<UnitGrid>(entity).Cell;
                    var declaredBlockerSize = em.GetComponentData<GridBlockerSize>(entity).Size;
                    RuntimeBuildingCombatInfo combat = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                    var blockerOrigin = combat.OriginCell;
                    var blockerSize = combat.FootprintCells;
                    bool overlaps = at.x < blockerOrigin.x + blockerSize.x && blockerOrigin.x < at.x + size.x &&
                        at.y < blockerOrigin.y + blockerSize.y && blockerOrigin.y < at.y + size.y;
                    if (!overlaps) continue;
                    OperationMapBuildingComponent identity = em.GetComponentData<OperationMapBuildingComponent>(entity);
                    Need(identity.OperationMapId.ToString() == selected.SourceBinding.SourceOperationMapId &&
                         !string.IsNullOrEmpty(identity.SourceGlobalObjectId.ToString()) &&
                         identity.PlacementIndex >= 0 && identity.PlacementIndex < 432 &&
                         combat.FootprintCells.Equals(declaredBlockerSize) &&
                         combat.OriginCell.x >= 0 && combat.OriginCell.y >= 0 &&
                         GridUtils.InBounds(selectionCenter, grid.Width, grid.Height),
                        "Every overlapping map blocker must retain exact source identity, combat origin/footprint and a valid selection center");
                    preservedSourceBlockers.Add(Tuple.Create(entity, identity.PlacementIndex, blockerOrigin, blockerSize));
                }
                int[] observed = preservedSourceBlockers.Select(item => item.Item2).OrderBy(index => index).ToArray();
                Need(expectedPlacementIndices.All(observed.Contains) && observed.Distinct().Count() == observed.Length,
                    "Rotated source building must retain every independently identified live source-map overlap; expected=" +
                    string.Join(",", expectedPlacementIndices.OrderBy(index => index)) + " observed=" + string.Join(",", observed));
                observedSourceOverlapCount = observed.Length;
                RemoveRuntimeBlockers(at, size);
                foreach (var item in preservedSourceBlockers)
                {
                    Need(em.Exists(item.Item1) && em.HasComponent<StaticGridBlocker>(item.Item1) &&
                         em.GetComponentData<OperationMapBuildingComponent>(item.Item1).PlacementIndex == item.Item2 &&
                         em.GetComponentData<RuntimeBuildingCombatInfo>(item.Item1).OriginCell.Equals(item.Item3) &&
                         em.GetComponentData<RuntimeBuildingCombatInfo>(item.Item1).FootprintCells.Equals(item.Item4) &&
                         em.GetComponentData<GridBlockerSize>(item.Item1).Size.Equals(item.Item4),
                        "Original runtime blocker removal must preserve each distinct source-authored blocker actor");
                }
            }
            int redirectCalls = 0;
            using var redirectQuery = em.CreateEntityQuery(typeof(UnitMove), typeof(UnitGrid), typeof(LocalTransform));
            var redirectOwner = new BuildingPlacementRedirectCompositionSystemHelper();
            var redirectContext = new BuildingPlacementRedirectCompositionSystemHelper.Context(
                (out EntityManager manager) => { manager = em; return true; },
                (out Entity sourceGridEntity, out GridConfig sourceGridConfig, out DynamicBuffer<GridRoad> sourceRoads,
                    out DynamicBlockerComponent sourceBlockers) =>
                {
                    sourceGridEntity = gridEntity; sourceGridConfig = em.GetComponentData<GridConfig>(gridEntity);
                    sourceRoads = em.GetBuffer<GridRoad>(gridEntity); sourceBlockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity); return true;
                }, _ => { }, () => redirectQuery);
            Action<RectInt> redirectUnits = rect => { redirectCalls++; redirectOwner.RedirectUnitsAroundPlacedBuilding(redirectContext, rect); };
            var registration = new GeneratedBuildingRegistration(em, gridEntity, pipeline.CanonicalCollection, buildPlaneY, redirectUnits);
            RuntimeBuildingEntity building = registration.RegisterAtLegalOrigin(rotatedDefinition,
                new Vector2Int(origin.x, origin.y), birth: new SourceBuildingBirthContext(true),
                worldEulerAngles: worldEuler, preserveAuthoredTransform: true,
                removeOverlappingBlockers: true, removeBlockers: ObserveAndRemoveRuntimeBlockers,
                sourceWorldRotationOverride: sourceRotation,
                allowSourceAuthoredMapWall: allowSourceAuthoredMapWall,
                sourceMapRuntimeFacts: sourceMapRuntimeFacts);
            bool expectedPathingBlocker = new BuildingRuntimeEntityCompositionSystemHelper()
                .ShouldRuntimeBuildingBlockPathing(rotatedDefinition);
            Need(building != null && building.CombatEntity != Entity.Null &&
                 (building.BlockerEntity != Entity.Null) == expectedPathingBlocker &&
                 em.Exists(building.CombatEntity) && (!expectedPathingBlocker || em.Exists(building.BlockerEntity)) &&
                 building.OriginCell.Equals(new Vector2Int(origin.x, origin.y)) &&
                 building.Definition.Prefab.name == expectedBuildingKey && building.Definition.FootprintCells.Equals(new Vector2Int(footprint.x, footprint.y)),
                "Captured source row must register through the existing shared owner with the original pathing-blocker policy, identity, and rotated footprint");
            registration.SetOwner(building, (byte?)row["faction_id"], allowSourceAuthoredMapWall,
                allowSourceAuthoredMapRunway: sourceMapRuntimeFacts?.HasRunway == true);
            autonomousWorld.Buildings.Add(building.Id, building);
            if (building.BlockerEntity != Entity.Null)
                registeredSourceMapActors.Add(building.BlockerEntity, Tuple.Create(building, sourceRowIndex, (string)row["source_path"]));
            autonomousWorld.UpdateS073LegacyDiagnosticBlockerOwners();
            RuntimeBuildingCombatInfo combatInfo = em.GetComponentData<RuntimeBuildingCombatInfo>(building.CombatEntity);
            Faction faction = em.GetComponentData<Faction>(building.CombatEntity);
            LocalTransform transform = em.GetComponentData<LocalTransform>(building.CombatEntity);
            int[] actorRotationBits = new[] {
                BitConverter.ToInt32(BitConverter.GetBytes(transform.Rotation.value.x), 0),
                BitConverter.ToInt32(BitConverter.GetBytes(transform.Rotation.value.y), 0),
                BitConverter.ToInt32(BitConverter.GetBytes(transform.Rotation.value.z), 0),
                BitConverter.ToInt32(BitConverter.GetBytes(transform.Rotation.value.w), 0) };
            Vector3 expectedCenter = new GeneratedSourceBuildingCenter().GetFootprintCenter(
                new Vector2Int(origin.x, origin.y), rotatedDefinition.FootprintCells, grid, buildPlaneY);
            bool combatIdentityMatches = combatInfo.RuntimeBuildingId == building.Id && combatInfo.OriginCell.Equals(origin) &&
                combatInfo.FootprintCells.Equals(footprint) && faction.Id == (byte)row["faction_id"] &&
                (!allowSourceAuthoredMapWall || (combatInfo.IsWall == 1 && combatInfo.IsGate == 0));
            bool rotationMatches = actorRotationBits.SequenceEqual(rotationBits.Select(value => (int)value));
            bool centerMatches = transform.Position.Equals((float3)expectedCenter);
            bool blockerEvidenceMatches = preservedSourceBlockers.Count == observedSourceOverlapCount &&
                observedSourceOverlapCount >= expectedPlacementIndices.Length && preservedSourceBlockers.All(item => em.Exists(item.Item1));
            int expectedRedirectCalls = expectedPathingBlocker ? 1 : 0;
            bool priorRuntimeActorsPreserved = preservedRuntimeBuildings.All(existing =>
                autonomousWorld.Buildings.TryGetValue(existing.Id, out var retained) && ReferenceEquals(retained, existing) &&
                (existing.CombatEntity == Entity.Null || em.Exists(existing.CombatEntity)));
            Need(combatIdentityMatches && rotationMatches && centerMatches && redirectCalls == expectedRedirectCalls && blockerEvidenceMatches &&
                 priorRuntimeActorsPreserved,
                "Captured source row " + sourceRowIndex + " registration mismatch: combat=" + combatIdentityMatches +
                " rotation=" + rotationMatches + " center=" + centerMatches + " redirectCalls=" + redirectCalls +
                " expectedRedirectCalls=" + expectedRedirectCalls +
                " blockerEvidence=" + blockerEvidenceMatches + " sourceOverlap=" + observedSourceOverlapCount +
                " expectedSourceOverlapCountAtLeast=" + expectedPlacementIndices.Length +
                " priorRuntimeActorsPreserved=" + priorRuntimeActorsPreserved);
            buildings.Add(new JObject { ["runtime_id"] = building.Id, ["definition"] = building.Definition.Prefab.name,
                ["origin"] = new JArray(origin.x, origin.y), ["combat_entity"] = building.CombatEntity.Index + ":" + building.CombatEntity.Version,
                ["blocker_entity"] = building.BlockerEntity.Index + ":" + building.BlockerEntity.Version });
            return new JObject { ["status"] = "Passed", ["source_row_index"] = sourceRowIndex,
                ["source_prefab_identity"] = row["prefab"].DeepClone(), ["origin"] = new JArray(origin.x, origin.y),
                ["runtime_id"] = building.Id,
                ["footprint"] = new JArray(footprint.x, footprint.y), ["captured_euler"] = new JArray(worldEuler.x, worldEuler.y, worldEuler.z),
                ["captured_rotation_f32bits"] = rotationBits.DeepClone(), ["actor_rotation_matches_capture"] = true,
                ["faction_id"] = (int)row["faction_id"], ["redirect_owner_calls"] = redirectCalls,
                ["runtime_pathing_blocker_expected"] = expectedPathingBlocker,
                ["combat_is_wall"] = combatInfo.IsWall == 1,
                ["combat_is_gate"] = combatInfo.IsGate == 1,
                ["shared_registration_kernel_used"] = true, ["same_live_registry_as_initial_spawn"] = autonomousWorld.Buildings.ContainsKey(building.Id),
                ["live_source_blocker_overlap_count"] = observedSourceOverlapCount,
                ["preexisting_runtime_building_overlap_count"] = preexistingRuntimeBuildingOverlaps.Count,
                ["preexisting_runtime_building_overlaps"] = preexistingRuntimeBuildingOverlaps,
                ["overlapping_runtime_buildings_preserved"] = preservedRuntimeBuildings.All(existing =>
                    autonomousWorld.Buildings.TryGetValue(existing.Id, out var retained) && ReferenceEquals(retained, existing)),
                ["original_overlap_removal_owner_scope"] = "RuntimeGridBlockerPresentationSystemHelper removes tracked runtime grid blockers; existing runtime buildings are retained",
                ["preserved_source_authored_blockers"] = new JArray(preservedSourceBlockers.Select(item => new JObject {
                    ["entity"] = item.Item1.Index + ":" + item.Item1.Version, ["placement_index"] = item.Item2,
                    ["origin"] = new JArray(item.Item3.x, item.Item3.y), ["footprint"] = new JArray(item.Item4.x, item.Item4.y) })),
                ["native_visual_readiness"] = false, ["mission_readiness_proven"] = false };
        }
        JObject RegisterCapturedSourceMapRow(int sourceRowIndex)
        {
            JObject row = (JObject)mapPlacementCapture["placement_rows"][sourceRowIndex];
            int definitionIndex = (int)row["prefab_index"];
            Need(definitionIndex >= 0 && definitionIndex < mapDefinitions.Definitions.Count,
                "Captured source map definition index must resolve for row " + sourceRowIndex);
            RuntimeDefinition definition = mapDefinitions.Definitions[definitionIndex];
            bool isGate = BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(definition);
            bool isCapturedRunway = sourceRowIndex == 0 && definition.HasRunway;
            bool isCapturedGate = sourceRowIndex >= 108 && sourceRowIndex <= 110 && isGate;
            if (!definition.SourceMetadataComplete || definition.AttackImpactPrefab != null || definition.MuzzleFlashPrefab != null ||
                (definition.HasRunway && !isCapturedRunway) || (isGate && !isCapturedGate))
                return null;

            SourceMapRuntimeFacts runtimeFacts = null;
            if (isCapturedRunway)
            {
                Need((string)row["category"] == "Building_Airport" &&
                     (string)row["prefab"]["path"] == "Assets/Game/Prefabs/Buildings/Building_Airport.prefab" &&
                     (int?)row["faction_id"] == 1,
                    "Airport runtime facts must remain bound to S073 source row 0");
                JObject data = (JObject)specialBuildingRuntimeReference["airport_runway_runtime_world_data"];
                JObject center = (JObject)data["center"], extents = (JObject)data["half_extents"];
                JArray rotation = (JArray)data["rotation_xyzw"];
                Need((string)data["owner"] == "BuildingRunwaySystem.TryResolveRuntimeRunwayWorldData" && rotation.Count == 4,
                    "Original runway world geometry owner result required");
                runtimeFacts = new SourceMapRuntimeFacts { SourceRowIndex = sourceRowIndex, SourceBuildingKey = "Building_Airport", HasRunway = true,
                    RunwayCenter = new[] { (float)center["x"], (float)center["y"], (float)center["z"] },
                    RunwayRotationXyzw = rotation.Select(value => (float)value).ToArray(),
                    RunwayHalfExtents = new[] { (float)extents["x"], (float)extents["y"], (float)extents["z"] } };
            }
            else if (isCapturedGate)
            {
                JObject gate = ((JArray)specialBuildingRuntimeReference["road_gates"]).Single(item => (int)item["row_index"] == sourceRowIndex) as JObject;
                Need((string)row["category"] == "Building_Road_Barrier" &&
                     (string)row["prefab"]["path"] == (string)gate["prefab_path"] &&
                     (int?)row["faction_id"] == (int?)gate["faction_id"] && (string)gate["door_lookup_name"] == "Door_Z" &&
                     (string)gate["owner"] == "BuildingRuntimeVisualPresentationSystemHelper.InitializeBuildingVisuals",
                    "Road-gate runtime facts must remain bound to the exact S073 source row and original visual owner");
                float open = (float)gate["door_local_euler_angles"]["z"];
                runtimeFacts = new SourceMapRuntimeFacts { SourceRowIndex = sourceRowIndex, SourceBuildingKey = "Building_Road_Barrier", HasDoor = true,
                    DoorClosedLocalEulerZ = 0f, DoorOpenLocalEulerZ = open };
            }

            JObject replayOrigin = (JObject)mapPlacementOriginReplay["origins"][sourceRowIndex];
            JArray capturedRotation = row["world_rotation_f32bits"] as JArray;
            JObject capturedEuler = row["world_euler_angles"] as JObject;
            Need(replayOrigin != null && capturedRotation != null && capturedRotation.Count == 4 &&
                 capturedEuler != null && (int?)replayOrigin["row_index"] == sourceRowIndex &&
                 (string)replayOrigin["source_path"] == (string)row["source_path"],
                "Exact captured placement and shared-origin replay must agree for row " + sourceRowIndex);

            return RegisterCapturedMapBuilding(sourceRowIndex, (string)row["category"],
                (string)row["source_path"], definition.FootprintCells.x, definition.FootprintCells.y,
                (int)replayOrigin["origin"][0], (int)replayOrigin["origin"][1], (bool)row["rotate_vertical"],
                (int)row["faction_id"], capturedRotation.Select(value => (int)value).ToArray(),
                new Vector3((float)capturedEuler["x"], (float)capturedEuler["y"], (float)capturedEuler["z"]),
                Array.Empty<int>(), allowSourceAuthoredMapWall: definition.IsWall, sourceMapRuntimeFacts: runtimeFacts);
        }
        using (var redirectQuery = em.CreateEntityQuery(typeof(UnitMove),
                   typeof(UnitGrid), typeof(LocalTransform)))
        {
            var redirectOwner = new BuildingPlacementRedirectCompositionSystemHelper();
            var redirectContext = new BuildingPlacementRedirectCompositionSystemHelper.Context(
                (out EntityManager manager) => { manager = em; return true; },
                (out Entity sourceGridEntity, out GridConfig sourceGridConfig, out DynamicBuffer<GridRoad> sourceRoads,
                    out DynamicBlockerComponent sourceBlockers) =>
                {
                    sourceGridEntity = gridEntity;
                    sourceGridConfig = em.GetComponentData<GridConfig>(gridEntity);
                    sourceRoads = em.GetBuffer<GridRoad>(gridEntity);
                    sourceBlockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
                    return true;
                },
                _ => { },
                () => redirectQuery);
            Action<RectInt> redirectSourceUnits = rect =>
            {
                redirectOwnerInvocationCount++;
                redirectOwner.RedirectUnitsAroundPlacedBuilding(redirectContext, rect);
            };
            ReadPinnedAuthoredMapBlocker(new Vector2Int(authoredOrigin.x, authoredOrigin.y),
                authoredDefinition.FootprintCells, out preservedAuthoredBlocker, out _, out _, out _,
                out capturedAuthoredRotation, out _);
            ActualCore::UnityEngine.Quaternion authoredWorldRotationOverride = new ActualCore::UnityEngine.Quaternion(
                authoredSourceWorldRotation.value.x, authoredSourceWorldRotation.value.y,
                authoredSourceWorldRotation.value.z, authoredSourceWorldRotation.value.w);
            var mapRegistration = new GeneratedBuildingRegistration(
                em, gridEntity, pipeline.CanonicalCollection, buildPlaneY, redirectSourceUnits);
            authoredMapBuilding = mapRegistration.RegisterAtLegalOrigin(
                authoredDefinition,
                new Vector2Int(authoredOrigin.x, authoredOrigin.y),
                birth: new SourceBuildingBirthContext(true),
                worldEulerAngles: authoredWorldEuler,
                preserveAuthoredTransform: true,
                removeOverlappingBlockers: true,
                removeBlockers: RemoveRuntimeBlockersAtOriginalBoundary,
                sourceWorldRotationOverride: authoredWorldRotationOverride);
            Need(authoredMapBuilding != null && authoredMapBuilding.CombatEntity != Entity.Null &&
                 authoredMapBuilding.Definition.Prefab.name == (string)authoredMapRow["prefab"]["key"] &&
                 authoredMapBuilding.OriginCell.x == authoredOrigin.x && authoredMapBuilding.OriginCell.y == authoredOrigin.y,
                 "Actual S073 map source row did not register through the shared runtime building owner");
            mapRegistration.SetOwner(authoredMapBuilding, (byte?)authoredMapRow["faction_id"]);
            autonomousWorld.Buildings.Add(authoredMapBuilding.Id, authoredMapBuilding);
            autonomousWorld.UpdateS073LegacyDiagnosticBlockerOwners();

            var mapCombatInfo = em.GetComponentData<RuntimeBuildingCombatInfo>(authoredMapBuilding.CombatEntity);
            var mapFaction = em.GetComponentData<Faction>(authoredMapBuilding.CombatEntity);
            LocalTransform mapActorTransform = em.GetComponentData<LocalTransform>(authoredMapBuilding.CombatEntity);
            var expectedMapCenter = new GeneratedSourceBuildingCenter().GetFootprintCenter(
                new Vector2Int(authoredOrigin.x, authoredOrigin.y),
                authoredDefinition.FootprintCells,
                grid,
                buildPlaneY);
            var expectedMapMathRotation = authoredSourceWorldRotation;
            Need(mapCombatInfo.RuntimeBuildingId == authoredMapBuilding.Id,
                "RuntimeBuildingCombatInfo runtime id mismatch actual=" + mapCombatInfo.RuntimeBuildingId + " registered=" + authoredMapBuilding.Id);
            Need(mapFaction.Id == (byte)authoredMapRow["faction_id"] && authoredMapBuilding.HasOwnerFaction &&
                 authoredMapBuilding.OwnerFactionId == (byte)authoredMapRow["faction_id"] &&
                 mapCombatInfo.OwnerFactionId == (byte)authoredMapRow["faction_id"],
                "Original owner-faction source mismatch registry=" + authoredMapBuilding.OwnerFactionId + " faction=" + mapFaction.Id +
                " combatInfo=" + mapCombatInfo.OwnerFactionId + " expected=" + (byte)authoredMapRow["faction_id"]);
            int[] actualActorRotationBits = new[]
            {
                BitConverter.ToInt32(BitConverter.GetBytes(mapActorTransform.Rotation.value.x), 0),
                BitConverter.ToInt32(BitConverter.GetBytes(mapActorTransform.Rotation.value.y), 0),
                BitConverter.ToInt32(BitConverter.GetBytes(mapActorTransform.Rotation.value.z), 0),
                BitConverter.ToInt32(BitConverter.GetBytes(mapActorTransform.Rotation.value.w), 0)
            };
            int[] expectedActorRotationBits = authoredRotationBits.Select(value => (int)value).ToArray();
            Need(actualActorRotationBits.SequenceEqual(expectedActorRotationBits),
                "Captured authored rotation mismatch actual=" + mapActorTransform.Rotation.value.x + "," + mapActorTransform.Rotation.value.y +
                "," + mapActorTransform.Rotation.value.z + "," + mapActorTransform.Rotation.value.w + " expected=" +
                expectedMapMathRotation.value.x + "," + expectedMapMathRotation.value.y + "," + expectedMapMathRotation.value.z + "," + expectedMapMathRotation.value.w);
            Need(mapActorTransform.Position.Equals((float3)expectedMapCenter),
                "Shared grid-centre actor position mismatch actual=" + mapActorTransform.Position.x + "," + mapActorTransform.Position.y +
                "," + mapActorTransform.Position.z + " expected=" + expectedMapCenter.x + "," + expectedMapCenter.y + "," + expectedMapCenter.z);
            Need(mapActorTransform.Scale == 1f, "Source combat actor scale mismatch actual=" + mapActorTransform.Scale);
            var mapGridBlockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
            bool mapFootprintBlocked = true;
            for (int y = authoredOrigin.y; y < authoredOrigin.y + authoredDefinition.FootprintCells.y; y++)
                for (int x = authoredOrigin.x; x < authoredOrigin.x + authoredDefinition.FootprintCells.x; x++)
                    mapFootprintBlocked &= mapGridBlockers.IsSet(x + y * grid.Width);
            Need(mapFootprintBlocked && (!expectsLegacyS002AuthoredOverlap ||
                 (preservedAuthoredBlocker != Entity.Null && em.Exists(preservedAuthoredBlocker) &&
                  em.HasComponent<StaticGridBlocker>(preservedAuthoredBlocker))),
                "Original static blocker owner did not retain the required source-authored blocker while registering the runtime map actor");
            Need(blockerRemovalCallbackCount == 1 && redirectOwnerInvocationCount == 1,
                "Shared registration must execute the selected row's blocker-removal and original redirect callbacks");
            var sourceMapPosition = new float3((float)authoredPosition["x"], (float)authoredPosition["y"], (float)authoredPosition["z"]);
            var sourceMapScale = new float3((float)authoredScale["x"], (float)authoredScale["y"], (float)authoredScale["z"]);
            buildings.Add(new JObject { ["runtime_id"] = authoredMapBuilding.Id,
                ["definition"] = authoredMapBuilding.Definition.Prefab.name,
                ["origin"] = new JArray(authoredOrigin.x, authoredOrigin.y),
                ["combat_entity"] = authoredMapBuilding.CombatEntity.Index + ":" + authoredMapBuilding.CombatEntity.Version,
                ["blocker_entity"] = authoredMapBuilding.BlockerEntity.Index + ":" + authoredMapBuilding.BlockerEntity.Version });
            authoredMapActorEvidence = new JObject
            {
                ["status"] = "Passed",
                ["source_row_index"] = (int)authoredMapRow["row_index"],
                ["source_prefab_identity"] = authoredMapRow["prefab"].DeepClone(),
                ["source_transform_position"] = new JArray(sourceMapPosition.x, sourceMapPosition.y, sourceMapPosition.z),
                ["source_transform_scale"] = new JArray(sourceMapScale.x, sourceMapScale.y, sourceMapScale.z),
                ["source_world_euler_angles"] = new JArray(authoredWorldEuler.x, authoredWorldEuler.y, authoredWorldEuler.z),
                ["source_world_rotation"] = new JArray(authoredSourceWorldRotation.value.x, authoredSourceWorldRotation.value.y,
                    authoredSourceWorldRotation.value.z, authoredSourceWorldRotation.value.w),
                ["source_world_rotation_f32bits"] = authoredRotationBits.DeepClone(),
                ["source_world_rotation_authority"] = mapPlacementCapture["placement_rotation_authority"].DeepClone(),
                ["source_world_center"] = authoredMapRow["world_center"].DeepClone(),
                ["runtime_id"] = authoredMapBuilding.Id,
                ["runtime_origin"] = new JArray(authoredOrigin.x, authoredOrigin.y),
                ["footprint"] = new JArray(authoredDefinition.FootprintCells.x, authoredDefinition.FootprintCells.y),
                ["owner_faction_id"] = (int)authoredMapRow["faction_id"],
                ["runtime_faction_component_id"] = mapFaction.Id,
                ["combat_info_owner_faction_id"] = mapCombatInfo.OwnerFactionId,
                ["combat_info_owner_faction_matches_config"] = mapCombatInfo.OwnerFactionId == (byte)authoredMapRow["faction_id"],
                ["shared_building_registration_kernel_used"] = true,
                ["shared_runtime_actor_factory_used"] = true,
                ["same_live_registry_as_initial_spawn"] = true,
                ["same_entity_manager"] = true,
                ["actual_redirect_owner_invoked"] = true,
                ["redirect_owner_invocation_count"] = redirectOwnerInvocationCount,
                ["blocker_removal_requested"] = true,
                ["blocker_removal_callback_count"] = blockerRemovalCallbackCount,
                ["preserved_authored_overlap_count"] = preservedAuthoredOverlaps,
                ["preserved_authored_overlap"] = preservedAuthoredOverlapEvidence,
                ["blocker_removal_effect"] = "UnregisteredAuthoredBlockerPreserved",
                ["native_coexistence_or_readiness_proven"] = false,
                ["original_static_blocker_owner_updated"] = true,
                ["combat_actor_faction_verified"] = mapFaction.Id == (byte)authoredMapRow["faction_id"],
                ["combat_actor_rotation_verified"] = actualActorRotationBits.SequenceEqual(expectedActorRotationBits),
                ["combat_actor_center_verified"] = mapActorTransform.Position.Equals((float3)expectedMapCenter),
                ["combat_actor_scale"] = mapActorTransform.Scale,
                ["footprint_blocked_in_selected_grid"] = mapFootprintBlocked,
                ["visual_instance_created"] = false,
                ["native_visual_readiness"] = false,
                ["rows_registered"] = 1,
                ["rows_total"] = 432
            };
            mapPlacementCallbackSuccessRows.Add(scheduledIndex);


        }

            return true;
        }

        bool IsCapturedPlayerProductionPlacement(int scheduledIndex)
        {
            mapPlacementPriorityPassRows.Add(scheduledIndex);
            JArray sourceRows = mapPlacementCapture["placement_rows"] as JArray;
            if (sourceRows == null || scheduledIndex < 0 || scheduledIndex >= sourceRows.Count)
                return false;
            JObject sourceRow = (JObject)sourceRows[scheduledIndex];
            RuntimeDefinition definition = mapDefinitions.Definitions[(int)sourceRow["prefab_index"]];
            int productionCount = MapBuildingPlacementPriorityPolicy.GetProductionCount(
                definition.ProductionSlots != null ? definition.ProductionSlots.Count : 0,
                definition.SpawnUnitPrefab != null,
                definition.SecondarySpawnUnitPrefab != null,
                definition.TertiarySpawnUnitPrefab != null,
                definition.QuaternarySpawnUnitPrefab != null);
            return MapBuildingPlacementPriorityPolicy.IsPlayerProductionPlacement(
                (byte)sourceRow["faction_id"], (byte)FactionIdentity.PlayerFactionId, productionCount);
        }
        var mapPlacementOwner = new MapBuildingPlacementSpawnPrefabSystemHelper();
        int mapPlacementSourceRowCount = ((JArray)mapPlacementCapture["placement_rows"]).Count;
        bool mapPlacementSequenceQueued = false;
        int mapPlacementOwnerUpdateCount = 0;
        while (!mapPlacementSequenceQueued && mapPlacementOwnerUpdateCount < 16)
        {
            mapPlacementSequenceQueued = mapPlacementOwner.UpdateDataOnly(
                mapPlacementSourceRowCount, IsCapturedPlayerProductionPlacement, RegisterCapturedMapRow);
            mapPlacementOwnerUpdateCount++;
        }
        Need(mapPlacementSourceRowCount == 432 && mapPlacementPriorityPassRows.SequenceEqual(Enumerable.Range(0, 432)) &&
             mapPlacementSequenceQueued && mapPlacementOwner.IsComplete && mapPlacementOwnerUpdateCount == 14 &&
             mapPlacementCallbackRows.Count == mapPlacementSourceRowCount &&
             mapPlacementCallbackRows.Distinct().OrderBy(sourceRowIndex => sourceRowIndex)
                 .SequenceEqual(Enumerable.Range(0, mapPlacementSourceRowCount)) &&
             mapPlacementCallbackSuccessRows.OrderBy(sourceRowIndex => sourceRowIndex).SequenceEqual(new[] { 1, 2, 3, 4, 5, 6, 20, 21 }
                 .Concat(sourceDrivenMapRows.Select(row => (int)row["source_row_index"]))
                 .OrderBy(sourceRowIndex => sourceRowIndex)) &&
             sourceDrivenMapRows.Any(row => (int)row["source_row_index"] == 25) &&
             authoredMapActorEvidence != null && (string)authoredMapActorEvidence["status"] == "Passed" &&
             authoredMapRotatedActorEvidence != null && (string)authoredMapRotatedActorEvidence["status"] == "Passed" &&
             authoredMapThirdBarrackEvidence != null && (string)authoredMapThirdBarrackEvidence["status"] == "Passed" &&
             authoredMapFourthBarrackEvidence != null && (string)authoredMapFourthBarrackEvidence["status"] == "Passed" &&
             authoredMapFifthFuelBladderEvidence != null && (string)authoredMapFifthFuelBladderEvidence["status"] == "Passed" &&
             authoredMapSixthFuelBladderEvidence != null && (string)authoredMapSixthFuelBladderEvidence["status"] == "Passed" &&
             authoredMapEnemyHallEvidence != null && (string)authoredMapEnemyHallEvidence["status"] == "Passed" &&
             authoredMapSecondEnemyHallEvidence != null && (string)authoredMapSecondEnemyHallEvidence["status"] == "Passed" &&
             authoredMapHouseEvidence != null && (string)authoredMapHouseEvidence["status"] == "Passed",
            "Original data-only placement owner must complete the 432-row priority pass and all fourteen normal 32-row updates, register eligible source-driven ordinary actors, and queue completion");

        var s073AIConfigs = new List<AIControllerConfig>
        {
            ReadAIController(enemyAIPath, "Enemy"), ReadAIController(playerAIPath, "Player")
        };
        AIPlanEntryStartupConfig s073AIPlan = ReadAIPlan(aiPlanPath);
        bool TryResolveS073SpawnCell(byte factionId, out int2 spawnCell)
        {
            LegacyInitialFactionData faction = initialData.Factions.SingleOrDefault(row => row != null && row.FactionId == factionId);
            if (faction == null) { spawnCell = default; return false; }
            spawnCell = new int2(faction.SpawnX, faction.SpawnY);
            return true;
        }
        var aiStartupSystem = new AIStartupSystem();
        AIStartupSystem.Result aiStartupResult = aiStartupSystem.Initialize(
            em, s073AIConfigs, s073AIPlan, TryResolveS073SpawnCell, AISettingsSnapshot.Defaults);
        using (var factionControlQuery = em.CreateEntityQuery(ComponentType.ReadOnly<FactionControlConfigTag>()))
        using (var buildPlanQuery = em.CreateEntityQuery(ComponentType.ReadOnly<AIBuildPlan>()))
        using (var productionPlanQuery = em.CreateEntityQuery(ComponentType.ReadOnly<AIProductionPlan>()))
        {
            Need(factionControlQuery.CalculateEntityCount() == 1 && buildPlanQuery.CalculateEntityCount() == 2 &&
                 productionPlanQuery.CalculateEntityCount() == 2,
                "Original S073 AI startup must materialize plans for its two exact preset controllers");
            Entity factionControlEntity = factionControlQuery.GetSingletonEntity();
            DynamicBuffer<FactionControlEntry> controls = em.GetBuffer<FactionControlEntry>(factionControlEntity);
            Need(controls.Length == 2 && controls.Any(row => row.FactionId == 2 && row.AIControlled == 1) &&
                 controls.Any(row => row.FactionId == 1 && row.IsPlayerFaction == 1 && row.AIControlled == 0),
                "Original S073 AI faction-control projection must preserve the authored enemy and disabled player-auto policies");
            using NativeArray<Entity> plans = buildPlanQuery.ToEntityArray(Allocator.Temp);
            var projectedPlans = plans.Select(entity => em.GetComponentData<AIBuildPlan>(entity)).ToArray();
            Need(projectedPlans.Any(row => row.FactionId == 2 && row.BaseCenterCell.x == initialData.Factions.Single(f => f.FactionId == 2).SpawnX &&
                                           row.BaseCenterCell.y == initialData.Factions.Single(f => f.FactionId == 2).SpawnY && row.Enabled == 1) &&
                 projectedPlans.Any(row => row.FactionId == 1 && row.BaseCenterCell.x == initialData.Factions.Single(f => f.FactionId == 1).SpawnX &&
                                           row.BaseCenterCell.y == initialData.Factions.Single(f => f.FactionId == 1).SpawnY && row.Enabled == 0),
                "Original S073 AI build plans must use source spawn cells and actual default player-auto policy");
        }
        Need(aiStartupResult.HasPlayerAutoMode && !aiStartupResult.PlayerAutoModeEnabled,
            "Original S073 AI startup result must report its configured but default-disabled player-auto mode");
        // Invoke the real original lifecycle owner on the same retained EntityManager. Play was
        // requested, but the source loading gate has not admitted SimulationActive; the rules
        // owner must therefore preserve Preparing and must not invent a playing transition.
        rulesState.Begin();
        try { rulesSystem.OnUpdate(ref rulesState); }
        finally { rulesState.End(); }
        var matchAfterRules = em.GetComponentData<SkirmishMatchState>(session);
        var gameplayAfterRules = em.GetComponentData<RuntimeGameplayStateComponent>(gameplayStateEntity);
        Need(matchAfterRules.Phase == SkirmishPhase.Preparing && gameplayAfterRules.PlayRequested == 1 &&
             gameplayAfterRules.SimulationActive == 0 && matchAfterRules.Outcome == SkirmishOutcome.None,
             "Original rules owner must retain the real inactive loading gate without synthesizing mission start");
        autonomousWorld.CompleteInitialRoadInputs();
        // The original initial-unit owner consumes completed initial-building requests for
        // this plan after observing their results. Preserve the pre-spawn receipts above,
        // then verify the owner consumed them and the actual buildings remain registered.
        requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
        int initialPlanRequestsRemaining = 0;
        for (int i = 0; i < requests.Length; i++)
            if (requests[i].PlanEntity == plan) initialPlanRequestsRemaining++;
        bool allTenInitialRequestsSucceeded = rows.Count == 10 && initialPlanRequestsRemaining == 0 &&
            em.HasComponent<InitialUnitsSpawnInitialized>(plan) && newlySpawned.Count == 24 &&
            actualBlockerRows.Count == (int)blockerConfig["blocker_count"];
        foreach (JObject row in rows)
        {
            int runtimeId = (int)row["building_runtime_id"];
            allTenInitialRequestsSucceeded &= (string)row["status"] == BuildingRuntimeSpawnRequest.Succeeded.ToString() &&
                (int)row["result_code"] == 0 && runtimeId > 0 && registry.ContainsKey(runtimeId);
        }
        bool authoredMapActorSucceeded = authoredMapBuilding != null && authoredMapActorEvidence != null &&
            (string)authoredMapActorEvidence["status"] == "Passed" &&
            registry.ContainsKey(authoredMapBuilding.Id) && autonomousWorld.Buildings.ContainsKey(authoredMapBuilding.Id);
        bool authoredMapRotatedActorSucceeded = authoredMapRotatedActorEvidence != null &&
            (string)authoredMapRotatedActorEvidence["status"] == "Passed" &&
            registry.ContainsKey((int)authoredMapRotatedActorEvidence["runtime_id"]) &&
            autonomousWorld.Buildings.ContainsKey((int)authoredMapRotatedActorEvidence["runtime_id"]);
        bool authoredMapThirdBarrackSucceeded = authoredMapThirdBarrackEvidence != null &&
            (string)authoredMapThirdBarrackEvidence["status"] == "Passed" &&
            registry.ContainsKey((int)authoredMapThirdBarrackEvidence["runtime_id"]) &&
            autonomousWorld.Buildings.ContainsKey((int)authoredMapThirdBarrackEvidence["runtime_id"]);
        bool authoredMapFourthBarrackSucceeded = authoredMapFourthBarrackEvidence != null &&
            (string)authoredMapFourthBarrackEvidence["status"] == "Passed" &&
            registry.ContainsKey((int)authoredMapFourthBarrackEvidence["runtime_id"]) &&
            autonomousWorld.Buildings.ContainsKey((int)authoredMapFourthBarrackEvidence["runtime_id"]);
        bool authoredMapFifthFuelBladderSucceeded = authoredMapFifthFuelBladderEvidence != null &&
            (string)authoredMapFifthFuelBladderEvidence["status"] == "Passed" &&
            registry.ContainsKey((int)authoredMapFifthFuelBladderEvidence["runtime_id"]) &&
            autonomousWorld.Buildings.ContainsKey((int)authoredMapFifthFuelBladderEvidence["runtime_id"]);
        bool authoredMapSixthFuelBladderSucceeded = authoredMapSixthFuelBladderEvidence != null &&
            (string)authoredMapSixthFuelBladderEvidence["status"] == "Passed" &&
            registry.ContainsKey((int)authoredMapSixthFuelBladderEvidence["runtime_id"]) &&
            autonomousWorld.Buildings.ContainsKey((int)authoredMapSixthFuelBladderEvidence["runtime_id"]);
        bool authoredMapEnemyHallSucceeded = authoredMapEnemyHallEvidence != null &&
            (string)authoredMapEnemyHallEvidence["status"] == "Passed" &&
            registry.ContainsKey((int)authoredMapEnemyHallEvidence["runtime_id"]) &&
            autonomousWorld.Buildings.ContainsKey((int)authoredMapEnemyHallEvidence["runtime_id"]);
        bool authoredMapSecondEnemyHallSucceeded = authoredMapSecondEnemyHallEvidence != null &&
            (string)authoredMapSecondEnemyHallEvidence["status"] == "Passed" &&
            registry.ContainsKey((int)authoredMapSecondEnemyHallEvidence["runtime_id"]) &&
            autonomousWorld.Buildings.ContainsKey((int)authoredMapSecondEnemyHallEvidence["runtime_id"]);
        bool authoredMapHouseSucceeded = authoredMapHouseEvidence != null &&
            (string)authoredMapHouseEvidence["status"] == "Passed" &&
            registry.ContainsKey((int)authoredMapHouseEvidence["runtime_id"]) &&
            autonomousWorld.Buildings.ContainsKey((int)authoredMapHouseEvidence["runtime_id"]);
        bool authoredMapWallActorSucceeded = authoredMapWallEvidence != null &&
            (string)authoredMapWallEvidence["status"] == "Passed" &&
            registry.ContainsKey((int)authoredMapWallEvidence["runtime_id"]) &&
            autonomousWorld.Buildings.ContainsKey((int)authoredMapWallEvidence["runtime_id"]);
        bool allSucceeded = allTenInitialRequestsSucceeded && authoredMapActorSucceeded &&
            authoredMapRotatedActorSucceeded && authoredMapThirdBarrackSucceeded && authoredMapFourthBarrackSucceeded &&
            authoredMapFifthFuelBladderSucceeded && authoredMapSixthFuelBladderSucceeded &&
            authoredMapEnemyHallSucceeded && authoredMapSecondEnemyHallSucceeded && authoredMapHouseSucceeded &&
            sourceDrivenMapRows.Count > 0 && sourceDrivenMapRows.All(row => (string)row["status"] == "Passed") &&
            authoredMapWallActorSucceeded;

        string runtimeCityConfigPath = Path.Combine(sourceRoot,
            "Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset");
        Need(Sha(runtimeCityConfigPath) == "e03e012f015aa3e06649d98a876088fc80661ede55bc8f47268074961232c987",
            "Exact selected Campaign runtime-city configuration required");
        Match runtimeCityCountMatch = Regex.Match(File.ReadAllText(runtimeCityConfigPath),
            "(?m)^  cityCount:\\s*(\\d+)\\s*$");
        Need(runtimeCityCountMatch.Success, "Authored runtime-city count must be observed");
        int runtimeCityCount = int.Parse(runtimeCityCountMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        // This headless world omits the optional city presenter. That is safe only
        // because the exact selected Campaign config requests zero generated cities.
        bool runtimeCityOwnerPresent = false;
        bool runtimeCityOwnerComplete = false;
        bool runtimeCityOmissionSafe = runtimeCityCount == 0;
        // Count through the same ownership kernel used to register these generated blockers.
        // A broad ECS query also includes authored Campaign blockers and cannot prove that the
        // optional runtime-blocker owner completed its own requested placements.
        int actualRuntimeBlockerOwnerCount = runtimeBlockerOwnership.Count;
        bool runtimeGridBlockerOwnerPresent = true;
        bool runtimeGridBlockerOwnerComplete = actualRuntimeBlockerOwnerCount == (int)blockerConfig["blocker_count"] &&
            actualBlockerRows.Count == (int)blockerConfig["blocker_count"];
        using var spawnConfigQuery = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig));
        using var spawnInitializedQuery = em.CreateEntityQuery(typeof(InitialUnitsSpawnInitialized));
        int initialSpawnConfigCount = spawnConfigQuery.CalculateEntityCount();
        int initialSpawnInitializedCount = spawnInitializedQuery.CalculateEntityCount();
        bool headlessGameplayInitialized = allSucceeded && mapPlacementOwner.IsComplete &&
            initialPlanRequestsRemaining == 0 && newlySpawned.Count == 24 && aiStartupResult.HasPlayerAutoMode &&
            autonomousWorld.InitialRoadReceipt != null && runtimeGridBlockerOwnerComplete && runtimeCityOmissionSafe;
        bool playRequestedAtGate = em.GetComponentData<RuntimeGameplayStateComponent>(gameplayStateEntity).PlayRequested != 0;
        bool worldAvailableAtGate = em.World != null && em.World.IsCreated;
        // The selected Campaign scene renders authored scenery, but decorations only instantiate
        // GameObjects and reserve temporary local cells; they never mutate simulation occupancy.
        // The CLI omits that presenter while retaining the actual city/blocker data-owner results.
        bool runtimeDecorationPresenterPresent = false;
        bool runtimeDecorationPresenterComplete = false;
        bool sharedLoadingGatePassed = GameplayStartGatePolicy.IsGameplayStartComplete(
            headlessGameplayInitialized,
            playRequestedAtGate,
            allSucceeded && initialPlanRequestsRemaining == 0,
            runtimeCityOwnerPresent,
            runtimeCityOwnerComplete,
            runtimeGridBlockerOwnerPresent,
            runtimeGridBlockerOwnerComplete,
            runtimeDecorationPresenterPresent,
            runtimeDecorationPresenterComplete,
            worldAvailableAtGate,
            initialSpawnConfigCount,
            initialSpawnInitializedCount);
        if (sharedLoadingGatePassed)
        {
            RuntimeGameplayStateComponent admittedGameplayState =
                em.GetComponentData<RuntimeGameplayStateComponent>(gameplayStateEntity);
            admittedGameplayState.SimulationActive = 1;
            em.SetComponentData(gameplayStateEntity, admittedGameplayState);
            rulesState.Begin();
            try { rulesSystem.OnUpdate(ref rulesState); }
            finally { rulesState.End(); }
        }
        var gameplayAfterHeadlessGate = em.GetComponentData<RuntimeGameplayStateComponent>(gameplayStateEntity);
        var matchAfterHeadlessGate = em.GetComponentData<SkirmishMatchState>(session);
        Need(!sharedLoadingGatePassed ||
             gameplayAfterHeadlessGate.SimulationActive == 1 && matchAfterHeadlessGate.Phase == SkirmishPhase.Playing,
             "Shared loading-gate admission must reach Playing only through the original legacy rules owner");
        Need(sharedLoadingGatePassed,
             "The complete actual S073 legacy startup must satisfy the shared gameplay-start gate");
        autonomousWorld.MarkS073LegacyLoadingGateAdmitted();
        var surfaceComponent = em.GetComponentData<MapSurfaceComponent>(surfaceEntity);
        int surfaceCount = MapSurfaceBlobAccess.SurfaceCount(ref surfaceComponent.SurfaceBlob.Value);
        Need(!em.HasBuffer<BuildingRuntimeOwnedBuildingSummary>(boundary),
            "S073 AI probe must begin without an existing owned-building summary buffer");
        DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> ownedBuildingProjection =
            em.AddBuffer<BuildingRuntimeOwnedBuildingSummary>(boundary);
        var expectedOwnedBuildingCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (RuntimeBuildingEntity building in registry.Values)
        {
            if (building == null || building.IsDestroyed || !building.HasOwnerFaction || building.Definition == null)
                continue;

            string buildingId = building.Definition.Prefab != null
                ? BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(building.Definition.Prefab.name)
                : string.Empty;
            if (string.IsNullOrEmpty(buildingId))
                buildingId = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(building.Definition.DisplayName);
            if (string.IsNullOrEmpty(buildingId))
                buildingId = building.Definition.DisplayName ?? string.Empty;

            var summaryKey = building.OwnerFactionId.ToString(CultureInfo.InvariantCulture) + "|" + buildingId;
            expectedOwnedBuildingCounts[summaryKey] = expectedOwnedBuildingCounts.TryGetValue(summaryKey, out int count)
                ? count + 1 : 1;
        }
        BuildingRuntimeReadModelProjection.PublishOwnedBuildingSummaries(
            ownedBuildingProjection, registry,
            building =>
            {
                var prefab = building.Definition.Prefab;
                string fallback = building.Definition.DisplayName;
                string normalized = prefab != null
                    ? BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(prefab.name)
                    : string.Empty;
                if (string.IsNullOrEmpty(normalized))
                    normalized = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(fallback);
                return new FixedString128Bytes(string.IsNullOrEmpty(normalized) ? fallback ?? string.Empty : normalized);
            });
        var projectedOwnedBuildings = new JArray(ownedBuildingProjection.Select(summary => new JObject
        {
            ["faction_id"] = summary.FactionId,
            ["building_id"] = summary.BuildingId.ToString(),
            ["count"] = summary.Count
        }));
        Need(ownedBuildingProjection.Length == expectedOwnedBuildingCounts.Count &&
             ownedBuildingProjection.All(summary => expectedOwnedBuildingCounts.TryGetValue(
                 summary.FactionId.ToString(CultureInfo.InvariantCulture) + "|" + summary.BuildingId,
                 out int expectedCount) && expectedCount == summary.Count),
             "Shared runtime owned-building readmodel projection disagrees with independently grouped live registry facts");
        Need(!em.HasBuffer<BuildingRuntimeFactionSummary>(boundary),
            "S073 AI probe must begin without an existing faction summary buffer");
        DynamicBuffer<BuildingRuntimeFactionSummary> factionSummaries =
            em.AddBuffer<BuildingRuntimeFactionSummary>(boundary);
        var factionIds = new List<byte>();
        foreach (RuntimeBuildingEntity building in registry.Values)
            if (building != null && building.HasOwnerFaction && !factionIds.Contains(building.OwnerFactionId))
                factionIds.Add(building.OwnerFactionId);
        var factionSummaryInputs = new JArray();
        var factionResourceSystem = new FactionResourceCompositionSystemHelper();
        foreach (byte factionId in factionIds)
        {
            bool resourceSnapshotPresent = factionResourceSystem.TryGetFactionResourceEconomy(
                em, registry, factionId,
                out FactionResourceCompositionSystemHelper.ResourceEconomySnapshot economy);
            int buildingCount = registry.Values.Count(building =>
                building != null && !building.IsDestroyed && building.HasOwnerFaction &&
                building.OwnerFactionId == factionId);
            BuildingRuntimeReadModelProjection.AddFactionSummary(
                factionSummaries, factionId, buildingCount, economy);
            factionSummaryInputs.Add(new JObject
            {
                ["faction_id"] = factionId,
                ["resource_snapshot_present"] = resourceSnapshotPresent,
                ["building_count_from_independent_live_registry_grouping"] = buildingCount,
                ["stored_oil_barrels"] = economy.StoredOilBarrels,
                ["stored_fuel_barrels"] = economy.StoredFuelBarrels,
                ["oil_barrels_per_day"] = economy.OilBarrelsPerDay,
                ["fuel_barrels_per_day"] = economy.FuelBarrelsPerDay
            });
        }
        Need(factionSummaries.Length == factionIds.Count,
            "Shared faction summary projection omitted a runtime owner faction");
        var aiPlanner = new AIBuildPlannerSystem();
        var aiPlannerState = State(em);
        aiPlanner.OnCreate(ref aiPlannerState);
        bool aiPlannerQueriesPresent = aiPlannerState.RequiredQueriesPresent;
        int aiRequestsBefore = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length;
        int aiPlannerStepCount = 0;
        const float aiPlannerStepSeconds = 1f / 60f;
        while (aiPlannerQueriesPresent && gameplayAfterHeadlessGate.SimulationActive != 0 &&
               aiPlannerStepCount < 2700 && em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length == aiRequestsBefore)
        {
            TimeData worldTime = em.WorldUnmanaged.Time;
            worldTime.ElapsedTime += aiPlannerStepSeconds;
            worldTime.DeltaTime = aiPlannerStepSeconds;
            em.WorldUnmanaged.Time = worldTime;
            aiPlannerState.Begin();
            try { aiPlanner.OnUpdate(ref aiPlannerState); }
            finally { aiPlannerState.End(); }
            aiPlannerStepCount++;
        }
        DynamicBuffer<BuildingRuntimeSpawnRequest> aiRequests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
        int aiRequestCountBeforeProcessing = aiRequests.Length;
        if (aiRequestCountBeforeProcessing > aiRequestsBefore)
            pipeline.ProcessRequests((float)em.WorldUnmanaged.Time.ElapsedTime, forceScan: true);
        aiRequests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
        var aiRequestRows = new JArray(aiRequests.Skip(aiRequestsBefore).Select(request => new JObject
        {
            ["request_id"] = request.RequestId,
            ["faction_id"] = request.FactionId,
            ["building_id"] = request.BuildingId.ToString(),
            ["preferred_origin"] = new JArray(request.PreferredOrigin.x, request.PreferredOrigin.y),
            ["status"] = request.Status.ToString(),
            ["result_code"] = request.ResultCode
        }));
        var aiPlanRows = new JArray();
        using (var aiPlanQuery = em.CreateEntityQuery(ComponentType.ReadOnly<AIBuildPlan>(), ComponentType.ReadOnly<AIBuildPlanEntry>()))
        using (var aiPlanEntities = aiPlanQuery.ToEntityArray(Allocator.Temp))
        {
            for (int i = 0; i < aiPlanEntities.Length; i++)
            {
                Entity planEntity = aiPlanEntities[i];
                AIBuildPlan actualPlan = em.GetComponentData<AIBuildPlan>(planEntity);
                aiPlanRows.Add(new JObject
                {
                    ["faction_id"] = actualPlan.FactionId,
                    ["enabled"] = actualPlan.Enabled != 0,
                    ["next_build_index"] = actualPlan.NextBuildIndex,
                    ["base_center"] = new JArray(actualPlan.BaseCenterCell.x, actualPlan.BaseCenterCell.y),
                    ["build_interval_seconds"] = actualPlan.BuildIntervalSeconds,
                    ["last_build_time"] = actualPlan.LastBuildTime,
                    ["entry_ids"] = new JArray(em.GetBuffer<AIBuildPlanEntry>(planEntity)
                        .Select(entry => entry.BuildingId.ToString()))
                });
            }
        }
        return new JObject { ["kind"] = "S073LegacyPlacementOwnerHarness", ["status"] = allSucceeded ? "Passed" : "Failed",
            ["mission"] = "S073", ["source_identity_hash"] = selected.SourceIdentityHash, ["campaign_source"] = selected.SourceBinding.SourceOperationMapId,
            ["placement_config_variant"] = new JObject { ["kind"] = (string)placementConfig["kind"],
                ["path"] = placementConfigPath, ["sha256"] = args[24],
                ["derived_from_legacy_sha256"] = args[7],
                ["derived_from_placement_receipt_sha256"] = (string)placementConfig["derived_from_placement_receipt_sha256"],
                ["loaded_as_external_config"] = true,
                ["all_candidate_origins_found"] = originalOwnerSearchRows == initialBuildings.Length,
                ["all_candidate_origins_resolved"] = originalOwnerSearchRows + originalPrefixReplayRows == initialBuildings.Length,
                ["original_owner_search_rows"] = originalOwnerSearchRows,
                ["original_owner_prefix_replay_rows"] = originalPrefixReplayRows,
                ["runtime_owner_acceptance_pending"] = !allSucceeded,
                ["persisted_to_project"] = false,
                ["overrides"] = placementCandidates, ["changes_campaign_geometry"] = false,
                ["changes_shared_spawn_rule"] = false, ["changes_resources_or_budgets"] = false },
            ["original_map_placement_origin_replay"] = mapPlacementOriginReplay,
            ["grid"] = new JObject { ["width"] = grid.Width, ["height"] = grid.Height, ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z), ["cell_size"] = grid.CellSize },
            ["opening_prefab_root_evidence"] = new JObject { ["production_capture_sha256"] = args[3],
                ["historical_compact_capture_sha256"] = (string)sourceIdentity["prefab_capture_sha256"],
                ["legacy_root_count"] = expectedInitialRoots.Properties().Count(), ["matched_current_capture"] = true,
                ["independent_name_readback"] = true },
            ["configured_spawnable_source_rows"] = configuredSpawnableSourceRows,
            ["runtime_blocker_placement"] = actualBlockerPlacement,
            ["map_placement_baked_owner_geometry_audit"] = bakedMapGeometryAudit,
            ["rotated_candidate_row_preflight"] = rotatedCandidatePreflight,
            ["authored_map_actor"] = authoredMapActorEvidence,
            ["authored_map_rotated_actor"] = authoredMapRotatedActorEvidence,
            ["authored_map_third_barrack_actor"] = authoredMapThirdBarrackEvidence,
            ["authored_map_fourth_barrack_actor"] = authoredMapFourthBarrackEvidence,
            ["authored_map_fifth_fuel_bladder_actor"] = authoredMapFifthFuelBladderEvidence,
            ["authored_map_sixth_fuel_bladder_actor"] = authoredMapSixthFuelBladderEvidence,
            ["authored_map_enemy_hall_actor"] = authoredMapEnemyHallEvidence,
            ["authored_map_second_enemy_hall_actor"] = authoredMapSecondEnemyHallEvidence,
            ["authored_map_house_actor"] = authoredMapHouseEvidence,
            ["source_driven_map_actors"] = sourceDrivenMapRows,
            ["authored_map_wall_actor"] = authoredMapWallEvidence,
            ["map_placement_sequence"] = new JObject
            {
                ["authority"] = "Original MapBuildingPlacementSpawnPrefabSystemHelper.UpdateDataOnly and its shared MapBuildingPlacementSequenceKernel",
                ["configured_source_rows"] = mapPlacementSourceRowCount,
                ["normal_update_count"] = mapPlacementOwnerUpdateCount,
                ["priority_pass_completed"] = mapPlacementPriorityPassRows.SequenceEqual(Enumerable.Range(0, mapPlacementSourceRowCount)),
                ["next_sequential_index"] = mapPlacementCallbackRows.Count,
                ["sequential_limit_per_update"] = 32,
                ["queued_complete"] = mapPlacementSequenceQueued,
                ["callback_attempt_source_rows"] = new JArray(mapPlacementCallbackRows),
                ["callback_success_source_rows"] = new JArray(mapPlacementCallbackSuccessRows),
                ["unsupported_sequential_source_rows"] = unsupportedSequentialRows,
                ["map_owner_complete"] = mapPlacementOwner.IsComplete
            },
            ["map_placements_materialized"] = (authoredMapActorEvidence != null ? 1 : 0) +
                (authoredMapRotatedActorEvidence != null ? 1 : 0) + (authoredMapThirdBarrackEvidence != null ? 1 : 0) +
                (authoredMapFourthBarrackEvidence != null ? 1 : 0) + (authoredMapFifthFuelBladderEvidence != null ? 1 : 0) +
                (authoredMapSixthFuelBladderEvidence != null ? 1 : 0) + (authoredMapEnemyHallEvidence != null ? 1 : 0) +
                (authoredMapSecondEnemyHallEvidence != null ? 1 : 0) + sourceDrivenMapRows.Count,
            ["map_placements_total"] = 432,
            ["map_placement_runtime_owner_complete"] = mapPlacementOwner.IsComplete,
            ["surface_samples"] = surfaceCount,
            ["authored_navigation_rows"] = authoredNavigation, ["initial_road_owner"] = autonomousWorld.InitialRoadReceipt, ["requests_queued"] = queued,
            ["processing_frames"] = frames, ["request_results"] = rows, ["registered_buildings"] = buildings,
            ["building_count"] = registry.Count, ["initial_plan_requests_remaining"] = initialPlanRequestsRemaining,
            ["owned_building_summary_projection"] = new JObject
            {
                ["owner"] = "Shared BuildingRuntimeReadModelProjection called with actual live registry entries",
                ["written_to_original_runtime_boundary"] = true,
                ["actual_registry_count"] = registry.Count,
                ["eligible_live_owned_registry_count"] = expectedOwnedBuildingCounts.Values.Sum(),
                ["summary_row_count"] = ownedBuildingProjection.Length,
                ["summary_rows"] = projectedOwnedBuildings,
                ["matches_independent_live_registry_grouping"] = true
            },
            ["faction_summary_projection"] = new JObject
            {
                ["owner"] = "Shared BuildingRuntimeReadModelProjection with original resource and building-count owners",
                ["faction_rows"] = factionSummaryInputs,
                ["projected_row_count"] = factionSummaries.Length,
                ["original_build_planner_queries_present"] = aiPlannerQueriesPresent,
                ["original_build_planner_normal_steps"] = aiPlannerStepCount,
                ["planner_step_seconds"] = aiPlannerStepSeconds,
                ["planner_elapsed_seconds"] = em.WorldUnmanaged.Time.ElapsedTime,
                ["original_plan_state_after_updates"] = aiPlanRows,
                ["original_spawn_processor_invoked"] = aiRequestCountBeforeProcessing > aiRequestsBefore,
                ["actual_new_build_requests"] = aiRequestRows
            },
            ["initial_plan_requests_consumed_by_original_owner"] = initialPlanRequestsRemaining == 0 && em.HasComponent<InitialUnitsSpawnInitialized>(plan),
            ["all_ten_actual_requests_succeeded"] = allTenInitialRequestsSucceeded,
            ["original_play_request"] = new JObject { ["owner"] = "RuntimeGameplayStateSystem.ResetForGameplayStart",
                ["request_observed"] = gameplayStateAfterRequest.PlayRequested == 1,
                ["simulation_active"] = gameplayAfterHeadlessGate.SimulationActive != 0,
                ["loading_gate_evaluated"] = true, ["simulation_started"] = matchAfterHeadlessGate.Phase == SkirmishPhase.Playing },
            ["headless_loading_gate"] = new JObject { ["policy"] = "GameplayStartGatePolicy.IsGameplayStartComplete",
                ["admitted"] = sharedLoadingGatePassed, ["headless_gameplay_owners_initialized"] = headlessGameplayInitialized,
                ["building_startup_complete"] = allSucceeded && initialPlanRequestsRemaining == 0,
                ["runtime_city_owner_present"] = runtimeCityOwnerPresent, ["runtime_city_count"] = runtimeCityCount,
                ["runtime_city_config_sha256"] = Sha(runtimeCityConfigPath),
                ["runtime_city_omission_safe_for_zero_count"] = runtimeCityOmissionSafe,
                ["runtime_city_owner_complete"] = runtimeCityOwnerComplete,
                ["runtime_grid_blocker_owner_present"] = runtimeGridBlockerOwnerPresent,
                ["runtime_grid_blocker_owner_count"] = actualRuntimeBlockerOwnerCount,
                ["runtime_grid_blocker_owner_complete"] = runtimeGridBlockerOwnerComplete,
                ["runtime_decoration_presenter_omitted"] = !runtimeDecorationPresenterPresent,
                ["runtime_decoration_gameplay_occupancy_effect"] = false,
                ["world_available"] = worldAvailableAtGate,
                ["initial_spawn_config_count"] = initialSpawnConfigCount,
                ["initial_spawn_initialized_count"] = initialSpawnInitializedCount,
                ["simulation_active_after_gate"] = gameplayAfterHeadlessGate.SimulationActive != 0,
                ["legacy_rules_phase_after_gate"] = matchAfterHeadlessGate.Phase.ToString() },
            ["original_rules_owner"] = new JObject { ["source_owner_invoked"] = true,
                ["required_queries_present"] = rulesState.RequiredQueriesPresent,
                ["same_retained_entity_manager"] = true,
                ["phase_before_gate"] = matchAfterRules.Phase.ToString(),
                ["phase_after_owner"] = matchAfterHeadlessGate.Phase.ToString(),
                ["play_requested"] = gameplayAfterHeadlessGate.PlayRequested != 0,
                ["simulation_active"] = gameplayAfterHeadlessGate.SimulationActive != 0,
                ["outcome"] = matchAfterHeadlessGate.Outcome.ToString(),
                ["gate_admission_applied"] = sharedLoadingGatePassed },
            ["initial_unit_spawn"] = new JObject { ["original_shared_spawner_executed"] = true,
                ["actual_units_materialized"] = newlySpawned.Count, ["faction_one"] = factionOneUnits,
                ["faction_two"] = factionTwoUnits, ["frames"] = spawnFrames,
                ["original_completion_component_observed"] = em.HasComponent<InitialUnitsSpawnInitialized>(plan),
                ["same_autonomous_world_host_retained"] = autonomousWorld.InitializationStage == "S073LegacyLoadingGateAdmitted_SchedulerCompositionPending" },
            ["original_ai_startup"] = new JObject { ["source_assets_sha256"] = new JObject {
                    ["enemy"] = args[27], ["player"] = args[28], ["plan"] = args[29] },
                ["original_owner_invoked"] = true, ["same_entity_manager"] = true,
                ["controller_count"] = s073AIConfigs.Count, ["build_plan_count"] = 2,
                ["production_plan_count"] = 2, ["has_player_auto_mode"] = aiStartupResult.HasPlayerAutoMode,
                ["player_auto_mode_enabled"] = aiStartupResult.PlayerAutoModeEnabled,
                ["spawn_cells_resolved_from_original_legacy_data"] = true },
            ["autonomous_host_stage"] = autonomousWorld.InitializationStage,
            ["mission_world_scope"] = "S073 original legacy startup and complete captured-map placement owners with shared loading-gate admission; legacy scheduler composition, autonomous ARIA, full mission outcome and player readiness remain pending",
            ["full_mission"] = false, ["autonomous_aria"] = false, ["mission_outcome"] = false, ["mission_ready"] = false,
            ["native_visual_readiness"] = false, ["unity_player_launched"] = false };
    }

    static JObject ReplayOriginalMapPlacementOrigins(JObject capture, GridConfig grid, IReadOnlyList<RuntimeDefinition> runtimeDefinitions)
    {
        var definitions = capture["definitions"] as JArray;
        var rows = capture["placement_rows"] as JArray;
        Need(definitions?.Count == 23 && rows?.Count == 432 && runtimeDefinitions?.Count == definitions.Count,
             "Complete captured S073 source map definition and placement domain required");
        var byPath = new Dictionary<string, JObject>(StringComparer.Ordinal);
        foreach (JObject definition in definitions)
        {
            JObject prefab = (JObject)definition["prefab"];
            string path = (string)prefab["path"];
            Need((string)prefab["codec"] == "source-asset-reference-v1" && !string.IsNullOrEmpty(path) &&
                 Guid.TryParseExact((string)prefab["guid"], "N", out _) && (long?)prefab["local_id"] > 0 &&
                 !byPath.ContainsKey(path), "Unique exact source prefab identity required for captured map definition");
            byPath.Add(path, definition);
        }

        var origins = new JArray();
        int wallRows = 0, rotatedRows = 0, geometricRows = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            JObject row = (JObject)rows[i];
            JObject prefab = (JObject)row["prefab"];
            Need((int?)row["row_index"] == i, "Captured map row order mismatch");
            Need(byPath.TryGetValue((string)prefab["path"], out var definition), "Captured map row has unknown source prefab path");
            Need(JToken.DeepEquals(prefab, definition["prefab"]), "Captured map row prefab identity mismatch");
            JObject fields = (JObject)definition["fields"];
            int prefabIndex = (int?)row["prefab_index"] ?? -1;
            Need(prefabIndex >= 0 && prefabIndex < runtimeDefinitions.Count &&
                 runtimeDefinitions[prefabIndex].Prefab.name == (string)prefab["key"],
                 "Captured map placement did not resolve to its loaded runtime definition");
            RuntimeDefinition runtimeDefinition = runtimeDefinitions[prefabIndex];
            bool isWall = runtimeDefinition.IsWall;
            bool rotateVertical = (bool?)row["rotate_vertical"] == true;
            if (isWall) wallRows++;
            int2 resolvedFootprint = MapBuildingPlacementOriginKernel.ResolvePlacementFootprint(
                runtimeDefinition.FootprintCells.x,
                runtimeDefinition.FootprintCells.y,
                runtimeDefinition.IsWall,
                runtimeDefinition.LocalBounds.size.x,
                runtimeDefinition.LocalBounds.size.z,
                rotateVertical);
            int width = resolvedFootprint.x, height = resolvedFootprint.y;
            if (rotateVertical)
            {
                rotatedRows++;
            }
            Need(width > 0 && height > 0 && width <= grid.Width && height <= grid.Height,
                 "Captured source footprint outside the selected Campaign grid domain");
            float3 world = new float3((float)row["world_center"]!["x"], (float)row["world_center"]!["y"], (float)row["world_center"]!["z"]);
            int2 center = GridUtils.WorldToCell(grid, world);
            bool geometric = (bool?)row["use_geometric_footprint_center"] == true;
            int2 origin = MapBuildingPlacementOriginKernel.ResolveOriginCell(
                center, new Vector2Int(width, height), grid, geometric);
            if (geometric) geometricRows++;
            origins.Add(new JObject { ["row_index"] = i, ["category"] = (string)row["category"],
                ["faction_id"] = (int)row["faction_id"], ["center_cell"] = new JArray(center.x, center.y),
                ["origin"] = new JArray(origin.x, origin.y), ["footprint"] = new JArray(width, height),
                ["rotate_vertical"] = rotateVertical, ["source_path"] = (string)row["source_path"] });
        }
        return new JObject { ["authority"] = "ExactCapturedSourceDefinitions plus shared footprint kernel with the exact original linear-wall predicate (definition.IsWall) and local-bounds inputs, GridUtils.WorldToCell, and shared MapBuildingPlacementOriginKernel.CenterCellToOrigin",
            ["source_rows"] = rows.Count, ["source_definitions"] = definitions.Count, ["rows_resolved"] = origins.Count,
            ["wall_rows"] = wallRows, ["rotated_rows"] = rotatedRows, ["geometric_center_rows"] = geometricRows,
            ["runtime_placements_processed"] = false, ["actors_registered"] = false, ["origins"] = origins };
    }

    public static int Main(string[] args)
    {
        JObject result;
        try
        {
            if (args.Length == 4 && args[0] == "--map-origin-replay")
            {
                string sourceRoot = Environment.GetEnvironmentVariable("WARLINE_S073_SOURCE_ROOT");
                if (string.IsNullOrEmpty(sourceRoot)) throw new InvalidDataException("Pinned S073 source root required");
                string mapPath = Path.Combine(sourceRoot, "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset");
                Need(Sha(mapPath) == args[3] && args[3] == "b145eae26821fa3ee559de12473354813c83d0a4c1997159086c20654ea0112a",
                     "Current selected S073 map definition changed");
                OperationMapDefinition selected = ReadSelectedMap(mapPath);
                SourceS073LegacySpawnRequestAdapter.RequireSharedPlacementSource(selected);
                JObject capture = JObject.Parse(File.ReadAllText(Path.GetFullPath(args[1])));
                Need(Sha(Path.GetFullPath(args[1])) == args[2] && args[2] == "a0107fcdaaf7ace274345b4615292134ec5e8547b456dffdb3c6b857dc72ae73" &&
                     (int?)capture["schema_version"] == 1 && (string)capture["kind"] == "S073OriginalMapBuildingReferences" &&
                     (string)capture["operation_map_id"] == selected.OperationMapId &&
                     (string)capture["source_identity_hash"] == selected.SourceIdentityHash &&
                     (string)capture["physical_source_binding"]?["operation_map_id"] == selected.SourceBinding.SourceOperationMapId &&
                     (string)capture["placement_config"]?["path"] == "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01_BuildingPlacements.asset" &&
                     (string)capture["placement_config"]?["guid"] == "a84d1564be130447197d5a5f2a7fe11d" &&
                     (int?)capture["placement_config"]?["placement_count"] == 432 &&
                     (capture["unsupported_values"] as JArray)?.Count == 0 &&
                     (bool?)capture["placements_processed"] == false && (bool?)capture["mission_world_allocated"] == false &&
                     (bool?)capture["full_mission_initialized"] == false && (bool?)capture["native_visual_readiness"] == false,
                     "Exact non-world source capture for the S073-bound original placement config required");
                var mapDefinitions = GeneratedBuildingDefinitions.LoadS073MapPlacements(
                    Path.GetFullPath(args[1]), args[2], sourceRoot, new Dictionary<string, Entity>(StringComparer.Ordinal));
                Need(mapDefinitions.Definitions.Count == 23 &&
                     mapDefinitions.Definitions.Select(d => d.Prefab.name).Distinct(StringComparer.Ordinal).Count() == 23 &&
                     mapDefinitions.Definitions.Count(d => d.IsWall) == 2 &&
                     mapDefinitions.Definitions.Count(d => d.HasRunway) == 1 &&
                     mapDefinitions.Definitions.Count(d => !d.IsWall && !d.HasRunway) == 20,
                    "Exact S073 map runtime definition domain must preserve the two wall and one runway gates");
                var metadata = selected.GridMetadata;
                var grid = new GridConfig { Width = metadata.Dimensions.x, Height = metadata.Dimensions.y,
                    CellSize = metadata.CellSize, Origin = new float3(metadata.Origin.x, metadata.Origin.y, metadata.Origin.z) };
                var replay = ReplayOriginalMapPlacementOrigins(capture, grid, mapDefinitions.Definitions);
                result = new JObject { ["kind"] = "S073MapPlacementOriginReplay", ["status"] = "Passed",
                    ["mission"] = "S073", ["source_identity_hash"] = selected.SourceIdentityHash,
                    ["selected_campaign_source"] = selected.SourceBinding.SourceOperationMapId,
                    ["map_definition_sha256"] = args[3], ["capture_sha256"] = args[2], ["replay"] = replay,
                    ["map_runtime_definitions"] = new JObject { ["count"] = mapDefinitions.Definitions.Count,
                        ["source_identity_count"] = mapDefinitions.AssetIdentities.Count,
                        ["ordinary_ground_count"] = mapDefinitions.Definitions.Count(d => !d.IsWall && !d.HasRunway),
                        ["wall_count"] = mapDefinitions.Definitions.Count(d => d.IsWall),
                        ["runway_count"] = mapDefinitions.Definitions.Count(d => d.HasRunway),
                        ["registration_executed"] = false },
                    ["world_allocated"] = false, ["runtime_placements_processed"] = false,
                    ["actors_registered"] = false, ["mission_initialized"] = false };
            }
            else result = Run(args);
        }
        catch (Exception error) { result = new JObject { ["kind"] = "S073LegacyPlacementOwnerHarness", ["status"] = "Failed", ["error"] = error.ToString(), ["full_mission"] = false, ["mission_ready"] = false, ["unity_player_launched"] = false }; }
        string marker = (string)result["kind"] == "S073MapPlacementOriginReplay" ? "[S073MapPlacementOriginReplay] " : "[S073LegacyPlacementOwnerHarness] ";
        Console.WriteLine(marker + result.ToString(Newtonsoft.Json.Formatting.None));
        return (string)result["status"] == "Passed" ? 0 : 1;
    }
}
