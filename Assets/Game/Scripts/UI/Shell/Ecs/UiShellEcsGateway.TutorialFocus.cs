using Game.Components;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiMissionTutorialFocusGateway
    {
        public bool TryFocusMissionTutorialTarget(bool selection)
        {
            if (!TryReadMissionTutorialTarget(out var lesson) || !TryGetMissionRoot(out var em,out _)) return false;
            float3 target = selection ? lesson.Selection : lesson.Destination;
            float height = 40f;
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
            em.SetComponentData(focus.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent {
                Requested=1, Smooth=1, SmoothTimeSeconds=.5f, UseExplicitPerspective=1,
                Perspective=new float4(target.y+height,65f,0f,40f), World=target });
            return true;
        }
    }
}
