#if UNITY_EDITOR
using System;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class AriaUiOwnedStorageTests
    {
        sealed class Fixture : IDisposable
        {
            readonly World previous;
            public readonly World World;
            public EntityManager Em => World.EntityManager;
            public Entity Root;
            readonly SystemHandle planner;
            public Fixture()
            {
                previous=Unity.Entities.World.DefaultGameObjectInjectionWorld;
                World=new World("AriaUiOwnedStorageFullUiRoot");
                Unity.Entities.World.DefaultGameObjectInjectionWorld=World;
                World.CreateSystem<UiShellStateSystem>();
                World.CreateSystem<AssistantGoalReadModelSystem>().Update(World.Unmanaged);
                World.CreateSystem<AssistantNarrationRequestSystem>().Update(World.Unmanaged);
                World.CreateSystem<AssistantControlOwnerSystem>().Update(World.Unmanaged);
                using var query=Em.CreateEntityQuery(typeof(UiShellRootComponent));
                Root=query.GetSingletonEntity();
                var shell=Em.GetComponentData<UiShellStateComponent>(Root);
                shell.ActiveRoute=UIRoute.Match;shell.TransitionSequenceId=7;
                Em.SetComponentData(Root,shell);
                planner=World.CreateSystem<AriaSkirmishPlanSystem>();
                UiShellEcsGateway.RegisterAsRuntimeGateway();
                UiShellRuntimeGateway.PublishAriaObservation(new AriaPlayObservation(AriaPlayObservationKind.Waiting,0,0,default,11,12));
            }
            public Entity Publish(bool active=true)
            {
                UiShellRuntimeGateway.PublishAriaSkirmishObservation(View(active));
                using var query=Em.CreateEntityQuery(typeof(AriaSkirmishObservationOwner));
                Assert.AreEqual(1,query.CalculateEntityCount());
                return query.GetSingletonEntity();
            }
            public void Tick()=>planner.Update(World.Unmanaged);
            public void Dispose()
            {
                Unity.Entities.World.DefaultGameObjectInjectionWorld=previous;
                UiShellEcsGateway.RegisterAsRuntimeGateway();
                World.Dispose();
            }
        }
        static AriaSkirmishObservation View(bool active=true)=>new AriaSkirmishObservation
        {
            Active=active,Finished=true,Frame=11,Time=12,Infantry=24,OwnMaterials=321,
            ExpandedSession=true,OwnAttackAirLive=3,
            FocusGroup=new AriaTouchTarget{Id=345,Available=true,Position=new Vector2(111,222),Drag=true,DragEnd=new Vector2(333,444)}
        };
        static void Intent(Fixture f,Entity storage)=>f.Em.SetComponentData(storage,new AriaSkirmishPlanComponent{Intent=AriaSkirmishIntent.DefendBase,Cycle=9});

        [Test] public void ActualFullRootPublishesCompleteValuesAndPlannerBridgesTouch()
        {
            using var f=new Fixture();
            Assert.IsTrue(f.Em.HasComponent<UiDiagnosticsOverlayComponent>(f.Root));
            Assert.IsTrue(f.Em.HasBuffer<AssistantCommandDispatchElement>(f.Root));
            Assert.IsTrue(f.Em.HasComponent<AssistantControlOwnerComponent>(f.Root));
            var storage=f.Publish();Intent(f,storage);
            Assert.IsFalse(f.Em.HasComponent<AriaSkirmishObservationComponent>(f.Root));
            Assert.IsFalse(f.Em.HasComponent<AriaSkirmishPlanComponent>(f.Root));
            Assert.AreEqual(View(),f.Em.GetComponentData<AriaSkirmishObservationComponent>(storage).Value);
            Assert.AreEqual(f.Root,f.Em.GetComponentData<AriaSkirmishObservationOwner>(storage).Boundary);
            Assert.AreEqual(AriaSkirmishIntent.DefendBase,UiShellRuntimeGateway.ReadAriaSkirmishIntent());
            f.Em.SetComponentData(f.Root,new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing,Actions=5});
            f.Tick();
            Assert.AreEqual(AriaPlayObservationKind.Finished,f.Em.GetComponentData<AriaPlayObservationComponent>(f.Root).Kind);
            Assert.AreEqual(5,f.Em.GetComponentData<AriaPlaySessionComponent>(f.Root).Actions);
            Assert.AreEqual(AriaSkirmishIntent.Handback,UiShellRuntimeGateway.ReadAriaSkirmishIntent());
        }
        [Test] public void NormalStartStopResetsOwnedPlanAndOriginalTouchOwner()
        {
            using var f=new Fixture();var storage=f.Publish();Intent(f,storage);
            var mission=f.Em.CreateEntity(typeof(CampaignMissionRuntimeComponent));
            f.Em.SetComponentData(mission,new CampaignMissionRuntimeComponent{MissionId="saga.ch01.m01.first_contact"});
            Assert.IsTrue(UiShellRuntimeGateway.TryStartAriaPlay());
            Assert.AreEqual(AriaPlayPhase.Starting,f.Em.GetComponentData<AriaPlaySessionComponent>(f.Root).Phase);
            Assert.AreEqual(default(AriaSkirmishPlanComponent),f.Em.GetComponentData<AriaSkirmishPlanComponent>(storage));
            Intent(f,storage);UiShellRuntimeGateway.StopAriaPlay();
            Assert.AreEqual(default(AriaPlaySessionComponent),f.Em.GetComponentData<AriaPlaySessionComponent>(f.Root));
            Assert.IsFalse(f.Em.GetComponentData<AriaSkirmishObservationComponent>(storage).Value.Active);
            Assert.AreEqual(default(AriaSkirmishPlanComponent),f.Em.GetComponentData<AriaSkirmishPlanComponent>(storage));
            Assert.AreEqual(0,f.Em.GetBuffer<AssistantCommandIntentRequestElement>(f.Root).Length);
        }
        [Test] public void TransitionRejectsIntentImmediatelyAndClearsOldActiveTouch()
        {
            using var f=new Fixture();var storage=f.Publish();Intent(f,storage);
            f.Em.SetComponentData(f.Root,new AriaPlaySessionComponent{Phase=AriaPlayPhase.Touching,Pressed=1});
            var shell=f.Em.GetComponentData<UiShellStateComponent>(f.Root);shell.TransitionSequenceId++;
            f.Em.SetComponentData(f.Root,shell);
            Assert.AreEqual(default(AriaSkirmishIntent),UiShellRuntimeGateway.ReadAriaSkirmishIntent());
            f.Tick();
            Assert.AreEqual(default(AriaPlaySessionComponent),f.Em.GetComponentData<AriaPlaySessionComponent>(f.Root));
            Assert.IsFalse(f.Em.GetComponentData<AriaSkirmishObservationComponent>(storage).Value.Active);
            storage=f.Publish();Assert.IsTrue(AriaSkirmishObservationStorage.TryGet(f.Em,f.Root,out var current));Assert.AreEqual(storage,current);
        }
        [Test] public void NonMatchAndInactiveObservationCannotLeakIntentOrEraseCampaignTouch()
        {
            using var f=new Fixture();var storage=f.Publish();Intent(f,storage);
            var shell=f.Em.GetComponentData<UiShellStateComponent>(f.Root);shell.ActiveRoute=UIRoute.MainMenu;
            f.Em.SetComponentData(f.Root,shell);
            Assert.AreEqual(default(AriaSkirmishIntent),UiShellRuntimeGateway.ReadAriaSkirmishIntent());f.Tick();
            shell.ActiveRoute=UIRoute.Match;f.Em.SetComponentData(f.Root,shell);f.Publish(false);
            f.Em.SetComponentData(f.Root,new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing,Actions=19});
            f.Tick();Assert.AreEqual(19,f.Em.GetComponentData<AriaPlaySessionComponent>(f.Root).Actions);
        }
        [Test] public void DestroyedRootStorageIsRemovedAndReplacementStartsFresh()
        {
            using var f=new Fixture();var old=f.Publish();Intent(f,old);
            var replacement=f.Em.Instantiate(f.Root);f.Em.DestroyEntity(f.Root);f.Root=replacement;
            f.Tick();Assert.IsFalse(f.Em.Exists(old));
            var current=f.Publish();Assert.AreNotEqual(old,current);
            Assert.AreEqual(default(AriaSkirmishPlanComponent),f.Em.GetComponentData<AriaSkirmishPlanComponent>(current));
            Assert.AreEqual(replacement,f.Em.GetComponentData<AriaSkirmishObservationOwner>(current).Boundary);
        }
        [Test] public void DuplicateStorageRefusesAndWorldReplacementHasNoCachedOwner()
        {
            using(var f=new Fixture())
            {
                var storage=f.Publish();Intent(f,storage);f.Em.Instantiate(storage);
                Assert.Throws<InvalidOperationException>(()=>UiShellRuntimeGateway.ReadAriaSkirmishIntent());
            }
            using(var f=new Fixture())
            {
                Assert.AreEqual(default(AriaSkirmishIntent),UiShellRuntimeGateway.ReadAriaSkirmishIntent());
                var storage=f.Publish();Assert.AreEqual(default(AriaSkirmishPlanComponent),f.Em.GetComponentData<AriaSkirmishPlanComponent>(storage));
            }
        }
        [Test] public void RepublishBeforePlannerCannotCarryOldContactIntoNewScope()
        {
            foreach(var kind in new[]{0,1,2,3})
            {
                using var f=new Fixture();var storage=f.Publish();Intent(f,storage);
                f.Em.SetComponentData(f.Root,new AriaPlaySessionComponent{Phase=AriaPlayPhase.Touching,Pressed=1,Target=new Vector2(55,66)});
                var next=View();
                if(kind==0){var shell=f.Em.GetComponentData<UiShellStateComponent>(f.Root);shell.TransitionSequenceId++;f.Em.SetComponentData(f.Root,shell);}
                if(kind==1)next.Frame--;
                if(kind==2)next.Time--;
                if(kind==3)next.Active=false;
                UiShellRuntimeGateway.PublishAriaSkirmishObservation(next);
                Assert.AreEqual(default(AriaPlaySessionComponent),f.Em.GetComponentData<AriaPlaySessionComponent>(f.Root),"old root contact survived scope change "+kind);
                Assert.AreEqual(default(AriaPlayObservationComponent),f.Em.GetComponentData<AriaPlayObservationComponent>(f.Root));
                Assert.AreEqual(default(AriaSkirmishPlanComponent),f.Em.GetComponentData<AriaSkirmishPlanComponent>(storage));
                f.Tick();Assert.AreEqual(AriaPlayPhase.Manual,f.Em.GetComponentData<AriaPlaySessionComponent>(f.Root).Phase);
            }
        }
        public static void RunFocusedValidation()
        {
            var t=new AriaUiOwnedStorageTests();
            t.ActualFullRootPublishesCompleteValuesAndPlannerBridgesTouch();
            t.NormalStartStopResetsOwnedPlanAndOriginalTouchOwner();
            t.TransitionRejectsIntentImmediatelyAndClearsOldActiveTouch();
            t.NonMatchAndInactiveObservationCannotLeakIntentOrEraseCampaignTouch();
            t.DestroyedRootStorageIsRemovedAndReplacementStartsFresh();
            t.DuplicateStorageRefusesAndWorldReplacementHasNoCachedOwner();
            t.RepublishBeforePlannerCannotCarryOldContactIntoNewScope();
            Debug.Log("[AriaUiOwnedStorageTests] result=Passed controls=7 actualUiShellBootstrap=1 originalTouchOwner=1 normalMissionAcceptance=Pending");
        }
        public static void RunVerifySharedUiStorageAndWarmup()
        {
            SkirmishInstalledAircraftSharedPreparation.Run();
            RunFocusedValidation();
            Game.Editor.S005SceneArtifactWarmup.Run();
            Debug.Log("[AriaUiOwnedStoragePreparation] result=Passed sharedControls=150 uiStorageControls=7 warmupScenes=2 snapshotAfterWrapperExit=1 normalMissionAcceptance=Pending");
        }
    }
}
#endif
