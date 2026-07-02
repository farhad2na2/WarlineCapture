using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AICombatOrderSystem))]
    public partial struct AIDiagnosticLogFlushSystem : ISystem
    {
        private EntityQuery _logQueueQuery;

        public void OnCreate(ref SystemState state)
        {
            _logQueueQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
                ComponentType.ReadWrite<AIDiagnosticLogComponent>());
            state.RequireForUpdate(_logQueueQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            bool shouldLog = ShouldFlushDiagnostics(ref state);
            foreach (DynamicBuffer<AIDiagnosticLogComponent> logs in SystemAPI
                         .Query<DynamicBuffer<AIDiagnosticLogComponent>>()
                         .WithAll<AIDiagnosticLogQueueComponent>())
            {
                if (shouldLog)
                {
                    for (int logIndex = 0; logIndex < logs.Length; logIndex++)
                    {
                        AIDiagnosticLogComponent log = logs[logIndex];
                        if (log.Severity == AIDiagnosticLogComponent.WarningSeverity)
                            Debug.LogWarning(log.Message.ToString());
                        else
                            Debug.Log(log.Message.ToString());
                    }
                }

                logs.Clear();
            }
        }

        private bool ShouldFlushDiagnostics(ref SystemState state)
        {
            return InitialUnitsRuntimeState.VerboseAILogs ||
                SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
                SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
        }
    }
}
