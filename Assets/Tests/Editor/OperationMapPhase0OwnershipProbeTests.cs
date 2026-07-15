#if UNITY_EDITOR

namespace Game.Tests.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using Game.Editor;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class OperationMapPhase0OwnershipProbeTests
    {
        private const string ReportPath =
            "Design/AgentReports/2026-07-15_opmap-004_phase0_ownership_baseline.json";

        [Test]
        public void CommittedReport_HasRequiredNeedsDecisionShape()
        {
            string json = File.ReadAllText(ReportPath);
            Assert.That(OperationMapPhase0OwnershipProbe.HasRequiredReportShape(json), Is.True);

            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadReport(json);
            Assert.That(report.result, Is.EqualTo("NeedsDecision"));
            Assert.That(report.counts.matchSceneViewFields, Is.EqualTo(28));
            Assert.That(report.counts.matchRoots, Is.EqualTo(16));
            Assert.That(report.counts.matchSubSceneRoots, Is.EqualTo(3));
            Assert.That(report.counts.needsDecision, Is.GreaterThan(0));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RequiredShape_RejectsMissingOwnershipRow(int collectionIndex)
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            switch (collectionIndex)
            {
                case 0:
                    report.matchSceneViewFields.RemoveAt(0);
                    break;
                case 1:
                    report.matchRoots.RemoveAt(0);
                    break;
                default:
                    report.matchSubSceneRoots.RemoveAt(0);
                    break;
            }
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsPassedStatusWhileDecisionsRemain()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.result = "Passed";
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsDecisionWithoutOwner()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            OperationMapPhase0OwnershipProbe.OwnershipRow decision = report.matchRoots.First(
                row => row.classification == "Mixed" || row.classification == "Unresolved");
            decision.decisionOwner = string.Empty;
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsUnsupportedBaselineSchema()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.opmap002Baseline.reportSchemaVersion++;
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsEmptyBaselineEvidenceCounts()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.opmap002Baseline.generatedChunkCount = 0;
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsFieldIdentityDrift()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.matchSceneViewFields[0].stableIdentity += ".drift";
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsRootOrderDrift()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            (report.matchRoots[0], report.matchRoots[1]) =
                (report.matchRoots[1], report.matchRoots[0]);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsMissingDirectInputHash()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.directInputHashes.RemoveAt(0);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsCanonicalInputHashDrift()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.directInputHashes[0].sha256 = new string('0', 64);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsClassificationCountDrift()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.counts.mixed++;
            report.counts.mapOwned--;
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsTargetCardinalityDrift()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            OperationMapPhase0OwnershipProbe.OwnershipRow row = report.matchSceneViewFields.First(
                candidate => candidate.currentElementCount > 0);
            row.currentElementCount++;
            AssertInvalid(report);
        }

        [Test]
        public void Publication_InvalidatesPriorSuccessBeforeValidationFailure()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "opmap004-publication-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(path, File.ReadAllText(ReportPath));
                OperationMapPhase0OwnershipProbe.InvalidateOutput(path);

                Assert.Throws<InvalidOperationException>(() =>
                    OperationMapPhase0OwnershipProbe.PublishReportAtomically(path, "{}"));
                Assert.That(File.Exists(path), Is.False);
                Assert.That(File.Exists(path + ".tmp"), Is.False);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
                if (File.Exists(path + ".tmp"))
                    File.Delete(path + ".tmp");
            }
        }

        private static OperationMapPhase0OwnershipProbe.OwnershipReport LoadCommittedReport()
        {
            return LoadReport(File.ReadAllText(ReportPath));
        }

        private static OperationMapPhase0OwnershipProbe.OwnershipReport LoadReport(string json)
        {
            return JsonUtility.FromJson<OperationMapPhase0OwnershipProbe.OwnershipReport>(json);
        }

        private static void AssertInvalid(OperationMapPhase0OwnershipProbe.OwnershipReport report)
        {
            Assert.That(
                OperationMapPhase0OwnershipProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
                Is.False);
        }
    }
}

#endif
