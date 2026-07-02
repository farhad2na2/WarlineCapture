using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct MapSurfaceDiagnosticsSystem : ISystem
    {
        private const double DiagnosticsIntervalSeconds = 1d;
        private EntityQuery _ecbSingletonQuery;
        private ComponentLookup<MapSurfaceDiagnosticsComponent> _diagnosticsLookup;
        private double _nextDiagnosticsTime;
        private SurfaceDiagnosticsSignature _lastSignature;
        private bool _hasLastSignature;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _ecbSingletonQuery = state.GetEntityQuery(ComponentType.ReadOnly<EndSimulationEntityCommandBufferSystem.Singleton>());
            _diagnosticsLookup = state.GetComponentLookup<MapSurfaceDiagnosticsComponent>();
            state.RequireForUpdate(_ecbSingletonQuery);
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
                Entity ecbEntity = _ecbSingletonQuery.GetSingletonEntity();
                var ecbSystem = state.EntityManager.GetComponentData<EndSimulationEntityCommandBufferSystem.Singleton>(ecbEntity);
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
            signature.CellCount = MapSurfaceBlobAccess.CellCount(ref blob);
            signature.SurfaceCount = MapSurfaceBlobAccess.SurfaceCount(ref blob);
            signature.ConnectionCount = MapSurfaceBlobAccess.ConnectionCount(ref blob);
            return signature;
        }

        internal static MapSurfaceDiagnosticsComponent BuildDiagnostics(MapSurfaceComponent surface)
        {
            MapSurfaceDiagnosticsComponent diagnostics = default;
            diagnostics.HasSurfaceData = surface.HasSurfaceData;
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
                return diagnostics;

            ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
            diagnostics.CellCount = MapSurfaceBlobAccess.CellCount(ref blob);
            diagnostics.SurfaceCount = MapSurfaceBlobAccess.SurfaceCount(ref blob);
            diagnostics.ConnectionCount = MapSurfaceBlobAccess.ConnectionCount(ref blob);

            diagnostics.LayeredCellCount = MapSurfaceBlobAccess.CountLayeredCells(ref blob);

            int surfaceCount = MapSurfaceBlobAccess.SurfaceCount(ref blob);
            for (int i = 0; i < surfaceCount; i++)
            {
                if (!MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref blob, i, out MapSurfaceSample sample))
                    continue;

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
}
