using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishS002AriaHarnessTests
    {
        [Test]
        public void CheckedInRunsCsvStaysHeaderOnly()
        {
            string path = Path.Combine(ProjectRoot(), SkirmishAcceptanceScaffold.RelativeRunsPath);
            string[] lines = File.ReadAllLines(path);
            Assert.AreEqual(1, lines.Length, "runs.csv must stay header-only until a live match appends a row.");
            Assert.AreEqual(SkirmishAcceptanceScaffold.RequiredHeader, lines[0]);
        }

        [Test]
        public void ForcedVictoryDoesNotAppend()
        {
            string path = TempCsv();
            try
            {
                File.WriteAllText(path, SkirmishAcceptanceScaffold.RequiredHeader + "\n");
                var facts = Finished(SkirmishS002AriaRunLog.ResultVictory);
                facts.ForcedVictory = true;
                Assert.IsFalse(SkirmishS002AriaRunLog.TryAppendTerminalRow(path, in facts, out string row, out string error));
                Assert.IsNull(row);
                Assert.IsTrue(error.IndexOf("Forced Victory", StringComparison.Ordinal) >= 0);
                Assert.AreEqual(1, File.ReadAllLines(path).Length);
                Assert.IsFalse(SkirmishS002AriaRunLog.IsCountedAriaWin(in facts, SkirmishS002AriaRunLog.ResultVictory));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void UnfinishedOutcomeIsRefusedUntilTheMatchEnds()
        {
            string path = TempCsv();
            try
            {
                var unfinished = Finished(SkirmishS002AriaRunLog.ResultVictory);
                unfinished.MatchFinished = false;
                unfinished.Aborted = false;
                Assert.IsFalse(SkirmishS002AriaRunLog.TryAcceptTerminal(in unfinished, out _, out string error));
                Assert.IsTrue(error.IndexOf("has not finished", StringComparison.Ordinal) >= 0);
                Assert.IsFalse(SkirmishS002AriaRunLog.TryAppendTerminalRow(path, in unfinished, out _, out _));
                Assert.IsFalse(File.Exists(path));

                var aborted = unfinished;
                aborted.Aborted = true;
                aborted.RunId = "S002-aria-rs-104731-en-1";
                Assert.IsTrue(SkirmishS002AriaRunLog.TryAppendTerminalRow(path, in aborted, out string row, out _));
                Assert.AreEqual(SkirmishS002AriaRunLog.ResultAbort, Field(row, 13));
                Assert.IsFalse(row.IndexOf("," + SkirmishS002AriaRunLog.ResultVictory + ",", StringComparison.Ordinal) >= 0);
                Assert.IsFalse(SkirmishS002AriaRunLog.IsCountedAriaWin(in aborted, SkirmishS002AriaRunLog.ResultAbort));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void FinishedOutcomesAppendAndOnlyUnguidedVictoryCounts()
        {
            string path = TempCsv();
            try
            {
                Assert.IsTrue(SkirmishS002AriaRunLog.TryNextAriaRunId(
                    SkirmishAcceptanceScaffold.RequiredHeader + "\n",
                    104731,
                    GameLocalization.EnglishLocaleCode,
                    out string first,
                    out _));
                Assert.AreEqual("S002-aria-rs-104731-en-1", first);

                AppendFinished(path, "S002-aria-rs-104731-en-1", SkirmishS002AriaRunLog.ResultDefeat, true, 0, 0, false);
                AppendFinished(path, "S002-aria-rs-104731-en-2", SkirmishS002AriaRunLog.ResultDraw, true, 0, 0, false);
                AppendFinished(path, "S002-row-slow", SkirmishS002AriaRunLog.ResultVictory, false, 0, 0, false);
                AppendFinished(path, "S002-row-violation", SkirmishS002AriaRunLog.ResultVictory, true, 1, 0, false);
                AppendFinished(path, "S002-row-intervention", SkirmishS002AriaRunLog.ResultVictory, true, 0, 2, false);
                AppendFinished(path, "S002-aria-rs-130365-fa-1", SkirmishS002AriaRunLog.ResultVictory, true, 0, 0, true);

                string[] lines = File.ReadAllLines(path);
                Assert.AreEqual(7, lines.Length);
                Assert.AreEqual(SkirmishS002AriaRunLog.ResultDefeat, Field(lines[1], 13));
                Assert.AreEqual(SkirmishS002AriaRunLog.ResultDraw, Field(lines[2], 13));
                Assert.AreEqual("0", Field(lines[3], 11));
                Assert.AreEqual(SkirmishS002AriaRunLog.ResultVictory, Field(lines[6], 13));
                Assert.AreEqual("1", Field(lines[6], 11));
                Assert.AreEqual("0", Field(lines[6], 16));
                Assert.AreEqual("0", Field(lines[6], 17));

                string used = File.ReadAllText(path);
                Assert.IsTrue(SkirmishS002AriaRunLog.TryNextAriaRunId(
                    used, 104731, GameLocalization.EnglishLocaleCode, out string next, out _));
                Assert.AreEqual("S002-aria-rs-104731-en-3", next);

                string filled = used + "S002-aria-rs-104731-en-3,rest\n";
                Assert.IsFalse(SkirmishS002AriaRunLog.TryNextAriaRunId(
                    filled, 104731, GameLocalization.EnglishLocaleCode, out _, out string full));
                Assert.IsTrue(full.IndexOf("three", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void AttackOrdersDamageOnlyLegalVisibleTargets()
        {
            using var world = new World(nameof(AttackOrdersDamageOnlyLegalVisibleTargets));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session);
            int materials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            Entity barracks = DesignatedBase(em, 2);
            Entity rifle = FirstGroupMember(em, FirstFactionGroup(em, session, 1, SkirmishRoleKind.Rifle).GroupId);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity enemyTank = FirstFactionUnit(em, 2, SkirmishRoleKind.Tank);
            Place(em, barracks, new float3(0f, 0f, 0f));
            Place(em, rifle, new float3(10f, 0f, 0f));
            RevealAll(em);
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(
                em, session, GroupOf(em, rifle), 1, SkirmishGroupOrderKind.Attack, barracks, out _));
            for (int i = 0; i < 3; i++)
                SkirmishExpandedEngagementService.Step(em, session, 1f, false);
            Assert.AreEqual(800, em.GetComponentData<UnitHealth>(barracks).Current);

            Place(em, tank, new float3(10f, 0f, 0f));
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(
                em, session, GroupOf(em, tank), 1, SkirmishGroupOrderKind.Attack, barracks, out _));
            Assert.Greater(SkirmishExpandedEngagementService.Step(em, session, 1f, false), 0);
            Assert.AreEqual(772, em.GetComponentData<UnitHealth>(barracks).Current);

            var reset = em.GetComponentData<UnitHealth>(enemyTank);
            int before = reset.Current;
            Place(em, tank, new float3(0f, 0f, 0f));
            Place(em, enemyTank, new float3(8f, 0f, 0f));
            SkirmishFogService.Reveal(em, enemyTank);
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(
                em, session, GroupOf(em, tank), 1, SkirmishGroupOrderKind.Attack, enemyTank, out _));
            SkirmishFogService.Hide(em, enemyTank);
            SkirmishExpandedEngagementService.Step(em, session, 1f, false);
            Assert.AreEqual(before, em.GetComponentData<UnitHealth>(enemyTank).Current);
            Assert.AreEqual(materials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
        }

        [Test]
        public void AssaultColumnKillsTheEnemyTankAndLeavesThePlayerBarracks()
        {
            using var world = new World(nameof(AssaultColumnKillsTheEnemyTankAndLeavesThePlayerBarracks));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session);
            int materials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            Entity playerBase = DesignatedBase(em, 1);
            Entity enemyBase = DesignatedBase(em, 2);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity apc = FirstApc(em, 1);
            Entity car = FirstFactionUnit(em, 1, SkirmishRoleKind.Car);
            Entity enemyTank = FirstFactionUnit(em, 2, SkirmishRoleKind.Tank);
            Place(em, playerBase, new float3(-228f, 0f, 0f));
            Place(em, enemyBase, new float3(228f, 0f, 0f));
            Place(em, tank, new float3(-168f, 0f, 0f));
            Place(em, apc, new float3(-168f, 0f, 4f));
            Place(em, car, new float3(-168f, 0f, -4f));
            Place(em, enemyTank, new float3(168f, 0f, 0f));
            RevealAll(em);

            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, tank), false, out _));
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, apc), true, out _));
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, car), true, out _));
            Assert.IsTrue(SkirmishArmyCommandService.TryAttack(em, session, enemyBase, out _));
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(
                em,
                session,
                GroupOf(em, enemyTank),
                2,
                SkirmishGroupOrderKind.Attack,
                playerBase,
                out _));

            for (int step = 0; step < 180; step++)
            {
                SkirmishExpandedEngagementService.Step(em, session, 1f, false);
                SkirmishWorldMovementService.Step(em, session, 1f, false);
            }

            Assert.AreEqual(0, em.GetComponentData<UnitHealth>(enemyTank).Current);
            Assert.AreEqual(800, em.GetComponentData<UnitHealth>(playerBase).Current);
            Assert.Less(em.GetComponentData<UnitHealth>(enemyBase).Current, 800);
            Assert.Greater(em.GetComponentData<UnitHealth>(tank).Current, 0);
            Assert.AreEqual(materials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
        }

        [Test]
        public void ExpandedAssaultPlanUsesVisibleCardsWithoutMutatingStocks()
        {
            using var world = new World(nameof(ExpandedAssaultPlanUsesVisibleCardsWithoutMutatingStocks));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session);
            int materials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                Infantry = 20,
                ExpandedNextPage = true,
                ExpandedPageIndex = 0,
                Squad0 = new AriaTouchTarget { Id = 10, Available = true },
                Squad3 = new AriaTouchTarget { Id = 13, Available = true },
                Squad4 = new AriaTouchTarget { Id = 14, Available = true },
                Attack = new AriaTouchTarget { Id = 42, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
            Assert.AreEqual(14, output.TargetId);
            Assert.AreEqual(1, plan.PagedToAssault);
            Assert.AreEqual(0, plan.AssaultIssued);

            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0, plan.AssaultIssued);
            Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
            Assert.AreEqual(AriaPlayObservationKind.Control, output.Kind);
            Assert.AreEqual(14, output.TargetId);

            view.ExpandedPageIndex = 1;
            view.ExpandedAssaultMask = 1 << 3;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
            Assert.AreEqual(13, output.TargetId);
            Assert.AreEqual(1, plan.AssaultSelecting);

            view.ExpandedSelectedMask = 1 << 3;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Attack, plan.Intent);
            Assert.AreEqual(42, output.TargetId);
            Assert.AreEqual(1, plan.AssaultIssued);
            Assert.AreEqual(materials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
        }

        [Test]
        public void ExpandedAssaultDoesNotAttackWithRiflesWhileNextPageExists()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                Infantry = 20,
                SelectionVisible = true,
                ExpandedNextPage = true,
                ExpandedPageIndex = 0,
                ExpandedAssaultMask = 0,
                Squad0 = new AriaTouchTarget { Id = 10, Available = true },
                Squad4 = new AriaTouchTarget { Id = 14, Available = false },
                Attack = new AriaTouchTarget { Id = 42, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0, plan.AssaultIssued);
            Assert.AreEqual(AriaSkirmishIntent.Inspect, plan.Intent);
            Assert.AreEqual(AriaPlayObservationKind.Waiting, output.Kind);
            Assert.AreEqual(0, output.TargetId);
        }

        [Test]
        public void ExpandedAssaultStartsImmediatelyAndDoesNotBlockOnFlatHealth()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                Infantry = 20,
                Time = 0f,
                EnemyHealth = 800f,
                PlayerHealth = 800f,
                ForceHealth = 20f,
                ExpandedNextPage = true,
                ExpandedPageIndex = 0,
                Squad4 = new AriaTouchTarget { Id = 14, Available = true },
                Attack = new AriaTouchTarget { Id = 42, Available = true },
                Squad0 = new AriaTouchTarget { Id = 10, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0f, plan.OpeningUntil);
            Assert.AreEqual(0, plan.AssaultStarted);
            Assert.AreEqual(AriaPlayPhase.Observing, touch.Phase);
            Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
            Assert.AreEqual(14, output.TargetId);

            view.Time = 200f;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreNotEqual(AriaPlayPhase.Blocked, touch.Phase);
            Assert.AreEqual(0f, plan.OpeningUntil);
            Assert.AreEqual(0, plan.AssaultStarted);
            Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
            Assert.AreEqual(14, output.TargetId);

            view.ExpandedPageIndex = 1;
            view.ExpandedStructureMask = 1 << 3;
            view.ExpandedAssaultMask = (1 << 0) | (1 << 3);
            view.ExpandedSelectedMask = 1 << 0;
            view.SelectionVisible = true;
            view.Squad3 = new AriaTouchTarget { Id = 13, Available = false };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreNotEqual(AriaSkirmishIntent.Attack, plan.Intent);
            Assert.AreNotEqual(42, output.TargetId);

            view.ExpandedSelectedMask = (1 << 0) | (1 << 3);
            view.Squad3 = new AriaTouchTarget { Id = 13, Available = true };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Attack, plan.Intent);
            Assert.AreEqual(42, output.TargetId);
            Assert.AreEqual(0, plan.AssaultIssued);
        }

        [Test]
        public void BlockedExpandedPlanRestartsTheTouchDriver()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                Time = 400f,
                ExpandedNextPage = true,
                Squad4 = new AriaTouchTarget { Id = 14, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent
            {
                Phase = AriaPlayPhase.Blocked,
                Attempts = 3
            };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaPlayPhase.Starting, touch.Phase);
            Assert.AreEqual(0, touch.Attempts);
            Assert.AreEqual(0, touch.GestureRequested);
            Assert.AreEqual(400f, touch.DueAt, 0.001f);
            Assert.AreEqual(400f, touch.LastProgressAt, 0.001f);
            Assert.AreEqual(400f, touch.LastObjectiveProgressAt, 0.001f);
        }

        [Test]
        public void PendingPathRequestDoesNotFreezeLocalStep()
        {
            using var world = new World(nameof(PendingPathRequestDoesNotFreezeLocalStep));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Place(em, tank, new float3(0f, 0f, 0f));
            em.AddComponentData(tank, new UnitMove { Speed = 8f, ArriveDistance = 0.35f });
            em.AddComponentData(tank, new UnitPathRequest { Goal = new int2(4, 0) });
            SkirmishWorldMovementService.AssignIntent(em, tank, new float3(80f, 0f, 0f), SkirmishGroupOrderKind.Move);
            Assert.IsFalse(em.HasComponent<UnitPathFollow>(tank));
            float before = em.GetComponentData<LocalTransform>(tank).Position.x;
            Assert.Greater(SkirmishWorldMovementService.Step(em, session, 1f, false), 0);
            Assert.Greater(em.GetComponentData<LocalTransform>(tank).Position.x, before + 1f);

            em.AddComponentData(tank, new UnitPathFollow { PathIndex = 0 });
            float held = em.GetComponentData<LocalTransform>(tank).Position.x;
            Assert.Greater(SkirmishWorldMovementService.Step(em, session, 1f, false), 0);
            Assert.Greater(em.GetComponentData<LocalTransform>(tank).Position.x, held + 1f);
            Assert.IsFalse(em.HasComponent<UnitPathFollow>(tank));
        }

        [Test]
        public void StructureInRangeIsDamagedWhileACombatantIsAlsoInRange()
        {
            using var world = new World(nameof(StructureInRangeIsDamagedWhileACombatantIsAlsoInRange));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session);
            Entity barracks = DesignatedBase(em, 2);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity enemyCar = FirstFactionUnit(em, 2, SkirmishRoleKind.Car);
            Place(em, barracks, new float3(10f, 0f, 0f));
            Place(em, tank, new float3(0f, 0f, 0f));
            Place(em, enemyCar, new float3(8f, 0f, 0f));
            RevealAll(em);
            int carHealth = em.GetComponentData<UnitHealth>(enemyCar).Current;
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(
                em, session, GroupOf(em, tank), 1, SkirmishGroupOrderKind.Attack, barracks, out _));
            Assert.Greater(SkirmishExpandedEngagementService.Step(em, session, 1f, false), 0);
            Assert.AreEqual(772, em.GetComponentData<UnitHealth>(barracks).Current);
            Assert.AreEqual(carHealth, em.GetComponentData<UnitHealth>(enemyCar).Current);
            Assert.AreEqual(0, em.GetComponentData<SkirmishMoveIntentComponent>(tank).Engaged);
        }

        [Test]
        public void DeadlineDrawPublishesFinishedMatchWithoutStampingVictory()
        {
            using var world = new World(nameof(DeadlineDrawPublishesFinishedMatchWithoutStampingVictory));
            BootPlayingSession(world, out Entity session);
            EntityManager em = world.EntityManager;
            Assert.Greater(em.GetComponentData<SkirmishResolvedSetupComponent>(session).DeadlineSeconds, 0);
            var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            clock.DeadlineSeconds = 0;
            clock.ElapsedSeconds = 1080f;
            clock.Playing = 1;
            clock.Paused = 0;
            em.SetComponentData(session, clock);
            Assert.IsFalse(em.HasComponent<SkirmishResultComponent>(session));
            PublishProjectedMatch(world, session);

            Assert.AreEqual(1, em.GetComponentData<SkirmishResultComponent>(session).Frozen);
            clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            Assert.AreEqual(1080, clock.DeadlineSeconds);
            SkirmishObjectiveStateComponent objective = em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            Assert.AreEqual(1, objective.Terminal);
            Assert.AreEqual(SkirmishOutcomeKind.Draw, objective.Outcome);
            Assert.AreEqual(SkirmishEndReasonKind.TimeLimit, objective.Reason);
            Assert.AreEqual(SkirmishSessionPhase.Finished, em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase);
            SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Finished, match.Phase);
            Assert.AreEqual(SkirmishOutcome.Draw, match.Outcome);
            Assert.AreEqual(SkirmishEndReason.TimeLimit, match.Reason);
            CheckedInRunsCsvStaysHeaderOnly();
        }

        [Test]
        public void DeadEnemyBarracksFinishesVictoryWithoutRulesSystem()
        {
            using var world = new World(nameof(DeadEnemyBarracksFinishesVictoryWithoutRulesSystem));
            BootPlayingSession(world, out Entity session);
            EntityManager em = world.EntityManager;
            Entity enemyBase = DesignatedBase(em, 2);
            Entity playerBase = DesignatedBase(em, 1);
            var health = em.GetComponentData<UnitHealth>(enemyBase);
            health.Current = 0;
            em.SetComponentData(enemyBase, health);
            Assert.IsFalse(em.HasComponent<SkirmishResultComponent>(session));
            Assert.AreEqual(SkirmishPhase.Playing, em.GetComponentData<SkirmishMatchState>(session).Phase);

            PublishProjectedMatch(world, session);

            Assert.Greater(em.GetComponentData<UnitHealth>(playerBase).Current, 0);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, em.GetComponentData<SkirmishObjectiveStateComponent>(session).Outcome);
            Assert.AreEqual(SkirmishEndReasonKind.MainBaseDestroyed, em.GetComponentData<SkirmishObjectiveStateComponent>(session).Reason);
            SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Finished, match.Phase);
            Assert.AreEqual(SkirmishOutcome.Victory, match.Outcome);
            Assert.AreEqual(SkirmishEndReason.MainBaseDestroyed, match.Reason);
            CheckedInRunsCsvStaysHeaderOnly();
        }

        [Test]
        public void AssaultColumnDestroysEnemyBarracksAndPublishesVictory()
        {
            using var world = new World(nameof(AssaultColumnDestroysEnemyBarracksAndPublishesVictory));
            BootPlayingSession(world, out Entity session);
            EntityManager em = world.EntityManager;
            PlaceOnAuthoredPads(em, session);
            int materials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            int enemyMaterials = em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials;
            Entity playerBase = DesignatedBase(em, 1);
            Entity enemyBase = DesignatedBase(em, 2);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity rocketeer = FirstFactionUnit(em, 1, SkirmishRoleKind.Rocketeer);
            Entity car = FirstFactionUnit(em, 1, SkirmishRoleKind.Car);
            Entity apc = FirstApc(em, 1);
            Assert.Less(em.GetComponentData<LocalTransform>(tank).Position.x, -100f);
            Assert.Greater(em.GetComponentData<LocalTransform>(enemyBase).Position.x, 100f);
            RevealAll(em);

            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, tank), false, out _));
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, apc), true, out _));
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, car), true, out _));
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, rocketeer), true, out _));
            Assert.IsTrue(SkirmishArmyCommandService.TryAttack(em, session, enemyBase, out _));

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            for (int step = 0; step < 180; step++)
            {
                SkirmishEnemyStrategySystem.Evaluate(em, session, owned, authored.ArmyGround);
                SkirmishExpandedEngagementService.Step(em, session, 1f, false);
                SkirmishWorldMovementService.Step(em, session, 1f, false);
            }

            Assert.AreEqual(0, em.GetComponentData<UnitHealth>(enemyBase).Current);
            Assert.Greater(em.GetComponentData<UnitHealth>(playerBase).Current, 0);
            Assert.AreEqual(materials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(enemyMaterials - 120, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            Assert.AreNotEqual(
                SkirmishGroupOrderKind.Attack,
                FirstFactionGroup(em, session, 2, SkirmishRoleKind.Rifle).LastOrder);

            PublishProjectedMatch(world, session);
            SkirmishObjectiveStateComponent objective = em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, objective.Outcome);
            Assert.AreEqual(SkirmishEndReasonKind.MainBaseDestroyed, objective.Reason);
            SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Finished, match.Phase);
            Assert.AreEqual(SkirmishOutcome.Victory, match.Outcome);
            Assert.AreEqual(SkirmishEndReason.MainBaseDestroyed, match.Reason);
            CheckedInRunsCsvStaysHeaderOnly();
        }

        [Test]
        public void StructureColumnWaitsForAttackOrderBeforeLatchingAssault()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                EnemyDesignatedAlive = true,
                ExpandedNextPage = true,
                ExpandedPageIndex = 0,
                Squad4 = new AriaTouchTarget { Id = 200, Available = true },
                Attack = new AriaTouchTarget { Id = 42, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
            Assert.AreEqual(200, output.TargetId);
            Assert.AreEqual(0, plan.AssaultIssued);

            view.ExpandedPageIndex = 1;
            view.ExpandedStructureMask = (1 << 0) | (1 << 3);
            view.ExpandedAssaultMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3);
            view.Squad0 = new AriaTouchTarget { Id = 100, Available = true };
            view.Squad1 = new AriaTouchTarget { Id = 101, Available = true };
            view.Squad2 = new AriaTouchTarget { Id = 102, Available = true };
            view.Squad3 = new AriaTouchTarget { Id = 103, Available = true };
            for (int slot = 0; slot < 4; slot++)
            {
                AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
                Assert.AreEqual(0, plan.AssaultIssued);
                Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
                Assert.AreEqual(100 + slot, output.TargetId);
                view.ExpandedSelectedMask |= 1 << slot;
            }

            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Attack, plan.Intent);
            Assert.AreEqual(42, output.TargetId);
            Assert.AreEqual(0, plan.AssaultIssued, "Attack is requested only; the latch waits for the order.");

            view.Attack = new AriaTouchTarget { Id = 42, Available = false };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0, plan.AssaultIssued);
            Assert.AreEqual(AriaSkirmishIntent.Inspect, plan.Intent);
            Assert.AreEqual(AriaPlayObservationKind.Waiting, output.Kind);

            view.Attack = new AriaTouchTarget { Id = 42, Available = true };
            view.ExpandedAttackOrderMask = view.ExpandedAssaultMask;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0, plan.AssaultIssued);
            Assert.AreEqual(1, plan.StructureOrdered);
            Assert.AreEqual(200, output.TargetId);

            view.ExpandedPageIndex = 0;
            view.ExpandedAssaultMask = 0;
            view.ExpandedStructureMask = 0;
            view.ExpandedSelectedMask = 0;
            view.ExpandedAttackOrderMask = 0;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(1, plan.AssaultIssued);
            Assert.AreEqual(1, plan.StructureOrdered);
        }

        [Test]
        public void PresentedColumnDestroysEnemyBaseBeforeDeadlineOnLiveSeeds()
        {
            PresentedColumnDestroysEnemyBaseBeforeDeadline(130365);
            PresentedColumnDestroysEnemyBaseBeforeDeadline(155923);
            CheckedInRunsCsvStaysHeaderOnly();
        }

        [Test]
        public void SingleTimeScaleBlipKeepsNormalSpeedUntilFinish()
        {
            SkirmishS002NormalSpeedLatch latch = SkirmishS002NormalSpeedLatch.Start();
            latch = SkirmishS002AriaRunLog.ObserveTimeScale(latch, 0f);
            Assert.IsTrue(latch.Normal);
            Assert.AreEqual(1, latch.ConsecutiveOffSpeed);
            latch = SkirmishS002AriaRunLog.ObserveTimeScale(latch, 1f);
            Assert.IsTrue(latch.Normal);
            Assert.AreEqual(0, latch.ConsecutiveOffSpeed);

            latch = SkirmishS002NormalSpeedLatch.Start();
            for (int i = 0; i < SkirmishS002AriaRunLog.SustainedOffSpeedSamples - 1; i++)
                latch = SkirmishS002AriaRunLog.ObserveTimeScale(latch, 2f);
            Assert.IsTrue(latch.Normal);
            latch = SkirmishS002AriaRunLog.ObserveTimeScale(latch, 2f);
            Assert.IsFalse(latch.Normal);
            Assert.AreEqual(SkirmishS002AriaRunLog.SustainedOffSpeedSamples, latch.ConsecutiveOffSpeed);

            Assert.IsTrue(SkirmishS002AriaRunLog.FinishNormalSpeed(latch, 1f, 1080f, 1080d));
            Assert.IsFalse(SkirmishS002AriaRunLog.FinishNormalSpeed(latch, 1f, 1080f, 400d));
            Assert.IsFalse(SkirmishS002AriaRunLog.FinishNormalSpeed(latch, 0f, 1080f, 1080d));
            Assert.IsFalse(SkirmishS002AriaRunLog.FinishNormalSpeed(latch, 1f, 20f, 40d));
            var stillNormal = SkirmishS002NormalSpeedLatch.Start();
            stillNormal = SkirmishS002AriaRunLog.ObserveTimeScale(stillNormal, 0f);
            Assert.IsTrue(SkirmishS002AriaRunLog.FinishNormalSpeed(stillNormal, 1f, 20f, 40d));
        }

        private static void PresentedColumnDestroysEnemyBaseBeforeDeadline(int seed)
        {
            using var world = new World("PresentedColumn-" + seed);
            BootPlayingSession(world, out Entity session, seed);
            EntityManager em = world.EntityManager;
            PlaceOnAuthoredPads(em, session);
            RevealAll(em);
            int materials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            Entity playerBase = DesignatedBase(em, 1);
            Entity enemyBase = DesignatedBase(em, 2);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Assert.AreNotEqual(Entity.Null, tank, "seed " + seed);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            int steps = 0;
            while (steps < 1080 && em.GetComponentData<UnitHealth>(enemyBase).Current > 0)
            {
                var output = new AriaPlayObservationComponent();
                AriaSkirmishObservation view = PresentedObservation(em, session, playerBase, enemyBase);
                AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
                ApplyPresentedControl(em, session, output);
                SkirmishFogService.Project(em, session);
                SkirmishEnemyStrategySystem.Evaluate(em, session, owned, authored.ArmyGround);
                SkirmishExpandedEngagementService.Step(em, session, 1f, false);
                SkirmishWorldMovementService.Step(em, session, 1f, false);
                steps++;
            }

            int enemyHp = em.GetComponentData<UnitHealth>(enemyBase).Current;
            int playerHp = em.GetComponentData<UnitHealth>(playerBase).Current;
            Assert.Less(steps, 1080, "seed " + seed + " enemyHp=" + enemyHp + " playerHp=" + playerHp);
            Assert.AreEqual(0, enemyHp, "seed " + seed);
            Assert.Greater(playerHp, 0, "seed " + seed);
            Assert.AreEqual(materials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials, "seed " + seed);
            Assert.AreEqual(1, plan.AssaultIssued, "seed " + seed);
            Assert.AreEqual(
                SkirmishGroupOrderKind.Attack,
                FirstFactionGroup(em, session, 1, SkirmishRoleKind.Tank).LastOrder,
                "seed " + seed);
            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {
                SkirmishObjectiveClockComponent clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
                clock.ElapsedSeconds = steps;
                clock.Playing = 1;
                clock.Paused = 0;
                em.SetComponentData(session, clock);
            }

            PublishProjectedMatch(world, session);
            SkirmishObjectiveStateComponent objective = em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, objective.Outcome, "seed " + seed);
            Assert.AreEqual(SkirmishEndReasonKind.MainBaseDestroyed, objective.Reason, "seed " + seed);
            SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Finished, match.Phase, "seed " + seed);
            Assert.AreEqual(SkirmishOutcome.Victory, match.Outcome, "seed " + seed);
            Assert.AreEqual(SkirmishEndReason.MainBaseDestroyed, match.Reason, "seed " + seed);
            Assert.AreNotEqual(SkirmishOutcome.Draw, match.Outcome, "seed " + seed);
        }

        private static AriaSkirmishObservation PresentedObservation(
            EntityManager em,
            Entity session,
            Entity playerBase,
            Entity enemyBase)
        {
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryReadPage(
                em,
                session,
                out int pageIndex,
                out bool nextPage,
                out SkirmishPresentedSlot slot0,
                out SkirmishPresentedSlot slot1,
                out SkirmishPresentedSlot slot2,
                out SkirmishPresentedSlot slot3));
            var slots = new[] { slot0, slot1, slot2, slot3 };
            int assault = 0;
            int selected = 0;
            int structure = 0;
            int ordered = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].Occupied)
                    continue;
                if (slots[i].Assault)
                    assault |= 1 << i;
                if (slots[i].Selected)
                    selected |= 1 << i;
                if (slots[i].Structure)
                    structure |= 1 << i;
                if (slots[i].AttackOrdered)
                    ordered |= 1 << i;
            }

            return new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                EnemyDesignatedAlive = em.GetComponentData<UnitHealth>(enemyBase).Current > 0,
                PlayerDesignatedAlive = em.GetComponentData<UnitHealth>(playerBase).Current > 0,
                Infantry = 20,
                ExpandedPageIndex = pageIndex,
                ExpandedNextPage = nextPage,
                ExpandedAssaultMask = assault,
                ExpandedSelectedMask = selected,
                ExpandedStructureMask = structure,
                ExpandedAttackOrderMask = ordered,
                Squad0 = PresentedCard(slot0, 100),
                Squad1 = PresentedCard(slot1, 101),
                Squad2 = PresentedCard(slot2, 102),
                Squad3 = PresentedCard(slot3, 103),
                Squad4 = new AriaTouchTarget { Id = 200, Available = nextPage },
                Attack = new AriaTouchTarget { Id = 42, Available = true }
            };
        }

        private static AriaTouchTarget PresentedCard(SkirmishPresentedSlot slot, int id)
        {
            return new AriaTouchTarget { Id = id, Available = slot.Occupied };
        }

        private static void ApplyPresentedControl(EntityManager em, Entity session, AriaPlayObservationComponent output)
        {
            if (output.Kind != AriaPlayObservationKind.Control)
                return;
            if (output.TargetId == 200)
                SkirmishExpandedPresentedOrders.TryAdvancePage(em, session);
            else if (output.TargetId >= 100 && output.TargetId <= 103)
                SkirmishExpandedPresentedOrders.TryPresentedSlot(em, session, output.TargetId - 100);
            else if (output.TargetId == 42)
                SkirmishExpandedPresentedOrders.TryAttackEnemyBase(em, session);
        }

        [Test]
        public void PayloadLoggersDoNotStampVictory()
        {
            Assert.IsTrue(SkirmishAriaAcceptancePayload.TryCreateFirstVisitS002(
                104731,
                GameLocalization.EnglishLocaleCode,
                out SkirmishAriaAcceptancePayload payload,
                out string error),
                error);
            string play = AriaPlayEditorValidation.AcceptExpandedPayload(in payload);
            string watch = SkirmishExpandedAriaWatchValidation.AcceptExpandedPayload(
                SkirmishAcceptanceCensusCapture.DefinitionId,
                SkirmishSizeId.Standard,
                SkirmishDifficultyId.Regular,
                155923,
                GameLocalization.PersianLocaleCode);
            Assert.IsTrue(play.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(watch.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(play.IndexOf("seed=104731", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(watch.IndexOf("seed=155923", StringComparison.Ordinal) >= 0);
        }

        [Test]
        public void SimulationStallFailsFastAfterGrace()
        {
            Assert.IsFalse(SkirmishS002AriaRunLog.IsSimulationNotAdvancing(
                playing: true,
                simulationActive: false,
                matchElapsedSeconds: 0f,
                wallSecondsSincePlaying: 10d));
            Assert.IsTrue(SkirmishS002AriaRunLog.IsSimulationNotAdvancing(
                playing: true,
                simulationActive: false,
                matchElapsedSeconds: 0f,
                wallSecondsSincePlaying: SkirmishS002AriaRunLog.SimulationStallGraceSeconds));
            Assert.IsTrue(SkirmishS002AriaRunLog.IsSimulationNotAdvancing(
                playing: true,
                simulationActive: true,
                matchElapsedSeconds: 0f,
                wallSecondsSincePlaying: SkirmishS002AriaRunLog.SimulationStallGraceSeconds + 1d));
            Assert.IsFalse(SkirmishS002AriaRunLog.IsSimulationNotAdvancing(
                playing: true,
                simulationActive: true,
                matchElapsedSeconds: 0.5f,
                wallSecondsSincePlaying: 120d));
            Assert.AreEqual(
                SkirmishS002AriaRunLog.AbortReasonSimulationNotAdvancing,
                "simulationNotAdvancing");
        }

        [Test]
        public void ExpandedObjectiveClockProjectsOntoMatchElapsed()
        {
            using var world = new World(nameof(ExpandedObjectiveClockProjectsOntoMatchElapsed));
            EntityManager em = world.EntityManager;
            Entity session = em.CreateEntity();
            em.AddComponentData(session, new SkirmishExpandedSessionComponent
            {
                SessionId = "elapsed-s002",
                CatalogId = "S002",
                IsLegacy = 0,
                Phase = SkirmishSessionPhase.Playing
            });
            em.AddComponentData(session, new SkirmishMatchState
            {
                SessionId = "elapsed-s002",
                Phase = SkirmishPhase.Preparing,
                ElapsedSeconds = 0f
            });
            em.AddComponentData(session, new SkirmishObjectiveClockComponent
            {
                ElapsedSeconds = 37.5f,
                DeadlineSeconds = 1080,
                Paused = 0,
                Playing = 1
            });

            SkirmishExpandedSessionControlService.ProjectMatchPhase(em, session);

            var match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Playing, match.Phase);
            Assert.AreEqual(37.5f, match.ElapsedSeconds, 0.001f);
            Assert.AreEqual(
                37.5f,
                SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match),
                0.001f);
        }

        [Test]
        public void RosterProjectionOnUpdateAppliesStructureDamageWithoutIterating()
        {
            using var world = new World(nameof(RosterProjectionOnUpdateAppliesStructureDamageWithoutIterating));
            BootPlayingSession(world, out Entity session);
            EntityManager em = world.EntityManager;
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity barracks = DesignatedBase(em, 2);
            Assert.AreNotEqual(Entity.Null, tank);
            em.RemoveComponent<SkirmishRoleOverlayComponent>(tank);
            em.RemoveComponent<UnitHealth>(tank);
            em.RemoveComponent<SkirmishRoleOverlayComponent>(barracks);
            em.RemoveComponent<UnitHealth>(barracks);

            world.GetOrCreateSystem<SkirmishRosterProjectionSystem>().Update(world.Unmanaged);

            SkirmishRoleOverlayComponent overlay = em.GetComponentData<SkirmishRoleOverlayComponent>(tank);
            Assert.AreEqual(1, overlay.Applied);
            Assert.Greater(overlay.Damage, 0);
            Assert.Greater(overlay.RangeWorld, 0f);
            Assert.IsTrue((overlay.TargetDomains & SkirmishTargetDomain.Structure) != 0);
            Assert.Greater(em.GetComponentData<UnitHealth>(tank).Max, 0);
            Assert.Greater(em.GetComponentData<UnitHealth>(barracks).Max, 0);
            Assert.AreEqual(SkirmishPhase.Playing, em.GetComponentData<SkirmishMatchState>(session).Phase);
        }

        [Test]
        public void FinishedCleanupDestroysOwnedUnitsOutsideTheSessionQuery()
        {
            using var world = new World(nameof(FinishedCleanupDestroysOwnedUnitsOutsideTheSessionQuery));
            BootPlayingSession(world, out Entity session);
            EntityManager em = world.EntityManager;
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            SkirmishExpandedSessionComponent expanded = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            expanded.Phase = SkirmishSessionPhase.Finished;
            em.SetComponentData(session, expanded);

            world.GetOrCreateSystem<SkirmishSessionCleanupSystem>().Update(world.Unmanaged);

            Assert.IsFalse(em.Exists(tank));
            Assert.AreEqual(
                SkirmishSessionPhase.Cleaning,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase);
            CheckedInRunsCsvStaysHeaderOnly();
        }

        [Test]
        public void AriaSessionOrdersStructureColumnAndDestroysEnemyBase()
        {
            using var world = new World(nameof(AriaSessionOrdersStructureColumnAndDestroysEnemyBase));
            BootPlayingSession(world, out Entity session, 130365);
            EntityManager em = world.EntityManager;
            PlaceOnAuthoredPads(em, session);
            RevealAll(em);
            int materials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            Entity playerBase = DesignatedBase(em, 1);
            Entity enemyBase = DesignatedBase(em, 2);
            Entity shell = em.CreateEntity();
            em.AddComponentData(shell, new AriaPlaySessionComponent { Phase = AriaPlayPhase.Manual });
            world.GetOrCreateSystem<AriaSkirmishStructureOrderSystem>().Update(world.Unmanaged);
            Assert.AreNotEqual(
                SkirmishGroupOrderKind.Attack,
                FirstFactionGroup(em, session, 1, SkirmishRoleKind.Tank).LastOrder);

            em.SetComponentData(shell, new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing });
            world.GetOrCreateSystem<AriaSkirmishStructureOrderSystem>().Update(world.Unmanaged);
            Assert.AreEqual(
                SkirmishGroupOrderKind.Attack,
                FirstFactionGroup(em, session, 1, SkirmishRoleKind.Tank).LastOrder);
            Assert.AreEqual(
                SkirmishGroupOrderKind.Attack,
                FirstFactionGroup(em, session, 1, SkirmishRoleKind.Rocketeer).LastOrder);
            Assert.AreEqual(materials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            int steps = 0;
            while (steps < 1080 && em.GetComponentData<UnitHealth>(enemyBase).Current > 0)
            {
                world.GetOrCreateSystem<AriaSkirmishStructureOrderSystem>().Update(world.Unmanaged);
                SkirmishEnemyStrategySystem.Evaluate(em, session, owned, authored.ArmyGround);
                SkirmishExpandedEngagementService.Step(em, session, 1f, false);
                SkirmishWorldMovementService.Step(em, session, 1f, false);
                steps++;
            }

            Assert.Less(steps, 1080, "enemyHp=" + em.GetComponentData<UnitHealth>(enemyBase).Current);
            Assert.AreEqual(0, em.GetComponentData<UnitHealth>(enemyBase).Current);
            Assert.Greater(em.GetComponentData<UnitHealth>(playerBase).Current, 0);
            Assert.AreEqual(materials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            PublishProjectedMatch(world, session);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, em.GetComponentData<SkirmishObjectiveStateComponent>(session).Outcome);
            Assert.AreEqual(SkirmishEndReasonKind.MainBaseDestroyed, em.GetComponentData<SkirmishObjectiveStateComponent>(session).Reason);
            SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Finished, match.Phase);
            Assert.AreEqual(SkirmishOutcome.Victory, match.Outcome);
            Assert.AreEqual(SkirmishEndReason.MainBaseDestroyed, match.Reason);
            CheckedInRunsCsvStaysHeaderOnly();
        }

        [Test]
        public void MissingBarracksPrefabStillAnchorsTheMeasuredBaseAndAriaDestroysIt()
        {
            using var world = new World(nameof(MissingBarracksPrefabStillAnchorsTheMeasuredBaseAndAriaDestroysIt));
            BootPlayingSession(world, out Entity session, 130365);
            EntityManager em = world.EntityManager;
            Entity playerBase = DesignatedBase(em, 1);
            Entity enemyBase = DesignatedBase(em, 2);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity rocketeer = FirstFactionUnit(em, 1, SkirmishRoleKind.Rocketeer);
            Assert.IsFalse(em.HasComponent<LocalTransform>(enemyBase));
            Assert.IsFalse(em.HasComponent<LocalTransform>(playerBase));
            Assert.IsFalse(em.HasComponent<LocalTransform>(rocketeer));

            GameObject tankPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tankPrefab.name = "Unit_Veh_Tank_USA";
            tankPrefab.hideFlags = HideFlags.HideAndDontSave;
            tankPrefab.SetActive(false);
            var catalog = new SkirmishVisualPrefabCatalog();
            catalog.Bind("Unit_Veh_Tank_USA", tankPrefab, true);
            try
            {
                SkirmishVisualSpawnService.BindCatalog(em, session, catalog);
                Assert.IsFalse(catalog.Contains("Building_Barrack"));
                world.GetOrCreateSystem<SkirmishVisualSpawnSystem>().Update(world.Unmanaged);

                Assert.IsTrue(em.HasComponent<LocalTransform>(enemyBase));
                Assert.IsTrue(em.HasComponent<LocalTransform>(playerBase));
                Assert.IsTrue(em.HasComponent<LocalTransform>(rocketeer));
                Assert.IsFalse(VisuallySpawned(em, enemyBase));
                Assert.IsFalse(VisuallySpawned(em, rocketeer));
                Assert.IsTrue(VisuallySpawned(em, tank));
                SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                Assert.IsTrue(TryStructureSpawn(setup, 2, true, out float enemyX, out float enemyZ));
                float3 enemyPos = em.GetComponentData<LocalTransform>(enemyBase).Position;
                Assert.AreEqual(enemyX, enemyPos.x, 0.05f);
                Assert.AreEqual(enemyZ, enemyPos.z, 0.05f);
                Assert.IsTrue(TryStructureSpawn(setup, 1, true, out float playerX, out float playerZ));
                float3 playerPos = em.GetComponentData<LocalTransform>(playerBase).Position;
                Assert.AreEqual(playerX, playerPos.x, 0.05f);
                Assert.AreEqual(playerZ, playerPos.z, 0.05f);
                float3 standIn = SkirmishWorldMovementService.DefaultAdvance(em, session);
                Assert.Greater(math.distance(enemyPos, standIn), 100f);
                Assert.Greater(math.distance(em.GetComponentData<LocalTransform>(tank).Position, float3.zero), 50f);

                int materials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
                Entity shell = em.CreateEntity();
                em.AddComponentData(shell, new AriaPlaySessionComponent { Phase = AriaPlayPhase.Manual });
                world.GetOrCreateSystem<AriaSkirmishStructureOrderSystem>().Update(world.Unmanaged);
                Assert.AreNotEqual(
                    SkirmishGroupOrderKind.Attack,
                    FirstFactionGroup(em, session, 1, SkirmishRoleKind.Tank).LastOrder);

                em.SetComponentData(shell, new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing });
                world.GetOrCreateSystem<AriaSkirmishStructureOrderSystem>().Update(world.Unmanaged);
                Assert.AreEqual(
                    SkirmishGroupOrderKind.Attack,
                    FirstFactionGroup(em, session, 1, SkirmishRoleKind.Tank).LastOrder);
                Assert.AreEqual(
                    SkirmishGroupOrderKind.Attack,
                    FirstFactionGroup(em, session, 1, SkirmishRoleKind.Rocketeer).LastOrder);
                SkirmishMoveIntentComponent ordered = em.GetComponentData<SkirmishMoveIntentComponent>(tank);
                Assert.AreEqual(enemyPos.x, ordered.DestinationX, 0.05f);
                Assert.AreEqual(enemyPos.z, ordered.DestinationZ, 0.05f);
                Assert.AreEqual(enemyBase, ordered.AttackTarget);
                Assert.AreEqual(materials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

                ordered.DestinationX = standIn.x;
                ordered.DestinationZ = standIn.z;
                ordered.Active = 0;
                em.SetComponentData(tank, ordered);
                Assert.Greater(SkirmishWorldMovementService.Step(em, session, 1f, false), 0);
                ordered = em.GetComponentData<SkirmishMoveIntentComponent>(tank);
                Assert.AreEqual(enemyPos.x, ordered.DestinationX, 0.05f);
                Assert.AreEqual(enemyPos.z, ordered.DestinationZ, 0.05f);
                Assert.AreEqual(1, ordered.Active);

                SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
                using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
                int steps = 0;
                while (steps < 1080 && em.GetComponentData<UnitHealth>(enemyBase).Current > 0)
                {
                    world.GetOrCreateSystem<AriaSkirmishStructureOrderSystem>().Update(world.Unmanaged);
                    SkirmishEnemyStrategySystem.Evaluate(em, session, owned, authored.ArmyGround);
                    SkirmishVisualSpawnService.EnsureMissingCombatTransforms(em, session);
                    SkirmishExpandedEngagementService.Step(em, session, 1f, false);
                    SkirmishWorldMovementService.Step(em, session, 1f, false);
                    steps++;
                }

                Assert.Less(steps, 1080, "enemyHp=" + em.GetComponentData<UnitHealth>(enemyBase).Current);
                Assert.AreEqual(0, em.GetComponentData<UnitHealth>(enemyBase).Current);
                Assert.Greater(em.GetComponentData<UnitHealth>(playerBase).Current, 0);
                Assert.AreEqual(materials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
                PublishProjectedMatch(world, session);
                Assert.AreEqual(SkirmishOutcomeKind.Victory, em.GetComponentData<SkirmishObjectiveStateComponent>(session).Outcome);
                Assert.AreEqual(SkirmishEndReasonKind.MainBaseDestroyed, em.GetComponentData<SkirmishObjectiveStateComponent>(session).Reason);
                SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
                Assert.AreEqual(SkirmishPhase.Finished, match.Phase);
                Assert.AreEqual(SkirmishOutcome.Victory, match.Outcome);
                Assert.AreEqual(SkirmishEndReason.MainBaseDestroyed, match.Reason);
                CheckedInRunsCsvStaysHeaderOnly();
            }
            finally
            {
                if (em.Exists(session))
                {
                    SkirmishScenarioSpawnSystem.DestroyAttemptOwned(
                        em, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
                }

                catalog.Dispose();
            }
        }

        [Test]
        public void LaunchResetsLiveTraceSoVictoryEvidenceOmitsPriorDraws()
        {
            string directory = Path.Combine(Path.GetTempPath(), "s002-aria-trace-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string live = Path.Combine(directory, "s002-aria-live-130365-en.jsonl");
                File.WriteAllText(
                    live,
                    "{\"elapsed\":1080.03,\"phase\":\"Finished\",\"outcome\":\"Draw\",\"reason\":\"TimeLimit\",\"playerBaseHp\":800,\"enemyBaseHp\":800}\n" +
                    "{\"elapsed\":12,\"phase\":\"Finished\",\"outcome\":\"Abort\",\"reason\":\"timeout\"}\n");

                SkirmishS002AriaRunHarness.ResetLiveTrace(live);
                Assert.AreEqual(string.Empty, File.ReadAllText(live));

                const string playing =
                    "{\"elapsed\":2,\"phase\":\"Playing\",\"outcome\":\"None\",\"reason\":\"None\",\"playerBaseHp\":800,\"enemyBaseHp\":760}\n";
                const string victory =
                    "{\"elapsed\":90,\"phase\":\"Finished\",\"outcome\":\"Victory\",\"reason\":\"MainBaseDestroyed\",\"playerBaseHp\":800,\"enemyBaseHp\":0}\n";
                File.AppendAllText(live, playing);
                File.AppendAllText(live, victory);

                string evidence = Path.Combine(directory, "s002-aria-rs-130365-en-1.jsonl");
                SkirmishS002AriaRunHarness.CopyLiveTraceToEvidence(live, evidence);
                string copied = File.ReadAllText(evidence);
                Assert.AreEqual(playing + victory, copied);
                Assert.AreEqual(2, File.ReadAllLines(evidence).Length);
                Assert.IsTrue(copied.IndexOf("\"phase\":\"Playing\"", StringComparison.Ordinal) >= 0);
                Assert.IsTrue(copied.IndexOf("\"outcome\":\"Victory\"", StringComparison.Ordinal) >= 0);
                Assert.IsTrue(copied.IndexOf("\"reason\":\"MainBaseDestroyed\"", StringComparison.Ordinal) >= 0);
                Assert.IsTrue(copied.IndexOf("\"outcome\":\"Draw\"", StringComparison.Ordinal) < 0);
                Assert.IsTrue(copied.IndexOf("\"outcome\":\"Abort\"", StringComparison.Ordinal) < 0);
                Assert.IsTrue(copied.IndexOf("TimeLimit", StringComparison.Ordinal) < 0);
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishS002AriaHarnessTests();
                suite.CheckedInRunsCsvStaysHeaderOnly();
                suite.ForcedVictoryDoesNotAppend();
                suite.UnfinishedOutcomeIsRefusedUntilTheMatchEnds();
                suite.FinishedOutcomesAppendAndOnlyUnguidedVictoryCounts();
                suite.AttackOrdersDamageOnlyLegalVisibleTargets();
                suite.AssaultColumnKillsTheEnemyTankAndLeavesThePlayerBarracks();
                suite.ExpandedAssaultPlanUsesVisibleCardsWithoutMutatingStocks();
                suite.ExpandedAssaultDoesNotAttackWithRiflesWhileNextPageExists();
                suite.ExpandedAssaultStartsImmediatelyAndDoesNotBlockOnFlatHealth();
                suite.BlockedExpandedPlanRestartsTheTouchDriver();
                suite.PendingPathRequestDoesNotFreezeLocalStep();
                suite.StructureInRangeIsDamagedWhileACombatantIsAlsoInRange();
                suite.DeadlineDrawPublishesFinishedMatchWithoutStampingVictory();
                suite.DeadEnemyBarracksFinishesVictoryWithoutRulesSystem();
                suite.AssaultColumnDestroysEnemyBarracksAndPublishesVictory();
                suite.StructureColumnWaitsForAttackOrderBeforeLatchingAssault();
                suite.PresentedColumnDestroysEnemyBaseBeforeDeadlineOnLiveSeeds();
                suite.SingleTimeScaleBlipKeepsNormalSpeedUntilFinish();
                suite.PayloadLoggersDoNotStampVictory();
                suite.SimulationStallFailsFastAfterGrace();
                suite.ExpandedObjectiveClockProjectsOntoMatchElapsed();
                suite.RosterProjectionOnUpdateAppliesStructureDamageWithoutIterating();
                suite.FinishedCleanupDestroysOwnedUnitsOutsideTheSessionQuery();
                suite.AriaSessionOrdersStructureColumnAndDestroysEnemyBase();
                suite.MissingBarracksPrefabStillAnchorsTheMeasuredBaseAndAriaDestroysIt();
                suite.LaunchResetsLiveTraceSoVictoryEvidenceOmitsPriorDraws();
                Debug.Log("[SkirmishS002AriaHarnessTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishS002AriaHarnessTests] result=Failed\n" + exception);
                throw;
            }
        }

        private static void AppendFinished(
            string path,
            string runId,
            string outcome,
            bool normalSpeed,
            int violations,
            int interventions,
            bool expectCounted)
        {
            var facts = Finished(outcome);
            facts.RunId = runId;
            facts.NormalSpeed = normalSpeed;
            facts.InputViolations = violations;
            facts.HumanInterventions = interventions;
            Assert.IsTrue(SkirmishS002AriaRunLog.TryAppendTerminalRow(path, in facts, out string row, out string error), error);
            Assert.AreEqual(outcome, Field(row, 13));
            Assert.AreEqual(expectCounted, SkirmishS002AriaRunLog.IsCountedAriaWin(in facts, outcome));
        }

        private static SkirmishS002AriaTerminalFacts Finished(string outcome)
        {
            return new SkirmishS002AriaTerminalFacts
            {
                RunId = "S002-aria-rs-104731-en-1",
                DefinitionVersion = 1,
                CodeHash = "abc",
                ConfigHash = "def",
                Seed = 104731,
                Locale = GameLocalization.EnglishLocaleCode,
                Device = "Editor",
                NormalSpeed = true,
                StartedAtUtc = "2026-09-22T00:00:00Z",
                MatchFinished = true,
                MatchOutcome = outcome,
                EndReason = "MainBaseDestroyed",
                DurationSeconds = 90f,
                TracePath = "Design/AgentReports/SkirmishExpansion/S002/_Evidence/sample.jsonl",
                LogPath = "Design/AgentReports/SkirmishExpansion/S002/_Evidence/sample-log.txt",
                ForcedVictory = false
            };
        }

        private static string Field(string row, int index)
        {
            string[] fields = row.Split(',');
            Assert.Greater(fields.Length, index);
            return fields[index];
        }

        private static string TempCsv()
        {
            return Path.Combine(Path.GetTempPath(), "s002-aria-harness-" + Guid.NewGuid().ToString("N") + ".csv");
        }

        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static void BootPlayingSession(World world, out Entity session, int seed = 104731)
        {
            EntityManager em = world.EntityManager;
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(ProjectRoot(), out var matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS002.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                "S002",
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                seed,
                authored,
                matrix,
                manifest,
                out _,
                out _,
                out var reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            world.GetOrCreateSystem<SkirmishSessionInitializationSystem>().Update(world.Unmanaged);
            world.GetOrCreateSystem<SkirmishScenarioSpawnSystem>().Update(world.Unmanaged);
            Assert.AreEqual(SkirmishSessionPhase.Playing, em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            SkirmishRosterProjectionSystem.Apply(em, owned, sessionId, setup);
            SkirmishArmyGroupSystem.RefreshAlive(em, session, owned, sessionId);
            SkirmishExpandedSessionControlService.ProjectMatchPhase(em, session);
            Assert.AreEqual(SkirmishPhase.Playing, em.GetComponentData<SkirmishMatchState>(session).Phase);
        }

        private static void PublishProjectedMatch(World world, Entity session)
        {
            world.GetOrCreateSystem<SkirmishObjectiveFactProjectionSystem>().Update(world.Unmanaged);
            world.GetOrCreateSystem<SkirmishBaseAssaultObjectiveSystem>().Update(world.Unmanaged);
            world.GetOrCreateSystem<SkirmishOutcomeSystem>().Update(world.Unmanaged);
            world.GetOrCreateSystem<SkirmishExpandedSessionControlSystem>().Update(world.Unmanaged);
        }

        private static void PlaceOnAuthoredPads(EntityManager em, Entity session)
        {
            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            using var units = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using NativeArray<Entity> entities = units.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                byte faction = em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId;
                SkirmishRoleKind role = em.GetComponentData<SkirmishUnitRoleComponent>(entities[i]).Role;
                Assert.IsTrue(TryForceSpawn(setup, faction, role, out float x, out float z), role + " faction " + faction);
                Place(em, entities[i], new float3(x, 0f, z));
            }

            using var structures = em.CreateEntityQuery(
                typeof(SkirmishStructureIdentityComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> pads = structures.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < pads.Length; i++)
            {
                byte faction = em.GetComponentData<SkirmishAttemptOwnedComponent>(pads[i]).FactionId;
                bool designated = em.GetComponentData<SkirmishStructureIdentityComponent>(pads[i]).DesignatedBase != 0;
                Assert.IsTrue(TryStructureSpawn(setup, faction, designated, out float x, out float z));
                Place(em, pads[i], new float3(x, 0f, z));
            }
        }

        private static bool TryForceSpawn(SkirmishResolvedSetup setup, byte faction, SkirmishRoleKind role, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            if (setup?.Forces == null)
                return false;
            for (int i = 0; i < setup.Forces.Length; i++)
            {
                if (setup.Forces[i].FactionId != faction || setup.Forces[i].RoleKind != role)
                    continue;
                x = setup.Forces[i].SpawnWorldX;
                z = setup.Forces[i].SpawnWorldZ;
                return true;
            }

            return false;
        }

        private static bool TryStructureSpawn(SkirmishResolvedSetup setup, byte faction, bool designated, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            if (setup?.Structures == null)
                return false;
            for (int i = 0; i < setup.Structures.Length; i++)
            {
                if (setup.Structures[i].FactionId != faction || setup.Structures[i].DesignatedBase != designated)
                    continue;
                x = setup.Structures[i].SpawnWorldX;
                z = setup.Structures[i].SpawnWorldZ;
                return true;
            }

            return false;
        }

        private static void CompileAndSpawn(EntityManager em, out Entity session)
        {
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(ProjectRoot(), out var matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS002.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                "S002",
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                104731,
                authored,
                matrix,
                manifest,
                out SkirmishResolvedSetup setup,
                out _,
                out var reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(
                em, session, setup, out SkirmishReasonCode reason, out byte visualPending), reason.ToString());
            Assert.AreEqual(0, visualPending);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            SkirmishRosterProjectionSystem.Apply(em, owned, sessionId, setup);
            SkirmishArmyGroupSystem.RefreshAlive(em, session, owned, sessionId);
        }

        private static Entity DesignatedBase(EntityManager em, byte faction)
        {
            using var query = em.CreateEntityQuery(
                typeof(SkirmishStructureIdentityComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishStructureIdentityComponent>(entities[i]).DesignatedBase == 0)
                    continue;
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId == faction)
                    return entities[i];
            }

            Assert.Fail("Missing designated base for faction " + faction);
            return Entity.Null;
        }

        private static Entity FirstApc(EntityManager em, byte faction)
        {
            Entity fast = FirstFactionUnit(em, faction, SkirmishRoleKind.ApcFast);
            return fast != Entity.Null ? fast : FirstFactionUnit(em, faction, SkirmishRoleKind.ApcArmored);
        }

        private static Entity FirstFactionUnit(EntityManager em, byte faction, SkirmishRoleKind role)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId == faction &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(entities[i]).Role == role)
                    return entities[i];
            }

            return Entity.Null;
        }

        private static SkirmishArmyGroupRecord FirstFactionGroup(
            EntityManager em,
            Entity session,
            byte faction,
            SkirmishRoleKind role)
        {
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].FactionId == faction && buffer[i].Role == role)
                    return buffer[i];
            }

            return default;
        }

        private static uint GroupOf(EntityManager em, Entity unit)
        {
            Assert.AreNotEqual(Entity.Null, unit);
            return em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).GroupId;
        }

        private static Entity FirstGroupMember(EntityManager em, uint groupId)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishArmyGroupMembershipComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishArmyGroupMembershipComponent>(entities[i]).GroupId == groupId)
                    return entities[i];
            }

            return Entity.Null;
        }

        private static void Place(EntityManager em, Entity unit, float3 position)
        {
            Assert.AreNotEqual(Entity.Null, unit);
            var transform = LocalTransform.FromPosition(position);
            if (em.HasComponent<LocalTransform>(unit))
                em.SetComponentData(unit, transform);
            else
                em.AddComponentData(unit, transform);
        }

        private static bool VisuallySpawned(EntityManager em, Entity entity)
        {
            return em.HasComponent<SkirmishVisualSpawnedComponent>(entity) &&
                   em.GetComponentData<SkirmishVisualSpawnedComponent>(entity).Spawned != 0;
        }

        private static void RevealAll(EntityManager em)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
                SkirmishFogService.Reveal(em, entities[i]);
        }
    }
}
