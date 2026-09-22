using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkirmishCapacityLifecycleSystem))]
    public partial struct SkirmishArmyGroupSystem : ISystem
    {
        private EntityQuery ownedUnits;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            ownedUnits = state.GetEntityQuery(ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            foreach ((RefRO<SkirmishExpandedSessionComponent> session, Entity entity) in
                     SystemAPI.Query<RefRO<SkirmishExpandedSessionComponent>>().WithEntityAccess())
            {
                if (session.ValueRO.IsLegacy != 0 || session.ValueRO.Phase != SkirmishSessionPhase.Playing)
                    continue;
                RefreshAlive(em, entity, ownedUnits, session.ValueRO.SessionId);
            }
        }

        public static void EnsureSession(EntityManager em, Entity session, SkirmishResolvedSetup setup)
        {
            if (!em.HasComponent<SkirmishArmySelectionComponent>(session))
            {
                em.AddComponentData(session, new SkirmishArmySelectionComponent
                {
                    PageSize = SkirmishArmyPaging.StandardPageSize,
                    PageIndex = 0,
                    NextGroupId = 0
                });
            }

            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session))
                em.AddBuffer<SkirmishArmyGroupRecord>(session);

            if (!em.HasComponent<SkirmishFogStateComponent>(session))
            {
                em.AddComponentData(session, new SkirmishFogStateComponent
                {
                    SharedFog = (byte)(setup != null && setup.SharedFog ? 1 : 0),
                    DevelopmentFullVision = (byte)(setup == null || setup.DevelopmentFullVision ? 1 : 0),
                    LastSeenExpireSeconds = setup != null ? setup.LastSeenExpireSeconds : 20
                });
            }
        }

        public static uint OpenGroup(
            EntityManager em,
            Entity session,
            byte factionId,
            SkirmishRoleKind role,
            SkirmishPopulationCategory domain)
        {
            EnsureSession(em, session, null);
            var selection = em.GetComponentData<SkirmishArmySelectionComponent>(session);
            selection.NextGroupId++;
            uint groupId = selection.NextGroupId;
            em.SetComponentData(session, selection);
            em.GetBuffer<SkirmishArmyGroupRecord>(session).Add(new SkirmishArmyGroupRecord
            {
                GroupId = groupId,
                FactionId = factionId,
                Role = role,
                Domain = domain,
                MemberCount = 0,
                AliveCount = 0
            });
            return groupId;
        }

        public static void BindMember(EntityManager em, Entity session, Entity member, uint groupId)
        {
            if (groupId == 0 || !em.HasComponent<SkirmishUnitRoleComponent>(member))
                return;

            SkirmishPopulationCategory domain = em.GetComponentData<SkirmishUnitRoleComponent>(member).Category;
            if (em.HasComponent<SkirmishArmyGroupMembershipComponent>(member))
            {
                var membership = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(member);
                membership.GroupId = groupId;
                membership.Domain = domain;
                em.SetComponentData(member, membership);
            }
            else
            {
                em.AddComponentData(member, new SkirmishArmyGroupMembershipComponent
                {
                    GroupId = groupId,
                    Domain = domain,
                    Leader = 0
                });
            }

            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                SkirmishArmyGroupRecord record = buffer[i];
                if (record.GroupId != groupId)
                    continue;
                record.MemberCount++;
                record.AliveCount++;
                if (record.MemberCount == 1)
                {
                    var membership = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(member);
                    membership.Leader = 1;
                    em.SetComponentData(member, membership);
                }

                buffer[i] = record;
                return;
            }
        }

        public static uint AssignProduced(EntityManager em, Entity session, uint reservationId)
        {
            if (reservationId == 0)
                return 0;

            using var query = em.CreateEntityQuery(
                typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishUnitRoleComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            uint groupId = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity member = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(member);
                if (owned.ReservationId != reservationId || owned.IsStructure != 0)
                    continue;
                if (groupId == 0)
                {
                    SkirmishUnitRoleComponent role = em.GetComponentData<SkirmishUnitRoleComponent>(member);
                    groupId = OpenGroup(em, session, owned.FactionId, role.Role, role.Category);
                }

                BindMember(em, session, member, groupId);
            }

            return groupId;
        }

        public static int RefreshAlive(
            EntityManager em,
            Entity session,
            EntityQuery owned,
            FixedString64Bytes sessionId)
        {
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session))
                return 0;

            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                SkirmishArmyGroupRecord record = buffer[i];
                record.AliveCount = 0;
                buffer[i] = record;
            }

            using NativeArray<Entity> entities = owned.ToEntityArray(Allocator.Temp);
            int living = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                if (!em.HasComponent<SkirmishAttemptOwnedComponent>(unit))
                    continue;
                var ownedUnit = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (!ownedUnit.SessionId.Equals(sessionId) || ownedUnit.IsStructure != 0)
                    continue;
                if (!em.HasComponent<SkirmishArmyGroupMembershipComponent>(unit))
                    continue;
                if (!IsAlive(em, unit))
                    continue;

                uint groupId = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).GroupId;
                for (int g = 0; g < buffer.Length; g++)
                {
                    SkirmishArmyGroupRecord record = buffer[g];
                    if (record.GroupId != groupId)
                        continue;
                    record.AliveCount++;
                    buffer[g] = record;
                    living++;
                    break;
                }
            }

            return living;
        }

        public static bool TryGet(EntityManager em, Entity session, uint groupId, out SkirmishArmyGroupRecord record)
        {
            record = default;
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session))
                return false;
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].GroupId != groupId)
                    continue;
                record = buffer[i];
                return true;
            }

            return false;
        }

        public static int CountFactionGroups(EntityManager em, Entity session, byte factionId)
        {
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session))
                return 0;
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            int total = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].FactionId == factionId)
                    total++;
            }

            return total;
        }

        public static bool TryGetPagedGroup(
            EntityManager em,
            Entity session,
            byte factionId,
            int pageIndex,
            int slotIndex,
            out SkirmishArmyGroupRecord record)
        {
            record = default;
            if (!em.HasComponent<SkirmishArmySelectionComponent>(session) ||
                !em.HasBuffer<SkirmishArmyGroupRecord>(session))
                return false;

            int pageSize = em.GetComponentData<SkirmishArmySelectionComponent>(session).PageSize;
            if (pageSize < 1)
                pageSize = SkirmishArmyPaging.StandardPageSize;
            int target = pageIndex * pageSize + slotIndex;
            if (pageIndex < 0 || slotIndex < 0 || slotIndex >= pageSize)
                return false;

            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            int seen = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].FactionId != factionId)
                    continue;
                if (seen == target)
                {
                    record = buffer[i];
                    return true;
                }

                seen++;
            }

            return false;
        }

        public static bool IsAlive(EntityManager em, Entity unit)
        {
            if (!em.Exists(unit) || !em.HasComponent<SkirmishAttemptOwnedComponent>(unit))
                return false;
            if (em.GetComponentData<SkirmishAttemptOwnedComponent>(unit).CapacityReleased != 0)
                return false;
            if (em.HasComponent<UnitHealth>(unit) && em.GetComponentData<UnitHealth>(unit).Current <= 0)
                return false;
            return true;
        }
    }
}
