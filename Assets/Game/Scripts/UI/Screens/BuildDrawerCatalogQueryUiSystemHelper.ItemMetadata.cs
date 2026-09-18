using System.Collections.Generic;
using Game.Catalog.Contracts;
using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class BuildDrawerCatalogQueryUiSystemHelper
    {
        private static int VisibleCreditsCost(int legacyCost) =>
            UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions) &&
            restrictions.UsesMaterialsOnlyConstruction ? 0 : legacyCost;

        private static BuildDrawerCatalogItem BuildBuildingItem(GameObject prefab, UiBuildingCatalogMetadata metadata)
        {
            return new BuildDrawerCatalogItem(
                BuildDrawerCategory.Buildings,
                prefab,
                string.IsNullOrWhiteSpace(metadata.DisplayName) ? prefab.name : metadata.DisplayName,
                ResolveBuildingTypeLabel(metadata),
                ResolveBuildingDescription(metadata),
                metadata.MaterialsCost,
                0,
                metadata.ProductionDurationSeconds,
                metadata.FootprintCells,
                metadata.Portrait,
                metadata.CardPortrait,
                metadata.ActionPortrait, VisibleCreditsCost(metadata.Price));
        }

        private static BuildDrawerCatalogItem BuildUnitItem(
            GameObject prefab,
            UiUnitCatalogMetadata metadata,
            BuildDrawerCategory category,
            bool isVehicle,
            bool isAir)
        {
            return new BuildDrawerCatalogItem(
                category,
                prefab,
                category == BuildDrawerCategory.Soldiers && UiShellRuntimeGateway.TryReadSkirmish(out _)
                    ? "Rifle squad (4)" : ResolveUnitDisplayName(prefab, metadata),
                ResolveUnitTypeLabel(prefab, metadata, isVehicle, isAir),
                ResolveUnitDescription(prefab, metadata),
                metadata.MaterialsCost,
                0,
                metadata.ProductionDurationSeconds,
                metadata.FootprintCells,
                metadata.Portrait,
                metadata.CardPortrait,
                metadata.ActionPortrait, VisibleCreditsCost(metadata.CreditsCost));
        }
    }
}
