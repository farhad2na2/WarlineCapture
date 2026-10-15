using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Tests.Editor
{
    public sealed class AriaAircraftServiceTests
    {
        static AriaSkirmishObservation View()=>new AriaSkirmishObservation {Frame=30,Time=100,ExpandedSession=true,
            ExpandedSelectedGroups=1,SelectedAircraft=true,ExpandedPageCount=1,ExpandedTotalGroups=2,
            ReturnAircraft=new AriaTouchTarget{Available=true,Id=66},
            PublicObjective=new AriaObjectiveTouchObservation{Applicable=true,Presented=true,Supported=true,PresentationEpoch=1,GroupId=11,PublicKey=22,Action=AriaObjectiveAction.Move},
            ObjectiveSupport=new AriaObjectiveSupportObservation{Presented=true,SimulationSeconds=50,DismountedCombatGroups=2},
            AircraftService=new AriaAircraftServiceObservation{Presented=true,Actor=new UiEntityHandle(5,3),World=18,GroupId=7,SetupHash=123,Airborne=true,HomeKnown=true}};
        static AriaSkirmishPlanComponent Plan()=>new AriaSkirmishPlanComponent {ObjectiveTouch=new AriaObjectiveTouchPlan{Stage=4,GroupId=11,PublicKey=22,PresentationEpoch=1,Action=AriaObjectiveAction.Move},
            ObjectiveSupport=new AriaObjectiveSupportPlan{Epoch=1,Action=AriaObjectiveSupportAction.ReturnAircraft,Deadline=200}};
        static void Step(ref AriaSkirmishObservation view,ref AriaSkirmishPlanComponent plan,out AriaPlayObservationComponent output)
        {output=default;var touch=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};AriaSkirmishPlanSystem.Step(view,ref plan,ref touch,ref output);view.Frame++;}
        [Test]public void ExactActorReceiptContactAndHomeAreAllRequired()
        {
            var v=View();var p=Plan();Step(ref v,ref p,out _);Step(ref v,ref p,out var command);Assert.AreEqual(66,command.TargetId);
            v.AircraftService.ReturnRevision=1;v.AircraftService.RequestFrame=31;v.AircraftService.ReturnContactTarget=66;
            v.AircraftService.AcceptedReturnContact=true;v.AircraftService.Airborne=false;v.AircraftService.PooledFactionFuel=100;
            Step(ref v,ref p,out _);Assert.IsFalse(p.ObjectiveSupport.ObservedAcceptance,"Parked elsewhere is not home");
            v.AircraftService.AtHome=true;Step(ref v,ref p,out _);Assert.IsTrue(p.ObjectiveSupport.ObservedAcceptance);
            Assert.AreEqual(4,p.ObjectiveTouch.Stage);
        }
        [Test]public void ReplacementAircraftCannotCompleteAnOldReturn()
        {
            var v=View();var p=Plan();Step(ref v,ref p,out _);Step(ref v,ref p,out _);
            v.AircraftService.Actor=new UiEntityHandle(5,4);v.AircraftService.AcceptedReturnContact=true;v.AircraftService.ReturnRevision=2;
            Step(ref v,ref p,out _);Assert.IsFalse(p.ObjectiveSupport.ObservedAcceptance);
            Assert.AreEqual(AriaObjectiveSupportAction.None,p.ObjectiveSupport.Action);
        }
        [Test]public void WrongReturnButtonContactCannotCertifyService()
        {
            var v=View();var p=Plan();Step(ref v,ref p,out _);Step(ref v,ref p,out _);
            v.AircraftService.ReturnRevision=1;v.AircraftService.RequestFrame=31;v.AircraftService.ReturnContactTarget=67;
            v.AircraftService.AcceptedReturnContact=true;v.AircraftService.Airborne=false;v.AircraftService.AtHome=true;v.AircraftService.PooledFactionFuel=100;
            Step(ref v,ref p,out _);Assert.IsFalse(p.ObjectiveSupport.ObservedAcceptance);Assert.AreEqual(2,p.ObjectiveSupport.Aircraft.Stage);
        }
        [Test]public void PublicContactRejectsWrongFrameAndScreenWithoutInventingAcceptance()
        {
            var m=new UiAircraftServiceModel{Visible=true,HasReceipt=true,CompletedTarget=66,RequestId=9,RequestFrame=30,CompletedFrame=31};
            Assert.IsTrue(SkirmishPresentedAircraftServicePolicy.ContactMatches(m));
            m.CompletedFrame=33;Assert.IsFalse(SkirmishPresentedAircraftServicePolicy.ContactMatches(m));
            m.CompletedFrame=31;m.CompletedScreen=new UnityEngine.Vector2(3,0);Assert.IsFalse(SkirmishPresentedAircraftServicePolicy.ContactMatches(m));
            m.CompletedScreen=default;m.HasReceipt=false;Assert.IsFalse(SkirmishPresentedAircraftServicePolicy.ContactMatches(m));
        }
        [Test]public void MultipleAircraftDoNotForceAggregateReturnOrSuppressObjectiveOrder()
        {
            var v=View();v.OwnAttackAirLive=3;var p=Plan();Step(ref v,ref p,out _);Step(ref v,ref p,out var command);
            Assert.AreEqual(66,command.TargetId);Assert.AreEqual(new UiEntityHandle(5,3),p.ObjectiveSupport.Aircraft.Actor);
            Assert.AreEqual(11,p.ObjectiveTouch.GroupId);Assert.AreEqual(4,p.ObjectiveTouch.Stage);
        }
        [TestCase(false)][TestCase(true)]public void ActualNormalReturnPublishesOnlyExactOwnedActor(bool foreign)
        {
            using var world=new World("AircraftReturnNormalFixture");var em=world.EntityManager;
            var session=em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            em.SetComponentData(session,new SkirmishExpandedSessionComponent{SessionId="attempt",SetupHash=123,Phase=SkirmishSessionPhase.Playing,ObjectiveKind=SkirmishObjectiveKind.FrontlineControl});
            var actor=em.CreateEntity(typeof(SelectedUnitTag),typeof(SkirmishAttemptOwnedComponent),typeof(Faction),typeof(UnitHealth),typeof(LocalTransform),
                typeof(UnitAirComponent),typeof(UnitAirMovement),typeof(UnitGrid),typeof(UnitMove),typeof(SkirmishUnitRoleComponent),typeof(SkirmishArmyGroupMembershipComponent),typeof(UnitSourcePrefabKey));
            em.SetComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId=foreign?"foreign":"attempt",StableObjectId="air.1",FactionId=1});
            em.SetComponentData(actor,new Faction{Id=1});em.SetComponentData(actor,new UnitHealth{Current=70,Max=100});em.SetComponentData(actor,LocalTransform.Identity);
            em.SetComponentData(actor,new UnitAirComponent{HomeInitialized=1,Airborne=1,HomeCell=new int2(4,6)});
            em.SetComponentData(actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.TransportHeli,Category=SkirmishPopulationCategory.Air});
            em.SetComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=7,Domain=SkirmishPopulationCategory.Air});
            em.SetComponentData(actor,new UnitSourcePrefabKey{Value=new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.TransportHeli))});
            em.AddBuffer<SkirmishArmyGroupRecord>(session).Add(new SkirmishArmyGroupRecord{GroupId=7,FactionId=1,Role=SkirmishRoleKind.TransportHeli,Domain=SkirmishPopulationCategory.Air,MemberCount=1,AliveCount=1});
            var queue=em.CreateEntity(typeof(RtsSelectionInputStateComponent));
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(queue).Add(new RtsSelectionCommandIntentRequestElement{Kind=RtsSelectionCommandIntentKind.ReturnToBase,RequestId=9,Frame=30,HasScreenPosition=1,ScreenPosition=new float2(200,100)});
            em.CreateEntity(typeof(RuntimeGameplayStateComponent));var respawn=em.CreateEntity(typeof(RespawnQueueTag),typeof(RespawnQueueComponent));
            em.AddBuffer<RespawnFactionSpawnPoint>(respawn).Add(new RespawnFactionSpawnPoint{FactionId=1,SpawnCell=new int2(4,6)});
            Assert.IsTrue(RtsSelectionImmediateSelectedUnitCommandSystem.ProcessPendingRequests(em,out var kind,out bool accepted,out _));
            Assert.IsTrue(accepted);Assert.AreEqual(RtsSelectionCommandIntentKind.ReturnToBase,kind);
            Assert.AreEqual(1,em.GetComponentData<UnitAirComponent>(actor).ReturningHome);
            Assert.AreEqual(!foreign,SkirmishPublicAircraftReturnFeedback.TryReadReceipt(em,actor,out var row));
            Assert.AreEqual(70,em.GetComponentData<UnitHealth>(actor).Current);
            if(!foreign){Assert.AreEqual(actor,row.Actor);Assert.AreEqual(9,row.RequestId);Assert.AreEqual(7,row.GroupId);}
        }
    }
}
