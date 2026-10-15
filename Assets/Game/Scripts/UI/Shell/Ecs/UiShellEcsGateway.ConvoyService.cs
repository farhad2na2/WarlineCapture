using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        // Public own infrastructure, own roster and typed service geometry only.
        // A readable static service point is not an affordability/repair quote.
        static void ReadPublicConvoyService(EntityManager em,Entity session,UiSkirmishPublicObjectiveModel model,List<UiSkirmishPublicGoalPosition> positions)
        {
            if(model.Goal==null||model.Goal.Perspective!=SkirmishPublicGoalPerspective.Escort||
                !em.HasComponent<SkirmishLaunchDefinitionRecord>(session)||!em.HasComponent<SkirmishResolvedSetupRecord>(session))return;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var launch=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);var resolved=em.GetComponentObject<SkirmishResolvedSetupRecord>(session);
            if(launch==null||resolved==null||!launch.Matches(resolved.Setup,native)||launch.Definition?.MapLayout==null||
                !launch.Definition.MapLayout.TryGetPad(1,SkirmishLegalPadKind.Service,out var pad)||
                !launch.Definition.MapLayout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z)||
                !float.IsFinite(x)||!float.IsFinite(z)||pad.WidthMetres<=0||pad.DepthMetres<=0)return;
            using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
            if(surfaces.CalculateEntityCount()!=1)return;
            var surface=surfaces.GetSingleton<MapSurfaceComponent>();
            if(surface.CellSize<=0||surface.HasSurfaceData==0||!surface.SurfaceBlob.IsCreated)return;
            Entity facility=Entity.Null;string facilityId=null;double nearest=double.PositiveInfinity;
            var facilityIds=new HashSet<string>(StringComparer.Ordinal);
            using(var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishStructureIdentityComponent),typeof(UnitHealth),typeof(Faction)))
            using(var entities=query.ToEntityArray(Allocator.Temp))foreach(var candidate in entities)
            {
                var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(candidate);
                if(owner.SessionId!=native.SessionId||owner.FactionId!=1||owner.IsStructure==0||owner.CapacityReleased!=0||owner.StableObjectId.IsEmpty||
                    em.GetComponentData<Faction>(candidate).Id!=1||em.GetComponentData<UnitHealth>(candidate).Current<=0||
                    em.HasComponent<UnitDeathAnimationComponent>(candidate)||em.HasComponent<Parent>(candidate)||
                    !em.HasComponent<SkirmishSharedActorTag>(candidate)||!em.HasComponent<LocalTransform>(candidate)||
                    em.GetComponentData<SkirmishStructureIdentityComponent>(candidate).StructureId.ToString()!=SkirmishStructureIds.GroundStaging)continue;
                string id=owner.StableObjectId.ToString();if(!facilityIds.Add(id))return;
                var actual=em.GetComponentData<LocalTransform>(candidate).Position;if(!math.all(math.isfinite(actual)))continue;
                double dx=(double)actual.x-x,dz=(double)actual.z-z,distance=dx*dx+dz*dz;
                if(distance>nearest||distance==nearest&&StringComparer.Ordinal.Compare(id,facilityId)>=0)continue;
                nearest=distance;facility=candidate;facilityId=id;
            }
            if(facility==Entity.Null)return;
            int materials=-1;
            using(var query=em.CreateEntityQuery(typeof(FactionEconomy),typeof(FactionTacticalMaterialsComponent)))
            using(var banks=query.ToEntityArray(Allocator.Temp))foreach(var bank in banks)
            {var own=em.GetComponentData<FactionTacticalMaterialsComponent>(bank);if(own.FactionId!=1||em.GetComponentData<FactionEconomy>(bank).FactionId!=1)continue;if(materials>=0)return;materials=own.Current;}
            if(materials<0)return;
            var destinations=new List<UiPublicServiceDestination>();
            foreach(var offset in new[]{new float2(0,0),new float2(4,0),new float2(-4,0)})
            {
                float2 point=new float2(x,z)+offset;int2 cell=(int2)math.floor((point-surface.GridOrigin.xz)/surface.CellSize);
                bool sampled=MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value,cell,out var sample);
                destinations.Add(new UiPublicServiceDestination{World=new Vector3(point.x,sampled?sample.Height:0,point.y),GroundAvailable=sampled});
            }
            if(!destinations[0].GroundAvailable)return;
            var trucks=new List<UiPublicConvoyRepairTruck>();var originalIds=new HashSet<string>(StringComparer.Ordinal);
            using(var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishArmyGroupMembershipComponent),typeof(UnitHealth),typeof(Faction)))
            using(var actors=query.ToEntityArray(Allocator.Temp))foreach(var actor in actors)
            {
                var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(owner.SessionId!=native.SessionId||owner.FactionId!=1||owner.IsStructure!=0||owner.CapacityReleased!=0||em.GetComponentData<Faction>(actor).Id!=1||
                    em.HasComponent<UnitDeathAnimationComponent>(actor)||em.HasComponent<Parent>(actor)||em.HasComponent<UnitAirMovement>(actor)||
                    em.HasComponent<UnitTransportPassenger>(actor)||em.HasComponent<UnitTransportCargoPassenger>(actor)||
                    !em.HasComponent<LocalTransform>(actor)||!em.HasComponent<SkirmishSharedActorTag>(actor))continue;
                string id=owner.StableObjectId.ToString();var health=em.GetComponentData<UnitHealth>(actor);if(health.Current<=0)continue;
                bool original=false;
                for(int i=0;i<model.Goal.NodeCount;i++)
                {var node=model.Goal.Node(i);if(node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject&&node.PublicId==id&&
                    node.RevealedSubjectKind==SkirmishPublicSubjectKind.ObjectiveConvoyTruck&&node.SubjectDisposition==SkirmishPublicSubjectDisposition.Live&&
                    node.Provenance==SkirmishPublicGoalProvenance.OwnRoster&&node.HealthKnown&&node.Health==health.Current&&node.MaximumHealth==health.Max)original=true;}
                if(!original)continue;if(!originalIds.Add(id))return;
                uint group=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId;
                var roster=Array.Find(model.OwnGroups,row=>row.Policy.GroupId==group);
                if(roster==null||roster.AliveCount!=1||roster.Policy.DesignatedSubjects.Length!=1||roster.Policy.DesignatedSubjects[0]!=id||
                    !VehicleRepairRules.TryQuote(health.Current,health.Max,out var quote))continue;
                trucks.Add(new UiPublicConvoyRepairTruck{OriginalPublicId=id,GroupId=group,Health=health.Current,MaximumHealth=health.Max,
                    Materials=quote.Materials,DurationTicks=quote.DurationTicks,Affordable=materials>=quote.Materials});
            }
            trucks.Sort((a,b)=>StringComparer.Ordinal.Compare(a.OriginalPublicId,b.OriginalPublicId));
            model.Service=new UiPublicConvoyService{PublicId="service/"+pad.PadId,FacilityPublicId=facilityId,Available=true,
                World=destinations[0].World,Materials=materials,EligibilityRadiusMetres=12,Destinations=destinations.ToArray(),Trucks=trucks.ToArray()};
            positions.Add(new UiSkirmishPublicGoalPosition{PublicId=model.Service.PublicId,World=model.Service.World,GroundAvailable=true});
        }
    }
}
