using Unity.Entities;

public sealed class RuntimeDiagnosticsSystem
{
    private World _cachedWorld;
    private Entity _diagnosticsEntity;
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

    private bool TryGetDiagnosticsEntity(out EntityManager entityManager, out Entity entity)
    {
        entityManager = default;
        entity = Entity.Null;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        if (_cachedWorld == world &&
            _diagnosticsEntity != Entity.Null &&
            entityManager.Exists(_diagnosticsEntity) &&
            entityManager.HasComponent<RuntimeDiagnosticsStateComponent>(_diagnosticsEntity))
        {
            entity = _diagnosticsEntity;
            return true;
        }

        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
        if (query.CalculateEntityCount() > 0)
        {
            entity = query.GetSingletonEntity();
            CacheDiagnosticsEntity(world, entity);
            return true;
        }

        entity = entityManager.CreateEntity(typeof(RuntimeDiagnosticsStateComponent));
        entityManager.SetName(entity, "RuntimeDiagnosticsState");
        entityManager.SetComponentData(entity, LegacyDiagnosticsState());
        CacheDiagnosticsEntity(world, entity);
        return true;
    }

    private void CacheDiagnosticsEntity(World world, Entity entity)
    {
        _cachedWorld = world;
        _diagnosticsEntity = entity;
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
            TransportBoardingDiagnostics = ToByte(InitialUnitsRuntimeState.TransportBoardingDiagnostics)
        };
    }

    private static void ApplyLegacyDiagnosticsState(RuntimeDiagnosticsStateComponent state)
    {
        InitialUnitsRuntimeState.VerboseAILogs = state.VerboseAILogs != 0;
        InitialUnitsRuntimeState.TransportBoardingDiagnostics = state.TransportBoardingDiagnostics != 0;
    }

    private static byte ToByte(bool value)
    {
        return value ? (byte)1 : (byte)0;
    }

    private static bool DiagnosticsStateEquals(RuntimeDiagnosticsStateComponent left, RuntimeDiagnosticsStateComponent right)
    {
        return left.VerboseAILogs == right.VerboseAILogs &&
            left.TransportBoardingDiagnostics == right.TransportBoardingDiagnostics;
    }
}
