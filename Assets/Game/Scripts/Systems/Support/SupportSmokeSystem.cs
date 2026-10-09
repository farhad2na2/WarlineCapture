using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    public struct SupportSmokeVisualOwnerComponent : IComponentData { public Entity Zone; }
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitGridMovementSystem))]
    [UpdateAfter(typeof(UnitAirMovementSystem))]
    [UpdateBefore(typeof(UnitAttackSystem))]
    [UpdateBefore(typeof(BuildingDefenseAttackSystem))]
    public partial struct SupportSmokeSystem : ISystem
    {
        private NativeList<SupportSmokeZoneComponent> activeZones;
        private EntityQuery missingCover,sessionRoots;
        public void OnCreate(ref SystemState state)
        {
            sessionRoots=state.GetEntityQuery(typeof(SupportSessionComponent));
            activeZones=new NativeList<SupportSmokeZoneComponent>(8,Allocator.Persistent);
            missingCover=state.GetEntityQuery(new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<UnitHealth>(),ComponentType.ReadOnly<LocalTransform>()},None=new[]{ComponentType.ReadOnly<SupportRangedCoverComponent>()}});
        }
        public void OnDestroy(ref SystemState state){if(activeZones.IsCreated)activeZones.Dispose();}
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();var em=state.EntityManager;
            using var roots=sessionRoots.ToEntityArray(Allocator.Temp);
            activeZones.Clear();using var ecb=new EntityCommandBuffer(Allocator.Temp);
            foreach(var (zoneRef,entity) in SystemAPI.Query<RefRO<SupportSmokeZoneComponent>>().WithEntityAccess())
            {
                var zone=zoneRef.ValueRO;
                bool live=SupportSessionUtility.TrySmokeOwner(em,roots,zone,out var owner);
                if(live)
                {
                    var session=em.GetComponentData<SupportSessionComponent>(owner);
                    live=!SupportSessionUtility.IsTerminal(em,owner,session) && zone.ExpiresAt>session.SimulationSeconds;
                }
                if(live)activeZones.Add(zone);
                else ecb.DestroyEntity(entity);
            }
            // Add derived cover only when an effect exists, avoiding structural work in ordinary missions.
            if(activeZones.Length>0 && !missingCover.IsEmptyIgnoreFilter)em.AddComponent<SupportRangedCoverComponent>(missingCover);
            foreach(var cover in SystemAPI.Query<RefRW<SupportRangedCoverComponent>>())cover.ValueRW.DirectDamagePermille=1000;
            foreach(var (cover,transform) in SystemAPI.Query<RefRW<SupportRangedCoverComponent>,RefRO<LocalTransform>>()
                .WithAll<UnitGrid,UnitHealth>().WithNone<UnitAirMovement,UnitAirComponent,RuntimeBuildingCombatInfo>()
                .WithNone<UnitTransportParachuteDropComponent,UnitTransportCargoDropComponent,UnitTransportRopeDropComponent>())
            {
                ushort factor=1000;
                foreach(var zone in activeZones)if(math.distancesq(transform.ValueRO.Position.xz,zone.Center.xz)<=zone.RadiusSquared)factor=(ushort)math.min((int)factor,(int)zone.DirectDamagePermille);
                cover.ValueRW.DirectDamagePermille=factor;
            }
            foreach(var (owner,visual) in SystemAPI.Query<RefRO<SupportSmokeVisualOwnerComponent>>().WithEntityAccess())
                if(!em.Exists(owner.ValueRO.Zone))ecb.DestroyEntity(visual);
            ecb.Playback(em);
        }
    }
}
