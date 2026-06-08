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
    private float _nextTargetRefreshTime;

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

        using NativeArray<Entity> squads = _squadQuery.ToEntityArray(Allocator.Temp);
        using NativeArray<Entity> targets = _targetQuery.ToEntityArray(Allocator.Temp);
        using NativeArray<AITargetPrioritySetting> targetPriorities =
            _targetPriorityQuery.ToComponentDataArray<AITargetPrioritySetting>(Allocator.Temp);

        for (int i = 0; i < squads.Length; i++)
        {
            Entity squadEntity = squads[i];
            AISquad squad = em.GetComponentData<AISquad>(squadEntity);
            if (squad.Purpose != (byte)AISquadPurpose.Attack)
                continue;

            AITargetPriority priority = ResolveTargetPriority(targetPriorities, squad.FactionId);
            if (!TrySelectTarget(em, targets, squad, priority, out Entity target, out int2 targetCell, out byte targetFaction, out AITargetKind kind, out int score, out string reason))
            {
                if (now - squad.LastLogTime >= LogIntervalSeconds)
                {
                    squad.LastLogTime = now;
                    if (shouldLog)
                        EnqueueDiagnostic(ref state, $"[AITarget] faction={squad.FactionId} squad={squad.SquadId} result=NoTarget");
                    em.SetComponentData(squadEntity, squad);
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
                if (shouldLog)
                    EnqueueDiagnostic(ref state, $"[AITarget] faction={squad.FactionId} squad={squad.SquadId} target={kind} score={score} reason={reason} targetFaction={targetFaction} targetCell={targetCell}");
            }

            em.SetComponentData(squadEntity, squad);
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
        NativeArray<Entity> targets,
        AISquad squad,
        AITargetPriority priority,
        out Entity bestTarget,
        out int2 bestCell,
        out byte bestFaction,
        out AITargetKind bestKind,
        out int bestScore,
        out string bestReason)
    {
        bestTarget = Entity.Null;
        bestCell = squad.TargetCell;
        bestFaction = squad.TargetFactionId;
        bestKind = AITargetKind.None;
        bestScore = int.MinValue;
        bestReason = "None";

        for (int i = 0; i < targets.Length; i++)
        {
            Entity target = targets[i];
            if (!em.Exists(target))
                continue;

            Faction faction = em.GetComponentData<Faction>(target);
            if (faction.Id == squad.FactionId)
                continue;

            UnitHealth health = em.GetComponentData<UnitHealth>(target);
            if (health.Current <= 0)
                continue;

            UnitGrid grid = em.GetComponentData<UnitGrid>(target);
            AITargetKind kind = ResolveTargetKind(em, target);
            int score = ScoreTarget(em, target, kind, priority, squad.RallyCell, grid.Cell, health, out string reason);
            if (score <= bestScore)
                continue;

            bestTarget = target;
            bestCell = grid.Cell;
            bestFaction = faction.Id;
            bestKind = kind;
            bestScore = score;
            bestReason = reason;
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

    private static AITargetPriority ResolveTargetPriority(NativeArray<AITargetPrioritySetting> settings, byte factionId)
    {
        if (!settings.IsCreated)
            return AITargetPriority.Balanced;

        for (int i = 0; i < settings.Length; i++)
        {
            AITargetPrioritySetting setting = settings[i];
            if (setting.FactionId == factionId)
                return (AITargetPriority)setting.Priority;
        }

        return AITargetPriority.Balanced;
    }

    private static int ScoreTarget(EntityManager em, Entity target, AITargetKind kind, AITargetPriority priority, int2 origin, int2 targetCell, UnitHealth health, out string reason)
    {
        int distance = math.abs(targetCell.x - origin.x) + math.abs(targetCell.y - origin.y);
        int healthValue = math.clamp(health.Max / 10, 0, 30);
        int score = 100 - math.min(distance, 100) + healthValue;

        switch (kind)
        {
            case AITargetKind.Threat:
                score += 45;
                reason = "Threat";
                break;
            case AITargetKind.Building:
                score += 35;
                reason = "Economy";
                break;
            default:
                score += 10;
                reason = "Unit";
                break;
        }

        if (em.HasComponent<UnitResourceHauler>(target))
        {
            score += 20;
            reason = "Economy";
        }

        switch (priority)
        {
            case AITargetPriority.Units:
                if (kind == AITargetKind.Unit || kind == AITargetKind.Threat)
                {
                    score += 35;
                    reason = kind == AITargetKind.Threat ? "Threat" : "Units";
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
                    reason = "Economy";
                }
                else if (kind == AITargetKind.Building)
                {
                    score += 25;
                    reason = "Economy";
                }
                break;
            case AITargetPriority.Production:
                if (kind == AITargetKind.Building)
                {
                    score += 45;
                    reason = "Production";
                }
                else if (kind == AITargetKind.Unit)
                {
                    score -= 10;
                }
                break;
        }

        return score;
    }
}
