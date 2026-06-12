#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class CitizenVisibleUnitSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new CitizenVisibleUnitSystemTests();
            tests.RemoveVisibleCitizenDestroysEntityAndClearsState();
            tests.ClearVisibleCitizensDestroysAllEntitiesAndClearsState();
            Debug.Log("[CitizenVisibleUnitFocusedValidation] result=Passed tests=2");
            EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[CitizenVisibleUnitFocusedValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void RemoveVisibleCitizenDestroysEntityAndClearsState()
    {
        using World world = new("CitizenVisibleUnitSystemTests");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity entity = em.CreateEntity(typeof(UnitGrid));
            var state = new CitizenPopulationStateSystem();
            var projection = new CitizenPopulationEcsProjectionSystem();
            projection.ResolveEntityManager();
            state.VisibleCitizensById[7] = new VisibleCitizenComponent
            {
                CitizenId = 7,
                UnitEntity = entity,
                GoalCell = new int2(1, 2),
                TargetBuildingId = 3
            };

            new CitizenVisibleUnitSystem().RemoveVisibleCitizen(state, projection, 7);

            Assert.IsFalse(em.Exists(entity));
            Assert.IsFalse(state.VisibleCitizensById.ContainsKey(7));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void ClearVisibleCitizensDestroysAllEntitiesAndClearsState()
    {
        using World world = new("CitizenVisibleUnitSystemTests");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity first = em.CreateEntity(typeof(UnitGrid));
            Entity second = em.CreateEntity(typeof(UnitGrid));
            var state = new CitizenPopulationStateSystem();
            var projection = new CitizenPopulationEcsProjectionSystem();
            projection.ResolveEntityManager();
            state.VisibleCitizensById[11] = new VisibleCitizenComponent
            {
                CitizenId = 11,
                UnitEntity = first,
                GoalCell = new int2(4, 5),
                TargetBuildingId = 6
            };
            state.VisibleCitizensById[12] = new VisibleCitizenComponent
            {
                CitizenId = 12,
                UnitEntity = second,
                GoalCell = new int2(7, 8),
                TargetBuildingId = 9
            };

            new CitizenVisibleUnitSystem().ClearVisibleCitizens(state, projection);

            Assert.IsFalse(em.Exists(first));
            Assert.IsFalse(em.Exists(second));
            Assert.AreEqual(0, state.VisibleCitizensById.Count);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }
}
#endif
