using Game.Components;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class ResourceExchangeResourceUtilitySystemHelper
    {
        public static int GetAmount(
            in FactionEconomy economy,
            in FactionTacticalMaterialsComponent materials,
            in ResourceExchangeWalletComponent wallet,
            ResourceExchangeResourceKind resourceKind)
        {
            switch (resourceKind)
            {
                case ResourceExchangeResourceKind.Credits:
                    return economy.Money;
                case ResourceExchangeResourceKind.Materials:
                    return materials.Current;
                case ResourceExchangeResourceKind.Oil:
                    return wallet.Oil;
                case ResourceExchangeResourceKind.Fuel:
                    return wallet.Fuel;
                case ResourceExchangeResourceKind.RushTickets:
                    return wallet.RushTickets;
                default:
                    return 0;
            }
        }

        public static int GetCapacity(
            in FactionTacticalMaterialsComponent materials,
            in ResourceExchangeWalletComponent wallet,
            ResourceExchangeResourceKind resourceKind)
        {
            switch (resourceKind)
            {
                case ResourceExchangeResourceKind.Materials:
                    return materials.Capacity;
                case ResourceExchangeResourceKind.Oil:
                    return wallet.OilCapacity;
                case ResourceExchangeResourceKind.Fuel:
                    return wallet.FuelCapacity;
                default:
                    return int.MaxValue;
            }
        }

        public static bool TrySpend(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            ResourceExchangeResourceKind resourceKind,
            int amount)
        {
            if (amount <= 0)
                return false;

            switch (resourceKind)
            {
                case ResourceExchangeResourceKind.Credits:
                    if (economy.Money < amount)
                        return false;
                    economy.Money -= amount;
                    return true;
                case ResourceExchangeResourceKind.Materials:
                    return FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                               ref materials,
                               amount,
                               FactionTacticalMaterialsSpendKind.Export) ==
                           FactionTacticalMaterialsMutationResult.Applied;
                case ResourceExchangeResourceKind.Oil:
                    if (wallet.Oil < amount)
                        return false;
                    wallet.Oil -= amount;
                    wallet.Version++;
                    return true;
                case ResourceExchangeResourceKind.Fuel:
                    if (wallet.Fuel < amount)
                        return false;
                    wallet.Fuel -= amount;
                    wallet.Version++;
                    return true;
                case ResourceExchangeResourceKind.RushTickets:
                    if (wallet.RushTickets < amount)
                        return false;
                    wallet.RushTickets -= amount;
                    wallet.Version++;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryGrantImport(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            ResourceExchangeResourceKind resourceKind,
            int amount)
        {
            if (amount <= 0)
                return false;

            switch (resourceKind)
            {
                case ResourceExchangeResourceKind.Credits:
                    economy.Money = SaturatingAdd(economy.Money, amount);
                    return true;
                case ResourceExchangeResourceKind.Materials:
                    return FactionTacticalMaterialsUtilitySystemHelper.TryGrant(
                               ref materials,
                               amount,
                               FactionTacticalMaterialsSourceKind.Import) ==
                           FactionTacticalMaterialsMutationResult.Applied;
                case ResourceExchangeResourceKind.Oil:
                    wallet.Oil = SaturatingAdd(wallet.Oil, amount);
                    wallet.Version++;
                    return true;
                case ResourceExchangeResourceKind.Fuel:
                    wallet.Fuel = SaturatingAdd(wallet.Fuel, amount);
                    wallet.Version++;
                    return true;
                case ResourceExchangeResourceKind.RushTickets:
                    wallet.RushTickets = SaturatingAdd(wallet.RushTickets, amount);
                    wallet.Version++;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryRefundReservedInput(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            ResourceExchangeResourceKind resourceKind,
            int amount)
        {
            if (resourceKind == ResourceExchangeResourceKind.Materials)
            {
                return FactionTacticalMaterialsUtilitySystemHelper.TryRefundExport(ref materials, amount) ==
                       FactionTacticalMaterialsMutationResult.Applied;
            }

            return TryGrantImport(ref economy, ref materials, ref wallet, resourceKind, amount);
        }

        private static int SaturatingAdd(int current, int amount)
        {
            current = math.max(0, current);
            return current >= int.MaxValue - amount ? int.MaxValue : current + amount;
        }
    }
}
