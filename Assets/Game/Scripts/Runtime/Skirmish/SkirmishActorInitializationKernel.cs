using System;

namespace Game.Runtime
{
    // Only mission-owned health/weapon overlay fields. Baked movement, footprint,
    // timers and other prefab components must be supplied/captured independently.
    [Serializable]
    public struct SkirmishActorOverlayValues
    {
        public int MaxHealth, Damage;
        public float RangeWorld;
        public byte TargetDomains;
    }

    [Serializable]
    public struct SkirmishActorInitializationState
    {
        public bool IsStructure, SharedActor, CombatSuppressed, HasHealth;
        public bool HasUnitAttack, HasBuildingWeapon, HasUnitCombat, HasAirLauncher, HasGroundLauncher;
        public byte Category, AllowedTargets, Domain, Visible, CanAttack, AutoEngage;
        public int HealthCurrent, HealthMax, AttackDamage, BuildingDamage, AirDamage, GroundDamage;
        public float AttackRange, AttackCooldown, BuildingRange, BuildingCooldown;
        public float AirBaseDetectionRange, AirMaxDetectionRange, AirMinRange, AirSupportRangeBonus;
        public float GroundMaxRange, GroundMinRange;
    }

    public static class SkirmishActorInitializationKernel
    {
        // Installed Unity math.min returns its first operand when the second is NaN.
        private static float Min(float a, float b) => float.IsNaN(b) || a < b ? a : b;

        public static void ApplyHealth(ref SkirmishActorInitializationState state, in SkirmishActorOverlayValues overlay)
        {
            state.HasHealth = true;
            state.HealthCurrent = overlay.MaxHealth;
            state.HealthMax = overlay.MaxHealth;
        }

        public static void BindWeapons(ref SkirmishActorInitializationState state, in SkirmishActorOverlayValues overlay, float shotInterval)
        {
            state.AllowedTargets = overlay.TargetDomains;
            // Values are the existing CombatTargetDomain/SkirmishPopulationCategory wire values.
            state.Domain = state.IsStructure ? (byte)4 : state.Category == 3 ? (byte)8 : state.Category == 1 ? (byte)1 : (byte)2;
            state.Visible = 1;
            if (state.HasUnitAttack)
            { state.AttackDamage = overlay.Damage; state.AttackRange = overlay.RangeWorld; state.AttackCooldown = shotInterval; }
            if (state.HasBuildingWeapon)
            { state.BuildingDamage = overlay.Damage; state.BuildingRange = overlay.RangeWorld; state.BuildingCooldown = shotInterval; }
            if (state.HasUnitCombat)
            { state.CanAttack = (byte)(overlay.Damage > 0 ? 1 : 0); state.AutoEngage = 1; }
            if (state.HasAirLauncher)
            {
                state.AirDamage = overlay.Damage;
                state.AirBaseDetectionRange = overlay.RangeWorld;
                state.AirMaxDetectionRange = overlay.RangeWorld;
                // Preserve Unity math.min's ordered comparison including NaN/signed-zero behavior.
                state.AirMinRange = Min(state.AirMinRange, overlay.RangeWorld);
                state.AirSupportRangeBonus = 0f;
            }
            if (state.HasGroundLauncher)
            {
                state.GroundDamage = overlay.Damage;
                state.GroundMaxRange = overlay.RangeWorld;
                float halfRange = overlay.RangeWorld * .5f;
                state.GroundMinRange = Min(state.GroundMinRange, halfRange);
            }
            if (state.SharedActor) state.CombatSuppressed = false;
        }
    }
}
