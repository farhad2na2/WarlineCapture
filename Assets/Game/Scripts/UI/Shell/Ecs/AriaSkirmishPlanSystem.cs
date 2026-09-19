using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Burst;
using Unity.Entities;
namespace Game.UI.Shell.Ecs
{
    /// <summary>Base Assault skill. Inputs contain only currently presented player information.</summary>
    [BurstCompile, UpdateInGroup(typeof(PresentationSystemGroup)), UpdateBefore(typeof(AriaPlayDecisionSystem))]
    public partial struct AriaSkirmishPlanSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (view, plan, touch, output) in SystemAPI.Query<RefRO<AriaSkirmishObservationComponent>, RefRW<AriaSkirmishPlanComponent>, RefRW<AriaPlaySessionComponent>, RefRW<AriaPlayObservationComponent>>())
                if (view.ValueRO.Value.Active) Step(view.ValueRO.Value, ref plan.ValueRW, ref touch.ValueRW, ref output.ValueRW);
        }
        public static void Step(in AriaSkirmishObservation view, ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch, ref AriaPlayObservationComponent output)
        {
            output = new AriaPlayObservationComponent { Kind = view.Finished ? AriaPlayObservationKind.Finished : AriaPlayObservationKind.Waiting, Time = view.Time, Frame = view.Frame, GoalId = 10000 + plan.Cycle * 10 + plan.Slot };
            if (view.Finished || touch.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked or AriaPlayPhase.Starting) return;
            if (plan.LastProgressAt == 0 || plan.EnemyHealth != view.EnemyHealth || plan.PlayerHealth != view.PlayerHealth || plan.ForceHealth != view.ForceHealth || plan.Infantry != view.Infantry)
            { plan.LastProgressAt = view.Time; plan.EnemyHealth = view.EnemyHealth; plan.PlayerHealth = view.PlayerHealth; plan.ForceHealth = view.ForceHealth; plan.Infantry = view.Infantry; }
            // A loop of successful taps is not evidence that the match is progressing.
            if (view.Time - plan.LastProgressAt > 150) { touch.Phase = AriaPlayPhase.Blocked; return; }
            if (touch.Phase == AriaPlayPhase.Touching) return;
            if (plan.TargetPending != 0 && (touch.TargetId == -20001 || touch.TargetId == -20002) && touch.Actions > plan.ActionsAtTarget)
            {
                plan.TargetPending = 0; plan.Slot++; plan.ObserveUntil = view.Time + 4;
                if (plan.Slot >= 5) { plan.Slot = 0; plan.Cycle++; plan.ObserveUntil = view.Time + 8; }
            }
            if (view.MapOpen)
            {
                plan.Intent = AriaSkirmishIntent.FindThreat;
                if (plan.MapFocusActions > 0 && touch.TargetId == -20004 && touch.Actions >= plan.MapFocusActions) plan.MapFocusActions = -1;
                bool focused = plan.MapFocusActions == -1;
                Target(focused || view.MapContactInView || !view.FocusThreat.Available ? view.CloseMap : view.FocusThreat, false, ref output);
                if (output.TargetId == -20004 && plan.MapFocusActions == 0) plan.MapFocusActions = touch.Actions + 1;
                return;
            }
            plan.MapFocusActions = 0;
            if (view.Time < plan.ObserveUntil) { plan.Intent = AriaSkirmishIntent.ObserveBattle; return; }
            if (view.Infantry < 16 && view.Time >= plan.NextRecruitAt &&
                plan.Intent is AriaSkirmishIntent.Recruit or AriaSkirmishIntent.ObserveBattle)
            {
                plan.Intent = AriaSkirmishIntent.Recruit;
                if (view.Recruit.Available) { plan.RecruitUnavailableAt = 0; Target(view.Recruit, false, ref output); return; }
                if (plan.RecruitUnavailableAt == 0) plan.RecruitUnavailableAt = view.Time;
                if (view.Time - plan.RecruitUnavailableAt < 3) return;
                // An unavailable Recruit control is visible affordability/capacity feedback.
                // Close the drawer and fight with existing units; never force an order.
                plan.NextRecruitAt = view.Time + 12;
            }
            if (view.DrawerOpen) { Target(view.CloseDrawer, false, ref output); return; }
            for (int i = 0; i < 5 && (view.AvailableSquads & (1 << plan.Slot)) == 0; i++) plan.Slot = (plan.Slot + 1) % 5;
            if (view.AvailableSquads == 0) { plan.Intent = AriaSkirmishIntent.ObserveBattle; return; }
            if (!view.SelectionVisible || view.SelectedSlot != plan.Slot)
            { plan.Intent = AriaSkirmishIntent.SelectSquad; Target(view.Squad(plan.Slot), false, ref output); return; }
            bool threat = view.Threat.Available || view.FocusThreat.Available;
            if (threat && !view.Threat.Available)
            { plan.Intent = AriaSkirmishIntent.FindThreat; Target(view.FocusThreat, false, ref output); return; }
            if (!threat && !view.EnemyBase.Available)
            { plan.Intent = AriaSkirmishIntent.FindBase; Target(view.FocusEnemy, false, ref output); return; }
            if (!view.AttackMode)
            { plan.Intent = AriaSkirmishIntent.Attack; Target(view.Attack, false, ref output); return; }
            plan.Intent = threat ? AriaSkirmishIntent.TargetThreat : AriaSkirmishIntent.TargetBase;
            Target(threat ? view.Threat : view.EnemyBase, true, ref output);
            plan.ActionsAtTarget = touch.Actions; plan.TargetPending = 1;
        }
        private static void Target(AriaTouchTarget target, bool world, ref AriaPlayObservationComponent output)
        {
            if (!target.Available) { output.Kind = AriaPlayObservationKind.Unavailable; return; }
            output.Kind = world ? AriaPlayObservationKind.WorldTarget : AriaPlayObservationKind.Control;
            output.TargetId = target.Id; output.Position = target.Position;
        }
    }
}
