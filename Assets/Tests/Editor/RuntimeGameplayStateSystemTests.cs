using Game.Components;
using Game.Runtime;
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
            RunCase(test => test.SettingGameplayFlag_WritesEcsSingleton());
            RunCase(test => test.SettingCameraInput_WritesEcsSingleton());
            RunCase(test => test.TryConsumeInitialCameraFocus_ReturnsWorldAndClearsRequest());
            RunCase(test => test.ReadGameplayState_ReturnsExternalEcsChange());
            RunCase(test => test.ResetForGameplayStart_RequestsPlayWithoutActivatingSimulation());
            RunCase(test => test.ResetForMatchShutdown_ClearsGameplayCameraAndFocus());
            RunCase(test => test.ReadGameplayState_RebindsAfterWorldReplacement());
            RunCase(test => test.SeparateWorlds_DoNotShareGameplayState());
            RunCase(test => test.DisposedWorld_ReadsDefaultAndIgnoresWrites());
            UnityEngine.Debug.Log("[RuntimeGameplayStateValidation] result=Passed tests=9");
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
    }

    [TearDown]
    public void TearDown()
    {
        if (World.DefaultGameObjectInjectionWorld == _world)
            World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world?.Dispose();
    }

    [Test]
    public void SettingGameplayFlag_WritesEcsSingleton()
    {
        var runtimeState = new RuntimeGameplayStateSystem(World.DefaultGameObjectInjectionWorld.EntityManager)
        {
            SelectionModeActive = true,
            SimulationActive = true,
            SuppressNextWorldClick = true,
            PlayerAutoModeEnabled = true
        };

        RuntimeGameplayStateComponent state = ReadSingleton<RuntimeGameplayStateComponent>();
        Assert.AreEqual(1, state.SelectionModeActive);
        Assert.AreEqual(1, state.SimulationActive);
        Assert.AreEqual(1, state.SuppressNextWorldClick);
        Assert.AreEqual(1, state.PlayerAutoModeEnabled);
    }

    [Test]
    public void SettingCameraInput_WritesEcsSingleton()
    {
        var runtimeState = new RuntimeGameplayStateSystem(World.DefaultGameObjectInjectionWorld.EntityManager)
        {
            ZoomInHeld = true,
            ZoomOutHeld = true
        };

        RuntimeCameraInputComponent input = ReadSingleton<RuntimeCameraInputComponent>();
        Assert.AreEqual(1, input.ZoomInHeld);
        Assert.AreEqual(1, input.ZoomOutHeld);
    }

    [Test]
    public void TryConsumeInitialCameraFocus_ReturnsWorldAndClearsRequest()
    {
        var runtimeState = new RuntimeGameplayStateSystem(World.DefaultGameObjectInjectionWorld.EntityManager);
        Vector3 focus = new(3f, 0f, 9f);
        runtimeState.InitialCameraFocusWorld = focus;
        runtimeState.InitialCameraFocusRequested = true;

        bool consumed = runtimeState.TryConsumeInitialCameraFocus(out Vector3 consumedFocus);

        Assert.IsTrue(consumed);
        Assert.AreEqual(focus, consumedFocus);
        Assert.AreEqual(0, ReadSingleton<RuntimeCameraFocusRequestComponent>().Requested);
    }

    [Test]
    public void ReadGameplayState_ReturnsExternalEcsChange()
    {
        var runtimeState = new RuntimeGameplayStateSystem(World.DefaultGameObjectInjectionWorld.EntityManager) { PlayRequested = true };
        Entity stateEntity = ReadSingletonEntity<RuntimeGameplayStateComponent>();
        RuntimeGameplayStateComponent state = _world.EntityManager.GetComponentData<RuntimeGameplayStateComponent>(stateEntity);
        state.BuildModeActive = 1;
        _world.EntityManager.SetComponentData(stateEntity, state);

        RuntimeGameplayStateComponent reread = runtimeState.ReadGameplayState();

        Assert.AreEqual(1, reread.PlayRequested);
        Assert.AreEqual(1, reread.BuildModeActive);
    }

    [Test]
    public void ResetForGameplayStart_RequestsPlayWithoutActivatingSimulation()
    {
        var runtimeState = new RuntimeGameplayStateSystem(World.DefaultGameObjectInjectionWorld.EntityManager)
        {
            SimulationActive = true,
            BuildModeActive = true,
            ZoomInHeld = true,
            InitialCameraFocusRequested = true
        };

        runtimeState.ResetForGameplayStart();

        RuntimeGameplayStateComponent state = ReadSingleton<RuntimeGameplayStateComponent>();
        Assert.AreEqual(1, state.PlayRequested);
        Assert.AreEqual(0, state.SimulationActive);
        Assert.AreEqual(0, state.BuildModeActive);
        Assert.AreEqual(1, state.SuppressNextWorldClick);
        Assert.AreEqual(0, ReadSingleton<RuntimeCameraInputComponent>().ZoomInHeld);
        Assert.AreEqual(0, ReadSingleton<RuntimeCameraFocusRequestComponent>().Requested);
    }

    [Test]
    public void ResetForMatchShutdown_ClearsGameplayCameraAndFocus()
    {
        var runtimeState = new RuntimeGameplayStateSystem(World.DefaultGameObjectInjectionWorld.EntityManager)
        {
            PlayRequested = true,
            SimulationActive = true,
            FullscreenMapOpen = true,
            ZoomOutHeld = true,
            InitialCameraFocusWorld = new Vector3(7f, 0f, 5f),
            InitialCameraFocusRequested = true
        };

        runtimeState.ResetForMatchShutdown();

        Assert.AreEqual(default(RuntimeGameplayStateComponent), ReadSingleton<RuntimeGameplayStateComponent>());
        Assert.AreEqual(default(RuntimeCameraInputComponent), ReadSingleton<RuntimeCameraInputComponent>());
        Assert.AreEqual(default(RuntimeCameraFocusRequestComponent), ReadSingleton<RuntimeCameraFocusRequestComponent>());
    }

    [Test]
    public void ReadGameplayState_RebindsAfterWorldReplacement()
    {
        var runtimeState = new RuntimeGameplayStateSystem(World.DefaultGameObjectInjectionWorld.EntityManager) { PlayRequested = true };
        Assert.AreEqual(1, ReadSingleton<RuntimeGameplayStateComponent>().PlayRequested);

        _world.Dispose();
        _world = new World("RuntimeGameplayStateSystemTests-Replacement");
        World.DefaultGameObjectInjectionWorld = _world;
        runtimeState.Bind(_world.EntityManager);

        RuntimeGameplayStateComponent replacementState = runtimeState.ReadGameplayState();

        Assert.AreEqual(default(RuntimeGameplayStateComponent), replacementState);
        Assert.AreEqual(1, CountSingletons<RuntimeGameplayStateComponent>());
    }

    [Test]
    public void SeparateWorlds_DoNotShareGameplayState()
    {
        var runtimeState = new RuntimeGameplayStateSystem(World.DefaultGameObjectInjectionWorld.EntityManager) { PlayerAutoModeEnabled = true };
        World firstWorld = _world;
        _world = new World("RuntimeGameplayStateSystemTests-Independent");
        World.DefaultGameObjectInjectionWorld = _world;
        runtimeState.Bind(_world.EntityManager);

        Assert.IsFalse(runtimeState.PlayerAutoModeEnabled);
        runtimeState.BuildModeActive = true;

        using (EntityQuery firstQuery = firstWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>()))
        {
            RuntimeGameplayStateComponent firstState = firstQuery.GetSingleton<RuntimeGameplayStateComponent>();
            Assert.AreEqual(1, firstState.PlayerAutoModeEnabled);
            Assert.AreEqual(0, firstState.BuildModeActive);
        }
        firstWorld.Dispose();
    }

    [Test]
    public void DisposedWorld_ReadsDefaultAndIgnoresWrites()
    {
        var runtimeState = new RuntimeGameplayStateSystem(_world.EntityManager) { PlayRequested = true };
        World disposedWorld = _world;
        World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world = null;
        disposedWorld.Dispose();

        Assert.DoesNotThrow(() => _ = runtimeState.PlayRequested);
        Assert.IsFalse(runtimeState.PlayRequested);
        Assert.DoesNotThrow(() => runtimeState.PlayRequested = true);
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

    private int CountSingletons<T>() where T : unmanaged, IComponentData
    {
        using EntityQuery query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return query.CalculateEntityCount();
    }
}
#endif
