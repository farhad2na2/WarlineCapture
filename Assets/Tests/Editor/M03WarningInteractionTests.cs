using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class M03WarningInteractionTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var test=new M03WarningInteractionTests();
            test.ConfirmedContactCannotBeOverwrittenByScoutAndStaleIsNotDestroyed();
            test.PauseStaleAttemptMalformedObservationAndTerminalAreFailClosed();
            test.JumpUsesReportSnapshotAndOptionalSkipRequiresCurrentGuidance();
            test.AmbiguousCameraCannotAcknowledgeFocus();
            test.GuidePauseRestoresTheSameAttemptAndTimeScale();
            test.PauseDoesNotResumeAReplacedAttempt();
            test.TenWorldTeardownsRestoreTheOriginalClock();
            test.ReturnCameraIsAttemptScopedSingleUseAndRespectsReducedMotion();
            test.IgnoredWarningEscalatesOnceWithoutSpeechOrCameraAndReadDismissesIt();
            Debug.Log("[M03WarningInteractionValidation] result=Passed tests=9 worldsDisposed=10"); ValidationExit.Passed();
        }
        catch(Exception exception) {Debug.LogException(exception); Debug.LogError("[M03WarningInteractionValidation] result=Failed"); ValidationExit.Failed();}
    }
    [Test] public void ConfirmedContactCannotBeOverwrittenByScoutAndStaleIsNotDestroyed()
    {
        using var f=new Fixture(); f.Observe(0,ThreatWarningSourceKind.ScoutReport,-1); f.Resolve();
        Assert.AreEqual(-1,f.Records[0].KnownVehicleCount); Assert.AreEqual(65,f.Records[0].EtaSeconds);
        f.Elapsed(10000); f.Observe(0,ThreatWarningSourceKind.GroundSensor,1,new float3(25,0,40)); f.Resolve();
        Assert.AreEqual(ThreatWarningSourceKind.GroundSensor,f.Records[0].Source);
        f.Elapsed(11000); f.Observe(0,ThreatWarningSourceKind.ScoutReport,-1,new float3(99,0,99)); f.Resolve();
        Assert.AreEqual(new float3(25,0,40),f.Records[0].FocusPosition);
        f.Elapsed(27000); f.Resolve(); Assert.AreEqual(1,f.Records[0].Stale); Assert.AreEqual(0,f.Records[0].Resolved);
        f.Elapsed(50000); f.Observe(1,ThreatWarningSourceKind.ScoutReport,-1); f.Resolve();
        Assert.AreEqual(0,f.Ledger.FocusElementIndex); Assert.AreEqual(1,f.Records[0].Critical);
        var element=f.Em.GetBuffer<CampaignMissionConvoyElementState>(f.Root)[0]; element.Resolved=1;
        var elements=f.Em.GetBuffer<CampaignMissionConvoyElementState>(f.Root); elements[0]=element; f.Resolve();
        Assert.AreEqual(1,f.Ledger.FocusElementIndex);
        f.Observe(0,ThreatWarningSourceKind.VisualContact,1); f.Resolve(); Assert.AreEqual(1,f.Records[0].Resolved);
    }
    [Test] public void PauseStaleAttemptMalformedObservationAndTerminalAreFailClosed()
    {
        using var f=new Fixture(); f.Observe(0,ThreatWarningSourceKind.ScoutReport,-1); f.Resolve();
        uint version=f.Ledger.Version;
        RuntimeGameplayStateTestHelper.SetSimulationActive(f.Em,false); f.Elapsed(60000); f.Resolve();
        Assert.AreEqual(version,f.Ledger.Version); Assert.AreEqual(65,f.Records[0].EtaSeconds);
        RuntimeGameplayStateTestHelper.SetSimulationActive(f.Em,true);
        f.Observe(1,ThreatWarningSourceKind.RadarPing,9,new float3(float.NaN,0,0));
        var stale=new ThreatWarningObservation {SessionToken="old",AttemptOrdinal=1,SourceVersion=1,ElementIndex=1,Source=ThreatWarningSourceKind.VisualContact};
        f.Em.GetBuffer<ThreatWarningObservation>(f.Root).Add(stale); f.Resolve(); Assert.AreEqual(1,f.Records.Length);
        var runtime=f.Em.GetComponentData<CampaignMissionRuntimeComponent>(f.Root); runtime.Outcome=MissionOutcomeKind.Defeat; f.Em.SetComponentData(f.Root,runtime);
        f.Observe(1,ThreatWarningSourceKind.ScoutReport,-1); f.Resolve(); Assert.AreEqual(0,f.Ledger.Active);
        Assert.AreEqual(0,f.Em.GetBuffer<ThreatWarningObservation>(f.Root).Length);
    }
    [Test] public void JumpUsesReportSnapshotAndOptionalSkipRequiresCurrentGuidance()
    {
        using var f=new Fixture(); f.Observe(0,ThreatWarningSourceKind.GroundSensor,1,new float3(12,0,24)); f.Resolve();
        using var cameras=f.Em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
        Entity camera=cameras.GetSingletonEntity();
        f.Em.SetComponentData(f.Root,new CampaignMissionGuidanceProjectionComponent {GuidanceId=45004,Prompt=CampaignMissionGuidancePromptKind.RadarBuildOption,Active=1});
        f.Interact(MissionDefenseInteractionKind.FocusWarning,45004); f.Interaction.Update(f.World.Unmanaged);
        Assert.AreEqual(new float3(12,0,24),f.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(camera).World);
        var focusRequest=f.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(camera);
        Assert.AreEqual(1,focusRequest.UseExplicitPerspective);
        Assert.AreEqual(40f,focusRequest.Perspective.x-focusRequest.World.y);
        Assert.AreEqual(3u,f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root).AcknowledgedGuidanceMask);
        f.Interact(MissionDefenseInteractionKind.SkipOptional,45003); f.Interaction.Update(f.World.Unmanaged);
        Assert.AreEqual(3u,f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root).AcknowledgedGuidanceMask);
        f.Interact(MissionDefenseInteractionKind.SkipOptional,45004); f.Interaction.Update(f.World.Unmanaged);
        Assert.AreEqual(11u,f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root).AcknowledgedGuidanceMask);
    }
    [Test] public void GuidePauseRestoresTheSameAttemptAndTimeScale()
    {
        float original=Time.timeScale;
        try
        {
            using var f=new Fixture(); Time.timeScale=1.5f;
            Entity popup=f.Em.CreateEntity(typeof(UiShellActivePopupComponent));
            f.Em.SetComponentData(popup,new UiShellActivePopupComponent {PopupKind=UiShellPopupKind.MissionFieldGuide,Visible=1});
            var pause=f.World.GetOrCreateSystem<MissionDefensePauseSystem>(); pause.Update(f.World.Unmanaged);
            Assert.AreEqual(0,Time.timeScale);
            using var gameplay=f.Em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            Assert.AreEqual(0,gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive);
            f.Em.SetComponentData(popup,new UiShellActivePopupComponent {PopupKind=UiShellPopupKind.Pause,Visible=1}); pause.Update(f.World.Unmanaged);
            Assert.AreEqual(0,Time.timeScale,"Switching between paused overlays must preserve the original scale.");
            f.Em.SetComponentData(popup,new UiShellActivePopupComponent()); pause.Update(f.World.Unmanaged);
            Assert.AreEqual(1.5f,Time.timeScale); Assert.AreEqual(1,gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive);
        }
        finally {Time.timeScale=original;}
    }
    [Test] public void AmbiguousCameraCannotAcknowledgeFocus()
    {
        using var f=new Fixture(); f.Observe(0,ThreatWarningSourceKind.GroundSensor,1,new float3(12,0,24)); f.Resolve();
        var duplicate=f.Em.CreateEntity(typeof(RuntimeCameraFocusRequestComponent));
        f.Interact(MissionDefenseInteractionKind.FocusWarning,45004); f.Interaction.Update(f.World.Unmanaged);
        Assert.AreEqual(1u,f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root).AcknowledgedGuidanceMask);
        Assert.AreEqual(0,f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root).PlayerRequestedFocus);
        Assert.AreEqual(0,f.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(duplicate).Requested);
    }
    [Test] public void PauseDoesNotResumeAReplacedAttempt()
    {
        float original=Time.timeScale;
        try
        {
            using var f=new Fixture(); Time.timeScale=1.25f;
            Entity popup=f.Em.CreateEntity(typeof(UiShellActivePopupComponent));
            f.Em.SetComponentData(popup,new UiShellActivePopupComponent {PopupKind=UiShellPopupKind.MissionFieldGuide,Visible=1});
            var pause=f.World.GetOrCreateSystem<MissionDefensePauseSystem>(); pause.Update(f.World.Unmanaged);
            var runtime=f.Em.GetComponentData<CampaignMissionRuntimeComponent>(f.Root); runtime.AttemptOrdinal++;
            f.Em.SetComponentData(f.Root,runtime); pause.Update(f.World.Unmanaged);
            Assert.AreEqual(1.25f,Time.timeScale);
            using var gameplay=f.Em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            Assert.AreEqual(0,gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive,
                "An old guide must not resume an attempt whose own readiness has not enabled simulation.");
        }
        finally {Time.timeScale=original;}
    }
    [Test] public void TenWorldTeardownsRestoreTheOriginalClock()
    {
        float original=Time.timeScale;
        try
        {
            for(int cycle=0;cycle<10;cycle++)
            {
                Time.timeScale=1.25f;
                using(var f=new Fixture())
                {
                    Entity popup=f.Em.CreateEntity(typeof(UiShellActivePopupComponent));
                    f.Em.SetComponentData(popup,new UiShellActivePopupComponent {PopupKind=UiShellPopupKind.MissionFieldGuide,Visible=1});
                    f.World.GetOrCreateSystem<MissionDefensePauseSystem>().Update(f.World.Unmanaged);
                    Assert.AreEqual(0,Time.timeScale);
                }
                Assert.AreEqual(1.25f,Time.timeScale,"World teardown left the global clock paused at cycle "+cycle);
            }
        }
        finally {Time.timeScale=original;}
    }
    [Test] public void ReturnCameraIsAttemptScopedSingleUseAndRespectsReducedMotion()
    {
        using var f=new Fixture();
        var defense=f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root);
        defense.FocusReturnAvailable=1; defense.FocusReturnPosition=new float3(5,0,8); defense.FocusReturnPerspective=new float4(95,78,0,58);
        f.Em.SetComponentData(f.Root,defense);
        f.Em.AddComponentData(f.Root,new CampaignMissionCameraTourState {ReducedMotion=1});
        using var cameras=f.Em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent)); var camera=cameras.GetSingletonEntity();
        RuntimeGameplayStateTestHelper.SetSimulationActive(f.Em,false);
        f.Interact(MissionDefenseInteractionKind.ReturnCamera,0); f.Interaction.Update(f.World.Unmanaged);
        Assert.AreEqual(1,f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root).FocusReturnAvailable);
        RuntimeGameplayStateTestHelper.SetSimulationActive(f.Em,true);
        f.Interact(MissionDefenseInteractionKind.ReturnCamera,0); f.Interaction.Update(f.World.Unmanaged);
        var request=f.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(camera);
        Assert.AreEqual(defense.FocusReturnPosition,request.World); Assert.AreEqual(defense.FocusReturnPerspective,request.Perspective);
        Assert.AreEqual(1,request.UseExplicitPerspective); Assert.AreEqual(0,request.Smooth);
        Assert.AreEqual(0,f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root).FocusReturnAvailable);
        f.Em.SetComponentData(camera,default(RuntimeCameraFocusRequestComponent));
        f.Interact(MissionDefenseInteractionKind.ReturnCamera,0); f.Interaction.Update(f.World.Unmanaged);
        Assert.AreEqual(0,f.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(camera).Requested);
        defense.AttemptOrdinal++; f.Em.SetComponentData(f.Root,defense);
        f.Interact(MissionDefenseInteractionKind.ReturnCamera,0); f.Interaction.Update(f.World.Unmanaged);
        Assert.AreEqual(0,f.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(camera).Requested);
    }
    [Test] public void IgnoredWarningEscalatesOnceWithoutSpeechOrCameraAndReadDismissesIt()
    {
        using var f=new Fixture(); f.Observe(0,ThreatWarningSourceKind.ScoutReport,-1); f.Resolve();
        uint speech=f.Ledger.PresentationVersion;
        f.Elapsed(29999); f.Resolve(); Assert.AreEqual(0,f.Records[0].AttentionEscalated);
        RuntimeGameplayStateTestHelper.SetSimulationActive(f.Em,false);
        f.Elapsed(30000); f.Resolve(); Assert.AreEqual(0,f.Records[0].AttentionEscalated);
        RuntimeGameplayStateTestHelper.SetSimulationActive(f.Em,true); f.Resolve();
        Assert.AreEqual(1,f.Records[0].AttentionEscalated); Assert.AreEqual(speech,f.Ledger.PresentationVersion);
        Assert.AreEqual(0,f.Records[0].Critical);
        uint version=f.Ledger.Version; f.Resolve(); Assert.AreEqual(version,f.Ledger.Version);
        using var cameras=f.Em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
        Assert.AreEqual(0,cameras.GetSingleton<RuntimeCameraFocusRequestComponent>().Requested);
        f.Interact(MissionDefenseInteractionKind.ReadWarning,0); f.Interaction.Update(f.World.Unmanaged);
        Assert.AreEqual(1,f.Records[0].ReadByPlayer);
        f.Elapsed(31000); f.Observe(0,ThreatWarningSourceKind.GroundSensor,1); f.Resolve();
        Assert.AreEqual(0,f.Records[0].FirstReportedAtMilliseconds); Assert.AreEqual(1,f.Records[0].ReadByPlayer);
        f.Observe(1,ThreatWarningSourceKind.ScoutReport,-1); f.Resolve();
        Assert.AreEqual(31000,f.Records[1].FirstReportedAtMilliseconds); Assert.AreEqual(0,f.Records[1].AttentionEscalated);
    }
    [TestCase(MissionRunKind.FirstClear, NarrativeGuidanceMode.Minimal)]
    [TestCase(MissionRunKind.Replay, NarrativeGuidanceMode.Minimal)]
    [TestCase(MissionRunKind.Retry, NarrativeGuidanceMode.Contextual)]
    public void EveryM3AttemptPublishesTheFirstLessonDespiteSavedGuidance(MissionRunKind kind, NarrativeGuidanceMode mode)
    {
        using var f=new Fixture(guidance:true);
        var runtime=f.Em.GetComponentData<CampaignMissionRuntimeComponent>(f.Root);
        runtime.MissionId="saga.ch01.m03.radar_warning"; runtime.Guidance=mode;
        runtime.RunKind=kind; runtime.ReplayTutorialEnabled=0;f.Em.SetComponentData(f.Root,runtime);
        f.Observe(0,ThreatWarningSourceKind.ScoutReport,-1);f.Resolve();
        f.World.GetOrCreateSystem<CampaignMissionGuidanceProjectionSystem>().Update(f.World.Unmanaged);
        var guidance=f.Em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(f.Root);
        Assert.AreEqual(1,guidance.Active);Assert.AreEqual(45001,guidance.GuidanceId);
        Assert.AreEqual(NarrativeGuidanceMode.Full,guidance.GuidanceMode);
    }

    [Test]
    public void MoveLessonRequiresANewAcceptedMoveAndArrival()
    {
        using var f=new Fixture(guidance:true);
        var runtime=f.Em.GetComponentData<CampaignMissionRuntimeComponent>(f.Root);
        runtime.MissionId="saga.ch01.m03.radar_warning"; runtime.Guidance=NarrativeGuidanceMode.Full;
        runtime.RunKind=MissionRunKind.FirstClear; f.Em.SetComponentData(f.Root,runtime);
        var defense=f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root);
        defense.AcknowledgedGuidanceMask=15; f.Em.SetComponentData(f.Root,defense);
        f.Observe(0,ThreatWarningSourceKind.ScoutReport,-1); f.Resolve();
        Entity moving=Entity.Null;
        for(int i=0;i<4;i++)
        {
            moving=f.Em.CreateEntity(typeof(CampaignMissionUnitRoleComponent),typeof(Faction),typeof(UnitHealth),
                typeof(UnitAttack),typeof(LocalTransform),typeof(UnitCombat));
            f.Em.SetComponentData(moving,new CampaignMissionUnitRoleComponent {SessionToken="session"});
            f.Em.SetComponentData(moving,new Faction {Id=1});
            f.Em.SetComponentData(moving,new UnitHealth {Current=100});
            f.Em.SetComponentData(moving,new UnitAttack {Range=40});
            f.Em.SetComponentData(moving,new UnitCombat {CanAttack=1});
            f.Em.SetComponentData(moving,LocalTransform.Identity);
        }
        var results=f.Em.AddBuffer<UnitMoveOrderResultElement>(f.Root);
        results.Add(new UnitMoveOrderResultElement {RequestId=1,Entity=moving,Kind=UnitMoveOrderRequestKind.GroupedManual,Issued=1});
        var system=f.World.GetOrCreateSystem<CampaignMissionGuidanceProjectionSystem>();
        system.Update(f.World.Unmanaged);
        Assert.AreEqual(0,f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root).AcknowledgedGuidanceMask&16u,
            "Starting in range and old command receipts must not skip teaching Move.");
        results=f.Em.GetBuffer<UnitMoveOrderResultElement>(f.Root);
        results.Add(new UnitMoveOrderResultElement {RequestId=2,Entity=moving,Kind=UnitMoveOrderRequestKind.GroupedManual,Issued=0});
        results.Add(new UnitMoveOrderResultElement {RequestId=3,Entity=moving,Kind=UnitMoveOrderRequestKind.TargetOnly,Issued=1});
        system.Update(f.World.Unmanaged);
        Assert.AreEqual(0,f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root).MoveAccepted);
        f.Em.AddComponent<UnitPathRequest>(moving);
        results=f.Em.GetBuffer<UnitMoveOrderResultElement>(f.Root);
        results.Add(new UnitMoveOrderResultElement {RequestId=4,Entity=moving,Kind=UnitMoveOrderRequestKind.GroupedManual,Issued=1});
        system.Update(f.World.Unmanaged);
        Assert.AreEqual(0,f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root).AcknowledgedGuidanceMask&16u,
            "An accepted order is not arrival.");
        f.Em.RemoveComponent<UnitPathRequest>(moving); system.Update(f.World.Unmanaged);
        Assert.AreEqual(16u,f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root).AcknowledgedGuidanceMask&16u);
    }

    private sealed class Fixture:IDisposable
    {
        public readonly World World=new("M3 warning interaction"); public EntityManager Em=>World.EntityManager;
        public readonly Entity Root; public readonly SystemHandle Resolver,Interaction;
        private BlobAssetReference<CampaignMissionCatalogBlob> catalog; private BlobAssetReference<OperationMapBlob> map;
        public DynamicBuffer<ThreatWarningRecord> Records=>Em.GetBuffer<ThreatWarningRecord>(Root);
        public ThreatWarningLedgerState Ledger=>Em.GetComponentData<ThreatWarningLedgerState>(Root);
        public Fixture(bool guidance=false)
        {
            Root=Em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionAttemptFactsComponent),
                typeof(CampaignMissionDefenseStateComponent),typeof(CampaignMissionGuidanceProjectionComponent),typeof(ThreatWarningLedgerState));
            Em.SetComponentData(Root,new CampaignMissionRuntimeComponent {SessionToken="session",AttemptOrdinal=1,SourceVersion=1,Phase=MissionPhaseKind.Engage});
            Em.SetComponentData(Root,new CampaignMissionDefenseStateComponent {SessionToken="session",AttemptOrdinal=1,SourceVersion=1});
            Em.SetComponentData(Root,new ThreatWarningLedgerState {SessionToken="session",AttemptOrdinal=1,SourceVersion=1,FocusElementIndex=-1});
            Em.AddBuffer<ThreatWarningRecord>(Root); Em.AddBuffer<ThreatWarningObservation>(Root); Em.AddBuffer<MissionDefenseInteractionRequest>(Root);
            var elements=Em.AddBuffer<CampaignMissionConvoyElementState>(Root); elements.Add(default); elements.Add(default);
            using(var builder=new BlobBuilder(Allocator.Temp))
            {
                ref var data=ref builder.ConstructRoot<CampaignMissionCatalogBlob>(); var missions=builder.Allocate(ref data.Missions,1); missions[0].Defense.Enabled=1;
                var authored=builder.Allocate(ref missions[0].Defense.Elements,2); authored[0].ContactAtMilliseconds=65000; authored[1].ContactAtMilliseconds=165000;
                if(guidance)
                {
                    missions[0].MissionId="saga.ch01.m03.radar_warning";
                    var steps=builder.Allocate(ref missions[0].Defense.GuidanceSteps,6);
                    steps[4].Completion=MissionGuidanceCompletionKind.SquadPositioned;
                    steps[4].Action=MissionGuidanceActionKind.Move;
                }
                catalog=builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
            }
            using(var builder=new BlobBuilder(Allocator.Temp)) {builder.ConstructRoot<OperationMapBlob>(); map=builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);}
            Em.AddComponentData(Root,new CampaignMissionCatalogComponent {Blob=catalog});
            Em.AddComponentData(Root,new OperationMapMetadataComponent {Blob=map});
            RuntimeGameplayStateTestHelper.SetPlayRequested(Em,true);
            Resolver=World.GetOrCreateSystem<ThreatWarningResolveSystem>(); Interaction=World.GetOrCreateSystem<MissionDefenseInteractionSystem>();
        }
        public void Elapsed(int ms)=>Em.SetComponentData(Root,new CampaignMissionAttemptFactsComponent {ElapsedMilliseconds=ms});
        public void Observe(int element,ThreatWarningSourceKind source,int count,float3 pos=default)=>Em.GetBuffer<ThreatWarningObservation>(Root).Add(new ThreatWarningObservation
        {SessionToken="session",AttemptOrdinal=1,SourceVersion=1,ElementIndex=element,Source=source,KnownVehicleCount=count,Position=pos,HasPosition=1,
            ObservedAtMilliseconds=Em.GetComponentData<CampaignMissionAttemptFactsComponent>(Root).ElapsedMilliseconds});
        public void Resolve()=>Resolver.Update(World.Unmanaged);
        public void Interact(MissionDefenseInteractionKind kind,int id)=>Em.GetBuffer<MissionDefenseInteractionRequest>(Root).Add(new MissionDefenseInteractionRequest
            {SessionToken="session",AttemptOrdinal=1,SourceVersion=1,Kind=kind,GuidanceId=id,WarningElementIndex=0});
        public void Dispose() {World.Dispose(); if(catalog.IsCreated) catalog.Dispose(); if(map.IsCreated) map.Dispose();}
    }
}
