using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;

public sealed class UiMatchHudResourceReadModelSystemTests
{
    [Test]
    public void Update_ProjectsCanonicalPlayerMaterials()
    {
        using World world = new(nameof(Update_ProjectsCanonicalPlayerMaterials));
        EntityManager em = world.EntityManager;
        Entity boundary = CreateBoundary(em);
        CreateFactionResources(em, FactionIdentity.EnemyFactionId, 800, 900, 2u);
        Entity player = CreateFactionResources(em, FactionIdentity.PlayerFactionId, 92, 120, 7u);

        SystemHandle system = world.CreateSystem<UiMatchHudResourceReadModelSystem>();
        UpdateSystem(world, system);

        UiMatchHudHeaderComponent header = em.GetComponentData<UiMatchHudHeaderComponent>(boundary);
        Assert.AreEqual(1u, header.ResourceVersion);
        Assert.AreEqual("92/120", header.MaterialsText.ToString());

        FactionTacticalMaterialsComponent materials =
            em.GetComponentData<FactionTacticalMaterialsComponent>(player);
        materials.Current = 1000;
        materials.Capacity = 2500;
        materials.Version++;
        em.SetComponentData(player, materials);

        UpdateSystem(world, system);
        header = em.GetComponentData<UiMatchHudHeaderComponent>(boundary);
        Assert.AreEqual(2u, header.ResourceVersion);
        Assert.AreEqual("1,000/2,500", header.MaterialsText.ToString());
    }

    [Test]
    public void Update_UnchangedCanonicalValues_DoesNotAllocateManagedMemory()
    {
        using World world = new(nameof(Update_UnchangedCanonicalValues_DoesNotAllocateManagedMemory));
        EntityManager em = world.EntityManager;
        Entity boundary = CreateBoundary(em);
        CreateFactionResources(em, FactionIdentity.PlayerFactionId, 35, 100, 3u);
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
            MaterialsText = "0/0"
        });
        return boundary;
    }

    private static Entity CreateFactionResources(
        EntityManager em,
        byte factionId,
        int materials,
        int capacity,
        uint version)
    {
        Entity entity = em.CreateEntity(typeof(FactionTacticalMaterialsComponent));
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
