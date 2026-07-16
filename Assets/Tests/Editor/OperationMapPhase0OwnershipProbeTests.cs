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
        public void ResolveReportOutputPath_EmptyConfiguredPathUsesOwnershipDefault()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;

            Assert.That(
                OperationMapPhase0OwnershipProbe.ResolveReportOutputPath(projectRoot, string.Empty),
                Is.EqualTo(OperationMapPhase0OwnershipProbe.DefaultReportPath));
        }

        [Test]
        public void CommittedReport_HasRequiredNeedsDecisionShape()
        {
            string json = File.ReadAllText(ReportPath);
            Assert.That(OperationMapPhase0OwnershipProbe.HasRequiredReportShape(json), Is.True);

            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadReport(json);
            Assert.That(report.result, Is.EqualTo("NeedsDecision"));
            Assert.That(report.counts.matchSceneViewFields, Is.EqualTo(29));
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
        public void RequiredShape_RejectsDecisionOwnerDrift()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            OperationMapPhase0OwnershipProbe.OwnershipRow decision = report.matchRoots.First(
                row => row.classification == "Mixed" || row.classification == "Unresolved");
            decision.decisionOwner = "Different owner";
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsUnsupportedBaselineSchema()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.opmap002Baseline.reportSchemaVersion++;
            AssertInvalid(report);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void RequiredShape_RejectsBaselineTotalDrift(int totalIndex)
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            switch (totalIndex)
            {
                case 0:
                    report.opmap002Baseline.generatedChunkCount++;
                    break;
                case 1:
                    report.opmap002Baseline.manifestSourceCount++;
                    break;
                case 2:
                    report.opmap002Baseline.buildingPlacementCount++;
                    break;
                default:
                    report.opmap002Baseline.vehiclePlacementCount++;
                    break;
            }
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsBaselineAggregateDrift()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.opmap002Baseline.generatedCombinedAggregateSha256 = new string('0', 64);
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
        public void RequiredShape_RejectsTargetIdentityDrift()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            OperationMapPhase0OwnershipProbe.OwnershipRow row = report.matchSceneViewFields.First(
                candidate => candidate.currentElementCount > 0);
            row.currentTargetIdentities[0] += ".drift";
            AssertInvalid(report);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RequiredShape_RejectsTypeDrift(bool declaredType)
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            OperationMapPhase0OwnershipProbe.OwnershipRow row = report.matchSceneViewFields[0];
            if (declaredType)
                row.declaredType = "UnityEngine.Object";
            else
                row.currentType = "UnityEngine.Object";
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsEvidenceDrift()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.matchSceneViewFields[0].evidencePaths[0] = "Assets/drift.asset";
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsRationaleDrift()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.matchSceneViewFields[0].rationale = "Different rationale.";
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsDispositionDrift()
        {
            OperationMapPhase0OwnershipProbe.OwnershipReport report = LoadCommittedReport();
            report.matchSceneViewFields[0].migrationDisposition = "DifferentDisposition";
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
