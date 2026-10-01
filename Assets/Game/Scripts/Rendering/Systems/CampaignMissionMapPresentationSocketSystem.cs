using System.Collections.Generic;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Rendering
{
    /// <summary>Temporarily retires explicitly declared scenery sockets while their mission owns the presentation.</summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct CampaignMissionMapPresentationSocketSystem : ISystem
    {
        private EntityQuery sockets,missions,hidden;
        public void OnCreate(ref SystemState state)
        {
            sockets=state.GetEntityQuery(typeof(OperationMapMissionPresentationSocket));
            missions=state.GetEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));
            hidden=state.GetEntityQuery(typeof(OperationMapMissionPresentationHiddenTag));
            steelVegetation=new NativeList<Entity>(Allocator.Persistent);
        }
        public void OnDestroy(ref SystemState state){if(steelVegetation.IsCreated)steelVegetation.Dispose();}
        public void OnUpdate(ref SystemState state)
        {
            var em=state.EntityManager;FixedString64Bytes active=default;
            if(missions.CalculateEntityCount()==1){var mission=missions.GetSingleton<CampaignMissionRuntimeComponent>();if(mission.Phase!=MissionPhaseKind.None)active=mission.MissionId;}
            using var roots=sockets.ToEntityArray(Allocator.Temp);
            var wanted=new HashSet<Entity>();
            foreach(var root in roots)if(em.GetComponentData<OperationMapMissionPresentationSocket>(root).MissionId.Equals(active))Collect(em,root,wanted);
            CollectSteelReserveSocket(em,active,wanted);
            using var retired=hidden.ToEntityArray(Allocator.Temp);
            foreach(var renderer in retired)if(!wanted.Contains(renderer)){if(em.HasComponent<DisableRendering>(renderer))em.RemoveComponent<DisableRendering>(renderer);em.RemoveComponent<OperationMapMissionPresentationHiddenTag>(renderer);}
            int added=0;
            foreach(var renderer in wanted)
                if(em.HasComponent<MaterialMeshInfo>(renderer)&&!em.HasComponent<DisableRendering>(renderer))
                {em.AddComponent<DisableRendering>(renderer);em.AddComponent<OperationMapMissionPresentationHiddenTag>(renderer);added++;}
            if(added>0)Debug.Log($"[MissionMapSocket] mission={active} hiddenRenderers={added} sourceGeometry=Preserved");
        }
        private static void Collect(EntityManager em,Entity entity,HashSet<Entity> result)
        {
            if(!em.Exists(entity)||!result.Add(entity)||!em.HasBuffer<Child>(entity))return;
            var children=em.GetBuffer<Child>(entity);for(int i=0;i<children.Length;i++)Collect(em,children[i].Value,result);
        }
    }
}
