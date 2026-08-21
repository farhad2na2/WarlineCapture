using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class RtsCameraRequestSystem
    {
        public bool QueueSetSmoothFocusTarget(
            EntityManager entityManager,
            Vector3 focusWorldPosition,
            bool resetVelocity,
            float smoothTimeSeconds = 0f)
        {
            RtsCameraRequestElement request = new()
            {
                Kind = RtsCameraRequestKind.SetSmoothFocusTarget,
                WorldPosition = new float3(focusWorldPosition.x, focusWorldPosition.y, focusWorldPosition.z),
                Flag = ToByte(resetVelocity),
                Value = smoothTimeSeconds
            };
            return TryEnqueue(entityManager, request);
        }

        public bool QueueClearSmoothFocusTarget(EntityManager entityManager) =>
            TryEnqueue(entityManager, new RtsCameraRequestElement
            {
                Kind = RtsCameraRequestKind.ClearSmoothFocusTarget
            });

        public bool QueueUpdateSmoothFocus(EntityManager entityManager, float smoothTime) =>
            TryEnqueue(entityManager, new RtsCameraRequestElement
            {
                Kind = RtsCameraRequestKind.UpdateSmoothFocus,
                Value = smoothTime
            });

        public bool QueueSetSmoothPerspectiveTarget(
            EntityManager entityManager,
            float height,
            float pitch,
            float yaw,
            float fieldOfView,
            float smoothTime,
            bool resetVelocity) =>
            TryEnqueue(entityManager, new RtsCameraRequestElement
            {
                Kind = RtsCameraRequestKind.SetSmoothPerspectiveTarget,
                Value = height,
                Value2 = pitch,
                Value3 = yaw,
                Value4 = fieldOfView,
                Value5 = smoothTime,
                Flag = ToByte(resetVelocity)
            });

        public bool QueueClearSmoothPerspectiveTarget(EntityManager entityManager) =>
            TryEnqueue(entityManager, new RtsCameraRequestElement
            {
                Kind = RtsCameraRequestKind.ClearSmoothPerspectiveTarget
            });

        public bool QueueUpdateSmoothPerspective(EntityManager entityManager, float fallbackSmoothTime) =>
            TryEnqueue(entityManager, new RtsCameraRequestElement
            {
                Kind = RtsCameraRequestKind.UpdateSmoothPerspective,
                Value = fallbackSmoothTime
            });

        private static bool TryProcessSmoothRequest(
            RtsCameraRequestElement request,
            RtsCameraSystem cameraSystem,
            Camera worldCamera)
        {
            switch (request.Kind)
            {
                case RtsCameraRequestKind.SetSmoothFocusTarget:
                    cameraSystem.SetSmoothFocusTarget(
                        ToVector3(request.WorldPosition),
                        request.Flag != 0,
                        request.Value);
                    return true;
                case RtsCameraRequestKind.ClearSmoothFocusTarget:
                    cameraSystem.ClearSmoothFocusTarget();
                    return true;
                case RtsCameraRequestKind.UpdateSmoothFocus:
                    if (cameraSystem.HasSmoothFocusTarget && worldCamera != null)
                    {
                        Vector3 currentGroundCenter = cameraSystem.GetCameraGroundCenterWorld(worldCamera);
                        Vector3 smoothedCenter = cameraSystem.UpdateSmoothFocus(currentGroundCenter, request.Value);
                        cameraSystem.MoveCameraGroundCenterTo(worldCamera, smoothedCenter);
                    }
                    return true;
                case RtsCameraRequestKind.SetSmoothPerspectiveTarget:
                    cameraSystem.SetSmoothPerspectiveTarget(
                        request.Value,
                        request.Value2,
                        request.Value3,
                        request.Value4,
                        request.Value5,
                        request.Flag != 0);
                    return true;
                case RtsCameraRequestKind.ClearSmoothPerspectiveTarget:
                    cameraSystem.ClearSmoothPerspectiveTarget();
                    return true;
                case RtsCameraRequestKind.UpdateSmoothPerspective:
                    cameraSystem.UpdateSmoothPerspective(worldCamera, request.Value);
                    return true;
                default:
                    return false;
            }
        }
    }
}
