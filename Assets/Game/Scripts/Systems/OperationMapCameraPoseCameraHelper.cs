using Game.Components;
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
            if (!OperationMapCameraPoseSelectionData.TryResolve(ref metadata, usePlanningPose, out OperationMapCameraBlob pose))
                return false;

            Transform cameraTransform = worldCamera.transform;
            cameraTransform.SetPositionAndRotation(ToVector3(pose.Position), ToQuaternion(pose.Rotation));
            worldCamera.orthographic = pose.IsOrthographic != 0;
            if (worldCamera.orthographic)
                worldCamera.orthographicSize = pose.OrthographicSize;
            else
                worldCamera.fieldOfView = pose.FieldOfView;
            return true;
        }

        private static Vector3 ToVector3(float3 value) => new(value.x, value.y, value.z);

        private static Quaternion ToQuaternion(quaternion value) =>
            new(value.value.x, value.value.y, value.value.z, value.value.w);
    }
}
