using Unity.Entities;

namespace Game.Runtime
{
    public sealed partial class RtsSelectionRuntimeCameraSystemHelper
    {
        private void UpdateSmoothCameraFocus(Context context)
        {
            if ((!context.CameraSystem.HasSmoothFocusTarget &&
                 !context.CameraSystem.HasSmoothPerspectiveTarget) || context.WorldCamera == null)
            {
                return;
            }

            if (!context.TryGetDefaultEntityManager(out EntityManager entityManager))
                return;

            if (context.CameraSystem.HasSmoothFocusTarget)
            {
                context.CameraRequestSystem.QueueUpdateSmoothFocus(
                    entityManager, context.ZoomTransitionSmoothTime);
            }
            if (context.CameraSystem.HasSmoothPerspectiveTarget)
            {
                context.CameraRequestSystem.QueueUpdateSmoothPerspective(
                    entityManager, context.ZoomTransitionSmoothTime);
            }
            ProcessCameraRequests(context, entityManager);
        }
    }
}
