using System;
using System.Collections.Generic;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    // Ephemeral normal-command presentation metadata, owned by this session.
    // Never saved as objective state and never used to issue a command.
    public sealed class SkirmishOriginalUnloadPresentation : IComponentData
    {
        public int RequestId,Frame;
        public uint GroupId,CarrierGroupId;
        public Entity Carrier;
        public string CarrierId;
        public float2 Screen;
        public bool RequestAccepted,Presented,ScopeAccepted;
        public float BeganAt;
        public string[] Ids;
        public Entity[] Actors;

        public static void CaptureBefore(EntityManager em,in RtsSelectionCommandIntentRequestElement request)
        {
            try{CaptureBeforeCore(em,request);}
            catch(Exception error){UnityEngine.Debug.LogWarning("[PublicUnloadObservation] result=Failed stage=BeforeRequest request="+request.RequestId+" error="+error.GetType().Name);}
        }
        static void CaptureBeforeCore(EntityManager em,in RtsSelectionCommandIntentRequestElement request)
        {
            if(request.Kind!=RtsSelectionCommandIntentKind.DisembarkTransport||request.HasTargetEntity==0||request.HasScreenPosition==0)return;
            using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if(sessions.CalculateEntityCount()!=1)return;
            var session=sessions.GetSingletonEntity();
            if(!SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out var goal,out _)||!goal.Score.ExitOpen)return;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var carrier=request.TargetEntity;
            if(!em.Exists(carrier)||!em.HasComponent<SkirmishAttemptOwnedComponent>(carrier))return;
            string carrierId=em.GetComponentData<SkirmishAttemptOwnedComponent>(carrier).StableObjectId.ToString();
            SkirmishPublicOriginalTransportGroup chosen=null;
            if(!em.HasBuffer<SkirmishArmyGroupRecord>(session))return;
            foreach(var group in em.GetBuffer<SkirmishArmyGroupRecord>(session,true))
                if(SkirmishPublicOriginalTransportProjection.TryRead(em,session,goal,group.GroupId,out var candidate)&&
                    candidate.CarrierPublicId==carrierId&&(chosen==null||candidate.OriginalGroupId<chosen.OriginalGroupId))chosen=candidate;
            if(chosen==null)return;
            var actors=new Entity[chosen.CarrierOriginalIds.Length];
            using var query=em.CreateEntityQuery(new EntityQueryDesc{
                All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState});
            using var values=query.ToEntityArray(Allocator.Temp);
            for(int i=0;i<actors.Length;i++)
            {
                foreach(var actor in values)
                {
                    var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    if(own.SessionId!=native.SessionId||own.StableObjectId.ToString()!=chosen.CarrierOriginalIds[i])continue;
                    if(actors[i]!=Entity.Null)return;actors[i]=actor;
                }
                if(actors[i]==Entity.Null)return;
            }
            var row=new SkirmishOriginalUnloadPresentation{RequestId=request.RequestId,Frame=request.Frame,Screen=request.ScreenPosition,
                GroupId=chosen.OriginalGroupId,CarrierGroupId=chosen.CarrierGroupId,Carrier=carrier,CarrierId=carrierId,BeganAt=UnityEngine.Time.unscaledTime,
                Ids=chosen.CarrierOriginalIds,Actors=actors};
            em.AddComponentObject(session,row);
            if(!em.HasComponent<SkirmishPublicOrderFeedbackComponent>(session))em.AddComponentData(session,new SkirmishPublicOrderFeedbackComponent());
        }

        public static void ObserveResult(EntityManager em,in RtsSelectionCommandResultElement result)
        {
            try{ObserveResultCore(em,result);}
            catch(Exception error){UnityEngine.Debug.LogWarning("[PublicUnloadObservation] result=Failed stage=NativeResult request="+result.RequestId+" error="+error.GetType().Name);}
        }
        static void ObserveResultCore(EntityManager em,in RtsSelectionCommandResultElement result)
        {
            if(result.Kind!=RtsSelectionCommandIntentKind.DisembarkTransport)return;
            using var query=em.CreateEntityQuery(typeof(SkirmishOriginalUnloadPresentation),typeof(SkirmishExpandedSessionComponent));
            if(query.CalculateEntityCount()!=1)return;
            var session=query.GetSingletonEntity();var row=em.GetComponentObject<SkirmishOriginalUnloadPresentation>(session);
            if(row.RequestId!=result.RequestId||row.Frame!=result.Frame||row.Carrier!=result.TargetEntity)return;
            row.RequestAccepted=result.Accepted!=0;
            row.ScopeAccepted=row.RequestAccepted&&TryReadProgress(em,session,row,out _,out _,out _);
            if(!row.ScopeAccepted)return;
            SkirmishPublicOrderFeedback.PublishUnloadRequest(em,session,row);
        }

        public static void MarkPresented(EntityManager em,in RtsSelectionCommandResultElement result)
        {
            try{MarkPresentedCore(em,result);}
            catch(Exception error){UnityEngine.Debug.LogWarning("[PublicUnloadObservation] result=Failed stage=Presentation request="+result.RequestId+" error="+error.GetType().Name);}
        }
        static void MarkPresentedCore(EntityManager em,in RtsSelectionCommandResultElement result)
        {
            using var query=em.CreateEntityQuery(typeof(SkirmishOriginalUnloadPresentation));if(query.CalculateEntityCount()!=1)return;
            var row=em.GetComponentObject<SkirmishOriginalUnloadPresentation>(query.GetSingletonEntity());
            if(result.Kind==RtsSelectionCommandIntentKind.DisembarkTransport&&row.RequestId==result.RequestId&&row.Frame==result.Frame&&row.Carrier==result.TargetEntity)
                row.Presented=true;
        }

        public static bool TryReadProgress(EntityManager em,Entity session,SkirmishOriginalUnloadPresentation row,
            out int completed,out int pending,out bool partialPlacement)
        {
            completed=0;pending=0;partialPlacement=false;
            try
            {
                if(!SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out var goal,out _))return false;
                return TryReadProgressFromPublicGoal(em,session,row,goal,out completed,out pending,out partialPlacement);
            }
            catch(Exception){return false;}
        }

        internal static bool TryReadProgressFromPublicGoal(EntityManager em,Entity session,SkirmishOriginalUnloadPresentation row,
            SkirmishPublicGoalSnapshot goal,out int completed,out int pending,out bool partialPlacement)
        {
            completed=0;pending=0;partialPlacement=false;
            if(row==null||row.Actors==null||row.Ids==null||row.Actors.Length==0||row.Actors.Length!=row.Ids.Length||
                !em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session))return false;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(goal==null||goal.ObserverFaction!=1||goal.Perspective!=SkirmishPublicGoalPerspective.Evacuate||
                goal.DefinitionId!=native.DefinitionId.ToString()||goal.DefinitionVersion!=native.ContentVersion)return false;
            bool ownCarrier=em.Exists(row.Carrier)&&em.HasComponent<SkirmishAttemptOwnedComponent>(row.Carrier)&&
                em.HasComponent<UnitHealth>(row.Carrier)&&em.GetComponentData<UnitHealth>(row.Carrier).Current>0&&
                em.HasComponent<Faction>(row.Carrier)&&em.GetComponentData<Faction>(row.Carrier).Id==1&&
                em.GetComponentData<SkirmishAttemptOwnedComponent>(row.Carrier).SessionId==native.SessionId&&
                em.GetComponentData<SkirmishAttemptOwnedComponent>(row.Carrier).StableObjectId.ToString()==row.CarrierId&&
                em.GetComponentData<SkirmishAttemptOwnedComponent>(row.Carrier).FactionId==1&&
                em.GetComponentData<SkirmishAttemptOwnedComponent>(row.Carrier).CapacityReleased==0;
            bool queued=ownCarrier&&em.HasComponent<UnitAirComponent>(row.Carrier)&&em.HasComponent<UnitAirMovement>(row.Carrier)&&
                (em.HasComponent<UnitTransportRopeDisembarkRequest>(row.Carrier)||em.HasComponent<UnitTransportAirdropRequest>(row.Carrier));
            for(int i=0;i<row.Actors.Length;i++)
            {
                SkirmishPublicGoalNode? shown=null;
                for(int n=0;n<goal.NodeCount;n++)if(goal.Node(n).PublicId==row.Ids[i])shown=goal.Node(n);
                if(!shown.HasValue||shown.Value.Kind!=SkirmishPublicGoalNodeKind.OwnDesignatedSubject||
                    shown.Value.SubjectDisposition==SkirmishPublicSubjectDisposition.Lost)return false;
                if(shown.Value.SubjectDisposition==SkirmishPublicSubjectDisposition.Evacuated){completed++;continue;}
                var actor=row.Actors[i];
                if(!em.Exists(actor)||!em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||!em.HasComponent<UnitHealth>(actor)||
                    em.GetComponentData<UnitHealth>(actor).Current<=0||!em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor)||
                    em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId!=row.GroupId)return false;
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(own.SessionId!=native.SessionId||own.FactionId!=1||own.CapacityReleased!=0||own.StableObjectId.ToString()!=row.Ids[i]||
                    !em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=1)return false;
                if(em.HasComponent<UnitTransportPassenger>(actor))
                {
                    if(!ownCarrier||!shown.Value.Mounted)return false;
                    if(em.GetComponentData<UnitTransportPassenger>(actor).Transport!=row.Carrier)return false;
                    if(!em.HasBuffer<UnitTransportPassengerElement>(row.Carrier))return false;
                    int reciprocal=0;foreach(var item in em.GetBuffer<UnitTransportPassengerElement>(row.Carrier,true))if(item.Passenger==actor)reciprocal++;
                    if(reciprocal!=1)return false;
                    if(!queued){partialPlacement=true;pending++;continue;}
                    pending++;continue;
                }
                if(em.HasComponent<UnitTransportParachuteDropComponent>(actor)||em.HasComponent<UnitTransportRopeDropComponent>(actor)||em.HasComponent<UnitTransportCargoDropComponent>(actor))
                {pending++;continue;}
                if(em.HasComponent<UnitTransportCargoPassenger>(actor)||em.HasComponent<Disabled>(actor))return false;
                completed++;
            }
            return !partialPlacement&&completed+pending==row.Actors.Length;
        }
    }
    public static partial class SkirmishPublicOrderFeedback
    {
        public static void PublishUnloadRequest(EntityManager em,Entity session,SkirmishOriginalUnloadPresentation request)
        {
            Publish(em,session,new SkirmishPublicOrderFeedbackComponent{GroupId=request.GroupId,NavigationGroupId=request.CarrierGroupId,
                Kind=RtsSelectionCommandIntentKind.DisembarkTransport,RequestId=request.RequestId,Frame=request.Frame,ScreenPosition=request.Screen,Accepted=1});
        }
    }
}
