using System.Collections.Generic;
using System.Linq;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Gate 3 "available" model (lead-s002-gate3): role.convoy_logic was listed for S002 only because it
    // shares its runtime prefab with role.logistics_truck; army profile G does not allow it, so the
    // production catalog has no convoy recipe. Support trucks haul on their own and are reported apart.
    public sealed class SkirmishGateUnitsModelTests
    {
        public static void RunObjectiveBudgetValidation()
        {
            new SkirmishGateUnitsModelTests().ObjectiveRunCoverageCostsExceedStartingMaterialsAndFabrication();
            Debug.Log("[SkirmishGate3ObjectiveBudget] result=Passed missions=3");
        }

        [Test]
        public void ObjectiveRunCoverageCostsExceedStartingMaterialsAndFabrication()
        {
            // Lane 28 S007 and lane 29 S013/S019. The credited sets include automatic haulers.
            var runs = new[] {
                (Id: "S007", Seconds: 593.496, Misses: 9, Cost: 1845,
                    Credited: "apc_armored apc_fast car gunner marksman rifle rocketeer tanker logistics_truck"),
                (Id: "S013", Seconds: 376.854, Misses: 11, Cost: 2165,
                    Credited: "apc_armored breacher marksman rifle rocketeer tanker logistics_truck"),
                (Id: "S019", Seconds: 115.192, Misses: 14, Cost: 2565,
                    Credited: "breacher convoy_logic rocketeer tanker logistics_truck")
            };
            var missions = SkirmishExpansionCatalogConfig.Load();
            Assert.NotNull(missions);
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string error), error);
            var preset = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
            Assert.NotNull(preset);
            var scene = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");

            foreach (var run in runs)
            {
                Assert.IsTrue(missions.TryGetDefinition(run.Id, out var definition), run.Id);
                var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
                Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular,
                    SkirmishSizeId.Standard, 130365, manifest, matrix, out var setup, out var reasons), string.Join("; ", reasons));
                using var catalog = SkirmishProductionCatalog.Create(setup, preset.buildingPlacement, scene);
                var unused = new HashSet<string>();
                SkirmishS002AriaRunHarness.GateAvailableTypes(setup, catalog, unused);
                unused.ExceptWith(run.Credited.Split(' ').Select(role => "role." + role));
                Assert.AreEqual(run.Misses, unused.Count, run.Id);

                int cost = 0;
                foreach (string type in unused)
                {
                    if (type.StartsWith("building.", System.StringComparison.Ordinal))
                    {
                        string key = SkirmishStructureIds.VisualKey(type);
                        var producer = catalog.Config.Spawnables.Single(prefab => prefab != null && prefab.name == key);
                        Assert.IsTrue(BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata(producer, out var metadata), type);
                        cost += metadata.MaterialsCost;
                    }
                    else
                    {
                        var recipe = catalog.Config.RuntimeProductionRecipes.Single(item =>
                            catalog.TryRole(item.UnitPrefab.name, out var role) && role.RoleId == type);
                        cost += recipe.MaterialsCost;
                    }
                }

                Assert.AreEqual(run.Cost, cost, run.Id);
                Assert.AreEqual(1, setup.Structures.Count(structure => structure.FactionId == setup.PlayerFaction &&
                    structure.StructureId == SkirmishStructureIds.FabricationDepot), run.Id);
                // Credit every possible 30 s cycle at Standard's 50-material output, even with interrupted supply.
                int maxStartingDepotOutput = 50 * (int)(run.Seconds / 30);
                Assert.Less(setup.MaterialsEach + maxStartingDepotOutput, cost, run.Id);
            }
        }

        [Test]
        public void SharedTruckPrefabDoesNotListConvoyLogic()
        {
            Assert.AreEqual(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.LogisticsTruck),
                SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.ConvoyLogic),
                "the collision this model guards against");
            Assert.IsFalse(SkirmishS002AriaRunHarness.GateCatalogListsRole(SkirmishRoleKind.ConvoyLogic, SkirmishRoleKind.LogisticsTruck));
            Assert.IsTrue(SkirmishS002AriaRunHarness.GateCatalogListsRole(SkirmishRoleKind.LogisticsTruck, SkirmishRoleKind.LogisticsTruck));
            Assert.IsTrue(SkirmishS002AriaRunHarness.GateCatalogListsRole(SkirmishRoleKind.Marksman, SkirmishRoleKind.Marksman));
        }

        [Test]
        public void OnlyLogisticsSupportIsReportedAsAutomatic()
        {
            Assert.IsTrue(SkirmishS002AriaRunHarness.GateSupportRole(SkirmishRoleKind.LogisticsTruck));
            Assert.IsTrue(SkirmishS002AriaRunHarness.GateSupportRole(SkirmishRoleKind.Tanker));
            Assert.IsFalse(SkirmishS002AriaRunHarness.GateSupportRole(SkirmishRoleKind.Radar), "radar must be ordered by the player");
            Assert.IsFalse(SkirmishS002AriaRunHarness.GateSupportRole(SkirmishRoleKind.ConvoyLogic));
            Assert.IsFalse(SkirmishS002AriaRunHarness.GateSupportRole(SkirmishRoleKind.Marksman));
        }
    }
}
