using Game.Components;
using Game.Configs;
using Unity.Entities;
using UnityEngine;
namespace Game.Authoring
{
    public sealed class SupportAbilityCatalogAuthoring : MonoBehaviour
    {
        public SupportAbilityCatalogConfig Catalog;
        private sealed class SupportBaker : Baker<SupportAbilityCatalogAuthoring>
        {
            public override void Bake(SupportAbilityCatalogAuthoring authoring)
            {
                if(authoring.Catalog==null || !authoring.Catalog.TryValidate(out _)) return;
                var blob=SupportCatalogProjection.Create(authoring.Catalog);
                AddBlobAsset(ref blob,out _);
                AddComponent(GetEntity(TransformUsageFlags.None),new SupportCatalogComponent { Blob=blob,Revision=authoring.Catalog.Revision });
            }
        }
    }
}
