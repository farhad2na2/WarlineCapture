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
        Result result = Initialize(em, aiControllerConfigs, AISettingsRuntimeState.CurrentSnapshot);
        AISettingsRuntimeState.PlayerAutoAIEnabled = result.PlayerAutoModeEnabled;
        return result;
    }

    public Result Initialize(
        EntityManager em,
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
        AISettingsSnapshot aiSettings)
    {
        if (aiControllerConfigs == null)
            return default;

        Entity configEntity;
        using (EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<FactionControlConfigTag>()))
        {
            configEntity = Entity.Null;
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                if (entities.Length == 0)
                    continue;

                configEntity = entities[0];
                break;
            }
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
            if (!ShouldIncludeAIConfig(config, ref enemyConfigIndex, aiSettings))
                continue;

            byte factionId = (byte)Mathf.Clamp(config.FactionId, 0, byte.MaxValue);
            bool isPlayer = config.Role == AIControllerRole.PlayerAuto;
            bool isAIControlled = aiSettings.ResolveEnabled(config) && (!isPlayer || aiSettings.PlayerAutoAIEnabled);
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
                hasPlayerEntry = true;
            }

            if (config.Role == AIControllerRole.Enemy)
                hasEnemyEntry = true;
        }

        if (!hasPlayerEntry)
        {
            entries.Add(new FactionControlEntry
            {
                FactionId = FactionIdentitySystem.PlayerFactionId,
                AIControlled = 0,
                IsPlayerFaction = 1,
                LastLogTime = -999f
            });
        }

        if (!hasEnemyEntry)
        {
            entries.Add(new FactionControlEntry
            {
                FactionId = FactionIdentitySystem.EnemyFactionId,
                AIControlled = 1,
                IsPlayerFaction = 0,
                LastLogTime = -999f
            });
        }

        return new Result(true, playerAutoModeEnabled);
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
