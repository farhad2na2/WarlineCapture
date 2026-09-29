using Game.Components;
using Game.Configs;
using Unity.Entities;
namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(SupportAbilityStartupSystem))]
    public partial class CampaignSupportOwnershipProjectionSystem : SystemBase
    {
        private long instance=-1,version=-1;
        protected override void OnUpdate()
        {
            using var query=EntityManager.CreateEntityQuery(typeof(CampaignMissionProgressStoreReferenceComponent),typeof(SupportMissionPolicyComponent));
            if(query.CalculateEntityCount()!=1)return;
            var root=query.GetSingletonEntity();var store=EntityManager.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store;
            if(store==null||store.InstanceId==instance&&store.SourceVersion==version)return;
            byte owned=0;foreach(var id in store.ReadSupportUnlocks())for(int kind=1;kind<=4;kind++)
                if(id==SupportAbilityCatalogConfig.Id((SupportAbilityKind)kind))owned|=(byte)(1<<(kind-1));
            var policy=EntityManager.GetComponentData<SupportMissionPolicyComponent>(root);policy.OwnedMask=owned;EntityManager.SetComponentData(root,policy);
            instance=store.InstanceId;version=store.SourceVersion;
        }
    }
}
