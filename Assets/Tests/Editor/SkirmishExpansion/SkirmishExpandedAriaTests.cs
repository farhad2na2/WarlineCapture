using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishExpandedAriaTests
    {
        [Test]
        public void SharedScoringUsesEligibilityAndHidesHostileCash()
        {
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var overlays = SkirmishRoleOverlayCatalog.CreateS002GroundSlice();
            var perception = new SkirmishPublicPerception
            {
                Playing = true,
                PlayerDesignatedAlive = true,
                EnemyDesignatedAlive = true,
                OwnMaterials = 900,
                OwnFuel = 700,
                OwnInfantryLive = 20,
                OwnGroundLive = 3,
                OwnSupplyLive = 34,
                OwnSupplyCap = 128,
                VisibleHostileTanks = 1,
                VisibleHostileGround = 1,
                KnowsHostileMaterials = false,
                HostileMaterialsIfKnown = 900
            };
            Assert.AreEqual(-1, SkirmishStrategyScoring.HostileMaterialsOrUnknown(perception));
            perception.KnowsHostileMaterials = true;
            Assert.AreEqual(900, SkirmishStrategyScoring.HostileMaterialsOrUnknown(perception));
            perception.KnowsHostileMaterials = false;
            Assert.IsTrue(SkirmishStrategyScoring.TryAfford(
                authored.ArmyGround, SkirmishReadinessStage.Established, overlays, perception,
                SkirmishRoleIds.Rocketeer, SkirmishRoleKind.Rocketeer));
            SkirmishStrategyScore score = SkirmishStrategyScoring.ScoreBaseAssault(
                perception, authored.ArmyGround, SkirmishReadinessStage.Established, overlays,
                SkirmishStrategyPriority.None);
            Assert.AreEqual(SkirmishStrategyPriority.RecruitCounter, score.Priority);
            Assert.AreEqual(SkirmishRoleKind.Rocketeer, score.RecruitRole);

            perception.OwnMaterials = 10;
            Assert.IsFalse(SkirmishStrategyScoring.TryAfford(
                authored.ArmyGround, SkirmishReadinessStage.Established, overlays, perception,
                SkirmishRoleIds.Tank, SkirmishRoleKind.Tank));
        }

        [Test]
        public void EnemyStrategyRecruitsThroughSharedProduceAndLeavesPlayerStocks()
        {
            using var world = new World(nameof(EnemyStrategyRecruitsThroughSharedProduceAndLeavesPlayerStocks));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            int playerMaterials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            int enemyMaterials = em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials;
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishStrategyScore score = SkirmishEnemyStrategySystem.Evaluate(
                em, session, owned, authored.ArmyGround);
            Assert.AreEqual(SkirmishStrategyPriority.RecruitCounter, score.Priority);
            Assert.AreEqual(playerMaterials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(enemyMaterials - 120, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            Assert.AreEqual(
                SkirmishStrategyPriority.RecruitCounter,
                em.GetComponentData<SkirmishEnemyStrategyComponent>(session).Priority);
        }

        [Test]
        public void EnemyAttackRequiresVisibleTargetAndUsesLegalGroupOrder()
        {
            using var world = new World(nameof(EnemyAttackRequiresVisibleTargetAndUsesLegalGroupOrder));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var stock = em.GetComponentData<SkirmishEnemyStockComponent>(session);
            stock.Materials = 0;
            em.SetComponentData(session, stock);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishStrategyScore score = SkirmishEnemyStrategySystem.Evaluate(
                em, session, owned, authored.ArmyGround);
            Assert.AreEqual(SkirmishStrategyPriority.AttackBase, score.Priority);
            uint groupId = em.GetComponentData<SkirmishEnemyStrategyComponent>(session).LastGroupId;
            Assert.AreNotEqual(0u, groupId);
            Assert.IsTrue(SkirmishArmyGroupSystem.TryGet(em, session, groupId, out SkirmishArmyGroupRecord group));
            Assert.AreEqual(2, group.FactionId);
            Assert.AreEqual(SkirmishGroupOrderKind.Attack, group.LastOrder);

            Entity playerBase = FirstPlayerBase(em);
            SkirmishFogService.Hide(em, playerBase);
            Assert.IsFalse(SkirmishArmyCommandService.TryIssueGroupOrder(
                em, session, groupId, 2, SkirmishGroupOrderKind.Attack, playerBase,
                out SkirmishCommandDecision hidden));
            Assert.AreEqual(SkirmishReasonCode.HiddenContact, hidden.Reason);
        }

        [Test]
        public void AriaSkillsStayOnVisibleControlsAndHandBackAfterRetries()
        {
            var recruit = SkirmishAriaSkillPolicy.Step(new SkirmishAriaPublicView
            {
                Playing = true,
                OwnInfantry = 8,
                CanAffordRifle = true,
                RecruitControlAvailable = true,
                PlayerDesignatedAlive = true,
                EnemyDesignatedAlive = true
            });
            Assert.AreEqual(SkirmishAriaSkillKind.Recruit, recruit.Skill);
            Assert.AreEqual(SkirmishAriaSkillPhase.Act, recruit.Phase);

            var handback = SkirmishAriaSkillPolicy.Step(new SkirmishAriaPublicView
            {
                Playing = true,
                FailedAttempts = 3,
                LastSkill = SkirmishAriaSkillKind.Attack,
                HoldControlAvailable = false,
                RecruitControlAvailable = false
            });
            Assert.IsTrue(handback.Handback);
            Assert.AreEqual(SkirmishAriaSkillKind.Handback, handback.Skill);

            var result = SkirmishAriaSkillPolicy.Step(new SkirmishAriaPublicView { Finished = true });
            Assert.AreEqual(SkirmishAriaSkillKind.RecognizeResult, result.Skill);
            Assert.AreEqual(SkirmishAriaSkillPhase.Terminal, result.Phase);
        }

        [Test]
        public void ExpandedAriaPlanTargetsPublicControlsWithoutGameplayMutation()
        {
            using var world = new World(nameof(ExpandedAriaPlanTargetsPublicControlsWithoutGameplayMutation));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            int materials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                Infantry = 8,
                CanAffordRifle = true,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                Recruit = new AriaTouchTarget { Id = 41, Available = true },
                Attack = new AriaTouchTarget { Id = 42, Available = true },
                Hold = new AriaTouchTarget { Id = 43, Available = true },
                Squad0 = new AriaTouchTarget { Id = 11, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var idle = new AriaPlaySessionComponent();
            var idleOutput = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref idle, ref idleOutput);
            Assert.AreEqual(AriaSkirmishIntent.Recruit, plan.Intent);
            Assert.AreEqual(41, idleOutput.TargetId);
            Assert.AreEqual(AriaPlayObservationKind.Control, idleOutput.Kind);

            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Recruit, plan.Intent);
            Assert.AreEqual(41, output.TargetId);
            Assert.AreEqual(AriaPlayObservationKind.Control, output.Kind);
            Assert.AreEqual(materials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            view.Infantry = 20;
            view.SelectionVisible = true;
            view.ExpandedRetries = 0;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Attack, plan.Intent);
            Assert.AreEqual(42, output.TargetId);

            view.ExpandedRetries = 3;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Hold, plan.Intent);
            Assert.AreEqual(43, output.TargetId);

            view.Finished = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Handback, plan.Intent);
            Assert.AreEqual(AriaPlayObservationKind.Finished, output.Kind);
        }

        [Test]
        public void PublicProjectionDoesNotExposeEnemyWalletToAria()
        {
            using var world = new World(nameof(PublicProjectionDoesNotExposeEnemyWalletToAria));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var enemy = em.GetComponentData<SkirmishEnemyStockComponent>(session);
            enemy.Materials = 77;
            em.SetComponentData(session, enemy);
            SkirmishAriaPublicView view = SkirmishAriaPublicProjection.FromSession(
                em, session, authored.ArmyGround);
            Assert.AreEqual(900, view.OwnMaterials);
            Assert.AreNotEqual(77, view.OwnMaterials);
            Assert.Greater(view.VisibleHostileCombat, 0);
            Assert.IsTrue(view.CanAffordRifle);
            SkirmishAriaSkillDecision decision = SkirmishAriaSkillPolicy.Step(view);
            Assert.AreNotEqual(SkirmishAriaSkillKind.None, decision.Skill);
        }

        [Test]
        public void S003AirMobilePublicControlsDoNotMutateGameplay()
        {
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var perception = new SkirmishPublicPerception
            {
                Playing = true,
                PlayerDesignatedAlive = true,
                EnemyDesignatedAlive = true,
                OwnMaterials = 450,
                OwnFuel = 350,
                OwnInfantryLive = 12,
                OwnGroundLive = 2,
                OwnSupplyLive = 20,
                OwnSupplyCap = 128,
                VisibleHostileAir = 1
            };
            SkirmishRoleOverlay[] overlays = SkirmishRoleOverlayCatalog.CreateAirMobileSlice();
            SkirmishStrategyScore air = SkirmishStrategyScoring.ScoreBaseAssault(
                perception, authored.ArmyAir, SkirmishReadinessStage.Field, overlays, SkirmishStrategyPriority.None);
            Assert.AreEqual(SkirmishStrategyPriority.RecruitCounter, air.Priority);
            Assert.AreEqual(SkirmishRoleKind.AntiAir, air.RecruitRole);
            Assert.AreEqual("counter.aa", air.Field);

            perception.VisibleHostileAir = 0;
            SkirmishStrategyScore ground = SkirmishStrategyScoring.ScoreBaseAssault(
                perception, authored.ArmyGround, SkirmishReadinessStage.Established,
                SkirmishRoleOverlayCatalog.CreateS002GroundSlice(), SkirmishStrategyPriority.None);
            Assert.AreNotEqual(SkirmishRoleKind.AntiAir, ground.RecruitRole);

            var aaSkill = SkirmishAriaSkillPolicy.Step(new SkirmishAriaPublicView
            {
                Playing = true,
                VisibleHostileAir = 1,
                CanAffordAntiAir = true,
                RecruitControlAvailable = true,
                OwnInfantry = 8,
                CanAffordRifle = true
            });
            Assert.AreEqual(SkirmishAriaSkillKind.Recruit, aaSkill.Skill);
            Assert.AreEqual("recruit.aa", aaSkill.Field);

            var padSkill = SkirmishAriaSkillPolicy.Step(new SkirmishAriaPublicView
            {
                Playing = true,
                VisibleHostileAir = 1,
                CanAffordAntiAir = false,
                PadReady = false,
                AirPadControlAvailable = true,
                OwnInfantry = 20,
                RecruitControlAvailable = true
            });
            Assert.AreEqual(SkirmishAriaSkillKind.Inspect, padSkill.Skill);
            Assert.AreEqual("pad.not_ready", padSkill.Field);

            using var world = new World(nameof(S003AirMobilePublicControlsDoNotMutateGameplay));
            EntityManager em = world.EntityManager;
            CompileAndSpawnS003(em, out Entity session, out SkirmishResolvedSetup setup);
            int playerMaterials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            int enemyMaterials = em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            SkirmishScenarioSpawnSystem.CreateForceMember(
                em,
                sessionId,
                new SkirmishResolvedForceEntry
                {
                    FactionId = 1,
                    RoleId = SkirmishRoleIds.AttackHeliLight,
                    RoleKind = SkirmishRoleKind.AttackHeliLight,
                    Quantity = 1,
                    SupplyCost = 8,
                    RuntimePrefabKey = SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.AttackHeliLight)
                },
                90,
                0,
                8,
                SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.AttackHeliLight));
            using (var spawned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent)))
            {
                SkirmishRosterProjectionSystem.Apply(em, spawned, sessionId, setup);
            }

            SkirmishFogService.Project(em, session);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishStrategyScore executed = SkirmishEnemyStrategySystem.Evaluate(
                em, session, owned, authored.ArmyAir);
            Assert.AreEqual(SkirmishStrategyPriority.RecruitCounter, executed.Priority);
            Assert.AreEqual(SkirmishRoleKind.AntiAir, executed.RecruitRole);
            Assert.AreEqual(playerMaterials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(enemyMaterials - 220, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);

            SkirmishAriaPublicView projected = SkirmishAriaPublicProjection.FromSession(
                em, session, authored.ArmyAir);
            Assert.AreEqual(playerMaterials, projected.OwnMaterials);
            Assert.IsTrue(projected.CanAffordAntiAir);
            Assert.IsFalse(projected.PadReady);
            Assert.IsFalse(projected.CanAffordTank);

            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                Infantry = 8,
                CanAffordRifle = true,
                CanAffordAntiAir = true,
                VisibleHostileAir = 1,
                PadReady = false,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                Recruit = new AriaTouchTarget { Id = 41, Available = true },
                RecruitAntiAir = new AriaTouchTarget { Id = 51, Available = true },
                AirPad = new AriaTouchTarget { Id = 52, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Recruit, plan.Intent);
            Assert.AreEqual(51, output.TargetId);
            Assert.AreEqual(AriaPlayObservationKind.Control, output.Kind);
            Assert.AreEqual(playerMaterials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            view.CanAffordAntiAir = false;
            view.RecruitAntiAir = new AriaTouchTarget { Id = 51, Available = false };
            view.AirQueueOffered = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Inspect, plan.Intent);
            Assert.AreEqual(52, output.TargetId);
            Assert.AreEqual(playerMaterials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(enemyMaterials - 220, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
        }

        [Test]
        public void S004EstablishedPadReadyDoesNotMutatePlayerStocks()
        {
            SkirmishAriaSkillDecision ready = SkirmishAriaSkillPolicy.Step(new SkirmishAriaPublicView
            {
                Playing = true,
                VisibleHostileAir = 1,
                CanAffordAntiAir = false,
                PadReady = true,
                AirPadControlAvailable = true,
                OwnInfantry = 20,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                AttackControlAvailable = true,
                GroupControlAvailable = true
            });
            Assert.AreNotEqual("pad.not_ready", ready.Field);

            using var world = new World(nameof(S004EstablishedPadReadyDoesNotMutatePlayerStocks));
            EntityManager em = world.EntityManager;
            CompileAndSpawnS004(em, out Entity session, out _);
            int playerMaterials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            int enemyMaterials = em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials;
            Assert.AreEqual(900, playerMaterials);
            Assert.AreEqual(900, enemyMaterials);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            SkirmishStrategyScore executed = SkirmishEnemyStrategySystem.Evaluate(
                em, session, owned, authored.ArmyAir);
            Assert.AreEqual(SkirmishStrategyPriority.RecruitCounter, executed.Priority);
            Assert.AreEqual(SkirmishRoleKind.AntiAir, executed.RecruitRole);
            Assert.AreEqual(playerMaterials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(enemyMaterials - 220, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);

            SkirmishAriaPublicView projected = SkirmishAriaPublicProjection.FromSession(
                em, session, authored.ArmyAir);
            Assert.AreEqual(900, projected.OwnMaterials);
            Assert.IsTrue(projected.PadReady);
            Assert.IsTrue(projected.AirQueueOffered);
            Assert.GreaterOrEqual(projected.VisibleHostileAir, 1);
            Assert.AreEqual(20, projected.OwnInfantry);
            Assert.IsTrue(projected.CanAffordAntiAir);
            Assert.IsFalse(projected.CanAffordTank);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(1, em.GetComponentData<SkirmishEnemyCapacityComponent>(session).AirLive);
            int enemyGround = em.GetComponentData<SkirmishEnemyCapacityComponent>(session).GroundLive;
            Assert.Greater(enemyGround, 0);

            SkirmishAriaSkillDecision counter = SkirmishAriaSkillPolicy.Step(new SkirmishAriaPublicView
            {
                Playing = true,
                VisibleHostileAir = projected.VisibleHostileAir,
                CanAffordAntiAir = projected.CanAffordAntiAir,
                PadReady = projected.PadReady,
                AirQueueOffered = projected.AirQueueOffered,
                RecruitControlAvailable = true,
                OwnInfantry = projected.OwnInfantry,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                AttackControlAvailable = true,
                GroupControlAvailable = true
            });
            Assert.AreEqual(SkirmishAriaSkillKind.Recruit, counter.Skill);
            Assert.AreEqual("recruit.aa", counter.Field);

            SkirmishAriaSkillDecision offensive = SkirmishAriaSkillPolicy.Step(new SkirmishAriaPublicView
            {
                Playing = true,
                VisibleHostileAir = 1,
                CanAffordAntiAir = false,
                PadReady = true,
                AirQueueOffered = true,
                RecruitControlAvailable = true,
                OwnInfantry = 20,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                AttackControlAvailable = true,
                GroupControlAvailable = true
            });
            Assert.AreEqual(SkirmishAriaSkillKind.Recruit, offensive.Skill);
            Assert.AreEqual("recruit.attack_heli", offensive.Field);

            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                Infantry = 20,
                CanAffordAntiAir = true,
                VisibleHostileAir = projected.VisibleHostileAir,
                PadReady = true,
                AirQueueOffered = true,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                SelectionVisible = true,
                RecruitAntiAir = new AriaTouchTarget { Id = 51, Available = true },
                AirPad = new AriaTouchTarget { Id = 52, Available = true },
                Attack = new AriaTouchTarget { Id = 53, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Recruit, plan.Intent);
            Assert.AreEqual(51, output.TargetId);
            Assert.AreEqual(playerMaterials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            view.CanAffordAntiAir = false;
            view.RecruitAntiAir = new AriaTouchTarget { Id = 51, Available = false };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Attack, plan.Intent);
            Assert.AreEqual(53, output.TargetId);
            Assert.AreNotEqual(52, output.TargetId);

            Entity pad = FindStructure(em, 1, SkirmishStructureIds.Helipad);
            var padHealth = em.GetComponentData<UnitHealth>(pad);
            padHealth.Current = 0;
            em.SetComponentData(pad, padHealth);
            SkirmishAriaPublicView afterPad = SkirmishAriaPublicProjection.FromSession(
                em, session, authored.ArmyAir);
            Assert.IsFalse(afterPad.PadReady);
            Assert.IsFalse(afterPad.AirQueueOffered);
            Assert.AreEqual(900, afterPad.OwnMaterials);
            SkirmishAriaSkillDecision padSkill = SkirmishAriaSkillPolicy.Step(new SkirmishAriaPublicView
            {
                Playing = true,
                VisibleHostileAir = 1,
                CanAffordAntiAir = false,
                PadReady = afterPad.PadReady,
                AirQueueOffered = afterPad.AirQueueOffered,
                AirPadControlAvailable = true,
                OwnInfantry = 20,
                RecruitControlAvailable = true
            });
            Assert.AreEqual(SkirmishAriaSkillKind.Inspect, padSkill.Skill);
            Assert.AreEqual("pad.not_ready", padSkill.Field);

            view.PadReady = false;
            view.AirQueueOffered = true;
            view.AirPad = new AriaTouchTarget { Id = 52, Available = true };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Inspect, plan.Intent);
            Assert.AreEqual(52, output.TargetId);
            Assert.AreEqual(playerMaterials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(enemyMaterials - 220, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(1, em.GetComponentData<SkirmishEnemyCapacityComponent>(session).AirLive);
            Assert.AreEqual(enemyGround, em.GetComponentData<SkirmishEnemyCapacityComponent>(session).GroundLive);
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedAriaTests();
                suite.SharedScoringUsesEligibilityAndHidesHostileCash();
                suite.EnemyStrategyRecruitsThroughSharedProduceAndLeavesPlayerStocks();
                suite.EnemyAttackRequiresVisibleTargetAndUsesLegalGroupOrder();
                suite.AriaSkillsStayOnVisibleControlsAndHandBackAfterRetries();
                suite.ExpandedAriaPlanTargetsPublicControlsWithoutGameplayMutation();
                suite.PublicProjectionDoesNotExposeEnemyWalletToAria();
                suite.S003AirMobilePublicControlsDoNotMutateGameplay();
                suite.S004EstablishedPadReadyDoesNotMutatePlayerStocks();
                SkirmishS002AriaHarnessTests.RunFocusedValidation();
                SkirmishS003AriaHarnessTests.RunFocusedValidation();
                SkirmishS004AriaHarnessTests.RunFocusedValidation();
                Debug.Log("[SkirmishExpandedAriaTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishExpandedAriaTests] result=Failed\n" + exception);
                throw;
            }
        }

        private static void CompileAndSpawn(EntityManager em, out Entity session, out SkirmishResolvedSetup setup)
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
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
                out setup,
                out _,
                out var reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(
                em, session, setup, out SkirmishReasonCode reason, out byte visualPending), reason.ToString());
            Assert.AreEqual(0, visualPending);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(
                em,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                setup);
            SkirmishArmyGroupSystem.RefreshAlive(
                em,
                session,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
        }

        private static void CompileAndSpawnS003(EntityManager em, out Entity session, out SkirmishResolvedSetup setup)
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS003.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                "S003",
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                SkirmishS003FirstVisit.SeedA,
                authored,
                matrix,
                manifest,
                out setup,
                out _,
                out var reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(
                em, session, setup, out SkirmishReasonCode reason, out byte visualPending), reason.ToString());
            Assert.AreEqual(0, visualPending);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(
                em,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                setup);
            SkirmishArmyGroupSystem.RefreshAlive(
                em,
                session,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
        }

        private static void CompileAndSpawnS004(EntityManager em, out Entity session, out SkirmishResolvedSetup setup)
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS004.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                "S004",
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                SkirmishS004FirstVisit.SeedA,
                authored,
                matrix,
                manifest,
                out setup,
                out _,
                out var reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(
                em, session, setup, out SkirmishReasonCode reason, out byte visualPending), reason.ToString());
            Assert.AreEqual(0, visualPending);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(
                em,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                setup);
            SkirmishArmyGroupSystem.RefreshAlive(
                em,
                session,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
        }

        private static Entity FindStructure(EntityManager em, byte faction, string structureId)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishStructureIdentityComponent), typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId != faction)
                    continue;
                if (em.GetComponentData<SkirmishStructureIdentityComponent>(entities[i]).StructureId.ToString() == structureId)
                    return entities[i];
            }

            return Entity.Null;
        }

        private static Entity FirstPlayerBase(EntityManager em)
        {
            using var query = em.CreateEntityQuery(
                typeof(SkirmishObjectiveRoleComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId == 1 &&
                    em.GetComponentData<SkirmishObjectiveRoleComponent>(entities[i]).Role ==
                    SkirmishObjectiveRoleKind.PlayerBase)
                    return entities[i];
            }

            return Entity.Null;
        }
    }
}
