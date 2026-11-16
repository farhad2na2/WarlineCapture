#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class SkirmishIndustrialBasinPlacementProbe
    {
        private const string Key = "Warline.IndustrialBasinPlacement";
        private const string ExitKey = "Warline.IndustrialBasinPlacement.ExitAfterFinish";
        private const string EvidenceDirectory = "/private/tmp/warline-s3-aria";
        private const string PlacementAuditOutputKey = "WARLINE_S073_PLACEMENT_AUDIT_OUTPUT";
        private const string PredicateRelativePath = "Logs/SkirmishProduction/s073-placement-predicate-audit-20261031-r3.jsonl";
        private const string AuthoredMaskRelativePath = "Logs/SkirmishProduction/s073-authored-building-mask-20261031-r3.bin";
        private static int stage;
        private static double next;
        private static double deadline;
        private static bool exitAfterFinish;

        static SkirmishIndustrialBasinPlacementProbe()
        {
            if (SessionState.GetBool(Key, false))
            {
                exitAfterFinish = SessionState.GetBool(ExitKey, false);
                S002PhysicalStartupObservation.OriginalPlacement = ObserveOriginalPlacement;
                EditorApplication.update += Tick;
            }
        }

        [MenuItem("Tools/Warline/Skirmish/Launch S3 Industrial Basin Placement Probe")]
        public static void Launch() => LaunchCore(false);

        public static void LaunchAndExit() => LaunchCore(true);

        private static void LaunchCore(bool shouldExitAfterFinish)
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit play mode first.");
            string predicatePath = PredicateAuditPath();
            if (File.Exists(predicatePath))
                throw new InvalidOperationException("Fresh placement predicate evidence path required: " + predicatePath);
            string maskPath = AuthoredMaskPath();
            if (File.Exists(maskPath) || File.Exists(maskPath + ".json"))
                throw new InvalidOperationException("Fresh authored-building mask evidence path required: " + maskPath);
            Directory.CreateDirectory(Path.GetDirectoryName(predicatePath));
            SessionState.SetBool(Key, true);
            SessionState.SetBool(ExitKey, shouldExitAfterFinish);
            exitAfterFinish = shouldExitAfterFinish;
            stage = 0;
            next = 0;
            deadline = 0;
            S002PhysicalStartupObservation.OriginalPlacement = ObserveOriginalPlacement;
            Directory.CreateDirectory(EvidenceDirectory);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;
            double now = EditorApplication.timeSinceStartup;
            if (deadline == 0)
                deadline = now + 180;
            if (now > deadline)
            {
                Finish("timeout");
                return;
            }
            if (now < next)
                return;

            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                    return;
                EntityManager em = world.EntityManager;
                if (stage == 0)
                {
                    GameLocalization.SetLocale("en", false);
                    if (!SkirmishLaunchProjection.IsShellReadyToEnterMatch(em))
                    {
                        next = now + 0.5;
                        return;
                    }
                    QuickGameConfig config = QuickGameConfig.Defaults;
                    config.MapSeed = 104729;
                    config.ScenarioIndex = SkirmishPresetConfig.IndustrialBasinScenarioIndex;
                    if (SkirmishLaunchProjection.TryGet(em, out _, out _))
                        return;
                    if (!SkirmishLaunchProjection.TryQueue(em, config))
                        return;
                    stage = 1;
                    next = now + 2;
                    return;
                }

                if (!SkirmishLaunchProjection.TryGet(em, out _, out SkirmishMatchState match))
                    return;
                SkirmishLaunchProjection.TryEnterMatch(em);
                SkirmishLaunchProjection.TryBeginGameplayIfLoaded();
                SkirmishLaunchProjection.TryRequestPlay(em);

                if (match.StartupFailure != SkirmishStartupFailureCode.None ||
                    match.Phase == SkirmishPhase.Playing)
                {
                    bool passed;
                    string audit = SkirmishLayoutAudit.Inspect(em, out passed);
                    string path = PlacementAuditPath();
                    File.WriteAllText(path,
                        $"phase={match.Phase} fail={match.StartupFailure} auditPassed={passed}\n{audit}");
                    Debug.Log($"[SkirmishIndustrialBasinPlacement] result={(passed && match.Phase == SkirmishPhase.Playing ? "Passed" : "Failed")} path={path}\n{audit}");
                    Finish(passed && match.Phase == SkirmishPhase.Playing ? "passed" : "failed");
                }
                else
                    next = now + 0.5;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish("exception");
            }
        }

        private static void Finish(string reason)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(Key, false);
            SessionState.SetBool(ExitKey, false);
            S002PhysicalStartupObservation.OriginalPlacement = null;
            Debug.Log($"[SkirmishIndustrialBasinPlacement] done reason={reason}");
            if (exitAfterFinish)
                EditorApplication.Exit(reason == "passed" ? 0 : reason == "failed" ? 1 : 2);
            else
                // Keep play mode so follow-up eval can inspect the live map.
                Debug.Log("[SkirmishIndustrialBasinPlacement] live map inspection remains available.");
        }

        private static string PredicateAuditPath()
        {
            string overridePath = Environment.GetEnvironmentVariable("WARLINE_S073_PREDICATE_OUTPUT");
            if (!string.IsNullOrEmpty(overridePath)) return FreshTemporaryOutput(overridePath);
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(root, PredicateRelativePath);
        }

        private static string PlacementAuditPath()
        {
            string overridePath = Environment.GetEnvironmentVariable(PlacementAuditOutputKey);
            if (!string.IsNullOrEmpty(overridePath)) return FreshTemporaryOutput(overridePath);
            return Path.Combine(EvidenceDirectory, "placement-audit.txt");
        }

        private static string AuthoredMaskPath()
        {
            string overridePath = Environment.GetEnvironmentVariable("WARLINE_S073_MASK_OUTPUT");
            if (!string.IsNullOrEmpty(overridePath)) return FreshTemporaryOutput(overridePath);
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(root, AuthoredMaskRelativePath);
        }

        private static string FreshTemporaryOutput(string value)
        {
            string path = Path.GetFullPath(value);
            string projectLogs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "SkirmishProduction")) + Path.DirectorySeparatorChar;
            if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal) && !path.StartsWith(projectLogs, StringComparison.Ordinal))
                throw new InvalidOperationException("Explicit S073 probe output must be under /private/tmp or project SkirmishProduction");
            return path;
        }

        private static void CaptureAuthoredMask(S002PhysicalStartupObservation.OriginalPlacementFacts facts, SkirmishMatchState match)
        {
            string path = AuthoredMaskPath();
            if (File.Exists(path)) return;
            if (facts.Buildings == null || !facts.Buildings.IsReady)
                throw new InvalidDataException("Live original authored-building cache is not ready");

            int cellCount = checked(facts.Grid.Width * facts.Grid.Height);
            var cells = new bool[cellCount];
            facts.Buildings.AppendTo(cells);
            var bytes = new byte[cellCount];
            int occupied = 0;
            for (int i = 0; i < cellCount; i++)
            {
                if (!cells[i]) continue;
                bytes[i] = 1;
                occupied++;
            }

            const string sourcePath = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset";
            var source = UnityEditor.AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(sourcePath);
            if (source == null)
                throw new InvalidDataException("Current S073 selected source metadata missing: " + sourcePath);
            if (!source.TryValidateMetadata(out string validationError))
                throw new InvalidDataException("Current S073 selected source metadata invalid: " + validationError);

            string sha256;
            using (var sha = SHA256.Create())
                sha256 = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();

            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.Write(bytes, 0, bytes.Length);

            var grid = source.GridMetadata;
            var surface = source.SurfaceMetadata;
            var navigation = source.NavigationMetadata;
            var manifest = new JObject
            {
                ["kind"] = "S073LiveOriginalAuthoredBuildingMask",
                ["authority"] = "BuildingPlacementAuthoredBuildingCache.AppendTo at original S073 placement validation",
                ["layout"] = "row-major y*width+x; one byte per cell; 1=reserved, 0=free",
                ["byte_length"] = bytes.Length,
                ["occupied_cell_count"] = occupied,
                ["sha256"] = sha256,
                ["scenario_index"] = match.ScenarioIndex,
                ["seed"] = match.Seed,
                ["session_id"] = match.SessionId.ToString(),
                ["selected_source_path"] = sourcePath,
                ["selected_operation_map_id"] = source.OperationMapId,
                ["selected_source_identity_hash"] = source.SourceIdentityHash,
                ["selected_content_hash"] = source.ContentHash,
                ["bound_source_map_id"] = source.SourceBinding.SourceOperationMapId,
                ["bound_source_identity_hash"] = source.SourceBinding.SourceIdentityHash,
                ["bound_source_content_hash"] = source.SourceBinding.SourceContentHash,
                ["source_scene_guid"] = source.SourceSceneReference.AssetGUID,
                ["authored_subscene_guid"] = navigation.AuthoredSubSceneGuid,
                ["grid_guid"] = grid.AssetGuid,
                ["grid_content_hash"] = grid.ContentHash,
                ["surface_guid"] = surface.AssetGuid,
                ["surface_content_hash"] = surface.ContentHash,
                ["width"] = facts.Grid.Width,
                ["height"] = facts.Grid.Height,
                ["cell_size"] = facts.Grid.CellSize,
                ["grid_origin"] = new JArray(facts.Grid.Origin.x, facts.Grid.Origin.y, facts.Grid.Origin.z),
                ["native_visual_readiness"] = false,
                ["full_mission_initialized"] = false
            };
            using (var writer = new StreamWriter(new FileStream(path + ".json", FileMode.CreateNew, FileAccess.Write, FileShare.None)))
                writer.WriteLine(manifest.ToString(Newtonsoft.Json.Formatting.Indented));
            Debug.Log("[S073AuthoredBuildingMask] " + manifest.ToString(Newtonsoft.Json.Formatting.None));
        }

        private static void ObserveOriginalPlacement(S002PhysicalStartupObservation.OriginalPlacementFacts facts)
        {
            if (facts == null || facts.Grid.Width <= 0 || facts.Grid.Height <= 0)
                throw new InvalidDataException("Original placement diagnostic facts incomplete");
            bool target = facts.Origin == new Vector2Int(934, 444) || facts.Origin == new Vector2Int(906, 478) ||
                          facts.Origin == new Vector2Int(938, 480) || facts.Origin == new Vector2Int(906, 512);
            if (!target) return;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) throw new InvalidOperationException("Original S073 world missing");
            EntityManager em = world.EntityManager;
            using (var matches = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>()))
            {
                if (matches.CalculateEntityCount() != 1) throw new InvalidDataException("Unique S073 match required");
                var match = matches.GetSingleton<SkirmishMatchState>();
                if (match.ScenarioIndex != SkirmishPresetConfig.IndustrialBasinScenarioIndex || match.Seed != 104729)
                    throw new InvalidDataException("Exact S073 placement probe session required");
                CaptureAuthoredMask(facts, match);
            }

            RectInt rect = facts.EffectiveRect;
            bool inside = BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(rect.position, rect.size, facts.Grid);
            bool[] sidewalks = facts.Roads.GetSidewalks();
            bool[] water = facts.Roads.GetWater();
            int sidewalkCells = 0, waterCells = 0, blockerCells = 0, dependencyBlockerCells = 0;
            if (inside)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    int index = y * facts.Grid.Width + x;
                    if (sidewalks != null && index < sidewalks.Length && sidewalks[index]) sidewalkCells++;
                    if (water != null && index < water.Length && water[index]) waterCells++;
                    if (facts.Blockers.Blocked.IsCreated && index < facts.Blockers.Blocked.Length && facts.Blockers.Blocked.IsSet(index)) blockerCells++;
                    if (facts.Dependencies.IsRuntimeBlockerCell(x, y, facts.Grid.Width, facts.Grid.Height)) dependencyBlockerCells++;
                }
            }
            bool authoredRoadOrSidewalkOverlap = facts.Roads.Overlaps(facts.Grid, rect.position, rect.size);
            bool authoredBuildingOverlap = facts.Buildings.Overlaps(facts.Grid, rect.position, rect.size);
            bool waterOverlap = facts.Roads.OverlapsWater(facts.Grid, rect.position, rect.size);
            bool? runtimeBuildingOverlap = facts.OverlapsRuntimeBuilding == null
                ? (bool?)null
                : facts.OverlapsRuntimeBuilding(rect);
            var row = new JObject
            {
                ["kind"] = "S073OriginalPlacementPredicateAudit",
                ["authority"] = "LiveOriginalBuildingPlacementValidator_PrePolicyObservation",
                ["scenario_index"] = SkirmishPresetConfig.IndustrialBasinScenarioIndex,
                ["seed"] = 104729,
                ["prefab"] = facts.Definition?.Prefab != null ? facts.Definition.Prefab.name : "",
                ["origin"] = new JArray(facts.Origin.x, facts.Origin.y),
                ["footprint"] = new JArray(facts.Footprint.x, facts.Footprint.y),
                ["effective_rect"] = new JArray(rect.xMin, rect.yMin, rect.width, rect.height),
                ["grid"] = new JObject
                {
                    ["width"] = facts.Grid.Width,
                    ["height"] = facts.Grid.Height,
                    ["cell_size"] = facts.Grid.CellSize,
                    ["origin"] = new JArray(facts.Grid.Origin.x, facts.Grid.Origin.y, facts.Grid.Origin.z)
                },
                ["inside_grid"] = inside,
                ["road_gate"] = facts.RoadGate,
                ["authored_building_overlap"] = authoredBuildingOverlap,
                ["authored_road_or_sidewalk_overlap"] = authoredRoadOrSidewalkOverlap,
                ["water_overlap"] = waterOverlap,
                ["sidewalk_cells"] = sidewalkCells,
                ["water_cells"] = waterCells,
                ["runtime_blocker_cells"] = blockerCells,
                ["dependency_runtime_blocker_cells"] = dependencyBlockerCells,
                ["runtime_building_overlap"] = runtimeBuildingOverlap,
                ["invalid_prefix_present"] = facts.HasInvalidPrefix,
                ["invalid_prefix_dimensions"] = new JArray(facts.PrefixWidth, facts.PrefixHeight)
            };
            string path = PredicateAuditPath();
            File.AppendAllText(path, row.ToString(Newtonsoft.Json.Formatting.None) + "\n");
            Debug.Log("[S073PlacementPredicateAudit] " + row.ToString(Newtonsoft.Json.Formatting.None));
        }
    }
}
#endif
