using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public static class RuntimeCameraFocusRequestUtility
    {
        public const float TacticalRevealHeight = 12f;
        public const float TacticalRevealPitch = 68f;
        public const float TacticalRevealYaw = 0f;
        public const float TacticalRevealFieldOfView = 38f;

        public static Vector3 GetInitialBuildingFootprintCenterWorld(
            Vector2Int originCell,
            Vector2Int footprintCells,
            GridConfig grid)
        {
            return new Vector3(
                grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
                grid.Origin.y,
                grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
        }

        public static bool HasActiveCampaignMission(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRuntimeComponent>());
            if (query.CalculateEntityCount() != 1)
                return false;

            CampaignMissionRuntimeComponent runtime = query.GetSingleton<CampaignMissionRuntimeComponent>();
            return !runtime.MissionId.IsEmpty && !runtime.SessionToken.IsEmpty;
        }

        public static void QueueTacticalRevealZoom(
            RtsCameraRequestSystem camera,
            EntityManager entityManager)
        {
            camera.QueueSetMatchIntroZoomSettlePending(entityManager, false);
            camera.QueueApplyPerspectiveModeInstant(
                entityManager,
                TacticalRevealHeight,
                TacticalRevealPitch,
                TacticalRevealYaw,
                TacticalRevealFieldOfView);
            camera.QueueCompleteZoomTransition(entityManager);
        }

        public static void Queue(
            RtsCameraRequestSystem camera,
            EntityManager entityManager,
            RuntimeCameraFocusRequestComponent request,
            Vector3 focusWorldPosition)
        {
            if (request.UseTacticalRevealZoom != 0)
                QueueTacticalRevealZoom(camera, entityManager);

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
