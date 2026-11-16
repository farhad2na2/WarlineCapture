using Game.Components;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Runtime
{
    /// <summary>
    /// Selects and validates a camera pose from operation-map metadata.
    /// This is data-only; callers own world, scene, and camera lifecycle checks.
    /// </summary>
    public static class OperationMapCameraPoseSelectionData
    {
        public static bool TryResolve(
            ref OperationMapBlob metadata,
            bool usePlanningPose,
            out OperationMapCameraBlob pose)
        {
            FixedString64Bytes cameraId = usePlanningPose
                ? metadata.PlanningCameraId
                : metadata.BattleCameraId;
            pose = default;
            if (cameraId.IsEmpty)
                return false;
            return OperationMapMetadataUtility.TryFindCamera(ref metadata, in cameraId, out pose) &&
                IsFinite(in pose);
        }

        private static bool IsFinite(in OperationMapCameraBlob pose) =>
            math.all(math.isfinite(pose.Position)) &&
            math.all(math.isfinite(pose.Rotation.value)) &&
            math.isfinite(pose.FieldOfView) && pose.FieldOfView > 0f &&
            math.isfinite(pose.OrthographicSize) && pose.OrthographicSize > 0f;
    }
}
