using Game.Components;
using Game.Tactical.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    public static class SkirmishEnemyCapabilityCommands
    {
        public static bool TryScan(EntityManager em, Entity session, Entity source, float3 destination,
            out int requestId, out TacticalCommandReasonCode reason)
        {
            requestId=0;reason=TacticalCommandReasonCode.ScanUnavailable;
            if(!SkirmishActorCommandAuthority.IsActiveOwnedActor(em,session,2,source) ||
               !em.HasComponent<UnitMove>(source) || !SelectionUiReadModelLookup.IsSelectedUnitScanCapable(em,source) ||
               !SkirmishFogService.TryGetActiveScanObserverFaction(em,source,out byte faction) || faction!=2 ||
               em.HasComponent<UnitScanOrder>(source) || !math.all(math.isfinite(destination))) return false;
            using var grids=em.CreateEntityQuery(typeof(GridConfig));
            if(grids.CalculateEntityCount()!=1)return false;
            var grid=grids.GetSingleton<GridConfig>();var cell=GridUtils.WorldToCell(grid,destination);
            if(!GridUtils.InBounds(cell,grid.Width,grid.Height))return false;
            using var queues=em.CreateEntityQuery(typeof(ScanIntelCommandQueueComponent));
            if(queues.CalculateEntityCount()>1)return false;
            int last=queues.IsEmptyIgnoreFilter?0:queues.GetSingleton<ScanIntelCommandQueueComponent>().LastRequestId;
            if(last<0 || last==int.MaxValue)return false;
            requestId=last+1;
            int radius=SelectionUiReadModelLookup.IsSelectedUnitScanSpecialist(em,source)
                ?ScanIntelCommandSystem.DefaultScanRadiusCells:ScanIntelCommandSystem.DefaultCombatUnitScanRadiusCells;
            ScanIntelCommandSystem.EnqueueScan(em,requestId,Time.frameCount,cell,destination,source,true,true,radius);
            ScanIntelCommandSystem.ProcessPendingRequests(em);
            if(queues.IsEmptyIgnoreFilter)return false;
            Entity queue=queues.GetSingletonEntity();
            if(!em.HasBuffer<ScanIntelCommandResultElement>(queue))return false;
            var results=em.GetBuffer<ScanIntelCommandResultElement>(queue,true);
            for(int i=results.Length-1;i>=0;i--)
            {
                var result=results[i];
                if(result.RequestId!=requestId || result.SourceEntity!=source || result.HasSourceEntity==0)continue;
                reason=(TacticalCommandReasonCode)result.ReasonCode;return result.Accepted!=0;
            }
            return false;
        }
    }
    public partial struct TransportBoardingCommandSystem
    {
        // Forward plane cargo uses its existing native airdrop owner. This does
        // not create a landing pad, change home/runway or attach/detach cargo.
        public static bool TryIssueOwnedDeploy(EntityManager em,Entity session,byte actingFaction,Entity transport,
            UnitTransportCapacitySystem capacitySystem,UnitMoveOrderSystem moveSystem,EntityQuery gridPathingQuery,
            int2 requestedCell,out TacticalCommandReasonCode reason)
        {
            reason=TacticalCommandReasonCode.InvalidTransport;
            if(!SkirmishActorCommandAuthority.IsActiveOwnedActor(em,session,actingFaction,transport) ||
               !em.HasBuffer<UnitTransportPassengerElement>(transport))return false;
            var passengers=em.GetBuffer<UnitTransportPassengerElement>(transport,true);
            for(int i=0;i<passengers.Length;i++)
            {
                Entity passenger=passengers[i].Passenger;
                if(!SkirmishActorCommandAuthority.IsActiveOwnedActor(em,session,actingFaction,passenger,true) ||
                   !em.HasComponent<UnitTransportPassenger>(passenger) ||
                   em.GetComponentData<UnitTransportPassenger>(passenger).Transport!=transport)
                {reason=TacticalCommandReasonCode.InvalidPassenger;return false;}
            }
            return TryIssueDeployDisembark(em,transport,capacitySystem,moveSystem,gridPathingQuery,requestedCell,
                Entity.Null,default,default,0,out reason);
        }
    }
}
