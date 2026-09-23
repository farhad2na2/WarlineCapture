using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    public static class SkirmishWorldMovementService
    {
        public const float InfantryMetersPerSecond = 4f;
        public const float GroundMetersPerSecond = 8f;
        public const float ArriveDistance = 0.35f;

        public static void AssignIntent(EntityManager em, Entity unit, float3 destination, SkirmishGroupOrderKind order)
        {
            // A missing anchor is filled from the measured pad before the step.
            // Pinning the unit at the origin here kept that pad from ever applying.
            var intent = new SkirmishMoveIntentComponent
            {
                DestinationX = destination.x,
                DestinationZ = destination.z,
                Active = 1,
                Order = order
            };
            if (em.HasComponent<SkirmishMoveIntentComponent>(unit))
                em.SetComponentData(unit, intent);
            else
                em.AddComponentData(unit, intent);

            if (em.HasComponent<HoldPositionOrderTag>(unit) && order != SkirmishGroupOrderKind.Hold)
                em.RemoveComponent<HoldPositionOrderTag>(unit);

            TryReuseSharedPath(em, unit, destination);
        }

        public static void ClearIntent(EntityManager em, Entity unit)
        {
            if (!em.HasComponent<SkirmishMoveIntentComponent>(unit))
                return;
            var intent = em.GetComponentData<SkirmishMoveIntentComponent>(unit);
            intent.Active = 0;
            intent.Order = SkirmishGroupOrderKind.Hold;
            intent.AttackTarget = Entity.Null;
            intent.Engaged = 0;
            intent.Cooldown = 0f;
            em.SetComponentData(unit, intent);
        }

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
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            bool hasGrid = !grids.IsEmptyIgnoreFilter;
            GridConfig grid = hasGrid ? grids.GetSingleton<GridConfig>() : default;
            int moved = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(unit).SessionId.Equals(sessionId))
                    continue;
                var intent = em.GetComponentData<SkirmishMoveIntentComponent>(unit);
                HomeAttackDestination(em, unit, ref intent);
                if (intent.Active == 0)
                    continue;
                if (intent.Engaged != 0)
                {
                    SyncVisual(em, unit);
                    moved++;
                    continue;
                }

                if (em.HasComponent<HoldPositionOrderTag>(unit))
                {
                    intent.Active = 0;
                    em.SetComponentData(unit, intent);
                    continue;
                }

                // Shared navigation owns movement whenever the unit carries real
                // movement data and the destination resolves inside the loaded grid.
                // The direct step remains only for worlds without a grid (Editor
                // fixtures) and as a bounded stall recovery, never as the default.
                if (TrySharedPathStep(em, unit, ref intent, deltaSeconds, hasGrid, in grid))
                {
                    moved++;
                    continue;
                }

                // Fallback integration: no shared follower can exist without a grid,
                // and a stalled follower is cleared so it cannot double-integrate.
                if (em.HasComponent<UnitPathFollow>(unit))
                    em.RemoveComponent<UnitPathFollow>(unit);

                var transform = em.GetComponentData<LocalTransform>(unit);
                float3 dest = new float3(intent.DestinationX, transform.Position.y, intent.DestinationZ);
                float3 delta = dest - transform.Position;
                delta.y = 0f;
                float distance = math.length(delta);
                if (distance <= ArriveDistance)
                {
                    transform.Position = dest;
                    em.SetComponentData(unit, transform);
                    intent.Active = 0;
                    em.SetComponentData(unit, intent);
                    SyncVisual(em, unit);
                    moved++;
                    continue;
                }

                float speed = ResolveSpeed(em, unit);
                float step = math.min(distance, speed * deltaSeconds);
                transform.Position += math.normalizesafe(delta) * step;
                em.SetComponentData(unit, transform);
                SyncVisual(em, unit);
                moved++;
            }

            return moved;
        }

        private static void HomeAttackDestination(
            EntityManager em,
            Entity unit,
            ref SkirmishMoveIntentComponent intent)
        {
            if (intent.Order != SkirmishGroupOrderKind.Attack)
                return;
            Entity target = intent.AttackTarget;
            if (target == Entity.Null || !em.Exists(target) || !em.HasComponent<LocalTransform>(target))
                return;

            // Attack issued before the base had a transform stored the stand-in pad.
            // Follow the target's current position or the column never enters range.
            float3 live = em.GetComponentData<LocalTransform>(target).Position;
            float dx = live.x - intent.DestinationX;
            float dz = live.z - intent.DestinationZ;
            bool moved = dx * dx + dz * dz > 0.01f;
            byte active = intent.Active;
            if (em.HasComponent<LocalTransform>(unit))
            {
                float3 position = em.GetComponentData<LocalTransform>(unit).Position;
                float distance = math.length(new float3(live.x - position.x, 0f, live.z - position.z));
                if (distance > ArriveDistance)
                    active = 1;
            }

            if (!moved && active == intent.Active)
                return;
            intent.DestinationX = live.x;
            intent.DestinationZ = live.z;
            intent.Active = active;
            em.SetComponentData(unit, intent);
        }

        public static float3 DefaultAdvance(EntityManager em, Entity session)
        {
            // The assault default is the enemy staging pad in the loaded map's frame.
            // The stand-in extent frame is only a no-layout fallback.
            if (em.HasComponent<SkirmishResolvedSetupRecord>(session))
            {
                SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                if (setup != null && setup.MeasuredLayoutBound &&
                    (setup.EnemyStagingWorldX != 0f || setup.EnemyStagingWorldZ != 0f))
                    return new float3(setup.EnemyStagingWorldX, 0f, setup.EnemyStagingWorldZ);
            }

            return SkirmishVisualSpawnService.StagingWorld(2, true);
        }

        private const float SharedPathStallSeconds = 3f;

        /// <summary>
        /// Drives the unit through the shared grid pathfinding pipeline instead of
        /// stepping the transform locally. Returns false when the shared path cannot
        /// own this unit (no movement data, no grid, out-of-grid goal) or after a
        /// bounded stall, in which case the caller falls back to the local step.
        /// </summary>
        private static bool TrySharedPathStep(
            EntityManager em,
            Entity unit,
            ref SkirmishMoveIntentComponent intent,
            float deltaSeconds,
            bool hasGrid,
            in GridConfig grid)
        {
            if (!hasGrid || !em.HasComponent<UnitMove>(unit))
                return false;

            int2 cell = GridUtils.WorldToCell(grid, new float3(intent.DestinationX, 0f, intent.DestinationZ));
            if (cell.x < 0 || cell.y < 0 || cell.x >= grid.Width || cell.y >= grid.Height)
                return false;

            // A stalled unit keeps the local step until the shared follower engages or
            // a new order arrives; it does not re-enter the wait every tick.
            if (intent.SharedStalled != 0)
            {
                if (!em.HasComponent<UnitPathFollow>(unit))
                    return false;
                intent.SharedStalled = 0;
            }

            var request = new UnitPathRequest { Goal = cell };
            if (!em.HasComponent<UnitPathRequest>(unit))
                em.AddComponentData(unit, request);
            else if (!em.GetComponentData<UnitPathRequest>(unit).Goal.Equals(cell))
                em.SetComponentData(unit, request);

            float3 position = em.GetComponentData<LocalTransform>(unit).Position;
            float dx = position.x - intent.LastProgressX;
            float dz = position.z - intent.LastProgressZ;
            bool progressed = dx * dx + dz * dz > 0.0004f;
            if (em.HasComponent<UnitPathFollow>(unit) || progressed)
            {
                intent.NoSharedPathSeconds = 0f;
                intent.LastProgressX = position.x;
                intent.LastProgressZ = position.z;
            }
            else if ((intent.NoSharedPathSeconds += deltaSeconds) >= SharedPathStallSeconds)
            {
                intent.NoSharedPathSeconds = 0f;
                intent.SharedStalled = 1;
                em.SetComponentData(unit, intent);
                return false;
            }

            float3 toDestination = new float3(intent.DestinationX - position.x, 0f, intent.DestinationZ - position.z);
            if (math.length(toDestination) <= ArriveDistance * 2f)
                intent.Active = 0;

            em.SetComponentData(unit, intent);
            return true;
        }

        private static float ResolveSpeed(EntityManager em, Entity unit)
        {
            if (em.HasComponent<UnitMove>(unit) && em.GetComponentData<UnitMove>(unit).Speed > 0f)
                return em.GetComponentData<UnitMove>(unit).Speed;
            SkirmishPopulationCategory domain = em.HasComponent<SkirmishArmyGroupMembershipComponent>(unit)
                ? em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).Domain
                : em.HasComponent<SkirmishUnitRoleComponent>(unit)
                    ? em.GetComponentData<SkirmishUnitRoleComponent>(unit).Category
                    : SkirmishPopulationCategory.Infantry;
            return domain == SkirmishPopulationCategory.Ground ||
                   domain == SkirmishPopulationCategory.LogisticsSupport
                ? GroundMetersPerSecond
                : InfantryMetersPerSecond;
        }

        private static void TryReuseSharedPath(EntityManager em, Entity unit, float3 destination)
        {
            if (!em.HasComponent<UnitMove>(unit))
                return;
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.IsEmptyIgnoreFilter)
                return;
            GridConfig grid = grids.GetSingleton<GridConfig>();
            int2 cell = GridUtils.WorldToCell(grid, destination);
            var request = new UnitPathRequest { Goal = cell };
            if (em.HasComponent<UnitPathRequest>(unit))
                em.SetComponentData(unit, request);
            else
                em.AddComponentData(unit, request);
        }

        private static void SyncVisual(EntityManager em, Entity unit)
        {
            if (!em.HasComponent<SkirmishVisualInstanceRecord>(unit) ||
                !em.HasComponent<LocalTransform>(unit))
                return;
            GameObject instance = em.GetComponentObject<SkirmishVisualInstanceRecord>(unit).Instance;
            if (instance == null)
                return;
            float3 position = em.GetComponentData<LocalTransform>(unit).Position;
            instance.transform.position = new Vector3(position.x, position.y, position.z);
        }
    }
}
