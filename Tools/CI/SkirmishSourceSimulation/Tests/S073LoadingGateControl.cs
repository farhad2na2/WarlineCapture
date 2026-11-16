using System;
using Game.Components;
using Game.Runtime;
using Unity.Entities;

internal static class S073LoadingGateControl
{
    private static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static void Main()
    {
        var em = new EntityManager();
        int next = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        bool Gate(bool initialized, bool play, bool buildings, bool? city, bool? blockers, bool? decorations) =>
            SourceS073LoadingGate.Evaluate(em, initialized, play, buildings, city, blockers, decorations);
        Need(!Gate(false, true, true, null, null, null), "Uninitialized gameplay must wait");
        Need(!Gate(true, false, true, null, null, null), "Missing play request must wait");
        Need(!Gate(true, true, false, null, null, null), "Incomplete building startup must wait");
        Need(!Gate(true, true, true, false, null, null), "Existing unspawned city must wait");
        Need(!Gate(true, true, true, null, false, null), "Existing unspawned blockers must wait");
        Need(!Gate(true, true, true, null, null, false), "Existing unspawned decorations must wait");
        Need(!Gate(true, true, true, false, false, false), "Unknown source-host runtime owners must fail closed");
        Need(Gate(true, true, true, null, null, null), "No initial spawn configs must pass original predicate");
        var plan = em.CreateEntity();
        em.AddComponentData(plan, new InitialUnitsSpawnConfig());
        Need(!Gate(true, true, true, true, true, true), "Unfinished spawn plan must wait");
        em.AddComponentData(plan, new InitialUnitsSpawnInitialized());
        Need(Gate(true, true, true, true, true, true), "Completed spawn plan must pass original predicate");
        var second = em.CreateEntity();
        em.AddComponentData(second, new InitialUnitsSpawnConfig());
        Need(!Gate(true, true, true, true, true, true), "Every present spawn plan must be initialized");
        Need(GameplayStartCompletionPredicate.GameplayStartRequested(true, true) &&
             GameplayStartCompletionPredicate.BuildingStartupComplete(true) &&
             GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(false, false) &&
             GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(true, true) &&
             GameplayStartCompletionPredicate.RuntimeWorldAvailable(true, true),
             "Native all-ready scalar truth-table row must remain true");
        Need(!GameplayStartCompletionPredicate.GameplayStartRequested(true, false),
             "Native missing-play truth-table row must remain false");
        Need(!GameplayStartCompletionPredicate.RuntimeWorldAvailable(true, false),
             "Native unavailable-world truth-table row must remain false");
        Need(GameplayStartCompletionPredicate.InitialSpawnConfigsComplete(0, 0),
             "Native zero-config truth-table row must remain true");
        Need(GameplayStartCompletionPredicate.InitialSpawnConfigsComplete(1, 1),
             "Native fully-initialized truth-table row must remain true");
        Need(!GameplayStartCompletionPredicate.InitialSpawnConfigsComplete(2, 1),
             "Native partial-initialization truth-table row must remain false");
        Console.WriteLine("[S073LoadingGateControl] result=Passed fixtureOnly=1 runtimeAdmission=0");
    }
}
