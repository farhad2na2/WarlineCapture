using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class FactionEconomyStartupSystem
{
    public void Initialize(EntityManager em, IReadOnlyList<AIControllerConfig> aiControllerConfigs)
    {
        if (aiControllerConfigs == null)
            return;

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<FactionEconomy>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        Dictionary<byte, Entity> economyEntitiesByFaction = new();
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<FactionEconomy>(entity))
                continue;

            FactionEconomy economy = em.GetComponentData<FactionEconomy>(entity);
            economyEntitiesByFaction[economy.FactionId] = entity;
        }

        int enemyConfigIndex = 0;
        for (int i = 0; i < aiControllerConfigs.Count; i++)
        {
            AIControllerConfig config = aiControllerConfigs[i];
            if (config == null)
                continue;
            if (!ShouldIncludeAIConfig(config, ref enemyConfigIndex))
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
                Money = AISettingsRuntimeState.ApplyStartingMoney(config.StartingMoney, config.Role),
                Oil = 0f,
                Fuel = 0f,
                OilIncomeRate = 0f,
                FuelIncomeRate = 0f,
                LastSellTime = 0f,
                LastLogTime = -999f
            });

            em.SetComponentData(economyEntity, new FactionEconomyPolicy
            {
                Enabled = AISettingsRuntimeState.ResolveEnabled(config) ? (byte)1 : (byte)0,
                IncomeMultiplier = AISettingsRuntimeState.ApplyIncomeMultiplier(config.IncomeMultiplier, config.Role),
                OilSellPrice = Mathf.Max(0, config.OilSellPrice),
                FuelSellPrice = Mathf.Max(0, config.FuelSellPrice),
                SellIntervalSeconds = Mathf.Max(1f, config.BuildIntervalSeconds)
            });
        }
    }

    private bool ShouldIncludeAIConfig(AIControllerConfig config, ref int enemyConfigIndex)
    {
        if (config == null || config.Role != AIControllerRole.Enemy)
            return true;

        int currentIndex = enemyConfigIndex;
        enemyConfigIndex++;
        return AISettingsRuntimeState.IsEnemyAIIndexEnabled(currentIndex);
    }
}
