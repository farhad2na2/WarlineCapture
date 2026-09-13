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
        ReadTutorialTarget(r.Em,r.Root,9,out var board);
        Assert.That(board.NeedsSelection,Is.True,"Three of four specialists is not the boarding objective.");
        r.Em.AddComponent<SelectedUnitTag>(r.People[3]);
        ReadTutorialTarget(r.Em,r.Root,9,out board);
        Assert.That(board.NeedsSelection,Is.False);
        r.Board(r.State.Carrier);
        ReadTutorialTarget(r.Em,r.Root,9,out board);
        Assert.That(board.Selection.x,Is.EqualTo(20),"Hidden passengers follow their transport, not stale ground positions.");
        foreach(var person in r.People) r.Em.RemoveComponent<UnitTransportPassenger>(person);
        r.Em.SetComponentData(r.State.Carrier,new UnitHealth{Current=0,Max=100});
        Assert.That(ReadTutorialTarget(r.Em,r.Root,9,out board),Is.True,
            "Aircraft boarding guidance must survive loss of the no-longer-required carrier after unloading.");
        Assert.That(board.Destination.x,Is.EqualTo(110));
    }
}
