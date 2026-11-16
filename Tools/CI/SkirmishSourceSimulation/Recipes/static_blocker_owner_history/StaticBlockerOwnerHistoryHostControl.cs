using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Invoked as the source host's alternate Main against either compiled owner.
// Each case constructs a fresh shim EntityManager and runs the actual compiled
// StaticGridBlockerUpdateSystem.OnUpdate once or through explicit transitions.
internal static class StaticBlockerOwnerHistoryHostControl
{
    private const int Width = 8, Height = 8;
    private sealed class Fixture
    {
        internal EntityManager Manager;
        internal Entity Grid;
        internal GridConfig Config;
        internal int Width, Height;
        internal StaticGridBlockerUpdateSystem Owner;
        internal SystemState State;
        internal readonly List<Entity> Entities = new List<Entity>();
    }

    private static void Need(bool value, string message)
    { if (!value) throw new InvalidOperationException("Compiled owner differential fixture: " + message); }

    private static Fixture Create(int width = Width, int height = Height)
    {
        int next = 1;
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var config = new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
        Entity grid = em.CreateEntity();
        em.AddComponentData(grid, config);
        em.AddComponentData(grid, default(DynamicBlockerComponent));
        em.AddComponentData(grid, default(DynamicOccupancyComponent));
        em.AddComponentData(grid, default(PathPoolComponent));
        GeneratedFreshGridStorage.Initialize(em, grid);
        var storage = em.GetComponentData<DynamicBlockerComponent>(grid);
        storage.AppliedGrid = config;
        storage.AppliedStorageEpoch = storage.StorageEpoch;
        storage.AppliedRevision = 1;
        storage.WriteInProgress = 0;
        em.SetComponentData(grid, storage);
        var state = new SystemState { EntityManager = em, Enabled = true };
        var owner = new StaticGridBlockerUpdateSystem();
        state.Begin(); owner.OnCreate(ref state); state.End();
        return new Fixture { Manager = em, Grid = grid, Config = config, Width = width, Height = height,
            Owner = owner, State = state };
    }

    private static Entity AddRow(Fixture f, bool isStatic, bool unit, bool size,
        int x, int z, int width, int height, Entity appliedGrid, ulong appliedEpoch,
        bool previous = true, byte faction = byte.MaxValue)
    {
        Entity e = f.Manager.CreateEntity();
        if (isStatic) f.Manager.AddComponentData(e, new StaticGridBlocker());
        if (unit) f.Manager.AddComponentData(e, new UnitGrid { Cell = new int2(x, z) });
        if (size) f.Manager.AddComponentData(e, new GridBlockerSize { Size = new int2(width, height) });
        if (faction != byte.MaxValue) f.Manager.AddComponentData(e, new FriendlyPassGridBlocker { AllowedFactionId = faction });
        if (previous)
        {
            var rect = StaticBlockerKernel.Bounds(x, z, width, height);
            f.Manager.AddComponentData(e, new StaticBlockerPrevBounds {
                Min = new int2(rect.MinX, rect.MinZ), Max = new int2(rect.MaxX, rect.MaxZ),
                FriendlyPassFactionId = faction, AppliedGrid = appliedGrid, StorageEpoch = appliedEpoch
            });
        }
        f.Entities.Add(e);
        return e;
    }

    private static Entity MismatchGrid(Entity grid)
    { return new Entity { Index = grid.Index, Version = grid.Version + 1 }; }

    private static void SeedCell(Fixture f, int x, int z, int count, byte friendly = byte.MaxValue)
    {
        var data = f.Manager.GetComponentData<DynamicBlockerComponent>(f.Grid);
        int index = z * f.Width + x;
        data.Counts[index] = count;
        data.Blocked.Set(index, count > 0);
        data.FriendlyPassFactionIds[index] = friendly;
        f.Manager.SetComponentData(f.Grid, data);
    }

    private static JObject Snapshot(Fixture f, string label)
    {
        var storage = f.Manager.GetComponentData<DynamicBlockerComponent>(f.Grid);
        var counts = new JArray(); var blocked = new JArray(); var friendly = new JArray();
        for (int i = 0; i < f.Width * f.Height; i++)
        { counts.Add(storage.Counts[i]); blocked.Add(storage.Blocked.IsSet(i)); friendly.Add(storage.FriendlyPassFactionIds[i]); }
        var rows = new JArray();
        foreach (var e in f.Entities)
        {
            var row = new JObject { ["entity"] = e.Index,
                ["static"] = f.Manager.HasComponent<StaticGridBlocker>(e),
                ["unit"] = f.Manager.HasComponent<UnitGrid>(e),
                ["size"] = f.Manager.HasComponent<GridBlockerSize>(e),
                ["history"] = f.Manager.HasComponent<StaticBlockerPrevBounds>(e) };
            if (f.Manager.HasComponent<StaticBlockerPrevBounds>(e))
            {
                var h = f.Manager.GetComponentData<StaticBlockerPrevBounds>(e);
                row["applied_index"] = h.AppliedGrid.Index; row["applied_version"] = h.AppliedGrid.Version;
                row["applied_epoch"] = (long)h.StorageEpoch;
                row["rect"] = new JArray(h.Min.x, h.Min.y, h.Max.x, h.Max.y);
                row["friendly"] = h.FriendlyPassFactionId;
            }
            rows.Add(row);
        }
        return new JObject { ["case"] = label, ["storage_epoch"] = (long)storage.StorageEpoch,
            ["applied_epoch"] = (long)storage.AppliedStorageEpoch, ["revision"] = (long)storage.AppliedRevision,
            ["write_in_progress"] = storage.WriteInProgress, ["counts"] = counts,
            ["blocked"] = blocked, ["friendly_pass"] = friendly, ["rows"] = rows };
    }

    private static JObject Update(Fixture f, string label)
    {
        f.State.Begin();
        try { f.Owner.OnUpdate(ref f.State); }
        finally { f.State.End(); }
        return Snapshot(f, label);
    }

    private static JObject MatchingDomainsAndExcludedIncompleteRows()
    {
        var f = Create(); ulong epoch = f.Manager.GetComponentData<DynamicBlockerComponent>(f.Grid).StorageEpoch;
        var live = AddRow(f, true, true, true, 4, 2, 1, 1, f.Grid, epoch);
        f.Manager.SetComponentData(live, new UnitGrid { Cell = new int2(5, 2) });
        SeedCell(f, 4, 2, 1);
        AddRow(f, false, false, false, 1, 1, 1, 1, f.Grid, epoch); // old removed domain
        SeedCell(f, 1, 1, 1);
        AddRow(f, true, true, false, 2, 4, 1, 1, MismatchGrid(f.Grid), 77); // excluded
        AddRow(f, true, false, true, 3, 4, 1, 1, MismatchGrid(f.Grid), 78); // excluded
        var result = Update(f, "matching-domains-incomplete-static-excluded");
        Need((long)result["storage_epoch"] == (long)epoch,
            "incomplete static history unexpectedly triggered a rebuild");
        return result;
    }

    private static JObject Mismatch(bool staticDomain)
    {
        var f = Create(); ulong epoch = f.Manager.GetComponentData<DynamicBlockerComponent>(f.Grid).StorageEpoch;
        var e = AddRow(f, staticDomain, staticDomain, staticDomain, 2, 3, 1, 1,
            MismatchGrid(f.Grid), epoch);
        SeedCell(f, 2, 3, 1);
        var result = Update(f, staticDomain ? "domain-A-entity-version-mismatch" : "domain-B-entity-version-mismatch");
        Need((long)result["storage_epoch"] == (long)(epoch + 1), "scanned domain mismatch failed to force rebuild");
        return result;
    }

    private static JObject StructuralTransitions()
    {
        var f = Create(); ulong epoch = f.Manager.GetComponentData<DynamicBlockerComponent>(f.Grid).StorageEpoch;
        Entity e = AddRow(f, true, false, false, 2, 2, 1, 1, MismatchGrid(f.Grid), 90);
        var incomplete = Update(f, "incomplete-static-before-filter-add");
        Need((long)incomplete["storage_epoch"] == (long)epoch, "incomplete row was not excluded");
        f.Manager.AddComponentData(e, new UnitGrid { Cell = new int2(2, 2) });
        f.Manager.AddComponentData(e, new GridBlockerSize { Size = new int2(1, 1) });
        var enteredA = Update(f, "incomplete-static-enter-domain-A");
        Need((long)enteredA["storage_epoch"] == (long)(epoch + 1), "filter addition did not join domain A");
        var history = f.Manager.GetComponentData<StaticBlockerPrevBounds>(e);
        history.AppliedGrid = MismatchGrid(f.Grid); history.StorageEpoch = 99;
        f.Manager.SetComponentData(e, history);
        f.Manager.RemoveComponent<StaticGridBlocker>(e);
        var enteredB = Update(f, "static-remove-enters-domain-B");
        Need((long)enteredB["storage_epoch"] == (long)(epoch + 2), "static removal did not join domain B");
        return new JObject { ["transitions"] = new JArray(incomplete, enteredA, enteredB) };
    }

    private static JObject OverlappingFriendlyWrites()
    {
        var f = Create();
        AddRow(f, true, true, true, 1, 1, 2, 1, f.Grid, 1, previous:false, faction:3);
        AddRow(f, true, true, true, 2, 1, 2, 1, f.Grid, 1, previous:false, faction:6);
        var result = Update(f, "overlapping-new-friendly-pass-order");
        Need((int)result["counts"][9] == 1 && (int)result["counts"][10] == 2 &&
             (int)result["counts"][11] == 1 && (int)result["friendly_pass"][10] == 6,
             "new blocker overlap or authored friendly-pass order differed");
        return result;
    }

    private const int SteadyBlockerRows = 4972;

    private static Fixture CreateSteadyFixture(bool forceRebuild)
    {
        var f = Create(2048, 1024);
        ulong epoch = f.Manager.GetComponentData<DynamicBlockerComponent>(f.Grid).StorageEpoch;
        var storage = f.Manager.GetComponentData<DynamicBlockerComponent>(f.Grid);
        for (int i = 0; i < SteadyBlockerRows; i++)
        {
            int x = (i * 37) % f.Width, z = i / f.Width;
            Entity applied = forceRebuild && i == 0 ? MismatchGrid(f.Grid) : f.Grid;
            AddRow(f, true, true, true, x, z, 1, 1, applied, epoch);
            int index = z * f.Width + x;
            storage.Counts[index] = 1;
            storage.Blocked.Set(index, true);
            storage.FriendlyPassFactionIds[index] = byte.MaxValue;
        }
        f.Manager.SetComponentData(f.Grid, storage);
        return f;
    }

    private static string PayloadHash(Fixture f)
    {
        var storage = f.Manager.GetComponentData<DynamicBlockerComponent>(f.Grid);
        var bytes = new byte[f.Width * f.Height * 6]; int p = 0;
        for (int i = 0; i < f.Width * f.Height; i++)
        {
            int count = storage.Counts[i];
            bytes[p++] = (byte)count; bytes[p++] = (byte)(count >> 8);
            bytes[p++] = (byte)(count >> 16); bytes[p++] = (byte)(count >> 24);
            bytes[p++] = storage.Blocked.IsSet(i) ? (byte)1 : (byte)0;
            bytes[p++] = storage.FriendlyPassFactionIds[i];
        }
        using (var hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    private static string HistoryHash(Fixture f)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            foreach (var e in f.Entities)
            {
                bool hasStatic = f.Manager.HasComponent<StaticGridBlocker>(e);
                bool hasUnit = f.Manager.HasComponent<UnitGrid>(e);
                bool hasSize = f.Manager.HasComponent<GridBlockerSize>(e);
                bool hasHistory = f.Manager.HasComponent<StaticBlockerPrevBounds>(e);
                writer.Write(e.Index); writer.Write(hasStatic); writer.Write(hasUnit); writer.Write(hasSize); writer.Write(hasHistory);
                if (hasUnit) { var unit = f.Manager.GetComponentData<UnitGrid>(e); writer.Write(unit.Cell.x); writer.Write(unit.Cell.y); }
                if (hasSize) { var size = f.Manager.GetComponentData<GridBlockerSize>(e); writer.Write(size.Size.x); writer.Write(size.Size.y); }
                if (hasHistory)
                {
                    var h = f.Manager.GetComponentData<StaticBlockerPrevBounds>(e);
                    writer.Write(h.Min.x); writer.Write(h.Min.y); writer.Write(h.Max.x); writer.Write(h.Max.y);
                    writer.Write(h.FriendlyPassFactionId); writer.Write(h.AppliedGrid.Index);
                    writer.Write(h.AppliedGrid.Version); writer.Write(h.StorageEpoch);
                }
            }
            writer.Flush();
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }
    }

    private static JObject CompactState(Fixture f)
    {
        var owner = f.Manager.GetComponentData<DynamicBlockerComponent>(f.Grid);
        int countRows = 0, staticRows = 0, completeStaticRows = 0, removedRows = 0, incompleteStaticRows = 0;
        foreach (var e in f.Entities)
        {
            countRows++;
            bool isStatic = f.Manager.HasComponent<StaticGridBlocker>(e);
            if (isStatic)
            {
                staticRows++;
                if (f.Manager.HasComponent<UnitGrid>(e) && f.Manager.HasComponent<GridBlockerSize>(e)) completeStaticRows++;
                else incompleteStaticRows++;
            }
            else if (f.Manager.HasComponent<StaticBlockerPrevBounds>(e)) removedRows++;
        }
        return new JObject { ["width"] = f.Width, ["height"] = f.Height, ["history_rows"] = countRows,
            ["static_rows"] = staticRows, ["complete_static_rows"] = completeStaticRows,
            ["incomplete_static_rows"] = incompleteStaticRows, ["removed_domain_rows"] = removedRows,
            ["storage_epoch"] = (long)owner.StorageEpoch, ["applied_epoch"] = (long)owner.AppliedStorageEpoch,
            ["revision"] = (long)owner.AppliedRevision, ["write_in_progress"] = owner.WriteInProgress,
            ["payload_sha256"] = PayloadHash(f), ["history_sha256"] = HistoryHash(f) };
    }

    private static JObject TimedSteadyPass(bool forceRebuild)
    {
        var f = CreateSteadyFixture(forceRebuild);
        var before = CompactState(f);
        var timer = new Stopwatch();
        f.State.Begin();
        timer.Start();
        try { f.Owner.OnUpdate(ref f.State); }
        finally { timer.Stop(); f.State.End(); }
        var after = CompactState(f);
        var beforeOwnerEpoch = (long)before["storage_epoch"];
        if (!forceRebuild)
        {
            Need((long)after["storage_epoch"] == beforeOwnerEpoch, "warm steady state unexpectedly rebuilt");
            Need((string)after["payload_sha256"] == (string)before["payload_sha256"] &&
                 (string)after["history_sha256"] == (string)before["history_sha256"],
                 "unchanged warm pass mutated storage or blocker histories");
        }
        else
        {
            Need((long)after["storage_epoch"] == beforeOwnerEpoch + 1, "forced mismatch failed to rebuild");
            Need((long)after["applied_epoch"] == (long)after["storage_epoch"] &&
                 (long)after["write_in_progress"] == 0, "forced rebuild failed to commit owner epoch");
            Need((string)after["payload_sha256"] == (string)before["payload_sha256"],
                 "forced rebuild changed the exact blocked/count/friendly storage payload");
        }
        return new JObject { ["kind"] = forceRebuild ? "forced_rebuild" : "warm_steady_state",
            ["on_update_elapsed_ticks"] = timer.ElapsedTicks,
            ["on_update_elapsed_ms"] = timer.Elapsed.TotalMilliseconds,
            ["before"] = before, ["after"] = after,
            ["full_update_measured"] = true, ["setup_and_state_hashing_included"] = false };
    }

    internal static int Main(string[] args)
    {
        var cases = new JArray {
            MatchingDomainsAndExcludedIncompleteRows(), Mismatch(true), Mismatch(false),
            StructuralTransitions(), OverlappingFriendlyWrites()
        };
        var result = new JObject { ["status"] = "Passed", ["case_count"] = 5,
            ["source_host_world_fixture"] = true, ["mission_run"] = false, ["cases"] = cases,
            ["steady_blocker_rows"] = SteadyBlockerRows,
            ["steady_cases"] = new JArray(TimedSteadyPass(false), TimedSteadyPass(true)) };
        Console.WriteLine("[StaticBlockerOwnerHistoryHostControl] " + result.ToString(Newtonsoft.Json.Formatting.None));
        return 0;
    }
}
