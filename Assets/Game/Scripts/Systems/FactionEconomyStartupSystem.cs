using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed partial class FactionEconomyStartupSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void Initialize(EntityManager em, IReadOnlyList<AIControllerConfig> aiControllerConfigs)
    {
        Initialize(em, aiControllerConfigs, AISettingsRuntimeState.CurrentSnapshot);
    }

    public void Initialize(
        EntityManager em,
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
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
            AIControllerConfig config = aiControllerConfigs[i];
            if (config == null)
                continue;
            if (!ShouldIncludeAIConfig(config, ref enemyConfigIndex, aiSettings))
                continue;

            byte factionId = (byte)Mathf.Clamp(config.FactionId, 0, byte.MaxValue);
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
                Enabled = aiSettings.ResolveEnabled(config) ? (byte)1 : (byte)0,
                IncomeMultiplier = aiSettings.ApplyIncomeMultiplier(config.IncomeMultiplier, config.Role),
                OilSellPrice = Mathf.Max(0, config.OilSellPrice),
                FuelSellPrice = Mathf.Max(0, config.FuelSellPrice),
                SellIntervalSeconds = Mathf.Max(1f, config.BuildIntervalSeconds)
            });
        }
    }

    private static bool ShouldIncludeAIConfig(
        AIControllerConfig config,
        ref int enemyConfigIndex,
        AISettingsSnapshot aiSettings)
    {
        if (config == null || config.Role != AIControllerRole.Enemy)
            return true;

        int currentIndex = enemyConfigIndex;
        enemyConfigIndex++;
        return aiSettings.IsEnemyAIIndexEnabled(currentIndex);
    }
}
