using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingVisualSystemTests
{
    private GameObject _root;
    private World _world;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
        _root = null;
        if (_world != null && _world.IsCreated)
            _world.Dispose();
        _world = null;
    }

    [Test]
    public void FindDescendantByName_FindsNestedChild()
    {
        _root = new GameObject("Root");
        GameObject branch = new("Branch");
        GameObject target = new("Target");
        branch.transform.SetParent(_root.transform);
        target.transform.SetParent(branch.transform);

        BuildingVisualSystem system = CreateSystem();
        Assert.AreSame(target.transform, system.FindDescendantByName(_root.transform, "Target"));
    }

    [Test]
    public void SetTransformVisible_TogglesGameObjectActiveState()
    {
        _root = new GameObject("Root");

        BuildingVisualSystem system = CreateSystem();
        system.SetTransformVisible(_root.transform, false);
        Assert.IsFalse(_root.activeSelf);

        system.SetTransformVisible(_root.transform, true);
        Assert.IsTrue(_root.activeSelf);
    }

    [Test]
    public void AnimatedBuildingParts_AreDiscoveredAndUpdatedByNameContract()
    {
        _root = new GameObject("Root");
        GameObject animated = new("Pump_Y_30");
        animated.transform.SetParent(_root.transform);

        BuildingVisualSystem system = CreateSystem();
        BuildingVisualSystem.AnimatedPart[] parts = system.FindAnimatedBuildingParts(_root.transform);

        Assert.IsNotNull(parts);
        Assert.AreEqual(1, parts.Length);
        Assert.AreSame(animated.transform, parts[0].Transform);
        Assert.AreEqual(Vector3.up, parts[0].Axis);
        Assert.AreEqual(30f, parts[0].AngleLimit);

        system.UpdateAnimatedBuildingParts(parts, true, 1f);
        Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(0f, animated.transform.localEulerAngles.y)), 0.01f);

        system.UpdateAnimatedBuildingParts(parts, false, 1f);
        Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0f, animated.transform.localEulerAngles.y)), 0.01f);
    }

    private BuildingVisualSystem CreateSystem()
    {
        _world ??= new World(nameof(BuildingVisualSystemTests));
        return _world.GetOrCreateSystemManaged<BuildingVisualSystem>();
    }
}
#endif
