#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Game.Composition;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class OperationMapPhase0OwnershipProbe
    {
        internal const string ReportSchema = "warline.operation-map.phase0-ownership";
        internal const int ReportSchemaVersion = 1;
        internal const string BaselineCommit = "3b7228292db7159c3c70025cf5d1676573721cd4";
        internal const string ReportPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_PHASE0_OWNERSHIP_REPORT_PATH";
        internal const string DefaultReportPath =
            "/private/tmp/warline-operation-map-phase0-ownership.json";

        private const int ExpectedFieldCount = 29;
        private const int ExpectedMatchRootCount = 16;
        private const int ExpectedSubSceneRootCount = 3;
        private const int ExpectedGeneratedChunkCount = 514;
        private const int ExpectedManifestSourceCount = 16542;
        private const int ExpectedBuildingPlacementCount = 451;
        private const int ExpectedVehiclePlacementCount = 29;
        private const string ExpectedGeneratedCombinedAggregateSha256 =
            "574afec991fbc1a684531c9f727c20eb296271260e7a4e1c4a8c300a2b642e79";
        private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
        private const string MatchSubScenePath = "Assets/Game/Scenes/Match/MatchSubScene.unity";
        private const string MatchSceneViewSourcePath =
            "Assets/Game/Scripts/Composition/MatchSceneView.cs";
        private const string BaselineEvidencePath =
            "Design/AgentReports/2026-07-14_opmap-002_phase0_baseline_probe.md";
        private const string TrackerPath =
            "Design/Architecture/operation_map_scene_split_and_generator_tracker.md";

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        public enum OwnershipClassification
        {
            ShellOwned = 0,
            MapOwned = 1,
            SharedConfig = 2,
            TemporaryCompatibility = 3,
            Mixed = 4,
            Unresolved = 5
        }

        public static void Run()
        {
            string projectRoot = RequireProjectRoot();
            string outputPath = ResolveReportOutputPath(
                projectRoot,
                Environment.GetEnvironmentVariable(ReportPathEnvironmentVariable));
            InvalidateOutput(outputPath);
            List<InputHashReport> beforeHashes = HashDirectInputs(projectRoot, null);
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            RequireSupportedSceneSetup(previousSetup);
            OwnershipReport report;

            try
            {
                RequireCleanLoadedScenes();
                Scene matchScene = OpenSceneForInspection(MatchScenePath);
                Scene subScene = OpenSceneForInspection(MatchSubScenePath);
                MatchSceneView matchSceneView = FindMatchSceneView(matchScene);
                List<OwnershipRow> fields = BuildFieldRows(CaptureFieldReports(matchSceneView));
                List<OwnershipRow> matchRoots = BuildRootRows(
                    CaptureSceneReport(matchScene), MatchRootSpecs());
                List<OwnershipRow> subSceneRoots = BuildRootRows(
                    CaptureSceneReport(subScene), SubSceneRootSpecs());
                BaselineReferenceReport baseline = LoadBaselineReference(projectRoot);
                List<InputHashReport> afterHashes = HashDirectInputs(projectRoot, beforeHashes);
                RequireInputHashesEqual(beforeHashes, afterHashes);
                report = BuildReport(
                    baseline,
                    beforeHashes,
                    fields,
                    matchRoots,
                    subSceneRoots);
            }
            finally
            {
                RestoreSceneSetup(previousSetup);
                DeleteIfPresent(outputPath + ".tmp");
            }

            PublishReportAtomically(outputPath, JsonUtility.ToJson(report, true) + "\n");
            Debug.Log(
                $"[OperationMapPhase0OwnershipProbe] result={report.result} " +
                $"fields={report.counts.matchSceneViewFields} " +
                $"matchRoots={report.counts.matchRoots} " +
                $"subSceneRoots={report.counts.matchSubSceneRoots} " +
                $"needsDecision={report.counts.needsDecision} report={outputPath}");
        }

        internal static string ResolveReportOutputPath(string projectRoot, string configuredPath)
        {
            string ownershipPath = string.IsNullOrWhiteSpace(configuredPath)
                ? DefaultReportPath
                : configuredPath;
            return OperationMapPhase0BaselineProbe.ResolveReportOutputPath(projectRoot, ownershipPath);
        }

        internal static OwnershipReport BuildReport(
            BaselineReferenceReport baseline,
            List<InputHashReport> directInputHashes,
            List<OwnershipRow> fields,
            List<OwnershipRow> matchRoots,
            List<OwnershipRow> subSceneRoots)
        {
            ValidateBaselineReference(baseline);
            ValidateInputHashes(directInputHashes);
            List<OwnershipRow> allRows = fields.Concat(matchRoots).Concat(subSceneRoots).ToList();
            int needsDecision = allRows.Count(row =>
                string.Equals(row.classification, OwnershipClassification.Mixed.ToString(), StringComparison.Ordinal) ||
                string.Equals(row.classification, OwnershipClassification.Unresolved.ToString(), StringComparison.Ordinal));

            var report = new OwnershipReport
            {
                reportSchema = ReportSchema,
                reportSchemaVersion = ReportSchemaVersion,
                baselineCommit = BaselineCommit,
                result = needsDecision == 0 ? "Passed" : "NeedsDecision",
                counts = BuildCounts(allRows, fields.Count, matchRoots.Count, subSceneRoots.Count, needsDecision),
                opmap002Baseline = baseline,
                directInputHashes = directInputHashes.OrderBy(entry => entry.path, StringComparer.Ordinal).ToList(),
                matchSceneViewFields = fields,
                matchRoots = matchRoots,
                matchSubSceneRoots = subSceneRoots
            };

            string json = JsonUtility.ToJson(report, true) + "\n";
            if (!HasRequiredReportShape(json))
                throw new InvalidOperationException("Ownership report failed its required-shape validation.");
            return report;
        }

        private static List<OperationMapPhase0BaselineProbe.SerializedObjectReferenceFieldReport>
            CaptureFieldReports(MatchSceneView matchSceneView)
        {
            Dictionary<string, OwnershipSpec> specs = FieldSpecs();
            string[] serializedReferenceFields = typeof(MatchSceneView)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(IsSerializedObjectReferenceField)
                .Select(field => field.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            string[] classifiedFields = specs.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (!serializedReferenceFields.SequenceEqual(classifiedFields, StringComparer.Ordinal))
                throw new InvalidOperationException(
                    "MatchSceneView serialized reference field set drifted from the ownership catalog.");
            var reports = new List<OperationMapPhase0BaselineProbe.SerializedObjectReferenceFieldReport>(
                specs.Count);
            Type viewType = typeof(MatchSceneView);
            foreach (string fieldName in specs.Keys.OrderBy(name => name, StringComparer.Ordinal))
            {
                FieldInfo field = viewType.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null || !TryGetObjectReferenceElementType(field.FieldType, out Type elementType))
                    throw new InvalidOperationException(
                        $"MatchSceneView serialized reference field drift: {fieldName}.");

                List<UnityEngine.Object> targets = EnumerateTargets(field.GetValue(matchSceneView));
                reports.Add(new OperationMapPhase0BaselineProbe.SerializedObjectReferenceFieldReport
                {
                    propertyName = fieldName,
                    declaredType = elementType.FullName,
                    isCollection = field.FieldType.IsArray ||
                                   typeof(System.Collections.IList).IsAssignableFrom(field.FieldType),
                    elementCount = targets.Count,
                    targets = targets.Select(CaptureObjectIdentity)
                        .OrderBy(entry => entry.assetPath, StringComparer.Ordinal)
                        .ThenBy(entry => entry.scenePath, StringComparer.Ordinal)
                        .ThenBy(entry => entry.hierarchyPath, StringComparer.Ordinal)
                        .ThenBy(entry => entry.localId)
                        .ToList()
                });
            }
            return reports;
        }

        private static bool IsSerializedObjectReferenceField(FieldInfo field)
        {
            bool serialized = field.IsPublic || field.IsDefined(typeof(SerializeField), inherit: true);
            return serialized && !field.IsNotSerialized &&
                   TryGetObjectReferenceElementType(field.FieldType, out _);
        }

        private static bool TryGetObjectReferenceElementType(Type type, out Type elementType)
        {
            elementType = type;
            if (type.IsArray)
                elementType = type.GetElementType();
            else if (type.IsGenericType && typeof(System.Collections.IList).IsAssignableFrom(type))
                elementType = type.GetGenericArguments()[0];
            return elementType != null && typeof(UnityEngine.Object).IsAssignableFrom(elementType);
        }

        private static List<UnityEngine.Object> EnumerateTargets(object value)
        {
            var targets = new List<UnityEngine.Object>();
            if (value is UnityEngine.Object single)
                targets.Add(single);
            else if (value is System.Collections.IEnumerable collection)
            {
                foreach (object entry in collection)
                {
                    if (entry is UnityEngine.Object target)
                        targets.Add(target);
                }
            }
            return targets;
        }

        private static OperationMapPhase0BaselineProbe.ObjectIdentityReport CaptureObjectIdentity(
            UnityEngine.Object target)
        {
            string assetPath = AssetDatabase.GetAssetPath(target);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out string assetGuid, out long localId);
            GameObject gameObject = target as GameObject;
            if (target is Component component)
                gameObject = component.gameObject;
            string scenePath = gameObject != null ? gameObject.scene.path : string.Empty;
            return new OperationMapPhase0BaselineProbe.ObjectIdentityReport
            {
                name = target.name,
                type = target.GetType().FullName,
                assetPath = assetPath,
                assetGuid = assetGuid,
                localId = localId,
                scenePath = scenePath,
                sceneGuid = string.IsNullOrEmpty(scenePath)
                    ? string.Empty
                    : AssetDatabase.AssetPathToGUID(scenePath),
                hierarchyPath = gameObject != null ? BuildHierarchyPath(gameObject.transform) : string.Empty,
                globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(target).ToString()
            };
        }

        private static OperationMapPhase0BaselineProbe.SceneReport CaptureSceneReport(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            var reports = new List<OperationMapPhase0BaselineProbe.RootObjectReport>(roots.Length);
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                reports.Add(new OperationMapPhase0BaselineProbe.RootObjectReport
                {
                    name = root.name,
                    siblingIndex = i,
                    hierarchyPath = root.name + "[" + i + "]",
                    rootComponentTypes = root.GetComponents<Component>()
                        .Where(component => component != null)
                        .Select(component => component.GetType().FullName)
                        .OrderBy(type => type, StringComparer.Ordinal)
                        .ToList()
                });
            }
            return new OperationMapPhase0BaselineProbe.SceneReport
            {
                path = scene.path,
                guid = AssetDatabase.AssetPathToGUID(scene.path),
                rootObjectCount = roots.Length,
                rootObjects = reports
            };
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            var segments = new Stack<string>();
            for (Transform current = transform; current != null; current = current.parent)
                segments.Push(current.name + "[" + current.GetSiblingIndex() + "]");
            return string.Join("/", segments);
        }

        internal static bool HasRequiredReportShape(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;
            try
            {
                OwnershipReport report = JsonUtility.FromJson<OwnershipReport>(json);
                if (report == null ||
                    !string.Equals(report.reportSchema, ReportSchema, StringComparison.Ordinal) ||
                    report.reportSchemaVersion != ReportSchemaVersion ||
                    !string.Equals(report.baselineCommit, BaselineCommit, StringComparison.Ordinal))
                    return false;
                if (report.counts == null ||
                    report.counts.matchSceneViewFields != ExpectedFieldCount ||
                    report.counts.matchRoots != ExpectedMatchRootCount ||
                    report.counts.matchSubSceneRoots != ExpectedSubSceneRootCount)
                    return false;
                if (report.matchSceneViewFields == null ||
                    report.matchSceneViewFields.Count != ExpectedFieldCount ||
                    report.matchRoots == null || report.matchRoots.Count != ExpectedMatchRootCount ||
                    report.matchSubSceneRoots == null ||
                    report.matchSubSceneRoots.Count != ExpectedSubSceneRootCount)
                    return false;
                if (!HasExactOrdering(report.matchSceneViewFields, sortByIdentity: true) ||
                    !HasSiblingOrdering(report.matchRoots) ||
                    !HasSiblingOrdering(report.matchSubSceneRoots))
                    return false;
                if (!HasExpectedRows(
                        report.matchSceneViewFields,
                        FieldSpecs(),
                        "Game.Composition.MatchSceneView::",
                        ExpectedFieldTargetIdentities(),
                        new[] { MatchSceneViewSourcePath, TrackerPath }) ||
                    !HasExpectedRows(
                        report.matchRoots,
                        MatchRootSpecs(),
                        MatchScenePath + "::",
                        null,
                        new[] { MatchScenePath, TrackerPath }) ||
                    !HasExpectedRows(
                        report.matchSubSceneRoots,
                        SubSceneRootSpecs(),
                        MatchSubScenePath + "::",
                        null,
                        new[] { MatchSubScenePath, TrackerPath }))
                    return false;

                List<OwnershipRow> rows = report.matchSceneViewFields
                    .Concat(report.matchRoots)
                    .Concat(report.matchSubSceneRoots)
                    .ToList();
                if (rows.Select(row => row.stableIdentity).Distinct(StringComparer.Ordinal).Count() != rows.Count ||
                    rows.Any(row => !IsCompleteRow(row)))
                    return false;
                int decisions = rows.Count(IsDecisionRow);
                if (report.counts.needsDecision != decisions)
                    return false;
                if (report.counts.shellOwned != Count(rows, OwnershipClassification.ShellOwned) ||
                    report.counts.mapOwned != Count(rows, OwnershipClassification.MapOwned) ||
                    report.counts.sharedConfig != Count(rows, OwnershipClassification.SharedConfig) ||
                    report.counts.temporaryCompatibility !=
                    Count(rows, OwnershipClassification.TemporaryCompatibility) ||
                    report.counts.mixed != Count(rows, OwnershipClassification.Mixed) ||
                    report.counts.unresolved != Count(rows, OwnershipClassification.Unresolved))
                    return false;
                if (decisions > 0 && !string.Equals(report.result, "NeedsDecision", StringComparison.Ordinal))
                    return false;
                if (decisions == 0 && !string.Equals(report.result, "Passed", StringComparison.Ordinal))
                    return false;
                ValidateBaselineReference(report.opmap002Baseline);
                ValidateInputHashes(report.directInputHashes);
                return report.counts.shellOwned + report.counts.mapOwned +
                       report.counts.sharedConfig + report.counts.temporaryCompatibility +
                       report.counts.mixed + report.counts.unresolved == rows.Count;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasExpectedRows(
            IReadOnlyList<OwnershipRow> rows,
            IReadOnlyDictionary<string, OwnershipSpec> specs,
            string identityPrefix,
            IReadOnlyDictionary<string, string[]> expectedFieldTargets,
            IEnumerable<string> baseEvidencePaths)
        {
            if (rows == null || rows.Count != specs.Count)
                return false;
            foreach (KeyValuePair<string, OwnershipSpec> pair in specs)
            {
                string stableIdentity = identityPrefix + pair.Key;
                OwnershipRow row = rows.SingleOrDefault(candidate =>
                    string.Equals(candidate.stableIdentity, stableIdentity, StringComparison.Ordinal));
                bool isField = expectedFieldTargets != null;
                string[] expectedTargets = isField
                    ? expectedFieldTargets[pair.Key]
                    : new[] { stableIdentity };
                string expectedName = isField
                    ? pair.Key
                    : pair.Key.Substring(0, pair.Key.LastIndexOf('['));
                string[] expectedEvidence = baseEvidencePaths
                    .Concat(expectedTargets.Select(TargetEvidencePath))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray();
                if (row == null ||
                    !string.Equals(row.name, expectedName, StringComparison.Ordinal) ||
                    !string.Equals(row.declaredType, pair.Value.declaredType, StringComparison.Ordinal) ||
                    !string.Equals(row.currentType, pair.Value.currentType, StringComparison.Ordinal) ||
                    row.currentElementCount != expectedTargets.Length ||
                    row.currentTargetIdentities == null ||
                    !row.currentTargetIdentities.SequenceEqual(expectedTargets, StringComparer.Ordinal) ||
                    !string.Equals(
                        row.classification,
                        pair.Value.classification.ToString(),
                        StringComparison.Ordinal) ||
                    row.evidencePaths == null ||
                    !row.evidencePaths.SequenceEqual(expectedEvidence, StringComparer.Ordinal) ||
                    !string.Equals(row.rationale, pair.Value.rationale, StringComparison.Ordinal) ||
                    !string.Equals(
                        row.migrationDisposition,
                        pair.Value.migrationDisposition,
                        StringComparison.Ordinal) ||
                    !string.Equals(row.decisionOwner, pair.Value.decisionOwner, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static string TargetEvidencePath(string targetIdentity)
        {
            int sceneSeparator = targetIdentity.IndexOf("::", StringComparison.Ordinal);
            if (sceneSeparator >= 0)
                return targetIdentity.Substring(0, sceneSeparator);
            int assetSeparator = targetIdentity.IndexOf("|guid:", StringComparison.Ordinal);
            return assetSeparator >= 0
                ? targetIdentity.Substring(0, assetSeparator)
                : targetIdentity;
        }

        private static List<OwnershipRow> BuildFieldRows(
            IReadOnlyList<OperationMapPhase0BaselineProbe.SerializedObjectReferenceFieldReport> fields)
        {
            Dictionary<string, OwnershipSpec> specs = FieldSpecs();
            if (fields == null || fields.Count != specs.Count)
                throw new InvalidOperationException(
                    $"MatchSceneView field drift: expected {specs.Count}, found {fields?.Count ?? 0}.");

            var rows = new List<OwnershipRow>(fields.Count);
            foreach (OperationMapPhase0BaselineProbe.SerializedObjectReferenceFieldReport field in
                     fields.OrderBy(entry => entry.propertyName, StringComparer.Ordinal))
            {
                if (!specs.TryGetValue(field.propertyName, out OwnershipSpec spec))
                    throw new InvalidOperationException(
                        $"Unclassified MatchSceneView field identity: {field.propertyName}");
                string currentType = field.targets == null || field.targets.Count == 0
                    ? "<none>"
                    : string.Join(
                        "|",
                        field.targets.Select(target => target.type)
                            .Distinct(StringComparer.Ordinal)
                            .OrderBy(type => type, StringComparer.Ordinal));
                if (!string.Equals(field.declaredType, spec.declaredType, StringComparison.Ordinal) ||
                    !string.Equals(currentType, spec.currentType, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"MatchSceneView type drift for {field.propertyName}: " +
                        $"declared={field.declaredType}, current={currentType}.");

                var evidence = new List<string> { MatchSceneViewSourcePath, TrackerPath };
                if (field.targets != null)
                {
                    foreach (OperationMapPhase0BaselineProbe.ObjectIdentityReport target in field.targets)
                    {
                        string path = !string.IsNullOrWhiteSpace(target.assetPath)
                            ? target.assetPath
                            : target.scenePath;
                        if (!string.IsNullOrWhiteSpace(path) && !evidence.Contains(path))
                            evidence.Add(path);
                    }
                }
                rows.Add(CreateRow(
                    "Game.Composition.MatchSceneView::" + field.propertyName,
                    field.propertyName,
                    field.declaredType,
                    currentType,
                    evidence,
                    spec,
                    field.elementCount,
                    (field.targets ?? new List<OperationMapPhase0BaselineProbe.ObjectIdentityReport>())
                        .Select(BuildTargetStableIdentity)
                        .OrderBy(identity => identity, StringComparer.Ordinal)
                        .ToList()));
            }
            return rows;
        }

        private static List<OwnershipRow> BuildRootRows(
            OperationMapPhase0BaselineProbe.SceneReport scene,
            Dictionary<string, OwnershipSpec> specs)
        {
            if (scene.rootObjects == null || scene.rootObjects.Count != specs.Count)
                throw new InvalidOperationException(
                    $"Root drift for {scene.path}: expected {specs.Count}, " +
                    $"found {scene.rootObjects?.Count ?? 0}.");

            var rows = new List<OwnershipRow>(scene.rootObjects.Count);
            for (int i = 0; i < scene.rootObjects.Count; i++)
            {
                OperationMapPhase0BaselineProbe.RootObjectReport root = scene.rootObjects[i];
                if (root.siblingIndex != i || !specs.TryGetValue(root.hierarchyPath, out OwnershipSpec spec))
                    throw new InvalidOperationException(
                        $"Unclassified ordered root identity in {scene.path}: {root.hierarchyPath}.");
                string currentType = string.Join("|", root.rootComponentTypes.OrderBy(type => type, StringComparer.Ordinal));
                if (!string.Equals(currentType, spec.currentType, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Root component drift for {scene.path}::{root.hierarchyPath}: {currentType}.");
                rows.Add(CreateRow(
                    scene.path + "::" + root.hierarchyPath,
                    root.name,
                    "UnityEngine.GameObject",
                    currentType,
                    new List<string> { scene.path, TrackerPath },
                    spec));
            }
            return rows;
        }

        private static OwnershipRow CreateRow(
            string stableIdentity,
            string name,
            string declaredType,
            string currentType,
            List<string> evidencePaths,
            OwnershipSpec spec,
            int currentElementCount = 1,
            List<string> currentTargetIdentities = null)
        {
            return new OwnershipRow
            {
                stableIdentity = stableIdentity,
                name = name,
                declaredType = declaredType,
                currentType = currentType,
                currentElementCount = currentElementCount,
                currentTargetIdentities = currentTargetIdentities ?? new List<string> { stableIdentity },
                classification = spec.classification.ToString(),
                evidencePaths = evidencePaths.OrderBy(path => path, StringComparer.Ordinal).ToList(),
                rationale = spec.rationale,
                migrationDisposition = spec.migrationDisposition,
                decisionOwner = spec.decisionOwner
            };
        }

        private static string BuildTargetStableIdentity(
            OperationMapPhase0BaselineProbe.ObjectIdentityReport target)
        {
            if (!string.IsNullOrWhiteSpace(target.scenePath))
                return target.scenePath + "::" + target.hierarchyPath + "|type:" + target.type;
            return target.assetPath + "|guid:" + target.assetGuid + "|localId:" + target.localId +
                   "|type:" + target.type;
        }

        private static BaselineReferenceReport LoadBaselineReference(string projectRoot)
        {
            string evidence = File.ReadAllText(Path.Combine(projectRoot, BaselineEvidencePath), Utf8WithoutBom);
            Match runtime = Regex.Match(
                evidence,
                @"result=Passed chunks=(?<chunks>[0-9]+) sources=(?<sources>[0-9]+)",
                RegexOptions.CultureInvariant);
            Match aggregate = Regex.Match(
                evidence,
                @"combined scene/meta aggregate SHA-256 `(?<hash>[0-9a-f]{64})`",
                RegexOptions.CultureInvariant);
            Match placements = Regex.Match(
                evidence,
                @"Placement counts are `(?<buildings>[0-9]+)` building and `(?<vehicles>[0-9]+)` vehicle",
                RegexOptions.CultureInvariant);
            if (!runtime.Success || !aggregate.Success || !placements.Success)
                throw new InvalidOperationException("Accepted opmap-002 evidence format is incomplete.");

            var baseline = new BaselineReferenceReport
            {
                reportSchema = OperationMapPhase0BaselineProbe.ReportSchema,
                reportSchemaVersion = OperationMapPhase0BaselineProbe.ReportSchemaVersion,
                result = "Passed",
                evidencePath = BaselineEvidencePath,
                evidenceSha256 = OperationMapPhase0BaselineProbe.ComputeSha256(
                    File.ReadAllBytes(Path.Combine(projectRoot, BaselineEvidencePath))),
                generatedChunkCount = int.Parse(runtime.Groups["chunks"].Value),
                manifestSourceCount = int.Parse(runtime.Groups["sources"].Value),
                buildingPlacementCount = int.Parse(placements.Groups["buildings"].Value),
                vehiclePlacementCount = int.Parse(placements.Groups["vehicles"].Value),
                generatedCombinedAggregateSha256 = aggregate.Groups["hash"].Value
            };
            ValidateBaselineReference(baseline);
            return baseline;
        }

        private static void ValidateBaselineReference(BaselineReferenceReport baseline)
        {
            if (baseline == null ||
                !string.Equals(
                    baseline.reportSchema,
                    OperationMapPhase0BaselineProbe.ReportSchema,
                    StringComparison.Ordinal) ||
                baseline.reportSchemaVersion != OperationMapPhase0BaselineProbe.ReportSchemaVersion ||
                !string.Equals(baseline.result, "Passed", StringComparison.Ordinal) ||
                !string.Equals(baseline.evidencePath, BaselineEvidencePath, StringComparison.Ordinal) ||
                !string.Equals(
                    baseline.evidenceSha256,
                    ExpectedDirectInputHashes()[BaselineEvidencePath],
                    StringComparison.Ordinal) ||
                baseline.generatedChunkCount != ExpectedGeneratedChunkCount ||
                baseline.manifestSourceCount != ExpectedManifestSourceCount ||
                baseline.buildingPlacementCount != ExpectedBuildingPlacementCount ||
                baseline.vehiclePlacementCount != ExpectedVehiclePlacementCount ||
                !string.Equals(
                    baseline.generatedCombinedAggregateSha256,
                    ExpectedGeneratedCombinedAggregateSha256,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("Unsupported opmap-002 cross-reference evidence.");
        }

        private static MatchSceneView FindMatchSceneView(Scene scene)
        {
            MatchSceneView[] candidates = Resources.FindObjectsOfTypeAll<MatchSceneView>();
            MatchSceneView match = candidates.SingleOrDefault(candidate => candidate.gameObject.scene == scene);
            if (match == null)
                throw new InvalidOperationException("Match scene must contain exactly one MatchSceneView.");
            return match;
        }

        private static Scene OpenSceneForInspection(string path)
        {
            Scene loaded = SceneManager.GetSceneByPath(path);
            return loaded.IsValid() && loaded.isLoaded
                ? loaded
                : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
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

        private static void RestoreSceneSetup(SceneSetup[] previousSetup)
        {
            if (previousSetup != null && previousSetup.Any(entry =>
                    !string.IsNullOrWhiteSpace(entry.path)))
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                return;
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void RequireSupportedSceneSetup(SceneSetup[] previousSetup)
        {
            if (previousSetup == null || previousSetup.Length == 0 ||
                previousSetup.All(entry => !string.IsNullOrWhiteSpace(entry.path)))
                return;
            if (previousSetup.Length != 1 ||
                previousSetup.Any(entry => !string.IsNullOrWhiteSpace(entry.path)))
                throw new InvalidOperationException(
                    "Ownership inspection cannot restore a mixed or multi-scene untitled setup exactly.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && string.IsNullOrWhiteSpace(scene.path) &&
                    scene.GetRootGameObjects().Length != 0)
                    throw new InvalidOperationException(
                        "Ownership inspection requires an empty untitled scene setup.");
            }
        }

        private static List<InputHashReport> HashDirectInputs(
            string projectRoot,
            IReadOnlyList<InputHashReport> expected)
        {
            var paths = new List<string>
            {
                BaselineEvidencePath,
                MatchSceneViewSourcePath,
                TrackerPath
            };
            if (expected == null)
            {
                paths.Add(MatchScenePath);
                paths.Add(MatchSubScenePath);
            }
            else
            {
                paths = expected.Select(entry => entry.path).ToList();
            }
            return paths.OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => new InputHashReport
                {
                    path = path,
                    sha256 = OperationMapPhase0BaselineProbe.ComputeSha256(
                        File.ReadAllBytes(Path.Combine(projectRoot, path)))
                })
                .ToList();
        }

        private static void RequireInputHashesEqual(
            IReadOnlyList<InputHashReport> before,
            IReadOnlyList<InputHashReport> after)
        {
            if (before.Count != after.Count)
                throw new InvalidOperationException("Direct input set changed during ownership inspection.");
            for (int i = 0; i < before.Count; i++)
            {
                if (!string.Equals(before[i].path, after[i].path, StringComparison.Ordinal) ||
                    !string.Equals(before[i].sha256, after[i].sha256, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Direct input changed during inspection: {before[i].path}");
            }
        }

        private static void ValidateInputHashes(IReadOnlyList<InputHashReport> hashes)
        {
            Dictionary<string, string> expected = ExpectedDirectInputHashes();
            if (hashes == null || hashes.Count != expected.Count)
                throw new InvalidOperationException("Exactly five direct input hashes are required.");
            string previous = null;
            foreach (InputHashReport hash in hashes.OrderBy(entry => entry.path, StringComparer.Ordinal))
            {
                if (!IsRepoRelativePath(hash.path) || !IsSha256(hash.sha256) ||
                    string.Equals(previous, hash.path, StringComparison.Ordinal) ||
                    !expected.TryGetValue(hash.path, out string expectedHash) ||
                    !string.Equals(hash.sha256, expectedHash, StringComparison.Ordinal))
                    throw new InvalidOperationException("Direct input hash set is invalid.");
                previous = hash.path;
            }
        }

        private static Dictionary<string, string> ExpectedDirectInputHashes()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [MatchScenePath] =
                    "182f3b4cb50f48e1a573e1e90ee0c13baf9d62fce46e35b1850ef72097db5d75",
                [MatchSubScenePath] =
                    "bcc255f3fb140a0d91687b45b679b47fb60f01f5cfa8690bac3032ec642dadd8",
                [MatchSceneViewSourcePath] =
                    "1cfaf1b472523aa4af608ac7eac0fb4ce89cacec97ddbda7d4e3ccf65cd03847",
                [BaselineEvidencePath] =
                    "d4d4674850766c5cd95e1bb5fbb6f26893e0bb019dbaf266a0c9897a3befc807",
                [TrackerPath] =
                    "de77a553cc83b0c2fa0a77f717e941191f6136e767e2202627d53e96f989c00f"
            };
        }

        internal static void PublishReportAtomically(string outputPath, string json)
        {
            if (!HasRequiredReportShape(json))
                throw new InvalidOperationException("Refusing to publish an invalid ownership report.");
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            string temporaryPath = outputPath + ".tmp";
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
                File.WriteAllText(temporaryPath, json, Utf8WithoutBom);
                if (!HasRequiredReportShape(File.ReadAllText(temporaryPath, Utf8WithoutBom)))
                    throw new InvalidOperationException("Persisted ownership report is invalid.");
                File.Move(temporaryPath, outputPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        internal static void InvalidateOutput(string outputPath)
        {
            DeleteIfPresent(outputPath);
            DeleteIfPresent(outputPath + ".tmp");
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static OwnershipCounts BuildCounts(
            IReadOnlyList<OwnershipRow> rows,
            int fieldCount,
            int matchRootCount,
            int subSceneRootCount,
            int needsDecision)
        {
            return new OwnershipCounts
            {
                matchSceneViewFields = fieldCount,
                matchRoots = matchRootCount,
                matchSubSceneRoots = subSceneRootCount,
                shellOwned = Count(rows, OwnershipClassification.ShellOwned),
                mapOwned = Count(rows, OwnershipClassification.MapOwned),
                sharedConfig = Count(rows, OwnershipClassification.SharedConfig),
                temporaryCompatibility = Count(rows, OwnershipClassification.TemporaryCompatibility),
                mixed = Count(rows, OwnershipClassification.Mixed),
                unresolved = Count(rows, OwnershipClassification.Unresolved),
                needsDecision = needsDecision
            };
        }

        private static int Count(IReadOnlyList<OwnershipRow> rows, OwnershipClassification value)
        {
            string name = value.ToString();
            return rows.Count(row => string.Equals(row.classification, name, StringComparison.Ordinal));
        }

        private static bool HasExactOrdering(IReadOnlyList<OwnershipRow> rows, bool sortByIdentity)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] == null || string.IsNullOrWhiteSpace(rows[i].stableIdentity))
                    return false;
                if (i > 0 && sortByIdentity &&
                    string.CompareOrdinal(rows[i - 1].stableIdentity, rows[i].stableIdentity) >= 0)
                    return false;
            }
            return true;
        }

        private static bool HasSiblingOrdering(IReadOnlyList<OwnershipRow> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] == null || string.IsNullOrWhiteSpace(rows[i].stableIdentity) ||
                    !rows[i].stableIdentity.EndsWith("[" + i + "]", StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static bool IsCompleteRow(OwnershipRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.stableIdentity) ||
                string.IsNullOrWhiteSpace(row.name) || string.IsNullOrWhiteSpace(row.declaredType) ||
                string.IsNullOrWhiteSpace(row.currentType) || string.IsNullOrWhiteSpace(row.classification) ||
                row.evidencePaths == null || row.evidencePaths.Count == 0 ||
                row.evidencePaths.Any(path => !IsRepoRelativePath(path)) ||
                string.IsNullOrWhiteSpace(row.rationale) ||
                string.IsNullOrWhiteSpace(row.migrationDisposition))
                return false;
            if (!Enum.TryParse(row.classification, false, out OwnershipClassification classification) ||
                !Enum.IsDefined(typeof(OwnershipClassification), classification))
                return false;
            if (row.currentElementCount < 0 || row.currentTargetIdentities == null ||
                row.currentTargetIdentities.Count != row.currentElementCount ||
                row.currentTargetIdentities.Any(string.IsNullOrWhiteSpace) ||
                !row.currentTargetIdentities.SequenceEqual(
                    row.currentTargetIdentities.OrderBy(identity => identity, StringComparer.Ordinal),
                    StringComparer.Ordinal))
                return false;
            return IsDecisionRow(row) == !string.IsNullOrWhiteSpace(row.decisionOwner);
        }

        private static bool IsDecisionRow(OwnershipRow row)
        {
            return string.Equals(row.classification, OwnershipClassification.Mixed.ToString(), StringComparison.Ordinal) ||
                   string.Equals(row.classification, OwnershipClassification.Unresolved.ToString(), StringComparison.Ordinal);
        }

        private static bool IsRepoRelativePath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path) &&
                   (path.StartsWith("Assets/", StringComparison.Ordinal) ||
                    path.StartsWith("Design/", StringComparison.Ordinal)) &&
                   path.IndexOf("..", StringComparison.Ordinal) < 0 && path.IndexOf('\\') < 0;
        }

        private static bool IsSha256(string value)
        {
            return value != null && value.Length == 64 && value.All(character =>
                (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'));
        }

        private static string RequireProjectRoot()
        {
            DirectoryInfo parent = Directory.GetParent(Application.dataPath);
            if (parent == null)
                throw new InvalidOperationException("Unable to resolve project root.");
            return parent.FullName;
        }

        private static Dictionary<string, OwnershipSpec> FieldSpecs()
        {
            var specs = new Dictionary<string, OwnershipSpec>(StringComparer.Ordinal);
            AddField(specs, "aiControllerConfigs", "Game.Configs.AIControllerConfig", "Game.Configs.AIControllerSceneConfigAsset", OwnershipClassification.SharedConfig, "KeepSharedReference", "AI policy is shared gameplay configuration; ScenarioSetup selects scenario policy.");
            AddField(specs, "aiPlanEntryConfig", "Game.Configs.AIPlanEntryStartupConfig", "Game.Configs.AIPlanEntryStartupConfig", OwnershipClassification.SharedConfig, "KeepSharedReference", "Plan-entry startup policy is shared configuration, not map geometry.");
            AddField(specs, "buildingPlacementConfig", "Game.Configs.BuildingPlacementSystemConfig", "Game.Configs.BuildingPlacementSystemSceneConfigAsset", OwnershipClassification.SharedConfig, "KeepSharedReference", "Placement-system policy is shared; map-authored placements have a separate map config.");
            AddField(specs, "dayNightConfig", "Game.Configs.DayNightSystemConfig", "Game.Configs.DayNightSystemSceneConfigAsset", OwnershipClassification.Mixed, "DecisionRequired", "The shell owns the lighting boundary while map metadata may own environment intent.", "Architecture owner and lighting owner");
            AddField(specs, "decorationCombinedMeshBaker", "Game.Runtime.CombinedMeshBaker", "Game.Runtime.CombinedMeshBaker", OwnershipClassification.MapOwned, "MoveToOperationMap", "The baker reference targets current canonical map decoration geometry.");
            AddField(specs, "decorationRoot", "UnityEngine.Transform", "UnityEngine.Transform", OwnershipClassification.MapOwned, "MoveToOperationMap", "The referenced Decorations root is canonical map geometry.");
            AddField(specs, "directionalLight", "UnityEngine.Light", "UnityEngine.Light", OwnershipClassification.ShellOwned, "KeepInMatchShell", "The normative target contract assigns the directional-light reference to the shell-owned lighting boundary.");
            AddField(specs, "factionVisualConfig", "Game.Configs.FactionVisualSettingsConfig", "Game.Configs.FactionVisualSettingsSceneConfigAsset", OwnershipClassification.SharedConfig, "KeepSharedReference", "Faction visuals are shared across operation maps.");
            AddField(specs, "gameStringsConfig", "Game.Configs.GameStringsConfig", "Game.Configs.GameStringsSceneConfigAsset", OwnershipClassification.SharedConfig, "KeepSharedReference", "Localized game strings are not map content.");
            AddField(specs, "globalVolume", "UnityEngine.Rendering.Volume", "UnityEngine.Rendering.Volume", OwnershipClassification.ShellOwned, "KeepInMatchShell", "The target contract assigns match post-processing to the runtime shell.");
            AddField(specs, "mapBuildingAuthoringRoot", "UnityEngine.Transform", "UnityEngine.Transform", OwnershipClassification.MapOwned, "MoveToOperationMap", "The exact target is the map's Buildings authoring root.");
            AddField(specs, "mapBuildingPlacementConfig", "Game.Configs.MapBuildingPlacementConfig", "Game.Configs.MapBuildingPlacementConfig", OwnershipClassification.MapOwned, "ReferenceFromOperationMapDefinition", "The target contract explicitly assigns map-owned building placements to the operation map.");
            AddField(specs, "mapSurfaceAuthoring", "Game.Authoring.MapSurfaceAuthoring", "Game.Authoring.MapSurfaceAuthoring", OwnershipClassification.MapOwned, "MoveToOperationMap", "Map surface and height metadata belong to the operation map.");
            AddField(specs, "mapVehicleAuthoringRoot", "UnityEngine.Transform", "UnityEngine.Transform", OwnershipClassification.MapOwned, "MoveToOperationMap", "The exact target is the map's Vehicles authoring root.");
            AddField(specs, "mapVehiclePlacementConfig", "Game.Configs.MapVehiclePlacementConfig", "Game.Configs.MapVehiclePlacementConfig", OwnershipClassification.MapOwned, "ReferenceFromOperationMapDefinition", "The target contract explicitly assigns map-owned vehicle placements to the operation map.");
            AddField(specs, "operationMapCatalog", "Game.Configs.OperationMapCatalogConfig", "Game.Configs.OperationMapCatalogConfig", OwnershipClassification.SharedConfig, "KeepSharedReference", "The compatibility catalog is shell-selected shared configuration and does not own map geometry or delivery.");
            AddField(specs, "prefabPreviewCameraConfig", "Game.Configs.PrefabPreviewCameraConfig", "Game.Configs.PrefabPreviewCameraSceneConfigAsset", OwnershipClassification.SharedConfig, "KeepSharedReference", "Prefab preview camera policy is shared UI/composition configuration.");
            AddField(specs, "resourceExchangeConfig", "Game.Configs.ResourceExchangeRecipeConfigSet", "Game.Configs.ResourceExchangeRecipeConfigSet", OwnershipClassification.SharedConfig, "KeepSharedReference", "Resource exchange recipes are shared gameplay data.");
            AddField(specs, "roadBuildConfig", "Game.Configs.RoadBuildSystemConfig", "Game.Configs.RoadBuildSystemSceneConfigAsset", OwnershipClassification.SharedConfig, "KeepSharedReference", "Road-build system policy is shared; active map metadata supplies map constraints.");
            AddField(specs, "rtsSelectionConfig", "Game.Configs.RTSSelectionSystemConfig", "Game.Configs.RTSSelectionSystemSceneConfigAsset", OwnershipClassification.SharedConfig, "KeepSharedReference", "Selection policy belongs to shared match composition.");
            AddField(specs, "runtimeCitySpawnerConfig", "Game.Configs.RuntimeCitySpawnerSystemConfig", "Game.Configs.RuntimeCitySpawnerSystemSceneConfigAsset", OwnershipClassification.SharedConfig, "KeepSharedReference", "Runtime city spawning policy is shared and consumes active map data.");
            AddField(specs, "runtimeDecorationSpawnerConfig", "Game.Configs.RuntimeDecorationSpawnerSystemConfig", "Game.Configs.RuntimeDecorationSpawnerSystemSceneConfigAsset", OwnershipClassification.SharedConfig, "KeepSharedReference", "Decoration spawning policy is shared while decoration inputs are map-owned.");
            AddField(specs, "runtimeGridBlockerConfig", "Game.Configs.RuntimeGridBlockerSystemConfig", "Game.Configs.RuntimeGridBlockerSystemSceneConfigAsset", OwnershipClassification.SharedConfig, "KeepSharedReference", "Blocker-system policy is shared while blocker metadata is map-owned.");
            AddField(specs, "runtimeGridConfig", "Game.Configs.GridAuthoringConfig", "Game.Configs.GridAuthoringSceneConfigAsset", OwnershipClassification.MapOwned, "ReferenceFromOperationMapDefinition", "The exact MatchSubScene grid asset describes current map dimensions and origin.");
            AddField(specs, "runtimeGridDebugViews", "Game.Authoring.GridAuthoring", "<none>", OwnershipClassification.TemporaryCompatibility, "RemoveAfterCompatibilityCutover", "The serialized debug-view collection is empty and carries no current ownership target.");
            AddField(specs, "staticMapPresentationManifest", "Game.Rendering.StaticMapPresentationManifest", "Game.Rendering.StaticMapPresentationManifest", OwnershipClassification.MapOwned, "ReferenceFromOperationMapDefinition", "Static presentation manifests become map-scoped under the target contract.");
            AddField(specs, "unitAttackTraceConfig", "Game.Configs.UnitAttackTraceSystemConfig", "Game.Configs.UnitAttackTraceSystemSceneConfigAsset", OwnershipClassification.SharedConfig, "KeepSharedReference", "Attack trace presentation policy is shared across maps.");
            AddField(specs, "visualQualityProfile", "Game.Configs.VisualQualityProfileAsset", "Game.Configs.VisualQualityProfileAsset", OwnershipClassification.SharedConfig, "KeepSharedReference", "Visual quality policy is shared and applied by shell composition.");
            AddField(specs, "worldCamera", "UnityEngine.Camera", "UnityEngine.Camera", OwnershipClassification.ShellOwned, "KeepInMatchShell", "The target contract explicitly assigns the world camera to the Match shell.");
            return specs;
        }

        private static Dictionary<string, string[]> ExpectedFieldTargetIdentities()
        {
            return new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["aiControllerConfigs"] = new[]
                {
                    "Assets/Game/Configs/Scene/Game_AI_Enemy_Config.asset|guid:fb8b5c545d7f641d3b153c2f18c57aad|localId:11400000|type:Game.Configs.AIControllerSceneConfigAsset",
                    "Assets/Game/Configs/Scene/Game_AI_PlayerAuto_Config.asset|guid:34d062317806444e2a70cd1ed240fc5a|localId:11400000|type:Game.Configs.AIControllerSceneConfigAsset"
                },
                ["operationMapCatalog"] = new[]
                {
                    "Assets/Game/Configs/OperationMaps/OperationMapCatalog_Compatibility.asset|guid:5f8cd53e9416439f9974a805ef924db2|localId:11400000|type:Game.Configs.OperationMapCatalogConfig"
                },
                ["aiPlanEntryConfig"] = new[] { "Assets/Game/Configs/Scene/Game_AI_PlanEntry_Startup_Config.asset|guid:8ac55f91a18b4e56b3ef2ed875c904d7|localId:11400000|type:Game.Configs.AIPlanEntryStartupConfig" },
                ["buildingPlacementConfig"] = new[] { "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset|guid:b2010000000000000000000000000004|localId:11400000|type:Game.Configs.BuildingPlacementSystemSceneConfigAsset" },
                ["dayNightConfig"] = new[] { "Assets/Game/Configs/Scene/Game_DayNight_Config.asset|guid:b2010000000000000000000000000009|localId:11400000|type:Game.Configs.DayNightSystemSceneConfigAsset" },
                ["decorationCombinedMeshBaker"] = new[] { "Assets/Game/Scenes/Match.unity::Decorations[4]|type:Game.Runtime.CombinedMeshBaker" },
                ["decorationRoot"] = new[] { "Assets/Game/Scenes/Match.unity::Decorations[4]|type:UnityEngine.Transform" },
                ["directionalLight"] = new[] { "Assets/Game/Scenes/Match.unity::Directional Light[8]|type:UnityEngine.Light" },
                ["factionVisualConfig"] = new[] { "Assets/Game/Configs/Scene/Game_FactionVisualSettings_Config.asset|guid:b201000000000000000000000000000a|localId:11400000|type:Game.Configs.FactionVisualSettingsSceneConfigAsset" },
                ["gameStringsConfig"] = new[] { "Assets/Game/Configs/Scene/Game_GameStrings_Config.asset|guid:6bdf401b93264f908f8a9d6c0bfb6b93|localId:11400000|type:Game.Configs.GameStringsSceneConfigAsset" },
                ["globalVolume"] = new[] { "Assets/Game/Scenes/Match.unity::Global Volume[7]|type:UnityEngine.Rendering.Volume" },
                ["mapBuildingAuthoringRoot"] = new[] { "Assets/Game/Scenes/Match.unity::Map[10]/Buildings[18]|type:UnityEngine.Transform" },
                ["mapBuildingPlacementConfig"] = new[] { "Assets/Game/Configs/Scene/Match_MapBuildingPlacement_Config.asset|guid:e859aa1a53b0942609e537713fd55fb7|localId:11400000|type:Game.Configs.MapBuildingPlacementConfig" },
                ["mapSurfaceAuthoring"] = new[] { "Assets/Game/Scenes/Match.unity::Map[10]|type:Game.Authoring.MapSurfaceAuthoring" },
                ["mapVehicleAuthoringRoot"] = new[] { "Assets/Game/Scenes/Match.unity::Map[10]/Vehicles[20]|type:UnityEngine.Transform" },
                ["mapVehiclePlacementConfig"] = new[] { "Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset|guid:03d5c67074cde47488712cef0e5f494a|localId:11400000|type:Game.Configs.MapVehiclePlacementConfig" },
                ["prefabPreviewCameraConfig"] = new[] { "Assets/Game/Configs/Scene/Game_PrefabPreviewCamera_Config.asset|guid:22b4ed6a358014f0fa1ff5472f267b0c|localId:11400000|type:Game.Configs.PrefabPreviewCameraSceneConfigAsset" },
                ["resourceExchangeConfig"] = new[] { "Assets/Game/Configs/Scene/Game_ResourceExchange_Config.asset|guid:58803fa3f1c245a6822e896daeb5cc8a|localId:11400000|type:Game.Configs.ResourceExchangeRecipeConfigSet" },
                ["roadBuildConfig"] = new[] { "Assets/Game/Configs/Scene/Game_RoadBuild_Config.asset|guid:b2010000000000000000000000000003|localId:11400000|type:Game.Configs.RoadBuildSystemSceneConfigAsset" },
                ["rtsSelectionConfig"] = new[] { "Assets/Game/Configs/Scene/Game_RTSSelection_Config.asset|guid:b2010000000000000000000000000002|localId:11400000|type:Game.Configs.RTSSelectionSystemSceneConfigAsset" },
                ["runtimeCitySpawnerConfig"] = new[] { "Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset|guid:b2010000000000000000000000000006|localId:11400000|type:Game.Configs.RuntimeCitySpawnerSystemSceneConfigAsset" },
                ["runtimeDecorationSpawnerConfig"] = new[] { "Assets/Game/Configs/Scene/Game_RuntimeDecorationSpawner_Config.asset|guid:b2010000000000000000000000000007|localId:11400000|type:Game.Configs.RuntimeDecorationSpawnerSystemSceneConfigAsset" },
                ["runtimeGridBlockerConfig"] = new[] { "Assets/Game/Configs/Scene/Game_RuntimeGridBlocker_Config.asset|guid:b2010000000000000000000000000008|localId:11400000|type:Game.Configs.RuntimeGridBlockerSystemSceneConfigAsset" },
                ["runtimeGridConfig"] = new[] { "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset|guid:b201000000000000000000000000000b|localId:11400000|type:Game.Configs.GridAuthoringSceneConfigAsset" },
                ["runtimeGridDebugViews"] = Array.Empty<string>(),
                ["staticMapPresentationManifest"] = new[] { "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationManifest.asset|guid:2d7b3d165106141ba81b98138bb8fa7f|localId:11400000|type:Game.Rendering.StaticMapPresentationManifest" },
                ["unitAttackTraceConfig"] = new[] { "Assets/Game/Configs/Scene/Game_UnitAttackTrace_Config.asset|guid:b2010000000000000000000000000005|localId:11400000|type:Game.Configs.UnitAttackTraceSystemSceneConfigAsset" },
                ["visualQualityProfile"] = new[] { "Assets/Game/Rendering/VisualQualityConfig.asset|guid:d9e06dd77d8b4533a0efb56ed3e14cbb|localId:11400000|type:Game.Configs.VisualQualityProfileAsset" },
                ["worldCamera"] = new[] { "Assets/Game/Scenes/Match.unity::Main Camera[5]|type:UnityEngine.Camera" }
            };
        }

        private static Dictionary<string, OwnershipSpec> MatchRootSpecs()
        {
            var specs = new Dictionary<string, OwnershipSpec>(StringComparer.Ordinal);
            AddRoot(specs, "Bootstrap[0]", "Game.Composition.MatchSceneView|UnityEngine.Transform", OwnershipClassification.ShellOwned, "KeepInMatchShell", "The bootstrap binder and lifecycle remain in the Match shell.");
            AddRoot(specs, "MatchSubScene[1]", "Unity.Scenes.SubScene|UnityEngine.Transform", OwnershipClassification.TemporaryCompatibility, "RetainUntilMapSubSceneCutover", "The current shell reference preserves the compatibility path until map-owned subscene loading exists.");
            AddRoot(specs, "Start[2]", "UnityEngine.Transform", OwnershipClassification.Unresolved, "DecisionRequired", "A bare transform named Start has no typed contract proving camera, spawn, or objective ownership.", "Scenario design owner and architecture owner");
            AddRoot(specs, "End[3]", "UnityEngine.Transform", OwnershipClassification.Unresolved, "DecisionRequired", "A bare transform named End has no typed contract proving camera, spawn, or objective ownership.", "Scenario design owner and architecture owner");
            AddRoot(specs, "Decorations[4]", "Game.Runtime.CombinedMeshBaker|UnityEngine.Transform", OwnershipClassification.MapOwned, "MoveToOperationMap", "The root is the exact decoration geometry/bake target referenced by MatchSceneView.");
            AddRoot(specs, "Main Camera[5]", "UnityEngine.AudioListener|UnityEngine.Camera|UnityEngine.FlareLayer|UnityEngine.Rendering.Universal.UniversalAdditionalCameraData|UnityEngine.Transform", OwnershipClassification.ShellOwned, "KeepInMatchShell", "The target contract assigns the world camera and listener boundary to the shell.");
            AddRoot(specs, "Reflection Probe[6]", "UnityEngine.ReflectionProbe|UnityEngine.Transform", OwnershipClassification.MapOwned, "MoveToOperationMap", "The tracker requires probes to migrate with explicit map parity.");
            AddRoot(specs, "Global Volume[7]", "UnityEngine.Rendering.Volume|UnityEngine.Transform", OwnershipClassification.ShellOwned, "KeepInMatchShell", "The match post-processing boundary remains shell-owned.");
            AddRoot(specs, "Directional Light[8]", "UnityEngine.Light|UnityEngine.Rendering.Universal.UniversalAdditionalLightData|UnityEngine.Transform", OwnershipClassification.ShellOwned, "KeepInMatchShell", "The normative target contract assigns match lighting roots to the shell-owned lighting boundary.");
            AddRoot(specs, "Directional Light (1)[9]", "UnityEngine.Light|UnityEngine.Rendering.Universal.UniversalAdditionalLightData|UnityEngine.Transform", OwnershipClassification.ShellOwned, "KeepInMatchShell", "The normative target contract assigns match lighting roots to the shell-owned lighting boundary.");
            AddRoot(specs, "Map[10]", "Game.Authoring.MapSurfaceAuthoring|UnityEngine.Transform", OwnershipClassification.MapOwned, "MoveToOperationMap", "The canonical map hierarchy and map-surface authoring belong to the operation map.");
            AddRoot(specs, "Faction2[11]", "UnityEngine.MeshFilter|UnityEngine.MeshRenderer|UnityEngine.Transform", OwnershipClassification.MapOwned, "MoveToOperationMap", "This exact root is serialized map presentation geometry.");
            AddRoot(specs, "Faction3[12]", "UnityEngine.MeshFilter|UnityEngine.MeshRenderer|UnityEngine.Transform", OwnershipClassification.MapOwned, "MoveToOperationMap", "This exact root is serialized map presentation geometry.");
            AddRoot(specs, "Faction4[13]", "UnityEngine.MeshFilter|UnityEngine.MeshRenderer|UnityEngine.Transform", OwnershipClassification.MapOwned, "MoveToOperationMap", "This exact root is serialized map presentation geometry.");
            AddRoot(specs, "Faction5[14]", "UnityEngine.MeshFilter|UnityEngine.MeshRenderer|UnityEngine.Transform", OwnershipClassification.MapOwned, "MoveToOperationMap", "This exact root is serialized map presentation geometry.");
            AddRoot(specs, "Faction1[15]", "UnityEngine.MeshFilter|UnityEngine.MeshRenderer|UnityEngine.Transform", OwnershipClassification.MapOwned, "MoveToOperationMap", "This exact root is serialized map presentation geometry.");
            return specs;
        }

        private static Dictionary<string, OwnershipSpec> SubSceneRootSpecs()
        {
            var specs = new Dictionary<string, OwnershipSpec>(StringComparer.Ordinal);
            AddRoot(specs, "Grid[0]", "Game.Authoring.GridAuthoring|UnityEngine.Transform", OwnershipClassification.MapOwned, "MoveToOperationMapSubScene", "Grid authoring describes map dimensions, origin, and cell ownership.");
            AddRoot(specs, "InitialUnitsSpawnerAuthoring[1]", "Game.Authoring.InitialUnitsSpawnerAuthoring|UnityEngine.Transform", OwnershipClassification.Mixed, "DecisionRequired", "ScenarioSetup owns starting units, but this authoring currently resides in the map-specific subscene.", "Gameplay scenario owner and architecture owner");
            AddRoot(specs, "UnitPrefabRegistryAuthoring[2]", "Game.Authoring.UnitPrefabRegistryAuthoring|UnityEngine.Transform", OwnershipClassification.SharedConfig, "KeepSharedReference", "The unit prefab registry is shared runtime authoring rather than map geometry.");
            return specs;
        }

        private static void AddField(
            IDictionary<string, OwnershipSpec> specs,
            string name,
            string declaredType,
            string currentType,
            OwnershipClassification classification,
            string disposition,
            string rationale,
            string decisionOwner = "")
        {
            specs.Add(name, new OwnershipSpec(
                declaredType, currentType, classification, disposition, rationale, decisionOwner));
        }

        private static void AddRoot(
            IDictionary<string, OwnershipSpec> specs,
            string identity,
            string currentType,
            OwnershipClassification classification,
            string disposition,
            string rationale,
            string decisionOwner = "")
        {
            specs.Add(identity, new OwnershipSpec(
                "UnityEngine.GameObject", currentType, classification, disposition, rationale, decisionOwner));
        }

        private sealed class OwnershipSpec
        {
            public readonly string declaredType;
            public readonly string currentType;
            public readonly OwnershipClassification classification;
            public readonly string migrationDisposition;
            public readonly string rationale;
            public readonly string decisionOwner;

            public OwnershipSpec(
                string declaredType,
                string currentType,
                OwnershipClassification classification,
                string migrationDisposition,
                string rationale,
                string decisionOwner)
            {
                this.declaredType = declaredType;
                this.currentType = currentType;
                this.classification = classification;
                this.migrationDisposition = migrationDisposition;
                this.rationale = rationale;
                this.decisionOwner = decisionOwner;
            }
        }

        [Serializable]
        internal sealed class OwnershipReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string baselineCommit;
            public string result;
            public OwnershipCounts counts;
            public BaselineReferenceReport opmap002Baseline;
            public List<InputHashReport> directInputHashes;
            public List<OwnershipRow> matchSceneViewFields;
            public List<OwnershipRow> matchRoots;
            public List<OwnershipRow> matchSubSceneRoots;
        }

        [Serializable]
        internal sealed class OwnershipCounts
        {
            public int matchSceneViewFields;
            public int matchRoots;
            public int matchSubSceneRoots;
            public int shellOwned;
            public int mapOwned;
            public int sharedConfig;
            public int temporaryCompatibility;
            public int mixed;
            public int unresolved;
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
            public int generatedChunkCount;
            public int manifestSourceCount;
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
        internal sealed class OwnershipRow
        {
            public string stableIdentity;
            public string name;
            public string declaredType;
            public string currentType;
            public int currentElementCount;
            public List<string> currentTargetIdentities;
            public string classification;
            public List<string> evidencePaths;
            public string rationale;
            public string migrationDisposition;
            public string decisionOwner;
        }
    }
}

#endif
