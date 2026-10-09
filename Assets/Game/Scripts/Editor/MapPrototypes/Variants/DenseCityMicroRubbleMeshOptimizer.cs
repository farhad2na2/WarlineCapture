#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor.MapVariants
{
    public static class DenseCityMicroRubbleMeshOptimizer
    {
        private const string MeshPath = "Assets/Game/Art/MapBeautify/DenseCity/DenseCity_SmallRubble.asset";
        private const int BlobCount = 489820;

        public static Mesh CreateLowPolyMesh(Mesh source)
        {
            if (source == null || source.subMeshCount != 1 || source.vertexCount % 13 != 0 ||
                source.GetTopology(0) != MeshTopology.Triangles ||
                source.HasVertexAttribute(VertexAttribute.TexCoord0) ||
                source.HasVertexAttribute(VertexAttribute.Color) ||
                source.HasVertexAttribute(VertexAttribute.Tangent))
                throw new InvalidOperationException("Unsupported micro-rubble mesh attributes/topology.");
            for (int channel = 1; channel < 8; channel++)
                if (source.HasVertexAttribute((VertexAttribute)((int)VertexAttribute.TexCoord0 + channel)))
                    throw new InvalidOperationException("Textured rubble cannot use the untextured fan optimization.");
            var vertices = source.vertices;
            var normals = source.normals;
            var indices = source.GetIndices(0);
            int count = vertices.Length / 13;
            if (normals.Length != vertices.Length || indices.Length != count * 36)
                throw new InvalidOperationException("Micro-rubble fan layout changed.");
            var reducedVertices = new Vector3[count * 5];
            var reducedNormals = new Vector3[count * 5];
            var reducedIndices = new int[count * 12];
            for (int blob = 0; blob < count; blob++)
            {
                int oldStart = blob * 13, start = blob * 5;
                for (int edge = 0; edge < 12; edge++)
                {
                    int i = blob * 36 + edge * 3;
                    if (indices[i] != oldStart || indices[i + 1] != oldStart + 1 + (edge + 1) % 12 ||
                        indices[i + 2] != oldStart + 1 + edge)
                        throw new InvalidOperationException("Micro-rubble indices are not independent twelve-edge fans.");
                }
                reducedVertices[start] = vertices[oldStart];
                reducedNormals[start] = normals[oldStart];
                for (int edge = 0; edge < 4; edge++)
                {
                    reducedVertices[start + 1 + edge] = vertices[oldStart + 1 + edge * 3];
                    reducedNormals[start + 1 + edge] = normals[oldStart + 1 + edge * 3];
                    int i = blob * 12 + edge * 3;
                    reducedIndices[i] = start;
                    reducedIndices[i + 1] = start + 1 + (edge + 1) % 4;
                    reducedIndices[i + 2] = start + 1 + edge;
                }
            }
            var result = new Mesh { name = source.name, indexFormat = IndexFormat.UInt32 };
            result.vertices = reducedVertices;
            result.normals = reducedNormals;
            result.SetIndices(reducedIndices, MeshTopology.Triangles, 0);
            result.RecalculateBounds();
            return result;
        }

        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Shared art optimization requires edit mode.");
            var source = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (source == null || source.vertexCount != BlobCount * 13 || source.GetIndexCount(0) != BlobCount * 36)
                throw new InvalidOperationException("Original shared micro-rubble mesh is not the audited candidate.");
            string beforeHash = FileDigest(MeshPath);
            string backup = Path.Combine(Path.GetTempPath(), "warline-micro-rubble-original-" + beforeHash + ".asset");
            if (!File.Exists(backup)) File.Copy(MeshPath, backup);
            string centersBefore = CenterDigest(source, 13);
            string guid = AssetDatabase.AssetPathToGUID(MeshPath);
            var reduced = CreateLowPolyMesh(source);
            try
            {
                // Keep the original shared asset and GUID. No source scene,
                // terrain, road, owner, collider or scatter placement is edited.
                EditorUtility.CopySerialized(reduced, source);
                EditorUtility.SetDirty(source);
                AssetDatabase.SaveAssetIfDirty(source);
                AssetDatabase.ImportAsset(MeshPath, ImportAssetOptions.ForceUpdate);
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
                if (saved.vertexCount != BlobCount * 5 || saved.GetIndexCount(0) != BlobCount * 12 ||
                    CenterDigest(saved, 5) != centersBefore || AssetDatabase.AssetPathToGUID(MeshPath) != guid)
                    throw new InvalidOperationException("Saved shared rubble conservation check failed; original backup=" + backup);
                string report = "Design/AgentReports/SkirmishExpansion/S005/shared-micro-rubble-optimization.json";
                File.WriteAllText(report, JsonUtility.ToJson(new Proof { meshPath = MeshPath, assetGuid = guid,
                    originalAssetSha256 = beforeHash, optimizedAssetSha256 = FileDigest(MeshPath),
                    originalBackupPath = backup, scatterCenterSha256 = centersBefore, blobCount = BlobCount,
                    trianglesBefore = BlobCount * 12, trianglesAfter = BlobCount * 4,
                    scatterCentersPreserved = true, physicalSceneEdited = false, nativeVisualReview = "Pending" }, true) + "\n");
                Debug.Log("[DenseCityMicroRubbleMeshOptimizer] result=Passed blobs=" + BlobCount +
                    " trianglesBefore=" + BlobCount * 12 + " trianglesAfter=" + BlobCount * 4 +
                    " centersPreserved=1 assetGuidPreserved=1 geometryOwnersChanged=0 nativeVisualReview=Pending");
            }
            finally { UnityEngine.Object.DestroyImmediate(reduced); }
        }

        private static string CenterDigest(Mesh mesh, int stride)
        {
            using var sha = SHA256.Create();
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                var vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i += stride)
                { writer.Write(vertices[i].x); writer.Write(vertices[i].y); writer.Write(vertices[i].z); }
            }
            return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }
        private static string FileDigest(string path)
        {
            using var sha = SHA256.Create(); using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        [Serializable] private sealed class Proof
        {
            public string meshPath, assetGuid, originalAssetSha256, optimizedAssetSha256,
                originalBackupPath, scatterCenterSha256, nativeVisualReview;
            public int blobCount, trianglesBefore, trianglesAfter;
            public bool scatterCentersPreserved, physicalSceneEdited;
        }
    }
}
#endif
