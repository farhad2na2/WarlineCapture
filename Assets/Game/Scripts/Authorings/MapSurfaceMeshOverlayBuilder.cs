using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Authoring
{
    /// <summary>Bakes each unique road mesh once; placements only store their transform and bounds.</summary>
    public static class MapSurfaceMeshOverlayBuilder
    {
        private static readonly Dictionary<(Mesh,int),MapSurfaceMeshGeometryAsset> cache=new();
        public static void ClearCachedGeometry()
        { foreach(var geometry in cache.Values)if(geometry!=null)Object.DestroyImmediate(geometry);cache.Clear(); }
        public static void Append(List<MapSurfaceSceneOverlayAuthoringData> output,Mesh mesh,Matrix4x4 matrix,
            MapSurfaceType type,MapSurfaceMovementMask mask,MapSurfaceFlags flags,int layer,int submesh=-1,Bounds? localBounds=null)
        {
            if(!cache.TryGetValue((mesh,submesh),out var geometry)||geometry==null)
            {
                var vertices=mesh.vertices;var indices=submesh<0?mesh.triangles:mesh.GetTriangles(submesh);
                var faces=new List<MapSurfaceMeshTriangle>();
                for(int i=0;i<indices.Length;i+=3)
                {
                    float3 a=vertices[indices[i]],b=vertices[indices[i+1]],c=vertices[indices[i+2]];
                    if(math.lengthsq(math.cross(b-a,c-a))>.00000001f)faces.Add(new MapSurfaceMeshTriangle{A=a,B=b,C=c});
                }
                geometry=ScriptableObject.CreateInstance<MapSurfaceMeshGeometryAsset>();geometry.name=mesh.name+"_GroundGeometry";geometry.hideFlags=HideFlags.DontUnloadUnusedAsset;
                geometry.Configure(mesh,submesh,faces.ToArray());cache[(mesh,submesh)]=geometry;
            }
            var local=localBounds??mesh.bounds;
            Vector3 center=matrix.MultiplyPoint3x4(local.center),e=local.extents;
            Vector3 ex=matrix.MultiplyVector(new Vector3(e.x,0,0)),ey=matrix.MultiplyVector(new Vector3(0,e.y,0)),ez=matrix.MultiplyVector(new Vector3(0,0,e.z));
            Vector3 extents=new Vector3(Mathf.Abs(ex.x)+Mathf.Abs(ey.x)+Mathf.Abs(ez.x),Mathf.Abs(ex.y)+Mathf.Abs(ey.y)+Mathf.Abs(ez.y),Mathf.Abs(ex.z)+Mathf.Abs(ey.z)+Mathf.Abs(ez.z));
            output.Add(new MapSurfaceSceneOverlayAuthoringData
            {
                Center=center,Rotation=Quaternion.identity,HalfExtents=new Vector2(extents.x,extents.z),Height=center.y+extents.y,
                Normal=Vector3.up,SurfaceType=type,MovementMask=mask,Flags=flags|MapSurfaceFlags.ExactMesh,LayerId=layer,
                Geometry=geometry,WorldToMesh=matrix.inverse
            });
        }
    }
}
