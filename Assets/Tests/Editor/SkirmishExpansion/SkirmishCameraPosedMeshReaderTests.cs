#if UNITY_EDITOR
using System;
using Game.Editor;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishCameraPosedMeshReaderTests
    {
        static Color[] X(float translation)=>new[]{new Color(1,0,0,translation),new Color(1,0,0,translation+10)};
        static Color[] Y()=>new[]{new Color(0,1,0,0),new Color(0,1,0,0)};
        static Color[] Z()=>new[]{new Color(0,0,1,0),new Color(0,0,1,0)};
        [Test] public void ExactTextureRowsInterpolateActualFramesWithoutBindPoseFallback()
        {Assert.IsTrue(SkirmishCameraPosedMeshReader.TryPoseVertex(new Vector3(1,2,3),Vector4.zero,new Vector4(1,0,0,0),new Vector3(0,1,.25f),X(2),Y(),Z(),out var posed));
            Assert.AreEqual(new Vector3(5.5f,2,3),posed);}
        [Test] public void WeightedBonesUseUnnormalizedShaderArithmeticWithHalfExactIndices()
        {Assert.IsTrue(SkirmishCameraPosedMeshReader.TryPoseVertex(Vector3.zero,new Vector4(0,1,0,0),new Vector4(.25f,.5f,0,0),Vector3.zero,X(2),Y(),Z(),out var posed));
            Assert.AreEqual(6.5f,posed.x);}
        [Test] public void MissingNonfiniteOrOutOfTextureFrameFailsClosed()
        {foreach(var frame in new[]{new Vector3(-1,0,0),new Vector3(0,2,0),new Vector3(0,1,float.NaN),new Vector3(0,1,2)})
            Assert.IsFalse(SkirmishCameraPosedMeshReader.TryPoseVertex(Vector3.zero,Vector4.zero,new Vector4(1,0,0,0),frame,X(0),Y(),Z(),out _));
            Assert.IsFalse(SkirmishCameraPosedMeshReader.TryPoseVertex(Vector3.zero,Vector4.zero,Vector4.one,Vector3.zero,null,Y(),Z(),out _));}
        [Test] public void HalfRoundedOrFractionalBoneIndicesAreUnsupported()
        {foreach(float bone in new[]{.9f,2049f})Assert.IsFalse(SkirmishCameraPosedMeshReader.TryPoseVertex(Vector3.zero,new Vector4(bone,0,0,0),
            new Vector4(1,0,0,0),Vector3.zero,new Color[2050],new Color[2050],new Color[2050],out _));}
        static void WithDraw(Action<EntityManager,Entity,Entity> action)
        {
            using var world=new World("ActualPosedMeshDrawReaderFixture");var em=world.EntityManager;
            var shader=Shader.Find("Universal Render Pipeline/Lit");Assert.IsNotNull(shader);
            var material=new Material(shader);var mesh=new Mesh();
            try
            {
                mesh.vertices=new[]{new Vector3(-1,-1,-1),new Vector3(1,1,-1),new Vector3(0,0,1),new Vector3(100,100,100)};
                mesh.subMeshCount=2;mesh.SetIndices(new[]{0,1,2},MeshTopology.Triangles,0);mesh.SetIndices(new[]{1,2,3},MeshTopology.Triangles,1);
                var root=em.CreateEntity();var draw=em.CreateEntity();em.AddBuffer<Child>(root).Add(new Child{Value=draw});
                em.AddComponentData(draw,new Parent{Value=root});em.AddComponentData(draw,new LocalToWorld{Value=float4x4.Translate(new float3(10,20,30))});
                em.AddSharedComponentManaged(draw,new RenderMeshArray(new[]{material},new[]{mesh}));
                em.AddComponentData(draw,MaterialMeshInfo.FromRenderMeshArrayIndices(0,0,0));
                action(em,root,draw);
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);}
        }
        [Test] public void ActualChildTraversalUsesDrawnSubmeshAndWorldTransformNotSafetyBounds()
        {WithDraw((em,root,draw)=>{em.AddComponentData(draw,new WorldRenderBounds{Value=new AABB{Center=float3.zero,Extents=new float3(64)}});
            Assert.IsTrue(SkirmishCameraPosedMeshReader.TryCollect(em,root,out var facts,out string error),error);Assert.AreEqual(1,facts.Length);
            Assert.AreEqual(new Vector3(10,20,30),facts[0].WorldBounds.center);Assert.AreEqual(new Vector3(2,2,2),facts[0].WorldBounds.size);
            Assert.AreEqual(3,facts[0].DrawnIndices);Assert.AreEqual(64,em.GetComponentData<WorldRenderBounds>(draw).Value.Extents.x);});}
        [Test] public void DisabledMeshInfoCannotContributeGeometry()
        {WithDraw((em,root,draw)=>{em.SetComponentEnabled<MaterialMeshInfo>(draw,false);Assert.IsFalse(SkirmishCameraPosedMeshReader.TryCollect(em,root,out _,out _));});}
        [Test] public void UploadedNonReadableMeshIsReadWithoutChangingItsImportOrFlags()
        {WithDraw((em,root,draw)=>{var array=em.GetSharedComponentManaged<RenderMeshArray>(draw);var mesh=array.GetMesh(em.GetComponentData<MaterialMeshInfo>(draw));
            mesh.UploadMeshData(true);Assert.IsFalse(mesh.isReadable);
            Assert.IsTrue(SkirmishCameraPosedMeshReader.TryCollect(em,root,out var facts,out string error),error);Assert.AreEqual(1,facts.Length);Assert.IsFalse(mesh.isReadable);});}
        [Test] public void SelectedSubmeshIsMeasuredAndDuplicateHierarchyEdgesCountOnce()
        {WithDraw((em,root,draw)=>{em.AddBuffer<LinkedEntityGroup>(root).Add(new LinkedEntityGroup{Value=draw});
            em.SetComponentData(draw,MaterialMeshInfo.FromRenderMeshArrayIndices(0,0,1));
            Assert.IsTrue(SkirmishCameraPosedMeshReader.TryCollect(em,root,out var facts,out string error),error);Assert.AreEqual(1,facts.Length);
            Assert.AreEqual(1,facts[0].SubMesh);Assert.Greater(facts[0].WorldBounds.max.x,100);});}
        [Test] public void EntireActualMaterialMeshRangeContributesEveryDraw()
        {WithDraw((em,root,draw)=>{var old=em.GetSharedComponentManaged<RenderMeshArray>(draw);var info=em.GetComponentData<MaterialMeshInfo>(draw);
            em.SetSharedComponentManaged(draw,new RenderMeshArray(new[]{old.GetMaterial(info)},new[]{old.GetMesh(info)},new[]{
                new MaterialMeshIndex{MaterialIndex=0,MeshIndex=0,SubMeshIndex=0},new MaterialMeshIndex{MaterialIndex=0,MeshIndex=0,SubMeshIndex=1}}));
            em.SetComponentData(draw,MaterialMeshInfo.FromMaterialMeshIndexRange(0,2));
            Assert.IsTrue(SkirmishCameraPosedMeshReader.TryCollect(em,root,out var facts,out string error),error);Assert.AreEqual(2,facts.Length);
            Assert.AreEqual(0,facts[0].SubMesh);Assert.AreEqual(1,facts[1].SubMesh);});}
        [Test] public void UnsupportedShaderCannotSilentlyUseRigidBindGeometry()
        {WithDraw((em,root,draw)=>{var old=em.GetSharedComponentManaged<RenderMeshArray>(draw);var info=em.GetComponentData<MaterialMeshInfo>(draw);
            var material=new Material(Shader.Find("Hidden/InternalErrorShader"));try{
                em.SetSharedComponentManaged(draw,new RenderMeshArray(new[]{material},new[]{old.GetMesh(info)}));
                Assert.IsFalse(SkirmishCameraPosedMeshReader.TryCollect(em,root,out _,out string error));StringAssert.StartsWith("PosedMeshShaderUnsupported:",error);
            }finally{UnityEngine.Object.DestroyImmediate(material);}});}
        [Test] public void ProjectionUsesActualVerticesAndRejectsRealDepthCrossing()
        {WithDraw((em,root,draw)=>{Assert.IsTrue(SkirmishCameraPosedMeshReader.TryCollect(em,root,out var facts,out string error),error);
            var gameObject=new GameObject("PosedProjectionCamera");var camera=gameObject.AddComponent<Camera>();var texture=new RenderTexture(1024,768,16);
            try{camera.targetTexture=texture;camera.nearClipPlane=.1f;camera.farClipPlane=100;
                Assert.IsTrue(SkirmishCameraPosedMeshReader.TryProjectDraw(camera,facts[0],out var pixels));
                Vector3[] vertices={new Vector3(9,19,29),new Vector3(11,21,29),new Vector3(10,20,31)};
                float minY=float.PositiveInfinity,maxY=float.NegativeInfinity;
                foreach(var vertex in vertices){float y=camera.WorldToScreenPoint(vertex).y;minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                Assert.AreEqual(maxY-minY,pixels.height,.001f);
                camera.nearClipPlane=30;Assert.IsFalse(SkirmishCameraPosedMeshReader.TryProjectDraw(camera,facts[0],out _));
            }finally{UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(gameObject);}});}
        public static void RunFocusedValidation()
        {var t=new SkirmishCameraPosedMeshReaderTests();t.ExactTextureRowsInterpolateActualFramesWithoutBindPoseFallback();
            t.WeightedBonesUseUnnormalizedShaderArithmeticWithHalfExactIndices();t.MissingNonfiniteOrOutOfTextureFrameFailsClosed();t.HalfRoundedOrFractionalBoneIndicesAreUnsupported();
            t.ActualChildTraversalUsesDrawnSubmeshAndWorldTransformNotSafetyBounds();t.DisabledMeshInfoCannotContributeGeometry();
            t.SelectedSubmeshIsMeasuredAndDuplicateHierarchyEdgesCountOnce();t.EntireActualMaterialMeshRangeContributesEveryDraw();
            t.UnsupportedShaderCannotSilentlyUseRigidBindGeometry();
            t.ProjectionUsesActualVerticesAndRejectsRealDepthCrossing();
            t.UploadedNonReadableMeshIsReadWithoutChangingItsImportOrFlags();
            Debug.Log("[SkirmishCameraPosedMeshReaderTests] result=Passed tests=11 visualAcceptance=0");}
    }
}
#endif
