using Game.Components;
using Unity.Entities;
using Unity.Burst;
namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SupportAbilityStartupSystem))]
    [UpdateBefore(typeof(SupportAbilityRequestSystem))]
    public partial struct SupportMissionContextProjectionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em=state.EntityManager;
            foreach(var (sessionRef,runtimeRef,root) in SystemAPI.Query<RefRO<SupportSessionComponent>,RefRO<CampaignMissionRuntimeComponent>>().WithEntityAccess())
            {
                if(sessionRef.ValueRO.TestEncounter!=0||!em.HasComponent<SupportMissionContextStampComponent>(root))continue;
                var runtime=runtimeRef.ValueRO;Entity source=Entity.Null;bool ambiguous=false;
                foreach(var (context,entity) in SystemAPI.Query<RefRO<SupportMissionContextComponent>>().WithEntityAccess())
                    if(context.ValueRO.MissionId.Equals(runtime.MissionId)&&context.ValueRO.OperationMapId.Equals(runtime.OperationMapId)&&context.ValueRO.MissionSourceVersion==runtime.SourceVersion)
                    {if(source!=Entity.Null)ambiguous=true;source=entity;}
                Apply(em,root,ambiguous?Entity.Null:source);
            }
        }
        public static void Apply(EntityManager em,Entity root,Entity source)
        {
            var session=em.GetComponentData<SupportSessionComponent>(root);
            var context=em.Exists(source)&&em.HasComponent<SupportMissionContextComponent>(source)&&em.HasBuffer<SupportAuthoredGroundRegionElement>(source)?em.GetComponentData<SupportMissionContextComponent>(source):default;
            if(context.Revision==0||context.MissionSourceVersion!=session.MissionSourceVersion)source=Entity.Null;
            var policy=em.GetComponentData<SupportMissionPolicyComponent>(root);policy.PopulationCeiling=source==Entity.Null?0:context.PopulationCeiling;em.SetComponentData(root,policy);
            var stamp=em.GetComponentData<SupportMissionContextStampComponent>(root);
            if(stamp.Source==source&&stamp.Revision==(source==Entity.Null?0:context.Revision)&&stamp.SessionToken.Equals(session.SessionToken)&&stamp.AttemptOrdinal==session.AttemptOrdinal&&stamp.MissionSourceVersion==session.MissionSourceVersion)return;
            var regions=em.GetBuffer<SupportGroundRegionElement>(root);regions.Clear();
            if(source!=Entity.Null)
            {
                policy.GroundMin=context.GroundMin;policy.GroundMax=context.GroundMax;
                foreach(var region in em.GetBuffer<SupportAuthoredGroundRegionElement>(source,true))regions.Add(new SupportGroundRegionElement {Min=region.Min,Max=region.Max,Visible=region.Visible,Protected=region.Protected,Version=context.Revision});
            }
            else policy.GroundMin=policy.GroundMax=default;
            em.SetComponentData(root,policy);
            em.SetComponentData(root,new SupportAirRouteComponent {SessionToken=session.SessionToken,AttemptOrdinal=session.AttemptOrdinal,Version=source==Entity.Null?0:context.Revision,Entry=context.Entry,Release=context.Release,Exit=context.Exit,Clearance=context.Clearance,Authored=(byte)(source==Entity.Null?0:context.RouteAuthored)});
            em.SetComponentData(root,new SupportMissionContextStampComponent {Source=source,Revision=source==Entity.Null?0:context.Revision,SessionToken=session.SessionToken,AttemptOrdinal=session.AttemptOrdinal,MissionSourceVersion=session.MissionSourceVersion});
        }
    }
}
