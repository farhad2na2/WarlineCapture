#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class TacticalFollowCameraComponentTests
{
    private World _previousWorld;
    private World _world;
    private EntityManager _entityManager;

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new TacticalFollowCameraComponentTests();
            RunCase(tests, nameof(TacticalFollowCameraMode_DefaultsToInactiveAndUnlocked), test => test.TacticalFollowCameraMode_DefaultsToInactiveAndUnlocked());
            RunCase(tests, nameof(TacticalFollowCameraRequestBuffer_StoresToggleRequest), test => test.TacticalFollowCameraRequestBuffer_StoresToggleRequest());
            RunCase(tests, nameof(TacticalFollowCameraPose_CanRepresentRestoreTarget), test => test.TacticalFollowCameraPose_CanRepresentRestoreTarget());
            Debug.Log("[TacticalFollowCameraComponentValidation] result=Passed tests=3");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[TacticalFollowCameraComponentValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(TacticalFollowCameraComponentTests tests, string name, System.Action<TacticalFollowCameraComponentTests> action)
    {
        tests.SetUp();
        try
        {
            action(tests);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[TacticalFollowCameraComponentValidation] result=Failed test={name} error={exception}");
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
        _world = new World("TacticalFollowCameraComponentTests");
        World.DefaultGameObjectInjectionWorld = _world;
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        if (World.DefaultGameObjectInjectionWorld == _world)
            World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world?.Dispose();
    }

    [Test]
    public void TacticalFollowCameraMode_DefaultsToInactiveAndUnlocked()
    {
        Entity entity = _entityManager.CreateEntity(typeof(TacticalFollowCameraModeComponent));

        TacticalFollowCameraModeComponent mode = _entityManager.GetComponentData<TacticalFollowCameraModeComponent>(entity);

        Assert.AreEqual(0, mode.Enabled);
        Assert.AreEqual(0, mode.PanInputLocked);
        Assert.AreEqual(0, mode.HasBaseTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.None, mode.BaseTargetKind);
        Assert.AreEqual(Entity.Null, mode.BaseTargetEntity);
        Assert.AreEqual(0, mode.HasTemporaryTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.None, mode.TemporaryTargetKind);
        Assert.AreEqual(Entity.Null, mode.TemporaryTargetEntity);
    }

    [Test]
    public void TacticalFollowCameraRequestBuffer_StoresToggleRequest()
    {
        Entity entity = _entityManager.CreateEntity(typeof(TacticalFollowCameraRequestQueueComponent));
        DynamicBuffer<TacticalFollowCameraRequestElement> requests =
            _entityManager.AddBuffer<TacticalFollowCameraRequestElement>(entity);

        requests.Add(new TacticalFollowCameraRequestElement
        {
            Kind = TacticalFollowCameraRequestKind.ToggleFollowMode,
            RequestId = 1
        });

        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(TacticalFollowCameraRequestKind.ToggleFollowMode, requests[0].Kind);
        Assert.AreEqual(1, requests[0].RequestId);
    }

    [Test]
    public void TacticalFollowCameraPose_CanRepresentRestoreTarget()
    {
        Entity entity = _entityManager.CreateEntity(typeof(TacticalFollowCameraPoseComponent));
        quaternion rotation = quaternion.EulerXYZ(math.radians(new float3(30f, 45f, 0f)));

        _entityManager.SetComponentData(entity, new TacticalFollowCameraPoseComponent
        {
            Valid = 1,
            Source = TacticalFollowCameraPoseSource.RestoreDefault,
            DesiredPosition = new float3(10f, 20f, 30f),
            DesiredRotation = rotation,
            FieldOfView = 36f,
            OrthographicSize = 24f,
            PositionDampingSeconds = 0.3f,
            RotationDampingSeconds = 0.25f,
            MaxTransitionSpeed = 80f
        });

        TacticalFollowCameraPoseComponent pose = _entityManager.GetComponentData<TacticalFollowCameraPoseComponent>(entity);

        Assert.AreEqual(1, pose.Valid);
        Assert.AreEqual(TacticalFollowCameraPoseSource.RestoreDefault, pose.Source);
        Assert.AreEqual(new float3(10f, 20f, 30f), pose.DesiredPosition);
        Assert.AreEqual(rotation.value, pose.DesiredRotation.value);
        Assert.AreEqual(36f, pose.FieldOfView);
        Assert.AreEqual(24f, pose.OrthographicSize);
        Assert.AreEqual(0.3f, pose.PositionDampingSeconds);
        Assert.AreEqual(0.25f, pose.RotationDampingSeconds);
        Assert.AreEqual(80f, pose.MaxTransitionSpeed);
    }
}
#endif
