using Game.Components;
using Unity.Collections;
using Unity.Entities;
namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitTransportAirdropSystem))]
    [UpdateAfter(typeof(UnitMoveOrderRequestSystem))]
    public partial struct SupportSupplySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();var em=state.EntityManager;
            if(SystemAPI.TryGetSingletonEntity<SupportCollectRequestElement>(out var root)&&em.HasComponent<SupportSessionComponent>(root))
            {
                var requests=em.GetBuffer<SupportCollectRequestElement>(root);
                while(requests.Length>0)
                {
                    var request=requests[0];requests.RemoveAt(0);var session=em.GetComponentData<SupportSessionComponent>(root);
                    var reason=request.SessionToken.Equals(session.SessionToken)&&request.AttemptOrdinal==session.AttemptOrdinal?SupportSupplyAdapterSystemHelper.BeginCollection(em,root,request.Collector,request.Crate):SupportRejectionReason.WrongAttempt;
                    if(em.HasComponent<SupportCollectionFeedbackComponent>(root))em.SetComponentData(root,new SupportCollectionFeedbackComponent {Reason=reason});
                    if(em.HasComponent<SupportInputStateComponent>(root)){var input=em.GetComponentData<SupportInputStateComponent>(root);input.Phase=(byte)(reason==SupportRejectionReason.None?0:5);input.Version++;em.SetComponentData(root,input);em.SetComponentData(root,new SupportPreviewComponent {Reason=reason});}
                    requests=em.GetBuffer<SupportCollectRequestElement>(root);
                }
            }
            foreach(var reference in SystemAPI.Query<RefRW<SupportSupplyReadModelComponent>>())reference.ValueRW=default;
            var ecb=new EntityCommandBuffer(Allocator.Temp);
            foreach(var (reference,entity) in SystemAPI.Query<RefRW<SupportSupplyCrateComponent>>().WithOptions(EntityQueryOptions.IncludeDisabledEntities).WithEntityAccess())
            {
                var crate=reference.ValueRO;bool valid=em.Exists(crate.Root)&&em.HasComponent<SupportSessionComponent>(crate.Root);
                SupportSessionComponent session=default;if(valid){session=em.GetComponentData<SupportSessionComponent>(crate.Root);valid=crate.SessionToken.Equals(session.SessionToken)&&crate.AttemptOrdinal==session.AttemptOrdinal&&!session.SessionToken.IsEmpty;
                    if(em.HasComponent<CampaignMissionRuntimeComponent>(crate.Root))valid&=em.GetComponentData<CampaignMissionRuntimeComponent>(crate.Root).Outcome==Game.Missions.Contracts.MissionOutcomeKind.None;}
                if(!valid){if(em.Exists(crate.Canopy))ecb.DestroyEntity(crate.Canopy);if(em.Exists(crate.Claimant))ecb.RemoveComponent<SupportCollectOrderComponent>(crate.Claimant);ecb.DestroyEntity(entity);continue;}
                if(crate.Released==0)continue;
                if(!em.HasComponent<UnitTransportCargoDropComponent>(entity))crate.Landed=1;
                Entity storage=Entity.Null;bool ambiguous=false;
                foreach(var (materials,store) in SystemAPI.Query<RefRO<FactionTacticalMaterialsComponent>>().WithEntityAccess())if(materials.ValueRO.FactionId==crate.FactionId){if(storage!=Entity.Null)ambiguous=true;storage=store;}
                if(ambiguous)storage=Entity.Null;
                if(crate.Claimant!=Entity.Null)
                {
                    bool owned=SupportSupplyAdapterSystemHelper.EligibleCollector(em,crate.Claimant,crate)&&em.HasComponent<SupportCollectOrderComponent>(crate.Claimant)&&em.GetComponentData<SupportCollectOrderComponent>(crate.Claimant).Crate==entity;
                    if(owned){var order=em.GetComponentData<SupportCollectOrderComponent>(crate.Claimant);owned=session.SimulationSeconds<order.ExpiresAt||Unity.Mathematics.math.distancesq(em.GetComponentData<Unity.Transforms.LocalTransform>(crate.Claimant).Position.xz,crate.LandingPosition.xz)<=4;}
                    if(!owned){if(em.Exists(crate.Claimant)&&em.HasComponent<SupportCollectOrderComponent>(crate.Claimant)&&em.GetComponentData<SupportCollectOrderComponent>(crate.Claimant).Crate==entity)ecb.RemoveComponent<SupportCollectOrderComponent>(crate.Claimant);crate.Claimant=Entity.Null;}
                    else if(session.Active!=0&&crate.Landed!=0&&storage!=Entity.Null)
                    {
                        int accepted=SupportSupplyAdapterSystemHelper.Transfer(em,entity,ref crate,storage);
                        if(accepted>0){ecb.RemoveComponent<SupportCollectOrderComponent>(crate.Claimant);crate.Claimant=Entity.Null;}
                    }
                }
                reference.ValueRW=crate;
                if(crate.RemainingMaterials<=0){if(em.Exists(crate.Canopy))ecb.DestroyEntity(crate.Canopy);ecb.DestroyEntity(entity);continue;}
                if(em.HasComponent<SupportSupplyReadModelComponent>(crate.Root))em.SetComponentData(crate.Root,new SupportSupplyReadModelComponent {Crate=entity,Remaining=crate.RemainingMaterials,Ready=crate.Landed,Claimed=(byte)(crate.Claimant!=Entity.Null?1:0),Full=(byte)(storage!=Entity.Null&&em.GetComponentData<FactionTacticalMaterialsComponent>(storage).Current>=em.GetComponentData<FactionTacticalMaterialsComponent>(storage).Capacity?1:0)});
            }
            ecb.Playback(em);ecb.Dispose();
        }
    }
}
