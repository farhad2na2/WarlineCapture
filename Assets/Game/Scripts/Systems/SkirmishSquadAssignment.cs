using Game.Components;
using Unity.Collections;
using Unity.Entities;
namespace Game.Runtime
{
    public static class SkirmishSquadAssignment
    {
        public static void Assign(EntityManager em)
        {
            using var query=em.CreateEntityQuery(typeof(Faction),typeof(UnitHealth),typeof(UnitSourcePrefabKey));
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
                byte slot=0;while(slot<3&&counts[slot]>=4)slot++;
                counts[slot]++;em.AddComponentData(entity,new SkirmishSquadMember{Slot=slot});
            }
        }
    }
}
