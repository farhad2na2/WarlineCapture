using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    // A spatial-only source-host diagnostic entity. This marker carries provenance,
    // never a faction, source-unit gameplay key, command, or Campaign runtime state.
    internal struct S073NeutralOccupancyProjectionContributor : IComponentData
    {
        public FixedString128Bytes SourceGlobalObjectId;
        public FixedString128Bytes LogicalOperationMapId;
        public FixedString64Bytes SourcePrefabKey;
        public int PlacementIndex;
    }

    internal static class SourceS073NeutralOccupancyProjectionImporter
    {
        const string ProjectionRelative = "Logs/SkirmishProduction/s073-authored-vehicle-projection-20261101-r3/projection.json";
        const string InputsRelative = "Logs/SkirmishProduction/s073-authored-vehicle-projection-20261101-r3/authored-vehicle-occupancy-inputs.json";
        const string ReceiptRelative = "Logs/SkirmishProduction/s073-authored-vehicle-projection-20261101-r3/capture-receipt.json";
        const string EditorLogRelative = "Logs/SkirmishProduction/s073-authored-vehicle-projection-20261101-r3/editor.log";
        const string ProjectionSha = "79192b7cd114ebee545ad96fc4154408d3c76383b6fefa4e47c60d0353aba2f4";
        const string InputsSha = "f88f7701625ee60b4dcf1d18b7c2898c33ab6725012c28805f7b934456c933fd";
        const string ReceiptSha = "e48e27ddb6b4ae4b9c843fa619c976ad87e494d2e4742e066879611153aa2b14";
        const string EditorLogSha = "3451a63eb326645960345e4ef03830f1b096c72a52954eabe5db6ef40700c32d";
        const string LogicalMapId = "opmap.skirmish.industrial_basin";
        const string SourceMapId = "opmap.skirmish.desert_base_01";
        const string LogicalMapPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset";
        const string SourceScenePath = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
        const string SourceSceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";

        internal static bool IsOptedIn => Environment.GetEnvironmentVariable("WARLINE_S073_NEUTRAL_OCCUPANCY_PROJECTION") == "1";

        internal static JObject ImportAndRebuild(EntityManager em, Entity gridEntity, Entity mapRoot, Entity session,
            Entity startupPlan, string sourceRepositoryRoot, string assetRepositoryRoot)
        {
            if (!IsOptedIn)
                throw new InvalidOperationException("Neutral physical occupancy projection importer requires its explicit opt-in");
            if (em == null || !em.Exists(gridEntity) || !em.Exists(mapRoot) || !em.Exists(session) || !em.Exists(startupPlan))
                throw new InvalidDataException("Actual S073 source world, grid, map root, session, and startup plan required");

            string repo = Path.GetFullPath(sourceRepositoryRoot ?? throw new ArgumentNullException(nameof(sourceRepositoryRoot)));
            string assetRepo = Path.GetFullPath(assetRepositoryRoot ?? throw new ArgumentNullException(nameof(assetRepositoryRoot)));
            string artifactRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("WARLINE_S073_NEUTRAL_OCCUPANCY_PROJECTION_ARTIFACT_ROOT") ?? repo);
            JObject projection, inputs, captureReceipt;
            string projectionPath = PinnedArtifact(artifactRoot, ProjectionRelative, ProjectionSha);
            string inputsPath = PinnedArtifact(artifactRoot, InputsRelative, InputsSha);
            string receiptPath = PinnedArtifact(artifactRoot, ReceiptRelative, ReceiptSha);
            string editorLogPath = PinnedArtifact(artifactRoot, EditorLogRelative, EditorLogSha);
            projection = ReadObject(projectionPath);
            inputs = ReadObject(inputsPath);
            captureReceipt = ReadObject(receiptPath);
            ValidateCaptureReceipt(captureReceipt, inputsPath, editorLogPath);
            ValidateProjection(projection, inputs, inputsPath, assetRepo);
            ValidateRuntimeBinding(em, gridEntity, mapRoot, session, startupPlan, assetRepo, projection);

            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            DynamicOccupancyComponent occupancyBefore = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
            int cellCount = checked(grid.Width * grid.Height);
            if (!occupancyBefore.Occupied.IsCreated || occupancyBefore.Occupied.Length != cellCount || occupancyBefore.GridSize != cellCount)
                throw new InvalidDataException("Source grid occupancy is not initialized for the selected Campaign grid");
            bool[] beforeMask = new bool[cellCount];
            for (int i = 0; i < cellCount; i++) beforeMask[i] = occupancyBefore.Occupied.IsSet(i);

            JObject beforeRoster = CaptureSourceRoster(em);
            JObject beforeFlags = CaptureGuardFlags(em, mapRoot, session, startupPlan);
            if ((int?)beforeRoster["count"] != 24 || (bool?)beforeFlags["initial_spawn_completion_marked"] != true ||
                (string)beforeFlags["phase"] != SkirmishPhase.Preparing.ToString() ||
                (string)beforeFlags["outcome"] != SkirmishOutcome.None.ToString() ||
                (int?)beforeFlags["physical_source_validated"] != 0 || (int?)beforeFlags["simulation_active"] != 0)
                throw new InvalidDataException("Neutral projection requires the completed 24-unit Preparing source boundary with physical validation and simulation inactive");

            var projected = new bool[cellCount];
            var expectedRows = new JArray();
            foreach (JObject row in ((JArray)projection["vehicles"]).OfType<JObject>().OrderBy(x => (int)x["placement_index"]))
            {
                int2 cell = Int2(row["pre_simulation_effective_cell"], "projection effective cell");
                int2 size = Int2(row["footprint"], "projection footprint");
                var productionBounds = Navigation.GridOccupancyKernel.Bounds(grid.Width, grid.Height, cell.x, cell.y, size.x, size.y);
                var expectedCells = new List<int>();
                for (int y = productionBounds.MinZ; y < productionBounds.MaxZ; y++)
                for (int x = productionBounds.MinX; x < productionBounds.MaxX; x++)
                {
                    int index = y * grid.Width + x;
                    projected[index] = true;
                    expectedCells.Add(index);
                }
                if (!IntArrayEquals(row["covered_row_major_cells"], expectedCells) ||
                    !Int2Equals(row["clipped_bounds_min"], new int2(productionBounds.MinX, productionBounds.MinZ)) ||
                    !Int2Equals(row["clipped_bounds_max_exclusive"], new int2(productionBounds.MaxX, productionBounds.MaxZ)))
                    throw new InvalidDataException("Projection row does not match production occupancy kernel bounds: " + (string)row["source_global_object_id"]);

                Entity entity = em.CreateEntity();
                em.AddComponentData(entity, new UnitGrid { Cell = cell });
                em.AddComponentData(entity, new UnitFootprint { Size = size });
                em.AddComponentData(entity, new S073NeutralOccupancyProjectionContributor
                {
                    SourceGlobalObjectId = new FixedString128Bytes((string)row["source_global_object_id"]),
                    LogicalOperationMapId = new FixedString128Bytes(LogicalMapId),
                    SourcePrefabKey = new FixedString64Bytes((string)row["unit_source_prefab_key"]),
                    PlacementIndex = (int)row["placement_index"]
                });
                if (em.HasComponent<Faction>(entity) || em.HasComponent<UnitSourcePrefabKey>(entity) ||
                    em.HasComponent<RuntimeBuildingCombatTag>(entity) || em.HasComponent<StaticGridBlocker>(entity))
                    throw new InvalidDataException("Spatial projection entity unexpectedly carries gameplay, source-unit, or blocker state");
                expectedRows.Add(new JObject
                {
                    ["entity_index"] = entity.Index,
                    ["source_global_object_id"] = (string)row["source_global_object_id"],
                    ["placement_index"] = (int)row["placement_index"],
                    ["source_prefab_key_provenance_only"] = (string)row["unit_source_prefab_key"],
                    ["cell"] = new JArray(cell.x, cell.y),
                    ["footprint"] = new JArray(size.x, size.y),
                    ["faction_component_present"] = false,
                    ["unit_source_prefab_key_component_present"] = false,
                    ["covered_row_major_cells"] = new JArray(expectedCells)
                });
            }

            JObject afterRosterBeforeRebuild = CaptureSourceRoster(em);
            if (!JToken.DeepEquals(beforeRoster, afterRosterBeforeRebuild))
                throw new InvalidDataException("Neutral spatial import changed the original 24-unit source roster");
            JObject afterFlagsBeforeRebuild = CaptureGuardFlags(em, mapRoot, session, startupPlan);
            if (!JToken.DeepEquals(beforeFlags, afterFlagsBeforeRebuild))
                throw new InvalidDataException("Neutral spatial import changed initial, simulation, or physical-source flags");

            RunOriginalOccupancyOwner(em);
            DynamicOccupancyComponent occupancyAfter = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
            int actualCount = 0, expectedCount = 0, mismatchCount = 0, projectedOverlap = 0, projectedAdditional = 0;
            var actualCells = new JArray();
            var mismatches = new JArray();
            for (int i = 0; i < cellCount; i++)
            {
                bool actual = occupancyAfter.Occupied.IsSet(i);
                bool union = beforeMask[i] || projected[i];
                if (actual) { actualCount++; actualCells.Add(i); }
                if (union) expectedCount++;
                if (projected[i] && beforeMask[i]) projectedOverlap++;
                if (projected[i] && !beforeMask[i]) projectedAdditional++;
                if (actual != union)
                {
                    mismatchCount++;
                    if (mismatches.Count < 128) mismatches.Add(new JObject { ["cell_index"] = i, ["expected_union"] = union, ["actual"] = actual });
                }
            }

            JObject afterRoster = CaptureSourceRoster(em);
            JObject afterFlags = CaptureGuardFlags(em, mapRoot, session, startupPlan);
            if (!JToken.DeepEquals(beforeRoster, afterRoster) || !JToken.DeepEquals(beforeFlags, afterFlags))
                throw new InvalidDataException("Original owner rebuild changed source roster or guarded startup/admission flags");

            return new JObject
            {
                ["kind"] = "S073SourceNeutralPhysicalOccupancyProjectionImport",
                ["status"] = mismatchCount == 0 ? "Passed" : "ProjectionUnionMismatch",
                ["scope"] = "OptInSourceWorldSpatialProjection_OriginalOccupancyOwner_NoGameplayStateOrAdmission",
                ["source_state_imported"] = false,
                ["campaign_runtime_state_read_or_copied"] = false,
                ["mission_ready"] = false,
                ["physical_source_validated_before_and_after"] = false,
                ["scheduler_admitted"] = false,
                ["simulation_active_before_and_after"] = false,
                ["source_artifacts"] = new JObject
                {
                    ["projection"] = new JObject { ["path"] = projectionPath, ["sha256"] = ProjectionSha },
                    ["inputs"] = new JObject { ["path"] = inputsPath, ["sha256"] = InputsSha },
                    ["capture_receipt"] = new JObject { ["path"] = receiptPath, ["sha256"] = ReceiptSha },
                    ["editor_log"] = new JObject { ["path"] = editorLogPath, ["sha256"] = EditorLogSha },
                    ["verified_projection_source_pin_count"] = ((JObject)projection["source_pins_sha256"]).Count
                },
                ["map_and_grid"] = new JObject
                {
                    ["logical_operation_map_id"] = LogicalMapId,
                    ["source_campaign_map_id"] = SourceMapId,
                    ["source_scene_guid"] = SourceSceneGuid,
                    ["width"] = grid.Width, ["height"] = grid.Height,
                    ["cell_size"] = grid.CellSize,
                    ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z)
                },
                ["imported_entity_count"] = expectedRows.Count,
                ["imported_entities"] = expectedRows,
                ["component_contract"] = new JArray("UnitGrid", "UnitFootprint", "S073NeutralOccupancyProjectionContributor"),
                ["forbidden_components_absent"] = new JArray("Faction", "UnitSourcePrefabKey", "combat", "AI", "commands", "economy", "garrison", "Campaign runtime state"),
                ["original_source_roster"] = new JObject
                {
                    ["count_before"] = (int)beforeRoster["count"],
                    ["count_after"] = (int)afterRoster["count"],
                    ["unchanged"] = JToken.DeepEquals(beforeRoster, afterRoster),
                    ["source_faction_counts"] = beforeRoster["faction_counts"].DeepClone()
                },
                ["original_dynamic_occupancy_owner_invoked_after_import"] = true,
                ["occupancy_union_comparison"] = new JObject
                {
                    ["basis"] = "Observed pre-import source roster occupancy union verified projection kernel cells",
                    ["baseline_occupied_cell_count"] = beforeMask.Count(x => x),
                    ["projected_occupied_cell_count"] = projected.Count(x => x),
                    ["projected_overlap_with_baseline_cell_count"] = projectedOverlap,
                    ["projected_additional_cell_count"] = projectedAdditional,
                    ["expected_union_occupied_cell_count"] = expectedCount,
                    ["actual_after_owner_occupied_cell_count"] = actualCount,
                    ["mismatch_count"] = mismatchCount,
                    ["first_mismatches"] = mismatches,
                    ["actual_occupied_row_major_cells"] = actualCells
                },
                ["initial_and_admission_flags_before"] = beforeFlags,
                ["initial_and_admission_flags_after"] = afterFlags,
                ["raw_native_comparison"] = new JObject
                {
                    ["status"] = "ReportedSeparatelyByOccupancyOwnerControl",
                    ["source_state_imported"] = false,
                    ["readiness_claimed"] = false
                }
            };
        }

        static void ValidateCaptureReceipt(JObject receipt, string inputPath, string editorLogPath)
        {
            if ((string)receipt["status"] != "Captured" || (int?)receipt["wrapper_exit"] != 0 ||
                (bool?)receipt["required_marker_present"] != true || (bool?)receipt["mission_ready"] != false ||
                (string)receipt["scope"] != "IsolatedPregameAuthoredScene_NotRuntimeCampaignStateOrReadiness" ||
                (string)receipt["input_sha256"] != InputsSha || (string)receipt["editor_log_sha256"] != EditorLogSha ||
                Sha(inputPath) != InputsSha || Sha(editorLogPath) != EditorLogSha)
                throw new InvalidDataException("Qualified isolated r3 capture receipt, input hash, or editor log mismatch");
        }

        static void ValidateProjection(JObject projection, JObject inputs, string inputsPath, string assetRepo)
        {
            if ((string)projection["kind"] != "S073AuthoredNeutralPhysicalOccupancyProjection" ||
                (int?)projection["schema_version"] != 1 || (string)projection["status"] != "Projected" ||
                (string)projection["scope"] != "PreSimulationProjectionFromIsolatedBakedCampaignScene_NotRuntimeCaptureOrSourceValidation" ||
                (string)projection["logical_operation_map_id"] != LogicalMapId ||
                (string)projection["source_campaign_map_id"] != SourceMapId ||
                (string)projection["source_scene_guid"] != SourceSceneGuid ||
                (string)projection["logical_map_config_sha256"] != Sha(Path.Combine(assetRepo, LogicalMapPath)) ||
                (string)projection["source_scene_sha256"] != Sha(Path.Combine(assetRepo, SourceScenePath)) ||
                (string)projection["source_scene_meta_sha256"] != Sha(Path.Combine(assetRepo, SourceScenePath + ".meta")) ||
                (string)projection["capture_input_sha256"] != InputsSha ||
                (int?)projection["vehicle_count"] != 22 || (projection["vehicles"] as JArray)?.Count != 22 ||
                (int?)projection["occupied_cell_count"] != 109 ||
                (int?)projection["out_of_bounds_footprint_cell_references"] != 0 ||
                (int?)projection["vehicles_with_partially_clipped_footprints"] != 0 ||
                (int?)projection["vehicles_wholly_outside_grid"] != 0 ||
                (bool?)projection["source_state_imported"] != false || (bool?)projection["campaign_state_copied"] != false ||
                (bool?)projection["state_transfer"] != false || (bool?)projection["source_fit_validated"] != false ||
                (bool?)projection["scheduler_admitted"] != false || (bool?)projection["mission_ready"] != false)
                throw new InvalidDataException("r3 projection scope, source binding, rows, or non-readiness flags changed");

            if ((string)inputs["kind"] != "S073AuthoredVehicleOccupancyProjectionInputs" ||
                (int?)inputs["schema_version"] != 1 || (string)inputs["status"] != "Captured" ||
                (string)inputs["scope"] != "IsolatedPreGameStreamedCampaignSourceScene_AuthoredNeutralPhysicalOccupancyInputsOnly" ||
                (bool?)inputs["source_streamed_in_isolated_world"] != true || (bool?)inputs["gameplay_systems_ran"] != false ||
                (bool?)inputs["runtime_campaign_state_read"] != false || (bool?)inputs["runtime_campaign_state_copied"] != false ||
                (bool?)inputs["faction_or_economy_values_captured"] != false || (bool?)inputs["source_fit_validated"] != false ||
                (bool?)inputs["source_runtime_validated"] != false || (bool?)inputs["scheduler_admitted"] != false ||
                (bool?)inputs["mission_ready"] != false || (int?)inputs["vehicle_count"] != 22 ||
                (inputs["vehicles"] as JArray)?.Count != 22 ||
                (string)inputs["logical_operation_map_id"] != LogicalMapId ||
                (string)inputs["source_campaign_map_id"] != SourceMapId ||
                (string)inputs["source_scene_guid"] != SourceSceneGuid ||
                (string)inputs["source_scene_path"] != SourceScenePath ||
                (string)inputs["logical_map_config_path"] != LogicalMapPath ||
                (string)inputs["source_scene_sha256"] != Sha(Path.Combine(assetRepo, SourceScenePath)) ||
                (string)inputs["source_scene_meta_sha256"] != Sha(Path.Combine(assetRepo, SourceScenePath + ".meta")) ||
                (string)inputs["logical_map_config_sha256"] != Sha(Path.Combine(assetRepo, LogicalMapPath)) ||
                (string)projection["capture_input_sha256"] != Sha(inputsPath))
                throw new InvalidDataException("r3 isolated occupancy input provenance or neutralization gates changed");

            ValidateGridProjection(projection["grid"] as JObject, inputs["grid"] as JObject);
            ValidateSourcePins(projection["source_pins_sha256"] as JObject, inputs["source_pins_sha256"] as JObject, assetRepo);
            ValidateRows(projection, inputs);
        }

        static void ValidateGridProjection(JObject projectionGrid, JObject inputGrid)
        {
            foreach (JObject grid in new[] { projectionGrid, inputGrid })
                if (grid == null || (int?)grid["width"] != 2048 || (int?)grid["height"] != 1024 ||
                    (float?)grid["cell_size"] != 1f || !(grid["origin"] is JArray origin) || origin.Count != 3 || origin.Any(x => x.Value<float>() != 0f))
                    throw new InvalidDataException("Qualified r3 projection grid must be the exact 2048×1024, one-unit zero-origin source grid");
        }

        static void ValidateSourcePins(JObject projectionPins, JObject inputPins, string assetRepo)
        {
            if (projectionPins == null || inputPins == null || projectionPins.Count < 16 || inputPins.Count != projectionPins.Count)
                throw new InvalidDataException("Complete duplicated r3 source pin maps required");
            foreach (JProperty property in projectionPins.Properties())
            {
                if ((string)inputPins[property.Name] != (string)property.Value)
                    throw new InvalidDataException("r3 projection and input source pins differ: " + property.Name);
                string path = Path.GetFullPath(Path.Combine(assetRepo, property.Name.Replace('/', Path.DirectorySeparatorChar)));
                string root = assetRepo.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!path.StartsWith(root, StringComparison.Ordinal) ||
                    (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 || Sha(path) != (string)property.Value)
                    throw new InvalidDataException("r3 isolated projection source pin changed: " + property.Name);
            }
        }

        static void ValidateRows(JObject projection, JObject inputs)
        {
            var projectionRows = ((JArray)projection["vehicles"]).OfType<JObject>().ToDictionary(x => (string)x["source_global_object_id"], StringComparer.Ordinal);
            var inputRows = ((JArray)inputs["vehicles"]).OfType<JObject>().ToDictionary(x => (string)x["source_global_object_id"], StringComparer.Ordinal);
            if (projectionRows.Count != 22 || inputRows.Count != 22 || !projectionRows.Keys.OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(inputRows.Keys.OrderBy(x => x, StringComparer.Ordinal), StringComparer.Ordinal))
                throw new InvalidDataException("Unique exact 22-row source-object identity join required");

            var indices = new HashSet<int>();
            var union = new SortedSet<int>();
            foreach (var pair in projectionRows)
            {
                JObject projected = pair.Value, input = inputRows[pair.Key];
                int placementIndex = (int)projected["placement_index"];
                if (!indices.Add(placementIndex) || (string)input["source_operation_map_id"] != SourceMapId ||
                    (string)input["presentation_role"] != "GameplayVehicles" ||
                    (string)input["scene_guid"] != SourceSceneGuid ||
                    (string)input["unit_source_prefab_key"] != (string)projected["unit_source_prefab_key"] ||
                    (int?)input["placement_index"] != placementIndex ||
                    !Int2Equals(input["pre_simulation_effective_cell"], Int2(projected["pre_simulation_effective_cell"], "pre-simulation cell")) ||
                    !Int2Equals(input["kernel_footprint"], Int2(projected["footprint"], "footprint")) ||
                    (bool?)input["unit_grid_initialized_component_present"] != false)
                    throw new InvalidDataException("r3 source row changed or cannot join to its isolated input: " + pair.Key);
                foreach (JToken cell in (JArray)projected["covered_row_major_cells"]) union.Add(cell.Value<int>());
            }
            if (indices.Count != 22 || union.Count != 109 || !IntArrayEquals(projection["occupied_row_major_cells"], union))
                throw new InvalidDataException("r3 projection unique placement or 109-cell union evidence changed");
        }

        static void ValidateRuntimeBinding(EntityManager em, Entity gridEntity, Entity mapRoot, Entity session,
            Entity startupPlan, string assetRepo, JObject projection)
        {
            using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
            using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
            using var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (maps.CalculateEntityCount() != 1 || maps.GetSingletonEntity() != mapRoot ||
                matches.CalculateEntityCount() != 1 || matches.GetSingletonEntity() != session ||
                gameplay.CalculateEntityCount() != 1 || !em.HasComponent<InitialUnitsSpawnInitialized>(startupPlan))
                throw new InvalidDataException("Unique selected S073 map/session and completed original startup plan required");
            var active = em.GetComponentData<ActiveOperationMapComponent>(mapRoot);
            var metadata = em.GetComponentData<OperationMapMetadataComponent>(mapRoot);
            if (active.OperationMapId.ToString() != LogicalMapId || !metadata.Blob.IsCreated ||
                metadata.Blob.Value.OperationMapId.ToString() != LogicalMapId ||
                metadata.Blob.Value.SourceOperationMapId.ToString() != SourceMapId || metadata.PhysicalSourceValidated != 0)
                throw new InvalidDataException("Selected S073 source map root does not match the isolated r3 projection; physical validation must remain false");
            var grid = em.GetComponentData<GridConfig>(gridEntity);
            var projectedGrid = (JObject)projection["grid"];
            if (grid.Width != (int)projectedGrid["width"] || grid.Height != (int)projectedGrid["height"] ||
                grid.CellSize != (float)projectedGrid["cell_size"] || !math.all(grid.Origin == new float3(0f)))
                throw new InvalidDataException("Current source EntityManager grid differs from the isolated r3 projection grid");
            string mapPath = Path.GetFullPath(Path.Combine(assetRepo, LogicalMapPath));
            if (Sha(mapPath) != (string)projection["logical_map_config_sha256"])
                throw new InvalidDataException("Current logical map asset differs from the r3 projection binding");
            var match = em.GetComponentData<SkirmishMatchState>(session);
            RuntimeGameplayStateComponent runtime = gameplay.GetSingleton<RuntimeGameplayStateComponent>();
            if (match.ScenarioIndex != 3 || match.Seed != 104729 || match.Outcome != SkirmishOutcome.None ||
                runtime.SimulationActive != 0 || runtime.PlayRequested != 1)
                throw new InvalidDataException("Opt-in neutral projection requires the exact S073 pre-simulation source boundary");
        }

        static JObject CaptureSourceRoster(EntityManager em)
        {
            using var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitSourcePrefabKey>(), ComponentType.ReadOnly<UnitFootprint>() },
                None = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<Disabled>(), ComponentType.ReadOnly<RuntimeBuildingCombatTag>(), ComponentType.ReadOnly<StaticGridBlocker>() }
            });
            using var entities = query.ToEntityArray(Allocator.Temp);
            var rows = new JArray();
            foreach (Entity entity in entities.OrderBy(x => x.Index).ThenBy(x => x.Version))
            {
                var cell = em.GetComponentData<UnitGrid>(entity).Cell;
                var footprint = em.GetComponentData<UnitFootprint>(entity).Size;
                rows.Add(new JObject
                {
                    ["entity_index"] = entity.Index,
                    ["entity_version"] = entity.Version,
                    ["faction_id"] = em.GetComponentData<Faction>(entity).Id,
                    ["unit_source_prefab_key"] = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString(),
                    ["cell"] = new JArray(cell.x, cell.y),
                    ["footprint"] = new JArray(footprint.x, footprint.y)
                });
            }
            return new JObject { ["count"] = entities.Length, ["faction_counts"] = new JObject(rows.OfType<JObject>().GroupBy(x => (byte)x["faction_id"]).OrderBy(x => x.Key).Select(x => new JProperty(x.Key.ToString(), x.Count()))), ["entities"] = rows };
        }

        static JObject CaptureGuardFlags(EntityManager em, Entity mapRoot, Entity session, Entity startupPlan)
        {
            if (!em.Exists(mapRoot) || !em.Exists(session) || !em.Exists(startupPlan)) throw new InvalidDataException("Guard entities disappeared during neutral projection import");
            using var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (gameplay.CalculateEntityCount() != 1) throw new InvalidDataException("Unique S073 gameplay flags required");
            var match = em.GetComponentData<SkirmishMatchState>(session);
            var runtime = gameplay.GetSingleton<RuntimeGameplayStateComponent>();
            var metadata = em.GetComponentData<OperationMapMetadataComponent>(mapRoot);
            return new JObject
            {
                ["phase"] = match.Phase.ToString(), ["outcome"] = match.Outcome.ToString(),
                ["startup_failure"] = match.StartupFailure.ToString(), ["scenario_index"] = match.ScenarioIndex,
                ["seed"] = match.Seed, ["session_id"] = match.SessionId.ToString(),
                ["simulation_active"] = runtime.SimulationActive, ["play_requested"] = runtime.PlayRequested,
                ["physical_source_validated"] = metadata.PhysicalSourceValidated,
                ["initial_spawn_completion_marked"] = em.HasComponent<InitialUnitsSpawnInitialized>(startupPlan)
            };
        }

        static void RunOriginalOccupancyOwner(EntityManager em)
        {
            var state = new SystemState { EntityManager = em, Enabled = true };
            var owner = new DynamicOccupancyRebuildSystem();
            bool created = false, begun = false;
            try
            {
                state.Begin(); begun = true; owner.OnCreate(ref state); created = true; state.End(); begun = false;
                state.Begin(); begun = true;
                if (!state.Enabled || !state.RequiredQueriesPresent)
                    throw new InvalidDataException("Original DynamicOccupancyRebuildSystem query requirements failed after neutral projection import");
                owner.OnUpdate(ref state); state.End(); begun = false;
            }
            finally
            {
                if (begun) state.End();
                if (created) { state.Begin(); owner.OnDestroy(ref state); state.End(); }
            }
        }

        static string PinnedArtifact(string root, string relative, string expected)
        {
            string full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(prefix, StringComparison.Ordinal) || !File.Exists(full) ||
                (File.GetAttributes(full) & FileAttributes.ReparsePoint) != 0 || Sha(full) != expected)
                throw new InvalidDataException("Required isolated r3 projection artifact pin mismatch: " + relative);
            return full;
        }

        static JObject ReadObject(string path)
        {
            using var text = File.OpenText(path);
            using var reader = new Newtonsoft.Json.JsonTextReader(text) { MaxDepth = 64, DateParseHandling = Newtonsoft.Json.DateParseHandling.None };
            return JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }

        static string Sha(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        static int2 Int2(JToken token, string label)
        {
            if (!(token is JArray values) || values.Count != 2 || values.Any(x => x.Type != JTokenType.Integer))
                throw new InvalidDataException("Integer pair required for " + label);
            return new int2(values[0].Value<int>(), values[1].Value<int>());
        }

        static bool Int2Equals(JToken token, int2 expected) =>
            token is JArray values && values.Count == 2 && values[0].Value<int>() == expected.x && values[1].Value<int>() == expected.y;

        static bool IntArrayEquals(JToken token, IEnumerable<int> expected) =>
            token is JArray values && values.Count == expected.Count() && values.Select(x => x.Value<int>()).SequenceEqual(expected);
    }
}
