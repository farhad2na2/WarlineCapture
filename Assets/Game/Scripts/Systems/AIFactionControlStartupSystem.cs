using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    public readonly struct AIFactionControlStartupEntry
    {
        public readonly bool Enabled;
        public readonly AIControllerRole Role;
        public readonly byte FactionId;

        public AIFactionControlStartupEntry(
            bool enabled,
            AIControllerRole role,
            byte factionId)
        {
            Enabled = enabled;
            Role = role;
            FactionId = factionId;
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct AIFactionControlStartupSystem : ISystem
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

        public void OnCreate(ref SystemState state)
        {
            // RequireForUpdate intentionally omitted: disabled startup helper; AI startup calls Initialize directly.
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public Result Initialize(
            EntityManager em,
            IReadOnlyList<AIFactionControlStartupEntry> aiControllerConfigs,
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
                AIFactionControlStartupEntry config = aiControllerConfigs[i];
                if (!ShouldIncludeAIConfig(config, ref enemyConfigIndex, aiSettings))
                    continue;

                byte factionId = config.FactionId;
                bool isPlayer = config.Role == AIControllerRole.PlayerAuto;
                bool isAIControlled = ResolveEnabled(config, aiSettings) && (!isPlayer || aiSettings.PlayerAutoAIEnabled);
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
                    FactionId = FactionIdentity.PlayerFactionId,
                    AIControlled = 0,
                    IsPlayerFaction = 1,
                    LastLogTime = -999f
                });
            }

            if (!hasEnemyEntry)
            {
                entries.Add(new FactionControlEntry
                {
                    FactionId = FactionIdentity.EnemyFactionId,
                    AIControlled = 1,
                    IsPlayerFaction = 0,
                    LastLogTime = -999f
                });
            }

            return new Result(true, playerAutoModeEnabled);
        }

        private static bool ShouldIncludeAIConfig(
            AIFactionControlStartupEntry config,
            ref int enemyConfigIndex,
            AISettingsSnapshot aiSettings)
        {
            if (config.Role != AIControllerRole.Enemy)
                return true;

            int currentIndex = enemyConfigIndex;
            enemyConfigIndex++;
            return aiSettings.IsEnemyAIIndexEnabled(currentIndex);
        }

        private static bool ResolveEnabled(AIFactionControlStartupEntry config, AISettingsSnapshot aiSettings)
        {
            if (!config.Enabled)
                return false;

            return config.Role != AIControllerRole.PlayerAuto || aiSettings.PlayerAutoAIEnabled;
        }
    }
}
