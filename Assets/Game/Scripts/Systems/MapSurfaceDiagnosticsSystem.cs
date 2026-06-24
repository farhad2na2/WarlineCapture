using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MapSurfaceDiagnosticsSystem : ISystem
{
    private const double DiagnosticsIntervalSeconds = 1d;
    private ComponentLookup<MapSurfaceDiagnosticsComponent> _diagnosticsLookup;
    private double _nextDiagnosticsTime;
    private SurfaceDiagnosticsSignature _lastSignature;
    private bool _hasLastSignature;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _diagnosticsLookup = state.GetComponentLookup<MapSurfaceDiagnosticsComponent>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        double now = SystemAPI.Time.ElapsedTime;
        if (now < _nextDiagnosticsTime)
            return;

        _nextDiagnosticsTime = now + DiagnosticsIntervalSeconds;
        if (!SystemAPI.TryGetSingletonEntity<MapSurfaceComponent>(out Entity surfaceEntity))
            return;

        MapSurfaceComponent surface = SystemAPI.GetComponent<MapSurfaceComponent>(surfaceEntity);
        using NativeReference<SurfaceDiagnosticsSignature> signatureReference = new(Allocator.TempJob);
        using NativeReference<MapSurfaceDiagnosticsComponent> diagnosticsReference = new(Allocator.TempJob);
        using NativeReference<byte> changedReference = new(Allocator.TempJob);

        state.Dependency = new BuildDiagnosticsJob
        {
            Surface = surface,
            HasLastSignature = (byte)(_hasLastSignature ? 1 : 0),
            LastSignature = _lastSignature,
            Signature = signatureReference,
            Diagnostics = diagnosticsReference,
            Changed = changedReference
        }.Schedule(state.Dependency);
        state.Dependency.Complete();

        if (changedReference.Value == 0)
            return;

        SurfaceDiagnosticsSignature signature = signatureReference.Value;
        MapSurfaceDiagnosticsComponent diagnostics = diagnosticsReference.Value;
        _diagnosticsLookup.Update(ref state);
        if (_diagnosticsLookup.HasComponent(surfaceEntity))
            _diagnosticsLookup[surfaceEntity] = diagnostics;
        else
        {
            var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
            ecb.AddComponent(surfaceEntity, diagnostics);
        }

        _lastSignature = signature;
        _hasLastSignature = true;
    }

    [BurstCompile]
    private struct BuildDiagnosticsJob : IJob
    {
        public MapSurfaceComponent Surface;
        public byte HasLastSignature;
        public SurfaceDiagnosticsSignature LastSignature;
        public NativeReference<SurfaceDiagnosticsSignature> Signature;
        public NativeReference<MapSurfaceDiagnosticsComponent> Diagnostics;
        public NativeReference<byte> Changed;

        public void Execute()
        {
            SurfaceDiagnosticsSignature signature = BuildSignature(Surface);
            Signature.Value = signature;

            if (HasLastSignature != 0 && signature.Equals(LastSignature))
            {
                Changed.Value = 0;
                Diagnostics.Value = default;
                return;
            }

            Changed.Value = 1;
            Diagnostics.Value = BuildDiagnostics(Surface);
        }
    }

    private static SurfaceDiagnosticsSignature BuildSignature(MapSurfaceComponent surface)
    {
        SurfaceDiagnosticsSignature signature = new()
        {
            HasSurfaceData = surface.HasSurfaceData,
            HasLayeredCells = surface.HasLayeredCells,
            HasRoadSurfaces = surface.HasRoadSurfaces,
            HasBridgeSurfaces = surface.HasBridgeSurfaces,
            Width = surface.Dimensions.x,
            Height = surface.Dimensions.y,
            BlobHash = surface.SurfaceBlob.IsCreated ? surface.SurfaceBlob.GetHashCode() : 0
        };

        if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
            return signature;

        ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
        signature.CellCount = blob.Cells.Length;
        signature.SurfaceCount = blob.Samples.Length;
        signature.ConnectionCount = blob.Connections.Length;
        return signature;
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

    private struct SurfaceDiagnosticsSignature
    {
        public byte HasSurfaceData;
        public byte HasLayeredCells;
        public byte HasRoadSurfaces;
        public byte HasBridgeSurfaces;
        public int Width;
        public int Height;
        public int CellCount;
        public int SurfaceCount;
        public int ConnectionCount;
        public int BlobHash;

        public bool Equals(SurfaceDiagnosticsSignature other)
        {
            return HasSurfaceData == other.HasSurfaceData &&
                   HasLayeredCells == other.HasLayeredCells &&
                   HasRoadSurfaces == other.HasRoadSurfaces &&
                   HasBridgeSurfaces == other.HasBridgeSurfaces &&
                   Width == other.Width &&
                   Height == other.Height &&
                   CellCount == other.CellCount &&
                   SurfaceCount == other.SurfaceCount &&
                   ConnectionCount == other.ConnectionCount &&
                   BlobHash == other.BlobHash;
        }
    }
}
