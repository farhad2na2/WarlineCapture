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
            var projection = new CitizenPopulationEcsProjectionSystem();
            projection.ResolveEntityManager();

            new CitizenMovementCommandSystem().IssueCitizenMoveCommand(
                projection,
                entity,
                new int2(12, 34));

            Assert.IsFalse(em.HasComponent<EngageTarget>(entity));
            Assert.IsFalse(em.HasComponent<UnitPathFollow>(entity));
            Assert.IsFalse(em.HasComponent<UnitPathRange>(entity));
            Assert.IsFalse(em.HasComponent<AutoWanderMoveTag>(entity));
            Assert.AreEqual(new int2(12, 34), em.GetComponentData<UnitTarget>(entity).Cell);
            Assert.AreEqual(new int2(12, 34), em.GetComponentData<UnitPathRequest>(entity).Goal);
            Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(entity));
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
            var projection = new CitizenPopulationEcsProjectionSystem();
            projection.ResolveEntityManager();

            new CitizenMovementCommandSystem().IssueCitizenMoveCommand(
                projection,
                entity,
                new int2(56, 78));

            Assert.AreEqual(new int2(56, 78), em.GetComponentData<UnitTarget>(entity).Cell);
            Assert.IsFalse(em.HasComponent<UnitPathRequest>(entity));
            Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(entity));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }
}
#endif
