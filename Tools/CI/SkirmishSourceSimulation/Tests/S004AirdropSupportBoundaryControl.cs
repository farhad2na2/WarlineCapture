using System;
using Game.Components;
using Unity.Entities;

internal static class S004AirdropSupportBoundaryControl
{
    private static void Main()
    {
        int next = 1;
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var session = em.CreateEntity();
        em.AddComponentData(session, new SupportSessionComponent());
        try { S004AirdropOwnerDiagnostic.Run(em); }
        catch (NotSupportedException error) when (error.Message == "Support state requires its original flight and paratrooper owners")
        {
            Console.WriteLine("[S004AirdropSupportBoundary] result=Passed supportSession=Rejected scope=SourceFixtureOnly");
            return;
        }
        throw new InvalidOperationException("Support session was not rejected");
    }
}
