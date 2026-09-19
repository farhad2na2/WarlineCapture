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
            if (observation.GoalId != session.GoalId)
            {
                session.GoalId = observation.GoalId; session.Attempts = 0;
                if (session.Phase != AriaPlayPhase.Touching) session.Phase = AriaPlayPhase.Observing;
                session.LastProgressAt = now;
            }
            if (session.Phase == AriaPlayPhase.Touching) return;
            if (observation.Kind is AriaPlayObservationKind.Cinematic or AriaPlayObservationKind.Waiting)
            {
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
            // Never press a control that moved while the hand was approaching it.
            if ((session.Target - observation.Position).sqrMagnitude > 9f)
            { session.Target = observation.Position; session.DueAt = now + .35f; return; }
            session.GestureRequested = 1;
            session.Phase = AriaPlayPhase.Touching;
            session.Attempts++;
        }
    }
}
