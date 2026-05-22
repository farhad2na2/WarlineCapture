#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class RuntimeCameraReferenceSystemTests
{
    private World _previousWorld;
    private World _world;
    private GameObject _cameraObject;

    [SetUp]
    public void SetUp()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("RuntimeCameraReferenceSystemTests");
        World.DefaultGameObjectInjectionWorld = _world;
        InitialUnitsRuntimeState.WorldCamera = null;
    }

    [TearDown]
    public void TearDown()
    {
        InitialUnitsRuntimeState.WorldCamera = null;
        if (_cameraObject != null)
            Object.DestroyImmediate(_cameraObject);
        if (World.DefaultGameObjectInjectionWorld == _world)
            World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world?.Dispose();
    }

    [Test]
    public void SetWorldCamera_WritesLegacyAndManagedEcsReference()
    {
        Camera camera = CreateCamera();
        var runtimeCameraReferenceSystem = new RuntimeCameraReferenceSystem();

        runtimeCameraReferenceSystem.SetWorldCamera(camera);

        Assert.AreSame(camera, InitialUnitsRuntimeState.WorldCamera);
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
        var runtimeCameraReferenceSystem = new RuntimeCameraReferenceSystem();
        runtimeCameraReferenceSystem.SetWorldCamera(camera);
        using EntityQuery query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeCameraReferenceComponent>());

        bool found = RuntimeCameraReferenceSystem.TryGetWorldCamera(_world.EntityManager, query, out Camera resolvedCamera);

        Assert.IsTrue(found);
        Assert.AreSame(camera, resolvedCamera);
    }

    [Test]
    public void ClearWorldCamera_ClearsLegacyAndManagedEcsReference()
    {
        Camera camera = CreateCamera();
        var runtimeCameraReferenceSystem = new RuntimeCameraReferenceSystem();
        runtimeCameraReferenceSystem.SetWorldCamera(camera);

        runtimeCameraReferenceSystem.ClearWorldCamera();

        Assert.IsNull(InitialUnitsRuntimeState.WorldCamera);
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
