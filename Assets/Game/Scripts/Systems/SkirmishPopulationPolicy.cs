using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace Game.Runtime
{
    internal static class SkirmishPopulationPolicy
    {
        public static bool CanQueue(EntityManager em,IReadOnlyDictionary<int,RuntimeBuildingEntity> buildings,RuntimeBuildingEntity producer,GameObject prefab,int productionIndex)
        {
            using var session=em.CreateEntityQuery(typeof(SkirmishMatchState));
            if(session.IsEmptyIgnoreFilter)return true;
            if(session.GetSingleton<SkirmishMatchState>().Phase!=SkirmishPhase.Playing||producer==null||prefab==null)return false;
            string key=prefab.name.ToLowerInvariant();
            int limit=key.Contains("soldier")?SkirmishPresetConfig.InfantryLimitPerFaction:key.Contains("truck_tray")?2:key.Contains("truck_tanker")?1:0;
            if(limit==0)return false;
            int count=0;byte faction=producer.OwnerFactionId;
            using var units=em.CreateEntityQuery(typeof(Faction),typeof(UnitHealth),typeof(UnitSourcePrefabKey));
            using var entities=units.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)
                if(em.GetComponentData<Faction>(entity).Id==faction&&em.GetComponentData<UnitHealth>(entity).Current>0&&
                   em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString().ToLowerInvariant().Contains(key))count++;
            if(buildings!=null)foreach(var pair in buildings)
            {
                var building=pair.Value;
                if(building?.PendingProductions==null||building.OwnerFactionId!=faction)continue;
                foreach(var pending in building.PendingProductions)
                    if(pending.Prefab==prefab)count+=Mathf.Max(1,pending.RemainingQuantity);
            }
            int quantity=BuildingDefinitionPrefabSystemHelper.GetProductionQuantity(producer.Definition,productionIndex);
            return count+Mathf.Max(1,quantity)<=limit;
        }
    }
}
