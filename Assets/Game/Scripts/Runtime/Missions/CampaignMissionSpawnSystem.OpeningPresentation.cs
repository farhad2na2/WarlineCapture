using Game.Components;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        private static readonly FixedString64Bytes EstablishBaseMissionId =
            "saga.ch01.m02.establish_base";
        private static readonly FixedString64Bytes EstablishBaseSweepStartAnchorId =
            "anchor.ch01.m02.resource_focus";
        private static readonly FixedString64Bytes EstablishBaseSweepEndAnchorId =
            "anchor.ch01.m02.build_lot";

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
                !TryFindAnchor(ref map, EstablishBaseSweepStartAnchorId, out OperationMapAnchorBlob start) ||
                !TryFindAnchor(ref map, EstablishBaseSweepEndAnchorId, out OperationMapAnchorBlob end))
                return;

            openingStartFocus = start.Position;
            openingEndFocus = end.Position;
            establishingFocus = math.lerp(start.Position, end.Position, 0.5f);
        }
    }
}
