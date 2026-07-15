#if UNITY_EDITOR

namespace Game.Tests.Editor
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Game.Editor;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class OperationMapPhase0NavigationMetadataOwnershipProbeTests
    {
        private const string ReportPath =
            "Design/AgentReports/2026-07-15_opmap-008_phase0_navigation_metadata_ownership.json";

        [Test]
        public void CommittedReport_HasStrictNeedsDecisionShape()
        {
            string json = File.ReadAllText(ReportPath);
            Assert.That(OperationMapPhase0NavigationMetadataOwnershipProbe.HasRequiredReportShape(json), Is.True);
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = Load(json);
            Assert.That(report.result, Is.EqualTo("NeedsDecision"));
            Assert.That(report.counts.authorities, Is.EqualTo(15));
            Assert.That(report.counts.runtimeConsumers, Is.EqualTo(15));
            Assert.That(report.counts.acceptedCrossReferences, Is.EqualTo(3));
            Assert.That(report.counts.needsDecision, Is.EqualTo(4));
            Assert.That(report.authorities.Count(entry => entry.classification == "MapOwned"), Is.EqualTo(7));
            Assert.That(report.authorities.Count(entry => entry.classification == "SharedConfig"), Is.EqualTo(4));
            Assert.That(report.authorities.Count(entry => entry.classification == "Mixed"), Is.EqualTo(3));
            Assert.That(report.authorities.Count(entry => entry.classification == "Unresolved"), Is.EqualTo(1));
            Assert.That(report.runtimeConsumers.Any(entry =>
                entry.exactType == "Game.Runtime.FixedWingRunwayHomeInitializationSystem"), Is.True);
            Assert.That(report.runtimeConsumers.Any(entry =>
                entry.exactType == "Game.Runtime.UnitGridMovementSystem"), Is.True);
            Assert.That(json, Does.Not.Contain("projectRoot"));
            Assert.That(json, Does.Not.Contain("unityVersion"));
            Assert.That(json, Does.Not.Contain("outputPath"));
        }

        [TestCase("reportSchema")]
        [TestCase("reportSchemaVersion")]
        [TestCase("baselineCommit")]
        [TestCase("result")]
        [TestCase("identityPayloadSha256")]
        public void RequiredShape_RejectsTopLevelDrift(string field)
        {
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = LoadCommitted();
            switch (field)
            {
                case "reportSchema": report.reportSchema += ".drift"; break;
                case "reportSchemaVersion": report.reportSchemaVersion++; break;
                case "baselineCommit": report.baselineCommit = new string('0', 40); break;
                case "result": report.result = "Passed"; break;
                case "identityPayloadSha256": report.identityPayloadSha256 = new string('0', 64); break;
            }
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsUnknownRootField()
        {
            string json = File.ReadAllText(ReportPath).TrimEnd();
            json = json.Substring(0, json.Length - 1) + ",\n\"unexpected\": true\n}";
            Assert.That(
                OperationMapPhase0NavigationMetadataOwnershipProbe.HasRequiredReportShape(json),
                Is.False);
        }

        [Test]
        public void RequiredShape_RejectsUnknownNestedField()
        {
            string json = File.ReadAllText(ReportPath);
            const string marker = "\"authorities\": [\n        {";
            Assert.That(json, Does.Contain(marker));
            json = json.Replace(marker, marker + "\n            \"unexpected\": \"drift\",", StringComparison.Ordinal);
            Assert.That(
                OperationMapPhase0NavigationMetadataOwnershipProbe.HasRequiredReportShape(json),
                Is.False);
        }

        [Test]
        public void RequiredShape_RejectsAcceptedEvidenceDrift()
        {
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = LoadCommitted();
            report.acceptedEvidence[0].sourceRevision = new string('0', 40);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsAcceptedEvidenceHashDrift()
        {
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = LoadCommitted();
            report.acceptedEvidence[2].evidenceSha256 = new string('0', 64);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsDirectInputAbsence()
        {
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = LoadCommitted();
            report.directInputHashes.RemoveAt(0);
            report.counts.directInputs--;
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsDirectInputDrift()
        {
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = LoadCommitted();
            report.directInputHashes[0].sha256 = new string('0', 64);
            AssertInvalid(report);
        }

        [Test]
        public void HashFiles_FailsClosedWhenSourceIsAbsent()
        {
            string root = Path.Combine(Path.GetTempPath(), "opmap008-absence-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                Assert.Throws<FileNotFoundException>(() =>
                    OperationMapPhase0NavigationMetadataOwnershipProbe.HashFiles(root, new[] { "missing.cs" }));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void RequiredShape_RejectsAuthorityOrderDrift()
        {
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = LoadCommitted();
            (report.authorities[0], report.authorities[1]) = (report.authorities[1], report.authorities[0]);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsConsumerOrderDrift()
        {
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = LoadCommitted();
            (report.runtimeConsumers[0], report.runtimeConsumers[1]) =
                (report.runtimeConsumers[1], report.runtimeConsumers[0]);
            AssertInvalid(report);
        }

        [TestCase("assetGuid")]
        [TestCase("localId")]
        [TestCase("exactType")]
        [TestCase("metadata")]
        [TestCase("sourceRevision")]
        public void RequiredShape_RejectsStableAuthorityDrift(string field)
        {
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = LoadCommitted();
            OperationMapPhase0NavigationMetadataOwnershipProbe.AuthorityReport authority =
                report.authorities.First(entry => !string.IsNullOrEmpty(entry.assetGuid));
            switch (field)
            {
                case "assetGuid": authority.assetGuid = new string('0', 32); break;
                case "localId": authority.localId++; break;
                case "exactType": authority.exactType += ".Drift"; break;
                case "metadata": authority.metadata += "|drift=1"; break;
                case "sourceRevision": authority.sourceRevision = new string('0', 40); break;
            }
            AssertInvalid(report);
        }

        [TestCase("currentOwner")]
        [TestCase("targetOwner")]
        [TestCase("classification")]
        [TestCase("migrationDisposition")]
        [TestCase("migrationOwner")]
        [TestCase("state")]
        [TestCase("decisionOwner")]
        public void RequiredShape_RejectsOwnershipOrDecisionDrift(string field)
        {
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = LoadCommitted();
            OperationMapPhase0NavigationMetadataOwnershipProbe.AuthorityReport authority = report.authorities[0];
            switch (field)
            {
                case "currentOwner": authority.currentOwner = string.Empty; break;
                case "targetOwner": authority.targetOwner = string.Empty; break;
                case "classification": authority.classification = "Unresolved"; break;
                case "migrationDisposition": authority.migrationDisposition = string.Empty; break;
                case "migrationOwner": authority.migrationOwner = string.Empty; break;
                case "state": authority.state = "Passed"; break;
                case "decisionOwner": authority.decisionOwner = string.Empty; break;
            }
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsDecisionDispositionMismatch()
        {
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = LoadCommitted();
            OperationMapPhase0NavigationMetadataOwnershipProbe.AuthorityReport authority =
                report.authorities.First(entry => entry.classification == "Mixed");
            authority.migrationDisposition = "MoveWithOperationMap";
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsRuntimeConsumerMemberIdentityDrift()
        {
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = LoadCommitted();
            report.runtimeConsumers[0].memberIdentity += ".Drift";
            AssertInvalid(report);
        }

        [Test]
        public void BridgeDeckParity_RejectsTypeFlagMismatch()
        {
            Assert.Throws<InvalidOperationException>(() =>
                OperationMapPhase0NavigationMetadataOwnershipProbe.RequireBridgeDeckParity(4, 3));
            Assert.Throws<InvalidOperationException>(() =>
                OperationMapPhase0NavigationMetadataOwnershipProbe.RequireBridgeDeckParity(-1, -1));
        }

        [Test]
        public void RequiredShape_RejectsCorrelatedPayloadAndDigestEdit()
        {
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report = LoadCommitted();
            report.authorities[0].metadata += "|correlated=1";
            report.identityPayloadSha256 =
                OperationMapPhase0NavigationMetadataOwnershipProbe.ComputeIdentityPayloadSha256(report);
            AssertInvalid(report);
        }

        [Test]
        public void ResolveReportOutputPath_RejectsProjectDestination()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            Assert.Throws<InvalidOperationException>(() =>
                OperationMapPhase0NavigationMetadataOwnershipProbe.ResolveReportOutputPath(
                    projectRoot,
                    Path.Combine(projectRoot, "opmap008.json")));
        }

        [Test]
        public void ResolveReportOutputPath_RejectsSymlinkedDirectory()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string target = Path.Combine(Path.GetTempPath(), "opmap008-target-" + Guid.NewGuid().ToString("N"));
            string link = Path.Combine(Path.GetTempPath(), "opmap008-link-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(target);
            try
            {
                Process process = Process.Start(new ProcessStartInfo("ln", $"-s {target} {link}")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process!.WaitForExit();
                Assert.That(process.ExitCode, Is.Zero);
                Assert.Throws<InvalidOperationException>(() =>
                    OperationMapPhase0NavigationMetadataOwnershipProbe.ResolveReportOutputPath(
                        projectRoot,
                        Path.Combine(link, "report.json")));
            }
            finally
            {
                if (Directory.Exists(link)) Directory.Delete(link);
                if (Directory.Exists(target)) Directory.Delete(target, true);
            }
        }

        [Test]
        public void Publication_UsesUniqueTemporaryPathAndLeavesNoTemporaryFile()
        {
            string path = TempReportPath("unique");
            try
            {
                string json = File.ReadAllText(ReportPath);
                OperationMapPhase0NavigationMetadataOwnershipProbe.PublishReportAtomically(path, json);
                Assert.That(File.ReadAllText(path), Is.EqualTo(json));
                Assert.That(Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".tmp-*"), Is.Empty);
            }
            finally
            {
                OperationMapPhase0NavigationMetadataOwnershipProbe.InvalidateOutput(path);
            }
        }

        [Test]
        public void Publication_RaceFailsClosedAndRemovesRaceOutput()
        {
            string path = TempReportPath("race");
            try
            {
                string json = File.ReadAllText(ReportPath);
                Assert.Throws<IOException>(() =>
                    OperationMapPhase0NavigationMetadataOwnershipProbe.PublishReportAtomically(
                        path,
                        json,
                        () => File.WriteAllText(path, "race")));
                Assert.That(File.Exists(path), Is.False);
                Assert.That(Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".tmp-*"), Is.Empty);
            }
            finally
            {
                OperationMapPhase0NavigationMetadataOwnershipProbe.InvalidateOutput(path);
            }
        }

        [Test]
        public void Publication_InvalidEvidenceRemovesPriorSuccess()
        {
            string path = TempReportPath("stale");
            try
            {
                OperationMapPhase0NavigationMetadataOwnershipProbe.PublishReportAtomically(
                    path,
                    File.ReadAllText(ReportPath));
                Assert.That(File.Exists(path), Is.True);
                Assert.Throws<InvalidOperationException>(() =>
                    OperationMapPhase0NavigationMetadataOwnershipProbe.PublishReportAtomically(path, "{}"));
                Assert.That(File.Exists(path), Is.False);
            }
            finally
            {
                OperationMapPhase0NavigationMetadataOwnershipProbe.InvalidateOutput(path);
            }
        }

        private static string TempReportPath(string kind)
        {
            return Path.Combine(
                "/private/tmp",
                $"opmap008-{kind}-{Guid.NewGuid():N}.json");
        }

        private static OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport
            LoadCommitted()
        {
            return Load(File.ReadAllText(ReportPath));
        }

        private static OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport Load(
            string json)
        {
            return JsonUtility.FromJson<
                OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport>(json);
        }

        private static void AssertInvalid(
            OperationMapPhase0NavigationMetadataOwnershipProbe.NavigationMetadataOwnershipReport report)
        {
            Assert.That(
                OperationMapPhase0NavigationMetadataOwnershipProbe.HasRequiredReportShape(
                    JsonUtility.ToJson(report)),
                Is.False);
        }
    }
}
#endif
