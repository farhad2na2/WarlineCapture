using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Composition
{
    public static class SkirmishCheckpointCompositionSystemHelper
    {
        public static bool TryRestoreFreshSession(
            EntityManager em,
            SkirmishCheckpointDocument document,
            SkirmishExpansionAuthoredSet authored,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            SkirmishContentManifest manifest,
            out Entity session,
            out SkirmishReasonCode reason)
        {
            session = Entity.Null;
            reason = SkirmishReasonCode.IncompatibleSave;
            if (document?.Header == null || document.Payload == null || authored == null)
                return false;

            if (document.Payload.PhysicalSupplyInitialized != 0)
            {
                reason = SkirmishReasonCode.UnsupportedCapability;
                return false;
            }

            if (!SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                    em,
                    document.Header.CatalogId,
                    document.Payload.DifficultyId,
                    document.Payload.SizeId,
                    document.Payload.Seed,
                    authored,
                    matrix,
                    manifest,
                    out SkirmishResolvedSetup setup,
                    out SkirmishLaunchPayload payload,
                    out _))
                return false;

            if (!SkirmishCheckpointCodec.IsCompatible(document.Header, setup))
                return false;

            payload.SessionId = document.Header.SessionId;
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            state.SessionId = new Unity.Collections.FixedString64Bytes(document.Header.SessionId);
            em.SetComponentData(session, state);
            if (em.HasComponent<SkirmishMatchState>(session))
            {
                var match = em.GetComponentData<SkirmishMatchState>(session);
                match.SessionId = state.SessionId;
                em.SetComponentData(session, match);
            }

            if (!SkirmishScenarioSpawnSystem.TrySpawnLedgers(em, session, setup, out reason, out _))
                return false;
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(em, owned, state.SessionId, setup);
            if (!em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {
                em.AddComponentData(session, new SkirmishObjectiveClockComponent
                {
                    DeadlineSeconds = setup.DeadlineSeconds,
                    Playing = 1
                });
            }

            if (!SkirmishCheckpointService.TryApply(em, session, document, out reason))
                return false;

            if (em.HasComponent<SkirmishCheckpointRequestComponent>(session))
            {
                var request = em.GetComponentData<SkirmishCheckpointRequestComponent>(session);
                request.Status = SkirmishCheckpointStatus.Restored;
                em.SetComponentData(session, request);
            }
            else
            {
                em.AddComponentData(session, new SkirmishCheckpointRequestComponent
                {
                    Status = SkirmishCheckpointStatus.Restored,
                    SimulationTick = document.Header.SimulationTick
                });
            }

            reason = SkirmishReasonCode.None;
            return true;
        }

        public static bool TryQueueFreshReplay(
            EntityManager em,
            SkirmishReplayRequest request,
            SkirmishExpansionAuthoredSet authored,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            SkirmishContentManifest manifest,
            out SkirmishLaunchPayload payload,
            out SkirmishResolvedSetup setup,
            out SkirmishReasonCode reason)
        {
            payload = null;
            setup = null;
            reason = SkirmishReasonCode.IncompatibleSave;
            if (request == null || request.Mode != SkirmishReplayMode.FreshStart)
                return false;

            if (!SkirmishSetupCompiler.TryCompile(
                    authored.DefinitionS002,
                    request.DifficultyId,
                    request.SizeId,
                    request.Seed,
                    manifest,
                    matrix,
                    out setup,
                    out _))
                return false;

            payload = SkirmishReplayService.CreateFreshLaunch(request, setup);
            if (payload.SessionId == request.SourceSessionId)
                return false;
            if (!SkirmishExpandedLaunchProjection.TryQueue(em, payload, setup))
                return false;
            reason = SkirmishReasonCode.None;
            return true;
        }

        public static bool TryReplayLive(EntityManager em, Entity session, out string newSessionId)
        {
            newSessionId = string.Empty;
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup == null)
                return false;

            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var replay = new SkirmishReplayRequest
            {
                SourceSessionId = state.SessionId.ToString(),
                CatalogId = state.CatalogId.ToString(),
                DifficultyId = state.DifficultyId,
                SizeId = state.SizeId,
                Seed = state.Seed,
                Mode = SkirmishReplayMode.FreshStart
            };
            SkirmishLaunchPayload payload = SkirmishReplayService.CreateFreshLaunch(replay, setup);
            SkirmishStartingBuildingsService.Cancel(em, session);
            SkirmishScenarioSpawnSystem.DestroyAttemptOwned(em, state.SessionId);
            ResetExpandedSessionOwned(em, session);
            if (em.HasComponent<SkirmishVisualPrefabCatalogRecord>(session))
            {
                em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(session).Catalog?.Dispose();
                em.RemoveComponent<SkirmishVisualPrefabCatalogRecord>(session);
            }

            if (em.HasComponent<SkirmishExpandedSessionComponent>(session))
                em.RemoveComponent<SkirmishExpandedSessionComponent>(session);

            if (!SkirmishExpandedLaunchProjection.TryQueue(em, payload, setup))
                return false;
            em.AddComponent<SkirmishExpandedReplayStartPending>(session);
            SkirmishLaunchProjection.TryRequestPlay(em);
            newSessionId = payload.SessionId;
            return newSessionId != replay.SourceSessionId;
        }

        public static bool TryConsumeExpandedReplay(
            EntityManager em,
            Entity session,
            SkirmishAction action)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session))
                return false;
            if (action != SkirmishAction.Replay && action != SkirmishAction.Restart)
                return false;
            TryReplayLive(em, session, out _);
            return true;
        }

        public static bool TryConsumeExpandedReplayRequest(EntityManager em, Entity session)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session))
                return false;
            if (!em.HasComponent<SkirmishExpandedReplayRequest>(session) ||
                em.GetComponentData<SkirmishExpandedReplayRequest>(session).Requested == 0)
                return false;
            return TryReplayLive(em, session, out _);
        }

        public static bool TrySettleExpandedPresentation(EntityManager em, Entity session)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session))
                return false;
            if (em.HasComponent<SkirmishResultComponent>(session) &&
                em.GetComponentData<SkirmishResultComponent>(session).Frozen != 0 &&
                em.GetComponentData<SkirmishResultComponent>(session).SaveAcknowledged == 0)
            {
                SkirmishExpandedSessionControlService.TrySettle(em, session);
            }

            return true;
        }

        private static void ResetExpandedSessionOwned(EntityManager em, Entity session)
        {
            RemoveIfPresent<SkirmishExpandedReplayStartPending>(em, session);
            if (em.HasBuffer<SkirmishTrackedUnit>(session))
                em.GetBuffer<SkirmishTrackedUnit>(session).Clear();
            RemoveIfPresent<SkirmishSharedMaterialsInitialized>(em, session);
            RemoveIfPresent<SkirmishSharedSupplyInitialized>(em, session);
            RemoveIfPresent<SkirmishVehicleFuelRemainderComponent>(em, session);
            RemoveIfPresent<SkirmishSharedBuildingsReady>(em, session);
            RemoveIfPresent<BuildingStartingGrantOwner>(em, session);
            if (em.HasBuffer<SkirmishStartingBuildingRequest>(session))
                em.RemoveComponent<SkirmishStartingBuildingRequest>(session);
            if (em.HasBuffer<SkirmishReadinessRequest>(session))
                em.GetBuffer<SkirmishReadinessRequest>(session).Clear();
            RemoveIfPresent<SkirmishEconomyStockComponent>(em, session);
            RemoveIfPresent<SkirmishCapacityComponent>(em, session);
            RemoveIfPresent<SkirmishEnemyStockComponent>(em, session);
            RemoveIfPresent<SkirmishEnemyCapacityComponent>(em, session);
            RemoveIfPresent<SkirmishEnemyStrategyComponent>(em, session);
            RemoveIfPresent<SkirmishObjectiveClockComponent>(em, session);
            RemoveIfPresent<SkirmishBaseAssaultFactComponent>(em, session);
            RemoveIfPresent<SkirmishObjectiveStateComponent>(em, session);
            RemoveIfPresent<SkirmishResultComponent>(em, session);
            RemoveIfPresent<SkirmishArmySelectionComponent>(em, session);
            RemoveIfPresent<SkirmishFogStateComponent>(em, session);
            RemoveIfPresent<SkirmishCheckpointRequestComponent>(em, session);
            RemoveIfPresent<SkirmishExpandedPauseRequest>(em, session);
            RemoveIfPresent<SkirmishExpandedReplayRequest>(em, session);
            RemoveIfPresent<SkirmishResolvedSetupComponent>(em, session);
            RemoveIfPresent<SkirmishResearchStateComponent>(em, session);
            if (em.HasBuffer<SkirmishResearchQueueItem>(session))
                em.GetBuffer<SkirmishResearchQueueItem>(session).Clear();
            if (em.HasBuffer<SkirmishProductionReservation>(session))
                em.GetBuffer<SkirmishProductionReservation>(session).Clear();
            if (em.HasBuffer<SkirmishArmyGroupRecord>(session))
                em.GetBuffer<SkirmishArmyGroupRecord>(session).Clear();
            if (em.HasBuffer<SkirmishArmyDrawerSlot>(session))
                em.GetBuffer<SkirmishArmyDrawerSlot>(session).Clear();
            if (em.HasComponent<SkirmishCheckpointDocumentRecord>(session))
                em.RemoveComponent<SkirmishCheckpointDocumentRecord>(session);
        }

        private static void RemoveIfPresent<T>(EntityManager em, Entity session) where T : unmanaged, IComponentData
        {
            if (em.HasComponent<T>(session))
                em.RemoveComponent<T>(session);
        }
    }
}
