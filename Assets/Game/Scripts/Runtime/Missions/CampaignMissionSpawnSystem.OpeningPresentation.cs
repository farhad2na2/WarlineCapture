using Game.Components;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        private static readonly FixedString64Bytes EstablishBaseMissionId =
            "saga.ch01.m02.establish_base";
        private static readonly FixedString64Bytes EstablishBaseCameraStartAnchorId =
            "anchor.ch01.m02.camera_start";
        private static readonly FixedString64Bytes EstablishBaseForwardPostAnchorId =
            "anchor.ch01.m02.forward_post";
        private static readonly FixedString64Bytes EstablishBaseBuildLotAnchorId =
            "anchor.ch01.m02.build_lot";
        internal const float EstablishBaseFocusTowardBuildLot = 0.56f;

        internal static void ResolveOpeningPresentationFocus(
            in FixedString64Bytes missionId,
            ref OperationMapBlob map,
            float3 playerFocus,
            float3 hostileFocus,
            out float3 openingStartFocus,
            out float3 openingEndFocus,
            out float3 establishingFocus)
        {
            openingStartFocus = playerFocus;
            openingEndFocus = hostileFocus;
            establishingFocus = math.lerp(playerFocus, hostileFocus, 0.40f);
            if (!missionId.Equals(EstablishBaseMissionId) ||
                !TryFindAnchor(ref map, EstablishBaseCameraStartAnchorId, out OperationMapAnchorBlob cameraStart) ||
                !TryFindAnchor(ref map, EstablishBaseForwardPostAnchorId, out OperationMapAnchorBlob forwardPost) ||
                !TryFindAnchor(ref map, EstablishBaseBuildLotAnchorId, out OperationMapAnchorBlob buildLot))
                return;

            openingStartFocus = cameraStart.Position;
            openingEndFocus = buildLot.Position;
            establishingFocus = math.lerp(
                forwardPost.Position,
                buildLot.Position,
                EstablishBaseFocusTowardBuildLot);
        }
    }
}
