using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    public partial struct AriaSkirmishPlanSystem
    {
        // Uses the existing Decision/Input systems for real paced contacts and
        // cancellation. This method never submits a gameplay command itself.
        public static void StepPublicObjective(in AriaSkirmishObservation view,
            in AriaObjectiveTouchObservation objective, ref AriaObjectiveTouchPlan plan,
            ref AriaPlaySessionComponent touch, ref AriaPlayObservationComponent output)
        {
            output = new AriaPlayObservationComponent { Kind = objective.Finished ? AriaPlayObservationKind.Finished : AriaPlayObservationKind.Waiting,
                Time = view.Time, Frame = view.Frame, GoalId = unchecked((int)(objective.PublicKey ^ objective.GroupId)) };
            if (touch.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked or AriaPlayPhase.Starting) return;
            if (objective.Finished) { touch.GestureRequested = 0; return; }
            if (!objective.Supported || !objective.Presented || objective.PresentationEpoch == 0)
            { touch.GestureRequested = 0; if (touch.Phase != AriaPlayPhase.Touching) touch.Phase = AriaPlayPhase.Blocked; return; }
            if (objective.Paused) { touch.GestureRequested=0;return; }
            if (touch.Phase == AriaPlayPhase.Touching) return;
            if (objective.GroupId == 0 || objective.PublicKey == 0 || objective.Action == AriaObjectiveAction.None) return;
            // Same frame may be observed repeatedly by a presentation callback.
            if (view.Frame <= plan.LastFrame) return;
            plan.LastFrame = view.Frame;
            if (plan.GroupId != objective.GroupId || plan.PublicKey != objective.PublicKey || plan.Action != objective.Action || plan.NavigationGroupId != objective.NavigationGroupId ||
                plan.RosterRevision != objective.RosterRevision || plan.PresentationEpoch != objective.PresentationEpoch)
            {
                plan = new AriaObjectiveTouchPlan { GroupId = objective.GroupId, PublicKey = objective.PublicKey,
                    Action = objective.Action, NavigationGroupId=objective.NavigationGroupId, RosterRevision = objective.RosterRevision, PresentationEpoch = objective.PresentationEpoch, LastFrame = view.Frame,
                    Deadline = view.Time + 30, OrderRevisionAtIssue = objective.AcceptedOrderRevision,
                    ProgressRevision = objective.PublicProgressRevision, LastContactActions = touch.Actions };
            }
            if (objective.Action==AriaObjectiveAction.Repair && objective.RepairWorkActive && !plan.CommandPending && plan.Stage!=3)
            {if(objective.ProgressKnown&&plan.ProgressRevision!=objective.PublicProgressRevision){plan.ProgressRevision=objective.PublicProgressRevision;touch.LastObjectiveProgressAt=view.Time;}return;}
            if (plan.CommandPending)
            {
                if (objective.AcceptedOrderRevision > plan.OrderRevisionAtIssue && objective.AcceptedGroupId == plan.GroupId &&
                    objective.AcceptedPublicKey == plan.PublicKey && objective.AcceptedAction == plan.Action &&
                    (plan.Action!=AriaObjectiveAction.Unload || objective.AcceptedNavigationGroupId==plan.NavigationGroupId) &&
                    objective.AcceptedContactTargetId == plan.LastTarget && objective.AcceptedFrame >= plan.IssueFrame)
                {
                    plan.CommandPending = false; plan.Stage = 4; plan.ObserveUntil = view.Time + 4;
                    // Actual displayed order acceptance, not a touch count, is progress.
                    touch.LastObjectiveProgressAt = view.Time;
                }
                else if (view.Time >= plan.Deadline)
                { touch.Phase = AriaPlayPhase.Blocked; touch.GestureRequested = 0; return; }
                else return;
            }
            if (plan.Stage == 4)
            {
                if (objective.ProgressKnown && objective.PublicProgressRevision != plan.ProgressRevision)
                { plan.ProgressRevision = objective.PublicProgressRevision; touch.LastObjectiveProgressAt = view.Time; }
                // Preserve the accepted order; never reissue just because dwell/travel takes time.
                return;
            }
            if (view.Time >= plan.Deadline || plan.RejectedAttempts >= 3)
            { touch.Phase = AriaPlayPhase.Blocked; touch.GestureRequested = 0; return; }
            if (plan.Stage == 3 && touch.Actions > plan.LastContactActions && touch.TargetId == plan.LastTarget)
            { plan.CommandPending = true; plan.Deadline = view.Time + 8; return; }
            if (view.MapOpen && (objective.GoalInView || !objective.ExclusiveGroupSelected))
            { Publish(view.CloseMap,false,ref plan,touch,ref output);return; }
            if (!objective.ExclusiveGroupSelected)
            {
                var selection = objective.ClearOtherSelection.Available ? objective.ClearOtherSelection : objective.SelectGroup;
                Publish(selection, false, ref plan, touch, ref output); return;
            }
            if(objective.Action==AriaObjectiveAction.Repair && !objective.Repair.Available)
            {Publish(objective.OpenRepair,false,ref plan,touch,ref output);return;}
            if (objective.Action==AriaObjectiveAction.Unload && !objective.Unload.Available)
            { Publish(objective.OpenUnload,false,ref plan,touch,ref output);return; }
            if (objective.Action!=AriaObjectiveAction.Unload && objective.Action!=AriaObjectiveAction.Repair && (!objective.GoalInView || !objective.CameraSettled))
            {
                if (!objective.CameraSettled) return;
                Publish(objective.Focus, false, ref plan, touch, ref output); return;
            }
            if (objective.Action == AriaObjectiveAction.Inspect)
            {
                // An inspected map/visible route must produce a fresh public decision.
                // Focus alone never issues an order or claims that the route is legal.
                return;
            }
            AriaTouchTarget target;
            bool world = false;
            if (objective.Action == AriaObjectiveAction.Move)
            {
                if (!objective.MoveMode) { Publish(objective.Move, false, ref plan, touch, ref output); return; }
                if (!objective.GroundFitsPublicGoal) return;
                target = objective.Ground; world = true;
            }
            else target = objective.Action==AriaObjectiveAction.Repair?objective.Repair:objective.Action == AriaObjectiveAction.Hold ? objective.Hold : objective.Unload;
            if (!target.Available) return;
            // Remember the exact next normal command before the gesture. Delivery is
            // verified only by an independently displayed accepted-order revision.
            if (plan.Stage != 3) { plan.OrderRevisionAtIssue = objective.AcceptedOrderRevision; plan.IssueFrame = view.Frame; }
            Publish(target, world, ref plan, touch, ref output); plan.Stage = 3;
        }

        static void Publish(in AriaTouchTarget target, bool world, ref AriaObjectiveTouchPlan plan,
            in AriaPlaySessionComponent touch, ref AriaPlayObservationComponent output)
        {
            if (!target.Available || target.Id == 0) return;
            if (plan.LastTarget == target.Id && touch.Actions > plan.LastContactActions)
            {
                bool scrollProgress = target.Drag && target.ScrollTargetId != 0 && plan.ScrollTargetId == target.ScrollTargetId &&
                    plan.BestScrollDistance - target.ScrollDistanceViewports >= .05f;
                if (scrollProgress) { plan.RejectedAttempts=0;plan.BestScrollDistance=target.ScrollDistanceViewports; }
                else plan.RejectedAttempts++;
                plan.LastContactActions = touch.Actions;
            }
            else if (plan.LastTarget != target.Id)
            { plan.LastTarget = target.Id; plan.LastContactActions = touch.Actions; }
            if (target.ScrollTargetId != plan.ScrollTargetId) { plan.ScrollTargetId=target.ScrollTargetId;plan.BestScrollDistance=target.ScrollDistanceViewports; }
            output.Kind = world ? AriaPlayObservationKind.WorldTarget : AriaPlayObservationKind.Control;
            output.TargetId = target.Id; output.Position = target.Position;
            output.Drag = target.Drag ? (byte)1 : (byte)0; output.DragEnd = target.DragEnd;
            output.ScrollTargetId = target.ScrollTargetId; output.ScrollDistanceViewports = target.ScrollDistanceViewports;
        }
    }
}
