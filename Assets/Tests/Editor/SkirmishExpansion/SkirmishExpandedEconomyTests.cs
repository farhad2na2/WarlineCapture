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
        public void NativeDeliveryRejectsStaleAttemptPauseAndTerminalState()
        {
            using var world = new World(nameof(NativeDeliveryRejectsStaleAttemptPauseAndTerminalState));
            var em = world.EntityManager;
            CompileAndSpawn(em, out var session, out _);
            var nativeState = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            nativeState.Phase = SkirmishSessionPhase.Playing;
            em.SetComponentData(session, nativeState);
            var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var key = new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Rifle));
            Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Rifle, 1, authored.ArmyGround, out var receipt));
            Assert.IsTrue(SkirmishProductionService.BindNativeQueue(em, session, receipt.ReservationId, 42));
            Assert.IsFalse(SkirmishProductionService.CanDeliverNative(em, session, receipt.ReservationId, 42, 1, key, new FixedString64Bytes("previous-attempt")));
            Assert.IsTrue(SkirmishProductionService.CanDeliverNative(em, session, receipt.ReservationId, 42, 1, key, state.SessionId));
            em.AddComponent<SkirmishObjectiveClockComponent>(session);
            var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            clock.Paused = 1; em.SetComponentData(session, clock);
            Assert.IsFalse(SkirmishProductionService.CanDeliverNative(em, session, receipt.ReservationId, 42, 1, key, state.SessionId));
            clock.Paused = 0; em.SetComponentData(session, clock);
            state.Phase = SkirmishSessionPhase.Finished; em.SetComponentData(session, state);
            Assert.IsFalse(SkirmishProductionService.CanDeliverNative(em, session, receipt.ReservationId, 42, 1, key, state.SessionId));
            Assert.AreEqual(SkirmishReservationPhase.Reserved, ReservationPhase(em, session, receipt.ReservationId));
        }

        [Test]
        public void NativeProducerLossReleasesOnlyItsOwnQueue()
        {
            using var world = new World(nameof(NativeProducerLossReleasesOnlyItsOwnQueue));
            var em = world.EntityManager;
            CompileAndSpawn(em, out var session, out _);
            var nativeState = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            nativeState.Phase = SkirmishSessionPhase.Playing;
            em.SetComponentData(session, nativeState);
            var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            int materials = SkirmishMaterialsService.Read(em, session, 1);
            Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Rifle, 1, authored.ArmyGround, out var first));
            Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Rifle, 1, authored.ArmyGround, out var second));
            Assert.IsTrue(SkirmishProductionService.BindNativeQueue(em, session, first.ReservationId, 42));
            Assert.IsTrue(SkirmishProductionService.BindNativeQueue(em, session, second.ReservationId, 43));
            SkirmishProductionService.StartNativeQueue(em, session, first.ReservationId, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
            Assert.AreEqual(1, SkirmishProductionService.NotifyProducerDestroyed(em, session, SkirmishProducerKind.Barracks, 1, 42));
            Assert.AreEqual(SkirmishReservationPhase.Lost, ReservationPhase(em, session, first.ReservationId));
            Assert.AreEqual(SkirmishReservationPhase.Reserved, ReservationPhase(em, session, second.ReservationId));
            Assert.AreEqual(4, em.GetComponentData<SkirmishCapacityComponent>(session).InfantryReserved);
            Assert.AreEqual(materials - 160, SkirmishMaterialsService.Read(em, session, 1));
            Assert.AreEqual(0, SkirmishProductionService.NotifyProducerDestroyed(em, session, SkirmishProducerKind.Barracks, 1, 42));
            Assert.AreEqual(1, SkirmishProductionService.NotifyProducerDestroyed(em, session, SkirmishProducerKind.Barracks, 1, 43));
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).InfantryReserved);
            Assert.AreEqual(materials - 80, SkirmishMaterialsService.Read(em, session, 1), "The unstarted queue is refunded once.");
        }

        [Test]
        public void NativePacketDeliveryKeepsOneGroupAndRefundsOnlyUndeliveredMembers()
        {
            using var world = new World(nameof(NativePacketDeliveryKeepsOneGroupAndRefundsOnlyUndeliveredMembers));
            var em = world.EntityManager;
            CompileAndSpawn(em, out var session, out _);
            var nativeState = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            nativeState.Phase = SkirmishSessionPhase.Playing;
            em.SetComponentData(session, nativeState);
            var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var before = em.GetComponentData<SkirmishCapacityComponent>(session);
            int materials = SkirmishMaterialsService.Read(em, session, 1);
            Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Rifle, 1,
                authored.ArmyGround, out var decision));
            Assert.IsTrue(SkirmishProductionService.BindNativeQueue(em, session, decision.ReservationId, 42));
            uint group = 0;
            Entity first = Entity.Null;
            for (int i = 0; i < 2; i++)
            {
                var member = em.CreateEntity(typeof(Faction), typeof(UnitSourcePrefabKey), typeof(UnitHealth));
                em.SetComponentData(member, new Faction { Id = 1 });
                em.SetComponentData(member, new UnitSourcePrefabKey {
                    Value = new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Rifle)) });
                Assert.IsFalse(SkirmishProductionService.AdoptNativeMember(em, session, decision.ReservationId, 99, member, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId));
                Assert.IsTrue(SkirmishProductionService.AdoptNativeMember(em, session, decision.ReservationId, 42, member, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId));
                Assert.IsFalse(SkirmishProductionService.AdoptNativeMember(em, session, decision.ReservationId, 42, member, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId));
                var receipt = em.GetBuffer<SkirmishProductionReservation>(session)[0];
                if (i == 0) { group = receipt.ProductionGroupId; first = member; }
                Assert.AreEqual(group, receipt.ProductionGroupId);
                Assert.Greater(em.GetComponentData<UnitHealth>(member).Current, 0);
                Assert.IsTrue(em.HasComponent<SkirmishSharedActorTag>(member));
            }
            var halfway = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(before.InfantryLive + 2, halfway.InfantryLive);
            Assert.AreEqual(2, halfway.InfantryReserved);
            Assert.IsTrue(SkirmishProductionService.TryCancel(em, session, decision.ReservationId, out var cancel));
            Assert.AreEqual(30, cancel.RefundedMaterials, "75% of the two unspawned recruits, never the two delivered recruits.");
            Assert.AreEqual(materials - 80 + 30, SkirmishMaterialsService.Read(em, session, 1));
            var after = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(before.InfantryLive + 2, after.InfantryLive);
            Assert.AreEqual(0, after.InfantryReserved);
            Assert.AreEqual(0, after.SupplyReserved);
            Assert.AreEqual(2, em.GetBuffer<SkirmishProductionReservation>(session)[0].RemainingMembers);
            Assert.IsFalse(SkirmishProductionService.TryCancel(em, session, decision.ReservationId, out _));
            Assert.IsTrue(SkirmishProductionService.TryReleaseDeath(em, session, first));
            Assert.AreEqual(before.InfantryLive + 1, em.GetComponentData<SkirmishCapacityComponent>(session).InfantryLive);
        }

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
        public void PlayerAndEnemyProductionReceiptsNeverShareAnIdOrRefundTheOtherFaction()
        {
            using var world = new World(nameof(PlayerAndEnemyProductionReceiptsNeverShareAnIdOrRefundTheOtherFaction));
            var em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            var army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.GroundManeuver);
            int playerBefore = SkirmishMaterialsService.Read(em, session, 1);
            int enemyBefore = SkirmishMaterialsService.Read(em, session, 2);
            Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Tank, 1, army, 1, out var player));
            Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Car, 1, army, 2, out var enemy));
            Assert.AreNotEqual(player.ReservationId, enemy.ReservationId);
            Assert.IsTrue(SkirmishProductionService.TryCancel(em, session, enemy.ReservationId, out _));
            Assert.AreEqual(playerBefore - player.MaterialsCost, SkirmishMaterialsService.Read(em, session, 1));
            Assert.AreEqual(enemyBefore, SkirmishMaterialsService.Read(em, session, 2));
            Assert.AreEqual(SkirmishReservationPhase.Reserved, ReservationPhase(em, session, player.ReservationId));
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).GroundReserved);
            Assert.AreEqual(0, em.GetComponentData<SkirmishEnemyCapacityComponent>(session).GroundReserved);
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
            int afterReadinessPurchase = SkirmishMaterialsService.Read(em, session, 1);
            Assert.IsFalse(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.Readiness, 1, out SkirmishResearchDecision duplicateReadiness));
            Assert.AreEqual(SkirmishReasonCode.QueueLocked, duplicateReadiness.Reason);
            Assert.AreEqual(afterReadinessPurchase, SkirmishMaterialsService.Read(em, session, 1));
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

        [Test]
        public void S004EstablishedGrantsDoNotDebitAndPadStartsLive()
        {
            using var world = new World(nameof(S004EstablishedGrantsDoNotDebitAndPadStartsLive));
            EntityManager em = world.EntityManager;
            CompileAndSpawnS004(em, out Entity session, out SkirmishResolvedSetup setup);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(240, em.GetComponentData<SkirmishEconomyStockComponent>(session).Oil);
            Assert.AreEqual(700, em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            var research = em.GetComponentData<SkirmishResearchStateComponent>(session);
            Assert.AreEqual(SkirmishReadinessStage.Established, research.Readiness);
            Assert.AreEqual(0, research.InfantryWeapons);
            Assert.AreEqual(0, research.VehicleProtection);
            Assert.AreEqual(0, research.AircraftEfficiency);
            Assert.AreEqual(0u, research.NextResearchId);
            Assert.AreEqual(0, em.GetBuffer<SkirmishResearchQueueItem>(session).Length);
            var capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(1, capacity.AirLive);
            Assert.AreEqual(setup.PlayerGround, capacity.GroundLive);
            Assert.AreEqual(42, capacity.SupplyLive);
            Assert.AreEqual(0, capacity.AirReserved);
            Entity pad = FindStructure(em, 1, SkirmishStructureIds.Helipad);
            Assert.AreNotEqual(Entity.Null, pad);
            Assert.AreEqual(500, em.GetComponentData<UnitHealth>(pad).Current);
            Assert.AreEqual(Entity.Null, FindStructure(em, 1, SkirmishStructureIds.Airport));
            Assert.AreNotEqual(Entity.Null, FindStructure(em, 2, SkirmishStructureIds.Helipad));

            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyAir, out SkirmishProductionDecision tank));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, tank.Reason);
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.ApcHeavy, 1, authored.ArmyAir, out SkirmishProductionDecision heavy));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, heavy.Reason);
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Siege, 1, authored.ArmyAir, out SkirmishProductionDecision siege));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, siege.Reason);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            research = em.GetComponentData<SkirmishResearchStateComponent>(session);
            research.Readiness = SkirmishReadinessStage.FullArsenal;
            em.SetComponentData(session, research);
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Fighter, 1, authored.ArmyAir, out SkirmishProductionDecision jet));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, jet.Reason);
            Assert.AreEqual("producer.airport", jet.Field);
            research.Readiness = SkirmishReadinessStage.Established;
            em.SetComponentData(session, research);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision queued));
            Assert.AreEqual(420, queued.MaterialsCost);
            Assert.AreEqual(480, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            Assert.IsTrue(SkirmishProductionService.TryStartQueued(em, session, queued.ReservationId, out _));
            em.SetComponentData(pad, new UnitHealth { Current = 0, Max = 500 });
            Assert.IsFalse(SkirmishProductionService.TryDispatchQueued(
                em, session, queued.ReservationId, authored.ArmyAir, out SkirmishProductionDecision refunded));
            Assert.AreEqual(420, refunded.RefundedMaterials);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);

            Assert.IsFalse(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.TransportHeli, 1, authored.ArmyAir, out SkirmishProductionDecision blocked));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, blocked.Reason);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            var before = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.AntiAir, 1, authored.ArmyAir, out SkirmishProductionDecision aa));
            Assert.AreEqual(220, aa.MaterialsCost);
            Assert.AreEqual(680, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(before.GroundLive + 1, em.GetComponentData<SkirmishCapacityComponent>(session).GroundLive);
            Assert.AreEqual(before.AirLive, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
        }

        [Test]
        public void S004EstablishedGrantLedgerAirCapAndHelipadResearch()
        {
            using var world = new World(nameof(S004EstablishedGrantLedgerAirCapAndHelipadResearch));
            EntityManager em = world.EntityManager;
            CompileAndSpawnS004(em, out Entity session, out SkirmishResolvedSetup setup);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Entity grantTransport = FirstRole(em, SkirmishRoleKind.TransportHeli, 1);
            Entity grantAa = FirstRole(em, SkirmishRoleKind.AntiAir, 1);
            Assert.AreNotEqual(Entity.Null, grantTransport);
            Assert.AreNotEqual(Entity.Null, grantAa);
            Assert.AreEqual(0, em.GetBuffer<SkirmishProductionReservation>(session).Length);
            Assert.AreEqual(0u, em.GetComponentData<SkirmishAttemptOwnedComponent>(grantTransport).ReservationId);
            Assert.AreEqual(0u, em.GetComponentData<SkirmishAttemptOwnedComponent>(grantAa).ReservationId);
            Assert.AreEqual(2, setup.TacticalAirCapEach);
            Assert.AreEqual(2, em.GetComponentData<SkirmishCapacityComponent>(session).AirCap);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(42, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyLive);
            Assert.AreEqual(900, PlayerMaterials(em, session));
            Assert.AreEqual(700, PlayerFuel(em, session));
            Assert.AreEqual(240, em.GetComponentData<SkirmishEconomyStockComponent>(session).Oil);
            Assert.AreEqual(1, em.GetComponentData<SkirmishEnemyCapacityComponent>(session).AirLive);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);

            var capped = em.GetComponentData<SkirmishCapacityComponent>(session);
            int airLive = capped.AirLive;
            capped.AirLive = capped.AirCap;
            em.SetComponentData(session, capped);
            Assert.IsFalse(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision full));
            Assert.AreEqual(SkirmishReasonCode.InsufficientCapacity, full.Reason);
            Assert.AreEqual(900, PlayerMaterials(em, session));
            Assert.AreEqual(700, PlayerFuel(em, session));
            capped = em.GetComponentData<SkirmishCapacityComponent>(session);
            capped.AirLive = airLive;
            em.SetComponentData(session, capped);

            Entity pad = FindStructure(em, 1, SkirmishStructureIds.Helipad);
            em.SetComponentData(pad, new UnitHealth { Current = 0, Max = 500 });
            Assert.IsFalse(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.AircraftEfficiency, 1, out SkirmishResearchDecision missingAir));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, missingAir.Reason);
            Assert.AreEqual("producer.air", missingAir.Field);
            Assert.AreEqual(0, em.GetComponentData<SkirmishResearchStateComponent>(session).AircraftEfficiency);
            Assert.AreEqual(900, PlayerMaterials(em, session));
            em.SetComponentData(pad, new UnitHealth { Current = 500, Max = 500 });

            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision queued));
            Assert.AreEqual(420, queued.MaterialsCost);
            Assert.AreEqual(8, queued.SupplyCost);
            Assert.AreEqual(SkirmishProducerKind.Helipad, queued.Producer);
            Assert.AreEqual(480, PlayerMaterials(em, session));
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(8, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyReserved);
            Assert.IsTrue(SkirmishProductionService.TryStartQueued(em, session, queued.ReservationId, out _));
            Assert.AreEqual(SkirmishReservationPhase.Producing, ReservationPhase(em, session, queued.ReservationId));

            capped = em.GetComponentData<SkirmishCapacityComponent>(session);
            capped.AirCap = 4;
            em.SetComponentData(session, capped);
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.TransportHeli, 1, authored.ArmyAir, out SkirmishProductionDecision locked));
            Assert.AreEqual(SkirmishReasonCode.QueueLocked, locked.Reason);
            Assert.AreEqual("queue", locked.Field);
            Assert.AreEqual(480, PlayerMaterials(em, session));
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            capped = em.GetComponentData<SkirmishCapacityComponent>(session);
            capped.AirCap = 2;
            em.SetComponentData(session, capped);

            var beforeAa = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.AntiAir, 1, authored.ArmyAir, out SkirmishProductionDecision boughtAa));
            Assert.AreEqual(220, boughtAa.MaterialsCost);
            Assert.AreEqual(SkirmishProducerKind.GroundStaging, boughtAa.Producer);
            Assert.AreEqual(260, PlayerMaterials(em, session));
            Assert.AreEqual(beforeAa.GroundLive + 1, em.GetComponentData<SkirmishCapacityComponent>(session).GroundLive);
            Assert.AreEqual(beforeAa.AirLive, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(
                "Unit_Veh_Missle_Launcher_Air",
                em.GetComponentData<UnitSourcePrefabKey>(FindReservedUnit(em, boughtAa.ReservationId)).Value.ToString());

            Assert.AreEqual(1, SkirmishProductionService.NotifyProducerDestroyed(
                em, session, SkirmishProducerKind.Helipad, 1));
            Assert.AreEqual(SkirmishReservationPhase.Lost, ReservationPhase(em, session, queued.ReservationId));
            Assert.AreEqual(Entity.Null, FindReservedUnit(em, queued.ReservationId));
            Assert.AreEqual(260, PlayerMaterials(em, session));
            Assert.AreEqual(700, PlayerFuel(em, session));
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyReserved);
            Assert.AreEqual(48, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyLive);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);

            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.TransportHeli, 1, authored.ArmyAir, out SkirmishProductionDecision reservedTransport));
            Assert.AreEqual(240, reservedTransport.MaterialsCost);
            Assert.AreEqual(20, PlayerMaterials(em, session));
            Assert.AreEqual(1, SkirmishProductionService.NotifyProducerDestroyed(
                em, session, SkirmishProducerKind.Helipad, 1));
            Assert.AreEqual(SkirmishReservationPhase.Cancelled, ReservationPhase(em, session, reservedTransport.ReservationId));
            Assert.AreEqual(260, PlayerMaterials(em, session));
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(48, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyLive);

            em.AddComponentData(grantTransport, new UnitFuelConsumption
            {
                GroundFuelPerCell = 10f,
                AirFuelPerCell = 4f,
                Enabled = 1
            });
            Assert.IsTrue(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.AircraftEfficiency, 1, out SkirmishResearchDecision airResearch));
            Assert.AreEqual(200, airResearch.MaterialsCost);
            Assert.AreEqual(SkirmishProducerKind.Helipad, ResearchProducer(em, session, airResearch.ResearchId));
            Assert.AreEqual(60, PlayerMaterials(em, session));
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, airResearch.ResearchId, out _));
            Assert.IsTrue(SkirmishResearchService.TryComplete(em, session, airResearch.ResearchId, out SkirmishResearchDecision completed));
            Assert.AreEqual(1, completed.LevelAfter);
            var research = em.GetComponentData<SkirmishResearchStateComponent>(session);
            Assert.AreEqual(1, research.AircraftEfficiency);
            Assert.AreEqual(SkirmishReadinessStage.Established, research.Readiness);
            Assert.AreEqual(0, research.InfantryWeapons);
            Assert.AreEqual(0, research.VehicleProtection);
            var stamped = em.GetComponentData<UnitFuelConsumption>(grantTransport);
            Assert.AreEqual(9f, stamped.GroundFuelPerCell, 0.001f);
            Assert.AreEqual(3.6f, stamped.AirFuelPerCell, 0.001f);
            Assert.IsFalse(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.AircraftEfficiency, 1, out SkirmishResearchDecision again));
            Assert.AreEqual(SkirmishReasonCode.AlreadyCompleted, again.Reason);
            Assert.AreEqual(60, PlayerMaterials(em, session));
            Assert.AreEqual(700, PlayerFuel(em, session));

            Assert.IsTrue(SkirmishProductionService.TryReleaseDeath(em, session, grantTransport));
            Assert.IsFalse(SkirmishProductionService.TryReleaseDeath(em, session, grantTransport));
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(40, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyLive);
            Assert.AreEqual(60, PlayerMaterials(em, session));
            Assert.AreEqual(700, PlayerFuel(em, session));
            Assert.AreEqual(240, em.GetComponentData<SkirmishEconomyStockComponent>(session).Oil);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishEnemyCapacityComponent>(session).AirLive);
            Assert.AreEqual(0u, em.GetComponentData<SkirmishAttemptOwnedComponent>(grantAa).ReservationId);
        }

        [Test]
        public void BlockedRegistryDispatchRefundsOnceWithoutPromotingPopulation()
        {
            using var world = new World(nameof(BlockedRegistryDispatchRefundsOnceWithoutPromotingPopulation));
            var em = world.EntityManager;
            CompileAndSpawnS003(em, out Entity session, out var setup);
            SkirmishMaterialsService.Initialize(em, session, setup);
            var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            string rifleKey = null;
            foreach (var force in setup.Forces)
                if (force.RoleKind == SkirmishRoleKind.Rifle) { rifleKey = force.RuntimePrefabKey; break; }
            Assert.IsNotEmpty(rifleKey);
            Entity prefab = em.CreateEntity(typeof(Prefab), typeof(UnitSourcePrefabKey), typeof(UnitFootprint));
            em.SetComponentData(prefab, new UnitSourcePrefabKey { Value = new FixedString64Bytes(rifleKey) });
            Entity registry = em.CreateEntity();
            em.AddBuffer<UnitPrefabRegistryEntry>(registry).Add(new UnitPrefabRegistryEntry { Prefab = prefab });
            var blocked = new NativeBitArray(64, Allocator.Temp, NativeArrayOptions.ClearMemory);
            var occupied = new NativeBitArray(64, Allocator.Temp, NativeArrayOptions.ClearMemory);
            try
            {
                Entity grid = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
                em.SetComponentData(grid, new GridConfig { Width = 8, Height = 8, CellSize = 1f });
                em.SetComponentData(grid, new DynamicBlockerComponent { GridSize = 64, Blocked = blocked });
                em.SetComponentData(grid, new DynamicOccupancyComponent { GridSize = 64, Occupied = occupied });
                var walkable = em.AddBuffer<GridWalkable>(grid);
                walkable.ResizeUninitialized(64);
                for (int i = 0; i < 64; i++) walkable[i] = new GridWalkable { Value = 0 };
                var before = em.GetComponentData<SkirmishCapacityComponent>(session);
                Assert.IsFalse(SkirmishProductionService.TryProduce(em, session, SkirmishRoleIds.Rifle, 1, authored.ArmyAir, out var decision));
                Assert.AreEqual(SkirmishReasonCode.BlockedSpawn, decision.Reason);
                Assert.AreEqual(450, SkirmishMaterialsService.Read(em, session, 1));
                var after = em.GetComponentData<SkirmishCapacityComponent>(session);
                Assert.AreEqual(before.InfantryLive, after.InfantryLive);
                Assert.AreEqual(0, after.InfantryReserved);
                Assert.IsFalse(SkirmishProductionService.TryRefundFailedDispatch(em, session, decision.ReservationId, out _));
                Assert.AreEqual(450, SkirmishMaterialsService.Read(em, session, 1));
            }
            finally { blocked.Dispose(); occupied.Dispose(); }
        }

        [Test]
        public void QueuedReadinessStartsAndCompletesThroughTickWithoutManualStart()
        {
            using var world = new World(nameof(QueuedReadinessStartsAndCompletesThroughTickWithoutManualStart));
            var em = world.EntityManager;
            CompileAndSpawnS003(em, out Entity session, out _);
            int before = SkirmishMaterialsService.Read(em, session, 1);
            Assert.IsTrue(SkirmishResearchService.TryQueue(em, session, SkirmishResearchKind.Readiness, 1, out var queued));
            Assert.AreEqual(before - 240, SkirmishMaterialsService.Read(em, session, 1));
            Assert.AreEqual(0, SkirmishResearchService.Tick(em, session, 100, true));
            Assert.AreEqual(SkirmishReadinessStage.Field, em.GetComponentData<SkirmishResearchStateComponent>(session).Readiness);
            Assert.AreEqual(0, SkirmishResearchService.Tick(em, session, 44, false));
            Assert.AreEqual(SkirmishReadinessStage.Field, em.GetComponentData<SkirmishResearchStateComponent>(session).Readiness);
            Assert.AreEqual(1, SkirmishResearchService.Tick(em, session, 1, false));
            Assert.AreEqual(SkirmishReadinessStage.Established, em.GetComponentData<SkirmishResearchStateComponent>(session).Readiness);
            Assert.AreEqual(0, SkirmishResearchService.Tick(em, session, 100, false));
            Assert.AreEqual(before - 240, SkirmishMaterialsService.Read(em, session, 1));
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedEconomyTests();
                suite.BlockedRegistryDispatchRefundsOnceWithoutPromotingPopulation();
                suite.S002StartingStocksAndCapsSeedFromMatrix();
                suite.CapacityLedgerReservePromoteReleaseOnce();
                suite.GroundStagingProducesTankThenDeathReleasesOnce();
                suite.CapacityLifecycleReleasesDeadOwnedUnitsOnce();
                suite.InfantrySquadCostsFourMembersAndMaterials();
                suite.MissingMaterialsAndCapsRejectWithoutMutation();
                suite.FuelGateRejectsWithoutStockMutation();
                suite.RecruitmentFuelCostStaysZeroOnS002Overlays();
                suite.PlayerAndEnemyProductionReceiptsNeverShareAnIdOrRefundTheOtherFaction();
                suite.NativePacketDeliveryKeepsOneGroupAndRefundsOnlyUndeliveredMembers();
                suite.NativeProducerLossReleasesOnlyItsOwnQueue();
                suite.NativeDeliveryRejectsStaleAttemptPauseAndTerminalState();
                suite.MidProduceCancelRefundsReservedThenSeventyFivePercent();
                suite.FailedDispatchAndProducerDestructionUseSpecifiedRefunds();
                suite.CategoryResearchFromStagingAndHqAppliesOnce();
                suite.QueuedReadinessStartsAndCompletesThroughTickWithoutManualStart();
                suite.ReplacementHqCanResearchWithoutBecomingVictoryBase();
                suite.S003AirMobileAaGatePadReadinessAndMissingPadRefund();
                suite.S004EstablishedGrantsDoNotDebitAndPadStartsLive();
                suite.S004EstablishedGrantLedgerAirCapAndHelipadResearch();
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

        private static void CompileAndSpawnS004(
            EntityManager em,
            out Entity session,
            out SkirmishResolvedSetup setup)
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
        }

        private static int PlayerMaterials(EntityManager em, Entity session) =>
            em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;

        private static int PlayerFuel(EntityManager em, Entity session) =>
            em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel;

        private static SkirmishProducerKind ResearchProducer(EntityManager em, Entity session, uint researchId)
        {
            DynamicBuffer<SkirmishResearchQueueItem> buffer = em.GetBuffer<SkirmishResearchQueueItem>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ResearchId == researchId)
                    return buffer[i].Producer;
            }

            return SkirmishProducerKind.None;
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
