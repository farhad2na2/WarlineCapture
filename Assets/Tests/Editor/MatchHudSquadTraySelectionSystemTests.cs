using Game.UI.Contracts;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class MatchHudSquadTraySelectionUiSystemHelperTests
{
    private World _world;
    private EntityManager _entityManager;
    private SelectionStateCompositionSystemHelper _selectionState;
    private FocusedUnitLifecycleCompositionSystemHelper _focusedLifecycle;
    private MatchHudSquadTraySelectionUiSystemHelper _system;
    private TestSquadTrayView _view;
    private Entity _lastHudSelection;
    private int _lastHudSquadCount;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.SelectSoldiersSlot_SelectsClusterOfFourSoldiers());
            RunCase(test => test.SelectCombatVehiclesSlot_SelectsTwoGroundCombatVehiclesOnly());
            RunCase(test => test.SelectAircraftAndTransportSlots_SelectsExpectedUnitKinds());
            RunCase(test => test.SelectSoldiersSlot_RebindsAfterWorldReplacement());
            UnityEngine.Debug.Log("[MatchHudSquadTraySelectionFocusedValidation] result=Passed tests=4");
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError("[MatchHudSquadTraySelectionFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunCase(Action<MatchHudSquadTraySelectionUiSystemHelperTests> testCase)
    {
        var tests = new MatchHudSquadTraySelectionUiSystemHelperTests();
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

    [SetUp]
    public void SetUp()
    {
        _world = new World("MatchHudSquadTraySelectionUiSystemHelperTests");
        _entityManager = _world.EntityManager;
        _selectionState = new SelectionStateCompositionSystemHelper();
        _focusedLifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        _system = new MatchHudSquadTraySelectionUiSystemHelper();
        _view = new TestSquadTrayView();
        _lastHudSelection = Entity.Null;
        _lastHudSquadCount = -1;
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void SelectSoldiersSlot_SelectsClusterOfFourSoldiers()
    {
        Entity soldierA = CreatePlayerUnit("Unit_Chr_Soldier_Rifleman_A", new float3(1f, 0f, 1f));
        Entity soldierB = CreatePlayerUnit("Unit_Chr_Soldier_Rifleman_B", new float3(2f, 0f, 1f));
        Entity soldierC = CreatePlayerUnit("Unit_Chr_Soldier_Rifleman_C", new float3(1f, 0f, 2f));
        Entity soldierD = CreatePlayerUnit("Unit_Chr_Soldier_Rifleman_D", new float3(2f, 0f, 2f));
        Entity soldierE = CreatePlayerUnit("Unit_Chr_Soldier_Rifleman_E", new float3(20f, 0f, 20f));
        Entity tank = CreatePlayerUnit("Unit_Veh_Tank_Heavy", new float3(0f, 0f, 3f), usesVehicleMotion: true);

        _system.SelectSlot(CreateContext(), _view, MatchHudSquadTraySlot.Soldiers);

        CollectionAssert.AreEquivalent(
            new[] { soldierA, soldierB, soldierC, soldierD },
            SelectedEntities());
        Assert.IsFalse(_entityManager.HasComponent<SelectedUnitTag>(soldierE));
        Assert.IsFalse(_entityManager.HasComponent<SelectedUnitTag>(tank));
        Assert.AreEqual(MatchHudSquadTraySlot.Soldiers, _view.SelectedSlot);
        Assert.AreEqual(4, _lastHudSquadCount);
        Assert.AreEqual(Entity.Null, _selectionState.FocusedUnit);
        Assert.AreEqual(4, _selectionState.CachedSelectedMoveEntities.Count);
    }

    [Test]
    public void SelectCombatVehiclesSlot_SelectsTwoGroundCombatVehiclesOnly()
    {
        Entity tank = CreatePlayerUnit("Unit_Veh_Tank_Heavy", new float3(1f, 0f, 0f), usesVehicleMotion: true);
        Entity launcher = CreatePlayerUnit("Unit_Veh_Missle_Launcher_Ground", new float3(2f, 0f, 0f), usesVehicleMotion: true);
        Entity truck = CreatePlayerUnit("Unit_Veh_Truck_Cargo", new float3(3f, 0f, 0f), usesVehicleMotion: true);
        Entity soldier = CreatePlayerUnit("Unit_Chr_Soldier_Rifleman", new float3(0f, 0f, 1f));

        _system.SelectSlot(CreateContext(), _view, MatchHudSquadTraySlot.CombatVehicles);

        CollectionAssert.AreEquivalent(new[] { tank, launcher }, SelectedEntities());
        Assert.IsFalse(_entityManager.HasComponent<SelectedUnitTag>(truck));
        Assert.IsFalse(_entityManager.HasComponent<SelectedUnitTag>(soldier));
        Assert.AreEqual(MatchHudSquadTraySlot.CombatVehicles, _view.SelectedSlot);
        Assert.AreEqual(2, _lastHudSquadCount);
        Assert.AreEqual(Entity.Null, _selectionState.FocusedUnit);
    }

    [Test]
    public void SelectAircraftAndTransportSlots_SelectsExpectedUnitKinds()
    {
        Entity helicopter = CreatePlayerUnit("Unit_Veh_Helicopter_Attack", new float3(1f, 0f, 0f), isAir: true);
        Entity jet = CreatePlayerUnit("Unit_Veh_Jet_Attack", new float3(2f, 0f, 0f), isAir: true);
        Entity transport = CreatePlayerUnit("Unit_Veh_APC_Transport", new float3(3f, 0f, 0f), usesVehicleMotion: true, transportSeats: 8);
        Entity airTransport = CreatePlayerUnit("Unit_Veh_Helicopter_Transport", new float3(4f, 0f, 0f), isAir: true, transportSeats: 6);

        _system.SelectSlot(CreateContext(), _view, MatchHudSquadTraySlot.AttackHelicopter);

        CollectionAssert.AreEquivalent(new[] { helicopter }, SelectedEntities());
        Assert.AreEqual(helicopter, _selectionState.FocusedUnit);
        Assert.AreEqual(helicopter, _lastHudSelection);

        _system.SelectSlot(CreateContext(), _view, MatchHudSquadTraySlot.Jet);

        CollectionAssert.AreEquivalent(new[] { jet }, SelectedEntities());
        Assert.AreEqual(jet, _selectionState.FocusedUnit);
        Assert.AreEqual(jet, _lastHudSelection);

        _system.SelectSlot(CreateContext(), _view, MatchHudSquadTraySlot.Transport);

        CollectionAssert.AreEquivalent(new[] { transport }, SelectedEntities());
        Assert.IsFalse(_entityManager.HasComponent<SelectedUnitTag>(airTransport));
        Assert.AreEqual(transport, _selectionState.FocusedUnit);
        Assert.AreEqual(transport, _lastHudSelection);
        Assert.AreEqual(MatchHudSquadTraySlot.Transport, _view.SelectedSlot);
    }

    [Test]
    public void SelectSoldiersSlot_RebindsAfterWorldReplacement()
    {
        EntityManager firstEntityManager = _entityManager;
        Entity firstSoldier = CreatePlayerUnit("Unit_Chr_Soldier_First", new float3(1f, 0f, 1f));
        _system.SelectSlot(CreateContext(), _view, MatchHudSquadTraySlot.Soldiers);
        Assert.IsTrue(firstEntityManager.HasComponent<SelectedUnitTag>(firstSoldier));

        using World replacementWorld = new(nameof(SelectSoldiersSlot_RebindsAfterWorldReplacement));
        try
        {
            _entityManager = replacementWorld.EntityManager;
            _selectionState = new SelectionStateCompositionSystemHelper();
            _focusedLifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
            _entityManager.CreateEntity();
            Entity replacementSoldier = CreatePlayerUnit(
                "Unit_Chr_Soldier_Replacement",
                new float3(4f, 0f, 4f));

            _system.SelectSlot(CreateContext(), _view, MatchHudSquadTraySlot.Soldiers);

            Assert.IsTrue(_entityManager.HasComponent<SelectedUnitTag>(replacementSoldier));
            Assert.AreEqual(1, SelectedEntities().Length);
            Assert.IsTrue(firstEntityManager.HasComponent<SelectedUnitTag>(firstSoldier));
        }
        finally
        {
            _entityManager = firstEntityManager;
        }
    }

    private MatchHudSquadTraySelectionUiSystemHelper.Context CreateContext()
    {
        return new MatchHudSquadTraySelectionUiSystemHelper.Context(
            null,
            TryGetEntityManager,
            _ => { },
            ClearCurrentSelection,
            () => { },
            (_, entity) => _lastHudSelection = entity,
            count => _lastHudSquadCount = count,
            _ => { },
            _selectionState,
            _focusedLifecycle);
    }

    private bool TryGetEntityManager(out EntityManager entityManager)
    {
        entityManager = _entityManager;
        return true;
    }

    private void ClearCurrentSelection(EntityManager entityManager, string reason)
    {
        using EntityQuery selectedQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        using NativeArray<Entity> selected = selectedQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < selected.Length; i++)
        {
            if (entityManager.HasComponent<SelectedUnitTag>(selected[i]))
                entityManager.RemoveComponent<SelectedUnitTag>(selected[i]);
        }

        _selectionState.ClearFocusedUnit();
        _selectionState.ClearSelectedMoveCache();
        _lastHudSelection = Entity.Null;
        _lastHudSquadCount = -1;
    }

    private Entity[] SelectedEntities()
    {
        using EntityQuery selectedQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        using NativeArray<Entity> selected = selectedQuery.ToEntityArray(Allocator.Temp);
        return selected.ToArray();
    }

    private Entity CreatePlayerUnit(
        string sourceKey,
        float3 position,
        bool usesVehicleMotion = false,
        bool isAir = false,
        int transportSeats = 0)
    {
        Entity entity = _entityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitHealth),
            typeof(UnitSourcePrefabKey),
            typeof(LocalToWorld));
        _entityManager.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = new int2((int)position.x, (int)position.z) });
        _entityManager.SetComponentData(entity, new UnitMove { Speed = 5f });
        _entityManager.SetComponentData(entity, new UnitMovementBehavior { UsesVehicleMotion = usesVehicleMotion ? (byte)1 : (byte)0 });
        _entityManager.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        _entityManager.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(sourceKey) });
        _entityManager.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });

        if (isAir)
            _entityManager.AddComponentData(entity, new UnitAirMovement { CruiseHeight = 18f });
        if (transportSeats > 0)
        {
            _entityManager.AddComponentData(entity, new UnitTransportCapacity { SoldierCapacity = transportSeats });
            _entityManager.AddBuffer<UnitTransportPassengerElement>(entity);
        }

        return entity;
    }

    private sealed class TestSquadTrayView : IMatchHudSquadTrayView
    {
        public MatchHudSquadTraySlot SelectedSlot { get; private set; } = MatchHudSquadTraySlot.None;
        public MatchHudSquadTraySlot FlashedSlot { get; private set; } = MatchHudSquadTraySlot.None;

        public void Bind(Action<MatchHudSquadTraySlot> cardClicked)
        {
        }

        public void ClearActiveSlot()
        {
            SelectedSlot = MatchHudSquadTraySlot.None;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            return false;
        }

        public void FlashDisabled(MatchHudSquadTraySlot slot)
        {
            FlashedSlot = slot;
        }

        public void SetSelectedSlot(MatchHudSquadTraySlot selectedSlot)
        {
            SelectedSlot = selectedSlot;
        }

        public bool TryGetPortraitSprite(MatchHudSquadTraySlot slot, out Sprite sprite)
        {
            sprite = null;
            return false;
        }

        public void Unbind()
        {
        }
    }
}
#endif
