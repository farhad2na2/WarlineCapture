using System.Collections.Generic;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    public static class SkirmishNativeObjectiveDeparture
    {
        // The objective tick owner first steps the actual reducer and commits its
        // collected facts. This operation never decides success from a caller flag.
        // It consumes the reducer's exact original-ID departure state, removes the
        // healthy native hierarchy, and releases capacity without death/loss/grants.
        public static bool TryRemoveSuccessfulOriginal(EntityManager em,Entity session,string originalId,out string error)
        {
            error="Committed original departure and exact native objective owner required.";
            if(!em.Exists(session)||!em.HasComponent<SkirmishNativeObjectiveRecord>(session)||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session)||!em.HasComponent<SkirmishCapacityComponent>(session))return false;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(attempt.IsLegacy!=0||attempt.Phase!=SkirmishSessionPhase.Playing||
                (attempt.ObjectiveKind!=SkirmishObjectiveKind.Breakthrough&&attempt.ObjectiveKind!=SkirmishObjectiveKind.ConvoyEscort))return false;
            var record=em.GetComponentObject<SkirmishNativeObjectiveRecord>(session);if(record?.Collector==null)return false;
            Entity actor=Entity.Null;
            using(var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent)))
            using(var entities=query.ToEntityArray(Allocator.Temp))foreach(var candidate in entities)
            {
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(candidate);
                if(owned.SessionId!=attempt.SessionId||owned.StableObjectId.ToString()!=originalId)continue;
                if(actor!=Entity.Null){error="Duplicate departure original ID.";return false;}actor=candidate;
            }
            if(actor==Entity.Null||!record.Collector.IsOriginalEntity(actor)||!em.HasComponent<SkirmishUnitRoleComponent>(actor)||
                !em.HasComponent<UnitHealth>(actor)||em.GetComponentData<UnitHealth>(actor).Current<=0||
                em.HasComponent<UnitTransportPassenger>(actor)||em.HasComponent<UnitTransportCargoPassenger>(actor))return false;
            var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
            if(owner.FactionId!=1||owner.IsStructure!=0||owner.ReservationId!=0||owner.CapacityReleased!=0)return false;
            var capacity=em.GetComponentData<SkirmishCapacityComponent>(session);
            bool bt=attempt.ObjectiveKind==SkirmishObjectiveKind.Breakthrough;
            if(bt)
            {
                if(role.Role!=SkirmishRoleKind.Rifle||role.Category!=SkirmishPopulationCategory.Infantry||role.SupplyCost!=1||
                    capacity.InfantryLive<1||capacity.SupplyLive<role.SupplyCost)return false;
                capacity.InfantryLive--;capacity.SupplyLive-=role.SupplyCost;
            }
            else if(role.Role!=SkirmishRoleKind.ConvoyLogic||role.Category!=SkirmishPopulationCategory.ObjectiveSupport||role.SupplyCost!=0)return false;
            var destroy=new HashSet<Entity>();
            if(!GatherOwnedHierarchy(em,actor,actor,attempt.SessionId,destroy,out error))return false;
            // All structural/capacity checks precede the collector's irreversible
            // success tombstone. The collector independently checks actual area,
            // native identity and the reducer's original departure record.
            bool accepted=bt?record.Collector.TryRecordEvacuated(em,session,originalId,record.Breakthrough,out error):
                record.Collector.TryRecordDelivered(em,session,originalId,record.Convoy,out error);
            if(!accepted)return false;
            owner.CapacityReleased=1;em.SetComponentData(actor,owner);
            if(bt)em.SetComponentData(session,capacity);
            if(em.HasBuffer<SkirmishTrackedUnit>(session))
            {
                var tracked=em.GetBuffer<SkirmishTrackedUnit>(session);
                for(int i=tracked.Length-1;i>=0;i--)if(destroy.Contains(tracked[i].Entity))tracked.RemoveAtSwapBack(i);
            }
            var removal=new NativeArray<Entity>(destroy.Count,Allocator.Temp);
            try
            {
                int index=0;foreach(var entity in destroy)removal[index++]=entity;
                em.DestroyEntity(removal);
            }
            finally{removal.Dispose();}
            error=null;return true;
        }

        private static bool GatherOwnedHierarchy(EntityManager em,Entity root,Entity current,FixedString64Bytes attempt,
            HashSet<Entity> result,out string error)
        {
            error="Departure hierarchy includes a missing, prefab, foreign or unrelated actor.";
            if(!em.Exists(current)||em.HasComponent<Prefab>(current))return false;
            if(!result.Add(current)){error=null;return true;}
            if(em.HasComponent<SkirmishAttemptOwnedComponent>(current))
            {
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(current);
                if(owned.SessionId!=attempt||current!=root)return false;
            }
            if(current!=root&&em.HasComponent<UnitHealth>(current))return false;
            if(em.HasBuffer<Child>(current))
            {
                var children=em.GetBuffer<Child>(current);
                for(int i=0;i<children.Length;i++)
                {
                    Entity child=children[i].Value;
                    if(!em.Exists(child)||!em.HasComponent<Parent>(child)||em.GetComponentData<Parent>(child).Value!=current||
                        !GatherOwnedHierarchy(em,root,child,attempt,result,out error))return false;
                }
            }
            if(em.HasBuffer<LinkedEntityGroup>(current))
            {
                var linked=em.GetBuffer<LinkedEntityGroup>(current);
                for(int i=0;i<linked.Length;i++)if(!GatherOwnedHierarchy(em,root,linked[i].Value,attempt,result,out error))return false;
            }
            error=null;return true;
        }
    }
}
