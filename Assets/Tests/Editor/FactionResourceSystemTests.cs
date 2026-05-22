#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

public sealed class FactionResourceSystemTests
{
    [Test]
    public void GetResourceTotals_CountsStorageBuildingsOnly()
    {
        var buildings = new Dictionary<int, TestResourceBuilding>
        {
            { 1, new TestResourceBuilding { OilStorageCapacity = 100, StoredOilBarrels = 42f } },
            { 2, new TestResourceBuilding { FuelStorageCapacity = 50, StoredFuelBarrels = 11.9f } },
            { 3, new TestResourceBuilding { OilStorageCapacity = 100, OilBarrelsPerDay = 5f, StoredOilBarrels = 99f } },
            { 4, new TestResourceBuilding { OilStorageCapacity = 100, StoredOilBarrels = 8f, IsDestroyed = true } }
        };

        var system = new FactionResourceSystem();
        system.GetResourceTotals(buildings, out int oil, out int fuel);

        Assert.AreEqual(42, oil);
        Assert.AreEqual(11, fuel);
    }

    [Test]
    public void TryGetFactionResourceEconomy_SumsFactionStorageAndRates()
    {
        var buildings = new Dictionary<int, TestResourceBuilding>
        {
            { 1, new TestResourceBuilding { HasOwnerFaction = true, OwnerFactionId = 2, OilStorageCapacity = 100, StoredOilBarrels = 12.5f, OilBarrelsPerDay = 3f } },
            { 2, new TestResourceBuilding { HasOwnerFaction = true, OwnerFactionId = 2, FuelStorageCapacity = 50, StoredFuelBarrels = 7f, FuelBarrelsPerDay = 2f } },
            { 3, new TestResourceBuilding { HasOwnerFaction = true, OwnerFactionId = 1, OilStorageCapacity = 100, StoredOilBarrels = 99f, OilBarrelsPerDay = 9f } }
        };

        var system = new FactionResourceSystem();
        bool found = system.TryGetFactionResourceEconomy(buildings, 2, out FactionResourceSystem.ResourceEconomySnapshot snapshot);

        Assert.IsTrue(found);
        Assert.AreEqual(2, snapshot.ResourceBuildingCount);
        Assert.AreEqual(12.5f, snapshot.StoredOilBarrels);
        Assert.AreEqual(7f, snapshot.StoredFuelBarrels);
        Assert.AreEqual(3f, snapshot.OilBarrelsPerDay);
        Assert.AreEqual(2f, snapshot.FuelBarrelsPerDay);
    }

    [Test]
    public void DrainFactionResource_DrainsRequestedResourceAcrossFactionBuildings()
    {
        var first = new TestResourceBuilding { HasOwnerFaction = true, OwnerFactionId = 2, OilStorageCapacity = 100, StoredOilBarrels = 3f };
        var second = new TestResourceBuilding { HasOwnerFaction = true, OwnerFactionId = 2, OilStorageCapacity = 100, StoredOilBarrels = 10f };
        var otherFaction = new TestResourceBuilding { HasOwnerFaction = true, OwnerFactionId = 1, OilStorageCapacity = 100, StoredOilBarrels = 20f };
        var buildings = new Dictionary<int, TestResourceBuilding>
        {
            { 1, first },
            { 2, second },
            { 3, otherFaction }
        };

        var system = new FactionResourceSystem();
        float drained = system.DrainFactionResource(buildings, 2, 8f, FactionResourceSystem.ResourceKind.Oil);

        Assert.AreEqual(8f, drained);
        Assert.AreEqual(0f, first.StoredOilBarrels);
        Assert.AreEqual(5f, second.StoredOilBarrels);
        Assert.AreEqual(20f, otherFaction.StoredOilBarrels);
    }

    [Test]
    public void TryGetPrimaryCapacityInfo_DerivesOilCapacityForFuelProducer()
    {
        var building = new TestResourceBuilding
        {
            FuelStorageCapacity = 10,
            FuelBarrelsPerDay = 5f,
            StoredOilBarrels = 7.2f
        };

        var system = new FactionResourceSystem();
        bool found = system.TryGetPrimaryCapacityInfo(building, 2f, out int current, out int max, out float progress01);

        Assert.IsTrue(found);
        Assert.AreEqual(8, current);
        Assert.AreEqual(20, max);
        Assert.AreEqual(0.36f, progress01, 0.0001f);
    }

    private sealed class TestResourceBuilding : FactionResourceSystem.IResourceBuilding
    {
        public bool IsDestroyed { get; set; }
        public bool HasOwnerFaction { get; set; }
        public byte OwnerFactionId { get; set; }
        public int OilStorageCapacity { get; set; }
        public int FuelStorageCapacity { get; set; }
        public float OilBarrelsPerDay { get; set; }
        public float FuelBarrelsPerDay { get; set; }
        public float StoredOilBarrels { get; set; }
        public float StoredFuelBarrels { get; set; }
    }
}
#endif
