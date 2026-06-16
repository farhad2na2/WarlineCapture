#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using Unity.Transforms;
using UnityEngine;

public sealed class SelectionUiReadModelLookupTests
{
    private World _world;
    private EntityManager _entityManager;
    private SelectionUiReadModelLookup _lookup;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.ResolveFocusedUnitNameAndDescription_UsesConfiguredDisplayInfo());
            passed++;
            RunCase(test => test.TryGetFocusedUnitCapacityInfo_ProjectsLoadingProgressFromActionTime());
            passed++;
            RunCase(test => test.GetFocusedUnitUiStatus_PrioritizesReturningEngagedAndMovingStates());
            passed++;
            RunCase(test => test.TryGetSelectedUnitsPortraitPose_CentersAndFramesSelectedUnits());
            passed++;
            RunCase(test => test.CommandCapabilities_ReturnTypedReasonsForHoldStopAndScan());
            passed++;

            Debug.Log($"[SelectionUiReadModelLookupValidation] result=Passed tests={passed}");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[SelectionUiReadModelLookupValidation] result=Failed passed={passed}");
            EditorApplication.Exit(1);
        }
    }

    private static void RunCase(Action<SelectionUiReadModelLookupTests> testCase)
    {
        var tests = new SelectionUiReadModelLookupTests();
        tests.SetUp();
        try
        {
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
        _world = new World("SelectionUiReadModelLookupTests");
        _entityManager = _world.EntityManager;
        _lookup = new SelectionUiReadModelLookup();
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void ResolveFocusedUnitNameAndDescription_UsesConfiguredDisplayInfo()
    {
        Entity entity = _entityManager.CreateEntity(typeof(UnitDisplayInfo), typeof(Faction), typeof(UnitMove));
        _entityManager.SetComponentData(entity, new UnitDisplayInfo
        {
            Name = new FixedString64Bytes("Scout Team"),
            Description = new FixedString128Bytes("Fast reconnaissance infantry.")
        });
        _entityManager.SetComponentData(entity, new Faction { Id = 0 });

        Assert.AreEqual("Scout Team", _lookup.ResolveFocusedUnitName(_entityManager, entity));
        Assert.AreEqual("Fast reconnaissance infantry.", _lookup.ResolveFocusedUnitDescription(_entityManager, entity));
    }

    [Test]
    public void TryGetFocusedUnitCapacityInfo_ProjectsLoadingProgressFromActionTime()
    {
        Entity entity = _entityManager.CreateEntity(typeof(UnitResourceHauler), typeof(UnitResourceHaulOrder));
        _entityManager.SetComponentData(entity, new UnitResourceHauler
        {
            BarrelCapacity = 100,
            FillDurationSeconds = 10f,
            CargoOilBarrels = 0f
        });
        _entityManager.SetComponentData(entity, new UnitResourceHaulOrder
        {
            Phase = 2,
            ActionEndsAt = 20f
        });

        Assert.IsTrue(_lookup.TryGetFocusedUnitCapacityInfo(_entityManager, entity, 15f, out int current, out int max, out float progress01));
        Assert.AreEqual(50, current);
        Assert.AreEqual(100, max);
        Assert.AreEqual(0.5f, progress01, 0.001f);
    }

    [Test]
    public void GetFocusedUnitUiStatus_PrioritizesReturningEngagedAndMovingStates()
    {
        Entity airUnit = _entityManager.CreateEntity(typeof(UnitAirComponent));
        _entityManager.SetComponentData(airUnit, new UnitAirComponent { ReturningHome = 1 });
        Entity engagedUnit = _entityManager.CreateEntity(typeof(EngageTarget));
        Entity missileUnit = _entityManager.CreateEntity(typeof(GroundMissileInFlightComponent));
        Entity commandedMissileUnit = _entityManager.CreateEntity(typeof(GroundMissileLauncherComponent), typeof(EngageTarget));
        _entityManager.SetComponentData(commandedMissileUnit, new EngageTarget { IsCommanded = 1 });
        Entity missilePreparingUnit = _entityManager.CreateEntity(typeof(GroundMissileLauncherStateComponent));
        _entityManager.SetComponentData(missilePreparingUnit, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Preparing
        });
        Entity autoTargetMissileUnit = _entityManager.CreateEntity(typeof(GroundMissileLauncherComponent), typeof(EngageTarget));
        _entityManager.SetComponentData(autoTargetMissileUnit, new EngageTarget { IsCommanded = 0 });
        Entity movingUnit = _entityManager.CreateEntity(typeof(UnitPathRequest));
        Entity holdingUnit = _entityManager.CreateEntity(typeof(HoldPositionOrderTag), typeof(UnitPathRequest));
        Entity manualGuardUnit = _entityManager.CreateEntity(typeof(ManualMoveOrderTag));

        Assert.AreEqual(SelectionUiReadModelLookup.FocusedUnitUiStatus.ReturningToBase, _lookup.GetFocusedUnitUiStatus(_entityManager, airUnit));
        Assert.AreEqual(SelectionUiReadModelLookup.FocusedUnitUiStatus.MissileLaunched, _lookup.GetFocusedUnitUiStatus(_entityManager, missileUnit));
        Assert.AreEqual(SelectionUiReadModelLookup.FocusedUnitUiStatus.MissileLaunched, _lookup.GetFocusedUnitUiStatus(_entityManager, commandedMissileUnit));
        Assert.AreEqual(SelectionUiReadModelLookup.FocusedUnitUiStatus.Idle, _lookup.GetFocusedUnitUiStatus(_entityManager, missilePreparingUnit));
        Assert.AreEqual(SelectionUiReadModelLookup.FocusedUnitUiStatus.Idle, _lookup.GetFocusedUnitUiStatus(_entityManager, autoTargetMissileUnit));
        Assert.AreEqual(SelectionUiReadModelLookup.FocusedUnitUiStatus.Engaged, _lookup.GetFocusedUnitUiStatus(_entityManager, engagedUnit));
        Assert.AreEqual(SelectionUiReadModelLookup.FocusedUnitUiStatus.Moving, _lookup.GetFocusedUnitUiStatus(_entityManager, movingUnit));
        Assert.AreEqual(SelectionUiReadModelLookup.FocusedUnitUiStatus.Idle, _lookup.GetFocusedUnitUiStatus(_entityManager, holdingUnit));
        Assert.AreEqual(SelectionUiReadModelLookup.FocusedUnitUiStatus.Idle, _lookup.GetFocusedUnitUiStatus(_entityManager, manualGuardUnit));
        StringAssert.Contains("MISSILE LAUNCHED", _lookup.ResolveHudSelectionStatus(_entityManager, missileUnit));
        StringAssert.Contains("MISSILE LAUNCHED", _lookup.ResolveHudSelectionStatus(_entityManager, commandedMissileUnit));
        StringAssert.Contains("IDLE", _lookup.ResolveHudSelectionStatus(_entityManager, missilePreparingUnit));
        StringAssert.Contains("IDLE", _lookup.ResolveHudSelectionStatus(_entityManager, autoTargetMissileUnit));
        StringAssert.Contains("HOLDING", _lookup.ResolveHudSelectionStatus(_entityManager, holdingUnit));
        StringAssert.Contains("IDLE", _lookup.ResolveHudSelectionStatus(_entityManager, manualGuardUnit));
    }

    [Test]
    public void TryGetSelectedUnitsPortraitPose_CentersAndFramesSelectedUnits()
    {
        Entity a = CreatePoseEntity(new float3(0f, 0f, 0f));
        Entity b = CreatePoseEntity(new float3(10f, 0f, 0f));
        NativeArray<Entity> selected = new(2, Allocator.Temp);
        try
        {
            selected[0] = a;
            selected[1] = b;

            Assert.IsTrue(_lookup.TryGetSelectedUnitsPortraitPose(_entityManager, selected, Entity.Null, out Vector3 center, out _, out float radius));
            Assert.AreEqual(new Vector3(5f, 0f, 0f), center);
            Assert.AreEqual(6.5f, radius, 0.001f);
        }
        finally
        {
            selected.Dispose();
        }
    }

    [Test]
    public void CommandCapabilities_ReturnTypedReasonsForHoldStopAndScan()
    {
        Entity soldier = CreateCommandableUnit("Unit_Chr_Rifle_Squad", "Rifle Squad");
        Assert.IsTrue(_lookup.CanHoldPosition(_entityManager, soldier, out TacticalCommandReasonCode holdReason));
        Assert.AreEqual(TacticalCommandReasonCode.None, holdReason);
        Assert.IsTrue(_lookup.CanStop(_entityManager, soldier, out TacticalCommandReasonCode stopReason));
        Assert.AreEqual(TacticalCommandReasonCode.None, stopReason);
        Assert.IsFalse(_lookup.CanScan(_entityManager, soldier, out TacticalCommandReasonCode soldierScanReason));
        Assert.AreEqual(TacticalCommandReasonCode.ScanUnavailable, soldierScanReason);

        Entity scoutDrone = CreateCommandableUnit("Unit_Veh_Drone_Recon", "Recon Drone", typeof(UnitAirMovement));
        Assert.IsTrue(_lookup.CanScan(_entityManager, scoutDrone, out TacticalCommandReasonCode droneScanReason));
        Assert.AreEqual(TacticalCommandReasonCode.None, droneScanReason);

        Entity passenger = CreateCommandableUnit("Unit_Chr_Rifle_Squad", "Passenger", typeof(UnitTransportPassenger));
        Assert.IsFalse(_lookup.CanHoldPosition(_entityManager, passenger, out TacticalCommandReasonCode passengerHoldReason));
        Assert.AreEqual(TacticalCommandReasonCode.CommandUnavailable, passengerHoldReason);

        Entity enemy = _entityManager.CreateEntity(typeof(Faction), typeof(UnitMove), typeof(UnitHealth));
        _entityManager.SetComponentData(enemy, new Faction { Id = FactionIdentity.EnemyFactionId });
        _entityManager.SetComponentData(enemy, new UnitHealth { Current = 10, Max = 10 });
        Assert.IsFalse(_lookup.CanStop(_entityManager, enemy, out TacticalCommandReasonCode enemyStopReason));
        Assert.AreEqual(TacticalCommandReasonCode.CommandUnavailable, enemyStopReason);

        Entity deadUnit = CreateCommandableUnit("Unit_Chr_Rifle_Squad", "Dead Unit");
        _entityManager.SetComponentData(deadUnit, new UnitHealth { Current = 0, Max = 10 });
        Assert.IsFalse(_lookup.CanHoldPosition(_entityManager, deadUnit, out TacticalCommandReasonCode deadHoldReason));
        Assert.AreEqual(TacticalCommandReasonCode.CommandUnavailable, deadHoldReason);

        Assert.IsFalse(_lookup.CanHoldPosition(_entityManager, Entity.Null, out TacticalCommandReasonCode noSelectionReason));
        Assert.AreEqual(TacticalCommandReasonCode.NoSelection, noSelectionReason);
    }

    private Entity CreatePoseEntity(float3 position)
    {
        Entity entity = _entityManager.CreateEntity(typeof(LocalTransform), typeof(LocalToWorld));
        _entityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
        _entityManager.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
        return entity;
    }

    private Entity CreateCommandableUnit(string sourceKey, string displayName, params ComponentType[] extraTypes)
    {
        ComponentType[] baseTypes =
        {
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitHealth),
            typeof(UnitSourcePrefabKey),
            typeof(UnitDisplayInfo)
        };
        var types = new ComponentType[baseTypes.Length + extraTypes.Length];
        Array.Copy(baseTypes, types, baseTypes.Length);
        Array.Copy(extraTypes, 0, types, baseTypes.Length, extraTypes.Length);

        Entity entity = _entityManager.CreateEntity(types);
        _entityManager.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(entity, new UnitHealth { Current = 10, Max = 10 });
        _entityManager.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(sourceKey) });
        _entityManager.SetComponentData(entity, new UnitDisplayInfo
        {
            Name = new FixedString64Bytes(displayName),
            Description = new FixedString128Bytes("Test unit.")
        });
        return entity;
    }
}
#endif
