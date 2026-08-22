using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public static class RuntimeCameraFocusRequestUtility
    {
        public const float TacticalRevealHeight = 9f;
        public const float TacticalRevealPitch = 30f;
        public const float TacticalRevealYaw = 0f;
        public const float TacticalRevealFieldOfView = 38f;
        public const float SquadRevealHeight = 9f;
        public const float SquadRevealPitch = 30f;
        public const float SquadRevealFieldOfView = 42f;
        public const float BazaarEstablishingHeight = 15f;
        public const float BazaarEstablishingPitch = 35f;
        public const float BazaarEstablishingYaw = 0f;
        public const float BazaarEstablishingFieldOfView = 50f;
        public const float CombatRevealHeight = 5.5f;
        public const float CombatRevealPitch = 27f;
        public const float CombatRevealFieldOfView = 38f;

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
            byte revealKind = 1,
            float smoothTimeSeconds = 0f,
            float rtsHeight = 24f,
            float rtsPitch = 58f,
            float rtsYaw = 10f,
            float rtsFieldOfView = 36f,
            bool useExplicitYaw = false,
            float explicitYaw = 0f)
        {
            camera.QueueSetMatchIntroZoomSettlePending(entityManager, false);
            bool showSquad = revealKind == 2;
            bool showBazaar = revealKind == 3;
            bool restoreRts = revealKind == 4;
            bool showCombat = revealKind == 5;
            float height = restoreRts ? rtsHeight : showBazaar ? BazaarEstablishingHeight :
                showCombat ? CombatRevealHeight : showSquad ? SquadRevealHeight : TacticalRevealHeight;
            float pitch = restoreRts ? rtsPitch : showBazaar ? BazaarEstablishingPitch :
                showCombat ? CombatRevealPitch : showSquad ? SquadRevealPitch : TacticalRevealPitch;
            float yaw = useExplicitYaw ? explicitYaw :
                restoreRts ? rtsYaw : showBazaar ? BazaarEstablishingYaw : TacticalRevealYaw;
            float fieldOfView = restoreRts ? rtsFieldOfView : showBazaar ? BazaarEstablishingFieldOfView :
                showCombat ? CombatRevealFieldOfView :
                showSquad ? SquadRevealFieldOfView : TacticalRevealFieldOfView;
            if (smoothTimeSeconds > 0f)
            {
                camera.QueueCompleteZoomTransition(entityManager);
                camera.QueueSetSmoothPerspectiveTarget(
                    entityManager,
                    height,
                    pitch,
                    yaw,
                    fieldOfView,
                    smoothTimeSeconds,
                    true);
            }
            else
            {
                camera.QueueApplyPerspectiveModeInstant(
                    entityManager,
                    height,
                    pitch,
                    yaw,
                    fieldOfView);
                camera.QueueCompleteZoomTransition(entityManager);
            }
        }

        public static void Queue(
            RtsCameraRequestSystem camera,
            EntityManager entityManager,
            RuntimeCameraFocusRequestComponent request,
            Vector3 focusWorldPosition,
            float rtsHeight,
            float rtsPitch,
            float rtsYaw,
            float rtsFieldOfView)
        {
            if (request.UseTacticalRevealZoom != 0)
                QueueTacticalRevealZoom(
                    camera,
                    entityManager,
                    request.UseTacticalRevealZoom,
                    request.Smooth != 0 ? Mathf.Max(0.6f, request.SmoothTimeSeconds) : 0f,
                    rtsHeight,
                    rtsPitch,
                    rtsYaw,
                    rtsFieldOfView,
                    request.UseExplicitYaw != 0,
                    request.YawDegrees);

            if (request.Smooth != 0)
            {
                camera.QueueSetSmoothFocusTarget(
                    entityManager,
                    focusWorldPosition,
                    true,
                    request.SmoothTimeSeconds);
                return;
            }

            camera.QueueMoveGroundCenterTo(entityManager, focusWorldPosition);
            camera.QueueClearSmoothFocusTarget(entityManager);
        }
    }
}
