using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionPatrolOrderSystem
    {
        internal const float GuidedMoveCameraFocusTowardHostiles = 0.35f;
        private EntityQuery _guidedMoveQuery;

        private void CreateGuidedMoveCameraQuery(ref SystemState state)
        {
            _guidedMoveQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CampaignMissionGuidedMoveInProgressTag>(),
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>());
        }

        private bool TryQueueGuidedMoveCamera(
            ref SystemState state,
            Entity focusEntity,
            in RuntimeCameraFocusRequestComponent focus,
            in CampaignMissionRuntimeComponent runtime,
            OperationMapMetadataComponent metadata,
            ref CampaignMissionOpeningPresentationComponent opening)
        {
            if (opening.Stage != 6 || opening.GuidedMoveCameraRequested != 0 || focus.Requested != 0 ||
                runtime.Phase != MissionPhaseKind.MoveToCover || _guidedMoveQuery.IsEmptyIgnoreFilter)
            {
                return false;
            }

            bool activeSessionMove = false;
            using (NativeArray<CampaignMissionUnitRoleComponent> roles =
                   _guidedMoveQuery.ToComponentDataArray<CampaignMissionUnitRoleComponent>(Allocator.Temp))
            {
                for (int index = 0; index < roles.Length; index++)
                {
                    if (!roles[index].SessionToken.Equals(runtime.SessionToken))
                        continue;
                    activeSessionMove = true;
                    break;
                }
            }

            if (!activeSessionMove || !CampaignMissionSpawnSystem.TryFindAnchor(
                    ref metadata.Blob.Value,
                    CampaignMissionGuidedMoveRouteUtility.AuthoredMoveTargetAnchorId,
                    out OperationMapAnchorBlob moveTarget))
            {
                return false;
            }

            state.EntityManager.SetComponentData(focusEntity, new RuntimeCameraFocusRequestComponent
            {
                Requested = 1,
                Smooth = 1,
                UseTacticalRevealZoom = 4,
                SmoothTimeSeconds = CinematicGlideSmoothTimeSeconds,
                World = ComputeGuidedMoveCameraFocus(moveTarget.Position, opening.HostileFocus)
            });
            opening.GuidedMoveCameraRequested = 1;
            return true;
        }

        internal static float3 ComputeGuidedMoveCameraFocus(float3 moveTarget, float3 hostileFocus) =>
            math.lerp(moveTarget, hostileFocus, GuidedMoveCameraFocusTowardHostiles);
    }
}
