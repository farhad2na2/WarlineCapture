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
    /// Read-only Phase 0A proof that current vehicle placements already produce ECS
    /// gameplay/render entities through UnitGridAuthoring bake + match-start spawn.
    /// Does not mutate scenes, SubScenes, Addressables, or presentation mode.
    /// </summary>
    public static class OperationMapVehicleEcsConversionInventoryProbe
    {
        internal const string ReportSchema = "warline.operation-map.vehicle-ecs-conversion-inventory";
        internal const int ReportSchemaVersion = 1;
        internal const string ReportPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_VEHICLE_ECS_CONVERSION_INVENTORY_REPORT_PATH";
        internal const string DefaultReportPath =
            "/private/tmp/warline-operation-map-vehicle-ecs-conversion-inventory.json";
        internal const string SummaryPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_VEHICLE_ECS_CONVERSION_INVENTORY_SUMMARY_PATH";
        internal const string DefaultSummaryPath =
            "/private/tmp/warline-operation-map-vehicle-ecs-conversion-inventory-summary.json";

        private const string CanonicalMapScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";

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
            ConversionReport report;
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
                $"[OperationMapVehicleEcsConversionInventoryProbe] result={report.result} " +
                $"placements={report.counts.placementCount} alreadyReady={report.counts.alreadyReadyCount} " +
                $"cleanupRequired={report.counts.cleanupRequiredCount} unresolved={report.counts.unresolvedJoinCount} " +
                $"report={outputPath} summary={summaryPath}");
        }

        internal static bool HasRequiredReportShape(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            ConversionReport report;
            try
            {
                report = JsonUtility.FromJson<ConversionReport>(json);
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
                   report.placements != null &&
                   report.dispositionCounts != null;
        }

        internal static string ClassifyConversionDisposition(
            bool exactAuthoredJoin,
            bool hasVehiclePrefab,
            bool hasUnitGridAuthoring,
            bool usesVehicleMotion,
            bool hasModelVisualRoot,
            bool hasModelRenderers,
            bool hasDestroyedVisualPrefab,
            bool hideAuthoringAfterSpawn,
            bool hasSourceKey)
        {
            if (!exactAuthoredJoin)
                return "UnresolvedAuthoredSourceJoin";
            if (!hasVehiclePrefab)
                return "MissingVehiclePrefab";
            if (!hasSourceKey)
                return "MissingVehicleSourceKey";
            if (!hasUnitGridAuthoring)
                return "MissingUnitGridAuthoring";
            if (!usesVehicleMotion)
                return "UnitGridAuthoringNotVehicleMotion";
            if (!hasModelVisualRoot || !hasModelRenderers)
                return "MissingModelRenderEntityRoot";
            if (!hideAuthoringAfterSpawn)
                return "DuplicateAuthoringVisualRisk";
            if (!hasDestroyedVisualPrefab)
                return "AlreadyProducesEcsMissingDestroyedVisual";
            return "AlreadyProducesEcsGameplayAndRender";
        }

        internal static bool IsAlreadyReadyDisposition(string disposition)
        {
            return string.Equals(disposition, "AlreadyProducesEcsGameplayAndRender", StringComparison.Ordinal) ||
                   string.Equals(disposition, "AlreadyProducesEcsMissingDestroyedVisual", StringComparison.Ordinal);
        }

        internal static bool IsCleanupRequiredDisposition(string disposition)
        {
            return !string.IsNullOrWhiteSpace(disposition) &&
                   !IsAlreadyReadyDisposition(disposition) &&
                   !string.Equals(disposition, "UnresolvedAuthoredSourceJoin", StringComparison.Ordinal);
        }

        private static ConversionReport BuildReport(string projectRoot, string outputPath)
        {
            Scene authoringScene = OpenSceneForInspection(CanonicalMapScenePath);
            OperationMapSceneView mapView = RequireSingleOperationMapSceneView(authoringScene);
            MapVehiclePlacementConfig config = mapView.VehiclePlacements;
            if (config == null || config.Placements == null)
                throw new InvalidOperationException("Vehicle placement config is missing.");

            Dictionary<string, List<GameObject>> objectsByNamePath = IndexSceneObjectsByNamePath(authoringScene);
            var placements = new List<PlacementConversionReport>(config.Placements.Count);
            var dispositionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            int alreadyReady = 0;
            int cleanupRequired = 0;
            int unresolvedJoins = 0;
            int missingPrefab = 0;
            int missingAuthoring = 0;
            int missingRenderRoot = 0;
            int duplicateRisk = 0;
            int missingDestroyedVisual = 0;

            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapVehiclePlacementConfigEntry placement = config.Placements[i];
                PlacementJoinState join = ResolveAuthoredJoin(
                    placement.SourcePath,
                    placement.WorldPosition,
                    placement.WorldEulerAngles,
                    placement.WorldScale,
                    objectsByNamePath);

                GameObject prefab = placement.VehiclePrefab;
                string prefabPath = prefab == null ? string.Empty : AssetDatabase.GetAssetPath(prefab);
                UnitGridAuthoring unitGrid = prefab == null ? null : prefab.GetComponent<UnitGridAuthoring>();
                Transform modelRoot = ResolveModelRoot(unitGrid, prefab);
                int modelRendererCount = CountRenderers(modelRoot);
                bool hasDestroyedVisual = unitGrid != null && unitGrid.VehicleDestroyedVisualPrefab != null;
                string sourceKey = placement.VehicleSourceKey ?? string.Empty;
                bool hasSourceKey = !string.IsNullOrWhiteSpace(sourceKey);
                bool usesVehicleMotion = unitGrid != null && unitGrid.UsesVehicleMotion;
                bool hideAuthoring = config.HideAuthoringVisualsAfterSpawn;

                string disposition = ClassifyConversionDisposition(
                    exactAuthoredJoin: string.Equals(join.resolveState, "Exact", StringComparison.Ordinal),
                    hasVehiclePrefab: prefab != null,
                    hasUnitGridAuthoring: unitGrid != null,
                    usesVehicleMotion: usesVehicleMotion,
                    hasModelVisualRoot: modelRoot != null,
                    hasModelRenderers: modelRendererCount > 0,
                    hasDestroyedVisualPrefab: hasDestroyedVisual,
                    hideAuthoringAfterSpawn: hideAuthoring,
                    hasSourceKey: hasSourceKey);

                if (!dispositionCounts.TryGetValue(disposition, out int count))
                    count = 0;
                dispositionCounts[disposition] = count + 1;

                if (IsAlreadyReadyDisposition(disposition))
                    alreadyReady++;
                if (IsCleanupRequiredDisposition(disposition))
                    cleanupRequired++;
                if (string.Equals(disposition, "UnresolvedAuthoredSourceJoin", StringComparison.Ordinal))
                    unresolvedJoins++;
                if (string.Equals(disposition, "MissingVehiclePrefab", StringComparison.Ordinal))
                    missingPrefab++;
                if (string.Equals(disposition, "MissingUnitGridAuthoring", StringComparison.Ordinal) ||
                    string.Equals(disposition, "UnitGridAuthoringNotVehicleMotion", StringComparison.Ordinal))
                    missingAuthoring++;
                if (string.Equals(disposition, "MissingModelRenderEntityRoot", StringComparison.Ordinal))
                    missingRenderRoot++;
                if (string.Equals(disposition, "DuplicateAuthoringVisualRisk", StringComparison.Ordinal))
                    duplicateRisk++;
                if (string.Equals(disposition, "AlreadyProducesEcsMissingDestroyedVisual", StringComparison.Ordinal))
                    missingDestroyedVisual++;

                placements.Add(new PlacementConversionReport
                {
                    placementIndex = i,
                    sourcePath = placement.SourcePath ?? string.Empty,
                    category = placement.Category ?? string.Empty,
                    factionId = placement.FactionId,
                    vehicleSourceKey = sourceKey,
                    vehiclePrefabPath = prefabPath,
                    authoredJoinResolveState = join.resolveState,
                    authoredJoinResolutionMethod = join.resolutionMethod,
                    authoredSourceGlobalObjectId = join.resolvedSourceGlobalObjectId,
                    hasVehiclePrefab = prefab != null,
                    hasUnitGridAuthoring = unitGrid != null,
                    usesVehicleMotion = usesVehicleMotion,
                    modelRootPath = modelRoot == null
                        ? string.Empty
                        : BuildRelativePath(prefab.transform, modelRoot),
                    modelRendererCount = modelRendererCount,
                    hasDestroyedVisualPrefab = hasDestroyedVisual,
                    destroyedVisualPrefabPath = hasDestroyedVisual
                        ? AssetDatabase.GetAssetPath(unitGrid.VehicleDestroyedVisualPrefab)
                        : string.Empty,
                    hideAuthoringVisualsAfterSpawn = hideAuthoring,
                    producesEcsGameplayEntity = unitGrid != null && usesVehicleMotion,
                    producesEcsRenderEntity = modelRoot != null && modelRendererCount > 0,
                    conversionDisposition = disposition,
                    runtimeSpawnPath =
                        "MapVehiclePlacementSpawnPrefabSystemHelper->RuntimeUnitPrefabSystem->" +
                        "UnitGridAuthoring.UnitGridBaker"
                });
            }

            placements = placements
                .OrderBy(row => row.placementIndex)
                .ThenBy(row => row.sourcePath, StringComparer.Ordinal)
                .ToList();

            string result = unresolvedJoins == 0 && cleanupRequired == 0
                ? "AllPlacementsAlreadyProduceEcs"
                : unresolvedJoins == 0
                    ? "CleanupRequiredBeforeCutover"
                    : "UnresolvedJoinsPendingReview";

            return new ConversionReport
            {
                reportSchema = ReportSchema,
                reportSchemaVersion = ReportSchemaVersion,
                result = result,
                capturedUtc = DateTime.UtcNow.ToString("o"),
                projectRoot = projectRoot,
                reportPath = outputPath,
                canonicalMapScenePath = CanonicalMapScenePath,
                vehiclePlacementConfigPath = AssetDatabase.GetAssetPath(config),
                spawnOnMatchStart = config.SpawnOnMatchStart,
                hideAuthoringVisualsAfterSpawn = config.HideAuthoringVisualsAfterSpawn,
                notes = new List<string>
                {
                    "Editor-time proof inspects authored placement joins and vehicle prefab UnitGridAuthoring bake inputs.",
                    "Runtime ECS prefab entity resolution still occurs through RuntimeUnitPrefabSystem at match start.",
                    "Does not mutate scenes, SubScenes, Addressables, or OperationMapPresentationKind."
                },
                counts = new ConversionCountsReport
                {
                    placementCount = placements.Count,
                    alreadyReadyCount = alreadyReady,
                    cleanupRequiredCount = cleanupRequired,
                    unresolvedJoinCount = unresolvedJoins,
                    missingPrefabCount = missingPrefab,
                    missingAuthoringCount = missingAuthoring,
                    missingRenderRootCount = missingRenderRoot,
                    duplicateAuthoringRiskCount = duplicateRisk,
                    missingDestroyedVisualCount = missingDestroyedVisual
                },
                dispositionCounts = dispositionCounts
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new DispositionCountReport
                    {
                        disposition = pair.Key,
                        count = pair.Value
                    })
                    .ToList(),
                placements = placements
            };
        }

        private static ConversionSummaryReport BuildSummary(ConversionReport report)
        {
            return new ConversionSummaryReport
            {
                reportSchema = report.reportSchema,
                reportSchemaVersion = report.reportSchemaVersion,
                result = report.result,
                vehiclePlacementConfigPath = report.vehiclePlacementConfigPath,
                placementCount = report.counts.placementCount,
                alreadyReadyCount = report.counts.alreadyReadyCount,
                cleanupRequiredCount = report.counts.cleanupRequiredCount,
                unresolvedJoinCount = report.counts.unresolvedJoinCount,
                dispositionCounts = report.dispositionCounts
            };
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
                    candidate.transform,
                    worldPosition,
                    worldEulerAngles,
                    worldScale))
                .ToList();
            List<GameObject> matches = pathMatches.Count <= 1
                ? pathMatches
                : transformMatches;

            if (matches.Count == 1)
            {
                return new PlacementJoinState
                {
                    resolveState = "Exact",
                    resolutionMethod = pathMatches.Count == 1
                        ? "UniqueHierarchyPath"
                        : "UniqueHierarchyPathAndTransformTuple",
                    resolvedSourceGlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(matches[0]).ToString()
                };
            }

            if (pathMatches.Count == 0)
            {
                return new PlacementJoinState
                {
                    resolveState = "Unresolved",
                    resolutionMethod = "HierarchyPathMissing",
                    resolvedSourceGlobalObjectId = string.Empty
                };
            }

            return new PlacementJoinState
            {
                resolveState = "Ambiguous",
                resolutionMethod = transformMatches.Count == 0
                    ? "NoTransformTupleMatchAmongPathCandidates"
                    : "MultipleTransformTupleMatches",
                resolvedSourceGlobalObjectId = string.Empty
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
            {
                return false;
            }

            Quaternion expectedRotation = Quaternion.Euler(expectedEulerAngles);
            return Quaternion.Angle(candidate.rotation, expectedRotation) <= RotationToleranceDegrees;
        }

        private static Transform ResolveModelRoot(UnitGridAuthoring authoring, GameObject prefab)
        {
            if (authoring != null)
            {
                var serialized = new SerializedObject(authoring);
                var modelRootProperty = serialized.FindProperty("modelRoot");
                if (modelRootProperty != null &&
                    modelRootProperty.objectReferenceValue is Transform configuredRoot &&
                    configuredRoot != null)
                {
                    return configuredRoot;
                }
            }

            if (prefab == null)
                return null;

            Transform fallback = prefab.transform.Find("Model");
            return fallback;
        }

        private static int CountRenderers(Transform root)
        {
            if (root == null)
                return 0;
            return root.GetComponentsInChildren<Renderer>(true).Length;
        }

        private static string BuildRelativePath(Transform root, Transform target)
        {
            if (root == null || target == null)
                return string.Empty;
            if (target == root)
                return root.name;

            var parts = new List<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            if (current != root)
                return target.name;

            parts.Reverse();
            return string.Join("/", parts);
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
            if (!index.TryGetValue(path, out List<GameObject> list))
            {
                list = new List<GameObject>();
                index[path] = list;
            }

            list.Add(transform.gameObject);
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
            string dataPath = Application.dataPath;
            if (string.IsNullOrWhiteSpace(dataPath))
                throw new InvalidOperationException("Application.dataPath is unavailable.");
            return Path.GetFullPath(Path.Combine(dataPath, ".."));
        }

        [Serializable]
        public sealed class ConversionReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public string capturedUtc;
            public string projectRoot;
            public string reportPath;
            public string canonicalMapScenePath;
            public string vehiclePlacementConfigPath;
            public bool spawnOnMatchStart;
            public bool hideAuthoringVisualsAfterSpawn;
            public List<string> notes;
            public ConversionCountsReport counts;
            public List<DispositionCountReport> dispositionCounts;
            public List<PlacementConversionReport> placements;
        }

        [Serializable]
        public sealed class ConversionSummaryReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public string vehiclePlacementConfigPath;
            public int placementCount;
            public int alreadyReadyCount;
            public int cleanupRequiredCount;
            public int unresolvedJoinCount;
            public List<DispositionCountReport> dispositionCounts;
        }

        [Serializable]
        public sealed class ConversionCountsReport
        {
            public int placementCount;
            public int alreadyReadyCount;
            public int cleanupRequiredCount;
            public int unresolvedJoinCount;
            public int missingPrefabCount;
            public int missingAuthoringCount;
            public int missingRenderRootCount;
            public int duplicateAuthoringRiskCount;
            public int missingDestroyedVisualCount;
        }

        [Serializable]
        public sealed class DispositionCountReport
        {
            public string disposition;
            public int count;
        }

        [Serializable]
        public sealed class PlacementConversionReport
        {
            public int placementIndex;
            public string sourcePath;
            public string category;
            public byte factionId;
            public string vehicleSourceKey;
            public string vehiclePrefabPath;
            public string authoredJoinResolveState;
            public string authoredJoinResolutionMethod;
            public string authoredSourceGlobalObjectId;
            public bool hasVehiclePrefab;
            public bool hasUnitGridAuthoring;
            public bool usesVehicleMotion;
            public string modelRootPath;
            public int modelRendererCount;
            public bool hasDestroyedVisualPrefab;
            public string destroyedVisualPrefabPath;
            public bool hideAuthoringVisualsAfterSpawn;
            public bool producesEcsGameplayEntity;
            public bool producesEcsRenderEntity;
            public string conversionDisposition;
            public string runtimeSpawnPath;
        }

        private sealed class PlacementJoinState
        {
            public string resolveState;
            public string resolutionMethod;
            public string resolvedSourceGlobalObjectId;
        }
    }
}

#endif
