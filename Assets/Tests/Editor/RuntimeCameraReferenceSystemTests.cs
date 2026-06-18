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
            RunCase(tests, nameof(SetWorldCamera_StoresManagedBoundaryReference), test => test.SetWorldCamera_StoresManagedBoundaryReference());
            RunCase(tests, nameof(TryGetWorldCamera_ReadsManagedBoundaryReference), test => test.TryGetWorldCamera_ReadsManagedBoundaryReference());
            RunCase(tests, nameof(ClearWorldCamera_ClearsManagedBoundaryReference), test => test.ClearWorldCamera_ClearsManagedBoundaryReference());
            Debug.Log("[RuntimeCameraReferenceFocusedValidation] result=Passed tests=3");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[RuntimeCameraReferenceFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
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
    public void SetWorldCamera_StoresManagedBoundaryReference()
    {
        Camera camera = CreateCamera();
        RuntimeCameraReferenceSystem runtimeCameraReferenceSystem = _world.GetOrCreateSystemManaged<RuntimeCameraReferenceSystem>();

        runtimeCameraReferenceSystem.SetWorldCamera(camera);

        Assert.AreSame(camera, runtimeCameraReferenceSystem.WorldCamera);
    }

    [Test]
    public void TryGetWorldCamera_ReadsManagedBoundaryReference()
    {
        Camera camera = CreateCamera();
        RuntimeCameraReferenceSystem runtimeCameraReferenceSystem = _world.GetOrCreateSystemManaged<RuntimeCameraReferenceSystem>();
        runtimeCameraReferenceSystem.SetWorldCamera(camera);

        bool found = RuntimeCameraReferenceSystem.TryGetWorldCamera(_world, out Camera resolvedCamera);

        Assert.IsTrue(found);
        Assert.AreSame(camera, resolvedCamera);
    }

    [Test]
    public void ClearWorldCamera_ClearsManagedBoundaryReference()
    {
        Camera camera = CreateCamera();
        RuntimeCameraReferenceSystem runtimeCameraReferenceSystem = _world.GetOrCreateSystemManaged<RuntimeCameraReferenceSystem>();
        runtimeCameraReferenceSystem.SetWorldCamera(camera);

        runtimeCameraReferenceSystem.ClearWorldCamera();

        bool found = RuntimeCameraReferenceSystem.TryGetWorldCamera(_world, out Camera resolvedCamera);
        Assert.IsFalse(found);
        Assert.IsNull(resolvedCamera);
        Assert.IsNull(runtimeCameraReferenceSystem.WorldCamera);
    }

    private Camera CreateCamera()
    {
        _cameraObject = new GameObject("RuntimeCameraReference_TestCamera");
        return _cameraObject.AddComponent<Camera>();
    }
}
#endif
