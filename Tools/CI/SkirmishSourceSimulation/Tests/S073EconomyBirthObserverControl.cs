using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Newtonsoft.Json.Linq;

internal static class S073EconomyBirthObserverControl
{
    static void Need(bool x,string message){if(!x)throw new InvalidOperationException(message);}
    static void Main()
    {
        const string flag="WARLINE_S073_ECONOMY_STARTUP_BIRTH_OBSERVER";
        var em=new EntityManager {LogicalAllocatorAdmitted=true};int next=1;
        em.AllocateLogicalIdentity=()=>new Entity{Index=next++,Version=1};
        int invocations=0;
        Environment.SetEnvironmentVariable(flag,null);
        Need(SourceS073EconomyStartupBirthObserver.Observe(em,"Fixture",()=>invocations++)==null && invocations==1,"Disabled observer must call supplied action once without a match");
        Environment.SetEnvironmentVariable(flag,"1");
        bool rejected=false;
        try {SourceS073EconomyStartupBirthObserver.Observe(em,"Fixture",()=>invocations++);}catch(InvalidOperationException){rejected=true;}
        Need(rejected && invocations==1,"Enabled observer must reject missing match before action");
        var match=em.CreateEntity();em.AddComponentData(match,new SkirmishMatchState {SessionId=new FixedString64Bytes("fixture"),Seed=104729,ScenarioIndex=3});
        var observed=SourceS073EconomyStartupBirthObserver.Observe(em,"Fixture",()=>{invocations++;var bank=em.CreateEntity();em.AddComponentData(bank,new FactionEconomy {FactionId=2,Money=17});});
        Need(invocations==2 && (int?)observed["created_bank_count"]==1 && (int?)observed["economy_bank_count_before"]==0 && (int?)observed["economy_bank_count_after"]==1,"Created bank observation differs");
        Need((bool?)observed["observer_mutated_ecs"]==false && (string)observed["session_id"]=="fixture" && (string)observed["owner_invocation"]=="Fixture","Observer identity or ownership differs");
        using var banks=em.CreateEntityQuery(typeof(FactionEconomy));Need(banks.CalculateEntityCount()==1 && banks.GetSingleton<FactionEconomy>().Money==17,"Observed fixture action did not persist exactly once");
        Environment.SetEnvironmentVariable(flag,null);
        Console.WriteLine("[S073EconomyBirthObserverControl] result=Passed fixtureOnly=1 nativeReadiness=0");
    }
}
