#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Game.Components;
using Unity.Collections;
using Game.Rendering.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Scenes;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Opt-in same-session observation through the original S073 water-mask owner.
    // The original CreateMask allocates only a result mask and temporary footprint list.
    [InitializeOnLoad]
    internal static class S073NativeWaterMaskSnapshot
    {
        const string OutputKey = "WARLINE_S073_NATIVE_WATER_MASK_OUTPUT";
        const string ExpectedIdentity = "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900";
        const string ExpectedContent = "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c";
        const string SourceScene = "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity";
        const string BoundCampaignScene = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
        static readonly string[] SourcePins =
        {
            "Assets/Game/Scripts/Editor/Skirmish/S073NativeWaterMaskSnapshot.cs",
            "Assets/Game/Scripts/Components/GridComponents.cs",
            "Assets/Game/Scripts/Components/InitialUnitsSpawnComponents.cs",
            "Assets/Game/Scripts/Components/OperationMapBuildingComponents.cs",
            "Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs",
            "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs",
            "Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs",
            "Assets/Game/Scripts/Systems/GridOccupancyKernel.cs",
            "Assets/Game/Scripts/Systems/SkirmishWaterPlacement.cs",
            "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs",
            "Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs",
            "Assets/Game/Scripts/Rendering/Contracts/IRenderBoundsPresentation.cs",
            "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
            "Assets/Game/Scenes/Match.unity",
            SourceScene,
            SourceScene + ".meta",
            BoundCampaignScene,
            BoundCampaignScene + ".meta"
        };

        static bool done;
        static World capturedWorld;
        static string capturedSession;

        static S073NativeWaterMaskSnapshot()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OutputKey)))
                EditorApplication.update += Observe;
        }

        static void Observe()
        {
            if (done || !EditorApplication.isPlaying) return;
            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                EntityManager em = world.EntityManager;
                using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
                if (matches.CalculateEntityCount() != 1) return;
                SkirmishMatchState match = matches.GetSingleton<SkirmishMatchState>();
                if (match.ScenarioIndex != 3 || match.Seed != 104729) return;
                if (match.Phase != SkirmishPhase.Playing) return;

                if (capturedWorld == null)
                {
                    capturedWorld = world;
                    capturedSession = match.SessionId.ToString();
                }
                if (!ReferenceEquals(world, capturedWorld) || match.SessionId.ToString() != capturedSession)
                    throw new InvalidOperationException("S073 world or session changed during water observation");

                using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
                using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(GridRoad),
                    typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
                if (maps.CalculateEntityCount() != 1 || grids.CalculateEntityCount() != 1)
                    throw new InvalidOperationException("Unique active S073 map and gameplay grid required");
                Entity mapEntity = maps.GetSingletonEntity();
                ActiveOperationMapComponent active = em.GetComponentData<ActiveOperationMapComponent>(mapEntity);
                OperationMapMetadataComponent metadata = em.GetComponentData<OperationMapMetadataComponent>(mapEntity);
                if (active.OperationMapId.ToString() != "opmap.skirmish.industrial_basin" ||
                    !metadata.Blob.IsCreated || metadata.PhysicalSourceValidated == 0 ||
                    metadata.Blob.Value.SourceIdentityHash.ToString() != ExpectedIdentity ||
                    metadata.Blob.Value.SourceContentHash.ToString() != ExpectedContent)
                    throw new InvalidOperationException("Exact validated S073 Campaign source binding required");

                Entity gridEntity = grids.GetSingletonEntity();
                GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
                if (grid.Width != 2048 || grid.Height != 1024 || grid.CellSize != 1f ||
                    grid.Origin.x != 0f || grid.Origin.y != 0f || grid.Origin.z != 0f)
                    throw new InvalidOperationException("Exact S073 2048x1024 one-meter grid frame required");

                IRenderBoundsPresentation provider = RenderBoundsGateway.Provider;
                if (provider == null || provider.GetType().FullName != "Game.Rendering.RenderBoundsPresentation+Reader")
                    throw new InvalidOperationException("Original RenderBoundsPresentation water provider required");

                JObject beforePins = HashPins();
                long entityOrderBefore = em.EntityOrderVersion;
                int waterFootprintCount;
                var footprints = new NativeList<RenderSurfaceFootprint>(Allocator.Temp);
                try
                {
                    provider.CollectWaterFootprints(em, ref footprints);
                    if (footprints.Length > 100000)
                        throw new InvalidOperationException("S073 water footprint domain exceeds the bounded observation limit");
                    waterFootprintCount = footprints.Length;
                }
                finally { footprints.Dispose(); }
                bool[] mask = InvokeOriginalCreateMask(em, grid);
                if (mask == null || mask.Length != checked(grid.Width * grid.Height))
                    throw new InvalidOperationException("Original S073 CreateMask returned no complete water mask");
                if (!ReferenceEquals(provider, RenderBoundsGateway.Provider) || em.EntityOrderVersion != entityOrderBefore)
                    throw new InvalidOperationException("Water observation changed provider binding or ECS structure");
                JObject afterPins = HashPins();
                if (!JToken.DeepEquals(beforePins, afterPins))
                    throw new InvalidOperationException("Pinned water observation sources changed during capture");

                JObject blockerObservation = CapturePlayingBlockerOccupancy(em, gridEntity, grid,
                    metadata, world, match, afterPins, entityOrderBefore);
                JObject loadedSourceFit = CaptureLoadedSourceFit(em, mapEntity, gridEntity, grid,
                    active, metadata, world, match, entityOrderBefore, afterPins);

                byte[] bytes = new byte[mask.Length];
                int setCells = 0;
                for (int i = 0; i < mask.Length; i++)
                {
                    bytes[i] = mask[i] ? (byte)1 : (byte)0;
                    if (mask[i]) setCells++;
                }
                string output = Path.GetFullPath(Environment.GetEnvironmentVariable(OutputKey));
                if (!output.StartsWith("/private/tmp/", StringComparison.Ordinal) || File.Exists(output))
                    throw new InvalidOperationException("Fresh /private/tmp create-new JSON output required");
                string maskPath = Path.ChangeExtension(output, ".water.bin");
                if (File.Exists(maskPath))
                    throw new InvalidOperationException("Fresh create-new S073 water mask output required");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using (var maskFile = new FileStream(maskPath, FileMode.CreateNew, FileAccess.Write))
                    maskFile.Write(bytes, 0, bytes.Length);

                JObject result = new JObject
                {
                    ["kind"] = "S073NativeWaterMaskObservation",
                    ["schema_version"] = 1,
                    ["scope"] = "SameSessionOriginalSkirmishWaterPlacementCreateMask_NotReadinessOrMissionAcceptance",
                    ["authority"] = "OriginalSkirmishWaterPlacement.CreateMask_OriginalRenderBoundsGatewayProvider",
                    ["unity_version"] = Application.unityVersion,
                    ["world_name"] = world.Name,
                    ["world_sequence"] = world.SequenceNumber.ToString(),
                    ["session_id"] = capturedSession,
                    ["scenario_index"] = match.ScenarioIndex,
                    ["seed"] = match.Seed,
                    ["phase"] = match.Phase.ToString(),
                    ["operation_map_id"] = active.OperationMapId.ToString(),
                    ["source_map_id"] = active.OperationMapId.ToString(),
                    ["source_campaign_map_id"] = metadata.Blob.Value.SourceOperationMapId.ToString(),
                    ["source_identity_hash"] = ExpectedIdentity,
                    ["source_content_hash"] = ExpectedContent,
                    ["physical_source_validated"] = true,
                    ["provider_type"] = provider.GetType().AssemblyQualifiedName,
                    ["provider_assembly_path"] = provider.GetType().Assembly.Location,
                    ["provider_assembly_sha256"] = HashFile(provider.GetType().Assembly.Location),
                    ["water_footprint_count"] = waterFootprintCount,
                    ["grid"] = new JObject { ["width"] = grid.Width, ["height"] = grid.Height,
                        ["cell_size"] = grid.CellSize, ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z) },
                    ["mask_path"] = maskPath,
                    ["mask_encoding"] = "uint8-bool01-row-major-xfast",
                    ["mask_cells"] = bytes.Length,
                    ["water_cells"] = setCells,
                    ["mask_sha256"] = HashBytes(bytes),
                    ["entity_order_version_before"] = entityOrderBefore,
                    ["entity_order_version_after"] = em.EntityOrderVersion,
                    ["provider_reference_unchanged"] = true,
                    ["source_pins_sha256"] = afterPins,
                    ["playing_passive_input_observation"] = blockerObservation,
                    ["loaded_campaign_source_fit_observation"] = loadedSourceFit,
                    ["campaign_gameplay_state_copied"] = false,
                    ["mission_ready"] = false,
                    ["source_fit_certified"] = false,
                    ["normal_input_playthrough"] = false,
                    ["player_readiness"] = false
                };
                using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(file)) writer.Write(result.ToString(Formatting.Indented) + "\n");
                done = true;
                EditorApplication.update -= Observe;
                Debug.Log("[S073LoadedCampaignSourceFit] result=Captured session=" + capturedSession +
                    " sceneGuid=" + loadedSourceFit["source_scene_guid"] +
                    " spawnPlanAvailable=" + ((bool)loadedSourceFit["original_initial_spawn_plan_available"] ? "1" : "0"));
                Debug.Log("[S073NativeWaterMask] result=Recorded cells=" + bytes.Length + " waterCells=" + setCells + " path=" + output);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                done = true;
                EditorApplication.update -= Observe;
                Debug.LogException(error);
                Debug.LogError("[S073NativeWaterMask] result=Failed " + error.Message);
                EditorApplication.Exit(1);
            }
        }

        // Read the Campaign scene and current native owner inputs in the same Playing world.
        // This is a source-fit diagnostic only: it never changes map admission or readiness.
        static JObject CaptureLoadedSourceFit(EntityManager em, Entity mapEntity, Entity gridEntity, GridConfig grid,
            ActiveOperationMapComponent active, OperationMapMetadataComponent metadata, World world,
            SkirmishMatchState match, long entityOrderBefore, JObject sourcePins)
        {
            const string expectedSourceSceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
            const string expectedConfig = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset";
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string sceneGuid = metadata.Blob.Value.Navigation.AuthoredSubSceneGuid.ToString();
            if (active.OperationMapId.ToString() != "opmap.skirmish.industrial_basin" ||
                metadata.Blob.Value.SourceOperationMapId.ToString() != "opmap.skirmish.desert_base_01" ||
                sceneGuid != expectedSourceSceneGuid)
                throw new InvalidOperationException("Exact S073 logical-to-Campaign scene binding required");
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            string configPath = Path.Combine(root, expectedConfig);
            if (scenePath != BoundCampaignScene || !File.Exists(scenePath) || !File.Exists(scenePath + ".meta") ||
                !File.Exists(configPath))
                throw new InvalidOperationException("GUID-resolved Campaign scene must match the pinned path and S073 map config must exist");
            string sceneMeta = File.ReadAllText(scenePath + ".meta");
            string config = File.ReadAllText(configPath);
            if (!sceneMeta.Contains("guid: " + sceneGuid, StringComparison.Ordinal) ||
                !config.Contains("operationMapId: opmap.skirmish.industrial_basin", StringComparison.Ordinal) ||
                !config.Contains("sourceOperationMapId: opmap.skirmish.desert_base_01", StringComparison.Ordinal) ||
                !config.Contains("authoredSubSceneGuid: " + sceneGuid, StringComparison.Ordinal))
                throw new InvalidOperationException("Current S073 config, scene meta and source binding disagree");

            var ecsGuid = new Unity.Entities.Hash128(sceneGuid);
            Entity sceneRoot = SceneSystem.GetSceneEntity(world.Unmanaged, ecsGuid);
            if (sceneRoot == Entity.Null || !em.Exists(sceneRoot) || !SceneSystem.IsSceneLoaded(world.Unmanaged, sceneRoot) ||
                !em.HasComponent<SceneReference>(sceneRoot) ||
                !em.GetComponentData<SceneReference>(sceneRoot).SceneGUID.Equals(ecsGuid) ||
                !em.HasBuffer<ResolvedSectionEntity>(sceneRoot))
                throw new InvalidOperationException("Bound Campaign ECS scene is not loaded and resolved in this S073 world");
            var resolved = em.GetBuffer<ResolvedSectionEntity>(sceneRoot, true);
            if (resolved.Length <= 0) throw new InvalidOperationException("Bound Campaign ECS scene has no resolved sections");

            bool IsOwnedByBoundScene(Entity entity)
            {
                if (!em.Exists(entity) || !em.HasComponent<SceneTag>(entity)) return false;
                Entity section = em.GetSharedComponent<SceneTag>(entity).SceneEntity;
                if (!em.Exists(section) || !em.HasComponent<SceneSectionData>(section) ||
                    !em.HasComponent<SceneEntityReference>(section) ||
                    !em.GetComponentData<SceneSectionData>(section).SceneGUID.Equals(ecsGuid) ||
                    em.GetComponentData<SceneEntityReference>(section).SceneEntity != sceneRoot) return false;
                foreach (ResolvedSectionEntity row in resolved)
                    if (row.SectionEntity == section) return true;
                return false;
            }

            if (!IsOwnedByBoundScene(gridEntity))
                throw new InvalidOperationException("Active gameplay grid is not owned by the bound Campaign scene");

            using var blockerQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<StaticGridBlocker>(), ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<GridBlockerSize>() },
                None = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<Disabled>() }
            });
            using var blockerEntities = blockerQuery.ToEntityArray(Allocator.Temp);
            using var buildingQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<OperationMapBuildingComponent>(), ComponentType.ReadOnly<Faction>() },
                None = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<Disabled>() }
            });
            using var buildingEntities = buildingQuery.ToEntityArray(Allocator.Temp);
            var blockers = new JArray();
            int boundBlockers = 0;
            foreach (Entity entity in blockerEntities)
            {
                bool owned = IsOwnedByBoundScene(entity);
                if (owned) boundBlockers++;
                var cell = em.GetComponentData<UnitGrid>(entity).Cell;
                var size = em.GetComponentData<GridBlockerSize>(entity).Size;
                blockers.Add(new JObject { ["entity"] = new JArray(entity.Index, entity.Version),
                    ["grid_cell"] = new JArray(cell.x, cell.y), ["size_cells"] = new JArray(size.x, size.y),
                    ["bound_campaign_scene_owned"] = owned,
                    ["has_static_blocker_history"] = em.HasComponent<StaticBlockerPrevBounds>(entity) });
            }
            var buildings = new JArray();
            int boundBuildings = 0;
            foreach (Entity entity in buildingEntities)
            {
                bool owned = IsOwnedByBoundScene(entity);
                if (owned) boundBuildings++;
                OperationMapBuildingComponent building = em.GetComponentData<OperationMapBuildingComponent>(entity);
                var row = new JObject { ["entity"] = new JArray(entity.Index, entity.Version),
                    ["operation_map_id"] = building.OperationMapId.ToString(), ["stable_id"] = building.StableId.ToString(),
                    ["source_global_object_id"] = building.SourceGlobalObjectId.ToString(),
                    ["placement_index"] = building.PlacementIndex, ["faction_id"] = em.GetComponentData<Faction>(entity).Id,
                    ["bound_campaign_scene_owned"] = owned };
                if (em.HasComponent<LocalTransform>(entity))
                {
                    LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
                    row["position"] = new JArray(transform.Position.x, transform.Position.y, transform.Position.z);
                }
                buildings.Add(row);
            }

            using var planQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
                ComponentType.ReadOnly<InitialUnitsFactionSpawnEntry>(), ComponentType.ReadOnly<InitialUnitsFactionBuildingSpawnEntry>());
            var spawnRows = new JArray();
            var buildingSpawnRows = new JArray();
            bool spawnPlanAvailable = planQuery.CalculateEntityCount() == 1;
            if (spawnPlanAvailable)
            {
                Entity plan = planQuery.GetSingletonEntity();
                foreach (InitialUnitsFactionSpawnEntry row in em.GetBuffer<InitialUnitsFactionSpawnEntry>(plan, true))
                    spawnRows.Add(new JObject { ["faction_id"] = row.FactionId,
                        ["spawn_cell"] = new JArray(row.SpawnCell.x, row.SpawnCell.y) });
                foreach (InitialUnitsFactionBuildingSpawnEntry row in em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(plan, true))
                    buildingSpawnRows.Add(new JObject { ["faction_id"] = row.FactionId,
                        ["prefab_lookup_key"] = row.PrefabLookupKey.ToString(),
                        ["origin_offset"] = new JArray(row.OriginOffset.x, row.OriginOffset.y) });
            }

            using var currentMatches = em.CreateEntityQuery(typeof(SkirmishMatchState));
            ActiveOperationMapComponent activeAfter = em.GetComponentData<ActiveOperationMapComponent>(mapEntity);
            OperationMapMetadataComponent metadataAfter = em.GetComponentData<OperationMapMetadataComponent>(mapEntity);
            if (currentMatches.CalculateEntityCount() != 1 || em.EntityOrderVersion != entityOrderBefore ||
                activeAfter.Generation != active.Generation || activeAfter.OperationMapId.ToString() != active.OperationMapId.ToString() ||
                metadataAfter.Generation != metadata.Generation || metadataAfter.PhysicalSourceValidated != metadata.PhysicalSourceValidated ||
                !metadataAfter.Blob.IsCreated || metadataAfter.Blob.Value.SourceIdentityHash.ToString() != metadata.Blob.Value.SourceIdentityHash.ToString() ||
                metadataAfter.Blob.Value.SourceContentHash.ToString() != metadata.Blob.Value.SourceContentHash.ToString() ||
                !JToken.DeepEquals(sourcePins, HashPins()) ||
                SceneSystem.GetSceneEntity(world.Unmanaged, ecsGuid) != sceneRoot ||
                !SceneSystem.IsSceneLoaded(world.Unmanaged, sceneRoot) ||
                em.GetComponentData<SkirmishMatchState>(currentMatches.GetSingletonEntity()).SessionId.ToString() != match.SessionId.ToString())
                throw new InvalidOperationException("S073 world, loaded scene, or source pins changed during passive capture");

            return new JObject
            {
                ["kind"] = "S073SameSessionLoadedCampaignSourceFitObservation",
                ["scope"] = "SamePlayingWorld_LoadedCampaignSceneOwnerAndSpawnInputs_DiagnosticOnly",
                ["world_name"] = world.Name, ["world_sequence"] = world.SequenceNumber.ToString(),
                ["session_id"] = match.SessionId.ToString(), ["phase"] = match.Phase.ToString(),
                ["logical_map_id"] = active.OperationMapId.ToString(),
                ["campaign_source_map_id"] = metadata.Blob.Value.SourceOperationMapId.ToString(),
                ["source_identity_hash"] = metadata.Blob.Value.SourceIdentityHash.ToString(),
                ["source_content_hash"] = metadata.Blob.Value.SourceContentHash.ToString(),
                ["source_scene_guid"] = sceneGuid, ["source_scene_path"] = scenePath,
                ["source_scene_sha256"] = (string)sourcePins[BoundCampaignScene],
                ["source_scene_meta_sha256"] = (string)sourcePins[BoundCampaignScene + ".meta"],
                ["s073_config_path"] = expectedConfig, ["s073_config_sha256"] = HashFile(configPath),
                ["scene_loaded"] = true, ["resolved_section_count"] = resolved.Length,
                ["active_grid_entity"] = new JArray(gridEntity.Index, gridEntity.Version),
                ["grid_owned_by_campaign_scene"] = true,
                ["grid"] = new JObject { ["width"] = grid.Width, ["height"] = grid.Height,
                    ["cell_size"] = grid.CellSize, ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z) },
                ["blocker_owner_type"] = "Game.Runtime.StaticGridBlockerUpdateSystem",
                ["blocker_owner_source_sha256"] = sourcePins["Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs"],
                ["blocker_contributor_query_count"] = blockerEntities.Length,
                ["blocker_contributors_owned_by_campaign_scene"] = boundBlockers,
                ["blocker_contributors"] = blockers,
                ["building_owner_query_count"] = buildingEntities.Length,
                ["building_entities_owned_by_campaign_scene"] = boundBuildings,
                ["building_entities"] = buildings,
                ["original_initial_unit_spawn_cells"] = spawnRows,
                ["original_initial_building_spawn_inputs"] = buildingSpawnRows,
                ["original_initial_spawn_plan_available"] = spawnPlanAvailable,
                ["capture_completeness"] = new JObject { ["loaded_campaign_scene"] = true,
                    ["active_grid_scene_ownership"] = true, ["blocker_owner_inputs"] = blockerEntities.Length > 0,
                    ["building_owner_inputs"] = buildingEntities.Length > 0,
                    ["original_initial_spawn_plan"] = spawnPlanAvailable },
                ["entity_order_version_before"] = entityOrderBefore,
                ["entity_order_version_after"] = em.EntityOrderVersion,
                ["source_fit_certified"] = false, ["physical_source_admission_changed"] = false,
                ["scheduler_admitted"] = false, ["mission_ready"] = false,
                ["player_readiness"] = false,
                ["limitations"] = new JArray("Current Playing positions are observations; this capture does not prove original startup-time placement legality.",
                    "This records loaded scene ownership and owner inputs; it does not certify all objectives, roads, camera bounds, or mission-wide fit.",
                    "No gameplay state is transferred to or imported by the independent source host.",
                    spawnPlanAvailable ? "Initial spawn plan inputs were present in this session." : "Initial spawn plan inputs were unavailable at Playing; spawn input completeness is false.")
            };
        }

        static JObject CapturePlayingBlockerOccupancy(EntityManager em, Entity gridEntity, GridConfig grid,
            OperationMapMetadataComponent metadata, World world, SkirmishMatchState match,
            JObject sourcePins, long entityOrderBefore)
        {
            DynamicBlockerComponent blockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
            int cells = checked(grid.Width * grid.Height);
            if (blockers.GridSize != cells || blockers.Counts.Length != cells ||
                blockers.FriendlyPassFactionIds.Length != cells || !blockers.Blocked.IsCreated ||
                blockers.Blocked.Length != cells || occupancy.GridSize != cells ||
                !occupancy.Occupied.IsCreated || occupancy.Occupied.Length != cells ||
                blockers.WriteInProgress != 0 || !RuntimeGridBlockerEpoch.IsCommitted(grid, blockers))
                throw new InvalidOperationException("Committed, complete native S073 blocker and occupancy arrays required");

            string output = Path.GetFullPath(Environment.GetEnvironmentVariable(OutputKey));
            string rawPath = Path.ChangeExtension(output, ".blocker-occupancy.bin");
            string receiptPath = rawPath + ".json";
            if (File.Exists(rawPath) || File.Exists(receiptPath))
                throw new InvalidOperationException("Fresh create-new native S073 blocker/occupancy outputs required");

            byte[] raw;
            using (var memory = new MemoryStream(checked(60 + ((cells + 7) / 8) * 2 + cells * 5)))
            {
                using (var writer = new BinaryWriter(memory, System.Text.Encoding.UTF8, true))
                {
                    writer.Write(new byte[] { (byte)'S', (byte)'0', (byte)'7', (byte)'3', (byte)'B', (byte)'O', (byte)'C', (byte)'1' });
                    writer.Write((uint)grid.Width); writer.Write((uint)grid.Height);
                    writer.Write(grid.CellSize); writer.Write(grid.Origin.x); writer.Write(grid.Origin.y); writer.Write(grid.Origin.z);
                    writer.Write(blockers.StorageEpoch); writer.Write(blockers.AppliedStorageEpoch); writer.Write(blockers.AppliedRevision);
                    writer.Write((uint)cells);
                    WriteBits(writer, cells, blockers.Blocked.IsSet);
                    for (int i = 0; i < cells; i++) writer.Write(blockers.Counts[i]);
                    for (int i = 0; i < cells; i++) writer.Write(blockers.FriendlyPassFactionIds[i]);
                    WriteBits(writer, cells, occupancy.Occupied.IsSet);
                    writer.Flush();
                }
                raw = memory.ToArray();
            }
            string rawHash = HashBytes(raw);
            using (var file = new FileStream(rawPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                file.Write(raw, 0, raw.Length);

            const string observation = "playing_passive_input_observation";
            var sidecar = new JObject
            {
                ["kind"] = "S073BlockerOccupancyRawSnapshot", ["schema_version"] = 1,
                ["status"] = "Captured", ["producer"] = "native", ["observation"] = observation,
                ["raw_path"] = rawPath, ["raw_sha256"] = rawHash, ["byte_length"] = raw.Length,
                ["width"] = grid.Width, ["height"] = grid.Height, ["cell_size"] = grid.CellSize,
                ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z),
                ["storage_epoch"] = blockers.StorageEpoch.ToString(),
                ["applied_storage_epoch"] = blockers.AppliedStorageEpoch.ToString(),
                ["applied_revision"] = blockers.AppliedRevision.ToString(),
                ["mission"] = "S073", ["seed"] = 104729,
                ["source_map_id"] = "opmap.skirmish.industrial_basin",
                ["source_campaign_map_id"] = metadata.Blob.Value.SourceOperationMapId.ToString(),
                ["source_identity_hash"] = metadata.Blob.Value.SourceIdentityHash.ToString(),
                ["source_content_hash"] = metadata.Blob.Value.SourceContentHash.ToString(),
                ["native_session_id"] = match.SessionId.ToString(),
                ["native_world_sequence"] = world.SequenceNumber.ToString(), ["native_world_name"] = world.Name,
                ["native_frame"] = Time.frameCount, ["native_editor_time_since_startup_seconds"] = EditorApplication.timeSinceStartup,
                ["native_source_pins_sha256"] = sourcePins.DeepClone(),
                ["source_host_state_transferred"] = false, ["campaign_state_transferred"] = false,
                ["readiness_claimed"] = false,
                ["scope"] = "SameSessionNativePlayingRawBlockerOccupancyCapture_NoInput_NoStateTransfer_NoAdmission"
            };
            using (var file = new FileStream(receiptPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(file)) writer.Write(sidecar.ToString(Formatting.Indented) + "\n");
            Debug.Log("[S073NativeBlockerOccupancy] result=Recorded rawSha256=" + rawHash + " path=" + rawPath);

            if (em.EntityOrderVersion != entityOrderBefore || !JToken.DeepEquals(sourcePins, HashPins()))
                throw new InvalidOperationException("Passive blocker/occupancy observation changed ECS structure or pinned sources");
            return new JObject
            {
                ["observation_name"] = observation,
                ["raw_blocker_occupancy_snapshot"] = sidecar,
                ["session_id"] = match.SessionId.ToString(), ["phase"] = match.Phase.ToString(),
                ["mission_ready"] = false, ["player_readiness"] = false,
                ["input_injected"] = false, ["gameplay_state_mutated"] = false
            };
        }

        static void WriteBits(BinaryWriter writer, int cells, Func<int, bool> isSet)
        {
            for (int start = 0; start < cells; start += 8)
            {
                byte bits = 0;
                for (int bit = 0; bit < 8 && start + bit < cells; bit++)
                    if (isSet(start + bit)) bits |= (byte)(1 << bit);
                writer.Write(bits);
            }
        }

        static bool[] InvokeOriginalCreateMask(EntityManager em, GridConfig grid)
        {
            Type owner = Type.GetType("Game.Runtime.SkirmishWaterPlacement, Assembly-CSharp", false);
            if (owner == null)
                owner = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Game.Runtime.SkirmishWaterPlacement", false)).FirstOrDefault(t => t != null);
            if (owner == null) throw new TypeLoadException("Original SkirmishWaterPlacement owner is unavailable");
            MethodInfo method = owner.GetMethod("CreateMask", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(owner.FullName, "CreateMask");
            try { return (bool[])method.Invoke(null, new object[] { em, grid }); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }

        static JObject HashPins()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var hashes = new JObject();
            foreach (string relative in SourcePins)
            {
                string path = Path.Combine(root, relative);
                if (!File.Exists(path)) throw new FileNotFoundException("Required S073 water source pin missing", relative);
                hashes[relative] = HashFile(path);
            }
            return hashes;
        }

        static string HashFile(string path) => HashBytes(File.ReadAllBytes(path));

        static string HashBytes(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
