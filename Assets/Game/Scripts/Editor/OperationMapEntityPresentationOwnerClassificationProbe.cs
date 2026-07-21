#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Game.Composition;
    using Game.Configs;
    using Game.Rendering;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Read-only Phase 0A classification of manifest renderer migration owners.
    /// Classification is evidence-only: exact placement joins and dependency inspection; never names.
    /// </summary>
    public static class OperationMapEntityPresentationOwnerClassificationProbe
    {
        internal const string ReportSchema =
            "warline.operation-map.entity-presentation-owner-classification";
        internal const int ReportSchemaVersion = 1;
        internal const string ReportPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_ENTITY_PRESENTATION_OWNER_CLASSIFICATION_REPORT_PATH";
        internal const string DefaultReportPath =
            "/private/tmp/warline-operation-map-entity-presentation-owner-classification.json";
        internal const string SummaryPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_ENTITY_PRESENTATION_OWNER_CLASSIFICATION_SUMMARY_PATH";
        internal const string DefaultSummaryPath =
            "/private/tmp/warline-operation-map-entity-presentation-owner-classification-summary.json";

        internal const string GameplayBuilding = "GameplayBuilding";
        internal const string GameplayVehicle = "GameplayVehicle";
        internal const string RenderOnlyEntity = "RenderOnlyEntity";
        internal const string MapMetadataProxy = "MapMetadataProxy";
        internal const string ApprovedManagedBoundary = "ApprovedManagedBoundary";
        internal const string RejectedUnresolved = "RejectedUnresolved";

        private const string CanonicalMapScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
        private const string ManifestPath =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationManifest.asset";
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        public static void Run()
        {
            string projectRoot = RequireProjectRoot();
            string outputPath = OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot, Environment.GetEnvironmentVariable(ReportPathEnvironmentVariable) ?? DefaultReportPath);
            string summaryPath = OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot, Environment.GetEnvironmentVariable(SummaryPathEnvironmentVariable) ?? DefaultSummaryPath);

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            OwnerClassificationReport report;
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
                $"[OperationMapEntityPresentationOwnerClassificationProbe] result={report.result} " +
                $"owners={report.counts.migrationOwnerCount} buildings={report.counts.gameplayBuildingCount} " +
                $"vehicles={report.counts.gameplayVehicleCount} renderOnly={report.counts.renderOnlyEntityCount} " +
                $"rejected={report.counts.rejectedUnresolvedCount} report={outputPath} summary={summaryPath}");
        }

        internal static string ClassifyOwnerRole(
            bool hasExactBuildingJoin,
            bool hasExactVehicleJoin,
            bool hasMixedOrAmbiguousSource,
            bool hasUnresolvedSource,
            bool hasBlockingDependency,
            bool hasExternalSceneReference)
        {
            if (hasMixedOrAmbiguousSource || hasUnresolvedSource ||
                hasBlockingDependency || hasExternalSceneReference)
            {
                return RejectedUnresolved;
            }
            if (hasExactBuildingJoin && hasExactVehicleJoin)
                return RejectedUnresolved;
            if (hasExactBuildingJoin)
                return GameplayBuilding;
            if (hasExactVehicleJoin)
                return GameplayVehicle;
            return RenderOnlyEntity;
        }

        internal static bool RequiresApprovedManagedBoundaryUntilEcsCutover(string ownerRole)
        {
            return string.Equals(ownerRole, GameplayBuilding, StringComparison.Ordinal);
        }

        internal static List<CatalogEntry> BuildApprovedManagedBoundaryCatalog()
        {
            return new List<CatalogEntry>
            {
                new()
                {
                    proxyId = "door-open-state-fx",
                    description = "Door-open state effects remain an explicitly approved transient presentation boundary.",
                    proposedOwner = "RuntimeBuildingEntity transient presentation",
                    isVisualMigrationOwner = false,
                    isApprovedTransientBoundary = true
                },
                new()
                {
                    proxyId = "production-transport-drop-visuals",
                    description = "Production transport and drop visuals remain approved transient managed presentation.",
                    proposedOwner = "RuntimeBuildingEntity production presentation",
                    isVisualMigrationOwner = false,
                    isApprovedTransientBoundary = true
                },
                new()
                {
                    proxyId = "runtime-building-entity-interim-presentation",
                    description = "Instance hierarchy, RuntimeBuildingEntityLink, and intact/destroyed presentation are not approved permanent boundaries; RuntimeBuildingEntity is required until ECS building conversion.",
                    proposedOwner = "ECS building presentation after GPT cutover",
                    isVisualMigrationOwner = false,
                    isApprovedTransientBoundary = false
                }
            };
        }

        internal static List<CatalogEntry> BuildMapMetadataProxyCatalog()
        {
            return new List<CatalogEntry>
            {
                new() { proxyId = "dynamic-blocker-metadata", description = "Non-visual dynamic blocker metadata.", proposedOwner = "Map runtime metadata", isVisualMigrationOwner = false },
                new() { proxyId = "grid-config-surface-metadata", description = "Non-visual grid configuration and surface metadata.", proposedOwner = "Map grid configuration", isVisualMigrationOwner = false },
                new() { proxyId = "minimap-binding-metadata", description = "Non-visual minimap binding metadata.", proposedOwner = "Map minimap binding", isVisualMigrationOwner = false },
                new() { proxyId = "road-bridge-ramp-surface-blobs", description = "Non-visual road, bridge, and ramp surface blob metadata.", proposedOwner = "Map navigation/surface blobs", isVisualMigrationOwner = false },
                new() { proxyId = "runway-anchor-bounds-metadata", description = "Non-visual runway anchor and bounds metadata.", proposedOwner = "Map runway metadata", isVisualMigrationOwner = false }
            };
        }

        internal static bool HasRequiredReportShape(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;
            OwnerClassificationReport report;
            try { report = JsonUtility.FromJson<OwnerClassificationReport>(json); }
            catch { return false; }
            return report != null &&
                   string.Equals(report.reportSchema, ReportSchema, StringComparison.Ordinal) &&
                   report.reportSchemaVersion == ReportSchemaVersion &&
                   !string.IsNullOrWhiteSpace(report.result) &&
                   report.counts != null &&
                   report.counts.migrationOwnerCount >= 0 &&
                   report.ownerRows != null &&
                   report.countsByRole != null &&
                   report.approvedManagedBoundaryCatalog != null &&
                   report.mapMetadataProxyCatalog != null;
        }

        internal static bool IsSuccessResult(string result)
        {
            return string.Equals(result, "OwnerClassificationComplete", StringComparison.Ordinal);
        }

        private static OwnerClassificationReport BuildReport(string projectRoot, string outputPath)
        {
            StaticMapPresentationManifest manifest =
                AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(ManifestPath);
            if (manifest == null)
                throw new InvalidOperationException($"Missing manifest: {ManifestPath}");

            Scene scene = OpenSceneForInspection(CanonicalMapScenePath);
            OperationMapSceneView mapView = RequireSingleOperationMapSceneView(scene);
            Dictionary<string, Renderer> renderersById = IndexSceneRenderersByGlobalId(scene);
            Dictionary<string, List<GameObject>> objectsByNamePath = IndexSceneObjectsByNamePath(scene);
            PlacementJoinSet buildingJoins = BuildBuildingJoins(mapView.BuildingPlacements, objectsByNamePath);
            PlacementJoinSet vehicleJoins = BuildVehicleJoins(mapView.VehiclePlacements, objectsByNamePath);
            HashSet<Transform> sourceTransforms = new(
                manifest.Sources.Select(source => renderersById.TryGetValue(
                    source.SourceGlobalObjectId ?? string.Empty, out Renderer renderer) ? renderer.transform : null)
                .Where(transform => transform != null));
            var owners = new Dictionary<string, OwnerClassificationRow>(StringComparer.Ordinal);

            foreach (StaticMapPresentationSourceEntry source in manifest.Sources)
            {
                renderersById.TryGetValue(source.SourceGlobalObjectId ?? string.Empty, out Renderer renderer);
                if (renderer == null)
                    continue;
                GameObject owner = ResolveMigrationOwner(renderer.gameObject, mapView.MapRoot, sourceTransforms);
                string ownerId = GlobalObjectId.GetGlobalObjectIdSlow(owner).ToString();
                if (!owners.TryGetValue(ownerId, out OwnerClassificationRow row))
                {
                    row = CreateOwnerRow(owner, ownerId);
                    owners.Add(ownerId, row);
                }

                row.sourceRendererCount++;
                List<PlacementJoin> buildingMatches = ResolveSourceJoins(buildingJoins, renderer.gameObject);
                List<PlacementJoin> vehicleMatches = ResolveSourceJoins(vehicleJoins, renderer.gameObject);
                row.hasExactBuildingJoin |= buildingMatches.Count == 1;
                row.hasExactVehicleJoin |= vehicleMatches.Count == 1;
                row.hasMixedOrAmbiguousSource |= buildingMatches.Count > 1 || vehicleMatches.Count > 1;
            }

            int unresolvedSourceCount = manifest.Sources.Count(source =>
                !renderersById.ContainsKey(source.SourceGlobalObjectId ?? string.Empty));

            // Authored gameplay buildings/vehicles are placement-joined scene objects and are usually
            // absent from the static-presentation manifest. Classify those join targets explicitly.
            AddGameplayPlacementOwners(owners, buildingJoins, isBuilding: true);
            AddGameplayPlacementOwners(owners, vehicleJoins, isBuilding: false);

            foreach (OwnerClassificationRow row in owners.Values)
            {
                row.ownerRole = ClassifyOwnerRole(
                    row.hasExactBuildingJoin, row.hasExactVehicleJoin, row.hasMixedOrAmbiguousSource,
                    row.hasUnresolvedSource, row.hasBlockingDependency, row.hasExternalSceneReference);
                row.requiresApprovedManagedBoundaryUntilEcsCutover =
                    RequiresApprovedManagedBoundaryUntilEcsCutover(row.ownerRole);
                row.evidenceNotes = BuildEvidenceNotes(row);
            }

            List<RoleCount> countsByRole = owners.Values
                .GroupBy(row => row.ownerRole)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new RoleCount { ownerRole = group.Key, count = group.Count() })
                .ToList();
            int rejectedCount = owners.Values.Count(row =>
                string.Equals(row.ownerRole, RejectedUnresolved, StringComparison.Ordinal));
            int gameplayBuildingCount = owners.Values.Count(row =>
                string.Equals(row.ownerRole, GameplayBuilding, StringComparison.Ordinal));
            int gameplayVehicleCount = owners.Values.Count(row =>
                string.Equals(row.ownerRole, GameplayVehicle, StringComparison.Ordinal));
            int renderOnlyCount = owners.Values.Count(row =>
                string.Equals(row.ownerRole, RenderOnlyEntity, StringComparison.Ordinal));
            bool complete = rejectedCount == 0 &&
                            owners.Count > 0 &&
                            unresolvedSourceCount == 0 &&
                            gameplayBuildingCount > 0 &&
                            gameplayVehicleCount > 0;
            return new OwnerClassificationReport
            {
                reportSchema = ReportSchema,
                reportSchemaVersion = ReportSchemaVersion,
                result = complete ? "OwnerClassificationComplete" : "OwnerClassificationHasRejectedOwners",
                reportPath = outputPath,
                projectRootMarker = Path.GetFileName(projectRoot.TrimEnd(Path.DirectorySeparatorChar)),
                canonicalMapScenePath = CanonicalMapScenePath,
                manifestPath = ManifestPath,
                counts = new OwnerClassificationCounts
                {
                    migrationOwnerCount = owners.Count,
                    rejectedUnresolvedCount = rejectedCount,
                    unresolvedManifestSourceCount = unresolvedSourceCount,
                    gameplayBuildingCount = gameplayBuildingCount,
                    gameplayVehicleCount = gameplayVehicleCount,
                    renderOnlyEntityCount = renderOnlyCount,
                    mapMetadataProxyCatalogCount = BuildMapMetadataProxyCatalog().Count,
                    approvedManagedBoundaryCatalogCount = BuildApprovedManagedBoundaryCatalog().Count
                },
                ownerRows = owners.Values.OrderBy(row => row.ownerGlobalObjectId, StringComparer.Ordinal).ToList(),
                countsByRole = countsByRole,
                approvedManagedBoundaryCatalog = BuildApprovedManagedBoundaryCatalog(),
                mapMetadataProxyCatalog = BuildMapMetadataProxyCatalog(),
                notes = new List<string>
                {
                    "No name inference is used.",
                    "Static-manifest migration owners are classified separately from authored building/vehicle placement join owners.",
                    "Authored Map/Buildings and Map/Vehicles placements are usually absent from the static-presentation source set.",
                    "GameplayBuilding still requires managed RuntimeBuildingEntity until GPT ECS cutover.",
                    "GameplayVehicle is already ECS-proven.",
                    "MapMetadataProxy and ApprovedManagedBoundary catalogs are non-owner supplemental records."
                }
            };
        }

        private static void AddGameplayPlacementOwners(
            Dictionary<string, OwnerClassificationRow> owners,
            PlacementJoinSet joins,
            bool isBuilding)
        {
            foreach (KeyValuePair<string, List<PlacementJoin>> pair in joins.bySourceGlobalId)
            {
                if (pair.Value == null || pair.Value.Count == 0)
                    continue;
                GameObject joined = pair.Value[0].resolvedSource;
                if (joined == null)
                    continue;
                if (!owners.TryGetValue(pair.Key, out OwnerClassificationRow row))
                {
                    // Do not treat BuildingDefinitionAuthoring/UnitGridAuthoring as blocking here;
                    // those are expected on gameplay placements and inventoried separately.
                    row = new OwnerClassificationRow
                    {
                        ownerGlobalObjectId = pair.Key,
                        sourceRendererCount = joined.GetComponentsInChildren<Renderer>(true).Length,
                        hasBlockingDependency = false,
                        hasExternalSceneReference = false
                    };
                    owners.Add(pair.Key, row);
                }

                if (isBuilding)
                    row.hasExactBuildingJoin = true;
                else
                    row.hasExactVehicleJoin = true;
                if (pair.Value.Count > 1)
                    row.hasMixedOrAmbiguousSource = true;
            }
        }

        private static OwnerClassificationRow CreateOwnerRow(GameObject owner, string ownerId)
        {
            Transform[] transforms = owner.GetComponentsInChildren<Transform>(true);
            bool blocking = false;
            bool external = false;
            foreach (Transform transform in transforms)
            {
                foreach (Component component in transform.GetComponents<Component>())
                {
                    blocking |= IsBlockingDependency(component);
                    external |= HasExternalSceneReference(owner.transform, component);
                }
            }
            return new OwnerClassificationRow
            {
                ownerGlobalObjectId = ownerId,
                hasBlockingDependency = blocking,
                hasExternalSceneReference = external
            };
        }

        private static string BuildEvidenceNotes(OwnerClassificationRow row)
        {
            var notes = new List<string>();
            if (row.hasExactBuildingJoin) notes.Add("exact-building-join");
            if (row.hasExactVehicleJoin) notes.Add("exact-vehicle-join");
            if (row.hasMixedOrAmbiguousSource) notes.Add("mixed-or-ambiguous-source");
            if (row.hasUnresolvedSource) notes.Add("unresolved-source");
            if (row.hasBlockingDependency) notes.Add("blocking-dependency");
            if (row.hasExternalSceneReference) notes.Add("external-scene-reference");
            if (notes.Count == 0) notes.Add("no-gameplay-join-or-blocking-evidence");
            return string.Join("; ", notes);
        }

        private static bool IsBlockingDependency(Component component)
        {
            if (component == null)
                return true;
            if (component is Transform ||
                component is MeshFilter ||
                component is MeshRenderer ||
                component is SkinnedMeshRenderer ||
                component is LODGroup)
            {
                return false;
            }
            if (component is Collider || component is Collider2D ||
                component is Rigidbody || component is Rigidbody2D)
            {
                return true;
            }
            if (component is Animator animator)
            {
                // Match inventory: controller-free animators are omitted, not blocking.
                return animator.runtimeAnimatorController != null;
            }
            if (component is Light ||
                component is Animation ||
                component is ParticleSystem ||
                component is ParticleSystemRenderer ||
                component is MonoBehaviour)
            {
                return true;
            }
            if (component is Renderer)
                return true;
            return true;
        }

        private static bool HasExternalSceneReference(Transform owner, Component component)
        {
            if (component == null)
                return false;
            SerializedObject serialized;
            try { serialized = new SerializedObject(component); }
            catch { return false; }
            SerializedProperty iterator = serialized.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType != SerializedPropertyType.ObjectReference ||
                    iterator.objectReferenceValue == null || iterator.propertyPath == "m_Script" ||
                    iterator.propertyPath == "m_GameObject")
                    continue;
                Transform target = GetSceneTransform(iterator.objectReferenceValue);
                if (target != null && target != owner && !target.IsChildOf(owner))
                    return true;
            }
            return false;
        }

        private static Transform GetSceneTransform(UnityEngine.Object target)
        {
            if (target is GameObject gameObject && gameObject.scene.IsValid()) return gameObject.transform;
            if (target is Component component && component.gameObject.scene.IsValid()) return component.transform;
            return null;
        }

        private static List<PlacementJoin> ResolveSourceJoins(PlacementJoinSet joins, GameObject source)
        {
            for (Transform current = source != null ? source.transform : null; current != null; current = current.parent)
            {
                if (joins.bySourceGlobalId.TryGetValue(GlobalObjectId.GetGlobalObjectIdSlow(current.gameObject).ToString(),
                        out List<PlacementJoin> matched) && matched.Count > 0)
                    return matched;
            }
            return new List<PlacementJoin>();
        }

        private static PlacementJoinSet BuildBuildingJoins(
            MapBuildingPlacementConfig config, Dictionary<string, List<GameObject>> objectsByNamePath)
        {
            if (config == null || config.Placements == null)
                throw new InvalidOperationException("Building placement config is missing.");
            var joins = new PlacementJoinSet();
            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapBuildingPlacementConfigEntry placement = config.Placements[i];
                AddJoin(joins, placement.SourcePath, placement.WorldPosition, placement.WorldEulerAngles,
                    placement.WorldScale, objectsByNamePath);
            }
            return joins;
        }

        private static PlacementJoinSet BuildVehicleJoins(
            MapVehiclePlacementConfig config, Dictionary<string, List<GameObject>> objectsByNamePath)
        {
            if (config == null || config.Placements == null)
                throw new InvalidOperationException("Vehicle placement config is missing.");
            var joins = new PlacementJoinSet();
            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapVehiclePlacementConfigEntry placement = config.Placements[i];
                AddJoin(joins, placement.SourcePath, placement.WorldPosition, placement.WorldEulerAngles,
                    placement.WorldScale, objectsByNamePath);
            }
            return joins;
        }

        private static void AddJoin(PlacementJoinSet set, string sourcePath, Vector3 position,
            Vector3 eulerAngles, Vector3 scale, Dictionary<string, List<GameObject>> objectsByNamePath)
        {
            objectsByNamePath.TryGetValue(sourcePath ?? string.Empty, out List<GameObject> pathMatches);
            pathMatches ??= new List<GameObject>();
            List<GameObject> matches = pathMatches.Count <= 1 ? pathMatches : pathMatches.Where(candidate =>
                (candidate.transform.position - position).sqrMagnitude <= 0.000001f &&
                (candidate.transform.lossyScale - scale).sqrMagnitude <= 0.000001f &&
                Quaternion.Angle(candidate.transform.rotation, Quaternion.Euler(eulerAngles)) <= 0.001f).ToList();
            if (matches.Count != 1)
                return;
            string id = GlobalObjectId.GetGlobalObjectIdSlow(matches[0]).ToString();
            if (!set.bySourceGlobalId.TryGetValue(id, out List<PlacementJoin> joins))
            {
                joins = new List<PlacementJoin>();
                set.bySourceGlobalId.Add(id, joins);
            }
            joins.Add(new PlacementJoin { resolvedSource = matches[0] });
        }

        private static GameObject ResolveMigrationOwner(
            GameObject sourceObject, Transform mapRoot, HashSet<Transform> sourceTransforms)
        {
            GameObject owner = sourceObject;
            for (Transform current = sourceObject.transform; current != null && current != mapRoot; current = current.parent)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(current.gameObject) || sourceTransforms.Contains(current))
                    owner = current.gameObject;
            }
            return owner;
        }

        private static Dictionary<string, Renderer> IndexSceneRenderersByGlobalId(Scene scene)
        {
            var index = new Dictionary<string, Renderer>(StringComparer.Ordinal);
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                index.TryAdd(GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString(), renderer);
            return index;
        }

        private static Dictionary<string, List<GameObject>> IndexSceneObjectsByNamePath(Scene scene)
        {
            var index = new Dictionary<string, List<GameObject>>(StringComparer.Ordinal);
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                string path = BuildNameHierarchyPath(transform);
                if (!index.TryGetValue(path, out List<GameObject> objects))
                    index.Add(path, objects = new List<GameObject>());
                objects.Add(transform.gameObject);
            }
            return index;
        }

        private static string BuildNameHierarchyPath(Transform transform)
        {
            var parts = new Stack<string>();
            for (Transform current = transform; current != null; current = current.parent) parts.Push(current.name);
            return string.Join("/", parts);
        }

        private static OperationMapSceneView RequireSingleOperationMapSceneView(Scene scene)
        {
            OperationMapSceneView[] views = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OperationMapSceneView>(true)).ToArray();
            if (views.Length != 1)
                throw new InvalidOperationException($"Expected exactly one OperationMapSceneView in {scene.path}.");
            return views[0];
        }

        private static Scene OpenSceneForInspection(string path) =>
            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        private static void RequireCleanLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException($"Loaded scene is dirty: {SceneManager.GetSceneAt(i).path}");
        }

        private static void RestoreSceneSetup(SceneSetup[] setup)
        {
            if (setup == null || setup.Length == 0)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        private static void PublishJsonAtomically(string outputPath, string json)
        {
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            string temporaryPath = outputPath + ".tmp";
            File.WriteAllText(temporaryPath, json, Utf8WithoutBom);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            File.Move(temporaryPath, outputPath);
        }

        private static string RequireProjectRoot() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static OwnerClassificationSummaryReport BuildSummary(OwnerClassificationReport report) => new()
        {
            reportSchema = report.reportSchema,
            reportSchemaVersion = report.reportSchemaVersion,
            result = report.result,
            counts = report.counts,
            countsByRole = report.countsByRole
        };

        private sealed class PlacementJoinSet
        {
            public readonly Dictionary<string, List<PlacementJoin>> bySourceGlobalId = new(StringComparer.Ordinal);
        }

        private sealed class PlacementJoin
        {
            public GameObject resolvedSource;
        }

        [Serializable] public sealed class CatalogEntry
        {
            public string proxyId;
            public string description;
            public string proposedOwner;
            public bool isVisualMigrationOwner;
            public bool isApprovedTransientBoundary;
        }

        [Serializable] public sealed class OwnerClassificationReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public string reportPath;
            public string projectRootMarker;
            public string canonicalMapScenePath;
            public string manifestPath;
            public OwnerClassificationCounts counts;
            public List<OwnerClassificationRow> ownerRows;
            public List<RoleCount> countsByRole;
            public List<CatalogEntry> approvedManagedBoundaryCatalog;
            public List<CatalogEntry> mapMetadataProxyCatalog;
            public List<string> notes;
        }

        [Serializable] public sealed class OwnerClassificationSummaryReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public OwnerClassificationCounts counts;
            public List<RoleCount> countsByRole;
        }

        [Serializable] public sealed class OwnerClassificationCounts
        {
            public int migrationOwnerCount;
            public int rejectedUnresolvedCount;
            public int unresolvedManifestSourceCount;
            public int gameplayBuildingCount;
            public int gameplayVehicleCount;
            public int renderOnlyEntityCount;
            public int mapMetadataProxyCatalogCount;
            public int approvedManagedBoundaryCatalogCount;
        }

        [Serializable] public sealed class OwnerClassificationRow
        {
            public string ownerGlobalObjectId;
            public int sourceRendererCount;
            public string ownerRole;
            public bool requiresApprovedManagedBoundaryUntilEcsCutover;
            public bool hasExactBuildingJoin;
            public bool hasExactVehicleJoin;
            public bool hasMixedOrAmbiguousSource;
            public bool hasUnresolvedSource;
            public bool hasBlockingDependency;
            public bool hasExternalSceneReference;
            public string evidenceNotes;
        }

        [Serializable] public sealed class RoleCount
        {
            public string ownerRole;
            public int count;
        }
    }
}

#endif
