using Game.Components;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Runtime
{
    // Normal command-result projection. It publishes no order, selection, money,
    // objective state, enemy fact or automatic gameplay behavior.
    public static partial class SkirmishPublicOrderFeedback
    {
        public const int Version=1;
        // Called with the actual normal Attack source list, never the later UI selection.
        public static uint PrecisionSourceGroup(EntityManager em, NativeArray<Entity> sources, Entity target)
        {
            if(sources.Length==0 || !PrecisionTarget(em,target,out var owner)||
                !SkirmishFogConsumerUtility.PlayerVisible(em,target))return 0;
            uint group=0;var unique=new HashSet<Entity>();
            foreach(var actor in sources)
            {
                if(!unique.Add(actor)||!em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||
                    !em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor)||
                    !em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=1||
                    !SkirmishArmyGroupSystem.IsAlive(em,actor))return 0;
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                uint id=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId;
                if(own.SessionId!=owner.SessionId||own.FactionId!=1||own.IsStructure!=0||own.CapacityReleased!=0||
                    id==0||(group!=0&&group!=id))return 0;
                group=id;
            }
            return group;
        }
        static bool PrecisionTarget(EntityManager em,Entity target,out SkirmishAttemptOwnedComponent owner)
        {
            owner=default;
            if(target==Entity.Null||!em.Exists(target)||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(target)||
                !em.HasComponent<SkirmishStructureIdentityComponent>(target)||
                em.GetComponentData<SkirmishStructureIdentityComponent>(target).DesignatedBase==0)return false;
            owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(target);
            return owner.FactionId!=1&&owner.CapacityReleased==0;
        }
        public static void PublishPrecisionAttack(EntityManager em,in RtsSelectionCommandIntentRequestElement request,
            in AttackOrderCommandSystem.Result result)
        {
            // Only the normal screen-request branch calls this. Native validation still owns fog/range/orders.
            if(request.HasScreenPosition==0||!result.Issued||result.PublicPrecisionGroup==0||
                result.PublicAcceptedMembers<=0||result.PublicAcceptedMembers>result.PublicSourceMembers||
                !PrecisionTarget(em,result.PublicPrecisionTarget,out var own))return;
            using var query=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var session in entities)
            {
                var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                if(attempt.SessionId!=own.SessionId||attempt.IsLegacy!=0||
                    attempt.Phase!=Game.Skirmish.Contracts.SkirmishSessionPhase.Playing||
                    attempt.ObjectiveKind!=Game.Skirmish.Contracts.SkirmishObjectiveKind.BaseAssault)continue;
                uint prior=em.HasComponent<SkirmishPublicPrecisionAttackFeedbackComponent>(session)?
                    em.GetComponentData<SkirmishPublicPrecisionAttackFeedbackComponent>(session).Revision:0;
                if(prior==uint.MaxValue)return;
                var row=new SkirmishPublicPrecisionAttackFeedbackComponent {Revision=prior+1,
                    GroupId=result.PublicPrecisionGroup,OriginalTarget=result.PublicPrecisionTarget,
                    Frame=request.Frame,RequestId=request.RequestId,
                    PublicTargetId=-20001,ScreenPosition=request.ScreenPosition,
                    AcceptedMembers=result.PublicAcceptedMembers,SourceMembers=result.PublicSourceMembers};
                if(em.HasComponent<SkirmishPublicPrecisionAttackFeedbackComponent>(session))em.SetComponentData(session,row);
                else em.AddComponentData(session,row);
                return;
            }
        }
        public static void MarkPrecisionAttackPresented(EntityManager em,in RtsSelectionCommandResultElement result)
        {
            if(result.Kind!=RtsSelectionCommandIntentKind.Attack||result.Accepted==0||result.HasTargetEntity==0)return;
            using var query=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishPublicPrecisionAttackFeedbackComponent));
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var session in entities)
            {
                var row=em.GetComponentData<SkirmishPublicPrecisionAttackFeedbackComponent>(session);
                if(row.RequestId!=result.RequestId||row.Frame!=result.Frame||
                    math.distancesq(row.ScreenPosition,result.ScreenPosition)>.0001f)continue;
                if(!PrecisionTarget(em,row.OriginalTarget,out var own)||
                    em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId!=own.SessionId)continue;
                row.Presented=1;em.SetComponentData(session,row);
            }
        }
        public static void MarkPresented(EntityManager em,RtsSelectionCommandIntentKind kind,int request,int frame)
        {
            using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishPublicOrderFeedbackComponent));
            if(sessions.CalculateEntityCount()!=1)return;
            var session=sessions.GetSingletonEntity();var row=em.GetComponentData<SkirmishPublicOrderFeedbackComponent>(session);
            if(row.Kind!=kind||row.RequestId!=request||row.Frame!=frame)return;
            row.Presented=1;em.SetComponentData(session,row);
        }
        public static void PublishMove(EntityManager em,in RtsSelectionCommandIntentRequestElement request,in SelectedMoveOrderCommandSystem.Result result)
        {
            if(!result.CommandResult.Accepted)
            {
                // A rejection carries exact selected-group/contact/request scope,
                // never an accepted destination or partial-member success claim.
                if(request.HasScreenPosition!=0&&TryUniqueOwnGroup(em,out var rejectedSession,out uint rejectedGroup))
                    Publish(em,rejectedSession,new SkirmishPublicOrderFeedbackComponent{GroupId=rejectedGroup,Kind=RtsSelectionCommandIntentKind.Move,
                        RequestId=request.RequestId,Frame=request.Frame,ScreenPosition=request.ScreenPosition,Accepted=0,HasWorldPosition=0});
                return;
            }
            if(result.PublicAcceptedGroup==0||!TryUniqueOwnGroup(em,out var session,out uint group)||group!=result.PublicAcceptedGroup)return;
            Publish(em,session,new SkirmishPublicOrderFeedbackComponent { GroupId=group,Kind=RtsSelectionCommandIntentKind.Move,
                RequestId=request.RequestId,Frame=request.Frame,ScreenPosition=request.ScreenPosition,
                WorldPosition=result.MarkerPosition,HasWorldPosition=result.CommandResult.Accepted?(byte)1:(byte)0,
                Accepted=result.CommandResult.Accepted?(byte)1:(byte)0 });
        }
        public static void PublishHold(EntityManager em,bool accepted)
        {
            if(!TryUniqueOwnGroup(em,out var session,out uint group))return;
            using(var held=em.CreateEntityQuery(typeof(SelectedUnitTag),typeof(UnitHealth)))
            using(var values=held.ToEntityArray(Allocator.Temp))
                foreach(var actor in values)if(em.GetComponentData<UnitHealth>(actor).Current>0&&!em.HasComponent<HoldPositionOrderTag>(actor))return;
            using var input=em.CreateEntityQuery(typeof(RtsSelectionInputStateComponent));
            if(input.CalculateEntityCount()!=1)return;
            var pointer=input.GetSingleton<RtsSelectionInputStateComponent>();
            if(pointer.HasLastKnownPointerPosition==0)return;
            Publish(em,session,new SkirmishPublicOrderFeedbackComponent {GroupId=group,Kind=RtsSelectionCommandIntentKind.HoldPosition,
                Frame=Time.frameCount,ScreenPosition=pointer.LastKnownPointerPosition,Accepted=accepted?(byte)1:(byte)0});
        }
        public static bool ShouldObserve(EntityManager em)
        {
            using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));if(sessions.CalculateEntityCount()!=1)return false;
            var attempt=sessions.GetSingleton<SkirmishExpandedSessionComponent>();
            return attempt.IsLegacy==0&&attempt.Phase==Game.Skirmish.Contracts.SkirmishSessionPhase.Playing&&attempt.ObjectiveKind!=Game.Skirmish.Contracts.SkirmishObjectiveKind.BaseAssault;
        }
        public static uint VerifyCompleteMoveGroup(EntityManager em,NativeArray<Entity> selected,NativeList<Entity> accepted,NativeList<int2> expectedGoals,bool observeBaseAssault=false)
        {
            if(!accepted.IsCreated||!expectedGoals.IsCreated||accepted.Length!=expectedGoals.Length||accepted.Length!=selected.Length||
                !TryUniqueOwnGroup(em,out _,out uint group,observeBaseAssault))return 0;
            var actual=new HashSet<Entity>();foreach(var actor in selected)if(!actual.Add(actor))return 0;
            for(int i=0;i<accepted.Length;i++)
            {
                var actor=accepted[i];if(!actual.Remove(actor)||!em.HasComponent<ManualMoveOrderTag>(actor)||!em.HasComponent<UnitTarget>(actor))return 0;
                int2 goal=em.HasComponent<UnitLongDistanceMove>(actor)?em.GetComponentData<UnitLongDistanceMove>(actor).FinalGoal:em.GetComponentData<UnitTarget>(actor).Cell;
                if(!goal.Equals(expectedGoals[i]))return 0;
            }
            return actual.Count==0?group:0;
        }
        static void Publish(EntityManager em,Entity session,SkirmishPublicOrderFeedbackComponent row)
        {
            uint prior=em.HasComponent<SkirmishPublicOrderFeedbackComponent>(session)?em.GetComponentData<SkirmishPublicOrderFeedbackComponent>(session).Revision:0;
            if(prior==uint.MaxValue)return;row.Revision=prior+1;
            if(em.HasComponent<SkirmishPublicOrderFeedbackComponent>(session))em.SetComponentData(session,row);else em.AddComponentData(session,row);
        }
        static bool TryUniqueOwnGroup(EntityManager em,out Entity session,out uint group,bool observeBaseAssault=false)
        {
            session=Entity.Null;group=0;
            using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if(sessions.CalculateEntityCount()!=1)return false;
            session=sessions.GetSingletonEntity();var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(attempt.IsLegacy!=0||attempt.Phase!=Game.Skirmish.Contracts.SkirmishSessionPhase.Playing||(!observeBaseAssault&&attempt.ObjectiveKind==Game.Skirmish.Contracts.SkirmishObjectiveKind.BaseAssault))return false;
            using var selected=em.CreateEntityQuery(typeof(SelectedUnitTag),typeof(UnitHealth),typeof(Faction));
            using var actors=selected.ToEntityArray(Allocator.Temp);
            int selectedCount=0;
            foreach(var actor in actors)
            {
                if(em.GetComponentData<UnitHealth>(actor).Current<=0)continue;
                if(!em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||!em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor))return false;
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                uint id=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId;
                if(own.SessionId!=attempt.SessionId||own.FactionId!=1||em.GetComponentData<Faction>(actor).Id!=1||own.IsStructure!=0||own.CapacityReleased!=0||id==0)return false;
                if(group!=0&&group!=id)return false;group=id;
                selectedCount++;
            }
            if(group==0)return false;
            int live=0;
            using var members=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishArmyGroupMembershipComponent),typeof(UnitHealth),typeof(Faction));
            using var all=members.ToEntityArray(Allocator.Temp);
            foreach(var actor in all)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(own.SessionId!=attempt.SessionId||own.FactionId!=1||own.CapacityReleased!=0||em.GetComponentData<UnitHealth>(actor).Current<=0||
                    em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId!=group)continue;
                if(em.GetComponentData<Faction>(actor).Id!=1||!em.HasComponent<SelectedUnitTag>(actor))return false;live++;
            }
            return live>0&&live==selectedCount;
        }
    }
}
