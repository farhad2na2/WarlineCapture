using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public static class RuntimeCameraFocusRequestUtility
    {
        // Mission introductions need enough visual travel time for players to understand
        // where the camera is taking them. Keep this shared floor so newly-authored tours
        // cannot accidentally ship with the near-snap timings used by utility camera jumps.
        public const float MinimumMissionIntroSmoothTimeSeconds = 2.25f;

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
        public const float CombatRevealHeight = 12f;
        public const float CombatRevealPitch = 30f;
        public const float CombatRevealFieldOfView = 38f;

        public static float EnsureMissionIntroSmoothTime(float smoothTimeSeconds) =>
            Mathf.Max(MinimumMissionIntroSmoothTimeSeconds, smoothTimeSeconds);

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
            Game.Simulation.CameraFocusSequence.Tactical(revealKind, smoothTimeSeconds, rtsHeight, rtsPitch,
                rtsYaw, rtsFieldOfView, useExplicitYaw, explicitYaw, action => Dispatch(camera, entityManager, action));
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
            var p = request.Perspective;
            var input = new Game.Simulation.CameraFocusRequest {
                ExplicitPerspective=request.UseExplicitPerspective!=0, Smooth=request.Smooth!=0,
                TacticalReveal=request.UseTacticalRevealZoom, ExplicitYaw=request.UseExplicitYaw!=0,
                Height=p.x, Pitch=p.y, Yaw=p.z, FieldOfView=p.w, Duration=request.SmoothTimeSeconds,
                FocusX=focusWorldPosition.x, FocusY=focusWorldPosition.y, FocusZ=focusWorldPosition.z,
                RevealYaw=request.YawDegrees, NormalHeight=rtsHeight, NormalPitch=rtsPitch,
                NormalYaw=rtsYaw, NormalFieldOfView=rtsFieldOfView };
            Game.Simulation.CameraFocusSequence.Emit(input, action => Dispatch(camera, entityManager, action));
        }

        private static void Dispatch(RtsCameraRequestSystem camera, EntityManager em, Game.Simulation.CameraFocusAction action)
        {
            switch (action.Kind)
            {
                case Game.Simulation.CameraFocusActionKind.ClearIntroSettle: camera.QueueSetMatchIntroZoomSettlePending(em, false); break;
                case Game.Simulation.CameraFocusActionKind.ClearPerspective: camera.QueueClearSmoothPerspectiveTarget(em); break;
                case Game.Simulation.CameraFocusActionKind.CompleteZoom: camera.QueueCompleteZoomTransition(em); break;
                case Game.Simulation.CameraFocusActionKind.InstantPerspective: camera.QueueApplyPerspectiveModeInstant(em, action.A, action.B, action.C, action.D); break;
                case Game.Simulation.CameraFocusActionKind.SmoothPerspective: camera.QueueSetSmoothPerspectiveTarget(em, action.A, action.B, action.C, action.D, action.Duration, true); break;
                case Game.Simulation.CameraFocusActionKind.MoveFocus: camera.QueueMoveGroundCenterTo(em, new Vector3(action.A, action.B, action.C)); break;
                case Game.Simulation.CameraFocusActionKind.SmoothFocus: camera.QueueSetSmoothFocusTarget(em, new Vector3(action.A, action.B, action.C), true, action.Duration); break;
                case Game.Simulation.CameraFocusActionKind.ClearFocus: camera.QueueClearSmoothFocusTarget(em); break;
                default: throw new System.InvalidOperationException("Unknown focus action");
            }
        }

    }
}
