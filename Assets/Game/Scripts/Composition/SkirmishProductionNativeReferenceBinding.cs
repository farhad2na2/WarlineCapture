using System;
using Game.Authoring;
using Game.Configs;
using Game.Runtime;
using Game.Simulation;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Composition
{
    /// <summary>Validates the opt-in authored package against the catalog's resolved native role objects.</summary>
    public static class SkirmishProductionNativeReferenceBinding
    {
        public static SkirmishProductionCatalog Create(SkirmishResolvedSetup setup,
            BuildingPlacementSystemConfig baseline, BuildingPlacementSystemConfig scene,
            BuildingProductionQueueCompositionSystemHelper.TryGetUnitProductionMetadataDelegate timingResolver,
            OneProducerAuthoredReferencePackageConfig packageConfig)
        {
            OneProducerAuthoredReferencePackage package = packageConfig != null
                ? packageConfig.ReadAndValidatePackage()
                : null;
            var catalog = SkirmishProductionCatalog.Create(setup, baseline, scene, timingResolver);
            try
            {
                if (package != null)
                    RequirePackageBinding(catalog, packageConfig, package);
                return catalog;
            }
            catch
            {
                catalog.Dispose();
                throw;
            }
        }

        static void RequirePackageBinding(SkirmishProductionCatalog catalog,
            OneProducerAuthoredReferencePackageConfig packageConfig,
            OneProducerAuthoredReferencePackage package)
        {
            if (catalog == null || catalog.Setup == null)
                throw new InvalidOperationException("Resolved production catalog required for package binding");
            int matchingSetupRoles = 0;
            foreach (var role in catalog.Setup.RoleOverlays ?? Array.Empty<SkirmishRoleOverlay>())
                if (role.RoleKind == SkirmishRoleKind.Rifle && role.Producer == SkirmishProducerKind.Barracks)
                    matchingSetupRoles++;
            if (matchingSetupRoles != 1 ||
                !catalog.TryRole(package.unit.key, out var overlay) ||
                overlay.RoleKind != SkirmishRoleKind.Rifle || overlay.Producer != SkirmishProducerKind.Barracks ||
                overlay.SquadMembers != package.quantity || package.quantity != 4 ||
                package.unit.key != "Unit_Chr_Soldier_Male_02_Alt_04" ||
                package.producer.key != "Building_Barrack")
                throw new InvalidOperationException("Configured package requires exactly one eligible compiled Rifle/Barracks role");

            if (!catalog.TryGetRolePrefabs(package.unit.key, out var producer, out var unit))
                throw new InvalidOperationException("Compiled native Rifle/Barracks prefab binding is missing");
            var producerAuthoring = producer.GetComponent<BuildingDefinitionAuthoring>();
            var unitAuthoring = unit.GetComponent<UnitGridAuthoring>();
            if (producerAuthoring == null || unitAuthoring == null)
                throw new InvalidOperationException("Current native role authoring components are required");
            var recipe = producerAuthoring.GetProductionOrDefault(package.recipeSlot);
            var transport = unitAuthoring.ProductionTransportPrefab;
            var transportAuthoring = transport != null
                ? transport.GetComponent<UnitGridAuthoring>()
                : null;
            if (recipe == null || recipe.spawnUnitPrefab != unit || recipe.quantity != overlay.SquadMembers ||
                transportAuthoring == null ||
                !packageConfig.IsBoundToNativeReferences(package, producer, unit, transport,
                    producerAuthoring.AuthoringConfig, unitAuthoring.AuthoringConfig,
                    transportAuthoring.AuthoringConfig, package.recipeSlot, recipe.quantity))
                throw new InvalidOperationException("Configured generic authored-reference package does not bind the resolved native role objects");
        }
    }
}
