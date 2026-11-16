extern alias ActualCore;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;
using Game.Rendering.SourceHost;

internal static class ReserveDropBufferCompileFixture
{
    private static int2 BindProductionWorldToCell(GridConfig grid, float3 world) =>
        GridUtils.WorldToCell(grid, world);

    public static int Main()
    {
        // The probe is compile-only: these references force the original method
        // bodies, provider contract, CellRect, and production GridUtils to bind.
        _ = typeof(ExtractedOriginalDropBufferReservation);
        _ = typeof(GridUtils);
        _ = (Func<GridConfig, float3, int2>)BindProductionWorldToCell;
        return 0;
    }
}
