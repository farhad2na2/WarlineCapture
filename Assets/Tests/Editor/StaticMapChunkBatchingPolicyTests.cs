using Game.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class StaticMapChunkBatchingPolicyTests
{
    [Test]
    public void BatchKeyEqualityIsDeterministicForIdenticalInputs()
    {
        Material material = CreateMaterial();
        try
        {
            StaticMapChunkBatchKey first = CreateKey(material);
            StaticMapChunkBatchKey second = CreateKey(material);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }
        finally
        {
            Object.DestroyImmediate(material);
        }
    }

    [Test]
    public void BatchKeySeparatesMaterialAndRendererState()
    {
        Material material = CreateMaterial();
        Material otherMaterial = CreateMaterial();
        try
        {
            StaticMapChunkBatchKey baseline = CreateKey(material);
            StaticMapChunkBatchKey[] variants =
            {
                CreateKey(material, chunkX: 4),
                CreateKey(material, chunkZ: -2),
                CreateKey(otherMaterial),
                CreateKey(material, lightmapIndex: 8),
                CreateKey(material, layer: 10),
                CreateKey(material, shadowCastingMode: ShadowCastingMode.Off),
                CreateKey(material, receiveShadows: false),
                CreateKey(material, lightProbeUsage: LightProbeUsage.Off),
                CreateKey(material, reflectionProbeUsage: ReflectionProbeUsage.Simple)
            };

            for (int i = 0; i < variants.Length; i++)
                Assert.That(variants[i], Is.Not.EqualTo(baseline), $"Variant {i} collapsed into the baseline batch key.");
        }
        finally
        {
            Object.DestroyImmediate(material);
            Object.DestroyImmediate(otherMaterial);
        }
    }

    [Test]
    public void ChunkCoordinatesUseFloorSemanticsAtNegativeBoundaries()
    {
        Assert.That(StaticMapChunkBatchingPolicy.GetChunkCoordinate(0f), Is.EqualTo(0));
        Assert.That(StaticMapChunkBatchingPolicy.GetChunkCoordinate(95.999f), Is.EqualTo(0));
        Assert.That(StaticMapChunkBatchingPolicy.GetChunkCoordinate(96f), Is.EqualTo(1));
        Assert.That(StaticMapChunkBatchingPolicy.GetChunkCoordinate(-0.001f), Is.EqualTo(-1));
        Assert.That(StaticMapChunkBatchingPolicy.GetChunkCoordinate(-96f), Is.EqualTo(-1));
        Assert.That(StaticMapChunkBatchingPolicy.GetChunkCoordinate(-96.001f), Is.EqualTo(-2));
    }

    [Test]
    public void SafetyClassificationAllowsOnlySupportedRendererComponents()
    {
        GameObject source = new GameObject("SafeStaticSource");
        try
        {
            source.AddComponent<MeshFilter>();
            MeshRenderer renderer = source.AddComponent<MeshRenderer>();
            source.AddComponent<BoxCollider>();

            Assert.That(
                StaticMapChunkBatchingPolicy.ClassifyRendererSafety(renderer, null, null, null, null),
                Is.EqualTo(StaticMapChunkRendererSafety.Safe));

            source.AddComponent<Light>();
            Assert.That(
                StaticMapChunkBatchingPolicy.ClassifyRendererSafety(renderer, null, null, null, null),
                Is.EqualTo(StaticMapChunkRendererSafety.UnsupportedComponents));
        }
        finally
        {
            Object.DestroyImmediate(source);
        }
    }

    [Test]
    public void SafetyClassificationRejectsLodAndOwnedHierarchies()
    {
        GameObject root = new GameObject("AuthoringRoot");
        GameObject source = new GameObject("StaticSource");
        try
        {
            source.transform.SetParent(root.transform, false);
            source.AddComponent<MeshFilter>();
            MeshRenderer renderer = source.AddComponent<MeshRenderer>();

            Assert.That(
                StaticMapChunkBatchingPolicy.ClassifyRendererSafety(renderer, null, root.transform, null, null),
                Is.EqualTo(StaticMapChunkRendererSafety.ExcludedAuthoringHierarchy));
            Assert.That(
                StaticMapChunkBatchingPolicy.ClassifyRendererSafety(renderer, root.transform, null, null, null),
                Is.EqualTo(StaticMapChunkRendererSafety.GeneratedOutput));

            root.AddComponent<LODGroup>();
            Assert.That(
                StaticMapChunkBatchingPolicy.ClassifyRendererSafety(renderer, null, null, null, null),
                Is.EqualTo(StaticMapChunkRendererSafety.LodGroup));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SourceEvaluationMapsSafetyAndMeshRulesToStableCategories()
    {
        GameObject source = new GameObject("StaticSource");
        Mesh mesh = null;
        Material material = null;
        try
        {
            MeshFilter meshFilter = source.AddComponent<MeshFilter>();
            MeshRenderer renderer = source.AddComponent<MeshRenderer>();

            StaticMapChunkSourceEvaluation missingMesh = StaticMapChunkBatchingPolicy.EvaluateSource(
                renderer,
                null,
                null,
                null,
                null);
            Assert.That(missingMesh.Eligibility, Is.EqualTo(StaticMapChunkSourceEligibility.MissingMesh));

            mesh = CreateTriangleMesh();
            material = CreateMaterial();
            meshFilter.sharedMesh = mesh;
            renderer.sharedMaterial = material;

            StaticMapChunkSourceEvaluation eligible = StaticMapChunkBatchingPolicy.EvaluateSource(
                renderer,
                null,
                null,
                null,
                null);
            Assert.That(eligible.Eligibility, Is.EqualTo(StaticMapChunkSourceEligibility.Eligible));
            Assert.That(eligible.MeshFilter, Is.SameAs(meshFilter));
            Assert.That(eligible.Mesh, Is.SameAs(mesh));
            Assert.That(eligible.Material, Is.SameAs(material));

            source.AddComponent<Light>();
            StaticMapChunkSourceEvaluation unsafeSource = StaticMapChunkBatchingPolicy.EvaluateSource(
                renderer,
                null,
                null,
                null,
                null);
            Assert.That(unsafeSource.Eligibility, Is.EqualTo(StaticMapChunkSourceEligibility.Unsafe));
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(material);
        }
    }

    private static StaticMapChunkBatchKey CreateKey(
        Material material,
        int chunkX = 3,
        int chunkZ = -1,
        int lightmapIndex = 7,
        int layer = 9,
        ShadowCastingMode shadowCastingMode = ShadowCastingMode.On,
        bool receiveShadows = true,
        LightProbeUsage lightProbeUsage = LightProbeUsage.BlendProbes,
        ReflectionProbeUsage reflectionProbeUsage = ReflectionProbeUsage.BlendProbes)
    {
        return new StaticMapChunkBatchKey(
            chunkX,
            chunkZ,
            material,
            lightmapIndex,
            layer,
            shadowCastingMode,
            receiveShadows,
            lightProbeUsage,
            reflectionProbeUsage);
    }

    private static Material CreateMaterial()
    {
        Shader shader = Shader.Find("Hidden/InternalErrorShader");
        Assert.That(shader, Is.Not.Null, "Unity's internal fallback shader is unavailable.");
        return new Material(shader);
    }

    private static Mesh CreateTriangleMesh()
    {
        var mesh = new Mesh();
        mesh.vertices = new[]
        {
            Vector3.zero,
            Vector3.right,
            Vector3.up
        };
        mesh.triangles = new[] { 0, 1, 2 };
        return mesh;
    }
}
