using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    public partial struct AriaSkirmishPlanSystem
    {
        public static bool StepPublicBoarding(in AriaSkirmishObservation v,ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch,ref AriaPlayObservationComponent output)
        {
            ref var p=ref plan.PublicBoarding;var f=v.PublicBoarding;var goal=v.PublicObjective;float now=v.ObjectiveSupport.SimulationSeconds;
            if(touch.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked or AriaPlayPhase.Starting){p=default;return false;}
            if(!goal.Applicable||!goal.Presented||!goal.Supported||goal.Finished||float.IsNaN(now)||float.IsInfinity(now)||now<0)return false;
            if(goal.Paused||touch.Phase==AriaPlayPhase.Touching)return p.Active;
            if(p.Epoch!=goal.PresentationEpoch)p=new AriaPublicBoardingPlan{Epoch=goal.PresentationEpoch};
            if(!p.Active)
            {
                // Inspect the ordinary carrier card before allowing an existing
                // mounted original group to unload. Recovery creates no old UI
                // receipt; the fresh public roster/current move is authoritative.
                if(goal.Action==AriaObjectiveAction.Unload&&f.ExitOpen&&(!f.Presented||f.GroupId!=goal.GroupId))
                {
                    if(p.InspectUntil==0)p.InspectUntil=now+30;
                    if(now>=p.InspectUntil){touch.Phase=AriaPlayPhase.Blocked;touch.GestureRequested=0;return true;}
                    Target(goal.ClearOtherSelection.Available?goal.ClearOtherSelection:goal.SelectGroup,false,ref output);return true;
                }
                p.InspectUntil=0;
                if(now<p.NextAt||!f.Presented||!f.Eligible&&!f.AlreadyMounted||!f.ExitOpen||!f.AlreadyMounted&&v.ObjectiveSupport.CriticalPublicRisk||
                    plan.ObjectiveSupport.Action!=AriaObjectiveSupportAction.None||plan.PublicScan.Active||
                    plan.ObjectiveTouch.CommandPending||plan.ObjectiveTouch.Stage==3||goal.PublicKey==0||f.GroupId!=goal.GroupId)return false;
                p=new AriaPublicBoardingPlan{Active=true,Epoch=goal.PresentationEpoch,GroupId=f.GroupId,CarrierGroupId=f.CarrierGroupId,
                    Carrier=f.Carrier,World=f.World,SetupHash=f.SetupHash,Goal=goal.PublicKey,Deadline=now+180,BaselineMoveRevision=f.MoveRevision,Stage=f.AlreadyMounted?2:0};
            }
            if(!f.ExitOpen||p.Goal!=goal.PublicKey||now>=p.Deadline)
            {
                // A paid/accepted boarding or carrier movement is never undone
                // by stopping the assistant. Unknown lifecycle cannot trigger an
                // opportunistic unload outside the actual public exit.
                p.Active=false;p.NextAt=now+30;
                if(p.Stage>=1){touch.Phase=AriaPlayPhase.Blocked;touch.GestureRequested=0;return true;}return false;
            }
            plan.Intent=AriaSkirmishIntent.EvacuateOriginals;
            if(p.Stage>=2)
            {
                if(!f.Presented||!f.CarrierSelected)
                {if(v.ExpandedSelectedGroups>0){TryDeselectSquads(v,ref output);return true;}Target(f.SelectCarrier,false,ref output);return true;}
            }
            if(f.Presented&&(!f.Carrier.Equals(p.Carrier)||f.World!=p.World||f.SetupHash!=p.SetupHash||f.GroupId!=p.GroupId||f.CarrierGroupId!=p.CarrierGroupId))
            {touch.Phase=AriaPlayPhase.Blocked;touch.GestureRequested=0;p.Active=false;return true;}
            if(p.Stage==0)
            {
                if(!f.Presented)return true;
                if(!goal.ExclusiveGroupSelected){Target(goal.ClearOtherSelection.Available?goal.ClearOtherSelection:goal.SelectGroup,false,ref output);return true;}
                if(!goal.CameraSettled)return true;
                if(!f.CarrierInView){Target(f.FocusCarrier,false,ref output);return true;}
                if(!f.BoardMode){Target(f.Board,false,ref output);return true;}
                if(!f.CarrierTarget.Available)return true;
                p.IssueFrame=v.Frame;p.Target=f.CarrierTarget.Id;p.Stage=1;Target(f.CarrierTarget,true,ref output);return true;
            }
            if(p.Stage==1)
            {
                if(!f.Presented||!f.Accepted||f.Frame<p.IssueFrame||f.ContactTarget!=p.Target)return true;
                p.RequestId=f.RequestId;p.Stage=2;touch.LastObjectiveProgressAt=v.Time;return true;
            }
            if(p.Stage==2)
            {
                if(!f.AllMounted||!f.AlreadyMounted&&f.RequestId!=p.RequestId)return true;
                if(f.AtExit){p.Active=false;p.NextAt=now+30;return false;}
                if(f.ExistingMoveToExit){p.Stage=4;return true;}
                if(!goal.CameraSettled)return true;
                if(!goal.GoalInView){Target(goal.Focus,false,ref output);return true;}
                if(!goal.MoveMode){Target(goal.Move,false,ref output);return true;}
                if(!goal.GroundFitsPublicGoal||!goal.Ground.Available)return true;
                p.BaselineMoveRevision=f.MoveRevision;p.IssueFrame=v.Frame;p.Target=goal.Ground.Id;p.Stage=3;Target(goal.Ground,true,ref output);return true;
            }
            if(p.Stage==3)
            {
                if(f.MoveAccepted&&f.MoveRevision>p.BaselineMoveRevision&&f.MoveFrame>=p.IssueFrame&&f.ContactTarget==p.Target)
                {p.Stage=4;touch.LastObjectiveProgressAt=v.Time;}return true;
            }
            if(p.Stage==4&&f.AllMounted&&f.AtExit)
            {p.Active=false;p.NextAt=now+30;touch.LastObjectiveProgressAt=v.Time;return false;}
            return true;
        }
    }
}
