using Game.Components;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiMissionTutorialFocusGateway
    {
        public const float MissionTutorialFocusSmoothTimeSeconds = 2.5f;

        public bool TryFocusMissionTutorialTarget(bool selection)
        {
            if (!TryReadMissionTutorialTarget(out var lesson) || !TryGetMissionRoot(out var em,out var root)) return false;
            float3 target = selection ? lesson.Selection : lesson.Destination;
            float height = 40f;
            if(!selection&&em.HasComponent<CampaignMissionRouteReopenedState>(root)&&
               em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).Prompt==CampaignMissionGuidancePromptKind.RouteRecords)
            {
                var records=em.GetComponentData<CampaignMissionRouteReopenedState>(root).RecordsBuilding;
                if(em.Exists(records)&&em.HasComponent<Unity.Transforms.LocalTransform>(records))
                {
                    // Keep the office and its entrance in the open space between
                    // the selected-unit card and ARIA, so preservation is visible.
                    target=(target+em.GetComponentData<Unity.Transforms.LocalTransform>(records).Position)*.5f;
                    height=50f;
                }
            }
            if (selection && lesson.DragSelection)
            {
                target = ((float3)lesson.SelectionMin + (float3)lesson.SelectionMax) * .5f;
                float3 size = (float3)lesson.SelectionMax - (float3)lesson.SelectionMin;
                float aspect = UnityEngine.Screen.height > 0
                    ? (float)UnityEngine.Screen.width / UnityEngine.Screen.height : 1f;
                // The side panels and command bar occupy much of the screen. Fit
                // the group inside the central playable viewport, not the full display.
                height = math.max(height, math.max(size.x / (math.max(.5f, aspect) * .42f), size.z / .5f) * 1.8f);
            }
            if (!math.all(math.isfinite(target))) return false;
            using var focus = em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            if (focus.CalculateEntityCount()!=1) return false;
            var request=CreateMissionTutorialFocusRequest(target,height);
            // The Oil hauler crosses this map while it is the selection target.
            // A long pan leaves its cue behind the HUD before a normal tap lands.
            if(selection && TryGetMissionRoot(out var missionManager,out var missionRoot) &&
               missionManager.GetComponentData<CampaignMissionRuntimeComponent>(missionRoot).MissionId.Equals(Game.Missions.Contracts.CampaignMissionSequence.SupplyLine))
                request.SmoothTimeSeconds=.65f;
            em.SetComponentData(focus.GetSingletonEntity(),request);
            return true;
        }

        public static RuntimeCameraFocusRequestComponent CreateMissionTutorialFocusRequest(float3 target,float height) => new()
        {
            Requested=1,
            Smooth=1,
            SmoothTimeSeconds=MissionTutorialFocusSmoothTimeSeconds,
            UseExplicitPerspective=1,
            Perspective=new float4(target.y+height,65f,0f,40f),
            World=target
        };
    }
}
