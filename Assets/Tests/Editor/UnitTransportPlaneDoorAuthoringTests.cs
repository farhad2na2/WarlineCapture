using System;
using Game.Components;
using Game.Editor;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

// Real prefab + real UnitGridAuthoring baker; no production pose or input writes.
public sealed class UnitTransportPlaneDoorAuthoringTests
{
    const string PrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab";
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new UnitTransportPlaneDoorAuthoringTests();
            tests.ActualPrefabNestedDoorUsesUnitRootCoordinates();
            tests.RootAndNestedModelTransformsAreAppliedExactlyOnce();
            tests.DirectChildDoorPreservesExistingRootLocalOffsets();
            Debug.Log("[UnitTransportPlaneDoorAuthoringTests] result=Passed cases=3 scope=IsolatedActualBakingNotNormalMission");
            ValidationExit.Passed();
        }
        catch (Exception error) { Debug.LogException(error); Debug.LogError("[UnitTransportPlaneDoorAuthoringTests] result=Failed"); ValidationExit.Failed(); }
    }

    [Test] public void ActualPrefabNestedDoorUsesUnitRootCoordinates() => Verify(0);
    [Test] public void RootAndNestedModelTransformsAreAppliedExactlyOnce() => Verify(1);
    [Test] public void DirectChildDoorPreservesExistingRootLocalOffsets() => Verify(2);

    static void Verify(int variant)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null, "Actual shared transport-plane prefab missing.");
        GameObject root = UnityEngine.Object.Instantiate(prefab);
        IDisposable bakingLifetime = null;
        using var world = new World("PlaneDoorRootSpaceActualBake" + variant);
        try
        {
            Transform door = null;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == "Door_X") { Assert.That(door, Is.Null, "Ambiguous Door_X."); door = t; }
            Assert.That(door, Is.Not.Null);
            Assert.That(door.parent, Is.Not.EqualTo(root.transform), "Actual prefab must exercise its nested door hierarchy.");
            if (variant == 1)
            {
                root.transform.SetPositionAndRotation(new Vector3(317f, 12f, -409f), Quaternion.Euler(0f, 37f, 0f));
                root.transform.localScale = Vector3.one * 1.7f;
                door.parent.localPosition += new Vector3(2f, 3f, 5f);
                door.parent.localRotation = Quaternion.Euler(11f, 19f, -7f);
                door.parent.localScale = new Vector3(1.3f, .8f, 1.9f);
            }
            else if (variant == 2)
            {
                door.SetParent(root.transform, false);
                door.localPosition = new Vector3(0f, 2f, -9f);
            }
            Vector3 expected = root.transform.InverseTransformPoint(door.position);
            expected.x = 0f; // Existing boarding centerline remains intentional.
            Quaternion expectedOpen = door.localRotation;
            Vector3 expectedClosedEuler = door.localEulerAngles; expectedClosedEuler.x = 0f;
            Quaternion expectedClosed = Quaternion.Euler(expectedClosedEuler);
            MapVariantPreparationTests.Bake(world, root, out bakingLifetime);
            using var query = world.EntityManager.CreateEntityQuery(typeof(UnitTransportPlaneDoorReference));
            Assert.That(query.CalculateEntityCount(), Is.EqualTo(1));
            var actual = query.GetSingleton<UnitTransportPlaneDoorReference>();
            Near(actual.DoorLocalPosition, expected, "door root frame");
            Near(actual.ApproachLocalPosition, expected + new Vector3(0f, 0f, -6f), "approach root frame");
            Near(actual.RolloutLocalPosition, expected + new Vector3(0f, 0f, -6f), "rollout root frame");
            Near(actual.InteriorLocalPosition, expected + new Vector3(0f, 1.45f, 9.5f), "interior root frame");
            Assert.That(math.abs(math.dot(actual.OpenLocalRotation.value, new float4(expectedOpen.x, expectedOpen.y, expectedOpen.z, expectedOpen.w))), Is.EqualTo(1f).Within(.0001f), "Animation remains in actual door parent-local frame.");
            Assert.That(math.abs(math.dot(actual.ClosedLocalRotation.value, new float4(expectedClosed.x, expectedClosed.y, expectedClosed.z, expectedClosed.w))), Is.EqualTo(1f).Within(.0001f));
            Assert.That(actual.OpenSeconds, Is.EqualTo(1.1f)); Assert.That(actual.CloseSeconds, Is.EqualTo(.9f));
            Assert.That(world.EntityManager.Exists(actual.DoorEntity), Is.True);
        }
        finally { bakingLifetime?.Dispose(); UnityEngine.Object.DestroyImmediate(root); }
    }

    static void Near(float3 actual, Vector3 expected, string reason) => Assert.That(math.distance(actual, new float3(expected.x, expected.y, expected.z)), Is.LessThan(.0002f), reason);
}
