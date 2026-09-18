using Game.Components;
using Unity.Collections;
using Unity.Entities;
namespace Game.Runtime
{
    public static class SkirmishSquadAssignment
    {
        public static void Assign(EntityManager em)
        {
            using var query=em.CreateEntityQuery(new EntityQueryDesc {
                All=new[]{ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitHealth>(),ComponentType.ReadOnly<UnitSourcePrefabKey>()},
                None=new[]{ComponentType.ReadOnly<OperationMapBuildingComponent>(),ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>()}});
            using var entities=query.ToEntityArray(Allocator.Temp);
            var counts=new int[4];
            foreach(var entity in entities)
                if(em.HasComponent<SkirmishSquadMember>(entity)&&em.GetComponentData<UnitHealth>(entity).Current>0)
                {int slot=em.GetComponentData<SkirmishSquadMember>(entity).Slot;if(slot<4)counts[slot]++;}
            foreach(var entity in entities)
            {
                if(em.GetComponentData<Faction>(entity).Id!=1||em.GetComponentData<UnitHealth>(entity).Current<=0||em.HasComponent<SkirmishSquadMember>(entity))continue;
                string key=em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString().ToLowerInvariant();
                if(key.Contains("light_armored_car")){em.AddComponentData(entity,new SkirmishSquadMember{Slot=4});continue;}
                if(!key.Contains("soldier"))continue;
                byte slot = 0;
                while (slot < 3 && counts[slot] >= 4) slot++;
                // Keep the four quick-select groups bounded as the army grows.
                // Existing members never jump cards; only new recruits fill gaps.
                if (counts[slot] >= 4)
                    for (byte candidate = 0; candidate < 4; candidate++)
                        if (counts[candidate] < counts[slot]) slot = candidate;
                counts[slot]++;em.AddComponentData(entity,new SkirmishSquadMember{Slot=slot});
            }
        }
    }
}
