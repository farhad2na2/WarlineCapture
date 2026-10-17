using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiVehicleRepairGateway
    {
        Entity repairIssuedActor,repairIssuedSession;
        ulong repairIssuedWorld;
        string repairIssuedAttempt,repairIssuedActorId;
        uint repairIssuedRequest;
        int repairIssuedFrame;
        float2 repairIssuedScreen;
        bool repairIssuedScreenKnown,repairIssuedCancel;

        bool IUiVehicleRepairGateway.TryReadVehicleRepair(out UiVehicleRepairModel model)
        {
            model=null;
            if(!TrySkirmish(out var em,out var session,out _)||!em.HasComponent<SkirmishExpandedSessionComponent>(session))return false;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(native.IsLegacy!=0||native.Phase!=Game.Skirmish.Contracts.SkirmishSessionPhase.Playing)return false;
            bool fa=GameLocalization.IsRightToLeft;
            model=new UiVehicleRepairModel{Applicable=true,AttemptId=native.SessionId.ToString()};
            if(!TrySingleRepairActor(em,native.SessionId,out var actor,out int selected,out bool anyVehicle))
            {
                model.Visible=anyVehicle&&selected>1;
                model.ActionLabel=fa?"برای تعمیر یک خودرو را انتخاب کنید.":"REPAIR · Select one vehicle";
                return true;
            }
            var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);var health=em.GetComponentData<UnitHealth>(actor);
            model.Visible=true;model.SingleOwnVehicle=true;model.ActorPublicId=owned.StableObjectId.ToString();
            model.GroupId=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId;
            model.Health=health.Current;model.MaximumHealth=health.Max;
            model.RequestPending=em.HasComponent<VehicleRepairRequestComponent>(actor);
            if(em.HasComponent<SkirmishObjectiveClockComponent>(session))model.Paused=em.GetComponentData<SkirmishObjectiveClockComponent>(session).Paused!=0;
            if(VehicleRepairRules.TryQuote(health.Current,health.Max,out var potential))
            {model.Materials=potential.Materials;model.DurationTicks=potential.DurationTicks;model.FractionNumerator=potential.FractionNumerator;model.FractionDenominator=potential.FractionDenominator;}
            VehicleRepairPlan quote=default;
            model.Eligible=!model.RequestPending&&VehicleFieldRepairService.TryQuote(em,session,actor,1,out quote,out _);
            if(model.Eligible)
            {model.Materials=quote.Materials;model.DurationTicks=quote.DurationTicks;model.FractionNumerator=quote.FractionNumerator;model.FractionDenominator=quote.FractionDenominator;}
            if(em.HasComponent<VehicleRepairStateComponent>(actor))
            {
                var state=em.GetComponentData<VehicleRepairStateComponent>(actor);
                if(state.Session!=session||state.AttemptId!=native.SessionId||state.ActorStableId!=owned.StableObjectId||state.Faction!=1||state.SetupHash!=native.SetupHash)
                {model.Eligible=false;model.Status=fa?"وضعیت تعمیر تأیید نشد.":"Repair state could not be verified.";}
                else
                {
                    model.StateRequestId=state.RequestId;model.PaidMaterials=state.PaidMaterials;model.RemainingTicks=state.RemainingTicks;model.RefundedMaterials=state.RefundedMaterials;
                    model.Phase=state.Phase switch{VehicleRepairPhase.Working=>UiVehicleRepairPhase.Working,VehicleRepairPhase.Completed=>UiVehicleRepairPhase.Completed,
                        VehicleRepairPhase.Cancelled=>UiVehicleRepairPhase.Cancelled,VehicleRepairPhase.Destroyed=>UiVehicleRepairPhase.Destroyed,
                        VehicleRepairPhase.AccountingFailure=>UiVehicleRepairPhase.AccountingFailure,_=>UiVehicleRepairPhase.None};
                    model.Working=state.Phase==VehicleRepairPhase.Working;
                    if(em.HasComponent<SkirmishObjectiveClockComponent>(session))
                    {
                        double age=em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds-state.LastClockSeconds;
                        model.TerminalFeedback=state.Phase==VehicleRepairPhase.AccountingFailure||age>=0&&age<=4&&(state.Phase==VehicleRepairPhase.Completed||state.Phase==VehicleRepairPhase.Cancelled);
                        if(model.TerminalFeedback)model.Eligible=false;
                    }
                }
            }
            if(repairIssuedWorld==em.WorldUnmanaged.SequenceNumber&&repairIssuedSession==session&&repairIssuedActor==actor&&repairIssuedAttempt==model.AttemptId&&repairIssuedActorId==model.ActorPublicId)
            {
                model.IssuedRequestId=repairIssuedRequest;model.IssuedFrame=repairIssuedFrame;model.IssuedScreen=repairIssuedScreen;
                model.IssuedScreenKnown=repairIssuedScreenKnown;model.IssuedCancel=repairIssuedCancel;
                model.StateMatchesIssued=repairIssuedRequest!=0&&model.StateRequestId==repairIssuedRequest&&
                    (repairIssuedCancel?model.Phase==UiVehicleRepairPhase.Cancelled:model.Phase==UiVehicleRepairPhase.Working||model.Phase==UiVehicleRepairPhase.Completed);
                model.RequestRejected=repairIssuedRequest!=0&&!model.RequestPending&&model.StateRequestId<repairIssuedRequest;
            }
            if(TryGetBoundary(out var uiEm,out var boundary)&&uiEm.HasComponent<AriaPlaySessionComponent>(boundary))
            {
                var contact=uiEm.GetComponentData<AriaPlaySessionComponent>(boundary);model.CompletedContactTargetId=contact.LastCompletedTargetId;
                model.CompletedContactFrame=contact.LastCompletedFrame;model.CompletedContactPosition=contact.LastCompletedPosition;
            }
            string seconds=Math.Ceiling((model.Working?model.RemainingTicks:model.DurationTicks)/(double)TimeSpan.TicksPerSecond).ToString(System.Globalization.CultureInfo.InvariantCulture);
            model.ActionLabel=model.RequestPending?(fa?"درخواست تعمیر در حال بررسی است.":"REPAIR REQUEST PENDING"):
                model.Working?(fa?"لغو تعمیر · "+model.PaidMaterials+" مصالح پرداخت شده · "+seconds+" ثانیه":"CANCEL REPAIR · PAID "+model.PaidMaterials+" · "+seconds+" s"):
                model.TerminalFeedback&&model.Phase==UiVehicleRepairPhase.Completed?(fa?"تعمیر انجام شد · سلامت "+model.Health+" / "+model.MaximumHealth:"REPAIR COMPLETE · HEALTH "+model.Health+" / "+model.MaximumHealth):
                model.TerminalFeedback&&model.Phase==UiVehicleRepairPhase.Cancelled?(fa?"تعمیر لغو شد · بازگشت "+model.RefundedMaterials+" مصالح":"REPAIR CANCELLED · REFUNDED "+model.RefundedMaterials+" MATERIALS"):
                model.TerminalFeedback&&model.Phase==UiVehicleRepairPhase.AccountingFailure?(fa?"بازگشت هزینه تعمیر در انتظار است.":"REPAIR REFUND PENDING"):
                model.Eligible?(fa?"تعمیر · "+model.Materials+" مصالح · "+seconds+" ثانیه":"REPAIR · "+model.Materials+" MATERIALS · "+seconds+" s"):
                health.Current>=health.Max?(fa?"بدون آسیب":"UNDAMAGED"):
                model.RequestRejected?(fa?"درخواست تعمیر پذیرفته نشد.":"REPAIR REQUEST REJECTED"):
                (fa?"تعمیر · "+model.Materials+" مصالح · توقف در محل تعمیر خودی و امن":"REPAIR · "+model.Materials+" MATERIALS · Park at a safe own service pad");
            if(!string.IsNullOrEmpty(model.Status))model.ActionLabel=model.Status;
            return true;
        }

        bool IUiVehicleRepairGateway.TryRequestQuotedVehicleRepair(UiVehicleRepairModel shown,bool cancel)
        {
            if(shown==null||!shown.SingleOwnVehicle||!((IUiVehicleRepairGateway)this).TryReadVehicleRepair(out var actual)||!actual.SingleOwnVehicle||
                shown.AttemptId!=actual.AttemptId||shown.ActorPublicId!=actual.ActorPublicId||shown.GroupId!=actual.GroupId||shown.Working!=actual.Working||
                actual.Paused||actual.RequestPending||cancel!=actual.Working||shown.StateRequestId!=actual.StateRequestId||
                !TrySkirmish(out var em,out var session,out _)||!TrySingleRepairActor(em,new FixedString64Bytes(actual.AttemptId),out var actor,out _,out _))return false;
            bool queued;
            if(cancel)queued=VehicleFieldRepairService.TryEnqueue(em,session,actor,1,VehicleRepairAction.Cancel,out _);
            else
            {
                if(!actual.Eligible||!SkirmishPresentedRepairPolicy.SameQuote(shown,actual))return false;
                var expected=new VehicleRepairPlan{Materials=shown.Materials,DurationTicks=shown.DurationTicks,FractionNumerator=shown.FractionNumerator,FractionDenominator=shown.FractionDenominator};
                // This exact quoted successor is required. Never substitute raw Start.
                queued=VehicleFieldRepairService.TryEnqueueQuoted(em,session,actor,1,expected,out _);
            }
            if(!queued||!em.HasComponent<VehicleRepairRequestComponent>(actor))return false;
            repairIssuedActor=actor;repairIssuedSession=session;repairIssuedWorld=em.WorldUnmanaged.SequenceNumber;repairIssuedAttempt=actual.AttemptId;
            repairIssuedActorId=actual.ActorPublicId;repairIssuedRequest=em.GetComponentData<VehicleRepairRequestComponent>(actor).RequestId;
            repairIssuedFrame=UnityEngine.Time.frameCount;repairIssuedCancel=cancel;repairIssuedScreenKnown=false;
            using var pointer=em.CreateEntityQuery(typeof(RtsSelectionInputStateComponent));
            if(pointer.CalculateEntityCount()==1){var state=pointer.GetSingleton<RtsSelectionInputStateComponent>();repairIssuedScreen=state.LastKnownPointerPosition;repairIssuedScreenKnown=state.HasLastKnownPointerPosition!=0;}
            return true;
        }
        static bool TrySingleRepairActor(EntityManager em,FixedString64Bytes attempt,out Entity actor,out int selected,out bool anyVehicle)
        {
            actor=Entity.Null;selected=0;anyVehicle=false;
            using var query=em.CreateEntityQuery(typeof(SelectedUnitTag),typeof(UnitHealth));using var values=query.ToEntityArray(Allocator.Temp);
            foreach(var candidate in values)
            {
                if(em.GetComponentData<UnitHealth>(candidate).Current<=0)continue;selected++;
                bool vehicle=em.HasComponent<UnitMovementBehavior>(candidate)&&em.GetComponentData<UnitMovementBehavior>(candidate).UsesVehicleMotion!=0&&
                    !em.HasComponent<UnitAirComponent>(candidate)&&!em.HasComponent<UnitAirMovement>(candidate);
                anyVehicle|=vehicle;
                if(!vehicle||em.HasComponent<Disabled>(candidate)||em.HasComponent<Prefab>(candidate)||em.HasComponent<UnitDeathAnimationComponent>(candidate)||
                    em.HasComponent<Parent>(candidate)||em.HasComponent<UnitTransportPassenger>(candidate)||em.HasComponent<UnitTransportCargoPassenger>(candidate)||
                    !em.HasComponent<LocalTransform>(candidate)||!em.HasComponent<SkirmishAttemptOwnedComponent>(candidate)||!em.HasComponent<Faction>(candidate)||
                    !em.HasComponent<SkirmishArmyGroupMembershipComponent>(candidate))continue;
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(candidate);
                if(own.SessionId==attempt&&own.FactionId==1&&own.IsStructure==0&&own.CapacityReleased==0&&own.StableObjectId.Length>0&&
                    em.GetComponentData<Faction>(candidate).Id==1)actor=candidate;
            }
            return selected==1&&actor!=Entity.Null;
        }
    }
}
