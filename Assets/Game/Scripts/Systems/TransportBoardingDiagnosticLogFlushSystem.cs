using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitTransportBoardingSystem))]
    public partial struct TransportBoardingDiagnosticLogFlushSystem : ISystem
    {
        private EntityQuery _logQueueQuery;

        public void OnCreate(ref SystemState state)
        {
            _logQueueQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<TransportBoardingDiagnosticLogQueueComponent>(),
                ComponentType.ReadWrite<TransportBoardingDiagnosticLogComponent>());
            state.RequireForUpdate(_logQueueQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            bool shouldLog = ShouldFlushDiagnostics(ref state);
            foreach (DynamicBuffer<TransportBoardingDiagnosticLogComponent> logs in SystemAPI
                         .Query<DynamicBuffer<TransportBoardingDiagnosticLogComponent>>()
                         .WithAll<TransportBoardingDiagnosticLogQueueComponent>())
            {
                if (shouldLog)
                {
                    for (int logIndex = 0; logIndex < logs.Length; logIndex++)
                        Debug.Log(logs[logIndex].Message.ToString());
                }

                logs.Clear();
            }
        }

        private bool ShouldFlushDiagnostics(ref SystemState state)
        {
            if (Application.isBatchMode)
                return true;

            return SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
                SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().TransportBoardingDiagnostics != 0;
        }
    }
}
