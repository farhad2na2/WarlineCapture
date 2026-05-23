using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class AIFactionControlStartupSystem
{
    public readonly struct Result
    {
        public readonly bool HasPlayerAutoMode;
        public readonly bool PlayerAutoModeEnabled;

        public Result(bool hasPlayerAutoMode, bool playerAutoModeEnabled)
        {
            HasPlayerAutoMode = hasPlayerAutoMode;
            PlayerAutoModeEnabled = playerAutoModeEnabled;
        }
    }

    public Result Initialize(EntityManager em, IReadOnlyList<AIControllerConfig> aiControllerConfigs)
    {
        if (aiControllerConfigs == null)
            return default;

        Entity configEntity;
        using (EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<FactionControlConfigTag>()))
        {
            using var entities = query.ToEntityArray(Allocator.Temp);
            configEntity = entities.Length > 0 ? entities[0] : Entity.Null;
        }

        if (configEntity == Entity.Null)
        {
            configEntity = em.CreateEntity(typeof(FactionControlConfigTag));
            em.AddBuffer<FactionControlEntry>(configEntity);
        }
        else if (!em.HasBuffer<FactionControlEntry>(configEntity))
        {
            em.AddBuffer<FactionControlEntry>(configEntity);
        }

        DynamicBuffer<FactionControlEntry> entries = em.GetBuffer<FactionControlEntry>(configEntity);
        entries.Clear();

        bool hasPlayerEntry = false;
        bool hasEnemyEntry = false;
        bool playerAutoModeEnabled = false;
        int enemyConfigIndex = 0;
        for (int i = 0; i < aiControllerConfigs.Count; i++)
        {
            AIControllerConfig config = aiControllerConfigs[i];
            if (config == null)
                continue;
            if (!ShouldIncludeAIConfig(config, ref enemyConfigIndex))
                continue;

            byte factionId = (byte)Mathf.Clamp(config.FactionId, 0, byte.MaxValue);
            bool isPlayer = config.Role == AIControllerRole.PlayerAuto;
            bool isAIControlled = AISettingsRuntimeState.ResolveEnabled(config) && (!isPlayer || AISettingsRuntimeState.PlayerAutoAIEnabled);
            entries.Add(new FactionControlEntry
            {
                FactionId = factionId,
                AIControlled = isAIControlled ? (byte)1 : (byte)0,
                IsPlayerFaction = isPlayer ? (byte)1 : (byte)0,
                LastLogTime = -999f
            });

            if (isPlayer)
            {
                playerAutoModeEnabled = isAIControlled;
                AISettingsRuntimeState.PlayerAutoAIEnabled = isAIControlled;
                hasPlayerEntry = true;
            }

            if (config.Role == AIControllerRole.Enemy)
                hasEnemyEntry = true;
        }

        if (!hasPlayerEntry)
        {
            entries.Add(new FactionControlEntry
            {
                FactionId = 0,
                AIControlled = 0,
                IsPlayerFaction = 1,
                LastLogTime = -999f
            });
        }

        if (!hasEnemyEntry)
        {
            entries.Add(new FactionControlEntry
            {
                FactionId = 1,
                AIControlled = 1,
                IsPlayerFaction = 0,
                LastLogTime = -999f
            });
        }

        return new Result(true, playerAutoModeEnabled);
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
