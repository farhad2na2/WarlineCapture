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

        public readonly struct EditorProductionTransportAllocationProbeSnapshot
        {
            public readonly long UpdateBytes;
            public readonly int UpdateAllocationSamples;
            public readonly int UpdateCalls;
            public readonly int ActiveUpdateCalls;
            public readonly long AcquireBytes;
            public readonly int AcquireAllocationSamples;
            public readonly int AcquireCalls;
            public readonly int PooledAcquireHits;
            public readonly int CreatedAcquireInstances;
            public readonly long CreateBytes;
            public readonly int CreateAllocationSamples;
            public readonly int CreateCalls;
            public readonly long DropVisualAcquireBytes;
            public readonly int DropVisualAcquireAllocationSamples;
            public readonly int DropVisualAcquireCalls;
            public readonly int PooledDropVisualAcquireHits;
            public readonly int CreatedDropVisualInstances;
            public readonly long DropVisualCreateBytes;
            public readonly int DropVisualCreateAllocationSamples;
            public readonly int DropVisualCreateCalls;

            private EditorProductionTransportAllocationProbeSnapshot(EditorProductionTransportAllocationProbeCounter counter)
            {
                UpdateBytes = counter.UpdateBytes;
                UpdateAllocationSamples = counter.UpdateAllocationSamples;
                UpdateCalls = counter.UpdateCalls;
                ActiveUpdateCalls = counter.ActiveUpdateCalls;
                AcquireBytes = counter.AcquireBytes;
                AcquireAllocationSamples = counter.AcquireAllocationSamples;
                AcquireCalls = counter.AcquireCalls;
                PooledAcquireHits = counter.PooledAcquireHits;
                CreatedAcquireInstances = counter.CreatedAcquireInstances;
                CreateBytes = counter.CreateBytes;
                CreateAllocationSamples = counter.CreateAllocationSamples;
                CreateCalls = counter.CreateCalls;
                DropVisualAcquireBytes = counter.DropVisualAcquireBytes;
                DropVisualAcquireAllocationSamples = counter.DropVisualAcquireAllocationSamples;
                DropVisualAcquireCalls = counter.DropVisualAcquireCalls;
                PooledDropVisualAcquireHits = counter.PooledDropVisualAcquireHits;
                CreatedDropVisualInstances = counter.CreatedDropVisualInstances;
                DropVisualCreateBytes = counter.DropVisualCreateBytes;
                DropVisualCreateAllocationSamples = counter.DropVisualCreateAllocationSamples;
                DropVisualCreateCalls = counter.DropVisualCreateCalls;
            }

            public static EditorProductionTransportAllocationProbeSnapshot Create(EditorProductionTransportAllocationProbeCounter counter)
            {
                return new EditorProductionTransportAllocationProbeSnapshot(counter);
            }
        }

        public struct EditorProductionTransportAllocationProbeCounter
        {
            public long UpdateBytes;
            public int UpdateAllocationSamples;
            public int UpdateCalls;
            public int ActiveUpdateCalls;
            public long AcquireBytes;
            public int AcquireAllocationSamples;
            public int AcquireCalls;
            public int PooledAcquireHits;
            public int CreatedAcquireInstances;
            public long CreateBytes;
            public int CreateAllocationSamples;
            public int CreateCalls;
            public long DropVisualAcquireBytes;
            public int DropVisualAcquireAllocationSamples;
            public int DropVisualAcquireCalls;
            public int PooledDropVisualAcquireHits;
            public int CreatedDropVisualInstances;
            public long DropVisualCreateBytes;
            public int DropVisualCreateAllocationSamples;
            public int DropVisualCreateCalls;

            public void AddUpdate(long allocatedBytes, bool hasActiveTransport)
            {
                UpdateCalls++;
                if (hasActiveTransport)
                    ActiveUpdateCalls++;
                if (allocatedBytes <= 0)
                    return;

                UpdateBytes += allocatedBytes;
                UpdateAllocationSamples++;
            }

            public void AddAcquire(long allocatedBytes, bool pooled, bool created)
            {
                AcquireCalls++;
                if (pooled)
                    PooledAcquireHits++;
                if (created)
                    CreatedAcquireInstances++;
                if (allocatedBytes <= 0)
                    return;

                AcquireBytes += allocatedBytes;
                AcquireAllocationSamples++;
            }

            public void AddCreate(long allocatedBytes)
            {
                CreateCalls++;
                if (allocatedBytes <= 0)
                    return;

                CreateBytes += allocatedBytes;
                CreateAllocationSamples++;
            }

            public void AddDropVisualAcquire(long allocatedBytes, bool pooled, bool created)
            {
                DropVisualAcquireCalls++;
                if (pooled)
                    PooledDropVisualAcquireHits++;
                if (created)
                    CreatedDropVisualInstances++;
                if (allocatedBytes <= 0)
                    return;

                DropVisualAcquireBytes += allocatedBytes;
                DropVisualAcquireAllocationSamples++;
            }

            public void AddDropVisualCreate(long allocatedBytes)
            {
                DropVisualCreateCalls++;
                if (allocatedBytes <= 0)
                    return;

                DropVisualCreateBytes += allocatedBytes;
                DropVisualCreateAllocationSamples++;
            }
        }

        private static EditorBuildingVisualAllocationProbeCounter editorBuildingVisualAllocationProbe;
        private static EditorProductionTransportAllocationProbeCounter editorProductionTransportAllocationProbe;
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

        public static void ResetEditorProductionTransportAllocationProbe()
        {
            editorProductionTransportAllocationProbe = default;
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

        public static void RecordEditorProductionTransportUpdateAllocation(long allocatedBytes, bool hasActiveTransport)
        {
            editorProductionTransportAllocationProbe.AddUpdate(allocatedBytes, hasActiveTransport);
        }

        public static void RecordEditorProductionTransportAcquireAllocation(long allocatedBytes, bool pooled, bool created)
        {
            editorProductionTransportAllocationProbe.AddAcquire(allocatedBytes, pooled, created);
        }

        public static void RecordEditorProductionTransportCreateAllocation(long allocatedBytes)
        {
            editorProductionTransportAllocationProbe.AddCreate(allocatedBytes);
        }

        public static void RecordEditorProductionTransportDropVisualAcquireAllocation(long allocatedBytes, bool pooled, bool created)
        {
            editorProductionTransportAllocationProbe.AddDropVisualAcquire(allocatedBytes, pooled, created);
        }

        public static void RecordEditorProductionTransportDropVisualCreateAllocation(long allocatedBytes)
        {
            editorProductionTransportAllocationProbe.AddDropVisualCreate(allocatedBytes);
        }

        public static EditorBuildingVisualAllocationProbeSnapshot GetEditorBuildingVisualAllocationProbe()
        {
            return EditorBuildingVisualAllocationProbeSnapshot.Create(editorBuildingVisualAllocationProbe);
        }

        public static EditorProductionTransportAllocationProbeSnapshot GetEditorProductionTransportAllocationProbe()
        {
            return EditorProductionTransportAllocationProbeSnapshot.Create(editorProductionTransportAllocationProbe);
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
