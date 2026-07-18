using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;

public sealed class ResourceExchangeCanonicalResourceUtilitySystemHelperTests
{
    [Test]
    public void PhysicalResources_CannotMutateDetachedEconomyOrWalletCounters()
    {
        FactionEconomy economy = new FactionEconomy { FactionId = 1, Money = 1000 };
        FactionTacticalMaterialsComponent materials = Materials(current: 50, capacity: 100);
        ResourceExchangeWalletComponent wallet = new ResourceExchangeWalletComponent { FactionId = 1 };

        Assert.IsFalse(ResourceExchangeResourceUtilitySystemHelper.TrySpend(
            ref economy,
            ref materials,
            ref wallet,
            ResourceExchangeResourceKind.Oil,
            250));
        Assert.IsFalse(ResourceExchangeResourceUtilitySystemHelper.TryGrantImport(
            ref economy,
            ref materials,
            ref wallet,
            ResourceExchangeResourceKind.Oil,
            40));

        Assert.AreEqual(1000, economy.Money);
        Assert.AreEqual(0u, wallet.Version);
    }

    [Test]
    public void PhysicalResources_AreReadOnlyFromFactionStorageSummary()
    {
        FactionEconomy economy = new FactionEconomy { FactionId = 1 };
        FactionTacticalMaterialsComponent materials = Materials(current: 0, capacity: 100);
        ResourceExchangeWalletComponent wallet = new ResourceExchangeWalletComponent { FactionId = 1 };
        BuildingRuntimeFactionUsableFuelSummary physicalResources =
            new BuildingRuntimeFactionUsableFuelSummary
            {
                FactionId = 1,
                StoredOilBarrels = 20.4f,
                StoredFuelBarrels = 30.6f,
                OilStorageCapacity = 80,
                FuelStorageCapacity = 90
            };

        Assert.AreEqual(20, ResourceExchangeResourceUtilitySystemHelper.GetAmount(
            economy,
            materials,
            wallet,
            physicalResources,
            ResourceExchangeResourceKind.Oil));
        Assert.AreEqual(30, ResourceExchangeResourceUtilitySystemHelper.GetAmount(
            economy,
            materials,
            wallet,
            physicalResources,
            ResourceExchangeResourceKind.Fuel));
        Assert.AreEqual(80, ResourceExchangeResourceUtilitySystemHelper.GetCapacity(
            materials,
            wallet,
            physicalResources,
            ResourceExchangeResourceKind.Oil));
        Assert.AreEqual(90, ResourceExchangeResourceUtilitySystemHelper.GetCapacity(
            materials,
            wallet,
            physicalResources,
            ResourceExchangeResourceKind.Fuel));
        Assert.IsFalse(ResourceExchangeResourceUtilitySystemHelper.TrySpend(
            ref economy,
            ref materials,
            ref wallet,
            ResourceExchangeResourceKind.Oil,
            1));
        Assert.IsFalse(ResourceExchangeResourceUtilitySystemHelper.TryGrantImport(
            ref economy,
            ref materials,
            ref wallet,
            ResourceExchangeResourceKind.Fuel,
            1));
    }

    [Test]
    public void Materials_ExportRefundAndImportUseCanonicalCounters()
    {
        FactionEconomy economy = new FactionEconomy { FactionId = 1 };
        FactionTacticalMaterialsComponent materials = Materials(current: 70, capacity: 120);
        ResourceExchangeWalletComponent wallet = new ResourceExchangeWalletComponent { FactionId = 1 };

        Assert.IsTrue(ResourceExchangeResourceUtilitySystemHelper.TrySpend(
            ref economy,
            ref materials,
            ref wallet,
            ResourceExchangeResourceKind.Materials,
            30));
        Assert.IsTrue(ResourceExchangeResourceUtilitySystemHelper.TryRefundReservedInput(
            ref economy,
            ref materials,
            ref wallet,
            ResourceExchangeResourceKind.Materials,
            10));
        Assert.IsTrue(ResourceExchangeResourceUtilitySystemHelper.TryGrantImport(
            ref economy,
            ref materials,
            ref wallet,
            ResourceExchangeResourceKind.Materials,
            15));

        Assert.AreEqual(65, materials.Current);
        Assert.AreEqual(20, materials.LifetimeSpent);
        Assert.AreEqual(20, materials.LifetimeExported);
        Assert.AreEqual(15, materials.LifetimeImported);
        Assert.AreEqual(3u, materials.Version);
    }

    [Test]
    public void Materials_RejectImportBeyondCanonicalCapacity()
    {
        FactionEconomy economy = new FactionEconomy { FactionId = 1 };
        FactionTacticalMaterialsComponent materials = Materials(current: 95, capacity: 100);
        ResourceExchangeWalletComponent wallet = new ResourceExchangeWalletComponent { FactionId = 1 };

        Assert.IsFalse(ResourceExchangeResourceUtilitySystemHelper.TryGrantImport(
            ref economy,
            ref materials,
            ref wallet,
            ResourceExchangeResourceKind.Materials,
            6));
        Assert.AreEqual(95, materials.Current);
        Assert.AreEqual(0, materials.LifetimeImported);
        Assert.AreEqual(0u, materials.Version);
    }

    [Test]
    public void CanonicalMutations_DoNotAllocateManagedMemoryAfterWarmup()
    {
        FactionEconomy economy = new FactionEconomy { FactionId = 1, Money = 1000 };
        FactionTacticalMaterialsComponent materials = Materials(current: 500, capacity: 1000);
        ResourceExchangeWalletComponent wallet = new ResourceExchangeWalletComponent { FactionId = 1 };

        for (int i = 0; i < 32; i++)
        {
            ResourceExchangeResourceUtilitySystemHelper.TrySpend(
                ref economy,
                ref materials,
                ref wallet,
                ResourceExchangeResourceKind.Materials,
                1);
            ResourceExchangeResourceUtilitySystemHelper.TryRefundReservedInput(
                ref economy,
                ref materials,
                ref wallet,
                ResourceExchangeResourceKind.Materials,
                1);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 512; i++)
        {
            ResourceExchangeResourceUtilitySystemHelper.TrySpend(
                ref economy,
                ref materials,
                ref wallet,
                ResourceExchangeResourceKind.Materials,
                1);
            ResourceExchangeResourceUtilitySystemHelper.TryRefundReservedInput(
                ref economy,
                ref materials,
                ref wallet,
                ResourceExchangeResourceKind.Materials,
                1);
        }

        Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.AreEqual(1000, economy.Money);
    }

    private static FactionTacticalMaterialsComponent Materials(int current, int capacity)
    {
        return new FactionTacticalMaterialsComponent
        {
            FactionId = 1,
            Current = current,
            Capacity = capacity
        };
    }
}
