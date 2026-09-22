using System;
using System.Collections.Generic;
using Game.Operations.Contracts;
using Game.Operations.Strategic;
using Game.Operations.Tactical;

namespace Game.Operations.Loop
{
    /// <summary>
    /// Package 3 launch, return, and recovery. Scene loading stays behind the shared
    /// match-launch seam; this session records the mode-tagged request and does not
    /// invoke that seam. Profile money stays in the package 1 commit store.
    /// </summary>
    public sealed class OperationsLoopSession
    {
        const byte AdvanceOrder = 0;
        const byte PauseOrder = 1;
        const byte ObserveOrder = 2;
        const byte ScanOrder = 3;
        const byte InteractOrder = 4;
        const byte RepairOrder = 5;
        const byte HoldOrder = 6;
        const byte MoveOrder = 7;
        const byte EscortOrder = 8;
        const byte ExtractOrder = 9;
        const byte ConcludeOrder = 10;
        const byte WithdrawOrder = 11;
        const byte AttackOrder = 12;
        const byte EscortHoldOrder = 13;
        const byte DestroyOrder = 14;

        readonly OperationsStrategicSession _strategic;
        readonly OperationsLoopStore _store;
        readonly List<OperationsLoopOrder> _orders = new();
        OperationsTacticalSession _mission;
        OperationsCompiledTactical _definition;
        string[] _history = { OperationsShellNames.Operations };
        string _selectedOfferId = string.Empty;
        string _stagedCheckpointId = string.Empty;
        int _selectedDistrict;
        OperationsMatchMode _foreign = OperationsMatchMode.None;
        bool _replaying;
        bool _corrupt;
        bool _resumable;

        OperationsLoopSession(OperationsStrategicSession strategic, OperationsLoopStore store)
        {
            _strategic = strategic;
            _store = store;
        }

        public OperationsLoopPhase Phase
        {
            get
            {
                OperationsLoopDocument document = _store.Committed;
                if (document.Phase == OperationsLoopPhase.PendingResult && ReceiptMatches(document))
                    return OperationsLoopPhase.Settled;
                if (document.Phase == OperationsLoopPhase.Dashboard && _strategic.TryReservedAttempt(out _))
                    return OperationsLoopPhase.Reserved;
                return document.Phase;
            }
        }

        public OperationsMatchMode Slot
        {
            get
            {
                if (_store.Committed.Slot != OperationsMatchMode.None)
                    return _store.Committed.Slot;
                return _foreign;
            }
        }

        public int ActionPoints => _strategic.ActionPoints;
        public int Credits => _strategic.RewardCredits;
        public int CommanderXp => _strategic.RewardCommanderXp;
        public int Day => _strategic.Day;
        public int RestartCount => _store.Committed.RestartCount;
        public string SessionId => _store.Committed.SessionId;
        public string ContentHash => _store.Committed.ContentHash;
        public string ResultHash => _store.Committed.ResultHash;
        public string PublishedCheckpointId => _store.Committed.PublishedCheckpointId;
        public string MissionId => _store.Committed.MissionId;
        /// <summary>Committed city-profile revision. Starts at 0; -1 is not a profile revision.</summary>
        public int ProfileRevision => _strategic.Revision;

        /// <summary>
        /// Revision written onto the active campaign run by the last accepted save.
        /// -1 means the run is absent, which is not a successful settlement stamp.
        /// </summary>
        public int CampaignRunRevision =>
            _strategic.HasActiveRun ? _strategic.Save.activeRun.revision : -1;
        public string MapId => _store.Committed.MapId;
        public string OfferId => _store.Committed.OfferId;
        public string RunId => _store.Committed.RunId;
        public string DistrictId => _store.Committed.DistrictId;
        public string ScenarioId => _store.Committed.ScenarioId;
        public string TransactionId => _store.Committed.TransactionId;
        public int Seed => _store.Committed.Seed;
        public int AttemptOrdinal => _store.Committed.AttemptOrdinal;
        public OperationsDifficultyKind Difficulty =>
            _strategic.HasActiveRun ? _strategic.Save.activeRun.difficulty : OperationsDifficultyKind.Regular;
        public bool HasMission => _mission != null;
        public bool HasStagedCheckpoint => _store.HasStaged;
        public bool HasPending => _strategic.HasPendingSave || _store.HasPending;
        public bool CheckpointCorrupt => _corrupt;
        public string CommittedJson => _strategic.CommittedJson;
        public byte[] CampaignEnvelope => _strategic.Store.CampaignEnvelope;
        public byte[] QuickGameEnvelope => _strategic.Store.QuickGameEnvelope;

        public static OperationsLoopSession Create(int seed, byte[] campaignEnvelope, byte[] quickGameEnvelope)
        {
            OperationsStrategicSession strategic = OperationsStrategicSession.CreateNew(campaignEnvelope, quickGameEnvelope);
            string commandId = OperationsStableIds.Generated("cmd", 1);
            OperationsCommandResult created = strategic.SubmitNewRun(
                new OperationsCommand(commandId, 0, OperationsCommandKind.NewRun, string.Empty, string.Empty, string.Empty),
                seed,
                OperationsDifficultyKind.Regular);
            if (!created.Accepted)
                throw new InvalidOperationException("new_run:" + created.ReasonCode);
            return new OperationsLoopSession(strategic, new OperationsLoopStore());
        }

        public static OperationsLoopSession Open(
            string strategicJson,
            byte[] campaignEnvelope,
            byte[] quickGameEnvelope,
            OperationsLoopDocument document,
            IDictionary<string, string> blobs)
        {
            if (string.IsNullOrEmpty(strategicJson))
                throw new ArgumentException("Strategic json is required.", nameof(strategicJson));
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            OperationsStrategicSession strategic = OperationsStrategicSession.FromCommittedJson(
                strategicJson,
                campaignEnvelope,
                quickGameEnvelope);
            var store = new OperationsLoopStore();
            store.LoadCommitted(document, blobs);
            var session = new OperationsLoopSession(strategic, store);
            session.LoadShell();
            session.TryRestore();
            return session;
        }

        public OperationsLoopDocument CopyDocument() => _store.Committed.Copy();

        public void CopyCheckpointBlobs(Dictionary<string, string> destination) => _store.CopyBlobs(destination);

        public void Interrupt()
        {
            _strategic.SimulateProcessCrash();
            _store.Abandon();
            _mission = null;
            _definition = null;
            _orders.Clear();
            _replaying = false;
            _selectedOfferId = string.Empty;
            _stagedCheckpointId = string.Empty;
            _selectedDistrict = 0;
            LoadShell();
            TryRestore();
        }

        public bool Offers(OperationsRecoveryChoice choice)
        {
            switch (choice)
            {
                case OperationsRecoveryChoice.ResumeReservation:
                    return (Phase == OperationsLoopPhase.Reserved || Phase == OperationsLoopPhase.LaunchDispatched) &&
                           _strategic.TryReservedAttempt(out _);
                case OperationsRecoveryChoice.ResumeCheckpoint:
                    return Phase == OperationsLoopPhase.Active && _resumable && HasMission;
                case OperationsRecoveryChoice.RestartAttempt:
                    return Phase == OperationsLoopPhase.Active && !_resumable && !_corrupt && !HasMission;
                case OperationsRecoveryChoice.Withdraw:
                    return Phase == OperationsLoopPhase.Reserved ||
                           Phase == OperationsLoopPhase.LaunchDispatched ||
                           (Phase == OperationsLoopPhase.Active && !_resumable && !_corrupt && !HasMission);
                case OperationsRecoveryChoice.RefundTechnicalFailure:
                    return _corrupt || _store.Committed.RefundEligible;
                case OperationsRecoveryChoice.SettlePendingResult:
                    return Phase == OperationsLoopPhase.PendingResult;
                case OperationsRecoveryChoice.ReturnToDashboard:
                    return Phase == OperationsLoopPhase.Settled && !_store.Committed.ReturnAcknowledged;
                default:
                    return false;
            }
        }

        public OperationsDistrictComponent District(int number) => _strategic.GetDistrict(number);

        public bool TryOffer(string missionId, out OperationsOfferSaveData offer)
        {
            OperationsOfferSaveData[] offers = _strategic.Offers;
            for (int index = 0; index < offers.Length; index++)
            {
                if (offers[index].missionId == missionId)
                {
                    offer = offers[index];
                    return true;
                }
            }

            offer = null;
            return false;
        }

        public bool TryReserved(out OperationsAttemptSaveData attempt) => _strategic.TryReservedAttempt(out attempt);

        public bool MissionVictory(string missionId)
        {
            OperationsMilestoneSaveData[] milestones = _strategic.Save.activeRun.milestones ?? Array.Empty<OperationsMilestoneSaveData>();
            for (int index = 0; index < milestones.Length; index++)
            {
                if (milestones[index].missionId == missionId && milestones[index].victory)
                    return true;
            }

            return false;
        }

        public bool TryCommittedResult(out OperationsMissionResult result)
        {
            OperationsLoopDocument document = _store.Committed;
            if (string.IsNullOrEmpty(document.ResultText))
            {
                result = default;
                return false;
            }

            result = OperationsMissionProjection.Decode(document, document.ResultText);
            return true;
        }

        public bool TryLaunchRequest(out OperationsModeLaunchRequest request)
        {
            OperationsLoopDocument document = _store.Committed;
            if (document.Slot != OperationsMatchMode.Operations || string.IsNullOrEmpty(document.SessionId))
            {
                request = default;
                return false;
            }

            request = new OperationsModeLaunchRequest(
                OperationsMatchMode.Operations,
                document.MapId,
                document.ScenarioId,
                document.SessionId,
                OperationsShellNames.Operations,
                true,
                false,
                OperationsShellNames.DispatchOwner);
            return true;
        }

        public string PublishedCheckpointText()
        {
            if (!_store.TryBlob(PublishedCheckpointId, out string text))
                throw new InvalidOperationException("checkpoint_missing");
            return text;
        }

        public void CorruptPublishedCheckpoint()
        {
            string id = PublishedCheckpointId;
            if (!_store.TryBlob(id, out string text))
                throw new InvalidOperationException("checkpoint_missing");
            _store.ReplaceBlob(id, OperationsCheckpointCodec.Corrupt(text));
        }

        public OperationsLoopStep TryOccupy(OperationsMatchMode mode)
        {
            if (mode == OperationsMatchMode.None || mode == OperationsMatchMode.Operations)
                return OperationsLoopStep.Reject("use_launch");
            if (_store.Committed.Slot == OperationsMatchMode.Operations ||
                Phase == OperationsLoopPhase.Active ||
                Phase == OperationsLoopPhase.LaunchDispatched ||
                Phase == OperationsLoopPhase.PendingResult ||
                Phase == OperationsLoopPhase.Settled)
                return OperationsLoopStep.Reject("exclusive");
            if (_foreign != OperationsMatchMode.None && _foreign != mode)
                return OperationsLoopStep.Reject("exclusive");
            _foreign = mode;
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep ReleaseForeignMode()
        {
            if (_store.Committed.Slot == OperationsMatchMode.Operations)
                return OperationsLoopStep.Reject("exclusive");
            _foreign = OperationsMatchMode.None;
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep OpenDistrict(int number)
        {
            if (Phase != OperationsLoopPhase.Dashboard)
                return OperationsLoopStep.Reject("phase");
            if (_history.Length != 1 || _history[0] != OperationsShellNames.Operations)
                return OperationsLoopStep.Reject("navigation");
            if (number < 1 || number > OperationsIdentityRules.DistrictCount)
                return OperationsLoopStep.Reject("district");
            _selectedDistrict = number;
            _history = new[] { OperationsShellNames.Operations, OperationsShellNames.DistrictDetail };
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep OpenBriefing(string offerId)
        {
            if (Top() != OperationsShellNames.DistrictDetail)
                return OperationsLoopStep.Reject("navigation");
            if (!TryOfferById(offerId, out OperationsOfferSaveData offer))
                return OperationsLoopStep.Reject("offer");
            if (offer.districtId != OperationsIdentityRules.DistrictId(_selectedDistrict))
                return OperationsLoopStep.Reject("district");
            _selectedOfferId = offer.offerId;
            Push(OperationsShellNames.MissionBriefing);
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep OpenReport()
        {
            if (Phase != OperationsLoopPhase.Dashboard || Top() != OperationsShellNames.Operations)
                return OperationsLoopStep.Reject("navigation");
            Push(OperationsShellNames.EndOfDayReport);
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep ContinueReport()
        {
            if (Top() != OperationsShellNames.EndOfDayReport)
                return OperationsLoopStep.Reject("navigation");
            Pop();
            return OperationsLoopStep.Ok("navigation");
        }

        public OperationsLoopStep Back()
        {
            string top = Top();
            if (top == OperationsShellNames.Match && HasMission)
            {
                SetPaused(true);
                return OperationsLoopStep.Ok("navigation_pause");
            }

            if (top == OperationsShellNames.MissionResult)
            {
                if (Phase != OperationsLoopPhase.Settled)
                    return OperationsLoopStep.Reject("settlement_pending");
                OperationsLoopStep begun = BeginReturn();
                if (!begun.Accepted)
                    return begun;
                return CompleteReturn();
            }

            if (_history.Length < 2)
                return OperationsLoopStep.Reject("root");
            Pop();
            if (top == OperationsShellNames.MissionBriefing)
                _selectedOfferId = string.Empty;
            return OperationsLoopStep.Ok("navigation");
        }

        public OperationsLoopStep ActivateRoot()
        {
            string block = RootBlock();
            if (block.Length > 0)
                return OperationsLoopStep.Reject(block);
            if (Phase == OperationsLoopPhase.Settled)
            {
                OperationsLoopStep begun = BeginReturn();
                if (!begun.Accepted)
                    return begun;
                return CompleteReturn();
            }

            _history = new[] { OperationsShellNames.Operations };
            _selectedOfferId = string.Empty;
            return OperationsLoopStep.Ok("navigation");
        }

        public OperationsCommandResult RequestEndDay(string commandId)
        {
            if (Phase != OperationsLoopPhase.Dashboard || _history.Length != 1)
                return RejectCommand(commandId, OperationsReasonCode.PreconditionFailed);
            return _strategic.Submit(Command(commandId, OperationsCommandKind.EndDay, string.Empty, string.Empty));
        }

        public OperationsCommandResult BeginDeploy(string commandId) =>
            BeginAttempt(commandId, OperationsCommandKind.Deploy);

        public OperationsCommandResult BeginPractice(string commandId) =>
            BeginAttempt(commandId, OperationsCommandKind.Practice);

        public OperationsCommandResult CompleteAttempt(string commandId)
        {
            if (!_strategic.HasPendingSave)
                return RejectCommand(commandId, OperationsReasonCode.PreconditionFailed);
            return _strategic.RetrySave(commandId);
        }

        public OperationsLoopStep BeginLaunch()
        {
            if (HasPending)
                return OperationsLoopStep.Reject("pending");
            if (Phase != OperationsLoopPhase.Reserved)
                return OperationsLoopStep.Reject("phase");
            if (Slot == OperationsMatchMode.Campaign || Slot == OperationsMatchMode.Skirmish)
                return OperationsLoopStep.Reject("mode_occupied");
            if (!_strategic.TryReservedAttempt(out OperationsAttemptSaveData attempt))
                return OperationsLoopStep.Reject("reservation");
            if (!TryCatalog(attempt.missionId, out OperationsCatalogEntry entry))
                return OperationsLoopStep.Reject("catalog");
            if (!OperationsLaunchFixtures.TryCompileMission(attempt.missionId, entry.OperationMapId, out _, out string hash, out string error))
                return OperationsLoopStep.Reject(error);

            OperationsLoopDocument next = _store.Committed.Copy();
            next.Phase = OperationsLoopPhase.LaunchDispatched;
            next.Slot = OperationsMatchMode.Operations;
            next.RunId = _strategic.Save.activeRun.runId;
            next.SessionId = attempt.sessionId;
            next.OfferId = attempt.offerId;
            next.MissionId = attempt.missionId;
            next.DistrictId = attempt.districtId;
            next.MapId = entry.OperationMapId;
            next.ScenarioId = entry.ScenarioId;
            next.TransactionId = attempt.transactionId;
            next.SnapshotHash = attempt.snapshotHash;
            next.ContentHash = hash;
            next.Seed = _strategic.Save.activeRun.seed;
            next.AttemptOrdinal = attempt.attemptOrdinal;
            next.RunRevision = _strategic.Revision;
            next.DefinitionVersion = 1;
            next.Practice = attempt.practice;
            next.RefundEligible = false;
            next.BeforeMetrics = Metrics(NumberOf(attempt.districtId));
            next.AfterMetrics = string.Empty;
            next.CreditsAtLaunch = Credits;
            next.XpAtLaunch = CommanderXp;
            next.ActionPointsAtLaunch = ActionPoints;
            next.ResultText = string.Empty;
            next.ResultHash = string.Empty;
            next.History = JoinHistory();
            _store.Begin(next, false);
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep CompleteLaunch()
        {
            if (!_store.TryPending(out OperationsLoopDocument pending) || pending.Phase != OperationsLoopPhase.LaunchDispatched)
                return OperationsLoopStep.Reject("phase");
            _store.Complete();
            _foreign = OperationsMatchMode.None;
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep BeginActive(bool mapReady, bool gridReady, bool catalogReady, string announcedHash)
        {
            if (HasPending)
                return OperationsLoopStep.Reject("pending");
            if (Phase != OperationsLoopPhase.LaunchDispatched)
                return OperationsLoopStep.Reject("phase");
            if (!mapReady)
                return FailReady("readiness_map");
            if (!gridReady)
                return FailReady("readiness_grid");
            if (!catalogReady)
                return FailReady("readiness_catalog");
            if (announcedHash != _store.Committed.ContentHash)
                return FailReady("hash_mismatch");
            if (!OperationsLaunchFixtures.TryCompileMission(
                    _store.Committed.MissionId,
                    _store.Committed.MapId,
                    out _,
                    out _,
                    out string error))
                return FailReady(error);

            OperationsLoopDocument next = _store.Committed.Copy();
            next.Phase = OperationsLoopPhase.Active;
            next.RefundEligible = false;
            next.History = JoinHistory();
            _store.Begin(next, false);
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep CompleteActive()
        {
            if (!_store.TryPending(out OperationsLoopDocument pending) || pending.Phase != OperationsLoopPhase.Active)
                return OperationsLoopStep.Reject("phase");
            _store.Complete();
            Push(OperationsShellNames.Match);
            Commit(document => document.History = JoinHistory());
            Spawn(_store.Committed);
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep BeginCheckpoint()
        {
            if (HasPending || !HasMission || Phase != OperationsLoopPhase.Active)
                return OperationsLoopStep.Reject("phase");
            OperationsLoopDocument document = _store.Committed;
            _stagedCheckpointId = "ckpt" + OperationsStableIds.Hex8(OperationsStableIds.Mix(2166136261u, document.CheckpointSequence + 1));
            string text = OperationsCheckpointCodec.Write(_mission, _definition, _orders, document.SessionId, document.ContentHash, document.RestartCount);
            _store.Stage(_stagedCheckpointId, text);
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep BeginCheckpointPublish()
        {
            if (!_store.HasStaged || _store.HasPending)
                return OperationsLoopStep.Reject("checkpoint");
            OperationsLoopDocument next = _store.Committed.Copy();
            next.CheckpointSequence++;
            next.PublishedCheckpointId = _stagedCheckpointId;
            next.History = JoinHistory();
            _store.Begin(next, true);
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep CompleteCheckpointPublish()
        {
            if (!_store.HasPending)
                return OperationsLoopStep.Reject("checkpoint");
            _store.Complete();
            _resumable = true;
            _corrupt = false;
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep PublishCheckpoint()
        {
            OperationsLoopStep begun = BeginCheckpointPublish();
            if (!begun.Accepted)
                return begun;
            return CompleteCheckpointPublish();
        }

        public OperationsLoopStep RestartAttempt()
        {
            if (Phase != OperationsLoopPhase.Active || _corrupt || string.IsNullOrEmpty(SessionId))
                return OperationsLoopStep.Reject("restart");
            Commit(document =>
            {
                document.RestartCount++;
                document.PublishedCheckpointId = string.Empty;
            });
            Spawn(_store.Committed);
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep BeginResult()
        {
            if (HasPending || !HasMission || !_mission.IsTerminal || Phase != OperationsLoopPhase.Active)
                return OperationsLoopStep.Reject("result");
            OperationsMissionResult projected = OperationsMissionProjection.Project(_mission, _definition, _store.Committed);
            OperationsLoopDocument next = _store.Committed.Copy();
            next.Phase = OperationsLoopPhase.PendingResult;
            next.ResultText = OperationsMissionProjection.Encode(projected);
            next.ResultHash = projected.ResultHash;
            next.History = JoinHistory();
            _store.Begin(next, false);
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep CompleteResult()
        {
            if (!_store.TryPending(out OperationsLoopDocument pending) || pending.Phase != OperationsLoopPhase.PendingResult)
                return OperationsLoopStep.Reject("result");
            _store.Complete();
            _mission = null;
            _definition = null;
            _orders.Clear();
            Push(OperationsShellNames.MissionResult);
            Commit(document => document.History = JoinHistory());
            return OperationsLoopStep.Ok();
        }

        public OperationsCommandResult BeginSettlement(string commandId)
        {
            if (Phase != OperationsLoopPhase.PendingResult && Phase != OperationsLoopPhase.Settled)
                return RejectCommand(commandId, OperationsReasonCode.PreconditionFailed);
            if (!TryCommittedResult(out OperationsMissionResult result))
                return RejectCommand(commandId, OperationsReasonCode.PreconditionFailed);
            return _strategic.SubmitResult(Command(commandId, OperationsCommandKind.Conclude, string.Empty, string.Empty), result, true);
        }

        public OperationsCommandResult CompleteSettlement(string commandId)
        {
            if (!_strategic.HasPendingSave)
                return RejectCommand(commandId, OperationsReasonCode.PreconditionFailed);
            OperationsCommandResult result = _strategic.RetrySave(commandId);
            if (!result.Accepted)
                return result;
            // Freeze the district that this settlement just wrote so a later day or
            // mission cannot rewrite the result card.
            string after = string.Empty;
            if (!string.IsNullOrEmpty(_store.Committed.DistrictId))
                after = Metrics(NumberOf(_store.Committed.DistrictId));
            if (_store.Committed.Phase != OperationsLoopPhase.Settled ||
                string.IsNullOrEmpty(_store.Committed.AfterMetrics))
            {
                Commit(document =>
                {
                    document.Phase = OperationsLoopPhase.Settled;
                    document.RefundEligible = false;
                    if (after.Length > 0)
                        document.AfterMetrics = after;
                });
            }

            return result;
        }

        public OperationsCommandResult ProbeSettlement(string commandId, OperationsMissionResult result) =>
            _strategic.SubmitResult(Command(commandId, OperationsCommandKind.Conclude, string.Empty, string.Empty), result);

        public OperationsLoopStep BeginReturn()
        {
            if (HasPending || Phase != OperationsLoopPhase.Settled)
                return OperationsLoopStep.Reject("return");
            OperationsLoopDocument next = _store.Committed.Copy();
            next.Phase = OperationsLoopPhase.Dashboard;
            next.Slot = OperationsMatchMode.None;
            next.ReturnAcknowledged = true;
            next.PublishedCheckpointId = string.Empty;
            next.History = OperationsShellNames.Operations;
            _store.Begin(next, false);
            return OperationsLoopStep.Ok();
        }

        public OperationsLoopStep CompleteReturn()
        {
            if (!_store.TryPending(out OperationsLoopDocument pending) || !pending.ReturnAcknowledged)
                return OperationsLoopStep.Reject("return");
            _store.Complete();
            _history = new[] { OperationsShellNames.Operations };
            _selectedOfferId = string.Empty;
            _selectedDistrict = 0;
            _foreign = OperationsMatchMode.None;
            return OperationsLoopStep.Ok("navigation");
        }

        public OperationsCommandResult BeginRefund(string commandId)
        {
            if (!Offers(OperationsRecoveryChoice.RefundTechnicalFailure))
                return RejectCommand(commandId, OperationsReasonCode.PreconditionFailed);
            return _strategic.SubmitLeavingPending(Command(commandId, OperationsCommandKind.TechnicalFailure, string.Empty, string.Empty));
        }

        public OperationsCommandResult CompleteRefund(string commandId)
        {
            if (!_strategic.HasPendingSave)
                return RejectCommand(commandId, OperationsReasonCode.PreconditionFailed);
            OperationsCommandResult result = _strategic.RetrySave(commandId);
            if (!result.Accepted)
                return result;
            _history = new[] { OperationsShellNames.Operations };
            Commit(document =>
            {
                document.Phase = OperationsLoopPhase.Dashboard;
                document.Slot = OperationsMatchMode.None;
                document.RefundEligible = false;
                document.PublishedCheckpointId = string.Empty;
                document.ReturnAcknowledged = false;
                document.History = OperationsShellNames.Operations;
            });
            _mission = null;
            _corrupt = false;
            _resumable = false;
            _foreign = OperationsMatchMode.None;
            return result;
        }

        public OperationsCommandResult Withdraw(string commandId)
        {
            if (Phase == OperationsLoopPhase.PendingResult || Phase == OperationsLoopPhase.Settled || HasMission)
                return RejectCommand(commandId, OperationsReasonCode.PreconditionFailed);
            if (!Offers(OperationsRecoveryChoice.Withdraw))
                return RejectCommand(commandId, OperationsReasonCode.PreconditionFailed);
            OperationsCommandResult result = _strategic.Submit(Command(commandId, OperationsCommandKind.Withdraw, string.Empty, string.Empty));
            if (!result.Accepted)
                return result;
            _history = new[] { OperationsShellNames.Operations };
            Commit(document =>
            {
                document.Phase = OperationsLoopPhase.Dashboard;
                document.Slot = OperationsMatchMode.None;
                document.PublishedCheckpointId = string.Empty;
                document.History = OperationsShellNames.Operations;
            });
            return result;
        }

        public OperationsTacticalCommandResult Observe(string unitId, string siteId) =>
            Record(ObserveOrder, unitId, siteId, 0, () => _mission.IssueObserve(unitId, siteId));

        public OperationsTacticalCommandResult Scan(string unitId, string siteId) =>
            Record(ScanOrder, unitId, siteId, 0, () => _mission.IssueScan(unitId, siteId));

        public OperationsTacticalCommandResult Interact(string unitId, string objectId) =>
            Record(InteractOrder, unitId, objectId, 0, () => _mission.IssueInteract(unitId, objectId));

        public OperationsTacticalCommandResult Repair(string unitId, string siteId) =>
            Record(RepairOrder, unitId, siteId, 0, () => _mission.IssueRepair(unitId, siteId));

        public OperationsTacticalCommandResult Hold(string unitId, string zoneAnchorId) =>
            Record(HoldOrder, unitId, zoneAnchorId, 0, () => _mission.IssueHold(unitId, zoneAnchorId));

        public OperationsTacticalCommandResult Move(string unitId, string anchorId) =>
            Record(MoveOrder, unitId, anchorId, 0, () => _mission.IssueMove(unitId, anchorId));

        public OperationsTacticalCommandResult EscortGo(string routeId) =>
            Record(EscortOrder, routeId, string.Empty, 0, () => _mission.IssueEscortGo(routeId));

        public OperationsTacticalCommandResult EscortHold() =>
            Record(EscortHoldOrder, string.Empty, string.Empty, 0, () => _mission.IssueEscortHold());

        public OperationsTacticalCommandResult Extract(string unitId) =>
            Record(ExtractOrder, unitId, string.Empty, 0, () => _mission.IssueExtract(unitId));

        public OperationsTacticalCommandResult Attack(string unitId, string hostileId) =>
            Record(AttackOrder, unitId, hostileId, 0, () => _mission.IssueAttack(unitId, hostileId));

        public OperationsTacticalCommandResult DestroySite(string siteId) =>
            Record(DestroyOrder, siteId, string.Empty, 0, () => _mission.ReportSiteDestroyed(siteId));

        public OperationsTacticalCommandResult ConcludeMission() =>
            Record(ConcludeOrder, string.Empty, string.Empty, 0, () => _mission.IssueConclude());

        public OperationsTacticalCommandResult WithdrawMission() =>
            Record(WithdrawOrder, string.Empty, string.Empty, 0, () => _mission.IssueWithdraw());

        public void Advance(int ticks)
        {
            RequireMission();
            Remember(AdvanceOrder, string.Empty, string.Empty, ticks);
            _mission.Advance(ticks);
        }

        public void SetPaused(bool paused)
        {
            RequireMission();
            Remember(PauseOrder, string.Empty, string.Empty, paused ? 1 : 0);
            _mission.SetPaused(paused);
        }

        public bool TryMissionTick(out int tick)
        {
            if (!HasMission)
            {
                tick = -1;
                return false;
            }

            tick = _mission.Tick;
            return true;
        }

        public int MissionMaterials
        {
            get
            {
                RequireMission();
                return _mission.Materials;
            }
        }

        public bool MissionTerminal => HasMission && _mission.IsTerminal;
        public OperationsOutcomeKind MissionOutcome => HasMission ? _mission.Outcome : OperationsOutcomeKind.None;

        /// <summary>
        /// Play HUD may read IN PROGRESS only while the mission is still live.
        /// The tick that latches Victory or Defeat leaves this in that same step.
        /// </summary>
        public bool PlayHudInProgress => HasMission && !_mission.IsTerminal;

        /// <summary>
        /// Conclude affordance for Partial teach UX. True only when the tactical
        /// partial predicate is satisfied and the mission is still live.
        /// </summary>
        public bool ConcludeAvailable =>
            HasMission &&
            Phase == OperationsLoopPhase.Active &&
            !_mission.IsTerminal &&
            _mission.IsPartialPredicateSatisfied;

        public bool TryNode(string nodeId, out OperationsTacticalNodeState state)
        {
            if (!HasMission)
            {
                state = default;
                return false;
            }

            return _mission.TryGetNode(nodeId, out state);
        }

        public bool TryActor(string objectId, out OperationsTacticalActorState state)
        {
            if (!HasMission)
            {
                state = default;
                return false;
            }

            return _mission.TryGetActor(objectId, out state);
        }

        /// <summary>
        /// Public spawned actor mirror for ARIA observation. Same roster the HUD/protection counters use.
        /// </summary>
        public OperationsTacticalActorState[] CopyPublicActors()
        {
            if (!HasMission)
                return Array.Empty<OperationsTacticalActorState>();
            return _mission.CopyActors();
        }

        /// <summary>
        /// Public tactical facts emitted by legal observe/scan/interact channels.
        /// </summary>
        public OperationsTacticalFact[] CopyPublicFacts()
        {
            if (!HasMission)
                return Array.Empty<OperationsTacticalFact>();
            return _mission.CopyFacts();
        }

        /// <summary>
        /// Resolves a Move destination for a public site/unit focus to the nearest greybox anchor.
        /// </summary>
        public bool TryResolveMoveAnchor(string focusId, out string anchorId)
        {
            anchorId = string.Empty;
            if (string.IsNullOrEmpty(focusId) || !OperationsMapGreyboxCatalog.TryGet(MapId, out OperationsMapGreybox map))
                return false;
            if (map.TryGetById(focusId, out _))
            {
                anchorId = focusId;
                return true;
            }

            if (!TryActor(focusId, out OperationsTacticalActorState actor) || !actor.Spawned)
                return false;

            float best = float.MaxValue;
            string bestId = string.Empty;
            for (int index = 0; index < map.Anchors.Length; index++)
            {
                OperationsGreyboxAnchor anchor = map.Anchors[index];
                float dx = anchor.X - actor.X;
                float dz = anchor.Z - actor.Z;
                float distance = (dx * dx) + (dz * dz);
                if (distance >= best)
                    continue;
                best = distance;
                bestId = anchor.AnchorId;
            }

            if (bestId.Length == 0)
                return false;
            anchorId = bestId;
            return true;
        }

        public bool WaveArmed(int group) => HasMission && _mission.IsWaveArmed(group);
        public bool WaveSpawned(int group) => HasMission && _mission.IsWaveSpawned(group);

        public OperationsShellFrame ReadShell()
        {
            string block = RootBlock();
            string top = Top();
            bool backEnabled = true;
            string backReason = "navigation";
            if (top == OperationsShellNames.Match && HasMission)
                backReason = "navigation_pause";
            else if (top == OperationsShellNames.MissionResult && Phase != OperationsLoopPhase.Settled)
            {
                backEnabled = false;
                backReason = "settlement_pending";
            }
            else if (top == OperationsShellNames.MissionResult)
                backReason = "navigation_return";
            else if (_history.Length < 2)
            {
                backEnabled = false;
                backReason = "root";
            }

            var history = new string[_history.Length];
            Array.Copy(_history, history, _history.Length);
            return new OperationsShellFrame(
                OperationsShellNames.Operations,
                top,
                history,
                backEnabled,
                backReason,
                block.Length == 0,
                block,
                Day,
                Phase);
        }

        public bool TryReadBriefing(out OperationsBriefingFrame frame)
        {
            frame = default;
            if (Top() != OperationsShellNames.MissionBriefing || !TryOfferById(_selectedOfferId, out OperationsOfferSaveData offer))
                return false;
            if (!TryCatalog(offer.missionId, out OperationsCatalogEntry entry))
                return false;
            if (!OperationsLaunchFixtures.TryCompileMission(offer.missionId, entry.OperationMapId, out _, out string hash, out _))
                return false;
            OperationsMissionDefinitionSchema schema = entry.ToDefinitionSchema();
            string[] approaches = OperationsAuthoredMissions.IsVerticalSlice(offer.missionId)
                ? OperationsAuthoredMissions.ApproachRoutes(offer.missionId)
                : OperationsLaunchFixtures.ApproachRoutes(entry.OperationMapId);
            frame = new OperationsBriefingFrame(
                offer.missionId,
                offer.districtId,
                entry.OperationMapId,
                entry.ScenarioId,
                1,
                0,
                approaches,
                schema.DisplayNameKey,
                schema.BriefKey,
                schema.DebriefKey,
                hash,
                entry.Family);
            return true;
        }

        public bool TryReadHud(out OperationsHudFrame frame)
        {
            frame = default;
            if (!HasMission || _definition == null)
                return false;
            var required = new List<OperationsHudObjective>();
            var optional = new List<OperationsHudObjective>();
            string focus = string.Empty;
            string escort = string.Empty;
            for (int index = 0; index < _definition.Nodes.Length; index++)
            {
                OperationsCompiledNode node = _definition.Nodes[index];
                if (!_mission.TryGetNode(node.NodeId, out OperationsTacticalNodeState state))
                    continue;
                int progress = state.ProgressCount > 0 ? state.ProgressCount : state.ProgressTicks;
                if (state.TargetCount > 1)
                    progress = OperationsObjectiveChrome.ClampProgress(state.ProgressCount, state.TargetCount);
                var row = new OperationsHudObjective(
                    node.NodeId,
                    node.Optional,
                    state.Phase == OperationsTacticalNodePhase.Complete,
                    state.Phase == OperationsTacticalNodePhase.Failed,
                    progress,
                    state.Phase == OperationsTacticalNodePhase.Active);
                if (node.Optional)
                    optional.Add(row);
                else
                    required.Add(row);
                if (focus.Length == 0 && !node.Optional && state.Phase == OperationsTacticalNodePhase.Active)
                {
                    focus = FirstPublicFocus(node, state);
                    if (focus.Length == 0)
                        focus = node.ZoneAnchorId;
                }
                if (node.Rule == OperationsObjectiveRuleKind.Escort && node.TargetIds.Length > 0)
                    escort = node.TargetIds[0];
            }

            int protectedSites = 0;
            int deaths = 0;
            OperationsTacticalActorState[] actors = _mission.CopyActors();
            for (int index = 0; index < actors.Length; index++)
            {
                if (actors[index].Body == OperationsTacticalBodyKind.Site && actors[index].Alive)
                    protectedSites++;
                if (actors[index].Faction == OperationsTacticalFaction.Neutral &&
                    actors[index].Body == OperationsTacticalBodyKind.Infantry &&
                    !actors[index].Alive)
                    deaths++;
            }

            bool live = Phase == OperationsLoopPhase.Active && !_mission.IsTerminal;
            int remaining = 0;
            bool timer = _definition.DeadlineTicks > 0;
            if (timer)
            {
                remaining = _definition.DeadlineTicks - _mission.Tick;
                if (remaining < 0)
                    remaining = 0;
            }

            frame = new OperationsHudFrame(
                required.ToArray(),
                optional.ToArray(),
                timer,
                remaining,
                escort,
                protectedSites,
                deaths,
                live,
                live,
                focus,
                _mission.IsPaused);
            return true;
        }

        string FirstPublicFocus(OperationsCompiledNode node, OperationsTacticalNodeState _)
        {
            string[] targets = node.TargetIds ?? Array.Empty<string>();
            if (targets.Length == 0)
                return string.Empty;
            if (node.Rule != OperationsObjectiveRuleKind.Scan)
                return targets[0];

            OperationsTacticalFact[] facts = _mission.CopyFacts();
            for (int index = 0; index < targets.Length; index++)
            {
                if (!HasFact(facts, OperationsTacticalFactKind.ScanConfirmed, targets[index]))
                    return targets[index];
            }

            return targets[0];
        }

        static bool HasFact(OperationsTacticalFact[] facts, OperationsTacticalFactKind kind, string objectId)
        {
            for (int index = 0; index < facts.Length; index++)
            {
                if (facts[index].Kind == kind && facts[index].ObjectId == objectId)
                    return true;
            }

            return false;
        }

        public bool TryReadResult(out OperationsResultFrame frame)
        {
            frame = default;
            if (!TryCommittedResult(out OperationsMissionResult result))
                return false;
            bool applied = Phase == OperationsLoopPhase.Settled || Phase == OperationsLoopPhase.Dashboard && _store.Committed.ReturnAcknowledged;
            int credits = applied ? Credits - _store.Committed.CreditsAtLaunch : 0;
            int xp = applied ? CommanderXp - _store.Committed.XpAtLaunch : 0;
            if (credits < 0)
                credits = 0;
            if (xp < 0)
                xp = 0;
            var achieved = new List<OperationsObjectiveFact>();
            var missed = new List<OperationsObjectiveFact>();
            for (int index = 0; index < result.MandatoryObjectiveFacts.Length; index++)
            {
                if (result.MandatoryObjectiveFacts[index].Completed)
                    achieved.Add(result.MandatoryObjectiveFacts[index]);
                else
                    missed.Add(result.MandatoryObjectiveFacts[index]);
            }

            OperationsOfferSaveData[] offers = _strategic.Offers;
            var next = new string[offers.Length];
            for (int index = 0; index < offers.Length; index++)
                next[index] = offers[index].missionId;
            string afterText = _store.Committed.AfterMetrics;
            if (string.IsNullOrEmpty(afterText))
                afterText = Metrics(NumberOf(_store.Committed.DistrictId));
            frame = new OperationsResultFrame(
                result.Outcome,
                result.TerminalReason,
                _store.Committed.DistrictId,
                ParseMetrics(_store.Committed.BeforeMetrics),
                ParseMetrics(afterText),
                result.CivilianDeaths,
                result.TaskForceLosses,
                credits,
                xp,
                applied,
                next,
                result.ResultHash,
                0,
                achieved.ToArray(),
                missed.ToArray());
            return true;
        }

        OperationsCommandResult BeginAttempt(string commandId, OperationsCommandKind kind)
        {
            if (Top() != OperationsShellNames.MissionBriefing || !TryOfferById(_selectedOfferId, out OperationsOfferSaveData offer))
                return RejectCommand(commandId, OperationsReasonCode.PreconditionFailed);
            return _strategic.SubmitLeavingPending(Command(commandId, kind, offer.districtId, offer.offerId));
        }

        OperationsLoopStep FailReady(string reason)
        {
            Commit(document => document.RefundEligible = true);
            return OperationsLoopStep.Reject(reason);
        }

        void Spawn(OperationsLoopDocument document)
        {
            if (!OperationsLaunchFixtures.TryCompileMission(
                    document.MissionId,
                    document.MapId,
                    out _definition,
                    out _,
                    out string error))
                throw new InvalidOperationException(error);
            _mission = new OperationsTacticalSession(_definition, Payload(document));
            _orders.Clear();
            _corrupt = false;
            _resumable = false;
        }

        void TryRestore()
        {
            _corrupt = false;
            _resumable = false;
            _mission = null;
            OperationsLoopDocument document = _store.Committed;
            if (document.Phase != OperationsLoopPhase.Active || string.IsNullOrEmpty(document.PublishedCheckpointId))
                return;
            if (!_store.TryBlob(document.PublishedCheckpointId, out string text) ||
                !OperationsCheckpointCodec.TryRead(text, out OperationsCheckpointImage image, out _) ||
                image.SessionId != document.SessionId ||
                image.ContentHash != document.ContentHash)
            {
                _corrupt = true;
                return;
            }

            Spawn(document);
            _replaying = true;
            for (int index = 0; index < image.Orders.Count; index++)
                Apply(image.Orders[index]);
            _replaying = false;
            _orders.Clear();
            _orders.AddRange(image.Orders);
            string rebuilt = OperationsCheckpointCodec.Write(_mission, _definition, _orders, document.SessionId, document.ContentHash, document.RestartCount);
            if (!OperationsCheckpointCodec.TryRead(rebuilt, out OperationsCheckpointImage check, out _) || check.Checksum != image.Checksum)
            {
                _mission = null;
                _definition = null;
                _orders.Clear();
                _corrupt = true;
                return;
            }

            _resumable = true;
        }

        void Apply(OperationsLoopOrder order)
        {
            switch (order.Kind)
            {
                case AdvanceOrder: _mission.Advance(order.N); break;
                case PauseOrder: _mission.SetPaused(order.N == 1); break;
                case ObserveOrder: _mission.IssueObserve(order.A, order.B); break;
                case ScanOrder: _mission.IssueScan(order.A, order.B); break;
                case InteractOrder: _mission.IssueInteract(order.A, order.B); break;
                case RepairOrder: _mission.IssueRepair(order.A, order.B); break;
                case HoldOrder: _mission.IssueHold(order.A, order.B); break;
                case MoveOrder: _mission.IssueMove(order.A, order.B); break;
                case EscortOrder: _mission.IssueEscortGo(order.A); break;
                case EscortHoldOrder: _mission.IssueEscortHold(); break;
                case ExtractOrder: _mission.IssueExtract(order.A); break;
                case AttackOrder: _mission.IssueAttack(order.A, order.B); break;
                case DestroyOrder: _mission.ReportSiteDestroyed(order.A); break;
                case ConcludeOrder: _mission.IssueConclude(); break;
                case WithdrawOrder: _mission.IssueWithdraw(); break;
                default: throw new InvalidOperationException("order");
            }
        }

        OperationsTacticalCommandResult Record(byte kind, string a, string b, int n, Func<OperationsTacticalCommandResult> call)
        {
            RequireMission();
            Remember(kind, a, b, n);
            return call();
        }

        void Remember(byte kind, string a, string b, int n)
        {
            if (_replaying)
                return;
            _orders.Add(new OperationsLoopOrder(kind, a, b, n));
        }

        void RequireMission()
        {
            if (!HasMission)
                throw new InvalidOperationException("no_mission");
        }

        OperationsLaunchPayload Payload(OperationsLoopDocument document)
        {
            return new OperationsLaunchPayload(
                OperationsIdentityRules.CurrentSchemaVersion,
                document.RunId,
                document.DistrictId,
                document.OfferId,
                document.MissionId,
                document.ScenarioId,
                document.MapId,
                document.RunRevision,
                document.DefinitionVersion,
                document.ContentHash,
                _strategic.Save.activeRun.difficulty,
                document.Seed,
                document.TransactionId,
                document.SessionId,
                document.AttemptOrdinal,
                document.SnapshotHash,
                document.Practice);
        }

        bool ReceiptMatches(OperationsLoopDocument document)
        {
            if (string.IsNullOrEmpty(document.ResultHash))
                return false;
            string key = document.RunId + "|" + document.OfferId + "|" + document.SessionId + "|" + document.AttemptOrdinal;
            OperationsReceiptSaveData[] receipts = _strategic.Save.receipts ?? Array.Empty<OperationsReceiptSaveData>();
            for (int index = 0; index < receipts.Length; index++)
            {
                if (receipts[index].settlementKey == key && receipts[index].resultHash == document.ResultHash)
                    return true;
            }

            return false;
        }

        void Commit(Action<OperationsLoopDocument> edit)
        {
            if (_store.HasPending)
                throw new InvalidOperationException("loop_pending");
            OperationsLoopDocument next = _store.Committed.Copy();
            edit(next);
            _store.Begin(next, false);
            _store.Complete();
        }

        void LoadShell()
        {
            string history = _store.Committed.History;
            _history = string.IsNullOrEmpty(history)
                ? new[] { OperationsShellNames.Operations }
                : history.Split('|');
        }

        string JoinHistory() => string.Join("|", _history);

        string Top() => _history.Length == 0 ? string.Empty : _history[_history.Length - 1];

        void Push(string route)
        {
            if (Top() == route)
                return;
            var grown = new string[_history.Length + 1];
            Array.Copy(_history, grown, _history.Length);
            grown[_history.Length] = route;
            _history = grown;
        }

        void Pop()
        {
            if (_history.Length < 2)
                return;
            var shrunk = new string[_history.Length - 1];
            Array.Copy(_history, shrunk, shrunk.Length);
            _history = shrunk;
        }

        string RootBlock()
        {
            if (Phase == OperationsLoopPhase.Active)
                return "active_attempt";
            if (Phase == OperationsLoopPhase.PendingResult)
                return "settlement_pending";
            if (Phase == OperationsLoopPhase.Reserved || Phase == OperationsLoopPhase.LaunchDispatched)
                return "reserved_attempt";
            return string.Empty;
        }

        OperationsCommand Command(string commandId, OperationsCommandKind kind, string districtId, string offerId) =>
            new(commandId, _strategic.Revision, kind, districtId, offerId, string.Empty);

        OperationsCommandResult RejectCommand(string commandId, OperationsReasonCode reason) =>
            new(commandId, false, reason, _strategic.Revision, string.Empty);

        bool TryOfferById(string offerId, out OperationsOfferSaveData offer)
        {
            OperationsOfferSaveData[] offers = _strategic.Offers;
            for (int index = 0; index < offers.Length; index++)
            {
                if (offers[index].offerId == offerId)
                {
                    offer = offers[index];
                    return true;
                }
            }

            offer = null;
            return false;
        }

        static bool TryCatalog(string missionId, out OperationsCatalogEntry entry)
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

        string Metrics(int number)
        {
            OperationsDistrictComponent district = _strategic.GetDistrict(number);
            return district.Security + "," + district.Trust + "," + district.Infrastructure + "," +
                   district.EnemyInfluence + "," + district.IntelConfidence + "," + district.Heat + "," +
                   district.SupplyReadiness;
        }

        static int[] ParseMetrics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<int>();
            string[] parts = text.Split(',');
            var values = new int[parts.Length];
            for (int index = 0; index < parts.Length; index++)
                values[index] = int.Parse(parts[index]);
            return values;
        }

        static int NumberOf(string districtId)
        {
            for (int number = 1; number <= OperationsIdentityRules.DistrictCount; number++)
            {
                if (OperationsIdentityRules.DistrictId(number) == districtId)
                    return number;
            }

            throw new InvalidOperationException(districtId);
        }
    }
}
