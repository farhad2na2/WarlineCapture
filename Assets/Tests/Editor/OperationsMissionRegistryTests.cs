using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tests.Editor
{
    // W38 (gap table step 2): the mission-keyed Operations registry and the pure objective-graph reducer steps.
    public sealed class OperationsMissionRegistryTests
    {
        [Test]
        public void RegistryResolvesO001ToItsShippedConfigAndKeysOtherMissionsByCode()
        {
            Assert.AreEqual("Operations/StreetSignals", OperationsMissionRegistry.ResourcePath("operation.o001"), "O001 keeps its shipped resource");
            Assert.AreEqual("Operations/Missions/O002", OperationsMissionRegistry.ResourcePath("operation.o002"));
            Assert.IsNull(OperationsMissionRegistry.ResourcePath("operation.o999"), "not a catalog mission");
            Assert.IsNull(OperationsMissionRegistry.ResourcePath("skirmish.base_assault"));
            Assert.AreEqual("O002", OperationsMissionRegistry.Code("operation.o002"));
            Assert.IsNull(OperationsMissionRegistry.Code("campaign.m01"));
            Assert.AreEqual("operations.o001.", OperationsMissionRegistry.CopyPrefix("operation.o001"));
            Assert.AreEqual(1102u, OperationsMissionRegistry.NewRunSeed("operation.o001"), "O001's new run keeps seed 1102 (its catalog canonical seed)");
            Assert.AreEqual(1103u, OperationsMissionRegistry.NewRunSeed("operation.o002"));
            var config = OperationsMissionRegistry.Load("operation.o001");
            Assert.IsNotNull(config, "O001 is registered");
            Assert.AreEqual("operation.o001", config.missionId);
            Assert.IsTrue(config.TryValidate(out string error), error);
            Assert.IsTrue(OperationsMissionRegistry.IsRegistered("operation.o001"));
            Assert.IsFalse(OperationsMissionRegistry.IsRegistered("operation.o002"), "no native O002 config yet");
            Assert.IsFalse(OperationsMissionRegistry.IsRegistered(null));
        }

        [Test]
        public void O001ConfigKeepsItsRulesAndHudRestrictions()
        {
            var config = OperationsMissionRegistry.Load("operation.o001");
            Assert.IsNotNull(config);
            Assert.AreEqual(3, config.requiredScans); Assert.AreEqual(3, config.scanPositions.Length);
            Assert.AreEqual(15f, config.scanSeconds); Assert.AreEqual(15f, config.evidenceSeconds); Assert.AreEqual(720f, config.deadlineSeconds);
            Assert.AreEqual(127, config.HudFlags, "building, production, economy, transport, air disabled; logistics and unrelated controls hidden");
            Assert.AreEqual(1, config.hudAvailableSquadMask);
            Assert.IsTrue(OperationsReconMissionConfig.TryGetPackageSize("FP_LIGHT", out int units, out int recon));
            Assert.AreEqual(16, units); Assert.AreEqual(2, recon);
            Assert.IsTrue(OperationsReconMissionConfig.TryGetPackageSize("EP_CELL", out units, out _)); Assert.AreEqual(20, units);
            Assert.IsFalse(OperationsReconMissionConfig.TryGetPackageSize("FP_UNKNOWN", out _, out _));
            // A config claiming another catalog mission's identity no longer validates against O001's catalog row.
            var copy = UnityEngine.Object.Instantiate(config);
            try
            {
                copy.missionId = "operation.o002";
                Assert.IsFalse(copy.TryValidate(out _), "O002 is ESCORT, not the RECON runtime");
                copy.missionId = "operation.o001"; copy.deadlineSeconds = 700f;
                Assert.IsFalse(copy.TryValidate(out _), "deadline must match the catalog (720 s)");
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
        }

        [Test]
        public void ScanStepAdvancesCompletesOrCancels()
        {
            var actor = new Entity { Index = 7, Version = 1 };
            var site = new OperationsReconSiteElement { Actor = actor, Radius = 8f };
            Assert.IsFalse(OperationsObjectiveGraphReducer.StepScan(ref site, true, 15f, 10f));
            Assert.AreEqual(10f, site.ChannelSeconds, 1e-5f); Assert.AreEqual(actor, site.Actor);
            Assert.IsTrue(OperationsObjectiveGraphReducer.StepScan(ref site, true, 15f, 10f), "completes at the scan time");
            Assert.AreEqual(15f, site.ChannelSeconds, 1e-5f); Assert.AreEqual(1, site.Completed); Assert.AreEqual(Entity.Null, site.Actor);
            Assert.IsFalse(OperationsObjectiveGraphReducer.StepScan(ref site, true, 15f, 10f), "a completed site never completes twice");
            var broken = new OperationsReconSiteElement { Actor = actor, ChannelSeconds = 9f };
            Assert.IsFalse(OperationsObjectiveGraphReducer.StepScan(ref broken, false, 15f, 1f));
            Assert.AreEqual(Entity.Null, broken.Actor); Assert.AreEqual(0f, broken.ChannelSeconds, "an interrupted scan restarts from zero");
            var idle = new OperationsReconSiteElement();
            Assert.IsFalse(OperationsObjectiveGraphReducer.StepScan(ref idle, true, 15f, 1f)); Assert.AreEqual(0f, idle.ChannelSeconds);
        }

        [Test]
        public void InteractAndCarryKeepEvidenceRecoverable()
        {
            var actor = new Entity { Index = 3, Version = 1 };
            var evidence = new OperationsReconEvidenceComponent { Actor = actor, Position = new float3(1, 0, 1) };
            Assert.IsFalse(OperationsObjectiveGraphReducer.StepInteract(ref evidence, true, 15f, 14f));
            Assert.IsTrue(OperationsObjectiveGraphReducer.StepInteract(ref evidence, true, 15f, 2f));
            Assert.AreEqual(actor, evidence.Carrier); Assert.AreEqual(Entity.Null, evidence.Actor); Assert.AreEqual(1, evidence.Recovered);
            OperationsObjectiveGraphReducer.CarryEvidence(ref evidence, true, new float3(5, 0, 6), true);
            Assert.AreEqual(new float3(5, 0, 6), evidence.Position); Assert.AreEqual(actor, evidence.Carrier);
            OperationsObjectiveGraphReducer.CarryEvidence(ref evidence, true, new float3(9, 0, 9), false);
            Assert.AreEqual(new float3(9, 0, 9), evidence.Position, "dropped at the carrier's last position");
            Assert.AreEqual(Entity.Null, evidence.Carrier); Assert.AreEqual(1, evidence.Recovered);
            var interrupted = new OperationsReconEvidenceComponent { Actor = actor, ChannelSeconds = 5f };
            Assert.IsFalse(OperationsObjectiveGraphReducer.StepInteract(ref interrupted, false, 15f, 1f));
            Assert.AreEqual(Entity.Null, interrupted.Actor); Assert.AreEqual(0f, interrupted.ChannelSeconds);
        }

        [Test]
        public void ExtractResolutionKeepsTheO001PriorityOrder()
        {
            OperationsReconOutcome R(int alive, int scans, int atExit, bool evidence, float t, bool withdraw, bool conclude, out bool partial) =>
                OperationsObjectiveGraphReducer.Resolve(alive, scans, 3, atExit, evidence, t, 720f, withdraw, conclude, out partial);
            Assert.AreEqual(OperationsReconOutcome.Defeat, R(1, 3, 2, true, 10, false, false, out _), "fewer than two survivors");
            Assert.AreEqual(OperationsReconOutcome.Victory, R(5, 3, 2, true, 10, true, true, out _), "victory before withdraw/conclude");
            Assert.AreEqual(OperationsReconOutcome.None, R(5, 3, 2, false, 10, false, false, out bool partial)); Assert.IsTrue(partial);
            Assert.AreEqual(OperationsReconOutcome.Partial, R(5, 2, 2, false, 720, false, false, out _), "deadline with a partial");
            Assert.AreEqual(OperationsReconOutcome.Defeat, R(5, 1, 2, false, 720, false, false, out partial), "deadline without a partial"); Assert.IsFalse(partial);
            Assert.AreEqual(OperationsReconOutcome.Withdraw, R(5, 2, 2, false, 10, true, true, out _), "withdraw before conclude");
            Assert.AreEqual(OperationsReconOutcome.Partial, R(5, 2, 2, false, 10, false, true, out _), "conclude with a partial");
            Assert.AreEqual(OperationsReconOutcome.None, R(5, 1, 2, false, 10, false, true, out _), "conclude without a partial is ignored");
        }
    }
}
