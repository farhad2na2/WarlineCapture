using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    public sealed class SkirmishExpandedResultJournal
    {
        private readonly Dictionary<string, SkirmishResultReceipt> receipts =
            new Dictionary<string, SkirmishResultReceipt>();

        public static string Key(
            string definitionId,
            int contentVersion,
            SkirmishDifficultyId difficulty,
            SkirmishSizeId size,
            string sessionId)
        {
            return (definitionId ?? string.Empty) + "|" + contentVersion + "|" +
                   (byte)difficulty + "|" + (byte)size + "|" + (sessionId ?? string.Empty);
        }

        public bool TrySettle(SkirmishResultReceipt incoming, out SkirmishResultReceipt stored, out SkirmishReasonCode reason)
        {
            stored = null;
            reason = SkirmishReasonCode.IncompatibleSave;
            if (incoming == null)
                return false;
            if (incoming.IsLegacy != 0 || incoming.IsCustom != 0)
            {
                reason = SkirmishReasonCode.UnsupportedCapability;
                return false;
            }

            if (incoming.Outcome == SkirmishOutcomeKind.None ||
                string.IsNullOrEmpty(incoming.SessionId) ||
                string.IsNullOrEmpty(incoming.DefinitionId))
                return false;

            string key = Key(incoming.DefinitionId, incoming.ContentVersion, incoming.DifficultyId, incoming.SizeId,
                incoming.SessionId);
            if (receipts.TryGetValue(key, out SkirmishResultReceipt existing))
            {
                if (existing.Outcome == incoming.Outcome && existing.Reason == incoming.Reason)
                {
                    stored = existing;
                    reason = SkirmishReasonCode.None;
                    return true;
                }

                reason = SkirmishReasonCode.DuplicateId;
                stored = existing;
                return false;
            }

            incoming.Settled = 1;
            receipts[key] = incoming;
            stored = incoming;
            reason = SkirmishReasonCode.None;
            return true;
        }

        public bool TryGet(string sessionId, out SkirmishResultReceipt receipt)
        {
            foreach (KeyValuePair<string, SkirmishResultReceipt> pair in receipts)
            {
                if (pair.Value.SessionId == sessionId)
                {
                    receipt = pair.Value;
                    return true;
                }
            }

            receipt = null;
            return false;
        }
    }

    public static class SkirmishResultSettlementService
    {
        public static readonly SkirmishExpandedResultJournal Shared = new SkirmishExpandedResultJournal();

        public static bool TrySettleSession(
            EntityManager em,
            Entity session,
            SkirmishExpandedResultJournal journal,
            out SkirmishResultReceipt receipt,
            out SkirmishReasonCode reason)
        {
            receipt = null;
            reason = SkirmishReasonCode.IncompatibleSave;
            if (em == default || session == Entity.Null ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishResultComponent>(session))
                return false;

            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var result = em.GetComponentData<SkirmishResultComponent>(session);
            if (result.Frozen == 0)
                return false;

            float elapsed = 0f;
            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
                elapsed = em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds;

            var incoming = new SkirmishResultReceipt
            {
                SessionId = state.SessionId.ToString(),
                CatalogId = state.CatalogId.ToString(),
                DefinitionId = state.DefinitionId.ToString(),
                ContentVersion = state.ContentVersion,
                DifficultyId = state.DifficultyId,
                SizeId = state.SizeId,
                SetupHash = state.SetupHash,
                Outcome = result.Outcome,
                Reason = result.Reason,
                ElapsedSeconds = elapsed,
                Seed = state.Seed,
                IsCustom = state.IsCustom,
                IsLegacy = state.IsLegacy
            };

            journal ??= Shared;
            if (!journal.TrySettle(incoming, out receipt, out reason))
                return false;

            result.SaveAcknowledged = 1;
            em.SetComponentData(session, result);
            return true;
        }
    }

    public static class SkirmishReplayService
    {
        public static SkirmishLaunchPayload CreateFreshLaunch(SkirmishReplayRequest request, SkirmishResolvedSetup setup)
        {
            return new SkirmishLaunchPayload
            {
                SessionId = "replay-" + (request?.CatalogId ?? "s002") + "-" + (request?.Seed ?? 0) + "-" +
                            System.Guid.NewGuid().ToString("N").Substring(0, 8),
                CatalogId = setup.CatalogId,
                DefinitionId = setup.DefinitionId,
                ContentVersion = setup.ContentVersion,
                AllConfigHashes = setup.SetupHash.ToString("X8"),
                MapId = setup.OperationMapId,
                LayoutId = setup.LayoutId,
                DifficultyId = request != null ? request.DifficultyId : setup.DifficultyId,
                SizeId = request != null ? request.SizeId : setup.SizeId,
                Seed = request != null ? request.Seed : setup.Seed,
                IsCustom = false,
                IsLegacy = false
            };
        }
    }
}
