using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Entities;

internal static class SourceSharedLogicalSpawnControl
{
    static void Need(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
    static void Main()
    {
        var em=new EntityManager { LogicalAllocatorAdmitted=true }; int next=1;
        em.AllocateLogicalIdentity=()=>new Entity {Index=next++,Version=1};
        var setup=new SkirmishResolvedSetup {DeadlineSeconds=725};
        var entity=em.CreateEntity();
        var session=new SkirmishExpandedSessionComponent {Phase=SkirmishSessionPhase.Spawning,Seed=123};
        em.AddComponentData(entity,session);
        em.AddComponentData(entity,new SkirmishObjectiveStateComponent {State=SkirmishObjectiveStateKind.Pending});
        SkirmishScenarioSpawnSystem.CommitLogicalSpawn(em,entity,setup,ref session,1);
        var actual=em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
        var clock=em.GetComponentData<SkirmishObjectiveClockComponent>(entity);
        Need(actual.SpawnComplete==1 && actual.SpawnVisualPending==1 && actual.Phase==SkirmishSessionPhase.Playing && actual.Seed==123,"Logical session transition differs");
        Need(em.GetComponentData<SkirmishObjectiveStateComponent>(entity).State==SkirmishObjectiveStateKind.Active,"Existing objective did not activate");
        Need(clock.ElapsedSeconds==0 && clock.DeadlineSeconds==725 && clock.Paused==0 && clock.Playing==1,"New original clock differs");
        var existing=em.CreateEntity(); var nextSession=new SkirmishExpandedSessionComponent {Phase=SkirmishSessionPhase.Spawning}; em.AddComponentData(existing,nextSession);
        var prior=new SkirmishObjectiveClockComponent {ElapsedSeconds=8,DeadlineSeconds=44,Paused=1,Playing=0};em.AddComponentData(existing,prior);
        SkirmishScenarioSpawnSystem.CommitLogicalSpawn(em,existing,setup,ref nextSession,0);
        var kept=em.GetComponentData<SkirmishObjectiveClockComponent>(existing);
        Need(!em.HasComponent<SkirmishObjectiveStateComponent>(existing) && kept.ElapsedSeconds==8 && kept.DeadlineSeconds==44 && kept.Paused==1 && kept.Playing==0,"Existing clock or absent objective changed");
#if WARLINE_S004_SOURCE_OUTER
        bool rejected=false;try {SourceS004OuterBoundaries.DeferNativePresentation(em,existing,out _);}catch(InvalidOperationException){rejected=true;}
        Need(rejected,"Source presentation deferral must reject without same-world opening authority");
#endif
        Console.WriteLine("[SourceSharedLogicalSpawnControl] result=Passed fixtureOnly=1 nativeReadiness=0");
    }
}
