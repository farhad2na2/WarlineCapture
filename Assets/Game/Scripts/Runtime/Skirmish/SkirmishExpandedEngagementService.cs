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
    /// Combatants met on the way are fought until the ordered structure is in range.
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

                // Registry-spawned units already acquire and damage combatants
                // through the shared engagement/attack systems. Applying the
                // expansion overlay as well made every encounter deal two hits.
                bool sharedCombat = em.HasComponent<SkirmishSharedActorTag>(unit);
                // Production units use shared combat for structures as well as units.
                // This fixture-only damage path must never supplement a real weapon.
                if (sharedCombat)
                {
                    IssueCommandedSiegeAttack(em, unit, intent.AttackTarget, sessionId, owned.FactionId, overlay);
                    continue;
                }
                Entity combatant = FindCombatant(em, sessionId, unit, owned.FactionId, overlay);
                Entity structure = OrderedStructure(em, intent.AttackTarget, owned.FactionId, unit, overlay);
                // A combatant on the way is fought only until the ordered structure
                // is itself in range. Stopping for every rifle at the enemy pad
                // kept tanks outside Barracks range for the whole match.
                intent.Engaged = structure == Entity.Null && combatant != Entity.Null ? (byte)1 : (byte)0;
                intent.Cooldown -= deltaSeconds;
                Entity victim = structure != Entity.Null ? structure : combatant;
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

        private static void IssueCommandedSiegeAttack(
            EntityManager em, Entity unit, Entity target, FixedString64Bytes sessionId,
            byte factionId, SkirmishRoleOverlayComponent overlay)
        {
            // Native launchers deliberately do not auto-acquire attack-move contacts.
            // Forward only the target already accepted by the mission's Attack command;
            // the native weapon continues to own preparation, projectiles and damage.
            if (!em.HasComponent<SkirmishUnitRoleComponent>(unit) ||
                em.GetComponentData<SkirmishUnitRoleComponent>(unit).Role != SkirmishRoleKind.Siege ||
                !em.HasComponent<GroundMissileLauncherComponent>(unit) ||
                target == Entity.Null || !em.Exists(target) ||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(target) ||
                !em.HasComponent<LocalTransform>(target) ||
                !SkirmishArmyGroupSystem.IsAlive(em, target) || !SkirmishFogService.IsVisibleToFaction(em, target, factionId))
                return;
            var targetOwner = em.GetComponentData<SkirmishAttemptOwnedComponent>(target);
            if (!targetOwner.SessionId.Equals(sessionId) || targetOwner.FactionId == 0 ||
                targetOwner.FactionId == factionId)
                return;
            var category = em.HasComponent<SkirmishUnitRoleComponent>(target)
                ? em.GetComponentData<SkirmishUnitRoleComponent>(target).Category
                : SkirmishPopulationCategory.None;
            if (!DomainAllows(overlay.TargetDomains, targetOwner.IsStructure != 0, category))
                return;
            float3 position = em.GetComponentData<LocalTransform>(target).Position;
            var weapon = em.GetComponentData<GroundMissileLauncherComponent>(unit);
            float distance = math.distance(em.GetComponentData<LocalTransform>(unit).Position.xz, position.xz);
            float minRange = math.max(0f, weapon.MinRange);
            if (!math.isfinite(distance) || distance < minRange ||
                distance > math.min(overlay.RangeWorld, math.max(minRange, weapon.MaxRange)))
                return;
            if (em.HasComponent<EngageTarget>(unit) &&
                em.GetComponentData<EngageTarget>(unit).Target == target)
                return;
            if (em.HasComponent<GroundMissileInFlightComponent>(unit)) return;
            if (em.HasComponent<GroundMissileLauncherStateComponent>(unit))
            {
                byte phase = em.GetComponentData<GroundMissileLauncherStateComponent>(unit).Phase;
                if (phase == (byte)GroundMissileLauncherPhase.Preparing ||
                    phase == (byte)GroundMissileLauncherPhase.Launching) return;
            }
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.CalculateEntityCount() != 1) return;
            UnitAttackOrderRequestSystem.EnqueueAndProcessDirectAttackTarget(
                em, unit, target, GridUtils.WorldToCell(grids.GetSingleton<GridConfig>(), position), position);
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
                if (!SkirmishFogService.IsVisibleToFaction(em, candidate, factionId) || !SkirmishArmyGroupSystem.IsAlive(em, candidate))
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
            if (!SkirmishFogService.IsVisibleToFaction(em, target, factionId) || !SkirmishArmyGroupSystem.IsAlive(em, target))
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
