using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct SkirmishOutcomeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            foreach ((RefRW<SkirmishExpandedSessionComponent> sessionRef,
                      RefRO<SkirmishObjectiveStateComponent> objective,
                      Entity entity) in
                     SystemAPI.Query<RefRW<SkirmishExpandedSessionComponent>,
                         RefRO<SkirmishObjectiveStateComponent>>().WithEntityAccess())
            {
                if (sessionRef.ValueRO.IsLegacy != 0 || objective.ValueRO.Terminal == 0)
                    continue;
                if (em.HasComponent<SkirmishResultComponent>(entity) &&
                    em.GetComponentData<SkirmishResultComponent>(entity).Frozen != 0)
                    continue;

                var result = new SkirmishResultComponent
                {
                    Outcome = objective.ValueRO.Outcome,
                    Reason = objective.ValueRO.Reason,
                    SetupHash = sessionRef.ValueRO.SetupHash,
                    SessionId = sessionRef.ValueRO.SessionId,
                    Frozen = 1,
                    SaveAcknowledged = 0
                };
                if (em.HasComponent<SkirmishResultComponent>(entity))
                    em.SetComponentData(entity, result);
                else
                    em.AddComponentData(entity, result);

                sessionRef.ValueRW.Phase = SkirmishSessionPhase.Finished;
                if (SystemAPI.TryGetSingleton<RuntimeGameplayStateComponent>(out var gameplay))
                {
                    gameplay.SimulationActive = 0;
                    gameplay.PlayRequested = 0;
                    Entity gameplayEntity = SystemAPI.GetSingletonEntity<RuntimeGameplayStateComponent>();
                    em.SetComponentData(gameplayEntity, gameplay);
                }
            }
        }
    }
}
