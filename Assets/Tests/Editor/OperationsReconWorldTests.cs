using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class OperationsReconWorldTests
{
    private World world;
    private EntityManager em;
    private SystemHandle system;
    private Entity root, player, second, gameplay;
    private BlobAssetReference<MapSurfaceBlob> surface;
    private double time;

    [SetUp]
    public void SetUp()
    {
        world = new World("Operations real entity rules");
        em = world.EntityManager;
        system = world.GetOrCreateSystem<OperationsReconObjectiveSystem>();
        gameplay = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(gameplay, new RuntimeGameplayStateComponent { PlayRequested = 1, SimulationActive = 1 });
        player = Unit(new float3(10, 0, 10));
        second = Unit(new float3(80, 0, 80));
        root = em.CreateEntity(typeof(OperationsReconMissionComponent), typeof(OperationsReconEvidenceComponent));
        em.SetComponentData(root, new OperationsReconMissionComponent
        {
            SessionId = "session.operations.aabb0001", Phase = OperationsReconPhase.Playing,
            DeadlineSeconds = 720, ScanSeconds = 15, EvidenceSeconds = 15,
            ExitPosition = new float3(80, 0, 80), ExitRadius = 8
        });
        em.SetComponentData(root, new OperationsReconEvidenceComponent { Position = new float3(65, 0, 10) });
        var sites = em.AddBuffer<OperationsReconSiteElement>(root);
        for (int i = 0; i < 3; i++) sites.Add(new OperationsReconSiteElement
        { RoleId = new FixedString64Bytes("signal_" + i), Position = new float3(10 + i * 20, 0, 10), Radius = 8 });
        var roster = em.AddBuffer<OperationsReconRosterElement>(root);
        roster.Add(new OperationsReconRosterElement { Unit = player, Recon = 1 });
        roster.Add(new OperationsReconRosterElement { Unit = second });
        em.AddBuffer<OperationsReconActionElement>(root);
        using var builder = new BlobBuilder(Allocator.Temp);
        ref var blob = ref builder.ConstructRoot<MapSurfaceBlob>();
        blob.CellSize = 1; blob.Dimensions = new int2(100, 100);
        var cells = builder.Allocate(ref blob.Cells, 10000);
        var samples = builder.Allocate(ref blob.Samples, 10000);
        builder.Allocate(ref blob.Connections, 0);
        builder.Allocate(ref blob.CompactSamples, 0);
        for (int i = 0; i < 10000; i++)
        {
            cells[i] = new MapSurfaceCell { FirstSurfaceIndex = i, SurfaceCount = 1 };
            samples[i] = new MapSurfaceSample { MovementMask = MapSurfaceMovementMask.Infantry };
        }
        surface = builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
        var map = em.CreateEntity(typeof(MapSurfaceComponent));
        em.SetComponentData(map, new MapSurfaceComponent { SurfaceBlob = surface, CellSize = 1, Dimensions = new int2(100, 100), HasSurfaceData = 1 });
    }

    [TearDown]
    public void TearDown() { world.Dispose(); if (surface.IsCreated) surface.Dispose(); }

    [Test]
    public void ScanRequiresExplicitActionAndFifteenSeconds_ThenTriggersFirstWaveOnlyOnce()
    {
        Tick(20);
        Assert.That(State.CompletedScans, Is.Zero);
        Request(OperationsReconAction.Scan, 0);
        Tick(14);
        Assert.That(State.CompletedScans, Is.Zero);
        Tick();
        Assert.That(State.CompletedScans, Is.EqualTo(1));
        Assert.That(State.FirstScanWaveTriggered, Is.EqualTo(1));
        Request(OperationsReconAction.Scan, 0); Tick(20);
        Assert.That(State.CompletedScans, Is.EqualTo(1));
    }

    [Test]
    public void LeavingRangeResetsChannel_AndRequiresNewAction()
    {
        Request(OperationsReconAction.Scan, 0); Tick(10);
        Move(player, new float3(30, 0, 10)); Tick();
        Move(player, new float3(10, 0, 10)); Tick(15);
        Assert.That(State.CompletedScans, Is.Zero);
        Request(OperationsReconAction.Scan, 0); Tick(15);
        Assert.That(State.CompletedScans, Is.EqualTo(1));
    }

    [Test]
    public void FrameCrossingDeadlineCannotFinishInteractionAfterTimeExpires()
    {
        var mission = State;
        mission.ElapsedSeconds = 710;
        em.SetComponentData(root, mission);
        Request(OperationsReconAction.Scan, 0);
        Tick(20);
        Assert.That(State.ElapsedSeconds, Is.EqualTo(720));
        Assert.That(State.CompletedScans, Is.Zero);
        Assert.That(em.GetBuffer<OperationsReconSiteElement>(root)[0].ChannelSeconds, Is.EqualTo(10));
        Assert.That(State.Outcome, Is.EqualTo(OperationsReconOutcome.Defeat));
    }

    [Test]
    public void PausedSimulationDoesNotAdvanceChannelOrDeadline()
    {
        Request(OperationsReconAction.Scan, 0); Tick(5);
        em.SetComponentData(gameplay, new RuntimeGameplayStateComponent { PlayRequested = 1 });
        Tick(30);
        Assert.That(State.ElapsedSeconds, Is.EqualTo(5));
        Assert.That(em.GetBuffer<OperationsReconSiteElement>(root)[0].ChannelSeconds, Is.EqualTo(5));
    }

    [Test]
    public void ThreeScansEvidenceAndTwoOriginalInfantryAtSafeExitWin_TerminalCannotChange()
    {
        ScanAll(); Recover();
        Assert.That(State.EvidenceWaveTriggered, Is.EqualTo(1));
        Move(player, new float3(80, 0, 80)); Tick();
        Assert.That(State.Outcome, Is.EqualTo(OperationsReconOutcome.Victory));
        Assert.That(State.MasteryCompleted, Is.EqualTo(1));
        em.SetComponentData(player, new UnitHealth());
        Request(OperationsReconAction.Withdraw); Tick();
        Assert.That(State.Outcome, Is.EqualTo(OperationsReconOutcome.Victory));
    }

    [Test]
    public void DeadCarrierDropsRecoverableEvidence_ReplacementDoesNotCountAsOriginal()
    {
        // A third original survivor permits evidence recovery after one combat loss.
        var third = Unit(new float3(65, 0, 10));
        em.GetBuffer<OperationsReconRosterElement>(root).Add(new OperationsReconRosterElement { Unit = third });
        ScanAll(); Recover(); Tick();
        em.DestroyEntity(player); Tick();
        Assert.That(em.GetComponentData<OperationsReconEvidenceComponent>(root).Carrier, Is.EqualTo(Entity.Null));
        player = third;
        Request(OperationsReconAction.RecoverEvidence); Tick(15);
        Assert.That(em.GetComponentData<OperationsReconEvidenceComponent>(root).Carrier, Is.EqualTo(third));
        Move(third, new float3(80, 0, 80));
        em.DestroyEntity(second);
        Unit(new float3(80, 0, 80)); Tick();
        Assert.That(State.Outcome, Is.EqualTo(OperationsReconOutcome.Defeat));
    }

    [Test]
    public void ConcludeRequiresTwoScansAndTwoOriginalInfantryAtExit()
    {
        Request(OperationsReconAction.Conclude); Tick();
        Assert.That(State.Outcome, Is.EqualTo(OperationsReconOutcome.None));
        Scan(0); Scan(1); Move(player, new float3(80, 0, 80));
        Request(OperationsReconAction.Conclude); Tick();
        Assert.That(State.Outcome, Is.EqualTo(OperationsReconOutcome.Partial));
    }

    [Test]
    public void HostileAtExitPreventsExtractionAndPartial()
    {
        var hostile = Unit(new float3(81, 0, 80));
        em.SetComponentData(hostile, new Faction { Id = 2 });
        ScanAll(); Recover(); Move(player, new float3(80, 0, 80));
        Request(OperationsReconAction.Conclude); Tick();
        Assert.That(State.InfantryAtExit, Is.Zero);
        Assert.That(State.Outcome, Is.EqualTo(OperationsReconOutcome.None));
        em.SetComponentData(hostile, new UnitHealth()); Tick();
        Assert.That(State.Outcome, Is.EqualTo(OperationsReconOutcome.Victory));
    }

    [Test]
    public void FirstScanReserveWaitsForWarning_ThenReleasesOnceThroughSharedMoveQueue()
    {
        var queue = em.CreateEntity(typeof(UnitMoveOrderQueueComponent));
        em.AddBuffer<UnitMoveOrderRequestElement>(queue);
        em.AddComponentData(root, new OperationsReconWaveComponent { WarningSeconds = 20 });
        var reserve = Unit(new float3(80, 0, 20));
        em.SetComponentData(reserve, new Faction { Id = 2 });
        em.AddComponentData(reserve, new OperationsReconReserveComponent { Session = root, Wave = 1 });
        em.AddComponent<Disabled>(reserve);
        var waves = world.GetOrCreateSystem<OperationsReconWaveSystem>();
        Scan(0); waves.Update(world.Unmanaged);
        Assert.That(em.HasComponent<Disabled>(reserve), Is.True);
        Tick(19); waves.Update(world.Unmanaged);
        Assert.That(em.HasComponent<Disabled>(reserve), Is.True);
        Tick(); waves.Update(world.Unmanaged);
        Assert.That(em.HasComponent<Disabled>(reserve), Is.False);
        Assert.That(em.GetBuffer<UnitMoveOrderRequestElement>(queue).Length, Is.EqualTo(1));
        waves.Update(world.Unmanaged);
        Assert.That(em.GetBuffer<UnitMoveOrderRequestElement>(queue).Length, Is.EqualTo(1));
    }

    [Test]
    public void PatrolUsesSharedOrdersAndYieldsToCombatPauseAndTerminal()
    {
        var queue = em.CreateEntity(typeof(UnitMoveOrderQueueComponent));
        em.AddBuffer<UnitMoveOrderRequestElement>(queue);
        var route = em.AddBuffer<OperationsReconPatrolWaypoint>(root);
        route.Add(new OperationsReconPatrolWaypoint { Position = new float3(10, 0, 10) });
        route.Add(new OperationsReconPatrolWaypoint { Position = new float3(40, 0, 10) });
        em.AddComponentData(player, new OperationsReconPatrolComponent { Session = root });
        em.AddComponentData(player, new EngageTarget { Target = second });
        var patrols = world.GetOrCreateSystem<OperationsReconPatrolSystem>();
        patrols.Update(world.Unmanaged);
        Assert.That(em.GetBuffer<UnitMoveOrderRequestElement>(queue).Length, Is.Zero);
        em.SetComponentData(player, default(EngageTarget));
        Tick(2); patrols.Update(world.Unmanaged);
        Assert.That(em.GetBuffer<UnitMoveOrderRequestElement>(queue).Length, Is.EqualTo(1));
        Assert.That(em.GetBuffer<UnitMoveOrderRequestElement>(queue)[0].Goal, Is.EqualTo(new int2(40, 10)));
        em.SetComponentData(gameplay, new RuntimeGameplayStateComponent());
        Tick(5); patrols.Update(world.Unmanaged);
        Assert.That(em.GetBuffer<UnitMoveOrderRequestElement>(queue).Length, Is.EqualTo(1));
        em.SetComponentData(gameplay, new RuntimeGameplayStateComponent { SimulationActive = 1 });
        var mission = State; mission.Phase = OperationsReconPhase.Terminal; mission.ElapsedSeconds += 10;
        em.SetComponentData(root, mission); patrols.Update(world.Unmanaged);
        Assert.That(em.GetBuffer<UnitMoveOrderRequestElement>(queue).Length, Is.EqualTo(1));
    }

    [Test]
    public void FirstScanLocationRemainsLatchedWhenEarlierIndexedSiteCompletesLater()
    {
        Scan(2);
        var first = State.FirstScanPosition;
        Scan(0);
        Assert.That(State.FirstScanPosition, Is.EqualTo(first));
        Assert.That(first, Is.EqualTo(new float3(50, 0, 10)));
    }

    [Test]
    public void StartupSuppressesStreamedLegacyForcesWithoutRemovingSelectedStartup()
    {
        var legacy = em.CreateEntity(typeof(InitialUnitsSpawnConfig), typeof(InitialUnitsBlockerChurnConfig));
        var selected = em.CreateEntity(typeof(InitialUnitsSpawnConfig), typeof(CustomGameStartupStateComponent));
        world.GetOrCreateSystem<OperationsReconStartupSystem>().Update(world.Unmanaged);
        Assert.That(em.HasComponent<InitialUnitsSpawnConfig>(legacy), Is.False);
        Assert.That(em.HasComponent<InitialUnitsBlockerChurnConfig>(legacy), Is.False);
        Assert.That(em.HasComponent<InitialUnitsSpawnConfig>(selected), Is.True);
    }

    [Test]
    public void StaleSessionAndEmbarkedInfantryCannotScan()
    {
        em.GetBuffer<OperationsReconActionElement>(root).Add(new OperationsReconActionElement
        { SessionId = "session.operations.aabb9999", Action = OperationsReconAction.Scan, Actor = player, SiteIndex = 0 });
        Tick(15); Assert.That(State.CompletedScans, Is.Zero);
        em.AddComponentData(player, new UnitTransportPassenger { Transport = second });
        Request(OperationsReconAction.Scan, 0); Tick(15);
        Assert.That(State.CompletedScans, Is.Zero);
    }

    private OperationsReconMissionComponent State => em.GetComponentData<OperationsReconMissionComponent>(root);
    private Entity Unit(float3 position)
    {
        var unit = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform), typeof(Faction));
        em.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(unit, new Faction { Id = 1 }); Move(unit, position); return unit;
    }
    private void Move(Entity unit, float3 position) => em.SetComponentData(unit, LocalTransform.FromPosition(position));
    private void Request(OperationsReconAction action, int site = 0) =>
        em.GetBuffer<OperationsReconActionElement>(root).Add(new OperationsReconActionElement
        { SessionId = State.SessionId, Action = action, Actor = player, SiteIndex = site });
    private void Tick(int count = 1)
    {
        for (int i = 0; i < count; i++) { world.SetTime(new TimeData(++time, 1f)); system.Update(world.Unmanaged); }
    }
    private void Scan(int i) { Move(player, new float3(10 + i * 20, 0, 10)); Request(OperationsReconAction.Scan, i); Tick(15); }
    private void ScanAll() { Scan(0); Scan(1); Scan(2); }
    private void Recover() { Move(player, new float3(65, 0, 10)); Request(OperationsReconAction.RecoverEvidence); Tick(15); }
}
