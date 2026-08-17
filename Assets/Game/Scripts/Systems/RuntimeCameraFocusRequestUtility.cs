using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public static class RuntimeCameraFocusRequestUtility
    {
        public const float TacticalRevealHeight = 14f;
        public const float TacticalRevealPitch = 58f;
        public const float TacticalRevealYaw = 10f;
        public const float TacticalRevealFieldOfView = 38f;
        public const float SquadRevealHeight = 12f;
        public const float SquadRevealPitch = 58f;
        public const float SquadRevealFieldOfView = 36f;

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
            EntityManager entityManager,
            bool showSquad = false)
        {
            camera.QueueSetMatchIntroZoomSettlePending(entityManager, false);
            camera.QueueApplyPerspectiveModeInstant(
                entityManager,
                showSquad ? SquadRevealHeight : TacticalRevealHeight,
                showSquad ? SquadRevealPitch : TacticalRevealPitch,
                TacticalRevealYaw,
                showSquad ? SquadRevealFieldOfView : TacticalRevealFieldOfView);
            camera.QueueCompleteZoomTransition(entityManager);
        }

        public static void Queue(
            RtsCameraRequestSystem camera,
            EntityManager entityManager,
            RuntimeCameraFocusRequestComponent request,
            Vector3 focusWorldPosition)
        {
            if (request.UseTacticalRevealZoom != 0)
                QueueTacticalRevealZoom(camera, entityManager, request.UseTacticalRevealZoom == 2);

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
