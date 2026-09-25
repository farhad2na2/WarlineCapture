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
        public float Health01;
        public bool Selected;
        public bool Assault;
        public bool Structure;
        public bool AttackOrdered;
        public bool Rifle;
        public uint GroupId;
    }

    /// <summary>
    /// Binds the visible squad page, Hold, and Attack controls to expanded group orders.
    /// </summary>
    public static class SkirmishExpandedPresentedOrders
    {
        public const int PresentedSlots = SkirmishArmyPaging.StandardPageSize;

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
                   role == SkirmishRoleKind.Siege ||
                   role == SkirmishRoleKind.AttackHeliLight || role == SkirmishRoleKind.AttackHeli ||
                   role == SkirmishRoleKind.Strike;
        }

        /// <summary>Roles whose public overlay can damage a Barracks. Cars and APCs cannot.</summary>
        public static bool IsStructureAssaultRole(SkirmishRoleKind role)
        {
            return role == SkirmishRoleKind.Tank ||
                   role == SkirmishRoleKind.Rocketeer ||
                   role == SkirmishRoleKind.Breacher ||
                   role == SkirmishRoleKind.Siege ||
                   role == SkirmishRoleKind.AttackHeliLight || role == SkirmishRoleKind.AttackHeli ||
                   role == SkirmishRoleKind.Strike;
        }

        public static bool TryReadPage(
            EntityManager em,
            Entity session,
            out int pageIndex,
            out bool previousPage,
            out bool nextPage,
            out int pageCount,
            out int totalGroups,
            out int selectedGroups,
            out int selectedOffPage,
            out uint rosterRevision,
            out SkirmishPresentedSlot slot0,
            out SkirmishPresentedSlot slot1,
            out SkirmishPresentedSlot slot2,
            out SkirmishPresentedSlot slot3,
            out SkirmishPresentedSlot slot4)
        {
            pageIndex = 0;
            nextPage = false;
            previousPage = false;
            pageCount = 1;
            totalGroups = selectedGroups = selectedOffPage = 0;
            rosterRevision = 2166136261u;
            slot0 = default;
            slot1 = default;
            slot2 = default;
            slot3 = default;
            slot4 = default;
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session))
                return false;

            if (!em.HasComponent<SkirmishArmySelectionComponent>(session)) return false;
            var currentSelection = em.GetComponentData<SkirmishArmySelectionComponent>(session);
            if (currentSelection.PageSize != PresentedSlots)
            {
                currentSelection.PageSize = PresentedSlots;
                em.SetComponentData(session, currentSelection);
            }
            SkirmishArmyDrawerProjection.Project(em, session);
            pageIndex = em.GetComponentData<SkirmishArmySelectionComponent>(session).PageIndex;
            totalGroups = PlayerGroupCount(em, session);
            pageCount = totalGroups <= 0 ? 1 : (totalGroups + PresentedSlots - 1) / PresentedSlots;
            previousPage = pageIndex > 0;
            nextPage = pageIndex + 1 < pageCount;
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
            if (slots.Length > 4)
                slot4 = FromDrawer(slots[4]);
            if (em.HasBuffer<SkirmishArmyGroupRecord>(session))
            {
                var groups = em.GetBuffer<SkirmishArmyGroupRecord>(session);
                for (int i = 0; i < groups.Length; i++)
                    if (groups[i].FactionId == 1 && groups[i].AliveCount > 0)
                    {
                        rosterRevision = (rosterRevision ^ groups[i].GroupId) * 16777619u;
                        rosterRevision = (rosterRevision ^ (uint)groups[i].AliveCount) * 16777619u;
                        if (groups[i].Selected == 0) continue;
                        selectedGroups++;
                        uint id = groups[i].GroupId;
                        bool visible = slot0.GroupId == id || slot1.GroupId == id || slot2.GroupId == id || slot3.GroupId == id || slot4.GroupId == id;
                        if (!visible) selectedOffPage++;
                    }
            }
            return true;
        }

        public static bool TryGetPresentedMember(EntityManager em, Entity session, int index, out Entity member)
        {
            member = Entity.Null;
            if (index < 0 || index >= PresentedSlots ||
                !em.HasComponent<SkirmishArmySelectionComponent>(session)) return false;
            int page = em.GetComponentData<SkirmishArmySelectionComponent>(session).PageIndex;
            if (!SkirmishArmyGroupSystem.TryGetPagedGroup(em, session, 1, page, index, out var group)) return false;
            var id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishArmyGroupMembershipComponent), typeof(SkirmishAttemptOwnedComponent), typeof(UnitHealth));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).SessionId.Equals(id) ||
                    em.GetComponentData<SkirmishArmyGroupMembershipComponent>(entity).GroupId != group.GroupId ||
                    em.GetComponentData<UnitHealth>(entity).Current <= 0) continue;
                member = entity;
                return true;
            }
            return false;
        }

        public static bool TryPresentedSlot(EntityManager em, Entity session, int index)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session))
                return false;
            if (index < 0 || index >= PresentedSlots)
                return false;
            int page = em.HasComponent<SkirmishArmySelectionComponent>(session)
                ? em.GetComponentData<SkirmishArmySelectionComponent>(session).PageIndex
                : 0;
            bool add = AnyPlayerSelected(em, session);
            return SkirmishArmySelectionService.TrySelectPageSlot(em, session, page, index, add, out _);
        }

        public static bool TryPresentedGroup(EntityManager em, Entity session, uint groupId)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session) || groupId == 0 || !em.HasBuffer<SkirmishArmyGroupRecord>(session)) return false;
            var groups = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            bool add = AnyPlayerSelected(em, session);
            for (int i = 0; i < groups.Length; i++)
                if (groups[i].FactionId == 1 && groups[i].GroupId == groupId && groups[i].AliveCount > 0)
                    return SkirmishArmySelectionService.TrySelectGroup(em, session, groupId, add, out _);
            return false;
        }

        public static bool TryHoldSelection(EntityManager em, Entity session)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session) ||
                !AnyPlayerSelected(em, session))
                return false;
            return SkirmishArmyCommandService.TryHold(em, session, out _);
        }



        public static bool TryChangePage(EntityManager em, Entity session, int delta)
        {
            if ((delta != -1 && delta != 1) || !SkirmishExpandedSessionControlService.IsExpanded(em, session))
                return false;
            if (!em.HasComponent<SkirmishArmySelectionComponent>(session))
                return false;
            int groups = PlayerGroupCount(em, session);
            var selection = em.GetComponentData<SkirmishArmySelectionComponent>(session);
            int pageSize = PresentedSlots;
            selection.PageSize = pageSize;
            int pages = groups <= 0 ? 1 : (groups + pageSize - 1) / pageSize;
            int destination = selection.PageIndex + delta;
            if (destination < 0 || destination >= pages)
                return false;
            selection.PageIndex = destination;
            em.SetComponentData(session, selection);
            SkirmishArmyDrawerProjection.Project(em, session);
            return true;
        }

        public static bool TryClearSelection(EntityManager em, Entity session)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session) || !AnyPlayerSelected(em, session)) return false;
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
                Health01 = slot.Health01,
                Selected = slot.Selected != 0,
                Assault = slot.AliveCount > 0 && IsAssaultRole(slot.Role),
                Structure = slot.AliveCount > 0 && IsStructureAssaultRole(slot.Role),
                AttackOrdered = slot.AliveCount > 0 && slot.LastOrder == SkirmishGroupOrderKind.Attack,
                Rifle = slot.AliveCount > 0 && slot.Role == SkirmishRoleKind.Rifle,
                GroupId = slot.GroupId
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


    }
}
