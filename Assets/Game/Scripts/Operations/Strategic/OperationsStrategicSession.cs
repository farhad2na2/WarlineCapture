using System;
using System.Collections.Generic;
using Game.Operations.Contracts;

namespace Game.Operations.Strategic
{
    public sealed class OperationsStrategicSession
    {
        private readonly OperationsProfileStore _store;

        private OperationsStrategicSession(OperationsProfileStore store) => _store = store;

        public OperationsProfileStore Store => _store;
        public OperationsSaveData Save => _store.Committed;
        public int Revision => _store.ProfileRevision;
        public string CommittedJson => _store.CommittedJson;
        public bool HasPendingSave => _store.HasPending;
        public bool HasActiveRun => OperationsSaveMigration.HasActiveRun(Save);
        public int Day => HasActiveRun ? Save.activeRun.day : 0;
        public int ActionPoints => HasActiveRun ? Save.activeRun.actionPoints : 0;
        public OperationsRunPhaseKind Phase => HasActiveRun ? Save.activeRun.phase : OperationsRunPhaseKind.None;
        public bool CityCompleted => HasActiveRun && Save.activeRun.cityCompleted;
        public int RewardCredits => Save.operationsRewardCredits;
        public int RewardCommanderXp => Save.operationsRewardCommanderXp;

        public static OperationsStrategicSession CreateNew(byte[] campaignEnvelope = null, byte[] quickGameEnvelope = null) =>
            new(OperationsProfileStore.CreateNew(campaignEnvelope, quickGameEnvelope));

        public static OperationsStrategicSession FromMigration(
            OperationsSaveMigrationResult migration,
            byte[] campaignEnvelope = null,
            byte[] quickGameEnvelope = null) =>
            new(OperationsProfileStore.FromMigration(migration, campaignEnvelope, quickGameEnvelope));

        public static OperationsStrategicSession FromCommittedJson(
            string json,
            byte[] campaignEnvelope = null,
            byte[] quickGameEnvelope = null) =>
            new(OperationsProfileStore.FromCommittedJson(json, campaignEnvelope, quickGameEnvelope));

        public OperationsDistrictComponent GetDistrict(int number) =>
            OperationsCityWorld.FromSave(Save).Districts[number - 1];

        public OperationsOfferSaveData[] Offers =>
            HasActiveRun ? Save.activeRun.offers ?? Array.Empty<OperationsOfferSaveData>() : Array.Empty<OperationsOfferSaveData>();

        public OperationsIncidentSaveData[] Incidents =>
            HasActiveRun ? Save.activeRun.incidents ?? Array.Empty<OperationsIncidentSaveData>() : Array.Empty<OperationsIncidentSaveData>();

        public bool TryReservedAttempt(out OperationsAttemptSaveData attempt)
        {
            attempt = null;
            if (!HasActiveRun || Save.activeRun.attempts == null)
                return false;
            for (int index = 0; index < Save.activeRun.attempts.Length; index++)
            {
                OperationsAttemptSaveData candidate = Save.activeRun.attempts[index];
                if (candidate.phase == OperationsAttemptPhase.Reserved)
                {
                    attempt = candidate;
                    return true;
                }
            }

            return false;
        }

        public OperationsCommandResult Submit(OperationsCommand command) => Submit(command, false);

        public OperationsCommandResult SubmitLeavingPending(OperationsCommand command) => Submit(command, true);

        public OperationsCommandResult SubmitNewRun(OperationsCommand command, int seed, OperationsDifficultyKind difficulty)
        {
            if (command.Kind != OperationsCommandKind.NewRun)
                throw new ArgumentException("New run confirmation requires OperationsCommandKind.NewRun.", nameof(command));
            if (seed == 0)
                throw new ArgumentOutOfRangeException(nameof(seed));

            if (!TryPrepare(command, out OperationsSaveData working, out OperationsCommandResult blocked))
                return blocked;

            OperationsCityWorld current = OperationsCityWorld.FromSave(working);
            if (current.FindReservedLiveAttempt(out _) ||
                current.Run.Phase == OperationsRunPhaseKind.Active ||
                current.Run.Phase == OperationsRunPhaseKind.PendingSettlement)
                return Finish(command, working, current, false, OperationsReasonCode.AttemptConflict, string.Empty, string.Empty, string.Empty, "new-run", false);

            if (current.HasActiveRun)
            {
                OperationsRunSummarySaveData[] summaries = working.runSummaries ?? Array.Empty<OperationsRunSummarySaveData>();
                OperationsRunSummarySaveData[] grown = new OperationsRunSummarySaveData[summaries.Length + 1];
                Array.Copy(summaries, grown, summaries.Length);
                grown[summaries.Length] = new OperationsRunSummarySaveData
                {
                    runId = current.Run.RunId,
                    cityCompleted = current.Run.CityCompleted,
                    endedDay = current.Run.Day
                };
                working.runSummaries = grown;
            }

            OperationsCityWorld created = OperationsRunInitializationSystem.Create(seed, difficulty);
            return Finish(command, working, created, true, OperationsReasonCode.None, string.Empty, string.Empty, string.Empty, "new-run", false);
        }

        public OperationsCommandResult SubmitResult(OperationsCommand command, OperationsMissionResult result) =>
            SubmitResult(command, result, false);

        public OperationsCommandResult SubmitResult(OperationsCommand command, OperationsMissionResult result, bool leavePending)
        {
            if (!TryPrepare(command, out OperationsSaveData working, out OperationsCommandResult blocked))
                return blocked;

            OperationsCityWorld world = OperationsCityWorld.FromSave(working);
            string key = SettlementKey(result);
            if (OpenAttemptSessionDiffers(world, result.SessionId))
            {
                return Finish(
                    command,
                    working,
                    world,
                    false,
                    OperationsReasonCode.Conflict,
                    string.Empty,
                    key,
                    result.ResultHash,
                    "stale-session",
                    leavePending);
            }

            if (TryFindReceipt(working, key, out OperationsReceiptSaveData receipt))
            {
                if (receipt.resultHash == result.ResultHash)
                {
                    return Finish(
                        command,
                        working,
                        world,
                        true,
                        OperationsReasonCode.None,
                        receipt.transactionId,
                        key,
                        result.ResultHash,
                        "duplicate-result",
                        leavePending,
                        false);
                }

                return Finish(command, working, world, false, OperationsReasonCode.Conflict, string.Empty, key, result.ResultHash, "conflict-result", leavePending);
            }

            bool accepted = OperationsConsequenceSystem.TryApply(world, working, result, out OperationsReasonCode reason);
            string transactionId = string.Empty;
            if (accepted)
            {
                int attemptIndex = world.FindAttemptBySession(result.SessionId);
                transactionId = world.Attempts[attemptIndex].TransactionId;
            }

            return Finish(
                command,
                working,
                world,
                accepted,
                reason,
                transactionId,
                key,
                result.ResultHash,
                result.Outcome.ToString(),
                leavePending,
                accepted);
        }

        public OperationsCommandResult RetrySave(string commandId)
        {
            if (!OperationsIdentityRules.IsValidGeneratedId(commandId, "cmd"))
                throw new ArgumentException("Retry Save requires a command id.", nameof(commandId));
            if (!_store.HasPending)
            {
                if (TryJournal(commandId, out OperationsCommandResult prior))
                    return prior;
                return new OperationsCommandResult(commandId, false, OperationsReasonCode.PreconditionFailed, Revision, string.Empty);
            }

            _store.CompleteCommit();
            if (!TryJournal(commandId, out OperationsCommandResult committed))
                throw new InvalidOperationException("Pending Operations commit did not contain command '" + commandId + "'.");
            return committed;
        }

        public void SimulateProcessCrash() => _store.AbandonPendingAsCrash();

        public OperationsCommandResult CommitFixtureMetrics(
            string commandId,
            int expectedRevision,
            int districtNumber,
            int security,
            int trust,
            int infrastructure,
            int enemyInfluence,
            int intelConfidence,
            int heat,
            int supplyReadiness)
        {
            OperationsCommand command = new(commandId, expectedRevision, OperationsCommandKind.Resume, string.Empty, string.Empty, string.Empty);
            if (!TryPrepare(command, out OperationsSaveData working, out OperationsCommandResult blocked))
                return blocked;
            OperationsCityWorld world = OperationsCityWorld.FromSave(working);
            if (!world.HasActiveRun)
                return Finish(command, working, world, false, OperationsReasonCode.PreconditionFailed, string.Empty, string.Empty, string.Empty, "fixture", false);
            OperationsDistrictComponent district = world.Districts[districtNumber - 1];
            district.Security = security;
            district.Trust = trust;
            district.Infrastructure = infrastructure;
            district.EnemyInfluence = enemyInfluence;
            district.IntelConfidence = intelConfidence;
            district.Heat = heat;
            district.SupplyReadiness = supplyReadiness;
            district.ChangeVersion++;
            world.Districts[districtNumber - 1] = district;
            return Finish(command, working, world, true, OperationsReasonCode.None, string.Empty, string.Empty, string.Empty, "fixture-metrics", false);
        }

        public OperationsCommandResult CommitFixtureSite(
            string commandId,
            int expectedRevision,
            string siteId,
            OperationsSiteStateKind state)
        {
            OperationsCommand command = new(commandId, expectedRevision, OperationsCommandKind.Resume, string.Empty, string.Empty, string.Empty);
            if (!TryPrepare(command, out OperationsSaveData working, out OperationsCommandResult blocked))
                return blocked;
            OperationsCityWorld world = OperationsCityWorld.FromSave(working);
            if (!world.HasActiveRun || !HasSite(world, siteId))
                return Finish(command, working, world, false, OperationsReasonCode.PreconditionFailed, string.Empty, string.Empty, string.Empty, "fixture-site", false);
            world.SetSite(siteId, state);
            return Finish(command, working, world, true, OperationsReasonCode.None, string.Empty, string.Empty, string.Empty, "fixture-site", false);
        }

        public OperationsCommandResult CommitFixtureMilestone(
            string commandId,
            int expectedRevision,
            string missionId,
            bool attempted,
            bool victory)
        {
            OperationsCommand command = new(commandId, expectedRevision, OperationsCommandKind.Resume, string.Empty, string.Empty, string.Empty);
            if (!TryPrepare(command, out OperationsSaveData working, out OperationsCommandResult blocked))
                return blocked;
            OperationsCityWorld world = OperationsCityWorld.FromSave(working);
            if (!world.TryMilestone(missionId, out int index))
            {
                world.Milestones.Add(new OperationsMilestoneComponent
                {
                    MissionId = missionId,
                    Attempted = attempted,
                    Victory = victory,
                    AttemptCount = attempted ? 1 : 0
                });
            }
            else
            {
                OperationsMilestoneComponent milestone = world.Milestones[index];
                milestone.Attempted = attempted;
                milestone.Victory = victory;
                world.Milestones[index] = milestone;
            }

            return Finish(command, working, world, true, OperationsReasonCode.None, string.Empty, string.Empty, string.Empty, "fixture-milestone", false);
        }

        private OperationsCommandResult Submit(OperationsCommand command, bool leavePending)
        {
            if (!TryPrepare(command, out OperationsSaveData working, out OperationsCommandResult blocked))
                return blocked;

            OperationsCityWorld world = OperationsCityWorld.FromSave(working);
            bool accepted = false;
            OperationsReasonCode reason = OperationsReasonCode.None;
            string transactionId = string.Empty;
            string grantKind = command.Kind.ToString();
            int endDayVictories = 0;
            OperationsDayReportSaveData report = null;

            switch (command.Kind)
            {
                case OperationsCommandKind.AbstractAction:
                    accepted = OperationsActionSystem.TryApply(world, command, out reason);
                    grantKind = "action";
                    break;
                case OperationsCommandKind.EndDay:
                    endDayVictories = world.Run.LiveVictoriesToday;
                    accepted = OperationsDayAdvanceSystem.TryAdvance(world, out reason, out report);
                    grantKind = "end-day";
                    break;
                case OperationsCommandKind.Deploy:
                    accepted = TryDeploy(world, working, out reason, out transactionId, command);
                    grantKind = "deploy";
                    break;
                case OperationsCommandKind.Withdraw:
                    accepted = TryWithdraw(world, out reason, out transactionId);
                    grantKind = "withdraw";
                    break;
                case OperationsCommandKind.Practice:
                    accepted = TryPractice(world, working, command, out reason, out transactionId);
                    grantKind = "practice";
                    break;
                case OperationsCommandKind.TechnicalFailure:
                    accepted = TryTechnicalFailure(world, out reason, out transactionId);
                    grantKind = "technical-failure";
                    break;
                case OperationsCommandKind.Resume:
                    accepted = TryResume(world, out reason, out transactionId);
                    grantKind = "resume";
                    break;
                case OperationsCommandKind.Conclude:
                case OperationsCommandKind.NewRun:
                    reason = OperationsReasonCode.PreconditionFailed;
                    break;
                default:
                    reason = OperationsReasonCode.PreconditionFailed;
                    break;
            }

            if (accepted && command.Kind == OperationsCommandKind.EndDay)
            {
                if (string.IsNullOrEmpty(transactionId))
                    transactionId = NextId(working, "txn");
                OperationsRewardSystem.ApplyEndDayBonus(working, transactionId, endDayVictories);
                working.repeatCreditsGrantedOnDay = 0;
                AppendReport(working, report);
            }

            return Finish(command, working, world, accepted, reason, transactionId, string.Empty, string.Empty, grantKind, leavePending, accepted);
        }

        private bool TryPrepare(OperationsCommand command, out OperationsSaveData working, out OperationsCommandResult blocked)
        {
            working = null;
            blocked = default;
            if (_store.IsReadOnly)
            {
                blocked = RejectWithoutWrite(command, OperationsReasonCode.SchemaUnknown);
                return false;
            }

            if (_store.HasPending)
            {
                blocked = RejectWithoutWrite(command, OperationsReasonCode.TechnicalFailure);
                return false;
            }

            if (TryJournal(command.CommandId, out OperationsCommandResult prior))
            {
                blocked = prior;
                return false;
            }

            working = _store.CloneCommitted();
            if (command.ExpectedRunRevision != working.profileRevision)
            {
                blocked = RejectWithoutWrite(command, OperationsReasonCode.InvalidRevision);
                return false;
            }

            return true;
        }

        private bool TryDeploy(
            OperationsCityWorld world,
            OperationsSaveData profile,
            out OperationsReasonCode reason,
            out string transactionId,
            OperationsCommand command)
        {
            reason = OperationsReasonCode.None;
            transactionId = string.Empty;
            if (!world.HasActiveRun || world.Run.Phase != OperationsRunPhaseKind.Dashboard || world.FindReservedLiveAttempt(out _))
            {
                reason = OperationsReasonCode.AttemptConflict;
                return false;
            }

            if (!world.TryOfferById(command.OfferId, out OperationsOfferComponent offer) || offer.DistrictId != command.DistrictId)
            {
                reason = OperationsReasonCode.OfferUnavailable;
                return false;
            }

            if (!TryCatalog(offer.MissionId, out OperationsCatalogEntry entry))
            {
                reason = OperationsReasonCode.OfferUnavailable;
                return false;
            }

            if (entry.LocalSlot == 10 && world.HasVictory(entry.MissionId))
            {
                reason = OperationsReasonCode.OfferUnavailable;
                return false;
            }

            if (!OperationsOfferDirectorSystem.IsDeployable(world, entry))
            {
                reason = OperationsReasonCode.InsufficientIntel;
                return false;
            }

            if (world.Run.ActionPoints < 1)
            {
                reason = OperationsReasonCode.InsufficientActionPoints;
                return false;
            }

            world.Run.ActionPoints--;
            world.MarkAttempted(entry.MissionId);
            int ordinal = 1;
            if (world.TryMilestone(entry.MissionId, out int milestoneIndex))
                ordinal = world.Milestones[milestoneIndex].AttemptCount;
            transactionId = NextId(profile, "txn");
            string sessionId = NextId(profile, "session");
            world.Attempts.Add(new OperationsAttemptComponent
            {
                SessionId = sessionId,
                OfferId = offer.OfferId,
                MissionId = entry.MissionId,
                DistrictId = entry.DistrictId,
                AttemptOrdinal = ordinal,
                TransactionId = transactionId,
                SnapshotHash = SnapshotHash(world, entry.MissionId),
                Phase = OperationsAttemptPhase.Reserved
            });
            world.Run.Phase = OperationsRunPhaseKind.Reserved;
            if (!Contains(profile.practiceMissionIds, entry.MissionId))
                profile.practiceMissionIds = Append(profile.practiceMissionIds, entry.MissionId);
            return true;
        }

        private static bool TryWithdraw(OperationsCityWorld world, out OperationsReasonCode reason, out string transactionId)
        {
            reason = OperationsReasonCode.None;
            transactionId = string.Empty;
            if (!world.FindReservedLiveAttempt(out OperationsAttemptComponent attempt))
            {
                reason = OperationsReasonCode.PreconditionFailed;
                return false;
            }

            int attemptIndex = world.FindAttemptBySession(attempt.SessionId);
            int districtIndex = world.DistrictIndex(attempt.DistrictId);
            OperationsMetricApplication applied = OperationsMetricMath.Apply(
                world.Districts[districtIndex],
                OperationsSignedMetricDelta.Withdrawn);
            world.Districts[districtIndex] = applied.District;
            attempt.Phase = OperationsAttemptPhase.Settled;
            attempt.ResultHash = "withdraw" + attempt.TransactionId;
            world.Attempts[attemptIndex] = attempt;
            world.Run.Phase = OperationsRunPhaseKind.Dashboard;
            transactionId = attempt.TransactionId;
            return true;
        }

        private static bool TryTechnicalFailure(OperationsCityWorld world, out OperationsReasonCode reason, out string transactionId)
        {
            reason = OperationsReasonCode.None;
            transactionId = string.Empty;
            if (!world.FindReservedLiveAttempt(out OperationsAttemptComponent attempt))
            {
                reason = OperationsReasonCode.PreconditionFailed;
                return false;
            }

            if (attempt.ApRefunded)
            {
                reason = OperationsReasonCode.DuplicateCommand;
                return false;
            }

            int attemptIndex = world.FindAttemptBySession(attempt.SessionId);
            world.Run.ActionPoints++;
            attempt.ApRefunded = true;
            attempt.Phase = OperationsAttemptPhase.RolledBack;
            world.Attempts[attemptIndex] = attempt;
            world.Run.Phase = OperationsRunPhaseKind.Dashboard;
            transactionId = attempt.TransactionId;
            return true;
        }

        private bool TryPractice(
            OperationsCityWorld world,
            OperationsSaveData profile,
            OperationsCommand command,
            out OperationsReasonCode reason,
            out string transactionId)
        {
            reason = OperationsReasonCode.None;
            transactionId = string.Empty;
            if (world.FindReservedLiveAttempt(out _))
            {
                reason = OperationsReasonCode.AttemptConflict;
                return false;
            }

            if (!world.TryOfferById(command.OfferId, out OperationsOfferComponent offer) ||
                !Contains(profile.practiceMissionIds, offer.MissionId))
            {
                reason = OperationsReasonCode.PreconditionFailed;
                return false;
            }

            transactionId = NextId(profile, "txn");
            world.Attempts.Add(new OperationsAttemptComponent
            {
                SessionId = NextId(profile, "session"),
                OfferId = offer.OfferId,
                MissionId = offer.MissionId,
                DistrictId = offer.DistrictId,
                AttemptOrdinal = 0,
                TransactionId = transactionId,
                SnapshotHash = SnapshotHash(world, offer.MissionId),
                Phase = OperationsAttemptPhase.Reserved,
                Practice = true
            });
            return true;
        }

        private static bool TryResume(OperationsCityWorld world, out OperationsReasonCode reason, out string transactionId)
        {
            reason = OperationsReasonCode.None;
            transactionId = string.Empty;
            if (!world.FindReservedLiveAttempt(out OperationsAttemptComponent attempt))
            {
                reason = OperationsReasonCode.PreconditionFailed;
                return false;
            }

            transactionId = attempt.TransactionId;
            return true;
        }

        private static bool OpenAttemptSessionDiffers(OperationsCityWorld world, string sessionId)
        {
            for (int index = 0; index < world.Attempts.Count; index++)
            {
                OperationsAttemptComponent attempt = world.Attempts[index];
                if (attempt.Phase == OperationsAttemptPhase.Reserved && attempt.SessionId != sessionId)
                    return true;
            }

            return false;
        }

        private OperationsCommandResult Finish(
            OperationsCommand command,
            OperationsSaveData working,
            OperationsCityWorld world,
            bool accepted,
            OperationsReasonCode reason,
            string transactionId,
            string settlementKey,
            string resultHash,
            string grantKind,
            bool leavePending,
            bool writeWorld = true)
        {
            if (accepted && string.IsNullOrEmpty(transactionId))
                transactionId = NextId(working, "txn");
            if (!accepted && reason == OperationsReasonCode.None)
                reason = OperationsReasonCode.PreconditionFailed;

            int newRevision = working.profileRevision + 1;
            working.profileRevision = newRevision;
            if (accepted && writeWorld && world.HasActiveRun)
            {
                world.Run.Revision = newRevision;
                world.WriteInto(working);
            }

            if (working.activeRun != null)
                working.activeRun.revision = newRevision;
            AppendJournal(working, command.CommandId, accepted, reason, newRevision, transactionId);
            if (accepted && writeWorld)
            {
                AppendReceipt(working, transactionId, settlementKey, resultHash, command.CommandId, grantKind);
            }

            if (!_store.TryBeginCommit(command.ExpectedRunRevision, working, out OperationsReasonCode beginReason))
                return RejectWithoutWrite(command, beginReason);
            OperationsCommandResult result = new(command.CommandId, accepted, accepted ? OperationsReasonCode.None : reason, newRevision, transactionId);
            if (!leavePending)
                _store.CompleteCommit();
            return result;
        }

        private OperationsCommandResult RejectWithoutWrite(OperationsCommand command, OperationsReasonCode reason) =>
            new(command.CommandId, false, reason, Revision, string.Empty);

        private bool TryJournal(string commandId, out OperationsCommandResult result)
        {
            OperationsCommandJournalSaveData[] journal = Save.commandJournal ?? Array.Empty<OperationsCommandJournalSaveData>();
            for (int index = 0; index < journal.Length; index++)
            {
                if (journal[index].commandId != commandId)
                    continue;
                result = new OperationsCommandResult(
                    journal[index].commandId,
                    journal[index].accepted,
                    (OperationsReasonCode)journal[index].reasonCode,
                    journal[index].newRevision,
                    journal[index].transactionId);
                return true;
            }

            result = default;
            return false;
        }

        private static string NextId(OperationsSaveData save, string name)
        {
            save.idSequence++;
            return OperationsStableIds.Generated(name, save.idSequence);
        }

        private static string SnapshotHash(OperationsCityWorld world, string missionId)
        {
            uint hash = OperationsStableIds.Mix(2166136261u, world.Run.Seed);
            hash = OperationsStableIds.Mix(hash, world.Run.Day);
            hash = OperationsStableIds.Mix(hash, missionId);
            for (int index = 0; index < world.Districts.Length; index++)
            {
                hash = OperationsStableIds.Mix(hash, world.Districts[index].Security);
                hash = OperationsStableIds.Mix(hash, world.Districts[index].EnemyInfluence);
                hash = OperationsStableIds.Mix(hash, world.Districts[index].SupplyReadiness);
            }

            return "snap" + OperationsStableIds.Hex8(hash) + OperationsStableIds.Hex8(OperationsStableIds.Mix(hash, world.Run.DirectorVersion));
        }

        private static string SettlementKey(OperationsMissionResult result) =>
            result.RunId + "|" + result.OfferId + "|" + result.SessionId + "|" + result.AttemptOrdinal.ToString();

        private static bool TryFindReceipt(OperationsSaveData save, string key, out OperationsReceiptSaveData receipt)
        {
            OperationsReceiptSaveData[] receipts = save.receipts ?? Array.Empty<OperationsReceiptSaveData>();
            for (int index = 0; index < receipts.Length; index++)
            {
                if (receipts[index].settlementKey == key && !string.IsNullOrEmpty(key))
                {
                    receipt = receipts[index];
                    return true;
                }
            }

            receipt = null;
            return false;
        }

        private static bool HasSite(OperationsCityWorld world, string siteId)
        {
            for (int index = 0; index < world.Sites.Count; index++)
            {
                if (world.Sites[index].SiteId == siteId)
                    return true;
            }

            return false;
        }

        private static bool TryCatalog(string missionId, out OperationsCatalogEntry entry)
        {
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                if (OperationsCatalogIndex.Entries[index].MissionId == missionId)
                {
                    entry = OperationsCatalogIndex.Entries[index];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        private static void AppendJournal(
            OperationsSaveData save,
            string commandId,
            bool accepted,
            OperationsReasonCode reason,
            int newRevision,
            string transactionId)
        {
            OperationsCommandJournalSaveData[] journal = save.commandJournal ?? Array.Empty<OperationsCommandJournalSaveData>();
            OperationsCommandJournalSaveData[] grown = new OperationsCommandJournalSaveData[journal.Length + 1];
            Array.Copy(journal, grown, journal.Length);
            grown[journal.Length] = new OperationsCommandJournalSaveData
            {
                commandId = commandId,
                accepted = accepted,
                reasonCode = (byte)(accepted ? OperationsReasonCode.None : reason),
                newRevision = newRevision,
                transactionId = transactionId ?? string.Empty
            };
            save.commandJournal = grown;
        }

        private static void AppendReceipt(
            OperationsSaveData save,
            string transactionId,
            string settlementKey,
            string resultHash,
            string commandId,
            string grantKind)
        {
            OperationsReceiptSaveData[] receipts = save.receipts ?? Array.Empty<OperationsReceiptSaveData>();
            OperationsReceiptSaveData[] grown = new OperationsReceiptSaveData[receipts.Length + 1];
            Array.Copy(receipts, grown, receipts.Length);
            grown[receipts.Length] = new OperationsReceiptSaveData
            {
                transactionId = transactionId ?? string.Empty,
                settlementKey = settlementKey ?? string.Empty,
                resultHash = resultHash ?? string.Empty,
                commandId = commandId ?? string.Empty,
                grantKind = grantKind ?? string.Empty
            };
            save.receipts = grown;
        }

        private static void AppendReport(OperationsSaveData save, OperationsDayReportSaveData report)
        {
            if (report == null)
                return;
            OperationsDayReportSaveData[] reports = save.committedReports ?? Array.Empty<OperationsDayReportSaveData>();
            OperationsDayReportSaveData[] grown = new OperationsDayReportSaveData[reports.Length + 1];
            Array.Copy(reports, grown, reports.Length);
            grown[reports.Length] = report;
            save.committedReports = grown;
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

        private static string[] Append(string[] values, string value)
        {
            string[] source = values ?? Array.Empty<string>();
            string[] grown = new string[source.Length + 1];
            Array.Copy(source, grown, source.Length);
            grown[source.Length] = value;
            return grown;
        }
    }

    public sealed class OperationsStrategicGateway : IUiOperationsGateway
    {
        private readonly OperationsStrategicSession _session;

        public OperationsStrategicGateway(OperationsStrategicSession session) =>
            _session = session ?? throw new ArgumentNullException(nameof(session));

        public bool TryReadOperations(out UiOperationsReadModel model)
        {
            if (!_session.HasActiveRun)
            {
                model = default;
                return false;
            }

            model = new UiOperationsReadModel(
                _session.Revision,
                _session.Day,
                _session.ActionPoints,
                _session.Phase,
                OperationsIdentityRules.DistrictId(1),
                _session.Phase == OperationsRunPhaseKind.Dashboard && !_session.HasPendingSave,
                _session.Phase == OperationsRunPhaseKind.Dashboard && _session.ActionPoints > 0);
            return true;
        }

        public OperationsCommandResult RequestDeploy(OperationsCommand command) => Forward(command, OperationsCommandKind.Deploy);
        public OperationsCommandResult RequestAbstractAction(OperationsCommand command) => Forward(command, OperationsCommandKind.AbstractAction);
        public OperationsCommandResult RequestEndDay(OperationsCommand command) => Forward(command, OperationsCommandKind.EndDay);
        public OperationsCommandResult RequestConclude(OperationsCommand command) => Forward(command, OperationsCommandKind.Conclude);
        public OperationsCommandResult RequestWithdraw(OperationsCommand command) => Forward(command, OperationsCommandKind.Withdraw);
        public OperationsCommandResult RequestPractice(OperationsCommand command) => Forward(command, OperationsCommandKind.Practice);
        public OperationsCommandResult RequestResume(OperationsCommand command) => Forward(command, OperationsCommandKind.Resume);

        private OperationsCommandResult Forward(OperationsCommand command, OperationsCommandKind expected)
        {
            if (command.Kind != expected)
            {
                return new OperationsCommandResult(
                    command.CommandId,
                    false,
                    OperationsReasonCode.PreconditionFailed,
                    _session.Revision,
                    string.Empty);
            }

            return _session.Submit(command);
        }
    }
}
