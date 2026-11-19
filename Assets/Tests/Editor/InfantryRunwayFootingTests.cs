#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Rendering;
using NUnit.Framework;
using UnityEditor;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class InfantryRunwayFootingTests
    {
        [Test]
        public void AllVisualRootsFollowTheDrawnTriangleAndReturnToTheirAuthoredPose()
        {
            using var world = new World("Runway visual footing");
            var em = world.EntityManager;
            var handle = world.GetOrCreateSystem<InfantryRunwayFootingSystem>();
            var runway = em.CreateEntity(typeof(LocalToWorld));
            em.SetComponentData(runway,new LocalToWorld { Value = float4x4.Translate(new float3(30,1,40)) });
            var triangles = em.AddBuffer<RunwayRenderTriangle>(runway);
            triangles.Add(new RunwayRenderTriangle { A = 0, B = new float3(0,0,4), C = new float3(4,1,0) });
            var owner = em.CreateEntity(typeof(LocalTransform),typeof(UnitSurfaceComponent),typeof(UnitModelLocalTransform),typeof(InfantryVisualFooting));
            var pose = new float3(.1f,-.2f,.3f);
            em.SetComponentData(owner,new UnitModelLocalTransform { Position=pose,Rotation=quaternion.identity,Scale=1 });
            em.SetComponentData(owner,new UnitSurfaceComponent { HasSurface=1, LastSampledHeight=.25f });
            Entity Visual()
            {
                var e=em.CreateEntity(typeof(LocalTransform),typeof(Parent));
                em.SetComponentData(e,new Parent{Value=owner});
                em.SetComponentData(e,LocalTransform.FromPosition(pose));
                return e;
            }
            Entity detail=Visual(), model=Visual(), mid=Visual(), low=Visual();
            em.AddComponentData(owner,new UnitDetailedVisualReference{Root=detail});
            em.AddComponentData(owner,new UnitModelInstanceReference{Instance=model});
            em.AddComponentData(owner,new UnitMidLodInstanceReference{Instance=mid});
            em.AddComponentData(owner,new UnitLowLodInstanceReference{Instance=low});
            void Tick(float x,float z,float expected)
            {
                var logical=LocalTransform.FromPositionRotationScale(new float3(x,.24f,z),quaternion.identity,2);
                em.SetComponentData(owner,logical);
                handle.Update(world.Unmanaged);
                em.CompleteAllTrackedJobs();
                Assert.That(em.GetComponentData<LocalTransform>(owner).Position,Is.EqualTo(logical.Position));
                Assert.That(em.GetComponentData<InfantryVisualFooting>(owner).Lift,Is.EqualTo(expected).Within(.0001f));
                foreach(var root in new[]{detail,model,mid,low})
                {
                    var position=em.GetComponentData<LocalTransform>(root).Position;
                    Assert.That(math.distance(position,pose+new float3(0,expected/2,0)),Is.LessThan(.0001f));
                }
            }
            Tick(31,41,1f);
            Tick(31,41,1f);
            Tick(33.5f,43.5f,0);
            Tick(31,41,1f);
            em.DestroyEntity(runway);
            Tick(31,41,0);
        }

        // A disconnected set of sampled faces can fit a triangle budget while leaving
        // holes in the body. Welding positions here ignores intended UV and normal seams.
        [TestCase("MidLOD", 0.35f)]
        [TestCase("LowLOD", 0.85f)]
        public void DistanceMeshKeepsTheSoldierSurface(string tier, float maximumRatio)
        {
            const string folder = "Assets/Game/Prefabs/Generated/";
            const string model = "SM_Chr_Soldier_Male_02_Alt_04_CombinedSkinned_31_lod0.asset";
            Mesh source = AssetDatabase.LoadAssetAtPath<Mesh>(folder + "CharactersBaked/ModelResources/" + model);
            Mesh lod = AssetDatabase.LoadAssetAtPath<Mesh>(folder + "MidLOD/" + tier + "_Unit_Chr_Soldier_Male_02_Alt_04.asset");
            Assert.That(source, Is.Not.Null);
            Assert.That(lod, Is.Not.Null);
            Assert.That(lod.subMeshCount, Is.EqualTo(source.subMeshCount));
            Assert.That(lod.triangles.Length, Is.InRange(3, Mathf.CeilToInt(source.triangles.Length * maximumRatio) + 3));
            Assert.That(BoundaryLength(lod), Is.LessThanOrEqualTo(1f + 1.5f * BoundaryLength(source)));
        }

        static float BoundaryLength(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] welded = new int[vertices.Length];
            var points = new List<Vector3>();
            var ids = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var position = Vector3Int.RoundToInt(vertices[i] * 100000f);
                if (!ids.TryGetValue(position, out int id))
                {
                    id = points.Count;
                    ids.Add(position, id);
                    points.Add(vertices[i]);
                }
                welded[i] = id;
            }

            var edges = new Dictionary<(int a, int b), int>();
            void CountEdge(int first, int second)
            {
                int a = welded[first], b = welded[second];
                if (a == b) return;
                var edge = (Math.Min(a, b), Math.Max(a, b));
                edges.TryGetValue(edge, out int count);
                edges[edge] = count + 1;
            }
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                CountEdge(triangles[i], triangles[i + 1]);
                CountEdge(triangles[i + 1], triangles[i + 2]);
                CountEdge(triangles[i + 2], triangles[i]);
            }
            float length = 0;
            foreach (var edge in edges)
                if (edge.Value == 1) length += Vector3.Distance(points[edge.Key.a], points[edge.Key.b]);
            return length;
        }

        public static void RunFocusedValidation()
        {
            new InfantryRunwayFootingTests().AllVisualRootsFollowTheDrawnTriangleAndReturnToTheirAuthoredPose();
            var tests = new InfantryRunwayFootingTests();
            tests.DistanceMeshKeepsTheSoldierSurface("MidLOD", 0.35f);
            tests.DistanceMeshKeepsTheSoldierSurface("LowLOD", 0.85f);
            Debug.Log("[InfantryRunwayFootingTests] result=Passed");
        }
    }
}
#endif
