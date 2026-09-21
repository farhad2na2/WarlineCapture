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
    }
}
