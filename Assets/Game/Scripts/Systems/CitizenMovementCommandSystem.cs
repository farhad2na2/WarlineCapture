using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal sealed class CitizenMovementCommandSystem
{
    public void IssueCitizenMoveCommand(CitizenPopulationEcsProjectionSystem ecsProjection, Entity entity, int2 goal)
    {
        EntityManager em = ecsProjection.EntityManager;
        EntityCommandBuffer ecb = new(Allocator.Temp);

        if (em.HasComponent<EngageTarget>(entity))
            ecb.RemoveComponent<EngageTarget>(entity);
        if (em.HasComponent<UnitPathFollow>(entity))
            ecb.RemoveComponent<UnitPathFollow>(entity);
        if (em.HasComponent<UnitPathRange>(entity))
            ecb.RemoveComponent<UnitPathRange>(entity);
        if (em.HasComponent<AutoWanderMoveTag>(entity))
            ecb.RemoveComponent<AutoWanderMoveTag>(entity);

        if (em.HasComponent<UnitTarget>(entity))
            ecb.SetComponent(entity, new UnitTarget { Cell = goal });
        else
            ecb.AddComponent(entity, new UnitTarget { Cell = goal });

        if (!em.HasComponent<UnitAirMovement>(entity))
        {
            if (em.HasComponent<UnitPathRequest>(entity))
                ecb.SetComponent(entity, new UnitPathRequest { Goal = goal });
            else
                ecb.AddComponent(entity, new UnitPathRequest { Goal = goal });
        }
        else if (em.HasComponent<UnitPathRequest>(entity))
        {
            ecb.RemoveComponent<UnitPathRequest>(entity);
        }

        if (!em.HasComponent<ManualMoveOrderTag>(entity))
            ecb.AddComponent<ManualMoveOrderTag>(entity);

        ecb.Playback(em);
        ecb.Dispose();
    }
}
