using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(SkirmishOutcomeSystem))]
    [UpdateBefore(typeof(SkirmishCheckpointSystem))]
    public partial struct SkirmishResultSettlementSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            foreach ((RefRO<SkirmishExpandedSessionComponent> session,
                      Entity entity) in
                     SystemAPI.Query<RefRO<SkirmishExpandedSessionComponent>>().WithEntityAccess())
            {
                if (session.ValueRO.IsLegacy != 0)
                    continue;
                if (!em.HasComponent<SkirmishResultComponent>(entity))
                    continue;
                var result = em.GetComponentData<SkirmishResultComponent>(entity);
                if (result.Frozen == 0 || result.SaveAcknowledged != 0)
                    continue;
                SkirmishResultSettlementService.TrySettleSession(
                    em, entity, SkirmishResultSettlementService.Shared, out _, out _);
            }
        }
    }
}
