using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private const float BaseThreatRecentSeconds = 8f;

        // Base-under-fire facts for the HUD. Everything published here is public to the
        // player: their own base and squads, and the attacker only while fog-visible to
        // faction 1. Read-only: no selection, camera or order changes happen here.
        private static void ReadBaseThreat(EntityManager em, Entity session, in SkirmishMatchState match, ref UiSkirmishModel model)
        {
            Entity home = match.PlayerMainBase;
            if (!SkirmishPublicObjectiveProjection.TryReadFocusPosition(em, home, out var homePosition)) return;
            model.PlayerBaseWorldKnown = true;
            model.PlayerBaseWorld = homePosition;

            using var queues = em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent));
            if (queues.CalculateEntityCount() != 1) return;
            Entity queue = queues.GetSingletonEntity();
            if (!em.HasBuffer<CombatDamageObservationElement>(queue)) return;
            float now = (float)em.WorldUnmanaged.Time.ElapsedTime;
            var observations = em.GetBuffer<CombatDamageObservationElement>(queue, true);
            int latest = -1;
            for (int i = 0; i < observations.Length; i++)
            {
                var observation = observations[i];
                if (observation.TargetEntity != home || observation.DamageApplied <= 0) continue;
                if (now - observation.ObservedAt > BaseThreatRecentSeconds) continue;
                if (latest < 0 || observation.EventId > observations[latest].EventId) latest = i;
            }
            if (latest < 0) return;
            model.PlayerBaseRecentlyHit = true;

            Entity source = observations[latest].SourceEntity;
            if (source != Entity.Null && em.Exists(source) && em.HasComponent<LocalTransform>(source) &&
                !em.HasComponent<Parent>(source) && SkirmishFogService.IsVisibleToFaction(em, source, 1))
            {
                model.BaseAttackerVisible = true;
                model.BaseAttackerWorld = em.GetComponentData<LocalTransform>(source).Position;
            }

            ReadNearestDefender(em, session, homePosition, ref model);
        }

        private static void ReadNearestDefender(EntityManager em, Entity session, Vector3 home, ref UiSkirmishModel model)
        {
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session) || !em.HasComponent<SkirmishExpandedSessionComponent>(session)) return;
            var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            var candidates = new Dictionary<uint, SkirmishArmyGroupRecord>();
            foreach (var group in em.GetBuffer<SkirmishArmyGroupRecord>(session, true))
                if (group.FactionId == 1 && group.AliveCount > 0 && group.GroupId != 0 &&
                    (group.Domain == SkirmishPopulationCategory.Infantry || group.Domain == SkirmishPopulationCategory.Ground))
                    candidates[group.GroupId] = group;
            if (candidates.Count == 0) return;

            using var query = em.CreateEntityQuery(
                ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),
                ComponentType.ReadOnly<SkirmishArmyGroupMembershipComponent>(),
                ComponentType.ReadOnly<UnitHealth>(),
                ComponentType.ReadOnly<LocalTransform>());
            using var actors = query.ToEntityArray(Allocator.Temp);
            var sums = new Dictionary<uint, Vector3>();
            var counts = new Dictionary<uint, int>();
            foreach (var actor in actors)
            {
                var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (!own.SessionId.Equals(attempt) || own.FactionId != 1 || own.IsStructure != 0 || own.CapacityReleased != 0) continue;
                if (em.GetComponentData<UnitHealth>(actor).Current <= 0 || em.HasComponent<Parent>(actor)) continue;
                uint id = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId;
                if (!candidates.ContainsKey(id)) continue;
                Vector3 position = em.GetComponentData<LocalTransform>(actor).Position;
                sums[id] = sums.TryGetValue(id, out var sum) ? sum + position : position;
                counts[id] = counts.TryGetValue(id, out int count) ? count + 1 : 1;
            }
            if (counts.Count == 0) return;
            var ids = new uint[counts.Count];
            var centers = new Vector2[counts.Count];
            int index = 0;
            foreach (var pair in counts)
            {
                Vector3 center = sums[pair.Key] / pair.Value;
                ids[index] = pair.Key;
                centers[index++] = new Vector2(center.x, center.z);
            }
            int nearest = SkirmishBaseThreatPolicy.NearestGroup(new Vector2(home.x, home.z), centers);
            if (nearest < 0) return;
            model.BaseDefenderGroupId = ids[nearest];
            model.BaseDefenderLabel = SkirmishSetupBriefing.RoleLabel(candidates[ids[nearest]].Role, GameLocalization.IsRightToLeft) +
                                      " (" + counts[ids[nearest]] + ")";
        }
    }
}
