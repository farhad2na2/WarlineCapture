using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Game.Editor;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class StaticMapMeshReadbackUtilityTests
{
    private const MeshUpdateFlags CopyFlags =
        MeshUpdateFlags.DontRecalculateBounds |
        MeshUpdateFlags.DontValidateIndices |
        MeshUpdateFlags.DontNotifyMeshUsers;

    private static readonly Vector3[] ExpectedPositions =
    {
        new(0f, 0f, 0f),
        new(1f, 0f, 0f),
        new(0f, 1f, 0f),
        new(10f, 0f, 0f),
        new(11f, 0f, 0f),
        new(10f, 1f, 0f)
    };

    private static readonly Vector3[] ExpectedNormals =
    {
        Vector3.forward,
        Vector3.forward,
        Vector3.forward,
        Vector3.back,
        Vector3.back,
        Vector3.back
    };

    private static readonly Vector2[] ExpectedUvs =
    {
        new(0f, 0f),
        new(1f, 0f),
        new(0f, 1f),
        new(0.25f, 0.25f),
        new(0.75f, 0.25f),
        new(0.25f, 0.75f)
    };

    private static readonly Color32[] ExpectedColors =
    {
        new(255, 0, 0, 255),
        new(0, 255, 0, 255),
        new(0, 0, 255, 255),
        new(255, 255, 0, 255),
        new(0, 255, 255, 255),
        new(255, 0, 255, 255)
    };

    [Test]
    public void CreateReadableClone_WithNullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CreateReadableClone(null));
    }

    [Test]
    public void CreateReadableClone_PreservesUnreadableMultiStreamMeshAndSource()
    {
        Mesh source = CreateSourceMesh();
        Mesh clone = null;

        try
        {
            source.UploadMeshData(true);
            Assert.That(source.isReadable, Is.False);

            MeshSnapshot sourceBefore = CaptureSnapshot(source);
            clone = CreateReadableClone(source);

            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.isReadable, Is.True);
            Assert.That(clone.indexFormat, Is.EqualTo(IndexFormat.UInt32));
            Assert.That(clone.GetVertexAttributes(), Is.EqualTo(sourceBefore.VertexAttributes));
            Assert.That(clone.vertices, Is.EqualTo(ExpectedPositions));
            Assert.That(clone.normals, Is.EqualTo(ExpectedNormals));
            Assert.That(clone.uv, Is.EqualTo(ExpectedUvs));
            Assert.That(clone.colors32, Is.EqualTo(ExpectedColors));
            Assert.That(clone.GetIndices(0, false), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(clone.GetIndices(1, false), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(clone.GetIndices(1, true), Is.EqualTo(new[] { 3, 4, 5 }));

            MeshSnapshot cloneSnapshot = CaptureSnapshot(clone);
            AssertSnapshotsEqual(sourceBefore, cloneSnapshot);

            Assert.That(source.isReadable, Is.False);
            Assert.That(source.name, Is.EqualTo("Unreadable Multi-Stream Source"));
            AssertSnapshotsEqual(sourceBefore, CaptureSnapshot(source));
        }
        finally
        {
            if (clone != null)
                UnityEngine.Object.DestroyImmediate(clone);
            UnityEngine.Object.DestroyImmediate(source);
        }
    }

    private static Mesh CreateReadableClone(Mesh source)
    {
        MethodInfo method = typeof(StaticMapMeshReadbackUtility).GetMethod(
            "CreateReadableClone",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, "Missing internal CreateReadableClone contract.");

        try
        {
            return (Mesh)method.Invoke(null, new object[] { source });
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static Mesh CreateSourceMesh()
    {
        var mesh = new Mesh
        {
            name = "Unreadable Multi-Stream Source"
        };

        var vertexAttributes = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 1),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 2)
        };

        mesh.SetVertexBufferParams(ExpectedPositions.Length, vertexAttributes);
        mesh.SetVertexBufferData(ExpectedPositions, 0, 0, ExpectedPositions.Length, 0, CopyFlags);

        var normalUvs = new NormalUv[ExpectedPositions.Length];
        for (int i = 0; i < normalUvs.Length; i++)
            normalUvs[i] = new NormalUv(ExpectedNormals[i], ExpectedUvs[i]);
        mesh.SetVertexBufferData(normalUvs, 0, 0, normalUvs.Length, 1, CopyFlags);
        mesh.SetVertexBufferData(ExpectedColors, 0, 0, ExpectedColors.Length, 2, CopyFlags);

        var indices = new uint[] { 0, 1, 2, 0, 1, 2 };
        mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
        mesh.SetIndexBufferData(indices, 0, 0, indices.Length, CopyFlags);

        var firstSubMesh = new SubMeshDescriptor(0, 3, MeshTopology.Triangles)
        {
            baseVertex = 0,
            firstVertex = 0,
            vertexCount = 3,
            bounds = new Bounds(new Vector3(0.5f, 0.5f, 0f), new Vector3(1f, 1f, 0.25f))
        };
        var secondSubMesh = new SubMeshDescriptor(3, 3, MeshTopology.Triangles)
        {
            baseVertex = 3,
            firstVertex = 3,
            vertexCount = 3,
            bounds = new Bounds(new Vector3(10.5f, 0.5f, 0f), new Vector3(1f, 1f, 0.5f))
        };

        mesh.subMeshCount = 2;
        mesh.SetSubMesh(0, firstSubMesh, CopyFlags);
        mesh.SetSubMesh(1, secondSubMesh, CopyFlags);
        mesh.bounds = new Bounds(new Vector3(5.5f, 0.5f, 2f), new Vector3(12f, 2f, 6f));
        return mesh;
    }

    private static MeshSnapshot CaptureSnapshot(Mesh mesh)
    {
        using Mesh.MeshDataArray dataArray = MeshUtility.AcquireReadOnlyMeshData(mesh);
        Mesh.MeshData data = dataArray[0];
        var vertexStreams = new byte[data.vertexBufferCount][];
        for (int stream = 0; stream < vertexStreams.Length; stream++)
            vertexStreams[stream] = data.GetVertexData<byte>(stream).ToArray();

        var subMeshes = new SubMeshDescriptor[data.subMeshCount];
        for (int subMesh = 0; subMesh < subMeshes.Length; subMesh++)
            subMeshes[subMesh] = data.GetSubMesh(subMesh);

        return new MeshSnapshot(
            mesh.GetVertexAttributes(),
            vertexStreams,
            data.GetIndexData<byte>().ToArray(),
            data.indexFormat,
            subMeshes,
            mesh.bounds);
    }

    private static void AssertSnapshotsEqual(MeshSnapshot expected, MeshSnapshot actual)
    {
        Assert.That(actual.VertexAttributes, Is.EqualTo(expected.VertexAttributes));
        Assert.That(actual.VertexStreams.Length, Is.EqualTo(expected.VertexStreams.Length));
        for (int stream = 0; stream < expected.VertexStreams.Length; stream++)
            Assert.That(actual.VertexStreams[stream], Is.EqualTo(expected.VertexStreams[stream]), $"Vertex stream {stream} differs.");

        Assert.That(actual.IndexFormat, Is.EqualTo(expected.IndexFormat));
        Assert.That(actual.IndexData, Is.EqualTo(expected.IndexData));
        Assert.That(actual.SubMeshes.Length, Is.EqualTo(expected.SubMeshes.Length));
        for (int subMesh = 0; subMesh < expected.SubMeshes.Length; subMesh++)
            AssertSubMeshesEqual(expected.SubMeshes[subMesh], actual.SubMeshes[subMesh], subMesh);

        AssertBoundsEqual(expected.Bounds, actual.Bounds, "Mesh bounds");
    }

    private static void AssertSubMeshesEqual(SubMeshDescriptor expected, SubMeshDescriptor actual, int subMesh)
    {
        Assert.That(actual.indexStart, Is.EqualTo(expected.indexStart), $"Submesh {subMesh} indexStart differs.");
        Assert.That(actual.indexCount, Is.EqualTo(expected.indexCount), $"Submesh {subMesh} indexCount differs.");
        Assert.That(actual.topology, Is.EqualTo(expected.topology), $"Submesh {subMesh} topology differs.");
        Assert.That(actual.baseVertex, Is.EqualTo(expected.baseVertex), $"Submesh {subMesh} baseVertex differs.");
        Assert.That(actual.firstVertex, Is.EqualTo(expected.firstVertex), $"Submesh {subMesh} firstVertex differs.");
        Assert.That(actual.vertexCount, Is.EqualTo(expected.vertexCount), $"Submesh {subMesh} vertexCount differs.");
        AssertBoundsEqual(expected.bounds, actual.bounds, $"Submesh {subMesh} bounds");
    }

    private static void AssertBoundsEqual(Bounds expected, Bounds actual, string message)
    {
        Assert.That(actual.center, Is.EqualTo(expected.center), $"{message} center differs.");
        Assert.That(actual.size, Is.EqualTo(expected.size), $"{message} size differs.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NormalUv
    {
        public readonly Vector3 Normal;
        public readonly Vector2 Uv;

        public NormalUv(Vector3 normal, Vector2 uv)
        {
            Normal = normal;
            Uv = uv;
        }
    }

    private sealed class MeshSnapshot
    {
        public readonly VertexAttributeDescriptor[] VertexAttributes;
        public readonly byte[][] VertexStreams;
        public readonly byte[] IndexData;
        public readonly IndexFormat IndexFormat;
        public readonly SubMeshDescriptor[] SubMeshes;
        public readonly Bounds Bounds;

        public MeshSnapshot(
            VertexAttributeDescriptor[] vertexAttributes,
            byte[][] vertexStreams,
            byte[] indexData,
            IndexFormat indexFormat,
            SubMeshDescriptor[] subMeshes,
            Bounds bounds)
        {
            VertexAttributes = vertexAttributes;
            VertexStreams = vertexStreams;
            IndexData = indexData;
            IndexFormat = indexFormat;
            SubMeshes = subMeshes;
            Bounds = bounds;
        }
    }
}
