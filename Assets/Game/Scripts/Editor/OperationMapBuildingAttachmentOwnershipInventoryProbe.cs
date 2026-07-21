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
    /// Read-only Phase 0A inventory of building visual attachment ownership.
    /// Ownership is established only by an exact authored source join or a configured destroyed prefab reference.
    /// </summary>
    public static class OperationMapBuildingAttachmentOwnershipInventoryProbe
    {
        internal const string ReportSchema =
            "warline.operation-map.building-attachment-ownership-inventory";
        internal const int ReportSchemaVersion = 1;
        internal const string ReportPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_BUILDING_ATTACHMENT_OWNERSHIP_INVENTORY_REPORT_PATH";
        internal const string DefaultReportPath =
            "/private/tmp/warline-operation-map-building-attachment-ownership-inventory.json";
        internal const string SummaryPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_BUILDING_ATTACHMENT_OWNERSHIP_INVENTORY_SUMMARY_PATH";
        internal const string DefaultSummaryPath =
            "/private/tmp/warline-operation-map-building-attachment-ownership-inventory-summary.json";

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
            AttachmentOwnershipInventoryReport report;
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
                $"[OperationMapBuildingAttachmentOwnershipInventoryProbe] result={report.result} " +
                $"placements={report.counts.placementCount} intact={report.counts.intactAttachmentCount} " +
                $"destroyed={report.counts.destroyedAttachmentCount} orphans={report.counts.unassignedOrphanCount} " +
                $"shared={report.counts.sharedAcrossBuildingsCount} report={outputPath} summary={summaryPath}");
        }

        internal static string ClassifyIntactDisposition(bool exactJoin, bool sourceReused)
        {
            if (!exactJoin)
                return "UnresolvedAuthoredSourceJoin";
            return sourceReused ? "SharedAcrossBuildings" : "AssignedIntact";
        }

        internal static string ClassifyOrphanDisposition()
        {
            return "UnassignedOrphan";
        }

        internal static string BuildClaimKey(
            string ownerSourceGlobalObjectId,
            string destroyedPrefabGuid,
            string destroyedPrefabLocalId)
        {
            return $"{ownerSourceGlobalObjectId ?? string.Empty}|{destroyedPrefabGuid ?? string.Empty}|" +
                $"{destroyedPrefabLocalId ?? string.Empty}";
        }

        internal static bool IsSuccessResult(string result)
        {
            return string.Equals(
                result,
                "AttachmentOwnershipInventoryComplete",
                StringComparison.Ordinal);
        }

        internal static bool HasRequiredReportShape(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            AttachmentOwnershipInventoryReport report;
            try
            {
                report = JsonUtility.FromJson<AttachmentOwnershipInventoryReport>(json);
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
                   report.attachments != null &&
                   report.dispositionCounts != null;
        }

        private static AttachmentOwnershipInventoryReport BuildReport(string projectRoot, string outputPath)
        {
            Scene authoringScene = OpenSceneForInspection(CanonicalMapScenePath);
            OperationMapSceneView mapView = RequireSingleOperationMapSceneView(authoringScene);
            MapBuildingPlacementConfig config = mapView.BuildingPlacements;
            if (config == null || config.Placements == null)
                throw new InvalidOperationException("Building placement config is missing.");

            Dictionary<string, List<GameObject>> objectsByNamePath = IndexSceneObjectsByNamePath(authoringScene);
            var joins = new List<PlacementJoinState>(config.Placements.Count);
            var placements = new List<BuildingPlacementReport>(config.Placements.Count);
            var sourcePlacementIndices = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            var sourceRoots = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            var counts = new AttachmentOwnershipCountsReport { placementCount = config.Placements.Count };

            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapBuildingPlacementConfigEntry placement = config.Placements[i];
                PlacementJoinState join = ResolveAuthoredJoin(
                    placement.SourcePath,
                    placement.WorldPosition,
                    placement.WorldEulerAngles,
                    placement.WorldScale,
                    objectsByNamePath);
                joins.Add(join);
                if (string.Equals(join.resolveState, "Exact", StringComparison.Ordinal))
                {
                    counts.exactJoinCount++;
                    sourceRoots[join.resolvedSourceGlobalObjectId] = join.resolvedSource;
                    if (!sourcePlacementIndices.TryGetValue(
                            join.resolvedSourceGlobalObjectId,
                            out List<int> placementIndices))
                    {
                        placementIndices = new List<int>();
                        sourcePlacementIndices.Add(join.resolvedSourceGlobalObjectId, placementIndices);
                    }
                    placementIndices.Add(i);
                }
                else
                {
                    counts.unresolvedJoinCount++;
                }
            }

            var reusedSourceIds = new HashSet<string>(
                sourcePlacementIndices
                    .Where(pair => pair.Value.Count > 1)
                    .Select(pair => pair.Key),
                StringComparer.Ordinal);
            counts.reusedSourceJoinCount = sourcePlacementIndices
                .Where(pair => pair.Value.Count > 1)
                .Sum(pair => pair.Value.Count);

            var attachments = new List<AttachmentOwnershipReport>();
            var dispositionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var intactOwnersByRendererId = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
            var destroyedClaimKeys = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapBuildingPlacementConfigEntry placement = config.Placements[i];
                PlacementJoinState join = joins[i];
                bool exactJoin = string.Equals(join.resolveState, "Exact", StringComparison.Ordinal);
                bool sourceReused = exactJoin && reusedSourceIds.Contains(join.resolvedSourceGlobalObjectId);
                string placementDisposition = ClassifyIntactDisposition(exactJoin, sourceReused);

                GameObject buildingPrefab = placement.BuildingPrefab;
                BuildingDefinitionAuthoring authoring = buildingPrefab == null
                    ? null
                    : buildingPrefab.GetComponentInChildren<BuildingDefinitionAuthoring>(true);
                if (authoring != null)
                    authoring.ApplyConfigIfAvailable();
                GameObject destroyedPrefab = authoring == null
                    ? null
                    : authoring.ConfiguredDestroyedVisualPrefab;

                placements.Add(new BuildingPlacementReport
                {
                    placementIndex = i,
                    sourcePath = placement.SourcePath ?? string.Empty,
                    category = placement.Category ?? string.Empty,
                    authoredJoinResolveState = join.resolveState,
                    authoredJoinResolutionMethod = join.resolutionMethod,
                    ownerSourceGlobalObjectId = join.resolvedSourceGlobalObjectId,
                    intactDisposition = placementDisposition,
                    buildingPrefabPath = buildingPrefab == null ? string.Empty : AssetDatabase.GetAssetPath(buildingPrefab),
                    hasConfiguredDestroyedVisualPrefab = destroyedPrefab != null,
                    destroyedPrefabPath = destroyedPrefab == null ? string.Empty : AssetDatabase.GetAssetPath(destroyedPrefab)
                });

                if (!exactJoin)
                    continue;

                foreach (Renderer renderer in join.resolvedSource.GetComponentsInChildren<Renderer>(true))
                {
                    string rendererId = GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString();
                    attachments.Add(new AttachmentOwnershipReport
                    {
                        attachmentIdentityKind = "SceneRendererGlobalObjectId",
                        sceneRendererGlobalObjectId = rendererId,
                        sceneObjectGlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(renderer.gameObject).ToString(),
                        ownerPlacementIndex = i,
                        ownerSourceGlobalObjectId = join.resolvedSourceGlobalObjectId,
                        state = "Intact",
                        ownershipEvidence = "ExactBuildingSourceAncestor",
                        disposition = placementDisposition
                    });
                    counts.intactAttachmentCount++;
                    if (string.Equals(placementDisposition, "AssignedIntact", StringComparison.Ordinal))
                        counts.assignedIntactCount++;
                    if (!intactOwnersByRendererId.TryGetValue(rendererId, out HashSet<int> ownerIndices))
                    {
                        ownerIndices = new HashSet<int>();
                        intactOwnersByRendererId.Add(rendererId, ownerIndices);
                    }
                    ownerIndices.Add(i);
                }

                if (destroyedPrefab != null)
                    InventoryDestroyedPrefabRenderers(
                        destroyedPrefab,
                        i,
                        join.resolvedSourceGlobalObjectId,
                        attachments,
                        destroyedClaimKeys,
                        counts);
            }

            counts.placementsWithDestroyedVisualCount = placements.Count(
                placement => placement.hasConfiguredDestroyedVisualPrefab);
            counts.sharedAcrossBuildingsCount = intactOwnersByRendererId.Count(
                pair => pair.Value.Count > 1);
            foreach (AttachmentOwnershipReport attachment in attachments)
            {
                if (string.Equals(attachment.state, "Intact", StringComparison.Ordinal) &&
                    intactOwnersByRendererId.TryGetValue(
                        attachment.sceneRendererGlobalObjectId,
                        out HashSet<int> owners) &&
                    owners.Count > 1)
                {
                    if (string.Equals(attachment.disposition, "AssignedIntact", StringComparison.Ordinal))
                        counts.assignedIntactCount = Math.Max(0, counts.assignedIntactCount - 1);
                    attachment.disposition = "SharedAcrossBuildings";
                }
            }

            // Rebuild attachment disposition counts after shared-conflict rewriting.
            dispositionCounts.Clear();
            foreach (AttachmentOwnershipReport attachment in attachments)
                AddDisposition(dispositionCounts, attachment.disposition);

            InventoryOrphans(
                mapView,
                authoringScene,
                sourceRoots.Values,
                attachments,
                dispositionCounts,
                counts);

            counts.dualStateCount = CountDualStateAttachments(attachments);
            string result = counts.unresolvedJoinCount > 0
                ? "UnresolvedBuildingJoinsPendingReview"
                : counts.unassignedOrphanCount == 0 &&
                  counts.sharedAcrossBuildingsCount == 0 &&
                  counts.dualStateCount == 0
                    ? "AttachmentOwnershipInventoryComplete"
                    : "AttachmentOwnershipHasOrphansOrConflicts";

            return new AttachmentOwnershipInventoryReport
            {
                reportSchema = ReportSchema,
                reportSchemaVersion = ReportSchemaVersion,
                result = result,
                capturedUtc = DateTime.UtcNow.ToString("o"),
                projectRoot = projectRoot,
                reportPath = outputPath,
                canonicalMapScenePath = CanonicalMapScenePath,
                buildingPlacementConfigPath = AssetDatabase.GetAssetPath(config),
                notes = new List<string>
                {
                    "Renderer components are inventoried as attachment identities for stable scene and prefab references.",
                    "Ownership is inferred only from exact building-source ancestry or configured destroyed visual prefab references.",
                    "No name-based, proximity-based, or semantic attachment role inference is performed.",
                    "Does not mutate scenes, SubScenes, Addressables, presentation mode, or assets."
                },
                counts = counts,
                dispositionCounts = dispositionCounts
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new DispositionCountReport
                    {
                        disposition = pair.Key,
                        count = pair.Value
                    })
                    .ToList(),
                placements = placements.OrderBy(placement => placement.placementIndex).ToList(),
                attachments = attachments
                    .OrderBy(attachment => attachment.ownerPlacementIndex)
                    .ThenBy(attachment => attachment.attachmentIdentityKind, StringComparer.Ordinal)
                    .ThenBy(attachment => attachment.sceneRendererGlobalObjectId, StringComparer.Ordinal)
                    .ThenBy(attachment => attachment.destroyedPrefabGuid, StringComparer.Ordinal)
                    .ThenBy(attachment => attachment.destroyedPrefabLocalId, StringComparer.Ordinal)
                    .ToList()
            };
        }

        private static void InventoryDestroyedPrefabRenderers(
            GameObject destroyedPrefab,
            int placementIndex,
            string ownerSourceGlobalObjectId,
            List<AttachmentOwnershipReport> attachments,
            HashSet<string> destroyedClaimKeys,
            AttachmentOwnershipCountsReport counts)
        {
            string prefabPath = AssetDatabase.GetAssetPath(destroyedPrefab);
            string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
            foreach (Renderer renderer in destroyedPrefab.GetComponentsInChildren<Renderer>(true))
            {
                GetPrefabIdentity(renderer, out string rendererGuid, out string localId);
                string guid = string.IsNullOrWhiteSpace(rendererGuid) ? prefabGuid : rendererGuid;
                string claimKey = BuildClaimKey(ownerSourceGlobalObjectId, guid, localId);
                if (!destroyedClaimKeys.Add(claimKey))
                    throw new InvalidOperationException(
                        $"Duplicate destroyed attachment claim key: {claimKey}");

                attachments.Add(new AttachmentOwnershipReport
                {
                    attachmentIdentityKind = "DestroyedPrefabRendererGuidLocalId",
                    destroyedPrefabGuid = guid,
                    destroyedPrefabLocalId = localId,
                    destroyedPrefabPath = prefabPath,
                    ownerPlacementIndex = placementIndex,
                    ownerSourceGlobalObjectId = ownerSourceGlobalObjectId,
                    state = "Destroyed",
                    ownershipEvidence = "ConfiguredDestroyedPrefabReference",
                    disposition = "AssignedDestroyed"
                });
                counts.destroyedAttachmentCount++;
                counts.assignedDestroyedCount++;
            }
        }

        private static void GetPrefabIdentity(Renderer renderer, out string guid, out string localId)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    renderer,
                    out guid,
                    out long localFileId))
            {
                localId = localFileId.ToString();
                return;
            }

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    renderer.gameObject,
                    out guid,
                    out localFileId))
            {
                localId = localFileId.ToString();
                return;
            }

            guid = string.Empty;
            localId = string.Empty;
        }

        private static void InventoryOrphans(
            OperationMapSceneView mapView,
            Scene scene,
            IEnumerable<GameObject> exactSourceRoots,
            List<AttachmentOwnershipReport> attachments,
            Dictionary<string, int> dispositionCounts,
            AttachmentOwnershipCountsReport counts)
        {
            Transform buildingsRoot = FindBuildingsRoot(mapView, scene);
            if (buildingsRoot == null)
                return;

            Transform[] sources = exactSourceRoots
                .Where(source => source != null)
                .Select(source => source.transform)
                .ToArray();
            foreach (Renderer renderer in buildingsRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (sources.Any(source => renderer.transform.IsChildOf(source)))
                    continue;

                string disposition = ClassifyOrphanDisposition();
                attachments.Add(new AttachmentOwnershipReport
                {
                    attachmentIdentityKind = "SceneRendererGlobalObjectId",
                    sceneRendererGlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString(),
                    sceneObjectGlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(renderer.gameObject).ToString(),
                    ownerPlacementIndex = -1,
                    state = "Unknown",
                    ownershipEvidence = "NoExactBuildingSourceAncestor",
                    disposition = disposition
                });
                counts.unassignedOrphanCount++;
                AddDisposition(dispositionCounts, disposition);
            }
        }

        private static Transform FindBuildingsRoot(OperationMapSceneView mapView, Scene scene)
        {
            if (mapView.MapRoot != null)
            {
                for (int i = 0; i < mapView.MapRoot.childCount; i++)
                {
                    Transform child = mapView.MapRoot.GetChild(i);
                    if (string.Equals(child.name, "Buildings", StringComparison.Ordinal))
                        return child;
                }
            }

            Transform[] candidates = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => string.Equals(transform.name, "Buildings", StringComparison.Ordinal))
                .ToArray();
            return candidates.Length == 1 ? candidates[0] : null;
        }

        private static int CountDualStateAttachments(List<AttachmentOwnershipReport> attachments)
        {
            var statesByIdentity = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (AttachmentOwnershipReport attachment in attachments)
            {
                string identity = attachment.attachmentIdentityKind + "|" +
                    (string.Equals(
                        attachment.attachmentIdentityKind,
                        "SceneRendererGlobalObjectId",
                        StringComparison.Ordinal)
                        ? attachment.sceneRendererGlobalObjectId
                        : attachment.destroyedPrefabGuid + ":" + attachment.destroyedPrefabLocalId);
                if (!statesByIdentity.TryGetValue(identity, out HashSet<string> states))
                {
                    states = new HashSet<string>(StringComparer.Ordinal);
                    statesByIdentity.Add(identity, states);
                }
                states.Add(attachment.state);
            }
            return statesByIdentity.Count(pair => pair.Value.Contains("Intact") && pair.Value.Contains("Destroyed"));
        }

        private static void AddDisposition(Dictionary<string, int> counts, string disposition)
        {
            counts[disposition] = counts.TryGetValue(disposition, out int current) ? current + 1 : 1;
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
        public sealed class AttachmentOwnershipInventoryReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public string capturedUtc;
            public string projectRoot;
            public string reportPath;
            public string canonicalMapScenePath;
            public string buildingPlacementConfigPath;
            public List<string> notes;
            public AttachmentOwnershipCountsReport counts;
            public List<DispositionCountReport> dispositionCounts;
            public List<BuildingPlacementReport> placements;
            public List<AttachmentOwnershipReport> attachments;
        }

        [Serializable]
        public sealed class AttachmentOwnershipInventorySummaryReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public string buildingPlacementConfigPath;
            public AttachmentOwnershipCountsReport counts;
            public List<DispositionCountReport> dispositionCounts;
        }

        [Serializable]
        public sealed class AttachmentOwnershipCountsReport
        {
            public int placementCount;
            public int exactJoinCount;
            public int unresolvedJoinCount;
            public int reusedSourceJoinCount;
            public int intactAttachmentCount;
            public int destroyedAttachmentCount;
            public int assignedIntactCount;
            public int assignedDestroyedCount;
            public int unassignedOrphanCount;
            public int sharedAcrossBuildingsCount;
            public int dualStateCount;
            public int placementsWithDestroyedVisualCount;
        }

        [Serializable]
        public sealed class DispositionCountReport
        {
            public string disposition;
            public int count;
        }

        [Serializable]
        public sealed class BuildingPlacementReport
        {
            public int placementIndex;
            public string sourcePath;
            public string category;
            public string authoredJoinResolveState;
            public string authoredJoinResolutionMethod;
            public string ownerSourceGlobalObjectId;
            public string intactDisposition;
            public string buildingPrefabPath;
            public bool hasConfiguredDestroyedVisualPrefab;
            public string destroyedPrefabPath;
        }

        [Serializable]
        public sealed class AttachmentOwnershipReport
        {
            public string attachmentIdentityKind;
            public string sceneRendererGlobalObjectId;
            public string sceneObjectGlobalObjectId;
            public string destroyedPrefabGuid;
            public string destroyedPrefabLocalId;
            public string destroyedPrefabPath;
            public int ownerPlacementIndex;
            public string ownerSourceGlobalObjectId;
            public string state;
            public string ownershipEvidence;
            public string disposition;
        }

        private sealed class PlacementJoinState
        {
            public string resolveState;
            public string resolutionMethod;
            public string resolvedSourceGlobalObjectId;
            public GameObject resolvedSource;
        }

        private static AttachmentOwnershipInventorySummaryReport BuildSummary(
            AttachmentOwnershipInventoryReport report)
        {
            return new AttachmentOwnershipInventorySummaryReport
            {
                reportSchema = report.reportSchema,
                reportSchemaVersion = report.reportSchemaVersion,
                result = report.result,
                buildingPlacementConfigPath = report.buildingPlacementConfigPath,
                counts = report.counts,
                dispositionCounts = report.dispositionCounts
            };
        }
    }
}

#endif
