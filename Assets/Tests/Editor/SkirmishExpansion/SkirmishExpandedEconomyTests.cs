using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishExpandedEconomyTests
    {
        [Test]
        public void S002StartingStocksAndCapsSeedFromMatrix()
        {
            using var world = new World(nameof(S002StartingStocksAndCapsSeedFromMatrix));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out SkirmishResolvedSetup setup);
            var stock = em.GetComponentData<SkirmishEconomyStockComponent>(session);
            Assert.AreEqual(900, stock.Materials);
            Assert.AreEqual(240, stock.Oil);
            Assert.AreEqual(700, stock.Fuel);
            var capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(48, capacity.InfantryCap);
            Assert.AreEqual(8, capacity.GroundCap);
            Assert.AreEqual(128, capacity.SupplyCap);
            Assert.AreEqual(setup.PlayerInfantry, capacity.InfantryLive);
            Assert.AreEqual(setup.PlayerGround, capacity.GroundLive);
            Assert.AreEqual(0, capacity.InfantryReserved);
        }

        [Test]
        public void CapacityLedgerReservePromoteReleaseOnce()
        {
            var capacity = new SkirmishCapacitySnapshot
            {
                GroundCap = 8,
                SupplyCap = 128
            };
            Assert.IsTrue(SkirmishCapacityLedger.TryReserve(
                ref capacity, SkirmishPopulationCategory.Ground, 1, 6));
            Assert.AreEqual(1, capacity.GroundReserved);
            Assert.AreEqual(6, capacity.SupplyReserved);
            Assert.IsTrue(SkirmishCapacityLedger.TryReleaseReserved(
                ref capacity, SkirmishPopulationCategory.Ground, 1, 6));
            Assert.AreEqual(0, capacity.GroundReserved);
            Assert.IsTrue(SkirmishCapacityLedger.TryReserve(
                ref capacity, SkirmishPopulationCategory.Ground, 1, 6));
            Assert.IsFalse(SkirmishCapacityLedger.TryReserve(
                ref capacity, SkirmishPopulationCategory.Ground, 8, 48));
            SkirmishCapacityLedger.PromoteLive(ref capacity, SkirmishPopulationCategory.Ground, 1, 6);
            Assert.AreEqual(1, capacity.GroundLive);
            Assert.AreEqual(0, capacity.GroundReserved);
            Assert.IsTrue(SkirmishCapacityLedger.TryReleaseDeath(
                ref capacity, SkirmishPopulationCategory.Ground, 6));
            Assert.IsFalse(SkirmishCapacityLedger.TryReleaseDeath(
                ref capacity, SkirmishPopulationCategory.Ground, 6));
            Assert.AreEqual(0, capacity.GroundLive);
            Assert.AreEqual(0, capacity.SupplyLive);
        }

        [Test]
        public void GroundStagingProducesTankThenDeathReleasesOnce()
        {
            using var world = new World(nameof(GroundStagingProducesTankThenDeathReleasesOnce));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var before = em.GetComponentData<SkirmishCapacityComponent>(session);
            int materials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision tank));
            Assert.AreEqual(360, tank.MaterialsCost);
            Assert.AreEqual(0, tank.FuelCost);
            Assert.AreEqual(6, tank.SupplyCost);
            Assert.AreEqual(1, tank.MemberCount);
            Assert.AreEqual(SkirmishProducerKind.GroundStaging, tank.Producer);
            Assert.AreEqual(materials - 360, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(700, em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel);
            var after = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(before.GroundLive + 1, after.GroundLive);
            Assert.AreEqual(before.SupplyLive + 6, after.SupplyLive);
            Assert.AreEqual(0, after.GroundReserved);
            Assert.AreEqual(SkirmishReservationPhase.Live, ReservationPhase(em, session, tank.ReservationId));

            Entity produced = FindReservedUnit(em, tank.ReservationId);
            Assert.AreNotEqual(Entity.Null, produced);
            Assert.AreEqual("Unit_Veh_Tank_USA", em.GetComponentData<UnitSourcePrefabKey>(produced).Value.ToString());
            var health = em.GetComponentData<UnitHealth>(produced);
            health.Current = 0;
            em.SetComponentData(produced, health);
            Assert.IsTrue(SkirmishProductionService.TryReleaseDeath(em, session, produced));
            Assert.IsFalse(SkirmishProductionService.TryReleaseDeath(em, session, produced));
            var released = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(before.GroundLive, released.GroundLive);
            Assert.AreEqual(before.SupplyLive, released.SupplyLive);
            Assert.AreEqual(SkirmishReservationPhase.Released, ReservationPhase(em, session, tank.ReservationId));
        }

        [Test]
        public void CapacityLifecycleReleasesDeadOwnedUnitsOnce()
        {
            using var world = new World(nameof(CapacityLifecycleReleasesDeadOwnedUnitsOnce));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var before = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision tank));
            Entity produced = FindReservedUnit(em, tank.ReservationId);
            var health = em.GetComponentData<UnitHealth>(produced);
            health.Current = 0;
            em.SetComponentData(produced, health);
            using var owned = em.CreateEntityQuery(
                ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),
                ComponentType.ReadOnly<SkirmishUnitRoleComponent>(),
                ComponentType.ReadOnly<UnitHealth>());
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            Assert.AreEqual(1, SkirmishCapacityLifecycleSystem.ReleaseDead(em, session, owned, sessionId));
            Assert.AreEqual(0, SkirmishCapacityLifecycleSystem.ReleaseDead(em, session, owned, sessionId));
            Assert.AreEqual(before.GroundLive, em.GetComponentData<SkirmishCapacityComponent>(session).GroundLive);
            Assert.AreEqual(SkirmishReservationPhase.Released, ReservationPhase(em, session, tank.ReservationId));
        }

        [Test]
        public void InfantrySquadCostsFourMembersAndMaterials()
        {
            using var world = new World(nameof(InfantrySquadCostsFourMembersAndMaterials));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var before = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Rifle, 1, authored.ArmyGround, out SkirmishProductionDecision rifle));
            Assert.AreEqual(4, rifle.MemberCount);
            Assert.AreEqual(80, rifle.MaterialsCost);
            Assert.AreEqual(before.InfantryLive + 4, em.GetComponentData<SkirmishCapacityComponent>(session).InfantryLive);
        }

        [Test]
        public void MissingMaterialsAndCapsRejectWithoutMutation()
        {
            using var world = new World(nameof(MissingMaterialsAndCapsRejectWithoutMutation));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var stock = em.GetComponentData<SkirmishEconomyStockComponent>(session);
            stock.Materials = 10;
            em.SetComponentData(session, stock);
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision poor));
            Assert.AreEqual(SkirmishReasonCode.InsufficientMaterials, poor.Reason);
            Assert.AreEqual(10, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            stock.Materials = 900;
            em.SetComponentData(session, stock);
            var capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
            capacity.GroundLive = capacity.GroundCap;
            em.SetComponentData(session, capacity);
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision full));
            Assert.AreEqual(SkirmishReasonCode.InsufficientCapacity, full.Reason);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
        }

        [Test]
        public void FuelGateRejectsWithoutStockMutation()
        {
            SkirmishRoleOverlay[] overlays = SkirmishRoleOverlayCatalog.CreateS002GroundSlice();
            for (int i = 0; i < overlays.Length; i++)
            {
                if (overlays[i].RoleKind != SkirmishRoleKind.Tank)
                    continue;
                overlays[i].FuelCost = 40;
                break;
            }

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            SkirmishProductionDecision dry = SkirmishProductionEligibility.Evaluate(
                new SkirmishProductionRequest
                {
                    RoleId = SkirmishRoleIds.Tank,
                    RoleKind = SkirmishRoleKind.Tank,
                    SquadCount = 1,
                    BarracksPresent = true,
                    GroundStagingPresent = true,
                    EnforceStocks = true,
                    MaterialsAvailable = 900,
                    FuelAvailable = 0,
                    GroundCap = 8,
                    SupplyCap = 128
                },
                authored.ArmyGround,
                SkirmishReadinessStage.Established,
                overlays);
            Assert.IsFalse(dry.Accepted);
            Assert.AreEqual(SkirmishReasonCode.InsufficientFuel, dry.Reason);
            Assert.AreEqual(40, dry.FuelCost);
        }

        [Test]
        public void RecruitmentFuelCostStaysZeroOnS002Overlays()
        {
            SkirmishRoleOverlay[] overlays = SkirmishRoleOverlayCatalog.CreateS002GroundSlice();
            for (int i = 0; i < overlays.Length; i++)
                Assert.AreEqual(0, overlays[i].FuelCost, overlays[i].RoleId);
        }

        [Test]
        public void MidProduceCancelRefundsReservedThenSeventyFivePercent()
        {
            using var world = new World(nameof(MidProduceCancelRefundsReservedThenSeventyFivePercent));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision queued));
            Assert.AreEqual(SkirmishReservationPhase.Reserved, ReservationPhase(em, session, queued.ReservationId));
            Assert.AreEqual(540, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).GroundReserved);
            Assert.IsTrue(SkirmishProductionService.TryCancel(
                em, session, queued.ReservationId, out SkirmishProductionDecision fullRefund));
            Assert.AreEqual(360, fullRefund.RefundedMaterials);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).GroundReserved);
            Assert.AreEqual(SkirmishReservationPhase.Cancelled, ReservationPhase(em, session, queued.ReservationId));

            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision started));
            Assert.IsTrue(SkirmishProductionService.TryStartQueued(em, session, started.ReservationId, out _));
            Assert.AreEqual(SkirmishReservationPhase.Producing, ReservationPhase(em, session, started.ReservationId));
            Assert.IsTrue(SkirmishProductionService.IsProducerLocked(em, session, SkirmishProducerKind.GroundStaging, 1));
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Car, 1, authored.ArmyGround, out SkirmishProductionDecision locked));
            Assert.AreEqual(SkirmishReasonCode.QueueLocked, locked.Reason);
            Assert.IsTrue(SkirmishProductionService.TryCancel(
                em, session, started.ReservationId, out SkirmishProductionDecision partial));
            Assert.AreEqual(270, partial.RefundedMaterials);
            Assert.AreEqual(810, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.IsFalse(SkirmishProductionService.IsProducerLocked(em, session, SkirmishProducerKind.GroundStaging, 1));
        }

        [Test]
        public void FailedDispatchAndProducerDestructionUseSpecifiedRefunds()
        {
            using var world = new World(nameof(FailedDispatchAndProducerDestructionUseSpecifiedRefunds));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision failed));
            Assert.IsTrue(SkirmishProductionService.TryRefundFailedDispatch(
                em, session, failed.ReservationId, out SkirmishProductionDecision spawnRefund));
            Assert.AreEqual(360, spawnRefund.RefundedMaterials);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision producing));
            Assert.IsTrue(SkirmishProductionService.TryStartQueued(em, session, producing.ReservationId, out _));
            Assert.AreEqual(1, SkirmishProductionService.NotifyProducerDestroyed(
                em, session, SkirmishProducerKind.GroundStaging, 1));
            Assert.AreEqual(SkirmishReservationPhase.Lost, ReservationPhase(em, session, producing.ReservationId));
            Assert.AreEqual(540, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).GroundReserved);

            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision live));
            Assert.IsFalse(SkirmishProductionService.TryCancel(em, session, live.ReservationId, out SkirmishProductionDecision launched));
            Assert.AreEqual(SkirmishReasonCode.NotCancellable, launched.Reason);
        }

        [Test]
        public void CategoryResearchFromStagingAndHqAppliesOnce()
        {
            using var world = new World(nameof(CategoryResearchFromStagingAndHqAppliesOnce));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            var research = em.GetComponentData<SkirmishResearchStateComponent>(session);
            Assert.AreEqual(SkirmishReadinessStage.Established, research.Readiness);
            Assert.AreEqual(0, research.VehicleProtection);
            Assert.AreEqual(0, research.InfantryWeapons);

            Entity tank = FirstRole(em, SkirmishRoleKind.Tank, 1);
            var health = em.GetComponentData<UnitHealth>(tank);
            health.Current = health.Max / 2;
            em.SetComponentData(tank, health);
            int half = health.Current;
            int baseMax = health.Max;
            Entity rifle = FirstRole(em, SkirmishRoleKind.Rifle, 1);
            int rifleDamage = em.GetComponentData<SkirmishRoleOverlayComponent>(rifle).Damage;

            Assert.IsTrue(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.VehicleProtection, 1, out SkirmishResearchDecision vehicle));
            Assert.AreEqual(200, vehicle.MaterialsCost);
            Assert.AreEqual(700, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, vehicle.ResearchId, out _));
            Assert.IsTrue(SkirmishProductionService.IsProducerLocked(em, session, SkirmishProducerKind.GroundStaging, 1));
            Assert.IsTrue(SkirmishResearchService.TryComplete(em, session, vehicle.ResearchId, out SkirmishResearchDecision done));
            Assert.AreEqual(1, done.LevelAfter);
            Assert.AreEqual(1, em.GetComponentData<SkirmishResearchStateComponent>(session).VehicleProtection);
            var upgraded = em.GetComponentData<UnitHealth>(tank);
            Assert.AreEqual(SkirmishResearchCosts.Scale(baseMax, 110), upgraded.Max);
            Assert.AreEqual((half * upgraded.Max + baseMax / 2) / baseMax, upgraded.Current);
            Assert.AreNotEqual(upgraded.Max, upgraded.Current);

            Assert.IsFalse(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.VehicleProtection, 1, out SkirmishResearchDecision again));
            Assert.AreEqual(SkirmishReasonCode.AlreadyCompleted, again.Reason);
            Assert.IsFalse(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.AircraftEfficiency, 1, out SkirmishResearchDecision air));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, air.Reason);

            Assert.IsTrue(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.InfantryWeapons, 1, out SkirmishResearchDecision infantry));
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, infantry.ResearchId, out _));
            Assert.IsTrue(SkirmishResearchService.TryComplete(em, session, infantry.ResearchId, out _));
            Assert.AreEqual(
                SkirmishResearchCosts.Scale(rifleDamage, 110),
                em.GetComponentData<SkirmishRoleOverlayComponent>(rifle).Damage);

            Assert.IsTrue(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.Readiness, 1, out SkirmishResearchDecision r3));
            Assert.AreEqual(480, r3.MaterialsCost);
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, r3.ResearchId, out _));
            Assert.IsTrue(SkirmishResearchService.TryComplete(em, session, r3.ResearchId, out SkirmishResearchDecision arsenal));
            Assert.AreEqual(SkirmishReadinessStage.FullArsenal, arsenal.ReadinessAfter);
        }

        [Test]
        public void ReplacementHqCanResearchWithoutBecomingVictoryBase()
        {
            using var world = new World(nameof(ReplacementHqCanResearchWithoutBecomingVictoryBase));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            Entity designated = FirstDesignated(em, SkirmishObjectiveRoleKind.PlayerBase);
            Assert.AreNotEqual(Entity.Null, designated);
            var designatedHealth = em.GetComponentData<UnitHealth>(designated);
            designatedHealth.Current = 0;
            em.SetComponentData(designated, designatedHealth);
            Assert.IsFalse(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.InfantryWeapons, 1, out SkirmishResearchDecision missing));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, missing.Reason);

            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
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
            em.AddComponentData(replacement, new UnitHealth { Current = 800, Max = 800 });
            Assert.IsFalse(em.HasComponent<SkirmishObjectiveRoleComponent>(replacement));
            Assert.AreEqual(0, em.GetComponentData<SkirmishStructureIdentityComponent>(replacement).DesignatedBase);
            Assert.IsTrue(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.InfantryWeapons, 1, out SkirmishResearchDecision fromReplacement));
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, fromReplacement.ResearchId, out _));
            Assert.IsTrue(SkirmishResearchService.TryCancel(em, session, fromReplacement.ResearchId, out SkirmishResearchDecision cancelled));
            Assert.AreEqual(150, cancelled.RefundedMaterials);

            Assert.IsTrue(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.InfantryWeapons, 1, out SkirmishResearchDecision researching));
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, researching.ResearchId, out _));
            em.SetComponentData(replacement, new UnitHealth { Current = 0, Max = 800 });
            Assert.AreEqual(1, SkirmishResearchService.NotifyProducerDestroyed(
                em, session, SkirmishProducerKind.Barracks, 1));
            Assert.AreEqual(SkirmishResearchPhase.Lost, ResearchPhase(em, session, researching.ResearchId));
            Assert.AreEqual(0, em.GetComponentData<SkirmishResearchStateComponent>(session).InfantryWeapons);
        }

        [Test]
        public void S003AirMobileAaGatePadReadinessAndMissingPadRefund()
        {
            using var world = new World(nameof(S003AirMobileAaGatePadReadinessAndMissingPadRefund));
            EntityManager em = world.EntityManager;
            CompileAndSpawnS003(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.AreEqual(450, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(350, em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel);
            Assert.AreEqual(SkirmishReadinessStage.Field, em.GetComponentData<SkirmishResearchStateComponent>(session).Readiness);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(2, em.GetComponentData<SkirmishCapacityComponent>(session).AirCap);

            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyAir, out SkirmishProductionDecision tank));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, tank.Reason);
            Assert.AreEqual(450, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            Assert.IsFalse(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision early));
            Assert.AreEqual(SkirmishReasonCode.MissingReadiness, early.Reason);
            Assert.AreEqual(450, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);

            var research = em.GetComponentData<SkirmishResearchStateComponent>(session);
            research.Readiness = SkirmishReadinessStage.Established;
            em.SetComponentData(session, research);
            Assert.IsFalse(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision missingPad));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, missingPad.Reason);
            Assert.AreEqual("producer.helipad", missingPad.Field);
            Assert.AreEqual(450, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            Entity pad = SkirmishScenarioSpawnSystem.CreateStructure(
                em,
                sessionId,
                new SkirmishResolvedStructureEntry
                {
                    FactionId = 1,
                    StructureId = SkirmishStructureIds.Helipad
                });
            em.AddComponentData(pad, new UnitHealth { Current = 400, Max = 400 });

            var capped = em.GetComponentData<SkirmishCapacityComponent>(session);
            int airLive = capped.AirLive;
            capped.AirLive = capped.AirCap;
            em.SetComponentData(session, capped);
            Assert.IsFalse(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision full));
            Assert.AreEqual(SkirmishReasonCode.InsufficientCapacity, full.Reason);
            Assert.AreEqual(450, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            capped = em.GetComponentData<SkirmishCapacityComponent>(session);
            capped.AirLive = airLive;
            em.SetComponentData(session, capped);

            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision queued));
            Assert.AreEqual(420, queued.MaterialsCost);
            Assert.AreEqual(8, queued.SupplyCost);
            Assert.AreEqual(SkirmishProducerKind.Helipad, queued.Producer);
            Assert.AreEqual(30, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.IsTrue(SkirmishProductionService.TryStartQueued(em, session, queued.ReservationId, out _));
            Assert.AreEqual(SkirmishReservationPhase.Producing, ReservationPhase(em, session, queued.ReservationId));

            em.SetComponentData(pad, new UnitHealth { Current = 0, Max = 400 });
            Assert.IsFalse(SkirmishProductionService.TryDispatchQueued(
                em, session, queued.ReservationId, authored.ArmyAir, out SkirmishProductionDecision refunded));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, refunded.Reason);
            Assert.AreEqual("producer.helipad", refunded.Field);
            Assert.AreEqual(420, refunded.RefundedMaterials);
            Assert.AreEqual(450, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(350, em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyReserved);
            Assert.AreEqual(SkirmishReservationPhase.Cancelled, ReservationPhase(em, session, queued.ReservationId));
            Assert.AreEqual(Entity.Null, FindReservedUnit(em, queued.ReservationId));

            var before = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.AntiAir, 1, authored.ArmyAir, out SkirmishProductionDecision aa));
            Assert.AreEqual(220, aa.MaterialsCost);
            Assert.AreEqual(6, aa.SupplyCost);
            Assert.AreEqual(SkirmishProducerKind.GroundStaging, aa.Producer);
            Assert.AreEqual(230, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            var after = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(before.GroundLive + 1, after.GroundLive);
            Assert.AreEqual(before.AirLive, after.AirLive);
            Assert.AreEqual(0, after.GroundReserved);
            Entity produced = FindReservedUnit(em, aa.ReservationId);
            Assert.AreEqual(
                "Unit_Veh_Missle_Launcher_Air",
                em.GetComponentData<UnitSourcePrefabKey>(produced).Value.ToString());
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedEconomyTests();
                suite.S002StartingStocksAndCapsSeedFromMatrix();
                suite.CapacityLedgerReservePromoteReleaseOnce();
                suite.GroundStagingProducesTankThenDeathReleasesOnce();
                suite.CapacityLifecycleReleasesDeadOwnedUnitsOnce();
                suite.InfantrySquadCostsFourMembersAndMaterials();
                suite.MissingMaterialsAndCapsRejectWithoutMutation();
                suite.FuelGateRejectsWithoutStockMutation();
                suite.RecruitmentFuelCostStaysZeroOnS002Overlays();
                suite.MidProduceCancelRefundsReservedThenSeventyFivePercent();
                suite.FailedDispatchAndProducerDestructionUseSpecifiedRefunds();
                suite.CategoryResearchFromStagingAndHqAppliesOnce();
                suite.ReplacementHqCanResearchWithoutBecomingVictoryBase();
                suite.S003AirMobileAaGatePadReadinessAndMissingPadRefund();
                Debug.Log("[SkirmishExpandedEconomyTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishExpandedEconomyTests] result=Failed\n" + exception);
                throw;
            }
        }

        private static void CompileAndSpawn(
            EntityManager em,
            out Entity session,
            out SkirmishResolvedSetup setup)
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
        }

        private static void CompileAndSpawnS003(
            EntityManager em,
            out Entity session,
            out SkirmishResolvedSetup setup)
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
        }

        private static Entity FindReservedUnit(EntityManager em, uint reservationId)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).ReservationId == reservationId)
                    return entities[i];
            }

            return Entity.Null;
        }

        private static SkirmishReservationPhase ReservationPhase(
            EntityManager em,
            Entity session,
            uint reservationId)
        {
            DynamicBuffer<SkirmishProductionReservation> buffer = em.GetBuffer<SkirmishProductionReservation>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ReservationId == reservationId)
                    return buffer[i].Phase;
            }

            return SkirmishReservationPhase.None;
        }

        private static SkirmishResearchPhase ResearchPhase(
            EntityManager em,
            Entity session,
            uint researchId)
        {
            DynamicBuffer<SkirmishResearchQueueItem> buffer = em.GetBuffer<SkirmishResearchQueueItem>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ResearchId == researchId)
                    return buffer[i].Phase;
            }

            return SkirmishResearchPhase.None;
        }

        private static Entity FirstRole(EntityManager em, SkirmishRoleKind role, byte faction)
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

        private static Entity FirstDesignated(EntityManager em, SkirmishObjectiveRoleKind role)
        {
            using var query = em.CreateEntityQuery(
                typeof(SkirmishObjectiveRoleComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishObjectiveRoleComponent>(entities[i]).Role == role)
                    return entities[i];
            }

            return Entity.Null;
        }
    }
}
