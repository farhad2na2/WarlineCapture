using Unity.Entities;
using Unity.Mathematics;

internal sealed class CitizenMovementCommandSystem
{
    public void IssueCitizenMoveCommand(CitizenPopulationEcsProjectionSystem ecsProjection, Entity entity, int2 goal)
    {
        if (ecsProjection.EntityManager.HasComponent<EngageTarget>(entity))
            ecsProjection.EntityManager.RemoveComponent<EngageTarget>(entity);
        if (ecsProjection.EntityManager.HasComponent<UnitPathFollow>(entity))
            ecsProjection.EntityManager.RemoveComponent<UnitPathFollow>(entity);
        if (ecsProjection.EntityManager.HasComponent<UnitPathRange>(entity))
            ecsProjection.EntityManager.RemoveComponent<UnitPathRange>(entity);
        if (ecsProjection.EntityManager.HasComponent<AutoWanderMoveTag>(entity))
            ecsProjection.EntityManager.RemoveComponent<AutoWanderMoveTag>(entity);

        if (ecsProjection.EntityManager.HasComponent<UnitTarget>(entity))
            ecsProjection.EntityManager.SetComponentData(entity, new UnitTarget { Cell = goal });
        else
            ecsProjection.EntityManager.AddComponentData(entity, new UnitTarget { Cell = goal });

        if (!ecsProjection.EntityManager.HasComponent<UnitAirMovement>(entity))
        {
            if (ecsProjection.EntityManager.HasComponent<UnitPathRequest>(entity))
                ecsProjection.EntityManager.SetComponentData(entity, new UnitPathRequest { Goal = goal });
            else
                ecsProjection.EntityManager.AddComponentData(entity, new UnitPathRequest { Goal = goal });
        }
        else if (ecsProjection.EntityManager.HasComponent<UnitPathRequest>(entity))
        {
            ecsProjection.EntityManager.RemoveComponent<UnitPathRequest>(entity);
        }

        if (!ecsProjection.EntityManager.HasComponent<ManualMoveOrderTag>(entity))
            ecsProjection.EntityManager.AddComponent<ManualMoveOrderTag>(entity);
    }
}
