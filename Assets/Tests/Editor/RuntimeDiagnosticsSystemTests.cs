using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;

public sealed class RuntimeDiagnosticsSystemTests
{
    private World _previousWorld;
    private World _world;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.SettingVerboseAILogs_WritesEcsSingleton());
            RunCase(test => test.SettingTransportBoardingDiagnostics_WritesEcsSingleton());
            RunCase(test => test.ReadDiagnosticsState_ReturnsFacadeWrites());
            RunCase(test => test.ReadDiagnosticsState_ReturnsExternalEcsChange());
            RunCase(test => test.ReadDiagnosticsState_FollowsReplacementWorld());
            RunCase(test => test.SelectionDiagnostics_UseProvidedMatchWorld());
            UnityEngine.Debug.Log("[RuntimeDiagnosticsValidation] result=Passed tests=6");
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogError("[RuntimeDiagnosticsValidation] result=Failed");
            UnityEngine.Debug.LogException(exception);
            throw;
        }
    }

    private static void RunCase(System.Action<RuntimeDiagnosticsSystemTests> testCase)
    {
        var tests = new RuntimeDiagnosticsSystemTests();
        tests.SetUp();
        try
        {
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("RuntimeDiagnosticsSystemTests");
        World.DefaultGameObjectInjectionWorld = _world;
        RuntimeGameplayStateTestHelper.SetVerboseAILogs(false);
        RuntimeGameplayStateTestHelper.SetTransportBoardingDiagnostics(false);
    }

    [TearDown]
    public void TearDown()
    {
        RuntimeGameplayStateTestHelper.SetVerboseAILogs(false);
        RuntimeGameplayStateTestHelper.SetTransportBoardingDiagnostics(false);
        if (World.DefaultGameObjectInjectionWorld == _world)
            World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world?.Dispose();
    }

    [Test]
    public void SettingVerboseAILogs_WritesEcsSingleton()
    {
        RuntimeDiagnosticsSystem diagnosticsSystem = ResolveDiagnosticsSystem();

        diagnosticsSystem.VerboseAILogs = true;

        Assert.IsTrue(new RuntimeDiagnosticsSystem().VerboseAILogs);
        RuntimeDiagnosticsStateComponent state = ReadSingleton<RuntimeDiagnosticsStateComponent>();
        Assert.AreEqual(1, state.VerboseAILogs);
    }

    [Test]
    public void SettingTransportBoardingDiagnostics_WritesEcsSingleton()
    {
        RuntimeDiagnosticsSystem diagnosticsSystem = ResolveDiagnosticsSystem();

        diagnosticsSystem.TransportBoardingDiagnostics = true;

        Assert.IsTrue(new RuntimeDiagnosticsSystem().TransportBoardingDiagnostics);
        RuntimeDiagnosticsStateComponent state = ReadSingleton<RuntimeDiagnosticsStateComponent>();
        Assert.AreEqual(1, state.TransportBoardingDiagnostics);
    }

    [Test]
    public void ReadDiagnosticsState_ReturnsFacadeWrites()
    {
        RuntimeGameplayStateTestHelper.SetVerboseAILogs(true);
        RuntimeGameplayStateTestHelper.SetTransportBoardingDiagnostics(true);
        RuntimeDiagnosticsSystem diagnosticsSystem = ResolveDiagnosticsSystem();

        RuntimeDiagnosticsStateComponent state = diagnosticsSystem.ReadDiagnosticsState();

        Assert.AreEqual(1, state.VerboseAILogs);
        Assert.AreEqual(1, state.TransportBoardingDiagnostics);
        RuntimeDiagnosticsStateComponent singleton = ReadSingleton<RuntimeDiagnosticsStateComponent>();
        Assert.AreEqual(1, singleton.VerboseAILogs);
        Assert.AreEqual(1, singleton.TransportBoardingDiagnostics);
    }

    [Test]
    public void ReadDiagnosticsState_ReturnsExternalEcsChange()
    {
        RuntimeDiagnosticsSystem diagnosticsSystem = ResolveDiagnosticsSystem();
        diagnosticsSystem.VerboseAILogs = true;
        diagnosticsSystem.TransportBoardingDiagnostics = true;
        RuntimeDiagnosticsStateComponent state = diagnosticsSystem.ReadDiagnosticsState();
        Assert.AreEqual(1, state.VerboseAILogs);
        Assert.AreEqual(1, state.TransportBoardingDiagnostics);

        Entity stateEntity = ReadSingletonEntity<RuntimeDiagnosticsStateComponent>();
        state.VerboseAILogs = 0;
        state.TransportBoardingDiagnostics = 0;
        _world.EntityManager.SetComponentData(stateEntity, state);

        RuntimeDiagnosticsStateComponent reread = diagnosticsSystem.ReadDiagnosticsState();

        Assert.AreEqual(0, reread.VerboseAILogs);
        Assert.AreEqual(0, reread.TransportBoardingDiagnostics);
        Assert.IsFalse(new RuntimeDiagnosticsSystem().VerboseAILogs);
        Assert.IsFalse(new RuntimeDiagnosticsSystem().TransportBoardingDiagnostics);
    }

    [Test]
    public void ReadDiagnosticsState_FollowsReplacementWorld()
    {
        RuntimeDiagnosticsSystem diagnosticsSystem = ResolveDiagnosticsSystem();
        diagnosticsSystem.VerboseAILogs = true;
        Assert.AreEqual(1, ReadSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs);

        _world.Dispose();
        _world = new World("RuntimeDiagnosticsSystemTests-Replacement");
        World.DefaultGameObjectInjectionWorld = _world;
        RuntimeGameplayStateTestHelper.SetVerboseAILogs(false);
        RuntimeGameplayStateTestHelper.SetTransportBoardingDiagnostics(false);

        RuntimeDiagnosticsStateComponent replacementState = diagnosticsSystem.ReadDiagnosticsState();

        Assert.AreEqual(0, replacementState.VerboseAILogs);
        Assert.AreEqual(0, replacementState.TransportBoardingDiagnostics);
        using EntityQuery stateQuery = _world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
        Assert.AreEqual(1, stateQuery.CalculateEntityCount());
    }

    [Test]
    public void SelectionDiagnostics_UseProvidedMatchWorld()
    {
        var selectionDiagnostics = new SelectionRuntimeDiagnosticsSystemHelper();
        Entity firstState = ReadSingletonEntity<RuntimeDiagnosticsStateComponent>();
        _world.EntityManager.SetComponentData(firstState, new RuntimeDiagnosticsStateComponent
        {
            TransportBoardingDiagnostics = 1
        });
        selectionDiagnostics.EnqueueSelectionDiagnostic(_world.EntityManager, "first match");

        using World replacementWorld = new("RuntimeDiagnosticsSystemTests-SelectionReplacement");
        Entity replacementState = replacementWorld.EntityManager.CreateEntity(typeof(RuntimeDiagnosticsStateComponent));
        replacementWorld.EntityManager.SetComponentData(replacementState, new RuntimeDiagnosticsStateComponent
        {
            TransportBoardingDiagnostics = 1
        });
        selectionDiagnostics.EnqueueSelectionDiagnostic(replacementWorld.EntityManager, "replacement match");

        AssertDiagnosticMessage(_world.EntityManager, "[Selection] first match");
        AssertDiagnosticMessage(replacementWorld.EntityManager, "[Selection] replacement match");
    }

    private static void AssertDiagnosticMessage(EntityManager entityManager, string expected)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<TransportBoardingDiagnosticLogQueueComponent>(),
            ComponentType.ReadOnly<TransportBoardingDiagnosticLogComponent>());
        Assert.AreEqual(1, query.CalculateEntityCount());
        DynamicBuffer<TransportBoardingDiagnosticLogComponent> messages =
            entityManager.GetBuffer<TransportBoardingDiagnosticLogComponent>(query.GetSingletonEntity());
        Assert.AreEqual(1, messages.Length);
        Assert.AreEqual(expected, messages[0].Message.ToString());
    }

    private RuntimeDiagnosticsSystem ResolveDiagnosticsSystem()
    {
        return new RuntimeDiagnosticsSystem();
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
