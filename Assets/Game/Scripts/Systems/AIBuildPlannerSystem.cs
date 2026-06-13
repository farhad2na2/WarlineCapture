using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(AIFactionControlSystem))]
public partial struct AIBuildPlannerSystem : ISystem
{
    private const float LogIntervalSeconds = 10f;
    private int _nextBuildSpawnRequestId;
    private EntityQuery _buildingRuntimeBoundaryQuery;
    private EntityQuery _diagnosticLogQueueQuery;
    private EntityQuery _planQuery;
    private EntityQuery _economyQuery;
    private EntityTypeHandle _entityType;
    private ComponentTypeHandle<FactionEconomy> _economyType;

    public void OnCreate(ref SystemState state)
    {
        _buildingRuntimeBoundaryQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>(),
            ComponentType.ReadOnly<BuildingConfiguredSpawnableReadModel>(),
            ComponentType.ReadOnly<BuildingRuntimeFactionSummary>(),
            ComponentType.ReadOnly<BuildingRuntimeOwnedBuildingSummary>(),
            ComponentType.ReadWrite<BuildingRuntimeSpawnRequest>());
        _diagnosticLogQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<AIDiagnosticLogComponent>());
        _planQuery = state.GetEntityQuery(ComponentType.ReadWrite<AIBuildPlan>(), ComponentType.ReadOnly<AIBuildPlanEntry>());
        _economyQuery = state.GetEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
        _entityType = state.GetEntityTypeHandle();
        _economyType = state.GetComponentTypeHandle<FactionEconomy>(true);
        state.RequireForUpdate(_buildingRuntimeBoundaryQuery);
        state.RequireForUpdate<AIBuildPlan>();
        state.RequireForUpdate<FactionEconomy>();
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
            return;

        if (!TryGetBuildingRuntimeBoundaryEntity(ref state, out Entity boundaryEntity))
            return;

        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
        GridConfig grid = SystemAPI.GetSingleton<GridConfig>();
        bool hasControls = SystemAPI.HasSingleton<FactionControlConfigTag>();
        DynamicBuffer<FactionControlEntry> controls = hasControls
            ? SystemAPI.GetSingletonBuffer<FactionControlEntry>(true)
            : default;
        bool shouldLog = ShouldQueueDiagnostics(ref state);

        EntityManager em = state.EntityManager;
        _entityType.Update(ref state);
        _economyType.Update(ref state);
        using NativeArray<ArchetypeChunk> planChunks = _planQuery.ToArchetypeChunkArray(Allocator.Temp);
        using NativeList<Entity> planEntities = new(_planQuery.CalculateEntityCount(), Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < planChunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = planChunks[chunkIndex].GetNativeArray(_entityType);
            planEntities.AddRange(entities);
        }

        NativeList<FactionEconomyRecord> economyRecords = BuildFactionEconomyRecords();
        try
        {
            for (int i = 0; i < planEntities.Length; i++)
            {
                Entity planEntity = planEntities[i];
                AIBuildPlan plan = em.GetComponentData<AIBuildPlan>(planEntity);
                if (plan.Enabled == 0 || !IsFactionAIControlled(plan.FactionId, hasControls, controls))
                    continue;

                if (!TryFindEconomyRecord(economyRecords, plan.FactionId, out int economyRecordIndex, out FactionEconomyRecord economyRecord))
                    continue;

                Entity economyEntity = economyRecord.Entity;
                FactionEconomy economy = economyRecord.Economy;
                ProcessCompletedSpawnRequests(ref state, boundaryEntity, planEntity, ref plan, ref economy, shouldLog);
                em.SetComponentData(economyEntity, economy);
                economyRecords[economyRecordIndex] = new FactionEconomyRecord(economyEntity, economy);

                float interval = Mathf.Max(0.1f, plan.BuildIntervalSeconds);
                if (now - plan.LastBuildTime < interval)
                {
                    em.SetComponentData(planEntity, plan);
                    continue;
                }

                DynamicBuffer<AIBuildPlanEntry> entries = em.GetBuffer<AIBuildPlanEntry>(planEntity);
                if (entries.Length == 0)
                {
                    LogNoPlanIfNeeded(ref state, ref plan, now, shouldLog);
                    em.SetComponentData(planEntity, plan);
                    continue;
                }

                if (plan.BaseCenterCell.x <= 0 && plan.BaseCenterCell.y <= 0)
                    plan.BaseCenterCell = ResolveDefaultBaseCenter(plan.FactionId, grid);

                bool handledDecision = false;
                int attempts = math.max(1, entries.Length);
                for (int attempt = 0; attempt < attempts; attempt++)
                {
                    int entryIndex = PositiveModulo(plan.NextBuildIndex + attempt, entries.Length);
                    string buildingId = entries[entryIndex].BuildingId.ToString();
                    if (string.IsNullOrWhiteSpace(buildingId))
                        continue;

                    if (TryGetOwnedBuildingCount(ref state, boundaryEntity, plan.FactionId, buildingId, out int ownedCount) &&
                        ownedCount > 0)
                        continue;

                    handledDecision = true;
                    if (HasPendingSpawnRequest(ref state, boundaryEntity, plan.FactionId, buildingId))
                    {
                        plan.LastBuildTime = now;
                        break;
                    }

                    if (!TryResolveSpawnableReadModel(ref state, boundaryEntity, buildingId, out BuildingConfiguredSpawnableReadModel spawnable) ||
                        spawnable.CanRequest == 0)
                    {
                        plan.NextBuildIndex = entryIndex + 1;
                        plan.LastBuildTime = now;
                        if (shouldLog)
                            EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={buildingId} result=MissingConfig");
                        break;
                    }

                    int cost = Mathf.Max(0, spawnable.Price);
                    if (economy.Money < cost)
                    {
                        plan.LastBuildTime = now;
                        if (shouldLog)
                            EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={spawnable.DisplayName.ToString()} cost={cost} result=InsufficientFunds money={economy.Money}");
                        break;
                    }

                    Vector2Int preferredOrigin = ResolvePreferredOrigin(plan.BaseCenterCell, entryIndex);
                    EnqueueSpawnRequest(ref state, boundaryEntity, planEntity, plan.FactionId, buildingId, entryIndex, preferredOrigin, cost, spawnable.DisplayName);
                    plan.LastBuildTime = now;
                    if (shouldLog)
                        EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={spawnable.DisplayName.ToString()} cell={new int2(preferredOrigin.x, preferredOrigin.y)} cost={cost} result=Requested");
                    break;
                }

                if (!handledDecision && now - plan.LastLogTime >= LogIntervalSeconds)
                {
                    plan.LastLogTime = now;
                    if (shouldLog)
                    {
                        TryGetFactionBuildingCount(ref state, boundaryEntity, plan.FactionId, out int ownedBuildings);
                        EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} result=Complete ownedBuildings={ownedBuildings}");
                    }
                }

                em.SetComponentData(planEntity, plan);
            }
        }
        finally
        {
            economyRecords.Dispose();
        }

    }

    private NativeList<FactionEconomyRecord> BuildFactionEconomyRecords()
    {
        int count = _economyQuery.CalculateEntityCount();
        NativeList<FactionEconomyRecord> records = new(count, Allocator.Temp);
        using NativeArray<ArchetypeChunk> chunks = _economyQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(_entityType);
            NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref _economyType);
            for (int i = 0; i < chunk.Count; i++)
                records.Add(new FactionEconomyRecord(entities[i], economies[i]));
        }

        return records;
    }

    private static bool TryFindEconomyRecord(
        NativeList<FactionEconomyRecord> records,
        byte factionId,
        out int index,
        out FactionEconomyRecord record)
    {
        for (int i = 0; i < records.Length; i++)
        {
            FactionEconomyRecord candidate = records[i];
            if (candidate.Economy.FactionId != factionId)
                continue;

            index = i;
            record = candidate;
            return true;
        }

        index = -1;
        record = default;
        return false;
    }

    private bool TryGetBuildingRuntimeBoundaryEntity(ref SystemState state, out Entity entity)
    {
        entity = Entity.Null;
        if (_buildingRuntimeBoundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entity = _buildingRuntimeBoundaryQuery.GetSingletonEntity();
        return entity != Entity.Null && state.EntityManager.Exists(entity);
    }

    private bool TryResolveSpawnableReadModel(
        ref SystemState state,
        Entity boundaryEntity,
        string buildingId,
        out BuildingConfiguredSpawnableReadModel spawnable)
    {
        spawnable = default;
        if (!state.EntityManager.HasBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity))
            return false;

        string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId);
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables =
            state.EntityManager.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity, true);
        for (int i = 0; i < spawnables.Length; i++)
        {
            BuildingConfiguredSpawnableReadModel candidate = spawnables[i];
            if (candidate.BuildingId.ToString() != normalized)
                continue;

            spawnable = candidate;
            return true;
        }

        return false;
    }

    private bool TryGetOwnedBuildingCount(
        ref SystemState state,
        Entity boundaryEntity,
        byte factionId,
        string buildingId,
        out int count)
    {
        count = 0;
        if (!state.EntityManager.HasBuffer<BuildingRuntimeOwnedBuildingSummary>(boundaryEntity))
            return false;

        string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId);
        DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> summaries =
            state.EntityManager.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(boundaryEntity, true);
        for (int i = 0; i < summaries.Length; i++)
        {
            BuildingRuntimeOwnedBuildingSummary summary = summaries[i];
            if (summary.FactionId != factionId || summary.BuildingId.ToString() != normalized)
                continue;

            count = summary.Count;
            return true;
        }

        return false;
    }

    private bool TryGetFactionBuildingCount(ref SystemState state, Entity boundaryEntity, byte factionId, out int count)
    {
        count = 0;
        if (!state.EntityManager.HasBuffer<BuildingRuntimeFactionSummary>(boundaryEntity))
            return false;

        DynamicBuffer<BuildingRuntimeFactionSummary> summaries =
            state.EntityManager.GetBuffer<BuildingRuntimeFactionSummary>(boundaryEntity, true);
        for (int i = 0; i < summaries.Length; i++)
        {
            BuildingRuntimeFactionSummary summary = summaries[i];
            if (summary.FactionId != factionId)
                continue;

            count = summary.BuildingCount;
            return true;
        }

        return false;
    }

    private bool HasPendingSpawnRequest(ref SystemState state, Entity boundaryEntity, byte factionId, string buildingId)
    {
        if (!state.EntityManager.HasBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity))
            return false;

        string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId);
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
            state.EntityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity, true);
        for (int i = 0; i < requests.Length; i++)
        {
            BuildingRuntimeSpawnRequest request = requests[i];
            if (request.FactionId == factionId &&
                request.BuildingId.ToString() == normalized &&
                request.Status == BuildingRuntimeSpawnRequest.Pending)
            {
                return true;
            }
        }

        return false;
    }

    private void EnqueueSpawnRequest(
        ref SystemState state,
        Entity boundaryEntity,
        Entity planEntity,
        byte factionId,
        string buildingId,
        int entryIndex,
        Vector2Int preferredOrigin,
        int cost,
        FixedString128Bytes displayName)
    {
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
            state.EntityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
        requests.Add(new BuildingRuntimeSpawnRequest
        {
            RequestId = ++_nextBuildSpawnRequestId,
            FactionId = factionId,
            BuildingId = ToFixedString128(BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId)),
            PreferredOrigin = new int2(preferredOrigin.x, preferredOrigin.y),
            Status = BuildingRuntimeSpawnRequest.Pending,
            PlanEntity = planEntity,
            EntryIndex = entryIndex,
            Cost = cost,
            DisplayName = displayName
        });
    }

    private void ProcessCompletedSpawnRequests(
        ref SystemState state,
        Entity boundaryEntity,
        Entity planEntity,
        ref AIBuildPlan plan,
        ref FactionEconomy economy,
        bool shouldLog)
    {
        if (!state.EntityManager.HasBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity))
            return;

        DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
            state.EntityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
        for (int i = requests.Length - 1; i >= 0; i--)
        {
            BuildingRuntimeSpawnRequest request = requests[i];
            if (request.PlanEntity != planEntity ||
                request.Status == BuildingRuntimeSpawnRequest.Pending)
            {
                continue;
            }

            if (request.Status == BuildingRuntimeSpawnRequest.Succeeded)
            {
                economy.Money = Mathf.Max(0, economy.Money - Mathf.Max(0, request.Cost));
                plan.NextBuildIndex = request.EntryIndex + 1;
            }
            else if (request.ResultCode == BuildingRuntimeSpawnRequest.MissingConfig)
            {
                plan.NextBuildIndex = request.EntryIndex + 1;
            }

            if (shouldLog)
            {
                EnqueueDiagnostic(
                    ref state,
                    $"[AIBuild] faction={request.FactionId} building={request.DisplayName.ToString()} cell={request.ActualOrigin} cost={request.Cost} result={SpawnResultLabel(request)}");
            }

            requests.RemoveAt(i);
        }
    }

    private static bool IsFactionAIControlled(byte factionId, bool hasControls, DynamicBuffer<FactionControlEntry> controls)
    {
        if (!hasControls)
            return FactionIdentitySystem.IsAiControlledByDefault(factionId);

        for (int i = 0; i < controls.Length; i++)
        {
            FactionControlEntry control = controls[i];
            if (control.FactionId == factionId)
                return control.AIControlled != 0;
        }

        return FactionIdentitySystem.IsAiControlledByDefault(factionId);
    }

    private static int2 ResolveDefaultBaseCenter(byte factionId, GridConfig grid)
    {
        int x = FactionIdentitySystem.IsPlayerControlled(factionId) ? grid.Width / 4 : (grid.Width * 3) / 4;
        int y = grid.Height / 2;
        return new int2(math.max(0, x), math.max(0, y));
    }

    private static Vector2Int ResolvePreferredOrigin(int2 baseCenterCell, int entryIndex)
    {
        int ring = entryIndex / 5;
        int spacing = 14 + ring * 8;
        int2 offset = PositiveModulo(entryIndex, 5) switch
        {
            0 => new int2(0, 0),
            1 => new int2(spacing, 0),
            2 => new int2(-spacing, 0),
            3 => new int2(0, spacing),
            _ => new int2(0, -spacing)
        };

        int2 origin = baseCenterCell + offset;
        return new Vector2Int(origin.x, origin.y);
    }

    private static int PositiveModulo(int value, int modulo)
    {
        if (modulo <= 0)
            return 0;

        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private static string SpawnResultLabel(BuildingRuntimeSpawnRequest request)
    {
        if (request.Status == BuildingRuntimeSpawnRequest.Succeeded)
            return "Placed";

        return request.ResultCode switch
        {
            BuildingRuntimeSpawnRequest.MissingConfig => "MissingConfig",
            BuildingRuntimeSpawnRequest.Blocked => "Blocked",
            _ => "Failed"
        };
    }

    private bool ShouldQueueDiagnostics(ref SystemState state)
    {
        return InitialUnitsRuntimeState.VerboseAILogs ||
            SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
            SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
    }

    private void EnqueueDiagnostic(ref SystemState state, FixedString512Bytes message)
    {
        EntityManager em = state.EntityManager;
        Entity queueEntity;
        if (_diagnosticLogQueueQuery.IsEmptyIgnoreFilter)
        {
            queueEntity = em.CreateEntity(typeof(AIDiagnosticLogQueueComponent));
            em.SetName(queueEntity, "AIDiagnosticLogQueue");
            em.AddBuffer<AIDiagnosticLogComponent>(queueEntity);
        }
        else
        {
            queueEntity = _diagnosticLogQueueQuery.GetSingletonEntity();
        }

        DynamicBuffer<AIDiagnosticLogComponent> logs = em.GetBuffer<AIDiagnosticLogComponent>(queueEntity);
        logs.Add(new AIDiagnosticLogComponent { Message = message });
    }

    private void LogNoPlanIfNeeded(ref SystemState state, ref AIBuildPlan plan, float now, bool shouldLog)
    {
        if (now - plan.LastLogTime < LogIntervalSeconds)
            return;

        plan.LastLogTime = now;
        if (shouldLog)
            EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} result=NoPlan");
    }

    private static FixedString128Bytes ToFixedString128(string value)
    {
        return new FixedString128Bytes(value ?? string.Empty);
    }

    private readonly struct FactionEconomyRecord
    {
        public FactionEconomyRecord(Entity entity, FactionEconomy economy)
        {
            Entity = entity;
            Economy = economy;
        }

        public readonly Entity Entity;
        public readonly FactionEconomy Economy;
    }
}
