using System;
using System.Linq;
using Game.Components;
using Unity.Entities;

internal static class S004OwnedSingleQueryOptionsControl
{
    private static void Main()
    {
        int next = 1;
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var active = em.CreateEntity(); var disabled = em.CreateEntity(); var prefab = em.CreateEntity();
        foreach (var entity in new[] { active, disabled, prefab }) em.AddComponentData(entity, new SupportSessionComponent());
        em.AddComponent<Disabled>(disabled); em.AddComponent<Prefab>(prefab);
        SystemAPI.Bind(em);
        int normal = SystemAPI.Query<RefRO<SupportSessionComponent>>().WithEntityAccess().Count();
        int withDisabled = SystemAPI.Query<RefRO<SupportSessionComponent>>()
            .WithOptions(EntityQueryOptions.IncludeDisabledEntities).WithEntityAccess().Count();
        if (normal != 1 || withDisabled != 2) throw new InvalidOperationException("Single-component query options changed its entity domain");
        try { SystemAPI.Query<RefRO<SupportSessionComponent>>().WithOptions(EntityQueryOptions.IncludePrefab); }
        catch (NotSupportedException) {
            Console.WriteLine("[S004SingleQueryOptions] result=Passed default=1 includeDisabled=2 unsupported=Rejected"); return;
        }
        throw new InvalidOperationException("Unsupported query options must fail closed");
    }
}
