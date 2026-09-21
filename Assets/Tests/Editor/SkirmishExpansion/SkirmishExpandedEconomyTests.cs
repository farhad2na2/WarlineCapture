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
            Assert.AreEqual(6, tank.SupplyCost);
            Assert.AreEqual(1, tank.MemberCount);
            Assert.AreEqual(SkirmishProducerKind.GroundStaging, tank.Producer);
            Assert.AreEqual(materials - 360, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
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
    }
}
