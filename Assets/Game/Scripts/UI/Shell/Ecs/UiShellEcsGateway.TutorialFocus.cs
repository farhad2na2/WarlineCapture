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
            if (!math.all(math.isfinite(target))) return false;
            using var focus = em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            if (focus.CalculateEntityCount()!=1) return false;
            em.SetComponentData(focus.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent {
                Requested=1, Smooth=1, SmoothTimeSeconds=.5f, UseExplicitPerspective=1,
                Perspective=new float4(target.y+40f,65f,0f,40f), World=target });
            return true;
        }
    }
}
