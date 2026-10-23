#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using Game.Components;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct InfantryPathOriginalMoveJob
    {
        internal static bool ShouldUseGroupedManualStop(
            bool isVehicle,
            bool campaignGuidedMove,
            bool manualMoveGroupMember,
            bool hasBoardingTarget) =>
            Movement.GridPathMovementKernel.ShouldUseGroupedManualStop(
                isVehicle, campaignGuidedMove, manualMoveGroupMember, hasBoardingTarget);

        private static bool IsSoftBlocker(int2 size)
        {
            int2 clamped = UnitFootprintUtility.ClampSize(size);
            return clamped.x == 1 && clamped.y == 1;
        }

        public static bool CanOccupyMovementTarget(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray dynamicBlocked,
            in NativeArray<byte> friendlyPassFactionIds,
            int2 targetCell,
            int2 footprintSize,
            int2 currentCell,
            byte factionId)
        {
            return UnitFootprintUtility.CanPlace(
                grid,
                walkable,
                dynamicBlocked,
                friendlyPassFactionIds,
                default,
                targetCell,
                footprintSize,
                currentCell,
                factionId);
        }

        private bool IsBlockedForFaction(int index, byte factionId)
        {
            if (!DynamicBlocked.IsCreated || !DynamicBlocked.IsSet(index))
                return false;

            if (FriendlyPassFactionIds.IsCreated &&
                (uint)index < (uint)FriendlyPassFactionIds.Length &&
                FriendlyPassFactionIds[index] == factionId)
            {
                return false;
            }

            return true;
        }
    }
}
#endif
