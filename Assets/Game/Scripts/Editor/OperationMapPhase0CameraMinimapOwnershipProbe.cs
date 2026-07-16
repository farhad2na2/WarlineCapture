#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using Newtonsoft.Json.Linq;
    using UnityEngine;

    public static class OperationMapPhase0CameraMinimapOwnershipProbe
    {
        internal const string ReportSchema = "warline.operation-map.phase0-camera-minimap-ownership";
        internal const int ReportSchemaVersion = 2;
        internal const string BaselineCommit = "d9e2f1ba0e9f7df2d35abe60488fb1d44d5c91bf";
        internal const string ReportPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_PHASE0_CAMERA_MINIMAP_OWNERSHIP_REPORT_PATH";
        internal const string DefaultReportPath =
            "/private/tmp/warline-operation-map-phase0-camera-minimap-ownership.json";

        private const string Opmap002Path =
            "Design/AgentReports/2026-07-14_opmap-002_phase0_baseline_probe.md";
        private const string Opmap004Path =
            "Design/AgentReports/2026-07-15_opmap-004_phase0_ownership_baseline.json";
        private const string TrackerPath =
            "Design/Architecture/operation_map_scene_split_and_generator_tracker.md";
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);
        private static readonly Type AssistantCommandIntentAdapterType =
            typeof(Game.UI.Shell.Ecs.UiShellEcsGateway).GetNestedType(
                "UiShellActionAdapter",
                BindingFlags.NonPublic);
        private static readonly string AssistantCommandIntentMappingIdentity =
            StableDeclaringTypeName(AssistantCommandIntentAdapterType) +
            "::ToAssistantCommandIntentKind(" +
            typeof(Game.UI.Contracts.UiAssistantCommandIntentKind).FullName + "," +
            typeof(Game.Components.AssistantRecommendationKind).FullName + ")";
        private static readonly Type GridBakerType =
            typeof(Game.Authoring.GridAuthoring).GetNestedType("GridBaker", BindingFlags.NonPublic);
        private static readonly string GridBakeIdentity =
            StableDeclaringTypeName(GridBakerType) + "::Bake(" +
            typeof(Game.Authoring.GridAuthoring).FullName + ")";

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
            ValidatedReportDestination destination = ResolveReportDestination(
                projectRoot,
                Environment.GetEnvironmentVariable(ReportPathEnvironmentVariable));

            ValidateDeclaredMethodSignatures();
            ValidateNoUnexpectedProducerCandidates(projectRoot);
            List<InputHashReport> beforeHashes = CaptureAndValidateInputs(projectRoot);
            List<OwnershipRow> rows = BuildRows();
            List<PresenceFinding> findings = BuildPresenceFindings();
            List<CrossReferenceReport> references = BuildCrossReferences(beforeHashes);
            List<InputHashReport> afterHashes = CaptureAndValidateInputs(projectRoot);
            RequireInputHashesEqual(beforeHashes, afterHashes);
            ValidateNoUnexpectedProducerCandidates(projectRoot);

            OwnershipReport report = BuildReport(beforeHashes, references, findings, rows);
            PublishReportAtomically(destination, JsonUtility.ToJson(report, true) + "\n");
            Debug.Log(
                $"[OperationMapPhase0CameraMinimapOwnershipProbe] result={report.result} " +
                $"rows={report.counts.evidenceRows} needsDecision={report.counts.needsDecision} " +
                $"runtimeObjectiveWriter={report.presenceFindings[2].status} report={destination.canonicalPath}");
        }

        internal static string ResolveReportOutputPath(string projectRoot, string configuredPath)
        {
            return ResolveReportDestination(projectRoot, configuredPath).canonicalPath;
        }

        internal static ValidatedReportDestination ResolveReportDestination(
            string projectRoot,
            string configuredPath)
        {
            string path = string.IsNullOrWhiteSpace(configuredPath) ? DefaultReportPath : configuredPath;
            string resolved = Path.GetFullPath(
                OperationMapPhase0BaselineProbe.ResolveReportOutputPath(projectRoot, path));
            string outputParent = Path.GetDirectoryName(resolved);
            if (string.IsNullOrWhiteSpace(outputParent) || !Directory.Exists(outputParent))
                throw new InvalidOperationException("Camera/minimap report output parent must already exist.");

            string canonicalProjectRoot = ResolveExistingDirectory(projectRoot);
            string canonicalOutputParent = ResolveExistingDirectory(outputParent);
            if (IsSameOrDescendant(canonicalOutputParent, canonicalProjectRoot))
                throw new InvalidOperationException("Camera/minimap report output resolves inside the Unity project.");

            return new ValidatedReportDestination(
                outputParent,
                canonicalOutputParent,
                canonicalProjectRoot,
                Path.Combine(canonicalOutputParent, Path.GetFileName(resolved)));
        }

        internal static OwnershipReport BuildReport(
            List<InputHashReport> directInputHashes,
            List<CrossReferenceReport> crossReferences,
            List<PresenceFinding> presenceFindings,
            List<OwnershipRow> rows)
        {
            ValidateDeclaredMethodSignatures();
            ValidateInputHashes(directInputHashes);
            ValidateCrossReferences(crossReferences);
            ValidatePresenceFindings(presenceFindings);

            List<OwnershipRow> orderedRows = rows?
                .OrderBy(row => row.stableIdentity, StringComparer.Ordinal)
                .ToList();
            int needsDecision = orderedRows?.Count(IsDecisionRow) ?? 0;
            var report = new OwnershipReport
            {
                reportSchema = ReportSchema,
                reportSchemaVersion = ReportSchemaVersion,
                baselineCommit = BaselineCommit,
                result = needsDecision > 0 ? "NeedsDecision" : "Passed",
                counts = BuildCounts(orderedRows, needsDecision),
                crossReferences = crossReferences.OrderBy(entry => entry.taskId, StringComparer.Ordinal).ToList(),
                directInputHashes = directInputHashes.OrderBy(entry => entry.path, StringComparer.Ordinal).ToList(),
                presenceFindings = presenceFindings.OrderBy(entry => entry.stableIdentity, StringComparer.Ordinal).ToList(),
                evidenceRows = orderedRows
            };

            if (!HasRequiredReportShape(JsonUtility.ToJson(report, true)))
                throw new InvalidOperationException("Camera/minimap ownership report failed required-shape validation.");
            return report;
        }

        internal static bool HasRequiredReportShape(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || HasVolatileOrLocalData(json))
                return false;

            try
            {
                ValidateDeclaredMethodSignatures();
                JObject root = JObject.Parse(json);
                if (!HasExactJsonSchema(root))
                    return false;

                OwnershipReport report = JsonUtility.FromJson<OwnershipReport>(json);
                if (report == null ||
                    !string.Equals(report.reportSchema, ReportSchema, StringComparison.Ordinal) ||
                    report.reportSchemaVersion != ReportSchemaVersion ||
                    !string.Equals(report.baselineCommit, BaselineCommit, StringComparison.Ordinal))
                    return false;

                ValidateInputHashes(report.directInputHashes);
                ValidateCrossReferences(report.crossReferences);
                ValidatePresenceFindings(report.presenceFindings);
                IReadOnlyDictionary<string, OwnershipSpec> specs = RowSpecs();
                if (report.evidenceRows == null || report.evidenceRows.Count != specs.Count ||
                    !HasStrictOrdering(report.evidenceRows, row => row.stableIdentity))
                    return false;

                for (int i = 0; i < report.evidenceRows.Count; i++)
                {
                    OwnershipRow row = report.evidenceRows[i];
                    if (row == null || !specs.TryGetValue(row.stableIdentity, out OwnershipSpec spec) ||
                        !RowMatchesSpec(row, spec))
                        return false;
                }

                int needsDecision = report.evidenceRows.Count(IsDecisionRow);
                if (report.counts == null || report.counts.evidenceRows != report.evidenceRows.Count ||
                    report.counts.shellOwned != Count(report.evidenceRows, OwnershipClassification.ShellOwned) ||
                    report.counts.mapOwned != Count(report.evidenceRows, OwnershipClassification.MapOwned) ||
                    report.counts.sharedConfig != Count(report.evidenceRows, OwnershipClassification.SharedConfig) ||
                    report.counts.temporaryCompatibility != Count(report.evidenceRows, OwnershipClassification.TemporaryCompatibility) ||
                    report.counts.mixed != Count(report.evidenceRows, OwnershipClassification.Mixed) ||
                    report.counts.unresolved != Count(report.evidenceRows, OwnershipClassification.Unresolved) ||
                    report.counts.needsDecision != needsDecision)
                    return false;

                return needsDecision > 0
                    ? string.Equals(report.result, "NeedsDecision", StringComparison.Ordinal)
                    : string.Equals(report.result, "Passed", StringComparison.Ordinal);
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is InvalidOperationException ||
                exception is NullReferenceException)
            {
                return false;
            }
        }

        internal static List<InputHashReport> CaptureAndValidateInputs(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("Project root is required.");

            IReadOnlyDictionary<string, SourceSpec> specs = SourceSpecs();
            var hashes = new List<InputHashReport>(specs.Count);
            foreach (KeyValuePair<string, SourceSpec> pair in specs.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                string fullPath = Path.Combine(projectRoot, pair.Key);
                if (!File.Exists(fullPath))
                    throw new InvalidOperationException("Required ownership source is missing: " + pair.Key);
                byte[] bytes = File.ReadAllBytes(fullPath);
                string text = Utf8WithoutBom.GetString(bytes);
                foreach (string requiredToken in pair.Value.requiredTokens)
                {
                    if (!text.Contains(requiredToken, StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            "Required ownership token is missing from " + pair.Key + ": " + requiredToken);
                }
                hashes.Add(new InputHashReport
                {
                    path = pair.Key,
                    sha256 = OperationMapPhase0BaselineProbe.ComputeSha256(bytes)
                });
            }

            ValidateInputHashes(hashes);
            return hashes;
        }

        internal static void PublishReportAtomically(
            ValidatedReportDestination destination,
            string json,
            Action beforeReplace = null)
        {
            if (destination == null)
                throw new InvalidOperationException("A validated camera/minimap report destination is required.");

            ValidateDestinationIdentity(destination);
            string outputName = Path.GetFileName(destination.canonicalPath);
            string mutexName = "WarlineOpmap007-" +
                               OperationMapPhase0BaselineProbe.ComputeSha256(
                                   Utf8WithoutBom.GetBytes(destination.canonicalPath));
            using var publicationMutex = new Mutex(false, mutexName);
            bool lockTaken = false;
            try
            {
                try
                {
                    lockTaken = publicationMutex.WaitOne(TimeSpan.FromSeconds(30));
                }
                catch (AbandonedMutexException)
                {
                    lockTaken = true;
                }
                if (!lockTaken)
                    throw new InvalidOperationException("Timed out waiting for exclusive camera/minimap report publication.");

                using PublicationDirectory directory = PublicationDirectory.Open(destination);
                directory.ValidateIdentity();
                InvalidateOutput(directory, outputName);
                if (!HasRequiredReportShape(json))
                    throw new InvalidOperationException("Refusing to publish an invalid camera/minimap ownership report.");

                string temporaryName = outputName + "." + Guid.NewGuid().ToString("N") + ".tmp";
                directory.WriteAllText(temporaryName, json);
                try
                {
                    if (!HasRequiredReportShape(directory.ReadAllText(temporaryName)))
                        throw new InvalidOperationException("Persisted camera/minimap ownership report is invalid.");

                    directory.ValidateIdentity();
                    beforeReplace?.Invoke();
                    directory.Replace(temporaryName, outputName);
                    if (!string.Equals(directory.ReadAllText(outputName), json, StringComparison.Ordinal))
                        throw new InvalidOperationException("Published camera/minimap ownership report bytes drifted.");
                    directory.ValidateIdentity();
                }
                catch
                {
                    directory.DeleteIfPresent(outputName);
                    throw;
                }
                finally
                {
                    directory.DeleteIfPresent(temporaryName);
                }
            }
            finally
            {
                if (lockTaken)
                    publicationMutex.ReleaseMutex();
            }
        }

        private static void InvalidateOutput(PublicationDirectory directory, string outputName)
        {
            directory.ValidateIdentity();
            directory.DeleteIfPresent(outputName);
            directory.DeleteIfPresent(outputName + ".tmp");
        }

        internal static void ValidateNoUnexpectedProducerCandidates(string projectRoot)
        {
            string scriptsRoot = Path.Combine(projectRoot, "Assets/Game/Scripts");
            if (!Directory.Exists(scriptsRoot))
                throw new InvalidOperationException("Runtime source audit root is missing: Assets/Game/Scripts");

            AuditCandidateToken(
                projectRoot,
                scriptsRoot,
                "MatchObjectiveRuntimeElement",
                new[]
                {
                    "Assets/Game/Scripts/Components/MatchObjectiveComponents.cs",
                    "Assets/Game/Scripts/UI/Shell/Ecs/AssistantObjectiveProjectionUtility.cs",
                    "Assets/Game/Scripts/UI/Shell/Ecs/AssistantReadModelSystems.cs",
                    "Assets/Game/Scripts/UI/Shell/Ecs/AssistantThreatReadModelSystem.cs",
                    "Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Assistant.cs"
                });
            AuditCandidateToken(
                projectRoot,
                scriptsRoot,
                "AssistantRecommendationKind.CameraFocus",
                new[]
                {
                    "Assets/Game/Scripts/Components/AssistantComponents.cs",
                    "Assets/Game/Scripts/UI/Shell/Ecs/AssistantObjectiveProjectionUtility.cs",
                    "Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Actions.cs"
                });
        }

        private static List<OwnershipRow> BuildRows()
        {
            return RowSpecs().Select(pair => new OwnershipRow
                {
                    stableIdentity = pair.Key,
                    subject = pair.Value.subject,
                    currentAuthority = pair.Value.currentAuthority,
                    currentType = pair.Value.currentType,
                    classification = pair.Value.classification.ToString(),
                    evidencePaths = pair.Value.evidencePaths.OrderBy(path => path, StringComparer.Ordinal).ToList(),
                    rationale = pair.Value.rationale,
                    migrationDisposition = pair.Value.migrationDisposition,
                    decisionOwner = pair.Value.decisionOwner
                })
                .OrderBy(row => row.stableIdentity, StringComparer.Ordinal)
                .ToList();
        }

        private static List<PresenceFinding> BuildPresenceFindings()
        {
            return PresenceSpecs().Select(pair => new PresenceFinding
                {
                    stableIdentity = pair.Key,
                    status = pair.Value.status,
                    currentAuthority = pair.Value.currentAuthority,
                    currentType = pair.Value.currentType,
                    evidencePaths = pair.Value.evidencePaths.OrderBy(path => path, StringComparer.Ordinal).ToList(),
                    rationale = pair.Value.rationale,
                    decisionOwner = pair.Value.decisionOwner
                })
                .OrderBy(row => row.stableIdentity, StringComparer.Ordinal)
                .ToList();
        }

        private static List<CrossReferenceReport> BuildCrossReferences(
            IReadOnlyList<InputHashReport> hashes)
        {
            Dictionary<string, string> hashByPath = hashes.ToDictionary(
                entry => entry.path,
                entry => entry.sha256,
                StringComparer.Ordinal);
            return new List<CrossReferenceReport>
            {
                new()
                {
                    taskId = "opmap-002",
                    reportSchema = OperationMapPhase0BaselineProbe.ReportSchema,
                    reportSchemaVersion = OperationMapPhase0BaselineProbe.ReportSchemaVersion,
                    result = "Passed",
                    evidencePath = Opmap002Path,
                    evidenceSha256 = hashByPath[Opmap002Path]
                },
                new()
                {
                    taskId = "opmap-004",
                    reportSchema = OperationMapPhase0OwnershipProbe.ReportSchema,
                    reportSchemaVersion = OperationMapPhase0OwnershipProbe.ReportSchemaVersion,
                    result = "NeedsDecision",
                    evidencePath = Opmap004Path,
                    evidenceSha256 = hashByPath[Opmap004Path]
                }
            };
        }

        private static OwnershipCounts BuildCounts(IReadOnlyList<OwnershipRow> rows, int needsDecision)
        {
            rows ??= Array.Empty<OwnershipRow>();
            return new OwnershipCounts
            {
                evidenceRows = rows.Count,
                shellOwned = Count(rows, OwnershipClassification.ShellOwned),
                mapOwned = Count(rows, OwnershipClassification.MapOwned),
                sharedConfig = Count(rows, OwnershipClassification.SharedConfig),
                temporaryCompatibility = Count(rows, OwnershipClassification.TemporaryCompatibility),
                mixed = Count(rows, OwnershipClassification.Mixed),
                unresolved = Count(rows, OwnershipClassification.Unresolved),
                needsDecision = needsDecision
            };
        }

        private static int Count(IReadOnlyList<OwnershipRow> rows, OwnershipClassification classification)
        {
            string expected = classification.ToString();
            return rows.Count(row => string.Equals(row.classification, expected, StringComparison.Ordinal));
        }

        private static bool RowMatchesSpec(OwnershipRow row, OwnershipSpec spec)
        {
            string expectedClassification = spec.classification.ToString();
            bool decision = spec.classification == OwnershipClassification.Mixed ||
                            spec.classification == OwnershipClassification.Unresolved;
            return string.Equals(row.subject, spec.subject, StringComparison.Ordinal) &&
                   string.Equals(row.currentAuthority, spec.currentAuthority, StringComparison.Ordinal) &&
                   string.Equals(row.currentType, spec.currentType, StringComparison.Ordinal) &&
                   string.Equals(row.classification, expectedClassification, StringComparison.Ordinal) &&
                   SequenceEqual(row.evidencePaths, spec.evidencePaths.OrderBy(path => path, StringComparer.Ordinal)) &&
                   string.Equals(row.rationale, spec.rationale, StringComparison.Ordinal) &&
                   string.Equals(row.migrationDisposition, spec.migrationDisposition, StringComparison.Ordinal) &&
                   string.Equals(row.decisionOwner, spec.decisionOwner, StringComparison.Ordinal) &&
                   (decision ? !string.IsNullOrWhiteSpace(row.decisionOwner) : string.IsNullOrEmpty(row.decisionOwner));
        }

        private static bool IsDecisionRow(OwnershipRow row)
        {
            return row != null &&
                   (string.Equals(row.classification, OwnershipClassification.Mixed.ToString(), StringComparison.Ordinal) ||
                    string.Equals(row.classification, OwnershipClassification.Unresolved.ToString(), StringComparison.Ordinal));
        }

        private static void ValidateInputHashes(IReadOnlyList<InputHashReport> hashes)
        {
            IReadOnlyDictionary<string, SourceSpec> expected = SourceSpecs();
            if (hashes == null || hashes.Count != expected.Count ||
                !HasStrictOrdering(hashes, hash => hash.path))
                throw new InvalidOperationException("Direct input hash set is incomplete or unordered.");

            foreach (InputHashReport hash in hashes)
            {
                if (hash == null || !expected.TryGetValue(hash.path, out SourceSpec spec) ||
                    !string.Equals(hash.sha256, spec.sha256, StringComparison.Ordinal))
                    throw new InvalidOperationException("Direct input hash set contains stale evidence.");
            }
        }

        private static void ValidateCrossReferences(IReadOnlyList<CrossReferenceReport> references)
        {
            IReadOnlyDictionary<string, CrossReferenceSpec> expected = CrossReferenceSpecs();
            if (references == null || references.Count != expected.Count ||
                !HasStrictOrdering(references, entry => entry.taskId))
                throw new InvalidOperationException("Cross-reference set is incomplete or unordered.");

            foreach (CrossReferenceReport reference in references)
            {
                if (reference == null || !expected.TryGetValue(reference.taskId, out CrossReferenceSpec spec) ||
                    !string.Equals(reference.reportSchema, spec.reportSchema, StringComparison.Ordinal) ||
                    reference.reportSchemaVersion != spec.reportSchemaVersion ||
                    !string.Equals(reference.result, spec.result, StringComparison.Ordinal) ||
                    !string.Equals(reference.evidencePath, spec.evidencePath, StringComparison.Ordinal) ||
                    !string.Equals(reference.evidenceSha256, SourceSpecs()[spec.evidencePath].sha256, StringComparison.Ordinal))
                    throw new InvalidOperationException("Cross-reference evidence is stale or unsupported.");
            }
        }

        private static void ValidatePresenceFindings(IReadOnlyList<PresenceFinding> findings)
        {
            IReadOnlyDictionary<string, PresenceSpec> expected = PresenceSpecs();
            if (findings == null || findings.Count != expected.Count ||
                !HasStrictOrdering(findings, entry => entry.stableIdentity))
                throw new InvalidOperationException("Presence findings are incomplete or unordered.");

            foreach (PresenceFinding finding in findings)
            {
                if (finding == null || !expected.TryGetValue(finding.stableIdentity, out PresenceSpec spec) ||
                    !string.Equals(finding.status, spec.status, StringComparison.Ordinal) ||
                    !string.Equals(finding.currentAuthority, spec.currentAuthority, StringComparison.Ordinal) ||
                    !string.Equals(finding.currentType, spec.currentType, StringComparison.Ordinal) ||
                    !SequenceEqual(finding.evidencePaths, spec.evidencePaths.OrderBy(path => path, StringComparer.Ordinal)) ||
                    !string.Equals(finding.rationale, spec.rationale, StringComparison.Ordinal) ||
                    !string.Equals(finding.decisionOwner, spec.decisionOwner, StringComparison.Ordinal) ||
                    (string.Equals(finding.status, "Unresolved", StringComparison.Ordinal)
                        ? string.IsNullOrWhiteSpace(finding.decisionOwner)
                        : !string.IsNullOrEmpty(finding.decisionOwner)))
                    throw new InvalidOperationException("Presence finding drifted from current evidence.");
            }
        }

        private static bool HasStrictOrdering<T>(IReadOnlyList<T> rows, Func<T, string> key)
        {
            if (rows == null)
                return false;
            string previous = null;
            for (int i = 0; i < rows.Count; i++)
            {
                if (ReferenceEquals(rows[i], null))
                    return false;
                string current = key(rows[i]);
                if (string.IsNullOrWhiteSpace(current) ||
                    (previous != null && string.CompareOrdinal(previous, current) >= 0))
                    return false;
                previous = current;
            }
            return true;
        }

        private static bool SequenceEqual(IEnumerable<string> actual, IEnumerable<string> expected)
        {
            return actual != null && actual.SequenceEqual(expected, StringComparer.Ordinal);
        }

        private static void RequireInputHashesEqual(
            IReadOnlyList<InputHashReport> before,
            IReadOnlyList<InputHashReport> after)
        {
            if (before == null || after == null || before.Count != after.Count)
                throw new InvalidOperationException("Direct input set changed during inspection.");
            for (int i = 0; i < before.Count; i++)
            {
                if (!string.Equals(before[i].path, after[i].path, StringComparison.Ordinal) ||
                    !string.Equals(before[i].sha256, after[i].sha256, StringComparison.Ordinal))
                    throw new InvalidOperationException("Direct input changed during inspection: " + before[i].path);
            }
        }

        private static bool HasVolatileOrLocalData(string json)
        {
            string[] forbidden =
            {
                "timestamp", "timeStamp", "sessionId", "sessionID", "reportPath",
                "/Users/", "\\\\Users\\\\", "WarlineCapture-Worktrees", "/private/tmp"
            };
            return forbidden.Any(token => json.Contains(token, StringComparison.Ordinal));
        }

        private static bool HasExactJsonSchema(JObject root)
        {
            return HasExactProperties(root,
                       "reportSchema", "reportSchemaVersion", "baselineCommit", "result", "counts",
                       "crossReferences", "directInputHashes", "presenceFindings", "evidenceRows") &&
                   HasExactObject(root["counts"],
                       "evidenceRows", "shellOwned", "mapOwned", "sharedConfig",
                       "temporaryCompatibility", "mixed", "unresolved", "needsDecision") &&
                   HasExactObjectArray(root["crossReferences"],
                       "taskId", "reportSchema", "reportSchemaVersion", "result", "evidencePath", "evidenceSha256") &&
                   HasExactObjectArray(root["directInputHashes"], "path", "sha256") &&
                   HasExactObjectArray(root["presenceFindings"],
                       "stableIdentity", "status", "currentAuthority", "currentType",
                       "evidencePaths", "rationale", "decisionOwner") &&
                   HasExactObjectArray(root["evidenceRows"],
                       "stableIdentity", "subject", "currentAuthority", "currentType", "classification",
                       "evidencePaths", "rationale", "migrationDisposition", "decisionOwner");
        }

        private static bool HasExactObject(JToken token, params string[] propertyNames)
        {
            return token is JObject value && HasExactProperties(value, propertyNames);
        }

        private static bool HasExactObjectArray(JToken token, params string[] propertyNames)
        {
            return token is JArray values &&
                   values.All(value => value is JObject item && HasExactProperties(item, propertyNames));
        }

        private static bool HasExactProperties(JObject value, params string[] propertyNames)
        {
            string[] actual = value.Properties().Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            string[] expected = propertyNames.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            return actual.SequenceEqual(expected, StringComparer.Ordinal);
        }

        private static void AuditCandidateToken(
            string projectRoot,
            string scriptsRoot,
            string token,
            IEnumerable<string> allowedPaths)
        {
            var allowed = new HashSet<string>(allowedPaths, StringComparer.Ordinal);
            string[] unexpected = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories)
                .Select(path => new
                {
                    FullPath = path,
                    RelativePath = Path.GetRelativePath(projectRoot, path).Replace('\\', '/')
                })
                .Where(entry => !entry.RelativePath.StartsWith("Assets/Game/Scripts/Editor/", StringComparison.Ordinal))
                .Where(entry => File.ReadAllText(entry.FullPath, Utf8WithoutBom).Contains(token, StringComparison.Ordinal))
                .Select(entry => entry.RelativePath)
                .Where(path => !allowed.Contains(path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (unexpected.Length > 0)
                throw new InvalidOperationException(
                    "New producer candidate found while auditing token `" + token + "`: " +
                    string.Join(", ", unexpected));
        }

        private static string ResolveExistingDirectory(string path)
        {
            string fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath))
                throw new InvalidOperationException("Cannot resolve missing output-containment directory: " + fullPath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                for (DirectoryInfo current = new(fullPath); current != null; current = current.Parent)
                {
                    if ((current.Attributes & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidOperationException(
                            "Cannot safely resolve a Windows reparse point in report output containment: " + current.FullName);
                }
                return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            IntPtr resolved = RealPath(fullPath, IntPtr.Zero);
            if (resolved == IntPtr.Zero)
                throw new InvalidOperationException("Failed to resolve report output containment path: " + fullPath);
            try
            {
                return Marshal.PtrToStringAnsi(resolved)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            finally
            {
                Free(resolved);
            }
        }

        private static bool IsSameOrDescendant(string candidate, string root)
        {
            StringComparison comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (string.Equals(candidate, root, comparison))
                return true;
            string rootWithSeparator = root + Path.DirectorySeparatorChar;
            return candidate.StartsWith(rootWithSeparator, comparison);
        }

        private static void ValidateDestinationIdentity(ValidatedReportDestination destination)
        {
            string currentRequestedParent = ResolveExistingDirectory(destination.requestedParent);
            if (IsSameOrDescendant(currentRequestedParent, destination.canonicalProjectRoot))
                throw new InvalidOperationException("Camera/minimap report output now resolves inside the Unity project.");
            if (!PathsEqual(currentRequestedParent, destination.canonicalParent))
                throw new InvalidOperationException("Camera/minimap report output parent identity changed after validation.");

            string currentCanonicalParent = ResolveExistingDirectory(destination.canonicalParent);
            if (!PathsEqual(currentCanonicalParent, destination.canonicalParent) ||
                !PathsEqual(
                    destination.canonicalPath,
                    Path.Combine(destination.canonicalParent, Path.GetFileName(destination.canonicalPath))))
            {
                throw new InvalidOperationException("Camera/minimap canonical publication destination changed after validation.");
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(
                left,
                right,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal);
        }

        internal static void ValidateDeclaredMethodSignatures()
        {
            MethodInfo[] declarations = AssistantCommandIntentAdapterType?
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(method => string.Equals(
                    method.Name,
                    "ToAssistantCommandIntentKind",
                    StringComparison.Ordinal))
                .ToArray();
            if (declarations == null || declarations.Length != 1)
                throw new InvalidOperationException("Expected exactly one ToAssistantCommandIntentKind declaration.");

            MethodInfo declaration = declarations[0];
            Type[] parameterTypes = declaration.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();
            Type[] expectedParameterTypes =
            {
                typeof(Game.UI.Contracts.UiAssistantCommandIntentKind),
                typeof(Game.Components.AssistantRecommendationKind)
            };
            if (declaration.ReturnType != typeof(Game.Components.AssistantCommandIntentKind) ||
                !parameterTypes.SequenceEqual(expectedParameterTypes))
            {
                throw new InvalidOperationException("ToAssistantCommandIntentKind declaration no longer matches the pinned signature.");
            }

            string declaredIdentity = StableDeclaringTypeName(declaration.DeclaringType) +
                                      "::" + declaration.Name + "(" +
                                      string.Join(",", parameterTypes.Select(type => type.FullName)) + ")";
            if (!string.Equals(
                    declaredIdentity,
                    AssistantCommandIntentMappingIdentity,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("ToAssistantCommandIntentKind stable identity drifted from its declaration.");
            }

            MethodInfo[] bakeDeclarations = GridBakerType?
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => string.Equals(method.Name, "Bake", StringComparison.Ordinal))
                .ToArray();
            if (bakeDeclarations == null || bakeDeclarations.Length != 1)
                throw new InvalidOperationException("Expected exactly one GridBaker.Bake declaration.");

            MethodInfo bakeDeclaration = bakeDeclarations[0];
            Type[] bakeParameterTypes = bakeDeclaration.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();
            if (bakeDeclaration.ReturnType != typeof(void) ||
                bakeParameterTypes.Length != 1 ||
                bakeParameterTypes[0] != typeof(Game.Authoring.GridAuthoring))
            {
                throw new InvalidOperationException("GridBaker.Bake no longer matches the pinned signature.");
            }

            string declaredBakeIdentity = StableDeclaringTypeName(bakeDeclaration.DeclaringType) +
                                          "::" + bakeDeclaration.Name + "(" +
                                          string.Join(",", bakeParameterTypes.Select(type => type.FullName)) + ")";
            if (!string.Equals(declaredBakeIdentity, GridBakeIdentity, StringComparison.Ordinal))
                throw new InvalidOperationException("GridBaker.Bake stable identity drifted from its declaration.");
        }

        private static string StableDeclaringTypeName(Type type)
        {
            if (type == null || string.IsNullOrWhiteSpace(type.FullName))
                throw new InvalidOperationException("Unable to resolve a stable declaring type name.");
            return type.FullName.Replace('+', '.');
        }

        [DllImport("libc", EntryPoint = "realpath", SetLastError = true)]
        private static extern IntPtr RealPath(string path, IntPtr buffer);

        [DllImport("libc", EntryPoint = "free")]
        private static extern void Free(IntPtr pointer);

        [DllImport("libc", EntryPoint = "open", SetLastError = true)]
        private static extern int UnixOpen(string path, int flags);

        [DllImport("libc", EntryPoint = "openat", SetLastError = true)]
        private static extern int UnixOpenAt(int directoryDescriptor, string path, int flags, int mode);

        [DllImport("libc", EntryPoint = "renameat", SetLastError = true)]
        private static extern int UnixRenameAt(
            int oldDirectoryDescriptor,
            string oldPath,
            int newDirectoryDescriptor,
            string newPath);

        [DllImport("libc", EntryPoint = "unlinkat", SetLastError = true)]
        private static extern int UnixUnlinkAt(int directoryDescriptor, string path, int flags);

        [DllImport("libc", EntryPoint = "read", SetLastError = true)]
        private static extern IntPtr UnixRead(int descriptor, byte[] buffer, UIntPtr count);

        [DllImport("libc", EntryPoint = "write", SetLastError = true)]
        private static extern IntPtr UnixWrite(int descriptor, IntPtr buffer, UIntPtr count);

        [DllImport("libc", EntryPoint = "fsync", SetLastError = true)]
        private static extern int UnixFsync(int descriptor);

        [DllImport("libc", EntryPoint = "fchmod", SetLastError = true)]
        private static extern int UnixFchmod(int descriptor, int mode);

        [DllImport("libc", EntryPoint = "fstat", SetLastError = true)]
        private static extern int UnixFstat(int descriptor, IntPtr status);

        [DllImport("libc", EntryPoint = "stat", SetLastError = true)]
        private static extern int UnixStat(string path, IntPtr status);

        [DllImport("libc", EntryPoint = "close", SetLastError = true)]
        private static extern int UnixClose(int descriptor);

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static string RequireProjectRoot()
        {
            DirectoryInfo parent = Directory.GetParent(Application.dataPath);
            if (parent == null)
                throw new InvalidOperationException("Unable to resolve Unity project root.");
            return parent.FullName;
        }

        private static IReadOnlyDictionary<string, CrossReferenceSpec> CrossReferenceSpecs()
        {
            return new SortedDictionary<string, CrossReferenceSpec>(StringComparer.Ordinal)
            {
                ["opmap-002"] = new(
                    OperationMapPhase0BaselineProbe.ReportSchema,
                    OperationMapPhase0BaselineProbe.ReportSchemaVersion,
                    "Passed",
                    Opmap002Path),
                ["opmap-004"] = new(
                    OperationMapPhase0OwnershipProbe.ReportSchema,
                    OperationMapPhase0OwnershipProbe.ReportSchemaVersion,
                    "NeedsDecision",
                    Opmap004Path)
            };
        }

        private static IReadOnlyDictionary<string, PresenceSpec> PresenceSpecs()
        {
            return new SortedDictionary<string, PresenceSpec>(StringComparer.Ordinal)
            {
                ["initial-focus-producer"] = new(
                    "Present",
                    "Game.Runtime.InitialUnitsSpawnSystem::ProcessInitialBuildingCompletion(Unity.Entities.EntityManager,Unity.Entities.Entity,Unity.Entities.Entity,Game.Components.GridConfig,int,ref Game.Runtime.InitialUnitsSpawnSystem.InitialSpawnDiagnosticLogWriter)",
                    "Legacy static write from ECS spawn completion",
                    new[] { "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs" },
                    "A successful player initial-base request writes the GridConfig-derived footprint center; current evidence does not support a no-producer finding.",
                    ""),
                ["objective-camera-focus-recommendation-producer"] = new(
                    "Present",
                    "Game.UI.Shell.Ecs.AssistantObjectiveProjectionUtility::TryBuildAnchorFocus",
                    "Objective operation-map anchor to CameraFocus recommendation",
                    new[]
                    {
                        "Assets/Game/Scripts/Components/AssistantComponents.cs",
                        "Assets/Game/Scripts/UI/Shell/Ecs/AssistantObjectiveProjectionUtility.cs",
                        "Assets/Game/Scripts/UI/Shell/Ecs/AssistantReadModelSystems.cs",
                        "Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Actions.cs"
                    },
                    "The typed objective anchor fallback publishes CameraFocus only when no entity, cell, or world-position target is available.",
                    ""),
                ["runtime-objective-writer"] = new(
                    "Unresolved",
                    "No writer found in audited sources",
                    "MatchObjectiveRuntimeElement buffer and state contract only",
                    new[]
                    {
                        "Assets/Game/Scripts/Components/MatchObjectiveComponents.cs",
                        "Assets/Game/Scripts/UI/Shell/Ecs/AssistantReadModelSystems.cs",
                        "Assets/Game/Scripts/UI/Shell/Ecs/AssistantThreatReadModelSystem.cs",
                        "Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Assistant.cs"
                    },
                    "No writer found in audited sources. The audit covers every runtime C# source under Assets/Game/Scripts and fails closed on a new MatchObjectiveRuntimeElement reference.",
                    "Mission runtime owner and assistant architecture owner")
            };
        }

        private static IReadOnlyDictionary<string, OwnershipSpec> RowSpecs()
        {
            var rows = new SortedDictionary<string, OwnershipSpec>(StringComparer.Ordinal);
            AddRow(rows, "Assets/Game/Configs/Scene/Game_RTSSelection_Config.asset::worldCamera", "Initial camera override", "Shared RTS selection config asset; current serialized value is null", "UnityEngine.Camera serialized reference", OwnershipClassification.TemporaryCompatibility, new[] { "Assets/Game/Configs/Scene/Game_RTSSelection_Config.asset", Opmap004Path }, "The scene camera remains the effective source, but shared config can override it if populated.", "RetireCameraReferenceOverride", "");
            AddRow(rows, "Assets/Game/Scenes/Match.unity::Main Camera[5]|type:UnityEngine.Camera", "Initial camera source", "Match shell scene", "UnityEngine.Camera at localId 1220593093; transform position (870.0283,42.030247,325.60086)", OwnershipClassification.ShellOwned, new[] { "Assets/Game/Scenes/Match.unity", Opmap004Path }, "opmap-004 pins this exact shell-owned camera identity and the scene supplies the initial transform.", "KeepInMatchShell", "");
            AddRow(rows, "Assets/Game/Scenes/Match/MatchSubScene.unity::Grid[0]", "Grid bounds authoring source", "Operation-map subscene", "Game.Authoring.GridAuthoring", OwnershipClassification.MapOwned, new[] { "Assets/Game/Scenes/Match/MatchSubScene.unity", Opmap004Path }, "opmap-004 already identifies this exact map-owned grid root; this probe follows its runtime projection instead of rescanning ownership.", "MoveWithOperationMapSubScene", "");
            AddRow(rows, GridBakeIdentity, "Grid bounds bake", "Grid authoring baker", "Unity.Entities.Baker<GridAuthoring> -> Game.Components.GridConfig", OwnershipClassification.MapOwned, new[] { "Assets/Game/Scripts/Authorings/GridAuthoring.cs", "Assets/Game/Scripts/Components/GridComponents.cs" }, "Width, height, cell size, and origin are baked into the ECS grid singleton.", "BakeIntoOperationMapMetadata", "");
            AddRow(rows, "Game.Components.GridConfig", "Runtime grid bounds authority", "ECS map metadata singleton", "Unity.Entities.IComponentData", OwnershipClassification.MapOwned, new[] { "Assets/Game/Scripts/Components/GridComponents.cs" }, "Camera clamp and minimap projection both derive world bounds from this component.", "PublishFromOperationMapMetadata", "");
            AddRow(rows, "Game.Composition.MatchHudMinimapDataSourceAdapter::TryGetGrid(out Game.UI.Contracts.MatchHudMinimapGridModel)", "Minimap grid projection source", "Managed UI adapter preferring active operation-map minimap metadata", "Active OperationMapMinimapBlob projection with GridConfig compatibility fallback", OwnershipClassification.MapOwned, new[] { "Assets/Game/Scripts/Composition/MatchHudMinimapDataSourceAdapter.cs", "Assets/Game/Scripts/Components/GridComponents.cs", "Assets/Game/Scripts/Components/OperationMapComponents.cs" }, "The adapter preserves active-map minimap origin and exact fractional extents while retaining the current grid fallback when active metadata is unavailable or unsupported.", "KeepActiveMapProjectionWithCompatibilityFallback", "");
            AddRow(rows, "Game.Runtime.InitialUnitsSpawnSystem::ProcessInitialBuildingCompletion(Unity.Entities.EntityManager,Unity.Entities.Entity,Unity.Entities.Entity,Game.Components.GridConfig,int,ref Game.Runtime.InitialUnitsSpawnSystem.InitialSpawnDiagnosticLogWriter)", "Initial focus producer", "Initial spawn ECS system writing legacy static state", "Game.Runtime.InitialUnitsSpawnSystem", OwnershipClassification.Mixed, new[] { "Assets/Game/Scripts/RuntimeState/InitialUnitsRuntimeState.cs", "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs" }, "The initial player-base footprint is scenario-derived while its world center uses map GridConfig; the producer bypasses the ECS focus-request writer.", "DecisionRequired", "Gameplay scenario owner and camera architecture owner");
            AddRow(rows, "Game.Runtime.RtsCameraRequestSystem::ProcessPendingRequests(Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)", "Tactical-follow clamp policy", "Camera request ECS bridge", "Unity.Entities.SystemBase", OwnershipClassification.Mixed, new[] { "Assets/Game/Scripts/Components/RtsCameraRequestComponents.cs", "Assets/Game/Scripts/Components/TacticalFollowCameraComponents.cs", "Assets/Game/Scripts/Systems/RtsCameraRequestSystem.cs", "Assets/Game/Scripts/Systems/RtsCameraSystem.cs", "Assets/Game/Scripts/Systems/TacticalFollowCameraModeSystemHelper.cs" }, "A valid tactical-follow pose suppresses normal camera requests and deliberately skips GridConfig boundary clamp while applying the follow pose.", "DecisionRequired", "Camera design owner and operation-map architecture owner");
            AddRow(rows, "Game.Runtime.RtsCameraRequestSystem::SyncGroundBoundary(Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)", "Camera boundary projection", "Camera request ECS bridge preferring active operation-map camera metadata", "OperationMapCameraBoundsComponent or GridConfig fallback -> UnityEngine.Rect -> RtsCameraSystem", OwnershipClassification.Mixed, new[] { "Assets/Game/Scripts/Components/GridComponents.cs", "Assets/Game/Scripts/Components/OperationMapComponents.cs", "Assets/Game/Scripts/Systems/RtsCameraRequestSystem.cs" }, "Map-owned active camera bounds are projected into mutable shell camera state; GridConfig remains an explicit compatibility fallback.", "DecisionRequired", "Camera architecture owner and operation-map architecture owner");
            AddRow(rows, "Game.Runtime.RtsCameraSystem::ClampCameraToGroundBoundary(UnityEngine.Camera)", "Camera footprint clamp", "Managed shell camera system", "Unity.Entities.SystemBase with UnityEngine.Camera mutation", OwnershipClassification.ShellOwned, new[] { "Assets/Game/Scripts/Systems/RtsCameraSystem.cs" }, "The shell camera fits and offsets its visible ground footprint within the boundary supplied by GridConfig.", "KeepShellConsumerBindMapBounds", "");
            AddRow(rows, "Game.Runtime.RtsSelectionRuntimeCameraSystemHelper::ConsumeInitialCameraFocusRequest(Game.Runtime.RtsSelectionRuntimeCameraSystemHelper.Context)", "Initial focus consumer", "Managed selection camera helper", "RuntimeGameplayStateSystem -> RtsCameraRequestElement.MoveGroundCenterTo", OwnershipClassification.ShellOwned, new[] { "Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs", "Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs" }, "The shell consumes the one-shot focus request and routes it through the camera request queue and grid clamp.", "KeepShellConsumer", "");
            AddRow(rows, "Game.Runtime.RuntimeGameplayStateSystem::InitialCameraFocusRequested/InitialCameraFocusWorld", "Initial focus override chain", "Disabled ECS facade mirroring InitialUnitsRuntimeState", "RuntimeCameraFocusRequestComponent plus legacy static mirror", OwnershipClassification.TemporaryCompatibility, new[] { "Assets/Game/Scripts/Components/RuntimeGameplayStateComponents.cs", "Assets/Game/Scripts/RuntimeState/InitialUnitsRuntimeState.cs", "Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs" }, "The public ECS facade exists, but the current producer writes the legacy static fields and the facade mirrors them on read.", "ReplaceLegacyMirrorWithEcsRequestProducer", "");
            AddRow(rows, "Game.Runtime.SelectionRuntimeConfigStartupSystemHelper::CreateStateFromConfig(Game.Configs.RTSSelectionSystemConfig,UnityEngine.Camera)", "Camera source and override resolution", "Managed selection startup helper", "Fallback MatchSceneView camera overridden by RTSSelectionSystemConfig.WorldCamera when non-null", OwnershipClassification.Mixed, new[] { "Assets/Game/Configs/Scene/Game_RTSSelection_Config.asset", "Assets/Game/Scripts/Systems/SelectionRuntimeConfigStartupSystemHelper.cs" }, "Current config is null and the shell camera wins, but the shared config contract can replace the camera identity.", "DecisionRequired", "Camera architecture owner");
            AddRow(rows, "Game.Runtime.TacticalFollowCameraModeSystemHelper::ClampDesiredPosition(Game.Components.TacticalFollowCameraTargetComponent,Unity.Mathematics.float3,Unity.Mathematics.float3)", "Tactical-follow local clamp", "Managed tactical-follow camera helper", "Target-clearance clamp over float3 pose", OwnershipClassification.ShellOwned, new[] { "Assets/Game/Scripts/Components/TacticalFollowCameraComponents.cs", "Assets/Game/Scripts/Systems/TacticalFollowCameraModeSystemHelper.cs" }, "The method enforces height and target clearance only; it does not clamp X/Z to operation-map bounds.", "KeepShellFramingPolicy", "");
            AddRow(rows, "Game.UI.Runtime.MatchHudMinimapInputUiSystemHelper::HandleFocusRequested(UnityEngine.Vector2)", "Minimap interaction clamp", "Managed UI input helper", "Projection normalized point -> GridConfig-clamped world point -> camera request", OwnershipClassification.ShellOwned, new[] { "Assets/Game/Scripts/UI/Screens/MatchHudMinimapInputUiSystemHelper.cs", "Assets/Game/Scripts/UI/Screens/MatchHudMinimapProjectionUiSystemHelper.cs" }, "Minimap interaction clamps the requested world focus back to canonical grid bounds before moving the shell camera.", "KeepShellInteractionBindMapBounds", "");
            AddRow(rows, "Game.UI.Runtime.MatchHudMinimapInputUiSystemHelper::Update()", "Compact/full-map projection selector", "Managed UI input helper", "Runtime branch over stable full-grid, expanded full-map, and camera-centered compact grids", OwnershipClassification.ShellOwned, new[] { "Assets/Game/Scripts/UI/Screens/MatchHudMinimapInputUiSystemHelper.cs" }, "Compact mode follows a camera-centered window; full-map mode may expand to include the camera footprint; stable mode uses exact grid bounds.", "BindProjectionPolicyFromOperationMapMinimapConfig", "");
            AddRow(rows, "Game.UI.Runtime.MatchHudMinimapProjectionUiSystemHelper::CreateCameraCenteredGrid(Game.UI.Contracts.MatchHudMinimapGridModel,UnityEngine.Camera,float)", "Compact minimap projection", "Managed UI projection helper", "Camera viewport-derived local projection grid", OwnershipClassification.ShellOwned, new[] { "Assets/Game/Scripts/UI/Screens/MatchHudMinimapProjectionUiSystemHelper.cs" }, "Compact projection centers on the camera and uses fixed 160-unit/4.5x window policy capped to map dimensions.", "MoveConstantsToOperationMapMinimapConfig", "");
            AddRow(rows, "Game.UI.Runtime.MatchHudMinimapProjectionUiSystemHelper::CreateFullGridIncludingCamera(Game.UI.Contracts.MatchHudMinimapGridModel,UnityEngine.Camera)", "Expanded full-map bounds", "Managed UI projection helper", "Union of GridConfig world rectangle and camera ground footprint", OwnershipClassification.Mixed, new[] { "Assets/Game/Scripts/UI/Screens/MatchHudMinimapProjectionUiSystemHelper.cs" }, "Full-map projection can exceed canonical map bounds to keep an out-of-bounds camera footprint visible; migration behavior requires product and architecture approval.", "DecisionRequired", "Camera design owner and operation-map architecture owner");
            AddRow(rows, "Game.UI.Shell.Ecs.AssistantCommandIntentSystem::QueueCameraPreview(ref Unity.Entities.SystemState,Unity.Mathematics.float3)", "Assistant camera-focus execution", "ECS assistant command intent system", "RtsCameraRequestElement.SetSmoothFocusTarget", OwnershipClassification.ShellOwned, new[] { "Assets/Game/Scripts/Components/RtsCameraRequestComponents.cs", "Assets/Game/Scripts/UI/Shell/Ecs/AssistantCommandIntentSystem.cs" }, "A valid focus intent queues smooth camera focus and clearing of drag state; RtsCameraSystem later clamps the target to active operation-map camera bounds or the compatibility grid fallback.", "KeepEcsIntentToShellCameraRequest", "");
            AddRow(rows, "Game.UI.Shell.Ecs.AssistantPreviewTargetUtility::TryResolve(Unity.Entities.EntityManager,Unity.Entities.EntityQuery,in Game.Components.AssistantCommandIntentRequestElement,out Unity.Mathematics.float3)", "Assistant focus target resolution", "Pure ECS utility reading active operation-map metadata on pending requests", "World position, LocalTransform position, or typed OperationMapAnchorBlob resolver", OwnershipClassification.ShellOwned, new[] { "Assets/Game/Scripts/Components/OperationMapComponents.cs", "Assets/Game/Scripts/Systems/OperationMapMetadataUtility.cs", "Assets/Game/Scripts/UI/Shell/Ecs/AssistantCommandIntentSystem.cs", "Assets/Game/Scripts/UI/Shell/Ecs/AssistantPreviewTargetUtility.cs" }, "Objective focus resolves a stable anchor id only from generation-matched, ready active-map metadata and rejects missing, wrong-kind, failed, or non-finite anchors.", "KeepTypedMapAnchorConsumer", "");
            AddRow(rows, "Game.UI.Shell.Ecs.AssistantGoalReadModelSystem::ToGoal(Game.Components.MatchObjectiveRuntimeElement,uint)", "Objective-to-assistant projection", "ECS assistant goal read-model system", "MatchObjectiveRuntimeElement -> AssistantGoalReadModelElement", OwnershipClassification.Unresolved, new[] { "Assets/Game/Scripts/Components/MatchObjectiveComponents.cs", "Assets/Game/Scripts/UI/Shell/Ecs/AssistantObjectiveProjectionUtility.cs", "Assets/Game/Scripts/UI/Shell/Ecs/AssistantReadModelSystems.cs" }, "No writer found in audited sources for runtime objective rows or active objective state; the projection remains decision-owned.", "DecisionRequired", "Mission runtime owner and assistant architecture owner");
            AddRow(rows, "Game.UI.Shell.Ecs.AssistantRecommendationSystem::BuildRecommendation(Unity.Entities.DynamicBuffer<Game.Components.AssistantGoalReadModelElement>,Unity.Entities.DynamicBuffer<Game.Components.AssistantThreatReadModelElement>,Game.Components.FocusedUnitUiReadModelComponent,Game.Components.BuildingRuntimeFactionUsableFuelSummary,uint)", "Objective recommendation projection", "ECS assistant recommendation system with pure objective projection utility", "Objective goal -> Attack, Move, or typed operation-map CameraFocus recommendation", OwnershipClassification.ShellOwned, new[] { "Assets/Game/Scripts/UI/Shell/Ecs/AssistantObjectiveProjectionUtility.cs", "Assets/Game/Scripts/UI/Shell/Ecs/AssistantReadModelSystems.cs", "Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Actions.cs" }, "Entity, cell, and world-position targets retain precedence; an anchor-only objective emits a non-executable Show Me recommendation carrying the stable anchor id.", "KeepTypedObjectiveRecommendationPolicy", "");
            AddRow(rows, AssistantCommandIntentMappingIdentity, "Assistant camera-focus UI mapping", "UI shell ECS gateway", "AssistantRecommendationKind.CameraFocus -> AssistantCommandIntentKind.FocusCamera", OwnershipClassification.ShellOwned, new[] { "Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Actions.cs" }, "The UI-to-ECS mapping copies the stable target id into the command request for request-time active-map resolution.", "KeepTypedCameraFocusMapping", "");
            return rows;
        }

        private static IReadOnlyDictionary<string, SourceSpec> SourceSpecs()
        {
            var sources = new SortedDictionary<string, SourceSpec>(StringComparer.Ordinal);
            AddSource(sources, "Assets/Game/Configs/Scene/Game_RTSSelection_Config.asset", "8ae86a1940658a3693fb86dc76afb4988f816371171c112cd8f6835da40b4041", "worldCamera: {fileID: 0}");
            AddSource(sources, "Assets/Game/Scenes/Match.unity", "182f3b4cb50f48e1a573e1e90ee0c13baf9d62fce46e35b1850ef72097db5d75", "m_Name: Main Camera", "m_LocalPosition: {x: 870.0283, y: 42.030247, z: 325.60086}");
            AddSource(sources, "Assets/Game/Scenes/Match/MatchSubScene.unity", "bcc255f3fb140a0d91687b45b679b47fb60f01f5cfa8690bac3032ec642dadd8", "m_Name: Grid");
            AddSource(sources, "Assets/Game/Scripts/Authorings/GridAuthoring.cs", "5ac5169f0351d57ed44c89716614f177ac829d597f34780225da06eb0f4da348", "AddComponent(entity, new GridConfig");
            AddSource(sources, "Assets/Game/Scripts/Components/AssistantComponents.cs", "2636d4345d2242b421150e058875f119859989427f860e5c1fdcb2761c716d54", "CameraFocus = 6", "FocusCamera = 5");
            AddSource(sources, "Assets/Game/Scripts/Components/GridComponents.cs", "632d66e1479fa0b0773ea1635c29a26c5efadfcc998caf5151980d4d5e20cd39", "public struct GridConfig : IComponentData");
            AddSource(sources, "Assets/Game/Scripts/Components/MatchObjectiveComponents.cs", "46397bdd608efd572fc88e037b651f424ae1a39333b5da86b6f6f3bcd47d336c", "public struct MatchObjectiveRuntimeElement : IBufferElementData");
            AddSource(sources, "Assets/Game/Scripts/Components/OperationMapComponents.cs", "c5d1dc6fb6404893cfa99e226b8c62e909a99b347e0abf508f7416ccbb0b5e68", "public struct OperationMapAnchorBlob", "public struct ActiveOperationMapComponent : IComponentData");
            AddSource(sources, "Assets/Game/Scripts/Components/RtsCameraRequestComponents.cs", "30b9badf0516ab289699cf03611a65b1fabc43d524c4fbbb62a03340f0e6db6f", "public struct RtsCameraRequestElement : IBufferElementData");
            AddSource(sources, "Assets/Game/Scripts/Components/RuntimeGameplayStateComponents.cs", "f9dcc4fa99369b4aa8b2406dde892d4d7c3e542695e1037d1c7a08dbcf9cfcc9", "public struct RuntimeCameraFocusRequestComponent : IComponentData");
            AddSource(sources, "Assets/Game/Scripts/Components/TacticalFollowCameraComponents.cs", "b6e905e9bfa1140385a6903532392de79ec2fefe5a643984dd96ebc63f5f525d", "public struct TacticalFollowCameraPoseComponent : IComponentData");
            AddSource(sources, "Assets/Game/Scripts/Composition/MatchHudMinimapDataSourceAdapter.cs", "38e6ba48f43e12d19b122b2baa950953b071b91e5a43406213bc513ab20e7a02", "public bool TryGetGrid(out MatchHudMinimapGridModel grid)", "TryGetActiveMapProjection(em, out OperationMapMinimapBlob projection)");
            AddSource(sources, "Assets/Game/Scripts/RuntimeState/InitialUnitsRuntimeState.cs", "89afee7610468e9a4da4d36fbdb265966553b887c0df0f20f02b5fe725934544", "public static bool InitialCameraFocusRequested;");
            AddSource(sources, "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs", "76b91da24026174d7ee13fd5691dfd6f75192b779e2805e212d3949748657aca", "InitialUnitsRuntimeState.InitialCameraFocusRequested = true;");
            AddSource(sources, "Assets/Game/Scripts/Systems/OperationMapMetadataUtility.cs", "ab2c54f4f566616ffa4ab7f6c8fae84fc8f6babebd5d4a442900e01ecd0d420d", "public static bool TryFindAnchor(");
            AddSource(sources, "Assets/Game/Scripts/Systems/RtsCameraRequestSystem.cs", "429284516114bc58607a1fb3098f3ed2ed52259bf39c60f6e34f52b3060fe85e", "skipClamp: tacticalFollowPoseValid", "TryGetActiveOperationMapCameraBoundary(entityManager, out boundary)");
            AddSource(sources, "Assets/Game/Scripts/Systems/RtsCameraSystem.cs", "215f6e947fa074b8216899f280043de5d04a3978f82fabe34555708cc9644c9c", "public void ClampCameraToGroundBoundary(Camera worldCamera)", "focusWorldPosition = ClampGroundPositionToBoundary(focusWorldPosition);");
            AddSource(sources, "Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs", "012ed914ad46e349620422de90799acf3fb670b4a073753c1a3fcc99dd8f2e67", "private void ConsumeInitialCameraFocusRequest(Context context)");
            AddSource(sources, "Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs", "f7317c834bea00e556a905821a22ace77703141496041ee6859955946ce844c7", "public bool InitialCameraFocusRequested", "LegacyCameraFocusRequest()");
            AddSource(sources, "Assets/Game/Scripts/Systems/SelectionRuntimeConfigStartupSystemHelper.cs", "6190b548741689e982ce88b8055d3c090b14a9bd6ea7efee11a58cd458ebd768", "if (config.WorldCamera != null)", "state.WorldCamera = config.WorldCamera;");
            AddSource(sources, "Assets/Game/Scripts/Systems/TacticalFollowCameraModeSystemHelper.cs", "d51a65befab8d93232ec33d0cf19545960457970ef86a3bc6dd9008b56e3956c", "private static Unity.Mathematics.float3 ClampDesiredPosition(");
            AddSource(sources, "Assets/Game/Scripts/UI/Screens/MatchHudMinimapInputUiSystemHelper.cs", "ee833701ea4a2f8f6cddfbf64da8ea587896ae947d7ea6b37e8a53b94a93d5c1", "CreateFullGridIncludingCamera(grid, worldCamera)", "CreateCameraCenteredGrid(");
            AddSource(sources, "Assets/Game/Scripts/UI/Screens/MatchHudMinimapProjectionUiSystemHelper.cs", "96791301184e377a441405e7a0ddfe73fdd7c9ab4f7f666fcf8cacd008182bc3", "public static MatchHudMinimapProjectionGrid CreateFullGridIncludingCamera(", "public static MatchHudMinimapProjectionGrid CreateCameraCenteredGrid(");
            AddSource(sources, "Assets/Game/Scripts/UI/Shell/Ecs/AssistantCommandIntentSystem.cs", "342d96374779a5cdf2e3c12962e7882d47980a7e25e66e9a68f12d71e402b67d", "private void QueueCameraPreview(ref SystemState state, float3 focusWorldPosition)", "AssistantPreviewTargetUtility.TryResolve(");
            AddSource(sources, "Assets/Game/Scripts/UI/Shell/Ecs/AssistantObjectiveProjectionUtility.cs", "646060a8f68b8aaa021c8151456bd60155f49ba5650afce6685e71644db535d3", "TryBuildAnchorFocus(", "Kind = AssistantRecommendationKind.CameraFocus", "TargetId = goal.OperationMapAnchorId");
            AddSource(sources, "Assets/Game/Scripts/UI/Shell/Ecs/AssistantPreviewTargetUtility.cs", "9d1402974ce8f37c6646f5fcf41f8e82707a2c84c44238df75dd0204aa540406", "public static bool TryResolve(", "TryResolveObjectiveAnchor(");
            AddSource(sources, "Assets/Game/Scripts/UI/Shell/Ecs/AssistantReadModelSystems.cs", "469b9fd5a3b0cc34fed42a8144286b3fcb8dc4083b412e20c40b25c4dab29fdd", "private static AssistantGoalReadModelElement ToGoal(", "AssistantObjectiveProjectionUtility.TryBuildAnchorFocus(");
            AddSource(sources, "Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Actions.cs", "6e5e5c4b2e154ee8601abd03c496d56459921f82966921ec375baebe44e0d1b5", "private static AssistantCommandIntentKind ToAssistantCommandIntentKind(", "AssistantRecommendationKind.CameraFocus => AssistantCommandIntentKind.FocusCamera", "TargetId = recommendation.TargetId");
            AddSource(sources, Opmap002Path, "d4d4674850766c5cd95e1bb5fbb6f26893e0bb019dbaf266a0c9897a3befc807", "result=Passed chunks=514 sources=16542");
            AddSource(sources, Opmap004Path, "29961188be1577dc9e232f9815fa7e27ef0a2b0a73b0acd26e338b22a782f4e3", "\"reportSchema\": \"warline.operation-map.phase0-ownership\"", "\"result\": \"NeedsDecision\"");
            AddSource(sources, TrackerPath, "8f293f7ce5b3c9c9eef777bcee9201bb82cb1b8d269e092a9b4eb4f565fc3122", "Inventory minimap projection, camera clamp, initial camera, full-map bounds, and objective-focus sources.");
            return sources;
        }

        private static void AddSource(
            IDictionary<string, SourceSpec> sources,
            string path,
            string sha256,
            params string[] tokens)
        {
            sources.Add(path, new SourceSpec(sha256, tokens));
        }

        private static void AddRow(
            IDictionary<string, OwnershipSpec> rows,
            string identity,
            string subject,
            string authority,
            string currentType,
            OwnershipClassification classification,
            string[] evidencePaths,
            string rationale,
            string disposition,
            string decisionOwner)
        {
            rows.Add(identity, new OwnershipSpec(
                subject,
                authority,
                currentType,
                classification,
                evidencePaths,
                rationale,
                disposition,
                decisionOwner));
        }

        private sealed class SourceSpec
        {
            public readonly string sha256;
            public readonly string[] requiredTokens;
            public SourceSpec(string sha256, string[] requiredTokens)
            {
                this.sha256 = sha256;
                this.requiredTokens = requiredTokens;
            }
        }

        private sealed class CrossReferenceSpec
        {
            public readonly string reportSchema;
            public readonly int reportSchemaVersion;
            public readonly string result;
            public readonly string evidencePath;
            public CrossReferenceSpec(string reportSchema, int reportSchemaVersion, string result, string evidencePath)
            {
                this.reportSchema = reportSchema;
                this.reportSchemaVersion = reportSchemaVersion;
                this.result = result;
                this.evidencePath = evidencePath;
            }
        }

        private sealed class PresenceSpec
        {
            public readonly string status;
            public readonly string currentAuthority;
            public readonly string currentType;
            public readonly string[] evidencePaths;
            public readonly string rationale;
            public readonly string decisionOwner;
            public PresenceSpec(
                string status,
                string currentAuthority,
                string currentType,
                string[] evidencePaths,
                string rationale,
                string decisionOwner)
            {
                this.status = status;
                this.currentAuthority = currentAuthority;
                this.currentType = currentType;
                this.evidencePaths = evidencePaths;
                this.rationale = rationale;
                this.decisionOwner = decisionOwner;
            }
        }

        private sealed class OwnershipSpec
        {
            public readonly string subject;
            public readonly string currentAuthority;
            public readonly string currentType;
            public readonly OwnershipClassification classification;
            public readonly string[] evidencePaths;
            public readonly string rationale;
            public readonly string migrationDisposition;
            public readonly string decisionOwner;
            public OwnershipSpec(string subject, string currentAuthority, string currentType, OwnershipClassification classification, string[] evidencePaths, string rationale, string migrationDisposition, string decisionOwner)
            {
                this.subject = subject;
                this.currentAuthority = currentAuthority;
                this.currentType = currentType;
                this.classification = classification;
                this.evidencePaths = evidencePaths;
                this.rationale = rationale;
                this.migrationDisposition = migrationDisposition;
                this.decisionOwner = decisionOwner;
            }
        }

        private sealed class PublicationDirectory : IDisposable
        {
            private const int UnixMissingEntryError = 2;
            private const int UnixOwnerReadWriteMode = 384;

            private readonly ValidatedReportDestination destination;
            private readonly int directoryDescriptor;

            private PublicationDirectory(
                ValidatedReportDestination destination,
                int directoryDescriptor)
            {
                this.destination = destination;
                this.directoryDescriptor = directoryDescriptor;
            }

            public static PublicationDirectory Open(ValidatedReportDestination destination)
            {
                ValidateDestinationIdentity(destination);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new InvalidOperationException(
                        "Camera/minimap report publication requires descriptor-relative filesystem operations.");
                }

                int descriptor = UnixOpen(destination.canonicalParent, UnixDirectoryOpenFlags());
                if (descriptor < 0)
                    throw UnixIoFailure("open canonical publication directory");

                var directory = new PublicationDirectory(destination, descriptor);
                try
                {
                    directory.ValidateIdentity();
                    return directory;
                }
                catch
                {
                    directory.Dispose();
                    throw;
                }
            }

            public void ValidateIdentity()
            {
                ValidateDestinationIdentity(destination);
                DirectoryIdentity descriptorIdentity = ReadDescriptorIdentity(directoryDescriptor);
                DirectoryIdentity pathIdentity = ReadPathIdentity(destination.canonicalParent);
                if (!descriptorIdentity.Equals(pathIdentity))
                {
                    throw new InvalidOperationException(
                        "Camera/minimap canonical publication directory was replaced after opening.");
                }
            }

            private static DirectoryIdentity ReadDescriptorIdentity(int descriptor)
            {
                return ReadIdentity(status => UnixFstat(descriptor, status), "inspect publication directory descriptor");
            }

            private static DirectoryIdentity ReadPathIdentity(string path)
            {
                return ReadIdentity(status => UnixStat(path, status), "inspect canonical publication directory path");
            }

            private static DirectoryIdentity ReadIdentity(
                Func<IntPtr, int> readStatus,
                string operation)
            {
                IntPtr status = Marshal.AllocHGlobal(512);
                try
                {
                    if (readStatus(status) != 0)
                        throw UnixIoFailure(operation);
                    long device = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                        ? unchecked((uint)Marshal.ReadInt32(status, 0))
                        : Marshal.ReadInt64(status, 0);
                    long inode = Marshal.ReadInt64(status, 8);
                    return new DirectoryIdentity(device, inode);
                }
                finally
                {
                    Marshal.FreeHGlobal(status);
                }
            }

            private readonly struct DirectoryIdentity : IEquatable<DirectoryIdentity>
            {
                private readonly long device;
                private readonly long inode;

                public DirectoryIdentity(long device, long inode)
                {
                    this.device = device;
                    this.inode = inode;
                }

                public bool Equals(DirectoryIdentity other)
                {
                    return device == other.device && inode == other.inode;
                }
            }

            public void WriteAllText(string name, string value)
            {
                RequireBasename(name);
                int descriptor = UnixOpenAt(
                    directoryDescriptor,
                    name,
                    UnixCreateExclusiveWriteFlags(),
                    UnixOwnerReadWriteMode);
                if (descriptor < 0)
                    throw UnixIoFailure("create temporary publication file");

                byte[] bytes = Utf8WithoutBom.GetBytes(value);
                IntPtr buffer = Marshal.AllocHGlobal(bytes.Length);
                try
                {
                    if (UnixFchmod(descriptor, UnixOwnerReadWriteMode) != 0)
                        throw UnixIoFailure("secure temporary publication file permissions");
                    Marshal.Copy(bytes, 0, buffer, bytes.Length);
                    int offset = 0;
                    while (offset < bytes.Length)
                    {
                        long written = UnixWrite(
                            descriptor,
                            IntPtr.Add(buffer, offset),
                            (UIntPtr)(uint)(bytes.Length - offset)).ToInt64();
                        if (written <= 0)
                            throw UnixIoFailure("write temporary publication file");
                        offset += checked((int)written);
                    }
                    if (UnixFsync(descriptor) != 0)
                        throw UnixIoFailure("flush temporary publication file");
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                    UnixClose(descriptor);
                }
            }

            public string ReadAllText(string name)
            {
                RequireBasename(name);
                int descriptor = UnixOpenAt(directoryDescriptor, name, UnixReadOnlyFlags(), 0);
                if (descriptor < 0)
                    throw UnixIoFailure("open publication file for verification");
                try
                {
                    using var bytes = new MemoryStream();
                    var buffer = new byte[8192];
                    while (true)
                    {
                        long read = UnixRead(descriptor, buffer, (UIntPtr)(uint)buffer.Length).ToInt64();
                        if (read < 0)
                            throw UnixIoFailure("read publication file for verification");
                        if (read == 0)
                            break;
                        bytes.Write(buffer, 0, checked((int)read));
                    }
                    return Utf8WithoutBom.GetString(bytes.ToArray());
                }
                finally
                {
                    UnixClose(descriptor);
                }
            }

            public void Replace(string temporaryName, string outputName)
            {
                RequireBasename(temporaryName);
                RequireBasename(outputName);
                if (UnixRenameAt(
                        directoryDescriptor,
                        temporaryName,
                        directoryDescriptor,
                        outputName) != 0)
                {
                    throw UnixIoFailure("atomically publish validated report");
                }
            }

            public void DeleteIfPresent(string name)
            {
                RequireBasename(name);
                if (UnixUnlinkAt(directoryDescriptor, name, 0) == 0)
                    return;
                int error = Marshal.GetLastWin32Error();
                if (error != UnixMissingEntryError)
                    throw new InvalidOperationException(
                        "Failed to remove descriptor-relative publication file; errno=" + error + ".");
            }

            public void Dispose()
            {
                if (directoryDescriptor >= 0)
                    UnixClose(directoryDescriptor);
            }

            private static void RequireBasename(string name)
            {
                if (string.IsNullOrWhiteSpace(name) ||
                    !string.Equals(name, Path.GetFileName(name), StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Publication entries must use basename-only identities.");
                }
            }

            private static int UnixDirectoryOpenFlags()
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? 0x100000 | 0x1000000
                    : 0x10000 | 0x80000;
            }

            private static int UnixCreateExclusiveWriteFlags()
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? 0x0001 | 0x0200 | 0x0800 | 0x1000000
                    : 0x0001 | 0x0040 | 0x0080 | 0x80000;
            }

            private static int UnixReadOnlyFlags()
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 0x1000000 : 0x80000;
            }

            private static InvalidOperationException UnixIoFailure(string operation)
            {
                return new InvalidOperationException(
                    "Failed to " + operation + "; errno=" + Marshal.GetLastWin32Error() + ".");
            }
        }

        internal sealed class ValidatedReportDestination
        {
            public readonly string requestedParent;
            public readonly string canonicalParent;
            public readonly string canonicalProjectRoot;
            public readonly string canonicalPath;

            public ValidatedReportDestination(
                string requestedParent,
                string canonicalParent,
                string canonicalProjectRoot,
                string canonicalPath)
            {
                this.requestedParent = requestedParent;
                this.canonicalParent = canonicalParent;
                this.canonicalProjectRoot = canonicalProjectRoot;
                this.canonicalPath = canonicalPath;
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
            public List<CrossReferenceReport> crossReferences;
            public List<InputHashReport> directInputHashes;
            public List<PresenceFinding> presenceFindings;
            public List<OwnershipRow> evidenceRows;
        }

        [Serializable]
        internal sealed class OwnershipCounts
        {
            public int evidenceRows;
            public int shellOwned;
            public int mapOwned;
            public int sharedConfig;
            public int temporaryCompatibility;
            public int mixed;
            public int unresolved;
            public int needsDecision;
        }

        [Serializable]
        internal sealed class CrossReferenceReport
        {
            public string taskId;
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public string evidencePath;
            public string evidenceSha256;
        }

        [Serializable]
        internal sealed class InputHashReport
        {
            public string path;
            public string sha256;
        }

        [Serializable]
        internal sealed class PresenceFinding
        {
            public string stableIdentity;
            public string status;
            public string currentAuthority;
            public string currentType;
            public List<string> evidencePaths;
            public string rationale;
            public string decisionOwner;
        }

        [Serializable]
        internal sealed class OwnershipRow
        {
            public string stableIdentity;
            public string subject;
            public string currentAuthority;
            public string currentType;
            public string classification;
            public List<string> evidencePaths;
            public string rationale;
            public string migrationDisposition;
            public string decisionOwner;
        }
    }
}

#endif
