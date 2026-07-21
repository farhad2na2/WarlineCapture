#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Game.Authoring;
    using Game.Composition;
    using Game.Configs;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Read-only Phase 0A inventory of managed RuntimeBuildingEntity dependencies for map placements.
    /// Does not mutate scenes, SubScenes, Addressables, presentation mode, or assets.
    /// </summary>
    public static class OperationMapRuntimeBuildingEntityDependencyInventoryProbe
    {
        internal const string ReportSchema =
            "warline.operation-map.runtime-building-entity-dependency-inventory";
        internal const int ReportSchemaVersion = 1;
        internal const string ReportPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_RUNTIME_BUILDING_ENTITY_DEPENDENCY_INVENTORY_REPORT_PATH";
        internal const string DefaultReportPath =
            "/private/tmp/warline-operation-map-runtime-building-entity-dependency-inventory.json";
        internal const string SummaryPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_RUNTIME_BUILDING_ENTITY_DEPENDENCY_INVENTORY_SUMMARY_PATH";
        internal const string DefaultSummaryPath =
            "/private/tmp/warline-operation-map-runtime-building-entity-dependency-inventory-summary.json";

        private const string CanonicalMapScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
        private const string AirportCategory = "Building_Airport";
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        public static void Run()
        {
            string projectRoot = RequireProjectRoot();
            string outputPath = OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot,
                Environment.GetEnvironmentVariable(ReportPathEnvironmentVariable) ?? DefaultReportPath);
            string summaryPath = OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot,
                Environment.GetEnvironmentVariable(SummaryPathEnvironmentVariable) ?? DefaultSummaryPath);

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            DependencyInventoryReport report;
            try
            {
                RequireCleanLoadedScenes();
                report = BuildReport(projectRoot, outputPath);
            }
            finally
            {
                RestoreSceneSetup(previousSetup);
            }

            PublishJsonAtomically(outputPath, JsonUtility.ToJson(report, true) + "\n");
            PublishJsonAtomically(summaryPath, JsonUtility.ToJson(BuildSummary(report), true) + "\n");
            Debug.Log(
                $"[OperationMapRuntimeBuildingEntityDependencyInventoryProbe] result={report.result} " +
                $"placements={report.counts.placementCount} managed={report.counts.requiresManagedRuntimeCount} " +
                $"unresolved={report.counts.unresolvedJoinCount} report={outputPath} summary={summaryPath}");
        }

        internal static bool HasRequiredReportShape(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            DependencyInventoryReport report;
            try
            {
                report = JsonUtility.FromJson<DependencyInventoryReport>(json);
            }
            catch
            {
                return false;
            }

            return report != null &&
                   string.Equals(report.reportSchema, ReportSchema, StringComparison.Ordinal) &&
                   report.reportSchemaVersion == ReportSchemaVersion &&
                   !string.IsNullOrWhiteSpace(report.result) &&
                   report.counts != null &&
                   report.counts.placementCount >= 0 &&
                   report.dependencyCatalog != null &&
                   report.placements != null &&
                   report.dispositionCounts != null;
        }

        internal static string ClassifyPlacementDisposition(
            bool exactAuthoredJoin,
            bool hasBuildingPrefab,
            bool hasBuildingDefinitionAuthoring)
        {
            if (!exactAuthoredJoin)
                return "UnresolvedAuthoredSourceJoin";
            if (!hasBuildingPrefab)
                return "MissingBuildingPrefab";
            if (!hasBuildingDefinitionAuthoring)
                return "MissingBuildingDefinitionAuthoring";
            return "RequiresManagedRuntimeBuildingEntity";
        }

        internal static bool IsManagedDependencyDisposition(string disposition)
        {
            return string.Equals(
                disposition,
                "RequiresManagedRuntimeBuildingEntity",
                StringComparison.Ordinal);
        }

        internal static List<DependencyCatalogEntry> BuildDependencyCatalog()
        {
            return new List<DependencyCatalogEntry>
            {
                new()
                {
                    dependencyId = "instance-presentation-hierarchy",
                    managedOwnerFieldOrSurface = "Instance / InstanceObject",
                    currentConsumerHint = "Managed building visual, selection, and transform consumers.",
                    proposedEcsOwnership = "LocalTransform + presentation entity hierarchy",
                    isApprovedTransientBoundary = false
                },
                new()
                {
                    dependencyId = "faction-visual-materials",
                    managedOwnerFieldOrSurface = "FactionVisualRenderers / FactionVisualBaseColors",
                    currentConsumerHint = "Faction tint application over managed renderers.",
                    proposedEcsOwnership = "Entities Graphics material/color presentation",
                    isApprovedTransientBoundary = false
                },
                new()
                {
                    dependencyId = "door-open-state",
                    managedOwnerFieldOrSurface = "DoorZ / DoorOpen01",
                    currentConsumerHint = "Production transport door animation.",
                    proposedEcsOwnership = "Optional animated entity or approved transient FX boundary",
                    isApprovedTransientBoundary = true
                },
                new()
                {
                    dependencyId = "intact-destroyed-visuals",
                    managedOwnerFieldOrSurface = "DestroyedVisualInstance / AliveVisualRoots",
                    currentConsumerHint = "Building destruction visual swaps.",
                    proposedEcsOwnership = "UnitDestroyedVisualReference-style intact/destroyed entity refs",
                    isApprovedTransientBoundary = false
                },
                new()
                {
                    dependencyId = "animated-resource-visuals",
                    managedOwnerFieldOrSurface = "AnimatedParts / ResourceVisualAnimationActive",
                    currentConsumerHint = "Managed resource and building part animation.",
                    proposedEcsOwnership = "Baked animation or approved transient presentation",
                    isApprovedTransientBoundary = true
                },
                new()
                {
                    dependencyId = "production-queues-and-slots",
                    managedOwnerFieldOrSurface =
                        "ProductionSpawnLocalPositions / ProducedUnitSlots / PendingProductions",
                    currentConsumerHint = "Building production queue and spawn scheduling.",
                    proposedEcsOwnership = "ECS buffers/components for production",
                    isApprovedTransientBoundary = false
                },
                new()
                {
                    dependencyId = "production-transport-visuals",
                    managedOwnerFieldOrSurface = "ActiveTransport / PendingDropVisual GameObjects",
                    currentConsumerHint = "Managed air transport and pending drop visuals.",
                    proposedEcsOwnership = "Approved transient boundary OR entity-space transport state",
                    isApprovedTransientBoundary = true
                },
                new()
                {
                    dependencyId = "runtime-building-transform-sync",
                    managedOwnerFieldOrSurface = "RuntimeBuildingEntityLink transform sync",
                    currentConsumerHint = "Managed link mirrors combat entity transform.",
                    proposedEcsOwnership = "Remove; combat entity owns transform",
                    isApprovedTransientBoundary = false
                },
                new()
                {
                    dependencyId = "runway-transform-discovery",
                    managedOwnerFieldOrSurface = "Runway discovery via transforms",
                    currentConsumerHint = "Airport production transport runway lookup.",
                    proposedEcsOwnership = "Typed ECS runway anchor/bounds",
                    isApprovedTransientBoundary = false
                },
                new()
                {
                    dependencyId = "selection-focus-transform",
                    managedOwnerFieldOrSurface = "Selection/UI focus via Instance transform",
                    currentConsumerHint = "Selection and UI world focus.",
                    proposedEcsOwnership = "Entity-derived focus position",
                    isApprovedTransientBoundary = false
                }
            };
        }

        private static DependencyInventoryReport BuildReport(string projectRoot, string outputPath)
        {
            Scene authoringScene = OpenSceneForInspection(CanonicalMapScenePath);
            OperationMapSceneView mapView = RequireSingleOperationMapSceneView(authoringScene);
            MapBuildingPlacementConfig config = mapView.BuildingPlacements;
            if (config == null || config.Placements == null)
                throw new InvalidOperationException("Building placement config is missing.");

            Dictionary<string, List<GameObject>> objectsByNamePath = IndexSceneObjectsByNamePath(authoringScene);
            var placements = new List<PlacementDependencyReport>(config.Placements.Count);
            var dispositionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var counts = new DependencyInventoryCountsReport();

            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapBuildingPlacementConfigEntry placement = config.Placements[i];
                PlacementJoinState join = ResolveAuthoredJoin(
                    placement.SourcePath,
                    placement.WorldPosition,
                    placement.WorldEulerAngles,
                    placement.WorldScale,
                    objectsByNamePath);
                GameObject prefab = placement.BuildingPrefab;
                BuildingDefinitionAuthoring authoring = prefab == null
                    ? null
                    : prefab.GetComponentInChildren<BuildingDefinitionAuthoring>(true);
                if (authoring != null)
                    authoring.ApplyConfigIfAvailable();
                bool hasPrefab = prefab != null;
                bool hasAuthoring = authoring != null;
                int productionSlotCount = hasAuthoring ? authoring.ConfiguredProductionCount : 0;
                int oilStorageCapacity = hasAuthoring ? authoring.ConfiguredOilStorageCapacity : 0;
                int fuelStorageCapacity = hasAuthoring ? authoring.ConfiguredFuelStorageCapacity : 0;
                float oilBarrelsPerDay = hasAuthoring ? authoring.ConfiguredOilBarrelsPerDay : 0f;
                float fuelBarrelsPerDay = hasAuthoring ? authoring.ConfiguredFuelBarrelsPerDay : 0f;
                bool hasResource = oilStorageCapacity > 0 || fuelStorageCapacity > 0 ||
                    oilBarrelsPerDay > 0f || fuelBarrelsPerDay > 0f;
                bool requiresRunway = string.Equals(placement.Category, AirportCategory, StringComparison.Ordinal);
                string disposition = ClassifyPlacementDisposition(
                    string.Equals(join.resolveState, "Exact", StringComparison.Ordinal),
                    hasPrefab,
                    hasAuthoring);
                List<string> dependencyFlags = BuildDependencyFlags(
                    productionSlotCount > 0,
                    requiresRunway);

                counts.placementCount++;
                counts.unresolvedJoinCount += string.Equals(
                    disposition, "UnresolvedAuthoredSourceJoin", StringComparison.Ordinal) ? 1 : 0;
                counts.missingPrefabCount += string.Equals(
                    disposition, "MissingBuildingPrefab", StringComparison.Ordinal) ? 1 : 0;
                counts.missingDefinitionAuthoringCount += string.Equals(
                    disposition, "MissingBuildingDefinitionAuthoring", StringComparison.Ordinal) ? 1 : 0;
                counts.requiresManagedRuntimeCount += IsManagedDependencyDisposition(disposition) ? 1 : 0;
                counts.hasDestroyedVisualCount += hasAuthoring &&
                    authoring.ConfiguredDestroyedVisualPrefab != null ? 1 : 0;
                counts.hasProductionCount += productionSlotCount > 0 ? 1 : 0;
                counts.hasResourceCount += hasResource ? 1 : 0;
                counts.requiresRunwayCount += requiresRunway ? 1 : 0;
                counts.hideAuthoringEnabled = config.HideAuthoringVisualsAfterSpawn;
                dispositionCounts[disposition] = dispositionCounts.TryGetValue(disposition, out int current)
                    ? current + 1
                    : 1;

                placements.Add(new PlacementDependencyReport
                {
                    placementIndex = i,
                    sourcePath = placement.SourcePath ?? string.Empty,
                    category = placement.Category ?? string.Empty,
                    factionId = placement.FactionId,
                    buildingPrefabPath = hasPrefab ? AssetDatabase.GetAssetPath(prefab) : string.Empty,
                    authoredJoinResolveState = join.resolveState,
                    authoredJoinResolutionMethod = join.resolutionMethod,
                    authoredSourceGlobalObjectId = join.resolvedSourceGlobalObjectId,
                    hasBuildingPrefab = hasPrefab,
                    hasBuildingDefinitionAuthoring = hasAuthoring,
                    hasDestroyedVisualPrefab = hasAuthoring &&
                        authoring.ConfiguredDestroyedVisualPrefab != null,
                    productionSlotCount = productionSlotCount,
                    oilStorageCapacity = oilStorageCapacity,
                    fuelStorageCapacity = fuelStorageCapacity,
                    oilBarrelsPerDay = oilBarrelsPerDay,
                    fuelBarrelsPerDay = fuelBarrelsPerDay,
                    hasCombat = hasAuthoring && authoring.ConfiguredCanAttack,
                    requiresRunwayOwnership = requiresRunway,
                    hideAuthoringVisualsAfterSpawn = config.HideAuthoringVisualsAfterSpawn,
                    authoredSourceRendererCount = CountRenderers(join.resolvedSource),
                    dependencyFlags = dependencyFlags,
                    conversionDisposition = disposition
                });
            }

            bool hasCleanupRequired = counts.missingPrefabCount > 0 ||
                counts.missingDefinitionAuthoringCount > 0;
            string result = counts.unresolvedJoinCount > 0
                ? "UnresolvedJoinsPendingReview"
                : hasCleanupRequired
                    ? "CleanupRequiredBeforeCutover"
                    : "AllPlacementsRequireManagedRuntimeBuildingEntity";
            return new DependencyInventoryReport
            {
                reportSchema = ReportSchema,
                reportSchemaVersion = ReportSchemaVersion,
                result = result,
                capturedUtc = DateTime.UtcNow.ToString("o"),
                projectRoot = projectRoot,
                reportPath = outputPath,
                canonicalMapScenePath = CanonicalMapScenePath,
                buildingPlacementConfigPath = AssetDatabase.GetAssetPath(config),
                hideAuthoringVisualsAfterSpawn = config.HideAuthoringVisualsAfterSpawn,
                dependencyCatalog = BuildDependencyCatalog(),
                counts = counts,
                dispositionCounts = dispositionCounts
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new DispositionCountReport { disposition = pair.Key, count = pair.Value })
                    .ToList(),
                placements = placements.OrderBy(row => row.placementIndex).ToList()
            };
        }

        private static DependencyInventorySummaryReport BuildSummary(DependencyInventoryReport report)
        {
            return new DependencyInventorySummaryReport
            {
                reportSchema = report.reportSchema,
                reportSchemaVersion = report.reportSchemaVersion,
                result = report.result,
                buildingPlacementConfigPath = report.buildingPlacementConfigPath,
                counts = report.counts,
                dispositionCounts = report.dispositionCounts
            };
        }

        private static List<string> BuildDependencyFlags(bool hasProduction, bool requiresRunway)
        {
            var flags = new List<string>
            {
                "instance-presentation-hierarchy",
                "faction-visual-materials",
                "door-open-state",
                "intact-destroyed-visuals",
                "animated-resource-visuals",
                "runtime-building-transform-sync",
                "selection-focus-transform"
            };
            if (hasProduction)
            {
                flags.Add("production-queues-and-slots");
                flags.Add("production-transport-visuals");
            }
            if (requiresRunway)
                flags.Add("runway-transform-discovery");
            return flags;
        }

        private static PlacementJoinState ResolveAuthoredJoin(
            string sourcePath,
            Vector3 worldPosition,
            Vector3 worldEulerAngles,
            Vector3 worldScale,
            Dictionary<string, List<GameObject>> objectsByNamePath)
        {
            string path = sourcePath ?? string.Empty;
            objectsByNamePath.TryGetValue(path, out List<GameObject> pathMatches);
            pathMatches ??= new List<GameObject>();
            List<GameObject> transformMatches = pathMatches
                .Where(candidate => TransformTupleMatches(
                    candidate.transform, worldPosition, worldEulerAngles, worldScale))
                .ToList();
            List<GameObject> matches = pathMatches.Count <= 1 ? pathMatches : transformMatches;
            if (matches.Count == 1)
            {
                return new PlacementJoinState
                {
                    resolveState = "Exact",
                    resolutionMethod = pathMatches.Count == 1
                        ? "UniqueHierarchyPath"
                        : "UniqueHierarchyPathAndTransformTuple",
                    resolvedSourceGlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(matches[0]).ToString(),
                    resolvedSource = matches[0]
                };
            }

            return new PlacementJoinState
            {
                resolveState = pathMatches.Count == 0 ? "Unresolved" : "Ambiguous",
                resolutionMethod = pathMatches.Count == 0
                    ? "HierarchyPathMissing"
                    : transformMatches.Count == 0
                        ? "NoTransformTupleMatchAmongPathCandidates"
                        : "MultipleTransformTupleMatches",
                resolvedSourceGlobalObjectId = string.Empty,
                resolvedSource = null
            };
        }

        private static bool TransformTupleMatches(
            Transform candidate,
            Vector3 expectedPosition,
            Vector3 expectedEulerAngles,
            Vector3 expectedScale)
        {
            const float PositionToleranceSquared = 0.000001f;
            const float ScaleToleranceSquared = 0.000001f;
            const float RotationToleranceDegrees = 0.001f;
            if ((candidate.position - expectedPosition).sqrMagnitude > PositionToleranceSquared ||
                (candidate.lossyScale - expectedScale).sqrMagnitude > ScaleToleranceSquared)
                return false;

            return Quaternion.Angle(candidate.rotation, Quaternion.Euler(expectedEulerAngles)) <=
                RotationToleranceDegrees;
        }

        private static int CountRenderers(GameObject source)
        {
            return source == null ? 0 : source.GetComponentsInChildren<Renderer>(true).Length;
        }

        private static Dictionary<string, List<GameObject>> IndexSceneObjectsByNamePath(Scene scene)
        {
            var index = new Dictionary<string, List<GameObject>>(StringComparer.Ordinal);
            foreach (GameObject root in scene.GetRootGameObjects())
                IndexHierarchy(root.transform, string.Empty, index);
            return index;
        }

        private static void IndexHierarchy(
            Transform transform,
            string parentPath,
            Dictionary<string, List<GameObject>> index)
        {
            string path = string.IsNullOrEmpty(parentPath)
                ? transform.name
                : parentPath + "/" + transform.name;
            if (!index.TryGetValue(path, out List<GameObject> objects))
            {
                objects = new List<GameObject>();
                index[path] = objects;
            }
            objects.Add(transform.gameObject);
            for (int i = 0; i < transform.childCount; i++)
                IndexHierarchy(transform.GetChild(i), path, index);
        }

        private static OperationMapSceneView RequireSingleOperationMapSceneView(Scene scene)
        {
            OperationMapSceneView[] views = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OperationMapSceneView>(true))
                .ToArray();
            if (views.Length != 1)
                throw new InvalidOperationException(
                    $"Expected exactly one OperationMapSceneView in {scene.path}, found {views.Length}.");
            return views[0];
        }

        private static Scene OpenSceneForInspection(string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException($"Failed to open scene for inspection: {path}");
            return scene;
        }

        private static void RequireCleanLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                    throw new InvalidOperationException($"Refuse dirty scene before probe: {scene.path}");
            }
        }

        private static void RestoreSceneSetup(SceneSetup[] previousSetup)
        {
            if (previousSetup == null || previousSetup.Length == 0)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                return;
            }
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        private static void PublishJsonAtomically(string outputPath, string json)
        {
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            string tempPath = outputPath + ".tmp";
            File.WriteAllText(tempPath, json, Utf8WithoutBom);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            File.Move(tempPath, outputPath);
        }

        private static string RequireProjectRoot()
        {
            if (string.IsNullOrWhiteSpace(Application.dataPath))
                throw new InvalidOperationException("Application.dataPath is unavailable.");
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        [Serializable]
        public sealed class DependencyInventoryReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public string capturedUtc;
            public string projectRoot;
            public string reportPath;
            public string canonicalMapScenePath;
            public string buildingPlacementConfigPath;
            public bool hideAuthoringVisualsAfterSpawn;
            public List<DependencyCatalogEntry> dependencyCatalog;
            public DependencyInventoryCountsReport counts;
            public List<DispositionCountReport> dispositionCounts;
            public List<PlacementDependencyReport> placements;
        }

        [Serializable]
        public sealed class DependencyInventorySummaryReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public string buildingPlacementConfigPath;
            public DependencyInventoryCountsReport counts;
            public List<DispositionCountReport> dispositionCounts;
        }

        [Serializable]
        public sealed class DependencyCatalogEntry
        {
            public string dependencyId;
            public string managedOwnerFieldOrSurface;
            public string currentConsumerHint;
            public string proposedEcsOwnership;
            public bool isApprovedTransientBoundary;
        }

        [Serializable]
        public sealed class DependencyInventoryCountsReport
        {
            public int placementCount;
            public int unresolvedJoinCount;
            public int missingPrefabCount;
            public int missingDefinitionAuthoringCount;
            public int requiresManagedRuntimeCount;
            public int hasDestroyedVisualCount;
            public int hasProductionCount;
            public int hasResourceCount;
            public int requiresRunwayCount;
            public bool hideAuthoringEnabled;
        }

        [Serializable]
        public sealed class DispositionCountReport
        {
            public string disposition;
            public int count;
        }

        [Serializable]
        public sealed class PlacementDependencyReport
        {
            public int placementIndex;
            public string sourcePath;
            public string category;
            public byte factionId;
            public string buildingPrefabPath;
            public string authoredJoinResolveState;
            public string authoredJoinResolutionMethod;
            public string authoredSourceGlobalObjectId;
            public bool hasBuildingPrefab;
            public bool hasBuildingDefinitionAuthoring;
            public bool hasDestroyedVisualPrefab;
            public int productionSlotCount;
            public int oilStorageCapacity;
            public int fuelStorageCapacity;
            public float oilBarrelsPerDay;
            public float fuelBarrelsPerDay;
            public bool hasCombat;
            public bool requiresRunwayOwnership;
            public bool hideAuthoringVisualsAfterSpawn;
            public int authoredSourceRendererCount;
            public List<string> dependencyFlags;
            public string conversionDisposition;
        }

        private sealed class PlacementJoinState
        {
            public string resolveState;
            public string resolutionMethod;
            public string resolvedSourceGlobalObjectId;
            public GameObject resolvedSource;
        }
    }
}

#endif
