using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public partial struct RtsSelectionImmediateSelectedUnitCommandSystem
    {
        private static bool ProcessDestroyFocusedUnit(
            EntityManager em,
            EntityQuery selectedQuery,
            Entity focusedUnit,
            out bool accepted,
            out TacticalCommandReasonCode rejectionReason,
            out int issuedCount)
        {
            accepted = false;
            rejectionReason = TacticalCommandReasonCode.None;
            issuedCount = 0;

            if (focusedUnit != Entity.Null && em.Exists(focusedUnit))
            {
                if (!IsPlayerControlled(em, focusedUnit) || em.HasComponent<CampaignMissionUnitRoleComponent>(focusedUnit))
                {
                    rejectionReason = TacticalCommandReasonCode.TargetNotAttackable;
                    return true;
                }

                DestroyUnit(em, focusedUnit);
                accepted = true;
                issuedCount = 1;
                return true;
            }

            if (!selectedQuery.IsEmptyIgnoreFilter)
            {
                using NativeList<Entity> selectedEntities = CollectSelectedPlayerUnits(em, selectedQuery, Allocator.Temp);
                for (int i = 0; i < selectedEntities.Length; i++)
                {
                    Entity entity = selectedEntities[i];
                    if(em.HasComponent<CampaignMissionUnitRoleComponent>(entity)) continue;
                    DestroyUnit(em, entity);
                    issuedCount++;
                }
            }

            if (issuedCount <= 0)
            {
                rejectionReason = TacticalCommandReasonCode.NoSelection;
                return true;
            }

            accepted = true;
            return true;
        }
    }
}
