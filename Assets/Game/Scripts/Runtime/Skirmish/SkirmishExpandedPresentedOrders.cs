using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public struct SkirmishPresentedSlot
    {
        public bool Occupied;
        public SkirmishRoleKind Role;
        public int Alive;
        public bool Selected;
        public bool Assault;
        public bool Structure;
        public bool AttackOrdered;
        public bool Rifle;
    }

    /// <summary>
    /// Binds the visible squad page, Hold, and Attack controls to expanded group orders.
    /// </summary>
    public static class SkirmishExpandedPresentedOrders
    {
        public const int PresentedSlots = 4;

        public static bool IsAssaultRole(SkirmishRoleKind role)
        {
            return role == SkirmishRoleKind.Rifle ||
                   role == SkirmishRoleKind.Gunner ||
                   role == SkirmishRoleKind.Marksman ||
                   role == SkirmishRoleKind.Tank ||
                   role == SkirmishRoleKind.Car ||
                   role == SkirmishRoleKind.ApcFast ||
                   role == SkirmishRoleKind.ApcArmored ||
                   role == SkirmishRoleKind.ApcHeavy ||
                   role == SkirmishRoleKind.Rocketeer ||
                   role == SkirmishRoleKind.Breacher ||
                   role == SkirmishRoleKind.Siege;
        }

        /// <summary>Roles whose public overlay can damage a Barracks. Cars and APCs cannot.</summary>
        public static bool IsStructureAssaultRole(SkirmishRoleKind role)
        {
            return role == SkirmishRoleKind.Tank ||
                   role == SkirmishRoleKind.Rocketeer ||
                   role == SkirmishRoleKind.Breacher ||
                   role == SkirmishRoleKind.Siege;
        }

        public static bool TryReadPage(
            EntityManager em,
            Entity session,
            out int pageIndex,
            out bool nextPage,
            out SkirmishPresentedSlot slot0,
            out SkirmishPresentedSlot slot1,
            out SkirmishPresentedSlot slot2,
            out SkirmishPresentedSlot slot3)
        {
            pageIndex = 0;
            nextPage = false;
            slot0 = default;
            slot1 = default;
            slot2 = default;
            slot3 = default;
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session))
                return false;

            SkirmishArmyDrawerProjection.Project(em, session);
            if (em.HasComponent<SkirmishArmySelectionComponent>(session))
                pageIndex = em.GetComponentData<SkirmishArmySelectionComponent>(session).PageIndex;
            nextPage = PlayerGroupCount(em, session) > PresentedSlots;
            if (!em.HasBuffer<SkirmishArmyDrawerSlot>(session))
                return true;

            DynamicBuffer<SkirmishArmyDrawerSlot> slots = em.GetBuffer<SkirmishArmyDrawerSlot>(session);
            if (slots.Length > 0)
                slot0 = FromDrawer(slots[0]);
            if (slots.Length > 1)
                slot1 = FromDrawer(slots[1]);
            if (slots.Length > 2)
                slot2 = FromDrawer(slots[2]);
            if (slots.Length > 3)
                slot3 = FromDrawer(slots[3]);
            return true;
        }

        public static bool TryPresentedSlot(EntityManager em, Entity session, int index)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session))
                return false;
            if (index == PresentedSlots)
                return TryAdvancePage(em, session);
            if (index < 0 || index >= PresentedSlots)
                return false;
            int page = em.HasComponent<SkirmishArmySelectionComponent>(session)
                ? em.GetComponentData<SkirmishArmySelectionComponent>(session).PageIndex
                : 0;
            bool add = AnyPlayerSelected(em, session);
            return SkirmishArmySelectionService.TrySelectPageSlot(em, session, page, index, add, out _);
        }

        public static bool TryHoldSelection(EntityManager em, Entity session)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session) ||
                !AnyPlayerSelected(em, session))
                return false;
            return SkirmishArmyCommandService.TryHold(em, session, out _);
        }

        public static bool TryAttackEnemyBase(EntityManager em, Entity session)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session) ||
                !AnyPlayerSelected(em, session))
                return false;
            Entity target = FindVisibleEnemyBase(em, session);
            if (target == Entity.Null)
                return false;
            return SkirmishArmyCommandService.TryAttack(em, session, target, out _);
        }

        /// <summary>
        /// Orders every living player group that can damage a Barracks onto the
        /// designated enemy base. Same command as the public Attack button, without
        /// requiring the squad page or a raycast. Does not add units or materials.
        /// </summary>
        public static int TryOrderStructureAssault(EntityManager em, Entity session)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase != SkirmishSessionPhase.Playing)
                return 0;
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session))
                return 0;

            Entity target = FindVisibleEnemyBase(em, session);
            if (target == Entity.Null && em.HasComponent<SkirmishFogStateComponent>(session))
            {
                SkirmishFogService.Project(em, session);
                target = FindVisibleEnemyBase(em, session);
            }

            if (target == Entity.Null)
                return 0;

            DynamicBuffer<SkirmishArmyGroupRecord> groups = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            var groupIds = new NativeList<uint>(groups.Length, Allocator.Temp);
            for (int i = 0; i < groups.Length; i++)
            {
                SkirmishArmyGroupRecord group = groups[i];
                if (group.FactionId != 1 || group.AliveCount <= 0)
                    continue;
                if (!IsStructureAssaultRole(group.Role))
                    continue;
                groupIds.Add(group.GroupId);
            }

            int issued = 0;
            for (int i = 0; i < groupIds.Length; i++)
            {
                if (SkirmishArmyCommandService.TryIssueGroupOrder(
                        em,
                        session,
                        groupIds[i],
                        1,
                        SkirmishGroupOrderKind.Attack,
                        target,
                        out _))
                    issued++;
            }

            groupIds.Dispose();
            return issued;
        }

        public static bool TryAdvancePage(EntityManager em, Entity session)
        {
            if (!em.HasComponent<SkirmishArmySelectionComponent>(session))
                return false;
            int groups = PlayerGroupCount(em, session);
            var selection = em.GetComponentData<SkirmishArmySelectionComponent>(session);
            int pageSize = selection.PageSize < 1 ? PresentedSlots : selection.PageSize;
            int pages = groups <= 0 ? 1 : (groups + pageSize - 1) / pageSize;
            if (pages <= 1)
                return false;
            selection.PageIndex = (selection.PageIndex + 1) % pages;
            em.SetComponentData(session, selection);
            SkirmishArmySelectionService.Clear(em, session);
            SkirmishArmyDrawerProjection.Project(em, session);
            return true;
        }

        private static SkirmishPresentedSlot FromDrawer(SkirmishArmyDrawerSlot slot)
        {
            return new SkirmishPresentedSlot
            {
                Occupied = slot.AliveCount > 0,
                Role = slot.Role,
                Alive = slot.AliveCount,
                Selected = slot.Selected != 0,
                Assault = slot.AliveCount > 0 && IsAssaultRole(slot.Role),
                Structure = slot.AliveCount > 0 && IsStructureAssaultRole(slot.Role),
                AttackOrdered = slot.AliveCount > 0 && slot.LastOrder == SkirmishGroupOrderKind.Attack,
                Rifle = slot.AliveCount > 0 && slot.Role == SkirmishRoleKind.Rifle
            };
        }

        private static int PlayerGroupCount(EntityManager em, Entity session)
        {
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session))
                return 0;
            DynamicBuffer<SkirmishArmyGroupRecord> groups = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            int count = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].FactionId == 1 && groups[i].AliveCount > 0)
                    count++;
            }

            return count;
        }

        private static bool AnyPlayerSelected(EntityManager em, Entity session)
        {
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session))
                return false;
            DynamicBuffer<SkirmishArmyGroupRecord> groups = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].FactionId == 1 && groups[i].Selected != 0 && groups[i].AliveCount > 0)
                    return true;
            }

            return false;
        }

        private static Entity FindVisibleEnemyBase(EntityManager em, Entity session)
        {
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return Entity.Null;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(
                typeof(SkirmishObjectiveRoleComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (!owned.SessionId.Equals(sessionId) || owned.FactionId == 1)
                    continue;
                if (em.GetComponentData<SkirmishObjectiveRoleComponent>(entity).Role !=
                    SkirmishObjectiveRoleKind.EnemyBase)
                    continue;
                if (!SkirmishFogService.IsVisible(em, entity) || !SkirmishArmyGroupSystem.IsAlive(em, entity))
                    continue;
                return entity;
            }

            return Entity.Null;
        }
    }
}
