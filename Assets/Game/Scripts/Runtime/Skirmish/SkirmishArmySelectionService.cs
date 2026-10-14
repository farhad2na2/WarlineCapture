using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishArmySelectionService
    {
        public static bool TrySelectGroup(
            EntityManager em,
            Entity session,
            uint groupId,
            bool add,
            out SkirmishCommandDecision decision)
        {
            decision = default;
            decision.GroupId = groupId;
            decision.Field = "groupId";
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session) ||
                !SkirmishArmyGroupSystem.TryGet(em, session, groupId, out SkirmishArmyGroupRecord record))
            {
                decision.Reason = SkirmishReasonCode.MissingReference;
                return false;
            }

            if (record.FactionId != 1 || record.AliveCount <= 0)
            {
                decision.Reason = SkirmishReasonCode.InvalidSelection;
                return false;
            }

            if (!add)
                Clear(em, session);

            MarkGroup(em, session, groupId, 1);
            int selected = ApplyTags(em, session, groupId, true);
            if (selected == 0)
                MarkGroup(em, session, groupId, 0);
            Recount(em, session);
            decision.Accepted = selected > 0;
            decision.IssuedCount = selected;
            decision.Reason = selected > 0 ? SkirmishReasonCode.None : SkirmishReasonCode.InvalidSelection;
            return decision.Accepted;
        }

        public static bool TryDeselectGroup(EntityManager em, Entity session, uint groupId)
        {
            if (!SkirmishArmyGroupSystem.TryGet(em, session, groupId, out var record) ||
                record.FactionId != 1 ||
                (record.Selected == 0 && !HasSelectedLivingPlayerMember(em, session, groupId))) return false;
            MarkGroup(em, session, groupId, 0);
            ApplyTags(em, session, groupId, false);
            Recount(em, session);
            SkirmishArmyDrawerProjection.Project(em, session);
            return true;
        }

        public static bool TrySelectPageSlot(
            EntityManager em,
            Entity session,
            int pageIndex,
            int slotIndex,
            bool add,
            out SkirmishCommandDecision decision)
        {
            decision = default;
            decision.Field = "page";
            if (!SkirmishArmyGroupSystem.TryGetPagedGroup(
                    em, session, 1, pageIndex, slotIndex, out SkirmishArmyGroupRecord record))
            {
                decision.Reason = SkirmishReasonCode.InvalidSelection;
                return false;
            }

            if (em.HasComponent<SkirmishArmySelectionComponent>(session))
            {
                var selection = em.GetComponentData<SkirmishArmySelectionComponent>(session);
                selection.PageIndex = pageIndex;
                em.SetComponentData(session, selection);
            }

            return TrySelectGroup(em, session, record.GroupId, add, out decision);
        }

        public static void Clear(EntityManager em, Entity session)
        {
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session))
                return;

            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                SkirmishArmyGroupRecord record = buffer[i];
                if (record.Selected == 0)
                    continue;
                record.Selected = 0;
                buffer[i] = record;
            }

            // Clear live selection even when a prior world command left the
            // record flags out of sync with the actual unit tags.
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(
                typeof(SelectedUnitTag),
                typeof(SkirmishArmyGroupMembershipComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (owned.FactionId == 1 && owned.SessionId.Equals(sessionId))
                    em.RemoveComponent<SelectedUnitTag>(unit);
            }
            Recount(em, session);
        }

        // A zero groupId asks whether any living player group is selected.
        public static bool HasSelectedLivingPlayerMember(EntityManager em, Entity session, uint groupId)
        {
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return false;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(
                typeof(SelectedUnitTag),
                typeof(SkirmishArmyGroupMembershipComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (owned.FactionId != 1 || !owned.SessionId.Equals(sessionId) ||
                    (groupId != 0 && em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).GroupId != groupId))
                    continue;
                if (SkirmishArmyGroupSystem.IsAlive(em, unit))
                    return true;
            }

            return false;
        }

        public static int SelectedLivingCount(EntityManager em, Entity session)
        {
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SelectedUnitTag), typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            int total = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).SessionId.Equals(sessionId))
                    continue;
                if (SkirmishArmyGroupSystem.IsAlive(em, entities[i]))
                    total++;
            }

            return total;
        }

        private static void MarkGroup(EntityManager em, Entity session, uint groupId, byte selected)
        {
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                SkirmishArmyGroupRecord record = buffer[i];
                if (record.GroupId != groupId)
                    continue;
                record.Selected = selected;
                buffer[i] = record;
                return;
            }
        }

        private static int ApplyTags(EntityManager em, Entity session, uint groupId, bool selected)
        {
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(
                typeof(SkirmishArmyGroupMembershipComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            int applied = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(unit).SessionId.Equals(sessionId))
                    continue;
                if (em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).GroupId != groupId)
                    continue;
                if (selected)
                {
                    if (!SkirmishArmyGroupSystem.IsAlive(em, unit))
                        continue;
                    if (!em.HasComponent<SelectedUnitTag>(unit))
                        em.AddComponent<SelectedUnitTag>(unit);
                    applied++;
                }
                else if (em.HasComponent<SelectedUnitTag>(unit))
                {
                    em.RemoveComponent<SelectedUnitTag>(unit);
                }
            }

            return applied;
        }

        private static void Recount(EntityManager em, Entity session)
        {
            if (!em.HasComponent<SkirmishArmySelectionComponent>(session))
                return;
            var selection = em.GetComponentData<SkirmishArmySelectionComponent>(session);
            int selected = 0;
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Selected != 0)
                    selected++;
            }

            selection.SelectedGroupCount = selected;
            em.SetComponentData(session, selection);
        }
    }
}
