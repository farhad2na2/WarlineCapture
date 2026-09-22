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

        /// <summary>
        /// Projects expanded session phase and objective-clock elapsed onto SkirmishMatchState.
        /// Expanded matches skip SkirmishRulesSystem, so this is the live elapsed source.
        /// </summary>
        public static void ProjectMatchPhase(EntityManager em, Entity session)
        {
            if (!em.HasComponent<SkirmishMatchState>(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return;
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var match = em.GetComponentData<SkirmishMatchState>(session);
            bool changed = false;
            if (state.Phase == SkirmishSessionPhase.Playing && match.Phase < SkirmishPhase.Playing)
            {
                match.Phase = SkirmishPhase.Playing;
                changed = true;
            }
            else if (state.Phase == SkirmishSessionPhase.Finished && match.Phase != SkirmishPhase.Finished)
            {
                match.Phase = SkirmishPhase.Finished;
                changed = true;
            }

            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {
                float elapsed = em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds;
                if (match.ElapsedSeconds != elapsed)
                {
                    match.ElapsedSeconds = elapsed;
                    changed = true;
                }
            }

            if (changed)
                em.SetComponentData(session, match);
        }

        /// <summary>
        /// Copies a frozen expanded result onto SkirmishMatchState.
        /// SkirmishRulesSystem does not do this for expanded sessions.
        /// </summary>
        public static void ProjectTerminalMatch(EntityManager em, Entity session)
        {
            if (!em.HasComponent<SkirmishMatchState>(session) ||
                !em.HasComponent<SkirmishResultComponent>(session))
                return;
            var result = em.GetComponentData<SkirmishResultComponent>(session);
            if (result.Frozen == 0)
                return;
            var match = em.GetComponentData<SkirmishMatchState>(session);
            match.Phase = SkirmishPhase.Finished;
            match.Outcome = result.Outcome == SkirmishOutcomeKind.Victory
                ? SkirmishOutcome.Victory
                : result.Outcome == SkirmishOutcomeKind.Defeat
                    ? SkirmishOutcome.Defeat
                    : result.Outcome == SkirmishOutcomeKind.Draw
                        ? SkirmishOutcome.Draw
                        : SkirmishOutcome.None;
            match.Reason = result.Reason == SkirmishEndReasonKind.Surrender
                ? SkirmishEndReason.Surrender
                : result.Reason == SkirmishEndReasonKind.TimeLimit
                    ? SkirmishEndReason.TimeLimit
                    : result.Reason == SkirmishEndReasonKind.BothBasesDestroyed
                        ? SkirmishEndReason.BothBasesDestroyed
                        : result.Reason == SkirmishEndReasonKind.MainBaseDestroyed
                            ? SkirmishEndReason.MainBaseDestroyed
                            : SkirmishEndReason.None;
            if (result.SaveAcknowledged != 0)
                match.ResultSaved = 1;
            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
                match.ElapsedSeconds = em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds;
            em.SetComponentData(session, match);
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
