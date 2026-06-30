#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class RtsCameraSystemTests
{
    private readonly System.Collections.Generic.List<GameObject> _createdObjects = new();
    private World _cameraSystemWorld;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(nameof(Dragging_CanBeSetAndCleared), test => test.Dragging_CanBeSetAndCleared());
            RunCase(nameof(SetSmoothFocusTarget_StoresGroundTarget), test => test.SetSmoothFocusTarget_StoresGroundTarget());
            RunCase(nameof(UpdateSmoothFocus_WhenAlreadyAtTargetClearsTarget), test => test.UpdateSmoothFocus_WhenAlreadyAtTargetClearsTarget());
            RunCase(nameof(ResetSession_ClearsDragAndSmoothFocus), test => test.ResetSession_ClearsDragAndSmoothFocus());
            RunCase(nameof(ResetCameraModeSession_ClearsModeTransitionState), test => test.ResetCameraModeSession_ClearsModeTransitionState());
            RunCase(nameof(PanCamera_MovesAlongFlattenedCameraAxes), test => test.PanCamera_MovesAlongFlattenedCameraAxes());
            RunCase(nameof(RuntimePanCamera_IgnoresPanAndDragWhenTacticalFollowLocked), test => test.RuntimePanCamera_IgnoresPanAndDragWhenTacticalFollowLocked());
            RunCase(nameof(ApplyPerspectiveCameraModeInstant_ConfiguresPerspectiveCamera), test => test.ApplyPerspectiveCameraModeInstant_ConfiguresPerspectiveCamera());
            RunCase(nameof(MoveCameraGroundCenterTo_PreservesHeightAndMovesGroundCenter), test => test.MoveCameraGroundCenterTo_PreservesHeightAndMovesGroundCenter());
            RunCase(nameof(UpdateFullscreenIsoZoom_ClampsTargets), test => test.UpdateFullscreenIsoZoom_ClampsTargets());
            RunCase(nameof(TacticalFollowPoseRequest_UpdatesCameraThroughRequestQueue), test => test.TacticalFollowPoseRequest_UpdatesCameraThroughRequestQueue());
            RunCase(nameof(TacticalFollowPoseRequest_CanRestoreOrthographicCameraThroughRequestQueue), test => test.TacticalFollowPoseRequest_CanRestoreOrthographicCameraThroughRequestQueue());
            RunCase(nameof(MatchIntroFirstPlay_StartsZoomedOutAndTransitionsToNormalThroughRequests), test => test.MatchIntroFirstPlay_StartsZoomedOutAndTransitionsToNormalThroughRequests());
            RunCase(nameof(MatchIntroFirstPlay_HoldsZoomedOutUntilIntroCompletes), test => test.MatchIntroFirstPlay_HoldsZoomedOutUntilIntroCompletes());
            Debug.Log("[RtsCameraFocusedValidation] result=Passed tests=14");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RtsCameraFocusedValidation] result=Failed\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    public static void RunMatchIntroValidation()
    {
        var tests = new RtsCameraSystemTests();
        try
        {
            tests.MatchIntroFirstPlay_StartsZoomedOutAndTransitionsToNormalThroughRequests();
            tests.MatchIntroFirstPlay_HoldsZoomedOutUntilIntroCompletes();
            Debug.Log("[RtsCameraMatchIntroValidation] result=Passed tests=2");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RtsCameraMatchIntroValidation] result=Failed\n{exception}");
            ValidationExit.Exit(1);
        }
        finally
        {
            tests.TearDown();
        }
    }

    private static void RunCase(string name, Action<RtsCameraSystemTests> action)
    {
        var tests = new RtsCameraSystemTests();
        try
        {
            action(tests);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RtsCameraFocusedValidation] result=Failed test={name} error={exception}");
            throw;
        }
        finally
        {
            tests.TearDown();
        }
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _createdObjects.Count; i++)
            UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
        _createdObjects.Clear();
        if (_cameraSystemWorld != null && _cameraSystemWorld.IsCreated)
            _cameraSystemWorld.Dispose();
        _cameraSystemWorld = null;
    }

    [Test]
    public void Dragging_CanBeSetAndCleared()
    {
        RtsCameraSystem cameraSystem = CreateCameraSystem();

        cameraSystem.SetDragging(true);
        Assert.IsTrue(cameraSystem.IsDragging);

        cameraSystem.ClearDragging();
        Assert.IsFalse(cameraSystem.IsDragging);
    }

    [Test]
    public void SetSmoothFocusTarget_StoresGroundTarget()
    {
        RtsCameraSystem cameraSystem = CreateCameraSystem();

        cameraSystem.SetSmoothFocusTarget(new Vector3(3f, 12f, -4f), resetVelocity: true);

        Assert.IsTrue(cameraSystem.HasSmoothFocusTarget);
        Assert.AreEqual(new Vector3(3f, 0f, -4f), cameraSystem.SmoothFocusTarget);
    }

    [Test]
    public void UpdateSmoothFocus_WhenAlreadyAtTargetClearsTarget()
    {
        RtsCameraSystem cameraSystem = CreateCameraSystem();
        Vector3 target = new(5f, 0f, 7f);
        cameraSystem.SetSmoothFocusTarget(target, resetVelocity: true);

        Vector3 smoothed = cameraSystem.UpdateSmoothFocus(target, 0.25f);

        Assert.AreEqual(target, smoothed);
        Assert.IsFalse(cameraSystem.HasSmoothFocusTarget);
    }

    [Test]
    public void ResetSession_ClearsDragAndSmoothFocus()
    {
        RtsCameraSystem cameraSystem = CreateCameraSystem();
        cameraSystem.SetDragging(true);
        cameraSystem.SetSmoothFocusTarget(new Vector3(1f, 0f, 2f), resetVelocity: true);

        cameraSystem.ResetSession();

        Assert.IsFalse(cameraSystem.IsDragging);
        Assert.IsFalse(cameraSystem.HasSmoothFocusTarget);
    }

    [Test]
    public void ResetCameraModeSession_ClearsModeTransitionState()
    {
        RtsCameraSystem cameraSystem = CreateCameraSystem();
        cameraSystem.WasPlayRequested = true;
        cameraSystem.WasBuildModeActive = true;
        cameraSystem.IsZoomTransitionActive = true;
        cameraSystem.NormalIsoModeActive = true;
        cameraSystem.FullscreenIsoTargetHeight = 20f;
        cameraSystem.FullscreenIsoTargetOrthographicSize = 12f;

        cameraSystem.ResetCameraModeSession();

        Assert.IsFalse(cameraSystem.WasPlayRequested);
        Assert.IsFalse(cameraSystem.WasBuildModeActive);
        Assert.IsFalse(cameraSystem.IsZoomTransitionActive);
        Assert.IsFalse(cameraSystem.NormalIsoModeActive);
    }

    [Test]
    public void PanCamera_MovesAlongFlattenedCameraAxes()
    {
        RtsCameraSystem cameraSystem = CreateCameraSystem();
        Camera camera = CreateCamera(new Vector3(0f, 10f, -10f), Quaternion.Euler(45f, 0f, 0f));

        bool moved = cameraSystem.PanCamera(camera, new Vector2(10f, 0f), 0.1f);

        Assert.IsTrue(moved);
        Assert.That(camera.transform.position.x, Is.EqualTo(-1f).Within(0.0001f));
        Assert.That(camera.transform.position.y, Is.EqualTo(10f).Within(0.0001f));
        Assert.That(camera.transform.position.z, Is.EqualTo(-10f).Within(0.0001f));
    }

    [Test]
    public void RuntimePanCamera_IgnoresPanAndDragWhenTacticalFollowLocked()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowPanLock");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            var runtime = new RuntimeGameplayStateSystem { PlayRequested = true };
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            var runtimeCameraSystem = new RtsSelectionRuntimeCameraSystemHelper();
            Camera camera = CreateCamera(new Vector3(0f, 10f, -10f), Quaternion.Euler(45f, 0f, 0f));
            Entity modeEntity = world.EntityManager.CreateEntity(typeof(TacticalFollowCameraModeComponent));
            world.EntityManager.SetComponentData(modeEntity, new TacticalFollowCameraModeComponent
            {
                Enabled = 1,
                PanInputLocked = 1
            });
            var context = new RtsSelectionRuntimeCameraSystemHelper.Context(
                runtime,
                new RtsSelectionInputCompositionSystemHelper(),
                cameraSystem,
                cameraRequestSystem,
                camera,
                null,
                null,
                null,
                default,
                TryGetDefaultEntityManager,
                NullMatchIntroStateQuery.Instance,
                null,
                null,
                null,
                0.03f,
                20f,
                10f,
                45f,
                24f,
                100f,
                58f,
                64f,
                10f,
                10f,
                36f,
                32f,
                40f,
                82f,
                10f,
                24f,
                10f);

            Vector3 originalPosition = camera.transform.position;

            runtimeCameraSystem.SetCameraDragging(context, true);
            runtimeCameraSystem.PanCamera(context, new Vector2(10f, 0f));

            Assert.IsFalse(cameraSystem.IsDragging);
            Assert.AreEqual(originalPosition, camera.transform.position);

            cameraSystem.SetDragging(true);
            Assert.IsTrue(runtimeCameraSystem.UpdateRuntimeCameraTick(context));

            Assert.IsFalse(cameraSystem.IsDragging);
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }

        bool TryGetDefaultEntityManager(out EntityManager entityManager)
        {
            entityManager = world.EntityManager;
            return world.IsCreated;
        }
    }

    [Test]
    public void ApplyPerspectiveCameraModeInstant_ConfiguresPerspectiveCamera()
    {
        RtsCameraSystem cameraSystem = CreateCameraSystem();
        Camera camera = CreateCamera(Vector3.zero, Quaternion.identity);
        camera.orthographic = true;

        cameraSystem.ApplyPerspectiveCameraModeInstant(camera, 24f, 58f, 10f, 36f);

        Assert.IsFalse(camera.orthographic);
        Assert.That(camera.transform.position.y, Is.EqualTo(24f).Within(0.0001f));
        Assert.That(camera.transform.rotation.eulerAngles.x, Is.EqualTo(58f).Within(0.0001f));
        Assert.That(camera.transform.rotation.eulerAngles.y, Is.EqualTo(10f).Within(0.0001f));
        Assert.That(camera.fieldOfView, Is.EqualTo(36f).Within(0.0001f));
    }

    [Test]
    public void MoveCameraGroundCenterTo_PreservesHeightAndMovesGroundCenter()
    {
        RtsCameraSystem cameraSystem = CreateCameraSystem();
        Camera camera = CreateCamera(new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));

        cameraSystem.MoveCameraGroundCenterTo(camera, new Vector3(5f, 0f, 7f));

        Vector3 groundCenter = cameraSystem.GetCameraGroundCenterWorld(camera);
        Assert.That(groundCenter.x, Is.EqualTo(5f).Within(0.0001f));
        Assert.That(groundCenter.z, Is.EqualTo(7f).Within(0.0001f));
        Assert.That(camera.transform.position.y, Is.EqualTo(10f).Within(0.0001f));
    }

    [Test]
    public void UpdateFullscreenIsoZoom_ClampsTargets()
    {
        RtsCameraSystem cameraSystem = CreateCameraSystem();
        cameraSystem.FullscreenIsoTargetHeight = 20f;
        cameraSystem.FullscreenIsoTargetOrthographicSize = 10f;

        cameraSystem.UpdateFullscreenIsoZoom(1f, 100f, 1f, 10f, 45f);

        Assert.That(cameraSystem.FullscreenIsoTargetHeight, Is.EqualTo(10f).Within(0.0001f));
        Assert.That(cameraSystem.FullscreenIsoTargetOrthographicSize, Is.EqualTo(8f).Within(0.0001f));
    }

    [Test]
    public void TacticalFollowPoseRequest_UpdatesCameraThroughRequestQueue()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowPose");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            Camera camera = CreateCamera(Vector3.zero, Quaternion.identity);

            cameraRequestSystem.QueueUpdateTacticalFollowPose(
                world.EntityManager,
                new Vector3(3f, 6f, -9f),
                new Vector3(3f, 1f, 1f),
                38f,
                0f);
            cameraRequestSystem.ProcessPendingRequests(world.EntityManager, cameraSystem, camera);

            Assert.AreEqual(new Vector3(3f, 6f, -9f), camera.transform.position);
            Assert.That(Vector3.Angle(camera.transform.forward, (new Vector3(3f, 1f, 1f) - camera.transform.position).normalized), Is.LessThan(0.01f));
            Assert.AreEqual(38f, camera.fieldOfView);
            Assert.IsFalse(camera.orthographic);
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void TacticalFollowPoseRequest_CanRestoreOrthographicCameraThroughRequestQueue()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowOrthoRestore");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            Camera camera = CreateCamera(Vector3.zero, Quaternion.identity);
            camera.orthographic = false;
            camera.orthographicSize = 3f;

            cameraRequestSystem.QueueUpdateTacticalFollowPose(
                world.EntityManager,
                new Vector3(8f, 12f, -14f),
                new Vector3(8f, 2f, 0f),
                45f,
                0f,
                true,
                11f);
            cameraRequestSystem.ProcessPendingRequests(world.EntityManager, cameraSystem, camera);

            Assert.AreEqual(new Vector3(8f, 12f, -14f), camera.transform.position);
            Assert.IsTrue(camera.orthographic);
            Assert.AreEqual(11f, camera.orthographicSize, 0.001f);
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void MatchIntroFirstPlay_StartsZoomedOutAndTransitionsToNormalThroughRequests()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.MatchIntro");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            var runtime = new RuntimeGameplayStateSystem();
            runtime.PlayRequested = true;

            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            var runtimeCameraSystem = new RtsSelectionRuntimeCameraSystemHelper();
            Camera camera = CreateCamera(new Vector3(0f, 10f, -10f), Quaternion.Euler(58f, 10f, 0f));
            camera.fieldOfView = 36f;

            var context = new RtsSelectionRuntimeCameraSystemHelper.Context(
                runtime,
                new RtsSelectionInputCompositionSystemHelper(),
                cameraSystem,
                cameraRequestSystem,
                camera,
                null,
                null,
                null,
                default,
                TryGetDefaultEntityManager,
                NullMatchIntroStateQuery.Instance,
                null,
                null,
                null,
                0.03f,
                20f,
                10f,
                45f,
                24f,
                100f,
                58f,
                64f,
                10f,
                10f,
                36f,
                32f,
                40f,
                82f,
                10f,
                24f,
                10f);

            Assert.IsTrue(runtimeCameraSystem.UpdateRuntimeCameraTick(context));

            Assert.IsTrue(cameraSystem.WasPlayRequested);
            Assert.IsTrue(cameraSystem.IsZoomTransitionActive);
            Assert.Greater(camera.transform.position.y, 24f, "First play should start slightly zoomed out before smoothing to normal.");
            Assert.Greater(camera.fieldOfView, 36f, "First play should start with a subtly wider FOV before smoothing to normal.");
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }

        bool TryGetDefaultEntityManager(out EntityManager entityManager)
        {
            entityManager = world.EntityManager;
            return world.IsCreated;
        }
    }

    [Test]
    public void MatchIntroFirstPlay_HoldsZoomedOutUntilIntroCompletes()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.MatchIntroDelayedSettle");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            var matchIntro = new FakeMatchIntroStateQuery(isGameplayInputLocked: true, isIntroComplete: false);

            var runtime = new RuntimeGameplayStateSystem();
            runtime.PlayRequested = true;

            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            var runtimeCameraSystem = new RtsSelectionRuntimeCameraSystemHelper();
            Camera camera = CreateCamera(new Vector3(0f, 10f, -10f), Quaternion.Euler(58f, 10f, 0f));
            camera.fieldOfView = 36f;

            var context = new RtsSelectionRuntimeCameraSystemHelper.Context(
                runtime,
                new RtsSelectionInputCompositionSystemHelper(),
                cameraSystem,
                cameraRequestSystem,
                camera,
                null,
                null,
                null,
                default,
                TryGetDefaultEntityManager,
                matchIntro,
                null,
                null,
                null,
                0.03f,
                20f,
                10f,
                45f,
                24f,
                100f,
                58f,
                64f,
                10f,
                10f,
                36f,
                32f,
                40f,
                82f,
                10f,
                24f,
                0.25f);

            Assert.IsTrue(runtimeCameraSystem.UpdateRuntimeCameraTick(context));
            Assert.IsTrue(cameraSystem.WasPlayRequested);
            Assert.IsFalse(cameraSystem.IsZoomTransitionActive, "Camera should hold the intro zoom while the shell intro is still locked.");
            Assert.Greater(camera.transform.position.y, 24f);
            Assert.Greater(camera.fieldOfView, 36f);

            matchIntro.Set(isGameplayInputLocked: false, isIntroComplete: true);

            Assert.IsTrue(runtimeCameraSystem.UpdateRuntimeCameraTick(context));
            Assert.IsTrue(cameraSystem.IsZoomTransitionActive, "Camera should begin settling only after the shell intro completes.");
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }

        bool TryGetDefaultEntityManager(out EntityManager entityManager)
        {
            entityManager = world.EntityManager;
            return world.IsCreated;
        }
    }

    private Camera CreateCamera(Vector3 position, Quaternion rotation)
    {
        var gameObject = new GameObject("RtsCameraSystemTests.Camera");
        _createdObjects.Add(gameObject);
        Camera camera = gameObject.AddComponent<Camera>();
        camera.transform.position = position;
        camera.transform.rotation = rotation;
        return camera;
    }

    private RtsCameraSystem CreateCameraSystem()
    {
        _cameraSystemWorld ??= new World("RtsCameraSystemTests.CameraSystem");
        return _cameraSystemWorld.GetOrCreateSystemManaged<RtsCameraSystem>();
    }

    private sealed class FakeMatchIntroStateQuery : IMatchIntroStateQuery
    {
        private bool isGameplayInputLocked;
        private bool isIntroComplete;

        public FakeMatchIntroStateQuery(bool isGameplayInputLocked, bool isIntroComplete)
        {
            Set(isGameplayInputLocked, isIntroComplete);
        }

        public void Set(bool isGameplayInputLocked, bool isIntroComplete)
        {
            this.isGameplayInputLocked = isGameplayInputLocked;
            this.isIntroComplete = isIntroComplete;
        }

        public bool IsGameplayInputLocked()
        {
            return isGameplayInputLocked;
        }

        public bool IsIntroComplete()
        {
            return isIntroComplete;
        }
    }
}
#endif
