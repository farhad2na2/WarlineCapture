#if UNITY_EDITOR

namespace Game.Tests.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Game.Editor;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class OperationMapPhase0CameraMinimapOwnershipProbeTests
    {
        private const string ReportPath =
            "Design/AgentReports/2026-07-15_opmap-007_phase0_camera_minimap_ownership.json";

        [Test]
        public void ResolveReportOutputPath_UsesExternalDefaultAndRejectsProjectOutput()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            Assert.That(
                OperationMapPhase0CameraMinimapOwnershipProbe.ResolveReportOutputPath(projectRoot, string.Empty),
                Is.EqualTo(OperationMapPhase0CameraMinimapOwnershipProbe.DefaultReportPath));
            Assert.Throws<InvalidOperationException>(() =>
                OperationMapPhase0CameraMinimapOwnershipProbe.ResolveReportOutputPath(
                    projectRoot,
                    Path.Combine(projectRoot, "report.json")));
        }

        [Test]
        public void CommittedReport_HasExactNeedsDecisionShape()
        {
            string json = File.ReadAllText(ReportPath);
            Assert.That(OperationMapPhase0CameraMinimapOwnershipProbe.HasRequiredReportShape(json), Is.True);

            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport report = LoadReport(json);
            Assert.That(report.baselineCommit, Is.EqualTo("d9e2f1ba0e9f7df2d35abe60488fb1d44d5c91bf"));
            Assert.That(report.result, Is.EqualTo("NeedsDecision"));
            Assert.That(report.counts.mixed, Is.GreaterThan(0));
            Assert.That(report.counts.unresolved, Is.GreaterThan(0));
            Assert.That(report.counts.needsDecision, Is.EqualTo(report.counts.mixed + report.counts.unresolved));
        }

        [Test]
        public void CurrentInputs_HaveExactBaselineHashesAndMissingRootFailsClosed()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            List<OperationMapPhase0CameraMinimapOwnershipProbe.InputHashReport> hashes =
                OperationMapPhase0CameraMinimapOwnershipProbe.CaptureAndValidateInputs(projectRoot);
            Assert.That(hashes.Select(hash => hash.path), Is.Ordered);
            Assert.That(hashes.Select(hash => hash.path), Does.Contain("Design/AgentReports/2026-07-15_opmap-004_phase0_ownership_baseline.json"));
            Assert.Throws<InvalidOperationException>(() =>
                OperationMapPhase0CameraMinimapOwnershipProbe.CaptureAndValidateInputs(
                    Path.Combine(Path.GetTempPath(), "opmap007-missing-" + Guid.NewGuid().ToString("N"))));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void RequiredShape_RejectsMissingSectionEntry(int section)
        {
            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport report = LoadCommittedReport();
            switch (section)
            {
                case 0:
                    report.crossReferences.RemoveAt(0);
                    break;
                case 1:
                    report.directInputHashes.RemoveAt(0);
                    break;
                case 2:
                    report.presenceFindings.RemoveAt(0);
                    break;
                default:
                    report.evidenceRows.RemoveAt(0);
                    break;
            }
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsStaleSourceAndCrossReferenceEvidence()
        {
            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.directInputHashes[0].sha256 = new string('0', 64);
            AssertInvalid(report);

            report = LoadCommittedReport();
            report.crossReferences[0].evidenceSha256 = new string('f', 64);
            AssertInvalid(report);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RequiredShape_RejectsOrderingDrift(int section)
        {
            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport report = LoadCommittedReport();
            switch (section)
            {
                case 0:
                    Swap(report.directInputHashes, 0, 1);
                    break;
                case 1:
                    Swap(report.presenceFindings, 0, 1);
                    break;
                default:
                    Swap(report.evidenceRows, 0, 1);
                    break;
            }
            AssertInvalid(report);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void RequiredShape_RejectsOwnershipRowDrift(int field)
        {
            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport report = LoadCommittedReport();
            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipRow row = report.evidenceRows[0];
            switch (field)
            {
                case 0: row.stableIdentity += ".drift"; break;
                case 1: row.subject = "drift"; break;
                case 2: row.currentAuthority = "drift"; break;
                case 3: row.currentType = "drift"; break;
                case 4: row.classification = "Unresolved"; break;
                case 5: row.evidencePaths[0] = "Assets/drift.cs"; break;
                case 6: row.rationale = "drift"; break;
                default: row.migrationDisposition = "drift"; break;
            }
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_EnforcesMixedAndUnresolvedNeedsDecisionSemantics()
        {
            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport report = LoadCommittedReport();
            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipRow[] decisions = report.evidenceRows
                .Where(row => row.classification == "Mixed" || row.classification == "Unresolved")
                .ToArray();
            Assert.That(decisions, Is.Not.Empty);
            Assert.That(decisions.All(row => row.migrationDisposition == "DecisionRequired"), Is.True);
            Assert.That(decisions.All(row => !string.IsNullOrWhiteSpace(row.decisionOwner)), Is.True);

            report.result = "Passed";
            AssertInvalid(report);
            report = LoadCommittedReport();
            report.evidenceRows.First(row => row.classification == "Mixed").decisionOwner = string.Empty;
            AssertInvalid(report);
            report = LoadCommittedReport();
            report.counts.unresolved--;
            report.counts.shellOwned++;
            AssertInvalid(report);
        }

        [Test]
        public void PresenceFindings_PinInitialAndCameraFocusProducersAndKeepObjectiveWriterDecisionOwned()
        {
            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport report = LoadCommittedReport();
            Assert.That(
                report.presenceFindings.Single(row => row.stableIdentity == "initial-focus-producer").status,
                Is.EqualTo("Present"));
            Assert.That(
                report.presenceFindings.Single(row => row.stableIdentity == "runtime-objective-writer").status,
                Is.EqualTo("Unresolved"));
            Assert.That(
                report.presenceFindings.Single(row => row.stableIdentity == "objective-camera-focus-recommendation-producer").status,
                Is.EqualTo("Present"));
            Assert.That(
                report.presenceFindings.Where(row => row.status == "Unresolved")
                    .All(row => row.currentAuthority == "No writer found in audited sources" &&
                                !string.IsNullOrWhiteSpace(row.decisionOwner)),
                Is.True);

            report.presenceFindings[0].status = "Unresolved";
            AssertInvalid(report);
        }

        [TestCase("MatchObjectiveRuntimeElement")]
        [TestCase("AssistantRecommendationKind.CameraFocus")]
        public void ProducerCandidateAudit_RejectsNewRuntimeSourceCandidate(string candidateToken)
        {
            string root = Path.Combine(Path.GetTempPath(), "opmap007-candidate-" + Guid.NewGuid().ToString("N"));
            string scripts = Path.Combine(root, "Assets/Game/Scripts/Feature");
            Directory.CreateDirectory(scripts);
            try
            {
                File.WriteAllText(Path.Combine(scripts, "NewCandidate.cs"), "// " + candidateToken);
                Assert.Throws<InvalidOperationException>(() =>
                    OperationMapPhase0CameraMinimapOwnershipProbe.ValidateNoUnexpectedProducerCandidates(root));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void StableMethodIdentities_PinFullyQualifiedSignaturesAndOverloads()
        {
            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport report = LoadCommittedReport();
            string[] methodIdentities = report.evidenceRows.Select(row => row.stableIdentity)
                .Where(identity => identity.Contains("::", StringComparison.Ordinal))
                .ToArray();
            Assert.That(methodIdentities, Has.None.Contains("(...)"));
            Assert.That(methodIdentities, Does.Contain(
                "Game.Runtime.RtsCameraRequestSystem::ProcessPendingRequests(Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)"));
            Assert.That(methodIdentities, Does.Contain(
                "Game.UI.Shell.Ecs.UiShellEcsGateway.UiShellActionAdapter::ToAssistantCommandIntentKind(Game.UI.Contracts.UiAssistantCommandIntentKind,Game.Components.AssistantRecommendationKind)"));
            Assert.That(methodIdentities, Does.Contain(
                "Game.Authoring.GridAuthoring.GridBaker::Bake(Game.Authoring.GridAuthoring)"));
            Assert.That(methodIdentities, Has.None.EqualTo(
                "Game.UI.Shell.Ecs.UiShellEcsGateway::ToAssistantCommandIntentKind(Game.UI.Contracts.UiAssistantCommandIntentKind,Game.Components.AssistantRecommendationKind)"));
            Assert.That(methodIdentities, Has.None.EqualTo(
                "Game.Authoring.GridAuthoring.Baker::Bake(Game.Authoring.GridAuthoring)"));
            Assert.DoesNotThrow(
                OperationMapPhase0CameraMinimapOwnershipProbe.ValidateDeclaredMethodSignatures);

            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipRow overload = report.evidenceRows.Single(row =>
                row.stableIdentity.Contains("::ProcessPendingRequests(", StringComparison.Ordinal));
            overload.stableIdentity = "Game.Runtime.RtsCameraRequestSystem::ProcessPendingRequests(Unity.Entities.EntityManager)";
            AssertInvalid(report);
        }

        [Test]
        public void BuildReport_ProducesTotalOrderingFromReversedRows()
        {
            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport source = LoadCommittedReport();
            source.evidenceRows.Reverse();

            OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport rebuilt =
                OperationMapPhase0CameraMinimapOwnershipProbe.BuildReport(
                    source.directInputHashes,
                    source.crossReferences,
                    source.presenceFindings,
                    source.evidenceRows);

            Assert.That(rebuilt.evidenceRows.Select(row => row.stableIdentity), Is.Ordered);
            Assert.That(
                OperationMapPhase0CameraMinimapOwnershipProbe.HasRequiredReportShape(
                    JsonUtility.ToJson(rebuilt)),
                Is.True);
        }

        [Test]
        public void Publication_InvalidatesPriorSuccessAndCleansTemporaryOutput()
        {
            RequireDescriptorRelativePublication();
            string outputPath = Path.Combine(
                Path.GetTempPath(),
                "opmap007-publication-" + Guid.NewGuid().ToString("N") + ".json");
            OperationMapPhase0CameraMinimapOwnershipProbe.ValidatedReportDestination destination =
                Destination(outputPath);
            try
            {
                File.WriteAllText(outputPath, File.ReadAllText(ReportPath));
                Assert.Throws<InvalidOperationException>(() =>
                    OperationMapPhase0CameraMinimapOwnershipProbe.PublishReportAtomically(destination, "{}"));
                Assert.That(File.Exists(outputPath), Is.False);
                Assert.That(File.Exists(outputPath + ".tmp"), Is.False);
                Assert.That(TemporaryOutputs(outputPath), Is.Empty);
            }
            finally
            {
                Delete(outputPath);
                Delete(outputPath + ".tmp");
            }
        }

        [Test]
        public void Publication_ConcurrentRunsPublishOnlyExactValidatedBytes()
        {
            RequireDescriptorRelativePublication();
            string outputPath = Path.Combine(
                Path.GetTempPath(),
                "opmap007-race-" + Guid.NewGuid().ToString("N") + ".json");
            string json = File.ReadAllText(ReportPath);
            OperationMapPhase0CameraMinimapOwnershipProbe.ValidatedReportDestination destination =
                Destination(outputPath);
            try
            {
                Parallel.For(0, 8, _ =>
                    OperationMapPhase0CameraMinimapOwnershipProbe.PublishReportAtomically(destination, json));
                Assert.That(File.ReadAllText(outputPath), Is.EqualTo(json));
                Assert.That(TemporaryOutputs(outputPath), Is.Empty);
            }
            finally
            {
                Delete(outputPath);
                foreach (string temporaryPath in TemporaryOutputs(outputPath))
                    Delete(temporaryPath);
            }
        }

        [Test]
        public void Publication_ValidInvalidRaceNeverPublishesForeignBytes()
        {
            RequireDescriptorRelativePublication();
            string outputPath = Path.Combine(
                Path.GetTempPath(),
                "opmap007-negative-race-" + Guid.NewGuid().ToString("N") + ".json");
            string json = File.ReadAllText(ReportPath);
            OperationMapPhase0CameraMinimapOwnershipProbe.ValidatedReportDestination destination =
                Destination(outputPath);
            try
            {
                Parallel.Invoke(
                    () => OperationMapPhase0CameraMinimapOwnershipProbe.PublishReportAtomically(destination, json),
                    () => Assert.Throws<InvalidOperationException>(() =>
                        OperationMapPhase0CameraMinimapOwnershipProbe.PublishReportAtomically(
                            destination,
                            "{\"foreignSuccess\":true}")));

                if (File.Exists(outputPath))
                    Assert.That(File.ReadAllText(outputPath), Is.EqualTo(json));
                Assert.That(TemporaryOutputs(outputPath), Is.Empty);
            }
            finally
            {
                Delete(outputPath);
                foreach (string temporaryPath in TemporaryOutputs(outputPath))
                    Delete(temporaryPath);
            }
        }

        [Test]
        public void ResolveReportOutputPath_RejectsSymlinkedParentResolvingIntoProject()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.Ignore("Unix symlink containment negative.");

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string container = Path.Combine(Path.GetTempPath(), "opmap007-link-" + Guid.NewGuid().ToString("N"));
            string link = Path.Combine(container, "project-link");
            Directory.CreateDirectory(container);
            try
            {
                Assert.That(Symlink(projectRoot, link), Is.EqualTo(0));
                Assert.Throws<InvalidOperationException>(() =>
                    OperationMapPhase0CameraMinimapOwnershipProbe.ResolveReportOutputPath(
                        projectRoot,
                        Path.Combine(link, "foreign-success.json")));
            }
            finally
            {
                Unlink(link);
                Directory.Delete(container, true);
            }
        }

        [Test]
        public void Publication_RejectsSymlinkParentRetargetImmediatelyBeforeReplace()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.Ignore("Unix symlink-retarget negative.");

            string container = Path.Combine(Path.GetTempPath(), "opmap007-retarget-" + Guid.NewGuid().ToString("N"));
            string originalParent = Path.Combine(container, "original");
            string retargetedParent = Path.Combine(container, "retargeted");
            string link = Path.Combine(container, "output-link");
            Directory.CreateDirectory(originalParent);
            Directory.CreateDirectory(retargetedParent);
            try
            {
                Assert.That(Symlink(originalParent, link), Is.EqualTo(0));
                OperationMapPhase0CameraMinimapOwnershipProbe.ValidatedReportDestination destination =
                    Destination(Path.Combine(link, "report.json"));

                Assert.Throws<InvalidOperationException>(() =>
                    OperationMapPhase0CameraMinimapOwnershipProbe.PublishReportAtomically(
                        destination,
                        File.ReadAllText(ReportPath),
                        () =>
                        {
                            Assert.That(Unlink(link), Is.EqualTo(0));
                            Assert.That(Symlink(retargetedParent, link), Is.EqualTo(0));
                        }));

                Assert.That(File.Exists(Path.Combine(originalParent, "report.json")), Is.False);
                Assert.That(File.Exists(Path.Combine(retargetedParent, "report.json")), Is.False);
                Assert.That(Directory.GetFiles(originalParent), Is.Empty);
                Assert.That(Directory.GetFiles(retargetedParent), Is.Empty);
            }
            finally
            {
                Unlink(link);
                Directory.Delete(container, true);
            }
        }

        [Test]
        public void Publication_RejectsCanonicalParentReplacementAfterFinalValidation()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.Ignore("Unix descriptor-relative parent-replacement negative.");

            string container = Path.Combine(
                Path.GetTempPath(),
                "opmap007-parent-replace-" + Guid.NewGuid().ToString("N"));
            string canonicalParent = Path.Combine(container, "canonical");
            string displacedParent = Path.Combine(container, "displaced");
            string attackerParent = Path.Combine(container, "attacker");
            Directory.CreateDirectory(canonicalParent);
            Directory.CreateDirectory(attackerParent);
            try
            {
                OperationMapPhase0CameraMinimapOwnershipProbe.ValidatedReportDestination destination =
                    Destination(Path.Combine(canonicalParent, "report.json"));

                Assert.Throws<InvalidOperationException>(() =>
                    OperationMapPhase0CameraMinimapOwnershipProbe.PublishReportAtomically(
                        destination,
                        File.ReadAllText(ReportPath),
                        () =>
                        {
                            Directory.Move(canonicalParent, displacedParent);
                            Assert.That(Symlink(attackerParent, canonicalParent), Is.EqualTo(0));
                        }));

                Assert.That(File.Exists(Path.Combine(canonicalParent, "report.json")), Is.False);
                Assert.That(File.Exists(Path.Combine(displacedParent, "report.json")), Is.False);
                Assert.That(File.Exists(Path.Combine(attackerParent, "report.json")), Is.False);
                Assert.That(Directory.GetFiles(displacedParent), Is.Empty);
                Assert.That(Directory.GetFiles(attackerParent), Is.Empty);
            }
            finally
            {
                Unlink(canonicalParent);
                Directory.Delete(container, true);
            }
        }

        [Test]
        public void Run_WritesOnlyExternalOutputAndLeavesAllInputsUnchanged()
        {
            RequireDescriptorRelativePublication();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = Path.Combine(
                Path.GetTempPath(),
                "opmap007-isolation-" + Guid.NewGuid().ToString("N") + ".json");
            string priorOverride = Environment.GetEnvironmentVariable(
                OperationMapPhase0CameraMinimapOwnershipProbe.ReportPathEnvironmentVariable);
            List<OperationMapPhase0CameraMinimapOwnershipProbe.InputHashReport> before =
                OperationMapPhase0CameraMinimapOwnershipProbe.CaptureAndValidateInputs(projectRoot);
            try
            {
                Environment.SetEnvironmentVariable(
                    OperationMapPhase0CameraMinimapOwnershipProbe.ReportPathEnvironmentVariable,
                    outputPath);
                OperationMapPhase0CameraMinimapOwnershipProbe.Run();

                Assert.That(File.Exists(outputPath), Is.True);
                Assert.That(
                    OperationMapPhase0CameraMinimapOwnershipProbe.HasRequiredReportShape(
                        File.ReadAllText(outputPath)),
                    Is.True);
                List<OperationMapPhase0CameraMinimapOwnershipProbe.InputHashReport> after =
                    OperationMapPhase0CameraMinimapOwnershipProbe.CaptureAndValidateInputs(projectRoot);
                Assert.That(after.Select(row => row.sha256), Is.EqualTo(before.Select(row => row.sha256)));
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    OperationMapPhase0CameraMinimapOwnershipProbe.ReportPathEnvironmentVariable,
                    priorOverride);
                Delete(outputPath);
                Delete(outputPath + ".tmp");
            }
        }

        [Test]
        public void RequiredShape_RejectsEveryUnknownFieldAndLocalData()
        {
            string json = File.ReadAllText(ReportPath);
            string needsDecisionField =
                "\"needsDecision\": " + LoadCommittedReport().counts.needsDecision;
            Assert.That(
                OperationMapPhase0CameraMinimapOwnershipProbe.HasRequiredReportShape(
                    json.Replace("\"result\": \"NeedsDecision\"", "\"result\": \"NeedsDecision\",\n    \"futureRootField\": true")),
                Is.False);
            Assert.That(
                OperationMapPhase0CameraMinimapOwnershipProbe.HasRequiredReportShape(
                    json.Replace(
                        needsDecisionField,
                        needsDecisionField + ",\n        \"futureNestedField\": 1")),
                Is.False);
            Assert.That(
                OperationMapPhase0CameraMinimapOwnershipProbe.HasRequiredReportShape(
                    json.Replace("\"result\": \"NeedsDecision\"", "\"result\": \"NeedsDecision\",\n    \"local\": \"/Users/example\"")),
                Is.False);
        }

        private static OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport LoadCommittedReport()
        {
            return LoadReport(File.ReadAllText(ReportPath));
        }

        private static OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport LoadReport(string json)
        {
            return JsonUtility.FromJson<OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport>(json);
        }

        private static void AssertInvalid(OperationMapPhase0CameraMinimapOwnershipProbe.OwnershipReport report)
        {
            Assert.That(
                OperationMapPhase0CameraMinimapOwnershipProbe.HasRequiredReportShape(
                    JsonUtility.ToJson(report)),
                Is.False);
        }

        private static void Swap<T>(IList<T> values, int left, int right)
        {
            (values[left], values[right]) = (values[right], values[left]);
        }

        private static void Delete(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static OperationMapPhase0CameraMinimapOwnershipProbe.ValidatedReportDestination Destination(
            string outputPath)
        {
            return OperationMapPhase0CameraMinimapOwnershipProbe.ResolveReportDestination(
                Directory.GetParent(Application.dataPath).FullName,
                outputPath);
        }

        private static void RequireDescriptorRelativePublication()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.Ignore("Descriptor-relative report publication is unavailable on Windows.");
        }

        private static string[] TemporaryOutputs(string outputPath)
        {
            return Directory.GetFiles(
                Path.GetDirectoryName(outputPath),
                Path.GetFileName(outputPath) + ".*.tmp");
        }

        [DllImport("libc", EntryPoint = "symlink", SetLastError = true)]
        private static extern int Symlink(string target, string linkPath);

        [DllImport("libc", EntryPoint = "unlink", SetLastError = true)]
        private static extern int Unlink(string path);
    }
}

#endif
