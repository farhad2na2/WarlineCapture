using Unity.Mathematics;
using Unity.Collections;

namespace Game.Components
{
    /// <summary>Exact contact with a baked road face; bounds are only a broad-phase accelerator.</summary>
    public static class MapSurfaceSceneOverlaySampling
    {
        public static bool TrySample(in MapSurfaceSceneOverlay overlay,float3 position,out float height)
        {
            height=overlay.Height;
            if((overlay.Flags & MapSurfaceFlags.ExactMesh)!=0)return false;
            float2 delta=position.xz-overlay.Center.xz;
            if((overlay.Flags & MapSurfaceFlags.ExactTriangle)!=0)
            {
                if(math.any(math.abs(delta)>overlay.HalfExtents+.001f))return false;
                float2 a=overlay.TriangleA,b=overlay.TriangleB,c=overlay.TriangleC;
                float denominator=Cross(b-a,c-a);
                if(math.abs(denominator)<.000001f)return false;
                float u=Cross(b-delta,c-delta)/denominator;
                float v=Cross(c-delta,a-delta)/denominator;
                if(u<-.00001f||v<-.00001f||u+v>1.00001f)return false;
                if(math.abs(overlay.Normal.y)<.00001f)return false;
                height=overlay.Height-math.dot(overlay.Normal.xz,delta)/overlay.Normal.y;
                return true;
            }
            float3 local=math.mul(math.inverse(overlay.Rotation),position-overlay.Center);
            return math.abs(local.x)<=overlay.HalfExtents.x && math.abs(local.z)<=overlay.HalfExtents.y;
        }
        public static bool TrySample(in MapSurfaceSceneOverlay overlay,float3 position,
            NativeArray<MapSurfaceMeshTriangle> triangles,NativeArray<MapSurfaceMeshInstance> instances,
            out float height,out float3 normal)
        {
            normal=overlay.Normal;
            if((overlay.Flags&MapSurfaceFlags.ExactMesh)==0)return TrySample(overlay,position,out height);
            height=float.NegativeInfinity;
            if(!instances.IsCreated||!triangles.IsCreated||(uint)overlay.GeometryInstanceIndex>=(uint)instances.Length||
                math.any(math.abs(position.xz-overlay.Center.xz)>overlay.HalfExtents+.001f))return false;
            var instance=instances[overlay.GeometryInstanceIndex];
            float originHeight=overlay.Height+1;
            float3 origin=math.transform(instance.WorldToMesh,new float3(position.x,originHeight,position.z));
            float3 direction=math.rotate(instance.WorldToMesh,new float3(0,-1,0));
            float3x3 normalTransform=math.transpose(new float3x3(instance.WorldToMesh.c0.xyz,instance.WorldToMesh.c1.xyz,instance.WorldToMesh.c2.xyz));
            int end=math.min(triangles.Length,instance.FirstTriangle+instance.TriangleCount);
            for(int i=instance.FirstTriangle;i<end;i++)
            {
                var triangle=triangles[i];float3 e1=triangle.B-triangle.A,e2=triangle.C-triangle.A;
                float3 p=math.cross(direction,e2);float determinant=math.dot(e1,p);
                if(math.abs(determinant)<.0000001f)continue;
                float inverse=1/determinant;float3 t=origin-triangle.A;
                float u=math.dot(t,p)*inverse;if(u<-.00001f||u>1.00001f)continue;
                float3 q=math.cross(t,e1);float v=math.dot(direction,q)*inverse;
                if(v<-.00001f||u+v>1.00001f)continue;
                float distance=math.dot(e2,q)*inverse;if(distance<0)continue;
                float candidateHeight=originHeight-distance;if(candidateHeight<=height)continue;
                float3 candidateNormal=math.normalizesafe(math.mul(normalTransform,math.cross(e1,e2)));
                if(math.abs(candidateNormal.y)<.7f)continue;
                height=candidateHeight;normal=candidateNormal.y<0?-candidateNormal:candidateNormal;
            }
            return math.isfinite(height);
        }
        private static float Cross(float2 a,float2 b)=>a.x*b.y-a.y*b.x;
    }
}
