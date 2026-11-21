using System;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    // W41b (Mac lane 49 S058, H6): the group record stayed Selected while no living member carried the
    // selection tag (tagged members died; reinforcements join untagged). ARIA saw an exclusive selection and
    // never tapped the card again, while the Move button refused NoSelection on every press (AriaBlocked 101).
    // The public objective projection now reports a group as selected only when every living member is tagged.
    public sealed class AriaStaleGroupSelectionTests
    {
        // ---------- pure rule (UiSkirmishPublicGroupSelection.Selected) ----------

        [TestCase(true, 4, 4, true)]   // (a) record and tags agree
        [TestCase(true, 0, 2, false)]  // (b) tagged members died, the living ones are untagged
        [TestCase(true, 3, 4, false)]  // (c) partly tagged (a reinforcement joined untagged)
        [TestCase(false, 0, 4, false)] // (d) unselected
        [TestCase(false, 4, 4, false)] // (d) stray tags never make an unselected record selected
        public void GroupIsSelectedOnlyWhenEveryLivingMemberIsTagged(bool record, int tagged, int living, bool expected) =>
            Assert.AreEqual(expected, UiSkirmishPublicGroupSelection.Selected(record, tagged, living));

        // ---------- projection (UiShellEcsGateway.TryOwnObjectiveGroups) on the S002 starting forces ----------

        [Test]
        public void RecordAndTagsAgreeIsSelected()
        {
            using var world = new World(nameof(RecordAndTagsAgreeIsSelected));
            var em = world.EntityManager; CompileAndSpawn(em, out var session);
            uint rifle = PlayerGroup(em, session, SkirmishRoleKind.Rifle);
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, rifle, false, out _));
            var row = Row(em, session, rifle);
            Assert.IsTrue(row.Selected, "(a) unchanged");
            Assert.IsTrue(row.RecordSelected); Assert.AreEqual(4, row.AliveCount); Assert.AreEqual(4, row.SelectedTaggedAlive);
        }

        [Test]
        public void DeadTaggedMembersLeaveTheGroupUnselectedAndOneCardTapSelectsTheLivingMembers()
        {
            using var world = new World(nameof(DeadTaggedMembersLeaveTheGroupUnselectedAndOneCardTapSelectsTheLivingMembers));
            var em = world.EntityManager; CompileAndSpawn(em, out var session);
            uint rifle = PlayerGroup(em, session, SkirmishRoleKind.Rifle);
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, rifle, false, out _));
            var members = Members(em, rifle);
            Assert.AreEqual(4, members.Length);
            // UnitDeathSystem strips the tag of a member that dies; the record stays Selected.
            Kill(em, members[0]); Kill(em, members[1]);
            // Members delivered later join the group with SelectedUnitTag stripped at spawn.
            em.RemoveComponent<SelectedUnitTag>(members[2]); em.RemoveComponent<SelectedUnitTag>(members[3]);
            var row = Row(em, session, rifle);
            Assert.IsFalse(row.Selected, "(b) record Selected but no living member tagged");
            Assert.IsTrue(row.RecordSelected); Assert.AreEqual(2, row.AliveCount); Assert.AreEqual(0, row.SelectedTaggedAlive);
            Assert.IsFalse(SkirmishArmySelectionService.HasSelectedLivingPlayerMember(em, session, rifle));

            // The card tap path (SelectionGameplayStartupSystemHelper -> TryTogglePresentedGroup) toggles on live tags:
            // with no tagged living member it selects, so one tap re-tags every living member.
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryTogglePresentedGroup(em, session, rifle));
            row = Row(em, session, rifle);
            Assert.IsTrue(row.Selected); Assert.AreEqual(2, row.SelectedTaggedAlive); Assert.AreEqual(2, row.AliveCount);
        }

        [Test]
        public void PartlyTaggedGroupIsUnselectedUntilDeselectedAndSelectedAgain()
        {
            using var world = new World(nameof(PartlyTaggedGroupIsUnselectedUntilDeselectedAndSelectedAgain));
            var em = world.EntityManager; CompileAndSpawn(em, out var session);
            uint rifle = PlayerGroup(em, session, SkirmishRoleKind.Rifle);
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, rifle, false, out _));
            var members = Members(em, rifle);
            em.RemoveComponent<SelectedUnitTag>(members[3]); // a reinforcement that joined untagged
            var row = Row(em, session, rifle);
            Assert.IsFalse(row.Selected, "(c) partial tag set");
            Assert.IsTrue(row.RecordSelected); Assert.AreEqual(4, row.AliveCount); Assert.AreEqual(3, row.SelectedTaggedAlive);

            // Tapping a card that still has tagged living members deselects it ...
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryTogglePresentedGroup(em, session, rifle));
            row = Row(em, session, rifle);
            Assert.IsFalse(row.Selected); Assert.IsFalse(row.RecordSelected); Assert.AreEqual(0, row.SelectedTaggedAlive);
            // ... and the next tap selects every living member.
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryTogglePresentedGroup(em, session, rifle));
            row = Row(em, session, rifle);
            Assert.IsTrue(row.Selected); Assert.AreEqual(4, row.SelectedTaggedAlive);
        }

        [Test]
        public void UnselectedGroupStaysUnselected()
        {
            using var world = new World(nameof(UnselectedGroupStaysUnselected));
            var em = world.EntityManager; CompileAndSpawn(em, out var session);
            uint rifle = PlayerGroup(em, session, SkirmishRoleKind.Rifle), gunner = PlayerGroup(em, session, SkirmishRoleKind.Gunner);
            Assert.IsFalse(Row(em, session, gunner).Selected, "(d) nothing selected");
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, rifle, false, out _));
            var row = Row(em, session, gunner);
            Assert.IsFalse(row.Selected, "(d) another group's selection");
            Assert.IsFalse(row.RecordSelected); Assert.AreEqual(0, row.SelectedTaggedAlive);
            // A stray tag on an unselected record does not make it selected.
            em.AddComponent<SelectedUnitTag>(Members(em, gunner)[0]);
            Assert.IsFalse(Row(em, session, gunner).Selected);
        }

        // ---------- planner (StepPublicObjective) on the corrected frame ----------

        [Test]
        public void DriftedSelectionIsTappedBeforeMoveIsPressed()
        {
            // (b) as the planner sees it after the fix: ExclusiveGroupSelected is false, the stale card is the
            // selected card (ClearOtherSelection), Move mode is off.
            var p = new AriaSkirmishPlanComponent(); var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var v = Presented(400);
            v.PublicObjective.GroupId = 39; v.PublicObjective.PublicKey = 22; v.PublicObjective.Action = AriaObjectiveAction.Move;
            v.PublicObjective.Intent = AriaSkirmishIntent.ControlZone;
            v.PublicObjective.ClearOtherSelection = T(31); v.PublicObjective.SelectGroup = T(31); v.PublicObjective.Move = T(33);
            v.PublicObjective.Focus = T(34); v.PublicObjective.GoalInView = true; v.PublicObjective.GroundFitsPublicGoal = true;
            v.PublicObjective.Ground = T(0x30000001);
            Step(v, ref p, ref t, out var o);
            Assert.AreEqual(31, o.TargetId, "the stale selected card is tapped, not Move");
            Press(ref t, 31);
            // One tap re-selects every living member: the frame now reports the exclusive selection.
            v.Frame++; v.Time += .5f; v.PublicObjective.ExclusiveGroupSelected = true; v.PublicObjective.ClearOtherSelection = default;
            Step(v, ref p, ref t, out o);
            Assert.AreEqual(33, o.TargetId, "Move is pressed only once the group is really selected");
            Press(ref t, 33);
            v.Frame++; v.Time += .5f; v.PublicObjective.MoveMode = true;
            Step(v, ref p, ref t, out o);
            Assert.AreEqual(0x30000001, o.TargetId); Assert.AreEqual(AriaPlayObservationKind.WorldTarget, o.Kind);
            Assert.AreEqual(0, p.ObjectiveFailures); Assert.AreNotEqual(AriaPlayPhase.Blocked, t.Phase);
        }

        [Test]
        public void ExclusiveSelectionWithoutMoveModeStillPressesMove()
        {
            // (a) unchanged: record and tags agree, so the card is not tapped again.
            var p = new AriaSkirmishPlanComponent(); var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var v = Presented(400);
            v.PublicObjective.GroupId = 39; v.PublicObjective.PublicKey = 22; v.PublicObjective.Action = AriaObjectiveAction.Move;
            v.PublicObjective.Intent = AriaSkirmishIntent.ControlZone; v.PublicObjective.ExclusiveGroupSelected = true;
            v.PublicObjective.SelectGroup = T(31); v.PublicObjective.Move = T(33); v.PublicObjective.GoalInView = true;
            Step(v, ref p, ref t, out var o);
            Assert.AreEqual(33, o.TargetId);
        }

        static void Press(ref AriaPlaySessionComponent t, int id)
        { if (t.TargetId != id) { t.TargetId = id; t.Attempts = 0; } t.Attempts++; t.Actions++; }

        static AriaTouchTarget T(int id) => new AriaTouchTarget { Available = true, Id = id };

        static AriaSkirmishObservation Presented(float time) => new AriaSkirmishObservation
        {
            Active = true, ExpandedSession = true, Frame = 700, Time = time, ObjectiveType = 1,
            PublicObjective = new AriaObjectiveTouchObservation { Applicable = true, Supported = true, Presented = true, PresentationEpoch = 1,
                Intent = AriaSkirmishIntent.ObserveObjective, CameraSettled = true, ProgressKnown = true, PublicProgressRevision = 1 },
            ObjectiveSupport = new AriaObjectiveSupportObservation { Presented = true, SimulationSeconds = time - 50, DismountedCombatGroups = 3, CaptureMajority = true }
        };

        static void Step(in AriaSkirmishObservation v, ref AriaSkirmishPlanComponent p, ref AriaPlaySessionComponent t, out AriaPlayObservationComponent o)
        { o = default; AriaSkirmishPlanSystem.Step(v, ref p, ref t, ref o); }

        // ---------- fixture ----------

        static UiSkirmishPublicGroupRow Row(EntityManager em, Entity session, uint group)
        {
            // The projection reads the goal only for designated originals; an empty goal keeps it out of the way.
            var ctor = typeof(SkirmishPublicGoalSnapshot).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();
            var goal = (SkirmishPublicGoalSnapshot)ctor.Invoke(new object[] { "W41b", 1, 1, default(SkirmishObjectiveKind),
                default(SkirmishPublicGoalPerspective), 0L, 0L, SkirmishOutcomeKind.None, default(SkirmishPublicGoalScore), Array.Empty<SkirmishPublicGoalNode>() });
            var method = typeof(UiShellEcsGateway).GetMethod("TryOwnObjectiveGroups", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            var args = new object[] { em, session, goal, null };
            Assert.IsTrue((bool)method.Invoke(null, args), "the own-group projection must read the S002 starting forces: " + Diagnose(em, session));
            var rows = (UiSkirmishPublicGroupRow[])args[3];
            var row = rows.SingleOrDefault(r => r.Policy.GroupId == group);
            Assert.IsNotNull(row, "group " + group + " projected");
            return row;
        }

        static string Diagnose(EntityManager em, Entity session)
        {
            var sb = new System.Text.StringBuilder();
            var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            var ids = new System.Collections.Generic.HashSet<uint>();
            foreach (var g in em.GetBuffer<SkirmishArmyGroupRecord>(session))
                if (g.FactionId == 1 && g.AliveCount > 0 && (g.GroupId == 0 || !ids.Add(g.GroupId))) sb.Append("dupGroup=" + g.GroupId + " ");
            using var query = em.CreateEntityQuery(new EntityQueryDesc { All = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),
                ComponentType.ReadOnly<SkirmishArmyGroupMembershipComponent>(), ComponentType.ReadOnly<UnitHealth>(), ComponentType.ReadOnly<Faction>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState });
            using var actors = query.ToEntityArray(Allocator.Temp);
            sb.Append("actors=" + actors.Length + " ");
            foreach (var actor in actors)
            {
                var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (own.SessionId != attempt || own.FactionId != 1 || own.IsStructure != 0 || own.CapacityReleased != 0 || em.GetComponentData<UnitHealth>(actor).Current <= 0) continue;
                var current = actor; int depth = 0;
                while (current != Entity.Null && depth++ < 8)
                {
                    if (!em.Exists(current) || !em.HasComponent<Unity.Transforms.LocalTransform>(current) || !em.HasComponent<SkirmishAttemptOwnedComponent>(current) || !em.HasComponent<Faction>(current))
                    { sb.Append($"actor={actor.Index} group={em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId} bad={current.Index} lt={em.HasComponent<Unity.Transforms.LocalTransform>(current)} own={em.HasComponent<SkirmishAttemptOwnedComponent>(current)} fac={em.HasComponent<Faction>(current)}; "); break; }
                    var o2 = em.GetComponentData<SkirmishAttemptOwnedComponent>(current);
                    if (o2.SessionId != attempt || o2.FactionId != 1 || o2.CapacityReleased != 0 || em.GetComponentData<Faction>(current).Id != 1)
                    { sb.Append($"actor={actor.Index} chain={current.Index} faction={em.GetComponentData<Faction>(current).Id}/{o2.FactionId}; "); break; }
                    current = em.HasComponent<Unity.Transforms.Parent>(current) ? em.GetComponentData<Unity.Transforms.Parent>(current).Value : Entity.Null;
                }
                if (sb.Length > 1500) break;
            }
            return sb.ToString();
        }

        static Entity[] Members(EntityManager em, uint group)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishArmyGroupMembershipComponent), typeof(SkirmishAttemptOwnedComponent), typeof(UnitHealth));
            using var all = query.ToEntityArray(Allocator.Temp);
            return all.ToArray().Where(e => em.GetComponentData<SkirmishArmyGroupMembershipComponent>(e).GroupId == group &&
                em.GetComponentData<UnitHealth>(e).Current > 0).OrderBy(e => e.Index).ToArray();
        }

        static void Kill(EntityManager em, Entity unit)
        {
            var hp = em.GetComponentData<UnitHealth>(unit); hp.Current = 0; em.SetComponentData(unit, hp);
            if (em.HasComponent<SelectedUnitTag>(unit)) em.RemoveComponent<SelectedUnitTag>(unit);
        }

        static uint PlayerGroup(EntityManager em, Entity session, SkirmishRoleKind role)
        {
            foreach (var record in em.GetBuffer<SkirmishArmyGroupRecord>(session))
                if (record.FactionId == 1 && record.Role == role) return record.GroupId;
            Assert.Fail("no player " + role + " group"); return 0;
        }

        // Same S002 v2 starting forces as SkirmishExpandedArmyTests.
        static void CompileAndSpawn(EntityManager em, out Entity session)
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
            const int s002Version = 2;
            var catalog = Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
            Assert.IsNotNull(catalog, "The fixture requires the packaged Skirmish catalog.");
            Assert.IsTrue(catalog.TryGetDefinition("S002", s002Version, out var definition), "The fixture requires the reviewed S002 v2 binding.");
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(em, "S002", SkirmishDifficultyId.Regular, SkirmishSizeId.Standard,
                104731, catalog.ToAuthoredSet(), matrix, manifest, out var setup, out _, out var reasons, contentVersion: s002Version),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(em, session, setup, out SkirmishReasonCode reason, out byte visualPending), reason.ToString());
            Assert.AreEqual(0, visualPending);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            var sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            SkirmishRosterProjectionSystem.Apply(em, owned, sessionId, setup);
            SkirmishArmyGroupSystem.RefreshAlive(em, session, owned, sessionId);
            // The headless fixture has no map grid, so the ledger spawn leaves the actors unplaced. The public
            // projection only reads own positions; place every owned actor at a distinct finite point.
            using var actors = owned.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < actors.Length; i++)
                if (!em.HasComponent<Unity.Transforms.LocalTransform>(actors[i]))
                    em.AddComponentData(actors[i], Unity.Transforms.LocalTransform.FromPosition(new Unity.Mathematics.float3(i * 3f, 0f, 10f)));
        }
    }
}
