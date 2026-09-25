using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(SkirmishOutcomeSystem))]
    [UpdateBefore(typeof(SkirmishSessionCleanupSystem))]
    public partial struct SkirmishCheckpointSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            // The first capture attaches a managed document component. Iterate a
            // snapshot so that structural change is legal on the first save too.
            using var sessions = state.GetEntityQuery(ComponentType.ReadOnly<SkirmishExpandedSessionComponent>())
                .ToEntityArray(Allocator.Temp);
            foreach (Entity entity in sessions)
            {
                if (em.GetComponentData<SkirmishExpandedSessionComponent>(entity).IsLegacy != 0)
                    continue;
                if (!em.HasComponent<SkirmishCheckpointRequestComponent>(entity))
                    continue;
                var request = em.GetComponentData<SkirmishCheckpointRequestComponent>(entity);
                if (request.Status != SkirmishCheckpointStatus.Requested)
                    continue;

                if (!SkirmishCheckpointService.TryCapture(em, entity, out SkirmishCheckpointDocument document))
                {
                    request.Status = SkirmishCheckpointStatus.Rejected;
                    em.SetComponentData(entity, request);
                    continue;
                }

                string path = em.HasComponent<SkirmishCheckpointDocumentRecord>(entity)
                    ? em.GetComponentObject<SkirmishCheckpointDocumentRecord>(entity).WritePath
                    : string.Empty;
                if (!string.IsNullOrEmpty(path) &&
                    !SkirmishCheckpointCodec.TryWriteAtomic(path, document, out _))
                {
                    request.Status = SkirmishCheckpointStatus.Rejected;
                    em.SetComponentData(entity, request);
                    continue;
                }

                if (em.HasComponent<SkirmishCheckpointDocumentRecord>(entity))
                    em.GetComponentObject<SkirmishCheckpointDocumentRecord>(entity).Document = document;
                else
                    em.AddComponentObject(entity, new SkirmishCheckpointDocumentRecord { Document = document, WritePath = path });

                request.Status = SkirmishCheckpointStatus.Written;
                request.SimulationTick = document.Header.SimulationTick;
                em.SetComponentData(entity, request);
            }
        }
    }
}
