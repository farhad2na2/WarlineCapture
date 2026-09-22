using System;
using System.IO;
using Game.Operations.Content;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Tactical;

namespace Game.Tests.Editor.Operations
{
    /// <summary>
    /// Focused mobile-ready checks for O001–O003 coach / escort / result / partial / practice.
    /// Host marker: <see cref="PassMarker"/> (checks=8).
    /// Landed presentation/pacing guards run in the same pass and do not add a second marker.
    /// </summary>
    public static class OperationsMobileReadyChecks
    {
        public const int ExpectedCheckCount = 8;
        public const string PassMarker = "[OperationsMobileReadyValidation] result=Passed checks=8";

        public static void RunAll()
        {
            // Landed shell/pacing guards. They must pass; the marker stays checks=8.
            RunLandedPresentationGuards();
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
            Require(loop.ProfileRevision >= 0, "profile_revision");

            int trust = ui.Deltas[0].Delta;
            int intel = ui.Deltas[1].Delta;
            int heat = ui.Deltas[2].Delta;
            Require(loop.BeginReturn().Accepted);
            Require(loop.CompleteReturn().Accepted);
            Require(loop.RequestEndDay(Id()).Accepted);
            Require(OperationsMissionResultProjection.TryRead(loop, out OperationsMissionResultUiFrame afterDay));
            Require(afterDay.MissionId == "operation.o001", afterDay.MissionId);
            Require(afterDay.Outcome == OperationsOutcomeKind.Victory);
            Require(afterDay.Deltas[0].Delta == trust && afterDay.Deltas[1].Delta == intel && afterDay.Deltas[2].Delta == heat, "result_lag");
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


        public static void EscortChromeNeverExceedsRequired()
        {
            Require(OperationsObjectiveChrome.FormatProgress(3, 2) == "2/2", "cap_3_of_2");
            Require(OperationsObjectiveChrome.FormatProgress(0, 2) == "0/2", "cap_0_of_2");
            Require(OperationsObjectiveChrome.ClampProgress(3, 2) == 2, "clamp");
            string title = OperationsLocalizedCopy.Require("operations.objective.escort_trucks", "en");
            string primary = OperationsLocalizedCopy.Require("operations.o002.objective.primary", "en");
            Require(title.IndexOf("two", StringComparison.Ordinal) >= 0, "escort_title_two");
            Require(primary.IndexOf("two", StringComparison.Ordinal) >= 0, "escort_primary_two");
            Require(title.IndexOf("ten", StringComparison.OrdinalIgnoreCase) < 0, "escort_title_ten");
            string presentation = ReadPresentationSource();
            Require(presentation.Contains("FormatProgress"), "presenter_caps_progress");
        }

        public static void ProtectChecklistLatchesWithOutcome()
        {
            OperationsLoopSession loop = Reach("operation.o002", 2110);
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

            // While the run is live, Protect may show as surviving. It is not an incomplete gate.
            Require(loop.PlayHudInProgress, "still_live");
            Require(loop.TryReadHud(out OperationsHudFrame hud));
            Require(HudContains(hud, "hold_clinic"), "hold_listed");
            Require(HudContains(hud, "protect_clinic"), "protect_listed");
            Require(HudActive(hud, "protect_clinic"), "protect_ongoing");
            Require(!HudActive(hud, "hold_clinic"), "hold_not_active_yet");
            Require(loop.TryNode("protect_clinic", out OperationsTacticalNodeState protect));
            Require(protect.Phase == OperationsTacticalNodePhase.Active, "protect_still_watching");
            Require(loop.TryNode("escort_trucks", out OperationsTacticalNodeState escort));
            Require(escort.ProgressCount <= escort.TargetCount, "escort_count");

            OperationsLoopSession won = Reach("operation.o002", 2112);
            Require(OperationsAriaInputSkills.TryPlayUnassistedWin(won));
            Require(won.MissionOutcome == OperationsOutcomeKind.Victory, "protect_victory");
            Require(!won.PlayHudInProgress, "hud_not_in_progress");
            Require(won.TryMissionTick(out int tick), "victory_tick");
            won.Advance(1);
            Require(won.TryMissionTick(out int heldTick) && heldTick == tick, "no_extra_advance");
            Require(won.TryReadHud(out OperationsHudFrame victoryHud));
            Require(HudContains(victoryHud, "protect_clinic"), "protect_held_listed");
            Require(!HudActive(victoryHud, "protect_clinic"), "protect_not_active");
            Require(HudComplete(victoryHud, "protect_clinic"), "protect_done");
            Require(won.TryNode("protect_clinic", out OperationsTacticalNodeState held));
            Require(held.Phase == OperationsTacticalNodePhase.Complete, "protect_phase_held");

            OperationsLoopSession doomed = Reach("operation.o002", 2111);
            Require(doomed.DestroySite("site.d01.clinic").Accepted);
            for (int step = 0; step < 5 && doomed.PlayHudInProgress; step++)
                doomed.Advance(1);
            Require(doomed.MissionOutcome == OperationsOutcomeKind.Defeat, "protect_fail");
            Require(doomed.MissionOutcome != OperationsOutcomeKind.Victory, "protect_not_victory");
            Require(doomed.MissionOutcome != OperationsOutcomeKind.Partial, "protect_not_partial");
            Require(!doomed.PlayHudInProgress, "defeat_not_in_progress");
            Require(doomed.TryNode("protect_clinic", out OperationsTacticalNodeState failed));
            Require(failed.Phase == OperationsTacticalNodePhase.Failed, "protect_failed_phase");
            Require(doomed.TryReadHud(out OperationsHudFrame defeatHud));
            Require(!HudActive(defeatHud, "protect_clinic"), "protect_left_active");
            Require(HudFailed(defeatHud, "protect_clinic"), "protect_failed_row");

            string presentation = ReadPresentationSource();
            Require(presentation.Contains("PlayHudInProgress"), "presenter_leaves_in_progress");
            Require(presentation.Contains("operations.hud.held"), "protect_held_copy");
            Require(presentation.Contains("operations.hud.surviving"), "protect_surviving_copy");
            string capture = File.ReadAllText(Path.Combine(
                FindRepoRoot(),
                "Assets",
                "Tests",
                "Editor",
                "Operations",
                "OperationsAriaPlayModeCapture.cs"));
            Require(capture.Contains("PlayHudInProgress"), "capture_same_latch");
            Require(capture.Contains("ShowMissionResult"), "capture_result_card");
            Require(capture.Contains("MayStampVictoryEvidence"), "capture_revision_guard");
        }

        static bool HudContains(OperationsHudFrame hud, string nodeId)
        {
            for (int index = 0; index < hud.Required.Length; index++)
            {
                if (hud.Required[index].NodeId == nodeId)
                    return true;
            }

            return false;
        }

        static bool HudActive(OperationsHudFrame hud, string nodeId)
        {
            for (int index = 0; index < hud.Required.Length; index++)
            {
                if (hud.Required[index].NodeId == nodeId)
                    return hud.Required[index].Active;
            }

            return false;
        }

        static bool HudComplete(OperationsHudFrame hud, string nodeId)
        {
            for (int index = 0; index < hud.Required.Length; index++)
            {
                if (hud.Required[index].NodeId == nodeId)
                    return hud.Required[index].Complete;
            }

            return false;
        }

        static bool HudFailed(OperationsHudFrame hud, string nodeId)
        {
            for (int index = 0; index < hud.Required.Length; index++)
            {
                if (hud.Required[index].NodeId == nodeId)
                    return hud.Required[index].Failed;
            }

            return false;
        }

        public static void ContentFramesBoundIntoLandedShell()
        {
            string presentation = ReadPresentationSource();
            Require(presentation.Contains("OperationsOnboardingCoach"), "coach_bind");
            Require(presentation.Contains("OperationsEscortRepairControls"), "escort_bind");
            Require(presentation.Contains("OperationsMissionResultProjection"), "result_bind");
            Require(presentation.Contains("ShowMissionResult"), "result_show");
            Require(presentation.Contains("operations.result.continue"), "continue_key");
            Require(presentation.Contains("OperationsAriaPlayModePresentation"), "landed_shell");
            Require(!presentation.Contains("class OperationsMobileReadyVoidHud"), "no_second_void_ui");
        }

        public static void MobileReadyEvidenceCaptureWired()
        {
            string root = FindRepoRoot();
            string capturePath = Path.Combine(
                root,
                "Assets",
                "Tests",
                "Editor",
                "Operations",
                "OperationsMobileReadyPlayModeCapture.cs");
            Require(File.Exists(capturePath), "capture_source");
            string capture = File.ReadAllText(capturePath);
            Require(capture.Contains("RunO001Coach"), "capture_o001");
            Require(capture.Contains("RunO002Escort"), "capture_o002");
            Require(capture.Contains("RunResultDeltas"), "capture_result");
            Require(capture.Contains("RunAllEvidence"), "capture_all");
            Require(capture.Contains("o001-coach-scan.en.png"), "png_scan");
            Require(capture.Contains("o001-coach-evidence.en.png"), "png_evidence");
            Require(capture.Contains("o001-coach-extract.en.png"), "png_extract");
            Require(capture.Contains("o002-escort-chips.en.png"), "png_escort");
            Require(capture.Contains("result-trust-intel-heat.en.png"), "png_result");
            Require(capture.Contains("mobile-ready-o001-o003"), "evidence_root");
            Require(capture.Contains("_Evidence"), "evidence_dir");
            Require(capture.Contains("Operations/Mobile Ready/Capture O001 Coach"), "menu_coach");
            Require(capture.Contains("Operations/Mobile Ready/Capture O002 Escort Chips"), "menu_escort");
            Require(capture.Contains("Operations/Mobile Ready/Capture Result Deltas"), "menu_result");
            Require(capture.Contains("OperationsAriaPlayModePresentation"), "uses_landed_shell");
            string invokePath = Path.Combine(root, "Tools", "Operations", "Invoke-OperationsMobileReadyCapture.ps1");
            Require(File.Exists(invokePath), "invoke_capture");
            string invoke = File.ReadAllText(invokePath);
            Require(invoke.Contains("OperationsMobileReadyPlayModeCapture.RunAllEvidence"), "invoke_all");
            Require(invoke.Contains("OperationsMobileReadyPlayModeCapture.RunO001Coach"), "invoke_coach");
            Require(invoke.Contains("OperationsMobileReadyPlayModeCapture.RunO002Escort"), "invoke_escort");
            Require(invoke.Contains("OperationsMobileReadyPlayModeCapture.RunResultDeltas"), "invoke_result");
            Require(invoke.Contains("WarlineCapture-Operations"), "shadow_path");
        }

        public static void RunLandedPresentationGuards()
        {
            DeadlinesAndPartialsPreserved();
            ScanRepairHoldPacingCompressed();
            HoldRefreshFreezesWithoutReissue();
            LocalizedObjectiveChromeNotRawIds();
            PresentationDrawsWorldAndHidesDebug();
            VictorySellsOutcomeWithoutBotChrome();
            CaptureHarnessStillWired();
            OwnershipStaysOperations();
            ContentFramesBoundIntoLandedShell();
            MobileReadyEvidenceCaptureWired();
            EscortChromeNeverExceedsRequired();
            ProtectChecklistLatchesWithOutcome();
        }

        public static void DeadlinesAndPartialsPreserved()
        {
            Require(OperationsAuthoredMissions.TryCompile("operation.o001", out OperationsCompiledTactical o001, out _, out string e1), e1);
            Require(o001.DeadlineTicks == 720 && o001.PartialProgressMinimum == 2);
            Require(OperationsAuthoredMissions.TryCompile("operation.o002", out OperationsCompiledTactical o002, out _, out string e2), e2);
            Require(o002.DeadlineTicks == 840 && o002.PartialProgressMinimum == 1);
            Require(OperationsAuthoredMissions.TryCompile("operation.o003", out OperationsCompiledTactical o003, out _, out string e3), e3);
            Require(o003.DeadlineTicks == 900 && o003.PartialMinimumComplete == 1);
            Require(CatalogDeadline("operation.o001") == 720);
            Require(CatalogDeadline("operation.o002") == 840);
            Require(CatalogDeadline("operation.o003") == 900);
        }

        public static void ScanRepairHoldPacingCompressed()
        {
            Require(OperationsTacticalRules.ScanSeconds <= 8, "scan_pacing");
            Require(OperationsTacticalRules.RepairSeconds <= 20, "repair_pacing");
            Require(OperationsTacticalRules.HoldRefreshSeconds <= 8, "hold_refresh");
            Require(OperationsAuthoredMissions.TryCompile("operation.o002", out OperationsCompiledTactical o002, out _, out _));
            Require(FindNode(o002, "hold_clinic").DurationTicks <= 15, "o002_hold");
            Require(OperationsAuthoredMissions.TryCompile("operation.o003", out OperationsCompiledTactical o003, out _, out _));
            Require(FindNode(o003, "hold_service_court").DurationTicks <= 24, "o003_hold");
        }

        public static void HoldRefreshFreezesWithoutReissue()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.CivicCenter;
            var authoring = new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.hold_refresh",
                MapId = map.MapId,
                Nodes = new[]
                {
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "hold_plaza",
                        Rule = OperationsObjectiveRuleKind.Hold,
                        ZoneAnchorId = Anchor(map, "site.plaza"),
                        DurationTicks = 12,
                        RadiusMeters = (int)OperationsTacticalRules.HoldMeters
                    }
                },
                Spawns = new[]
                {
                    new OperationsTacticalSpawnAuthoring
                    {
                        ObjectId = "unit.d02.rifle.01",
                        RoleId = "role.friendly.rifle",
                        RosterRole = OperationsRosterRoleKind.RifleInfantry,
                        Faction = OperationsTacticalFaction.Player,
                        Body = OperationsTacticalBodyKind.Infantry,
                        AnchorId = Anchor(map, "site.plaza"),
                        Health = 100
                    }
                }
            };
            OperationsTacticalCompileResult compile = OperationsTacticalCompiler.Compile(authoring);
            Require(compile.Accepted, compile.Error);
            var session = new OperationsTacticalSession(compile.Definition, OperationsP0Checks.CreateLaunch());
            Require(session.IssueHold("unit.d02.rifle.01", Anchor(map, "site.plaza")).Accepted);
            session.Advance(OperationsTacticalRules.HoldRefreshSeconds);
            int progress = Node(session, "hold_plaza").ProgressTicks;
            Require(progress > 0);
            session.Advance(OperationsTacticalRules.HoldRefreshSeconds + 2);
            Require(Node(session, "hold_plaza").ProgressTicks == progress, "stale_hold_froze");
            Require(session.IssueHold("unit.d02.rifle.01", Anchor(map, "site.plaza")).Accepted);
            session.Advance(1);
            Require(Node(session, "hold_plaza").ProgressTicks == progress + 1, "refresh_resumes");
        }

        public static void LocalizedObjectiveChromeNotRawIds()
        {
            Require(OperationsLocalizedCopy.TryGet("operations.objective.scan_signals", "en", out string en) &&
                    en.IndexOf("scan_signals", StringComparison.Ordinal) < 0);
            Require(OperationsLocalizedCopy.TryGet("operations.objective.scan_signals", "fa", out string fa) && fa.Length > 0);
            OperationsLoopSession loop = ReachActive("operation.o001", 2210);
            Require(loop.TryReadHud(out OperationsHudFrame hud));
            Require(hud.Required.Length > 0);
            string presentation = ReadPresentationSource();
            Require(presentation.Contains("BuildLocalizedObjectives"), "localized_builder");
            Require(presentation.Contains("operations.objective."), "objective_keys");
            Require(!presentation.Contains("seed=\" + Seed") && !presentation.Contains("seed=\").Append"), "no_seed_concat");
        }

        public static void PresentationDrawsWorldAndHidesDebug()
        {
            string presentation = ReadPresentationSource();
            string world = ReadCaptureSource("OperationsTacticalWorldShell.cs");
            Require(world.Contains("OperationsTacticalWorldShell"), "world_shell");
            Require(world.Contains("using Game.Operations.Contracts;"), "contracts_using");
            Require(world.Contains("CreatePrimitive"), "primitives");
            Require(world.Contains("Selection"), "selection");
            Require(world.Contains("Universal Render Pipeline/Unlit"), "urp_unlit");
            Require(world.Contains("_BaseColor"), "base_color");
            Require(world.Contains("UniversalAdditionalCameraData"), "urp_camera");
            Require(!world.Contains("Sprites/Default"), "no_builtin_sprite_shader");
            Require(presentation.Contains("OperationsTacticalWorldShell"), "presenter_uses_world");
            Require(presentation.Contains("PhonePanelRect"), "phone_mock");
            Require(presentation.Contains("_world?.Sync"), "active_syncs_world");
            int active = presentation.IndexOf("void DrawActiveChrome", StringComparison.Ordinal);
            int fat = presentation.IndexOf("void DrawFatThumbBar", StringComparison.Ordinal);
            Require(active >= 0 && fat > active, "active_chrome");
            string activeBody = presentation.Substring(active, fat - active);
            Require(!activeBody.Contains("new Rect(0, 0, Screen.width, Screen.height)"), "active_not_void");
            Require(presentation.Contains("ShowDebugChrome = false") || presentation.Contains("ShowDebugChrome=false"), "debug_off");
            Require(!presentation.Contains("Ops-owned win screen (Watch shared-UI seam not opened)"), "no_dev_footer");
        }

        public static void VictorySellsOutcomeWithoutBotChrome()
        {
            string presentation = ReadPresentationSource();
            Require(presentation.Contains("DrawVictoryCard"), "victory_card");
            Require(presentation.Contains("reward_credits"), "credits");
            Require(presentation.Contains("result.victory"), "localized_victory");
            Require(presentation.Contains("operations.result.continue"), "continue_on_result");
            Require(!presentation.Contains("intents="), "no_intent_chrome");
            Require(!presentation.Contains("result_hash="), "no_hash_chrome");
            int victory = presentation.IndexOf("public void ShowVictory", StringComparison.Ordinal);
            int log = presentation.IndexOf("void LogVictoryDebug", StringComparison.Ordinal);
            Require(victory >= 0 && log > victory, "show_victory");
            string victoryBody = presentation.Substring(victory, log - victory);
            Require(victoryBody.Contains("ShowMissionResult"), "result_card_path");
            Require(!victoryBody.Contains("reward_credits"), "no_credits_only_panel");
            Require(!victoryBody.Contains("operations.hud.in_progress"), "victory_not_in_progress");
        }

        public static void CaptureHarnessStillWired()
        {
            string root = FindRepoRoot();
            Require(File.Exists(Path.Combine(root, "Tools", "Operations", "check_aria_playmode_capture.py")));
            Require(File.Exists(Path.Combine(root, "Tools", "Operations", "Invoke-OperationsAriaPlayModeCapture.ps1")));
            Require(File.Exists(Path.Combine(root, "Assets", "Game", "Scripts", "Operations", "Capture", "OperationsTacticalWorldShell.cs")));
        }

        public static void OwnershipStaysOperations()
        {
            string presentation = ReadPresentationSource();
            string world = ReadCaptureSource("OperationsTacticalWorldShell.cs");
            Require(!presentation.Contains("MatchSceneView") && !world.Contains("MatchSceneView"));
            Require(!presentation.Contains("AriaPlayCapability") && !world.Contains("AriaPlayCapability"));
            Require(!presentation.Contains("SaveDataModel") && !world.Contains("SkirmishExpansion"));
            Require(presentation.Contains("namespace Game.Operations.Capture"));
            Require(world.Contains("namespace Game.Operations.Capture"));
        }

        static OperationsCompiledNode FindNode(OperationsCompiledTactical compiled, string nodeId)
        {
            for (int index = 0; index < compiled.Nodes.Length; index++)
            {
                if (compiled.Nodes[index].NodeId == nodeId)
                    return compiled.Nodes[index];
            }

            throw new InvalidOperationException("missing_node:" + nodeId);
        }

        static int CatalogDeadline(string missionId)
        {
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                if (OperationsCatalogIndex.Entries[index].MissionId == missionId)
                    return OperationsCatalogIndex.Entries[index].HardDeadlineSeconds;
            }

            throw new InvalidOperationException("missing_catalog:" + missionId);
        }

        static OperationsTacticalNodeState Node(OperationsTacticalSession session, string nodeId)
        {
            Require(session.TryGetNode(nodeId, out OperationsTacticalNodeState state));
            return state;
        }

        static string Anchor(OperationsMapGreybox map, string alias)
        {
            Require(map.TryGetByAlias(alias, out OperationsGreyboxAnchor anchor));
            return anchor.AnchorId;
        }

        static OperationsLoopSession ReachActive(string missionId, int seed)
        {
            OperationsLoopSession loop = OperationsLoopSession.Create(seed, new byte[] { 9, 9, 9 }, new byte[] { 8, 8 });
            Require(loop.TryOffer(missionId, out OperationsOfferSaveData offer));
            int district = 1;
            for (int number = 1; number <= 6; number++)
            {
                if (OperationsIdentityRules.DistrictId(number) == offer.districtId)
                    district = number;
            }

            Require(loop.OpenDistrict(district).Accepted);
            Require(loop.OpenBriefing(offer.offerId).Accepted);
            string deployId = "cmd.operations." + seed.ToString("x8");
            Require(loop.BeginDeploy(deployId).Accepted && loop.CompleteAttempt(deployId).Accepted);
            Require(loop.BeginLaunch().Accepted && loop.CompleteLaunch().Accepted);
            Require(loop.BeginActive(true, true, true, loop.ContentHash).Accepted && loop.CompleteActive().Accepted);
            return loop;
        }

        static string ReadPresentationSource() => ReadCaptureSource("OperationsAriaPlayModePresentation.cs");

        static string ReadCaptureSource(string fileName)
        {
            return File.ReadAllText(Path.Combine(
                FindRepoRoot(),
                "Assets",
                "Game",
                "Scripts",
                "Operations",
                "Capture",
                fileName));
        }

        static string FindRepoRoot()
        {
            string directory = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(directory))
            {
                if (File.Exists(Path.Combine(directory, "AGENTS.md")) &&
                    Directory.Exists(Path.Combine(directory, "Assets", "Game", "Scripts", "Operations")))
                    return directory;
                directory = Directory.GetParent(directory)?.FullName;
            }

            throw new InvalidOperationException("repo_root");
        }

        static void Require(bool condition, string message = "require")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
