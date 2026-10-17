using Game.UI.Contracts;
using Game.Skirmish.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        void ObserveOffensiveAir(ref AriaSkirmishObservation view)
        {
            if(!view.PublicObjective.Presented||!view.PublicObjective.Supported)return;
            if(UiShellRuntimeGateway.TryReadAirRecruit(out var recruit))
            {
                bool drawerOpen=view.DrawerOpen;
                void TargetFor(ref AriaAirRecruitChoice row,string key)
                {row.Target=drawerOpen?ObserveCatalogTarget(BuildDrawerCategory.Aircrafts,key):ObserveWatchButton(_commandControlsView?.BuildButton);}
                // These are the ordinary catalog buttons/scroll routes. Native
                // quote eligibility never substitutes for a presented touch.
                TargetFor(ref recruit.Light,"Unit_Veh_Helicopter_Attack_Small");TargetFor(ref recruit.Heavy,"Unit_Veh_Helicopter_Attack");
                TargetFor(ref recruit.Fighter,"Unit_Veh_Jet_02");TargetFor(ref recruit.Strike,"Unit_Veh_Jet_01");
                TargetFor(ref recruit.Drone,"Unit_Veh_Drone");TargetFor(ref recruit.TransportHeli,"Unit_Veh_Helicopter_Transport");
                TargetFor(ref recruit.TransportPlane,"Unit_Veh_Plane_Transport");view.AirRecruit=recruit;
            }
            if(watchSelection==null||!watchSelection.TryReadPresentedAircraftService(Time.frameCount,out var model)||
                watchSkirmish==null||!watchSkirmish.TryReadPublicObjectiveForAria(Time.frameCount,out var frame))return;
            string publicId=null;
            for(int i=0;i<frame.Goal.NodeCount;i++)if(PublicObjectiveKey(frame.Goal.Node(i).PublicId)==view.PublicObjective.PublicKey)
            {if(publicId!=null)return;publicId=frame.Goal.Node(i).PublicId;}
            view.OffensiveAir=new AriaOffensiveAirObservation {Presented=true,Eligible=model.OffensiveEligible,Role=model.Role,
                Actor=model.Actor,World=model.World,GroupId=model.GroupId,SetupHash=model.SetupHash,Paused=model.Paused,
                Busy=model.PressureBusy,Returning=model.Returning,Revision=model.PressureRevision,RequestFrame=model.PressureFrame,
                ContactTarget=model.CompletedTarget,AcceptedContact=model.PressureRevision>0&&model.CompletedTarget!=0&&
                    model.CompletedFrame>=model.PressureFrame&&model.CompletedFrame-model.PressureFrame<=2&&
                    Vector2.Distance(model.CompletedScreen,model.PressureScreen)<=2,
                GoalFits=publicId!=null&&watchSkirmish.PresentedPublicScanCenterFits(Time.frameCount,publicId,new Vector2(model.PressureGoal.x,model.PressureGoal.z))};
        }
    }
}
