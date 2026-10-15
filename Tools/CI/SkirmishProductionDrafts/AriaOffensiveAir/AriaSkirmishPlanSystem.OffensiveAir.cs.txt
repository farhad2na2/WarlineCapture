using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
namespace Game.UI.Shell.Ecs
{
    public partial struct AriaSkirmishPlanSystem
    {
        public static bool WantsPublicAirReadiness(in AriaSkirmishObservation v)
        {
            var f=v.AirRecruit;if(!f.Presented)return false;
            bool Later(AriaAirRecruitChoice r)=>!r.CanQueue&&r.CanQueueAtFullReadiness&&!r.Pending&&r.Live==0;
            if(v.VisibleHostileAir>0&&Later(f.Fighter))return true;
            if(f.Light.Live+f.Heavy.Live+f.Strike.Live+f.Fighter.Live<2&&(Later(f.Light)||Later(f.Heavy)||Later(f.Strike)))return true;
            if(Later(f.Drone))return true;
            return v.ObjectiveSupport.Evacuation&&
                f.TransportHeli.Live+f.TransportPlane.Live==0&&(Later(f.TransportHeli)||Later(f.TransportPlane));
        }
        public static AriaPublicAirRole ChoosePublicAirRecruit(in AriaSkirmishObservation v)
        {
            var f=v.AirRecruit;if(!f.Presented)return AriaPublicAirRole.None;
            bool Ready(AriaAirRecruitChoice r)=>r.CanQueue&&!r.Pending&&r.Live==0;
            if(v.VisibleHostileAir>0&&Ready(f.Fighter))return AriaPublicAirRole.Fighter;
            int pressure=f.Light.Live+f.Heavy.Live+f.Strike.Live+f.Fighter.Live;
            // Two independent pressure groups can support FC's separate zones;
            // the same bound stays modest for BT/CE and never changes native caps.
            if(pressure<2&&!f.Light.Pending&&!f.Heavy.Pending&&!f.Strike.Pending&&!f.Fighter.Pending)
            {
                if(Ready(f.Strike))return AriaPublicAirRole.Strike;
                if(Ready(f.Heavy))return AriaPublicAirRole.AttackHeli;
                if(Ready(f.Light))return AriaPublicAirRole.AttackHeliLight;
            }
            if(v.ObjectiveSupport.Evacuation&&
                f.TransportHeli.Live+f.TransportPlane.Live==0&&!f.TransportHeli.Pending&&!f.TransportPlane.Pending)
            {
                if(Ready(f.TransportHeli))return AriaPublicAirRole.TransportHeli;
                if(Ready(f.TransportPlane))return AriaPublicAirRole.TransportPlane;
            }
            if(Ready(f.Drone))return AriaPublicAirRole.Drone;
            return AriaPublicAirRole.None;
        }
        public static bool StepOffensiveAir(in AriaSkirmishObservation v,ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch,ref AriaPlayObservationComponent output)
        {
            ref var p=ref plan.OffensiveAir;var o=v.PublicObjective;var f=v.OffensiveAir;
            if(touch.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked or AriaPlayPhase.Starting)
            {p=default;return false;}
            if(!o.Applicable||!o.Presented||!o.Supported||o.Finished){p=default;return false;}
            if(o.Paused||touch.Phase==AriaPlayPhase.Touching)return p.Active;
            float now=v.ObjectiveSupport.SimulationSeconds;
            if(float.IsNaN(now)||float.IsInfinity(now)||now<0)return false;
            if(p.Epoch!=o.PresentationEpoch)p=new AriaOffensiveAirPlan {Epoch=o.PresentationEpoch,NextAt=now+20};
            bool urgent=v.ObjectiveSupport.CriticalPublicRisk||o.RepairWorkActive||o.Action==AriaObjectiveAction.Repair||
                o.Action==AriaObjectiveAction.Unload||plan.ObjectiveTouch.CommandPending||plan.ObjectiveTouch.Stage==3;
            if(p.Active&&(urgent||p.GoalKey!=o.PublicKey||now>=p.Deadline))return EndOffensiveAir(ref p,now);
            if(!p.Active)
            {
                bool anyCombatAir=v.AirRecruit.Presented?
                    v.AirRecruit.Light.Live+v.AirRecruit.Heavy.Live+v.AirRecruit.Fighter.Live+v.AirRecruit.Strike.Live>0:f.Presented&&f.Eligible;
                if(urgent||plan.ObjectiveSupport.Action!=AriaObjectiveSupportAction.None||plan.PublicBoarding.Active||plan.PublicScan.Active||
                    !anyCombatAir||o.PublicKey==0||now<p.NextAt||(o.Action!=AriaObjectiveAction.Inspect&&plan.ObjectiveTouch.Stage!=4))return false;
                int page=p.NextPage,slot=p.NextSlot;uint epoch=p.Epoch;
                p=new AriaOffensiveAirPlan {Active=true,Epoch=epoch,NextPage=page,NextSlot=slot,GoalKey=o.PublicKey,Deadline=now+35};
            }
            plan.Intent=AriaSkirmishIntent.Attack;
            if(v.DrawerOpen){Target(v.CloseDrawer,false,ref output);return true;}
            if(!p.Bound)
            {
                if(f.Presented&&v.ExpandedSelectedGroups==1&&v.ExpandedSelectedOffPage==0)
                {
                    // Continue the bounded page sweep past noncombat aircraft
                    // and already assigned sorties. Deselecting a card never
                    // cancels that actor's accepted order.
                    if(!f.Eligible||f.Returning||f.Busy){TryDeselectSquads(v,ref output);return true;}
                    p.Bound=true;p.Actor=f.Actor;p.World=f.World;p.GroupId=f.GroupId;p.SetupHash=f.SetupHash;p.BaselineRevision=f.Revision;
                    return true;
                }
                if(v.ExpandedSelectedGroups>0){TryDeselectSquads(v,ref output);return true;}
                if(v.ExpandedPageCount<=0||p.PagesVisited>v.ExpandedPageCount)return EndOffensiveAir(ref p,now);
                if(p.NextPage>=v.ExpandedPageCount)p.NextPage=0;
                if(v.ExpandedPageIndex!=p.NextPage){Target(v.ExpandedPageIndex<p.NextPage?v.NextSquadPage:v.PreviousSquadPage,false,ref output);return true;}
                for(int slot=p.NextSlot;slot<5;slot++)if((v.ExpandedAirMask&(1<<slot))!=0&&v.Squad(slot).Available)
                {p.NextSlot=slot+1;if(p.NextSlot==5){p.NextSlot=0;p.NextPage=(p.NextPage+1)%v.ExpandedPageCount;}Target(v.Squad(slot),false,ref output);return true;}
                p.NextSlot=0;p.NextPage=(p.NextPage+1)%v.ExpandedPageCount;p.PagesVisited++;Target(v.NextSquadPage,false,ref output);return true;
            }
            if(!f.Presented||!f.Eligible||f.Returning||!f.Actor.Equals(p.Actor)||f.World!=p.World||f.GroupId!=p.GroupId||f.SetupHash!=p.SetupHash)
                return EndOffensiveAir(ref p,now);
            if(p.Stage==2)
            {
                if(f.Revision>p.BaselineRevision&&f.RequestFrame>=p.IssueFrame&&f.AcceptedContact&&f.ContactTarget==p.Target&&f.GoalFits)
                {touch.LastObjectiveProgressAt=v.Time;return EndOffensiveAir(ref p,now);}
                return true;
            }
            if(f.Busy)return EndOffensiveAir(ref p,now);
            if(!o.CameraSettled)return true;
            if(!o.GoalInView){Target(o.Focus,false,ref output);return true;}
            if(!v.AttackMode){Target(v.Attack,false,ref output);return true;}
            if(!o.GroundFitsPublicGoal||!o.Ground.Available)return true;
            p.Stage=2;p.Target=o.Ground.Id;p.IssueFrame=v.Frame;Target(o.Ground,true,ref output);return true;
        }
        static bool EndOffensiveAir(ref AriaOffensiveAirPlan p,float now)
        {
            int page=p.NextPage,slot=p.NextSlot;uint epoch=p.Epoch;
            p=new AriaOffensiveAirPlan {Epoch=epoch,NextPage=page,NextSlot=slot,NextAt=now+20};return false;
        }
    }
}
