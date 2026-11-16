using System;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static class OperationMapCameraPoseSelectionDataControl
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static OperationMapCameraBlob Camera(string id, float x, float fieldOfView = 45f, float orthographicSize = 20f) =>
        new OperationMapCameraBlob
        {
            Id = new FixedString64Bytes(id),
            Position = new float3(x, 25f, 30f),
            Rotation = quaternion.identity,
            FieldOfView = fieldOfView,
            OrthographicSize = orthographicSize,
            IsOrthographic = 0
        };

    private static OperationMapBlob Metadata(params OperationMapCameraBlob[] cameras) =>
        new OperationMapBlob
        {
            PlanningCameraId = new FixedString64Bytes("camera.planning"),
            BattleCameraId = new FixedString64Bytes("camera.battle"),
            Cameras = new BlobArray<OperationMapCameraBlob>(cameras)
        };

    private static bool Resolve(ref OperationMapBlob metadata, bool planning, out OperationMapCameraBlob pose) =>
        OperationMapCameraPoseSelectionData.TryResolve(ref metadata, planning, out pose);

    private static void Main()
    {
        OperationMapBlob distinct = Metadata(Camera("camera.planning", 11f), Camera("camera.battle", 22f));
        Need(Resolve(ref distinct, true, out var planning) && planning.Position.x == 11f,
            "Planning ID did not resolve its own camera");
        Need(Resolve(ref distinct, false, out var battle) && battle.Position.x == 22f,
            "Battle ID did not resolve its own camera");

        OperationMapBlob missing = Metadata(Camera("camera.other", 1f));
        Need(!Resolve(ref missing, true, out _), "Missing selected camera ID was accepted");

        OperationMapBlob nonfinitePosition = Metadata(Camera("camera.planning", float.NaN));
        Need(!Resolve(ref nonfinitePosition, true, out _), "Nonfinite position was accepted");
        OperationMapBlob nonfiniteRotation = Metadata(Camera("camera.planning", 1f));
        var badRotation = nonfiniteRotation.Cameras[0];
        badRotation.Rotation = new quaternion(float.NaN, 0f, 0f, 1f);
        nonfiniteRotation.Cameras[0] = badRotation;
        Need(!Resolve(ref nonfiniteRotation, true, out _), "Nonfinite rotation was accepted");

        OperationMapBlob invalidFieldOfView = Metadata(Camera("camera.planning", 1f, fieldOfView: 0f));
        Need(!Resolve(ref invalidFieldOfView, true, out _), "Nonpositive field of view was accepted");
        OperationMapBlob invalidOrthographicSize = Metadata(Camera("camera.planning", 1f, orthographicSize: 0f));
        Need(!Resolve(ref invalidOrthographicSize, true, out _), "Nonpositive orthographic size was accepted");

        OperationMapBlob duplicateFirstInvalid = Metadata(
            Camera("camera.planning", float.NaN),
            Camera("camera.planning", 99f));
        Need(!Resolve(ref duplicateFirstInvalid, true, out _),
            "Duplicate IDs must preserve first-match invalid-pose failure");

        Console.WriteLine("[OperationMapCameraPoseSelectionDataControl] result=Passed planning=1 battle=1 missing_id=1 nonfinite=2 invalid_fov=1 invalid_orthographic_size=1 duplicate_first_invalid=1 world=0 camera_lifecycle=0 readiness=0");
    }
}
