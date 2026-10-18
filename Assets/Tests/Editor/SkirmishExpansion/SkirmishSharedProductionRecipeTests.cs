using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishSharedProductionRecipeTests
    {

        public static void RunStartingStructureCatalogValidation()
        {
            var tests = new SkirmishSharedProductionRecipeTests();
            tests.PackagedS002StartingStructuresResolveWithoutAddingProductionRoles();
            tests.MissingAuthoredStartingStructureRefusesWithoutMutatingCatalogs();
            Debug.Log("[SkirmishStartingStructureCatalogControls] result=Passed controls=2 normalMissionPassed=0");
        }

        [Test]
        public void PackagedS002StartingStructuresResolveWithoutAddingProductionRoles()
        {
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string error), error);
            var definition = Resources.Load<SkirmishScenarioDefinitionConfig>(SkirmishExpansionCatalogFactory.PackagedS002DefinitionPath);
            Assert.NotNull(definition);
            var manifest = new Game.Skirmish.Contracts.SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,
                Game.Skirmish.Contracts.SkirmishDifficultyId.Regular,
                Game.Skirmish.Contracts.SkirmishSizeId.Standard, 155923, manifest, matrix,
                out var setup, out var reasons), string.Join("; ", reasons));
            var baseline = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                "Assets/Game/Configs/Skirmish/Construction.asset");
            var scene = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
            var satellite = scene.Spawnables.Find(p => p != null && p.name == "Building_Satelite_Dish");
            Assert.NotNull(satellite);
            Assert.IsFalse(baseline.Spawnables.Contains(satellite));
            var before = baseline.Spawnables.ToArray();
            int roleCount = setup.RoleOverlays.Length;
            using var catalog = SkirmishProductionCatalog.Create(setup, baseline, scene);
            Assert.IsTrue(catalog.Config.Spawnables.Contains(satellite));
            Assert.IsFalse(catalog.TryRole("Unit_Veh_Drone", out _));
            var definitions = new BuildingDefinitionPrefabSystemHelper();
            definitions.ConfigureAuthoringMetadataResolvers(
                Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            definitions.RebuildSpawnablesLookup(catalog.Config.Spawnables, catalog.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs);
            try
            {
                definitions.RebuildConfiguredSpawnableDefinitions(null, o => Object.DestroyImmediate(o));
                foreach (var structure in setup.Structures)
                {
                    string key = Game.Skirmish.Contracts.SkirmishStructureIds.VisualKey(structure.StructureId);
                    var prefab = catalog.Config.Spawnables.Find(p => p != null && p.name == key);
                    Assert.NotNull(prefab, key);
                    Assert.IsTrue(Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata(prefab, out var metadata), key);
                    Assert.IsTrue(definitions.TryGetConfiguredDefinition(prefab, out var actual), key);
                    Assert.AreSame(prefab, actual.Prefab, key);
                    Assert.AreEqual(Mathf.Max(1, metadata.MaxHealth), actual.MaxHealth, key);
                    Assert.AreEqual(metadata.Role, actual.Role, key);
                    Assert.AreEqual(Mathf.Max(0, metadata.MaterialsCost), actual.MaterialsCost, key);
                    Assert.Greater(actual.FootprintCells.x, 0, key);
                    Assert.Greater(actual.FootprintCells.y, 0, key);
                }
            }
            finally { definitions.ClearConfiguredSpawnableDefinitions(o => Object.DestroyImmediate(o)); }
            Assert.AreEqual(roleCount, catalog.Config.RuntimeProductionRecipes.Count);
            CollectionAssert.AreEqual(before, baseline.Spawnables);
            Assert.AreEqual(roleCount, setup.RoleOverlays.Length);
        }

        [Test]
        public void MissingAuthoredStartingStructureRefusesWithoutMutatingCatalogs()
        {
            var baseline = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                "Assets/Game/Configs/Skirmish/Construction.asset");
            var scene = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
            var before = baseline.Spawnables.ToArray();
            int sceneCount = scene.Spawnables.Count;
            var missingSatelliteScene = Object.Instantiate(scene);
            try
            {
                Assert.Greater(missingSatelliteScene.Spawnables.RemoveAll(p => p != null && p.name == "Building_Satelite_Dish"), 0);
                var setup = new SkirmishResolvedSetup {
                    Structures = new[] { new SkirmishResolvedStructureEntry {
                        StructureId = Game.Skirmish.Contracts.SkirmishStructureIds.SatelliteDish, FactionId = 1 } },
                    RoleOverlays = System.Array.Empty<Game.Skirmish.Contracts.SkirmishRoleOverlay>()
                };
                var error = Assert.Throws<System.InvalidOperationException>(
                    () => SkirmishProductionCatalog.Create(setup, baseline, missingSatelliteScene));
                StringAssert.Contains("Building_Satelite_Dish", error.Message);
                CollectionAssert.AreEqual(before, baseline.Spawnables);
                Assert.AreEqual(sceneCount, scene.Spawnables.Count);
            }
            finally { Object.DestroyImmediate(missingSatelliteScene); }
        }

        public static void RunProducerMessageValidation()
        {
            var tests = new SkirmishSharedProductionRecipeTests();
            tests.MissingExpandedTankerProducerNamesGroundStagingBeforeLegacyFuelBladder();
            tests.ProducerMessageOverrideDoesNotApplyToLegacyWorld();
            tests.RuntimeRecipeChangesProducerPacketAndPriceWithoutMutatingAuthoredDefaults();
            Debug.Log("[SkirmishProducerMessageTests] result=Passed tests=3");
        }

        [Test]
        public void MissingExpandedTankerProducerNamesGroundStagingBeforeLegacyFuelBladder()
        {
            var baseline = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                "Assets/Game/Configs/Skirmish/Construction.asset");
            var scene = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
            var setup = new SkirmishResolvedSetup {
                Readiness = Game.Skirmish.Contracts.SkirmishReadinessStage.Field,
                ArmyProfileId = Game.Skirmish.Contracts.SkirmishArmyProfileId.AirMobile,
                RoleOverlays = SkirmishRoleOverlayCatalog.CreateAirMobileSlice()
            };
            using var catalog = SkirmishProductionCatalog.Create(setup, baseline, scene);
            using var world = new Unity.Entities.World("MissingTankerProducerMessage");
            var em = world.EntityManager;
            var session = em.CreateEntity(typeof(Game.Components.SkirmishExpandedSessionComponent));
            em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = setup });
            em.AddComponentObject(session, new SkirmishProductionCatalogRecord { Catalog = catalog });
            var tanker = catalog.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs.Find(p => p.name == "Unit_Veh_Truck_Tanker");
            var staging = catalog.Config.Spawnables.Find(p => p.name == "Building_GroundStaging");
            var fuel = catalog.Config.Spawnables.Find(p => p.name == "Building_Fuel_Bladder");
            Assert.NotNull(tanker); Assert.NotNull(staging); Assert.NotNull(fuel);
            var definitions = new[] {
                new BuildingDefinition { Prefab = fuel, DisplayName = "Fuel Bladder" },
                new BuildingDefinition { Prefab = staging, DisplayName = "Ground Staging" }
            };
            Assert.AreEqual(BuildingUiCommandSystemHelper.CampRequestFailure.MissingProducerBuilding,
                SkirmishNativeProduction.RequestFailure(em, tanker, false));
            Assert.IsTrue(SkirmishNativeProduction.TryRequiredProducerDisplayName(em, tanker, definitions, out var name));
            Assert.AreEqual("Ground Staging", name);
        }

        [Test]
        public void ProducerMessageOverrideDoesNotApplyToLegacyWorld()
        {
            using var world = new Unity.Entities.World("LegacyProducerMessage");
            var unit = new GameObject("Unit_Veh_Truck_Tanker");
            try
            {
                Assert.IsFalse(SkirmishNativeProduction.TryRequiredProducerDisplayName(world.EntityManager, unit,
                    System.Array.Empty<BuildingDefinition>(), out var name));
                Assert.IsEmpty(name);
            }
            finally { Object.DestroyImmediate(unit); }
        }

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
            Assert.AreEqual(BuildingUiCommandSystemHelper.CampRequestFailure.MissingProducerBuilding,
                SkirmishNativeProduction.RequestFailure(em, aaPrefab, false), "A missing producer is not an army-cap rejection.");
            var ground = em.CreateEntity(typeof(Game.Components.SkirmishStructureIdentityComponent),
                typeof(Game.Components.SkirmishAttemptOwnedComponent), typeof(Game.Components.UnitHealth));
            em.SetComponentData(ground, new Game.Components.SkirmishStructureIdentityComponent {
                Producer = Game.Skirmish.Contracts.SkirmishProducerKind.GroundStaging });
            em.SetComponentData(ground, new Game.Components.SkirmishAttemptOwnedComponent { FactionId = 2 });
            em.SetComponentData(ground, new Game.Components.UnitHealth { Current = 100, Max = 100 });
            Assert.AreEqual(BuildingUiCommandSystemHelper.CampRequestFailure.MissingProducerBuilding,
                SkirmishNativeProduction.RequestFailure(em, aaPrefab, false), "An enemy producer cannot supply player recruitment.");
            em.SetComponentData(ground, new Game.Components.SkirmishAttemptOwnedComponent { FactionId = 1 });
            Assert.AreEqual(BuildingUiCommandSystemHelper.CampRequestFailure.None,
                SkirmishNativeProduction.RequestFailure(em, aaPrefab, false));
            em.SetComponentData(ground, new Game.Components.UnitHealth { Current = 0, Max = 100 });
            Assert.AreEqual(BuildingUiCommandSystemHelper.CampRequestFailure.MissingProducerBuilding,
                SkirmishNativeProduction.RequestFailure(em, aaPrefab, false), "A destroyed producer must be reported before capacity.");
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
