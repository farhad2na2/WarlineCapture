using System;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        readonly SkirmishConvoyServiceJourney convoyServiceJourney=new SkirmishConvoyServiceJourney();
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
            var stage=convoyServiceJourney.Tick(model,selected,acceptedHold,repairButton!=null&&SkirmishPresentedRepairPolicy.HasAcceptedContact(selected,repairButton.GetEntityId().GetHashCode()));
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
                view.PublicObjective=new AriaObjectiveTouchObservation{Applicable=true,Intent=AriaSkirmishIntent.ServiceOriginalTruck};return;
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
            var ground=service.Ground[destination];
            if(ground.Available&&!WatchTargetIsReachable(ground.Position,ground.Id,true))ground=default;
            ulong key=PublicObjectiveKey(service.Model.Service.PublicId);
            bool exclusive=frame.ExclusiveSelectedGroup==group;
            var result=new AriaObjectiveTouchObservation{Applicable=true,Supported=true,Presented=true,PresentationEpoch=frame.PresentationEpoch,
                Paused=frame.Paused,GroupId=group,NavigationGroupId=group,RosterRevision=frame.RosterRevision^convoyServiceJourney.Epoch,PublicKey=key,
                Action=stage==ConvoyServiceStage.Hold?AriaObjectiveAction.Hold:AriaObjectiveAction.Move,Intent=AriaSkirmishIntent.ServiceOriginalTruck,
                ExclusiveGroupSelected=exclusive,SelectGroup=MatchHudSquadTouchNavigation.GroupTarget(watchSquads,group),
                ClearOtherSelection=view.ExpandedSelectedGroups>0&&!exclusive?MatchHudSquadTouchNavigation.SelectedCardTarget(watchSquads):default,
                CameraSettled=frame.CameraSettled,GoalInView=service.GroundVisible[destination],GroundFitsPublicGoal=true,
                Focus=ObserveWatchButton(service.FocusButton),Ground=ground,MoveMode=frame.MoveMode,
                Move=ObserveWatchButton(_commandControlsView?.MoveButton),Hold=ObserveWatchButton(_commandControlsView?.HoldButton),
                AcceptedOrderRevision=frame.AcceptedOrderRevision,AcceptedGroupId=frame.AcceptedGroup,AcceptedAction=frame.AcceptedAction,
                AcceptedContactTargetId=frame.AcceptedContactTargetId,AcceptedFrame=frame.AcceptedFrame,
                AcceptedPublicKey=frame.AcceptedGoalId==service.Model.Service.PublicId?key:0,
                ProgressKnown=true,PublicProgressRevision=convoyServiceJourney.ProgressRevision};
            if(!result.Focus.Available)result.Focus=ObservePublicObjectiveMapFocus(service.Model.Service.Destinations[destination].World,view);
            view.PublicObjective=result;
        }
    }
}
