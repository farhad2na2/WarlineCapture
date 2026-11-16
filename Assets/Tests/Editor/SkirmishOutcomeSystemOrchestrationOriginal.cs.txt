using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct SkirmishOutcomeSystem : ISystem
    {
        private EntityQuery terminalSessions;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            terminalSessions = state.GetEntityQuery(
                ComponentType.ReadWrite<SkirmishExpandedSessionComponent>(),
                ComponentType.ReadOnly<SkirmishObjectiveStateComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            // Expanded sessions return from SkirmishRulesSystem before it can set
            // SkirmishPhase.Finished. Copy the sessions first. An idiomatic foreach
            // keeps structural changes illegal for the whole OnUpdate, so
            // AddComponent after that foreach still threw in the harness.
            using NativeArray<Entity> sessions = terminalSessions.ToEntityArray(Allocator.Temp);
            var pending = new NativeList<Entity>(Allocator.Temp);
            for (int i = 0; i < sessions.Length; i++)
            {
                Entity entity = sessions[i];
                SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                SkirmishObjectiveStateComponent objective = em.GetComponentData<SkirmishObjectiveStateComponent>(entity);
                if (session.IsLegacy == 0 && session.Phase == SkirmishSessionPhase.Playing &&
                    em.HasComponent<SkirmishMatchState>(entity) && em.HasBuffer<SkirmishTrackedUnit>(entity))
                {
                    var match = em.GetComponentData<SkirmishMatchState>(entity);
                    SkirmishRulesSystem.TrackLosses(em, entity, ref match, session.SessionId);
                    em.SetComponentData(entity, match);
                }
                if (session.IsLegacy != 0 || objective.Terminal == 0)
                    continue;
                if (em.HasComponent<SkirmishResultComponent>(entity) &&
                    em.GetComponentData<SkirmishResultComponent>(entity).Frozen != 0)
                {
                    if (session.Phase != SkirmishSessionPhase.Finished)
                    {
                        session.Phase = SkirmishSessionPhase.Finished;
                        em.SetComponentData(entity, session);
                    }

                    SkirmishExpandedSessionControlService.ProjectTerminalMatch(em, entity);
                    continue;
                }

                session.Phase = SkirmishSessionPhase.Finished;
                em.SetComponentData(entity, session);
                pending.Add(entity);
            }

            bool ended = false;
            for (int i = 0; i < pending.Length; i++)
            {
                Entity entity = pending[i];
                SkirmishObjectiveStateComponent objective = em.GetComponentData<SkirmishObjectiveStateComponent>(entity);
                SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                var result = new SkirmishResultComponent
                {
                    Outcome = objective.Outcome,
                    Reason = objective.Reason,
                    SetupHash = session.SetupHash,
                    SessionId = session.SessionId,
                    Frozen = 1,
                    SaveAcknowledged = 0
                };
                if (em.HasComponent<SkirmishResultComponent>(entity))
                    em.SetComponentData(entity, result);
                else
                    em.AddComponentData(entity, result);
                SkirmishExpandedSessionControlService.ProjectTerminalMatch(em, entity);
                ended = true;
            }

            pending.Dispose();
            if (!ended)
                return;
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
