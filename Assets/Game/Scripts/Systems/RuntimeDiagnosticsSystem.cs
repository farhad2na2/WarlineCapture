using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RuntimeDiagnosticsSystem : ISystem
    {
        private bool _hasCachedLegacyState;
        private RuntimeDiagnosticsStateComponent _lastLegacyState;

#if UNITY_EDITOR
        public readonly struct EditorBuildingVisualAllocationProbeSnapshot
        {
            public readonly long TotalBytes;
            public readonly int AllocationSamples;
            public readonly int CreateCalls;
            public readonly int PooledHits;
            public readonly int WrapperCreates;
            public readonly int PrefabInstantiates;
            public readonly long PrefabInstantiateBytes;
            public readonly int PrefabInstantiateAllocationSamples;

            private EditorBuildingVisualAllocationProbeSnapshot(EditorBuildingVisualAllocationProbeCounter counter)
            {
                TotalBytes = counter.TotalBytes;
                AllocationSamples = counter.AllocationSamples;
                CreateCalls = counter.CreateCalls;
                PooledHits = counter.PooledHits;
                WrapperCreates = counter.WrapperCreates;
                PrefabInstantiates = counter.PrefabInstantiates;
                PrefabInstantiateBytes = counter.PrefabInstantiateBytes;
                PrefabInstantiateAllocationSamples = counter.PrefabInstantiateAllocationSamples;
            }

            public static EditorBuildingVisualAllocationProbeSnapshot Create(EditorBuildingVisualAllocationProbeCounter counter)
            {
                return new EditorBuildingVisualAllocationProbeSnapshot(counter);
            }
        }

        public struct EditorBuildingVisualAllocationProbeCounter
        {
            public long TotalBytes;
            public int AllocationSamples;
            public int CreateCalls;
            public int PooledHits;
            public int WrapperCreates;
            public int PrefabInstantiates;
            public long PrefabInstantiateBytes;
            public int PrefabInstantiateAllocationSamples;

            public void Add(
                long allocatedBytes,
                bool pooled,
                bool wrapperCreated,
                bool prefabInstantiated,
                long prefabInstantiateBytes)
            {
                CreateCalls++;
                if (pooled)
                    PooledHits++;
                if (wrapperCreated)
                    WrapperCreates++;
                if (prefabInstantiated)
                    PrefabInstantiates++;

                if (allocatedBytes > 0)
                {
                    TotalBytes += allocatedBytes;
                    AllocationSamples++;
                }

                if (prefabInstantiateBytes > 0)
                {
                    PrefabInstantiateBytes += prefabInstantiateBytes;
                    PrefabInstantiateAllocationSamples++;
                }
            }
        }

        private static EditorBuildingVisualAllocationProbeCounter editorBuildingVisualAllocationProbe;
#endif

        public bool VerboseAILogs
        {
            get => ReadDiagnosticsState().VerboseAILogs != 0;
            set => WriteDiagnosticsState(state =>
            {
                state.VerboseAILogs = ToByte(value);
                return state;
            });
        }

        public bool ShouldLogAI => InitialUnitsRuntimeState.VerboseAILogs;

        public bool TransportBoardingDiagnostics
        {
            get => ReadDiagnosticsState().TransportBoardingDiagnostics != 0;
            set => WriteDiagnosticsState(state =>
            {
                state.TransportBoardingDiagnostics = ToByte(value);
                return state;
            });
        }

        public bool ShouldLogTransportBoarding => InitialUnitsRuntimeState.TransportBoardingDiagnostics;

        public bool BuildingRuntimeSliceDiagnostics
        {
            get => ReadDiagnosticsState().BuildingRuntimeSliceDiagnostics != 0;
            set => WriteDiagnosticsState(state =>
            {
                state.BuildingRuntimeSliceDiagnostics = ToByte(value);
                return state;
            });
        }

        public bool ShouldLogBuildingRuntimeSlices => InitialUnitsRuntimeState.BuildingRuntimeSliceDiagnostics;

        public void OnCreate(ref SystemState state)
        {
            // RequireForUpdate intentionally omitted: disabled diagnostics facade; accessors create/read backing state.
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

#if UNITY_EDITOR
        public static void ResetEditorBuildingVisualAllocationProbe()
        {
            editorBuildingVisualAllocationProbe = default;
        }

        public static void RecordEditorBuildingVisualAllocation(
            long allocatedBytes,
            bool pooled,
            bool wrapperCreated,
            bool prefabInstantiated,
            long prefabInstantiateBytes)
        {
            editorBuildingVisualAllocationProbe.Add(
                allocatedBytes,
                pooled,
                wrapperCreated,
                prefabInstantiated,
                prefabInstantiateBytes);
        }

        public static EditorBuildingVisualAllocationProbeSnapshot GetEditorBuildingVisualAllocationProbe()
        {
            return EditorBuildingVisualAllocationProbeSnapshot.Create(editorBuildingVisualAllocationProbe);
        }
#endif

        public RuntimeDiagnosticsStateComponent ReadDiagnosticsState()
        {
            RuntimeDiagnosticsStateComponent state = LegacyDiagnosticsState();
            if (TryGetDiagnosticsEntity(out EntityManager entityManager, out Entity entity))
            {
                if (!_hasCachedLegacyState || !DiagnosticsStateEquals(state, _lastLegacyState))
                {
                    entityManager.SetComponentData(entity, state);
                    CacheLegacyState(state);
                    return state;
                }

                return entityManager.GetComponentData<RuntimeDiagnosticsStateComponent>(entity);
            }

            return state;
        }

        private void WriteDiagnosticsState(System.Func<RuntimeDiagnosticsStateComponent, RuntimeDiagnosticsStateComponent> mutate)
        {
            RuntimeDiagnosticsStateComponent state = mutate(LegacyDiagnosticsState());
            ApplyLegacyDiagnosticsState(state);
            CacheLegacyState(state);
            if (TryGetDiagnosticsEntity(out EntityManager entityManager, out Entity entity))
                entityManager.SetComponentData(entity, state);
        }

        private static bool TryGetDiagnosticsEntity(out EntityManager entityManager, out Entity entity)
        {
            entityManager = default;
            entity = Entity.Null;
            if (!TryGetLiveEntityManager(out entityManager))
                return false;

            World world = entityManager.World;
            if (world == null || !world.IsCreated)
                return false;

            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
            if (query.CalculateEntityCount() > 0)
            {
                entity = query.GetSingletonEntity();
                return true;
            }

            entity = entityManager.CreateEntity(typeof(RuntimeDiagnosticsStateComponent));
            entityManager.SetName(entity, "RuntimeDiagnosticsState");
            entityManager.SetComponentData(entity, LegacyDiagnosticsState());
            return true;
        }

        private static bool TryGetLiveEntityManager(out EntityManager entityManager)
        {
            entityManager = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            entityManager = world.EntityManager;
            return true;
        }

        private void CacheLegacyState(RuntimeDiagnosticsStateComponent state)
        {
            _lastLegacyState = state;
            _hasCachedLegacyState = true;
        }

        private static RuntimeDiagnosticsStateComponent LegacyDiagnosticsState()
        {
            return new RuntimeDiagnosticsStateComponent
            {
                VerboseAILogs = ToByte(InitialUnitsRuntimeState.VerboseAILogs),
                TransportBoardingDiagnostics = ToByte(InitialUnitsRuntimeState.TransportBoardingDiagnostics),
                BuildingRuntimeSliceDiagnostics = ToByte(InitialUnitsRuntimeState.BuildingRuntimeSliceDiagnostics)
            };
        }

        private static void ApplyLegacyDiagnosticsState(RuntimeDiagnosticsStateComponent state)
        {
            InitialUnitsRuntimeState.VerboseAILogs = state.VerboseAILogs != 0;
            InitialUnitsRuntimeState.TransportBoardingDiagnostics = state.TransportBoardingDiagnostics != 0;
            InitialUnitsRuntimeState.BuildingRuntimeSliceDiagnostics = state.BuildingRuntimeSliceDiagnostics != 0;
        }

        private static byte ToByte(bool value)
        {
            return value ? (byte)1 : (byte)0;
        }

        private static bool DiagnosticsStateEquals(RuntimeDiagnosticsStateComponent left, RuntimeDiagnosticsStateComponent right)
        {
            return left.VerboseAILogs == right.VerboseAILogs &&
                left.TransportBoardingDiagnostics == right.TransportBoardingDiagnostics &&
                left.BuildingRuntimeSliceDiagnostics == right.BuildingRuntimeSliceDiagnostics;
        }
    }
}
