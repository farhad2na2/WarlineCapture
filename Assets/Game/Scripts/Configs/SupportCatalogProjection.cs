using Game.Components;
using Unity.Collections;
using Unity.Entities;
namespace Game.Configs
{
    public static class SupportCatalogProjection
    {
        public static BlobAssetReference<SupportCatalogBlob> Create(SupportAbilityCatalogConfig config)
        {
            using var builder=new BlobBuilder(Allocator.Temp);
            ref var root=ref builder.ConstructRoot<SupportCatalogBlob>();
            var definitions=builder.Allocate(ref root.Abilities,config.Abilities.Length);
            for(int i=0;i<config.Abilities.Length;i++)
            {
                var a=config.Abilities[i];
                definitions[i]=new SupportAbilityDefinition { Kind=a.Kind,Id=a.Id,Charges=a.Charges,FuelCost=a.FuelCost,
                    Damage=a.Damage,Materials=a.Materials,CooldownSeconds=a.CooldownSeconds,Radius=a.Radius,DurationSeconds=a.DurationSeconds,
                    ApproachSeconds=a.ApproachSeconds,DirectDamagePermille=a.DirectDamagePermille,ProductionReady=(byte)(a.ProductionReady?1:0),
                    AircraftSourceKey=a.Kind==SupportAbilityKind.Smoke?default:new FixedString64Bytes(a.Kind==SupportAbilityKind.Supply?config.Abilities[2].SourcePrefab.name:a.SourcePrefab.name) };
            }
            return builder.CreateBlobAssetReference<SupportCatalogBlob>(Allocator.Persistent);
        }
        public static void Install(EntityManager em,Entity root,SupportAbilityCatalogConfig config,byte ownedMask,byte allowedMask)
        {
            if(em.HasComponent<SupportCatalogComponent>(root)) return;
            em.AddComponent<SupportMissionContextStampComponent>(root);em.AddComponent<SupportAirRouteComponent>(root);em.AddComponent<SupportPayloadBindingsComponent>(root);em.AddComponent<SupportSupplyReadModelComponent>(root);em.AddComponent<SupportCollectionFeedbackComponent>(root);em.AddBuffer<SupportCollectRequestElement>(root);
            em.AddComponentData(root,new SupportCatalogComponent { Blob=Create(config),Revision=config.Revision,OwnsBlob=1 });
            em.AddComponentData(root,new SupportSessionComponent { FactionId=1,NextRequestId=1,CatalogRevision=config.Revision });
            em.AddComponentData(root,new SupportMissionPolicyComponent { OwnedMask=ownedMask,AllowedMask=allowedMask });
            em.AddComponentData(root,new SupportSmokeVisualConfigComponent { Prefab=config.Abilities[0].SourcePrefab });
            em.AddComponent<SupportFuelAvailabilityComponent>(root);em.AddComponent<SupportInputStateComponent>(root);em.AddComponent<SupportPreviewComponent>(root);em.AddComponent<SupportProposalComponent>(root);
            em.AddBuffer<SupportAbilityStateElement>(root);em.AddBuffer<SupportRequestElement>(root);
            em.AddBuffer<SupportReceiptElement>(root);em.AddBuffer<SupportGroundRegionElement>(root);
        }
    }
}
