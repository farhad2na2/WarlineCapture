#if UNITY_EDITOR

namespace Game.Tests.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using Game.Editor;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class OperationMapPhase0PlacementOwnershipProbeTests
    {
        private const string ReportPath =
            "Design/AgentReports/2026-07-15_opmap-006_phase0_placement_ownership.json";

        [Test]
        public void ResolveReportOutputPath_EmptyPathUsesExternalDefault()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            Assert.That(
                OperationMapPhase0PlacementOwnershipProbe.ResolveReportOutputPath(projectRoot, string.Empty),
                Is.EqualTo(OperationMapPhase0PlacementOwnershipProbe.DefaultReportPath));
        }

        [Test]
        public void ResolveReportOutputPath_ProjectDestinationFailsClosed()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string path = Path.Combine(projectRoot, "placement-ownership.json");
            Assert.Throws<InvalidOperationException>(() =>
                OperationMapPhase0PlacementOwnershipProbe.ResolveReportOutputPath(projectRoot, path));
        }

        [Test]
        public void CommittedReport_HasExactNeedsDecisionShapeWithoutMachineData()
        {
            string json = File.ReadAllText(ReportPath);
            Assert.That(OperationMapPhase0PlacementOwnershipProbe.HasRequiredReportShape(json), Is.True);
            Assert.That(json, Does.Not.Contain("projectRoot"));
            Assert.That(json, Does.Not.Contain("unityVersion"));
            Assert.That(json, Does.Not.Contain("outputPath"));

            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadReport(json);
            Assert.That(report.result, Is.EqualTo("NeedsDecision"));
            Assert.That(report.counts.totalPlacements, Is.EqualTo(480));
            Assert.That(report.counts.needsDecision, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RequiredShape_RejectsSchemaOrBaselineDrift(bool schema)
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            if (schema)
                report.reportSchemaVersion++;
            else
                report.baselineCommit = new string('0', 40);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsMissingDirectInputHash()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.directInputHashes.RemoveAt(0);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsDirectInputHashDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.directInputHashes[0].sha256 = new string('0', 64);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsStaleOpmap002Evidence()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.opmap002Baseline.evidenceSha256 = new string('0', 64);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsOpmap002PlacementCountDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.opmap002Baseline.buildingPlacementCount++;
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsRuntimeConsumerSetDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.runtimeConsumers.RemoveAt(0);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsRuntimeConsumerResponsibilityDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.runtimeConsumers[0].responsibility = "Different responsibility.";
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsMatchSceneViewBindingDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.placementConfigs[0].binding.configField = "differentField";
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsConfigAssetLocalIdDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.placementConfigs[0].asset.localId++;
            AssertInvalid(report);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RequiredShape_RejectsSpawnOrHideFlagDrift(bool hideFlag)
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            if (hideFlag)
                report.placementConfigs[0].hideAuthoringVisualsAfterSpawn = false;
            else
                report.placementConfigs[0].spawnOnMatchStart = false;
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsFactionCountDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.placementConfigs[0].factionCounts[0].count++;
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsPlacementCountDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.placementConfigs[0].entries.RemoveAt(0);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsCorrelatedSummaryAndEntryCountDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.placementConfigs[0].entries.RemoveAt(0);
            report.placementConfigs[0].count--;
            report.counts.buildingPlacements--;
            report.counts.totalPlacements--;
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsSourceOccurrenceCountDriftWithRecomputedIdentity()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            OperationMapPhase0PlacementOwnershipProbe.PlacementEntryReport entry =
                report.placementConfigs[0].entries[0];
            entry.configSourcePathOccurrenceCount++;
            entry.stableIdentitySha256 = OperationMapPhase0BaselineProbe.ComputeSha256(
                System.Text.Encoding.UTF8.GetBytes(
                    OperationMapPhase0PlacementOwnershipProbe.BuildPlacementStableIdentity(entry)));
            AssertInvalid(report);
        }

        [Test]
        public void StableIdentity_CanonicalizesSignedZero()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            OperationMapPhase0PlacementOwnershipProbe.PlacementEntryReport entry =
                report.placementConfigs[0].entries.First(candidate =>
                    candidate.worldEulerAngles.x == 0f && candidate.worldEulerAngles.z == 0f);
            string positiveZeroIdentity =
                OperationMapPhase0PlacementOwnershipProbe.BuildPlacementStableIdentity(entry);
            entry.worldEulerAngles.x = -0f;
            entry.worldEulerAngles.z = -0f;
            string negativeZeroIdentity =
                OperationMapPhase0PlacementOwnershipProbe.BuildPlacementStableIdentity(entry);
            Assert.That(negativeZeroIdentity, Is.EqualTo(positiveZeroIdentity));
        }

        [Test]
        public void RequiredShape_RejectsHierarchyEvidenceDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.placementConfigs[0].entries[0].hierarchyPaths[0] += "/drift";
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsPrefabGuidOrLocalIdDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.placementConfigs[0].entries[0].prefab.localId++;
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsPlacementStableIdentityDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.placementConfigs[0].entries[0].stableIdentitySha256 = new string('0', 64);
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsPlacementOrderDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            (report.placementConfigs[0].entries[0], report.placementConfigs[0].entries[1]) =
                (report.placementConfigs[0].entries[1], report.placementConfigs[0].entries[0]);
            AssertInvalid(report);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RequiredShape_RejectsDecisionOwnerOrMigrationDrift(int field)
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            if (field == 0)
                report.decisions[0].decisionOwner = string.Empty;
            else
                report.decisions[0].migrationDisposition = "Different migration.";
            AssertInvalid(report);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RequiredShape_RejectsDecisionSemanticsDrift(int field)
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            if (field == 0)
                report.result = "Passed";
            else if (field == 1)
                report.counts.needsDecision = 0;
            else
                report.decisions[0].state = "Passed";
            AssertInvalid(report);
        }

        [Test]
        public void Publication_IsAtomicAndInvalidEvidenceRemovesPriorSuccess()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "opmap006-publication-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                string valid = File.ReadAllText(ReportPath);
                OperationMapPhase0PlacementOwnershipProbe.PublishReportAtomically(path, valid);
                Assert.That(File.ReadAllText(path), Is.EqualTo(valid));
                Assert.That(File.Exists(path + ".tmp"), Is.False);

                Assert.Throws<InvalidOperationException>(() =>
                    OperationMapPhase0PlacementOwnershipProbe.PublishReportAtomically(path, "{}"));
                Assert.That(File.Exists(path), Is.False);
                Assert.That(File.Exists(path + ".tmp"), Is.False);
            }
            finally
            {
                OperationMapPhase0PlacementOwnershipProbe.InvalidateOutput(path);
            }
        }

        private static OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport
            LoadCommittedReport()
        {
            return LoadReport(File.ReadAllText(ReportPath));
        }

        private static OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport LoadReport(
            string json)
        {
            return JsonUtility.FromJson<
                OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport>(json);
        }

        private static void AssertInvalid(
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report)
        {
            Assert.That(
                OperationMapPhase0PlacementOwnershipProbe.HasRequiredReportShape(
                    JsonUtility.ToJson(report)),
                Is.False);
        }
    }
}

#endif
