using Game.Components;
using Game.Tactical.Contracts;
using ResourceResult = Game.Components.FactionConstructionResourceMutationResult;

namespace Game.Runtime
{
    public static class FactionConstructionResourceUtilitySystemHelper
    {
        public static ResourceResult Evaluate(
            in FactionEconomy economy,
            in FactionTacticalMaterialsComponent materials,
            int creditsCost,
            int materialsCost)
        {
            if (!MissionConstructionCostPolicy.TryResolve(economy.MaterialsOnlyConstruction,
                    creditsCost, materialsCost, out creditsCost, out materialsCost))
                return ResourceResult.InvalidCost;
            if (economy.FactionId != materials.FactionId ||
                economy.Money < 0 ||
                !FactionTacticalMaterialsUtilitySystemHelper.CanAfford(materials, 0))
                return ResourceResult.InvalidState;

            bool lacksCredits = creditsCost > economy.Money;
            if (!FactionTacticalMaterialsUtilitySystemHelper.CanAfford(materials, materialsCost))
                return lacksCredits ? ResourceResult.InsufficientCreditsAndMaterials
                    : ResourceResult.InsufficientMaterials;
            return lacksCredits ? ResourceResult.InsufficientCredits
                : ResourceResult.Applied;
        }

        public static ResourceResult TrySpend(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            int creditsCost,
            int materialsCost)
        {
            ResourceResult evaluation =
                Evaluate(economy, materials, creditsCost, materialsCost);
            if (evaluation != ResourceResult.Applied)
                return evaluation;

            MissionConstructionCostPolicy.TryResolve(economy.MaterialsOnlyConstruction,
                creditsCost, materialsCost, out creditsCost, out materialsCost);
            FactionTacticalMaterialsComponent nextMaterials = materials;

            if (materialsCost > 0 &&
                FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                    ref nextMaterials,
                    materialsCost,
                    FactionTacticalMaterialsSpendKind.Construction) !=
                FactionTacticalMaterialsMutationResult.Applied)
                return ResourceResult.InvalidState;

            economy.Money -= creditsCost;
            materials = nextMaterials;
            return ResourceResult.Applied;
        }

        public static ResourceResult TryRollback(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            int creditsCost,
            int materialsCost)
        {
            if (!MissionConstructionCostPolicy.TryResolve(economy.MaterialsOnlyConstruction,
                    creditsCost, materialsCost, out creditsCost, out materialsCost))
                return ResourceResult.InvalidCost;
            if (economy.FactionId != materials.FactionId ||
                economy.Money < 0 ||
                creditsCost > int.MaxValue - economy.Money)
                return ResourceResult.InvalidState;

            FactionTacticalMaterialsComponent nextMaterials = materials;
            if (materialsCost > 0 &&
                FactionTacticalMaterialsUtilitySystemHelper.TryRefundConstruction(
                    ref nextMaterials,
                    materialsCost) != FactionTacticalMaterialsMutationResult.Applied)
                return ResourceResult.InvalidState;

            economy.Money += creditsCost;
            materials = nextMaterials;
            return ResourceResult.Applied;
        }
    }
}
