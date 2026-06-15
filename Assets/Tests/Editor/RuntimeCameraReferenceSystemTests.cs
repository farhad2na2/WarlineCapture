#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class RuntimeCameraReferenceSystemTests
{
    private World _previousWorld;
    private World _world;
    private GameObject _cameraObject;

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new RuntimeCameraReferenceSystemTests();
            RunCase(tests, nameof(SetWorldCamera_WritesManagedEcsReference), test => test.SetWorldCamera_WritesManagedEcsReference());
            RunCase(tests, nameof(TryGetWorldCamera_ReadsManagedEcsReference), test => test.TryGetWorldCamera_ReadsManagedEcsReference());
            RunCase(tests, nameof(ClearWorldCamera_ClearsManagedEcsReference), test => test.ClearWorldCamera_ClearsManagedEcsReference());
            Debug.Log("[RuntimeCameraReferenceFocusedValidation] result=Passed tests=3");
            EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[RuntimeCameraReferenceFocusedValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    private static void RunCase(RuntimeCameraReferenceSystemTests tests, string name, System.Action<RuntimeCameraReferenceSystemTests> action)
    {
        tests.SetUp();
        try
        {
            action(tests);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[RuntimeCameraReferenceFocusedValidation] result=Failed test={name} error={exception}");
            throw;
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
        _world = new World("RuntimeCameraReferenceSystemTests");
        World.DefaultGameObjectInjectionWorld = _world;
    }

    [TearDown]
    public void TearDown()
    {
        if (_cameraObject != null)
            Object.DestroyImmediate(_cameraObject);
        if (World.DefaultGameObjectInjectionWorld == _world)
            World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world?.Dispose();
    }

    [Test]
    public void SetWorldCamera_WritesManagedEcsReference()
    {
        Camera camera = CreateCamera();
        RuntimeCameraReferenceSystem runtimeCameraReferenceSystem = _world.GetOrCreateSystemManaged<RuntimeCameraReferenceSystem>();

        runtimeCameraReferenceSystem.SetWorldCamera(camera);

        using EntityQuery query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeCameraReferenceComponent>());
        Assert.AreEqual(1, query.CalculateEntityCount());
        Entity entity = query.GetSingletonEntity();
        RuntimeCameraReferenceComponent component = _world.EntityManager.GetComponentObject<RuntimeCameraReferenceComponent>(entity);
        Assert.AreSame(camera, component.WorldCamera);
    }

    [Test]
    public void TryGetWorldCamera_ReadsManagedEcsReference()
    {
        Camera camera = CreateCamera();
        RuntimeCameraReferenceSystem runtimeCameraReferenceSystem = _world.GetOrCreateSystemManaged<RuntimeCameraReferenceSystem>();
        runtimeCameraReferenceSystem.SetWorldCamera(camera);
        using EntityQuery query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeCameraReferenceComponent>());

        bool found = RuntimeCameraReferenceSystem.TryGetWorldCamera(_world.EntityManager, query, out Camera resolvedCamera);

        Assert.IsTrue(found);
        Assert.AreSame(camera, resolvedCamera);
    }

    [Test]
    public void ClearWorldCamera_ClearsManagedEcsReference()
    {
        Camera camera = CreateCamera();
        RuntimeCameraReferenceSystem runtimeCameraReferenceSystem = _world.GetOrCreateSystemManaged<RuntimeCameraReferenceSystem>();
        runtimeCameraReferenceSystem.SetWorldCamera(camera);

        runtimeCameraReferenceSystem.ClearWorldCamera();

        using EntityQuery query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeCameraReferenceComponent>());
        Entity entity = query.GetSingletonEntity();
        RuntimeCameraReferenceComponent component = _world.EntityManager.GetComponentObject<RuntimeCameraReferenceComponent>(entity);
        Assert.IsNull(component.WorldCamera);
    }

    private Camera CreateCamera()
    {
        _cameraObject = new GameObject("RuntimeCameraReference_TestCamera");
        return _cameraObject.AddComponent<Camera>();
    }
}
#endif
