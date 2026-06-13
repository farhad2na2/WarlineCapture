using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class AIStartupSystem
{
    private readonly RuntimeDiagnosticsSystem _runtimeDiagnosticsSystem = new();
    private readonly FactionEconomyStartupSystem _factionEconomyStartupSystem = new();
    private readonly AIFactionControlStartupSystem _factionControlStartupSystem = new();
    private readonly AIPlanEntryStartupSystem _planEntryStartupSystem = new();

    public delegate bool TryResolveFactionSpawnCell(byte factionId, out int2 spawnCell);

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

    public Result Initialize(
        World world,
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
        AIPlanEntryStartupConfig planEntryConfig,
        TryResolveFactionSpawnCell resolveFactionSpawnCell)
    {
        return Initialize(
            world,
            aiControllerConfigs,
            planEntryConfig,
            resolveFactionSpawnCell,
            AISettingsRuntimeState.CurrentSnapshot);
    }

    public Result Initialize(
        World world,
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
        AIPlanEntryStartupConfig planEntryConfig,
        TryResolveFactionSpawnCell resolveFactionSpawnCell,
        AISettingsSnapshot aiSettings)
    {
        Result result = default;
        if (world == null || !world.IsCreated)
            return result;

        EntityManager em = world.EntityManager;
        if (aiControllerConfigs != null)
        {
            _factionEconomyStartupSystem.Initialize(em, aiControllerConfigs, aiSettings);
            AIFactionControlStartupSystem.Result factionControlResult = _factionControlStartupSystem.Initialize(em, aiControllerConfigs, aiSettings);
            result = new Result(factionControlResult.HasPlayerAutoMode, factionControlResult.PlayerAutoModeEnabled);
            EnsureAIBuildPlansInitialized(em, aiControllerConfigs, planEntryConfig, resolveFactionSpawnCell, aiSettings);
            EnsureAIProductionPlansInitialized(em, aiControllerConfigs, planEntryConfig, aiSettings);
            EnsureAISquadPlansInitialized(em, aiControllerConfigs, aiSettings);
            EnsureAITargetPrioritySettingsInitialized(em, aiControllerConfigs, aiSettings);
        }

        return result;
    }

    public void LogConfigValidation(IReadOnlyList<AIControllerConfig> aiControllerConfigs)
    {
        LogConfigValidation(aiControllerConfigs, AISettingsRuntimeState.CurrentSnapshot);
    }

    public void LogConfigValidation(
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
        AISettingsSnapshot aiSettings)
    {
        if (!ShouldQueueAIConfigDiagnostics())
            return;

        bool queuedDiagnostics = false;
        if (aiControllerConfigs == null || aiControllerConfigs.Count == 0)
        {
            queuedDiagnostics |= TryEnqueueAIDiagnostic(
                "[AIConfigSummary] configs=0 enabled=0 playerAuto=0 enemy=0 result=MissingConfigs",
                AIDiagnosticLogComponent.WarningSeverity);
            FlushQueuedAIDiagnostics(queuedDiagnostics);
            return;
        }

        int enabledCount = 0;
        int playerAutoCount = 0;
        int enemyCount = 0;
        for (int i = 0; i < aiControllerConfigs.Count; i++)
        {
            AIControllerConfig config = aiControllerConfigs[i];
            if (config == null)
            {
                queuedDiagnostics |= TryEnqueueAIDiagnostic(
                    $"[AIConfig] index={i} result=MissingConfig",
                    AIDiagnosticLogComponent.WarningSeverity);
                continue;
            }

            if (config.Enabled)
                enabledCount++;
            if (config.Role == AIControllerRole.PlayerAuto)
                playerAutoCount++;
            if (config.Role == AIControllerRole.Enemy)
                enemyCount++;

            queuedDiagnostics |= TryEnqueueAIDiagnostic(
                $"[AIConfig] name={config.name} faction={config.FactionId} role={config.Role} difficulty={config.Difficulty} " +
                $"enabled={(config.Enabled ? 1 : 0)} autoControlsPlayer={(config.AutoControlsPlayerFaction ? 1 : 0)} " +
                $"money={config.StartingMoney} income={config.IncomeMultiplier:F2} oilSell={config.OilSellPrice} fuelSell={config.FuelSellPrice} " +
                $"buildInterval={config.BuildIntervalSeconds:F1} productionInterval={config.UnitProductionIntervalSeconds:F1} " +
                $"attackInterval={config.AttackIntervalSeconds:F1} maxAttackGroups={config.MaxActiveAttackGroups} " +
                $"defenseRadius={config.DefenseRadiusCells} aggression={config.Aggression:F2} " +
                $"preferredBuildings={config.PreferredBuildingIds?.Count ?? 0} preferredUnits={config.PreferredUnitIds?.Count ?? 0} " +
                $"preferredVehicles={config.PreferredVehicleIds?.Count ?? 0}");
        }

        queuedDiagnostics |= TryEnqueueAIDiagnostic($"[AIConfigSummary] configs={aiControllerConfigs.Count} enabled={enabledCount} playerAuto={playerAutoCount} enemy={enemyCount} result=Ready");
        queuedDiagnostics |= TryEnqueueAIDiagnostic(
            $"[AISettings] difficulty={aiSettings.Difficulty} startingMoney={aiSettings.StartingMoney} " +
            $"income={aiSettings.IncomeMultiplier:F2} buildSpeed={aiSettings.BuildSpeed} " +
            $"productionSpeed={aiSettings.UnitProductionSpeed} groupSize={aiSettings.AttackGroupSize} " +
            $"attackFrequency={aiSettings.AttackFrequency} aggression={aiSettings.Aggression} " +
            $"expansion={aiSettings.Expansion} targetPriority={aiSettings.TargetPriority} " +
            $"playerAuto={(aiSettings.PlayerAutoAIEnabled ? 1 : 0)} enemyCount={aiSettings.EnemyAICount}");
        FlushQueuedAIDiagnostics(queuedDiagnostics);
    }

    private bool ShouldQueueAIConfigDiagnostics()
    {
        return _runtimeDiagnosticsSystem.ReadDiagnosticsState().VerboseAILogs != 0;
    }

    private bool TryEnqueueAIDiagnostic(FixedString512Bytes message, byte severity = AIDiagnosticLogComponent.LogSeverity)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<AIDiagnosticLogComponent>());
        Entity queueEntity;
        if (query.IsEmptyIgnoreFilter)
        {
            queueEntity = em.CreateEntity(typeof(AIDiagnosticLogQueueComponent));
            em.SetName(queueEntity, "AIDiagnosticLogQueue");
            em.AddBuffer<AIDiagnosticLogComponent>(queueEntity);
        }
        else
        {
            queueEntity = query.GetSingletonEntity();
        }

        DynamicBuffer<AIDiagnosticLogComponent> logs = em.GetBuffer<AIDiagnosticLogComponent>(queueEntity);
        logs.Add(new AIDiagnosticLogComponent
        {
            Message = message,
            Severity = severity
        });
        return true;
    }

    private void FlushQueuedAIDiagnostics(bool queuedDiagnostics)
    {
        if (!queuedDiagnostics)
            return;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        SystemHandle flushSystem = world.GetOrCreateSystem<AIDiagnosticLogFlushSystem>();
        flushSystem.Update(world.Unmanaged);
    }

    private void EnsureAIBuildPlansInitialized(
        EntityManager em,
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
        AIPlanEntryStartupConfig planEntryConfig,
        TryResolveFactionSpawnCell resolveFactionSpawnCell,
        AISettingsSnapshot aiSettings)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIBuildPlan>());
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<AIBuildPlan> planType = em.GetComponentTypeHandle<AIBuildPlan>(true);
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        Dictionary<byte, Entity> planEntitiesByFaction = new();
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<AIBuildPlan> plans = chunk.GetNativeArray(ref planType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AIBuildPlan plan = plans[i];

                planEntitiesByFaction[plan.FactionId] = entity;
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
            if (!planEntitiesByFaction.TryGetValue(factionId, out Entity planEntity) || planEntity == Entity.Null)
            {
                planEntity = em.CreateEntity(typeof(AIBuildPlan));
                em.AddBuffer<AIBuildPlanEntry>(planEntity);
                planEntitiesByFaction[factionId] = planEntity;
            }
            else if (!em.HasBuffer<AIBuildPlanEntry>(planEntity))
            {
                em.AddBuffer<AIBuildPlanEntry>(planEntity);
            }

            int2 baseCenterCell = resolveFactionSpawnCell != null && resolveFactionSpawnCell(factionId, out int2 configuredCell)
                ? configuredCell
                : int2.zero;
            em.SetComponentData(planEntity, new AIBuildPlan
            {
                FactionId = factionId,
                Enabled = aiSettings.ResolveBuildEnabled(config) ? (byte)1 : (byte)0,
                NextBuildIndex = 0,
                BaseCenterCell = baseCenterCell,
                BuildIntervalSeconds = aiSettings.ApplyBuildInterval(config.BuildIntervalSeconds, config.Role),
                LastBuildTime = -999f,
                LastLogTime = -999f
            });

            DynamicBuffer<AIBuildPlanEntry> entries = em.GetBuffer<AIBuildPlanEntry>(planEntity);
            entries.Clear();
            _planEntryStartupSystem.WriteBuildPlanEntries(entries, config.PreferredBuildingIds, planEntryConfig);
        }
    }

    private void EnsureAIProductionPlansInitialized(
        EntityManager em,
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
        AIPlanEntryStartupConfig planEntryConfig,
        AISettingsSnapshot aiSettings)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIProductionPlan>());
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<AIProductionPlan> planType = em.GetComponentTypeHandle<AIProductionPlan>(true);
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        Dictionary<byte, Entity> planEntitiesByFaction = new();
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<AIProductionPlan> plans = chunk.GetNativeArray(ref planType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AIProductionPlan plan = plans[i];

                planEntitiesByFaction[plan.FactionId] = entity;
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
            if (!planEntitiesByFaction.TryGetValue(factionId, out Entity planEntity) || planEntity == Entity.Null)
            {
                planEntity = em.CreateEntity(typeof(AIProductionPlan));
                em.AddBuffer<AIProductionPlanEntry>(planEntity);
                planEntitiesByFaction[factionId] = planEntity;
            }
            else if (!em.HasBuffer<AIProductionPlanEntry>(planEntity))
            {
                em.AddBuffer<AIProductionPlanEntry>(planEntity);
            }

            em.SetComponentData(planEntity, new AIProductionPlan
            {
                FactionId = factionId,
                Enabled = aiSettings.ResolveEnabled(config) ? (byte)1 : (byte)0,
                NextUnitIndex = 0,
                TargetProducedUnits = 3,
                MaxQueuedUnits = 3,
                UnitProductionIntervalSeconds = aiSettings.ApplyProductionInterval(config.UnitProductionIntervalSeconds, config.Role),
                LastProductionTime = -999f,
                LastLogTime = -999f
            });

            DynamicBuffer<AIProductionPlanEntry> entries = em.GetBuffer<AIProductionPlanEntry>(planEntity);
            entries.Clear();
            _planEntryStartupSystem.WriteProductionPlanEntries(
                entries,
                config.PreferredUnitIds,
                config.PreferredVehicleIds,
                planEntryConfig);
        }
    }

    private void EnsureAISquadPlansInitialized(
        EntityManager em,
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
        AISettingsSnapshot aiSettings)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AISquadPlan>());
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<AISquadPlan> planType = em.GetComponentTypeHandle<AISquadPlan>(true);
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        Dictionary<byte, Entity> planEntitiesByFaction = new();
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<AISquadPlan> plans = chunk.GetNativeArray(ref planType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AISquadPlan plan = plans[i];

                planEntitiesByFaction[plan.FactionId] = entity;
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
            if (!planEntitiesByFaction.TryGetValue(factionId, out Entity planEntity) || planEntity == Entity.Null)
            {
                planEntity = em.CreateEntity(typeof(AISquadPlan));
                planEntitiesByFaction[factionId] = planEntity;
            }

            int maxUnits = config.Difficulty switch
            {
                AIControllerDifficulty.Easy => 6,
                AIControllerDifficulty.Hard => 12,
                _ => 8
            };
            int minUnits = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 5f, config.Aggression)), 2, maxUnits);
            maxUnits = aiSettings.ApplyMaxSquadUnits(maxUnits, config.Role);
            minUnits = aiSettings.ApplyMinSquadUnits(minUnits, maxUnits, config.Role);

            em.SetComponentData(planEntity, new AISquadPlan
            {
                FactionId = factionId,
                Enabled = aiSettings.ResolveEnabled(config) ? (byte)1 : (byte)0,
                MinUnits = minUnits,
                MaxUnits = maxUnits,
                MaxActiveSquads = aiSettings.ApplyMaxActiveSquads(config.MaxActiveAttackGroups, config.Role),
                NextSquadId = 1,
                LastLogTime = -999f
            });
        }
    }

    private void EnsureAITargetPrioritySettingsInitialized(
        EntityManager em,
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
        AISettingsSnapshot aiSettings)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AITargetPrioritySetting>());
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<AITargetPrioritySetting> settingType = em.GetComponentTypeHandle<AITargetPrioritySetting>(true);
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        Dictionary<byte, Entity> settingsByFaction = new();
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<AITargetPrioritySetting> settings = chunk.GetNativeArray(ref settingType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AITargetPrioritySetting setting = settings[i];

                settingsByFaction[setting.FactionId] = entity;
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
            if (!settingsByFaction.TryGetValue(factionId, out Entity settingEntity) || settingEntity == Entity.Null)
            {
                settingEntity = em.CreateEntity(typeof(AITargetPrioritySetting));
                settingsByFaction[factionId] = settingEntity;
            }

            em.SetComponentData(settingEntity, new AITargetPrioritySetting
            {
                FactionId = factionId,
                Priority = config.Role == AIControllerRole.Enemy ? (byte)aiSettings.TargetPriority : (byte)AITargetPriority.Balanced
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
