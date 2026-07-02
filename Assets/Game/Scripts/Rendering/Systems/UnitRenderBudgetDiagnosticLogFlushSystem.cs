using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Rendering
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitRenderBudgetSystem))]
    public partial struct UnitRenderBudgetDiagnosticLogFlushSystem : ISystem
    {
        private EntityQuery _logQueueQuery;

        public void OnCreate(ref SystemState state)
        {
            _logQueueQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UnitRenderBudgetDiagnosticLogQueueComponent>(),
                ComponentType.ReadWrite<UnitRenderBudgetDiagnosticLogComponent>());
            state.RequireForUpdate(_logQueueQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (DynamicBuffer<UnitRenderBudgetDiagnosticLogComponent> logs in SystemAPI
                         .Query<DynamicBuffer<UnitRenderBudgetDiagnosticLogComponent>>()
                         .WithAll<UnitRenderBudgetDiagnosticLogQueueComponent>())
            {
                for (int logIndex = 0; logIndex < logs.Length; logIndex++)
                {
                    UnitRenderBudgetDiagnosticLogComponent log = logs[logIndex];
                    if (log.Severity == UnitRenderBudgetDiagnosticLogComponent.WarningSeverity)
                        Debug.LogWarning(log.Message.ToString());
                    else
                        Debug.Log(log.Message.ToString());
                }

                logs.Clear();
            }
        }
    }
}
