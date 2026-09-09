using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        public bool TryReadMissionCameraTour()=>TryGetActiveCameraTour(out _,out _,out _);
        private bool TryGetActiveCameraTour(out EntityManager em,out Entity root,out CampaignMissionCameraTourState tour)
        {
            tour=default;
            if(!TryGetMissionRoot(out em,out root) || !em.HasComponent<CampaignMissionCameraTourState>(root) ||
                !em.HasComponent<CampaignMissionOpeningPresentationComponent>(root)) return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            var opening=em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root);
            tour=em.GetComponentData<CampaignMissionCameraTourState>(root);
            bool active=runtime.Phase==MissionPhaseKind.FindSquad && opening.Stage<6 ||
                runtime.Phase==MissionPhaseKind.SecureCorridor && em.HasComponent<CampaignMissionFinalePresentationComponent>(root) &&
                em.GetComponentData<CampaignMissionFinalePresentationComponent>(root).Stage<4;
            return runtime.Outcome==MissionOutcomeKind.None && active &&
                tour.SessionToken.Equals(runtime.SessionToken) && tour.AttemptOrdinal==runtime.AttemptOrdinal && tour.SourceVersion==runtime.SourceVersion;
        }
        private bool RequestCameraTourAction(UiMissionDefenseAction action)
        {
            if(!TryGetActiveCameraTour(out var em,out var root,out var tour)) return false;
            if(action==UiMissionDefenseAction.SkipCameraTour)
            {
                if(em.GetComponentData<CampaignMissionRuntimeComponent>(root).Phase==MissionPhaseKind.SecureCorridor) tour.FinaleSkipRequested=1;
                else tour.SkipRequested=1;
            }
            else tour.ReducedMotion=1;
            em.SetComponentData(root,tour); return true;
        }
    }
}
