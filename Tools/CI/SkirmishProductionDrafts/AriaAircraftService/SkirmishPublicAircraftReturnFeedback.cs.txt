using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public static class SkirmishPublicAircraftReturnFeedback
    {
        // Called only after the existing command service accepted this exact actor.
        // An observational failure must never change the ordinary command outcome.
        public static void ObserveAccepted(EntityManager em,Entity actor,in RtsSelectionCommandIntentRequestElement request)
        {
            try
            {
                if(request.Kind!=RtsSelectionCommandIntentKind.ReturnToBase||request.RequestId<=0||request.Frame<0||
                    !TryReadActor(em,actor,out var session,out var attempt,out var own,out uint group))return;
                var air=em.GetComponentData<UnitAirComponent>(actor);
                if(air.HomeInitialized==0||air.ReturningHome==0||!math.all(math.isfinite(air.HomePosition)))return;
                var prior=em.HasComponent<SkirmishPublicAircraftReturnReceipt>(actor)?em.GetComponentData<SkirmishPublicAircraftReturnReceipt>(actor):default;
                if(prior.Revision==uint.MaxValue)return;
                var row=new SkirmishPublicAircraftReturnReceipt {Session=session,Actor=actor,Attempt=attempt.SessionId,
                    StableId=own.StableObjectId,WorldSequence=em.WorldUnmanaged.SequenceNumber,SetupHash=attempt.SetupHash,
                    GroupId=group,Revision=prior.Revision+1,RequestId=request.RequestId,Frame=request.Frame,
                    Role=em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role,PrefabKey=em.GetComponentData<UnitSourcePrefabKey>(actor).Value,
                    Screen=request.ScreenPosition,ScreenKnown=request.HasScreenPosition,HomeCell=air.HomeCell,HomePosition=air.HomePosition};
                // UI-button requests may omit a world pointer. Use the same actual
                // current pointer boundary as normal Hold feedback, never a target guess.
                if(row.ScreenKnown==0)
                {
                    using var input=em.CreateEntityQuery(typeof(RtsSelectionInputStateComponent));
                    if(input.CalculateEntityCount()==1)
                    {var pointer=input.GetSingleton<RtsSelectionInputStateComponent>();row.Screen=pointer.LastKnownPointerPosition;row.ScreenKnown=pointer.HasLastKnownPointerPosition;}
                }
                if(row.ScreenKnown==0||!math.all(math.isfinite(row.Screen)))return;
                if(em.HasComponent<SkirmishPublicAircraftReturnReceipt>(actor))em.SetComponentData(actor,row);else em.AddComponentData(actor,row);
            }
            catch(System.Exception){ /* Readout is optional; ordinary accepted Return remains accepted. */ }
        }
        public static bool TryReadActor(EntityManager em,Entity actor,out Entity session,
            out SkirmishExpandedSessionComponent attempt,out SkirmishAttemptOwnedComponent own,out uint group)
        {
            session=Entity.Null;attempt=default;own=default;group=0;
            if(!em.Exists(actor)||em.HasComponent<Disabled>(actor)||em.HasComponent<Prefab>(actor)||em.HasComponent<Parent>(actor)||
                em.HasComponent<UnitDeathAnimationComponent>(actor)||!em.HasComponent<LocalTransform>(actor)||
                !em.HasComponent<UnitHealth>(actor)||em.GetComponentData<UnitHealth>(actor).Current<=0||
                !em.HasComponent<UnitAirComponent>(actor)||!em.HasComponent<UnitAirMovement>(actor)||
                !em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=1||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||!em.HasComponent<SkirmishUnitRoleComponent>(actor)||!em.HasComponent<UnitSourcePrefabKey>(actor)||
                !em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor))return false;
            own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
            if(own.FactionId!=1||own.IsStructure!=0||own.CapacityReleased!=0||own.StableObjectId.Length==0||
                role.Category!=SkirmishPopulationCategory.Air||SkirmishRoleIds.Category(role.Role)!=SkirmishPopulationCategory.Air||
                SpawnableKeyText.Normalize(em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString())!=SpawnableKeyText.Normalize(SkirmishRoleCatalogConfig.RuntimePrefabKey(role.Role))||
                !math.all(math.isfinite(em.GetComponentData<LocalTransform>(actor).Position)))return false;
            using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if(sessions.CalculateEntityCount()!=1)return false;
            session=sessions.GetSingletonEntity();attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(attempt.IsLegacy!=0||attempt.Phase!=SkirmishSessionPhase.Playing||attempt.ObjectiveKind==SkirmishObjectiveKind.BaseAssault||
                attempt.SessionId!=own.SessionId)return false;
            group=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId;
            if(group==0||!em.HasBuffer<SkirmishArmyGroupRecord>(session))return false;
            int groupRows=0;
            foreach(var row in em.GetBuffer<SkirmishArmyGroupRecord>(session,true))if(row.GroupId==group)
            {if(row.FactionId!=1||row.Role!=role.Role||row.Domain!=SkirmishPopulationCategory.Air||row.AliveCount!=1)return false;groupRows++;}
            if(groupRows!=1)return false;
            using var members=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishArmyGroupMembershipComponent),typeof(UnitHealth));
            using var actors=members.ToEntityArray(Allocator.Temp);
            int count=0;
            foreach(var member in actors)
            {
                var memberOwn=em.GetComponentData<SkirmishAttemptOwnedComponent>(member);
                if(memberOwn.SessionId!=attempt.SessionId||em.GetComponentData<UnitHealth>(member).Current<=0)continue;
                if(memberOwn.StableObjectId==own.StableObjectId&&member!=actor)return false;
                if(em.GetComponentData<SkirmishArmyGroupMembershipComponent>(member).GroupId==group)
                {if(member!=actor)return false;count++;}
            }
            return count==1;
        }
        public static bool TryReadReceipt(EntityManager em,Entity actor,out SkirmishPublicAircraftReturnReceipt row)
        {
            row=default;
            if(!TryReadActor(em,actor,out var session,out var attempt,out var own,out uint group)||
                !em.HasComponent<SkirmishPublicAircraftReturnReceipt>(actor))return false;
            row=em.GetComponentData<SkirmishPublicAircraftReturnReceipt>(actor);
            var air=em.GetComponentData<UnitAirComponent>(actor);
            return row.Session==session&&row.Actor==actor&&row.Attempt==attempt.SessionId&&row.StableId==own.StableObjectId&&
                row.WorldSequence==em.WorldUnmanaged.SequenceNumber&&row.SetupHash==attempt.SetupHash&&row.GroupId==group&&
                row.Role==em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role&&row.PrefabKey==em.GetComponentData<UnitSourcePrefabKey>(actor).Value&&
                air.HomeInitialized!=0&&air.HomeCell.Equals(row.HomeCell)&&air.HomePosition.Equals(row.HomePosition)&&math.all(math.isfinite(row.HomePosition))&&
                row.Revision>0&&row.RequestId>0&&row.Frame>=0&&row.ScreenKnown==1&&math.all(math.isfinite(row.Screen));
        }
    }
}
