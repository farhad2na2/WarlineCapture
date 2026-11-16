using System.Linq;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class SkirmishWorldSetup
    {
        internal static SkirmishPresetRuntimeValues ProjectNativePreset(SkirmishPresetConfig preset)
        {
            var source = preset.buildingPlacement.InitialUnitsConfig;
            var initial = new InitialUnitsSpawnConfig
            {
                BlockerCount = source.BlockerCount,
                SpawnRadiusCells = source.SpawnRadiusCells,
                RespawnDelaySeconds = source.RespawnDelaySeconds,
                RandomSeed = source.RandomSeed,
                InitialDollars = source.InitialDollars,
                InitialMaterials = source.InitialMaterials,
                MaterialsCapacity = source.MaterialsCapacity,
                InitialAiMaterials = source.InitialAiMaterials,
                AiMaterialsCapacity = source.AiMaterialsCapacity,
                InitialOil = source.InitialOil,
                InitialFuel = source.InitialFuel,
                CreateFactionBases = source.CreateFactionBases ? (byte)1 : (byte)0,
                BaseHalfWidthCells = source.BaseHalfWidthCells,
                BaseHalfHeightCells = source.BaseHalfHeightCells,
                BaseMinimumUnitsPerFaction = source.BaseMinimumUnitsPerFaction,
                BaseWallPrefabLookupKey = source.BaseWallPrefab != null ? source.BaseWallPrefab.name : default,
                BaseGatePrefabLookupKey = source.BaseGatePrefab != null ? source.BaseGatePrefab.name : default,
                BaseCoreBuildingPrefabLookupKey = source.BaseCoreBuildingPrefab != null ? source.BaseCoreBuildingPrefab.name : default
            };
            var factions = source.Factions.Select(faction => new InitialUnitsFactionSpawnEntry
            {
                FactionId = checked((byte)faction.FactionId),
                SpawnCell = new int2(faction.SpawnCell.x, faction.SpawnCell.y)
            }).ToArray();
            return SkirmishPresetRuntimeProjection.FromNativePreset(
                initial, factions, preset.reinforcementInfantryTarget, preset.attackSquadSize);
        }

        public static void SuppressUnselectedStartupConfigs(EntityManager em)
        {
            SkirmishStartupConfigSuppression.SuppressUnselectedStartupConfigs(em);
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
            SkirmishPresetRuntimeValues values = ProjectNativePreset(preset);
            using(var query=em.CreateEntityQuery(typeof(AIBuildPlan)))
            {
                using var entities=query.ToEntityArray(Allocator.Temp);
                foreach(var entity in entities)
                {
                    var plan=em.GetComponentData<AIBuildPlan>(entity);
                    foreach(var faction in values.Factions)
                        if(faction.FactionId==plan.FactionId)
                            plan.BaseCenterCell=faction.SpawnCell;
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
                    plan.TargetProducedUnits=values.ReinforcementInfantryTarget;
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
                    plan.MinUnits=plan.MaxUnits=math.clamp(values.AttackSquadSize,4,8);
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
            SkirmishPresetRuntimeValues values = ProjectNativePreset(preset);
            var initial=values.InitialUnits;
            FactionTacticalMaterialsStartupSystemHelper.ApplyInitialResourceTotalsFromSource(em,new InitialUnitsSpawnConfig
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
