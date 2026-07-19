using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;

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

public sealed class UiMatchIdentityReadModelSystemTests
{
    [Test]
    public void ShellStartup_CreatesIdentityReadModelBoundary()
    {
        using World world = new(nameof(ShellStartup_CreatesIdentityReadModelBoundary));
        world.CreateSystem<UiShellStateSystem>();

        EntityQuery boundaryQuery = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UiShellRootComponent>(),
            ComponentType.ReadOnly<UiMatchIdentityReadModelComponent>());

        Assert.AreEqual(1, boundaryQuery.CalculateEntityCount());
    }

    [Test]
    public void Update_ProjectsChangesAndClearsActiveIdentity()
    {
        using World world = new(nameof(Update_ProjectsChangesAndClearsActiveIdentity));
        EntityManager em = world.EntityManager;
        Entity boundary = em.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiMatchIdentityReadModelComponent));
        Entity activeMap = em.CreateEntity(typeof(ActiveOperationMapComponent));
        em.SetComponentData(activeMap, ActiveIdentity("map.one", "scenario.one", "mission.one"));

        SystemHandle system = world.CreateSystem<UiMatchIdentityReadModelSystem>();
        UpdateSystem(world, system);

        UiMatchIdentityReadModelComponent identity =
            em.GetComponentData<UiMatchIdentityReadModelComponent>(boundary);
        Assert.AreEqual("map.one", identity.OperationMapId.ToString());
        Assert.AreEqual("scenario.one", identity.ScenarioId.ToString());
        Assert.AreEqual("mission.one", identity.MissionId.ToString());
        Assert.AreEqual(1u, identity.Version);

        UpdateSystem(world, system);
        Assert.AreEqual(
            1u,
            em.GetComponentData<UiMatchIdentityReadModelComponent>(boundary).Version);

        em.SetComponentData(activeMap, ActiveIdentity("map.one", "scenario.two", "mission.two"));
        UpdateSystem(world, system);
        identity = em.GetComponentData<UiMatchIdentityReadModelComponent>(boundary);
        Assert.AreEqual("scenario.two", identity.ScenarioId.ToString());
        Assert.AreEqual("mission.two", identity.MissionId.ToString());
        Assert.AreEqual(2u, identity.Version);

        em.DestroyEntity(activeMap);
        UpdateSystem(world, system);
        identity = em.GetComponentData<UiMatchIdentityReadModelComponent>(boundary);
        Assert.IsTrue(identity.OperationMapId.IsEmpty);
        Assert.IsTrue(identity.ScenarioId.IsEmpty);
        Assert.IsTrue(identity.MissionId.IsEmpty);
        Assert.AreEqual(3u, identity.Version);
    }

    [Test]
    public void Update_UnchangedIdentity_DoesNotAllocateManagedMemory()
    {
        using World world = new(nameof(Update_UnchangedIdentity_DoesNotAllocateManagedMemory));
        EntityManager em = world.EntityManager;
        em.CreateEntity(typeof(UiShellRootComponent), typeof(UiMatchIdentityReadModelComponent));
        Entity activeMap = em.CreateEntity(typeof(ActiveOperationMapComponent));
        em.SetComponentData(activeMap, ActiveIdentity("map.one", "scenario.one", "mission.one"));
        SystemHandle system = world.CreateSystem<UiMatchIdentityReadModelSystem>();
        UpdateSystem(world, system);

        for (int i = 0; i < 64; i++)
            UpdateSystem(world, system);

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 512; i++)
            UpdateSystem(world, system);

        Assert.AreEqual(0L, System.GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static ActiveOperationMapComponent ActiveIdentity(
        string operationMapId,
        string scenarioId,
        string missionId)
    {
        return new ActiveOperationMapComponent
        {
            OperationMapId = new FixedString64Bytes(operationMapId),
            ScenarioId = new FixedString64Bytes(scenarioId),
            MissionId = new FixedString64Bytes(missionId)
        };
    }

    private static void UpdateSystem(World world, SystemHandle system)
    {
        world.Unmanaged.GetUnsafeSystemRef<UiMatchIdentityReadModelSystem>(system)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(system));
    }
}
