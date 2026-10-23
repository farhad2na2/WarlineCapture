using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishSharedCombatBinding
    {
        public static SkirmishActorOverlayValues OverlayValues(in SkirmishRoleOverlay overlay) =>
            new SkirmishActorOverlayValues { MaxHealth=overlay.MaxHealth, Damage=overlay.Damage,
                RangeWorld=overlay.RangeWorld, TargetDomains=(byte)overlay.TargetDomains };

        public static SkirmishActorInitializationState CaptureState(EntityManager em, Entity entity)
        {
            var state = new SkirmishActorInitializationState
            {
                IsStructure=em.HasComponent<SkirmishStructureIdentityComponent>(entity),
                SharedActor=em.HasComponent<SkirmishSharedActorTag>(entity),
                CombatSuppressed=em.HasComponent<CampaignMissionCombatSuppressedTag>(entity),
                HasHealth=em.HasComponent<UnitHealth>(entity),
                Category=em.HasComponent<SkirmishUnitRoleComponent>(entity) ? (byte)em.GetComponentData<SkirmishUnitRoleComponent>(entity).Category : (byte)0,
                HasUnitAttack=em.HasComponent<UnitAttack>(entity), HasBuildingWeapon=em.HasComponent<BuildingDefenseWeapon>(entity),
                HasUnitCombat=em.HasComponent<UnitCombat>(entity), HasAirLauncher=em.HasComponent<AirMissileLauncherComponent>(entity),
                HasGroundLauncher=em.HasComponent<GroundMissileLauncherComponent>(entity)
            };
            if(state.HasHealth){var v=em.GetComponentData<UnitHealth>(entity);state.HealthCurrent=v.Current;state.HealthMax=v.Max;}
            if(em.HasComponent<CombatTargetPolicy>(entity)){var v=em.GetComponentData<CombatTargetPolicy>(entity);state.AllowedTargets=(byte)v.AllowedTargets;state.Domain=(byte)v.Domain;state.Visible=v.Visible;}
            if(state.HasUnitAttack){var v=em.GetComponentData<UnitAttack>(entity);state.AttackDamage=v.Damage;state.AttackRange=v.Range;state.AttackCooldown=v.CooldownSeconds;}
            if(state.HasBuildingWeapon){var v=em.GetComponentData<BuildingDefenseWeapon>(entity);state.BuildingDamage=v.Damage;state.BuildingRange=v.Range;state.BuildingCooldown=v.CooldownSeconds;}
            if(state.HasUnitCombat){var v=em.GetComponentData<UnitCombat>(entity);state.CanAttack=v.CanAttack;state.AutoEngage=v.AutoEngage;}
            if(state.HasAirLauncher){var v=em.GetComponentData<AirMissileLauncherComponent>(entity);state.AirDamage=v.AirTargetDamage;state.AirBaseDetectionRange=v.BaseDetectionRange;state.AirMaxDetectionRange=v.MaxDetectionRange;state.AirMinRange=v.MinRange;state.AirSupportRangeBonus=v.MaxSupportRangeBonus;}
            if(state.HasGroundLauncher){var v=em.GetComponentData<GroundMissileLauncherComponent>(entity);state.GroundDamage=v.Damage;state.GroundMaxRange=v.MaxRange;state.GroundMinRange=v.MinRange;}
            return state;
        }

        public static void Bind(EntityManager em, Entity entity, SkirmishRoleOverlay overlay)
        {
            var state=CaptureState(em,entity);
            var values=OverlayValues(overlay);
            SkirmishActorInitializationKernel.BindWeapons(ref state,values,SkirmishExpandedEngagementService.ShotIntervalSeconds);
            var policy=new CombatTargetPolicy {AllowedTargets=(CombatTargetDomain)state.AllowedTargets,Domain=(CombatTargetDomain)state.Domain,Visible=state.Visible};
            if(em.HasComponent<CombatTargetPolicy>(entity))em.SetComponentData(entity,policy);else em.AddComponentData(entity,policy);
            if(state.HasUnitAttack){var v=em.GetComponentData<UnitAttack>(entity);v.Damage=state.AttackDamage;v.Range=state.AttackRange;v.CooldownSeconds=state.AttackCooldown;em.SetComponentData(entity,v);}
            if(state.HasBuildingWeapon){var v=em.GetComponentData<BuildingDefenseWeapon>(entity);v.Damage=state.BuildingDamage;v.Range=state.BuildingRange;v.CooldownSeconds=state.BuildingCooldown;em.SetComponentData(entity,v);}
            if(state.HasUnitCombat){var v=em.GetComponentData<UnitCombat>(entity);v.CanAttack=state.CanAttack;v.AutoEngage=state.AutoEngage;em.SetComponentData(entity,v);}
            if(state.HasAirLauncher){var v=em.GetComponentData<AirMissileLauncherComponent>(entity);v.AirTargetDamage=state.AirDamage;v.BaseDetectionRange=state.AirBaseDetectionRange;v.MaxDetectionRange=state.AirMaxDetectionRange;v.MinRange=state.AirMinRange;v.MaxSupportRangeBonus=state.AirSupportRangeBonus;em.SetComponentData(entity,v);}
            if(state.HasGroundLauncher){var v=em.GetComponentData<GroundMissileLauncherComponent>(entity);v.Damage=state.GroundDamage;v.MaxRange=state.GroundMaxRange;v.MinRange=state.GroundMinRange;em.SetComponentData(entity,v);}
            if(UsesSharedCommands(em,entity))em.RemoveComponent<CampaignMissionCombatSuppressedTag>(entity);
        }

        public static bool UsesSharedCommands(EntityManager em, Entity entity) =>
            em.HasComponent<CombatTargetPolicy>(entity) && em.HasComponent<SkirmishSharedActorTag>(entity);
    }
}
