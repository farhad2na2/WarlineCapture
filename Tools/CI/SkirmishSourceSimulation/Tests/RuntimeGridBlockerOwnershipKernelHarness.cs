using System;
using System.Collections.Generic;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;

internal static class RuntimeGridBlockerOwnershipKernelHarness
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static int Main()
    {
        var ownership = new RuntimeGridBlockerOwnershipKernel();
        ownership.InitializeGrid(8, 8);

        int first = ownership.AllocateId();
        int second = ownership.AllocateId();
        int third = ownership.AllocateId();
        ownership.Register(first, new Entity(1, 1), new int2(1, 1), new int2(3, 2));
        ownership.Register(second, new Entity(2, 1), new int2(3, 2), new int2(2, 2));
        ownership.Register(third, new Entity(3, 1), new int2(6, 6), new int2(1, 1));
        Need(ownership.TryGetOwnedEntity(new Entity(2, 1), out RuntimeGridBlockerOwnershipRecord secondRecord) &&
             secondRecord.Id == second && secondRecord.OriginCell.Equals(new int2(3, 2)) &&
             secondRecord.SizeCells.Equals(new int2(2, 2)),
            "Entity lookup must return the exact shared runtime blocker ownership record");
        Need(!ownership.TryGetOwnedEntity(new Entity(2, 2), out _),
            "Entity lookup must preserve versioned ECS identity");
        bool rejectedDuplicateEntity = false;
        try { ownership.Register(4, new Entity(2, 1), new int2(0, 0), new int2(1, 1)); }
        catch (InvalidOperationException) { rejectedDuplicateEntity = true; }
        Need(rejectedDuplicateEntity && ownership.Count == 3 &&
             ownership.TryGetOwnedEntity(new Entity(2, 1), out secondRecord) && secondRecord.Id == second,
            "An ECS entity cannot acquire a second blocker owner or alter existing membership");

        IReadOnlyList<int> overlaps = ownership.FindOverlappingIds(new int2(2, 2), new int2(2, 1));
        Need(overlaps.Count == 2 &&
             ((overlaps[0] == first && overlaps[1] == second) ||
              (overlaps[0] == second && overlaps[1] == first)),
            "Footprint removal query must return exactly the matching owner records");
        Need(ownership.IsRuntimeBlockerCell(3, 2, 8, 8) && ownership.IsRuntimeBlockerCell(6, 6, 8, 8),
            "Overlapping and separate runtime blocker cells must be counted");
        Need(!ownership.IsRuntimeBlockerCell(3, 2, 9, 8) && !ownership.IsRuntimeBlockerCell(-1, 2, 8, 8),
            "Cell membership must reject a mismatched grid and out-of-range coordinates");

        Need(ownership.TryRemove(first, true, out RuntimeGridBlockerOwnershipRecord removedFirst) &&
             removedFirst.Entity == new Entity(1, 1), "Removing a blocker returns its exact ECS membership record");
        Need(!ownership.IsRuntimeBlockerCell(1, 1, 8, 8) && ownership.IsRuntimeBlockerCell(3, 2, 8, 8),
            "Removing one blocker decrements its cells without clearing another overlapping blocker");
        Need(ownership.TryRemove(second, true, out _) && !ownership.IsRuntimeBlockerCell(3, 2, 8, 8) &&
             ownership.IsRuntimeBlockerCell(6, 6, 8, 8), "Final overlap removal clears only the owned cells");
        Need(!ownership.TryRemove(second, true, out _), "Removed blocker ids cannot be removed twice");
        Need(!ownership.TryGetOwnedEntity(new Entity(2, 1), out _),
            "Entity-to-owner index must be removed with the original ownership record");

        bool rejectedOutOfBounds = false;
        try { ownership.Register(99, new Entity(99, 1), new int2(7, 7), new int2(2, 2)); }
        catch (ArgumentOutOfRangeException) { rejectedOutOfBounds = true; }
        Need(rejectedOutOfBounds && ownership.Count == 1,
            "Out-of-bounds blocker footprints must fail before changing registry membership");

        ownership.Clear();
        Need(ownership.Count == 0 && !ownership.IsRuntimeBlockerCell(6, 6, 8, 8) &&
             !ownership.TryGetOwnedEntity(new Entity(3, 1), out _),
            "Disposal clears records and occupancy");
        ownership.InitializeGrid(8, 8);
        Need(ownership.AllocateId() == 4, "Clearing owner state must preserve monotonic blocker identity");

        Console.WriteLine("[RuntimeGridBlockerOwnershipKernelHarness] Passed overlap-counts=preserved removal=owner-scoped bounds=fail-closed id=monotonic");
        return 0;
    }
}
