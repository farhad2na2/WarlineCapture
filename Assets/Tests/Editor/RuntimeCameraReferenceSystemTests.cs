using Game.Rendering;
using Game.Components;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using System.Reflection;

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
            RunCase(tests, nameof(Snapshot_PublishesOnlyAtOrderedSystemBoundary), test => test.Snapshot_PublishesOnlyAtOrderedSystemBoundary());
            RunCase(tests, nameof(Snapshot_ReadsDoNotRepublish), test => test.Snapshot_ReadsDoNotRepublish());
            RunCase(tests, nameof(Snapshot_SignatureIsStableUntilCameraChanges), test => test.Snapshot_SignatureIsStableUntilCameraChanges());
            RunCase(tests, nameof(ClearCamera_PublishesVersionedInvalidBoundary), test => test.ClearCamera_PublishesVersionedInvalidBoundary());
            RunCase(tests, nameof(System_IsOrderFirstInSimulationGroup), test => test.System_IsOrderFirstInSimulationGroup());
            Debug.Log("[RuntimeCameraReferenceFocusedValidation] result=Passed tests=8");
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

    [Test]
    public void Snapshot_PublishesOnlyAtOrderedSystemBoundary()
    {
        Camera camera = CreateCamera();
        camera.transform.SetPositionAndRotation(
            new Vector3(12f, 34f, 56f),
            Quaternion.Euler(45f, 15f, 0f));
        RuntimeCameraReferenceSystem system =
            _world.GetOrCreateSystemManaged<RuntimeCameraReferenceSystem>();

        system.SetWorldCamera(camera);

        Assert.IsFalse(
            RuntimeCameraReferenceSystem.TryGetCameraSnapshot(
                _world,
                out RuntimeCameraSnapshotComponent before));
        Assert.That(before.PublicationVersion, Is.Zero);

        system.Update();

        Assert.IsTrue(
            RuntimeCameraReferenceSystem.TryGetCameraSnapshot(
                _world,
                out RuntimeCameraSnapshotComponent after));
        Assert.That(after.PublicationVersion, Is.EqualTo(1u));
        Assert.That(after.Position, Is.EqualTo(new float3(12f, 34f, 56f)));
        Assert.That(after.Signature.Low, Is.Not.Zero);
        Assert.That(after.Signature.High, Is.Not.Zero);
    }

    [Test]
    public void Snapshot_ReadsDoNotRepublish()
    {
        RuntimeCameraReferenceSystem system =
            _world.GetOrCreateSystemManaged<RuntimeCameraReferenceSystem>();
        system.SetWorldCamera(CreateCamera());
        system.Update();

        Assert.IsTrue(
            RuntimeCameraReferenceSystem.TryGetCameraSnapshot(
                _world,
                out RuntimeCameraSnapshotComponent first));
        Assert.IsTrue(
            RuntimeCameraReferenceSystem.TryGetCameraSnapshot(
                _world,
                out RuntimeCameraSnapshotComponent second));

        Assert.That(first.PublicationVersion, Is.EqualTo(1u));
        Assert.That(second.PublicationVersion, Is.EqualTo(first.PublicationVersion));
        Assert.That(second.Signature.Low, Is.EqualTo(first.Signature.Low));
        Assert.That(second.Signature.High, Is.EqualTo(first.Signature.High));
    }

    [Test]
    public void Snapshot_SignatureIsStableUntilCameraChanges()
    {
        Camera camera = CreateCamera();
        RuntimeCameraReferenceSystem system =
            _world.GetOrCreateSystemManaged<RuntimeCameraReferenceSystem>();
        system.SetWorldCamera(camera);
        system.Update();
        RuntimeCameraReferenceSystem.TryGetCameraSnapshot(
            _world,
            out RuntimeCameraSnapshotComponent first);

        system.Update();
        RuntimeCameraReferenceSystem.TryGetCameraSnapshot(
            _world,
            out RuntimeCameraSnapshotComponent unchanged);

        camera.transform.position = new Vector3(1f, 2f, 3f);
        system.Update();
        RuntimeCameraReferenceSystem.TryGetCameraSnapshot(
            _world,
            out RuntimeCameraSnapshotComponent changed);

        Assert.That(unchanged.PublicationVersion, Is.EqualTo(2u));
        Assert.That(unchanged.Signature.Low, Is.EqualTo(first.Signature.Low));
        Assert.That(unchanged.Signature.High, Is.EqualTo(first.Signature.High));
        Assert.That(changed.PublicationVersion, Is.EqualTo(3u));
        Assert.That(
            changed.Signature.Low != first.Signature.Low ||
            changed.Signature.High != first.Signature.High,
            Is.True);
    }

    [Test]
    public void ClearCamera_PublishesVersionedInvalidBoundary()
    {
        RuntimeCameraReferenceSystem system =
            _world.GetOrCreateSystemManaged<RuntimeCameraReferenceSystem>();
        system.SetWorldCamera(CreateCamera());
        system.Update();
        system.ClearWorldCamera();

        Assert.IsTrue(
            RuntimeCameraReferenceSystem.TryGetCameraSnapshot(
                _world,
                out RuntimeCameraSnapshotComponent stillPublished));
        Assert.That(stillPublished.PublicationVersion, Is.EqualTo(1u));

        system.Update();

        Assert.IsFalse(
            RuntimeCameraReferenceSystem.TryGetCameraSnapshot(
                _world,
                out RuntimeCameraSnapshotComponent invalid));
        Assert.That(invalid.PublicationVersion, Is.EqualTo(2u));
        Assert.That(invalid.Signature.Low, Is.Zero);
        Assert.That(invalid.Signature.High, Is.Zero);
    }

    [Test]
    public void System_IsOrderFirstInSimulationGroup()
    {
        UpdateInGroupAttribute attribute =
            typeof(RuntimeCameraReferenceSystem).GetCustomAttribute<
                UpdateInGroupAttribute>();

        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute.GroupType, Is.EqualTo(typeof(SimulationSystemGroup)));
        Assert.That(attribute.OrderFirst, Is.True);
    }

    private Camera CreateCamera()
    {
        _cameraObject = new GameObject("RuntimeCameraReference_TestCamera");
        return _cameraObject.AddComponent<Camera>();
    }
}
#endif
