using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;
using Game.Runtime;

public sealed class AirAttackGateBypassTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            AirAttackGateBypassTests tests = new();
            tests.UnitTargetOrderSystem_AirUnitIgnoresBaseBreachAndEngagesFinalTarget();
            tests.AttackOrderCommandSystem_AirSourceDoesNotResolveBaseBreach();
            Debug.Log("[AirAttackGateBypassValidation] result=Passed tests=2");
            ValidationExit.Passed();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[AirAttackGateBypassValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void UnitTargetOrderSystem_AirUnitIgnoresBaseBreachAndEngagesFinalTarget()
    {
        using World world = new("AirAttackGateBypassTests_TargetOrder");
        EntityManager em = world.EntityManager;
        CreateGrid(em);

        Entity finalTarget = CreateRuntimeBuildingTarget(em, new int2(60, 8), new int2(4, 4), new float3(62f, 0f, 10f));
        Entity gateTarget = CreateRuntimeBuildingTarget(em, new int2(20, 8), new int2(2, 2), new float3(21f, 0f, 9f), isGate: true);
        Entity helicopter = CreateAirAttacker(em, new float3(2f, 12f, 9f));
        int resolverCalls = 0;

        NativeArray<Entity> selected = new(1, Allocator.Temp);
        UnitTargetOrderSystem.AttackOrderIssueResult result;
        try
        {
            selected[0] = helicopter;
            result = new UnitTargetOrderSystem().IssueAttackTarget(
                em,
                selected,
                finalTarget,
                (
                    byte _,
                    Entity __,
                    int2 ___,
                    int2 ____,
                    out Entity breach,
                    out int2 breachCell,
                    out float3 breachPosition) =>
                {
                    resolverCalls++;
                    breach = gateTarget;
                    breachCell = new int2(21, 9);
                    breachPosition = new float3(21f, 0f, 9f);
                    return true;
                });
        }
        finally
        {
            selected.Dispose();
        }

        Assert.IsTrue(result.CommandResult.Accepted);
        Assert.AreEqual(1, result.IssuedCount);
        Assert.AreEqual(0, resolverCalls, "Air units should not even ask for base-breach routing.");
        Assert.IsTrue(em.HasComponent<EngageTarget>(helicopter));
        EngageTarget engage = em.GetComponentData<EngageTarget>(helicopter);
        Assert.AreEqual(finalTarget, engage.Target, "Helicopters should attack the requested target directly instead of retargeting to the gate.");
        Assert.AreEqual(new int2(62, 10), engage.Cell);
        Assert.IsFalse(em.HasComponent<BaseBreachOrder>(helicopter));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(helicopter));
    }

    [Test]
    public void AttackOrderCommandSystem_AirSourceDoesNotResolveBaseBreach()
    {
        using World world = new("AirAttackGateBypassTests_CommandSystem");
        EntityManager em = world.EntityManager;
        CreateGrid(em);

        Entity finalTarget = CreateRuntimeBuildingTarget(em, new int2(72, 12), new int2(4, 4), new float3(74f, 0f, 14f));
        Entity gateTarget = CreateRuntimeBuildingTarget(em, new int2(18, 12), new int2(2, 2), new float3(19f, 0f, 13f), isGate: true);
        Entity helicopter = CreateAirAttacker(em, new float3(3f, 12f, 13f));
        int resolverCalls = 0;

        AttackOrderCommandSystem.Result result = new AttackOrderCommandSystem().IssueAttackTarget(
            em,
            finalTarget,
            (
                byte _,
                Entity __,
                int2 ___,
                int2 ____,
                out Entity breach,
                out int2 breachCell,
                out float3 breachPosition) =>
            {
                resolverCalls++;
                breach = gateTarget;
                breachCell = new int2(19, 13);
                breachPosition = new float3(19f, 0f, 13f);
                return true;
            },
            (EntityManager _, System.Collections.Generic.List<Entity> sources) => sources.Add(helicopter));

        Assert.IsTrue(result.Issued);
        Assert.AreEqual(0, resolverCalls, "The UI attack command path should skip gate/breach lookup for helicopters.");
        Assert.IsTrue(em.HasComponent<EngageTarget>(helicopter));
        EngageTarget engage = em.GetComponentData<EngageTarget>(helicopter);
        Assert.AreEqual(finalTarget, engage.Target);
        Assert.IsFalse(em.HasComponent<BaseBreachOrder>(helicopter));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(helicopter));
    }

    private static void CreateGrid(EntityManager em)
    {
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 128,
            Height = 128,
            CellSize = 1f,
            Origin = float3.zero
        });
    }

    private static Entity CreateAirAttacker(EntityManager em, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitFootprint),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(UnitAirMovement),
            typeof(UnitAirComponent),
            typeof(LocalTransform));

        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = new int2((int)math.floor(position.x), (int)math.floor(position.z)) });
        em.SetComponentData(entity, new UnitMove { Speed = 12f, WalkSpeed = 12f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1, AggroRangeCells = 64, ChaseBreakDistance = 80f });
        em.SetComponentData(entity, new UnitAttack { Range = 16f, CooldownSeconds = 1f, Damage = 10 });
        em.SetComponentData(entity, new UnitAirMovement { CruiseHeight = 12f, RunwayTaxiSpeed = 0f });
        em.SetComponentData(entity, new UnitAirComponent
        {
            HomePosition = new float3(position.x, 0f, position.z),
            HomeCell = new int2((int)math.floor(position.x), (int)math.floor(position.z)),
            HomeInitialized = 1,
            Airborne = 1
        });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateRuntimeBuildingTarget(
        EntityManager em,
        int2 originCell,
        int2 footprintCells,
        float3 position,
        bool isGate = false)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(UnitRespawnPrefab),
            typeof(RuntimeBuildingCombatTag),
            typeof(RuntimeBuildingCombatInfo),
            typeof(LocalTransform));

        em.SetComponentData(entity, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = originCell + footprintCells / 2 });
        em.SetComponentData(entity, new UnitFootprint { Size = footprintCells });
        em.SetComponentData(entity, new UnitHealth { Current = 250, Max = 250 });
        em.SetComponentData(entity, new UnitRespawnPrefab { Prefab = Entity.Null });
        em.SetComponentData(entity, new RuntimeBuildingCombatInfo
        {
            OwnerFactionId = FactionIdentity.EnemyFactionId,
            OriginCell = originCell,
            FootprintCells = footprintCells,
            IsWall = 0,
            IsGate = isGate ? (byte)1 : (byte)0
        });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }
}
