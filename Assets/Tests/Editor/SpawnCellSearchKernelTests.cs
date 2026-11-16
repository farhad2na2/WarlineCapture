using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using Game.Runtime.Spawning;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Tests.Editor
{
    public sealed class SpawnCellSearchKernelTests
    {
        public static int RunChecks()
        {
            var test = new SpawnCellSearchKernelTests();
            test.OriginalSourcesPinned(); test.RandomAndSeedManagedAndBurst();
            test.SearchMatchesOriginalManagedAndBurst(); test.SearchBoundaryControls();
            test.SurfaceMatchesOriginalNativeAndBurst();
            Debug.Log("[SpawnCellSearchParity] result=Passed checks=5 searchCases=384 randomCases=4096 surfaceCases=36 burst=Passed missionReady=0");
            return 5;
        }
        [Serializable] private sealed class PlacementTrace
        {
            public int schema_version = 1;
            public string scope = "NativeAdapterFixture";
            public PlacementTraceCase[] cases;
        }
        [Serializable] private sealed class PlacementTraceCase
        {
            public string id, stage;
            public int width, height, center_x, center_z, radius, size_x, size_z;
            public bool is_air, has_match, found;
            public int surface_count, x, z;
            public uint random_state_before, random_state_after;
            public int[] walkable, blocked, occupied, reserved_before, reserved_after, surface_admission;
        }
        private static int[] Bits(NativeBitArray bits)
        { var result = new int[bits.Length]; for (int i = 0; i < result.Length; i++) result[i] = bits.IsSet(i) ? 1 : 0; return result; }
        private static int[] Walk(NativeArray<GridWalkable> cells)
        { var result = new int[cells.Length]; for (int i = 0; i < result.Length; i++) result[i] = cells[i].Value; return result; }
        public static void EmitNativeTrace()
        {
            var rows = new System.Collections.Generic.List<PlacementTraceCase>();
            for (int scenario = 0; scenario < 10; scenario++)
            {
                int width = scenario == 8 ? 80 : 9, height = scenario == 8 ? 1 : 8, count = width * height;
                using var walkOwner = new NativeArray<GridWalkable>(count, Allocator.Temp); var walk = walkOwner;
                using var blockedOwner = new NativeBitArray(count, Allocator.Temp); var blocked = blockedOwner;
                using var occupiedOwner = new NativeBitArray(count, Allocator.Temp); var occupied = occupiedOwner;
                using var reservedOwner = new NativeBitArray(count, Allocator.Temp); var reserved = reservedOwner;
                for (int i = 0; i < count; i++) walk[i] = new GridWalkable { Value = (byte)(scenario == 3 || scenario == 8 ? 0 : 1) };
                var center = scenario == 8 ? default : scenario == 6 ? new int2(-2, 20) : scenario == 7 ? new int2(8, 7) : new int2(4, 4);
                var size = scenario == 7 ? new int2(2) : scenario == 9 ? new int2(3, 2) : new int2(1);
                int radius = scenario == 2 || scenario == 8 ? 0 : 2; bool air = scenario >= 4 && scenario <= 6;
                if (scenario == 1 || scenario == 2) blocked.Set(40, true);
                if (scenario == 4) { blocked.Set(40, true); reserved.Set(40, true); }
                if (scenario == 5) reserved.Set(40, true);
                if (scenario == 8) walk[79] = new GridWalkable { Value = 1 };
                if (scenario == 9) for (int i = 0; i < count; i++) { occupied.Set(i, i % 13 == 0); reserved.Set(i, i % 17 == 0); }
                var random = new Unity.Mathematics.Random((uint)scenario + 711);
                var row = new PlacementTraceCase { id = "search-" + scenario, stage = "initial_search", width = width, height = height, center_x = center.x, center_z = center.y, radius = radius, size_x = size.x, size_z = size.y, is_air = air, random_state_before = random.state, walkable = Walk(walk), blocked = Bits(blocked), occupied = Bits(occupied), reserved_before = Bits(reserved), surface_admission = Array.Empty<int>() };
                var grid = new GridConfig { Width = width, Height = height, CellSize = 1 };
                row.found = InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref random, grid, walk, blocked, occupied, ref reserved, center, radius, size, air, out var cell);
                row.x = cell.x; row.z = cell.y; row.random_state_after = random.state; row.reserved_after = Bits(reserved); rows.Add(row);
            }
            using (var world = new World("SpawnSurfaceNativeTrace"))
            {
                var em = world.EntityManager; var prefab = em.CreateEntity(typeof(UnitMovementBehavior)); var match = em.CreateEntity();
                for (int scenario = 0; scenario < 8; scenario++)
                {
                    bool hasMatch = scenario != 0; int surfaceCount = scenario == 1 ? 0 : scenario == 2 ? 2 : 1;
                    if (hasMatch && !em.HasComponent<SkirmishMatchState>(match)) em.AddComponent<SkirmishMatchState>(match);
                    using var blob = SurfaceBlob(9, 8, scenario % 4);
                    var component = new MapSurfaceComponent { SurfaceBlob = blob, HasSurfaceData = 1, CellSize = 1, Dimensions = new int2(9, 8) };
                    Entity first = Entity.Null, second = Entity.Null;
                    if (surfaceCount > 0) { first = em.CreateEntity(typeof(MapSurfaceComponent)); em.SetComponentData(first, component); }
                    if (surfaceCount > 1) { second = em.CreateEntity(typeof(MapSurfaceComponent)); em.SetComponentData(second, component); }
                    try
                    {
                        using var walkOwner = new NativeArray<GridWalkable>(72, Allocator.Temp); var walk = walkOwner;
                        using var blockedOwner = new NativeBitArray(72, Allocator.Temp); var blocked = blockedOwner;
                        using var occupiedOwner = new NativeBitArray(72, Allocator.Temp); var occupied = occupiedOwner;
                        using var reservedOwner = new NativeBitArray(72, Allocator.Temp); var reserved = reservedOwner;
                        for (int i = 0; i < 72; i++) walk[i] = new GridWalkable { Value = 1 };
                        var grid = new GridConfig { Width = 9, Height = 8, CellSize = 1 }; var size = scenario == 7 ? new int2(2) : new int2(1);
                        var cell = new int2(4, 4); reserved.Set(40, true); if (size.x == 2) { reserved.Set(41, true); reserved.Set(49, true); reserved.Set(50, true); }
                        var admission = new int[72]; var validation = new MapSurfaceTraversalValidation();
                        for (int i = 0; i < 72; i++) admission[i] = validation.CanTraverseFootprint(component, 1, grid, new int2(i % 9, i / 9), size, false) ? 1 : 0;
                        var row = new PlacementTraceCase { id = "surface-" + scenario, stage = "surface_correction", width = 9, height = 8, center_x = cell.x, center_z = cell.y, size_x = size.x, size_z = size.y, has_match = hasMatch, surface_count = surfaceCount, walkable = Walk(walk), blocked = Bits(blocked), occupied = Bits(occupied), reserved_before = Bits(reserved), surface_admission = admission };
                        row.found = SkirmishSpawnPlacement.EnsureTraversable(em, grid, walk, blocked, occupied, ref reserved, prefab, size, ref cell);
                        row.x = cell.x; row.z = cell.y; row.reserved_after = Bits(reserved); rows.Add(row);
                    }
                    finally { if (first != Entity.Null) em.DestroyEntity(first); if (second != Entity.Null) em.DestroyEntity(second); }
                }
            }
            Debug.Log("[SpawnPlacementNativeTrace] " + JsonUtility.ToJson(new PlacementTrace { cases = rows.ToArray() }));
        }
        [Test] public void OriginalSourcesPinned()
        {
            string[] names = { "SpawnCellUtility", "InitialUnitsSpawnSystem", "SkirmishSpawnPlacement", "SkirmishTypedSpawnConstraint" };
            string[] hashes = { "862be7043f62b164560c32acc7a083449ce896961bd898423ceb775d6781cc67", "d97e5bf0f4ebdfaa97188a17e9cc68e6eedf23b5e6fd5ef5a473f325a321fe83", "110b88f5ef7fbd9395be4c93528387965cd5ecbc371511f32037dad3b290fd74", "44d814fe29405323e081202a2088600bfcf918d6f496e6f1486a0883423ad403" };
            using var sha = SHA256.Create();
            for (int i = 0; i < names.Length; i++)
            {
                string source = File.ReadAllText(Path.Combine(Application.dataPath, "Tests/Editor/" + names[i] + "PlacementOriginal.cs.txt")).Replace("\r\n", "\n");
                Assert.AreEqual(hashes[i], BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(source))).Replace("-", "").ToLowerInvariant());
            }
        }
        private struct Storage : ISpawnCellStorage
        {
            public NativeArray<GridWalkable> Walkable;
            public NativeBitArray Blocked, Occupied, Reserved;
            public bool IsWalkable(int index) => Walkable[index].Value != 0;
            public bool IsBlocked(int index) => Blocked.IsSet(index);
            public bool IsOccupied(int index) => Occupied.IsSet(index);
            public bool IsReserved(int index) => Reserved.IsSet(index);
            public void SetReserved(int index, bool value) => Reserved.Set(index, value);
        }
        [BurstCompile(CompileSynchronously = true)] private struct RandomJob : IJob
        {
            public NativeArray<int> Errors, Managed;
            public NativeArray<byte> StableBytes;
            [BurstDiscard] private static void MarkManaged(ref int value) { value = 1; }
            public void Execute()
            {
                int flag = 0; MarkManaged(ref flag); Managed[0] = flag;
                int errors = 0;
                for (int i = 0; i < 4096; i++)
                {
                    uint seed = unchecked((uint)i * 74983u + 1u);
                    var native = new Unity.Mathematics.Random(seed); var pure = SpawnCellRandom.FromSeed(seed);
                    if (native.state != pure.State) errors++;
                    for (int draw = 0; draw < 17; draw++)
                    {
                        int minimum = -i - draw, maximum = i + draw;
                        if (native.NextInt(minimum, maximum) != pure.NextInt(minimum, maximum) || native.state != pure.State) errors++;
                    }
                    uint expectedSeed = 2166136261u ^ unchecked((uint)-i), actualSeed = PlacementSeed.Begin(-i);
                    for (int b = 0; b < StableBytes.Length; b++)
                    { expectedSeed = unchecked((expectedSeed ^ StableBytes[b]) * 16777619u); actualSeed = PlacementSeed.AddByte(actualSeed, StableBytes[b]); }
                    var center = new int2(i - 2000, -i);
                    expectedSeed = math.max(1u, math.hash(new uint3(unchecked((uint)center.x), unchecked((uint)center.y), expectedSeed)));
                    if (expectedSeed != PlacementSeed.Finish(actualSeed, center.x, center.y)) errors++;
                }
                Errors[0] = errors;
            }
        }
        [Test] public void RandomAndSeedManagedAndBurst()
        {
            using var errors = new NativeArray<int>(1, Allocator.TempJob);
            using var managed = new NativeArray<int>(1, Allocator.TempJob);
            using var bytes = new NativeArray<byte>(System.Text.Encoding.UTF8.GetBytes("actor/é/東京"), Allocator.TempJob);
            var job = new RandomJob { Errors = errors, Managed = managed, StableBytes = bytes };
            job.Execute(); Assert.AreEqual(1, managed[0]); Assert.AreEqual(0, errors[0]);
            job.Schedule().Complete(); Assert.AreEqual(0, managed[0], "Actual Burst required"); Assert.AreEqual(0, errors[0]);
            var id = new FixedString64Bytes("original.actor.é");
            uint seed = PlacementSeed.Begin(-879); for (int i = 0; i < id.Length; i++) seed = PlacementSeed.AddByte(seed, id[i]);
            var pure = SpawnCellRandom.FromSeed(PlacementSeed.Finish(seed, -12, 29));
            Assert.AreEqual(pure.State, Game.Runtime.SkirmishTypedSpawnConstraint.PlacementRandom(-879, id, new int2(-12, 29)).state);
            Assert.Throws<ArgumentException>(() => Game.Runtime.SkirmishTypedSpawnConstraint.PlacementRandom(0, default, default));
        }
        [BurstCompile(CompileSynchronously = true)] private struct SearchJob : IJob
        {
            public NativeArray<GridWalkable> Walkable;
            public NativeBitArray Blocked, Occupied, OriginalReserved, NativeReserved, PureReserved;
            public NativeArray<int> Errors, Managed;
            [BurstDiscard] private static void MarkManaged(ref int value) { value = 1; }
            public void Execute()
            {
                int flag = 0; MarkManaged(ref flag); Managed[0] = flag;
                var grid = new GridConfig { Width = 9, Height = 8, CellSize = 1 };
                for (int scenario = 0; scenario < Errors.Length; scenario++)
                {
                    for (int index = 0; index < 72; index++)
                    {
                        Walkable[index] = new GridWalkable { Value = (byte)(scenario % 16 == 0 ? 0 : (index * 17 + scenario * 11) % 7 == 0 ? 0 : 1) };
                        Blocked.Set(index, (index * 13 + scenario) % 17 == 0); Occupied.Set(index, (index + scenario * 3) % 19 == 0);
                        bool reserve = (index * 7 + scenario) % 23 == 0;
                        OriginalReserved.Set(index, reserve); NativeReserved.Set(index, reserve); PureReserved.Set(index, reserve);
                    }
                    var center = new int2(scenario % 15 - 3, scenario % 11 - 2);
                    var size = new int2(scenario % 6 - 1, (scenario / 5) % 5 - 1);
                    int radius = scenario % 7 - 2; bool air = scenario % 2 != 0;
                    uint seed = (uint)scenario + 1;
                    var originalRandom = new Unity.Mathematics.Random(seed); var nativeRandom = originalRandom;
                    var pureRandom = SpawnCellRandom.FromSeed(seed);
                    bool expected = OriginalInitial.TryFindInitialUnitSpawnCell(ref originalRandom, grid, Walkable, Blocked, Occupied, ref OriginalReserved, center, radius, size, air, out var expectedCell);
                    bool actual = InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref nativeRandom, grid, Walkable, Blocked, Occupied, ref NativeReserved, center, radius, size, air, out var actualCell);
                    var storage = new Storage { Walkable = Walkable, Blocked = Blocked, Occupied = Occupied, Reserved = PureReserved };
                    bool pure = SpawnCellSearchKernel.TryFindInitial(ref storage, ref pureRandom, grid.Width, grid.Height, center.x, center.y, radius, size.x, size.y, air, out int x, out int z);
                    int errors = expected != actual || expected != pure || math.any(expectedCell != actualCell) || expectedCell.x != x || expectedCell.y != z || originalRandom.state != nativeRandom.state || originalRandom.state != pureRandom.State ? 1 : 0;
                    for (int index = 0; index < 72; index++) if (OriginalReserved.IsSet(index) != NativeReserved.IsSet(index) || OriginalReserved.IsSet(index) != PureReserved.IsSet(index)) errors++;
                    Errors[scenario] = errors;
                }
            }
        }
        [Test] public void SearchMatchesOriginalManagedAndBurst()
        {
            using var walk = new NativeArray<GridWalkable>(72, Allocator.TempJob);
            using var blocked = new NativeBitArray(72, Allocator.TempJob); using var occupied = new NativeBitArray(72, Allocator.TempJob);
            using var a = new NativeBitArray(72, Allocator.TempJob); using var b = new NativeBitArray(72, Allocator.TempJob); using var c = new NativeBitArray(72, Allocator.TempJob);
            using var errors = new NativeArray<int>(384, Allocator.TempJob); using var managed = new NativeArray<int>(1, Allocator.TempJob);
            var job = new SearchJob { Walkable = walk, Blocked = blocked, Occupied = occupied, OriginalReserved = a, NativeReserved = b, PureReserved = c, Errors = errors, Managed = managed };
            job.Execute(); Assert.AreEqual(1, managed[0]); for (int i = 0; i < errors.Length; i++) Assert.AreEqual(0, errors[i], "Managed case " + i);
            job.Schedule().Complete(); Assert.AreEqual(0, managed[0], "Actual Burst required"); for (int i = 0; i < errors.Length; i++) Assert.AreEqual(0, errors[i], "Burst case " + i);
        }
        [Test] public void SearchBoundaryControls()
        {
            using var walkOwner = new NativeArray<GridWalkable>(80, Allocator.Temp); var walk = walkOwner;
            using var blocked = new NativeBitArray(80, Allocator.Temp); using var occupied = new NativeBitArray(80, Allocator.Temp);
            using var reservedOwner = new NativeBitArray(80, Allocator.Temp); var reserved = reservedOwner;
            var grid = new GridConfig { Width = 80, Height = 1, CellSize = 1 };
            walk[79] = new GridWalkable { Value = 1 }; var random = new Unity.Mathematics.Random(37); var expectedRandom = random;
            for (int i = 0; i < 256; i++) expectedRandom.NextInt(0, 1);
            Assert.IsTrue(InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref random, grid, walk, blocked, occupied, ref reserved, default, 0, new int2(1), false, out var cell));
            Assert.AreEqual(new int2(79, 0), cell, "Whole-grid fallback follows failed local rings"); Assert.AreEqual(expectedRandom.state, random.state); Assert.IsTrue(reserved.IsSet(79));
            Assert.IsFalse(InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref random, grid, walk, blocked, occupied, ref reserved, default, 0, new int2(1), false, out cell)); Assert.AreEqual(default(int2), cell);
            reserved.Clear(); walk[3] = new GridWalkable { Value = 1 }; blocked.Set(3, true); reserved.Set(3, true); uint before = random.state;
            Assert.IsTrue(InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref random, grid, walk, blocked, occupied, ref reserved, new int2(3, 0), 12, new int2(1), true, out cell));
            Assert.AreEqual(new int2(3, 0), cell); Assert.AreEqual(before, random.state, "Air blocked+reserved preference consumes no random values");
        }
        private struct NativeSurface : ISpawnSurfaceAdmission
        {
            public MapSurfaceComponent Surface; public GridConfig Grid; public bool Vehicle;
            public bool CanTraverseFootprint(int x, int z, int sizeX, int sizeZ)
                => new MapSurfaceTraversalValidation().CanTraverseFootprint(Surface, 1, Grid, new int2(x, z), new int2(sizeX, sizeZ), Vehicle);
        }
        [BurstCompile(CompileSynchronously = true)] private struct SurfaceJob : IJob
        {
            public Storage Storage; public NativeSurface Surface;
            public int SizeX, SizeZ, SurfaceCount, X, Z; public bool HasMatch;
            public NativeArray<int> Result;
            [BurstDiscard] private static void MarkManaged(ref int value) { value = 1; }
            public void Execute()
            {
                int flag = 0; MarkManaged(ref flag); Result[3] = flag;
                int x = X, z = Z;
                Result[0] = SpawnCellSearchKernel.EnsureTraversable(ref Storage, ref Surface, Surface.Grid.Width, Surface.Grid.Height, SizeX, SizeZ, HasMatch, SurfaceCount, ref x, ref z) ? 1 : 0;
                Result[1] = x; Result[2] = z;
            }
        }
        private static BlobAssetReference<MapSurfaceBlob> SurfaceBlob(int width, int height, int pattern)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<MapSurfaceBlob>(); root.Dimensions = new int2(width, height); root.CellSize = 1;
            var cells = builder.Allocate(ref root.Cells, width * height); var samples = builder.Allocate(ref root.Samples, width * height);
            builder.Allocate(ref root.Connections, 0); builder.Allocate(ref root.CompactSamples, 0);
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new MapSurfaceCell { FirstSurfaceIndex = i, SurfaceCount = 1 };
                bool admitted = pattern == 0 || pattern == 1 && i % 7 == 0 || pattern == 2 && i == 70;
                samples[i] = new MapSurfaceSample { Cell = new int2(i % width, i / width), SurfaceId = i, Normal = new float3(0, 1, 0), MovementMask = admitted ? MapSurfaceMovementMask.AllGroundUnits : MapSurfaceMovementMask.None, SlopeDegrees = pattern == 0 ? 35 : 0 };
            }
            return builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
        }
        [Test] public void SurfaceMatchesOriginalNativeAndBurst()
        {
            using var world = new World("SpawnSurfaceParity"); var em = world.EntityManager;
            var prefab = em.CreateEntity(typeof(UnitMovementBehavior)); var match = em.CreateEntity(typeof(SkirmishMatchState));
            using var walkOwner = new NativeArray<GridWalkable>(72, Allocator.TempJob); var walk = walkOwner;
            using var blocked = new NativeBitArray(72, Allocator.TempJob); using var occupied = new NativeBitArray(72, Allocator.TempJob);
            using var aOwner = new NativeBitArray(72, Allocator.TempJob); using var bOwner = new NativeBitArray(72, Allocator.TempJob); using var cOwner = new NativeBitArray(72, Allocator.TempJob);
            using var result = new NativeArray<int>(4, Allocator.TempJob);
            var grid = new GridConfig { Width = 9, Height = 8, CellSize = 1 };
            for (int scenario = 0; scenario < 36; scenario++)
            {
                int mode = scenario % 4; bool hasMatch = mode != 0; int count = mode == 1 ? 0 : mode == 2 ? 2 : 1;
                if (hasMatch && !em.HasComponent<SkirmishMatchState>(match)) em.AddComponent<SkirmishMatchState>(match);
                if (!hasMatch && em.HasComponent<SkirmishMatchState>(match)) em.RemoveComponent<SkirmishMatchState>(match);
                using var blob = SurfaceBlob(9, 8, scenario / 4 % 4);
                var component = new MapSurfaceComponent { SurfaceBlob = blob, HasSurfaceData = 1, CellSize = 1, Dimensions = new int2(9, 8) };
                Entity first = Entity.Null, second = Entity.Null;
                if (count > 0) { first = em.CreateEntity(typeof(MapSurfaceComponent)); em.SetComponentData(first, component); }
                if (count > 1) { second = em.CreateEntity(typeof(MapSurfaceComponent)); em.SetComponentData(second, component); }
                try
                {
                    bool vehicle = scenario % 3 == 0; em.SetComponentData(prefab, new UnitMovementBehavior { UsesVehicleMotion = (byte)(vehicle ? 1 : 0) });
                    var size = new int2(scenario % 3, scenario / 3 % 3); var start = new int2(4, 4);
                    var a = aOwner; var b = bOwner; var c = cOwner;
                    for (int i = 0; i < 72; i++)
                    {
                        walk[i] = new GridWalkable { Value = (byte)(i % 11 == 0 ? 0 : 1) }; blocked.Set(i, i % 17 == 0); occupied.Set(i, i % 19 == 0);
                        bool reserve = i == 40 || i == 41 || i % 23 == 0; a.Set(i, reserve); b.Set(i, reserve); c.Set(i, reserve);
                    }
                    var expected = start; var actual = start;
                    bool found = OriginalSkirmishSpawnPlacement.EnsureTraversable(em, grid, walk, blocked, occupied, ref a, prefab, size, ref expected);
                    bool native = SkirmishSpawnPlacement.EnsureTraversable(em, grid, walk, blocked, occupied, ref b, prefab, size, ref actual);
                    Assert.AreEqual(found, native, "native " + scenario); Assert.AreEqual(expected, actual);
                    var job = new SurfaceJob { Storage = new Storage { Walkable = walk, Blocked = blocked, Occupied = occupied, Reserved = c }, Surface = new NativeSurface { Surface = component, Grid = grid, Vehicle = vehicle }, SizeX = size.x, SizeZ = size.y, X = start.x, Z = start.y, HasMatch = hasMatch, SurfaceCount = count, Result = result };
                    job.Execute(); Assert.AreEqual(1, result[3]); Assert.AreEqual(found ? 1 : 0, result[0]); Assert.AreEqual(expected.x, result[1]); Assert.AreEqual(expected.y, result[2]);
                    for (int i = 0; i < 72; i++) { Assert.AreEqual(a.IsSet(i), b.IsSet(i), "native reservation " + scenario); Assert.AreEqual(a.IsSet(i), c.IsSet(i), "shared reservation " + scenario); c.Set(i, i == 40 || i == 41 || i % 23 == 0); }
                    job.Schedule().Complete(); Assert.AreEqual(0, result[3], "Actual Burst required"); Assert.AreEqual(found ? 1 : 0, result[0]); Assert.AreEqual(expected.x, result[1]); Assert.AreEqual(expected.y, result[2]);
                    for (int i = 0; i < 72; i++) Assert.AreEqual(a.IsSet(i), c.IsSet(i), "Burst reservation " + scenario);
                }
                finally { if (first != Entity.Null) em.DestroyEntity(first); if (second != Entity.Null) em.DestroyEntity(second); }
            }
        }
    }
}

namespace Game.Tests.Editor
{
    public static class OriginalSpawnCellUtility
    {
        public static int2 FindSpawnCellNear(
            ref Random rng,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int2 center,
            int radiusCells,
            int2 footprintSize)
        {
            return TryFindSpawnCellNear(ref rng, grid, walkable, blocked, occupied, ref reserved, center, radiusCells, footprintSize, out int2 cell)
                ? cell
                : center;
        }

        public static bool TryFindSpawnCellNear(
            ref Random rng,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int2 center,
            int radiusCells,
            int2 footprintSize,
            out int2 cell)
        {
            center.x = math.clamp(center.x, 0, grid.Width - 1);
            center.y = math.clamp(center.y, 0, grid.Height - 1);
            radiusCells = math.max(0, radiusCells);
            cell = default;

            if (TryReserveCell(grid, walkable, blocked, occupied, ref reserved, center.x, center.y, footprintSize, out int2 exactCell))
            {
                cell = exactCell;
                return true;
            }

            const int randomTries = 128;
            for (int i = 0; i < randomTries; i++)
            {
                int x = center.x + rng.NextInt(-radiusCells, radiusCells + 1);
                int y = center.y + rng.NextInt(-radiusCells, radiusCells + 1);
                if (TryReserveCell(grid, walkable, blocked, occupied, ref reserved, x, y, footprintSize, out int2 randomCell))
                {
                    cell = randomCell;
                    return true;
                }
            }

            int maxRadius = math.max(8, radiusCells + 32);
            for (int r = 0; r <= maxRadius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (math.abs(dx) != r && math.abs(dy) != r)
                            continue;

                        int x = center.x + dx;
                        int y = center.y + dy;
                        if (TryReserveCell(grid, walkable, blocked, occupied, ref reserved, x, y, footprintSize, out int2 ringCell))
                        {
                            cell = ringCell;
                            return true;
                        }
                    }
                }
            }

            return TryFindAnyFreeCell(grid, walkable, blocked, occupied, ref reserved, footprintSize, out cell);
        }

        public static int2 FindSpawnCellNear(
            ref Random rng,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int2 center,
            int radiusCells)
        {
            return FindSpawnCellNear(ref rng, grid, walkable, blocked, occupied, ref reserved, center, radiusCells, new int2(1, 1));
        }

        private static bool TryFindAnyFreeCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int2 footprintSize,
            out int2 cell)
        {
            cell = default;
            int gridSize = grid.Width * grid.Height;
            for (int idx = 0; idx < gridSize; idx++)
            {
                int2 candidate = GridUtils.IndexToCell(idx, grid.Width);
                if (!IsCellFree(grid, walkable, blocked, occupied, reserved, candidate.x, candidate.y, footprintSize))
                    continue;

                ReserveFootprint(grid, ref reserved, candidate, footprintSize);
                cell = candidate;
                return true;
            }

            return false;
        }

        private static bool TryReserveCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int x,
            int y,
            int2 footprintSize,
            out int2 cell)
        {
            cell = default;
            if (!IsCellFree(grid, walkable, blocked, occupied, reserved, x, y, footprintSize))
                return false;

            cell = new int2(x, y);
            ReserveFootprint(grid, ref reserved, cell, footprintSize);
            return true;
        }

        private static bool IsCellFree(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            in NativeBitArray reserved,
            int x,
            int y,
            int2 footprintSize)
        {
            int2 center = new int2(x, y);
            int2 size = UnitFootprintUtility.ClampSize(footprintSize);
            int2 min = UnitFootprintUtility.GetMinCell(center, size);
            int2 max = min + size;
            if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
                return false;

            for (int yy = min.y; yy < max.y; yy++)
            {
                int row = yy * grid.Width;
                for (int xx = min.x; xx < max.x; xx++)
                {
                    int idx = row + xx;
                    if (walkable[idx].Value == 0)
                        return false;
                    if (blocked.IsSet(idx) || occupied.IsSet(idx) || reserved.IsSet(idx))
                        return false;
                }
            }

            return true;
        }

        private static void ReserveFootprint(in GridConfig grid, ref NativeBitArray reserved, int2 center, int2 footprintSize)
        {
            int2 size = UnitFootprintUtility.ClampSize(footprintSize);
            int2 min = UnitFootprintUtility.GetMinCell(center, size);
            int2 max = min + size;
            for (int y = min.y; y < max.y; y++)
            {
                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                    reserved.Set(row + x, true);
            }
        }
    }
}

namespace Game.Tests.Editor { internal static class OriginalInitial {
        internal static bool TryFindInitialUnitSpawnCell(
            ref Unity.Mathematics.Random rng,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int2 center,
            int radiusCells,
            int2 footprintSize,
            bool isAirUnit,
            out int2 cell)
        {
            if (isAirUnit &&
                TryReserveInitialAirSpawnCell(grid, walkable, blocked, occupied, ref reserved, center, footprintSize, out cell))
            {
                return true;
            }

            return OriginalSpawnCellUtility.TryFindSpawnCellNear(
                ref rng,
                grid,
                walkable,
                blocked,
                occupied,
                ref reserved,
                center,
                radiusCells,
                footprintSize,
                out cell);
        }
        private static bool TryReserveInitialAirSpawnCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int2 center,
            int2 footprintSize,
            out int2 cell)
        {
            cell = default;
            int2 size = UnitFootprintUtility.ClampSize(footprintSize);
            int2 min = UnitFootprintUtility.GetMinCell(center, size);
            int2 max = min + size;
            if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
                return false;

            for (int y = min.y; y < max.y; y++)
            {
                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                {
                    int index = row + x;
                    if (walkable[index].Value == 0 || occupied.IsSet(index))
                        return false;
                    if (reserved.IsSet(index) && !blocked.IsSet(index))
                        return false;
                }
            }

            for (int y = min.y; y < max.y; y++)
            {
                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                    reserved.Set(row + x, true);
            }

            cell = center;
            return true;
        }
} }

namespace Game.Tests.Editor
{
    internal static class OriginalSkirmishSpawnPlacement
    {
        internal static bool EnsureTraversable(EntityManager em, in GridConfig grid,
            NativeArray<GridWalkable> walkable, NativeBitArray blocked, NativeBitArray occupied,
            ref NativeBitArray reserved, Entity prefab, int2 size, ref int2 cell)
        {
            using var match=em.CreateEntityQuery(typeof(SkirmishMatchState));
            if(match.IsEmptyIgnoreFilter)return true;
            using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
            if(surfaces.CalculateEntityCount()!=1)return false;
            var surface=surfaces.GetSingleton<MapSurfaceComponent>();
            bool vehicle=em.HasComponent<UnitMovementBehavior>(prefab)&&em.GetComponentData<UnitMovementBehavior>(prefab).UsesVehicleMotion!=0;
            var validation=new MapSurfaceTraversalValidation();
            if(validation.CanTraverseFootprint(surface,1,grid,cell,size,vehicle))return true;

            // The compatibility grid can call a slope or a baked rooftop "clear".
            // Release only this just-reserved candidate and search valid terrain.
            var minimum=UnitFootprintUtility.GetMinCell(cell,size);
            for(int y=0;y<size.y;y++)for(int x=0;x<size.x;x++)reserved.Set((minimum.y+y)*grid.Width+minimum.x+x,false);
            for(int radius=1;radius<=48;radius++)
                for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)
                {
                    if(math.abs(x)!=radius&&math.abs(z)!=radius)continue;
                    var candidate=cell+new int2(x,z);
                    if(!validation.CanTraverseFootprint(surface,1,grid,candidate,size,vehicle))continue;
                    minimum=UnitFootprintUtility.GetMinCell(candidate,size);
                    bool free=true;
                    for(int dy=0;dy<size.y&&free;dy++)for(int dx=0;dx<size.x;dx++)
                    {int index=(minimum.y+dy)*grid.Width+minimum.x+dx;if(walkable[index].Value==0||blocked.IsSet(index)||occupied.IsSet(index)||reserved.IsSet(index)){free=false;break;}}
                    if(!free)continue;
                    for(int dy=0;dy<size.y;dy++)for(int dx=0;dx<size.x;dx++)reserved.Set((minimum.y+dy)*grid.Width+minimum.x+dx,true);
                    cell=candidate;return true;
                }
            return false;
        }
    }
}
