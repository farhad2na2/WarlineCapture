using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiSkirmishPublicObjectiveGateway
    {
        bool IUiSkirmishPublicObjectiveGateway.TryReadSkirmishPublicObjective(out UiSkirmishPublicObjectiveModel model)
        {
            model=null;
            if(!TrySkirmish(out var em,out var session,out _)||!em.HasComponent<SkirmishExpandedSessionComponent>(session))return false;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(attempt.IsLegacy!=0||attempt.ObjectiveKind==SkirmishObjectiveKind.BaseAssault)return false;
            bool fa=GameLocalization.IsRightToLeft;
            model=new UiSkirmishPublicObjectiveModel { Applicable=true, AttemptId=attempt.SessionId.ToString(),
                UnavailableText=fa?"اطلاعات هدف هنوز آماده نیست.":"Objective information is not ready yet." };
            if(!SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out var goal,out _)||
                !SkirmishMissionLogicalBindingProvider.TryRead(em,session,out var logical)||logical.Areas==null)return true;
            model.Goal=goal;model.PublicAreas=logical.Areas;
            using(var gameplay=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
                model.Paused=gameplay.CalculateEntityCount()==1&&gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive==0;
            if(!em.HasComponent<SkirmishObjectiveClockComponent>(session))return true;
            model.SimulationSeconds=em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds;
            var positions=new List<UiSkirmishPublicGoalPosition>();
            using(var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent)))
            {
                bool hasSurface=surfaces.CalculateEntityCount()==1;
                var surface=hasSurface?surfaces.GetSingleton<MapSurfaceComponent>():default;
                hasSurface&=surface.CellSize>0;model.GroundCellSize=hasSurface?surface.CellSize:0;
                for(int i=0;i<goal.NodeCount;i++)
                {
                    var node=goal.Node(i);if(!node.PositionKnown)continue;
                    var sample=default(MapSurfaceSample);
                    int2 cell=hasSurface?(int2)math.floor((new float2(node.WorldX,node.WorldZ)-surface.GridOrigin.xz)/surface.CellSize):default;
                    bool sampled=hasSurface&&surface.HasSurfaceData!=0&&surface.SurfaceBlob.IsCreated&&
                        MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value,cell,out sample);
                    positions.Add(new UiSkirmishPublicGoalPosition { PublicId=node.PublicId,GroundAvailable=sampled,
                        World=new Vector3(node.WorldX,sampled?sample.Height:0,node.WorldZ) });
                }
                var perimeters=new List<UiSkirmishPublicPerimeter>();
                foreach(SkirmishObjectiveAreaRole role in Enum.GetValues(typeof(SkirmishObjectiveAreaRole)))
                {
                    if(!hasSurface||surface.HasSurfaceData==0||!surface.SurfaceBlob.IsCreated||role==SkirmishObjectiveAreaRole.None||!logical.Areas.TryRead(role,out var area))continue;
                    var perimeter=area.Shape==SkirmishObjectiveAreaShape.AxisAlignedRectangle?
                        new[]{area.Points[0],new SkirmishObjectiveAreaPoint(area.Points[1].WorldX,area.Points[0].WorldZ),area.Points[1],new SkirmishObjectiveAreaPoint(area.Points[0].WorldX,area.Points[1].WorldZ)}:area.Points;
                    var points=new List<Vector3>();
                    foreach(var point in perimeter)
                    {
                        int2 cell=(int2)math.floor((new float2(point.WorldX,point.WorldZ)-surface.GridOrigin.xz)/surface.CellSize);
                        if(!hasSurface||surface.HasSurfaceData==0||!surface.SurfaceBlob.IsCreated||!MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value,cell,out var sampled))break;
                        points.Add(new Vector3(point.WorldX,sampled.Height,point.WorldZ));
                    }
                    if(points.Count==perimeter.Length)perimeters.Add(new UiSkirmishPublicPerimeter {PublicId=area.StableAreaId,Role=role,WorldPoints=points.ToArray()});
                }
                model.Perimeters=perimeters.ToArray();
            }
            model.Positions=positions.ToArray();
            if(!TryOwnObjectiveGroups(em,session,goal,out var groups))return true;
            model.OwnGroups=groups;model.Available=true;
            // Exact public accepted group/goal/contact feedback is installed by the
            // separate normal result projection. Never infer it from tap completion.
            model.CommandFeedbackReady=SkirmishPublicOrderFeedback.Version==1;
            if(em.HasComponent<SkirmishPublicOrderFeedbackComponent>(session))
            {
                var row=em.GetComponentData<SkirmishPublicOrderFeedbackComponent>(session);
                model.Order=new UiSkirmishPublicOrderReadout {Present=row.Presented!=0,Accepted=row.Accepted!=0,Revision=row.Revision,GroupId=row.GroupId,NavigationGroupId=row.NavigationGroupId,
                    Action=row.Kind==RtsSelectionCommandIntentKind.Move?AriaObjectiveAction.Move:row.Kind==RtsSelectionCommandIntentKind.HoldPosition?AriaObjectiveAction.Hold:row.Kind==RtsSelectionCommandIntentKind.DisembarkTransport?AriaObjectiveAction.Unload:AriaObjectiveAction.None,
                    Frame=row.Frame,RequestId=row.RequestId,Screen=row.ScreenPosition,World=row.WorldPosition,WorldKnown=row.HasWorldPosition!=0};
            }
            if(TryGetBoundary(out var uiEm,out var boundary)&&uiEm.HasComponent<AriaPlaySessionComponent>(boundary))
            {
                var contact=uiEm.GetComponentData<AriaPlaySessionComponent>(boundary);
                model.CompletedContactTargetId=contact.LastCompletedTargetId;model.CompletedContactFrame=contact.LastCompletedFrame;
                model.CompletedContactPosition=contact.LastCompletedPosition;
            }
            ReadPublicOriginalUnload(em,session,model);
            return true;
        }

        bool IUiSkirmishPublicObjectiveGateway.TryFocusSkirmishPublicObjective(string publicId)
        {
            if(!((IUiSkirmishPublicObjectiveGateway)this).TryReadSkirmishPublicObjective(out var model)||!model.Available||
                model.Goal.Outcome!=SkirmishOutcomeKind.None)return false;
            UiSkirmishPublicGoalPosition? target=null;
            foreach(var row in model.Positions)if(row.PublicId==publicId){if(target.HasValue)return false;target=row;}
            if(!target.HasValue||!target.Value.GroundAvailable||!TrySkirmish(out var em,out _,out _))return false;
            using var focus=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            if(focus.CalculateEntityCount()!=1)return false;
            // This is the ordinary visible focus button's command boundary. It
            // changes only camera focus; it never selects or orders an actor.
            em.SetComponentData(focus.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent { Requested=1,Smooth=1,SmoothTimeSeconds=.35f,World=target.Value.World });
            return true;
        }

        static bool TryOwnObjectiveGroups(EntityManager em,Entity session,SkirmishPublicGoalSnapshot goal,out UiSkirmishPublicGroupRow[] result)
        {
            result=Array.Empty<UiSkirmishPublicGroupRow>();
            if(!em.HasBuffer<SkirmishArmyGroupRecord>(session))return false;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),ComponentType.ReadOnly<SkirmishArmyGroupMembershipComponent>(),ComponentType.ReadOnly<UnitHealth>(),ComponentType.ReadOnly<Faction>()},Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState});
            using var actors=query.ToEntityArray(Allocator.Temp);
            var rows=new List<UiSkirmishPublicGroupRow>();var ids=new HashSet<uint>();
            using var grids=em.CreateEntityQuery(typeof(GridConfig));bool haveGrid=grids.CalculateEntityCount()==1;
            var grid=haveGrid?grids.GetSingleton<GridConfig>():default;
            foreach(var group in em.GetBuffer<SkirmishArmyGroupRecord>(session,true))
            {
                if(group.FactionId!=1||group.AliveCount<=0)continue;
                if(group.GroupId==0||!ids.Add(group.GroupId))return false;
                var points=new List<Vector2>();var originals=new List<string>();var destinations=new List<Vector2>();bool mounted=false,moveActive=true,holdActive=true;float health=0,maximum=0;
                foreach(var actor in actors)
                {
                    var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    if(own.SessionId!=attempt||own.FactionId!=1||own.IsStructure!=0||own.CapacityReleased!=0||em.GetComponentData<Faction>(actor).Id!=1||
                        em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId!=group.GroupId)continue;
                    var hp=em.GetComponentData<UnitHealth>(actor);if(hp.Current<=0)continue;
                    if(!TryPublicOwnPosition(em,actor,attempt,out var position))return false;
                    points.Add(new Vector2(position.x,position.z));health+=hp.Current;maximum+=hp.Max;
                    moveActive&=em.HasComponent<ManualMoveOrderTag>(actor);holdActive&=em.HasComponent<HoldPositionOrderTag>(actor);
                    if(haveGrid&&em.HasComponent<UnitTarget>(actor))
                    {
                        int2 target=em.HasComponent<UnitLongDistanceMove>(actor)?em.GetComponentData<UnitLongDistanceMove>(actor).FinalGoal:em.GetComponentData<UnitTarget>(actor).Cell;
                        destinations.Add(new Vector2(grid.Origin.x+(target.x+.5f)*grid.CellSize,grid.Origin.z+(target.y+.5f)*grid.CellSize));
                    }
                    mounted|=em.HasComponent<UnitTransportPassenger>(actor)||em.HasComponent<UnitTransportCargoPassenger>(actor);
                    for(int i=0;i<goal.NodeCount;i++)
                    {
                        var node=goal.Node(i);
                        if(node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject&&node.PublicId==own.StableObjectId.ToString()&&node.SubjectDisposition==SkirmishPublicSubjectDisposition.Live)
                            originals.Add(node.PublicId);
                    }
                }
                if(points.Count==0)continue;
                Vector2 center=Vector2.zero;foreach(var point in points)center+=point;center/=points.Count;
                bool ground=group.Domain==SkirmishPopulationCategory.Infantry||group.Domain==SkirmishPopulationCategory.Ground||
                    group.Domain==SkirmishPopulationCategory.LogisticsSupport||
                    (group.Domain==SkirmishPopulationCategory.ObjectiveSupport&&group.Role==SkirmishRoleKind.ConvoyLogic&&originals.Count>0);
                var policy=new SkirmishPublicPlayerGroup {GroupId=group.GroupId,Own=true,Living=true,
                    CombatInfantry=group.Domain==SkirmishPopulationCategory.Infantry&&SkirmishExpandedPresentedOrders.IsAssaultRole(group.Role),
                    Dismounted=!mounted,CanMove=ground&&!mounted,CanHold=ground&&!mounted,CanUnload=false,
                    PositionKnown=true,WorldX=center.x,WorldZ=center.y,DesignatedSubjects=originals.ToArray()};
                SkirmishPublicOriginalTransportGroup transport=null;
                bool unload=mounted&&SkirmishPublicOriginalTransportProjection.TryRead(em,session,goal,group.GroupId,out transport);
                Entity carrier=Entity.Null;
                if(unload)foreach(var candidate in actors)
                {var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(candidate);if(owner.SessionId==attempt&&owner.StableObjectId.ToString()==transport.CarrierPublicId){if(carrier!=Entity.Null){unload=false;break;}carrier=candidate;}}
                policy.CanUnload=unload&&carrier!=Entity.Null;
                rows.Add(new UiSkirmishPublicGroupRow {Policy=policy,UnloadCarrierGroupId=policy.CanUnload?transport.CarrierGroupId:0,
                    UnloadCarrier=policy.CanUnload?new UiEntityHandle(carrier.Index,carrier.Version):UiEntityHandle.Null,UnloadCarrierId=policy.CanUnload?transport.CarrierPublicId:null,Selected=group.Selected!=0,AliveCount=points.Count,
                    MoveActive=moveActive&&destinations.Count==points.Count,HoldActive=holdActive,CurrentMoveGoals=destinations.ToArray(),
                    Health01=maximum>0?Mathf.Clamp01(health/maximum):0,OwnMemberPositions=points.ToArray(),
                    Label=SkirmishSetupBriefing.RoleLabel(group.Role,GameLocalization.IsRightToLeft)+" ("+points.Count+")"});
            }
            result=rows.ToArray();return true;
        }
        static bool TryPublicOwnPosition(EntityManager em,Entity actor,FixedString64Bytes attempt,out float3 position)
        {
            position=default;var seen=new HashSet<Entity>();var matrix=float4x4.identity;var current=actor;
            while(current!=Entity.Null)
            {
                if(!em.Exists(current)||!seen.Add(current)||seen.Count>64||!em.HasComponent<LocalTransform>(current)||
                    !em.HasComponent<SkirmishAttemptOwnedComponent>(current)||!em.HasComponent<Faction>(current))return false;
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(current);
                if(own.SessionId!=attempt||own.FactionId!=1||own.CapacityReleased!=0||em.GetComponentData<Faction>(current).Id!=1)return false;
                var local=em.GetComponentData<LocalTransform>(current).ToMatrix();
                if(em.HasComponent<PostTransformMatrix>(current))local=math.mul(local,em.GetComponentData<PostTransformMatrix>(current).Value);
                matrix=math.mul(local,matrix);current=em.HasComponent<Parent>(current)?em.GetComponentData<Parent>(current).Value:Entity.Null;
            }
            position=matrix.c3.xyz;
            return math.all(math.isfinite(position));
        }
    }
}
