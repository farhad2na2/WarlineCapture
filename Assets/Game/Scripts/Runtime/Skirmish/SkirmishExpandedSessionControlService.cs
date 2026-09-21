using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishExpandedSessionControlService
    {
        public static bool TryPause(EntityManager em, Entity session, out SkirmishCheckpointDocument document)
        {
            document = null;
            if (!IsExpandedPlaying(em, session))
                return false;
            SetPaused(em, session, true);
            return SkirmishCheckpointService.TryCapture(em, session, out document);
        }

        public static bool TryResume(EntityManager em, Entity session)
        {
            if (!IsExpanded(em, session))
                return false;
            SetPaused(em, session, false);
            return true;
        }

        public static bool TrySettle(EntityManager em, Entity session, SkirmishExpandedResultJournal journal = null)
        {
            return SkirmishResultSettlementService.TrySettleSession(
                em, session, journal ?? SkirmishResultSettlementService.Shared, out _, out _);
        }

        public static void RequestReplay(EntityManager em, Entity session)
        {
            if (!IsExpanded(em, session))
                return;
            if (em.HasComponent<SkirmishExpandedReplayRequest>(session))
            {
                var request = em.GetComponentData<SkirmishExpandedReplayRequest>(session);
                request.Requested = 1;
                em.SetComponentData(session, request);
                return;
            }

            em.AddComponentData(session, new SkirmishExpandedReplayRequest { Requested = 1 });
        }

        public static bool IsExpanded(EntityManager em, Entity session)
        {
            return em.HasComponent<SkirmishExpandedSessionComponent>(session) &&
                   em.GetComponentData<SkirmishExpandedSessionComponent>(session).IsLegacy == 0;
        }

        private static bool IsExpandedPlaying(EntityManager em, Entity session)
        {
            if (!IsExpanded(em, session))
                return false;
            if (em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase ==
                SkirmishSessionPhase.Playing)
                return true;
            return em.HasComponent<SkirmishObjectiveClockComponent>(session) &&
                   em.GetComponentData<SkirmishObjectiveClockComponent>(session).Playing != 0;
        }

        private static void SetPaused(EntityManager em, Entity session, bool paused)
        {
            if (!em.HasComponent<SkirmishObjectiveClockComponent>(session))
                return;
            var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            clock.Paused = (byte)(paused ? 1 : 0);
            em.SetComponentData(session, clock);
        }
    }
}
