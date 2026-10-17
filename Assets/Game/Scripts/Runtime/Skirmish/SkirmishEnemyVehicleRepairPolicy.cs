using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    // Own-faction recovery adapter. Paid work, threat visibility, exact quotes,
    // refunds and health remain exclusively owned by the shared repair service.
    public static class SkirmishEnemyVehicleRepairPolicy
    {
        public static HashSet<uint> ReservedGroups(EntityManager em,Entity session)
        {
            var result=new HashSet<uint>();
            if(!em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session))return result;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),
                ComponentType.ReadOnly<SkirmishArmyGroupMembershipComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            using var actors=query.ToEntityArray(Allocator.Temp);
            foreach(var actor in actors)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(own.SessionId!=attempt||own.FactionId!=2)continue;
                bool reserved=em.HasComponent<VehicleRepairRequestComponent>(actor)&&em.GetComponentData<VehicleRepairRequestComponent>(actor).Session==session;
                if(em.HasComponent<VehicleRepairStateComponent>(actor))
                {
                    var state=em.GetComponentData<VehicleRepairStateComponent>(actor);
                    reserved|=state.Session==session&&state.Phase==VehicleRepairPhase.Working;
                }
                if(reserved)result.Add(em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId);
            }
            return result;
        }
        public static int Step(EntityManager em,Entity session,SkirmishEnemyPerceptionBuilder.Snapshot facts,int budget,out HashSet<uint> reserved)
        {
            reserved=ReservedGroups(em,session);int dispatched=0;
            if(facts==null||budget<=0||!SkirmishEnemyPlannerService.TryReadActiveClock(em,session,out _)||!TryPad(em,session,out var pad))return 0;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            // Deterministic own-group/actor ordering, independent of Entity order.
            var groups=new List<SkirmishEnemyPerceptionBuilder.Group>(facts.Groups);groups.Sort((a,b)=>a.Id.CompareTo(b.Id));
            using var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishArmyGroupMembershipComponent),typeof(UnitHealth),typeof(LocalTransform));
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var group in groups)
            {
                if(dispatched>=budget)break;
                if(reserved.Contains(group.Id)||group.Category!=Game.Skirmish.Contracts.SkirmishPopulationCategory.Ground||SkirmishEnemyGroupAllocation.Healthy(group))continue;
                var actors=new List<Entity>();
                foreach(var actor in entities)
                {
                    var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    if(own.SessionId==attempt&&own.FactionId==2&&own.IsStructure==0&&own.CapacityReleased==0&&
                        em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId==group.Id&&
                        SkirmishActorCommandAuthority.IsLivingOwnedActor(em,actor,2))actors.Add(actor);
                }
                if(actors.Count!=group.Members||actors.Count==0)continue;
                // Refund debt is cancelled work. The shared service still blocks
                // another paid Start, but it must not strand a combat group.
                bool debt=false;foreach(var actor in actors)if(em.HasComponent<VehicleRepairStateComponent>(actor))
                {var state=em.GetComponentData<VehicleRepairStateComponent>(actor);if(state.Session==session&&state.Phase==VehicleRepairPhase.AccountingFailure)debt=true;}
                if(debt)continue;
                actors.Sort((a,b)=>System.StringComparer.Ordinal.Compare(em.GetComponentData<SkirmishAttemptOwnedComponent>(a).StableObjectId.ToString(),
                    em.GetComponentData<SkirmishAttemptOwnedComponent>(b).StableObjectId.ToString()));
                bool allNear=true;
                foreach(var actor in actors)if(math.distancesq(em.GetComponentData<LocalTransform>(actor).Position.xz,pad.xz)>144)allNear=false;
                if(!allNear)
                {
                    // Exactly the normal group command validator, with no direct
                    // transform, path, health, Fuel or stock writes.
                    bool already=true;
                    foreach(var actor in actors)
                    {
                        if(!em.HasComponent<SkirmishMoveIntentComponent>(actor)){already=false;break;}
                        var move=em.GetComponentData<SkirmishMoveIntentComponent>(actor);
                        if(move.Order!=Game.Skirmish.Contracts.SkirmishGroupOrderKind.Move||
                            move.DestinationX!=pad.x||move.DestinationZ!=pad.z){already=false;break;}
                    }
                    if(already||SkirmishArmyCommandService.TryIssueGroupOrder(em,session,group.Id,2,
                        Game.Skirmish.Contracts.SkirmishGroupOrderKind.Move,Entity.Null,pad,out _))
                    {reserved.Add(group.Id);if(!already)dispatched++;}
                    continue;
                }
                foreach(var actor in actors)
                {
                    if(dispatched>=budget)break;
                    if(VehicleFieldRepairService.TryQuote(em,session,actor,2,out var quote,out _)&&
                        VehicleFieldRepairService.TryEnqueueQuoted(em,session,actor,2,in quote,out _))
                    {reserved.Add(group.Id);dispatched++;continue;}
                    // A normal retreat order may still be attached at its actual
                    // arrival. Stop through the same Hold command, then re-quote at
                    // a later policy decision; Hold itself grants no paid work.
                    if(em.HasComponent<ManualMoveOrderTag>(actor)&&SkirmishArmyCommandService.TryIssueGroupOrder(em,session,group.Id,2,
                        Game.Skirmish.Contracts.SkirmishGroupOrderKind.Hold,Entity.Null,default,out _))
                    {reserved.Add(group.Id);dispatched++;break;}
                }
            }
            return dispatched;
        }
        public static void RemoveReserved(SkirmishEnemyPerceptionBuilder.Snapshot facts,HashSet<uint> reserved)
        {for(int i=facts.Groups.Count-1;i>=0;i--)if(reserved.Contains(facts.Groups[i].Id)){facts.CombatSupply-=facts.Groups[i].Supply;facts.Groups.RemoveAt(i);}}
        static bool TryPad(EntityManager em,Entity session,out float3 pad)
        {
            pad=default;
            if(!em.Exists(session)||!em.HasComponent<SkirmishLaunchDefinitionRecord>(session)||!em.HasComponent<SkirmishResolvedSetupRecord>(session)||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session))return false;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);var launch=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
            if(em.HasComponent<SkirmishObjectiveStateComponent>(session))
            {
                var objective=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
                if(objective.Terminal!=0||objective.Outcome!=Game.Skirmish.Contracts.SkirmishOutcomeKind.None||
                    objective.State!=Game.Skirmish.Contracts.SkirmishObjectiveStateKind.Active)return false;
            }
            if(native.Phase!=Game.Skirmish.Contracts.SkirmishSessionPhase.Playing||native.SpawnComplete==0||launch==null||
                !launch.Matches(em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup,native)||
                !launch.Definition.MapLayout.TryGetPad(2,SkirmishLegalPadKind.Service,out var typed)||
                !launch.Definition.MapLayout.TryProjectLocalToMap(typed.CenterX,typed.CenterZ,out float x,out float z))return false;
            using var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishStructureIdentityComponent),typeof(UnitHealth),typeof(Faction),typeof(LocalTransform));
            using var values=query.ToEntityArray(Allocator.Temp);
            foreach(var facility in values)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(facility);
                if(own.SessionId!=native.SessionId||own.FactionId!=2||own.IsStructure==0||own.CapacityReleased!=0||
                    em.GetComponentData<Faction>(facility).Id!=2||em.GetComponentData<UnitHealth>(facility).Current<=0||
                    em.HasComponent<Parent>(facility)||em.HasComponent<UnitDeathAnimationComponent>(facility)||
                    !em.HasComponent<SkirmishSharedActorTag>(facility)||!math.all(math.isfinite(em.GetComponentData<LocalTransform>(facility).Position))||
                    em.GetComponentData<SkirmishStructureIdentityComponent>(facility).StructureId.ToString()!=Game.Skirmish.Contracts.SkirmishStructureIds.GroundStaging)continue;
                pad=new float3(x,0,z);return math.all(math.isfinite(pad));
            }
            return false;
        }
    }
}
