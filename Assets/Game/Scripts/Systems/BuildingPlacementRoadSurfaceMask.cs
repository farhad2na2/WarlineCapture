using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    /// <summary>Projects authored road surfaces into the same placement mask as player-built roads.</summary>
    internal static class BuildingPlacementRoadSurfaceMask
    {
        internal static void Append(EntityManager em, Entity surfaceEntity, GridConfig grid, bool[] mask)
        {
            if (em.HasComponent<MapSurfaceComponent>(surfaceEntity))
            {
                var surface=em.GetComponentData<MapSurfaceComponent>(surfaceEntity);
                if(surface.HasSurfaceData!=0 && surface.SurfaceBlob.IsCreated)
                {
                    ref var blob=ref surface.SurfaceBlob.Value;
                    for(int y=0;y<grid.Height;y++) for(int x=0;x<grid.Width;x++)
                    {
                        var world=grid.Origin+new float3((x+.5f)*grid.CellSize,0,(y+.5f)*grid.CellSize);
                        int2 cell=(int2)math.floor((world.xz-surface.GridOrigin.xz)/surface.CellSize);
                        if(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob,cell,out var sample) && IsRoad(sample.SurfaceType,sample.Flags))
                            mask[y*grid.Width+x]=true;
                    }
                }
            }
            if(!em.HasBuffer<MapSurfaceSceneOverlay>(surfaceEntity)) return;
            var overlays=em.GetBuffer<MapSurfaceSceneOverlay>(surfaceEntity,true);
            var triangles=em.HasBuffer<MapSurfaceMeshTriangle>(surfaceEntity)
                ? em.GetBuffer<MapSurfaceMeshTriangle>(surfaceEntity,true).AsNativeArray() : default;
            var instances=em.HasBuffer<MapSurfaceMeshInstance>(surfaceEntity)
                ? em.GetBuffer<MapSurfaceMeshInstance>(surfaceEntity,true).AsNativeArray() : default;
            for(int i=0;i<overlays.Length;i++) AppendOverlay(overlays[i],triangles,instances,grid,mask);
        }

        internal static void AppendOverlay(in MapSurfaceSceneOverlay overlay,
            NativeArray<MapSurfaceMeshTriangle> triangles,NativeArray<MapSurfaceMeshInstance> instances,
            GridConfig grid,bool[] mask)
        {
            if(!IsRoad(overlay.SurfaceType,overlay.Flags)) return;
            if((overlay.Flags & MapSurfaceFlags.ExactMesh)!=0)
            {
                if(!instances.IsCreated || !triangles.IsCreated || (uint)overlay.GeometryInstanceIndex>=(uint)instances.Length) return;
                var instance=instances[overlay.GeometryInstanceIndex];
                var toWorld=math.inverse(instance.WorldToMesh);
                int end=math.min(triangles.Length,instance.FirstTriangle+instance.TriangleCount);
                for(int t=instance.FirstTriangle;t<end;t++)
                {
                    var triangle=triangles[t];
                    Rasterize(math.transform(toWorld,triangle.A).xz,math.transform(toWorld,triangle.B).xz,
                        math.transform(toWorld,triangle.C).xz,grid,mask);
                }
                return;
            }
            if((overlay.Flags & MapSurfaceFlags.ExactTriangle)!=0)
            {
                Rasterize(overlay.Center.xz+overlay.TriangleA,overlay.Center.xz+overlay.TriangleB,
                    overlay.Center.xz+overlay.TriangleC,grid,mask);
                return;
            }
            float2 u=math.mul(overlay.Rotation,new float3(overlay.HalfExtents.x,0,0)).xz;
            float2 v=math.mul(overlay.Rotation,new float3(0,0,overlay.HalfExtents.y)).xz;
            float2 c=overlay.Center.xz;
            Rasterize(c-u-v,c-u+v,c+u+v,grid,mask);
            Rasterize(c-u-v,c+u+v,c+u-v,grid,mask);
        }

        private static bool IsRoad(MapSurfaceType type,MapSurfaceFlags flags) =>
            (flags & MapSurfaceFlags.Road)!=0 || type is MapSurfaceType.Road or MapSurfaceType.DirtRoad or
                MapSurfaceType.Highway or MapSurfaceType.BridgeDeck or MapSurfaceType.Ramp;

        // Positive-area triangle/cell overlap also catches a road edge between cell centres.
        private static void Rasterize(float2 a,float2 b,float2 c,GridConfig grid,bool[] mask)
        {
            a=(a-grid.Origin.xz)/grid.CellSize; b=(b-grid.Origin.xz)/grid.CellSize; c=(c-grid.Origin.xz)/grid.CellSize;
            if(math.abs(Cross(b-a,c-a))<.00001f) return;
            int2 min=math.max(0,(int2)math.floor(math.min(a,math.min(b,c))));
            int2 max=math.min(new int2(grid.Width,grid.Height),(int2)math.ceil(math.max(a,math.max(b,c))));
            for(int y=min.y;y<max.y;y++) for(int x=min.x;x<max.x;x++)
            {
                int index=y*grid.Width+x;
                if(mask[index]) continue;
                float2 center=new(x+.5f,y+.5f);
                if(OverlapsAxis(a,b,c,center,b-a) && OverlapsAxis(a,b,c,center,c-b) && OverlapsAxis(a,b,c,center,a-c))
                    mask[index]=true;
            }
        }
        private static bool OverlapsAxis(float2 a,float2 b,float2 c,float2 center,float2 edge)
        {
            float2 axis=new(-edge.y,edge.x);
            float lo=math.min(math.dot(a-center,axis),math.min(math.dot(b-center,axis),math.dot(c-center,axis)));
            float hi=math.max(math.dot(a-center,axis),math.max(math.dot(b-center,axis),math.dot(c-center,axis)));
            float radius=.5f*(math.abs(axis.x)+math.abs(axis.y));
            return lo<radius-.00001f && hi>-radius+.00001f;
        }
        private static float Cross(float2 a,float2 b)=>a.x*b.y-a.y*b.x;
    }
}
