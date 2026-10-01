using System.Collections.Generic;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Rendering
{
    public partial struct CampaignMissionMapPresentationSocketSystem
    {
        private Entity steelReserve;
        private NativeList<Entity> steelVegetation;
        // Baked presentation role contract; Rendering does not depend on Authoring.
        private const byte RenderOnlyRole=3;

        private void CollectSteelReserveSocket(EntityManager em,FixedString64Bytes active,HashSet<Entity> wanted)
        {
            bool split=active.Equals(new FixedString64Bytes(CampaignMissionSequence.SplitFront));
            if(!split&&!active.Equals(new FixedString64Bytes(CampaignMissionSequence.SteelPush)))
            {steelReserve=Entity.Null;steelVegetation.Clear();return;}
            using var missionQuery=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));
            using var mapQuery=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
            if(missionQuery.CalculateEntityCount()!=1||mapQuery.CalculateEntityCount()!=1)return;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(missionQuery.GetSingletonEntity());
            if(runtime.Phase==MissionPhaseKind.ReturnReplay)
            {steelReserve=Entity.Null;steelVegetation.Clear();return;}
            var missionRoot=missionQuery.GetSingletonEntity();
            Entity reserveEntity=Entity.Null;
            if(split && em.HasComponent<CampaignMissionSplitFrontFuelState>(missionRoot))
                {var fuel=em.GetComponentData<CampaignMissionSplitFrontFuelState>(missionRoot);if(fuel.Initialized!=0)reserveEntity=fuel.FuelReserve;}
            else if(!split && em.HasComponent<CampaignMissionSteelPushState>(missionRoot))
                {var reserve=em.GetComponentData<CampaignMissionSteelPushState>(missionRoot);if(reserve.Initialized!=0)reserveEntity=reserve.FuelReserve;}
            if(!em.Exists(reserveEntity)||!em.HasComponent<RuntimeBuildingCombatInfo>(reserveEntity))
            {steelReserve=Entity.Null;steelVegetation.Clear();return;}
            var metadata=mapQuery.GetSingleton<OperationMapMetadataComponent>();
            if(!metadata.Blob.IsCreated)return;
            ref var map=ref metadata.Blob.Value;
            if(!map.OperationMapId.Equals(split?"opmap.ch04.split_front_refinery_review":"opmap.ch04.steel_push_refinery_approach")||
                !map.SourceOperationMapId.Equals("opmap.skirmish.refinerydistrict_prepared")||
                !map.SourceContentHash.Equals("2d4fd91a415a68f0299a4075e37730ecd7b746e94e5a2a1e2a6cd7baca687778"))return;
            if(steelReserve!=reserveEntity)
            {
                steelVegetation.Clear();
                var building=em.GetComponentData<RuntimeBuildingCombatInfo>(reserveEntity);
                float2 min=map.Grid.Origin.xz+(float2)building.OriginCell*map.Grid.CellSize;
                float2 max=min+(float2)building.FootprintCells*map.Grid.CellSize;
                using var vegetationQuery=em.CreateEntityQuery(typeof(DenseCityPresentationIdentity),typeof(LocalToWorld));
                using var roots=vegetationQuery.ToEntityArray(Allocator.Temp);
                if(roots.Length==0)return;
                foreach(var root in roots)
                {
                    var identity=em.GetComponentData<DenseCityPresentationIdentity>(root);
                    if(identity.Role!=RenderOnlyRole||
                        identity.Category!=(byte)DenseCityPresentationSemanticCategory.Vegetation)continue;
                    // Only the qualified cosmetic owner whose actual mesh intersects
                    // this attempt's normal placement is replaced by the depot socket.
                    if(!IntersectsSteelFootprint(em,root,min,max))continue;
                    steelVegetation.Add(root);
                    Debug.Log($"[SteelPushDepotSocket] vegetation={identity.StableId} footprint={building.OriginCell}/{building.FootprintCells} owner=render-only sourceGeometry=Preserved");
                }
                // Bounds may be projected a frame after entity-scene activation.
                // Retry that initial collection rather than caching an empty yard.
                if(steelVegetation.Length>0)steelReserve=reserveEntity;
            }
            foreach(var root in steelVegetation)Collect(em,root,wanted);
        }

        private static bool IntersectsSteelFootprint(EntityManager em,Entity entity,float2 min,float2 max)
        {
            if(!em.Exists(entity))return false;
            if(em.HasComponent<MaterialMeshInfo>(entity)&&em.HasComponent<WorldRenderBounds>(entity))
            {
                var bounds=em.GetComponentData<WorldRenderBounds>(entity).Value;
                if(math.all((bounds.Center+bounds.Extents).xz>min)&&math.all((bounds.Center-bounds.Extents).xz<max))return true;
            }
            if(!em.HasBuffer<Child>(entity))return false;
            var children=em.GetBuffer<Child>(entity);
            for(int i=0;i<children.Length;i++)if(IntersectsSteelFootprint(em,children[i].Value,min,max))return true;
            return false;
        }
    }
}
