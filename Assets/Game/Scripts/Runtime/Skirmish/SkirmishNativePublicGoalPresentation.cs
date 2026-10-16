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
    // Player HUD producer only. It never exports internal occupancy facts, enemy
    // original rosters, hidden health, enemy transforms, resources or commands.
    // The UI must render this snapshot before its ARIA presentation adapter uses it.
    public static class SkirmishNativePublicGoalPresentation
    {
        public static bool TryReadPlayer(EntityManager em,Entity session,
            out SkirmishPublicGoalSnapshot snapshot,out string error)
        {
            snapshot=null;error="Current player public objective presentation unavailable.";
            if(!em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session)||
                !em.HasComponent<SkirmishNativeObjectiveRecord>(session)||
                !em.HasComponent<SkirmishObjectiveStateComponent>(session)||
                !SkirmishMissionLogicalBindingProvider.TryRead(em,session,out var binding))return false;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(attempt.IsLegacy!=0||attempt.ObjectiveKind==SkirmishObjectiveKind.BaseAssault)return false;
            var owner=em.GetComponentObject<SkirmishNativeObjectiveRecord>(session);
            if(owner?.Areas==null||owner.Collector==null||binding.Areas==null||owner.Areas.LogicalFingerprint!=binding.Areas.LogicalFingerprint||
                owner.Areas.ObjectiveKind!=attempt.ObjectiveKind)return false;
            var nodes=new List<SkirmishPublicGoalNode>();var score=new SkirmishPublicGoalScore();
            long elapsed,deadline;SkirmishPublicGoalPerspective perspective;
            if(attempt.ObjectiveKind==SkirmishObjectiveKind.FrontlineControl)
            {
                var state=owner.Frontline;elapsed=state.ElapsedTicks;deadline=state.DeadlineTicks;
                perspective=SkirmishPublicGoalPerspective.Capture;
                score.TicketsKnown=true;score.OwnTickets=state.PlayerTickets;score.OpposingTickets=state.EnemyTickets;
                if(!Zone(owner.Areas,SkirmishObjectiveAreaRole.FrontlineA,state.ZoneA,elapsed,nodes)||
                    !Zone(owner.Areas,SkirmishObjectiveAreaRole.FrontlineB,state.ZoneB,elapsed,nodes)||
                    !Zone(owner.Areas,SkirmishObjectiveAreaRole.FrontlineC,state.ZoneC,elapsed,nodes))return false;
            }
            else if(attempt.ObjectiveKind==SkirmishObjectiveKind.Breakthrough)
            {
                var state=owner.Breakthrough;elapsed=state.ElapsedTicks;deadline=state.DeadlineTicks;
                perspective=SkirmishPublicGoalPerspective.Evacuate;
                score.ProgressKnown=true;score.OriginalSubjects=12;score.Required=8;score.ExitStateKnown=true;score.ExitOpen=state.ExitOpen;
                if(state.OriginalSubjects==null||state.OriginalSubjects.Length!=12||
                    !Marker(owner.Areas,SkirmishObjectiveAreaRole.BreakthroughCorridorA,SkirmishPublicGoalNodeKind.ExitCorridor,elapsed,out var a)||
                    !Marker(owner.Areas,SkirmishObjectiveAreaRole.BreakthroughCorridorB,SkirmishPublicGoalNodeKind.ExitCorridor,elapsed,out var b)||
                    !Marker(owner.Areas,SkirmishObjectiveAreaRole.BreakthroughExit,SkirmishPublicGoalNodeKind.EvacuationExit,elapsed,out var exit)||
                    a.PublicId!=state.CorridorA.StableCorridorId||b.PublicId!=state.CorridorB.StableCorridorId)return false;
                a.WorkProgressKnown=b.WorkProgressKnown=true;a.RequiredWorkTicks=b.RequiredWorkTicks=SkirmishBreakthroughReducer.CorridorHoldTicks;
                a.WorkTicks=state.CorridorA.HoldTicks;b.WorkTicks=state.CorridorB.HoldTicks;
                nodes.Add(a);nodes.Add(b);nodes.Add(exit);
                if(!OwnedOriginals(em,attempt.SessionId,out var actors))return false;
                var sealedIds=new HashSet<string>(owner.Collector.OriginalStableIds(),StringComparer.Ordinal);
                if(sealedIds.Count!=score.OriginalSubjects)return false;
                foreach(var subject in state.OriginalSubjects)
                {
                    if(!sealedIds.Remove(subject.StableOriginalId)||!Enum.IsDefined(typeof(SkirmishBreakthroughSubjectStatus),subject.Status))return false;
                    var disposition=subject.Status==SkirmishBreakthroughSubjectStatus.Alive?SkirmishPublicSubjectDisposition.Live:
                        subject.Status==SkirmishBreakthroughSubjectStatus.Dead?SkirmishPublicSubjectDisposition.Lost:SkirmishPublicSubjectDisposition.Evacuated;
                    if(disposition==SkirmishPublicSubjectDisposition.Lost)score.SubjectsLost++;
                    if(disposition==SkirmishPublicSubjectDisposition.Evacuated)score.Completed++;
                    if(!Subject(em,owner.Collector,actors,subject.StableOriginalId,SkirmishRoleKind.Rifle,SkirmishObjectiveRoleKind.BreakthroughDesignated,
                        SkirmishPublicSubjectKind.DesignatedRifle,disposition,elapsed,subject.ExitDwellTicks,SkirmishBreakthroughReducer.ExitDwellTicks,out var node))return false;
                    nodes.Add(node);
                }
            }
            else if(attempt.ObjectiveKind==SkirmishObjectiveKind.ConvoyEscort)
            {
                var state=owner.Convoy;elapsed=state.ElapsedTicks;deadline=state.DeadlineTicks;
                perspective=SkirmishPublicGoalPerspective.Escort;
                score.ProgressKnown=true;score.OriginalSubjects=3;score.Required=2;
                if(state.OriginalTrucks==null||state.OriginalTrucks.Length!=3||
                    !Marker(owner.Areas,SkirmishObjectiveAreaRole.ConvoyDestination,SkirmishPublicGoalNodeKind.DeliveryDestination,elapsed,out var destination)||
                    destination.PublicId!=state.StableDestinationId)return false;
                nodes.Add(destination);
                if(!OwnedOriginals(em,attempt.SessionId,out var actors))return false;
                var sealedIds=new HashSet<string>(owner.Collector.OriginalStableIds(),StringComparer.Ordinal);
                if(sealedIds.Count!=score.OriginalSubjects)return false;
                foreach(var truck in state.OriginalTrucks)
                {
                    if(!sealedIds.Remove(truck.StableOriginalId)||!Enum.IsDefined(typeof(SkirmishConvoySubjectStatus),truck.Status))return false;
                    var disposition=truck.Status==SkirmishConvoySubjectStatus.Alive?SkirmishPublicSubjectDisposition.Live:
                        truck.Status==SkirmishConvoySubjectStatus.Dead?SkirmishPublicSubjectDisposition.Lost:SkirmishPublicSubjectDisposition.Delivered;
                    if(disposition==SkirmishPublicSubjectDisposition.Lost)score.SubjectsLost++;
                    if(disposition==SkirmishPublicSubjectDisposition.Delivered)score.Completed++;
                    if(!Subject(em,owner.Collector,actors,truck.StableOriginalId,SkirmishRoleKind.ConvoyLogic,SkirmishObjectiveRoleKind.ConvoyTruck,
                        SkirmishPublicSubjectKind.ObjectiveConvoyTruck,disposition,elapsed,truck.DestinationDwellTicks,SkirmishConvoyReducer.DestinationDwellTicks,out var node))return false;
                    nodes.Add(node);
                }
            }
            else return false;
            var objective=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            if(objective.Kind!=attempt.ObjectiveKind)return false;
            var outcome=objective.Terminal!=0?objective.Outcome:SkirmishOutcomeKind.None;
            return SkirmishPublicGoalSnapshot.TryCreate(attempt.DefinitionId.ToString(),attempt.ContentVersion,1,attempt.ObjectiveKind,
                perspective,elapsed,Math.Max(0,deadline-elapsed),outcome,score,nodes.ToArray(),out snapshot,out error);
        }

        private static bool Zone(SkirmishCompiledObjectiveAreas areas,SkirmishObjectiveAreaRole role,
            SkirmishFrontlineZoneState state,long elapsed,List<SkirmishPublicGoalNode> nodes)
        {
            if(!Marker(areas,role,SkirmishPublicGoalNodeKind.CaptureZone,elapsed,out var node)||node.PublicId!=state.StableZoneId)return false;
            node.OwnerKnown=true;node.OwnerFaction=(int)state.Owner;node.Contested=state.Contested;
            node.CaptureProgressKnown=state.Phase!=SkirmishFrontlineCapturePhase.None;
            node.CapturePhase=state.Phase==SkirmishFrontlineCapturePhase.Neutralizing?SkirmishPublicCapturePhase.Neutralizing:
                state.Phase==SkirmishFrontlineCapturePhase.Capturing?SkirmishPublicCapturePhase.Capturing:
                state.Owner!=SkirmishFrontlineSide.Neutral?SkirmishPublicCapturePhase.Held:SkirmishPublicCapturePhase.None;
            if(node.CaptureProgressKnown){node.CapturingFaction=(int)state.CapturingSide;node.CaptureFraction=(float)((double)state.ProgressTicks/SkirmishFrontlineReducer.CaptureTicks);}
            nodes.Add(node);return true;
        }

        // Marker points are public focus locations, not a path/placement certificate.
        // The actual perimeter remains the source for rings and inside-goal checks.
        private static bool Marker(SkirmishCompiledObjectiveAreas areas,SkirmishObjectiveAreaRole role,
            SkirmishPublicGoalNodeKind kind,long elapsed,out SkirmishPublicGoalNode node)
        {
            node=default;if(!areas.TryRead(role,out var area)||area.Points==null||area.Points.Length<2)return false;
            double x=0,z=0;foreach(var point in area.Points){x+=point.WorldX;z+=point.WorldZ;}
            float centerX=(float)(x/area.Points.Length),centerZ=(float)(z/area.Points.Length);
            if(!areas.Contains(role,centerX,centerZ))
            {
                bool found=false;
                for(int i=0;i<area.Points.Length;i++)
                {
                    var a=area.Points[i];var b=area.Points[(i+1)%area.Points.Length];var c=area.Points[(i+2)%area.Points.Length];
                    if(Math.Abs(((double)b.WorldX-a.WorldX)*(c.WorldZ-a.WorldZ)-((double)b.WorldZ-a.WorldZ)*(c.WorldX-a.WorldX))<1e-8)continue;
                    float px=(float)(((double)a.WorldX+b.WorldX+c.WorldX)/3),pz=(float)(((double)a.WorldZ+b.WorldZ+c.WorldZ)/3);
                    if(!areas.Contains(role,px,pz))continue;centerX=px;centerZ=pz;found=true;break;
                }
                if(!found)return false;
            }
            node=new SkirmishPublicGoalNode{PublicId=area.StableAreaId,Kind=kind,Provenance=SkirmishPublicGoalProvenance.PublicObjective,
                PositionKnown=true,WorldX=centerX,WorldZ=centerZ,LastObservedTicks=elapsed};return true;
        }

        private static bool OwnedOriginals(EntityManager em,FixedString64Bytes attempt,out Dictionary<string,Entity> actors)
        {
            actors=new Dictionary<string,Entity>(StringComparer.Ordinal);
            using var query=em.CreateEntityQuery(new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),
                ComponentType.ReadOnly<SkirmishObjectiveRoleComponent>(),ComponentType.ReadOnly<Faction>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState});
            using var values=query.ToEntityArray(Allocator.Temp);
            foreach(var actor in values)
            {
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(owned.SessionId!=attempt||owned.FactionId!=1||owned.IsStructure!=0)continue;
                if(em.GetComponentData<Faction>(actor).Id!=1||!actors.TryAdd(owned.StableObjectId.ToString(),actor))return false;
            }
            return true;
        }

        private static bool Subject(EntityManager em,SkirmishNativeObjectiveCollector collector,Dictionary<string,Entity> actors,string id,SkirmishRoleKind role,
            SkirmishObjectiveRoleKind objectiveRole,SkirmishPublicSubjectKind publicRole,SkirmishPublicSubjectDisposition disposition,
            long elapsed,long dwell,long required,out SkirmishPublicGoalNode node)
        {
            node=new SkirmishPublicGoalNode{PublicId=id,Kind=SkirmishPublicGoalNodeKind.OwnDesignatedSubject,
                Provenance=SkirmishPublicGoalProvenance.OwnRoster,OwnerKnown=true,OwnerFaction=1,LastObservedTicks=elapsed,
                RevealedSubjectKind=publicRole,SubjectDisposition=disposition};
            if(!actors.TryGetValue(id,out var actor))return disposition!=SkirmishPublicSubjectDisposition.Live;
            if(!collector.IsOriginalEntity(actor)||!em.HasComponent<UnitHealth>(actor)||!em.HasComponent<SkirmishUnitRoleComponent>(actor)||
                !em.HasComponent<SkirmishSharedActorTag>(actor))return false;
            var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            var identity=em.GetComponentData<SkirmishObjectiveRoleComponent>(actor);var health=em.GetComponentData<UnitHealth>(actor);
            if(owned.ReservationId!=0||identity.Role!=objectiveRole||identity.FactionId!=1||identity.StableObjectId.ToString()!=id||
                em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role!=role||
                em.GetComponentData<SkirmishUnitRoleComponent>(actor).Category!=(role==SkirmishRoleKind.ConvoyLogic?SkirmishPopulationCategory.ObjectiveSupport:SkirmishRoleIds.Category(role)))return false;
            if(disposition!=SkirmishPublicSubjectDisposition.Live)
                return disposition==SkirmishPublicSubjectDisposition.Lost&&health.Current<=0;
            if(owned.CapacityReleased!=0||health.Current<=0||health.Max<=0||
                !OwnWorldPosition(em,actor,owned.SessionId,out var position))return false;
            node.PositionKnown=true;node.WorldX=position.x;node.WorldZ=position.z;
            // Own-roster knowledge, not a claim that the actor is inside the viewport.
            node.CurrentlyVisible=true;node.HealthKnown=true;node.Health=health.Current;node.MaximumHealth=health.Max;
            node.HealthProvenance=SkirmishPublicHealthProvenance.OwnRoster;node.HealthObservedTicks=elapsed;
            node.Mounted=em.HasComponent<UnitTransportPassenger>(actor)||em.HasComponent<UnitTransportCargoPassenger>(actor);
            node.WorkProgressKnown=true;node.WorkTicks=dwell;node.RequiredWorkTicks=required;return true;
        }

        private static bool OwnWorldPosition(EntityManager em,Entity actor,FixedString64Bytes attempt,out float3 position)
        {
            position=default;var seen=new HashSet<Entity>();var matrix=float4x4.identity;var current=actor;
            while(current!=Entity.Null)
            {
                if(!em.Exists(current)||!seen.Add(current)||seen.Count>64||!em.HasComponent<LocalTransform>(current))return false;
                if(em.HasComponent<Faction>(current)&&em.GetComponentData<Faction>(current).Id!=1)return false;
                if(em.HasComponent<SkirmishAttemptOwnedComponent>(current))
                {
                    var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(current);
                    if(owner.SessionId!=attempt||owner.FactionId!=1)return false;
                }
                var local=em.GetComponentData<LocalTransform>(current).ToMatrix();
                if(em.HasComponent<PostTransformMatrix>(current))local=math.mul(local,em.GetComponentData<PostTransformMatrix>(current).Value);
                matrix=math.mul(local,matrix);current=em.HasComponent<Parent>(current)?em.GetComponentData<Parent>(current).Value:Entity.Null;
            }
            position=matrix.c3.xyz;return math.all(math.isfinite(position));
        }
    }
}
