using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(AISquadSystem))]
public partial struct AITargetingSystem : ISystem
{
    private const float LogIntervalSeconds = 6f;
    private const float TargetRefreshSeconds = 0.5f;
    private EntityQuery _squadQuery;
    private EntityQuery _targetQuery;
    private EntityQuery _targetPriorityQuery;
    private EntityQuery _diagnosticLogQueueQuery;
    private EntityTypeHandle _entityType;
    private ComponentTypeHandle<AISquad> _squadType;
    private ComponentTypeHandle<Faction> _factionType;
    private ComponentTypeHandle<UnitGrid> _unitGridType;
    private ComponentTypeHandle<UnitHealth> _unitHealthType;
    private ComponentTypeHandle<AITargetPrioritySetting> _targetPriorityType;
    private float _nextTargetRefreshTime;

    private enum TargetReason : byte
    {
        None,
        Threat,
        Economy,
        Unit,
        Units,
        Production
    }

    public void OnCreate(ref SystemState state)
    {
        _squadQuery = state.GetEntityQuery(ComponentType.ReadWrite<AISquad>());
        _targetQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitHealth>());
        _targetPriorityQuery = state.GetEntityQuery(ComponentType.ReadOnly<AITargetPrioritySetting>());
        _diagnosticLogQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<AIDiagnosticLogComponent>());
        _entityType = state.GetEntityTypeHandle();
        _squadType = state.GetComponentTypeHandle<AISquad>(false);
        _factionType = state.GetComponentTypeHandle<Faction>(true);
        _unitGridType = state.GetComponentTypeHandle<UnitGrid>(true);
        _unitHealthType = state.GetComponentTypeHandle<UnitHealth>(true);
        _targetPriorityType = state.GetComponentTypeHandle<AITargetPrioritySetting>(true);
        state.RequireForUpdate<AISquad>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
            return;

        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
        if (now < _nextTargetRefreshTime)
            return;

        _nextTargetRefreshTime = now + TargetRefreshSeconds;

        EntityManager em = state.EntityManager;
        bool shouldLog = ShouldQueueDiagnostics(ref state);

        _entityType.Update(ref state);
        _squadType.Update(ref state);
        _factionType.Update(ref state);
        _unitGridType.Update(ref state);
        _unitHealthType.Update(ref state);
        _targetPriorityType.Update(ref state);

        using NativeArray<ArchetypeChunk> squadChunks = _squadQuery.ToArchetypeChunkArray(Allocator.Temp);
        using NativeArray<ArchetypeChunk> targetChunks = _targetQuery.ToArchetypeChunkArray(Allocator.Temp);
        using NativeArray<ArchetypeChunk> targetPriorityChunks = _targetPriorityQuery.ToArchetypeChunkArray(Allocator.Temp);

        for (int chunkIndex = 0; chunkIndex < squadChunks.Length; chunkIndex++)
        {
            ArchetypeChunk squadChunk = squadChunks[chunkIndex];
            NativeArray<AISquad> squads = squadChunk.GetNativeArray(ref _squadType);

            for (int i = 0; i < squads.Length; i++)
            {
                AISquad squad = squads[i];
                if (squad.Purpose != (byte)AISquadPurpose.Attack)
                    continue;

                AITargetPriority priority = ResolveTargetPriority(targetPriorityChunks, ref _targetPriorityType, squad.FactionId);
                if (!TrySelectTarget(
                        em,
                        targetChunks,
                        _entityType,
                        ref _factionType,
                        ref _unitGridType,
                        ref _unitHealthType,
                        squad,
                        priority,
                        out Entity target,
                        out int2 targetCell,
                        out byte targetFaction,
                        out AITargetKind kind,
                        out int score,
                        out TargetReason reason))
                {
                    if (now - squad.LastLogTime >= LogIntervalSeconds)
                    {
                        squad.LastLogTime = now;
                        squads[i] = squad;
                        if (shouldLog)
                            EnqueueDiagnostic(ref state, $"[AITarget] faction={squad.FactionId} squad={squad.SquadId} result=NoTarget");
                    }
                    continue;
                }

                bool changed =
                    squad.TargetEntity != target ||
                    squad.TargetCell.x != targetCell.x ||
                    squad.TargetCell.y != targetCell.y ||
                    squad.TargetScore != score ||
                    squad.TargetKind != (byte)kind;

                squad.TargetEntity = target;
                squad.TargetFactionId = targetFaction;
                squad.TargetKind = (byte)kind;
                squad.TargetCell = targetCell;
                squad.TargetScore = score;

                if (changed || now - squad.LastLogTime >= LogIntervalSeconds)
                {
                    squad.LastLogTime = now;
                    squads[i] = squad;
                    if (shouldLog)
                        EnqueueDiagnostic(ref state, $"[AITarget] faction={squad.FactionId} squad={squad.SquadId} target={kind} score={score} reason={TargetReasonLabel(reason)} targetFaction={targetFaction} targetCell={targetCell}");
                }
                else
                {
                    squads[i] = squad;
                }
            }
        }
    }

    private bool ShouldQueueDiagnostics(ref SystemState state)
    {
        if (Application.isBatchMode)
            return true;

        return SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
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

    private static bool TrySelectTarget(
        EntityManager em,
        NativeArray<ArchetypeChunk> targetChunks,
        EntityTypeHandle entityType,
        ref ComponentTypeHandle<Faction> factionType,
        ref ComponentTypeHandle<UnitGrid> unitGridType,
        ref ComponentTypeHandle<UnitHealth> unitHealthType,
        AISquad squad,
        AITargetPriority priority,
        out Entity bestTarget,
        out int2 bestCell,
        out byte bestFaction,
        out AITargetKind bestKind,
        out int bestScore,
        out TargetReason bestReason)
    {
        bestTarget = Entity.Null;
        bestCell = squad.TargetCell;
        bestFaction = squad.TargetFactionId;
        bestKind = AITargetKind.None;
        bestScore = int.MinValue;
        bestReason = TargetReason.None;

        for (int chunkIndex = 0; chunkIndex < targetChunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = targetChunks[chunkIndex];
            NativeArray<Entity> targets = chunk.GetNativeArray(entityType);
            NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);
            NativeArray<UnitGrid> grids = chunk.GetNativeArray(ref unitGridType);
            NativeArray<UnitHealth> healths = chunk.GetNativeArray(ref unitHealthType);

            for (int i = 0; i < targets.Length; i++)
            {
                Entity target = targets[i];
                Faction faction = factions[i];
                if (faction.Id == squad.FactionId)
                    continue;

                UnitHealth health = healths[i];
                if (health.Current <= 0)
                    continue;

                UnitGrid grid = grids[i];
                AITargetKind kind = ResolveTargetKind(em, target);
                int score = ScoreTarget(em, target, kind, priority, squad.RallyCell, grid.Cell, health, out TargetReason reason);
                if (score <= bestScore)
                    continue;

                bestTarget = target;
                bestCell = grid.Cell;
                bestFaction = faction.Id;
                bestKind = kind;
                bestScore = score;
                bestReason = reason;
            }
        }

        return bestTarget != Entity.Null;
    }

    private static AITargetKind ResolveTargetKind(EntityManager em, Entity target)
    {
        if (em.HasComponent<UnitAttack>(target) || em.HasComponent<UnitCombat>(target))
            return AITargetKind.Threat;
        if (em.HasComponent<StaticGridBlocker>(target) || em.HasComponent<GridBlockerSize>(target))
            return AITargetKind.Building;
        return AITargetKind.Unit;
    }

    private static AITargetPriority ResolveTargetPriority(
        NativeArray<ArchetypeChunk> chunks,
        ref ComponentTypeHandle<AITargetPrioritySetting> targetPriorityType,
        byte factionId)
    {
        if (!chunks.IsCreated)
            return AITargetPriority.Balanced;

        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<AITargetPrioritySetting> settings = chunks[chunkIndex].GetNativeArray(ref targetPriorityType);
            for (int i = 0; i < settings.Length; i++)
            {
                AITargetPrioritySetting setting = settings[i];
                if (setting.FactionId == factionId)
                    return (AITargetPriority)setting.Priority;
            }
        }

        return AITargetPriority.Balanced;
    }

    private static int ScoreTarget(EntityManager em, Entity target, AITargetKind kind, AITargetPriority priority, int2 origin, int2 targetCell, UnitHealth health, out TargetReason reason)
    {
        int distance = math.abs(targetCell.x - origin.x) + math.abs(targetCell.y - origin.y);
        int healthValue = math.clamp(health.Max / 10, 0, 30);
        int score = 100 - math.min(distance, 100) + healthValue;

        switch (kind)
        {
            case AITargetKind.Threat:
                score += 45;
                reason = TargetReason.Threat;
                break;
            case AITargetKind.Building:
                score += 35;
                reason = TargetReason.Economy;
                break;
            default:
                score += 10;
                reason = TargetReason.Unit;
                break;
        }

        if (em.HasComponent<UnitResourceHauler>(target))
        {
            score += 20;
            reason = TargetReason.Economy;
        }

        switch (priority)
        {
            case AITargetPriority.Units:
                if (kind == AITargetKind.Unit || kind == AITargetKind.Threat)
                {
                    score += 35;
                    reason = kind == AITargetKind.Threat ? TargetReason.Threat : TargetReason.Units;
                }
                else if (kind == AITargetKind.Building)
                {
                    score -= 10;
                }
                break;
            case AITargetPriority.Economy:
                if (em.HasComponent<UnitResourceHauler>(target))
                {
                    score += 50;
                    reason = TargetReason.Economy;
                }
                else if (kind == AITargetKind.Building)
                {
                    score += 25;
                    reason = TargetReason.Economy;
                }
                break;
            case AITargetPriority.Production:
                if (kind == AITargetKind.Building)
                {
                    score += 45;
                    reason = TargetReason.Production;
                }
                else if (kind == AITargetKind.Unit)
                {
                    score -= 10;
                }
                break;
        }

        return score;
    }

    private static string TargetReasonLabel(TargetReason reason)
    {
        return reason switch
        {
            TargetReason.Threat => "Threat",
            TargetReason.Economy => "Economy",
            TargetReason.Unit => "Unit",
            TargetReason.Units => "Units",
            TargetReason.Production => "Production",
            _ => "None"
        };
    }
}
