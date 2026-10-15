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
        public static uint VerifyCompleteMoveGroup(EntityManager em,NativeArray<Entity> selected,NativeList<Entity> accepted,NativeList<int2> expectedGoals)
        {
            if(!accepted.IsCreated||!expectedGoals.IsCreated||accepted.Length!=expectedGoals.Length||accepted.Length!=selected.Length||
                !TryUniqueOwnGroup(em,out _,out uint group))return 0;
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
        static bool TryUniqueOwnGroup(EntityManager em,out Entity session,out uint group)
        {
            session=Entity.Null;group=0;
            using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if(sessions.CalculateEntityCount()!=1)return false;
            session=sessions.GetSingletonEntity();var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(attempt.IsLegacy!=0||attempt.Phase!=Game.Skirmish.Contracts.SkirmishSessionPhase.Playing||attempt.ObjectiveKind==Game.Skirmish.Contracts.SkirmishObjectiveKind.BaseAssault)return false;
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
