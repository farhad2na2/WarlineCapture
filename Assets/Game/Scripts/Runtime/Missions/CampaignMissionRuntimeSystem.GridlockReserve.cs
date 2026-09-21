using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        // The finite reserve enters play after its public warning. Holding its orders
        // alone is insufficient: nearby rifles could otherwise kill it before release.
        internal static void UpdateGridlockReserve(EntityManager em, Entity root,
            in CampaignMissionGridlockState state)
        {
            if (state.Ready == 0 || state.Failure != GridlockFailure.None) return;
            bool released = state.CounterattackWarned != 0 &&
                state.ElapsedMilliseconds >= state.CounterattackReleaseAtMilliseconds;
            using var changes = new EntityCommandBuffer(Allocator.Temp);
            foreach (var member in em.GetBuffer<CampaignMissionGridlockMember>(root, true))
            {
                if (member.Kind != GridlockMemberKind.Counterattack || member.Dead != 0 ||
                    member.HealthInitialized == 0 || !em.Exists(member.Entity)) continue;
                bool disabled = em.HasComponent<Disabled>(member.Entity);
                if (disabled == released) changes.SetEnabled(member.Entity, released);
            }
            changes.Playback(em);
        }
    }
}
