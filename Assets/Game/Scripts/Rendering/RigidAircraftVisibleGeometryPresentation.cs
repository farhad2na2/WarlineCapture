using System;
using Game.Configs;
using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Rendering
{
    public static class RigidAircraftVisibleGeometryPresentation
    {
        public static bool TryRead(EntityManager em,Entity entity,out Bounds bounds,out bool hasDraw,out string failure)
            =>TryRead(em,entity,SkirmishRigidAircraftGeometryConfig.Load(),out bounds,out hasDraw,out failure);
        public static bool TryRead(EntityManager em,Entity entity,IRigidAircraftGeometryDrawLookup cache,out Bounds bounds,out bool hasDraw,out string failure)
        {
            bounds=default;hasDraw=false;failure=null;
            if(!em.Exists(entity)){failure="VisibleGeometryEntityMissing";return false;}
            if(em.HasComponent<Disabled>(entity)||em.HasComponent<DisableRendering>(entity)||em.HasComponent<Prefab>(entity)||
                !em.HasComponent<MaterialMeshInfo>(entity)||!em.IsComponentEnabled<MaterialMeshInfo>(entity))return true;
            if(cache==null||!em.HasComponent<LocalToWorld>(entity)||!em.HasComponent<RenderMeshArray>(entity))
            {failure="RigidAircraftCacheOrDrawBindingMissing";return false;}
            if(em.HasComponent<MaterialPropertyRenderPixel>(entity)){failure="AnimatedDrawCannotUseRigidAircraftMetadata";return false;}
            var matrix=em.GetComponentData<LocalToWorld>(entity).Value;
            for(int c=0;c<4;c++)if(!math.all(math.isfinite(matrix[c]))){failure="RigidAircraftTransformNonfinite";return false;}
            var array=em.GetSharedComponentManaged<RenderMeshArray>(entity);var info=em.GetComponentData<MaterialMeshInfo>(entity);
            bool found=false;
            if(info.HasMaterialMeshIndexRange)
            {
                var range=info.MaterialMeshIndexRange;var indices=array.MaterialMeshIndices;
                if(indices==null||range.start<0||range.length<=0||range.end>indices.Length){failure="RigidAircraftDrawRangeInvalid";return false;}
                for(int i=range.start;i<range.end;i++)
                {
                    var draw=indices[i];
                    if(array.MeshReferences==null||array.MaterialReferences==null||draw.MeshIndex<0||draw.MaterialIndex<0||draw.MeshIndex>=array.MeshReferences.Length||draw.MaterialIndex>=array.MaterialReferences.Length)
                    {failure="RigidAircraftDrawReferenceInvalid";return false;}
                    if(!Append(array.MeshReferences[draw.MeshIndex].Value,array.MaterialReferences[draw.MaterialIndex].Value,draw.SubMeshIndex,matrix,cache,ref bounds,ref found,out failure))return false;
                }
            }
            else
            {
                if(info.Mesh>=0||info.Material>=0){failure="RigidAircraftRuntimeRegisteredDrawUnsupported";return false;}
                if(!Append(array.GetMesh(info),array.GetMaterial(info),info.SubMesh,matrix,cache,ref bounds,ref found,out failure))return false;
            }
            hasDraw=found;return found;
        }
        static bool Append(Mesh mesh,Material material,int subMesh,float4x4 matrix,IRigidAircraftGeometryDrawLookup cache,ref Bounds combined,ref bool found,out string failure)
        {
            failure="RigidAircraftDrawMetadataUnsupported";
            if(mesh==null||material==null||material.shader==null||material.shader!=Shader.Find("Universal Render Pipeline/Lit")||
                subMesh<0||subMesh>=mesh.subMeshCount||mesh.GetTopology(subMesh)!=MeshTopology.Triangles||!cache.TryRead(mesh,subMesh,out var draw)||
                draw.VertexCount!=mesh.vertexCount||draw.DrawnIndexCount!=(long)mesh.GetIndexCount(subMesh))return false;
            if(cache is IRigidAircraftWorldDrawLookup contextual)
            {
                if(!contextual.TryReadWorld(mesh,subMesh,draw,(Matrix4x4)matrix,out var world,out failure))return false;
                if(!found){combined=world;found=true;}else combined.Encapsulate(world);
                failure=null;return true;
            }
            // Cache bounds were measured from exactly the drawn indexed vertices,
            // not Mesh.bounds/submesh bounds/culling safety bounds. Transform all
            // corners conservatively at the current live renderer pose.
            for(int i=0;i<8;i++)
            {
                Vector3 local=draw.LocalBounds.center+Vector3.Scale(draw.LocalBounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var p=math.transform(matrix,(float3)local);if(!math.all(math.isfinite(p)))return false;
                if(!found){combined=new Bounds(p,Vector3.zero);found=true;}else combined.Encapsulate((Vector3)p);
            }
            failure=null;return true;
        }
    }
}
