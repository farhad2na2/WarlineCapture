using Unity.Entities;

public partial struct MapSurfaceDiagnosticsSystem : ISystem
{
    private const double DiagnosticsIntervalSeconds = 1d;
    private double _nextDiagnosticsTime;

    public void OnUpdate(ref SystemState state)
    {
        double now = SystemAPI.Time.ElapsedTime;
        if (now < _nextDiagnosticsTime)
            return;

        _nextDiagnosticsTime = now + DiagnosticsIntervalSeconds;
        if (!SystemAPI.TryGetSingletonEntity<MapSurfaceComponent>(out Entity surfaceEntity))
            return;

        MapSurfaceComponent surface = state.EntityManager.GetComponentData<MapSurfaceComponent>(surfaceEntity);
        MapSurfaceDiagnosticsComponent diagnostics = BuildDiagnostics(surface);
        if (state.EntityManager.HasComponent<MapSurfaceDiagnosticsComponent>(surfaceEntity))
            state.EntityManager.SetComponentData(surfaceEntity, diagnostics);
        else
            state.EntityManager.AddComponentData(surfaceEntity, diagnostics);
    }

    internal static MapSurfaceDiagnosticsComponent BuildDiagnostics(MapSurfaceComponent surface)
    {
        MapSurfaceDiagnosticsComponent diagnostics = default;
        diagnostics.HasSurfaceData = surface.HasSurfaceData;
        if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
            return diagnostics;

        ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
        diagnostics.CellCount = blob.Cells.Length;
        diagnostics.SurfaceCount = blob.Samples.Length;
        diagnostics.ConnectionCount = blob.Connections.Length;

        for (int i = 0; i < blob.Cells.Length; i++)
        {
            if (blob.Cells[i].SurfaceCount > 1)
                diagnostics.LayeredCellCount++;
        }

        for (int i = 0; i < blob.Samples.Length; i++)
        {
            MapSurfaceSample sample = blob.Samples[i];
            if ((sample.Flags & MapSurfaceFlags.Road) != 0)
                diagnostics.RoadSurfaceCount++;
            if ((sample.Flags & MapSurfaceFlags.Bridge) != 0)
                diagnostics.BridgeSurfaceCount++;
            if ((sample.Flags & MapSurfaceFlags.Ramp) != 0)
                diagnostics.RampSurfaceCount++;
            if (sample.SurfaceType == MapSurfaceType.Blocked ||
                sample.MovementMask == MapSurfaceMovementMask.None)
            {
                diagnostics.BlockedSurfaceCount++;
            }
        }

        return diagnostics;
    }
}
