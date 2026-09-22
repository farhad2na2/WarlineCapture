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
            SkirmishWorldMovementService.Step(em, session, 1f, false);
            Assert.AreEqual(held, em.GetComponentData<LocalTransform>(tank).Position.x, 0.001f);
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
            PublishProjectedMatch(world, session);

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
                suite.BlockedExpandedPlanRestartsTheTouchDriver();
                suite.PendingPathRequestDoesNotFreezeLocalStep();
                suite.StructureInRangeIsDamagedWhileACombatantIsAlsoInRange();
                suite.DeadlineDrawPublishesFinishedMatchWithoutStampingVictory();
                suite.AssaultColumnDestroysEnemyBarracksAndPublishesVictory();
                suite.PayloadLoggersDoNotStampVictory();
                suite.SimulationStallFailsFastAfterGrace();
                suite.ExpandedObjectiveClockProjectsOntoMatchElapsed();
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

        private static void BootPlayingSession(World world, out Entity session)
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
                104731,
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
        }

        private static void PublishProjectedMatch(World world, Entity session)
        {
            EntityManager em = world.EntityManager;
            if (!em.HasComponent<SkirmishBaseAssaultFactComponent>(session))
            {
                em.AddComponentData(session, new SkirmishBaseAssaultFactComponent
                {
                    PlayerDesignatedAlive = 1,
                    EnemyDesignatedAlive = 1
                });
            }

            if (!em.HasComponent<SkirmishResultComponent>(session))
                em.AddComponentData(session, new SkirmishResultComponent());

            world.GetOrCreateSystem<SkirmishObjectiveFactProjectionSystem>().Update(world.Unmanaged);
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

        private static void RevealAll(EntityManager em)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
                SkirmishFogService.Reveal(em, entities[i]);
        }
    }
}
