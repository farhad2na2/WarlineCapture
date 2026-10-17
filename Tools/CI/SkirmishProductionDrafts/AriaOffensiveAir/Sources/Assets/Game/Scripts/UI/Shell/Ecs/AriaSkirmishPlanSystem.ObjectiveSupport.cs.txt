using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    public partial struct AriaSkirmishPlanSystem
    {
        // Support publishes the same visible touch targets as the ordinary HUD.
        // It never calls a production, research, repair or troop-order service.
        public static bool StepPublicObjectiveSupport(in AriaSkirmishObservation view,
            ref AriaSkirmishPlanComponent plan,ref AriaPlaySessionComponent touch,
            ref AriaPlayObservationComponent output)
        {
            ref var support=ref plan.ObjectiveSupport;
            var objective=view.PublicObjective;var facts=view.ObjectiveSupport;
            if(!objective.Applicable)return false;
            if(touch.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked or AriaPlayPhase.Starting)
            {support=default;return false;}
            if(touch.Phase==AriaPlayPhase.Touching)return true;
            if(!objective.Presented||!objective.Supported||objective.Finished||!facts.Presented||
                float.IsNaN(facts.SimulationSeconds)||float.IsInfinity(facts.SimulationSeconds)||facts.SimulationSeconds<0)
            {support=default;return false;}
            if(objective.Paused)return true;
            if(support.Epoch!=objective.PresentationEpoch)
                support=new AriaObjectiveSupportPlan{Epoch=objective.PresentationEpoch};
            if(view.Frame<=support.LastFrame)return support.Action!=AriaObjectiveSupportAction.None||support.Closing;
            support.LastFrame=view.Frame;
            float now=facts.SimulationSeconds;
            if(support.NextAircraftCheck==0)support.NextAircraftCheck=now+45;
            bool urgent=facts.CriticalPublicRisk||objective.RepairWorkActive||
                objective.Action==AriaObjectiveAction.Repair||objective.Action==AriaObjectiveAction.Unload;
            bool changedGoal=plan.ObjectiveTouch.GroupId!=objective.GroupId||
                plan.ObjectiveTouch.PublicKey!=objective.PublicKey||plan.ObjectiveTouch.Action!=objective.Action;
            if(urgent||changedGoal||plan.ObjectiveTouch.CommandPending||plan.ObjectiveTouch.Stage==3)
            {
                // Cancel only this plan's unconfirmed placement through its visible
                // control. Stop/manual takeover above leaves accepted work to player.
                if(support.Action==AriaObjectiveSupportAction.Helipad&&view.PlacementOpen)
                {Target(view.PlacementCancel,false,ref output);return true;}
                if(support.Action!=AriaObjectiveSupportAction.None)
                {support.Action=AriaObjectiveSupportAction.None;support.Closing=view.DrawerOpen;support.NextDecisionAt=now+4;}
                if(support.Closing&&view.DrawerOpen)
                {Target(view.CloseDrawer,false,ref output);return true;}
                support.Closing=false;
                // A missing group has no command to pre-empt; allow the narrowly
                // scoped ordinary-infantry recovery decision below.
                if(!(objective.GroupId==0&&facts.DismountedCombatGroups==0&&!urgent))return false;
            }
            if(support.Closing)
            {
                if(view.DrawerOpen){Target(view.CloseDrawer,false,ref output);return true;}
                support.Closing=false;return false;
            }
            if(support.Action!=AriaObjectiveSupportAction.None)
            {
                if(SupportAccepted(view,support))
                {
                    // Actual queue/own roster/research/build state is evidence;
                    // completed gestures alone never count as battle progress.
                    support.ObservedAcceptance=true;support.Action=AriaObjectiveSupportAction.None;
                    support.NextDecisionAt=now+4;support.Closing=view.DrawerOpen;
                    touch.LastObjectiveProgressAt=view.Time;
                    if(support.Closing)Target(view.CloseDrawer,false,ref output);
                    return true;
                }
                if(now>=support.Deadline||support.RepeatedContacts>=3)
                {
                    if(support.Action==AriaObjectiveSupportAction.Helipad&&view.PlacementOpen)
                    {Target(view.PlacementCancel,false,ref output);return true;}
                    support.Action=AriaObjectiveSupportAction.None;support.NextDecisionAt=now+15;
                    support.Closing=view.DrawerOpen;
                    if(support.Closing){Target(view.CloseDrawer,false,ref output);return true;}
                    return false;
                }
                return ExecuteSupport(view,ref plan,ref touch,ref output);
            }
            if(now<support.NextDecisionAt||view.PlacementOpen||view.MapOpen)return false;
            // Keep active objective orders intact. Recovery can also recruit when
            // no eligible objective group remains, without inventing replacements
            // for the immutable BT originals or CE trucks.
            bool recovery=facts.DismountedCombatGroups==0;
            if(plan.ObjectiveTouch.Stage!=4&&!recovery)return false;
            var chosen=ChoosePublicObjectiveSupport(view);
            if(chosen==AriaObjectiveSupportAction.None&&now>=support.NextAircraftCheck&&view.ExpandedTotalGroups>0)
            {
                chosen=AriaObjectiveSupportAction.ReturnAircraft;support.NextAircraftCheck=now+45;
                var cursor=support.Aircraft;
                support.Aircraft=new AriaAircraftServicePlan{NextPage=cursor.NextPage,NextSlot=cursor.NextSlot};
            }
            if(chosen==AriaObjectiveSupportAction.None)return false;
            support.RecruitAirRole=chosen==AriaObjectiveSupportAction.AttackAircraft?ChoosePublicAirRecruit(view):AriaPublicAirRole.None;
            if(support.RecruitAirRole!=AriaPublicAirRole.None)support.BaselineAir=view.AirRecruit.For(support.RecruitAirRole).Live;
            support.Action=chosen;support.Deadline=now+(chosen==AriaObjectiveSupportAction.Helipad?45:
                chosen==AriaObjectiveSupportAction.ReturnAircraft?120:30);
            support.LastTarget=0;support.ScrollTargetId=0;support.BestScrollDistance=0;support.RepeatedContacts=0;support.LastActions=touch.Actions;
            support.BaselineInfantry=view.Infantry;support.BaselineTankers=view.TankersCommitted;
            support.BaselineAir=support.RecruitAirRole==AriaPublicAirRole.None?view.OwnAttackAirLive:view.AirRecruit.For(support.RecruitAirRole).Live;support.BaselineReadiness=facts.Readiness;
            support.ResearchId=facts.ResearchId;support.ObservedAcceptance=false;
            support.BaselineRosterRevision=view.ExpandedRosterRevision;support.AirStage=0;
            if(chosen==AriaObjectiveSupportAction.Helipad){plan.PadStage=1;plan.PadDeadline=support.Deadline;}
            return ExecuteSupport(view,ref plan,ref touch,ref output);
        }

        public static AriaObjectiveSupportAction ChoosePublicObjectiveSupport(in AriaSkirmishObservation view)
        {
            var f=view.ObjectiveSupport;
            // Capture requires two independently useful groups; BT needs living
            // dismounted support to open a corridor; CE needs an ordinary escort.
            int desired=f.CaptureMajority?2:1;
            if(f.DismountedCombatGroups<desired&&!view.RifleRecruitPending&&view.CanAffordRifle)
                return AriaObjectiveSupportAction.Rifle;
            if(view.VisibleHostileAir>0&&!view.AntiAirCommitted&&view.CanAffordAntiAir)
                return AriaObjectiveSupportAction.AntiAir;
            if(!view.LogisticsTruckCommitted&&view.CanAffordLogisticsTruck)return AriaObjectiveSupportAction.LogisticsTruck;
            if(view.TankersCommitted<2&&view.CanAffordTanker)return AriaObjectiveSupportAction.Tanker;
            if(ChoosePublicAirRecruit(view)!=AriaPublicAirRole.None)return AriaObjectiveSupportAction.AttackAircraft;
            if(WantsPublicAirReadiness(view)&&f.ResearchVisible&&!f.ResearchInProgress&&view.CanUpgradeReadiness)
                return AriaObjectiveSupportAction.Readiness;
            if(!view.AirProfile)return AriaObjectiveSupportAction.None;
            if(f.ResearchVisible&&!f.ResearchInProgress&&!view.ReadinessEligible&&view.CanUpgradeReadiness)
                return AriaObjectiveSupportAction.Readiness;
            if(view.ReadinessEligible&&!view.PadPresent&&view.CanBuildAirPad)return AriaObjectiveSupportAction.Helipad;
            if(!view.AirRecruit.Presented&&view.AirQueueOffered&&!view.AirRecruitPending&&view.OwnAttackAirLive==0)
                return AriaObjectiveSupportAction.AttackAircraft;
            return AriaObjectiveSupportAction.None;
        }

        static bool SupportAccepted(in AriaSkirmishObservation v,in AriaObjectiveSupportPlan p)
        {
            switch(p.Action)
            {
                case AriaObjectiveSupportAction.Rifle:return v.RifleRecruitPending||v.Infantry>p.BaselineInfantry;
                case AriaObjectiveSupportAction.AntiAir:return v.AntiAirCommitted;
                case AriaObjectiveSupportAction.LogisticsTruck:return v.LogisticsTruckCommitted;
                case AriaObjectiveSupportAction.Tanker:return v.TankersCommitted>p.BaselineTankers;
                case AriaObjectiveSupportAction.Readiness:return v.ObjectiveSupport.ResearchVisible&&
                    (v.ObjectiveSupport.Readiness>p.BaselineReadiness||v.ObjectiveSupport.ResearchInProgress&&v.ObjectiveSupport.ResearchId!=p.ResearchId);
                case AriaObjectiveSupportAction.Helipad:return v.PadPresent;
                case AriaObjectiveSupportAction.AttackAircraft:return p.RecruitAirRole==AriaPublicAirRole.None?
                    v.AirRecruitPending||v.OwnAttackAirLive>p.BaselineAir:
                    v.AirRecruit.Presented&&(v.AirRecruit.For(p.RecruitAirRole).Pending||v.AirRecruit.For(p.RecruitAirRole).Live>p.BaselineAir);
                case AriaObjectiveSupportAction.ReturnAircraft:return false; // Exact actor receipt owner below.
                default:return false;
            }
        }
        static bool ExecuteSupport(in AriaSkirmishObservation view,ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch,ref AriaPlayObservationComponent output)
        {
            ref var p=ref plan.ObjectiveSupport;
            if(p.Action==AriaObjectiveSupportAction.ReturnAircraft)
                return StepExactAircraftService(view,ref plan,ref touch,ref output);
            if(p.Action==AriaObjectiveSupportAction.Helipad)
            {
                var simulationView=view;simulationView.Time=view.ObjectiveSupport.SimulationSeconds;
                StepHomePadPlacement(simulationView,ref plan,ref touch,ref output);return true;
            }
            var target=p.Action==AriaObjectiveSupportAction.Rifle?view.Recruit:
                p.Action==AriaObjectiveSupportAction.AntiAir?view.RecruitAntiAir:
                p.Action==AriaObjectiveSupportAction.LogisticsTruck?view.RecruitLogisticsTruck:
                p.Action==AriaObjectiveSupportAction.Tanker?view.RecruitTanker:
                p.Action==AriaObjectiveSupportAction.Readiness?view.UpgradeReadiness:view.RecruitAir;
            if(p.Action==AriaObjectiveSupportAction.AttackAircraft&&p.RecruitAirRole!=AriaPublicAirRole.None)
                target=view.AirRecruit.For(p.RecruitAirRole).Target;
            if(!target.Available)return true;
            if(p.LastTarget==target.Id&&touch.Actions>p.LastActions)
            {
                // A new public state is checked before this branch. Three same
                // contacts without acceptance yield back to objective play.
                bool progress=target.Drag&&target.ScrollTargetId!=0&&p.ScrollTargetId==target.ScrollTargetId&&
                    p.BestScrollDistance-target.ScrollDistanceViewports>=.05f;
                if(progress){p.RepeatedContacts=0;p.BestScrollDistance=target.ScrollDistanceViewports;}
                else p.RepeatedContacts++;
            }
            else if(p.LastTarget!=target.Id)p.RepeatedContacts=0;
            if(p.ScrollTargetId!=target.ScrollTargetId)
            {p.ScrollTargetId=target.ScrollTargetId;p.BestScrollDistance=target.ScrollDistanceViewports;}
            p.LastTarget=target.Id;p.LastActions=touch.Actions;
            plan.Intent=AriaSkirmishIntent.Recruit;Target(target,false,ref output);return true;
        }
    }
}
