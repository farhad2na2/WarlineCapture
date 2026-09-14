using Game.Components;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.Transforms;
public sealed partial class M04AirliftIntegrationTests
{
    private static bool ReadTutorialTarget(Unity.Entities.EntityManager em,Unity.Entities.Entity root,int step,
        out Game.UI.Contracts.UiMissionTutorialTarget target)
    {
        var method=typeof(UiShellEcsGateway).GetMethod("ResolveExtractionTutorialTarget",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
        object[] args={em,root,step,null}; bool result=(bool)method.Invoke(null,args);
        target=(Game.UI.Contracts.UiMissionTutorialTarget)args[3]; return result;
    }

    [Test]
    public void BilingualGuidanceCopyFitsNarrationMessages()
    {
        foreach(var lesson in Game.Configs.M04AirliftCopyCatalog.Lessons)
            foreach(var text in new[]{lesson.Body,lesson.PersianBody})
                Assert.DoesNotThrow(()=>{var message=new Unity.Collections.FixedString512Bytes(text);},"Recorded lesson copy must fit in both languages.");
        foreach(var entry in Game.Configs.M04AirliftCopyCatalog.Ui)
            if(entry.Key=="mission.m04.tutorial.selection_progress")
                foreach(var text in new[]{entry.English,entry.Persian})
                    Assert.DoesNotThrow(()=>{var message=new Unity.Collections.FixedString512Bytes(string.Format(text,3));});
    }

    [Test]
    public void CommandStateReportsPersistentSelectionModeAndClearsAfterExit()
    {
        var previous=Unity.Entities.World.DefaultGameObjectInjectionWorld;
        using var world=new Unity.Entities.World("M4 selection mode");
        try
        {
            Unity.Entities.World.DefaultGameObjectInjectionWorld=world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            var em=world.EntityManager;
            em.CreateEntity(typeof(Game.UI.Shell.Contracts.Ecs.UiShellRootComponent));
            var input=em.CreateEntity(typeof(RtsSelectionInputStateComponent));
            var gameplay=em.CreateEntity(typeof(RuntimeGameplayStateComponent));
            em.SetComponentData(gameplay,new RuntimeGameplayStateComponent{SelectionModeActive=1});
            Assert.That(UiShellEcsGateway.TryReadMatchHudCommandState(out var state),Is.True);
            Assert.That(state.ActiveCommandMode,Is.EqualTo(Game.Tactical.Contracts.TacticalCommandMode.Select));
            em.SetComponentData(gameplay,new RuntimeGameplayStateComponent());
            UiShellEcsGateway.TryReadMatchHudCommandState(out state);
            Assert.That(state.ActiveCommandMode,Is.EqualTo(Game.Tactical.Contracts.TacticalCommandMode.None));
            em.SetComponentData(input,new RtsSelectionInputStateComponent{ActiveCommandMode=(int)Game.Tactical.Contracts.TacticalCommandMode.Board});
            UiShellEcsGateway.TryReadMatchHudCommandState(out state);
            Assert.That(state.ActiveCommandMode,Is.EqualTo(Game.Tactical.Contracts.TacticalCommandMode.Board));
        }
        finally
        {
            Unity.Entities.World.DefaultGameObjectInjectionWorld=previous;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void TutorialTargetsFollowSelectionTransportAndDestinationForEveryActionLesson()
    {
        using var r=new Roster();
        r.State.LandingCenter=new float3(120,0,30);
        r.Em.AddComponentData(r.Root,r.State);
        r.Em.SetComponentData(r.State.Carrier,LocalTransform.FromPosition(new float3(20,0,10)));
        r.Em.SetComponentData(r.State.Aircraft,LocalTransform.FromPosition(new float3(110,0,20)));
        foreach(int step in new[]{2,3,4,5,6,7,8,9,11})
        {
            Assert.That(ReadTutorialTarget(r.Em,r.Root,step,out var target),Is.True);
            Assert.That(target.NeedsSelection,Is.True,$"Step {step} must recover missing selection.");
            if(step is 6 or 7) Assert.That((float3)target.Destination,Is.EqualTo(r.State.LandingCenter));
            if(step==11) Assert.That((float3)target.Destination,Is.EqualTo(r.State.DepartureCenter));
            if(step==5) Assert.That(target.Destination.x,Is.EqualTo(20));
            if(step==9) Assert.That(target.Destination.x,Is.EqualTo(110));
        }
        r.Em.AddComponent<SelectedUnitTag>(r.State.Carrier);
        ReadTutorialTarget(r.Em,r.Root,6,out var move);
        Assert.That(move.NeedsSelection,Is.False);
        r.Em.AddComponent<UnitPathRequest>(r.State.Carrier);
        ReadTutorialTarget(r.Em,r.Root,6,out move);
        Assert.That(move.Moving,Is.True,"No repeated Move hint while the accepted trip is in progress.");
        for(int i=0;i<3;i++) r.Em.AddComponent<SelectedUnitTag>(r.People[i]);
        r.Em.SetComponentData(r.People[3],LocalTransform.FromPosition(new float3(90,0,7)));
        ReadTutorialTarget(r.Em,r.Root,4,out var partial);
        Assert.That(partial.RequiredSelectionCount,Is.EqualTo(4));
        Assert.That(partial.Selection.x,Is.EqualTo(90),"Partial-selection guidance must identify a missing specialist.");
        ReadTutorialTarget(r.Em,r.Root,9,out var board);
        Assert.That(board.NeedsSelection,Is.True,"Three of four specialists is not the boarding objective.");
        r.Em.AddComponent<SelectedUnitTag>(r.People[3]);
        ReadTutorialTarget(r.Em,r.Root,9,out board);
        Assert.That(board.NeedsSelection,Is.False);
        r.Board(r.State.Carrier);
        ReadTutorialTarget(r.Em,r.Root,9,out board);
        Assert.That(board.Selection.x,Is.EqualTo(20),"Hidden passengers follow their transport, not stale ground positions.");
        foreach(var person in r.People) r.Em.RemoveComponent<SelectedUnitTag>(person);
        ReadTutorialTarget(r.Em,r.Root,5,out var aboard);
        Assert.That(aboard.NeedsSelection,Is.False,"Boarded passengers must not trigger another Select command.");
        foreach(var person in r.People) r.Em.RemoveComponent<UnitTransportPassenger>(person);
        r.Em.SetComponentData(r.State.Carrier,new UnitHealth{Current=0,Max=100});
        Assert.That(ReadTutorialTarget(r.Em,r.Root,9,out board),Is.True,
            "Aircraft boarding guidance must survive loss of the no-longer-required carrier after unloading.");
        Assert.That(board.Destination.x,Is.EqualTo(110));
    }
}
