using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Components;
using Game.Runtime.Navigation;
using Game.Runtime.Spawning;

// Real allocator transaction replay. Native results are assertions only; the
// initial census and subsequent derived placements construct every reservation.
static class StartupPlacementProgram
{
    static void Need(bool ok, string message) { if (!ok) throw new ArgumentException(message); }
    static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    static long work;
    static void Charge(long amount = 1) { work += amount; Need(amount >= 0 && work <= 512L * 1024 * 1024, "Replay inspection budget exceeded"); }
    sealed class Artifacts
    {
        readonly string root;
        readonly JsonElement table;
        readonly Dictionary<string, byte[]> cache = new();
        long loaded;
        internal Artifacts(string file, JsonElement values) { root = Path.GetDirectoryName(Path.GetFullPath(file)); table = values; }
        internal byte[] Read(string name, int count)
        {
            Need(!string.IsNullOrWhiteSpace(name) && Path.GetFileName(name) == name && name != "." && name != "..", "Artifact escape");
            if (cache.TryGetValue(name, out var old)) { Need(old.Length == count, "Cached artifact shape changed"); return old; }
            Need(table.TryGetProperty(name, out var entry), "Missing actual artifact " + name);
            Need(entry.GetProperty("encoding").GetString() == "u8" && entry.GetProperty("bytes").GetInt64() == count, "Artifact shape/encoding differs");
            string path = Path.Combine(root, name);
            for (var item = new FileInfo(path) as FileSystemInfo; item != null; item = item is FileInfo f ? f.Directory : ((DirectoryInfo)item).Parent)
                Need((item.Attributes & FileAttributes.ReparsePoint) == 0, "Artifact symlink rejected");
            Need(new FileInfo(path).Length == count, "Artifact length differs");
            loaded += count; Need(loaded <= 512L * 1024 * 1024, "Artifact allocation budget exceeded");
            var bytes = File.ReadAllBytes(path);
            Need(Hash(bytes) == entry.GetProperty("sha256").GetString(), "Artifact bytes differ");
            Need(bytes.All(x => x <= 1), "Binary mask required");
            cache.Add(name, bytes); return bytes;
        }
    }
    struct Storage : ISpawnCellStorage
    {
        internal byte[] Walkable, Blocked, Occupied, Reserved;
        public bool IsWalkable(int i) { Charge(); return Walkable[i] != 0; }
        public bool IsBlocked(int i) { Charge(); return Blocked[i] != 0; }
        public bool IsOccupied(int i) { Charge(); return Occupied[i] != 0; }
        public bool IsReserved(int i) { Charge(); return Reserved[i] != 0; }
        public void SetReserved(int i, bool v) { Charge(); Reserved[i] = (byte)(v ? 1 : 0); }
    }
    struct Surface : ISpawnSurfaceAdmission
    {
        internal int Width, Height, Sx, Sz;
        internal byte[] Admission;
        public bool CanTraverseFootprint(int x, int z, int sx, int sz)
        {
            Charge(); Need(sx == Sx && sz == Sz, "Footprint profile mismatch");
            return x >= 0 && z >= 0 && x < Width && z < Height && Admission[x + z * Width] != 0;
        }
    }
    record Existing(string Identity, int X, int Z, int Sx, int Sz);
    static string Identity(JsonElement row) => row.GetProperty("entity_index").GetInt32() + ":" + row.GetProperty("entity_version").GetInt32();
    static (int X, int Z) Pair(JsonElement row) { Need(row.GetArrayLength() == 2, "Exact cell pair required"); return (row[0].GetInt32(), row[1].GetInt32()); }
    static Existing CensusRow(JsonElement row)
    {
        var cell = Pair(row.GetProperty("cell")); var size = Pair(row.GetProperty("raw_footprint"));
        Need(Math.Abs((long)cell.X) <= 1048576 && Math.Abs((long)cell.Z) <= 1048576 && size.X >= 1 && size.Z >= 1 && size.X <= 256 && size.Z <= 256, "Census outside bounded actual profile");
        return new Existing(Identity(row), cell.X, cell.Z, size.X, size.Z);
    }
    static (int W, int H, GridCoordinateFrame Frame) Grid(JsonElement row)
    {
        int width = row.GetProperty("Width").GetInt32(), height = row.GetProperty("Height").GetInt32();
        Need(width > 0 && height > 0 && (long)width * height <= 16 * 1024 * 1024, "Complete bounded grid required");
        var origin = row.GetProperty("Origin");
        var frame = new GridCoordinateFrame { OriginX = origin.GetProperty("x").GetSingle(), OriginY = origin.GetProperty("y").GetSingle(), OriginZ = origin.GetProperty("z").GetSingle(), CellSize = row.GetProperty("CellSize").GetSingle() };
        Need(float.IsFinite(frame.OriginX) && float.IsFinite(frame.OriginY) && float.IsFinite(frame.OriginZ) && float.IsFinite(frame.CellSize) && frame.CellSize > 0, "Finite actual grid required");
        return (width, height, frame);
    }
    static object Replay(string file)
    {
        Need(new FileInfo(file).Length <= 128L * 1024 * 1024, "Capture JSON exceeds128MiB");
        byte[] bytes = File.ReadAllBytes(file); Need(bytes.Length <= 128 * 1024 * 1024, "Capture JSON exceeds128MiB");
        using var document = JsonDocument.Parse(bytes); var root = document.RootElement; SpawnJson.Unique(root);
        Need(root.GetProperty("schema_version").GetInt32() == 1 && root.GetProperty("scope").GetString() == "ActualS002World_PostRoster_FrameBoundaryObservation", "Exact native startup capture required");
        var grid = Grid(root.GetProperty("grid")); int count = checked(grid.W * grid.H);
        var artifacts = new Artifacts(file, root.GetProperty("artifacts"));
        byte[] walk = artifacts.Read("walkable.u8", count);
        var profiles = new Dictionary<string, Surface>();
        foreach (var profile in root.GetProperty("footprint_surface_profiles").EnumerateArray())
        {
            int sx = profile.GetProperty("width").GetInt32(), sz = profile.GetProperty("height").GetInt32();
            Need(sx >= 1 && sz >= 1 && sx <= 64 && sz <= 64, "Positive bounded footprint profile required");
            bool vehicle = profile.GetProperty("vehicle").GetBoolean();
            string key = sx + "x" + sz + (vehicle ? "-vehicle" : "-infantry");
            Need(profile.GetProperty("key").GetString() == key && profile.GetProperty("policy").GetString() == "Native MapSurfaceTraversalValidation.CanTraverseFootprint", "Exact native footprint policy required");
            var mask = artifacts.Read(profile.GetProperty("artifact").GetString(), count); Charge(count);
            for (int z = 0; z < grid.H; z++) for (int x = 0; x < grid.W; x++) if (mask[x + z * grid.W] != 0)
            {
                int mx = x - (sx - 1) / 2, mz = z - (sz - 1) / 2;
                Need(mx >= 0 && mz >= 0 && mx + sx <= grid.W && mz + sz <= grid.H, "Surface admits out-of-grid footprint");
            }
            Need(profiles.TryAdd(key, new Surface { Width = grid.W, Height = grid.H, Sx = sx, Sz = sz, Admission = mask }), "Duplicate footprint profile");
        }
        var events = root.GetProperty("allocator_transactions").EnumerateArray().ToArray();
        int expected = root.GetProperty("expected_force_count").GetInt32(); Need(expected > 0 && expected <= 512 && events.Length == expected, "Complete actual allocator sequence required");
        var actorInputs = new StartupActorState.Inputs(root);
        Need(actorInputs.Forces.Count == expected, "Authored roster count differs from actual native allocator sequence");
        var derivedActorStates = new List<object>();
        var existing = new Dictionary<string, Existing>(); var stableIds = new HashSet<string>(); var results = new List<object>();
        var finalActors = root.GetProperty("actors").EnumerateArray().Where(a => !a.GetProperty("is_structure").GetBoolean()).ToDictionary(a => a.GetProperty("stable_id").GetString());
        Need(finalActors.Count == expected, "Exact post-roster force census required");
        var firstCensus = events[0].GetProperty("reservation_census_native_query_order");
        Need(firstCensus.GetArrayLength() <= 65536, "Bounded complete initial census required");
        foreach (var row in firstCensus.EnumerateArray())
        { var actor = CensusRow(row); Need(existing.TryAdd(actor.Identity, actor), "Duplicate initial census identity"); }
        int initialCensus = existing.Count;
        foreach (var row in events)
        {
            int ordinal = results.Count; Need(row.GetProperty("ordinal").GetInt32() == ordinal, "Native creation order differs");
            var g = Grid(row.GetProperty("grid")); Need(g.Equals(grid), "Allocator layout changed");
            string id = row.GetProperty("stable_id").GetString(); Need(!string.IsNullOrEmpty(id) && stableIds.Add(id), "Fresh stable actor ID required");
            Need(row.GetProperty("session_id").GetString() == root.GetProperty("session_id").GetString(), "Mixed sessions");
            Need(row.GetProperty("reservation_census_native_query_order").GetArrayLength() <= 65536, "Bounded complete census required");
            var census = row.GetProperty("reservation_census_native_query_order").EnumerateArray().Select(CensusRow).ToArray();
            Need(census.Length == existing.Count && census.Select(x => x.Identity).Distinct().Count() == census.Length && census.All(x => existing.TryGetValue(x.Identity, out var derived) && x == derived), "Native census differs from initial actors plus DERIVED placements at " + id);
            var reserved = new byte[count]; Charge(count);
            foreach (var actor in existing.Values)
            {
                var bounds = GridOccupancyKernel.Bounds(grid.W, grid.H, actor.X, actor.Z, actor.Sx, actor.Sz);
                for (int z = bounds.MinZ; z < bounds.MaxZ; z++) for (int x = bounds.MinX; x < bounds.MaxX; x++) { reserved[x + z * grid.W] = 1; Charge(); }
            }
            Need(Hash(reserved) == row.GetProperty("reserved_u8_sha256").GetString(), "Native reservation differs from derived census at " + id);
            var world = row.GetProperty("requested_world"); Need(world.GetArrayLength() == 3, "Complete requested world required");
            float wx = world[0].GetSingle(), wz = world[2].GetSingle(); Need(float.IsFinite(wx) && float.IsFinite(world[1].GetSingle()) && float.IsFinite(wz), "Finite requested world required");
            var center = GridCoordinateKernel.WorldToCell(grid.Frame, wx, wz); var capturedCenter = Pair(row.GetProperty("requested_cell")); Need(center.X == capturedCenter.X && center.Z == capturedCenter.Z, "Requested world/cell differs");
            var size = Pair(row.GetProperty("footprint")); Need(size.X >= 1 && size.Z >= 1 && size.X <= 64 && size.Z <= 64, "Actual positive raw footprint required");
            var before = row.GetProperty("pre_placement_actor"); var components = before.GetProperty("components");
            var prefabSize = components.GetProperty("UnitFootprint").GetProperty("Size"); Need(prefabSize.GetProperty("x").GetInt32() == size.X && prefabSize.GetProperty("y").GetInt32() == size.Z, "Actual prefab footprint differs");
            bool air = row.GetProperty("air").GetBoolean(); Need(!air && components.GetProperty("UnitAirMovement").ValueKind == JsonValueKind.Null, "This startup profile requires ground actors");
            bool vehicle = components.GetProperty("UnitMovementBehavior").GetProperty("UsesVehicleMotion").GetByte() != 0;
            Need(profiles.TryGetValue(size.X + "x" + size.Z + (vehicle ? "-vehicle" : "-infantry"), out var surface), "Actual footprint admission absent");
            int seed = row.GetProperty("seed").GetInt32(); Need(seed == root.GetProperty("seed").GetInt32(), "Mission seed changed");
            uint placementSeed = PlacementSeed.Begin(seed); foreach (byte b in Encoding.UTF8.GetBytes(id)) placementSeed = PlacementSeed.AddByte(placementSeed, b); placementSeed = PlacementSeed.Finish(placementSeed, center.X, center.Z);
            var random = SpawnCellRandom.FromSeed(placementSeed); Need(random.State == row.GetProperty("random_state_before_find").GetUInt32(), "Derived native RNG differs");
            Need(row.GetProperty("initial_search_radius").GetInt32() == 12 && row.GetProperty("surface_correction_radius").GetInt32() == 48, "Production search contract changed");
            var storage = new Storage { Walkable = walk, Blocked = artifacts.Read(row.GetProperty("blocked_artifact").GetString(), count), Occupied = artifacts.Read(row.GetProperty("effective_occupied_artifact").GetString(), count), Reserved = reserved };
            bool found = SpawnCellSearchKernel.TryFindInitial(ref storage, ref random, grid.W, grid.H, center.X, center.Z, 12, size.X, size.Z, false, out int px, out int pz);
            if (found) found = SpawnCellSearchKernel.EnsureTraversable(ref storage, ref surface, grid.W, grid.H, size.X, size.Z, true, 1, ref px, ref pz);
            var native = row.GetProperty("placement_result"); Need(found == native.GetProperty("success").GetBoolean() && found, "Actual placement failed or differs at " + id);
            Need(Identity(before) == Identity(native.GetProperty("actor")), "Native result actor identity differs at " + id);
            var nativeCell = native.GetProperty("actor").GetProperty("components").GetProperty("UnitGrid").GetProperty("Cell");
            Need(px == nativeCell.GetProperty("x").GetInt32() && pz == nativeCell.GetProperty("y").GetInt32(), "Derived placement differs from native result at " + id);
            Need(finalActors.TryGetValue(id, out var final), "Final actor absent");
            Need(Identity(before) == Identity(final), "Post-roster actor identity differs at " + id);
            var finalCell = final.GetProperty("components").GetProperty("UnitGrid").GetProperty("Cell");
            Need(px == finalCell.GetProperty("x").GetInt32() && pz == finalCell.GetProperty("y").GetInt32(), "Post-roster cell differs at " + id);
            var position = GridCoordinateKernel.CellToWorldCenter(grid.Frame, px, pz);
            derivedActorStates.Add(StartupActorState.Derive(actorInputs, ordinal, row, final, px, pz, position.X, position.Z));
            Need(existing.TryAdd(Identity(before), new Existing(Identity(before), px, pz, size.X, size.Z)), "New actor identity already existed");
            results.Add(new { id, ordinal, cell = new[] { px, pz }, footprint = new[] { size.X, size.Z }, vehicle, typed_occupancy_input = row.GetProperty("typed").GetBoolean(), derived_random_state = random.State });
        }
        return new { result = "PassedDerivedPlacementCellsAndSerializedActorDefaults", scope = "CapturedRequestsAndAllocatorFacts_DerivedReservationsPlacementAndSerializedActorDefaults", force_actors = results.Count, initial_census = initialCensus, grid_width = grid.W, grid_height = grid.H, inspected_work = work, actors = results, derived_actor_states = derivedActorStates, grounding_derived = false, typed_pad_policy_derived = false, full_mission_initialized = false, terminal_parity = false, player_ready = false, source_capture_sha256 = Hash(bytes) };
    }
    static int Main(string[] args)
    {
        try { Need(args.Length == 1, "One native capture path required"); Console.WriteLine(JsonSerializer.Serialize(Replay(args[0]))); return 0; }
        catch (Exception error) { Console.WriteLine(JsonSerializer.Serialize(new { result = "Failed", error = error.Message, full_mission_initialized = false, terminal_parity = false, player_ready = false })); return 1; }
    }
}
