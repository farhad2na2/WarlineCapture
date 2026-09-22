using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public struct SkirmishHealthReadout
    {
        public const byte Hidden = 0;
        public const byte LastSeen = 1;
        public const byte Visible = 2;

        public byte Knowledge;
        public int DisplayedCurrent;
        public int Max;
        public float AgeSeconds;
    }

    public static class SkirmishBaseAssaultHudProjection
    {
        public static void Observe(EntityManager em, Entity session, float deltaSeconds, bool paused)
        {
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(
                typeof(SkirmishObjectiveRoleComponent),
                typeof(UnitHealth),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).SessionId.Equals(sessionId))
                    continue;
                var health = em.GetComponentData<UnitHealth>(entity);
                SkirmishContactSight sight = em.HasComponent<SkirmishContactSightComponent>(entity)
                    ? em.GetComponentData<SkirmishContactSightComponent>(entity).Sight
                    : SkirmishContactSight.Unknown;
                var observed = em.HasComponent<SkirmishObservedHealthComponent>(entity)
                    ? em.GetComponentData<SkirmishObservedHealthComponent>(entity)
                    : new SkirmishObservedHealthComponent();
                if (sight == SkirmishContactSight.Visible)
                {
                    observed.LastSeenHealth = health.Current;
                    observed.LastSeenMax = health.Max;
                    observed.AgeSeconds = 0f;
                    observed.Known = 1;
                }
                else if (observed.Known != 0 && !paused)
                    observed.AgeSeconds += deltaSeconds;

                if (em.HasComponent<SkirmishObservedHealthComponent>(entity))
                    em.SetComponentData(entity, observed);
                else
                    em.AddComponentData(entity, observed);
            }

            BindDesignatedMatchBases(em, session);
        }

        public static SkirmishHealthReadout ReadPlayerBase(EntityManager em, Entity session) =>
            Read(em, session, SkirmishObjectiveRoleKind.PlayerBase);

        public static SkirmishHealthReadout ReadEnemyBase(EntityManager em, Entity session) =>
            Read(em, session, SkirmishObjectiveRoleKind.EnemyBase);

        public static void BindDesignatedMatchBases(EntityManager em, Entity session)
        {
            if (!em.HasComponent<SkirmishMatchState>(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            Entity player = Entity.Null;
            Entity enemy = Entity.Null;
            using var query = em.CreateEntityQuery(
                typeof(SkirmishObjectiveRoleComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).SessionId.Equals(sessionId))
                    continue;
                SkirmishObjectiveRoleKind role = em.GetComponentData<SkirmishObjectiveRoleComponent>(entity).Role;
                if (role == SkirmishObjectiveRoleKind.PlayerBase)
                    player = entity;
                else if (role == SkirmishObjectiveRoleKind.EnemyBase)
                    enemy = entity;
            }

            var match = em.GetComponentData<SkirmishMatchState>(session);
            match.PlayerMainBase = player;
            match.EnemyMainBase = enemy;
            em.SetComponentData(session, match);
        }

        public static SkirmishHealthReadout Read(EntityManager em, Entity session, SkirmishObjectiveRoleKind role)
        {
            var readout = new SkirmishHealthReadout { Knowledge = SkirmishHealthReadout.Hidden };
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return readout;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(
                typeof(SkirmishObjectiveRoleComponent),
                typeof(UnitHealth),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).SessionId.Equals(sessionId))
                    continue;
                if (em.GetComponentData<SkirmishObjectiveRoleComponent>(entity).Role != role)
                    continue;
                var health = em.GetComponentData<UnitHealth>(entity);
                SkirmishContactSight sight = em.HasComponent<SkirmishContactSightComponent>(entity)
                    ? em.GetComponentData<SkirmishContactSightComponent>(entity).Sight
                    : SkirmishContactSight.Unknown;
                var observed = em.HasComponent<SkirmishObservedHealthComponent>(entity)
                    ? em.GetComponentData<SkirmishObservedHealthComponent>(entity)
                    : new SkirmishObservedHealthComponent();
                readout.Max = health.Max > 0 ? health.Max : observed.LastSeenMax;
                if (sight == SkirmishContactSight.Visible)
                {
                    readout.Knowledge = SkirmishHealthReadout.Visible;
                    readout.DisplayedCurrent = health.Current;
                    readout.AgeSeconds = 0f;
                    return readout;
                }

                if (observed.Known != 0)
                {
                    readout.Knowledge = SkirmishHealthReadout.LastSeen;
                    readout.DisplayedCurrent = observed.LastSeenHealth;
                    readout.AgeSeconds = observed.AgeSeconds;
                    return readout;
                }

                readout.Knowledge = SkirmishHealthReadout.Hidden;
                readout.DisplayedCurrent = 0;
                return readout;
            }

            return readout;
        }
    }
}
