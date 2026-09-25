using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishSharedProductionRecipeTests
    {
        [Test]
        public void RuntimeRecipeChangesProducerPacketAndPriceWithoutMutatingAuthoredDefaults()
        {
            var producer = new GameObject("Building_Barrack");
            var rifle = new GameObject("Unit_Soldier_Rifle");
            var rocketeer = new GameObject("Unit_Soldier_Rocketeer");
            try
            {
                var definitions = new BuildingDefinitionPrefabSystemHelper();
                definitions.ConfigureAuthoringMetadataResolvers(
                    (GameObject prefab, out BuildingDefinitionPrefabSystemHelper.BuildingDefinitionMetadata metadata) => {
                        metadata = new BuildingDefinitionPrefabSystemHelper.BuildingDefinitionMetadata {
                            DisplayName = "Barracks", FootprintCells = Vector2Int.one, MaxHealth = 800,
                            ProductionSpawnUnitPrefabs = new[] { rifle }, ProductionQuantities = new[] { 1 }
                        }; return true;
                    },
                    (GameObject prefab, out BuildingDefinitionPrefabSystemHelper.UnitDefinitionMetadata metadata) => {
                        metadata = new BuildingDefinitionPrefabSystemHelper.UnitDefinitionMetadata {
                            DisplayName = prefab.name, Price = 900, CreditsCost = 50, CanRequest = true,
                            FootprintCells = Vector2Int.one
                        }; return true;
                    });
                var recipes = new[] {
                    new BuildingProductionRecipe { ProducerPrefab = producer, UnitPrefab = rifle, Quantity = 4, MaterialsCost = 80 },
                    new BuildingProductionRecipe { ProducerPrefab = producer, UnitPrefab = rocketeer, Quantity = 4, MaterialsCost = 120 }
                };
                definitions.ConfigureProductionRecipes(recipes);
                definitions.RebuildSpawnablesLookup(new[] { producer }, new[] { rifle, rocketeer });
                var expanded = definitions.CreateRuntimeBuildingDefinition(producer, "", "", Vector2Int.one, 1, null);
                Assert.AreEqual(2, expanded.ProductionSlots.Count);
                Assert.AreEqual(4, expanded.ProductionSlots[1].Quantity);
                Assert.AreSame(rocketeer, expanded.ProductionSlots[1].SpawnUnitPrefab);
                Assert.IsTrue(definitions.TryResolveConfiguredUnitResourceCosts(rocketeer, 999, out int credits, out int materials));
                Assert.AreEqual(0, credits);
                Assert.AreEqual(120, materials);
                Assert.IsTrue(definitions.TryGetConfiguredUnitReadModel(1, out _, out _, out int displayedPrice, out _, out _));
                Assert.AreEqual(materials, displayedPrice);
                Assert.Throws<System.ArgumentException>(() => definitions.ConfigureProductionRecipes(new[] { recipes[0], recipes[0] }));
                Assert.IsTrue(definitions.TryResolveConfiguredUnitResourceCosts(rocketeer, 999, out _, out materials));
                Assert.AreEqual(120, materials, "Rejected recipes must not partly replace the working catalog.");
                definitions.ConfigureProductionRecipes(null);
                var authored = definitions.CreateRuntimeBuildingDefinition(producer, "", "", Vector2Int.one, 1, null);
                Assert.AreEqual(1, authored.ProductionSlots.Count);
                Assert.AreEqual(1, authored.ProductionSlots[0].Quantity);
                Assert.IsTrue(definitions.TryResolveConfiguredUnitResourceCosts(rocketeer, 999, out credits, out materials));
                Assert.AreEqual(50, credits);
                Assert.AreEqual(900, materials);
                Assert.AreEqual(4, expanded.ProductionSlots[1].Quantity);
            }
            finally
            {
                Object.DestroyImmediate(producer); Object.DestroyImmediate(rifle); Object.DestroyImmediate(rocketeer);
            }
        }

        [Test]
        public void AuthoredAirMobileCatalogResolvesEveryMissionRecipe()
        {
            var baseline = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                "Assets/Game/Configs/Skirmish/Construction.asset");
            var scene = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
            int originalBuildings = baseline.Spawnables.Count;
            int originalUnits = scene.UnitPrefabRegistryConfig.UnitSpawnPrefabs.Count;
            var setup = new SkirmishResolvedSetup {
                ArmyProfileId = Game.Skirmish.Contracts.SkirmishArmyProfileId.AirMobile,
                RoleOverlays = SkirmishRoleOverlayCatalog.CreateAirMobileSlice()
            };
            using var catalog = SkirmishProductionCatalog.Create(setup, baseline, scene);
            Assert.AreEqual(19, catalog.Config.RuntimeProductionRecipes.Count);
            Assert.IsTrue(catalog.TryRole("Unit_Veh_Missle_Launcher_Air", out var aa));
            Assert.AreEqual(220, aa.MaterialsCost);
            var queues = new BuildingProductionQueueCompositionSystemHelper();
            queues.ConfigureProductionRecipes(catalog.Config.RuntimeProductionRecipes);
            var aaPrefab = catalog.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs.Find(p => p.name == "Unit_Veh_Missle_Launcher_Air");
            var delivery = queues.ResolveProductionTransportSettings(aaPrefab,
                catalog.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs, null, null);
            Assert.IsNull(delivery.TransportPrefab, "Field AA leaves its Ground Staging producer without requiring an Airport.");
            Assert.IsFalse(delivery.RequiresAirportRunway);

            Assert.IsFalse(catalog.TryRole("Unit_Veh_Tank_USA", out _));
            Assert.AreEqual(originalBuildings, baseline.Spawnables.Count);
            Assert.AreEqual(originalUnits, scene.UnitPrefabRegistryConfig.UnitSpawnPrefabs.Count);
            using var world = new Unity.Entities.World("NativeProductionReadiness");
            var em = world.EntityManager;
            var session = em.CreateEntity(typeof(Game.Components.SkirmishExpandedSessionComponent));
            em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = setup });
            em.AddComponentObject(session, new SkirmishProductionCatalogRecord { Catalog = catalog });
            em.AddComponentData(session, new Game.Components.SkirmishResearchStateComponent {
                FactionId = 1, Readiness = Game.Skirmish.Contracts.SkirmishReadinessStage.Field });
            var pad = catalog.Config.Spawnables.Find(item => item.name == "Building_Helipad");
            var airport = catalog.Config.Spawnables.Find(item => item.name == "Building_Airport");
            Assert.AreEqual(BuildingUiCommandSystemHelper.CampRequestFailure.ReadinessEstablishedRequired,
                SkirmishNativeProduction.RequestFailure(em, pad, true));
            Assert.AreEqual(BuildingUiCommandSystemHelper.CampRequestFailure.ReadinessFullArsenalRequired,
                SkirmishNativeProduction.RequestFailure(em, airport, true));
            em.SetComponentData(session, new Game.Components.SkirmishResearchStateComponent {
                FactionId = 1, Readiness = Game.Skirmish.Contracts.SkirmishReadinessStage.Established });
            Assert.AreEqual(BuildingUiCommandSystemHelper.CampRequestFailure.None,
                SkirmishNativeProduction.RequestFailure(em, pad, true));
            Assert.AreEqual(BuildingUiCommandSystemHelper.CampRequestFailure.ReadinessFullArsenalRequired,
                SkirmishNativeProduction.RequestFailure(em, airport, true));
            foreach (var failure in new[] {
                BuildingUiCommandSystemHelper.CampRequestFailure.ReadinessEstablishedRequired,
                BuildingUiCommandSystemHelper.CampRequestFailure.ReadinessFullArsenalRequired,
                BuildingUiCommandSystemHelper.CampRequestFailure.ArmyCapacityReached,
                BuildingUiCommandSystemHelper.CampRequestFailure.ResearchInProgress })
                Assert.AreEqual(failure, BuildingCampItemCommandPolicySystemHelper.ToRequestFailure(
                    BuildingCampItemCommandPolicySystemHelper.ToResultCode(failure, true)));
        }

        [Test]
        public void CatalogOverlayLeavesOriginalBuildingListAndRegistryUntouched()
        {
            var original = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
            var registry = ScriptableObject.CreateInstance<UnitPrefabRegistryAuthoringConfig>();
            var barracks = new GameObject("Building_Barrack");
            var pad = new GameObject("Building_Helipad");
            BuildingPlacementSystemConfig overlay = null;
            try
            {
                original.Spawnables.Add(barracks);
                overlay = BuildingPlacementSystemConfig.CreateRuntimeCatalogOverlay(original, registry,
                    new[] { barracks, pad }, System.Array.Empty<BuildingProductionRecipe>());
                Assert.AreEqual(1, original.Spawnables.Count);
                Assert.IsNull(original.UnitPrefabRegistryConfig);
                Assert.AreEqual(2, overlay.Spawnables.Count);
                Assert.AreSame(registry, overlay.UnitPrefabRegistryConfig);
                overlay.Spawnables.Clear();
                Assert.AreEqual(1, original.Spawnables.Count);
            }
            finally
            {
                if (overlay != null) Object.DestroyImmediate(overlay);
                Object.DestroyImmediate(original); Object.DestroyImmediate(registry);
                Object.DestroyImmediate(barracks); Object.DestroyImmediate(pad);
            }
        }
    }
}
