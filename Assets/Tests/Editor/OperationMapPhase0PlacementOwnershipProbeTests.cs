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
            "Design/AgentReports/2026-07-17_operation_map_placement_ownership_refresh.json";

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
            Assert.That(report.counts.duplicateSourcePathGroups, Is.EqualTo(54));
            Assert.That(report.counts.duplicateSourcePathEntries, Is.EqualTo(162));
            Assert.That(report.counts.needsDecision, Is.EqualTo(2));
        }

        [Test]
        public void CommittedReport_DuplicatePathsAreGroupedWithoutEntryLevelClaims()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            foreach (OperationMapPhase0PlacementOwnershipProbe.PlacementConfigReport config in
                     report.placementConfigs)
            {
                foreach (OperationMapPhase0PlacementOwnershipProbe.SourcePathGroupReport group in
                         config.sourcePathGroups)
                {
                    OperationMapPhase0PlacementOwnershipProbe.PlacementEntryReport[] entries =
                        config.entries.Where(entry =>
                            entry.sourcePathGroupSha256 == group.stableIdentitySha256).ToArray();
                    Assert.That(entries, Has.Length.EqualTo(group.configEntryCount));
                    if (group.configEntryCount == 1 && group.sceneCandidateCount == 1)
                    {
                        Assert.That(group.resolution, Is.EqualTo("Resolved"));
                        Assert.That(entries[0].resolvedHierarchyPath, Is.EqualTo(group.candidates[0]));
                        continue;
                    }

                    Assert.That(group.resolution, Is.EqualTo("Unresolved"));
                    Assert.That(group.decisionOwner, Is.Not.Empty);
                    Assert.That(entries, Has.All.Matches<
                        OperationMapPhase0PlacementOwnershipProbe.PlacementEntryReport>(entry =>
                            entry.sourceResolution == "Unresolved" &&
                            entry.sourceOwnership == "Mixed" &&
                            entry.sourceHiding == "Unresolved" &&
                            entry.resolvedHierarchyPath == string.Empty &&
                            entry.decisionOwner == group.decisionOwner));
                }
            }
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

        [TestCase("sourcePath")]
        [TestCase("sourcePathGroupSha256")]
        [TestCase("sourceResolution")]
        [TestCase("resolvedHierarchyPath")]
        [TestCase("sourceOwnership")]
        [TestCase("sourceHiding")]
        [TestCase("decisionOwner")]
        [TestCase("category")]
        [TestCase("sourceKey")]
        [TestCase("factionId")]
        [TestCase("configSourcePathOccurrenceCount")]
        [TestCase("sceneCandidateCount")]
        [TestCase("worldCenter")]
        [TestCase("worldPosition")]
        [TestCase("worldEulerAngles")]
        [TestCase("worldScale")]
        [TestCase("yawDegrees")]
        [TestCase("rotateVertical")]
        [TestCase("prefabAssetPath")]
        [TestCase("prefabAssetGuid")]
        [TestCase("prefabLocalId")]
        [TestCase("prefabType")]
        public void Comparer_DistinguishesEveryStableIdentityField(string field)
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementEntryReport original =
                LoadCommittedReport().placementConfigs[0].entries[0];
            OperationMapPhase0PlacementOwnershipProbe.PlacementEntryReport changed =
                JsonUtility.FromJson<OperationMapPhase0PlacementOwnershipProbe.PlacementEntryReport>(
                    JsonUtility.ToJson(original));
            MutateStableIdentityField(changed, field);

            int forward = OperationMapPhase0PlacementOwnershipProbe.ComparePlacementEntries(original, changed);
            int reverse = OperationMapPhase0PlacementOwnershipProbe.ComparePlacementEntries(changed, original);
            Assert.That(forward, Is.Not.Zero, field);
            Assert.That(reverse, Is.EqualTo(-forward), field);
            Assert.That(
                OperationMapPhase0PlacementOwnershipProbe.BuildPlacementStableIdentity(changed),
                Is.Not.EqualTo(OperationMapPhase0PlacementOwnershipProbe.BuildPlacementStableIdentity(original)),
                field);
        }

        [Test]
        public void RequiredShape_RejectsHierarchyEvidenceDrift()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            report.placementConfigs[0].sourcePathGroups[0].candidates[0] += "/drift";
            AssertInvalid(report);
        }

        [Test]
        public void RequiredShape_RejectsCorrelatedPayloadAndHashEdits()
        {
            OperationMapPhase0PlacementOwnershipProbe.PlacementOwnershipReport report = LoadCommittedReport();
            OperationMapPhase0PlacementOwnershipProbe.PlacementEntryReport entry =
                report.placementConfigs[0].entries[0];
            entry.prefab.assetPath += ".drift";
            entry.stableIdentitySha256 = OperationMapPhase0BaselineProbe.ComputeSha256(
                System.Text.Encoding.UTF8.GetBytes(
                    OperationMapPhase0PlacementOwnershipProbe.BuildPlacementStableIdentity(entry)));
            report.placementConfigs[0].identityAggregateSha256 =
                OperationMapPhase0PlacementOwnershipProbe.ComputePlacementAggregate(
                    report.placementConfigs[0].entries);
            report.identityPayloadSha256 =
                OperationMapPhase0PlacementOwnershipProbe.ComputeIdentityPayloadSha256(
                    report.placementConfigs);

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

        private static void MutateStableIdentityField(
            OperationMapPhase0PlacementOwnershipProbe.PlacementEntryReport entry,
            string field)
        {
            switch (field)
            {
                case "sourcePath": entry.sourcePath += "x"; break;
                case "sourcePathGroupSha256": entry.sourcePathGroupSha256 = new string('f', 64); break;
                case "sourceResolution": entry.sourceResolution += "x"; break;
                case "resolvedHierarchyPath": entry.resolvedHierarchyPath += "x"; break;
                case "sourceOwnership": entry.sourceOwnership += "x"; break;
                case "sourceHiding": entry.sourceHiding += "x"; break;
                case "decisionOwner": entry.decisionOwner += "x"; break;
                case "category": entry.category += "x"; break;
                case "sourceKey": entry.sourceKey += "x"; break;
                case "factionId": entry.factionId++; break;
                case "configSourcePathOccurrenceCount": entry.configSourcePathOccurrenceCount++; break;
                case "sceneCandidateCount": entry.sceneCandidateCount++; break;
                case "worldCenter": entry.worldCenter.x += 1f; break;
                case "worldPosition": entry.worldPosition.x += 1f; break;
                case "worldEulerAngles": entry.worldEulerAngles.x += 1f; break;
                case "worldScale": entry.worldScale.x += 1f; break;
                case "yawDegrees": entry.yawDegrees += 1f; break;
                case "rotateVertical": entry.rotateVertical = !entry.rotateVertical; break;
                case "prefabAssetPath": entry.prefab.assetPath += "x"; break;
                case "prefabAssetGuid": entry.prefab.assetGuid = new string('f', 32); break;
                case "prefabLocalId": entry.prefab.localId++; break;
                case "prefabType": entry.prefab.type += "x"; break;
                default: throw new ArgumentOutOfRangeException(nameof(field), field, null);
            }
        }
    }
}

#endif
