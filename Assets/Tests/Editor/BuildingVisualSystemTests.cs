using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingVisualSystemTests
{
    private const string LegacyOilPumpArtPrefabPath = "Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipline_OilPump_01.prefab";

    private GameObject _root;
    private World _world;

    public static void RunFocusedValidation()
    {
        var tests = new BuildingVisualSystemTests();
        int testCount = 0;
        RunTest(tests, tests.FindDescendantByName_FindsNestedChild, ref testCount);
        RunTest(tests, tests.SetTransformVisible_TogglesGameObjectActiveState, ref testCount);
        RunTest(tests, tests.AnimatedBuildingParts_AreDiscoveredAndUpdatedByNameContract, ref testCount);
        RunTest(tests, tests.LegacyOilPumpParts_AreDiscoveredAndUpdatedForMapAuthoredVisuals, ref testCount);
        RunTest(tests, tests.LegacyOilPumpPrefabAsset_UsesMapAuthoredAnimationFallback, ref testCount);
        Debug.Log($"[BuildingVisualSystemFocusedValidation] result=Passed tests={testCount}");
    }

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
        Assert.IsFalse(parts[0].ContinuousRotation);

        system.UpdateAnimatedBuildingParts(parts, true, 1f);
        Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(0f, animated.transform.localEulerAngles.y)), 0.01f);

        system.UpdateAnimatedBuildingParts(parts, false, 1f);
        Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0f, animated.transform.localEulerAngles.y)), 0.01f);
    }

    [Test]
    public void LegacyOilPumpParts_AreDiscoveredAndUpdatedForMapAuthoredVisuals()
    {
        _root = new GameObject("SM_Prop_Pipline_OilPump_01");
        GameObject arm = new("SM_Prop_Pipline_OilPump_Arm_01");
        GameObject armTop = new("SM_Prop_Pipline_Arm_Top_01");
        GameObject wheel = new("SM_Prop_Pipline_Wheel_01");
        arm.transform.SetParent(_root.transform, false);
        armTop.transform.SetParent(arm.transform, false);
        wheel.transform.SetParent(_root.transform, false);

        BuildingVisualSystem system = CreateSystem();
        BuildingVisualSystem.AnimatedPart[] parts = system.FindAnimatedBuildingParts(_root.transform);

        Assert.IsNotNull(parts);
        Assert.AreEqual(2, parts.Length);
        Assert.AreSame(arm.transform, parts[0].Transform);
        Assert.AreEqual(Vector3.right, parts[0].Axis);
        Assert.AreEqual(15f, parts[0].AngleLimit);
        Assert.IsFalse(parts[0].ContinuousRotation);
        Assert.AreSame(wheel.transform, parts[1].Transform);
        Assert.AreEqual(Vector3.right, parts[1].Axis);
        Assert.AreEqual(365f, parts[1].AngleLimit);
        Assert.IsTrue(parts[1].ContinuousRotation);

        system.UpdateAnimatedBuildingParts(parts, true, 1f);
        Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(0f, arm.transform.localEulerAngles.x)), 0.01f);
        Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(0f, wheel.transform.localEulerAngles.x)), 0.01f);

        system.UpdateAnimatedBuildingParts(parts, false, 1f);
        Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0f, arm.transform.localEulerAngles.x)), 0.01f);
        Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0f, wheel.transform.localEulerAngles.x)), 0.01f);
    }

    [Test]
    public void LegacyOilPumpPrefabAsset_UsesMapAuthoredAnimationFallback()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LegacyOilPumpArtPrefabPath);
        Assert.NotNull(prefab, $"Missing legacy oil pump art prefab at {LegacyOilPumpArtPrefabPath}.");
        _root = Object.Instantiate(prefab);

        BuildingVisualSystem system = CreateSystem();
        BuildingVisualSystem.AnimatedPart[] parts = system.FindAnimatedBuildingParts(_root.transform);

        Assert.IsNotNull(parts);
        Assert.AreEqual(2, parts.Length);
        Assert.AreEqual("SM_Prop_Pipline_OilPump_Arm_01", parts[0].Transform.name);
        Assert.AreEqual("SM_Prop_Pipline_Wheel_01", parts[1].Transform.name);
        Assert.IsFalse(parts[0].ContinuousRotation);
        Assert.IsTrue(parts[1].ContinuousRotation);

        Vector3 armBase = parts[0].Transform.localEulerAngles;
        Vector3 wheelBase = parts[1].Transform.localEulerAngles;
        system.UpdateAnimatedBuildingParts(parts, true, 1f);
        Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(armBase.x, parts[0].Transform.localEulerAngles.x)), 0.01f);
        Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(wheelBase.x, parts[1].Transform.localEulerAngles.x)), 0.01f);
    }

    private BuildingVisualSystem CreateSystem()
    {
        _world ??= new World(nameof(BuildingVisualSystemTests));
        return _world.GetOrCreateSystemManaged<BuildingVisualSystem>();
    }

    private static void RunTest(BuildingVisualSystemTests tests, System.Action action, ref int testCount)
    {
        try
        {
            action();
            testCount++;
        }
        finally
        {
            tests.TearDown();
        }
    }
}
#endif
