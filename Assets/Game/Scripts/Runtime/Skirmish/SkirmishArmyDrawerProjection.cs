using Game.Components;
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

            var selection = em.GetComponentData<SkirmishArmySelectionComponent>(session);
            int pageSize = selection.PageSize < 1 ? SkirmishArmyPaging.StandardPageSize : selection.PageSize;
            int page = selection.PageIndex;
            DynamicBuffer<SkirmishArmyGroupRecord> groups = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            int written = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].FactionId != 1)
                    continue;
                int slotIndex = written;
                written++;
                if (slotIndex / pageSize != page)
                    continue;
                slots.Add(new SkirmishArmyDrawerSlot
                {
                    GroupId = groups[i].GroupId,
                    Role = groups[i].Role,
                    AliveCount = groups[i].AliveCount,
                    Selected = groups[i].Selected,
                    LastOrder = groups[i].LastOrder
                });
            }

            return slots.Length;
        }
    }
}
