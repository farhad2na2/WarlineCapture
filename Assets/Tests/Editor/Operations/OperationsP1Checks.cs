using System;
using Game.Operations.Contracts;
using Game.Operations.Strategic;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsP1Checks
    {
        public const int ExpectedCheckCount = 18;
        public const string PassMarker = "[OperationsP1Validation] result=Passed checks=18";

        public static void RunAll()
        {
            DeterministicDayTraceMatchesRules();
            SignedClampAndOutcomeDeltas();
            SixActionsRespectLimitsAndAp();
            FloorRecoveryRemainsPossible();
            OfferDirectorStableAndSlotGated();
            IncidentsCapCooldownAndExpiry();
            AdjacencyPressureIsSimultaneous();
            DuplicateCommandDoesNotDoubleSpend();
            ConflictingSettlementIsRejected();
            CrashPreservesPriorRevision();
            StaleWriterAndUnknownSchemaRejected();
            TechnicalFailureRefundsOnceAndReservedResumes();
            RewardsStayInsideOperationsEnvelope();
            MigrationRoundTripPreservesCity();
            CityWinWithoutPurchases();
            WithdrawAppliesConsequencesWithoutRefund();
            NewRunArchivesCityAndKeepsProfileRewards();
            DistrictIncidentsSpareTheAlternateCrossing();
        }

        public static void DeterministicDayTraceMatchesRules()
        {
            OperationsStrategicSession first = ScriptedDay(out OperationsCommandResult end);
            OperationsStrategicSession second = ScriptedDay(out OperationsCommandResult endAgain);
            Require(end.Accepted && endAgain.Accepted);
            Require(first.Day == 2 && first.ActionPoints == 3);
            AssertDistrict(first, 1, 40, 51, 45, 50, 29, 11, 45);
            AssertDistrict(first, 2, 40, 40, 40, 55, 17, 22, 40);
            AssertDistrict(first, 3, 33, 40, 35, 60, 12, 20, 35);
            AssertDistrict(first, 6, 28, 35, 40, 65, 12, 25, 35);
            Require(first.Incidents.Length == 1, "incidents=" + first.Incidents.Length);
            Require(first.Incidents[0].districtId == "district.operations.d06");
            Require(first.Incidents[0].missionId == "operation.o051");
            Require(first.Incidents[0].kind == (byte)OperationsIncidentKind.HostilePressure);
            Require(first.Incidents[0].dueDay == 3);
            Require(SameCity(first, second), "day trace diverged");
            Require(first.Save.committedReports.Length == 1);
            Require(first.Save.committedReports[0].districtDeltas[0].appliedIntelConfidence == -3);
            Require(first.Save.committedReports[0].districtDeltas[0].requestedIntelConfidence == -3);
        }

        public static void SignedClampAndOutcomeDeltas()
        {
            OperationsSignedMetricDelta recon = OperationsSignedMetricDelta.Victory(OperationsMissionFamilyKind.Recon);
            Require(recon.EnemyInfluence == -2, "signed recon enemy was clamped");
            OperationsSignedMetricDelta half = OperationsSignedMetricDelta.HalfTowardZero(recon);
            Require(half.EnemyInfluence == -1 && half.IntelConfidence == 9 && half.Trust == 0);

            OperationsStrategicSession session = NewRun(1201);
            OperationsCommandResult metrics = session.CommitFixtureMetrics(Id(), session.Revision, 1, 50, 1, 98, 1, 20, 10, 40);
            OperationsOfferSaveData offer = FindMission(session, "operation.o001");
            int beforeCredits = session.RewardCredits;
            OperationsCommandResult deploy = session.Submit(Deploy(metrics.NewRevision, offer));
            OperationsAttemptSaveData attempt = Reserved(session);
            OperationsCommandResult partial = session.SubmitResult(
                Cmd(deploy.NewRevision, OperationsCommandKind.Conclude),
                Result(session, attempt, "partialhash0001", OperationsOutcomeKind.Partial));
            Require(partial.Accepted, partial.ReasonCode.ToString());
            OperationsDistrictComponent district = session.GetDistrict(1);
            Require(district.EnemyInfluence == 0, "partial E=" + district.EnemyInfluence);
            Require(district.IntelConfidence == 29, "partial C=" + district.IntelConfidence);
            Require(district.Trust == 1, "partial T=" + district.Trust);
            Require(district.Infrastructure == 98);
            Require(!Victory("operation.o001", session));
            Require(session.RewardCredits == beforeCredits);

            OperationsCommandResult redeploy = session.Submit(Deploy(partial.NewRevision, FindMission(session, "operation.o001")));
            attempt = Reserved(session);
            OperationsObjectiveFact[] sites =
            {
                new("anchor.operations.d01.service", false, true, 1)
            };
            OperationsCommandResult defeat = session.SubmitResult(
                Cmd(redeploy.NewRevision, OperationsCommandKind.Conclude),
                Result(session, attempt, "defeathash00001", OperationsOutcomeKind.Defeat, 11, 8, 16, sites));
            Require(defeat.Accepted, defeat.ReasonCode.ToString());
            district = session.GetDistrict(1);
            Require(district.Trust == 0, "harm trust cap T=" + district.Trust);
            Require(district.Infrastructure == 93, "site harm I=" + district.Infrastructure);
            Require(district.Security == 44, "loss S=" + district.Security);
            Require(district.EnemyInfluence == 6, "defeat E=" + district.EnemyInfluence);
            Require(SiteState(session, "anchor.operations.d01.service") == OperationsSiteStateKind.Damaged);
        }

        public static void SixActionsRespectLimitsAndAp()
        {
            OperationsStrategicSession session = NewRun(1202);
            int revision = session.Revision;
            OperationsCommandResult analyze = session.Submit(Action(revision, 1, OperationsAbstractActionKind.Analyze));
            OperationsCommandResult again = session.Submit(Action(analyze.NewRevision, 1, OperationsAbstractActionKind.Analyze));
            Require(analyze.Accepted && analyze.NewRevision != revision);
            Require(!again.Accepted && again.ReasonCode == OperationsReasonCode.ActionLimitReached);
            Require(session.ActionPoints == 2);
            Require(session.GetDistrict(1).IntelConfidence == 32);
            Require(session.GetDistrict(1).PublicHintMissionId == "operation.o001");
            Require(!Victory("operation.o001", session));

            OperationsCommandResult blocked = session.CommitFixtureMetrics(Id(), again.NewRevision, 1, 40, 45, 45, 100, 20, 20, 45);
            OperationsCommandResult service = session.Submit(Action(blocked.NewRevision, 1, OperationsAbstractActionKind.Service));
            Require(!service.Accepted && service.ReasonCode == OperationsReasonCode.PreconditionFailed);
            Require(session.ActionPoints == 2);

            OperationsCommandResult patrol = session.Submit(Action(service.NewRevision, 2, OperationsAbstractActionKind.Patrol));
            OperationsCommandResult allocate = session.Submit(Action(patrol.NewRevision, 3, OperationsAbstractActionKind.Allocate));
            OperationsCommandResult allocateAgain = session.Submit(Action(allocate.NewRevision, 4, OperationsAbstractActionKind.Allocate));
            Require(patrol.Accepted && allocate.Accepted);
            Require(!allocateAgain.Accepted && allocateAgain.ReasonCode == OperationsReasonCode.ActionLimitReached);
            Require(session.ActionPoints == 0);
            Require(session.GetDistrict(2).Security == 40 && session.GetDistrict(2).Heat == 27);
            Require(session.GetDistrict(3).SupplyReadiness == 43);

            OperationsCommandResult starved = session.Submit(Action(allocateAgain.NewRevision, 5, OperationsAbstractActionKind.Deescalate));
            Require(!starved.Accepted && starved.ReasonCode == OperationsReasonCode.InsufficientActionPoints);
            OperationsCommandResult end = session.Submit(End(starved.NewRevision));
            Require(end.Accepted, end.ReasonCode.ToString());
            OperationsCommandResult deescalate = session.Submit(Action(end.NewRevision, 5, OperationsAbstractActionKind.Deescalate));
            OperationsCommandResult community = session.Submit(Action(deescalate.NewRevision, 5, OperationsAbstractActionKind.Community));
            Require(deescalate.Accepted && community.Accepted);
            Require(session.GetDistrict(5).Heat == 1, "H=" + session.GetDistrict(5).Heat);
            Require(session.GetDistrict(5).Trust == 46, "T=" + session.GetDistrict(5).Trust);
        }

        public static void FloorRecoveryRemainsPossible()
        {
            byte[] campaign = { 9, 9, 9 };
            byte[] quick = { 8, 8 };
            OperationsStrategicSession session = NewRun(1203, campaign, quick);
            OperationsCommandResult primed = session.CommitFixtureMetrics(Id(), session.Revision, 1, 0, 0, 0, 100, 0, 100, 0);
            int revision = primed.NewRevision;
            Require(!session.Submit(Action(revision, 1, OperationsAbstractActionKind.Service)).Accepted);
            revision = session.Revision;
            Require(session.ActionPoints == 3);
            OperationsCommandResult patrol = session.Submit(Action(revision, 1, OperationsAbstractActionKind.Patrol));
            OperationsCommandResult community = session.Submit(Action(patrol.NewRevision, 1, OperationsAbstractActionKind.Community));
            OperationsCommandResult analyze = session.Submit(Action(community.NewRevision, 1, OperationsAbstractActionKind.Analyze));
            Require(patrol.Accepted && community.Accepted && analyze.Accepted);
            Require(session.GetDistrict(1).Security == 5);
            Require(session.GetDistrict(1).Trust == 6);
            Require(session.GetDistrict(1).IntelConfidence == 12);
            OperationsCommandResult end = session.Submit(End(analyze.NewRevision));
            Require(end.Accepted, end.ReasonCode.ToString());
            OperationsCommandResult deescalate = session.Submit(Action(end.NewRevision, 1, OperationsAbstractActionKind.Deescalate));
            OperationsCommandResult allocate = session.Submit(Action(deescalate.NewRevision, 1, OperationsAbstractActionKind.Allocate));
            Require(deescalate.Accepted && allocate.Accepted);
            Require(session.GetDistrict(1).SupplyReadiness > 0);
            Require(session.GetDistrict(1).Heat < 100);

            int guard = 0;
            while (session.GetDistrict(1).EnemyInfluence > 70)
            {
                if (++guard > 80)
                    throw new InvalidOperationException("Enemy floor did not recover. " + Dump(session));
                if (session.ActionPoints <= 0)
                {
                    OperationsCommandResult day = session.Submit(End(session.Revision));
                    Require(day.Accepted, day.ReasonCode.ToString());
                    continue;
                }

                if (TryWinDistrict(session, 1))
                    continue;
                if (session.GetDistrict(1).Security < 40 &&
                    TryAction(session, 1, OperationsAbstractActionKind.Patrol))
                    continue;
                if (session.GetDistrict(1).IntelConfidence < 40 &&
                    TryAction(session, 1, OperationsAbstractActionKind.Analyze))
                    continue;
                OperationsCommandResult refresh = session.Submit(End(session.Revision));
                Require(refresh.Accepted, refresh.ReasonCode.ToString());
            }

            if (session.ActionPoints <= 0)
                Require(session.Submit(End(session.Revision)).Accepted);
            int infrastructure = session.GetDistrict(1).Infrastructure;
            OperationsCommandResult recovered = session.Submit(Action(session.Revision, 1, OperationsAbstractActionKind.Service));
            Require(recovered.Accepted, recovered.ReasonCode.ToString());
            Require(session.GetDistrict(1).Infrastructure == infrastructure + 5);
            Require(SameBytes(campaign, session.Store.CampaignEnvelope));
            Require(SameBytes(quick, session.Store.QuickGameEnvelope));
        }

        public static void OfferDirectorStableAndSlotGated()
        {
            OperationsStrategicSession session = NewRun(1204);
            string[] before = OfferIds(session);
            Require(before.Length == 12);
            Require(FindMission(session, "operation.o001").missionId == "operation.o001");
            Require(FindMission(session, "operation.o002").missionId == "operation.o002");
            Require(!HasOffer(session, "operation.o003"));
            Require(!HasOffer(session, "operation.o010"));
            OperationsCommandResult analyze = session.Submit(Action(session.Revision, 1, OperationsAbstractActionKind.Analyze));
            Require(analyze.Accepted);
            Require(SameIds(before, OfferIds(session)));
            OperationsStrategicSession twin = NewRun(1204);
            Require(SameIds(before, OfferIds(twin)));

            OperationsCommandResult win = WinMission(session, "operation.o001", analyze.NewRevision);
            Require(win.Accepted, win.ReasonCode.ToString());
            Require(!OperationsOfferDirectorSystem.IsSlotEligible(World(session), "district.operations.d01", 6));
            OperationsCommandResult end = session.Submit(End(win.NewRevision));
            Require(end.Accepted, end.ReasonCode.ToString());
            Require(HasOffer(session, "operation.o002") && HasOffer(session, "operation.o003"));
            Require(!HasOffer(session, "operation.o004"));
            Require(OperationsOfferDirectorSystem.IsSlotEligible(World(session), "district.operations.d01", 4));
            Require(!OperationsOfferDirectorSystem.IsSlotEligible(World(session), "district.operations.d01", 6));

            OperationsCommandResult second = WinMission(session, "operation.o002", end.NewRevision);
            Require(second.Accepted, second.ReasonCode.ToString());
            Require(OperationsOfferDirectorSystem.IsSlotEligible(World(session), "district.operations.d01", 6));
            Require(!OperationsOfferDirectorSystem.IsSlotEligible(World(session), "district.operations.d01", 10));
        }

        public static void IncidentsCapCooldownAndExpiry()
        {
            OperationsStrategicSession session = NewRun(1205);
            OperationsCommandResult first = session.Submit(End(session.Revision));
            Require(first.Accepted);
            Require(session.Incidents.Length == 1);
            string firstId = session.Incidents[0].incidentId;
            int due = session.Incidents[0].dueDay;
            byte kind = session.Incidents[0].kind;
            string district = session.Incidents[0].districtId;
            OperationsCommandResult second = session.Submit(End(first.NewRevision));
            Require(second.Accepted);
            Require(ContainsIncident(session, firstId));
            Require(session.Incidents.Length <= 2);
            while (session.Day < due)
            {
                OperationsCommandResult step = session.Submit(End(session.Revision));
                Require(step.Accepted, step.ReasonCode.ToString());
            }

            OperationsCommandResult expired = session.Submit(End(session.Revision));
            Require(expired.Accepted);
            Require(!ContainsIncident(session, firstId));
            Require(LastReport(session).expiredIncidentCount >= 1);
            for (int index = 0; index < session.Incidents.Length; index++)
            {
                bool same = session.Incidents[index].districtId == district && session.Incidents[index].kind == kind;
                Require(!same, "cooldown reused an incident kind");
            }
        }

        public static void AdjacencyPressureIsSimultaneous()
        {
            OperationsStrategicSession session = NewRun(1206);
            int revision = session.Revision;
            for (int number = 1; number <= 6; number++)
            {
                int enemy = number == 1 || number == 3 ? 80 : 10;
                OperationsCommandResult wrote = session.CommitFixtureMetrics(Id(), revision, number, 40, 50, 50, enemy, 20, 10, 50);
                Require(wrote.Accepted, wrote.ReasonCode.ToString());
                revision = wrote.NewRevision;
            }

            OperationsCommandResult end = session.Submit(End(revision));
            Require(end.Accepted, end.ReasonCode.ToString());
            AssertDistrict(session, 1, 38, 50, 50, 80, 17, 5, 48);
            AssertDistrict(session, 2, 40, 50, 50, 11, 17, 5, 50);
            AssertDistrict(session, 3, 38, 50, 50, 80, 17, 5, 48);
            AssertDistrict(session, 4, 40, 50, 50, 11, 17, 5, 50);
        }

        public static void DuplicateCommandDoesNotDoubleSpend()
        {
            OperationsStrategicSession session = NewRun(1207);
            OperationsOfferSaveData offer = FindMission(session, "operation.o001");
            OperationsCommand command = Deploy(session.Revision, offer);
            OperationsCommandResult first = session.Submit(command);
            OperationsCommandResult second = session.Submit(command);
            Require(first.Accepted && second.Accepted);
            Require(first.TransactionId == second.TransactionId);
            Require(first.NewRevision == second.NewRevision);
            Require(session.ActionPoints == 2);
            Require(session.Phase == OperationsRunPhaseKind.Reserved);
        }

        public static void ConflictingSettlementIsRejected()
        {
            OperationsStrategicSession session = NewRun(1208);
            OperationsCommandResult deployed = WinMission(session, "operation.o001", session.Revision);
            Require(deployed.Accepted, deployed.ReasonCode.ToString());
            int credits = session.RewardCredits;
            int enemy = session.GetDistrict(1).EnemyInfluence;
            OperationsAttemptSaveData attempt = LastSettled(session, "operation.o001");
            OperationsCommand duplicate = Cmd(deployed.NewRevision, OperationsCommandKind.Conclude);
            OperationsCommandResult same = session.SubmitResult(duplicate, Result(session, attempt, attempt.resultHash, OperationsOutcomeKind.Victory));
            Require(same.Accepted, same.ReasonCode.ToString());
            Require(same.TransactionId == attempt.transactionId);
            Require(session.RewardCredits == credits);
            Require(session.GetDistrict(1).EnemyInfluence == enemy);

            OperationsCommandResult conflict = session.SubmitResult(
                Cmd(same.NewRevision, OperationsCommandKind.Conclude),
                Result(session, attempt, "hashdiff000001", OperationsOutcomeKind.Victory));
            Require(!conflict.Accepted && conflict.ReasonCode == OperationsReasonCode.Conflict);
            Require(session.RewardCredits == credits);
            Require(session.GetDistrict(1).EnemyInfluence == enemy);
            int receipts = 0;
            for (int index = 0; index < session.Save.receipts.Length; index++)
            {
                if (session.Save.receipts[index].resultHash == attempt.resultHash)
                    receipts++;
            }

            Require(receipts == 1);
        }

        public static void CrashPreservesPriorRevision()
        {
            OperationsStrategicSession session = NewRun(1209);
            int revision = session.Revision;
            int points = session.ActionPoints;
            string before = session.CommittedJson;
            OperationsCommand command = Action(revision, 1, OperationsAbstractActionKind.Analyze);
            OperationsCommandResult pending = session.SubmitLeavingPending(command);
            Require(pending.Accepted);
            Require(session.Revision == revision);
            Require(session.ActionPoints == points);
            Require(session.CommittedJson == before);
            OperationsCommandResult blocked = session.Submit(Action(revision, 2, OperationsAbstractActionKind.Patrol));
            Require(!blocked.Accepted && blocked.ReasonCode == OperationsReasonCode.TechnicalFailure);
            OperationsCommandResult saved = session.RetrySave(command.CommandId);
            Require(saved.Accepted && saved.TransactionId == pending.TransactionId);
            Require(session.ActionPoints == points - 1);
            OperationsCommandResult retry = session.RetrySave(command.CommandId);
            Require(retry.TransactionId == saved.TransactionId);
            Require(session.ActionPoints == points - 1);

            OperationsStrategicSession crashed = NewRun(1210);
            int crashedRevision = crashed.Revision;
            int crashedPoints = crashed.ActionPoints;
            OperationsCommand crashCommand = Action(crashedRevision, 1, OperationsAbstractActionKind.Community);
            Require(crashed.SubmitLeavingPending(crashCommand).Accepted);
            crashed.SimulateProcessCrash();
            Require(crashed.Revision == crashedRevision);
            Require(crashed.ActionPoints == crashedPoints);
            OperationsCommandResult recovered = crashed.Submit(crashCommand);
            Require(recovered.Accepted);
            Require(crashed.ActionPoints == crashedPoints - 1);
            Require(crashed.Submit(crashCommand).TransactionId == recovered.TransactionId);
            Require(crashed.ActionPoints == crashedPoints - 1);
        }

        public static void StaleWriterAndUnknownSchemaRejected()
        {
            OperationsStrategicSession session = NewRun(1211);
            int stale = session.Revision;
            OperationsCommandResult first = session.Submit(Action(stale, 1, OperationsAbstractActionKind.Patrol));
            Require(first.Accepted);
            OperationsCommandResult writer = session.Submit(Action(stale, 2, OperationsAbstractActionKind.Patrol));
            Require(!writer.Accepted && writer.ReasonCode == OperationsReasonCode.InvalidRevision);
            Require(session.GetDistrict(1).Security == 45);
            Require(session.GetDistrict(2).Security == 35);
            Require(session.Revision == first.NewRevision);

            OperationsSaveMigrationResult unknown = OperationsSaveMigration.Migrate(new OperationsSaveData
            {
                schemaVersion = OperationsIdentityRules.CurrentSchemaVersion + 8,
                profileRevision = 4
            });
            OperationsStrategicSession locked = OperationsStrategicSession.FromMigration(unknown);
            string json = locked.CommittedJson;
            OperationsCommandResult rejected = locked.SubmitNewRun(Cmd(4, OperationsCommandKind.NewRun), 1212, OperationsDifficultyKind.Regular);
            Require(!rejected.Accepted && rejected.ReasonCode == OperationsReasonCode.SchemaUnknown);
            Require(locked.Revision == 4);
            Require(locked.CommittedJson == json);
            Require(!locked.HasActiveRun);
        }

        public static void TechnicalFailureRefundsOnceAndReservedResumes()
        {
            byte[] campaign = { 3, 1, 4 };
            byte[] quick = { 1, 5, 9 };
            OperationsStrategicSession session = NewRun(1213, campaign, quick);
            OperationsOfferSaveData offer = FindMission(session, "operation.o001");
            OperationsCommandResult deploy = session.Submit(Deploy(session.Revision, offer));
            Require(deploy.Accepted);
            Require(session.ActionPoints == 2);
            string snapshot = session.Save.pendingDeployment.snapshotHash;
            string json = session.CommittedJson;
            OperationsStrategicSession reloaded = OperationsStrategicSession.FromCommittedJson(json, campaign, quick);
            Require(reloaded.Phase == OperationsRunPhaseKind.Reserved);
            Require(reloaded.ActionPoints == 2);
            Require(reloaded.Save.pendingDeployment.snapshotHash == snapshot);
            OperationsCommandResult resume = reloaded.Submit(Cmd(reloaded.Revision, OperationsCommandKind.Resume));
            Require(resume.Accepted, resume.ReasonCode.ToString());
            Require(resume.TransactionId == deploy.TransactionId);
            Require(reloaded.ActionPoints == 2);
            Require(reloaded.Save.pendingDeployment.snapshotHash == snapshot);

            OperationsCommandResult refund = reloaded.Submit(Cmd(resume.NewRevision, OperationsCommandKind.TechnicalFailure));
            Require(refund.Accepted, refund.ReasonCode.ToString());
            Require(reloaded.ActionPoints == 3);
            Require(reloaded.Phase == OperationsRunPhaseKind.Dashboard);
            Require(reloaded.Save.pendingDeployment == null);
            OperationsCommandResult second = reloaded.Submit(Cmd(refund.NewRevision, OperationsCommandKind.TechnicalFailure));
            Require(!second.Accepted);
            Require(reloaded.ActionPoints == 3);
            Require(SameBytes(campaign, reloaded.Store.CampaignEnvelope));
            Require(SameBytes(quick, reloaded.Store.QuickGameEnvelope));
        }

        public static void RewardsStayInsideOperationsEnvelope()
        {
            byte[] campaign = System.Text.Encoding.UTF8.GetBytes("{\"chapterOpeningSeen\":true}");
            byte[] quick = { 6, 6 };
            OperationsStrategicSession session = NewRun(1214, campaign, quick);
            OperationsDistrictComponent before = session.GetDistrict(1);
            OperationsCommandResult deploy = session.Submit(Deploy(session.Revision, FindMission(session, "operation.o001")));
            OperationsCommandResult refund = session.Submit(Cmd(deploy.NewRevision, OperationsCommandKind.TechnicalFailure));
            Require(refund.Accepted, refund.ReasonCode.ToString());
            Require(Contains(session.Save.practiceMissionIds, "operation.o001"));
            OperationsCommandResult practice = session.Submit(Practice(refund.NewRevision, FindMission(session, "operation.o001")));
            Require(practice.Accepted, practice.ReasonCode.ToString());
            OperationsAttemptSaveData practiceAttempt = Reserved(session);
            OperationsCommandResult practiceResult = session.SubmitResult(
                Cmd(practice.NewRevision, OperationsCommandKind.Conclude),
                Result(session, practiceAttempt, "practicehash0001", OperationsOutcomeKind.Victory));
            Require(practiceResult.Accepted, practiceResult.ReasonCode.ToString());
            Require(session.RewardCredits == 0 && session.RewardCommanderXp == 0);
            Require(session.GetDistrict(1).Security == before.Security);
            Require(session.GetDistrict(1).EnemyInfluence == before.EnemyInfluence);

            OperationsCommandResult victory = WinMission(session, "operation.o001", practiceResult.NewRevision);
            Require(victory.Accepted, victory.ReasonCode.ToString());
            Require(session.RewardCredits == 120, "credits=" + session.RewardCredits);
            Require(session.RewardCommanderXp == 50, "xp=" + session.RewardCommanderXp);
            Require(Contains(session.Save.firstClearRewardIds, "operation.o001"));
            OperationsCommandResult end = session.Submit(End(victory.NewRevision));
            Require(end.Accepted, end.ReasonCode.ToString());
            Require(session.RewardCredits == 140, "end credits=" + session.RewardCredits);
            Require(session.RewardCommanderXp == 50);
            string json = session.CommittedJson;
            for (int index = 0; index < OperationsCommitJson.ForbiddenAccountKeys.Length; index++)
            {
                string key = "\"" + OperationsCommitJson.ForbiddenAccountKeys[index] + "\":";
                Require(json.IndexOf(key, StringComparison.Ordinal) < 0, key);
            }

            Require(SameBytes(campaign, session.Store.CampaignEnvelope));
            Require(SameBytes(quick, session.Store.QuickGameEnvelope));
            Require(!Victory("operation.o010", session));
        }

        public static void MigrationRoundTripPreservesCity()
        {
            OperationsSaveMigrationResult legacy = OperationsSaveMigration.Migrate(new OperationsSaveData
            {
                schemaVersion = 0,
                profileRevision = 3,
                firstClearRewardIds = new[] { "operation.o009" }
            });
            Require(legacy.Disposition == OperationsSaveDispositionKind.Migrated);
            byte[] campaign = { 2, 2 };
            byte[] quick = { 3, 3 };
            OperationsStrategicSession session = OperationsStrategicSession.FromMigration(legacy, campaign, quick);
            Require(session.Revision == 3);
            Require(!session.HasActiveRun);
            OperationsCommandResult created = session.SubmitNewRun(Cmd(3, OperationsCommandKind.NewRun), 1215, OperationsDifficultyKind.Regular);
            Require(created.Accepted, created.ReasonCode.ToString());
            Require(Contains(session.Save.firstClearRewardIds, "operation.o009"));
            OperationsCommandResult analyze = session.Submit(Action(created.NewRevision, 1, OperationsAbstractActionKind.Analyze));
            Require(analyze.Accepted);
            OperationsStrategicSession reloaded = OperationsStrategicSession.FromCommittedJson(session.CommittedJson, campaign, quick);
            Require(reloaded.Revision == analyze.NewRevision);
            Require(reloaded.GetDistrict(1).IntelConfidence == session.GetDistrict(1).IntelConfidence);
            Require(reloaded.Day == 1);
            OperationsCommandResult patrol = reloaded.Submit(Action(reloaded.Revision, 2, OperationsAbstractActionKind.Patrol));
            Require(patrol.Accepted, patrol.ReasonCode.ToString());
            Require(reloaded.GetDistrict(2).Security == 40);
            Require(SameBytes(campaign, reloaded.Store.CampaignEnvelope));
            Require(SameBytes(quick, reloaded.Store.QuickGameEnvelope));
        }

        public static void CityWinWithoutPurchases()
        {
            byte[] campaign = { 1 };
            byte[] quick = { 2 };
            OperationsStrategicSession session = NewRun(1216, campaign, quick);
            int steps = 0;
            while (!session.CityCompleted)
            {
                if (++steps > 900)
                    throw new InvalidOperationException(Dump(session));
                if (session.Phase != OperationsRunPhaseKind.Dashboard)
                    throw new InvalidOperationException(Dump(session));
                if (session.ActionPoints <= 0 || !TryProgress(session))
                {
                    OperationsCommandResult end = session.Submit(End(session.Revision));
                    Require(end.Accepted, end.ReasonCode + " " + Dump(session));
                }
            }

            Require(session.CityCompleted);
            Require(session.Save.activeRun.consecutiveStableDays >= 2);
            for (int number = 1; number <= 6; number++)
            {
                OperationsDistrictComponent district = session.GetDistrict(number);
                Require(district.Security >= 60 && district.Trust >= 50 && district.Infrastructure >= 50);
                Require(district.EnemyInfluence <= 35 && district.SupplyReadiness >= 40);
                Require(Victory(OperationsIdentityRules.MissionId((number - 1) * 10 + 10), session));
            }

            Require(SameBytes(campaign, session.Store.CampaignEnvelope));
            Require(SameBytes(quick, session.Store.QuickGameEnvelope));
            Require(session.CommittedJson.IndexOf("\"credits\":", StringComparison.Ordinal) < 0);
        }

        public static void WithdrawAppliesConsequencesWithoutRefund()
        {
            OperationsStrategicSession session = NewRun(1217);
            int security = session.GetDistrict(1).Security;
            int enemy = session.GetDistrict(1).EnemyInfluence;
            OperationsCommandResult deploy = session.Submit(Deploy(session.Revision, FindMission(session, "operation.o001")));
            Require(session.ActionPoints == 2);
            OperationsCommandResult withdraw = session.Submit(Cmd(deploy.NewRevision, OperationsCommandKind.Withdraw));
            Require(withdraw.Accepted, withdraw.ReasonCode.ToString());
            Require(session.Phase == OperationsRunPhaseKind.Dashboard);
            Require(session.ActionPoints == 2);
            Require(session.GetDistrict(1).Security == security - 2);
            Require(session.GetDistrict(1).EnemyInfluence == enemy + 3);
            Require(session.RewardCredits == 0);
            Require(!Victory("operation.o001", session));
            OperationsCommandResult conclude = session.Submit(Cmd(withdraw.NewRevision, OperationsCommandKind.Conclude));
            Require(!conclude.Accepted && conclude.ReasonCode == OperationsReasonCode.PreconditionFailed);
        }

        public static void NewRunArchivesCityAndKeepsProfileRewards()
        {
            byte[] campaign = { 9, 1 };
            byte[] quick = { 8 };
            OperationsStrategicSession session = NewRun(1220, campaign, quick);
            OperationsCommandResult victory = WinMission(session, "operation.o001", session.Revision);
            Require(victory.Accepted, victory.ReasonCode.ToString());
            string oldRun = session.Save.activeRun.runId;
            int credits = session.RewardCredits;
            Require(credits == 120, "credits=" + credits);
            OperationsCommandResult refused = session.Submit(Cmd(victory.NewRevision, OperationsCommandKind.NewRun));
            Require(!refused.Accepted && refused.ReasonCode == OperationsReasonCode.PreconditionFailed);
            Require(session.Save.activeRun.runId == oldRun);
            Require(session.Save.runSummaries.Length == 0);
            OperationsCommandResult created = session.SubmitNewRun(
                Cmd(refused.NewRevision, OperationsCommandKind.NewRun),
                1221,
                OperationsDifficultyKind.Regular);
            Require(created.Accepted, created.ReasonCode.ToString());
            Require(session.Save.activeRun.runId != oldRun);
            Require(session.Day == 1);
            Require(session.Save.runSummaries.Length == 1);
            Require(session.Save.runSummaries[0].runId == oldRun);
            Require(session.RewardCredits == credits);
            Require(session.RewardCommanderXp == 50);
            Require(Contains(session.Save.firstClearRewardIds, "operation.o001"));
            Require(Contains(session.Save.practiceMissionIds, "operation.o001"));
            Require(SameBytes(campaign, session.Store.CampaignEnvelope));
            Require(SameBytes(quick, session.Store.QuickGameEnvelope));
            RequireNoDemoArt(session.CommittedJson);
            Require(CountRoutes(session, "district.operations.d01") == 1, "Old Quarter gained a second route");
            Require(CountRoutes(session, "district.operations.d04") == 2);
        }

        public static void DistrictIncidentsSpareTheAlternateCrossing()
        {
            OperationsStrategicSession blockade = NewRun(1222);
            Require(CountRoutes(blockade, "district.operations.d01") == 1);
            Require(RouteState(blockade, OperationsCityWorld.MainRouteId(1)) == OperationsRouteStateKind.Open);
            Require(CountRoutes(blockade, "district.operations.d04") == 2);
            Require(RouteState(blockade, OperationsCityWorld.MainRouteId(4)) == OperationsRouteStateKind.Open);
            Require(RouteState(blockade, OperationsCityWorld.CrossingRouteId(4)) == OperationsRouteStateKind.Open);
            int revision = Attempt(blockade, "operation.o031");
            revision = CalmDistricts(blockade, revision);
            revision = WriteMetrics(blockade, revision, 4, 40, 50, 55, 80, 40, 20, 20);
            OperationsCommandResult opened = blockade.Submit(End(revision));
            Require(opened.Accepted, opened.ReasonCode.ToString());
            OperationsIncidentSaveData incident = IncidentOn(blockade, "district.operations.d04");
            Require(incident.kind == (byte)OperationsIncidentKind.RoadBlockade, "kind=" + incident.kind);
            Require(incident.missionId == "operation.o034", incident.missionId);
            Require(incident.routeId == OperationsCityWorld.MainRouteId(4));
            Require(RouteState(blockade, OperationsCityWorld.MainRouteId(4)) == OperationsRouteStateKind.Open);
            EndUntilIncidentGone(blockade, incident.incidentId);
            Require(RouteState(blockade, OperationsCityWorld.MainRouteId(4)) == OperationsRouteStateKind.Contested);
            Require(RouteState(blockade, OperationsCityWorld.CrossingRouteId(4)) == OperationsRouteStateKind.Open);
            Require(RouteState(blockade, OperationsCityWorld.MainRouteId(1)) == OperationsRouteStateKind.Open);
            RequireNoDemoArt(blockade.CommittedJson);

            OperationsStrategicSession disruption = NewRun(1223);
            revision = Attempt(disruption, "operation.o021");
            string serviceId = OperationsCityWorld.ServiceSiteId(3);
            OperationsCommandResult restored = disruption.CommitFixtureSite(Id(), revision, serviceId, OperationsSiteStateKind.Restored);
            Require(restored.Accepted, restored.ReasonCode.ToString());
            Require(SiteState(disruption, serviceId) == OperationsSiteStateKind.Restored);
            revision = CalmDistricts(disruption, restored.NewRevision);
            revision = WriteMetrics(disruption, revision, 3, 40, 50, 20, 75, 40, 15, 60);
            OperationsCommandResult pressured = disruption.Submit(End(revision));
            Require(pressured.Accepted, pressured.ReasonCode.ToString());
            OperationsIncidentSaveData serviceIncident = IncidentOn(disruption, "district.operations.d03");
            Require(serviceIncident.kind == (byte)OperationsIncidentKind.ServiceDisruption, "kind=" + serviceIncident.kind);
            Require(serviceIncident.siteId == serviceId);
            Require(SiteState(disruption, serviceId) == OperationsSiteStateKind.Restored);
            EndUntilIncidentGone(disruption, serviceIncident.incidentId);
            Require(SiteState(disruption, serviceId) == OperationsSiteStateKind.Damaged);
            Require(RouteState(disruption, OperationsCityWorld.MainRouteId(3)) == OperationsRouteStateKind.Open);
            Require(RouteState(disruption, OperationsCityWorld.CrossingRouteId(4)) == OperationsRouteStateKind.Open);
            Require(CountRoutes(disruption, "district.operations.d01") == 1);
            RequireNoDemoArt(disruption.CommittedJson);
        }

        private static int _nextId = 1;

        private static string Id() => "cmd.operations." + (_nextId++).ToString("x8");

        private static OperationsCommand Cmd(int revision, OperationsCommandKind kind) =>
            new(Id(), revision, kind, string.Empty, string.Empty, string.Empty);

        private static OperationsCommand Action(int revision, int district, OperationsAbstractActionKind kind) =>
            new(Id(), revision, OperationsCommandKind.AbstractAction, OperationsIdentityRules.DistrictId(district), string.Empty, OperationsDashboardActionMap.ActionId(kind));

        private static OperationsCommand Deploy(int revision, OperationsOfferSaveData offer) =>
            new(Id(), revision, OperationsCommandKind.Deploy, offer.districtId, offer.offerId, string.Empty);

        private static OperationsCommand Practice(int revision, OperationsOfferSaveData offer) =>
            new(Id(), revision, OperationsCommandKind.Practice, offer.districtId, offer.offerId, string.Empty);

        private static OperationsCommand End(int revision) =>
            new(Id(), revision, OperationsCommandKind.EndDay, string.Empty, string.Empty, string.Empty);

        private static OperationsStrategicSession NewRun(int seed, byte[] campaign = null, byte[] quick = null)
        {
            OperationsStrategicSession session = OperationsStrategicSession.CreateNew(campaign, quick);
            OperationsCommandResult created = session.SubmitNewRun(Cmd(0, OperationsCommandKind.NewRun), seed, OperationsDifficultyKind.Regular);
            Require(created.Accepted, created.ReasonCode.ToString());
            return session;
        }

        private static OperationsStrategicSession ScriptedDay(out OperationsCommandResult end)
        {
            OperationsStrategicSession session = NewRun(1102);
            OperationsCommandResult analyze = session.Submit(Action(session.Revision, 1, OperationsAbstractActionKind.Analyze));
            OperationsCommandResult community = session.Submit(Action(analyze.NewRevision, 1, OperationsAbstractActionKind.Community));
            OperationsCommandResult patrol = session.Submit(Action(community.NewRevision, 2, OperationsAbstractActionKind.Patrol));
            end = session.Submit(End(patrol.NewRevision));
            return session;
        }

        private static OperationsCommandResult WinMission(OperationsStrategicSession session, string missionId, int revision)
        {
            OperationsOfferSaveData offer = FindMission(session, missionId);
            OperationsCommandResult deploy = session.Submit(Deploy(revision, offer));
            if (!deploy.Accepted)
                return deploy;
            OperationsAttemptSaveData attempt = Reserved(session);
            return session.SubmitResult(
                Cmd(deploy.NewRevision, OperationsCommandKind.Conclude),
                Result(session, attempt, "win" + missionId.Replace(".", "") + attempt.attemptOrdinal.ToString("00"), OperationsOutcomeKind.Victory));
        }

        private static bool TryProgress(OperationsStrategicSession session)
        {
            if (TryWinOffer(session, true))
                return true;
            if (TryWinOffer(session, false))
                return true;
            return TryLift(session);
        }

        private static bool TryWinAny(OperationsStrategicSession session) => TryWinOffer(session, false);

        private static bool TryWinDistrict(OperationsStrategicSession session, int districtNumber)
        {
            string districtId = OperationsIdentityRules.DistrictId(districtNumber);
            OperationsOfferSaveData[] offers = session.Offers;
            OperationsOfferSaveData best = null;
            int bestSlot = 99;
            bool bestFresh = false;
            for (int index = 0; index < offers.Length; index++)
            {
                if (offers[index].districtId != districtId)
                    continue;
                bool fresh = !Victory(offers[index].missionId, session);
                int slot = Slot(offers[index].missionId);
                if (best != null && bestFresh && !fresh)
                    continue;
                if (best != null && bestFresh == fresh && slot >= bestSlot)
                    continue;
                best = offers[index];
                bestSlot = slot;
                bestFresh = fresh;
            }

            if (best == null)
                return false;
            if (!Deployable(session, best.missionId))
            {
                OperationsCommandResult analyze = session.Submit(Action(session.Revision, districtNumber, OperationsAbstractActionKind.Analyze));
                return analyze.Accepted;
            }

            return WinMission(session, best.missionId, session.Revision).Accepted;
        }

        private static bool TryWinOffer(OperationsStrategicSession session, bool urgentOnly)
        {
            OperationsOfferSaveData[] offers = session.Offers;
            OperationsOfferSaveData best = null;
            int bestSlot = 99;
            for (int index = 0; index < offers.Length; index++)
            {
                if (urgentOnly && !offers[index].urgent)
                    continue;
                if (Victory(offers[index].missionId, session))
                    continue;
                int slot = Slot(offers[index].missionId);
                if (slot >= bestSlot)
                    continue;
                best = offers[index];
                bestSlot = slot;
            }

            if (best == null)
                return false;
            if (!Deployable(session, best.missionId))
            {
                if (!OperationsIdentityRules.TryParseDistrictNumber(best.districtId, out int district))
                    return false;
                OperationsCommandResult analyze = session.Submit(Action(session.Revision, district, OperationsAbstractActionKind.Analyze));
                return analyze.Accepted;
            }

            OperationsCommandResult won = WinMission(session, best.missionId, session.Revision);
            return won.Accepted;
        }

        private static bool TryLift(OperationsStrategicSession session)
        {
            for (int number = 1; number <= 6; number++)
            {
                OperationsDistrictComponent district = session.GetDistrict(number);
                if (district.Security < 60 && TryAction(session, number, OperationsAbstractActionKind.Patrol))
                    return true;
                if (district.Trust < 50 && TryAction(session, number, OperationsAbstractActionKind.Community))
                    return true;
                if (district.Infrastructure < 50 && district.EnemyInfluence <= 70 &&
                    TryAction(session, number, OperationsAbstractActionKind.Service))
                    return true;
                if (district.SupplyReadiness < 40 && TryAction(session, number, OperationsAbstractActionKind.Allocate))
                    return true;
                if (district.Heat > 40 && TryAction(session, number, OperationsAbstractActionKind.Deescalate))
                    return true;
                if (district.IntelConfidence < 40 && TryAction(session, number, OperationsAbstractActionKind.Analyze))
                    return true;
            }

            return false;
        }

        private static bool TryAction(OperationsStrategicSession session, int district, OperationsAbstractActionKind kind)
        {
            int before = session.ActionPoints;
            OperationsCommandResult result = session.Submit(Action(session.Revision, district, kind));
            if (result.Accepted)
                return true;
            Require(session.ActionPoints == before);
            return false;
        }

        private static bool Deployable(OperationsStrategicSession session, string missionId)
        {
            OperationsCityWorld world = World(session);
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                if (OperationsCatalogIndex.Entries[index].MissionId == missionId)
                    return OperationsOfferDirectorSystem.IsDeployable(world, OperationsCatalogIndex.Entries[index]);
            }

            return false;
        }

        private static OperationsCityWorld World(OperationsStrategicSession session) =>
            OperationsCityWorld.FromSave(session.Save);

        private static int Slot(string missionId)
        {
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                if (OperationsCatalogIndex.Entries[index].MissionId == missionId)
                    return OperationsCatalogIndex.Entries[index].LocalSlot;
            }

            return 99;
        }

        private static OperationsMissionResult Result(
            OperationsStrategicSession session,
            OperationsAttemptSaveData attempt,
            string hash,
            OperationsOutcomeKind outcome,
            int deaths = 0,
            int losses = 0,
            int initial = 16,
            OperationsObjectiveFact[] sites = null)
        {
            return new OperationsMissionResult(
                1,
                session.Save.activeRun.runId,
                attempt.offerId,
                attempt.missionId,
                attempt.sessionId,
                attempt.attemptOrdinal,
                1,
                outcome,
                "terminal",
                1,
                Array.Empty<OperationsObjectiveFact>(),
                Array.Empty<OperationsObjectiveFact>(),
                deaths,
                sites ?? Array.Empty<OperationsObjectiveFact>(),
                Array.Empty<OperationsObjectiveFact>(),
                Array.Empty<string>(),
                losses,
                initial,
                hash);
        }

        private static OperationsOfferSaveData FindMission(OperationsStrategicSession session, string missionId)
        {
            OperationsOfferSaveData[] offers = session.Offers;
            for (int index = 0; index < offers.Length; index++)
            {
                if (offers[index].missionId == missionId)
                    return offers[index];
            }

            throw new InvalidOperationException("Missing offer " + missionId + " " + Dump(session));
        }

        private static bool HasOffer(OperationsStrategicSession session, string missionId)
        {
            OperationsOfferSaveData[] offers = session.Offers;
            for (int index = 0; index < offers.Length; index++)
            {
                if (offers[index].missionId == missionId)
                    return true;
            }

            return false;
        }

        private static string[] OfferIds(OperationsStrategicSession session)
        {
            OperationsOfferSaveData[] offers = session.Offers;
            string[] ids = new string[offers.Length];
            for (int index = 0; index < offers.Length; index++)
                ids[index] = offers[index].offerId;
            Array.Sort(ids, StringComparer.Ordinal);
            return ids;
        }

        private static OperationsAttemptSaveData Reserved(OperationsStrategicSession session)
        {
            Require(session.TryReservedAttempt(out OperationsAttemptSaveData attempt));
            return attempt;
        }

        private static OperationsAttemptSaveData LastSettled(OperationsStrategicSession session, string missionId)
        {
            OperationsAttemptSaveData[] attempts = session.Save.activeRun.attempts;
            for (int index = attempts.Length - 1; index >= 0; index--)
            {
                if (attempts[index].missionId == missionId && attempts[index].phase == OperationsAttemptPhase.Settled)
                    return attempts[index];
            }

            throw new InvalidOperationException("Missing settled attempt " + missionId);
        }

        private static bool Victory(string missionId, OperationsStrategicSession session)
        {
            OperationsMilestoneSaveData[] milestones = session.Save.activeRun.milestones ?? Array.Empty<OperationsMilestoneSaveData>();
            for (int index = 0; index < milestones.Length; index++)
            {
                if (milestones[index].missionId == missionId)
                    return milestones[index].victory;
            }

            return false;
        }

        private static bool ContainsIncident(OperationsStrategicSession session, string incidentId)
        {
            for (int index = 0; index < session.Incidents.Length; index++)
            {
                if (session.Incidents[index].incidentId == incidentId)
                    return true;
            }

            return false;
        }

        private static OperationsDayReportSaveData LastReport(OperationsStrategicSession session)
        {
            OperationsDayReportSaveData[] reports = session.Save.committedReports;
            return reports[reports.Length - 1];
        }

        private static int Attempt(OperationsStrategicSession session, string missionId)
        {
            OperationsCommandResult deploy = session.Submit(Deploy(session.Revision, FindMission(session, missionId)));
            Require(deploy.Accepted, deploy.ReasonCode.ToString());
            OperationsCommandResult refund = session.Submit(Cmd(deploy.NewRevision, OperationsCommandKind.TechnicalFailure));
            Require(refund.Accepted, refund.ReasonCode.ToString());
            return refund.NewRevision;
        }

        private static int CalmDistricts(OperationsStrategicSession session, int revision)
        {
            for (int number = 1; number <= 6; number++)
                revision = WriteMetrics(session, revision, number, 50, 50, 50, 20, 40, 10, 50);
            return revision;
        }

        private static int WriteMetrics(
            OperationsStrategicSession session,
            int revision,
            int number,
            int security,
            int trust,
            int infrastructure,
            int enemy,
            int intel,
            int heat,
            int supply)
        {
            OperationsCommandResult wrote = session.CommitFixtureMetrics(
                Id(),
                revision,
                number,
                security,
                trust,
                infrastructure,
                enemy,
                intel,
                heat,
                supply);
            Require(wrote.Accepted, wrote.ReasonCode.ToString());
            return wrote.NewRevision;
        }

        private static void EndUntilIncidentGone(OperationsStrategicSession session, string incidentId)
        {
            for (int step = 0; step < 6 && ContainsIncident(session, incidentId); step++)
            {
                OperationsCommandResult end = session.Submit(End(session.Revision));
                Require(end.Accepted, end.ReasonCode.ToString());
            }

            Require(!ContainsIncident(session, incidentId), "incident stayed active");
        }

        private static OperationsIncidentSaveData IncidentOn(OperationsStrategicSession session, string districtId)
        {
            for (int index = 0; index < session.Incidents.Length; index++)
            {
                if (session.Incidents[index].districtId == districtId)
                    return session.Incidents[index];
            }

            throw new InvalidOperationException("Missing incident on " + districtId + " " + Dump(session));
        }

        private static int CountRoutes(OperationsStrategicSession session, string districtId)
        {
            int count = 0;
            OperationsRouteSaveData[] routes = session.Save.activeRun.routes ?? Array.Empty<OperationsRouteSaveData>();
            for (int index = 0; index < routes.Length; index++)
            {
                if (routes[index].districtId == districtId)
                    count++;
            }

            return count;
        }

        private static OperationsRouteStateKind RouteState(OperationsStrategicSession session, string routeId)
        {
            OperationsRouteSaveData[] routes = session.Save.activeRun.routes;
            for (int index = 0; index < routes.Length; index++)
            {
                if (routes[index].routeId == routeId)
                    return (OperationsRouteStateKind)routes[index].state;
            }

            throw new InvalidOperationException("Missing route " + routeId);
        }

        private static void RequireNoDemoArt(string json)
        {
            string[] banned =
            {
                "Demo2",
                "PolygonBattleRoyale",
                "SM_Bld_",
                "SM_Env_Bridge",
                "Demo2_Environment_Asset_Manifest",
                ".unity"
            };
            for (int index = 0; index < banned.Length; index++)
                Require(json.IndexOf(banned[index], StringComparison.Ordinal) < 0, banned[index]);
        }

        private static OperationsSiteStateKind SiteState(OperationsStrategicSession session, string siteId)
        {
            OperationsSiteSaveData[] sites = session.Save.activeRun.sites;
            for (int index = 0; index < sites.Length; index++)
            {
                if (sites[index].siteId == siteId)
                    return (OperationsSiteStateKind)sites[index].state;
            }

            throw new InvalidOperationException("Missing site " + siteId);
        }

        private static bool Contains(string[] values, string value)
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

        private static bool SameIds(string[] left, string[] right)
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

        private static bool SameCity(OperationsStrategicSession left, OperationsStrategicSession right)
        {
            if (left.Incidents.Length != right.Incidents.Length)
                return false;
            for (int index = 0; index < left.Incidents.Length; index++)
            {
                if (left.Incidents[index].incidentId != right.Incidents[index].incidentId)
                    return false;
            }

            for (int number = 1; number <= 6; number++)
            {
                OperationsDistrictComponent a = left.GetDistrict(number);
                OperationsDistrictComponent b = right.GetDistrict(number);
                if (a.Security != b.Security || a.Trust != b.Trust || a.Infrastructure != b.Infrastructure ||
                    a.EnemyInfluence != b.EnemyInfluence || a.IntelConfidence != b.IntelConfidence ||
                    a.Heat != b.Heat || a.SupplyReadiness != b.SupplyReadiness)
                    return false;
            }

            return true;
        }

        private static bool SameBytes(byte[] left, byte[] right)
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

        private static void AssertDistrict(
            OperationsStrategicSession session,
            int number,
            int security,
            int trust,
            int infrastructure,
            int enemy,
            int intel,
            int heat,
            int supply)
        {
            OperationsDistrictComponent district = session.GetDistrict(number);
            string actual = district.Security + "," + district.Trust + "," + district.Infrastructure + "," +
                            district.EnemyInfluence + "," + district.IntelConfidence + "," + district.Heat + "," +
                            district.SupplyReadiness;
            string expected = security + "," + trust + "," + infrastructure + "," + enemy + "," + intel + "," + heat + "," + supply;
            Require(actual == expected, "D" + number.ToString("00") + " " + actual + " expected " + expected);
        }

        private static string Dump(OperationsStrategicSession session)
        {
            string text = "day=" + session.Day + " ap=" + session.ActionPoints + " phase=" + session.Phase +
                          " stable=" + (session.HasActiveRun ? session.Save.activeRun.consecutiveStableDays.ToString() : "0") +
                          " city=" + session.CityCompleted;
            if (!session.HasActiveRun)
                return text;
            for (int number = 1; number <= 6; number++)
            {
                OperationsDistrictComponent district = session.GetDistrict(number);
                text += " | d" + number + "=" + district.Security + "," + district.Trust + "," + district.Infrastructure + "," +
                        district.EnemyInfluence + "," + district.IntelConfidence + "," + district.Heat + "," + district.SupplyReadiness;
            }

            int victories = 0;
            OperationsMilestoneSaveData[] milestones = session.Save.activeRun.milestones ?? Array.Empty<OperationsMilestoneSaveData>();
            for (int index = 0; index < milestones.Length; index++)
            {
                if (milestones[index].victory)
                    victories++;
            }

            return text + " victories=" + victories + " incidents=" + session.Incidents.Length;
        }

        private static void Require(bool condition, string message = "Operations P1 check failed.")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
