using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Entities;

internal static class SourceUiFinishedOwnerControl
{
    static void Need(bool value, string label) { if (!value) throw new Exception(label); }
    static void Main()
    {
        var em = new EntityManager(); int next = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var missingMatch = em.CreateEntity();
        em.AddComponentData(missingMatch, new SkirmishExpandedSessionComponent());
        em.AddComponentObject(missingMatch, new SkirmishResolvedSetupRecord { Setup = new SkirmishResolvedSetup() });
        var army = (SkirmishArmyProfileConfig)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SkirmishArmyProfileConfig));
        bool missingMatchRejected = false;
        try { PublicCommandCatalog.Read(em, missingMatch, army); }
        catch (NotSupportedException error) { missingMatchRejected = error.Message == "Actual same-session match state required for public UI flags"; }
        Need(missingMatchRejected, "missing match state must fail closed at the public catalog");
        var session = em.CreateEntity();
        em.AddComponentData(session, new SkirmishMatchState { Phase = SkirmishPhase.Playing });
        var setup = new SkirmishResolvedSetup();
        bool SourceFinished() => SourceUiPublicFlags.Read(setup, SkirmishAriaPublicProjection.FromSession(em, session, null), em.GetComponentData<SkirmishMatchState>(session)).Finished;
        bool MatchFinished() => em.GetComponentData<SkirmishMatchState>(session).Phase == SkirmishPhase.Finished;
        Need(!SourceFinished() && !MatchFinished(), "initial playing state");
        em.AddComponentData(session, new SkirmishResultComponent { Frozen = 0 });
        Need(!SourceFinished() && !MatchFinished(), "non-frozen result must follow the actual match phase");
        SkirmishExpandedSessionControlService.ProjectTerminalMatch(em, session);
        Need(!SourceFinished() && !MatchFinished(), "original owner must not project an unfrozen result");
        em.SetComponentData(session, new SkirmishResultComponent { Frozen = 1, Outcome = SkirmishOutcomeKind.Victory });
        SkirmishExpandedSessionControlService.ProjectTerminalMatch(em, session);
        Need(SourceFinished() && MatchFinished(), "frozen result must be projected by original owner");
        Console.WriteLine("[SourceUiFinishedOwnerControl] result=Passed missingMatch=Rejected initial=Aligned nonFrozen=Aligned frozenProjection=Aligned fixtureOnly=1 nativeParity=0 readiness=0");
    }
}
