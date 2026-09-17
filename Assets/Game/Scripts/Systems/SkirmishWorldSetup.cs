using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class SkirmishWorldSetup
    {
        public static void NormalizeScenery(EntityManager em)
        {
            using var query=em.CreateEntityQuery(new EntityQueryDesc
            {
                All=new[]{ComponentType.ReadOnly<Faction>()},
                Any=new[]{ComponentType.ReadOnly<OperationMapBuildingComponent>(),ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>()}
            });
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)
            {
                em.SetComponentData(entity,new Faction{Id=0});
                if(em.HasComponent<RuntimeBuildingCombatInfo>(entity))
                {var v=em.GetComponentData<RuntimeBuildingCombatInfo>(entity);v.OwnerFactionId=0;em.SetComponentData(entity,v);}
                if(em.HasComponent<BuildingResourceStorageComponent>(entity))
                {var v=em.GetComponentData<BuildingResourceStorageComponent>(entity);v.OwnerFactionId=0;em.SetComponentData(entity,v);}
                if(em.HasComponent<MaterialFabricationComponent>(entity))
                {var v=em.GetComponentData<MaterialFabricationComponent>(entity);v.OwnerFactionId=0;em.SetComponentData(entity,v);}
                if(em.HasComponent<OperationMapAuthoredVehiclePresentation>(entity))
                {var v=em.GetComponentData<OperationMapAuthoredVehiclePresentation>(entity);v.FactionId=0;em.SetComponentData(entity,v);}
                if(em.HasComponent<AIControlledTag>(entity))em.RemoveComponent<AIControlledTag>(entity);
            }
        }
        public static void SeedSupplyAndAnchors(EntityManager em)
        {
            using(var query=em.CreateEntityQuery(typeof(AIBuildPlan)))
            {
                using var entities=query.ToEntityArray(Allocator.Temp);
                foreach(var entity in entities){var plan=em.GetComponentData<AIBuildPlan>(entity);plan.BaseCenterCell=new int2(plan.FactionId==1?900:1190,plan.FactionId==1?490:530);em.SetComponentData(entity,plan);}
            }
            using(var query=em.CreateEntityQuery(typeof(BuildingResourceStorageComponent),typeof(UnitSourcePrefabKey)))
            {
                using var entities=query.ToEntityArray(Allocator.Temp);
                foreach(var entity in entities)
                {
                    var storage=em.GetComponentData<BuildingResourceStorageComponent>(entity);
                    if(storage.OwnerFactionId is not (1 or 2)||!em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString().ToLowerInvariant().Contains("fuel_bladder"))continue;
                    storage.StoredFuelBarrels=math.min(storage.FuelStorageCapacity,160);storage.Version++;em.SetComponentData(entity,storage);
                }
            }
        }
        public static void UseTacticalResources(EntityManager em)
        {
            var preset=UnityEngine.Resources.Load<Game.Configs.SkirmishPresetConfig>(Game.Configs.SkirmishPresetConfig.ResourceName);
            var initial=preset.buildingPlacement.InitialUnitsConfig;
            FactionTacticalMaterialsStartupSystemHelper.ApplyInitialResourceTotals(em,new InitialUnitsSpawnConfig
            {InitialMaterials=initial.InitialMaterials,MaterialsCapacity=initial.MaterialsCapacity,InitialAiMaterials=initial.InitialAiMaterials,AiMaterialsCapacity=initial.AiMaterialsCapacity});
            using var query=em.CreateEntityQuery(typeof(FactionEconomy));
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)
            {
                var economy=em.GetComponentData<FactionEconomy>(entity);
                economy.Money=0;economy.MaterialsOnlyConstruction=1;em.SetComponentData(entity,economy);
                if(em.HasComponent<FactionEconomyPolicy>(entity))
                {var policy=em.GetComponentData<FactionEconomyPolicy>(entity);policy.Enabled=0;em.SetComponentData(entity,policy);}
            }
        }
    }
}
