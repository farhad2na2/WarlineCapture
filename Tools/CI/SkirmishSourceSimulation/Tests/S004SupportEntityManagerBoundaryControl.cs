using System;
using System.Linq;
using Game.Components;
using Unity.Collections;
using Unity.Entities;

internal static class S004SupportEntityManagerBoundaryControl
{
    private static void Main()
    {
        int next = 1; var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var first = em.CreateEntity(); var second = em.CreateEntity();
        em.AddComponentData(first, new SupportSmokeZoneComponent());
        em.AddComponentData(second, new SupportSmokeZoneComponent());
        em.AddBuffer<SupportRequestElement>(first);
        using (var types = em.GetComponentTypes(first, Allocator.Temp))
        {
            if (types.Length != 2 || !types.Contains(ComponentType.ReadWrite<SupportSmokeZoneComponent>()) ||
                !types.Contains(ComponentType.ReadWrite<SupportRequestElement>()))
                throw new InvalidOperationException("Component enumeration omitted a component or buffer");
        }
        using (var query = em.CreateEntityQuery(typeof(SupportSmokeZoneComponent))) em.AddComponent<SupportRangedCoverComponent>(query);
        if (!em.HasComponent<SupportRangedCoverComponent>(first) || !em.HasComponent<SupportRangedCoverComponent>(second))
            throw new InvalidOperationException("Query component addition did not cover its rows");
        em.RemoveComponent(first, ComponentType.ReadWrite<SupportRequestElement>());
        if (em.HasBuffer<SupportRequestElement>(first) || !em.HasComponent<SupportSmokeZoneComponent>(first))
            throw new InvalidOperationException("Typed component removal changed the wrong row");
        Console.WriteLine("[S004SupportEntityManagerBoundary] result=Passed enumeration=2 queryAdd=2 bufferRemoval=Passed scope=SourceFixtureOnly");
    }
}
