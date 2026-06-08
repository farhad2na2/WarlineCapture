using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateBefore(typeof(UnitPathfindingSystem))]
public partial struct AIFactionControlSystem : ISystem
{
    private const float LogIntervalSeconds = 10f;
    private const float ControlRefreshSeconds = 0.5f;
    private EntityQuery _buildingRuntimeBoundaryQuery;
    private EntityQuery _diagnosticLogQueueQuery;
    private float _nextControlRefreshTime;

    public void OnCreate(ref SystemState state)
    {
        _buildingRuntimeBoundaryQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>(),
            ComponentType.ReadOnly<BuildingRuntimeFactionSummary>());
        _diagnosticLogQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<AIDiagnosticLogComponent>());
        state.RequireForUpdate<FactionControlConfigTag>();
        state.RequireForUpdate<Faction>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
            return;

        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
        if (now < _nextControlRefreshTime)
            return;

        _nextControlRefreshTime = now + ControlRefreshSeconds;

        bool shouldLogDiagnostics = ShouldQueueDiagnostics(ref state);
        Entity diagnosticQueueEntity = shouldLogDiagnostics ? EnsureDiagnosticQueue(ref state) : Entity.Null;
        DynamicBuffer<FactionControlEntry> controls = SystemAPI.GetSingletonBuffer<FactionControlEntry>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        for (int controlIndex = 0; controlIndex < controls.Length; controlIndex++)
        {
            FactionControlEntry control = controls[controlIndex];
            bool aiControlled = control.AIControlled != 0;
            int controlledUnits = 0;

            foreach (var (faction, entity) in SystemAPI.Query<RefRO<Faction>>().WithEntityAccess())
            {
                if (faction.ValueRO.Id != control.FactionId)
                    continue;

                if (SystemAPI.HasComponent<UnitGrid>(entity) && !SystemAPI.HasComponent<StaticGridBlocker>(entity))
                    controlledUnits++;

                if (aiControlled)
                {
                    AddIfMissing<AIControlledTag>(ref ecb, ref state, entity);
                    RemoveIfPresent<ManualControlledTag>(ref ecb, ref state, entity);
                    RemoveIfPresent<ManualMoveOrderTag>(ref ecb, ref state, entity);
                    RemoveIfPresent<ManualMoveGroupMemberTag>(ref ecb, ref state, entity);
                    RemoveIfPresent<UnitPathRequest>(ref ecb, ref state, entity);
                    RemoveIfPresent<UnitPathRetryCooldown>(ref ecb, ref state, entity);
                    RemoveCommandedEngageTargetIfPresent(ref ecb, ref state, entity);
                }
                else
                {
                    AddIfMissing<ManualControlledTag>(ref ecb, ref state, entity);
                    RemoveIfPresent<AIControlledTag>(ref ecb, ref state, entity);
                    RemoveAICombatOrderIfPresent(ref ecb, ref state, entity);
                }
            }

            bool shouldLog = shouldLogDiagnostics && now - control.LastLogTime >= LogIntervalSeconds;
            if (shouldLog)
            {
                control.LastLogTime = now;
                controls[controlIndex] = control;
                TryGetFactionBuildingCount(ref state, control.FactionId, out int controlledBuildings);
                EnqueueDiagnostic(
                    ref state,
                    diagnosticQueueEntity,
                    $"[AIControlMode] faction={control.FactionId} mode={(aiControlled ? "Auto" : "Manual")} controlledUnits={controlledUnits} controlledBuildings={controlledBuildings}");
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private static void AddIfMissing<T>(ref EntityCommandBuffer ecb, ref SystemState state, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (!state.EntityManager.HasComponent<T>(entity))
            ecb.AddComponent<T>(entity);
    }

    private bool TryGetFactionBuildingCount(ref SystemState state, byte factionId, out int buildingCount)
    {
        buildingCount = 0;
        if (_buildingRuntimeBoundaryQuery.IsEmptyIgnoreFilter)
            return false;

        Entity entity = _buildingRuntimeBoundaryQuery.GetSingletonEntity();
        if (!state.EntityManager.HasBuffer<BuildingRuntimeFactionSummary>(entity))
            return false;

        DynamicBuffer<BuildingRuntimeFactionSummary> summaries =
            state.EntityManager.GetBuffer<BuildingRuntimeFactionSummary>(entity, true);
        for (int i = 0; i < summaries.Length; i++)
        {
            BuildingRuntimeFactionSummary summary = summaries[i];
            if (summary.FactionId != factionId)
                continue;

            buildingCount = summary.BuildingCount;
            return true;
        }

        return false;
    }

    private static void RemoveIfPresent<T>(ref EntityCommandBuffer ecb, ref SystemState state, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (state.EntityManager.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
    }

    private bool ShouldQueueDiagnostics(ref SystemState state)
    {
        if (Application.isBatchMode)
            return true;

        return SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
            SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
    }

    private Entity EnsureDiagnosticQueue(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        if (_diagnosticLogQueueQuery.IsEmptyIgnoreFilter)
        {
            Entity queueEntity = em.CreateEntity(typeof(AIDiagnosticLogQueueComponent));
            em.SetName(queueEntity, "AIDiagnosticLogQueue");
            em.AddBuffer<AIDiagnosticLogComponent>(queueEntity);
            return queueEntity;
        }

        return _diagnosticLogQueueQuery.GetSingletonEntity();
    }

    private void EnqueueDiagnostic(ref SystemState state, Entity queueEntity, FixedString512Bytes message)
    {
        EntityManager em = state.EntityManager;
        DynamicBuffer<AIDiagnosticLogComponent> logs = em.GetBuffer<AIDiagnosticLogComponent>(queueEntity);
        logs.Add(new AIDiagnosticLogComponent { Message = message });
    }

    private static void RemoveCommandedEngageTargetIfPresent(ref EntityCommandBuffer ecb, ref SystemState state, Entity entity)
    {
        if (!state.EntityManager.HasComponent<EngageTarget>(entity))
            return;

        EngageTarget target = state.EntityManager.GetComponentData<EngageTarget>(entity);
        if (target.IsCommanded != 0)
            ecb.RemoveComponent<EngageTarget>(entity);
    }

    private static void RemoveAICombatOrderIfPresent(ref EntityCommandBuffer ecb, ref SystemState state, Entity entity)
    {
        if (!state.EntityManager.HasComponent<AICombatOrderTag>(entity))
            return;

        RemoveIfPresent<AICombatOrderTag>(ref ecb, ref state, entity);
        RemoveIfPresent<EngageTarget>(ref ecb, ref state, entity);
        RemoveIfPresent<UnitPathRequest>(ref ecb, ref state, entity);
        RemoveIfPresent<UnitPathFollow>(ref ecb, ref state, entity);
        RemoveIfPresent<UnitPathRange>(ref ecb, ref state, entity);
        RemoveIfPresent<AutoWanderMoveTag>(ref ecb, ref state, entity);
    }
}
