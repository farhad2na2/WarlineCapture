using System;
using Game.Runtime;
using Game.Simulation;
using Unity.Entities;

// Focused source-owner fixture. No scene, actual prefab, placement or mission is loaded.
internal static class SourceHelipadNonBlockingControl
{
    static void Need(bool value,string label) { if(!value)throw new Exception(label); }
    static void Check(RuntimeDefinition definition,bool expectedBlocking,bool deferred)
    {
        var owner=new BuildingRuntimeEntityCompositionSystemHelper();
        bool actual=owner.ShouldRuntimeBuildingBlockPathing(definition);
        Need(actual==expectedBlocking,"original pathing decision");
        int removed=0,redirected=0,deferredRedirects=0,created=0,committed=0;
        bool actorPathing=false;
        var decision=BuildingRegistrationKernel.Register(new BuildingRegistrationKernel.Context {
            DeferSideEffects=()=>deferred, AllocateId=()=>7, PrepareSourceData=id=>Need(id==7,"allocated ID"),
            ShouldBlockPathing=()=>owner.ShouldRuntimeBuildingBlockPathing(definition),
            RemoveOverlappingBlockers=()=>removed++, CreateActorsAndFoundation=(id,blocking)=>{created++;actorPathing=blocking;},
            AddDeferredRedirectFootprint=()=>deferredRedirects++, RedirectUnits=()=>redirected++,
            CommitData=(id,defer)=>committed++ },true);
        Need(decision.PathBlocking==expectedBlocking && actorPathing==expectedBlocking && created==1 && committed==1,"registration decision");
        Need(removed==(expectedBlocking?1:0),"overlapping blocker removal");
        Need(redirected==(expectedBlocking&&!deferred?1:0),"immediate redirect");
        Need(deferredRedirects==(expectedBlocking&&deferred?1:0),"deferred redirect");
    }
    static void Main()
    {
        var helipad=new RuntimeDefinition { DisplayName="Helipad",Prefab=new SourceUnitPrefab("Building_Helipad",new Entity { Index=1,Version=1 }) };
        var barracks=new RuntimeDefinition { DisplayName="Barracks",Prefab=new SourceUnitPrefab("Building_Barracks",new Entity { Index=2,Version=1 }) };
        Check(helipad,false,false);Check(helipad,false,true);
        Check(barracks,true,false);Check(barracks,true,true);
        Console.WriteLine("[SourceHelipadNonBlockingControl] result=Passed helipad=NonBlocking barracks=Blocking immediate=2 deferred=2 fixtureOnly=1 gameplay=0 readiness=0");
    }
}
