#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class SelectionUiQuerySystemTests
{
    private World _world;
    private EntityManager _entityManager;
    private SelectionUiQuerySystem _querySystem;

    [SetUp]
    public void SetUp()
    {
        _world = new World("SelectionUiQuerySystemTests");
        _entityManager = _world.EntityManager;
        _querySystem = new SelectionUiQuerySystem();
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

        Assert.AreEqual("Scout Team", _querySystem.ResolveFocusedUnitName(_entityManager, entity));
        Assert.AreEqual("Fast reconnaissance infantry.", _querySystem.ResolveFocusedUnitDescription(_entityManager, entity));
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

        Assert.IsTrue(_querySystem.TryGetFocusedUnitCapacityInfo(_entityManager, entity, 15f, out int current, out int max, out float progress01));
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

        Assert.AreEqual(SelectionUiQuerySystem.FocusedUnitUiStatus.ReturningToBase, _querySystem.GetFocusedUnitUiStatus(_entityManager, airUnit));
        Assert.AreEqual(SelectionUiQuerySystem.FocusedUnitUiStatus.MissileLaunched, _querySystem.GetFocusedUnitUiStatus(_entityManager, missileUnit));
        Assert.AreEqual(SelectionUiQuerySystem.FocusedUnitUiStatus.MissileLaunched, _querySystem.GetFocusedUnitUiStatus(_entityManager, commandedMissileUnit));
        Assert.AreEqual(SelectionUiQuerySystem.FocusedUnitUiStatus.Idle, _querySystem.GetFocusedUnitUiStatus(_entityManager, missilePreparingUnit));
        Assert.AreEqual(SelectionUiQuerySystem.FocusedUnitUiStatus.Idle, _querySystem.GetFocusedUnitUiStatus(_entityManager, autoTargetMissileUnit));
        Assert.AreEqual(SelectionUiQuerySystem.FocusedUnitUiStatus.Engaged, _querySystem.GetFocusedUnitUiStatus(_entityManager, engagedUnit));
        Assert.AreEqual(SelectionUiQuerySystem.FocusedUnitUiStatus.Moving, _querySystem.GetFocusedUnitUiStatus(_entityManager, movingUnit));
        Assert.AreEqual(SelectionUiQuerySystem.FocusedUnitUiStatus.Idle, _querySystem.GetFocusedUnitUiStatus(_entityManager, holdingUnit));
        Assert.AreEqual(SelectionUiQuerySystem.FocusedUnitUiStatus.Idle, _querySystem.GetFocusedUnitUiStatus(_entityManager, manualGuardUnit));
        StringAssert.Contains("MISSILE LAUNCHED", _querySystem.ResolveHudSelectionStatus(_entityManager, missileUnit));
        StringAssert.Contains("MISSILE LAUNCHED", _querySystem.ResolveHudSelectionStatus(_entityManager, commandedMissileUnit));
        StringAssert.Contains("IDLE", _querySystem.ResolveHudSelectionStatus(_entityManager, missilePreparingUnit));
        StringAssert.Contains("IDLE", _querySystem.ResolveHudSelectionStatus(_entityManager, autoTargetMissileUnit));
        StringAssert.Contains("HOLDING", _querySystem.ResolveHudSelectionStatus(_entityManager, holdingUnit));
        StringAssert.Contains("IDLE", _querySystem.ResolveHudSelectionStatus(_entityManager, manualGuardUnit));
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

            Assert.IsTrue(_querySystem.TryGetSelectedUnitsPortraitPose(_entityManager, selected, Entity.Null, out Vector3 center, out _, out float radius));
            Assert.AreEqual(new Vector3(5f, 0f, 0f), center);
            Assert.AreEqual(6.5f, radius, 0.001f);
        }
        finally
        {
            selected.Dispose();
        }
    }

    private Entity CreatePoseEntity(float3 position)
    {
        Entity entity = _entityManager.CreateEntity(typeof(LocalTransform), typeof(LocalToWorld));
        _entityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
        _entityManager.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
        return entity;
    }
}
#endif
