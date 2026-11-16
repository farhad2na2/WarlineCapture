using System;
using System.Collections.Generic;
using Game.Runtime;
using Unity.Entities;

internal static class StrictSourceEntityReferenceRemapperControl
{
    private struct Nested { public Entity Target; }
    private struct Component { public int Value; public Nested Link; }
    private struct UnsupportedComponent { public string ManagedReference; }

    private static void Need(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Reject(Action action, string message)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new Exception(message);
    }

    private static void RejectUnsupported(Action action, string message)
    {
        try { action(); }
        catch (NotSupportedException) { return; }
        throw new Exception(message);
    }

    public static int Main()
    {
        var source = new HashSet<Entity> { new Entity(1, 1), new Entity(2, 1) };
        var target = new HashSet<Entity> { new Entity(11, 3), new Entity(12, 3) };
        var map = new Dictionary<Entity, Entity> { [new Entity(1, 1)] = new Entity(11, 3), [new Entity(2, 1)] = new Entity(12, 3) };
        var original = new Component { Value = 7, Link = new Nested { Target = new Entity(2, 1) } };
        var copy = StrictSourceEntityReferenceRemapper.Remap(original, map, source.Contains, target.Contains);
        Need(copy.Value == 7 && copy.Link.Target == new Entity(12, 3), "Nested Entity reference was not remapped");
        Need(original.Link.Target == new Entity(2, 1), "Source value was mutated");

        var missing = new Dictionary<Entity, Entity> { [new Entity(1, 1)] = new Entity(11, 3) };
        Reject(() => StrictSourceEntityReferenceRemapper.Remap(original, missing, source.Contains, target.Contains),
            "Missing entity reference mapping was accepted");
        Reject(() => StrictSourceEntityReferenceRemapper.Remap(original, map, _ => false, target.Contains),
            "Source-domain mismatch was accepted");
        Reject(() => StrictSourceEntityReferenceRemapper.Remap(original, map, source.Contains, _ => false),
            "Target-domain mismatch was accepted");
        Reject(() => StrictSourceEntityReferenceRemapper.Remap(original,
            new Dictionary<Entity, Entity> { [new Entity(1, 1)] = new Entity(11, 3), [new Entity(2, 1)] = new Entity(11, 3) },
            source.Contains, target.Contains), "Non-injective target map was accepted");
        RejectUnsupported(() => StrictSourceEntityReferenceRemapper.Remap(
            new UnsupportedComponent { ManagedReference = "shared" }, map, source.Contains, target.Contains),
            "Managed reference storage was accepted");
        Console.WriteLine("[StrictSourceEntityReferenceRemapperControl] result=Passed entityRemap=1 missingReferenceRejected=1 domainChecks=2 unsupportedTypeRejected=1");
        return 0;
    }
}
