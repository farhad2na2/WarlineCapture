using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Entities;
using UnityEngine;
namespace Game.Composition
{
    internal static partial class CampaignMissionCatalogProjection
    {
        private static void EnsureSupport(EntityManager em,Entity root, string missionId)
        {
            var bindings=Resources.Load<SupportRuntimeBindingsConfig>("Support/SupportRuntimeBindings");
            if(bindings==null || bindings.Catalog==null || bindings.Policies==null || !bindings.Catalog.TryValidate(out _)) return;
            byte owned=0;
            var profile=SaveService.CreateDefault().LoadProfile();
            foreach(var id in profile.ownedSupportAbilityUnlocks ?? System.Array.Empty<string>())
                for(int i=0;i<bindings.Catalog.Abilities.Length;i++) if(bindings.Catalog.Abilities[i].Id==id) owned|=(byte)(1<<((int)bindings.Catalog.Abilities[i].Kind-1));
            byte allowed=bindings.Policies.TryResolve(missionId,out var policy)?policy.AllowedMask:(byte)0;
            SupportCatalogProjection.Install(em,root,bindings.Catalog,owned,allowed);
            if(!em.HasBuffer<SupportMissionPolicyElement>(root))
            {
                var policies=em.AddBuffer<SupportMissionPolicyElement>(root);
                foreach(var entry in bindings.Policies.Missions)policies.Add(new SupportMissionPolicyElement
                {MissionPrefix="saga."+entry.MissionId.ToLowerInvariant().Replace("-", ".")+".",AllowedMask=entry.AllowedMask,LessonKind=entry.LessonKind,PopulationCeiling=entry.PopulationCeiling});
            }
            var current=em.GetComponentData<SupportMissionPolicyComponent>(root);
            current.AllowedMask=allowed;current.LessonKind=policy.LessonKind;current.OwnedMask=owned;current.PopulationCeiling=policy.PopulationCeiling;em.SetComponentData(root,current);
        }
    }
}
