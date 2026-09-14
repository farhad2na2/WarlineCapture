using Game.Components;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
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
    public void EveryEntryStartsFullTutorialDespiteSavedGuidanceAndReplayToggle()
    {
        foreach(Game.Missions.Contracts.MissionRunKind kind in new[]{Game.Missions.Contracts.MissionRunKind.FirstClear,Game.Missions.Contracts.MissionRunKind.Replay,Game.Missions.Contracts.MissionRunKind.Retry})
        foreach(Game.Narrative.Contracts.NarrativeGuidanceMode mode in System.Enum.GetValues(typeof(Game.Narrative.Contracts.NarrativeGuidanceMode)))
        {
            using var r=new Roster();
            var launch=Game.Runtime.MissionLaunchPayloadFactory.Create(r.Runtime.MissionId.ToString(),r.Runtime.ScenarioId.ToString(),r.Runtime.OperationMapId.ToString(),
                Game.Missions.Contracts.MissionLaunchOriginKind.CampaignOperations,kind,mode,false,1,"m4-entry",1,42);
            Assert.AreEqual(Game.Narrative.Contracts.NarrativeGuidanceMode.Full,launch.Guidance);Assert.IsTrue(launch.ReplayTutorialEnabled);
            var retry=Game.Runtime.MissionLaunchPayloadFactory.CreateRetry(launch,2);
            Assert.AreEqual(Game.Narrative.Contracts.NarrativeGuidanceMode.Full,retry.Guidance);Assert.IsTrue(retry.ReplayTutorialEnabled);
            // Also exercise pre-fix payloads already in memory, without normalizing their saved settings.
            r.Runtime.Guidance=mode;r.Runtime.RunKind=kind;r.Runtime.ReplayTutorialEnabled=0;
            r.State.SessionToken=r.Runtime.SessionToken;r.State.AttemptOrdinal=r.Runtime.AttemptOrdinal;r.State.SourceVersion=r.Runtime.SourceVersion;
            r.Em.AddComponent<CampaignMissionRootComponent>(r.Root);r.Em.AddComponentData(r.Root,r.Runtime);r.Em.AddComponentData(r.Root,r.Facts);r.Em.AddComponentData(r.Root,r.State);
            r.Em.AddComponent<CampaignMissionGuidanceProjectionComponent>(r.Root);r.Em.AddBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(r.Root);
            r.Em.AddComponentData(r.Em.CreateEntity(),new RuntimeGameplayStateComponent{SimulationActive=1});
            r.Em.AddComponentData(r.Em.CreateEntity(),new AssistantSettingsComponent{GuidanceLevel=AssistantGuidanceLevel.Off});
            r.World.CreateSystem<Game.Runtime.CampaignMissionGuidanceProjectionSystem>().Update(r.World.Unmanaged);
            var guidance=r.Em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(r.Root);
            Assert.AreEqual(55001,guidance.GuidanceId,$"{kind}/{mode} must start at lesson one.");
            Assert.AreEqual(1,guidance.Active);Assert.AreEqual(Game.Narrative.Contracts.NarrativeGuidanceMode.Full,guidance.GuidanceMode);
            Assert.IsFalse(guidance.Title.IsEmpty);Assert.IsFalse(guidance.Body.IsEmpty);Assert.AreEqual(1,guidance.CanExecute);
        }
    }

    [Test]
    public void BilingualGuidanceCopyFitsNarrationMessages()
    {
        foreach(var lesson in Game.Configs.M04AirliftCopyCatalog.Lessons)
            foreach(var text in new[]{lesson.Body,lesson.PersianBody})
                Assert.DoesNotThrow(()=>{var message=new Unity.Collections.FixedString512Bytes(text);},"Recorded lesson copy must fit in both languages.");
        foreach(var entry in Game.Configs.M04AirliftCopyCatalog.Ui)
            if(entry.Key=="mission.m04.tutorial.selection_progress" || entry.Key.StartsWith("mission.m04.tutorial.hold."))
                foreach(var text in new[]{entry.English,entry.Persian})
                    Assert.DoesNotThrow(()=>{var message=new Unity.Collections.FixedString512Bytes(string.Format(text,3));});
    }

    [Test]
    public void RescueSelectionExcludesOverlappingVehiclesOnlyDuringPassengerLessons()
    {
        using var r=new Roster();
        r.Em.AddComponentData(r.Root,r.Runtime);r.Em.AddComponentData(r.Root,r.State);
        r.Em.AddComponentData(r.Root,new CampaignMissionGuidanceProjectionComponent{GuidanceId=55009});
        foreach(var person in r.People) r.Em.AddComponentData(person,new CampaignMissionUnitRoleComponent{MissionRoleId="role.friendly.specialist"});
        foreach(int lesson in new[]{4,5,9,8,10})
        {
            r.Em.SetComponentData(r.Root,new CampaignMissionGuidanceProjectionComponent{GuidanceId=55000+lesson});
            var selection=new System.Collections.Generic.List<Unity.Entities.Entity>(r.People){r.State.Aircraft,r.State.Carrier,r.Escort};
            int count=Game.Runtime.SelectionUiReadModelLookup.PreferTutorialRescuePassengers(r.Em,selection);
            Assert.AreEqual(lesson is 4 or 5 or 9 ? 4 : 7,count);
        }
        r.Em.SetComponentData(r.Root,new CampaignMissionGuidanceProjectionComponent{GuidanceId=55009});
        var vehicles=new System.Collections.Generic.List<Unity.Entities.Entity>{r.State.Aircraft,r.State.Carrier};
        Assert.AreEqual(2,Game.Runtime.SelectionUiReadModelLookup.PreferTutorialRescuePassengers(r.Em,vehicles),"An intentional vehicle-only selection stays available.");
    }

    [Test]
    public void LandingHoldInstructionDistinguishesCountdownContestedAndReturn()
    {
        using var r=new Roster();
        var resolve=typeof(UiShellEcsGateway).GetMethod("ResolveLandingHoldStatus",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        int Read()=>(int)resolve.Invoke(null,new object[]{r.Em,r.State,r.Facts,18f,20000});
        r.Facts.ExtractionSecureMilliseconds=10000;Assert.AreEqual(10,Read());
        r.Facts.ExtractionContested=1;Assert.AreEqual(-1,Read());
        r.Em.SetComponentData(r.State.Aircraft,LocalTransform.FromPosition(new float3(50,0,0)));Assert.AreEqual(-2,Read());
        r.Facts.ExtractionContested=0;r.Facts.ExtractionSecureMilliseconds=20000;
        r.Em.SetComponentData(r.State.Aircraft,LocalTransform.Identity);Assert.AreEqual(0,Read());
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
        foreach(int step in new[]{2,3,4,5,6,7,8,9,10,11})
        {
            Assert.That(ReadTutorialTarget(r.Em,r.Root,step,out var target),Is.True);
            Assert.That(target.NeedsSelection,Is.True,$"Step {step} must recover missing selection.");
            if(step is 6 or 7 or 10) Assert.That((float3)target.Destination,Is.EqualTo(r.State.LandingCenter));
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
