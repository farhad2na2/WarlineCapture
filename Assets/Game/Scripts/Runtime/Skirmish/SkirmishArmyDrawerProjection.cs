using Game.Components;
using Unity.Collections;
using Unity.Mathematics;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishArmyDrawerProjection
    {
        public static int Project(EntityManager em, Entity session)
        {
            if (!em.HasBuffer<SkirmishArmyDrawerSlot>(session))
                em.AddBuffer<SkirmishArmyDrawerSlot>(session);
            DynamicBuffer<SkirmishArmyDrawerSlot> slots = em.GetBuffer<SkirmishArmyDrawerSlot>(session);
            slots.Clear();
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session) ||
                !em.HasComponent<SkirmishArmySelectionComponent>(session))
                return 0;

            // Read the actual surviving members once for both human cards and ARIA's public model.
            DynamicBuffer<SkirmishArmyGroupRecord> groups = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            var healthByGroup = new NativeArray<int2>(groups.Length, Allocator.Temp);
            try
            {
                var sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
                using var units = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                    typeof(SkirmishArmyGroupMembershipComponent), typeof(UnitHealth));
                using var members = units.ToEntityArray(Allocator.Temp);
                foreach (var member in members)
                {
                    var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(member);
                    var health = em.GetComponentData<UnitHealth>(member);
                    if (!owned.SessionId.Equals(sessionId) || owned.FactionId != 1 ||
                        owned.CapacityReleased != 0 || health.Current <= 0) continue;
                    uint group = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(member).GroupId;
                    for (int i = 0; i < groups.Length; i++)
                        if (groups[i].FactionId == 1 && groups[i].GroupId == group)
                        {
                            healthByGroup[i] += new int2(math.clamp(health.Current, 0, health.Max), math.max(0, health.Max));
                            break;
                        }
                }

                var selection = em.GetComponentData<SkirmishArmySelectionComponent>(session);
                // Normalize surviving expanded sessions at the projection owner boundary.
                // Selection is GroupId based, so the selected set survives this page-size migration.
                int pageSize = SkirmishArmyPaging.StandardPageSize;
                selection.PageSize = pageSize;
                int liveGroups = 0;
                for (int i = 0; i < groups.Length; i++)
                    if (groups[i].FactionId == 1 && groups[i].AliveCount > 0) liveGroups++;
                selection.PageIndex = math.clamp(selection.PageIndex, 0, math.max(0, (liveGroups - 1) / pageSize));
                em.SetComponentData(session, selection);
                int page = selection.PageIndex;
                int written = 0;
                for (int i = 0; i < groups.Length; i++)
                {
                    if (groups[i].FactionId != 1 || groups[i].AliveCount <= 0)
                        continue;
                    int slotIndex = written;
                    written++;
                    if (slotIndex / pageSize != page)
                        continue;
                    var groupHealth = healthByGroup[i];
                    slots.Add(new SkirmishArmyDrawerSlot
                    {
                        GroupId = groups[i].GroupId,
                        Health01 = groupHealth.y > 0 ? math.saturate((float)groupHealth.x / groupHealth.y) : 0f,
                        Role = groups[i].Role,
                        AliveCount = groups[i].AliveCount,
                        Selected = groups[i].Selected,
                        LastOrder = groups[i].LastOrder
                    });
                }

                return slots.Length;
            }
            finally { healthByGroup.Dispose(); }
        }
    }
}
