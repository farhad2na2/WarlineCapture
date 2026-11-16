#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class BuildingRuntimeProcessingCompositionSystemHelper
    {
        const string S073PreparingSurveyOutput = "WARLINE_S073_PREPARING_CANDIDATE_PAIR_SURVEY_OUTPUT";
        const string S073BarrackKey = "building_barrack";
        const string S073AmmoKey = "building_ammunition_depot";
        // Match the original owner's full bounded search for the 28-cell Barrack
        // footprint (28 * 4 = 112); smaller footprints remain owner-bounded.
        const int S073SurveySearchRadiusLimit = 112;

        [Serializable]
        sealed class S073CandidateSurveyRow
        {
            public string building_key;
            public string prefab_name;
            public string source_prefab_lookup_key;
            public int faction_id;
            public int source_entry_index;
            public int[] source_spawn_cell;
            public int[] source_origin_offset;
            public int[] preferred_origin;
            public bool owner_search_found;
            public int[] selected_origin;
            public int[] effective_rect;
            public int evaluated_original_predicate_count;
            public S073CandidateRejectionReasons rejection_reasons;
            public bool original_placement_predicate;
            public bool skirmish_level_predicate;
            public bool authored_building_mask_clear;
            public bool authored_road_and_sidewalk_mask_clear;
            public bool authored_water_mask_clear;
            public int grid_road_cells;
            public int authored_sidewalk_cells;
            public int authored_water_cells;
            public bool other_source_footprints_clear;
            public bool pair_nonoverlap;
            public bool selected_by_original_owner_search;
            public bool placement_committed;
        }

        [Serializable]
        sealed class S073CandidateRejectionReasons
        {
            // Independent failure totals can overlap. failure_mask_counts uses one
            // bit per predicate in the order documented below, so each evaluated
            // candidate is also represented exactly once by its combined failures.
            public int evaluated_candidates;
            public int original_owner_predicate_failed;
            public int foundation_level_failed;
            public int authored_building_mask_overlap;
            public int authored_road_or_sidewalk_mask_overlap;
            public int authored_water_mask_overlap;
            public int outside_grid;
            public int other_eight_source_footprint_overlap;
            public int reserved_target_pair_overlap;
            public int grid_road_cells_nonzero;
            public int[] failure_mask_counts = new int[256];
            public int other_eight_only_candidate_count;
            public int other_eight_only_candidate_detail_limit = 256;
            public bool other_eight_only_candidate_details_truncated;
            public S073OtherFootprintOnlyCandidate[] other_eight_only_candidates;
            public string[] failure_mask_bits =
            {
                "original_owner_predicate", "foundation_level", "authored_building_mask",
                "authored_road_or_sidewalk_mask", "authored_water_mask", "inside_grid",
                "other_eight_source_footprints", "reserved_target_pair"
            };
            public string accounting = "Independent predicate totals may overlap; failure_mask_counts[index] is an exact mutually exclusive count, where index bits follow failure_mask_bits and zero means every added survey predicate passed. Grid-road cells are reported separately and are not an extra survey predicate beyond original_owner_predicate.";
        }

        [Serializable]
        sealed class S073OtherFootprintOnlyCandidate
        {
            public int candidate_ordinal;
            public int failure_mask;
            public int[] origin;
            public int[] effective_rect;
            public bool original_owner_predicate;
            public bool foundation_level_predicate;
            public bool authored_building_mask_clear;
            public bool authored_road_or_sidewalk_mask_clear;
            public bool authored_water_mask_clear;
            public bool inside_grid;
            public int grid_road_cells;
            public bool reserved_pair_clear;
            public S073OverlappingSourceEntryRectangle[] overlapping_source_entry_rectangles;
        }

        [Serializable]
        sealed class S073OverlappingSourceEntryRectangle
        {
            public int source_entry_index;
            public int faction_id;
            public string building_key;
            public int[] effective_rect;
        }

        [Serializable]
        sealed class S073PreparingSurveyReceipt
        {
            public int schema_version = 1;
            public string kind = "S073PreparingOriginalOwnerCandidatePairSurvey";
            public string status;
            public string scope = "OptInPreparingPhaseOriginalOwnerSearchAndOriginalPredicate_WithCurrentSourceMasks_NoSpawn";
            public string world_name;
            public string world_sequence_number;
            public string session_id;
            public int scenario_index;
            public int seed;
            public string phase;
            public string plan_entity;
            public int grid_width;
            public int grid_height;
            public float grid_cell_size;
            public int search_radius_limit;
            public bool source_plan_match_unique;
            public bool source_masks_complete;
            public bool target_buildings_absent_before_survey;
            public bool candidates_nonoverlapping;
            public bool candidate_pair_proven;
            public bool ecs_gameplay_entities_added_or_changed_by_survey;
            public bool derived_placement_caches_may_be_refreshed;
            public bool placement_committed_by_survey;
            public bool readiness_claimed;
            public string failure_reason;
            public string[] source_pins_sha256;
            public S073SourceFootprintObservation[] other_source_footprints;
            public S073CandidateSurveyRow[] candidate_rows;
            public S073LogicalReauthoringSurvey logical_reauthoring_survey;
        }

        [Serializable]
        sealed class S073LogicalReauthoringSurvey
        {
            public string status;
            public string scope = "NonMutatingOriginalOwnerSearchForLogicalS073BuildingRelocation_NotSequentialOwnerAcceptance";
            public bool source_world_mutated;
            public bool placement_committed;
            public bool joint_owner_acceptance_proven;
            public bool source_fit_certified;
            public int refinery_only_ammo_candidate_count;
            public int eligible_ammo_other_footprint_only_candidate_count;
            public S073AmmoRelocationOption[] ammo_candidate_options;
            public int selected_barrack_source_entry_index;
            public string selected_barrack_source_key;
            public int[] selected_barrack_origin;
            public int[] selected_barrack_rect;
            public int selected_ammunition_candidate_ordinal;
            public int ammunition_source_entry_index;
            public string ammunition_source_key;
            public int[] selected_ammunition_origin;
            public int[] selected_ammunition_rect;
            public S073SourceFootprintObservation relocated_refinery_source;
            public int remaining_fixed_source_footprint_count;
            public S073LogicalRefineryCandidate refinery_search;
            public string failure_reason;
        }

        [Serializable]
        sealed class S073AmmoRelocationOption
        {
            public int candidate_ordinal;
            public int failure_mask;
            public int[] origin;
            public int[] effective_rect;
            public bool original_owner_predicate;
            public bool foundation_level_predicate;
            public bool authored_building_mask_clear;
            public bool authored_road_or_sidewalk_mask_clear;
            public bool authored_water_mask_clear;
            public bool inside_grid;
            public int grid_road_cells;
            public bool clears_selected_barrack_rect;
            public bool overlaps_only_relocatable_faction_two_refinery;
            public bool eligible;
            public S073OverlappingSourceEntryRectangle[] overlapping_source_entry_rectangles;
        }

        [Serializable]
        sealed class S073LogicalRefineryCandidate
        {
            public int source_entry_index;
            public string source_building_key;
            public int[] preferred_origin;
            public bool original_owner_search_found;
            public int evaluated_original_predicate_count;
            public int[] selected_origin;
            public int[] effective_rect;
            public bool original_owner_predicate;
            public bool foundation_level_predicate;
            public bool authored_building_mask_clear;
            public bool authored_road_or_sidewalk_mask_clear;
            public bool authored_water_mask_clear;
            public bool inside_grid;
            public int grid_road_cells;
            public int authored_sidewalk_cells;
            public int authored_water_cells;
            public bool clears_barrack_and_ammunition_rectangles;
            public bool clears_remaining_seven_source_rectangles;
            public bool placement_committed;
        }

        [Serializable]
        sealed class S073SourceFootprintObservation
        {
            public int source_entry_index;
            public int faction_id;
            public string building_key;
            public int[] preferred_origin;
            public int[] effective_rect;
            public bool rotation_vertical;
        }

        readonly struct S073SourceFootprint
        {
            public readonly int EntryIndex;
            public readonly int FactionId;
            public readonly string Key;
            public readonly RectInt Rect;
            public readonly S073InitialSourceRow Source;

            public S073SourceFootprint(int entryIndex, int factionId, string key, RectInt rect, S073InitialSourceRow source)
            {
                EntryIndex = entryIndex;
                FactionId = factionId;
                Key = key;
                Rect = rect;
                Source = source;
            }
        }

        readonly struct S073InitialSourceRow
        {
            public readonly int2 SpawnCell;
            public readonly int2 OriginOffset;
            public readonly Vector2Int Preferred;
            public readonly string Key;
            public readonly int EntryIndex;
            public readonly string SourcePrefabLookupKey;

            public S073InitialSourceRow(string key, string sourcePrefabLookupKey, int2 spawnCell, int2 originOffset, int entryIndex)
            {
                Key = key;
                SourcePrefabLookupKey = sourcePrefabLookupKey;
                SpawnCell = spawnCell;
                OriginOffset = originOffset;
                Preferred = new Vector2Int(spawnCell.x + originOffset.x, spawnCell.y + originOffset.y);
                EntryIndex = entryIndex;
            }
        }

        void TrySurveyS073PreparingInitialPair(EntityManager em, BuildingRuntimeSpawnRequest request,
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
            BuildingRuntimeSpawnCompositionSystemHelper.Context runtimeSpawnContext)
        {
            string output = Environment.GetEnvironmentVariable(S073PreparingSurveyOutput);
            if (string.IsNullOrWhiteSpace(output) || File.Exists(output)) return;

            using var matches = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>());
            if (matches.CalculateEntityCount() != 1) return;
            SkirmishMatchState match = matches.GetSingleton<SkirmishMatchState>();
            if (match.ScenarioIndex != SkirmishPresetConfig.IndustrialBasinScenarioIndex || match.Seed != 104729 ||
                match.Phase != SkirmishPhase.Preparing || request.RequestKind != BuildingRuntimeSpawnRequest.KindBuilding ||
                request.FactionId != 2 || request.HasOwnerFaction == 0 ||
                BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(request.BuildingId.ToString()) != S073BarrackKey)
                return;

            S073PreparingSurveyReceipt receipt = new()
            {
                status = "FailedClosed",
                world_name = em.World.Name,
                world_sequence_number = em.WorldUnmanaged.SequenceNumber.ToString(),
                session_id = match.SessionId.ToString(),
                scenario_index = match.ScenarioIndex,
                seed = match.Seed,
                phase = match.Phase.ToString(),
                plan_entity = request.PlanEntity == Entity.Null ? "null" : request.PlanEntity.Index + ":" + request.PlanEntity.Version,
                search_radius_limit = S073SurveySearchRadiusLimit,
                source_pins_sha256 = HashS073SurveySourcePins(),
                candidate_rows = Array.Empty<S073CandidateSurveyRow>(),
                readiness_claimed = false,
                ecs_gameplay_entities_added_or_changed_by_survey = false,
                derived_placement_caches_may_be_refreshed = true,
                placement_committed_by_survey = false
            };

            try
            {
                if (request.PlanEntity == Entity.Null || !em.Exists(request.PlanEntity) ||
                    !em.HasComponent<InitialUnitsSpawnConfig>(request.PlanEntity) ||
                    !em.HasBuffer<InitialUnitsFactionSpawnEntry>(request.PlanEntity) ||
                    !em.HasBuffer<InitialUnitsFactionBuildingSpawnEntry>(request.PlanEntity))
                    throw new InvalidOperationException("Original legacy initial-spawn plan buffers are incomplete");

                if (!TryResolveS073InitialSourcePair(em, request.PlanEntity, out var barrackSource,
                    out var ammoSource, out string sourceFailure))
                    throw new InvalidOperationException(sourceFailure);
                receipt.source_plan_match_unique = true;
                if (request.PlanEntity == Entity.Null ||
                    BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(request.BuildingId.ToString()) != barrackSource.Key ||
                    !new Vector2Int(request.PreferredOrigin.x, request.PreferredOrigin.y).Equals(barrackSource.Preferred))
                    throw new InvalidOperationException("Trigger request does not match the verified S073 Barrack source row");

                if (HasExistingS073EnemyTarget(em))
                    throw new InvalidOperationException("Target faction-2 Barrack/Ammo already exists; no non-mutating pair survey is safe");
                receipt.target_buildings_absent_before_survey = true;

                if (definitionSystem == null || runtimeSpawnSystem == null || runtimeSpawnContext.TryGetGridData == null ||
                    runtimeSpawnContext.GetPlacementFootprint == null || runtimeSpawnContext.GetEffectivePlacementRect == null ||
                    runtimeSpawnContext.IsPlacementValid == null ||
                    !runtimeSpawnContext.TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads,
                        out DynamicBlockerComponent blockers))
                    throw new InvalidOperationException("Original owner grid and placement delegates are unavailable");

                using var surfaces = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
                if (surfaces.CalculateEntityCount() != 1)
                    throw new InvalidOperationException("Unique current MapSurfaceComponent is required");
                MapSurfaceComponent surface = surfaces.GetSingleton<MapSurfaceComponent>();
                long cellCount = (long)grid.Width * grid.Height;
                if (grid.Width <= 0 || grid.Height <= 0 || cellCount > 4194304 ||
                    surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated ||
                    surface.Dimensions.x != grid.Width || surface.Dimensions.y != grid.Height)
                    throw new InvalidOperationException("Complete matching original grid/surface data is required");

                _skirmishRoads.Ensure(em, surfaces, grid);
                _skirmishBuildings.Ensure(em, grid);
                bool[] sidewalks = _skirmishRoads.GetSidewalks();
                bool[] waters = _skirmishRoads.GetWater();
                if (!_skirmishRoads.IsReady || !_skirmishBuildings.IsReady || sidewalks == null || waters == null ||
                    sidewalks.Length != cellCount || waters.Length != cellCount)
                    throw new InvalidOperationException("Current authored road/sidewalk/water/building masks are incomplete");
                receipt.source_masks_complete = true;
                receipt.grid_width = grid.Width;
                receipt.grid_height = grid.Height;
                receipt.grid_cell_size = grid.CellSize;

                if (!TryResolveConfiguredBuildingDefinition(definitionSystem, S073BarrackKey, out BuildingDefinition barrackDefinition) ||
                    !TryResolveConfiguredBuildingDefinition(definitionSystem, S073AmmoKey, out BuildingDefinition ammoDefinition) ||
                    barrackDefinition?.Prefab == null || ammoDefinition?.Prefab == null)
                    throw new InvalidOperationException("Both current configured target definitions are required");

                if (!TryResolveS073OtherSourceFootprints(em, request.PlanEntity, runtimeSpawnContext, definitionSystem,
                    grid, barrackSource, ammoSource, out S073SourceFootprint[] otherSourceFootprints,
                    out S073SourceFootprintObservation[] sourceFootprintEvidence, out string footprintFailure))
                    throw new InvalidOperationException("All eight non-target source-request footprints must resolve exactly: " + footprintFailure);
                receipt.other_source_footprints = sourceFootprintEvidence;

                var rows = new S073CandidateSurveyRow[2];
                if (!SurveyS073Candidate(runtimeSpawnSystem, runtimeSpawnContext, barrackDefinition, barrackSource,
                    grid, roads, blockers, surface, sidewalks, waters, otherSourceFootprints, null,
                    out rows[0], out string barrackFailure))
                    throw new InvalidOperationException("Barrack original search found no fully valid candidate within the fixed bound: " + barrackFailure);
                receipt.candidate_rows = rows;

                RectInt barrackRect = new RectInt(rows[0].effective_rect[0], rows[0].effective_rect[1],
                    rows[0].effective_rect[2], rows[0].effective_rect[3]);
                // First preserve the original independent search. If it fails only
                // because an authored faction-2 refinery overlaps otherwise valid
                // Ammo candidates, a separately labeled read-only logical survey
                // may ask the original owner to relocate that refinery around a
                // selected Ammo rectangle, the Barrack rectangle, and seven fixed
                // source rectangles. It never commits either move.
                if (!SurveyS073Candidate(runtimeSpawnSystem, runtimeSpawnContext, ammoDefinition, ammoSource,
                    grid, roads, blockers, surface, sidewalks, waters, otherSourceFootprints, null,
                    out rows[1], out string ammoFailure))
                {
                    receipt.candidate_rows = rows;
                    receipt.logical_reauthoring_survey = SurveyS073LogicalReauthoring(
                        runtimeSpawnSystem, runtimeSpawnContext, definitionSystem, grid, roads, blockers,
                        surface, sidewalks, waters, barrackSource, ammoSource, barrackRect,
                        otherSourceFootprints, rows[1].rejection_reasons);
                    throw new InvalidOperationException("Ammo original search found no individually valid candidate disjoint from all eight original source footprints: " + ammoFailure);
                }

                receipt.candidate_rows = rows;
                receipt.candidates_nonoverlapping = !barrackRect.Overlaps(new RectInt(rows[1].effective_rect[0],
                    rows[1].effective_rect[1], rows[1].effective_rect[2], rows[1].effective_rect[3]));
                // The second search uses the unmodified original owner context. Rect
                // separation alone does not prove that the owner would accept both
                // placements after the first is committed/reserved. Preserve the
                // individual candidate observations, but fail closed on joint proof.
                receipt.candidate_pair_proven = false;
                receipt.status = "RecordedIndependentOriginalOwnerCandidates_JointPairUnproven";
                receipt.failure_reason = "Joint acceptance is unproven: the first candidate was not reserved in original owner state";
            }
            catch (Exception ex)
            {
                receipt.failure_reason = ex.GetType().Name + ": " + ex.Message;
                Debug.LogError("[S073PreparingCandidatePairSurvey] result=FailedClosed reason=" + receipt.failure_reason);
            }

            try
            {
                using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using var writer = new StreamWriter(stream);
                writer.WriteLine(JsonUtility.ToJson(receipt));
                Debug.Log("[S073PreparingCandidatePairSurvey] result=" + receipt.status + " path=" + output +
                    " candidates=" + (receipt.candidate_rows == null ? 0 : receipt.candidate_rows.Length) +
                    " readinessClaimed=0 placementCommitted=0");
            }
            catch (Exception ex)
            {
                // Diagnostic output must never change the original request path.
                Debug.LogError("[S073PreparingCandidatePairSurvey] output=result=Failed error=" + ex.Message);
            }
        }

        // Separate opt-in exhaustive audit of every origin in the existing native
        // Barrack ring-search bound. The validation delegate always returns false,
        // so the production owner search traverses its full domain without placing
        // a building or changing the original request's result.
        void TrySurveyS073PreparingBarrackExhaustive(EntityManager em, BuildingRuntimeSpawnRequest request,
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
            BuildingRuntimeSpawnCompositionSystemHelper.Context runtimeSpawnContext)
        {
            TrySurveyS073PreparingBarrackAtCenter(em, request, definitionSystem, runtimeSpawnSystem,
                runtimeSpawnContext, "WARLINE_S073_PREPARING_BARRACK_EXHAUSTIVE_SURVEY_OUTPUT", false);
        }

        void TrySurveyS073PreparingBarrackFromFactionTwoSpawn(EntityManager em, BuildingRuntimeSpawnRequest request,
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
            BuildingRuntimeSpawnCompositionSystemHelper.Context runtimeSpawnContext)
        {
            TrySurveyS073PreparingBarrackAtCenter(em, request, definitionSystem, runtimeSpawnSystem,
                runtimeSpawnContext, "WARLINE_S073_PREPARING_BARRACK_SPAWN_CENTER_SURVEY_OUTPUT", true);
        }

        void TrySurveyS073PreparingBarrackAtCenter(EntityManager em, BuildingRuntimeSpawnRequest request,
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
            BuildingRuntimeSpawnCompositionSystemHelper.Context runtimeSpawnContext,
            string outputKey, bool centerAtFactionTwoSpawn)
        {
            const string scope = "CurrentS073Plan_OriginalOwnerBoundedRingSearch_ExhaustiveReadOnly_NoSpawn";
            const int searchRadius = 112;
            string configuredOutput = Environment.GetEnvironmentVariable(outputKey);
            if (string.IsNullOrWhiteSpace(configuredOutput)) return;

            string output = Path.GetFullPath(configuredOutput);
            if (!output.StartsWith("/private/tmp/", StringComparison.Ordinal) || File.Exists(output)) return;

            var result = new JObject
            {
                ["kind"] = "S073PreparingBarrackExhaustiveCandidateSurvey",
                ["schema_version"] = 1,
                ["status"] = "FailedClosed",
                ["scope"] = scope,
                ["search_radius_limit"] = searchRadius,
                ["candidate_domain_count_by_original_ring_bound"] = 1 + 4 * searchRadius * (searchRadius + 1),
                ["source_fit_validated"] = false,
                ["scheduler_admitted"] = false,
                ["mission_ready"] = false,
                ["player_readiness"] = false,
                ["placement_committed_by_survey"] = false,
                ["ecs_gameplay_entities_added_or_changed_by_survey"] = false,
                ["derived_placement_caches_may_be_refreshed"] = true,
                ["valid_candidate_origins"] = new JArray(),
                ["failure_reason"] = null
            };

            try
            {
                using var matches = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>());
                if (matches.CalculateEntityCount() != 1) return;
                SkirmishMatchState match = matches.GetSingleton<SkirmishMatchState>();
                if (match.ScenarioIndex != SkirmishPresetConfig.IndustrialBasinScenarioIndex ||
                    match.Seed != 104729 || match.Phase != SkirmishPhase.Preparing ||
                    request.RequestKind != BuildingRuntimeSpawnRequest.KindBuilding || request.FactionId != 2 ||
                    request.HasOwnerFaction == 0 ||
                    BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(request.BuildingId.ToString()) != S073BarrackKey)
                    return; // Other startup requests must not consume the one-shot survey output path.
                if (request.PlanEntity == Entity.Null || !em.Exists(request.PlanEntity) ||
                    !em.HasBuffer<InitialUnitsFactionSpawnEntry>(request.PlanEntity) ||
                    !em.HasBuffer<InitialUnitsFactionBuildingSpawnEntry>(request.PlanEntity))
                    throw new InvalidOperationException("Original S073 initial building plan buffers required");
                if (HasExistingS073EnemyTarget(em))
                    throw new InvalidOperationException("Faction-2 Barrack/Ammo already exists; survey must precede target placement");

                var factionRows = em.GetBuffer<InitialUnitsFactionSpawnEntry>(request.PlanEntity, true);
                int factionOneCount = 0, factionTwoCount = 0;
                int2 factionOneSpawn = default, factionTwoSpawn = default;
                foreach (InitialUnitsFactionSpawnEntry faction in factionRows)
                {
                    if (faction.FactionId == 1) { factionOneCount++; factionOneSpawn = faction.SpawnCell; }
                    if (faction.FactionId == 2) { factionTwoCount++; factionTwoSpawn = faction.SpawnCell; }
                }
                if (factionOneCount != 1 || factionTwoCount != 1)
                    throw new InvalidOperationException("Unique player faction-1 and enemy faction-2 source spawns required");

                var sourceRows = em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(request.PlanEntity, true);
                int barrackCount = 0, barrackIndex = -1, playerBarrackCount = 0, playerBarrackIndex = -1;
                InitialUnitsFactionBuildingSpawnEntry barrackEntry = default;
                InitialUnitsFactionBuildingSpawnEntry playerBarrackEntry = default;
                for (int i = 0; i < sourceRows.Length; i++)
                {
                    InitialUnitsFactionBuildingSpawnEntry source = sourceRows[i];
                    if (BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(source.PrefabLookupKey.ToString()) != S073BarrackKey)
                        continue;
                    if (source.FactionId == 2)
                    {
                        barrackCount++;
                        barrackIndex = i;
                        barrackEntry = source;
                    }
                    else if (source.FactionId == 1)
                    {
                        playerBarrackCount++;
                        playerBarrackIndex = i;
                        playerBarrackEntry = source;
                    }
                }
                if (barrackCount != 1 || playerBarrackCount != 1)
                    throw new InvalidOperationException("Unique faction-1 player and faction-2 enemy Barrack rows in current source plan required");
                var barrackSource = new S073InitialSourceRow(S073BarrackKey, barrackEntry.PrefabLookupKey.ToString(),
                    factionTwoSpawn, barrackEntry.OriginOffset, barrackIndex);
                var playerBarrackSource = new S073InitialSourceRow(S073BarrackKey, playerBarrackEntry.PrefabLookupKey.ToString(),
                    factionOneSpawn, playerBarrackEntry.OriginOffset, playerBarrackIndex);
                if (request.PlanEntity == Entity.Null || request.PreferredOrigin.x != barrackSource.Preferred.x ||
                    request.PreferredOrigin.y != barrackSource.Preferred.y)
                    throw new InvalidOperationException("Original Barrack request differs from current-plan source offset");
                result["session_id"] = match.SessionId.ToString();
                result["world_sequence_number"] = em.WorldUnmanaged.SequenceNumber.ToString();
                result["scenario_index"] = match.ScenarioIndex;
                result["seed"] = match.Seed;
                result["phase"] = match.Phase.ToString();
                result["plan_entity"] = request.PlanEntity.Index + ":" + request.PlanEntity.Version;
                result["source_spawn_cells"] = new JObject
                {
                    ["faction_1_player"] = new JArray(factionOneSpawn.x, factionOneSpawn.y),
                    ["faction_2_enemy"] = new JArray(factionTwoSpawn.x, factionTwoSpawn.y)
                };
                result["barrack_source_row"] = new JObject
                {
                    ["entry_index"] = barrackIndex,
                    ["prefab_lookup_key"] = barrackEntry.PrefabLookupKey.ToString(),
                    ["origin_offset"] = new JArray(barrackEntry.OriginOffset.x, barrackEntry.OriginOffset.y),
                    ["preferred_origin"] = new JArray(barrackSource.Preferred.x, barrackSource.Preferred.y)
                };
                Vector2Int surveyCenter = centerAtFactionTwoSpawn
                    ? new Vector2Int(factionTwoSpawn.x, factionTwoSpawn.y)
                    : barrackSource.Preferred;
                result["survey_center"] = new JObject
                {
                    ["kind"] = centerAtFactionTwoSpawn ? "Faction2SourceSpawnCell_DiagnosticOnly" : "CurrentBarrackSourcePreferredOrigin",
                    ["cell"] = new JArray(surveyCenter.x, surveyCenter.y),
                    ["exact_faction2_source_spawn_cell"] = new JArray(factionTwoSpawn.x, factionTwoSpawn.y),
                    ["actual_authored_barrack_request_preferred_origin"] = new JArray(barrackSource.Preferred.x, barrackSource.Preferred.y),
                    ["matches_actual_authored_barrack_request_origin"] = surveyCenter == barrackSource.Preferred,
                    ["source_request_or_offset_modified"] = false,
                    ["survey_only_center"] = true
                };
                result["player_barrack_source_row"] = new JObject
                {
                    ["entry_index"] = playerBarrackIndex,
                    ["prefab_lookup_key"] = playerBarrackEntry.PrefabLookupKey.ToString(),
                    ["origin_offset"] = new JArray(playerBarrackEntry.OriginOffset.x, playerBarrackEntry.OriginOffset.y),
                    ["preferred_origin"] = new JArray(playerBarrackSource.Preferred.x, playerBarrackSource.Preferred.y)
                };

                if (definitionSystem == null || runtimeSpawnSystem == null ||
                    runtimeSpawnContext.TryGetGridData == null || runtimeSpawnContext.GetPlacementFootprint == null ||
                    runtimeSpawnContext.GetEffectivePlacementRect == null || runtimeSpawnContext.IsPlacementValid == null ||
                    !runtimeSpawnContext.TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads,
                        out DynamicBlockerComponent blockers))
                    throw new InvalidOperationException("Original owner grid and placement delegates unavailable");
                using var surfaces = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
                if (surfaces.CalculateEntityCount() != 1)
                    throw new InvalidOperationException("Unique current map surface required");
                MapSurfaceComponent surface = surfaces.GetSingleton<MapSurfaceComponent>();
                long cellCount = (long)grid.Width * grid.Height;
                if (grid.Width <= 0 || grid.Height <= 0 || cellCount > 4194304 || grid.CellSize <= 0 ||
                    surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated ||
                    surface.Dimensions.x != grid.Width || surface.Dimensions.y != grid.Height)
                    throw new InvalidOperationException("Complete matching source grid/surface required");

                _skirmishRoads.Ensure(em, surfaces, grid);
                _skirmishBuildings.Ensure(em, grid);
                bool[] sidewalks = _skirmishRoads.GetSidewalks();
                bool[] waters = _skirmishRoads.GetWater();
                if (!_skirmishRoads.IsReady || !_skirmishBuildings.IsReady || sidewalks == null || waters == null ||
                    sidewalks.Length != cellCount || waters.Length != cellCount || roads.Length != cellCount ||
                    blockers.GridSize != cellCount || !blockers.Blocked.IsCreated || blockers.Blocked.Length != cellCount ||
                    !blockers.Counts.IsCreated || blockers.Counts.Length != cellCount ||
                    !blockers.FriendlyPassFactionIds.IsCreated || blockers.FriendlyPassFactionIds.Length != cellCount ||
                    !RuntimeGridBlockerEpoch.IsCommitted(grid, blockers))
                    throw new InvalidOperationException("Complete current authored road/sidewalk/water/building masks required");
                result["source_masks_complete"] = true;
                result["grid"] = new JObject { ["width"] = grid.Width, ["height"] = grid.Height,
                    ["cell_size"] = grid.CellSize, ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z) };
                using (var mapMetadata = em.CreateEntityQuery(ComponentType.ReadOnly<OperationMapMetadataComponent>()))
                {
                    if (mapMetadata.CalculateEntityCount() != 1)
                        throw new InvalidOperationException("Unique current operation-map metadata required");
                    OperationMapMetadataComponent metadata = mapMetadata.GetSingleton<OperationMapMetadataComponent>();
                    if (!metadata.Blob.IsCreated)
                        throw new InvalidOperationException("Current operation-map source identity metadata unavailable");
                    if (!metadata.Blob.Value.OperationMapId.Equals("opmap.skirmish.industrial_basin") ||
                        !metadata.Blob.Value.SourceOperationMapId.Equals("opmap.skirmish.desert_base_01"))
                        throw new InvalidOperationException("Current Industrial Basin / Desert Base Campaign source binding required");
                    result["operation_map_source_hashes"] = new JObject
                    {
                        ["operation_map_id"] = metadata.Blob.Value.OperationMapId.ToString(),
                        ["source_operation_map_id"] = metadata.Blob.Value.SourceOperationMapId.ToString(),
                        ["source_identity_hash"] = metadata.Blob.Value.SourceIdentityHash.ToString(),
                        ["source_content_hash"] = metadata.Blob.Value.SourceContentHash.ToString()
                    };
                }

                if (!TryResolveConfiguredBuildingDefinition(definitionSystem, S073BarrackKey, out BuildingDefinition definition) ||
                    definition?.Prefab == null)
                    throw new InvalidOperationException("Current configured Barrack definition required");
                Vector2Int footprint = runtimeSpawnContext.GetPlacementFootprint(definition, false);
                if (footprint.x != 28 || footprint.y != 15)
                    throw new InvalidOperationException("Current configured Barrack footprint differs from original 28x15 search bound");
                RectInt playerBaseRect = runtimeSpawnContext.GetEffectivePlacementRect(definition,
                    playerBarrackSource.Preferred, grid, false);
                RectInt enemyBaseRect = runtimeSpawnContext.GetEffectivePlacementRect(definition,
                    barrackSource.Preferred, grid, false);
                double playerBaseCenterX = playerBaseRect.x + playerBaseRect.width * 0.5;
                double playerBaseCenterY = playerBaseRect.y + playerBaseRect.height * 0.5;
                double enemyBaseCenterX = enemyBaseRect.x + enemyBaseRect.width * 0.5;
                double enemyBaseCenterY = enemyBaseRect.y + enemyBaseRect.height * 0.5;
                result["base_centers"] = new JObject
                {
                    ["faction_1_player_barrack_center_cell"] = new JArray(playerBaseCenterX, playerBaseCenterY),
                    ["faction_2_enemy_barrack_source_center_cell"] = new JArray(enemyBaseCenterX, enemyBaseCenterY)
                };

                var fixedFootprints = new List<S073SourceFootprint>(sourceRows.Length - 1);
                var fixedEvidence = new JArray();
                for (int i = 0; i < sourceRows.Length; i++)
                {
                    if (i == barrackIndex) continue;
                    InitialUnitsFactionBuildingSpawnEntry source = sourceRows[i];
                    int2 spawn = source.FactionId == 1 ? factionOneSpawn :
                        source.FactionId == 2 ? factionTwoSpawn : throw new InvalidOperationException("Unexpected source faction at entry " + i);
                    string keyText = source.PrefabLookupKey.ToString();
                    string key = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(keyText);
                    if (string.IsNullOrWhiteSpace(key) ||
                        !TryResolveConfiguredBuildingDefinition(definitionSystem, key, out BuildingDefinition sourceDefinition) ||
                        sourceDefinition?.Prefab == null)
                        throw new InvalidOperationException("Current definition unresolved for other source building " + i);
                    Vector2Int preferred = new(spawn.x + source.OriginOffset.x, spawn.y + source.OriginOffset.y);
                    RectInt rect = runtimeSpawnContext.GetEffectivePlacementRect(sourceDefinition, preferred, grid, false);
                    fixedFootprints.Add(new S073SourceFootprint(i, source.FactionId, key,
                        rect, new S073InitialSourceRow(key, keyText, spawn, source.OriginOffset, i)));
                    fixedEvidence.Add(new JObject { ["entry_index"] = i, ["faction_id"] = source.FactionId,
                        ["building_key"] = key, ["preferred_origin"] = new JArray(preferred.x, preferred.y),
                        ["effective_rect"] = new JArray(rect.x, rect.y, rect.width, rect.height) });
                }
                if (fixedFootprints.Count != 9)
                    throw new InvalidOperationException("Expected all nine non-Barrack current-plan source footprints");
                result["other_nine_source_footprints"] = fixedEvidence;

                int entityOrderBefore = em.EntityOrderVersion;
                JObject sourcePinsBefore = HashS073ExhaustiveSurveySourcePins();
                var validRows = (JArray)result["valid_candidate_origins"];
                int candidateVisits = 0, cachedInvalidCandidateCount = 0;
                int originalPredicateCalls = 0;
                int ownerRejectedCount = 0, levelRejectedCount = 0, buildingMaskRejectedCount = 0;
                int roadMaskRejectedCount = 0, waterMaskRejectedCount = 0, boundsRejectedCount = 0;
                int otherSourceOverlapCount = 0;
                double farthestFromPlayer = double.NegativeInfinity;
                double nearestToEnemy = double.PositiveInfinity;
                JObject selectedCandidate = null;

                var surveyContext = runtimeSpawnContext.WithPlacementValidation((candidateDefinition, origin,
                    candidateFootprint, rotateVertical, candidateGrid, candidateRoads, candidateBlockers) =>
                {
                    originalPredicateCalls++;
                    RectInt rect = runtimeSpawnContext.GetEffectivePlacementRect(candidateDefinition, origin,
                        candidateGrid, rotateVertical);
                    bool ownerValid = runtimeSpawnContext.IsPlacementValid(candidateDefinition, origin,
                        candidateFootprint, rotateVertical, candidateGrid, candidateRoads, candidateBlockers);
                    bool levelValid = BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(surface, rect);
                    bool buildingClear = !_skirmishBuildings.Overlaps(candidateGrid, rect.position, rect.size);
                    bool roadAndSidewalkClear = !_skirmishRoads.Overlaps(candidateGrid, rect.position, rect.size);
                    bool waterClear = !_skirmishRoads.OverlapsWater(candidateGrid, rect.position, rect.size);
                    bool within = BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(
                        rect.position, rect.size, candidateGrid);
                    int gridRoadCells = 0, sidewalkCells = 0, waterCells = 0;
                    if (within)
                    {
                        for (int y = rect.yMin; y < rect.yMax; y++)
                        for (int x = rect.xMin; x < rect.xMax; x++)
                        {
                            int index = y * candidateGrid.Width + x;
                            if (index < candidateRoads.Length && candidateRoads[index].Value != 0) gridRoadCells++;
                            if (sidewalks[index]) sidewalkCells++;
                            if (waters[index]) waterCells++;
                        }
                    }
                    bool otherSourcesClear = true;
                    foreach (S073SourceFootprint other in fixedFootprints)
                        if (rect.Overlaps(other.Rect)) { otherSourcesClear = false; break; }

                    if (!ownerValid) ownerRejectedCount++;
                    if (!levelValid) levelRejectedCount++;
                    if (!buildingClear) buildingMaskRejectedCount++;
                    if (!roadAndSidewalkClear || gridRoadCells != 0) roadMaskRejectedCount++;
                    if (!waterClear || waterCells != 0) waterMaskRejectedCount++;
                    if (!within) boundsRejectedCount++;
                    if (!otherSourcesClear) otherSourceOverlapCount++;
                    bool valid = ownerValid && levelValid && buildingClear && roadAndSidewalkClear &&
                        gridRoadCells == 0 && waterClear && waterCells == 0 && within && otherSourcesClear;
                    if (valid)
                    {
                        double centerX = rect.x + rect.width * 0.5;
                        double centerY = rect.y + rect.height * 0.5;
                        double playerDistanceCells = Math.Sqrt((centerX - playerBaseCenterX) * (centerX - playerBaseCenterX) +
                            (centerY - playerBaseCenterY) * (centerY - playerBaseCenterY));
                        double enemyDistanceCells = Math.Sqrt((centerX - enemyBaseCenterX) * (centerX - enemyBaseCenterX) +
                            (centerY - enemyBaseCenterY) * (centerY - enemyBaseCenterY));
                        double enemySpawnDistanceCells = Math.Sqrt((centerX - factionTwoSpawn.x) * (centerX - factionTwoSpawn.x) +
                            (centerY - factionTwoSpawn.y) * (centerY - factionTwoSpawn.y));
                        var candidate = new JObject
                        {
                            ["origin"] = new JArray(origin.x, origin.y),
                            ["effective_rect"] = new JArray(rect.x, rect.y, rect.width, rect.height),
                            ["original_owner_predicate_valid"] = true,
                            ["foundation_level_valid"] = true,
                            ["authored_building_mask_clear"] = true,
                            ["authored_road_sidewalk_and_grid_road_clear"] = true,
                            ["authored_water_clear"] = true,
                            ["inside_grid"] = true,
                            ["all_nine_other_source_footprints_clear"] = true,
                            ["grid_road_cells"] = gridRoadCells,
                            ["sidewalk_cells"] = sidewalkCells,
                            ["water_cells"] = waterCells,
                            ["barrack_center_cell"] = new JArray(centerX, centerY),
                            ["distance_to_faction_1_player_barrack_center_cells"] = Math.Round(playerDistanceCells, 4),
                            ["distance_to_faction_2_enemy_barrack_center_cells"] = Math.Round(enemyDistanceCells, 4),
                            ["distance_to_faction_2_enemy_spawn_cells"] = Math.Round(enemySpawnDistanceCells, 4),
                            ["distance_to_faction_1_player_barrack_center_world_units"] = Math.Round(playerDistanceCells * grid.CellSize, 4),
                            ["distance_to_faction_2_enemy_barrack_center_world_units"] = Math.Round(enemyDistanceCells * grid.CellSize, 4),
                            ["distance_to_faction_2_enemy_spawn_world_units"] = Math.Round(enemySpawnDistanceCells * grid.CellSize, 4),
                            ["placement_committed"] = false
                        };
                        validRows.Add(candidate);
                        if (playerDistanceCells > farthestFromPlayer ||
                            (Math.Abs(playerDistanceCells - farthestFromPlayer) < 0.000001 && enemyDistanceCells < nearestToEnemy))
                        {
                            farthestFromPlayer = playerDistanceCells;
                            nearestToEnemy = enemyDistanceCells;
                            selectedCandidate = (JObject)candidate.DeepClone();
                        }
                    }
                    // Keep the original owner's ring traversal going after every
                    // legal point so every origin through radius 112 is inspected.
                    return false;
                });

                bool surveyIteratorFound = runtimeSpawnSystem.TryFindValidInitialBuildingOrigin(surveyContext, definition,
                    surveyCenter, false, grid, roads, blockers, out _,
                    requirePreferredOrigin: false, allowAuthoredRoadOverlap: false,
                    emitCensusEvents: false, maxSearchRadiusLimit: searchRadius,
                    candidateObserved: (candidate, candidateRect, cachedInvalid) =>
                    {
                        candidateVisits++;
                        if (cachedInvalid) cachedInvalidCandidateCount++;
                    });
                if (surveyIteratorFound)
                    throw new InvalidOperationException("Survey-only predicate unexpectedly admitted an owner placement");
                int expectedCandidateVisits = 1 + 4 * searchRadius * (searchRadius + 1);
                if (candidateVisits != expectedCandidateVisits)
                    throw new InvalidOperationException("Owner iterator did not visit the entire original radius-112 candidate domain: " + candidateVisits);
                if (em.EntityOrderVersion != entityOrderBefore)
                    throw new InvalidOperationException("Survey changed ECS entity structure");
                JObject sourcePinsAfter = HashS073ExhaustiveSurveySourcePins();
                if (!JToken.DeepEquals(sourcePinsBefore, sourcePinsAfter))
                    throw new InvalidOperationException("Pinned source changed during exhaustive survey");

                result["source_pins_sha256"] = sourcePinsBefore;
                result["source_masks_hashes"] = HashS073ExhaustiveSurveyMasks(grid, roads, blockers, sidewalks, waters);
                result["survey_iterator_exhausted_original_ring_bound"] = true;
                result["survey_iterator_forced_owner_acceptance_false"] = true;
                result["production_owner_search_result_claimed"] = false;
                result["owner_iterator_candidate_visits"] = candidateVisits;
                result["cached_invalid_origins_skipped_by_original_owner"] = cachedInvalidCandidateCount;
                result["original_owner_predicate_invocation_count_excluding_cached_invalid"] = originalPredicateCalls;
                result["valid_candidate_count"] = validRows.Count;
                result["predicate_rejection_counts"] = new JObject
                {
                    ["owner_predicate"] = ownerRejectedCount,
                    ["foundation_level"] = levelRejectedCount,
                    ["authored_building_mask"] = buildingMaskRejectedCount,
                    ["authored_road_sidewalk_or_grid_road"] = roadMaskRejectedCount,
                    ["authored_water"] = waterMaskRejectedCount,
                    ["outside_grid"] = boundsRejectedCount,
                    ["overlap_any_of_nine_other_source_footprints"] = otherSourceOverlapCount
                };
                result["farthest_from_player_barrack_center_candidate"] = selectedCandidate;
                result["first_candidate_passing_all_survey_predicates"] = validRows.Count > 0 ? validRows[0].DeepClone() : JValue.CreateNull();
                result["candidate_order"] = "Original owner Chebyshev ring order; only origins passing all recorded predicates are emitted";
                result["ranking_policy"] = "Farthest valid Barrack center from the faction-1 Barrack center; ties prefer nearer faction-2 Barrack center. Faction-2 spawn distance is recorded separately; no balance threshold is applied.";
                result["source_plan_match_unique"] = true;
                result["source_masks_complete"] = true;
                result["target_barrack_and_ammo_absent_before_survey"] = true;
                result["candidate_set_exhaustive_within_original_bound"] = true;
                result["candidate_validity_definition"] = "Original owner IsPlacementValid and not cached-invalid; Skirmish foundation level; authored building/road/sidewalk/water masks; zero grid-road footprint cells; in-grid; no overlap with any of the nine other current-plan building footprints";
                result["status"] = "RecordedExhaustiveReadOnlyCandidates";
            }
            catch (Exception exception)
            {
                result["failure_reason"] = exception.GetType().Name + ": " + exception.Message;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using var writer = new StreamWriter(stream);
                writer.Write(result.ToString(Newtonsoft.Json.Formatting.Indented));
                writer.Write("\n");
                Debug.Log("[S073PreparingBarrackExhaustiveSurvey] result=" + result["status"] +
                    " candidates=" + result["valid_candidate_count"] + " readiness=0 path=" + output);
            }
            catch (Exception exception)
            {
                Debug.LogError("[S073PreparingBarrackExhaustiveSurvey] output=result=Failed error=" + exception.Message);
            }
        }

        static JObject HashS073ExhaustiveSurveySourcePins()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] paths =
            {
                "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset",
                "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
                "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.S073PreparingSurvey.cs",
                "Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCompositionSystemHelper.InitialPlacement.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementAdapterCompositionSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementValidationUtilitySystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredBuildingCache.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementRoadSurfaceMask.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellCacheCompositionSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellCacheCompositionSystemHelper.RoadValidation.cs",
                "Assets/Game/Scripts/Systems/SkirmishWaterPlacement.cs",
                "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs",
                "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingRuntimeContextFactoryCompositionSystemHelper.cs",
                "Assets/Game/Scripts/Components/GridComponents.cs",
                "Assets/Game/Scripts/Components/OperationMapComponents.cs",
                "Assets/Game/Scripts/Components/MapSurfaceComponents.cs",
                "Assets/Game/Scripts/Components/MapSurfaceBlobAccess.cs"
            };
            var pins = new JObject();
            using (var sha = SHA256.Create())
                foreach (string path in paths)
                {
                    string full = Path.Combine(root, path);
                    if (!File.Exists(full)) throw new FileNotFoundException("Survey source pin missing", path);
                    pins[path] = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(full))).Replace("-", "").ToLowerInvariant();
                }
            return pins;
        }

        JObject HashS073ExhaustiveSurveyMasks(GridConfig grid, DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockers, bool[] sidewalks, bool[] waters)
        {
            int count = checked(grid.Width * grid.Height);
            if (roads.Length != count || blockers.GridSize != count || !blockers.Blocked.IsCreated ||
                blockers.Blocked.Length != count || !blockers.Counts.IsCreated || blockers.Counts.Length != count ||
                !blockers.FriendlyPassFactionIds.IsCreated || blockers.FriendlyPassFactionIds.Length != count)
                throw new InvalidOperationException("Complete committed native road/blocker masks required for mask hashes");
            byte[] roadBytes = new byte[count];
            byte[] authoredRoadSidewalkBytes = new byte[count];
            byte[] buildingBytes = new byte[count];
            byte[] sidewalkBytes = new byte[count];
            byte[] waterBytes = new byte[count];
            byte[] blockerCountBytes = new byte[checked(count * sizeof(int))];
            byte[] friendlyPassBytes = new byte[count];
            byte[] blockedBits = new byte[(count + 7) / 8];
            bool[] buildingMask = new bool[count];
            bool[] authoredRoadSidewalkMask = new bool[count];
            _skirmishBuildings.AppendTo(buildingMask);
            _skirmishRoads.AppendTo(authoredRoadSidewalkMask);
            for (int i = 0; i < count; i++)
            {
                roadBytes[i] = roads[i].Value;
                authoredRoadSidewalkBytes[i] = authoredRoadSidewalkMask[i] ? (byte)1 : (byte)0;
                buildingBytes[i] = buildingMask[i] ? (byte)1 : (byte)0;
                sidewalkBytes[i] = sidewalks[i] ? (byte)1 : (byte)0;
                waterBytes[i] = waters[i] ? (byte)1 : (byte)0;
                uint blockerCount = unchecked((uint)blockers.Counts[i]);
                int countOffset = i * sizeof(int);
                blockerCountBytes[countOffset] = (byte)blockerCount;
                blockerCountBytes[countOffset + 1] = (byte)(blockerCount >> 8);
                blockerCountBytes[countOffset + 2] = (byte)(blockerCount >> 16);
                blockerCountBytes[countOffset + 3] = (byte)(blockerCount >> 24);
                friendlyPassBytes[i] = blockers.FriendlyPassFactionIds[i];
                if (blockers.Blocked.IsSet(i)) blockedBits[i >> 3] |= (byte)(1 << (i & 7));
            }
            var result = new JObject
            {
                ["grid_road_sha256"] = HashS073SurveyBytes(roadBytes),
                ["authored_road_or_sidewalk_sha256"] = HashS073SurveyBytes(authoredRoadSidewalkBytes),
                ["authored_building_sha256"] = HashS073SurveyBytes(buildingBytes),
                ["authored_sidewalk_sha256"] = HashS073SurveyBytes(sidewalkBytes),
                ["authored_water_sha256"] = HashS073SurveyBytes(waterBytes),
                ["dynamic_blocker_counts_sha256"] = HashS073SurveyBytes(blockerCountBytes),
                ["dynamic_blocker_friendly_pass_faction_ids_sha256"] = HashS073SurveyBytes(friendlyPassBytes),
                ["dynamic_blocked_bits_sha256"] = HashS073SurveyBytes(blockedBits),
                ["dynamic_blocker_storage_epoch"] = blockers.StorageEpoch.ToString(),
                ["dynamic_blocker_applied_revision"] = blockers.AppliedRevision.ToString()
            };
            return result;
        }

        static string HashS073SurveyBytes(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        static bool TryResolveS073InitialSourcePair(EntityManager em, Entity plan,
            out S073InitialSourceRow barrack, out S073InitialSourceRow ammo, out string failure)
        {
            barrack = default;
            ammo = default;
            failure = string.Empty;
            var factions = em.GetBuffer<InitialUnitsFactionSpawnEntry>(plan, true);
            var buildings = em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(plan, true);
            int spawnMatches = 0;
            int2 factionSpawn = default;
            foreach (var row in factions)
                if (row.FactionId == 2) { spawnMatches++; factionSpawn = row.SpawnCell; }
            if (spawnMatches != 1) { failure = "Unique faction-2 original spawn row required"; return false; }

            int barracksMatches = 0, ammoMatches = 0;
            for (int i = 0; i < buildings.Length; i++)
            {
                var row = buildings[i];
                if (row.FactionId != 2) continue;
                string key = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(row.PrefabLookupKey.ToString());
                if (key == S073BarrackKey)
                {
                    barrack = new S073InitialSourceRow(key, row.PrefabLookupKey.ToString(), factionSpawn, row.OriginOffset, i);
                    barracksMatches++;
                }
                else if (key == S073AmmoKey)
                {
                    ammo = new S073InitialSourceRow(key, row.PrefabLookupKey.ToString(), factionSpawn, row.OriginOffset, i);
                    ammoMatches++;
                }
            }
            if (barracksMatches != 1 || ammoMatches != 1)
            { failure = "Exactly one faction-2 source row for Barrack and Ammo Depot required"; return false; }
            return true;
        }

        S073LogicalReauthoringSurvey SurveyS073LogicalReauthoring(
            BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
            BuildingRuntimeSpawnCompositionSystemHelper.Context originalContext,
            BuildingDefinitionPrefabSystemHelper definitionSystem, GridConfig grid,
            DynamicBuffer<GridRoad> roads, DynamicBlockerComponent blockers, MapSurfaceComponent surface,
            bool[] sidewalks, bool[] waters, S073InitialSourceRow barrackSource, S073InitialSourceRow ammoSource,
            RectInt barrackRect, S073SourceFootprint[] originalOtherFootprints,
            S073CandidateRejectionReasons ammoRejections)
        {
            var result = new S073LogicalReauthoringSurvey
            {
                status = "FailedClosed",
                source_world_mutated = false,
                placement_committed = false,
                joint_owner_acceptance_proven = false,
                source_fit_certified = false,
                eligible_ammo_other_footprint_only_candidate_count = 0,
                selected_ammunition_candidate_ordinal = -1,
                selected_barrack_source_entry_index = barrackSource.EntryIndex,
                selected_barrack_source_key = barrackSource.Key,
                selected_barrack_origin = new[] { barrackRect.x, barrackRect.y },
                selected_barrack_rect = new[] { barrackRect.x, barrackRect.y, barrackRect.width, barrackRect.height },
                ammunition_source_entry_index = ammoSource.EntryIndex,
                ammunition_source_key = ammoSource.Key
            };

            if (ammoRejections == null || ammoRejections.other_eight_only_candidates == null)
            {
                result.failure_reason = "Exact same-run Ammo mask-64 candidate records required";
                return result;
            }
            if (ammoRejections.other_eight_only_candidate_details_truncated ||
                ammoRejections.other_eight_only_candidate_count != ammoRejections.other_eight_only_candidates.Length)
            {
                result.failure_reason = "Complete untruncated same-run Ammo mask-64 candidate records required";
                return result;
            }

            S073SourceFootprint[] refineryMatches = originalOtherFootprints
                .Where(footprint => footprint.FactionId == 2 && footprint.Key == "building_refinery")
                .ToArray();
            if (refineryMatches.Length != 1 || refineryMatches[0].Source.Key != "building_refinery")
            {
                result.failure_reason = "Exactly one faction-2 source Refinery row required for logical relocation";
                return result;
            }

            S073SourceFootprint refinery = refineryMatches[0];
            S073SourceFootprint[] remainingSeven = originalOtherFootprints
                .Where(footprint => footprint.EntryIndex != refinery.EntryIndex).ToArray();
            if (originalOtherFootprints.Length != 8 || remainingSeven.Length != 7 ||
                remainingSeven.Any(footprint => footprint.EntryIndex == refinery.EntryIndex))
            {
                result.failure_reason = "Exactly the other seven fixed source footprints must remain after removing only the faction-2 Refinery row";
                return result;
            }
            result.relocated_refinery_source = new S073SourceFootprintObservation
            {
                source_entry_index = refinery.EntryIndex,
                faction_id = refinery.FactionId,
                building_key = refinery.Key,
                preferred_origin = new[] { refinery.Source.Preferred.x, refinery.Source.Preferred.y },
                effective_rect = new[] { refinery.Rect.x, refinery.Rect.y, refinery.Rect.width, refinery.Rect.height },
                rotation_vertical = false
            };
            result.remaining_fixed_source_footprint_count = remainingSeven.Length;

            var barrackRects = new[] { barrackRect };
            var ammoOptions = new List<S073AmmoRelocationOption>();
            foreach (S073OtherFootprintOnlyCandidate candidate in ammoRejections.other_eight_only_candidates)
            {
                if (candidate == null || candidate.failure_mask != (1 << 6) || candidate.origin == null ||
                    candidate.origin.Length != 2 || candidate.effective_rect == null || candidate.effective_rect.Length != 4)
                    continue;
                RectInt ammoRect = new RectInt(candidate.effective_rect[0], candidate.effective_rect[1],
                    candidate.effective_rect[2], candidate.effective_rect[3]);
                S073OverlappingSourceEntryRectangle[] overlaps = candidate.overlapping_source_entry_rectangles ??
                    Array.Empty<S073OverlappingSourceEntryRectangle>();
                bool refineryOnly = overlaps.Length == 1 && overlaps[0] != null &&
                    overlaps[0].source_entry_index == refinery.EntryIndex && overlaps[0].faction_id == 2 &&
                    overlaps[0].building_key == "building_refinery";
                bool clearsBarrack = barrackRects.All(rect => !rect.Overlaps(ammoRect));
                bool nonOverlapPredicatesPass = candidate.original_owner_predicate &&
                    candidate.foundation_level_predicate && candidate.authored_building_mask_clear &&
                    candidate.authored_road_or_sidewalk_mask_clear && candidate.authored_water_mask_clear &&
                    candidate.inside_grid && candidate.grid_road_cells == 0;
                bool eligible = refineryOnly && clearsBarrack && nonOverlapPredicatesPass;
                ammoOptions.Add(new S073AmmoRelocationOption
                {
                    candidate_ordinal = candidate.candidate_ordinal,
                    failure_mask = candidate.failure_mask,
                    origin = (int[])candidate.origin.Clone(),
                    effective_rect = (int[])candidate.effective_rect.Clone(),
                    original_owner_predicate = candidate.original_owner_predicate,
                    foundation_level_predicate = candidate.foundation_level_predicate,
                    authored_building_mask_clear = candidate.authored_building_mask_clear,
                    authored_road_or_sidewalk_mask_clear = candidate.authored_road_or_sidewalk_mask_clear,
                    authored_water_mask_clear = candidate.authored_water_mask_clear,
                    inside_grid = candidate.inside_grid,
                    grid_road_cells = candidate.grid_road_cells,
                    clears_selected_barrack_rect = clearsBarrack,
                    overlaps_only_relocatable_faction_two_refinery = refineryOnly,
                    eligible = eligible,
                    overlapping_source_entry_rectangles = overlaps
                });
                if (refineryOnly) result.refinery_only_ammo_candidate_count++;
                if (eligible) result.eligible_ammo_other_footprint_only_candidate_count++;
            }
            result.ammo_candidate_options = ammoOptions.ToArray();

            S073AmmoRelocationOption selectedAmmo = ammoOptions
                .Where(option => option.eligible)
                .OrderBy(option => option.candidate_ordinal)
                .FirstOrDefault();
            if (selectedAmmo == null)
            {
                result.status = "NoEligibleAmmoCandidateAfterBarrackAndRefineryIsolation";
                result.failure_reason = "No recorded mask-64 Ammo candidate clears the selected Barrack and overlaps only the relocatable faction-2 Refinery";
                return result;
            }
            result.selected_ammunition_candidate_ordinal = selectedAmmo.candidate_ordinal;
            result.selected_ammunition_origin = (int[])selectedAmmo.origin.Clone();
            result.selected_ammunition_rect = (int[])selectedAmmo.effective_rect.Clone();

            if (!TryResolveConfiguredBuildingDefinition(definitionSystem, "building_refinery", out BuildingDefinition refineryDefinition) ||
                refineryDefinition?.Prefab == null)
            {
                result.failure_reason = "Current original configured faction-2 Refinery definition required";
                return result;
            }

            var refinerySearch = new S073LogicalRefineryCandidate
            {
                source_entry_index = refinery.Source.EntryIndex,
                source_building_key = refinery.Source.Key,
                preferred_origin = new[] { refinery.Source.Preferred.x, refinery.Source.Preferred.y },
                original_owner_search_found = false,
                placement_committed = false
            };
            result.refinery_search = refinerySearch;
            RectInt selectedAmmoRect = new RectInt(selectedAmmo.effective_rect[0], selectedAmmo.effective_rect[1],
                selectedAmmo.effective_rect[2], selectedAmmo.effective_rect[3]);
            int validated = 0;
            S073LogicalRefineryCandidate accepted = null;
            var refineryContext = originalContext.WithPlacementValidation((candidateDefinition, origin, footprint,
                rotate, candidateGrid, candidateRoads, candidateBlockers) =>
            {
                validated++;
                RectInt rect = originalContext.GetEffectivePlacementRect(candidateDefinition, origin, candidateGrid, rotate);
                bool originalValid = originalContext.IsPlacementValid(candidateDefinition, origin, footprint, rotate,
                    candidateGrid, candidateRoads, candidateBlockers);
                bool levelValid = BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(surface, rect);
                bool buildingClear = !_skirmishBuildings.Overlaps(candidateGrid, rect.position, rect.size);
                bool roadAndSidewalkClear = !_skirmishRoads.Overlaps(candidateGrid, rect.position, rect.size);
                bool waterClear = !_skirmishRoads.OverlapsWater(candidateGrid, rect.position, rect.size);
                bool insideGrid = BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(
                    rect.position, rect.size, candidateGrid);
                bool clearsBarrackAndAmmo = !rect.Overlaps(barrackRect) && !rect.Overlaps(selectedAmmoRect);
                bool clearsRemainingSeven = remainingSeven.All(footprintRow => !rect.Overlaps(footprintRow.Rect));
                int gridRoadCells = 0, sidewalkCells = 0, waterCells = 0;
                if (insideGrid)
                {
                    for (int y = rect.yMin; y < rect.yMax; y++)
                    for (int x = rect.xMin; x < rect.xMax; x++)
                    {
                        int index = y * candidateGrid.Width + x;
                        if (index < candidateRoads.Length && candidateRoads[index].Value != 0) gridRoadCells++;
                        if (index < sidewalks.Length && sidewalks[index]) sidewalkCells++;
                        if (index < waters.Length && waters[index]) waterCells++;
                    }
                }
                bool valid = originalValid && levelValid && buildingClear && roadAndSidewalkClear && waterClear &&
                    insideGrid && clearsBarrackAndAmmo && clearsRemainingSeven;
                if (valid)
                {
                    accepted = new S073LogicalRefineryCandidate
                    {
                        source_entry_index = refinery.Source.EntryIndex,
                        source_building_key = refinery.Source.Key,
                        preferred_origin = new[] { refinery.Source.Preferred.x, refinery.Source.Preferred.y },
                        original_owner_search_found = true,
                        original_owner_predicate = originalValid,
                        foundation_level_predicate = levelValid,
                        authored_building_mask_clear = buildingClear,
                        authored_road_or_sidewalk_mask_clear = roadAndSidewalkClear && gridRoadCells == 0,
                        authored_water_mask_clear = waterClear && waterCells == 0,
                        inside_grid = insideGrid,
                        grid_road_cells = gridRoadCells,
                        authored_sidewalk_cells = sidewalkCells,
                        authored_water_cells = waterCells,
                        clears_barrack_and_ammunition_rectangles = clearsBarrackAndAmmo,
                        clears_remaining_seven_source_rectangles = clearsRemainingSeven,
                        selected_origin = new[] { origin.x, origin.y },
                        effective_rect = new[] { rect.x, rect.y, rect.width, rect.height },
                        placement_committed = false
                    };
                }
                return valid;
            });

            bool found = runtimeSpawnSystem.TryFindValidInitialBuildingOrigin(refineryContext, refineryDefinition,
                refinery.Source.Preferred, false, grid, roads, blockers, out Vector2Int refineryOrigin,
                requirePreferredOrigin: false, allowAuthoredRoadOverlap: false, emitCensusEvents: false,
                maxSearchRadiusLimit: S073SurveySearchRadiusLimit);
            refinerySearch.evaluated_original_predicate_count = validated;
            refinerySearch.original_owner_search_found = found && accepted != null;
            if (!found || accepted == null)
            {
                result.status = "NoLegalRefineryOriginForSelectedAmmoAndBarrack";
                result.failure_reason = "Original refinery owner search found no candidate satisfying its source predicate, native surface masks, seven remaining source rectangles, and both selected target rectangles";
                return result;
            }
            if (refineryOrigin.x != accepted.selected_origin[0] || refineryOrigin.y != accepted.selected_origin[1])
            {
                result.status = "FailedClosedOwnerReturnMismatch";
                result.failure_reason = "Relocated refinery search return differs from the captured original-owner candidate";
                return result;
            }
            accepted.evaluated_original_predicate_count = validated;
            result.refinery_search = accepted;
            result.status = "LogicalRefineryCandidateFound_NoSequentialOwnerAcceptance";
            result.failure_reason = "This is a non-mutating candidate survey; the original owner did not commit or sequentially revalidate Barrack, Ammo, and Refinery together";
            return result;
        }

        static bool TryResolveS073OtherSourceFootprints(EntityManager em, Entity plan,
            BuildingRuntimeSpawnCompositionSystemHelper.Context context,
            BuildingDefinitionPrefabSystemHelper definitionSystem, GridConfig grid,
            S073InitialSourceRow barrack, S073InitialSourceRow ammo,
            out S073SourceFootprint[] footprints, out S073SourceFootprintObservation[] evidence,
            out string failure)
        {
            footprints = Array.Empty<S073SourceFootprint>();
            evidence = Array.Empty<S073SourceFootprintObservation>();
            failure = string.Empty;
            var factions = em.GetBuffer<InitialUnitsFactionSpawnEntry>(plan, true);
            int factionOneCount = 0, factionTwoCount = 0;
            int2 factionOneSpawn = default, factionTwoSpawn = default;
            foreach (InitialUnitsFactionSpawnEntry faction in factions)
            {
                if (faction.FactionId == 1) { factionOneCount++; factionOneSpawn = faction.SpawnCell; }
                if (faction.FactionId == 2) { factionTwoCount++; factionTwoSpawn = faction.SpawnCell; }
            }
            if (factionOneCount != 1 || factionTwoCount != 1)
            { failure = "Unique faction-1 and faction-2 spawn rows required"; return false; }

            var buildings = em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(plan, true);
            var resolved = new System.Collections.Generic.List<S073SourceFootprint>(8);
            var rows = new System.Collections.Generic.List<S073SourceFootprintObservation>(8);
            for (int i = 0; i < buildings.Length; i++)
            {
                if (i == barrack.EntryIndex || i == ammo.EntryIndex) continue;
                InitialUnitsFactionBuildingSpawnEntry source = buildings[i];
                int2 spawn;
                if (source.FactionId == 1) spawn = factionOneSpawn;
                else if (source.FactionId == 2) spawn = factionTwoSpawn;
                else { failure = "Unexpected faction in initial building source row " + i; return false; }

                string sourceKey = source.PrefabLookupKey.ToString();
                string normalizedKey = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(sourceKey);
                if (string.IsNullOrWhiteSpace(normalizedKey) ||
                    !TryResolveConfiguredBuildingDefinition(definitionSystem, normalizedKey, out BuildingDefinition definition) ||
                    definition?.Prefab == null)
                { failure = "Configured definition unresolved for source row " + i + " key=" + sourceKey; return false; }

                // CreateConfiguredInitialBuildingRequest sets RotateVertical=0 for
                // every legacy source row; use the same effective-rect delegate.
                Vector2Int preferred = new(spawn.x + source.OriginOffset.x, spawn.y + source.OriginOffset.y);
                RectInt rect = context.GetEffectivePlacementRect(definition, preferred, grid, false);
                var sourceRow = new S073InitialSourceRow(normalizedKey, sourceKey, spawn, source.OriginOffset, i);
                resolved.Add(new S073SourceFootprint(i, source.FactionId, normalizedKey, rect, sourceRow));
                rows.Add(new S073SourceFootprintObservation
                {
                    source_entry_index = i,
                    faction_id = source.FactionId,
                    building_key = normalizedKey,
                    preferred_origin = new[] { preferred.x, preferred.y },
                    effective_rect = new[] { rect.x, rect.y, rect.width, rect.height },
                    rotation_vertical = false
                });
            }
            if (resolved.Count != 8)
            { failure = "Exactly eight non-target original building source rows required; resolved=" + resolved.Count; return false; }
            footprints = resolved.ToArray();
            evidence = rows.ToArray();
            return true;
        }

        bool SurveyS073Candidate(BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
            BuildingRuntimeSpawnCompositionSystemHelper.Context originalContext, BuildingDefinition definition,
            S073InitialSourceRow source, GridConfig grid, DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockers, MapSurfaceComponent surface, bool[] sidewalks, bool[] waters,
            S073SourceFootprint[] otherSourceFootprints, RectInt? reservedPairRect,
            out S073CandidateSurveyRow row, out string failure)
        {
            row = new S073CandidateSurveyRow
            {
                building_key = source.Key,
                prefab_name = definition.Prefab.name,
                source_prefab_lookup_key = source.SourcePrefabLookupKey,
                faction_id = 2,
                source_entry_index = source.EntryIndex,
                source_spawn_cell = new[] { source.SpawnCell.x, source.SpawnCell.y },
                source_origin_offset = new[] { source.OriginOffset.x, source.OriginOffset.y },
                preferred_origin = new[] { source.Preferred.x, source.Preferred.y },
                placement_committed = false,
                selected_by_original_owner_search = true
            };
            failure = string.Empty;
            int validated = 0;
            S073CandidateSurveyRow accepted = null;
            var rejectionReasons = new S073CandidateRejectionReasons();
            row.rejection_reasons = rejectionReasons;
            var otherFootprintOnlyCandidates = new List<S073OtherFootprintOnlyCandidate>();
            var surveyContext = originalContext.WithPlacementValidation((candidateDefinition, origin, footprint, rotate,
                candidateGrid, candidateRoads, candidateBlockers) =>
            {
                validated++;
                RectInt rect = originalContext.GetEffectivePlacementRect(candidateDefinition, origin, candidateGrid, rotate);
                bool originalValid = originalContext.IsPlacementValid(candidateDefinition, origin, footprint, rotate,
                    candidateGrid, candidateRoads, candidateBlockers);
                bool levelValid = BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(
                    surface, rect);
                bool buildingClear = !_skirmishBuildings.Overlaps(candidateGrid, rect.position, rect.size);
                bool authoredRoadAndSidewalkClear = !_skirmishRoads.Overlaps(candidateGrid, rect.position, rect.size);
                bool waterClear = !_skirmishRoads.OverlapsWater(candidateGrid, rect.position, rect.size);
                bool pairClear = !reservedPairRect.HasValue || !rect.Overlaps(reservedPairRect.Value);
                bool otherSourceFootprintsClear = true;
                List<S073OverlappingSourceEntryRectangle> overlappingSourceEntries = null;
                for (int i = 0; i < otherSourceFootprints.Length; i++)
                {
                    if (!rect.Overlaps(otherSourceFootprints[i].Rect)) continue;
                    otherSourceFootprintsClear = false;
                    if (overlappingSourceEntries == null) overlappingSourceEntries = new List<S073OverlappingSourceEntryRectangle>();
                    overlappingSourceEntries.Add(new S073OverlappingSourceEntryRectangle
                    {
                        source_entry_index = otherSourceFootprints[i].EntryIndex,
                        faction_id = otherSourceFootprints[i].FactionId,
                        building_key = otherSourceFootprints[i].Key,
                        effective_rect = new[] { otherSourceFootprints[i].Rect.x, otherSourceFootprints[i].Rect.y,
                            otherSourceFootprints[i].Rect.width, otherSourceFootprints[i].Rect.height }
                    });
                }
                int gridRoadCells = 0, sidewalkCells = 0, waterCells = 0;
                bool within = BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(
                    rect.position, rect.size, candidateGrid);
                if (within)
                {
                    for (int y = rect.yMin; y < rect.yMax; y++)
                    for (int x = rect.xMin; x < rect.xMax; x++)
                    {
                        int index = y * candidateGrid.Width + x;
                        if (index < candidateRoads.Length && candidateRoads[index].Value != 0) gridRoadCells++;
                        if (index < sidewalks.Length && sidewalks[index]) sidewalkCells++;
                        if (index < waters.Length && waters[index]) waterCells++;
                    }
                }
                rejectionReasons.evaluated_candidates++;
                int failedMask = 0;
                if (!originalValid) { rejectionReasons.original_owner_predicate_failed++; failedMask |= 1 << 0; }
                if (!levelValid) { rejectionReasons.foundation_level_failed++; failedMask |= 1 << 1; }
                if (!buildingClear) { rejectionReasons.authored_building_mask_overlap++; failedMask |= 1 << 2; }
                if (!authoredRoadAndSidewalkClear) { rejectionReasons.authored_road_or_sidewalk_mask_overlap++; failedMask |= 1 << 3; }
                if (!waterClear) { rejectionReasons.authored_water_mask_overlap++; failedMask |= 1 << 4; }
                if (!within) { rejectionReasons.outside_grid++; failedMask |= 1 << 5; }
                if (!otherSourceFootprintsClear) { rejectionReasons.other_eight_source_footprint_overlap++; failedMask |= 1 << 6; }
                if (!pairClear) { rejectionReasons.reserved_target_pair_overlap++; failedMask |= 1 << 7; }
                if (gridRoadCells > 0) rejectionReasons.grid_road_cells_nonzero++;
                rejectionReasons.failure_mask_counts[failedMask]++;
                if (failedMask == (1 << 6))
                {
                    rejectionReasons.other_eight_only_candidate_count++;
                    if (otherFootprintOnlyCandidates.Count < rejectionReasons.other_eight_only_candidate_detail_limit)
                    {
                        otherFootprintOnlyCandidates.Add(new S073OtherFootprintOnlyCandidate
                        {
                            candidate_ordinal = validated,
                            failure_mask = failedMask,
                            origin = new[] { origin.x, origin.y },
                            effective_rect = new[] { rect.x, rect.y, rect.width, rect.height },
                            original_owner_predicate = originalValid,
                            foundation_level_predicate = levelValid,
                            authored_building_mask_clear = buildingClear,
                            authored_road_or_sidewalk_mask_clear = authoredRoadAndSidewalkClear,
                            authored_water_mask_clear = waterClear,
                            inside_grid = within,
                            grid_road_cells = gridRoadCells,
                            reserved_pair_clear = pairClear,
                            overlapping_source_entry_rectangles = overlappingSourceEntries?.ToArray() ?? Array.Empty<S073OverlappingSourceEntryRectangle>()
                        });
                    }
                    else rejectionReasons.other_eight_only_candidate_details_truncated = true;
                }
                bool valid = originalValid && levelValid && buildingClear && authoredRoadAndSidewalkClear &&
                    waterClear && otherSourceFootprintsClear && pairClear && within;
                if (valid)
                {
                    accepted = new S073CandidateSurveyRow
                    {
                        building_key = source.Key,
                        prefab_name = candidateDefinition.Prefab.name,
                        source_prefab_lookup_key = source.SourcePrefabLookupKey,
                        faction_id = 2,
                        source_entry_index = source.EntryIndex,
                        source_spawn_cell = new[] { source.SpawnCell.x, source.SpawnCell.y },
                        source_origin_offset = new[] { source.OriginOffset.x, source.OriginOffset.y },
                        preferred_origin = new[] { source.Preferred.x, source.Preferred.y },
                        rejection_reasons = rejectionReasons,
                        owner_search_found = true,
                        selected_origin = new[] { origin.x, origin.y },
                        effective_rect = new[] { rect.x, rect.y, rect.width, rect.height },
                        original_placement_predicate = originalValid,
                        skirmish_level_predicate = levelValid,
                        authored_building_mask_clear = buildingClear,
                        authored_road_and_sidewalk_mask_clear = authoredRoadAndSidewalkClear && gridRoadCells == 0,
                        authored_water_mask_clear = waterClear && waterCells == 0,
                        grid_road_cells = gridRoadCells,
                        authored_sidewalk_cells = sidewalkCells,
                        authored_water_cells = waterCells,
                        other_source_footprints_clear = otherSourceFootprintsClear,
                        pair_nonoverlap = reservedPairRect.HasValue && pairClear,
                        evaluated_original_predicate_count = validated,
                        selected_by_original_owner_search = true,
                        placement_committed = false
                    };
                }
                return valid;
            });

            bool found = runtimeSpawnSystem.TryFindValidInitialBuildingOrigin(surveyContext, definition,
                source.Preferred, false, grid, roads, blockers, out Vector2Int resolved,
                requirePreferredOrigin: false, allowAuthoredRoadOverlap: false,
                emitCensusEvents: false, maxSearchRadiusLimit: S073SurveySearchRadiusLimit);
            rejectionReasons.other_eight_only_candidates = otherFootprintOnlyCandidates.ToArray();
            if (!found || accepted == null)
            {
                failure = "originalSearchFound=" + found + " evaluatedOriginalPredicates=" + validated;
                row.owner_search_found = false;
                row.evaluated_original_predicate_count = validated;
                return false;
            }
            if (resolved.x != accepted.selected_origin[0] || resolved.y != accepted.selected_origin[1])
            {
                failure = "Selected origin disagrees with original owner return";
                return false;
            }
            row = accepted;
            return true;
        }

        static bool HasExistingS073EnemyTarget(EntityManager em)
        {
            using var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(), ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                None = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<OperationMapBuildingComponent>() }
            });
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (Entity entity in entities)
            {
                RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                if (info.OwnerFactionId != 2) continue;
                string key = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(
                    em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString());
                if (key == S073BarrackKey || key == S073AmmoKey) return true;
            }
            return false;
        }

        static string[] HashS073SurveySourcePins()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] paths =
            {
                "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset",
                "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.S073PreparingSurvey.cs",
                "Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCompositionSystemHelper.InitialPlacement.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementAdapterCompositionSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredBuildingCache.cs",
                "Assets/Game/Scripts/Systems/SkirmishLayoutAudit.cs",
                "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs",
                "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs"
            };
            var pins = new string[paths.Length];
            using (var sha = SHA256.Create())
                for (int i = 0; i < paths.Length; i++)
                {
                    string full = Path.Combine(root, paths[i]);
                    pins[i] = paths[i] + "=" + (File.Exists(full)
                        ? BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(full))).Replace("-", "").ToLowerInvariant()
                        : "missing");
                }
            return pins;
        }
    }
}
#endif
