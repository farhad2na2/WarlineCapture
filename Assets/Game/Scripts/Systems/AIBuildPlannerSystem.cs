using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

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

    private enum BuildDecisionResult : byte
    {
        None = 0,
        Pending = 1,
        MissingConfig = 2,
        InsufficientFunds = 3,
        Request = 4
    }

    private struct BuildCandidateEntry
    {
        public int EntryIndex;
        public FixedString128Bytes BuildingId;
        public byte IsValid;
    }

    private struct BuildDecision
    {
        public BuildDecisionResult Result;
        public int EntryIndex;
        public FixedString128Bytes BuildingId;
        public BuildingConfiguredSpawnableReadModel Spawnable;
        public int Cost;
        public int2 PreferredOrigin;
    }

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
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive == 0)
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

                float interval = math.max(0.1f, plan.BuildIntervalSeconds);
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

                BuildDecision decision = SelectBuildDecision(ref state, boundaryEntity, entries, plan, economy.Money);
                bool handledDecision = decision.Result != BuildDecisionResult.None;
                switch (decision.Result)
                {
                    case BuildDecisionResult.Pending:
                        plan.LastBuildTime = now;
                        break;

                    case BuildDecisionResult.MissingConfig:
                        plan.NextBuildIndex = decision.EntryIndex + 1;
                        plan.LastBuildTime = now;
                        if (shouldLog)
                            EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.BuildingId.ToString()} result=MissingConfig");
                        break;

                    case BuildDecisionResult.InsufficientFunds:
                        plan.LastBuildTime = now;
                        if (shouldLog)
                            EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.Spawnable.DisplayName.ToString()} cost={decision.Cost} result=InsufficientFunds money={economy.Money}");
                        break;

                    case BuildDecisionResult.Request:
                        EnqueueSpawnRequest(
                            ref state,
                            boundaryEntity,
                            planEntity,
                            plan.FactionId,
                            decision.BuildingId.ToString(),
                            decision.EntryIndex,
                            decision.PreferredOrigin,
                            decision.Cost,
                            decision.Spawnable.DisplayName);
                        plan.LastBuildTime = now;
                        if (shouldLog)
                            EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.Spawnable.DisplayName.ToString()} cell={decision.PreferredOrigin} cost={decision.Cost} result=Requested");
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

    private BuildDecision SelectBuildDecision(
        ref SystemState state,
        Entity boundaryEntity,
        DynamicBuffer<AIBuildPlanEntry> entries,
        AIBuildPlan plan,
        int economyMoney)
    {
        using NativeList<BuildCandidateEntry> normalizedEntries = BuildNormalizedBuildEntries(entries);
        using NativeArray<BuildingConfiguredSpawnableReadModel> spawnables =
            CopyBoundaryBuffer<BuildingConfiguredSpawnableReadModel>(state.EntityManager, boundaryEntity, Allocator.TempJob);
        using NativeArray<BuildingRuntimeOwnedBuildingSummary> ownedSummaries =
            CopyBoundaryBuffer<BuildingRuntimeOwnedBuildingSummary>(state.EntityManager, boundaryEntity, Allocator.TempJob);
        using NativeArray<BuildingRuntimeSpawnRequest> spawnRequests =
            CopyBoundaryBuffer<BuildingRuntimeSpawnRequest>(state.EntityManager, boundaryEntity, Allocator.TempJob);
        using NativeReference<BuildDecision> decision = new(Allocator.TempJob);

        new SelectBuildDecisionJob
        {
            Entries = normalizedEntries.AsArray(),
            Spawnables = spawnables,
            OwnedSummaries = ownedSummaries,
            SpawnRequests = spawnRequests,
            Plan = plan,
            EconomyMoney = economyMoney,
            Decision = decision
        }.Schedule(state.Dependency).Complete();

        return decision.Value;
    }

    private static NativeList<BuildCandidateEntry> BuildNormalizedBuildEntries(DynamicBuffer<AIBuildPlanEntry> entries)
    {
        NativeList<BuildCandidateEntry> normalizedEntries = new(entries.Length, Allocator.TempJob);
        for (int i = 0; i < entries.Length; i++)
        {
            string buildingId = entries[i].BuildingId.ToString();
            if (string.IsNullOrWhiteSpace(buildingId))
            {
                normalizedEntries.Add(new BuildCandidateEntry
                {
                    EntryIndex = i,
                    IsValid = 0
                });
                continue;
            }

            normalizedEntries.Add(new BuildCandidateEntry
            {
                EntryIndex = i,
                BuildingId = ToFixedString128(BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId)),
                IsValid = 1
            });
        }

        return normalizedEntries;
    }

    private static NativeArray<T> CopyBoundaryBuffer<T>(
        EntityManager em,
        Entity boundaryEntity,
        Allocator allocator)
        where T : unmanaged, IBufferElementData
    {
        if (!em.HasBuffer<T>(boundaryEntity))
            return new NativeArray<T>(0, allocator);

        DynamicBuffer<T> buffer = em.GetBuffer<T>(boundaryEntity, true);
        NativeArray<T> copy = new(buffer.Length, allocator);
        for (int i = 0; i < buffer.Length; i++)
            copy[i] = buffer[i];

        return copy;
    }

    [BurstCompile]
    private struct SelectBuildDecisionJob : IJob
    {
        [ReadOnly] public NativeArray<BuildCandidateEntry> Entries;
        [ReadOnly] public NativeArray<BuildingConfiguredSpawnableReadModel> Spawnables;
        [ReadOnly] public NativeArray<BuildingRuntimeOwnedBuildingSummary> OwnedSummaries;
        [ReadOnly] public NativeArray<BuildingRuntimeSpawnRequest> SpawnRequests;
        public AIBuildPlan Plan;
        public int EconomyMoney;
        public NativeReference<BuildDecision> Decision;

        public void Execute()
        {
            BuildDecision decision = default;
            if (Entries.Length == 0)
            {
                Decision.Value = decision;
                return;
            }

            int attempts = math.max(1, Entries.Length);
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int candidateIndex = PositiveModulo(Plan.NextBuildIndex + attempt, Entries.Length);
                BuildCandidateEntry entry = Entries[candidateIndex];
                if (entry.IsValid == 0)
                    continue;

                if (TryGetOwnedBuildingCount(entry.BuildingId, Plan.FactionId, out int ownedCount) &&
                    ownedCount > 0)
                {
                    continue;
                }

                decision.EntryIndex = entry.EntryIndex;
                decision.BuildingId = entry.BuildingId;
                if (HasPendingSpawnRequest(entry.BuildingId, Plan.FactionId))
                {
                    decision.Result = BuildDecisionResult.Pending;
                    break;
                }

                if (!TryResolveSpawnableReadModel(entry.BuildingId, out BuildingConfiguredSpawnableReadModel spawnable) ||
                    spawnable.CanRequest == 0)
                {
                    decision.Result = BuildDecisionResult.MissingConfig;
                    break;
                }

                int cost = math.max(0, spawnable.Price);
                decision.Spawnable = spawnable;
                decision.Cost = cost;
                if (EconomyMoney < cost)
                {
                    decision.Result = BuildDecisionResult.InsufficientFunds;
                    break;
                }

                decision.Result = BuildDecisionResult.Request;
                decision.PreferredOrigin = ResolvePreferredOriginCell(Plan.BaseCenterCell, entry.EntryIndex);
                break;
            }

            Decision.Value = decision;
        }

        private bool TryResolveSpawnableReadModel(
            FixedString128Bytes buildingId,
            out BuildingConfiguredSpawnableReadModel spawnable)
        {
            for (int i = 0; i < Spawnables.Length; i++)
            {
                BuildingConfiguredSpawnableReadModel candidate = Spawnables[i];
                if (!candidate.BuildingId.Equals(buildingId))
                    continue;

                spawnable = candidate;
                return true;
            }

            spawnable = default;
            return false;
        }

        private bool TryGetOwnedBuildingCount(
            FixedString128Bytes buildingId,
            byte factionId,
            out int count)
        {
            for (int i = 0; i < OwnedSummaries.Length; i++)
            {
                BuildingRuntimeOwnedBuildingSummary summary = OwnedSummaries[i];
                if (summary.FactionId != factionId || !summary.BuildingId.Equals(buildingId))
                    continue;

                count = summary.Count;
                return true;
            }

            count = 0;
            return false;
        }

        private bool HasPendingSpawnRequest(FixedString128Bytes buildingId, byte factionId)
        {
            for (int i = 0; i < SpawnRequests.Length; i++)
            {
                BuildingRuntimeSpawnRequest request = SpawnRequests[i];
                if (request.FactionId == factionId &&
                    request.BuildingId.Equals(buildingId) &&
                    request.Status == BuildingRuntimeSpawnRequest.Pending)
                {
                    return true;
                }
            }

            return false;
        }
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

    private void EnqueueSpawnRequest(
        ref SystemState state,
        Entity boundaryEntity,
        Entity planEntity,
        byte factionId,
        string buildingId,
        int entryIndex,
        int2 preferredOrigin,
        int cost,
        FixedString128Bytes displayName)
    {
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
            state.EntityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
        requests.Add(new BuildingRuntimeSpawnRequest
        {
            RequestId = ++_nextBuildSpawnRequestId,
            FactionId = factionId,
            HasOwnerFaction = 1,
            BuildingId = ToFixedString128(BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId)),
            PreferredOrigin = preferredOrigin,
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
                economy.Money = math.max(0, economy.Money - math.max(0, request.Cost));
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
            return FactionIdentity.IsAiControlledByDefault(factionId);

        for (int i = 0; i < controls.Length; i++)
        {
            FactionControlEntry control = controls[i];
            if (control.FactionId == factionId)
                return control.AIControlled != 0;
        }

        return FactionIdentity.IsAiControlledByDefault(factionId);
    }

    private static int2 ResolveDefaultBaseCenter(byte factionId, GridConfig grid)
    {
        int x = FactionIdentity.IsPlayerControlled(factionId) ? grid.Width / 4 : (grid.Width * 3) / 4;
        int y = grid.Height / 2;
        return new int2(math.max(0, x), math.max(0, y));
    }

    private static int2 ResolvePreferredOriginCell(int2 baseCenterCell, int entryIndex)
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

        return baseCenterCell + offset;
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
