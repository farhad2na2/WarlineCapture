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
    /// Read-only Phase 0A inventory of current static-presentation sources and placement joins.
    /// Does not mutate scenes, manifests, Addressables, or accepted ownership.
    /// </summary>
    public static class OperationMapEntityPresentationMigrationInventoryProbe
    {
        internal const string ReportSchema = "warline.operation-map.entity-presentation-migration-inventory";
        internal const int ReportSchemaVersion = 1;
        internal const string ReportPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_ENTITY_PRESENTATION_MIGRATION_INVENTORY_REPORT_PATH";
        internal const string DefaultReportPath =
            "/private/tmp/warline-operation-map-entity-presentation-migration-inventory.json";
        internal const string SummaryPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_ENTITY_PRESENTATION_MIGRATION_INVENTORY_SUMMARY_PATH";
        internal const string DefaultSummaryPath =
            "/private/tmp/warline-operation-map-entity-presentation-migration-inventory-summary.json";

        private const string CanonicalMapScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
        private const string ManifestPath =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationManifest.asset";

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        private static readonly string[] ProtectedRootNames =
        {
            "AuthoredCityOverrides",
            "Buildings",
            "DenseCity_GradingArchive",
            "Mountains",
            "ResourceAreas",
            "Roads",
            "Runways",
            "Vehicles"
        };

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
            InventoryReport report;
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
                $"[OperationMapEntityPresentationMigrationInventoryProbe] result={report.result} " +
                $"sources={report.counts.sourceCount} chunks={report.counts.chunkCount} " +
                $"buildings={report.counts.buildingPlacementCount} vehicles={report.counts.vehiclePlacementCount} " +
                $"unresolved={report.counts.unresolvedCount} report={outputPath} summary={summaryPath}");
        }

        internal static bool HasRequiredReportShape(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            InventoryReport report;
            try
            {
                report = JsonUtility.FromJson<InventoryReport>(json);
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
                   report.counts.sourceCount >= 0 &&
                   report.counts.chunkCount >= 0 &&
                   report.counts.buildingPlacementCount >= 0 &&
                   report.counts.vehiclePlacementCount >= 0 &&
                   report.protectedRoots != null &&
                   report.sources != null &&
                   report.owners != null &&
                   report.placementJoins != null &&
                   report.classificationCounts != null;
        }

        internal static string ClassifyWithoutNameInference(
            bool buildingJoinExact,
            bool vehicleJoinExact,
            bool underProtectedRoot,
            int buildingJoinCount,
            int vehicleJoinCount,
            bool ownerRenderOnlyCandidate = false)
        {
            if (buildingJoinCount > 1 || vehicleJoinCount > 1 || (buildingJoinExact && vehicleJoinExact))
                return "MixedOrAmbiguous";
            if (buildingJoinExact)
                return "GameplayBuildingCandidate";
            if (vehicleJoinExact)
                return "GameplayVehicleCandidate";
            if (underProtectedRoot)
                return "ProtectedAuthoredCandidate";
            if (ownerRenderOnlyCandidate)
                return "StaticRenderOnlyCandidate";
            return "UnresolvedPendingReview";
        }

        private static InventoryReport BuildReport(string projectRoot, string outputPath)
        {
            StaticMapPresentationManifest manifest =
                AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(ManifestPath);
            if (manifest == null)
                throw new InvalidOperationException($"Missing manifest: {ManifestPath}");

            Scene authoringScene = OpenSceneForInspection(CanonicalMapScenePath);
            OperationMapSceneView mapView = RequireSingleOperationMapSceneView(authoringScene);
            Dictionary<string, Renderer> renderersByGlobalId =
                IndexSceneRenderersByGlobalId(authoringScene);
            HashSet<Transform> sourceTransforms = new(
                manifest.Sources
                    .Select(source =>
                        renderersByGlobalId.TryGetValue(
                            source.SourceGlobalObjectId ?? string.Empty,
                            out Renderer renderer)
                            ? renderer.transform
                            : null)
                    .Where(transform => transform != null));
            Dictionary<string, List<GameObject>> objectsByNamePath = IndexSceneObjectsByNamePath(authoringScene);
            List<ProtectedRootReport> protectedRoots = BuildProtectedRootReports(authoringScene);
            HashSet<string> protectedGlobalIds = new(
                protectedRoots
                    .Where(root => root.present && !string.IsNullOrWhiteSpace(root.globalObjectId))
                    .Select(root => root.globalObjectId),
                StringComparer.Ordinal);

            PlacementJoinSet buildingJoins = BuildBuildingJoins(
                mapView.BuildingPlacements,
                objectsByNamePath);
            PlacementJoinSet vehicleJoins = BuildVehicleJoins(
                mapView.VehiclePlacements,
                objectsByNamePath);

            var sources = new List<SourceInventoryReport>(manifest.Sources.Count);
            var ownersByGlobalId = new Dictionary<string, OwnerInventoryReport>(StringComparer.Ordinal);
            var classificationCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            int unresolved = 0;
            int mixed = 0;
            int buildingCandidates = 0;
            int vehicleCandidates = 0;
            int protectedCandidates = 0;
            int staticRenderOnlyCandidates = 0;
            int unresolvedSourceObjects = 0;

            for (int i = 0; i < manifest.Sources.Count; i++)
            {
                StaticMapPresentationSourceEntry source = manifest.Sources[i];
                string sourceGlobalId = source.SourceGlobalObjectId ?? string.Empty;
                renderersByGlobalId.TryGetValue(sourceGlobalId, out Renderer sourceRenderer);
                GameObject sourceObject = sourceRenderer != null ? sourceRenderer.gameObject : null;
                if (sourceObject == null)
                    unresolvedSourceObjects++;
                string namePath = sourceObject == null
                    ? string.Empty
                    : BuildNameHierarchyPath(sourceObject.transform);
                bool underProtected = IsUnderProtectedRoot(sourceObject, protectedGlobalIds);
                OwnerInventoryReport owner = sourceObject == null
                    ? null
                    : GetOrCreateOwnerReport(
                        ownersByGlobalId,
                        ResolveMigrationOwner(sourceObject, mapView.MapRoot, sourceTransforms));
                if (owner != null)
                    owner.sourceRendererCount++;

                List<PlacementJoinReport> buildingMatches = ResolveSourceJoins(
                    buildingJoins,
                    sourceObject);
                List<PlacementJoinReport> vehicleMatches = ResolveSourceJoins(
                    vehicleJoins,
                    sourceObject);

                string classification = ClassifyWithoutNameInference(
                    buildingJoinExact: buildingMatches.Count == 1,
                    vehicleJoinExact: vehicleMatches.Count == 1,
                    underProtectedRoot: underProtected,
                    buildingJoinCount: buildingMatches.Count,
                    vehicleJoinCount: vehicleMatches.Count,
                    ownerRenderOnlyCandidate:
                        owner != null &&
                        string.Equals(
                            owner.candidateDisposition,
                            "RenderOnlyEntityCandidate",
                            StringComparison.Ordinal));

                classificationCounts.TryGetValue(classification, out int count);
                classificationCounts[classification] = count + 1;
                switch (classification)
                {
                    case "GameplayBuildingCandidate":
                        buildingCandidates++;
                        break;
                    case "GameplayVehicleCandidate":
                        vehicleCandidates++;
                        break;
                    case "ProtectedAuthoredCandidate":
                        protectedCandidates++;
                        break;
                    case "StaticRenderOnlyCandidate":
                        staticRenderOnlyCandidates++;
                        break;
                    case "MixedOrAmbiguous":
                        mixed++;
                        break;
                    default:
                        unresolved++;
                        break;
                }

                string prefabPath = sourceObject == null
                    ? string.Empty
                    : PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sourceObject);
                string prefabGuid = string.IsNullOrWhiteSpace(prefabPath)
                    ? string.Empty
                    : AssetDatabase.AssetPathToGUID(prefabPath);
                long prefabLocalId = 0;
                GameObject correspondingPrefabObject = sourceObject == null
                    ? null
                    : PrefabUtility.GetCorrespondingObjectFromSource(sourceObject);
                if (correspondingPrefabObject != null &&
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        correspondingPrefabObject,
                        out string correspondingPrefabGuid,
                        out long correspondingPrefabLocalId))
                {
                    prefabGuid = correspondingPrefabGuid;
                    prefabLocalId = correspondingPrefabLocalId;
                }

                sources.Add(new SourceInventoryReport
                {
                    sourceIndex = i,
                    sourceGlobalObjectId = sourceGlobalId,
                    sourceHierarchyPath = source.SourceHierarchyPath ?? string.Empty,
                    sourceNameHierarchyPath = namePath,
                    sourceDependencyHash = source.SourceDependencyHash ?? string.Empty,
                    chunkId = source.ChunkId ?? string.Empty,
                    generatedObjectName = source.GeneratedObjectName ?? string.Empty,
                    meshAssetGuid = source.MeshAssetGuid ?? string.Empty,
                    meshLocalId = source.MeshLocalId,
                    overlaySource = source.OverlaySource,
                    sourceObjectResolved = sourceObject != null,
                    sourceRendererType =
                        sourceRenderer != null ? sourceRenderer.GetType().FullName : string.Empty,
                    sourceRendererEnabled = sourceRenderer != null && sourceRenderer.enabled,
                    sourceActiveInHierarchy = sourceObject != null && sourceObject.activeInHierarchy,
                    worldPosition = sourceObject != null
                        ? sourceObject.transform.position
                        : Vector3.zero,
                    worldRotation = sourceObject != null
                        ? sourceObject.transform.rotation
                        : Quaternion.identity,
                    worldScale = sourceObject != null
                        ? sourceObject.transform.lossyScale
                        : Vector3.zero,
                    worldBoundsCenter = source.WorldBounds.center,
                    worldBoundsSize = source.WorldBounds.size,
                    underProtectedRoot = underProtected,
                    prefabAssetPath = prefabPath ?? string.Empty,
                    prefabAssetGuid = prefabGuid ?? string.Empty,
                    prefabLocalId = prefabLocalId,
                    migrationOwnerGlobalObjectId = owner?.globalObjectId ?? string.Empty,
                    classification = classification,
                    buildingJoinCount = buildingMatches.Count,
                    vehicleJoinCount = vehicleMatches.Count,
                    componentTypes = sourceObject == null
                        ? new List<string>()
                        : sourceObject.GetComponents<Component>()
                            .Select(component =>
                                component != null ? component.GetType().FullName : "<missing>")
                            .OrderBy(type => type, StringComparer.Ordinal)
                            .ToList(),
                    materialGuids = source.Materials == null
                        ? new List<string>()
                        : source.Materials
                            .Select(material => material?.AssetGuid ?? string.Empty)
                            .Where(guid => !string.IsNullOrWhiteSpace(guid))
                            .Distinct(StringComparer.Ordinal)
                            .OrderBy(guid => guid, StringComparer.Ordinal)
                            .ToList()
                });
            }

            sources.Sort((left, right) =>
            {
                int byPath = string.CompareOrdinal(left.sourceHierarchyPath, right.sourceHierarchyPath);
                return byPath != 0 ? byPath : left.sourceIndex.CompareTo(right.sourceIndex);
            });

            List<ClassificationCountReport> orderedCounts = classificationCounts
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new ClassificationCountReport
                {
                    classification = entry.Key,
                    count = entry.Value
                })
                .ToList();

            List<PlacementJoinReport> allJoins = buildingJoins.All
                .Concat(vehicleJoins.All)
                .OrderBy(join => join.kind, StringComparer.Ordinal)
                .ThenBy(join => join.sourcePath, StringComparer.Ordinal)
                .ThenBy(join => join.placementIndex)
                .ToList();

            string result = mixed == 0 &&
                            unresolvedSourceObjects == 0 &&
                            buildingJoins.UnresolvedPlacementCount == 0 &&
                            vehicleJoins.UnresolvedPlacementCount == 0 &&
                            buildingJoins.ReusedSourceObjectCount == 0 &&
                            vehicleJoins.ReusedSourceObjectCount == 0
                ? "InventoryCompletePendingReview"
                : "InventoryCompleteWithAmbiguities";

            return new InventoryReport
            {
                reportSchema = ReportSchema,
                reportSchemaVersion = ReportSchemaVersion,
                result = result,
                reportPath = outputPath,
                projectRootMarker = Path.GetFileName(projectRoot.TrimEnd(Path.DirectorySeparatorChar)),
                historicalBaselineLabels = new List<string>
                {
                    "16542_historical_pre_clear_sources",
                    "514_historical_pre_clear_chunks",
                    "451_historical_building_placements",
                    "29_historical_vehicle_placements"
                },
                counts = new InventoryCountsReport
                {
                    sourceCount = manifest.Sources.Count,
                    chunkCount = manifest.Chunks.Count,
                    unresolvedSourceObjectCount = unresolvedSourceObjects,
                    migrationOwnerCount = ownersByGlobalId.Count,
                    ownersRequiringDependencyReviewCount =
                        ownersByGlobalId.Values.Count(owner =>
                            string.Equals(
                                owner.candidateDisposition,
                                "RequiresExplicitDependencyReview",
                                StringComparison.Ordinal)),
                    blockingDependencyCount =
                        ownersByGlobalId.Values.Sum(owner => owner.blockingDependencyCount),
                    externalSceneReferenceCount =
                        ownersByGlobalId.Values.Sum(owner => owner.externalSceneReferenceCount),
                    buildingPlacementCount = buildingJoins.All.Count,
                    vehiclePlacementCount = vehicleJoins.All.Count,
                    unresolvedCount = unresolved,
                    mixedOrAmbiguousCount = mixed,
                    gameplayBuildingCandidateCount = buildingCandidates,
                    gameplayVehicleCandidateCount = vehicleCandidates,
                    protectedAuthoredCandidateCount = protectedCandidates,
                    staticRenderOnlyCandidateCount = staticRenderOnlyCandidates,
                    unresolvedBuildingPlacementCount = buildingJoins.UnresolvedPlacementCount,
                    unresolvedVehiclePlacementCount = vehicleJoins.UnresolvedPlacementCount,
                    reusedBuildingSourceObjectCount = buildingJoins.ReusedSourceObjectCount,
                    reusedVehicleSourceObjectCount = vehicleJoins.ReusedSourceObjectCount
                },
                manifest = new ManifestIdentityReport
                {
                    path = ManifestPath,
                    operationMapId = manifest.OperationMapId,
                    contentHash = manifest.ContentHash,
                    canonicalScenePath = manifest.CanonicalScenePath,
                    canonicalSceneGuid = manifest.CanonicalSceneGuid,
                    canonicalSceneDependencyHash = manifest.CanonicalSceneDependencyHash
                },
                protectedRoots = protectedRoots
                    .OrderBy(root => root.hierarchyPath, StringComparer.Ordinal)
                    .ToList(),
                classificationCounts = orderedCounts,
                owners = ownersByGlobalId.Values
                    .OrderBy(owner => owner.hierarchyPath, StringComparer.Ordinal)
                    .ToList(),
                placementJoins = allJoins,
                sources = sources
            };
        }

        private static List<PlacementJoinReport> ResolveSourceJoins(
            PlacementJoinSet joins,
            GameObject sourceObject)
        {
            for (Transform current = sourceObject != null ? sourceObject.transform : null;
                 current != null;
                 current = current.parent)
            {
                string globalId = GlobalObjectId.GetGlobalObjectIdSlow(current.gameObject).ToString();
                if (joins.BySourceGlobalId.TryGetValue(
                        globalId,
                        out List<PlacementJoinReport> byAncestorId) &&
                    byAncestorId.Count > 0)
                {
                    return byAncestorId;
                }
            }

            return new List<PlacementJoinReport>();
        }

        private static InventorySummaryReport BuildSummary(InventoryReport report)
        {
            return new InventorySummaryReport
            {
                reportSchema = report.reportSchema,
                reportSchemaVersion = report.reportSchemaVersion,
                result = report.result,
                counts = report.counts,
                manifest = report.manifest,
                protectedRoots = report.protectedRoots,
                classificationCounts = report.classificationCounts,
                unresolvedBuildingPlacementCount = report.counts.unresolvedBuildingPlacementCount,
                unresolvedVehiclePlacementCount = report.counts.unresolvedVehiclePlacementCount,
                nextOwner = "Owner approvals, then Grok low-risk candidate scaffolding",
                nextBlockedActions = new List<string>
                {
                    "Do not mutate accepted authoring/static package ownership before owner approvals.",
                    "Capture the missing Android Phase 0 baseline before production cutover acceptance.",
                    "Grok may add non-mutating migration-record and dry-run transaction scaffolding.",
                    "Return the first ownership mutation and candidate parity review to GPT-5.6."
                }
            };
        }

        private static PlacementJoinSet BuildBuildingJoins(
            MapBuildingPlacementConfig config,
            Dictionary<string, List<GameObject>> objectsByNamePath)
        {
            if (config == null || config.Placements == null)
                throw new InvalidOperationException("Building placement config is missing.");

            var set = new PlacementJoinSet();
            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapBuildingPlacementConfigEntry placement = config.Placements[i];
                AddJoin(
                    set,
                    "Building",
                    i,
                    placement.SourcePath,
                    placement.WorldPosition,
                    placement.WorldEulerAngles,
                    placement.WorldScale,
                    objectsByNamePath);
            }
            FinalizePlacementReuse(set);
            return set;
        }

        private static PlacementJoinSet BuildVehicleJoins(
            MapVehiclePlacementConfig config,
            Dictionary<string, List<GameObject>> objectsByNamePath)
        {
            if (config == null || config.Placements == null)
                throw new InvalidOperationException("Vehicle placement config is missing.");

            var set = new PlacementJoinSet();
            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapVehiclePlacementConfigEntry placement = config.Placements[i];
                AddJoin(
                    set,
                    "Vehicle",
                    i,
                    placement.SourcePath,
                    placement.WorldPosition,
                    placement.WorldEulerAngles,
                    placement.WorldScale,
                    objectsByNamePath);
            }
            FinalizePlacementReuse(set);
            return set;
        }

        private static void FinalizePlacementReuse(PlacementJoinSet set)
        {
            foreach (List<PlacementJoinReport> joins in set.BySourceGlobalId.Values)
            {
                if (joins.Count <= 1)
                    continue;

                set.ReusedSourceObjectCount++;
                for (int i = 0; i < joins.Count; i++)
                    joins[i].resolveState = "Reused";
            }
        }

        private static void AddJoin(
            PlacementJoinSet set,
            string kind,
            int index,
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
            string resolvedGlobalId = string.Empty;
            string resolveState;
            string resolutionMethod;
            if (matches.Count == 1)
            {
                resolveState = "Exact";
                resolutionMethod = pathMatches.Count == 1
                    ? "UniqueHierarchyPath"
                    : "UniqueHierarchyPathAndTransformTuple";
                resolvedGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(matches[0]).ToString();
            }
            else if (pathMatches.Count == 0)
            {
                resolveState = "Unresolved";
                resolutionMethod = "HierarchyPathMissing";
                set.UnresolvedPlacementCount++;
            }
            else
            {
                resolveState = "Ambiguous";
                resolutionMethod = transformMatches.Count == 0
                    ? "NoTransformTupleMatchAmongPathCandidates"
                    : "MultipleTransformTupleMatches";
                set.UnresolvedPlacementCount++;
            }

            var join = new PlacementJoinReport
            {
                kind = kind,
                placementIndex = index,
                sourcePath = path,
                resolveState = resolveState,
                resolutionMethod = resolutionMethod,
                scenePathMatchCount = pathMatches.Count,
                transformTupleMatchCount = transformMatches.Count,
                resolvedSourceGlobalObjectId = resolvedGlobalId
            };
            set.All.Add(join);

            if (!set.BySourcePath.TryGetValue(path, out List<PlacementJoinReport> byPath))
            {
                byPath = new List<PlacementJoinReport>();
                set.BySourcePath[path] = byPath;
            }

            byPath.Add(join);

            if (!string.IsNullOrWhiteSpace(resolvedGlobalId))
            {
                if (!set.BySourceGlobalId.TryGetValue(resolvedGlobalId, out List<PlacementJoinReport> byId))
                {
                    byId = new List<PlacementJoinReport>();
                    set.BySourceGlobalId[resolvedGlobalId] = byId;
                }

                byId.Add(join);
            }
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

        private static GameObject ResolveMigrationOwner(
            GameObject sourceObject,
            Transform mapRoot,
            HashSet<Transform> sourceTransforms)
        {
            GameObject owner = sourceObject;
            for (Transform current = sourceObject.transform;
                 current != null && current != mapRoot;
                 current = current.parent)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(current.gameObject) ||
                    sourceTransforms.Contains(current))
                {
                    owner = current.gameObject;
                }
            }

            return owner;
        }

        private static OwnerInventoryReport GetOrCreateOwnerReport(
            Dictionary<string, OwnerInventoryReport> ownersByGlobalId,
            GameObject owner)
        {
            string ownerGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(owner).ToString();
            if (ownersByGlobalId.TryGetValue(ownerGlobalId, out OwnerInventoryReport existing))
                return existing;

            var componentCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var dispositionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var externalReferences = new List<CrossObjectReferenceReport>();
            int blockingDependencyCount = 0;
            Transform[] transforms = owner.GetComponentsInChildren<Transform>(true);
            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Component[] components = transforms[transformIndex].GetComponents<Component>();
                for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    Component component = components[componentIndex];
                    string typeName = component != null
                        ? component.GetType().FullName
                        : "<missing>";
                    string disposition = GetDependencyDisposition(component);
                    componentCounts.TryGetValue(typeName, out int componentCount);
                    componentCounts[typeName] = componentCount + 1;
                    dispositionCounts.TryGetValue(disposition, out int dispositionCount);
                    dispositionCounts[disposition] = dispositionCount + 1;
                    if (IsBlockingDisposition(disposition))
                        blockingDependencyCount++;
                    if (component != null)
                        AppendExternalSceneReferences(owner.transform, component, externalReferences);
                }
            }

            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(owner);
            string prefabGuid = string.IsNullOrWhiteSpace(prefabPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(prefabPath);
            long prefabLocalId = 0;
            GameObject correspondingPrefabObject =
                PrefabUtility.GetCorrespondingObjectFromSource(owner);
            if (correspondingPrefabObject != null &&
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    correspondingPrefabObject,
                    out string correspondingPrefabGuid,
                    out long correspondingPrefabLocalId))
            {
                prefabGuid = correspondingPrefabGuid;
                prefabLocalId = correspondingPrefabLocalId;
            }

            var report = new OwnerInventoryReport
            {
                globalObjectId = ownerGlobalId,
                hierarchyPath = BuildIndexedHierarchyPath(owner.transform),
                nameHierarchyPath = BuildNameHierarchyPath(owner.transform),
                prefabAssetPath = prefabPath ?? string.Empty,
                prefabAssetGuid = prefabGuid,
                prefabLocalId = prefabLocalId,
                worldPosition = owner.transform.position,
                worldRotation = owner.transform.rotation,
                worldScale = owner.transform.lossyScale,
                sourceRendererCount = 0,
                hierarchyObjectCount = transforms.Length,
                blockingDependencyCount = blockingDependencyCount,
                externalSceneReferenceCount = externalReferences.Count,
                candidateDisposition = blockingDependencyCount == 0 && externalReferences.Count == 0
                    ? "RenderOnlyEntityCandidate"
                    : "RequiresExplicitDependencyReview",
                componentTypes = componentCounts
                    .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                    .Select(entry => new DependencyTypeCountReport
                    {
                        type = entry.Key,
                        count = entry.Value
                    })
                    .ToList(),
                dispositionCounts = dispositionCounts
                    .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                    .Select(entry => new DependencyTypeCountReport
                    {
                        type = entry.Key,
                        count = entry.Value
                    })
                    .ToList(),
                externalSceneReferences = externalReferences
                    .OrderBy(reference => reference.componentGlobalObjectId, StringComparer.Ordinal)
                    .ThenBy(reference => reference.propertyPath, StringComparer.Ordinal)
                    .ToList()
            };
            ownersByGlobalId.Add(ownerGlobalId, report);
            return report;
        }

        private static string GetDependencyDisposition(Component component)
        {
            if (component == null)
                return "RejectMissingScriptOrComponent";
            if (component is Transform)
                return "BakeEntityTransform";
            if (component is MeshFilter || component is MeshRenderer || component is SkinnedMeshRenderer)
                return "BakeEntitiesGraphics";
            if (component is LODGroup)
                return "BakeEntitiesGraphicsLod";
            if (component is Collider || component is Collider2D ||
                component is Rigidbody || component is Rigidbody2D)
            {
                return "RejectProhibitedPhysics";
            }
            if (component is Light)
                return "ReviewBakedLightingDisposition";
            if (component is Animator animator)
            {
                return animator.runtimeAnimatorController == null
                    ? "OmitInertAnimatorWithoutController"
                    : "ReviewAnimationDisposition";
            }
            if (component is Animation)
                return "ReviewAnimationDisposition";
            if (component is ParticleSystem || component is ParticleSystemRenderer)
                return "ReviewVfxDisposition";
            if (component is MonoBehaviour)
                return "ReviewManagedScriptDisposition";
            if (component is Renderer)
                return "ReviewRendererDisposition";
            return "ReviewExplicitComponentDisposition";
        }

        private static bool IsBlockingDisposition(string disposition)
        {
            return disposition.StartsWith("Reject", StringComparison.Ordinal) ||
                   disposition.StartsWith("Review", StringComparison.Ordinal);
        }

        private static void AppendExternalSceneReferences(
            Transform owner,
            Component component,
            List<CrossObjectReferenceReport> output)
        {
            SerializedObject serialized;
            try
            {
                serialized = new SerializedObject(component);
            }
            catch
            {
                return;
            }

            SerializedProperty iterator = serialized.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType != SerializedPropertyType.ObjectReference ||
                    iterator.objectReferenceValue == null ||
                    string.Equals(iterator.propertyPath, "m_Script", StringComparison.Ordinal) ||
                    string.Equals(iterator.propertyPath, "m_GameObject", StringComparison.Ordinal))
                {
                    continue;
                }

                Transform targetTransform = GetSceneTransform(iterator.objectReferenceValue);
                if (targetTransform == null ||
                    targetTransform == owner ||
                    targetTransform.IsChildOf(owner))
                {
                    continue;
                }

                output.Add(new CrossObjectReferenceReport
                {
                    componentGlobalObjectId =
                        GlobalObjectId.GetGlobalObjectIdSlow(component).ToString(),
                    componentType = component.GetType().FullName,
                    propertyPath = iterator.propertyPath,
                    targetGlobalObjectId =
                        GlobalObjectId.GetGlobalObjectIdSlow(iterator.objectReferenceValue).ToString(),
                    targetHierarchyPath = BuildIndexedHierarchyPath(targetTransform)
                });
            }
        }

        private static Transform GetSceneTransform(UnityEngine.Object target)
        {
            switch (target)
            {
                case GameObject gameObject when gameObject.scene.IsValid():
                    return gameObject.transform;
                case Component component when component.gameObject.scene.IsValid():
                    return component.transform;
                default:
                    return null;
            }
        }

        private static List<ProtectedRootReport> BuildProtectedRootReports(Scene scene)
        {
            HashSet<string> names = new(ProtectedRootNames, StringComparer.Ordinal);
            var reports = new List<ProtectedRootReport>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!names.Contains(transform.name))
                        continue;

                    reports.Add(new ProtectedRootReport
                    {
                        name = transform.name,
                        hierarchyPath = BuildIndexedHierarchyPath(transform),
                        nameHierarchyPath = BuildNameHierarchyPath(transform),
                        globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(transform.gameObject).ToString(),
                        present = true
                    });
                }
            }

            if (!reports.Any(report =>
                    string.Equals(report.name, "AuthoredCityOverrides", StringComparison.Ordinal)))
            {
                reports.Add(new ProtectedRootReport
                {
                    name = "AuthoredCityOverrides",
                    hierarchyPath = string.Empty,
                    nameHierarchyPath = string.Empty,
                    globalObjectId = string.Empty,
                    present = false
                });
            }

            return reports;
        }

        private static bool IsUnderProtectedRoot(GameObject sourceObject, HashSet<string> protectedGlobalIds)
        {
            if (sourceObject == null || protectedGlobalIds.Count == 0)
                return false;

            for (Transform current = sourceObject.transform; current != null; current = current.parent)
            {
                string id = GlobalObjectId.GetGlobalObjectIdSlow(current.gameObject).ToString();
                if (protectedGlobalIds.Contains(id))
                    return true;
            }

            return false;
        }

        private static Dictionary<string, Renderer> IndexSceneRenderersByGlobalId(Scene scene)
        {
            var map = new Dictionary<string, Renderer>(StringComparer.Ordinal);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    string id = GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString();
                    if (!map.ContainsKey(id))
                        map.Add(id, renderer);
                }
            }

            return map;
        }

        private static Dictionary<string, List<GameObject>> IndexSceneObjectsByNamePath(Scene scene)
        {
            var map = new Dictionary<string, List<GameObject>>(StringComparer.Ordinal);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    string path = BuildNameHierarchyPath(transform);
                    if (!map.TryGetValue(path, out List<GameObject> list))
                    {
                        list = new List<GameObject>();
                        map[path] = list;
                    }

                    list.Add(transform.gameObject);
                }
            }

            return map;
        }

        private static string BuildIndexedHierarchyPath(Transform transform)
        {
            var parts = new Stack<string>();
            for (Transform current = transform; current != null; current = current.parent)
                parts.Push($"{current.name}[{current.GetSiblingIndex()}]");
            return string.Join("/", parts);
        }

        private static string BuildNameHierarchyPath(Transform transform)
        {
            var parts = new Stack<string>();
            for (Transform current = transform; current != null; current = current.parent)
                parts.Push(current.name);
            return string.Join("/", parts);
        }

        private static OperationMapSceneView RequireSingleOperationMapSceneView(Scene scene)
        {
            OperationMapSceneView found = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                OperationMapSceneView[] views = root.GetComponentsInChildren<OperationMapSceneView>(true);
                for (int i = 0; i < views.Length; i++)
                {
                    if (found != null)
                        throw new InvalidOperationException("Expected exactly one OperationMapSceneView.");
                    found = views[i];
                }
            }

            return found ?? throw new InvalidOperationException("OperationMapSceneView is missing.");
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
                    throw new InvalidOperationException($"Loaded scene is dirty: {scene.path}");
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
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            string temporaryPath = outputPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(temporaryPath, json, Utf8WithoutBom);
                File.Move(temporaryPath, outputPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static string RequireProjectRoot()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Unable to resolve the Unity project root.");
            return Path.GetFullPath(root);
        }

        private sealed class PlacementJoinSet
        {
            public List<PlacementJoinReport> All { get; } = new();
            public Dictionary<string, List<PlacementJoinReport>> BySourcePath { get; } =
                new(StringComparer.Ordinal);
            public Dictionary<string, List<PlacementJoinReport>> BySourceGlobalId { get; } =
                new(StringComparer.Ordinal);
            public int UnresolvedPlacementCount;
            public int ReusedSourceObjectCount;
        }

        [Serializable]
        public sealed class InventoryReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public string reportPath;
            public string projectRootMarker;
            public List<string> historicalBaselineLabels;
            public InventoryCountsReport counts;
            public ManifestIdentityReport manifest;
            public List<ProtectedRootReport> protectedRoots;
            public List<ClassificationCountReport> classificationCounts;
            public List<OwnerInventoryReport> owners;
            public List<PlacementJoinReport> placementJoins;
            public List<SourceInventoryReport> sources;
        }

        [Serializable]
        public sealed class InventorySummaryReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public InventoryCountsReport counts;
            public ManifestIdentityReport manifest;
            public List<ProtectedRootReport> protectedRoots;
            public List<ClassificationCountReport> classificationCounts;
            public int unresolvedBuildingPlacementCount;
            public int unresolvedVehiclePlacementCount;
            public string nextOwner;
            public List<string> nextBlockedActions;
        }

        [Serializable]
        public sealed class InventoryCountsReport
        {
            public int sourceCount;
            public int chunkCount;
            public int unresolvedSourceObjectCount;
            public int migrationOwnerCount;
            public int ownersRequiringDependencyReviewCount;
            public int blockingDependencyCount;
            public int externalSceneReferenceCount;
            public int buildingPlacementCount;
            public int vehiclePlacementCount;
            public int unresolvedCount;
            public int mixedOrAmbiguousCount;
            public int gameplayBuildingCandidateCount;
            public int gameplayVehicleCandidateCount;
            public int protectedAuthoredCandidateCount;
            public int staticRenderOnlyCandidateCount;
            public int unresolvedBuildingPlacementCount;
            public int unresolvedVehiclePlacementCount;
            public int reusedBuildingSourceObjectCount;
            public int reusedVehicleSourceObjectCount;
        }

        [Serializable]
        public sealed class ManifestIdentityReport
        {
            public string path;
            public string operationMapId;
            public string contentHash;
            public string canonicalScenePath;
            public string canonicalSceneGuid;
            public string canonicalSceneDependencyHash;
        }

        [Serializable]
        public sealed class ProtectedRootReport
        {
            public string name;
            public string hierarchyPath;
            public string nameHierarchyPath;
            public string globalObjectId;
            public bool present;
        }

        [Serializable]
        public sealed class ClassificationCountReport
        {
            public string classification;
            public int count;
        }

        [Serializable]
        public sealed class PlacementJoinReport
        {
            public string kind;
            public int placementIndex;
            public string sourcePath;
            public string resolveState;
            public string resolutionMethod;
            public int scenePathMatchCount;
            public int transformTupleMatchCount;
            public string resolvedSourceGlobalObjectId;
        }

        [Serializable]
        public sealed class OwnerInventoryReport
        {
            public string globalObjectId;
            public string hierarchyPath;
            public string nameHierarchyPath;
            public string prefabAssetPath;
            public string prefabAssetGuid;
            public long prefabLocalId;
            public Vector3 worldPosition;
            public Quaternion worldRotation;
            public Vector3 worldScale;
            public int sourceRendererCount;
            public int hierarchyObjectCount;
            public int blockingDependencyCount;
            public int externalSceneReferenceCount;
            public string candidateDisposition;
            public List<DependencyTypeCountReport> componentTypes;
            public List<DependencyTypeCountReport> dispositionCounts;
            public List<CrossObjectReferenceReport> externalSceneReferences;
        }

        [Serializable]
        public sealed class DependencyTypeCountReport
        {
            public string type;
            public int count;
        }

        [Serializable]
        public sealed class CrossObjectReferenceReport
        {
            public string componentGlobalObjectId;
            public string componentType;
            public string propertyPath;
            public string targetGlobalObjectId;
            public string targetHierarchyPath;
        }

        [Serializable]
        public sealed class SourceInventoryReport
        {
            public int sourceIndex;
            public string sourceGlobalObjectId;
            public string sourceHierarchyPath;
            public string sourceNameHierarchyPath;
            public string sourceDependencyHash;
            public string chunkId;
            public string generatedObjectName;
            public string meshAssetGuid;
            public long meshLocalId;
            public bool overlaySource;
            public bool sourceObjectResolved;
            public string sourceRendererType;
            public bool sourceRendererEnabled;
            public bool sourceActiveInHierarchy;
            public Vector3 worldPosition;
            public Quaternion worldRotation;
            public Vector3 worldScale;
            public Vector3 worldBoundsCenter;
            public Vector3 worldBoundsSize;
            public bool underProtectedRoot;
            public string prefabAssetPath;
            public string prefabAssetGuid;
            public long prefabLocalId;
            public string migrationOwnerGlobalObjectId;
            public string classification;
            public int buildingJoinCount;
            public int vehicleJoinCount;
            public List<string> componentTypes;
            public List<string> materialGuids;
        }
    }
}

#endif
