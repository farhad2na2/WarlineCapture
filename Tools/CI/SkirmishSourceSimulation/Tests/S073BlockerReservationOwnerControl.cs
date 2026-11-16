using System;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static class S073BlockerReservationOwnerControl
{
    const int Width = 8;
    const int Height = 8;

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static Entity AddBlocker(
        EntityManager em,
        int2 unitGridCell,
        int2 size,
        bool operationMapBuilding,
        bool hasRuntimeBuildingInfo,
        int2 origin)
    {
        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new StaticGridBlocker());
        em.AddComponentData(entity, new UnitGrid { Cell = unitGridCell });
        em.AddComponentData(entity, new GridBlockerSize { Size = size });
        if (operationMapBuilding)
            em.AddComponentData(entity, new OperationMapBuildingComponent());
        if (hasRuntimeBuildingInfo)
            em.AddComponentData(entity, new RuntimeBuildingCombatInfo
            {
                OriginCell = origin,
                FootprintCells = size
            });
        return entity;
    }

    static void MarkExpected(bool[] expected, int x, int y, int width, int height)
    {
        for (int row = y; row < y + height; row++)
            for (int column = x; column < x + width; column++)
                if ((uint)column < Width && (uint)row < Height)
                    expected[row * Width + column] = true;
    }

    static int Count(bool[] values)
    {
        int count = 0;
        foreach (bool value in values) if (value) count++;
        return count;
    }

    static void Main()
    {
        int nextId = 1;
        var em = new EntityManager
        {
            LogicalAllocatorAdmitted = true,
            AllocateLogicalIdentity = () => new Entity { Index = nextId++, Version = 1 }
        };
        var grid = new GridConfig { Width = Width, Height = Height, CellSize = 1f, Origin = float3.zero };

        // Operation-map authoring stores UnitGrid at origin + footprint/2.
        AddBlocker(em, new int2(2, 2), new int2(2, 2), true, true, new int2(1, 1));
        // Non-operation blockers retain the legacy UnitGrid-cell fallback.
        AddBlocker(em, new int2(4, 1), new int2(1, 2), false, false, default);
        // An operation marker without RuntimeBuildingCombatInfo also falls back.
        AddBlocker(em, new int2(6, 1), new int2(2, 2), true, false, default);
        // Actual authored origin/footprint overlaps the right and lower map edges.
        AddBlocker(em, new int2(8, 7), new int2(3, 3), true, true, new int2(7, 6));

        var reserved = new NativeBitArray(new bool[Width * Height]);
        InitialUnitsSpawnSystem.ReserveStaticBlockerFootprints(em, ref reserved, grid);

        var expected = new bool[Width * Height];
        MarkExpected(expected, 1, 1, 2, 2);
        MarkExpected(expected, 4, 1, 1, 2);
        MarkExpected(expected, 6, 1, 2, 2);
        MarkExpected(expected, 7, 6, 1, 2);
        for (int index = 0; index < expected.Length; index++)
            Require(reserved.IsSet(index) == expected[index],
                "Reservation mismatch at cell " + (index % Width) + "," + (index / Width));

        Require(!reserved.IsSet(3 * Width + 3), "Operation blocker used UnitGrid center instead of authored origin");
        Console.WriteLine("[S073BlockerReservationOwnerControl] result=Passed owner=InitialUnitsSpawnSystem operation_origin=Passed non_operation_fallback=Passed missing_info_fallback=Passed edge_clipping=Passed occupied_cells=" + Count(expected) + " scope=BoundedFixture_NotMissionReadiness");
    }
}
