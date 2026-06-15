#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class CitizenVisibleUnitSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new CitizenVisibleUnitSystemTests();
            tests.SpawnVisibleCitizenProjectsPrefabAndQueuesCitizenMovement();
            tests.RemoveVisibleCitizenDestroysEntityAndClearsState();
            tests.ClearVisibleCitizensDestroysAllEntitiesAndClearsState();
            Debug.Log("[CitizenVisibleUnitFocusedValidation] result=Passed tests=3");
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
    public void SpawnVisibleCitizenProjectsPrefabAndQueuesCitizenMovement()
    {
        using World world = new("CitizenVisibleUnitSystemTests");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        GameObject prefab = new("Unit_Chr_Civilian_Male_01");
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = em.CreateEntity(typeof(GridConfig));
            em.SetComponentData(gridEntity, new GridConfig
            {
                Width = 128,
                Height = 128,
                CellSize = 1f,
                Origin = float3.zero
            });

            Entity prefabEntity = em.CreateEntity(
                typeof(Prefab),
                typeof(UnitGrid),
                typeof(LocalTransform),
                typeof(UnitPrevWorldPos),
                typeof(UnitGridInitialized),
                typeof(UnitMovementBehavior),
                typeof(UnitCombat),
                typeof(Faction),
                typeof(UnitTarget),
                typeof(UnitPathRequest),
                typeof(UnitPathFollow),
                typeof(SelectedUnitTag));
            em.SetName(prefabEntity, prefab.name);
            em.SetComponentData(prefabEntity, new UnitMovementBehavior { AllowIdleWander = 1, UsesVehicleMotion = 0 });
            em.SetComponentData(prefabEntity, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
            em.SetComponentData(prefabEntity, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(prefabEntity, new UnitTarget { Cell = new int2(99, 99) });
            em.SetComponentData(prefabEntity, new UnitPathRequest { Goal = new int2(98, 98) });

            Entity registryEntity = em.CreateEntity(typeof(UnitPrefabRegistryTag));
            DynamicBuffer<UnitPrefabRegistryEntry> registry = em.AddBuffer<UnitPrefabRegistryEntry>(registryEntity);
            registry.Add(new UnitPrefabRegistryEntry { Prefab = prefabEntity });

            EntityQuery registryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            EntityQuery prefabCandidatesQuery = em.CreateEntityQuery(ComponentType.ReadOnly<Prefab>());
            EntityQuery liveUnitsQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitRespawnPrefab>(),
                ComponentType.ReadOnly<Faction>());

            var configuredPrefabs = new List<GameObject> { prefab };
            var spawnPrefabSystem = new BuildingSpawnPrefabSystem();
            var spawnPrefabContext = new BuildingSpawnPrefabSystem.Context(
                configuredPrefabs,
                registryQuery,
                prefabCandidatesQuery,
                liveUnitsQuery);
            var citizenPrefabContext = new CitizenPrefabSystem.Context(
                null,
                spawnPrefabSystem,
                TryGetEntityManager,
                null,
                () => spawnPrefabContext);
            var prefabSelectionSystem = new CitizenPrefabSelectionSystem();
            SetCitizenPrefabs(prefabSelectionSystem, prefab);

            var state = new CitizenPopulationStateSystem();
            var projection = new CitizenPopulationEcsProjectionSystem();
            projection.ResolveEntityManager();
            SystemHandle movementSystem = world.CreateSystem<CitizenMovementCommandSystem>();
            var citizen = new CitizenRecordComponent
            {
                CitizenId = 15,
                HouseholdId = 1,
                HomeBuildingId = 2,
                CurrentTargetBuildingId = 3,
                Gender = CitizenGender.Male,
                LifeState = CitizenLifeState.Alive,
                Status = CitizenStatus.GoingToWork
            };

            new CitizenVisibleUnitSystem().SpawnVisibleCitizen(
                state,
                projection,
                new CitizenPrefabSystem(),
                citizenPrefabContext,
                prefabSelectionSystem,
                new CitizenTravelSystem(),
                new CitizenBuildingReadSystem(),
                new CitizenStatusTransitionSystem(),
                citizen,
                new Vector3(4f, 0f, 6f));

            Assert.IsTrue(state.VisibleCitizensById.TryGetValue(15, out VisibleCitizenComponent visibleCitizen));
            movementSystem.Update(world.Unmanaged);
            Assert.AreNotEqual(Entity.Null, visibleCitizen.UnitEntity);
            Assert.AreNotEqual(prefabEntity, visibleCitizen.UnitEntity);
            Assert.IsTrue(em.Exists(visibleCitizen.UnitEntity));
            Assert.AreEqual(new int2(4, 6), visibleCitizen.GoalCell);
            Assert.AreEqual(citizen.CurrentTargetBuildingId, visibleCitizen.TargetBuildingId);
            Assert.AreEqual(new int2(4, 6), em.GetComponentData<UnitGrid>(visibleCitizen.UnitEntity).Cell);
            Assert.AreEqual(new float3(4f, 0f, 6f), em.GetComponentData<LocalTransform>(visibleCitizen.UnitEntity).Position);
            Assert.AreEqual(new float3(4f, 0f, 6f), em.GetComponentData<UnitPrevWorldPos>(visibleCitizen.UnitEntity).Value);
            Assert.IsFalse(em.HasComponent<UnitGridInitialized>(visibleCitizen.UnitEntity));
            Assert.IsFalse(em.HasComponent<UnitPathFollow>(visibleCitizen.UnitEntity));
            Assert.IsFalse(em.HasComponent<SelectedUnitTag>(visibleCitizen.UnitEntity));
            Assert.IsTrue(em.HasComponent<CivilianUnitTag>(visibleCitizen.UnitEntity));
            Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(visibleCitizen.UnitEntity));
            Assert.AreEqual(new int2(4, 6), em.GetComponentData<UnitTarget>(visibleCitizen.UnitEntity).Cell);
            Assert.AreEqual(new int2(4, 6), em.GetComponentData<UnitPathRequest>(visibleCitizen.UnitEntity).Goal);
            Assert.AreEqual(0, em.GetComponentData<UnitMovementBehavior>(visibleCitizen.UnitEntity).AllowIdleWander);
            Assert.AreEqual(0, em.GetComponentData<UnitCombat>(visibleCitizen.UnitEntity).CanAttack);
            Assert.AreEqual(0, em.GetComponentData<UnitCombat>(visibleCitizen.UnitEntity).AutoEngage);
            Assert.AreEqual(2, em.GetComponentData<Faction>(visibleCitizen.UnitEntity).Id);

            bool TryGetEntityManager(out EntityManager entityManager)
            {
                entityManager = em;
                return true;
            }
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            World.DefaultGameObjectInjectionWorld = previousWorld;
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

    private static void SetCitizenPrefabs(CitizenPrefabSelectionSystem prefabSelectionSystem, GameObject malePrefab)
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(CitizenPrefabSelectionSystem)
            .GetField("_maleCitizenPrefabs", Flags)
            .SetValue(prefabSelectionSystem, new[] { malePrefab });
        typeof(CitizenPrefabSelectionSystem)
            .GetField("_femaleCitizenPrefabs", Flags)
            .SetValue(prefabSelectionSystem, new GameObject[0]);
    }
}
#endif
