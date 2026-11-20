using System.Collections.Generic;
using System.Text;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime.Combat
{
    // W37 diagnosis (lead, 2026-11-20): evidence trace only, no behaviour change. Logs each hit on a Convoy Escort
    // objective truck: attacker type/role/owner, the truck's path progress, order and motion, and each player group's
    // distance to the truck. It reads components only. Rate limit: one line per truck and attacker per 1 s, and every
    // first hit and killing hit.
    public static class SkirmishConvoyTruckDamageTrace
    {
        private const float MinInterval = 1f;
        private const int MaxGroups = 12;
        private static readonly Dictionary<string, float> LastLine = new();
        private static readonly Dictionary<string, float4> LastSample = new();
        private static string sessionSeen;

        public static void Trace(EntityManager em, Entity source, Entity target, string kind, int before, int after)
        {
            if (before <= after || !em.Exists(target) || !em.HasComponent<SkirmishConvoyObjectiveTruckComponent>(target) ||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(target)) return;
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(target);
            using var sessionQuery = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishObjectiveClockComponent));
            using var sessions = sessionQuery.ToEntityArray(Allocator.Temp);
            foreach (var session in sessions)
            {
                var match = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                if (match.IsLegacy != 0 || match.Phase != SkirmishSessionPhase.Playing || match.ObjectiveKind != SkirmishObjectiveKind.ConvoyEscort ||
                    !match.SessionId.Equals(owner.SessionId)) continue;
                string sessionId = match.SessionId.ToString();
                if (sessionSeen != sessionId) { sessionSeen = sessionId; LastLine.Clear(); LastSample.Clear(); }
                float now = em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds;
                Log(em, session, source, target, kind, before, after, now);
                return;
            }
        }

        private static void Log(EntityManager em, Entity session, Entity source, Entity target, string kind, int before, int after, float now)
        {
            byte slot = em.GetComponentData<SkirmishConvoyObjectiveTruckComponent>(target).Slot;
            float3 truckPos = Cell(em, target);
            string truckKey = target.Index + ":" + target.Version;
            // Speed from the previous hit sample on this truck (hits arrive about once a second while under fire).
            string speed = "-";
            if (LastSample.TryGetValue(truckKey, out float4 prev) && now > prev.w + 0.05f)
                speed = (math.distance(prev.xyz, truckPos) / (now - prev.w)).ToString("F2") + "/" + (now - prev.w).ToString("F1") + "s";
            LastSample[truckKey] = new float4(truckPos, now);

            string attackerKey = truckKey + ":" + source.Index + ":" + source.Version;
            bool first = !LastLine.ContainsKey(truckKey), kill = after <= 0;
            if (!first && !kill && LastLine.TryGetValue(attackerKey, out float last) && now >= last && now - last < MinInterval) return;
            LastLine[truckKey] = now; LastLine[attackerKey] = now;

            string role = "unknown", faction = "unknown", stable = "unknown", attackerGroup = "-", attackerDist = "-";
            if (source != Entity.Null && em.Exists(source))
            {
                if (em.HasComponent<SkirmishUnitRoleComponent>(source)) role = em.GetComponentData<SkirmishUnitRoleComponent>(source).Role.ToString();
                if (em.HasComponent<Faction>(source)) faction = em.GetComponentData<Faction>(source).Id.ToString();
                if (em.HasComponent<SkirmishAttemptOwnedComponent>(source)) stable = em.GetComponentData<SkirmishAttemptOwnedComponent>(source).StableObjectId.ToString();
                if (em.HasComponent<SkirmishArmyGroupMembershipComponent>(source)) attackerGroup = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(source).GroupId.ToString();
                if (em.HasComponent<UnitGrid>(source)) attackerDist = math.distance(Cell(em, source), truckPos).ToString("F1");
            }

            uint truckGroup = em.HasComponent<SkirmishArmyGroupMembershipComponent>(target) ? em.GetComponentData<SkirmishArmyGroupMembershipComponent>(target).GroupId : 0u;
            string order = "-";
            if (truckGroup != 0 && em.HasBuffer<SkirmishArmyGroupRecord>(session))
                foreach (var record in em.GetBuffer<SkirmishArmyGroupRecord>(session))
                    if (record.GroupId == truckGroup) { order = record.LastOrder.ToString(); break; }
            bool moving = em.HasComponent<UnitPathFollow>(target) && em.HasComponent<UnitPathRange>(target);
            string path = moving ? em.GetComponentData<UnitPathFollow>(target).PathIndex + "/" + em.GetComponentData<UnitPathRange>(target).Length : "-";
            string goal = em.HasComponent<UnitLongDistanceMove>(target) ? em.GetComponentData<UnitLongDistanceMove>(target).FinalGoal.ToString() :
                em.HasComponent<UnitTarget>(target) ? em.GetComponentData<UnitTarget>(target).Cell.ToString() : "-";
            string cell = em.HasComponent<UnitGrid>(target) ? em.GetComponentData<UnitGrid>(target).Cell.ToString() : "-";

            UnityEngine.Debug.Log("[ConvoyTruckHit] slot=" + slot + " truck=" + truckKey + " group=" + truckGroup + " hp=" + after + "/" +
                (em.HasComponent<UnitHealth>(target) ? em.GetComponentData<UnitHealth>(target).Max : 0) + " damage=" + (before - after) +
                " source=" + kind + " attacker=" + source.Index + ":" + source.Version + " role=" + role + " faction=" + faction + " attacker_group=" + attackerGroup +
                " attacker_id=" + stable + " attacker_dist=" + attackerDist + " order=" + order + " state=" + (moving ? "moving" : "waiting") + " path=" + path +
                " goal=" + goal + " cell=" + cell + " pos=(" + truckPos.x.ToString("F1") + "," + truckPos.z.ToString("F1") + ") speed=" + speed +
                " escorts=" + Escorts(em, target, truckGroup, truckPos) + (first ? " first=1" : "") + (kill ? " kill=1" : "") + " t=" + now.ToString("F2"));
        }

        // Positions are grid cells (Game.Runtime.Combat does not reference Unity.Transforms); distances are in cells.
        private static float3 Cell(EntityManager em, Entity e)
        {
            if (!em.HasComponent<UnitGrid>(e)) return float3.zero;
            int2 c = em.GetComponentData<UnitGrid>(e).Cell; return new float3(c.x, 0f, c.y);
        }

        // Player (faction 1) groups other than the convoy trucks: nearest living member's distance to the hit truck.
        private static string Escorts(EntityManager em, Entity truck, uint truckGroup, float3 truckPos)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishArmyGroupMembershipComponent), typeof(Faction), typeof(UnitGrid), typeof(SkirmishUnitRoleComponent));
            using var units = query.ToEntityArray(Allocator.Temp);
            var nearest = new Dictionary<uint, (float dist, string role, int alive)>();
            foreach (var unit in units)
            {
                if (unit == truck || em.GetComponentData<Faction>(unit).Id != 1 || em.HasComponent<SkirmishConvoyObjectiveTruckComponent>(unit)) continue;
                if (em.HasComponent<UnitHealth>(unit) && em.GetComponentData<UnitHealth>(unit).Current <= 0) continue;
                uint group = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).GroupId;
                if (group == 0 || group == truckGroup) continue;
                float d = math.distance(Cell(em, unit), truckPos);
                string r = em.GetComponentData<SkirmishUnitRoleComponent>(unit).Role.ToString();
                nearest[group] = nearest.TryGetValue(group, out var v) ? (math.min(v.dist, d), v.role, v.alive + 1) : (d, r, 1);
            }
            var list = new List<KeyValuePair<uint, (float dist, string role, int alive)>>(nearest);
            list.Sort((a, b) => a.Value.dist.CompareTo(b.Value.dist));
            var sb = new StringBuilder("[");
            for (int i = 0; i < list.Count && i < MaxGroups; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(list[i].Key).Append(':').Append(list[i].Value.role).Append('x').Append(list[i].Value.alive).Append(':').Append(list[i].Value.dist.ToString("F0"));
            }
            return sb.Append(']').ToString();
        }
    }
}
