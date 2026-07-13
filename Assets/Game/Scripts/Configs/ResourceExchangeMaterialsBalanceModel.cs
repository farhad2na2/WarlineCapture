using Game.Components;
using UnityEngine;

namespace Game.Configs
{
    public readonly struct ResourceExchangeMaterialsBalanceResult
    {
        public readonly float LocalCreditsPerMaterial;
        public readonly float ImportCreditsPerMaterial;
        public readonly float ImportMarkup;
        public readonly float MaterialsRoundTripRetention;
        public readonly float OilDirectExportCreditsPerBarrel;
        public readonly float OilFabricateExportCreditsPerBarrel;

        public ResourceExchangeMaterialsBalanceResult(
            float localCreditsPerMaterial,
            float importCreditsPerMaterial,
            float importMarkup,
            float materialsRoundTripRetention,
            float oilDirectExportCreditsPerBarrel,
            float oilFabricateExportCreditsPerBarrel)
        {
            LocalCreditsPerMaterial = localCreditsPerMaterial;
            ImportCreditsPerMaterial = importCreditsPerMaterial;
            ImportMarkup = importMarkup;
            MaterialsRoundTripRetention = materialsRoundTripRetention;
            OilDirectExportCreditsPerBarrel = oilDirectExportCreditsPerBarrel;
            OilFabricateExportCreditsPerBarrel = oilFabricateExportCreditsPerBarrel;
        }
    }

    public static class ResourceExchangeMaterialsBalanceModel
    {
        public static ResourceExchangeReason Evaluate(
            ResourceExchangeRecipeConfigSet exchangeConfig,
            BuildingDefinitionAuthoringConfig depotConfig,
            string scenarioTag,
            out ResourceExchangeMaterialsBalanceResult result)
        {
            result = default;
            if (exchangeConfig == null || depotConfig == null || string.IsNullOrWhiteSpace(scenarioTag))
                return ResourceExchangeReason.InvalidRecipe;

            ResourceExchangeMaterialsBalanceConfig balance = exchangeConfig.MaterialsBalance;
            if (balance == null ||
                !depotConfig.MaterialFabricationEnabled ||
                depotConfig.MaterialFabricationOilConsumedPerCycle <= 0f ||
                depotConfig.MaterialFabricationMaterialsOutputPerCycle <= 0)
            {
                return ResourceExchangeReason.InvalidRate;
            }

            ResourceExchangeRecipeConfigEntry importMaterials = null;
            ResourceExchangeRecipeConfigEntry exportMaterials = null;
            ResourceExchangeRecipeConfigEntry exportOil = null;
            for (int i = 0; i < exchangeConfig.Recipes.Count; i++)
            {
                ResourceExchangeRecipeConfigEntry recipe = exchangeConfig.Recipes[i];
                if (recipe == null || !string.Equals(recipe.MissionTag, scenarioTag, System.StringComparison.Ordinal))
                    continue;

                if (recipe.RouteType == ResourceExchangeRouteType.Import &&
                    recipe.InputResource == ResourceExchangeResourceKind.Credits &&
                    recipe.OutputResource == ResourceExchangeResourceKind.Materials)
                {
                    importMaterials = recipe;
                }
                else if (recipe.RouteType == ResourceExchangeRouteType.Export &&
                         recipe.InputResource == ResourceExchangeResourceKind.Materials &&
                         recipe.OutputResource == ResourceExchangeResourceKind.Credits)
                {
                    exportMaterials = recipe;
                }
                else if (recipe.RouteType == ResourceExchangeRouteType.Export &&
                         recipe.InputResource == ResourceExchangeResourceKind.Oil &&
                         recipe.OutputResource == ResourceExchangeResourceKind.Credits)
                {
                    exportOil = recipe;
                }
            }

            if (importMaterials == null || exportMaterials == null || exportOil == null)
                return ResourceExchangeReason.InvalidRecipe;

            float materialsPerOil =
                depotConfig.MaterialFabricationMaterialsOutputPerCycle /
                depotConfig.MaterialFabricationOilConsumedPerCycle;
            float localOilCost =
                balance.OilOpportunityCreditsPerBarrel /
                materialsPerOil;
            float depotCost =
                depotConfig.Price /
                (balance.DepotAmortizationCycles *
                 (float)depotConfig.MaterialFabricationMaterialsOutputPerCycle);
            float localCreditsPerMaterial =
                localOilCost + depotCost + balance.LogisticsCreditsPerMaterial;
            float importMaterialsPerCredit = EffectiveOutputPerInput(importMaterials);
            if (localCreditsPerMaterial <= 0f || importMaterialsPerCredit <= 0f)
                return ResourceExchangeReason.InvalidRate;

            float importCreditsPerMaterial = 1f / importMaterialsPerCredit;
            float importMarkup = importCreditsPerMaterial / localCreditsPerMaterial;
            float materialsExportCredits = EffectiveOutputPerInput(exportMaterials);
            float oilDirectExportCredits = EffectiveOutputPerInput(exportOil);
            float materialsRoundTripRetention = materialsExportCredits * importMaterialsPerCredit;
            float oilFabricateExportCredits = materialsPerOil * materialsExportCredits;

            result = new ResourceExchangeMaterialsBalanceResult(
                localCreditsPerMaterial,
                importCreditsPerMaterial,
                importMarkup,
                materialsRoundTripRetention,
                oilDirectExportCredits,
                oilFabricateExportCredits);

            if (importMarkup < balance.MinimumImportMarkup || importMarkup > balance.MaximumImportMarkup)
                return ResourceExchangeReason.InvalidRate;
            if (materialsRoundTripRetention > ResourceExchangeRecipeConfigValidator.MaximumRoundTripResourceRetention)
                return ResourceExchangeReason.InvalidRate;
            if (oilFabricateExportCredits > oilDirectExportCredits)
                return ResourceExchangeReason.InvalidRate;

            return ResourceExchangeReason.None;
        }

        private static float EffectiveOutputPerInput(ResourceExchangeRecipeConfigEntry recipe)
        {
            return recipe.OutputPerInput * (1f - Mathf.Clamp(recipe.FeePercent, 0f, 0.95f));
        }
    }
}
