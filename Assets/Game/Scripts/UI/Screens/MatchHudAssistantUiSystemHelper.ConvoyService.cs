using System;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        readonly SkirmishConvoyServiceJourney convoyServiceJourney=new SkirmishConvoyServiceJourney();
        readonly System.Collections.Generic.HashSet<uint> convoyDelivering=new System.Collections.Generic.HashSet<uint>();
        string convoyHoldTraced;
        // W37g (lead): a service return Move waits, with the truck held where it is, until an own escort group is within
        // EscortRadiusMetres, for at most ServiceDeferSeconds once per Travel epoch (the journey retries after 20 s).
        uint convoyDeferEpoch;float convoyDeferSince;bool convoyDeferOver;
        bool ConvoyEscortNear(AriaPresentedObjectiveFrame frame,uint truckGroup)
        {
            SkirmishPublicPlayerGroup truck=null;foreach(var g in frame.Groups)if(g!=null&&g.GroupId==truckGroup){truck=g;break;}
            if(truck==null||!truck.PositionKnown)return true;
            foreach(var g in frame.Groups)
                if(g!=null&&g.Own&&g.Living&&g.PositionKnown&&g.GroupId!=truckGroup&&(g.DesignatedSubjects==null||g.DesignatedSubjects.Length==0)&&
                    Game.Simulation.AriaConvoyCoverPolicy.Dist(g.WorldX,g.WorldZ,truck.WorldX,truck.WorldZ)<=Game.Simulation.AriaConvoyCoverPolicy.EscortRadiusMetres)return true;
            return false;
        }
        bool ConvoyServiceOwnsRepair=>watchSkirmish!=null&&watchSkirmish.TryReadPublicObjectiveForAria(Time.frameCount,out var frame)&&
            frame.Goal?.Perspective==SkirmishPublicGoalPerspective.Escort;
        void ResetConvoyServiceJourney(){convoyServiceJourney.Reset();publicRepairScope=null;}
        void ObserveConvoyService(ref AriaSkirmishObservation view,in AriaObjectiveTouchObservation escort)
        {
            if(watchSkirmish==null||!watchSkirmish.TryReadPublicObjectiveForAria(Time.frameCount,out var frame)||
                frame.Goal?.Perspective!=SkirmishPublicGoalPerspective.Escort)return;
            var service=frame.ConvoyService;
            var model=frame.OwnModel;
            if(model==null){view.PublicObjective=new AriaObjectiveTouchObservation{Applicable=true};return;}
            UiVehicleRepairModel selected=null;UnityEngine.UI.Button repairButton=null;
            if(watchSelection!=null)watchSelection.TryReadPresentedFieldRepair(Time.frameCount,out selected,out repairButton);
            string holdGoal=convoyServiceJourney.Stage==ConvoyServiceStage.EmergencyHold?convoyServiceJourney.OriginalId:service?.Model.Service.PublicId;
            bool acceptedHold=frame.AcceptedGroup==convoyServiceJourney.GroupId&&frame.AcceptedGoalId==holdGoal&&
                frame.AcceptedAction==AriaObjectiveAction.Hold&&_commandControlsView?.HoldButton!=null&&frame.AcceptedContactTargetId==_commandControlsView.HoldButton.GetEntityId().GetHashCode();
            var oldStage=convoyServiceJourney.Stage;
            convoyDelivering.Clear();
            for(int i=0;i<frame.Goal.NodeCount;i++)
                if(frame.Goal.Node(i).Kind==SkirmishPublicGoalNodeKind.DeliveryDestination)
                    foreach(var approach in frame.Approaches)
                        if(approach.InsideGoalFootprint&&approach.GoalId==frame.Goal.Node(i).PublicId)convoyDelivering.Add(approach.GroupId);
            uint holdRevision=convoyServiceJourney.HoldRevisionAtStart;
            var stage=convoyServiceJourney.Tick(model,selected,acceptedHold,repairButton!=null&&SkirmishPresentedRepairPolicy.HasAcceptedContact(selected,repairButton.GetEntityId().GetHashCode()),convoyDelivering);
            if(oldStage==ConvoyServiceStage.Hold||stage==ConvoyServiceStage.Hold)TraceConvoyServiceHold(frame,model,selected,holdGoal,holdRevision,oldStage,stage);
            if(oldStage==ConvoyServiceStage.Repair&&stage==ConvoyServiceStage.Hold)publicRepairScope=null;
            if(oldStage==ConvoyServiceStage.None&&(stage==ConvoyServiceStage.Travel||stage==ConvoyServiceStage.EmergencyHold))watchSkirmish.PresentConvoyServiceDiagnosis(null);
            if(stage==ConvoyServiceStage.None)return;
            if(stage==ConvoyServiceStage.Resume)
            {
                convoyServiceJourney.Resume();publicObjectiveChoice=default;nextPublicObjectiveDecision=0;publicRepairScope=null;
                view.PublicObjective=escort;return;
            }
            if(stage==ConvoyServiceStage.Handback)
            {
                watchSkirmish.PresentConvoyServiceDiagnosis(UiShellRuntimeGateway.Localization.IsRightToLeft?
                    "دسترسی به محل تعمیر یا تعمیر تأیید نشد. کنترل با شماست.":convoyServiceJourney.Failure+" You have control.");
                if(oldStage!=ConvoyServiceStage.Handback){publicObjectiveChoice=default;nextPublicObjectiveDecision=0;publicRepairScope=null;}
                if(SkirmishConvoyServiceJourney.PresentsEscortObjective(stage))view.PublicObjective=escort;
                return;
            }
            if(stage==ConvoyServiceStage.Repair)
            {
                if(view.PublicObjective.Action!=AriaObjectiveAction.Repair)
                    view.PublicObjective=new AriaObjectiveTouchObservation{Applicable=true,Supported=true,Presented=true,PresentationEpoch=frame.PresentationEpoch,
                        Paused=frame.Paused,Intent=AriaSkirmishIntent.RepairVehicle};
                return;
            }
            if(stage==ConvoyServiceStage.EmergencyHold)
            {
                uint originalGroup=convoyServiceJourney.GroupId;bool only=frame.ExclusiveSelectedGroup==originalGroup;
                view.PublicObjective=new AriaObjectiveTouchObservation{Applicable=true,Supported=true,Presented=true,PresentationEpoch=frame.PresentationEpoch,
                    Paused=frame.Paused,GroupId=originalGroup,NavigationGroupId=originalGroup,RosterRevision=frame.RosterRevision^convoyServiceJourney.Epoch,
                    PublicKey=PublicObjectiveKey(convoyServiceJourney.OriginalId),Action=AriaObjectiveAction.Hold,Intent=AriaSkirmishIntent.StopDamagedTruck,
                    ExclusiveGroupSelected=only,SelectGroup=MatchHudSquadTouchNavigation.GroupTarget(watchSquads,originalGroup),
                    ClearOtherSelection=view.ExpandedSelectedGroups>0&&!only?MatchHudSquadTouchNavigation.SelectedCardTarget(watchSquads):default,
                    GoalInView=true,CameraSettled=true,Hold=ObserveWatchButton(_commandControlsView?.HoldButton),AcceptedOrderRevision=frame.AcceptedOrderRevision,
                    AcceptedGroupId=frame.AcceptedGroup,AcceptedAction=frame.AcceptedAction,AcceptedFrame=frame.AcceptedFrame,AcceptedContactTargetId=frame.AcceptedContactTargetId,
                    AcceptedPublicKey=frame.AcceptedGoalId==convoyServiceJourney.OriginalId?PublicObjectiveKey(convoyServiceJourney.OriginalId):0};
                return;
            }
            if(service==null){view.PublicObjective=new AriaObjectiveTouchObservation{Applicable=true};return;}
            int destination=convoyServiceJourney.DestinationIndex;
            if(destination<0||destination>=service.Ground.Length||destination>=service.GroundVisible.Length)
            {view.PublicObjective=new AriaObjectiveTouchObservation{Applicable=true};return;}
            uint group=convoyServiceJourney.GroupId;
            if(stage==ConvoyServiceStage.Travel&&convoyServiceJourney.Failures==0)
            {
                if(convoyDeferEpoch!=convoyServiceJourney.Epoch){convoyDeferEpoch=convoyServiceJourney.Epoch;convoyDeferSince=frame.SimulationSeconds;convoyDeferOver=false;}
                if(!convoyDeferOver&&(view.EscortCapable==0||ConvoyEscortNear(frame,group)||
                    frame.SimulationSeconds-convoyDeferSince>=Game.Simulation.AriaConvoyCoverPolicy.ServiceDeferSeconds))convoyDeferOver=true;
                if(!convoyDeferOver)
                {
                    // Hold the truck where it is (the emergency-Hold order); escorts converge on the cover point at the truck.
                    bool only=frame.ExclusiveSelectedGroup==group;
                    view.ConvoyServiceDeferred=true;
                    view.PublicObjective=new AriaObjectiveTouchObservation{Applicable=true,Supported=true,Presented=true,PresentationEpoch=frame.PresentationEpoch,
                        Paused=frame.Paused,GroupId=group,NavigationGroupId=group,RosterRevision=frame.RosterRevision^convoyServiceJourney.Epoch,
                        PublicKey=PublicObjectiveKey(convoyServiceJourney.OriginalId),Action=AriaObjectiveAction.Hold,Intent=AriaSkirmishIntent.StopDamagedTruck,
                        ExclusiveGroupSelected=only,SelectGroup=MatchHudSquadTouchNavigation.GroupTarget(watchSquads,group),
                        ClearOtherSelection=view.ExpandedSelectedGroups>0&&!only?MatchHudSquadTouchNavigation.SelectedCardTarget(watchSquads):default,
                        GoalInView=true,CameraSettled=true,Hold=ObserveWatchButton(_commandControlsView?.HoldButton),AcceptedOrderRevision=frame.AcceptedOrderRevision,
                        AcceptedGroupId=frame.AcceptedGroup,AcceptedAction=frame.AcceptedAction,AcceptedFrame=frame.AcceptedFrame,AcceptedContactTargetId=frame.AcceptedContactTargetId,
                        AcceptedPublicKey=frame.AcceptedGoalId==convoyServiceJourney.OriginalId?PublicObjectiveKey(convoyServiceJourney.OriginalId):0};
                    return;
                }
            }
            var ground=service.Ground[destination];
            if(ground.Available&&!WatchTargetIsReachable(ground.Position,ground.Id,true))ground=default;
            ulong key=PublicObjectiveKey(service.Model.Service.PublicId);
            bool exclusive=frame.ExclusiveSelectedGroup==group;
            var result=new AriaObjectiveTouchObservation{Applicable=true,Supported=true,Presented=true,PresentationEpoch=frame.PresentationEpoch,
                Paused=frame.Paused,GroupId=group,NavigationGroupId=group,RosterRevision=frame.RosterRevision^convoyServiceJourney.Epoch,PublicKey=key,
                Action=stage==ConvoyServiceStage.Hold?AriaObjectiveAction.Hold:AriaObjectiveAction.Move,Intent=AriaSkirmishIntent.ServiceOriginalTruck,
                ExclusiveGroupSelected=exclusive,SelectGroup=MatchHudSquadTouchNavigation.GroupTarget(watchSquads,group),
                ClearOtherSelection=view.ExpandedSelectedGroups>0&&!exclusive?MatchHudSquadTouchNavigation.SelectedCardTarget(watchSquads):default,
                // W37b: Hold stops the selected truck where it is (inside the pad radius); like EmergencyHold it needs no
                // camera focus. W37 proof: 13 of 17 service Holds waited on focus (inView=False) and the 20 s Hold stage expired.
                CameraSettled=stage==ConvoyServiceStage.Hold||frame.CameraSettled,GoalInView=stage==ConvoyServiceStage.Hold||service.GroundVisible[destination],GroundFitsPublicGoal=true,
                Focus=ObserveWatchButton(service.FocusButton),Ground=ground,MoveMode=frame.MoveMode,
                Move=ObserveWatchButton(_commandControlsView?.MoveButton),Hold=ObserveWatchButton(_commandControlsView?.HoldButton),
                AcceptedOrderRevision=frame.AcceptedOrderRevision,AcceptedGroupId=frame.AcceptedGroup,AcceptedAction=frame.AcceptedAction,
                AcceptedContactTargetId=frame.AcceptedContactTargetId,AcceptedFrame=frame.AcceptedFrame,
                AcceptedPublicKey=frame.AcceptedGoalId==service.Model.Service.PublicId?key:0,
                ProgressKnown=true,PublicProgressRevision=convoyServiceJourney.ProgressRevision};
            if(!result.Focus.Available)result.Focus=ObservePublicObjectiveMapFocus(service.Model.Service.Destinations[destination].World,view);
            view.PublicObjective=result;
        }

        // W37b evidence only: why a service Hold does not become a Repair (first failing receipt condition), traced on change.
        void TraceConvoyServiceHold(AriaPresentedObjectiveFrame frame,UiSkirmishPublicObjectiveModel model,UiVehicleRepairModel selected,string holdGoal,
            uint holdRevision,ConvoyServiceStage oldStage,ConvoyServiceStage stage)
        {
            if(!Game.Runtime.AriaCommandEvidence.Active&&!Debug.isDebugBuild)return;
            uint group=convoyServiceJourney.GroupId;var order=model.Order;
            var own=Array.Find(model.OwnGroups,row=>row.Policy.GroupId==group);
            var service=model.Service;int holdButton=_commandControlsView?.HoldButton!=null?_commandControlsView.HoldButton.GetEntityId().GetHashCode():0;
            bool exact=selected!=null&&selected.SingleOwnVehicle&&selected.GroupId==group&&selected.ActorPublicId==convoyServiceJourney.OriginalId;
            bool inside=own!=null&&SkirmishPublicServiceAcceptance.AllInside(service,own.OwnMemberPositions);
            string why=!order.Present?"no_order":order.GroupId!=group?"order_group="+order.GroupId:order.Action!=AriaObjectiveAction.Hold?"order_action="+order.Action:
                order.Revision<=holdRevision?"no_new_hold":!order.Accepted?"hold_refused":frame.AcceptedGoalId!=holdGoal?"goal="+(frame.AcceptedGoalId??"null"):
                frame.AcceptedContactTargetId!=holdButton?"contact":own==null||!own.HoldActive?"hold_inactive":!exact?"not_exact":"ok";
            string key=group+"|"+oldStage+">"+stage+"|"+why+"|"+inside+"|"+(own!=null&&own.MoveActive)+"|"+(own!=null&&own.HoldActive)+"|"+exact+"|"+(selected!=null&&selected.Eligible);
            if(key==convoyHoldTraced)return;
            convoyHoldTraced=key;
            float distance=own==null||service==null?-1:Vector2.Distance(new Vector2(own.Policy.WorldX,own.Policy.WorldZ),new Vector2(service.World.x,service.World.z));
            Debug.Log($"[ConvoyServiceHold] group={group} stage={oldStage}>{stage} why={why} orderRev={order.Revision}>{holdRevision} allInside={inside} moveActive={own!=null&&own.MoveActive} holdActive={own!=null&&own.HoldActive} exact={exact} quoteEligible={selected!=null&&selected.Eligible} distance={distance:F1} failure={convoyServiceJourney.Failure??"-"} t={model.SimulationSeconds:F2}");
        }
    }
}
