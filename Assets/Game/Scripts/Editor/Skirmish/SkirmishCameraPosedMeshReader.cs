using System;
using System.Collections.Generic;
using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // QA-only geometric measurement. Culling safety boxes are never modified.
    public static class SkirmishCameraPosedMeshReader
    {
        [Serializable] public sealed class Fact
        {
            public int EntityIndex,EntityVersion,SubMesh,DrawnIndices;
            public string MeshPath,MaterialPath,ShaderPath,ShaderName,GeometrySource;
            public Vector3 AnimationFrames;
            public Bounds WorldBounds;
            [NonSerialized] internal Vector3[] DrawnWorldVertices;
        }
        const string AnimatedShader="Packages/com.sniveler-code.gpu-animation/Runtime/Shaders/InstanceShader.shadergraph";
        public static bool TryCollect(EntityManager em,Entity root,out Fact[] facts,out string error)
        {
            facts=null;error="PosedMeshMissing";
            var result=new List<Fact>();var visited=new HashSet<Entity>();var queue=new Queue<Entity>();queue.Enqueue(root);
            // One read per exact texture for this synchronous observation only.
            // No persistent cache can conceal edited texture contents next frame.
            var pixels=new Dictionary<Texture2D,Color[]>();
            try
            {
                while(queue.Count>0)
                {
                    var entity=queue.Dequeue();if(!visited.Add(entity))continue;
                    if(visited.Count>4096){error="PosedMeshHierarchyLimit";return false;}
                    if(!em.Exists(entity)){error="PosedMeshHierarchyEntityMissing";return false;}
                    if(em.HasBuffer<LinkedEntityGroup>(entity))foreach(var child in em.GetBuffer<LinkedEntityGroup>(entity,true))queue.Enqueue(child.Value);
                    if(em.HasBuffer<Child>(entity))foreach(var child in em.GetBuffer<Child>(entity,true))queue.Enqueue(child.Value);
                    if(em.HasComponent<Disabled>(entity)||em.HasComponent<DisableRendering>(entity)||em.HasComponent<Prefab>(entity))continue;
                    if(!em.HasComponent<MaterialMeshInfo>(entity))continue;
                    if(!em.IsComponentEnabled<MaterialMeshInfo>(entity))continue;
                    if(!em.HasComponent<RenderMeshArray>(entity)||!em.HasComponent<LocalToWorld>(entity))
                    {error="PosedMeshRenderBindingMissing";return false;}
                    var array=em.GetSharedComponentManaged<RenderMeshArray>(entity);
                    var info=em.GetComponentData<MaterialMeshInfo>(entity);
                    if(info.HasMaterialMeshIndexRange)
                    {
                        var range=info.MaterialMeshIndexRange;var indices=array.MaterialMeshIndices;
                        if(indices==null||range.start<0||range.length<=0||range.end>indices.Length){error="PosedMeshDrawRangeInvalid";return false;}
                        for(int i=range.start;i<range.end;i++)
                        {
                            var item=indices[i];
                            if(array.MeshReferences==null||array.MaterialReferences==null||item.MeshIndex<0||item.MaterialIndex<0||
                                item.MeshIndex>=array.MeshReferences.Length||item.MaterialIndex>=array.MaterialReferences.Length)
                            {error="PosedMeshDrawReferenceInvalid";return false;}
                            if(!TryDraw(em,entity,array.MeshReferences[item.MeshIndex].Value,array.MaterialReferences[item.MaterialIndex].Value,
                                item.SubMeshIndex,pixels,out var fact,out error))return false;
                            result.Add(fact);
                        }
                    }
                    else
                    {
                        // Runtime registered IDs need a graphics-owner resolver;
                        // never substitute the first array mesh for an unknown ID.
                        if(info.Mesh>=0||info.Material>=0){error="PosedMeshRuntimeRegisteredIdUnsupported";return false;}
                        if(!TryDraw(em,entity,array.GetMesh(info),array.GetMaterial(info),info.SubMesh,pixels,out var fact,out error))return false;
                        result.Add(fact);
                    }
                }
                if(result.Count==0)return false;facts=result.ToArray();error=null;return true;
            }
            catch(Exception exception)when(exception is ArgumentException||exception is InvalidOperationException||exception is UnityException||exception is IndexOutOfRangeException)
            {error="PosedMeshRead:"+exception.GetType().Name+":"+exception.Message;return false;}
        }

        static bool TryDraw(EntityManager em,Entity entity,Mesh mesh,Material material,int subMesh,Dictionary<Texture2D,Color[]> pixels,out Fact fact,out string error)
        {
            fact=null;error="PosedMeshDrawInvalid";
            if(mesh==null||material==null||material.shader==null||subMesh<0||subMesh>=mesh.subMeshCount||mesh.GetTopology(subMesh)!=MeshTopology.Triangles)return false;
            string shaderPath=AssetDatabase.GetAssetPath(material.shader);
            bool animated=shaderPath==AnimatedShader;
            bool rigid=material.shader.name=="Universal Render Pipeline/Lit"&&shaderPath=="Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader"||
                material.shader.name=="Universal Render Pipeline/Simple Lit"&&shaderPath=="Packages/com.unity.render-pipelines.universal/Shaders/SimpleLit.shader"||
                material.shader.name=="Universal Render Pipeline/Unlit"&&shaderPath=="Packages/com.unity.render-pipelines.universal/Shaders/Unlit.shader";
            if(!animated&&!rigid){error="PosedMeshShaderUnsupported:"+material.shader.name;return false;}
            if(!animated&&em.HasComponent<MaterialPropertyRenderPixel>(entity)){error="PosedMeshAnimatedShaderMismatch";return false;}
            if(animated&&(!em.HasComponent<MaterialPropertyRenderPixel>(entity)||
                em.HasComponent<MaterialPropertyShowModel>(entity)&&em.GetComponentData<MaterialPropertyShowModel>(entity).Value!=1))
            {error="PosedMeshAnimationStateUnavailable";return false;}
            var matrix=em.GetComponentData<LocalToWorld>(entity).Value;
            for(int i=0;i<4;i++)if(!math.all(math.isfinite(matrix[i]))){error="PosedMeshTransformInvalid";return false;}
            // Editor's read-only mesh data API can inspect an uploaded production
            // mesh without enabling Read/Write or changing the imported asset.
            using var meshData=MeshUtility.AcquireReadOnlyMeshData(mesh);var data=meshData[0];
            using var vertexData=new NativeArray<Vector3>(data.vertexCount,Allocator.Temp);
            using var indexData=new NativeArray<int>(data.GetSubMesh(subMesh).indexCount,Allocator.Temp);
            data.GetVertices(vertexData);data.GetIndices(indexData,subMesh,true);
            var vertices=vertexData.ToArray();var indices=indexData.ToArray();
            if(indices.Length==0){error="PosedMeshEmptyDraw";return false;}
            var bones=new List<Vector4>();var weights=new List<Vector4>();Vector3 frames=default;
            Color[][] rows=null;
            if(animated)
            {
                if(!data.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord2)||!data.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord3))
                {error="PosedMeshSkinChannelsMissing";return false;}
                using var boneData=new NativeArray<Vector4>(data.vertexCount,Allocator.Temp);
                using var weightData=new NativeArray<Vector4>(data.vertexCount,Allocator.Temp);
                data.GetUVs(2,boneData);data.GetUVs(3,weightData);bones.AddRange(boneData.ToArray());weights.AddRange(weightData.ToArray());
                if(bones.Count!=vertices.Length||weights.Count!=vertices.Length){error="PosedMeshSkinChannelsMissing";return false;}
                frames=em.GetComponentData<MaterialPropertyRenderPixel>(entity).Value;
                rows=new Color[3][];int width=0,height=0;
                string[] names={"_SnivelerMainTextureFirst","_SnivelerMainTextureSecond","_SnivelerMainTextureThird"};
                for(int i=0;i<3;i++)
                {
                    var texture=material.GetTexture(names[i]) as Texture2D;
                    if(texture==null||!texture.isReadable||(texture.format!=TextureFormat.RGBAHalf&&texture.format!=TextureFormat.RGBAFloat)||
                        i>0&&(texture.width!=width||texture.height!=height)){error="PosedMeshAnimationTextureUnsupported";return false;}
                    width=texture.width;height=texture.height;
                    if(!pixels.TryGetValue(texture,out rows[i])){rows[i]=texture.GetPixels();pixels.Add(texture,rows[i]);}
                }
            }
            bool found=false;Bounds world=default;var drawn=new Vector3[indices.Length];int cursor=0;
            foreach(int index in indices)
            {
                if(index<0||index>=vertices.Length){error="PosedMeshVertexIndexInvalid";return false;}
                Vector3 local=vertices[index];
                if(animated&&!TryPoseVertex(local,bones[index],weights[index],frames,rows[0],rows[1],rows[2],out local))
                {error="PosedMeshAnimationSampleInvalid";return false;}
                var point=math.transform(matrix,(float3)local);
                if(!math.all(math.isfinite(point))){error="PosedMeshVertexNonfinite";return false;}
                drawn[cursor++]=point;
                if(!found){world=new Bounds(point,Vector3.zero);found=true;}else world.Encapsulate((Vector3)point);
            }
            fact=new Fact{EntityIndex=entity.Index,EntityVersion=entity.Version,SubMesh=subMesh,DrawnIndices=indices.Length,
                MeshPath=AssetDatabase.GetAssetPath(mesh),MaterialPath=AssetDatabase.GetAssetPath(material),ShaderPath=shaderPath,ShaderName=material.shader.name,
                GeometrySource=animated?"ActualGpuAnimationFrameVertices":"ActualRigidDrawnVertices",AnimationFrames=frames,WorldBounds=world,DrawnWorldVertices=drawn};
            error=null;return true;
        }
        public static bool TryProjectDraw(Camera camera,Fact fact,out Rect pixels)
        {
            pixels=default;
            if(camera==null||camera.pixelWidth<=0||camera.pixelHeight<=0||fact?.DrawnWorldVertices==null||fact.DrawnWorldVertices.Length==0)return false;
            var minimum=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var maximum=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            foreach(var point in fact.DrawnWorldVertices)
            {
                var projected=camera.WorldToScreenPoint(point);
                if(!Finite(projected)||projected.z<=camera.nearClipPlane||projected.z>=camera.farClipPlane)return false;
                minimum=Vector2.Min(minimum,projected);maximum=Vector2.Max(maximum,projected);
            }
            pixels=Rect.MinMaxRect(minimum.x,minimum.y,maximum.x,maximum.y);return pixels.width>0&&pixels.height>0;
        }
        // Matches InstanceShader.hlsl: two frame-base offsets, four weighted
        // matrix rows, then unnormalised lerp. No guessed rig or bind-pose fallback.
        public static bool TryPoseVertex(Vector3 point,Vector4 bones,Vector4 weights,Vector3 frames,Color[] row0,Color[] row1,Color[] row2,out Vector3 posed)
        {
            posed=default;
            if(row0==null||row1==null||row2==null||row0.Length!=row1.Length||row0.Length!=row2.Length||
                !Finite(point)||!Finite(frames)||frames.x<0||frames.y<0||frames.x>int.MaxValue||frames.y>int.MaxValue||frames.z<0||frames.z>1)return false;
            Vector3 first=default,second=default;
            for(int b=0;b<4;b++)
            {
                if(!float.IsFinite(bones[b])||bones[b]<0||bones[b]>int.MaxValue||math.floor(bones[b])!=bones[b]||
                    math.f16tof32(math.f32tof16(bones[b]))!=bones[b]||!float.IsFinite(weights[b])||weights[b]<0)return false;
                long a=(long)(int)frames.x+(int)bones[b],c=(long)(int)frames.y+(int)bones[b];
                if(a<0||c<0||a>=row0.Length||c>=row0.Length)return false;
                first+=Transform(point,row0[(int)a],row1[(int)a],row2[(int)a])*weights[b];
                second+=Transform(point,row0[(int)c],row1[(int)c],row2[(int)c])*weights[b];
            }
            posed=Vector3.LerpUnclamped(first,second,frames.z);return Finite(posed);
        }
        static Vector3 Transform(Vector3 p,Color x,Color y,Color z)=>new(x.r*p.x+x.g*p.y+x.b*p.z+x.a,y.r*p.x+y.g*p.y+y.b*p.z+y.a,z.r*p.x+z.g*p.y+z.b*p.z+z.a);
        static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
    }
}
