using Game.Components;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

public sealed class UiShellEcsGatewayResourceHeaderTests
{
    [Test]
    public void MatchHudResourceValues_UsesVersionedUsableFuelSummaryWithoutTextFallback()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudResourceValues_UsesVersionedUsableFuelSummaryWithoutTextFallback));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            Entity boundary = em.CreateEntity(typeof(UiShellRootComponent));
            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
                em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            summaries.Add(new BuildingRuntimeFactionUsableFuelSummary
            {
                FactionId = FactionIdentity.PlayerFactionId,
                StoredOilBarrels = 12.4f,
                StoredFuelBarrels = 3.6f,
                OilStorageCapacity = 200,
                FuelStorageCapacity = 100,
                Version = 7u
            });

            Assert.IsTrue(UiShellRuntimeGateway.TryReadMatchHudResourceValues(
                out UiMatchHudResourceValuesModel values));
            Assert.IsTrue(values.IsValid);
            Assert.IsFalse(values.RequiresTextFallback);
            Assert.AreEqual(12, values.Oil);
            Assert.AreEqual(4, values.Fuel);
            Assert.IsTrue(values.ShowOil);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudResourceValues_SignalsLegacyTextFallbackWhenNumericSourceIsUnavailable()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudResourceValues_SignalsLegacyTextFallbackWhenNumericSourceIsUnavailable));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            Entity boundary = em.CreateEntity(
                typeof(UiShellRootComponent),
                typeof(UiMatchHudHeaderComponent));
            em.SetComponentData(boundary, CreatePopulatedHeader(resourceVersion: 5u));

            Assert.IsTrue(UiShellRuntimeGateway.TryReadMatchHudResourceValues(
                out UiMatchHudResourceValuesModel values));
            Assert.IsTrue(values.IsValid);
            Assert.IsTrue(values.RequiresTextFallback);
            Assert.IsFalse(values.ShowOil);

            Assert.IsTrue(UiShellRuntimeGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header));
            Assert.AreEqual("FALLBACK FUEL", header.FuelText);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudResourceValues_WarmedVersionChangesDoNotAllocate()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudResourceValues_WarmedVersionChangesDoNotAllocate));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            Entity boundary = em.CreateEntity(
                typeof(UiShellRootComponent),
                typeof(UiMatchHudHeaderComponent));
            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
                em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            BuildingRuntimeFactionUsableFuelSummary summary = new()
            {
                FactionId = FactionIdentity.PlayerFactionId,
                StoredOilBarrels = 2f,
                StoredFuelBarrels = 15f,
                OilStorageCapacity = 200,
                FuelStorageCapacity = 100,
                Version = 7u
            };
            summaries.Add(summary);

            UiMatchHudResourceValuesModel last = default;
            for (int i = 0; i < 32; i++)
            {
                summary.Version++;
                summaries[0] = summary;
                Assert.IsTrue(UiShellRuntimeGateway.TryReadMatchHudResourceValues(out last));
            }

            long before = System.GC.GetAllocatedBytesForCurrentThread();
            bool allReadsSucceeded = true;
            for (int i = 0; i < 128; i++)
            {
                summary.Version++;
                summaries[0] = summary;
                allReadsSucceeded &= UiShellRuntimeGateway.TryReadMatchHudResourceValues(out last);
            }

            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.IsTrue(allReadsSucceeded);
            Assert.AreEqual(0L, allocated);
            Assert.IsTrue(last.IsValid);
            Assert.IsFalse(last.RequiresTextFallback);
            Assert.AreEqual(2, last.Oil);
            Assert.AreEqual(15, last.Fuel);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudHeader_UsesLivePlayerResourceStorage()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudHeader_UsesLivePlayerResourceStorage));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            em.CreateEntity(typeof(UiShellRootComponent));
            Entity storageEntity = em.CreateEntity(
                typeof(BuildingResourceStorageComponent),
                typeof(Faction));
            em.SetComponentData(storageEntity, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(storageEntity, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 7,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OilStorageCapacity = 200,
                FuelStorageCapacity = 100,
                StoredOilBarrels = 12.4f,
                StoredFuelBarrels = 3.6f
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header));
            Assert.AreEqual("12", header.OilText);
            Assert.AreEqual("4", header.FuelText);
            Assert.IsTrue(header.ShowOil);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudHeader_IgnoresRefineryOutputUntilFuelIsDeliveredToStorage()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudHeader_IgnoresRefineryOutputUntilFuelIsDeliveredToStorage));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            em.CreateEntity(typeof(UiShellRootComponent));
            Entity refinery = em.CreateEntity(
                typeof(BuildingResourceStorageComponent),
                typeof(Faction));
            em.SetComponentData(refinery, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(refinery, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 21,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                FuelStorageCapacity = 100,
                FuelBarrelsPerDay = 40f,
                StoredFuelBarrels = 33f
            });
            Entity fuelStorage = em.CreateEntity(
                typeof(BuildingResourceStorageComponent),
                typeof(Faction));
            em.SetComponentData(fuelStorage, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(fuelStorage, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 22,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                FuelStorageCapacity = 100,
                StoredFuelBarrels = 8f
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header));
            Assert.AreEqual("0", header.OilText);
            Assert.AreEqual("8", header.FuelText);
            Assert.IsFalse(header.ShowOil);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudHeader_PrefersVersionedUsableFuelSummary()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudHeader_PrefersVersionedUsableFuelSummary));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            Entity boundary = em.CreateEntity(typeof(UiShellRootComponent));
            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
                em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            summaries.Add(new BuildingRuntimeFactionUsableFuelSummary
            {
                FactionId = FactionIdentity.PlayerFactionId,
                StoredOilBarrels = 2f,
                StoredFuelBarrels = 15f,
                OilStorageCapacity = 200,
                FuelStorageCapacity = 100,
                Version = 7u
            });
            Entity refinery = em.CreateEntity(
                typeof(BuildingResourceStorageComponent),
                typeof(Faction));
            em.SetComponentData(refinery, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(refinery, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 41,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                FuelStorageCapacity = 100,
                FuelBarrelsPerDay = 40f,
                StoredFuelBarrels = 99f
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header));
            Assert.AreEqual("2", header.OilText);
            Assert.AreEqual("15", header.FuelText);
            Assert.IsTrue(header.ShowOil);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudHeader_VersionOnlyMutationPreservesNonemptyProjectedStringsAndReferences()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudHeader_VersionOnlyMutationPreservesNonemptyProjectedStringsAndReferences));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            Entity boundary = em.CreateEntity(
                typeof(UiShellRootComponent),
                typeof(UiMatchHudHeaderComponent));
            em.SetComponentData(boundary, CreatePopulatedHeader(resourceVersion: 5u));
            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
                em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            summaries.Add(new BuildingRuntimeFactionUsableFuelSummary
            {
                FactionId = FactionIdentity.PlayerFactionId,
                StoredOilBarrels = 2f,
                StoredFuelBarrels = 15f,
                OilStorageCapacity = 200,
                FuelStorageCapacity = 100,
                Version = 7u
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel first));
            Assert.AreEqual("ATTACK ORDER", first.OrderText);
            Assert.AreEqual("ALPHA SQUAD", first.SquadText);
            Assert.AreEqual("2", first.OilText);
            Assert.AreEqual("15", first.FuelText);
            Assert.AreEqual("78/100", first.MaterialsText);
            Assert.AreEqual("LOW", first.CivilianRiskText);

            summaries = em.GetBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            BuildingRuntimeFactionUsableFuelSummary updated = summaries[0];
            updated.Version = 8u;
            summaries[0] = updated;
            UiMatchHudHeaderComponent component = em.GetComponentData<UiMatchHudHeaderComponent>(boundary);
            component.ResourceVersion = 6u;
            em.SetComponentData(boundary, component);

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel versionOnly));
            AssertHeaderStringReferencesSame(first, versionOnly);

            updated.StoredFuelBarrels = 16f;
            updated.Version = 9u;
            summaries[0] = updated;

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel changed));
            Assert.AreEqual("2", changed.OilText);
            Assert.AreEqual("16", changed.FuelText);
            Assert.AreSame(first.OrderText, changed.OrderText);
            Assert.AreSame(first.SquadText, changed.SquadText);
            Assert.AreSame(first.OilText, changed.OilText);
            Assert.AreSame(first.MaterialsText, changed.MaterialsText);
            Assert.AreSame(first.CivilianRiskText, changed.CivilianRiskText);
            Assert.AreNotSame(first.FuelText, changed.FuelText);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudHeader_WarmedVersionOnlyMutationDoesNotAllocate()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudHeader_WarmedVersionOnlyMutationDoesNotAllocate));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            Entity boundary = em.CreateEntity(
                typeof(UiShellRootComponent),
                typeof(UiMatchHudHeaderComponent));
            UiMatchHudHeaderComponent component = CreatePopulatedHeader(resourceVersion: 5u);
            em.SetComponentData(boundary, component);
            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
                em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            summaries.Add(new BuildingRuntimeFactionUsableFuelSummary
            {
                FactionId = FactionIdentity.PlayerFactionId,
                StoredOilBarrels = 2f,
                StoredFuelBarrels = 15f,
                OilStorageCapacity = 200,
                FuelStorageCapacity = 100,
                Version = 7u
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel first));
            BuildingRuntimeFactionUsableFuelSummary summary = summaries[0];
            UiMatchHudHeaderModel last = first;
            for (int i = 0; i < 32; i++)
            {
                summary.Version++;
                summaries[0] = summary;
                component.ResourceVersion++;
                em.SetComponentData(boundary, component);
                Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out last));
            }

            long before = System.GC.GetAllocatedBytesForCurrentThread();
            bool allReadsSucceeded = true;
            for (int i = 0; i < 128; i++)
            {
                summary.Version++;
                summaries[0] = summary;
                component.ResourceVersion++;
                em.SetComponentData(boundary, component);
                allReadsSucceeded &= UiShellEcsGateway.TryReadMatchHudHeader(out last);
            }

            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.IsTrue(allReadsSucceeded);
            Assert.AreEqual(0L, allocated);
            AssertHeaderStringReferencesSame(first, last);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudHeader_EmptyUsableFuelSummaryDoesNotFallbackToRefineryOutput()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudHeader_EmptyUsableFuelSummaryDoesNotFallbackToRefineryOutput));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            Entity boundary = em.CreateEntity(typeof(UiShellRootComponent));
            em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            DynamicBuffer<BuildingRuntimeFactionSummary> economySummaries =
                em.AddBuffer<BuildingRuntimeFactionSummary>(boundary);
            economySummaries.Add(new BuildingRuntimeFactionSummary
            {
                FactionId = FactionIdentity.PlayerFactionId,
                BuildingCount = 1,
                StoredFuelBarrels = 77f,
                FuelBarrelsPerDay = 40f
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header));
            Assert.AreEqual("0", header.OilText);
            Assert.AreEqual("0", header.FuelText);
            Assert.IsFalse(header.ShowOil);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudHeader_EmptyUsableFuelSummaryFallsBackToLiveUsableStorage()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudHeader_EmptyUsableFuelSummaryFallsBackToLiveUsableStorage));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            Entity boundary = em.CreateEntity(typeof(UiShellRootComponent));
            em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            Entity fuelStorage = em.CreateEntity(
                typeof(BuildingResourceStorageComponent),
                typeof(Faction));
            em.SetComponentData(fuelStorage, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(fuelStorage, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 73,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                FuelStorageCapacity = 100,
                StoredFuelBarrels = 42f
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header));
            Assert.AreEqual("0", header.OilText);
            Assert.AreEqual("42", header.FuelText);
            Assert.IsFalse(header.ShowOil);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudHeader_ShowsOilWhenActiveSummaryTeachesExtraction()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudHeader_ShowsOilWhenActiveSummaryTeachesExtraction));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            Entity boundary = em.CreateEntity(typeof(UiShellRootComponent));
            em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            DynamicBuffer<BuildingRuntimeFactionSummary> economySummaries =
                em.AddBuffer<BuildingRuntimeFactionSummary>(boundary);
            economySummaries.Add(new BuildingRuntimeFactionSummary
            {
                FactionId = FactionIdentity.PlayerFactionId,
                BuildingCount = 1,
                OilBarrelsPerDay = 25f
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header));
            Assert.AreEqual("0", header.OilText);
            Assert.AreEqual("0", header.FuelText);
            Assert.IsTrue(header.ShowOil);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    private static UiMatchHudHeaderComponent CreatePopulatedHeader(uint resourceVersion)
    {
        return new UiMatchHudHeaderComponent
        {
            ResourceVersion = resourceVersion,
            OrderText = new FixedString32Bytes("ATTACK ORDER"),
            SquadText = new FixedString32Bytes("ALPHA SQUAD"),
            FuelText = new FixedString32Bytes("FALLBACK FUEL"),
            MaterialsText = new FixedString32Bytes("78/100"),
            CivilianRiskText = new FixedString32Bytes("LOW")
        };
    }

    private static void AssertHeaderStringReferencesSame(
        in UiMatchHudHeaderModel expected,
        in UiMatchHudHeaderModel actual)
    {
        Assert.AreSame(expected.OrderText, actual.OrderText);
        Assert.AreSame(expected.SquadText, actual.SquadText);
        Assert.AreSame(expected.OilText, actual.OilText);
        Assert.AreSame(expected.FuelText, actual.FuelText);
        Assert.AreSame(expected.MaterialsText, actual.MaterialsText);
        Assert.AreSame(expected.CivilianRiskText, actual.CivilianRiskText);
    }
}
