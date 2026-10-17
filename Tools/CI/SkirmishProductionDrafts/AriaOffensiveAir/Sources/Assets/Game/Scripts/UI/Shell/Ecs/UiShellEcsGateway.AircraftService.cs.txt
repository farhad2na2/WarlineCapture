using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiAircraftServiceGateway
    {
        bool IUiAircraftServiceGateway.TryReadSelectedAircraftService(out UiAircraftServiceModel model)
        {
            model=null;
            if(!TrySkirmish(out var em,out var session,out _))return false;
            using var selected=em.CreateEntityQuery(typeof(SelectedUnitTag));
            using var values=selected.ToEntityArray(Allocator.Temp);
            // A focused actor cannot conceal an extra selected actor. Multi-air
            // commands remain legal normally but cannot certify this single row.
            if(values.Length!=1)return false;
            var actor=values[0];
            if(!SkirmishPublicAircraftReturnFeedback.TryReadActor(em,actor,out var actualSession,out var attempt,out var own,out uint group)||actualSession!=session)return false;
            var air=em.GetComponentData<UnitAirComponent>(actor);
            model=new UiAircraftServiceModel {Visible=true,Attempt=attempt.SessionId.ToString(),StableId=own.StableObjectId.ToString(),
                Actor=new UiEntityHandle(actor.Index,actor.Version),World=em.WorldUnmanaged.SequenceNumber,SetupHash=attempt.SetupHash,
                GroupId=group,Airborne=air.Airborne!=0,Returning=air.ReturningHome!=0,
                Rolling=air.TakeoffRolling!=0||air.LandingRolling!=0,HomeKnown=air.HomeInitialized!=0,
                AtHome=em.HasComponent<UnitGrid>(actor)&&em.GetComponentData<UnitGrid>(actor).Cell.Equals(air.HomeCell),
                PooledFactionFuel=SkirmishStartingSupplyService.ReadFuel(em,session,1)};
            if(em.HasComponent<SkirmishObjectiveClockComponent>(session))model.Paused=em.GetComponentData<SkirmishObjectiveClockComponent>(session).Paused!=0;
            if(SkirmishPublicAircraftReturnFeedback.TryReadReceipt(em,actor,out var receipt))
            {
                model.HasReceipt=true;model.ReturnRevision=receipt.Revision;model.RequestId=receipt.RequestId;model.RequestFrame=receipt.Frame;model.RequestScreen=receipt.Screen;
            }
            if(TryGetBoundary(out var uiEm,out var boundary)&&uiEm.HasComponent<AriaPlaySessionComponent>(boundary))
            {
                var touch=uiEm.GetComponentData<AriaPlaySessionComponent>(boundary);
                model.CompletedTarget=touch.LastCompletedTargetId;model.CompletedFrame=touch.LastCompletedFrame;model.CompletedScreen=touch.LastCompletedPosition;
            }
            model.CanScan=em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role==Game.Skirmish.Contracts.SkirmishRoleKind.Drone&&SelectionUiReadModelLookup.IsSelectedUnitScanCapable(em,actor);
            if(SkirmishPublicScanFeedback.TryRead(em,actor,out var scan))
            {model.ScanRevision=scan.Revision;model.ScanRequestId=scan.RequestId;model.ScanFrame=scan.Frame;model.ScanScreen=scan.Screen;model.ScanCenter=scan.CenterWorld;model.ScanCompleted=scan.Completed!=0;}
            model.Role=PublicAirRole(em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role);
            model.ScanInProgress=em.HasComponent<UnitScanOrder>(actor);
            model.OffensiveEligible=SkirmishPublicAirPressureFeedback.Eligible(em,actor);
            model.PressureBusy=em.HasComponent<AttackMoveOrder>(actor)||em.HasComponent<EngageTarget>(actor)||em.HasComponent<BaseBreachOrder>(actor);
            if(SkirmishPublicAirPressureFeedback.TryRead(em,actor,out var pressure))
            {model.PressureRevision=pressure.Revision;model.PressureFrame=pressure.Frame;model.PressureScreen=pressure.Screen;model.PressureGoal=pressure.Position;}
            bool fa=GameLocalization.IsRightToLeft;
            string state=model.Returning?(fa?"در حال بازگشت":"RETURNING"):model.Rolling?(fa?"در حال حرکت روی باند":"ROLLING"):
                model.Airborne?(fa?"در پرواز":"AIRBORNE"):(fa?"پارک شده":"PARKED");
            if(model.ScanInProgress&&model.ScanRevision==0)state=(fa?"در حال پایش":"SCOUTING")+" · "+state;
            if(model.ScanRevision>0)state=(model.ScanCompleted?(fa?"پایش انجام شد":"SCAN COMPLETE"):(fa?"در حال پایش":"SCOUTING"))+" · "+state;
            model.Status=state+(fa?" · سوخت مشترک نیرو ":" · FACTION FUEL ")+model.PooledFactionFuel;
            if(model.OffensiveEligible)model.Status+=(fa?" · رزمی":" · COMBAT")+(model.PressureBusy?(fa?" · در حال ماموریت":" · ASSIGNED"):"");
            return true;
        }
    }
}
