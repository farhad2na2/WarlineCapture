using Game.UI.Contracts;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
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
            RunCase(nameof(PanCamera_ClampsViewportInsideGroundBoundary), test => test.PanCamera_ClampsViewportInsideGroundBoundary());
            RunCase(nameof(ResolveClampSafePerspectiveHeight_LeavesPanRoomAtMaxZoom), test => test.ResolveClampSafePerspectiveHeight_LeavesPanRoomAtMaxZoom());
            RunCase(nameof(ProcessPendingRequests_ClampsInitialCameraViewportInsideGrid), test => test.ProcessPendingRequests_ClampsInitialCameraViewportInsideGrid());
            RunCase(nameof(RuntimePanCamera_IgnoresDirectPanAndDragWhenTacticalFollowLocked), test => test.RuntimePanCamera_IgnoresDirectPanAndDragWhenTacticalFollowLocked());
            RunCase(nameof(RuntimeCameraTick_ReusesTacticalFollowQueriesWithoutManagedAllocation), test => test.RuntimeCameraTick_ReusesTacticalFollowQueriesWithoutManagedAllocation());
            RunCase(nameof(TacticalFollowQueryCache_RebindsWhenWorldChanges), test => test.TacticalFollowQueryCache_RebindsWhenWorldChanges());
            RunCase(nameof(RuntimePanCamera_IgnoresBuildModeDragPanWhenTacticalFollowLocked), test => test.RuntimePanCamera_IgnoresBuildModeDragPanWhenTacticalFollowLocked());
            RunCase(nameof(RuntimePanCamera_IgnoresFullscreenIsoDragPanWhenTacticalFollowLocked), test => test.RuntimePanCamera_IgnoresFullscreenIsoDragPanWhenTacticalFollowLocked());
            RunCase(nameof(RuntimeCameraTick_DoesNotApplyNormalCameraMotionWhileTacticalFollowPoseValid), test => test.RuntimeCameraTick_DoesNotApplyNormalCameraMotionWhileTacticalFollowPoseValid());
            RunCase(nameof(RuntimeCameraTick_DoesNotApplyNormalCameraMotionWhileTacticalFollowRestorePoseValid), test => test.RuntimeCameraTick_DoesNotApplyNormalCameraMotionWhileTacticalFollowRestorePoseValid());
            RunCase(nameof(RuntimeCameraTick_RemovesQueuedNormalCameraMotionWhileTacticalFollowPoseValid), test => test.RuntimeCameraTick_RemovesQueuedNormalCameraMotionWhileTacticalFollowPoseValid());
            RunCase(nameof(ApplyPerspectiveCameraModeInstant_ConfiguresPerspectiveCamera), test => test.ApplyPerspectiveCameraModeInstant_ConfiguresPerspectiveCamera());
            RunCase(nameof(MatchHudZoomButtonsStepBetweenMinDefaultAndMaxHeights), test => test.MatchHudZoomButtonsStepBetweenMinDefaultAndMaxHeights());
            RunCase(nameof(MoveCameraGroundCenterTo_PreservesHeightAndMovesGroundCenter), test => test.MoveCameraGroundCenterTo_PreservesHeightAndMovesGroundCenter());
            RunCase(nameof(UpdateFullscreenIsoZoom_ClampsTargets), test => test.UpdateFullscreenIsoZoom_ClampsTargets());
            RunCase(nameof(TacticalFollowPoseRequest_UpdatesCameraThroughRequestQueue), test => test.TacticalFollowPoseRequest_UpdatesCameraThroughRequestQueue());
            RunCase(nameof(TacticalFollowPoseRequest_SmoothlyApproachesTargetWithoutSnapping), test => test.TacticalFollowPoseRequest_SmoothlyApproachesTargetWithoutSnapping());
            RunCase(nameof(TacticalFollowPoseRequest_ResetVelocityPreventsCarryOverOvershoot), test => test.TacticalFollowPoseRequest_ResetVelocityPreventsCarryOverOvershoot());
            RunCase(nameof(TacticalFollowPoseRequest_DoesNotClampToRtsGroundBoundary), test => test.TacticalFollowPoseRequest_DoesNotClampToRtsGroundBoundary());
            RunCase(nameof(TacticalFollowPoseRequest_SuppressesNormalRequestsAndBoundaryClampWhilePoseValid), test => test.TacticalFollowPoseRequest_SuppressesNormalRequestsAndBoundaryClampWhilePoseValid());
            RunCase(nameof(TacticalFollowPoseRequest_UsesExplicitRestoreRotationInsteadOfLookAt), test => test.TacticalFollowPoseRequest_UsesExplicitRestoreRotationInsteadOfLookAt());
            RunCase(nameof(TacticalFollowPoseRequest_CanRestoreOrthographicCameraThroughRequestQueue), test => test.TacticalFollowPoseRequest_CanRestoreOrthographicCameraThroughRequestQueue());
            RunCase(nameof(RuntimeCameraTick_DoesNotAutoSettleManualZoomOutAfterIntroComplete), test => test.RuntimeCameraTick_DoesNotAutoSettleManualZoomOutAfterIntroComplete());
            RunCase(nameof(MatchIntroFirstPlay_StartsZoomedOutAndTransitionsToNormalThroughRequests), test => test.MatchIntroFirstPlay_StartsZoomedOutAndTransitionsToNormalThroughRequests());
            RunCase(nameof(MatchIntroFirstPlay_HoldsZoomedOutUntilIntroCompletes), test => test.MatchIntroFirstPlay_HoldsZoomedOutUntilIntroCompletes());
            Debug.Log("[RtsCameraFocusedValidation] result=Passed tests=31");
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
        InitialUnitsRuntimeState.ResetSession();
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
    public void PanCamera_ClampsViewportInsideGroundBoundary()
    {
        RtsCameraSystem cameraSystem = CreateCameraSystem();
        Camera camera = CreateCamera(new Vector3(50f, 10f, 50f), Quaternion.Euler(90f, 0f, 0f));
        camera.orthographic = true;
        camera.orthographicSize = 10f;
        camera.aspect = 1f;
        Rect boundary = new(0f, 0f, 100f, 100f);
        cameraSystem.SetGroundBoundary(boundary);

        bool moved = cameraSystem.PanCamera(camera, new Vector2(1000f, 1000f), 1f);

        Assert.IsTrue(moved);
        AssertCameraFootprintInside(cameraSystem, camera, boundary);
    }

    [Test]
    public void ResolveClampSafePerspectiveHeight_LeavesPanRoomAtMaxZoom()
    {
        RtsCameraSystem cameraSystem = CreateCameraSystem();
        Camera camera = CreateCamera(new Vector3(0f, 24f, -20f), Quaternion.Euler(58f, 10f, 0f));
        camera.fieldOfView = 36f;
        camera.aspect = 16f / 9f;
        Rect boundary = new(-80f, -80f, 160f, 160f);
        cameraSystem.SetGroundBoundary(boundary);

        float safeHeight = cameraSystem.ResolveClampSafePerspectiveHeight(
            camera,
            160f,
            24f,
            58f,
            10f,
            36f,
            0.88f);

        Assert.That(safeHeight, Is.LessThan(160f), "Max zoom should be lowered before it fights the RTS boundary clamp.");
        Assert.That(safeHeight, Is.GreaterThanOrEqualTo(24f));

        cameraSystem.ApplyPerspectiveCameraModeInstant(camera, safeHeight, 58f, 10f, 36f);
        cameraSystem.MoveCameraGroundCenterTo(camera, Vector3.zero);
        Vector3 beforePan = camera.transform.position;

        Assert.IsTrue(cameraSystem.PanCamera(camera, new Vector2(10f, 0f), 0.1f));
        Vector2 beforePanXZ = new(beforePan.x, beforePan.z);
        Vector2 afterPanXZ = new(camera.transform.position.x, camera.transform.position.z);

        Assert.That(Vector2.Distance(beforePanXZ, afterPanXZ), Is.GreaterThan(0.001f), "Safe max zoom must leave enough boundary room for drag pan.");
        AssertCameraFootprintInside(cameraSystem, camera, boundary);
    }

    [Test]
    public void ProcessPendingRequests_ClampsInitialCameraViewportInsideGrid()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.InitialGridClamp");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            Camera camera = CreateCamera(new Vector3(-50f, 10f, -50f), Quaternion.Euler(90f, 0f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.aspect = 1f;
            Entity gridEntity = world.EntityManager.CreateEntity(typeof(GridConfig));
            world.EntityManager.SetComponentData(gridEntity, new GridConfig
            {
                Width = 100,
                Height = 100,
                CellSize = 1f,
                Origin = float3.zero
            });

            cameraRequestSystem.ProcessPendingRequests(world.EntityManager, cameraSystem, camera);

            AssertCameraFootprintInside(cameraSystem, camera, new Rect(0f, 0f, 100f, 100f));
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void RuntimePanCamera_IgnoresDirectPanAndDragWhenTacticalFollowLocked()
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
            var context = CreateRuntimeCameraContext(runtime, new RtsSelectionInputCompositionSystemHelper(), cameraSystem, cameraRequestSystem, camera, TryGetDefaultEntityManager);

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
    public void RuntimeCameraTick_ReusesTacticalFollowQueriesWithoutManagedAllocation()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowQueryAllocation");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            var runtime = new RuntimeGameplayStateSystem { PlayRequested = true };
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            var runtimeCameraSystem = new RtsSelectionRuntimeCameraSystemHelper();
            Camera camera = CreateCamera(new Vector3(0f, 10f, -10f), Quaternion.Euler(45f, 0f, 0f));
            world.EntityManager.CreateEntity(typeof(TacticalFollowCameraModeComponent));
            Entity poseEntity = world.EntityManager.CreateEntity(typeof(TacticalFollowCameraPoseComponent));
            world.EntityManager.SetComponentData(poseEntity, new TacticalFollowCameraPoseComponent { Valid = 1 });
            var context = CreateRuntimeCameraContext(
                runtime,
                new RtsSelectionInputCompositionSystemHelper(),
                cameraSystem,
                cameraRequestSystem,
                camera,
                TryGetDefaultEntityManager);

            Assert.IsTrue(runtimeCameraSystem.UpdateRuntimeCameraTick(context));
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 300; i++)
                runtimeCameraSystem.UpdateRuntimeCameraTick(context);
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

            Assert.AreEqual(0L, allocatedBytes, "Warm tactical-follow camera reads must reuse world-bound ECS queries.");
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
    public void TacticalFollowQueryCache_RebindsWhenWorldChanges()
    {
        var cache = new TacticalFollowCameraStateQueryCache();
        var firstWorld = new World("RtsCameraSystemTests.TacticalFollowQueryCache.First");
        var secondWorld = new World("RtsCameraSystemTests.TacticalFollowQueryCache.Second");

        try
        {
            Entity firstModeEntity = firstWorld.EntityManager.CreateEntity(typeof(TacticalFollowCameraModeComponent));
            firstWorld.EntityManager.SetComponentData(firstModeEntity, new TacticalFollowCameraModeComponent
            {
                Enabled = 1,
                PanInputLocked = 1
            });
            Assert.IsTrue(cache.IsPanInputLocked(firstWorld.EntityManager));
            Assert.IsFalse(cache.HasValidPose(firstWorld.EntityManager));

            firstWorld.Dispose();

            secondWorld.EntityManager.CreateEntity(typeof(TacticalFollowCameraModeComponent));
            Entity secondPoseEntity = secondWorld.EntityManager.CreateEntity(typeof(TacticalFollowCameraPoseComponent));
            secondWorld.EntityManager.SetComponentData(
                secondPoseEntity,
                new TacticalFollowCameraPoseComponent { Valid = 1 });

            Assert.IsFalse(cache.IsPanInputLocked(secondWorld.EntityManager));
            Assert.IsTrue(cache.HasValidPose(secondWorld.EntityManager));
        }
        finally
        {
            if (firstWorld.IsCreated)
                firstWorld.Dispose();
            if (secondWorld.IsCreated)
                secondWorld.Dispose();
        }
    }

    [Test]
    public void RuntimePanCamera_IgnoresBuildModeDragPanWhenTacticalFollowLocked()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowBuildPanLock");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            var runtime = new RuntimeGameplayStateSystem { PlayRequested = true, BuildModeActive = true };
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            var runtimeCameraSystem = new RtsSelectionRuntimeCameraSystemHelper();
            Camera camera = CreateCamera(new Vector3(0f, 100f, -10f), Quaternion.Euler(64f, 10f, 0f));
            camera.fieldOfView = 32f;
            cameraSystem.WasPlayRequested = true;
            cameraSystem.WasBuildModeActive = true;
            Entity modeEntity = world.EntityManager.CreateEntity(typeof(TacticalFollowCameraModeComponent));
            world.EntityManager.SetComponentData(modeEntity, new TacticalFollowCameraModeComponent
            {
                Enabled = 1,
                PanInputLocked = 1
            });
            var context = CreateRuntimeCameraContext(runtime, new RtsSelectionInputCompositionSystemHelper(), cameraSystem, cameraRequestSystem, camera, TryGetDefaultEntityManager);

            Vector3 originalPosition = camera.transform.position;
            cameraSystem.SetDragging(true);

            Assert.IsFalse(runtimeCameraSystem.UpdateRuntimeCameraTick(context));

            Assert.IsFalse(cameraSystem.IsDragging);
            Assert.AreEqual(originalPosition, camera.transform.position);
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
    public void RuntimePanCamera_IgnoresFullscreenIsoDragPanWhenTacticalFollowLocked()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowFullscreenIsoPanLock");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            var runtime = new RuntimeGameplayStateSystem { PlayRequested = true, FullscreenMapIsoMode = true };
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            var runtimeCameraSystem = new RtsSelectionRuntimeCameraSystemHelper();
            Camera camera = CreateCamera(new Vector3(0f, 40f, -10f), Quaternion.Euler(82f, 10f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 24f;
            cameraSystem.FullscreenIsoTargetHeight = 40f;
            cameraSystem.FullscreenIsoTargetOrthographicSize = 24f;
            Entity modeEntity = world.EntityManager.CreateEntity(typeof(TacticalFollowCameraModeComponent));
            world.EntityManager.SetComponentData(modeEntity, new TacticalFollowCameraModeComponent
            {
                Enabled = 1,
                PanInputLocked = 1
            });
            var context = CreateRuntimeCameraContext(runtime, new RtsSelectionInputCompositionSystemHelper(), cameraSystem, cameraRequestSystem, camera, TryGetDefaultEntityManager);

            Vector3 originalPosition = camera.transform.position;
            cameraSystem.SetDragging(true);

            Assert.IsFalse(runtimeCameraSystem.UpdateRuntimeCameraTick(context));

            Assert.IsFalse(cameraSystem.IsDragging);
            Assert.AreEqual(originalPosition, camera.transform.position);
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
    public void RuntimeCameraTick_DoesNotApplyNormalCameraMotionWhileTacticalFollowPoseValid()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowOwnsCamera");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            var runtime = new RuntimeGameplayStateSystem { PlayRequested = true };
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            var runtimeCameraSystem = new RtsSelectionRuntimeCameraSystemHelper();
            Camera camera = CreateCamera(new Vector3(0f, 60f, -25f), Quaternion.Euler(58f, 10f, 0f));
            camera.fieldOfView = 58f;
            cameraSystem.SetSmoothFocusTarget(new Vector3(90f, 0f, 90f), resetVelocity: true);
            cameraSystem.BeginZoomTransition(false);
            CreateTacticalFollowPose(world.EntityManager, TacticalFollowCameraPoseSource.BaseTarget);
            var context = CreateRuntimeCameraContext(runtime, new RtsSelectionInputCompositionSystemHelper(), cameraSystem, cameraRequestSystem, camera, TryGetDefaultEntityManager);

            Vector3 originalPosition = camera.transform.position;
            Quaternion originalRotation = camera.transform.rotation;
            float originalFieldOfView = camera.fieldOfView;

            Assert.IsTrue(runtimeCameraSystem.UpdateRuntimeCameraTick(context));

            Assert.That(Vector3.Distance(originalPosition, camera.transform.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(originalRotation, camera.transform.rotation), Is.LessThan(0.0001f));
            Assert.That(Mathf.Abs(originalFieldOfView - camera.fieldOfView), Is.LessThan(0.0001f));
            Assert.IsFalse(cameraSystem.HasSmoothFocusTarget);
            Assert.IsFalse(cameraSystem.IsZoomTransitionActive);
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
    public void RuntimeCameraTick_DoesNotApplyNormalCameraMotionWhileTacticalFollowRestorePoseValid()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowRestoreOwnsCamera");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            var runtime = new RuntimeGameplayStateSystem { PlayRequested = true };
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            var runtimeCameraSystem = new RtsSelectionRuntimeCameraSystemHelper();
            Camera camera = CreateCamera(new Vector3(30f, 50f, -35f), Quaternion.Euler(55f, 8f, 0f));
            camera.fieldOfView = 54f;
            cameraSystem.SetSmoothFocusTarget(new Vector3(-90f, 0f, 90f), resetVelocity: true);
            cameraSystem.BeginZoomTransition(false);
            CreateTacticalFollowPose(world.EntityManager, TacticalFollowCameraPoseSource.RestoreDefault);
            var context = CreateRuntimeCameraContext(runtime, new RtsSelectionInputCompositionSystemHelper(), cameraSystem, cameraRequestSystem, camera, TryGetDefaultEntityManager);

            Vector3 originalPosition = camera.transform.position;
            Quaternion originalRotation = camera.transform.rotation;
            float originalFieldOfView = camera.fieldOfView;

            Assert.IsTrue(runtimeCameraSystem.UpdateRuntimeCameraTick(context));

            Assert.That(Vector3.Distance(originalPosition, camera.transform.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(originalRotation, camera.transform.rotation), Is.LessThan(0.0001f));
            Assert.That(Mathf.Abs(originalFieldOfView - camera.fieldOfView), Is.LessThan(0.0001f));
            Assert.IsFalse(cameraSystem.HasSmoothFocusTarget);
            Assert.IsFalse(cameraSystem.IsZoomTransitionActive);
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
    public void RuntimeCameraTick_RemovesQueuedNormalCameraMotionWhileTacticalFollowPoseValid()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowSuppressesQueuedCameraMotion");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            var runtime = new RuntimeGameplayStateSystem { PlayRequested = true };
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            var runtimeCameraSystem = new RtsSelectionRuntimeCameraSystemHelper();
            Camera camera = CreateCamera(new Vector3(0f, 60f, -25f), Quaternion.Euler(58f, 10f, 0f));
            EntityManager em = world.EntityManager;
            cameraRequestSystem.QueueMoveGroundCenterTo(em, new Vector3(250f, 0f, 250f));
            cameraRequestSystem.QueueSetSmoothFocusTarget(em, new Vector3(-140f, 0f, 90f), resetVelocity: true);
            cameraRequestSystem.QueueUpdatePerspectiveMode(em, 100f, 70f, 20f, 50f, 0.1f, completeTransitionOnArrive: false);
            cameraRequestSystem.QueuePan(em, new Vector2(80f, -20f), 1f);
            CreateTacticalFollowPose(em, TacticalFollowCameraPoseSource.BaseTarget);
            var context = CreateRuntimeCameraContext(runtime, new RtsSelectionInputCompositionSystemHelper(), cameraSystem, cameraRequestSystem, camera, TryGetDefaultEntityManager);

            Vector3 originalPosition = camera.transform.position;
            Quaternion originalRotation = camera.transform.rotation;
            float originalFieldOfView = camera.fieldOfView;

            Assert.IsTrue(runtimeCameraSystem.UpdateRuntimeCameraTick(context));

            Assert.That(Vector3.Distance(originalPosition, camera.transform.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(originalRotation, camera.transform.rotation), Is.LessThan(0.0001f));
            Assert.That(Mathf.Abs(originalFieldOfView - camera.fieldOfView), Is.LessThan(0.0001f));
            Assert.AreEqual(0, em.GetBuffer<RtsCameraRequestElement>(cameraRequestSystem.EnsureCameraEntity(em)).Length);
            Assert.IsFalse(cameraSystem.HasSmoothFocusTarget);
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
    public void MatchHudZoomButtonsStepBetweenMinDefaultAndMaxHeights()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        RtsCameraSystem cameraSystem = CreateCameraSystem();
        RtsCameraRequestSystem cameraRequestSystem = _cameraSystemWorld.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
        World.DefaultGameObjectInjectionWorld = _cameraSystemWorld;
        Camera camera = CreateCamera(new Vector3(0f, 24f, -20f), Quaternion.Euler(58f, 10f, 0f));
        camera.fieldOfView = 36f;
        var helper = new SelectionUiCameraSystemHelper(cameraSystem, cameraRequestSystem);

        try
        {
            helper.Init(null, camera);
            MatchHudZoomControlState state = helper.ReadZoomControlState();
            Assert.IsTrue(state.ZoomInEnabled, "Default zoom should allow stepping in.");
            Assert.IsTrue(state.ZoomOutEnabled, "Default zoom should allow stepping out.");

            Assert.IsTrue(helper.RequestZoomInLevel());
            Assert.That(Mathf.Abs(camera.transform.position.y - 10f), Is.GreaterThan(0.0001f), "Zoom-in button should start a smooth transition instead of snapping to min height.");
            state = helper.ReadZoomControlState();
            Assert.IsFalse(state.ZoomInEnabled, "Min zoom disables the zoom-in button.");
            Assert.IsTrue(state.ZoomOutEnabled, "Min zoom still allows returning toward default.");

            Assert.IsFalse(helper.RequestZoomInLevel(), "Already at min zoom should not queue another level change.");
            Assert.That(Mathf.Abs(camera.transform.position.y - 10f), Is.GreaterThan(0.0001f));

            Assert.IsTrue(helper.RequestZoomOutLevel());
            state = helper.ReadZoomControlState();
            Assert.IsTrue(state.ZoomInEnabled, "Default zoom re-enables zoom in.");
            Assert.IsTrue(state.ZoomOutEnabled, "Default zoom re-enables zoom out.");

            cameraSystem.BeginZoomTransition(false);
            Assert.IsTrue(helper.RequestZoomOutLevel());
            Assert.IsFalse(cameraSystem.IsZoomTransitionActive, "Match HUD zoom buttons must clear the normal camera zoom transition so zoom-out is not pulled back to default.");
            Assert.That(Mathf.Abs(camera.transform.position.y - 45f), Is.GreaterThan(0.0001f), "Zoom-out button should start a smooth transition instead of snapping to max height.");
            state = helper.ReadZoomControlState();
            Assert.IsTrue(state.ZoomInEnabled, "Max zoom still allows returning toward default.");
            Assert.IsFalse(state.ZoomOutEnabled, "Max zoom disables the zoom-out button.");

            Assert.IsTrue(helper.RequestZoomInLevel());
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
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
    public void TacticalFollowPoseRequest_SmoothlyApproachesTargetWithoutSnapping()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowSmoothPose");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            Vector3 startPosition = new(0f, 5f, 0f);
            Vector3 desiredPosition = new(18f, 9f, -24f);
            Vector3 lookAt = new(18f, 1.5f, -4f);
            Camera camera = CreateCamera(startPosition, Quaternion.identity);
            camera.fieldOfView = 60f;

            QueueAndProcess();

            float firstDistance = Vector3.Distance(camera.transform.position, desiredPosition);
            Assert.Greater(Vector3.Distance(startPosition, desiredPosition), firstDistance, "First smooth request should move toward the tactical follow pose.");
            Assert.Greater(firstDistance, 0.5f, "First smooth request must not snap directly to the tactical follow pose.");
            Assert.That(camera.fieldOfView, Is.GreaterThan(38f), "Field of view should ease toward the tactical follow value instead of snapping.");

            for (int i = 0; i < 180; i++)
                QueueAndProcess();

            Assert.That(Vector3.Distance(camera.transform.position, desiredPosition), Is.LessThan(0.1f));
            Assert.That(Vector3.Angle(camera.transform.forward, (lookAt - camera.transform.position).normalized), Is.LessThan(1f));
            Assert.That(camera.fieldOfView, Is.EqualTo(38f).Within(0.1f));

            void QueueAndProcess()
            {
                cameraRequestSystem.QueueUpdateTacticalFollowPose(
                    world.EntityManager,
                    desiredPosition,
                    lookAt,
                    38f,
                    0.12f);
                cameraRequestSystem.ProcessPendingRequests(world.EntityManager, cameraSystem, camera);
            }
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void TacticalFollowPoseRequest_ResetVelocityPreventsCarryOverOvershoot()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowResetVelocity");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            Camera camera = CreateCamera(new Vector3(0f, 5f, 0f), Quaternion.identity);
            Vector3 farPose = new(100f, 5f, 0f);
            Vector3 restorePose = new(0f, 5f, 0f);

            for (int i = 0; i < 12; i++)
            {
                cameraRequestSystem.QueueUpdateTacticalFollowPose(
                    world.EntityManager,
                    farPose,
                    new Vector3(100f, 1f, 8f),
                    38f,
                    0.35f);
                cameraRequestSystem.ProcessPendingRequests(world.EntityManager, cameraSystem, camera);
            }

            float beforeSwitchX = camera.transform.position.x;
            Assert.Greater(beforeSwitchX, 0f);

            cameraRequestSystem.QueueUpdateTacticalFollowPose(
                world.EntityManager,
                restorePose,
                new Vector3(0f, 1f, 8f),
                60f,
                0.35f,
                false,
                0f,
                true);
            cameraRequestSystem.ProcessPendingRequests(world.EntityManager, cameraSystem, camera);

            Assert.Less(
                camera.transform.position.x,
                beforeSwitchX,
                "Resetting tactical-follow velocity should move toward the new restore target immediately instead of carrying old velocity farther away.");
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void TacticalFollowPoseRequest_DoesNotClampToRtsGroundBoundary()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowNoGroundClamp");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            EntityManager em = world.EntityManager;
            Entity grid = em.CreateEntity(typeof(GridConfig));
            em.SetComponentData(grid, new GridConfig
            {
                Width = 4,
                Height = 4,
                CellSize = 1f,
                Origin = new float3(0f, 0f, 0f)
            });

            Camera camera = CreateCamera(new Vector3(0f, 10f, 0f), Quaternion.Euler(58f, 10f, 0f));
            Vector3 desiredPosition = new(-80f, 18f, -80f);
            Vector3 lookAt = new(-70f, 2f, -70f);

            cameraRequestSystem.QueueUpdateTacticalFollowPose(
                em,
                desiredPosition,
                lookAt,
                38f,
                0f,
                false,
                0f,
                true);
            cameraRequestSystem.ProcessPendingRequests(em, cameraSystem, camera);

            Assert.That(Vector3.Distance(camera.transform.position, desiredPosition), Is.LessThan(0.0001f));
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void TacticalFollowPoseRequest_SuppressesNormalRequestsAndBoundaryClampWhilePoseValid()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowRequestSuppression");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            EntityManager em = world.EntityManager;
            Entity grid = em.CreateEntity(typeof(GridConfig));
            em.SetComponentData(grid, new GridConfig
            {
                Width = 4,
                Height = 4,
                CellSize = 1f,
                Origin = new float3(0f, 0f, 0f)
            });
            CreateTacticalFollowPose(em, TacticalFollowCameraPoseSource.RestoreDefault);

            Camera camera = CreateCamera(new Vector3(-60f, 18f, -60f), Quaternion.Euler(30f, 20f, 0f));
            camera.fieldOfView = 42f;
            Vector3 originalPosition = camera.transform.position;
            Quaternion originalRotation = camera.transform.rotation;
            float originalFieldOfView = camera.fieldOfView;
            cameraSystem.SetDragging(true);
            cameraRequestSystem.QueueClearDragging(em);
            cameraRequestSystem.QueueMoveGroundCenterTo(em, new Vector3(2f, 0f, 2f));
            cameraRequestSystem.QueueSetSmoothFocusTarget(em, new Vector3(1f, 0f, 1f), resetVelocity: true);
            cameraRequestSystem.QueueUpdatePerspectiveMode(em, 100f, 70f, 20f, 50f, 0.1f, completeTransitionOnArrive: false);
            cameraRequestSystem.QueuePan(em, new Vector2(80f, -20f), 1f);

            cameraRequestSystem.ProcessPendingRequests(em, cameraSystem, camera);

            Assert.That(Vector3.Distance(camera.transform.position, originalPosition), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(camera.transform.rotation, originalRotation), Is.LessThan(0.0001f));
            Assert.That(Mathf.Abs(camera.fieldOfView - originalFieldOfView), Is.LessThan(0.0001f));
            Assert.IsFalse(cameraSystem.IsDragging);
            Assert.IsFalse(cameraSystem.HasSmoothFocusTarget);
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void TacticalFollowPoseRequest_UsesExplicitRestoreRotationInsteadOfLookAt()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.TacticalFollowRestoreRotation");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            EntityManager em = world.EntityManager;
            Camera camera = CreateCamera(new Vector3(30f, 8f, -30f), Quaternion.Euler(25f, -80f, 0f));
            Vector3 restorePosition = new(10f, 70f, -15f);
            Quaternion restoreRotation = Quaternion.Euler(62f, 12f, 0f);
            Vector3 misleadingLookAt = restorePosition + Vector3.right * 100f;

            cameraRequestSystem.QueueUpdateTacticalFollowPose(
                em,
                restorePosition,
                misleadingLookAt,
                55f,
                0f,
                false,
                0f,
                true,
                restoreRotation);
            cameraRequestSystem.ProcessPendingRequests(em, cameraSystem, camera);

            Assert.That(Vector3.Distance(camera.transform.position, restorePosition), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(camera.transform.rotation, restoreRotation), Is.LessThan(0.0001f));
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void RuntimeCameraTick_DoesNotAutoSettleManualZoomOutAfterIntroComplete()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("RtsCameraSystemTests.ManualZoomOutPan");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            var runtime = new RuntimeGameplayStateSystem { PlayRequested = true };
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem cameraRequestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            var runtimeCameraSystem = new RtsSelectionRuntimeCameraSystemHelper();
            Camera camera = CreateCamera(new Vector3(0f, 45f, -20f), Quaternion.Euler(58f, 10f, 0f));
            camera.fieldOfView = 36f;
            cameraSystem.WasPlayRequested = true;
            cameraSystem.MatchIntroZoomSettlePending = false;
            cameraSystem.IsZoomTransitionActive = false;

            var context = CreateRuntimeCameraContext(
                runtime,
                new RtsSelectionInputCompositionSystemHelper(),
                cameraSystem,
                cameraRequestSystem,
                camera,
                TryGetDefaultEntityManager);

            Assert.IsTrue(runtimeCameraSystem.UpdateRuntimeCameraTick(context));
            Assert.IsFalse(cameraSystem.IsZoomTransitionActive, "Manual max zoom-out after intro must not be mistaken for intro zoom-out settle.");
            Assert.IsFalse(cameraSystem.MatchIntroZoomSettlePending);

            Vector3 beforePan = camera.transform.position;
            runtimeCameraSystem.SetCameraDragging(context, true);
            runtimeCameraSystem.PanCamera(context, new Vector2(10f, 0f));

            Assert.IsTrue(cameraSystem.IsDragging);
            Assert.That(Vector3.Distance(beforePan, camera.transform.position), Is.GreaterThan(0.001f), "Manual max zoom-out must remain draggable after the runtime camera tick.");
            Assert.IsFalse(cameraSystem.IsZoomTransitionActive);
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
            Assert.IsFalse(cameraSystem.MatchIntroZoomSettlePending);
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
            Assert.IsTrue(cameraSystem.MatchIntroZoomSettlePending, "Only the actual delayed match intro should arm the zoom settle.");
            Assert.Greater(camera.transform.position.y, 24f);
            Assert.Greater(camera.fieldOfView, 36f);

            matchIntro.Set(isGameplayInputLocked: false, isIntroComplete: true);

            Assert.IsTrue(runtimeCameraSystem.UpdateRuntimeCameraTick(context));
            Assert.IsTrue(cameraSystem.IsZoomTransitionActive, "Camera should begin settling only after the shell intro completes.");
            Assert.IsFalse(cameraSystem.MatchIntroZoomSettlePending);
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

    private static void CreateTacticalFollowPose(EntityManager em, TacticalFollowCameraPoseSource source)
    {
        Entity poseEntity = em.CreateEntity(typeof(TacticalFollowCameraPoseComponent));
        em.SetComponentData(poseEntity, new TacticalFollowCameraPoseComponent
        {
            Valid = 1,
            Source = source,
            DesiredPosition = new float3(10f, 15f, -10f),
            DesiredRotation = quaternion.identity,
            LookAt = new float3(10f, 1f, 0f),
            FieldOfView = 38f,
            PositionDampingSeconds = 0.32f,
            RotationDampingSeconds = 0.22f
        });
    }

    private static RtsSelectionRuntimeCameraSystemHelper.Context CreateRuntimeCameraContext(
        RuntimeGameplayStateSystem runtime,
        RtsSelectionInputCompositionSystemHelper input,
        RtsCameraSystem cameraSystem,
        RtsCameraRequestSystem cameraRequestSystem,
        Camera camera,
        RtsSelectionRuntimeCameraSystemHelper.TryGetEntityManagerAction tryGetDefaultEntityManager)
    {
        return new RtsSelectionRuntimeCameraSystemHelper.Context(
            runtime,
            input,
            cameraSystem,
            cameraRequestSystem,
            camera,
            null,
            null,
            null,
            default,
            tryGetDefaultEntityManager,
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
    }

    private static void AssertCameraFootprintInside(RtsCameraSystem cameraSystem, Camera camera, Rect boundary)
    {
        Assert.IsTrue(cameraSystem.TryGetCameraGroundBounds(camera, out Rect footprint));
        Assert.That(footprint.xMin, Is.GreaterThanOrEqualTo(boundary.xMin - 0.001f));
        Assert.That(footprint.xMax, Is.LessThanOrEqualTo(boundary.xMax + 0.001f));
        Assert.That(footprint.yMin, Is.GreaterThanOrEqualTo(boundary.yMin - 0.001f));
        Assert.That(footprint.yMax, Is.LessThanOrEqualTo(boundary.yMax + 0.001f));
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
