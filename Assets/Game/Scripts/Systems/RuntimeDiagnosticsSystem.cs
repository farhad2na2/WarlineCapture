using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RuntimeDiagnosticsSystem : ISystem
    {
        private bool _hasCachedLegacyState;
        private RuntimeDiagnosticsStateComponent _lastLegacyState;

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
