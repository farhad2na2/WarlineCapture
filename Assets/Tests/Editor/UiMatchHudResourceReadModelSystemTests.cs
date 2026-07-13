using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;

public sealed class UiMatchHudResourceReadModelSystemTests
{
    [Test]
    public void Update_ProjectsCanonicalPlayerCreditsAndMaterials()
    {
        using World world = new(nameof(Update_ProjectsCanonicalPlayerCreditsAndMaterials));
        EntityManager em = world.EntityManager;
        Entity boundary = CreateBoundary(em);
        CreateFactionResources(em, FactionIdentity.EnemyFactionId, 900000, 800, 900, 2u);
        Entity player = CreateFactionResources(em, FactionIdentity.PlayerFactionId, 187540, 92, 120, 7u);

        SystemHandle system = world.CreateSystem<UiMatchHudResourceReadModelSystem>();
        UpdateSystem(world, system);

        UiMatchHudHeaderComponent header = em.GetComponentData<UiMatchHudHeaderComponent>(boundary);
        Assert.AreEqual(1u, header.ResourceVersion);
        Assert.AreEqual("187,540", header.CreditsText.ToString());
        Assert.AreEqual("92/120", header.SupplyText.ToString());

        FactionEconomy economy = em.GetComponentData<FactionEconomy>(player);
        economy.Money = 2000000;
        em.SetComponentData(player, economy);
        FactionTacticalMaterialsComponent materials =
            em.GetComponentData<FactionTacticalMaterialsComponent>(player);
        materials.Current = 1000;
        materials.Capacity = 2500;
        materials.Version++;
        em.SetComponentData(player, materials);

        UpdateSystem(world, system);
        header = em.GetComponentData<UiMatchHudHeaderComponent>(boundary);
        Assert.AreEqual(2u, header.ResourceVersion);
        Assert.AreEqual("2,000,000", header.CreditsText.ToString());
        Assert.AreEqual("1,000/2,500", header.SupplyText.ToString());
    }

    [Test]
    public void Update_UnchangedCanonicalValues_DoesNotAllocateManagedMemory()
    {
        using World world = new(nameof(Update_UnchangedCanonicalValues_DoesNotAllocateManagedMemory));
        EntityManager em = world.EntityManager;
        Entity boundary = CreateBoundary(em);
        CreateFactionResources(em, FactionIdentity.PlayerFactionId, 12500, 35, 100, 3u);
        SystemHandle system = world.CreateSystem<UiMatchHudResourceReadModelSystem>();
        UpdateSystem(world, system);

        for (int i = 0; i < 64; i++)
            UpdateSystem(world, system);

        uint unchangedVersion = em.GetComponentData<UiMatchHudHeaderComponent>(boundary).ResourceVersion;

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 512; i++)
            UpdateSystem(world, system);

        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(0L, allocated);
        Assert.AreEqual(
            unchangedVersion,
            em.GetComponentData<UiMatchHudHeaderComponent>(boundary).ResourceVersion);
    }

    private static Entity CreateBoundary(EntityManager em)
    {
        Entity boundary = em.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiMatchHudHeaderComponent));
        em.SetComponentData(boundary, new UiMatchHudHeaderComponent
        {
            CreditsText = "0",
            SupplyText = "0/0"
        });
        return boundary;
    }

    private static Entity CreateFactionResources(
        EntityManager em,
        byte factionId,
        int credits,
        int materials,
        int capacity,
        uint version)
    {
        Entity entity = em.CreateEntity(
            typeof(FactionEconomy),
            typeof(FactionTacticalMaterialsComponent));
        em.SetComponentData(entity, new FactionEconomy
        {
            FactionId = factionId,
            Money = credits
        });
        em.SetComponentData(entity, new FactionTacticalMaterialsComponent
        {
            FactionId = factionId,
            Current = materials,
            Capacity = capacity,
            Version = version
        });
        return entity;
    }

    private static void UpdateSystem(World world, SystemHandle system)
    {
        world.Unmanaged.GetUnsafeSystemRef<UiMatchHudResourceReadModelSystem>(system)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(system));
    }
}
