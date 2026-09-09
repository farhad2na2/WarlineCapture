using Game.Components;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct AICombatOrderSystem
    {
        private bool CanReceiveCombatOrderFromLookups(Entity unit, byte factionId)
        {
            if (unit == Entity.Null ||
                !_entityStorageInfoLookup.Exists(unit) ||
                !_factionLookup.HasComponent(unit) ||
                _factionLookup[unit].Id != factionId ||
                !_aiControlledLookup.HasComponent(unit) ||
                _missionRoleLookup.HasComponent(unit) ||
                !_unitHealthLookup.HasComponent(unit) ||
                _unitHealthLookup[unit].Current <= 0 ||
                !_unitCombatLookup.HasComponent(unit) ||
                !_unitAttackLookup.HasComponent(unit) ||
                !_unitTransformLookup.HasComponent(unit) ||
                _staticGridBlockerLookup.HasComponent(unit))
            {
                return false;
            }

            UnitCombat combat = _unitCombatLookup[unit];
            return combat.CanAttack != 0;
        }

        private static bool CanReceiveCombatOrder(EntityManager em, Entity unit, byte factionId)
        {
            if (unit == Entity.Null ||
                !em.Exists(unit) ||
                !em.HasComponent<Faction>(unit) ||
                em.GetComponentData<Faction>(unit).Id != factionId ||
                !em.HasComponent<AIControlledTag>(unit) ||
                em.HasComponent<CampaignMissionUnitRoleComponent>(unit) ||
                !em.HasComponent<UnitHealth>(unit) ||
                em.GetComponentData<UnitHealth>(unit).Current <= 0 ||
                !em.HasComponent<UnitCombat>(unit) ||
                !em.HasComponent<UnitAttack>(unit) ||
                !em.HasComponent<LocalTransform>(unit) ||
                em.HasComponent<StaticGridBlocker>(unit))
            {
                return false;
            }

            UnitCombat combat = em.GetComponentData<UnitCombat>(unit);
            return combat.CanAttack != 0;
        }

    }
}
