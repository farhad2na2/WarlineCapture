using Game.Tactical.Contracts;
using Game.Components;
using Game.Runtime;

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public sealed class TacticalFollowCameraModeCommandSystemHelperTests
{
    private World _world;
    private EntityManager _em;
    private TacticalFollowCameraModeSystemHelper _system;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.ToggleWithoutSelectionRejectsAndPublishesDisabledReadModel());
            passed++;
            RunCase(test => test.SelectedUnitWithoutPendingRequestPublishesEnabledReadModel());
            passed++;
            RunCase(test => test.SelectedUnitReadModelDoesNotReadLocalTransformWhileSelectionMarkerJobWrites());
            passed++;
            RunCase(test => test.ToggleWithSelectedUnitEntersFollowModeAndLocksPanData());
            passed++;
            RunCase(test => test.ToggleWhileActiveExitsFollowModeAndClearsPanLockData());
            passed++;
            RunCase(test => test.ActiveFollowTargetLossExitsAndPublishesTargetLostFeedback());
            passed++;
            RunCase(test => test.FocusedOwnedUnitIsPreferredAsBaseTarget());
            passed++;
            RunCase(test => test.FocusedOwnedUnitUsesLocalTransformBeforePortraitPose());
            passed++;
            RunCase(test => test.SelectedUnitPublishesTargetCenterAndForward());
            passed++;
            RunCase(test => test.SelectedGroupPublishesCentroidAndRadius());
            passed++;
            RunCase(test => test.SelectedUnitPublishesFollowPoseBehindTarget());
            passed++;
            RunCase(test => test.SelectedLargeUnitUsesRenderBoundsForFollowDistanceAndHeight());
            passed++;
            RunCase(test => test.SelectedLargeUnitPrefersSelectionHitboxOverSafetyPaddedRenderBounds());
            passed++;
            RunCase(test => test.SelectedLargeFootprintUnitFramesWithoutRenderBounds());
            passed++;
            RunCase(test => test.ToggleExitPublishesRestorePoseWhenRestoreWasCaptured());
            passed++;
            RunCase(test => test.SelectedBuildingPublishesBuildingTargetAndPoseWhenNoUnitsSelected());
            passed++;
            RunCase(test => test.SelectedUnitTakesPriorityOverSelectedBuildingTarget());
            passed++;
            RunCase(test => test.SelectedUnitPriorityDocumentsMixedUnitBuildingRule());
            passed++;
            RunCase(test => test.SelectionChangeWhileActiveKeepsOriginalBaseTarget());
            passed++;
            RunCase(test => test.SelectedBuildingClickWhileActiveKeepsOriginalUnitTargetAndExitEnabled());
            passed++;
            RunCase(test => test.DestroyedGroupMemberFallsBackToRemainingSelectedUnit());
            passed++;
            RunCase(test => test.PassengerSelectedUnitFallsBackToSelectedBuildingTarget());
            passed++;
            RunCase(test => test.BuildingFollowPoseClampsHeightAndDistanceAroundBounds());
            passed++;
            RunCase(test => test.MatchingGroundMissileBecomesTemporaryFollowTarget());
            passed++;
            RunCase(test => test.MatchingAirMissileBecomesTemporaryFollowTarget());
            passed++;
            RunCase(test => test.MissileTemporaryFollowUsesForwardLookAheadFraming());
            passed++;
            RunCase(test => test.ActiveTemporaryMissileDoesNotJitterToLaterMissile());
            passed++;
            RunCase(test => test.GroundMissileImpactHoldsExplosionThenReturnsToBaseTarget());
            passed++;
            RunCase(test => test.AirMissileImpactHoldsExplosionThenReturnsToBaseTarget());
            passed++;
            RunCase(test => test.TemporaryMissileDespawnWithoutImpactHoldsLastPoseThenReturnsToBaseTarget());
            passed++;
            RunCase(test => test.BaseTargetLossDuringMissileFollowFinishesMissileThenExitsSafely());
            passed++;
            RunCase(test => test.ProductionGroundLauncherProjectileIsAdoptedDuringFollowMode());
            passed++;
            RunCase(test => test.ProductionGroundLauncherProjectileImpactReturnsToBaseTarget());
            passed++;
            RunCase(test => test.ProductionAirLauncherProjectileIsAdoptedDuringFollowMode());
            passed++;
            RunCase(test => test.ProductionAirLauncherProjectileImpactReturnsToBaseTarget());
            passed++;
            RunCase(test => test.FollowedAirUnitAttackVfxCreatesImpactCutawayThenReturns());
            passed++;
            RunCase(test => test.UnfollowedAirUnitAttackVfxDoesNotCreateImpactCutaway());
            passed++;
            UnityEngine.Debug.Log($"[TacticalFollowCameraModeCommandValidation] result=Passed tests={passed}");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError($"[TacticalFollowCameraModeCommandValidation] result=Failed passed={passed}");
            ValidationExit.Failed();
        }
    }

    private static void RunCase(Action<TacticalFollowCameraModeCommandSystemHelperTests> testCase)
    {
        var tests = new TacticalFollowCameraModeCommandSystemHelperTests();
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
        _world = new World(nameof(TacticalFollowCameraModeCommandSystemHelperTests));
        _em = _world.EntityManager;
        _system = new TacticalFollowCameraModeSystemHelper();
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void ToggleWithoutSelectionRejectsAndPublishesDisabledReadModel()
    {
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);

        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.AreEqual(0, GetRequestBuffer().Length);
        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(0, mode.Enabled);
        Assert.AreEqual(0, mode.PanInputLocked);
        Assert.AreEqual(0, mode.HasBaseTarget);
        Assert.IsTrue(_system.TryReadUiReadModel(_em, out TacticalFollowCameraUiReadModelComponent readModel));
        Assert.AreEqual(1, readModel.Visible);
        Assert.AreEqual(0, readModel.Enabled);
        Assert.AreEqual(0, readModel.Selected);
        Assert.AreEqual((int)TacticalCommandReasonCode.NoSelection, readModel.ReasonCode);
        Assert.AreEqual((int)TacticalFollowCameraFeedbackCode.None, readModel.FeedbackCode);
        Assert.AreEqual(0, readModel.FeedbackSequence);
    }

    [Test]
    public void SelectedUnitWithoutPendingRequestPublishesEnabledReadModel()
    {
        CreateSelectedUnit(new float3(4f, 0f, 6f), quaternion.identity);

        Assert.IsFalse(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.TryReadUiReadModel(_em, out TacticalFollowCameraUiReadModelComponent readModel));
        Assert.AreEqual(1, readModel.Visible);
        Assert.AreEqual(1, readModel.Enabled);
        Assert.AreEqual(0, readModel.Selected);
        Assert.AreEqual((int)TacticalCommandReasonCode.None, readModel.ReasonCode);
    }

    [Test]
    public void SelectedUnitReadModelDoesNotReadLocalTransformWhileSelectionMarkerJobWrites()
    {
        Entity selected = CreateSelectedUnit(new float3(4f, 0f, 6f), quaternion.identity);
        Entity outline = _em.CreateEntity(
            typeof(LocalTransform),
            typeof(SelectionObjectOutlineTag),
            typeof(SelectionMarkerOwner),
            typeof(SelectionObjectOutlineVisibleScale));
        _em.SetComponentData(outline, LocalTransform.FromPosition(float3.zero));
        _em.SetComponentData(outline, new SelectionMarkerOwner { Value = selected });
        _em.SetComponentData(outline, new SelectionObjectOutlineVisibleScale { Value = 1f });
        SystemHandle markerVisibility = _world.CreateSystem<SelectionMarkerVisibilitySystem>();
        markerVisibility.Update(_world.Unmanaged);

        try
        {
            Assert.DoesNotThrow(() => _system.ProcessPendingRequests(_em));
            Assert.IsTrue(_system.TryReadUiReadModel(_em, out TacticalFollowCameraUiReadModelComponent readModel));
            Assert.AreEqual(1, readModel.Visible);
            Assert.AreEqual(1, readModel.Enabled);
        }
        finally
        {
            _em.CompleteAllTrackedJobs();
        }
    }

    [Test]
    public void ToggleWithSelectedUnitEntersFollowModeAndLocksPanData()
    {
        Entity selected = CreateSelectedUnit(new float3(4f, 0f, 6f), quaternion.identity);
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);

        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.Enabled);
        Assert.AreEqual(1, mode.PanInputLocked);
        Assert.AreEqual(1, mode.HasBaseTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.Unit, mode.BaseTargetKind);
        Assert.AreEqual(selected, mode.BaseTargetEntity);
        Assert.IsTrue(_system.TryReadUiReadModel(_em, out TacticalFollowCameraUiReadModelComponent readModel));
        Assert.AreEqual(1, readModel.Enabled);
        Assert.AreEqual(1, readModel.Selected);
        Assert.AreEqual((int)TacticalCommandReasonCode.None, readModel.ReasonCode);
        Assert.AreEqual((int)TacticalFollowCameraFeedbackCode.EnteredFollowMode, readModel.FeedbackCode);
        Assert.AreEqual(1, readModel.FeedbackSequence);
    }

    [Test]
    public void ToggleWhileActiveExitsFollowModeAndClearsPanLockData()
    {
        CreateSelectedUnit(new float3(4f, 0f, 6f), quaternion.identity);
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);

        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(0, mode.Enabled);
        Assert.AreEqual(0, mode.PanInputLocked);
        Assert.AreEqual(0, mode.HasBaseTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.None, mode.BaseTargetKind);
        Assert.AreEqual(Entity.Null, mode.BaseTargetEntity);
        Assert.IsTrue(_system.TryReadUiReadModel(_em, out TacticalFollowCameraUiReadModelComponent readModel));
        Assert.AreEqual(1, readModel.Enabled);
        Assert.AreEqual(0, readModel.Selected);
        Assert.AreEqual((int)TacticalFollowCameraFeedbackCode.ExitedFollowMode, readModel.FeedbackCode);
        Assert.AreEqual(2, readModel.FeedbackSequence);
    }

    [Test]
    public void ActiveFollowTargetLossExitsAndPublishesTargetLostFeedback()
    {
        Entity selected = CreateSelectedUnit(new float3(4f, 0f, 6f), quaternion.identity);
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        _em.DestroyEntity(selected);

        Assert.IsFalse(_system.RefreshActiveTargetAndPose(_em));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(0, mode.Enabled);
        Assert.AreEqual(0, mode.PanInputLocked);
        Assert.AreEqual(0, mode.HasBaseTarget);
        Assert.IsTrue(_system.TryReadUiReadModel(_em, out TacticalFollowCameraUiReadModelComponent readModel));
        Assert.AreEqual(0, readModel.Enabled);
        Assert.AreEqual(0, readModel.Selected);
        Assert.AreEqual((int)TacticalCommandReasonCode.NoSelection, readModel.ReasonCode);
        Assert.AreEqual((int)TacticalFollowCameraFeedbackCode.TargetLost, readModel.FeedbackCode);
        Assert.AreEqual(2, readModel.FeedbackSequence);
    }

    [Test]
    public void FocusedOwnedUnitIsPreferredAsBaseTarget()
    {
        CreateSelectedUnit(new float3(0f, 0f, 0f), quaternion.identity);
        Entity focused = CreateSelectedUnit(new float3(8f, 0f, 2f), quaternion.identity);
        Entity readModelEntity = _em.CreateEntity(typeof(FocusedUnitUiReadModelComponent));
        _em.SetComponentData(readModelEntity, new FocusedUnitUiReadModelComponent
        {
            FocusedUnit = focused,
            HasFocusedUnit = 1,
            OwnedByPlayer = 1
        });
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);

        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.Enabled);
        Assert.AreEqual(TacticalFollowCameraTargetKind.Unit, mode.BaseTargetKind);
        Assert.AreEqual(focused, mode.BaseTargetEntity);
    }

    [Test]
    public void FocusedOwnedUnitUsesLocalTransformBeforePortraitPose()
    {
        Entity focused = CreateSelectedUnit(new float3(8f, 0f, 2f), quaternion.RotateY(math.radians(90f)));
        Entity readModelEntity = _em.CreateEntity(typeof(FocusedUnitUiReadModelComponent));
        _em.SetComponentData(readModelEntity, new FocusedUnitUiReadModelComponent
        {
            FocusedUnit = focused,
            HasFocusedUnit = 1,
            OwnedByPlayer = 1,
            HasPortraitPose = 1,
            PortraitWorldPosition = new float3(80f, 10f, 80f),
            PortraitForward = new float3(0f, 0f, 1f)
        });
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);

        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(focused, target.TargetEntity);
        Assert.AreEqual(new float3(8f, 0f, 2f), target.Center);
        Assert.That(math.distance(new float3(1f, 0f, 0f), target.ForwardHint), Is.LessThan(0.001f));
    }

    [Test]
    public void SelectedUnitPublishesTargetCenterAndForward()
    {
        Entity selected = CreateSelectedUnit(new float3(4f, 0f, 6f), quaternion.RotateY(math.radians(90f)));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);

        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(TacticalFollowCameraTargetKind.Unit, target.TargetKind);
        Assert.AreEqual(selected, target.TargetEntity);
        Assert.AreEqual(new float3(4f, 0f, 6f), target.Center);
        Assert.That(math.distance(new float3(1f, 0f, 0f), target.ForwardHint), Is.LessThan(0.001f));
        Assert.Greater(target.BoundsRadius, 0f);
        Assert.Greater(target.DesiredDistance, target.BoundsRadius);
        Assert.Greater(target.DesiredHeight, 0f);
    }

    [Test]
    public void SelectedGroupPublishesCentroidAndRadius()
    {
        CreateSelectedUnit(new float3(0f, 0f, 0f), quaternion.identity);
        CreateSelectedUnit(new float3(10f, 0f, 0f), quaternion.identity);
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);

        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(TacticalFollowCameraTargetKind.UnitGroup, mode.BaseTargetKind);
        Assert.AreEqual(Entity.Null, mode.BaseTargetEntity);
        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(TacticalFollowCameraTargetKind.UnitGroup, target.TargetKind);
        Assert.AreEqual(Entity.Null, target.TargetEntity);
        Assert.AreEqual(new float3(5f, 0f, 0f), target.Center);
        Assert.GreaterOrEqual(target.BoundsRadius, 7f);
        Assert.Greater(target.DesiredDistance, target.BoundsRadius);
        Assert.Greater(target.DesiredHeight, 0f);
    }

    [Test]
    public void SelectedUnitPublishesFollowPoseBehindTarget()
    {
        CreateSelectedUnit(new float3(4f, 0f, 6f), quaternion.identity);
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);

        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(1, pose.Valid);
        Assert.AreEqual(TacticalFollowCameraPoseSource.BaseTarget, pose.Source);
        Assert.That(pose.DesiredPosition.z, Is.LessThan(6f));
        Assert.That(pose.DesiredPosition.y, Is.GreaterThan(0f));
        Assert.That(pose.LookAt.y, Is.GreaterThan(0f));
        Assert.AreEqual(0, pose.Orthographic);
        Assert.Greater(pose.FieldOfView, 1f);
        Assert.Greater(pose.PositionDampingSeconds, 0f);
    }

    [Test]
    public void SelectedLargeUnitUsesRenderBoundsForFollowDistanceAndHeight()
    {
        Entity selected = CreateSelectedUnit(new float3(10f, 0f, 20f), quaternion.identity);
        Entity renderChild = _em.CreateEntity(typeof(Parent), typeof(WorldRenderBounds));
        _em.SetComponentData(renderChild, new Parent { Value = selected });
        _em.SetComponentData(renderChild, new WorldRenderBounds
        {
            Value = new AABB
            {
                Center = new float3(10f, 3f, 22f),
                Extents = new float3(8f, 3f, 14f)
            }
        });
        DynamicBuffer<Child> children = _em.AddBuffer<Child>(selected);
        children.Add(new Child { Value = renderChild });
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);

        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.GreaterOrEqual(target.BoundsRadius, 16f);
        Assert.GreaterOrEqual(target.DesiredDistance, 49f);
        Assert.GreaterOrEqual(target.DesiredHeight, 18f);
        Assert.AreEqual(22f, target.Center.z, 0.001f);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.That(pose.DesiredPosition.z, Is.LessThan(-20f));
        Assert.That(pose.DesiredPosition.y, Is.GreaterThan(18f));
    }

    [Test]
    public void SelectedLargeUnitPrefersSelectionHitboxOverSafetyPaddedRenderBounds()
    {
        Entity selected = CreateSelectedUnit(new float3(10f, 0f, 20f), quaternion.identity);
        _em.AddComponentData(selected, new UnitSelectionHitbox
        {
            Center = new float3(0f, 2f, 1f),
            Extents = new float3(5f, 2f, 11f)
        });
        Entity renderChild = _em.CreateEntity(typeof(Parent), typeof(WorldRenderBounds));
        _em.SetComponentData(renderChild, new Parent { Value = selected });
        _em.SetComponentData(renderChild, new WorldRenderBounds
        {
            Value = new AABB
            {
                Center = new float3(10f, 32f, 20f),
                Extents = new float3(64f, 64f, 64f)
            }
        });
        DynamicBuffer<Child> children = _em.AddBuffer<Child>(selected);
        children.Add(new Child { Value = renderChild });
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);

        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.Greater(target.BoundsRadius, 11f);
        Assert.Less(target.BoundsRadius, 20f);
        Assert.Greater(target.DesiredDistance, 36f);
        Assert.Less(target.DesiredDistance, 70f);
        Assert.AreEqual(21f, target.Center.z, 0.001f);
    }

    [Test]
    public void SelectedLargeFootprintUnitFramesWithoutRenderBounds()
    {
        Entity selected = CreateSelectedUnit(new float3(0f, 0f, 0f), quaternion.identity);
        _em.AddComponentData(selected, new UnitFootprint { Size = new int2(17, 21) });
        Entity grid = _em.CreateEntity(typeof(GridConfig));
        _em.SetComponentData(grid, new GridConfig
        {
            Width = 200,
            Height = 200,
            CellSize = 1f,
            Origin = float3.zero
        });
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);

        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.GreaterOrEqual(target.BoundsRadius, 13f);
        Assert.Greater(target.DesiredDistance, 40f);
        Assert.Greater(target.DesiredHeight, 15f);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.That(pose.DesiredPosition.z, Is.LessThan(-35f));
    }

    [Test]
    public void ToggleExitPublishesRestorePoseWhenRestoreWasCaptured()
    {
        GameObject cameraObject = new GameObject("TacticalFollowRestoreCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(20f, 14f, -30f);
            camera.transform.rotation = Quaternion.Euler(35f, 20f, 0f);
            camera.orthographic = true;
            camera.fieldOfView = 46f;
            camera.orthographicSize = 9f;
            CreateSelectedUnit(new float3(4f, 0f, 6f), quaternion.identity);
            QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
            Assert.IsTrue(_system.ProcessPendingRequests(_em, camera));
            QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);

            Assert.IsTrue(_system.ProcessPendingRequests(_em, camera));

            Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
            Assert.AreEqual(0, mode.Enabled);
            Assert.AreEqual(0, mode.PanInputLocked);
            Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
            Assert.AreEqual(TacticalFollowCameraPoseSource.RestoreDefault, pose.Source);
            Assert.That(math.distance(new float3(20f, 14f, -30f), pose.DesiredPosition), Is.LessThan(0.001f));
            Assert.AreEqual(1, pose.Orthographic);
            Assert.AreEqual(46f, pose.FieldOfView, 0.001f);
            Assert.AreEqual(9f, pose.OrthographicSize, 0.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void SelectedBuildingPublishesBuildingTargetAndPoseWhenNoUnitsSelected()
    {
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        var context = new TacticalFollowCameraModeSystemHelper.Context(
            (out Vector3 worldPosition, out float boundsRadius) =>
            {
                worldPosition = new Vector3(18f, 0f, 24f);
                boundsRadius = 7f;
                return true;
            });

        Assert.IsTrue(_system.ProcessPendingRequests(_em, null, context));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.Enabled);
        Assert.AreEqual(TacticalFollowCameraTargetKind.Building, mode.BaseTargetKind);
        Assert.AreEqual(Entity.Null, mode.BaseTargetEntity);
        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(TacticalFollowCameraTargetKind.Building, target.TargetKind);
        Assert.AreEqual(new float3(18f, 0f, 24f), target.Center);
        Assert.GreaterOrEqual(target.BoundsRadius, 7f);
        Assert.Greater(target.DesiredDistance, target.BoundsRadius);
        Assert.Greater(target.DesiredHeight, target.BoundsRadius);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.BaseTarget, pose.Source);
        Assert.That(pose.DesiredPosition.y, Is.GreaterThan(target.LookAt.y));
        Assert.AreEqual(0, pose.Orthographic);
    }

    [Test]
    public void SelectedUnitTakesPriorityOverSelectedBuildingTarget()
    {
        Entity selected = CreateSelectedUnit(new float3(4f, 0f, 6f), quaternion.identity);
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        var context = new TacticalFollowCameraModeSystemHelper.Context(
            (out Vector3 worldPosition, out float boundsRadius) =>
            {
                worldPosition = new Vector3(18f, 0f, 24f);
                boundsRadius = 7f;
                return true;
            });

        Assert.IsTrue(_system.ProcessPendingRequests(_em, null, context));

        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(TacticalFollowCameraTargetKind.Unit, target.TargetKind);
        Assert.AreEqual(selected, target.TargetEntity);
    }

    [Test]
    public void SelectedUnitPriorityDocumentsMixedUnitBuildingRule()
    {
        Entity selected = CreateSelectedUnit(new float3(6f, 0f, 9f), quaternion.identity);
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        var context = new TacticalFollowCameraModeSystemHelper.Context(
            (out Vector3 worldPosition, out float boundsRadius) =>
            {
                worldPosition = new Vector3(18f, 0f, 24f);
                boundsRadius = 7f;
                return true;
            });

        Assert.IsTrue(_system.ProcessPendingRequests(_em, null, context));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(TacticalFollowCameraTargetKind.Unit, mode.BaseTargetKind);
        Assert.AreEqual(selected, mode.BaseTargetEntity);
        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(TacticalFollowCameraTargetKind.Unit, target.TargetKind);
        Assert.AreEqual(selected, target.TargetEntity);
    }

    [Test]
    public void SelectionChangeWhileActiveKeepsOriginalBaseTarget()
    {
        Entity first = CreateSelectedUnit(new float3(4f, 0f, 6f), quaternion.identity);
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        Entity second = CreateSelectedUnit(new float3(14f, 0f, 18f), quaternion.RotateY(math.radians(90f)));
        _em.RemoveComponent<SelectedUnitTag>(first);

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(TacticalFollowCameraTargetKind.Unit, mode.BaseTargetKind);
        Assert.AreEqual(first, mode.BaseTargetEntity);
        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(first, target.TargetEntity);
        Assert.AreEqual(new float3(4f, 0f, 6f), target.Center);
        Assert.AreNotEqual(second, target.TargetEntity);
    }

    [Test]
    public void SelectedBuildingClickWhileActiveKeepsOriginalUnitTargetAndExitEnabled()
    {
        Entity soldier = CreateSelectedUnit(new float3(4f, 0f, 6f), quaternion.identity);
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        _em.RemoveComponent<SelectedUnitTag>(soldier);
        var airportContext = new TacticalFollowCameraModeSystemHelper.Context(
            (out Vector3 worldPosition, out float boundsRadius) =>
            {
                worldPosition = new Vector3(80f, 0f, 90f);
                boundsRadius = 22f;
                return true;
            });

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, airportContext));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.Enabled);
        Assert.AreEqual(TacticalFollowCameraTargetKind.Unit, mode.BaseTargetKind);
        Assert.AreEqual(soldier, mode.BaseTargetEntity);
        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(TacticalFollowCameraTargetKind.Unit, target.TargetKind);
        Assert.AreEqual(soldier, target.TargetEntity);
        Assert.AreEqual(new float3(4f, 0f, 6f), target.Center);
        Assert.IsTrue(_system.TryReadUiReadModel(_em, out TacticalFollowCameraUiReadModelComponent model));
        Assert.AreEqual(1, model.Selected);
        Assert.AreEqual(1, model.Enabled);
    }

    [Test]
    public void DestroyedGroupMemberFallsBackToRemainingSelectedUnit()
    {
        Entity first = CreateSelectedUnit(new float3(0f, 0f, 0f), quaternion.identity);
        Entity second = CreateSelectedUnit(new float3(10f, 0f, 0f), quaternion.identity);
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        _em.DestroyEntity(first);

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(TacticalFollowCameraTargetKind.Unit, mode.BaseTargetKind);
        Assert.AreEqual(second, mode.BaseTargetEntity);
        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(second, target.TargetEntity);
        Assert.AreEqual(new float3(10f, 0f, 0f), target.Center);
    }

    [Test]
    public void PassengerSelectedUnitFallsBackToSelectedBuildingTarget()
    {
        Entity transport = _em.CreateEntity();
        Entity passenger = CreateSelectedUnit(new float3(4f, 0f, 6f), quaternion.identity);
        _em.AddComponentData(passenger, new UnitTransportPassenger { Transport = transport });
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        var context = new TacticalFollowCameraModeSystemHelper.Context(
            (out Vector3 worldPosition, out float boundsRadius) =>
            {
                worldPosition = new Vector3(22f, 0f, 28f);
                boundsRadius = 6f;
                return true;
            });

        Assert.IsTrue(_system.ProcessPendingRequests(_em, null, context));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(TacticalFollowCameraTargetKind.Building, mode.BaseTargetKind);
        Assert.AreEqual(Entity.Null, mode.BaseTargetEntity);
        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(TacticalFollowCameraTargetKind.Building, target.TargetKind);
        Assert.AreEqual(new float3(22f, 0f, 28f), target.Center);
    }

    [Test]
    public void BuildingFollowPoseClampsHeightAndDistanceAroundBounds()
    {
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        var context = new TacticalFollowCameraModeSystemHelper.Context(
            (out Vector3 worldPosition, out float boundsRadius) =>
            {
                worldPosition = new Vector3(12f, 0f, 14f);
                boundsRadius = 18f;
                return true;
            });

        Assert.IsTrue(_system.ProcessPendingRequests(_em, null, context));

        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        float flatDistance = math.distance(
            new float2(pose.DesiredPosition.x, pose.DesiredPosition.z),
            new float2(target.LookAt.x, target.LookAt.z));
        Assert.GreaterOrEqual(flatDistance, target.BoundsRadius + 4f);
        Assert.GreaterOrEqual(pose.DesiredPosition.y, target.Center.y + target.BoundsRadius * 0.65f);
        Assert.Greater(pose.DesiredPosition.y, pose.LookAt.y);
    }

    [Test]
    public void MatchingGroundMissileBecomesTemporaryFollowTarget()
    {
        Entity launcher = CreateSelectedUnit(new float3(0f, 0f, 0f), quaternion.identity);
        Entity unrelated = CreateSelectedUnit(new float3(12f, 0f, 0f), quaternion.identity);
        _em.RemoveComponent<SelectedUnitTag>(unrelated);
        Entity unrelatedMissile = CreateGroundMissile(unrelated, new float3(3f, 2f, 4f), new float3(30f, 0f, 4f));
        Entity missile = CreateGroundMissile(launcher, new float3(5f, 3f, 6f), new float3(40f, 0f, 6f));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.HasTemporaryTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.GroundMissile, mode.TemporaryTargetKind);
        Assert.AreEqual(missile, mode.TemporaryTargetEntity);
        Assert.AreNotEqual(unrelatedMissile, mode.TemporaryTargetEntity);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.TemporaryMissile, pose.Source);
    }

    [Test]
    public void MatchingAirMissileBecomesTemporaryFollowTarget()
    {
        Entity launcher = CreateSelectedUnit(new float3(0f, 0f, 0f), quaternion.identity);
        Entity missile = CreateAirMissile(launcher, new float3(5f, 6f, 6f), new float3(0f, 0f, 20f));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.HasTemporaryTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.AirMissile, mode.TemporaryTargetKind);
        Assert.AreEqual(missile, mode.TemporaryTargetEntity);
        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(TacticalFollowCameraTargetKind.AirMissile, target.TargetKind);
        Assert.AreEqual(missile, target.TargetEntity);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.TemporaryMissile, pose.Source);
    }

    [Test]
    public void MissileTemporaryFollowUsesForwardLookAheadFraming()
    {
        Entity launcher = CreateSelectedUnit(new float3(0f, 0f, 0f), quaternion.identity);
        float3 missilePosition = new float3(5f, 6f, 6f);
        CreateAirMissile(launcher, missilePosition, new float3(0f, 0f, 20f));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em));

        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(TacticalFollowCameraTargetKind.AirMissile, target.TargetKind);
        Assert.That(target.LookAt.z, Is.GreaterThan(missilePosition.z + 4f));
        Assert.That(target.LookAt.y, Is.GreaterThan(missilePosition.y));
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.TemporaryMissile, pose.Source);
        Assert.That(pose.DesiredPosition.z, Is.LessThan(target.LookAt.z));
        Assert.That(pose.DesiredPosition.y, Is.GreaterThan(target.Center.y));
    }

    [Test]
    public void ActiveTemporaryMissileDoesNotJitterToLaterMissile()
    {
        Entity launcher = CreateSelectedUnit(new float3(0f, 0f, 0f), quaternion.identity);
        Entity first = CreateAirMissile(launcher, new float3(5f, 6f, 6f), new float3(0f, 0f, 20f));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em));
        Entity second = CreateAirMissile(launcher, new float3(10f, 6f, 12f), new float3(0f, 0f, 20f));

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(first, mode.TemporaryTargetEntity);
        Assert.AreNotEqual(second, mode.TemporaryTargetEntity);
    }

    [Test]
    public void GroundMissileImpactHoldsExplosionThenReturnsToBaseTarget()
    {
        Entity launcher = CreateSelectedUnit(new float3(0f, 0f, 0f), quaternion.identity);
        Entity missile = CreateGroundMissile(launcher, new float3(5f, 3f, 6f), new float3(40f, 0f, 6f));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 1f));
        _em.RemoveComponent<GroundMissileProjectileComponent>(missile);
        _em.AddComponentData(missile, new GroundMissileImpactRequestComponent
        {
            Source = launcher,
            Position = new float3(40f, 0f, 6f)
        });

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 1.1f));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.HasTemporaryTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.GroundMissile, mode.TemporaryTargetKind);
        Assert.Greater(mode.ReturnHoldUntilTime, 1.1f);
        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(new float3(40f, 0f, 6f), target.Center);
        _em.RemoveComponent<GroundMissileImpactRequestComponent>(missile);
        mode.ReturnHoldUntilTime = 1.2f;
        _em.SetComponentData(GetModeEntity(), mode);

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 2.5f));

        Assert.IsTrue(_system.TryReadMode(_em, out mode));
        Assert.AreEqual(0, mode.HasTemporaryTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.None, mode.TemporaryTargetKind);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.BaseTarget, pose.Source);
    }

    [Test]
    public void AirMissileImpactHoldsExplosionThenReturnsToBaseTarget()
    {
        Entity launcher = CreateSelectedUnit(new float3(0f, 0f, 0f), quaternion.identity);
        Entity missile = CreateAirMissile(launcher, new float3(5f, 6f, 6f), new float3(0f, 0f, 20f));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 1f));
        _em.RemoveComponent<AirMissileProjectileComponent>(missile);
        _em.AddComponentData(missile, new AirMissileImpactRequestComponent
        {
            Source = launcher,
            Position = new float3(5f, 8f, 30f),
            VisualSeparation = 0f
        });

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 1.1f));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.HasTemporaryTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.AirMissile, mode.TemporaryTargetKind);
        Assert.Greater(mode.ReturnHoldUntilTime, 1.1f);
        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent target));
        Assert.AreEqual(new float3(5f, 8f, 30f), target.Center);
        _em.RemoveComponent<AirMissileImpactRequestComponent>(missile);
        mode.ReturnHoldUntilTime = 1.2f;
        _em.SetComponentData(GetModeEntity(), mode);

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 2.5f));

        Assert.IsTrue(_system.TryReadMode(_em, out mode));
        Assert.AreEqual(0, mode.HasTemporaryTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.None, mode.TemporaryTargetKind);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.BaseTarget, pose.Source);
    }

    [Test]
    public void TemporaryMissileDespawnWithoutImpactHoldsLastPoseThenReturnsToBaseTarget()
    {
        Entity launcher = CreateSelectedUnit(new float3(0f, 0f, 0f), quaternion.identity);
        Entity missile = CreateAirMissile(launcher, new float3(5f, 6f, 6f), new float3(0f, 0f, 20f));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 1f));
        _em.RemoveComponent<AirMissileProjectileComponent>(missile);

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 1.1f));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.HasTemporaryTarget);
        Assert.Greater(mode.ReturnHoldUntilTime, 1.1f);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.TemporaryMissile, pose.Source);
        mode.ReturnHoldUntilTime = 1.2f;
        _em.SetComponentData(GetModeEntity(), mode);

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 2.5f));

        Assert.IsTrue(_system.TryReadMode(_em, out mode));
        Assert.AreEqual(0, mode.HasTemporaryTarget);
        Assert.IsTrue(_system.TryReadPose(_em, out pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.BaseTarget, pose.Source);
    }

    [Test]
    public void BaseTargetLossDuringMissileFollowFinishesMissileThenExitsSafely()
    {
        Entity launcher = CreateSelectedUnit(new float3(0f, 0f, 0f), quaternion.identity);
        Entity missile = CreateAirMissile(launcher, new float3(5f, 6f, 6f), new float3(0f, 0f, 20f));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 1f));
        _em.DestroyEntity(launcher);

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 1.1f));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.Enabled);
        Assert.AreEqual(1, mode.HasTemporaryTarget);
        Assert.AreEqual(missile, mode.TemporaryTargetEntity);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.TemporaryMissile, pose.Source);
        _em.RemoveComponent<AirMissileProjectileComponent>(missile);
        _em.AddComponentData(missile, new AirMissileImpactRequestComponent
        {
            Source = launcher,
            Position = new float3(5f, 8f, 30f),
            VisualSeparation = 0f
        });

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 1.2f));

        Assert.IsTrue(_system.TryReadMode(_em, out mode));
        Assert.AreEqual(1, mode.Enabled);
        Assert.AreEqual(1, mode.HasTemporaryTarget);
        Assert.Greater(mode.ReturnHoldUntilTime, 1.2f);
        _em.RemoveComponent<AirMissileImpactRequestComponent>(missile);
        mode.ReturnHoldUntilTime = 1.3f;
        _em.SetComponentData(GetModeEntity(), mode);

        Assert.IsFalse(_system.RefreshActiveTargetAndPose(_em, default, 2.5f));

        Assert.IsTrue(_system.TryReadMode(_em, out mode));
        Assert.AreEqual(0, mode.Enabled);
        Assert.AreEqual(0, mode.PanInputLocked);
        Assert.AreEqual(0, mode.HasTemporaryTarget);
    }

    [Test]
    public void ProductionGroundLauncherProjectileIsAdoptedDuringFollowMode()
    {
        Entity target = _em.CreateEntity(typeof(LocalTransform));
        _em.SetComponentData(target, LocalTransform.FromPosition(new float3(60f, 0f, 0f)));
        Entity launcher = CreateProductionGroundLauncher(new float3(0f, 0f, 0f), target, new float3(60f, 0f, 0f));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        SystemHandle fireSystem = _world.CreateSystem<GroundMissileLauncherFireSystem>();
        _world.SetTime(new TimeData(0.1d, 0.1f));
        fireSystem.Update(_world.Unmanaged);
        Entity projectile = GetSingletonEntity<GroundMissileProjectileComponent>();

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 0.2f));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.HasTemporaryTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.GroundMissile, mode.TemporaryTargetKind);
        Assert.AreEqual(projectile, mode.TemporaryTargetEntity);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.TemporaryMissile, pose.Source);
        Assert.AreEqual(launcher, _em.GetComponentData<GroundMissileProjectileComponent>(projectile).Source);
    }

    [Test]
    public void ProductionGroundLauncherProjectileImpactReturnsToBaseTarget()
    {
        Entity target = _em.CreateEntity(typeof(LocalTransform));
        _em.SetComponentData(target, LocalTransform.FromPosition(new float3(60f, 0f, 0f)));
        Entity launcher = CreateProductionGroundLauncher(new float3(0f, 0f, 0f), target, new float3(60f, 0f, 0f));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        SystemHandle fireSystem = _world.CreateSystem<GroundMissileLauncherFireSystem>();
        _world.SetTime(new TimeData(0.1d, 0.1f));
        fireSystem.Update(_world.Unmanaged);
        Entity projectile = GetSingletonEntity<GroundMissileProjectileComponent>();
        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 0.2f));
        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(projectile, mode.TemporaryTargetEntity);
        _em.RemoveComponent<GroundMissileProjectileComponent>(projectile);
        _em.AddComponentData(projectile, new GroundMissileImpactRequestComponent
        {
            Source = launcher,
            Position = new float3(60f, 0f, 0f)
        });

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 0.3f));

        Assert.IsTrue(_system.TryReadMode(_em, out mode));
        Assert.AreEqual(1, mode.HasTemporaryTarget);
        Assert.Greater(mode.ReturnHoldUntilTime, 0.3f);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.TemporaryMissile, pose.Source);
        _em.RemoveComponent<GroundMissileImpactRequestComponent>(projectile);
        mode.ReturnHoldUntilTime = 0.4f;
        _em.SetComponentData(GetModeEntity(), mode);

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 0.8f));

        Assert.IsTrue(_system.TryReadMode(_em, out mode));
        Assert.AreEqual(0, mode.HasTemporaryTarget);
        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent followTarget));
        Assert.AreEqual(launcher, followTarget.TargetEntity);
        Assert.IsTrue(_system.TryReadPose(_em, out pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.BaseTarget, pose.Source);
    }

    [Test]
    public void ProductionAirLauncherProjectileIsAdoptedDuringFollowMode()
    {
        Entity target = CreateAirTarget(new float3(30f, 10f, 0f));
        Entity launcher = CreateProductionAirLauncher(new float3(0f, 0f, 0f), target, new float3(30f, 10f, 0f));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        SystemHandle fireSystem = _world.CreateSystem<AirMissileLauncherFireControlSystem>();
        _world.SetTime(new TimeData(0.1d, 0.1f));
        fireSystem.Update(_world.Unmanaged);
        Entity projectile = GetSingletonEntity<AirMissileProjectileComponent>();

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 0.2f));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.HasTemporaryTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.AirMissile, mode.TemporaryTargetKind);
        Assert.AreEqual(projectile, mode.TemporaryTargetEntity);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.TemporaryMissile, pose.Source);
        Assert.AreEqual(launcher, _em.GetComponentData<AirMissileProjectileComponent>(projectile).Source);
    }

    [Test]
    public void ProductionAirLauncherProjectileImpactReturnsToBaseTarget()
    {
        Entity target = CreateAirTarget(new float3(30f, 10f, 0f));
        Entity launcher = CreateProductionAirLauncher(new float3(0f, 0f, 0f), target, new float3(30f, 10f, 0f));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        SystemHandle fireSystem = _world.CreateSystem<AirMissileLauncherFireControlSystem>();
        _world.SetTime(new TimeData(0.1d, 0.1f));
        fireSystem.Update(_world.Unmanaged);
        Entity projectile = GetSingletonEntity<AirMissileProjectileComponent>();
        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 0.2f));
        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(projectile, mode.TemporaryTargetEntity);
        _em.RemoveComponent<AirMissileProjectileComponent>(projectile);
        _em.AddComponentData(projectile, new AirMissileImpactRequestComponent
        {
            Source = launcher,
            Position = new float3(30f, 10f, 0f),
            VisualSeparation = 0f
        });

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 0.3f));

        Assert.IsTrue(_system.TryReadMode(_em, out mode));
        Assert.AreEqual(1, mode.HasTemporaryTarget);
        Assert.Greater(mode.ReturnHoldUntilTime, 0.3f);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.TemporaryMissile, pose.Source);
        _em.RemoveComponent<AirMissileImpactRequestComponent>(projectile);
        mode.ReturnHoldUntilTime = 0.4f;
        _em.SetComponentData(GetModeEntity(), mode);

        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 0.8f));

        Assert.IsTrue(_system.TryReadMode(_em, out mode));
        Assert.AreEqual(0, mode.HasTemporaryTarget);
        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent followTarget));
        Assert.AreEqual(launcher, followTarget.TargetEntity);
        Assert.IsTrue(_system.TryReadPose(_em, out pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.BaseTarget, pose.Source);
    }

    [Test]
    public void FollowedAirUnitAttackVfxCreatesImpactCutawayThenReturns()
    {
        Entity aircraft = CreateSelectedAirUnit(new float3(0f, 12f, 0f), quaternion.identity);
        Entity target = _em.CreateEntity(typeof(LocalTransform));
        float3 targetPosition = new float3(24f, 0f, 10f);
        _em.SetComponentData(target, LocalTransform.FromPosition(targetPosition));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        Entity request = _em.CreateEntity(typeof(UnitAttackVfxRequest));
        _em.SetComponentData(request, new UnitAttackVfxRequest
        {
            Kind = (byte)UnitAttackVfxRequestKind.MuzzleFlash,
            Source = aircraft,
            Target = target,
            SourcePosition = new float3(0f, 12f, 0f),
            TargetPosition = targetPosition,
            PlaybackPosition = new float3(0f, 12f, 1f),
            PlaybackRotation = quaternion.identity
        });
        SystemHandle cinematicSystem = _world.CreateSystem<TacticalFollowAttackCinematicSystem>();
        _world.SetTime(new TimeData(10d, 0.016f));

        cinematicSystem.Update(_world.Unmanaged);
        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 10.1f));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(1, mode.HasTemporaryTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.AttackImpact, mode.TemporaryTargetKind);
        Assert.AreEqual(target, mode.TemporaryTargetEntity);
        Assert.AreEqual(0f, mode.ReturnHoldUntilTime);
        using (EntityQuery cinematicQuery =
               _em.CreateEntityQuery(ComponentType.ReadOnly<TacticalFollowAttackCinematicStateComponent>()))
        {
            Assert.IsFalse(cinematicQuery.IsEmptyIgnoreFilter);
            TacticalFollowAttackCinematicStateComponent cinematic =
                _em.GetComponentData<TacticalFollowAttackCinematicStateComponent>(cinematicQuery.GetSingletonEntity());
            Assert.AreEqual(1, cinematic.Active);
        }

        Assert.IsTrue(_system.TryReadTarget(_em, out TacticalFollowCameraTargetComponent followTarget));
        Assert.AreEqual(TacticalFollowCameraTargetKind.AttackImpact, followTarget.TargetKind);
        Assert.AreEqual(targetPosition, followTarget.Center);
        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.TemporaryMissile, pose.Source);

        _world.SetTime(new TimeData(15d, 5f));
        cinematicSystem.Update(_world.Unmanaged);
        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 15.1f));

        Assert.IsTrue(_system.TryReadMode(_em, out mode));
        Assert.AreEqual(0, mode.HasTemporaryTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.None, mode.TemporaryTargetKind);
        Assert.IsTrue(_system.TryReadPose(_em, out pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.BaseTarget, pose.Source);
    }

    [Test]
    public void UnfollowedAirUnitAttackVfxDoesNotCreateImpactCutaway()
    {
        CreateSelectedAirUnit(new float3(0f, 12f, 0f), quaternion.identity);
        Entity unrelatedAircraft = CreateAirTarget(new float3(12f, 10f, 0f));
        Entity target = _em.CreateEntity(typeof(LocalTransform));
        _em.SetComponentData(target, LocalTransform.FromPosition(new float3(24f, 0f, 10f)));
        QueueRequest(TacticalFollowCameraRequestKind.ToggleFollowMode);
        Assert.IsTrue(_system.ProcessPendingRequests(_em));
        Entity request = _em.CreateEntity(typeof(UnitAttackVfxRequest));
        _em.SetComponentData(request, new UnitAttackVfxRequest
        {
            Kind = (byte)UnitAttackVfxRequestKind.Impact,
            Source = unrelatedAircraft,
            Target = target,
            SourcePosition = new float3(12f, 10f, 0f),
            TargetPosition = new float3(24f, 0f, 10f),
            PlaybackPosition = new float3(24f, 0f, 10f),
            PlaybackRotation = quaternion.identity
        });
        SystemHandle cinematicSystem = _world.CreateSystem<TacticalFollowAttackCinematicSystem>();
        _world.SetTime(new TimeData(10d, 0.016f));

        cinematicSystem.Update(_world.Unmanaged);
        Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 10.1f));

        Assert.IsTrue(_system.TryReadMode(_em, out TacticalFollowCameraModeComponent mode));
        Assert.AreEqual(0, mode.HasTemporaryTarget);
        Assert.AreEqual(TacticalFollowCameraTargetKind.None, mode.TemporaryTargetKind);
        using (EntityQuery cinematicQuery =
               _em.CreateEntityQuery(ComponentType.ReadOnly<TacticalFollowAttackCinematicStateComponent>()))
        {
            if (!cinematicQuery.IsEmptyIgnoreFilter)
            {
                TacticalFollowAttackCinematicStateComponent cinematic =
                    _em.GetComponentData<TacticalFollowAttackCinematicStateComponent>(cinematicQuery.GetSingletonEntity());
                Assert.AreEqual(0, cinematic.Active);
            }
        }

        Assert.IsTrue(_system.TryReadPose(_em, out TacticalFollowCameraPoseComponent pose));
        Assert.AreEqual(TacticalFollowCameraPoseSource.BaseTarget, pose.Source);
    }

    private Entity CreateSelectedUnit(float3 position, quaternion rotation)
    {
        Entity entity = _em.CreateEntity(typeof(SelectedUnitTag), typeof(LocalTransform));
        _em.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, rotation, 1f));
        return entity;
    }

    private Entity CreateSelectedAirUnit(float3 position, quaternion rotation)
    {
        Entity entity = _em.CreateEntity(typeof(SelectedUnitTag), typeof(UnitAirMovement), typeof(LocalTransform));
        _em.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, rotation, 1f));
        _em.SetComponentData(entity, new UnitAirMovement
        {
            CruiseHeight = math.max(1f, position.y),
            RunwayTaxiSpeed = 5f
        });
        return entity;
    }

    private Entity CreateGroundMissile(Entity source, float3 position, float3 targetPosition)
    {
        Entity entity = _em.CreateEntity(typeof(LocalTransform), typeof(GroundMissileProjectileComponent));
        _em.SetComponentData(entity, LocalTransform.FromPosition(position));
        _em.SetComponentData(entity, new GroundMissileProjectileComponent
        {
            Source = source,
            TargetPosition = targetPosition,
            DurationSeconds = 4f,
            DamageRadius = 3f,
            Damage = 50,
            Interceptable = 1
        });
        return entity;
    }

    private Entity CreateAirMissile(Entity source, float3 position, float3 velocity)
    {
        Entity entity = _em.CreateEntity(typeof(LocalTransform), typeof(AirMissileProjectileComponent));
        _em.SetComponentData(entity, LocalTransform.FromPosition(position));
        _em.SetComponentData(entity, new AirMissileProjectileComponent
        {
            Source = source,
            Velocity = velocity,
            Speed = math.max(1f, math.length(velocity)),
            LifetimeSeconds = 6f,
            ProximityFuseRadius = 4f,
            Damage = 60,
            TrackingQuality = 1f
        });
        return entity;
    }

    private Entity CreateProductionGroundLauncher(float3 position, Entity target, float3 targetPosition)
    {
        Entity entity = _em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(GroundMissileLauncherComponent),
            typeof(GroundMissileLauncherStateComponent),
            typeof(LocalTransform));
        _em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        _em.SetComponentData(entity, new GroundMissileLauncherComponent
        {
            MinRange = 5f,
            MaxRange = 600f,
            PrepareSeconds = 0.01f,
            ReloadSeconds = 3f,
            BatteryElevatedAngleDegrees = -30f,
            RocketSpeed = 100f,
            ArcHeight = 10f,
            DamageRadius = 5f,
            Damage = 100
        });
        _em.SetComponentData(entity, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Preparing,
            TargetEntity = target,
            TargetCell = new int2(60, 0),
            TargetWorldPosition = targetPosition,
            Timer = 0f,
            SelectedRocketSlot = -1
        });
        _em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private Entity CreateProductionAirLauncher(float3 position, Entity target, float3 targetPosition)
    {
        Entity entity = _em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform),
            typeof(AirMissileLauncherComponent),
            typeof(AirMissileLauncherStateComponent),
            typeof(AirDefenseSupportLinkComponent),
            typeof(AirMissileLauncherTargetComponent));
        _em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        _em.SetComponentData(entity, new UnitHealth { Current = 500, Max = 500 });
        _em.SetComponentData(entity, LocalTransform.FromPosition(position));
        _em.SetComponentData(entity, new AirMissileLauncherComponent
        {
            MinRange = 4f,
            BaseDetectionRange = 120f,
            MaxDetectionRange = 260f,
            AirTargetPriority = 25f,
            IncomingMissilePriority = 100f,
            TurretYawSpeedDegreesPerSecond = 900f,
            AimToleranceDegrees = 5f,
            LockSeconds = 1f,
            LaunchDelaySeconds = 0.1f,
            ReloadSeconds = 1.5f,
            MissileSpeed = 95f,
            MissileAcceleration = 0f,
            MissileTurnRateDegreesPerSecond = 120f,
            MissileLifetimeSeconds = 5f,
            ProximityFuseRadius = 4f,
            AirTargetDamage = 120,
            IncomingMissileDamage = 9999,
            TrackingQuality = 0.75f,
            MaxSupportRangeBonus = 180f,
            MaxSupportTrackingBonus = 0.3f
        });
        _em.SetComponentData(entity, new AirMissileLauncherStateComponent
        {
            Phase = (byte)AirMissileLauncherPhase.Locked,
            TargetEntity = target,
            TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit,
            TargetWorldPosition = targetPosition,
            PredictedInterceptPosition = targetPosition,
            Timer = 0f,
            EffectiveRange = 120f,
            EffectiveLockSeconds = 1f,
            EffectiveTrackingQuality = 0.75f,
            EffectiveTurnRateDegreesPerSecond = 120f
        });
        _em.SetComponentData(entity, new AirDefenseSupportLinkComponent
        {
            LockTimeMultiplier = 1f
        });
        _em.SetComponentData(entity, new AirMissileLauncherTargetComponent
        {
            Target = target,
            TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit,
            TargetWorldPosition = targetPosition,
            PredictedInterceptPosition = targetPosition,
            Score = 25f
        });
        return entity;
    }

    private Entity CreateAirTarget(float3 position)
    {
        Entity entity = _em.CreateEntity(
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform),
            typeof(UnitAirMovement));
        _em.SetComponentData(entity, new Faction { Id = FactionIdentity.EnemyFactionId });
        _em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        _em.SetComponentData(entity, LocalTransform.FromPosition(position));
        _em.SetComponentData(entity, new UnitAirMovement
        {
            CruiseHeight = 12f,
            RunwayTaxiSpeed = 5f
        });
        return entity;
    }

    private Entity GetSingletonEntity<T>() where T : unmanaged, IComponentData
    {
        using EntityQuery query = _em.CreateEntityQuery(ComponentType.ReadOnly<T>());
        Assert.AreEqual(1, query.CalculateEntityCount());
        return query.GetSingletonEntity();
    }

    private DynamicBuffer<TacticalFollowCameraRequestElement> GetRequestBuffer()
    {
        using EntityQuery query = _em.CreateEntityQuery(
            ComponentType.ReadOnly<TacticalFollowCameraRequestQueueComponent>(),
            ComponentType.ReadWrite<TacticalFollowCameraRequestElement>());
        return _em.GetBuffer<TacticalFollowCameraRequestElement>(query.GetSingletonEntity());
    }

    private Entity GetModeEntity()
    {
        using EntityQuery query = _em.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraModeComponent>());
        return query.GetSingletonEntity();
    }

    private void QueueRequest(TacticalFollowCameraRequestKind kind)
    {
        Entity queue = EnsureRequestEntity();
        TacticalFollowCameraRequestQueueComponent component =
            _em.GetComponentData<TacticalFollowCameraRequestQueueComponent>(queue);
        component.LastRequestId++;
        _em.SetComponentData(queue, component);
        _em.GetBuffer<TacticalFollowCameraRequestElement>(queue).Add(new TacticalFollowCameraRequestElement
        {
            Kind = kind,
            RequestId = component.LastRequestId
        });
    }

    private Entity EnsureRequestEntity()
    {
        using EntityQuery query = _em.CreateEntityQuery(
            ComponentType.ReadWrite<TacticalFollowCameraRequestQueueComponent>(),
            ComponentType.ReadWrite<TacticalFollowCameraRequestElement>());
        if (!query.IsEmptyIgnoreFilter)
            return query.GetSingletonEntity();

        Entity queue = _em.CreateEntity(typeof(TacticalFollowCameraRequestQueueComponent));
        _em.AddBuffer<TacticalFollowCameraRequestElement>(queue);
        return queue;
    }
}
#endif
