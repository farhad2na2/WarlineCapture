#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingProductionSystemTests
{
    [Test]
    public void InitializePendingProduction_SetsReadyTimeAndTransportFields()
    {
        var pending = new TestPendingProduction();
        var system = new BuildingProductionSystem();

        system.InitializePendingProduction(
            pending,
            productionIndex: 2,
            spawnUnitPrefab: null,
            now: 10f,
            productionDurationSeconds: 4f,
            reservedProductionSlotIndex: 1,
            transportPrefab: null,
            transportArrivalSeconds: 3f,
            transportHoldForNextReadySeconds: 5f,
            transportMaxConcurrent: 2,
            transportMode: BuildingProductionSystem.ProductionTransportMode.Plane,
            transportRequiresAirportRunway: true);

        Assert.AreEqual(2, pending.ProductionIndex);
        Assert.AreEqual(10f, pending.StartedAt);
        Assert.AreEqual(14f, pending.ReadyAt);
        Assert.AreEqual(1, pending.ReservedProductionSlotIndex);
        Assert.AreEqual(3f, pending.TransportArrivalSeconds);
        Assert.AreEqual(5f, pending.TransportHoldForNextReadySeconds);
        Assert.AreEqual(2, pending.TransportMaxConcurrent);
        Assert.AreEqual(BuildingProductionSystem.ProductionTransportMode.Plane, pending.TransportMode);
        Assert.IsTrue(pending.TransportRequiresAirportRunway);
    }

    [Test]
    public void ResolveProductionDurationSeconds_UsesUnitAuthoringDuration()
    {
        GameObject prefab = new("Unit_Infantry_Test");
        try
        {
            UnitGridAuthoring authoring = prefab.AddComponent<UnitGridAuthoring>();
            SetAuthoringField(authoring, "productionDurationSeconds", 12.5f);

            var system = new BuildingProductionSystem();

            Assert.AreEqual(12.5f, system.ResolveProductionDurationSeconds(prefab), 0.0001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void ResolveProductionTransportSettings_UsesConfiguredTransportAuthoring()
    {
        GameObject producedPrefab = new("Unit_Infantry_Test");
        GameObject transportPrefab = new("Unit_Veh_Helicopter_Transport");
        try
        {
            UnitGridAuthoring producedAuthoring = producedPrefab.AddComponent<UnitGridAuthoring>();
            UnitGridAuthoring transportAuthoring = transportPrefab.AddComponent<UnitGridAuthoring>();
            SetAuthoringField(producedAuthoring, "productionTransportPrefab", transportPrefab);
            SetAuthoringField(transportAuthoring, "productionTransportArrivalSeconds", 8f);
            SetAuthoringField(transportAuthoring, "productionTransportHoldForNextReadySeconds", 3f);
            SetAuthoringField(transportAuthoring, "productionTransportMaxConcurrent", 4);

            var system = new BuildingProductionSystem();
            BuildingProductionSystem.ProductionTransportSettings settings = system.ResolveProductionTransportSettings(
                producedPrefab,
                new[] { transportPrefab },
                new Dictionary<string, GameObject> { ["unit_veh_helicopter_transport"] = transportPrefab },
                null);

            Assert.AreSame(transportPrefab, settings.TransportPrefab);
            Assert.AreEqual(8f, settings.ArrivalSeconds, 0.0001f);
            Assert.AreEqual(3f, settings.HoldForNextReadySeconds, 0.0001f);
            Assert.AreEqual(4, settings.MaxConcurrent);
            Assert.AreEqual(BuildingProductionSystem.ProductionTransportMode.Helicopter, settings.Mode);
            Assert.IsFalse(settings.RequiresAirportRunway);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(producedPrefab);
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void ResolveProductionTransportSettings_DefaultsLargeVehicleToPlaneTransport()
    {
        GameObject producedPrefab = new("Unit_Veh_TankHeavy_Test");
        GameObject helicopterPrefab = new("Unit_Veh_Helicopter_Transport");
        GameObject planePrefab = new("Unit_Veh_Plane_Transport");
        try
        {
            producedPrefab.AddComponent<UnitGridAuthoring>();
            helicopterPrefab.AddComponent<UnitGridAuthoring>();
            planePrefab.AddComponent<UnitGridAuthoring>();

            var prefabsByKey = new Dictionary<string, GameObject>
            {
                ["unit_veh_helicopter_transport"] = helicopterPrefab,
                ["unit_veh_plane_transport"] = planePrefab
            };
            var system = new BuildingProductionSystem();
            BuildingProductionSystem.ProductionTransportSettings settings = system.ResolveProductionTransportSettings(
                producedPrefab,
                new[] { helicopterPrefab, planePrefab },
                prefabsByKey,
                (GameObject _, out Bounds bounds) =>
                {
                    bounds = new Bounds(Vector3.zero, new Vector3(3f, 1f, 2f));
                    return true;
                });

            Assert.AreSame(planePrefab, settings.TransportPrefab);
            Assert.AreEqual(BuildingProductionSystem.ProductionTransportMode.Plane, settings.Mode);
            Assert.IsTrue(settings.RequiresAirportRunway);
            Assert.AreEqual(1, settings.MaxConcurrent);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(producedPrefab);
            UnityEngine.Object.DestroyImmediate(helicopterPrefab);
            UnityEngine.Object.DestroyImmediate(planePrefab);
        }
    }

    [Test]
    public void GetProgress_ComputesRemainingAndCanCapTransportProgress()
    {
        var transportPrefab = new GameObject("Transport");
        try
        {
            var pending = new TestPendingProduction
            {
                StartedAt = 0f,
                ReadyAt = 10f,
                TransportPrefab = transportPrefab
            };

            var system = new BuildingProductionSystem();
            BuildingProductionSystem.PendingProductionProgress progress = system.GetProgress(pending, 9.9f, true);

            Assert.AreEqual(10f, progress.DurationSeconds);
            Assert.AreEqual(0.1f, progress.RemainingSeconds, 0.0001f);
            Assert.AreEqual(0.97f, progress.Progress01, 0.0001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void TransportLaunchAndDelay_UsesArrivalWindow()
    {
        var transportPrefab = new GameObject("Transport");
        try
        {
            var pending = new TestPendingProduction
            {
                StartedAt = 0f,
                ReadyAt = 20f,
                TransportPrefab = transportPrefab,
                TransportArrivalSeconds = 5f
            };

            var system = new BuildingProductionSystem();

            Assert.AreEqual(15f, system.GetTransportLaunchAt(pending));
            Assert.IsFalse(system.ShouldLaunchTransport(pending, 14.9f));
            Assert.IsTrue(system.ShouldLaunchTransport(pending, 15f));

            system.DelayPendingProduction(pending, 2f);

            Assert.AreEqual(2f, pending.StartedAt);
            Assert.AreEqual(22f, pending.ReadyAt);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void ReadinessHelpers_ReportReadyAndSoonStates()
    {
        var pending = new TestPendingProduction
        {
            ReadyAt = 20f
        };

        var system = new BuildingProductionSystem();

        Assert.IsFalse(system.IsReady(pending, 19.9f));
        Assert.IsTrue(system.IsReady(pending, 20f));
        Assert.IsTrue(system.IsReadyWithin(pending, 18f, 2.5f));
        Assert.IsFalse(system.IsReadyWithin(pending, 16f, 2.5f));
    }

    [Test]
    public void PruneProducedUnits_RemovesDeadUnitsAndClearsDeadSlots()
    {
        using World world = new("BuildingProductionSystemTests");
        EntityManager entityManager = world.EntityManager;
        Entity alive = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(alive, new UnitHealth { Current = 5, Max = 10 });
        Entity dead = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(dead, new UnitHealth { Current = 0, Max = 10 });
        var producedUnits = new List<Entity> { alive, dead, Entity.Null };
        GameObject deadPrefab = new("DeadPrefab");
        var producedUnitPrefabs = new Dictionary<Entity, GameObject>
        {
            [dead] = deadPrefab
        };
        Entity[] slots = { dead, alive };

        try
        {
            var system = new BuildingProductionSystem();
            system.PruneProducedUnits(producedUnits, slots, producedUnitPrefabs, entityManager);

            Assert.AreEqual(1, producedUnits.Count);
            Assert.AreEqual(alive, producedUnits[0]);
            Assert.IsFalse(producedUnitPrefabs.ContainsKey(dead));
            Assert.AreEqual(Entity.Null, slots[0]);
            Assert.AreEqual(alive, slots[1]);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(deadPrefab);
        }
    }

    [Test]
    public void TransportPendingQueries_FindReadyAndSoonEntries()
    {
        var transportPrefab = new GameObject("Transport");
        try
        {
            var ready = new TestPendingProduction
            {
                TransportPrefab = transportPrefab,
                ReadyAt = 10f
            };
            var soon = new TestPendingProduction
            {
                TransportPrefab = transportPrefab,
                ReadyAt = 13f
            };
            var later = new TestPendingProduction
            {
                TransportPrefab = transportPrefab,
                ReadyAt = 20f
            };
            var pending = new List<TestPendingProduction> { soon, ready, later };

            var system = new BuildingProductionSystem();

            Assert.AreSame(ready, system.FindNextReadyTransportPending(pending, transportPrefab, 10f));
            Assert.AreSame(soon, system.FindNextSoonTransportPending(pending, transportPrefab, 10f, 4f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void PendingRemovalHelpers_RemoveByReferenceAndIndex()
    {
        var first = new TestPendingProduction();
        var second = new TestPendingProduction();
        var third = new TestPendingProduction();
        var pending = new List<TestPendingProduction> { first, second, third };

        var system = new BuildingProductionSystem();

        Assert.IsTrue(system.RemovePendingProduction(pending, second));
        Assert.AreEqual(2, pending.Count);
        Assert.AreSame(first, pending[0]);
        Assert.AreSame(third, pending[1]);

        Assert.IsTrue(system.RemovePendingAt(pending, 0));
        Assert.AreEqual(1, pending.Count);
        Assert.AreSame(third, pending[0]);
    }

    [Test]
    public void BuildingGameplayComposition_InitializesRuntimeDollarsFromInitialUnitsConfig()
    {
        var placementConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        var initialUnitsConfig = ScriptableObject.CreateInstance<InitialUnitsSpawnerAuthoringSceneConfigAsset>();
        BuildingGameplayCompositionSystem.Result result = default;
        try
        {
            SetPrivateField(initialUnitsConfig, "initialDollars", 12345);
            SetPrivateField(placementConfig, "initialUnitsConfig", initialUnitsConfig);

            var composition = new BuildingGameplayCompositionSystem();
            result = composition.Initialize(
                placementConfig,
                worldCamera: null,
                runtimeUiRoot: null,
                roadFootprintQuerySystem: null,
                roadFootprintQueryContext: default,
                factionVisuals: null,
                dayNight: null);

            Assert.AreEqual(12345, result.UiCommand.CurrentDollars(result.UiCommandContext));
        }
        finally
        {
            result.Dispose?.Invoke();
            UnityEngine.Object.DestroyImmediate(initialUnitsConfig);
            UnityEngine.Object.DestroyImmediate(placementConfig);
        }
    }

    private sealed class TestPendingProduction : BuildingProductionSystem.IPendingProduction
    {
        public int ProductionIndex { get; set; }
        public GameObject Prefab { get; set; }
        public float StartedAt { get; set; }
        public float ReadyAt { get; set; }
        public int ReservedProductionSlotIndex { get; set; }
        public GameObject TransportPrefab { get; set; }
        public float TransportArrivalSeconds { get; set; }
        public float TransportHoldForNextReadySeconds { get; set; }
        public int TransportMaxConcurrent { get; set; }
        public BuildingProductionSystem.ProductionTransportMode TransportMode { get; set; }
        public bool TransportRequiresAirportRunway { get; set; }
    }

    private static void SetAuthoringField<T>(UnitGridAuthoring authoring, string fieldName, T value)
    {
        FieldInfo field = typeof(UnitGridAuthoring).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"{nameof(UnitGridAuthoring)} must expose serialized field '{fieldName}' for this test.");
        field.SetValue(authoring, value);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = FindPrivateField(target.GetType(), fieldName);
        Assert.IsNotNull(field, $"{target.GetType().Name} must expose serialized field '{fieldName}' for this test.");
        field.SetValue(target, value);
    }

    private static FieldInfo FindPrivateField(System.Type type, string fieldName)
    {
        for (System.Type current = type; current != null; current = current.BaseType)
        {
            FieldInfo field = current.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return field;
        }

        return null;
    }
}
#endif
