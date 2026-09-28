using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    public static class SupportAirValidationUtilitySystemHelper
    {
        public static SupportRejectionReason ValidateStrikeTarget(EntityManager em,in SupportSessionComponent session,in SupportRequestElement request)
        {
            var target=request.Target;
            if(request.TargetKind!=SupportTargetKind.Entity)return SupportRejectionReason.InvalidTargetType;
            if(target==Entity.Null || !em.Exists(target) || !em.HasComponent<UnitHealth>(target) ||
               em.GetComponentData<UnitHealth>(target).Current<=0 || !em.HasComponent<LocalTransform>(target) || em.HasComponent<Disabled>(target))return SupportRejectionReason.TargetGone;
            if(!em.HasComponent<SupportTargetEligibilityComponent>(target))return SupportRejectionReason.NotVisible;
            var knowledge=em.GetComponentData<SupportTargetEligibilityComponent>(target);
            if(!knowledge.SessionToken.Equals(session.SessionToken) || knowledge.AttemptOrdinal!=session.AttemptOrdinal || knowledge.CurrentlyVisible==0)return SupportRejectionReason.NotVisible;
            if(knowledge.Protected!=0)return SupportRejectionReason.ProtectedTarget;
            if(knowledge.HostileConfirmed==0 || !em.HasComponent<Faction>(target) || em.GetComponentData<Faction>(target).Id==session.FactionId || em.GetComponentData<Faction>(target).Id==FactionIdentity.NeutralFactionId)return SupportRejectionReason.NotConfirmed;
            bool groundVehicle=em.HasComponent<UnitMovementBehavior>(target)&&em.GetComponentData<UnitMovementBehavior>(target).UsesVehicleMotion!=0;
            bool structure=em.HasComponent<RuntimeBuildingCombatInfo>(target);
            if((!groundVehicle&&!structure) || knowledge.IsMilitaryTarget==0 || em.HasComponent<UnitAirMovement>(target) || em.HasComponent<UnitAirComponent>(target) ||
               em.HasComponent<UnitTransportParachuteDropComponent>(target) || em.HasComponent<UnitTransportCargoDropComponent>(target))return SupportRejectionReason.InvalidTargetType;
            if(request.TargetKnowledgeVersion==0 || knowledge.SourceVersion!=request.TargetKnowledgeVersion)return SupportRejectionReason.StalePreview;
            return SupportRejectionReason.None;
        }
        public static Entity ResolveAircraftPrefab(EntityManager em,FixedString64Bytes key)
        {
            using var query=em.CreateEntityQuery(ComponentType.ReadOnly<UnitPrefabRegistryTag>(),ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            if(query.CalculateEntityCount()!=1)return Entity.Null;
            foreach(var entry in em.GetBuffer<UnitPrefabRegistryEntry>(query.GetSingletonEntity(),true))
                if(em.Exists(entry.Prefab) && em.HasComponent<UnitSourcePrefabKey>(entry.Prefab) &&
                   em.GetComponentData<UnitSourcePrefabKey>(entry.Prefab).Value.Equals(key))
                {
                    var model=em.HasComponent<UnitDetailedVisualReference>(entry.Prefab)
                        ? em.GetComponentData<UnitDetailedVisualReference>(entry.Prefab).Root
                        : em.HasComponent<UnitModelPrefabReference>(entry.Prefab)
                            ? em.GetComponentData<UnitModelPrefabReference>(entry.Prefab).Prefab : Entity.Null;
                    if(model!=Entity.Null && em.Exists(model))return entry.Prefab;
                }
            return Entity.Null;
        }
        // Embedded models must be cloned with their owning linked hierarchy so Entity references remap.
        // Strip the actor root before it can enter any gameplay query; only presentation survives.
        public static Entity CreateFlightPresentation(EntityManager em,Entity prefab,float3 position,SupportAbilityKind kind)
        {
            // The transport is an effect, viewed above the close mission camera. Keep
            // the complete source hierarchy readable without changing route or cargo.
            float scale=kind==SupportAbilityKind.Paratroopers||kind==SupportAbilityKind.Supply?.25f:1f;
            if(!em.HasComponent<UnitDetailedVisualReference>(prefab))
            {
                var root=em.CreateEntity(typeof(LocalTransform),typeof(LocalToWorld));
                em.SetComponentData(root,LocalTransform.FromPositionRotationScale(position,quaternion.identity,scale));
                em.AddComponentData(root,em.GetComponentData<UnitModelPrefabReference>(prefab));
                if(em.HasComponent<UnitModelLocalTransform>(prefab))em.AddComponentData(root,em.GetComponentData<UnitModelLocalTransform>(prefab));
                if(em.HasComponent<UnitAttackImpactVfxReference>(prefab))em.AddComponentData(root,em.GetComponentData<UnitAttackImpactVfxReference>(prefab));
                em.AddBuffer<LinkedEntityGroup>(root).Add(new LinkedEntityGroup {Value=root});return root;
            }
            var flight=em.Instantiate(prefab);
            using(var types=em.GetComponentTypes(flight,Allocator.Temp))
                foreach(var type in types)
                    if(type!=ComponentType.ReadWrite<LocalTransform>() && type!=ComponentType.ReadWrite<LocalToWorld>() &&
                       type!=ComponentType.ReadWrite<Child>() && type!=ComponentType.ReadWrite<LinkedEntityGroup>() &&
                       type!=ComponentType.ReadWrite<UnitDetailedVisualReference>() && type!=ComponentType.ReadWrite<UnitModelLocalTransform>() &&
                       type!=ComponentType.ReadWrite<UnitTransportPlaneDoorReference>() && type!=ComponentType.ReadWrite<UnitDestroyedVisualReference>() && type!=ComponentType.ReadWrite<UnitAttackImpactVfxReference>())
                        em.RemoveComponent(flight,type);
            if(!em.HasComponent<LocalTransform>(flight))em.AddComponent<LocalTransform>(flight);
            em.SetComponentData(flight,LocalTransform.FromPositionRotationScale(position,quaternion.identity,scale));
            if(!em.HasComponent<LocalToWorld>(flight))em.AddComponent<LocalToWorld>(flight);
            return flight;
        }
        public static bool SafeRoute(EntityManager em,Entity root,in SupportSessionComponent session,out SupportAirRouteComponent route,bool payloadTarget=false,float3 payloadPosition=default)
        {
            route=default;
            if(!em.HasComponent<SupportAirRouteComponent>(root))return false;
            route=em.GetComponentData<SupportAirRouteComponent>(root);
            if(payloadTarget)route.Release=new float3(payloadPosition.x,math.max(route.Release.y,payloadPosition.y+12),payloadPosition.z);
            if(route.Authored==0 || route.Version==0 || !route.SessionToken.Equals(session.SessionToken) || route.AttemptOrdinal!=session.AttemptOrdinal ||
               !math.all(math.isfinite(route.Entry)) || !math.all(math.isfinite(route.Release)) || !math.all(math.isfinite(route.Exit)) ||
               !math.isfinite(route.Clearance) || route.Clearance<0 || route.Release.y<12 ||
               math.distancesq(route.Entry,route.Release)<1 || math.distancesq(route.Release,route.Exit)<1)return false;
            using var query=em.CreateEntityQuery(ComponentType.ReadOnly<AirMissileLauncherComponent>(),ComponentType.ReadOnly<SupportTargetEligibilityComponent>(),
                ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitHealth>(),ComponentType.ReadOnly<LocalTransform>());
            using var chunks=query.ToArchetypeChunkArray(Allocator.Temp);
            var et=em.GetEntityTypeHandle();
            foreach(var chunk in chunks)
                foreach(var entity in chunk.GetNativeArray(et))
                {
                    var knowledge=em.GetComponentData<SupportTargetEligibilityComponent>(entity);
                    if(knowledge.CurrentlyVisible==0 || knowledge.HostileConfirmed==0 || !knowledge.SessionToken.Equals(session.SessionToken) ||
                       knowledge.AttemptOrdinal!=session.AttemptOrdinal || em.GetComponentData<Faction>(entity).Id==session.FactionId || em.GetComponentData<UnitHealth>(entity).Current<=0)continue;
                    var launcher=em.GetComponentData<AirMissileLauncherComponent>(entity);
                    float range=launcher.BaseDetectionRange;
                    if(em.HasComponent<AirMissileLauncherStateComponent>(entity))range=math.max(range,em.GetComponentData<AirMissileLauncherStateComponent>(entity).EffectiveRange);
                    if(em.HasComponent<AirDefenseSupportLinkComponent>(entity))range=math.max(range,math.min(launcher.MaxDetectionRange,launcher.BaseDetectionRange+em.GetComponentData<AirDefenseSupportLinkComponent>(entity).RangeBonus));
                    if(!math.isfinite(range) || range<0)return false;
                    float2 position=em.GetComponentData<LocalTransform>(entity).Position.xz;float radius=range+route.Clearance;
                    if(SegmentDistanceSquared(position,route.Entry.xz,route.Release.xz)<=radius*radius ||
                       SegmentDistanceSquared(position,route.Release.xz,route.Exit.xz)<=radius*radius)return false;
                }
            return true;
        }
        public static float SegmentDistanceSquared(float2 p,float2 a,float2 b)
        {var d=b-a;float t=math.saturate(math.dot(p-a,d)/math.max(math.lengthsq(d),.0001f));return math.distancesq(p,a+d*t);}
    }
}
