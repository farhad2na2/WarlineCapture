using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishExpandedObjectiveTests
    {
        [Test]
        public void ReplacementBarracksAndArmyWipeAreNonTerminal()
        {
            var facts = new SkirmishBaseAssaultFacts
            {
                PlayerDesignatedAlive = true,
                EnemyDesignatedAlive = true,
                ReplacementBarracksPresent = true,
                FieldArmyWiped = true,
                Playing = true,
                Paused = false,
                ElapsedSeconds = 10f,
                DeadlineSeconds = 1080
            };
            Assert.IsFalse(SkirmishBaseAssaultObjectiveSystem.TryEvaluate(in facts, out _, out _));
        }

        [Test]
        public void PausedDeadlineDoesNotFreeze()
        {
            var facts = new SkirmishBaseAssaultFacts
            {
                PlayerDesignatedAlive = true,
                EnemyDesignatedAlive = true,
                Playing = true,
                Paused = true,
                ElapsedSeconds = 1080f,
                DeadlineSeconds = 1080
            };
            Assert.IsFalse(SkirmishBaseAssaultObjectiveSystem.TryEvaluate(in facts, out _, out _));
        }

        [Test]
        public void BothDesignatedDeadIsDrawRegardlessOfReplacement()
        {
            var facts = new SkirmishBaseAssaultFacts
            {
                PlayerDesignatedAlive = false,
                EnemyDesignatedAlive = false,
                ReplacementBarracksPresent = true,
                Playing = true,
                ElapsedSeconds = 12f,
                DeadlineSeconds = 1080
            };
            Assert.IsTrue(SkirmishBaseAssaultObjectiveSystem.TryEvaluate(
                in facts, out SkirmishOutcomeKind outcome, out SkirmishEndReasonKind reason));
            Assert.AreEqual(SkirmishOutcomeKind.Draw, outcome);
            Assert.AreEqual(SkirmishEndReasonKind.BothBasesDestroyed, reason);
        }

        [Test]
        public void SurrenderRequiresPlaying()
        {
            var idle = new SkirmishBaseAssaultFacts
            {
                PlayerDesignatedAlive = true,
                EnemyDesignatedAlive = true,
                Playing = false,
                Surrender = true,
                DeadlineSeconds = 1080
            };
            Assert.IsFalse(SkirmishBaseAssaultObjectiveSystem.TryEvaluate(in idle, out _, out _));

            var playing = idle;
            playing.Playing = true;
            Assert.IsTrue(SkirmishBaseAssaultObjectiveSystem.TryEvaluate(
                in playing, out SkirmishOutcomeKind outcome, out SkirmishEndReasonKind reason));
            Assert.AreEqual(SkirmishOutcomeKind.Defeat, outcome);
            Assert.AreEqual(SkirmishEndReasonKind.Surrender, reason);
        }

        [Test]
        public void DesignatedFactsIgnoreReplacementBarracks()
        {
            using var world = new World(nameof(DesignatedFactsIgnoreReplacementBarracks));
            EntityManager em = world.EntityManager;
            var sessionId = new FixedString64Bytes("fact-s002");
            Entity designated = SkirmishScenarioSpawnSystem.CreateStructure(
                em,
                sessionId,
                new SkirmishResolvedStructureEntry
                {
                    FactionId = 1,
                    StructureId = SkirmishStructureIds.Barracks,
                    ObjectiveRoleId = SkirmishObjectiveIds.BasePlayer,
                    DesignatedBase = true
                });
            Entity replacement = SkirmishScenarioSpawnSystem.CreateStructure(
                em,
                sessionId,
                new SkirmishResolvedStructureEntry
                {
                    FactionId = 1,
                    StructureId = SkirmishStructureIds.BarracksReplacement,
                    ObjectiveRoleId = string.Empty,
                    DesignatedBase = false
                });
            em.AddComponentData(designated, new UnitHealth { Current = 800, Max = 800 });
            em.AddComponentData(replacement, new UnitHealth { Current = 0, Max = 800 });
            Entity enemy = SkirmishScenarioSpawnSystem.CreateStructure(
                em,
                sessionId,
                new SkirmishResolvedStructureEntry
                {
                    FactionId = 2,
                    StructureId = SkirmishStructureIds.Barracks,
                    ObjectiveRoleId = SkirmishObjectiveIds.BaseEnemy,
                    DesignatedBase = true
                });
            em.AddComponentData(enemy, new UnitHealth { Current = 800, Max = 800 });

            Entity soldier = em.CreateEntity();
            em.AddComponentData(soldier, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Rifle });
            em.AddComponentData(soldier, new UnitHealth { Current = 0, Max = 100 });

            using var designatedQuery = em.CreateEntityQuery(
                typeof(SkirmishObjectiveRoleComponent), typeof(UnitHealth));
            using var structures = em.CreateEntityQuery(typeof(SkirmishStructureIdentityComponent));
            using var combat = em.CreateEntityQuery(typeof(SkirmishUnitRoleComponent), typeof(UnitHealth));
            SkirmishBaseAssaultFacts facts = SkirmishObjectiveFactProjectionSystem.Collect(
                em, designatedQuery, structures, combat);
            Assert.IsTrue(facts.PlayerDesignatedAlive);
            Assert.IsTrue(facts.EnemyDesignatedAlive);
            Assert.IsTrue(facts.ReplacementBarracksPresent);
            Assert.IsTrue(facts.FieldArmyWiped);
            Assert.IsFalse(SkirmishBaseAssaultObjectiveSystem.TryEvaluate(in facts, out _, out _));
        }

        [Test]
        public void HiddenHealthProjectsVisibleLastSeenAndUnknown()
        {
            using var world = new World(nameof(HiddenHealthProjectsVisibleLastSeenAndUnknown));
            EntityManager em = world.EntityManager;
            var sessionId = new FixedString64Bytes("hud-s002");
            Entity session = em.CreateEntity();
            em.AddComponentData(session, new SkirmishExpandedSessionComponent
            {
                SessionId = sessionId,
                CatalogId = "S002",
                IsLegacy = 0,
                Phase = SkirmishSessionPhase.Playing
            });
            em.AddComponentData(session, new SkirmishMatchState { SessionId = sessionId });

            Entity player = SkirmishScenarioSpawnSystem.CreateStructure(
                em,
                sessionId,
                new SkirmishResolvedStructureEntry
                {
                    FactionId = 1,
                    StructureId = SkirmishStructureIds.Barracks,
                    ObjectiveRoleId = SkirmishObjectiveIds.BasePlayer,
                    DesignatedBase = true
                });
            Entity enemy = SkirmishScenarioSpawnSystem.CreateStructure(
                em,
                sessionId,
                new SkirmishResolvedStructureEntry
                {
                    FactionId = 2,
                    StructureId = SkirmishStructureIds.Barracks,
                    ObjectiveRoleId = SkirmishObjectiveIds.BaseEnemy,
                    DesignatedBase = true
                });
            em.AddComponentData(player, new UnitHealth { Current = 800, Max = 800 });
            em.AddComponentData(enemy, new UnitHealth { Current = 800, Max = 800 });
            em.AddComponentData(player, new SkirmishContactSightComponent { Sight = SkirmishContactSight.Unknown });
            em.AddComponentData(enemy, new SkirmishContactSightComponent { Sight = SkirmishContactSight.Visible });

            SkirmishBaseAssaultHudProjection.Observe(em, session, 0.5f, false);
            SkirmishHealthReadout visible = SkirmishBaseAssaultHudProjection.ReadEnemyBase(em, session);
            Assert.AreEqual(SkirmishHealthReadout.Visible, visible.Knowledge);
            Assert.AreEqual(800, visible.DisplayedCurrent);
            Assert.AreEqual(0f, visible.AgeSeconds);
            SkirmishHealthReadout hidden = SkirmishBaseAssaultHudProjection.ReadPlayerBase(em, session);
            Assert.AreEqual(SkirmishHealthReadout.Hidden, hidden.Knowledge);
            Assert.AreEqual(0, hidden.DisplayedCurrent);

            var enemyHealth = em.GetComponentData<UnitHealth>(enemy);
            enemyHealth.Current = 410;
            em.SetComponentData(enemy, enemyHealth);
            SkirmishBaseAssaultHudProjection.Observe(em, session, 0.5f, false);
            Assert.AreEqual(410, SkirmishBaseAssaultHudProjection.ReadEnemyBase(em, session).DisplayedCurrent);

            em.SetComponentData(enemy, new SkirmishContactSightComponent { Sight = SkirmishContactSight.LastSeen });
            enemyHealth.Current = 90;
            em.SetComponentData(enemy, enemyHealth);
            SkirmishBaseAssaultHudProjection.Observe(em, session, 2f, false);
            SkirmishHealthReadout lastSeen = SkirmishBaseAssaultHudProjection.ReadEnemyBase(em, session);
            Assert.AreEqual(SkirmishHealthReadout.LastSeen, lastSeen.Knowledge);
            Assert.AreEqual(410, lastSeen.DisplayedCurrent);
            Assert.GreaterOrEqual(lastSeen.AgeSeconds, 2f);

            SkirmishBaseAssaultHudProjection.Observe(em, session, 5f, true);
            Assert.AreEqual(lastSeen.AgeSeconds,
                SkirmishBaseAssaultHudProjection.ReadEnemyBase(em, session).AgeSeconds);
            Assert.AreEqual(enemy, em.GetComponentData<SkirmishMatchState>(session).EnemyMainBase);
            Assert.AreEqual(player, em.GetComponentData<SkirmishMatchState>(session).PlayerMainBase);
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedObjectiveTests();
                suite.ReplacementBarracksAndArmyWipeAreNonTerminal();
                suite.PausedDeadlineDoesNotFreeze();
                suite.BothDesignatedDeadIsDrawRegardlessOfReplacement();
                suite.SurrenderRequiresPlaying();
                suite.DesignatedFactsIgnoreReplacementBarracks();
                suite.HiddenHealthProjectsVisibleLastSeenAndUnknown();
                Debug.Log("[SkirmishExpandedObjectiveTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishExpandedObjectiveTests] result=Failed\n" + exception);
                throw;
            }
        }
    }
}
