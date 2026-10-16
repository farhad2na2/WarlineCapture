using Game.UI.Contracts;
using Game.Tactical.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        void ObservePublicScan(ref AriaSkirmishObservation view)
        {
            if(!view.PublicObjective.Presented||!view.PublicObjective.Supported||watchSelection==null||
                !watchSelection.TryReadPresentedAircraftService(Time.frameCount,out var model)||!model.CanScan||
                watchSkirmish==null||!watchSkirmish.TryReadPublicObjectiveForAria(Time.frameCount,out var frame))return;
            string publicId=null;
            for(int i=0;i<frame.Goal.NodeCount;i++)if(PublicObjectiveKey(frame.Goal.Node(i).PublicId)==view.PublicObjective.PublicKey)
            {if(publicId!=null)return;publicId=frame.Goal.Node(i).PublicId;}
            bool mode=UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)&&command.ActiveCommandMode==TacticalCommandMode.Scan;
            view.PublicScan=new AriaPublicScanObservation {Presented=true,SelectedDrone=true,InProgress=model.ScanInProgress,Actor=model.Actor,World=model.World,
                GroupId=model.GroupId,SetupHash=model.SetupHash,Revision=model.ScanRevision,RequestId=model.ScanRequestId,
                RequestFrame=model.ScanFrame,Completed=model.ScanCompleted,ContactTarget=model.CompletedTarget,ScanMode=mode,
                Scan=ObserveWatchButton(_commandControlsView?.ScanButton),
                ContactAccepted=model.ScanRevision>0&&model.CompletedTarget!=0&&model.CompletedFrame>=model.ScanFrame&&model.CompletedFrame-model.ScanFrame<=2&&
                    Vector2.Distance(model.CompletedScreen,model.ScanScreen)<=2,
                CenterInPublicGoal=publicId!=null&&watchSkirmish.PresentedPublicScanCenterFits(Time.frameCount,publicId,new Vector2(model.ScanCenter.x,model.ScanCenter.z))};
        }
    }
}
