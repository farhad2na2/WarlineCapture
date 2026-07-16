using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    internal static class OperationMapCameraPoseCameraHelper
    {
        public static bool TryApplyInitialPose(World world, Camera worldCamera) =>
            TryApplyPose(world, worldCamera, usePlanningPose: true);

        public static bool TryApplyPlanningPose(World world, Camera worldCamera) =>
            TryApplyPose(world, worldCamera, usePlanningPose: true);

        public static bool TryApplyBattlePose(World world, Camera worldCamera) =>
            TryApplyPose(world, worldCamera, usePlanningPose: false);

        private static bool TryApplyPose(World world, Camera worldCamera, bool usePlanningPose)
        {
            if (world == null || !world.IsCreated || worldCamera == null)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ActiveOperationMapComponent>(),
                ComponentType.ReadOnly<OperationMapMetadataComponent>());
            if (query.CalculateEntityCount() != 1)
                return false;

            query.CompleteDependency();
            OperationMapMetadataComponent metadataComponent =
                entityManager.GetComponentData<OperationMapMetadataComponent>(query.GetSingletonEntity());
            if (!metadataComponent.Blob.IsCreated)
                return false;

            ref OperationMapBlob metadata = ref metadataComponent.Blob.Value;
            FixedString64Bytes cameraId = usePlanningPose
                ? metadata.PlanningCameraId
                : metadata.BattleCameraId;
            if (cameraId.IsEmpty ||
                !OperationMapMetadataUtility.TryFindCamera(ref metadata, in cameraId, out OperationMapCameraBlob pose) ||
                !IsFinite(in pose))
            {
                return false;
            }

            Transform cameraTransform = worldCamera.transform;
            cameraTransform.SetPositionAndRotation(ToVector3(pose.Position), ToQuaternion(pose.Rotation));
            worldCamera.orthographic = pose.IsOrthographic != 0;
            if (worldCamera.orthographic)
                worldCamera.orthographicSize = pose.OrthographicSize;
            else
                worldCamera.fieldOfView = pose.FieldOfView;
            return true;
        }

        private static bool IsFinite(in OperationMapCameraBlob pose) =>
            math.all(math.isfinite(pose.Position)) &&
            math.all(math.isfinite(pose.Rotation.value)) &&
            math.isfinite(pose.FieldOfView) && pose.FieldOfView > 0f &&
            math.isfinite(pose.OrthographicSize) && pose.OrthographicSize > 0f;

        private static Vector3 ToVector3(float3 value) => new(value.x, value.y, value.z);

        private static Quaternion ToQuaternion(quaternion value) =>
            new(value.value.x, value.value.y, value.value.z, value.value.w);
    }
}
