using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    internal static class CampaignMissionFinaleCameraUtility
    {
        internal static bool TryComputeLiveCombatFocus(
            EntityManager entityManager,
            EntityQuery missionCombatantsQuery,
            in FixedString64Bytes sessionToken,
            out float3 friendlyFocus,
            out float3 hostileFocus) =>
            TryComputeFocus(
                entityManager,
                missionCombatantsQuery,
                sessionToken,
                false,
                out friendlyFocus,
                out hostileFocus);

        internal static bool TryComputeCasualtyFocus(
            EntityManager entityManager,
            EntityQuery missionCombatantsQuery,
            in FixedString64Bytes sessionToken,
            out float3 friendlyFocus,
            out float3 casualtyFocus) =>
            TryComputeFocus(
                entityManager,
                missionCombatantsQuery,
                sessionToken,
                true,
                out friendlyFocus,
                out casualtyFocus);

        private static bool TryComputeFocus(
            EntityManager entityManager,
            EntityQuery missionCombatantsQuery,
            in FixedString64Bytes sessionToken,
            bool requireDeadHostiles,
            out float3 friendlyFocus,
            out float3 hostileFocus)
        {
            friendlyFocus = float3.zero;
            hostileFocus = float3.zero;
            int friendlyCount = 0;
            int hostileCount = 0;
            using NativeArray<Entity> combatants =
                missionCombatantsQuery.ToEntityArray(Allocator.Temp);
            for (int index = 0; index < combatants.Length; index++)
            {
                Entity entity = combatants[index];
                CampaignMissionUnitRoleComponent role =
                    entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(entity);
                if (!role.SessionToken.Equals(sessionToken))
                    continue;

                float3 position = entityManager.GetComponentData<LocalTransform>(entity).Position;
                if (!math.all(math.isfinite(position)))
                    continue;

                Faction faction = entityManager.GetComponentData<Faction>(entity);
                UnitHealth health = entityManager.GetComponentData<UnitHealth>(entity);
                if (FactionIdentity.IsPlayerControlled(faction.Id) && health.Current > 0)
                {
                    friendlyFocus += position;
                    friendlyCount++;
                }
                else if (!FactionIdentity.IsPlayerControlled(faction.Id) &&
                         (requireDeadHostiles ? health.Current <= 0 : health.Current > 0))
                {
                    hostileFocus += position;
                    hostileCount++;
                }
            }

            if (friendlyCount == 0 || hostileCount == 0)
                return false;

            friendlyFocus /= friendlyCount;
            hostileFocus /= hostileCount;
            return true;
        }
    }
}
