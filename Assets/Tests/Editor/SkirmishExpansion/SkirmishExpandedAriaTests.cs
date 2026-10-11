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
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishExpandedAriaTests
    {
        [Test]
        public void CombinedArmsPreservesStructureForceBeforeBuyingRifles()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, AirProfile = true, CombinedArmsField = true,
                Infantry = 12, OwnStructureAttackUnits = 4, CanAffordRocketeer = true, CanAffordRifle = true,
                Time = 10, PlayerHealth = 800, EnemyHealth = 800,
                Recruit = new AriaTouchTarget { Id = 80, Available = true },
                RecruitRocketeer = new AriaTouchTarget { Id = 120, Available = true },
                CloseDrawer = new AriaTouchTarget { Id = 9, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(120, output.TargetId, "Use the presented structure-capable squad before spending its budget on rifles.");
            view.RocketeerRecruitPending = true;
            view.DrawerOpen = true;
            view.Time++;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(9, output.TargetId, "An accepted paid queue must close Build rather than queue another squad.");
            view.RocketeerRecruitPending = false;
            view.DrawerOpen = false;
            view.OwnStructureAttackUnits = 8;
            view.Infantry = 16;
            view.Time++;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(80, output.TargetId, "A viable structure force can then receive rifle support.");
        }
        [TestCase(false)]
        [TestCase(true)]
        public void AirOpeningUsesVisibleDefenseControlsBeforeCommittingTheAssault(bool combinedArms)
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, AirProfile = true, CombinedArmsField = combinedArms,
                Infantry = 16, Time = 10, PlayerHealth = 800, EnemyHealth = 800,
                DefenseBuild = new AriaTouchTarget { Id = 50, Available = true },
                FocusPlayer = new AriaTouchTarget { Id = 51, Available = true },
                PlacementConfirm = new AriaTouchTarget { Id = 52, Available = true },
                Site0 = new AriaTouchTarget { Id = 53, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(51, output.TargetId);
            Assert.AreEqual(0, plan.AssaultStarted);
            touch.Actions++;
            view.Time = 11;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            view.Time = 14;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(50, output.TargetId, "Construction must begin through the presented Build control.");
            plan.DefenseStage = 2;
            plan.DefensePositioned = 1;
            view.PlacementOpen = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(52, output.TargetId);
            Assert.AreEqual(0, plan.DefensesPlaced, "A proposed confirm is not a completed purchase.");
            touch.Actions++;
            view.PlacementOpen = false;
            view.Time = 15;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0, plan.DefensesPlaced, "A completed tap without a new presented tower is not a purchase.");
            view.OwnDefenseTowers = 1;
            view.Time = 16;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(1, plan.DefensesPlaced);
            Assert.AreEqual(0, plan.AssaultStarted);
        }

        [Test]
        public void LegacyDefenseConfirmationDoesNotWaitForUnavailableBuildingCensus()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = false, Time = 15, Infantry = 16,
                PlayerHealth = 800, EnemyHealth = 800,
                DefenseBuild = new AriaTouchTarget { Id = 50, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent
            { DefenseStage = 3, DefenseDeadline = 75, DefenseActions = 1 };
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing, Actions = 2 };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(1, plan.DefensesPlaced);
            Assert.AreEqual(2, plan.DefenseStage, "Legacy confirmation must continue without an expanded-only census.");
        }

        [Test]
        public void PresentedHomeRaidUsesSquadAttackAndResumesObjectiveOnlyAfterItClears()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, PresentedHomeThreat = true,
                PlayerHealth = 800, EnemyHealth = 800, EnemyDesignatedAlive = true, Time = 10,
                ExpandedAssaultMask = 3, ExpandedPageCount = 1,
                FocusPlayer = new AriaTouchTarget { Id = 51, Available = true },
                Squad0 = new AriaTouchTarget { Id = 10, Available = true },
                Squad1 = new AriaTouchTarget { Id = 11, Available = true },
                Attack = new AriaTouchTarget { Id = 12, Available = true },
                ThreatGround = new AriaTouchTarget { Id = -20006, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent { AssaultIssued = 1, StructureOrdered = 1 };
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(51, output.TargetId, "Camera guidance cannot issue a defensive troop order.");
            touch.Actions++;
            view.Time++;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(10, output.TargetId);
            view.ExpandedSelectedMask = 1;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(11, output.TargetId);
            view.ExpandedSelectedMask = 3;
            view.ExpandedSelectedGroups = 2;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(12, output.TargetId);
            view.AttackMode = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(-20006, output.TargetId);
            Assert.AreEqual(AriaPlayObservationKind.WorldTarget, output.Kind);
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(5, plan.HomeDefenseStage, "An offered target is not a completed defensive order.");
            touch.Actions++;
            view.AttackMode = false;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(8, plan.HomeDefenseStage);
            view.PresentedHomeThreat = false;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0, plan.HomeDefenseStage);
            Assert.AreEqual(0, plan.AssaultIssued, "A defensive order cannot certify an enemy-base assault.");
        }

        [Test]
        public void FieldArmyReservesPartOfItsStartingForceInsteadOfUnfilledCapacity()
        {
            var perception = new SkirmishPublicPerception
            {
                Playing = true, PlayerDesignatedAlive = true, EnemyDesignatedAlive = true,
                OwnInfantryLive = 12, OwnGroundLive = 2, OwnSupplyLive = 20,
                OwnStartingSupply = 20, OwnSupplyCap = 128
            };
            Assert.AreEqual(SkirmishStrategyPriority.AttackBase,
                SkirmishStrategyScoring.ScoreBaseAssault(perception, null,
                    SkirmishReadinessStage.Field, null, SkirmishStrategyPriority.None).Priority);
            perception.OwnSupplyLive = 5;
            Assert.AreEqual(SkirmishStrategyPriority.Hold,
                SkirmishStrategyScoring.ScoreBaseAssault(perception, null,
                    SkirmishReadinessStage.Field, null, SkirmishStrategyPriority.AttackBase).Priority);
            perception.OwnSupplyLive = 10;
            Assert.AreEqual(SkirmishStrategyPriority.AttackBase,
                SkirmishStrategyScoring.ScoreBaseAssault(perception, null,
                    SkirmishReadinessStage.Field, null, SkirmishStrategyPriority.Hold).Priority);
        }

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
            Entity tank = FirstRole(em, 1, SkirmishRoleKind.Tank);
            Entity playerBase = FirstPlayerBase(em);
            Assert.IsFalse(SkirmishFogService.IsVisibleToFaction(em, tank, 2));
            Assert.AreEqual(0, SkirmishEnemyStrategySystem.Perceive(em, session, owned).VisibleHostileTanks);
            SkirmishStrategyScore unknown = SkirmishEnemyStrategySystem.Evaluate(em, session, owned, authored.ArmyGround);
            Assert.AreEqual(SkirmishStrategyPriority.AttackBase, unknown.Priority);
            Assert.AreEqual(playerMaterials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(enemyMaterials, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials,
                "An unknown tank cannot cause a counter purchase.");
            uint approachId = em.GetComponentData<SkirmishEnemyStrategyComponent>(session).LastGroupId;
            Assert.IsTrue(SkirmishArmyGroupSystem.TryGet(em, session, approachId, out var approach));
            Assert.AreEqual(SkirmishGroupOrderKind.Move, approach.LastOrder,
                "An unknown fixed base permits only its authored public objective approach.");
            Assert.IsFalse(SkirmishArmyCommandService.TryIssueGroupOrder(em, session, approachId, 2,
                SkirmishGroupOrderKind.Attack, playerBase, out var unknownAttack));
            Assert.AreEqual(SkirmishReasonCode.HiddenContact, unknownAttack.Reason);
            Entity observer = CreateNativeFogObserver(world, session, 2, new float3(100, 0, 100));
            PlaceFixtureContact(em, tank, new float3(105, 0, 100));
            PlaceFixtureContact(em, playerBase, new float3(108, 0, 100));
            AdvanceNativeFog(world);
            AssertDirectObservation(em, tank, 2, observer);
            AssertDirectObservation(em, playerBase, 2, observer);
            Assert.AreEqual(1, SkirmishEnemyStrategySystem.Perceive(em, session, owned).VisibleHostileTanks);
            SkirmishStrategyScore score = SkirmishEnemyStrategySystem.Evaluate(
                em, session, owned, authored.ArmyGround);
            Assert.AreEqual(SkirmishStrategyPriority.RecruitCounter, score.Priority);
            Assert.AreEqual(playerMaterials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(enemyMaterials - 120, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            Assert.AreEqual(
                SkirmishStrategyPriority.RecruitCounter,
                em.GetComponentData<SkirmishEnemyStrategyComponent>(session).Priority);

            SkirmishStrategyScore second = SkirmishEnemyStrategySystem.Evaluate(
                em, session, owned, authored.ArmyGround);
            Assert.AreEqual(SkirmishStrategyPriority.AttackBase, second.Priority);
            Assert.AreEqual(playerMaterials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(enemyMaterials - 120, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            uint groupId = em.GetComponentData<SkirmishEnemyStrategyComponent>(session).LastGroupId;
            Assert.AreNotEqual(0u, groupId);
            Assert.IsTrue(SkirmishArmyGroupSystem.TryGet(em, session, groupId, out SkirmishArmyGroupRecord group));
            Assert.AreEqual(2, group.FactionId);
            Assert.AreEqual(SkirmishRoleKind.Tank, group.Role);
            Assert.AreEqual(SkirmishGroupOrderKind.Attack, group.LastOrder);
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
            Entity playerBase = FirstPlayerBase(em);
            Entity observer = CreateNativeFogObserver(world, session, 2, new float3(100, 0, 100));
            PlaceFixtureContact(em, playerBase, new float3(105, 0, 100));
            AdvanceNativeFog(world);
            AssertDirectObservation(em, playerBase, 2, observer);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishStrategyScore score = SkirmishEnemyStrategySystem.Evaluate(
                em, session, owned, authored.ArmyGround);
            Assert.AreEqual(SkirmishStrategyPriority.AttackBase, score.Priority);
            uint groupId = em.GetComponentData<SkirmishEnemyStrategyComponent>(session).LastGroupId;
            Assert.AreNotEqual(0u, groupId);
            Assert.IsTrue(SkirmishArmyGroupSystem.TryGet(em, session, groupId, out SkirmishArmyGroupRecord group));
            Assert.AreEqual(2, group.FactionId);
            Assert.AreEqual(SkirmishGroupOrderKind.Attack, group.LastOrder);

            Assert.IsTrue(SkirmishFogService.TryGetKnowledge(em, playerBase, 2, out var observed));
            PlaceFixtureContact(em, observer, new float3(500, 0, 500));
            AdvanceNativeFog(world);
            Assert.IsFalse(SkirmishFogService.IsVisibleToFaction(em, playerBase, 2));
            Assert.IsTrue(SkirmishFogService.TryGetKnowledge(em, playerBase, 2, out var lost));
            Assert.AreEqual(SkirmishContactSight.LastSeen, lost.Sight);
            Assert.AreEqual(observed.LastSeenPosition, lost.LastSeenPosition);
            Assert.AreEqual(observed.LastObservedTimeSeconds, lost.LastObservedTimeSeconds);
            PlaceFixtureContact(em, playerBase, new float3(800, 0, 800));
            AdvanceNativeFog(world);
            Assert.IsTrue(SkirmishFogService.TryGetKnowledge(em, playerBase, 2, out var hiddenMove));
            Assert.AreEqual(lost.LastSeenPosition, hiddenMove.LastSeenPosition,
                "A hidden actor's live motion cannot refresh enemy LastSeen.");
            Assert.AreEqual(lost.LastObservedTimeSeconds, hiddenMove.LastObservedTimeSeconds);
            Assert.IsFalse(SkirmishArmyCommandService.TryIssueGroupOrder(
                em, session, groupId, 2, SkirmishGroupOrderKind.Attack, playerBase,
                out SkirmishCommandDecision hidden));
            Assert.AreEqual(SkirmishReasonCode.HiddenContact, hidden.Reason);
        }

        [Test]
        public void EnemyStructureDefendersReactToVisibleUnarmedIntrusionAndResumeBaseObjective()
        {
            using var world = new World(nameof(EnemyStructureDefendersReactToVisibleUnarmedIntrusionAndResumeBaseObjective));
            EntityManager em = world.EntityManager;
            CompileAndSpawnS003(em, out Entity session, out _);
            var sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            Entity enemyBase = Entity.Null;
            using (var query = em.CreateEntityQuery(typeof(SkirmishObjectiveRoleComponent), typeof(SkirmishAttemptOwnedComponent)))
            using (var entities = query.ToEntityArray(Allocator.Temp))
                foreach (Entity entity in entities)
                    if (em.GetComponentData<SkirmishObjectiveRoleComponent>(entity).Role == SkirmishObjectiveRoleKind.EnemyBase)
                        enemyBase = entity;
            Assert.AreNotEqual(Entity.Null, enemyBase);
            // Ledger-only editor fixtures have no world pose until visual attach.
            if (!em.HasComponent<LocalTransform>(enemyBase))
                em.AddComponentData(enemyBase, LocalTransform.FromPosition(new float3(300, 0, 300)));
            float3 basePosition = em.GetComponentData<LocalTransform>(enemyBase).Position;
            const uint groupId = 777;
            Entity defender = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(LocalTransform),
                typeof(UnitHealth), typeof(SkirmishArmyGroupMembershipComponent), typeof(SkirmishRoleOverlayComponent));
            em.SetComponentData(defender, new SkirmishAttemptOwnedComponent
                { SessionId = sessionId, FactionId = 2 });
            em.SetComponentData(defender, LocalTransform.FromPosition(basePosition + new float3(20, 0, 0)));
            em.SetComponentData(defender, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(defender, new SkirmishArmyGroupMembershipComponent { GroupId = groupId });
            em.SetComponentData(defender, new SkirmishRoleOverlayComponent
                { Damage = 10, TargetDomains = SkirmishTargetDomain.Structure, RangeWorld = 35 });
            Entity pad = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(LocalTransform),
                typeof(UnitHealth), typeof(SkirmishContactSightComponent));
            em.SetComponentData(pad, new SkirmishAttemptOwnedComponent
                { SessionId = sessionId, FactionId = 1, IsStructure = 1 });
            em.SetComponentData(pad, LocalTransform.FromPosition(basePosition + new float3(35, 0, 0)));
            em.SetComponentData(pad, new UnitHealth { Current = 100, Max = 100 });
            EnsureNativeFogFixture(world, session);
            FixtureSet(em, defender, new Faction { Id = 2 });
            FixtureSet(em, defender, new UnitCombat { AggroRangeCells = 35 });
            AdvanceNativeFog(world);
            AssertDirectObservation(em, pad, 2, defender);
            Assert.AreEqual(pad, SkirmishEnemyStrategySystem.FindDefendedHostileStructure(em, session, groupId));
            em.SetComponentData(defender, new SkirmishRoleOverlayComponent
                { Damage = 10, TargetDomains = SkirmishTargetDomain.Infantry, RangeWorld = 35 });
            Assert.AreEqual(Entity.Null, SkirmishEnemyStrategySystem.FindDefendedHostileStructure(em, session, groupId));
            em.SetComponentData(defender, new SkirmishRoleOverlayComponent
                { Damage = 10, TargetDomains = SkirmishTargetDomain.Structure, RangeWorld = 35 });
            em.SetComponentData(pad, LocalTransform.FromPosition(basePosition + new float3(150, 0, 0)));
            AdvanceNativeFog(world);
            Assert.AreEqual(Entity.Null, SkirmishEnemyStrategySystem.FindDefendedHostileStructure(em, session, groupId));
            em.SetComponentData(pad, LocalTransform.FromPosition(basePosition + new float3(35, 0, 0)));
            PlaceFixtureContact(em, defender, basePosition + new float3(300, 0, 0));
            AdvanceNativeFog(world);
            Assert.AreEqual(Entity.Null, SkirmishEnemyStrategySystem.FindDefendedHostileStructure(em, session, groupId));
            PlaceFixtureContact(em, defender, basePosition + new float3(20, 0, 0));
            AdvanceNativeFog(world);
            AssertDirectObservation(em, pad, 2, defender);
            em.SetComponentData(pad, new UnitHealth { Current = 0, Max = 100 });
            Assert.AreEqual(Entity.Null, SkirmishEnemyStrategySystem.FindDefendedHostileStructure(em, session, groupId));
        }

        [Test]
        public void AircraftReturnWaitsForTouchAndLandingBeforeSecondSortie()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, AirProfile = true, PadPresent = true,
                PlayerDesignatedAlive = true, EnemyDesignatedAlive = true,
                Time = 100, OwnAttackAirLive = 1, OwnAttackAirActive = 1, OwnAirFuel = 100,
                ExpandedAirMask = 1, Squad0 = new AriaTouchTarget { Id = 41, Available = true },
                ReturnAircraft = new AriaTouchTarget { Id = 42, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent
            {
                AssaultStarted = 1, AssaultIssued = 1, StructureOrdered = 1,
                LastProgressAt = 100, AirSortieObservedAt = 50
            };
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = default(AriaPlayObservationComponent);
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(41, output.TargetId);
            Assert.AreEqual(1, plan.AirCycleStage);
            view.SelectedAircraft = true;
            view.ReturnAircraft.Available = false;
            view.OpenAircraftActions = new AriaTouchTarget { Id = 43, Available = true };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(43, output.TargetId);
            Assert.AreEqual(2, plan.AirCycleStage, "Opening Commands must not count as a Return order.");
            touch.Actions++;
            view.OpenAircraftActions.Available = false;
            view.ReturnAircraft.Available = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(42, output.TargetId);
            Assert.AreEqual(3, plan.AirCycleStage);
            view.SelectedAircraft = false;
            view.OwnAttackAirActive = 0;
            view.OwnAttackAirLanded = 1;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(3, plan.AirCycleStage, "A physical landing cannot substitute for the completed Return touch.");
            touch.Actions++;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(4, plan.AirCycleStage);
            Assert.AreEqual(1, plan.AssaultIssued, "Do not overwrite the return order during service.");
            view.Time = 106;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(1, plan.AirCycleCount);
            Assert.AreEqual(0, plan.AssaultIssued, "Only a landed, fueled aircraft may be relaunched.");
            Assert.AreEqual(0, plan.AirCycleStage);
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
            Entity contact = FirstRole(em, 2, SkirmishRoleKind.Tank);
            Entity observer = CreateNativeFogObserver(world, session, 1, new float3(100, 0, 100));
            PlaceFixtureContact(em, contact, new float3(105, 0, 100));
            AdvanceNativeFog(world);
            AssertDirectObservation(em, contact, 1, observer);
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
            Entity playerAir = SkirmishScenarioSpawnSystem.CreateForceMember(
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

            Entity observer = CreateNativeFogObserver(world, session, 2, new float3(100, 0, 100));
            PlaceFixtureContact(em, playerAir, new float3(105, 0, 100));
            Assert.IsFalse(SkirmishFogService.IsVisibleToFaction(em, playerAir, 2));
            AdvanceNativeFog(world);
            AssertDirectObservation(em, playerAir, 2, observer);
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
            view.Infantry = 20;
            view.RecruitAntiAir = new AriaTouchTarget { Id = 51, Available = false };
            view.AirQueueOffered = false;
            view.ReadinessEligible = true;
            view.CanBuildAirPad = true;
            view.PadPresent = false;
            view.FocusPlayer = new AriaTouchTarget { Id = 53, Available = true };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.BuildAirPad, plan.Intent);
            Assert.AreEqual(53, output.TargetId);
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
            Entity playerAir = FirstRole(em, 1, SkirmishRoleKind.TransportHeli);
            Entity enemyAir = FirstRole(em, 2, SkirmishRoleKind.TransportHeli);
            Entity enemyObserver = CreateNativeFogObserver(world, session, 2, new float3(100, 0, 100));
            Entity playerObserver = CreateNativeFogObserver(world, session, 1, new float3(200, 0, 200));
            PlaceFixtureContact(em, playerAir, new float3(105, 0, 100));
            PlaceFixtureContact(em, enemyAir, new float3(205, 0, 200));
            AdvanceNativeFog(world);
            AssertDirectObservation(em, playerAir, 2, enemyObserver);
            AssertDirectObservation(em, enemyAir, 1, playerObserver);
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

            view.PadReady = afterPad.PadReady;
            view.PadPresent = afterPad.PadPresent;
            view.ReadinessEligible = afterPad.ReadinessEligible;
            view.CanBuildAirPad = true;
            view.AirQueueOffered = afterPad.AirQueueOffered;
            view.AirPad = new AriaTouchTarget { Id = 52, Available = true };
            view.FocusPlayer = new AriaTouchTarget { Id = 53, Available = true };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.BuildAirPad, plan.Intent);
            Assert.AreEqual(53, output.TargetId);
            Assert.AreEqual(playerMaterials, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(enemyMaterials - 220, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(1, em.GetComponentData<SkirmishEnemyCapacityComponent>(session).AirLive);
            Assert.AreEqual(enemyGround, em.GetComponentData<SkirmishEnemyCapacityComponent>(session).GroundLive);
        }

        [Test]
        public void ExpandedAttackWaitsForVisibleWorldTargetAndStalledBattleHandsBack()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, Time = 1,
                AttackMode = true, SelectionVisible = true, Infantry = 12,
                EnemyHealth = 800, PlayerHealth = 800,
                Attack = new AriaTouchTarget { Id = 1, Available = true },
                FocusEnemy = new AriaTouchTarget { Id = 2, Available = true },
                EnemyBase = new AriaTouchTarget { Id = 3, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(3, output.TargetId);
            Assert.AreEqual(AriaSkirmishIntent.TargetBase, plan.Intent);

            view.EnemyBase.Available = false;
            view.Time = 10;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(2, output.TargetId);
            Assert.AreEqual(AriaSkirmishIntent.FindBase, plan.Intent);
            Assert.AreEqual(1, touch.LastObjectiveProgressAt);

            view.Time = 182;
            touch.GestureRequested = 1;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaPlayPhase.Blocked, touch.Phase);
            Assert.AreEqual(0, touch.GestureRequested);
        }

        [Test]
        public void ExpandedCatalogSwipePreservesGestureAndPlacementHasDeadline()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, Time = 1,
                Infantry = 8, CanAffordRifle = true,
                Recruit = new AriaTouchTarget { Id = 4, Available = true,
                    Drag = true, Position = new Vector2(100, 100), DragEnd = new Vector2(100, 400) }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(4, output.TargetId);
            Assert.AreEqual(1, output.Drag);
            Assert.AreEqual(view.Recruit.DragEnd, output.DragEnd);

            view.PlacementOpen = true;
            view.PlacementConfirm = new AriaTouchTarget { Id = 5, Available = true };
            view.PlacementCancel = new AriaTouchTarget { Id = 6, Available = true };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(6, output.TargetId, "An unplanned valid preview must not be committed.");
            view.Time = 27;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(6, output.TargetId);
        }

        [Test]
        public void ExpandedAssaultDoesNotFinishWhenNextPageIsCovered()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, Time = 1,
                EnemyDesignatedAlive = true, ExpandedCanNextPage = true, ExpandedPageCount = 2,
                ExpandedAssaultMask = 15, ExpandedAttackOrderMask = 15,
                ExpandedSelectedMask = 15, ExpandedStructureMask = 4,
                CanUpgradeReadiness = true,
                UpgradeReadiness = new AriaTouchTarget { Id = 10, Available = true },
                CloseDrawer = new AriaTouchTarget { Id = 11, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0, plan.AssaultIssued);
            Assert.AreEqual(AriaPlayObservationKind.Waiting, output.Kind);
            view.DrawerOpen = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(11, output.TargetId);
            Assert.AreEqual(0, plan.AssaultIssued);
            view.DrawerOpen = false;
            view.NextSquadPage = new AriaTouchTarget { Id = 12, Available = true };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(12, output.TargetId);
            Assert.AreEqual(0, plan.AssaultIssued);
            // A request is not a completed page transition. Keep the target
            // stable while the touch actuator aims and releases.
            for (int frame = 0; frame < 4; frame++)
            {
                view.Time += .2f;
                AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
                Assert.AreEqual(12, output.TargetId);
                Assert.AreEqual(0, plan.AssaultIssued);
            }
        }

        [Test]
        public void AirOpeningWaitsForDeliveryAndCommitsOneSupplyTruck()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, AirProfile = true, Time = 1,
                Infantry = 12, CanAffordRifle = true, CanAffordLogisticsTruck = true,
                Recruit = new AriaTouchTarget { Id = 71, Available = true },
                RecruitLogisticsTruck = new AriaTouchTarget { Id = 72, Available = true },
                CloseDrawer = new AriaTouchTarget { Id = 73, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(71, output.TargetId);
            view.Recruit = default;
            view.Time = 2;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaPlayObservationKind.Waiting, output.Kind);
            Assert.AreEqual(0, plan.AssaultStarted, "A transiently hidden control must not abandon recruitment.");
            view.Recruit = new AriaTouchTarget { Id = 71, Available = true };
            view.RifleRecruitPending = true;
            view.DrawerOpen = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(73, output.TargetId, "Close Build while the paid packet arrives.");
            view.DrawerOpen = false;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaPlayObservationKind.Waiting, output.Kind);
            Assert.AreEqual(0, plan.AssaultStarted);
            view.Infantry = 20;
            view.RifleRecruitPending = false;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(72, output.TargetId);
            Assert.AreEqual(1, plan.AssaultStarted);
            view.LogisticsTruckCommitted = true;
            view.DrawerOpen = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(73, output.TargetId, "A pending truck is already committed.");
        }

        [Test]
        public void AirSupplyProgressIsBoundedAndRepeatedMilestonesDoNotMaskAStall()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, AirProfile = true, Time = 1,
                Infantry = 20, LogisticsTruckCommitted = true, OwnMaterials = 100
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            view.Time = 150; view.OwnMaterials = 120;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(150, plan.LastProgressAt);
            view.Time = 200; view.OwnMaterials = 100;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            view.Time = 220; view.OwnMaterials = 120;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(150, plan.LastProgressAt, "Earning previously spent stock is not new progress.");
            view.Time = 250; view.OwnMaterials = 300;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(250, plan.LastProgressAt);
            view.Time = 431; view.OwnMaterials = 400;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaPlayPhase.Blocked, touch.Phase, "Saving beyond the aircraft goal must not hide a stall.");
        }

        [Test]
        public void SuccessfulPagingClearsTapRetriesWithoutExtendingBattleWatchdog()
        {
            var view = new AriaSkirmishObservation
            { Active = true, ExpandedSession = true, Infantry = 20, Time = 10, ExpandedPageIndex = 1 };
            var plan = new AriaSkirmishPlanComponent { LastProgressAt = 1, Infantry = 20 };
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing, Attempts = 3 };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0, touch.Attempts);
            Assert.AreEqual(1, plan.LastProgressAt);
            touch.Attempts = 2;
            view.Time = 11;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(2, touch.Attempts, "An unchanged page cannot erase failed taps.");
            view.ExpandedPageIndex = 0; view.Time = 182;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaPlayPhase.Blocked, touch.Phase, "Paging must not keep a stagnant battle alive.");
        }

        [Test]
        public void AirPadPlanningWaitsUntilThePublicCardIsAffordable()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, AirProfile = true, Infantry = 20,
                ReadinessEligible = true, AirPad = new AriaTouchTarget { Id = 82, Available = true },
                FocusPlayer = new AriaTouchTarget { Id = 83, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaPlayObservationKind.Waiting, output.Kind);
            view.CanBuildAirPad = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(83, output.TargetId, "Home focus precedes the build card.");
            touch.Actions++; view.Time = 1;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            view.Time = 2;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(82, output.TargetId);
        }

        [Test]
        public void HomePadRequiresCompletedSiteTouchAndFreshLegalFeedback()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, Time = 1, Frame = 10,
                ReadinessEligible = true, CanBuildAirPad = true,
                FocusPlayer = new AriaTouchTarget { Id = 10, Available = true },
                AirPad = new AriaTouchTarget { Id = 11, Available = true },
                PlacementConfirm = new AriaTouchTarget { Id = 12, Available = true },
                PlacementCancel = new AriaTouchTarget { Id = 13, Available = true },
                PadSite0 = new AriaTouchTarget { Id = 14, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            view.PlacementOpen = true; // The initially valid preview is enemy-side.
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(13, output.TargetId);
            view.PlacementOpen = false;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(10, output.TargetId);
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreNotEqual(12, output.TargetId);
            touch.Actions = 1; touch.TargetId = 10; view.Time = 2;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreNotEqual(12, output.TargetId, "Focus alone cannot authorize Confirm.");
            view.Time = 3; view.Frame = 11;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(11, output.TargetId);
            touch.Actions = 2; touch.TargetId = 11; view.PlacementOpen = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(14, output.TargetId);
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreNotEqual(12, output.TargetId, "An incomplete site touch cannot authorize Confirm.");
            touch.Actions = 3; touch.TargetId = 14;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreNotEqual(12, output.TargetId);
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreNotEqual(12, output.TargetId, "Feedback from the same frame is stale.");
            view.Frame = 12;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(12, output.TargetId);
            touch.Actions = 4; touch.TargetId = 12;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreNotEqual(12, output.TargetId, "A completed Confirm is never sent twice.");
        }

        [Test]
        public void HomePadCancelsWhenAllVisibleSitesAreBlocked()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, Time = 1, Frame = 10,
                ReadinessEligible = true, CanBuildAirPad = true,
                FocusPlayer = new AriaTouchTarget { Id = 10, Available = true },
                AirPad = new AriaTouchTarget { Id = 11, Available = true },
                PlacementConfirm = new AriaTouchTarget { Id = 12, Available = true },
                PlacementCancel = new AriaTouchTarget { Id = 13, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            touch.Actions = 1; touch.TargetId = 10; view.Time = 2;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            view.Time = 3;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(11, output.TargetId);
            touch.Actions = 2; touch.TargetId = 11; view.PlacementOpen = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(10, output.TargetId, "A single refocus is allowed.");
            touch.Actions = 3; touch.TargetId = 10;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(13, output.TargetId);
            Assert.AreEqual(8, plan.PadStage);
        }

        [Test]
        public void PadAffordabilityUsesPersistentAuthoredCatalogWithoutADrawer()
        {
            using var world = new World(nameof(PadAffordabilityUsesPersistentAuthoredCatalogWithoutADrawer));
            var em = world.EntityManager;
            CompileAndSpawnS003(em, out var session, out _);
            var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var readiness = new SkirmishResearchStateComponent { Readiness = SkirmishReadinessStage.Established };
            if (em.HasComponent<SkirmishResearchStateComponent>(session)) em.SetComponentData(session, readiness);
            else em.AddComponentData(session, readiness);
            var boundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
            var catalog = em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
            int balance = SkirmishMaterialsService.Read(em, session, 1);
            var item = new BuildingConfiguredSpawnableReadModel
            { BuildingId = "Building_Helipad", MaterialsCost = balance + 1, CanRequest = 1 };
            catalog.Add(item);
            Assert.IsFalse(SkirmishAriaPublicProjection.FromSession(em, session, authored.ArmyAir).CanBuildAirPad);
            item.MaterialsCost = balance; catalog[0] = item;
            Assert.IsTrue(SkirmishAriaPublicProjection.FromSession(em, session, authored.ArmyAir).CanBuildAirPad);
            item.CanRequest = 0; catalog[0] = item;
            Assert.IsFalse(SkirmishAriaPublicProjection.FromSession(em, session, authored.ArmyAir).CanBuildAirPad);
            em.DestroyEntity(boundary);
            Assert.IsFalse(SkirmishAriaPublicProjection.FromSession(em, session, authored.ArmyAir).CanBuildAirPad);
        }

        [Test]
        public void StrikeStatusIsPresentedToTheSharedAircraftReturnAndServicePlan()
        {
            using var world = new World(nameof(StrikeStatusIsPresentedToTheSharedAircraftReturnAndServicePlan));
            var em = world.EntityManager;
            CompileAndSpawnS003(em, out var session, out _);
            var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var baseline = SkirmishAriaPublicProjection.FromSession(em, session, authored.ArmyAir);
            var actor = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent),
                typeof(UnitHealth), typeof(UnitAirComponent));
            em.SetComponentData(actor, new SkirmishAttemptOwnedComponent
            { SessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId, FactionId = 1 });
            em.SetComponentData(actor, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Strike });
            em.SetComponentData(actor, new UnitHealth { Current = 300, Max = 300 });
            em.SetComponentData(actor, new UnitAirComponent { HomeInitialized = 1 });
            var landed = SkirmishAriaPublicProjection.FromSession(em, session, authored.ArmyAir);
            Assert.AreEqual(baseline.OwnAttackAirLive + 1, landed.OwnAttackAirLive);
            Assert.AreEqual(baseline.OwnAttackAirLanded + 1, landed.OwnAttackAirLanded);
            em.SetComponentData(actor, new UnitAirComponent { HomeInitialized = 1, Airborne = 1 });
            var active = SkirmishAriaPublicProjection.FromSession(em, session, authored.ArmyAir);
            Assert.AreEqual(baseline.OwnAttackAirActive + 1, active.OwnAttackAirActive);
            em.SetComponentData(actor, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.TransportPlane });
            Assert.AreEqual(baseline.OwnAttackAirLive,
                SkirmishAriaPublicProjection.FromSession(em, session, authored.ArmyAir).OwnAttackAirLive);
        }

        [Test]
        public void AirSupplyBuysTwoPresentedTankersBeforeResearchWithoutStarvingTheAssault()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, AirProfile = true, CombinedArmsField = true,
                Infantry = 20, OwnStructureAttackUnits = 8, LogisticsTruckCommitted = true,
                CanAffordTanker = true, CanUpgradeReadiness = true, Time = 10,
                PlayerHealth = 800, EnemyHealth = 800,
                RecruitTanker = new AriaTouchTarget { Id = 601, Available = true },
                UpgradeReadiness = new AriaTouchTarget { Id = 604, Available = true },
                CanAffordRocketeer = true,
                RecruitRocketeer = new AriaTouchTarget { Id = 602, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, AssaultIssued = 1 };
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(601, output.TargetId);
            view.TankersCommitted = 1;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(601, output.TargetId, "One pending/live Tanker still allows the second paid delivery.");
            view.TankersCommitted = 2;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(604, output.TargetId, "Two commitments end supply purchasing and allow research.");
            view.TankersCommitted = 1;
            view.CanAffordTanker = false;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(604, output.TargetId, "An unaffordable or capacity-blocked Tanker cannot be forced.");
            view.CanAffordTanker = true;
            view.OwnStructureAttackUnits = 4;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(602, output.TargetId, "Build the viable structure assault before spending its budget on Fuel transport.");
        }

        [Test]
        public void PublicTankerCommitmentsCountOwnLiveAndPendingMembersWithoutMutatingStocks()
        {
            using var world = new World(nameof(PublicTankerCommitmentsCountOwnLiveAndPendingMembersWithoutMutatingStocks));
            var em = world.EntityManager;
            CompileAndSpawnS003(em, out var session, out _);
            var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            int before = SkirmishMaterialsService.Read(em, session, 1);
            int startingTankers = SkirmishAriaPublicProjection.FromSession(em, session, authored.ArmyAir).TankersCommitted;
            Assert.AreEqual(1, startingTankers, "The compiled Field package includes its real starting Tanker.");
            var actor = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent), typeof(UnitHealth));
            em.SetComponentData(actor, new SkirmishAttemptOwnedComponent { SessionId = attempt, FactionId = 1 });
            em.SetComponentData(actor, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Tanker });
            em.SetComponentData(actor, new UnitHealth { Current = 100, Max = 100 });
            var receipts = em.GetBuffer<SkirmishProductionReservation>(session);
            receipts.Add(new SkirmishProductionReservation
            { ReservationId = 901, FactionId = 1, Role = SkirmishRoleKind.Tanker, MemberCount = 2,
                DeliveredMembers = 1, Phase = SkirmishReservationPhase.Producing });
            receipts.Add(new SkirmishProductionReservation
            { ReservationId = 902, FactionId = 2, Role = SkirmishRoleKind.Tanker, MemberCount = 1,
                Phase = SkirmishReservationPhase.Producing });
            Assert.AreEqual(startingTankers + 2, SkirmishAriaPublicProjection.FromSession(em, session, authored.ArmyAir).TankersCommitted);
            Assert.AreEqual(before, SkirmishMaterialsService.Read(em, session, 1));
            em.SetComponentData(actor, new UnitHealth { Current = 0, Max = 100 });
            Assert.AreEqual(startingTankers + 1, SkirmishAriaPublicProjection.FromSession(em, session, authored.ArmyAir).TankersCommitted,
                "A lost Tanker must not suppress replacement recruitment.");
            em.SetComponentData(actor, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(actor, new SkirmishAttemptOwnedComponent { SessionId = "different-attempt", FactionId = 1 });
            Assert.AreEqual(startingTankers + 1, SkirmishAriaPublicProjection.FromSession(em, session, authored.ArmyAir).TankersCommitted);
        }

        [Test]
        public void VisibleAssaultAdvanceKeepsLongMarchActiveButAStoppedForceStillBlocks()
        {
            var view = new AriaSkirmishObservation
            { Active = true, ExpandedSession = true, Infantry = 20, Time = 10,
                AssaultDistanceVisible = true, NearestFriendlyObjectiveDistance = 400 };
            var plan = new AriaSkirmishPlanComponent { LastProgressAt = 1, Infantry = 20, AssaultIssued = 1 };
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(1, plan.LastProgressAt, "First observation establishes a baseline, not progress.");
            view.Time = 175; view.NearestFriendlyObjectiveDistance = 250;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(175, plan.LastProgressAt);
            view.Time = 340; view.NearestFriendlyObjectiveDistance = 80;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(340, plan.LastProgressAt);
            Assert.AreNotEqual(AriaPlayPhase.Blocked, touch.Phase);
            view.Time = 521;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaPlayPhase.Blocked, touch.Phase);
        }

        [Test]
        public void AssaultPagingJitterAndRetreatDoNotRenewTheBestApproach()
        {
            var view = new AriaSkirmishObservation
            { Active = true, ExpandedSession = true, Infantry = 20, Time = 10,
                AssaultDistanceVisible = true, NearestFriendlyObjectiveDistance = 100 };
            var plan = new AriaSkirmishPlanComponent { LastProgressAt = 1, Infantry = 20, AssaultIssued = 1 };
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            foreach (float distance in new[] { 99f, 140f, 100f, 99f })
            {
                view.Time += 30; view.ExpandedPageIndex = 1 - view.ExpandedPageIndex;
                view.NearestFriendlyObjectiveDistance = distance;
                AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            }
            Assert.AreEqual(1, plan.LastProgressAt);
            Assert.AreEqual(100, plan.BestAssaultDistance);
            view.Time = 182;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaPlayPhase.Blocked, touch.Phase);
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedAriaTests();
                suite.VisibleAssaultAdvanceKeepsLongMarchActiveButAStoppedForceStillBlocks();
                suite.AssaultPagingJitterAndRetreatDoNotRenewTheBestApproach();
                suite.StrikeStatusIsPresentedToTheSharedAircraftReturnAndServicePlan();
                suite.AirSupplyBuysTwoPresentedTankersBeforeResearchWithoutStarvingTheAssault();
                suite.PublicTankerCommitmentsCountOwnLiveAndPendingMembersWithoutMutatingStocks();
                suite.AirOpeningUsesVisibleDefenseControlsBeforeCommittingTheAssault(false);
                suite.AirOpeningUsesVisibleDefenseControlsBeforeCommittingTheAssault(true);
                suite.CombinedArmsPreservesStructureForceBeforeBuyingRifles();
                suite.LegacyDefenseConfirmationDoesNotWaitForUnavailableBuildingCensus();
                suite.PresentedHomeRaidUsesSquadAttackAndResumesObjectiveOnlyAfterItClears();
                suite.FieldArmyReservesPartOfItsStartingForceInsteadOfUnfilledCapacity();
                suite.SharedScoringUsesEligibilityAndHidesHostileCash();
                suite.EnemyStrategyRecruitsThroughSharedProduceAndLeavesPlayerStocks();
                suite.EnemyAttackRequiresVisibleTargetAndUsesLegalGroupOrder();
                suite.EnemyStructureDefendersReactToVisibleUnarmedIntrusionAndResumeBaseObjective();
                suite.AircraftReturnWaitsForTouchAndLandingBeforeSecondSortie();
                suite.AriaSkillsStayOnVisibleControlsAndHandBackAfterRetries();
                suite.ExpandedAriaPlanTargetsPublicControlsWithoutGameplayMutation();
                suite.PublicProjectionDoesNotExposeEnemyWalletToAria();
                suite.S003AirMobilePublicControlsDoNotMutateGameplay();
                suite.S004EstablishedPadReadyDoesNotMutatePlayerStocks();
                suite.ExpandedAttackWaitsForVisibleWorldTargetAndStalledBattleHandsBack();
                suite.ExpandedCatalogSwipePreservesGestureAndPlacementHasDeadline();
                suite.ExpandedAssaultDoesNotFinishWhenNextPageIsCovered();
                suite.AirOpeningWaitsForDeliveryAndCommitsOneSupplyTruck();
                suite.AirSupplyProgressIsBoundedAndRepeatedMilestonesDoNotMaskAStall();
                suite.SuccessfulPagingClearsTapRetriesWithoutExtendingBattleWatchdog();
                suite.AirPadPlanningWaitsUntilThePublicCardIsAffordable();
                suite.HomePadRequiresCompletedSiteTouchAndFreshLegalFeedback();
                suite.HomePadCancelsWhenAllVisibleSitesAreBlocked();
                suite.PadAffordabilityUsesPersistentAuthoredCatalogWithoutADrawer();
                SkirmishS002AriaHarnessTests.RunFocusedValidation();
                Debug.Log("[SkirmishExpandedAriaTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishExpandedAriaTests] result=Failed\n" + exception);
                throw;
            }
        }

        // These isolated tests spawn ledgers without baked acquisition/pose components.
        // Supply fixture observer geometry and drive the actual positive-time fog system;
        // never write sight/knowledge, borrow the player's view, or enable debug full vision.
        private static void EnsureNativeFogFixture(World world, Entity session)
        {
            var em = world.EntityManager;
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.IsEmptyIgnoreFilter)
                em.AddComponentData(em.CreateEntity(), new GridConfig { Width = 4096, Height = 4096, CellSize = 1 });
            Assert.AreEqual(1, grids.CalculateEntityCount());
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            state.Phase = SkirmishSessionPhase.Playing;
            em.SetComponentData(session, state);
            var fog = em.GetComponentData<SkirmishFogStateComponent>(session);
            Assert.AreEqual(1, fog.SharedFog);
            Assert.AreEqual(0, fog.DevelopmentFullVision);
        }

        private static Entity CreateNativeFogObserver(World world, Entity session, byte faction, float3 position)
        {
            EnsureNativeFogFixture(world, session);
            var em = world.EntityManager;
            Entity observer = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(Faction), typeof(UnitHealth), typeof(UnitCombat));
            em.SetComponentData(observer, new SkirmishAttemptOwnedComponent
            { SessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                StableObjectId = new FixedString64Bytes("aria.fixture.observer." + faction + "." + observer.Index), FactionId = faction });
            em.SetComponentData(observer, new Faction { Id = faction });
            em.SetComponentData(observer, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(observer, new UnitCombat { AggroRangeCells = 20 });
            PlaceFixtureContact(em, observer, position);
            return observer;
        }

        private static void PlaceFixtureContact(EntityManager em, Entity actor, float3 position)
        {
            Assert.AreNotEqual(Entity.Null, actor);
            FixtureSet(em, actor, LocalTransform.FromPosition(position));
            FixtureSet(em, actor, new UnitGrid { Cell = new int2((int)position.x, (int)position.z) });
        }

        private static void FixtureSet<T>(EntityManager em, Entity actor, T component) where T : unmanaged, IComponentData
        { if (em.HasComponent<T>(actor)) em.SetComponentData(actor, component); else em.AddComponentData(actor, component); }

        private static void AdvanceNativeFog(World world)
        {
            world.SetTime(new TimeData(world.Unmanaged.Time.ElapsedTime + 1, .1f));
            world.GetOrCreateSystem<SkirmishSharedFogUpdateSystem>().Update(world.Unmanaged);
            world.EntityManager.CompleteAllTrackedJobs();
        }

        private static void AssertDirectObservation(EntityManager em, Entity contact, byte faction, Entity observer)
        {
            Assert.IsTrue(SkirmishFogService.IsVisibleToFaction(em, contact, faction));
            Assert.IsTrue(SkirmishFogService.TryGetKnowledge(em, contact, faction, out var knowledge));
            Assert.AreEqual(SkirmishContactSight.Visible, knowledge.Sight);
            Assert.AreEqual(1, knowledge.ObservationKind);
            Assert.AreEqual(observer, knowledge.ObservationSource);
        }

        private static Entity FirstRole(EntityManager em, byte faction, SkirmishRoleKind role)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).FactionId == faction &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role == role) return actor;
            Assert.Fail("Compiled fixture has no " + faction + "/" + role);
            return Entity.Null;
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
