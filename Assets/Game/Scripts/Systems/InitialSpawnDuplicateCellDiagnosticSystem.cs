using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public readonly struct InitialSpawnDuplicateCellDiagnosticSystem
{
    public void LogInitialSpawnCellDuplicates(
        ref SystemState state,
        in GridConfig grid,
        ref InitialSpawnDiagnosticLogSystem diagnosticLogSystem)
    {
        using var query = state.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        using var occupiedCells = new NativeHashSet<int>(math.max(1024, entities.Length * 32), Allocator.Temp);
        using var centers = new NativeHashSet<int>(math.max(1, entities.Length), Allocator.Temp);
        EntityManager em = state.EntityManager;
        int duplicateCells = 0;
        int duplicateCenters = 0;
        int occupiedFootprintCells = 0;
        int countedEntities = 0;
        string samples = string.Empty;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity unit = entities[i];
            if (!em.Exists(unit) || em.HasComponent<StaticGridBlocker>(unit))
                continue;

            countedEntities++;
            int2 cell = em.GetComponentData<UnitGrid>(unit).Cell;
            int2 size = em.GetComponentData<UnitFootprint>(unit).Size;
            int centerKey = (uint)cell.x < (uint)grid.Width && (uint)cell.y < (uint)grid.Height
                ? cell.y * grid.Width + cell.x
                : int.MinValue + countedEntities;
            if (!centers.Add(centerKey))
            {
                duplicateCenters++;
                if (samples.Length < 430)
                    samples += $" center={cell}";
            }

            int2 min = UnitFootprintUtility.GetMinCell(cell, UnitFootprintUtility.ClampSize(size));
            int2 max = min + UnitFootprintUtility.ClampSize(size);
            for (int y = min.y; y < max.y; y++)
            {
                if (y < 0 || y >= grid.Height)
                    continue;

                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                {
                    if (x < 0 || x >= grid.Width)
                        continue;

                    occupiedFootprintCells++;
                    if (!occupiedCells.Add(row + x))
                    {
                        duplicateCells++;
                        if (samples.Length < 430)
                            samples += $" footprint={new int2(x, y)}";
                    }
                }
            }
        }

        diagnosticLogSystem.EnqueueLog(em, $"[InitialSpawnDiag] entities={countedEntities} occupiedCells={occupiedFootprintCells} duplicateCenters={duplicateCenters} duplicateFootprintCells={duplicateCells} samples={samples}");
    }
}
