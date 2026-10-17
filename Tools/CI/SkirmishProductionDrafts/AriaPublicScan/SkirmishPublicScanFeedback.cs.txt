using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class SkirmishPublicScanFeedback
    {
        public static void ObserveAccepted(EntityManager em,in RtsSelectionCommandIntentRequestElement request,in ScanIntelCommandSystem.Result result)
        {
            try
            {
                if(!result.CommandResult.Accepted||!result.HasSourceEntity||!result.DeferredToSource||
                    request.Kind!=RtsSelectionCommandIntentKind.Scan||request.RequestId<=0||request.Frame<0||request.HasScreenPosition==0||
                    !math.all(math.isfinite(request.ScreenPosition))||!math.all(math.isfinite(result.CenterWorld))||
                    !TrySource(em,result.SourceEntity,out var session,out var attempt,out var own,out uint group)||
                    !em.HasComponent<UnitScanOrder>(result.SourceEntity))return;
                var order=em.GetComponentData<UnitScanOrder>(result.SourceEntity);
                if(order.RequestId!=request.RequestId||order.SourceEntity!=result.SourceEntity||!order.CenterCell.Equals(result.CenterCell))return;
                uint prior=em.HasComponent<SkirmishPublicScanReceipt>(result.SourceEntity)?em.GetComponentData<SkirmishPublicScanReceipt>(result.SourceEntity).Revision:0;
                if(prior==uint.MaxValue)return;
                var row=new SkirmishPublicScanReceipt{Session=session,Actor=result.SourceEntity,Attempt=attempt.SessionId,StableId=own.StableObjectId,
                    World=em.WorldUnmanaged.SequenceNumber,SetupHash=attempt.SetupHash,GroupId=group,Revision=prior+1,RequestId=request.RequestId,
                    Frame=request.Frame,CenterCell=result.CenterCell,CenterWorld=result.CenterWorld,RadiusCells=result.RadiusCells,Screen=request.ScreenPosition};
                if(em.HasComponent<SkirmishPublicScanReceipt>(result.SourceEntity))em.SetComponentData(result.SourceEntity,row);else em.AddComponentData(result.SourceEntity,row);
            }
            catch(System.Exception){ /* Optional observation cannot alter normal Scan. */ }
        }
        // Owner-thread callback only after the joined native job chose its real
        // duration-complete branch, before that branch's ECB removes the order.
        public static void ObserveCompleted(EntityManager em,Entity actor,int requestId)
        {
            try
            {
                if(!TryRead(em,actor,out var row)||row.RequestId!=requestId||!em.HasComponent<UnitScanOrder>(actor))return;
                var order=em.GetComponentData<UnitScanOrder>(actor);
                if(order.RequestId!=requestId||order.SourceEntity!=actor||order.HasStarted==0||!order.CenterCell.Equals(row.CenterCell)||
                    !math.isfinite(order.DurationSeconds)||order.DurationSeconds<=0||!math.isfinite(order.StartedTimeSeconds)||
                    em.World.Time.ElapsedTime-order.StartedTimeSeconds<order.DurationSeconds)return;
                row.Completed=1;em.SetComponentData(actor,row);
            }
            catch(System.Exception){ }
        }
        static bool TrySource(EntityManager em,Entity actor,out Entity session,out SkirmishExpandedSessionComponent attempt,
            out SkirmishAttemptOwnedComponent own,out uint group)
        {
            if(!SkirmishPublicAircraftReturnFeedback.TryReadActor(em,actor,out session,out attempt,out own,out group))return false;
            return em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role==SkirmishRoleKind.Drone;
        }
        public static bool TryRead(EntityManager em,Entity actor,out SkirmishPublicScanReceipt row)
        {
            row=default;
            if(!TrySource(em,actor,out var session,out var attempt,out var own,out uint group)||!em.HasComponent<SkirmishPublicScanReceipt>(actor))return false;
            row=em.GetComponentData<SkirmishPublicScanReceipt>(actor);
            return row.Session==session&&row.Actor==actor&&row.Attempt==attempt.SessionId&&row.StableId==own.StableObjectId&&
                row.World==em.WorldUnmanaged.SequenceNumber&&row.SetupHash==attempt.SetupHash&&row.GroupId==group&&row.Revision>0&&row.RequestId>0&&
                row.Frame>=0&&row.RadiusCells>0&&math.all(math.isfinite(row.Screen))&&math.all(math.isfinite(row.CenterWorld));
        }
    }
}
