#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SelectedUnitOrderSnapshotSystemTests
{
    private World _world;
    private EntityManager _entityManager;

    [SetUp]
    public void SetUp()
    {
        _world = new World("SelectedUnitOrderSnapshotSystemTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        if (_world != null && _world.IsCreated)
            _world.Dispose();
    }

    [Test]
    public void RestorePreservedUnitOrders_RestoresExistingComponentsAndRemovesNewOnes()
    {
        SelectedUnitOrderSnapshotSystem system = new();
        Entity target = _entityManager.CreateEntity();
        Entity selectedWithOrders = _entityManager.CreateEntity(typeof(SelectedUnitTag));
        Entity selectedWithoutOrders = _entityManager.CreateEntity(typeof(SelectedUnitTag));

        EngageTarget originalEngage = new()
        {
            Target = target,
            Cell = new int2(3, 4),
            Position = new float3(3f, 0f, 4f),
            IsCommanded = 1
        };
        UnitTarget originalTarget = new() { Cell = new int2(5, 6) };
        UnitPathRequest originalRequest = new() { Goal = new int2(7, 8) };
        UnitPathFollow originalFollow = new() { PathIndex = 9 };
        UnitPathRange originalRange = new() { Start = 10, Length = 11 };
        _entityManager.AddComponentData(selectedWithOrders, originalEngage);
        _entityManager.AddComponentData(selectedWithOrders, originalTarget);
        _entityManager.AddComponentData(selectedWithOrders, originalRequest);
        _entityManager.AddComponentData(selectedWithOrders, originalFollow);
        _entityManager.AddComponentData(selectedWithOrders, originalRange);

        system.PreserveSelectedUnitOrders(_entityManager);

        _entityManager.RemoveComponent<EngageTarget>(selectedWithOrders);
        _entityManager.SetComponentData(selectedWithOrders, new UnitTarget { Cell = new int2(50, 60) });
        _entityManager.RemoveComponent<UnitPathRequest>(selectedWithOrders);
        _entityManager.SetComponentData(selectedWithOrders, new UnitPathFollow { PathIndex = 90 });
        _entityManager.RemoveComponent<UnitPathRange>(selectedWithOrders);
        _entityManager.AddComponentData(selectedWithoutOrders, new UnitPathRequest { Goal = new int2(12, 13) });

        system.RestorePreservedUnitOrders(_entityManager);

        Assert.IsTrue(_entityManager.HasComponent<EngageTarget>(selectedWithOrders));
        Assert.AreEqual(originalEngage.Target, _entityManager.GetComponentData<EngageTarget>(selectedWithOrders).Target);
        Assert.AreEqual(originalEngage.Cell, _entityManager.GetComponentData<EngageTarget>(selectedWithOrders).Cell);
        Assert.AreEqual(originalTarget.Cell, _entityManager.GetComponentData<UnitTarget>(selectedWithOrders).Cell);
        Assert.AreEqual(originalRequest.Goal, _entityManager.GetComponentData<UnitPathRequest>(selectedWithOrders).Goal);
        Assert.AreEqual(originalFollow.PathIndex, _entityManager.GetComponentData<UnitPathFollow>(selectedWithOrders).PathIndex);
        Assert.AreEqual(originalRange.Start, _entityManager.GetComponentData<UnitPathRange>(selectedWithOrders).Start);
        Assert.AreEqual(originalRange.Length, _entityManager.GetComponentData<UnitPathRange>(selectedWithOrders).Length);
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(selectedWithoutOrders));
    }

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.RestorePreservedUnitOrders_RestoresExistingComponentsAndRemovesNewOnes());
            Debug.Log("[SelectedUnitOrderSnapshotFocusedValidation] result=Passed tests=1");
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[SelectedUnitOrderSnapshotFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunCase(System.Action<SelectedUnitOrderSnapshotSystemTests> testCase)
    {
        SelectedUnitOrderSnapshotSystemTests tests = new();
        try
        {
            tests.SetUp();
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
        }
    }
}
#endif
