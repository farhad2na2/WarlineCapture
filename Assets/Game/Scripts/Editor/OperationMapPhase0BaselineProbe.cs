#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using Game.Authoring;
    using Game.Composition;
    using Game.Configs;
    using Game.Rendering;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    public static class OperationMapPhase0BaselineProbe
    {
        internal const string ReportSchema = "warline.operation-map.phase0-baseline";
        internal const int ReportSchemaVersion = 1;
        internal const string ReportPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_PHASE0_BASELINE_REPORT_PATH";
        internal const string DefaultReportPath =
            "/private/tmp/warline-operation-map-phase0-baseline.json";

        private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
        private const string MatchSubScenePath = "Assets/Game/Scenes/Match/MatchSubScene.unity";
        private const string ManifestPath =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationManifest.asset";
        private const string IntegrityPath =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationSceneIntegrity.json";
        private const string GeneratedSceneFolder =
            StaticMapPresentationBaker.SceneOutputFolder;
        private const string AggregateAlgorithm =
            "sha256(utf8(path\\0sha256\\n), entries sorted by ordinal path)";
        private const int IntegritySchemaVersion = 1;

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        public static void Run()
        {
            string projectRoot = RequireProjectRoot();
            string outputPath = ResolveReportOutputPath(
                projectRoot,
                Environment.GetEnvironmentVariable(ReportPathEnvironmentVariable));
            BaselineReport report = null;
            PublishReportAtomically(outputPath, () =>
            {
                RequireCleanLoadedScenes();
                SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
                try
                {
                    report = BuildReport(projectRoot, outputPath, previousSetup);
                    return JsonUtility.ToJson(report, true) + "\n";
                }
                finally
                {
                    RestoreSceneSetup(previousSetup);
                }
            });

            Debug.Log(
                $"[OperationMapPhase0BaselineProbe] result=Passed " +
                $"chunks={report.manifest.chunkCount} sources={report.manifest.sourceCount} " +
                $"report={outputPath}");
        }

        internal static void PublishReportAtomically(
            string outputPath,
            Func<string> buildJson,
            Action<string, string, Encoding> writeAllText = null)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Report output path is required.", nameof(outputPath));
            if (buildJson == null)
                throw new ArgumentNullException(nameof(buildJson));

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            string temporaryPath = outputPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                string json = buildJson();
                if (!TryValidateRequiredReportShape(json, out string rejectionReason))
                {
                    throw new InvalidOperationException(
                        $"Operation-map baseline report failed its schema-shape check: {rejectionReason}");
                }

                Action<string, string, Encoding> writer = writeAllText ??
                    ((path, content, encoding) => File.WriteAllText(path, content, encoding));
                writer(temporaryPath, json, Utf8WithoutBom);
                if (!File.Exists(temporaryPath))
                {
                    throw new InvalidOperationException(
                        "Persisted operation-map baseline report is missing after write.");
                }
                if (!TryValidateRequiredReportShape(
                        File.ReadAllText(temporaryPath, Utf8WithoutBom),
                        out string persistedRejectionReason))
                {
                    throw new InvalidOperationException(
                        "Persisted operation-map baseline report failed its schema-shape check: " +
                        persistedRejectionReason);
                }

                if (File.Exists(outputPath))
                    File.Replace(temporaryPath, outputPath, null);
                else
                    File.Move(temporaryPath, outputPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        internal static string ResolveReportOutputPath(string projectRoot, string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("Project root is required.", nameof(projectRoot));

            string candidate = string.IsNullOrWhiteSpace(configuredPath)
                ? DefaultReportPath
                : configuredPath.Trim();
            if (!Path.IsPathRooted(candidate))
                throw new InvalidOperationException("The operation-map baseline report path must be absolute.");

            string fullPath = Path.GetFullPath(candidate);
            string normalizedRoot = Path.GetFullPath(projectRoot);
            if (IsSameOrChildPath(fullPath, normalizedRoot))
            {
                throw new InvalidOperationException(
                    $"Refusing to write the operation-map baseline report inside project root {normalizedRoot}.");
            }
            if (!string.Equals(Path.GetExtension(fullPath), ".json", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The operation-map baseline report path must end in .json.");

            string directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Baseline report directory does not exist: {directory}");
            return fullPath;
        }

        internal static string ComputeSha256(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            using SHA256 algorithm = SHA256.Create();
            return ToLowerHex(algorithm.ComputeHash(bytes));
        }

        internal static string ComputeAggregateHash(IEnumerable<HashInput> inputs)
        {
            if (inputs == null)
                throw new ArgumentNullException(nameof(inputs));

            HashInput[] ordered = inputs
                .OrderBy(input => input.path, StringComparer.Ordinal)
                .ToArray();
            var builder = new StringBuilder(ordered.Length * 128);
            string previousPath = null;
            for (int i = 0; i < ordered.Length; i++)
            {
                HashInput input = ordered[i];
                if (string.IsNullOrWhiteSpace(input.path) || string.IsNullOrWhiteSpace(input.sha256))
                    throw new InvalidOperationException($"Aggregate hash input {i} is incomplete.");
                if (string.Equals(previousPath, input.path, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Aggregate hash input path is duplicated: {input.path}");

                builder.Append(input.path).Append('\0').Append(input.sha256).Append('\n');
                previousPath = input.path;
            }

            return ComputeSha256(Utf8WithoutBom.GetBytes(builder.ToString()));
        }

        internal static bool HasRequiredReportShape(string json)
        {
            return TryValidateRequiredReportShape(json, out _);
        }

        private static bool TryValidateRequiredReportShape(string json, out string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                rejectionReason = "empty-json";
                return false;
            }

            try
            {
                BaselineReport report = JsonUtility.FromJson<BaselineReport>(json);
                if (report == null) return Reject("missing-report", out rejectionReason);
                if (!string.Equals(report.reportSchema, ReportSchema, StringComparison.Ordinal) ||
                    report.reportSchemaVersion != ReportSchemaVersion)
                    return Reject("unsupported-report-schema", out rejectionReason);
                if (!string.Equals(report.result, "Passed", StringComparison.Ordinal))
                    return Reject("result-not-passed", out rejectionReason);
                if (!IsProjectReportComplete(report.project))
                    return Reject("project-incomplete", out rejectionReason);
                if (report.sceneSetupBeforeProbe == null)
                    return Reject("scene-setup-missing", out rejectionReason);
                if (!IsSceneReportSetComplete(report.scenes, report.subSceneReference))
                    return Reject("scene-set-incomplete", out rejectionReason);
                if (!IsMatchSceneViewReferenceSetComplete(report.matchSceneViewReferences))
                    return Reject("match-scene-view-references-incomplete", out rejectionReason);
                if (!IsManifestReportComplete(report.manifest))
                    return Reject("manifest-incomplete", out rejectionReason);
                if (!IsGeneratedOutputsReportComplete(report.generatedOutputs, report.manifest))
                    return Reject("generated-outputs-incomplete", out rejectionReason);
                if (!IsBuildSettingsSceneSetComplete(report.buildSettingsScenes))
                    return Reject("build-settings-scenes-incomplete", out rejectionReason);
                if (!IsPlacementReportComplete(report.buildingPlacements))
                    return Reject("building-placements-incomplete", out rejectionReason);
                if (!IsPlacementReportComplete(report.vehiclePlacements))
                    return Reject("vehicle-placements-incomplete", out rejectionReason);
                if (!IsMapDataReportComplete(report.mapData))
                    return Reject("map-data-incomplete", out rejectionReason);
                if (!Path.IsPathRooted(report.reportPath))
                    return Reject("report-path-invalid", out rejectionReason);

                rejectionReason = "none";
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is InvalidOperationException ||
                exception is NotSupportedException)
            {
                rejectionReason = "unreadable-json:" + exception.GetType().Name;
                return false;
            }
        }

        private static bool Reject(string reason, out string rejectionReason)
        {
            rejectionReason = reason;
            return false;
        }

        internal static bool HasSupportedIntegrityDocumentShape(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                IntegrityDocument document = JsonUtility.FromJson<IntegrityDocument>(json);
                if (document == null || document.schemaVersion != IntegritySchemaVersion ||
                    !IsHash128(document.contentHash) || document.scenes == null || document.scenes.Length == 0)
                {
                    return false;
                }

                var paths = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < document.scenes.Length; i++)
                {
                    IntegrityEntry entry = document.scenes[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.scenePath) ||
                        !entry.scenePath.EndsWith(".unity", StringComparison.Ordinal) ||
                        !IsSha256(entry.fileHash) || !IsSha256(entry.metaHash) ||
                        !paths.Add(entry.scenePath))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool IsProjectReportComplete(ProjectReport project)
        {
            return project != null &&
                   !string.IsNullOrWhiteSpace(project.unityVersion) &&
                   !string.IsNullOrWhiteSpace(project.projectName) &&
                   Path.IsPathRooted(project.projectRoot) &&
                   !string.IsNullOrWhiteSpace(project.productName) &&
                   !string.IsNullOrWhiteSpace(project.productGuid) &&
                   !string.IsNullOrWhiteSpace(project.companyName) &&
                   !string.IsNullOrWhiteSpace(project.applicationIdentifier);
        }

        private static bool IsSceneReportSetComplete(
            IReadOnlyList<SceneReport> scenes,
            SubSceneReferenceReport subSceneReference)
        {
            if (scenes == null || scenes.Count != 2 || subSceneReference == null ||
                string.IsNullOrWhiteSpace(subSceneReference.componentHierarchyPath) ||
                string.IsNullOrWhiteSpace(subSceneReference.componentGlobalObjectId) ||
                !string.Equals(subSceneReference.sceneAssetPath, MatchSubScenePath, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(subSceneReference.sceneAssetGuid) || !subSceneReference.autoLoad)
            {
                return false;
            }

            SceneReport match = scenes.SingleOrDefault(
                scene => string.Equals(scene?.path, MatchScenePath, StringComparison.Ordinal));
            SceneReport subScene = scenes.SingleOrDefault(
                scene => string.Equals(scene?.path, MatchSubScenePath, StringComparison.Ordinal));
            return IsSceneReportComplete(match) && IsSceneReportComplete(subScene) &&
                   subScene.autoLoadKnown && subScene.autoLoad;
        }

        private static bool IsSceneReportComplete(SceneReport scene)
        {
            return scene != null &&
                   !string.IsNullOrWhiteSpace(scene.guid) &&
                   !string.IsNullOrWhiteSpace(scene.loadMode) &&
                   scene.loadedDuringInspection &&
                   scene.rootObjectCount > 0 &&
                   scene.hierarchyObjectCount >= scene.rootObjectCount &&
                   IsSha256(scene.hierarchySha256) &&
                   scene.rootObjects != null &&
                   scene.rootObjects.Count == scene.rootObjectCount &&
                   scene.rootObjects.All(root => root != null &&
                       !string.IsNullOrWhiteSpace(root.name) &&
                       !string.IsNullOrWhiteSpace(root.hierarchyPath) &&
                       root.hierarchyObjectCount > 0 &&
                       IsSha256(root.hierarchySha256) &&
                       root.rootComponentTypes != null && root.rootComponentTypes.Count > 0);
        }

        private static bool IsMatchSceneViewReferenceSetComplete(
            IReadOnlyList<SerializedObjectReferenceFieldReport> references)
        {
            return references != null && references.Count > 0 && references.All(reference =>
                reference != null &&
                !string.IsNullOrWhiteSpace(reference.propertyName) &&
                !string.IsNullOrWhiteSpace(reference.declaredType) &&
                reference.elementCount >= 0 &&
                reference.targets != null &&
                reference.targets.Count == reference.elementCount &&
                (reference.isCollection || reference.elementCount == 1) &&
                reference.targets.All(IsObjectIdentityComplete));
        }

        private static bool IsManifestReportComplete(ManifestReport manifest)
        {
            return manifest != null && IsObjectIdentityComplete(manifest.asset) &&
                   manifest.schemaVersion == StaticMapPresentationManifest.CurrentSchemaVersion &&
                   string.Equals(
                       manifest.operationMapId,
                       StaticMapPresentationBaker.CurrentOperationMapId,
                       StringComparison.Ordinal) &&
                   string.Equals(manifest.canonicalScenePath, MatchScenePath, StringComparison.Ordinal) &&
                   !string.IsNullOrWhiteSpace(manifest.canonicalSceneGuid) &&
                   IsHash128(manifest.canonicalSceneDependencyHash) &&
                   string.Equals(
                       manifest.canonicalSceneDependencyHash,
                       manifest.computedCanonicalSceneDependencyHash,
                       StringComparison.Ordinal) &&
                   manifest.chunkSize > 0f && IsHash128(manifest.contentHash) &&
                   string.Equals(manifest.contentHash, manifest.computedContentHash, StringComparison.Ordinal) &&
                   manifest.chunkCount > 0 && manifest.sourceCount > 0 &&
                   IsSha256(manifest.fileSha256) && IsSha256(manifest.metaSha256);
        }

        private static bool IsGeneratedOutputsReportComplete(
            GeneratedOutputsReport generated,
            ManifestReport manifest)
        {
            if (generated == null || manifest == null ||
                !string.Equals(generated.integrityPath, IntegrityPath, StringComparison.Ordinal) ||
                generated.integritySchemaVersion != IntegritySchemaVersion ||
                !string.Equals(generated.integrityContentHash, manifest.contentHash, StringComparison.Ordinal) ||
                !IsSha256(generated.integrityFileSha256) || !IsSha256(generated.integrityMetaSha256) ||
                generated.manifestSceneCount != manifest.chunkCount ||
                generated.manifestSceneCount <= 0 ||
                generated.ledgerSceneCount != generated.manifestSceneCount ||
                generated.diskSceneCount != generated.manifestSceneCount ||
                generated.diskMetaCount != generated.manifestSceneCount ||
                !generated.exactFileSetParity ||
                !string.Equals(generated.aggregateAlgorithm, AggregateAlgorithm, StringComparison.Ordinal) ||
                !IsSha256(generated.sceneFilesAggregateSha256) ||
                !IsSha256(generated.sceneMetasAggregateSha256) ||
                !IsSha256(generated.combinedAggregateSha256) ||
                generated.files == null || generated.files.Count != generated.manifestSceneCount)
            {
                return false;
            }

            var paths = new HashSet<string>(StringComparer.Ordinal);
            var guids = new HashSet<string>(StringComparer.Ordinal);
            var sceneInputs = new List<HashInput>(generated.files.Count);
            var metaInputs = new List<HashInput>(generated.files.Count);
            var combinedInputs = new List<HashInput>(generated.files.Count * 2);
            string previousPath = null;
            for (int i = 0; i < generated.files.Count; i++)
            {
                GeneratedSceneFileReport file = generated.files[i];
                if (file == null || string.IsNullOrWhiteSpace(file.scenePath) ||
                    !file.scenePath.EndsWith(".unity", StringComparison.Ordinal) ||
                    !paths.Add(file.scenePath) ||
                    (previousPath != null && string.CompareOrdinal(previousPath, file.scenePath) >= 0) ||
                    string.IsNullOrWhiteSpace(file.sceneGuid) || !guids.Add(file.sceneGuid) ||
                    !IsSha256(file.sceneSha256) ||
                    !string.Equals(file.metaPath, file.scenePath + ".meta", StringComparison.Ordinal) ||
                    !IsSha256(file.metaSha256))
                {
                    return false;
                }

                sceneInputs.Add(new HashInput(file.scenePath, file.sceneSha256));
                metaInputs.Add(new HashInput(file.metaPath, file.metaSha256));
                combinedInputs.Add(new HashInput(file.scenePath, file.sceneSha256));
                combinedInputs.Add(new HashInput(file.metaPath, file.metaSha256));
                previousPath = file.scenePath;
            }

            return string.Equals(
                       generated.sceneFilesAggregateSha256,
                       ComputeAggregateHash(sceneInputs),
                       StringComparison.Ordinal) &&
                   string.Equals(
                       generated.sceneMetasAggregateSha256,
                       ComputeAggregateHash(metaInputs),
                       StringComparison.Ordinal) &&
                   string.Equals(
                       generated.combinedAggregateSha256,
                       ComputeAggregateHash(combinedInputs),
                       StringComparison.Ordinal);
        }

        private static bool IsBuildSettingsSceneSetComplete(IReadOnlyList<BuildSettingsSceneReport> scenes)
        {
            if (scenes == null || scenes.Count == 0)
                return false;

            var paths = new HashSet<string>(StringComparer.Ordinal);
            var guids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < scenes.Count; i++)
            {
                BuildSettingsSceneReport scene = scenes[i];
                if (scene == null || scene.buildSettingsIndex != i ||
                    string.IsNullOrWhiteSpace(scene.path) ||
                    !scene.path.EndsWith(".unity", StringComparison.Ordinal) ||
                    !paths.Add(scene.path) || string.IsNullOrWhiteSpace(scene.guid) || !guids.Add(scene.guid))
                {
                    return false;
                }
            }

            return paths.Contains(MatchScenePath);
        }

        private static bool IsPlacementReportComplete(PlacementReport placements)
        {
            if (placements == null || string.IsNullOrWhiteSpace(placements.kind) ||
                !IsObjectIdentityComplete(placements.config) ||
                !IsObjectIdentityComplete(placements.authoringRoot) || placements.count <= 0 ||
                !IsSha256(placements.identityPathAggregateSha256) || placements.entries == null ||
                placements.entries.Count != placements.count)
            {
                return false;
            }

            PlacementEntryReport previous = null;
            for (int i = 0; i < placements.entries.Count; i++)
            {
                PlacementEntryReport entry = placements.entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.sourcePath) ||
                    string.IsNullOrWhiteSpace(entry.category) ||
                    entry.configSourcePathOccurrenceCount <= 0 || entry.sceneMatchCount <= 0 ||
                    !IsFinite(entry.worldCenter) || !IsFinite(entry.worldPosition) ||
                    !IsFinite(entry.worldEulerAngles) || !IsFinite(entry.worldScale) ||
                    !IsFinite(entry.yawDegrees) || !IsObjectIdentityComplete(entry.prefab) ||
                    (previous != null && ComparePlacementEntries(previous, entry) >= 0))
                {
                    return false;
                }

                previous = entry;
            }

            return true;
        }

        private static bool IsMapDataReportComplete(MapDataReport mapData)
        {
            return mapData != null && IsObjectIdentityComplete(mapData.mapSurfaceAuthoring) &&
                   IsObjectIdentityComplete(mapData.gridAsset) && IsObjectIdentityComplete(mapData.mapSurfaceAsset) &&
                   mapData.gridWidth > 0 && mapData.gridHeight > 0 && mapData.gridCellSize > 0f &&
                   mapData.gridCellCount == (long)mapData.gridWidth * mapData.gridHeight &&
                   mapData.blockedCellCount >= 0 && mapData.blockedCellCount <= mapData.gridCellCount &&
                   mapData.mapSurfaceDimensions.x == mapData.gridWidth &&
                   mapData.mapSurfaceDimensions.y == mapData.gridHeight &&
                   mapData.mapSurfaceCellSize == mapData.gridCellSize &&
                   mapData.mapSurfaceOrigin == mapData.gridOrigin &&
                   mapData.surfaceCount >= mapData.gridCellCount && mapData.connectionCount >= 0 &&
                   mapData.payloadVersion > 0 && mapData.compressedPayloadBytes > 0 &&
                   mapData.uncompressedPayloadBytes > 0 && IsHash128(mapData.runtimeBlobHash) &&
                   mapData.dimensionsOriginCellSizeConsistent;
        }

        private static bool IsObjectIdentityComplete(ObjectIdentityReport identity)
        {
            if (identity == null || string.IsNullOrWhiteSpace(identity.name) ||
                string.IsNullOrWhiteSpace(identity.type) || string.IsNullOrWhiteSpace(identity.globalObjectId))
            {
                return false;
            }

            bool assetIdentity = !string.IsNullOrWhiteSpace(identity.assetPath) &&
                                 !string.IsNullOrWhiteSpace(identity.assetGuid) && identity.localId != 0;
            bool sceneIdentity = !string.IsNullOrWhiteSpace(identity.scenePath) &&
                                 !string.IsNullOrWhiteSpace(identity.sceneGuid) &&
                                 !string.IsNullOrWhiteSpace(identity.hierarchyPath);
            return assetIdentity || sceneIdentity;
        }

        private static bool IsSha256(string value)
        {
            return IsLowerHex(value, 64);
        }

        private static bool IsHash128(string value)
        {
            return IsLowerHex(value, 32);
        }

        private static bool IsLowerHex(string value, int length)
        {
            return value != null && value.Length == length &&
                   value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static BaselineReport BuildReport(
            string projectRoot,
            string outputPath,
            IReadOnlyList<SceneSetup> previousSetup)
        {
            RequireAsset(MatchScenePath);
            RequireAsset(MatchSubScenePath);
            RequireFile(projectRoot, ManifestPath);
            RequireFile(projectRoot, IntegrityPath);

            Scene matchScene = OpenSceneForInspection(MatchScenePath);
            Scene subScene = OpenSceneForInspection(MatchSubScenePath);
            MatchSceneView matchSceneView = RequireSingleMatchSceneView(matchScene);
            SubSceneReferenceReport subSceneReference = RequireSubSceneReference(matchScene);
            RequireEqual(MatchSubScenePath, subSceneReference.sceneAssetPath, "Match subscene asset path");

            StaticMapPresentationManifest manifest =
                AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(ManifestPath);
            if (manifest == null)
                throw new InvalidOperationException($"Static presentation manifest is missing: {ManifestPath}");
            if (matchSceneView.StaticMapPresentationManifest != manifest)
                throw new InvalidOperationException("MatchSceneView does not reference the canonical static presentation manifest.");

            ManifestReport manifestReport = BuildManifestReport(projectRoot, manifest);
            GeneratedOutputsReport generatedOutputs = BuildGeneratedOutputsReport(projectRoot, manifest);
            PlacementReport buildingPlacements = BuildBuildingPlacementReport(matchScene, matchSceneView);
            PlacementReport vehiclePlacements = BuildVehiclePlacementReport(matchScene, matchSceneView);
            MapDataReport mapData = BuildMapDataReport(matchSceneView);

            return new BaselineReport
            {
                reportSchema = ReportSchema,
                reportSchemaVersion = ReportSchemaVersion,
                result = "Passed",
                reportPath = NormalizeSeparators(outputPath),
                project = BuildProjectReport(projectRoot),
                sceneSetupBeforeProbe = BuildSceneSetupReport(previousSetup),
                scenes = new List<SceneReport>
                {
                    BuildSceneReport(matchScene, "ExplicitInspection", false, true),
                    BuildSceneReport(subScene, "ExplicitInspection", subSceneReference.autoLoad, true)
                },
                subSceneReference = subSceneReference,
                matchSceneViewReferences = BuildMatchSceneViewReferenceReports(matchSceneView),
                manifest = manifestReport,
                generatedOutputs = generatedOutputs,
                buildSettingsScenes = BuildSettingsSceneReports(),
                buildingPlacements = buildingPlacements,
                vehiclePlacements = vehiclePlacements,
                mapData = mapData
            };
        }

        private static ProjectReport BuildProjectReport(string projectRoot)
        {
            return new ProjectReport
            {
                unityVersion = Application.unityVersion,
                projectName = new DirectoryInfo(projectRoot).Name,
                projectRoot = NormalizeSeparators(projectRoot),
                productName = Application.productName,
                productGuid = PlayerSettings.productGUID.ToString(),
                companyName = PlayerSettings.companyName,
                applicationIdentifier = PlayerSettings.applicationIdentifier
            };
        }

        private static List<SceneSetupReport> BuildSceneSetupReport(IReadOnlyList<SceneSetup> setup)
        {
            var reports = new List<SceneSetupReport>(setup?.Count ?? 0);
            if (setup == null)
                return reports;

            for (int i = 0; i < setup.Count; i++)
            {
                SceneSetup entry = setup[i];
                reports.Add(new SceneSetupReport
                {
                    setupIndex = i,
                    path = entry.path ?? string.Empty,
                    guid = string.IsNullOrWhiteSpace(entry.path)
                        ? string.Empty
                        : AssetDatabase.AssetPathToGUID(entry.path),
                    isLoaded = entry.isLoaded,
                    isActive = entry.isActive,
                    isSubScene = entry.isSubScene
                });
            }

            return reports;
        }

        private static SceneReport BuildSceneReport(
            Scene scene,
            string loadMode,
            bool autoLoad,
            bool autoLoadKnown)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException($"Scene was not loaded for inspection: {scene.path}");

            GameObject[] roots = scene.GetRootGameObjects();
            var rootReports = new List<RootObjectReport>(roots.Length);
            var sceneHashInputs = new List<HashInput>(roots.Length);
            int hierarchyObjectCount = 0;
            for (int i = 0; i < roots.Length; i++)
            {
                RootObjectReport root = BuildRootObjectReport(roots[i], i);
                rootReports.Add(root);
                hierarchyObjectCount += root.hierarchyObjectCount;
                sceneHashInputs.Add(new HashInput(root.hierarchyPath, root.hierarchySha256));
            }

            return new SceneReport
            {
                path = scene.path,
                guid = AssetDatabase.AssetPathToGUID(scene.path),
                loadMode = loadMode,
                loadedDuringInspection = scene.isLoaded,
                autoLoadKnown = autoLoadKnown,
                autoLoad = autoLoad,
                rootObjectCount = roots.Length,
                hierarchyObjectCount = hierarchyObjectCount,
                hierarchySha256 = ComputeAggregateHash(sceneHashInputs),
                rootObjects = rootReports
            };
        }

        private static RootObjectReport BuildRootObjectReport(GameObject root, int rootIndex)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            var builder = new StringBuilder(transforms.Length * 96);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                Component[] components = transform.GetComponents<Component>();
                string[] componentTypes = components
                    .Select(component => component == null
                        ? "<MissingScript>"
                        : component.GetType().FullName)
                    .OrderBy(type => type, StringComparer.Ordinal)
                    .ToArray();
                builder.Append(BuildIndexedHierarchyPath(transform))
                    .Append('|').Append(transform.gameObject.activeSelf ? '1' : '0')
                    .Append('|').Append(transform.gameObject.layer)
                    .Append('|').AppendJoin(",", componentTypes)
                    .Append('\n');
            }

            string path = $"{root.name}[{rootIndex}]";
            return new RootObjectReport
            {
                name = root.name,
                siblingIndex = rootIndex,
                hierarchyPath = path,
                activeSelf = root.activeSelf,
                directChildCount = root.transform.childCount,
                hierarchyObjectCount = transforms.Length,
                hierarchySha256 = ComputeSha256(Utf8WithoutBom.GetBytes(builder.ToString())),
                rootComponentTypes = root.GetComponents<Component>()
                    .Select(component => component == null ? "<MissingScript>" : component.GetType().FullName)
                    .OrderBy(type => type, StringComparer.Ordinal)
                    .ToList()
            };
        }

        private static SubSceneReferenceReport RequireSubSceneReference(Scene matchScene)
        {
            var references = new List<SubSceneReferenceReport>();
            GameObject[] roots = matchScene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                MonoBehaviour[] behaviours = roots[rootIndex].GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour behaviour = behaviours[i];
                    if (behaviour == null ||
                        !string.Equals(behaviour.GetType().FullName, "Unity.Scenes.SubScene", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var serialized = new SerializedObject(behaviour);
                    SerializedProperty sceneAssetProperty = serialized.FindProperty("_SceneAsset");
                    SerializedProperty autoLoadProperty = serialized.FindProperty("AutoLoadScene");
                    Object sceneAsset = sceneAssetProperty?.objectReferenceValue;
                    references.Add(new SubSceneReferenceReport
                    {
                        componentHierarchyPath = BuildIndexedHierarchyPath(behaviour.transform),
                        componentGlobalObjectId = GetGlobalObjectId(behaviour),
                        sceneAssetPath = sceneAsset != null ? AssetDatabase.GetAssetPath(sceneAsset) : string.Empty,
                        sceneAssetGuid = sceneAsset != null
                            ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sceneAsset))
                            : string.Empty,
                        autoLoad = autoLoadProperty != null && autoLoadProperty.boolValue
                    });
                }
            }

            if (references.Count != 1)
                throw new InvalidOperationException($"Expected one Match SubScene reference; found {references.Count}.");
            if (string.IsNullOrWhiteSpace(references[0].sceneAssetPath))
                throw new InvalidOperationException("Match SubScene reference has no scene asset.");
            return references[0];
        }

        private static List<SerializedObjectReferenceFieldReport> BuildMatchSceneViewReferenceReports(
            MatchSceneView view)
        {
            FieldInfo[] fields = typeof(MatchSceneView)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(IsSerializedObjectReferenceField)
                .OrderBy(field => field.Name, StringComparer.Ordinal)
                .ToArray();
            if (fields.Length == 0)
                throw new InvalidOperationException("MatchSceneView has no serialized object-reference fields.");

            var serialized = new SerializedObject(view);
            var reports = new List<SerializedObjectReferenceFieldReport>(fields.Length);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                SerializedProperty property = serialized.FindProperty(field.Name);
                if (property == null)
                    throw new InvalidOperationException($"Serialized MatchSceneView property is missing: {field.Name}");

                bool collection = TryGetObjectReferenceElementType(field.FieldType, out Type elementType);
                var targets = new List<ObjectIdentityReport>();
                if (collection)
                {
                    for (int elementIndex = 0; elementIndex < property.arraySize; elementIndex++)
                    {
                        SerializedProperty element = property.GetArrayElementAtIndex(elementIndex);
                        Object target = element.objectReferenceValue;
                        if (target == null)
                        {
                            throw new InvalidOperationException(
                                $"MatchSceneView reference is missing: {field.Name}[{elementIndex}]");
                        }
                        targets.Add(BuildObjectIdentity(target));
                    }
                }
                else
                {
                    Object target = property.objectReferenceValue;
                    if (target == null)
                        throw new InvalidOperationException($"MatchSceneView reference is missing: {field.Name}");
                    targets.Add(BuildObjectIdentity(target));
                }

                reports.Add(new SerializedObjectReferenceFieldReport
                {
                    propertyName = field.Name,
                    declaredType = collection ? elementType.FullName : field.FieldType.FullName,
                    isCollection = collection,
                    elementCount = targets.Count,
                    targets = targets
                });
            }

            return reports;
        }

        private static bool IsSerializedObjectReferenceField(FieldInfo field)
        {
            bool serialized = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
            if (!serialized || field.IsStatic || field.IsNotSerialized)
                return false;
            if (typeof(Object).IsAssignableFrom(field.FieldType))
                return true;
            return TryGetObjectReferenceElementType(field.FieldType, out _);
        }

        private static bool TryGetObjectReferenceElementType(Type type, out Type elementType)
        {
            elementType = null;
            if (type.IsArray)
                elementType = type.GetElementType();
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                elementType = type.GetGenericArguments()[0];
            return elementType != null && typeof(Object).IsAssignableFrom(elementType);
        }

        private static ObjectIdentityReport BuildObjectIdentity(Object target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            string assetPath = AssetDatabase.GetAssetPath(target) ?? string.Empty;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out string assetGuid, out long localId);
            string scenePath = string.Empty;
            string hierarchyPath = string.Empty;
            if (target is Component component)
            {
                scenePath = component.gameObject.scene.path ?? string.Empty;
                hierarchyPath = BuildIndexedHierarchyPath(component.transform);
            }
            else if (target is GameObject gameObject)
            {
                scenePath = gameObject.scene.path ?? string.Empty;
                hierarchyPath = BuildIndexedHierarchyPath(gameObject.transform);
            }

            return new ObjectIdentityReport
            {
                name = target.name,
                type = target.GetType().FullName,
                assetPath = assetPath,
                assetGuid = assetGuid ?? string.Empty,
                localId = localId,
                scenePath = scenePath,
                sceneGuid = string.IsNullOrWhiteSpace(scenePath)
                    ? string.Empty
                    : AssetDatabase.AssetPathToGUID(scenePath),
                hierarchyPath = hierarchyPath,
                globalObjectId = GetGlobalObjectId(target)
            };
        }

        private static ManifestReport BuildManifestReport(
            string projectRoot,
            StaticMapPresentationManifest manifest)
        {
            if (manifest.SchemaVersion != StaticMapPresentationManifest.CurrentSchemaVersion)
                throw new InvalidOperationException($"Unsupported manifest schema: {manifest.SchemaVersion}");
            RequireEqual(MatchScenePath, manifest.CanonicalScenePath, "Manifest canonical scene path");
            if (manifest.ChunkSize <= 0f || manifest.Chunks == null || manifest.Chunks.Count == 0 ||
                manifest.Sources == null || manifest.Sources.Count == 0)
            {
                throw new InvalidOperationException("Static presentation manifest shape is incomplete.");
            }

            string computedCanonicalHash = StaticMapPresentationCanonicalSourceHash.Compute(MatchScenePath);
            RequireEqual(
                manifest.CanonicalSceneDependencyHash,
                computedCanonicalHash,
                "Manifest canonical dependency hash");
            string computedContentHash = ComputeManifestContentHash(manifest);
            RequireEqual(manifest.ContentHash, computedContentHash, "Manifest content hash");

            var chunkIds = new HashSet<string>(StringComparer.Ordinal);
            var scenePaths = new HashSet<string>(StringComparer.Ordinal);
            int nextSourceIndex = 0;
            for (int chunkIndex = 0; chunkIndex < manifest.Chunks.Count; chunkIndex++)
            {
                StaticMapPresentationChunkEntry chunk = manifest.Chunks[chunkIndex];
                if (chunk == null || string.IsNullOrWhiteSpace(chunk.ChunkId) ||
                    string.IsNullOrWhiteSpace(chunk.ScenePath) || chunk.SourceCount <= 0)
                {
                    throw new InvalidOperationException($"Manifest chunk is incomplete at index {chunkIndex}.");
                }
                if (!chunkIds.Add(chunk.ChunkId))
                    throw new InvalidOperationException($"Manifest chunk id is duplicated: {chunk.ChunkId}");
                if (!scenePaths.Add(chunk.ScenePath))
                    throw new InvalidOperationException($"Manifest scene path is duplicated: {chunk.ScenePath}");
                if (chunk.SourceStartIndex != nextSourceIndex ||
                    chunk.SourceStartIndex + chunk.SourceCount > manifest.Sources.Count)
                {
                    throw new InvalidOperationException($"Manifest source range is inconsistent for {chunk.ChunkId}.");
                }

                for (int sourceIndex = chunk.SourceStartIndex;
                     sourceIndex < chunk.SourceStartIndex + chunk.SourceCount;
                     sourceIndex++)
                {
                    StaticMapPresentationSourceEntry source = manifest.Sources[sourceIndex];
                    if (source == null || string.IsNullOrWhiteSpace(source.SourceGlobalObjectId) ||
                        string.IsNullOrWhiteSpace(source.SourceHierarchyPath) ||
                        string.IsNullOrWhiteSpace(source.SourceDependencyHash) ||
                        !string.Equals(source.ChunkId, chunk.ChunkId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Manifest source is incomplete or assigned to the wrong chunk at index {sourceIndex}.");
                    }
                }

                nextSourceIndex += chunk.SourceCount;
            }

            if (nextSourceIndex != manifest.Sources.Count)
                throw new InvalidOperationException("Manifest chunk ranges do not cover every source exactly once.");

            return new ManifestReport
            {
                asset = BuildObjectIdentity(manifest),
                schemaVersion = manifest.SchemaVersion,
                operationMapId = manifest.OperationMapId,
                canonicalScenePath = manifest.CanonicalScenePath,
                canonicalSceneGuid = AssetDatabase.AssetPathToGUID(manifest.CanonicalScenePath),
                canonicalSceneDependencyHash = manifest.CanonicalSceneDependencyHash,
                computedCanonicalSceneDependencyHash = computedCanonicalHash,
                chunkSize = manifest.ChunkSize,
                contentHash = manifest.ContentHash,
                computedContentHash = computedContentHash,
                chunkCount = manifest.Chunks.Count,
                sourceCount = manifest.Sources.Count,
                fileSha256 = ComputeFileSha256(ResolveProjectPath(projectRoot, ManifestPath)),
                metaSha256 = ComputeFileSha256(ResolveProjectPath(projectRoot, ManifestPath + ".meta"))
            };
        }

        private static string ComputeManifestContentHash(StaticMapPresentationManifest manifest)
        {
            return StaticMapPresentationBaker.ComputeContentHash(
                manifest.ChunkSize,
                manifest.Chunks,
                manifest.Sources);
        }

        private static GeneratedOutputsReport BuildGeneratedOutputsReport(
            string projectRoot,
            StaticMapPresentationManifest manifest)
        {
            string integrityPhysicalPath = ResolveProjectPath(projectRoot, IntegrityPath);
            string integrityJson = File.ReadAllText(integrityPhysicalPath);
            if (!HasSupportedIntegrityDocumentShape(integrityJson))
                throw new InvalidOperationException("Static presentation integrity ledger is unreadable or incomplete.");
            IntegrityDocument ledger = JsonUtility.FromJson<IntegrityDocument>(integrityJson);
            RequireEqual(manifest.ContentHash, ledger.contentHash, "Integrity ledger content hash");

            string[] manifestPaths = manifest.Chunks.Select(chunk => chunk.ScenePath).ToArray();
            string[] ledgerPaths = ledger.scenes.Select(entry => entry?.scenePath).ToArray();
            if (!StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                    projectRoot,
                    manifest.OperationMapId,
                    IntegrityPath,
                    manifest.ContentHash,
                    manifestPaths,
                    out _,
                    out string integrityRejectionReason))
            {
                throw new InvalidOperationException(
                    $"Authoritative static presentation integrity validation failed: {integrityRejectionReason}");
            }

            string sceneFolder = ResolveProjectPath(projectRoot, GeneratedSceneFolder);
            string[] diskScenePaths = Directory.GetFiles(sceneFolder, "*.unity", SearchOption.TopDirectoryOnly)
                .Select(path => ToAssetPath(projectRoot, path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            string[] diskMetaScenePaths = Directory.GetFiles(sceneFolder, "*.unity.meta", SearchOption.TopDirectoryOnly)
                .Select(path => ToAssetPath(projectRoot, path.Substring(0, path.Length - ".meta".Length)))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            RequireUniqueCompletePaths(manifestPaths, "manifest");
            RequireUniqueCompletePaths(ledgerPaths, "integrity ledger");
            RequireSetEqual(manifestPaths, ledgerPaths, "manifest", "integrity ledger");
            RequireSetEqual(manifestPaths, diskScenePaths, "manifest", "generated scene files");
            RequireSetEqual(manifestPaths, diskMetaScenePaths, "manifest", "generated scene metadata files");

            Dictionary<string, IntegrityEntry> ledgerByPath = ledger.scenes.ToDictionary(
                entry => entry.scenePath,
                entry => entry,
                StringComparer.Ordinal);
            var files = new List<GeneratedSceneFileReport>(manifestPaths.Length);
            var sceneHashInputs = new List<HashInput>(manifestPaths.Length);
            var metaHashInputs = new List<HashInput>(manifestPaths.Length);
            var combinedHashInputs = new List<HashInput>(manifestPaths.Length * 2);
            foreach (string path in manifestPaths.OrderBy(path => path, StringComparer.Ordinal))
            {
                string fileHash = ComputeFileSha256(ResolveProjectPath(projectRoot, path));
                string metaPath = path + ".meta";
                string metaHash = ComputeFileSha256(ResolveProjectPath(projectRoot, metaPath));
                IntegrityEntry expected = ledgerByPath[path];
                RequireEqual(expected.fileHash, fileHash, $"Integrity scene hash for {path}");
                RequireEqual(expected.metaHash, metaHash, $"Integrity metadata hash for {path}");

                files.Add(new GeneratedSceneFileReport
                {
                    scenePath = path,
                    sceneGuid = AssetDatabase.AssetPathToGUID(path),
                    sceneSha256 = fileHash,
                    metaPath = metaPath,
                    metaSha256 = metaHash
                });
                sceneHashInputs.Add(new HashInput(path, fileHash));
                metaHashInputs.Add(new HashInput(metaPath, metaHash));
                combinedHashInputs.Add(new HashInput(path, fileHash));
                combinedHashInputs.Add(new HashInput(metaPath, metaHash));
            }

            return new GeneratedOutputsReport
            {
                integrityPath = IntegrityPath,
                integritySchemaVersion = ledger.schemaVersion,
                integrityContentHash = ledger.contentHash,
                integrityFileSha256 = ComputeFileSha256(integrityPhysicalPath),
                integrityMetaSha256 = ComputeFileSha256(ResolveProjectPath(projectRoot, IntegrityPath + ".meta")),
                manifestSceneCount = manifestPaths.Length,
                ledgerSceneCount = ledgerPaths.Length,
                diskSceneCount = diskScenePaths.Length,
                diskMetaCount = diskMetaScenePaths.Length,
                exactFileSetParity = true,
                aggregateAlgorithm = AggregateAlgorithm,
                sceneFilesAggregateSha256 = ComputeAggregateHash(sceneHashInputs),
                sceneMetasAggregateSha256 = ComputeAggregateHash(metaHashInputs),
                combinedAggregateSha256 = ComputeAggregateHash(combinedHashInputs),
                files = files
            };
        }

        private static List<BuildSettingsSceneReport> BuildSettingsSceneReports()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            var reports = new List<BuildSettingsSceneReport>(scenes.Length);
            for (int i = 0; i < scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];
                if (string.IsNullOrWhiteSpace(scene.path))
                    throw new InvalidOperationException($"Build settings scene path is missing at index {i}.");
                reports.Add(new BuildSettingsSceneReport
                {
                    buildSettingsIndex = i,
                    path = scene.path,
                    guid = AssetDatabase.AssetPathToGUID(scene.path),
                    enabled = scene.enabled
                });
            }

            return reports;
        }

        private static PlacementReport BuildBuildingPlacementReport(Scene scene, MatchSceneView view)
        {
            MapBuildingPlacementConfig config = view.MapBuildingPlacementConfig;
            if (config == null || config.Placements == null || config.Placements.Count == 0)
                throw new InvalidOperationException("Match building placement config is missing or empty.");

            Dictionary<string, int> scenePaths = BuildNamePathCounts(scene);
            var entries = new List<PlacementEntryReport>(config.Placements.Count);
            Dictionary<string, int> configPathCounts = config.Placements
                .GroupBy(placement => placement?.SourcePath ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapBuildingPlacementConfigEntry placement = config.Placements[i];
                int sceneMatchCount = ValidatePlacementSourcePath(
                    placement?.SourcePath,
                    scenePaths,
                    "building",
                    i);
                if (placement.BuildingPrefab == null)
                    throw new InvalidOperationException($"Building placement prefab is missing at index {i}.");
                entries.Add(new PlacementEntryReport
                {
                    sourcePath = placement.SourcePath,
                    category = placement.Category,
                    sourceKey = string.Empty,
                    factionId = placement.FactionId,
                    configSourcePathOccurrenceCount = configPathCounts[placement.SourcePath],
                    sceneMatchCount = sceneMatchCount,
                    worldCenter = placement.WorldCenter,
                    worldPosition = placement.WorldPosition,
                    worldEulerAngles = placement.WorldEulerAngles,
                    worldScale = placement.WorldScale,
                    yawDegrees = placement.YawDegrees,
                    rotateVertical = placement.RotateVertical,
                    prefab = BuildObjectIdentity(placement.BuildingPrefab)
                });
            }

            entries.Sort(PlacementEntryReportComparer.Instance);
            RequireNoDuplicatePlacementEntries(entries, "building");
            return BuildPlacementReport(
                "Building",
                config,
                config.SpawnOnMatchStart,
                config.HideAuthoringVisualsAfterSpawn,
                view.MapBuildingAuthoringRoot,
                entries);
        }

        private static PlacementReport BuildVehiclePlacementReport(Scene scene, MatchSceneView view)
        {
            MapVehiclePlacementConfig config = view.MapVehiclePlacementConfig;
            if (config == null || config.Placements == null || config.Placements.Count == 0)
                throw new InvalidOperationException("Match vehicle placement config is missing or empty.");

            Dictionary<string, int> scenePaths = BuildNamePathCounts(scene);
            var entries = new List<PlacementEntryReport>(config.Placements.Count);
            Dictionary<string, int> configPathCounts = config.Placements
                .GroupBy(placement => placement?.SourcePath ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapVehiclePlacementConfigEntry placement = config.Placements[i];
                int sceneMatchCount = ValidatePlacementSourcePath(
                    placement?.SourcePath,
                    scenePaths,
                    "vehicle",
                    i);
                if (placement.VehiclePrefab == null || string.IsNullOrWhiteSpace(placement.VehicleSourceKey))
                    throw new InvalidOperationException($"Vehicle placement identity is missing at index {i}.");
                entries.Add(new PlacementEntryReport
                {
                    sourcePath = placement.SourcePath,
                    category = placement.Category,
                    sourceKey = placement.VehicleSourceKey,
                    factionId = placement.FactionId,
                    configSourcePathOccurrenceCount = configPathCounts[placement.SourcePath],
                    sceneMatchCount = sceneMatchCount,
                    worldCenter = placement.WorldCenter,
                    worldPosition = placement.WorldPosition,
                    worldEulerAngles = placement.WorldEulerAngles,
                    worldScale = placement.WorldScale,
                    prefab = BuildObjectIdentity(placement.VehiclePrefab)
                });
            }

            entries.Sort(PlacementEntryReportComparer.Instance);
            RequireNoDuplicatePlacementEntries(entries, "vehicle");
            return BuildPlacementReport(
                "Vehicle",
                config,
                config.SpawnOnMatchStart,
                config.HideAuthoringVisualsAfterSpawn,
                view.MapVehicleAuthoringRoot,
                entries);
        }

        private static PlacementReport BuildPlacementReport(
            string kind,
            Object config,
            bool spawnOnMatchStart,
            bool hideAuthoringVisualsAfterSpawn,
            Transform authoringRoot,
            List<PlacementEntryReport> entries)
        {
            if (authoringRoot == null)
                throw new InvalidOperationException($"{kind} authoring root is missing.");
            var inputs = entries.Select(entry =>
            {
                string identity = BuildPlacementStableIdentity(entry);
                return new HashInput(identity, ComputeSha256(Utf8WithoutBom.GetBytes(identity)));
            });
            return new PlacementReport
            {
                kind = kind,
                config = BuildObjectIdentity(config),
                spawnOnMatchStart = spawnOnMatchStart,
                hideAuthoringVisualsAfterSpawn = hideAuthoringVisualsAfterSpawn,
                authoringRoot = BuildObjectIdentity(authoringRoot),
                count = entries.Count,
                identityPathAggregateSha256 = ComputeAggregateHash(inputs),
                entries = entries
            };
        }

        private static MapDataReport BuildMapDataReport(MatchSceneView view)
        {
            MapSurfaceAuthoring authoring = view.MapSurfaceAuthoring;
            if (authoring == null || authoring.BakedSurfaceData == null || authoring.GridConfig == null)
                throw new InvalidOperationException("Match map-surface authoring references are incomplete.");

            MapSurfaceDataAsset surface = authoring.BakedSurfaceData;
            GridAuthoringConfig grid = authoring.GridConfig;
            if (view.RuntimeGridConfig == null || view.RuntimeGridConfig != grid)
                throw new InvalidOperationException("Match runtime grid and map-surface grid references are inconsistent.");
            if (grid.Width <= 0 || grid.Height <= 0 || grid.CellSize <= 0f ||
                surface.Dimensions.x <= 0 || surface.Dimensions.y <= 0 || surface.CellSize <= 0f)
            {
                throw new InvalidOperationException("Grid or map-surface dimensions are invalid.");
            }
            if (grid.Width != surface.Dimensions.x || grid.Height != surface.Dimensions.y ||
                Math.Abs(grid.CellSize - surface.CellSize) > 0.0001f ||
                Vector3.SqrMagnitude(grid.Origin - surface.GridOrigin) > 0.00000001f)
            {
                throw new InvalidOperationException("Grid and map-surface dimensions, origin, or cell size are inconsistent.");
            }
            long gridCellCount = (long)grid.Width * grid.Height;
            if (surface.SurfaceCount < gridCellCount || !surface.HasCompactPayload)
                throw new InvalidOperationException("Map-surface payload does not cover the configured grid.");

            return new MapDataReport
            {
                mapSurfaceAuthoring = BuildObjectIdentity(authoring),
                gridAsset = BuildObjectIdentity(grid),
                gridWidth = grid.Width,
                gridHeight = grid.Height,
                gridCellSize = grid.CellSize,
                gridOrigin = grid.Origin,
                gridCellCount = gridCellCount,
                blockedCellCount = grid.BlockedCells?.Length ?? 0,
                mapSurfaceAsset = BuildObjectIdentity(surface),
                mapSurfaceDimensions = surface.Dimensions,
                mapSurfaceCellSize = surface.CellSize,
                mapSurfaceOrigin = surface.GridOrigin,
                surfaceCount = surface.SurfaceCount,
                connectionCount = surface.ConnectionCount,
                payloadVersion = surface.PayloadVersion,
                payloadEncoding = surface.PayloadEncoding,
                compressedPayloadBytes = surface.CompressedPayloadBytes,
                uncompressedPayloadBytes = surface.UncompressedPayloadBytes,
                runtimeBlobHash = surface.ComputeRuntimeBlobHash().ToString(),
                dimensionsOriginCellSizeConsistent = true
            };
        }

        private static Dictionary<string, int> BuildNamePathCounts(Scene scene)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    string path = BuildNameHierarchyPath(transforms[i]);
                    counts[path] = counts.TryGetValue(path, out int count) ? count + 1 : 1;
                }
            }
            return counts;
        }

        private static int ValidatePlacementSourcePath(
            string sourcePath,
            IReadOnlyDictionary<string, int> scenePaths,
            string kind,
            int index)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new InvalidOperationException($"{kind} placement source path is missing at index {index}.");
            if (!scenePaths.TryGetValue(sourcePath, out int matches) || matches <= 0)
            {
                throw new InvalidOperationException(
                    $"{kind} placement source path does not resolve in Match: {sourcePath}");
            }

            return matches;
        }

        internal static int ComparePlacementEntries(
            PlacementEntryReport left,
            PlacementEntryReport right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (left == null)
                return -1;
            if (right == null)
                return 1;

            int comparison = string.CompareOrdinal(left.sourcePath, right.sourcePath);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.category, right.category);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.sourceKey, right.sourceKey);
            if (comparison != 0) return comparison;
            comparison = left.factionId.CompareTo(right.factionId);
            if (comparison != 0) return comparison;
            comparison = left.configSourcePathOccurrenceCount.CompareTo(right.configSourcePathOccurrenceCount);
            if (comparison != 0) return comparison;
            comparison = left.sceneMatchCount.CompareTo(right.sceneMatchCount);
            if (comparison != 0) return comparison;
            comparison = CompareVectorBits(left.worldCenter, right.worldCenter);
            if (comparison != 0) return comparison;
            comparison = CompareVectorBits(left.worldPosition, right.worldPosition);
            if (comparison != 0) return comparison;
            comparison = CompareVectorBits(left.worldEulerAngles, right.worldEulerAngles);
            if (comparison != 0) return comparison;
            comparison = CompareVectorBits(left.worldScale, right.worldScale);
            if (comparison != 0) return comparison;
            comparison = CompareFloatBits(left.yawDegrees, right.yawDegrees);
            if (comparison != 0) return comparison;
            comparison = left.rotateVertical.CompareTo(right.rotateVertical);
            return comparison != 0 ? comparison : CompareObjectIdentities(left.prefab, right.prefab);
        }

        internal static string BuildPlacementStableIdentity(PlacementEntryReport entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            var builder = new StringBuilder(512);
            AppendIdentityString(builder, entry.sourcePath);
            AppendIdentityString(builder, entry.category);
            AppendIdentityString(builder, entry.sourceKey);
            builder.Append(entry.factionId).Append('|')
                .Append(entry.configSourcePathOccurrenceCount).Append('|')
                .Append(entry.sceneMatchCount).Append('|');
            AppendVectorBits(builder, entry.worldCenter);
            AppendVectorBits(builder, entry.worldPosition);
            AppendVectorBits(builder, entry.worldEulerAngles);
            AppendVectorBits(builder, entry.worldScale);
            builder.Append(BitConverter.SingleToInt32Bits(entry.yawDegrees).ToString("x8", CultureInfo.InvariantCulture))
                .Append('|').Append(entry.rotateVertical ? '1' : '0').Append('|');
            AppendObjectIdentity(builder, entry.prefab);
            return builder.ToString();
        }

        private static void RequireNoDuplicatePlacementEntries(
            IReadOnlyList<PlacementEntryReport> entries,
            string kind)
        {
            for (int i = 1; i < entries.Count; i++)
            {
                if (ComparePlacementEntries(entries[i - 1], entries[i]) == 0)
                {
                    throw new InvalidOperationException(
                        $"Exact {kind} placement identity is duplicated at sorted indices {i - 1} and {i}.");
                }
            }
        }

        private static int CompareObjectIdentities(ObjectIdentityReport left, ObjectIdentityReport right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;

            int comparison = string.CompareOrdinal(left.assetGuid, right.assetGuid);
            if (comparison != 0) return comparison;
            comparison = left.localId.CompareTo(right.localId);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.globalObjectId, right.globalObjectId);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.assetPath, right.assetPath);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.scenePath, right.scenePath);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.sceneGuid, right.sceneGuid);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.hierarchyPath, right.hierarchyPath);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.type, right.type);
            return comparison != 0 ? comparison : string.CompareOrdinal(left.name, right.name);
        }

        private static int CompareVectorBits(Vector3 left, Vector3 right)
        {
            int comparison = CompareFloatBits(left.x, right.x);
            if (comparison != 0) return comparison;
            comparison = CompareFloatBits(left.y, right.y);
            return comparison != 0 ? comparison : CompareFloatBits(left.z, right.z);
        }

        private static int CompareFloatBits(float left, float right)
        {
            int comparison = left.CompareTo(right);
            return comparison != 0
                ? comparison
                : BitConverter.SingleToInt32Bits(left).CompareTo(BitConverter.SingleToInt32Bits(right));
        }

        private static void AppendIdentityString(StringBuilder builder, string value)
        {
            string normalized = value ?? string.Empty;
            builder.Append(normalized.Length).Append(':').Append(normalized).Append('|');
        }

        private static void AppendVectorBits(StringBuilder builder, Vector3 value)
        {
            builder.Append(BitConverter.SingleToInt32Bits(value.x).ToString("x8", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(BitConverter.SingleToInt32Bits(value.y).ToString("x8", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(BitConverter.SingleToInt32Bits(value.z).ToString("x8", CultureInfo.InvariantCulture))
                .Append('|');
        }

        private static void AppendObjectIdentity(StringBuilder builder, ObjectIdentityReport identity)
        {
            if (identity == null)
            {
                builder.Append("null");
                return;
            }

            AppendIdentityString(builder, identity.assetGuid);
            builder.Append(identity.localId).Append('|');
            AppendIdentityString(builder, identity.globalObjectId);
            AppendIdentityString(builder, identity.assetPath);
            AppendIdentityString(builder, identity.scenePath);
            AppendIdentityString(builder, identity.sceneGuid);
            AppendIdentityString(builder, identity.hierarchyPath);
            AppendIdentityString(builder, identity.type);
            AppendIdentityString(builder, identity.name);
        }

        private static MatchSceneView RequireSingleMatchSceneView(Scene scene)
        {
            MatchSceneView[] views = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MatchSceneView>(true))
                .ToArray();
            if (views.Length != 1)
                throw new InvalidOperationException($"Expected one MatchSceneView; found {views.Length}.");
            return views[0];
        }

        private static Scene OpenSceneForInspection(string path)
        {
            Scene existing = SceneManager.GetSceneByPath(path);
            if (existing.IsValid() && existing.isLoaded)
                return existing;
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private static void RequireCleanLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.isDirty)
                {
                    throw new InvalidOperationException(
                        $"Refusing to probe while a loaded scene has unsaved changes: {scene.path}");
                }
            }
        }

        private static void RestoreSceneSetup(SceneSetup[] previousSetup)
        {
            if (previousSetup != null && previousSetup.Any(entry => !string.IsNullOrWhiteSpace(entry.path)))
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static string ComputeFileSha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 algorithm = SHA256.Create();
            return ToLowerHex(algorithm.ComputeHash(stream));
        }

        private static string ToLowerHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static string GetGlobalObjectId(Object target)
        {
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(target);
            return id.identifierType == 0 ? string.Empty : id.ToString();
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

        private static string RequireProjectRoot()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Unable to resolve the Unity project root.");
            return Path.GetFullPath(root);
        }

        private static void RequireAsset(string path)
        {
            if (string.IsNullOrWhiteSpace(AssetDatabase.AssetPathToGUID(path)))
                throw new FileNotFoundException("Required Unity asset is missing.", path);
        }

        private static void RequireFile(string projectRoot, string assetPath)
        {
            string path = ResolveProjectPath(projectRoot, assetPath);
            if (!File.Exists(path))
                throw new FileNotFoundException("Required project file is missing.", path);
        }

        private static string ResolveProjectPath(string projectRoot, string assetPath)
        {
            string root = Path.GetFullPath(projectRoot);
            string fullPath = Path.GetFullPath(Path.Combine(root, assetPath));
            if (!IsSameOrChildPath(fullPath, root))
                throw new InvalidOperationException($"Project-relative path escaped the project root: {assetPath}");
            return fullPath;
        }

        private static string ToAssetPath(string projectRoot, string physicalPath)
        {
            string root = Path.GetFullPath(projectRoot);
            string fullPath = Path.GetFullPath(physicalPath);
            if (!IsSameOrChildPath(fullPath, root))
                throw new InvalidOperationException($"Physical path is outside the project: {physicalPath}");
            return NormalizeSeparators(fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar));
        }

        private static bool IsSameOrChildPath(string candidate, string root)
        {
            string fullCandidate = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar);
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            StringComparison comparison =
                Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.OSXEditor
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
            return string.Equals(fullCandidate, fullRoot, comparison) ||
                   fullCandidate.StartsWith(fullRoot + Path.DirectorySeparatorChar, comparison);
        }

        private static string NormalizeSeparators(string path)
        {
            return path?.Replace('\\', '/') ?? string.Empty;
        }

        private static void RequireUniqueCompletePaths(IEnumerable<string> paths, string label)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int index = 0;
            foreach (string path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new InvalidOperationException($"{label} path is missing at index {index}.");
                if (!seen.Add(path))
                    throw new InvalidOperationException($"{label} path is duplicated: {path}");
                index++;
            }
        }

        private static void RequireSetEqual(
            IEnumerable<string> left,
            IEnumerable<string> right,
            string leftLabel,
            string rightLabel)
        {
            string[] leftSet = left.OrderBy(path => path, StringComparer.Ordinal).ToArray();
            string[] rightSet = right.OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (!leftSet.SequenceEqual(rightSet, StringComparer.Ordinal))
            {
                string missing = string.Join(", ", leftSet.Except(rightSet, StringComparer.Ordinal).Take(10));
                string stale = string.Join(", ", rightSet.Except(leftSet, StringComparer.Ordinal).Take(10));
                throw new InvalidOperationException(
                    $"Scene file-set mismatch between {leftLabel} and {rightLabel}; " +
                    $"missing=[{missing}] stale=[{stale}].");
            }
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (string.IsNullOrWhiteSpace(expected) ||
                !string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{label} mismatch; expected='{expected ?? "<null>"}' actual='{actual ?? "<null>"}'.");
            }
        }

        [Serializable]
        internal readonly struct HashInput
        {
            public readonly string path;
            public readonly string sha256;

            public HashInput(string path, string sha256)
            {
                this.path = path;
                this.sha256 = sha256;
            }
        }

        [Serializable]
        internal sealed class BaselineReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public string reportPath;
            public ProjectReport project;
            public List<SceneSetupReport> sceneSetupBeforeProbe;
            public List<SceneReport> scenes;
            public SubSceneReferenceReport subSceneReference;
            public List<SerializedObjectReferenceFieldReport> matchSceneViewReferences;
            public ManifestReport manifest;
            public GeneratedOutputsReport generatedOutputs;
            public List<BuildSettingsSceneReport> buildSettingsScenes;
            public PlacementReport buildingPlacements;
            public PlacementReport vehiclePlacements;
            public MapDataReport mapData;
        }

        [Serializable]
        internal sealed class ProjectReport
        {
            public string unityVersion;
            public string projectName;
            public string projectRoot;
            public string productName;
            public string productGuid;
            public string companyName;
            public string applicationIdentifier;
        }

        [Serializable]
        internal sealed class SceneSetupReport
        {
            public int setupIndex;
            public string path;
            public string guid;
            public bool isLoaded;
            public bool isActive;
            public bool isSubScene;
        }

        [Serializable]
        internal sealed class SceneReport
        {
            public string path;
            public string guid;
            public string loadMode;
            public bool loadedDuringInspection;
            public bool autoLoadKnown;
            public bool autoLoad;
            public int rootObjectCount;
            public int hierarchyObjectCount;
            public string hierarchySha256;
            public List<RootObjectReport> rootObjects;
        }

        [Serializable]
        internal sealed class RootObjectReport
        {
            public string name;
            public int siblingIndex;
            public string hierarchyPath;
            public bool activeSelf;
            public int directChildCount;
            public int hierarchyObjectCount;
            public string hierarchySha256;
            public List<string> rootComponentTypes;
        }

        [Serializable]
        internal sealed class SubSceneReferenceReport
        {
            public string componentHierarchyPath;
            public string componentGlobalObjectId;
            public string sceneAssetPath;
            public string sceneAssetGuid;
            public bool autoLoad;
        }

        [Serializable]
        internal sealed class SerializedObjectReferenceFieldReport
        {
            public string propertyName;
            public string declaredType;
            public bool isCollection;
            public int elementCount;
            public List<ObjectIdentityReport> targets;
        }

        [Serializable]
        internal sealed class ObjectIdentityReport
        {
            public string name;
            public string type;
            public string assetPath;
            public string assetGuid;
            public long localId;
            public string scenePath;
            public string sceneGuid;
            public string hierarchyPath;
            public string globalObjectId;
        }

        [Serializable]
        internal sealed class ManifestReport
        {
            public ObjectIdentityReport asset;
            public int schemaVersion;
            public string operationMapId;
            public string canonicalScenePath;
            public string canonicalSceneGuid;
            public string canonicalSceneDependencyHash;
            public string computedCanonicalSceneDependencyHash;
            public float chunkSize;
            public string contentHash;
            public string computedContentHash;
            public int chunkCount;
            public int sourceCount;
            public string fileSha256;
            public string metaSha256;
        }

        [Serializable]
        private sealed class IntegrityDocument
        {
            public int schemaVersion;
            public string contentHash;
            public IntegrityEntry[] scenes;
        }

        [Serializable]
        private sealed class IntegrityEntry
        {
            public string scenePath;
            public string fileHash;
            public string metaHash;
        }

        [Serializable]
        internal sealed class GeneratedOutputsReport
        {
            public string integrityPath;
            public int integritySchemaVersion;
            public string integrityContentHash;
            public string integrityFileSha256;
            public string integrityMetaSha256;
            public int manifestSceneCount;
            public int ledgerSceneCount;
            public int diskSceneCount;
            public int diskMetaCount;
            public bool exactFileSetParity;
            public string aggregateAlgorithm;
            public string sceneFilesAggregateSha256;
            public string sceneMetasAggregateSha256;
            public string combinedAggregateSha256;
            public List<GeneratedSceneFileReport> files;
        }

        [Serializable]
        internal sealed class GeneratedSceneFileReport
        {
            public string scenePath;
            public string sceneGuid;
            public string sceneSha256;
            public string metaPath;
            public string metaSha256;
        }

        [Serializable]
        internal sealed class BuildSettingsSceneReport
        {
            public int buildSettingsIndex;
            public string path;
            public string guid;
            public bool enabled;
        }

        [Serializable]
        internal sealed class PlacementReport
        {
            public string kind;
            public ObjectIdentityReport config;
            public bool spawnOnMatchStart;
            public bool hideAuthoringVisualsAfterSpawn;
            public ObjectIdentityReport authoringRoot;
            public int count;
            public string identityPathAggregateSha256;
            public List<PlacementEntryReport> entries;
        }

        [Serializable]
        internal sealed class PlacementEntryReport
        {
            public string sourcePath;
            public string category;
            public string sourceKey;
            public byte factionId;
            public int configSourcePathOccurrenceCount;
            public int sceneMatchCount;
            public Vector3 worldCenter;
            public Vector3 worldPosition;
            public Vector3 worldEulerAngles;
            public Vector3 worldScale;
            public float yawDegrees;
            public bool rotateVertical;
            public ObjectIdentityReport prefab;
        }

        private sealed class PlacementEntryReportComparer : IComparer<PlacementEntryReport>
        {
            internal static readonly PlacementEntryReportComparer Instance = new();

            public int Compare(PlacementEntryReport left, PlacementEntryReport right)
            {
                return ComparePlacementEntries(left, right);
            }
        }

        [Serializable]
        internal sealed class MapDataReport
        {
            public ObjectIdentityReport mapSurfaceAuthoring;
            public ObjectIdentityReport gridAsset;
            public int gridWidth;
            public int gridHeight;
            public float gridCellSize;
            public Vector3 gridOrigin;
            public long gridCellCount;
            public int blockedCellCount;
            public ObjectIdentityReport mapSurfaceAsset;
            public Vector2Int mapSurfaceDimensions;
            public float mapSurfaceCellSize;
            public Vector3 mapSurfaceOrigin;
            public int surfaceCount;
            public int connectionCount;
            public int payloadVersion;
            public byte payloadEncoding;
            public int compressedPayloadBytes;
            public int uncompressedPayloadBytes;
            public string runtimeBlobHash;
            public bool dimensionsOriginCellSizeConsistent;
        }
    }
}

#endif
