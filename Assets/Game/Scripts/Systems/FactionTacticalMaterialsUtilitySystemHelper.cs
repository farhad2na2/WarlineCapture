using Game.Components;

namespace Game.Runtime
{
    public static class FactionTacticalMaterialsUtilitySystemHelper
    {
        public static FactionTacticalMaterialsMutationResult TryGrant(
            ref FactionTacticalMaterialsComponent materials,
            int amount,
            FactionTacticalMaterialsSourceKind sourceKind)
        {
            if (amount <= 0)
                return FactionTacticalMaterialsMutationResult.InvalidAmount;
            if (!HasValidState(materials))
                return FactionTacticalMaterialsMutationResult.InvalidState;
            if (amount > materials.Capacity - materials.Current)
                return FactionTacticalMaterialsMutationResult.CapacityExceeded;

            materials.Current += amount;
            switch (sourceKind)
            {
                case FactionTacticalMaterialsSourceKind.Fabrication:
                    materials.LifetimeFabricated = SaturatingAdd(materials.LifetimeFabricated, amount);
                    break;
                case FactionTacticalMaterialsSourceKind.Import:
                    materials.LifetimeImported = SaturatingAdd(materials.LifetimeImported, amount);
                    break;
                case FactionTacticalMaterialsSourceKind.Reward:
                    materials.LifetimeRewarded = SaturatingAdd(materials.LifetimeRewarded, amount);
                    break;
                default:
                    materials.Current -= amount;
                    return FactionTacticalMaterialsMutationResult.InvalidState;
            }

            IncrementVersion(ref materials);
            return FactionTacticalMaterialsMutationResult.Applied;
        }

        public static FactionTacticalMaterialsMutationResult TrySpend(
            ref FactionTacticalMaterialsComponent materials,
            int amount,
            FactionTacticalMaterialsSpendKind spendKind)
        {
            if (amount <= 0)
                return FactionTacticalMaterialsMutationResult.InvalidAmount;
            if (!HasValidState(materials))
                return FactionTacticalMaterialsMutationResult.InvalidState;
            if (amount > materials.Current)
                return FactionTacticalMaterialsMutationResult.InsufficientMaterials;
            if (spendKind > FactionTacticalMaterialsSpendKind.Export)
                return FactionTacticalMaterialsMutationResult.InvalidState;

            materials.Current -= amount;
            materials.LifetimeSpent = SaturatingAdd(materials.LifetimeSpent, amount);
            switch (spendKind)
            {
                case FactionTacticalMaterialsSpendKind.Construction:
                    materials.LifetimeConstructionSpent =
                        SaturatingAdd(materials.LifetimeConstructionSpent, amount);
                    break;
                case FactionTacticalMaterialsSpendKind.Repair:
                    materials.LifetimeRepairSpent = SaturatingAdd(materials.LifetimeRepairSpent, amount);
                    break;
                case FactionTacticalMaterialsSpendKind.Infrastructure:
                    materials.LifetimeInfrastructureSpent =
                        SaturatingAdd(materials.LifetimeInfrastructureSpent, amount);
                    break;
                case FactionTacticalMaterialsSpendKind.Upgrade:
                    materials.LifetimeUpgradeSpent = SaturatingAdd(materials.LifetimeUpgradeSpent, amount);
                    break;
                case FactionTacticalMaterialsSpendKind.Export:
                    materials.LifetimeExported = SaturatingAdd(materials.LifetimeExported, amount);
                    break;
            }

            IncrementVersion(ref materials);
            return FactionTacticalMaterialsMutationResult.Applied;
        }

        public static bool HasCapacity(in FactionTacticalMaterialsComponent materials, int amount)
        {
            return amount >= 0 &&
                   HasValidState(materials) &&
                   amount <= materials.Capacity - materials.Current;
        }

        public static bool CanAfford(in FactionTacticalMaterialsComponent materials, int amount)
        {
            return amount >= 0 && HasValidState(materials) && amount <= materials.Current;
        }

        public static FactionTacticalMaterialsMutationResult TryRefundExport(
            ref FactionTacticalMaterialsComponent materials,
            int amount)
        {
            if (amount <= 0)
                return FactionTacticalMaterialsMutationResult.InvalidAmount;
            if (!HasValidState(materials))
                return FactionTacticalMaterialsMutationResult.InvalidState;
            if (amount > materials.Capacity - materials.Current)
                return FactionTacticalMaterialsMutationResult.CapacityExceeded;
            if (amount > materials.LifetimeExported || amount > materials.LifetimeSpent)
                return FactionTacticalMaterialsMutationResult.InvalidState;

            materials.Current += amount;
            materials.LifetimeExported -= amount;
            materials.LifetimeSpent -= amount;
            IncrementVersion(ref materials);
            return FactionTacticalMaterialsMutationResult.Applied;
        }

        public static FactionTacticalMaterialsMutationResult TryRefundConstruction(
            ref FactionTacticalMaterialsComponent materials,
            int amount)
        {
            if (amount <= 0)
                return FactionTacticalMaterialsMutationResult.InvalidAmount;
            if (!HasValidState(materials))
                return FactionTacticalMaterialsMutationResult.InvalidState;
            if (amount > materials.Capacity - materials.Current)
                return FactionTacticalMaterialsMutationResult.CapacityExceeded;
            if (amount > materials.LifetimeSpent || amount > materials.LifetimeConstructionSpent)
                return FactionTacticalMaterialsMutationResult.InvalidState;

            materials.Current += amount;
            materials.LifetimeSpent -= amount;
            materials.LifetimeConstructionSpent -= amount;
            IncrementVersion(ref materials);
            return FactionTacticalMaterialsMutationResult.Applied;
        }

        private static bool HasValidState(in FactionTacticalMaterialsComponent materials)
        {
            return materials.Capacity >= 0 &&
                   materials.Current >= 0 &&
                   materials.Current <= materials.Capacity &&
                   materials.LifetimeFabricated >= 0 &&
                   materials.LifetimeImported >= 0 &&
                   materials.LifetimeRewarded >= 0 &&
                   materials.LifetimeExported >= 0 &&
                   materials.LifetimeSpent >= 0 &&
                   materials.LifetimeConstructionSpent >= 0 &&
                   materials.LifetimeRepairSpent >= 0 &&
                   materials.LifetimeInfrastructureSpent >= 0 &&
                   materials.LifetimeUpgradeSpent >= 0;
        }

        private static int SaturatingAdd(int current, int amount)
        {
            return current >= int.MaxValue - amount ? int.MaxValue : current + amount;
        }

        private static void IncrementVersion(ref FactionTacticalMaterialsComponent materials)
        {
            materials.Version = materials.Version == uint.MaxValue ? 1u : materials.Version + 1u;
        }
    }
}
