#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        // A shader-settled capture can occur after a real producer delivers a member.
        // Require every authored starting actor and recognize extra actors only from
        // the native paid-delivery ledger; never relax the initial force budget.
        public static bool NativeStartupRosterMatches(EntityManager em, Entity session,
            SkirmishResolvedSetup setup, out int initialCount, out int deliveredCount)
        {
            initialCount = deliveredCount = 0;
            var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            var initial = new Dictionary<string, (byte Faction, SkirmishRoleKind Role)>();
            for (int i = 0; i < setup.Forces.Length; i++)
            {
                var force = setup.Forces[i];
                for (int member = 0; member < Unity.Mathematics.math.max(1, force.Quantity); member++)
                    initial.Add(force.RoleId + "." + force.FactionId + "." + i + "." + member,
                        (force.FactionId, force.RoleKind));
            }
            var delivered = new Dictionary<string, (byte Faction, SkirmishRoleKind Role, uint Reservation)>();
            if (em.HasBuffer<SkirmishProductionReservation>(session))
                foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session, true))
                {
                    if (receipt.DeliveredMembers == 0) continue;
                    if (receipt.DeliveredMembers < 0 || receipt.DeliveredMembers > receipt.MemberCount ||
                        receipt.MaterialsPaid <= 0 || receipt.ProducerRuntimeId == 0) return false;
                    for (int member = 0; member < receipt.DeliveredMembers; member++)
                        if (!delivered.TryAdd("production." + receipt.ReservationId + "." + member,
                            (receipt.FactionId, receipt.Role, receipt.ReservationId))) return false;
                }
            using var query = em.CreateEntityQuery(typeof(SkirmishUnitRoleComponent),
                typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishVisualSpawnedComponent));
            using var actors = query.ToEntityArray(Allocator.Temp);
            var seen = new HashSet<string>();
            foreach (var actor in actors)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                var role = em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role;
                string id = owner.StableObjectId.ToString();
                if (!owner.SessionId.Equals(attempt) || !seen.Add(id) ||
                    em.GetComponentData<SkirmishVisualSpawnedComponent>(actor).Spawned == 0) return false;
                if (initial.TryGetValue(id, out var start))
                {
                    if (owner.ReservationId != 0 || owner.FactionId != start.Faction || role != start.Role) return false;
                    initialCount++;
                }
                else if (delivered.TryGetValue(id, out var production))
                {
                    if (owner.ReservationId != production.Reservation || owner.FactionId != production.Faction ||
                        role != production.Role) return false;
                    deliveredCount++;
                }
                else return false;
            }
            return initialCount == initial.Count;
        }
    }
}
#endif
