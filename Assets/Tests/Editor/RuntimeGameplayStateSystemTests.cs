#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class RuntimeGameplayStateSystemTests
{
    private World _previousWorld;
    private World _world;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.SettingGameplayFlag_WritesLegacyAndEcsSingleton());
            RunCase(test => test.SettingCameraInput_WritesLegacyAndEcsSingleton());
            RunCase(test => test.TryConsumeInitialCameraFocus_ReturnsWorldAndClearsRequest());
            RunCase(test => test.ReadGameplayState_MirrorsLegacyStateIntoEcsSingleton());
            RunCase(test => test.ResetForGameplayStart_RequestsPlayWithoutActivatingSimulation());
            RunCase(test => test.ReadGameplayState_DoesNotOverwriteEcsWhenLegacyIsUnchanged());
            RunCase(test => test.ReadGameplayState_MirrorsLaterLegacyChangeOnceDetected());
            UnityEngine.Debug.Log("[RuntimeGameplayStateValidation] result=Passed tests=7");
            ValidationExit.Passed();
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogError("[RuntimeGameplayStateValidation] result=Failed");
            UnityEngine.Debug.LogException(exception);
            ValidationExit.Failed();
        }
    }

    private static void RunCase(System.Action<RuntimeGameplayStateSystemTests> testCase)
    {
        var tests = new RuntimeGameplayStateSystemTests();
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
        _world = new World("RuntimeGameplayStateSystemTests");
        World.DefaultGameObjectInjectionWorld = _world;
        ResetLegacyState();
    }

    [TearDown]
    public void TearDown()
    {
        ResetLegacyState();
        if (World.DefaultGameObjectInjectionWorld == _world)
            World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world?.Dispose();
    }

    [Test]
    public void SettingGameplayFlag_WritesLegacyAndEcsSingleton()
    {
        var runtimeState = new RuntimeGameplayStateSystem();

        runtimeState.SelectionModeActive = true;
        runtimeState.SimulationActive = true;
        runtimeState.SuppressNextWorldClick = true;
        runtimeState.PlayerAutoModeEnabled = true;

        Assert.IsTrue(InitialUnitsRuntimeState.SelectionModeActive);
        Assert.IsTrue(InitialUnitsRuntimeState.SimulationActive);
        Assert.IsTrue(InitialUnitsRuntimeState.SuppressNextWorldClick);
        Assert.IsTrue(InitialUnitsRuntimeState.PlayerAutoModeEnabled);
        RuntimeGameplayStateComponent state = ReadSingleton<RuntimeGameplayStateComponent>();
        Assert.AreEqual(1, state.SelectionModeActive);
        Assert.AreEqual(1, state.SimulationActive);
        Assert.AreEqual(1, state.SuppressNextWorldClick);
        Assert.AreEqual(1, state.PlayerAutoModeEnabled);
    }

    [Test]
    public void SettingCameraInput_WritesLegacyAndEcsSingleton()
    {
        var runtimeState = new RuntimeGameplayStateSystem();

        runtimeState.ZoomInHeld = true;
        runtimeState.ZoomOutHeld = true;

        Assert.IsTrue(InitialUnitsRuntimeState.ZoomInHeld);
        Assert.IsTrue(InitialUnitsRuntimeState.ZoomOutHeld);
        RuntimeCameraInputComponent input = ReadSingleton<RuntimeCameraInputComponent>();
        Assert.AreEqual(1, input.ZoomInHeld);
        Assert.AreEqual(1, input.ZoomOutHeld);
    }

    [Test]
    public void TryConsumeInitialCameraFocus_ReturnsWorldAndClearsRequest()
    {
        var runtimeState = new RuntimeGameplayStateSystem();
        Vector3 focus = new(3f, 0f, 9f);
        runtimeState.InitialCameraFocusWorld = focus;
        runtimeState.InitialCameraFocusRequested = true;

        bool consumed = runtimeState.TryConsumeInitialCameraFocus(out Vector3 consumedFocus);

        Assert.IsTrue(consumed);
        Assert.AreEqual(focus, consumedFocus);
        Assert.IsFalse(InitialUnitsRuntimeState.InitialCameraFocusRequested);
        RuntimeCameraFocusRequestComponent request = ReadSingleton<RuntimeCameraFocusRequestComponent>();
        Assert.AreEqual(0, request.Requested);
    }

    [Test]
    public void ReadGameplayState_MirrorsLegacyStateIntoEcsSingleton()
    {
        InitialUnitsRuntimeState.PlayRequested = true;
        InitialUnitsRuntimeState.SimulationActive = true;
        InitialUnitsRuntimeState.BuildModeActive = true;
        InitialUnitsRuntimeState.PlayerAutoModeEnabled = true;
        var runtimeState = new RuntimeGameplayStateSystem();

        RuntimeGameplayStateComponent state = runtimeState.ReadGameplayState();

        Assert.AreEqual(1, state.PlayRequested);
        Assert.AreEqual(1, state.SimulationActive);
        Assert.AreEqual(1, state.BuildModeActive);
        Assert.AreEqual(1, state.PlayerAutoModeEnabled);
        RuntimeGameplayStateComponent singleton = ReadSingleton<RuntimeGameplayStateComponent>();
        Assert.AreEqual(1, singleton.PlayRequested);
        Assert.AreEqual(1, singleton.SimulationActive);
        Assert.AreEqual(1, singleton.BuildModeActive);
        Assert.AreEqual(1, singleton.PlayerAutoModeEnabled);
    }

    [Test]
    public void ResetForGameplayStart_RequestsPlayWithoutActivatingSimulation()
    {
        var runtimeState = new RuntimeGameplayStateSystem();
        runtimeState.SimulationActive = true;
        runtimeState.BuildModeActive = true;

        runtimeState.ResetForGameplayStart();

        RuntimeGameplayStateComponent state = ReadSingleton<RuntimeGameplayStateComponent>();
        Assert.AreEqual(1, state.PlayRequested);
        Assert.AreEqual(0, state.SimulationActive);
        Assert.AreEqual(0, state.BuildModeActive);
        Assert.IsTrue(InitialUnitsRuntimeState.PlayRequested);
        Assert.IsFalse(InitialUnitsRuntimeState.SimulationActive);
    }

    [Test]
    public void ReadGameplayState_DoesNotOverwriteEcsWhenLegacyIsUnchanged()
    {
        var runtimeState = new RuntimeGameplayStateSystem();
        runtimeState.PlayRequested = true;
        RuntimeGameplayStateComponent state = runtimeState.ReadGameplayState();
        Assert.AreEqual(1, state.PlayRequested);

        Entity stateEntity = ReadSingletonEntity<RuntimeGameplayStateComponent>();
        state.BuildModeActive = 1;
        _world.EntityManager.SetComponentData(stateEntity, state);

        RuntimeGameplayStateComponent reread = runtimeState.ReadGameplayState();

        Assert.AreEqual(1, reread.BuildModeActive);
        Assert.IsFalse(InitialUnitsRuntimeState.BuildModeActive);
    }

    [Test]
    public void ReadGameplayState_MirrorsLaterLegacyChangeOnceDetected()
    {
        var runtimeState = new RuntimeGameplayStateSystem();
        RuntimeGameplayStateComponent state = runtimeState.ReadGameplayState();
        Assert.AreEqual(0, state.BuildModeActive);

        InitialUnitsRuntimeState.BuildModeActive = true;

        RuntimeGameplayStateComponent reread = runtimeState.ReadGameplayState();

        Assert.AreEqual(1, reread.BuildModeActive);
        RuntimeGameplayStateComponent singleton = ReadSingleton<RuntimeGameplayStateComponent>();
        Assert.AreEqual(1, singleton.BuildModeActive);
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

    private static void ResetLegacyState()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        InitialUnitsRuntimeState.SimulationActive = false;
        InitialUnitsRuntimeState.InitialCameraFocusRequested = false;
        InitialUnitsRuntimeState.InitialCameraFocusWorld = Vector3.zero;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.BuildModeActive = false;
        InitialUnitsRuntimeState.FullscreenMapOpen = false;
        InitialUnitsRuntimeState.FullscreenMapIsoMode = false;
        InitialUnitsRuntimeState.ZoomInHeld = false;
        InitialUnitsRuntimeState.ZoomOutHeld = false;
        InitialUnitsRuntimeState.SuppressNextWorldClick = false;
        InitialUnitsRuntimeState.PlayerAutoModeEnabled = false;
    }
}
#endif
