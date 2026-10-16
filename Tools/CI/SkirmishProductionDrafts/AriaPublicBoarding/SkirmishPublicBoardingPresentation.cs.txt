using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    // Ephemeral normal-input observation. No production/order/health mutation.
    public sealed class SkirmishPublicBoardingPresentation : IComponentData
    {
        public Entity Session,Carrier;
        public FixedString64Bytes Attempt,CarrierId;
        public FixedString64Bytes CarrierPrefab;
        public SkirmishRoleKind CarrierRole;
        public ulong World;
        public uint SetupHash,GroupId,CarrierGroupId;
        public int RequestId,Frame;
        public float2 Screen;
        public Entity[] Actors;
        public string[] Ids;
        public bool Accepted,Presented;
        public sealed class Candidate
        {
            public Entity Session,Carrier;
            public uint GroupId,CarrierGroupId;
            public float3 CarrierPosition;
            public string CarrierId;
            public Entity[] Actors;
            public string[] Ids;
            public int Seats;
        }
        static bool Own(EntityManager em,Entity actor,FixedString64Bytes attempt,bool mounted=false)
        {
            if(!em.Exists(actor)||em.HasComponent<Prefab>(actor)||em.HasComponent<UnitDeathAnimationComponent>(actor)||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||!em.HasComponent<Faction>(actor)||
                !em.HasComponent<UnitHealth>(actor)||em.GetComponentData<UnitHealth>(actor).Current<=0||
                !em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor))return false;
            var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            return own.SessionId==attempt&&own.FactionId==1&&own.IsStructure==0&&own.CapacityReleased==0&&own.StableObjectId.Length>0&&
                em.GetComponentData<Faction>(actor).Id==1&&(mounted||!em.HasComponent<Disabled>(actor)&&!em.HasComponent<Parent>(actor));
        }
        public static bool TryCandidate(EntityManager em,Entity session,out Candidate candidate)
        {
            candidate=null;
            if(!SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out var goal,out _)||goal.Perspective!=SkirmishPublicGoalPerspective.Evacuate||!goal.Score.ExitOpen)return false;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            using var selectedQuery=em.CreateEntityQuery(typeof(SelectedUnitTag));using var selected=selectedQuery.ToEntityArray(Allocator.Temp);
            if(selected.Length==0)return false;
            uint group=0;var ids=new List<string>();var actors=new List<Entity>();var stableIds=new HashSet<string>(StringComparer.Ordinal);
            foreach(var actor in selected)
            {
                if(!Own(em,actor,attempt.SessionId)||!em.HasComponent<UnitGrid>(actor)||!em.HasComponent<SkirmishUnitRoleComponent>(actor)||
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Category!=SkirmishPopulationCategory.Infantry||
                    em.HasComponent<UnitTransportPassenger>(actor)||em.HasComponent<UnitTransportCargoPassenger>(actor)||
                    em.HasComponent<UnitTransportBoardingTarget>(actor)||em.HasComponent<ManualMoveOrderTag>(actor))return false;
                uint current=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId;
                if(current==0||group!=0&&group!=current)return false;group=current;
                string id=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).StableObjectId.ToString();
                bool original=false;
                for(int i=0;i<goal.NodeCount;i++)
                {var node=goal.Node(i);if(node.PublicId==id&&node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject&&node.RevealedSubjectKind==SkirmishPublicSubjectKind.DesignatedRifle&&node.SubjectDisposition==SkirmishPublicSubjectDisposition.Live)original=true;}
                if(!original||!stableIds.Add(id))return false;actors.Add(actor);ids.Add(id);
            }
            using var allQuery=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishArmyGroupMembershipComponent),typeof(UnitHealth));
            using var all=allQuery.ToEntityArray(Allocator.Temp);int groupMembers=0;
            foreach(var member in all)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(member);
                if(own.SessionId==attempt.SessionId&&em.GetComponentData<UnitHealth>(member).Current>0&&
                    em.GetComponentData<SkirmishArmyGroupMembershipComponent>(member).GroupId==group)
                {if(!em.HasComponent<SelectedUnitTag>(member))return false;groupMembers++;}
            }
            if(groupMembers!=selected.Length)return false;
            Entity best=Entity.Null;string bestId=null;int bestDistance=int.MaxValue,bestSeats=0;
            foreach(var carrier in all)
            {
                if(!Own(em,carrier,attempt.SessionId)||actors.Contains(carrier)||!em.HasComponent<UnitGrid>(carrier)||!em.HasComponent<UnitFootprint>(carrier)||
                    !em.HasComponent<LocalTransform>(carrier)||!em.HasComponent<SkirmishUnitRoleComponent>(carrier)||!em.HasComponent<UnitSourcePrefabKey>(carrier)||
                    em.HasComponent<UnitTransportPassenger>(carrier)||em.HasComponent<UnitTransportCargoPassenger>(carrier)||em.HasComponent<UnitTransportBoardingTarget>(carrier))continue;
                var role=em.GetComponentData<SkirmishUnitRoleComponent>(carrier).Role;
                if(role==SkirmishRoleKind.ConvoyLogic||SpawnableKeyText.Normalize(em.GetComponentData<UnitSourcePrefabKey>(carrier).Value.ToString())!=SpawnableKeyText.Normalize(SkirmishRoleCatalogConfig.RuntimePrefabKey(role)))continue;
                var capacity=TransportBoardingCapacitySystemHelper.ResolveTransportSlotAvailability(em,carrier);
                if(capacity.AvailableSoldierSeats<selected.Length)continue;
                int2 cell=em.GetComponentData<UnitGrid>(carrier).Cell;int direct=TransportBoardingCommandSystem.GetTransportBoardingDirectCells(em,carrier);int far=0;
                foreach(var actor in actors)far=math.max(far,math.cmax(math.abs(em.GetComponentData<UnitGrid>(actor).Cell-cell)));
                // This bounded first boarding policy uses only actually nearby
                // own carriers. Native path/landing/occupancy still decides legality.
                if(far>direct)continue;
                string id=em.GetComponentData<SkirmishAttemptOwnedComponent>(carrier).StableObjectId.ToString();
                if(best!=Entity.Null&&(far>bestDistance||far==bestDistance&&StringComparer.Ordinal.Compare(id,bestId)>=0))continue;
                best=carrier;bestId=id;bestDistance=far;bestSeats=capacity.AvailableSoldierSeats;
            }
            if(best==Entity.Null)return false;
            var position=em.GetComponentData<LocalTransform>(best).Position;if(!math.all(math.isfinite(position)))return false;
            candidate=new Candidate{Session=session,GroupId=group,Carrier=best,CarrierGroupId=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(best).GroupId,
                CarrierId=bestId,CarrierPosition=position,Actors=actors.ToArray(),Ids=ids.ToArray(),Seats=bestSeats};
            return candidate.CarrierGroupId!=0;
        }
        public static void CaptureBefore(EntityManager em,in RtsSelectionCommandIntentRequestElement request)
        {
            try
            {
                if(request.Kind!=RtsSelectionCommandIntentKind.BoardTransport||request.HasScreenPosition==0||request.RequestId<=0||!math.all(math.isfinite(request.ScreenPosition)))return;
                using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));if(sessions.CalculateEntityCount()!=1)return;
                var session=sessions.GetSingletonEntity();if(!TryCandidate(em,session,out var candidate))return;
                var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                var row=em.HasComponent<SkirmishPublicBoardingPresentation>(session)?em.GetComponentObject<SkirmishPublicBoardingPresentation>(session):new SkirmishPublicBoardingPresentation();
                row.Session=session;row.Carrier=candidate.Carrier;row.CarrierId=new FixedString64Bytes(candidate.CarrierId);row.CarrierGroupId=candidate.CarrierGroupId;
                row.Attempt=attempt.SessionId;row.SetupHash=attempt.SetupHash;row.World=em.WorldUnmanaged.SequenceNumber;row.GroupId=candidate.GroupId;
                row.CarrierRole=em.GetComponentData<SkirmishUnitRoleComponent>(candidate.Carrier).Role;row.CarrierPrefab=em.GetComponentData<UnitSourcePrefabKey>(candidate.Carrier).Value;
                row.RequestId=request.RequestId;row.Frame=request.Frame;row.Screen=request.ScreenPosition;row.Actors=candidate.Actors;row.Ids=candidate.Ids;row.Accepted=false;row.Presented=false;
                if(!em.HasComponent<SkirmishPublicBoardingPresentation>(session))em.AddComponentObject(session,row);
            }
            catch(Exception){ }
        }
        public static void ObserveResult(EntityManager em,in RtsSelectionCommandResultElement result,bool presented=false)
        {
            try
            {
                using var query=em.CreateEntityQuery(typeof(SkirmishPublicBoardingPresentation));if(query.CalculateEntityCount()!=1)return;
                var session=query.GetSingletonEntity();var row=em.GetComponentObject<SkirmishPublicBoardingPresentation>(session);
                if(result.Kind!=RtsSelectionCommandIntentKind.BoardTransport||result.RequestId!=row.RequestId||result.Frame!=row.Frame)return;
                if(presented){row.Presented=result.Accepted!=0&&row.Accepted;return;}
                row.Accepted=result.Accepted!=0&&TryReadProgress(em,session,row,out _,out _);
            }
            catch(Exception){ }
        }
        // Fresh recovery has no UI receipt. This reconstructs only an actually
        // public mounted roster, never a historical Board acceptance claim.
        public static bool TryReadExistingMounted(EntityManager em,Entity session,out SkirmishPublicBoardingPresentation row)
        {
            row=null;
            if(!SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out var goal,out _)||goal.Perspective!=SkirmishPublicGoalPerspective.Evacuate||!goal.Score.ExitOpen||!em.HasBuffer<SkirmishArmyGroupRecord>(session))return false;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            using var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            using var actors=query.ToEntityArray(Allocator.Temp);
            for(int pass=0;pass<2;pass++)foreach(var group in em.GetBuffer<SkirmishArmyGroupRecord>(session,true))
            {
                if(!SkirmishPublicOriginalTransportProjection.TryRead(em,session,goal,group.GroupId,out var publicMount))continue;
                Entity carrier=Entity.Null;var originals=new Entity[publicMount.CarrierOriginalIds.Length];
                foreach(var actor in actors)
                {
                    var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);if(own.SessionId!=attempt.SessionId)continue;
                    if(own.StableObjectId.ToString()==publicMount.CarrierPublicId){if(carrier!=Entity.Null)return false;carrier=actor;}
                    for(int i=0;i<originals.Length;i++)if(own.StableObjectId.ToString()==publicMount.CarrierOriginalIds[i]){if(originals[i]!=Entity.Null)return false;originals[i]=actor;}
                }
                if(carrier==Entity.Null||!em.HasComponent<SkirmishUnitRoleComponent>(carrier)||!em.HasComponent<UnitSourcePrefabKey>(carrier))return false;
                bool selected=em.HasComponent<SelectedUnitTag>(carrier);
                foreach(var original in originals)selected|=em.Exists(original)&&em.HasComponent<SelectedUnitTag>(original);
                if(pass==0&&!selected||pass==1&&selected)continue;
                var candidate=new SkirmishPublicBoardingPresentation{Session=session,Carrier=carrier,CarrierId=new FixedString64Bytes(publicMount.CarrierPublicId),
                    CarrierGroupId=publicMount.CarrierGroupId,GroupId=group.GroupId,Attempt=attempt.SessionId,SetupHash=attempt.SetupHash,World=em.WorldUnmanaged.SequenceNumber,
                    CarrierRole=em.GetComponentData<SkirmishUnitRoleComponent>(carrier).Role,CarrierPrefab=em.GetComponentData<UnitSourcePrefabKey>(carrier).Value,
                    Actors=originals,Ids=publicMount.CarrierOriginalIds};
                if(!TryReadProgress(em,session,candidate,out int pending,out int mounted)||pending!=0||mounted!=originals.Length)continue;
                row=candidate;return true;
            }
            return false;
        }
        public static bool TryReadProgress(EntityManager em,Entity session,SkirmishPublicBoardingPresentation row,out int pending,out int mounted)
        {
            pending=0;mounted=0;
            if(row==null||row.Session!=session||!em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session)||row.World!=em.WorldUnmanaged.SequenceNumber||
                row.Actors==null||row.Ids==null||row.Actors.Length==0||row.Ids.Length!=row.Actors.Length)return false;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(!SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out var goal,out _)||goal.Perspective!=SkirmishPublicGoalPerspective.Evacuate||!goal.Score.ExitOpen)return false;
            if(attempt.SessionId!=row.Attempt||attempt.SetupHash!=row.SetupHash||!Own(em,row.Carrier,row.Attempt)||
                em.GetComponentData<SkirmishAttemptOwnedComponent>(row.Carrier).StableObjectId!=row.CarrierId||
                !em.HasComponent<SkirmishUnitRoleComponent>(row.Carrier)||em.GetComponentData<SkirmishUnitRoleComponent>(row.Carrier).Role!=row.CarrierRole||
                !em.HasComponent<UnitSourcePrefabKey>(row.Carrier)||em.GetComponentData<UnitSourcePrefabKey>(row.Carrier).Value!=row.CarrierPrefab||
                em.GetComponentData<SkirmishArmyGroupMembershipComponent>(row.Carrier).GroupId!=row.CarrierGroupId)return false;
            var seen=new HashSet<Entity>();
            for(int i=0;i<row.Actors.Length;i++)
            {
                var actor=row.Actors[i];if(!seen.Add(actor)||!Own(em,actor,row.Attempt,true)||
                    em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).StableObjectId.ToString()!=row.Ids[i]||
                    !em.HasComponent<SkirmishUnitRoleComponent>(actor)||em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role!=SkirmishRoleKind.Rifle||
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Category!=SkirmishPopulationCategory.Infantry||
                    em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId!=row.GroupId)return false;
                bool original=false;
                for(int n=0;n<goal.NodeCount;n++)
                {var node=goal.Node(n);if(node.PublicId==row.Ids[i]&&node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject&&node.RevealedSubjectKind==SkirmishPublicSubjectKind.DesignatedRifle&&node.SubjectDisposition==SkirmishPublicSubjectDisposition.Live&&node.Mounted==em.HasComponent<UnitTransportPassenger>(actor))original=true;}
                if(!original||em.HasComponent<UnitTransportCargoPassenger>(actor))return false;
                Entity carrier=em.HasComponent<UnitTransportPassenger>(actor)?em.GetComponentData<UnitTransportPassenger>(actor).Transport:
                    em.HasComponent<UnitTransportCargoPassenger>(actor)?em.GetComponentData<UnitTransportCargoPassenger>(actor).Transport:Entity.Null;
                if(carrier==row.Carrier)
                {
                    if(!em.HasBuffer<UnitTransportPassengerElement>(carrier))return false;int count=0;
                    foreach(var passenger in em.GetBuffer<UnitTransportPassengerElement>(carrier,true))if(passenger.Passenger==actor)count++;
                    if(count!=1)return false;mounted++;
                }
                else if(carrier==Entity.Null&&!em.HasComponent<Parent>(actor)&&!em.HasComponent<Disabled>(actor)&&em.HasComponent<UnitTransportBoardingTarget>(actor)&&em.GetComponentData<UnitTransportBoardingTarget>(actor).Transport==row.Carrier)pending++;
                else return false;
            }
            return pending+mounted==row.Actors.Length;
        }
    }
}
