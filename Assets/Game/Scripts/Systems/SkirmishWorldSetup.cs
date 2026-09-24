using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class SkirmishWorldSetup
    {
        public static void SuppressUnselectedStartupConfigs(EntityManager em)
        {
            using var session=em.CreateEntityQuery(typeof(SkirmishMatchState));
            if(session.IsEmptyIgnoreFilter)return;
            using var expanded=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            bool authoredRoster = expanded.CalculateEntityCount() == 1 &&
                expanded.GetSingleton<SkirmishExpandedSessionComponent>().IsLegacy == 0;
            // A streamed SubScene may arrive after managed startup has projected
            // the selected preset. Keep its rendering registry, but never let its
            // legacy spawner add a second force, fuel seed, or blocker churn.
            // Expanded missions author both armies themselves, so the selected
            // legacy spawn config must also be suppressed.
            using var query=em.CreateEntityQuery(new EntityQueryDesc{
                All=new[]{ComponentType.ReadOnly<InitialUnitsSpawnConfig>()},
                None=authoredRoster ? System.Array.Empty<ComponentType>() :
                    new[]{ComponentType.ReadOnly<CustomGameStartupStateComponent>()}});
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)
            {
                em.RemoveComponent<InitialUnitsSpawnConfig>(entity);
                if(em.HasComponent<InitialUsableFuelStorageSeedPending>(entity))em.RemoveComponent<InitialUsableFuelStorageSeedPending>(entity);
                if(em.HasComponent<InitialUnitsBlockerChurnConfig>(entity))em.RemoveComponent<InitialUnitsBlockerChurnConfig>(entity);
            }
        }

        public static void NormalizeScenery(EntityManager em)
        {
            using var query=em.CreateEntityQuery(new EntityQueryDesc
            {
                All=new[]{ComponentType.ReadOnly<Faction>()},
                Any=new[]{ComponentType.ReadOnly<OperationMapBuildingComponent>(),ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>()},
                None=new[]{ComponentType.ReadOnly<SkirmishSceneryNormalized>()}
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
                if(em.HasComponent<SkirmishSquadMember>(entity))em.RemoveComponent<SkirmishSquadMember>(entity);
                em.AddComponent<SkirmishSceneryNormalized>(entity);
            }
        }
        public static void SeedSupplyAndAnchors(EntityManager em)
        {
            var preset=SkirmishPresetResolver.Load(em);
            using(var query=em.CreateEntityQuery(typeof(AIBuildPlan)))
            {
                using var entities=query.ToEntityArray(Allocator.Temp);
                foreach(var entity in entities)
                {
                    var plan=em.GetComponentData<AIBuildPlan>(entity);
                    foreach(var faction in preset.buildingPlacement.InitialUnitsConfig.Factions)
                        if(faction.FactionId==plan.FactionId)
                            plan.BaseCenterCell=new int2(faction.SpawnCell.x,faction.SpawnCell.y);
                    em.SetComponentData(entity,plan);
                }
            }
            using(var query=em.CreateEntityQuery(typeof(AIProductionPlan)))
            {
                using var entities=query.ToEntityArray(Allocator.Temp);
                foreach(var entity in entities)
                {
                    var plan=em.GetComponentData<AIProductionPlan>(entity);
                    if(plan.FactionId!=2)continue;
                    plan.TargetProducedUnits=preset.reinforcementInfantryTarget;
                    em.SetComponentData(entity,plan);
                }
            }
            using(var query=em.CreateEntityQuery(typeof(AISquadPlan)))
            {
                using var entities=query.ToEntityArray(Allocator.Temp);
                foreach(var entity in entities)
                {
                    var plan=em.GetComponentData<AISquadPlan>(entity);
                    if(plan.FactionId!=2)continue;
                    // Assemble a meaningful wave instead of feeding four recruits
                    // at a time into the player's complete starting defense.
                    plan.MinUnits=plan.MaxUnits=math.clamp(preset.attackSquadSize,4,8);
                    em.SetComponentData(entity,plan);
                }
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
            var preset=SkirmishPresetResolver.Load(em);
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
