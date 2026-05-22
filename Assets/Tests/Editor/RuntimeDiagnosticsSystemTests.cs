#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;

public sealed class RuntimeDiagnosticsSystemTests
{
    private World _previousWorld;
    private World _world;

    [SetUp]
    public void SetUp()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("RuntimeDiagnosticsSystemTests");
        World.DefaultGameObjectInjectionWorld = _world;
        InitialUnitsRuntimeState.VerboseAILogs = false;
    }

    [TearDown]
    public void TearDown()
    {
        InitialUnitsRuntimeState.VerboseAILogs = false;
        if (World.DefaultGameObjectInjectionWorld == _world)
            World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world?.Dispose();
    }

    [Test]
    public void SettingVerboseAILogs_WritesLegacyAndEcsSingleton()
    {
        var diagnosticsSystem = new RuntimeDiagnosticsSystem();

        diagnosticsSystem.VerboseAILogs = true;

        Assert.IsTrue(InitialUnitsRuntimeState.VerboseAILogs);
        RuntimeDiagnosticsStateComponent state = ReadSingleton<RuntimeDiagnosticsStateComponent>();
        Assert.AreEqual(1, state.VerboseAILogs);
    }

    [Test]
    public void ReadDiagnosticsState_MirrorsLegacyStateIntoEcsSingleton()
    {
        InitialUnitsRuntimeState.VerboseAILogs = true;
        var diagnosticsSystem = new RuntimeDiagnosticsSystem();

        RuntimeDiagnosticsStateComponent state = diagnosticsSystem.ReadDiagnosticsState();

        Assert.AreEqual(1, state.VerboseAILogs);
        RuntimeDiagnosticsStateComponent singleton = ReadSingleton<RuntimeDiagnosticsStateComponent>();
        Assert.AreEqual(1, singleton.VerboseAILogs);
    }

    [Test]
    public void ReadDiagnosticsState_DoesNotOverwriteEcsWhenLegacyIsUnchanged()
    {
        var diagnosticsSystem = new RuntimeDiagnosticsSystem();
        diagnosticsSystem.VerboseAILogs = true;
        RuntimeDiagnosticsStateComponent state = diagnosticsSystem.ReadDiagnosticsState();
        Assert.AreEqual(1, state.VerboseAILogs);

        Entity stateEntity = ReadSingletonEntity<RuntimeDiagnosticsStateComponent>();
        state.VerboseAILogs = 0;
        _world.EntityManager.SetComponentData(stateEntity, state);

        RuntimeDiagnosticsStateComponent reread = diagnosticsSystem.ReadDiagnosticsState();

        Assert.AreEqual(0, reread.VerboseAILogs);
        Assert.IsTrue(InitialUnitsRuntimeState.VerboseAILogs);
    }

    private T ReadSingleton<T>() where T : unmanaged, IComponentData
    {
        using EntityQuery query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
        Assert.AreEqual(1, query.CalculateEntityCount());
        return query.GetSingleton<T>();
    }

    private Entity ReadSingletonEntity<T>() where T : unmanaged, IComponentData
    {
        using EntityQuery query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
        Assert.AreEqual(1, query.CalculateEntityCount());
        return query.GetSingletonEntity();
    }
}
#endif
