using System;
using System.Collections.Generic;
using System.Linq;

// Source-only semantic control for the proposed owner-history query seam.
// This deliberately models query membership and preflight ordering; it is not
// an ECS enumeration or performance benchmark.
internal static class StaticBlockerOwnerHistoryDifferentialControl
{
    private struct GridId : IEquatable<GridId>
    {
        public int Index, Version;
        public bool Equals(GridId other) { return Index == other.Index && Version == other.Version; }
    }

    private sealed class Row
    {
        public int Id, Owner;
        public bool StaticBlocker, UnitGrid, Size;
        public GridId Applied;
        public int AppliedEpoch;
        public string Delta;
        public bool Opening;
    }

    private sealed class Outcome
    {
        public bool Rebuild;
        public readonly List<int> New = new List<int>(), Existing = new List<int>(), Removed = new List<int>();
        public readonly List<int> Opening = new List<int>();
        public string Storage;
    }

    private static bool InOldDomainA(Row r) { return r.StaticBlocker && r.UnitGrid && r.Size; }
    private static bool InOldDomainB(Row r) { return !r.StaticBlocker; }
    private static bool InUnion(Row r) { return !r.StaticBlocker || (r.UnitGrid && r.Size); }

    private static Outcome Run(List<Row> rows, bool combined, int owner, GridId grid, int epoch)
    {
        var result = new Outcome { Storage = "prior" };
        // Both paths finish all mismatch checks before any delta is applied.
        foreach (var row in rows)
        {
            bool selected = combined ? InUnion(row) : InOldDomainA(row) || InOldDomainB(row);
            if (selected && row.Owner == owner && (!row.Applied.Equals(grid) || row.AppliedEpoch != epoch))
                result.Rebuild = true;
        }
        if (result.Rebuild) result.Storage = "cleared-and-rebuilt";
        // Preserve authored phase order. New rows can overlap and order matters.
        foreach (var row in rows.Where(r => r.Owner == owner && r.Delta == "new")) result.New.Add(row.Id);
        foreach (var row in rows.Where(r => r.Owner == owner && r.Delta == "existing")) result.Existing.Add(row.Id);
        foreach (var row in rows.Where(r => r.Owner == owner && r.Delta == "removed")) result.Removed.Add(row.Id);
        foreach (var row in rows.Where(r => r.Owner == owner && r.Opening)) result.Opening.Add(row.Id);
        return result;
    }

    private static void Need(bool value, string message)
    { if (!value) throw new InvalidOperationException("Static blocker owner-history differential: " + message); }

    private static Row R(int id, bool blocker, bool unit, bool size, GridId grid, int epoch,
        int owner = 7, string delta = "", bool opening = false)
    { return new Row { Id = id, Owner = owner, StaticBlocker = blocker, UnitGrid = unit, Size = size,
        Applied = grid, AppliedEpoch = epoch, Delta = delta, Opening = opening }; }

    private static void Compare(List<Row> rows, int owner, GridId grid, int epoch, string label)
    {
        var oldPath = Run(rows, false, owner, grid, epoch);
        var candidate = Run(rows, true, owner, grid, epoch);
        Need(oldPath.Rebuild == candidate.Rebuild, label + ": rebuild mismatch");
        Need(oldPath.Storage == candidate.Storage, label + ": storage mismatch");
        Need(oldPath.New.SequenceEqual(candidate.New) && oldPath.Existing.SequenceEqual(candidate.Existing) &&
             oldPath.Removed.SequenceEqual(candidate.Removed) && oldPath.Opening.SequenceEqual(candidate.Opening),
             label + ": query-eligible phase/write order mismatch");
    }

    internal static int Main()
    {
        var grid = new GridId { Index = 4, Version = 2 };
        var foreignVersion = new GridId { Index = 4, Version = 3 };
        var otherGrid = new GridId { Index = 5, Version = 1 };
        var rows = new List<Row> {
            R(1, true, true, true, grid, 9, delta:"new", opening:true), // Domain A
            R(2, false, false, false, grid, 9, delta:"existing", opening:true), // Domain B
            R(3, true, false, true, foreignVersion, 1), // incomplete: skipped by both
            R(4, true, true, false, otherGrid, 0), // incomplete: skipped by both
            R(5, false, true, true, grid, 9, owner:88, delta:"removed"), // foreign owner
            R(6, true, true, true, grid, 9, delta:"new", opening:true), // overlapping friendly pass
            R(7, false, false, false, grid, 9, delta:"new", opening:true)
        };
        Compare(rows, 7, grid, 9, "matching histories and three membership cases");
        // Mismatch in either queried domain must force rebuild; entity version matters.
        rows[0].Applied = foreignVersion;
        Compare(rows, 7, grid, 9, "matching index but foreign entity version");
        Need(Run(rows, true, 7, grid, 9).Rebuild, "version mismatch did not rebuild");
        rows[0].Applied = grid; rows[1].AppliedEpoch = 8;
        Compare(rows, 7, grid, 9, "queried epoch mismatch");
        Need(Run(rows, true, 7, grid, 9).Rebuild, "epoch mismatch did not rebuild");
        rows[1].AppliedEpoch = 9; rows[0].AppliedEpoch = 8;
        Compare(rows, 7, grid, 9, "second queried-domain epoch mismatch");
        Need(Run(rows, true, 7, grid, 9).Rebuild, "domain A epoch mismatch did not rebuild");
        rows[0].AppliedEpoch = 9; rows[2].AppliedEpoch = 0;
        Compare(rows, 7, grid, 9, "mismatching incomplete-static history remains excluded");
        Need(!Run(rows, true, 7, grid, 9).Rebuild, "incomplete-static history incorrectly forced rebuild");
        // Structural transitions across each filter boundary retain exact domains.
        rows[1].StaticBlocker = true; rows[1].UnitGrid = false; // leaves B, remains excluded
        rows[1].AppliedEpoch = 9;
        Compare(rows, 7, grid, 9, "B to incomplete-static transition");
        rows[1].StaticBlocker = false; // re-enters B
        Compare(rows, 7, grid, 9, "incomplete-static to B transition");
        rows[1].StaticBlocker = true; rows[1].UnitGrid = true; rows[1].Size = true; // enters A
        Compare(rows, 7, grid, 9, "incomplete-static to A transition");
        Need(Run(rows, true, 7, grid, 9).New.SequenceEqual(new[] { 1, 6, 7 }) &&
             Run(rows, true, 7, grid, 9).Opening.SequenceEqual(new[] { 1, 2, 6, 7 }),
             "new/opening phase order changed for overlapping friendly blockers");
        Console.WriteLine("[StaticBlockerOwnerHistoryDifferentialControl] result=Passed cases=8 source_only=true speedup_claimed=false");
        return 0;
    }
}
