using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishFacilityEconomyProfileTests
    {
        [Test]
        public void BothFactionsAndExpansionFacilitiesUseSelectedSizeWithoutStockGrants()
        {
            foreach (var size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
            {
                using var world = new World("facility-profile-" + size);
                var em = world.EntityManager;
                var id = new FixedString64Bytes("profile-attempt");
                var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
                em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = id, SizeId = size });
                int oilRate = size == SkirmishSizeId.Standard ? 120 : size == SkirmishSizeId.War ? 180 : 240;
                int materialOutput = size == SkirmishSizeId.Standard ? 50 : size == SkirmishSizeId.War ? 80 : 110;
                int materialInput = size == SkirmishSizeId.Standard ? 10 : size == SkirmishSizeId.War ? 16 : 22;
                foreach (byte faction in new byte[] { 1, 2 })
                foreach (string kind in new[] { SkirmishStructureIds.OilPump, SkirmishStructureIds.Refinery,
                    SkirmishStructureIds.RefineryModule, SkirmishStructureIds.FabricationDepot })
                for (int copy = 0; copy < 2; copy++)
                {
                    var entity = em.CreateEntity(typeof(BuildingResourceStorageComponent));
                    em.SetComponentData(entity, new BuildingResourceStorageComponent {
                        OwnerFactionId = faction, OilStorageCapacity = 200, FuelStorageCapacity = 300,
                        StoredOilBarrels = 17, StoredFuelBarrels = 29, OilBarrelsPerDay = kind == SkirmishStructureIds.OilPump ? 50 : 0 });
                    if (kind == SkirmishStructureIds.FabricationDepot)
                        em.AddComponentData(entity, new MaterialFabricationComponent {
                            OwnerFactionId = faction, OilConsumedPerCycle = 4, MaterialsOutputPerCycle = 20,
                            CycleDurationSeconds = 30, CycleProgressSeconds = 7 });
                    SkirmishScenarioSpawnSystem.BindStructure(em, entity, id, new Game.Configs.SkirmishResolvedStructureEntry {
                        StructureId = kind, FactionId = faction });
                    var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                    Assert.AreEqual(17, storage.StoredOilBarrels);
                    Assert.AreEqual(29, storage.StoredFuelBarrels);
                    if (kind == SkirmishStructureIds.OilPump) Assert.AreEqual(oilRate, storage.OilBarrelsPerDay);
                    if (kind == SkirmishStructureIds.FabricationDepot)
                    {
                        var recipe = em.GetComponentData<MaterialFabricationComponent>(entity);
                        Assert.AreEqual(materialInput, recipe.OilConsumedPerCycle);
                        Assert.AreEqual(materialOutput, recipe.MaterialsOutputPerCycle);
                        Assert.AreEqual(7, recipe.CycleProgressSeconds);
                    }
                }
            }
        }

        [Test]
        public void LegacyAndOtherAttemptBindingsKeepAuthoredRecipes()
        {
            foreach (bool legacy in new[] { true, false })
            {
                using var world = new World("profile-reject-" + legacy);
                var em = world.EntityManager;
                var id = new FixedString64Bytes("current-attempt");
                var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
                em.SetComponentData(session, new SkirmishExpandedSessionComponent {
                    SessionId = id, SizeId = SkirmishSizeId.Standard, IsLegacy = (byte)(legacy ? 1 : 0) });
                var pump = em.CreateEntity(typeof(BuildingResourceStorageComponent));
                em.SetComponentData(pump, new BuildingResourceStorageComponent { OilBarrelsPerDay = 50, StoredOilBarrels = 7 });
                var depot = em.CreateEntity(typeof(MaterialFabricationComponent));
                em.SetComponentData(depot, new MaterialFabricationComponent { OilConsumedPerCycle = 4,
                    MaterialsOutputPerCycle = 20, CycleDurationSeconds = 30, CycleProgressSeconds = 11 });
                var binding = legacy ? id : new FixedString64Bytes("older-attempt");
                SkirmishFacilityEconomyProfile.Apply(em, pump, binding, SkirmishStructureIds.OilPump);
                SkirmishFacilityEconomyProfile.Apply(em, depot, binding, SkirmishStructureIds.FabricationDepot);
                Assert.IsFalse(em.HasComponent<BuildingResourceProductionProfileComponent>(pump));
                Assert.AreEqual(50, em.GetComponentData<BuildingResourceStorageComponent>(pump).OilBarrelsPerDay);
                var recipe = em.GetComponentData<MaterialFabricationComponent>(depot);
                Assert.AreEqual(4, recipe.OilConsumedPerCycle); Assert.AreEqual(20, recipe.MaterialsOutputPerCycle);
                Assert.AreEqual(11, recipe.CycleProgressSeconds);
            }
        }

        [Test]
        public void ProfiledPumpAndCampaignNeighborKeepDifferentClocksAndRates()
        {
            using var world = new World(nameof(ProfiledPumpAndCampaignNeighborKeepDifferentClocksAndRates));
            var em = world.EntityManager;
            var campaign = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            var expanded = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            var storage = new BuildingResourceStorageComponent { OilStorageCapacity = 1000, OilBarrelsPerDay = 50 };
            em.SetComponentData(campaign, storage); em.SetComponentData(expanded, storage);
            em.AddComponentData(expanded, new BuildingResourceProductionProfileComponent { PeriodSeconds = 60, OilPerPeriod = 120 });
            using var q = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
            var result = BuildingResourceProductionEcsSystem.ApplyStorageQuery(em, q, 120, 60, 2);
            Assert.AreEqual(145, result.OilExtractedBarrels, .001);
            Assert.AreEqual(25, em.GetComponentData<BuildingResourceStorageComponent>(campaign).StoredOilBarrels, .001);
            Assert.AreEqual(50, em.GetComponentData<BuildingResourceStorageComponent>(campaign).OilBarrelsPerDay);
            Assert.AreEqual(120, em.GetComponentData<BuildingResourceStorageComponent>(expanded).StoredOilBarrels, .001);
        }

        [Test]
        public void HaulerMetadataSyncPreservesProfileWithoutChangingSourceDefinition()
        {
            using var world = new World(nameof(HaulerMetadataSyncPreservesProfileWithoutChangingSourceDefinition));
            var em = world.EntityManager;
            var entity = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(entity, new BuildingResourceStorageComponent { StoredOilBarrels = 7 });
            em.AddComponentData(entity, new BuildingResourceProductionProfileComponent { PeriodSeconds = 60, OilPerPeriod = 120 });
            var definition = new BuildingDefinition { OilBarrelsPerDay = 50, OilStorageCapacity = 200 };
            var runtime = new RuntimeBuildingEntity { Id = 9, CombatEntity = entity,
                Definition = definition, HasOwnerFaction = true, OwnerFactionId = 1 };
            Assert.IsTrue(ResourceHaulerStorageAccessSystemHelper.TryGetEntityResourceStorage(em, runtime, out var storage));
            Assert.AreEqual(120, storage.OilBarrelsPerDay); Assert.AreEqual(7, storage.StoredOilBarrels);
            ResourceHaulerStorageAccessSystemHelper.CommitEntityResourceStorage(em, runtime, storage);
            BuildingProductionTickCompositionSystemHelper.SyncBuildingResourceStorageFromEcs(em, runtime);
            Assert.AreEqual(120, em.GetComponentData<BuildingResourceStorageComponent>(entity).OilBarrelsPerDay);
            Assert.AreEqual(50, definition.OilBarrelsPerDay);
        }

        [Test]
        public void RefineryWaitsForFullBatchInputAndCapacityAndPreservesReservations()
        {
            var profile = new BuildingResourceProductionProfileComponent {
                PeriodSeconds = 60, FuelPerPeriod = 120, OilPerFuelBatch = 30, FuelPerBatch = 60, FuelBatchSeconds = 30 };
            var storage = new BuildingResourceStorageComponent { OilStorageCapacity = 200, FuelStorageCapacity = 100,
                StoredOilBarrels = 30, StoredFuelBarrels = 50 };
            Assert.AreEqual(0, BuildingResourceProductionEcsSystem.ApplyProfileTick(ref storage, ref profile, 30).FuelProducedBarrels);
            Assert.AreEqual(30, storage.StoredOilBarrels); Assert.AreEqual(0, profile.FuelBatchProgressSeconds);
            storage.StoredFuelBarrels = 0; storage.ReservedOilOutboundBarrels = 1;
            Assert.AreEqual(0, BuildingResourceProductionEcsSystem.ApplyProfileTick(ref storage, ref profile, 30).FuelProducedBarrels);
            storage.ReservedOilOutboundBarrels = 0; storage.ReservedFuelInboundBarrels = 41;
            Assert.AreEqual(0, BuildingResourceProductionEcsSystem.ApplyProfileTick(ref storage, ref profile, 30).FuelProducedBarrels);
            storage.ReservedFuelInboundBarrels = 0;
            Assert.AreEqual(0, BuildingResourceProductionEcsSystem.ApplyProfileTick(ref storage, ref profile, 29).FuelProducedBarrels);
            Assert.AreEqual(30, storage.StoredOilBarrels);
            Assert.AreEqual(60, BuildingResourceProductionEcsSystem.ApplyProfileTick(ref storage, ref profile, 1).FuelProducedBarrels);
            Assert.AreEqual(0, storage.StoredOilBarrels); Assert.AreEqual(60, storage.StoredFuelBarrels);
        }

        [Serializable] private sealed class SavedParts { public SkirmishNativeCheckpointParts.Part[] Parts; }
        [Test]
        public void NativeCheckpointPreservesRefineryBatchProgressAcrossFreshWorld()
        {
            using var source = new World("profile-save"); var em = source.EntityManager;
            var actor = em.CreateEntity(typeof(BuildingResourceProductionProfileComponent), typeof(BuildingResourceStorageComponent));
            em.SetComponentData(actor, new BuildingResourceProductionProfileComponent {
                PeriodSeconds = 60, FuelPerPeriod = 120, OilPerFuelBatch = 30, FuelPerBatch = 60,
                FuelBatchSeconds = 30, FuelBatchProgressSeconds = 23 });
            em.SetComponentData(actor, new BuildingResourceStorageComponent { OilStorageCapacity = 200,
                FuelStorageCapacity = 300, StoredOilBarrels = 30 });
            var parts = SkirmishNativeCheckpointParts.Capture(em, actor, _ => "refinery");
            parts = JsonUtility.FromJson<SavedParts>(JsonUtility.ToJson(new SavedParts { Parts = parts })).Parts;
            Assert.IsTrue(SkirmishNativeCheckpointParts.Validate(parts, new HashSet<string> { "refinery" }));
            using var target = new World("profile-continue"); var restored = target.EntityManager; var copy = restored.CreateEntity();
            SkirmishNativeCheckpointParts.Apply(restored, copy, parts, _ => Entity.Null);
            var profile = restored.GetComponentData<BuildingResourceProductionProfileComponent>(copy);
            var storage = restored.GetComponentData<BuildingResourceStorageComponent>(copy);
            Assert.AreEqual(23, profile.FuelBatchProgressSeconds);
            Assert.AreEqual(60, BuildingResourceProductionEcsSystem.ApplyProfileTick(ref storage, ref profile, 7).FuelProducedBarrels);
            Assert.AreEqual(0, storage.StoredOilBarrels);
        }

        [Test]
        public void ExpandedOilRoutesFundOneMaterialsBatchThenTheFullRefineryBatch()
        {
            using var world = new World(nameof(ExpandedOilRoutesFundOneMaterialsBatchThenTheFullRefineryBatch));
            var em = world.EntityManager;
            var attempt = new FixedString64Bytes("oil-route");
            var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = attempt });
            var factory = em.CreateEntity(typeof(MaterialFabricationComponent), typeof(MaterialFabricationInputTag),
                typeof(BuildingResourceStorageComponent), typeof(SkirmishAttemptOwnedComponent));
            em.SetComponentData(factory, new MaterialFabricationComponent { OwnerFactionId = 1,
                ProductionEnabled = 1, OilConsumedPerCycle = 10 });
            em.SetComponentData(factory, new SkirmishAttemptOwnedComponent { SessionId = attempt, FactionId = 1 });
            em.SetComponentData(factory, new BuildingResourceStorageComponent { OwnerFactionId = 1, OilStorageCapacity = 200 });
            var refinery = em.CreateEntity(typeof(BuildingResourceProductionProfileComponent),
                typeof(BuildingResourceStorageComponent), typeof(SkirmishAttemptOwnedComponent));
            em.SetComponentData(refinery, new BuildingResourceProductionProfileComponent { OilPerFuelBatch = 30, FuelPerBatch = 60 });
            em.SetComponentData(refinery, new SkirmishAttemptOwnedComponent { SessionId = attempt, FactionId = 1 });
            var refineryStock = new BuildingResourceStorageComponent { OwnerFactionId = 1,
                OilStorageCapacity = 5000, FuelStorageCapacity = 5000, StoredOilBarrels = 12 };
            em.SetComponentData(refinery, refineryStock);
            var factoryView = new RuntimeBuildingEntity { CombatEntity = factory, OwnerFactionId = 1 };
            var refineryView = new RuntimeBuildingEntity { CombatEntity = refinery, OwnerFactionId = 1 };
            // A funded foreign attempt cannot divert this session's first Materials batch.
            var foreign = em.Instantiate(factory);
            em.SetComponentData(foreign, new SkirmishAttemptOwnedComponent { SessionId = new FixedString64Bytes("foreign"), FactionId = 1 });
            em.SetComponentData(foreign, new BuildingResourceStorageComponent { OwnerFactionId = 1, StoredOilBarrels = 10 });
            Assert.IsTrue(ResourceHaulerAutomaticRoutePolicySystemHelper.TryResolveExpandedOilDemand(em, factoryView, 8, out int materialPriority, out _));
            Assert.IsTrue(ResourceHaulerAutomaticRoutePolicySystemHelper.TryResolveExpandedOilDemand(em, refineryView, 8, out int fuelPriority, out float required));
            Assert.Greater(materialPriority, fuelPriority);
            Assert.AreEqual(30, required, "12 Oil is enough for a truckload but not the declared refinery batch");
            var factoryStock = em.GetComponentData<BuildingResourceStorageComponent>(factory);
            factoryStock.ReservedOilInboundBarrels = 10;
            em.SetComponentData(factory, factoryStock);
            ResourceHaulerAutomaticRoutePolicySystemHelper.TryResolveExpandedOilDemand(em, factoryView, 8, out materialPriority, out _);
            ResourceHaulerAutomaticRoutePolicySystemHelper.TryResolveExpandedOilDemand(em, refineryView, 8, out fuelPriority, out _);
            Assert.Greater(fuelPriority, materialPriority, "one physically reserved Materials batch allows Fuel supply next");
            refineryStock.ReservedOilInboundBarrels = 18;
            em.SetComponentData(refinery, refineryStock);
            ResourceHaulerAutomaticRoutePolicySystemHelper.TryResolveExpandedOilDemand(em, refineryView, 8, out fuelPriority, out _);
            Assert.AreEqual(0, fuelPriority, "the committed full batch does not attract every remaining truck");
            refineryStock.ReservedOilInboundBarrels = 0; refineryStock.StoredFuelBarrels = 5000;
            em.SetComponentData(refinery, refineryStock);
            ResourceHaulerAutomaticRoutePolicySystemHelper.TryResolveExpandedOilDemand(em, refineryView, 8, out fuelPriority, out _);
            Assert.AreEqual(0, fuelPriority, "full Fuel output storage cannot demand another batch");
            em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = attempt, IsLegacy = 1 });
            Assert.IsFalse(ResourceHaulerAutomaticRoutePolicySystemHelper.TryResolveExpandedOilDemand(em, refineryView, 8, out _, out _));
        }

        public static void RunFocusedValidation()
        {
            var tests = new SkirmishFacilityEconomyProfileTests();
            tests.BothFactionsAndExpansionFacilitiesUseSelectedSizeWithoutStockGrants();
            tests.LegacyAndOtherAttemptBindingsKeepAuthoredRecipes();
            tests.ProfiledPumpAndCampaignNeighborKeepDifferentClocksAndRates();
            tests.HaulerMetadataSyncPreservesProfileWithoutChangingSourceDefinition();
            tests.RefineryWaitsForFullBatchInputAndCapacityAndPreservesReservations();
            tests.NativeCheckpointPreservesRefineryBatchProgressAcrossFreshWorld();
            tests.ExpandedOilRoutesFundOneMaterialsBatchThenTheFullRefineryBatch();
            Debug.Log("[SkirmishFacilityEconomyProfileTests] result=Passed tests=7");
        }
    }
}
