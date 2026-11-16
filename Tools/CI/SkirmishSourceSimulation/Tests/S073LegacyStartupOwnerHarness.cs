using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Game.Composition;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Simulation;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static class S073LegacyStartupOwnerHarness
{
    static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static string Sha256(string path)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    static void RequireCurrentLegacyAssets(JObject source, string repoRoot)
    {
        var pins = source["source_artifacts"]?["configuration_asset_sha256"] as JObject ??
            throw new InvalidDataException("Current S073 legacy asset pins required");
        string root = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (var pin in pins.Properties())
        {
            if (Path.IsPathRooted(pin.Name) || pin.Name.Split('/').Any(part => part == ".." || part == ""))
                throw new InvalidDataException("S073 config asset path must remain repository-relative");
            string path = Path.GetFullPath(Path.Combine(root, pin.Name.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root, StringComparison.Ordinal) || Sha256(path) != (string)pin.Value)
                throw new InvalidDataException("S073 original legacy configuration changed: " + pin.Name);
        }
    }

    static void RequireSourcePrefabIdentityReadback(JObject source, JObject identity, JObject snapshot, string snapshotPath, string identityPath, string assetRepoRoot)
    {
        Need((int?)identity["schema_version"] == 1 &&
             (string)identity["kind"] == "S073LegacyPrefabSourceNameReadback" &&
             (string)identity["authority"] == "AssetDatabase exact GUID/local-fileID GameObject names; source data only" &&
             (string)identity["mission"] == "S073" &&
             identity["unity_world_allocated"]?.Value<bool>() == false &&
             identity["mission_world_allocated"]?.Value<bool>() == false &&
             identity["mission_initialized"]?.Value<bool>() == false &&
             identity["native_visual_readiness"]?.Value<bool>() == false,
             "Exact source-only S073 prefab identity readback required");
        Need((string)identity["prefab_capture_sha256"] == Sha256(snapshotPath),
             "S073 source-name readback must refer to the pinned prefab capture");
        Need(((string)source["source_artifacts"]?["config_readback_sha256"])?.Length == 64 &&
             (string)identity["source_readback_sha256"] == (string)source["source_artifacts"]?["source_yaml_readback_sha256"] &&
             ((string)identity["source_readback_sha256"])?.Length == 64 &&
             (string)identity["prefab_capture_sha256"] == (string)source["source_artifacts"]?["prefab_capture_sha256"] &&
             Sha256(identityPath) ==
                 (string)source["source_artifacts"]?["name_readback_sha256"],
             "S073 source config readback lineage mismatch");

        var expectedRoots = source["captured_unit_prefab_roots"] as JObject ??
            throw new InvalidDataException("Captured S073 prefab roots required");
        var keys = (source["initial_spawn"]?["Factions"] as JArray ?? throw new InvalidDataException("S073 factions required"))
            .OfType<JObject>().SelectMany(f => (f["Units"] as JArray ?? throw new InvalidDataException("S073 units required"))
                .OfType<JObject>().Select(u => (string)u["SourceKey"]))
            .Concat((source["initial_spawn"]?["UnitPrefabRegistryKeys"] as JArray ?? new JArray()).Values<string>())
            .Distinct(StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToArray();
        Need(keys.Length == 4 && expectedRoots.Properties().Select(p => p.Name).OrderBy(key => key, StringComparer.Ordinal)
            .SequenceEqual(keys), "Exact four S073 unit prefab source keys required");

        var rows = (identity["objects"] as JArray ?? throw new InvalidDataException("S073 source object rows required"))
            .OfType<JObject>().ToArray();
        string root = Path.GetFullPath(assetRepoRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (string key in keys)
        {
            var matches = rows.Where(row => (string)row["unit_source_key"] == key).ToArray();
            Need(matches.Length == 1, "Unique audited S073 source asset identity required for " + key);
            JObject row = matches[0];
            var sourcePins = identity["source_pins"] as JObject ?? throw new InvalidDataException("Pinned S073 identity inputs required");
            string assetPath = (string)row["asset_path"], metaPath = (string)row["meta_path"];
            Need(assetPath != null && metaPath != null && sourcePins[assetPath]?.Type == JTokenType.String &&
                 sourcePins[metaPath]?.Type == JTokenType.String &&
                 sourcePins[assetPath].Value<string>() == (string)row["asset_sha256"] &&
                 sourcePins[metaPath].Value<string>() == (string)row["meta_sha256"],
                 "S073 asset identity must be pinned to its prefab and .meta sources: " + key);
            foreach (string path in new[] { assetPath, metaPath })
            {
                string fullPath = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
                Need(fullPath.StartsWith(root, StringComparison.Ordinal) && Sha256(fullPath) == (string)sourcePins[path],
                     "Current S073 prefab identity source pin changed: " + path);
            }
            Need((string)row["game_object_name"] == key &&
                 ((string)row["guid"])?.Length == 32 && row["file_id"]?.Type == JTokenType.Integer &&
                 ((string)row["asset_path"])?.EndsWith(".prefab", StringComparison.Ordinal) == true &&
                 (row["uses"] as JArray)?.OfType<JObject>().Any(use =>
                     ((string)use["source_path"] == "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset" &&
                      ((string)use["field_path"])?.Contains(".units[") == true && (long?)use["file_id"] == (long?)row["file_id"]) ||
                     ((string)use["source_path"] == "Assets/Game/Configs/Skirmish/UnitRegistry.asset" &&
                      ((string)use["field_path"])?.Contains(".unitSpawnPrefabs[") == true && (long?)use["file_id"] == (long?)row["file_id"])) == true,
                 "S073 unit source reference must resolve to the exact authored prefab identity: " + key);
            Need((string)expectedRoots[key] == (string)snapshot["opening_prefabs"]?[key],
                 "S073 authored source key must resolve to the pinned converted prefab root: " + key);
        }
    }

    static void VerifyLegacyPresetProviderBoundary(EntityManager em, Entity plan,
        LegacyInitialSpawnData source, int reinforcementTarget, int attackSquadSize)
    {
        SkirmishPresetRuntimeProjection.BindValidatedSourceReadback(
            em, source, reinforcementTarget, attackSquadSize);
        try
        {
        var fallback = new InitialUnitsSpawnConfig { InitialMaterials = -71, MaterialsCapacity = -71 };
        var nativeProjection = SkirmishPresetRuntimeProjection.FromNativePreset(
            fallback, Array.Empty<InitialUnitsFactionSpawnEntry>(), 0, 99);
        Need(nativeProjection.ReinforcementInfantryTarget == 0 && nativeProjection.AttackSquadSize == 99,
            "Native preset projection must retain out-of-range source scalars for the original consumers to handle");
        var noMatch = SkirmishPresetRuntimeProjection.ResolveLegacyInitialOwnerValues(em, fallback);
        Need(noMatch.InitialMaterials == fallback.InitialMaterials && noMatch.MaterialsCapacity == fallback.MaterialsCapacity,
            "Zero-match native fallback behavior changed");

        Entity first = em.CreateEntity();
        Entity second = em.CreateEntity();
        em.AddComponentData(first, new SkirmishMatchState { ScenarioIndex = 3, Phase = SkirmishPhase.Preparing });
        em.AddComponentData(second, new SkirmishMatchState { ScenarioIndex = 3, Phase = SkirmishPhase.Preparing });
        var multipleMatch = SkirmishPresetRuntimeProjection.ResolveLegacyInitialOwnerValues(em, fallback);
        Need(multipleMatch.InitialMaterials == fallback.InitialMaterials,
            "Multiple-match native fallback behavior changed");
        em.DestroyEntity(second);

        var original = em.GetComponentData<InitialUnitsSpawnConfig>(plan);
        InitialUnitsFactionSpawnEntry[] originalRows;
        using (var rows = em.GetBuffer<InitialUnitsFactionSpawnEntry>(plan).ToNativeArray(Allocator.Temp))
            originalRows = rows.ToArray();
        try
        {
            var divergentConfig = original;
            divergentConfig.InitialMaterials = 9999;
            divergentConfig.MaterialsCapacity = 9999;
            divergentConfig.InitialAiMaterials = 9999;
            divergentConfig.AiMaterialsCapacity = 9999;
            em.SetComponentData(plan, divergentConfig);
            var rows = em.GetBuffer<InitialUnitsFactionSpawnEntry>(plan);
            rows.Clear();
            rows.Add(new InitialUnitsFactionSpawnEntry { FactionId = 2, SpawnCell = new int2(1, 2) });
            var divergent = SkirmishPresetRuntimeProjection.ResolveLegacyInitialOwnerValues(em, fallback);
            Need(divergent.InitialMaterials == source.InitialMaterials &&
                 divergent.MaterialsCapacity == source.MaterialsCapacity &&
                 divergent.InitialAiMaterials == source.InitialAiMaterials &&
                 divergent.AiMaterialsCapacity == source.AiMaterialsCapacity,
                "Pinned source preset must remain authoritative when a valid ECS plan diverges");

            var playing = em.GetComponentData<SkirmishMatchState>(first);
            playing.Phase = SkirmishPhase.Playing;
            em.SetComponentData(first, playing);
            var phaseFallback = SkirmishPresetRuntimeProjection.ResolveLegacyInitialOwnerValues(em, fallback);
            Need(phaseFallback.InitialMaterials == fallback.InitialMaterials,
                "Playing-phase original fallback behavior changed");
        }
        finally
        {
            em.SetComponentData(plan, original);
            var rows = em.GetBuffer<InitialUnitsFactionSpawnEntry>(plan);
            rows.Clear();
            foreach (var row in originalRows) rows.Add(row);
            em.DestroyEntity(first);
        }
        Console.WriteLine("[S073LegacyPresetProviderControls] result=Passed sourceAuthority=PinnedReadback divergentPlan=RejectedAsAuthority zeroMatches=Fallback multipleMatches=Fallback playingPhase=Fallback");
        }
        finally
        {
            SkirmishPresetRuntimeProjection.ReleaseBoundSourceReadback(em);
        }
    }

    static JObject ExecuteOriginalBuildingPlacement(EntityManager em, Entity boundary,
        IReadOnlyList<RuntimeDefinition> definitions, Dictionary<int, RuntimeBuildingEntity> buildings,
        LegacyInitialSpawnData data, Entity plan, AutonomousWorld sourceHost, string repo)
    {
        string selectedPath = Path.Combine(repo, "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset");
        string selectedSha = Environment.GetEnvironmentVariable("WARLINE_S073_OPERATION_MAP_SHA256");
        Need(selectedSha != null && selectedSha.Length == 64 && Sha256(selectedPath) == selectedSha,
            "Pinned current S073 OperationMapDefinition required for building placement");
        string graphPath = Environment.GetEnvironmentVariable("WARLINE_S073_SURFACE_GRAPH_PATH");
        string graphSha = Environment.GetEnvironmentVariable("WARLINE_S073_SURFACE_GRAPH_SHA256");
        Need(!string.IsNullOrEmpty(graphPath) && graphSha != null && graphSha.Length == 64 && Sha256(graphPath) == graphSha,
            "Pinned source-authored S073 OperationMap graph required");
        using var graphText = File.OpenText(graphPath);
        using var graphReader = new Newtonsoft.Json.JsonTextReader(graphText) { MaxDepth = 32, DateParseHandling = Newtonsoft.Json.DateParseHandling.None };
        var graph = JObject.Load(graphReader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        var assets = new SourceConfigMaterializer(graph);
        var selected = assets.ByPath<OperationMapDefinition>("Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset");
        Need(selected.OperationMapId == "opmap.skirmish.industrial_basin" && selected.ContentHash == "cbbaa8366cbfb5e0ad9bc6bf70a98b48503df10e3d0c5c45757295af841a7a6d" &&
             selected.SourceIdentityHash == "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900" &&
             selected.SourceBinding.SourceOperationMapId == "opmap.skirmish.desert_base_01" && selected.SourceSceneReference != null,
            "Materialized selected S073 OperationMap source identity changed");
        string sourceSceneGuid = selected.SourceSceneReference.AssetGUID;
        SourceS073LegacySpawnRequestAdapter.RequireSharedPlacementSource(selected);

        string sourceRoot = Path.GetFullPath(repo);
        string physical = Environment.GetEnvironmentVariable("WARLINE_S073_PHYSICAL_PATH");
        string water = Environment.GetEnvironmentVariable("WARLINE_S073_WATER_PATH");
        string startup = Environment.GetEnvironmentVariable("WARLINE_S073_STARTUP_PATH");
        string relocationPath = Environment.GetEnvironmentVariable("WARLINE_S073_CAPTURE_RELOCATION_PATH");
        string relocationSha = Environment.GetEnvironmentVariable("WARLINE_S073_CAPTURE_RELOCATION_SHA256");
        string cachePath = Environment.GetEnvironmentVariable("WARLINE_S073_NATIVE_AUTHORED_MASK_PATH");
        Need(new[] { physical, water, startup, cachePath, relocationPath }.All(path => !string.IsNullOrEmpty(path)) &&
             relocationSha != null && relocationSha.Length == 64,
            "Pinned S073 Campaign placement captures, relocation receipt, and original authored-building cache required");
        var domain = SourceS073LegacySpawnRequestAdapter.LoadSharedPlacementSource(selected,
            physical, Sha256(physical), water, Sha256(water), startup, Sha256(startup), sourceRoot,
            relocationPath, relocationSha);
        bool[] authoredBuildings = SourceS073LegacySpawnRequestAdapter.LoadOriginalAuthoredBuildingCache(selected,
            cachePath, Environment.GetEnvironmentVariable("WARLINE_S073_NATIVE_AUTHORED_MASK_SHA256"), sourceSceneGuid);
        domain = domain.BindValidatedS073NativeAuthoredBuildingMask(selected.OperationMapId,
            selected.SourceIdentityHash, selected.ContentHash, authoredBuildings);
        var geometry = domain.Build(2048, 1024, 1f, 0f, 0f, 0f);
        bool[] roads = domain.CopyOriginalOwnerRoadMask();
        bool[] waterMask = domain.BuildWaterMask(true, 2048, 1024, 1f, 0f, 0f, 0f);
        bool[] surfaceSidewalks = geometry.Sidewalks;
        var gridAsset = assets.ByPath<Game.Configs.GridAuthoringSceneConfigAsset>("Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset");
        var surfaceAsset = assets.ByPath<MapSurfaceDataAsset>("Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset");
        Need(gridAsset.Width == 2048 && gridAsset.Height == 1024 && gridAsset.CellSize == 1f &&
             gridAsset.Origin.x == 0f && gridAsset.Origin.y == 0f && gridAsset.Origin.z == 0f,
            "Original Campaign grid authoring frame differs from selected S073 source");
        if (!surfaceAsset.TryCreateRuntimeBlobAsset(Allocator.Persistent, out var surfaceBlob))
            throw new InvalidDataException("Original Campaign surface decoder rejected selected S073 source");
        Entity grid = Entity.Null, surface = Entity.Null;
        try
        {
            var gridConfig = new GridConfig { Width = gridAsset.Width, Height = gridAsset.Height, CellSize = gridAsset.CellSize,
                Origin = new float3(gridAsset.Origin.x, gridAsset.Origin.y, gridAsset.Origin.z) };
            int cellCount = checked(gridAsset.Width * gridAsset.Height);
            grid = em.CreateEntity();
            GeneratedGridSourceBaking.Bake(em, grid, gridAsset);
            GeneratedFreshGridStorage.Initialize(em, grid);
            sourceHost.SyncS073LegacyDiagnosticRoads(grid);
            surface = em.CreateEntity();
            em.AddComponentData(surface, new MapSurfaceComponent { SurfaceBlob = surfaceBlob,
                GridOrigin = new float3(gridAsset.Origin.x, gridAsset.Origin.y, gridAsset.Origin.z),
                CellSize = gridAsset.CellSize, Dimensions = new int2(gridAsset.Width, gridAsset.Height), HasSurfaceData = 1 });
            bool[] surfaceRoads = new bool[cellCount];
            BuildingPlacementRoadSurfaceMask.Append(em, surface, gridConfig, surfaceRoads);
            for (int i = 0; i < cellCount; i++) roads[i] |= surfaceRoads[i];
            string buildConfigPath = Path.Combine(repo, "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
            string buildConfig = File.ReadAllText(buildConfigPath);
            var planeMatch = Regex.Match(buildConfig, "(?m)^  buildPlaneY: ([^\\r\\n]+)$");
            if (!planeMatch.Success || !float.TryParse(planeMatch.Groups[1].Value,
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float buildPlaneY))
                throw new InvalidDataException("Current authored building placement buildPlaneY required");

            // Let the native InitialUnitsSpawnSystem issue its own requests before
            // the existing generated building pipeline consumes them.
            if (!em.HasComponent<BuildingRuntimeStateTag>(boundary)) em.AddComponentData(boundary, new BuildingRuntimeStateTag());
            if (!em.HasBuffer<BuildingConfiguredSpawnableReadModel>(boundary)) em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
            if (!em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary)) em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
            RuntimeGameplayStartTransition.ResetForGameplayStart(em);
            bool playRequestedByOwner;
            using (var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
                playRequestedByOwner = gameplay.CalculateEntityCount() == 1 &&
                    gameplay.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested != 0;
            Need(playRequestedByOwner, "Actual native gameplay-start owner did not publish PlayRequested");
            var unitOwner = new OriginalS073InitialUnitsSpawnOwner(em);
            JObject unitResult = unitOwner.Step();
            var queuedRows = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            Need(queuedRows.Length == 10, "Original initial-unit owner did not issue all ten starting-building requests");
            int sourceRow = 0;
            foreach (var faction in data.Factions)
            foreach (var building in faction.Buildings)
            {
                var request = queuedRows[sourceRow];
                int expectedX = faction.SpawnX + building.OriginOffsetX;
                int expectedY = faction.SpawnY + building.OriginOffsetY;
                Need(request.RequestId == sourceRow + 1 && request.RequestKind == BuildingRuntimeSpawnRequest.KindBuilding &&
                     request.Status == BuildingRuntimeSpawnRequest.Pending && request.HasOwnerFaction == 1 &&
                     request.RequirePreferredOrigin == 1 && request.FactionId == faction.FactionId &&
                     request.PlanEntity == plan && request.PreferredOrigin.x == expectedX &&
                     request.PreferredOrigin.y == expectedY &&
                     request.BuildingId.ToString() == BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(building.LookupKey) &&
                     GeneratedRuntimeSpawnProcessor.IsLegacyInitialRequest(em, request),
                     "Original owner request differs from source row " + sourceRow);
                sourceRow++;
            }
            Need(sourceRow == 10, "Original owner request accounting changed");
            var result = SourceS073LegacySpawnRequestAdapter.ProcessOriginalLegacyStartingBuildings(em, grid,
                boundary, definitions, buildings, authoredBuildings, roads, surfaceSidewalks, waterMask, buildPlaneY);
            if ((string)result["status"] == "Passed")
            {
                // Run the real blocker owner first so initial unit placement reads
                // the same world’s registered building footprints and baked grid.
                var blockerState = new SystemState { EntityManager = em, Enabled = true };
                var blockerOwner = new StaticGridBlockerUpdateSystem();
                blockerState.Begin();
                blockerOwner.OnCreate(ref blockerState);
                blockerState.End();
                blockerState.Begin();
                if (blockerState.Enabled && blockerState.RequiredQueriesPresent)
                    blockerOwner.OnUpdate(ref blockerState);
                blockerState.End();

                for (int frame = 0; frame < 16 && (string)unitResult["status"] != "Completed"; frame++)
                {
                    unitResult = unitOwner.Step();
                }
                result["unit_spawn_owner"] = unitResult;
                if (Environment.GetEnvironmentVariable("WARLINE_S073_SOURCE_OCCUPANCY_CENSUS") == "1" &&
                    (bool?)unitResult["original_dynamic_occupancy_owner_invoked"] == true)
                {
                    result["source_occupancy_contributor_census"] = SourceS073OccupancyContributorCensus.Capture(em);
                }
                result["play_requested_by_original_gameplay_state_owner"] = playRequestedByOwner;
                result["units_spawned"] = unitResult != null && (int?)unitResult["units_after"] == 24;
                result["initial_spawn_completion_marked"] = unitResult != null && (bool?)unitResult["owner_marked_completion"] == true;
                using (var spawned = em.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                    None = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<Disabled>(),
                        ComponentType.ReadOnly<RuntimeBuildingCombatTag>(), ComponentType.ReadOnly<StaticGridBlocker>() }
                }))
                using (var entities = spawned.ToEntityArray(Allocator.Temp))
                {
                    Need(entities.Length == 24, "Actual S073 source-spawned unit roster required for same-world retention");
                    using (var matches = em.CreateEntityQuery(typeof(SkirmishMatchState)))
                    {
                        Need(matches.CalculateEntityCount() == 1, "Unique actual legacy match identity required for same-world retention");
                        sourceHost.RetainS073LegacyDiagnosticStartup(em, grid, surface,
                            matches.GetSingletonEntity(), boundary, plan, entities.ToArray());
                    }
                }
                sourceHost.CompleteInitialRoadInputs();
                result["same_world_autonomous_host"] = new JObject
                {
                    ["status"] = "RetainedDiagnosticStartup",
                    ["world_id"] = sourceHost.WorldId,
                    ["initialization_stage"] = sourceHost.InitializationStage,
                    ["same_entity_manager"] = true,
                    ["initial_road_projection"] = sourceHost.InitialRoadReceipt
                };
                result["world_initialized"] = false;
            }
            result["source_map_path"] = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset";
            result["source_map_sha256"] = selectedSha;
            result["source_authored_cache_sha256"] = Environment.GetEnvironmentVariable("WARLINE_S073_NATIVE_AUTHORED_MASK_SHA256");
            result["active_water_footprint_count"] = domain.WaterFootprintCount;
            result["world_initialized"] = false;
            return result;
        }
        finally
        {
            if (grid != Entity.Null && em.Exists(grid)) RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage(em, grid);
            if (surface != Entity.Null && em.Exists(surface)) em.DestroyEntity(surface);
            surfaceBlob.Dispose();
        }
    }

    static void Main(string[] args)
    {
        Need(args.Length == 9, "prefab capture, source identity readback, external S073 config, source repository, current prefab asset repository, full template capture/hash, and legacy building definitions/hash required");
        var snapshot = JObject.Parse(File.ReadAllText(args[0]));
        var identity = JObject.Parse(File.ReadAllText(args[1]));
        var source = JObject.Parse(File.ReadAllText(args[2]));
        Need((string)source["kind"] == "S073LegacyInitialSpawnExternalData" && (string)source["mission"] == "S073",
            "Exact external S073 legacy config required");
        Need(Sha256(args[1]) == (string)source["source_artifacts"]?["name_readback_sha256"],
             "S073 external config must pin its source-identity readback");
        RequireCurrentLegacyAssets(source, args[3]);
        RequireSourcePrefabIdentityReadback(source, identity, snapshot, args[0], args[1], args[4]);
        var data = source["initial_spawn"].ToObject<LegacyInitialSpawnData>();
        var presetValues = source["preset_values"] as JObject ??
            throw new InvalidDataException("Source-pinned Skirmish preset values required");
        Need((string)presetValues["asset_path"] == "Assets/Game/Resources/SkirmishIndustrialBasin.asset" &&
             ((string)presetValues["asset_sha256"])?.Length == 64 &&
             (int?)presetValues["reinforcementInfantryTarget"] >= 1 &&
             (int?)presetValues["attackSquadSize"] is >= 4 and <= 8,
             "Exact pinned Industrial Basin preset scalars required");
        string presetAssetPath = Path.Combine(args[3], "Assets/Game/Resources/SkirmishIndustrialBasin.asset");
        Need(Sha256(presetAssetPath) == (string)presetValues["asset_sha256"],
             "External preset values must pin the current source asset bytes");
        var expectedRoots = source["captured_unit_prefab_roots"] as JObject ?? throw new InvalidDataException("Captured S073 prefab roots required");
        Need(JToken.DeepEquals(expectedRoots, snapshot["opening_prefabs"]), "Captured S073 prefab root namespace changed");
        string templateCapturePath = Path.GetFullPath(args[5]);
        Need(Sha256(templateCapturePath) == args[6], "Exact retained full S073 production-template capture required");
        var templateCapture = JObject.Parse(File.ReadAllText(templateCapturePath));
        string[] productionRoots =
        {
            "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_APC_Fast", "Unit_Veh_APC_Heavy", "Unit_Veh_APC_Slow",
            "Unit_Veh_Light_Armored_Car", "Unit_Veh_Missle_Launcher_Air", "Unit_Veh_Missle_Launcher_Ground",
            "Unit_Veh_Radar_Tank", "Unit_Veh_Tank_USA", "Unit_Veh_Truck_Canopy", "Unit_Veh_Truck_Tanker", "Unit_Veh_Truck_Tray"
        };
        Need((string)templateCapture["kind"] == "AuthoredSkirmishPrefabReferenceData" &&
             (string)templateCapture["authority"] == "ActualSourceAssetsAndNativeBakers_NotMissionState" &&
             (templateCapture["opening_prefabs"] as JObject)?.Count == productionRoots.Length &&
             productionRoots.All(key => templateCapture["opening_prefabs"]?[key] != null),
             "Retained native source capture must include all 12 exact legacy production roots");

        var em = new EntityManager();
        int nextIdentity = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = nextIdentity++, Version = 1 };
        var buildings = new Dictionary<int, RuntimeBuildingEntity>();
        using var sourceHost = new AutonomousWorld(em, buildings);
        string hostOutput = Environment.GetEnvironmentVariable("WARLINE_S073_HOST_OUTPUT");
        Need(!string.IsNullOrWhiteSpace(hostOutput) && Directory.Exists(hostOutput),
            "Fresh harness output required for actual initial-road source receipts");
        string matchScenePath = Path.Combine(args[3], "Assets/Game/Scenes/Match.unity");
        sourceHost.PrepareFreshInitialRoads(new JObject
        {
            ["kind"] = "SkirmishSourceSimulationRun",
            ["schema_version"] = 1,
            ["repository_root"] = Path.GetFullPath(args[3]),
            ["source_repository_root"] = Path.GetFullPath(args[3]),
            ["output_directory"] = Path.GetFullPath(hostOutput),
            ["mission_scene"] = new JObject { ["path"] = matchScenePath, ["sha256"] = Sha256(matchScenePath) }
        });
        var keys = data.UnitPrefabRegistryKeys.Concat(data.Factions.SelectMany(f => f.Units).Select(u => u.SourceKey))
            .Distinct(StringComparer.Ordinal).ToArray();
        var imported = GeneratedReferenceTemplateImporter.Import(em, snapshot, Path.GetDirectoryName(args[0]), keys);
        Need(imported.Prefabs.Count == 4 && imported.ImportedEntities == 140 && imported.ImportedComponents == 862 &&
             imported.OmittedPresentationComponents == 4, "Actual pinned S073 prefab closure import changed");
        var startup = SkirmishLegacyS073StartupOwner.InitializeFromSource(
            em, data, imported.Prefabs,
            (int)presetValues["reinforcementInfantryTarget"], (int)presetValues["attackSquadSize"]);
        var initialized = startup.LegacyOwner;
        var runtimePreset = startup.Preset;
        Need(initialized.Initialized && initialized.FactionCount == 2 && initialized.UnitRosterCount == 3 &&
             initialized.InitialUnitEntryCount == 10 && initialized.InitialBuildingEntryCount == 10 &&
             initialized.UnitRegistryEntryCount == 3 && initialized.VisualEntryCount == 3 &&
             initialized.MissingVisualReferenceCount == 2, "Original legacy startup owner result changed");
        Need(runtimePreset.InitialUnits.InitialMaterials == data.InitialMaterials &&
             runtimePreset.InitialUnits.InitialAiMaterials == data.InitialAiMaterials &&
             runtimePreset.ReinforcementInfantryTarget == (int)presetValues["reinforcementInfantryTarget"] &&
             runtimePreset.AttackSquadSize == (int)presetValues["attackSquadSize"] &&
             runtimePreset.Factions.Length == data.Factions.Length,
             "Shared preset projection differs from actual source-backed ECS startup data");

        Entity plan;
        using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<CustomGameStartupStateComponent>(),
                                                ComponentType.ReadOnly<InitialUnitsSpawnConfig>()))
        {
            Need(query.CalculateEntityCount() == 1, "Unique original startup config required");
            plan = query.GetSingletonEntity();
            var config = em.GetComponentData<InitialUnitsSpawnConfig>(plan);
            Need(config.BlockerCount == 0 && config.SpawnRadiusCells == 6 && config.RandomSeed == 1 &&
                 config.InitialMaterials == 220 && config.MaterialsCapacity == 600 && config.InitialAiMaterials == 220 &&
                 config.AiMaterialsCapacity == 600 && config.InitialOil == 0 && config.InitialFuel == 160 &&
                 config.CreateFactionBases == 0, "S073 original resource/budget values changed");
            var unitRows = em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(plan);
            int firstFactionUnits = 0, secondFactionUnits = 0;
            for (int i = 0; i < unitRows.Length; i++)
            {
                if (unitRows[i].FactionId == 1) firstFactionUnits += unitRows[i].Count;
                if (unitRows[i].FactionId == 2) secondFactionUnits += unitRows[i].Count;
            }
            Need(firstFactionUnits == 12 && secondFactionUnits == 12, "S073 actual per-faction unit grant changed");
            Need(em.GetBuffer<InitialUnitsFactionSpawnEntry>(plan).Length == 2 &&
                 em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(plan).Length == 10 &&
                 em.GetBuffer<UnitPrefabRegistryEntry>(plan).Length == 3 &&
                 em.GetBuffer<CustomGameVisualRegistryEntry>(plan).Length == 3,
                 "Original S073 startup buffers changed");
            Need(!em.HasComponent<InitialUnitsSpawnInitialized>(plan), "Do not invent initial spawn completion");
        }

        VerifyLegacyPresetProviderBoundary(em, plan, data,
            (int)presetValues["reinforcementInfantryTarget"], (int)presetValues["attackSquadSize"]);

        string maskPath = Environment.GetEnvironmentVariable("WARLINE_S073_NATIVE_AUTHORED_MASK_PATH");
        Need(!string.IsNullOrWhiteSpace(maskPath), "Pinned native S073 authored-mask capture required for launch identity");
        string maskManifestPath = maskPath + ".json";
        Need(Sha256(maskManifestPath) == Environment.GetEnvironmentVariable("WARLINE_S073_NATIVE_AUTHORED_MASK_MANIFEST_SHA256"),
             "Exact retained S073 authored-mask manifest pin required for launch identity");
        JObject capturedLaunch = JObject.Parse(File.ReadAllText(maskManifestPath));
        Need((string)capturedLaunch["kind"] == "S073LiveOriginalAuthoredBuildingMask" &&
             (int?)capturedLaunch["scenario_index"] == SkirmishPresetConfig.IndustrialBasinScenarioIndex &&
             (int?)capturedLaunch["seed"] == QuickGameConfig.Defaults.MapSeed &&
             (string)capturedLaunch["selected_source_path"] == "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
             "Actual retained S073 launch scenario/seed/source identity required");
        var launchConfig = QuickGameConfig.Defaults;
        launchConfig.ScenarioIndex = (int)capturedLaunch["scenario_index"];
        Need(SkirmishLegacyS073StartupOwner.TryQueueCapturedSelection(
                 em, (int)capturedLaunch["scenario_index"], (int)capturedLaunch["seed"]),
             "Shared original legacy launch owner rejected the pinned S073 default-config selection");
        Entity match;
        SkirmishMatchState queuedMatch;
        using (var matches = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>()))
        {
            Need(matches.CalculateEntityCount() == 1, "Unique actual queued S073 legacy session required");
            match = matches.GetSingletonEntity();
            queuedMatch = em.GetComponentData<SkirmishMatchState>(match);
        }
        Need(queuedMatch.Phase == SkirmishPhase.Queued && !queuedMatch.SessionId.IsEmpty &&
             queuedMatch.Seed == launchConfig.MapSeed && queuedMatch.ScenarioIndex == launchConfig.ScenarioIndex &&
             em.HasBuffer<SkirmishActionRequest>(match) && em.HasBuffer<SkirmishTrackedUnit>(match),
             "Shared original launch owner must publish the queued S073 legacy session and request buffers");
        var launchSnapshot = em.GetComponentObject<SkirmishLaunchSnapshot>(match);
        Need(launchSnapshot != null && launchSnapshot.Configuration.MapSeed == launchConfig.MapSeed &&
             launchSnapshot.Configuration.ScenarioIndex == launchConfig.ScenarioIndex &&
             Newtonsoft.Json.Linq.JToken.DeepEquals(
                 Newtonsoft.Json.Linq.JObject.FromObject(launchSnapshot.Configuration),
                 Newtonsoft.Json.Linq.JObject.FromObject(launchConfig.NormalizeForBaseAssault())),
             "Original queued legacy configuration snapshot must retain the actual pinned S073 selection");
        InitialUnitsSpawnConfig seededSpawnConfig;
        using (var spawnConfigs = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig)))
        {
            Need(spawnConfigs.CalculateEntityCount() == 1,
                 "Original seed owner must retain exactly one selected legacy startup configuration");
            seededSpawnConfig = spawnConfigs.GetSingleton<InitialUnitsSpawnConfig>();
        }
        using (var matches = em.CreateEntityQuery(typeof(SkirmishMatchState)))
        {
            var afterSeed = em.GetComponentData<SkirmishMatchState>(matches.GetSingletonEntity());
            Need(afterSeed.Phase == SkirmishPhase.Queued && afterSeed.Seed == queuedMatch.Seed &&
                 seededSpawnConfig.RandomSeed == (uint)queuedMatch.Seed,
                 "Original ApplySeed owner must project the queued session seed without advancing phase");
        }
        var boundary = em.CreateEntity();
        em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        string definitionsPath = Path.GetFullPath(args[7]);
        Need(Sha256(definitionsPath) == args[8], "Exact S073 original-legacy definition capture required");
        var recipeEm = new EntityManager { LogicalAllocatorAdmitted = true };
        int nextRecipeIdentity = 1;
        recipeEm.AllocateLogicalIdentity = () => new Entity { Index = nextRecipeIdentity++, Version = 1 };
        var recipeTemplates = GeneratedReferenceTemplateImporter.Import(recipeEm, templateCapture,
            Path.GetDirectoryName(templateCapturePath), productionRoots);
        Need(recipeTemplates.Prefabs.Count == productionRoots.Length && recipeTemplates.ImportedEntities > 0,
             "All twelve source-derived S073 production roots must be imported before definition decoding");
        var definitions = GeneratedBuildingDefinitions.LoadS073Legacy(definitionsPath, args[8], recipeTemplates.Prefabs, source, args[3]);
        Need(definitions.Definitions.Count == 6 && definitions.Definitions.Select(d => d.Prefab.name).Distinct(StringComparer.Ordinal).Count() == 6,
             "Exact six original S073 building definitions required");
        var barrack = definitions.Definitions.Single(d => d.Prefab.name == "Building_Barrack");
        Need(barrack.FootprintCells.x == 28 && barrack.FootprintCells.y == 15 && barrack.ProductionSlots.Count == 1 &&
             barrack.ProductionSlots[0].SpawnUnitPrefab.name == "Unit_Chr_Soldier_Male_02_Alt_04" &&
             barrack.ProductionSlots[0].Quantity == 4 && barrack.SecondarySpawnUnitPrefab == null &&
             barrack.TertiarySpawnUnitPrefab == null && barrack.QuaternarySpawnUnitPrefab == null,
             "S073 Barrack legacy single-slot recipe differs from its authored source");
        var fuel = definitions.Definitions.Single(d => d.Prefab.name == "Building_Fuel_Bladder");
        Need(fuel.FootprintCells.x == 14 && fuel.FootprintCells.y == 10 && fuel.ProductionSlots.Count == 11 &&
             fuel.ProductionSlots.All(slot => slot.Quantity == 1 && recipeTemplates.Prefabs.ContainsKey(slot.SpawnUnitPrefab.name)),
             "S073 Fuel Bladder legacy production domain differs from original source");
        foreach (var definition in definitions.Definitions.Where(d => d.Prefab.name != "Building_Barrack" && d.Prefab.name != "Building_Fuel_Bladder"))
            Need(definition.ProductionSlots.Count == 0, "S073 non-producer unexpectedly acquired runtime-overlay recipes");
        SkirmishPresetRuntimeProjection.BindValidatedSourceReadback(
            em, data, (int)presetValues["reinforcementInfantryTarget"],
            (int)presetValues["attackSquadSize"], (uint)queuedMatch.Seed);
        JObject placement;
        try
        {
            placement = ExecuteOriginalBuildingPlacement(em, boundary, definitions.Definitions,
                buildings, data, plan, sourceHost, args[3]);
            placement["actual_legacy_launch"] = new JObject
            {
                ["owner"] = "SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin",
                ["status"] = "Queued",
                ["session_id"] = queuedMatch.SessionId.ToString(),
                ["scenario_index"] = queuedMatch.ScenarioIndex,
                ["seed"] = queuedMatch.Seed,
                ["phase"] = queuedMatch.Phase.ToString(),
                ["source_capture_session_id"] = (string)capturedLaunch["session_id"],
                ["source_selection_captured"] = true,
                ["seed_owner"] = "SkirmishLegacyLaunchOwner.ApplySeed",
                ["startup_random_seed"] = seededSpawnConfig.RandomSeed,
                ["phase_after_seed"] = SkirmishPhase.Queued.ToString()
            };
        }
        finally
        {
            SkirmishPresetRuntimeProjection.ReleaseBoundSourceReadback(em);
        }
        Console.WriteLine("[S073LegacyInitialBuildingPlacement] " + placement.ToString(Newtonsoft.Json.Formatting.None));
        bool unitsSpawned = (bool?)placement["units_spawned"] == true;
        bool initialSpawnCompleted = (bool?)placement["initial_spawn_completion_marked"] == true;
        bool unitOwnerInvoked = placement["unit_spawn_owner"] != null;
        Console.WriteLine("[S073LegacyStartupOwnerHarness] result=Passed actualLegacyStartupOwner=1 importedInitialPrefabRoots=4 importedInitialEntities=140 sourceRows=10 acceptedByOriginalGate=10 legacyDefinitionRoots=6 productionRoots=12 factions=2 unitsEach=12 originalRuntimeBuildingOwnerInvoked=1 originalInitialUnitsSpawnOwnerInvoked=" +
            (unitOwnerInvoked ? "1" : "0") + " playRequestedByOriginalOwner=" + ((bool?)placement["play_requested_by_original_gameplay_state_owner"] == true ? "1" : "0") +
            " initialSpawnCompletionMarked=" + (initialSpawnCompleted ? "1" : "0") + " world=0 unitsSpawned=" + (unitsSpawned ? "1" : "0") + " fullMission=0 readiness=0");
    }
}
