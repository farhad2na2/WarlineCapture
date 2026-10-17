using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    // Active paid repair has no checksum-covered native recovery protocol yet.
    // Refuse capture/application instead of discarding a reservation or debt.
    internal static class SkirmishNativeRepairCheckpointGuard
    {
        internal static bool TryValidateWorld(EntityManager em,Entity session,out string failure)
        {
            failure="NativeRepairSessionMissing";
            if(em==default||!em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session))return false;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            using(var query=em.CreateEntityQuery(new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<VehicleRepairRequestComponent>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState}))
            using(var entities=query.ToEntityArray(Allocator.Temp))
                foreach(var actor in entities)
                {
                    var request=em.GetComponentData<VehicleRepairRequestComponent>(actor);
                    bool owned=em.HasComponent<SkirmishAttemptOwnedComponent>(actor)&&em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).SessionId==native.SessionId;
                    if(request.Session==session||owned){failure="NativeRepairPendingRequestUnsupported";return false;}
                }
            using(var query=em.CreateEntityQuery(new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<VehicleRepairStateComponent>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState}))
            using(var entities=query.ToEntityArray(Allocator.Temp))
                foreach(var actor in entities)
                {
                    var state=em.GetComponentData<VehicleRepairStateComponent>(actor);
                    bool hasOwner=em.HasComponent<SkirmishAttemptOwnedComponent>(actor);
                    var owner=hasOwner?em.GetComponentData<SkirmishAttemptOwnedComponent>(actor):default;
                    if(state.Session!=session&&(!hasOwner||owner.SessionId!=native.SessionId))continue;
                    if(state.Session!=session||!hasOwner||owner.SessionId!=native.SessionId||state.AttemptId!=native.SessionId||
                        owner.StableObjectId!=state.ActorStableId||state.ActorStableId.Length==0||state.PadId.Length==0||
                        state.SetupHash!=native.SetupHash||state.ServiceFacilityStableId.Length==0||!math.all(math.isfinite(state.ServiceFacilityPosition))||
                        owner.IsStructure!=0||owner.FactionId!=state.Faction||state.Faction<1||state.Faction>2||
                        !em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=state.Faction||
                        !Enum.IsDefined(typeof(VehicleRepairPhase),state.Phase)||state.Phase==VehicleRepairPhase.None||
                        state.RequestId==0||state.IncomingDamage>1||state.NewMovement>1||state.PaidMaterials<=0||state.RefundDue<0||state.RefundedMaterials<0||
                        state.RefundedMaterials>state.RefundDue||state.RefundDue>state.PaidMaterials||
                        state.FractionDenominator<=0||state.FractionDenominator>4L*int.MaxValue||
                        state.FractionNumerator<=0||state.FractionNumerator>state.FractionDenominator/4||
                        state.DurationTicks<=0||state.RemainingTicks<0||state.RemainingTicks>state.DurationTicks||
                        !double.IsFinite(state.LastClockSeconds)||state.LastClockSeconds<0||
                        !double.IsFinite(state.FractionalTicks)||state.FractionalTicks<0||state.FractionalTicks>=1||
                        !math.all(math.isfinite(state.AcceptedPosition))||!math.all(math.isfinite(state.PadPosition))||
                        state.LastMaximum<=0||state.LastHealth<0||state.LastHealth>state.LastMaximum)
                    {failure="NativeRepairReceiptIdentityOrSchemaInvalid";return false;}
                    long expectedMaterials=(80*state.FractionNumerator+state.FractionDenominator-1)/state.FractionDenominator;
                    long expectedTicks=(80*TimeSpan.TicksPerSecond*state.FractionNumerator+state.FractionDenominator-1)/state.FractionDenominator;
                    if(state.PaidMaterials!=expectedMaterials||state.DurationTicks!=expectedTicks)
                    {failure="NativeRepairAcceptedQuoteInvalid";return false;}
                    if(state.Phase==VehicleRepairPhase.Working||state.Phase==VehicleRepairPhase.AccountingFailure||
                        state.RefundDue>state.RefundedMaterials)
                    {failure="NativeRepairUnsettledPaidStateUnsupported";return false;}
                    if(state.Phase==VehicleRepairPhase.Completed&&state.RemainingTicks!=0)
                    {failure="NativeRepairCompletedTicksInvalid";return false;}
                    int expectedRefund=state.Phase==VehicleRepairPhase.Cancelled?
                        VehicleRepairRules.Refund(state.PaidMaterials,state.DurationTicks-state.RemainingTicks,false):0;
                    if(state.RefundDue!=expectedRefund||state.RefundedMaterials!=expectedRefund)
                    {failure="NativeRepairSettledRefundInvalid";return false;}
                }
            failure=null;return true;
        }
        internal static bool NoSerializedRepair(SkirmishNativeCheckpointSnapshotService.Snapshot snapshot)
        {
            if(snapshot==null||!NoRepairParts(snapshot.SessionParts))return false;
            if(snapshot.Actors!=null)foreach(var actor in snapshot.Actors)if(actor!=null&&!NoRepairParts(actor.Parts))return false;
            if(snapshot.Banks!=null)foreach(var bank in snapshot.Banks)if(bank!=null&&!NoRepairParts(bank.Parts))return false;
            if(snapshot.MapReferences!=null)foreach(var map in snapshot.MapReferences)if(map!=null&&!NoRepairParts(map.Parts))return false;
            return true;
        }
        private static bool NoRepairParts(SkirmishNativeCheckpointParts.Part[] parts)
        {
            if(parts==null)return true; // Existing codec owns required-container validation.
            foreach(var part in parts)if(part!=null&&(part.Type==typeof(VehicleRepairStateComponent).FullName||
                part.Type==typeof(VehicleRepairRequestComponent).FullName))return false;
            return true;
        }
    }
}
