using System;
using Game.Operations.Content;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Tactical;

namespace Game.Tests.Editor.Operations
{
    /// <summary>
    /// Focused mobile-ready checks for O001–O003 coach / escort / result / partial / practice.
    /// Host marker: <see cref="PassMarker"/>.
    /// </summary>
    public static class OperationsMobileReadyChecks
    {
        public const int ExpectedCheckCount = 8;
        public const string PassMarker = "[OperationsMobileReadyValidation] result=Passed checks=8";

        public static void RunAll()
        {
            CoachScanEvidenceExtractOrder();
            CoachNeverBlocksLongerThanThreeSeconds();
            EscortFatThumbChipsAndClinicWarning();
            RepairChipsAndPumpWarning();
            ResultScreenOutcomeDeltasAndContinue();
            PartialTeachConcludeSeparateFromWithdraw();
            O003HoldTrimmedWithDefendWave();
            PracticeSurfacedFromFailResult();
        }

        public static void CoachScanEvidenceExtractOrder()
        {
            OperationsLoopSession loop = Reach("operation.o001", 2101);
            var coach = new OperationsOnboardingCoach();
            Require(coach.TryRead(loop, out OperationsCoachFrame scan));
            Require(scan.Step == OperationsCoachStepKind.Scan);
            Require(scan.StepIndex == 1 && scan.StepCount == 3);
            Require(scan.TitleKey == "operations.coach.o001.scan.title");
            Require(OperationsLocalizedCopy.Require(scan.TitleKey, "en").Length > 0);
            Require(OperationsLocalizedCopy.Require(scan.BodyKey, "fa").Length > 0);

            // Advance to evidence step via scan completion path used by ARIA.
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            Require(map.TryGetByAlias("site.signal_a", out OperationsGreyboxAnchor a));
            Require(map.TryGetByAlias("site.signal_b", out OperationsGreyboxAnchor b));
            Require(map.TryGetByAlias("site.signal_c", out OperationsGreyboxAnchor c));
            Require(loop.Move("unit.d01.recon.01", a.AnchorId).Accepted);
            Require(loop.Move("unit.d01.recon.02", b.AnchorId).Accepted);
            Require(loop.Move("unit.d01.rifle.01", c.AnchorId).Accepted);
            for (int step = 0; step < 8; step++)
                loop.Advance(1);
            Require(loop.Observe("unit.d01.recon.01", "site.d01.signal_a").Accepted);
            Require(loop.Scan("unit.d01.recon.01", "site.d01.signal_a").Accepted);
            Require(loop.Observe("unit.d01.recon.02", "site.d01.signal_b").Accepted);
            Require(loop.Scan("unit.d01.recon.02", "site.d01.signal_b").Accepted);
            Require(loop.Observe("unit.d01.rifle.01", "site.d01.signal_c").Accepted);
            Require(loop.Scan("unit.d01.rifle.01", "site.d01.signal_c").Accepted);
            for (int step = 0; step < 20; step++)
                loop.Advance(1);
            Require(NodeComplete(loop, "scan_signals"));

            coach.Reset();
            Require(coach.TryRead(loop, out OperationsCoachFrame evidence));
            Require(evidence.Step == OperationsCoachStepKind.Evidence);
            Require(evidence.StepIndex == 2);
            Require(evidence.FocusNodeId == "interact_relay");
        }

        public static void CoachNeverBlocksLongerThanThreeSeconds()
        {
            OperationsLoopSession loop = Reach("operation.o001", 2102);
            var coach = new OperationsOnboardingCoach();
            Require(coach.TryRead(loop, out OperationsCoachFrame frame));
            Require(frame.InputBlocked);
            Require(frame.InputBlockRemainingSeconds <= OperationsOnboardingCoach.MaxInputBlockSeconds);
            Require(OperationsOnboardingCoach.MaxInputBlockSeconds == 3f);

            coach.AdvanceSeconds(1.5f);
            Require(coach.IsInputBlocked);
            Require(coach.InputBlockRemainingSeconds <= 1.5f + 0.001f);

            coach.AdvanceSeconds(1.6f);
            Require(!coach.IsInputBlocked);
            Require(coach.InputBlockRemainingSeconds == 0f);
            Require(coach.TryRead(loop, out frame));
            Require(!frame.InputBlocked);

            // Fresh step highlight, then player input clears immediately.
            coach.Reset();
            Require(coach.TryRead(loop, out _));
            Require(coach.IsInputBlocked);
            coach.NotifyPlayerInput();
            Require(!coach.IsInputBlocked);
        }

        public static void EscortFatThumbChipsAndClinicWarning()
        {
            OperationsLoopSession loop = Reach("operation.o002", 2103);
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            Require(map.TryGetByAlias("site.junction", out OperationsGreyboxAnchor junction));
            Require(loop.Move("unit.d01.recon.01", junction.AnchorId).Accepted);
            for (int step = 0; step < 8; step++)
                loop.Advance(1);
            Require(loop.Observe("unit.d01.recon.01", "site.d01.junction").Accepted);
            Require(loop.Scan("unit.d01.recon.01", "site.d01.junction").Accepted);
            for (int step = 0; step < 20; step++)
                loop.Advance(1);
            Require(NodeComplete(loop, "scan_junction"));
            loop.Advance(1);

            Require(OperationsEscortRepairControls.TryRead(loop, out OperationsEscortRepairControlFrame controls));
            Require(controls.MissionId == "operation.o002");
            Require(controls.WarningVisible);
            Require(controls.WarningKey == "operations.warning.clinic");
            Require(HasChip(controls, OperationsFatThumbChipKind.Go, enabled: true));
            Require(HasChip(controls, OperationsFatThumbChipKind.Hold, enabled: true));
            Require(HasRouteChip(controls, "route.main"));
            Require(HasRouteChip(controls, "route.safe"));
            Require(OperationsLocalizedCopy.Require("operations.controls.escort.go", "en") == "Go");
            Require(OperationsLocalizedCopy.Require("operations.warning.clinic", "fa").Length > 0);
        }

        public static void RepairChipsAndPumpWarning()
        {
            OperationsLoopSession loop = Reach("operation.o003", 2104);
            Require(OperationsEscortRepairControls.TryRead(loop, out OperationsEscortRepairControlFrame controls));
            Require(controls.MissionId == "operation.o003");
            Require(controls.WarningVisible);
            // Pumps start at 25 HP → damaged warning.
            Require(controls.WarningKey == "operations.warning.pumps");
            Require(HasChip(controls, OperationsFatThumbChipKind.Repair, enabled: false) ||
                    HasChip(controls, OperationsFatThumbChipKind.Repair, enabled: true));
        }

        public static void ResultScreenOutcomeDeltasAndContinue()
        {
            OperationsLoopSession loop = Reach("operation.o001", 2105);
            Require(OperationsAriaInputSkills.TryPlayVisibleControlWin(loop, "operation.o001"));
            FinishToResult(loop);
            Require(OperationsMissionResultProjection.TryRead(loop, out OperationsMissionResultUiFrame ui));
            Require(ui.Outcome == OperationsOutcomeKind.Victory);
            Require(ui.OutcomeKey == "operations.o001.result.victory");
            Require(ui.ContinueKey == "operations.result.continue");
            Require(ui.Deltas.Length == 3);
            Require(ui.Deltas[0].Metric == OperationsDistrictMetricKind.Trust);
            Require(ui.Deltas[1].Metric == OperationsDistrictMetricKind.IntelConfidence);
            Require(ui.Deltas[2].Metric == OperationsDistrictMetricKind.Heat);
            Require(ui.Deltas[0].Delta != 0 || ui.Deltas[1].Delta != 0 || ui.Deltas[2].Delta != 0);
            Require(OperationsLocalizedCopy.Require(ui.ContinueKey, "en") == "Continue");
            // Victory path: Practice CTA not required (fail/result surface is separate).
            Require(!ui.PracticeAvailable);
        }

        public static void PartialTeachConcludeSeparateFromWithdraw()
        {
            OperationsLoopSession partial = Reach("operation.o001", 2106);
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            Require(map.TryGetByAlias("site.signal_a", out OperationsGreyboxAnchor a));
            Require(map.TryGetByAlias("site.signal_b", out OperationsGreyboxAnchor b));
            Require(partial.Move("unit.d01.rifle.01", a.AnchorId).Accepted);
            Require(partial.Move("unit.d01.rifle.02", b.AnchorId).Accepted);
            for (int step = 0; step < 8; step++)
                partial.Advance(1);
            Require(partial.Observe("unit.d01.rifle.01", "site.d01.signal_a").Accepted);
            Require(partial.Scan("unit.d01.rifle.01", "site.d01.signal_a").Accepted);
            Require(partial.Observe("unit.d01.rifle.02", "site.d01.signal_b").Accepted);
            Require(partial.Scan("unit.d01.rifle.02", "site.d01.signal_b").Accepted);
            for (int step = 0; step < 20; step++)
                partial.Advance(1);
            Require(partial.Extract("unit.d01.rifle.01").Accepted);
            Require(partial.Extract("unit.d01.rifle.02").Accepted);
            for (int step = 0; step < 30; step++)
                partial.Advance(1);

            Require(partial.ConcludeAvailable);
            Require(OperationsPartialTeach.TryRead(partial, out OperationsPartialTeachFrame teach));
            Require(teach.ConcludeVisible);
            Require(teach.WithdrawVisible);
            Require(teach.WithdrawSeparateFromConclude);
            Require(teach.ConcludeStakesKey == "operations.o001.result.partial");
            Require(teach.ConcludeTitleKey != teach.WithdrawTitleKey);

            Require(partial.ConcludeMission().Accepted);
            Require(partial.MissionOutcome == OperationsOutcomeKind.Partial);
            Finish(partial);
            Require(partial.Credits == 0);
        }

        public static void O003HoldTrimmedWithDefendWave()
        {
            Require(OperationsAuthoredMissions.TryCompile(
                "operation.o003",
                out OperationsCompiledTactical o003,
                out string hash,
                out string error), error);
            Require(hash == OperationsAuthoredMissions.O003Hash);
            Require(hash == "ops-authored-o003-v2");

            bool foundHold = false;
            for (int index = 0; index < o003.Nodes.Length; index++)
            {
                if (o003.Nodes[index].NodeId != "hold_service_court")
                    continue;
                Require(o003.Nodes[index].DurationTicks == 20, "hold_duration");
                foundHold = true;
            }

            Require(foundHold);

            bool foundDefendWave = false;
            for (int index = 0; index < o003.Waves.Length; index++)
            {
                if (o003.Waves[index].Group == 2 &&
                    o003.Waves[index].TriggerNodeId == "repair_pump_east")
                {
                    Require(o003.Waves[index].WarningSeconds >= OperationsTacticalRules.MinimumWarningSeconds);
                    foundDefendWave = true;
                }
            }

            Require(foundDefendWave);

            // AriaWon path still completes after trim.
            OperationsLoopSession loop = Reach("operation.o003", 2107);
            Require(OperationsAriaInputSkills.TryPlayVisibleControlWin(loop, "operation.o003"));
            Require(loop.MissionOutcome == OperationsOutcomeKind.Victory);
        }

        public static void PracticeSurfacedFromFailResult()
        {
            OperationsLoopSession loop = Reach("operation.o002", 2108);
            Require(loop.WithdrawMission().Accepted);
            Require(loop.MissionOutcome == OperationsOutcomeKind.Withdrawn);
            FinishToResult(loop);
            Require(OperationsMissionResultProjection.TryRead(loop, out OperationsMissionResultUiFrame ui));
            Require(ui.PracticeAvailable);
            Require(ui.PracticeKey == "operations.result.practice");
            Require(ui.ZeroCredits);
            Require(ui.Outcome == OperationsOutcomeKind.Withdrawn);
            Require(ui.Deltas.Length == 3);

            string practiceId = Id();
            Require(OperationsMissionResultProjection.TryContinueAndPractice(loop, practiceId, out string reason), reason);
            Require(loop.Phase == OperationsLoopPhase.Reserved || loop.HasPending);
        }

        static int _nextId = 0xD100;

        static string Id() => "cmd.operations." + (_nextId++).ToString("x8");

        static OperationsLoopSession NewLoop(int seed) =>
            OperationsLoopSession.Create(seed, new byte[] { 9, 9, 9 }, new byte[] { 8, 8 });

        static void OpenBriefing(OperationsLoopSession loop, string missionId)
        {
            Require(loop.TryOffer(missionId, out OperationsOfferSaveData offer), missionId);
            int number = DistrictNumber(offer.districtId);
            Require(loop.OpenDistrict(number).Accepted);
            Require(loop.OpenBriefing(offer.offerId).Accepted);
        }

        static void ReachActive(OperationsLoopSession loop, string missionId)
        {
            OpenBriefing(loop, missionId);
            string deployId = Id();
            Require(loop.BeginDeploy(deployId).Accepted);
            Require(loop.CompleteAttempt(deployId).Accepted);
            Require(loop.BeginLaunch().Accepted);
            Require(loop.CompleteLaunch().Accepted);
            Require(loop.BeginActive(true, true, true, loop.ContentHash).Accepted);
            Require(loop.CompleteActive().Accepted);
        }

        static OperationsLoopSession Reach(string missionId, int seed)
        {
            OperationsLoopSession loop = NewLoop(seed);
            if (missionId == "operation.o003" && !loop.TryOffer(missionId, out _))
            {
                ReachActive(loop, "operation.o001");
                Require(OperationsAriaInputSkills.TryPlayVisibleControlWin(loop, "operation.o001"));
                Finish(loop);
                Require(loop.RequestEndDay(Id()).Accepted);
            }

            ReachActive(loop, missionId);
            return loop;
        }

        static void FinishToResult(OperationsLoopSession loop)
        {
            Require(loop.BeginResult().Accepted);
            Require(loop.CompleteResult().Accepted);
            string settleId = Id();
            Require(loop.BeginSettlement(settleId).Accepted);
            Require(loop.CompleteSettlement(settleId).Accepted);
        }

        static void Finish(OperationsLoopSession loop)
        {
            FinishToResult(loop);
            Require(loop.BeginReturn().Accepted);
            Require(loop.CompleteReturn().Accepted);
        }

        static int DistrictNumber(string districtId)
        {
            for (int number = 1; number <= 6; number++)
            {
                if (OperationsIdentityRules.DistrictId(number) == districtId)
                    return number;
            }

            throw new InvalidOperationException(districtId);
        }

        static bool NodeComplete(OperationsLoopSession loop, string nodeId) =>
            loop.TryNode(nodeId, out OperationsTacticalNodeState state) &&
            state.Phase == OperationsTacticalNodePhase.Complete;

        static bool HasChip(OperationsEscortRepairControlFrame frame, OperationsFatThumbChipKind kind, bool enabled)
        {
            for (int index = 0; index < frame.Chips.Length; index++)
            {
                if (frame.Chips[index].Kind == kind && frame.Chips[index].Enabled == enabled)
                    return true;
            }

            return false;
        }

        static bool HasRouteChip(OperationsEscortRepairControlFrame frame, string routeId)
        {
            for (int index = 0; index < frame.Chips.Length; index++)
            {
                if (frame.Chips[index].Kind == OperationsFatThumbChipKind.Route &&
                    frame.Chips[index].RouteId == routeId &&
                    frame.Chips[index].Enabled)
                    return true;
            }

            return false;
        }

        static void Require(bool condition, string message = "require")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
