using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class FactionTacticalMaterialsStartupSystemHelper
    {
        internal static void ApplyInitialResourceTotals(EntityManager em, InitialUnitsSpawnConfig config)
        {
            Entity playerEconomyEntity = Entity.Null;
            using NativeList<FactionMaterialsSeed> materialSeeds = new(Allocator.Temp);
            using (EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<FactionEconomy>()))
            using (NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp))
            {
                EntityTypeHandle entityType = em.GetEntityTypeHandle();
                ComponentTypeHandle<FactionEconomy> economyType = em.GetComponentTypeHandle<FactionEconomy>(false);
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    ArchetypeChunk chunk = chunks[chunkIndex];
                    NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                    NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref economyType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        Entity economyEntity = entities[i];
                        FactionEconomy economy = economies[i];
                        if (FactionIdentity.IsPlayerControlled(economy.FactionId))
                        {
                            economy.Money = math.max(0, config.InitialDollars);
                            economies[i] = economy;
                            playerEconomyEntity = economyEntity;
                        }

                        materialSeeds.Add(new FactionMaterialsSeed(economyEntity, economy.FactionId));
                    }
                }
            }

            for (int i = 0; i < materialSeeds.Length; i++)
            {
                FactionMaterialsSeed seed = materialSeeds[i];
                ApplyInitialMaterials(em, seed.Entity, seed.FactionId, config);
            }

            if (playerEconomyEntity != Entity.Null)
                return;

            playerEconomyEntity = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
            em.SetComponentData(playerEconomyEntity, new FactionEconomy
            {
                FactionId = FactionIdentity.PlayerFactionId,
                Money = math.max(0, config.InitialDollars)
            });
            em.SetComponentData(playerEconomyEntity, new FactionEconomyPolicy
            {
                Enabled = 0,
                IncomeMultiplier = 1f
            });
            ApplyInitialMaterials(em, playerEconomyEntity, FactionIdentity.PlayerFactionId, config);
        }

        private readonly struct FactionMaterialsSeed
        {
            public readonly Entity Entity;
            public readonly byte FactionId;

            public FactionMaterialsSeed(Entity entity, byte factionId)
            {
                Entity = entity;
                FactionId = factionId;
            }
        }

        private static void ApplyInitialMaterials(
            EntityManager em,
            Entity economyEntity,
            byte factionId,
            in InitialUnitsSpawnConfig config)
        {
            bool isPlayerControlled = FactionIdentity.IsPlayerControlled(factionId);
            int configuredAiCapacity = math.max(0, config.AiMaterialsCapacity);
            int capacity = isPlayerControlled || configuredAiCapacity == 0
                ? math.max(0, config.MaterialsCapacity)
                : configuredAiCapacity;
            int configuredCurrent = isPlayerControlled
                ? config.InitialMaterials
                : config.InitialAiMaterials;
            int current = math.min(math.max(0, configuredCurrent), capacity);
            FactionTacticalMaterialsComponent materials = new()
            {
                FactionId = factionId,
                Current = current,
                Capacity = capacity,
                Version = 1u
            };

            if (em.HasComponent<FactionTacticalMaterialsComponent>(economyEntity))
                em.SetComponentData(economyEntity, materials);
            else
                em.AddComponentData(economyEntity, materials);

            FactionMaterialFabricationTelemetryComponent fabricationTelemetry = new()
            {
                FactionId = factionId
            };
            if (em.HasComponent<FactionMaterialFabricationTelemetryComponent>(economyEntity))
                em.SetComponentData(economyEntity, fabricationTelemetry);
            else
                em.AddComponentData(economyEntity, fabricationTelemetry);

            FactionFuelLogisticsTelemetryComponent fuelLogisticsTelemetry = new()
            {
                FactionId = factionId
            };
            if (em.HasComponent<FactionFuelLogisticsTelemetryComponent>(economyEntity))
                em.SetComponentData(economyEntity, fuelLogisticsTelemetry);
            else
                em.AddComponentData(economyEntity, fuelLogisticsTelemetry);

            if (!em.HasComponent<MaterialFabricationEconomyEventQueueComponent>(economyEntity))
                em.AddComponentData(economyEntity, new MaterialFabricationEconomyEventQueueComponent());
            DynamicBuffer<MaterialFabricationEconomyEventElement> events =
                em.HasBuffer<MaterialFabricationEconomyEventElement>(economyEntity)
                    ? em.GetBuffer<MaterialFabricationEconomyEventElement>(economyEntity)
                    : em.AddBuffer<MaterialFabricationEconomyEventElement>(economyEntity);
            events.EnsureCapacity(MaterialFabricationEconomyEventQueueComponent.Capacity);
        }
    }
}
