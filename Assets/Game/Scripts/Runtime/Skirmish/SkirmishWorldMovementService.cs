using Game.Components;
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
            if (!em.HasComponent<LocalTransform>(unit))
                em.AddComponentData(unit, LocalTransform.FromPosition(destination * 0f));
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
            int moved = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(unit).SessionId.Equals(sessionId))
                    continue;
                var intent = em.GetComponentData<SkirmishMoveIntentComponent>(unit);
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

                if (SharedPathOwns(em, unit))
                {
                    SyncVisual(em, unit);
                    moved++;
                    continue;
                }

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

        public static float3 DefaultAdvance(EntityManager em, Entity session)
        {
            _ = session;
            _ = em;
            return SkirmishVisualSpawnService.StagingWorld(2, true);
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

        private static bool SharedPathOwns(EntityManager em, Entity unit)
        {
            // A pending path request is not movement. Live Desert Base has a grid,
            // so Attack writes UnitPathRequest, but skirmish units often never gain
            // a follow. Yielding on the request alone froze the assault at the pad.
            return em.HasComponent<UnitPathFollow>(unit);
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
