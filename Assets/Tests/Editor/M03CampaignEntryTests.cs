using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class M03CampaignEntryTests
{
    private const string M1="saga.ch01.m01.first_contact",M2="saga.ch01.m02.establish_base",M3="saga.ch01.m03.radar_warning",M4="saga.ch01.m04.airlift";
    public static void RunFocusedValidation()
    {
        try
        {
            var tests=new M03CampaignEntryTests();
            tests.LockedM3CannotSelectOrDeploy();
            tests.LegacyM2ClearUnlocksExactM3AndDoubleDeployQueuesOnce();
            tests.CompletedM3UnlocksImplementedM4Deployment();
            tests.ReadinessAndMapIdentityNeverFallBackToAnotherMission();
            tests.ProjectionRefreshesAfterOtherSaveServiceWritesAndStoreReplacement();
            tests.ProjectionPreservesDurableStateAfterFailedWriteAndAcceptsRetry();
            Debug.Log("[M03CampaignEntryValidation] result=Passed tests=6"); ValidationExit.Passed();
        }
        catch(Exception error) {Debug.LogException(error); Debug.LogError("[M03CampaignEntryValidation] result=Failed"); ValidationExit.Failed();}
    }
    [TestCase(MissionRunKind.FirstClear)] [TestCase(MissionRunKind.Replay)] [TestCase(MissionRunKind.Retry)]
    public void M3LaunchAndRetryAlwaysRequireFullTutorial(MissionRunKind runKind)
    {
        foreach(Game.Narrative.Contracts.NarrativeGuidanceMode mode in Enum.GetValues(typeof(Game.Narrative.Contracts.NarrativeGuidanceMode)))
        {
            var launch=MissionLaunchPayloadFactory.Create(M3,"scenario.ch01.m03.radar_warning","opmap.ch01.convoy_approach_01",
                MissionLaunchOriginKind.CampaignOperations,runKind,mode,false,1,"tutorial-entry",1,42);
            Assert.AreEqual(Game.Narrative.Contracts.NarrativeGuidanceMode.Full,launch.Guidance);
            Assert.IsTrue(launch.ReplayTutorialEnabled);
            var retry=MissionLaunchPayloadFactory.CreateRetry(launch,2);
            Assert.AreEqual(Game.Narrative.Contracts.NarrativeGuidanceMode.Full,retry.Guidance);
            Assert.IsTrue(retry.ReplayTutorialEnabled);
        }
    }

    [Test] public void LockedM3CannotSelectOrDeploy()
    {
        using var f=new Fixture(false); f.Project();
        f.Request(UiCampaignMissionActionKind.Select,M3); f.Request(UiCampaignMissionActionKind.Deploy,M3); f.Project();
        Assert.AreEqual(M1,f.Card.SelectedMissionId.ToString()); Assert.AreEqual(0,f.Launches.Length);
        Assert.AreEqual(0,f.Card.AvailableMissionMask&4);
    }
    [Test] public void LegacyM2ClearUnlocksExactM3AndDoubleDeployQueuesOnce()
    {
        using var f=new Fixture(true); f.Project();
        Assert.IsTrue(f.Store.ReadAll().Single(p=>p.missionId==M3).available);
        Assert.AreEqual(M3,f.Card.SelectedMissionId.ToString()); Assert.AreEqual(4,f.Card.AvailableMissionMask&4);
        Assert.AreEqual(50000,f.Briefing.StartingCredits); Assert.AreEqual(100,f.Briefing.StartingMaterials);
        Assert.AreEqual(7,f.Briefing.HostileUnitCount); Assert.AreEqual(4,f.Briefing.Rewards.Length);
        f.Request(UiCampaignMissionActionKind.Deploy,M3); f.Request(UiCampaignMissionActionKind.Deploy,M3); f.Project();
        Assert.AreEqual(1,f.Launches.Length);
        var launch=f.Launches[0];
        Assert.AreEqual(M3,launch.MissionId.ToString()); Assert.AreEqual("scenario.ch01.m03.radar_warning",launch.ScenarioId.ToString());
        Assert.AreEqual("opmap.ch01.convoy_approach_01",launch.OperationMapId.ToString());
        Assert.AreEqual(MissionRunKind.FirstClear,launch.RunKind); Assert.AreEqual(1,f.Briefing.DeployQueued);
        f.Request(UiCampaignMissionActionKind.Deploy,M3); f.Project(); Assert.AreEqual(1,f.Launches.Length);
    }
    [Test] public void CompletedM3UnlocksImplementedM4Deployment()
    {
        using var f=new Fixture(true); f.Store.EnsureAvailable(M4); f.Project();
        f.Request(UiCampaignMissionActionKind.Select,M4); f.Request(UiCampaignMissionActionKind.Deploy,M4); f.Project();
        Assert.AreEqual(M4,f.Card.SelectedMissionId.ToString()); Assert.AreEqual(1,f.Launches.Length);
        Assert.AreEqual(M4,f.Launches[0].MissionId.ToString());
        Assert.AreEqual(8,f.Card.AvailableMissionMask&8);
    }
    [Test] public void ReadinessAndMapIdentityNeverFallBackToAnotherMission()
    {
        using var f=new Fixture(true); f.Project(); f.Request(UiCampaignMissionActionKind.Deploy,M3); f.Project();
        var launch=f.Launches[0]; var catalog=f.Em.GetComponentData<CampaignMissionCatalogComponent>(f.Root);
        var wrongMap=new ActiveOperationMapComponent {MissionId=M1,ScenarioId="scenario.ch01.m01.first_contact",OperationMapId="opmap.ch01.first_contact_01"};
        Assert.IsFalse(CampaignMissionLaunchSystem.TryValidate(in launch,in catalog,in wrongMap,out var reason));
        Assert.AreEqual("operation-map-mismatch",reason.ToString());
        Entity map=f.Em.CreateEntity(typeof(ActiveOperationMapComponent),typeof(OperationMapReadinessComponent));
        f.Em.SetComponentData(map,new ActiveOperationMapComponent {MissionId=launch.MissionId,ScenarioId=launch.ScenarioId,OperationMapId=launch.OperationMapId});
        f.Em.SetComponentData(map,new OperationMapReadinessComponent {RequiredFlags=OperationMapReadinessFlags.MapSurface});
        var system=f.World.GetOrCreateSystem<CampaignMissionLaunchSystem>(); system.Update(f.World.Unmanaged);
        Assert.AreEqual(1,f.Launches.Length); Assert.AreEqual(MissionPhaseKind.None,f.Em.GetComponentData<CampaignMissionRuntimeComponent>(f.Root).Phase);
        f.Em.SetComponentData(map,new OperationMapReadinessComponent {RequiredFlags=OperationMapReadinessFlags.MapSurface,FailedFlags=OperationMapReadinessFlags.MapSurface});
        system.Update(f.World.Unmanaged);
        Assert.AreEqual(0,f.Launches.Length);
        var results=f.Em.GetBuffer<CampaignMissionLaunchResultElement>(f.Root);
        Assert.AreEqual(1,results.Length); Assert.AreEqual(0,results[0].Accepted);
        Assert.AreEqual("operation-map-readiness-failed",results[0].ReasonCode.ToString());
        Assert.AreEqual(MissionPhaseKind.None,f.Em.GetComponentData<CampaignMissionRuntimeComponent>(f.Root).Phase);
    }
    [Test] public void ProjectionRefreshesAfterOtherSaveServiceWritesAndStoreReplacement()
    {
        using var f = new Fixture(true); f.Project();
        uint version = f.Card.Version;
        for (int i = 0; i < 30; i++) f.Project();
        Assert.AreEqual(version, f.Card.Version);
        var other = new SaveService(new JsonSaveRepository(f.SaveRoot));
        var profile = other.LoadProfile();
        profile.campaignMissionProgress.Single(p => p.missionId == M3).pendingResume = true;
        other.SaveProfile(profile); f.Project();
        Assert.AreEqual(1, f.Card.PendingResume, "A different SaveService must invalidate the projection.");
        var replacement = new CampaignMissionProgressStore(other);
        f.Em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(f.Root).Store = replacement;
        f.Project(); Assert.AreEqual(1, f.Card.PendingResume);
        replacement.SetPendingResume(M3, false, 0); f.Project(); Assert.AreEqual(0, f.Card.PendingResume);
        other.DeleteAllSaveData(); f.Project(); Assert.AreEqual(0, f.Card.AvailableMissionMask & 4);
    }
    [Test] public void ProjectionPreservesDurableStateAfterFailedWriteAndAcceptsRetry()
    {
        using var f = new Fixture(true); f.Project();
        long revision = f.Store.SourceVersion;
        string fault = Path.Combine(f.SaveRoot, SaveService.ProfileFileName + ".tmp");
        Directory.CreateDirectory(fault);
        Assert.Throws<UnauthorizedAccessException>(() => f.Store.SetPendingResume(M3, true, 1));
        Assert.AreEqual(revision, f.Store.SourceVersion);
        f.Project(); Assert.AreEqual(0, f.Card.PendingResume);
        Directory.Delete(fault);
        Assert.IsTrue(f.Store.SetPendingResume(M3, true, 1)); f.Project();
        Assert.AreEqual(1, f.Card.PendingResume);
    }
    [Test] public void ReturningToCampaignFocusesNextUnfinishedMissionWithoutChangingManualSelection()
    {
        using var f = new Fixture(true); f.Project();
        f.Em.AddComponentData(f.UiRoot, new UiShellStateComponent { ActiveRoute = UIRoute.Campaign });
        f.Request(UiCampaignMissionActionKind.Select,M2); f.Project();
        Assert.AreEqual(M2, f.Card.SelectedMissionId.ToString());
        Assert.AreEqual(3, f.Card.CompletedMissionMask & 3);
        f.Request(UiCampaignMissionActionKind.Refresh,M2); f.Project();
        Assert.AreEqual(M3, f.Card.SelectedMissionId.ToString());
        Assert.AreEqual(4, f.Card.AvailableMissionMask & 4);
        f.Request(UiCampaignMissionActionKind.Select,M2); f.Project();
        Assert.AreEqual(M2, f.Card.SelectedMissionId.ToString());
        f.Em.SetComponentData(f.UiRoot, new UiShellStateComponent { ActiveRoute = UIRoute.MissionBriefing });
        f.Request(UiCampaignMissionActionKind.Refresh,M2); f.Project();
        Assert.AreEqual(M2, f.Card.SelectedMissionId.ToString(), "Opening a replay briefing must preserve the chosen completed mission.");
    }

    private sealed class Fixture:IDisposable
    {
        public readonly World World=new("M3 campaign entry");
        public EntityManager Em=>World.EntityManager;
        public readonly Entity Root,UiRoot;
        public readonly CampaignMissionProgressStore Store;
        public string SaveRoot => saveRoot;
        private readonly string saveRoot=Path.Combine(Path.GetTempPath(),"warline-m03-entry-"+Guid.NewGuid().ToString("N"));
        private readonly SystemHandle projection;
        public UiCampaignOperationsComponent Card=>Em.GetComponentData<UiCampaignOperationsComponent>(UiRoot);
        public UiMissionBriefingComponent Briefing {get {using var q=Em.CreateEntityQuery(typeof(UiMissionBriefingComponent)); return q.GetSingleton<UiMissionBriefingComponent>();}}
        public DynamicBuffer<CampaignMissionLaunchRequestElement> Launches=>Em.GetBuffer<CampaignMissionLaunchRequestElement>(Root);
        public Fixture(bool legacyM2Cleared)
        {
            var missions=AssetDatabase.LoadAssetAtPath<MissionDefinitionCatalogConfig>("Assets/Game/Configs/Campaign/CampaignMissionCatalog.asset");
            var maps=AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>("Assets/Game/Configs/OperationMaps/Chapter01/OperationMapCatalog_Chapter01.asset");
            Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(Em,missions,maps,29,out Root,out string error),error);
            Store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(saveRoot)));
            if(legacyM2Cleared)
            {
                Assert.IsTrue(Store.Settle(M1,"legacy-one",0,true,3,60000,null));
                Assert.IsTrue(Store.Settle(M2,"legacy-two",0,true,3,60000,null));
            }
            Em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(Root).Store=Store;
            UiRoot=Em.CreateEntity(typeof(UiShellRootComponent)); Em.AddBuffer<UiShellRouteRequestComponent>(UiRoot);
            projection=World.GetOrCreateSystem<UiCampaignMissionProjectionSystem>();
        }
        public void Project() {projection.Update(World.Unmanaged); Em.CompleteAllTrackedJobs();}
        public void Request(UiCampaignMissionActionKind action,string id)=>Em.GetBuffer<UiCampaignMissionActionRequestElement>(UiRoot)
            .Add(new UiCampaignMissionActionRequestElement {Action=action,MissionId=new FixedString64Bytes(id)});
        public void Dispose()
        {
            var catalog=Em.GetComponentData<CampaignMissionCatalogComponent>(Root);
            if(catalog.Blob.IsCreated) catalog.Blob.Dispose(); catalog.Blob=default; catalog.OwnsBlob=0; Em.SetComponentData(Root,catalog);
            World.Dispose(); if(Directory.Exists(saveRoot)) Directory.Delete(saveRoot,true);
        }
    }
}
