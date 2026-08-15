using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public static class RuntimeCameraFocusRequestUtility
    {
        public static void Queue(
            RtsCameraRequestSystem camera,
            EntityManager entityManager,
            RuntimeCameraFocusRequestComponent request,
            Vector3 focusWorldPosition)
        {
            if (request.Smooth != 0)
            {
                camera.QueueSetSmoothFocusTarget(entityManager, focusWorldPosition, true);
                return;
            }

            camera.QueueMoveGroundCenterTo(entityManager, focusWorldPosition);
            camera.QueueClearSmoothFocusTarget(entityManager);
        }
    }
}
