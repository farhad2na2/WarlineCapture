using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(UnitAirMovementSystem))]
public partial struct FixedWingRunwayHomeInitializationSystem : ISystem
{
    private EntityQuery _boundaryQuery;

    public void OnCreate(ref SystemState state)
    {
        _boundaryQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>(),
            ComponentType.ReadOnly<BuildingFactionRunwayReadModel>());

        state.RequireForUpdate(_boundaryQuery);
        state.RequireForUpdate<UnitAirMovement>();
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity boundaryEntity = _boundaryQuery.GetSingletonEntity();
        if (!state.EntityManager.HasBuffer<BuildingFactionRunwayReadModel>(boundaryEntity))
            return;

        DynamicBuffer<BuildingFactionRunwayReadModel> runways =
            state.EntityManager.GetBuffer<BuildingFactionRunwayReadModel>(boundaryEntity, true);
        if (runways.Length == 0)
            return;

        EntityManager em = state.EntityManager;
        foreach (var (airState, transform, grid, faction, sourceKey, entity) in SystemAPI
                     .Query<RefRW<UnitAirComponent>, RefRO<LocalTransform>, RefRO<UnitGrid>, RefRO<Faction>, RefRO<UnitSourcePrefabKey>>()
                     .WithAll<UnitAirMovement, UnitMove>()
                     .WithNone<UnitDeathAnimationComponent, Disabled, UnitSpawnTransitTag>()
                     .WithEntityAccess())
        {
            if (!ShouldInitializeRunwayHome(em, entity, airState.ValueRO, sourceKey.ValueRO.Value))
                continue;

            if (!TryFindNearestRunway(runways, faction.ValueRO.Id, transform.ValueRO.Position, out BuildingFactionRunwayReadModel runway))
                continue;

            UnitAirComponent initialized = airState.ValueRO;
            initialized.HomePosition = transform.ValueRO.Position;
            initialized.HomeCell = grid.ValueRO.Cell;
            initialized.HomeInitialized = 1;
            initialized.ReturningHome = 0;
            initialized.Airborne = 0;
            initialized.UsesRunway = 1;
            initialized.TakeoffRolling = 0;
            initialized.LandingRolling = 0;
            initialized.AttackRunActive = 0;
            initialized.ReturnApproachInitialized = 0;
            initialized.RunwayTakeoffPosition = runway.TakeoffPosition;
            initialized.RunwayTakeoffCell = runway.TakeoffCell;
            initialized.RunwayLandingPosition = runway.LandingPosition;
            initialized.RunwayLandingCell = runway.LandingCell;
            initialized.AttackRunExitPosition = default;
            airState.ValueRW = initialized;
        }
    }

    private static bool ShouldInitializeRunwayHome(
        EntityManager em,
        Entity entity,
        UnitAirComponent airState,
        FixedString64Bytes sourceKey)
    {
        if (airState.UsesRunway != 0 ||
            airState.Airborne != 0 ||
            airState.ReturningHome != 0 ||
            airState.TakeoffRolling != 0 ||
            airState.LandingRolling != 0 ||
            airState.AttackRunActive != 0 ||
            airState.ReturnApproachInitialized != 0)
        {
            return false;
        }

        if (em.HasComponent<UnitTarget>(entity) ||
            em.HasComponent<UnitPathRequest>(entity) ||
            em.HasComponent<ManualMoveOrderTag>(entity))
        {
            return false;
        }

        return FixedWingRunwayUnitUtility.IsFixedWingRunwayUnit(sourceKey);
    }

    private static bool TryFindNearestRunway(
        DynamicBuffer<BuildingFactionRunwayReadModel> runways,
        byte factionId,
        float3 position,
        out BuildingFactionRunwayReadModel nearestRunway)
    {
        nearestRunway = default;
        float bestDistanceSq = float.MaxValue;
        bool found = false;
        for (int i = 0; i < runways.Length; i++)
        {
            BuildingFactionRunwayReadModel runway = runways[i];
            if (runway.FactionId != factionId)
                continue;

            float3 delta = runway.Center - position;
            delta.y = 0f;
            float distanceSq = math.lengthsq(delta);
            if (found && distanceSq >= bestDistanceSq)
                continue;

            nearestRunway = runway;
            bestDistanceSq = distanceSq;
            found = true;
        }

        return found;
    }

}

internal static class FixedWingRunwayUnitUtility
{
    public static bool IsFixedWingRunwayUnit(FixedString64Bytes sourceKey)
    {
        string key = sourceKey.ToString();
        if (string.IsNullOrEmpty(key))
            return false;

        if (key.IndexOf("Helicopter", StringComparison.OrdinalIgnoreCase) >= 0 ||
            key.IndexOf("Heli", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        return key.IndexOf("Jet", StringComparison.OrdinalIgnoreCase) >= 0 ||
               key.IndexOf("Drone", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
