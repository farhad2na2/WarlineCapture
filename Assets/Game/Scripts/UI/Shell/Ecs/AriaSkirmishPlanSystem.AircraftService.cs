using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    public partial struct AriaSkirmishPlanSystem
    {
        static bool StepExactAircraftService(in AriaSkirmishObservation view,ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch,ref AriaPlayObservationComponent output)
        {
            ref var p=ref plan.ObjectiveSupport.Aircraft;
            var facts=view.AircraftService;
            plan.Intent=AriaSkirmishIntent.ReturnAircraft;
            if(view.DrawerOpen){return ServiceTarget(view,ref plan,view.CloseDrawer,ref output);}
            if(p.Bound)
            {
                if(!facts.Presented||!facts.Actor.Equals(p.Actor)||facts.World!=p.World||facts.GroupId!=p.GroupId||facts.SetupHash!=p.SetupHash)
                    return EndExactAircraftService(view,ref plan,false);
                if(facts.Paused)return true;
                if(p.Stage==2)
                {
                    if(facts.ReturnRevision<=p.ReturnRevision||!facts.AcceptedReturnContact||facts.ReturnContactTarget!=p.Target||facts.RequestFrame<p.IssueFrame)return true;
                    p.Stage=3;plan.Intent=AriaSkirmishIntent.ServiceAircraft;
                }
                if(p.Stage==3)
                {
                    // Shared fuel is a faction pool. This proves the same actor
                    // finished its accepted return, never an invented fuel refill.
                    if(!facts.Airborne&&!facts.Rolling&&!facts.Returning&&facts.HomeKnown&&facts.AtHome&&facts.PooledFactionFuel>0)
                    {touch.LastObjectiveProgressAt=view.Time;return EndExactAircraftService(view,ref plan,true);}
                    return true;
                }
                if(!view.ReturnAircraft.Available){return ServiceTarget(view,ref plan,view.OpenAircraftActions,ref output);}
                p.Stage=2;p.IssueFrame=view.Frame;p.Target=view.ReturnAircraft.Id;p.Actions=touch.Actions;
                return ServiceTarget(view,ref plan,view.ReturnAircraft,ref output);
            }
            // Every normal non-infantry group is one actor. The actual selected
            // readout still validates that invariant rather than trusting a mask.
            if(facts.Presented&&view.SelectedAircraft&&view.ExpandedSelectedGroups==1&&view.ExpandedSelectedOffPage==0)
            {
                if(facts.NativeTaskBusy||!facts.HomeKnown||facts.Returning||!facts.Airborne&&!facts.Rolling)
                    return EndExactAircraftService(view,ref plan,false);
                p.Bound=true;p.Actor=facts.Actor;p.World=facts.World;p.GroupId=facts.GroupId;p.SetupHash=facts.SetupHash;
                p.ReturnRevision=facts.ReturnRevision;p.Stage=1;
                return true;
            }
            if(view.ExpandedSelectedGroups>0){TryDeselectSquads(view,ref output);return true;}
            if(view.ExpandedPageCount<=0||p.PagesVisited>view.ExpandedPageCount)return EndExactAircraftService(view,ref plan,false);
            if(p.NextPage>=view.ExpandedPageCount)p.NextPage=0;
            if(view.ExpandedPageIndex!=p.NextPage)
            {return ServiceTarget(view,ref plan,view.ExpandedPageIndex<p.NextPage?view.NextSquadPage:view.PreviousSquadPage,ref output);}
            for(int slot=p.NextSlot;slot<5;slot++)
                if((view.ExpandedAirMask&(1<<slot))!=0&&view.Squad(slot).Available)
                {
                    p.NextSlot=slot+1;if(p.NextSlot>=5){p.NextSlot=0;p.NextPage=(p.NextPage+1)%view.ExpandedPageCount;}
                    return ServiceTarget(view,ref plan,view.Squad(slot),ref output);
                }
            p.NextSlot=0;p.NextPage=(p.NextPage+1)%view.ExpandedPageCount;p.PagesVisited++;
            return ServiceTarget(view,ref plan,view.NextSquadPage,ref output);
        }
        public const float MaxServiceTargetWaitSeconds=5f;
        // W27b: a wanted control that is not shown is waited for briefly, then the service ends (its normal
        // 15 s pause) instead of reporting Unavailable until AriaPlayDecisionSystem stops ARIA with 114.
        static bool ServiceTarget(in AriaSkirmishObservation view,ref AriaSkirmishPlanComponent plan,in AriaTouchTarget target,
            ref AriaPlayObservationComponent output)
        {
            ref var p=ref plan.ObjectiveSupport.Aircraft;
            if(target.Available){p.UnavailableSince=0;Target(target,false,ref output);return true;}
            if(p.UnavailableSince<=0)p.UnavailableSince=view.Time;
            if(view.Time-p.UnavailableSince>MaxServiceTargetWaitSeconds)return EndExactAircraftService(view,ref plan,false);
            output.Kind=AriaPlayObservationKind.Waiting;return true;
        }
        static bool EndExactAircraftService(in AriaSkirmishObservation view,ref AriaSkirmishPlanComponent plan,bool completed)
        {
            ref var support=ref plan.ObjectiveSupport;var cursor=support.Aircraft;
            support.ObservedAcceptance=completed;support.Action=AriaObjectiveSupportAction.None;
            support.NextDecisionAt=view.ObjectiveSupport.SimulationSeconds+15;
            support.Aircraft=new AriaAircraftServicePlan {NextPage=cursor.NextPage,NextSlot=cursor.NextSlot};
            return false;
        }
    }
}
