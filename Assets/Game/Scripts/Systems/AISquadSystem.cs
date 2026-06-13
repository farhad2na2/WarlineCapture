using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateAfter(typeof(AIProductionSystem))]
public partial struct AISquadSystem : ISystem
{
    private const float LogIntervalSeconds = 10f;
    private EntityQuery _diagnosticLogQueueQuery;
    private EntityQuery _planQuery;
    private EntityQuery _unitQuery;
    private EntityQuery _squadQuery;
    private EntityQuery _factionGridQuery;
    private EntityTypeHandle _entityType;
    private ComponentTypeHandle<AISquad> _squadType;
    private ComponentTypeHandle<Faction> _factionType;
    private ComponentTypeHandle<UnitGrid> _unitGridType;
    private ComponentTypeHandle<UnitHealth> _unitHealthType;
    private ComponentLookup<AISquadMember> _squadMemberLookup;
    private ComponentLookup<StaticGridBlocker> _staticGridBlockerLookup;
    private ComponentLookup<EngageTarget> _engageTargetLookup;
    private ComponentLookup<UnitPathRequest> _pathRequestLookup;

    private struct CandidateUnitRecord
    {
        public Entity Entity;
        public byte FactionId;
        public int2 Cell;
        public byte Assigned;
    }

    public void OnCreate(ref SystemState state)
    {
        _diagnosticLogQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<AIDiagnosticLogComponent>());
        _planQuery = state.GetEntityQuery(ComponentType.ReadWrite<AISquadPlan>());
        _unitQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitHealth>(),
            ComponentType.ReadOnly<AIControlledTag>());
        _squadQuery = state.GetEntityQuery(ComponentType.ReadOnly<AISquad>());
        _factionGridQuery = state.GetEntityQuery(ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitGrid>());
        _entityType = state.GetEntityTypeHandle();
        _squadType = state.GetComponentTypeHandle<AISquad>(true);
        _factionType = state.GetComponentTypeHandle<Faction>(true);
        _unitGridType = state.GetComponentTypeHandle<UnitGrid>(true);
        _unitHealthType = state.GetComponentTypeHandle<UnitHealth>(true);
        _squadMemberLookup = state.GetComponentLookup<AISquadMember>(true);
        _staticGridBlockerLookup = state.GetComponentLookup<StaticGridBlocker>(true);
        _engageTargetLookup = state.GetComponentLookup<EngageTarget>(true);
        _pathRequestLookup = state.GetComponentLookup<UnitPathRequest>(true);
        state.RequireForUpdate<AISquadPlan>();
        state.RequireForUpdate<Faction>();
        state.RequireForUpdate<UnitGrid>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
            return;

        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
        bool hasControls = SystemAPI.HasSingleton<FactionControlConfigTag>();
        DynamicBuffer<FactionControlEntry> controls = hasControls
            ? SystemAPI.GetSingletonBuffer<FactionControlEntry>(true)
            : default;
        bool shouldLog = ShouldQueueDiagnostics(ref state);

        EntityManager em = state.EntityManager;
        _entityType.Update(ref state);
        using NativeArray<ArchetypeChunk> planChunks = _planQuery.ToArchetypeChunkArray(Allocator.Temp);
        using NativeList<Entity> planEntities = new(_planQuery.CalculateEntityCount(), Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < planChunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = planChunks[chunkIndex].GetNativeArray(_entityType);
            planEntities.AddRange(entities);
        }

        NativeList<CandidateUnitRecord> candidateUnits = BuildAvailableUnitCandidates(ref state);
        try
        {
            for (int i = 0; i < planEntities.Length; i++)
            {
                Entity planEntity = planEntities[i];
                AISquadPlan plan = em.GetComponentData<AISquadPlan>(planEntity);
                if (plan.Enabled == 0 || !IsFactionAIControlled(plan.FactionId, hasControls, controls))
                    continue;

                _squadType.Update(ref state);
                _factionType.Update(ref state);
                _unitGridType.Update(ref state);

                int activeSquads = CountActiveSquads(_squadQuery, ref _squadType, plan.FactionId, state.Dependency);
                int maxActiveSquads = math.max(1, plan.MaxActiveSquads);
                if (activeSquads >= maxActiveSquads)
                {
                    LogCompleteIfNeeded(ref state, ref plan, now, activeSquads, shouldLog);
                    em.SetComponentData(planEntity, plan);
                    continue;
                }

                int maxUnits = math.max(1, plan.MaxUnits);
                int minUnits = math.clamp(math.max(1, plan.MinUnits), 1, maxUnits);
                using NativeList<Entity> members = new(maxUnits, Allocator.Temp);
                using NativeList<int> memberCandidateIndices = new(maxUnits, Allocator.Temp);
                int2 cellSum = int2.zero;

                for (int unitIndex = 0; unitIndex < candidateUnits.Length && members.Length < maxUnits; unitIndex++)
                {
                    CandidateUnitRecord candidate = candidateUnits[unitIndex];
                    if (candidate.Assigned != 0 ||
                        candidate.FactionId != plan.FactionId)
                        continue;

                    members.Add(candidate.Entity);
                    memberCandidateIndices.Add(unitIndex);
                    cellSum += candidate.Cell;
                }

                if (members.Length < minUnits)
                {
                    if (now - plan.LastLogTime >= LogIntervalSeconds)
                    {
                        plan.LastLogTime = now;
                        if (shouldLog)
                            EnqueueDiagnostic(ref state, $"[AISquad] faction={plan.FactionId} result=Waiting units={members.Length} minUnits={minUnits}");
                    }
                    em.SetComponentData(planEntity, plan);
                    continue;
                }

                int squadId = plan.NextSquadId <= 0 ? 1 : plan.NextSquadId;
                byte targetFactionId = FactionIdentitySystem.ResolveDefaultTargetFaction(plan.FactionId);
                int2 rallyCell = cellSum / members.Length;
                int2 targetCell = ResolveInitialTargetCell(
                    _factionGridQuery,
                    ref _factionType,
                    ref _unitGridType,
                    targetFactionId,
                    rallyCell,
                    state.Dependency);
                Entity squadEntity = em.CreateEntity(typeof(AISquad));
                DynamicBuffer<AISquadUnit> squadUnits = em.AddBuffer<AISquadUnit>(squadEntity);
                em.SetComponentData(squadEntity, new AISquad
                {
                    SquadId = squadId,
                    FactionId = plan.FactionId,
                    Purpose = (byte)AISquadPurpose.Attack,
                    TargetFactionId = targetFactionId,
                    TargetKind = (byte)AITargetKind.None,
                    TargetEntity = Entity.Null,
                    RallyCell = rallyCell,
                    TargetCell = targetCell,
                    TargetScore = 0,
                    MinUnits = minUnits,
                    MaxUnits = maxUnits,
                    LastOrderTime = -999f,
                    LastLogTime = now
                });

                for (int memberIndex = 0; memberIndex < members.Length; memberIndex++)
                {
                    Entity unit = members[memberIndex];
                    squadUnits.Add(new AISquadUnit { Unit = unit });
                }

                for (int memberIndex = 0; memberIndex < members.Length; memberIndex++)
                {
                    Entity unit = members[memberIndex];
                    em.AddComponentData(unit, new AISquadMember
                    {
                        Squad = squadEntity,
                        SquadId = squadId
                    });
                }

                for (int memberIndex = 0; memberIndex < memberCandidateIndices.Length; memberIndex++)
                {
                    int candidateIndex = memberCandidateIndices[memberIndex];
                    CandidateUnitRecord candidate = candidateUnits[candidateIndex];
                    candidate.Assigned = 1;
                    candidateUnits[candidateIndex] = candidate;
                }

                plan.NextSquadId = squadId + 1;
                plan.LastLogTime = now;
                em.SetComponentData(planEntity, plan);
                if (shouldLog)
                    EnqueueDiagnostic(ref state, $"[AISquad] faction={plan.FactionId} squad={squadId} purpose=Attack units={members.Length} targetFaction={targetFactionId} targetCell={targetCell}");
            }
        }
        finally
        {
            if (candidateUnits.IsCreated)
                candidateUnits.Dispose();
        }

    }

    private static int CountActiveSquads(
        EntityQuery squadQuery,
        ref ComponentTypeHandle<AISquad> squadType,
        byte factionId,
        JobHandle dependency)
    {
        using NativeArray<ArchetypeChunk> chunks = squadQuery.ToArchetypeChunkArray(Allocator.TempJob);
        using NativeReference<int> count = new(Allocator.TempJob);

        new CountActiveSquadsJob
        {
            Chunks = chunks,
            SquadType = squadType,
            FactionId = factionId,
            Count = count
        }.Schedule(dependency).Complete();

        return count.Value;
    }

    private NativeList<CandidateUnitRecord> BuildAvailableUnitCandidates(ref SystemState state)
    {
        _entityType.Update(ref state);
        _factionType.Update(ref state);
        _unitGridType.Update(ref state);
        _unitHealthType.Update(ref state);
        _squadMemberLookup.Update(ref state);
        _staticGridBlockerLookup.Update(ref state);
        _engageTargetLookup.Update(ref state);
        _pathRequestLookup.Update(ref state);

        NativeList<CandidateUnitRecord> candidates = new(_unitQuery.CalculateEntityCount(), Allocator.TempJob);
        using NativeArray<ArchetypeChunk> chunks = _unitQuery.ToArchetypeChunkArray(Allocator.TempJob);

        new BuildAvailableUnitCandidatesJob
        {
            Chunks = chunks,
            EntityType = _entityType,
            FactionType = _factionType,
            UnitGridType = _unitGridType,
            UnitHealthType = _unitHealthType,
            SquadMemberLookup = _squadMemberLookup,
            StaticGridBlockerLookup = _staticGridBlockerLookup,
            EngageTargetLookup = _engageTargetLookup,
            PathRequestLookup = _pathRequestLookup,
            Candidates = candidates
        }.Schedule(state.Dependency).Complete();

        return candidates;
    }

    private static int2 ResolveInitialTargetCell(
        EntityQuery factionGridQuery,
        ref ComponentTypeHandle<Faction> factionType,
        ref ComponentTypeHandle<UnitGrid> unitGridType,
        byte targetFactionId,
        int2 fallbackCell,
        JobHandle dependency)
    {
        using NativeArray<ArchetypeChunk> chunks = factionGridQuery.ToArchetypeChunkArray(Allocator.TempJob);
        using NativeReference<int2> targetCell = new(Allocator.TempJob);

        new ResolveInitialTargetCellJob
        {
            Chunks = chunks,
            FactionType = factionType,
            UnitGridType = unitGridType,
            TargetFactionId = targetFactionId,
            FallbackCell = fallbackCell,
            TargetCell = targetCell
        }.Schedule(dependency).Complete();

        return targetCell.Value;
    }

    [BurstCompile]
    private struct CountActiveSquadsJob : IJob
    {
        [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
        [ReadOnly] public ComponentTypeHandle<AISquad> SquadType;
        public byte FactionId;
        public NativeReference<int> Count;

        public void Execute()
        {
            int count = 0;
            ComponentTypeHandle<AISquad> squadType = SquadType;
            for (int chunkIndex = 0; chunkIndex < Chunks.Length; chunkIndex++)
            {
                NativeArray<AISquad> squads = Chunks[chunkIndex].GetNativeArray(ref squadType);
                for (int i = 0; i < squads.Length; i++)
                {
                    if (squads[i].FactionId == FactionId)
                        count++;
                }
            }

            Count.Value = count;
        }
    }

    [BurstCompile]
    private struct BuildAvailableUnitCandidatesJob : IJob
    {
        [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
        [ReadOnly] public EntityTypeHandle EntityType;
        [ReadOnly] public ComponentTypeHandle<Faction> FactionType;
        [ReadOnly] public ComponentTypeHandle<UnitGrid> UnitGridType;
        [ReadOnly] public ComponentTypeHandle<UnitHealth> UnitHealthType;
        [ReadOnly] public ComponentLookup<AISquadMember> SquadMemberLookup;
        [ReadOnly] public ComponentLookup<StaticGridBlocker> StaticGridBlockerLookup;
        [ReadOnly] public ComponentLookup<EngageTarget> EngageTargetLookup;
        [ReadOnly] public ComponentLookup<UnitPathRequest> PathRequestLookup;
        public NativeList<CandidateUnitRecord> Candidates;

        public void Execute()
        {
            EntityTypeHandle entityType = EntityType;
            ComponentTypeHandle<Faction> factionType = FactionType;
            ComponentTypeHandle<UnitGrid> unitGridType = UnitGridType;
            ComponentTypeHandle<UnitHealth> unitHealthType = UnitHealthType;

            for (int chunkIndex = 0; chunkIndex < Chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = Chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);
                NativeArray<UnitGrid> grids = chunk.GetNativeArray(ref unitGridType);
                NativeArray<UnitHealth> healths = chunk.GetNativeArray(ref unitHealthType);

                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (SquadMemberLookup.HasComponent(entity) ||
                        StaticGridBlockerLookup.HasComponent(entity) ||
                        EngageTargetLookup.HasComponent(entity) ||
                        PathRequestLookup.HasComponent(entity) ||
                        healths[i].Current <= 0)
                    {
                        continue;
                    }

                    Candidates.Add(new CandidateUnitRecord
                    {
                        Entity = entity,
                        FactionId = factions[i].Id,
                        Cell = grids[i].Cell
                    });
                }
            }
        }
    }

    [BurstCompile]
    private struct ResolveInitialTargetCellJob : IJob
    {
        [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
        [ReadOnly] public ComponentTypeHandle<Faction> FactionType;
        [ReadOnly] public ComponentTypeHandle<UnitGrid> UnitGridType;
        public byte TargetFactionId;
        public int2 FallbackCell;
        public NativeReference<int2> TargetCell;

        public void Execute()
        {
            int bestDistance = int.MaxValue;
            int2 bestCell = FallbackCell;
            bool found = false;
            ComponentTypeHandle<Faction> factionType = FactionType;
            ComponentTypeHandle<UnitGrid> unitGridType = UnitGridType;

            for (int chunkIndex = 0; chunkIndex < Chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = Chunks[chunkIndex];
                NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);
                NativeArray<UnitGrid> grids = chunk.GetNativeArray(ref unitGridType);

                for (int i = 0; i < factions.Length; i++)
                {
                    if (factions[i].Id != TargetFactionId)
                        continue;

                    int2 cell = grids[i].Cell;
                    int distance = math.abs(cell.x - FallbackCell.x) + math.abs(cell.y - FallbackCell.y);
                    if (found && distance >= bestDistance)
                        continue;

                    found = true;
                    bestDistance = distance;
                    bestCell = cell;
                }
            }

            TargetCell.Value = bestCell;
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

    private void LogCompleteIfNeeded(ref SystemState state, ref AISquadPlan plan, float now, int activeSquads, bool shouldLog)
    {
        if (now - plan.LastLogTime < LogIntervalSeconds)
            return;

        plan.LastLogTime = now;
        if (shouldLog)
            EnqueueDiagnostic(ref state, $"[AISquad] faction={plan.FactionId} result=Complete activeSquads={activeSquads}");
    }
}
