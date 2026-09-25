using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public static class SkirmishArmyCommandService
    {
        // Called only after a public/shared target request has selected this source.
        // The shared AttackMove owner executes travel and combat; no second order
        // is issued by the UI's Attack-mode button.
        public static bool TryAttackMember(EntityManager em, Entity unit, Entity target)
        {
            if (!em.HasComponent<SkirmishAttemptOwnedComponent>(unit) ||
                !em.HasComponent<SkirmishArmyGroupMembershipComponent>(unit) ||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(target) ||
                !SkirmishArmyGroupSystem.IsAlive(em, target) || !SkirmishFogService.IsVisible(em, target)) return false;
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
            var targetOwner = em.GetComponentData<SkirmishAttemptOwnedComponent>(target);
            if (!owner.SessionId.Equals(targetOwner.SessionId) || owner.FactionId == targetOwner.FactionId) return false;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishArmyGroupRecord));
            using var entities = sessions.ToEntityArray(Allocator.Temp);
            foreach (var session in entities)
            {
                var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                if (!state.SessionId.Equals(owner.SessionId) || state.Phase != SkirmishSessionPhase.Playing) continue;
                if (!ApplyOrder(em, unit, SkirmishGroupOrderKind.Attack, em.GetComponentData<LocalTransform>(target).Position, target)) return false;
                AriaCommandEvidence.Accepted("ArmyAttack", owner.FactionId);
                uint groupId = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).GroupId;
                var groups = em.GetBuffer<SkirmishArmyGroupRecord>(session);
                for (int i = 0; i < groups.Length; i++)
                    if (groups[i].GroupId == groupId && groups[i].FactionId == owner.FactionId)
                    {
                        var group = groups[i]; group.LastOrder = SkirmishGroupOrderKind.Attack; groups[i] = group;
                        break;
                    }
                return true;
            }
            return false;
        }

        public static bool TryHold(
            EntityManager em,
            Entity session,
            out SkirmishCommandDecision decision)
        {
            return Issue(em, session, SkirmishGroupOrderKind.Hold, Entity.Null, default, out decision);
        }

        public static bool TryMove(
            EntityManager em,
            Entity session,
            out SkirmishCommandDecision decision)
        {
            return TryMove(em, session, SkirmishWorldMovementService.DefaultAdvance(em, session), out decision);
        }

        public static bool TryMove(
            EntityManager em,
            Entity session,
            float3 destination,
            out SkirmishCommandDecision decision)
        {
            return Issue(em, session, SkirmishGroupOrderKind.Move, Entity.Null, destination, out decision);
        }

        public static bool TryAttack(
            EntityManager em,
            Entity session,
            Entity target,
            out SkirmishCommandDecision decision)
        {
            decision = default;
            decision.Order = SkirmishGroupOrderKind.Attack;
            decision.Field = "target";
            if (target == Entity.Null || !em.Exists(target) || !em.HasComponent<SkirmishAttemptOwnedComponent>(target))
            {
                decision.Reason = SkirmishReasonCode.MissingReference;
                return false;
            }

            var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(target);
            if (owned.FactionId == 1)
            {
                decision.Reason = SkirmishReasonCode.InvalidSelection;
                return false;
            }

            if (!SkirmishArmyGroupSystem.IsAlive(em, target) && owned.IsStructure == 0)
            {
                decision.Reason = SkirmishReasonCode.InvalidSelection;
                return false;
            }

            if (!SkirmishFogService.IsVisible(em, target))
            {
                decision.Reason = SkirmishReasonCode.HiddenContact;
                return false;
            }

            float3 destination = em.HasComponent<LocalTransform>(target)
                ? em.GetComponentData<LocalTransform>(target).Position
                : SkirmishWorldMovementService.DefaultAdvance(em, session);
            return Issue(em, session, SkirmishGroupOrderKind.Attack, target, destination, out decision);
        }

        public static bool TryIssueGroupOrder(
            EntityManager em,
            Entity session,
            uint groupId,
            byte actingFaction,
            SkirmishGroupOrderKind order,
            Entity target,
            out SkirmishCommandDecision decision)
        {
            decision = default;
            decision.Order = order;
            decision.GroupId = groupId;
            decision.Field = "groupId";
            if (!SkirmishArmyGroupSystem.TryGet(em, session, groupId, out SkirmishArmyGroupRecord record) ||
                record.FactionId != actingFaction ||
                record.AliveCount <= 0)
            {
                decision.Reason = SkirmishReasonCode.InvalidSelection;
                return false;
            }

            if (order == SkirmishGroupOrderKind.Attack)
            {
                if (target == Entity.Null || !em.Exists(target) || !em.HasComponent<SkirmishAttemptOwnedComponent>(target))
                {
                    decision.Reason = SkirmishReasonCode.MissingReference;
                    decision.Field = "target";
                    return false;
                }

                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(target);
                if (owned.FactionId == actingFaction)
                {
                    decision.Reason = SkirmishReasonCode.InvalidSelection;
                    return false;
                }

                if (!SkirmishFogService.IsVisible(em, target))
                {
                    decision.Reason = SkirmishReasonCode.HiddenContact;
                    return false;
                }
            }

            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(
                typeof(SkirmishArmyGroupMembershipComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            int issued = 0;
            int excluded = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                var unitOwned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (!unitOwned.SessionId.Equals(sessionId) || unitOwned.FactionId != actingFaction)
                    continue;
                if (em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).GroupId != groupId)
                    continue;
                if (!SkirmishArmyGroupSystem.IsAlive(em, unit))
                    continue;
                if (!Accepts(em, unit, order))
                {
                    excluded++;
                    continue;
                }

                float3 destination = order == SkirmishGroupOrderKind.Attack &&
                                     target != Entity.Null &&
                                     em.HasComponent<LocalTransform>(target)
                    ? em.GetComponentData<LocalTransform>(target).Position
                    : SkirmishWorldMovementService.DefaultAdvance(em, session);
                if (ApplyOrder(em, unit, order, destination, target)) issued++;
                else excluded++;
            }

            decision.IssuedCount = issued;
            decision.ExcludedCount = excluded;
            if (issued == 0)
            {
                decision.Reason = excluded > 0
                    ? SkirmishReasonCode.IncompatibleOrder
                    : SkirmishReasonCode.InvalidSelection;
                return false;
            }

            if (em.HasBuffer<SkirmishArmyGroupRecord>(session))
            {
                DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
                for (int i = 0; i < buffer.Length; i++)
                {
                    SkirmishArmyGroupRecord stamped = buffer[i];
                    if (stamped.GroupId != groupId)
                        continue;
                    stamped.LastOrder = order;
                    buffer[i] = stamped;
                    break;
                }
            }

            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            AriaCommandEvidence.Accepted("GroupOrder", actingFaction);
            return true;
        }

        private static bool Issue(
            EntityManager em,
            Entity session,
            SkirmishGroupOrderKind order,
            Entity target,
            float3 destination,
            out SkirmishCommandDecision decision)
        {
            decision = default;
            decision.Order = order;
            decision.Field = "selection";
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(
                typeof(SelectedUnitTag),
                typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishArmyGroupMembershipComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            int issued = 0;
            int excluded = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(unit).SessionId.Equals(sessionId))
                    continue;
                if (!SkirmishArmyGroupSystem.IsAlive(em, unit))
                    continue;
                if (!Accepts(em, unit, order))
                {
                    excluded++;
                    continue;
                }

                if (ApplyOrder(em, unit, order, destination, target)) issued++;
                else excluded++;
            }

            decision.IssuedCount = issued;
            decision.ExcludedCount = excluded;
            if (issued == 0)
            {
                decision.Reason = excluded > 0
                    ? SkirmishReasonCode.IncompatibleOrder
                    : SkirmishReasonCode.InvalidSelection;
                return false;
            }

            StampGroups(em, session, order);
            AriaCommandEvidence.Accepted("SelectionOrder", 1);
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            return true;
        }

        private static bool Accepts(EntityManager em, Entity unit, SkirmishGroupOrderKind order)
        {
            if (order != SkirmishGroupOrderKind.Move)
                return true;
            SkirmishPopulationCategory domain = em.HasComponent<SkirmishArmyGroupMembershipComponent>(unit)
                ? em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).Domain
                : em.GetComponentData<SkirmishUnitRoleComponent>(unit).Category;
            return domain == SkirmishPopulationCategory.Infantry ||
                   domain == SkirmishPopulationCategory.Ground ||
                   domain == SkirmishPopulationCategory.LogisticsSupport ||
                   (domain == SkirmishPopulationCategory.Air &&
                    SkirmishSharedCombatBinding.UsesSharedCommands(em, unit) &&
                    em.HasComponent<UnitAirMovement>(unit));
        }

        private static bool ApplyOrder(
            EntityManager em,
            Entity unit,
            SkirmishGroupOrderKind order,
            float3 destination,
            Entity target)
        {
            if (order == SkirmishGroupOrderKind.Hold)
            {
                SkirmishWorldMovementService.ClearIntent(em, unit);
                if (!em.HasComponent<HoldPositionOrderTag>(unit))
                    em.AddComponent<HoldPositionOrderTag>(unit);
                return true;
            }

            if (em.HasComponent<HoldPositionOrderTag>(unit))
                em.RemoveComponent<HoldPositionOrderTag>(unit);
            float cooldown = 0f;
            byte engaged = 0;
            if (order == SkirmishGroupOrderKind.Attack &&
                em.HasComponent<SkirmishMoveIntentComponent>(unit))
            {
                SkirmishMoveIntentComponent previous = em.GetComponentData<SkirmishMoveIntentComponent>(unit);
                if (previous.Order == order && previous.AttackTarget == target)
                {
                    if (previous.Active != 0 && SkirmishSharedCombatBinding.UsesSharedCommands(em, unit))
                        return true;
                    // Enemy Evaluate reissues Attack every tick. Resetting the
                    // cooldown made that group fire once per frame.
                    cooldown = previous.Cooldown;
                    engaged = previous.Engaged;
                }
            }

            if (!SkirmishWorldMovementService.AssignIntent(em, unit, destination, order)) return false;
            if (!em.HasComponent<SkirmishMoveIntentComponent>(unit))
                return false;
            var intent = em.GetComponentData<SkirmishMoveIntentComponent>(unit);
            intent.AttackTarget = order == SkirmishGroupOrderKind.Attack ? target : Entity.Null;
            intent.Cooldown = cooldown;
            intent.Engaged = engaged;
            em.SetComponentData(unit, intent);
            return true;
        }

        private static void StampGroups(EntityManager em, Entity session, SkirmishGroupOrderKind order)
        {
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session))
                return;
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                SkirmishArmyGroupRecord record = buffer[i];
                if (record.Selected == 0)
                    continue;
                record.LastOrder = order;
                buffer[i] = record;
            }
        }
    }
}
