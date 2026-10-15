using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    // Import only together with the reviewed native bootstrap/lifecycle/recovery
    // package. Unauthored non-BA Playing sessions fail closed; they never inherit
    // BA result rules or silently run without an objective owner.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkirmishObjectiveFactProjectionSystem))]
    [UpdateAfter(typeof(UnitDeathSystem))]
    [UpdateBefore(typeof(SkirmishOutcomeSystem))]
    public partial struct SkirmishNativeObjectiveTickSystem:ISystem
    {
        private EntityQuery sessions;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            sessions=state.GetEntityQuery(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishObjectiveStateComponent));
        }
        public void OnUpdate(ref SystemState state)
        {
            var em=state.EntityManager;
            using var entities=sessions.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)
            {
                var session=em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                if(session.IsLegacy!=0||session.ObjectiveKind==SkirmishObjectiveKind.BaseAssault||session.Phase!=SkirmishSessionPhase.Playing)continue;
                if(!SkirmishNativeObjectiveTick.TryTick(em,entity,out string error))
                    Debug.LogError("[SkirmishNativeObjectiveTick] result=Failed reason="+error);
            }
        }
    }
}
