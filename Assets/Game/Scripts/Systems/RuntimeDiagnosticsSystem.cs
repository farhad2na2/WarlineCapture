using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RuntimeDiagnosticsSystem : ISystem
    {
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

        public readonly struct EditorTransportBoardingAllocationProbeSnapshot
        {
            public readonly long UpdateBytes;
            public readonly int UpdateAllocationSamples;
            public readonly int UpdateCalls;
            public readonly int HandledUpdateCalls;
            public readonly long CommandBytes;
            public readonly int CommandAllocationSamples;
            public readonly int CommandCalls;
            public readonly int HandledCommandCalls;

            private EditorTransportBoardingAllocationProbeSnapshot(EditorTransportBoardingAllocationProbeCounter counter)
            {
                UpdateBytes = counter.UpdateBytes;
                UpdateAllocationSamples = counter.UpdateAllocationSamples;
                UpdateCalls = counter.UpdateCalls;
                HandledUpdateCalls = counter.HandledUpdateCalls;
                CommandBytes = counter.CommandBytes;
                CommandAllocationSamples = counter.CommandAllocationSamples;
                CommandCalls = counter.CommandCalls;
                HandledCommandCalls = counter.HandledCommandCalls;
            }

            public static EditorTransportBoardingAllocationProbeSnapshot Create(EditorTransportBoardingAllocationProbeCounter counter)
            {
                return new EditorTransportBoardingAllocationProbeSnapshot(counter);
            }
        }

        public struct EditorTransportBoardingAllocationProbeCounter
        {
            public long UpdateBytes;
            public int UpdateAllocationSamples;
            public int UpdateCalls;
            public int HandledUpdateCalls;
            public long CommandBytes;
            public int CommandAllocationSamples;
            public int CommandCalls;
            public int HandledCommandCalls;

            public void AddUpdate(long allocatedBytes, bool handled)
            {
                UpdateCalls++;
                if (handled)
                    HandledUpdateCalls++;
                if (allocatedBytes <= 0)
                    return;

                UpdateBytes += allocatedBytes;
                UpdateAllocationSamples++;
            }

            public void AddCommand(long allocatedBytes, bool handled)
            {
                CommandCalls++;
                if (handled)
                    HandledCommandCalls++;
                if (allocatedBytes <= 0)
                    return;

                CommandBytes += allocatedBytes;
                CommandAllocationSamples++;
            }
        }

        public enum EditorGameplayRuntimeAllocationProbePhase
        {
            RuntimeCity = 0,
            RuntimeGridBlockers = 1,
            RuntimeDecorations = 2,
            RoadBuild = 3,
            BuildingPlacement = 4,
            Selection = 5,
            DayNight = 6,
            CitizenPopulation = 7,
            MainMenu = 8,
            LoadingGate = 9,
            EndUpdate = 10
        }

        public readonly struct EditorGameplayRuntimeAllocationProbeSnapshot
        {
            public readonly EditorGameplayRuntimeAllocationProbeCounter RuntimeCity;
            public readonly EditorGameplayRuntimeAllocationProbeCounter RuntimeGridBlockers;
            public readonly EditorGameplayRuntimeAllocationProbeCounter RuntimeDecorations;
            public readonly EditorGameplayRuntimeAllocationProbeCounter RoadBuild;
            public readonly EditorGameplayRuntimeAllocationProbeCounter BuildingPlacement;
            public readonly EditorGameplayRuntimeAllocationProbeCounter Selection;
            public readonly EditorGameplayRuntimeAllocationProbeCounter DayNight;
            public readonly EditorGameplayRuntimeAllocationProbeCounter CitizenPopulation;
            public readonly EditorGameplayRuntimeAllocationProbeCounter MainMenu;
            public readonly EditorGameplayRuntimeAllocationProbeCounter LoadingGate;
            public readonly EditorGameplayRuntimeAllocationProbeCounter EndUpdate;

            private EditorGameplayRuntimeAllocationProbeSnapshot(
                EditorGameplayRuntimeAllocationProbeCounter runtimeCity,
                EditorGameplayRuntimeAllocationProbeCounter runtimeGridBlockers,
                EditorGameplayRuntimeAllocationProbeCounter runtimeDecorations,
                EditorGameplayRuntimeAllocationProbeCounter roadBuild,
                EditorGameplayRuntimeAllocationProbeCounter buildingPlacement,
                EditorGameplayRuntimeAllocationProbeCounter selection,
                EditorGameplayRuntimeAllocationProbeCounter dayNight,
                EditorGameplayRuntimeAllocationProbeCounter citizenPopulation,
                EditorGameplayRuntimeAllocationProbeCounter mainMenu,
                EditorGameplayRuntimeAllocationProbeCounter loadingGate,
                EditorGameplayRuntimeAllocationProbeCounter endUpdate)
            {
                RuntimeCity = runtimeCity;
                RuntimeGridBlockers = runtimeGridBlockers;
                RuntimeDecorations = runtimeDecorations;
                RoadBuild = roadBuild;
                BuildingPlacement = buildingPlacement;
                Selection = selection;
                DayNight = dayNight;
                CitizenPopulation = citizenPopulation;
                MainMenu = mainMenu;
                LoadingGate = loadingGate;
                EndUpdate = endUpdate;
            }

            public static EditorGameplayRuntimeAllocationProbeSnapshot Create(
                EditorGameplayRuntimeAllocationProbeCounter runtimeCity,
                EditorGameplayRuntimeAllocationProbeCounter runtimeGridBlockers,
                EditorGameplayRuntimeAllocationProbeCounter runtimeDecorations,
                EditorGameplayRuntimeAllocationProbeCounter roadBuild,
                EditorGameplayRuntimeAllocationProbeCounter buildingPlacement,
                EditorGameplayRuntimeAllocationProbeCounter selection,
                EditorGameplayRuntimeAllocationProbeCounter dayNight,
                EditorGameplayRuntimeAllocationProbeCounter citizenPopulation,
                EditorGameplayRuntimeAllocationProbeCounter mainMenu,
                EditorGameplayRuntimeAllocationProbeCounter loadingGate,
                EditorGameplayRuntimeAllocationProbeCounter endUpdate)
            {
                return new EditorGameplayRuntimeAllocationProbeSnapshot(
                    runtimeCity,
                    runtimeGridBlockers,
                    runtimeDecorations,
                    roadBuild,
                    buildingPlacement,
                    selection,
                    dayNight,
                    citizenPopulation,
                    mainMenu,
                    loadingGate,
                    endUpdate);
            }
        }

        public struct EditorGameplayRuntimeAllocationProbeCounter
        {
            public long Bytes;
            public int AllocationSamples;
            public int UpdateSamples;

            public void Add(long allocatedBytes)
            {
                UpdateSamples++;
                if (allocatedBytes <= 0)
                    return;

                Bytes += allocatedBytes;
                AllocationSamples++;
            }
        }

        public readonly struct EditorGameplayRuntimeAllocationProbeScope : System.IDisposable
        {
            private readonly EditorGameplayRuntimeAllocationProbePhase phase;
            private readonly long startBytes;

            public EditorGameplayRuntimeAllocationProbeScope(EditorGameplayRuntimeAllocationProbePhase phase)
            {
                this.phase = phase;
                startBytes = System.GC.GetAllocatedBytesForCurrentThread();
            }

            public void Dispose()
            {
                RecordEditorGameplayRuntimeAllocation(
                    phase,
                    System.GC.GetAllocatedBytesForCurrentThread() - startBytes);
            }
        }

        private static EditorBuildingVisualAllocationProbeCounter editorBuildingVisualAllocationProbe;
        private static EditorProductionTransportAllocationProbeCounter editorProductionTransportAllocationProbe;
        private static EditorTransportBoardingAllocationProbeCounter editorTransportBoardingAllocationProbe;
        private static EditorGameplayRuntimeAllocationProbeCounter editorGameplayRuntimeCityAllocationProbe;
        private static EditorGameplayRuntimeAllocationProbeCounter editorGameplayRuntimeGridBlockersAllocationProbe;
        private static EditorGameplayRuntimeAllocationProbeCounter editorGameplayRuntimeDecorationsAllocationProbe;
        private static EditorGameplayRuntimeAllocationProbeCounter editorGameplayRoadBuildAllocationProbe;
        private static EditorGameplayRuntimeAllocationProbeCounter editorGameplayBuildingPlacementAllocationProbe;
        private static EditorGameplayRuntimeAllocationProbeCounter editorGameplaySelectionAllocationProbe;
        private static EditorGameplayRuntimeAllocationProbeCounter editorGameplayDayNightAllocationProbe;
        private static EditorGameplayRuntimeAllocationProbeCounter editorGameplayCitizenPopulationAllocationProbe;
        private static EditorGameplayRuntimeAllocationProbeCounter editorGameplayMainMenuAllocationProbe;
        private static EditorGameplayRuntimeAllocationProbeCounter editorGameplayLoadingGateAllocationProbe;
        private static EditorGameplayRuntimeAllocationProbeCounter editorGameplayEndUpdateAllocationProbe;
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

        public bool ShouldLogAI => ReadDiagnosticsState().VerboseAILogs != 0;

        public bool TransportBoardingDiagnostics
        {
            get => ReadDiagnosticsState().TransportBoardingDiagnostics != 0;
            set => WriteDiagnosticsState(state =>
            {
                state.TransportBoardingDiagnostics = ToByte(value);
                return state;
            });
        }

        public bool ShouldLogTransportBoarding => ReadDiagnosticsState().TransportBoardingDiagnostics != 0;

        public bool BuildingRuntimeSliceDiagnostics
        {
            get => ReadDiagnosticsState().BuildingRuntimeSliceDiagnostics != 0;
            set => WriteDiagnosticsState(state =>
            {
                state.BuildingRuntimeSliceDiagnostics = ToByte(value);
                return state;
            });
        }

        public bool ShouldLogBuildingRuntimeSlices => ReadDiagnosticsState().BuildingRuntimeSliceDiagnostics != 0;

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

        public static void ResetEditorTransportBoardingAllocationProbe()
        {
            editorTransportBoardingAllocationProbe = default;
        }

        public static void ResetEditorGameplayRuntimeAllocationProbe()
        {
            editorGameplayRuntimeCityAllocationProbe = default;
            editorGameplayRuntimeGridBlockersAllocationProbe = default;
            editorGameplayRuntimeDecorationsAllocationProbe = default;
            editorGameplayRoadBuildAllocationProbe = default;
            editorGameplayBuildingPlacementAllocationProbe = default;
            editorGameplaySelectionAllocationProbe = default;
            editorGameplayDayNightAllocationProbe = default;
            editorGameplayCitizenPopulationAllocationProbe = default;
            editorGameplayMainMenuAllocationProbe = default;
            editorGameplayLoadingGateAllocationProbe = default;
            editorGameplayEndUpdateAllocationProbe = default;
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

        public static void RecordEditorTransportBoardingUpdateAllocation(long allocatedBytes, bool handled)
        {
            editorTransportBoardingAllocationProbe.AddUpdate(allocatedBytes, handled);
        }

        public static void RecordEditorTransportBoardingCommandAllocation(long allocatedBytes, bool handled)
        {
            editorTransportBoardingAllocationProbe.AddCommand(allocatedBytes, handled);
        }

        public static void RecordEditorGameplayRuntimeAllocation(
            EditorGameplayRuntimeAllocationProbePhase phase,
            long allocatedBytes)
        {
            switch (phase)
            {
                case EditorGameplayRuntimeAllocationProbePhase.RuntimeCity:
                    editorGameplayRuntimeCityAllocationProbe.Add(allocatedBytes);
                    break;
                case EditorGameplayRuntimeAllocationProbePhase.RuntimeGridBlockers:
                    editorGameplayRuntimeGridBlockersAllocationProbe.Add(allocatedBytes);
                    break;
                case EditorGameplayRuntimeAllocationProbePhase.RuntimeDecorations:
                    editorGameplayRuntimeDecorationsAllocationProbe.Add(allocatedBytes);
                    break;
                case EditorGameplayRuntimeAllocationProbePhase.RoadBuild:
                    editorGameplayRoadBuildAllocationProbe.Add(allocatedBytes);
                    break;
                case EditorGameplayRuntimeAllocationProbePhase.BuildingPlacement:
                    editorGameplayBuildingPlacementAllocationProbe.Add(allocatedBytes);
                    break;
                case EditorGameplayRuntimeAllocationProbePhase.Selection:
                    editorGameplaySelectionAllocationProbe.Add(allocatedBytes);
                    break;
                case EditorGameplayRuntimeAllocationProbePhase.DayNight:
                    editorGameplayDayNightAllocationProbe.Add(allocatedBytes);
                    break;
                case EditorGameplayRuntimeAllocationProbePhase.CitizenPopulation:
                    editorGameplayCitizenPopulationAllocationProbe.Add(allocatedBytes);
                    break;
                case EditorGameplayRuntimeAllocationProbePhase.MainMenu:
                    editorGameplayMainMenuAllocationProbe.Add(allocatedBytes);
                    break;
                case EditorGameplayRuntimeAllocationProbePhase.LoadingGate:
                    editorGameplayLoadingGateAllocationProbe.Add(allocatedBytes);
                    break;
                case EditorGameplayRuntimeAllocationProbePhase.EndUpdate:
                    editorGameplayEndUpdateAllocationProbe.Add(allocatedBytes);
                    break;
            }
        }

        public static EditorBuildingVisualAllocationProbeSnapshot GetEditorBuildingVisualAllocationProbe()
        {
            return EditorBuildingVisualAllocationProbeSnapshot.Create(editorBuildingVisualAllocationProbe);
        }

        public static EditorProductionTransportAllocationProbeSnapshot GetEditorProductionTransportAllocationProbe()
        {
            return EditorProductionTransportAllocationProbeSnapshot.Create(editorProductionTransportAllocationProbe);
        }

        public static EditorTransportBoardingAllocationProbeSnapshot GetEditorTransportBoardingAllocationProbe()
        {
            return EditorTransportBoardingAllocationProbeSnapshot.Create(editorTransportBoardingAllocationProbe);
        }

        public static EditorGameplayRuntimeAllocationProbeSnapshot GetEditorGameplayRuntimeAllocationProbe()
        {
            return EditorGameplayRuntimeAllocationProbeSnapshot.Create(
                editorGameplayRuntimeCityAllocationProbe,
                editorGameplayRuntimeGridBlockersAllocationProbe,
                editorGameplayRuntimeDecorationsAllocationProbe,
                editorGameplayRoadBuildAllocationProbe,
                editorGameplayBuildingPlacementAllocationProbe,
                editorGameplaySelectionAllocationProbe,
                editorGameplayDayNightAllocationProbe,
                editorGameplayCitizenPopulationAllocationProbe,
                editorGameplayMainMenuAllocationProbe,
                editorGameplayLoadingGateAllocationProbe,
                editorGameplayEndUpdateAllocationProbe);
        }
#endif

        public RuntimeDiagnosticsStateComponent ReadDiagnosticsState()
        {
            return TryGetDiagnosticsEntity(out EntityManager entityManager, out Entity entity)
                ? entityManager.GetComponentData<RuntimeDiagnosticsStateComponent>(entity)
                : default;
        }

        private void WriteDiagnosticsState(System.Func<RuntimeDiagnosticsStateComponent, RuntimeDiagnosticsStateComponent> mutate)
        {
            if (!TryGetDiagnosticsEntity(out EntityManager entityManager, out Entity entity))
                return;
            RuntimeDiagnosticsStateComponent state = entityManager.GetComponentData<RuntimeDiagnosticsStateComponent>(entity);
            entityManager.SetComponentData(entity, mutate(state));
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

        private static byte ToByte(bool value)
        {
            return value ? (byte)1 : (byte)0;
        }

    }
}
