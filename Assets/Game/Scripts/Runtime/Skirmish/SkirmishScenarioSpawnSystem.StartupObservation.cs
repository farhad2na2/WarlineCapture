#if UNITY_EDITOR
using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct SkirmishScenarioSpawnSystem
    {
        // Editor-only passive observer. The normal player path has no observer.
        // Buffers are borrowed for the callback duration and must never be mutated.
        public struct StartupAllocatorObservation
        {
            public EntityManager EntityManager;
            public Entity Actor, GridEntity;
            public GridConfig Grid;
            public int Seed;
            public uint RandomState;
            public int2 RequestedCell, Footprint;
            public float3 RequestedWorld;
            public bool Air, Typed;
            public NativeBitArray Blocked, Occupied, Reserved;
        }
        public static Action<StartupAllocatorObservation> ObserveStartupAllocator;
        public static Action<EntityManager, Entity, bool> ObserveStartupPlacementResult;
    }
}
#endif
