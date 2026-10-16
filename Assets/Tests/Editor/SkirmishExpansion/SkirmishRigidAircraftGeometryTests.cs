using System;
using Game.Configs;
using Game.Editor;
using Game.Rendering;
using Game.Rendering.Contracts;
using Game.Runtime;
using NUnit.Framework;
using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishRigidAircraftGeometryTests
    {
        sealed class MeasuredProvider:IRenderVisibleGeometryPresentation
        {
            public SkirmishRigidAircraftGeometryConfig Cache;
            public bool TryReadVisibleGeometryBounds(EntityManager em,Entity entity,out Bounds bounds,out bool hasDraw,out string error)
                =>RigidAircraftVisibleGeometryPresentation.TryRead(em,entity,Cache,out bounds,out hasDraw,out error);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)]
        [TestCase(10)][TestCase(11)][TestCase(12)][TestCase(13)][TestCase(14)][TestCase(15)][TestCase(16)]
        public void ActualRuntimeDrawUsesMeasuredMetadata(int mutation)
        {
            using var world=new World("ActualRigidAircraftGeometryFixture");var em=world.EntityManager;
            var mesh=new Mesh();var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var cache=ScriptableObject.CreateInstance<SkirmishRigidAircraftGeometryConfig>();var sources=new GameObject[3];Material unsupported=null;
            try
            {
                mesh.vertices=new[]{new Vector3(-1,-1,-1),new Vector3(1,1,-1),new Vector3(0,0,1),new Vector3(100,100,100)};
                mesh.subMeshCount=2;mesh.SetIndices(new[]{0,1,2},MeshTopology.Triangles,0);mesh.SetIndices(new[]{1,2,3},MeshTopology.Triangles,1);
                mesh.bounds=new Bounds(Vector3.zero,Vector3.one*128); // Deliberately wrong culling/mesh metadata.
                cache.Sources=new SkirmishRigidAircraftGeometryConfig.Source[3];
                for(int i=0;i<3;i++){sources[i]=new GameObject("LogicalMetadataFixture"+i);cache.Sources[i]=new SkirmishRigidAircraftGeometryConfig.Source{Prefab=sources[i],Path="Fixture/"+i,DependencyHash=new string('a',32)};}
                cache.Draws=new[]{SkirmishRigidAircraftGeometryAuthoring.MeasureDraw(mesh,0),SkirmishRigidAircraftGeometryAuthoring.MeasureDraw(mesh,1)};
                Assert.That(cache.Draws[0].LocalBounds.size,Is.EqualTo(Vector3.one*2),"Unindexed outlier and inflated Mesh.bounds cannot define this draw");
                var actor=em.CreateEntity();em.AddComponentData(actor,new LocalToWorld{Value=float4x4.Translate(new float3(10,20,30))});
                em.AddComponentData(actor,new WorldRenderBounds{Value=new AABB{Extents=new float3(64)}});
                em.AddSharedComponentManaged(actor,new RenderMeshArray(new[]{material},new[]{mesh}));
                em.AddComponentData(actor,MaterialMeshInfo.FromRenderMeshArrayIndices(0,0,0));
                if(mutation==1)em.SetComponentEnabled<MaterialMeshInfo>(actor,false);
                if(mutation==2)cache.Draws=Array.Empty<SkirmishRigidAircraftGeometryConfig.Draw>();
                if(mutation==3){unsupported=new Material(Shader.Find("Hidden/InternalErrorShader"));em.SetSharedComponentManaged(actor,new RenderMeshArray(new[]{unsupported},new[]{mesh}));}
                if(mutation==4)em.AddComponentData(actor,new MaterialPropertyRenderPixel());
                if(mutation==5)em.SetComponentData(actor,new LocalToWorld{Value=float4x4.Translate(new float3(40,55,60))});
                if(mutation==6)em.SetComponentData(actor,MaterialMeshInfo.FromRenderMeshArrayIndices(0,0,1));
                if(mutation==7)
                {em.SetSharedComponentManaged(actor,new RenderMeshArray(new[]{material},new[]{mesh},new[]{new MaterialMeshIndex{MeshIndex=0,MaterialIndex=0,SubMeshIndex=0},new MaterialMeshIndex{MeshIndex=0,MaterialIndex=0,SubMeshIndex=1}}));em.SetComponentData(actor,MaterialMeshInfo.FromMaterialMeshIndexRange(0,2));}
                if(mutation==8||mutation==9)
                {
                    var expected=ScriptableObject.CreateInstance<SkirmishRigidAircraftGeometryConfig>();
                    try
                    {
                        expected.Sources=(SkirmishRigidAircraftGeometryConfig.Source[])cache.Sources.Clone();expected.Draws=cache.Draws;
                        if(mutation==8)expected.Sources[0]=new SkirmishRigidAircraftGeometryConfig.Source{Prefab=sources[0],Path="Fixture/0",DependencyHash=new string('b',32)};
                        else{mesh.SetIndices(new[]{2,1,0},MeshTopology.Triangles,0);expected.Draws=new[]{SkirmishRigidAircraftGeometryAuthoring.MeasureDraw(mesh,0),cache.Draws[1]};}
                        Assert.That(SkirmishRigidAircraftGeometryAuthoring.Equal(cache,expected,out _),Is.False,"Dependency or exact indexed geometry must invalidate cache even at identical counts/bounds");return;
                    }
                    finally{UnityEngine.Object.DestroyImmediate(expected);}
                }
                mesh.UploadMeshData(true);Assert.That(mesh.isReadable,Is.False);
                if(mutation>=13)
                {
                    Assert.That(RigidAircraftVisibleGeometryPresentation.TryRead(em,actor,cache,out _,out _,out _),Is.True);
                    if(mutation==13)cache.SchemaVersion=2;
                    if(mutation==14)cache.Draws[0].LocalBounds=new Bounds(Vector3.zero,Vector3.one*100);
                    if(mutation==15)cache.Sources[0].DependencyHash=new string('b',32);
                    if(mutation==16)cache.Draws[0].GeometrySha256=new string('b',64);
                    Assert.That(RigidAircraftVisibleGeometryPresentation.TryRead(em,actor,cache,out _,out _,out _),Is.False,"Mutation after first successful lookup must not reuse stale validation");return;
                }
                if(mutation>=10)
                {
                    var root=em.CreateEntity();em.AddBuffer<Child>(root).Add(new Child{Value=actor});em.AddBuffer<LinkedEntityGroup>(root).Add(new LinkedEntityGroup{Value=actor});
                    var wing=em.CreateEntity();em.AddComponentData(wing,new LocalToWorld{Value=float4x4.Translate(new float3(110,20,30))});
                    em.AddSharedComponentManaged(wing,new RenderMeshArray(new[]{material},new[]{mesh}));em.AddComponentData(wing,MaterialMeshInfo.FromRenderMeshArrayIndices(0,0,(ushort)(mutation==11?2:0)));
                    em.GetBuffer<Child>(root).Add(new Child{Value=wing});if(mutation==12)em.DestroyEntity(wing);
                    bool complete=SkirmishMissionViewProfileApplication.TryReadRigidAircraftVisibleHierarchy(em,root,new MeasuredProvider{Cache=cache},out var full);
                    Assert.That(complete,Is.EqualTo(mutation==10),"Missing/unsupported wing cannot silently fall back to body-only fit");
                    if(complete){Assert.That(full.min.x,Is.EqualTo(9));Assert.That(full.max.x,Is.EqualTo(111));}
                    return;
                }
                bool result=RigidAircraftVisibleGeometryPresentation.TryRead(em,actor,cache,out var bounds,out bool drawn,out string failure);
                if(mutation>=2&&mutation<=4){Assert.That(result,Is.False);Assert.That(failure,Is.Not.Empty);return;}
                Assert.That(result,Is.True,failure);Assert.That(drawn,Is.EqualTo(mutation!=1));
                if(mutation==1)return;
                Assert.That(bounds.center,Is.EqualTo(mutation==5?new Vector3(40,55,60):mutation==6?new Vector3(60,70,79.5f):mutation==7?new Vector3(59.5f,69.5f,79.5f):new Vector3(10,20,30)));
                Assert.That(em.GetComponentData<WorldRenderBounds>(actor).Value.Extents.x,Is.EqualTo(64),"Culling safety bounds remain untouched");
                Assert.That(mesh.isReadable,Is.False,"Runtime did not read back or change the uploaded mesh");
            }
            finally{foreach(var source in sources)if(source!=null)UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(cache);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);if(unsupported!=null)UnityEngine.Object.DestroyImmediate(unsupported);}
        }
        [TestCase(false)][TestCase(true)] public void EditorPlayEntryUsesActualSourceAndVertexVerification(bool geometryMutation)
        {
            var measured=SkirmishRigidAircraftGeometryAuthoring.Measure();
            try
            {
                Assert.DoesNotThrow(()=>SkirmishRigidAircraftGeometryAuthoring.ValidateForEditorPlay(measured));
                if(geometryMutation)measured.Draws[0].GeometrySha256=new string('b',64);
                else measured.Sources[0].DependencyHash=new string('b',32);
                Assert.Throws<InvalidOperationException>(()=>SkirmishRigidAircraftGeometryAuthoring.ValidateForEditorPlay(measured));
            }
            finally{UnityEngine.Object.DestroyImmediate(measured);}
        }
        [Test] public void AircraftFitUsesIndependentAltitudeAndFov()
        {
            var catalog=SkirmishExpansionCatalogConfig.Load();Assert.That(catalog.TryGetDefinition("S005",out var definition),Is.True);
            var profile=SkirmishMissionViewProfile.CreateS005EngineeringCandidate(definition);
            try
            {
                profile.MinHeight=12;profile.MaxHeight=profile.BuildHeight=20;profile.NormalHeight=18;profile.Pitch=65;
                var bounds=new Bounds(new Vector3(100,55,100),new Vector3(30,8,24));
                Assert.That(SkirmishMissionViewProfileApplication.TryAircraftPose(bounds,profile,16f/9,.1f,out var position,out _),Is.True);
                Assert.That(position.y,Is.GreaterThan(profile.MaxHeight),"Aircraft altitude is independent of the ground zoom ceiling");
                Assert.That(profile.AircraftFieldOfView,Is.EqualTo(60));Assert.That(profile.NormalFieldOfView,Is.EqualTo(46));
            }
            finally{UnityEngine.Object.DestroyImmediate(profile);}
        }
        public static void RunFocusedValidation()
        {var t=new SkirmishRigidAircraftGeometryTests();for(int i=0;i<17;i++)t.ActualRuntimeDrawUsesMeasuredMetadata(i);t.EditorPlayEntryUsesActualSourceAndVertexVerification(false);t.EditorPlayEntryUsesActualSourceAndVertexVerification(true);t.AircraftFitUsesIndependentAltitudeAndFov();Debug.Log("[RigidAircraftGeometry] result=Passed controls=20 nativeCameraAcceptance=Pending");}
    }
}
