using Game.Components;

namespace Game.Runtime
{
    public static class FactionConstructionResourceUtilitySystemHelper
    {
        public static FactionConstructionResourceMutationResult Evaluate(
            in FactionEconomy economy,
            in FactionTacticalMaterialsComponent materials,
            int creditsCost,
            int materialsCost)
        {
            if (creditsCost < 0 || materialsCost < 0)
                return FactionConstructionResourceMutationResult.InvalidCost;
            if (economy.FactionId != materials.FactionId ||
                economy.Money < 0 ||
                !FactionTacticalMaterialsUtilitySystemHelper.CanAfford(materials, 0))
                return FactionConstructionResourceMutationResult.InvalidState;

            bool lacksCredits = creditsCost > economy.Money;
            bool lacksMaterials = !FactionTacticalMaterialsUtilitySystemHelper.CanAfford(materials, materialsCost);
            if (lacksCredits && lacksMaterials)
                return FactionConstructionResourceMutationResult.InsufficientCreditsAndMaterials;
            if (lacksCredits)
                return FactionConstructionResourceMutationResult.InsufficientCredits;
            if (lacksMaterials)
                return FactionConstructionResourceMutationResult.InsufficientMaterials;

            return FactionConstructionResourceMutationResult.Applied;
        }

        public static FactionConstructionResourceMutationResult TrySpend(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            int creditsCost,
            int materialsCost)
        {
            FactionConstructionResourceMutationResult evaluation =
                Evaluate(economy, materials, creditsCost, materialsCost);
            if (evaluation != FactionConstructionResourceMutationResult.Applied)
                return evaluation;

            FactionEconomy nextEconomy = economy;
            FactionTacticalMaterialsComponent nextMaterials = materials;
            nextEconomy.Money -= creditsCost;

            if (materialsCost > 0 &&
                FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                    ref nextMaterials,
                    materialsCost,
                    FactionTacticalMaterialsSpendKind.Construction) !=
                FactionTacticalMaterialsMutationResult.Applied)
                return FactionConstructionResourceMutationResult.InvalidState;

            economy = nextEconomy;
            materials = nextMaterials;
            return FactionConstructionResourceMutationResult.Applied;
        }

        public static FactionConstructionResourceMutationResult TryRollback(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            int creditsCost,
            int materialsCost)
        {
            if (creditsCost < 0 || materialsCost < 0)
                return FactionConstructionResourceMutationResult.InvalidCost;
            if (economy.FactionId != materials.FactionId ||
                economy.Money < 0 ||
                creditsCost > int.MaxValue - economy.Money)
                return FactionConstructionResourceMutationResult.InvalidState;

            FactionTacticalMaterialsComponent nextMaterials = materials;
            if (materialsCost > 0 &&
                FactionTacticalMaterialsUtilitySystemHelper.TryRefundConstruction(
                    ref nextMaterials,
                    materialsCost) != FactionTacticalMaterialsMutationResult.Applied)
                return FactionConstructionResourceMutationResult.InvalidState;

            economy.Money += creditsCost;
            materials = nextMaterials;
            return FactionConstructionResourceMutationResult.Applied;
        }
    }
}
