using System;
using Game.Editor.MapVariants;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class DenseCityMicroRubbleMeshOptimizerTests
    {
        private static Mesh MakeFans()
        {
            var vertices = new Vector3[26]; var indices = new int[72];
            for (int blob = 0; blob < 2; blob++)
            {
                int start = blob * 13; var center = new Vector3(blob * 17.123f, blob * 3.456f, -blob * 5.789f);
                vertices[start] = center;
                for (int edge = 0; edge < 12; edge++)
                {
                    float angle = edge * Mathf.PI / 6;
                    vertices[start + 1 + edge] = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * .18f;
                    int i = blob * 36 + edge * 3;
                    indices[i] = start; indices[i + 1] = start + 1 + (edge + 1) % 12; indices[i + 2] = start + 1 + edge;
                }
            }
            var mesh = new Mesh { name = "MicroRubbleFixture", vertices = vertices, triangles = indices };
            mesh.RecalculateNormals(); return mesh;
        }

        [Test]
        public void DistinctScatterCentersNormalsAndUpwardFacesSurviveReduction()
        {
            var source = MakeFans(); Mesh reduced = null;
            try
            {
                reduced = DenseCityMicroRubbleMeshOptimizer.CreateLowPolyMesh(source);
                Assert.AreEqual(10, reduced.vertexCount); Assert.AreEqual(24, reduced.GetIndexCount(0));
                var before = source.vertices; var after = reduced.vertices; var indices = reduced.triangles;
                Assert.AreEqual(before[0], after[0]); Assert.AreEqual(before[13], after[5]);
                for (int blob = 0; blob < 2; blob++)
                    for (int edge = 0; edge < 4; edge++)
                    {
                        int vertex = blob * 5 + edge + 1;
                        Assert.AreEqual(before[blob * 13 + edge * 3 + 1], after[vertex]);
                        Assert.AreEqual(source.normals[blob * 13 + edge * 3 + 1], reduced.normals[vertex]);
                    }
                for (int i = 0; i < indices.Length; i += 3)
                    Assert.Greater(Vector3.Cross(after[indices[i + 1]] - after[indices[i]],
                        after[indices[i + 2]] - after[indices[i]]).y, 0, "ground faces remain visible from above");
            }
            finally { if (reduced != null) UnityEngine.Object.DestroyImmediate(reduced); UnityEngine.Object.DestroyImmediate(source); }
        }

        [Test]
        public void UnrecognizedFansAndTextureCoordinatesAreRejected()
        {
            var source = MakeFans();
            try
            {
                var indices = source.triangles; indices[1] = 14; source.triangles = indices;
                Assert.Throws<InvalidOperationException>(() => DenseCityMicroRubbleMeshOptimizer.CreateLowPolyMesh(source));
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
            source = MakeFans();
            try
            {
                source.uv2 = new Vector2[source.vertexCount];
                Assert.Throws<InvalidOperationException>(() => DenseCityMicroRubbleMeshOptimizer.CreateLowPolyMesh(source));
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }

        public static void RunFocusedValidation()
        {
            var tests = new DenseCityMicroRubbleMeshOptimizerTests();
            tests.DistinctScatterCentersNormalsAndUpwardFacesSurviveReduction();
            tests.UnrecognizedFansAndTextureCoordinatesAreRejected();
            Debug.Log("[DenseCityMicroRubbleMeshOptimizerTests] result=Passed tests=2");
        }
    }
}
