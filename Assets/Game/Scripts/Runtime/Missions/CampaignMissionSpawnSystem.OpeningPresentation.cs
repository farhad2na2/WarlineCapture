using Game.Components;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        private static readonly FixedString64Bytes EstablishBaseMissionId =
            "saga.ch01.m02.establish_base";
        private static readonly FixedString64Bytes EstablishBaseCameraStartAnchorId =
            "anchor.ch01.m02.camera_start";
        private static readonly FixedString64Bytes EstablishBaseForwardPostAnchorId =
            "anchor.ch01.m02.forward_post";
        private static readonly FixedString64Bytes EstablishBaseBuildLotAnchorId =
            "anchor.ch01.m02.build_lot";
        internal const float EstablishBaseFocusTowardBuildLot = 0.56f;
        private static readonly FixedString64Bytes RadarWarningMissionId = "saga.ch01.m03.radar_warning";
        private static readonly FixedString64Bytes RadarPost = "anchor.ch01.m03.forward_post";
        private static readonly FixedString64Bytes RadarFork = "anchor.ch01.m03.fork";

        internal static void FrameMissionCommandGroup(Unity.Entities.EntityManager em,
            in CampaignMissionRuntimeComponent runtime, ref OperationMapBlob map, ref CampaignMissionOpeningPresentationComponent opening)
        {
            FixedString64Bytes first="role.friendly.command_squad",second="role.friendly.heavy_apc",third=default;
            switch(runtime.MissionId.ToString())
            {
                case "saga.ch01.m04.airlift": second="role.friendly.rescue_apc";break;
                case "saga.ch02.m03.market_lifeline": case "saga.ch03.m01.signal_trace":
                case "saga.ch03.m02.safehouse_sweep": case "saga.ch03.m03.false_front":
                    first="role.market.rifle";second=default;break;
                case "saga.ch02.m04.power_relay": first="role.power.rifle";second="role.power.engineer";break;
                case "saga.ch02.m05.route_reopened": first="role.route.rifle";second="role.route.engineer";break;
                case "saga.ch03.m04.evidence_chain": second="role.friendly.evidence_apc";break;
                case "saga.ch04.m01.air_corridor": first="role.friendly.launcher";second="role.friendly.ground_sensor";break;
                case "saga.ch04.m04.grounded_signal": second="role.friendly.rescue_apc";break;
                case "saga.ch04.m05.armor_break": second="role.friendly.armor";break;
                case "saga.ch05.m02.trust_under_fire": first="role.friendly.escort.north";second="role.friendly.convoy.north";break;
                case "saga.ch05.m03.network_collapse": first="role.friendly.escort";second=default;break;
                case "saga.ch05.m04.last_corridor": first="role.friendly.escort";second="role.friendly.medicine";break;
                case "saga.ch05.m05.command_node": first="role.friendly.engineer";second="role.friendly.specialist";break;
            }
            FixedString64Bytes primaryGroup=runtime.MissionId.Equals(new FixedString64Bytes("saga.ch02.m02.supply_line"))
                ?new FixedString64Bytes("group.ch02.m02.squad_a"):default;
            using var query=em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent),typeof(Unity.Transforms.LocalTransform));
            using var entities=query.ToEntityArray(Allocator.Temp);
            float3 min=new float3(float.PositiveInfinity),max=new float3(float.NegativeInfinity);int count=0;
            foreach(var entity in entities)
            {
                var role=em.GetComponentData<CampaignMissionUnitRoleComponent>(entity);
                if(!role.SessionToken.Equals(runtime.SessionToken)||role.MissionRoleId.IsEmpty||(!role.MissionRoleId.Equals(first)&&!role.MissionRoleId.Equals(second)&&!role.MissionRoleId.Equals(third)))continue;
                if(!primaryGroup.IsEmpty&&!role.UnitGroupId.Equals(primaryGroup))continue;
                var p=em.GetComponentData<Unity.Transforms.LocalTransform>(entity).Position;
                min=math.min(min,p);max=math.max(max,p);count++;
            }
            if(runtime.MissionId.Equals(new FixedString64Bytes("saga.ch05.m01.citywide_alert")) &&
                TryFindAnchor(ref map,new FixedString64Bytes("anchor.ch05.m01.producer_clinic"),out var barracks))
            {
                // Recruitment is the first action. Show its real authored producer lot.
                min=barracks.Position;max=min+new float3(28,16,15);count=1;
            }
            if(runtime.MissionId.Equals(EstablishBaseMissionId) &&
                TryFindAnchor(ref map,EstablishBaseBuildLotAnchorId,out var buildArea))
            {
                // The opening instruction is construction. Show the complete legal
                // build area rather than the squad stationed across the road.
                float radius=math.max(20f,buildArea.Radius);
                min=buildArea.Position-new float3(radius,0,radius);
                max=buildArea.Position+new float3(radius,4,radius);count=1;
            }
            if(count==0)return;
            float3 center=(min+max)*.5f;
            // World focus is kept separate from the actual squad focus. Camera guidance
            // must never change the destinations used by gameplay or the narrated tour.
            float aspect=math.clamp((float)UnityEngine.Screen.width/math.max(1,UnityEngine.Screen.height),1.5f,2.4f);
            const float pitch=60f,fov=50f;
            float sine=math.sin(math.radians(pitch)),cosine=math.cos(math.radians(pitch)),tangent=math.tan(math.radians(fov*.5f));
            float height=48;float3 focus=center;
            for(;height<160;height+=4)
            {
                focus=center+new float3(.20f*height/sine*tangent*aspect,0,-.08f*height*tangent/(sine*sine));
                float3 camera=focus+new float3(0,height,-height*cosine/sine);
                bool fits=true;
                for(int corner=0;corner<8;corner++)
                {
                    float3 point=new float3((corner&1)==0?min.x-5:max.x+5,(corner&2)==0?min.y:max.y+4,(corner&4)==0?min.z-5:max.z+5);
                    float3 delta=point-camera;
                    float depth=-delta.y*sine+delta.z*cosine;
                    float2 uv=new float2(.5f+delta.x/(2*depth*tangent*aspect),.5f+(delta.y*cosine+delta.z*sine)/(2*depth*tangent));
                    if(depth<=0||uv.x<.12f||uv.x>.69f||uv.y<.28f||uv.y>.81f)fits=false;
                }
                if(fits)break;
            }
            focus=center+new float3(.20f*height/sine*tangent*aspect,0,-.08f*height*tangent/(sine*sine));
            opening.CommandCameraFocus=focus;
            opening.CommandCameraPerspective=new float4(center.y+height,pitch,0,fov);
        }

        internal static void ResolveOpeningPresentationFocus(
            in FixedString64Bytes missionId,
            ref OperationMapBlob map,
            float3 playerFocus,
            float3 hostileFocus,
            out float3 openingStartFocus,
            out float3 openingEndFocus,
            out float3 establishingFocus)
        {
            openingStartFocus = playerFocus;
            openingEndFocus = hostileFocus;
            establishingFocus = math.lerp(playerFocus, hostileFocus, 0.40f);
            if (missionId.Equals(RadarWarningMissionId) &&
                TryFindAnchor(ref map, RadarPost, out OperationMapAnchorBlob post) &&
                TryFindAnchor(ref map, RadarFork, out OperationMapAnchorBlob approach))
            {
                openingStartFocus = playerFocus;
                openingEndFocus = approach.Position;
                // Introduce the defenders and their road, not the decorative airfield around the post.
                establishingFocus = playerFocus;
                return;
            }
            if (!missionId.Equals(EstablishBaseMissionId) ||
                !TryFindAnchor(ref map, EstablishBaseCameraStartAnchorId, out OperationMapAnchorBlob cameraStart) ||
                !TryFindAnchor(ref map, EstablishBaseForwardPostAnchorId, out OperationMapAnchorBlob forwardPost) ||
                !TryFindAnchor(ref map, EstablishBaseBuildLotAnchorId, out OperationMapAnchorBlob buildLot))
                return;

            openingStartFocus = playerFocus;
            openingEndFocus = playerFocus;
            establishingFocus = math.lerp(
                forwardPost.Position,
                buildLot.Position,
                EstablishBaseFocusTowardBuildLot);
        }
    }
}
