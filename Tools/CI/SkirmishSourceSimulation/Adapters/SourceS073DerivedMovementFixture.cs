using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Disposable algorithm control forked from the actual S073 source-created unit
// roster. Activation and all mutable native storage belong only to this fixture.
internal static class SourceS073DerivedMovementFixture
{
    const float Delta = 1f / 60f;
    const int MaxTicks = 180;

    static string Sha(string path)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    static void CopyIfPresent<T>(EntityManager source, Entity from, EntityManager target, Entity to) where T : struct
    {
        if (source.HasComponent<T>(from)) target.AddComponentData(to, source.GetComponentData<T>(from));
    }

    static Entity CopySpatialEntity(EntityManager source, Entity from, EntityManager target)
    {
        Entity to = target.CreateEntity();
        target.AddComponentData(to, source.GetComponentData<UnitGrid>(from));
        target.AddComponentData(to, source.GetComponentData<UnitFootprint>(from));
        CopyIfPresent<Faction>(source, from, target, to);
        CopyIfPresent<LocalTransform>(source, from, target, to);
        CopyIfPresent<UnitMove>(source, from, target, to);
        CopyIfPresent<UnitMovementBehavior>(source, from, target, to);
        CopyIfPresent<UnitVehicleMovement>(source, from, target, to);
        CopyIfPresent<UnitVehicleKinematics>(source, from, target, to);
        CopyIfPresent<UnitAirMovement>(source, from, target, to);
        return to;
    }

    internal static JObject Run(EntityManager source, Entity sourceGrid, Entity session, string outputDirectory)
    {
        const string optIn = "WARLINE_S073_DERIVED_MOVEMENT_FIXTURE";
        if (!string.Equals(Environment.GetEnvironmentVariable(optIn), "1", StringComparison.Ordinal)) return null;

        RuntimeGameplayStateComponent sourceGameplayBefore;
        using (var matches = source.CreateEntityQuery(typeof(SkirmishMatchState)))
        using (var maps = source.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent)))
        using (var gameplay = source.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
        {
            if (matches.CalculateEntityCount() != 1 || maps.CalculateEntityCount() != 1 || gameplay.CalculateEntityCount() != 1 ||
                matches.GetSingletonEntity() != session || matches.GetSingleton<SkirmishMatchState>().Phase != SkirmishPhase.Preparing ||
                matches.GetSingleton<SkirmishMatchState>().Outcome != SkirmishOutcome.None ||
                gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive != 0)
                throw new InvalidDataException("Derived movement fixture requires the retained inactive Preparing source host");
            sourceGameplayBefore = gameplay.GetSingleton<RuntimeGameplayStateComponent>();
        }

        using var gridQuery = source.CreateEntityQuery(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent), typeof(PathPoolComponent));
        if (gridQuery.CalculateEntityCount() != 1 || gridQuery.GetSingletonEntity() != sourceGrid)
            throw new InvalidDataException("Unique actual source host movement grid required");
        GridConfig config = source.GetComponentData<GridConfig>(sourceGrid);
        long cellCountLong = (long)config.Width * config.Height;
        if (cellCountLong <= 0 || cellCountLong > 2_097_152) throw new InvalidDataException("Bounded exact source grid required");
        int cellCount = (int)cellCountLong;

        // Create fresh backing storage for every mutable native container. No source
        // NativeArray, NativeList, NativeBitArray or dynamic-buffer handle is reused.
        EntityManager fixture = new EntityManager();
        int nextId = 1;
        fixture.LogicalAllocatorAdmitted = true;
        fixture.AllocateLogicalIdentity = () => new Entity { Index = nextId++, Version = 1 };
        Entity grid = fixture.CreateEntity();
        fixture.AddComponentData(grid, config);
        CopyBuffer<GridWalkable>(source, sourceGrid, fixture, grid);
        CopyBuffer<GridRoad>(source, sourceGrid, fixture, grid);
        CopyBuffer<GridRoadSidewalk>(source, sourceGrid, fixture, grid);
        CopyBuffer<GridRoadDirt>(source, sourceGrid, fixture, grid);

        DynamicBlockerComponent sourceBlockers = source.GetComponentData<DynamicBlockerComponent>(sourceGrid);
        if (sourceBlockers.WriteInProgress != 0 || sourceBlockers.Counts.Length != cellCount ||
            sourceBlockers.FriendlyPassFactionIds.Length != cellCount || !sourceBlockers.Blocked.IsCreated || sourceBlockers.Blocked.Length != cellCount)
            throw new InvalidDataException("Committed source blocker arrays do not match the source grid");
        var blockerCopy = new DynamicBlockerComponent {
            GridSize = sourceBlockers.GridSize, Counts = new NativeArray<int>(sourceBlockers.Counts.ToArray()),
            FriendlyPassFactionIds = new NativeArray<byte>(sourceBlockers.FriendlyPassFactionIds.ToArray()),
            Blocked = new NativeBitArray(CloneBits(sourceBlockers.Blocked)), StorageEpoch = sourceBlockers.StorageEpoch,
            AppliedStorageEpoch = sourceBlockers.AppliedStorageEpoch, AppliedRevision = sourceBlockers.AppliedRevision,
            AppliedGrid = sourceBlockers.AppliedGrid, WriteInProgress = 0, EpochExhausted = sourceBlockers.EpochExhausted
        };
        fixture.AddComponentData(grid, blockerCopy);
        // The original occupancy owner rebuilds this from the copied source actors.
        fixture.AddComponentData(grid, new DynamicOccupancyComponent {
            GridSize = cellCount, Occupied = new NativeBitArray(new bool[cellCount])
        });
        fixture.AddComponentData(grid, new PathPoolComponent { Cells = new NativeList<int2>(Math.Max(64, source.GetComponentData<PathPoolComponent>(sourceGrid).Cells.Length), Allocator.Persistent) });

        using var actors = source.CreateEntityQuery(new EntityQueryDesc {
            All = new[] { ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitFootprint>() },
            None = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<Disabled>(),
                ComponentType.ReadOnly<StaticGridBlocker>(), ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
                ComponentType.ReadOnly<UnitDeathAnimationComponent>() }
        });
        using var actorEntities = actors.ToEntityArray(Allocator.Temp);
        if (actorEntities.Length < 1 || actorEntities.Length > 128) throw new InvalidDataException("Bounded actual source actor domain required");
        Entity unit = Entity.Null;
        Entity sourceUnit = Entity.Null;
        foreach (Entity actor in actorEntities)
        {
            Entity copy = CopySpatialEntity(source, actor, fixture);
            if (!source.HasComponent<Faction>(actor) || source.GetComponentData<Faction>(actor).Id != FactionIdentity.PlayerFactionId ||
                source.HasComponent<UnitAirMovement>(actor)) continue;
            if (new[] { typeof(LocalTransform), typeof(UnitMove), typeof(UnitMovementBehavior), typeof(UnitVehicleMovement), typeof(UnitVehicleKinematics) }
                .Any(t => !source.HasComponent(actor, t))) continue;
            if (unit == Entity.Null) { unit = copy; sourceUnit = actor; }
        }
        if (unit == Entity.Null) throw new InvalidDataException("No source-created player ground unit has the complete original movement-owner component set");

        // This runtime state is created only in the disposable fixture manager.
        Entity fixtureGameplay = fixture.CreateEntity();
        fixture.AddComponentData(fixtureGameplay, new RuntimeGameplayStateComponent { PlayRequested = 1, SimulationActive = 1 });
        var deferred = new OwnedEndSimulationBuffers();
        Entity deferredEntity = fixture.CreateEntity();
        fixture.AddComponentData(deferredEntity, new EndSimulationEntityCommandBufferSystem.Singleton { Owner = deferred });
        UnitMoveOrderRequestSystem.EnsureQueueEntity(fixture);
        fixture.WorldUnmanaged.Time = new TimeData { ElapsedTime = 0, DeltaTime = Delta };

        using var owners = new OriginalGroundOwners(fixture, generatedFresh: true);
        owners.OccupancyPhase();
        UnitGrid startGrid = fixture.GetComponentData<UnitGrid>(unit);
        LocalTransform startTransform = fixture.GetComponentData<LocalTransform>(unit);
        UnitFootprint footprint = fixture.GetComponentData<UnitFootprint>(unit);
        var walkable = fixture.GetBuffer<GridWalkable>(grid);
        var blockers = fixture.GetComponentData<DynamicBlockerComponent>(grid);
        var occupancy = fixture.GetComponentData<DynamicOccupancyComponent>(grid);
        int2 goal = default;
        bool found = false;
        foreach (int2 direction in new[] { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1) })
        {
            int2 candidate = startGrid.Cell + direction;
            if (candidate.x < 0 || candidate.y < 0 || candidate.x + footprint.Size.x > config.Width || candidate.y + footprint.Size.y > config.Height) continue;
            bool free = true;
            for (int y = candidate.y; y < candidate.y + footprint.Size.y && free; y++)
            for (int x = candidate.x; x < candidate.x + footprint.Size.x; x++)
            {
                int index = y * config.Width + x;
                if (walkable[index].Value == 0 || blockers.Counts[index] != 0 || blockers.Blocked.IsSet(index) || occupancy.Occupied.IsSet(index)) { free = false; break; }
            }
            if (free) { goal = candidate; found = true; break; }
        }
        if (!found) throw new InvalidDataException("No adjacent source-grid cell is clear in the cloned actor/blocker domain");
        int request = UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder(fixture, unit, goal);
        UnitMoveOrderRequestSystem.ProcessPendingRequests(fixture);
        if (!UnitMoveOrderRequestSystem.TryGetResult(fixture, request, out UnitMoveOrderResultElement result) || result.Issued == 0)
            throw new InvalidDataException("Original move-order owner rejected the disposable source-derived request");

        var beforeGameplay = fixture.GetComponentData<RuntimeGameplayStateComponent>(fixtureGameplay);
        int observedMovementTicks = 0;
        for (int tick = 0; tick < MaxTicks; tick++)
        {
            fixture.WorldUnmanaged.Time = new TimeData { ElapsedTime = (tick + 1) * (double)Delta, DeltaTime = Delta };
            owners.OccupancyPhase();
            owners.PathPhase();
            owners.MovementPhase();
            owners.EndSimulationPhase();
            observedMovementTicks++;
            UnitGrid nowGrid = fixture.GetComponentData<UnitGrid>(unit);
            LocalTransform nowTransform = fixture.GetComponentData<LocalTransform>(unit);
            if (!nowGrid.Cell.Equals(startGrid.Cell) || !nowTransform.Position.Equals(startTransform.Position)) break;
        }
        UnitGrid endGrid = fixture.GetComponentData<UnitGrid>(unit);
        LocalTransform endTransform = fixture.GetComponentData<LocalTransform>(unit);
        RuntimeGameplayStateComponent afterGameplay = fixture.GetComponentData<RuntimeGameplayStateComponent>(fixtureGameplay);
        using var sourceGameplay = source.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
        RuntimeGameplayStateComponent sourceAfterGameplay = sourceGameplay.GetSingleton<RuntimeGameplayStateComponent>();
        if (beforeGameplay.PlayRequested != afterGameplay.PlayRequested || beforeGameplay.SimulationActive != afterGameplay.SimulationActive ||
            sourceGameplayBefore.PlayRequested != sourceAfterGameplay.PlayRequested ||
            sourceGameplayBefore.SimulationActive != sourceAfterGameplay.SimulationActive)
            throw new InvalidDataException("Fixture changed activation outside its disposable manager");

        var output = new JObject {
            ["kind"] = "S073SourceDerivedMovementFixture", ["schema_version"] = 1,
            ["status"] = endTransform.Position.Equals(startTransform.Position) ? "NoMovementObserved" : "MovementObserved",
            ["scope"] = "ActualSourceSpawnedActorAndSourceGridDeepCopied_ToDisposableEntityManager_ExplicitFixtureActivation_OriginalMovePathOccupancyMovementOwners_NoAdmissionOrParityClaim",
            ["source_unit"] = new JObject { ["entity_index"] = sourceUnit.Index, ["entity_version"] = sourceUnit.Version,
                ["faction"] = source.GetComponentData<Faction>(sourceUnit).Id },
            ["fixture_unit"] = new JObject { ["entity_index"] = unit.Index, ["entity_version"] = unit.Version },
            ["fixture_grid"] = new JObject { ["width"] = config.Width, ["height"] = config.Height,
                ["cells"] = cellCount, ["source_actor_count"] = actorEntities.Length, ["copied_actor_count"] = actorEntities.Length,
                ["walkability_road_and_blocker_storage_deep_copied"] = true,
                ["occupancy_rebuilt_from_copied_source_actors"] = true, ["path_pool_fixture_local_empty"] = true,
                ["shared_mutable_native_handles"] = false },
            ["start_cell"] = new JArray(startGrid.Cell.x, startGrid.Cell.y), ["target_cell"] = new JArray(goal.x, goal.y),
            ["end_cell"] = new JArray(endGrid.Cell.x, endGrid.Cell.y),
            ["start_position"] = new JArray(startTransform.Position.x, startTransform.Position.y, startTransform.Position.z),
            ["end_position"] = new JArray(endTransform.Position.x, endTransform.Position.y, endTransform.Position.z),
            ["pathfinding_owner_invoked"] = true, ["occupancy_owner_invoked"] = true, ["movement_owner_invoked"] = true,
            ["positive_delta_time"] = Delta, ["bounded_movement_updates"] = observedMovementTicks,
            ["fixture_play_requested"] = beforeGameplay.PlayRequested, ["fixture_simulation_active"] = beforeGameplay.SimulationActive,
            ["source_play_requested_before"] = sourceGameplayBefore.PlayRequested,
            ["source_play_requested_after"] = sourceAfterGameplay.PlayRequested,
            ["source_simulation_active_before"] = sourceGameplayBefore.SimulationActive,
            ["source_simulation_active_after"] = sourceAfterGameplay.SimulationActive,
            ["source_play_requested_unchanged"] = sourceGameplayBefore.PlayRequested == sourceAfterGameplay.PlayRequested,
            ["source_simulation_active_unchanged"] = sourceGameplayBefore.SimulationActive == sourceAfterGameplay.SimulationActive,
            ["s073_admission_claimed"] = false, ["native_parity_claimed"] = false, ["mission_ready"] = false
        };
        string path = Path.Combine(outputDirectory, "s073-source-derived-movement-fixture.json");
        using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(file)) writer.Write(output.ToString(Formatting.Indented) + "\n");
        output["output_path"] = path;
        output["output_sha256"] = Sha(path);
        Console.WriteLine("[S073DerivedMovementFixture] result=" + (string)output["status"] + " boundedUpdates=" + observedMovementTicks + " sourceActivationUnchanged=1 admissionClaimed=0");
        return output;
    }

    static bool[] CloneBits(NativeBitArray source)
    {
        var copy = new bool[source.Length];
        for (int i = 0; i < copy.Length; i++) copy[i] = source.IsSet(i);
        return copy;
    }

    static void CopyBuffer<T>(EntityManager source, Entity from, EntityManager target, Entity to) where T : struct, IBufferElementData
    {
        var input = source.GetBuffer<T>(from);
        var output = target.AddBuffer<T>(to);
        for (int i = 0; i < input.Length; i++) output.Add(input[i]);
    }
}
