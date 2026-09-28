using Game.Components;
using Game.Configs;
using Unity.Entities;
using UnityEngine;
namespace Game.Authoring
{
    public sealed class SupportMissionContextAuthoring : MonoBehaviour
    {
        public SupportMissionContextConfig Config;
        [BakingVersion("WarlineCapture",1)]
        private sealed class ContextBaker : Baker<SupportMissionContextAuthoring>
        {
            public override void Bake(SupportMissionContextAuthoring authoring)
            {
                var c=authoring.Config;if(c==null)return;DependsOn(c);
                if(!c.TryValidate(out var error)){Debug.LogError("[SupportMissionContext] "+error,authoring);return;}
                var entity=GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new SupportMissionContextComponent {MissionId=c.MissionId,OperationMapId=c.OperationMapId,MissionSourceVersion=c.MissionSourceVersion,Revision=c.Revision,GroundMin=c.GroundMin,GroundMax=c.GroundMax,Entry=c.Entry,Release=c.Release,Exit=c.Exit,Clearance=c.Clearance,PopulationCeiling=c.PopulationCeiling,RouteAuthored=(byte)(c.RouteAuthored?1:0)});
                var regions=AddBuffer<SupportAuthoredGroundRegionElement>(entity);
                foreach(var r in c.Regions)regions.Add(new SupportAuthoredGroundRegionElement {Min=r.Min,Max=r.Max,Visible=(byte)(r.Visible?1:0),Protected=(byte)(r.Protected?1:0)});
            }
        }
    }
}
