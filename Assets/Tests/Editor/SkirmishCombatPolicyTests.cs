using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class SkirmishCombatPolicyTests
{
    [Test]
    public void MovingAttackTargetEnteringRangeStopsApproachAndDeadTargetReleasesPath()
    {
        using var world = new World("Skirmish approach completion");
        var em = world.EntityManager;
        var target = em.CreateEntity(typeof(LocalTransform), typeof(UnitHealth));
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(50, 0, 0)));
        em.SetComponentData(target, new UnitHealth { Current = 100, Max = 100 });
        var unit = em.CreateEntity(typeof(BaseBreachOrder), typeof(UnitAttack), typeof(LocalTransform),
            typeof(UnitPathFollow), typeof(ManualMoveOrderTag));
        em.SetComponentData(unit, LocalTransform.FromPosition(float3.zero));
        em.SetComponentData(unit, new UnitAttack { Range = 32 });
        em.SetComponentData(unit, new BaseBreachOrder { FinalTarget = target, FinalPosition = new float3(50, 0, 0),
            FinalCell = new int2(20, 0), Stage = BaseBreachOrder.StageMovingToFinalTarget, IsCommanded = 1 });
        var grid = new GridConfig { Width = 100, Height = 100, CellSize = 1 };
        SkirmishCombatApproachSystem.ResumeReadyApproaches(em, grid);
        Assert.IsTrue(em.HasComponent<UnitPathFollow>(unit), "An out-of-range target still requires an approach.");
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(30, 0, 0)));
        SkirmishCombatApproachSystem.ResumeReadyApproaches(em, grid);
        Assert.IsFalse(em.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(em.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsFalse(em.HasComponent<BaseBreachOrder>(unit));
        var engage = em.GetComponentData<EngageTarget>(unit);
        Assert.AreEqual(target, engage.Target);
        Assert.AreEqual(new float3(30, 0, 0), engage.Position, "Use current target position, not old approach position.");
        Assert.AreEqual(1, engage.IsCommanded);
        em.RemoveComponent<EngageTarget>(unit);
        em.AddComponent<UnitPathFollow>(unit); em.AddComponent<ManualMoveOrderTag>(unit);
        em.AddComponentData(unit, new BaseBreachOrder { FinalTarget = target, Stage = BaseBreachOrder.StageMovingToFinalTarget });
        em.SetComponentData(target, new UnitHealth { Current = 0, Max = 100 });
        var breachSystem = world.CreateSystem<BaseBreachOrderSystem>();
        breachSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitPathFollow>(unit), "Dead targets must not leave a movement order suppressing combat.");
        Assert.IsFalse(em.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsFalse(em.HasComponent<EngageTarget>(unit));
        // A later explicit player Move has no breach component and must remain intact.
        em.AddComponent<UnitPathFollow>(unit); em.AddComponent<ManualMoveOrderTag>(unit);
        SkirmishCombatApproachSystem.ResumeReadyApproaches(em, grid);
        Assert.IsTrue(em.HasComponent<UnitPathFollow>(unit));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(unit));
    }

    [TestCase("en")]
    [TestCase("fa-IR")]
    public void SkirmishGuideHasLocalizedObjectiveOrdersBuildAndSupplyPages(string locale)
    {
        string previous = GameLocalization.CurrentLocaleCode;
        try
        {
            GameLocalization.SetLocale(locale, false);
            var method = typeof(Game.UI.Runtime.MissionFieldGuideView).GetMethod("CreateSkirmishCatalog",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var catalog = (Game.UI.Contracts.UiMissionGuideCatalog)method.Invoke(null, null);
            Assert.AreEqual(4, catalog.Topics.Length);
            Assert.Zero(catalog.Classes.Length, "Skirmish must not advertise campaign-only aircraft or transport lessons.");
            foreach (var topic in catalog.Topics)
                foreach (var key in new[] { topic.TitleKey, topic.BodyKey, topic.ExampleKey, topic.MistakeKey, topic.DiagramKey })
                {
                    string text = GameText.Get(key);
                    Assert.IsNotEmpty(text, key);
                    Assert.AreNotEqual(key, text, key);
                    if (locale == "fa-IR") Assert.That(text, Does.Match("[\\u0600-\\u06ff]"), key);
                }
        }
        finally { GameLocalization.SetLocale(previous, false); }
    }

    [Test]
    public void SkirmishGuideRoutesWithoutACampaignMissionAndRejectsFinishedMatches()
    {
        var previous = World.DefaultGameObjectInjectionWorld;
        using var world = new World("Skirmish guide routing");
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            var em = world.EntityManager;
            var root = em.CreateEntity(typeof(Game.UI.Shell.Contracts.Ecs.UiShellRootComponent));
            em.AddBuffer<Game.UI.Shell.Contracts.Ecs.UiShellPopupRequestComponent>(root);
            var match = em.CreateEntity(typeof(SkirmishMatchState));
            em.SetComponentData(match, new SkirmishMatchState { Phase = SkirmishPhase.Playing });
            var gateway = (Game.UI.Shell.Ecs.UiShellEcsGateway)System.Activator.CreateInstance(typeof(Game.UI.Shell.Ecs.UiShellEcsGateway), true);
            Assert.IsTrue(gateway.TryRequestMissionDefenseAction(Game.UI.Contracts.UiMissionDefenseAction.OpenGuide, 0, 0));
            var requests = em.GetBuffer<Game.UI.Shell.Contracts.Ecs.UiShellPopupRequestComponent>(root);
            Assert.AreEqual(Game.UI.Contracts.UiShellPopupKind.MissionFieldGuide, requests[0].PopupKind);
            Assert.AreEqual(Game.UI.Contracts.UiShellPopupIntent.Show, requests[0].Intent);
            Assert.IsTrue(gateway.TryRequestMissionDefenseAction(Game.UI.Contracts.UiMissionDefenseAction.CloseGuide, 0, 0));
            Assert.AreEqual(Game.UI.Contracts.UiShellPopupIntent.Hide, requests[1].Intent);
            em.SetComponentData(match, new SkirmishMatchState { Phase = SkirmishPhase.Finished });
            Assert.IsFalse(gateway.TryRequestMissionDefenseAction(Game.UI.Contracts.UiMissionDefenseAction.OpenGuide, 0, 0));
        }
        finally { World.DefaultGameObjectInjectionWorld = previous; }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RecruitsCannotSpawnInDisconnectedPocketsButCanUseAnOpenExit(bool openExit)
    {
        var grid = new GridConfig { Width = 128, Height = 128, CellSize = 1 };
        var walkable = new Unity.Collections.NativeArray<GridWalkable>(128 * 128, Unity.Collections.Allocator.TempJob);
        var blocked = new Unity.Collections.NativeBitArray(128 * 128, Unity.Collections.Allocator.TempJob);
        var reserved = new Unity.Collections.NativeBitArray(128 * 128, Unity.Collections.Allocator.TempJob);
        try
        {
            for (int i = 0; i < walkable.Length; i++) walkable[i] = new GridWalkable { Value = 1 };
            for (int y = 0; y < 128; y++) blocked.Set(y * 128 + 60, !(openExit && y == 70));
            reserved.Set(72 * 128 + 100, true);
            SkirmishPopulationPolicy.ReserveDisconnectedSpawnCells(grid, walkable, blocked, new int2(64, 64), 1, ref reserved);
            Assert.AreEqual(!openExit, reserved.IsSet(64 * 128 + 50), "A disconnected candidate must wait or use a reachable exit.");
            Assert.IsFalse(reserved.IsSet(64 * 128 + 90), "The approach remains usable.");
            Assert.IsTrue(reserved.IsSet(72 * 128 + 100), "Existing reservations must survive connectivity checks.");
        }
        finally { walkable.Dispose(); blocked.Dispose(); reserved.Dispose(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RecruitmentUsesActualTerrainEvenWhenCompatibilityGridSaysWalkable(bool openExit)
    {
        using var builder = new BlobBuilder(Unity.Collections.Allocator.Temp);
        ref var root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.Dimensions = new int2(128, 128);
        root.CellSize = 1;
        root.RuntimeEncoding = MapSurfaceRuntimeEncoding.Full;
        var cells = builder.Allocate(ref root.Cells, 128 * 128);
        var samples = builder.Allocate(ref root.Samples, 128 * 128);
        builder.Allocate(ref root.Connections, 0);
        builder.Allocate(ref root.CompactSamples, 0);
        for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                int i = y * 128 + x;
                bool rock = (x == 60 && !(openExit && y == 70)) || (x == 90 && y == 64);
                cells[i] = new MapSurfaceCell { FirstSurfaceIndex = i, SurfaceCount = 1 };
                samples[i] = new MapSurfaceSample
                {
                    Cell = new int2(x, y), Normal = new float3(0, 1, 0),
                    SurfaceType = rock ? MapSurfaceType.Blocked : MapSurfaceType.Terrain,
                    MovementMask = rock ? MapSurfaceMovementMask.None : MapSurfaceMovementMask.Infantry
                };
            }
        using var blob = builder.CreateBlobAssetReference<MapSurfaceBlob>(Unity.Collections.Allocator.Persistent);
        var surface = new MapSurfaceComponent { SurfaceBlob = blob, Dimensions = root.Dimensions, CellSize = 1, HasSurfaceData = 1 };
        var grid = new GridConfig { Width = 128, Height = 128, CellSize = 1 };
        var walkable = new Unity.Collections.NativeArray<GridWalkable>(128 * 128, Unity.Collections.Allocator.TempJob);
        using var blocked = new Unity.Collections.NativeBitArray(128 * 128, Unity.Collections.Allocator.TempJob);
        var reserved = new Unity.Collections.NativeBitArray(128 * 128, Unity.Collections.Allocator.TempJob);
        try
        {
            for (int i = 0; i < walkable.Length; i++) walkable[i] = new GridWalkable { Value = 1 };
            SkirmishPopulationPolicy.ReserveDisconnectedSpawnCells(grid, walkable, blocked, new int2(64, 64), 1, ref reserved, surface);
            Assert.AreEqual(!openExit, reserved.IsSet(64 * 128 + 50), "Surface-only barriers must separate spawn pockets.");
            Assert.IsTrue(reserved.IsSet(64 * 128 + 90), "Do not instantiate a recruit on impassable terrain.");
            Assert.IsFalse(reserved.IsSet(64 * 128 + 91), "Keep connected walkable ground available.");
        }
        finally { walkable.Dispose(); reserved.Dispose(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AttackApproachUsesTheSameTerrainMaskAsMovement(bool allBlocked)
    {
        using var builder = new BlobBuilder(Unity.Collections.Allocator.Temp);
        ref var root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.Dimensions = new int2(80, 80);
        root.CellSize = 1;
        root.RuntimeEncoding = MapSurfaceRuntimeEncoding.Full;
        var cells = builder.Allocate(ref root.Cells, 6400);
        var samples = builder.Allocate(ref root.Samples, 6400);
        builder.Allocate(ref root.Connections, 0);
        builder.Allocate(ref root.CompactSamples, 0);
        for (int i = 0; i < 6400; i++)
        {
            bool rock = allBlocked || i % 80 < 30;
            cells[i] = new MapSurfaceCell { FirstSurfaceIndex = i, SurfaceCount = 1 };
            samples[i] = new MapSurfaceSample
            {
                Cell = new int2(i % 80, i / 80), Normal = new float3(0, 1, 0),
                SurfaceType = rock ? MapSurfaceType.Blocked : MapSurfaceType.Terrain,
                MovementMask = rock ? MapSurfaceMovementMask.None : MapSurfaceMovementMask.Infantry
            };
        }
        using var blob = builder.CreateBlobAssetReference<MapSurfaceBlob>(Unity.Collections.Allocator.Persistent);
        var surface = new MapSurfaceComponent { SurfaceBlob = blob, Dimensions = root.Dimensions, CellSize = 1, HasSurfaceData = 1 };
        var grid = new GridConfig { Width = 80, Height = 80, CellSize = 1 };
        var walkable = new Unity.Collections.NativeArray<GridWalkable>(6400, Unity.Collections.Allocator.Temp);
        using var blocked = new Unity.Collections.NativeBitArray(6400, Unity.Collections.Allocator.Temp);
        try
        {
            for (int i = 0; i < 6400; i++) walkable[i] = new GridWalkable { Value = 1 };
            bool found = SkirmishCombatApproachSystem.TryFindApproach(grid, walkable, blocked,
                new float3(5, 0, 40), new float3(40, 0, 40), new int2(1), 16, out var goal, surface);
            Assert.AreEqual(!allBlocked, found);
            if (found) Assert.GreaterOrEqual(goal.x, 30, "Do not direct an attacker onto terrain rejected by pathfinding.");
        }
        finally { walkable.Dispose(); }
    }

    [Test]
    public void FullArmyUsesBalancedStableQuickSelectGroups()
    {
        using var world = new World("SkirmishQuickGroups");
        var em = world.EntityManager;
        var units = new System.Collections.Generic.List<Entity>();
        for (int i = 0; i < 8; i++) units.Add(Unit(em, 1, default, "Soldier"));
        SkirmishSquadAssignment.Assign(em);
        var originalSlots = units.ConvertAll(e => em.GetComponentData<SkirmishSquadMember>(e).Slot);
        for (int i = 8; i < 24; i++) units.Add(Unit(em, 1, default, "Soldier"));
        SkirmishSquadAssignment.Assign(em);
        int[] counts = new int[4];
        foreach (var unit in units) counts[em.GetComponentData<SkirmishSquadMember>(unit).Slot]++;
        CollectionAssert.AreEqual(new[] { 6, 6, 6, 6 }, counts);
        for (int i = 0; i < 8; i++)
            Assert.AreEqual(originalSlots[i], em.GetComponentData<SkirmishSquadMember>(units[i]).Slot, "Existing troops must stay under the same card.");
        em.SetComponentData(units[0], new UnitHealth());
        var replacement = Unit(em, 1, default, "Soldier");
        SkirmishSquadAssignment.Assign(em);
        Assert.AreEqual(originalSlots[0], em.GetComponentData<SkirmishSquadMember>(replacement).Slot);
    }

    [Test]
    public void BothOwnedWatchtowersUseSkirmishCombatValuesAndSceneryIsUnchanged()
    {
        using var world = new World("SkirmishWatchtowerBalance");
        var em = world.EntityManager;
        var player = Unit(em, 1, default, "Building_GuardTower");
        var enemy = Unit(em, 2, default, "Building_GuardTower");
        var scenery = Unit(em, 0, default, "Building_GuardTower");
        foreach (var e in new[] { player, enemy, scenery })
            em.SetComponentData(e, new UnitAttack { Range = 100, Damage = 10, CooldownSeconds = .3f });
        SkirmishCombatPolicy.ApplyRoster(em, default);
        foreach (var e in new[] { player, enemy })
        {
            Assert.AreEqual(55, em.GetComponentData<UnitAttack>(e).Range);
            Assert.AreEqual(.8f, em.GetComponentData<UnitAttack>(e).CooldownSeconds);
        }
        Assert.AreEqual(100, em.GetComponentData<UnitAttack>(scenery).Range);
    }

    [Test]
    public void AuthoredCanalIsReservedForSkirmishWithoutChangingCampaignPlacement()
    {
        using var world = new World("SkirmishCanalFootprint");
        var em = world.EntityManager;
        var mesh = new Mesh { name = "SM_Gen_Env_Water_Plane_01", bounds = new Bounds(Vector3.zero, new Vector3(4, 0, 10)) };
        var provider = Game.Rendering.Contracts.RenderBoundsGateway.Provider;
        Game.Rendering.Contracts.RenderBoundsGateway.Bind(Game.Rendering.RenderBoundsPresentation.Create());
        try
        {
            var water = em.CreateEntity(typeof(Unity.Rendering.MaterialMeshInfo), typeof(LocalToWorld));
            em.AddSharedComponentManaged(water, new Unity.Rendering.RenderMeshArray(new Material[0], new[] { mesh }));
            em.SetComponentData(water, Unity.Rendering.MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
            em.SetComponentData(water, new LocalToWorld { Value = float4x4.TRS(new float3(20, 0, 20), quaternion.RotateY(math.PI / 2), new float3(1)) });
            var grid = new GridConfig { Width = 64, Height = 64, CellSize = 1 };
            Assert.IsNull(SkirmishWaterPlacement.CreateMask(em, grid), "Campaign behavior is unchanged.");
            em.CreateEntity(typeof(SkirmishMatchState));
            var mask = SkirmishWaterPlacement.CreateMask(em, grid);
            Assert.IsTrue(mask[20 * 64 + 16], "Reserve the rotated canal.");
            Assert.IsFalse(mask[16 * 64 + 20], "Do not reserve the old, unrotated footprint.");
            Assert.IsFalse(mask[25 * 64 + 20], "Adjacent dry ground remains available.");
        }
        finally { Object.DestroyImmediate(mesh); Game.Rendering.Contracts.RenderBoundsGateway.Bind(provider); }
    }

    [Test]
    public void PaidRecruitsRallyOnceAndNeverOverridePlayerOrders()
    {
        using var world = new World("SkirmishRecruitRally");
        var em = world.EntityManager;
        em.SetComponentData(em.CreateEntity(typeof(GridConfig)), new GridConfig { Width = 256, Height = 256, CellSize = 1 });
        var home = Unit(em, 1, new float3(80, 0, 80), "Building_Barrack");
        var fresh = Unit(em, 1, new float3(80, 0, 80), "Soldier");
        var commanded = Unit(em, 1, new float3(80, 0, 80), "Soldier");
        foreach (var unit in new[] { fresh, commanded }) { em.AddComponent<UnitMove>(unit); em.AddComponent<SkirmishSquadMember>(unit); }
        em.AddComponent<HoldPositionOrderTag>(commanded);
        var root = em.CreateEntity();
        var produced = em.AddBuffer<BuildingProducedUnitReadModel>(root);
        produced.Add(new BuildingProducedUnitReadModel { Unit = fresh });
        produced.Add(new BuildingProducedUnitReadModel { Unit = commanded });
        var match = new SkirmishMatchState { Phase = SkirmishPhase.Playing, PlayerMainBase = home };
        SkirmishCombatPolicy.RallyPlayerReinforcements(em, match);
        Assert.IsTrue(em.HasComponent<UnitPathRequest>(fresh));
        Assert.IsTrue(em.HasComponent<SkirmishRallyAssigned>(fresh));
        Assert.IsFalse(em.HasComponent<ManualMoveOrderTag>(fresh), "Automatic rally must permit combat interruption.");
        Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(commanded));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(commanded));
        em.RemoveComponent<UnitPathRequest>(fresh);
        em.AddComponent<ManualMoveOrderTag>(fresh);
        em.AddComponentData(fresh, new UnitPathRequest { Goal = new int2(20, 30) });
        SkirmishCombatPolicy.RallyPlayerReinforcements(em, match);
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(fresh), "A later player Move retains its priority.");
        Assert.AreEqual(new int2(20, 30), em.GetComponentData<UnitPathRequest>(fresh).Goal);
        em.RemoveComponent<UnitPathRequest>(fresh);
        SkirmishCombatPolicy.RallyPlayerReinforcements(em, match);
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(fresh), "A completed rally is not reissued.");
    }

    [Test]
    public void SkirmishPausePopupFreezesSimulationAndRestoresItOnlyForTheSameActiveSession()
    {
        float originalScale = Time.timeScale;
        using var world = new World("SkirmishPauseOwnership");
        var em = world.EntityManager;
        try
        {
            Time.timeScale = 1;
            var gameplay = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
            em.SetComponentData(gameplay, new RuntimeGameplayStateComponent { PlayRequested = 1, SimulationActive = 1 });
            var match = em.CreateEntity(typeof(SkirmishMatchState));
            em.SetComponentData(match, new SkirmishMatchState { SessionId = "pause-test", Phase = SkirmishPhase.Playing });
            var popup = em.CreateEntity(typeof(Game.UI.Shell.Contracts.Ecs.UiShellActivePopupComponent));
            em.SetComponentData(popup, new Game.UI.Shell.Contracts.Ecs.UiShellActivePopupComponent { Visible = 1, PopupKind = Game.UI.Contracts.UiShellPopupKind.Pause });
            var system = world.CreateSystem<Game.UI.Shell.Ecs.MissionDefensePauseSystem>();
            system.Update(world.Unmanaged);
            Assert.Zero(Time.timeScale);
            Assert.Zero(em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).SimulationActive);
            em.SetComponentData(popup, new Game.UI.Shell.Contracts.Ecs.UiShellActivePopupComponent());
            system.Update(world.Unmanaged);
            Assert.AreEqual(1, Time.timeScale);
            Assert.AreEqual(1, em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).SimulationActive);
            em.SetComponentData(popup, new Game.UI.Shell.Contracts.Ecs.UiShellActivePopupComponent { Visible = 1, PopupKind = Game.UI.Contracts.UiShellPopupKind.Settings });
            system.Update(world.Unmanaged);
            em.SetComponentData(match, new SkirmishMatchState { SessionId = "pause-test", Phase = SkirmishPhase.Finished });
            system.Update(world.Unmanaged);
            Assert.Zero(Time.timeScale, "The result keeps ownership of the frozen world.");
            Assert.Zero(em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).SimulationActive, "Closing a terminal match must not restart simulation.");
            em.DestroyEntity(match);
            system.Update(world.Unmanaged);
            Assert.AreEqual(1, Time.timeScale, "Leaving skirmish must release the freeze for menu/replay.");
            Assert.Zero(em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).SimulationActive);
        }
        finally { Time.timeScale = originalScale; }
    }

    [Test]
    public void SkirmishResultWithoutPausePopupFreezesAndReleasesOnSessionTeardown()
    {
        float originalScale = Time.timeScale;
        using var world = new World("SkirmishResultFreeze");
        var em = world.EntityManager;
        try
        {
            Time.timeScale = 1;
            var gameplay = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
            var match = em.CreateEntity(typeof(SkirmishMatchState));
            em.SetComponentData(match, new SkirmishMatchState { SessionId = "result", Phase = SkirmishPhase.Finished });
            var system = world.CreateSystem<Game.UI.Shell.Ecs.MissionDefensePauseSystem>();
            system.Update(world.Unmanaged);
            Assert.Zero(Time.timeScale);
            Assert.Zero(em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).SimulationActive);
            system.Update(world.Unmanaged);
            Assert.Zero(Time.timeScale, "Repeated result updates must not restart the simulation.");
            em.DestroyEntity(match);
            system.Update(world.Unmanaged);
            Assert.AreEqual(1, Time.timeScale);
            Assert.Zero(em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).PlayRequested);
        }
        finally { Time.timeScale = originalScale; }
    }

    [Test]
    public void StartingUnitPreviewLookupDoesNotExpandRecruitmentCatalog()
    {
        var prefab = new GameObject("Unit_Veh_Light_Armored_Car");
        try
        {
            var lookup = new BuildingDefinitionPrefabSystemHelper();
            lookup.RebuildSpawnablesLookup(null, null);
            lookup.RegisterInitialUnitPreviewPrefab(prefab);
            Assert.IsTrue(lookup.TryResolveConfiguredUnitSpawnPrefab(prefab.name, out var resolved));
            Assert.AreSame(prefab, resolved);
            Assert.Zero(lookup.ConfiguredUnitCount);
            lookup.ClearConfiguredPrefabLookups();
            Assert.IsFalse(lookup.TryResolveConfiguredUnitSpawnPrefab(prefab.name, out _));
        }
        finally { Object.DestroyImmediate(prefab); }
    }

    [Test]
    public void InfantryDoesNotReserveBarracksExitForItsLifetimeInSkirmish()
    {
        using var world = new World("SkirmishSpawnSlot"); var em = world.EntityManager;
        var rifle = Unit(em, 2, default, "Unit_Chr_Soldier_Male_02_Alt_04");
        var car = Unit(em, 2, default, "Unit_Veh_Light_Armored_Car");
        Assert.IsFalse(SkirmishPopulationPolicy.ReleasesProductionSlot(em, rifle), "Campaign slot ownership is unchanged.");
        em.CreateEntity(typeof(SkirmishMatchState));
        Assert.IsTrue(SkirmishPopulationPolicy.ReleasesProductionSlot(em, rifle));
        Assert.IsFalse(SkirmishPopulationPolicy.ReleasesProductionSlot(em, car), "Vehicle parking stays reserved.");
        Assert.IsFalse(SkirmishPopulationPolicy.ReleasesProductionSlot(em, Entity.Null));
    }

    [Test]
    public void LateAuthoredSceneryNeverAddsCombatVehiclesToThePresetRoster()
    {
        using var world = new World("SkirmishLateScenery"); var em = world.EntityManager;
        var car = Unit(em, 1, default, "Unit_Veh_Light_Armored_Car");
        SkirmishWorldSetup.NormalizeScenery(em);
        var decoration = Unit(em, 1, default, "Unit_Veh_Light_Armored_Car");
        em.AddComponent<OperationMapAuthoredVehiclePresentation>(decoration);
        SkirmishSquadAssignment.Assign(em);
        Assert.IsTrue(em.HasComponent<SkirmishSquadMember>(car));
        Assert.IsFalse(em.HasComponent<SkirmishSquadMember>(decoration));
        SkirmishWorldSetup.NormalizeScenery(em);
        Assert.AreEqual(0, em.GetComponentData<Faction>(decoration).Id);
        Assert.AreEqual(1, em.GetComponentData<Faction>(car).Id);
        Assert.IsTrue(em.HasComponent<SkirmishSceneryNormalized>(decoration));
    }

    [Test]
    public void DestroyedSquadsReleaseSlotsWhileSurvivorsKeepTheirOrdersAndCampaignIsUnchanged()
    {
        using var world = new World("SkirmishSquadRetirement");
        var em = world.EntityManager;
        var dead = Unit(em, 2, default, "Soldier");
        em.SetComponentData(dead, new UnitHealth());
        var removed = Unit(em, 2, default, "Soldier");
        em.DestroyEntity(removed);
        var empty = em.CreateEntity(typeof(AISquad));
        var members = em.AddBuffer<AISquadUnit>(empty);
        members.Add(new AISquadUnit { Unit = dead });
        members.Add(new AISquadUnit { Unit = removed });
        SkirmishCombatPolicy.RetireEmptySquads(em);
        Assert.IsTrue(em.Exists(empty), "Campaign squad lifecycle stays unchanged.");
        em.CreateEntity(typeof(SkirmishMatchState));
        var survivor = Unit(em, 2, default, "Soldier");
        var active = em.CreateEntity(typeof(AISquad));
        em.SetComponentData(active, new AISquad { TargetEntity = survivor, LastOrderTime = 42 });
        var activeMembers = em.AddBuffer<AISquadUnit>(active);
        activeMembers.Add(new AISquadUnit { Unit = dead });
        activeMembers.Add(new AISquadUnit { Unit = survivor });
        SkirmishCombatPolicy.RetireEmptySquads(em);
        Assert.IsFalse(em.Exists(empty));
        Assert.IsTrue(em.Exists(active));
        Assert.AreEqual(1, em.GetBuffer<AISquadUnit>(active).Length);
        Assert.AreEqual(survivor, em.GetBuffer<AISquadUnit>(active)[0].Unit);
        Assert.AreEqual(42, em.GetComponentData<AISquad>(active).LastOrderTime);
    }

    [Test]
    public void EnemyWaitsForOpeningButDefendsImmediatelyAndReturnsToTheMainBaseObjective()
    {
        using var world=new World("SkirmishTargetPolicy");var em=world.EntityManager;
        var player=Unit(em,1,new float3(20,0,20),"Building_Barrack");
        var enemy=Unit(em,2,new float3(300,0,300),"Building_Barrack");
        em.AddComponent<RuntimeBuildingCombatTag>(player);em.AddComponent<RuntimeBuildingCombatTag>(enemy);
        var session=em.CreateEntity(typeof(SkirmishMatchState));
        var match=new SkirmishMatchState{Phase=SkirmishPhase.Playing,PlayerMainBase=player,EnemyMainBase=enemy};
        em.SetComponentData(session,match);
        var squad=em.CreateEntity(typeof(AISquad));em.SetComponentData(squad,new AISquad{FactionId=2});
        Assert.IsTrue(SkirmishCombatPolicy.TryAssignTargets(em));
        Assert.AreEqual(Entity.Null,em.GetComponentData<AISquad>(squad).TargetEntity);
        var truck=Unit(em,1,new float3(300,0,302),"Truck");em.AddComponent<UnitResourceHauler>(truck);
        var attacker=Unit(em,1,new float3(300,0,320),"Soldier");
        SkirmishCombatPolicy.TryAssignTargets(em);
        Assert.AreEqual(attacker,em.GetComponentData<AISquad>(squad).TargetEntity,"React to the combat threat, not the closer logistics vehicle.");
        em.SetComponentData(attacker,new UnitHealth());
        match.ElapsedSeconds=Resources.Load<SkirmishPresetConfig>(SkirmishPresetConfig.ResourceName).firstAttackSeconds;
        em.SetComponentData(session,match);SkirmishCombatPolicy.TryAssignTargets(em);
        Assert.AreEqual(player,em.GetComponentData<AISquad>(squad).TargetEntity);
    }

    [Test]
    public void ScenarioCombatValuesApplyEquallyToNewReplacementsWithoutHealingThem()
    {
        using var world=new World("SkirmishBalance");var em=world.EntityManager;
        var preset=Resources.Load<SkirmishPresetConfig>(SkirmishPresetConfig.ResourceName);
        var player=Unit(em,1,default,"Unit_Chr_Soldier_Test");
        var enemy=Unit(em,2,default,"Unit_Chr_Soldier_Test");
        SkirmishCombatPolicy.ApplyRoster(em,default);
        Assert.AreEqual(preset.rifleRange,em.GetComponentData<UnitAttack>(player).Range);
        Assert.AreEqual(em.GetComponentData<UnitAttack>(player).Damage,em.GetComponentData<UnitAttack>(enemy).Damage);
        em.SetComponentData(player,new UnitHealth{Current=7,Max=125});
        var replacement=Unit(em,2,default,"Unit_Chr_Soldier_Test");
        SkirmishCombatPolicy.ApplyRoster(em,default);
        Assert.AreEqual(preset.rifleDamage,em.GetComponentData<UnitAttack>(replacement).Damage);
        Assert.AreEqual(7,em.GetComponentData<UnitHealth>(player).Current);
    }

    [Test]
    public void AttackingSquadFightsNearbyDefendersBeforeResumingBaseAttack()
    {
        using var world = new World("SkirmishFieldDefense"); var em = world.EntityManager;
        var ownBase = Unit(em, 2, new float3(500, 0, 500), "Building_Barrack");
        var targetBase = Unit(em, 1, new float3(20, 0, 20), "Building_Barrack");
        em.AddComponent<RuntimeBuildingCombatTag>(ownBase); em.AddComponent<RuntimeBuildingCombatTag>(targetBase);
        var session = em.CreateEntity(typeof(SkirmishMatchState));
        em.SetComponentData(session, new SkirmishMatchState { Phase = SkirmishPhase.Playing, ElapsedSeconds = 300,
            PlayerMainBase = targetBase, EnemyMainBase = ownBase });
        var attacker = Unit(em, 2, new float3(50, 0, 20), "Soldier");
        var defender = Unit(em, 1, new float3(40, 0, 20), "Soldier");
        var truck = Unit(em, 1, new float3(49, 0, 20), "Truck"); em.AddComponent<UnitResourceHauler>(truck);
        var squad = em.CreateEntity(typeof(AISquad)); em.SetComponentData(squad, new AISquad { FactionId = 2 });
        em.AddBuffer<AISquadUnit>(squad).Add(new AISquadUnit { Unit = attacker });
        var trailingCar = Unit(em, 2, new float3(300, 0, 20), "Unit_Veh_Light_Armored_Car");
        em.GetBuffer<AISquadUnit>(squad).Add(new AISquadUnit { Unit = trailingCar });
        SkirmishCombatPolicy.TryAssignTargets(em);
        Assert.AreEqual(defender, em.GetComponentData<AISquad>(squad).TargetEntity,
            "Fight the defender near the attack squad even far from home; ignore the closer supply truck.");
        em.SetComponentData(defender, new UnitHealth());
        SkirmishCombatPolicy.TryAssignTargets(em);
        Assert.AreEqual(targetBase, em.GetComponentData<AISquad>(squad).TargetEntity);
    }

    [Test]
    public void AttackersEngageArmedTowersOnTheirRouteAndIgnoreUnarmedSupplyBuildings()
    {
        using var world = new World("SkirmishTowerThreat"); var em = world.EntityManager;
        var own = Unit(em, 2, new float3(500, 0, 500), "Building_Barrack");
        var objective = Unit(em, 1, new float3(20, 0, 20), "Building_Barrack");
        em.AddComponent<RuntimeBuildingCombatTag>(own); em.AddComponent<RuntimeBuildingCombatTag>(objective);
        var session = em.CreateEntity(typeof(SkirmishMatchState));
        em.SetComponentData(session, new SkirmishMatchState { Phase = SkirmishPhase.Playing, ElapsedSeconds = 300,
            PlayerMainBase = objective, EnemyMainBase = own });
        var attacker = Unit(em, 2, new float3(150, 0, 20), "Soldier");
        var tower = Unit(em, 1, new float3(140, 0, 20), "Building_Guard_Tower");
        em.AddComponent<RuntimeBuildingCombatTag>(tower);
        var depot = Unit(em, 1, new float3(149, 0, 20), "Building_Depot");
        em.AddComponent<RuntimeBuildingCombatTag>(depot); em.SetComponentData(depot, new UnitAttack());
        var squad = em.CreateEntity(typeof(AISquad)); em.SetComponentData(squad, new AISquad { FactionId = 2 });
        em.AddBuffer<AISquadUnit>(squad).Add(new AISquadUnit { Unit = attacker });
        SkirmishCombatPolicy.TryAssignTargets(em);
        Assert.AreEqual(tower, em.GetComponentData<AISquad>(squad).TargetEntity);
        em.SetComponentData(tower, new UnitHealth());
        SkirmishCombatPolicy.TryAssignTargets(em);
        Assert.AreEqual(objective, em.GetComponentData<AISquad>(squad).TargetEntity);
    }

    [Test]
    public void LateSceneSpawnerCannotOverwriteTheSelectedSkirmishResources()
    {
        using var world=new World("SkirmishLateSceneSpawner");var em=world.EntityManager;
        var sceneSpawner=em.CreateEntity(typeof(InitialUnitsSpawnConfig),typeof(InitialUsableFuelStorageSeedPending),typeof(InitialUnitsBlockerChurnConfig));
        em.SetComponentData(sceneSpawner,new InitialUnitsSpawnConfig{InitialFuel=10000});
        SkirmishWorldSetup.SuppressUnselectedStartupConfigs(em);
        Assert.IsTrue(em.HasComponent<InitialUnitsSpawnConfig>(sceneSpawner),"Campaign scene spawners remain unchanged.");
        em.CreateEntity(typeof(SkirmishMatchState));
        var selected=em.CreateEntity(typeof(InitialUnitsSpawnConfig),typeof(CustomGameStartupStateComponent));
        em.SetComponentData(selected,new InitialUnitsSpawnConfig{InitialFuel=160});
        SkirmishWorldSetup.SuppressUnselectedStartupConfigs(em);
        Assert.IsTrue(em.Exists(sceneSpawner),"Keep the streamed entity and its rendering registry alive.");
        Assert.IsFalse(em.HasComponent<InitialUnitsSpawnConfig>(sceneSpawner));
        Assert.IsFalse(em.HasComponent<InitialUsableFuelStorageSeedPending>(sceneSpawner));
        Assert.IsFalse(em.HasComponent<InitialUnitsBlockerChurnConfig>(sceneSpawner));
        Assert.AreEqual(160,em.GetComponentData<InitialUnitsSpawnConfig>(selected).InitialFuel);
        var later=em.CreateEntity(typeof(InitialUnitsSpawnConfig));
        SkirmishWorldSetup.SuppressUnselectedStartupConfigs(em);
        Assert.IsFalse(em.HasComponent<InitialUnitsSpawnConfig>(later),"Also suppress a spawner streamed after the first check.");
    }

    [Test]
    public void SkirmishCatalogUsesOnlyItsPresetAndDoesNotRestrictCampaign()
    {
        using var world=new World("SkirmishCatalog");var em=world.EntityManager;
        var unsupported=new GameObject("UnsupportedCampaignUnit");
        try
        {
            Assert.IsTrue(SkirmishCatalogPolicy.Allows(em,unsupported,false));
            em.CreateEntity(typeof(SkirmishMatchState));
            Assert.IsFalse(SkirmishCatalogPolicy.Allows(em,unsupported,false));
            Assert.IsFalse(SkirmishCatalogPolicy.Allows(em,unsupported,true));
            var preset=Resources.Load<SkirmishPresetConfig>(SkirmishPresetConfig.ResourceName);
            Assert.AreEqual(6,preset.buildingPlacement.Spawnables.Count);
            Assert.AreEqual(3,preset.buildingPlacement.UnitPrefabRegistryConfig.UnitSpawnPrefabs.Count);
            foreach(var prefab in preset.buildingPlacement.Spawnables)
                Assert.IsTrue(SkirmishCatalogPolicy.Allows(em,prefab,true));
            foreach(var prefab in preset.buildingPlacement.UnitPrefabRegistryConfig.UnitSpawnPrefabs)
                Assert.IsTrue(SkirmishCatalogPolicy.Allows(em,prefab,false));
        }
        finally {Object.DestroyImmediate(unsupported);}
    }

    [Test]
    public void FailedStartupKeepsItsCauseAndDoesNotBecomeAPlayedResult()
    {
        using var world=new World("SkirmishStartupFailure");var em=world.EntityManager;
        var session=em.CreateEntity(typeof(SkirmishMatchState));
        em.SetComponentData(session,new SkirmishMatchState{Phase=SkirmishPhase.Preparing,Seed=99});
        Assert.IsTrue(SkirmishStartupPolicy.Fail(em,SkirmishStartupFailureCode.BuildingPlacement));
        Assert.IsFalse(SkirmishStartupPolicy.Fail(em,SkirmishStartupFailureCode.Timeout));
        var match=em.GetComponentData<SkirmishMatchState>(session);
        Assert.AreEqual(SkirmishStartupFailureCode.BuildingPlacement,match.StartupFailure);
        Assert.AreEqual(SkirmishOutcome.None,match.Outcome);
        Assert.AreEqual(SkirmishPhase.Preparing,match.Phase);
        Assert.IsFalse(SkirmishOutcomeRules.Evaluate(ref match,false,false,900,true));
        var gameplay=em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(gameplay,new RuntimeGameplayStateComponent{PlayRequested=1,SimulationActive=1,SelectionModeActive=1,BuildModeActive=1});
        SkirmishStartupPolicy.StopFailedStartup(em);
        Assert.AreEqual(0,em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).SimulationActive);
        Assert.AreEqual(0,em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).PlayRequested);
    }

    [Test]
    public void StartupFailureCannotInterruptCampaignOrAnActiveSkirmish()
    {
        using var world=new World("StartupFailureIsolation");var em=world.EntityManager;
        Assert.IsFalse(SkirmishStartupPolicy.Fail(em,SkirmishStartupFailureCode.BuildingPlacement));
        var session=em.CreateEntity(typeof(SkirmishMatchState));
        em.SetComponentData(session,new SkirmishMatchState{Phase=SkirmishPhase.Playing});
        Assert.IsFalse(SkirmishStartupPolicy.Fail(em,SkirmishStartupFailureCode.Timeout));
        Assert.AreEqual(SkirmishStartupFailureCode.None,em.GetComponentData<SkirmishMatchState>(session).StartupFailure);
    }

    [Test]
    public void CampaignWorldDoesNotEnterSkirmishTargetSelection()
    {
        using var world=new World("CampaignPolicyIsolation");
        Assert.IsFalse(SkirmishCombatPolicy.TryAssignTargets(world.EntityManager));
    }

    private static Entity Unit(EntityManager em,byte faction,float3 position,string key)
    {
        var entity=em.CreateEntity(typeof(Faction),typeof(UnitHealth),typeof(UnitAttack),typeof(UnitCombat),typeof(UnitGrid),typeof(LocalTransform),typeof(UnitSourcePrefabKey));
        em.SetComponentData(entity,new Faction{Id=faction});
        em.SetComponentData(entity,new UnitHealth{Current=125,Max=125});
        em.SetComponentData(entity,new UnitAttack{Range=85,Damage=18,CooldownSeconds=.42f});
        em.SetComponentData(entity,LocalTransform.FromPosition(position));
        em.SetComponentData(entity,new UnitGrid{Cell=(int2)position.xz});
        em.SetComponentData(entity,new UnitSourcePrefabKey{Value=new Unity.Collections.FixedString64Bytes(key)});
        return entity;
    }
}
