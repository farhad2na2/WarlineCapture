#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class BuildingVisualSystemTests
{
    private GameObject _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
        _root = null;
    }

    [Test]
    public void FindDescendantByName_FindsNestedChild()
    {
        _root = new GameObject("Root");
        GameObject branch = new("Branch");
        GameObject target = new("Target");
        branch.transform.SetParent(_root.transform);
        target.transform.SetParent(branch.transform);

        var system = new BuildingVisualSystem();
        Assert.AreSame(target.transform, system.FindDescendantByName(_root.transform, "Target"));
    }

    [Test]
    public void SetTransformVisible_TogglesGameObjectActiveState()
    {
        _root = new GameObject("Root");

        var system = new BuildingVisualSystem();
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

        var system = new BuildingVisualSystem();
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
}
#endif
