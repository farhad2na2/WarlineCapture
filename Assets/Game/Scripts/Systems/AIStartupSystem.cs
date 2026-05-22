using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class AIStartupSystem
{
    private readonly RuntimeDiagnosticsSystem _runtimeDiagnosticsSystem = new();

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
        bool activeFixedTacticalMission,
        TryResolveFactionSpawnCell resolveFactionSpawnCell)
    {
        Result result = default;
        if (world == null || !world.IsCreated)
            return result;

        EntityManager em = world.EntityManager;
        if (aiControllerConfigs != null)
        {
            EnsureFactionEconomiesInitialized(em, aiControllerConfigs);
            result = EnsureFactionControlConfigInitialized(em, aiControllerConfigs);
            EnsureAIBuildPlansInitialized(em, aiControllerConfigs, resolveFactionSpawnCell);
            EnsureAIProductionPlansInitialized(em, aiControllerConfigs);
            EnsureAISquadPlansInitialized(em, aiControllerConfigs);
            EnsureAITargetPrioritySettingsInitialized(em, aiControllerConfigs);
        }

        DisableGenericAIPlansForFixedTacticalMission(world, activeFixedTacticalMission);
        return result;
    }

    public void LogConfigValidation(IReadOnlyList<AIControllerConfig> aiControllerConfigs)
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
            $"[AISettings] difficulty={AISettingsRuntimeState.Difficulty} startingMoney={AISettingsRuntimeState.StartingMoney} " +
            $"income={AISettingsRuntimeState.IncomeMultiplier:F2} buildSpeed={AISettingsRuntimeState.BuildSpeed} " +
            $"productionSpeed={AISettingsRuntimeState.UnitProductionSpeed} groupSize={AISettingsRuntimeState.AttackGroupSize} " +
            $"attackFrequency={AISettingsRuntimeState.AttackFrequency} aggression={AISettingsRuntimeState.Aggression} " +
            $"expansion={AISettingsRuntimeState.Expansion} targetPriority={AISettingsRuntimeState.TargetPriority} " +
            $"playerAuto={(AISettingsRuntimeState.PlayerAutoAIEnabled ? 1 : 0)} enemyCount={AISettingsRuntimeState.EnemyAICount}");
        FlushQueuedAIDiagnostics(queuedDiagnostics);
    }

    public void DisableGenericAIPlansForFixedTacticalMission(World world, bool activeFixedTacticalMission)
    {
        if (!activeFixedTacticalMission || world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        DisableAIBuildPlans(em);
        DisableAIProductionPlans(em);
        DisableAISquadPlans(em);
    }

    private bool ShouldQueueAIConfigDiagnostics()
    {
        if (Application.isBatchMode)
            return true;

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

    private void EnsureFactionEconomiesInitialized(EntityManager em, IReadOnlyList<AIControllerConfig> aiControllerConfigs)
    {
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

    private Result EnsureFactionControlConfigInitialized(EntityManager em, IReadOnlyList<AIControllerConfig> aiControllerConfigs)
    {
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

    private void EnsureAIBuildPlansInitialized(
        EntityManager em,
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
        TryResolveFactionSpawnCell resolveFactionSpawnCell)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIBuildPlan>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        Dictionary<byte, Entity> planEntitiesByFaction = new();
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AIBuildPlan>(entity))
                continue;

            AIBuildPlan plan = em.GetComponentData<AIBuildPlan>(entity);
            planEntitiesByFaction[plan.FactionId] = entity;
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
                Enabled = AISettingsRuntimeState.ResolveBuildEnabled(config) ? (byte)1 : (byte)0,
                NextBuildIndex = 0,
                BaseCenterCell = baseCenterCell,
                BuildIntervalSeconds = AISettingsRuntimeState.ApplyBuildInterval(config.BuildIntervalSeconds, config.Role),
                LastBuildTime = -999f,
                LastLogTime = -999f
            });

            DynamicBuffer<AIBuildPlanEntry> entries = em.GetBuffer<AIBuildPlanEntry>(planEntity);
            entries.Clear();
            AddBuildPlanEntries(entries, config.PreferredBuildingIds);
        }
    }

    private void AddBuildPlanEntries(DynamicBuffer<AIBuildPlanEntry> entries, List<string> preferredBuildingIds)
    {
        if (preferredBuildingIds != null)
        {
            for (int i = 0; i < preferredBuildingIds.Count; i++)
            {
                string buildingId = preferredBuildingIds[i];
                if (string.IsNullOrWhiteSpace(buildingId))
                    continue;

                entries.Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes(buildingId) });
            }
        }

        if (entries.Length > 0)
            return;

        entries.Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes("Tent_Regular") });
        entries.Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes("Building_Barrack") });
        entries.Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes("Building_OilPump") });
        entries.Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes("Building_Fuel_Bladder") });
        entries.Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes("Building_Ammunition_Depot") });
    }

    private void EnsureAIProductionPlansInitialized(EntityManager em, IReadOnlyList<AIControllerConfig> aiControllerConfigs)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIProductionPlan>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        Dictionary<byte, Entity> planEntitiesByFaction = new();
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AIProductionPlan>(entity))
                continue;

            AIProductionPlan plan = em.GetComponentData<AIProductionPlan>(entity);
            planEntitiesByFaction[plan.FactionId] = entity;
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
                Enabled = AISettingsRuntimeState.ResolveEnabled(config) ? (byte)1 : (byte)0,
                NextUnitIndex = 0,
                TargetProducedUnits = 3,
                MaxQueuedUnits = 3,
                UnitProductionIntervalSeconds = AISettingsRuntimeState.ApplyProductionInterval(config.UnitProductionIntervalSeconds, config.Role),
                LastProductionTime = -999f,
                LastLogTime = -999f
            });

            DynamicBuffer<AIProductionPlanEntry> entries = em.GetBuffer<AIProductionPlanEntry>(planEntity);
            entries.Clear();
            AddProductionPlanEntries(entries, config.PreferredUnitIds, config.PreferredVehicleIds);
        }
    }

    private void AddProductionPlanEntries(DynamicBuffer<AIProductionPlanEntry> entries, List<string> preferredUnitIds, List<string> preferredVehicleIds)
    {
        AddProductionPlanEntries(entries, preferredUnitIds);
        AddProductionPlanEntries(entries, preferredVehicleIds);

        if (entries.Length > 0)
            return;

        entries.Add(new AIProductionPlanEntry { UnitId = new FixedString64Bytes("Unit_Chr_Soldier_Male_02_Alt_04") });
    }

    private void AddProductionPlanEntries(DynamicBuffer<AIProductionPlanEntry> entries, List<string> preferredUnitIds)
    {
        if (preferredUnitIds == null)
            return;

        for (int i = 0; i < preferredUnitIds.Count; i++)
        {
            string unitId = preferredUnitIds[i];
            if (string.IsNullOrWhiteSpace(unitId))
                continue;

            entries.Add(new AIProductionPlanEntry { UnitId = new FixedString64Bytes(unitId) });
        }
    }

    private void EnsureAISquadPlansInitialized(EntityManager em, IReadOnlyList<AIControllerConfig> aiControllerConfigs)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AISquadPlan>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        Dictionary<byte, Entity> planEntitiesByFaction = new();
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AISquadPlan>(entity))
                continue;

            AISquadPlan plan = em.GetComponentData<AISquadPlan>(entity);
            planEntitiesByFaction[plan.FactionId] = entity;
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
            maxUnits = AISettingsRuntimeState.ApplyMaxSquadUnits(maxUnits, config.Role);
            minUnits = AISettingsRuntimeState.ApplyMinSquadUnits(minUnits, maxUnits, config.Role);

            em.SetComponentData(planEntity, new AISquadPlan
            {
                FactionId = factionId,
                Enabled = AISettingsRuntimeState.ResolveEnabled(config) ? (byte)1 : (byte)0,
                MinUnits = minUnits,
                MaxUnits = maxUnits,
                MaxActiveSquads = AISettingsRuntimeState.ApplyMaxActiveSquads(config.MaxActiveAttackGroups, config.Role),
                NextSquadId = 1,
                LastLogTime = -999f
            });
        }
    }

    private void EnsureAITargetPrioritySettingsInitialized(EntityManager em, IReadOnlyList<AIControllerConfig> aiControllerConfigs)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AITargetPrioritySetting>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        Dictionary<byte, Entity> settingsByFaction = new();
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AITargetPrioritySetting>(entity))
                continue;

            AITargetPrioritySetting setting = em.GetComponentData<AITargetPrioritySetting>(entity);
            settingsByFaction[setting.FactionId] = entity;
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
            if (!settingsByFaction.TryGetValue(factionId, out Entity settingEntity) || settingEntity == Entity.Null)
            {
                settingEntity = em.CreateEntity(typeof(AITargetPrioritySetting));
                settingsByFaction[factionId] = settingEntity;
            }

            em.SetComponentData(settingEntity, new AITargetPrioritySetting
            {
                FactionId = factionId,
                Priority = config.Role == AIControllerRole.Enemy ? (byte)AISettingsRuntimeState.TargetPriority : (byte)AITargetPriority.Balanced
            });
        }
    }

    private void DisableAIBuildPlans(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIBuildPlan>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AIBuildPlan>(entity))
                continue;

            AIBuildPlan plan = em.GetComponentData<AIBuildPlan>(entity);
            plan.Enabled = 0;
            em.SetComponentData(entity, plan);
        }
    }

    private void DisableAIProductionPlans(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIProductionPlan>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AIProductionPlan>(entity))
                continue;

            AIProductionPlan plan = em.GetComponentData<AIProductionPlan>(entity);
            plan.Enabled = 0;
            em.SetComponentData(entity, plan);
        }
    }

    private void DisableAISquadPlans(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AISquadPlan>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AISquadPlan>(entity))
                continue;

            AISquadPlan plan = em.GetComponentData<AISquadPlan>(entity);
            plan.Enabled = 0;
            em.SetComponentData(entity, plan);
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
