using System;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DenseCityPhysicsComponentStripperTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCityPhysicsStripperTemp";
    private const string PrefabPath = TempRoot + "/physics.prefab";

    public static void RunFocusedValidation()
    {
        var suite = new DenseCityPhysicsComponentStripperTests();
        Action[] tests =
        {
            suite.StripInstanceHierarchy_RemovesActiveAndInactivePhysicsWithoutMutatingPrefab,
            suite.StripInstanceHierarchy_RejectsPersistentPrefabAsset,
            suite.StripInstanceHierarchy_RemovesPrimitiveCollider,
            suite.StripInstanceHierarchy_RecordsRemovedComponentOverrides
        };

        for (int index = 0; index < tests.Length; index++)
        {
            suite.SetUp();
            try
            {
                tests[index]();
            }
            finally
            {
                suite.TearDown();
            }
        }

        Debug.Log($"[DenseCityPhysicsStripperValidation] result=Passed tests={tests.Length}");
    }

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(TempRoot);
        AssetDatabase.CreateFolder("Assets/Tests/Editor", "DenseCityPhysicsStripperTemp");
    }

    [TearDown]
    public void TearDown() => AssetDatabase.DeleteAsset(TempRoot);

    [Test]
    public void StripInstanceHierarchy_RemovesActiveAndInactivePhysicsWithoutMutatingPrefab()
    {
        GameObject prefab = CreatePhysicsPrefab();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            DenseCityPhysicsStripResult result =
                DenseCityPhysicsComponentStripper.StripInstanceHierarchy(instance);

            Assert.That(result.Colliders3D, Is.EqualTo(1));
            Assert.That(result.Colliders2D, Is.EqualTo(1));
            Assert.That(result.Rigidbodies3D, Is.EqualTo(1));
            Assert.That(result.Rigidbodies2D, Is.EqualTo(1));
            Assert.That(result.Total, Is.EqualTo(4));
            Assert.That(instance.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(instance.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(instance.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            Assert.That(instance.GetComponentsInChildren<Rigidbody2D>(true), Is.Empty);

            GameObject persisted = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(persisted.GetComponentsInChildren<Collider>(true), Has.Length.EqualTo(1));
            Assert.That(persisted.GetComponentsInChildren<Collider2D>(true), Has.Length.EqualTo(1));
            Assert.That(persisted.GetComponentsInChildren<Rigidbody>(true), Has.Length.EqualTo(1));
            Assert.That(persisted.GetComponentsInChildren<Rigidbody2D>(true), Has.Length.EqualTo(1));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void StripInstanceHierarchy_RejectsPersistentPrefabAsset()
    {
        GameObject prefab = CreatePhysicsPrefab();

        Assert.That(
            () => DenseCityPhysicsComponentStripper.StripInstanceHierarchy(prefab),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("instance-only"));
        Assert.That(prefab.GetComponentsInChildren<Collider>(true), Has.Length.EqualTo(1));
    }

    [Test]
    public void StripInstanceHierarchy_RemovesPrimitiveCollider()
    {
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            DenseCityPhysicsStripResult result =
                DenseCityPhysicsComponentStripper.StripInstanceHierarchy(primitive);

            Assert.That(result.Colliders3D, Is.EqualTo(1));
            Assert.That(result.Total, Is.EqualTo(1));
            Assert.That(primitive.GetComponent<Collider>(), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(primitive);
        }
    }

    [Test]
    public void StripInstanceHierarchy_RecordsRemovedComponentOverrides()
    {
        GameObject prefab = CreatePhysicsPrefab();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            DenseCityPhysicsComponentStripper.StripInstanceHierarchy(instance);

            Assert.That(
                PrefabUtility.GetRemovedComponents(instance),
                Has.Count.EqualTo(4));
            Assert.That(
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance),
                Is.EqualTo(PrefabPath));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static GameObject CreatePhysicsPrefab()
    {
        var source = new GameObject("PhysicsSource");
        source.AddComponent<BoxCollider>();
        source.AddComponent<Rigidbody>();
        var inactiveChild = new GameObject("InactivePhysics");
        inactiveChild.transform.SetParent(source.transform, false);
        inactiveChild.AddComponent<BoxCollider2D>();
        inactiveChild.AddComponent<Rigidbody2D>();
        inactiveChild.SetActive(false);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
        UnityEngine.Object.DestroyImmediate(source);
        return prefab;
    }
}
