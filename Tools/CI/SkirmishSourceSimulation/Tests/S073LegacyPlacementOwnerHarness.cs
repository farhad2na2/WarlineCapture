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
using UnityEngine;
using UnityEngine.AddressableAssets;

internal static class S073LegacyPlacementOwnerHarness
{
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
        Need(args.Length == 25, "Expected source templates, S073 configs, Campaign inputs and external placement-candidate descriptor");
        string initialTemplatePath = Path.GetFullPath(args[0]), templatePath = Path.GetFullPath(args[2]), identityPath = Path.GetFullPath(args[4]);
        string legacyPath = Path.GetFullPath(args[6]), definitionsPath = Path.GetFullPath(args[8]);
        string sourceRoot = Path.GetFullPath(args[10]);
        string mapPath = Path.Combine(sourceRoot, "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset");
        string graphPath = Path.GetFullPath(args[11]), initialPath = Path.GetFullPath(args[13]);
        string physicalPath = Path.GetFullPath(args[15]), waterPath = Path.GetFullPath(args[17]), startupPath = Path.GetFullPath(args[19]);
        string roadOutput = Path.GetFullPath(args[22]); Directory.CreateDirectory(roadOutput);
        JObject initialTemplate = JObject.Parse(File.ReadAllText(initialTemplatePath));
        JObject template = JObject.Parse(File.ReadAllText(templatePath));
        JObject legacy = JObject.Parse(File.ReadAllText(legacyPath));
        JObject graph = JObject.Parse(File.ReadAllText(graphPath));
        JObject initialization = JObject.Parse(File.ReadAllText(initialPath));
        Need(Sha(initialTemplatePath) == args[1] && Sha(templatePath) == args[3] && Sha(identityPath) == args[5] && Sha(legacyPath) == args[7] &&
             Sha(definitionsPath) == args[9] && Sha(graphPath) == args[12] && Sha(initialPath) == args[14] &&
             Sha(physicalPath) == args[16] && Sha(waterPath) == args[18] && Sha(startupPath) == args[20],
             "A pinned S073/Campaign owner input changed");
        Need((string)legacy["kind"] == "S073LegacyInitialSpawnExternalData" && (string)legacy["mission"] == "S073", "Actual S073 legacy config required");
        Need(args[5] == Required(legacy, "source_artifacts.name_readback_sha256"), "S073 source identity readback changed");
        Need((string)initialization["kind"] == "GeneratedMissionInitialization" && (string)initialization["mission_id"] == "S002", "Pinned shared Campaign asset graph source required");
        Need(Sha(mapPath) == args[21], "Current logical S073 OperationMapDefinition changed");
        var expectedInitialRoots = legacy["captured_unit_prefab_roots"] as JObject ?? throw new InvalidDataException("S073 initial prefab key set required");
        Need(JToken.DeepEquals(expectedInitialRoots, initialTemplate["opening_prefabs"]) &&
             (string)initialTemplate["kind"] == "AuthoredSkirmishPrefabReferenceData" &&
             (string)initialTemplate["authority"] == "ActualSourceAssetsAndNativeBakers_NotMissionState" &&
             (string)initialTemplate["opening_prefabs"]?["Unit_Chr_Soldier_Male_02_Alt_04"] != null,
             "Exact four-root S073 initial source capture required");
        Need(JToken.DeepEquals(expectedInitialRoots, new JObject(expectedInitialRoots.Properties().Select(p => new JProperty(p.Name, template["opening_prefabs"]?[p.Name])))),
             "The production source capture changed an initial S073 prefab root");
        Need(Sha(identityPath) == args[5] && (string)JObject.Parse(File.ReadAllText(identityPath))["prefab_capture_sha256"] == args[1],
             "Exact S073 initial prefab identity readback lineage required");
        OperationMapDefinition selected = ReadSelectedMap(mapPath);
        SourceS073LegacySpawnRequestAdapter.RequireSharedPlacementSource(selected);

        // Reject source-bound placement candidates that fit the grid but cross the
        // selected logical map's authored playable rectangle. Do this before the
        // expensive Campaign surface/materialization path so failed candidates are cheap.
        string placementConfigPath = Path.GetFullPath(args[23]);
        var placementConfig = JObject.Parse(File.ReadAllText(placementConfigPath));
        Need(Sha(placementConfigPath) == args[24] && (int?)placementConfig["schema_version"] == 1 &&
             (string)placementConfig["kind"] == "S073LegacyBuildingOriginsCandidateV1" &&
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

        var em = new EntityManager { LogicalAllocatorAdmitted = true };
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
        using var roadOwner = new SourceFreshInitialRoadOwner(roadRun);
        roadOwner.Sync(em, gridEntity); roadOwner.RequireBeforePath();

        var initialData = legacy["initial_spawn"].ToObject<LegacyInitialSpawnData>();
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
            ScenarioIndex = 3, Phase = SkirmishPhase.Playing, StartupFailure = SkirmishStartupFailureCode.None, Outcome = SkirmishOutcome.None, Reason = SkirmishEndReason.None });
        Entity boundary = em.CreateEntity(); em.AddComponentData(boundary, new BuildingRuntimeStateTag());
        var requests = em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        var definitions = GeneratedBuildingDefinitions.LoadS073Legacy(definitionsPath, args[9], templates, legacy, sourceRoot);
        Need(definitions.Definitions.Count == 6, "Exact S073 original building definitions required");
        var configured = em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
        foreach (var d in definitions.Definitions) configured.Add(new BuildingConfiguredSpawnableReadModel { BuildingId = new FixedString128Bytes(d.Prefab.name), FootprintCells = new int2(d.FootprintCells.x, d.FootprintCells.y) });

        var domain = SourceS073LegacySpawnRequestAdapter.LoadSharedPlacementSource(selected, physicalPath, args[16], waterPath, args[18], startupPath, args[20], sourceRoot);
        var masks = domain.Build(gridData.Width, gridData.Height, gridData.CellSize, gridData.Origin.x, gridData.Origin.y, gridData.Origin.z);
        var roads = new bool[checked(gridData.Width * gridData.Height)]; BuildingPlacementRoadSurfaceMask.Append(em, surfaceEntity, em.GetComponentData<GridConfig>(gridEntity), roads);
        if (domain.UsesOriginalOwnerMaskCapture) roads = domain.CopyOriginalOwnerRoadMask();
        var water = domain.BuildWaterMask(true, gridData.Width, gridData.Height, gridData.CellSize, gridData.Origin.x, gridData.Origin.y, gridData.Origin.z);
        var grid = em.GetComponentData<GridConfig>(gridEntity);
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
        Need(Sha(placementConfigPath) == args[24] && (int?)placementConfig["schema_version"] == 1 &&
             (string)placementConfig["kind"] == "S073LegacyBuildingOriginsCandidateV1" &&
             (string)placementConfig["authority"] == "DerivedByOriginalBuildingPlacementOwner_ValidationCandidateOnly" &&
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
        bool everyCandidateFound = true;
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
            Need(sourceOverride != null && (int?)sourceOverride["request_index"] == i &&
                 (int?)sourceOverride["faction_id"] == entry.FactionId &&
                 (string)sourceOverride["building_id"] == key &&
                 (string)sourceOverride["search_policy"] == "BuildingRuntimeSpawnCompositionSystemHelper.TryFindValidInitialBuildingOrigin" &&
                 (bool?)sourceOverride["original_owner_search_found"] == true &&
                 original?.Count == 2 && (int?)original[0] == originalOrigin.x && (int?)original[1] == originalOrigin.y &&
                 candidate?.Count == 2 && footprint?.Count == 2 &&
                 (int?)footprint[0] == definition.FootprintCells.x && (int?)footprint[1] == definition.FootprintCells.y,
                 "External S073 origin row differs from current legacy plan or source definition");
            var candidateOrigin = new Vector2Int((int)candidate[0], (int)candidate[1]);
            placementCandidates.Add(new JObject { ["request_index"] = i, ["faction_id"] = entry.FactionId,
                ["building_id"] = key, ["original_origin"] = new JArray(originalOrigin.x, originalOrigin.y),
                ["footprint"] = new JArray(definition.FootprintCells.x, definition.FootprintCells.y),
                ["original_owner_search_found"] = true,
                ["candidate_origin"] = new JArray(candidateOrigin.x, candidateOrigin.y),
                ["source_config_sha256"] = args[24] });
            entry.OriginOffset = new int2(candidateOrigin.x - factionSpawn.x, candidateOrigin.y - factionSpawn.y);
            initialBuildings[i] = entry;
        }
        int queued = SourceS073LegacySpawnRequestAdapter.Queue(em, plan, boundary, true);
        Need(queued == 10 && requests.Length == 10, "Actual S073 source rows were not queued");
        string constructionPath = Path.Combine(sourceRoot, "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset");
        string constructionText = File.ReadAllText(constructionPath);
        var planeMatch = Regex.Match(constructionText, @"(?m)^  buildPlaneY: ([^\r\n]+)$");
        if (!planeMatch.Success || !float.TryParse(planeMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float buildPlaneY))
            throw new InvalidDataException("Actual S073 build plane required");
        var registry = new Dictionary<int, RuntimeBuildingEntity>();
        var pipeline = new GeneratedStartingBuildingPipeline(em, gridEntity, boundary, definitions.Definitions, true, registry,
            masks.AuthoredBuildings, roads, masks.Sidewalks, water, buildPlaneY, RuntimeBlocked);
        int frames = 0;
        for (; frames < 64; frames++)
        {
            pipeline.ProcessRequests((frames + 1) / 60f, forceScan: frames == 0);
            requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            bool pending = false;
            for (int i = 0; i < requests.Length; i++) if (requests[i].Status == BuildingRuntimeSpawnRequest.Pending) { pending = true; break; }
            if (!pending) { frames++; break; }
        }
        requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
        var rows = new JArray(requests.Select(r => new JObject { ["request_id"] = r.RequestId, ["building_id"] = r.BuildingId.ToString(),
            ["faction_id"] = r.FactionId, ["preferred_origin"] = new JArray(r.PreferredOrigin.x, r.PreferredOrigin.y), ["status"] = r.Status.ToString(),
            ["result_code"] = r.ResultCode, ["building_runtime_id"] = r.BuildingRuntimeId, ["actual_origin"] = new JArray(r.ActualOrigin.x, r.ActualOrigin.y),
            ["actual_footprint"] = new JArray(r.ActualFootprint.x, r.ActualFootprint.y) }));
        var buildings = new JArray(registry.OrderBy(pair => pair.Key).Select(pair => new JObject { ["runtime_id"] = pair.Key,
            ["definition"] = pair.Value.Definition.Prefab.name, ["origin"] = new JArray(pair.Value.OriginCell.x, pair.Value.OriginCell.y),
            ["combat_entity"] = pair.Value.CombatEntity.Index + ":" + pair.Value.CombatEntity.Version,
            ["blocker_entity"] = pair.Value.BlockerEntity.Index + ":" + pair.Value.BlockerEntity.Version }));
        roadOwner.Complete();
        bool allSucceeded = requests.Length == 10 && registry.Count == 10;
        for (int i = 0; i < requests.Length; i++) allSucceeded &= requests[i].Status == BuildingRuntimeSpawnRequest.Succeeded && requests[i].BuildingRuntimeId > 0;
        var surfaceComponent = em.GetComponentData<MapSurfaceComponent>(surfaceEntity);
        int surfaceCount = MapSurfaceBlobAccess.SurfaceCount(ref surfaceComponent.SurfaceBlob.Value);
        return new JObject { ["kind"] = "S073LegacyPlacementOwnerHarness", ["status"] = allSucceeded ? "Passed" : "Failed",
            ["mission"] = "S073", ["source_identity_hash"] = selected.SourceIdentityHash, ["campaign_source"] = selected.SourceBinding.SourceOperationMapId,
            ["placement_config_variant"] = new JObject { ["kind"] = (string)placementConfig["kind"],
                ["path"] = placementConfigPath, ["sha256"] = args[24],
                ["derived_from_legacy_sha256"] = args[7],
                ["derived_from_placement_receipt_sha256"] = (string)placementConfig["derived_from_placement_receipt_sha256"],
                ["loaded_as_external_config"] = true,
                ["all_candidate_origins_found"] = everyCandidateFound, ["persisted_to_project"] = false,
                ["overrides"] = placementCandidates, ["changes_campaign_geometry"] = false,
                ["changes_shared_spawn_rule"] = false, ["changes_resources_or_budgets"] = false },
            ["grid"] = new JObject { ["width"] = grid.Width, ["height"] = grid.Height, ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z), ["cell_size"] = grid.CellSize },
            ["surface_samples"] = surfaceCount,
            ["authored_navigation_rows"] = authoredNavigation, ["initial_road_owner"] = roadOwner.Receipt(), ["requests_queued"] = queued,
            ["processing_frames"] = frames, ["request_results"] = rows, ["registered_buildings"] = buildings,
            ["building_count"] = registry.Count, ["all_ten_actual_requests_succeeded"] = allSucceeded,
            ["mission_world_scope"] = "S073 mission-specific initial-origin candidate derived from actual LegacyInitialSpawnData; same manager, source grid/surface/masks and shared runtime spawn owner",
            ["full_mission"] = false, ["autonomous_aria"] = false, ["mission_outcome"] = false, ["mission_ready"] = false,
            ["native_visual_readiness"] = false, ["unity_player_launched"] = false };
    }

    public static int Main(string[] args)
    {
        JObject result;
        try { result = Run(args); }
        catch (Exception error) { result = new JObject { ["kind"] = "S073LegacyPlacementOwnerHarness", ["status"] = "Failed", ["error"] = error.ToString(), ["full_mission"] = false, ["mission_ready"] = false, ["unity_player_launched"] = false }; }
        Console.WriteLine("[S073LegacyPlacementOwnerHarness] " + result.ToString(Newtonsoft.Json.Formatting.None));
        return (string)result["status"] == "Passed" ? 0 : 1;
    }
}
