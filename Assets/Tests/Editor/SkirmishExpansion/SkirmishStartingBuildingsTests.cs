using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tests.Editor
{
    public sealed class SkirmishStartingBuildingsTests
    {
        [Test]
        public void PhysicalSupplyGrantIsAtomicAndDoesNotRefillOnRepeatedStartup()
        {
            using var world = new World(nameof(PhysicalSupplyGrantIsAtomicAndDoesNotRefillOnRepeatedStartup));
            var em = world.EntityManager;
            var id = new FixedString64Bytes("supply-test");
            Entity session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishSharedBuildingsReady));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = id });
            Entity Store(byte faction, bool refinery)
            {
                var entity = em.CreateEntity(typeof(BuildingResourceStorageComponent), typeof(SkirmishAttemptOwnedComponent));
                em.SetComponentData(entity, new SkirmishAttemptOwnedComponent { SessionId = id, FactionId = faction, IsStructure = 1 });
                em.SetComponentData(entity, new BuildingResourceStorageComponent
                { OwnerFactionId = faction, OilStorageCapacity = refinery ? 500 : 0, FuelStorageCapacity = 500,
                  FuelBarrelsPerDay = refinery ? 10 : 0 });
                return entity;
            }
            Entity playerRefinery = Store(1, true);
            Entity playerFuel = Store(1, false);
            Store(2, true);
            var setup = new SkirmishResolvedSetup { OilEach = 120, UsableFuelEach = 350 };
            Assert.IsFalse(SkirmishStartingSupplyService.Initialize(em, session, setup));
            Assert.AreEqual(0, em.GetComponentData<BuildingResourceStorageComponent>(playerRefinery).StoredOilBarrels);
            Assert.AreEqual(0, SkirmishStartingSupplyService.ReadUsableFuel(em, 1));
            Store(2, false);
            Assert.IsTrue(SkirmishStartingSupplyService.Initialize(em, session, setup));
            Assert.AreEqual(120, em.GetComponentData<BuildingResourceStorageComponent>(playerRefinery).StoredOilBarrels);
            Assert.AreEqual(0, em.GetComponentData<BuildingResourceStorageComponent>(playerRefinery).StoredFuelBarrels);
            Assert.AreEqual(350, SkirmishStartingSupplyService.ReadUsableFuel(em, 1));
            Assert.AreEqual(350, SkirmishStartingSupplyService.ReadUsableFuel(em, 2));
            var consumed = em.GetComponentData<BuildingResourceStorageComponent>(playerFuel);
            consumed.StoredFuelBarrels = 320.75f;
            consumed.ReservedFuelOutboundBarrels = 20;
            consumed.CivilianFuelReserveBarrels = 10;
            em.SetComponentData(playerFuel, consumed);
            Assert.IsTrue(SkirmishStartingSupplyService.Initialize(em, session, setup));
            Assert.AreEqual(290.75f, SkirmishStartingSupplyService.ReadUsableFuel(em, 1));
            Assert.IsTrue(SkirmishStartingSupplyService.TryWriteFuel(em, session, 1, 270));
            Assert.AreEqual(270.75f, SkirmishStartingSupplyService.ReadUsableFuel(em, 1));
            Assert.AreEqual(20, em.GetComponentData<BuildingResourceStorageComponent>(playerFuel).ReservedFuelOutboundBarrels);
            Assert.IsFalse(SkirmishStartingSupplyService.TryWriteFuel(em, session, 1, 600));
            Assert.AreEqual(270.75f, SkirmishStartingSupplyService.ReadUsableFuel(em, 1));
        }

        [Test]
        public void OilGrantStartsFabricationAndTheHaulChainBeforeRefineryOverflow()
        {
            using var world = new World(nameof(OilGrantStartsFabricationAndTheHaulChainBeforeRefineryOverflow));
            var em = world.EntityManager;
            var id = new FixedString64Bytes("oil-priority");
            var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishSharedBuildingsReady));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = id });
            Entity Store(byte faction, string key, int capacity, bool pump = false, bool fabrication = false)
            {
                var entity = em.CreateEntity(typeof(BuildingResourceStorageComponent), typeof(SkirmishAttemptOwnedComponent));
                em.SetComponentData(entity, new SkirmishAttemptOwnedComponent
                { SessionId = id, StableObjectId = new FixedString64Bytes(key + faction), FactionId = faction, IsStructure = 1 });
                em.SetComponentData(entity, new BuildingResourceStorageComponent
                { OwnerFactionId = faction, OilStorageCapacity = capacity, OilBarrelsPerDay = pump ? 10 : 0 });
                if (fabrication) em.AddComponent<MaterialFabricationComponent>(entity);
                return entity;
            }
            Entity refinery = Entity.Null, depot = Entity.Null, source = Entity.Null;
            for (byte faction = 1; faction <= 2; faction++)
            {
                var r = Store(faction, "refinery", 5000);
                var d = Store(faction, "fabrication", 24, fabrication: true);
                var o = Store(faction, "pump", 200, pump: true);
                if (faction == 1) { refinery = r; depot = d; source = o; }
            }
            Assert.IsTrue(SkirmishStartingSupplyService.Initialize(em, session, new SkirmishResolvedSetup { OilEach = 120 }));
            Assert.AreEqual(24f, em.GetComponentData<BuildingResourceStorageComponent>(depot).StoredOilBarrels);
            Assert.AreEqual(96f, em.GetComponentData<BuildingResourceStorageComponent>(source).StoredOilBarrels);
            Assert.AreEqual(0f, em.GetComponentData<BuildingResourceStorageComponent>(refinery).StoredOilBarrels);
            Assert.AreEqual(120, SkirmishStartingSupplyService.ReadOil(em, session, 1));
            Assert.AreEqual(120, SkirmishStartingSupplyService.ReadOil(em, session, 2));
        }

        [Test]
        public void StartingGrantsWaitForSharedResultsAndBindTheActualBaseOnce()
        {
            using var world = new World(nameof(StartingGrantsWaitForSharedResultsAndBindTheActualBaseOnce));
            var em = world.EntityManager;
            Entity session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = new FixedString64Bytes("test") });
            Entity grid = em.CreateEntity(typeof(GridConfig));
            em.SetComponentData(grid, new GridConfig { Width = 100, Height = 100, CellSize = 1f });
            Entity boundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
            em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary).Add(new BuildingConfiguredSpawnableReadModel
            { BuildingId = new FixedString128Bytes("Building_Barrack"), FootprintCells = new int2(10, 10) });
            em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
            var setup = new SkirmishResolvedSetup
            {
                Structures = new[] { new SkirmishResolvedStructureEntry
                { StructureId = SkirmishStructureIds.Barracks, FactionId = 1, DesignatedBase = true,
                  ObjectiveRoleId = "original", SpawnWorldX = 30, SpawnWorldZ = 30 } }
            };
            Assert.IsFalse(SkirmishStartingBuildingsService.Step(em, session, setup, out var reason));
            Assert.AreEqual(SkirmishReasonCode.None, reason);
            Assert.IsFalse(SkirmishStartingBuildingsService.Step(em, session, setup, out _));
            Assert.AreEqual(1, em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length);
            Entity scenery = em.CreateEntity(typeof(RuntimeBuildingCombatInfo), typeof(UnitHealth), typeof(OperationMapBuildingComponent));
            em.SetComponentData(scenery, new RuntimeBuildingCombatInfo { RuntimeBuildingId = 42, OwnerFactionId = 0 });
            em.SetComponentData(scenery, new UnitHealth { Current = 700, Max = 700 });
            Entity actual = em.CreateEntity(typeof(RuntimeBuildingCombatInfo), typeof(UnitHealth));
            em.SetComponentData(actual, new RuntimeBuildingCombatInfo { RuntimeBuildingId = 42, OwnerFactionId = 1 });
            em.SetComponentData(actual, new UnitHealth { Current = 500, Max = 500 });
            var request = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary)[0];
            request.Status = BuildingRuntimeSpawnRequest.Succeeded;
            request.BuildingRuntimeId = 42;
            var results = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            results[0] = request;
            Assert.IsTrue(SkirmishStartingBuildingsService.Step(em, session, setup, out _));
            Assert.AreEqual("original", em.GetComponentData<SkirmishObjectiveRoleComponent>(actual).StableObjectId.ToString());
            Assert.IsFalse(em.HasComponent<SkirmishAttemptOwnedComponent>(scenery));
            Assert.IsTrue(SkirmishStartingBuildingsService.Step(em, session, setup, out _));
            Assert.AreEqual(1, em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length);
            SkirmishStartingBuildingsService.Cancel(em, session);
            Assert.AreEqual(0, em.GetComponentData<UnitHealth>(actual).Current);
            Assert.AreEqual(700, em.GetComponentData<UnitHealth>(scenery).Current);
        }
    }
}
