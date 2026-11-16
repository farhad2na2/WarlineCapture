using System;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;

internal static class S004SourcePresetDayControl
{
    private static void Main(string[] args)
    {
        if(args.Length!=4)throw new ArgumentException("setup and preset paths/hashes required");
        var setup=SourceS004CompiledSetupInput.Read(args[0],args[1]);var em=new EntityManager();int next=1;
        em.LogicalAllocatorAdmitted=true;em.AllocateLogicalIdentity=()=>new Entity{Index=next++,Version=1};
        var session=em.CreateEntity();var id=new FixedString64Bytes("s004-preset-control");
        em.AddComponentData(session,new SkirmishExpandedSessionComponent {SessionId=id});
        void Reject(string label,string hash) {try{AutonomousWorld.ResolveS004SourcePresetDay(em,session,setup,args[2],hash);}catch(InvalidOperationException){return;}throw new Exception("Accepted invalid "+label);}
        Reject("missing match",args[3]);
        em.AddComponentData(session,new SkirmishMatchState {SessionId=id,ScenarioIndex=6,Seed=setup.Seed});
        if(AutonomousWorld.ResolveS004SourcePresetDay(em,session,setup,args[2],args[3])!=120f)throw new Exception("Wrong selected day");
        Reject("hash",new string('0',64));
        var state=em.GetComponentData<SkirmishMatchState>(session);state.ScenarioIndex=5;em.SetComponentData(session,state);Reject("scenario",args[3]);
        state.ScenarioIndex=6;state.Seed++;em.SetComponentData(session,state);Reject("seed",args[3]);
        state.Seed=setup.Seed;state.SessionId=new FixedString64Bytes("other");em.SetComponentData(session,state);Reject("session",args[3]);
        state.SessionId=id;em.SetComponentData(session,state);var duplicate=em.CreateEntity();em.AddComponentData(duplicate,state);Reject("duplicate",args[3]);
        Console.WriteLine("[S004SourcePresetDayControl] result=Passed seconds=120 negativeCases=6 fixtureOnly=1 nativeReadiness=0");
    }
}
