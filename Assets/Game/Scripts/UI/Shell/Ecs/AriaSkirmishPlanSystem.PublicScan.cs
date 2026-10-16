using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    public partial struct AriaSkirmishPlanSystem
    {
        static void ResetScanAttempt(ref AriaPublicScanPlan p,float nextAt)
        {
            p=new AriaPublicScanPlan{Epoch=p.Epoch,NextPage=p.NextPage,NextSlot=p.NextSlot,NextAt=nextAt};
        }
        public static bool StepPublicScan(in AriaSkirmishObservation v,ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch,ref AriaPlayObservationComponent output)
        {
            ref var p=ref plan.PublicScan;var objective=v.PublicObjective;var facts=v.PublicScan;
            if(touch.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Starting or AriaPlayPhase.Blocked)
            {p=default;return false;}
            if(!objective.Applicable||!objective.Presented||!objective.Supported||objective.Finished)return false;
            if(objective.Paused||touch.Phase==AriaPlayPhase.Touching)return p.Active;
            float now=v.ObjectiveSupport.SimulationSeconds;
            if(float.IsNaN(now)||float.IsInfinity(now)||now<0)return false;
            if(p.Epoch!=objective.PresentationEpoch)p=new AriaPublicScanPlan{Epoch=objective.PresentationEpoch,NextAt=now+30};
            bool urgent=v.ObjectiveSupport.CriticalPublicRisk||objective.RepairWorkActive||objective.Action==AriaObjectiveAction.Repair||
                objective.Action==AriaObjectiveAction.Unload||plan.ObjectiveTouch.CommandPending||plan.ObjectiveTouch.Stage==3;
            if(p.Active&&(urgent||p.GoalKey!=objective.PublicKey||now>=p.Deadline))
            {ResetScanAttempt(ref p,now+20);return false;}
            if(!p.Active)
            {
                if(urgent||plan.ObjectiveSupport.Action!=AriaObjectiveSupportAction.None||objective.PublicKey==0||
                    (objective.Action!=AriaObjectiveAction.Inspect&&plan.ObjectiveTouch.Stage!=4)||now<p.NextAt)return false;
                ResetScanAttempt(ref p,p.NextAt);
                p.Active=true;p.GoalKey=objective.PublicKey;p.Deadline=now+120;
            }
            plan.Intent=AriaSkirmishIntent.Scout;
            if(v.DrawerOpen){Target(v.CloseDrawer,false,ref output);return true;}
            if(!p.Bound)
            {
                // Recovery restores the native order and rebased duration,
                // not an old touch receipt. Preserve that ongoing work; it is
                // neither a fresh request nor public completion evidence.
                if(facts.Presented&&facts.SelectedDrone&&facts.InProgress)
                {ResetScanAttempt(ref p,now+15);return false;}
                if(facts.Presented&&facts.SelectedDrone&&v.ExpandedSelectedGroups==1&&v.ExpandedSelectedOffPage==0)
                {p.Bound=true;p.Actor=facts.Actor;p.World=facts.World;p.GroupId=facts.GroupId;p.SetupHash=facts.SetupHash;p.BaselineRevision=facts.Revision;return true;}
                if(v.ExpandedSelectedGroups>0){TryDeselectSquads(v,ref output);return true;}
                if(v.ExpandedPageCount<=0||p.PagesVisited>v.ExpandedPageCount){ResetScanAttempt(ref p,now+30);return false;}
                if(p.NextPage>=v.ExpandedPageCount)p.NextPage=0;
                if(v.ExpandedPageIndex!=p.NextPage){Target(v.ExpandedPageIndex<p.NextPage?v.NextSquadPage:v.PreviousSquadPage,false,ref output);return true;}
                for(int i=p.NextSlot;i<5;i++)if((v.ExpandedAirMask&(1<<i))!=0&&v.Squad(i).Available)
                {p.NextSlot=i+1;if(p.NextSlot==5){p.NextSlot=0;p.NextPage=(p.NextPage+1)%v.ExpandedPageCount;}Target(v.Squad(i),false,ref output);return true;}
                p.NextSlot=0;p.NextPage=(p.NextPage+1)%v.ExpandedPageCount;p.PagesVisited++;Target(v.NextSquadPage,false,ref output);return true;
            }
            if(!facts.Presented||!facts.SelectedDrone||!facts.Actor.Equals(p.Actor)||facts.World!=p.World||facts.GroupId!=p.GroupId||facts.SetupHash!=p.SetupHash)
            {ResetScanAttempt(ref p,now+20);return false;}
            if(p.Stage==2)
            {
                if(facts.Revision>p.BaselineRevision&&facts.RequestFrame>=p.IssueFrame&&facts.ContactAccepted&&facts.ContactTarget==p.Target&&facts.CenterInPublicGoal)
                {p.RequestId=facts.RequestId;p.Stage=3;touch.LastObjectiveProgressAt=v.Time;}
                return true;
            }
            if(p.Stage==3)
            {
                if(facts.RequestId==p.RequestId&&facts.CenterInPublicGoal&&facts.Completed)
                {ResetScanAttempt(ref p,now+45);touch.LastObjectiveProgressAt=v.Time;return false;}
                return true;
            }
            if(!objective.CameraSettled)return true;
            if(!objective.GoalInView){Target(objective.Focus,false,ref output);return true;}
            if(!facts.ScanMode){Target(facts.Scan,false,ref output);return true;}
            if(!objective.GroundFitsPublicGoal||!objective.Ground.Available)return true;
            p.Target=objective.Ground.Id;p.IssueFrame=v.Frame;p.Stage=2;Target(objective.Ground,true,ref output);return true;
        }
    }
}
