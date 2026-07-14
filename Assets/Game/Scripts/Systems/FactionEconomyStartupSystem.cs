using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    public readonly struct FactionEconomyStartupEntry
    {
        public readonly bool Enabled;
        public readonly AIControllerRole Role;
        public readonly byte FactionId;
        public readonly int StartingMoney;
        public readonly float IncomeMultiplier;
        public readonly int OilSellPrice;
        public readonly int FuelSellPrice;
        public readonly float BuildIntervalSeconds;

        public FactionEconomyStartupEntry(
            bool enabled,
            AIControllerRole role,
            byte factionId,
            int startingMoney,
            float incomeMultiplier,
            int oilSellPrice,
            int fuelSellPrice,
            float buildIntervalSeconds)
        {
            Enabled = enabled;
            Role = role;
            FactionId = factionId;
            StartingMoney = startingMoney;
            IncomeMultiplier = incomeMultiplier;
            OilSellPrice = oilSellPrice;
            FuelSellPrice = fuelSellPrice;
            BuildIntervalSeconds = buildIntervalSeconds;
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct FactionEconomyStartupSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // RequireForUpdate intentionally omitted: disabled startup helper; AI startup calls Initialize directly.
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public void Initialize(
            EntityManager em,
            IReadOnlyList<FactionEconomyStartupEntry> aiControllerConfigs,
            AISettingsSnapshot aiSettings)
        {
            if (aiControllerConfigs == null)
                return;

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            ComponentTypeHandle<FactionEconomy> economyType = em.GetComponentTypeHandle<FactionEconomy>(true);
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            Dictionary<byte, Entity> economyEntitiesByFaction = new();
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref economyType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    FactionEconomy economy = economies[i];

                    economyEntitiesByFaction[economy.FactionId] = entity;
                }
            }

            int enemyConfigIndex = 0;
            for (int i = 0; i < aiControllerConfigs.Count; i++)
            {
                FactionEconomyStartupEntry config = aiControllerConfigs[i];
                if (!ShouldIncludeAIConfig(config, ref enemyConfigIndex, aiSettings))
                    continue;

                byte factionId = config.FactionId;
                if (!economyEntitiesByFaction.TryGetValue(factionId, out Entity economyEntity) || economyEntity == Entity.Null)
                {
                    economyEntity = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
                    economyEntitiesByFaction[factionId] = economyEntity;
                }
                else if (!em.HasComponent<FactionEconomyPolicy>(economyEntity))
                {
                    em.AddComponent<FactionEconomyPolicy>(economyEntity);
                }

                em.SetComponentData(economyEntity, new FactionEconomy
                {
                    FactionId = factionId,
                    Money = aiSettings.ApplyStartingMoney(config.StartingMoney, config.Role),
                    Oil = 0f,
                    Fuel = 0f,
                    OilIncomeRate = 0f,
                    FuelIncomeRate = 0f,
                    LastSellTime = 0f,
                    LastLogTime = -999f
                });

                em.SetComponentData(economyEntity, new FactionEconomyPolicy
                {
                    Enabled = ResolveEnabled(config, aiSettings) ? (byte)1 : (byte)0,
                    IncomeMultiplier = aiSettings.ApplyIncomeMultiplier(config.IncomeMultiplier, config.Role),
                    OilSellPrice = config.OilSellPrice < 0 ? 0 : config.OilSellPrice,
                    FuelSellPrice = config.FuelSellPrice < 0 ? 0 : config.FuelSellPrice,
                    SellIntervalSeconds = config.BuildIntervalSeconds < 1f ? 1f : config.BuildIntervalSeconds
                });

                if (!em.HasComponent<FactionTacticalMaterialsComponent>(economyEntity))
                {
                    em.AddComponentData(economyEntity, new FactionTacticalMaterialsComponent
                    {
                        FactionId = factionId
                    });
                }
                if (!em.HasComponent<FactionMaterialFabricationTelemetryComponent>(economyEntity))
                {
                    em.AddComponentData(economyEntity, new FactionMaterialFabricationTelemetryComponent
                    {
                        FactionId = factionId
                    });
                }
                if (!em.HasComponent<FactionFuelLogisticsTelemetryComponent>(economyEntity))
                {
                    em.AddComponentData(economyEntity, new FactionFuelLogisticsTelemetryComponent
                    {
                        FactionId = factionId
                    });
                }

                EnsureMaterialFabricationEventQueue(em, economyEntity);
            }
        }

        private static void EnsureMaterialFabricationEventQueue(EntityManager em, Entity economyEntity)
        {
            if (!em.HasComponent<MaterialFabricationEconomyEventQueueComponent>(economyEntity))
                em.AddComponentData(economyEntity, new MaterialFabricationEconomyEventQueueComponent());

            DynamicBuffer<MaterialFabricationEconomyEventElement> events =
                em.HasBuffer<MaterialFabricationEconomyEventElement>(economyEntity)
                    ? em.GetBuffer<MaterialFabricationEconomyEventElement>(economyEntity)
                    : em.AddBuffer<MaterialFabricationEconomyEventElement>(economyEntity);
            events.EnsureCapacity(MaterialFabricationEconomyEventQueueComponent.Capacity);
        }

        private static bool ShouldIncludeAIConfig(
            FactionEconomyStartupEntry config,
            ref int enemyConfigIndex,
            AISettingsSnapshot aiSettings)
        {
            if (config.Role != AIControllerRole.Enemy)
                return true;

            int currentIndex = enemyConfigIndex;
            enemyConfigIndex++;
            return aiSettings.IsEnemyAIIndexEnabled(currentIndex);
        }

        private static bool ResolveEnabled(FactionEconomyStartupEntry config, AISettingsSnapshot aiSettings)
        {
            if (!config.Enabled)
                return false;

            return config.Role != AIControllerRole.PlayerAuto || aiSettings.PlayerAutoAIEnabled;
        }
    }
}
