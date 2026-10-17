using Game.Skirmish.Contracts;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        uint boardNavigationEpoch,boardNavigationCarrier;
        void ObservePublicBoarding(ref AriaSkirmishObservation view)
        {
            if(!view.PublicObjective.Presented||!view.PublicObjective.Supported||watchSkirmish==null||
                !watchSkirmish.TryReadPublicObjectiveForAria(Time.frameCount,out var frame)||frame.Goal.Perspective!=SkirmishPublicGoalPerspective.Evacuate||!frame.Goal.Score.ExitOpen)return;
            if(boardNavigationEpoch!=frame.PresentationEpoch){boardNavigationEpoch=frame.PresentationEpoch;boardNavigationCarrier=0;}
            var observation=new AriaPublicBoardingObservation{ExitOpen=true};
            if(watchSkirmish.TryReadPresentedBoarding(Time.frameCount,out var model))
            {
                boardNavigationCarrier=model.CarrierGroupId;string exit=null;
                for(int i=0;i<frame.Goal.NodeCount;i++)if(frame.Goal.Node(i).Kind==SkirmishPublicGoalNodeKind.EvacuationExit&&PublicObjectiveKey(frame.Goal.Node(i).PublicId)==view.PublicObjective.PublicKey)
                {if(exit!=null)return;exit=frame.Goal.Node(i).PublicId;}
                observation.Presented=true;observation.Eligible=model.Eligible;observation.GroupId=model.GroupId;
                observation.Carrier=model.Carrier;observation.CarrierGroupId=model.CarrierGroupId;observation.World=model.World;observation.SetupHash=model.SetupHash;
                observation.RequestId=model.RequestId;observation.Frame=model.Frame;observation.ContactTarget=model.CompletedTarget;
                observation.Accepted=SkirmishPresentedBoardingPolicy.AcceptedContact(model);
                observation.AllMounted=model.AllMounted;observation.AlreadyMounted=model.AlreadyMounted;observation.CarrierSelected=model.CarrierSelected;
                observation.MoveRevision=model.MoveRevision;observation.MoveFrame=model.MoveFrame;
                observation.AtExit=exit!=null&&watchSkirmish.PresentedBoardingFootprintFits(Time.frameCount,exit,model.CarrierFootprint);
                observation.MoveAccepted=exit!=null&&SkirmishPresentedBoardingPolicy.AcceptedMoveContact(model)&&
                    watchSkirmish.PresentedBoardingFootprintFits(Time.frameCount,exit,new[]{new Vector2(model.MoveWorld.x,model.MoveWorld.z)})&&
                    watchSkirmish.PresentedBoardingFootprintFits(Time.frameCount,exit,model.MoveGoals);
                observation.ExistingMoveToExit=exit!=null&&model.CarrierSelected&&model.MoveActive&&watchSkirmish.PresentedBoardingFootprintFits(Time.frameCount,exit,model.MoveGoals);
                observation.FocusCarrier=ObservePublicObjectiveMapFocus(model.CarrierWorld,view);
                var camera=Camera.main;
                if(camera!=null)
                {
                    var screen=camera.WorldToScreenPoint(model.CarrierWorld);
                    int key=unchecked(model.Carrier.Index*397^model.Carrier.Version^0x24000000);
                    if(key!=0&&screen.z>0&&Screen.safeArea.Contains(screen)&&WatchTargetIsReachable(screen,key,true))
                    {observation.CarrierInView=true;observation.CarrierTarget=new AriaTouchTarget{Available=true,Id=key,Position=screen};}
                }
                var wheel=Object.FindAnyObjectByType<CommandWheelPanelView>();
                observation.Board=ObserveWatchButton(wheel!=null?wheel.NextBoardButton:null);
                if(UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command))observation.BoardMode=command.ActiveCommandMode==TacticalCommandMode.Board;
            }
            observation.SelectCarrier=MatchHudSquadTouchNavigation.GroupTarget(watchSquads,boardNavigationCarrier);
            view.PublicBoarding=observation;
        }
    }
    public static class SkirmishPresentedBoardingPolicy
    {
        public static bool AcceptedContact(UiPublicBoardingModel m)=>m!=null&&m.Accepted&&m.Presented&&m.RequestId>0&&m.CompletedTarget!=0&&
            m.CompletedFrame>=m.Frame&&m.CompletedFrame-m.Frame<=2&&Vector2.Distance(m.CompletedScreen,m.Screen)<=2;
        public static bool AcceptedMoveContact(UiPublicBoardingModel m)=>m!=null&&m.MovePresented&&m.MoveAccepted&&m.MoveRevision>0&&m.CompletedTarget!=0&&
            m.CompletedFrame>=m.MoveFrame&&m.CompletedFrame-m.MoveFrame<=2&&Vector2.Distance(m.CompletedScreen,m.MoveScreen)<=2;
    }
}
