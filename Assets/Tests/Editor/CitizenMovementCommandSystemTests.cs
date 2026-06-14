#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class CitizenMovementCommandSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new CitizenMovementCommandSystemTests();
            tests.GroundCitizenMoveCommandClearsCurrentOrderAndRequestsPath();
            tests.AirCitizenMoveCommandClearsGroundPathRequest();
            Debug.Log("[CitizenMovementCommandFocusedValidation] result=Passed tests=2");
            EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[CitizenMovementCommandFocusedValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void GroundCitizenMoveCommandClearsCurrentOrderAndRequestsPath()
    {
        using World world = new("CitizenMovementCommandSystemTests");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity entity = em.CreateEntity(
                typeof(EngageTarget),
                typeof(UnitPathFollow),
                typeof(UnitPathRange),
                typeof(AutoWanderMoveTag),
                typeof(UnitTarget),
                typeof(UnitPathRequest));
            SystemHandle movementSystem = world.CreateSystem<CitizenMovementCommandSystem>();

            Assert.IsTrue(CitizenMovementCommandSystem.TryEnqueueMoveCommand(em, entity, new int2(12, 34)));
            movementSystem.Update(world.Unmanaged);

            Assert.IsFalse(em.HasComponent<EngageTarget>(entity));
            Assert.IsFalse(em.HasComponent<UnitPathFollow>(entity));
            Assert.IsFalse(em.HasComponent<UnitPathRange>(entity));
            Assert.IsFalse(em.HasComponent<AutoWanderMoveTag>(entity));
            Assert.AreEqual(new int2(12, 34), em.GetComponentData<UnitTarget>(entity).Cell);
            Assert.AreEqual(new int2(12, 34), em.GetComponentData<UnitPathRequest>(entity).Goal);
            Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(entity));
            AssertAcceptedResult(em, entity, new int2(12, 34));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void AirCitizenMoveCommandClearsGroundPathRequest()
    {
        using World world = new("CitizenMovementCommandSystemTests");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity entity = em.CreateEntity(
                typeof(UnitAirMovement),
                typeof(UnitTarget),
                typeof(UnitPathRequest));
            SystemHandle movementSystem = world.CreateSystem<CitizenMovementCommandSystem>();

            Assert.IsTrue(CitizenMovementCommandSystem.TryEnqueueMoveCommand(em, entity, new int2(56, 78)));
            movementSystem.Update(world.Unmanaged);

            Assert.AreEqual(new int2(56, 78), em.GetComponentData<UnitTarget>(entity).Cell);
            Assert.IsFalse(em.HasComponent<UnitPathRequest>(entity));
            Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(entity));
            AssertAcceptedResult(em, entity, new int2(56, 78));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    private static void AssertAcceptedResult(EntityManager em, Entity entity, int2 goal)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<CitizenMovementCommandQueueComponent>(),
            ComponentType.ReadOnly<CitizenMoveCommandResultElement>());
        Entity queueEntity = query.GetSingletonEntity();
        DynamicBuffer<CitizenMoveCommandResultElement> results = em.GetBuffer<CitizenMoveCommandResultElement>(queueEntity);

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(entity, results[0].UnitEntity);
        Assert.AreEqual(goal, results[0].Goal);
        Assert.AreEqual(1, results[0].Accepted);
    }
}
#endif
