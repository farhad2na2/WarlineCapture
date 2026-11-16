#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Captures only baked authored vehicle placement facts from the isolated selected
    /// Campaign entity scene. It never reads live Campaign faction/economy/garrison state.
    /// </summary>
    public static class S073AuthoredVehicleOccupancyProjectionCapture
    {
        private const string SceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
        private const string LogicalMapId = "opmap.skirmish.industrial_basin";
        private const string SourceMapId = "opmap.skirmish.desert_base_01";
        private const string MapConfigPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset";
        private const int ExpectedVehicles = 22;

        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle Editor required for isolated authored-scene projection capture");
            string[] args = Environment.GetCommandLineArgs();
            int arg = Array.IndexOf(args, "-s073AuthoredVehicleProjectionOutput");
            if (arg < 0 || arg + 1 >= args.Length)
                throw new ArgumentException("Fresh output directory required through -s073AuthoredVehicleProjectionOutput");
            string outputDirectory = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[arg + 1]);
            Directory.CreateDirectory(outputDirectory);

            string scenePath = AssetDatabase.GUIDToAssetPath(SceneGuid);
            string sceneMetaPath = scenePath + ".meta";
            if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(scenePath) || !File.Exists(sceneMetaPath) || !File.Exists(MapConfigPath))
                throw new InvalidOperationException("Pinned selected-map config or authored Campaign scene is missing");
            if (!File.ReadAllText(sceneMetaPath).Contains("guid: " + SceneGuid, StringComparison.Ordinal))
                throw new InvalidOperationException("Authored scene meta GUID differs from selected map binding");
            string mapText = File.ReadAllText(MapConfigPath);
            if (!mapText.Contains("operationMapId: " + LogicalMapId, StringComparison.Ordinal) ||
                !mapText.Contains("sourceOperationMapId: " + SourceMapId, StringComparison.Ordinal) ||
                !mapText.Contains("authoredSubSceneGuid: " + SceneGuid, StringComparison.Ordinal))
                throw new InvalidOperationException("Selected S073 config no longer binds the pinned Campaign scene/source");

            string sceneHash = SkirmishOfflineDataExporter.HashFile(scenePath);
            string sceneMetaHash = SkirmishOfflineDataExporter.HashFile(sceneMetaPath);
            string mapHash = SkirmishOfflineDataExporter.HashFile(MapConfigPath);
            World originalDefault = World.DefaultGameObjectInjectionWorld;
            JArray rows;
            JObject gridFact;
            GridConfig sourceGrid;
            int rootCount;
            using (var world = new World("S073AuthoredVehicleOccupancyProjection", WorldFlags.Game))
            {
                var streamingSystems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in streamingSystems)
                    if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal))
                        throw new InvalidOperationException("Gameplay systems are forbidden in isolated projection capture");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, streamingSystems);
                var scene = SceneSystem.LoadSceneAsync(world.Unmanaged, new Unity.Entities.Hash128(SceneGuid),
                    new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
                world.Update();
                if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene))
                    throw new InvalidOperationException("Pinned Campaign authored entity scene did not finish streaming");

                EntityManager em = world.EntityManager;
                em.CompleteAllTrackedJobs();
                Entity sceneEntity = SceneSystem.GetSceneEntity(world.Unmanaged, new Unity.Entities.Hash128(SceneGuid));
                if (sceneEntity == Entity.Null || !em.Exists(sceneEntity))
                    throw new InvalidOperationException("Streamed Campaign scene root is unavailable");

                using (var grids = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>(), ComponentType.ReadOnly<SceneTag>()))
                {
                    if (grids.CalculateEntityCount() != 1)
                        throw new InvalidOperationException("Exactly one authored scene grid required");
                    Entity gridEntity = grids.GetSingletonEntity();
                    RequireSceneOwnership(em, gridEntity, sceneEntity, SceneGuid);
                    GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
                    if (grid.Width != 2048 || grid.Height != 1024 || grid.CellSize != 1f ||
                        !math.all(math.isfinite(grid.Origin)))
                        throw new InvalidOperationException("Authored scene grid frame differs from pinned S073 source");
                    sourceGrid = grid;
                    gridFact = new JObject { ["width"] = grid.Width, ["height"] = grid.Height,
                        ["cell_size"] = grid.CellSize, ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z) };
                }

                using (var roots = em.CreateEntityQuery(ComponentType.ReadOnly<OperationMapEntityPresentationRoot>()))
                using (var rootEntities = roots.ToEntityArray(Allocator.Temp))
                {
                    rootCount = 0;
                    foreach (Entity root in rootEntities)
                    {
                        var role = em.GetComponentData<OperationMapEntityPresentationRoot>(root);
                        if (role.OperationMapId.ToString() != SourceMapId || role.Role != 2) continue;
                        RequireSceneOwnership(em, root, sceneEntity, SceneGuid);
                        rootCount++;
                    }
                    if (rootCount != 1)
                        throw new InvalidOperationException("One source GameplayVehicles presentation root required");
                }

                using (var query = em.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitFootprint>(),
                        ComponentType.ReadOnly<UnitSourcePrefabKey>(), ComponentType.ReadOnly<LocalTransform>(),
                        ComponentType.ReadOnly<SceneTag>() },
                    None = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<Disabled>() }
                }))
                using (var entities = query.ToEntityArray(Allocator.Temp))
                {
                    rows = new JArray();
                    var placementIndices = new System.Collections.Generic.HashSet<int>();
                    var globalIds = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                    var diagnostics = new JObject
                    {
                        ["base_query_count"] = entities.Length,
                        ["with_authored_vehicle_presentation"] = 0,
                        ["identity_on_primary_entity"] = 0,
                        ["with_detailed_visual_reference"] = 0,
                        ["detail_root_exists"] = 0,
                        ["identity_on_detail_root"] = 0,
                        ["identity_map_and_role_match"] = 0,
                        ["placement_join_matches"] = 0,
                        ["primary_scene_guid_matches"] = 0,
                        ["detail_root_scene_guid_matches"] = 0,
                        ["occupancy_exclusions_absent"] = 0,
                        ["candidates"] = new JArray()
                    };
                    bool membershipFailure = false;
                    foreach (Entity entity in entities)
                    {
                        if (!em.HasComponent<OperationMapAuthoredVehiclePresentation>(entity)) continue;
                        diagnostics["with_authored_vehicle_presentation"] = (int)diagnostics["with_authored_vehicle_presentation"] + 1;
                        var marker = em.GetComponentData<OperationMapAuthoredVehiclePresentation>(entity);
                        bool identityOnPrimary = em.HasComponent<OperationMapEntityPresentationIdentity>(entity);
                        if (identityOnPrimary)
                            diagnostics["identity_on_primary_entity"] = (int)diagnostics["identity_on_primary_entity"] + 1;
                        bool hasDetail = em.HasComponent<UnitDetailedVisualReference>(entity);
                        if (hasDetail)
                            diagnostics["with_detailed_visual_reference"] = (int)diagnostics["with_detailed_visual_reference"] + 1;
                        Entity detailRoot = hasDetail ? em.GetComponentData<UnitDetailedVisualReference>(entity).Root : Entity.Null;
                        bool rootExists = detailRoot != Entity.Null && em.Exists(detailRoot);
                        if (rootExists) diagnostics["detail_root_exists"] = (int)diagnostics["detail_root_exists"] + 1;
                        bool rootHasIdentity = rootExists && em.HasComponent<OperationMapEntityPresentationIdentity>(detailRoot);
                        if (rootHasIdentity)
                            diagnostics["identity_on_detail_root"] = (int)diagnostics["identity_on_detail_root"] + 1;
                        OperationMapEntityPresentationIdentity identity = rootHasIdentity
                            ? em.GetComponentData<OperationMapEntityPresentationIdentity>(detailRoot) : default;
                        var key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                        bool primaryScene = IsOwnedByScene(em, entity, sceneEntity, SceneGuid);
                        if (primaryScene) diagnostics["primary_scene_guid_matches"] = (int)diagnostics["primary_scene_guid_matches"] + 1;
                        bool detailScene = rootExists && IsOwnedByScene(em, detailRoot, sceneEntity, SceneGuid);
                        if (detailScene) diagnostics["detail_root_scene_guid_matches"] = (int)diagnostics["detail_root_scene_guid_matches"] + 1;
                        bool excluded = em.HasComponent<StaticGridBlocker>(entity) || em.HasComponent<RuntimeBuildingCombatTag>(entity) ||
                            em.HasComponent<UnitDeathAnimationComponent>(entity);
                        if (!excluded)
                            diagnostics["occupancy_exclusions_absent"] = (int)diagnostics["occupancy_exclusions_absent"] + 1;
                        var candidateDiagnostic = new JObject
                        {
                            ["unit_source_prefab_key"] = key,
                            ["placement_index_from_vehicle_marker"] = marker.PlacementIndex,
                            ["identity_on_primary_entity"] = identityOnPrimary,
                            ["detail_reference_present"] = hasDetail,
                            ["detail_root_exists"] = rootExists,
                            ["identity_on_detail_root"] = rootHasIdentity,
                            ["detail_root_source_map_id"] = rootHasIdentity ? identity.OperationMapId.ToString() : null,
                            ["detail_root_role"] = rootHasIdentity ? identity.Role : (byte?)null,
                            ["detail_root_source_global_object_id"] = rootHasIdentity ? identity.SourceGlobalObjectId.ToString() : null,
                            ["detail_root_placement_index"] = rootHasIdentity ? identity.PlacementIndex : (int?)null,
                            ["primary_scene_guid_matches"] = primaryScene,
                            ["detail_root_scene_guid_matches"] = detailScene,
                            ["occupancy_exclusion_present"] = excluded
                        };
                        ((JArray)diagnostics["candidates"]).Add(candidateDiagnostic);
                        if (!rootHasIdentity || !primaryScene || !detailScene || excluded)
                        {
                            membershipFailure = true;
                            continue;
                        }
                        bool mapRole = identity.OperationMapId.ToString() == SourceMapId && identity.Role == 2;
                        if (mapRole) diagnostics["identity_map_and_role_match"] = (int)diagnostics["identity_map_and_role_match"] + 1;
                        bool placementMatches = marker.PlacementIndex == identity.PlacementIndex;
                        if (placementMatches) diagnostics["placement_join_matches"] = (int)diagnostics["placement_join_matches"] + 1;
                        var grid = em.GetComponentData<UnitGrid>(entity);
                        var footprint = em.GetComponentData<UnitFootprint>(entity);
                        LocalTransform authoredTransform = em.GetComponentData<LocalTransform>(entity);
                        int2 transformCell = GridUtils.WorldToCell(sourceGrid, authoredTransform.Position);
                        bool hasAirMovement = em.HasComponent<UnitAirMovement>(entity);
                        bool gridAlreadyInitialized = em.HasComponent<UnitGridInitialized>(entity);
                        int2 effectiveCell = hasAirMovement || gridAlreadyInitialized ? grid.Cell : transformCell;
                        string globalId = identity.SourceGlobalObjectId.ToString();
                        if (!mapRole || !placementMatches || identity.PlacementIndex < 0 ||
                            identity.PlacementIndex >= ExpectedVehicles || string.IsNullOrWhiteSpace(globalId) ||
                            string.IsNullOrWhiteSpace(key) || !key.StartsWith("Unit_Veh_", StringComparison.Ordinal) ||
                            !placementIndices.Add(identity.PlacementIndex) || !globalIds.Add(globalId))
                        {
                            membershipFailure = true;
                            continue;
                        }

                        int2 size = footprint.Size;
                        var bounds = Game.Runtime.Navigation.GridOccupancyKernel.Bounds(2048, 1024,
                            effectiveCell.x, effectiveCell.y, size.x, size.y);
                        int nominalSizeX = Math.Max(1, size.x);
                        int nominalSizeY = Math.Max(1, size.y);
                        int nominalMinX = effectiveCell.x - (nominalSizeX - 1) / 2;
                        int nominalMinY = effectiveCell.y - (nominalSizeY - 1) / 2;
                        rows.Add(new JObject
                        {
                            ["source_global_object_id"] = globalId,
                            ["source_operation_map_id"] = identity.OperationMapId.ToString(),
                            ["presentation_role"] = "GameplayVehicles",
                            ["placement_index"] = identity.PlacementIndex,
                            ["unit_source_prefab_key"] = key,
                            ["scene_guid"] = SceneGuid,
                            ["baked_unit_grid_cell"] = new JArray(grid.Cell.x, grid.Cell.y),
                            ["authored_local_transform_position"] = new JArray(authoredTransform.Position.x,
                                authoredTransform.Position.y, authoredTransform.Position.z),
                            ["unit_air_movement_component_present"] = hasAirMovement,
                            ["unit_grid_initialized_component_present"] = gridAlreadyInitialized,
                            ["candidate_cell_from_authored_transform"] = new JArray(transformCell.x, transformCell.y),
                            ["pre_simulation_effective_cell"] = new JArray(effectiveCell.x, effectiveCell.y),
                            ["authored_footprint"] = new JArray(size.x, size.y),
                            ["kernel_footprint"] = new JArray(Math.Max(1, size.x), Math.Max(1, size.y)),
                            ["kernel_nominal_bounds_min"] = new JArray(nominalMinX, nominalMinY),
                            ["kernel_nominal_bounds_max_exclusive"] = new JArray(nominalMinX + nominalSizeX, nominalMinY + nominalSizeY),
                            ["kernel_bounds_min"] = new JArray(bounds.MinX, bounds.MinZ),
                            ["kernel_bounds_max_exclusive"] = new JArray(bounds.MaxX, bounds.MaxZ)
                        });
                    }
                    if (membershipFailure || rows.Count != ExpectedVehicles || placementIndices.Count != ExpectedVehicles ||
                        Enumerable.Range(0, ExpectedVehicles).Any(i => !placementIndices.Contains(i)))
                    {
                        WriteFailureDiagnostic(outputDirectory, diagnostics, rows.Count);
                        throw new InvalidOperationException("Authored vehicle contributor join failed; see failure-diagnostic.json");
                    }
                }
            }

            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld) ||
                SkirmishOfflineDataExporter.HashFile(scenePath) != sceneHash ||
                SkirmishOfflineDataExporter.HashFile(sceneMetaPath) != sceneMetaHash ||
                SkirmishOfflineDataExporter.HashFile(MapConfigPath) != mapHash)
                throw new InvalidOperationException("Default World or pinned authored source assets changed during capture");

            JObject pins = new JObject();
            string[] paths = { MapConfigPath, scenePath, sceneMetaPath,
                "Assets/Game/Scripts/Editor/Skirmish/S073AuthoredVehicleOccupancyProjectionCapture.cs",
                "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs",
                "Assets/Game/Scripts/Authorings/OperationMapEntityPresentationIdentityAuthoring.cs",
                "Assets/Game/Scripts/Authorings/OperationMapAuthoredVehicleOwnershipAuthoring.cs",
                "Assets/Game/Scripts/Editor/OperationMapVehicleCandidateMigrationEditor.cs",
                "Assets/Game/Scripts/Components/OperationMapEntityPresentationComponents.cs",
                "Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs",
                "Assets/Game/Scripts/Systems/GridOccupancyKernel.cs",
                "Assets/Game/Scripts/Systems/UnitGridSnapSystem.cs",
                "Assets/Game/Scripts/Components/GridCoordinateKernel.cs",
                "Assets/Game/Scripts/Components/GridComponents.cs",
                "Assets/Game/Scripts/Components/UnitVisualComponents.cs",
                "Design/AgentReports/2026-07-21_dense_city_phase0a_vehicle_ecs_conversion_inventory.json" };
            foreach (string relative in paths)
                pins[relative] = SkirmishOfflineDataExporter.HashFile(Path.GetFullPath(relative));

            var output = new JObject
            {
                ["kind"] = "S073AuthoredVehicleOccupancyProjectionInputs",
                ["schema_version"] = 1,
                ["status"] = "Captured",
                ["scope"] = "IsolatedPreGameStreamedCampaignSourceScene_AuthoredNeutralPhysicalOccupancyInputsOnly",
                ["logical_operation_map_id"] = LogicalMapId,
                ["source_campaign_map_id"] = SourceMapId,
                ["source_scene_guid"] = SceneGuid,
                ["source_scene_path"] = scenePath,
                ["source_scene_sha256"] = sceneHash,
                ["source_scene_meta_sha256"] = sceneMetaHash,
                ["logical_map_config_path"] = MapConfigPath,
                ["logical_map_config_sha256"] = mapHash,
                ["unity_version"] = Application.unityVersion,
                ["source_streamed_in_isolated_world"] = true,
                ["gameplay_systems_ran"] = false,
                ["runtime_campaign_state_read"] = false,
                ["runtime_campaign_state_copied"] = false,
                ["faction_or_economy_values_captured"] = false,
                ["presentation_root_count"] = rootCount,
                ["grid"] = gridFact,
                ["occupancy_query"] = new JObject
                {
                    ["all"] = new JArray("UnitGrid", "UnitFootprint", "UnitSourcePrefabKey", "LocalTransform", "SceneTag"),
                    ["required_vehicle_marker"] = "OperationMapAuthoredVehiclePresentation",
                    ["identity_join"] = "UnitDetailedVisualReference.Root -> OperationMapEntityPresentationIdentity",
                    ["none"] = new JArray("Prefab", "Disabled"),
                    ["additional_occupancy_exclusions_verified_absent"] = new JArray("StaticGridBlocker", "RuntimeBuildingCombatTag", "UnitDeathAnimationComponent"),
                    ["reference_owner"] = "DynamicOccupancyRebuildSystem._trackedUnitsQuery"
                },
                ["cell_projection_policy"] = "Read-only reproduction of UnitGridSnapSystem.InitializeAuthoredVehicleGridJob: authored vehicles without UnitAirMovement or UnitGridInitialized use GridUtils.WorldToCell(LocalTransform.Position); either excluded component retains the captured baked UnitGrid cell. This is pre-simulation projection, not proof the owner ran.",
                ["neutralization_policy"] = "SkirmishWorldSetup.NormalizeScenery assigns faction 0 and removes AI/squad membership; no faction value was read or serialized here",
                ["vehicles"] = rows,
                ["vehicle_count"] = rows.Count,
                ["source_pins_sha256"] = pins,
                ["source_fit_validated"] = false,
                ["source_runtime_validated"] = false,
                ["scheduler_admitted"] = false,
                ["mission_ready"] = false
            };
            string outputPath = Path.Combine(outputDirectory, "authored-vehicle-occupancy-inputs.json");
            using (var file = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(file)) writer.Write(output.ToString(Formatting.Indented) + "\n");
            Debug.Log("[S073AuthoredVehicleOccupancyProjection] result=Captured vehicles=" + rows.Count + " sourceFit=0 ready=0");
        }

        private static void RequireSceneOwnership(EntityManager em, Entity entity, Entity sceneEntity, string sceneGuid)
        {
            if (!IsOwnedByScene(em, entity, sceneEntity, sceneGuid))
                throw new InvalidOperationException("Baked entity belongs to a different streamed source scene");
        }

        private static bool IsOwnedByScene(EntityManager em, Entity entity, Entity sceneEntity, string sceneGuid)
        {
            if (entity == Entity.Null || !em.Exists(entity) || !em.HasComponent<SceneTag>(entity)) return false;
            Entity section = em.GetSharedComponent<SceneTag>(entity).SceneEntity;
            if (section == Entity.Null || !em.Exists(section) || !em.HasComponent<SceneSectionData>(section) ||
                !em.HasComponent<SceneEntityReference>(section)) return false;
            SceneSectionData data = em.GetComponentData<SceneSectionData>(section);
            SceneEntityReference reference = em.GetComponentData<SceneEntityReference>(section);
            return data.SceneGUID.ToString() == sceneGuid && reference.SceneEntity == sceneEntity;
        }

        private static void WriteFailureDiagnostic(string outputDirectory, JObject diagnostic, int joinedRows)
        {
            diagnostic["kind"] = "S073AuthoredVehicleOccupancyProjectionFailureDiagnostic";
            diagnostic["schema_version"] = 1;
            diagnostic["scope"] = "IsolatedStreamedSourceScene_ComponentMembershipAndDirectVisualRootJoinOnly";
            diagnostic["expected_vehicle_rows"] = ExpectedVehicles;
            diagnostic["joined_vehicle_rows"] = joinedRows;
            diagnostic["source_state_imported"] = false;
            diagnostic["runtime_campaign_state_read"] = false;
            diagnostic["mission_ready"] = false;
            string path = Path.Combine(outputDirectory, "failure-diagnostic.json");
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(file)) writer.Write(diagnostic.ToString(Formatting.Indented) + "\n");
        }
    }
}
#endif
