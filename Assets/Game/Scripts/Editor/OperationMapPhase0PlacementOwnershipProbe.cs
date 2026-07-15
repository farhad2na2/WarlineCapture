#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Game.Composition;
    using Game.Configs;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class OperationMapPhase0PlacementOwnershipProbe
    {
        internal const string ReportSchema = "warline.operation-map.phase0-placement-ownership";
        internal const int ReportSchemaVersion = 1;
        internal const string BaselineCommit = "98cfe8cedb3c7d18a14819759bb0d5e51c202264";
        internal const string ReportPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_PHASE0_PLACEMENT_OWNERSHIP_REPORT_PATH";
        internal const string DefaultReportPath =
            "/private/tmp/warline-operation-map-phase0-placement-ownership.json";

        private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
        private const string MatchSceneGuid = "cc4f48a57793d4597b4ffac2906c515e";
        private const string BuildingAssetPath =
            "Assets/Game/Configs/Scene/Match_MapBuildingPlacement_Config.asset";
        private const string BuildingAssetGuid = "e859aa1a53b0942609e537713fd55fb7";
        private const string VehicleAssetPath =
            "Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset";
        private const string VehicleAssetGuid = "03d5c67074cde47488712cef0e5f494a";
        private const string BaselineEvidencePath =
            "Design/AgentReports/2026-07-14_opmap-002_phase0_baseline_probe.md";
        private const string TrackerPath =
            "Design/Architecture/operation_map_scene_split_and_generator_tracker.md";
        private const string TargetOwner = "Operation map definition";
        private const string CurrentOwner =
            "Match scene compatibility binding through Game.Composition.MatchSceneView";
        private const string MigrationDisposition =
            "Move config and authoring hierarchy together; preserve the config .meta GUID; bind through the operation map definition; prove placement identity parity before removing the MatchSceneView compatibility fields.";
        private const string DecisionOwner =
            "Operation map architecture owner and gameplay placement owner";
        private const string DecisionState = "NeedsDecision";
        private const int ExpectedBuildingCount = 451;
        private const int ExpectedVehicleCount = 29;
        private const string ExpectedGeneratedAggregate =
            "574afec991fbc1a684531c9f727c20eb296271260e7a4e1c4a8c300a2b642e79";

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        public static void Run()
        {
            string projectRoot = RequireProjectRoot();
            string outputPath = ResolveReportOutputPath(
                projectRoot,
                Environment.GetEnvironmentVariable(ReportPathEnvironmentVariable));
            InvalidateOutput(outputPath);
            List<InputHashReport> beforeHashes = HashDirectInputs(projectRoot);
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            RequireSupportedSceneSetup(previousSetup);
            PlacementOwnershipReport report;

            try
            {
                RequireCleanLoadedScenes();
                RequireExpectedRuntimeConsumers(projectRoot);
                Scene matchScene = OpenSceneForInspection(MatchScenePath);
                MatchSceneView view = RequireSingleMatchSceneView(matchScene);
                Dictionary<string, List<string>> scenePaths = BuildScenePathIndex(matchScene);
                BaselineReferenceReport baseline = LoadBaselineReference(projectRoot);
                List<RuntimeConsumerReport> consumers = BuildRuntimeConsumers();
                List<PlacementConfigReport> placements = new()
                {
                    BuildBuildingReport(view, scenePaths),
                    BuildVehicleReport(view, scenePaths)
                };
                placements.Sort((left, right) => string.CompareOrdinal(left.kind, right.kind));
                List<InputHashReport> afterHashes = HashDirectInputs(projectRoot);
                RequireInputHashesEqual(beforeHashes, afterHashes);
                report = BuildReport(baseline, beforeHashes, consumers, placements);
            }
            finally
            {
                RestoreSceneSetup(previousSetup);
                DeleteIfPresent(outputPath + ".tmp");
            }

            PublishReportAtomically(outputPath, JsonUtility.ToJson(report, true) + "\n");
            Debug.Log(
                $"[OperationMapPhase0PlacementOwnershipProbe] result={report.result} " +
                $"buildings={report.counts.buildingPlacements} " +
                $"vehicles={report.counts.vehiclePlacements} " +
                $"needsDecision={report.counts.needsDecision} report={outputPath}");
        }

        internal static string ResolveReportOutputPath(string projectRoot, string configuredPath)
        {
            string path = string.IsNullOrWhiteSpace(configuredPath)
                ? DefaultReportPath
                : configuredPath;
            return OperationMapPhase0BaselineProbe.ResolveReportOutputPath(projectRoot, path);
        }

        internal static PlacementOwnershipReport BuildReport(
            BaselineReferenceReport baseline,
            List<InputHashReport> directInputHashes,
            List<RuntimeConsumerReport> runtimeConsumers,
            List<PlacementConfigReport> placements)
        {
            List<PlacementConfigReport> orderedPlacements = placements
                .OrderBy(entry => entry.kind, StringComparer.Ordinal).ToList();
            List<RuntimeConsumerReport> orderedConsumers = runtimeConsumers
                .OrderBy(entry => entry.path, StringComparer.Ordinal).ToList();
            List<OwnershipDecisionReport> decisions = BuildDecisions();
            var report = new PlacementOwnershipReport
            {
                reportSchema = ReportSchema,
                reportSchemaVersion = ReportSchemaVersion,
                baselineCommit = BaselineCommit,
                result = DecisionState,
                counts = BuildCounts(orderedPlacements, orderedConsumers, decisions),
                opmap002Baseline = baseline,
                directInputHashes = directInputHashes
                    .OrderBy(entry => entry.path, StringComparer.Ordinal).ToList(),
                runtimeConsumers = orderedConsumers,
                placementConfigs = orderedPlacements,
                decisions = decisions
            };

            ValidateBaselineReference(report.opmap002Baseline);
            ValidateInputHashes(report.directInputHashes);
            if (!HasExpectedCounts(report.counts) || !HasConsistentCounts(report))
                throw new InvalidOperationException("Placement ownership count schema validation failed.");
            if (!HasExpectedRuntimeConsumers(report.runtimeConsumers))
                throw new InvalidOperationException("Placement ownership runtime-consumer schema validation failed.");
            if (!HasExpectedPlacementReport(report.placementConfigs[0], BuildingSpec()))
                throw new InvalidOperationException("Building placement schema validation failed.");
            if (!HasExpectedPlacementReport(report.placementConfigs[1], VehicleSpec()))
                throw new InvalidOperationException("Vehicle placement schema validation failed.");
            if (!HasExpectedDecisions(report.decisions))
                throw new InvalidOperationException("Placement ownership decision schema validation failed.");
            if (!HasRequiredReportShape(JsonUtility.ToJson(report, true)))
                throw new InvalidOperationException("Placement ownership report failed schema validation.");
            return report;
        }

        internal static bool HasRequiredReportShape(string json)
        {
            if (string.IsNullOrWhiteSpace(json) ||
                json.Contains("projectRoot", StringComparison.Ordinal) ||
                json.Contains("unityVersion", StringComparison.Ordinal) ||
                json.Contains("outputPath", StringComparison.Ordinal) ||
                json.Contains("generatedFiles", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                PlacementOwnershipReport report = JsonUtility.FromJson<PlacementOwnershipReport>(json);
                if (report == null ||
                    !string.Equals(report.reportSchema, ReportSchema, StringComparison.Ordinal) ||
                    report.reportSchemaVersion != ReportSchemaVersion ||
                    !string.Equals(report.baselineCommit, BaselineCommit, StringComparison.Ordinal) ||
                    !string.Equals(report.result, DecisionState, StringComparison.Ordinal) ||
                    !HasExpectedCounts(report.counts) || !HasConsistentCounts(report))
                {
                    return false;
                }

                ValidateBaselineReference(report.opmap002Baseline);
                ValidateInputHashes(report.directInputHashes);
                if (!HasExpectedRuntimeConsumers(report.runtimeConsumers) ||
                    report.placementConfigs == null || report.placementConfigs.Count != 2 ||
                    !IsStrictlyOrdered(report.placementConfigs.Select(entry => entry.kind)) ||
                    !HasExpectedPlacementReport(report.placementConfigs[0], BuildingSpec()) ||
                    !HasExpectedPlacementReport(report.placementConfigs[1], VehicleSpec()) ||
                    !HasExpectedDecisions(report.decisions))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static void PublishReportAtomically(string outputPath, string json)
        {
            InvalidateOutput(outputPath);
            if (!HasRequiredReportShape(json))
                throw new InvalidOperationException("Refusing to publish invalid placement ownership evidence.");

            string temporaryPath = outputPath + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, json, Utf8WithoutBom);
                string persisted = File.ReadAllText(temporaryPath, Utf8WithoutBom);
                if (!string.Equals(persisted, json, StringComparison.Ordinal) ||
                    !HasRequiredReportShape(persisted))
                {
                    throw new InvalidOperationException("Persisted placement ownership evidence is invalid.");
                }
                File.Move(temporaryPath, outputPath);
            }
            finally
            {
                DeleteIfPresent(temporaryPath);
            }
        }

        internal static void InvalidateOutput(string outputPath)
        {
            DeleteIfPresent(outputPath);
            DeleteIfPresent(outputPath + ".tmp");
        }

        private static PlacementConfigReport BuildBuildingReport(
            MatchSceneView view,
            IReadOnlyDictionary<string, List<string>> scenePaths)
        {
            MapBuildingPlacementConfig config = view.MapBuildingPlacementConfig;
            Transform root = view.MapBuildingAuthoringRoot;
            PlacementSpec spec = BuildingSpec();
            RequireExactBinding(view, spec, config, root);
            if (config.Placements == null || config.Placements.Count != spec.count)
                throw new InvalidOperationException("Building placement count drifted.");

            Dictionary<string, int> pathCounts = config.Placements
                .GroupBy(entry => entry?.SourcePath ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var entries = new List<PlacementEntryReport>(config.Placements.Count);
            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapBuildingPlacementConfigEntry entry = config.Placements[i];
                if (entry == null || entry.BuildingPrefab == null)
                    throw new InvalidOperationException($"Building placement identity is missing at index {i}.");
                entries.Add(BuildPlacementEntry(
                    entry.SourcePath,
                    entry.Category,
                    string.Empty,
                    entry.FactionId,
                    pathCounts[entry.SourcePath],
                    scenePaths,
                    entry.WorldCenter,
                    entry.WorldPosition,
                    entry.WorldEulerAngles,
                    entry.WorldScale,
                    entry.YawDegrees,
                    entry.RotateVertical,
                    entry.BuildingPrefab));
            }
            return BuildPlacementConfigReport(
                spec,
                config.SpawnOnMatchStart,
                config.HideAuthoringVisualsAfterSpawn,
                entries,
                config,
                root,
                view);
        }

        private static PlacementConfigReport BuildVehicleReport(
            MatchSceneView view,
            IReadOnlyDictionary<string, List<string>> scenePaths)
        {
            MapVehiclePlacementConfig config = view.MapVehiclePlacementConfig;
            Transform root = view.MapVehicleAuthoringRoot;
            PlacementSpec spec = VehicleSpec();
            RequireExactBinding(view, spec, config, root);
            if (config.Placements == null || config.Placements.Count != spec.count)
                throw new InvalidOperationException("Vehicle placement count drifted.");

            Dictionary<string, int> pathCounts = config.Placements
                .GroupBy(entry => entry?.SourcePath ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var entries = new List<PlacementEntryReport>(config.Placements.Count);
            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapVehiclePlacementConfigEntry entry = config.Placements[i];
                if (entry == null || entry.VehiclePrefab == null ||
                    string.IsNullOrWhiteSpace(entry.VehicleSourceKey))
                    throw new InvalidOperationException($"Vehicle placement identity is missing at index {i}.");
                entries.Add(BuildPlacementEntry(
                    entry.SourcePath,
                    entry.Category,
                    entry.VehicleSourceKey,
                    entry.FactionId,
                    pathCounts[entry.SourcePath],
                    scenePaths,
                    entry.WorldCenter,
                    entry.WorldPosition,
                    entry.WorldEulerAngles,
                    entry.WorldScale,
                    0f,
                    false,
                    entry.VehiclePrefab));
            }
            return BuildPlacementConfigReport(
                spec,
                config.SpawnOnMatchStart,
                config.HideAuthoringVisualsAfterSpawn,
                entries,
                config,
                root,
                view);
        }

        private static PlacementEntryReport BuildPlacementEntry(
            string sourcePath,
            string category,
            string sourceKey,
            byte factionId,
            int sourceOccurrenceCount,
            IReadOnlyDictionary<string, List<string>> scenePaths,
            Vector3 worldCenter,
            Vector3 worldPosition,
            Vector3 worldEulerAngles,
            Vector3 worldScale,
            float yawDegrees,
            bool rotateVertical,
            GameObject prefab)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) ||
                !scenePaths.TryGetValue(sourcePath, out List<string> hierarchyPaths) ||
                hierarchyPaths.Count == 0)
            {
                throw new InvalidOperationException($"Placement source path does not resolve in Match: {sourcePath}");
            }

            var report = new PlacementEntryReport
            {
                sourcePath = sourcePath,
                category = category,
                sourceKey = sourceKey ?? string.Empty,
                factionId = factionId,
                configSourcePathOccurrenceCount = sourceOccurrenceCount,
                sceneMatchCount = hierarchyPaths.Count,
                hierarchyPaths = hierarchyPaths.OrderBy(path => path, StringComparer.Ordinal).ToList(),
                worldCenter = worldCenter,
                worldPosition = worldPosition,
                worldEulerAngles = worldEulerAngles,
                worldScale = worldScale,
                yawDegrees = yawDegrees,
                rotateVertical = rotateVertical,
                prefab = BuildObjectIdentity(prefab)
            };
            report.stableIdentitySha256 = ComputeSha256(BuildPlacementStableIdentity(report));
            return report;
        }

        private static PlacementConfigReport BuildPlacementConfigReport(
            PlacementSpec spec,
            bool spawnOnMatchStart,
            bool hideAuthoringVisualsAfterSpawn,
            List<PlacementEntryReport> entries,
            UnityEngine.Object config,
            Transform root,
            MatchSceneView view)
        {
            entries.Sort(PlacementEntryComparer.Instance);
            for (int i = 1; i < entries.Count; i++)
            {
                if (ComparePlacementEntries(entries[i - 1], entries[i]) >= 0)
                    throw new InvalidOperationException($"Duplicate {spec.kind} placement identity.");
            }

            return new PlacementConfigReport
            {
                kind = spec.kind,
                asset = BuildObjectIdentity(config),
                binding = new MatchSceneViewBindingReport
                {
                    scenePath = MatchScenePath,
                    sceneGuid = MatchSceneGuid,
                    matchSceneView = BuildObjectIdentity(view),
                    configField = spec.configField,
                    authoringRootField = spec.rootField,
                    authoringRoot = BuildObjectIdentity(root)
                },
                spawnOnMatchStart = spawnOnMatchStart,
                hideAuthoringVisualsAfterSpawn = hideAuthoringVisualsAfterSpawn,
                count = entries.Count,
                factionCounts = entries.GroupBy(entry => entry.factionId)
                    .OrderBy(group => group.Key)
                    .Select(group => new FactionCountReport
                    {
                        factionId = group.Key,
                        count = group.Count()
                    }).ToList(),
                identityAggregateSha256 = ComputePlacementAggregate(entries),
                entries = entries
            };
        }

        private static void RequireExactBinding(
            MatchSceneView view,
            PlacementSpec spec,
            UnityEngine.Object config,
            Transform root)
        {
            if (view == null || config == null || root == null)
                throw new InvalidOperationException($"{spec.kind} MatchSceneView binding is incomplete.");
            var serialized = new SerializedObject(view);
            if (serialized.FindProperty(spec.configField)?.objectReferenceValue != config ||
                serialized.FindProperty(spec.rootField)?.objectReferenceValue != root)
            {
                throw new InvalidOperationException($"{spec.kind} MatchSceneView serialized binding drifted.");
            }
            ObjectIdentityReport configIdentity = BuildObjectIdentity(config);
            ObjectIdentityReport rootIdentity = BuildObjectIdentity(root);
            if (!string.Equals(configIdentity.assetPath, spec.assetPath, StringComparison.Ordinal) ||
                !string.Equals(configIdentity.assetGuid, spec.assetGuid, StringComparison.Ordinal) ||
                configIdentity.localId != 11400000 ||
                !string.Equals(rootIdentity.hierarchyPath, spec.rootHierarchyPath, StringComparison.Ordinal) ||
                rootIdentity.localId != spec.rootLocalId)
            {
                throw new InvalidOperationException(
                    $"{spec.kind} exact binding identity drifted: " +
                    $"config={configIdentity.assetPath}|{configIdentity.assetGuid}|{configIdentity.localId}; " +
                    $"root={rootIdentity.hierarchyPath}|{rootIdentity.localId}|{rootIdentity.globalObjectId}.");
            }
        }

        private static bool HasExpectedPlacementReport(PlacementConfigReport report, PlacementSpec spec)
        {
            if (report == null || !string.Equals(report.kind, spec.kind, StringComparison.Ordinal) ||
                !HasExactAssetIdentity(report.asset, spec.assetPath, spec.assetGuid, spec.configType) ||
                report.binding == null ||
                !string.Equals(report.binding.scenePath, MatchScenePath, StringComparison.Ordinal) ||
                !string.Equals(report.binding.sceneGuid, MatchSceneGuid, StringComparison.Ordinal) ||
                !HasExactSceneIdentity(
                    report.binding.matchSceneView,
                    "Bootstrap[0]",
                    300000004,
                    typeof(MatchSceneView).FullName) ||
                !string.Equals(report.binding.configField, spec.configField, StringComparison.Ordinal) ||
                !string.Equals(report.binding.authoringRootField, spec.rootField, StringComparison.Ordinal) ||
                !HasExactSceneIdentity(
                    report.binding.authoringRoot,
                    spec.rootHierarchyPath,
                    spec.rootLocalId,
                    typeof(Transform).FullName) ||
                !report.spawnOnMatchStart || !report.hideAuthoringVisualsAfterSpawn ||
                report.count != spec.count || report.entries == null || report.entries.Count != spec.count ||
                !HasFactionCounts(report.factionCounts, spec.faction0Count, spec.faction1Count))
            {
                return false;
            }

            Dictionary<string, int> sourcePathCounts = report.entries
                .GroupBy(entry => entry?.sourcePath ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            Dictionary<int, int> factionCounts = report.entries
                .Where(entry => entry != null)
                .GroupBy(entry => entry.factionId)
                .ToDictionary(group => group.Key, group => group.Count());
            if (report.factionCounts.Sum(entry => entry.count) != report.count ||
                factionCounts.Count != report.factionCounts.Count ||
                report.factionCounts.Any(entry =>
                    !factionCounts.TryGetValue(entry.factionId, out int count) || count != entry.count))
            {
                return false;
            }

            PlacementEntryReport previous = null;
            for (int i = 0; i < report.entries.Count; i++)
            {
                PlacementEntryReport entry = report.entries[i];
                if (!IsCompletePlacementEntry(entry) ||
                    !sourcePathCounts.TryGetValue(entry.sourcePath, out int occurrenceCount) ||
                    entry.configSourcePathOccurrenceCount != occurrenceCount ||
                    (previous != null && ComparePlacementEntries(previous, entry) >= 0) ||
                    !string.Equals(
                        entry.stableIdentitySha256,
                        ComputeSha256(BuildPlacementStableIdentity(entry)),
                        StringComparison.Ordinal))
                {
                    return false;
                }
                previous = entry;
            }

            return string.Equals(
                report.identityAggregateSha256,
                ComputePlacementAggregate(report.entries),
                StringComparison.Ordinal);
        }

        private static bool IsCompletePlacementEntry(PlacementEntryReport entry)
        {
            return entry != null &&
                   !string.IsNullOrWhiteSpace(entry.sourcePath) &&
                   !string.IsNullOrWhiteSpace(entry.category) &&
                   entry.configSourcePathOccurrenceCount > 0 && entry.sceneMatchCount > 0 &&
                   entry.hierarchyPaths != null && entry.hierarchyPaths.Count == entry.sceneMatchCount &&
                   IsStrictlyOrdered(entry.hierarchyPaths) &&
                   entry.hierarchyPaths.All(path => path.StartsWith("Map[10]/", StringComparison.Ordinal)) &&
                   IsFinite(entry.worldCenter) && IsFinite(entry.worldPosition) &&
                   IsFinite(entry.worldEulerAngles) && IsFinite(entry.worldScale) &&
                   IsFinite(entry.yawDegrees) && IsSha256(entry.stableIdentitySha256) &&
                   entry.prefab != null && !string.IsNullOrWhiteSpace(entry.prefab.assetPath) &&
                   !string.IsNullOrWhiteSpace(entry.prefab.assetGuid) && entry.prefab.localId != 0 &&
                   string.Equals(entry.prefab.type, typeof(GameObject).FullName, StringComparison.Ordinal);
        }

        internal static int ComparePlacementEntries(PlacementEntryReport left, PlacementEntryReport right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
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
            comparison = CompareStringLists(left.hierarchyPaths, right.hierarchyPaths);
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
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.prefab.assetGuid, right.prefab.assetGuid);
            return comparison != 0 ? comparison : left.prefab.localId.CompareTo(right.prefab.localId);
        }

        internal static string BuildPlacementStableIdentity(PlacementEntryReport entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            var builder = new StringBuilder(640);
            AppendIdentity(builder, entry.sourcePath);
            AppendIdentity(builder, entry.category);
            AppendIdentity(builder, entry.sourceKey);
            builder.Append(entry.factionId).Append('|')
                .Append(entry.configSourcePathOccurrenceCount).Append('|')
                .Append(entry.sceneMatchCount).Append('|');
            foreach (string path in entry.hierarchyPaths)
                AppendIdentity(builder, path);
            AppendVectorBits(builder, entry.worldCenter);
            AppendVectorBits(builder, entry.worldPosition);
            AppendVectorBits(builder, entry.worldEulerAngles);
            AppendVectorBits(builder, entry.worldScale);
            builder.Append(CanonicalFloatBits(entry.yawDegrees).ToString("x8", CultureInfo.InvariantCulture))
                .Append('|').Append(entry.rotateVertical ? '1' : '0').Append('|');
            AppendIdentity(builder, entry.prefab.assetPath);
            AppendIdentity(builder, entry.prefab.assetGuid);
            builder.Append(entry.prefab.localId).Append('|');
            AppendIdentity(builder, entry.prefab.type);
            return builder.ToString();
        }

        private static string ComputePlacementAggregate(IReadOnlyList<PlacementEntryReport> entries)
        {
            var builder = new StringBuilder(entries.Count * 80);
            for (int i = 0; i < entries.Count; i++)
            {
                string identity = BuildPlacementStableIdentity(entries[i]);
                builder.Append(identity.Length).Append(':').Append(identity).Append('\n');
            }
            return ComputeSha256(builder.ToString());
        }

        private static Dictionary<string, List<string>> BuildScenePathIndex(Scene scene)
        {
            var paths = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    string namePath = BuildNameHierarchyPath(transform);
                    if (!paths.TryGetValue(namePath, out List<string> matches))
                    {
                        matches = new List<string>();
                        paths.Add(namePath, matches);
                    }
                    matches.Add(BuildIndexedHierarchyPath(transform));
                }
            }
            foreach (List<string> matches in paths.Values)
                matches.Sort(StringComparer.Ordinal);
            return paths;
        }

        private static ObjectIdentityReport BuildObjectIdentity(UnityEngine.Object target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            string assetPath = AssetDatabase.GetAssetPath(target) ?? string.Empty;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out string assetGuid, out long localId);
            GameObject gameObject = target as GameObject;
            if (target is Component component)
                gameObject = component.gameObject;
            string scenePath = gameObject != null ? gameObject.scene.path ?? string.Empty : string.Empty;
            GlobalObjectId globalId = GlobalObjectId.GetGlobalObjectIdSlow(target);
            string globalObjectId = globalId.identifierType == 0 ? string.Empty : globalId.ToString();
            if (localId == 0 && !string.IsNullOrEmpty(scenePath))
            {
                Match sceneObjectId = Regex.Match(
                    globalObjectId,
                    @"^GlobalObjectId_V1-[0-9]+-[0-9a-f]{32}-(?<localId>[0-9]+)-[0-9]+$",
                    RegexOptions.CultureInvariant);
                if (sceneObjectId.Success)
                    localId = long.Parse(sceneObjectId.Groups["localId"].Value, CultureInfo.InvariantCulture);
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
                hierarchyPath = gameObject != null ? BuildIndexedHierarchyPath(gameObject.transform) : string.Empty,
                globalObjectId = globalObjectId
            };
        }

        private static BaselineReferenceReport LoadBaselineReference(string projectRoot)
        {
            string path = Path.Combine(projectRoot, BaselineEvidencePath);
            string evidence = File.ReadAllText(path, Utf8WithoutBom);
            Match counts = Regex.Match(
                evidence,
                @"Placement counts are `(?<buildings>[0-9]+)` building and `(?<vehicles>[0-9]+)` vehicle",
                RegexOptions.CultureInvariant);
            Match aggregate = Regex.Match(
                evidence,
                @"combined scene/meta aggregate SHA-256 `(?<hash>[0-9a-f]{64})`",
                RegexOptions.CultureInvariant);
            if (!counts.Success || !aggregate.Success)
                throw new InvalidOperationException("opmap-002 placement evidence is incomplete.");
            var report = new BaselineReferenceReport
            {
                reportSchema = OperationMapPhase0BaselineProbe.ReportSchema,
                reportSchemaVersion = OperationMapPhase0BaselineProbe.ReportSchemaVersion,
                result = "Passed",
                evidencePath = BaselineEvidencePath,
                evidenceSha256 = ComputeSha256(File.ReadAllBytes(path)),
                buildingPlacementCount = int.Parse(counts.Groups["buildings"].Value, CultureInfo.InvariantCulture),
                vehiclePlacementCount = int.Parse(counts.Groups["vehicles"].Value, CultureInfo.InvariantCulture),
                generatedCombinedAggregateSha256 = aggregate.Groups["hash"].Value
            };
            ValidateBaselineReference(report);
            return report;
        }

        private static void ValidateBaselineReference(BaselineReferenceReport report)
        {
            if (report == null ||
                !string.Equals(report.reportSchema, OperationMapPhase0BaselineProbe.ReportSchema, StringComparison.Ordinal) ||
                report.reportSchemaVersion != OperationMapPhase0BaselineProbe.ReportSchemaVersion ||
                !string.Equals(report.result, "Passed", StringComparison.Ordinal) ||
                !string.Equals(report.evidencePath, BaselineEvidencePath, StringComparison.Ordinal) ||
                !string.Equals(report.evidenceSha256, ExpectedDirectInputHashes()[BaselineEvidencePath], StringComparison.Ordinal) ||
                report.buildingPlacementCount != ExpectedBuildingCount ||
                report.vehiclePlacementCount != ExpectedVehicleCount ||
                !string.Equals(report.generatedCombinedAggregateSha256, ExpectedGeneratedAggregate, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Unsupported opmap-002 placement baseline.");
            }
        }

        private static List<InputHashReport> HashDirectInputs(string projectRoot)
        {
            return ExpectedDirectInputHashes().Keys.OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => new InputHashReport
                {
                    path = path,
                    sha256 = ComputeSha256(File.ReadAllBytes(Path.Combine(projectRoot, path)))
                }).ToList();
        }

        private static void ValidateInputHashes(IReadOnlyList<InputHashReport> hashes)
        {
            Dictionary<string, string> expected = ExpectedDirectInputHashes();
            if (hashes == null || hashes.Count != expected.Count ||
                !IsStrictlyOrdered(hashes.Select(entry => entry.path)))
                throw new InvalidOperationException("Direct input hash set is incomplete or unordered.");
            foreach (InputHashReport hash in hashes)
            {
                if (hash == null || !expected.TryGetValue(hash.path, out string expectedHash) ||
                    !string.Equals(hash.sha256, expectedHash, StringComparison.Ordinal))
                    throw new InvalidOperationException("Direct input hash drifted.");
            }
        }

        private static Dictionary<string, string> ExpectedDirectInputHashes()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [BuildingAssetPath] = "26973214f433c44ebca01f302ecbe05789c84e573dc48eb8b2c21f241823464d",
                [BuildingAssetPath + ".meta"] = "6d8bde44c602566dde36f229d334f1cb727c9b46d9ccadf5d7f785658c50f106",
                [VehicleAssetPath] = "898199006ec1e8be4916554c07f4e9635c8e35e5ca52c7b035a7e10375c9cf30",
                [VehicleAssetPath + ".meta"] = "94ce2a975f2a211b8a01b40563ec100f81ee388baa71bb327a8ee1dec9b2f9d9",
                [MatchScenePath] = "dca7c83b765ce40099ce4fd62a53cbee5bc306107f8a026abcb941a59bf53a46",
                ["Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs"] = "bd200299767aeb0a7efd3d67d6c3e451ef3dc9327fe025163138486dbd0ff689",
                ["Assets/Game/Scripts/Composition/MatchSceneView.cs"] = "74704998d2a0daff6c9cdc4dd370c8a7d00d0a828b08741a7cd5a03ca562d1a6",
                ["Assets/Game/Scripts/Configs/MapBuildingPlacementConfig.cs"] = "a1d91971d5f3baebbbf85a8140780f9dd0af9f79dfce6b098e2b6d63d15cddaf",
                ["Assets/Game/Scripts/Configs/MapVehiclePlacementConfig.cs"] = "513af70e6d5934735fa4767261be0814ae07a20c22a30abbe529583b4633fceb",
                ["Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs"] = "3eca4ad45ed5f8f7a4303d8d7b5e1c7f8af26ada57220edcc5ea0edcb382d613",
                ["Assets/Game/Scripts/Systems/ManagedGameplayStartupSystemHelper.cs"] = "4a3c2d575c54d1a4b808529fd42f4ecfedce5fee6682cfbb8ace7c22d6f8f07c",
                ["Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnPrefabSystemHelper.cs"] = "c783987d991c00b229ed704d98db8bfc54e71573506b4c5b65b6b032299947e6",
                ["Assets/Game/Scripts/Systems/MapVehiclePlacementClearanceSystemHelper.cs"] = "22d55b2424bf5b9c117ea22f338d838501a9c4ad68dc0d3005f8da2c5034c54a",
                ["Assets/Game/Scripts/Systems/MapVehiclePlacementSpawnPrefabSystemHelper.cs"] = "ab22e1db28ec08d59f1db8b0f21a6e76682688ef1d9095b390ca45e43533b323",
                [BaselineEvidencePath] = "d4d4674850766c5cd95e1bb5fbb6f26893e0bb019dbaf266a0c9897a3befc807",
                [TrackerPath] = "7621e3f1c17ac7a0d7a8945cf80c8fe1ee9c1c5b0caa180d72312d9735a414e0"
            };
        }

        private static List<RuntimeConsumerReport> BuildRuntimeConsumers()
        {
            return ExpectedRuntimeConsumerSpecs()
                .Select(pair => new RuntimeConsumerReport
                {
                    path = pair.Key,
                    consumesBuildingPlacements = pair.Value.building,
                    consumesVehiclePlacements = pair.Value.vehicle,
                    responsibility = pair.Value.responsibility
                })
                .OrderBy(entry => entry.path, StringComparer.Ordinal).ToList();
        }

        private static bool HasExpectedRuntimeConsumers(IReadOnlyList<RuntimeConsumerReport> consumers)
        {
            Dictionary<string, RuntimeConsumerSpec> expected = ExpectedRuntimeConsumerSpecs();
            if (consumers == null || consumers.Count != expected.Count ||
                !IsStrictlyOrdered(consumers.Select(entry => entry.path)))
                return false;
            foreach (RuntimeConsumerReport consumer in consumers)
            {
                if (consumer == null || !expected.TryGetValue(consumer.path, out RuntimeConsumerSpec spec) ||
                    consumer.consumesBuildingPlacements != spec.building ||
                    consumer.consumesVehiclePlacements != spec.vehicle ||
                    !string.Equals(consumer.responsibility, spec.responsibility, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static Dictionary<string, RuntimeConsumerSpec> ExpectedRuntimeConsumerSpecs()
        {
            return new Dictionary<string, RuntimeConsumerSpec>(StringComparer.Ordinal)
            {
                ["Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs"] = new(true, true, "Forwards both config and authoring-root bindings into managed gameplay startup."),
                ["Assets/Game/Scripts/Composition/MatchSceneView.cs"] = new(true, true, "Owns the current serialized compatibility bindings and exposes them to Match bootstrap."),
                ["Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs"] = new(true, true, "Constructs both placement runtime contexts and schedules their startup updates."),
                ["Assets/Game/Scripts/Systems/ManagedGameplayStartupSystemHelper.cs"] = new(true, true, "Forwards both placement contracts into building gameplay composition."),
                ["Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnPrefabSystemHelper.cs"] = new(true, false, "Resolves authored hierarchy paths, spawns building runtime instances, and hides sources/root after spawn."),
                ["Assets/Game/Scripts/Systems/MapVehiclePlacementClearanceSystemHelper.cs"] = new(false, true, "Consumes vehicle placement transforms to refresh runtime blocker clearance."),
                ["Assets/Game/Scripts/Systems/MapVehiclePlacementSpawnPrefabSystemHelper.cs"] = new(false, true, "Spawns vehicle entities from placement identities and hides the vehicle authoring root after spawn.")
            };
        }

        private static void RequireExpectedRuntimeConsumers(string projectRoot)
        {
            string scriptsRoot = Path.Combine(projectRoot, "Assets/Game/Scripts");
            string[] configDefinitionPaths =
            {
                "Assets/Game/Scripts/Configs/MapBuildingPlacementConfig.cs",
                "Assets/Game/Scripts/Configs/MapVehiclePlacementConfig.cs"
            };
            string[] discovered = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories)
                .Select(path => NormalizeSeparators(Path.GetRelativePath(projectRoot, path)))
                .Where(path => !path.StartsWith("Assets/Game/Scripts/Editor/", StringComparison.Ordinal))
                .Where(path => !configDefinitionPaths.Contains(path, StringComparer.Ordinal))
                .Where(path =>
                {
                    string source = File.ReadAllText(Path.Combine(projectRoot, path), Utf8WithoutBom);
                    return source.Contains("MapBuildingPlacementConfig", StringComparison.Ordinal) ||
                           source.Contains("MapVehiclePlacementConfig", StringComparison.Ordinal);
                })
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            string[] expected = ExpectedRuntimeConsumerSpecs().Keys
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (!discovered.SequenceEqual(expected, StringComparer.Ordinal))
                throw new InvalidOperationException("Runtime placement consumer set drifted.");
        }

        private static List<OwnershipDecisionReport> BuildDecisions()
        {
            return new[] { BuildingSpec(), VehicleSpec() }
                .Select(spec => new OwnershipDecisionReport
                {
                    stableIdentity = spec.assetPath,
                    currentOwner = CurrentOwner,
                    targetOwner = TargetOwner,
                    ownershipOwner = "Operation map architecture owner",
                    migrationDisposition = MigrationDisposition,
                    migrationOwner = "Gameplay placement owner",
                    state = DecisionState,
                    decisionOwner = DecisionOwner
                })
                .OrderBy(entry => entry.stableIdentity, StringComparer.Ordinal).ToList();
        }

        private static bool HasExpectedDecisions(IReadOnlyList<OwnershipDecisionReport> decisions)
        {
            List<OwnershipDecisionReport> expected = BuildDecisions();
            if (decisions == null || decisions.Count != expected.Count ||
                !IsStrictlyOrdered(decisions.Select(entry => entry.stableIdentity)))
                return false;
            for (int i = 0; i < decisions.Count; i++)
            {
                OwnershipDecisionReport actual = decisions[i];
                OwnershipDecisionReport target = expected[i];
                if (actual == null ||
                    !string.Equals(actual.stableIdentity, target.stableIdentity, StringComparison.Ordinal) ||
                    !string.Equals(actual.currentOwner, target.currentOwner, StringComparison.Ordinal) ||
                    !string.Equals(actual.targetOwner, target.targetOwner, StringComparison.Ordinal) ||
                    !string.Equals(actual.ownershipOwner, target.ownershipOwner, StringComparison.Ordinal) ||
                    !string.Equals(actual.migrationDisposition, target.migrationDisposition, StringComparison.Ordinal) ||
                    !string.Equals(actual.migrationOwner, target.migrationOwner, StringComparison.Ordinal) ||
                    !string.Equals(actual.state, DecisionState, StringComparison.Ordinal) ||
                    !string.Equals(actual.decisionOwner, DecisionOwner, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static PlacementSpec BuildingSpec() => new(
            "Building", BuildingAssetPath, BuildingAssetGuid, typeof(MapBuildingPlacementConfig).FullName,
            "mapBuildingPlacementConfig", "mapBuildingAuthoringRoot", "Map[10]/Buildings[18]",
            4130205265208547405, ExpectedBuildingCount, 205, 246);

        private static PlacementSpec VehicleSpec() => new(
            "Vehicle", VehicleAssetPath, VehicleAssetGuid, typeof(MapVehiclePlacementConfig).FullName,
            "mapVehiclePlacementConfig", "mapVehicleAuthoringRoot", "Map[10]/Vehicles[20]",
            8246677813643586928, ExpectedVehicleCount, 7, 22);

        private static bool HasExpectedCounts(ReportCounts counts)
        {
            return counts != null && counts.placementConfigs == 2 &&
                   counts.buildingPlacements == ExpectedBuildingCount &&
                   counts.vehiclePlacements == ExpectedVehicleCount &&
                   counts.totalPlacements == ExpectedBuildingCount + ExpectedVehicleCount &&
                   counts.runtimeConsumers == ExpectedRuntimeConsumerSpecs().Count &&
                   counts.needsDecision == 2;
        }

        private static ReportCounts BuildCounts(
            IReadOnlyList<PlacementConfigReport> placements,
            IReadOnlyList<RuntimeConsumerReport> consumers,
            IReadOnlyList<OwnershipDecisionReport> decisions)
        {
            int buildingCount = placements.Single(entry =>
                string.Equals(entry.kind, BuildingSpec().kind, StringComparison.Ordinal)).count;
            int vehicleCount = placements.Single(entry =>
                string.Equals(entry.kind, VehicleSpec().kind, StringComparison.Ordinal)).count;
            return new ReportCounts
            {
                placementConfigs = placements.Count,
                buildingPlacements = buildingCount,
                vehiclePlacements = vehicleCount,
                totalPlacements = buildingCount + vehicleCount,
                runtimeConsumers = consumers.Count,
                needsDecision = decisions.Count(entry =>
                    string.Equals(entry.state, DecisionState, StringComparison.Ordinal))
            };
        }

        private static bool HasConsistentCounts(PlacementOwnershipReport report)
        {
            if (report?.counts == null || report.placementConfigs == null ||
                report.runtimeConsumers == null || report.decisions == null)
                return false;
            PlacementConfigReport building = report.placementConfigs.SingleOrDefault(entry =>
                entry != null && string.Equals(entry.kind, BuildingSpec().kind, StringComparison.Ordinal));
            PlacementConfigReport vehicle = report.placementConfigs.SingleOrDefault(entry =>
                entry != null && string.Equals(entry.kind, VehicleSpec().kind, StringComparison.Ordinal));
            return building != null && vehicle != null &&
                   report.counts.placementConfigs == report.placementConfigs.Count &&
                   report.counts.buildingPlacements == building.count &&
                   report.counts.vehiclePlacements == vehicle.count &&
                   report.counts.totalPlacements == report.placementConfigs.Sum(entry => entry.count) &&
                   report.counts.runtimeConsumers == report.runtimeConsumers.Count &&
                   report.counts.needsDecision == report.decisions.Count(entry =>
                       string.Equals(entry.state, DecisionState, StringComparison.Ordinal));
        }

        private static bool HasFactionCounts(IReadOnlyList<FactionCountReport> counts, int faction0, int faction1)
        {
            return counts != null && counts.Count == 2 &&
                   counts[0].factionId == 0 && counts[0].count == faction0 &&
                   counts[1].factionId == 1 && counts[1].count == faction1;
        }

        private static bool HasExactAssetIdentity(
            ObjectIdentityReport identity,
            string path,
            string guid,
            string type)
        {
            return identity != null && string.Equals(identity.assetPath, path, StringComparison.Ordinal) &&
                   string.Equals(identity.assetGuid, guid, StringComparison.Ordinal) &&
                   identity.localId == 11400000 && string.Equals(identity.type, type, StringComparison.Ordinal) &&
                   string.IsNullOrEmpty(identity.scenePath) && string.IsNullOrEmpty(identity.hierarchyPath) &&
                   !string.IsNullOrWhiteSpace(identity.globalObjectId);
        }

        private static bool HasExactSceneIdentity(
            ObjectIdentityReport identity,
            string hierarchyPath,
            long localId,
            string type)
        {
            return identity != null && string.Equals(identity.scenePath, MatchScenePath, StringComparison.Ordinal) &&
                   string.Equals(identity.sceneGuid, MatchSceneGuid, StringComparison.Ordinal) &&
                   string.Equals(identity.hierarchyPath, hierarchyPath, StringComparison.Ordinal) &&
                   identity.localId == localId && string.Equals(identity.type, type, StringComparison.Ordinal) &&
                   !string.IsNullOrWhiteSpace(identity.globalObjectId);
        }

        private static void RequireInputHashesEqual(
            IReadOnlyList<InputHashReport> before,
            IReadOnlyList<InputHashReport> after)
        {
            if (before.Count != after.Count)
                throw new InvalidOperationException("Direct input set changed during inspection.");
            for (int i = 0; i < before.Count; i++)
            {
                if (!string.Equals(before[i].path, after[i].path, StringComparison.Ordinal) ||
                    !string.Equals(before[i].sha256, after[i].sha256, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Direct input changed during inspection: {before[i].path}");
            }
        }

        private static Scene OpenSceneForInspection(string path)
        {
            Scene loaded = SceneManager.GetSceneByPath(path);
            return loaded.IsValid() && loaded.isLoaded
                ? loaded
                : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private static MatchSceneView RequireSingleMatchSceneView(Scene scene)
        {
            MatchSceneView[] views = Resources.FindObjectsOfTypeAll<MatchSceneView>()
                .Where(candidate => candidate.gameObject.scene == scene).ToArray();
            if (views.Length != 1)
                throw new InvalidOperationException($"Expected one MatchSceneView; found {views.Length}.");
            return views[0];
        }

        private static void RequireCleanLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.isDirty)
                    throw new InvalidOperationException($"Refusing to inspect dirty scene: {scene.path}");
            }
        }

        private static void RequireSupportedSceneSetup(SceneSetup[] setup)
        {
            if (setup == null || setup.Length == 0 || setup.All(entry => !string.IsNullOrWhiteSpace(entry.path)))
                return;
            if (setup.Length != 1 || setup.Any(entry => !string.IsNullOrWhiteSpace(entry.path)))
                throw new InvalidOperationException("Cannot restore mixed untitled scene setup exactly.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && string.IsNullOrWhiteSpace(scene.path) && scene.rootCount != 0)
                    throw new InvalidOperationException("Untitled scene must be empty for deterministic inspection.");
            }
        }

        private static void RestoreSceneSetup(SceneSetup[] setup)
        {
            if (setup != null && setup.Any(entry => !string.IsNullOrWhiteSpace(entry.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static string RequireProjectRoot()
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Unity project root could not be resolved.");
            return root;
        }

        private static string BuildNameHierarchyPath(Transform transform)
        {
            var parts = new Stack<string>();
            for (Transform current = transform; current != null; current = current.parent)
                parts.Push(current.name);
            return string.Join("/", parts);
        }

        private static string BuildIndexedHierarchyPath(Transform transform)
        {
            var parts = new Stack<string>();
            for (Transform current = transform; current != null; current = current.parent)
                parts.Push(current.name + "[" + current.GetSiblingIndex() + "]");
            return string.Join("/", parts);
        }

        private static bool IsStrictlyOrdered(IEnumerable<string> values)
        {
            string previous = null;
            foreach (string value in values)
            {
                if (string.IsNullOrWhiteSpace(value) ||
                    (previous != null && string.CompareOrdinal(previous, value) >= 0))
                    return false;
                previous = value;
            }
            return true;
        }

        private static int CompareStringLists(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            int comparison = left.Count.CompareTo(right.Count);
            if (comparison != 0) return comparison;
            for (int i = 0; i < left.Count; i++)
            {
                comparison = string.CompareOrdinal(left[i], right[i]);
                if (comparison != 0) return comparison;
            }
            return 0;
        }

        private static int CompareVectorBits(Vector3 left, Vector3 right)
        {
            int comparison = CompareFloatBits(left.x, right.x);
            if (comparison != 0) return comparison;
            comparison = CompareFloatBits(left.y, right.y);
            return comparison != 0 ? comparison : CompareFloatBits(left.z, right.z);
        }

        private static int CompareFloatBits(float left, float right) =>
            CanonicalFloatBits(left).CompareTo(CanonicalFloatBits(right));

        private static void AppendVectorBits(StringBuilder builder, Vector3 value)
        {
            builder.Append(CanonicalFloatBits(value.x).ToString("x8", CultureInfo.InvariantCulture)).Append(',')
                .Append(CanonicalFloatBits(value.y).ToString("x8", CultureInfo.InvariantCulture)).Append(',')
                .Append(CanonicalFloatBits(value.z).ToString("x8", CultureInfo.InvariantCulture)).Append('|');
        }

        private static int CanonicalFloatBits(float value) =>
            value == 0f ? 0 : BitConverter.SingleToInt32Bits(value);

        private static void AppendIdentity(StringBuilder builder, string value)
        {
            string normalized = value ?? string.Empty;
            builder.Append(normalized.Length).Append(':').Append(normalized).Append('|');
        }

        private static string ComputeSha256(string value) =>
            OperationMapPhase0BaselineProbe.ComputeSha256(Utf8WithoutBom.GetBytes(value));

        private static string ComputeSha256(byte[] value) =>
            OperationMapPhase0BaselineProbe.ComputeSha256(value);

        private static bool IsSha256(string value) =>
            value != null && value.Length == 64 &&
            value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

        private static bool IsFinite(Vector3 value) =>
            IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        private static string NormalizeSeparators(string path) => path.Replace('\\', '/');

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private sealed class PlacementEntryComparer : IComparer<PlacementEntryReport>
        {
            internal static readonly PlacementEntryComparer Instance = new();
            public int Compare(PlacementEntryReport left, PlacementEntryReport right) =>
                ComparePlacementEntries(left, right);
        }

        private sealed class PlacementSpec
        {
            public readonly string kind;
            public readonly string assetPath;
            public readonly string assetGuid;
            public readonly string configType;
            public readonly string configField;
            public readonly string rootField;
            public readonly string rootHierarchyPath;
            public readonly long rootLocalId;
            public readonly int count;
            public readonly int faction0Count;
            public readonly int faction1Count;

            public PlacementSpec(
                string kind, string assetPath, string assetGuid, string configType,
                string configField, string rootField, string rootHierarchyPath, long rootLocalId,
                int count, int faction0Count, int faction1Count)
            {
                this.kind = kind;
                this.assetPath = assetPath;
                this.assetGuid = assetGuid;
                this.configType = configType;
                this.configField = configField;
                this.rootField = rootField;
                this.rootHierarchyPath = rootHierarchyPath;
                this.rootLocalId = rootLocalId;
                this.count = count;
                this.faction0Count = faction0Count;
                this.faction1Count = faction1Count;
            }
        }

        private sealed class RuntimeConsumerSpec
        {
            public readonly bool building;
            public readonly bool vehicle;
            public readonly string responsibility;

            public RuntimeConsumerSpec(bool building, bool vehicle, string responsibility)
            {
                this.building = building;
                this.vehicle = vehicle;
                this.responsibility = responsibility;
            }
        }

        [Serializable]
        internal sealed class PlacementOwnershipReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string baselineCommit;
            public string result;
            public ReportCounts counts;
            public BaselineReferenceReport opmap002Baseline;
            public List<InputHashReport> directInputHashes;
            public List<RuntimeConsumerReport> runtimeConsumers;
            public List<PlacementConfigReport> placementConfigs;
            public List<OwnershipDecisionReport> decisions;
        }

        [Serializable]
        internal sealed class ReportCounts
        {
            public int placementConfigs;
            public int buildingPlacements;
            public int vehiclePlacements;
            public int totalPlacements;
            public int runtimeConsumers;
            public int needsDecision;
        }

        [Serializable]
        internal sealed class BaselineReferenceReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public string evidencePath;
            public string evidenceSha256;
            public int buildingPlacementCount;
            public int vehiclePlacementCount;
            public string generatedCombinedAggregateSha256;
        }

        [Serializable]
        internal sealed class InputHashReport
        {
            public string path;
            public string sha256;
        }

        [Serializable]
        internal sealed class RuntimeConsumerReport
        {
            public string path;
            public bool consumesBuildingPlacements;
            public bool consumesVehiclePlacements;
            public string responsibility;
        }

        [Serializable]
        internal sealed class PlacementConfigReport
        {
            public string kind;
            public ObjectIdentityReport asset;
            public MatchSceneViewBindingReport binding;
            public bool spawnOnMatchStart;
            public bool hideAuthoringVisualsAfterSpawn;
            public int count;
            public List<FactionCountReport> factionCounts;
            public string identityAggregateSha256;
            public List<PlacementEntryReport> entries;
        }

        [Serializable]
        internal sealed class MatchSceneViewBindingReport
        {
            public string scenePath;
            public string sceneGuid;
            public ObjectIdentityReport matchSceneView;
            public string configField;
            public string authoringRootField;
            public ObjectIdentityReport authoringRoot;
        }

        [Serializable]
        internal sealed class FactionCountReport
        {
            public int factionId;
            public int count;
        }

        [Serializable]
        internal sealed class PlacementEntryReport
        {
            public string stableIdentitySha256;
            public string sourcePath;
            public string category;
            public string sourceKey;
            public int factionId;
            public int configSourcePathOccurrenceCount;
            public int sceneMatchCount;
            public List<string> hierarchyPaths;
            public Vector3 worldCenter;
            public Vector3 worldPosition;
            public Vector3 worldEulerAngles;
            public Vector3 worldScale;
            public float yawDegrees;
            public bool rotateVertical;
            public ObjectIdentityReport prefab;
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
        internal sealed class OwnershipDecisionReport
        {
            public string stableIdentity;
            public string currentOwner;
            public string targetOwner;
            public string ownershipOwner;
            public string migrationDisposition;
            public string migrationOwner;
            public string state;
            public string decisionOwner;
        }
    }
}

#endif
