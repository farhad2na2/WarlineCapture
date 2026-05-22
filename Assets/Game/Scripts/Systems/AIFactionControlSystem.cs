using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateBefore(typeof(UnitPathfindingSystem))]
public partial struct AIFactionControlSystem : ISystem
{
    private const float LogIntervalSeconds = 10f;
    private EntityQuery _buildingPlacementRuntimeQuery;

    public void OnCreate(ref SystemState state)
    {
        _buildingPlacementRuntimeQuery = state.GetEntityQuery(ComponentType.ReadOnly<BuildingPlacementRuntimeComponent>());
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
        DynamicBuffer<FactionControlEntry> controls = SystemAPI.GetSingletonBuffer<FactionControlEntry>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        BuildingPlacementSystem buildingPlacement = GetBuildingPlacement(ref state);

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

            bool shouldLog = AILog.IsEnabled && now - control.LastLogTime >= LogIntervalSeconds;
            if (shouldLog)
            {
                control.LastLogTime = now;
                controls[controlIndex] = control;
                int controlledBuildings = buildingPlacement != null
                    ? buildingPlacement.CountRuntimeBuildingsForFaction(control.FactionId)
                    : 0;
                AILog.Log($"[AIControlMode] faction={control.FactionId} mode={(aiControlled ? "Auto" : "Manual")} controlledUnits={controlledUnits} controlledBuildings={controlledBuildings}");
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

    private BuildingPlacementSystem GetBuildingPlacement(ref SystemState state)
    {
        if (_buildingPlacementRuntimeQuery.IsEmptyIgnoreFilter)
            return null;

        Entity entity = _buildingPlacementRuntimeQuery.GetSingletonEntity();
        return state.EntityManager.GetComponentObject<BuildingPlacementRuntimeComponent>(entity).BuildingPlacement;
    }

    private static void RemoveIfPresent<T>(ref EntityCommandBuffer ecb, ref SystemState state, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (state.EntityManager.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
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
