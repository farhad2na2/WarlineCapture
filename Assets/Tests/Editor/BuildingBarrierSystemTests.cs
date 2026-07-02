using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class BuildingBarrierUtilitySystemHelperTests
{
    private World _world;
    private EntityManager _entityManager;
    private GameObject _doorObject;

    [SetUp]
    public void SetUp()
    {
        _world = new World("BuildingBarrierUtilitySystemHelperTests");
        _entityManager = _world.EntityManager;
        _doorObject = new GameObject("Door_Z");
    }

    [TearDown]
    public void TearDown()
    {
        if (_world != null && _world.IsCreated)
            _world.Dispose();
        if (_doorObject != null)
            Object.DestroyImmediate(_doorObject);
    }

    [Test]
    public void UpdateRoadBarrierDoors_KeepsGateClosedWithoutNearbyFriendlyUnits()
    {
        BuildingBarrierUtilitySystemHelper system = new();
        RuntimeBuildingEntity gate = CreateGate(ownerFactionId: 1);
        BuildingBarrierUtilitySystemHelper.Context context = CreateContext(system, gate);

        system.UpdateRoadBarrierDoors(context, 1f);

        Assert.That(gate.DoorOpen01, Is.EqualTo(0f).Within(0.001f));
        Assert.That(NormalizeSignedAngle(_doorObject.transform.localEulerAngles.z), Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void UpdateRoadBarrierDoors_OpensGateForNearbyOwnerFactionUnit()
    {
        BuildingBarrierUtilitySystemHelper system = new();
        RuntimeBuildingEntity gate = CreateGate(ownerFactionId: 1);
        BuildingBarrierUtilitySystemHelper.Context context = CreateContext(system, gate);
        CreateLiveUnit(factionId: 1, cell: new int2(12, 12), footprint: new int2(1, 1));

        system.UpdateRoadBarrierDoors(context, 1f);

        Assert.That(gate.DoorOpen01, Is.GreaterThan(0.5f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, NormalizeSignedAngle(_doorObject.transform.localEulerAngles.z))), Is.GreaterThan(20f));
    }

    public static void RunFocusedValidation()
    {
        RunCase(nameof(UpdateRoadBarrierDoors_KeepsGateClosedWithoutNearbyFriendlyUnits),
            test => test.UpdateRoadBarrierDoors_KeepsGateClosedWithoutNearbyFriendlyUnits());
        RunCase(nameof(UpdateRoadBarrierDoors_OpensGateForNearbyOwnerFactionUnit),
            test => test.UpdateRoadBarrierDoors_OpensGateForNearbyOwnerFactionUnit());
        Debug.Log("[BuildingBarrierFocusedValidation] result=Passed tests=2");
    }

    private static void RunCase(string name, System.Action<BuildingBarrierUtilitySystemHelperTests> action)
    {
        BuildingBarrierUtilitySystemHelperTests tests = new();
        tests.SetUp();
        try
        {
            action(tests);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[BuildingBarrierFocusedValidation] result=Failed test={name} error={ex}");
            throw;
        }
        finally
        {
            tests.TearDown();
        }
    }

    private RuntimeBuildingEntity CreateGate(byte ownerFactionId)
    {
        return new RuntimeBuildingEntity
        {
            Id = 1,
            Definition = new BuildingDefinition
            {
                DisplayName = "Building_Road_Barrier",
                FootprintCells = new Vector2Int(4, 2)
            },
            OriginCell = new Vector2Int(10, 10),
            DoorZ = _doorObject.transform,
            DoorClosedLocalEulerZ = 0f,
            DoorOpenLocalEulerZ = 90f,
            DoorOpen01 = 0f,
            HasOwnerFaction = true,
            OwnerFactionId = ownerFactionId
        };
    }

    private BuildingBarrierUtilitySystemHelper.Context CreateContext(BuildingBarrierUtilitySystemHelper system, RuntimeBuildingEntity gate)
    {
        Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
        {
            { gate.Id, gate }
        };
        EntityQuery liveUnitsQuery = _entityManager.CreateEntityQuery(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint));

        return new BuildingBarrierUtilitySystemHelper.Context(
            runtimeBuildings,
            tryGetEntityManager: (out EntityManager em) =>
            {
                em = _entityManager;
                return true;
            },
            tryGetGridData: null,
            ensureEntityQueries: null,
            getLiveFactionUnitsQuery: () => liveUnitsQuery,
            isWallGateDefinition: system.IsWallGateDefinitionCached,
            tryGetRuntimeBuildingApproachCell: null);
    }

    private void CreateLiveUnit(byte factionId, int2 cell, int2 footprint)
    {
        Entity entity = _entityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint));
        _entityManager.SetComponentData(entity, new Faction { Id = factionId });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = cell });
        _entityManager.SetComponentData(entity, new UnitFootprint { Size = footprint });
    }

    private static float NormalizeSignedAngle(float angle)
    {
        return Mathf.DeltaAngle(0f, angle);
    }
}
#endif
