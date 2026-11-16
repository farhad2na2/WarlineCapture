#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Editor
{
    // Passive observer of the original owner's live candidate loop. It never
    // creates requests or calls placement validation itself.
    [InitializeOnLoad]
    internal static class S073NativeInitialBuildingCandidateCensus
    {
        const string OutputVariable = "WARLINE_S073_INITIAL_BUILDING_CANDIDATE_CENSUS_OUTPUT";
        static readonly string OutputPath;
        static RectInt lastLevelRect;
        static bool lastLevelResult;
        static bool hasLastLevelResult;
        static readonly List<string> pendingRows = new List<string>();

        static S073NativeInitialBuildingCandidateCensus()
        {
            OutputPath = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrWhiteSpace(OutputPath)) return;
            BuildingRuntimeSpawnCompositionSystemHelper.InitialPlacementCandidateObserved += Observe;
            BuildingRuntimeSpawnCompositionSystemHelper.InitialPlacementSearchCompleted += ObserveCompletion;
            BuildingPlacementAdapterCompositionSystemHelper.SkirmishFootprintLevelValidationObserved += ObserveLevel;
            Debug.Log("[S073InitialBuildingCandidateCensus] result=Armed scope=original-owner-candidate-callback output=" + OutputPath);
        }

        static void ObserveLevel(RectInt rect, bool level)
        {
            lastLevelRect = rect;
            lastLevelResult = level;
            hasLastLevelResult = true;
        }

        static void Observe(BuildingRuntimeSpawnCompositionSystemHelper.InitialPlacementCandidateObservation value)
        {
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated || !EditorApplication.isPlaying) { hasLastLevelResult = false; return; }
                var em = world.EntityManager;
                using var matches = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>());
                if (matches.CalculateEntityCount() != 1) { hasLastLevelResult = false; return; }
                var match = matches.GetSingleton<SkirmishMatchState>();

                // These are the two failed original requests in the retained r6
                // placement audit. Candidate origins below always come from the
                // owner's actual loop, never from a generated survey grid.
                bool barracks = value.PrefabName == "Building_Barrack" && value.PreferredOrigin.x == 828 && value.PreferredOrigin.y == 462;
                bool ammunition = value.PrefabName == "Building_Ammunition_Depot" && value.PreferredOrigin.x == 867 && value.PreferredOrigin.y == 462;
                if (!barracks && !ammunition) { hasLastLevelResult = false; return; }
                bool hasLevel = match.Phase == SkirmishPhase.Playing && value.PlacementValidationRan &&
                    hasLastLevelResult && lastLevelRect.Equals(value.EffectiveRect);

                var row = new JObject
                {
                    ["kind"] = "S073NativeOriginalPlacementCandidate",
                    ["scope"] = "same-native-world-original-owner-loop",
                    ["world_sequence_number"] = world.SequenceNumber.ToString(),
                    ["session_id"] = match.SessionId.ToString(),
                    ["scenario_index"] = match.ScenarioIndex,
                    ["seed"] = match.Seed,
                    ["phase"] = (byte)match.Phase,
                    ["prefab"] = value.PrefabName,
                    ["preferred_origin_from_original_request"] = new JArray(value.PreferredOrigin.x, value.PreferredOrigin.y),
                    ["candidate_origin_from_original_loop"] = new JArray(value.CandidateOrigin.x, value.CandidateOrigin.y),
                    ["effective_rect_from_original_context"] = new JArray(value.EffectiveRect.x, value.EffectiveRect.y, value.EffectiveRect.width, value.EffectiveRect.height),
                    ["cached_invalid_from_original_predicate"] = value.CachedInvalid,
                    ["original_placement_validation_ran"] = value.PlacementValidationRan,
                    ["original_placement_validation_result"] = value.PlacementValidationRan ? (JToken)value.PlacementValid : JValue.CreateNull(),
                    // The production validator short-circuits surface-level
                    // checks outside Playing. False here means no observed
                    // level predicate for this candidate, not a level pass.
                    ["original_skirmish_level_check_ran_for_candidate"] = hasLevel,
                    ["original_skirmish_level_check_result"] = hasLevel ? (JToken)lastLevelResult : JValue.CreateNull(),
                    ["candidate_added_or_placement_committed"] = false
                };
                pendingRows.Add(row.ToString(Newtonsoft.Json.Formatting.None));
                hasLastLevelResult = false;
            }
            catch (Exception ex)
            {
                hasLastLevelResult = false;
                Debug.LogError("[S073InitialBuildingCandidateCensus] observer=result=Failed error=" + ex);
            }
        }

        static void ObserveCompletion(BuildingRuntimeSpawnCompositionSystemHelper.InitialPlacementSearchObservation value)
        {
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated || !EditorApplication.isPlaying) return;
                var em = world.EntityManager;
                using var matches = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>());
                if (matches.CalculateEntityCount() != 1) return;
                var match = matches.GetSingleton<SkirmishMatchState>();
                bool barracks = value.PrefabName == "Building_Barrack" && value.PreferredOrigin.x == 828 && value.PreferredOrigin.y == 462;
                bool ammunition = value.PrefabName == "Building_Ammunition_Depot" && value.PreferredOrigin.x == 867 && value.PreferredOrigin.y == 462;
                if (!barracks && !ammunition) return;
                var row = new JObject
                {
                    ["kind"] = "S073NativeOriginalPlacementSearchCompleted",
                    ["scope"] = "same-native-world-original-owner-loop",
                    ["world_sequence_number"] = world.SequenceNumber.ToString(),
                    ["session_id"] = match.SessionId.ToString(),
                    ["scenario_index"] = match.ScenarioIndex,
                    ["seed"] = match.Seed,
                    ["prefab"] = value.PrefabName,
                    ["preferred_origin_from_original_request"] = new JArray(value.PreferredOrigin.x, value.PreferredOrigin.y),
                    ["original_search_found_candidate"] = value.Found,
                    ["resolved_origin_from_original_owner"] = value.Found ? (JToken)new JArray(value.ResolvedOrigin.x, value.ResolvedOrigin.y) : JValue.CreateNull(),
                    ["evaluated_candidate_count"] = value.EvaluatedCandidateCount,
                    ["survey_generated_candidate_origins"] = false,
                    ["placement_committed_by_survey"] = false,
                    ["source_pins_sha256"] = HashSourcePins()
                };
                pendingRows.Add(row.ToString(Newtonsoft.Json.Formatting.None));
                File.AppendAllLines(OutputPath, pendingRows);
                pendingRows.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogError("[S073InitialBuildingCandidateCensus] completion_observer=result=Failed error=" + ex);
            }
        }

        static JObject HashSourcePins()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] paths =
            {
                "Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCompositionSystemHelper.InitialPlacement.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementAdapterCompositionSystemHelper.cs",
                "Assets/Game/Scripts/Editor/Skirmish/S073NativeInitialBuildingCandidateCensus.cs",
                "Assets/Game/Scripts/Systems/BuildingEntityManagerAccessSystem.cs"
            };
            var pins = new JObject();
            using (var sha = SHA256.Create())
                foreach (string path in paths)
                {
                    string full = Path.Combine(root, path);
                    pins[path] = File.Exists(full)
                        ? BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(full))).Replace("-", "").ToLowerInvariant()
                        : null;
                }
            return pins;
        }
    }
}
#endif
