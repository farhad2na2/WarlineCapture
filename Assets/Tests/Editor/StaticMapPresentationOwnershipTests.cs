using System.Collections.Generic;
using Game.Rendering;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public sealed class StaticMapPresentationOwnershipTests
{
    private readonly List<Object> _objects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = _objects.Count - 1; i >= 0; i--)
        {
            if (_objects[i] != null)
                Object.DestroyImmediate(_objects[i]);
        }
        _objects.Clear();
    }

    [Test]
    public void ValidManifest_DisablesOnlyOwnedRendererWithoutRuntimeBatches()
    {
        Transform root = CreateRoot();
        MeshRenderer owned = CreateRenderer(root, "Owned");
        MeshRenderer fallback = CreateRenderer(root, "Fallback");
        StaticMapPresentationManifest manifest = CreateManifest(root, owned);
        StaticMapPresentationOwnership ownership = new();

        ownership.Initialize(RuntimePlatform.Android, manifest, root, null, null, null);

        Assert.That(ownership.UsingPresentation, Is.True);
        Assert.That(ownership.UsingLegacyFallback, Is.False);
        Assert.That(ownership.SuppressedRendererCount, Is.EqualTo(1));
        Assert.That(owned.enabled, Is.False);
        Assert.That(fallback.enabled, Is.True);
        Assert.That(root.Find("RuntimeStaticMapBatches"), Is.Null);
    }

    [Test]
    public void Dispose_RestoresCanonicalRendererOwnership()
    {
        Transform root = CreateRoot();
        MeshRenderer renderer = CreateRenderer(root, "Owned");
        StaticMapPresentationManifest manifest = CreateManifest(root, renderer);
        StaticMapPresentationOwnership ownership = new();

        ownership.Initialize(RuntimePlatform.Android, manifest, root, null, null, null);
        Assert.That(renderer.enabled, Is.False);

        ownership.Dispose();

        Assert.That(renderer.enabled, Is.True);
        Assert.That(ownership.UsingPresentation, Is.False);
        Assert.That(ownership.UsingLegacyFallback, Is.False);
        Assert.That(ownership.SuppressedRendererCount, Is.Zero);
    }

    [Test]
    public void FailedReinitialize_RestoresCanonicalRendererBeforeLegacyFallback()
    {
        Transform root = CreateRoot();
        MeshRenderer renderer = CreateRenderer(root, "Owned");
        StaticMapPresentationManifest validManifest = CreateManifest(root, renderer);
        StaticMapPresentationManifest invalidManifest = CreateManifest(
            root,
            renderer,
            AdditionalSource("Missing[99]", renderer));
        StaticMapPresentationOwnership ownership = new();

        ownership.Initialize(RuntimePlatform.Android, validManifest, root, null, null, null);
        Assert.That(renderer.enabled, Is.False);

        ownership.Initialize(RuntimePlatform.Android, invalidManifest, root, null, null, null);

        Assert.That(renderer.enabled, Is.True);
        Assert.That(ownership.UsingPresentation, Is.False);
        Assert.That(ownership.UsingLegacyFallback, Is.True);
        Assert.That(ownership.Failure, Does.Contain("canonical renderer"));
    }

    [Test]
    public void InvalidManifest_IsAllOrNothingAndUsesLegacyFallback()
    {
        Transform root = CreateRoot();
        MeshRenderer renderer = CreateRenderer(root, "Owned");
        StaticMapPresentationManifest manifest = CreateManifest(
            root,
            renderer,
            AdditionalSource("Missing[99]", renderer));
        StaticMapPresentationOwnership ownership = new();

        ownership.Initialize(RuntimePlatform.Android, manifest, root, null, null, null);

        Assert.That(ownership.UsingPresentation, Is.False);
        Assert.That(ownership.UsingLegacyFallback, Is.True);
        Assert.That(ownership.Failure, Does.Contain("canonical renderer"));
        Assert.That(renderer.enabled, Is.True);
    }

    [Test]
    public void NullSource_UsesLegacyFallbackWithoutThrowing()
    {
        Transform root = CreateRoot();
        MeshRenderer renderer = CreateRenderer(root, "Owned");
        StaticMapPresentationManifest manifest = CreateManifest(root, renderer);
        manifest.EditorSetData(
            "Assets/Match.unity",
            "dependency",
            32f,
            "content",
            new List<StaticMapPresentationChunkEntry>
            {
                new("chunk", "Assets/Chunk.unity", renderer.bounds, 0, 1)
            },
            new List<StaticMapPresentationSourceEntry> { null });
        StaticMapPresentationOwnership ownership = new();

        Assert.DoesNotThrow(() =>
            ownership.Initialize(RuntimePlatform.Android, manifest, root, null, null, null));

        Assert.That(ownership.UsingLegacyFallback, Is.True);
        Assert.That(ownership.Failure, Does.Contain("source 0 is invalid"));
        Assert.That(renderer.enabled, Is.True);
    }

    [Test]
    public void NullMaterialEntry_UsesLegacyFallbackWithoutThrowing()
    {
        Transform root = CreateRoot();
        MeshRenderer renderer = CreateRenderer(root, "Owned");
        StaticMapPresentationSourceEntry source = new(
            "source-id",
            Path(root, renderer.transform),
            "hash",
            "chunk",
            renderer.name,
            renderer.bounds,
            renderer.GetComponent<MeshFilter>().sharedMesh,
            "mesh-guid",
            1,
            new List<StaticMapPresentationMaterialEntry> { null },
            false);
        StaticMapPresentationManifest manifest = CreateManifest(root, renderer);
        manifest.EditorSetData(
            "Assets/Match.unity",
            "dependency",
            32f,
            "content",
            new List<StaticMapPresentationChunkEntry>
            {
                new("chunk", "Assets/Chunk.unity", renderer.bounds, 0, 1)
            },
            new List<StaticMapPresentationSourceEntry> { source });
        StaticMapPresentationOwnership ownership = new();

        Assert.DoesNotThrow(() =>
            ownership.Initialize(RuntimePlatform.Android, manifest, root, null, null, null));

        Assert.That(ownership.UsingLegacyFallback, Is.True);
        Assert.That(ownership.Failure, Does.Contain("material 0 is invalid"));
        Assert.That(renderer.enabled, Is.True);
    }

    [Test]
    public void SiblingIndexedPath_ResolvesDuplicateNamesExactly()
    {
        Transform root = CreateRoot();
        MeshRenderer first = CreateRenderer(root, "Duplicate");
        MeshRenderer second = CreateRenderer(root, "Duplicate");
        StaticMapPresentationManifest manifest = CreateManifest(root, second);
        StaticMapPresentationOwnership ownership = new();

        ownership.Initialize(RuntimePlatform.Android, manifest, root, null, null, null);

        Assert.That(ownership.UsingPresentation, Is.True);
        Assert.That(first.enabled, Is.True);
        Assert.That(second.enabled, Is.False);
    }

    [Test]
    public void PlayerSiblingIndexDrift_ResolvesByRendererIdentity()
    {
        Transform root = CreateRoot();
        MeshRenderer renderer = CreateRenderer(root, "Owned");
        StaticMapPresentationManifest manifest = CreateManifest(root, renderer);
        GameObject inserted = new("PlayerOnlySibling");
        _objects.Add(inserted);
        inserted.transform.SetParent(root, false);
        inserted.transform.SetSiblingIndex(0);
        StaticMapPresentationOwnership ownership = new();

        ownership.Initialize(RuntimePlatform.Android, manifest, root, null, null, null);

        Assert.That(ownership.UsingPresentation, Is.True);
        Assert.That(ownership.UsingLegacyFallback, Is.False);
        Assert.That(renderer.enabled, Is.False);
    }

    [Test]
    public void ContentIdenticalMaterialInstance_UsesPresentationOwnership()
    {
        Transform root = CreateRoot();
        MeshRenderer renderer = CreateRenderer(root, "Owned");
        StaticMapPresentationManifest manifest = CreateManifest(root, renderer);
        Material clonedMaterial = new(renderer.sharedMaterial);
        _objects.Add(clonedMaterial);
        renderer.sharedMaterial = clonedMaterial;
        StaticMapPresentationOwnership ownership = new();

        ownership.Initialize(RuntimePlatform.Android, manifest, root, null, null, null);

        Assert.That(ownership.UsingPresentation, Is.True);
        Assert.That(ownership.UsingLegacyFallback, Is.False);
        Assert.That(renderer.enabled, Is.False);
    }

    [Test]
    public void ChangedMaterialInstance_UsesLegacyFallback()
    {
        Transform root = CreateRoot();
        MeshRenderer renderer = CreateRenderer(root, "Owned");
        StaticMapPresentationManifest manifest = CreateManifest(root, renderer);
        Material changedMaterial = new(renderer.sharedMaterial);
        changedMaterial.SetColor("_BaseColor", Color.red);
        _objects.Add(changedMaterial);
        renderer.sharedMaterial = changedMaterial;
        StaticMapPresentationOwnership ownership = new();

        ownership.Initialize(RuntimePlatform.Android, manifest, root, null, null, null);

        Assert.That(ownership.UsingPresentation, Is.False);
        Assert.That(ownership.UsingLegacyFallback, Is.True);
        Assert.That(ownership.Failure, Does.Contain("material-0"));
        Assert.That(renderer.enabled, Is.True);
    }

    [Test]
    public void PresentationSuppression_PreservesMeshFilterAndOverlayGeometry()
    {
        Transform root = CreateRoot();
        MeshRenderer renderer = CreateRenderer(root, "Overlay");
        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        Mesh mesh = filter.sharedMesh;
        StaticMapPresentationManifest manifest = CreateManifest(root, renderer, overlaySource: true);
        StaticMapPresentationOwnership ownership = new();

        ownership.Initialize(RuntimePlatform.Android, manifest, root, null, null, null);

        Assert.That(renderer.enabled, Is.False);
        Assert.That(renderer.GetComponent<MeshFilter>(), Is.SameAs(filter));
        Assert.That(filter.sharedMesh, Is.SameAs(mesh));
        Assert.That(renderer.gameObject.activeSelf, Is.True);
    }

    [Test]
    public void Dispose_RestoresEveryOriginalRendererState()
    {
        Transform root = CreateRoot();
        MeshRenderer renderer = CreateRenderer(root, "Owned");
        StaticMapPresentationManifest manifest = CreateManifest(root, renderer);
        StaticMapPresentationOwnership ownership = new();
        ownership.Initialize(RuntimePlatform.Android, manifest, root, null, null, null);

        ownership.Dispose();

        Assert.That(renderer.enabled, Is.True);
        Assert.That(ownership.SuppressedRendererCount, Is.Zero);
        Assert.That(ownership.UsingPresentation, Is.False);
    }

    [Test]
    public void NonAndroid_KeepsLegacyFallback()
    {
        Transform root = CreateRoot();
        StaticMapPresentationOwnership ownership = new();

        ownership.Initialize(RuntimePlatform.OSXEditor, null, root, null, null, null);

        Assert.That(ownership.UsingPresentation, Is.False);
        Assert.That(ownership.UsingLegacyFallback, Is.True);
        Assert.That(ownership.Failure, Is.Null);
    }

    private Transform CreateRoot()
    {
        GameObject root = new("Map");
        _objects.Add(root);
        return root.transform;
    }

    private MeshRenderer CreateRenderer(Transform parent, string name)
    {
        GameObject source = GameObject.CreatePrimitive(PrimitiveType.Cube);
        source.name = name;
        source.transform.SetParent(parent, false);
        Object.DestroyImmediate(source.GetComponent<Collider>());
        MeshRenderer renderer = source.GetComponent<MeshRenderer>();
        Material material = new(Shader.Find("Universal Render Pipeline/Lit"));
        _objects.Add(material);
        renderer.sharedMaterial = material;
        return renderer;
    }

    private StaticMapPresentationManifest CreateManifest(
        Transform root,
        MeshRenderer renderer,
        StaticMapPresentationSourceEntry additionalSource = null,
        bool overlaySource = false)
    {
        List<StaticMapPresentationSourceEntry> sources = new()
        {
            CreateSource(root, renderer, overlaySource)
        };
        if (additionalSource != null)
            sources.Add(additionalSource);
        StaticMapPresentationManifest manifest = ScriptableObject.CreateInstance<StaticMapPresentationManifest>();
        _objects.Add(manifest);
        manifest.EditorSetData(
            "Assets/Match.unity",
            "dependency",
            32f,
            "content",
            new List<StaticMapPresentationChunkEntry>
            {
                new("chunk", "Assets/Chunk.unity", renderer.bounds, 0, sources.Count)
            },
            sources);
        return manifest;
    }

    private StaticMapPresentationSourceEntry CreateSource(
        Transform root,
        MeshRenderer renderer,
        bool overlaySource)
    {
        Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
        return new StaticMapPresentationSourceEntry(
            "source-id",
            Path(root, renderer.transform),
            "hash",
            "chunk",
            renderer.name,
            renderer.bounds,
            mesh,
            "mesh-guid",
            1,
            Materials(renderer),
            overlaySource);
    }

    private StaticMapPresentationSourceEntry AdditionalSource(
        string path,
        MeshRenderer renderer)
    {
        Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
        return new StaticMapPresentationSourceEntry(
            "missing-id",
            path,
            "hash",
            "chunk",
            "Missing",
            renderer.bounds,
            mesh,
            "mesh-guid",
            1,
            Materials(renderer),
            false);
    }

    private static List<StaticMapPresentationMaterialEntry> Materials(MeshRenderer renderer)
    {
        return new List<StaticMapPresentationMaterialEntry>
        {
            new(renderer.sharedMaterial, "material-guid", 1)
        };
    }

    private static string Path(Transform root, Transform renderer)
    {
        return $"{root.name}[{root.GetSiblingIndex()}]/{renderer.name}[{renderer.GetSiblingIndex()}]";
    }
}
