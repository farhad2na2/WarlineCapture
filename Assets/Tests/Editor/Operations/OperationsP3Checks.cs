using System;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Strategic;
using Game.Operations.Tactical;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsP3Checks
    {
        public const int ExpectedCheckCount = 12;
        public const string PassMarker = "[OperationsP3Validation] result=Passed checks=12";

        public static void RunAll()
        {
            DashboardDeployRealMissionReturnsCorrectDistrict();
            SecondDistrictResultStaysOnLaunchedDistrict();
            ModeLaunchIsExclusive();
            ReserveRefundAndDuplicateDeploy();
            AppInterruptionAtEveryCommitBoundary();
            CheckpointRestoresProgressWithoutDuplicateCargo();
            CorruptCheckpointRefundsWithoutTimerReset();
            MissingCheckpointOffersRestartOrWithdraw();
            DuplicateConflictAndStaleSession();
            PracticeGrantsNothingAndWithdrawDoesNotRefund();
            RootHeaderHistoryAndReportDoNotSimulate();
            FrozenResultRejectsWithdrawRewrite();
        }

        public static void DashboardDeployRealMissionReturnsCorrectDistrict()
        {
            byte[] campaign = { 9, 9, 9 };
            byte[] quick = { 8, 8 };
            OperationsLoopSession loop = OperationsLoopSession.Create(1301, campaign, quick);
            OpenBriefing(loop, "operation.o001");
            Require(loop.TryReadBriefing(out OperationsBriefingFrame briefing));
            Require(briefing.MissionId == "operation.o001");
            Require(briefing.DistrictId == "district.operations.d01");
            Require(briefing.MapId == OperationsMapGreyboxCatalog.OldQuarterMapId);
            Require(briefing.LiveApCost == 1 && briefing.PracticeApCost == 0);
            Require(briefing.Family == OperationsMissionFamilyKind.Recon);
            Require(Contains(briefing.Approaches, "route.main") && Contains(briefing.Approaches, "route.safe"));
            Require(briefing.NameKey.IndexOf("operations.mission.", StringComparison.Ordinal) == 0);
            Require(briefing.BriefKey.Length > 0 && briefing.DebriefKey.Length > 0);

            string deployId = Id();
            OperationsCommandResult deploy = loop.BeginDeploy(deployId);
            Require(deploy.Accepted, deploy.ReasonCode.ToString());
            Require(loop.CompleteAttempt(deployId).Accepted);
            Require(loop.ActionPoints == 2);
            Require(loop.BeginLaunch().Accepted);
            Require(loop.CompleteLaunch().Accepted);
            Require(loop.TryLaunchRequest(out OperationsModeLaunchRequest request));
            Require(request.Mode == OperationsMatchMode.Operations);
            Require(request.Exclusive && !request.InvokesSharedSceneView);
            Require(request.ReturnRoute == "Operations");
            Require(request.DispatchOwner == "Game.Operations.Loop");
            Require(loop.BeginActive(true, true, true, loop.ContentHash).Accepted, "active");
            Require(loop.CompleteActive().Accepted);

            Require(loop.TryReadHud(out OperationsHudFrame hud));
            Require(!hud.TimerVisible);
            Require(hud.WithdrawAvailable && hud.PauseAvailable);
            Require(hud.CameraFocus == "site.d01.clinic");
            Require(hud.CivilianDeaths == 0 && hud.ProtectedSites >= 1);
            Require(ContainsObjective(hud.Required, "scan_clinic") && !ContainsObjective(hud.Required, "hold_courtyard"));
            Require(ContainsObjective(hud.Optional, "hold_courtyard"));
            Require(hud.EscortTarget.Length == 0);

            PlayOldQuarter(loop);
            Require(loop.MissionOutcome == OperationsOutcomeKind.Victory, "outcome=" + loop.MissionOutcome);
            Require(loop.BeginResult().Accepted);
            Require(loop.CompleteResult().Accepted);
            Require(loop.Credits == 0 && loop.CommanderXp == 0);
            AssertDistrict(loop, 1, 40, 45, 45, 50, 20, 20, 45);
            Require(loop.TryReadResult(out OperationsResultFrame pending));
            Require(!pending.RewardsApplied && pending.ReceivedCredits == 0);
            Require(pending.Outcome == OperationsOutcomeKind.Victory);
            Require(pending.TerminalReason == "mandatory_complete");
            Require(pending.CampaignStarsAwarded == 0);

            string settleId = Id();
            Require(loop.BeginSettlement(settleId).Accepted);
            Require(loop.CompleteSettlement(settleId).Accepted);
            Require(loop.TryReadResult(out OperationsResultFrame settled));
            Require(settled.RewardsApplied && settled.ReceivedCredits == 120 && settled.ReceivedXp == 50);
            Require(Same(settled.Before, new[] { 40, 45, 45, 50, 20, 20, 45 }));
            Require(Same(settled.After, new[] { 40, 46, 45, 48, 38, 22, 45 }));
            Require(settled.DistrictId == "district.operations.d01");
            Require(settled.CivilianDeaths == 0 && settled.TaskForceLosses == 0);
            Require(ContainsFact(settled.Achieved, "extract_force"));
            Require(!ContainsFact(settled.Achieved, "hold_courtyard"));
            Require(loop.TryCommittedResult(out OperationsMissionResult result));
            Require(Contains(result.ExtractedEvidenceIds, "site.d01.evidence"));
            int credits = loop.Credits;
            Require(loop.TryReadResult(out _));
            Require(loop.Credits == credits);

            string returnId = Id();
            Require(returnId.Length > 0);
            Require(loop.BeginReturn().Accepted);
            Require(loop.CompleteReturn().Accepted);
            Require(loop.Phase == OperationsLoopPhase.Dashboard);
            Require(loop.ActionPoints == 2);
            Require(loop.Credits == 120 && loop.CommanderXp == 50);
            Require(loop.MissionVictory("operation.o001"));
            Require(!loop.MissionVictory("operation.o011"));
            AssertDistrict(loop, 1, 40, 46, 45, 48, 38, 22, 45);
            AssertDistrict(loop, 2, 35, 40, 40, 55, 20, 25, 40);
            OperationsShellFrame shell = loop.ReadShell();
            Require(shell.Top == "Operations" && shell.History.Length == 1 && shell.RootEnabled);
            Require(shell.RootRoute == "Operations");
            Require(Same(loop.CampaignEnvelope, campaign) && Same(loop.QuickGameEnvelope, quick));
            Require(loop.CommittedJson.IndexOf("starsEarned", StringComparison.Ordinal) < 0);
            Require(loop.CommittedJson.IndexOf("Demo2", StringComparison.Ordinal) < 0);
        }

        public static void SecondDistrictResultStaysOnLaunchedDistrict()
        {
            OperationsLoopSession loop = NewLoop(1302);
            ReachActive(loop, "operation.o011");
            Require(loop.TryReadHud(out OperationsHudFrame hud));
            Require(hud.TimerVisible && hud.TimerRemaining == 900);
            Require(hud.EscortTarget == "unit.d02.cargo.01");
            PlayCivic(loop);
            Require(loop.MissionOutcome == OperationsOutcomeKind.Victory, "civic " + loop.MissionOutcome);
            Finish(loop);
            AssertDistrict(loop, 2, 35, 41, 40, 53, 38, 27, 40);
            AssertDistrict(loop, 1, 40, 45, 45, 50, 20, 20, 45);
            Require(loop.MissionVictory("operation.o011"));
            Require(loop.Credits == 120 && loop.ActionPoints == 2);
            Require(loop.TryReadResult(out OperationsResultFrame result));
            Require(result.DistrictId == "district.operations.d02");
            Require(result.CampaignStarsAwarded == 0);
        }

        public static void ModeLaunchIsExclusive()
        {
            OperationsLoopSession loop = NewLoop(1303);
            Require(loop.TryOccupy(OperationsMatchMode.Campaign).Accepted);
            OpenBriefing(loop, "operation.o001");
            string deployId = Id();
            Require(loop.BeginDeploy(deployId).Accepted);
            Require(loop.CompleteAttempt(deployId).Accepted);
            OperationsLoopStep blocked = loop.BeginLaunch();
            Require(!blocked.Accepted && blocked.Reason == "mode_occupied");
            Require(loop.Slot == OperationsMatchMode.Campaign);
            Require(loop.Phase == OperationsLoopPhase.Reserved);
            Require(loop.ActionPoints == 2);
            Require(loop.ReleaseForeignMode().Accepted);
            Require(loop.BeginLaunch().Accepted);
            Require(loop.CompleteLaunch().Accepted);
            Require(loop.TryLaunchRequest(out OperationsModeLaunchRequest request));
            Require(request.Mode == OperationsMatchMode.Operations && request.Exclusive);
            Require(!request.InvokesSharedSceneView);
            Require(loop.BeginActive(true, true, true, loop.ContentHash).Accepted);
            Require(loop.CompleteActive().Accepted);
            OperationsLoopStep skirmish = loop.TryOccupy(OperationsMatchMode.Skirmish);
            Require(!skirmish.Accepted && skirmish.Reason == "exclusive");
            Require(loop.CommittedJson.IndexOf("starsEarned", StringComparison.Ordinal) < 0);
            Require(loop.CommittedJson.IndexOf("MatchSceneView", StringComparison.Ordinal) < 0);
        }

        public static void ReserveRefundAndDuplicateDeploy()
        {
            OperationsLoopSession loop = NewLoop(1304);
            OpenBriefing(loop, "operation.o001");
            string deployId = Id();
            OperationsCommandResult first = loop.BeginDeploy(deployId);
            Require(first.Accepted);
            Require(loop.CompleteAttempt(deployId).Accepted);
            Require(loop.ActionPoints == 2);
            OperationsCommandResult repeat = loop.BeginDeploy(deployId);
            Require(repeat.Accepted && repeat.TransactionId == first.TransactionId && repeat.NewRevision == first.NewRevision);
            Require(!loop.HasPending && loop.ActionPoints == 2);
            string secondId = Id();
            OperationsCommandResult conflict = loop.BeginDeploy(secondId);
            Require(!conflict.Accepted && conflict.ReasonCode == OperationsReasonCode.AttemptConflict);
            OperationsCommandResult closed = loop.CompleteAttempt(secondId);
            Require(!closed.Accepted && closed.ReasonCode == OperationsReasonCode.AttemptConflict);
            Require(!loop.HasPending);
            Require(loop.ActionPoints == 2 && loop.Phase == OperationsLoopPhase.Reserved);

            Require(loop.BeginLaunch().Accepted);
            Require(loop.CompleteLaunch().Accepted);
            OperationsLoopStep unreadiness = loop.BeginActive(false, true, true, loop.ContentHash);
            Require(!unreadiness.Accepted && unreadiness.Reason == "readiness_map");
            string refundId = Id();
            Require(loop.BeginRefund(refundId).Accepted);
            Require(loop.CompleteRefund(refundId).Accepted);
            Require(loop.ActionPoints == 3 && loop.Phase == OperationsLoopPhase.Dashboard);
            AssertDistrict(loop, 1, 40, 45, 45, 50, 20, 20, 45);
            Require(loop.TryOffer("operation.o001", out OperationsOfferSaveData offer) && offer.deployable);
            OperationsCommandResult again = loop.BeginRefund(Id());
            Require(!again.Accepted);
            Require(loop.ActionPoints == 3);

            OpenBriefing(loop, "operation.o001");
            string redeploy = Id();
            Require(loop.BeginDeploy(redeploy).Accepted);
            Require(loop.CompleteAttempt(redeploy).Accepted);
            Require(loop.BeginLaunch().Accepted);
            Require(loop.CompleteLaunch().Accepted);
            OperationsLoopStep hash = loop.BeginActive(true, true, true, "not-the-hash");
            Require(!hash.Accepted && hash.Reason == "hash_mismatch");
            string secondRefund = Id();
            Require(loop.BeginRefund(secondRefund).Accepted);
            Require(loop.CompleteRefund(secondRefund).Accepted);
            Require(loop.ActionPoints == 3);
            Require(!loop.BeginRefund(Id()).Accepted);
        }

        public static void AppInterruptionAtEveryCommitBoundary()
        {
            OperationsLoopSession reserve = NewLoop(1310);
            OpenBriefing(reserve, "operation.o001");
            string reserveId = Id();
            Require(reserve.BeginDeploy(reserveId).Accepted);
            reserve.Interrupt();
            Require(reserve.ActionPoints == 3 && !reserve.TryReserved(out _));
            OpenBriefing(reserve, "operation.o001");
            Require(reserve.BeginDeploy(reserveId).Accepted);
            Require(reserve.CompleteAttempt(reserveId).Accepted);
            Require(reserve.ActionPoints == 2);

            Require(reserve.BeginLaunch().Accepted);
            reserve.Interrupt();
            Require(reserve.Phase == OperationsLoopPhase.Reserved);
            Require(reserve.Slot == OperationsMatchMode.None);
            Require(!reserve.TryLaunchRequest(out _));
            Require(reserve.BeginLaunch().Accepted);
            Require(reserve.CompleteLaunch().Accepted);

            Require(reserve.BeginActive(true, true, true, reserve.ContentHash).Accepted);
            reserve.Interrupt();
            Require(reserve.Phase == OperationsLoopPhase.LaunchDispatched && !reserve.HasMission);
            Require(reserve.ActionPoints == 2);
            Require(reserve.BeginActive(true, true, true, reserve.ContentHash).Accepted);
            Require(reserve.CompleteActive().Accepted);
            reserve.Advance(1);
            Require(reserve.BeginCheckpoint().Accepted);
            Require(reserve.HasStagedCheckpoint && reserve.PublishedCheckpointId.Length == 0);
            reserve.Interrupt();
            Require(!reserve.HasStagedCheckpoint && reserve.PublishedCheckpointId.Length == 0 && !reserve.HasMission);

            OperationsLoopSession publish = Reach("operation.o001", 1311);
            publish.Advance(1);
            Require(publish.BeginCheckpoint().Accepted);
            Require(publish.BeginCheckpointPublish().Accepted);
            publish.Interrupt();
            Require(publish.PublishedCheckpointId.Length == 0 && !publish.HasMission);

            OperationsLoopSession result = Reach("operation.o001", 1312);
            PlayOldQuarter(result);
            Require(result.BeginResult().Accepted);
            result.Interrupt();
            Require(result.ResultHash.Length == 0 && result.Credits == 0);
            AssertDistrict(result, 1, 40, 45, 45, 50, 20, 20, 45);
            Require(result.Phase == OperationsLoopPhase.Active);

            OperationsLoopSession settled = Reach("operation.o001", 1313);
            PlayOldQuarter(settled);
            Require(settled.BeginResult().Accepted);
            Require(settled.CompleteResult().Accepted);
            string settleId = Id();
            Require(settled.BeginSettlement(settleId).Accepted);
            settled.Interrupt();
            Require(settled.Credits == 0);
            AssertDistrict(settled, 1, 40, 45, 45, 50, 20, 20, 45);
            Require(settled.ResultHash.Length > 0);
            Require(settled.BeginSettlement(settleId).Accepted);
            Require(settled.CompleteSettlement(settleId).Accepted);
            Require(settled.Credits == 120);
            AssertDistrict(settled, 1, 40, 46, 45, 48, 38, 22, 45);
            Require(settled.BeginReturn().Accepted);
            settled.Interrupt();
            Require(settled.Credits == 120);
            Require(settled.ReadShell().Top == "MissionResult");
            Require(settled.BeginReturn().Accepted);
            Require(settled.CompleteReturn().Accepted);
            Require(settled.Credits == 120 && settled.ReadShell().Top == "Operations");

            OperationsLoopSession refund = NewLoop(1314);
            OpenBriefing(refund, "operation.o001");
            string refundDeploy = Id();
            Require(refund.BeginDeploy(refundDeploy).Accepted);
            Require(refund.CompleteAttempt(refundDeploy).Accepted);
            Require(refund.BeginLaunch().Accepted);
            Require(refund.CompleteLaunch().Accepted);
            Require(!refund.BeginActive(true, false, true, refund.ContentHash).Accepted);
            string refundId = Id();
            Require(refund.BeginRefund(refundId).Accepted);
            refund.Interrupt();
            Require(refund.ActionPoints == 2);
            Require(refund.BeginRefund(refundId).Accepted);
            Require(refund.CompleteRefund(refundId).Accepted);
            Require(refund.ActionPoints == 3 && refund.Phase == OperationsLoopPhase.Dashboard);
        }

        public static void CheckpointRestoresProgressWithoutDuplicateCargo()
        {
            OperationsLoopSession civic = Reach("operation.o011", 1320);
            Require(civic.TryActor("unit.d02.cargo.01", out OperationsTacticalActorState origin));
            Require(civic.Repair("unit.d02.repair.01", "site.d02.clinic").Accepted);
            Require(civic.EscortGo("route.main").Accepted);
            civic.Advance(4);
            Require(civic.TryNode("repair_clinic", out OperationsTacticalNodeState repair));
            Require(civic.TryActor("unit.d02.cargo.01", out OperationsTacticalActorState moved));
            int materials = civic.MissionMaterials;
            Require(repair.ProgressTicks > 0, "repair=" + repair.ProgressTicks);
            Require(materials == 200, "materials=" + materials);
            Require(moved.X != origin.X || moved.Z != origin.Z);
            int tick = Tick(civic);
            Require(civic.BeginCheckpoint().Accepted);
            Require(civic.PublishCheckpoint().Accepted);
            string text = civic.PublishedCheckpointText();
            Require(text.IndexOf("unit.d02.cargo.01", StringComparison.Ordinal) >= 0);
            Require(text.IndexOf("Entity", StringComparison.Ordinal) < 0);
            Require(text.IndexOf("Demo2", StringComparison.Ordinal) < 0);
            civic.Advance(3);
            Require(civic.TryActor("unit.d02.cargo.01", out OperationsTacticalActorState later));
            Require(later.X != moved.X || later.Z != moved.Z);
            civic.Interrupt();
            Require(Tick(civic) == tick);
            Require(civic.TryNode("repair_clinic", out OperationsTacticalNodeState restored));
            Require(restored.ProgressTicks == repair.ProgressTicks);
            Require(civic.MissionMaterials == materials);
            Require(civic.TryActor("unit.d02.cargo.01", out OperationsTacticalActorState cargo));
            Require(cargo.X == moved.X && cargo.Z == moved.Z);
            civic.Advance(1);
            Require(civic.TryNode("repair_clinic", out OperationsTacticalNodeState next));
            Require(next.ProgressTicks == repair.ProgressTicks + 1);
            Require(civic.MissionMaterials == materials);

            OperationsLoopSession quarter = Reach("operation.o001", 1321);
            Require(quarter.Observe("unit.d01.rifle.01", "site.d01.clinic").Accepted);
            Require(quarter.Scan("unit.d01.rifle.01", "site.d01.clinic").Accepted);
            bool armed = false;
            for (int step = 0; step < 40 && !armed; step++)
            {
                quarter.Advance(1);
                armed = quarter.WaveArmed(1);
            }

            Require(armed && !quarter.WaveSpawned(1));
            int armedTick = Tick(quarter);
            Require(quarter.BeginCheckpoint().Accepted);
            Require(quarter.PublishCheckpoint().Accepted);
            quarter.Advance(10);
            quarter.Interrupt();
            Require(Tick(quarter) == armedTick);
            Require(quarter.WaveArmed(1) && !quarter.WaveSpawned(1));
            quarter.Advance(29);
            Require(!quarter.WaveSpawned(1));
            quarter.Advance(1);
            Require(quarter.WaveSpawned(1));
        }

        public static void CorruptCheckpointRefundsWithoutTimerReset()
        {
            OperationsLoopSession loop = Reach("operation.o001", 1330);
            loop.Advance(5);
            int ap = loop.ActionPoints;
            Require(loop.BeginCheckpoint().Accepted);
            Require(loop.PublishCheckpoint().Accepted);
            loop.CorruptPublishedCheckpoint();
            loop.Interrupt();
            Require(!loop.HasMission && loop.CheckpointCorrupt);
            Require(loop.Offers(OperationsRecoveryChoice.RefundTechnicalFailure));
            Require(!loop.Offers(OperationsRecoveryChoice.ResumeCheckpoint));
            Require(!loop.Offers(OperationsRecoveryChoice.RestartAttempt));
            string refundId = Id();
            Require(loop.BeginRefund(refundId).Accepted);
            Require(loop.CompleteRefund(refundId).Accepted);
            Require(loop.ActionPoints == ap + 1);
            Require(loop.Phase == OperationsLoopPhase.Dashboard);
            AssertDistrict(loop, 1, 40, 45, 45, 50, 20, 20, 45);
            Require(loop.Credits == 0);
            Require(!loop.BeginRefund(Id()).Accepted);
            Require(!loop.HasMission);
        }

        public static void MissingCheckpointOffersRestartOrWithdraw()
        {
            OperationsLoopSession loop = Reach("operation.o001", 1340);
            string sessionId = loop.SessionId;
            int ap = loop.ActionPoints;
            loop.Advance(4);
            loop.Interrupt();
            Require(!loop.HasMission);
            Require(loop.Offers(OperationsRecoveryChoice.RestartAttempt));
            Require(loop.Offers(OperationsRecoveryChoice.Withdraw));
            Require(!loop.Offers(OperationsRecoveryChoice.ResumeCheckpoint));
            Require(loop.RestartAttempt().Accepted);
            Require(loop.SessionId == sessionId);
            Require(loop.ActionPoints == ap);
            Require(loop.RestartCount == 1);
            Require(Tick(loop) == 0);
            AssertDistrict(loop, 1, 40, 45, 45, 50, 20, 20, 45);
        }

        public static void DuplicateConflictAndStaleSession()
        {
            OperationsLoopSession loop = Reach("operation.o001", 1350);
            PlayOldQuarter(loop);
            Finish(loop);
            Require(loop.TryCommittedResult(out OperationsMissionResult original));
            Require(loop.Credits == 120);
            OperationsCommandResult duplicate = loop.ProbeSettlement(Id(), original);
            Require(duplicate.Accepted, duplicate.ReasonCode.ToString());
            Require(loop.Credits == 120);
            AssertDistrict(loop, 1, 40, 46, 45, 48, 38, 22, 45);

            var conflict = new OperationsMissionResult(
                original.SchemaVersion,
                original.RunId,
                original.OfferId,
                original.MissionId,
                original.SessionId,
                original.AttemptOrdinal,
                original.DefinitionVersion,
                original.Outcome,
                original.TerminalReason,
                original.ElapsedTicks,
                original.MandatoryObjectiveFacts,
                original.OptionalObjectiveFacts,
                original.CivilianDeaths,
                original.ProtectedSiteFacts,
                original.DeliveredCargoFacts,
                original.ExtractedEvidenceIds,
                original.TaskForceLosses,
                original.InitialTaskForceCount,
                "rhconflict0001");
            OperationsCommandResult rejected = loop.ProbeSettlement(Id(), conflict);
            Require(!rejected.Accepted && rejected.ReasonCode == OperationsReasonCode.Conflict);
            Require(loop.Credits == 120);
            AssertDistrict(loop, 1, 40, 46, 45, 48, 38, 22, 45);

            OpenBriefing(loop, "operation.o001");
            string redeploy = Id();
            Require(loop.BeginDeploy(redeploy).Accepted);
            Require(loop.CompleteAttempt(redeploy).Accepted);
            Require(loop.ActionPoints == 1);
            OperationsCommandResult stale = loop.ProbeSettlement(Id(), original);
            Require(!stale.Accepted && stale.ReasonCode == OperationsReasonCode.Conflict);
            Require(loop.Credits == 120 && loop.ActionPoints == 1);
            AssertDistrict(loop, 1, 40, 46, 45, 48, 38, 22, 45);
            Require(loop.TryReserved(out OperationsAttemptSaveData reserved));
            Require(reserved.sessionId != original.SessionId);
        }

        public static void PracticeGrantsNothingAndWithdrawDoesNotRefund()
        {
            OperationsLoopSession withdrawn = Reach("operation.o001", 1360);
            withdrawn.Advance(1);
            Require(withdrawn.WithdrawMission().Accepted);
            Require(withdrawn.MissionOutcome == OperationsOutcomeKind.Withdrawn);
            Finish(withdrawn);
            Require(withdrawn.ActionPoints == 2 && withdrawn.Credits == 0 && withdrawn.CommanderXp == 0);
            Require(!withdrawn.MissionVictory("operation.o001"));
            AssertDistrict(withdrawn, 1, 38, 45, 45, 53, 20, 20, 45);

            OperationsLoopSession practice = NewLoop(1361);
            OpenBriefing(practice, "operation.o001");
            string deployId = Id();
            Require(practice.BeginDeploy(deployId).Accepted);
            Require(practice.CompleteAttempt(deployId).Accepted);
            string refundId = Id();
            Require(practice.BeginLaunch().Accepted);
            Require(practice.CompleteLaunch().Accepted);
            Require(!practice.BeginActive(false, false, false, practice.ContentHash).Accepted);
            Require(practice.BeginRefund(refundId).Accepted);
            Require(practice.CompleteRefund(refundId).Accepted);
            Require(practice.ActionPoints == 3);
            OpenBriefing(practice, "operation.o001");
            string practiceId = Id();
            Require(practice.BeginPractice(practiceId).Accepted);
            Require(practice.CompleteAttempt(practiceId).Accepted);
            Require(practice.ActionPoints == 3);
            Require(practice.BeginLaunch().Accepted);
            Require(practice.CompleteLaunch().Accepted);
            Require(practice.BeginActive(true, true, true, practice.ContentHash).Accepted);
            Require(practice.CompleteActive().Accepted);
            PlayOldQuarter(practice);
            Finish(practice);
            Require(practice.ActionPoints == 3 && practice.Credits == 0 && practice.CommanderXp == 0);
            Require(!practice.MissionVictory("operation.o001"));
            AssertDistrict(practice, 1, 40, 45, 45, 50, 20, 20, 45);
        }

        public static void RootHeaderHistoryAndReportDoNotSimulate()
        {
            OperationsLoopSession loop = NewLoop(1370);
            Require(loop.Day == 1 && loop.ReadShell().Top == "Operations" && !loop.ReadShell().BackEnabled);
            Require(loop.OpenReport().Accepted);
            Require(loop.ReadShell().Top == "EndOfDayReport");
            OperationsCommandResult end = loop.RequestEndDay(Id());
            Require(!end.Accepted && end.ReasonCode == OperationsReasonCode.PreconditionFailed);
            Require(loop.Day == 1 && loop.ActionPoints == 3);
            Require(loop.ContinueReport().Accepted);
            Require(loop.Day == 1 && loop.ReadShell().Top == "Operations");

            OpenBriefing(loop, "operation.o001");
            Require(loop.ReadShell().Top == "MissionBriefing");
            Require(loop.Back().Accepted && loop.ReadShell().Top == "DistrictDetail");
            Require(loop.Back().Accepted && loop.ReadShell().Top == "Operations");
            Require(loop.ActionPoints == 3 && loop.Day == 1);

            ReachActive(loop, "operation.o001");
            OperationsShellFrame match = loop.ReadShell();
            Require(match.Top == "Match" && !match.RootEnabled && match.RootBlockReason == "active_attempt");
            Require(loop.Back().Reason == "navigation_pause");
            Require(loop.ReadShell().Top == "Match");
            Require(loop.TryReadHud(out OperationsHudFrame hud) && hud.Paused);
            Require(loop.Phase == OperationsLoopPhase.Active && loop.ActionPoints == 2 && loop.Day == 1);
            Require(!loop.RequestEndDay(Id()).Accepted);
            Require(!loop.ActivateRoot().Accepted);
        }

        public static void FrozenResultRejectsWithdrawRewrite()
        {
            OperationsLoopSession loop = Reach("operation.o001", 1380);
            PlayOldQuarter(loop);
            Require(loop.BeginResult().Accepted);
            Require(loop.CompleteResult().Accepted);
            OperationsCommandResult early = loop.Withdraw(Id());
            Require(!early.Accepted && early.ReasonCode == OperationsReasonCode.PreconditionFailed);
            AssertDistrict(loop, 1, 40, 45, 45, 50, 20, 20, 45);
            Require(loop.Credits == 0);
            string settleId = Id();
            Require(loop.BeginSettlement(settleId).Accepted);
            Require(loop.CompleteSettlement(settleId).Accepted);
            OperationsCommandResult late = loop.Withdraw(Id());
            Require(!late.Accepted);
            AssertDistrict(loop, 1, 40, 46, 45, 48, 38, 22, 45);
            Require(loop.Credits == 120 && loop.CommanderXp == 50);
            Require(loop.TryCommittedResult(out OperationsMissionResult result));
            Require(result.Outcome == OperationsOutcomeKind.Victory);
        }

        static int _nextId = 0xA100;

        static string Id() => "cmd.operations." + (_nextId++).ToString("x8");

        static OperationsLoopSession NewLoop(int seed) =>
            OperationsLoopSession.Create(seed, new byte[] { 9, 9, 9 }, new byte[] { 8, 8 });

        static void OpenBriefing(OperationsLoopSession loop, string missionId)
        {
            Require(loop.Phase == OperationsLoopPhase.Dashboard, "phase=" + loop.Phase);
            Require(loop.TryOffer(missionId, out OperationsOfferSaveData offer), missionId);
            int number = DistrictNumber(offer.districtId);
            Require(loop.OpenDistrict(number).Accepted, "district");
            Require(loop.OpenBriefing(offer.offerId).Accepted, "briefing");
        }

        static void ReachActive(OperationsLoopSession loop, string missionId)
        {
            OpenBriefing(loop, missionId);
            string deployId = Id();
            OperationsCommandResult deploy = loop.BeginDeploy(deployId);
            Require(deploy.Accepted, deploy.ReasonCode.ToString());
            Require(loop.CompleteAttempt(deployId).Accepted);
            Require(loop.BeginLaunch().Accepted, "launch");
            Require(loop.CompleteLaunch().Accepted);
            OperationsLoopStep active = loop.BeginActive(true, true, true, loop.ContentHash);
            Require(active.Accepted, active.Reason);
            Require(loop.CompleteActive().Accepted);
        }

        static OperationsLoopSession Reach(string missionId, int seed)
        {
            OperationsLoopSession loop = NewLoop(seed);
            ReachActive(loop, missionId);
            return loop;
        }

        static void Finish(OperationsLoopSession loop)
        {
            Require(loop.BeginResult().Accepted, "result");
            Require(loop.CompleteResult().Accepted);
            string settleId = Id();
            OperationsCommandResult begun = loop.BeginSettlement(settleId);
            Require(begun.Accepted, begun.ReasonCode.ToString());
            OperationsCommandResult settled = loop.CompleteSettlement(settleId);
            Require(settled.Accepted, settled.ReasonCode.ToString());
            Require(loop.BeginReturn().Accepted);
            Require(loop.CompleteReturn().Accepted);
        }

        static void PlayOldQuarter(OperationsLoopSession loop)
        {
            Require(loop.Observe("unit.d01.rifle.01", "site.d01.clinic").Accepted, "observe");
            Require(loop.Scan("unit.d01.rifle.01", "site.d01.clinic").Accepted, "scan");
            Require(loop.Interact("unit.d01.rifle.02", "site.d01.evidence").Accepted, "interact");
            bool extractA = false;
            bool extractB = false;
            for (int step = 0; step < 50 && !loop.MissionTerminal; step++)
            {
                loop.Advance(1);
                if (!extractB && Completed(loop, "interact_evidence"))
                {
                    Require(loop.Extract("unit.d01.rifle.02").Accepted, "extract-b");
                    extractB = true;
                }

                if (!extractA && Completed(loop, "scan_clinic"))
                {
                    Require(loop.Extract("unit.d01.rifle.01").Accepted, "extract-a");
                    extractA = true;
                }
            }

            Require(loop.MissionTerminal && loop.MissionOutcome == OperationsOutcomeKind.Victory, "old-quarter " + loop.MissionOutcome + " tick=" + Tick(loop));
        }

        static void PlayCivic(OperationsLoopSession loop)
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.CivicCenter;
            Require(map.TryGetByAlias("site.plaza", out OperationsGreyboxAnchor plaza));
            Require(loop.Move("unit.d02.rifle.01", plaza.AnchorId).Accepted, "move");
            Require(loop.Repair("unit.d02.repair.01", "site.d02.clinic").Accepted, "repair");
            Require(loop.EscortGo("route.main").Accepted, "escort");
            bool holding = false;
            for (int step = 0; step < 90 && !loop.MissionTerminal; step++)
            {
                loop.Advance(1);
                if (!holding && loop.TryActor("unit.d02.rifle.01", out OperationsTacticalActorState rifle) &&
                    OperationsTacticalRules.Within(rifle.X, rifle.Z, plaza.X, plaza.Z, OperationsTacticalRules.HoldMeters))
                {
                    Require(loop.Hold("unit.d02.rifle.01", plaza.AnchorId).Accepted, "hold");
                    holding = true;
                }
            }

            Require(loop.MissionTerminal && loop.MissionOutcome == OperationsOutcomeKind.Victory, "civic " + loop.MissionOutcome + " tick=" + Tick(loop));
        }

        static bool Completed(OperationsLoopSession loop, string nodeId) =>
            loop.TryNode(nodeId, out OperationsTacticalNodeState state) && state.Phase == OperationsTacticalNodePhase.Complete;

        static int Tick(OperationsLoopSession loop)
        {
            Require(loop.TryMissionTick(out int tick));
            return tick;
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

        static void AssertDistrict(
            OperationsLoopSession loop,
            int number,
            int security,
            int trust,
            int infrastructure,
            int enemy,
            int intel,
            int heat,
            int supply)
        {
            OperationsDistrictComponent district = loop.District(number);
            string actual = district.Security + "," + district.Trust + "," + district.Infrastructure + "," +
                            district.EnemyInfluence + "," + district.IntelConfidence + "," + district.Heat + "," +
                            district.SupplyReadiness;
            string expected = security + "," + trust + "," + infrastructure + "," + enemy + "," + intel + "," + heat + "," + supply;
            Require(actual == expected, "D" + number.ToString("00") + " " + actual + " expected " + expected);
        }

        static bool Contains(string[] values, string value)
        {
            if (values == null)
                return false;
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index] == value)
                    return true;
            }

            return false;
        }

        static bool ContainsObjective(OperationsHudObjective[] values, string nodeId)
        {
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index].NodeId == nodeId)
                    return true;
            }

            return false;
        }

        static bool ContainsFact(OperationsObjectiveFact[] values, string nodeId)
        {
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index].NodeId == nodeId)
                    return true;
            }

            return false;
        }

        static bool Same(int[] left, int[] right)
        {
            if (left.Length != right.Length)
                return false;
            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                    return false;
            }

            return true;
        }

        static bool Same(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
                return false;
            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                    return false;
            }

            return true;
        }

        static void Require(bool condition, string message = "Operations P3 check failed.")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
