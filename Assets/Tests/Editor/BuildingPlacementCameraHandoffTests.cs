using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingPlacementCameraHandoffTests
{
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
