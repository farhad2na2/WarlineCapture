using Game.Components;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class S005GuardMoveReceiptTests
    {
        public static void RunFocusedValidation()
        {var t=new S005GuardMoveReceiptTests();for(int i=0;i<13;i++)t.ExactOwnedFullMemberReceipt(i);for(int i=0;i<4;i++){t.ActualNativeCompleteGroup(i);t.ActualAttackResultObservation(i);}Debug.Log("[S005GuardMoveReceipt] result=Passed controls=21 normalMissionPending=1");}
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
        [TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)][TestCase(11)][TestCase(12)]
        public void ExactOwnedFullMemberReceipt(int mutation)
        {
            var member=new AriaMoveExecutionEvidence.Member{stableId="own.original",entityIndex=5,entityVersion=2,goalWorld=new Vector3(10,0,10)};
            var row=new AriaMoveExecutionEvidence.Row{attempt="attempt",group=7,frame=101,requestFrame=100,requestId=3,receipt=9,accepted=true,completeGroup=true,
                screen=new Vector2(50,60),marker=new Vector3(10,0,10),members=new[]{member}};
            var proof=new S005GuardMoveReceipt{Attempt="attempt",Group=7,DispatchFrame=100,LastRequest=2,CompletedBefore=5,VerifiedReceipt=9,Screen=new Vector2(50,60),Deadline=20,
                QaEnvelope=new Rect(0,0,20,20),Actual=row,Members=new[]{new AriaMoveExecutionEvidence.Member{stableId="own.original",entityIndex=5,entityVersion=2}}};
            if(mutation==1)row.receipt=8;if(mutation==2)row.group=8;if(mutation==3)row.requestFrame=99;if(mutation==4)row.requestId=2;
            if(mutation==5)row.screen=Vector2.zero;if(mutation==6)row.completeGroup=false;if(mutation==7)row.members=System.Array.Empty<AriaMoveExecutionEvidence.Member>();
            if(mutation==8)member.goalWorld=new Vector3(21,0,10);if(mutation==9)member.entityVersion=3;if(mutation==10)row.attempt="foreign";
            var actual=proof.Evaluate(mutation==11?5u:6u,mutation==12,10);
            Assert.That(actual,Is.EqualTo(mutation==0?S005GuardReceiptState.Accepted:mutation>=6&&mutation<=9?S005GuardReceiptState.Retry:S005GuardReceiptState.Waiting));
            if(actual==S005GuardReceiptState.Waiting)Assert.That(proof.Evaluate(mutation==11?5u:6u,mutation==12,21),Is.EqualTo(S005GuardReceiptState.Retry));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void ActualNativeCompleteGroup(int mutation)
        {
            using var world=new World("S005 passive full-member control");var em=world.EntityManager;
            var session=em.CreateEntity();em.AddComponentData(session,new SkirmishExpandedSessionComponent{SessionId=new FixedString64Bytes("guard-native"),Phase=SkirmishSessionPhase.Playing,ObjectiveKind=SkirmishObjectiveKind.BaseAssault});
            var selected=new NativeArray<Entity>(2,Allocator.Temp);using var selectedLifetime=selected;using var accepted=new NativeList<Entity>(Allocator.Temp);using var goals=new NativeList<int2>(Allocator.Temp);
            for(int i=0;i<2;i++)
            {
                var actor=em.CreateEntity();selected[i]=actor;
                em.AddComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId=new FixedString64Bytes("guard-native"),FactionId=1,StableObjectId=new FixedString64Bytes("original-"+i)});
                em.AddComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=7});em.AddComponentData(actor,new UnitHealth{Current=100,Max=100});
                em.AddComponentData(actor,new Faction{Id=1});em.AddComponent<SelectedUnitTag>(actor);em.AddComponent<ManualMoveOrderTag>(actor);
                em.AddComponentData(actor,new UnitTarget{Cell=new int2(10+i,10)});
                if(i==0||mutation!=1){accepted.Add(actor);goals.Add(new int2(10+i,10));}
            }
            if(mutation==2)em.SetComponentData(selected[1],new UnitTarget{Cell=new int2(99,99)});
            if(mutation==3)em.RemoveComponent<SelectedUnitTag>(selected[1]);
            Assert.That(SkirmishPublicOrderFeedback.VerifyCompleteMoveGroup(em,selected,accepted,goals,true),Is.EqualTo(mutation==0?7u:0u));
            Assert.That(SkirmishPublicOrderFeedback.VerifyCompleteMoveGroup(em,selected,accepted,goals),Is.Zero,"Normal BA HUD remains unsupported");
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void ActualAttackResultObservation(int mutation)
        {
            using var world=new World("S005 actual passive Attack Move observation");var em=world.EntityManager;
            var session=em.CreateEntity();em.AddComponentData(session,new SkirmishExpandedSessionComponent{SessionId=new FixedString64Bytes("attack-native"),Phase=SkirmishSessionPhase.Playing,ObjectiveKind=SkirmishObjectiveKind.BaseAssault});
            var grid=em.CreateEntity();em.AddComponentData(grid,new GridConfig{Width=100,Height=100,CellSize=2});
            using var accepted=new NativeList<Entity>(Allocator.Temp);
            for(int i=0;i<2;i++)
            {
                var actor=em.CreateEntity();em.AddComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId=new FixedString64Bytes("attack-native"),FactionId=1,StableObjectId=new FixedString64Bytes("original-"+i)});
                em.AddComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=7});em.AddComponentData(actor,new UnitHealth{Current=100,Max=100});em.AddComponentData(actor,new Faction{Id=1});
                em.AddComponent<SelectedUnitTag>(actor);em.AddComponent<ManualMoveOrderTag>(actor);em.AddComponentData(actor,new UnitTarget{Cell=new int2(10+i,10)});
                em.AddComponentData(actor,new AttackMoveOrder{Destination=new int2(mutation==2&&i==1?99:10+i,10)});
                if(mutation!=1||i==0)accepted.Add(actor);
            }
            AriaMoveExecutionEvidence.Row observed=null;void Receive(AriaMoveExecutionEvidence.Row row){observed=row;}
            AriaCommandEvidence.Begin();AriaMoveExecutionEvidence.Recorded+=Receive;
            try
            {
                var request=new RtsSelectionCommandIntentRequestElement{InputReceipt=7,RequestId=3,Frame=10};
                var result=SelectedMoveOrderCommandSystem.Result.Success(new int2(10,10),new float3(20,0,20),1,7);
                if(mutation==3)AriaMoveExecutionEvidence.ObserveAttackMove(em,accepted.AsArray(),result);
                else{using var scope=new AriaMoveExecutionEvidence.AttackRequestScope(request);AriaMoveExecutionEvidence.ObserveAttackMove(em,accepted.AsArray(),result);}
                if(mutation==3)Assert.That(observed,Is.Null,"No normal request scope cannot produce attributed result");
                else{Assert.That(observed,Is.Not.Null);Assert.That(observed.completeGroup,Is.EqualTo(mutation==0));Assert.That(observed.operation,Is.EqualTo("AttackMove"));Assert.That(observed.requestId,Is.EqualTo(3));}
            }
            finally{AriaMoveExecutionEvidence.Recorded-=Receive;AriaCommandEvidence.End();}
        }
    }
}
