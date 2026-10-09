using Game.UI.Contracts;
using Game.Components;
using Game.Missions.Contracts;
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
    private int _buildingSelectionClears;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.SelectSoldiersSlot_SelectsClusterOfFourSoldiers());
            RunCase(test => test.SelectCombatVehiclesSlot_SelectsTwoGroundCombatVehiclesOnly());
            RunCase(test => test.SelectAircraftAndTransportSlots_SelectsExpectedUnitKinds());
            RunCase(test => test.SelectSoldiersSlot_RebindsAfterWorldReplacement());
            RunCase(test => test.SelectSoldiersSlot_ReplaySelectsExactlyCurrentMissionSquad());
            RunCase(test => test.ExpandedSelectionReplacesBuildingFocusAndPreservesAdditiveMembers());
            UnityEngine.Debug.Log("[MatchHudSquadTraySelectionFocusedValidation] result=Passed tests=6");
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
    public void OperationsRifleCardSelectsSurvivingOriginalForceWithoutCyclingOrUnrelatedUnits()
    {
        var root = _entityManager.CreateEntity(typeof(OperationsReconMissionComponent));
        _entityManager.SetComponentData(root, new OperationsReconMissionComponent { Phase = OperationsReconPhase.Playing });
        _entityManager.AddBuffer<OperationsReconRosterElement>(root);
        var expected = new List<Entity>();
        for (int i = 0; i < 16; i++)
        {
            var unit = CreatePlayerUnit("Unit_Chr_Soldier_Operations_" + i, new float3(i * 10, 0, 0));
            _entityManager.GetBuffer<OperationsReconRosterElement>(root).Add(new OperationsReconRosterElement { Unit = unit });
            if (i == 0) _entityManager.DestroyEntity(unit);
            else if (i == 1) _entityManager.SetComponentData(unit, new UnitHealth { Current = 0, Max = 100 });
            else expected.Add(unit);
        }
        CreatePlayerUnit("Unit_Chr_Soldier_Unrelated", new float3(1, 0, 1));
        for (int click = 0; click < 2; click++)
        {
            _system.SelectSlot(CreateContext(), _view, MatchHudSquadTraySlot.Soldiers);
            CollectionAssert.AreEquivalent(expected, SelectedEntities());
            Assert.That(_lastHudSquadCount, Is.EqualTo(14));
        }
        _entityManager.DestroyEntity(root);
        _system.SelectSlot(CreateContext(), _view, MatchHudSquadTraySlot.Soldiers);
        Assert.That(SelectedEntities().Length, Is.EqualTo(4), "Leaving Operations must restore shared four-soldier quick selection.");
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

    [Test]
    public void SelectSoldiersSlot_ReplaySelectsExactlyCurrentMissionSquad()
    {
        FixedString64Bytes firstSession = new("m01-tray-first");
        Entity runtime = _entityManager.CreateEntity(typeof(CampaignMissionRuntimeComponent));
        _entityManager.SetComponentData(runtime, CreateGuidedRuntime(firstSession));
        Entity[] firstSquad = CreateMissionSquad(firstSession, "First");
        CreatePlayerUnit("Unit_Chr_Soldier_Unrelated", new float3(30f, 0f, 30f));

        _system.SelectSlot(CreateContext(), _view, MatchHudSquadTraySlot.Soldiers);
        CollectionAssert.AreEquivalent(firstSquad, SelectedEntities());

        for (int i = 0; i < firstSquad.Length; i++)
            _entityManager.DestroyEntity(firstSquad[i]);
        FixedString64Bytes replaySession = new("m01-tray-replay");
        _entityManager.SetComponentData(runtime, CreateGuidedRuntime(replaySession));
        Entity[] replaySquad = CreateMissionSquad(replaySession, "Replay");

        _system.SelectSlot(CreateContext(), _view, MatchHudSquadTraySlot.Soldiers);

        CollectionAssert.AreEquivalent(replaySquad, SelectedEntities(),
            "A replay must ignore the previous tray cycle and select all four current-session soldiers.");
        Assert.AreEqual(4, _selectionState.CachedSelectedMoveEntities.Count);
        Assert.AreEqual(4, _lastHudSquadCount);
    }

    [Test]
    public void ExpandedSelectionReplacesBuildingFocusAndPreservesAdditiveMembers()
    {
        Entity oldFocus = CreatePlayerUnit("Building_Airport", new float3(1, 0, 1));
        Entity strike = CreatePlayerUnit("Unit_Veh_Jet_01", new float3(2, 0, 2), isAir: true);
        _entityManager.AddComponentData(strike, new SkirmishAttemptOwnedComponent { FactionId = 1 });
        _selectionState.SetFocusedUnit(oldFocus);
        _selectionState.CacheSelectedMoveEntity(_entityManager, oldFocus);
        _entityManager.AddComponent<SelectedUnitTag>(strike);
        _system.SynchronizeExpandedSelection(CreateContext(), _entityManager);
        Assert.AreEqual(1, _buildingSelectionClears);
        Assert.AreEqual(strike, _selectionState.FocusedUnit);
        Assert.AreEqual(strike, _lastHudSelection);
        CollectionAssert.AreEqual(new[] { strike }, _selectionState.CachedSelectedMoveEntities);
        Assert.IsTrue(_entityManager.HasComponent<SelectedUnitTag>(strike));
        Assert.IsFalse(_entityManager.HasComponent<UnitTarget>(strike), "selection must not issue movement orders");

        Entity second = CreatePlayerUnit("Unit_Veh_Helicopter_Attack_Small", new float3(3, 0, 3), isAir: true);
        _entityManager.AddComponentData(second, new SkirmishAttemptOwnedComponent { FactionId = 1 });
        _entityManager.AddComponent<SelectedUnitTag>(second);
        _system.SynchronizeExpandedSelection(CreateContext(), _entityManager);
        CollectionAssert.AreEquivalent(new[] { strike, second }, SelectedEntities());
        CollectionAssert.AreEquivalent(new[] { strike, second }, _selectionState.CachedSelectedMoveEntities);
        Assert.AreEqual(Entity.Null, _selectionState.FocusedUnit);
        Assert.AreEqual(2, _lastHudSquadCount);

        _entityManager.RemoveComponent<SelectedUnitTag>(strike);
        _entityManager.RemoveComponent<SelectedUnitTag>(second);
        _system.SynchronizeExpandedSelection(CreateContext(), _entityManager);
        Assert.AreEqual(0, _selectionState.CachedSelectedMoveEntities.Count);
        Assert.AreEqual(Entity.Null, _selectionState.FocusedUnit);
        Assert.AreEqual(0, _lastHudSquadCount);
        Assert.AreEqual(3, _buildingSelectionClears);
    }

    private MatchHudSquadTraySelectionUiSystemHelper.Context CreateContext()
    {
        return new MatchHudSquadTraySelectionUiSystemHelper.Context(
            null,
            TryGetEntityManager,
            _ => { },
            ClearCurrentSelection,
            () => _buildingSelectionClears++,
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

    private Entity[] CreateMissionSquad(FixedString64Bytes session, string suffix)
    {
        var squad = new Entity[4];
        for (int i = 0; i < squad.Length; i++)
        {
            Entity entity = CreatePlayerUnit(
                $"Unit_Chr_Soldier_{suffix}_{i}", new float3(i + 1f, 0f, i + 1f));
            _entityManager.AddComponentData(entity, new CampaignMissionUnitRoleComponent
            {
                MissionRoleId = new FixedString64Bytes("role.friendly.command_squad"),
                UnitGroupId = new FixedString64Bytes("group.ch01.m01.command_squad"),
                SessionToken = session
            });
            squad[i] = entity;
        }
        return squad;
    }

    private static CampaignMissionRuntimeComponent CreateGuidedRuntime(FixedString64Bytes session) => new()
    {
        MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
        SessionToken = session,
        Phase = MissionPhaseKind.FindSquad,
        Outcome = MissionOutcomeKind.None,
        Version = 1,
        SourceVersion = 1,
        AttemptOrdinal = 1,
        DeterministicSeed = 1001001
    };

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
