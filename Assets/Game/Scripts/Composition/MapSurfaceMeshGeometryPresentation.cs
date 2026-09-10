using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Unity.Entities;

namespace Game.Composition
{
    public static class MapSurfaceMeshGeometryPresentation
    {
        public static void Publish(EntityManager em,Entity entity,MapSurfaceSceneOverlayAuthoringData[] authored)
        {
            if(!em.HasBuffer<MapSurfaceMeshTriangle>(entity))em.AddBuffer<MapSurfaceMeshTriangle>(entity);
            if(!em.HasBuffer<MapSurfaceMeshInstance>(entity))em.AddBuffer<MapSurfaceMeshInstance>(entity);
            var faces=em.GetBuffer<MapSurfaceMeshTriangle>(entity);faces.Clear();
            var instances=em.GetBuffer<MapSurfaceMeshInstance>(entity);instances.Clear();
            var ranges=new Dictionary<MapSurfaceMeshGeometryAsset,(int Start,int Count)>();
            for(int i=0;i<authored.Length;i++)
            {
                var source=authored[i];var geometry=source.Geometry;
                if(geometry==null){if((source.Flags&MapSurfaceFlags.ExactMesh)!=0)throw new System.InvalidOperationException("Missing baked road contact geometry.");instances.Add(default);continue;}
                if(!ranges.TryGetValue(geometry,out var range))
                {
                    range=(faces.Length,geometry.Triangles.Length);ranges.Add(geometry,range);
                    foreach(var triangle in geometry.Triangles)faces.Add(triangle);
                }
                instances.Add(new MapSurfaceMeshInstance{WorldToMesh=source.WorldToMesh,FirstTriangle=range.Start,TriangleCount=range.Count});
            }
        }
    }
}
