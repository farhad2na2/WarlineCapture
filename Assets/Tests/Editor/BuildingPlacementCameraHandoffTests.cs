using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingPlacementCameraHandoffTests
{
    [Test]
    public void MapFocusCancelsDeliveryTransitionAndRetainsTheRequestedCenter()
    {
        var previous = World.DefaultGameObjectInjectionWorld;
        var go = new GameObject("MapFocusOwnershipTest");
        using var world = new World("MapFocusOwnershipTest");
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            var camera = go.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(1015, 32, 670), Quaternion.Euler(40, 10, 0));
            camera.fieldOfView = 36;
            var system = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            var requests = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            var helper = new SelectionUiCameraSystemHelper(system, requests);
            helper.Init(null, camera);
            system.SetSmoothFocusTarget(new Vector3(1027, 0, 726), true);
            system.SetSmoothPerspectiveTarget(40, 40, 10, 36, 1, true);
            var destination = new Vector3(1185, 0, 434);
            helper.MoveCameraGroundCenterTo(destination);
            Assert.That(system.HasSmoothFocusTarget, Is.False);
            Assert.That(system.HasSmoothPerspectiveTarget, Is.False);
            Assert.That(Vector3.Distance(system.GetCameraGroundCenterWorld(camera), destination), Is.LessThan(.01f));
            // A later smooth-update tick cannot restore the previous delivery focus.
            requests.QueueUpdateSmoothFocus(world.EntityManager, 1);
            requests.ProcessPendingRequests(world.EntityManager, system, camera);
            Assert.That(Vector3.Distance(system.GetCameraGroundCenterWorld(camera), destination), Is.LessThan(.01f));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void RemotePlacementCentersInPlayableViewportAtDifferentCameraPoses()
    {
        var go = new GameObject("PlacementCameraTest");
        try
        {
            using var world = new World("PlacementCameraTest");
            var system = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            var camera = go.AddComponent<Camera>(); camera.aspect = 20f / 9f; camera.fieldOfView = 36;
            foreach (bool orthographic in new[] { false, true })
            foreach (var start in new[] { new Vector3(300, 40, 100), new Vector3(1000, 24, 700), new Vector3(-20, 55, -50) })
            {
                camera.orthographic = orthographic; camera.orthographicSize = 25;
                camera.transform.SetPositionAndRotation(start, Quaternion.Euler(58, 10, 0));
                var target = new Vector3(883, .67f, 423);
                var viewport = new Vector2(.4f, .6f);
                var desired = SelectionUiCameraSystemHelper.ResolvePlacementCameraGroundCenter(camera, target, viewport);
                system.MoveCameraGroundCenterTo(camera, desired);
                var actual = camera.WorldToViewportPoint(target);
                Assert.That(actual.z, Is.GreaterThan(0));
                Assert.That(Vector2.Distance(actual, viewport), Is.LessThan(.001f));
            }
        }
        finally { Object.DestroyImmediate(go); }
    }
}
