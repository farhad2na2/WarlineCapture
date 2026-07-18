using Game.Components;
using UnityEngine;

namespace Game.Configs
{
    public readonly struct ResourceExchangeMaterialsBalanceResult
    {
        public readonly float LocalMaterialsPerOil;
        public readonly float ExchangeMaterialsPerOil;
        public readonly float ExchangeEfficiency;
        public readonly float MaterialsRoundTripRetention;

        public ResourceExchangeMaterialsBalanceResult(
            float localMaterialsPerOil,
            float exchangeMaterialsPerOil,
            float exchangeEfficiency,
            float materialsRoundTripRetention)
        {
            LocalMaterialsPerOil = localMaterialsPerOil;
            ExchangeMaterialsPerOil = exchangeMaterialsPerOil;
            ExchangeEfficiency = exchangeEfficiency;
            MaterialsRoundTripRetention = materialsRoundTripRetention;
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

            if (!depotConfig.MaterialFabricationEnabled ||
                depotConfig.MaterialFabricationOilConsumedPerCycle <= 0f ||
                depotConfig.MaterialFabricationMaterialsOutputPerCycle <= 0)
            {
                return ResourceExchangeReason.InvalidRate;
            }

            ResourceExchangeRecipeConfigEntry oilToMaterials = null;
            ResourceExchangeRecipeConfigEntry materialsToOil = null;
            for (int i = 0; i < exchangeConfig.Recipes.Count; i++)
            {
                ResourceExchangeRecipeConfigEntry recipe = exchangeConfig.Recipes[i];
                if (recipe == null || !string.Equals(recipe.MissionTag, scenarioTag, System.StringComparison.Ordinal))
                    continue;

                if (recipe.InputResource == ResourceExchangeResourceKind.Oil &&
                    recipe.OutputResource == ResourceExchangeResourceKind.Materials)
                {
                    oilToMaterials = recipe;
                }
                else if (recipe.InputResource == ResourceExchangeResourceKind.Materials &&
                         recipe.OutputResource == ResourceExchangeResourceKind.Oil)
                {
                    materialsToOil = recipe;
                }
            }

            if (oilToMaterials == null || materialsToOil == null)
                return ResourceExchangeReason.InvalidRecipe;

            float localMaterialsPerOil =
                depotConfig.MaterialFabricationMaterialsOutputPerCycle /
                depotConfig.MaterialFabricationOilConsumedPerCycle;
            float exchangeMaterialsPerOil = EffectiveOutputPerInput(oilToMaterials);
            float exchangeOilPerMaterial = EffectiveOutputPerInput(materialsToOil);
            if (localMaterialsPerOil <= 0f || exchangeMaterialsPerOil <= 0f || exchangeOilPerMaterial <= 0f)
                return ResourceExchangeReason.InvalidRate;

            float exchangeEfficiency = exchangeMaterialsPerOil / localMaterialsPerOil;
            float roundTripRetention = exchangeMaterialsPerOil * exchangeOilPerMaterial;
            result = new ResourceExchangeMaterialsBalanceResult(
                localMaterialsPerOil,
                exchangeMaterialsPerOil,
                exchangeEfficiency,
                roundTripRetention);

            if (exchangeEfficiency >= 1f ||
                roundTripRetention > ResourceExchangeRecipeConfigValidator.MaximumRoundTripResourceRetention)
            {
                return ResourceExchangeReason.InvalidRate;
            }

            return ResourceExchangeReason.None;
        }

        private static float EffectiveOutputPerInput(ResourceExchangeRecipeConfigEntry recipe)
        {
            return recipe.OutputPerInput * (1f - Mathf.Clamp(recipe.FeePercent, 0f, 0.95f));
        }
    }
}
