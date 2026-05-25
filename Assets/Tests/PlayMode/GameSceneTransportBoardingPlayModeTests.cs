#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameSceneTransportBoardingPlayModeTests
{
    private const string GameSceneName = "Game";
    private const string TransportHelicopterName = "Unit_Veh_Helicopter_Transport";
    private const int HelicopterBoardingClearanceCells = 1;

    [TearDown]
    public void TearDown()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.BuildModeActive = false;
        InitialUnitsRuntimeState.FullscreenMapOpen = false;
        InitialUnitsRuntimeState.FullscreenMapIsoMode = false;
        InitialUnitsRuntimeState.ZoomInHeld = false;
        InitialUnitsRuntimeState.ZoomOutHeld = false;
        InitialUnitsRuntimeState.SuppressNextWorldClick = false;
        InitialUnitsRuntimeState.PlayerAutoModeEnabled = false;
        Time.timeScale = 1f;
        SetLogAssertIgnoreFailingMessages(false);
    }

    [Test]
    public async Task GameScene_NearbySoldierClickingTransportHelipadArea_WalksAndBoards()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        Time.timeScale = 20f;

        SetLogAssertIgnoreFailingMessages(true);
        AsyncOperation load = SceneManager.LoadSceneAsync(GameSceneName, LoadSceneMode.Single);
        while (load != null && !load.isDone)
            await NextFrame();
        await NextFrame();
        await NextFrame();

        GameBootstrap bootstrap = null;
        for (int frame = 0; frame < 120 && bootstrap == null; frame++)
        {
            bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
            await NextFrame();
        }

        Assert.NotNull(bootstrap, "Game scene must contain GameBootstrap.");
        Assert.NotNull(bootstrap.SelectionUiCommand, "GameBootstrap must initialize selection command dependencies.");
        Assert.NotNull(bootstrap.SelectionUiReadModel, "GameBootstrap must initialize selection read-model dependencies.");
        Assert.NotNull(bootstrap.SelectionUiCamera, "GameBootstrap must initialize selection camera dependencies.");

        bootstrap.BeginGameplay();

        Entity transport = Entity.Null;
        Entity passenger = Entity.Null;
        EntityManager em = default;
        GridConfig grid = default;
        for (int frame = 0; frame < 1200; frame++)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                em = world.EntityManager;
                if (IsInitialSpawnReady(em) &&
                    TryGetGrid(em, out grid) &&
                    TryFindPlayerTransportHelicopter(em, out transport) &&
                    TryFindClosestPlayerSoldier(em, transport, out passenger))
                {
                    break;
                }
            }

            await NextFrame();
        }

        Assert.IsTrue(transport != Entity.Null, "Initial player base must spawn a transport helicopter.");
        Assert.IsTrue(passenger != Entity.Null, "Initial player base must spawn selectable soldiers.");
        Assert.IsTrue(em.HasComponent<UnitTransportCapacity>(transport), "Transport helicopter must have transport capacity baked into the real Game scene entity.");
        Assert.Greater(em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity, 0);
        Assert.IsTrue(
            TryFindNearbyTransportStagingCell(em, grid, transport, passenger, out int2 stagingCell),
            $"Test setup must find a free walkable staging cell near the real transport helicopter. transport={DescribeUnit(em, transport)} passenger={DescribeUnit(em, passenger)}");

        MoveUnitToCell(em, passenger, grid, stagingCell);
        await NextFrame();
        await NextFrame();
        Assert.IsFalse(
            IsWithinImmediateBoardingRange(em, transport, passenger),
            $"The staged soldier should start near the helicopter but outside instant boarding range so this validates the walk-to-board path. passenger={DescribeUnit(em, passenger)} transport={DescribeUnit(em, transport)}");

        var selectionState = new SelectionStateSystem();
        Assert.IsTrue(FocusUnitForTest(em, passenger, selectionState), "Test setup should cache the clicked soldier selection through the focused-unit lifecycle boundary.");
        Assert.NotNull(bootstrap.MainMenu, "Game scene must initialize the main menu controller used by toolbar pointer capture.");
        InvokeToolbarPointerCapture(bootstrap.MainMenu);

        Camera camera = bootstrap.WorldCamera != null ? bootstrap.WorldCamera : Camera.main;
        Assert.NotNull(camera, "Game scene must have a world camera for RTS selection.");
        Vector3 transportWorld = em.GetComponentData<LocalTransform>(transport).Position;
        PositionCameraForClick(camera, transportWorld);

        Vector3 clickWorld = transportWorld + new Vector3(14f, 0f, 0f);
        clickWorld.y = grid.Origin.y;
        Vector3 screen = camera.WorldToScreenPoint(clickWorld);
        Assert.Greater(screen.z, 0f, "Transport helipad click point must be in front of the camera.");

        Assert.NotNull(bootstrap.BuildingSelectionClick, "Game scene must initialize the building selection click boundary before selection input.");
        bootstrap.BuildingSelectionClick.HandleBuildingSelectionClick(
            bootstrap.BuildingSelectionClickContext,
            new Vector2(screen.x, screen.y));
        Assert.IsTrue(
            em.HasComponent<SelectedUnitTag>(passenger),
            "Building selection input must not consume selected soldiers when the click belongs to a boardable transport on a helipad.");
        Assert.IsFalse(
            em.HasComponent<UnitTarget>(passenger),
            $"Building selection input must not issue MoveOrderToBuilding before transport boarding. movement={DescribeMovementState(em, passenger)}");

        RemoveIfPresent<SelectedUnitTag>(em, passenger);
        await NextFrame();

        bool issuedBoardOrder = TryIssueBoardTransportForTest(
            em,
            selectionState,
            new Vector2(screen.x, screen.y),
            clickWorld);

        Assert.IsTrue(
            issuedBoardOrder,
            $"Clicking the real transport helicopter's helipad area should issue a boarding order for a nearby soldier in the real Game scene. transport={DescribeUnit(em, transport)} passenger={DescribeUnit(em, passenger)} clickWorld={clickWorld} transportAir={DescribeAirState(em, transport)}");
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger), "Selected soldier should receive UnitTransportBoardingTarget from the Game scene click path.");
        UnitTransportBoardingTarget boardingTarget = em.GetComponentData<UnitTransportBoardingTarget>(passenger);
        Assert.IsTrue(
            math.max(
                math.abs(boardingTarget.Goal.x - em.GetComponentData<UnitGrid>(transport).Cell.x),
                math.abs(boardingTarget.Goal.y - em.GetComponentData<UnitGrid>(transport).Cell.y)) <= HelicopterBoardingClearanceCells,
            $"Boarding goal must be next to the helicopter center, not a far stale footprint edge. boarding={DescribeBoardingTarget(em, passenger)} transport={DescribeUnit(em, transport)}");

        for (int frame = 0; frame < 2400; frame++)
        {
            if (em.Exists(passenger) && em.HasComponent<UnitTransportPassenger>(passenger))
                break;
            await NextFrame();
        }

        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passenger), $"Soldier should walk to and board the selected transport helicopter in the real Game scene. passenger={DescribeUnit(em, passenger)} transport={DescribeUnit(em, transport)} boarding={DescribeBoardingTarget(em, passenger)} movement={DescribeMovementState(em, passenger)} transportAir={DescribeAirState(em, transport)}");
        Assert.IsTrue(em.HasComponent<Disabled>(passenger), "Boarded soldier should be hidden/disabled while inside the helicopter.");
        Assert.IsTrue(TransportPassengerBufferContains(em, transport, passenger), "Transport passenger buffer should contain the boarded soldier.");
    }

    [Test]
    public async Task GameScene_TransportHelicopterExit_DropsAndDispersesPassengersOneByOne()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        Time.timeScale = 20f;

        SetLogAssertIgnoreFailingMessages(true);
        AsyncOperation load = SceneManager.LoadSceneAsync(GameSceneName, LoadSceneMode.Single);
        while (load != null && !load.isDone)
            await NextFrame();
        await NextFrame();
        await NextFrame();

        GameBootstrap bootstrap = null;
        for (int frame = 0; frame < 120 && bootstrap == null; frame++)
        {
            bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
            await NextFrame();
        }

        Assert.NotNull(bootstrap, "Game scene must contain GameBootstrap.");
        Assert.NotNull(bootstrap.SelectionUiCommand, "GameBootstrap must initialize selection command dependencies.");
        Assert.NotNull(bootstrap.SelectionUiReadModel, "GameBootstrap must initialize selection read-model dependencies.");
        Assert.NotNull(bootstrap.SelectionUiCamera, "GameBootstrap must initialize selection camera dependencies.");

        bootstrap.BeginGameplay();

        Entity transport = Entity.Null;
        Entity passengerA = Entity.Null;
        Entity passengerB = Entity.Null;
        EntityManager em = default;
        for (int frame = 0; frame < 1200; frame++)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                em = world.EntityManager;
                if (IsInitialSpawnReady(em) &&
                    TryFindPlayerTransportHelicopter(em, out transport) &&
                    TryFindClosestPlayerSoldiers(em, transport, out passengerA, out passengerB))
                {
                    break;
                }
            }

            await NextFrame();
        }

        Assert.IsTrue(transport != Entity.Null, "Initial player base must spawn a transport helicopter.");
        Assert.IsTrue(passengerA != Entity.Null && passengerB != Entity.Null, "Initial player base must spawn at least two soldiers for helicopter exit validation.");

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Clear();
        LoadPassengerForTest(em, transport, passengerA);
        LoadPassengerForTest(em, transport, passengerB);
        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(2, passengers.Length);

        Assert.IsTrue(RequestDisembarkTransportForTest(em, transport), "Test setup should queue and process a transport disembark command.");
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "Exit should start the rope disembark request in the real Game scene.");

        for (int frame = 0; frame < 2400; frame++)
        {
            bool finished =
                !em.HasComponent<UnitTransportRopeDisembarkRequest>(transport) &&
                PassengerFinishedRopeExit(em, passengerA) &&
                PassengerFinishedRopeExit(em, passengerB);
            if (finished)
                break;

            await NextFrame();
        }

        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "The real helicopter should finish the rope exit request after both passengers clear the landing point.");
        Assert.IsTrue(PassengerFinishedRopeExit(em, passengerA), $"First passenger should be active and clear of rope state. passenger={DescribeUnit(em, passengerA)}");
        Assert.IsTrue(PassengerFinishedRopeExit(em, passengerB), $"Second passenger should be active and clear of rope state. passenger={DescribeUnit(em, passengerB)}");
        Assert.AreNotEqual(em.GetComponentData<UnitGrid>(passengerA).Cell, em.GetComponentData<UnitGrid>(passengerB).Cell, "Exited passengers should disperse to separate cells instead of remaining stacked.");
        Assert.IsFalse(TransportPassengerBufferContains(em, transport, passengerA));
        Assert.IsFalse(TransportPassengerBufferContains(em, transport, passengerB));
    }

    private static async Task NextFrame()
    {
        await Task.Yield();
    }

    private static void SetLogAssertIgnoreFailingMessages(bool value)
    {
        System.Type logAssertType =
            System.Type.GetType("UnityEngine.TestTools.LogAssert, UnityEngine.TestRunner") ??
            System.Type.GetType("UnityEngine.TestTools.LogAssert, UnityEngine.TestFramework");
        PropertyInfo property = logAssertType?.GetProperty("ignoreFailingMessages", BindingFlags.Static | BindingFlags.Public);
        property?.SetValue(null, value);
    }

    private static void InvokeToolbarPointerCapture(MainMenuPlayUI mainMenu)
    {
        MethodInfo pointerDown = typeof(MainMenuPlayUI).GetMethod("OnToolbarUiPointerDown", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo mouseDown = typeof(MainMenuPlayUI).GetMethod("OnToolbarUiMouseDown", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(pointerDown, "Toolbar pointer capture method should exist.");
        Assert.NotNull(mouseDown, "Toolbar mouse capture method should exist.");
        pointerDown.Invoke(mainMenu, new object[] { null });
        mouseDown.Invoke(mainMenu, new object[] { null });
    }

    private static bool FocusUnitForTest(EntityManager em, Entity entity, SelectionStateSystem selectionState)
    {
        return new FocusedUnitLifecycleSystem().FocusUnitEntity(
            em,
            entity,
            selectionState,
            new UnitTargetOrderSystem(),
            "TransportBoardingPlayModeTest",
            "TransportBoardingPlayModeTest",
            null,
            null,
            null,
            null);
    }

    private static bool TryIssueBoardTransportForTest(
        EntityManager em,
        SelectionStateSystem selectionState,
        Vector2 screenPosition,
        Vector3 clickWorld)
    {
        TransportBoardingCommandSystem.Result result = new TransportBoardingCommandSystem().TryIssueBoardTransportOrderToClickedUnit(
            em,
            screenPosition,
            new UnitTransportBoardingSystem(),
            new UnitMoveOrderSystem(),
            selectionState,
            TryGetNoClickedUnit,
            (Vector2 _, EntityManager entityManager, out int2 cell, out Vector3 worldPoint) =>
            {
                worldPoint = clickWorld;
                if (!TryGetGrid(entityManager, out GridConfig grid))
                {
                    cell = default;
                    return false;
                }

                cell = GridUtils.WorldToCell(grid, clickWorld);
                return true;
            });

        return result.Accepted;
    }

    private static bool RequestDisembarkTransportForTest(EntityManager em, Entity transport)
    {
        var inputSystem = new RtsSelectionInputSystem();
        if (!inputSystem.QueueDisembarkTransportCommandRequest(transport, Time.frameCount) ||
            !inputSystem.TryGetCommandBuffers(
            out _,
            out Entity commandEntity,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out DynamicBuffer<RtsSelectionCommandResultElement> results))
        {
            return false;
        }

        bool processed = new SelectionTransportCommandRequestSystem().ProcessPendingRequests(
            em,
            commandEntity,
            requests,
            results,
            new TransportBoardingCommandSystem(),
            new UnitTransportBoardingSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateSystem(),
            TryGetNoClickedUnit,
            TryGetNoClickedCell);

        results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        return processed && results.Length > 0 && results[results.Length - 1].Accepted != 0;
    }

    private static bool TryGetNoClickedUnit(Vector2 screenPosition, EntityManager em, out Entity entity)
    {
        entity = Entity.Null;
        return false;
    }

    private static bool TryGetNoClickedCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        cell = default;
        worldPoint = default;
        return false;
    }

    private static bool IsInitialSpawnReady(EntityManager em)
    {
        using EntityQuery configs = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        using EntityQuery initialized = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());
        int total = configs.CalculateEntityCount();
        return total > 0 && initialized.CalculateEntityCount() >= total;
    }

    private static bool TryGetGrid(EntityManager em, out GridConfig grid)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        if (query.IsEmptyIgnoreFilter)
        {
            grid = default;
            return false;
        }

        grid = em.GetComponentData<GridConfig>(query.GetSingletonEntity());
        return true;
    }

    private static bool TryFindPlayerTransportHelicopter(EntityManager em, out Entity transport)
    {
        transport = Entity.Null;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<LocalToWorld>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.GetComponentData<Faction>(entity).Id != 0)
                continue;
            if (!SourceContains(em, entity, TransportHelicopterName))
                continue;

            transport = entity;
            return true;
        }

        return false;
    }

    private static bool TryFindClosestPlayerSoldier(EntityManager em, Entity transport, out Entity soldier)
    {
        soldier = Entity.Null;
        if (transport == Entity.Null || !em.Exists(transport) || !em.HasComponent<UnitGrid>(transport))
            return false;

        int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
        int bestScore = int.MaxValue;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitMovementBehavior>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<LocalToWorld>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.GetComponentData<Faction>(entity).Id != 0 ||
                em.HasComponent<UnitAirMovement>(entity) ||
                em.HasComponent<UnitTransportPassenger>(entity))
            {
                continue;
            }

            string source = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!source.StartsWith("Unit_Chr", System.StringComparison.OrdinalIgnoreCase) &&
                source.IndexOf("_Chr_", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
            int score = math.abs(cell.x - transportCell.x) + math.abs(cell.y - transportCell.y);
            if (score >= bestScore)
                continue;

            bestScore = score;
            soldier = entity;
        }

        return soldier != Entity.Null;
    }

    private static bool TryFindClosestPlayerSoldiers(EntityManager em, Entity transport, out Entity soldierA, out Entity soldierB)
    {
        soldierA = Entity.Null;
        soldierB = Entity.Null;
        if (transport == Entity.Null || !em.Exists(transport) || !em.HasComponent<UnitGrid>(transport))
            return false;

        int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
        int bestA = int.MaxValue;
        int bestB = int.MaxValue;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitMovementBehavior>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<LocalToWorld>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.GetComponentData<Faction>(entity).Id != 0 ||
                em.HasComponent<UnitAirMovement>(entity) ||
                em.HasComponent<UnitTransportPassenger>(entity))
            {
                continue;
            }

            string source = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!source.StartsWith("Unit_Chr", System.StringComparison.OrdinalIgnoreCase) &&
                source.IndexOf("_Chr_", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
            int score = math.abs(cell.x - transportCell.x) + math.abs(cell.y - transportCell.y);
            if (score < bestA)
            {
                bestB = bestA;
                soldierB = soldierA;
                bestA = score;
                soldierA = entity;
            }
            else if (score < bestB)
            {
                bestB = score;
                soldierB = entity;
            }
        }

        return soldierA != Entity.Null && soldierB != Entity.Null;
    }

    private static void SelectOnly(EntityManager em, Entity selected)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            if (em.HasComponent<SelectedUnitTag>(entities[i]))
                em.RemoveComponent<SelectedUnitTag>(entities[i]);
        }

        if (!em.HasComponent<SelectedUnitTag>(selected))
            em.AddComponent<SelectedUnitTag>(selected);
    }

    private static void PositionCameraForClick(Camera camera, Vector3 target)
    {
        camera.transform.position = target + new Vector3(0f, 90f, -90f);
        camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = Mathf.Max(camera.farClipPlane, 1000f);
        camera.orthographic = false;
    }

    private static bool TransportPassengerBufferContains(EntityManager em, Entity transport, Entity passenger)
    {
        if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
            return false;

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        for (int i = 0; i < passengers.Length; i++)
        {
            if (passengers[i].Passenger == passenger)
                return true;
        }

        return false;
    }

    private static void LoadPassengerForTest(EntityManager em, Entity transport, Entity passenger)
    {
        RemoveIfPresent<SelectedUnitTag>(em, passenger);
        RemoveIfPresent<UnitTarget>(em, passenger);
        RemoveIfPresent<UnitPathRequest>(em, passenger);
        RemoveIfPresent<UnitPathFollow>(em, passenger);
        RemoveIfPresent<UnitPathRange>(em, passenger);
        RemoveIfPresent<ManualMoveOrderTag>(em, passenger);
        RemoveIfPresent<UnitPathRetryCooldown>(em, passenger);
        RemoveIfPresent<UnitLongDistanceMove>(em, passenger);
        RemoveIfPresent<UnitTransportBoardingTarget>(em, passenger);
        RemoveIfPresent<UnitTransportRopeDropState>(em, passenger);
        RemoveIfPresent<UnitTransportRopeDisperseState>(em, passenger);
        RemoveIfPresent<UnitTransportRopeLandingClearance>(em, passenger);

        if (em.HasComponent<UnitTransportPassenger>(passenger))
            em.SetComponentData(passenger, new UnitTransportPassenger { Transport = transport });
        else
            em.AddComponentData(passenger, new UnitTransportPassenger { Transport = transport });
        if (!em.HasComponent<Disabled>(passenger))
            em.AddComponent<Disabled>(passenger);

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = passenger });
    }

    private static bool PassengerFinishedRopeExit(EntityManager em, Entity passenger)
    {
        return em.Exists(passenger) &&
               !em.HasComponent<Disabled>(passenger) &&
               !em.HasComponent<UnitTransportPassenger>(passenger) &&
               !em.HasComponent<UnitTransportRopeDropState>(passenger) &&
               !em.HasComponent<UnitTransportRopeDisperseState>(passenger) &&
               !em.HasComponent<UnitTransportRopeLandingClearance>(passenger);
    }

    private static bool TryFindNearbyTransportStagingCell(EntityManager em, GridConfig grid, Entity transport, Entity passenger, out int2 stagingCell)
    {
        stagingCell = default;
        if (!em.Exists(transport) || !em.HasComponent<UnitGrid>(transport) || !em.HasComponent<UnitFootprint>(transport))
            return false;

        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerData>(),
            ComponentType.ReadOnly<DynamicOccupancyData>());
        Entity gridEntity = query.GetSingletonEntity();
        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBlockerData blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
        DynamicOccupancyData occupancy = em.GetComponentData<DynamicOccupancyData>(gridEntity);
        int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
        int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
        int2 passengerSize = em.HasComponent<UnitFootprint>(passenger)
            ? em.GetComponentData<UnitFootprint>(passenger).Size
            : new int2(1, 1);

        int boardingClearance = em.HasComponent<UnitAirMovement>(transport) ? HelicopterBoardingClearanceCells : 4;
        int startRadius = math.max(18, math.max(transportSize.x, transportSize.y) + boardingClearance + 4);
        int endRadius = startRadius + 18;
        for (int radius = startRadius; radius <= endRadius; radius++)
        {
            int steps = math.max(1, radius * 8);
            for (int step = 0; step < steps; step++)
            {
                int2 candidate = transportCell + SquareRingOffset(radius, step);
                if (!GridUtils.InBounds(candidate, grid.Width, grid.Height))
                    continue;
                if (UnitFootprintUtility.ContainsCellWithPadding(transportCell, transportSize, candidate, boardingClearance))
                    continue;
                if (!CanStagePassengerAt(grid, walkable, blockerData, occupancy, candidate, passengerSize, 0))
                    continue;

                stagingCell = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool CanStagePassengerAt(
        GridConfig grid,
        DynamicBuffer<GridWalkable> walkable,
        DynamicBlockerData blockerData,
        DynamicOccupancyData occupancy,
        int2 cell,
        int2 footprintSize,
        byte factionId)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
            return false;

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int index = row + x;
                if ((uint)index >= (uint)walkable.Length || walkable[index].Value == 0)
                    return false;
                if (blockerData.Blocked.IsCreated &&
                    blockerData.Blocked.IsSet(index) &&
                    (!blockerData.FriendlyPassFactionIds.IsCreated ||
                     (uint)index >= (uint)blockerData.FriendlyPassFactionIds.Length ||
                     blockerData.FriendlyPassFactionIds[index] != factionId))
                {
                    return false;
                }

                if (occupancy.Occupied.IsCreated && occupancy.Occupied.IsSet(index))
                    return false;
            }
        }

        return true;
    }

    private static void MoveUnitToCell(EntityManager em, Entity entity, GridConfig grid, int2 cell)
    {
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        float3 position = GridUtils.CellToWorldCenter(grid, cell);
        if (em.HasComponent<LocalTransform>(entity))
            em.SetComponentData(entity, LocalTransform.FromPosition(position));
        if (em.HasComponent<LocalToWorld>(entity))
            em.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });

        RemoveIfPresent<UnitTarget>(em, entity);
        RemoveIfPresent<UnitPathRequest>(em, entity);
        RemoveIfPresent<UnitPathFollow>(em, entity);
        RemoveIfPresent<UnitPathRange>(em, entity);
        RemoveIfPresent<ManualMoveOrderTag>(em, entity);
        RemoveIfPresent<UnitPathRetryCooldown>(em, entity);
        RemoveIfPresent<UnitLongDistanceMove>(em, entity);
        RemoveIfPresent<UnitTransportBoardingTarget>(em, entity);
    }

    private static bool IsWithinImmediateBoardingRange(EntityManager em, Entity transport, Entity passenger)
    {
        int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
        int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
        int2 passengerCell = em.GetComponentData<UnitGrid>(passenger).Cell;
        int boardingClearance = em.HasComponent<UnitAirMovement>(transport) ? HelicopterBoardingClearanceCells : 4;
        return UnitFootprintUtility.ContainsCellWithPadding(transportCell, transportSize, passengerCell, boardingClearance) ||
               math.max(math.abs(passengerCell.x - transportCell.x), math.abs(passengerCell.y - transportCell.y)) <= 2;
    }

    private static int2 SquareRingOffset(int radius, int step)
    {
        int topLen = (2 * radius) + 1;
        if (step < topLen)
            return new int2(-radius + step, radius);

        step -= topLen;
        int rightLen = 2 * radius;
        if (step < rightLen)
            return new int2(radius, (radius - 1) - step);

        step -= rightLen;
        int bottomLen = 2 * radius;
        if (step < bottomLen)
            return new int2((radius - 1) - step, -radius);

        step -= bottomLen;
        return new int2(-radius, (-radius + 1) + step);
    }

    private static bool SourceContains(EntityManager em, Entity entity, string text)
    {
        return em.HasComponent<UnitSourcePrefabKey>(entity) &&
               em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString().IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string DescribeUnit(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return "<missing>";

        string source = em.HasComponent<UnitSourcePrefabKey>(entity)
            ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
            : em.GetName(entity);
        string cell = em.HasComponent<UnitGrid>(entity)
            ? em.GetComponentData<UnitGrid>(entity).Cell.ToString()
            : "<no-cell>";
        return $"{source}@{cell}";
    }

    private static string DescribeBoardingTarget(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity) || !em.HasComponent<UnitTransportBoardingTarget>(entity))
            return "<none>";

        UnitTransportBoardingTarget boarding = em.GetComponentData<UnitTransportBoardingTarget>(entity);
        return $"transport={DescribeUnit(em, boarding.Transport)} goal={boarding.Goal}";
    }

    private static string DescribeAirState(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity) || !em.HasComponent<UnitAirState>(entity))
            return "<none>";

        UnitAirState air = em.GetComponentData<UnitAirState>(entity);
        return $"airborne={air.Airborne} takeoff={air.TakeoffRolling} landing={air.LandingRolling} returning={air.ReturningHome} homeInit={air.HomeInitialized}";
    }

    private static string DescribeMovementState(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return "<missing>";

        System.Text.StringBuilder builder = new();
        builder.Append(em.HasComponent<SelectedUnitTag>(entity) ? "selected=1" : "selected=0");
        if (em.HasComponent<UnitTarget>(entity))
            builder.Append($" target={em.GetComponentData<UnitTarget>(entity).Cell}");
        if (em.HasComponent<UnitPathRequest>(entity))
            builder.Append($" request={em.GetComponentData<UnitPathRequest>(entity).Goal}");
        if (em.HasComponent<UnitPathFollow>(entity))
            builder.Append($" followIndex={em.GetComponentData<UnitPathFollow>(entity).PathIndex}");
        if (em.HasComponent<UnitPathRange>(entity))
        {
            UnitPathRange range = em.GetComponentData<UnitPathRange>(entity);
            builder.Append($" pathRange={range.Start}+{range.Length}");
        }
        builder.Append(em.HasComponent<ManualMoveOrderTag>(entity) ? " manual=1" : " manual=0");
        if (em.HasComponent<UnitLongDistanceMove>(entity))
            builder.Append($" longGoal={em.GetComponentData<UnitLongDistanceMove>(entity).FinalGoal}");
        if (em.HasComponent<UnitPathRetryCooldown>(entity))
            builder.Append($" retryUntil={em.GetComponentData<UnitPathRetryCooldown>(entity).ResumeFrame}");
        if (em.HasComponent<UnitVehicleKinematics>(entity))
        {
            UnitVehicleKinematics kinematics = em.GetComponentData<UnitVehicleKinematics>(entity);
            builder.Append($" speed={kinematics.CurrentSpeed:0.00} stall={kinematics.StallSeconds:0.00}");
        }

        return builder.ToString();
    }

    private static void RemoveIfPresent<T>(EntityManager em, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            em.RemoveComponent<T>(entity);
    }
}
#endif
