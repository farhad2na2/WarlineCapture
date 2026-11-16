using System;
using Game.Components;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;

internal static class S004TransportDeployAttackOwnerControl
{
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static void Main()
    {
        int next = 100;
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var target = em.CreateEntity();
        var transport = em.CreateEntity();
        var nestedTransport = em.CreateEntity();
        var passenger = em.CreateEntity();
        em.AddComponentData(passenger, new UnitCombat { CanAttack = 1 });
        em.AddComponentData(passenger, new UnitAttack { Damage = 10 });
        em.AddBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = nestedTransport });
        em.AddBuffer<UnitTransportPassengerElement>(nestedTransport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        em.AddComponentData(transport, new UnitTransportDeployAttackTarget { TargetEntity = target, TargetCell = new int2(3, 4) });
        var owner = new UnitTransportDeployAttackSystem();
        var state = new SystemState { EntityManager = em, Enabled = true };
        state.Begin(); owner.OnCreate(ref state); state.End();
        state.Begin(); owner.OnUpdate(ref state); state.End();
        Require(!em.HasComponent<UnitTransportDeployAttackTarget>(transport), "Attack intent was not consumed");
        Require(em.HasComponent<UnitTransportDeployOrder>(transport), "Nested attack-capable cargo did not produce a deploy order");
        var order = em.GetComponentData<UnitTransportDeployOrder>(transport);
        Require(order.TargetEntity == target && order.TargetCell.Equals(new int2(3, 4)) && order.AttackAfterDeploy == 1, "Original deploy order differs");
        Console.WriteLine("[S004TransportDeployAttackOwnerControl] result=Passed cases=1 nestedPayload=Passed scope=SourceFixtureOnly");
    }
}
