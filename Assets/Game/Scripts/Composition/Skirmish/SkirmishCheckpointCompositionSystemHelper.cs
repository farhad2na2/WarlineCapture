using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Composition
{
    public sealed class SkirmishReplayLaunchRecord : IComponentData
    {
        public SkirmishLaunchPayload Payload;
        public SkirmishResolvedSetup Setup;
    }

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

            // Compatibility is a read-only gate. Do not queue a scene/session or
            // replace another attempt before the saved setup has been checked.
            using var existingSessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if (existingSessions.CalculateEntityCount() != 0 ||
                document.Payload.SessionId != document.Header.SessionId ||
                document.Payload.CatalogId != document.Header.CatalogId ||
                document.Payload.DefinitionId != document.Header.DefinitionId ||
                document.Payload.SetupHash != document.Header.SetupHash ||
                document.Payload.ContentVersion != document.Header.ContentVersion ||
                !SkirmishExpansionCatalogFactory.TryGetDefinition(authored, document.Header.CatalogId, document.Header.ContentVersion, out var definition) ||
                !SkirmishSetupCompiler.TryCompile(definition, document.Payload.DifficultyId,
                    document.Payload.SizeId, document.Payload.Seed, manifest, matrix, out var checkedSetup, out _) ||
                !SkirmishCheckpointCodec.IsCompatible(document.Header, checkedSetup))
                return false;

            // Old fixture documents cannot reconstruct the versioned native enemy planner.
            if (checkedSetup.EnemyPolicy != null) { reason = SkirmishReasonCode.UnsupportedCapability; return false; }

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
                    out _, contentVersion: document.Header.ContentVersion))
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
            if (request == null || request.Mode != SkirmishReplayMode.FreshStart || request.ContentVersion < 0)
                return false;
            SkirmishScenarioDefinitionConfig definition;
            bool found = request.ContentVersion > 0
                ? SkirmishExpansionCatalogFactory.TryGetDefinition(authored, request.CatalogId, request.ContentVersion, out definition)
                : SkirmishExpansionCatalogFactory.TryGetDefinition(authored, request.CatalogId, out definition);
            if (!found) return false;

            if (!SkirmishSetupCompiler.TryCompile(
                    definition,
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
            // In-place replay is only safe for ledger-only worlds. Native buildings,
            // stores, wrecks and source-scene state require the match unload boundary.
            if (em.HasComponent<SkirmishSharedSupplyInitialized>(session) ||
                !SkirmishExpandedSessionControlService.IsExpanded(em, session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup == null)
                return false;

            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var replay = new SkirmishReplayRequest
            {
                SourceSessionId = state.SessionId.ToString(),
                CatalogId = state.CatalogId.ToString(),
                ContentVersion = state.ContentVersion,
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
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(session)) return false;
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (setup == null || !em.HasComponent<SkirmishMatchState>(session)) return false;
            using var pending = em.CreateEntityQuery(typeof(SkirmishReturnRequest));
            if (!pending.IsEmptyIgnoreFilter) return true;
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var match = em.GetComponentData<SkirmishMatchState>(session);
            var payload = SkirmishReplayService.CreateFreshLaunch(new SkirmishReplayRequest
            {
                SourceSessionId = state.SessionId.ToString(), CatalogId = state.CatalogId.ToString(),
                ContentVersion = state.ContentVersion,
                DifficultyId = state.DifficultyId, SizeId = state.SizeId, Seed = state.Seed,
                Mode = SkirmishReplayMode.FreshStart
            }, setup);
            var request = em.CreateEntity(typeof(SkirmishReturnRequest));
            em.SetComponentData(request, new SkirmishReturnRequest
                { Action = action, Seed = match.Seed, ScenarioIndex = match.ScenarioIndex });
            em.AddComponentObject(request, new SkirmishReplayLaunchRecord { Payload = payload, Setup = setup });
            return true;
        }

        public static bool TryConsumeExpandedReplayRequest(EntityManager em, Entity session)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session))
                return false;
            if (!em.HasComponent<SkirmishExpandedReplayRequest>(session) ||
                em.GetComponentData<SkirmishExpandedReplayRequest>(session).Requested == 0)
                return false;
            return TryConsumeExpandedReplay(em, session, SkirmishAction.Replay);
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
            RemoveIfPresent<SkirmishEnemyPlannerV1Component>(em, session);
            RemoveIfPresent<SkirmishEnemyProductionIntentV1Component>(em, session);
            if (em.HasBuffer<SkirmishEnemyGroupAssignmentV1>(session))
                em.RemoveComponent<SkirmishEnemyGroupAssignmentV1>(session);
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
            RemoveIfPresent<SkirmishEnemyResearchStateComponent>(em, session);
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
