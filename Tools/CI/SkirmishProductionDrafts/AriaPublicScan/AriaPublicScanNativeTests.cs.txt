using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Tests.Editor
{
    public sealed class AriaPublicScanNativeTests
    {
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void ActualNativeDurationCompletionIsDistinctFromDeathReplacementAndDisappearance(int mutation)
        {
            using var world=new World("ScanDurationOwner");var em=world.EntityManager;
            var session=em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            em.SetComponentData(session,new SkirmishExpandedSessionComponent{SessionId="scan-attempt",SetupHash=99,Phase=SkirmishSessionPhase.Playing,ObjectiveKind=SkirmishObjectiveKind.FrontlineControl});
            em.AddBuffer<SkirmishArmyGroupRecord>(session).Add(new SkirmishArmyGroupRecord{GroupId=7,FactionId=1,Role=SkirmishRoleKind.Drone,Domain=SkirmishPopulationCategory.Air,AliveCount=1,MemberCount=1});
            var actor=em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(Faction),typeof(UnitHealth),typeof(LocalTransform),typeof(UnitGrid),
                typeof(UnitAirComponent),typeof(UnitAirMovement),typeof(SkirmishUnitRoleComponent),typeof(UnitSourcePrefabKey),typeof(SkirmishArmyGroupMembershipComponent),typeof(UnitScanOrder));
            em.SetComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId="scan-attempt",StableObjectId="drone.1",FactionId=1});
            em.SetComponentData(actor,new Faction{Id=1});em.SetComponentData(actor,new UnitHealth{Current=60,Max=100});em.SetComponentData(actor,LocalTransform.Identity);
            em.SetComponentData(actor,new UnitAirComponent{Airborne=1,HomeInitialized=1});
            em.SetComponentData(actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.Drone,Category=SkirmishPopulationCategory.Air});
            em.SetComponentData(actor,new UnitSourcePrefabKey{Value=new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Drone))});
            em.SetComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=7,Domain=SkirmishPopulationCategory.Air});
            em.SetComponentData(actor,new UnitScanOrder{RequestId=9,StartedFrame=20,SourceEntity=actor,RadiusCells=12,HasStarted=1,DurationSeconds=8,ReturnHomeAfterCompletion=1});
            em.AddComponentData(actor,new SkirmishPublicScanReceipt{Session=session,Actor=actor,Attempt="scan-attempt",StableId="drone.1",World=em.WorldUnmanaged.SequenceNumber,
                SetupHash=99,GroupId=7,Revision=1,RequestId=9,Frame=20,RadiusCells=12});
            SkirmishPublicScanFeedback.ObserveCompleted(em,actor,9);
            Assert.AreEqual(0,em.GetComponentData<SkirmishPublicScanReceipt>(actor).Completed,"Early observer cannot complete native duration");
            if(mutation==1)em.SetComponentData(actor,new UnitHealth{Current=0,Max=100});
            if(mutation==2){var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);own.StableObjectId="replacement";em.SetComponentData(actor,own);}
            if(mutation==3)em.RemoveComponent<UnitScanOrder>(actor);
            world.SetTime(new TimeData(10,1));world.GetOrCreateSystem<UnitScanOrderExecutionSystem>().Update(world.Unmanaged);em.CompleteAllTrackedJobs();
            Assert.IsFalse(em.HasComponent<UnitScanOrder>(actor));
            Assert.AreEqual(mutation==0?1:0,em.GetComponentData<SkirmishPublicScanReceipt>(actor).Completed);
            Assert.AreEqual(mutation==1?0:60,em.GetComponentData<UnitHealth>(actor).Current,"Scan observation cannot heal");
        }
    }
}
