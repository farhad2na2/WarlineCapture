using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishSharedCombatBinding
    {
        public static void Bind(EntityManager em, Entity entity, SkirmishRoleOverlay overlay)
        {
            bool structure = em.HasComponent<SkirmishStructureIdentityComponent>(entity);
            var category = em.HasComponent<SkirmishUnitRoleComponent>(entity)
                ? em.GetComponentData<SkirmishUnitRoleComponent>(entity).Category : SkirmishPopulationCategory.None;
            var policy = new CombatTargetPolicy
            {
                AllowedTargets = (CombatTargetDomain)(byte)overlay.TargetDomains,
                Domain = structure ? CombatTargetDomain.Structure : category == SkirmishPopulationCategory.Air
                    ? CombatTargetDomain.Air : category == SkirmishPopulationCategory.Infantry
                        ? CombatTargetDomain.Infantry : CombatTargetDomain.Ground,
                Visible = (byte)(SkirmishFogService.IsVisible(em, entity) ? 1 : 0)
            };
            if (em.HasComponent<CombatTargetPolicy>(entity)) em.SetComponentData(entity, policy);
            else em.AddComponentData(entity, policy);
            if (em.HasComponent<UnitAttack>(entity))
            {
                var attack = em.GetComponentData<UnitAttack>(entity);
                attack.Damage = overlay.Damage;
                attack.Range = overlay.RangeWorld;
                attack.CooldownSeconds = SkirmishExpandedEngagementService.ShotIntervalSeconds;
                em.SetComponentData(entity, attack);
            }
            if (em.HasComponent<BuildingDefenseWeapon>(entity))
            {
                // BuildingDefenseAttackSystem owns native tower shots. Updating only
                // UnitAttack leaves its actual range and fire rate unchanged.
                var weapon = em.GetComponentData<BuildingDefenseWeapon>(entity);
                weapon.Damage = overlay.Damage;
                weapon.Range = overlay.RangeWorld;
                weapon.CooldownSeconds = SkirmishExpandedEngagementService.ShotIntervalSeconds;
                em.SetComponentData(entity, weapon);
            }
            if (em.HasComponent<UnitCombat>(entity))
            {
                var combat = em.GetComponentData<UnitCombat>(entity);
                combat.CanAttack = (byte)(overlay.Damage > 0 ? 1 : 0);
                combat.AutoEngage = 1;
                em.SetComponentData(entity, combat);
            }
            if (em.HasComponent<AirMissileLauncherComponent>(entity))
            {
                // This unit fires through the missile owner, not UnitAttackSystem.
                var launcher = em.GetComponentData<AirMissileLauncherComponent>(entity);
                launcher.AirTargetDamage = overlay.Damage;
                launcher.BaseDetectionRange = overlay.RangeWorld;
                launcher.MaxDetectionRange = overlay.RangeWorld;
                launcher.MinRange = Unity.Mathematics.math.min(launcher.MinRange, overlay.RangeWorld);
                launcher.MaxSupportRangeBonus = 0f;
                em.SetComponentData(entity, launcher);
            }
            if (UsesSharedCommands(em, entity))
                em.RemoveComponent<CampaignMissionCombatSuppressedTag>(entity);
        }

        public static bool UsesSharedCommands(EntityManager em, Entity entity) =>
            em.HasComponent<CombatTargetPolicy>(entity) && em.HasComponent<SkirmishSharedActorTag>(entity);
    }
}
