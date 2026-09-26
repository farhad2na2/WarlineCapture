using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Burst;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    /// <summary>Campaign instruction skill: acts only on currently rendered guidance observations.</summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(AriaPlayInputSystem))]
    public partial struct AriaPlayDecisionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (observation, session) in SystemAPI.Query<RefRO<AriaPlayObservationComponent>, RefRW<AriaPlaySessionComponent>>())
                Step(observation.ValueRO, ref session.ValueRW);
        }

        public static void Step(in AriaPlayObservationComponent observation, ref AriaPlaySessionComponent session)
        {
            if (session.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked or AriaPlayPhase.Starting) return;
            if (observation.Kind == AriaPlayObservationKind.Finished) { session.Phase = AriaPlayPhase.Manual; return; }
            float now = observation.Time;
            // A changing control is not objective progress. Bound cycles such as
            // Select -> Move -> Select even when each control gets fresh retries.
            ulong goalBit=1UL << (observation.GoalId & 63);
            if (session.ObjectiveWatchdogInitialized == 0 || (session.VisitedGoalMask & goalBit) == 0)
            {
                session.ObjectiveWatchdogInitialized = 1;
                session.VisitedGoalMask |= goalBit;
                session.LastObjectiveProgressAt = now;
            }
            if (session.Phase != AriaPlayPhase.Touching && now - session.LastObjectiveProgressAt > 180f)
            {
                session.Phase = AriaPlayPhase.Blocked;
                session.GestureRequested = 0;
                return;
            }
            if (observation.GoalId != session.GoalId)
            {
                session.GoalId = observation.GoalId; session.Attempts = 0;
                if (session.Phase != AriaPlayPhase.Touching) session.Phase = AriaPlayPhase.Observing;
                session.LastProgressAt = now;
            }
            if (session.Phase == AriaPlayPhase.Touching) return;
            if (observation.Kind == AriaPlayObservationKind.Cinematic)
            {
                session.Phase = now - session.LastProgressAt > 180f ? AriaPlayPhase.Blocked : AriaPlayPhase.Waiting;
                session.GestureRequested = 0;
                return;
            }
            if (observation.Kind == AriaPlayObservationKind.Waiting)
            {
                // World guidance can disappear for a frame while its camera focus or
                // command-mode presentation settles. Keep an already prepared aim so
                // that the next visible frame can submit it instead of restarting the
                // human-paced delay forever.
                // Preserve only a short presentation gap. A target that remains hidden
                // must return to Waiting so camera/show-me guidance can take over rather
                // than leaving the session permanently aimed at stale screen geometry.
                if (session.Phase != AriaPlayPhase.Aiming || now > session.DueAt + 1.5f)
                    session.Phase = now - session.LastProgressAt > 180f ? AriaPlayPhase.Blocked : AriaPlayPhase.Waiting;
                session.GestureRequested = 0;
                return;
            }
            if (observation.Kind == AriaPlayObservationKind.Unavailable)
            {
                session.GestureRequested = 0;
                if (now - session.LastProgressAt > 15f) session.Phase = AriaPlayPhase.Blocked;
                return;
            }
            if (now < session.DueAt) return;
            if (session.TargetId != observation.TargetId)
            {
                session.TargetId = observation.TargetId; session.Attempts = 0;
                session.LastProgressAt = now;
                // A replacement control can occupy the same pixels as the previous one.
                // Show a fresh approach instead of immediately pressing its new action.
                session.Phase = AriaPlayPhase.Observing;
            }
            if (session.Attempts >= 3) { session.Phase = AriaPlayPhase.Blocked; return; }
            if (session.Phase != AriaPlayPhase.Aiming)
            {
                session.Target = observation.Position;
                session.Phase = AriaPlayPhase.Aiming;
                session.DueAt = now + .9f;
                return;
            }
            // The direct campaign ring is a fixed world point. Camera focus can still be
            // moving it across the screen, so require a short stable presentation before
            // pressing; otherwise the release can resolve several map cells away.
            float positionDelta = (session.Target - observation.Position).sqrMagnitude;
            if (observation.Kind == AriaPlayObservationKind.WorldTarget && observation.TargetId == 1 && positionDelta > 9f)
            { session.Target = observation.Position; session.DueAt = now + .35f; return; }
            // Moving contacts and map contacts must not postpone their tap forever.
            // Refresh those from the latest visible observation immediately before input.
            if (observation.Kind == AriaPlayObservationKind.WorldTarget || observation.TargetId == -20004)
                session.Target = observation.Position;
            // Layout controls still need a stable position before pressing.
            else if (positionDelta > 9f)
            { session.Target = observation.Position; session.DueAt = now + .35f; return; }
            session.Drag = observation.Drag; session.DragEnd = observation.DragEnd;
            session.GestureRequested = 1;
            session.Phase = AriaPlayPhase.Touching;
            session.Attempts++;
        }
    }
}
