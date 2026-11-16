using System;
using System.IO;
using Game.Components;
using Game.Runtime;
using Unity.Entities;

internal static class S004ResourceDaySourceControl
{
    private static void Main(string[] args)
    {
        if (args.Length != 2) throw new ArgumentException("Pinned source path and hash required");
        var manager = new EntityManager(); int next = 1;
        manager.LogicalAllocatorAdmitted = true;
        manager.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        float seconds = AutonomousWorld.ResolveS004SourceOnlyResourceDay(manager, args[0], args[1]);
        if (seconds != 300f) throw new Exception("Original source fallback changed");
        bool badHash = false;
        try { AutonomousWorld.ResolveS004SourceOnlyResourceDay(manager, args[0], new string('0', 64)); }
        catch (InvalidOperationException) { badHash = true; }
        if (!badHash) throw new Exception("Changed source pin accepted");
        var legacy = manager.CreateEntity(); manager.AddComponentData(legacy, new SkirmishMatchState());
        bool legacyRejected = false;
        try { AutonomousWorld.ResolveS004SourceOnlyResourceDay(manager, args[0], args[1]); }
        catch (InvalidOperationException) { legacyRejected = true; }
        if (!legacyRejected) throw new Exception("Legacy preset override bypassed");
        Console.WriteLine("[S004ResourceDaySourceControl] result=Passed seconds=300 hashMismatch=Rejected legacyOverride=Rejected fixtureOnly=1 nativeDay=Unknown");
    }
}
