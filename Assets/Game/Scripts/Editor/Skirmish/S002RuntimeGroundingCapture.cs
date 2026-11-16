#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Editor
{
    // The missing authoritative input for offline grounded transforms. This is
    // part of the existing passive startup observation, not a second game launch.
    internal static class S002RuntimeGroundingCapture
    {
        internal static JObject Write(EntityManager em, Entity surfaceEntity, MapSurfaceComponent surface, string path, JObject artifacts)
        {
            var reference = surface.SurfaceBlob;
            ref var blob = ref reference.Value;
            if (!MapSurfaceBlobAccess.IsCompactSingleLayer(ref blob) || blob.Connections.Length != 0 ||
                blob.CompactSamples.Length != checked(surface.Dimensions.x * surface.Dimensions.y) ||
                !math.all(blob.Dimensions == surface.Dimensions) || blob.CellSize != surface.CellSize || !math.all(blob.GridOrigin == surface.GridOrigin))
                throw new InvalidOperationException("Unsupported actual S002 grounding surface; no flat fallback");
            S002StartupAllocatorCapture.RequireSameSurface(surface);
            S002PhysicalStartupCapture.RequireSameSurface(surface);
            bool hasOverlays = em.HasBuffer<MapSurfaceSceneOverlay>(surfaceEntity);
            bool hasTriangles = em.HasBuffer<MapSurfaceMeshTriangle>(surfaceEntity);
            bool hasInstances = em.HasBuffer<MapSurfaceMeshInstance>(surfaceEntity);
            var overlays = hasOverlays ? em.GetBuffer<MapSurfaceSceneOverlay>(surfaceEntity, true) : default;
            var triangles = hasTriangles ? em.GetBuffer<MapSurfaceMeshTriangle>(surfaceEntity, true) : default;
            var instances = hasInstances ? em.GetBuffer<MapSurfaceMeshInstance>(surfaceEntity, true) : default;
            int overlayCount = hasOverlays ? overlays.Length : 0;
            int triangleCount = hasTriangles ? triangles.Length : 0;
            int instanceCount = hasInstances ? instances.Length : 0;
            if (overlayCount > 1000000 || triangleCount > 16000000 || instanceCount > 1000000)
                throw new InvalidOperationException("Actual grounding geometry exceeds bounded profile");
            WriteFile(path, "surface-compact.bin", "compact12-le", writer =>
            {
                ref var actual = ref reference.Value;
                for (int i = 0; i < actual.CompactSamples.Length; i++)
                {
                    var s = actual.CompactSamples[i];
                    writer.Write(s.PackedHeight); writer.Write(s.NormalX); writer.Write(s.NormalY); writer.Write(s.NormalZ);
                    writer.Write(s.LayerId); writer.Write((byte)s.SurfaceType); writer.Write((ushort)s.MovementMask); writer.Write((ushort)s.Flags);
                }
            }, artifacts);
            WriteFile(path, "surface-overlays.bin", "overlay77-le", writer =>
            {
                writer.Write(overlayCount);
                for (int i = 0; i < overlayCount; i++)
                {
                    var v = overlays[i]; F3(writer, v.Center); writer.Write(v.YawRadians); F2(writer, v.HalfExtents);
                    writer.Write(v.Height); F3(writer, v.Normal); F2(writer, v.TriangleA); F2(writer, v.TriangleB); F2(writer, v.TriangleC);
                    writer.Write(v.GeometryInstanceIndex); writer.Write((byte)v.SurfaceType); writer.Write((ushort)v.MovementMask);
                    writer.Write((ushort)v.Flags); writer.Write(v.LayerId);
                }
            }, artifacts);
            WriteFile(path, "surface-triangles.bin", "triangle36-le", writer =>
            {
                writer.Write(triangleCount);
                for (int i = 0; i < triangleCount; i++) { var v = triangles[i]; F3(writer, v.A); F3(writer, v.B); F3(writer, v.C); }
            }, artifacts);
            WriteFile(path, "surface-instances.bin", "instance72-le", writer =>
            {
                writer.Write(instanceCount);
                for (int i = 0; i < instanceCount; i++)
                {
                    var v = instances[i]; F4(writer, v.WorldToMesh.c0); F4(writer, v.WorldToMesh.c1); F4(writer, v.WorldToMesh.c2); F4(writer, v.WorldToMesh.c3);
                    writer.Write(v.FirstTriangle); writer.Write(v.TriangleCount);
                }
            }, artifacts);
            return new JObject
            {
                ["runtime_encoding"] = (int)blob.RuntimeEncoding, ["width"] = surface.Dimensions.x, ["height"] = surface.Dimensions.y,
                ["cell_size"] = surface.CellSize, ["origin"] = new JArray(surface.GridOrigin.x, surface.GridOrigin.y, surface.GridOrigin.z),
                ["compact_min_height"] = blob.CompactMinHeight, ["compact_height_step"] = blob.CompactHeightStep,
                ["compact_count"] = blob.CompactSamples.Length, ["connection_count"] = 0,
                ["overlay_count"] = overlayCount, ["triangle_count"] = triangleCount, ["instance_count"] = instanceCount,
                ["overlays_buffer_present"] = hasOverlays, ["triangles_buffer_present"] = hasTriangles, ["instances_buffer_present"] = hasInstances,
                ["compact_artifact"] = "surface-compact.bin", ["overlays_artifact"] = "surface-overlays.bin",
                ["triangles_artifact"] = "surface-triangles.bin", ["instances_artifact"] = "surface-instances.bin",
                ["same_blob_as_all_allocator_transactions"] = true, ["ground_offset"] = 0
            };
        }
        static void WriteFile(string root, string name, string encoding, Action<BinaryWriter> write, JObject artifacts)
        {
            string path = Path.Combine(root, name);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new BinaryWriter(stream)) write(writer);
            artifacts[name] = new JObject { ["encoding"] = encoding, ["bytes"] = new FileInfo(path).Length, ["sha256"] = SkirmishOfflineDataExporter.HashFile(path) };
        }
        static void F2(BinaryWriter w, float2 v) { w.Write(v.x); w.Write(v.y); }
        static void F3(BinaryWriter w, float3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
        static void F4(BinaryWriter w, float4 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); w.Write(v.w); }
    }
}
#endif
