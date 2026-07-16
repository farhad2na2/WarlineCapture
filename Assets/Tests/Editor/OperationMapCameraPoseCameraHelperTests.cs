using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class OperationMapCameraPoseCameraHelperTests
{
    [Test]
    public void AppliesInitialPlanningAndBattlePosesFromActiveMapBlob()
    {
        using var world = new World("OperationMapCameraPoseTests");
        BlobAssetReference<OperationMapBlob> blob = CreateMetadataBlob();
        var cameraObject = new GameObject("OperationMapCameraPoseTestsCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        try
        {
            Entity entity = world.EntityManager.CreateEntity(
                typeof(ActiveOperationMapComponent),
                typeof(OperationMapMetadataComponent));
            world.EntityManager.SetComponentData(entity, new ActiveOperationMapComponent
            {
                OperationMapId = new FixedString64Bytes("opmap.test.camera")
            });
            world.EntityManager.SetComponentData(entity, new OperationMapMetadataComponent
            {
                Blob = blob,
                Generation = 1
            });

            Assert.That(OperationMapCameraPoseCameraHelper.TryApplyInitialPose(world, camera), Is.True);
            AssertPose(camera, new Vector3(10f, 20f, 30f), Quaternion.identity, true, 35f);

            camera.transform.position = Vector3.zero;
            Assert.That(OperationMapCameraPoseCameraHelper.TryApplyPlanningPose(world, camera), Is.True);
            AssertPose(camera, new Vector3(10f, 20f, 30f), Quaternion.identity, true, 35f);

            Assert.That(OperationMapCameraPoseCameraHelper.TryApplyBattlePose(world, camera), Is.True);
            AssertPose(
                camera,
                new Vector3(40f, 50f, 60f),
                new Quaternion(0f, 0.70710677f, 0f, 0.70710677f),
                false,
                55f);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
            blob.Dispose();
        }
    }

    [Test]
    public void MissingActiveMapLeavesCameraUnchanged()
    {
        using var world = new World("OperationMapCameraPoseMissingTests");
        var cameraObject = new GameObject("OperationMapCameraPoseMissingCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(7f, 8f, 9f);
        try
        {
            Assert.That(OperationMapCameraPoseCameraHelper.TryApplyInitialPose(world, camera), Is.False);
            Assert.That(camera.transform.position, Is.EqualTo(new Vector3(7f, 8f, 9f)));
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    private static BlobAssetReference<OperationMapBlob> CreateMetadataBlob()
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref OperationMapBlob root = ref builder.ConstructRoot<OperationMapBlob>();
        root.PlanningCameraId = new FixedString64Bytes("camera.test.planning");
        root.BattleCameraId = new FixedString64Bytes("camera.test.battle");
        BlobBuilderArray<OperationMapCameraBlob> cameras = builder.Allocate(ref root.Cameras, 2);
        cameras[0] = new OperationMapCameraBlob
        {
            Id = root.PlanningCameraId,
            Position = new float3(10f, 20f, 30f),
            Rotation = quaternion.identity,
            OrthographicSize = 35f,
            FieldOfView = 60f,
            IsOrthographic = 1,
            ClampToCameraBounds = 1
        };
        cameras[1] = new OperationMapCameraBlob
        {
            Id = root.BattleCameraId,
            Position = new float3(40f, 50f, 60f),
            Rotation = new quaternion(0f, 0.70710677f, 0f, 0.70710677f),
            OrthographicSize = 20f,
            FieldOfView = 55f,
            IsOrthographic = 0,
            ClampToCameraBounds = 1
        };
        return builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
    }

    private static void AssertPose(
        Camera camera,
        Vector3 position,
        Quaternion rotation,
        bool orthographic,
        float projectionValue)
    {
        Assert.That(camera.transform.position, Is.EqualTo(position));
        Assert.That(Quaternion.Angle(camera.transform.rotation, rotation), Is.LessThan(0.001f));
        Assert.That(camera.orthographic, Is.EqualTo(orthographic));
        if (orthographic)
            Assert.That(camera.orthographicSize, Is.EqualTo(projectionValue));
        else
            Assert.That(camera.fieldOfView, Is.EqualTo(projectionValue));
    }
}
