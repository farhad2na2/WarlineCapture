using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        void ObserveAircraftService(ref AriaSkirmishObservation view)
        {
            if(watchSelection==null||!watchSelection.TryReadPresentedAircraftService(Time.frameCount,out var row))return;
            view.AircraftService=new AriaAircraftServiceObservation {Presented=true,NativeTaskBusy=row.ScanInProgress||row.PressureBusy,Actor=row.Actor,World=row.World,
                GroupId=row.GroupId,SetupHash=row.SetupHash,Airborne=row.Airborne,Rolling=row.Rolling,
                Returning=row.Returning,HomeKnown=row.HomeKnown,AtHome=row.AtHome,Paused=row.Paused,PooledFactionFuel=row.PooledFactionFuel,
                ReturnRevision=row.ReturnRevision,RequestFrame=row.RequestFrame,RequestId=row.RequestId,
                ReturnContactTarget=row.CompletedTarget,AcceptedReturnContact=SkirmishPresentedAircraftServicePolicy.ContactMatches(row)};
        }
    }
    public static class SkirmishPresentedAircraftServicePolicy
    {
        public static bool ContactMatches(UiAircraftServiceModel row)=>
            row!=null&&row.Visible&&row.HasReceipt&&row.CompletedTarget!=0&&
            row.RequestId>0&&row.CompletedFrame>=row.RequestFrame&&row.CompletedFrame-row.RequestFrame<=2&&
            Vector2.Distance(row.RequestScreen,row.CompletedScreen)<=2;
    }
}
