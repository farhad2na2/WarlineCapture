#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class BuildingFactionVisualSystemTests
{
    private GameObject _root;
    private Material _material;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("BuildingFactionVisualSystemTestsRoot");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        _material = new Material(shader);
        if (_material.HasProperty("_BaseColor"))
            _material.SetColor("_BaseColor", Color.white);
        if (_material.HasProperty("_Color"))
            _material.SetColor("_Color", Color.white);
    }

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
        if (_material != null)
            Object.DestroyImmediate(_material);
    }

    [Test]
    public void CacheBuildingRenderersExcludesDestroyedVisual()
    {
        RuntimeBuildingData building = new();
        Renderer liveRenderer = CreateRenderer("Live", _root.transform);
        GameObject destroyed = new("Destroyed");
        destroyed.transform.SetParent(_root.transform, false);
        Renderer destroyedRenderer = CreateRenderer("DestroyedModel", destroyed.transform);

        var system = new BuildingFactionVisualSystem();
        system.CacheBuildingRenderers(building, _root.transform, destroyed.transform);

        Assert.AreEqual(1, building.FactionVisualRenderers.Length);
        Assert.AreSame(liveRenderer, building.FactionVisualRenderers[0]);
        CollectionAssert.DoesNotContain(building.FactionVisualRenderers, destroyedRenderer);
    }

    [Test]
    public void ApplyOwnerFactionTintsCachedRenderersAndClearRestoresBaseColor()
    {
        Renderer renderer = CreateRenderer("Live", _root.transform);
        RuntimeBuildingData building = new()
        {
            HasOwnerFaction = true,
            OwnerFactionId = 1
        };

        var system = new BuildingFactionVisualSystem();
        MaterialPropertyBlock propertyBlock = new();
        system.CacheBuildingRenderers(building, _root.transform, null);

        system.ApplyOwnerFaction(
            new BuildingFactionVisualSystem.Context(null, propertyBlock, 0.5f),
            building);

        Color tinted = ReadAppliedColor(renderer);
        Assert.That(tinted.r, Is.GreaterThan(0.9f));
        Assert.That(tinted.g, Is.LessThan(0.7f));
        Assert.That(tinted.b, Is.LessThan(0.7f));

        system.Clear(new BuildingFactionVisualSystem.Context(null, propertyBlock, 0.5f), building);

        Color restored = ReadAppliedColor(renderer);
        Assert.That(restored.r, Is.EqualTo(1f).Within(0.001f));
        Assert.That(restored.g, Is.EqualTo(1f).Within(0.001f));
        Assert.That(restored.b, Is.EqualTo(1f).Within(0.001f));
    }

    private Renderer CreateRenderer(string name, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = _material;
        return renderer;
    }

    private static Color ReadAppliedColor(Renderer renderer)
    {
        MaterialPropertyBlock propertyBlock = new();
        renderer.GetPropertyBlock(propertyBlock);
        if (renderer.sharedMaterial.HasProperty("_BaseColor"))
            return propertyBlock.GetColor("_BaseColor");
        return propertyBlock.GetColor("_Color");
    }
}
#endif
