#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class BuildingDestroyedVisualSystemTests
{
    private GameObject _root;
    private GameObject _building;
    private GameObject _aliveRoot;
    private GameObject _destroyedPrefab;
    private World _world;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(nameof(BeginDestroyedVisualHidesAliveRootsAndSpawnsConfiguredPrefab),
                test => test.BeginDestroyedVisualHidesAliveRootsAndSpawnsConfiguredPrefab());
            RunCase(nameof(BeginDestroyedVisualReusesExistingInstanceAndCleanupDestroysIt),
                test => test.BeginDestroyedVisualReusesExistingInstanceAndCleanupDestroysIt());
            Debug.Log("[BuildingDestroyedVisualFocusedValidation] result=Passed tests=2");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[BuildingDestroyedVisualFocusedValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    private static void RunCase(string name, Action<BuildingDestroyedVisualSystemTests> action)
    {
        BuildingDestroyedVisualSystemTests tests = new();
        tests.SetUp();
        try
        {
            action(tests);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[BuildingDestroyedVisualFocusedValidation] result=Failed test={name} error={exception}");
            throw;
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("BuildingDestroyedVisualSystemTestsRoot");
        _building = new GameObject("RuntimeBuilding");
        _building.transform.SetParent(_root.transform, false);
        _building.transform.position = new Vector3(4f, 2f, 8f);
        _building.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        _building.transform.localScale = new Vector3(2f, 3f, 4f);

        _aliveRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(_aliveRoot.GetComponent<Collider>());
        _aliveRoot.name = "AliveRoot";
        _aliveRoot.transform.SetParent(_building.transform, false);

        _destroyedPrefab = new GameObject("DestroyedPrefab");
        GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(mesh.GetComponent<Collider>());
        mesh.name = "Destroyed";
        mesh.transform.SetParent(_destroyedPrefab.transform, false);
    }

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
        if (_destroyedPrefab != null)
            Object.DestroyImmediate(_destroyedPrefab);
        if (_world != null && _world.IsCreated)
            _world.Dispose();
        _world = null;
    }

    [Test]
    public void BeginDestroyedVisualHidesAliveRootsAndSpawnsConfiguredPrefab()
    {
        RuntimeBuildingEntity building = CreateBuilding();
        BuildingDestroyedVisualSystem system = CreateBuildingDestroyedVisualSystem();
        var context = new BuildingDestroyedVisualSystem.Context(
            CreateBuildingVisualSystem(),
            Object.DestroyImmediate);

        system.BeginDestroyedVisual(context, building);

        Assert.IsFalse(_aliveRoot.activeSelf);
        Assert.IsNotNull(building.DestroyedVisualInstance);
        Assert.IsTrue(building.DestroyedVisualInstance.activeSelf);
        Assert.AreEqual("RuntimeBuilding_Destroyed", building.DestroyedVisualInstance.name);
        Assert.AreSame(_building.transform, building.DestroyedVisualInstance.transform.parent);
        Assert.That(building.DestroyedVisualInstance.transform.position.x, Is.EqualTo(4f).Within(0.001f));
        Assert.That(building.DestroyedVisualInstance.transform.position.y, Is.EqualTo(2f).Within(0.001f));
        Assert.That(building.DestroyedVisualInstance.transform.position.z, Is.EqualTo(8f).Within(0.001f));
        Assert.That(building.DestroyedVisualInstance.transform.localScale.x, Is.EqualTo(1f).Within(0.001f));
        Assert.That(building.DestroyedVisualInstance.transform.localScale.y, Is.EqualTo(1f).Within(0.001f));
        Assert.That(building.DestroyedVisualInstance.transform.localScale.z, Is.EqualTo(1f).Within(0.001f));
        Assert.That(building.DestroyedVisualInstance.transform.lossyScale.x, Is.EqualTo(2f).Within(0.001f));
        Assert.That(building.DestroyedVisualInstance.transform.lossyScale.y, Is.EqualTo(3f).Within(0.001f));
        Assert.That(building.DestroyedVisualInstance.transform.lossyScale.z, Is.EqualTo(4f).Within(0.001f));
    }

    [Test]
    public void BeginDestroyedVisualReusesExistingInstanceAndCleanupDestroysIt()
    {
        RuntimeBuildingEntity building = CreateBuilding();
        BuildingDestroyedVisualSystem system = CreateBuildingDestroyedVisualSystem();
        var context = new BuildingDestroyedVisualSystem.Context(
            CreateBuildingVisualSystem(),
            Object.DestroyImmediate);

        system.BeginDestroyedVisual(context, building);
        GameObject firstInstance = building.DestroyedVisualInstance;
        firstInstance.SetActive(false);

        system.BeginDestroyedVisual(context, building);

        Assert.AreSame(firstInstance, building.DestroyedVisualInstance);
        Assert.IsTrue(firstInstance.activeSelf);

        system.CleanupDestroyedVisual(context, building);

        Assert.IsNull(building.DestroyedVisualInstance);
        Assert.IsTrue(firstInstance == null);
    }

    private BuildingVisualSystem CreateBuildingVisualSystem()
    {
        _world ??= new World(nameof(BuildingDestroyedVisualSystemTests));
        return _world.GetOrCreateSystemManaged<BuildingVisualSystem>();
    }

    private BuildingDestroyedVisualSystem CreateBuildingDestroyedVisualSystem()
    {
        _world ??= new World(nameof(BuildingDestroyedVisualSystemTests));
        return _world.GetOrCreateSystemManaged<BuildingDestroyedVisualSystem>();
    }

    private RuntimeBuildingEntity CreateBuilding()
    {
        return new RuntimeBuildingEntity
        {
            Instance = _building,
            Definition = new BuildingDefinition { DestroyedVisualPrefab = _destroyedPrefab },
            AliveVisualRoots = new[] { _aliveRoot.transform }
        };
    }
}
#endif
