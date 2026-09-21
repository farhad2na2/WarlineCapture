using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishResearchEffects
    {
        public static void ApplyCompleted(EntityManager em, Entity session, byte factionId)
        {
            if (!em.HasComponent<SkirmishResearchStateComponent>(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return;
            var research = em.GetComponentData<SkirmishResearchStateComponent>(session);
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            SkirmishRoleOverlay[] overlays = ResolveOverlays(em, session);
            using var query = em.CreateEntityQuery(
                typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishUnitRoleComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (!owned.SessionId.Equals(sessionId) || owned.FactionId != factionId || owned.IsStructure != 0)
                    continue;
                ApplyUnit(em, unit, research, overlays);
            }
        }

        public static void ApplyUnit(
            EntityManager em,
            Entity unit,
            SkirmishResearchStateComponent research,
            SkirmishRoleOverlay[] overlays)
        {
            if (!em.HasComponent<SkirmishUnitRoleComponent>(unit))
                return;
            SkirmishRoleKind role = em.GetComponentData<SkirmishUnitRoleComponent>(unit).Role;
            if (!SkirmishRoleOverlayCatalog.TryGet(overlays, role, out SkirmishRoleOverlay catalog))
                return;
            SkirmishPopulationCategory category = SkirmishRoleIds.Category(role);
            var stamp = em.HasComponent<SkirmishUpgradeStampComponent>(unit)
                ? em.GetComponentData<SkirmishUpgradeStampComponent>(unit)
                : new SkirmishUpgradeStampComponent();
            if (research.InfantryWeapons != 0 && category == SkirmishPopulationCategory.Infantry)
            {
                ApplyInfantryDamage(em, unit, catalog);
                stamp.InfantryWeapons = 1;
            }

            if (research.VehicleProtection != 0 && category == SkirmishPopulationCategory.Ground)
            {
                ApplyVehicleHealth(em, unit, catalog);
                stamp.VehicleProtection = 1;
            }

            if (research.AircraftEfficiency != 0 &&
                category == SkirmishPopulationCategory.Air &&
                stamp.AircraftEfficiency == 0)
            {
                ApplyAircraftFuel(em, unit);
                stamp.AircraftEfficiency = 1;
            }

            if (em.HasComponent<SkirmishUpgradeStampComponent>(unit))
                em.SetComponentData(unit, stamp);
            else
                em.AddComponentData(unit, stamp);
        }

        private static void ApplyInfantryDamage(EntityManager em, Entity unit, SkirmishRoleOverlay catalog)
        {
            int upgraded = SkirmishResearchCosts.Scale(catalog.Damage, SkirmishResearchCosts.InfantryDamagePercent);
            if (em.HasComponent<SkirmishRoleOverlayComponent>(unit))
            {
                var overlay = em.GetComponentData<SkirmishRoleOverlayComponent>(unit);
                overlay.Damage = upgraded;
                em.SetComponentData(unit, overlay);
            }

            if (em.HasComponent<UnitAttack>(unit))
            {
                var attack = em.GetComponentData<UnitAttack>(unit);
                attack.Damage = upgraded;
                em.SetComponentData(unit, attack);
            }
        }

        private static void ApplyVehicleHealth(EntityManager em, Entity unit, SkirmishRoleOverlay catalog)
        {
            int upgradedMax = SkirmishResearchCosts.Scale(catalog.MaxHealth, SkirmishResearchCosts.VehicleHealthPercent);
            if (em.HasComponent<SkirmishRoleOverlayComponent>(unit))
            {
                var overlay = em.GetComponentData<SkirmishRoleOverlayComponent>(unit);
                overlay.MaxHealth = upgradedMax;
                em.SetComponentData(unit, overlay);
            }

            if (!em.HasComponent<UnitHealth>(unit))
                return;
            var health = em.GetComponentData<UnitHealth>(unit);
            int previousMax = health.Max > 0 ? health.Max : catalog.MaxHealth;
            int current = health.Current;
            if (previousMax > 0)
                current = (current * upgradedMax + previousMax / 2) / previousMax;
            health.Max = upgradedMax;
            health.Current = current;
            em.SetComponentData(unit, health);
        }

        private static void ApplyAircraftFuel(EntityManager em, Entity unit)
        {
            if (!em.HasComponent<UnitFuelConsumption>(unit))
                return;
            var fuel = em.GetComponentData<UnitFuelConsumption>(unit);
            fuel.GroundFuelPerCell *= SkirmishResearchCosts.AircraftFuelPercent / 100f;
            fuel.AirFuelPerCell *= SkirmishResearchCosts.AircraftFuelPercent / 100f;
            em.SetComponentData(unit, fuel);
        }

        private static SkirmishRoleOverlay[] ResolveOverlays(EntityManager em, Entity session)
        {
            if (em.HasComponent<SkirmishResolvedSetupRecord>(session) &&
                em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup?.RoleOverlays != null)
                return em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup.RoleOverlays;
            return SkirmishRoleOverlayCatalog.CreateS002GroundSlice();
        }
    }
}
