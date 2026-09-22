using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    /// <summary>
    /// Applies authored overlay damage when an attack order is in range.
    /// Combatants met on the way are fought before the ordered structure.
    /// Hidden contacts and out-of-domain targets take no damage.
    /// </summary>
    public static class SkirmishExpandedEngagementService
    {
        public const float ShotIntervalSeconds = 1f;

        public static int Step(EntityManager em, Entity session, float deltaSeconds, bool paused)
        {
            if (paused || deltaSeconds <= 0f || !em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return 0;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(
                typeof(SkirmishMoveIntentComponent),
                typeof(SkirmishAttemptOwnedComponent),
                typeof(LocalTransform));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            int shots = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                if (!em.Exists(unit) || !em.HasComponent<SkirmishMoveIntentComponent>(unit))
                    continue;
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (!owned.SessionId.Equals(sessionId) || owned.IsStructure != 0)
                    continue;
                var intent = em.GetComponentData<SkirmishMoveIntentComponent>(unit);
                if (em.HasComponent<HoldPositionOrderTag>(unit) || intent.Order != SkirmishGroupOrderKind.Attack)
                {
                    if (intent.Engaged != 0)
                    {
                        intent.Engaged = 0;
                        em.SetComponentData(unit, intent);
                    }

                    continue;
                }

                if (!CanShoot(em, unit, out SkirmishRoleOverlayComponent overlay))
                {
                    intent.Engaged = 0;
                    em.SetComponentData(unit, intent);
                    continue;
                }

                Entity combatant = FindCombatant(em, sessionId, unit, owned.FactionId, overlay);
                intent.Engaged = combatant != Entity.Null ? (byte)1 : (byte)0;
                intent.Cooldown -= deltaSeconds;
                Entity victim = combatant;
                if (victim == Entity.Null)
                    victim = OrderedStructure(em, intent.AttackTarget, owned.FactionId, unit, overlay);
                if (victim != Entity.Null && intent.Cooldown <= 0f)
                {
                    ApplyShot(em, victim, overlay.Damage);
                    intent.Cooldown = ShotIntervalSeconds;
                    shots++;
                }
                else if (intent.Cooldown < 0f)
                {
                    intent.Cooldown = 0f;
                }

                if (em.Exists(unit) && em.HasComponent<SkirmishMoveIntentComponent>(unit))
                    em.SetComponentData(unit, intent);
            }

            return shots;
        }

        public static bool DomainAllows(
            SkirmishTargetDomain domains,
            bool structure,
            SkirmishPopulationCategory category)
        {
            if (structure)
                return (domains & SkirmishTargetDomain.Structure) != 0;
            if (category == SkirmishPopulationCategory.Infantry)
                return (domains & SkirmishTargetDomain.Infantry) != 0;
            if (category == SkirmishPopulationCategory.Ground)
                return (domains & SkirmishTargetDomain.Ground) != 0;
            if (category == SkirmishPopulationCategory.Air)
                return (domains & SkirmishTargetDomain.Air) != 0;
            return false;
        }

        private static bool CanShoot(EntityManager em, Entity unit, out SkirmishRoleOverlayComponent overlay)
        {
            overlay = default;
            if (!SkirmishArmyGroupSystem.IsAlive(em, unit) ||
                !em.HasComponent<SkirmishRoleOverlayComponent>(unit) ||
                !em.HasComponent<LocalTransform>(unit))
                return false;
            overlay = em.GetComponentData<SkirmishRoleOverlayComponent>(unit);
            return overlay.Damage > 0 && overlay.RangeWorld > 0f;
        }

        private static Entity FindCombatant(
            EntityManager em,
            FixedString64Bytes sessionId,
            Entity shooter,
            byte factionId,
            SkirmishRoleOverlayComponent overlay)
        {
            float3 origin = em.GetComponentData<LocalTransform>(shooter).Position;
            float best = overlay.RangeWorld * overlay.RangeWorld;
            Entity chosen = Entity.Null;
            using var query = em.CreateEntityQuery(
                typeof(SkirmishAttemptOwnedComponent),
                typeof(LocalTransform),
                typeof(UnitHealth));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity candidate = entities[i];
                if (candidate == shooter)
                    continue;
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(candidate);
                if (!owned.SessionId.Equals(sessionId) ||
                    owned.FactionId == factionId ||
                    owned.IsStructure != 0)
                    continue;
                if (!SkirmishFogService.IsVisible(em, candidate) || !SkirmishArmyGroupSystem.IsAlive(em, candidate))
                    continue;
                if (!em.HasComponent<SkirmishUnitRoleComponent>(candidate))
                    continue;
                SkirmishPopulationCategory category = em.GetComponentData<SkirmishUnitRoleComponent>(candidate).Category;
                if (!DomainAllows(overlay.TargetDomains, false, category))
                    continue;
                float distance = math.distancesq(
                    origin.xz,
                    em.GetComponentData<LocalTransform>(candidate).Position.xz);
                if (distance > best)
                    continue;
                best = distance;
                chosen = candidate;
            }

            return chosen;
        }

        private static Entity OrderedStructure(
            EntityManager em,
            Entity target,
            byte factionId,
            Entity shooter,
            SkirmishRoleOverlayComponent overlay)
        {
            if (target == Entity.Null || !em.Exists(target) || !em.HasComponent<SkirmishAttemptOwnedComponent>(target))
                return Entity.Null;
            var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(target);
            if (owned.FactionId == factionId || owned.IsStructure == 0)
                return Entity.Null;
            if (!SkirmishFogService.IsVisible(em, target) || !SkirmishArmyGroupSystem.IsAlive(em, target))
                return Entity.Null;
            if (!DomainAllows(overlay.TargetDomains, true, SkirmishPopulationCategory.None))
                return Entity.Null;
            if (!em.HasComponent<LocalTransform>(target) || !em.HasComponent<LocalTransform>(shooter))
                return Entity.Null;
            float distance = math.distancesq(
                em.GetComponentData<LocalTransform>(shooter).Position.xz,
                em.GetComponentData<LocalTransform>(target).Position.xz);
            if (distance > overlay.RangeWorld * overlay.RangeWorld)
                return Entity.Null;
            return target;
        }

        private static void ApplyShot(EntityManager em, Entity victim, int damage)
        {
            if (damage <= 0 || !em.HasComponent<UnitHealth>(victim))
                return;
            var health = em.GetComponentData<UnitHealth>(victim);
            health.Current = health.Current - damage;
            if (health.Current < 0)
                health.Current = 0;
            em.SetComponentData(victim, health);
        }
    }
}
